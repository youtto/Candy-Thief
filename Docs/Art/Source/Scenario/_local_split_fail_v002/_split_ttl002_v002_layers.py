"""Second local split: closed dark-outline fill for characters. Logo keeps color mask."""
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

SRC = Path(r"c:\Users\mooomoocat\Desktop\Candy Thief\Docs\Art\Source\Scenario\ART-TTL-002_CT_Title_Day_v002_source.png")
OUT = SRC.parent
PAD = 10

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


def outline_blob(box, lum_max=62, close_iter=4, dilate=2):
    roi = in_box(box)
    dark = (lum < lum_max) & roi
    closed = ndimage.binary_closing(dark, iterations=close_iter)
    closed = ndimage.binary_dilation(closed, iterations=dilate)
    filled = ndimage.binary_fill_holes(closed)
    blob = keep_largest(filled, 1)
    # grow to original thick outline
    blob = ndimage.binary_dilation(blob, iterations=2)
    blob &= roi
    return blob


sky = (bch > 180) & (gch > 140) & (rch < 170) & (bch > rch + 20)

# Logo (first pass was acceptable)
logo_box = (200, 60, 900, 500)
logo_roi = in_box(logo_box)
logo_core = logo_roi & (~sky) & (lum < 242)
halo = logo_roi & (lum > 215) & (bch < 235) & (rch > 180)
logo = ndimage.binary_closing(logo_core | halo, iterations=2)
logo = ndimage.binary_dilation(logo, iterations=1)
logo = keep_largest(logo, 1)
logo = ndimage.binary_fill_holes(logo)
print("logo", int(logo.sum()), tuple(int(v) for v in (np.where(logo)[1].min(), np.where(logo)[0].min(), np.where(logo)[1].max(), np.where(logo)[0].max())))

guard = outline_blob((30, 740, 460, 1310), lum_max=64, close_iter=5, dilate=3)
print("guard", int(guard.sum()), tuple(int(v) for v in (np.where(guard)[1].min(), np.where(guard)[0].min(), np.where(guard)[1].max(), np.where(guard)[0].max())))

thief = outline_blob((430, 750, 1020, 1420), lum_max=64, close_iter=5, dilate=3)
print("thief", int(thief.sum()), tuple(int(v) for v in (np.where(thief)[1].min(), np.where(thief)[0].min(), np.where(thief)[1].max(), np.where(thief)[0].max())))

# dust puffs (near feet)
dust = (lum > 215) & (rch > 195) & (gch > 185) & (bch > 170)
dust &= in_box((360, 1080, 780, 1400))
dust = ndimage.binary_opening(dust, iterations=1)
dust = ndimage.binary_dilation(dust, iterations=2)
thief = thief | dust

# speed ticks near guard (small dark arcs)
ticks = (lum < 55) & in_box((300, 800, 480, 1000))
ticks = ticks & ndimage.binary_dilation(guard, iterations=22) & (~thief)
guard = guard | ndimage.binary_dilation(ticks, iterations=1)

overlap = guard & thief
print("overlap", int(overlap.sum()))
if overlap.any():
    # sack belongs to thief
    guard = guard & (~overlap)

print("guard2", int(guard.sum()), tuple(int(v) for v in (np.where(guard)[1].min(), np.where(guard)[0].min(), np.where(guard)[1].max(), np.where(guard)[0].max())))
print("thief2", int(thief.sum()), tuple(int(v) for v in (np.where(thief)[1].min(), np.where(thief)[0].min(), np.where(thief)[1].max(), np.where(thief)[0].max())))


def crop_rgba(mask, pad=PAD):
    ys, xs = np.where(mask)
    x0, x1 = max(0, int(xs.min()) - pad), min(w, int(xs.max()) + pad + 1)
    y0, y1 = max(0, int(ys.min()) - pad), min(h, int(ys.max()) + pad + 1)
    core = mask[y0:y1, x0:x1]
    alpha = (core.astype(np.uint8) * 255)
    aimg = Image.fromarray(alpha, "L").filter(ImageFilter.GaussianBlur(radius=0.5))
    a = np.array(aimg)
    a[core] = 255
    a[~ndimage.binary_dilation(core, iterations=2)] = 0
    layer = np.zeros((y1 - y0, x1 - x0, 4), dtype=np.uint8)
    layer[:, :, :3] = rgb[y0:y1, x0:x1]
    layer[:, :, 3] = a
    layer[a == 0] = 0
    return Image.fromarray(layer, "RGBA"), (x0, y0, x1, y1)


def inpaint(hole):
    filled = rgb.copy()
    known = ~hole
    for y in range(h):
        row_h = hole[y]
        if not row_h.any():
            continue
        row_k = known[y]
        if not row_k.any():
            continue
        xs_k = np.where(row_k)[0]
        xs_h = np.where(row_h)[0]
        idx = np.searchsorted(xs_k, xs_h)
        idx = np.clip(idx, 0, len(xs_k) - 1)
        idx_l = np.clip(idx - 1, 0, len(xs_k) - 1)
        dl = np.abs(xs_k[idx_l] - xs_h)
        dr = np.abs(xs_k[idx] - xs_h)
        pick = np.where(dl <= dr, xs_k[idx_l], xs_k[idx])
        filled[y, xs_h] = rgb[y, pick]
    dist, inds = ndimage.distance_transform_edt(hole, return_indices=True)
    empty_rows = hole.all(axis=1)
    if empty_rows.any():
        filled[empty_rows] = rgb[inds[0][empty_rows], inds[1][empty_rows]]
    # extra: for sky hole, prefer sampling sky pixels at same y from outside logo
    sky_hole = hole & in_box((160, 40, 930, 520))
    if sky_hole.any():
        for y in range(40, 520):
            hs = np.where(sky_hole[y])[0]
            if hs.size == 0:
                continue
            ks = np.where((~hole[y]) & sky[y])[0]
            if ks.size == 0:
                continue
            idx = np.searchsorted(ks, hs)
            idx = np.clip(idx, 0, len(ks) - 1)
            filled[y, hs] = rgb[y, ks[idx]]
    return filled


hole = logo | guard | thief
bg = inpaint(hole)

Image.fromarray(bg, "RGB").save(OUT / "CT_Title_Background_v002.png")
logo_im, logo_rect = crop_rgba(logo)
guard_im, guard_rect = crop_rgba(guard)
thief_im, thief_rect = crop_rgba(thief)
logo_im.save(OUT / "CT_Title_Logo_v002.png")
guard_im.save(OUT / "CT_Title_Guard_v002.png")
thief_im.save(OUT / "CT_Title_Thief_v002.png")

comp = Image.fromarray(bg, "RGB").convert("RGBA")
for im, rect in ((guard_im, guard_rect), (thief_im, thief_rect), (logo_im, logo_rect)):
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    layer.paste(im, (rect[0], rect[1]), im)
    comp = Image.alpha_composite(comp, layer)
comp.convert("RGB").save(OUT / "ART-TTL-002_CT_Title_Day_v002_layer_composite.png")

ov = rgb.copy().astype(np.float32)
ov[logo] = ov[logo] * 0.45 + np.array([255, 220, 40]) * 0.55
ov[guard] = ov[guard] * 0.45 + np.array([40, 90, 255]) * 0.55
ov[thief] = ov[thief] * 0.45 + np.array([255, 40, 90]) * 0.55
Image.fromarray(ov.astype(np.uint8), "RGB").save(OUT / "ART-TTL-002_v002_layer_masks.png")

orig = rgb.astype(np.int16)
cnp = np.array(comp.convert("RGB")).astype(np.int16)
print("composite mad", round(float(np.abs(orig - cnp).mean()), 3))
print("rects", {"logo": logo_rect, "guard": guard_rect, "thief": thief_rect})
print("saved")
