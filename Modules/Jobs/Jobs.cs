using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using RaaiVan.Modules.GlobalUtilities;

namespace RaaiVan.Modules.Jobs
{
    public class Jobs
    {
        public static void run(Guid applicationId, string jobName)
        {
            jobName = jobName.ToLower();

            switch (jobName)
            {
                case "import_stock_data":
                    {
                        import_stock_data(applicationId);
                        break;
                    }
                default:
                    break;
            }
        }

        public static void import_stock_data(Guid applicationId)
        {
            string folderAddress = PublicMethods.map_path("~/stock");

            string[] filePaths = Directory.GetFiles(folderAddress);

            foreach (string path in filePaths) {
                try
                {
                    import_stock_file(applicationId, path);
                    File.Delete(path);
                }
                catch { }
            }
        }

        private static void import_stock_file(Guid applicationId, string fileAddress) {
            using (StreamReader reader = new StreamReader(fileAddress))
            {
                if (reader.EndOfStream) return;

                string[] header = reader.ReadLine().Split(',').ToList()
                    .Select(u => u.Replace("<", "").Replace(">", "").Trim().ToLower()).ToArray();

                List<StockItem> items = new List<StockItem>();

                while (!reader.EndOfStream)
                {
                    string[] values = reader.ReadLine().Split(',');

                    Dictionary<string, string> dic = new Dictionary<string, string>();

                    for (int i = 0; i < header.Length; ++i)
                        dic[header[i]] = values[i];

                    StockItem newItem = to_stock_item(dic);

                    if (newItem != null && newItem.validate()) items.Add(newItem);
                }

                PublicMethods.split_list<StockItem>(items, 200, itms => {
                    STController.add_stock_history(itms);
                });
            }
        }

        private static StockItem to_stock_item(Dictionary<string, string> dic)
        {
            StockItem item = new StockItem();

            try
            {
                item.Symbol = dic[StockItem.Map[StockParam.Symbol]];
                item.Date = new DateTime(int.Parse(dic[StockItem.Map[StockParam.Date]].Substring(0, 4)),
                    int.Parse(dic[StockItem.Map[StockParam.Date]].Substring(4, 2)),
                    int.Parse(dic[StockItem.Map[StockParam.Date]].Substring(6, 2)));
                item.PreviousPrice = double.Parse(dic[StockItem.Map[StockParam.PreviousPrice]]);
                item.OpeningPrice = double.Parse(dic[StockItem.Map[StockParam.OpeningPrice]]);
                item.ClosingPrice = double.Parse(dic[StockItem.Map[StockParam.ClosingPrice]]);
                item.HighestPrice = double.Parse(dic[StockItem.Map[StockParam.HighestPrice]]);
                item.LowestPrice = double.Parse(dic[StockItem.Map[StockParam.LowestPrice]]);
                item.LastPrice = double.Parse(dic[StockItem.Map[StockParam.LastPrice]]);
                item.Volume = double.Parse(dic[StockItem.Map[StockParam.Volume]]);
                item.TotalValue = double.Parse(dic[StockItem.Map[StockParam.TotalValue]]);
                item.TotalTransactions = double.Parse(dic[StockItem.Map[StockParam.TotalTransactions]]);
                item.Code = dic[StockItem.Map[StockParam.Code]];
                item.Name = dic[StockItem.Map[StockParam.Name]];
                item.LatinName = dic[StockItem.Map[StockParam.LatinName]];
            }
            catch { return null; }

            return item.Volume == 0 ? null : item;
        }
    }
}
