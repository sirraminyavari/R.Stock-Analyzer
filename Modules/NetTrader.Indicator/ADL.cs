using NetTrader.Indicator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTrader.Indicator
{
    /// <summary>
    /// Accumulation / Distribution Line 
    /// </summary>
    public class ADL : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<Ohlc> OhlcList { get; set; }

        public ADL(List<Ohlc> ohlcList)
        {
            this.Load(ohlcList);
        }

        /// <summary>
        /// Acc/Dist = ((Close – Low) – (High – Close)) / (High – Low) * Period's volume
        /// </summary>
        /// <see cref="http://www.investopedia.com/terms/a/accumulationdistribution.asp"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            SingleDoubleSerie adlSerie = new SingleDoubleSerie();

            for (int i = 0; i < OhlcList.Count; i++) {
                Ohlc ohlc = OhlcList[i];

                double denominator = ohlc.High - ohlc.Low;
                double value = denominator == 0 ? 0 :
                    (((ohlc.Close - ohlc.Low) - (ohlc.High - ohlc.Close)) / denominator) * ohlc.Volume;

                adlSerie.Values.Add(value + (i > 0 ? adlSerie.Values[i - 1].Value : 0));
            }

            return adlSerie;
        }
    }
}
