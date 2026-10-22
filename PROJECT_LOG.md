# Journal du projet

Suivi des actions entreprises, mis à jour au fur et à mesure.
Projet Unity 6 (URP, 3D) : marchand-10-2026-mistral.

## 2026-10-22

### Mise en place
- Vérification de la connexion Unity MCP : scène active `SampleScene`, console sans erreur.
- Ajout de `CLAUDE.md` à la racine : directives de travail (plan d'abord, vérification en 3 niveaux, règles git, interdits, qualité C#).

### Structure du projet
- Création des dossiers sous `Assets/` (via l'éditeur Unity) :
  - `Scripts/` : `Core`, `Player`, `Enemies`, `UI`, `Data`
  - `ScriptableObjects`, `Prefabs`, `Materials`, `Models`, `Textures`, `Audio`, `Animations`, `Editor`
- Ajout d'un `.gitignore` Unity (`Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, `*.csproj`, `*.sln`, `.vscode/`).
- Vérifié : console sans erreur, dossiers et `.meta` présents.

### Étape 0 — Mise en place (déplacement des unités)
- Contexte : prototype « Marchand », système de déplacement unique (NavMesh) pour la carte de campagne et le combat tactique.
- Versions vérifiées : Unity 6000.6.6f1, URP 17.6.0, AI Navigation 2.0.16 (déjà installé), Input System 1.20.0 (nouveau système uniquement).
- Nouveaux dossiers : `Scripts/Movement`, `Scripts/Controllers`, `Scripts/Camera`, `Scenes/Tests` (`Player/` et `Enemies/` conservés).
- Scène vide créée : `Assets/Scenes/Tests/Test_Movement.unity` (0 objet, ni caméra ni lumière). `SampleScene` rechargée comme scène active.
- `.gitignore` : ajout de `*.slnx`.
- Vérifié : console sans erreur, scène créée et chargeable. Package présent.

### Étape 1 — Déplacement au clic (test jetable)
- Scène `Test_Movement` : `Ground` (plan 30×30 avec `NavMeshSurface`, bake sauvegardé dans `Assets/Scenes/Tests/Test_Movement/NavMesh-Ground.asset`), 4 cubes `Obstacle_1…4`, capsule `Unit` (`NavMeshAgent` + `ClickToMoveTest`), `Main Camera` fixe (position 0, 24, -16.8 ; rotation X 55°), `Directional Light`.
- Script jetable : `Assets/Scripts/Movement/ClickToMoveTest.cs` (nouveau Input System, `Mouse.current` + raycast + `SetDestination`). À remplacer à l'étape 2.
- Matériaux URP : `Mat_Ground`, `Mat_Obstacle`, `Mat_Unit` dans `Assets/Materials/`.
- Vérifié : console sans erreur ; chemin NavMesh à 5 coins (contournement) ; en Play Mode, l'unité rejoint (0, 10) derrière le mur en contournant les obstacles ; captures de scène OK.
- Non vérifié par moi : le clic souris réel (le Play Mode a été piloté par code).

### Étape 2 — Architecture du déplacement
- `Scripts/Movement/UnitMover.cs` : seul script qui touche le `NavMeshAgent`. API : `MoveTo`, `Stop`, `IsMoving`, `TryGetPath(destination, out length, out endPoint)` (calcul sans déplacer), événements `MoveStarted`, `Arrived`, `PathFailed(Vector3)`.
- Règles décidées : destination inatteignable → l'unité va au point atteignable le plus proche (rayon de recherche `snapRadius` = 5 m) ; `PathFailed` seulement si aucun chemin n'existe (mouvement en cours conservé) ; un nouvel ordre remplace immédiatement le précédent (pas d'`Arrived` pour l'ordre remplacé).
- `Scripts/Controllers/PlayerUnitController.cs` : clic gauche → raycast → `UnitMover.MoveTo`.
- `Scripts/Controllers/DebugMoveController.cs` : Espace → waypoint suivant via `UnitMover` ; journalise les événements du mover.
- Scène `Test_Movement` : `Unit` porte `UnitMover`, `PlayerUnitController` (activé) et `DebugMoveController` (désactivé) ; 4 waypoints `DebugWaypoints/DebugWaypoint_1…4`.
- Supprimé (avec accord) : `ClickToMoveTest.cs` et son composant sur `Unit`.
- Vérifié : compilation sans erreur ; en Play Mode, `TryGetPath` ne déplace pas l'unité ; point hors NavMesh ramené au bord (14.3, 0, 0) ; nouvel ordre en plein trajet remplace l'ancien (2 `MoveStarted`, 1 seul `Arrived`) ; `PathFailed` émis pour un point lointain sans interrompre le mouvement ; l'unité rejoint le waypoint 2 ; seul `UnitMover` référence `NavMeshAgent`.
- Non vérifié par moi : clic souris réel et touche Espace.

### Étape 3 — Visualisation du chemin
- `UnitMover` étendu (lecture seule) : `TryGetPathCorners(destination, buffer, out count, out reachesDestination)` et `GetRemainingPath(buffer)` (sans allocation, basé sur `steeringTarget` et le chemin de l'ordre en cours). `TryGetPath` inchangé en comportement.
- `Scripts/UI/PathPreview.cs` : aperçu au survol (vert = point atteint, orange = l'unité s'arrêterait au point le plus proche, rien si aucun chemin ou souris hors sol), chemin restant (cyan) pendant le déplacement, marqueur jaune à la destination (visible dès l'ordre, caché à l'arrivée ou après `Stop()`). Recalcul limité à 0,1 s (`refreshInterval`) et seulement si le point survolé a bougé de plus de 5 cm quand l'unité est immobile.
- Sélection : pas de système de sélection encore ; `PathPreview.SetSelected(bool)` + case `selectedAtStart` (cochée) en attendant. Seul l'aperçu au survol dépend de la sélection.
- Assets : `Mat_PathValid`, `Mat_PathPartial`, `Mat_PathRemaining`, `Mat_Marker` (URP Unlit) ; prefab `Assets/Prefabs/DestinationMarker.prefab` (cylindre aplati, sans collider) ; instance `DestinationMarker_Unit` dans la scène (désactivée).
- Scène : `Unit` porte `PathPreview` et 2 enfants `PathPreviewLine` / `RemainingPathLine` (LineRenderer en espace monde).
- Vérifié (Play Mode, souris simulée via l'Input System) : aperçu vert à 5 points derrière le mur ; orange au survol d'un cube (fin à 3.5, 0, -4) ; rien hors du sol ; chemin restant de 5 puis 3 points ; marqueur actif pendant le trajet, caché à l'arrivée et après `Stop()` ; console sans erreur ; captures de scène OK.
- Non vérifié par moi : rendu dans le Game view avec la vraie souris.

### Étape 4 — Types de terrain : coûts et vitesse
- Accord obtenu : modification de `ProjectSettings/NavMeshAreas.asset` (via l'API Unity, noms seulement) et suppression des 4 cubes dans la copie de scène uniquement.
- Zones NavMesh déclarées : `Route` (3), `Foret` (4), `Marais` (5), `Montagne` (6). La plaine est la zone intégrée `Walkable` (0).
- `Scripts/Data/TerrainSettings.cs` + asset `Assets/ScriptableObjects/TerrainSettings.asset` : coût de chemin et multiplicateur de vitesse par zone, modifiables dans l'Inspector. Source unique des valeurs (les coûts du projet restent à 1 par défaut).
  - Route : coût 1, vitesse ×1.25 | Plaine : 2, ×1.0 | Forêt : 3, ×0.75 | Marais : 8, ×0.35 | Montagne : 10, ×0.5
- `UnitMover` : ajout de `SetAreaCost(int|string, float)`, `GetAreaCost(int)`, `SetSpeedMultiplier(float)` et `GroundPosition`. Un coût modifié s'applique à l'ordre suivant (le trajet en cours n'est pas recalculé). Reste le seul script qui touche le `NavMeshAgent`.
- `Scripts/Movement/UnitTerrainEffects.cs` : applique les coûts du `TerrainSettings` au démarrage et ajuste la vitesse selon la zone sous l'unité (contrôle toutes les 0,1 s pendant un déplacement, sans allocation). À ajouter sur chaque unité, sinon ses coûts restent ceux du projet (1).
- Nouvelle scène `Assets/Scenes/Tests/Test_Terrain.unity` (copie de `Test_Movement`, qui reste intacte) avec son propre NavMesh `Test_Terrain/NavMesh-Ground.asset` : bande de marais sur toute la largeur coupée par une route verticale (x = -8), forêt au sud-est, montagne au nord-ouest. Zones = boîtes plates de 10 cm avec `NavMeshModifier`. Matériaux `Mat_Terrain_*`.
- Vérifié (Play Mode) : coûts appliqués (2/1/3/8/10) ; de (0, -12) à (0, 12) le chemin passe par la route (0 échantillon de marais, 28,8 m contre 24 m en ligne droite) ; avec `SetAreaCost("Marais", 2)` sur l'unité le chemin devient direct (24 m, 12 échantillons de marais), puis retour au détour quand le coût revient à 8 ; vitesse mesurée 3,5 m/s en plaine, 1,23 m/s dans le marais, 4,37 m/s sur la route ; console sans erreur ; captures OK.
- Non vérifié par moi : le rendu dans le Game view et la sensation de ralentissement avec la vraie souris.

### Étape 5 — Caméra
- `Scripts/Camera/StrategyCamera.cs` (sur la `Main Camera`) : vue de dessus inclinée autour d'un point de visée au sol. Déplacement ZQSD/WASD + flèches (touches physiques : fonctionne en AZERTY et QWERTY), défilement au bord de l'écran (désactivable), zoom molette (8 à 60 m, multiplicatif, lissé), rotation optionnelle (`allowRotation`, touches Q/E physiques, soit A/E en AZERTY), limites (`Bounds` ±48 m, gizmo cyan), centrage sur `focusTarget` (touche F, `SetFocusTarget(Transform)` pour la sélection future). Temps non mis à l'échelle (`unscaledDeltaTime`). Tous les réglages sont dans l'Inspector. Aucune dépendance à `UnitMover` ni aux contrôleurs.
- Défaut corrigé pendant les tests : avant le premier mouvement de souris, la position lue est (0, 0) (un coin) et le défilement au bord faisait dériver la caméra au démarrage. Il n'est actif qu'après un vrai mouvement de souris.
- Scène `Test_Terrain` agrandie : sol 100×100 m, route verticale (x = -8) sur tout l'axe, marais sur toute la largeur, route transversale (z = -20), 6 nouvelles zones (forêts, montagnes, marais) ; NavMesh re-bake dans le même asset ; `focusTarget` = `Unit`. `Test_Movement` non modifiée.
- Vérifié (Play Mode, entrées simulées) : départ propre (visée 0,0,0, distance 29,3, angle 55°) ; déplacement clavier ~20 m/s puis blocage aux limites (z = 48) ; zoom bloqué à 8 et 60 ; centrage sur l'unité ; rotation active (+127° en 1,5 s) puis désactivée (yaw inchangé) ; défilement au bord (6,6 m en 1,3 s, fenêtre au focus) ; clic de déplacement correct (écart 0,00 m) à zoom 60, à zoom 8, et après rotation de 135° ; console sans erreur.
- Observation (hors périmètre) : un clic sur le collider de l'unité elle-même (ou un point caché derrière elle) donne une destination sur la capsule ; à traiter avec le futur système de sélection (masque de couches pour le raycast).
- Non vérifié par moi : ressenti réel (vitesses, lissage) et clavier AZERTY.

### Étape 5 bis — Contrôles souris de la caméra
- `StrategyCamera` : clic droit maintenu = déplacement de la vue en « tirant le sol » (le point du sol saisi reste sous le curseur, à tout zoom et tout angle). Réglages : `mouseDragEnabled`, `dragCursor` (texture optionnelle), `dragCursorHotspot`. Propriété `IsDragging`. `[RequireComponent(typeof(Camera))]`.
- Pendant le glissement : limites respectées, centrage (F) annulé, défilement au bord suspendu, déplacement direct sans lissage.
- `Scripts/UI/HandCursorTexture.cs` : dessine par code un curseur « main fermée » 32×32 (aucun asset externe) ; utilisé tant que `dragCursor` est vide. Le curseur système est rétabli au relâchement, à la désactivation du composant et à la perte de focus.
- Vérifié (Play Mode, souris simulée) : le point saisi reste sous le curseur (écart 0,00 m) à zoom 29 et zoom 60 ; limites ±48 respectées ; centrage annulé par le glissement ; aucun défilement au bord pendant le glissement (0,00 m) ; le clic gauche déplace toujours l'unité (écart 0,00 m) ; console sans erreur ; dessin de la main contrôlé sur une image exportée.
- Non vérifié par moi : l'apparition réelle de l'icône (je ne vois pas le curseur système).
- Constat : recompiler pendant un Play Mode réinitialise le champ non sérialisé `path` de `UnitMover` et fait lever une `NullReferenceException` à l'aperçu de chemin (uniquement en rechargement à chaud, jamais en lancement normal). À durcir (initialisation paresseuse) si on veut tolérer le rechargement à chaud.

### Étape 5 ter — Rotation à la souris (bouton du milieu)
- `StrategyCamera` : bouton du milieu maintenu + mouvement horizontal = rotation de la caméra autour du point de visée (angle d'inclinaison inchangé). Glisser vers la droite fait suivre la scène vers la droite. Réglages : `mouseRotateSensitivity` (0,25 °/pixel, valeur négative pour inverser), `rotateCursor` (texture optionnelle), `rotateCursorHotspot`. Propriété `IsRotating`. Suit la case `allowRotation` (désactive aussi la rotation clavier). Le défilement au bord est suspendu pendant la rotation.
- Icône : `Scripts/UI/CursorTextures.cs` (ex-`HandCursorTexture.cs`, renommé et généralisé) génère par code la main (`CreateHand`) et l'icône de rotation (`CreateRotation` : deux flèches courbes horaires). Les curseurs sont restaurés au relâchement, à la désactivation du composant et à la perte de focus ; la rotation prime sur le glissement si les deux sont actifs.
- Vérifié (Play Mode, souris simulée) : +200 px à droite → yaw 310° (attendu 310°) ; −100 px → 335° (attendu 335°) ; point de visée inchangé pendant la rotation ; curseur au bord de l'écran sans défilement ; rotation désactivée (`allowRotation` décoché) → aucun effet ; clic gauche toujours correct après rotation ; console sans erreur ; dessin de l'icône contrôlé sur une image exportée.
- Non vérifié par moi : l'apparition réelle de l'icône, le sens et la sensibilité ressentis avec une vraie souris.
- Incident : le renommage du script via l'outil d'assets a d'abord déplacé le fichier à la racine de `Assets/` sans extension (paramètre erroné), puis Unity a gardé l'ancien chemin dans sa liste de sources (erreur CS2001). Pour débloquer la compilation sans suppression, `Assets/Scripts/UI/HandCursorTexture.cs` a été recréé comme fichier de commentaire. **À supprimer avec l'accord de l'utilisateur** (il ne sert plus à rien).

### Étape 6 — Plusieurs unités, sélection, évitement
- Prefab `Assets/Prefabs/Unit.prefab` : capsule, `NavMeshAgent` (rayon 0,5, distance d'arrêt 0,3, évitement haute qualité), `UnitMover`, `UnitTerrainEffects`, `PathPreview`, `SelectableUnit`, « nez » (orientation), anneau de sélection (caché par défaut), marqueur de destination en enfant (détaché au démarrage par `PathPreview`).
- `Scripts/Controllers/SelectableUnit.cs` (nommé ainsi pour ne pas heurter `UnityEngine.UI.Selectable`) : état de sélection, anneau, accès à `UnitMover`. `UnitMover` ignore la sélection.
- `Scripts/Controllers/SelectionManager.cs` : seule responsabilité = la sélection. Clic sur une unité = sélection, Maj + clic = ajout/retrait, glisser (> 8 px) = rectangle (Image uGUI sur Canvas Overlay), Échap = tout désélectionner, clic sur le sol = événement `GroundClicked`. Recentre la caméra sur la première unité sélectionnée (`StrategyCamera.SetFocusTarget`).
- `Scripts/Controllers/PlayerCommandController.cs` + `Scripts/Movement/FormationLayout.cs` : un clic sol envoie la sélection ; une unité va au point exact, plusieurs se répartissent en grille compacte (1,6 m) orientée dans le sens du déplacement, chaque unité prenant l'emplacement libre le plus proche.
- `PathPreview` adapté au prefab (caméra principale par défaut, marqueur détaché et détruit avec l'unité, aperçu au survol seulement pour les unités sélectionnées).
- `UnitMover` : priorité d'évitement basse (99) à l'arrêt, priorité réglée sur l'agent (40 à 60 selon l'unité) en mouvement. Corrige le blocage observé en fin de trajet : une unité coincée entre deux unités déjà arrivées qui ne s'écartaient pas.
- Scène `Test_Terrain` : 5 unités (`Unit_1…5`), enclos de 12×12 m (murs de 2 m, « Not Walkable » pour que leur dessus ne soit pas navigable) dont le mur sud est coupé en deux par un **passage de 2,2 m** ; NavMesh re-bake ; `SelectionCanvas`/`SelectionBox` ; objet `Selection` (`SelectionManager` + `PlayerCommandController`).
- Vérifié (Play Mode, souris et clavier simulés) : clic = 1 sélection ; Maj + clic ajoute puis retire ; Échap vide ; rectangle = 5 unités ; un ordre à une seule unité laisse les autres immobiles ; destinations de formation toutes distinctes (écart minimal 1,6 m) ; rectangle affiché (778×523 px) et masqué au relâchement ; **traversée du passage par les 5 unités sélectionnées ensemble : aller en 4,1 s et retour en 4,8 s, toutes arrivées, immobilité maximale 0,00 s** ; console sans erreur ; capture de scène avec anneaux verts.
- Méthode de test : la sélection n'a pu être testée qu'avec Unity au premier plan. Le projet a `runInBackground: 0` (Play Mode en pause quand Unity perd le focus) ; je l'ai activé **en mémoire seulement** pendant les tests (`ProjectSettings` inchangé, empreinte vérifiée).
- Non vérifié par moi : l'usage réel avec la souris (rectangle visible à l'écran, Maj + clic, ressenti de l'évitement et de la formation).
- `PlayerUnitController.cs` n'est plus utilisé (remplacé par `SelectionManager` + `PlayerCommandController`) : à supprimer avec accord.

### Étape 7 — IA ennemie de base
- `Scripts/Core/UnitTeam.cs` : enum `Team` (Player, Enemy) + composant `UnitTeam` avec registre statique des unités actives. Ajouté au prefab `Unit` (Player par défaut) : les 5 unités du joueur sont automatiquement `Player`.
- `Scripts/Controllers/AIUnitController.cs` : pilote l'unité **uniquement via `UnitMover`** (aucune référence au `NavMeshAgent`). Trois états : `Patrol` (points de l'inspecteur en boucle, pause de 1,5 s, un point inaccessible est sauté), `Chase` (cible = unité adverse la plus proche dans le rayon de détection 9 m, destination rafraîchie toutes les 0,3 s, arrêt au contact à 1,5 m), `ReturnToPatrol` (si la cible dépasse 15 m ou disparaît : retour au point de patrouille le plus proche, puis reprise de la patrouille). Gizmos : rayon de détection (jaune), distance d'abandon (orange), tracé de la patrouille (cyan). Propriété `State` en lecture seule.
- `Assets/Prefabs/UnitEnemy.prefab` : **variant** du prefab `Unit` — équipe Enemy, matériau rouge `Mat_UnitEnemy`, `AIUnitController`, vitesse 3,0 (joueur : 3,5), `SelectableUnit` retiré (ni sélectionnable, ni touché par le rectangle), `PathPreview` désactivé.
- Scène `Test_Terrain` : `Enemy_1` patrouille de (14, -24) à (30, -24) à l'est de l'enclos, `Enemy_2` de (10, 12) à (28, 12) ; points sous `PatrolRoutes`.
- `UnitMover` : **non modifié** (dernière modification avant l'étape, vérifiée).
- Vérifié (Play Mode, entrées simulées) : patrouille entre les deux extrémités ; `Patrol → Chase` quand `Unit_1` passe à 8,9 m ; contact à 1,49 m (l'ennemi s'arrête) ; `Unit_1` accélérée pour le test (9 m/s) et envoyée à 48 m : `Chase → ReturnToPatrol` à exactement 15,0 m, retour au point de patrouille le plus proche, `Patrol` reprise (l'ennemi repart vers son point suivant) ; un clic sur un ennemi ne sélectionne rien ; un rectangle plein écran sélectionne les unités du joueur (4) et aucun ennemi ; console sans erreur ; capture avec ennemis rouges.
- Non vérifié par moi : l'affichage des gizmos (la capture de test ne les rend pas ; à regarder dans la Scene view) et le ressenti à la souris.
- Choix de conception : un clic sur le collider d'un ennemi est traité comme un clic sur le sol (les unités sélectionnées s'y rendent) ; le combat et le ciblage viendront plus tard.

### Étape 7 bis — Détection visuelle uniquement (ligne de vue)
- Problème : les ennemis détectaient à travers les murs (test de distance seul).
- `AIUnitController` : une unité n'est détectée que si elle est dans le rayon **et visible**. `CanSee` trace un rayon des yeux (`eyeHeight` = 0,5 m au-dessus du pivot) jusqu'à la cible ; tout collider solide qui n'est pas une unité bloque la vue (`sightBlockers`, masque de couches, tout par défaut ; triggers ignorés). Pas de couche dédiée : `ProjectSettings` non touché. L'ouïe viendra plus tard comme seconde source de détection.
- Poursuite : tant que la cible est visible, comportement inchangé (rafraîchissement 0,3 s, contact à 1,5 m, abandon à 15 m). Si la vue est perdue, l'ennemi va à la **dernière position vue** (jamais à la position réelle) et abandonne à son arrivée s'il ne revoit rien, ou après `loseSightTime` (3 s). Contact et abandon à distance ne s'appliquent qu'à une cible visible. Vérification de vue toutes les 0,2 s pendant la poursuite (aucune allocation).
- Gizmos en jeu : ligne verte vers la cible vue, rouge si bloquée (+ sphère magenta sur la dernière position vue).
- Vérifié (Play Mode, unités déplacées par script) : ennemi collé au mur est, 5 unités à 7–11 m derrière le mur : toutes « bloquées par Wall_East », aucune poursuite (état `Patrol`) ; ennemi face au passage de 2,2 m : vue dégagée, `Chase` ; vue perdue en pleine poursuite : l'ennemi contourne vers la dernière position vue (par le sud, jamais à travers le mur) puis abandonne après 3 s ; test de non-régression : patrouille, poursuite en terrain dégagé (8,8 m), contact (1,49 m), abandon à 15,0 m, retour et reprise de la patrouille ; console sans erreur ; `UnitMover` et `ProjectSettings` inchangés.
- Non vérifié par moi : les gizmos de vue à l'écran et le ressenti à la souris. Pas de cône de vision (seulement la ligne de vue).

### Étape 7 ter — Recherche après perte de la cible + vitesse du joueur
- Vitesse des unités du joueur : 3,5 → **4,5** (prefab `Unit`) ; ennemis inchangés à 3,0 (variant `UnitEnemy`). Les 5 unités de la scène suivent le prefab.
- `AIUnitController` passe à 5 états : `Patrol`, `Chase`, `Investigate` (marche vers la dernière position vue), `Scan` (reste sur place et regarde autour), `ReturnToPatrol`.
  - Vue perdue → `Investigate` → à l'arrivée `Scan` : l'unité reste immobile `searchDuration` = 4 s et balaie la tête de `scanAngle` = 90° d'un côté, puis de l'autre, puis revient de face (rotation directe du `Transform` à l'arrêt, sans toucher au `NavMeshAgent` ni à `UnitMover`). La détection reste active pendant `Investigate` et `Scan` : revoir une unité du joueur relance la poursuite.
  - Fin du scan → l'ennemi **reprend ce qu'il faisait** : retour au point de patrouille qu'il visait avant la poursuite (et non plus au plus proche) ; sans patrouille, retour à sa position de départ.
  - Cible trop loin (> 15 m) ou disparue → `Scan` sur place, sans se déplacer vers elle.
  - Garde-fou `investigateTimeout` = 6 s (remplace `loseSightTime`) : si la dernière position vue n'est pas atteinte, le scan commence là où il se trouve.
  - Gizmos : ligne magenta vers la dernière position vue (`Investigate`), ligne jaune du regard (`Scan`).
- Vérifié (Play Mode, unités déplacées par script) : vue perdue → `Chase → Investigate → Scan → ReturnToPatrol → Patrol` ; scan de 4,0 s, balayage -90°…+90°, retour à 0° ; retour vers le même point de patrouille qu'avant la poursuite ; abandon à plus de 15 m → scan en place (0,0 m parcouru) ; joueur visible pendant le scan → `Chase` ; console sans erreur ; `UnitMover` et `ProjectSettings` inchangés.
- Non vérifié par moi : l'aspect visuel du scan (rotation du nez) et les gizmos à l'écran.
- Incident corrigé : une sauvegarde faite juste après un Play avait enregistré `Enemy_1` à x = 15,31 ; remis en (14, 1, -24).

## À faire / en attente
- Regarder les gizmos de détection dans la Scene view (sélectionner `Enemy_1`).
- Supprimer `Assets/Scripts/UI/HandCursorTexture.cs` (fichier de commentaire provisoire) et `Assets/Scripts/Controllers/PlayerUnitController.cs` (inutilisé) après accord.
- Tester dans `Test_Terrain` (Game view) : se promener, zoomer, tourner (A/E ou bouton du milieu), centrer (F), cliquer pour déplacer à plusieurs zooms, clic droit maintenu pour faire glisser la vue (icône main), bouton du milieu maintenu pour tourner (icône rotation).
- Tester dans le Game view : clic souris (Player actif) puis touche Espace (cocher `DebugMoveController`, décocher `PlayerUnitController`).
- Premier commit de sécurité (non fait, aucun commit dans le dépôt).
- Définir le design du jeu (aucun script ni asset de jeu pour l'instant).
