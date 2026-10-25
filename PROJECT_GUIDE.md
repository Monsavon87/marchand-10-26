# Marchand — Guide de reprise du projet

Ce guide s'adresse à une équipe qui reprend le projet sans l'avoir suivi. Il dit **comment le jeu est construit et par où commencer**, pour ne pas avoir à lire le code ligne à ligne. Il ne répète pas les deux autres fichiers de suivi, il y renvoie :

| Fichier             | Contenu                                                                    | Quand le lire                                        |
| ------------------- | -------------------------------------------------------------------------- | ---------------------------------------------------- |
| `GAME_DESIGN.md`    | Les décisions de game design (le « quoi » et le « pourquoi »)              | Avant de toucher au gameplay                         |
| `PROJECT_LOG.md`    | L'historique : ce qui a été fait, vérifié, et ses limites, étape par étape | Pour comprendre pourquoi un morceau est comme il est |
| `CLAUDE.md`         | Les règles de travail et de code de l'équipe                               | Avant d'écrire la première ligne                     |
| `ECONOMY_REPORT.md` | Le dernier rapport d'équilibrage de l'économie (généré par le simulateur)  | Avant de régler des prix ou des villes               |

Les noms de classes, de fichiers et de dossiers cités ici existent tels quels dans le dépôt.

---

## 1. Le jeu en bref

« Marchand » est un prototype de RPG : le joueur est un marchand qui **voyage** de ville en ville avec sa caravane sur une carte de campagne, **commerce** (achète là où une marchandise abonde, revend là où elle manque) et **se bat** quand une bande de brigands le touche (combat tactique en temps réel avec pause, dans une arène).

- Unity 6 (éditeur **6000.6.6f1**), pipeline **URP**, Input System, uGUI (texte « legacy », sans police importée).
- Visuels : uniquement des primitives et du texte. Le jeu (messages, interface) est en **français** ; le code et les commentaires sont en **anglais**.
- Une seule monnaie : l'**U.V.** (unité de valeur), toujours un entier.

État au moment de l'écriture :

- **Démo jouable** (version 0.1.0 construite, voir `Builds/`) : carte de campagne `Demo_Campaign` avec 5 villes et 6 bandes ennemies, combats, recrutement, aide, écran de fin.
- **Économie** (étapes E1 à E9 et F1 faites) : marchandises, cargaison, portefeuille, marchés des villes, prix, échanges, évolution des marchés, interface du marché, simulateur d'équilibrage, butin en marchandises.
- **En attente de décision** : réglages d'équilibrage d'après `ECONOMY_REPORT.md` (le butin en marchandises est maintenant en jeu, voir F1).

---

## 2. Démarrer

### Ouvrir et jouer

1. Ouvrir le dossier avec l'éditeur **6000.6.6f1** (ne pas changer de version sans décision).
2. Ouvrir `Assets/Scenes/MainMenu.unity` et lancer, ou ouvrir directement `Assets/Scenes/Demo_Campaign.unity`.
3. Sur la carte : clic gauche pour aller quelque part, clic sur une ville pour y aller, clic sur une bande pour la poursuivre. La liste complète des touches est dans l'asset `GameHelp` (aussi affichée en jeu par le bouton Aide).

### Les scènes (`Assets/Scenes/`)

| Scène                                                                                                      | Rôle                                                                                             |
| ---------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| `MainMenu`                                                                                                 | Écran-titre (jouer, aide, quitter, numéro de version)                                            |
| `Demo_Campaign`                                                                                            | **La scène principale** : carte, 5 villes, 6 bandes, économie, interface de ville                |
| `Combat_Arena`, `Arena_Foret`, `Arena_Fort`                                                                | Arènes de combat chargées par-dessus la carte (tirées au hasard)                                 |
| `Tests/Test_Campaign`, `Test_Movement`, `Test_Terrain`, `Test_Combat`, `Test_Combat_2v2`, `Test_Combat_AI` | Bancs d'essai d'un système à la fois. À ne rejouer que si l'on touche au système qu'ils couvrent |

Les scènes de la liste de build sont dans `ProjectSettings/EditorBuildSettings.asset` (`MainMenu`, `Demo_Campaign`, arènes, `Test_Campaign`). La liste garde une ligne désactivée vers `SampleScene`, supprimée en G0 (ligne laissée pour ne pas toucher à `ProjectSettings`) ; Unity l'affiche « Deleted ».

### Touches de debug (jeu en éditeur)

| Touche                  | Effet                                                             | Script                |
| ----------------------- | ----------------------------------------------------------------- | --------------------- |
| T / Espace              | Laisser le temps passer / pause (c'est une vraie fonction du jeu) | `CampaignPause`       |
| I                       | Ouvrir / fermer la cargaison (Échap la ferme aussi)               | `InventoryPanel`      |
| F3                      | Tableau des stocks de toutes les villes                           | `MarketDebugView`     |
| F4 (PageHaut / PageBas) | Tableau des prix d'une ville (changer de ville)                   | `PriceDebugView`      |
| F5 (Maj : +7)           | Avancer le calendrier d'un jour                                   | `EconomyDebugInput`   |
| B ou V + un chiffre     | Acheter / vendre sans interface (Maj : le maximum)                | `TradeDebugInput`     |
| 1 à 0 / Maj + chiffre   | Ajouter / retirer une marchandise de la cargaison                 | `InventoryDebugInput` |
| K                       | Blesser les unités sélectionnées (combat)                         | `CombatDebugInput`    |

Ces scripts sont du debug : à retirer ou désactiver quand l'interface correspondante le remplace.

---

## 3. Carte du code (`Assets/Scripts/`)

Un script = une responsabilité, rangé par fonctionnalité. Chaque classe a un commentaire de tête qui dit ce qu'elle fait : c'est la première chose à lire.

### Core, Movement, Controllers, Camera — les bases

| Script                                                    | Rôle                                                                                                                   |
| --------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| `Movement/UnitMover`                                      | **Le seul script qui touche au `NavMeshAgent`.** Tout déplacement (joueur, ennemis, carte, combat) passe par lui       |
| `Movement/UnitTerrainEffects`, `Data/TerrainSettings`     | Coûts de chemin et vitesses selon le terrain (route, forêt, marais…)                                                   |
| `Movement/MoveOrderGate`                                  | Autorise ou limite un ordre de déplacement (le combat limite la distance, la carte redirige vers l'entrée d'une ville) |
| `Movement/FormationLayout`                                | Calcul pur des places en formation                                                                                     |
| `Core/UnitTeam`                                           | Le camp d'une unité (`Team`) et le registre de toutes les unités                                                       |
| `Core/UiPointerGuard`                                     | Dit si la souris est sur l'interface, pour qu'un clic sur un bouton ne devienne pas un clic dans le monde              |
| `Controllers/SelectionManager`                            | Sélection (clic, Maj, rectangle) ; ne donne aucun ordre                                                                |
| `Controllers/PlayerCommandController`                     | Transforme un clic sur le sol en ordre pour les unités sélectionnées (par `ActionController`)                          |
| `Controllers/SelectableUnit`, `CampaignSelectionKeeper`   | Anneau de sélection ; la caravane reste toujours sélectionnée sur la carte                                             |
| `Controllers/AIUnitController`                            | Cerveau de patrouille des ennemis (route ou errance, poursuite à vue, fouille ; dans l'arène, recherche de l'ennemi au côté opposé avec ouïe et filet)                                             |
| `Controllers/AICombatBrain`                               | Décisions de combat de l'IA (cible, contournement, recul, repli)                                                       |
| `Controllers/PlayerUnitController`, `DebugMoveController` | Contrôleurs de `Test_Movement` seulement (clic pour déplacer ; Espace vers le point suivant), gardés pour cette scène   |
| `Camera/StrategyCamera`                                   | Caméra de stratégie (ZQSD, bords d'écran, rotation, zoom)                                                              |

### Combat (`Combat/`)

| Script                                                                           | Rôle                                                                                                                                                 |
| -------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| `CombatManager`                                                                  | Fait tourner le combat en temps réel avec pause ; détecte la fin ; ne bouge jamais une unité                                                         |
| `CombatSpawner`                                                                  | Remplit l'arène depuis `EncounterData` (une unité par membre de chaque camp) et démarre le combat                                                    |
| `Actions/ActionController`                                                       | **Point d'entrée unique** de ce que fait une unité (joueur et IA passent par lui) : valide, exécute une action à la fois, file d'ordres              |
| `Actions/MoveAction`, `AttackAction`, `ShootAction`, `ChaseAction`, `WaitAction` | Les actions (« Déplacer », « Attaquer », « Tirer », « Poursuivre », « Attendre »)                                                                    |
| `Actions/ShotSpotFinder`, `LineOfSight`, `Projectile`, `ShotPassThrough`         | Tir à distance : place de tir, ligne de vue, flèche, décors qui ne bloquent pas                                                                      |
| `Rules/ICombatRules`, `CombatRules`, `CombatRulesProvider`, `AttackZone`         | Règles de résolution (dé + bonus contre défense, zones face / flanc / dos). Pour changer de système de règles, changer ce que le fournisseur renvoie |
| `UnitStats`, `Health`, `UnitDeath`                                               | Chiffres de combat copiés de `UnitDefinition`, points de vie, mort                                                                                   |
| `Feedback/*`                                                                     | Journal de combat, cible visée, zones, portée de l'arme, textes flottants                                                                            |

### Campagne (`Campaign/`)

| Script                         | Rôle                                                                                                                                                        |
| ------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `CampaignPause`                | **Seul propriétaire de `Time.timeScale` sur la carte** ; états `CampaignTimeState` (voir § 4.1)                                                             |
| `CampaignClock`                | Calendrier de la carte (minutes en `double`) ; événement `DayStarted` ; `AdvanceMinutes`                                                                                          |
| `CampaignTravel`               | Clic sur une ville = aller à son entrée ; événement `TownReached` (qui vide aussi la file d'ordres de la caravane)                                                                                           |
| `CampaignEncounters`           | Détecte le contact caravane / ennemi                                                                                                                        |
| `CampaignEncounterFlow`        | Rencontre → combat → retour : construit `EncounterData`, endort la carte, charge l'arène, applique le résultat (survivants, butin, bande mise de côté, défaite) |
| `PartyRoster`, `PartyCapacity` | Les unités d'un groupe (avec leurs PV ; butin d'une bande) et la taille maximale du groupe                                                                  |
| `BandLootSetup`                | Donne à chaque bande ennemie son butin, tiré une fois au démarrage de la scène depuis sa table (`LootTable`), avec une graine réglable                       |
| `NavMeshPicker` (`Movement/`)    | Tirages de points atteignables sur le NavMesh (autour d'un point, sur toute la carte, route, saut d'errance avec marge de bord et rappel vers le centre) pour l'errance et le retour des bandes |
| `BandRespawner`                | Fait revenir les bandes battues : délai tiré en jours du monde (`DayStarted`), retour à un point tiré au hasard sur la carte, avec un nouveau comportement (route générée ou errance) à plus de 50 m de la caravane et hors de la vue du joueur, même composition, nouveau butin ; réglages dans `BandRespawnSettings` |
| `DemoGoal`                     | But de la démo : 100 jours (`DemoGoalSettings`) ; à la fin, tient la carte et lève `Finished` avec la richesse finale (argent + cargaison au prix de base) |
| `PointOfInterest`              | Une ville de la carte (données et test de clic)                                                                                                             |
| `TownRest`, `TownRecruitment`  | Arrivée en ville (l'événement qui ouvre la fenêtre de ville ; elle ne soigne plus rien) et recrutement |
| `PartyRegeneration`, `HealingRules`, `TownHospital` | Régénération naturelle du groupe (8 % des PV max par jour du monde, plafonnée à 80 %), la règle de calcul pure, et l'hôpital (soin payé en U.V., 25 par PV rendu, par unité ou pour tout le groupe) ; réglages dans `HealingSettings` |
| `PartyPayroll`, `PayrollRules`, `IDailyCost`, `WageCost` | Paie des salaires du groupe à chaque début de jour (`DayStarted`) par la règle pure `PayrollRules` (partagée avec le simulateur), 7 jours offerts, dette si solde insuffisant (payée en premier ensuite) ; `IDailyCost` est le point d'extension pour d'autres coûts quotidiens (seul `WageCost` existe) ; réglages dans `PayrollSettings` |
| `CampaignHud`                  | Horloge, message, état du temps                                                                                                                             |

### Données (`Data/`) — les ScriptableObjects (voir § 5)

`UnitDefinition`, `AIProfile`, `CombatSettings`, `CombatRulesSettings`, `AttackZoneSettings`, `ActionTimingSettings`, `TerrainSettings`, `HealingSettings`, `PayrollSettings`, `OrderQueueSettings`, `BandBehaviorSettings`, `RecruitmentSettings`, `EncounterChannel` / `EncounterData`, `GameHelp`, `PartyMember`, et pour l'économie `GoodDefinition`, `GoodCatalog`, `TownDefinition`, `CargoCapacity`, `PricingSettings`, `EconomySettings`, et pour le butin `LootTable` (valeur totale et ressources d'une bande) et `LootWealthScale` (les mots Maigre à Très riche).

### Économie (`Economy/`)

| Script                                                                                  | Rôle                                                                                                                                       |
| --------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| `Wallet`                                                                                | **Seule source de vérité de l'argent** (U.V.) : `TryDeposit` / `TryWithdraw`, tout ou rien                                                 |
| `Inventory`                                                                             | La cargaison : quantités entières par marchandise, avec le **prix de revient moyen** de ce qu'on porte (un achat l'ajoute, une vente le retire au prorata, le butin compte pour 0), limite en masse **et** en volume ; `TryAdd` / `TryRemove`                               |
| `Market`                                                                                | Le marché d'une ville : stock par marchandise, jamais négatif ; la ville achète **tout**                                                   |
| `IPricingRules`, `PricingRules`                                                         | Calcul des prix (voir § 4.2) ; interface pour pouvoir changer de règles                                                                    |
| `TradeService`                                                                          | **Seul point d'entrée pour acheter et vendre**, tout ou rien ; lectures `GetTrend` (la tendance si on fait l'action), `AverageCost`, `UnitProfit` (le profit d'une unité vendue ici) et `UnitPrice`, `Preview`, `MaxQuantity`, `GetTrend` pour l'interface |
| `TradeMessages`, `MoneyFormat`                                                          | Les phrases affichées pour chaque statut, le format des montants (le service ne contient aucun texte)                                      |
| `TradePreview`, `TradeResult`, `TradeRecord`, `TradeStatus`, `TradeKind`, `PriceTrend`… | Types de résultat                                                                                                                          |
| `TradeJournal`                                                                          | Journal des échanges réussis                                                                                                               |
| `EconomyTicker`, `MarketDayRules`, `IEconomyModifier`                                   | Évolution quotidienne des marchés (production, consommation, retour vers la cible) ; point d'extension pour des événements                 |
| `MarketNetworkCheck`                                                                    | Vérifie que les profils des villes se complètent (avertissements dans la console)                                                          |
| `LootRoller`, `LootResult`, `GoodStack`, `LootMessages` | Tirage du butin d'une bande (valeur tirée puis partagée au hasard entre U.V. et marchandises, arrondi aléatoire, reproductible avec un `Random` injecté), son résultat, une pile de marchandise, les phrases de butin |
| `Simulation/*`                                                                          | Simulateur d'équilibrage sans rendu (voir § 6)                                                                                             |
| `*DebugView`, `*DebugInput`                                                             | Outils de debug (voir § 2)                                                                                                                 |

### Interface (`UI/`)

| Script                                                                                                           | Rôle                                                                                                          |
| ---------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| `UiLayering`                                                                                                     | Règle d'affichage : toute fenêtre qui s'ouvre passe au premier plan                                           |
| `Campaign/TownPanel`                                                                                             | **Fenêtre principale de la ville** (infos génériques + un bouton par fonction)                                |
| `Campaign/TownFunctionWindow`                                                                                    | Base d'une fenêtre de fonction de ville (ouvrir, fermer, « Retour à la ville »)                               |
| `Campaign/MarketWindow`, `MarketRowView`                                                                         | Fenêtre du marché à deux onglets : Acheter (ce que la ville offre, colonne Achat) et Vendre (toutes les marchandises, colonnes Vente et Profit/u ; disposition des colonnes dans `MarketColumns`), sélection, quantité 1 +1 +10 Max, aperçu avec le profit de la vente, boutons, refus expliqués)                                      |
| `Campaign/RecruitmentWindow`                                                                                     | Fenêtre de recrutement                                                                                        |
| `Campaign/HospitalWindow`                                                                                        | Fenêtre de l'hôpital : une ligne par unité (PV, prix du soin complet, bouton) et un bouton pour tout le groupe, refus avec la raison |
| `Campaign/InventoryPanel`, `TradeHud`                                                                            | Cargaison (touche I) ; U.V. et cargaison en permanence sur la carte                                           |
| `Campaign/PayrollHud`                                                                                            | Ligne « Salaires : X U.V. par jour » et « Dette » en rouge, sur la carte (événementielle) |
| `Campaign/GameOverScreen`, `DemoEndScreen`, `BandTargeting`, `TownLabel`, `CampaignMenuButtons`, `LabeledButton` | Fin de partie, fin de démo, niveau de richesse du butin de la bande survolée, nom des villes, boutons Menu / Aide                   |
| `Combat/*`                                                                                                       | Panneau de l'unité, barre d'ordres, boutons d'action, bandeau d'unités, barres de vie, écran de fin de combat |
| `HelpPanel`, `Menu/MainMenu`, `Menu/VersionLabel`, `PathPreview`, `CursorTextures`                               | Aide, écran-titre, numéro de version, ligne de trajet, curseurs                                               |

Les dossiers `Enemies/` et `Player/` sont **vides** (vestiges) : ne pas y chercher de code.

---

## 4. Comment ça marche

### 4.1 Le temps de la carte (comportement « Bannerlord »)

- Le monde (patrouilles, calendrier, marchés) est **figé** tant que le joueur n'a aucune action en cours. Il s'écoule pendant un déplacement ou une poursuite, ou quand le joueur appuie sur **T** (laisser le temps passer). **Espace** met en pause.
- `CampaignPause` pilote `Time.timeScale` et expose l'état : `Frozen`, `Moving`, `Waiting`, `Paused`, `Held`. Une fenêtre qui doit tenir la carte immobile appelle `SetHeld(propriétaire, true)` (et `false` à la fermeture).
- `CampaignClock` suit le temps du monde : `Time.deltaTime` × minutes par mètre × vitesse de base de la caravane. Il lève `DayStarted(jour)` une fois par jour franchi, dans l'ordre, même pour un saut de plusieurs jours (`AdvanceMinutes`). **Rien de ce qui est économique ne doit avoir son propre minuteur : il écoute le calendrier.**
- Le temps passé en ville est nul (le monde y est tenu figé).

### 4.2 L'économie

- **Données** : une marchandise (`GoodDefinition` : prix de base, masse, volume, cours fixe ou non), le catalogue (`GoodCatalog`), une ville (`TownDefinition` : par marchandise un stock de départ, un stock cible, une production et une consommation par jour ; un stock cible par défaut pour ce que la ville n'échange pas).
- **Prix** : prix moyen = prix de base × facteur(stock / stock cible), le facteur suivant une courbe décroissante bornée (`PricingSettings`) — rare = cher. Un écart acheteur / vendeur s'ajoute (arrondi toujours en défaveur du joueur). Chaque unité est tarifée au niveau de stock le plus bas de son mouvement : **acheter puis revendre dans la même ville fait toujours perdre**. Le blé et l'argent ont un cours fixe.
- **Échanges** : `TradeService.Buy` / `Sell` vérifient **toutes** les conditions avant de rien changer (stock de la ville, argent, masse, volume, quantité possédée), puis appliquent portefeuille, stock et cargaison, avec annulation si un pas échoue. Un échange refusé ne change rien et n'émet aucun événement.
- **Évolution** : `EconomyTicker` écoute `DayStarted` et applique à chaque marché un jour de production, de consommation puis de retour de 20 % de l'écart vers la cible (`MarketDayRules`, arithmétique entière). Une marchandise hors du profil d'une ville n'y a ni cible ni production : ce qu'on y vend s'écoule vers 0.
- **Règle d'or** : l'interface ne calcule aucune règle. Elle demande à `TradeService` et écoute les événements.

### 4.3 Du combat à la carte et retour

1. `CampaignEncounters` détecte le contact et lève `EncounterStarted`.
2. `CampaignEncounterFlow` construit un `EncounterData` (les deux groupes, le butin tiré de la bande, l'arène), le dépose dans l'asset `EncounterChannel`, **désactive tous les autres objets racine de la scène** (la carte « dort » telle quelle), et charge l'arène en additif. Il calcule déjà ce que la cargaison peut prendre du butin (`Inventory.PlanAdd`, marchandises les plus chères d'abord), pour que l'écran de fin le dise. Cet objet doit rester un objet racine à lui.
3. `CombatSpawner` (dans l'arène) lit `EncounterData` et crée les unités. À la fin, l'écran de fin de combat liste le butin (U.V., marchandises gagnées, « perdu faute de place ») et demande le retour par le canal.
4. `CampaignEncounterFlow` décharge l'arène, réveille la carte et applique le résultat : survivants avec leurs PV, morts retirés du groupe, bande battue mise de côté (désactivée, pas détruite : voir `BandRespawner`), U.V. du butin versées au `Wallet`, marchandises qui rentrent ajoutées à l'`Inventory` (`TryAdd`), le reste perdu et signalé par le message de la carte. Une défaite lève `GameOver` ; vaincre la dernière bande lève `AllBandsDefeated`.

### 4.4 Le système d'actions (combat et IA)

`ActionController` est le seul moyen de faire agir une unité. Le joueur (via `PlayerCommandController`) et l'IA (`AICombatBrain`, `AIUnitController`) l'utilisent de la même façon. Il valide la demande, exécute une action à la fois, garde une file de 4 ordres au plus et tient un journal (demandé / accepté / refusé / terminé). `UnitMover` exécute les déplacements ; il ne sait rien du combat.

### 4.5 L'interface de ville

Quand la caravane arrive : repos, puis `TownPanel` (hub) s'ouvre et tient la carte. Chaque fonction est une `TownFunctionWindow` (marché, recrutement) qui remplace le hub à l'écran et se ferme par « Retour à la ville ». **Règle d'affichage** : toute fenêtre qui s'ouvre appelle `UiLayering.BringToFront` ; l'horloge, le groupe, les U.V. et la cargaison restent dessous.

---

## 5. Les données : où changer quoi

Tout se règle dans des ScriptableObjects (`Assets/ScriptableObjects/`) :

| Pour changer…                                              | Ouvrir                                                                       |
| ---------------------------------------------------------- | ---------------------------------------------------------------------------- |
| Les chiffres d'une unité (PV, arme, armure…)               | `Units/Def_Fantassin`, `Def_Archer` (`UnitDefinition`)                       |
| Le comportement d'un ennemi                                | `AI/AI_Aggressive`, `AI_Cautious`                                            |
| Les règles de combat, zones, durées d'actions              | `Rules_Default`, `Zones_Default`, `ActionTiming_Default`, `CombatSettings_*` |
| Les terrains (coût de chemin, vitesse)                     | `TerrainSettings`                                                            |
| Les offres et le prix de recrutement, la taille du groupe  | `RecruitmentSettings`                                                        |
| La régénération (% par jour, plafond) et le prix du soin par PV | `HealingSettings_Default` |
| Route ou errance des bandes (poids, rayons, attente), routes générées, distance entre bandes, marge de bord | `BandBehaviorSettings_Default` |
| La taille de la file d'ordres sur la carte (Maj + clic) | `OrderQueueSettings_Map` (en combat : `CombatSettings.MaxQueuedOrders`) |
| Le salaire par jour d'une unité | `UnitDefinition.dailyWage` (`Def_*`) |
| Les jours de paie offerts, la dette qui bloque le recrutement | `PayrollSettings_Default` |
| Le texte de l'aide                                         | `GameHelp`                                                                   |
| Le message au lancement de la carte | `CampaignHud.startMessage` (objet `CampaignCanvas` de `Demo_Campaign`) |
| La première bande à battre (premier capital) | Le Voleur : `Enemy_6`, table `Loot_Voleur` ; le simulateur la lit dans `SimulationSettings.starterLoot` |
| Un prix de base, une masse, un volume                      | `Economy/Goods/Good_*`                                                       |
| Ce que produit / consomme une ville, son stock cible       | `Economy/Towns/Town_*`                                                       |
| La courbe des prix, les bornes, l'écart acheteur / vendeur | `Economy/PricingSettings_Default`                                            |
| Vitesse du retour des marchés vers leur cible              | `Economy/EconomySettings_Default`                                            |
| La capacité de la caravane (kg, L)                         | `Economy/CargoCapacity_Caravan`                                              |
| Les réglages du simulateur                                 | `Economy/SimulationSettings_Default`                                         |
| Le butin d'une bande (valeur totale, ressources et poids), les mots de richesse | `Loot/Loot_*.asset` (une table par bande, assignée sur le `PartyRoster` de l'objet `Enemy_*`) ; `Loot/LootWealthScale_Default` ; la graine : composant `BandLoot` de la scène |
| Les minutes de jeu par mètre                               | Le composant `CampaignClock`, objet `Campaign`                               |

### Structure de `Demo_Campaign`

Objets racine principaux : `Ground` (NavMesh), `Main Camera`, `Caravan` (le joueur : mouvement, groupe, `Wallet`, `Inventory`, `TradeService`), `Enemy_1` à `Enemy_6` (bandes patrouillantes), `Towns` (5 villes avec leur `Market`), `Campaign` (horloge, déplacement, rencontres, repos, recrutement, pause), `Economy` (`EconomyTicker`), `EncounterFlow`, `CampaignCanvas` (toute l'interface de la carte et des villes), `TerrainZones`, `PatrolRoutes`, `EconomySimulator` et les vues de debug.

---

## 6. L'équilibrage : le simulateur

`EconomySimulator` (objet de `Demo_Campaign`) joue l'économie **sans rendu** : un marchand simulé achète, voyage (durées réelles du NavMesh), se bat (modèle tiré au sort : `SimulationCombat`) et vend, **uniquement par `TradeService`** et par le vrai calendrier. Son groupe (`SimulatedParty`) part du vrai groupe de la caravane : PV, salaires payés par `PayrollRules` (la vraie paie est coupée pendant la simulation puis remise), régénération du jeu, hôpital et recrues par des règles simples de `SimulationSettings`, réserve de salaires. Chaque U.V. passe par un grand livre vérifié à chaque étape. Il écrit `ECONOMY_REPORT.md` : verdict sur les critères d'équilibrage, profit après salaires, détail de l'argent, part du butin, ventes hors profil, effet de chaque mécanique (une enlevée à la fois), sensibilité à la chance de victoire, parties de 300 jours, anomalies, protocole de test automatique, réglages suggérés.

Les combats du simulateur suivent une table mesurée (`CombatOddsTable`, asset `CombatOdds_Bench`) : le marchand combat à 85 % de chances ou plus, sinon contourne la bande.

**Banc d'essai des combats** (pour refaire la table) : en Play Mode dans `Combat_Arena`, pour chaque combat : créer un `EncounterData("Combat_Arena", LootResult.Empty, true)` avec `PlayerSideByAi = true` et les deux camps, `EncounterChannel.Begin`, `SceneManager.LoadScene("Combat_Arena")`, puis avancer image par image (`EditorApplication.Step`) jusqu'à `HasResult`, en réimposant `Time.timeScale = 5` à chaque image (le `CombatManager` le remet à 1 à chaque reprise ; ×10 fausse les résultats), en levant la pause (`SetPaused(false)`) et en coupant les caméras. Écrire ensuite la table avec `CombatOddsTable.Set`. Remettre `timeScale` à 1 et vérifier `ProjectSettings`.

Lancer : entrer en Play Mode dans `Demo_Campaign` (session neuve), puis appeler `EconomySimulator.Run()` une fois (par exemple avec une commande d'éditeur ; environ 2 secondes). Rien n'est sauvegardé dans la session. À rejouer après tout changement de prix, de profil de ville ou de capacité.

---

## 7. Comment ajouter…

- **Une marchandise** : créer un `GoodDefinition` (identifiant en minuscules sans espaces), l'ajouter au `GoodCatalog_Default`, puis lui donner une place dans les `TownDefinition` des villes qui la produisent ou la consomment (le `MarketNetworkCheck` avertit si personne ne la produit ou ne la consomme).
- **Une ville** : un objet avec `PointOfInterest` et `Market` dans `Towns`, un nouveau `TownDefinition` (deux villes ne doivent pas partager le même profil), un `TownLabel` ; vérifier le NavMesh à son entrée.
- **Une fonction de ville** (forge, taverne…) : créer une fenêtre qui hérite de `TownFunctionWindow`, la placer dans `CampaignCanvas`, l'ajouter à la liste `functions` du `TownPanel`. Elle passera au premier plan toute seule.
- **Un événement d'économie** (famine, fête) : implémenter `IEconomyModifier` et l'inscrire par `EconomyTicker.AddModifier`. La liste est vide pour l'instant.
- **Une unité** : un `UnitDefinition`, puis une offre dans `RecruitmentSettings` ou un membre dans le `PartyRoster` d'une bande.
- **Le butin d'une bande** : un `LootTable` (valeur totale min et max, poids des U.V. et de chaque marchandise ; ni épices ni argent tant qu'on ne veut pas aggraver l'anomalie des épices), assigné au `PartyRoster` de la bande ; `BandLootSetup` le tire au démarrage. Une bande sans table est une erreur dans la console.
- **Un type d'action de combat** : une classe qui hérite de `UnitAction`, lancée par `ActionController` ; le joueur et l'IA y auront accès par le même chemin.

---

## 8. Règles de l'équipe (résumé de `CLAUDE.md`)

- Une étape à la fois : **plan validé avant de coder**, puis vérification à trois niveaux (compilation, Play Mode sans erreur en console, capture d'écran quand c'est visuel), rapport honnête de ce qui a été et n'a pas été testé.
- C# : références par `[SerializeField] private` (pas de `Find` / `GetComponent` à l'exécution), pas de logique lourde ni d'allocation dans `Update`, événements initialisés avec un délégué vide, `Debug.LogError` plutôt qu'un échec silencieux, jamais de `#pragma warning disable`, un script = une responsabilité, fichier nommé comme sa classe.
- Les données de jeu vont dans des ScriptableObjects, jamais en dur.
- Scènes et prefabs : modifiés par l'éditeur (outil MCP / scripts éditeur temporaires dans `Assets/Editor/TemporaryGeneratedScripts/`, supprimés ensuite), **jamais** en éditant le YAML à la main ni les fichiers `.meta`.
- Ne jamais, sans accord explicite : supprimer des fichiers ou assets, modifier `ProjectSettings/`, `Packages/manifest.json`, les asmdef ou la version de l'éditeur, installer ou retirer un package, faire un `push`, un `reset --hard`, un `clean` ou réécrire l'historique.
- Ne pas versionner `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`.
- Toute fenêtre qui s'ouvre passe au premier plan (`UiLayering.BringToFront`).
- Mettre à jour `GAME_DESIGN.md` (décisions), `PROJECT_LOG.md` (ce qui est fait) et **ce guide** (structure) à chaque étape qui les concerne. Messages de commit en anglais.

---

## 9. Pièges connus

- **Compilation et focus** : Unity ne recompile pas quand sa fenêtre n'a pas le focus ; après une modification de script, forcer un `AssetDatabase.Refresh()` et attendre que `Library/ScriptAssemblies/Assembly-CSharp.dll` soit plus récent que le script.
- **Tests en éditeur sans focus** : quand la fenêtre d'Unity n'a pas le focus, le Play Mode peut ne plus avancer d'image (le compteur d'images reste figé, un `Destroy` reste en attente). Passer l'éditeur en pause (`EditorApplication.isPaused = true`) et avancer par `EditorApplication.Step()` dans une boucle avec une courte attente débloque tout (touches simulées comprises : celles-ci n'ont pas été vues dans ce mode, utiliser les méthodes publiques, par exemple `UnitMover.MoveTo` pour faire passer le temps).
- **`timeScale` enregistré à 0** : après des tests de pause, Unity a déjà enregistré `ProjectSettings/TimeManager.asset` avec `timeScale` 0 (la carte paraît « gelée », les unités « ne bougent pas »). En fin de test, `git status ProjectSettings` ; restaurer `TimeManager.asset` (`git checkout`) et remettre `runInBackground` à `false`. En cas de doute sur une unité immobile, lire `Time.timeScale` d'abord.
- **Les builds réécrivent des réglages** : un build (même raté) modifie `ProjectSettings/` et des assets de rendu (`PC_RPAsset.asset`, `UniversalRenderPipelineGlobalSettings.asset`…). Les restaurer ensuite sauf décision contraire. `BuildPipeline.BuildPlayer` ne tourne pas dans une commande MCP : le lancer par `EditorApplication.delayCall` et écrire le compte rendu dans un fichier.
- **Build depuis le MCP** : `BuildPipeline.BuildPlayer` est refusé dans une commande MCP ; passer par un `[MenuItem]` dans un script temporaire de `Assets/Editor/TemporaryGeneratedScripts/`, l'exécuter avec `Unity_ManageMenuItem`, différer le build par `EditorApplication.delayCall` et écrire le rapport dans un fichier ; supprimer le script ensuite. `AssetDatabase.DeleteAsset` est refusé aussi : pour supprimer un asset, l'appeler depuis un menu du même script temporaire ; supprimer le dossier `TemporaryGeneratedScripts` par le système de fichiers, mais **jamais `Assets/Editor` ni `Assets/Editor.meta`** (suivis par git).
- **Matériaux aux noms trompeurs** : `Mat_PathOutOfRange` sert au trait d'un tir bloqué (`CombatTargeting.blockedShotMaterial`, dans les scènes de combat) et `Mat_PathPlan` au trait des ordres en file (`QueuedPathLine` du prefab `Unit`) : ne pas les prendre pour des restes.
- **Versions de démo** : toujours au format x.y.z (`PlayerSettings.bundleVersion`), zip nommé `Marchand_Demo_Windows_vX.Y.Z.zip`, version et commit notés dans `PROJECT_LOG.md`.
- **Tests au clavier simulé** : dans une longue session de Play, les touches simulées finissent par ne plus être vues ; relancer le Play. Les touches I et Échap n'ont été testées qu'indirectement.
- **Namespace** : dans un script éditeur, écrire `UnityEngine.UI.Image` (alias `UImage`) pour éviter la collision avec un autre type `Image`.
- **Paie et calendrier du simulateur** : `EconomySimulator` avance le vrai calendrier ; il coupe la vraie paie (`PartyPayroll.enabled = false`) pendant la simulation et paie lui-même son groupe simulé, puis la remet. Tout nouveau coût quotidien du jeu prélevé sur le Wallet doit être coupé ou suivi de la même façon, et ajouté au grand livre (`ScenarioResult.LedgerBalance`), sinon le contrôle T2 échoue.
- **Précision du calendrier** : `CampaignClock` compte les minutes en `double` : ne pas revenir à un `float` (la fraction de minute ajoutée à chaque image se perd dès quelques centaines de jours, et les rapports du simulateur ne seraient plus reproductibles).
- **`NavMeshPath` dans un champ** : le créer dans `Awake` ou `Start`, jamais dans l'initialiseur d'un champ de MonoBehaviour (exception au chargement).
- **Tests qui avancent le calendrier** : au-delà du jour 100, `DemoGoal` termine la démo et fige la carte (état Held, bandes immobiles) : rester sous 100 jours ou relancer le Play.
- **Tester une défaite de bande sans combat** : désactiver la bande et appeler `OnBandDefeated` du `BandRespawner` par `SendMessage` (l'événement du flux ne peut pas être levé de l'extérieur).
- **Une ville n'achète que son profil** : `TradeService` refuse une vente hors profil (`TradeStatus.NotBoughtHere`) ; tout nouvel acheteur (simulateur, interface, quête) doit passer par lui ou tester `Market.IsInProfile`, et `TradePlanner` ne vise que des villes qui achètent.
- **IA de combat et recherche** : sans recherche, deux camps IA restent à leur départ ; avec une recherche vers un point fixe, deux camps qui se croisent hors de vue fouillent le camp vide d'en face. La recherche a besoin de l'ouïe et du filet (`AIUnitController`). La recherche n'est donnée que par `CombatSpawner` (arène) : ne pas l'activer pour les bandes de la carte.
- **Test dans la scène du jeu** : `CampaignEncounterFlow` endort tous les objets racine de la scène sauf le sien. Tout nouvel objet racine de `Demo_Campaign` dort donc pendant un combat ; ne pas y mettre ce qui doit rester éveillé.

---

## 10. Où en est-on, et la suite

Fait : démo jouable (v0.2.0), ménage technique du code débranché (G0), économie E1 à E9, butin en marchandises (F1), marché à onglets et profit (F2), retour des bandes et nouveau but de la démo (F3), soin naturel et hôpital (F4), salaires du groupe et dette (F5), file d'ordres sur la carte, comportement des bandes (route ou errance).

À décider avec le responsable du jeu :

1. **Réglages d'équilibrage** d'après `ECONOMY_REPORT.md` : bornes de prix (marge théorique de 262 %), profils des villes (une ville n'achète plus que son profil : les routes rentables sont celles du profil), butin des bandes (maintenant en U.V. et en marchandises, à rejuger sur le rapport), prix des recrues.
2. **Départ de la partie** : avec seulement la part en U.V. du plus petit butin (≈ 600 U.V.), le marchand simulé se bloque ; avec la valeur complète (marchandises vendues), il s'en sort (rapport, § 5 bis). À juger en jouant : faut-il un petit capital de départ ?
3. **Partie manuelle** : la jouer depuis 0 U.V. pour juger du rythme (le simulateur ne peut pas le faire).
4. Points de design non tranchés : liste du § 8 de `GAME_DESIGN.md` (initiative, aggro, course, ouïe, équilibrage des unités, dialogue avant combat, arène intérieure).
5. Idées non demandées : sélecteur de vitesse du temps, temps qui passe en ville, autres fonctions de ville (forge, taverne), conséquences réelles d'une défaite (aujourd'hui : fin de partie provisoire).
