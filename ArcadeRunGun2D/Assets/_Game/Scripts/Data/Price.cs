using System;

namespace HyperArcade
{
    [Serializable]
    public struct Price
    {
        public CurrencyType currency;
        public int amount;

        public Price(CurrencyType currency, int amount)
        {
            this.currency = currency;
            this.amount = amount;
        }

        public bool IsFree => amount <= 0;
    }
}
