using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using RaaiVan.Modules.GlobalUtilities;

namespace RaaiVan.Modules.Jobs
{
    public class STController
    {
        public static bool add_stock_history(List<StockItem> items)
        {
            return items != null && items.Count > 0 && STDataProvider.AddStockHistory(items);
        }

        public static Dictionary<string, List<AnalysisStockItem>> collect_data_for_analysis(int lastNDays, int ignorefirstNDays = 0)
        {
            return STDataProvider.CollectDataForAnalysis(lastNDays, ignorefirstNDays);
        }

        public static DateTime? get_last_active_date(int? ignoreDays)
        {
            return STDataProvider.GetLastActiveDate(ignoreDays);
        }

        public static List<string> get_active_symbols(string searchText, int? ignoreDays,
            DateTime? dateFrom, DateTime? dateTo, int count, int lowerBoundary, ref long totalCount)
        {
            return STDataProvider.GetActiveSymbols(searchText, ignoreDays, dateFrom, dateTo, count, lowerBoundary, ref totalCount);
        }

        public static List<KeyValuePair<DateTime, double>> get_market_roc(int? ignoreDays, DateTime? dateFrom, DateTime? dateTo)
        {
            return STDataProvider.GetMarketROC(ignoreDays, dateFrom, dateTo);
        }

        public static Dictionary<string, List<StockItem>> get_symbol_data(List<string> symbols, 
            int? ignoreDays, DateTime? dateFrom, DateTime? dateTo)
        {
            return STDataProvider.GetSymbolData(symbols, ignoreDays, dateFrom, dateTo);
        }

        public static Dictionary<string, List<StockItem>> get_market_symbols_data(int? ignoreDays, DateTime? dateFrom, DateTime? dateTo)
        {
            Dictionary<string, List<StockItem>> dic = STDataProvider.GetMarketSymbolsData(ignoreDays, dateFrom, dateTo);

            List<KeyValuePair<string, string>> items = new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>("شاخص_صنعت6", "industry"),
                new KeyValuePair<string, string>("شاخص_قیمت6", "price"),
                new KeyValuePair<string, string>("شاخص_کل6", "total")
            };

            Dictionary<string, List<StockItem>> ret = new Dictionary<string, List<StockItem>>();

            items.Where(i => dic.ContainsKey(i.Key)).ToList().ForEach(i =>
            {
                ret[i.Value] = dic[i.Key];
                ret[i.Value].Reverse();
            });

            return ret;
        }
    }
}
