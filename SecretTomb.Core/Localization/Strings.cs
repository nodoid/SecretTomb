using System;
using SecretTomb.Core.Engine;

namespace SecretTomb.Core.Localization;

public enum Language
{
    English,
    French,
}

/// <summary>
/// Every piece of text the game shows, in English and in the original French.
/// Accented capitals use the extra glyphs in <see cref="Oric.OricFont"/>.
/// </summary>
public static class Strings
{
    public static Language Current { get; set; } = Language.English;

    /// <summary>Picks the language for a first run from the device's two-letter language code.</summary>
    public static Language FromIso(string iso) =>
        string.Equals(iso, "fr", StringComparison.OrdinalIgnoreCase) ? Language.French : Language.English;

    public static string IsoCode(Language language) => language == Language.French ? "fr" : "en";

    private static string L(string fr, string en) => Current == Language.French ? fr : en;

    // ---------------- title and credits ----------------
    public static string TitleLine1 => L("LE SECRET", "THE SECRET");
    public static string TitleLine2 => L("DU TOMBEAU", "OF THE TOMB");
    public static string Title => L("LE SECRET DU TOMBEAU", "THE SECRET OF THE TOMB");
    public static string Subtitle => L("LE TOMBEAU D'AXAYACATL", "THE TOMB OF AXAYACATL");
    public static string Credit => L("PAR PFJ", "BY PFJ");
    public static string BasedOn => L("D'APRÈS LE JEU LORICIELS DE 1985", "BASED ON THE 1985 LORICIELS GAME");
    public static string Authors => L("DE Y. PETITJEAN ET S. SCHRUDER", "BY Y. PETITJEAN AND S. SCHRUDER");
    public static string PressToStart(bool mobile) =>
        mobile ? L("TOUCHEZ L'ÉCRAN POUR JOUER", "TAP THE SCREEN TO PLAY") : L("APPUYEZ SUR ESPACE", "PRESS SPACE");
    public static string English => "ENGLISH";
    public static string French => "FRANÇAIS";
    public static string LanguageHint(bool mobile) =>
        mobile ? L("CHOISISSEZ LA LANGUE", "CHOOSE YOUR LANGUAGE") : L("L : CHANGER DE LANGUE", "L: CHANGE LANGUAGE");

    // ---------------- menu ----------------
    public static string Play => L("JOUER", "PLAY");
    public static string Instructions => L("INSTRUCTIONS", "INSTRUCTIONS");
    public static string HallOfFame => L("TABLEAU D'HONNEUR", "HALL OF FAME");
    public static string LanguageItem => L("LANGUE : FRANÇAIS", "LANGUAGE: ENGLISH");
    public static string Volume => L("VOLUME", "VOLUME");
    public static string DifficultyItem => L("DIFFICULTÉ", "DIFFICULTY");
    public static string DifficultyName(Difficulty d) => d switch
    {
        Difficulty.Easy => L("FACILE", "EASY"),
        Difficulty.Hard => L("DIFFICILE", "HARD"),
        _ => L("NORMAL", "NORMAL"),
    };
    public static string Quit => L("QUITTER", "QUIT");
    public static string VolumeName(int level) => level switch
    {
        0 => L("COUPÉ", "OFF"),
        1 => L("FAIBLE", "LOW"),
        2 => L("MOYEN", "MEDIUM"),
        _ => L("FORT", "HIGH"),
    };

    // ---------------- status bar ----------------
    public static string Score => L("SCORE", "SCORE");
    public static string Air => L("AIR", "AIR");
    public static string Paused => L("PAUSE", "PAUSED");
    public static string Resume(bool mobile) => mobile ? L("TOUCHEZ POUR REPRENDRE", "TAP TO RESUME") : L("ESPACE : REPRENDRE", "SPACE TO RESUME");
    public static string BackToMenu(bool mobile) => mobile ? L("RETOUR : MENU", "BACK: MENU") : L("ÉCHAP : MENU", "ESC: MENU");
    public static string Demo => L("DÉMO", "DEMO");

    public static string HintKey(bool mobile) => mobile ? "" : "E: ";
    public static string HintRide => L("MONTER", "RIDE");
    public static string HintOpen => L("OUVRIR", "OPEN");
    public static string HintClose => L("FERMER", "CLOSE");
    public static string HintRead => L("LIRE", "READ");
    public static string HintPull => L("TIRER", "PULL");
    public static string HintDrink => L("BOIRE", "DRINK");
    public static string HintSearch => L("FOUILLER", "SEARCH");

    // ---------------- messages ----------------
    public static string Intro => L("LA PIERRE FABULEUSE EST DANS LE TOMBEAU...", "THE FABULOUS STONE LIES IN THE TOMB...");
    public static string FoundPistol =>
        L("UN SQUELETTE TENAIT UN PISTOLET LASER ! (12 TIRS)", "A SKELETON HELD A LASER PISTOL! (12 SHOTS)");
    public static string FoundAmmo => L("MUNITIONS ! +8 TIRS", "AMMUNITION! +8 SHOTS");
    public static string FoundGem => L("UNE PIERRE PRÉCIEUSE ! +100", "A PRECIOUS GEM! +100");
    public static string FoundKey => L("LA CLÉ DE JADE !", "THE JADE KEY!");
    public static string FoundBook => L("LE LIVRE SACRÉ !", "THE SACRED BOOK!");
    public static string FoundIdol => L("UNE IDOLE D'OR ! ELLE A UNE JUMELLE...", "A GOLDEN IDOL! IT HAS A TWIN...");
    public static string FoundBothIdols => L("LES DEUX IDOLES JUMELLES SONT À VOUS !", "YOU HAVE BOTH TWIN IDOLS!");
    public static string FoundHelmet =>
        L("UN CASQUE DE PLONGÉE ! VOUS RESPIREZ SOUS L'EAU.", "A DIVING HELMET! NOW YOU CAN BREATHE UNDER WATER.");
    public static string NoWeapon => L("VOUS N'AVEZ PAS D'ARME !", "YOU HAVE NO WEAPON!");
    public static string NoAmmo => L("PLUS DE MUNITIONS !", "OUT OF AMMUNITION!");
    public static string LowAmmo => L("ATTENTION : PLUS QUE 3 TIRS !", "CAREFUL: ONLY 3 SHOTS LEFT!");
    public static string GuardianImmune =>
        L("SON ARMURE LE PROTÈGE ! FUYEZ !", "ITS ARMOUR PROTECTS IT! RUN!");
    public static string ChestDestroyed => L("VOUS AVEZ DÉTRUIT UN COFFRE !", "YOU HAVE DESTROYED A CHEST!");
    public static string AmmoDestroyed => L("VOUS AVEZ DÉTRUIT DES MUNITIONS !", "YOU HAVE DESTROYED SOME AMMUNITION!");
    public static string ChestOpened => L("UN COFFRE PLEIN D'OR ! +500", "A CHEST FULL OF GOLD! +500");
    public static string SecretFound => L("UN PASSAGE SECRET !", "A SECRET PASSAGE!");
    public static string SolidStone => L("DE LA PIERRE, RIEN QUE DE LA PIERRE.", "SOLID STONE.");
    public static string JadeLocked => L("UNE PORTE DE JADE, FERMÉE À CLÉ.", "A JADE DOOR. IT IS LOCKED.");
    public static string JadeOpens => L("LA CLÉ DE JADE OUVRE LA PORTE !", "THE JADE KEY OPENS THE DOOR!");
    public static string GateWontOpen => L("CETTE PORTE NE S'OUVRE PAS.", "THIS DOOR WILL NOT OPEN.");
    public static string BookOpens => L("LE LIVRE SACRÉ OUVRE LA PORTE !", "THE SACRED BOOK OPENS THE DOOR!");
    public static string TwinsOpen => L("LES IDOLES JUMELLES OUVRENT LA PORTE !", "THE TWIN IDOLS OPEN THE DOOR!");
    public static string PortcullisRises => L("UNE HERSE SE LÈVE AU LOIN...", "SOMEWHERE, A PORTCULLIS RISES...");
    public static string PortcullisFalls => L("UNE HERSE RETOMBE AU LOIN...", "SOMEWHERE, A PORTCULLIS FALLS...");
    public static string PortcullisStuck =>
        L("ELLE NE BOUGE PAS. IL Y A SÛREMENT UN MÉCANISME.", "IT WILL NOT BUDGE. THERE MUST BE A MECHANISM.");
    public static string StoneTaken =>
        L("LA PIERRE MIRACULEUSE ! VITE, SORTEZ DU TOMBEAU !", "THE MIRACULOUS STONE! NOW ESCAPE FROM THE TOMB!");
    public static string SarcophagusEmpty => L("LE SARCOPHAGE EST VIDE.", "THE SARCOPHAGUS IS EMPTY.");
    public static string BoulderComing => L("UN GRONDEMENT... UN ROCHER ARRIVE !", "A RUMBLE... A BOULDER IS COMING!");
    public static string HoldBreath => L("RETENEZ VOTRE SOUFFLE !", "HOLD YOUR BREATH!");
    public static string CheckpointReached => L("LA PIERRE DU SOLEIL BRILLE...", "THE SUN STONE GLOWS...");
    public static string Teleported => L("VOUS ÊTES TRANSPORTÉ AILLEURS !", "YOU ARE CARRIED AWAY!");
    public static string RideCart => L("EN ROUTE !", "ALL ABOARD!");
    public static string LivesLeft(int n) => n == 1
        ? L("ATTENTION : DERNIÈRE VIE !", "CAREFUL: LAST LIFE!")
        : L($"IL VOUS RESTE {n} VIES.", $"YOU HAVE {n} LIVES LEFT.");

    public static string DeathText(DeathCause cause) => cause switch
    {
        DeathCause.Ghoul => L("LES GRIFFES DE LA GOULE ! VOUS ÊTES MORT.", "THE GHOUL'S CLAWS! YOU ARE DEAD."),
        DeathCause.Guardian => L("ÉCRASÉ PAR LE GARDIEN DE FER !", "CRUSHED BY THE IRON GUARDIAN!"),
        DeathCause.Fish => L("DÉVORÉ PAR LES PIRANHAS !", "EATEN BY PIRANHAS!"),
        DeathCause.Dart => L("UNE FLÉCHETTE EMPOISONNÉE ! VOUS ÊTES MORT.", "A POISONED DART! YOU ARE DEAD."),
        DeathCause.Pit => L("VOUS ÊTES TOMBÉ DANS UN TROU !", "YOU FELL INTO A PIT!"),
        DeathCause.Chasm => L("VOUS ÊTES TOMBÉ DANS L'ABÎME !", "YOU FELL INTO THE ABYSS!"),
        DeathCause.Drowned => L("VOUS VOUS ÊTES NOYÉ !", "YOU HAVE DROWNED!"),
        DeathCause.Boulder => L("ÉCRASÉ PAR UN ROCHER !", "FLATTENED BY A BOULDER!"),
        DeathCause.Collapse => L("LE SOL S'EFFONDRE SOUS VOS PIEDS !", "THE FLOOR GIVES WAY BENEATH YOU!"),
        DeathCause.Youth => L("LA FONTAINE DE JOUVENCE ! VOUS RAJEUNISSEZ... JUSQU'À DISPARAÎTRE !",
            "THE WATER OF YOUTH! YOU GROW YOUNGER AND YOUNGER... AND VANISH!"),
        _ => "",
    };

    // ---------------- inscriptions ----------------
    public static string InscriptionHeader => L("UNE INSCRIPTION", "AN INSCRIPTION");
    public static string CannotRead =>
        L("VOUS NE SAVEZ PAS LIRE CES SIGNES... PAS ENCORE.", "YOU CANNOT READ THESE SIGNS... NOT YET.");
    public static string RosettaNote =>
        L("DESSOUS, SA TRADUCTION ! VOUS SAVEZ LIRE LES SIGNES.", "BELOW IT, A TRANSLATION! NOW YOU CAN READ THE SIGNS.");
    public static string CloseReading(bool mobile) => mobile ? L("TOUCHEZ POUR CONTINUER", "TAP TO CONTINUE") : L("E : CONTINUER", "E: CONTINUE");

    /// <summary>The message carved on an inscription (0 is the great inscription).</summary>
    public static string Inscription(int id) => id switch
    {
        0 => L("À LA GLOIRE D'AXAYACATL", "TO THE GLORY OF AXAYACATL"),
        1 => L("LES DOUBLES SONT UNE CLÉ", "THE TWINS ARE A KEY"),
        2 => L("NE BUVEZ PAS L'EAU DE LA FONTAINE", "DO NOT DRINK FROM THE FOUNTAIN"),
        3 => L("LES PASSAGES SECRETS SONT DIFFÉRENTS DES MURS", "SECRET PASSAGES ARE NOT LIKE THE WALLS"),
        4 => L("LE LIVRE SACRÉ OUVRE LA PORTE", "THE SACRED BOOK OPENS THE DOOR"),
        5 => L("LE SIGNE DU CRÂNE SIGNIFIE LA MORT", "THE SIGN OF THE SKULL MEANS DEATH"),
        _ => "",
    };

    /// <summary>The same message in the old script ('#' is the skull sign).</summary>
    public static string InscriptionGlyphs(int id) => id == 5 ? L("# SIGNIFIE LA MORT", "# MEANS DEATH") : Inscription(id);

    // ---------------- end of game ----------------
    public static string GameOver => L("PARTIE TERMINÉE", "GAME OVER");
    public static string Victory => L("VICTOIRE !", "VICTORY!");
    public static string VictoryText1 => L("VOUS ÊTES SORTI DU TOMBEAU", "YOU HAVE ESCAPED FROM THE TOMB");
    public static string VictoryText2 => L("AVEC LA PIERRE MIRACULEUSE !", "WITH THE MIRACULOUS STONE!");
    public static string LostText => L("LE TOMBEAU GARDE SON SECRET...", "THE TOMB KEEPS ITS SECRET...");
    public static string TreasureLine(int found, int total) => L($"TRÉSORS : {found}/{total}", $"TREASURE: {found}/{total}");
    public static string SlainLine(int n) => L($"MONSTRES DÉTRUITS : {n}", $"MONSTERS DESTROYED: {n}");
    public static string TimeTaken(int seconds) => L("TEMPS : ", "TIME: ") + $"{seconds / 60}:{seconds % 60:D2}";
    public static string BonusLine(int bonus) => L($"BONUS : {bonus}", $"BONUS: {bonus}");
    public static string YourScore => L("VOTRE SCORE", "YOUR SCORE");
    public static string EnterName => L("ENTREZ VOTRE NOM", "ENTER YOUR NAME");
    public static string ChangeLetter(bool mobile) =>
        mobile ? L("TOUCHEZ < > POUR CHANGER", "TAP < > TO CHANGE LETTER") : L("FLÈCHES OU CLAVIER", "CURSOR KEYS OR TYPE");
    public static string AcceptLetter(bool mobile) =>
        mobile ? L("TOUCHEZ LA LETTRE POUR VALIDER", "TAP THE LETTER TO ACCEPT") : L("ESPACE POUR VALIDER", "SPACE TO ACCEPT");
    public static string EndMarkHint => L("< TERMINE LE NOM", "< ENDS THE NAME");
    public static string NameHeader => L("NOM", "NAME");
    public static string PlayOrMenu(bool mobile) =>
        mobile ? L("TOUCHEZ POUR JOUER", "TAP TO PLAY") : L("ESPACE : JOUER   ÉCHAP : MENU", "SPACE: PLAY   ESC: MENU");

    // ---------------- instructions ----------------
    public static string Page(int page, int pages) => $"{page}/{pages}";
    public static string NextPage(bool mobile) =>
        mobile ? L("TOUCHEZ POUR CONTINUER", "TAP TO CONTINUE") : L("ESPACE POUR CONTINUER", "SPACE TO CONTINUE");

    public static string StoryHeader => L("L'HISTOIRE", "THE STORY");
    public static string RulesHeader => L("LES DANGERS", "THE DANGERS");
    public static string SecretsHeader => L("LES SECRETS", "THE SECRETS");
    public static string ControlsHeader => L("LES COMMANDES", "THE CONTROLS");

    public static string[] Story => Current == Language.French
        ?
        [
            "EN PLEINE FORÊT MEXICAINE,",
            "HEURE ET JOUR INCONNUS.",
            "",
            "APRÈS DES JOURS DE MARCHE, J'APERÇOIS",
            "ENFIN LA PYRAMIDE. LE LIEU LÉGENDAIRE",
            "EXISTE, ET IL CACHERAIT UNE PIERRE",
            "FABULEUSE...",
            "",
            "J'ENTRE, ET JE DESCENDS L'ESCALIER",
            "QUI MÈNE AU TOMBEAU D'AXAYACATL.",
            "J'ENTENDS DES BRUITS DE PAS.",
            "JE NE SUIS PAS SEUL...",
            "",
            "TROUVEZ LA PIERRE MIRACULEUSE",
            "ET RESSORTEZ VIVANT DE LA PYRAMIDE.",
        ]
        :
        [
            "DEEP IN THE MEXICAN JUNGLE,",
            "TIME AND DAY UNKNOWN.",
            "",
            "AFTER DAYS OF WALKING I SEE IT AT",
            "LAST: THE PYRAMID. THE LEGEND IS TRUE,",
            "AND SOMEWHERE INSIDE LIES A FABULOUS",
            "STONE...",
            "",
            "I ENTER, AND GO DOWN THE STAIRS THAT",
            "LEAD TO THE TOMB OF AXAYACATL.",
            "I HEAR FOOTSTEPS.",
            "I AM NOT ALONE...",
            "",
            "FIND THE MIRACULOUS STONE AND",
            "GET OUT OF THE PYRAMID ALIVE.",
        ];

    public static string[] Rules => Current == Language.French
        ?
        [
            "LES GOULES FONCENT SUR VOUS. LEUR",
            "CONTACT EST MORTEL : TIREZ !",
            "",
            "LES GARDIENS DE FER SONT INVULNÉRABLES.",
            "UNE SEULE SOLUTION : LA FUITE.",
            "",
            "LES MUNITIONS SONT RARES. ET LE LASER",
            "DÉTRUIT AUSSI LES COFFRES...",
            "",
            "FLÉCHETTES, TROUS, ROCHERS, EAUX",
            "PROFONDES : SAUTEZ, COUREZ, NAGEZ.",
            "SOUS L'EAU, SURVEILLEZ VOTRE AIR.",
            "",
            "LES MONSTRES NE FRANCHISSENT NI LES",
            "ESCALIERS NI LES SQUELETTES.",
        ]
        :
        [
            "GHOULS RUSH AT YOU, AND THEIR TOUCH",
            "IS DEADLY: SHOOT THEM!",
            "",
            "THE IRON GUARDIANS CANNOT BE HARMED.",
            "THERE IS ONLY ONE ANSWER: RUN.",
            "",
            "AMMUNITION IS SCARCE. AND THE LASER",
            "DESTROYS CHESTS TOO...",
            "",
            "DARTS, PITS, BOULDERS AND DEEP WATER:",
            "JUMP, RUN AND SWIM.",
            "UNDER WATER, WATCH YOUR AIR.",
            "",
            "MONSTERS WILL NOT CROSS STAIRS",
            "OR SKELETONS.",
        ];

    public static string[] Secrets => Current == Language.French
        ?
        [
            "LES BÂTISSEURS ONT LAISSÉ DES MESSAGES",
            "DANS LEUR ÉCRITURE. LA GRANDE",
            "INSCRIPTION DE L'ENTRÉE PORTE SA",
            "TRADUCTION : LISEZ-LA D'ABORD.",
            "",
            "CERTAINS MURS SONT DES PORTES",
            "SECRÈTES : FOUILLEZ CEUX QUI",
            "SEMBLENT DIFFÉRENTS.",
            "",
            "CHARIOT, TÉLÉPORTEUR, LEVIER : LES",
            "MACHINES DES ANCIENS MÈNENT LÀ OÙ",
            "LES PIEDS NE VONT PAS.",
            "",
            "LES PIERRES DU SOLEIL GARDENT VOTRE",
            "PROGRESSION. VIES : 5 EN FACILE,",
            "3 EN NORMAL, 1 EN DIFFICILE.",
        ]
        :
        [
            "THE BUILDERS LEFT MESSAGES IN THEIR",
            "OWN SCRIPT. THE GREAT INSCRIPTION",
            "BY THE ENTRANCE CARRIES ITS OWN",
            "TRANSLATION: READ IT FIRST.",
            "",
            "SOME WALLS ARE SECRET DOORS: SEARCH",
            "THE ONES THAT LOOK DIFFERENT.",
            "",
            "MINE CART, TELEPORTER, LEVER: THE",
            "MACHINES OF THE ANCIENTS GO WHERE",
            "FEET CANNOT.",
            "",
            "SUN STONES REMEMBER YOUR PROGRESS.",
            "LIVES: 5 ON EASY, 3 ON NORMAL,",
            "1 ON HARD (AS IN THE ORIGINAL).",
        ];

    public static (string key, string action)[] Controls(bool mobile) => mobile
        ? Current == Language.French
            ?
            [
                ("MANCHE (GAUCHE)", "MARCHER"),
                ("BOUTON LASER", "TIRER"),
                ("BOUTON SAUT", "SAUTER / CHARIOT"),
                ("BOUTON MAIN", "OUVRIR, LIRE..."),
                ("BOUTON PAUSE", "PAUSE"),
                ("RETOUR", "MENU"),
            ]
            :
            [
                ("STICK (LEFT)", "WALK"),
                ("LASER BUTTON", "FIRE"),
                ("JUMP BUTTON", "JUMP / RIDE"),
                ("HAND BUTTON", "OPEN, READ..."),
                ("PAUSE BUTTON", "PAUSE"),
                ("BACK", "MENU"),
            ]
        : Current == Language.French
            ?
            [
                ("FLÈCHES / WASD", "MARCHER"),
                ("ESPACE", "TIRER"),
                ("I J K L", "TIRER EN HAUT..."),
                ("X", "SAUTER / CHARIOT"),
                ("E / ENTRÉE", "OUVRIR, LIRE..."),
                ("P", "PAUSE"),
                ("ÉCHAP", "MENU"),
                ("F11", "PLEIN ÉCRAN"),
            ]
            :
            [
                ("CURSORS / WASD", "WALK"),
                ("SPACE", "FIRE"),
                ("I J K L", "FIRE UP/LEFT/..."),
                ("X", "JUMP / RIDE"),
                ("E / ENTER", "OPEN, READ..."),
                ("P", "PAUSE"),
                ("ESC", "MENU"),
                ("F11", "FULL SCREEN"),
            ];
}
