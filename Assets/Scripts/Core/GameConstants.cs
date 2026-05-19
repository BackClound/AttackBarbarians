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

        // Enemy
        public const string EnemySpawned = "Enemy.Spawned";
        public const string EnemyKilled = "Enemy.Killed";

        // Damage
        public const string DamageApplied = "Damage.Applied";

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
    }

    public static class ResourcePaths
    {
        public const string GameConfig = "Config/GameConfig";
        public const string ConfigDatabase = "Config/ConfigDatabase";
    }

    public static class ConfigIds
    {
        public const string PlayerDefault = "player.default";
        public const string EnemyBat = "enemy.bat";
        public const string SkillShoot = "skill.shoot";
        public const string BuffAttackUp = "buff.attack_up";
        public const string Wave01 = "wave.01";
        public const string DropTableCommon = "drop.common";
    }

    public static class PoolKeys
    {
        public const string Enemy = "Enemy";
        public const string Bullet = "Bullet";
        public const string DamageNumber = "DamageNumber";
    }
}
