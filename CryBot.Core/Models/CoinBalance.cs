﻿namespace CryBot.Core.Models
{
    public class CoinBalance
    {
        public decimal Balance { get; set; }

        public decimal PricePerUnit { get; set; }

        public decimal Price { get; set; }

        public string Currency { get; set; }

        public string MarketName { get; set; }
    }
}