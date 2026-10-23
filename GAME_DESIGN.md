# Marchand — Décisions de game design

Ce fichier regroupe les décisions de conception **prises et validées**, pour ne pas avoir à y revenir.
Il ne décrit pas le code (voir `PROJECT_LOG.md` pour l'historique des étapes, `CLAUDE.md` pour les règles de travail).
Quand une décision change, on modifie la ligne concernée et on note la date. Les points non tranchés sont à la fin.

Dernière mise à jour : 2026-10-23.

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

### 6.2 Manches et temps d'action
- Le temps est découpé en **manches de 6 secondes** de jeu.
- Chaque unité dispose de **6 secondes de temps d'action par manche**, rechargées au début de la manche suivante. **Toute action a un coût en secondes** (marcher coûte le temps de marche, attendre coûte sa durée, les futures actions auront leur durée).
- Les ennemis sont soumis au **même budget** que le joueur.
- Les vitesses sont réalistes : marche **1,4 m/s** pour les joueurs, **1,2 m/s** pour les ennemis (environ 8,4 m et 7,2 m par manche). La course et le sprint viendront plus tard.
- Pas de « pause » en milieu de manche : une unité marche en continu d'une manche à l'autre. Elle ne s'arrête que si son temps d'action est épuisé ; elle attend alors la manche suivante et reprend.

### 6.3 Ordres
- Une unité exécute **une seule action à la fois** ; une nouvelle demande **remplace** l'action en cours (pas de file d'attente).
- Un ordre de déplacement plus long que ce qui reste dans la manche **continue sur les manches suivantes** jusqu'au point cliqué (programmable aussi en pause).
- Aperçu du trajet : vert = ce que l'unité parcourt avant la fin de la manche en cours, rouge = la suite, ligne fine cyan = trajet complet vers la destination finale.
- À l'arrêt, une unité fait face au point visé (et un fantassin immobile est un obstacle : les autres le contournent).

### 6.4 Unités
- Deux unités de départ : **Fantassin** (mêlée) et **Archer** (distance). Chaque type est décrit par une `UnitDefinition` : PV max, armure, bonus d'attaque, défense, dégâts de l'arme, allonge, zone morte, portée maximale (0 = mêlée), vitesse, temps d'action par manche.
- Une unité a **une seule arme** : mêlée si portée maximale = 0 (c'est l'allonge qui compte), à distance sinon (portée maximale, avec une zone morte minimale éventuelle).
- À 0 PV, l'unité arrête tout, est retirée du jeu et libère sa place sur le NavMesh.

### 6.5 Règles de résolution (simplifiées, esprit d100)
- Jet d100 + bonus d'attaque, comparé à la défense de la cible ; si touché, dégâts de l'arme moins l'armure (minimum 0).
- Toutes les valeurs sont dans des données. Les règles sont **isolées** (`CombatRules`) pour pouvoir remplacer le système plus tard sans toucher au reste.

### 6.5 bis Attaque au corps à corps (étape C3)
- Une attaque **dure `attackTime` secondes** (2 s par défaut) et **dépense du temps d'action** de la manche : c'est ce qui empêche les attaques en rafale (3 coups par manche au maximum). Il faut le temps d'une attaque complète pour la commencer, sinon on attend la manche suivante.
- Une demande « Attaquer » **dure jusqu'à la mort de la cible** (ou jusqu'à un nouvel ordre) : l'unité se rapproche seule, se tourne, frappe, recommence.
- Le coup touche si **jet + bonus d'attaque ≥ défense** ; dégâts = dégâts de l'arme − armure, minimum 0. Valeurs de test : Fantassin attaque 25 / défense 50 (76 % de toucher contre un Fantassin), Archer attaque 20 / défense 35.
- L'**allonge** se mesure de **centre à centre**. Une unité qui s'approche s'arrête à 85 % de son allonge.
- Un clic gauche sur un ennemi = ordre d'attaque (dans la scène de combat) ; sur la carte de campagne, c'est un simple déplacement. L'IA attaque par **le même chemin** que le joueur.
- Retour visuel : cercle d'allonge au sol (et cercle intérieur de zone morte), surbrillance de l'ennemi survolé avec la **chance de toucher**, texte flottant (dégâts ou « Raté »), journal de combat à l'écran.
- Les attaques à distance (Archer) arrivent à l'étape C4.

### 6.6 Intelligence artificielle
- Patrouille entre des points ; poursuite d'une cible **vue** (détection **visuelle uniquement** : un mur bloque la vue ; l'ouïe sera une autre source de détection plus tard).
- Cible perdue : l'ennemi va au dernier point vu, reste un moment à scruter, puis reprend ce qu'il faisait avant la poursuite.

## 7. Méthode de travail (rappel)

- Une étape à la fois, plan validé avant de coder, vérifications à trois niveaux, rapport honnête. Détails dans `CLAUDE.md`.
- Commits de sécurité avant les grosses étapes ; push seulement sur demande explicite.

## 8. Points non tranchés (à décider quand le sujet arrive)

- Course / sprint : coût, vitesse, durée.
- Ouïe (détection par le bruit) pour l'IA.
- Valeurs d'équilibrage définitives des deux unités (les chiffres actuels sont des valeurs de test).
- Contenu de la boîte de dialogue avant combat (rencontre sur la carte).
- Arène intérieure : plan et murs.
