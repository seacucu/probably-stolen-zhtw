@echo off
rem Build a single-file Windows executable into dist\
python -m PyInstaller --noconfirm --onefile --windowed --name "ProbablyStolen-zhTW-Patcher" ^
  --add-data "data;data" --collect-all UnityPy --exclude-module TypeTreeGeneratorAPI ^
  --hidden-import zhtw_patcher.gui run_patcher.py
