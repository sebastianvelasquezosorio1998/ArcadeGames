using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Potenciador comprado antes de la partida:
    ///  - CoinMultiplier: multiplica las monedas de toda la partida (value = 2).
    ///  - Shield: invulnerabilidad temporal (duration segundos) al pulsar su botón en el HUD.
    ///  - EnemyToCoins: convierte a todos los enemigos en pantalla en monedas (no afecta jefes).
    /// </summary>
    [CreateAssetMenu(menuName = "HyperArcade/Consumable", fileName = "Consumable_")]
    public class ConsumableData : ScriptableObject
    {
        public ConsumableType type;
        public string displayName = "x2 Monedas";
        [TextArea] public string description;
        public UiSlot icon = UiSlot.MultiplierIcon;
        public Price price = new Price(CurrencyType.Coins, 200);
        public float value = 2f;
        public float duration = 6f;
        [Tooltip("Cantidad que se recibe por compra.")]
        public int amountPerPurchase = 1;
    }
}
