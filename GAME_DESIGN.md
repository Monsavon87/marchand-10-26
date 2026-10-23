# Marchand — Décisions de game design

Ce fichier regroupe les décisions de conception **prises et validées**, pour ne pas avoir à y revenir.
Il ne décrit pas le code (voir `PROJECT_LOG.md` pour l'historique des étapes, `CLAUDE.md` pour les règles de travail).
Quand une décision change, on modifie la ligne concernée et on note la date. Les points non tranchés sont à la fin.

Dernière mise à jour : 2026-10-23 (gestion du combat).

## 1. Le jeu

- « Marchand » : RPG où le joueur incarne un marchand qui voyage, fait du commerce et se bat. Prototype Unity 6, URP.
- Visuels : uniquement des primitives et du texte. Aucun asset externe.
- Langue : le jeu (messages, interface) est en français ; le code et ses commentaires sont en anglais.
- Les données de jeu (stats, règles, réglages) vivent dans des ScriptableObjects, jamais en dur dans le code.

## 2. Principe d'architecture central

- **Un seul système de déplacement** pour la carte de campagne et le combat tactique, pour toutes les unités (joueur et ennemis) : `UnitMover` est le seul script qui touche au NavMeshAgent.
- On ajoute des couches par-dessus, on ne réécrit ni ne duplique l'existant. Toute modification de `UnitMover` doit être justifiée avant.
- Le joueur et l'IA agissent **exactement par le même chemin** (le système d'actions).

## 3. Terrain

- Chaque zone de terrain a un coût de chemin et un multiplicateur de vitesse (route rapide, forêt, marais, montagne lents), dans `TerrainSettings`.
- Une unité préfère un chemin plus long sur terrain facile à un chemin court sur terrain difficile.
- Les zones infranchissables existent (lac).

## 4. Carte de campagne

- La carte est une **miniature** (façon Bannerlord). Le groupe du joueur est **une seule unité** : la caravane.
- Le temps de jeu avance **uniquement quand la caravane se déplace** : la distance parcourue est convertie en temps (réglage : 10 minutes de jeu par mètre en terrain normal ; le terrain lent allonge, la route raccourcit). Objectif : 1 à 2 jours entre deux villes.
- Les villes sont des cubes ; un clic sur une ville envoie la caravane à son entrée ; l'arrivée déclenche un événement « Arrivé à … ».
- Rencontre : quand la caravane et un ennemi se touchent, les deux s'arrêtent et un événement « rencontre » est déclenché. Ce sera plus tard la boîte de dialogue avant combat.

## 5. Contrôles et caméra

- Clic gauche : sélectionner une unité (Maj = ajouter/retirer) ou donner un ordre sur le sol ; glisser = rectangle de sélection ; Échap = tout désélectionner. Un rectangle vide ne change pas la sélection ; il faut bouger de plus de 16 px pour qu'un clic devienne un rectangle.
- Caméra : déplacement ZQSD (positions physiques) et bords d'écran, clic droit maintenu = glisser la vue (icône main), bouton du milieu maintenu = rotation (icône dédiée), molette = zoom, F = centrer.
- Espace = pause (en combat).

## 6. Combat tactique

### 6.1 Principes
- Vue isométrique 3D, **distances réelles, pas de grille**, dans l'esprit de Baldur's Gate.
- **Temps réel avec pause.** On peut donner des ordres pendant la pause ; ils s'exécutent à la reprise.
- Périmètre du prototype : 2 camps (Joueur, Ennemi), 2 types d'unités (mêlée et distance), une arène extérieure et une arène intérieure.

### 6.2 Temps d'action (pas de manches)
- **Il n'y a plus de manches** ni de réserve de temps par unité (décision du 2026-10-23). Chaque action a simplement **un coût en temps** : marcher dure le temps de marche, attendre dure sa durée, une attaque dure `attackTime`.
- Une unité fait **une seule action à la fois** ; rien d'autre ne la plafonne. Les ennemis suivent exactement les mêmes règles que le joueur.
- Un **système d'initiative** viendra plus tard pour décider qui agit quand ; il est à définir.
- Les vitesses sont réalistes : marche **1,4 m/s** pour les joueurs, **1,2 m/s** pour les ennemis. La course et le sprint viendront plus tard.

### 6.3 Ordres et file d'ordres
- Un **clic** donne un ordre qui **remplace** l'action en cours et **vide la file**. **Maj + clic** **ajoute** l'ordre à la **file** de l'unité (jusqu'à **4 ordres** derrière l'action en cours, réglable dans `CombatSettings`). **X** annule l'action en cours et la file des unités sélectionnées. Hors combat (carte de campagne), il n'y a pas de file : Maj + clic est un ordre normal. (Décision du 2026-10-23, elle remplace l'ancien « pas de file d'attente ».)
- Les ordres peuvent être donnés pendant la pause ; ils s'exécutent à la reprise.
- **Aperçu avec Maj** : quand Maj est enfoncée, la ligne du nouvel ordre part du **point où l'unité sera après ses ordres déjà donnés** (fin du dernier déplacement en file, ou position de la dernière cible à attaquer), pas de l'unité. Sans ordre en file, ou hors combat, elle part de l'unité.
- **Chemin de la file** : tant qu'une unité est **sélectionnée**, le chemin de **tous** les ordres en file derrière l'ordre en cours est tracé, bout à bout (une ligne fine cyan). Il disparaît quand l'unité n'est plus sélectionnée. Pareil pour la ligne du trajet en cours et son repère de destination : visibles seulement pour une unité sélectionnée.
- Un ordre en file dont la cible est morte (ou impossible) est **ignoré** : la file passe au suivant.
- Un ordre de déplacement, même très long, est exécuté jusqu'au bout. L'aperçu du trajet est un seul trait vers le point visé (pas d'aperçu de durée).
- À l'arrêt, une unité fait face au point visé (et une unité immobile est un obstacle : les autres la contournent).
- **Récupération** : après une attaque, une unité récupère **0,5 s** (réglable dans `ActionTimingSettings`, une valeur par type d'action) : entre deux coups, et avant l'ordre suivant de la file.

### 6.3 bis Gestion du combat
- Un combat **commence** avec les unités présentes et **se termine** quand un camp n'a plus d'unité vivante (victoire du camp restant, ou match nul si les deux tombent ensemble). Événements : début de combat, pause/reprise (avec la raison), fin de combat avec le vainqueur.
- **Espace** met en pause (temps de jeu suspendu, interface utilisable, ordres possibles). **Pauses automatiques** (réglables, activées par défaut) : quand une unité alliée meurt, et quand **une unité ennemie repère** une unité du joueur (décision du 2026-10-23, elle remplace « un ennemi entre en vue du joueur ») : c'est l'IA ennemie qui détecte (vue de l'ennemi, ligne de vue, ou attaque reçue), une seule pause par unité ennemie, la première fois. Le jeu affiche la raison (« un ennemi vous a repéré ») sous la bannière PAUSE ; à la fin du combat, la bannière PAUSE disparaît pour laisser place au résultat.
- La mort d'une unité vide sa file et annule son action ; les ordres des autres qui la visaient s'arrêtent ou sont ignorés quand leur tour vient.

### 6.4 Unités
- Deux unités de départ : **Fantassin** (mêlée) et **Archer** (distance). Chaque type est décrit par une `UnitDefinition` : PV max, armure, bonus d'attaque, défense, dégâts de l'arme, allonge, zone morte, portée maximale (0 = mêlée), temps d'une attaque, vitesse.
- Une unité a **une seule arme** : mêlée si portée maximale = 0 (c'est l'allonge qui compte), à distance sinon (portée maximale, avec une zone morte minimale éventuelle).
- À 0 PV, l'unité arrête tout, est retirée du jeu et libère sa place sur le NavMesh.

### 6.5 Règles de résolution (simplifiées, esprit d100)
- Jet d100 + bonus d'attaque, comparé à la défense de la cible ; si touché, dégâts de l'arme moins l'armure (minimum 0).
- Toutes les valeurs sont dans des données. Les règles sont **isolées** (`CombatRules`) pour pouvoir remplacer le système plus tard sans toucher au reste.

### 6.5 bis Attaque au corps à corps (étape C3)
- Une attaque **dure `attackTime` secondes** (2 s par défaut) : c'est ce qui empêche les attaques en rafale (un coup toutes les 2 s).
- Une demande « Attaquer » **dure jusqu'à la mort de la cible** (ou jusqu'à un nouvel ordre) : l'unité se rapproche seule, se tourne, frappe, recommence.
- Le coup touche si **jet + bonus d'attaque ≥ défense** ; dégâts = dégâts de l'arme − armure, minimum 0. Valeurs de test : Fantassin attaque 25 / défense 50 (76 % de toucher contre un Fantassin), Archer attaque 20 / défense 35.
- L'**allonge** se mesure de **centre à centre**. Une unité qui s'approche s'arrête à 85 % de son allonge.
- Un clic gauche sur un ennemi = ordre d'attaque (dans la scène de combat) ; sur la carte de campagne, c'est un simple déplacement. L'IA attaque par **le même chemin** que le joueur.
- Retour visuel : cercle d'allonge au sol (et cercle intérieur de zone morte), surbrillance de l'ennemi survolé avec la **chance de toucher**, texte flottant (dégâts ou « Raté »), journal de combat à l'écran.
- Les attaques à distance sont décrites au §6.5 quater.

### 6.5 ter Orientation et zones d'attaque (étape C4)
- L'**orientation** d'une unité compte : selon l'endroit d'où l'attaquant frappe, il touche la **face**, le **flanc** ou le **dos** de sa cible. Les angles sont des paramètres (`AttackZoneSettings`) : par défaut face ±60° (120° devant), dos ±60° (120° derrière), flancs de 60° de chaque côté.
- Chaque zone a un **bonus de toucher** (ajouté au total du jet) et un **bonus de dégâts** (ajouté aux dégâts de l'arme, **avant** l'armure). Valeurs de test : face +0/+0, flanc +10/+2, dos +20/+4 (Fantassin contre Fantassin : 76 %/8, 86 %/10, 96 %/12). Tout passe par `CombatRules`.
- La zone est celle où se trouve l'attaquant **au moment où le coup porte** (fin des 2 s de l'attaque).
- **Focus** (décision du 2026-10-23) : une unité **garde son regard sur sa première cible**. Celle qu'elle a reçu l'ordre d'attaquer, ou à défaut le **premier** qui l'a frappée (elle se tourne alors vers lui). Un second attaquant ne la fait pas pivoter : s'il est de dos, il garde son bonus de dos à chaque coup. Le focus dure jusqu'à la mort de la cible, ou jusqu'à un nouvel ordre (déplacer, attendre, attaquer quelqu'un d'autre). Une unité en marche regarde où elle va. **Plus tard**, une notion de sélection de cible ou de gestion d'aggro remplacera cette règle simple.
- L'aperçu au survol montre les zones autour de l'ennemi (face gris, flancs jaunes, dos rouge, la zone visée en gras) et « Toucher : x % / Dos : +20 toucher, +4 dégâts ». Le journal indique la zone et ses bonus.
- Une unité qui a repéré son attaquant se tourne vers lui avant le coup (l'IA poursuit et fait face) : surprendre demande une cible inattentive.

### 6.5 quater Attaque à distance (étape C6)
- L'action **Tirer** (unités dont la portée maximale > 0) remplace « Attaquer » pour l'Archer. Hors de portée, l'unité s'approche (jusqu'à 85 % de sa portée) ; en portée, elle se tourne vers la cible, **vise pendant `attackTime`** (2 s), puis tire, puis récupère comme après toute attaque.
- **Ligne de vue** : un rayon part de l'œil de l'archer vers l'œil de la cible. Tout objet solide du décor bloque le tir (pilier, mur, muret) ; les **unités ne bloquent pas** la ligne. Pour un décor qui ne doit pas bloquer (herbe haute, barrière basse), on lui ajoute le composant **`ShotPassThrough`** (pas de couche ni d'étiquette dédiée : on ne touche pas aux `ProjectSettings`). Les couches qui bloquent sont celles de `SightBlockers` dans `CombatSettings` (les mêmes que pour la vue).
- **Zone morte** : sous la zone morte de l'arme, on ne peut pas tirer.
- **Tir impossible** (ligne bloquée ou cible dans la zone morte) : avant que le combat soit engagé, l'ordre est **refusé** avec la raison. Le combat engagé, l'unité **tente de se repositionner** : elle cherche autour de la cible le point accessible le plus proche, en portée, hors zone morte, avec une ligne dégagée, et y va (jusqu'à 3 tentatives), puis tire. Sans solution, l'action se termine « impossible » et le journal le dit.
- **Malus de distance** : des **paliers en fraction de la portée maximale**, modifiables dans `CombatRulesSettings` (valeurs de test : jusqu'à 50 % → 0, jusqu'à 75 % → −10, jusqu'à 100 % → −25 au total du jet). Il s'ajoute au jet comme les bonus de zone. Les zones de C4 s'appliquent aux tirs (zone d'où part la flèche).
- **La flèche** (petit projectile visible, `Projectile`) **vole vers le point visé au moment du tir** et ne suit pas sa cible (décision du 2026-10-23 : ce sont des flèches, pas des missiles à tête chercheuse). Vitesse et épaisseur dans `CombatSettings`. Elle touche **la première unité** qu'elle rencontre ; à l'impact seulement on lance le dé et on applique zone, malus de distance et dégâts. Si elle n'en rencontre aucune (cible partie), elle se perd (« La flèche de X se perd »). Elle suit le temps de jeu : figée pendant la pause.
- **Tirs alliés** : paramètre `allowFriendlyFire` de `CombatSettings`, **désactivé par défaut**. Désactivé, les flèches passent à côté des unités de leur propre camp. Activé, la première unité sur la trajectoire est touchée, amie ou ennemie (l'aperçu prévient : « Touchera X en chemin »).
- **Aperçu** (archer sélectionné, ennemi survolé) : le cercle de portée et de zone morte au sol, une **ligne de tir verte** si la ligne est dégagée ou **rouge** si elle est bloquée / cible trop proche, et le texte « Distance : 13,1 m (−10 toucher) », « TIR BLOQUÉ » ou « TROP PRÈS », la chance de toucher avec le malus, les bonus de zone. Le **journal** écrit « X tire sur Y (face, distance 13,1 m) : jet 76 + 20 + 10 (flanc) − 10 (distance) = 96 contre 50 > touché ... ».

### 6.6 Intelligence artificielle
- Patrouille entre des points ; poursuite d'une cible **vue** (détection **visuelle uniquement** : un mur bloque la vue ; l'ouïe sera une autre source de détection plus tard).
- Cible perdue : l'ennemi va au dernier point vu, reste un moment à scruter, puis reprend ce qu'il faisait avant la poursuite.

### 6.6 bis IA de combat (étape C7)
- Quand le combat est engagé et qu'une unité IA **connaît** un ennemi, le composant **`AICombatBrain`** prend le relais d'`AIUnitController` (qui garde la patrouille, la poursuite et la recherche hors combat). Il décide toutes les 0,5 s (réglable). Quand plus aucun ennemi n'est connu pendant 5 s, il rend la main : l'unité scrute puis reprend sa patrouille.
- **Aucun avantage caché** : l'IA ne connaît que les ennemis qu'elle voit (ligne de vue, dans sa portée de vision : 9 m, ou la portée de son arme si elle est à distance) et celui qui l'a attaquée. Elle agit uniquement par `ActionController` (déplacer, attendre, attaquer ou tirer), avec les mêmes contraintes que le joueur.
- **Choix de la cible** (profil) : règle de base `Nearest` (la plus proche), `Weakest` (la moins de PV) ou `MostDangerous` (dégâts par seconde de l'arme), plus deux bonus : **point faible** (flanc ou dos de l'ennemi atteignable depuis sa position) et **protection des alliés** (l'ennemi qui attaque un allié est prioritaire). Elle **garde sa cible** (règle du focus) sauf si une autre est nettement meilleure (marge) et pas plus souvent que le délai du profil.
- **Mêlée** : avance et frappe ; si elle est loin de sa cible, elle **contourne** pour l'attaquer par le flanc ou le dos quand le détour reste raisonnable (≤ 1,5× le trajet direct). **Distance** : tire (avec le repositionnement de C6) et **garde ses distances** : si un ennemi passe sous la distance de confort (40 % de la portée), elle recule vers un point avec ligne de vue, 2 fois de suite au plus, puis tire quand même.
- **Repli** (profil) : sous un seuil de PV, l'unité va vers le point accessible le plus éloigné des ennemis, de préférence hors de leur vue ; elle y reste. Si un ennemi la rattrape, elle repart (jusqu'à 4 fois), puis se bat. Acculée, elle se bat. Elle **reprend le combat** quand ses PV dépassent le seuil de retour (prévu pour le soin, qui n'existe pas encore : sans soin, elle ne revient pas).
- **Profils** (`AIProfile`, ScriptableObject) : **Agressif** (ne se replie jamais, la plus proche) et **Prudent** (se replie sous 35 % de PV, la plus faible, évite les positions exposées : flanc ou dos d'un ennemi proche). Tous les seuils et poids sont des paramètres.
- **Debug** : la case « Show Decisions » du cerveau écrit chaque décision dans la console (« Enemy_Guard · nouvelle cible Player_3 (la plus faible, 12 PV, point faible exposé) »). La dernière décision est lisible (`LastDecision`).

### 6.7 Interface de combat (étape C8)
- Principe : l'interface **lit** l'état et les événements du jeu et **demande** les ordres par `ActionController` ; elle ne contient aucune règle, et le jeu ne dépend pas d'elle. Textes en `Text` Unity de base (pas de TextMeshPro pour l'instant), sans habillage graphique.
- **Panneau d'unité** (en haut à gauche, unité sélectionnée) : nom et type, barre et valeur de PV, armure, défense, attaque, arme (mêlée : dégâts, allonge, temps du coup ; distance : dégâts, portée, zone morte, temps du tir), ordres en cours. Il n'y a pas de budget de temps : ce sont les ordres qui sont affichés.
- **Barre de vie** au-dessus de chaque unité, tournée vers la caméra : bleue pour le joueur, rouge pour l'ennemi.
- **Cartes des unités du joueur** (en bas, au centre) : nom, PV ; la sélectionnée est en surbrillance ; un clic sélectionne l'unité. **Barre d'ordres** (au-dessus des boutons) : l'action en cours (en couleur) puis la file.
- **Journal défilant** (en bas à gauche, molette) : coloré (touché du joueur en vert, touché ennemi en rouge, raté en gris, notes en jaune, grands moments en bleu) ; il suit la dernière ligne sauf si le joueur remonte pour lire.
- **Aperçu au survol** : chance de toucher, **dégâts estimés** (« Dégâts : 8 (PV 40 > 32) », calculés par les règles), zone, distance et malus, ligne de tir, portée au sol.
- **Boutons et raccourcis** : **Attaquer (C)** active un mode « choisir une cible » (le prochain clic sur un ennemi attaque, un clic au sol ne donne aucun ordre, Échap annule) ; **Attendre (T)** 2 s ; **Annuler (X)** ; **Pause / Reprendre (Espace)**. La touche A reste à la caméra (déplacement). Un clic sur l'interface n'est jamais pris comme un clic dans le monde.
- **Message de refus** (sous la bannière) : « Player_2 : la ligne de tir est bloquée », « la cible est trop proche », « aucun chemin... », visible 3 s. Seuls les refus du camp du joueur s'affichent.
- **Écran de fin** : « Victoire ! », « Défaite » ou « Match nul », bouton **Continuer** : recharge la scène de combat (champ « scène suivante » prévu pour la campagne plus tard).
- Scène de validation : `Test_Combat_2v2` (Fantassin + Archer de chaque côté, ennemis pilotés par l'IA : Fantassin Agressif, Archer Prudent).

### 6.8 De la carte au combat et retour (étape C9)
- Sur la carte, chaque pion est un **groupe** (`PartyRoster`) : la caravane du joueur (au départ Fantassin + Archer) et chaque bande ennemie (ex. 2 Fantassins, ou Fantassin + Archer), avec les PV de chacun, le profil d'IA des ennemis et le **butin** de la bande (valeur fixe en pièces, sans lien avec l'économie pour l'instant).
- Quand la caravane touche une bande, après 1 s, la **carte est mise en veille** (tout reste figé : position, heure, patrouilles, caméra) et l'**arène** `Combat_Arena` est chargée par-dessus. Une seule arène pour le prototype (champ « arène » prévu pour en choisir d'autres).
- Les unités de combat sont créées à partir de l'`EncounterData` (types, PV actuels, profils), avec les mêmes prefabs que partout : le joueur d'un côté, l'ennemi de l'autre, en ligne. L'interface et les règles sont celles du combat normal.
- **Fin du combat** : écran « Victoire » / « Défaite » avec les pertes des deux camps et, en cas de victoire, le butin. « Continuer » ramène à la carte.
- **Victoire** : la caravane retrouve sa position, les survivants gardent leurs PV, les morts quittent le groupe, la bande vaincue disparaît, le butin est ajouté au total (affiché en bas de l'écran de la carte avec le groupe).
- **Défaite** (décision du 2026-10-23, provisoire) : écran **« Fin de partie »** avec un bouton « Recommencer » qui recharge la carte.
- Il n'y a pas encore de soin : les PV perdus le restent d'un combat à l'autre.

## 7. Méthode de travail (rappel)

- Une étape à la fois, plan validé avant de coder, vérifications à trois niveaux, rapport honnête. Détails dans `CLAUDE.md`.
- Commits de sécurité avant les grosses étapes ; push seulement sur demande explicite.

## 8. Points non tranchés (à décider quand le sujet arrive)

- Système d'initiative : qui agit quand (à définir ; remplace l'ancien découpage en manches). Le rythme actuel vient seulement des durées et des récupérations des actions.
- Sélection de cible et gestion d'aggro (qui une unité décide-t-elle d'affronter quand plusieurs l'attaquent).
- Course / sprint : coût, vitesse, durée.
- Ouïe (détection par le bruit) pour l'IA.
- Valeurs d'équilibrage définitives des deux unités (les chiffres actuels sont des valeurs de test).
- Contenu de la boîte de dialogue avant combat (rencontre sur la carte).
- Arène intérieure : plan et murs.
