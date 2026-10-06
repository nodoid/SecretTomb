#!/usr/bin/env python3
"""Builds the support page (with the privacy policy) for the store "Support URL".

    python3 tools/make_support_page.py

Writes docs/SecretTomb-Support.html (self-contained: the icon and a screenshot are embedded)
and copies it to ~/Downloads, ready to upload to the web site. The page is in English (the
game itself and the store listings are also in French).
"""
import base64
import io
import os
import shutil

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'docs', 'SecretTomb-Support.html')
EMAIL = 'paul@all-the-johnsons.co.uk'


def jpeg(path, width, quality=82):
    im = Image.open(path).convert('RGB')
    im = im.resize((width, round(im.height * width / im.width)), Image.LANCZOS)
    buf = io.BytesIO()
    im.save(buf, 'JPEG', quality=quality, optimize=True, progressive=True)
    return 'data:image/jpeg;base64,' + base64.b64encode(buf.getvalue()).decode()


def shot(name, lang):
    p = os.path.join(ROOT, 'stores', lang, 'mac-app-store', 'screenshots-1440x900', name + '.png')
    return p if os.path.exists(p) else None


def main():
    icon = jpeg(os.path.join(ROOT, 'art', 'icon-1024.png'), 160)
    hero_path = shot('01-hall', 'en')
    hero_html = f'<img class="hero" src="{jpeg(hero_path, 1080)}" alt="The Secret of the Tomb: the entrance hall">' if hero_path else ''
    pics = [p for p in (shot('03-pillars', 'en'), shot('04-swim', 'en'), shot('05-cart', 'en')) if p]
    gallery_html = ('<div class="gallery">' + ''.join(f'<img src="{jpeg(p, 520, 78)}" alt="">' for p in pics) + '</div>') if pics else ''

    sections = [
        ('about', 'The game', '''<p>Deep in the Mexican jungle, after days of walking, an archaeologist finally sees it: the pyramid of legend, said to hide a fabulous stone. Inside, stairs lead down to the tomb of Axayacatl, and footsteps echo in the dark. You are not alone.</p>
<p>The Secret of the Tomb is a new, high-resolution remake of <em>Le Secret du Tombeau</em> (also known as <em>Le Tombeau d'Axayacatl</em>), the 1985 Oric adventure by Yves Petitjean and Serge Schruder (Loriciels). As in the original, the tomb scrolls in every direction around you. It runs on iPhone, iPad, Android, Mac and Windows, in English and French: choose the language on the title screen.</p>'''),
        ('controls', 'Controls', '''<table><thead><tr><th></th><th>Mac and Windows</th><th>iPhone, iPad and Android</th></tr></thead><tbody>
<tr><td>Walk</td><td>Cursor keys or <kbd>W</kbd><kbd>A</kbd><kbd>S</kbd><kbd>D</kbd></td><td>The stick: put your left thumb down anywhere on the left</td></tr>
<tr><td>Fire the laser</td><td><kbd>Space</kbd> (the way you face), or <kbd>I</kbd> <kbd>J</kbd> <kbd>K</kbd> <kbd>L</kbd> to fire up, left, down or right</td><td>The red laser button</td></tr>
<tr><td>Jump / ride the mine cart</td><td><kbd>X</kbd></td><td>The blue jump button</td></tr>
<tr><td>Open, close, read, pull, search</td><td><kbd>E</kbd> or <kbd>Enter</kbd></td><td>The green hand button</td></tr>
<tr><td>Pause / menu</td><td><kbd>P</kbd> / <kbd>Esc</kbd></td><td>The pause button / Back</td></tr>
<tr><td>Full screen</td><td><kbd>F11</kbd> or <kbd>Alt</kbd>+<kbd>Enter</kbd></td><td>Always</td></tr>
</tbody></table>
<p>Game controllers work too: the left stick walks, A fires, B jumps, X opens, and the right stick fires in any of the four directions.</p>'''),
        ('tomb', 'The tomb', '''<ul>
<li><strong>Ghouls</strong> rush at you, often in groups, and their touch is deadly: shoot them. Their bones stay where they fall, and other monsters will not cross bones, or stairs.</li>
<li><strong>Iron guardians</strong> cannot be harmed by the laser. The only answer is to run.</li>
<li><strong>Ammunition is scarce</strong>, and the laser destroys chests and ammunition too, so look before you shoot.</li>
<li><strong>Traps:</strong> serpent heads that shoot poisoned darts, pits to jump, a rolling boulder, deep water (watch your air) and floors that give way.</li>
<li><strong>The old script:</strong> the builders left messages in their own writing. The great inscription by the entrance carries its own translation; read it first, and every other inscription becomes readable.</li>
<li><strong>Secret doors</strong> look almost like the walls around them. Search the ones that look different.</li>
<li><strong>Machines</strong> take you where feet cannot: a mine cart over the chasm, a teleporter, and a lever.</li>
<li><strong>Sun stones</strong> on the floor remember your progress: lose a life and you start again from the last one.</li>
<li>Take the <strong>miraculous stone</strong> from Axayacatl's sarcophagus and get out of the pyramid alive to win.</li>
</ul>'''),
        ('difficulty', 'Difficulty', '''<p>Choose <em>Difficulty</em> on the menu. <strong>Easy</strong> gives five lives, slower monsters, more air and more ammunition. <strong>Normal</strong> gives three lives. <strong>Hard</strong> is the original: one life, faster monsters and fewer shots, and the escape bonus is doubled.</p>'''),
        ('faq', 'Questions', '''<h3>A door won't open.</h3><p>Some doors need something you carry: the jade door needs the jade key, and two other doors need things the inscriptions tell you about. Read them.</p>
<h3>I can't read the inscriptions.</h3><p>Read the great inscription near the entrance first. It is written in the old script with a translation underneath.</p>
<h3>I keep drowning.</h3><p>You can only hold your breath for a few seconds. Somewhere in the flooded galleries there is a way to breathe under water.</p>
<h3>How do I change the language?</h3><p>Tap or click <em>English</em> or <em>Français</em> on the title screen, or choose <em>Language</em> on the menu. The first time, the game follows your device's language.</p>
<h3>Where are my scores kept?</h3><p>On your device, in the game's own storage, so they are still there the next time you play. Uninstalling the game deletes them.</p>
<h3>Can I play with a controller?</h3><p>Yes, on Mac, Windows, iPad, iPhone and Android, with any controller the system recognises.</p>'''),
        ('privacy', 'Privacy', '''<p><strong>The Secret of the Tomb collects nothing.</strong> It has no accounts, advertising, analytics or tracking, and it never connects to the internet. It doesn't collect, store, share or sell any personal information, from anyone, including children.</p>
<p>The game keeps two small files in its private storage on your device: the Hall of Fame (the names you enter, with your scores) and your settings (language, volume and difficulty). They never leave your device and are deleted when you uninstall the game.</p>
<p>If this policy changes, the new version will be posted here with a new date. Last updated: 6 October 2026.</p>'''),
        ('contact', 'Contact', f'<p>Found a bug, or have a question this page doesn\'t answer? Email <a href="mailto:{EMAIL}?subject=The%20Secret%20of%20the%20Tomb%20support">{EMAIL}</a>.</p>'),
    ]

    nav = ''.join(f'<a href="#{sid}">{title}</a>' for sid, title, _ in sections)
    body = ''.join(f'<section id="{sid}"><h2>{title}</h2>{content}</section>' for sid, title, content in sections)

    GALLERY = gallery_html
    html = f'''<!doctype html>
<html lang="en-GB">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>The Secret of the Tomb Support</title>
<meta name="description" content="Help and support for The Secret of the Tomb (Le Secret du Tombeau), the high-resolution remake of the 1985 Oric adventure, for iPhone, iPad, Android, Mac and Windows: controls, how to play, questions, privacy and contact.">
<link rel="icon" href="{icon}">
<style>
:root {{ --bg: #F4F2EE; --pane: #FFFFFF; --chip: #E9E5DC; --text: #1E1B18; --muted: #6B655C;
  --accent: #9A6A08; --accent-2: #2C4AA8; --border: #DDD7CB; }}
@media (prefers-color-scheme: dark) {{
  :root:not([data-theme="light"]) {{ --bg: #0E0F1E; --pane: #171A30; --chip: #252946; --text: #EEECE6;
    --muted: #A9A8B8; --accent: #F0C04A; --accent-2: #8FB4FF; --border: #2E3355; }}
}}
:root[data-theme="dark"] {{ --bg: #0E0F1E; --pane: #171A30; --chip: #252946; --text: #EEECE6;
  --muted: #A9A8B8; --accent: #F0C04A; --accent-2: #8FB4FF; --border: #2E3355; }}
* {{ box-sizing: border-box; }}
html {{ scroll-behavior: smooth; }}
body {{ margin: 0; background: var(--bg); color: var(--text);
  font: 16px/1.6 -apple-system, BlinkMacSystemFont, "Segoe UI", system-ui, Roboto, "Helvetica Neue", Arial, sans-serif; }}
a {{ color: var(--accent); }}
.wrap {{ max-width: 1000px; margin: 0 auto; padding: 0 16px; }}
header {{ background: var(--pane); border-bottom: 1px solid var(--border); }}
header .wrap {{ padding-top: 32px; padding-bottom: 26px; }}
.top {{ display: flex; justify-content: space-between; align-items: center; gap: 12px; flex-wrap: wrap; }}
.brand {{ display: flex; align-items: center; gap: 16px; }}
.brand img {{ width: 72px; height: 72px; border-radius: 16px; }}
.eyebrow {{ color: var(--accent-2); font-weight: 600; margin: 0; }}
h1 {{ font-size: clamp(28px, 5vw, 40px); line-height: 1.15; margin: 0; }}
.lead {{ font-size: 18px; color: var(--muted); margin: 16px 0 0; max-width: 48em; }}
.hero {{ width: 100%; border-radius: 14px; margin-top: 22px; display: block; }}
nav.toc {{ display: flex; flex-wrap: wrap; gap: 8px; margin-top: 22px; }}
nav.toc a {{ text-decoration: none; color: var(--text); background: var(--chip); border-radius: 999px; padding: 6px 14px; font-size: 14px; }}
main section {{ background: var(--pane); border: 1px solid var(--border); border-radius: 14px; padding: 8px 24px 16px; margin: 20px 0; }}
h2 {{ font-size: 24px; margin: 16px 0 8px; }}
h3 {{ font-size: 17px; margin: 18px 0 2px; }}
table {{ width: 100%; border-collapse: collapse; font-size: 15px; }}
.table {{ overflow-x: auto; }}
th, td {{ text-align: left; padding: 8px 10px; border-bottom: 1px solid var(--border); vertical-align: top; }}
th {{ color: var(--muted); font-weight: 600; }}
kbd {{ font: 13px ui-monospace, SFMono-Regular, Menlo, monospace; background: var(--chip); border: 1px solid var(--border);
  border-radius: 5px; padding: 1px 5px; }}
.gallery {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 12px; margin: 14px 0 6px; }}
.gallery img {{ width: 100%; border-radius: 10px; display: block; }}
footer {{ color: var(--muted); font-size: 14px; padding: 8px 0 40px; }}
</style>
</head>
<body>
<header><div class="wrap">
  <div class="top">
    <div class="brand"><img src="{icon}" alt="">
      <div><p class="eyebrow">{'Support'}</p><h1>{'The Secret of the Tomb'}</h1></div></div>
  </div>
  <p class="lead">{"The 1984 Oric castle adventure, remade in high resolution for iPhone, iPad, Android, Mac and Windows. In English and French."}</p>
  {hero_html}
  <nav class="toc">{nav}</nav>
</div></header>
<main class="wrap">
{body.replace('<table>', '<div class="table"><table>').replace('</table>', '</table></div>').replace('</section><section id="controls">', GALLERY + '</section><section id="controls">', 1)}
</main>
<footer class="wrap">{"By PFJ, based on the 1985 Loriciels game <em>Le Secret du Tombeau</em> by Yves Petitjean and Serge Schruder. © 2026 Paul F. Johnson."}</footer>
</body>
</html>
'''
    with open(OUT, 'w') as f:
        f.write(html)
    dl = os.path.join(os.path.expanduser('~'), 'Downloads', 'SecretTomb-Support.html')
    shutil.copy(OUT, dl)
    print(f'{os.path.relpath(OUT, ROOT)} ({len(html) // 1024} KB), copied to {dl}')


if __name__ == '__main__':
    main()
