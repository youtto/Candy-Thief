"""Place title layers onto 1080x1920 and export production copies."""
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

work = Path(__file__).resolve().parent
src = work.parent
assets = Path(r"c:\Users\mooomoocat\Desktop\Candy Thief\CandyThief\Assets\Art\Title")
docs = src

THIEF_CROP = (438, 748)
GUARD_CROP = (18, 728)
PAD = 4


def connected_labels(alpha, thresh=8):
    h, w = alpha.shape
    lab = np.zeros((h, w), np.int32)
    n = 0
    neigh = ((1, 0), (-1, 0), (0, 1), (0, -1))
    for y in range(h):
        row = alpha[y]
        for x in range(w):
            if row[x] <= thresh or lab[y, x]:
                continue
            n += 1
            q = deque([(x, y)])
            lab[y, x] = n
            while q:
                cx, cy = q.popleft()
                for dx, dy in neigh:
                    nx, ny = cx + dx, cy + dy
                    if 0 <= nx < w and 0 <= ny < h and lab[ny, nx] == 0 and alpha[ny, nx] > thresh:
                        lab[ny, nx] = n
                        q.append((nx, ny))
    return lab, n


def drop_left_small(img, cx_max=80, area_max=25000):
    arr = np.array(img)
    lab, n = connected_labels(arr[:, :, 3])
    keep = np.ones(n + 1, dtype=bool)
    keep[0] = False
    dropped = []
    for i in range(1, n + 1):
        ys, xs = np.where(lab == i)
        area = xs.size
        cx = float(xs.mean())
        if cx < cx_max and area < area_max:
            keep[i] = False
            dropped.append((i, area, cx, int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())))
    mask = keep[lab]
    arr[~mask] = 0
    return Image.fromarray(arr), dropped


def trim_pad(img, pad=PAD):
    arr = np.array(img)
    ys, xs = np.where(arr[:, :, 3] > 8)
    x0, y0, x1, y1 = int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())
    x0 = max(0, x0 - pad)
    y0 = max(0, y0 - pad)
    x1 = min(arr.shape[1] - 1, x1 + pad)
    y1 = min(arr.shape[0] - 1, y1 + pad)
    cropped = Image.fromarray(arr[y0 : y1 + 1, x0 : x1 + 1])
    return cropped, (x0, y0, x1 - x0 + 1, y1 - y0 + 1)


def logo_pos(v002_rgb, logo_arr):
    scale = 4
    vis = logo_arr[::scale, ::scale, 3] > 40
    rgb = logo_arr[::scale, ::scale, :3]
    vsmall = v002_rgb[::scale, ::scale]
    ch, cw = vis.shape
    H, W = vsmall.shape[:2]
    best = None
    for y in range(8, 40):
        for x in range(30, 80):
            if y + ch > H or x + cw > W:
                continue
            patch = vsmall[y : y + ch, x : x + cw]
            d = np.abs(patch[vis].astype(np.int16) - rgb[vis].astype(np.int16)).mean()
            if best is None or d < best[0]:
                best = (float(d), x * scale, y * scale)
    vis_f = logo_arr[:, :, 3] > 40
    rgb_f = logo_arr[:, :, :3]
    chf, cwf = vis_f.shape
    Hf, Wf = v002_rgb.shape[:2]
    bx, by = best[1], best[2]
    refine = None
    for y in range(max(0, by - 6), by + 7):
        for x in range(max(0, bx - 6), bx + 7):
            if y + chf > Hf or x + cwf > Wf:
                continue
            patch = v002_rgb[y : y + chf, x : x + cwf]
            d = np.abs(patch[vis_f].astype(np.int16) - rgb_f[vis_f].astype(np.int16)).mean()
            if refine is None or d < refine[0]:
                refine = (float(d), x, y)
    return refine


def paste_rgba(base, sprite, xy):
    x, y = xy
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    layer.paste(sprite, (x, y), sprite)
    return Image.alpha_composite(base, layer)


thief_851 = Image.open(work / "CT_Title_Thief_851.png")
guard_851 = Image.open(work / "CT_Title_Guard_851.png")
logo = Image.open(work / "CT_Title_Logo_v002.png")
bg = Image.open(work / "CT_Title_Background_v002.png").convert("RGBA")
v002 = Image.open(docs / "ART-TTL-002_CT_Title_Day_v002_source.png").convert("RGBA")
v002_rgb = np.array(v002.convert("RGB"))

thief_dropped, thief_drop = drop_left_small(thief_851)
thief_img, thief_box = trim_pad(thief_dropped)
guard_img, guard_box = trim_pad(guard_851.convert("RGBA"))

thief_xy = (THIEF_CROP[0] + thief_box[0], THIEF_CROP[1] + thief_box[1])
guard_xy = (GUARD_CROP[0] + guard_box[0], GUARD_CROP[1] + guard_box[1])
logo_arr = np.array(logo)
logo_match = logo_pos(v002_rgb, logo_arr)
logo_xy = (logo_match[1], logo_match[2])

print("thief drop", thief_drop)
print("thief size", thief_img.size, "box", thief_box, "xy", thief_xy)
print("guard size", guard_img.size, "box", guard_box, "xy", guard_xy)
print("logo size", logo.size, "match", logo_match, "xy", logo_xy)

# Sanity: saved cleaned files should match recomputed
saved_thief = Image.open(work / "CT_Title_Thief_v002.png")
saved_guard = Image.open(work / "CT_Title_Guard_v002.png")
print("saved thief", saved_thief.size, "recomputed", thief_img.size)
print("saved guard", saved_guard.size, "recomputed", guard_img.size)

comp = bg.copy()
comp = paste_rgba(comp, guard_img, guard_xy)
comp = paste_rgba(comp, thief_img, thief_xy)
comp = paste_rgba(comp, logo, logo_xy)

comp_path = work / "CT_Title_v002_layered_composite.png"
comp.save(comp_path)
v002.save(work / "_v002_rgba.png")

# Side-by-side compare
w, h = 1080, 1920
compare = Image.new("RGB", (w * 2 + 24, h + 80), (32, 32, 36))
compare.paste(v002.convert("RGB"), (0, 64))
compare.paste(comp.convert("RGB"), (w + 24, 64))
draw = ImageDraw.Draw(compare)
draw.text((24, 16), "v002 plate", fill=(240, 240, 240))
draw.text((w + 48, 16), "Unity layers composite (BG+Guard+Thief+Logo)", fill=(240, 240, 240))
compare_path = docs / "ART-TTL-002_v002_vs_layered_compare.png"
compare.save(compare_path)
print("saved", comp_path)
print("saved", compare_path)

# Diff MAD on opaque character regions vs v002
comp_rgb = np.array(comp.convert("RGB"))
mad = np.abs(comp_rgb.astype(np.int16) - v002_rgb.astype(np.int16)).mean()
print("full MAD vs v002", float(mad))

# Copy production files
assets.mkdir(parents=True, exist_ok=True)
copies = {
    "CT_Title_Background_v002.png": bg,
    "CT_Title_Thief_v002.png": thief_img,
    "CT_Title_Guard_v002.png": guard_img,
    "CT_Title_Logo_v002.png": logo,
    "CT_Title_NicknameFrame_v001.png": Image.open(work / "CT_Title_NicknameFrame_v001.png"),
    "CT_Title_StartButton_v001.png": Image.open(work / "CT_Title_StartButton_v001.png"),
}
for name, img in copies.items():
    docs_path = docs / name
    img.save(docs_path)
    img.save(assets / name)
    print("copied", name, img.size)

layout = {
    "thief": {"x": thief_xy[0], "y": thief_xy[1], "w": thief_img.size[0], "h": thief_img.size[1]},
    "guard": {"x": guard_xy[0], "y": guard_xy[1], "w": guard_img.size[0], "h": guard_img.size[1]},
    "logo": {"x": logo_xy[0], "y": logo_xy[1], "w": logo.size[0], "h": logo.size[1]},
}
print("LAYOUT", layout)
