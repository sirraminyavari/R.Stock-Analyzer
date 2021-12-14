using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using RaaiVan.Modules.GlobalUtilities;
using System.Collections;

namespace RaaiVan.Modules.Jobs
{
    public class StockUtil
    {
        private static bool _Processing = false;
        private static double _Progress = 0;
        private static double _ItemProgress = 0;
        private static double _ProcessedFiles = 0;

        public static bool Processing { get { return _Processing; } }

        public static double Progress { get { return _Progress; } }

        public static double ItemProgress { get { return _ItemProgress; } }

        public static double ProcessedFiles { get { return _ProcessedFiles; } }

        public static void start_processing() {
            if (_Processing) return;
            _Progress = _ItemProgress = _ProcessedFiles = 0;
            _Processing = true;
        }

        public static void create_csv(int predictionLength, int periodLength, int slide, int slideStep, 
            int ignoreFirstNDays, List<AnalysisParam> parameters, double[] ranges, string fullOutputFileName)
        {
            if (predictionLength < 0) predictionLength = 0;
            if (slide < 0) slide = 0;
            if (slideStep < 1) slideStep = 1;
            parameters = parameters.Distinct().ToList();
            ranges = ranges.ToList().OrderBy(r => r).ToArray();

            int totalLength = predictionLength + periodLength + (slide * slideStep);
            int lastNDays = (int)(totalLength * 2);

            Dictionary<string, List<AnalysisStockItem>> symbolsDic = 
                STController.collect_data_for_analysis(lastNDays, ignoreFirstNDays);

            List<string> tempVarNames = "a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z"
                .Split(',').Take(parameters.Count).ToList();

            string header = "target,class,symbol," + string.Join(",", Enumerable.Range(1, periodLength)
                .Select(u => string.Join(",", tempVarNames.Select(v => v + u.ToString()))));

            Dictionary<double, List<string>> rangeItems = new Dictionary<double, List<string>>();

            foreach (string symbol in symbolsDic.Keys)
            {
                if (symbolsDic[symbol].Count < totalLength) continue;

                Enumerable.Range(0, slide + 1).ToList().ForEach(s =>
                {
                    double target = predictionLength == 1 ? 0 :
                        Enumerable.Range((s * slideStep), predictionLength)
                            .Select(r => symbolsDic[symbol][r].ChangeRate)
                            .Aggregate(1.0, (acc, x) => acc * (1 + x));
                    target = Math.Round(target - 1, 6);

                    if (ranges.Length > 0 && (target < ranges[0] || target > ranges[ranges.Length - 1])) return;

                    bool isZeroOne = ranges.Length == 2 && ranges[1] > ranges[0];
                    double _class = 0;
                    double dicKey = 0;

                    if (isZeroOne)
                        _class = Math.Round((target - ranges[0]) / (ranges[1] - ranges[0]), 4);
                    else if (ranges.Length > 0)
                    {
                        for (int ind = 1; ind < ranges.Length; ++ind)
                        {
                            if (target <= ranges[ind])
                            {
                                dicKey = _class = ind - 1;
                                break;
                            }
                        }
                    }

                    string features = string.Join(",", Enumerable.Range((s * slideStep) + predictionLength, periodLength)
                        .Select(r => symbolsDic[symbol][r].toCSV(parameters)));

                    string newLine = target.ToString() + "," + _class.ToString() + "," + symbol + "," + features;

                    if (!rangeItems.ContainsKey(dicKey)) rangeItems[dicKey] = new List<string>();

                    rangeItems[dicKey].Add(newLine);
                });
            }

            int minCount = rangeItems.Keys.ToList().Select(k => rangeItems[k].Count).Min();
            List<string> items = new List<string>();

            rangeItems.Keys.ToList().ForEach(k => {
                items.AddRange(rangeItems[k].OrderBy(i => PublicMethods.get_random_number(5)).Take(minCount));
            });

            StringBuilder csvData = new StringBuilder();
            csvData.AppendLine(header);

            items.OrderBy(i => PublicMethods.get_random_number(5)).ToList()
                .ForEach(line => csvData.AppendLine(line));

            File.WriteAllText(fullOutputFileName, csvData.ToString(), Encoding.UTF8);
        }

        public static void create_csv(Dictionary<string, List<StockItem>> data, Dictionary<string, List<StockItem>> marketSymbols,
            List<KeyValuePair<DateTime, double>> marketROC, int predictionLength, int slide, int slideStep,
            List<KeyValuePair<double, double>> ranges, bool ignoreEqualization, string mapName, string fullOutputFileName)
        {
            string mapFileName = PublicMethods.map_path("~/stock_maps/" + (string.IsNullOrEmpty(mapName) ? "default" : mapName) + ".json");

            Dictionary<string, object> optionsMap = !File.Exists(mapFileName) ? null : 
                PublicMethods.fromJSON(File.ReadAllText(mapFileName));

            if (optionsMap == null || optionsMap.Keys.Count == 0) return;

            _Processing = true;

            if (predictionLength < 0) predictionLength = 0;
            if (slide < 0) slide = 0;
            if (slideStep < 1) slideStep = 1;

            if (predictionLength == 0 || ranges == null) ranges = new List<KeyValuePair<double, double>>();

            ranges = ranges.Select(r => r.Key <= r.Value ? r : new KeyValuePair<double, double>(r.Value, r.Key))
                .OrderBy(r => r.Key).ToList();

            List<DateTime> activeDates = get_active_dates(data);

            Dictionary<double, List<Dictionary<string, object>>> rangeItems = new Dictionary<double, List<Dictionary<string, object>>>();

            //Extract Transforms
            List<KeyValuePair<string, Filter>> transforms = new List<KeyValuePair<string, Filter>>();

            if (optionsMap.ContainsKey("transforms") && optionsMap["transforms"].GetType() == typeof(ArrayList))
            {
                transforms = ((ArrayList)optionsMap["transforms"]).ToArray()
                    .Where(u => u.GetType() == typeof(Dictionary<string, object>))
                    .Select(x =>
                    {
                        Dictionary<string, object> dic = (Dictionary<string, object>)x;

                        string name = dic.ContainsKey("name") ? dic["name"].ToString() : string.Empty;
                        Filter operation = !dic.ContainsKey("operation") || dic["operation"].GetType() != typeof(Dictionary<string, object>) ?
                            null : new Filter((Dictionary<string, object>)dic["operation"]);

                        if (!string.IsNullOrEmpty(name) && operation != null && !operation.isLogical())
                            return new KeyValuePair<string, Filter>(name, operation);
                        else return new KeyValuePair<string, Filter>(null, null);
                    })
                    .Where(f => !string.IsNullOrEmpty(f.Key) && f.Value != null).ToList();
            }
            //end of Extract Transforms

            //Extract Filters
            List<Filter> filters = new List<Filter>();

            if (optionsMap.ContainsKey("filters") && optionsMap["filters"].GetType() == typeof(ArrayList)) {
                filters = ((ArrayList)optionsMap["filters"]).ToArray()
                    .Where(u => u.GetType() == typeof(Dictionary<string, object>))
                    .Select(x => new Filter((Dictionary<string, object>)x))
                    .Where(f => f.isLogical()).ToList();
            }
            //end of Extract Filters

            //Extract Drops
            List<string> drops = new List<string>();

            if (optionsMap.ContainsKey("drop") && optionsMap["drop"].GetType() == typeof(ArrayList))
            {
                drops = ((ArrayList)optionsMap["drop"]).ToArray().Where(u => u.GetType() == typeof(string))
                    .Select(x => (string)x).Where(v => !string.IsNullOrEmpty(v)).Distinct().ToList();
            }
            //end of Extract Drops

            double totalCount = (slide + 1) * data.Keys.Count;
            double processedCount = 0;

            _ItemProgress = 0;

            data.Keys.ToList().ForEach(symbol =>
            {
                int validDataCount = 70;

                int rangeFrom = data[symbol].Count - 500;
                int offset = rangeFrom < 0 ? -rangeFrom : 0;
                data[symbol] = data[symbol].GetRange(rangeFrom + offset, 500 - offset);

                Enumerable.Range(0, slide + 1).ToList().ForEach(s =>
                {
                    processedCount++;
                    _ItemProgress = processedCount / totalCount;

                    int targetStartIndex = data[symbol].Count - predictionLength - (s * slideStep);

                    if (targetStartIndex < validDataCount) return;

                    List<StockItem> targetList = data[symbol].GetRange(targetStartIndex, predictionLength);
                    List<StockItem> trainList = data[symbol].GetRange(0, targetStartIndex);

                    int targetEmptyCount = StockUtil.max_consecutive_empty_days(targetList);
                    int trainEmptyCount = StockUtil.max_consecutive_empty_days(trainList.GetRange(trainList.Count - validDataCount, validDataCount));

                    //if (targetEmptyCount >= predictionLength) return;

                    //if true, checks if the target hits a particular rate within the period
                    bool hitMode = ranges.Count > 0 && !ranges.Any(r => r.Key != r.Value);
                    
                    bool isZeroOne = ranges.Count == 1 && !hitMode;
                    double _class = 0;
                    double dicKey = 0;

                    double target = 1;

                    if (hitMode) {
                        target = targetList.Select(r => r.ChangeRate).Aggregate(1.0, (acc, x) =>
                        {
                            double newValue = acc * (1 + x);
                            int ind = ranges.FindLastIndex(rng => (newValue - 1) >= rng.Key);
                            if (ind >= 0 && ind >= _class) dicKey = _class = ind + 1;
                            return newValue;
                        });

                        target = Math.Round(target - 1, 6);
                    }
                    else if(ranges.Count > 0)
                    {
                        double maxTarget = -1000;

                        target = targetList.Select(r => r.ChangeRate).Aggregate(1.0, (acc, x) =>
                        {
                            double newValue = acc * (1 + x);
                            if (newValue > maxTarget) maxTarget = newValue;
                            return newValue;
                        });

                        maxTarget = Math.Round(maxTarget - 1, 6);
                        target = Math.Round(target - 1, 6);

                        if (!ranges.Any(r => target >= r.Key && target <= r.Value)) return;

                        if (isZeroOne)
                        {
                            _class = Math.Round((maxTarget - ranges[0].Key) / (ranges[0].Value - ranges[0].Key), 4);

                            if (_class >= 1) _class = 0.9999;
                            else if (_class <= 0) _class = 0.0001;
                        }
                        else
                        {
                            for (int ind = 0; ind < ranges.Count; ++ind)
                            {
                                if (target <= ranges[ind].Value)
                                {
                                    dicKey = _class = ind;
                                    break;
                                }
                            }
                        }
                    }

                    int dateIndex = (s * slideStep) + targetList.Count;
                    int cnt = 500;
                    List<double> mRoc = marketROC.GetRange(dateIndex, Math.Min(cnt, marketROC.Count - dateIndex))
                        .Select(u => u.Value).ToList();

                    Dictionary<string, List<StockItem>> msItems = new Dictionary<string, List<StockItem>>();
                    marketSymbols.Keys.ToList().ForEach(k => msItems[k] = marketSymbols[k]
                        .GetRange(dateIndex, Math.Min(cnt, marketSymbols[k].Count - dateIndex)).ToList());

                    Dictionary<string, object> features = StockUtil.calculate_indicators(symbol, trainList, optionsMap, mRoc, msItems);

                    if (features != null)
                    {
                        features["target"] = optionsMap.ContainsKey("fill_target_with_slide") &&
                            optionsMap["fill_target_with_slide"].ToString().ToLower() == "true"  ? s : target;
                        features["class"] = _class;
                        features["symbol"] = symbol;

                        bool? hasDayOfWeek = !optionsMap.ContainsKey("day_of_week") ? false :
                            PublicMethods.parse_bool(optionsMap["day_of_week"].ToString());

                        if (hasDayOfWeek.HasValue && hasDayOfWeek.Value)
                        {
                            DayOfWeek dayOfWeek = trainList.Last().Date.DayOfWeek;

                            features["is_saturday"] = dayOfWeek == DayOfWeek.Saturday ? 1 : 0;
                            features["is_sunday"] = dayOfWeek == DayOfWeek.Sunday ? 1 : 0;
                            features["is_monday"] = dayOfWeek == DayOfWeek.Monday ? 1 : 0;
                            features["is_tuesday"] = dayOfWeek == DayOfWeek.Tuesday ? 1 : 0;
                            features["is_wednesday"] = dayOfWeek == DayOfWeek.Wednesday ? 1 : 0;
                        }


                        bool? hasIdleDays = !optionsMap.ContainsKey("idle_days") ? false :
                            PublicMethods.parse_bool(optionsMap["idle_days"].ToString());

                        if (hasIdleDays.HasValue && hasIdleDays.Value)
                        {
                            int idleDaysPeriod = 5;

                            int lastIdleDays = predictionLength > 0 ? trainList[trainList.Count - 1].IdleDays :
                                get_days_diff(activeDates, trainList[trainList.Count - 1].Date, activeDates.Last());

                            features["last_idle_days"] = lastIdleDays;
                            features["idle_days"] = trainList
                                .GetRange(trainList.Count - idleDaysPeriod - 1, idleDaysPeriod).Select(u => u.IdleDays).Sum();
                        }

                        if (!rangeItems.ContainsKey(dicKey)) rangeItems[dicKey] = new List<Dictionary<string, object>>();

                        transforms.ForEach(t => features[t.Key] = t.Value.calculate(features));

                        if (!filters.Any(f => !f.validate(features)))
                        {
                            drops.Where(k => features.ContainsKey(k)).ToList().ForEach(d => features.Remove(d));

                            rangeItems[dicKey].Add(features);
                        }
                    }
                });
            });

            List<string> header = new List<string>() { "target", "class", "symbol" };

            if (rangeItems.Count == 0 || rangeItems[rangeItems.Keys.ToList()[0]].Count == 0) return;

            rangeItems[rangeItems.Keys.ToList()[0]].First().Keys.ToList().ForEach(k => {
                if (!header.Any(h => h == k)) header.Add(k);
            });

            int minCount = rangeItems.Keys.ToList().Select(k => rangeItems[k].Count).Min();
            List<Dictionary<string, object>> items = new List<Dictionary<string, object>>();

            rangeItems.Keys.ToList().ForEach(k => {
                items.AddRange(rangeItems[k].OrderBy(i => PublicMethods.get_random_number(5))
                    .Take(ignoreEqualization ? rangeItems[k].Count : minCount));
            });

            StringBuilder csvData = new StringBuilder();
            csvData.AppendLine(string.Join(",", header));

            items.OrderBy(i => PublicMethods.get_random_number(5)).ToList()
                .ForEach(x => csvData.AppendLine(string.Join(",", header.Select(h => x[h].ToString()))));

            File.WriteAllText(fullOutputFileName, csvData.ToString(), Encoding.UTF8);

            _ProcessedFiles++;
            _ItemProgress = 0;

            _Processing = false;
        }

        public static void create_csv(Dictionary<string, List<StockItem>> data, Dictionary<string, List<StockItem>> marketSymbols,
            List<KeyValuePair<DateTime, double>> marketROC,
            int predictionLength, int slide, int slideStep, double[] ranges, bool ignoreEqualization, 
            string mapName, string fullOutputFileName)
        {
            List<KeyValuePair<double, double>> r = new List<KeyValuePair<double, double>>();

            if (ranges != null && ranges.Length > 1) {
                ranges = ranges.ToList().OrderBy(x => x).ToArray();

                for (int i = 1; i < ranges.Length; ++i)
                    r.Add(new KeyValuePair<double, double>(ranges[i - 1], ranges[i]));
            }

            create_csv(data, marketSymbols, marketROC, 
                predictionLength, slide, slideStep, r, ignoreEqualization, mapName, fullOutputFileName);
        }

        public static void correlation_csv(Dictionary<string, List<StockItem>> data, int period,
             List<string> symbols, double? clusteringThreshold, string fullOutputFileName)
        {
            _Processing = true;

            if (symbols == null) symbols = new List<string>();

            double totalCount = Math.Pow(data.Keys.Count, 2);
            double processedCount = 0;

            _ItemProgress = 0;

            List<string> header = new List<string>() { "symbol" };
            List<Dictionary<string, object>> allData = new List<Dictionary<string, object>>();

            data.Keys.ToList().ForEach(symbol =>
            {
                Dictionary<string, object> features = new Dictionary<string, object>();

                if(symbols.Count == 0 || symbols.Any(s => s == symbol)) header.Add(symbol);

                features["symbol"] = symbol;

                data.Keys.Where(k => symbols.Count == 0 || symbols.Any(s => s == k)).ToList().ForEach(other =>
                {
                    processedCount++;
                    _ItemProgress = processedCount / totalCount;

                    features[other] = (double)(other == symbol ? 1 : compute_correlation_coeff(data[symbol], data[other], period));
                });

                allData.Add(features);
            });

            //Generate Clusters
            string listFolder = fullOutputFileName.Substring(0, fullOutputFileName.LastIndexOf("\\")) + "\\clusters";
            generate_clusters(listFolder, allData, clusteringThreshold, symbols);
            //end of Generate Clusters

            StringBuilder csvData = new StringBuilder();
            csvData.AppendLine(string.Join(",", header));

            allData.ForEach(x => csvData.AppendLine(string.Join(",", header.Select(h => x[h].ToString()))));

            File.WriteAllText(fullOutputFileName, csvData.ToString(), Encoding.UTF8);

            _ProcessedFiles++;
            _ItemProgress = 0;

            _Processing = false;
        }

        private static void generate_clusters(string folderPath, List<Dictionary<string, object>> allData,
            double? clusteringThreshold, List<string> symbols)
        {
            if (!clusteringThreshold.HasValue || clusteringThreshold.Value < 0.1) clusteringThreshold = 0.4;

            try { if (Directory.Exists(folderPath)) Directory.Delete(folderPath, true); }
            catch { }

            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            List<List<string>> clusters = new List<List<string>>();
            
            if (symbols.Count > 0)
            {
                clusters = symbols.Select(s =>
                    allData.Where(d => (double)d[s] >= clusteringThreshold).Select(d => (string)d["symbol"]).ToList()).ToList();
            }
            else
            {
                Dictionary<string, List<string>> graph = new Dictionary<string, List<string>>();

                allData.ForEach(d =>
                {
                    graph[(string)d["symbol"]] = d.Keys.ToList()
                        .Where(k => d[k].GetType() == typeof(double) && (double)d[k] >= clusteringThreshold).Select(k => k).ToList();
                });

                while (true)
                {
                    string key = graph.Keys.ToList().Where(k => !clusters.Any(c => c.Any(s => s == k)))
                        .OrderByDescending(o => graph[o].Count).FirstOrDefault();

                    if (string.IsNullOrEmpty(key)) break;

                    List<string> newCluster = new List<string>() { key };

                    while (true)
                    {
                        string newKey = graph[key]
                            .Where(k => graph.ContainsKey(k) && !newCluster.Any(n => n == k || !graph[k].Any(a => a == n)))
                            .Select(k =>
                            {
                                List<string> lst = graph[k].Where(a => a != k && graph[key].Any(b => b == a)).ToList();
                                return new KeyValuePair<string, List<string>>(k, lst);
                            })
                            .Where(u => u.Value.Count > 0)
                            .OrderByDescending(o => o.Value.Count)
                            .Select(u => u.Key).FirstOrDefault();

                        if (string.IsNullOrEmpty(newKey)) break;

                        newCluster.Add(newKey);
                    }

                    clusters.Add(newCluster);
                }
            }

            List<List<string>> processed = new List<List<string>>();

            clusters.OrderByDescending(c => c.Count).ToList()
                .ForEach(lst =>
                {
                    if (symbols.Count == 0)
                    {
                        lst = lst.Where(u => !processed.Any(p => p.Any(x => x == u))).ToList();

                        if (lst.Count < 10) return;

                        allData
                            .Where(row =>
                            {
                                string smbl = (string)row["symbol"];
                                return !lst.Any(a => a == smbl) && !processed.Any(p => p.Any(x => x == smbl));
                            })
                            .Select(row =>
                            {
                                double avg = lst.Select(itm => (double)row[itm]).Average();
                                return new KeyValuePair<Dictionary<string, object>, double>(row, avg);
                            })
                            .Where(pr => pr.Value >= clusteringThreshold)
                            .OrderByDescending(pr => pr.Value)
                            .ToList()
                            .ForEach(pr =>
                            {
                                double newAvg = lst.Select(itm => (double)pr.Key[itm]).Average();
                                if (newAvg > clusteringThreshold) lst.Add((string)pr.Key["symbol"]);
                            });
                    }

                    //double val = symbols.Count > 0 || processed.Count == 0 ? 0 :
                    //    (double)lst.Where(l => processed.Any(p => p.Any(x => x == l))).Count() / lst.Count;
                    //if (lst.Count < 10 || val >= 0.5) return;

                    processed.Add(lst);

                    File.WriteAllText(folderPath + "\\cluster_" + lst.Count.ToString() + "_" +
                            PublicMethods.get_random_number() + ".txt", string.Join(",", lst), Encoding.UTF8);
                });
        }

        public static List<DateTime> get_active_dates(Dictionary<string, List<StockItem>> dic)
        {
            List<DateTime> ret = new List<DateTime>();
            dic.Keys.ToList().ForEach(k => ret.AddRange(dic[k].Select(u => u.Date)));
            return ret.Distinct().OrderBy(d => d).ToList();
        }

        public static int get_days_diff(List<DateTime> activeDates, DateTime first, DateTime second)
        {
            int firstIndex = activeDates.FindIndex(u => u == first);
            int secondIndex = activeDates.FindIndex(u => u == second);

            return firstIndex < 0 || secondIndex < 0 ? 0 : Math.Abs(firstIndex - secondIndex);
        }

        public static Dictionary<string, List<StockItem>> fill_empty_dates(Dictionary<string, List<StockItem>> dic)
        {
            List<DateTime> dates = get_active_dates(dic);

            dic.Keys.ToList().ForEach(k =>
            {
                int datesInd = 0, itemInd = 0;
                dic[k] = dic[k].OrderBy(u => u.Date).ToList();
                
                while (datesInd < dates.Count && itemInd < dic[k].Count)
                {
                    DateTime? itemDate = null;
                    if (itemInd < dic[k].Count) itemDate = dic[k][itemInd].Date;

                    if (!itemDate.HasValue)
                    {
                        if(dic[k].Count > 0) dic[k].Last().IdleDays += 1;
                        datesInd++;
                    }
                    else if (dates[datesInd] == itemDate.Value)
                    {
                        itemInd++;
                        datesInd++;
                    }
                    else if (dates[datesInd] < itemDate.Value)
                    {
                        if(itemInd > 0) dic[k][itemInd - 1].IdleDays += 1;
                        datesInd++;
                    }
                }
            });

            return dic;
        }

        public static int max_consecutive_empty_days(List<StockItem> items) {
            return items.Count == 0 ? 0 : items.Select(u => u.IdleDays).Max();
        }

        public static double compute_correlation_coeff(List<double> first, List<double> second, 
            int periodLength = 0, int shiftSecondToTheLeft = 0)
        {
            if (shiftSecondToTheLeft != 0 && second.Count > Math.Abs(shiftSecondToTheLeft))
            {
                second = shiftSecondToTheLeft > 0 ?
                    second.GetRange(shiftSecondToTheLeft, second.Count - shiftSecondToTheLeft) :
                    second.GetRange(0, second.Count + shiftSecondToTheLeft);
            }

            if (periodLength <= 0) periodLength = Math.Min(first.Count, second.Count);
            periodLength = Math.Min(periodLength, Math.Min(first.Count, second.Count));

            first = first.GetRange(first.Count - periodLength, periodLength);
            second = second.GetRange(second.Count - periodLength, periodLength);

            if (first.Count != second.Count || first.Count < 2) return 0;

            double avg1 = first.Average();
            double avg2 = second.Average();

            double sum1 = first.Zip(second, (x1, y1) => (x1 - avg1) * (y1 - avg2)).Sum();

            double sumSqr1 = first.Sum(x => Math.Pow((x - avg1), 2.0));
            double sumSqr2 = second.Sum(y => Math.Pow((y - avg2), 2.0));

            return sum1 / Math.Sqrt(sumSqr1 * sumSqr2);
        }

        public static double compute_correlation_coeff(List<StockItem> first, List<StockItem> second, 
            int periodLength = 0, int shiftSecondToTheLeft = 0)
        {
            List<double> firstValues = new List<double>();
            List<double> secondValues = new List<double>();

            int fInd = first.Count - 1, sInd = second.Count - 1;

            while (fInd >= 0 && sInd >= 0 && firstValues.Count < (periodLength + shiftSecondToTheLeft))
            {
                if (first[fInd].Date == second[sInd].Date)
                {
                    firstValues.Add(first[fInd].ChangeRate);
                    secondValues.Add(second[sInd].ChangeRate);

                    fInd--;
                    sInd--;
                }
                else if (first[fInd].Date > second[sInd].Date) fInd--;
                else sInd--;
            }

            return compute_correlation_coeff(firstValues, secondValues, periodLength, shiftSecondToTheLeft);
        }

        public static double moving_average_value(double first, double second)
        {
            try
            {
                return first == 0 ? 0 : (second / first) - 1;
            }
            catch { return 0; }
        }

        public static double moving_average_value(List<double?> first, List<double?> second, int offset = 0)
        {
            try
            {
                int firstIndex = first.Count - offset - 1;
                int secondIxdex = second.Count - offset - 1;

                return firstIndex < 0 || secondIxdex < 0 ? 0 : 
                    moving_average_value(first[firstIndex].Value, second[secondIxdex].Value);
            }
            catch { return 0; }
        }

        private static int get_option_value(string name, Dictionary<string, object> refOptions, Dictionary<string, object> partOptions)
        {
            int? refValue = refOptions == null || !refOptions.ContainsKey(name) ? 0 : PublicMethods.parse_int(refOptions[name].ToString());
            int? partValue = partOptions == null || !partOptions.ContainsKey(name) ? 0 : PublicMethods.parse_int(partOptions[name].ToString());

            return partValue.HasValue && partValue.Value > 0 ? partValue.Value : 
                (refValue.HasValue && refValue.Value > 0 ? refValue.Value : 0);
        }

        private static CalculatorOptions get_parameters(Dictionary<string, object> refOptions, Dictionary<string, object> partOptions)
        {
            bool? trend = !partOptions.ContainsKey("trend") ? false :
                PublicMethods.parse_bool(partOptions["trend"].ToString());

            int trendPeriod = get_option_value("trend_period", null, partOptions);

            return new CalculatorOptions()
            {
                Period = trendPeriod > 0 ? trendPeriod : get_option_value("period", refOptions, partOptions),
                PredictionPeriod = get_option_value("prediction_period", refOptions, partOptions),
                RaminTrend = trendPeriod > 0 || (trend.HasValue && trend.Value)
            };
        }

        public static Dictionary<string, object> calculate_indicators(string symbol, 
            List<StockItem> items, Dictionary<string, object> map, List<double> marketROC, 
            Dictionary<string, List<StockItem>> marketSymbols)
        {
            try
            {
                Dictionary<string, object> ret = new Dictionary<string, object>();

                if (!map.ContainsKey("indicators") || map["indicators"].GetType() != typeof(Dictionary<string, object>))
                    return new Dictionary<string, object>();

                Dictionary<string, object> indicators = (Dictionary<string, object>)map["indicators"];

                indicators.Keys.ToList().ForEach(name => {
                    List<Dictionary<string, object>> opArray = new List<Dictionary<string, object>>();

                    if (indicators[name].GetType() == typeof(ArrayList))
                        opArray = ((ArrayList)indicators[name]).ToArray()
                            .Where(u => u.GetType() == typeof(Dictionary<string, object>))
                            .Select(x => (Dictionary<string, object>)x).ToList();
                    else if (indicators[name].GetType() == typeof(Dictionary<string, object>))
                        opArray.Add((Dictionary<string, object>)indicators[name]);

                    if (opArray.Count == 0) opArray.Add(new Dictionary<string, object>());

                    opArray.Select(o => get_parameters(map, o)).ToList().ForEach(options => {
                        switch (name.ToLower())
                        {
                            case "market_roc": //Market Rate-of-Change
                                StockCalculator.market_roc(ret, marketROC, options.PredictionPeriod, options.RaminTrend, options.Period);
                                return;
                            case "industry_roc":
                            case "price_roc":
                            case "total_roc":
                                string colName = name.Substring(0, name.IndexOf('_')).ToLower();
                                if (marketSymbols.ContainsKey(colName))
                                {
                                    StockCalculator.market_index_roc(ret, colName, marketSymbols[colName],
                                        options.PredictionPeriod, options.RaminTrend, options.Period);
                                }
                                return;
                            case "roc": //Rate-of-Change
                                StockCalculator.roc(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                            case "vroc": //Volume Rate-of-Change
                                StockCalculator.vroc(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                            case "bollinger_band": //Bollinger Band
                                StockCalculator.bollinger_band(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                            case "sma": //Simple Moving Average
                                StockCalculator.sma(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                            case "wma": //Weighted Moving Average
                                StockCalculator.wma(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                            case "ema": //Exponential Moving Average
                                StockCalculator.ema(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                            case "dema": //Double Exponential Moving Average
                                StockCalculator.dema(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                            case "zlema": //Zero Lag Exponential Moving Average
                                StockCalculator.zlema(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                            case "macd": //Moving Average Convergence Divergence
                                StockCalculator.macd(ret, items, options.PredictionPeriod, options.RaminTrend, options.Period);
                                return;
                            case "adl": //Accumulation Distribution Line
                                StockCalculator.adl(ret, items, options.PredictionPeriod);
                                return;
                            case "momentum": //Momentum
                                StockCalculator.momentum(ret, items, options.PredictionPeriod);
                                return;
                            case "obv": //On-Balance Volume
                                StockCalculator.obv(ret, items, options.PredictionPeriod);
                                return;
                            case "pvt": //Price Volume Trend
                                StockCalculator.pvt(ret, items, options.PredictionPeriod);
                                return;
                            case "sar": //Stop and Reverse
                                StockCalculator.sar(ret, items, options.PredictionPeriod);
                                return;
                            case "ichimoku": //Ichimoku
                                StockCalculator.ichimoku(ret, items, options.PredictionPeriod);
                                return;
                            case "envelopes": //Moving Average Envelopes
                                StockCalculator.envelopes(ret, items, options.Period, options.PredictionPeriod);
                                return;
                            case "aroon": //Aroon
                                StockCalculator.aroon(ret, items, options.Period, options.PredictionPeriod);
                                return;
                            case "adx": //Average Directional Index
                                StockCalculator.adx(ret, items, options.Period, options.PredictionPeriod);
                                return;
                            case "cci": //Commodity Channel Index
                                StockCalculator.cci(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                            case "cmf": //Chaikin Money Flow
                                StockCalculator.cmf(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                            case "cmo": //Chande Momentum Oscillator
                                StockCalculator.cmo(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                            case "dpo": //Chande Momentum Oscillator
                                StockCalculator.dpo(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                            case "rsi": //Relative Strength Index
                                StockCalculator.rsi(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                            case "trix": //Triple Exponential Average
                                StockCalculator.trix(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                            case "wpr": //Williams Percent Range
                                StockCalculator.wpr(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                            case "atr": //Average True Range
                                StockCalculator.atr(ret, items, options.Period, options.PredictionPeriod, options.RaminTrend);
                                return;
                        }
                    });
                });

                return ret;
            }
            catch (Exception ex)
            {
                string strEx = ex.ToString();
                return null;
            }
        }
    }
}
