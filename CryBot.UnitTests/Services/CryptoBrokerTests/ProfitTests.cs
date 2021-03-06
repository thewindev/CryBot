﻿using CryBot.Core.Trader;
using CryBot.Core.Exchange.Models;

using FluentAssertions;

using System.Threading.Tasks;

using Xunit;

namespace CryBot.UnitTests.Services.CryptoBrokerTests
{
    public class ProfitTests : CoinTraderTestBase
    {
        [Fact]
        public async Task SoldCoin_Should_InvokeOrderUpdated()
        {
            await CoinTrader.UpdateOrder(new CryptoOrder { IsClosed = true, OrderType = CryptoOrderType.LimitSell, Price = 1100 });
            CoinTrader.TraderState.Budget.Available.Should().Be(1100);
        }

        [Fact]
        public async Task SoldCoin_Should_CalculateCorrectProfit()
        {
            var sellOrder = new CryptoOrder { IsClosed = true, OrderType = CryptoOrderType.LimitSell, Price = 1100, Uuid = "S" };
            var trade = new Trade
            {
                BuyOrder = new CryptoOrder
                {
                    Price = 1000
                },
                SellOrder = sellOrder
            };
            CoinTrader.TraderState.Trades.Add(trade);
            CoinTrader.TraderState.Budget.Profit = 5;
            CoinTrader.TraderState.Budget.Earned = 1;

            await CoinTrader.UpdateOrder(sellOrder);

            CoinTrader.TraderState.Budget.Profit.Should().Be(15);
            CoinTrader.TraderState.Budget.Earned.Should().Be(101);
            trade.Profit.Should().Be(10);
        }
    }
}
