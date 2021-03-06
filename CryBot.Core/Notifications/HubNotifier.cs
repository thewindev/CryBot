﻿using CryBot.Core.Storage;
using CryBot.Core.Exchange.Models;

using Microsoft.AspNetCore.SignalR;

using System.Threading.Tasks;

namespace CryBot.Core.Notifications
{
    public class HubNotifier: IHubNotifier
    {
        private readonly IHubContext<ApplicationHub> _hub;

        public HubNotifier(IHubContext<ApplicationHub> hub)
        {
            _hub = hub;
        }
        
        public async Task UpdateTrader(TraderState trader)
        {
            await _hub.Clients.All.SendAsync("traderUpdate:" + trader.Market, trader);
        }

        public async Task UpdateTicker(Ticker ticker)
        {
            await _hub.Clients.All.SendAsync("priceUpdate:" + ticker.Market, ticker);
        }
    }
}