"""Circular Celtic knot for the paddle medallion (D-017, like reference/paddle_centre_medallion_ref.jpg).

Strands are closed parametric curves: a 7/3 rosette woven with a rounded diamond core; a plain ring frames them.
Every crossing is found and over/under alternates along each strand (the Celtic rule). Rendering uses exact distance
fields (no stroked polylines): the strands are split into pieces between crossings, so at a crossing the over-piece's
border cuts the under-piece cleanly.
  fill    = raised strand surface, groove = cut lines (strand borders + the gap under an over-strand),
  profile = 0..1 rounded strand height. Canvas covers [-EXTENT, EXTENT] medallion radii around the centre.
Run directly to write a preview PNG: python medallion_knot.py out.png
"""
import math
import sys

import numpy as np
from PIL import Image

EXTENT = 1.08          # canvas half size in medallion radii
CANVAS = 1024          # pixels
WIDTH = 0.085          # strand width (in R)
BORDER = 0.024         # cut line on each side of a strand (in R)
RING_R = 0.985         # frame ring radius (not woven)
RING_W = 0.05


def rosette(samples=1500):
    p, q = 7, 3
    t = np.linspace(0, 2 * math.pi * q, samples, endpoint=False)
    r = 0.565 + 0.345 * np.cos(p * t / q)
    return np.stack([r * np.cos(t), r * np.sin(t)], 1)


def diamond(samples=500, size=0.30, n=4.0):
    t = np.linspace(0, 2 * math.pi, samples, endpoint=False)
    c, s = np.cos(t), np.sin(t)
    rr = size / (np.abs(c) ** n + np.abs(s) ** n) ** (1 / n)
    x, y = rr * c, rr * s
    a = math.pi / 4
    return np.stack([x * math.cos(a) - y * math.sin(a), x * math.sin(a) + y * math.cos(a)], 1)


def find_crossings(curves):
    """List of dicts {a:(curve, pos), b:(curve, pos), pt, angle}. pos = segment index + fraction."""
    out = []
    for ca in range(len(curves)):
        A = curves[ca]
        a0, a1 = A, np.roll(A, -1, 0)
        for cb in range(ca, len(curves)):
            B = curves[cb]
            b0, b1 = B, np.roll(B, -1, 0)
            d1 = (a1 - a0)[:, None, :]
            d2 = (b1 - b0)[None, :, :]
            den = d1[..., 0] * d2[..., 1] - d1[..., 1] * d2[..., 0]
            w = b0[None, :, :] - a0[:, None, :]
            with np.errstate(divide="ignore", invalid="ignore"):
                ta = (w[..., 0] * d2[..., 1] - w[..., 1] * d2[..., 0]) / den
                tb = (w[..., 0] * d1[..., 1] - w[..., 1] * d1[..., 0]) / den
            hit = (np.abs(den) > 1e-12) & (ta >= 0) & (ta < 1) & (tb >= 0) & (tb < 1)
            for i, j in zip(*np.nonzero(hit)):
                i, j = int(i), int(j)
                if ca == cb and (abs(i - j) <= 2 or abs(i - j) >= len(A) - 2 or j <= i):
                    continue
                va, vb = d1[i, 0], d2[0, j]
                cosang = abs(np.dot(va, vb)) / (np.linalg.norm(va) * np.linalg.norm(vb))
                out.append({"a": (ca, i + float(ta[i, j])), "b": (cb, j + float(tb[i, j])),
                            "pt": a0[i] + va * ta[i, j], "angle": math.acos(min(1.0, cosang))})
    return out


def assign_over(curves, crossings):
    """Alternate over/under along every curve. Sets crossing['over'] = 'a' or 'b'. Returns the conflict count."""
    events = {c: [] for c in range(len(curves))}
    for k, cr in enumerate(crossings):
        events[cr["a"][0]].append((cr["a"][1], k, "a"))
        events[cr["b"][0]].append((cr["b"][1], k, "b"))
    conflicts = 0
    for c in sorted(events, key=lambda c: -len(events[c])):
        ev = sorted(events[c])
        start = 0
        for n, (_, k, side) in enumerate(ev):
            if "over" in crossings[k]:
                start = (0 if crossings[k]["over"] == side else 1) ^ (n % 2)
                break
        for n, (_, k, side) in enumerate(ev):
            other = "b" if side == "a" else "a"
            val = side if ((n % 2) ^ start) == 0 else other
            if "over" not in crossings[k]:
                crossings[k]["over"] = val
            elif crossings[k]["over"] != val:
                conflicts += 1
    return conflicts


def to_px(pts):
    scale = CANVAS / (2 * EXTENT)
    return np.stack([CANVAS / 2 + pts[:, 0] * scale, CANVAS / 2 - pts[:, 1] * scale], 1)


def polyline_distance(P, closed, box=None):
    """Exact distance (px) from every canvas pixel in box (x0, y0, x1, y1) to polyline P (px coords)."""
    if box is None:
        box = (0, 0, CANVAS, CANVAS)
    x0, y0, x1, y1 = box
    xs = np.arange(x0, x1) + 0.5
    ys = np.arange(y0, y1) + 0.5
    X, Y = np.meshgrid(xs, ys)
    D = np.full(X.shape, 1e9, np.float32)
    A = P if closed else P[:-1]
    B = np.roll(P, -1, 0) if closed else P[1:]
    for a, b in zip(A, B):
        d = b - a
        L2 = float(d @ d) or 1e-9
        t = np.clip(((X - a[0]) * d[0] + (Y - a[1]) * d[1]) / L2, 0, 1)
        D = np.minimum(D, np.hypot(X - (a[0] + t * d[0]), Y - (a[1] + t * d[1])))
    return D


def piece_around(curve, pos, half_len):
    """Sub-polyline of a closed curve centred on parameter pos, about 2*half_len long (in curve units)."""
    n = len(curve)
    seg = np.linalg.norm(np.roll(curve, -1, 0) - curve, axis=1).mean()
    m = max(2, int(half_len / seg))
    i = int(pos)
    idx = [(i + o) % n for o in range(-m, m + 2)]
    return curve[idx]


def draw(curves, crossings):
    scale = CANVAS / (2 * EXTENT)
    w = WIDTH * scale
    b = BORDER * scale
    # 1) all strands merged (no over/under yet)
    D = np.full((CANVAS, CANVAS), 1e9, np.float32)
    for c in curves:
        D = np.minimum(D, polyline_distance(to_px(c), True))
    fill = D < w / 2
    groove = (D >= w / 2) & (D < w / 2 + b)
    dtop = D.copy()
    # 2) each crossing: inside a window around it, the over-piece cuts the under-piece
    for cr in crossings:
        ov, un = (cr["a"], cr["b"]) if cr["over"] == "a" else (cr["b"], cr["a"])
        ang = max(cr["angle"], math.radians(12))
        reach = (WIDTH / 2 + BORDER) / math.sin(ang) + WIDTH          # how far along the strands they interact (R)
        P_over = to_px(piece_around(curves[ov[0]], ov[1], reach * 1.6))
        P_under = to_px(piece_around(curves[un[0]], un[1], reach * 1.6))
        cx, cy = to_px(np.array([cr["pt"]]))[0]
        rad = reach * scale * 1.1
        x0, y0 = max(0, int(cx - rad)), max(0, int(cy - rad))
        x1, y1 = min(CANVAS, int(cx + rad) + 1), min(CANVAS, int(cy + rad) + 1)
        box = (x0, y0, x1, y1)
        Do = polyline_distance(P_over, False, box)
        Du = polyline_distance(P_under, False, box)
        X, Y = np.meshgrid(np.arange(x0, x1) + 0.5, np.arange(y0, y1) + 0.5)
        inside = np.hypot(X - cx, Y - cy) < rad
        o_fill = Do < w / 2
        o_band = Do < w / 2 + b
        u_fill = (Du < w / 2) & ~o_band
        f = o_fill | u_fill
        g = (o_band & ~o_fill) | ((Du < w / 2 + b) & ~o_band & ~u_fill)
        sl = (slice(y0, y1), slice(x0, x1))
        # pixels that belong to neither of the two crossing pieces keep what other strands put there
        mine = o_band | (Du < w / 2 + b)
        f = f | (fill[sl] & ~mine)
        g = (g | (groove[sl] & ~mine)) & ~f
        fill[sl] = np.where(inside, f, fill[sl])
        groove[sl] = np.where(inside, g, groove[sl])
        dtop[sl] = np.where(inside, np.where(o_band, Do, np.where(Du < w / 2 + b, Du, dtop[sl])), dtop[sl])
    # 3) frame ring (plain, not woven)
    R = np.hypot(*np.meshgrid((np.arange(CANVAS) + 0.5 - CANVAS / 2) / scale, (np.arange(CANVAS) + 0.5 - CANVAS / 2) / scale))
    rd = np.abs(R - RING_R) * scale
    ring_fill = rd < RING_W * scale / 2
    ring_groove = (rd >= RING_W * scale / 2) & (rd < RING_W * scale / 2 + b)
    fill |= ring_fill
    groove = (groove | ring_groove) & ~fill
    dtop = np.where(ring_fill, rd * (WIDTH / RING_W), dtop)
    profile = np.clip(1 - (dtop / (w / 2)) ** 2, 0, 1).astype(np.float32)
    return fill, groove, profile


_CACHE = {}


def knot():
    if "k" not in _CACHE:
        curves = [rosette(), diamond()]
        cr = find_crossings(curves)
        conflicts = assign_over(curves, cr)
        f, g, p = draw(curves, cr)
        _CACHE["k"] = (f, g, p, len(cr), conflicts)
    return _CACHE["k"]


def sample(X, Y):
    """X, Y in medallion radii (any shape) -> fill, groove, profile at those points (nearest canvas pixel)."""
    f, g, p, _, _ = knot()
    scale = CANVAS / (2 * EXTENT)
    cx = np.clip((CANVAS / 2 + X * scale).astype(int), 0, CANVAS - 1)
    cy = np.clip((CANVAS / 2 - Y * scale).astype(int), 0, CANVAS - 1)
    return f[cy, cx], g[cy, cx], p[cy, cx]


if __name__ == "__main__":
    f, g, p, n, conflicts = knot()
    img = np.full(f.shape + (3,), 60, np.uint8)
    img[f] = (np.array([200, 140, 70]) * (0.7 + 0.3 * p[f, None])).astype(np.uint8)
    img[g] = (25, 15, 8)
    Image.fromarray(img).save(sys.argv[1] if len(sys.argv) > 1 else "medallion.png")
    print(f"crossings {n}, alternation conflicts {conflicts}")
