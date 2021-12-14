using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using RaaiVan.Modules.GlobalUtilities;

namespace RaaiVan.Modules.Jobs
{
    class STDataProvider
    {
        private static string GetFullyQualifiedName(string name)
        {
            return "[dbo]." + "[ST_" + name + "]"; //'[dbo].' is database owner and 'CN_' is module qualifier
        }

        private static Dictionary<string, List<AnalysisStockItem>> _parse_analysis_stock_items(ref IDataReader reader)
        {
            Dictionary<string, List<AnalysisStockItem>> ret = new Dictionary<string, List<AnalysisStockItem>>();
            
            while (reader.Read())
            {
                try
                {
                    AnalysisStockItem item = new AnalysisStockItem();

                    if (!string.IsNullOrEmpty(reader["Code"].ToString())) item.Code = (string)reader["Code"];
                    if (!string.IsNullOrEmpty(reader["Symbol"].ToString())) item.Symbol = (string)reader["Symbol"];
                    if (!string.IsNullOrEmpty(reader["Date"].ToString())) item.Date = (DateTime)reader["Date"];
                    if (!string.IsNullOrEmpty(reader["DateDiff"].ToString())) item.DateDiff = (int)reader["DateDiff"];
                    if (!string.IsNullOrEmpty(reader["DayOfWeek"].ToString())) item.DayOfWeek = (int)reader["DayOfWeek"];
                    if (!string.IsNullOrEmpty(reader["ChangeRate"].ToString())) item.ChangeRate = (double)reader["ChangeRate"];
                    if (!string.IsNullOrEmpty(reader["HighestByLowest"].ToString())) item.HighestByLowest = (double)reader["HighestByLowest"];

                    if (Math.Abs(item.ChangeRate) > 0.1 || item.HighestByLowest < 0 || item.HighestByLowest > 1.2) continue;

                    if(!ret.ContainsKey(item.Symbol)) ret[item.Symbol] = new List<AnalysisStockItem>();

                    ret[item.Symbol].Add(item);
                }
                catch (Exception e) { string s = e.ToString(); }
            }

            if (!reader.IsClosed) reader.Close();

            return ret;
        }

        private static List<string> _parse_symbols(ref IDataReader reader, ref long totalCount)
        {
            List<string> ret = new List<string>();

            while (reader.Read())
            {
                try
                {
                    if (!string.IsNullOrEmpty(reader["Symbol"].ToString())) ret.Add((string)reader["Symbol"]);
                    if (!string.IsNullOrEmpty(reader["TotalCount"].ToString())) totalCount = (long)reader["TotalCount"];
                }
                catch (Exception e) { }
            }

            if (!reader.IsClosed) reader.Close();

            return ret;
        }

        private static List<KeyValuePair<DateTime, double>> _parse_market_roc(ref IDataReader reader)
        {
            List<KeyValuePair<DateTime, double>> ret = new List<KeyValuePair<DateTime, double>>();

            while (reader.Read())
            {
                try
                {
                    ret.Add(new KeyValuePair<DateTime, double>((DateTime)reader["Date"], (double)reader["Value"]));
                }
                catch (Exception e) { }
            }

            if (!reader.IsClosed) reader.Close();

            return ret;
        }

        private static Dictionary<string, List<StockItem>> _parse_stock_items(ref IDataReader reader)
        {
            Dictionary<string, List<StockItem>> ret = new Dictionary<string, List<StockItem>>();

            while (reader.Read())
            {
                try
                {
                    StockItem item = new StockItem();

                    if (!string.IsNullOrEmpty(reader["Symbol"].ToString())) item.Symbol = (string)reader["Symbol"];
                    if (!string.IsNullOrEmpty(reader["Date"].ToString())) item.Date = (DateTime)reader["Date"];
                    if (!string.IsNullOrEmpty(reader["Code"].ToString())) item.Code = (string)reader["Code"];
                    if (!string.IsNullOrEmpty(reader["PreviousPrice"].ToString())) item.PreviousPrice = (double)reader["PreviousPrice"];
                    if (!string.IsNullOrEmpty(reader["OpeningPrice"].ToString())) item.OpeningPrice = (double)reader["OpeningPrice"];
                    if (!string.IsNullOrEmpty(reader["ClosingPrice"].ToString())) item.ClosingPrice = (double)reader["ClosingPrice"];
                    if (!string.IsNullOrEmpty(reader["HighestPrice"].ToString())) item.HighestPrice = (double)reader["HighestPrice"];
                    if (!string.IsNullOrEmpty(reader["LowestPrice"].ToString())) item.LowestPrice = (double)reader["LowestPrice"];
                    if (!string.IsNullOrEmpty(reader["LastPrice"].ToString())) item.LastPrice = (double)reader["LastPrice"];
                    if (!string.IsNullOrEmpty(reader["Volume"].ToString())) item.Volume = (double)reader["Volume"];
                    if (!string.IsNullOrEmpty(reader["TotalValue"].ToString())) item.TotalValue = (double)reader["TotalValue"];
                    if (!string.IsNullOrEmpty(reader["TotalTransactions"].ToString())) item.TotalTransactions = (double)reader["TotalTransactions"];

                    if (!ret.ContainsKey(item.Symbol)) ret[item.Symbol] = new List<StockItem>();

                    ret[item.Symbol].Add(item);
                }
                catch (Exception e) {  }
            }

            if (!reader.IsClosed) reader.Close();

            return ret;
        }

        public static bool AddStockHistory(List<StockItem> items)
        {
            SqlConnection con = new SqlConnection(ProviderUtil.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;

            //Add Items
            DataTable stockTable = new DataTable();
            stockTable.Columns.Add(StockParam.Code.ToString(), typeof(string));
            stockTable.Columns.Add(StockParam.Date.ToString(), typeof(DateTime));
            stockTable.Columns.Add(StockParam.Symbol.ToString(), typeof(string));
            stockTable.Columns.Add(StockParam.PreviousPrice.ToString(), typeof(double));
            stockTable.Columns.Add(StockParam.OpeningPrice.ToString(), typeof(double));
            stockTable.Columns.Add(StockParam.ClosingPrice.ToString(), typeof(double));
            stockTable.Columns.Add(StockParam.HighestPrice.ToString(), typeof(double));
            stockTable.Columns.Add(StockParam.LowestPrice.ToString(), typeof(double));
            stockTable.Columns.Add(StockParam.LastPrice.ToString(), typeof(double));
            stockTable.Columns.Add(StockParam.Volume.ToString(), typeof(double));
            stockTable.Columns.Add(StockParam.TotalValue.ToString(), typeof(double));
            stockTable.Columns.Add(StockParam.TotalTransactions.ToString(), typeof(double));
            stockTable.Columns.Add(StockParam.Name.ToString(), typeof(string));
            stockTable.Columns.Add(StockParam.LatinName.ToString(), typeof(string));

            foreach (StockItem item in items)
            {
                stockTable.Rows.Add(item.Code, item.Date, item.Symbol, item.PreviousPrice, item.OpeningPrice, item.ClosingPrice,
                    item.HighestPrice, item.LowestPrice, item.LastPrice, item.Volume, item.TotalValue, item.TotalTransactions,
                    PublicMethods.verify_string(item.Name.Substring(0, Math.Min(90, item.Name.Length))),
                    PublicMethods.verify_string(item.LatinName.Substring(0, Math.Min(90, item.LatinName.Length))));
            }

            SqlParameter stockParam = new SqlParameter("@Stock", SqlDbType.Structured);
            stockParam.TypeName = "[dbo].[StockItemTableType]";
            stockParam.Value = stockTable;
            //end of Add Items

            cmd.Parameters.Add(stockParam);

            string arguments = "@Stock";
            cmd.CommandText = ("EXEC" + " " + GetFullyQualifiedName("AddStockHistory") + " " + arguments);

            con.Open();

            try { return ProviderUtil.succeed((IDataReader)cmd.ExecuteReader()); }
            catch (Exception ex) { string strEx = ex.ToString(); return false; }
            finally { con.Close(); }
        }

        public static Dictionary<string, List<AnalysisStockItem>> CollectDataForAnalysis(int lastNDays, int ignorefirstNDays = 0)
        {
            try
            {
                IDataReader reader = ProviderUtil.execute_reader(GetFullyQualifiedName("CollectDataForAnalysis"),
                    lastNDays, ignorefirstNDays);
                return _parse_analysis_stock_items(ref reader);
            }
            catch (Exception ex) { return new Dictionary<string, List<AnalysisStockItem>> (); }
        }

        public static DateTime? GetLastActiveDate(int? ignoreDays)
        {
            try
            {
                if (ignoreDays.HasValue && ignoreDays.Value <= 0) ignoreDays = null;

                return ProviderUtil.succeed_datetime(ProviderUtil.execute_reader(GetFullyQualifiedName("GetLastActiveDate"), ignoreDays));
            }
            catch (Exception ex) { return null; }
        }

        public static List<string> GetActiveSymbols(string searchText, int? ignoreDays,
            DateTime? dateFrom, DateTime? dateTo, int count, int lowerBoundary, ref long totalCount)
        {
            try
            {
                IDataReader reader = ProviderUtil.execute_reader(GetFullyQualifiedName("GetActiveSymbols"),
                    searchText, ignoreDays, dateFrom, dateTo, count, lowerBoundary);
                return _parse_symbols(ref reader, ref totalCount);
            }
            catch (Exception ex) { return new List<string>(); }
        }

        public static List<KeyValuePair<DateTime, double>> GetMarketROC(int? ignoreDays, DateTime? dateFrom, DateTime? dateTo)
        {
            try
            {
                if (ignoreDays.HasValue && ignoreDays.Value <= 0) ignoreDays = null;

                IDataReader reader = ProviderUtil.execute_reader(GetFullyQualifiedName("GetMarketROC"), ignoreDays, dateFrom, dateTo);
                return _parse_market_roc(ref reader);
            }
            catch (Exception ex) { return new List<KeyValuePair<DateTime, double>>(); }
        }

        public static Dictionary<string, List<StockItem>> GetSymbolData(List<string> symbols, 
            int? ignoreDays, DateTime? dateFrom, DateTime? dateTo)
        {
            try
            {
                IDataReader reader = ProviderUtil.execute_reader(GetFullyQualifiedName("GetSymbolData"),
                    string.Join(",", symbols), ',', ignoreDays, dateFrom, dateTo);
                return _parse_stock_items(ref reader);
            }
            catch (Exception ex) { return new Dictionary<string, List<StockItem>>(); }
        }

        public static Dictionary<string, List<StockItem>> GetMarketSymbolsData(int? ignoreDays, DateTime? dateFrom, DateTime? dateTo)
        {
            try
            {
                IDataReader reader = ProviderUtil.execute_reader(GetFullyQualifiedName("GetMarketSymbolsData"), ignoreDays, dateFrom, dateTo);
                return _parse_stock_items(ref reader);
            }
            catch (Exception ex) { return new Dictionary<string, List<StockItem>>(); }
        }
    }
}
