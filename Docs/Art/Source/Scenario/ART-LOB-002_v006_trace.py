"""ART-LOB-002 v006: measured geometric trace of v004.

Silhouette comes from v004 alpha (smoothed). Interior shapes are ellipses
and paths fitted to v004 landmarks. Colors are sampled from v004 interiors.
No generative upscale, no v004/v005 overwrite.
"""
from __future__ import annotations

from collections import defaultdict
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent
SRC = ROOT / "ART-LOB-002_CT_Lobby_Thief_Idle_v004_source.png"
OUT_SVG = ROOT / "ART-LOB-002_CT_Lobby_Thief_Idle_v006.svg"
OUT_PNG = ROOT / "ART-LOB-002_CT_Lobby_Thief_Idle_v006_source.png"
OUT_COMPARE = ROOT / "ART-LOB-002_CT_Lobby_Thief_Idle_v004_v006_compare.png"

SRC_W, SRC_H = 100, 130
SCALE = 6
SS = 4
OUT_W, OUT_H = SRC_W * SCALE, SRC_H * SCALE
WORK_W, WORK_H = OUT_W * SS, OUT_H * SS
K = SCALE * SS  # 24

# Sampled from v004 interior pixels (not AA fringe).
COL_OUTLINE = (6, 4, 3, 255)
COL_BEANIE = (236, 86, 85, 255)
COL_BEANIE_STRIPE = (246, 127, 120, 255)
COL_SKIN = (248, 208, 176, 255)
COL_WHITE = (250, 250, 250, 255)
COL_MASK = (10, 8, 7, 255)
COL_SHIRT_BLACK = (22, 20, 19, 255)
COL_PANTS = (24, 22, 21, 255)

OUTLINE_PX = 4.35  # source pixels


def opaque_mask(src: Image.Image) -> np.ndarray:
    a = np.array(src.split()[-1])
    return a >= 128


def mask_edges(mask: np.ndarray):
    h, w = mask.shape
    segs = []
    for y in range(h):
        for x in range(w):
            if not mask[y, x]:
                continue
            if y == 0 or not mask[y - 1, x]:
                segs.append(((x, y), (x + 1, y)))
            if x + 1 == w or not mask[y, x + 1]:
                segs.append(((x + 1, y), (x + 1, y + 1)))
            if y + 1 == h or not mask[y + 1, x]:
                segs.append(((x + 1, y + 1), (x, y + 1)))
            if x == 0 or not mask[y, x - 1]:
                segs.append(((x, y + 1), (x, y)))
    return segs


def stitch_rings(segs):
    nxt = defaultdict(list)
    for a, b in segs:
        nxt[a].append(b)
    rings = []
    while nxt:
        start = next(iter(nxt))
        ring = [start]
        cur = start
        for _ in range(30000):
            opts = nxt.get(cur)
            if not opts:
                break
            nxt_pt = opts.pop()
            if not opts:
                del nxt[cur]
            ring.append(nxt_pt)
            cur = nxt_pt
            if cur == start:
                break
        if len(ring) >= 4:
            arr = np.array(ring[:-1] if ring[0] == ring[-1] else ring, dtype=np.float64)
            if len(arr) >= 3:
                rings.append(arr)
    rings.sort(key=lambda r: abs(_area(r)), reverse=True)
    return rings


def _area(ring: np.ndarray) -> float:
    x, y = ring[:, 0], ring[:, 1]
    return 0.5 * float(np.dot(x, np.roll(y, -1)) - np.dot(y, np.roll(x, -1)))


def chaikin(pts: np.ndarray, iters: int = 3) -> np.ndarray:
    pts = np.asarray(pts, dtype=np.float64)
    for _ in range(iters):
        a = pts
        b = np.roll(pts, -1, axis=0)
        nxt = np.empty((len(pts) * 2, 2), dtype=np.float64)
        nxt[0::2] = 0.75 * a + 0.25 * b
        nxt[1::2] = 0.25 * a + 0.75 * b
        pts = nxt
    return pts


def cubic(p0, p1, p2, p3, n=24):
    t = np.linspace(0, 1, n)[:, None]
    return (1 - t) ** 3 * p0 + 3 * (1 - t) ** 2 * t * p1 + 3 * (1 - t) * t ** 2 * p2 + t ** 3 * p3


def quad(p0, p1, p2, n=16):
    t = np.linspace(0, 1, n)[:, None]
    return (1 - t) ** 2 * p0 + 2 * (1 - t) * t * p1 + t ** 2 * p2


def ellipse_poly(cx, cy, rx, ry, n=80, rot_deg=0.0):
    t = np.linspace(0, 2 * np.pi, n, endpoint=False)
    x, y = rx * np.cos(t), ry * np.sin(t)
    a = np.radians(rot_deg)
    ca, sa = np.cos(a), np.sin(a)
    return np.column_stack([ca * x - sa * y + cx, sa * x + ca * y + cy])


def rounded_rect_poly(x, y, w, h, r, n=8):
    r = min(r, w * 0.5, h * 0.5)
    pts = []
    corners = [
        ((x + w - r, y + r), 270, 360),
        ((x + w - r, y + h - r), 0, 90),
        ((x + r, y + h - r), 90, 180),
        ((x + r, y + r), 180, 270),
    ]
    for (cx, cy), a0, a1 in corners:
        ang = np.linspace(np.radians(a0), np.radians(a1), n)
        pts.append(np.column_stack([cx + r * np.cos(ang), cy + r * np.sin(ang)]))
    return np.vstack(pts)


def connected_blobs(mask: np.ndarray):
    h, w = mask.shape
    vis = np.zeros((h, w), dtype=bool)
    blobs = []
    ys, xs = np.where(mask)
    for y0, x0 in zip(ys, xs):
        if vis[y0, x0]:
            continue
        stack = [(y0, x0)]
        vis[y0, x0] = True
        pts = []
        while stack:
            y, x = stack.pop()
            pts.append((x, y))
            for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                ny, nx = y + dy, x + dx
                if 0 <= ny < h and 0 <= nx < w and not vis[ny, nx] and mask[ny, nx]:
                    vis[ny, nx] = True
                    stack.append((ny, nx))
        blobs.append(np.array(pts, dtype=np.int32))
    blobs.sort(key=len, reverse=True)
    return blobs


def ellipse_from_blob(pts: np.ndarray):
    xy = pts.astype(np.float64) + 0.5
    cx, cy = xy[:, 0].mean(), xy[:, 1].mean()
    cov = np.cov(xy.T)
    eigval, eigvec = np.linalg.eigh(cov)
    order = np.argsort(eigval)[::-1]
    eigval = eigval[order]
    eigvec = eigvec[:, order]
    rx = 2.0 * np.sqrt(max(eigval[0], 1e-6))
    ry = 2.0 * np.sqrt(max(eigval[1], 1e-6))
    ang = float(np.degrees(np.arctan2(eigvec[1, 0], eigvec[0, 0])))
    return cx, cy, rx, ry, ang


def work_pts(ring):
    return [(float(x) * K, float(y) * K) for x, y in ring]


def fill_poly(draw: ImageDraw.ImageDraw, ring, color):
    pts = work_pts(ring)
    if len(pts) >= 3:
        draw.polygon(pts, fill=color)


def fill_ellipse(draw, cx, cy, rx, ry, color, rot=0.0, n=80):
    fill_poly(draw, ellipse_poly(cx, cy, rx, ry, n=n, rot_deg=rot), color)


def stroke_poly(draw, ring, color, width_src):
    pts = work_pts(ring)
    w = max(1, int(round(width_src * K)))
    if len(pts) >= 2:
        draw.line(pts + [pts[0]], fill=color, width=w, joint="curve")


def beanie_path():
    """Interior beanie, source pixels. Tilted cap with rounded crown and curved brim."""
    # Crown and sides, then brim left-to-right on the forehead.
    p = [np.array(v, dtype=np.float64) for v in [
        (14.0, 47.5),
        (8.5, 36.0), (10.0, 22.0), (18.0, 12.5),
        (28.0, 4.8), (42.0, 4.2), (50.0, 6.2),
        (62.0, 4.5), (78.0, 6.5), (86.5, 16.0),
        (93.0, 26.0), (93.5, 38.0), (88.0, 47.0),
        (70.0, 51.5), (50.0, 52.2), (28.0, 51.0),
        (14.0, 47.5),
    ]]
    # Interpret as start + series of cubic triples... easier: use quadratic chain.
    pts = [p[0]]
    # left up to top
    pts.append(quad(p[0], (8.2, 34.0), (11.5, 20.5), 14)[1:])
    pts.append(quad((11.5, 20.5), (22.0, 6.5), (38.0, 5.0), 14)[1:])
    pts.append(quad((38.0, 5.0), (50.0, 3.6), (62.0, 5.2), 12)[1:])
    pts.append(quad((62.0, 5.2), (80.0, 7.0), (88.5, 18.0), 14)[1:])
    pts.append(quad((88.5, 18.0), (95.0, 32.0), (88.5, 46.5), 14)[1:])
    pts.append(quad((88.5, 46.5), (70.0, 52.0), (50.0, 52.4), 14)[1:])
    pts.append(quad((50.0, 52.4), (28.0, 51.8), (14.0, 47.5), 14)[1:])
    return np.vstack(pts)


def beanie_stripe_path():
    """Single light band across the beanie. Present in v004; no extra knit rows."""
    # A thick ribbon: outer loop around a curved band y~16-26.
    top = np.vstack([
        quad((18.0, 18.5), (36.0, 11.5), (52.0, 13.0), 16),
        quad((52.0, 13.0), (70.0, 14.5), (84.0, 22.0), 16)[1:],
    ])
    bot = np.vstack([
        quad((82.5, 28.5), (68.0, 21.5), (50.0, 20.5), 16),
        quad((50.0, 20.5), (32.0, 19.0), (17.5, 24.5), 16)[1:],
    ])
    return np.vstack([top, bot])


def mask_path():
    """Burglar mask: tapered wings around both eyes, connected at the bridge."""
    # Outer silhouette of the mask (interior), eyes painted white on top.
    pts = []
    # left point -> over left eye -> bridge top -> over right eye -> right point
    # then bottom edge back.
    pts.append(quad((12.5, 62.0), (11.0, 54.0), (18.0, 50.5), 12))
    pts.append(quad((18.0, 50.5), (28.0, 46.5), (38.5, 47.5), 12)[1:])
    pts.append(quad((38.5, 47.5), (45.0, 50.5), (50.0, 53.5), 10)[1:])
    pts.append(quad((50.0, 53.5), (56.5, 49.5), (64.0, 46.0), 10)[1:])
    pts.append(quad((64.0, 46.0), (76.0, 44.5), (86.0, 48.5), 12)[1:])
    pts.append(quad((86.0, 48.5), (93.5, 53.0), (92.0, 60.5), 10)[1:])
    pts.append(quad((92.0, 60.5), (90.5, 68.0), (82.0, 70.5), 12)[1:])
    pts.append(quad((82.0, 70.5), (70.0, 69.5), (60.5, 67.0), 12)[1:])
    pts.append(quad((60.5, 67.0), (54.0, 71.0), (49.5, 71.5), 10)[1:])
    pts.append(quad((49.5, 71.5), (44.0, 71.5), (38.0, 68.5), 10)[1:])
    pts.append(quad((38.0, 68.5), (26.0, 71.0), (16.5, 68.5), 12)[1:])
    pts.append(quad((16.5, 68.5), (11.5, 66.5), (12.5, 62.0), 10)[1:])
    return np.vstack(pts)


def mouth_path():
    """Small filled smile from v004, no extra lip volume."""
    upper = quad((45.8, 75.2), (50.6, 78.6), (55.6, 75.0), 14)
    lower = quad((55.6, 75.0), (50.6, 80.4), (45.8, 75.2), 14)[1:]
    return np.vstack([upper, lower])


def svg_path(ring) -> str:
    parts = [f"M {ring[0, 0]:.3f},{ring[0, 1]:.3f}"]
    for x, y in ring[1:]:
        parts.append(f"L {x:.3f},{y:.3f}")
    parts.append("Z")
    return " ".join(parts)


def svg_color(c):
    return f"rgb({c[0]},{c[1]},{c[2]})"


def measure_eyes(src_arr):
    rgb, a = src_arr[:, :, :3].astype(int), src_arr[:, :, 3]
    white = (a >= 128) & (rgb[:, :, 0] > 220) & (rgb[:, :, 1] > 220) & (rgb[:, :, 2] > 220)
    yy = np.arange(a.shape[0])[:, None]
    blobs = connected_blobs(white & (yy < 72))
    blobs = sorted(blobs[:2], key=lambda p: p[:, 0].mean())
    eyes = [ellipse_from_blob(b) for b in blobs]
    pupils = []
    dark = (a >= 128) & (rgb[:, :, 0] < 40) & (rgb[:, :, 1] < 40) & (rgb[:, :, 2] < 40)
    for b in blobs:
        x0, y0 = int(b[:, 0].min()), int(b[:, 1].min())
        x1, y1 = int(b[:, 0].max()), int(b[:, 1].max())
        region = np.zeros(a.shape, dtype=bool)
        region[y0:y1 + 1, x0:x1 + 1] = True
        pblobs = connected_blobs(dark & region)
        cx, cy = b[:, 0].mean(), b[:, 1].mean()
        best, best_s = None, 1e9
        for pb in pblobs:
            if not (8 <= len(pb) <= 80):
                continue
            s = abs(pb[:, 0].mean() - cx) + abs(pb[:, 1].mean() - cy) - 0.15 * len(pb)
            if s < best_s:
                best, best_s = pb, s
        if best is not None:
            pupils.append(ellipse_from_blob(best))
    return eyes, pupils


def write_svg(sil_ring, eyes, pupils):
    beanie = beanie_path()
    stripe = beanie_stripe_path()
    mask = mask_path()
    mouth = mouth_path()
    lines = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{OUT_W}" height="{OUT_H}" viewBox="0 0 {SRC_W} {SRC_H}">',
        "  <!-- ART-LOB-002 v006. viewBox = v004 pixel space. Geometric trace, not upscale. -->",
        '  <g id="outline">',
        f'    <path d="{svg_path(sil_ring)}" fill="{svg_color(COL_OUTLINE)}" />',
        "  </g>",
        '  <g id="body">',
        f'    <rect x="31.5" y="86.0" width="37.0" height="26.5" rx="11" fill="{svg_color(COL_SHIRT_BLACK)}" />',
        f'    <rect x="32.2" y="87.2" width="35.6" height="5.0" rx="2.2" fill="{svg_color(COL_WHITE)}" />',
        f'    <rect x="32.2" y="95.0" width="35.6" height="4.6" rx="2.0" fill="{svg_color(COL_WHITE)}" />',
        f'    <rect x="32.0" y="103.4" width="36.0" height="4.4" rx="2.0" fill="{svg_color(COL_WHITE)}" />',
        f'    <rect x="33.5" y="109.5" width="14.5" height="17.5" rx="6.5" fill="{svg_color(COL_PANTS)}" />',
        f'    <rect x="52.0" y="109.5" width="14.5" height="17.5" rx="6.5" fill="{svg_color(COL_PANTS)}" />',
        f'    <ellipse cx="21.5" cy="99.5" rx="8.2" ry="7.6" fill="{svg_color(COL_SKIN)}" />',
        f'    <ellipse cx="78.2" cy="98.8" rx="8.2" ry="7.6" fill="{svg_color(COL_SKIN)}" />',
        f'    <ellipse cx="25.5" cy="93.5" rx="7.4" ry="6.2" fill="{svg_color(COL_SHIRT_BLACK)}" />',
        f'    <ellipse cx="74.5" cy="93.0" rx="7.4" ry="6.2" fill="{svg_color(COL_SHIRT_BLACK)}" />',
        f'    <rect x="21.8" y="91.5" width="9.5" height="3.2" rx="1.4" fill="{svg_color(COL_WHITE)}" />',
        f'    <rect x="68.8" y="91.0" width="9.5" height="3.2" rx="1.4" fill="{svg_color(COL_WHITE)}" />',
        "  </g>",
        '  <g id="head">',
        f'    <ellipse cx="49.0" cy="58.5" rx="41.0" ry="27.8" fill="{svg_color(COL_SKIN)}" />',
        "  </g>",
        '  <g id="beanie">',
        f'    <path d="{svg_path(beanie)}" fill="{svg_color(COL_BEANIE)}" />',
        f'    <path d="{svg_path(stripe)}" fill="{svg_color(COL_BEANIE_STRIPE)}" />',
        "  </g>",
        '  <g id="face">',
        f'    <path d="{svg_path(mask)}" fill="{svg_color(COL_MASK)}" />',
    ]
    for cx, cy, rx, ry, ang in eyes:
        lines.append(
            f'    <ellipse cx="{cx:.3f}" cy="{cy:.3f}" rx="{rx:.3f}" ry="{ry:.3f}" '
            f'transform="rotate({ang:.3f} {cx:.3f} {cy:.3f})" fill="{svg_color(COL_WHITE)}" />'
        )
    for cx, cy, rx, ry, ang in pupils:
        lines.append(
            f'    <ellipse cx="{cx:.3f}" cy="{cy:.3f}" rx="{rx:.3f}" ry="{ry:.3f}" '
            f'transform="rotate({ang:.3f} {cx:.3f} {cy:.3f})" fill="{svg_color(COL_OUTLINE)}" />'
        )
    lines += [
        f'    <path d="{svg_path(mouth)}" fill="{svg_color(COL_OUTLINE)}" />',
        "  </g>",
        "</svg>",
    ]
    OUT_SVG.write_text("\n".join(lines) + "\n", encoding="utf-8")


def chamfer_dist(opaque: np.ndarray) -> np.ndarray:
    h, w = opaque.shape
    inf = 1.0e6
    dist = np.where(opaque, inf, 0.0).astype(np.float32)
    for y in range(h):
        for x in range(w):
            if not opaque[y, x]:
                continue
            v = dist[y, x]
            if x:
                v = min(v, dist[y, x - 1] + 1.0)
            if y:
                v = min(v, dist[y - 1, x] + 1.0)
            if x and y:
                v = min(v, dist[y - 1, x - 1] + 1.414)
            if x + 1 < w and y:
                v = min(v, dist[y - 1, x + 1] + 1.414)
            dist[y, x] = v
    for y in range(h - 1, -1, -1):
        for x in range(w - 1, -1, -1):
            if not opaque[y, x]:
                continue
            v = dist[y, x]
            if x + 1 < w:
                v = min(v, dist[y, x + 1] + 1.0)
            if y + 1 < h:
                v = min(v, dist[y + 1, x] + 1.0)
            if x + 1 < w and y + 1 < h:
                v = min(v, dist[y + 1, x + 1] + 1.414)
            if x and y + 1 < h:
                v = min(v, dist[y + 1, x - 1] + 1.414)
            dist[y, x] = v
    return dist


def rasterize(sil_ring, eyes, pupils, src_mask: np.ndarray) -> Image.Image:
    outline = Image.new("RGBA", (WORK_W, WORK_H), (0, 0, 0, 0))
    od = ImageDraw.Draw(outline)
    fill_poly(od, sil_ring, COL_OUTLINE)

    # Clip interiors to the v004 silhouette inset by the outline width.
    interior = chamfer_dist(src_mask) >= (OUTLINE_PX * 0.92)
    clip = Image.fromarray((interior.astype(np.uint8) * 255)).resize(
        (WORK_W, WORK_H), Image.Resampling.LANCZOS
    )

    body = Image.new("RGBA", (WORK_W, WORK_H), (0, 0, 0, 0))
    d = ImageDraw.Draw(body)

    # Legs first, then torso, arms, head, beanie, face.
    fill_poly(d, rounded_rect_poly(33.5, 109.5, 14.5, 17.5, 6.5), COL_PANTS)
    fill_poly(d, rounded_rect_poly(52.0, 109.5, 14.5, 17.5, 6.5), COL_PANTS)
    fill_poly(d, rounded_rect_poly(31.5, 86.0, 37.0, 26.5, 11.0), COL_SHIRT_BLACK)
    fill_poly(d, rounded_rect_poly(32.2, 87.2, 35.6, 5.0, 2.2), COL_WHITE)
    fill_poly(d, rounded_rect_poly(32.2, 95.0, 35.6, 4.6, 2.0), COL_WHITE)
    fill_poly(d, rounded_rect_poly(32.0, 103.4, 36.0, 4.4, 2.0), COL_WHITE)

    fill_ellipse(d, 21.5, 99.5, 8.2, 7.6, COL_SKIN)
    fill_ellipse(d, 78.2, 98.8, 8.2, 7.6, COL_SKIN)
    fill_ellipse(d, 25.5, 93.5, 7.4, 6.2, COL_SHIRT_BLACK)
    fill_ellipse(d, 74.5, 93.0, 7.4, 6.2, COL_SHIRT_BLACK)
    fill_poly(d, rounded_rect_poly(21.8, 91.5, 9.5, 3.2, 1.4), COL_WHITE)
    fill_poly(d, rounded_rect_poly(68.8, 91.0, 9.5, 3.2, 1.4), COL_WHITE)

    fill_ellipse(d, 49.0, 58.5, 41.0, 27.8, COL_SKIN)
    fill_poly(d, beanie_path(), COL_BEANIE)
    fill_poly(d, beanie_stripe_path(), COL_BEANIE_STRIPE)
    fill_poly(d, mask_path(), COL_MASK)
    for cx, cy, rx, ry, ang in eyes:
        fill_ellipse(d, cx, cy, rx, ry, COL_WHITE, rot=ang, n=96)
    for cx, cy, rx, ry, ang in pupils:
        fill_ellipse(d, cx, cy, rx, ry, COL_OUTLINE, rot=ang, n=48)
    fill_poly(d, mouth_path(), COL_OUTLINE)

    body = ImageChops_multiply_alpha(body, clip)
    acc = Image.alpha_composite(outline, body)
    return acc.resize((OUT_W, OUT_H), Image.Resampling.LANCZOS)


def ImageChops_multiply_alpha(rgba: Image.Image, clip: Image.Image) -> Image.Image:
    r, g, b, a = rgba.split()
    a = Image.fromarray(
        (np.array(a).astype(np.float32) * (np.array(clip).astype(np.float32) / 255.0))
        .clip(0, 255)
        .astype(np.uint8)
    )
    return Image.merge("RGBA", (r, g, b, a))


def make_compare(v004: Image.Image, v006: Image.Image) -> Image.Image:
    cell_w, cell_h = 300, 390
    pad, header, gutter = 16, 36, 12
    cols, rows = 2, 3
    w = pad * 2 + cols * cell_w + gutter
    h = pad * 2 + header + rows * (cell_h + 22) + (rows - 1) * gutter
    canvas = Image.new("RGB", (w, h), (48, 48, 48))
    draw = ImageDraw.Draw(canvas)
    draw.text((pad, 8), "v004 100x130 (3x nearest)     v006 600x780 (display 300x390)", fill=(220, 220, 220))
    left = v004.resize((cell_w, cell_h), Image.Resampling.NEAREST)
    right = v006.resize((cell_w, cell_h), Image.Resampling.LANCZOS)
    bgs = [("black", (0, 0, 0)), ("white", (255, 255, 255)), ("pink", (255, 105, 180))]
    for i, (label, bg) in enumerate(bgs):
        y = pad + header + i * (cell_h + 22 + gutter)
        for j, sprite in enumerate((left, right)):
            x = pad + j * (cell_w + gutter)
            cell = Image.new("RGBA", (cell_w, cell_h), bg + (255,))
            cell = Image.alpha_composite(cell, sprite.convert("RGBA"))
            canvas.paste(cell.convert("RGB"), (x, y))
            draw.text((x, y + cell_h + 4), f"{label} {'v004' if j == 0 else 'v006'}", fill=(200, 200, 200))
    return canvas


def main():
    src = Image.open(SRC).convert("RGBA")
    if src.size != (SRC_W, SRC_H):
        raise RuntimeError(f"v004 size {src.size}, expected {SRC_W}x{SRC_H}")
    mask = opaque_mask(src)
    rings = stitch_rings(mask_edges(mask))
    if not rings:
        raise RuntimeError("no silhouette")
    sil = chaikin(rings[0], iters=3)
    arr = np.array(src)
    eyes, pupils = measure_eyes(arr)
    write_svg(sil, eyes, pupils)
    png = rasterize(sil, eyes, pupils, mask)
    png.save(OUT_PNG)
    make_compare(src, png).save(OUT_COMPARE)
    print("eyes", eyes)
    print("pupils", pupils)
    print("png", png.size, png.mode, "silhouette_pts", len(sil))
    print("wrote", OUT_PNG.name, OUT_SVG.name, OUT_COMPARE.name)


if __name__ == "__main__":
    main()
