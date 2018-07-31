﻿using System;

namespace CryBot.Contracts
{
    public class Candle
    {
        public string Currency { get; set; }

        public DateTime Timestamp { get; set; }

        public decimal High { get; set; }

        public decimal Low { get; set; }

        public decimal Open { get; set; }

        public decimal Close { get; set; }

        public decimal Volume { get; set; }
    }
}