using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTrader.Indicator
{
    /// <summary>
    /// Volume Rate of Change (VROC)
    /// </summary>
    public class VROC : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<Ohlc> OhlcList { get; set; }
        protected int Period { get; set; }

        public VROC(List<Ohlc> ohlcList, int period)
        {
            this.Load(ohlcList);
            this.Period = period;
        }

        /// <summary>
        /// VROC = ((VOLUME (i) - VOLUME (i - n)) / VOLUME (i - n)) * 100
        /// </summary>
        /// <see cref="http://ta.mql4.com/indicators/volumes/rate_of_change"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            SingleDoubleSerie rocSerie = new SingleDoubleSerie();

            for (int i = 0; i < OhlcList.Count; i++)
            {
                if (i >= this.Period)
                {
                    double denominator = OhlcList[i - this.Period].Volume;
                    rocSerie.Values.Add(denominator == 0 ? 0 : 
                        ((OhlcList[i].Volume - denominator) / denominator) * 100);
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
