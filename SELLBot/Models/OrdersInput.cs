﻿namespace SELLBot.Models
{
    public class OrdersInput
    {
        public string seller { get; set; }
        public string buyer { get; set; }
        public decimal price { get; set; }
        public decimal quantity { get; set; }
        public decimal ask { get; set; }
        public decimal bid { get; set; }
        public decimal lastPrice { get; set; }
        public string exchange { get; set; }
        public string percentage { get; set; }
        public decimal coinDecimals { get; set; }
    }
}
