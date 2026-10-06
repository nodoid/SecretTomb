#!/usr/bin/env python3
"""Draws the game's high-resolution artwork into SecretTomb.Core/Art/*.png.

    python3 tools/make_art.py

Two techniques are used, both supersampled and reduced with a Lanczos filter:

* Carved stone (walls, floors, gates, the sarcophagus...) is modelled as a height map: the
  raised and recessed parts of each carving are drawn as soft shapes, then lit from the top
  left, with a fine grain, so every block looks like real relief work.
* Figures and objects (the explorer, the monsters, the treasures) are vector drawings with
  gradient shading and a dark outline.

One "unit" is one pixel of the game's 240 x 224 layout; a tile of the tomb is 24 x 24 units.
Pictures are written at PX pixels per unit (10: a tile is 240 x 240 pixels).
"""
import math
import os
import random

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'SecretTomb.Core', 'Art')
PX = 10          # output pixels per unit
SS = 3           # supersampling
T = 24           # tile size in units

INK = (40, 24, 12)
GOLD_L = (255, 226, 132)
GOLD = (222, 172, 64)
GOLD_D = (150, 100, 28)
SAND = (214, 168, 76)
SLATE = (52, 64, 128)
JADE = (70, 184, 120)
SKIN = (238, 196, 150)
SKIN_D = (192, 140, 100)
KHAKI = (206, 182, 120)
KHAKI_D = (150, 126, 76)
BROWN = (118, 74, 40)
BROWN_D = (74, 44, 22)


# --------------------------------------------------------------------------- helpers

def blur(a, sigma):
    """Gaussian-like blur of a float array (three box passes), sigma in pixels."""
    if sigma < 0.5:
        return a
    r = max(1, int(round(sigma * 1.0)))
    for _ in range(3):
        for axis in (0, 1):
            pad = [(0, 0), (0, 0)]
            pad[axis] = (r + 1, r)
            p = np.pad(a, pad, mode='edge')
            c = np.cumsum(p, axis=axis)
            if axis == 0:
                a = (c[2 * r + 1:, :] - c[:-2 * r - 1, :]) / (2 * r + 1)
            else:
                a = (c[:, 2 * r + 1:] - c[:, :-2 * r - 1]) / (2 * r + 1)
    return a


class Canvas:
    """A picture w x h units, drawn at K = PX * SS pixels per unit."""

    def __init__(self, w, h, px=None):
        px = px or PX
        self.w, self.h = w, h
        self.k = px * SS
        self.px = px
        self.W, self.H = round(w * self.k), round(h * self.k)

    def P(self, pts):
        return [(x * self.k, y * self.k) for x, y in pts]

    def B(self, box):
        x0, y0, x1, y1 = box
        return [x0 * self.k, y0 * self.k, x1 * self.k, y1 * self.k]

    def mask(self, kind, geo, width=None, r=0.5):
        m = Image.new('L', (self.W, self.H), 0)
        d = ImageDraw.Draw(m)
        if kind == 'poly':
            d.polygon(self.P(geo), fill=255)
        elif kind == 'ellipse':
            d.ellipse(self.B(geo), fill=255)
        elif kind == 'ring':
            d.ellipse(self.B(geo), outline=255, width=max(1, int(width * self.k)))
        elif kind == 'rect':
            d.rectangle(self.B(geo), fill=255)
        elif kind == 'rrect':
            d.rounded_rectangle(self.B(geo), radius=r * self.k, fill=255)
        elif kind == 'line':
            d.line(self.P(geo), fill=255, width=max(1, int(width * self.k)), joint='curve')
            for x, y in geo:
                rr = width * self.k / 2
                d.ellipse([x * self.k - rr, y * self.k - rr, x * self.k + rr, y * self.k + rr], fill=255)
        return m

    def arr(self, m):
        return np.asarray(m, dtype=np.float32) / 255.0


def save(img, name, k):
    """Reduces a supersampled picture and writes it."""
    w, h = img.size
    out = img.resize((max(1, round(w / SS)), max(1, round(h / SS))), Image.LANCZOS)
    os.makedirs(OUT, exist_ok=True)
    out.save(os.path.join(OUT, name + '.png'))


# --------------------------------------------------------------------------- relief (carved stone)

class Relief(Canvas):
    """A height map and a colour map, lit from the top left."""

    def __init__(self, w, h, base, px=None, seed=0):
        super().__init__(w, h, px)
        self.hgt = np.zeros((self.H, self.W), np.float32)
        self.col = np.zeros((self.H, self.W, 3), np.float32)
        self.col[:] = np.array(base, np.float32) / 255.0
        self.alpha = np.ones((self.H, self.W), np.float32)
        self.rng = np.random.default_rng(seed)
        self.dark = np.zeros((self.H, self.W), np.float32)   # extra darkening (paint, soot)
        self.glow = np.zeros((self.H, self.W, 3), np.float32)  # emissive light

    def soft(self, m, soft):
        a = self.arr(m)
        return blur(a, soft * self.k) if soft else a

    def raise_(self, kind, geo, h, soft=0.6, mode='set', **kw):
        """Sets (or adds to) the height inside a shape, with soft edges."""
        a = self.soft(self.mask(kind, geo, **kw), soft)
        if mode == 'add':
            self.hgt += a * h
        elif mode == 'max':
            self.hgt = np.maximum(self.hgt, a * h)
        else:
            self.hgt = self.hgt * (1 - a) + a * h
        return a

    def paint(self, kind, geo, colour, soft=0.15, amount=1.0, **kw):
        a = self.soft(self.mask(kind, geo, **kw), soft)[..., None] * amount
        self.col = self.col * (1 - a) + a * (np.array(colour, np.float32) / 255.0)
        return a

    def shadow(self, kind, geo, amount, soft=0.3, **kw):
        a = self.soft(self.mask(kind, geo, **kw), soft)
        self.dark = np.maximum(self.dark, a * amount)

    def emit(self, kind, geo, colour, soft=1.0, amount=1.0, **kw):
        a = self.soft(self.mask(kind, geo, **kw), soft)[..., None] * amount
        self.glow += a * (np.array(colour, np.float32) / 255.0)

    def cut_alpha(self, kind, geo, soft=0.0, **kw):
        """Makes the picture transparent outside a shape."""
        a = self.soft(self.mask(kind, geo, **kw), soft)
        self.alpha = np.minimum(self.alpha, a)

    def grain(self, amount=0.08, size=0.35, specks=0.0):
        n = blur(self.rng.normal(size=(self.H, self.W)).astype(np.float32), size * self.k)
        n /= max(1e-6, n.std())
        self.col *= (1 + amount * n)[..., None]
        if specks:
            s = self.rng.random((self.H, self.W)) < specks
            s = blur(s.astype(np.float32), 0.6) * 4
            self.col *= (1 - 0.35 * np.clip(s, 0, 1))[..., None]

    def render(self, name, depth=1.0, ambient=0.42, diffuse=0.78, light=(-0.6, -0.75, 0.9), spec=0.12, smooth=0.1,
               ao=0.35):
        h = blur(self.hgt, smooth * self.k)
        gy, gx = np.gradient(h)
        nx, ny = -gx * self.k * depth, -gy * self.k * depth
        nz = np.ones_like(nx)
        n = np.sqrt(nx * nx + ny * ny + nz * nz)
        L = np.array(light, np.float32)
        L /= np.linalg.norm(L)
        lam = np.clip((nx * L[0] + ny * L[1] + nz * L[2]) / n, 0, 1)
        flat = L[2]
        shade = ambient + diffuse * lam / flat * 0.85
        # Ambient occlusion: hollows are darker than their surroundings.
        if ao:
            wide = blur(h, 2.0 * self.k)
            shade *= np.clip(1 - ao * np.clip(wide - h, 0, 1) * 2.2, 0.35, 1)
        # A little sheen on the slopes facing the light.
        hl = np.clip((lam / flat - 1.0) * 4, 0, 1) * spec
        rgb = self.col * shade[..., None] + hl[..., None]
        rgb *= (1 - self.dark)[..., None]
        rgb += self.glow
        rgb = np.clip(rgb, 0, 1)
        img = np.dstack([rgb * 255, np.clip(self.alpha, 0, 1) * 255]).astype(np.uint8)
        save(Image.fromarray(img), name, self.k)


# --------------------------------------------------------------------------- vector figures

class Pic(Canvas):
    """Vector drawing on a transparent picture."""

    def __init__(self, w, h, px=None):
        super().__init__(w, h, px)
        self.im = Image.new('RGBA', (self.W, self.H), (0, 0, 0, 0))

    def _paste(self, colour_img, m):
        layer = Image.new('RGBA', self.im.size, (0, 0, 0, 0))
        layer.paste(colour_img, (0, 0), m)
        self.im = Image.alpha_composite(self.im, layer)

    def gradient(self, c0, c1, box=None, angle=60):
        x0, y0, x1, y1 = box or (0, 0, self.w, self.h)
        a = math.radians(angle)
        dx, dy = math.cos(a), math.sin(a)
        yy, xx = np.mgrid[0:self.H, 0:self.W].astype(np.float32) / self.k
        proj = xx * dx + yy * dy
        corners = [x0 * dx + y0 * dy, x1 * dx + y0 * dy, x0 * dx + y1 * dy, x1 * dx + y1 * dy]
        lo, hi = min(corners), max(corners)
        t = np.clip((proj - lo) / max(1e-6, hi - lo), 0, 1)[..., None]
        c0, c1 = np.array(c0[:3], np.float32), np.array(c1[:3], np.float32)
        rgb = (c0 * (1 - t) + c1 * t).astype(np.uint8)
        alpha = np.full((self.H, self.W, 1), 255, np.uint8)
        return Image.fromarray(np.concatenate([rgb, alpha], axis=2))

    def shape(self, kind, geo, fill, dark=None, outline=INK, width=0.45, angle=60, alpha=255, lw=1.0, **kw):
        """A shape filled with a gradient (light top left to dark bottom right) and outlined
        (lines are lw units thick)."""
        m = self.mask(kind, geo, width=lw, **kw)
        if dark is None:
            dark = tuple(int(c * 0.62) for c in fill[:3])
        box = m.getbbox()
        bb = (box[0] / self.k, box[1] / self.k, box[2] / self.k, box[3] / self.k) if box else None
        img = self.gradient(fill, dark, bb, angle)
        if alpha < 255:
            m = m.point(lambda v: v * alpha // 255)
        if outline is not None and width > 0:
            o = m.filter(ImageFilter.MaxFilter(max(3, int(width * self.k) | 1)))
            self._paste(Image.new('RGBA', self.im.size, outline + (255,)), o)
        self._paste(img, m)
        return m

    def flat(self, kind, geo, colour, alpha=255, **kw):
        m = self.mask(kind, geo, **kw)
        if alpha < 255:
            m = m.point(lambda v: v * alpha // 255)
        self._paste(Image.new('RGBA', self.im.size, colour[:3] + (255,)), m)
        return m

    def glow(self, cx, cy, r, colour, alpha=160):
        m = self.mask('ellipse', (cx - r, cy - r, cx + r, cy + r)).filter(ImageFilter.GaussianBlur(r * self.k * 0.45))
        m = m.point(lambda v: v * alpha // 255)
        self._paste(Image.new('RGBA', self.im.size, colour[:3] + (255,)), m)

    def save(self, name):
        save(self.im, name, self.k)


def mirror_pts(pts, w):
    return [(w - x, y) for x, y in pts]


# =========================================================================== tomb tiles

def wall_block(r, inset=0.7):
    """The basic sandstone block: bevelled, with a dark joint around it."""
    r.raise_('rrect', (inset, inset, T - inset, T - inset), 1.0, soft=0.9, r=1.6)


def wall_face(name, closed_eyes=False, seam=False, seed=1):
    """A block carved with the face of the rain god (as on the original's walls)."""
    r = Relief(T, T, SAND, seed=seed)
    wall_block(r)
    # Frame band and recessed panel.
    r.raise_('rrect', (2.6, 2.6, T - 2.6, T - 2.6), 0.78, soft=0.35, r=1.0)
    r.raise_('rrect', (3.6, 3.6, T - 3.6, T - 3.6), 0.62, soft=0.3, r=0.8)
    # Brow.
    r.raise_('rrect', (5.0, 5.0, 19.0, 7.0), 1.02, soft=0.35, r=0.8)
    # Goggle eyes.
    for cx in (8.4, 15.6):
        if closed_eyes:
            r.raise_('ellipse', (cx - 2.6, 8.0, cx + 2.6, 12.2), 0.98, soft=0.4)
            r.raise_('line', [(cx - 2.0, 10.1), (cx + 2.0, 10.1)], 0.7, soft=0.15, width=0.5)
        else:
            r.raise_('ellipse', (cx - 3.0, 7.4, cx + 3.0, 13.4), 1.05, soft=0.35)
            r.raise_('ellipse', (cx - 1.8, 8.6, cx + 1.8, 12.2), 0.7, soft=0.25)
            r.raise_('ellipse', (cx - 0.9, 9.5, cx + 0.9, 11.3), 0.95, soft=0.2)
    # Nose.
    r.raise_('poly', [(10.8, 11.0), (13.2, 11.0), (14.4, 15.2), (9.6, 15.2)], 1.0, soft=0.4)
    # Mouth with fangs.
    r.raise_('rrect', (6.5, 15.8, 17.5, 20.0), 0.32, soft=0.3, r=1.2)
    r.paint('rrect', (6.6, 15.9, 17.4, 19.9), (110, 70, 30), soft=0.3, amount=0.55, r=1.2)
    for fx in (8.0, 10.6, 13.4, 16.0):
        r.raise_('poly', [(fx - 0.9, 15.8), (fx + 0.9, 15.8), (fx, 18.0)], 0.9, soft=0.12)
    for fx in (9.3, 12.0, 14.7):
        r.raise_('poly', [(fx - 0.8, 20.0), (fx + 0.8, 20.0), (fx, 18.4)], 0.85, soft=0.12)
    # Ear spools.
    for cx in (3.9, 20.1):
        r.raise_('ellipse', (cx - 1.0, 10.0, cx + 1.0, 14.0), 0.95, soft=0.25)
    if seam:
        # A secret door: a hairline seam runs round the face.
        m = r.mask('rrect', (2.4, 2.4, T - 2.4, T - 2.4), r=1.0)
        edge = np.asarray(m.filter(ImageFilter.FIND_EDGES), np.float32) / 255
        r.dark = np.maximum(r.dark, blur(edge, 0.6) * 1.6)
    r.grain(0.07, 0.25, specks=0.004)
    r.grain(0.05, 1.5)
    r.render(name, depth=2.4)


def wall_fret(name, seed=3):
    """A block carved with the stepped fret (xicalcoliuhqui) pattern."""
    r = Relief(T, T, SAND, seed=seed)
    wall_block(r)
    r.raise_('rrect', (2.6, 2.6, T - 2.6, T - 2.6), 0.72, soft=0.35, r=1.0)
    path = [(4.5, 19.5), (4.5, 4.5), (19.5, 4.5), (19.5, 15.5), (9.0, 15.5), (9.0, 9.0), (15.0, 9.0), (15.0, 12.0),
            (12.0, 12.0)]
    r.raise_('line', path, 1.0, soft=0.25, width=2.0)
    r.raise_('line', [(4.5, 19.5), (19.5, 19.5)], 1.0, soft=0.25, width=2.0)
    r.grain(0.07, 0.25, specks=0.004)
    r.grain(0.05, 1.5)
    r.render(name, depth=2.4)


def wall_skull(name, seed=5):
    r = Relief(T, T, SAND, seed=seed)
    wall_block(r)
    r.raise_('rrect', (2.6, 2.6, T - 2.6, T - 2.6), 0.62, soft=0.35, r=1.0)
    r.raise_('ellipse', (5.5, 4.0, 18.5, 16.5), 1.1, soft=0.6)
    r.raise_('rrect', (8.0, 13.0, 16.0, 20.0), 1.0, soft=0.4, r=1.2)
    for cx in (9.2, 14.8):
        r.raise_('ellipse', (cx - 2.2, 8.0, cx + 2.2, 12.6), 0.35, soft=0.3)
        r.paint('ellipse', (cx - 2.2, 8.0, cx + 2.2, 12.6), (40, 20, 10), amount=0.7)
    r.raise_('poly', [(12, 12.4), (11, 14.6), (13, 14.6)], 0.5, soft=0.15)
    for x in (9.5, 11.2, 12.8, 14.5):
        r.raise_('line', [(x, 16.0), (x, 19.4)], 0.55, soft=0.1, width=0.35)
    r.paint('rrect', (2.6, 2.6, T - 2.6, T - 2.6), (196, 70, 50), amount=0.18, r=1.0)
    r.grain(0.07, 0.25, specks=0.004)
    r.render(name, depth=2.4)


def stones(r, rng, rows, base, vary=0.12, bevel=0.5):
    """Lays irregular flagstones over a tile (seams at the edges, so tiles join)."""
    y = 0.0
    hs = [T * f for f in rows]
    for i, hgt in enumerate(hs):
        x = 0.0
        while x < T - 0.5:
            w = rng.choice([7, 8, 9, 10, 12, 14])
            w = min(w, T - x) if T - x - w >= 5 else T - x
            r.raise_('rrect', (x + 0.45, y + 0.45, x + w - 0.45, y + hgt - 0.45), 0.55 + rng.random() * 0.12,
                     soft=bevel, r=0.9)
            f = 1 + (rng.random() - 0.5) * 2 * vary
            c = tuple(min(255, int(v * f)) for v in base)
            r.paint('rect', (x + 0.3, y + 0.3, x + w - 0.3, y + hgt - 0.3), c, soft=0.2)
            x += w
        y += hgt


def floor(name, seed, cracks=0, crack_seed=None):
    rng = random.Random(seed)
    r = Relief(T, T, SLATE, seed=seed)
    r.col[:] = np.array((30, 36, 70), np.float32) / 255
    layouts = [[0.5, 0.5], [0.34, 0.33, 0.33], [0.42, 0.58], [0.6, 0.4]]
    stones(r, rng, layouts[seed % len(layouts)], SLATE)
    r.grain(0.09, 0.2, specks=0.006)
    r.grain(0.06, 2.0)
    if cracks:
        cr = random.Random(crack_seed or seed + 99)
        for _ in range(cracks):
            x, y = cr.uniform(3, 21), cr.uniform(3, 21)
            pts = [(x, y)]
            for _ in range(5):
                x += cr.uniform(-3, 3)
                y += cr.uniform(-3, 3)
                pts.append((min(23, max(1, x)), min(23, max(1, y))))
            r.raise_('line', pts, 0.05, soft=0.05, width=0.28)
            r.shadow('line', pts, 0.55, soft=0.05, width=0.25)
    r.render(name, depth=0.6, ambient=0.55, diffuse=0.6)


def sunstone(name):
    rng = random.Random(7)
    r = Relief(T, T, SLATE, seed=7)
    r.col[:] = np.array((30, 36, 70), np.float32) / 255
    stones(r, rng, [0.5, 0.5], SLATE)
    r.raise_('ellipse', (2.0, 2.0, 22.0, 22.0), 0.7, soft=0.5)
    r.paint('ellipse', (2.0, 2.0, 22.0, 22.0), (176, 140, 70), soft=0.3)
    r.raise_('ring', (3.5, 3.5, 20.5, 20.5), 0.85, soft=0.2, width=0.9)
    for i in range(16):
        a = i * math.pi / 8
        r.raise_('poly', [(12 + 9.5 * math.cos(a - 0.12), 12 + 9.5 * math.sin(a - 0.12)),
                          (12 + 9.5 * math.cos(a + 0.12), 12 + 9.5 * math.sin(a + 0.12)),
                          (12 + 7.0 * math.cos(a), 12 + 7.0 * math.sin(a))], 0.9, soft=0.12)
    r.raise_('ring', (6.5, 6.5, 17.5, 17.5), 0.85, soft=0.2, width=0.7)
    r.raise_('ellipse', (8.5, 8.5, 15.5, 15.5), 0.95, soft=0.3)
    for cx, cy in ((10.6, 11.2), (13.4, 11.2)):
        r.raise_('ellipse', (cx - 0.7, cy - 0.7, cx + 0.7, cy + 0.7), 0.7, soft=0.1)
    r.raise_('line', [(10.4, 13.6), (13.6, 13.6)], 0.7, soft=0.1, width=0.5)
    r.grain(0.08, 0.25, specks=0.004)
    r.render(name, depth=0.8, ambient=0.5, diffuse=0.7)


def plate(name):
    rng = random.Random(2)
    r = Relief(T, T, SLATE, seed=2)
    r.col[:] = np.array((30, 36, 70), np.float32) / 255
    stones(r, rng, [0.5, 0.5], SLATE)
    r.raise_('rrect', (5.0, 5.0, 19.0, 19.0), 0.8, soft=0.4, r=1.0)
    r.paint('rrect', (5.0, 5.0, 19.0, 19.0), (66, 76, 130), r=1.0)
    r.raise_('rrect', (7.0, 7.0, 17.0, 17.0), 0.72, soft=0.25, r=0.8)
    r.grain(0.09, 0.2, specks=0.006)
    r.render(name, depth=0.7, ambient=0.55, diffuse=0.6)


def stairs(name):
    r = Relief(T, T, SAND, seed=11)
    steps = 6
    for i in range(steps):
        y0 = i * T / steps
        h = 1.0 - i * 0.14
        r.raise_('rect', (2.0, y0, T - 2.0, y0 + T / steps), h, soft=0.15)
        r.raise_('rect', (2.0, y0, T - 2.0, y0 + 0.8), h + 0.06, soft=0.1)
        r.paint('rect', (2.0, y0, T - 2.0, y0 + T / steps), tuple(int(c * (1 - i * 0.08)) for c in SAND), soft=0.05)
    for x0 in (0.0, T - 2.0):
        r.raise_('rect', (x0, 0, x0 + 2.0, T), 1.2, soft=0.2)
    r.grain(0.07, 0.25, specks=0.004)
    r.render(name, depth=2.4)


def door(name, slab, emblem, seed):
    """A stone door: a heavy slab in a sandstone frame, carved with an emblem."""
    r = Relief(T, T, SAND, seed=seed)
    r.raise_('rect', (0, 0, T, T), 1.1, soft=0.0)
    r.raise_('rrect', (2.5, 1.5, T - 2.5, T - 0.5), 0.5, soft=0.3, r=0.6)
    r.raise_('rrect', (3.4, 2.4, T - 3.4, T - 0.9), 0.8, soft=0.5, r=1.0)
    r.paint('rrect', (3.4, 2.4, T - 3.4, T - 0.9), slab, soft=0.25, r=1.0)
    emblem(r)
    r.grain(0.08, 0.25, specks=0.003)
    r.grain(0.05, 1.5)
    r.render(name, depth=2.4)


def emblem_sun(r):
    r.raise_('ellipse', (7.0, 7.0, 17.0, 17.0), 1.05, soft=0.4)
    r.raise_('ellipse', (9.5, 9.5, 14.5, 14.5), 0.9, soft=0.3)
    for i in range(8):
        a = i * math.pi / 4
        r.raise_('poly', [(12 + 5.2 * math.cos(a - 0.25), 12 + 5.2 * math.sin(a - 0.25)),
                          (12 + 5.2 * math.cos(a + 0.25), 12 + 5.2 * math.sin(a + 0.25)),
                          (12 + 8.0 * math.cos(a), 12 + 8.0 * math.sin(a))], 1.0, soft=0.15)


def emblem_key(r):
    r.raise_('ring', (8.0, 5.5, 16.0, 13.5), 1.08, soft=0.25, width=1.4)
    r.raise_('rect', (11.2, 13.0, 12.8, 20.0), 1.08, soft=0.25)
    r.raise_('rect', (12.5, 16.0, 15.0, 17.3), 1.08, soft=0.2)
    r.raise_('rect', (12.5, 18.5, 14.2, 19.8), 1.08, soft=0.2)
    r.emit('ellipse', (8.0, 5.5, 16.0, 13.5), (40, 90, 60), soft=1.5, amount=0.6)


def emblem_book(r):
    r.raise_('poly', [(5.5, 8.0), (12.0, 9.5), (18.5, 8.0), (18.5, 17.5), (12.0, 19.0), (5.5, 17.5)], 1.05, soft=0.35)
    r.raise_('line', [(12.0, 9.5), (12.0, 19.0)], 0.8, soft=0.1, width=0.5)
    for i in range(4):
        y = 11.0 + i * 1.8
        r.raise_('line', [(7.0, y), (10.8, y + 0.4)], 0.92, soft=0.08, width=0.35)
        r.raise_('line', [(13.2, y + 0.4), (17.0, y)], 0.92, soft=0.08, width=0.35)
    r.paint('poly', [(5.5, 8.0), (12.0, 9.5), (18.5, 8.0), (18.5, 17.5), (12.0, 19.0), (5.5, 17.5)], (190, 60, 70),
            amount=0.5)


def emblem_twins(r):
    for cx in (8.5, 15.5):
        r.raise_('ellipse', (cx - 2.0, 5.0, cx + 2.0, 9.0), 1.08, soft=0.25)
        r.raise_('poly', [(cx - 2.6, 9.5), (cx + 2.6, 9.5), (cx + 3.0, 17.0), (cx - 3.0, 17.0)], 1.05, soft=0.3)
        r.raise_('rect', (cx - 2.4, 17.0, cx - 0.6, 20.5), 1.0, soft=0.2)
        r.raise_('rect', (cx + 0.6, 17.0, cx + 2.4, 20.5), 1.0, soft=0.2)
        r.raise_('poly', [(cx - 3.2, 4.5), (cx, 2.6), (cx + 3.2, 4.5)], 1.0, soft=0.2)
    r.paint('rrect', (4, 3, 20, 21), (238, 200, 90), amount=0.35, r=1)


def door_open(name):
    """An open doorway: the frame alone (drawn over the floor)."""
    r = Relief(T, T, SAND, seed=12)
    r.raise_('rect', (0, 0, 3.0, T), 1.1, soft=0.25)
    r.raise_('rect', (T - 3.0, 0, T, T), 1.1, soft=0.25)
    r.raise_('rect', (0, 0, T, 1.6), 1.1, soft=0.25)
    a1 = r.mask('rect', (0, 0, 3.0, T))
    a2 = r.mask('rect', (T - 3.0, 0, T, T))
    a3 = r.mask('rect', (0, 0, T, 1.6))
    m = np.maximum(np.maximum(r.arr(a1), r.arr(a2)), r.arr(a3))
    r.alpha = m
    r.grain(0.08, 0.25)
    r.render(name, depth=2.4)


def lever(name, up):
    r = Relief(T, T, SAND, seed=21)
    wall_block(r)
    r.raise_('rrect', (2.6, 2.6, T - 2.6, T - 2.6), 0.72, soft=0.35, r=1.0)
    r.raise_('rrect', (10.6, 4.5, 13.4, 19.5), 0.25, soft=0.2, r=0.6)
    r.paint('rrect', (10.6, 4.5, 13.4, 19.5), (40, 26, 14), r=0.6)
    y = 6.5 if up else 17.5
    r.raise_('line', [(12, 12), (12, y)], 1.35, soft=0.2, width=1.2)
    r.paint('line', [(12, 12), (12, y)], (120, 90, 60), width=1.2)
    r.raise_('ellipse', (10.0, y - 2.0, 14.0, y + 2.0), 1.5, soft=0.3)
    r.paint('ellipse', (10.0, y - 2.0, 14.0, y + 2.0), (190, 70, 40))
    r.raise_('ellipse', (10.4, 10.4, 13.6, 13.6), 1.0, soft=0.2)
    r.paint('ellipse', (10.4, 10.4, 13.6, 13.6), (120, 120, 130))
    r.grain(0.07, 0.25)
    r.render(name, depth=2.4)


def tablet(name):
    rng = random.Random(31)
    r = Relief(T, T, SAND, seed=31)
    wall_block(r)
    r.raise_('rrect', (3.0, 3.0, T - 3.0, T - 3.0), 0.55, soft=0.3, r=0.6)
    r.paint('rrect', (3.0, 3.0, T - 3.0, T - 3.0), (236, 214, 160), r=0.6)
    for row in range(3):
        y = 5.0 + row * 5.2
        x = 4.6
        while x < 18.0:
            w = rng.choice([2.6, 3.0, 3.4])
            glyph_marks(r, rng, x, y, w)
            x += w + 0.8
    r.grain(0.06, 0.25)
    r.render(name, depth=2.4)


def glyph_marks(r, rng, x, y, w):
    for _ in range(3):
        kind = rng.randrange(3)
        if kind == 0:
            r.shadow('ellipse', (x + rng.random(), y + rng.random() * 2, x + w - rng.random(), y + 3.5), 0.55, soft=0.05)
        elif kind == 1:
            r.shadow('line', [(x, y + rng.random() * 3.5), (x + w, y + rng.random() * 3.5)], 0.6, soft=0.05,
                     width=0.35)
        else:
            r.shadow('line', [(x + rng.random() * w, y), (x + rng.random() * w, y + 3.6)], 0.6, soft=0.05, width=0.35)


def rosetta(name):
    """The inscription "To the glory of Axayacatl": a long plaque, three blocks wide."""
    r = Relief(3 * T, T, SAND, seed=41)
    for i in range(3):
        r.raise_('rrect', (i * T + 0.7, 0.7, (i + 1) * T - 0.7, T - 0.7), 1.0, soft=0.9, r=1.6)
    r.raise_('rrect', (2.2, 2.6, 3 * T - 2.2, T - 2.6), 1.06, soft=0.4, r=1.0)
    r.raise_('rrect', (3.4, 3.8, 3 * T - 3.4, T - 3.8), 0.92, soft=0.3, r=0.8)
    r.paint('rrect', (3.4, 3.8, 3 * T - 3.4, T - 3.8), (240, 222, 170), r=0.8)
    r.grain(0.05, 0.25)
    r.render(name, depth=2.4)


def launcher(name):
    """A block carved as a serpent's head; the darts fly from its open mouth (facing down)."""
    r = Relief(T, T, SAND, seed=51)
    wall_block(r)
    r.raise_('poly', [(5.0, 3.0), (19.0, 3.0), (20.0, 15.0), (16.0, 22.0), (8.0, 22.0), (4.0, 15.0)], 1.25, soft=0.6)
    for cx in (8.0, 16.0):
        r.raise_('ellipse', (cx - 2.2, 7.0, cx + 2.2, 11.4), 1.45, soft=0.3)
        r.paint('ellipse', (cx - 1.2, 8.0, cx + 1.2, 10.4), (200, 40, 30))
        r.emit('ellipse', (cx - 1.0, 8.2, cx + 1.0, 10.2), (120, 20, 10), soft=0.5)
    for i in range(4):
        y = 12.5 + i * 1.6
        r.raise_('line', [(7.0, y), (12.0, y + 0.8), (17.0, y)], 1.15, soft=0.1, width=0.35)
    r.raise_('ellipse', (9.0, 18.0, 15.0, 23.5), 0.2, soft=0.3)
    r.paint('ellipse', (9.0, 18.0, 15.0, 23.5), (20, 10, 5), amount=0.9)
    r.paint('poly', [(5.0, 3.0), (19.0, 3.0), (20.0, 15.0), (16.0, 22.0), (8.0, 22.0), (4.0, 15.0)], (110, 170, 90),
            amount=0.35)
    r.grain(0.07, 0.25)
    r.render(name, depth=2.4)


def gateway(name):
    """The pyramid's doorway, seen from the jungle."""
    r = Relief(T, T, SAND, seed=61)
    r.raise_('rect', (0, 0, T, T), 1.0, soft=0.0)
    for i in range(3):
        r.raise_('rect', (0, i * 2.0, T, i * 2.0 + 2.0), 1.15 - i * 0.05, soft=0.2)
    r.raise_('poly', [(4.5, 7.0), (19.5, 7.0), (19.5, T), (4.5, T)], 0.0, soft=0.3)
    r.paint('poly', [(4.5, 7.0), (19.5, 7.0), (19.5, T), (4.5, T)], (8, 6, 12))
    r.shadow('poly', [(4.5, 7.0), (19.5, 7.0), (19.5, T), (4.5, T)], 0.8, soft=0.5)
    r.raise_('rect', (2.5, 6.0, 21.5, 7.6), 1.25, soft=0.2)
    for x in (3.5, 20.5):
        r.raise_('rect', (x - 1.2, 7.0, x + 1.2, T), 1.15, soft=0.2)
    r.grain(0.08, 0.25, specks=0.004)
    r.render(name, depth=2.4)


def pit(name):
    rng = random.Random(71)
    r = Relief(T, T, SLATE, seed=71)
    r.col[:] = np.array((30, 36, 70), np.float32) / 255
    stones(r, rng, [0.5, 0.5], SLATE)
    pts = []
    for i in range(14):
        a = i * 2 * math.pi / 14
        rr = 9.0 + rng.uniform(-1.2, 1.0)
        pts.append((12 + rr * math.cos(a), 12 + rr * math.sin(a) * 0.95))
    r.raise_('poly', pts, -0.6, soft=0.4)
    r.paint('poly', pts, (6, 5, 10), soft=0.6)
    inner = [(12 + (x - 12) * 0.7, 12 + (y - 12) * 0.7 + 1.0) for x, y in pts]
    r.shadow('poly', inner, 0.9, soft=1.0)
    r.grain(0.08, 0.2)
    r.render(name, depth=0.7, ambient=0.5)


def chasm(name):
    r = Relief(T, T, (10, 10, 18), seed=81)
    r.grain(0.4, 0.5)
    r.grain(0.3, 2.5)
    r.render(name, depth=0.1, ambient=1.0, diffuse=0.0, ao=0)


def chasm_lip(name):
    """The rock face below the edge of the chasm (drawn under any floor that borders it)."""
    r = Relief(T, 10, (120, 96, 60), seed=82)
    r.raise_('rect', (0, 0, T, 10), 1.0, soft=0)
    rng = random.Random(82)
    for _ in range(14):
        x = rng.uniform(0, T)
        r.raise_('line', [(x, 0), (x + rng.uniform(-2, 2), rng.uniform(4, 10))], 0.6, soft=0.3, width=rng.uniform(1, 2.5))
    yy = np.linspace(0, 1, r.H, dtype=np.float32)[:, None]
    r.dark = np.maximum(r.dark, np.clip(yy * 1.1, 0, 1))
    r.alpha = np.clip(1.4 - yy * 1.4, 0, 1) * np.ones((r.H, r.W), np.float32)
    r.grain(0.15, 0.3)
    r.render(name, depth=1.0)


def rails(name):
    p = Pic(T, T)
    for i in range(4):
        y = 1.5 + i * 6.0
        p.shape('rect', (3.0, y, 21.0, y + 3.2), (150, 100, 56), (92, 58, 30), width=0.3)
        p.flat('line', [(4.5, y + 1.0), (19.5, y + 1.2)], (80, 50, 26), width=0.25)
    for x in (6.0, 18.0):
        p.shape('rect', (x - 0.9, -0.2, x + 0.9, T + 0.2), (196, 200, 214), (90, 94, 110), width=0.25, angle=0)
    p.save(name)


def portcullis(name):
    p = Pic(T, T)
    for x in (3.0, 7.5, 12.0, 16.5, 21.0):
        p.shape('rect', (x - 0.9, -0.5, x + 0.9, T + 0.5), (220, 170, 90), (120, 80, 30), width=0.3, angle=0)
    for y in (4.0, 12.0, 20.0):
        p.shape('rect', (0.5, y - 0.8, T - 0.5, y + 0.8), (200, 150, 70), (110, 70, 28), width=0.3, angle=90)
    for x in (3.0, 7.5, 12.0, 16.5, 21.0):
        p.shape('poly', [(x - 1.2, T - 1.0), (x + 1.2, T - 1.0), (x, T + 0.8)], (230, 190, 110), width=0.2)
    p.save(name)


def water_frames(prefix, frames=6):
    """Tileable rippling water, as an animation cycle."""
    k = PX * SS
    n = T * k
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32) / n * 2 * math.pi
    for f in range(frames):
        ph = f / frames * 2 * math.pi
        v = (np.sin(2 * xx + 1 * yy + ph) + np.sin(-1 * xx + 3 * yy - ph) + np.sin(3 * xx - 2 * yy + 2 * ph) +
             0.6 * np.sin(5 * xx + 4 * yy - 2 * ph))
        caustic = np.clip(1 - np.abs(v) * 0.9, 0, 1) ** 3
        depth = 0.5 + 0.5 * np.sin(1 * xx + 2 * yy + ph) * 0.3
        base = np.array((22, 74, 160), np.float32) / 255
        deep = np.array((12, 40, 104), np.float32) / 255
        rgb = deep[None, None] * (1 - depth[..., None]) + base[None, None] * depth[..., None]
        rgb = rgb + caustic[..., None] * np.array((0.32, 0.5, 0.6), np.float32)
        img = np.dstack([np.clip(rgb, 0, 1) * 255, np.full((n, n), 255)]).astype(np.uint8)
        save(Image.fromarray(img), f'{prefix}{f}', k)


def grass(name, seed):
    p = Pic(T, T)
    rng = random.Random(seed)
    p.flat('rect', (0, 0, T, T), (38, 74, 34))
    for _ in range(70):
        x, y = rng.uniform(-2, T + 2), rng.uniform(-2, T + 2)
        c = rng.choice([(54, 104, 44), (66, 120, 50), (30, 62, 28), (90, 110, 40), (112, 92, 46)])
        a = rng.uniform(0, math.pi)
        L, W = rng.uniform(2.0, 4.0), rng.uniform(0.8, 1.5)
        pts = [(x + L * math.cos(a), y + L * math.sin(a)), (x - W * math.sin(a), y + W * math.cos(a)),
               (x - L * math.cos(a), y - L * math.sin(a)), (x + W * math.sin(a), y - W * math.cos(a))]
        for dx in (-T, 0, T):
            for dy in (-T, 0, T):
                p.flat('poly', [(px + dx, py + dy) for px, py in pts], c)
    p.save(name)


def tree(name, seed):
    """A jungle tree seen from above: a crown of palm fronds (drawn larger than a tile)."""
    S = 40
    p = Pic(S, S)
    rng = random.Random(seed)
    c = S / 2
    p.glow(c + 2.5, c + 3.0, 15, (0, 0, 0), alpha=150)
    fronds = 11
    for layer in range(2):
        for i in range(fronds):
            a = i * 2 * math.pi / fronds + layer * 0.3 + rng.uniform(-0.1, 0.1)
            L = rng.uniform(13, 18) * (0.8 if layer else 1.0)
            tip = (c + L * math.cos(a), c + L * math.sin(a))
            side = 3.2 * (0.8 if layer else 1.0)
            n = (-math.sin(a), math.cos(a))
            mid = (c + L * 0.55 * math.cos(a), c + L * 0.55 * math.sin(a))
            pts = [(c, c), (mid[0] + n[0] * side, mid[1] + n[1] * side), tip,
                   (mid[0] - n[0] * side, mid[1] - n[1] * side)]
            light = (96, 168, 62) if layer else (58, 118, 44)
            dark = (40, 86, 32) if layer else (24, 58, 24)
            p.shape('poly', pts, light, dark, outline=(14, 34, 12), width=0.3, angle=math.degrees(a))
            p.flat('line', [(c, c), tip], (130, 170, 70) if layer else (70, 110, 46), width=0.3)
    p.shape('ellipse', (c - 2.2, c - 2.2, c + 2.2, c + 2.2), (150, 112, 60), (90, 60, 30), width=0.3)
    p.save(name)


def fountain(name):
    r = Relief(T, T, SLATE, seed=91)
    rng = random.Random(91)
    r.col[:] = np.array((30, 36, 70), np.float32) / 255
    stones(r, rng, [0.5, 0.5], SLATE)
    r.raise_('ellipse', (1.0, 1.0, 23.0, 23.0), 1.2, soft=0.5)
    r.paint('ellipse', (1.0, 1.0, 23.0, 23.0), (200, 186, 150))
    r.raise_('ellipse', (3.4, 3.4, 20.6, 20.6), 0.4, soft=0.3)
    r.paint('ellipse', (3.4, 3.4, 20.6, 20.6), (70, 190, 220))
    r.emit('ellipse', (3.4, 3.4, 20.6, 20.6), (20, 60, 70), soft=1.5)
    r.raise_('ellipse', (9.5, 9.5, 14.5, 14.5), 1.3, soft=0.4)
    r.paint('ellipse', (9.5, 9.5, 14.5, 14.5), (210, 200, 170))
    for i in range(8):
        a = i * math.pi / 4
        x, y = 12 + 10.2 * math.cos(a), 12 + 10.2 * math.sin(a)
        r.raise_('ellipse', (x - 0.8, y - 0.8, x + 0.8, y + 0.8), 1.35, soft=0.15)
    r.grain(0.06, 0.25)
    r.render(name, depth=2.4)


def sarcophagus(name, opened):
    r = Relief(T, T, (150, 140, 120), seed=101)
    r.alpha = r.arr(r.mask('rrect', (1.5, 0.5, 22.5, 23.5), r=2.0))
    r.raise_('rrect', (1.5, 0.5, 22.5, 23.5), 1.0, soft=0.6, r=2.0)
    if opened:
        r.raise_('rrect', (4.0, 3.0, 20.0, 21.0), 0.1, soft=0.4, r=1.0)
        r.paint('rrect', (4.0, 3.0, 20.0, 21.0), (20, 14, 10), r=1.0)
        r.shadow('rrect', (4.0, 3.0, 20.0, 21.0), 0.6, soft=1.0, r=1.0)
        r.raise_('rrect', (13.0, 1.0, 23.5, 12.0), 1.4, soft=0.5, r=1.0)
        r.paint('rrect', (13.0, 1.0, 23.5, 12.0), (120, 112, 96), r=1.0)
    else:
        r.raise_('rrect', (3.2, 2.0, 20.8, 22.0), 1.3, soft=0.6, r=1.6)
        r.raise_('ring', (4.2, 3.0, 19.8, 21.0), 1.4, soft=0.2, width=0.6)
        # The jade mask of the king.
        r.raise_('ellipse', (7.0, 4.0, 17.0, 15.5), 1.7, soft=0.6)
        r.paint('ellipse', (7.0, 4.0, 17.0, 15.5), JADE, soft=0.3)
        for cx in (9.8, 14.2):
            r.raise_('ellipse', (cx - 1.4, 7.6, cx + 1.4, 9.4), 1.5, soft=0.2)
            r.paint('ellipse', (cx - 1.4, 7.6, cx + 1.4, 9.4), (240, 240, 230))
            r.paint('ellipse', (cx - 0.5, 8.0, cx + 0.5, 9.0), (20, 20, 20))
        r.raise_('poly', [(11.2, 9.5), (12.8, 9.5), (13.3, 12.0), (10.7, 12.0)], 1.85, soft=0.2)
        r.paint('rrect', (9.6, 12.8, 14.4, 13.8), (180, 40, 40), r=0.4)
        for y in (17.0, 19.0):
            r.raise_('line', [(6.0, y), (18.0, y)], 1.45, soft=0.1, width=0.5)
    r.grain(0.07, 0.25, specks=0.003)
    r.render(name, depth=2.4)


def teleporter(name):
    r = Relief(T, T, SLATE, seed=111)
    rng = random.Random(111)
    r.col[:] = np.array((30, 36, 70), np.float32) / 255
    stones(r, rng, [0.5, 0.5], SLATE)
    r.raise_('ellipse', (1.5, 1.5, 22.5, 22.5), 0.8, soft=0.4)
    r.paint('ellipse', (1.5, 1.5, 22.5, 22.5), (90, 90, 110))
    r.raise_('ring', (4.0, 4.0, 20.0, 20.0), 0.95, soft=0.15, width=1.2)
    r.paint('ring', (4.0, 4.0, 20.0, 20.0), (60, 220, 210), width=1.2)
    r.emit('ring', (4.0, 4.0, 20.0, 20.0), (30, 120, 120), soft=0.6, width=1.2)
    for i in range(4):
        a = math.pi / 4 + i * math.pi / 2
        x, y = 12 + 10 * math.cos(a), 12 + 10 * math.sin(a)
        r.raise_('ellipse', (x - 1.6, y - 1.6, x + 1.6, y + 1.6), 1.6, soft=0.3)
        r.paint('ellipse', (x - 1.6, y - 1.6, x + 1.6, y + 1.6), SAND)
    r.raise_('ellipse', (9, 9, 15, 15), 0.9, soft=0.3)
    r.paint('ellipse', (9, 9, 15, 15), (60, 220, 210))
    r.grain(0.06, 0.25)
    r.render(name, depth=0.8)


# =========================================================================== objects

def skeleton(name, pistol=False):
    p = Pic(T, T)
    bone = (234, 226, 200)
    bone_d = (160, 150, 120)
    p.glow(12, 13, 10, (0, 0, 0), alpha=80)
    # Spine and ribs.
    p.shape('line', [(12, 8), (12.4, 16)], bone, bone_d, width=0.3)
    for i in range(4):
        y = 9.5 + i * 1.6
        p.shape('line', [(8.5, y + 0.6), (12, y), (15.5, y + 0.6)], bone, bone_d, width=0.3)
    # Pelvis, limbs.
    p.shape('ellipse', (10.0, 15.6, 15.0, 18.0), bone, bone_d, width=0.3)
    for pts in ([(8.6, 10), (5.0, 13.5), (3.5, 17.0)], [(15.4, 10), (18.5, 12.0), (20.0, 15.5)],
                [(11.0, 17.6), (9.5, 21.5), (9.0, 23.0)], [(14.0, 17.6), (16.0, 21.0), (17.5, 23.0)]):
        p.shape('line', pts, bone, bone_d, width=0.3)
    # Skull.
    p.shape('ellipse', (8.8, 2.0, 15.2, 8.4), bone, bone_d, width=0.35)
    for cx in (10.7, 13.3):
        p.flat('ellipse', (cx - 0.9, 4.2, cx + 0.9, 6.0), (30, 20, 16))
    p.flat('poly', [(12, 6.2), (11.5, 7.1), (12.5, 7.1)], (30, 20, 16))
    if pistol:
        p.shape('poly', [(18.5, 15.0), (23.0, 14.2), (23.2, 15.8), (20.5, 16.4), (20.0, 18.5), (18.6, 18.4)],
                (120, 210, 200), (40, 110, 110), width=0.35)
        p.glow(22.5, 15.0, 2.0, (90, 255, 230), alpha=180)
    p.save(name)


def chest(name, opened=False):
    p = Pic(T, T)
    p.glow(12.5, 15.5, 10, (0, 0, 0), alpha=110)
    if opened:
        p.shape('rrect', (4.0, 4.0, 20.0, 9.5), (130, 80, 40), (80, 46, 20), r=0.8)
        p.shape('rrect', (4.0, 9.0, 20.0, 20.0), (40, 24, 12), (20, 12, 6), r=0.8)
        p.shape('rect', (4.0, 15.0, 20.0, 20.0), (150, 96, 50), (96, 58, 26))
    else:
        p.shape('rrect', (4.0, 6.0, 20.0, 20.0), (164, 104, 54), (98, 58, 26), r=1.0)
        p.shape('rrect', (3.5, 5.0, 20.5, 11.0), (190, 124, 66), (120, 74, 34), r=1.2)
        for x in (6.0, 18.0):
            p.shape('rect', (x - 0.8, 5.0, x + 0.8, 20.0), GOLD_L, GOLD_D, width=0.3)
        p.shape('rrect', (10.5, 9.5, 13.5, 13.5), GOLD_L, GOLD_D, r=0.4, width=0.3)
        p.flat('ellipse', (11.6, 10.8, 12.4, 11.8), (40, 20, 10))
    p.save(name)


def gem(name):
    p = Pic(14, 14)
    p.glow(7, 7, 6.5, (60, 255, 220), alpha=120)
    pts = [(7, 1.5), (12, 5.5), (7, 12.5), (2, 5.5)]
    p.shape('poly', pts, (130, 255, 230), (20, 140, 140), width=0.35)
    p.flat('poly', [(7, 1.5), (9.5, 5.5), (7, 6.5), (4.5, 5.5)], (220, 255, 250), alpha=200)
    p.flat('poly', [(2, 5.5), (12, 5.5), (11.4, 6.2), (2.6, 6.2)], (255, 255, 255), alpha=120)
    p.save(name)


def ammo(name):
    p = Pic(14, 14)
    p.glow(7, 8, 6, (0, 0, 0), alpha=100)
    p.shape('rrect', (2.5, 3.5, 11.5, 12.5), (110, 120, 140), (50, 56, 70), r=1.0)
    p.shape('rrect', (4.0, 1.8, 10.0, 4.2), (150, 160, 176), (80, 86, 100), r=0.6)
    p.shape('poly', [(7.8, 4.8), (4.8, 8.6), (6.9, 8.6), (6.0, 11.4), (9.4, 7.4), (7.3, 7.4)], (120, 255, 230),
            (30, 170, 160), width=0.25)
    p.glow(7, 8, 3.5, (80, 255, 230), alpha=110)
    p.save(name)


def jade_key(name):
    p = Pic(14, 14)
    p.glow(7, 7, 6.5, (40, 255, 120), alpha=90)
    p.shape('ellipse', (1.5, 1.5, 7.5, 7.5), (120, 230, 150), (30, 120, 70), width=0.35)
    p.flat('ellipse', (3.3, 3.3, 5.7, 5.7), (20, 60, 30))
    p.shape('poly', [(6.2, 5.2), (12.6, 11.6), (11.4, 12.8), (5.0, 6.4)], (120, 230, 150), (30, 120, 70), width=0.35)
    p.shape('poly', [(9.6, 10.8), (11.2, 9.2), (12.4, 10.4), (10.8, 12.0)], (120, 230, 150), (30, 120, 70), width=0.3)
    p.shape('poly', [(8.0, 9.2), (9.6, 7.6), (10.6, 8.6), (9.0, 10.2)], (120, 230, 150), (30, 120, 70), width=0.3)
    p.save(name)


def book(name):
    p = Pic(14, 14)
    p.glow(7, 7, 6.5, (255, 120, 120), alpha=70)
    p.shape('poly', [(1.0, 3.0), (7.0, 4.2), (13.0, 3.0), (13.0, 11.5), (7.0, 12.8), (1.0, 11.5)], (240, 226, 190),
            (170, 150, 110), width=0.35)
    p.flat('line', [(7.0, 4.2), (7.0, 12.8)], (120, 90, 60), width=0.3)
    for i in range(4):
        y = 5.4 + i * 1.5
        for x0, x1 in ((2.2, 5.8), (8.2, 11.8)):
            p.flat('line', [(x0, y), (x1, y + 0.3)], (190, 50, 50) if i % 2 else (40, 90, 160), width=0.4)
    p.shape('rect', (0.4, 2.6, 13.6, 3.2), (190, 40, 50), width=0.2)
    p.save(name)


def idol(name):
    p = Pic(14, 14)
    p.glow(7, 7, 6.5, (255, 200, 60), alpha=110)
    p.shape('poly', [(4.4, 0.8), (7.0, -0.2), (9.6, 0.8), (9.2, 2.4), (4.8, 2.4)], GOLD_L, GOLD_D, width=0.3)
    p.shape('ellipse', (4.6, 1.8, 9.4, 6.2), GOLD_L, GOLD_D, width=0.35)
    p.flat('rect', (5.6, 3.4, 6.6, 4.2), INK)
    p.flat('rect', (7.4, 3.4, 8.4, 4.2), INK)
    p.shape('poly', [(4.2, 6.0), (9.8, 6.0), (10.6, 10.6), (3.4, 10.6)], GOLD, GOLD_D, width=0.35)
    p.shape('rect', (4.0, 10.4, 6.4, 13.4), GOLD, GOLD_D, width=0.3)
    p.shape('rect', (7.6, 10.4, 10.0, 13.4), GOLD, GOLD_D, width=0.3)
    p.flat('ellipse', (6.3, 7.4, 7.7, 8.8), (60, 220, 200))
    p.save(name)


def helmet(name):
    p = Pic(14, 14)
    p.glow(7, 8, 6, (0, 0, 0), alpha=90)
    p.shape('ellipse', (1.2, 1.0, 12.8, 12.6), (236, 184, 92), (150, 96, 30), width=0.4)
    p.shape('ellipse', (3.6, 3.6, 10.4, 10.0), (170, 220, 240), (40, 90, 130), width=0.5)
    p.flat('ellipse', (4.6, 4.4, 7.0, 6.4), (255, 255, 255), alpha=170)
    for a in range(0, 360, 45):
        x, y = 7 + 4.6 * math.cos(math.radians(a)), 6.8 + 4.4 * math.sin(math.radians(a))
        p.flat('ellipse', (x - 0.45, y - 0.45, x + 0.45, y + 0.45), (120, 80, 30))
    p.shape('rect', (2.0, 11.4, 12.0, 13.2), (200, 150, 70), (120, 80, 30), width=0.3)
    p.save(name)


def stone(name):
    """The miraculous stone: a glowing crystal."""
    p = Pic(14, 14)
    p.glow(7, 7, 7, (120, 255, 255), alpha=200)
    p.glow(7, 7, 4, (255, 255, 255), alpha=200)
    pts = [(7, 0.8), (11.6, 4.0), (11.0, 10.0), (7, 13.2), (3.0, 10.0), (2.4, 4.0)]
    p.shape('poly', pts, (200, 255, 255), (40, 170, 210), width=0.35)
    p.flat('poly', [(7, 0.8), (11.6, 4.0), (7, 6.0), (2.4, 4.0)], (255, 255, 255), alpha=190)
    p.flat('poly', [(7, 6.0), (11.0, 10.0), (7, 13.2)], (60, 160, 220), alpha=140)
    p.save(name)


def life(name):
    """The explorer's hat: one life."""
    p = Pic(14, 14)
    p.shape('ellipse', (0.6, 6.0, 13.4, 11.4), KHAKI, KHAKI_D, width=0.45)
    p.shape('ellipse', (3.0, 1.6, 11.0, 9.0), (226, 204, 146), KHAKI_D, width=0.45)
    p.flat('rect', (3.2, 6.4, 10.8, 7.4), BROWN)
    p.save(name)


def pistol(name):
    p = Pic(14, 14)
    p.shape('poly', [(1.0, 4.2), (12.6, 3.0), (13.0, 6.2), (7.0, 7.0), (6.2, 11.8), (3.0, 11.8), (3.6, 6.6), (1.0, 6.6)],
            (120, 210, 200), (40, 110, 110), width=0.4)
    p.flat('rect', (8.0, 4.0, 11.6, 4.8), (220, 255, 250))
    p.glow(12.8, 4.4, 1.8, (90, 255, 230), alpha=200)
    p.save(name)


def boulder(name):
    S = 22
    r = Relief(S, S, (150, 130, 100), seed=121)
    yy, xx = np.mgrid[0:r.H, 0:r.W].astype(np.float32) / r.k
    d = np.sqrt((xx - S / 2) ** 2 + (yy - S / 2) ** 2) / (S / 2 - 0.5)
    r.hgt = np.sqrt(np.clip(1 - d * d, 0, 1)) * 6
    r.alpha = np.clip((1 - d) * 30, 0, 1)
    rng = random.Random(121)
    for _ in range(5):
        x, y = rng.uniform(4, 18), rng.uniform(4, 18)
        pts = [(x, y)]
        for _ in range(3):
            x += rng.uniform(-3, 3)
            y += rng.uniform(-3, 3)
            pts.append((x, y))
        r.shadow('line', pts, 0.6, soft=0.08, width=0.3)
    r.grain(0.12, 0.3, specks=0.01)
    r.render(name, depth=0.25, ao=0)


def cart(name):
    p = Pic(20, 24)
    p.glow(10, 13, 10, (0, 0, 0), alpha=120)
    for y in (4.0, 20.0):
        for x in (2.8, 17.2):
            p.shape('rrect', (x - 1.4, y - 2.4, x + 1.4, y + 2.4), (80, 80, 90), (30, 30, 40), r=0.6, width=0.3)
    p.shape('rrect', (2.5, 1.5, 17.5, 22.5), (150, 96, 50), (90, 54, 24), r=1.2)
    p.shape('rrect', (4.2, 3.2, 15.8, 20.8), (70, 44, 22), (40, 24, 10), r=0.8)
    for y in (2.2, 11.4, 21.0):
        p.shape('rect', (2.4, y, 17.6, y + 1.2), (160, 160, 170), (80, 80, 90), width=0.25)
    p.save(name)


# =========================================================================== figures

def hero(name, facing, frame, swim=False):
    """The archaeologist: pith helmet, khaki shirt, laser pistol (facing down, up or right)."""
    W, H = 18, 26
    p = Pic(W, H)
    step = 1.6 if frame else -1.6
    if not swim:
        p.glow(9, 24.0, 7, (0, 0, 0), alpha=120)
    if facing == 'down':
        if not swim:
            # Legs and boots.
            p.shape('rrect', (5.2, 15.5 + max(0, -step), 8.6, 22.5 + min(0, -step) + 0.8), BROWN, BROWN_D, r=0.8)
            p.shape('rrect', (9.4, 15.5 + max(0, step), 12.8, 22.5 + min(0, step) + 0.8), BROWN, BROWN_D, r=0.8)
            p.shape('rrect', (4.8, 21.0 - step * 0.5, 8.8, 24.2 - step * 0.5), (70, 46, 26), (40, 24, 12), r=0.8)
            p.shape('rrect', (9.2, 21.0 + step * 0.5, 13.2, 24.2 + step * 0.5), (70, 46, 26), (40, 24, 12), r=0.8)
        # Body, belt, arms.
        p.shape('rrect', (4.2, 9.0, 13.8, 17.0), KHAKI, KHAKI_D, r=2.0)
        p.flat('rect', (4.6, 15.0, 13.4, 16.2), (90, 56, 28))
        p.flat('rect', (8.4, 15.0, 9.6, 16.2), GOLD)
        p.shape('rrect', (1.8, 9.6 + step * 0.4, 4.8, 16.0 + step * 0.4), KHAKI, KHAKI_D, r=1.2)
        p.shape('rrect', (13.2, 9.6 - step * 0.4, 16.2, 16.0 - step * 0.4), KHAKI, KHAKI_D, r=1.2)
        p.shape('ellipse', (13.0, 15.2 - step * 0.4, 16.4, 18.0 - step * 0.4), SKIN, SKIN_D, width=0.35)
        p.shape('rect', (13.8, 16.6 - step * 0.4, 15.8, 20.2 - step * 0.4), (120, 210, 200), (40, 110, 110), width=0.3)
        p.shape('ellipse', (1.6, 15.2 + step * 0.4, 5.0, 18.0 + step * 0.4), SKIN, SKIN_D, width=0.35)
        # Head and helmet.
        p.shape('ellipse', (5.4, 4.8, 12.6, 11.4), SKIN, SKIN_D, width=0.4)
        p.flat('ellipse', (6.9, 7.6, 8.1, 8.8), INK)
        p.flat('ellipse', (9.9, 7.6, 11.1, 8.8), INK)
        p.flat('line', [(7.8, 10.0), (10.2, 10.0)], (150, 70, 50), width=0.4)
        p.shape('ellipse', (2.8, 3.4, 15.2, 7.4), (226, 204, 146), KHAKI_D, width=0.4)
        p.shape('ellipse', (5.0, 0.4, 13.0, 6.2), (240, 222, 170), KHAKI_D, width=0.4)
        p.flat('rect', (5.2, 4.4, 12.8, 5.2), BROWN)
    elif facing == 'up':
        if not swim:
            p.shape('rrect', (5.2, 15.5 + max(0, step), 8.6, 22.5 + min(0, step) + 0.8), BROWN, BROWN_D, r=0.8)
            p.shape('rrect', (9.4, 15.5 + max(0, -step), 12.8, 22.5 + min(0, -step) + 0.8), BROWN, BROWN_D, r=0.8)
            p.shape('rrect', (4.8, 21.0 + step * 0.5, 8.8, 24.2 + step * 0.5), (70, 46, 26), (40, 24, 12), r=0.8)
            p.shape('rrect', (9.2, 21.0 - step * 0.5, 13.2, 24.2 - step * 0.5), (70, 46, 26), (40, 24, 12), r=0.8)
        p.shape('rrect', (1.8, 9.6 - step * 0.4, 4.8, 16.0 - step * 0.4), KHAKI, KHAKI_D, r=1.2)
        p.shape('rrect', (13.2, 9.6 + step * 0.4, 16.2, 16.0 + step * 0.4), KHAKI, KHAKI_D, r=1.2)
        p.shape('rrect', (4.2, 9.0, 13.8, 17.0), KHAKI, KHAKI_D, r=2.0)
        # Satchel and its strap.
        p.flat('line', [(4.8, 9.6), (13.0, 15.6)], BROWN_D, width=0.8)
        p.shape('rrect', (5.6, 10.6, 12.4, 15.6), (150, 100, 56), (96, 60, 30), r=1.0)
        p.shape('ellipse', (5.4, 4.8, 12.6, 11.4), (110, 70, 40), (70, 40, 20), width=0.4)
        p.shape('ellipse', (2.8, 3.4, 15.2, 7.4), (226, 204, 146), KHAKI_D, width=0.4)
        p.shape('ellipse', (5.0, 0.6, 13.0, 6.6), (240, 222, 170), KHAKI_D, width=0.4)
        p.flat('rect', (5.2, 4.6, 12.8, 5.4), BROWN)
    else:  # right
        if not swim:
            p.shape('rrect', (6.0 + step, 15.5, 9.4 + step, 22.8), BROWN, BROWN_D, r=0.8)
            p.shape('rrect', (8.6 - step, 15.5, 12.0 - step, 22.8), (100, 62, 32), BROWN_D, r=0.8)
            p.shape('rrect', (5.4 + step * 1.3, 21.0, 10.4 + step * 1.3, 24.2), (70, 46, 26), (40, 24, 12), r=0.8)
            p.shape('rrect', (8.0 - step * 1.3, 21.0, 13.0 - step * 1.3, 24.2), (60, 40, 22), (40, 24, 12), r=0.8)
        p.shape('rrect', (5.0, 9.0, 12.4, 17.0), KHAKI, KHAKI_D, r=2.0)
        p.shape('rrect', (3.6, 10.6, 6.6, 15.4), (150, 100, 56), (96, 60, 30), r=1.0)
        p.flat('rect', (5.4, 15.0, 12.0, 16.2), (90, 56, 28))
        # Pistol arm, held forward.
        p.shape('rrect', (8.4, 10.6, 15.2, 13.2), KHAKI, KHAKI_D, r=1.0)
        p.shape('ellipse', (13.6, 10.4, 16.4, 13.4), SKIN, SKIN_D, width=0.35)
        p.shape('poly', [(14.2, 10.2), (17.8, 9.8), (17.8, 11.2), (15.6, 11.6), (15.4, 13.0), (14.2, 13.0)],
                (120, 210, 200), (40, 110, 110), width=0.3)
        p.shape('ellipse', (6.0, 4.8, 13.0, 11.4), SKIN, SKIN_D, width=0.4)
        p.flat('ellipse', (10.8, 7.4, 11.9, 8.6), INK)
        p.flat('poly', [(12.8, 8.2), (14.0, 9.4), (12.6, 9.6)], SKIN_D)
        p.shape('ellipse', (3.2, 3.6, 16.0, 7.4), (226, 204, 146), KHAKI_D, width=0.4)
        p.shape('ellipse', (5.4, 0.4, 13.2, 6.2), (240, 222, 170), KHAKI_D, width=0.4)
        p.flat('rect', (5.6, 4.4, 13.0, 5.2), BROWN)
    p.save(name)


def ghoul(name, frame):
    """The tomb's ghoul: pale, hunched, red-eyed, claws reaching out."""
    W, H = 22, 24
    p = Pic(W, H)
    s = 1.4 if frame else -1.4
    pale = (196, 206, 168)
    pale_d = (110, 120, 90)
    p.glow(11, 22.5, 8, (0, 0, 0), alpha=120)
    # Legs.
    p.shape('poly', [(6.5, 15.0), (9.6, 15.0), (9.0 + s * 0.6, 22.5), (5.6 + s * 0.6, 22.5)], pale, pale_d)
    p.shape('poly', [(12.4, 15.0), (15.5, 15.0), (16.4 - s * 0.6, 22.5), (13.0 - s * 0.6, 22.5)], pale, pale_d)
    # Ragged loincloth.
    p.shape('poly', [(5.8, 14.0), (16.2, 14.0), (15.4, 18.2), (13.0, 16.6), (11.0, 18.6), (9.0, 16.6), (6.6, 18.2)],
            (120, 90, 60), (70, 50, 30))
    # Arms reaching out.
    p.shape('poly', [(5.5, 8.5), (7.6, 9.5), (4.2, 14.5 + s), (1.6, 13.8 + s)], pale, pale_d)
    p.shape('poly', [(16.5, 8.5), (14.4, 9.5), (17.8, 14.5 - s), (20.4, 13.8 - s)], pale, pale_d)
    for x, y in ((1.0, 14.0 + s), (2.6, 15.4 + s), (4.2, 15.6 + s), (21.0, 14.0 - s), (19.4, 15.4 - s), (17.8, 15.6 - s)):
        p.flat('poly', [(x - 0.5, y - 0.6), (x + 0.5, y - 0.6), (x, y + 1.2)], (60, 40, 30))
    # Torso with ribs.
    p.shape('poly', [(6.2, 7.6), (15.8, 7.6), (15.0, 14.6), (7.0, 14.6)], pale, pale_d)
    for i in range(3):
        y = 9.4 + i * 1.6
        p.flat('line', [(8.0, y), (11.0, y + 0.5), (14.0, y)], pale_d, width=0.3)
    # Head: bald, pointed ears, red eyes, fangs.
    p.shape('poly', [(5.2, 3.0), (7.2, 4.6), (6.6, 6.4)], pale, pale_d, width=0.3)
    p.shape('poly', [(16.8, 3.0), (14.8, 4.6), (15.4, 6.4)], pale, pale_d, width=0.3)
    p.shape('ellipse', (6.6, 0.8, 15.4, 9.4), pale, pale_d, width=0.45)
    for cx in (9.2, 12.8):
        p.glow(cx, 4.6, 1.8, (255, 40, 20), alpha=200)
        p.flat('ellipse', (cx - 1.0, 3.8, cx + 1.0, 5.4), (255, 60, 40))
    p.flat('rrect', (8.6, 6.6, 13.4, 8.0), (60, 20, 20), r=0.4)
    for x in (9.6, 12.4):
        p.flat('poly', [(x - 0.4, 6.6), (x + 0.4, 6.6), (x, 7.7)], (250, 250, 240))
    p.save(name)


def guardian(name, frame):
    """The armoured guardian: a horned giant of black iron that no laser can harm."""
    W, H = 26, 28
    p = Pic(W, H)
    s = 1.2 if frame else -1.2
    iron = (86, 90, 108)
    iron_d = (30, 32, 44)
    edge = (110, 150, 255)
    p.glow(13, 26.5, 10, (0, 0, 0), alpha=140)
    p.shape('rrect', (7.0, 17.0 + max(0, s), 12.0, 26.0 + min(0, s)), iron, iron_d, r=1.0)
    p.shape('rrect', (14.0, 17.0 + max(0, -s), 19.0, 26.0 + min(0, -s)), iron, iron_d, r=1.0)
    p.shape('rrect', (1.6, 9.0 - s * 0.5, 6.6, 20.0 - s * 0.5), iron, iron_d, r=1.6)
    p.shape('rrect', (19.4, 9.0 + s * 0.5, 24.4, 20.0 + s * 0.5), iron, iron_d, r=1.6)
    p.shape('ellipse', (1.4, 17.6 - s * 0.5, 6.8, 22.4 - s * 0.5), iron, iron_d)
    p.shape('ellipse', (19.2, 17.6 + s * 0.5, 24.6, 22.4 + s * 0.5), iron, iron_d)
    p.shape('rrect', (5.6, 7.6, 20.4, 19.6), iron, iron_d, r=2.4)
    for y in (11.0, 14.0, 17.0):
        p.flat('line', [(7.0, y), (19.0, y)], iron_d, width=0.4)
        p.flat('line', [(7.0, y - 0.5), (19.0, y - 0.5)], edge, width=0.2, alpha=150)
    p.shape('ellipse', (0.6, 6.4, 8.6, 12.0), (104, 108, 126), iron_d)
    p.shape('ellipse', (17.4, 6.4, 25.4, 12.0), (104, 108, 126), iron_d)
    # Horned helm.
    p.shape('poly', [(8.0, 4.0), (4.0, 0.4), (3.0, 2.4), (6.6, 6.6)], (220, 210, 190), (120, 110, 90), width=0.35)
    p.shape('poly', [(18.0, 4.0), (22.0, 0.4), (23.0, 2.4), (19.4, 6.6)], (220, 210, 190), (120, 110, 90), width=0.35)
    p.shape('rrect', (7.6, 1.6, 18.4, 10.2), iron, iron_d, r=2.0)
    p.flat('rect', (8.6, 5.0, 17.4, 6.6), (14, 10, 16))
    for cx in (10.8, 15.2):
        p.glow(cx, 5.8, 2.2, (255, 30, 20), alpha=220)
        p.flat('rect', (cx - 1.4, 5.3, cx + 1.4, 6.3), (255, 80, 50))
    for y in (8.0, 9.0):
        p.flat('line', [(9.0, y), (17.0, y)], iron_d, width=0.3)
    p.flat('line', [(7.8, 2.0), (18.2, 2.0)], edge, width=0.25, alpha=170)
    p.save(name)


def fish(name, frame):
    W, H = 18, 11
    p = Pic(W, H)
    t = 1.0 if frame else -1.0
    p.shape('poly', [(2.6, 5.5), (0.2, 2.2 + t), (0.6, 8.8 + t)], (210, 90, 70), (130, 40, 30), width=0.35)
    p.shape('ellipse', (2.0, 1.0, 16.8, 10.0), (130, 140, 150), (60, 66, 80), width=0.4)
    p.shape('poly', [(5.0, 6.8), (15.4, 6.6), (12.0, 9.8), (6.0, 9.4)], (230, 90, 60), (150, 40, 30), width=0.0)
    p.shape('poly', [(8.0, 1.4), (11.0, -0.4), (12.0, 1.6)], (120, 130, 140), width=0.3)
    p.flat('ellipse', (12.4, 3.0, 14.4, 5.0), (250, 240, 120))
    p.flat('ellipse', (13.1, 3.6, 14.0, 4.5), INK)
    p.flat('poly', [(16.8, 5.6), (13.0, 6.2), (16.4, 7.2)], (40, 10, 10))
    for x in (14.2, 15.2, 16.0):
        p.flat('poly', [(x - 0.35, 6.0), (x + 0.35, 6.0), (x, 6.9)], (255, 255, 255))
    p.save(name)


# =========================================================================== touch buttons

def button_icons():
    white, grey = (255, 255, 255), (200, 210, 230)
    p = Pic(14, 14)
    arc = [(1.5 + t * 8.5, 12.5 - 9.0 * math.sin(t * math.pi * 0.62)) for t in [i / 12 for i in range(13)]]
    p.shape('line', arc, white, grey, outline=None, lw=1.8)
    p.shape('poly', [(8.2, 1.4), (13.2, 3.2), (9.4, 6.8)], white, grey, outline=None)
    p.shape('rect', (0.6, 12.6, 6.0, 13.6), white, grey, outline=None)
    p.save('BtnJump')
    p = Pic(14, 14)
    for i, (x, top) in enumerate([(3.4, 4.0), (5.8, 1.6), (8.2, 1.2), (10.6, 2.6)]):
        p.shape('rrect', (x - 1.0, top, x + 1.0, 9.0), white, grey, outline=None, r=1.0)
    p.shape('rrect', (2.4, 6.0, 11.6, 13.0), white, grey, outline=None, r=2.6)
    p.shape('rrect', (0.4, 6.6, 2.6, 10.6), white, grey, outline=None, r=1.0)
    p.save('BtnAct')
    p = Pic(14, 14)
    p.shape('rrect', (3.0, 2.0, 6.0, 12.0), white, grey, outline=None, r=0.6)
    p.shape('rrect', (8.0, 2.0, 11.0, 12.0), white, grey, outline=None, r=0.6)
    p.save('BtnPause')


# =========================================================================== title picture

def title_picture(name, w=240, h=150, px=8):
    """The pyramid in the jungle at dusk, its temple glowing (title, menus and store art)."""
    p = Pic(w, h, px)
    rng = random.Random(1985)
    sky = p.gradient((26, 18, 64), (232, 120, 70), (0, 0, w, h * 0.8), angle=90)
    p.im = sky
    # Stars and moon.
    for _ in range(90):
        x, y = rng.uniform(0, w), rng.uniform(0, h * 0.4)
        rr = rng.choice([0.25, 0.3, 0.4, 0.55])
        p.flat('ellipse', (x - rr, y - rr, x + rr, y + rr), (255, 255, 240), alpha=rng.randint(120, 255))
    p.glow(196, 26, 22, (255, 240, 200), alpha=90)
    p.flat('ellipse', (188, 18, 204, 34), (255, 248, 222))
    p.flat('ellipse', (192, 16, 208, 32), (230, 150, 110), alpha=60)
    # Far hills.
    hill = [(0, 104)] + [(x, 98 + 6 * math.sin(x / 17.0) + 3 * math.sin(x / 7.0)) for x in range(0, w + 1, 4)] + [(w, 104), (w, h), (0, h)]
    p.flat('poly', hill, (60, 34, 70))
    # The pyramid: nine stepped tiers, a central stairway and the temple.
    base_y, top_y = 130.0, 46.0
    tiers = 9
    cx = 120.0
    for i in range(tiers):
        y1 = base_y - i * (base_y - top_y) / tiers
        y0 = y1 - (base_y - top_y) / tiers
        hw1 = 96 - i * 8.4
        hw0 = hw1 - 3.0
        p.shape('poly', [(cx - hw0, y0), (cx + hw0, y0), (cx + hw1, y1), (cx - hw1, y1)], (226, 170, 84),
                (120, 70, 40), outline=(60, 30, 22), width=0.35, angle=10)
        p.flat('poly', [(cx - hw0, y0), (cx - hw0 + 1.2, y0), (cx - hw1 + 1.2, y1), (cx - hw1, y1)], (255, 214, 140),
               alpha=110)
        p.flat('poly', [(cx + hw0 - 2.2, y0), (cx + hw0, y0), (cx + hw1, y1), (cx + hw1 - 2.2, y1)], (60, 30, 30),
               alpha=110)
        # Carved frieze on each tier.
        for x in np.arange(cx - hw0 + 4, cx + hw0 - 3, 6.0):
            p.flat('rect', (x, y0 + 2.4, x + 3.2, y1 - 2.0), (150, 96, 50), alpha=90)
    # Stairway.
    p.shape('poly', [(cx - 9, top_y), (cx + 9, top_y), (cx + 16, base_y), (cx - 16, base_y)], (238, 196, 120),
            (150, 96, 56), outline=(60, 30, 22), width=0.3, angle=90)
    for i in range(36):
        y = top_y + i * (base_y - top_y) / 36
        hw = 9 + 7 * (y - top_y) / (base_y - top_y)
        p.flat('line', [(cx - hw, y), (cx + hw, y)], (120, 70, 40), width=0.35)
    for sgn in (-1, 1):
        p.flat('poly', [(cx + sgn * 9, top_y), (cx + sgn * 11, top_y), (cx + sgn * 19, base_y), (cx + sgn * 16, base_y)],
               (190, 130, 70))
    # Temple on top.
    p.shape('rect', (cx - 16, 26, cx + 16, top_y), (230, 180, 96), (140, 86, 44), outline=(60, 30, 22), width=0.35)
    p.shape('poly', [(cx - 19, 26), (cx + 19, 26), (cx + 14, 18), (cx - 14, 18)], (200, 70, 50), (120, 30, 26),
            outline=(60, 30, 22), width=0.35)
    for x in np.arange(cx - 12, cx + 12, 4.0):
        p.flat('rect', (x, 20.0, x + 2.0, 24.0), (250, 210, 120), alpha=170)
    p.glow(cx, 38, 16, (255, 220, 120), alpha=180)
    p.flat('rect', (cx - 5.5, 31, cx + 5.5, top_y), (255, 236, 170))
    p.glow(cx, 38, 7, (255, 255, 230), alpha=200)
    # Jungle foreground.
    for layer, (col, base) in enumerate([((22, 52, 30), 128), ((12, 34, 20), 140)]):
        for i in range(30):
            x = rng.uniform(-10, w + 10)
            rr = rng.uniform(10, 20) * (1.2 if layer else 1.0)
            y = base + rng.uniform(-6, 6)
            if layer == 0 and abs(x - cx) < 70:
                y += 12
            p.flat('ellipse', (x - rr, y - rr * 0.8, x + rr, y + rr * 0.8), col)
        p.flat('rect', (0, base + 4, w, h), col)
    for i in range(10):
        x = rng.uniform(0, w)
        if abs(x - cx) < 60:
            continue
        top = rng.uniform(60, 90)
        p.flat('line', [(x, h), (x + rng.uniform(-4, 4), top)], (16, 30, 18), width=1.6)
        for k in range(7):
            a = -math.pi / 2 + (k - 3) * 0.5
            p.flat('line', [(x, top), (x + 16 * math.cos(a) * 1.3, top + 16 * math.sin(a) + 9)], (14, 34, 18), width=2.0)
    p.save(name)


# =========================================================================== main

def main():
    random.seed(1985)
    wall_face('Wall0', seed=1)
    wall_face('Wall1', closed_eyes=True, seed=2)
    wall_fret('Wall2')
    wall_face('Secret', seam=True, seed=1)
    wall_skull('SkullWall')
    for i in range(4):
        floor(f'Floor{i}', seed=i + 1)
    floor('Cracked', seed=2, cracks=4)
    sunstone('SunStone')
    plate('Plate')
    stairs('Stairs')
    door('Door', (150, 110, 70), emblem_sun, 13)
    door('JadeDoor', JADE, emblem_key, 14)
    door('BookGate', (110, 60, 120), emblem_book, 15)
    door('TwinGate', (230, 226, 210), emblem_twins, 16)
    door_open('DoorOpen')
    lever('LeverUp', True)
    lever('LeverDown', False)
    tablet('Tablet')
    rosetta('Rosetta')
    launcher('Launcher')
    gateway('Gateway')
    pit('Pit')
    chasm('Chasm')
    chasm_lip('ChasmLip')
    rails('Rails')
    portcullis('Portcullis')
    water_frames('Water')
    grass('Grass0', 1)
    grass('Grass1', 2)
    tree('Tree0', 1)
    tree('Tree1', 2)
    fountain('Fountain')
    sarcophagus('Sarcophagus', False)
    sarcophagus('SarcophagusOpen', True)
    teleporter('Teleporter')
    skeleton('Skeleton')
    skeleton('SkeletonPistol', pistol=True)
    chest('Chest')
    chest('ChestOpen', opened=True)
    gem('Gem')
    ammo('Ammo')
    jade_key('JadeKey')
    book('Book')
    idol('Idol')
    helmet('Helmet')
    stone('Stone')
    life('Life')
    pistol('Pistol')
    boulder('Boulder')
    cart('Cart')
    for f in (0, 1):
        hero(f'HeroDown{f}', 'down', f)
        hero(f'HeroUp{f}', 'up', f)
        hero(f'HeroSide{f}', 'right', f)
        ghoul(f'Ghoul{f}', f)
        guardian(f'Guardian{f}', f)
        fish(f'Fish{f}', f)
    hero('SwimDown', 'down', 0, swim=True)
    hero('SwimUp', 'up', 0, swim=True)
    hero('SwimSide', 'right', 0, swim=True)
    button_icons()
    title_picture('Title')
    print('art written to', os.path.relpath(OUT, ROOT))


if __name__ == '__main__':
    main()
