/// <summary>
/// 程序集分阶段迁移阶段（与 <see cref="AssemblyMigrationCatalog"/> 一致）。
/// </summary>
/// <remarks>
/// <para>阶段 0 表示全部脚本仍在默认 <c>Assembly-CSharp</c>。</para>
/// <para>通过 Unity 菜单 <c>Attack Barbarians/Assembly/Apply Through Phase...</c> 推进；每阶段仅增加 <c>.asmref</c>，不移动业务脚本目录结构（除 Contracts / L0 等已文档化的准备项）。</para>
/// </remarks>
public enum AssemblyMigrationPhase
{
    /// <summary>未启用 asmdef，全部在 Assembly-CSharp。</summary>
    None = 0,

    /// <summary>AB.Core：Singleton 框架。</summary>
    CoreSingleton = 1,

    /// <summary>AB.Core：Contracts 枚举、L0 基础类型、Events、Pool、Utils/DeviceInfoUtils。</summary>
    CoreFoundation = 2,

    /// <summary>AB.Config：Config、Data、Stats。</summary>
    Config = 3,

    /// <summary>AB.Combat：Player、Enemy、Damage、Projectile、Collision、AutoAttack、Common、Wall、Interface。</summary>
    Combat = 4,

    /// <summary>AB.Skills：SkillSystem。</summary>
    Skills = 5,

    /// <summary>AB.Gameplay：Wave、Boss、SpecialEnemy、Map、Upgrade、Elite、Content 等。</summary>
    Gameplay = 6,

    /// <summary>AB.Meta：Save、Talent、Equipment、Shop、Economy、Achievement、DailyReward。</summary>
    Meta = 7,

    /// <summary>AB.Presentation：UI、Audio、Performance、ObjectVFX。</summary>
    Presentation = 8,

    /// <summary>AB.App：GameBootstrapper、Managers 编排。</summary>
    App = 9,

    /// <summary>AB.Editor：Editor 脚本归入 AB.Editor。</summary>
    Editor = 10,

    /// <summary>迁移完成，无剩余 Assembly-CSharp 游戏脚本（除 Plugins）。</summary>
    Complete = 11
}
