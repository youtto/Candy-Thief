"""Prepare BG inpaint mask, thief/guard crops, and logo cleanup. Does not touch v001/v002 sources."""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

ROOT = Path(r"c:\Users\mooomoocat\Desktop\Candy Thief\Docs\Art\Source\Scenario")
SRC = ROOT / "ART-TTL-002_CT_Title_Day_v002_source.png"
LOGO_IN = ROOT / "_local_split_fail_v002" / "CT_Title_Logo_v002.png"
OUT = ROOT / "_ttl002_work"
OUT.mkdir(exist_ok=True)

rgb = np.array(Image.open(SRC).convert("RGB"))
h, w = rgb.shape[:2]
lum = (0.2126 * rgb[:, :, 0] + 0.7152 * rgb[:, :, 1] + 0.0722 * rgb[:, :, 2]).astype(np.float32)
rch, gch, bch = rgb[:, :, 0].astype(np.int16), rgb[:, :, 1].astype(np.int16), rgb[:, :, 2].astype(np.int16)


def in_box(box):
    x0, y0, x1, y1 = box
    m = np.zeros((h, w), dtype=bool)
    m[y0:y1, x0:x1] = True
    return m


def keep_largest(mask, n=1):
    lab, cnt = ndimage.label(mask)
    if cnt == 0:
        return mask
    areas = ndimage.sum(mask, lab, index=list(range(1, cnt + 1)))
    order = np.argsort(areas)[::-1]
    keep = np.zeros_like(mask)
    for i in order[:n]:
        keep |= lab == (i + 1)
    return keep


def outline_blob(box, lum_max=64, close_iter=5, dilate=3):
    roi = in_box(box)
    dark = (lum < lum_max) & roi
    closed = ndimage.binary_dilation(ndimage.binary_closing(dark, iterations=close_iter), iterations=dilate)
    filled = keep_largest(ndimage.binary_fill_holes(closed), 1)
    filled = ndimage.binary_dilation(filled, iterations=3)
    return filled & roi


sky = (bch > 180) & (gch > 140) & (rch < 170) & (bch > rch + 20)

# Logo
logo_roi = in_box((190, 50, 915, 515))
logo = keep_largest(ndimage.binary_fill_holes(
    ndimage.binary_dilation(
        ndimage.binary_closing(
            (logo_roi & (~sky) & (lum < 242)) | (logo_roi & (lum > 215) & (bch < 235) & (rch > 180)),
            iterations=2,
        ),
        iterations=2,
    )
), 1)
logo = ndimage.binary_dilation(logo, iterations=2)

# Characters
guard = outline_blob((20, 720, 445, 1310))
thief = outline_blob((440, 740, 1035, 1435))
dust = ndimage.binary_dilation(
    ndimage.binary_opening(
        (lum > 215) & (rch > 195) & (gch > 185) & (bch > 170) & in_box((330, 1080, 800, 1410)),
        iterations=1,
    ),
    iterations=3,
)
ticks = (lum < 55) & in_box((290, 790, 490, 1020))
ticks = ndimage.binary_dilation(ticks, iterations=2) & ndimage.binary_dilation(guard, iterations=24)

edit = logo | guard | thief | dust | ticks
edit = ndimage.binary_dilation(edit, iterations=2)
print("edit pct", round(100 * edit.mean(), 2), "px", int(edit.sum()))
for name, m in (("logo", logo), ("guard", guard), ("thief", thief)):
    ys, xs = np.where(m)
    print(name, int(m.sum()), int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max()))

# Sunburst mask: transparent = edit, opaque = keep
keep_a = np.where(edit, 0, 255).astype(np.uint8)
mask = Image.new("RGBA", (w, h), (0, 0, 0, 255))
mask.putalpha(Image.fromarray(keep_a, "L"))
mask_path = OUT / "ART-TTL-002_v002_bg_edit_mask.png"
mask.save(mask_path)

ov = rgb.copy().astype(np.float32)
ov[edit] = ov[edit] * 0.4 + np.array([255, 40, 110], np.float32) * 0.6
Image.fromarray(ov.astype(np.uint8)).save(OUT / "ART-TTL-002_v002_bg_edit_mask_preview.png")

# Crops for 851 — RGB, include full subject, small pad
def save_crop(box, name):
    x0, y0, x1, y1 = box
    im = Image.fromarray(rgb[y0:y1, x0:x1], "RGB")
    p = OUT / name
    im.save(p)
    print("crop", name, im.size, p.stat().st_size)
    return p

guard_crop = save_crop((18, 728, 448, 1298), "CT_Title_Guard_crop_v002.png")
thief_crop = save_crop((438, 748, 1028, 1428), "CT_Title_Thief_crop_v002.png")

# Logo cleanup: close white-halo nicks, keep RGB of original logo
logo_im = Image.open(LOGO_IN).convert("RGBA")
la = np.array(logo_im)
alpha = la[:, :, 3]
# visible
vis = alpha > 40
# white-ish outline ring
white = (la[:, :, 0] > 210) & (la[:, :, 1] > 210) & (la[:, :, 2] > 210) & vis
# dilate white ring then clip to a 3px band outside current vis, fill with white
ring = ndimage.binary_dilation(white, iterations=3) & (~ndimage.binary_erosion(vis, iterations=1))
# also close holes in alpha
closed = ndimage.binary_closing(vis, iterations=2)
new_a = alpha.copy()
new_a[closed] = np.maximum(new_a[closed], 255)
# where we added halo, paint white RGB
add = closed & (alpha < 40)
la[add, 0] = 255
la[add, 1] = 255
la[add, 2] = 255
la[add, 3] = 255
# fill tiny interior holes (alpha 0 inside vis hull)
hull = ndimage.binary_fill_holes(closed)
tiny = hull & (alpha < 20) & (~white)
# don't fill large transparent interior — logo has none
# only pixels completely enclosed
if tiny.sum() < 800:
    la[tiny, 3] = 255
    # sample nearby rgb
    ys, xs = np.where(tiny)
    for y, x in zip(ys, xs):
        la[y, x, :3] = (255, 255, 255)
la[:, :, 3] = np.where(hull, 255, 0)
out_logo = Image.fromarray(la, "RGBA")
# trim
bbox = out_logo.getbbox()
out_logo = out_logo.crop(bbox)
logo_path = OUT / "CT_Title_Logo_v002.png"
out_logo.save(logo_path)
print("logo", out_logo.size, logo_path.stat().st_size)
print("mask bytes", mask_path.stat().st_size)
print("done", OUT)
