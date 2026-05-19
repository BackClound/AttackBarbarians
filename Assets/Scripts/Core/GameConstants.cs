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
        public const string GameStateChanged = "Game.StateChanged";
        public const string GamePaused = "Game.Paused";
        public const string GameResumed = "Game.Resumed";
        public const string GameOver = "Game.Over";
        public const string WaveStarted = "Wave.Started";
        public const string WaveCompleted = "Wave.Completed";
        public const string PlayerDamaged = "Player.Damaged";
        public const string EnemyKilled = "Enemy.Killed";
    }

    public static class ResourcePaths
    {
        public const string GameConfig = "Config/GameConfig";
    }

    public static class PoolKeys
    {
        public const string Enemy = "Enemy";
        public const string Bullet = "Bullet";
        public const string DamageNumber = "DamageNumber";
    }
}
