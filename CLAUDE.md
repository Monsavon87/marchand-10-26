# Directives pour l'agent — Projet Unity 3D

## Contexte
- Jeu 3D, Unity 6 (éditeur 6000.6.x), pipeline URP.
- Je donne les consignes et je valide les résultats. Tu fais le travail.
- Réponds-moi en français. Code, noms de classes/variables et commentaires de code en anglais.

## Méthode de travail (obligatoire)
1. **Plan d'abord.** Pour toute tâche qui dépasse un petit correctif, propose un plan court (fichiers touchés, objets de scène concernés, risques) et attends mon accord avant de modifier quoi que ce soit.
2. **Exécute** uniquement ce qui est dans le plan validé. Si tu découvres qu'il faut plus, reviens me voir.
3. **Vérifie** avant de dire que c'est fini :
   - Niveau 1 : aucune erreur de compilation (lis la console Unity).
   - Niveau 2 : Play Mode lancé quelques secondes, puis arrêté, console sans erreur ni exception.
   - Niveau 3 : capture d'écran de la scène ou du Game view quand le résultat est visuel.
4. **Rapport final** court : ce qui a changé, ce qui a été vérifié, ce qui reste à tester par moi. Ne prétends jamais avoir testé quelque chose que tu n'as pas testé.
5. **Documents de suivi.** À chaque étape, mets à jour `GAME_DESIGN.md` (décisions), `PROJECT_LOG.md` (ce qui est fait) et `PROJECT_GUIDE.md` (guide de reprise : structure du code, scènes, données, pièges) quand l'étape change la structure ou la façon de travailler.

## Git
- Avant une grosse tâche, propose un commit de sécurité (message clair, en anglais).
- Ne fais jamais de `push`, `reset --hard`, `clean`, ni de réécriture d'historique sans mon accord explicite.
- Ne commit pas `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`. Vérifie le `.gitignore` Unity au démarrage.

## Interdits sans mon accord explicite
- Supprimer des fichiers, scènes, prefabs ou assets.
- Modifier `ProjectSettings/`, `Packages/manifest.json`, les Assembly Definitions ou la version de l'éditeur.
- Installer, mettre à jour ou retirer un package.
- Toucher aux fichiers `.meta` à la main.

## Scènes et prefabs
- Utilise le MCP / CLI Unity pour créer ou modifier des GameObjects, scènes et prefabs.
- N'édite **jamais** les fichiers de scène ou de prefab (YAML) à la main : risque de corrompre les IDs.
- Les scripts éditeur temporaires (ex. construire une hiérarchie en masse) vont dans `Assets/Editor/TemporaryGeneratedScripts/`. Supprime-les à la fin de la tâche.
- Ne me demande pas de réglage manuel dans l'éditeur si tu peux le faire toi-même via le MCP. Si c'est impossible, dis-moi exactement quoi cliquer.

## Qualité du code C#
- Références via `[SerializeField] private` plutôt que `GetComponent` / `GetChild` / `Find`.
- Pas de logique lourde dans `Update` (pas d'allocations, pas de `Find`, pas de LINQ dans les boucles chaudes).
- Événements initialisés avec un delegate vide pour éviter les vérifications de null.
- Si un null ou un état invalide casserait le comportement, journalise avec `Debug.LogError` : pas d'échec silencieux.
- Ne masque pas les avertissements (`#pragma warning disable`) : corrige la cause.
- Un script = une responsabilité. Nomme les fichiers comme la classe. Ranges-les par fonctionnalité sous `Assets/Scripts/`.
- Les données de jeu (stats, configs) vont dans des ScriptableObjects, pas en dur dans le code.

## Interface (UI)
- Toute fenêtre qui s'ouvre passe au premier plan : elle appelle `UiLayering.BringToFront` à l'ouverture, pour que le résultat ne dépende jamais de l'ordre des objets dans la scène. Les affichages permanents de l'écran (horloge, groupe, U.V. et cargaison) restent en dessous de toutes les fenêtres.
- Une nouvelle fenêtre ou un nouvel affichage = vérifier à l'écran (capture) qu'il est bien au-dessus ou au-dessous de ce qu'il faut.
- La page d'aide a une place limitée : le principe tient en 5 ou 6 lignes de 140 caractères au plus. Quand une étape y ajoute du texte, vérifier par capture que rien ne déborde sur les listes de contrôles (résumer plutôt qu'empiler).

## Compétences Unity
- Avant de toucher à l'UI, URP, post-processing, NavMesh, physique, audio ou packages, utilise le skill Unity correspondant si disponible.
- Si une API ou un comportement t'est incertain, cherche dans la documentation Unity plutôt que de deviner.

## Quand tu doutes
- Pose-moi la question plutôt que de supposer, surtout sur le design du jeu.
- Ne change pas de direction technique en cours de route sans me prévenir.
