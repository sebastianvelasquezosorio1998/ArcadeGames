using System.Collections.Generic;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>Lista de enemigos vivos: auto-apuntado, conversor en monedas, límite de enemigos.</summary>
    public static class EnemyRegistry
    {
        private static readonly List<EnemyController> Active = new List<EnemyController>();

        public static IReadOnlyList<EnemyController> All => Active;
        public static int Count => Active.Count;

        public static void Register(EnemyController e)
        {
            if (!Active.Contains(e)) Active.Add(e);
        }

        public static void Unregister(EnemyController e) => Active.Remove(e);

        public static EnemyController FindNearest(Vector2 from, float range, Vector2 direction, float maxAngle)
        {
            EnemyController best = null;
            float bestDist = range * range;
            foreach (var e in Active)
            {
                if (e == null || !e.IsAlive) continue;
                Vector2 to = (Vector2)e.AimPoint - from;
                float d = to.sqrMagnitude;
                if (d > bestDist) continue;
                if (direction != Vector2.zero && Vector2.Angle(direction, to) > maxAngle) continue;
                bestDist = d;
                best = e;
            }
            return best;
        }

        /// <summary>Copia segura para iterar mientras se eliminan enemigos.</summary>
        public static List<EnemyController> Snapshot() => new List<EnemyController>(Active);

        public static void Clear() => Active.Clear();
    }
}
