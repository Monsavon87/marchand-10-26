# Rapport d'équilibrage économique (boucle complète : butin, retour des bandes, soin, recrues, salaires)

Simulation sans rendu de **100 jours** (et 300 jours pour les parties longues), graine **1**, capital de départ **10 000 U.V.**, 5 villes, 10 marchandises. Le marchand achète et vend uniquement par `TradeService`, avec le calendrier et les durées de trajet réelles de la carte (chemin NavMesh, terrains compris). Son groupe part de la composition réelle de la caravane (Def_Fantassin 40/40, Def_Archer 25/25) ; il est payé chaque jour par la règle du jeu (`PayrollRules` : 7 jours offerts, dette payée en premier), se soigne par la régénération du jeu (8 % par jour jusqu'à 80 %) et à l'hôpital (25 U.V. par PV). Ce rapport est reproductible : même graine, même résultat. **Aucun réglage du jeu n'a été modifié.**

## 0. Verdict

| Critère | Mesure | Résultat |
|---|---|---|
| Profit positif (après salaires, soins et recrues) | B : 74 478 U.V. (744 U.V. par jour) ; C : 139 873 U.V. (1 371 U.V. par jour) ; C positif sur 10 graines sur 10 | **respecté** |
| Profit borné (dernière tranche de 30 jours ≤ 1.50 × la deuxième) | B : × 0.28 ; C : × 1.61 | **non respecté** |
| Butin qui pèse sans dominer (10 % à 40 % des recettes, moyenne des graines) | moyenne des 10 graines : 54 % (de 47 % à 62 %) ; graine 1 seule : 51 % | **non respecté** |
| Aucun profit infini (300 jours : dernier tiers ≤ 1,1 × le tiers du milieu, pas de dépassement) | B : × 1.01 ; C : × 1.03 | **respecté** |

Survie de la boucle complète : 9 partie(s) sur 10 vont au bout des 100 jours sans défaite (une défaite met fin à la campagne, comme dans le jeu).

## 1. Résumé des scénarios

Profit net = argent à la fin - dette - capital de départ : salaires, soins et recrues déjà payés.

| Scénario | Profit net | Par jour | Trajets | Salaires | Soins | Recrues | Butin (part des recettes) | Combats V/D | Groupe final | Fin |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| A. Marchand glouton, sans combat | 77 780 U.V. | 777 U.V. | 58 | 8 930 U.V. | 0 U.V. | 0 U.V. (0) | 0 U.V. (0 %) | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 100 jours écoulés. |
| B. Marchand prévoyant, sans combat | 74 478 U.V. | 744 U.V. | 58 | 8 930 U.V. | 0 U.V. | 0 U.V. (0) | 0 U.V. (0 %) | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 100 jours écoulés. |
| B'. Scénario B rejoué (contrôle de graine) | 74 478 U.V. | 744 U.V. | 58 | 8 930 U.V. | 0 U.V. | 0 U.V. (0) | 0 U.V. (0 %) | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 100 jours écoulés. |
| C. Boucle complète (combats, butin, soin, recrues, salaires) | 139 873 U.V. | 1 371 U.V. | 59 | 16 625 U.V. | 6 025 U.V. | 9 000 U.V. (3) | 87 989 U.V. (51 %) | 17 / 0 | Def_Fantassin 40/40, Def_Archer 20/25, Def_Fantassin 33/40, Def_Fantassin 32/40 | Terminé : 100 jours écoulés. |
| C'. Scénario C rejoué (contrôle de graine) | 139 873 U.V. | 1 371 U.V. | 59 | 16 625 U.V. | 6 025 U.V. | 9 000 U.V. (3) | 87 989 U.V. (51 %) | 17 / 0 | Def_Fantassin 40/40, Def_Archer 20/25, Def_Fantassin 33/40, Def_Fantassin 32/40 | Terminé : 100 jours écoulés. |
| E. Départ réaliste (capital = argent du plus petit butin), sans combat | 428 U.V. | 26 U.V. | 5 | 855 U.V. | 0 U.V. | 0 U.V. (0) | 0 U.V. (0 %) | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Bloqué : aucun trajet rentable pendant 10 jours (ville Bourg-Val). |
| E2. Départ réaliste (capital = valeur complète du plus petit butin), sans combat | 73 559 U.V. | 728 U.V. | 61 | 8 930 U.V. | 0 U.V. | 0 U.V. (0) | 0 U.V. (0 %) | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 100 jours écoulés. |
| C sans hôpital | 92 741 U.V. | 1 449 U.V. | 39 | 9 465 U.V. | 0 U.V. | 9 000 U.V. (3) | 50 419 U.V. (45 %) | 10 / 1 | Def_Fantassin 32/40, Def_Fantassin 32/40, Def_Fantassin 32/40, Def_Fantassin 32/40 | Défaite en combat sur la route Hautmont > Bourg-Val (fin de la campagne). |
| C sans recrutement | 99 007 U.V. | 980 U.V. | 57 | 8 930 U.V. | 1 200 U.V. | 0 U.V. (0) | 23 748 U.V. (22 %) | 6 / 0 | Def_Fantassin 32/40, Def_Archer 20/25 | Terminé : 100 jours écoulés. |
| C sans salaires | 160 298 U.V. | 1 602 U.V. | 58 | 0 U.V. | 6 750 U.V. | 6 000 U.V. (2) | 89 890 U.V. (52 %) | 17 / 0 | Def_Fantassin 32/40, Def_Archer 25/25, Def_Fantassin 32/40, Def_Fantassin 40/40 | Terminé : 100 jours écoulés. |
| B sans salaires | 83 439 U.V. | 834 U.V. | 58 | 0 U.V. | 0 U.V. | 0 U.V. (0) | 0 U.V. (0 %) | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 100 jours écoulés. |
| B sur 300 jours | 157 742 U.V. | 524 U.V. | 162 | 27 930 U.V. | 0 U.V. | 0 U.V. (0) | 0 U.V. (0 %) | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 300 jours écoulés. |
| C sur 300 jours | 328 404 U.V. | 1 091 U.V. | 164 | 51 450 U.V. | 16 225 U.V. | 9 000 U.V. (3) | 217 711 U.V. (54 %) | 40 / 0 | Def_Fantassin 34/40, Def_Archer 21/25, Def_Fantassin 36/40, Def_Fantassin 35/40 | Terminé : 300 jours écoulés. |

## 2. Durées de trajet (jours de calendrier, distance en mètres)

| De \ Vers | Port-Brume | Aubeterre | Hautmont | Bourg-Val | Carrefour |
|---|---:|---:|---:|---:|---:|
| Port-Brume (Portuaire) | - | 2.0 j (356 m) | 1.0 j (183 m) | 0.9 j (168 m) | 0.6 j (96 m) |
| Aubeterre (Agricole) | 2.0 j (356 m) | - | 1.0 j (178 m) | 1.1 j (192 m) | 1.5 j (242 m) |
| Hautmont (Minière) | 1.0 j (183 m) | 1.0 j (178 m) | - | 1.9 j (347 m) | 1.2 j (202 m) |
| Bourg-Val (Artisane) | 0.9 j (168 m) | 1.1 j (192 m) | 1.9 j (347 m) | - | 0.9 j (153 m) |
| Carrefour (Transit) | 0.6 j (96 m) | 1.5 j (242 m) | 1.1 j (173 m) | 0.7 j (121 m) | - |

## 3. Ce qui rapporte (scénario B, sans combat)

Par marchandise transportée :

| Marchandise | Trajets | Unités | Coût | Recette | Profit | Rendement moyen |
|---|---:|---:|---:|---:|---:|---:|
| Outils | 25 | 96 | 275 846 U.V. | 313 152 U.V. | 37 306 U.V. | 14 % |
| Fer | 25 | 157 | 100 553 U.V. | 120 496 U.V. | 19 943 U.V. | 20 % |
| Épices | 2 | 3 | 40 950 U.V. | 58 186 U.V. | 17 236 U.V. | 42 % |
| Cuir | 3 | 14 | 19 260 U.V. | 23 382 U.V. | 4 122 U.V. | 21 % |
| Tissu de laine | 2 | 9 | 19 982 U.V. | 23 858 U.V. | 3 876 U.V. | 19 % |
| Vin | 1 | 7 | 3 366 U.V. | 4 291 U.V. | 925 U.V. | 27 % |

Par route (ville de départ > ville d'arrivée) :

| Route | Trajets | Profit | Profit / jour de trajet |
|---|---:|---:|---:|
| Bourg-Val > Hautmont | 22 | 31 300 U.V. | 738 U.V. |
| Hautmont > Bourg-Val | 23 | 18 949 U.V. | 427 U.V. |
| Port-Brume > Bourg-Val | 2 | 17 236 U.V. | 9 215 U.V. |
| Bourg-Val > Aubeterre | 3 | 6 743 U.V. | 2 106 U.V. |
| Aubeterre > Hautmont | 3 | 3 230 U.V. | 1 091 U.V. |
| Aubeterre > Bourg-Val | 2 | 2 947 U.V. | 1 380 U.V. |
| Bourg-Val > Port-Brume | 1 | 2 009 U.V. | 2 148 U.V. |
| Hautmont > Aubeterre | 1 | 561 U.V. | 568 U.V. |
| Hautmont > Port-Brume | 1 | 433 U.V. | 426 U.V. |

Trajets (les 12 premiers et les 4 derniers) :

| Jour | Trajet | Marchandise | Quantité | Coût | Recette | Profit | Profit prévu | Vente hors profil |
|---:|---|---|---:|---:|---:|---:|---:|---|
| 0 | Aubeterre > Bourg-Val | Cuir | 6 | 8 390 U.V. | 10 598 U.V. | 2 208 U.V. | 2 208 U.V. |  |
| 1 | Bourg-Val > Aubeterre | Outils | 4 | 10 710 U.V. | 14 049 U.V. | 3 339 U.V. | 3 339 U.V. |  |
| 2 | Aubeterre > Hautmont | Outils | 2 | 7 277 U.V. | 8 407 U.V. | 1 130 U.V. | 1 130 U.V. |  |
| 3 | Hautmont > Bourg-Val | Fer | 11 | 6 803 U.V. | 9 239 U.V. | 2 436 U.V. | 2 436 U.V. |  |
| 5 | Bourg-Val > Aubeterre | Tissu de laine | 5 | 10 787 U.V. | 13 281 U.V. | 2 494 U.V. | 2 494 U.V. |  |
| 6 | Aubeterre > Hautmont | Cuir | 4 | 5 435 U.V. | 6 610 U.V. | 1 175 U.V. | 1 175 U.V. |  |
| 7 | Hautmont > Aubeterre | Fer | 5 | 3 187 U.V. | 3 748 U.V. | 561 U.V. | 561 U.V. |  |
| 8 | Aubeterre > Hautmont | Vin | 7 | 3 366 U.V. | 4 291 U.V. | 925 U.V. | 925 U.V. |  |
| 9 | Hautmont > Bourg-Val | Fer | 7 | 4 599 U.V. | 5 609 U.V. | 1 010 U.V. | 1 010 U.V. |  |
| 11 | Bourg-Val > Port-Brume | Outils | 4 | 11 214 U.V. | 13 223 U.V. | 2 009 U.V. | 2 009 U.V. |  |
| 12 | Port-Brume > Bourg-Val | Épices | 1 | 12 600 U.V. | 21 612 U.V. | 9 012 U.V. | 9 012 U.V. |  |
| 13 | Bourg-Val > Hautmont | Outils | 5 | 14 805 U.V. | 17 383 U.V. | 2 578 U.V. | 2 578 U.V. |  |
| ... | | | | | | | | |
| 92 | Bourg-Val > Hautmont | Outils | 4 | 11 466 U.V. | 12 871 U.V. | 1 405 U.V. | 1 405 U.V. |  |
| 94 | Hautmont > Bourg-Val | Fer | 6 | 3 854 U.V. | 4 537 U.V. | 683 U.V. | 683 U.V. |  |
| 96 | Bourg-Val > Hautmont | Outils | 4 | 11 466 U.V. | 12 871 U.V. | 1 405 U.V. | 1 405 U.V. |  |
| 98 | Hautmont > Bourg-Val | Fer | 6 | 3 854 U.V. | 4 537 U.V. | 683 U.V. | 683 U.V. |  |

## 3 bis. Ventes hors profil (interdites : une ville n'achète que ce qu'elle produit ou consomme ; ce tableau doit rester à 0)

| Scénario | Trajets hors profil | Part des trajets | Profit de ces trajets | Part du profit du commerce | Dont épices |
|---|---:|---:|---:|---:|---:|
| A. Marchand glouton, sans combat | 0 | 0 % | 0 U.V. | 0 % | 0 trajet(s), 0 U.V. |
| B. Marchand prévoyant, sans combat | 0 | 0 % | 0 U.V. | 0 % | 0 trajet(s), 0 U.V. |
| C. Boucle complète (combats, butin, soin, recrues, salaires) | 0 | 0 % | 0 U.V. | 0 % | 0 trajet(s), 0 U.V. |
| B sur 300 jours | 0 | 0 % | 0 U.V. | 0 % | 0 trajet(s), 0 U.V. |

## 4. Évolution des prix (scénario B)

Prix médian (sans écart acheteur/vendeur) : moyenne, minimum et maximum sur la durée, par ville. `profil` = la ville produit ou consomme la marchandise ; `hors profil` = elle ne l'achète pas (une ville n'achète que ce qu'elle produit ou consomme) : ce prix n'est jamais appliqué. Entre parenthèses : part des jours au prix plancher (×0.5) / plafond (×2).

**Blé** (prix de base 960 U.V., cours fixe)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 960 U.V. | 960 U.V. | 960 U.V. | 15-20 | 0 % / 0 % | 0 % |
| Aubeterre | profil | 960 U.V. | 960 U.V. | 960 U.V. | 60-60 | 0 % / 0 % | 0 % |
| Hautmont | profil | 960 U.V. | 960 U.V. | 960 U.V. | 12-20 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 960 U.V. | 960 U.V. | 960 U.V. | 18-24 | 0 % / 0 % | 0 % |
| Carrefour | profil | 960 U.V. | 960 U.V. | 960 U.V. | 40-40 | 0 % / 0 % | 0 % |

**Sel** (prix de base 300 U.V.)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 237 U.V. | 237 U.V. | 240 U.V. | 45-46 | 0 % / 0 % | 0 % |
| Aubeterre | profil | 367 U.V. | 365 U.V. | 455 U.V. | 4-8 | 0 % / 0 % | 0 % |
| Hautmont | profil | 265 U.V. | 249 U.V. | 266 U.V. | 18-20 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 330 U.V. | 330 U.V. | 330 U.V. | 10-10 | 0 % / 0 % | 0 % |
| Carrefour | profil | 300 U.V. | 300 U.V. | 300 U.V. | 15-15 | 0 % / 0 % | 0 % |

**Vin** (prix de base 520 U.V.)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 672 U.V. | 672 U.V. | 672 U.V. | 8-8 | 0 % / 0 % | 0 % |
| Aubeterre | profil | 436 U.V. | 416 U.V. | 478 U.V. | 24-30 | 0 % / 0 % | 0 % |
| Hautmont | profil | 624 U.V. | 565 U.V. | 739 U.V. | 6-12 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 613 U.V. | 613 U.V. | 613 U.V. | 10-10 | 0 % / 0 % | 0 % |
| Carrefour | profil | 520 U.V. | 520 U.V. | 520 U.V. | 14-14 | 0 % / 0 % | 0 % |

**Huile d'olive** (prix de base 2 400 U.V.)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 1 920 U.V. | 1 920 U.V. | 1 920 U.V. | 24-24 | 0 % / 0 % | 0 % |
| Aubeterre | hors profil (n'achète pas) | 4 800 U.V. | 4 800 U.V. | 4 800 U.V. | 0-0 | 0 % / 100 % | 100 % |
| Hautmont | profil | 2 937 U.V. | 2 920 U.V. | 3 640 U.V. | 4-8 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 2 640 U.V. | 2 640 U.V. | 2 640 U.V. | 10-10 | 0 % / 0 % | 0 % |
| Carrefour | profil | 2 400 U.V. | 2 400 U.V. | 2 400 U.V. | 10-10 | 0 % / 0 % | 0 % |

**Tissu de laine** (prix de base 2 400 U.V.)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 2 400 U.V. | 2 400 U.V. | 2 400 U.V. | 10-10 | 0 % / 0 % | 0 % |
| Aubeterre | profil | 2 461 U.V. | 2 400 U.V. | 3 540 U.V. | 3-8 | 0 % / 0 % | 0 % |
| Hautmont | profil | 2 553 U.V. | 2 400 U.V. | 3 720 U.V. | 3-10 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 2 084 U.V. | 1 848 U.V. | 2 208 U.V. | 30-40 | 0 % / 0 % | 0 % |
| Carrefour | profil | 2 400 U.V. | 2 400 U.V. | 2 400 U.V. | 12-12 | 0 % / 0 % | 0 % |

**Cuir** (prix de base 1 500 U.V.)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 1 776 U.V. | 1 768 U.V. | 2 132 U.V. | 6-10 | 0 % / 0 % | 0 % |
| Aubeterre | profil | 1 208 U.V. | 1 200 U.V. | 1 350 U.V. | 20-24 | 0 % / 0 % | 0 % |
| Hautmont | profil | 1 535 U.V. | 1 500 U.V. | 2 175 U.V. | 4-10 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 1 704 U.V. | 1 550 U.V. | 2 108 U.V. | 8-17 | 0 % / 0 % | 0 % |
| Carrefour | profil | 1 500 U.V. | 1 500 U.V. | 1 500 U.V. | 10-10 | 0 % / 0 % | 0 % |

**Fer** (prix de base 700 U.V.)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 742 U.V. | 700 U.V. | 1 003 U.V. | 5-12 | 0 % / 0 % | 0 % |
| Aubeterre | profil | 718 U.V. | 700 U.V. | 1 015 U.V. | 4-10 | 0 % / 0 % | 0 % |
| Hautmont | profil | 592 U.V. | 539 U.V. | 625 U.V. | 38-48 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 842 U.V. | 753 U.V. | 1 076 U.V. | 5-14 | 0 % / 0 % | 0 % |
| Carrefour | profil | 700 U.V. | 700 U.V. | 700 U.V. | 10-10 | 0 % / 0 % | 0 % |

**Outils** (prix de base 3 000 U.V.)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 3 111 U.V. | 3 000 U.V. | 4 350 U.V. | 4-10 | 0 % / 0 % | 0 % |
| Aubeterre | profil | 3 130 U.V. | 3 000 U.V. | 4 650 U.V. | 3-10 | 0 % / 0 % | 0 % |
| Hautmont | profil | 3 521 U.V. | 3 150 U.V. | 4 550 U.V. | 4-11 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 2 647 U.V. | 2 400 U.V. | 2 820 U.V. | 23-30 | 0 % / 0 % | 0 % |
| Carrefour | profil | 3 000 U.V. | 3 000 U.V. | 3 000 U.V. | 8-8 | 0 % / 0 % | 0 % |

**Épices** (prix de base 15 000 U.V.)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 13 388 U.V. | 11 250 U.V. | 14 000 U.V. | 7-10 | 0 % / 0 % | 0 % |
| Aubeterre | hors profil (n'achète pas) | 30 000 U.V. | 30 000 U.V. | 30 000 U.V. | 0-0 | 0 % / 100 % | 100 % |
| Hautmont | hors profil (n'achète pas) | 30 000 U.V. | 30 000 U.V. | 30 000 U.V. | 0-0 | 0 % / 100 % | 100 % |
| Bourg-Val | profil | 17 849 U.V. | 16 500 U.V. | 22 750 U.V. | 2-5 | 0 % / 0 % | 0 % |
| Carrefour | profil | 15 000 U.V. | 15 000 U.V. | 15 000 U.V. | 4-4 | 0 % / 0 % | 0 % |

**Argent** (prix de base 23 000 U.V., cours fixe)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 23 000 U.V. | 23 000 U.V. | 23 000 U.V. | 5-6 | 0 % / 0 % | 0 % |
| Aubeterre | profil | 23 000 U.V. | 23 000 U.V. | 23 000 U.V. | 6-6 | 0 % / 0 % | 0 % |
| Hautmont | profil | 23 000 U.V. | 23 000 U.V. | 23 000 U.V. | 12-20 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 23 000 U.V. | 23 000 U.V. | 23 000 U.V. | 3-3 | 0 % / 0 % | 0 % |
| Carrefour | profil | 23 000 U.V. | 23 000 U.V. | 23 000 U.V. | 6-6 | 0 % / 0 % | 0 % |

## 4 bis. Combats mesurés (banc d'essai)

Banc d'essai du 2026-10-25 : arène Combat_Arena, les deux camps joués par l'IA de combat (profil AI_Aggressive), recherche de l'ennemi par l'ouïe, temps ×5 (vérifié identique à ×1), 8 combats par match, unités en pleine santé. Borne basse de ce que fait un joueur.

| Caravane | Bande | Victoires | PV perdus si victoire (moyenne / pire) | Unités perdues par victoire |
|---|---|---:|---:|---:|
| Def_Fantassin x1 | Def_Fantassin x1 | 7 / 8 (88 %) | 74 % / 80 % | 0.00 |
| Def_Fantassin x1 | Def_Archer x2 | 8 / 8 (100 %) | 21 % / 35 % | 0.00 |
| Def_Fantassin x1 | Def_Archer x1, Def_Fantassin x1 | 3 / 16 (19 %) | 77 % / 90 % | 0.00 |
| Def_Fantassin x1 | Def_Fantassin x2 | 0 / 8 (0 %) | - | - |
| Def_Fantassin x1 | Def_Archer x1, Def_Fantassin x2 | 0 / 8 (0 %) | - | - |
| Def_Archer x1, Def_Fantassin x1 | Def_Fantassin x1 | 8 / 8 (100 %) | 9 % / 12 % | 0.00 |
| Def_Archer x1, Def_Fantassin x1 | Def_Archer x2 | 8 / 8 (100 %) | 15 % / 26 % | 0.00 |
| Def_Archer x1, Def_Fantassin x1 | Def_Archer x1, Def_Fantassin x1 | 6 / 16 (38 %) | 64 % / 89 % | 0.33 |
| Def_Archer x1, Def_Fantassin x1 | Def_Fantassin x2 | 8 / 8 (100 %) | 37 % / 49 % | 0.00 |
| Def_Archer x1, Def_Fantassin x1 | Def_Archer x1, Def_Fantassin x2 | 0 / 8 (0 %) | - | - |
| Def_Archer x1, Def_Fantassin x2 | Def_Fantassin x1 | 8 / 8 (100 %) | 11 % / 15 % | 0.00 |
| Def_Archer x1, Def_Fantassin x2 | Def_Archer x2 | 8 / 8 (100 %) | 7 % / 8 % | 0.00 |
| Def_Archer x1, Def_Fantassin x2 | Def_Archer x1, Def_Fantassin x1 | 16 / 16 (100 %) | 39 % / 57 % | 0.19 |
| Def_Archer x1, Def_Fantassin x2 | Def_Fantassin x2 | 8 / 8 (100 %) | 30 % / 38 % | 0.00 |
| Def_Archer x1, Def_Fantassin x2 | Def_Archer x1, Def_Fantassin x2 | 8 / 8 (100 %) | 41 % / 63 % | 0.38 |
| Def_Archer x1, Def_Fantassin x3 | Def_Fantassin x1 | 8 / 8 (100 %) | 8 % / 11 % | 0.00 |
| Def_Archer x1, Def_Fantassin x3 | Def_Archer x2 | 8 / 8 (100 %) | 6 % / 14 % | 0.00 |
| Def_Archer x1, Def_Fantassin x3 | Def_Archer x1, Def_Fantassin x1 | 16 / 16 (100 %) | 16 % / 26 % | 0.00 |
| Def_Archer x1, Def_Fantassin x3 | Def_Fantassin x2 | 8 / 8 (100 %) | 25 % / 33 % | 0.25 |
| Def_Archer x1, Def_Fantassin x3 | Def_Archer x1, Def_Fantassin x2 | 8 / 8 (100 %) | 43 % / 55 % | 0.25 |

## 5. La boucle complète : acheter, voyager, combattre, se soigner, recruter, payer, vendre (scénario C)

Rencontres : 15 % de chance par jour de voyage de croiser une des bandes présentes sur la carte. Le marchand voit la bande et sa composition (comme le joueur) : il ne combat que si sa chance de victoire atteint 85 %, sinon il la contourne (détour de 12 h, la caravane étant plus rapide que les bandes) ; un contournement rate dans 5 % des cas et le combat est alors forcé. Chance de victoire et blessures : celles des **vrais combats mesurés** (§ 4 bis) pour la composition du groupe contre celle de la bande, réduites en proportion quand le groupe est sous 80 % de ses PV ; une unité peut mourir selon la fréquence mesurée (un survivant au moins). Le butin est celui de la table de la bande (U.V. et marchandises ; les marchandises qui ne rentrent pas sont perdues) ; une bande battue revient après le délai des réglages de retour, avec un nouveau butin. En ville, le marchand garde 7 jours de salaires en réserve (sous 2 fois la réserve, il dépense tout : un marchand pauvre doit commercer pour vivre), soigne à l'hôpital (au complet) toute unité sous 60 % de ses PV si la réserve reste couverte, et recrute Def_Fantassin (3 000 U.V.) jusqu'à 4 unités, sans dette et en gardant la réserve.

D'où vient et où va l'argent (graine 1) :

| Poste | C | B (sans combat) |
|---|---:|---:|
| Marge du commerce (ventes - achats, butin vendu compris) | 140 074 U.V. | 83 408 U.V. |
| Butin en U.V. | 31 449 U.V. | 0 U.V. |
| Salaires payés | -16 625 U.V. | -8 930 U.V. |
| Hôpital | -6 025 U.V. | -0 U.V. |
| Recrues | -9 000 U.V. | -0 U.V. |
| Dette restante | -0 U.V. | -0 U.V. |
| **Profit net** | **139 873 U.V.** | **74 478 U.V.** |
| Profit net par jour | 1 371 U.V. | 744 U.V. |
| Profit par jour avant salaires | 1 534 U.V. | 834 U.V. |
| Salaires en part de la marge du commerce | 12 % | 11 % |

- Rencontres : 17 ; combats choisis : 17 ; bandes contournées : 0 ; combats forcés (contournement raté) : 0 ; combats résolus par la mesure : 17.
- Combats : 17 victoire(s), 0 défaite(s) ; unités perdues : 1 ; butin ramassé : 87 989 U.V. (perdu faute de place : 0 U.V.), soit 51 % des recettes.
- Soins : 164 PV rendus par la régénération, 241 PV à l'hôpital (5 visite(s), 6 025 U.V.).
- Recrues : 3 (9 000 U.V.) ; recrutement refusé pour dette : 0 fois ; plus grosse dette : 0 U.V..
- Marchandises restées en cargaison à la fin (au prix de base) : 0 U.V. ; moyenne sur les 10 graines : 1 200 U.V. (butin qu'aucune ville du chemin n'achetait, ou cargaison du trajet perdu).
- Groupe à la fin : Def_Fantassin 40/40, Def_Archer 20/25, Def_Fantassin 33/40, Def_Fantassin 32/40. Fin : Terminé : 100 jours écoulés.

Trajets avec un combat (graine 1) :

| Jour | Trajet | Résultat | Valeur du butin | Profit (vente du butin comprise) |
|---:|---|---|---:|---:|
| 9 | Hautmont > Bourg-Val (Fer) | Victoire | 6 729 U.V. | 3 427 U.V. |
| 16 | Aubeterre > Hautmont (Cuir) | Victoire | 1 652 U.V. | 791 U.V. |
| 19 | Bourg-Val > Hautmont (Outils) | Victoire | 14 585 U.V. | 12 551 U.V. |
| 22 | Bourg-Val > Port-Brume (Outils) | Victoire | 2 997 U.V. | 2 126 U.V. |
| 24 | Bourg-Val > Hautmont (Outils) | Victoire | 2 103 U.V. | 2 317 U.V. |
| 28 | Bourg-Val > Hautmont (Outils) | Victoire | 4 621 U.V. | 4 267 U.V. |
| 38 | Hautmont > Bourg-Val (Fer) | Victoire | 5 577 U.V. | 4 241 U.V. |
| 44 | Bourg-Val > Hautmont (Outils) | Victoire | 4 717 U.V. | 4 267 U.V. |
| 55 | Bourg-Val > Hautmont (Outils) | Victoire | 4 068 U.V. | 3 937 U.V. |
| 59 | Bourg-Val > Hautmont (Outils) | Victoire | 4 006 U.V. | 4 946 U.V. |
| 63 | Bourg-Val > Hautmont (Outils) | Victoire | 5 143 U.V. | 5 761 U.V. |
| 67 | Bourg-Val > Hautmont (Outils) | Victoire | 4 176 U.V. | 1 657 U.V. |
| 71 | Bourg-Val > Hautmont (Outils) | Victoire, 1 unité(s) perdue(s) | 14 190 U.V. | 11 939 U.V. |
| 76 | Hautmont > Bourg-Val (Fer) | Victoire | 1 939 U.V. | 2 054 U.V. |
| 80 | Hautmont > Bourg-Val (Fer) | Victoire | 3 466 U.V. | 2 838 U.V. |
| 84 | Hautmont > Bourg-Val (Fer) | Victoire | 4 158 U.V. | 3 086 U.V. |
| 94 | Bourg-Val > Hautmont (Outils) | Victoire | 3 862 U.V. | 4 819 U.V. |

Effet de la chance (même scénario, 10 graines) :

| Graine | Profit net | Par jour | Jours | V / D | Unités perdues | Butin (part) | Salaires | Hôpital | Recrues | Groupe final | Fin |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| 1 | 139 873 U.V. | 1 371 U.V. | 102 | 17 / 0 | 1 | 87 989 U.V. (51 %) | 16 625 U.V. | 6 025 U.V. | 9 000 U.V. | Def_Fantassin 40/40, Def_Archer 20/25, Def_Fantassin 33/40, Def_Fantassin 32/40 | Terminé : 100 jours écoulés. |
| 2 | 125 818 U.V. | 1 258 U.V. | 100 | 13 / 0 | 3 | 81 100 U.V. (49 %) | 15 040 U.V. | 8 450 U.V. | 15 000 U.V. | Def_Fantassin 34/40, Def_Fantassin 34/40, Def_Fantassin 33/40, Def_Fantassin 33/40 | Terminé : 100 jours écoulés. |
| 3 | 149 946 U.V. | 1 499 U.V. | 100 | 14 / 0 | 2 | 103 757 U.V. (56 %) | 15 400 U.V. | 6 825 U.V. | 12 000 U.V. | Def_Fantassin 38/40, Def_Fantassin 28/40, Def_Fantassin 36/40, Def_Fantassin 37/40 | Terminé : 100 jours écoulés. |
| 4 | 126 365 U.V. | 1 263 U.V. | 100 | 13 / 0 | 1 | 72 554 U.V. (47 %) | 15 400 U.V. | 4 150 U.V. | 9 000 U.V. | Def_Fantassin 34/40, Def_Fantassin 29/40, Def_Fantassin 29/40, Def_Fantassin 31/40 | Terminé : 100 jours écoulés. |
| 5 | 184 731 U.V. | 1 829 U.V. | 101 | 23 / 0 | 2 | 137 619 U.V. (62 %) | 15 460 U.V. | 11 025 U.V. | 12 000 U.V. | Def_Fantassin 32/40, Def_Fantassin 32/40, Def_Fantassin 32/40, Def_Fantassin 36/40 | Terminé : 100 jours écoulés. |
| 6 | 148 945 U.V. | 1 489 U.V. | 100 | 18 / 0 | 1 | 99 384 U.V. (54 %) | 15 745 U.V. | 8 975 U.V. | 9 000 U.V. | Def_Fantassin 37/40, Def_Fantassin 37/40, Def_Fantassin 32/40, Def_Fantassin 38/40 | Terminé : 100 jours écoulés. |
| 7 | 101 703 U.V. | 1 564 U.V. | 65 | 12 / 1 | 0 | 67 219 U.V. (56 %) | 10 325 U.V. | 2 100 U.V. | 6 000 U.V. | Def_Fantassin 32/40, Def_Archer 20/25, Def_Fantassin 32/40, Def_Fantassin 32/40 | Défaite en combat sur la route Bourg-Val > Hautmont (fin de la campagne). |
| 8 | 138 283 U.V. | 1 382 U.V. | 100 | 16 / 0 | 1 | 87 805 U.V. (52 %) | 16 090 U.V. | 5 850 U.V. | 9 000 U.V. | Def_Fantassin 33/40, Def_Fantassin 33/40, Def_Fantassin 32/40, Def_Fantassin 37/40 | Terminé : 100 jours écoulés. |
| 9 | 138 162 U.V. | 1 381 U.V. | 100 | 16 / 0 | 1 | 88 463 U.V. (51 %) | 16 420 U.V. | 9 450 U.V. | 9 000 U.V. | Def_Fantassin 32/40, Def_Fantassin 32/40, Def_Fantassin 40/40, Def_Fantassin 40/40 | Terminé : 100 jours écoulés. |
| 10 | 160 284 U.V. | 1 586 U.V. | 101 | 18 / 0 | 2 | 115 732 U.V. (59 %) | 15 040 U.V. | 8 900 U.V. | 12 000 U.V. | Def_Fantassin 36/40, Def_Fantassin 35/40, Def_Fantassin 32/40, Def_Fantassin 32/40 | Terminé : 100 jours écoulés. |

Parties perdues en combat : 1 sur 10. Profit net : minimum 101 703 U.V., moyenne 141 411 U.V., maximum 184 731 U.V.. Part du butin : de 47 % à 62 %.

Parties menées au bout seulement (9) : profit net moyen 145 823 U.V. (1 451 U.V. par jour), part du butin moyenne 54 %.

## 5 bis. Le départ de la partie manuelle

La caravane démarre avec **0 U.V.** : sans argent on ne peut rien acheter, la première somme vient du butin d'une bande (une partie en U.V., le reste en marchandises à revendre). Butin des bandes de la carte (valeur totale tirée dans la table de la bande, U.V. et marchandises au prix de base) : 1 500 U.V. à 3 000 U.V., 2 500 U.V. à 4 500 U.V., 3 000 U.V. à 6 000 U.V., 4 000 U.V. à 8 000 U.V., 4 500 U.V. à 8 500 U.V., 8 000 U.V. à 16 000 U.V.. La recrue la moins chère coûte 3 000 U.V.. Les salaires du groupe de départ courent dès la fin des jours offerts.

Scénario E : un marchand prévoyant qui part avec la seule part en U.V. du plus petit butin (600 U.V.) finit à 1 028 U.V. en 16 jours (5 trajets, 855 U.V. de salaires payés, plus grosse dette 0 U.V.) ; il double son capital au jour 6. Bloqué : aucun trajet rentable pendant 10 jours (ville Bourg-Val).

Scénario E2 : le même, en comptant aussi les marchandises de ce butin comme de l'argent (1 500 U.V.) finit à 75 059 U.V. en 101 jours (61 trajets, 8 930 U.V. de salaires payés, plus grosse dette 0 U.V.) ; il double son capital au jour 12. Terminé : 100 jours écoulés.

## 5 ter. Effet de chaque mécanique (une seule enlevée à la fois, même graine)

Enlever une mécanique change aussi les décisions et les tirages qui suivent (un autre trajet, un autre combat) : un écart de signe inattendu sur une seule graine n'est pas forcément une erreur.

| Comparaison | Profit net | Écart | Salaires | Hôpital | Recrues | V / D | Groupe final | Fin |
|---|---:|---:|---:|---:|---:|---:|---|---|
| C. Boucle complète (combats, butin, soin, recrues, salaires) | 139 873 U.V. | référence | 16 625 U.V. | 6 025 U.V. | 9 000 U.V. | 17 / 0 | Def_Fantassin 40/40, Def_Archer 20/25, Def_Fantassin 33/40, Def_Fantassin 32/40 | Terminé : 100 jours écoulés. |
| C sans hôpital | 92 741 U.V. | -47 132 U.V. | 9 465 U.V. | 0 U.V. | 9 000 U.V. | 10 / 1 | Def_Fantassin 32/40, Def_Fantassin 32/40, Def_Fantassin 32/40, Def_Fantassin 32/40 | Défaite en combat sur la route Hautmont > Bourg-Val (fin de la campagne). |
| C sans recrutement | 99 007 U.V. | -40 866 U.V. | 8 930 U.V. | 1 200 U.V. | 0 U.V. | 6 / 0 | Def_Fantassin 32/40, Def_Archer 20/25 | Terminé : 100 jours écoulés. |
| C sans salaires | 160 298 U.V. | 20 425 U.V. | 0 U.V. | 6 750 U.V. | 6 000 U.V. | 17 / 0 | Def_Fantassin 32/40, Def_Archer 25/25, Def_Fantassin 32/40, Def_Fantassin 40/40 | Terminé : 100 jours écoulés. |
| B. Marchand prévoyant, sans combat | 74 478 U.V. | référence | 8 930 U.V. | 0 U.V. | 0 U.V. | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 100 jours écoulés. |
| B sans salaires | 83 439 U.V. | 8 961 U.V. | 0 U.V. | 0 U.V. | 0 U.V. | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 100 jours écoulés. |

Sans combat, les salaires coûtent 8 961 U.V. de profit sur la partie (8 930 U.V. payés ; l'écart comprend aussi la réserve qui réduit les achats).

## 5 quinquies. Sensibilité au contournement raté (hypothèse du simulateur)

Les chances de victoire viennent maintenant de la mesure ; l'inconnue est la part des bandes que le joueur n'arrive pas à éviter (inattention, terrain lent, bande qui coupe la route). La boucle complète est rejouée sur les mêmes 10 graines avec d'autres taux d'échec du contournement :

| Contournement raté | Parties menées au bout | Profit net moyen | Profit net médian | Profit par jour moyen | Part du butin moyenne | Combats gagnés par partie |
|---:|---:|---:|---:|---:|---:|---:|
| 5 % (réglage actuel) | 9 / 10 | 141 411 U.V. | 139 873 U.V. | 1 462 U.V. | 54 % | 16.0 |
| 15 % | 9 / 10 | 141 411 U.V. | 139 873 U.V. | 1 462 U.V. | 54 % | 16.0 |
| 30 % | 9 / 10 | 141 411 U.V. | 139 873 U.V. | 1 462 U.V. | 54 % | 16.0 |

## 5 quater. Parties de 300 jours : le profit s'arrête-t-il de croître ?

- B sur 300 jours : profit net 157 742 U.V. en 301 jours (524 U.V. par jour). Profit par tranche de 30 jours : 38 779 U.V., 14 896 U.V., 4 118 U.V., 13 135 U.V., 19 924 U.V., 12 995 U.V., 3 978 U.V., 12 276 U.V., 13 135 U.V., 20 064 U.V.. Rapport dernier tiers / tiers du milieu : × 1.01. Terminé : 300 jours écoulés.
- C sur 300 jours : profit net 328 404 U.V. en 301 jours (1 091 U.V. par jour). Profit par tranche de 30 jours : 49 913 U.V., 30 039 U.V., 48 281 U.V., 29 375 U.V., 23 711 U.V., 20 486 U.V., 37 218 U.V., 38 244 U.V., 8 198 U.V., 38 743 U.V.. Rapport dernier tiers / tiers du milieu : × 1.03. Terminé : 300 jours écoulés.

## 6. Anomalies

- Marchandise(s) jamais rentable(s) en transport : Blé, Argent.
- Marge théorique maximale d'une unité (vendre au plafond, acheter au plancher) : 262 % du prix d'achat, au-dessus du seuil de 150 % : les bornes de prix laissent de la place à de très grosses marges.
- **Départ difficile** : avec la seule part en U.V. du plus petit butin (600 U.V.), le marchand simulé finit à 428 U.V. de profit net (Bloqué : aucun trajet rentable pendant 10 jours (ville Bourg-Val).).

## 7. Protocole de test automatique (refait à chaque rapport)

| Contrôle | Résultat | Détail |
|---|---|---|
| T2-T3. Grand livre et invariants à chaque étape (solde = capital + ventes - achats + butin - salaires - soins - recrues ; dette ≥ 0 ; groupe et PV possibles ; cargaison ; stocks ; salaires payés + dette = salaires dus) | réussi | 0 écart(s) sur 22 parties |
| T4. Déterminisme du commerce (B et B' même graine) | réussi | 58 trajets comparés |
| T4. Déterminisme de la boucle complète (C et C' : trajets, combats, salaires, soins, recrues, groupe final) | réussi | 59 trajets, 17 victoires comparés |
| Jours offerts : le premier jour payé est le jour 8 de la partie | réussi | 20 parties avec salaires, premier jour payé : 8 |
| T5. Sans salaires : rien n'est payé, aucune dette | réussi | 2 parties |
| T5. Sans combat : aucun butin, aucun soin, aucune recrue, groupe intact | réussi | 7 parties |
| T5. Sans hôpital : aucun soin payé | réussi | hôpital 0 U.V. |
| T5. Sans recrutement : aucune recrue | réussi | 0 recrue(s) |
| T5. Les salaires coûtent (B sans salaires gagne plus que B) | réussi | 83 439 U.V. contre 74 478 U.V. |
| T5. Recrutement jamais avec une dette (règle du jeu) | réussi | 0 refus pour dette au total |
| Le marchand ne combat que par choix (chance ≥ seuil) ou contournement raté ; chaque rencontre est soit un combat, soit un détour | réussi | 260 rencontres : 251 combats choisis, 1 forcés, 8 contournées |
| Combats résolus par la table mesurée (aucun repli sur le modèle simple) | réussi | 252 sur 252 combats |
| Aucune vente à une ville hors de son profil (une ville n'achète que ce qu'elle produit ou consomme) | réussi | 0 trajet(s) hors profil sur 1398 |
| T6. Pas de dépassement (soldes bien sous la limite des entiers) | réussi | solde le plus haut : 338 404 U.V. |

Les tests des règles pures (T1), de non-régression du jeu (T7) et la relecture des chiffres (T8) sont faits à part, en Play Mode : voir `PROJECT_LOG.md`.

## 8. Réglages à ajuster (suggestions, rien n'a été changé)

Valeurs actuelles : écart acheteur/vendeur 10 %, facteur de prix de ×0.5 à ×2, capital de départ simulé 10 000 U.V..

Suggestions tirées des mesures :

- **Butin** : 54 % des recettes en moyenne (au-dessus de 40 %) : baisser la valeur des tables de butin ou allonger le délai de retour des bandes.
- **Départ** : partir de 600 U.V. ne suffit pas au marchand simulé, avec les salaires (855 U.V. payés, dette maximale 0 U.V.) : un petit capital de départ, ou plus de jours offerts.
- Rapprocher les profils des villes concernées (une ville qui produit ce que l'autre consomme) ou réduire l'écart acheteur/vendeur.
- **Marges** : resserrer les bornes de prix (`minFactor` plus haut, `maxFactor` plus bas) ou élargir l'écart acheteur/vendeur, pour réduire la marge maximale d'un trajet.

| Réglage | Où | Effet |
|---|---|---|
| Facteurs minimum et maximum, courbe de prix | `PricingSettings` | Bornent la marge d'une unité et le rendement d'un trajet. |
| Écart acheteur/vendeur (`spreadPercent`) | `PricingSettings` | Coût fixe de chaque aller-retour ; plus il est grand, moins les petites routes rapportent. |
| Retour vers la cible (`reversionPercent`) | `EconomySettings` | Vitesse à laquelle un marché vidé ou inondé retrouve son prix. |
| Production, consommation, stock cible ; stock cible hors profil (`defaultTargetStock`) | `TownDefinition` (5 villes) | Quelles routes existent, et combien une ville paie ce qu'elle n'a pas. |
| Masse et volume de la cargaison | `CargoCapacity_Caravan` | Plafond du profit par trajet. |
| Salaire par jour | `UnitDefinition.dailyWage` (`Def_*`) | Coût du voyage. |
| Jours offerts, dette qui bloque le recrutement | `PayrollSettings_Default` | Départ de la partie. |
| Régénération, prix du soin par PV | `HealingSettings_Default` | Coût des blessures. |
| Prix des recrues | `RecruitmentSettings` | Coût d'un groupe complet. |
| Butin des bandes (valeur min-max, part en U.V.) | `LootTable` des bandes | Poids des combats dans les recettes. |
| Délai de retour des bandes | `BandRespawnSettings_Default` | Nombre de combats possibles, donc de butin. |
| Hypothèses du simulateur (rencontres, victoire, dégâts, réserve, seuil d'hôpital) | `SimulationSettings` | Pas des réglages du jeu : seulement la façon de jouer du marchand simulé. |

## 9. Limites de cette simulation

- Le marchand simulé est un optimiseur à courte vue (le meilleur profit par jour du prochain trajet) : un humain peut faire mieux (enchaîner des routes) ou moins bien.
- Les chances de victoire et les blessures viennent de vrais combats de l'arène joués par l'IA des deux côtés (§ 4 bis) : l'IA ne fait ni pause, ni focalisation, ni repli malin, c'est une borne basse de ce que fait un joueur. Les rencontres restent tirées au sort (chance par jour de voyage) et le contournement est une hypothèse ; un groupe blessé est jugé en réduisant la chance mesurée en pleine santé.
- Une défaite met fin à la campagne (comme dans le jeu aujourd'hui) : les moyennes sur les graines mélangent des parties complètes et des parties courtes. La cargaison du trajet perdu est achetée mais jamais vendue : une partie courte peut afficher une marge du commerce négative, et une part du butin sans signification (« commerce à perte »).
- La part du butin est une estimation : le butin est compté au prix de base, alors que ses marchandises sont vendues au prix du marché avec la cargaison (elles sont aussi dans la marge du commerce).
- Les règles du marchand (réserve de 7 jours de salaires, hôpital sous 60 %, recrues jusqu'à 4) sont simples et fixes ; un joueur peut décider autrement.
- Les bandes sont rencontrées au hasard sur la route ; leurs patrouilles et leur errance réelles ne sont pas modélisées.
- Le temps passé en ville est nul (comme dans le jeu) ; l'hôpital et le recrutement se paient directement, sans passer par leurs fenêtres.
- Les durées de trajet sont celles du chemin NavMesh à vitesse de base ralentie par les terrains traversés ; un détour décidé par le joueur n'est pas simulé.
- Toutes les parties s'enchaînent sur le vrai calendrier (en double précision) : le rapport est identique d'une exécution à l'autre, quel que soit le jour où elle commence.

