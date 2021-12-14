using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using RaaiVan.Modules.GlobalUtilities;
using RaaiVan.Modules.Jobs;

namespace RaaiVan.Web.Ajax
{
    /// <summary>
    /// Summary description for StockAPI
    /// </summary>
    public class StockAPI : IHttpHandler, IRequiresSessionState
    {
        ParamsContainer paramsContainer = null;

        public void ProcessRequest(HttpContext context)
        {
            paramsContainer = new ParamsContainer(context);

            string responseText = string.Empty;
            string command = PublicMethods.parse_string(context.Request.Params["Command"], false);

            switch (command.ToLower())
            {
                case "create_csv_status":
                    paramsContainer.return_response("{\"Progress\": " + Math.Round(StockUtil.ItemProgress * 100, 4).ToString() + "% }");
                    return;
                case "csv":
                    {
                        PublicMethods.set_timeout(() =>
                        {
                            if (StockUtil.Processing) return;
                            else StockUtil.start_processing();

                            int? predictionLength = PublicMethods.parse_int(context.Request.Params["pl"]); //Prediction Length
                            int? slide = PublicMethods.parse_int(context.Request.Params["s"]); //Slide
                            int? slideStep = PublicMethods.parse_int(context.Request.Params["ss"]); //Slide Step
                            int? ignoreDays = PublicMethods.parse_int(context.Request.Params["id"]); //Ignore Days
                            bool? ignoreEqualization = PublicMethods.parse_bool(context.Request.Params["ie"]); //Ignore Equalization
                            string cn = PublicMethods.parse_string(context.Request.Params["cn"], false); //Cluster File Name

                            long totalCount = 0;
                            List<string> symbols = STController.get_active_symbols(null, ignoreDays,
                                STController.get_last_active_date(10 + (ignoreDays.HasValue && ignoreDays.Value > 0 ? ignoreDays.Value : 0)), 
                                null, 1000, 0, ref totalCount);

                            if (!string.IsNullOrEmpty(cn)) {
                                string filePath = PublicMethods.map_path("~/stock_analysis/" + cn + ".txt");
                                string symbolNames = !System.IO.File.Exists(filePath) ? string.Empty : System.IO.File.ReadAllText(filePath);
                                if(!string.IsNullOrEmpty(symbolNames))
                                    symbols = symbols.Where(s => symbolNames.Split(',').Any(l => l == s)).ToList();
                            }

                            Dictionary<string, List<StockItem>> dic = 
                                StockUtil.fill_empty_dates(STController.get_symbol_data(symbols, ignoreDays, null, null));

                            List<KeyValuePair<DateTime, double>> marketROC = STController.get_market_roc(ignoreDays, null, null);
                            Dictionary<string, List<StockItem>> marketSymbols = STController.get_market_symbols_data(ignoreDays, null, null);

                            string strRanges = PublicMethods.parse_string(context.Request.Params["range"], false);

                            string mapName = PublicMethods.parse_string(context.Request.Params["map"], false);
                            string fileName = PublicMethods.parse_string(context.Request.Params["filename"], false);

                            if (strRanges.IndexOf(":") > 0)
                            {
                                List<KeyValuePair<double, double>> ranges = new List<KeyValuePair<double, double>>();

                                strRanges.Split(',').Where(u => !string.IsNullOrEmpty(u)).ToList()
                                    .ForEach(x => {
                                        string[] parts = x.Split(':');

                                        if (parts.Length == 2)
                                        {
                                            double? key = PublicMethods.parse_double(parts[0]);
                                            double? value = PublicMethods.parse_double(parts[1]);

                                            if (key.HasValue && value.HasValue)
                                                ranges.Add(new KeyValuePair<double, double>(key.Value, value.Value));
                                        }
                                    });

                                StockUtil.create_csv(dic,
                                    marketSymbols,
                                    marketROC,
                                    !predictionLength.HasValue ? 0 : predictionLength.Value,
                                    !slide.HasValue ? 0 : slide.Value,
                                    !slideStep.HasValue ? 0 : slideStep.Value,
                                    ranges,
                                    ignoreEqualization.HasValue && ignoreEqualization.Value,
                                    mapName,
                                    PublicMethods.map_path("~/stock_analysis/" +
                                        (!string.IsNullOrEmpty(fileName) ? fileName : "stock_c" + ranges.Count.ToString()) + ".csv"));
                            }
                            else {
                                List<double> ranges = new List<double>();

                                try { strRanges.Split(',').ToList().ForEach(u => ranges.Add(double.Parse(u))); }
                                catch { ranges = new List<double>(); }

                                StockUtil.create_csv(dic,
                                    marketSymbols,
                                    marketROC,
                                    !predictionLength.HasValue ? 0 : predictionLength.Value,
                                    !slide.HasValue ? 0 : slide.Value,
                                    !slideStep.HasValue ? 0 : slideStep.Value,
                                    ranges.ToArray(),
                                    ignoreEqualization.HasValue && ignoreEqualization.Value,
                                    mapName,
                                    PublicMethods.map_path("~/stock_analysis/" +
                                        (!string.IsNullOrEmpty(fileName) ? fileName : "stock_c" + (ranges.Count - 1).ToString()) + ".csv"));
                            }
                        }, 0);

                        paramsContainer.return_response("{\"Message\":\"Job Started\"}");
                    }
                    return;
                case "correlations":
                    {
                        PublicMethods.set_timeout(() =>
                        {
                            if (StockUtil.Processing) return;
                            else StockUtil.start_processing();

                            int? period = PublicMethods.parse_int(context.Request.Params["period"]);

                            long totalCount = 0;
                            List<string> symbols = STController.get_active_symbols(null, null,
                                STController.get_last_active_date(10), null, 1000, 0, ref totalCount);
                            Dictionary<string, List<StockItem>> dic = STController.get_symbol_data(symbols, null, null, null);

                            string fileName = PublicMethods.parse_string(context.Request.Params["filename"], false);

                            StockUtil.correlation_csv(dic, !period.HasValue ? 200 : period.Value,
                                ListMaker.get_string_items(context.Request.Params["symbols"], ','),
                                PublicMethods.parse_double(context.Request.Params["ct"]), //Clustering Threshold
                                PublicMethods.map_path("~/stock_analysis/" + 
                                    (!string.IsNullOrEmpty(fileName) ? fileName : "correlations") + ".csv"));
                        }, 0);

                        paramsContainer.return_response("{\"Message\":\"Job Started\"}");
                    }
                    return;
            }

            paramsContainer.return_response("{\"message\":\"command not recognized\"}");
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}