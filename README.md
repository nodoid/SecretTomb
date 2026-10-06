# The Secret of the Tomb (Le Secret du Tombeau)

A high-resolution MonoGame remake of **Le Secret du Tombeau** (also known as *Le Tombeau
d'Axayacatl*), the 1985 Oric adventure by Yves Petitjean and Serge Schruder (Loriciels), for
Android, iOS, macOS and Windows, in **English and French**.

Deep in the Mexican jungle an archaeologist finds the pyramid of legend. Below it lies the tomb of
Axayacatl and, somewhere inside, a miraculous stone. Armed with a laser pistol taken from a
skeleton, you explore the scrolling tomb: shoot the ghouls, flee the iron guardians, dodge darts,
pits and a rolling boulder, swim the flooded galleries, ride the mine cart over the chasm, find the
secret doors, decipher the builders' script, and escape with the stone.

By PFJ, based on the 1985 Loriciels game. Released under the [DILLIGAF License](LICENSE).

## How it looks

- **Layout:** the Oric's 240 x 224 screen, with the original's title bar, a scrolling view of the
  tomb and a status bar.
- **Rendering:** at the full resolution of the display (up to 8x on screen; 10x for store
  captures), with smooth scrolling, cast shadows, torchlight and animated water.
- **Artwork:** every tile, figure and object is drawn by `tools/make_art.py` into
  `SecretTomb.Core/Art`. Carved stone is modelled as a height map and lit, so the walls are real
  relief carvings (the original's rain-god faces); figures are vector drawings.
- **Text:** the Oric ROM font, smoothed with Scale2x, with added French accents. The tomb's own
  script is generated in `Graphics/Glyphs.cs`.
- **Sound:** square waves and noise in the style of the Oric's AY-3-8912 (`Audio/Synth.cs`).

## The tomb

The whole tomb is the character map `SecretTomb.Core/World/Tomb.map` (60 x 48 squares; the legend
is in `World/Tomb.cs`). `Engine/Adventure.cs` holds the rules, `Engine/TombRenderer.cs` draws it,
and `Engine/Autopilot.cs` plays it from start to finish (the demo, the store captures and a test).

## Languages and options

All text is in `SecretTomb.Core/Localization/Strings.cs`. The first run follows the device
language; the **ENGLISH / FRANÇAIS** buttons on the title screen (or **L**, or the menu) switch at
any time, and the choice is saved. The app name is localised too: "Secret of the Tomb" /
"Secret du Tombeau" (iOS and macOS `InfoPlist.strings`, Android `values-fr`).

**Difficulty** (menu): Easy (five lives, slower monsters, more air and ammunition), Normal (three
lives) or Hard (one life, as in the original). The **Hall of Fame** is saved on the device after
every qualifying game (straight away, then again with the name), so it carries over to later
sessions.

## Projects

| Project | Target |
|---|---|
| `SecretTomb.Core` | The whole game (net10.0): tomb, rules, renderer, scenes, input, audio, capture mode |
| `SecretTomb.Android` | Android 6+ |
| `SecretTomb.iOS` | iOS 15+ (UIScene life cycle) |
| `SecretTomb.DesktopGL` | macOS (also runs on Linux and Windows) |
| `SecretTomb.WindowsDX` | Windows (DirectX), packaged as MSIX for the Microsoft Store |
| `SecretTomb.Tests` | xUnit tests, including an autopilot run proving the tomb can be completed |

Identifiers:
- Bundle ID (iOS and macOS) and Android package: `uk.co.allthejohnsons.secrettomb`
- Windows identity: `49556nodoid.TheSecretoftheTomb`
- Profiles (read from `~/Downloads`, never committed): iOS `rel-secrettomb` / `devel-secrettomb`,
  macOS `relsecrettombmac.provisionprofile` / `develsecrettombmac.provisionprofile`
- Android upload key: `~/keys/secrettomb-upload.jks` (password in the Keychain)

## Controls

| | Desktop | Phone / tablet |
|---|---|---|
| Walk | Cursor keys or WASD | Stick (left thumb, anywhere on the left) |
| Fire | `SPACE`; `I` `J` `K` `L` fire up / left / down / right | Laser button |
| Jump / ride the cart | `X` | Jump button |
| Open, close, read, pull, search | `E` or `ENTER` | Hand button |
| Pause / menu | `P` / `Esc` | Pause button / Back |
| Language / volume | `L` / `V` (title and menu) | Title screen / menu |

Game controllers: left stick walks, A fires, B jumps, X opens, right stick fires in four directions.

## Build and run

```sh
dotnet test SecretTomb.Tests                     # unit tests
dotnet run --project SecretTomb.DesktopGL        # play on the Mac
python3 tools/make_art.py                        # redraw the artwork
python3 art/make_icons.py                        # regenerate every platform icon
```

Release builds go to `releases/` (git-ignored):

```sh
tools/build_desktop.sh       # macOS universal .app + App Store .pkg   (tools/build_desktop.sh dev: runs locally)
tools/build_release.sh       # Android .aab/.apk and the iOS .ipa
tools/build_windows.sh       # Windows x64 + arm64 .msix/.zip, built and tested in the Parallels "Windows 11" VM
```

In Debug builds, `SECRETTOMB_START=play` opens straight into a game (for simulators).

## Store assets

The listing copy for every store, in English and French, is in [`docs/LISTING.md`](docs/LISTING.md)
(`python3 tools/check_listing.py` checks the limits); the privacy policy is
[`docs/PRIVACY.md`](docs/PRIVACY.md); the support page (English, with the privacy policy) is
`docs/SecretTomb-Support.html` (`python3 tools/make_support_page.py` rebuilds it and copies it to
`~/Downloads`). `python3 tools/make_upload_copy.py` writes a paste-ready `UPLOAD.txt` for each store
and language into `stores/`. Screenshots, graphics and app previews are generated into
`stores/en` and `stores/fr` (git-ignored):

```sh
tools/build_windows.sh capture   # Microsoft Store stills from the Windows build (both languages)
tools/capture_all.sh             # every other capture, the store images, and the app preview videos
```

Capture mode (`SecretTomb --capture <dir> stills|videos [--mobile] [--lang fr] [--scale n]`)
plays the tomb on autopilot and saves frames at high resolution, recording the sound effects so
the videos get a matching soundtrack. The videos are also copied to `~/Movies/The Secret of the Tomb`.

## Licence

The Secret of the Tomb code, artwork, tools and documentation are released under the
**[DILLIGAF License](LICENSE)**. The licence can't cover what isn't ours: the original
*Le Secret du Tombeau* game and its name, the Oric ROM font, and third-party packages such as
MonoGame, which keep their own licences.
