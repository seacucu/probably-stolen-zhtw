"""Small Tkinter front end: pick the game folder, install or restore."""
from __future__ import annotations

import queue
import threading
import tkinter as tk
from pathlib import Path
from tkinter import filedialog, messagebox, scrolledtext, ttk

from . import core

TITLE = f"Probably Stolen 繁體中文模組 v{core.PATCH_VERSION}"


class App(tk.Tk):
    def __init__(self) -> None:
        super().__init__()
        self.title(TITLE)
        self.geometry("620x420")
        self.minsize(520, 360)
        self.msgs: queue.Queue = queue.Queue()
        self.busy = False

        frm = ttk.Frame(self, padding=12)
        frm.pack(fill="both", expand=True)

        ttk.Label(frm, text="遊戲資料夾：").grid(row=0, column=0, sticky="w")
        self.path = tk.StringVar(value=str(core.find_game_dir() or ""))
        ttk.Entry(frm, textvariable=self.path).grid(row=0, column=1, sticky="ew", padx=6)
        ttk.Button(frm, text="瀏覽…", command=self.browse).grid(row=0, column=2)

        self.state = tk.StringVar()
        ttk.Label(frm, textvariable=self.state).grid(row=1, column=0, columnspan=3, sticky="w", pady=(8, 4))

        btns = ttk.Frame(frm)
        btns.grid(row=2, column=0, columnspan=3, sticky="w", pady=4)
        self.btn_install = ttk.Button(btns, text="安裝／重新安裝", command=self.do_install)
        self.btn_install.pack(side="left")
        self.btn_restore = ttk.Button(btns, text="還原原版", command=self.do_restore)
        self.btn_restore.pack(side="left", padx=8)
        self.force = tk.BooleanVar()
        ttk.Checkbutton(btns, text="強制安裝（遊戲版本不符時）", variable=self.force).pack(side="left", padx=8)

        self.log = scrolledtext.ScrolledText(frm, height=14, state="disabled")
        self.log.grid(row=3, column=0, columnspan=3, sticky="nsew", pady=(6, 0))
        frm.columnconfigure(1, weight=1)
        frm.rowconfigure(3, weight=1)

        self.path.trace_add("write", lambda *_: self.refresh())
        self.refresh()
        self.after(100, self.pump)

    def write(self, text: str) -> None:
        self.msgs.put(text)

    def pump(self) -> None:
        while not self.msgs.empty():
            item = self.msgs.get()
            if item is None:
                self.busy = False
                self.refresh()
                continue
            self.log.configure(state="normal")
            self.log.insert("end", item + "\n")
            self.log.see("end")
            self.log.configure(state="disabled")
        self.after(100, self.pump)

    def browse(self) -> None:
        d = filedialog.askdirectory(title="選擇 Probably Stolen 遊戲資料夾（含 Probably Stolen.exe）")
        if d:
            self.path.set(d)

    def refresh(self) -> None:
        game = Path(self.path.get())
        ok = bool(self.path.get()) and core.is_game_dir(game)
        if not ok:
            self.state.set("狀態：請選擇含有「Probably Stolen.exe」的遊戲資料夾")
        else:
            try:
                self.state.set("狀態：" + core.status(game).message)
            except OSError as e:
                self.state.set(f"狀態：無法讀取遊戲檔案（{e}）")
        for b in (self.btn_install, self.btn_restore):
            b.configure(state="normal" if ok and not self.busy else "disabled")

    def run(self, job) -> None:
        self.busy = True
        self.refresh()

        def worker() -> None:
            try:
                job()
            except core.PatchError as e:
                self.write(f"錯誤：{e}")
                self.after(0, lambda: messagebox.showerror(TITLE, str(e)))
            except PermissionError:
                msg = "沒有寫入遊戲資料夾的權限。請對本程式按右鍵，選擇「以系統管理員身分執行」。"
                self.write(f"錯誤：{msg}")
                self.after(0, lambda: messagebox.showerror(TITLE, msg))
            except Exception as e:  # unexpected: show it so users can report it
                self.write(f"未預期的錯誤：{type(e).__name__}: {e}")
                self.after(0, lambda: messagebox.showerror(TITLE, f"{type(e).__name__}: {e}"))
            finally:
                self.msgs.put(None)

        threading.Thread(target=worker, daemon=True).start()

    def do_install(self) -> None:
        game = Path(self.path.get())
        self.run(lambda: core.install(game, self.write, force=self.force.get()))

    def do_restore(self) -> None:
        game = Path(self.path.get())
        self.run(lambda: core.restore(game, self.write))


def main() -> None:
    App().mainloop()
