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
    public const string AssembliesRoot = "Assets/Scripts/_Assemblies";
    public const string EditorAssembliesRoot = "Assets/Editor/_Assemblies";

    public readonly struct FolderBinding
    {
        public readonly string Folder;
        public readonly string AssemblyName;
        public readonly AssemblyMigrationPhase MinimumPhase;

        public FolderBinding(string folder, string assemblyName, AssemblyMigrationPhase minimumPhase)
        {
            Folder = folder;
            AssemblyName = assemblyName;
            MinimumPhase = minimumPhase;
        }
    }

    /// <summary>按阶段推进时需先完成的资源搬移（相对 Assets/）。</summary>
    public readonly struct AssetMove
    {
        public readonly string Source;
        public readonly string Destination;
        public readonly AssemblyMigrationPhase RequiredPhase;

        public AssetMove(string source, string destination, AssemblyMigrationPhase requiredPhase)
        {
            Source = source;
            Destination = destination;
            RequiredPhase = requiredPhase;
        }
    }

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

    public static string GetAsmdefPath(string assemblyName)
    {
        if (assemblyName == "AB.Editor")
        {
            return $"{EditorAssembliesRoot}/{assemblyName}/{assemblyName}.asmdef";
        }

        return $"{AssembliesRoot}/{assemblyName}/{assemblyName}.asmdef";
    }

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
