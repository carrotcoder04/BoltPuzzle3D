#!/usr/bin/env python3
"""Cập nhật mục "Level gallery" trong README.md từ ảnh chụp bằng cheat (Screenshots/Levels/Level_XXX.png).

Dùng:  python3 tools/update_level_gallery.py
- Thu nhỏ từng ảnh về rộng 240px, lưu vào Docs/levels/level_XXX.jpg (cần Pillow: pip3 install pillow;
  không có Pillow thì dùng `sips` trên macOS).
- Viết lại bảng ảnh giữa hai dấu <!-- LEVELS:START --> và <!-- LEVELS:END --> trong README.md.
"""
import glob, os, re, shutil, subprocess, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Screenshots", "Levels")
DST = os.path.join(ROOT, "Docs", "levels")
README = os.path.join(ROOT, "README.md")
WIDTH, COLS = 240, 5


def thumb(src, dst):
    try:
        from PIL import Image
        im = Image.open(src).convert("RGB")
        im.thumbnail((WIDTH, WIDTH * 3), Image.LANCZOS)
        im.save(dst, quality=82, optimize=True)
    except ImportError:
        if shutil.which("sips"):
            subprocess.run(["sips", "-s", "format", "jpeg", "--resampleWidth", str(WIDTH), src, "--out", dst],
                           check=True, stdout=subprocess.DEVNULL)
        else:
            sys.exit("Cần Pillow (pip3 install pillow) hoặc sips (macOS).")


def main():
    files = sorted(glob.glob(os.path.join(SRC, "Level_*.png")))
    if not files:
        sys.exit("Chưa có ảnh trong Screenshots/Levels. Vào màn Game trong Editor, bấm All trên thanh cheat.")
    os.makedirs(DST, exist_ok=True)
    cells = []
    for f in files:
        n = int(re.search(r"(\d+)", os.path.basename(f)).group(1))
        out = os.path.join(DST, "level_%03d.jpg" % n)
        thumb(f, out)
        cells.append('<td align="center"><img src="Docs/levels/level_%03d.jpg" width="150"><br><sub>Level %d</sub></td>' % (n, n))
    rows = ["<tr>" + "".join(cells[i:i + COLS]) + "</tr>" for i in range(0, len(cells), COLS)]
    block = "<!-- LEVELS:START -->\n<table>\n" + "\n".join(rows) + "\n</table>\n<!-- LEVELS:END -->"
    text = open(README, encoding="utf-8").read()
    text = re.sub(r"<!-- LEVELS:START -->.*?<!-- LEVELS:END -->", lambda m: block, text, flags=re.S)
    open(README, "w", encoding="utf-8").write(text)
    print("Đã cập nhật %d level vào README.md" % len(cells))


if __name__ == "__main__":
    main()
