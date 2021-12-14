using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RaaiVan.Modules.GlobalUtilities;

namespace NetTrader.Indicator
{
    public class Ohlc
    {
        public DateTime Date { get; set; }

        public double Open { get; set; }

        public double High { get; set; }

        public double Low { get; set; }

        public double Close { get; set; }

        public double Volume { get; set; }

        public double AdjClose { get; set; }

        public double Previous { get; set; }

        public static Ohlc fromStockItem(StockItem item) {
            return new Ohlc()
            {
                Date = item.Date,
                Open = item.OpeningPrice,
                Close = item.ClosingPrice,
                High = item.HighestPrice,
                Low = item.LowestPrice,
                Volume = item.Volume,
                AdjClose = item.LastPrice,
                Previous = item.PreviousPrice
            };
        }

        public static List<Ohlc> fromStockItems(List<StockItem> items)
        {
            return items.Select(u => Ohlc.fromStockItem(u)).Where(x => x != null).OrderBy(o => o.Date).ToList();
        }
    }
}
