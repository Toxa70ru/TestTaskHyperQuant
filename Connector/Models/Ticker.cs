using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connector.Models
{
    public class Ticker
    {
        public string Symbol {  get; set; } //символьная пара
        public float Bid { get; set; } // цена покупки
        public float BidSize { get; set; } // размер заказа на покупку
        public float Ask { get; set; } // цена продажи
        public float AskSize { get; set; } // размер заказа на продажу
        public float DailyChange { get; set; } // изменение за день
        public float DailyChangeRelative { get; set; } // относительное изменение за день
        public float LastPrice { get; set; } // последняя цена
        public float Volume { get; set; } // объем
        public float High { get; set; } // максимум дня
        public float Low { get; set; } // минимум дня
    }
}
