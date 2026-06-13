using System.Collections.Generic;

/// <summary>
/// 文件夹 → 程序集 → 最低迁移阶段的静态目录（供 Editor 工具生成 <c>.asmref</c>）。
/// </summary>
/// <remarks>
/// <para>程序集定义位于 <c>Assets/Scripts/_Assemblies/{Name}/{Name}.asmdef</c>。</para>
/// <para>详见 <c>docs/version_02/asmdef_migration.md</c>。</para>
/// </remarks>
public static class AssemblyMigrationCatalog
{
    /// <summary>运行时程序集定义根目录（相对 Assets）。</summary>
    public const string AssembliesRoot = "Assets/Scripts/_Assemblies";
    /// <summary>Editor 程序集定义根目录（相对 Assets）。</summary>
    public const string EditorAssembliesRoot = "Assets/Editor/_Assemblies";

    /// <summary>脚本目录与目标程序集、最低迁移阶段的绑定关系。</summary>
    public readonly struct FolderBinding
    {
        /// <summary>脚本目录路径（相对 Assets）。</summary>
        public readonly string Folder;
        /// <summary>目标程序集名称（如 AB.Core）。</summary>
        public readonly string AssemblyName;
        /// <summary>启用该绑定所需的最低迁移阶段。</summary>
        public readonly AssemblyMigrationPhase MinimumPhase;

        /// <summary>构造目录绑定项。</summary>
        /// <param name="folder">脚本目录。</param>
        /// <param name="assemblyName">程序集名。</param>
        /// <param name="minimumPhase">最低阶段。</param>
        public FolderBinding(string folder, string assemblyName, AssemblyMigrationPhase minimumPhase)
        {
            Folder = folder;
            AssemblyName = assemblyName;
            MinimumPhase = minimumPhase;
        }
    }

    /// <summary>阶段激活前需搬移的资源项（保持 GUID）。</summary>
    public readonly struct AssetMove
    {
        /// <summary>源路径（相对 Assets）。</summary>
        public readonly string Source;
        /// <summary>目标路径（相对 Assets）。</summary>
        public readonly string Destination;
        /// <summary>执行搬移所需的最低迁移阶段。</summary>
        public readonly AssemblyMigrationPhase RequiredPhase;

        /// <summary>构造资源搬移项。</summary>
        /// <param name="source">源路径。</param>
        /// <param name="destination">目标路径。</param>
        /// <param name="requiredPhase">所需阶段。</param>
        public AssetMove(string source, string destination, AssemblyMigrationPhase requiredPhase)
        {
            Source = source;
            Destination = destination;
            RequiredPhase = requiredPhase;
        }
    }

    /// <summary>全部目录绑定列表，供 Editor 工具生成 <c>.asmref</c>。</summary>
    public static IReadOnlyList<FolderBinding> FolderBindings { get; } = new[]
    {
        new FolderBinding("Assets/Scripts/Core/Singleton", "AB.Core", AssemblyMigrationPhase.CoreSingleton),
        new FolderBinding("Assets/Scripts/Contracts", "AB.Core", AssemblyMigrationPhase.CoreFoundation),
        new FolderBinding("Assets/Scripts/Core/L0", "AB.Core", AssemblyMigrationPhase.CoreFoundation),
        new FolderBinding("Assets/Scripts/Events", "AB.Core", AssemblyMigrationPhase.CoreFoundation),
        new FolderBinding("Assets/Scripts/Pool", "AB.Core", AssemblyMigrationPhase.CoreFoundation),
        new FolderBinding("Assets/Scripts/Utils/DeviceInfo", "AB.Core", AssemblyMigrationPhase.CoreFoundation),

        new FolderBinding("Assets/Scripts/Config", "AB.Config", AssemblyMigrationPhase.Config),
        new FolderBinding("Assets/Scripts/Data", "AB.Config", AssemblyMigrationPhase.Config),
        new FolderBinding("Assets/Scripts/Stats", "AB.Config", AssemblyMigrationPhase.Config),

        new FolderBinding("Assets/Scripts/Player", "AB.Combat", AssemblyMigrationPhase.Combat),
        new FolderBinding("Assets/Scripts/Enemy", "AB.Combat", AssemblyMigrationPhase.Combat),
        new FolderBinding("Assets/Scripts/Damage", "AB.Combat", AssemblyMigrationPhase.Combat),
        new FolderBinding("Assets/Scripts/Projectile", "AB.Combat", AssemblyMigrationPhase.Combat),
        new FolderBinding("Assets/Scripts/Collision", "AB.Combat", AssemblyMigrationPhase.Combat),
        new FolderBinding("Assets/Scripts/AutoAttack", "AB.Combat", AssemblyMigrationPhase.Combat),
        new FolderBinding("Assets/Scripts/Common", "AB.Combat", AssemblyMigrationPhase.Combat),
        new FolderBinding("Assets/Scripts/Wall", "AB.Combat", AssemblyMigrationPhase.Combat),
        new FolderBinding("Assets/Scripts/Interface", "AB.Combat", AssemblyMigrationPhase.Combat),

        new FolderBinding("Assets/Scripts/SkillSystem", "AB.Skills", AssemblyMigrationPhase.Skills),

        new FolderBinding("Assets/Scripts/Wave", "AB.Gameplay", AssemblyMigrationPhase.Gameplay),
        new FolderBinding("Assets/Scripts/Boss", "AB.Gameplay", AssemblyMigrationPhase.Gameplay),
        new FolderBinding("Assets/Scripts/SpecialEnemy", "AB.Gameplay", AssemblyMigrationPhase.Gameplay),
        new FolderBinding("Assets/Scripts/Map", "AB.Gameplay", AssemblyMigrationPhase.Gameplay),
        new FolderBinding("Assets/Scripts/Upgrade", "AB.Gameplay", AssemblyMigrationPhase.Gameplay),
        new FolderBinding("Assets/Scripts/Elite", "AB.Gameplay", AssemblyMigrationPhase.Gameplay),
        new FolderBinding("Assets/Scripts/Content", "AB.Gameplay", AssemblyMigrationPhase.Gameplay),

        new FolderBinding("Assets/Scripts/Save", "AB.Meta", AssemblyMigrationPhase.Meta),
        new FolderBinding("Assets/Scripts/Talent", "AB.Meta", AssemblyMigrationPhase.Meta),
        new FolderBinding("Assets/Scripts/Equipment", "AB.Meta", AssemblyMigrationPhase.Meta),
        new FolderBinding("Assets/Scripts/Shop", "AB.Meta", AssemblyMigrationPhase.Meta),
        new FolderBinding("Assets/Scripts/Economy", "AB.Meta", AssemblyMigrationPhase.Meta),
        new FolderBinding("Assets/Scripts/Achievement", "AB.Meta", AssemblyMigrationPhase.Meta),
        new FolderBinding("Assets/Scripts/DailyReward", "AB.Meta", AssemblyMigrationPhase.Meta),

        new FolderBinding("Assets/Scripts/UI", "AB.Presentation", AssemblyMigrationPhase.Presentation),
        new FolderBinding("Assets/Scripts/Audio", "AB.Presentation", AssemblyMigrationPhase.Presentation),
        new FolderBinding("Assets/Scripts/Performance", "AB.Presentation", AssemblyMigrationPhase.Presentation),
        new FolderBinding("Assets/Scripts/ObjectVFX", "AB.Presentation", AssemblyMigrationPhase.Presentation),

        new FolderBinding("Assets/Scripts/Core/App", "AB.App", AssemblyMigrationPhase.App),
        new FolderBinding("Assets/Scripts/Managers", "AB.App", AssemblyMigrationPhase.App),
        new FolderBinding("Assets/Scripts/Utils", "AB.App", AssemblyMigrationPhase.App),

        new FolderBinding("Assets/Editor/Config", "AB.Editor", AssemblyMigrationPhase.Editor),
    };

    /// <summary>阶段激活前由 Editor 搬移的资源（保持 GUID）。</summary>
    public static IReadOnlyList<AssetMove> AssetMoves { get; } = new[]
    {
        new AssetMove(
            "Assets/Scripts/Damage/ElementType.cs",
            "Assets/Scripts/Contracts/ElementType.cs",
            AssemblyMigrationPhase.CoreFoundation),
        new AssetMove(
            "Assets/Scripts/Projectile/ProjectileMotionType.cs",
            "Assets/Scripts/Contracts/ProjectileMotionType.cs",
            AssemblyMigrationPhase.CoreFoundation),
        new AssetMove("Assets/Scripts/Core/EventBus.cs", "Assets/Scripts/Core/L0/EventBus.cs", AssemblyMigrationPhase.CoreFoundation),
        new AssetMove("Assets/Scripts/Core/ServiceLocator.cs", "Assets/Scripts/Core/L0/ServiceLocator.cs", AssemblyMigrationPhase.CoreFoundation),
        new AssetMove("Assets/Scripts/Core/IGameSystem.cs", "Assets/Scripts/Core/L0/IGameSystem.cs", AssemblyMigrationPhase.CoreFoundation),
        new AssetMove("Assets/Scripts/Core/GameState.cs", "Assets/Scripts/Core/L0/GameState.cs", AssemblyMigrationPhase.CoreFoundation),
        new AssetMove("Assets/Scripts/Core/GameStateChange.cs", "Assets/Scripts/Core/L0/GameStateChange.cs", AssemblyMigrationPhase.CoreFoundation),
        new AssetMove("Assets/Scripts/Core/GameStateMachine.cs", "Assets/Scripts/Core/L0/GameStateMachine.cs", AssemblyMigrationPhase.CoreFoundation),
        new AssetMove("Assets/Scripts/Core/GameConstants.cs", "Assets/Scripts/Core/L0/GameConstants.cs", AssemblyMigrationPhase.CoreFoundation),
        new AssetMove("Assets/Scripts/Core/GameDebug.cs", "Assets/Scripts/Core/L0/GameDebug.cs", AssemblyMigrationPhase.CoreFoundation),
        new AssetMove(
            "Assets/Scripts/Core/AssemblyMigrationPhase.cs",
            "Assets/Scripts/Core/L0/AssemblyMigrationPhase.cs",
            AssemblyMigrationPhase.CoreFoundation),
        new AssetMove(
            "Assets/Scripts/Core/AssemblyMigrationCatalog.cs",
            "Assets/Scripts/Core/L0/AssemblyMigrationCatalog.cs",
            AssemblyMigrationPhase.CoreFoundation),
        new AssetMove(
            "Assets/Scripts/Utils/DeviceInfoUtils.cs",
            "Assets/Scripts/Utils/DeviceInfo/DeviceInfoUtils.cs",
            AssemblyMigrationPhase.CoreFoundation),
        new AssetMove(
            "Assets/Scripts/Core/GameBootstrapper.cs",
            "Assets/Scripts/Core/App/GameBootstrapper.cs",
            AssemblyMigrationPhase.App),
        new AssetMove(
            "Assets/Scripts/Save/SaveConstants.cs",
            "Assets/Scripts/Config/SaveConstants.cs",
            AssemblyMigrationPhase.Config),
    };

    /// <summary>获取指定程序集的 <c>.asmdef</c> 文件路径。</summary>
    /// <param name="assemblyName">程序集名称。</param>
    /// <returns>相对项目根的路径。</returns>
    public static string GetAsmdefPath(string assemblyName)
    {
        if (assemblyName == "AB.Editor")
        {
            return $"{EditorAssembliesRoot}/{assemblyName}/{assemblyName}.asmdef";
        }

        return $"{AssembliesRoot}/{assemblyName}/{assemblyName}.asmdef";
    }

    /// <summary>枚举不超过指定阶段应激活的全部目录绑定。</summary>
    /// <param name="phase">目标迁移阶段。</param>
    /// <returns>符合条件的绑定项。</returns>
    public static IEnumerable<FolderBinding> GetBindingsUpTo(AssemblyMigrationPhase phase)
    {
        for (int i = 0; i < FolderBindings.Count; i++)
        {
            FolderBinding binding = FolderBindings[i];
            if (binding.MinimumPhase <= phase && binding.MinimumPhase > AssemblyMigrationPhase.None)
            {
                yield return binding;
            }
        }
    }

    /// <summary>枚举不超过指定阶段应执行的全部资源搬移。</summary>
    /// <param name="phase">目标迁移阶段。</param>
    /// <returns>符合条件的搬移项。</returns>
    public static IEnumerable<AssetMove> GetMovesUpTo(AssemblyMigrationPhase phase)
    {
        for (int i = 0; i < AssetMoves.Count; i++)
        {
            AssetMove move = AssetMoves[i];
            if (move.RequiredPhase <= phase)
            {
                yield return move;
            }
        }
    }
}
