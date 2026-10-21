namespace HyperArcade
{
    /// <summary>
    /// Reglas de compra de la tienda (sin UI). Cada operación valida, cobra y aplica.
    /// La UI (ShopController / PreMatchPopup) solo interpreta el PurchaseResult.
    /// </summary>
    public static class ShopService
    {
        // ---------------- Personajes ----------------

        public static PurchaseResult UnlockCharacter(CharacterData character)
        {
            if (character == null) return PurchaseResult.InvalidItem;
            if (PlayerProgress.IsCharacterUnlocked(character.id)) return PurchaseResult.AlreadyOwned;
            if (!Wallet.TrySpend(character.unlockPrice, "unlock_character_" + character.id)) return PurchaseResult.NotEnoughCurrency;

            PlayerProgress.UnlockCharacter(character.id);
            PlayerProgress.SelectCharacter(character.id);
            return PurchaseResult.Success;
        }

        // ---------------- Armas ----------------

        public static PurchaseResult UnlockWeapon(WeaponData weapon)
        {
            if (weapon == null) return PurchaseResult.InvalidItem;
            if (PlayerProgress.IsWeaponUnlocked(weapon.id)) return PurchaseResult.AlreadyOwned;
            if (!Wallet.TrySpend(weapon.unlockPrice, "unlock_weapon_" + weapon.id)) return PurchaseResult.NotEnoughCurrency;

            PlayerProgress.UnlockWeapon(weapon.id);
            PlayerProgress.SelectWeapon(weapon.id);
            return PurchaseResult.Success;
        }

        public static Price GetDamageUpgradePrice(WeaponData weapon) =>
            new Price(CurrencyType.Coins, weapon.GetUpgradeCost(PlayerProgress.GetWeapon(weapon.id).damageLevel));

        public static Price GetSpeedUpgradePrice(WeaponData weapon) =>
            new Price(CurrencyType.Coins, weapon.GetUpgradeCost(PlayerProgress.GetWeapon(weapon.id).delayLevel));

        /// <summary>Mejora de Damage.</summary>
        public static PurchaseResult UpgradeDamage(WeaponData weapon)
        {
            if (weapon == null) return PurchaseResult.InvalidItem;
            var progress = PlayerProgress.GetWeapon(weapon.id);
            if (!progress.unlocked) return PurchaseResult.Locked;
            if (progress.damageLevel >= weapon.maxDamageLevel) return PurchaseResult.MaxLevel;
            if (!Wallet.TrySpend(GetDamageUpgradePrice(weapon), "upgrade_damage_" + weapon.id)) return PurchaseResult.NotEnoughCurrency;

            PlayerProgress.IncreaseWeaponDamage(weapon.id);
            return PurchaseResult.Success;
        }

        /// <summary>Mejora de Attack Delay (reduce el tiempo entre disparos).</summary>
        public static PurchaseResult UpgradeAttackSpeed(WeaponData weapon)
        {
            if (weapon == null) return PurchaseResult.InvalidItem;
            var progress = PlayerProgress.GetWeapon(weapon.id);
            if (!progress.unlocked) return PurchaseResult.Locked;
            bool atFloor = weapon.GetAttackDelay(progress.delayLevel) <= weapon.minAttackDelay;
            if (progress.delayLevel >= weapon.maxDelayLevel || atFloor) return PurchaseResult.MaxLevel;
            if (!Wallet.TrySpend(GetSpeedUpgradePrice(weapon), "upgrade_speed_" + weapon.id)) return PurchaseResult.NotEnoughCurrency;

            PlayerProgress.IncreaseWeaponSpeed(weapon.id);
            return PurchaseResult.Success;
        }

        // ---------------- Consumibles ----------------

        public static PurchaseResult BuyConsumable(ConsumableData consumable)
        {
            if (consumable == null) return PurchaseResult.InvalidItem;
            if (!Wallet.TrySpend(consumable.price, "buy_" + consumable.type)) return PurchaseResult.NotEnoughCurrency;

            PlayerProgress.AddConsumable(consumable.type, consumable.amountPerPurchase);
            return PurchaseResult.Success;
        }
    }
}
