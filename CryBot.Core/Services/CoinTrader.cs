﻿using CryBot.Core.Models;
using CryBot.Core.Utilities;

using Orleans;

using System;

using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

using System.Threading.Tasks;

using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public class CoinTrader
    {
        private readonly ICryptoApi _cryptoApi;
        private readonly IClusterClient _orleansClient;
        private readonly IHubNotifier _hubNotifier;
        private ITraderGrain _traderGrain;

        public CoinTrader(ICryptoApi cryptoApi, IClusterClient orleansClient, IHubNotifier hubNotifier)
        {
            _cryptoApi = cryptoApi;
            _orleansClient = orleansClient;
            _hubNotifier = hubNotifier;
        }

        public void Initialize(string market)
        {
            Market = market;
            Strategy = new HoldUntilPriceDropsStrategy();
            _cryptoApi.TickerUpdated
                .Where(t => t.Market == market)
                .Select(ticker => Observable.FromAsync(token => UpdatePrice(ticker)))
                .Concat()
                .Subscribe();

            _cryptoApi.OrderUpdated
                .Where(o => o.Market == market)
                .Select(order => Observable.FromAsync(token => UpdateOrder(order)))
                .Concat()
                .Subscribe();
        }

        public string Market { get; set; }

        public Ticker Ticker { get; set; }

        public ITradingStrategy Strategy { get; set; }

        public List<Trade> Trades { get; set; } = new List<Trade>();

        public Budget Budget { get; set; } = new Budget();

        public async Task<Unit> UpdatePrice(Ticker ticker)
        {
            //Console.WriteLine($"{ticker.Id} - {Budget}");
            Ticker = ticker;
            await UpdateTrades();
            Task.Run(async () =>
            {
                await _traderGrain.UpdateTrades(Trades);
                await _hubNotifier.UpdateTicker(ticker);
                var traderData = await _traderGrain.GetTraderData();
                traderData.CurrentTicker = ticker;
                await _hubNotifier.UpdateTrader(traderData);
            });
            return Unit.Default;
        }

        public async Task StartAsync()
        {
            _traderGrain = _orleansClient.GetGrain<ITraderGrain>(Market);
            await _traderGrain.SetMarketAsync(Market);
            var traderState = await _traderGrain.GetTraderData();
            Trades = traderState.Trades ?? new List<Trade>();
            /*var backtester = new BackTester(_cryptoApi);
            var bestSettings = await backtester.FindBestSettings(Market);
            Console.WriteLine($"Best settings for {Market} are {bestSettings.TraderSettings} with profit of {bestSettings.TraderStats.Profit}");
            Strategy.Settings = bestSettings.TraderSettings;
*/

            Strategy.Settings = traderState.Settings ?? TraderSettings.Default;
            if (Trades.Count == 0)
            {
                Trades.Add(new Trade { Status = TradeStatus.Empty });
            }
        }

        public async Task<Unit> UpdateOrder(CryptoOrder cryptoOrder)
        {
            switch (cryptoOrder.OrderType)
            {
                case CryptoOrderType.LimitSell:
                    Budget.Available += cryptoOrder.Price;
                    var tradeForSellOrder = Trades.FirstOrDefault(t => t.SellOrder.Uuid == cryptoOrder.Uuid);
                    if (tradeForSellOrder != null)
                    {
                        var tradeProfit = tradeForSellOrder.BuyOrder.Price.GetReadablePercentageChange(tradeForSellOrder.SellOrder.Price);
                        Budget.Profit += tradeProfit;
                        Budget.Earned += tradeForSellOrder.SellOrder.Price - tradeForSellOrder.BuyOrder.Price;
                        Console.WriteLine($"{cryptoOrder.Uuid}: SELL - {tradeProfit}");
                        tradeForSellOrder.Profit = tradeProfit;
                        tradeForSellOrder.Status = TradeStatus.Completed;
                    }
                    break;
                case CryptoOrderType.LimitBuy:
                    var tradeForBuyOrder = Trades.FirstOrDefault(t => t.BuyOrder.Uuid == cryptoOrder.Uuid);
                    if (tradeForBuyOrder != null)
                    {
                        tradeForBuyOrder.Status = TradeStatus.Bought;
                    }
                    break;
            }

            return await Task.FromResult(Unit.Default);
        }

        private async Task UpdateTrades()
        {
            List<Trade> newTrades = new List<Trade>();
            foreach (var trade in Trades.Where(t => t.Status != TradeStatus.Completed))
            {
                var newTrade = await UpdateTrade(trade);
                if (newTrade != Trade.Empty)
                {
                    newTrades.Add(newTrade);
                }
            }
            if (newTrades.Count > 0)
                Trades.AddRange(newTrades);
        }

        private async Task<Trade> UpdateTrade(Trade trade)
        {
            var tradeAction = Strategy.CalculateTradeAction(Ticker, trade);
            switch (tradeAction.TradeAdvice)
            {
                case TradeAdvice.Buy:
                    var buyOrder = await CreateBuyOrder(tradeAction.OrderPricePerUnit);
                    trade.BuyOrder = buyOrder;
                    trade.Status = TradeStatus.Buying;
                    break;
                case TradeAdvice.Sell:
                    await CreateSellOrder(trade, tradeAction.OrderPricePerUnit);
                    return new Trade { Status = TradeStatus.Empty };
                case TradeAdvice.Cancel:
                    Console.WriteLine($"{trade.BuyOrder.Uuid}: Canceling order {trade.BuyOrder.Uuid}");
                    var cancelResponse = await _cryptoApi.CancelOrder(trade.BuyOrder.Uuid);
                    if (cancelResponse.IsSuccessful)
                    {
                        Budget.Available += trade.BuyOrder.Price;
                        trade.Status = TradeStatus.Empty;
                    }
                    break;
            }

            return Trade.Empty;
        }

        private async Task CreateSellOrder(Trade trade, decimal pricePerUnit)
        {
            var sellOrder = new CryptoOrder
            {
                PricePerUnit = pricePerUnit,
                Limit = pricePerUnit,
                OrderType = CryptoOrderType.LimitSell,
                Market = Market,
                Price = pricePerUnit * trade.BuyOrder.Quantity,
                Quantity = trade.BuyOrder.Quantity,
                Uuid = Ticker.Id.ToString()
            };
            var sellResponse = await _cryptoApi.SellCoinAsync(sellOrder);
            if (sellResponse.IsSuccessful)
            {
                trade.Status = TradeStatus.Selling;
                trade.SellOrder = sellResponse.Content;
            }
        }

        private async Task<CryptoOrder> CreateBuyOrder(decimal pricePerUnit)
        {
            if (Budget.Available < Strategy.Settings.TradingBudget)
            {
                Budget.Available += Strategy.Settings.TradingBudget;
                Budget.Invested += Strategy.Settings.TradingBudget;
            }
            var priceWithoutCommission = Strategy.Settings.TradingBudget * Consts.BittrexCommission;
            var quantity = priceWithoutCommission / pricePerUnit;
            quantity = quantity.RoundSatoshi();
            var buyOrder = new CryptoOrder
            {
                PricePerUnit = pricePerUnit,
                Price = Strategy.Settings.TradingBudget,
                Quantity = quantity,
                IsOpened = true,
                Market = Market,
                Limit = pricePerUnit,
                Opened = Ticker.Timestamp,
                OrderType = CryptoOrderType.LimitBuy,
                Uuid = Ticker.Id.ToString()
            };
            var buyResponse = await _cryptoApi.BuyCoinAsync(buyOrder);
            if (buyResponse.IsSuccessful)
            {
                Budget.Available -= buyResponse.Content.Price;
            }

            return buyResponse.Content;
        }
    }
}