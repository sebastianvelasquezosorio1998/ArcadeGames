namespace HyperArcade
{
    // Enumeraciones compartidas por todo el juego. Mantenerlas en un solo archivo
    // facilita que el equipo de arte / reskin vea qué "ranuras" existen.

    public enum CurrencyType { Coins, Diamonds }

    public enum Team { Player, Enemy }

    public enum GameMode { Levels, Endless }

    public enum MatchState { Starting, Playing, ReviveOffer, Paused, Victory, Defeat }

    public enum MissionType { KillEnemies, Survive, DefeatBoss }

    public enum EnemyBehavior { Walker, Flyer, Shooter, Boss }

    public enum SpecialAbilityType { SpreadBurst, PiercingShot, Nova }

    public enum ConsumableType { CoinMultiplier, Shield, EnemyToCoins }

    public enum PurchaseResult { Success, NotEnoughCurrency, AlreadyOwned, MaxLevel, Locked, InvalidItem }

    /// <summary>Estados de animación que cualquier VisualSet puede definir.</summary>
    public enum AnimState { Idle, Run, Jump, Fall, Attack, Hurt, Death }

    /// <summary>Puntos del juego donde se muestra un anuncio recompensado.</summary>
    public enum AdPlacement { MenuDiamonds, MenuCoins, ShopDiamonds, ShopCoins, Revive, DoubleCoins }

    /// <summary>Efectos de sonido. El audio real lo provee el SkinProfile activo.</summary>
    public enum SfxId
    {
        ButtonClick, Purchase, Error, Shoot, Special, Jump, PlayerHit, PlayerDie,
        EnemyHit, EnemyDie, Coin, Shield, Converter, BossAppear, Victory, Defeat, Star
    }

    public enum MusicId { None, Menu, Gameplay, Boss }

    /// <summary>Ranuras de sprites de interfaz reemplazables por marca.</summary>
    public enum UiSlot
    {
        Logo, MenuBackground, PanelBackground, ButtonBackground,
        CoinIcon, DiamondIcon, StarFull, StarEmpty, LockIcon,
        ShieldIcon, MultiplierIcon, ConverterIcon,
        JoystickBase, JoystickKnob, JumpButton, AttackButton, SpecialButton, PauseButton
    }

    /// <summary>Colores de marca reemplazables.</summary>
    public enum PaletteSlot { Primary, Secondary, Accent, Text, Background, Danger }

    /// <summary>Textos de marca reemplazables.</summary>
    public enum BrandTextSlot { GameTitle, CompanyName, Slogan }
}
