using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaaiVan.Modules.GlobalUtilities
{
    public enum StockParam
    {
        Code,
        Date,
        Symbol,
        PreviousPrice,
        OpeningPrice,
        ClosingPrice,
        HighestPrice,
        LowestPrice,
        LastPrice,
        Volume,
        TotalValue,
        TotalTransactions,
        Name,
        LatinName
    }

    public class StockItem
    {
        public string Code;
        public DateTime Date;
        public string Symbol;
        public double PreviousPrice;
        public double OpeningPrice;
        public double ClosingPrice;
        public double HighestPrice;
        public double LowestPrice;
        public double LastPrice;
        public double Volume;
        public double TotalValue;
        public double TotalTransactions;
        public string Name;
        public string LatinName;
        public int IdleDays;

        public double ChangeRate
        {
            get { return Math.Round(PreviousPrice == 1000 ? 0 : (ClosingPrice / PreviousPrice) - 1, 6); }
        }

        public StockItem()
        {
            PreviousPrice = OpeningPrice = ClosingPrice = HighestPrice = LowestPrice =
                LastPrice = Volume = TotalValue = TotalTransactions = 0;

            IdleDays = 0;
        }

        public bool validate()
        {
            return !string.IsNullOrEmpty(Symbol) && PreviousPrice > 0 && OpeningPrice > 0 && ClosingPrice > 0 &&
                HighestPrice > 0 && LowestPrice > 0 && LastPrice > 0 && Volume > 0 && TotalTransactions > 0;
        }

        private static Dictionary<StockParam, string> _Map = null;
        public static Dictionary<StockParam, string> Map
        {
            get
            {
                if (_Map == null)
                {
                    _Map = new Dictionary<StockParam, string>();

                    _Map[StockParam.Code] = "col11";
                    _Map[StockParam.Date] = "dtyyyymmdd";
                    _Map[StockParam.Symbol] = "ticker";
                    _Map[StockParam.PreviousPrice] = "openint3";
                    _Map[StockParam.OpeningPrice] = "open";
                    _Map[StockParam.ClosingPrice] = "close";
                    _Map[StockParam.HighestPrice] = "high";
                    _Map[StockParam.LowestPrice] = "low";
                    _Map[StockParam.LastPrice] = "col15";
                    _Map[StockParam.Volume] = "vol";
                    _Map[StockParam.TotalValue] = "openint";
                    _Map[StockParam.TotalTransactions] = "openint2";
                    _Map[StockParam.Name] = "col13";
                    _Map[StockParam.LatinName] = "col12";
                }

                return _Map;
            }
        }
    }

    public enum AnalysisParam
    {
        DateDiff,
        DayOfWeek,
        ChangeRate,
        HighestByLowest
    }

    public class AnalysisStockItem
    {
        public string Code;
        public string Symbol;
        public DateTime Date;
        public int DateDiff;
        public int DayOfWeek;
        public double ChangeRate;
        public double HighestByLowest;

        public string toCSV(List<AnalysisParam> parameters)
        {
            return string.Join(",", parameters.Select(p =>
            {
                switch (p)
                {
                    case AnalysisParam.DateDiff:
                        return DateDiff.ToString();
                    case AnalysisParam.DayOfWeek:
                        return DayOfWeek.ToString();
                    case AnalysisParam.ChangeRate:
                        return ChangeRate.ToString();
                    case AnalysisParam.HighestByLowest:
                        return HighestByLowest.ToString();
                }

                return string.Empty;
            }));
        }
    }

    public class CalculatorOptions {
        public int Period;
        public int PredictionPeriod;
        public bool RaminTrend;

        public CalculatorOptions() {
            Period = PredictionPeriod = 0;
            RaminTrend = false;
        }
    }

    public enum FilterOperator {
        None,
        Equal,
        GreaterThan,
        GreaterThanOrEqual,
        SmallerThan,
        SmallerThanOrEqual,
        Sum,
        Minus,
        Multiply,
        Divide,
        Average
    }

    public class Filter
    {
        public string Name;
        public FilterOperator Operator;
        public double Value;
        private List<Filter> Sub;

        public Filter(Dictionary<string, object> dic)
        {
            if (dic == null) dic = new Dictionary<string, object>();

            Name = dic.ContainsKey("name") ? dic["name"].ToString() : string.Empty;

            switch (!dic.ContainsKey("operator") ? string.Empty : dic["operator"].ToString()) {
                case "=":
                    Operator = FilterOperator.Equal;
                    break;
                case ">":
                    Operator = FilterOperator.GreaterThan;
                    break;
                case ">=":
                    Operator = FilterOperator.GreaterThanOrEqual;
                    break;
                case "<":
                    Operator = FilterOperator.SmallerThan;
                    break;
                case "<=":
                    Operator = FilterOperator.SmallerThanOrEqual;
                    break;
                case "+":
                    Operator = FilterOperator.Sum;
                    break;
                case "-":
                    Operator = FilterOperator.Minus;
                    break;
                case "*":
                    Operator = FilterOperator.Multiply;
                    break;
                case "/":
                    Operator = FilterOperator.Divide;
                    break;
                case "avg":
                    Operator = FilterOperator.Average;
                    break;
                default:
                    Operator = FilterOperator.None;
                    break;
            }

            double? v = PublicMethods.parse_double(!dic.ContainsKey("value") ? "___" : dic["value"].ToString());
            Value = !v.HasValue ? 0 : v.Value;

            Sub = new List<Filter>();

            if (dic.ContainsKey("sub") && dic["sub"].GetType() == typeof(ArrayList)) {
                ((ArrayList)dic["sub"]).ToArray().Where(u => u.GetType() == typeof(Dictionary<string, object>))
                    .ToList().ForEach(s => Sub.Add(new Filter((Dictionary<string, object>)s)));
            }
        }

        public bool isLogical() {
            List<FilterOperator> lst = new List<FilterOperator>() {
                FilterOperator.Equal, FilterOperator.GreaterThan, FilterOperator.GreaterThanOrEqual, 
                FilterOperator.SmallerThan, FilterOperator.SmallerThanOrEqual
            };

            return lst.Any(u => u == Operator);
        }

        public double calculate(Dictionary<string, object> dic) {
            if (isLogical()) return 0;

            switch (Operator)
            {
                case FilterOperator.Sum:
                    return Sub.Select(u => u.calculate(dic)).Sum();
                case FilterOperator.Minus:
                    return Sub.Count != 2 ? 0 : Sub[0].calculate(dic) - Sub[1].calculate(dic);
                case FilterOperator.Multiply:
                    return Sub.Count != 2 ? 0 : Sub[0].calculate(dic) * Sub[1].calculate(dic);
                case FilterOperator.Divide:
                    double denominator = Sub.Count != 2 ? 0 : Sub[1].calculate(dic);
                    return Sub.Count != 2 || denominator == 0 ? 0 : Sub[0].calculate(dic) / denominator;
                case FilterOperator.Average:
                    return Sub.Select(u => u.calculate(dic)).Average();
                default:
                    double v;

                    if (dic == null || string.IsNullOrEmpty(Name) || !dic.ContainsKey(Name) || 
                        !double.TryParse(dic[Name].ToString(), out v)) return Value;
                    else return v;
            }
        }

        public bool validate(Dictionary<string, object> dic) {
            if (!isLogical()) return false;

            double v;

            if (!dic.ContainsKey(Name) || !double.TryParse(dic[Name].ToString(), out v)) return false;

            double left = v, right = Value;

            if (Sub.Count == 2) {
                left = Sub[0].calculate(dic);
                right = Sub[1].calculate(dic);
            }

            switch (Operator) {
                case FilterOperator.Equal:
                    return left == right;
                case FilterOperator.GreaterThan:
                    return left > right;
                case FilterOperator.GreaterThanOrEqual:
                    return left >= right;
                case FilterOperator.SmallerThan:
                    return left < right;
                case FilterOperator.SmallerThanOrEqual:
                    return left <= right;
                default:
                    return false;
            }
        }
    }
}
