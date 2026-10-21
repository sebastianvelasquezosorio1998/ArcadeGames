using UnityEngine;

namespace HyperArcade
{
    /// <summary>Trigger bajo el escenario: el jugador que cae muere; enemigos/monedas vuelven al pool.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerHealth>();
            if (player != null) { player.Kill(); return; }

            var enemy = other.GetComponentInParent<EnemyController>();
            if (enemy != null) { enemy.Despawn(); return; }

            var coin = other.GetComponentInParent<Coin>();
            if (coin != null) coin.Collect();
        }
    }
}
