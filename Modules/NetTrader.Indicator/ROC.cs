using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTrader.Indicator
{
    /// <summary>
    /// Rate of Change (ROC)
    /// </summary>
    public class ROC : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<Ohlc> OhlcList { get; set; }
        protected int Period { get; set; }

        public ROC(List<Ohlc> ohlcList, int period)
        {
            this.Load(ohlcList);
            this.Period = period;
        }

        /// <summary>
        /// ROC = [(Close - Close n periods ago) / (Close n periods ago)] * 100
        /// </summary>
        /// <see cref="http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:rate_of_change_roc_and_momentum"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            SingleDoubleSerie rocSerie = new SingleDoubleSerie();
                
            for (int i = 0; i < OhlcList.Count; i++)
            {   
                if (i >= this.Period)
                {
                    double close = OhlcList[i].Close;
                    double previousClose = OhlcList[i - this.Period].Close;

                    double value = previousClose == 0 ? 0 : ((close / previousClose) - 1) * 100;

                    rocSerie.Values.Add(value);
                }
                else
                {
                    rocSerie.Values.Add(null);
                }
            }

            return rocSerie;
        }

        public SingleDoubleSerie CalculateRamin()
        {
            SingleDoubleSerie rocSerie = new SingleDoubleSerie();

            for (int i = 0; i < OhlcList.Count; i++)
            {
                if (i >= this.Period)
                {
                    double close = OhlcList[i].Close;

                    //this line adjustes changes of properties افزایش سرمایه
                    double previousClose = OhlcList[i].Previous; // OhlcList[i - this.Period].Close;

                    double value = previousClose == 0 ? 0 : ((close / previousClose) - 1) * 100;

                    rocSerie.Values.Add(value);
                }
                else
                {
                    rocSerie.Values.Add(null);
                }
            }

            return rocSerie;
        }
    }
}
