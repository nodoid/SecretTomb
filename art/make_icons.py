"""Generates every platform icon (run from the repo root): the carved face of the tomb's walls,
in gold, with the miraculous stone glowing on its brow, on a deep blue ground."""
import os
import shutil
import subprocess
import sys
import tempfile

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, 'tools')
import make_art  # noqa: E402

PROJECT = 'SecretTomb'


def artwork():
    """The face and the stone, drawn as vector art at high resolution."""
    tmp = tempfile.mkdtemp()
    make_art.OUT = tmp
    make_art.PX = 42
    make_art.wall_face('Face', seed=1)
    make_art.stone('Stone')
    face = Image.open(os.path.join(tmp, 'Face.png')).convert('RGBA')
    stone = Image.open(os.path.join(tmp, 'Stone.png')).convert('RGBA')
    # Fade the stone's own glow out before the edge of its square.
    mask = Image.new('L', stone.size, 0)
    ImageDraw.Draw(mask).ellipse([stone.width * 0.12, stone.height * 0.12, stone.width * 0.88, stone.height * 0.88], fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(stone.width * 0.06))
    alpha = Image.fromarray((np.asarray(stone.getchannel('A'), np.float32) * np.asarray(mask, np.float32) / 255).astype(np.uint8))
    stone.putalpha(alpha)
    shutil.rmtree(tmp)
    return face, stone


FACE, STONE = artwork()


def icon(size, pad=0.1, wide=None):
    w, h = wide or (size, size)
    s = 2
    W, H = w * s, h * s
    bg = Image.new('RGBA', (W, H))
    d = ImageDraw.Draw(bg)
    for y in range(H):
        t = y / max(1, H - 1)
        d.line([(0, y), (W, y)], fill=(int(24 + 10 * t), int(26 + 6 * t), int(84 - 40 * t), 255))
    glow = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    r = int(min(W, H) * 0.46)
    gd.ellipse([W // 2 - r, H // 2 - r, W // 2 + r, H // 2 + r], fill=(255, 190, 80, 80))
    bg = Image.alpha_composite(bg, glow.filter(ImageFilter.GaussianBlur(min(W, H) * 0.08)))
    side = int(min(W, H) * (1 - 2 * pad))
    face = FACE.resize((side, side), Image.LANCZOS)
    fx, fy = (W - side) // 2, (H - side) // 2 + int(side * 0.04)
    shadow = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    shadow.paste((0, 0, 0, 170), (fx + side // 40, fy + side // 30), face)
    bg = Image.alpha_composite(bg, shadow.filter(ImageFilter.GaussianBlur(side * 0.02)))
    bg.alpha_composite(face, (fx, fy))
    st = int(side * 0.36)
    stone = STONE.resize((st, st), Image.LANCZOS)
    halo = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    hd = ImageDraw.Draw(halo)
    cx, cy = W // 2, fy + int(side * 0.12)
    hd.ellipse([cx - st, cy - st, cx + st, cy + st], fill=(150, 255, 255, 150))
    bg = Image.alpha_composite(bg, halo.filter(ImageFilter.GaussianBlur(st * 0.35)))
    bg.alpha_composite(stone, (cx - st // 2, cy - st // 2))
    return bg.resize((w, h), Image.LANCZOS)


def save(im, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.save(path)


for folder, px in [('mdpi', 48), ('hdpi', 72), ('xhdpi', 96), ('xxhdpi', 144), ('xxxhdpi', 192)]:
    save(icon(px, 0.08), f'{PROJECT}.Android/Resources/drawable-{folder}/icon.png')
    save(icon(px * 2, 0.3), f'{PROJECT}.Android/Resources/drawable-{folder}/splash.png')

ios = f'{PROJECT}.iOS/AppIcon.xcassets/AppIcon.appiconset'
os.makedirs(ios, exist_ok=True)
for n in [20, 29, 40, 58, 60, 76, 80, 87, 120, 152, 167, 180, 1024]:
    icon(n, 0.08).convert('RGB').save(os.path.join(ios, f'icon_{n}x{n}.png'))

os.makedirs(f'{PROJECT}.WindowsDX/Windows/Assets', exist_ok=True)
icon(256, 0.04).save(f'{PROJECT}.WindowsDX/Icon.ico', sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
shutil.copy(f'{PROJECT}.WindowsDX/Icon.ico', f'{PROJECT}.DesktopGL/Icon.ico')
for name, size in [('Square44x44Logo', (44, 44)), ('Square150x150Logo', (150, 150)), ('StoreLogo', (50, 50)),
                   ('Wide310x150Logo', (310, 150)), ('SplashScreen', (620, 300)), ('Square71x71Logo', (71, 71)),
                   ('Square310x310Logo', (310, 310))]:
    icon(min(size), 0.08, wide=size).save(f'{PROJECT}.WindowsDX/Windows/Assets/{name}.png')
icon(44, 0.0).save(f'{PROJECT}.WindowsDX/Windows/Assets/Square44x44Logo.targetsize-44_altform-unplated.png')

iconset = f'art/{PROJECT}.iconset'
os.makedirs(iconset, exist_ok=True)
for s in [16, 32, 128, 256, 512]:
    icon(s, 0.08).save(f'{iconset}/icon_{s}x{s}.png')
    icon(s * 2, 0.08).save(f'{iconset}/icon_{s}x{s}@2x.png')
subprocess.run(['iconutil', '-c', 'icns', iconset, '-o', f'{PROJECT}.DesktopGL/{PROJECT}.icns'], check=True)
shutil.rmtree(iconset)
icon(1024, 0.08).convert('RGB').save('art/icon-1024.png')
print('icons done')
