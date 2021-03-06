﻿using CryBot.Core.Trader;
using CryBot.Core.Strategies;
using CryBot.Core.Exchange.Models;

using Orleans;
using Orleans.Providers;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Storage
{
    [StorageProvider(ProviderName = "OrleansSqlStore")]
    public class TraderGrain : Grain<TraderState>, ITraderGrain
    {
        public Task UpdatePriceAsync(Ticker ticker)
        {
            State.CurrentTicker = ticker;
            return Task.CompletedTask;
        }

        public Task AddTradeAsync(Trade trade)
        {
            if (State.Trades == null)
                State.Trades = new List<Trade>();
            State.Trades.Add(trade);
            return WriteStateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            await WriteStateAsync();
            await base.OnDeactivateAsync();
        }

        public Task<List<Trade>> GetActiveTrades()
        {
            return Task.FromResult(State.Trades ?? new List<Trade>());
        }

        public Task<TraderSettings> GetSettings()
        {
            return Task.FromResult(State.Settings ?? TraderSettings.Default);
        }

        public Task<TraderState> GetTraderData()
        {
            return Task.FromResult(State);
        }

        public Task UpdateTrades(List<Trade> trades)
        {
            State.Trades = trades;
            return WriteStateAsync();
        }

        public Task SetMarketAsync(string market)
        {
            State.Market = market;
            return WriteStateAsync();
        }

        public Task SetBudgetAsync(Budget budget)
        {
            State.Budget = budget;
            return WriteStateAsync();
        }

        public Task<bool> IsInitialized()
        {
            return Task.FromResult(State.Trades != null && State.Trades.Count > 0);
        }
    }
}