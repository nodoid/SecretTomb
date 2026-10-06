#!/usr/bin/env python3
"""Builds the store screenshots, graphics and app preview videos in store/ from game captures.

    python3 tools/make_store_assets.py stills <capture-root>
    python3 tools/make_store_assets.py video <capture-root> <en|fr> <mobile|desktop>
    python3 tools/make_store_assets.py xbox             (only the Xbox images)

<capture-root> holds the output of the game's capture mode (see tools/capture_all.sh):

    <lang>/desktop/stills/*.png     macOS captures with desktop prompts   (2400 x 2240)
    <lang>/mobile/stills/*.png      captures with touch prompts
    <lang>/windows/stills/*.png     captures made by the Windows build (tools/build_windows.sh capture)
    <lang>/video-<layout>/video/NN  frame folders + NN.wav from `videos`

The game screen keeps its proportions; the rest of each image is the same picture enlarged,
blurred and darkened, so nothing is stretched and there are no black bars. Videos are also
copied to ~/Movies/The Secret of the Tomb.
"""
import glob
import os
import shutil
import subprocess
import sys
import wave

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
STORE = os.path.join(ROOT, 'stores')
MOVIES = os.path.join(os.path.expanduser('~'), 'Movies', 'The Secret of the Tomb')
LANGS = {'en': 'English', 'fr': 'French'}

# The screens used for every store, in listing order (Google Play takes the first eight).
SHOTS = ['01-hall', '02-title', '03-pillars', '04-swim', '07-cart', '05-rosetta', '06-boulder',
         '08-burial', '12-stone', '13-victory']

# Video segments in montage order; every captured frame is used (50 per second, ~29 s).
SEGMENTS = ['01', '02', '03', '04', '05', '06', '07', '08', '09', '10']


def fit(img, w, h):
    """The screen fitted into w x h over a blurred, darkened copy of itself."""
    img = img.convert('RGB')
    cover = max(w / img.width, h / img.height)
    bg = img.resize((int(img.width * cover) + 1, int(img.height * cover) + 1), Image.BILINEAR)
    bg = bg.crop(((bg.width - w) // 2, (bg.height - h) // 2, (bg.width - w) // 2 + w, (bg.height - h) // 2 + h))
    bg = bg.resize((max(1, w // 8), max(1, h // 8)), Image.BILINEAR).filter(ImageFilter.GaussianBlur(6)).resize((w, h), Image.BICUBIC)
    bg = ImageEnhance.Brightness(bg).enhance(0.45)
    s = min(w / img.width, h / img.height)
    fg = img.resize((round(img.width * s), round(img.height * s)), Image.LANCZOS)
    shadow = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rectangle([(w - fg.width) // 2 - 6, (h - fg.height) // 2 - 6,
                                      (w + fg.width) // 2 + 6, (h + fg.height) // 2 + 6], fill=(0, 0, 0, 170))
    out = Image.alpha_composite(bg.convert('RGBA'), shadow.filter(ImageFilter.GaussianBlur(max(w, h) // 120)))
    out.paste(fg, ((w - fg.width) // 2, (h - fg.height) // 2))
    return out.convert('RGB')


def screenshots(src, dest, w, h, count=len(SHOTS)):
    if not os.path.isdir(src):
        print(f'skipped {dest}: no captures in {src}')
        return
    os.makedirs(dest, exist_ok=True)
    for i, name in enumerate(SHOTS[:count]):
        fit(Image.open(os.path.join(src, name + '.png')), w, h).save(os.path.join(dest, f'{i + 1:02d}-{name[3:]}.png'))
    print(f'{os.path.relpath(dest, ROOT)}: {count} x {w}x{h}')


_PYRAMID = None


def pyramid():
    """The title illustration (the pyramid at dusk, no text), drawn large."""
    global _PYRAMID
    if _PYRAMID is None:
        import tempfile
        sys.path.insert(0, os.path.join(ROOT, 'tools'))
        import make_art
        tmp = tempfile.mkdtemp()
        make_art.OUT = tmp
        make_art.title_picture('Big', px=16)
        _PYRAMID = Image.open(os.path.join(tmp, 'Big.png')).convert('RGB')
        shutil.rmtree(tmp)
    return _PYRAMID


def night(w, h, focus=0.5):
    """Text-free art: the pyramid in the jungle, cropped to fill w x h (focus: where the
    pyramid sits across the width, 0 left .. 1 right)."""
    src = pyramid()
    k = max(w / src.width, h / src.height)
    big = src.resize((round(src.width * k), round(src.height * k)), Image.LANCZOS)
    x = int((big.width - w) * (1 - focus))
    y = int((big.height - h) * 0.25)
    return big.crop((x, y, x + w, y + h))


def scale2x(rows):
    """Scale2x (EPX) on rows of characters, as the game smooths its font."""
    h, w = len(rows), len(rows[0])
    at = lambda x, y: rows[min(max(y, 0), h - 1)][min(max(x, 0), w - 1)]
    out = [[''] * (w * 2) for _ in range(h * 2)]
    for y in range(h):
        for x in range(w):
            b, d, e, f, hh = at(x, y - 1), at(x - 1, y), at(x, y), at(x + 1, y), at(x, y + 1)
            e0 = e1 = e2 = e3 = e
            if b != hh and d != f:
                e0 = d if d == b else e
                e1 = f if b == f else e
                e2 = d if d == hh else e
                e3 = f if hh == f else e
            out[y * 2][x * 2], out[y * 2][x * 2 + 1] = e0, e1
            out[y * 2 + 1][x * 2], out[y * 2 + 1][x * 2 + 1] = e2, e3
    return [''.join(r) for r in out]


def oric_text(text, height, colour=(255, 214, 90)):
    """Text in the game's font (the Oric ROM font with its French accents), smoothed with Scale2x
    as in the game, in gold with a dark drop shadow."""
    import re
    src = open(os.path.join(ROOT, 'SecretTomb.Core', 'Oric', 'OricFont.cs')).read()
    rom = [int(x, 16) for x in re.findall(r'0x([0-9A-F]{2}),', src.split('Data =')[1].split('];')[0])]
    extras = {}
    for ch, rows in re.findall(r"\('\\u([0-9A-F]{4})', \[([^\]]*)\]\)", src):
        extras[chr(int(ch, 16))] = [int(x, 16) for x in re.findall(r'0x([0-9A-F]{2})', rows)]

    def glyph(c):
        if c in extras:
            return extras[c]
        i = ord(c) - 32
        return rom[i * 8:i * 8 + 8] if 0 <= i < 96 else [0] * 8

    rows = ['' for _ in range(8)]
    for c in text:
        g = glyph(c)
        for y in range(8):
            rows[y] += ''.join('Y' if g[y] & (1 << (5 - x)) else '.' for x in range(6))
    for _ in range(3):
        rows = scale2x(rows)
    mask = Image.new('L', (len(rows[0]), len(rows)))
    mask.putdata([255 if ch == 'Y' else 0 for r in rows for ch in r])
    k = height / mask.height
    mask = mask.resize((round(mask.width * k), round(height)), Image.LANCZOS)
    pad = max(2, int(height * 0.08))
    out = Image.new('RGBA', (mask.width + pad * 2, mask.height + pad * 2), (0, 0, 0, 0))
    shadow = Image.new('RGBA', out.size, (0, 0, 0, 0))
    shadow.paste((0, 0, 0, 200), (pad + pad // 2, pad + pad // 2), mask)
    out = Image.alpha_composite(out, shadow.filter(ImageFilter.GaussianBlur(pad / 2)))
    out.paste(colour + (255,), (pad, pad), mask)
    return out


def shade_bottom(img, start=0.55, strength=0.85):
    """Darkens the lower part of a picture so a title reads over it."""
    w, h = img.size
    grad = Image.new('L', (1, h))
    grad.putdata([int(255 * strength * max(0.0, (y / h - start) / (1 - start))) for y in range(h)])
    dark = Image.new('RGB', (w, h), (6, 8, 16))
    return Image.composite(dark, img, grad.resize((w, h)))


def xbox(ms, lang):
    """The Xbox images Partner Center asks for: key art and hero art with the title, square art without."""
    x = os.path.join(ms, 'xbox')
    os.makedirs(x, exist_ok=True)
    lines = ['THE SECRET', 'OF THE TOMB'] if lang == 'en' else ['LE SECRET', 'DU TOMBEAU']
    sub = 'THE TOMB OF AXAYACATL' if lang == 'en' else "LE TOMBEAU D'AXAYACATL"
    # Branded key art, 584 x 800: the pyramid above the title.
    img = shade_bottom(night(584, 800), 0.5)
    y = 540
    for line in lines:
        t = oric_text(line, 54)
        img.paste(t, ((584 - t.width) // 2, y), t)
        y += t.height
    t2 = oric_text(sub, 22, (150, 220, 255))
    img.paste(t2, ((584 - t2.width) // 2, y + 8), t2)
    img.save(os.path.join(x, 'branded-key-art-584x800.png'))
    # Titled hero art, 16:9: the title over the lower left.
    for w, h in [(1920, 1080), (3840, 2160)]:
        img = shade_bottom(night(w, h, 0.5), 0.45)
        y = int(h * 0.6)
        for line in lines:
            t = oric_text(line, h * 0.1)
            img.paste(t, (int(w * 0.05), y), t)
            y += t.height
        t2 = oric_text(sub, h * 0.045, (150, 220, 255))
        img.paste(t2, (int(w * 0.055), y), t2)
        img.save(os.path.join(x, f'titled-hero-art-{w}x{h}.png'))
    # Featured promotional square art: no title.
    for n in (1080, 2160):
        night(n, n).save(os.path.join(x, f'featured-promotional-square-art-{n}x{n}.png'))


def stills(cap):
    icon = Image.open(os.path.join(ROOT, 'art', 'icon-1024.png')).convert('RGB')
    for lang in LANGS:
        desktop = os.path.join(cap, lang, 'desktop', 'stills')
        mobile = os.path.join(cap, lang, 'mobile', 'stills')
        windows = os.path.join(cap, lang, 'windows', 'stills')
        out = os.path.join(STORE, lang)

        # Apple App Store (iOS): landscape iPhone 6.9" and 6.5", iPad 13".
        a = os.path.join(out, 'app-store')
        screenshots(mobile, os.path.join(a, 'iphone-6.9in-2868x1320'), 2868, 1320)
        screenshots(mobile, os.path.join(a, 'iphone-6.5in-2688x1242'), 2688, 1242)
        screenshots(mobile, os.path.join(a, 'ipad-13in-2752x2064'), 2752, 2064)
        icon.save(os.path.join(a, 'icon-1024.png'))

        # Mac App Store.
        m = os.path.join(out, 'mac-app-store')
        screenshots(desktop, os.path.join(m, 'screenshots-2880x1800'), 2880, 1800)
        screenshots(desktop, os.path.join(m, 'screenshots-1440x900'), 1440, 900)

        # Google Play: phone and 10" tablet, icon and feature graphic.
        g = os.path.join(out, 'google-play')
        screenshots(mobile, os.path.join(g, 'phone-screenshots-1920x1080'), 1920, 1080, 8)
        screenshots(mobile, os.path.join(g, 'tablet-screenshots-2560x1600'), 2560, 1600, 8)
        icon.resize((512, 512), Image.LANCZOS).save(os.path.join(g, 'icon-512.png'))
        title = Image.open(os.path.join(desktop, '02-title.png')).convert('RGB')
        crop = title.crop((0, 0, title.width, title.height * 118 // 224))
        fit(crop, 1024, 500).save(os.path.join(g, 'feature-graphic-1024x500.png'))

        # Microsoft Store: captured by the Windows (DirectX) build when available.
        ms = os.path.join(out, 'microsoft-store')
        src = windows if os.path.isdir(windows) else desktop
        screenshots(src, os.path.join(ms, 'screenshots-3840x2160'), 3840, 2160)
        screenshots(src, os.path.join(ms, 'screenshots-1920x1080'), 1920, 1080)
        screenshots(src, os.path.join(ms, 'screenshots-1366x768'), 1366, 768)
        fit(Image.open(os.path.join(src, '02-title.png')), 2160, 2160).save(os.path.join(ms, 'box-art-2160x2160.png'))
        fit(Image.open(os.path.join(src, '02-title.png')), 1440, 2160).save(os.path.join(ms, 'poster-art-1440x2160.png'))
        icon.resize((300, 300), Image.LANCZOS).save(os.path.join(ms, 'store-logo-300x300.png'))
        # Super hero art must carry no text: the pyramid, off to the right of the Store's own title.
        for w, h in [(1920, 1080), (3840, 2160)]:
            night(w, h, 0.35).save(os.path.join(ms, f'super-hero-art-{w}x{h}.png'))
        xbox(ms, lang)
    print('stills done')


def montage(video_root, tmp):
    """Joins every frame of each segment, and their audio, into one sequence."""
    frames = os.path.join(tmp, 'frames')
    shutil.rmtree(tmp, ignore_errors=True)
    os.makedirs(frames)
    n = 0
    audio = bytearray()
    rate = 22050
    for seg in SEGMENTS:
        files = sorted(glob.glob(os.path.join(video_root, seg, 'f*.png')))
        for f in files:
            os.link(f, os.path.join(frames, f'{n:05d}.png'))
            n += 1
        with wave.open(os.path.join(video_root, seg + '.wav')) as wv:
            rate = wv.getframerate()
            pcm = wv.readframes(wv.getnframes())
        want = len(files) * rate // 50 * 2
        audio += pcm[:want] + bytes(max(0, want - len(pcm)))
    wav = os.path.join(tmp, 'audio.wav')
    with wave.open(wav, 'wb') as wv:
        wv.setnchannels(1)
        wv.setsampwidth(2)
        wv.setframerate(rate)
        wv.writeframes(bytes(audio))
    return frames, wav, n


def encode(frames, wav, out, w, h):
    """H.264 / AAC at 30 fps, as App Store Connect wants; blurred fill around the screen."""
    os.makedirs(os.path.dirname(out), exist_ok=True)
    vf = (f'[0:v]split[a][b];'
          f'[a]scale={w // 8}:{h // 8}:force_original_aspect_ratio=increase,crop={w // 8}:{h // 8},boxblur=6:2,'
          f'scale={w}:{h},eq=brightness=-0.22:saturation=0.8[bg];'
          f'[b]scale=-2:{h}:flags=lanczos[fg];'
          f'[bg][fg]overlay=(W-w)/2:(H-h)/2,fps=30,format=yuv420p[v]')
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', '50', '-i', os.path.join(frames, '%05d.png'),
                    '-i', wav, '-filter_complex', vf, '-map', '[v]', '-map', '1:a',
                    '-c:v', 'libx264', '-profile:v', 'high', '-level', '4.0', '-crf', '17', '-preset', 'slow',
                    '-c:a', 'aac', '-b:a', '256k', '-ar', '44100', '-ac', '2',
                    '-movflags', '+faststart', '-shortest', out], check=True)
    print('video', os.path.relpath(out, ROOT))


def video(cap, lang, layout):
    root = os.path.join(cap, lang, f'video-{layout}', 'video')
    tmp = os.path.join(cap, '_montage')
    frames, wav, n = montage(root, tmp)
    print(f'{lang} {layout}: {n} frames, {n / 50:.1f} s')
    os.makedirs(MOVIES, exist_ok=True)
    name = LANGS[lang]
    if layout == 'mobile':
        targets = [(os.path.join(STORE, lang, 'app-store', 'app-preview-iphone-1920x886.mp4'), 1920, 886,
                    f'The Secret of the Tomb - iPhone app preview 1920x886 - {name}.mp4'),
                   (os.path.join(STORE, lang, 'app-store', 'app-preview-ipad-1600x1200.mp4'), 1600, 1200,
                    f'The Secret of the Tomb - iPad app preview 1600x1200 - {name}.mp4')]
    else:
        targets = [(os.path.join(STORE, lang, 'mac-app-store', 'app-preview-1920x1080.mp4'), 1920, 1080,
                    f'The Secret of the Tomb - Mac app preview 1920x1080 - {name}.mp4')]
    for out, w, h, movie in targets:
        encode(frames, wav, out, w, h)
        shutil.copy(out, os.path.join(MOVIES, movie))
    shutil.rmtree(tmp, ignore_errors=True)


if __name__ == '__main__':
    if len(sys.argv) >= 3 and sys.argv[1] == 'stills':
        stills(sys.argv[2])
    elif len(sys.argv) == 2 and sys.argv[1] == 'xbox':
        for lang in LANGS:
            xbox(os.path.join(STORE, lang, 'microsoft-store'), lang)
            print(f'store/{lang}/microsoft-store/xbox')
    elif len(sys.argv) == 5 and sys.argv[1] == 'video':
        video(sys.argv[2], sys.argv[3], sys.argv[4])
    else:
        sys.exit(__doc__)
