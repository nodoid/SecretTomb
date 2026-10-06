# Store listing: The Secret of the Tomb / Le Secret du Tombeau

Paste-ready sheets for each store and language are generated from this file into `stores/<en|fr>/<store>/UPLOAD.txt` by `tools/make_upload_copy.py`.

Everything to paste into App Store Connect (iOS and Mac), Google Play Console and Microsoft Partner Center, in **English** and **French**. Every field is within its store's limit (checked by `tools/check_listing.py`). The screenshots, graphics and app previews are generated into `stores/en/` and `stores/fr/` by `tools/capture_all.sh` (and `tools/build_windows.sh capture` for the Microsoft Store screenshots).

> **Before you submit:**
> - The game is a new remake *inspired by* the 1985 Oric game *Le Secret du Tombeau* (Loriciels, by Yves Petitjean and Serge Schruder). The tomb, graphics, sound and code are all new; nothing was taken from the original program. The French name and the credit do use the original's title and its authors' names, though. Stores can reject apps that use another party's name or IP without permission, so make sure you have the rights (or permission) to use the title *Le Secret du Tombeau* before you publish. If not, rename the French title in `Strings.cs` and in the platform name files.
> - Age rating: a laser pistol against monsters (ghouls, iron guardians, piranhas), no blood, a dark tomb. Answer "Infrequent/Mild Cartoon or Fantasy Violence" and "Infrequent/Mild Horror/Fear Themes" (Apple 9+); the IARC questionnaire will probably give PEGI 7 / Everyone 10+.

---

## Shared details

| Field | Value |
|---|---|
| App name | The Secret of the Tomb (French: Le Secret du Tombeau) |
| Home screen name | Secret of the Tomb (French: Secret du Tombeau) |
| Bundle ID (iOS, macOS) | `uk.co.allthejohnsons.secrettomb` |
| Package name (Android) | `uk.co.allthejohnsons.secrettomb` |
| Android upload key | `~/keys/secrettomb-upload.jks`, alias `secrettomb`, password in Keychain ("SecretTomb Android upload keystore"); SHA-256 `67:93:C8:E2:EE:28:AD:1B:4C:81:D9:78:47:E1:BF:D6:CB:57:13:A1:6C:A8:69:4C:D2:EA:EF:ED:C7:16:3D:1A` |
| Windows package identity | `49556nodoid.TheSecretoftheTomb`, publisher `CN=A6EAEB04-6634-41C1-BDFE-695819ECE444` |
| Provisioning profiles | iOS `rel-secrettomb` (App Store), `devel-secrettomb`; macOS `rel-secrettomb-mac` (App Store), `devel-secrettomb-mac` |
| Developer / seller | Paul F. Johnson |
| Category | Games, then Adventure (secondary: Action) |
| Price | Free (or set your own) |
| Contains ads | No |
| In-app purchases | No |
| Languages | English, French |
| Support email | paul@all-the-johnsons.co.uk |
| Support URL | Host `docs/SecretTomb-Support.html` (copy in `~/Downloads`) and paste its URL here |
| Privacy policy URL | The support page's `#privacy` section (the policy is also in `docs/PRIVACY.md`) |
| Age rating | Apple 9+ (infrequent/mild fantasy violence, infrequent/mild horror/fear); PEGI 7; ESRB Everyone 10+ |
| Supported devices | iPhone, iPad, Android phones and tablets, Macs with Apple silicon or Intel (macOS 12 or later), Windows 10/11 PCs (x64 and Arm) |
| Orientation | Landscape only |

---

## Apple App Store (iPhone and iPad)

### English

**Name** (30 max)
```
The Secret of the Tomb
```

**Subtitle** (30 max)
```
Escape the Aztec pyramid
```

**Promotional text** (170 max)
```
The 1985 Oric classic, remade in high resolution. Explore the tomb of Axayacatl, read its ancient script and escape with the miraculous stone. In English and French.
```

**Keywords** (100 max)
```
adventure,tomb,pyramid,aztec,retro,oric,explorer,treasure,maze,laser,ghoul,classic,loriciels,1985
```

**Description** (4000 max)
```
Deep in the Mexican jungle, after days of walking, you finally see it: the pyramid of legend, said to hide a fabulous stone. Inside, stairs lead down to the tomb of Axayacatl, and footsteps echo in the dark. You are not alone.

A REMAKE OF A CLASSIC
The Secret of the Tomb is a new, high-resolution remake of Le Secret du Tombeau, the 1985 Oric adventure by Yves Petitjean and Serge Schruder. As in the original, the tomb scrolls in every direction around you, carved from golden stone.

EXPLORE THE TOMB
- Halls of pillars, flooded galleries, a chasm crossed by mine cart, a library and the burial chamber of Axayacatl
- Secret doors hidden among the walls: search the ones that look different
- An ancient script: read the great inscription and every message in the tomb becomes clear
- A jade key, a sacred book, twin idols and a diving helmet open the way
- A teleporter, a lever and a fountain you would do well to leave alone

BEWARE
- Ghouls rush at you, and their touch is deadly: keep your laser ready
- Iron guardians cannot be harmed. Run!
- Poisoned darts, pits, a rolling boulder and floors that give way
- Under water, watch your air
- Ammunition is scarce, and the laser destroys treasure chests too

PLAY YOUR WAY
- On-screen stick and buttons, or a game controller
- Easy, Normal or Hard (one life, as in the original)
- Sun stones remember your progress
- Play in English or French: switch on the title screen
- Hall of Fame, kept on your device

HIGH RESOLUTION
Carved stone, rippling water, torchlight and smooth scrolling at the full resolution of your screen, with the spirit and colours of the original.

No ads, no tracking and no internet connection needed.

By PFJ, based on the 1985 Loriciels game by Yves Petitjean and Serge Schruder.
```

**What's new** (4000 max)
```
First release.
```

### Français

**Nom** (30 max)
```
Le Secret du Tombeau
```

**Sous-titre** (30 max)
```
Fuyez la pyramide aztèque
```

**Texte promotionnel** (170 max)
```
Le classique Oric de 1985 revient en haute résolution. Explorez le tombeau d'Axayacatl, déchiffrez son écriture et fuyez avec la pierre miraculeuse.
```

**Mots-clés** (100 max)
```
aventure,tombeau,pyramide,aztèque,rétro,oric,explorateur,trésor,labyrinthe,laser,loriciels,1985
```

**Description** (4000 max)
```
En pleine forêt mexicaine, après des jours de marche, vous l'apercevez enfin : la pyramide légendaire, qui cacherait une pierre fabuleuse. À l'intérieur, un escalier descend vers le tombeau d'Axayacatl, et des bruits de pas résonnent dans l'obscurité. Vous n'êtes pas seul.

LE RETOUR D'UN CLASSIQUE
Le Secret du Tombeau est un remake en haute résolution du jeu d'aventure Oric de 1985 d'Yves Petitjean et Serge Schruder. Comme dans l'original, le tombeau défile dans toutes les directions autour de vous, taillé dans la pierre dorée.

EXPLOREZ LE TOMBEAU
- Salle des piliers, galeries inondées, gouffre traversé en chariot, bibliothèque et chambre funéraire d'Axayacatl
- Des portes secrètes cachées parmi les murs : fouillez ceux qui semblent différents
- Une écriture ancienne : lisez la grande inscription et tous les messages du tombeau s'éclairent
- Une clé de jade, un livre sacré, des idoles jumelles et un casque de plongée ouvrent le chemin
- Un téléporteur, un levier, et une fontaine qu'il vaut mieux laisser tranquille

ATTENTION
- Les goules foncent sur vous et leur contact est mortel : gardez votre laser prêt
- Les gardiens de fer sont invulnérables. Fuyez !
- Fléchettes empoisonnées, trous, rocher roulant et sols qui s'effondrent
- Sous l'eau, surveillez votre air
- Les munitions sont rares, et le laser détruit aussi les coffres

JOUEZ À VOTRE FAÇON
- Manche et boutons à l'écran, ou manette
- Facile, Normal ou Difficile (une seule vie, comme dans l'original)
- Les pierres du soleil gardent votre progression
- Jouez en français ou en anglais : choisissez sur l'écran titre
- Tableau d'honneur, conservé sur votre appareil

HAUTE RÉSOLUTION
Pierre sculptée, eau ondulante, lueur des torches et défilement fluide à la pleine résolution de votre écran, dans l'esprit et les couleurs de l'original.

Sans publicité, sans pistage et sans connexion Internet.

Par PFJ, d'après le jeu Loriciels de 1985 d'Yves Petitjean et Serge Schruder.
```

**Nouveautés** (4000 max)
```
Première version.
```

**Screenshots:** `stores/<en|fr>/app-store/iphone-6.9in-2868x1320/` (also `iphone-6.5in-2688x1242/`) and `ipad-13in-2752x2064/`, 10 each, landscape.
**App previews:** `stores/<en|fr>/app-store/app-preview-iphone-1920x886.mp4` (iPhone 6.9") and `app-preview-ipad-1600x1200.mp4` (iPad 13"), 28 s, H.264 30 fps with stereo AAC. Copies are in `~/Movies/The Secret of the Tomb/`.
**Icon:** `stores/en/app-store/icon-1024.png`.

---

## Mac App Store

Use the same name, subtitle, promotional text and keywords as iOS, in each language.

### English

**Description** (4000 max)
```
Deep in the Mexican jungle, after days of walking, you finally see it: the pyramid of legend, said to hide a fabulous stone. Inside, stairs lead down to the tomb of Axayacatl, and footsteps echo in the dark. You are not alone.

A REMAKE OF A CLASSIC
The Secret of the Tomb is a new, high-resolution remake of Le Secret du Tombeau, the 1985 Oric adventure by Yves Petitjean and Serge Schruder. As in the original, the tomb scrolls in every direction around you.

EXPLORE THE TOMB
- Halls of pillars, flooded galleries, a chasm crossed by mine cart, a library and the burial chamber of Axayacatl
- Secret doors, an ancient script to decipher, a teleporter and a lever
- Ghouls to shoot, iron guardians to flee, darts, pits and a rolling boulder
- Easy, Normal or Hard (one life, as in the original)

CONTROLS
Keyboard: cursor keys or WASD to walk, SPACE to fire, I J K L to fire up, left, down or right, X to jump or ride the mine cart, E to open, read or search, P to pause, F11 for full screen. Game controllers work too.

HIGH RESOLUTION
Carved stone, rippling water, torchlight and smooth scrolling at the full resolution of your display, in a resizable window or full screen.

In English and French. No ads, no tracking and no internet connection needed.

By PFJ, based on the 1985 Loriciels game by Yves Petitjean and Serge Schruder.
```

### Français

**Description** (4000 max)
```
En pleine forêt mexicaine, après des jours de marche, vous l'apercevez enfin : la pyramide légendaire, qui cacherait une pierre fabuleuse. À l'intérieur, un escalier descend vers le tombeau d'Axayacatl, et des bruits de pas résonnent dans l'obscurité. Vous n'êtes pas seul.

LE RETOUR D'UN CLASSIQUE
Le Secret du Tombeau est un remake en haute résolution du jeu d'aventure Oric de 1985 d'Yves Petitjean et Serge Schruder. Comme dans l'original, le tombeau défile dans toutes les directions autour de vous.

EXPLOREZ LE TOMBEAU
- Salle des piliers, galeries inondées, gouffre traversé en chariot, bibliothèque et chambre funéraire d'Axayacatl
- Portes secrètes, écriture ancienne à déchiffrer, téléporteur et levier
- Des goules à abattre, des gardiens de fer à fuir, des fléchettes, des trous et un rocher roulant
- Facile, Normal ou Difficile (une seule vie, comme dans l'original)

COMMANDES
Clavier : flèches ou WASD pour marcher, ESPACE pour tirer, I J K L pour tirer en haut, à gauche, en bas ou à droite, X pour sauter ou monter dans le chariot, E pour ouvrir, lire ou fouiller, P pour la pause, F11 pour le plein écran. Les manettes fonctionnent aussi.

HAUTE RÉSOLUTION
Pierre sculptée, eau ondulante, lueur des torches et défilement fluide à la pleine résolution de votre écran, en fenêtre redimensionnable ou en plein écran.

En français et en anglais. Sans publicité, sans pistage et sans connexion Internet.

Par PFJ, d'après le jeu Loriciels de 1985 d'Yves Petitjean et Serge Schruder.
```

**Screenshots:** `stores/<en|fr>/mac-app-store/screenshots-2880x1800/` (or `screenshots-1440x900/`), 10.
**App preview:** `stores/<en|fr>/mac-app-store/app-preview-1920x1080.mp4`, 28 s. Copies are in `~/Movies/The Secret of the Tomb/`.

---

## Google Play

### English

**App name** (30 max)
```
The Secret of the Tomb
```

**Short description** (80 max)
```
Explore an Aztec tomb, decipher its script and escape with the miraculous stone.
```

**Full description** (4000 max)
```
Deep in the Mexican jungle, after days of walking, you finally see it: the pyramid of legend, said to hide a fabulous stone. Inside, stairs lead down to the tomb of Axayacatl, and footsteps echo in the dark. You are not alone.

The Secret of the Tomb is a new, high-resolution remake of Le Secret du Tombeau, the 1985 Oric adventure by Yves Petitjean and Serge Schruder. As in the original, the tomb scrolls in every direction around you.

EXPLORE THE TOMB
• Halls of pillars, flooded galleries, a chasm crossed by mine cart, a library and the burial chamber of Axayacatl
• Secret doors hidden among the walls
• An ancient script: read the great inscription and every message becomes clear
• A jade key, a sacred book, twin idols and a diving helmet open the way

BEWARE
• Ghouls whose touch is deadly, and iron guardians no laser can harm
• Poisoned darts, pits, a rolling boulder and floors that give way
• Deep water: watch your air
• Scarce ammunition, and a laser that destroys treasure chests too

PLAY
• On-screen stick and buttons, or a game controller
• Easy, Normal or Hard (one life, as in the original)
• English and French: switch on the title screen
• Hall of Fame, kept on your device

No ads, no tracking and no internet connection needed.

By PFJ, based on the 1985 Loriciels game by Yves Petitjean and Serge Schruder.
```

### Français

**Nom de l'application** (30 max)
```
Le Secret du Tombeau
```

**Description courte** (80 max)
```
Explorez un tombeau aztèque, déchiffrez son écriture et fuyez avec la pierre.
```

**Description complète** (4000 max)
```
En pleine forêt mexicaine, après des jours de marche, vous l'apercevez enfin : la pyramide légendaire, qui cacherait une pierre fabuleuse. À l'intérieur, un escalier descend vers le tombeau d'Axayacatl, et des bruits de pas résonnent dans l'obscurité. Vous n'êtes pas seul.

Le Secret du Tombeau est un remake en haute résolution du jeu d'aventure Oric de 1985 d'Yves Petitjean et Serge Schruder. Comme dans l'original, le tombeau défile dans toutes les directions autour de vous.

EXPLOREZ LE TOMBEAU
• Salle des piliers, galeries inondées, gouffre traversé en chariot, bibliothèque et chambre funéraire d'Axayacatl
• Des portes secrètes cachées parmi les murs
• Une écriture ancienne : lisez la grande inscription et tous les messages s'éclairent
• Une clé de jade, un livre sacré, des idoles jumelles et un casque de plongée ouvrent le chemin

ATTENTION
• Des goules au contact mortel, et des gardiens de fer qu'aucun laser n'atteint
• Fléchettes empoisonnées, trous, rocher roulant et sols qui s'effondrent
• Eaux profondes : surveillez votre air
• Des munitions rares, et un laser qui détruit aussi les coffres

JOUEZ
• Manche et boutons à l'écran, ou manette
• Facile, Normal ou Difficile (une seule vie, comme dans l'original)
• Français et anglais : choisissez sur l'écran titre
• Tableau d'honneur, conservé sur votre appareil

Sans publicité, sans pistage et sans connexion Internet.

Par PFJ, d'après le jeu Loriciels de 1985 d'Yves Petitjean et Serge Schruder.
```

**Screenshots:** `stores/<en|fr>/google-play/phone-screenshots-1920x1080/` and `tablet-screenshots-2560x1600/`, 8 each.
**Graphics:** `stores/<en|fr>/google-play/icon-512.png`, `feature-graphic-1024x500.png`.
**Data safety:** no data collected or shared.

---

## Microsoft Store

Partner Center asks for more than the other stores. Everything below is needed for every submission.

### English

**Product name** (256 max)
```
The Secret of the Tomb
```

**Short description** (1000 max)
```
A high-resolution remake of Le Secret du Tombeau, the 1985 Oric adventure. Explore the scrolling tomb of Axayacatl, decipher its ancient script, outwit its monsters and traps, and escape with the miraculous stone. In English and French.
```

**Description** (10000 max)
```
Deep in the Mexican jungle, after days of walking, you finally see it: the pyramid of legend, said to hide a fabulous stone. Inside, stairs lead down to the tomb of Axayacatl, and footsteps echo in the dark. You are not alone.

The Secret of the Tomb is a new, high-resolution remake of Le Secret du Tombeau, the 1985 Oric adventure by Yves Petitjean and Serge Schruder. As in the original, the tomb scrolls in every direction around you: halls of pillars, flooded galleries, a chasm crossed by mine cart, secret doors, an ancient script to decipher, ghouls to shoot and iron guardians to flee.

Keyboard: cursor keys or WASD to walk, SPACE to fire, I J K L to fire up, left, down or right, X to jump or ride the mine cart, E to open, read or search, P to pause, F11 for full screen. Game controllers work too.

Easy, Normal or Hard (one life, as in the original). In English and French. No ads, no tracking and no internet connection needed.

By PFJ, based on the 1985 Loriciels game by Yves Petitjean and Serge Schruder.
```

**Product features** (20 max, 200 characters each)
```
A scrolling Aztec tomb to explore, as in the 1985 original
An ancient script to decipher, and secret doors to find
Ghouls to shoot and iron guardians no laser can harm
Darts, pits, a rolling boulder and flooded galleries
A mine cart, a teleporter and a lever
Easy, Normal or Hard (one life, as in the original)
High-resolution graphics in a window or full screen
English and French
Keyboard or game controller
```

**Search terms** (7 max, 30 characters each)
```
tomb adventure
aztec pyramid
retro
oric
treasure maze
explorer
secret du tombeau
```

**What's new in this version** (1500 max)
```
First release.
```

**Notes for certification** (2000 max)
```
No account, sign-in, network connection or purchase is needed; all content is available immediately. To play: press SPACE (or click) on the title (ENGLISH / FRANÇAIS there switch the language), then choose PLAY. Cursor keys or WASD walk, SPACE fires the laser, I J K L fire in four directions, X jumps, E opens doors and reads inscriptions, P pauses, Esc returns to the menu, F11 toggles full screen. DIFFICULTY on the menu: Easy gives five lives. Instructions are on the menu. The game is fully offline and stores only high scores and settings locally.
```

### Français

**Nom du produit** (256 max)
```
Le Secret du Tombeau
```

**Description courte** (1000 max)
```
Un remake en haute résolution du Secret du Tombeau, l'aventure Oric de 1985. Explorez le tombeau d'Axayacatl, déchiffrez son écriture ancienne, déjouez ses monstres et ses pièges, et fuyez avec la pierre miraculeuse. En français et en anglais.
```

**Description** (10000 max)
```
En pleine forêt mexicaine, après des jours de marche, vous l'apercevez enfin : la pyramide légendaire, qui cacherait une pierre fabuleuse. À l'intérieur, un escalier descend vers le tombeau d'Axayacatl, et des bruits de pas résonnent dans l'obscurité. Vous n'êtes pas seul.

Le Secret du Tombeau est un remake en haute résolution du jeu d'aventure Oric de 1985 d'Yves Petitjean et Serge Schruder. Comme dans l'original, le tombeau défile dans toutes les directions autour de vous : salle des piliers, galeries inondées, gouffre traversé en chariot, portes secrètes, écriture ancienne à déchiffrer, goules à abattre et gardiens de fer à fuir.

Clavier : flèches ou WASD pour marcher, ESPACE pour tirer, I J K L pour tirer en haut, à gauche, en bas ou à droite, X pour sauter ou monter dans le chariot, E pour ouvrir, lire ou fouiller, P pour la pause, F11 pour le plein écran. Les manettes fonctionnent aussi.

Facile, Normal ou Difficile (une seule vie, comme dans l'original). En français et en anglais. Sans publicité, sans pistage et sans connexion Internet.

Par PFJ, d'après le jeu Loriciels de 1985 d'Yves Petitjean et Serge Schruder.
```

**Fonctionnalités** (20 max, 200 characters each)
```
Un tombeau aztèque à explorer en défilement, comme dans l'original de 1985
Une écriture ancienne à déchiffrer, et des portes secrètes à trouver
Des goules à abattre et des gardiens de fer qu'aucun laser n'atteint
Fléchettes, trous, rocher roulant et galeries inondées
Un chariot, un téléporteur et un levier
Facile, Normal ou Difficile (une seule vie, comme dans l'original)
Graphismes haute résolution, en fenêtre ou en plein écran
Français et anglais
Clavier ou manette
```

**Termes de recherche** (7 max, 30 characters each)
```
aventure tombeau
pyramide aztèque
rétro
oric
trésor labyrinthe
explorateur
loriciels
```

**Nouveautés de cette version** (1500 max)
```
Première version.
```

**Remarques pour la certification** (2000 max)
```
Aucun compte, connexion, réseau ou achat n'est nécessaire ; tout le contenu est disponible immédiatement. Pour jouer : appuyez sur ESPACE (ou cliquez) sur le titre (ENGLISH / FRANÇAIS y changent la langue), puis choisissez JOUER. Les flèches ou WASD font marcher, ESPACE tire au laser, I J K L tirent dans quatre directions, X fait sauter, E ouvre les portes et lit les inscriptions, P met en pause, Échap revient au menu, F11 bascule le plein écran. DIFFICULTÉ dans le menu : Facile donne cinq vies. Les instructions sont dans le menu. Le jeu fonctionne entièrement hors ligne et ne garde que les scores et les réglages sur l'appareil.
```

**Screenshots:** captured by the Windows (DirectX) build running in Parallels: `stores/<en|fr>/microsoft-store/screenshots-3840x2160/` (4K), `screenshots-1920x1080/` and `screenshots-1366x768/`, 10 each.

**Store art** (in `stores/<en|fr>/microsoft-store/`)

| Partner Center slot | File | Notes |
|---|---|---|
| 16:9 Super hero art | `super-hero-art-3840x2160.png` (also `-1920x1080`) | No title text, as the Store overlays its own |
| 1:1 Box art | `box-art-2160x2160.png` | |
| 2:3 Poster art | `poster-art-1440x2160.png` | |
| App tile icon | `store-logo-300x300.png` | |
| Xbox branded key art | `xbox/branded-key-art-584x800.png` | Includes the title |
| Xbox titled hero art | `xbox/titled-hero-art-3840x2160.png` (also `-1920x1080`) | Includes the title |
| Xbox featured promotional square art | `xbox/featured-promotional-square-art-2160x2160.png` (also `-1080x1080`) | No title |

All of them are built from the game's own artwork by `tools/make_store_assets.py`.

**Packages:** `releases/windows/SecretTomb-1.0.0-x64.msix` and `SecretTomb-1.0.0-arm64.msix` (built by `tools/build_windows.sh`; the Store signs them).

**Restricted capabilities** (Partner Center › Submission options › *Why does your app need these capabilities?*)

The package declares one restricted capability, `runFullTrust` (in `SecretTomb.WindowsDX/Windows/AppxManifest.xml`). No other restricted or general capabilities are declared: not `internetClient`, file system access, webcam, microphone or location. Paste:
```
runFullTrust: The Secret of the Tomb is a packaged desktop (Win32) game, built with .NET 10 and MonoGame and rendered with DirectX 11. Every packaged Win32 desktop app needs runFullTrust to start its executable (Windows.FullTrustApplication entry point). The game uses it only to run its own process. It does not use the internet, other apps or processes, the user's documents or system settings. It reads the keyboard, mouse and game controllers, draws with DirectX, plays sound with XAudio, and saves its high scores and settings in its own app data folder.
```

**Other Partner Center answers**

| Field | Answer |
|---|---|
| Category | Games › Adventure |
| Pricing | Free |
| Age ratings | IARC questionnaire: same answers as Google Play (mild fantasy violence and horror; no blood, gambling or user interaction) |
| Input | Keyboard and game controller (Xbox controller supported) |
| Display modes | Windowed and full screen; landscape |
| Privacy policy URL | The support page's `#privacy` section |
| Support contact | paul@all-the-johnsons.co.uk |
| Languages | English (United Kingdom) en-gb and French (France) fr-fr, as in the package manifest |
