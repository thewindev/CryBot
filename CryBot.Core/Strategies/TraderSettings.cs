﻿using System;

namespace CryBot.Core.Strategies
{
    public class TraderSettings
    {
        public decimal HighStopLossPercentage { get; set; }

        public decimal StopLoss { get; set; }

        public decimal BuyTrigger { get; set; }
        
        public decimal MinimumTakeProfit { get; set; }
        
        public decimal BuyLowerPercentage { get; set; }
        
        public decimal TradingBudget { get; set; }

        public decimal FirstBuyLowerPercentage { get; set; }

        public TimeSpan ExpirationTime { get; set; }

        public static TraderSettings Default { get; } = new TraderSettings
        {
            //FBLP: -1| BLP: -2| MTP: 0| HSL: -10| SL: -2| BT: -4| ET: 1.00:00:00
            //{BLP: 0| MTP: 0| HSL: -5| SL: -4| BT: -2| ET: 1.00:00:00}
            FirstBuyLowerPercentage = -0M,
            BuyLowerPercentage = -0.5M,
            TradingBudget = 0.0012M,
            MinimumTakeProfit = 0.5M,
            HighStopLossPercentage = -5M,
            StopLoss = -5,
            BuyTrigger = -2M,
            MaxChildTrades = 1,
            ExpirationTime = TimeSpan.FromHours(1)
        };

        public int MaxChildTrades { get; set; }

        public override string ToString()
        {
            return $"FBLP: {FirstBuyLowerPercentage}| BLP: {BuyLowerPercentage}| MTP: {MinimumTakeProfit}| HSL: {HighStopLossPercentage}| SL: {StopLoss}| BT: {BuyTrigger}| ET: {ExpirationTime.ToString()}";
        }
    }
}