#!/usr/bin/env python3
"""Fix invalid Unity GUIDs in Upgrade assets and update all YAML references."""

from __future__ import annotations

import re
import secrets
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UPGRADE_DIR = ROOT / "Assets/Resources/Config/Upgrade"
ASSETS_ROOT = ROOT / "Assets"

HEX_GUID = re.compile(r"^[0-9a-f]{32}$")
META_GUID = re.compile(r"^(guid: )(.+)$", re.MULTILINE)
REF_GUID = re.compile(r"guid: ([^,\s}]+)")


def new_hex_guid() -> str:
    return secrets.token_hex(16)


def fix_upgrade_meta_files() -> dict[str, str]:
    """Return mapping old_guid -> new_guid for changed meta files."""
    mapping: dict[str, str] = {}

    for meta_path in sorted(UPGRADE_DIR.glob("*.meta")):
        text = meta_path.read_text(encoding="utf-8")
        match = META_GUID.search(text)
        if not match:
            continue

        old_guid = match.group(2).strip()
        if HEX_GUID.match(old_guid):
            continue

        new_guid = new_hex_guid()
        while new_guid in mapping.values():
            new_guid = new_hex_guid()

        mapping[old_guid] = new_guid
        updated = META_GUID.sub(rf"\g<1>{new_guid}", text, count=1)
        meta_path.write_text(updated, encoding="utf-8")
        print(f"fixed meta: {meta_path.name} {old_guid[:24]}... -> {new_guid}")

    return mapping


def replace_guids_in_assets(mapping: dict[str, str]) -> int:
    if not mapping:
        return 0

    replacements = 0
    for asset_path in ASSETS_ROOT.rglob("*.asset"):
        text = asset_path.read_text(encoding="utf-8")
        original = text
        for old, new in mapping.items():
            text = text.replace(old, new)
        if text != original:
            asset_path.write_text(text, encoding="utf-8")
            replacements += 1
            print(f"updated refs: {asset_path.relative_to(ROOT)}")
    return replacements


def rebuild_config_database_upgrade_options() -> None:
    """Rewrite upgradeOptions list using hex GUIDs from Upgrade/*.meta."""
    db_path = ROOT / "Assets/Resources/Config/ConfigDatabase.asset"
    text = db_path.read_text(encoding="utf-8")

    guids: list[str] = []
    for asset in sorted(UPGRADE_DIR.glob("UpgradeOption_*.asset")):
        meta_path = asset.with_suffix(asset.suffix + ".meta")
        if not meta_path.exists():
            continue
        match = META_GUID.search(meta_path.read_text(encoding="utf-8"))
        if not match:
            continue
        guid = match.group(2).strip()
        if HEX_GUID.match(guid):
            guids.append(guid)

    block = "  upgradeOptions:\n" + "".join(
        f"  - {{fileID: 11400000, guid: {guid}, type: 2}}\n" for guid in guids
    )
    text = re.sub(r"  upgradeOptions:\n(?:  - .*\n)*", block, text, count=1)
    db_path.write_text(text, encoding="utf-8")
    print(f"rebuilt ConfigDatabase upgradeOptions: {len(guids)} entries")


def rebuild_reward_pool() -> None:
    """Rewrite RewardPool_Default entries (AttackUp first, tier options, unlock last)."""
    pool_path = UPGRADE_DIR / "RewardPool_Default.asset"
    meta_path = pool_path.with_suffix(pool_path.suffix + ".meta")
    pool_guid_match = META_GUID.search(meta_path.read_text(encoding="utf-8"))
    pool_script = "165cc72cc3f594281af4630af166481f"

    # Order: AttackUp, all skill buff tiers (exclude unlock), unlock cards
    unlock_names = {
        "UpgradeOption_UnlockLightning",
        "UpgradeOption_UnlockThunder",
        "UpgradeOption_UnlockHeal",
        "UpgradeOption_UnlockIce",
        "UpgradeOption_UnlockFireRain",
        "UpgradeOption_UnlockWaterWave",
    }

    def read_guid(name: str) -> str | None:
        meta = UPGRADE_DIR / f"{name}.asset.meta"
        if not meta.exists():
            return None
        m = META_GUID.search(meta.read_text(encoding="utf-8"))
        return m.group(2).strip() if m else None

    ordered: list[str] = []
    attack = read_guid("UpgradeOption_AttackUp")
    if attack:
        ordered.append(attack)

    for asset in sorted(UPGRADE_DIR.glob("UpgradeOption_*.asset")):
        stem = asset.stem
        if stem == "UpgradeOption_AttackUp" or stem in unlock_names:
            continue
        g = read_guid(stem)
        if g:
            ordered.append(g)

    for name in sorted(unlock_names):
        g = read_guid(name)
        if g:
            ordered.append(g)

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
        f"  m_Script: {{fileID: 11500000, guid: {pool_script}, type: 3}}",
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
    for guid in ordered:
        lines.append(f"  - option: {{fileID: 11400000, guid: {guid}, type: 2}}")
        lines.append("    weightOverride: -1")
    lines.append("  blockedMutualGroups: []")
    lines.append("")
    pool_path.write_text("\n".join(lines), encoding="utf-8")
    print(f"rebuilt RewardPool_Default: {len(ordered)} entries")


def verify_no_invalid_refs() -> list[str]:
    issues: list[str] = []
    for asset_path in ASSETS_ROOT.rglob("*.asset"):
        for guid in REF_GUID.findall(asset_path.read_text(encoding="utf-8")):
            if not HEX_GUID.match(guid) and len(guid) < 32:
                issues.append(f"{asset_path.relative_to(ROOT)}: {guid}")
    return issues


def main() -> None:
    mapping = fix_upgrade_meta_files()
    replace_guids_in_assets(mapping)
    rebuild_config_database_upgrade_options()
    rebuild_reward_pool()

    issues = verify_no_invalid_refs()
    if issues:
        print(f"\nWARNING: {len(issues)} potentially invalid GUID refs remain:")
        for item in issues[:30]:
            print(f"  {item}")
    else:
        print("\nAll scanned asset GUID references look valid (32-char hex).")


if __name__ == "__main__":
    main()
