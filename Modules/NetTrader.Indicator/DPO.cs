using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTrader.Indicator
{
    /// <summary>
    /// Detrended Price Oscillator (DPO)
    /// </summary>
    public class DPO : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<Ohlc> OhlcList { get; set; }
        protected int Period = 10;

        public DPO()
        { 
        
        }

        public DPO(List<Ohlc> ohlcList, int period)
        {
            this.Load(ohlcList);
            this.Period = period;
        }

        /// <summary>
        /// Price {X/2 + 1} periods ago less the X-period simple moving average.
        /// X refers to the number of periods used to calculate the Detrended Price 
        /// Oscillator. A 20-day DPO would use a 20-day SMA that is displaced by 11 
        /// periods {20/2 + 1 = 11}. This displacement shifts the 20-day SMA 11 days 
        /// to the left, which actually puts it in the middle of the look-back 
        /// period. The value of the 20-day SMA is then subtracted from the price 
        /// in the middle of this look-back period. In short, DPO(20) equals price
        /// 11 days ago less the 20-day SMA.  
        /// </summary>
        /// <see cref="http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:detrended_price_osci"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            SingleDoubleSerie dpoSerie = new SingleDoubleSerie();

            SMA sma = new SMA(OhlcList, Period);
            List<double?> smaList = sma.Calculate().Values;
            
            for (int i = 0; i < OhlcList.Count; i++)
            {
                int pastInd = i - ((Period / 2) + 1);

                if(pastInd >= 0 && smaList[pastInd].HasValue)
                    dpoSerie.Values.Add(OhlcList[i].Close - smaList[pastInd].Value);
                else dpoSerie.Values.Add(null);
            }

            return dpoSerie;
        }
    }
}
