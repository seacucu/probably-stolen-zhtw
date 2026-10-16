"""Core install / restore logic for the Probably Stolen Demo Traditional Chinese (Taiwan) patch.

The patch only modifies files on the player's own machine:
  * the zh string-table bundle         -> Traditional Chinese text
  * aa/catalog.json                    -> CRC of that bundle zeroed so the edited bundle loads
  * sharedassets0.assets               -> CJK fonts swapped for TC fonts, western fonts get a CJK fallback
  * level0                             -> language dropdown label "Simplified Chinese" -> "繁體中文"

Every original file is copied to <game>/zhTW_backup/ before anything is written, and each patch
step always starts from those backups, so installing twice or after a failed run is safe.
"""
from __future__ import annotations

import hashlib
import json
import os
import shutil
import struct
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Callable, Optional

import UnityPy
from UnityPy.helpers.TypeTreeHelper import TypeTreeNode

PATCH_VERSION = "1.0.0"
DATA_FOLDER = "Probably Stolen_Data"
EXE_NAME = "Probably Stolen.exe"
BACKUP_DIR = "zhTW_backup"
GAME_FOLDER_NAMES = ("Probably Stolen Demo", "Probably Stolen")

AA = Path("StreamingAssets/aa")
ZH_BUNDLE = AA / "StandaloneWindows64/localization-string-tables-chinese(simplified)(zh)_assets_all.bundle"
SHARED_BUNDLE = AA / "StandaloneWindows64/localization-assets-shared_assets_all.bundle"
CATALOG = AA / "catalog.json"
SHAREDASSETS = Path("sharedassets0.assets")
LEVEL0 = Path("level0")
GLOBALGAMEMANAGERS = Path("globalgamemanagers.assets")

# Files this patch writes (relative to the data folder).
TARGETS = (ZH_BUNDLE, CATALOG, SHAREDASSETS, LEVEL0)

# SHA-256 of the original files of the build this patch was made for (demo, Unity 2021.3.45f2).
KNOWN_ORIGINALS = {
    ZH_BUNDLE.as_posix(): "1b7b1fa09806ed1f9edcde30ba3d51b96a8ea08bed39e26dcbb1d9996dabdeac",
    CATALOG.as_posix(): "f179b45c6fb29c41170d39ddcd05987c0df90bd9be4003b99d2fb4fe8b73800c",
    SHAREDASSETS.as_posix(): "4286db8d54e9756271fb9177ba431caef985a57c94bee298e7cb1406af75d91d",
    LEVEL0.as_posix(): "1050c451011c051795ebcb0ec159031d612dc0044963497ecc453a830c06398c",
}

# Font objects (by name) -> replacement font file in data/fonts.
FONT_SWAPS = {
    "NotoSansSC-VariableFont_wght": ("TaipeiSansTCBeta-Regular.ttf", "Taipei Sans TC Beta"),
    "WDXLLubrifontSC-Regular": ("WDXLLubrifontTC-Regular.ttf", "WDXL Lubrifont TC"),
    "MaShanZheng-Regular": ("LXGWWenKaiTC-Regular.ttf", "LXGW WenKai TC"),
}
# TMP font assets whose pre-baked SC glyphs must be cleared so glyphs regenerate from the TC fonts.
TMP_RESET = ("NotoSansSC-VariableFont_wght", "WDXLLubrifontSC-Regular SDF", "MaShanZheng-Regular SDF")
# The CJK TMP asset used as fallback for the western fonts below.
CJK_FALLBACK = "NotoSansSC-VariableFont_wght"
# Western TMP assets used directly by some UI (language dropdown, main-menu buttons).
WESTERN_TMP = ("NotoSans-Bold SDF", "NotoSans-Medium SDF", "NotoSans-VariableFont_wdth,wght SDF",
               "NotoSerif-Medium SDF", "NotoSans-BoldItalic SDF", "NotoSans-MediumItalic SDF")
DROPDOWN_OLD, DROPDOWN_NEW = "Simplified Chinese", "繁體中文"

Log = Callable[[str], None]


class PatchError(Exception):
    pass


def resource_dir() -> Path:
    """data/ folder, both from source and inside a PyInstaller bundle."""
    base = Path(getattr(sys, "_MEIPASS", Path(__file__).resolve().parent.parent))
    return base / "data"


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def _nodes(cls: str) -> TypeTreeNode:
    data = json.loads((resource_dir() / "typetrees.json").read_text(encoding="utf-8"))
    return TypeTreeNode.from_list([TypeTreeNode(l, t, n, 0, 0, m_MetaFlag=f) for l, t, n, f in data[cls]])


# ---------------------------------------------------------------- game discovery

def _steam_libraries() -> list[Path]:
    roots: list[Path] = []
    try:
        import winreg
        for hive, key in ((winreg.HKEY_CURRENT_USER, r"Software\Valve\Steam"),
                          (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\WOW6432Node\Valve\Steam")):
            try:
                with winreg.OpenKey(hive, key) as k:
                    for name in ("SteamPath", "InstallPath"):
                        try:
                            roots.append(Path(winreg.QueryValueEx(k, name)[0]))
                        except OSError:
                            pass
            except OSError:
                pass
    except ImportError:
        pass
    roots.append(Path(r"C:\Program Files (x86)\Steam"))
    libs: list[Path] = []
    for root in roots:
        vdf = root / "steamapps" / "libraryfolders.vdf"
        if vdf.is_file():
            for line in vdf.read_text(encoding="utf-8", errors="ignore").splitlines():
                parts = line.strip().split('"')
                if len(parts) >= 4 and parts[1] == "path":
                    libs.append(Path(parts[3].replace("\\\\", "\\")))
        libs.append(root)
    seen, unique = set(), []
    for lib in libs:
        key = str(lib).lower()
        if key not in seen:
            seen.add(key)
            unique.append(lib)
    return unique


def is_game_dir(path: Path) -> bool:
    return (path / EXE_NAME).is_file() and (path / DATA_FOLDER / ZH_BUNDLE).is_file()


def find_game_dir() -> Optional[Path]:
    for lib in _steam_libraries():
        for name in GAME_FOLDER_NAMES:
            candidate = lib / "steamapps" / "common" / name
            if is_game_dir(candidate):
                return candidate
    return None


def game_running() -> bool:
    try:
        out = subprocess.run(["tasklist", "/FI", f"IMAGENAME eq {EXE_NAME}"], capture_output=True,
                             text=True, creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0)).stdout
        return EXE_NAME.lower() in out.lower()
    except OSError:
        return False


# ---------------------------------------------------------------- status

@dataclass
class Status:
    installed: bool
    version_known: bool
    message: str


def _backup_root(game: Path) -> Path:
    return game / BACKUP_DIR


def status(game: Path) -> Status:
    data = game / DATA_FOLDER
    manifest = _backup_root(game) / "manifest.json"
    if manifest.is_file():
        info = json.loads(manifest.read_text(encoding="utf-8"))
        return Status(True, True, f"已安裝繁中模組 v{info.get('patch_version', '?')}")
    known = all((data / rel).is_file() and sha256(data / rel) == h for rel, h in KNOWN_ORIGINALS.items())
    if known:
        return Status(False, True, "未安裝，遊戲版本相符")
    return Status(False, False, "未安裝；遊戲檔案和本補丁製作時的版本不同（可能已更新）")


# ---------------------------------------------------------------- patch steps

def _write_atomic(path: Path, data: bytes) -> None:
    tmp = path.with_name(path.name + ".zhtw_tmp")
    tmp.write_bytes(data)
    os.replace(tmp, path)


def _patch_strings(src: Path, shared: Path, strings: dict, log: Log) -> bytes:
    keys: dict[str, dict[int, str]] = {}
    for obj in UnityPy.load(str(shared)).objects:
        if obj.type.name == "MonoBehaviour":
            t = obj.read_typetree()
            if "m_TableCollectionName" in t:
                keys[t["m_TableCollectionName"]] = {e["m_Id"]: e["m_Key"] for e in t["m_Entries"]}
    env = UnityPy.load(str(src))
    replaced = total = 0
    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        t = obj.read_typetree()
        if "m_TableData" not in t:
            continue
        table = t["m_Name"].rsplit("_", 1)[0]
        for e in t["m_TableData"]:
            total += 1
            new = strings.get(table, {}).get(keys.get(table, {}).get(e["m_Id"]))
            if new is not None:
                e["m_Localized"] = new
                replaced += 1
        obj.save_typetree(t)
    log(f"  文字：替換 {replaced}/{total} 條")
    if replaced == 0:
        raise PatchError("找不到可替換的字串，遊戲的字串表結構可能已改變。")
    return env.file.save(packer="lz4")


def _patch_catalog(src: Path, original_bundle_size: int, log: Log) -> bytes:
    import base64
    cat = json.loads(src.read_text(encoding="utf-8"))
    raw = bytearray(base64.b64decode(cat["m_ExtraDataString"]))
    u16 = lambda s: s.encode("utf-16-le")
    anchor = raw.find(u16('"m_BundleSize":%d,' % original_bundle_size))
    if anchor < 0:
        raise PatchError("在 catalog.json 中找不到簡中字串表的項目。")
    start = raw.rfind(u16('"m_Crc":'), 0, anchor) + len(u16('"m_Crc":'))
    end = raw.find(u16(","), start)
    digits = raw[start:end].decode("utf-16-le")
    raw[start:end] = u16("0" + " " * (len(digits) - 1))  # same length keeps the length-prefixed blob valid
    cat["m_ExtraDataString"] = base64.b64encode(bytes(raw)).decode()
    log("  catalog：已停用簡中字串表的 CRC 檢查")
    return json.dumps(cat, separators=(",", ":")).encode("utf-8")


def _mono_name(raw: bytes) -> Optional[str]:
    # MonoBehaviour layout: m_GameObject PPtr(12) + m_Enabled(4, aligned) + m_Script PPtr(12) + m_Name
    try:
        n = struct.unpack_from("<i", raw, 28)[0]
        return raw[32:32 + n].decode("utf-8") if 0 < n < 256 else None
    except (struct.error, UnicodeDecodeError):
        return None


def _patch_fonts(src: Path, ggm: Path, log: Log) -> bytes:
    font_node = _nodes("TMPro.TMP_FontAsset")
    env = UnityPy.load(str(src), str(ggm))
    assets = next(v for k, v in env.files.items() if k.lower().endswith(SHAREDASSETS.name))
    fonts_dir = resource_dir() / "fonts"
    swapped = set()
    tmp_by_name = {}
    for obj in assets.objects.values():
        if obj.type.name == "Font":
            t = obj.read_typetree()
            if t["m_Name"] in FONT_SWAPS:
                ttf, family = FONT_SWAPS[t["m_Name"]]
                t["m_FontData"] = list((fonts_dir / ttf).read_bytes())
                t["m_FontNames"] = [family]
                obj.save_typetree(t)
                swapped.add(t["m_Name"])
        elif obj.type.name == "MonoBehaviour":
            name = _mono_name(obj.get_raw_data())
            if name in TMP_RESET or name in WESTERN_TMP:
                tmp_by_name[name] = obj
    missing = (set(FONT_SWAPS) - swapped) | (set(TMP_RESET) - set(tmp_by_name))
    if missing:
        raise PatchError("找不到以下字型資產，遊戲版本可能已改變：" + "、".join(sorted(missing)))
    for name in TMP_RESET:
        obj = tmp_by_name[name]
        t = obj.read_typetree(font_node)
        pad = 0 if (t["m_AtlasRenderMode"] & 0x1) else 1
        t["m_GlyphTable"], t["m_CharacterTable"], t["m_UsedGlyphRects"] = [], [], []
        t["m_FreeGlyphRects"] = [{"m_X": 0, "m_Y": 0, "m_Width": t["m_AtlasWidth"] - pad,
                                  "m_Height": t["m_AtlasHeight"] - pad}]
        t["m_AtlasTextures"] = t["m_AtlasTextures"][:1]
        t["m_AtlasTextureIndex"] = 0
        t["m_FontFeatureTable"]["m_GlyphPairAdjustmentRecords"] = []
        t["m_KerningTable"]["kerningPairs"] = []
        obj.save_typetree(t, font_node)
    fallback = {"m_FileID": 0, "m_PathID": tmp_by_name[CJK_FALLBACK].path_id}
    for name in WESTERN_TMP:
        obj = tmp_by_name.get(name)
        if obj is None:
            continue
        t = obj.read_typetree(font_node)
        if fallback not in t["m_FallbackFontAssetTable"]:
            t["m_FallbackFontAssetTable"].append(fallback)
        obj.save_typetree(t, font_node)
    log(f"  字型：替換 {len(swapped)} 套中文字型，{len(TMP_RESET)} 個字型資產重建字形")
    return assets.save()


def _patch_menu(src: Path, log: Log) -> bytes:
    node = _nodes("TMPro.TMP_Dropdown")
    env = UnityPy.load(str(src))
    count = 0
    for obj in env.file.objects.values():
        if obj.type.name != "MonoBehaviour" or DROPDOWN_OLD.encode() not in obj.get_raw_data():
            continue
        t = obj.read_typetree(node)
        for opt in t["m_Options"]["m_Options"]:
            if opt["m_Text"] == DROPDOWN_OLD:
                opt["m_Text"] = DROPDOWN_NEW
                count += 1
        obj.save_typetree(t, node)
    log(f"  選單：更新 {count} 個語言選項")
    return env.file.save()


# ---------------------------------------------------------------- install / restore

def install(game: Path, log: Log = print, force: bool = False) -> None:
    if not is_game_dir(game):
        raise PatchError(f"這不是《Probably Stolen》的遊戲資料夾：{game}")
    if game_running():
        raise PatchError("遊戲正在執行，請先關閉遊戲。")
    data = game / DATA_FOLDER
    backup = _backup_root(game)
    manifest_path = backup / "manifest.json"
    if manifest_path.is_file():
        log("偵測到先前的安裝，將從備份的原檔重新套用。")
    else:
        st = status(game)
        if not st.version_known and not force:
            raise PatchError(st.message + "。若仍要嘗試，請勾選「強制安裝」。")
        log("備份原始檔案……")
        for rel in TARGETS:
            dst = backup / rel
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(data / rel, dst)
        manifest_path.write_text(json.dumps({
            "patch_version": PATCH_VERSION,
            "originals": {rel.as_posix(): sha256(backup / rel) for rel in TARGETS},
        }, ensure_ascii=False, indent=1), encoding="utf-8")

    strings = json.loads((resource_dir() / "strings.json").read_text(encoding="utf-8"))
    log("套用譯文……")
    zh = _patch_strings(backup / ZH_BUNDLE, data / SHARED_BUNDLE, strings, log)
    cat = _patch_catalog(backup / CATALOG, (backup / ZH_BUNDLE).stat().st_size, log)
    log("替換字型（檔案較大，約需一分鐘）……")
    fonts = _patch_fonts(backup / SHAREDASSETS, data / GLOBALGAMEMANAGERS, log)
    log("更新語言選單……")
    menu = _patch_menu(backup / LEVEL0, log)

    log("寫入遊戲資料夾……")
    for rel, blob in ((ZH_BUNDLE, zh), (CATALOG, cat), (SHAREDASSETS, fonts), (LEVEL0, menu)):
        _write_atomic(data / rel, blob)
    log("安裝完成！啟動遊戲後，在右上角語言選單選擇「繁體中文」。")


def restore(game: Path, log: Log = print) -> None:
    if game_running():
        raise PatchError("遊戲正在執行，請先關閉遊戲。")
    backup = _backup_root(game)
    manifest_path = backup / "manifest.json"
    if not manifest_path.is_file():
        raise PatchError("找不到備份，可能尚未安裝。也可以用 Steam 的「驗證遊戲檔案完整性」還原。")
    info = json.loads(manifest_path.read_text(encoding="utf-8"))
    data = game / DATA_FOLDER
    for rel, digest in info["originals"].items():
        src = backup / rel
        if sha256(src) != digest:
            raise PatchError(f"備份檔案已損毀：{rel}。請改用 Steam 的「驗證遊戲檔案完整性」。")
        shutil.copy2(src, data / rel)
        log(f"  已還原 {rel}")
    shutil.rmtree(backup)
    log("已還原為原版遊戲。")
