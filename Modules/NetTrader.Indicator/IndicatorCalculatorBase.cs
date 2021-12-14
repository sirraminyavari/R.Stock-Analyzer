using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NetTrader.Indicator
{
    public abstract class IndicatorCalculatorBase<T>
    {
        protected abstract List<Ohlc> OhlcList { get; set; }

        public virtual void Load(List<Ohlc> ohlcList)
        {
            this.OhlcList = ohlcList;
        }

        public abstract T Calculate();
    }
}
