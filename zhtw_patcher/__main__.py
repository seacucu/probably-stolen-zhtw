"""Entry point. No arguments: open the GUI. CLI: install|restore|status [--game DIR] [--force]."""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from zhtw_patcher import core


def cli(argv: list[str]) -> int:
    p = argparse.ArgumentParser(prog="zhtw_patcher", description="Probably Stolen 繁體中文模組")
    p.add_argument("action", choices=("install", "restore", "status"))
    p.add_argument("--game", type=Path, help="遊戲資料夾（預設自動搜尋 Steam 收藏庫）")
    p.add_argument("--force", action="store_true", help="遊戲版本不符時仍嘗試安裝")
    args = p.parse_args(argv)
    game = args.game or core.find_game_dir()
    if game is None or not core.is_game_dir(game):
        print("找不到遊戲資料夾，請用 --game 指定。", file=sys.stderr)
        return 2
    try:
        if args.action == "status":
            print(core.status(game).message)
        elif args.action == "install":
            core.install(game, force=args.force)
        else:
            core.restore(game)
    except core.PatchError as e:
        print(f"錯誤：{e}", file=sys.stderr)
        return 1
    return 0


def main() -> None:
    if len(sys.argv) > 1:
        if hasattr(sys.stdout, "reconfigure") and sys.stdout:
            sys.stdout.reconfigure(encoding="utf-8")
        sys.exit(cli(sys.argv[1:]))
    from zhtw_patcher.gui import main as gui_main
    gui_main()


if __name__ == "__main__":
    main()
