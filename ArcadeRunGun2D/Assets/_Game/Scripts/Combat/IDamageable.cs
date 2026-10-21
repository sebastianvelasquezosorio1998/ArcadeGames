using UnityEngine;

namespace HyperArcade
{
    public struct DamageInfo
    {
        public float amount;
        public Team sourceTeam;
        public Vector2 hitPoint;
        public Vector2 direction;

        public DamageInfo(float amount, Team sourceTeam, Vector2 hitPoint, Vector2 direction)
        {
            this.amount = amount;
            this.sourceTeam = sourceTeam;
            this.hitPoint = hitPoint;
            this.direction = direction;
        }
    }

    /// <summary>Cualquier cosa que puede recibir daño (jugador, enemigos, objetos destructibles).</summary>
    public interface IDamageable
    {
        Team Team { get; }
        bool IsAlive { get; }
        void TakeDamage(DamageInfo info);
    }
}
