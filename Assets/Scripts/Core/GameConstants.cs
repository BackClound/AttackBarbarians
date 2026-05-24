/// <summary>
/// 全局常量：Tag、Layer、事件 Key、Resources 路径、对象池 Key。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。静态类，供全项目引用。</para>
/// <para><b>使用方式：</b>事件发布/订阅使用 <see cref="EventKeys"/>；对象池使用 <see cref="PoolKeys"/>；配置加载使用 <see cref="ResourcePaths"/>。</para>
/// </remarks>
public static class GameConstants
{
    public static class Tags
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Wall = "Wall";
    }

    public static class Layers
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Wall = "Wall";
        public const string Projectile = "Projectile";
    }

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
    }

    public static class UiPanelIds
    {
        public const string MainMenu = "ui.main_menu";
        public const string Pause = "ui.pause";
        public const string GameOver = "ui.game_over";
        public const string WaveTransition = "ui.wave_transition";
        public const string Upgrade = "ui.upgrade";
    }

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
    }

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
    }

    public static class ConfigIds
    {
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
        public const string UpgradeGoldBonus = "upgrade.gold_bonus";
        public const string TalentMaxHp = "talent.max_hp";
        public const string TalentAttackDamage = "talent.attack_damage";

        public const string EquipmentWeaponBattleAxe = "equipment.weapon.battle_axe";
        public const string EquipmentHelmetLeatherCap = "equipment.helmet.leather_cap";
        public const string EquipmentChestWarriorPlate = "equipment.chest.warrior_plate";
        public const string EquipmentBootsWarriorBoots = "equipment.boots.warrior_boots";
        public const string EquipmentSetWarrior = "set.warrior";
    }

    public static class PoolKeys
    {
        public const string Enemy = "Enemy";
        public const string Bullet = "Bullet";
        public const string DamageNumber = "DamageNumber";
    }
}
