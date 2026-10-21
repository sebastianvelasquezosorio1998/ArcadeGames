using UnityEngine;

namespace HyperArcade
{
    /// <summary>Suelta monedas repartiendo el valor en varias piezas (máx. 6) para que se vea generoso.</summary>
    public static class CoinDropper
    {
        private const int MaxPieces = 6;

        public static void Drop(Vector2 position, int totalValue)
        {
            if (totalValue <= 0) return;
            var prefab = GameConfig.Instance != null ? GameConfig.Instance.coinPrefab : null;
            if (prefab == null)
            {
                // Sin prefab: se suman directamente.
                if (MatchController.Instance != null) MatchController.Instance.AddMatchCoins(totalValue);
                return;
            }

            int pieces = Mathf.Clamp(totalValue, 1, MaxPieces);
            int baseValue = totalValue / pieces;
            int remainder = totalValue % pieces;
            for (int i = 0; i < pieces; i++)
            {
                int value = baseValue + (i < remainder ? 1 : 0);
                var coin = PoolManager.Spawn(prefab, position, Quaternion.identity);
                coin.Init(value, new Vector2(Random.Range(-3f, 3f), Random.Range(5f, 9f)));
            }
        }
    }
}
