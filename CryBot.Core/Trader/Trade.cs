using CryBot.Core.Strategies;
using CryBot.Core.Exchange.Models;

using System;

using System.Collections.Generic;

namespace CryBot.Core.Trader
{
    public class Trade
    {
        public Trade()
        {
            Id = Guid.NewGuid();    
        }

        public Guid Id { get; set; }

        public List<Guid> ChildTrades { get; set; } = new List<Guid>();

        public CryptoOrder BuyOrder { get; set; } = new CryptoOrder();

        public CryptoOrder SellOrder { get; set; } = new CryptoOrder();

        public decimal MaxPricePerUnit { get; set; }

        public string Market { get; set; }

        public decimal Profit { get; set; }

        public ITradingStrategy Strategy { get; set; }

        public Ticker CurrentTicker { get; set; } = new Ticker();

        public bool TriggeredBuy { get; set; }

        public TradeStatus Status { get; set; }

        public static Trade Empty { get; set; } = new Trade();

        public TradeReason BuyReason { get; set; }

        public TradeReason SellReason { get; set; }
    }
}