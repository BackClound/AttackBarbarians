#!/usr/bin/env python3
"""Generate multi-tier UpgradeOption assets and update RewardPool_Default / ConfigDatabase."""

from __future__ import annotations

import re
import secrets
import textwrap
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UPGRADE_DIR = ROOT / "Assets/Resources/Config/Upgrade"
CONFIG_DB = ROOT / "Assets/Resources/Config/ConfigDatabase.asset"
POOL_ASSET = UPGRADE_DIR / "RewardPool_Default.asset"
SCRIPT_GUID = "a8800af4b187049ada2bb0decfa19516"
POOL_SCRIPT_GUID = "165cc72cc3f594281af4630af166481f"

SKILL_BUFF_KINDS = [
    "None",
    "ShootTrajectoryLines",
    "ShootVolleyCount",
    "ShootPierce",
    "ShootBounce",
    "ShootSplitOnHit",
    "LightningBoltCount",
    "LightningChainTargets",
    "LightningEndExplosion",
    "LightningStun",
    "ThunderStrikeCount",
    "ThunderRadius",
    "ThunderStunAll",
    "ThunderStunDuration",
    "ThunderPersistentZone",
    "FireRainRadius",
    "FireRainDuration",
    "FireRainChainOnKill",
    "WaterWaveCount",
    "WaterSlowStrength",
    "WaterSlowDuration",
    "WaterWaveSize",
    "IceTrajectoryLines",
    "IceVolleyCount",
    "IceFreezeDuration",
    "IceProjectileSize",
    "IceHalfRangeExplosion",
    "IceExplosionRadius",
    "HealRegenPerSecond",
    "HealMaxHpPercent",
    "HealPeriodicTenPercent",
    "HealPeakGrowthEvery3Min",
    "HealOneTimeFull",
    "GlobalAttackSpeed",
    "GlobalBaseDamage",
    "GlobalCritChance",
    "GlobalCritDamage",
    "GlobalCooldownReduction",
]

LEGACY_FILE = {
    ("ShootPierce", 1): "UpgradeOption_ShootPierce",
    ("ShootTrajectoryLines", 1): "UpgradeOption_ShootTrajectoryLines",
    ("GlobalCritChance", 1): "UpgradeOption_GlobalCritChance",
    ("GlobalAttackSpeed", 1): "UpgradeOption_GlobalAttackSpeed",
    ("LightningChainTargets", 1): "UpgradeOption_LightningChain",
    ("ThunderRadius", 1): "UpgradeOption_ThunderRadius",
}

LEGACY_CONFIG_ID = {
    ("ShootPierce", 1): "upgrade.shoot_pierce",
    ("ShootTrajectoryLines", 1): "upgrade.shoot_trajectory",
    ("GlobalCritChance", 1): "upgrade.global_crit_chance",
    ("GlobalAttackSpeed", 1): "upgrade.global_attack_speed",
    ("LightningChainTargets", 1): "upgrade.lightning_chain",
    ("ThunderRadius", 1): "upgrade.thunder_radius",
}

LABELS = {
    "ShootTrajectoryLines": "Twin Volley",
    "ShootVolleyCount": "Volley Count",
    "ShootPierce": "Piercing Arrows",
    "ShootBounce": "Arrow Bounce",
    "ShootSplitOnHit": "Split Shot",
    "LightningBoltCount": "Lightning Bolts",
    "LightningChainTargets": "Lightning Chain",
    "LightningEndExplosion": "Chain Explosion",
    "ThunderStrikeCount": "Thunder Strikes",
    "ThunderRadius": "Thunder Radius",
    "ThunderStunDuration": "Thunder Stun+",
    "ThunderPersistentZone": "Thunder Zone",
    "FireRainRadius": "Fire Rain Radius",
    "FireRainDuration": "Fire Rain Duration",
    "FireRainChainOnKill": "Fire Rain Chain",
    "WaterWaveCount": "Water Waves",
    "WaterSlowStrength": "Water Slow+",
    "WaterSlowDuration": "Slow Duration",
    "WaterWaveSize": "Wave Size",
    "IceTrajectoryLines": "Ice Fan",
    "IceVolleyCount": "Ice Volley",
    "IceFreezeDuration": "Freeze Duration",
    "IceProjectileSize": "Ice Size",
    "IceHalfRangeExplosion": "Mid-Range Blast",
    "IceExplosionRadius": "Blast Radius",
    "HealMaxHpPercent": "Max HP Up",
    "GlobalAttackSpeed": "Attack Speed Up",
    "GlobalBaseDamage": "Base Damage Up",
    "GlobalCritChance": "Crit Chance Up",
    "GlobalCritDamage": "Crit Damage Up",
    "GlobalCooldownReduction": "Cooldown Reduction",
}

UNLOCK_OPTIONS = [
    ("UpgradeOption_UnlockLightning", "upgrade.unlock_lightning"),
    ("UpgradeOption_UnlockThunder", "upgrade.unlock_thunder"),
    ("UpgradeOption_UnlockHeal", "upgrade.unlock_heal"),
    ("UpgradeOption_UnlockIce", "upgrade.unlock_ice"),
    ("UpgradeOption_UnlockFireRain", "upgrade.unlock_fire_rain"),
    ("UpgradeOption_UnlockWaterWave", "upgrade.unlock_water_wave"),
]


def get_max_tier(kind: str) -> int:
    if kind in {
        "ShootTrajectoryLines",
        "LightningBoltCount",
        "LightningChainTargets",
    }:
        return 4
    if kind in {
        "LightningEndExplosion",
        "ThunderRadius",
        "FireRainRadius",
        "FireRainDuration",
        "WaterSlowDuration",
        "IceExplosionRadius",
        "HealMaxHpPercent",
    }:
        return 5
    if kind in {
        "ShootPierce",
        "WaterSlowStrength",
        "IceFreezeDuration",
        "IceProjectileSize",
        "IceHalfRangeExplosion",
        "ThunderPersistentZone",
    }:
        return 3
    if kind in {
        "ShootVolleyCount",
        "ShootBounce",
        "ShootSplitOnHit",
        "ThunderStunDuration",
        "FireRainChainOnKill",
        "WaterWaveCount",
        "WaterWaveSize",
        "IceTrajectoryLines",
        "IceVolleyCount",
    }:
        return 2
    if kind in {
        "GlobalAttackSpeed",
        "GlobalBaseDamage",
        "GlobalCritChance",
        "GlobalCritDamage",
        "GlobalCooldownReduction",
    }:
        return 5
    if kind == "ThunderStrikeCount":
        return 4
    return 1


def count_at_tier(table: list[int], tier: int) -> int:
    tier = max(1, min(tier, len(table)))
    return table[tier - 1]


def build_description(kind: str, tier: int) -> str:
    if kind == "ShootTrajectoryLines":
        return f"扇形弹道 {count_at_tier([2, 3, 4, 5], tier)} 条"
    if kind == "ShootVolleyCount":
        return f"每次齐射 {count_at_tier([2, 3], tier)} 发"
    if kind == "ShootPierce":
        return f"穿透 +{count_at_tier([1, 2, 3], tier)}"
    if kind == "ShootBounce":
        return f"弹射 {tier} 次"
    if kind == "ShootSplitOnHit":
        return f"命中分裂 {tier} 次"
    if kind == "LightningBoltCount":
        return f"并行闪电 {count_at_tier([2, 3, 4, 5], tier)} 道"
    if kind == "LightningChainTargets":
        return f"链式连接 {count_at_tier([2, 3, 4, 5], tier)} 个目标"
    if kind == "LightningEndExplosion":
        return "链末端范围爆炸"
    if kind == "ThunderStrikeCount":
        return f"落雷 {count_at_tier([2, 3, 4, 5], tier)} 次"
    if kind == "ThunderRadius":
        return "落雷范围扩大"
    if kind == "ThunderStunDuration":
        return "麻痹时长提升"
    if kind == "ThunderPersistentZone":
        return "生成持续伤害区域"
    if kind == "FireRainRadius":
        return "火雨范围扩大"
    if kind == "FireRainDuration":
        return "火雨持续时间延长"
    if kind == "FireRainChainOnKill":
        return "击杀连锁附近 3 敌" if tier >= 2 else "击杀连锁附近 2 敌"
    if kind == "WaterWaveCount":
        return "水浪 3 道" if tier >= 2 else "水浪 2 道"
    if kind == "WaterSlowStrength":
        return "减速强度提升"
    if kind == "WaterSlowDuration":
        return "减速时长延长"
    if kind == "WaterWaveSize":
        return "水浪体积扩大"
    if kind == "IceTrajectoryLines":
        return "冰霜扇形 3 条" if tier >= 2 else "冰霜扇形 2 条"
    if kind == "IceVolleyCount":
        return "冰霜齐射 3 发" if tier >= 2 else "冰霜齐射 2 发"
    if kind == "IceFreezeDuration":
        return "冰冻时长延长"
    if kind == "IceProjectileSize":
        return "冰霜弹道体积扩大"
    if kind == "IceHalfRangeExplosion":
        return "半程范围爆炸并冰冻"
    if kind == "IceExplosionRadius":
        return "爆炸范围扩大"
    if kind == "HealMaxHpPercent":
        return "最大生命百分比提升"
    if kind == "GlobalAttackSpeed":
        return f"全局攻速 +{tier * 10}%"
    if kind == "GlobalBaseDamage":
        return f"全局伤害 +{tier * 10}%"
    if kind == "GlobalCritChance":
        return f"全局暴击率 +{tier * 10}%"
    if kind == "GlobalCritDamage":
        return f"全局暴击伤害 +{tier * 10}%"
    if kind == "GlobalCooldownReduction":
        return f"全局冷却缩减 {tier * 10}%"
    return kind


def to_snake_case(value: str) -> str:
    parts: list[str] = []
    for i, ch in enumerate(value):
        if ch.isupper() and i > 0:
            parts.append("_")
        parts.append(ch.lower())
    return "".join(parts)


def build_file_name(kind: str, tier: int) -> str:
    legacy = LEGACY_FILE.get((kind, tier))
    if legacy:
        return legacy
    if get_max_tier(kind) > 1:
        return f"UpgradeOption_{kind}_T{tier}"
    return f"UpgradeOption_{kind}"


def build_config_id(kind: str, tier: int) -> str:
    legacy = LEGACY_CONFIG_ID.get((kind, tier))
    if legacy:
        return legacy
    snake = to_snake_case(kind)
    if tier <= 1 and get_max_tier(kind) == 1:
        return f"upgrade.{snake}"
    return f"upgrade.{snake}.t{tier}"


def build_display_name(kind: str, tier: int) -> str:
    label = LABELS.get(kind, kind)
    return label if tier <= 1 else f"{label} T{tier}"


def yaml_escape(value: str) -> str:
    escaped = value.encode("unicode_escape").decode("ascii")
    escaped = escaped.replace('"', '\\"')
    return f'"{escaped}"'


def read_guid(meta_path: Path) -> str | None:
    if not meta_path.exists():
        return None
    match = re.search(r"^guid: (.+)$", meta_path.read_text(encoding="utf-8"), re.MULTILINE)
    return match.group(1).strip() if match else None


def write_meta(asset_path: Path, guid: str) -> None:
    meta_path = asset_path.with_suffix(asset_path.suffix + ".meta")
    meta_path.write_text(
        textwrap.dedent(
            f"""\
            fileFormatVersion: 2
            guid: {guid}
            NativeFormatImporter:
              externalObjects: {{}}
              mainObjectFileID: 11400000
              userData:
              assetBundleName:
              assetBundleVariant:
            """
        ),
        encoding="utf-8",
    )


def new_guid() -> str:
    return secrets.token_hex(16)


def write_upgrade_asset(
    file_name: str,
    config_id: str,
    display_name: str,
    description: str,
    kind: str,
    tier: int,
    effect_type: int = 1,
    max_stacks: int = 1,
) -> str:
    asset_path = UPGRADE_DIR / f"{file_name}.asset"
    meta_path = asset_path.with_suffix(asset_path.suffix + ".meta")
    guid = read_guid(meta_path) or new_guid()

    kind_index = SKILL_BUFF_KINDS.index(kind)
    content = textwrap.dedent(
        f"""\
        %YAML 1.1
        %TAG !u! tag:yousandi.cn,2023:
        --- !u!114 &11400000
        MonoBehaviour:
          m_ObjectHideFlags: 0
          m_CorrespondingSourceObject: {{fileID: 0}}
          m_PrefabInstance: {{fileID: 0}}
          m_PrefabAsset: {{fileID: 0}}
          m_GameObject: {{fileID: 0}}
          m_Enabled: 1
          m_EditorHideFlags: 0
          m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID}, type: 3}}
          m_Name: {file_name}
          m_EditorClassIdentifier:
          configId: {config_id}
          displayName: {display_name}
          icon: {{fileID: 0}}
          description: {yaml_escape(description)}
          rarity: 0
          weight: 100
          effectType: {effect_type}
          maxStacks: {max_stacks}
          minWave: 1
          maxWave: 0
          mutuallyExclusiveGroup:
          prerequisiteOptionIds: []
          buffConfigId:
          buffStacks: 1
          skillConfigId:
          skillLevelDelta: 1
          skillBuffKind: {kind_index}
          skillBuffTier: {tier}
          resourceAmount: 10
          directModifiers: []
        """
    )
    asset_path.write_text(content, encoding="utf-8")
    if not meta_path.exists():
        write_meta(asset_path, guid)
    return guid


def collect_skill_buff_specs() -> list[tuple[str, str, str, str, str, str, int]]:
    specs: list[tuple[str, str, str, str, str, str, int]] = []
    for kind in SKILL_BUFF_KINDS:
        if kind == "None":
            continue
        max_tier = get_max_tier(kind)
        if max_tier <= 1:
            continue
        for tier in range(1, max_tier + 1):
            file_name = build_file_name(kind, tier)
            config_id = build_config_id(kind, tier)
            display_name = build_display_name(kind, tier)
            description = build_description(kind, tier)
            specs.append((file_name, config_id, display_name, description, kind, file_name, tier))
    return specs


def update_pool_entries(option_guids: list[str]) -> None:
    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:yousandi.cn,2023:",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: {fileID: 0}",
        "  m_Enabled: 1",
        "  m_EditorHideFlags: 0",
        f"  m_Script: {{fileID: 11500000, guid: {POOL_SCRIPT_GUID}, type: 3}}",
        "  m_Name: RewardPool_Default",
        "  m_EditorClassIdentifier:",
        "  configId: reward_pool.default",
        "  displayName: Default Reward Pool",
        "  icon: {fileID: 0}",
        "  choiceCount: 3",
        "  minWave: 1",
        "  maxWave: 0",
        "  entries:",
    ]
    for guid in option_guids:
        lines.append(f"  - option: {{fileID: 11400000, guid: {guid}, type: 2}}")
        lines.append("    weightOverride: -1")
    lines.append("  blockedMutualGroups: []")
    lines.append("")
    POOL_ASSET.write_text("\n".join(lines), encoding="utf-8")


def update_config_database(option_guids: list[str]) -> None:
    text = CONFIG_DB.read_text(encoding="utf-8")
    block = "  upgradeOptions:\n" + "".join(
        f"  - {{fileID: 11400000, guid: {guid}, type: 2}}\n" for guid in option_guids
    )
    text = re.sub(r"  upgradeOptions:\n(?:  - .*\n)*", block, text, count=1)
    CONFIG_DB.write_text(text, encoding="utf-8")


def main() -> None:
    UPGRADE_DIR.mkdir(parents=True, exist_ok=True)
    option_guids: list[str] = []

    attack_guid = read_guid(UPGRADE_DIR / "UpgradeOption_AttackUp.asset.meta")
    if attack_guid:
        option_guids.append(attack_guid)

    for spec in collect_skill_buff_specs():
        file_name, config_id, display_name, description, kind, _, tier = spec
        guid = write_upgrade_asset(
            file_name,
            config_id,
            display_name,
            description,
            kind,
            tier,
        )
        option_guids.append(guid)

    for file_name, _ in UNLOCK_OPTIONS:
        guid = read_guid(UPGRADE_DIR / f"{file_name}.asset.meta")
        if guid:
            option_guids.append(guid)

    pool_guid = read_guid(POOL_ASSET.with_suffix(".asset.meta"))
    if pool_guid:
        option_guids_for_db = list(option_guids)
        update_pool_entries(option_guids)
        update_config_database(option_guids_for_db)

    created = len(collect_skill_buff_specs())
    print(f"Generated/updated {created} multi-tier skill buff upgrade options.")
    print(f"RewardPool_Default entries: {len(option_guids)} (+ unlock/stat cards).")


if __name__ == "__main__":
    main()
