using NetTrader.Indicator;
using RaaiVan.Modules.GlobalUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaaiVan.Modules.Jobs
{
    public class StockCalculator
    {
        private static double get_double_value(double? value, double? min = null, double? max = null)
        {
            min = max = null; //////////////////////////////////////

            double ret = !value.HasValue || double.IsNaN(value.Value) ? 0 : value.Value;
            if (min.HasValue && ret < min.Value) ret = min.Value;
            if (max.HasValue && ret > max.Value) ret = max.Value;
            return ret;
        }

        private static double get_volume_average(List<StockItem> ohlcList, int period) {
            return period == 0 ? 0 : ohlcList.GetRange(ohlcList.Count - period, period).Select(u => u.Volume).Average();
        }

        public static void ramin_trend(string entryName, Dictionary<string, object> dic, List<double> values, int period, 
            bool isProcessed = false)
        {
            //values are ascending based on time

            List<double> rate = isProcessed ? values : new List<double>();

            if (!isProcessed)
            {
                for (int i = values.Count - 1; i > 0; --i)
                {
                    double first = get_double_value(values[i]), second = get_double_value(values[i - 1]);
                    rate.Add(second == 0 ? 0 : (first / second) - 1);
                }
            }

            if (period > rate.Count) Enumerable.Range(0, period - rate.Count).ToList().ForEach(x => rate.Add(0));

            List<double> selected = rate.GetRange(0, period);

            int returns = 0;

            for (int i = 1; i < selected.Count; ++i)
                if ((selected[i] * selected[i - 1]) < 0) returns++;

            double negative = selected.Where(u => u < 0).Select(x => (1 / (x + 1)) - 1).Sum();
            double positive = selected.Where(u => u > 0).Sum();

            dic[entryName + "_" + period.ToString() + "_negative"] = negative;
            dic[entryName + "_" + period.ToString() + "_positive"] = positive;
            dic[entryName + "_" + period.ToString() + "_level"] = 1 -
                (Math.Max(negative, positive) == 0 ? 0 : Math.Min(negative, positive) / Math.Max(negative, positive));
            dic[entryName + "_" + period.ToString() + "_returns"] = returns;
        }

        private static void ramin_trend_moving_average(string entryName, Dictionary<string, object> dic, 
            List<double?> shortValues, List<double?> longValues, int period = 0) {
            List<double> values = new List<double>();

            Enumerable.Range(1, period + 5).ToList().ForEach(i =>
            {
                values.Add(get_double_value(longValues[longValues.Count - i]) -
                    get_double_value(shortValues[shortValues.Count - i]));
            });

            values.Reverse();

            ramin_trend(entryName, dic, values, period);
        }

        public static void moving_average(string name, Dictionary<string, object> dic,
            List<double?> values, int period, int predictionPeriod, bool raminTrendMode)
        {
            if (raminTrendMode)
                ramin_trend(name + "_trend_" + period.ToString(), dic, values.Select(v => get_double_value(v)).ToList(), period);
            else
            {
                Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
                {
                    int index = values.Count - i;

                    double first = index > 0 ? get_double_value(values[index - 1]) : 0;
                    double second = index >= 0 ? get_double_value(values[index]) : 0;

                    dic[name + "_signal_" + period.ToString() + "_" + i.ToString()] = first == 0 ? 0 : (second / first) - 1;
                });
            }
        }

        public static void sma(Dictionary<string, object> dic, List<StockItem> ohlcList, 
            int period, int predictionPeriod, bool raminTrendMode)
        {
            moving_average("sma", dic,
                new SMA(Ohlc.fromStockItems(ohlcList), period).Calculate().Values, 
                period, predictionPeriod, raminTrendMode);
        }

        public static void wma(Dictionary<string, object> dic, List<StockItem> ohlcList,
            int period, int predictionPeriod, bool raminTrendMode)
        {
            moving_average("wma", dic,
                new WMA(Ohlc.fromStockItems(ohlcList), period).Calculate().Values,
                period, predictionPeriod, raminTrendMode);
        }

        public static void ema(Dictionary<string, object> dic, List<StockItem> ohlcList,
            int period, int predictionPeriod, bool raminTrendMode)
        {
            moving_average("ema", dic,
                new EMA(Ohlc.fromStockItems(ohlcList), period).Calculate().Values,
                period, predictionPeriod, raminTrendMode);
        }

        public static void dema(Dictionary<string, object> dic, List<StockItem> ohlcList,
            int period, int predictionPeriod, bool raminTrendMode)
        {
            moving_average("dema", dic,
                new DEMA(Ohlc.fromStockItems(ohlcList), period).Calculate().Values,
                period, predictionPeriod, raminTrendMode);
        }

        public static void zlema(Dictionary<string, object> dic, List<StockItem> ohlcList,
            int period, int predictionPeriod, bool raminTrendMode)
        {
            moving_average("zlema", dic,
                new ZLEMA(Ohlc.fromStockItems(ohlcList), period).Calculate().Values,
                period, predictionPeriod, raminTrendMode);
        }

        public static void macd(Dictionary<string, object> dic, List<StockItem> ohlcList, int predictionPeriod, 
            bool raminTrendMode, int raminTrendPeriod)
        {
            MACDSerie macd = new MACD(Ohlc.fromStockItems(ohlcList)).Calculate();

            if (raminTrendMode && raminTrendPeriod > 0)
                ramin_trend_moving_average("macd_trend", dic, macd.MACDLine, macd.Signal, raminTrendPeriod);
            else
            {
                Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
                {
                    dic["macd_" + i.ToString()] =
                        get_double_value(StockUtil.moving_average_value(macd.MACDLine, macd.Signal, i - 1), min: -20, max: 20);
                });
            }
        }

        public static void envelopes(Dictionary<string, object> dic, List<StockItem> ohlcList, int period, int predictionPeriod)
        {
            EnvelopeSerie envelopes = new Envelope(Ohlc.fromStockItems(ohlcList), period).Calculate();

            Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
            {
                int ind = ohlcList.Count - 1;
                int uInd = envelopes.Upper.Count - i;

                double price = ind < 0 ? 0 : get_double_value(ohlcList[ind].ClosingPrice);
                double upper = uInd < 0 ? 0 : get_double_value(envelopes.Upper[uInd]);

                dic["envelopes_up_" + i.ToString()] = get_double_value(StockUtil.moving_average_value(upper, price));
            });

            Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
            {
                int ind = ohlcList.Count - 1;
                int lInd = envelopes.Lower.Count - i;

                double price = ind < 0 ? 0 : get_double_value(ohlcList[ind].ClosingPrice);
                double lower = lInd < 0 ? 0 : get_double_value(envelopes.Lower[lInd]);

                dic["envelopes_down_" + i.ToString()] = get_double_value(StockUtil.moving_average_value(lower, price));
            });
        }

        public static void adl(Dictionary<string, object> dic, List<StockItem> ohlcList, int predictionPeriod)
        {
            SingleDoubleSerie adl = new ADL(Ohlc.fromStockItems(ohlcList)).Calculate();

            double avg = get_volume_average(ohlcList, predictionPeriod);

            Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
            {
                int ind = adl.Values.Count - i;
                dic["adl_" + i.ToString()] = ind < 0 || avg == 0 ? 0 : get_double_value(adl.Values[adl.Values.Count - i]) / avg;
            });
        }

        public static void adx(Dictionary<string, object> dic, List<StockItem> ohlcList, int period, int predictionPeriod)
        {
            ADXSerie adx = new ADX(Ohlc.fromStockItems(ohlcList), period).Calculate();

            Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
            {
                int ind = adx.ADX.Count - i;
                if (i > 0) dic["adx_" + period.ToString() + "_" + i.ToString()] = get_double_value(adx.ADX[ind]);
            });
        }

        public static void aroon(Dictionary<string, object> dic, List<StockItem> ohlcList, int period, int predictionPeriod)
        {
            AroonSerie aroon = new Aroon(Ohlc.fromStockItems(ohlcList), period).Calculate();

            Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
            {
                int ind = aroon.Up.Count - i;
                dic["aroon_" + period.ToString() + "_up_" + i.ToString()] = ind < 0 ? 0 : get_double_value(aroon.Up[ind]);
            });

            Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
            {
                int ind = aroon.Down.Count - i;
                dic["aroon_" + period.ToString() + "_down_" + i.ToString()] = ind < 0 ? 0 : get_double_value(aroon.Down[ind]);
            });
        }

        public static void bollinger_band(Dictionary<string, object> dic, List<StockItem> ohlcList, 
            int period, int predictionPeriod, bool raminTrendMode)
        {
            BollingerBandSerie bollingerBand = new BollingerBand(Ohlc.fromStockItems(ohlcList), period).Calculate();

            if (raminTrendMode && period > 0)
            {
                ramin_trend("bollinger_" + period.ToString() + "_trend",
                    dic, bollingerBand.BandWidth.Select(u => get_double_value(u)).ToList(), period);
            }
            else
            {
                Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
                {
                    int ind = bollingerBand.BandWidth.Count - i;

                    dic["bollinger_" + period.ToString() + "_bandwidth_" + i.ToString()] = ind < 0 ? 0 :
                        get_double_value(bollingerBand.BandWidth[ind]);
                });

                Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
                {
                    int ind = bollingerBand.BPercent.Count - i;

                    dic["bollinger_" + period.ToString() + "_bpercent_" + i.ToString()] = ind < 0 ? 0 :
                        get_double_value(bollingerBand.BPercent[ind]);
                });
            }
        }

        public static void cci(Dictionary<string, object> dic, List<StockItem> ohlcList,
            int period, int predictionPeriod, bool raminTrendMode)
        {
            SingleDoubleSerie cci = new CCI(Ohlc.fromStockItems(ohlcList), period).Calculate();

            if (raminTrendMode && period > 0)
            {
                ramin_trend("cci_" + period.ToString() + "_trend",
                    dic, cci.Values.Select(u => get_double_value(u)).ToList(), period);
            }
            else
            {
                Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
                {
                    int ind = cci.Values.Count - i;
                    dic["cci_" + period.ToString() + "_" + i.ToString()] = ind < 0 ? 0 : get_double_value(cci.Values[ind]);
                });
            }
        }

        public static void cmf(Dictionary<string, object> dic, List<StockItem> ohlcList,
            int period, int predictionPeriod, bool raminTrendMode)
        {
            SingleDoubleSerie cmf = new CMF(Ohlc.fromStockItems(ohlcList), period).Calculate();

            if (raminTrendMode && period > 0)
            {
                ramin_trend("cmf_" + period.ToString() + "_trend",
                    dic, cmf.Values.Select(u => get_double_value(u)).ToList(), period);
            }
            else
            {
                Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
                {
                    int ind = cmf.Values.Count - i;
                    dic["cmf_" + period.ToString() + "_" + i.ToString()] = ind < 0 ? 0 : get_double_value(cmf.Values[ind]);
                });
            }
        }

        public static void cmo(Dictionary<string, object> dic, List<StockItem> ohlcList,
            int period, int predictionPeriod, bool raminTrendMode)
        {
            SingleDoubleSerie cmo = new CMO(Ohlc.fromStockItems(ohlcList), period).Calculate();

            if (raminTrendMode && period > 0)
            {
                ramin_trend("cmo_" + period.ToString() + "_trend",
                    dic, cmo.Values.Select(u => get_double_value(u)).ToList(), period);
            }
            else
            {
                Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
                {
                    int ind = cmo.Values.Count - i;
                    dic["cmo_" + period.ToString() + "_" + i.ToString()] = ind < 0 ? 0 : get_double_value(cmo.Values[ind]);
                });
            }
        }

        public static void dpo(Dictionary<string, object> dic, List<StockItem> ohlcList,
            int period, int predictionPeriod, bool raminTrendMode)
        {
            SingleDoubleSerie dpo = new DPO(Ohlc.fromStockItems(ohlcList), period).Calculate();

            if (raminTrendMode && period > 0)
            {
                ramin_trend("dpo_" + period.ToString() + "_trend",
                    dic, dpo.Values.Select(u => get_double_value(u)).ToList(), period);
            }
            else
            {
                List<double> values = dpo.Values.GetRange(dpo.Values.Count - predictionPeriod, predictionPeriod)
                    .Select(u => get_double_value(u)).ToList();

                values.Reverse();

                double max = values.Max();
                double min = values.Min();

                Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
                {
                    dic["dpo_" + period.ToString() + "_" + i.ToString()] = max == min ? 0 : (values[i - 1] - min) / (max - min);
                });
            }
        }

        public static void market_roc(Dictionary<string, object> dic, List<double> marketROC, int predictionPeriod,
            bool raminTrendMode, int raminTrendPeriod)
        {
            if (raminTrendMode && raminTrendPeriod > 0)
                ramin_trend("market_roc_trend", dic, marketROC.Select(u => u / 100).ToList(), raminTrendPeriod, isProcessed: true);
            else
            {
                Enumerable.Range(0, predictionPeriod).ToList().ForEach(i =>
                {
                    dic["market_roc_" + (i + 1).ToString()] = i >= marketROC.Count ? 0 : get_double_value(marketROC[i]);
                });
            }
        }

        public static void market_index_roc(Dictionary<string, object> dic, string colName, List<StockItem> data, int predictionPeriod,
            bool raminTrendMode, int raminTrendPeriod)
        {
            if (raminTrendMode && raminTrendPeriod > 0) {
                List<double> values = data.Select(d => get_double_value(d.ClosingPrice)).ToList();
                values.Reverse();
                ramin_trend(colName + "_roc_trend", dic, values, raminTrendPeriod);
            }
            else
            {
                List<Ohlc> lst = Ohlc.fromStockItems(data);
                lst.Reverse();
                SingleDoubleSerie roc = new ROC(lst, 1).Calculate();

                Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
                {
                    int ind = roc.Values.Count - i;
                    dic[colName + "_roc_" + i.ToString()] = ind < 0 ? 0 : get_double_value(roc.Values[ind]);
                });
            }
        }

        public static void roc(Dictionary<string, object> dic, List<StockItem> ohlcList,
            int period, int predictionPeriod, bool raminTrendMode)
        {
            if (raminTrendMode && period > 0)
                ramin_trend("roc_trend", dic, ohlcList.Select(u => get_double_value(u.ClosingPrice)).ToList(), period);
            else
            {
                SingleDoubleSerie roc = new ROC(Ohlc.fromStockItems(ohlcList), period).CalculateRamin();

                Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
                {
                    int ind = roc.Values.Count - i;
                    dic["roc_" + period.ToString() + "_" + i.ToString()] = ind < 0 ? 0 : get_double_value(roc.Values[ind]);
                });
            }
        }

        public static void vroc(Dictionary<string, object> dic, List<StockItem> ohlcList,
            int period, int predictionPeriod, bool raminTrendMode)
        {
            if (raminTrendMode && period > 0)
                ramin_trend("vroc_trend", dic, ohlcList.Select(u => get_double_value(u.Volume)).ToList(), period);
            else
            {
                SingleDoubleSerie vroc = new VROC(Ohlc.fromStockItems(ohlcList), period).Calculate();

                Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
                {
                    int ind = vroc.Values.Count - i;
                    dic["vroc_" + period.ToString() + "_" + i.ToString()] = ind < 0 ? 0 : get_double_value(vroc.Values[ind]);
                });
            }
        }

        public static void rsi(Dictionary<string, object> dic, List<StockItem> ohlcList,
            int period, int predictionPeriod, bool raminTrendMode)
        {
            RSISerie rsi = new RSI(Ohlc.fromStockItems(ohlcList), period).Calculate();

            if (raminTrendMode && period > 0)
                ramin_trend("rsi_" + period.ToString() + "_trend", dic, rsi.RSI.Select(u => get_double_value(u)).ToList(), period);
            else
            {
                Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
                {
                    int ind = rsi.RSI.Count - i;
                    dic["rsi_" + period.ToString() + "_" + i.ToString()] = ind < 0 ? 0 : get_double_value(rsi.RSI[ind]);
                });
            }
        }

        public static void trix(Dictionary<string, object> dic, List<StockItem> ohlcList,
            int period, int predictionPeriod, bool raminTrendMode)
        {
            SingleDoubleSerie trix = new TRIX(Ohlc.fromStockItems(ohlcList), period).Calculate();

            if (raminTrendMode && period > 0)
                ramin_trend("trix_" + period.ToString() + "_trend", dic, trix.Values.Select(u => get_double_value(u)).ToList(), period);
            else
            {
                Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
                {
                    int ind = trix.Values.Count - i;
                    dic["trix_" + period.ToString() + "_" + i.ToString()] = ind < 0 ? 0 : get_double_value(trix.Values[ind]);
                });
            }
        }

        public static void wpr(Dictionary<string, object> dic, List<StockItem> ohlcList,
            int period, int predictionPeriod, bool raminTrendMode)
        {
            SingleDoubleSerie wpr = new WPR(Ohlc.fromStockItems(ohlcList), period).Calculate();

            if (raminTrendMode && period > 0)
                ramin_trend("wpr_" + period.ToString() + "_trend", dic, wpr.Values.Select(u => get_double_value(u)).ToList(), period);
            else
            {
                Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
                {
                    int ind = wpr.Values.Count - i;
                    dic["wpr_" + period.ToString() + "_" + i.ToString()] = ind < 0 ? 0 : get_double_value(wpr.Values[ind]);
                });
            }
        }

        public static void atr(Dictionary<string, object> dic, List<StockItem> ohlcList,
            int period, int predictionPeriod, bool raminTrendMode)
        {
            ATRSerie atr = new ATR(Ohlc.fromStockItems(ohlcList), period).Calculate();

            if (raminTrendMode && period > 0)
                ramin_trend("atr_" + period.ToString() + "_trend", dic, atr.ATR.Select(u => get_double_value(u)).ToList(), period);
            else
            {
                Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
                {
                    int ind = atr.ATR.Count - i;

                    double prev = ind < 1 ? 0 : get_double_value(atr.ATR[ind - 1]);

                    dic["atr_" + period.ToString() + "_" + i.ToString()] = ind < 0 || prev == 0 ? 0 :
                        (get_double_value(atr.ATR[ind]) / prev) - 1;
                });
            }
        }

        public static void momentum(Dictionary<string, object> dic, List<StockItem> ohlcList, int predictionPeriod)
        {
            SingleDoubleSerie momentum = new Momentum(Ohlc.fromStockItems(ohlcList)).Calculate();

            List<double> values = momentum.Values.GetRange(momentum.Values.Count - predictionPeriod, predictionPeriod)
                .Select(u => get_double_value(u)).ToList();

            values.Reverse();

            double max = values.Max();
            double min = values.Min();

            Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
            {
                dic["momentum_" + i.ToString()] = max == min ? 0 : (values[i - 1] - min) / (max - min);
            });
        }

        public static void obv(Dictionary<string, object> dic, List<StockItem> ohlcList, int predictionPeriod)
        {
            SingleDoubleSerie obv = new OBV(Ohlc.fromStockItems(ohlcList)).Calculate();

            double avg = get_volume_average(ohlcList, predictionPeriod);

            Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
            {
                int ind = obv.Values.Count - i;
                dic["obv_" + i.ToString()] = ind < 0 || avg == 0 ? 0 : get_double_value(obv.Values[obv.Values.Count - i]) / avg;
            });
        }

        public static void pvt(Dictionary<string, object> dic, List<StockItem> ohlcList, int predictionPeriod)
        {
            SingleDoubleSerie pvt = new PVT(Ohlc.fromStockItems(ohlcList)).Calculate();

            double avg = get_volume_average(ohlcList, predictionPeriod);

            Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
            {
                int ind = pvt.Values.Count - i;
                dic["pvt_" + i.ToString()] = ind < 0 || avg == 0 ? 0 : get_double_value(pvt.Values[pvt.Values.Count - i]) / avg;
            });
        }

        public static void sar(Dictionary<string, object> dic, List<StockItem> ohlcList, int predictionPeriod)
        {
            SingleDoubleSerie sar = new SAR(Ohlc.fromStockItems(ohlcList)).Calculate();

            List<double> values = sar.Values.GetRange(sar.Values.Count - predictionPeriod, predictionPeriod)
                .Select(u => get_double_value(u)).ToList();

            values.Reverse();

            double max = values.Max();
            double min = values.Min();

            Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
            {
                dic["sar_" + i.ToString()] = max == min ? 0 : (values[i - 1] - min) / (max - min);
            });
        }

        public static void ichimoku(Dictionary<string, object> dic, List<StockItem> ohlcList, int predictionPeriod)
        {
            IchimokuSerie ichimoku = new Ichimoku(Ohlc.fromStockItems(ohlcList)).Calculate();

            Enumerable.Range(1, predictionPeriod).ToList().ForEach(i =>
            {
                int aInd = ichimoku.LeadingSpanA.Count - i;
                int bInd = ichimoku.LeadingSpanB.Count - i;

                double aValue = aInd < 0 ? 0 : get_double_value(ichimoku.LeadingSpanA[aInd]);
                double bValue = bInd < 0 ? 0 : get_double_value(ichimoku.LeadingSpanB[bInd]);

                dic["ichimoku_" + i.ToString()] = aValue == 0 ? 0 : (bValue / aValue) - 1;
            });
        }
    }
}
