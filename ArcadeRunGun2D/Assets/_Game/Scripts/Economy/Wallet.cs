using System;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Economía dual: Monedas (moneda suave, se gana jugando) y Diamantes (moneda premium).
    /// Toda modificación pasa por aquí y se guarda inmediatamente.
    /// </summary>
    public static class Wallet
    {
        /// <summary>(tipo, nuevo saldo, delta)</summary>
        public static event Action<CurrencyType, int, int> Changed;

        public static int Coins => Get(CurrencyType.Coins);
        public static int Diamonds => Get(CurrencyType.Diamonds);

        public static int Get(CurrencyType type)
        {
            var d = SaveSystem.Data;
            if (d == null) return 0;
            return type == CurrencyType.Coins ? d.coins : d.diamonds;
        }

        public static bool CanAfford(Price price) => price.IsFree || Get(price.currency) >= price.amount;

        /// <param name="source">Origen para analítica (ej. "match", "ad_menu", "level_reward").</param>
        public static void Add(CurrencyType type, int amount, string source)
        {
            if (amount <= 0 || SaveSystem.Data == null) return;
            Set(type, Get(type) + amount, amount);
            Debug.Log($"[Wallet] +{amount} {type} ({source})");
        }

        public static bool TrySpend(Price price, string reason)
        {
            if (price.IsFree) return true;
            if (!CanAfford(price)) return false;
            Set(price.currency, Get(price.currency) - price.amount, -price.amount);
            Debug.Log($"[Wallet] -{price.amount} {price.currency} ({reason})");
            return true;
        }

        private static void Set(CurrencyType type, int value, int delta)
        {
            var d = SaveSystem.Data;
            value = Mathf.Max(0, value);
            if (type == CurrencyType.Coins) d.coins = value; else d.diamonds = value;
            SaveSystem.Save();
            Changed?.Invoke(type, value, delta);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Changed = null;
    }
}
