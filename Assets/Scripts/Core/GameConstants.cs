/// <summary>
/// 全局常量：Tag、Layer、事件 Key、Resources 路径、对象池 Key。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。静态类，供全项目引用。</para>
/// <para><b>使用方式：</b>事件发布/订阅使用 <see cref="EventKeys"/>；对象池使用 <see cref="PoolKeys"/>；配置加载使用 <see cref="ResourcePaths"/>。</para>
/// </remarks>
public static class GameConstants
{
    /// <summary>Unity Tag 名称常量。</summary>
    public static class Tags
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Wall = "Wall";
    }

    /// <summary>Unity Layer 名称常量。</summary>
    public static class Layers
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Wall = "Wall";
        public const string Projectile = "Projectile";
    }

    /// <summary><see cref="EventBus"/> 事件 Key 字符串常量，按业务域分组。</summary>
    public static class EventKeys
    {
        // Game
        public const string GameStateChanged = "Game.StateChanged";
        public const string GameStarted = "Game.Started";
        public const string GamePaused = "Game.Paused";
        public const string GameResumed = "Game.Resumed";
        public const string GameOver = "Game.Over";

        // Wave
        public const string WaveStarted = "Wave.Started";
        public const string WaveCompleted = "Wave.Completed";

        // Player
        public const string PlayerDamaged = "Player.Damaged";
        public const string PlayerDied = "Player.Died";
        public const string PlayerHealthChanged = "Player.HealthChanged";
        public const string PlayerLevelUp = "Player.LevelUp";
        public const string PlayerAttackStarted = "Player.AttackStarted";
        public const string PlayerSkillCast = "Player.SkillCast";
        public const string PlayerStatsChanged = "Player.StatsChanged";

        // Enemy
        public const string EnemySpawned = "Enemy.Spawned";
        public const string EnemyKilled = "Enemy.Killed";

        // Boss
        public const string BossSpawned = "Boss.Spawned";
        public const string BossPhaseChanged = "Boss.PhaseChanged";
        public const string BossDefeated = "Boss.Defeated";

        // Elite
        public const string EliteSpawned = "Elite.Spawned";

        // Special Enemy
        public const string SpecialEnemySpawned = "SpecialEnemy.Spawned";
        public const string SpecialEnemyAbilityUsed = "SpecialEnemy.AbilityUsed";

        // Run
        public const string RunRewardSettled = "Run.RewardSettled";

        // Damage
        public const string DamageApplied = "Damage.Applied";

        // Projectile
        public const string ProjectileHit = "Projectile.Hit";

        // Skill
        public const string SkillUsed = "Skill.Used";
        public const string SkillLevelUp = "Skill.LevelUp";
        public const string SkillUnlocked = "Skill.Unlocked";

        // Buff
        public const string BuffApplied = "Buff.Applied";
        public const string BuffRemoved = "Buff.Removed";
        public const string BuffChanged = "Buff.Changed";

        // UI
        public const string UiPanelOpened = "UI.PanelOpened";
        public const string UiPanelClosed = "UI.PanelClosed";

        // Audio
        public const string AudioPlaySfx = "Audio.PlaySfx";
        public const string AudioPlayMusic = "Audio.PlayMusic";

        // Save
        public const string SaveCompleted = "Save.Completed";
        public const string SaveLoaded = "Save.Loaded";

        // Upgrade
        public const string UpgradeSelectionOpened = "Upgrade.SelectionOpened";
        public const string UpgradeSelectionCompleted = "Upgrade.SelectionCompleted";
        public const string UpgradeChoicesReady = "Upgrade.ChoicesReady";
        public const string UpgradeChoiceApplied = "Upgrade.ChoiceApplied";

        // Talent
        public const string TalentChanged = "Talent.Changed";

        // Equipment
        public const string EquipmentChanged = "Equipment.Changed";

        // Map
        public const string MapLoaded = "Map.Loaded";

        // Gameplay Event
        public const string GameplayEventStarted = "GameplayEvent.Started";
        public const string GameplayEventEnded = "GameplayEvent.Ended";

        // Economy
        public const string ResourceChanged = "Economy.ResourceChanged";

        // Shop
        public const string ShopPurchased = "Shop.Purchased";
        public const string ShopPurchaseFailed = "Shop.PurchaseFailed";

        // Achievement
        public const string AchievementProgressChanged = "Achievement.ProgressChanged";
        public const string AchievementClaimed = "Achievement.Claimed";
        public const string AchievementClaimFailed = "Achievement.ClaimFailed";

        // Daily Reward
        public const string DailyRewardClaimed = "DailyReward.Claimed";
        public const string DailyRewardClaimFailed = "DailyReward.ClaimFailed";
        public const string DailyRewardStateChanged = "DailyReward.StateChanged";

        // Upgrade Card
        public const string UpgradeCardGranted = "UpgradeCard.Granted";

        // Ad
        public const string AdRewardCompleted = "Ad.RewardCompleted";
        public const string AdRewardFailed = "Ad.RewardFailed";
        public const string AdRewardStateChanged = "Ad.StateChanged";
    }

    /// <summary>UI 面板标识符，用于 <see cref="GameEvents.RaiseUiPanelOpened"/> 等事件。</summary>
    public static class UiPanelIds
    {
        public const string MainMenu = "ui.main_menu";
        public const string Pause = "ui.pause";
        public const string GameOver = "ui.game_over";
        public const string WaveTransition = "ui.wave_transition";
        public const string Upgrade = "ui.upgrade";
        public const string Shop = "ui.shop";
        public const string SignIn = "ui.sign_in";
        public const string Achievement = "ui.achievement";
    }

    /// <summary>音频资源标识符，供 <see cref="AudioManager"/> 与事件系统引用。</summary>
    public static class AudioIds
    {
        public const string MusicMainMenu = "audio.music.main_menu";
        public const string MusicPaused = "audio.music.paused";
        public const string MusicGameOver = "audio.music.game_over";
        public const string MusicUpgrade = "audio.music.upgrade";
        public const string MusicBoss = "audio.music.boss";
        public const string SfxBossSpawn = "audio.sfx.boss_spawn";
        public const string SfxBossPhase = "audio.sfx.boss_phase";
        public const string SfxBossDefeat = "audio.sfx.boss_defeat";
        public const string SfxSpecialEnemySpawn = "audio.sfx.special_enemy_spawn";
        public const string SfxSpecialEnemyAbility = "audio.sfx.special_enemy_ability";
        public const string SfxUiClick = "audio.sfx.ui_click";
        public const string SfxUiConfirm = "audio.sfx.ui_confirm";
        public const string SfxPlayerHurt = "audio.sfx.player_hurt";
        public const string SfxEnemyHit = "audio.sfx.enemy_hit";
        public const string SfxEnemyKill = "audio.sfx.enemy_kill";
        public const string SfxSkillCast = "audio.sfx.skill_cast";
        public const string MusicGameplay = "audio.music.gameplay";
    }

    /// <summary>Resources 加载路径（不含扩展名）。</summary>
    public static class ResourcePaths
    {
        public const string GameConfig = "Config/GameConfig";
        public const string ConfigDatabase = "Config/ConfigDatabase";
        public const string EliteModeConfig = "Config/Elite/EliteModeConfig_Default";
        public const string RunRewardSettlement = "Config/RunReward/RunRewardSettlement_Default";
        public const string DamageCalculation = "Config/Damage/DamageCalculation_Default";
        public const string ProjectileDefault = "Config/Projectile/Projectile_Default";
        public const string AutoAttackDefault = "Config/AutoAttack/AutoAttack_Default";
        public const string CollisionDefault = "Config/Collision/Collision_Default";
        public const string RewardPoolDefault = "Config/Upgrade/RewardPool_Default";
        public const string SkillUnlockTable = "Config/Skill/SkillUnlockTable_Default";
        public const string MapDefault = "Config/Map/MapData_Default";
        public const string AudioDatabase = "Config/Audio/AudioDatabase";
        public const string ShopCatalog = "Config/Shop/ShopCatalog_Default";
        public const string AchievementCatalog = "Config/Achievement/AchievementCatalog_Default";
        public const string DailyRewardCatalog = "Config/DailyReward/DailyRewardCatalog_Default";
        public const string AdConfig = "Config/Ad/AdConfig_Default";
        public const string PerformanceBudget = "Config/Performance/PerformanceBudget_Default";
    }

    /// <summary>配置表条目 ID，与 ScriptableObject 资产一一对应。</summary>
    public static class ConfigIds
    {
        public const string MapDefault = "map.default";
        public const string PlayerDefault = "player.default";
        public const string EnemyBat = "enemy.bat";
        public const string SkillShoot = "skill.shoot";
        public const string SkillLightning = "skill.lightning";
        public const string SkillThunder = "skill.thunder";
        public const string SkillFireRain = "skill.fire_rain";
        public const string SkillWaterWave = "skill.water_wave";
        public const string SkillIce = "skill.ice";
        public const string SkillHeal = "skill.heal";
        public const string AutoAttackDefault = "auto_attack.default";
        public const string ProjectileDefault = "projectile.default";
        public const string BuffAttackUp = "buff.attack_up";
        public const string Wave01 = "wave.01";
        public const string BossBatKing = "boss.bat_king";
        public const string BossSkillAreaSlam = "boss_skill.area_slam";
        public const string BossSkillSummon = "boss_skill.summon_bats";
        public const string SpecialAbilityCharge = "special_ability.charge";
        public const string SpecialAbilityShield = "special_ability.shield";
        public const string SpecialAbilitySplit = "special_ability.split";
        public const string SpecialAbilitySummon = "special_ability.summon";
        public const string SpecialAbilityRanged = "special_ability.ranged";
        public const string EnemyBatCharge = "enemy.bat_charge";
        public const string EnemyBatShield = "enemy.bat_shield";
        public const string EnemyBatSplit = "enemy.bat_split";
        public const string EnemyBatSummoner = "enemy.bat_summoner";
        public const string EnemyBatRanged = "enemy.bat_ranged";
        public const string DropTableCommon = "drop.common";
        public const string RewardPoolDefault = "reward_pool.default";
        public const string UpgradeAttackUp = "upgrade.attack_up";
        public const string UpgradeShootPierce = "upgrade.shoot_pierce";
        public const string UpgradeUnlockLightning = "upgrade.unlock_lightning";
        public const string UpgradeUnlockThunder = "upgrade.unlock_thunder";
        public const string UpgradeUnlockFireRain = "upgrade.unlock_fire_rain";
        public const string UpgradeUnlockWaterWave = "upgrade.unlock_water_wave";
        public const string UpgradeUnlockIce = "upgrade.unlock_ice";
        public const string UpgradeUnlockHeal = "upgrade.unlock_heal";
        public const string UpgradeLightningChain = "upgrade.lightning_chain";
        public const string UpgradeThunderRadius = "upgrade.thunder_radius";
        public const string UpgradeGoldBonus = "upgrade.gold_bonus";
        public const string TalentMaxHp = "talent.max_hp";
        public const string TalentAttackDamage = "talent.attack_damage";

        public const string EquipmentWeaponBattleAxe = "equipment.weapon.battle_axe";
        public const string EquipmentHelmetLeatherCap = "equipment.helmet.leather_cap";
        public const string EquipmentChestWarriorPlate = "equipment.chest.warrior_plate";
        public const string EquipmentBootsWarriorBoots = "equipment.boots.warrior_boots";
        public const string EquipmentSetWarrior = "set.warrior";

        public const string GameplayEventSwarm = "gameplay_event.swarm";

        public const string ShopGoldPackSmall = "shop.gold_pack_small";
        public const string ShopGoldPackLarge = "shop.gold_pack_large";
        public const string ShopDiamondPack = "shop.diamond_pack";
        public const string ShopFreeDiamond = "shop.free_diamond";

        public const string ShopCrateCommonSingle = "shop.crate_common_single";
        public const string ShopCrateCommonTen = "shop.crate_common_ten";
        public const string ShopCratePremiumSingle = "shop.crate_premium_single";
        public const string ShopCratePremiumTen = "shop.crate_premium_ten";
        public const string ShopGoldSupplyLowSingle = "shop.gold_supply_low_single";
        public const string ShopGoldSupplyLowTen = "shop.gold_supply_low_ten";
        public const string ShopGoldSupplyStdSingle = "shop.gold_supply_std_single";
        public const string ShopGoldSupplyStdTen = "shop.gold_supply_std_ten";
        public const string ShopAdCrateCommon = "shop.ad_crate_common";
        public const string ShopAdCratePremium = "shop.ad_crate_premium";
        public const string ShopAdGoldSupply = "shop.ad_gold_supply";

        public const string ShopExchangeDiamond1 = "shop.exchange_diamond_1";
        public const string ShopExchangeDiamond2 = "shop.exchange_diamond_2";
        public const string ShopExchangeDiamond3 = "shop.exchange_diamond_3";
        public const string ShopExchangeDiamond4 = "shop.exchange_diamond_4";
        public const string ShopExchangeGold1 = "shop.exchange_gold_1";
        public const string ShopExchangeGold2 = "shop.exchange_gold_2";
        public const string ShopExchangeGold3 = "shop.exchange_gold_3";
        public const string ShopExchangeGold4 = "shop.exchange_gold_4";

        public const string AchievementFirstBlood = "achievement.first_blood";
        public const string AchievementWave5 = "achievement.wave_5";
        public const string AchievementBossSlayer = "achievement.boss_slayer";
        public const string AchievementGoldCollector = "achievement.gold_collector";

        public const string DailyRewardDay1 = "daily_reward.day_1";
        public const string DailyRewardDay7 = "daily_reward.day_7";

        public const string AdTicketEarn = "ad.ticket_earn";
        public const string UpgradeReroll = "ad.upgrade_reroll";
        public const string UpgradeSelectAll = "ad.upgrade_select_all";
    }

    /// <summary><see cref="PoolManager"/> 对象池 Key 常量。</summary>
    public static class PoolKeys
    {
        public const string Enemy = "Enemy";
        public const string Bullet = "Bullet";
        public const string DamageNumber = "DamageNumber";
        public const string CombatVfx = "CombatVfx";
    }
}
