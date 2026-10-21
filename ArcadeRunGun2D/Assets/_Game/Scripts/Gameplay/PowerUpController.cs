using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Aplica los consumibles equipados en la pre-partida:
    ///  - Multiplicador x2: se consume al empezar y dura toda la partida.
    ///  - Escudo y Convertidor: quedan disponibles como botones del HUD (1 uso por partida),
    ///    y la unidad solo se descuenta del inventario cuando el jugador lo usa.
    /// </summary>
    public class PowerUpController : MonoBehaviour
    {
        public event Action Changed;

        private readonly Dictionary<ConsumableType, int> _charges = new Dictionary<ConsumableType, int>();
        private PlayerHealth _player;
        private MatchStats _stats;

        public bool MultiplierActive { get; private set; }

        public void Setup(PlayerHealth player, MatchStats stats)
        {
            _player = player;
            _stats = stats;
            _charges.Clear();
            MultiplierActive = false;

            var config = GameConfig.Instance;
            foreach (var type in GameSession.EquippedConsumables)
            {
                if (PlayerProgress.GetConsumableCount(type) <= 0) continue;
                var data = config.GetConsumable(type);
                if (data == null) continue;

                if (type == ConsumableType.CoinMultiplier)
                {
                    if (!PlayerProgress.TryUseConsumable(type)) continue;
                    _stats.CoinMultiplier = Mathf.Max(1f, data.value);
                    MultiplierActive = true;
                    GameEvents.RaisePowerUpActivated(type, -1f);
                }
                else
                {
                    _charges[type] = 1;
                }
            }
            Changed?.Invoke();
        }

        public bool IsAvailable(ConsumableType type) => _charges.TryGetValue(type, out int c) && c > 0;

        public bool TryActivate(ConsumableType type)
        {
            if (!IsAvailable(type) || _player == null || !_player.IsAlive) return false;
            var data = GameConfig.Instance.GetConsumable(type);
            if (data == null || !PlayerProgress.TryUseConsumable(type)) return false;

            _charges[type]--;
            switch (type)
            {
                case ConsumableType.Shield:
                    _player.ActivateShield(data.duration);
                    break;
                case ConsumableType.EnemyToCoins:
                    foreach (var enemy in EnemyRegistry.Snapshot()) if (enemy != null) enemy.ConvertToCoins();
                    AudioManager.Play(SfxId.Converter);
                    CameraFollow2D.Shake(0.25f, 0.3f);
                    break;
            }
            GameEvents.RaisePowerUpActivated(type, data.duration);
            Changed?.Invoke();
            return true;
        }
    }
}
