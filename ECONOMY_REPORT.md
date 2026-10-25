# Rapport d'équilibrage économique (boucle complète : butin, retour des bandes, soin, recrues, salaires)

Simulation sans rendu de **100 jours** (et 300 jours pour les parties longues), graine **1**, capital de départ **10 000 U.V.**, 5 villes, 10 marchandises. Le marchand achète et vend uniquement par `TradeService`, avec le calendrier et les durées de trajet réelles de la carte (chemin NavMesh, terrains compris). Son groupe part de la composition réelle de la caravane (Def_Fantassin 40/40, Def_Archer 25/25) ; il est payé chaque jour par la règle du jeu (`PayrollRules` : 7 jours offerts, dette payée en premier), se soigne par la régénération du jeu (8 % par jour jusqu'à 80 %) et à l'hôpital (25 U.V. par PV). Ce rapport est reproductible : même graine, même résultat. **Aucun réglage du jeu n'a été modifié.**

## 0. Verdict

| Critère | Mesure | Résultat |
|---|---|---|
| Profit positif (après salaires, soins et recrues) | B : 74 478 U.V. (744 U.V. par jour) ; C : 1 641 U.V. (149 U.V. par jour) ; C positif sur 4 graines sur 10 | **non respecté** |
| Profit borné (dernière tranche de 30 jours ≤ 1.50 × la deuxième) | B : × 0.28 ; C : partie trop courte | **respecté** |
| Butin qui pèse sans dominer (10 % à 40 % des recettes, moyenne des graines) | moyenne des 10 graines : 13 % (de 0 % à 68 %) ; graine 1 seule : 0 % | **respecté** |
| Aucun profit infini (300 jours : dernier tiers ≤ 1,1 × le tiers du milieu, pas de dépassement) | B : × 1.01 ; C : partie arrêtée avant la fin | **respecté** |

Survie de la boucle complète : 0 partie(s) sur 10 vont au bout des 100 jours sans défaite (une défaite met fin à la campagne, comme dans le jeu).

## 1. Résumé des scénarios

Profit net = argent à la fin - dette - capital de départ : salaires, soins et recrues déjà payés.

| Scénario | Profit net | Par jour | Trajets | Salaires | Soins | Recrues | Butin (part des recettes) | Combats V/D | Groupe final | Fin |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| A. Marchand glouton, sans combat | 77 780 U.V. | 777 U.V. | 58 | 8 930 U.V. | 0 U.V. | 0 U.V. (0) | 0 U.V. (0 %) | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 100 jours écoulés. |
| B. Marchand prévoyant, sans combat | 74 478 U.V. | 744 U.V. | 58 | 8 930 U.V. | 0 U.V. | 0 U.V. (0) | 0 U.V. (0 %) | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 100 jours écoulés. |
| B'. Scénario B rejoué (contrôle de graine) | 74 478 U.V. | 744 U.V. | 58 | 8 930 U.V. | 0 U.V. | 0 U.V. (0) | 0 U.V. (0 %) | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 100 jours écoulés. |
| C. Boucle complète (combats, butin, soin, recrues, salaires) | 1 641 U.V. | 149 U.V. | 8 | 875 U.V. | 0 U.V. | 6 000 U.V. (2) | 0 U.V. (0 %) | 0 / 1 | Def_Fantassin 40/40, Def_Archer 25/25, Def_Fantassin 40/40, Def_Fantassin 40/40 | Défaite en combat sur la route Hautmont > Bourg-Val (fin de la campagne). |
| C'. Scénario C rejoué (contrôle de graine) | 1 641 U.V. | 149 U.V. | 8 | 875 U.V. | 0 U.V. | 6 000 U.V. (2) | 0 U.V. (0 %) | 0 / 1 | Def_Fantassin 40/40, Def_Archer 25/25, Def_Fantassin 40/40, Def_Fantassin 40/40 | Défaite en combat sur la route Hautmont > Bourg-Val (fin de la campagne). |
| E. Départ réaliste (capital = argent du plus petit butin), sans combat | 428 U.V. | 26 U.V. | 5 | 855 U.V. | 0 U.V. | 0 U.V. (0) | 0 U.V. (0 %) | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Bloqué : aucun trajet rentable pendant 10 jours (ville Bourg-Val). |
| E2. Départ réaliste (capital = valeur complète du plus petit butin), sans combat | 73 559 U.V. | 728 U.V. | 61 | 8 930 U.V. | 0 U.V. | 0 U.V. (0) | 0 U.V. (0 %) | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 100 jours écoulés. |
| C sans hôpital | 1 641 U.V. | 149 U.V. | 8 | 875 U.V. | 0 U.V. | 6 000 U.V. (2) | 0 U.V. (0 %) | 0 / 1 | Def_Fantassin 40/40, Def_Archer 25/25, Def_Fantassin 40/40, Def_Fantassin 40/40 | Défaite en combat sur la route Hautmont > Bourg-Val (fin de la campagne). |
| C sans recrutement | 26 067 U.V. | 1 241 U.V. | 16 | 1 425 U.V. | 300 U.V. | 0 U.V. (0) | 5 577 U.V. (20 %) | 1 / 1 | Def_Fantassin 32/40, Def_Archer 25/25 | Défaite en combat sur la route Bourg-Val > Hautmont (fin de la campagne). |
| C sans salaires | 27 782 U.V. | 1 322 U.V. | 15 | 0 U.V. | 700 U.V. | 6 000 U.V. (2) | 8 381 U.V. (24 %) | 2 / 1 | Def_Fantassin 32/40, Def_Archer 25/25, Def_Fantassin 40/40, Def_Fantassin 32/40 | Défaite en combat sur la route Bourg-Val > Hautmont (fin de la campagne). |
| B sans salaires | 83 439 U.V. | 834 U.V. | 58 | 0 U.V. | 0 U.V. | 0 U.V. (0) | 0 U.V. (0 %) | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 100 jours écoulés. |
| B sur 300 jours | 157 742 U.V. | 524 U.V. | 162 | 27 930 U.V. | 0 U.V. | 0 U.V. (0) | 0 U.V. (0 %) | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 300 jours écoulés. |
| C sur 300 jours | 1 641 U.V. | 149 U.V. | 8 | 875 U.V. | 0 U.V. | 6 000 U.V. (2) | 0 U.V. (0 %) | 0 / 1 | Def_Fantassin 40/40, Def_Archer 25/25, Def_Fantassin 40/40, Def_Fantassin 40/40 | Défaite en combat sur la route Hautmont > Bourg-Val (fin de la campagne). |

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

## 5. La boucle complète : acheter, voyager, combattre, se soigner, recruter, payer, vendre (scénario C)

Les combats sont tirés au sort (pas d'arène) : 15 % de chance de rencontre par jour de voyage tant qu'une bande est sur la carte ; chance de victoire 75 % × (PV du groupe / PV de 4 Def_Fantassin en pleine santé) ; une victoire coûte à chaque unité 10 à 50 % de ses PV max (une unité à 0 meurt, un survivant au moins) ; le butin est celui de la table de la bande (U.V. et marchandises ; les marchandises qui ne rentrent pas sont perdues) ; une bande battue revient après le délai des réglages de retour, avec un nouveau butin. En ville, le marchand garde 7 jours de salaires en réserve (sous 2 fois la réserve, il dépense tout : un marchand pauvre doit commercer pour vivre), soigne à l'hôpital (au complet) toute unité sous 60 % de ses PV si la réserve reste couverte, et recrute Def_Fantassin (3 000 U.V.) jusqu'à 4 unités, sans dette et en gardant la réserve.

D'où vient et où va l'argent (graine 1) :

| Poste | C | B (sans combat) |
|---|---:|---:|
| Marge du commerce (ventes - achats, butin vendu compris) | 8 516 U.V. | 83 408 U.V. |
| Butin en U.V. | 0 U.V. | 0 U.V. |
| Salaires payés | -875 U.V. | -8 930 U.V. |
| Hôpital | -0 U.V. | -0 U.V. |
| Recrues | -6 000 U.V. | -0 U.V. |
| Dette restante | -0 U.V. | -0 U.V. |
| **Profit net** | **1 641 U.V.** | **74 478 U.V.** |
| Profit net par jour | 149 U.V. | 744 U.V. |
| Profit par jour avant salaires | 228 U.V. | 834 U.V. |
| Salaires en part de la marge du commerce | 10 % | 11 % |

- Combats : 0 victoire(s), 1 défaite(s) ; unités perdues : 0 ; butin ramassé : 0 U.V. (perdu faute de place : 0 U.V.), soit 0 % des recettes.
- Soins : 0 PV rendus par la régénération, 0 PV à l'hôpital (0 visite(s), 0 U.V.).
- Recrues : 2 (6 000 U.V.) ; recrutement refusé pour dette : 0 fois ; plus grosse dette : 0 U.V..
- Marchandises restées en cargaison à la fin (au prix de base) : 5 600 U.V. ; moyenne sur les 10 graines : 7 270 U.V. (butin qu'aucune ville du chemin n'achetait, ou cargaison du trajet perdu).
- Groupe à la fin : Def_Fantassin 40/40, Def_Archer 25/25, Def_Fantassin 40/40, Def_Fantassin 40/40. Fin : Défaite en combat sur la route Hautmont > Bourg-Val (fin de la campagne).

Trajets avec un combat (graine 1) :

| Jour | Trajet | Résultat | Valeur du butin | Profit (vente du butin comprise) |
|---:|---|---|---:|---:|
| 9 | Hautmont > Bourg-Val (Fer) | Défaite | 0 U.V. | -5 060 U.V. |

Effet de la chance (même scénario, 10 graines) :

| Graine | Profit net | Par jour | Jours | V / D | Unités perdues | Butin (part) | Salaires | Hôpital | Recrues | Groupe final | Fin |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| 1 | 1 641 U.V. | 149 U.V. | 11 | 0 / 1 | 0 | 0 U.V. (0 %) | 875 U.V. | 0 U.V. | 6 000 U.V. | Def_Fantassin 40/40, Def_Archer 25/25, Def_Fantassin 40/40, Def_Fantassin 40/40 | Défaite en combat sur la route Hautmont > Bourg-Val (fin de la campagne). |
| 2 | -6 569 U.V. | -1 313 U.V. | 5 | 1 / 1 | 0 | 5 117 U.V. (0 %) | 0 U.V. | 450 U.V. | 6 000 U.V. | Def_Fantassin 35/40, Def_Archer 20/25, Def_Fantassin 32/40, Def_Fantassin 40/40 | Défaite en combat sur la route Port-Brume > Bourg-Val (fin de la campagne). |
| 3 | 33 006 U.V. | 1 737 U.V. | 19 | 1 / 1 | 0 | 11 004 U.V. (26 %) | 2 275 U.V. | 450 U.V. | 6 000 U.V. | Def_Fantassin 40/40, Def_Archer 22/25, Def_Fantassin 32/40, Def_Fantassin 32/40 | Défaite en combat sur la route Hautmont > Bourg-Val (fin de la campagne). |
| 4 | -8 639 U.V. | -8 639 U.V. | 1 | 0 / 1 | 0 | 0 U.V. (0 %) | 0 U.V. | 0 U.V. | 6 000 U.V. | Def_Fantassin 40/40, Def_Archer 25/25, Def_Fantassin 40/40, Def_Fantassin 40/40 | Défaite en combat sur la route Aubeterre > Hautmont (fin de la campagne). |
| 5 | 31 632 U.V. | 2 108 U.V. | 15 | 3 / 1 | 0 | 16 294 U.V. (39 %) | 1 575 U.V. | 2 075 U.V. | 6 000 U.V. | Def_Fantassin 34/40, Def_Archer 25/25, Def_Fantassin 35/40, Def_Fantassin 40/40 | Défaite en combat sur la route Bourg-Val > Hautmont (fin de la campagne). |
| 6 | -6 986 U.V. | -2 328 U.V. | 3 | 1 / 1 | 0 | 4 728 U.V. (0 %) | 0 U.V. | 450 U.V. | 6 000 U.V. | Def_Fantassin 40/40, Def_Archer 20/25, Def_Fantassin 32/40, Def_Fantassin 32/40 | Défaite en combat sur la route Hautmont > Bourg-Val (fin de la campagne). |
| 7 | -2 721 U.V. | -453 U.V. | 6 | 0 / 1 | 0 | 0 U.V. (0 %) | 0 U.V. | 0 U.V. | 6 000 U.V. | Def_Fantassin 40/40, Def_Archer 25/25, Def_Fantassin 40/40, Def_Fantassin 40/40 | Défaite en combat sur la route Hautmont > Aubeterre (fin de la campagne). |
| 8 | -8 474 U.V. | -2 824 U.V. | 3 | 0 / 1 | 0 | 0 U.V. (0 %) | 0 U.V. | 0 U.V. | 6 000 U.V. | Def_Fantassin 40/40, Def_Archer 25/25, Def_Fantassin 40/40, Def_Fantassin 40/40 | Défaite en combat sur la route Hautmont > Bourg-Val (fin de la campagne). |
| 9 | -7 937 U.V. | -1 587 U.V. | 5 | 1 / 1 | 0 | 6 604 U.V. (0 %) | 0 U.V. | 475 U.V. | 6 000 U.V. | Def_Fantassin 28/40, Def_Archer 18/25, Def_Fantassin 40/40, Def_Fantassin 28/40 | Défaite en combat sur la route Port-Brume > Bourg-Val (fin de la campagne). |
| 10 | 9 838 U.V. | 1 639 U.V. | 6 | 2 / 1 | 0 | 11 621 U.V. (68 %) | 0 U.V. | 1 225 U.V. | 6 000 U.V. | Def_Fantassin 40/40, Def_Archer 25/25, Def_Fantassin 28/40, Def_Fantassin 40/40 | Défaite en combat sur la route Hautmont > Aubeterre (fin de la campagne). |

Parties perdues en combat : 10 sur 10. Profit net : minimum -8 639 U.V., moyenne 3 479 U.V., maximum 33 006 U.V.. Part du butin : de 0 % à 68 %.

## 5 bis. Le départ de la partie manuelle

La caravane démarre avec **0 U.V.** : sans argent on ne peut rien acheter, la première somme vient du butin d'une bande (une partie en U.V., le reste en marchandises à revendre). Butin des bandes de la carte (valeur totale tirée dans la table de la bande, U.V. et marchandises au prix de base) : 1 500 U.V. à 3 000 U.V., 2 500 U.V. à 4 500 U.V., 3 000 U.V. à 6 000 U.V., 4 000 U.V. à 8 000 U.V., 4 500 U.V. à 8 500 U.V., 8 000 U.V. à 16 000 U.V.. La recrue la moins chère coûte 3 000 U.V.. Les salaires du groupe de départ courent dès la fin des jours offerts.

Scénario E : un marchand prévoyant qui part avec la seule part en U.V. du plus petit butin (600 U.V.) finit à 1 028 U.V. en 16 jours (5 trajets, 855 U.V. de salaires payés, plus grosse dette 0 U.V.) ; il double son capital au jour 6. Bloqué : aucun trajet rentable pendant 10 jours (ville Bourg-Val).

Scénario E2 : le même, en comptant aussi les marchandises de ce butin comme de l'argent (1 500 U.V.) finit à 75 059 U.V. en 101 jours (61 trajets, 8 930 U.V. de salaires payés, plus grosse dette 0 U.V.) ; il double son capital au jour 12. Terminé : 100 jours écoulés.

## 5 ter. Effet de chaque mécanique (une seule enlevée à la fois, même graine)

Enlever une mécanique change aussi les décisions et les tirages qui suivent (un autre trajet, un autre combat) : un écart de signe inattendu sur une seule graine n'est pas forcément une erreur.

| Comparaison | Profit net | Écart | Salaires | Hôpital | Recrues | V / D | Groupe final | Fin |
|---|---:|---:|---:|---:|---:|---:|---|---|
| C. Boucle complète (combats, butin, soin, recrues, salaires) | 1 641 U.V. | référence | 875 U.V. | 0 U.V. | 6 000 U.V. | 0 / 1 | Def_Fantassin 40/40, Def_Archer 25/25, Def_Fantassin 40/40, Def_Fantassin 40/40 | Défaite en combat sur la route Hautmont > Bourg-Val (fin de la campagne). |
| C sans hôpital | 1 641 U.V. | 0 U.V. | 875 U.V. | 0 U.V. | 6 000 U.V. | 0 / 1 | Def_Fantassin 40/40, Def_Archer 25/25, Def_Fantassin 40/40, Def_Fantassin 40/40 | Défaite en combat sur la route Hautmont > Bourg-Val (fin de la campagne). |
| C sans recrutement | 26 067 U.V. | 24 426 U.V. | 1 425 U.V. | 300 U.V. | 0 U.V. | 1 / 1 | Def_Fantassin 32/40, Def_Archer 25/25 | Défaite en combat sur la route Bourg-Val > Hautmont (fin de la campagne). |
| C sans salaires | 27 782 U.V. | 26 141 U.V. | 0 U.V. | 700 U.V. | 6 000 U.V. | 2 / 1 | Def_Fantassin 32/40, Def_Archer 25/25, Def_Fantassin 40/40, Def_Fantassin 32/40 | Défaite en combat sur la route Bourg-Val > Hautmont (fin de la campagne). |
| B. Marchand prévoyant, sans combat | 74 478 U.V. | référence | 8 930 U.V. | 0 U.V. | 0 U.V. | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 100 jours écoulés. |
| B sans salaires | 83 439 U.V. | 8 961 U.V. | 0 U.V. | 0 U.V. | 0 U.V. | 0 / 0 | Def_Fantassin 40/40, Def_Archer 25/25 | Terminé : 100 jours écoulés. |

Sans combat, les salaires coûtent 8 961 U.V. de profit sur la partie (8 930 U.V. payés ; l'écart comprend aussi la réserve qui réduit les achats).

## 5 quinquies. Sensibilité à la chance de victoire (hypothèse du simulateur)

La survie dépend presque entièrement de la chance de gagner un combat, qui est une hypothèse (les vrais combats tactiques ne sont pas joués). La boucle complète est rejouée sur les mêmes 10 graines avec d'autres chances de victoire pour un groupe complet :

| Victoire (groupe complet) | Parties menées au bout | Profit net moyen | Profit net médian | Profit par jour moyen | Part du butin moyenne | Combats gagnés par partie |
|---:|---:|---:|---:|---:|---:|---:|
| 75 % (réglage actuel) | 0 / 10 | 3 479 U.V. | -2 721 U.V. | -1 151 U.V. | 13 % | 0.9 |
| 85 % | 0 / 10 | 20 543 U.V. | 9 838 U.V. | -61 U.V. | 23 % | 2.5 |
| 95 % | 1 / 10 | 36 755 U.V. | 25 022 U.V. | 171 U.V. | 28 % | 4.7 |

## 5 quater. Parties de 300 jours : le profit s'arrête-t-il de croître ?

- B sur 300 jours : profit net 157 742 U.V. en 301 jours (524 U.V. par jour). Profit par tranche de 30 jours : 38 779 U.V., 14 896 U.V., 4 118 U.V., 13 135 U.V., 19 924 U.V., 12 995 U.V., 3 978 U.V., 12 276 U.V., 13 135 U.V., 20 064 U.V.. Rapport dernier tiers / tiers du milieu : × 1.01. Terminé : 300 jours écoulés.
- C sur 300 jours : profit net 1 641 U.V. en 11 jours (149 U.V. par jour). Profit par tranche de 30 jours : partie trop courte. Rapport dernier tiers / tiers du milieu : partie trop courte. Défaite en combat sur la route Hautmont > Bourg-Val (fin de la campagne).

## 6. Anomalies

- Marchandise(s) jamais rentable(s) en transport : Blé, Argent.
- Marge théorique maximale d'une unité (vendre au plafond, acheter au plancher) : 262 % du prix d'achat, au-dessus du seuil de 150 % : les bornes de prix laissent de la place à de très grosses marges.
- **Départ difficile** : avec la seule part en U.V. du plus petit butin (600 U.V.), le marchand simulé finit à 428 U.V. de profit net (Bloqué : aucun trajet rentable pendant 10 jours (ville Bourg-Val).).

## 7. Protocole de test automatique (refait à chaque rapport)

| Contrôle | Résultat | Détail |
|---|---|---|
| T2-T3. Grand livre et invariants à chaque étape (solde = capital + ventes - achats + butin - salaires - soins - recrues ; dette ≥ 0 ; groupe et PV possibles ; cargaison ; stocks ; salaires payés + dette = salaires dus) | réussi | 0 écart(s) sur 22 parties |
| T4. Déterminisme du commerce (B et B' même graine) | réussi | 58 trajets comparés |
| T4. Déterminisme de la boucle complète (C et C' : trajets, combats, salaires, soins, recrues, groupe final) | réussi | 8 trajets, 0 victoires comparés |
| Jours offerts : le premier jour payé est le jour 8 de la partie | réussi | 13 parties avec salaires, premier jour payé : 8 |
| T5. Sans salaires : rien n'est payé, aucune dette | réussi | 2 parties |
| T5. Sans combat : aucun butin, aucun soin, aucune recrue, groupe intact | réussi | 7 parties |
| T5. Sans hôpital : aucun soin payé | réussi | hôpital 0 U.V. |
| T5. Sans recrutement : aucune recrue | réussi | 0 recrue(s) |
| T5. Les salaires coûtent (B sans salaires gagne plus que B) | réussi | 83 439 U.V. contre 74 478 U.V. |
| T5. Recrutement jamais avec une dette (règle du jeu) | réussi | 0 refus pour dette au total |
| Aucune vente à une ville hors de son profil (une ville n'achète que ce qu'elle produit ou consomme) | réussi | 0 trajet(s) hors profil sur 571 |
| T6. Pas de dépassement (soldes bien sous la limite des entiers) | réussi | solde le plus haut : 167 742 U.V. |

Les tests des règles pures (T1), de non-régression du jeu (T7) et la relecture des chiffres (T8) sont faits à part, en Play Mode : voir `PROJECT_LOG.md`.

## 8. Réglages à ajuster (suggestions, rien n'a été changé)

Valeurs actuelles : écart acheteur/vendeur 10 %, facteur de prix de ×0.5 à ×2, capital de départ simulé 10 000 U.V..

Suggestions tirées des mesures :

- **Survie** : 10 parties sur 10 perdues en combat, après 1.9 combat(s) en moyenne. Le chiffre dépend des hypothèses du simulateur (victoire 75 % pour un groupe complet, voir § 5 quinquies), mais la cause est structurelle : une rencontre toutes les quelques journées de voyage et **une seule défaite qui met fin à la campagne** ; même à 95 % de victoire, une partie de 100 jours va rarement au bout. Pistes côté jeu : qu'une défaite coûte (une part de la cargaison et de l'argent, des blessés) sans finir la partie ; ou moins de rencontres (bandes moins nombreuses, plus lentes, évitables) ; ou un groupe de départ plus solide.
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
- Les combats sont un modèle tiré au sort (rencontre, victoire selon les PV du groupe, dégâts d'une victoire) dont les valeurs sont des hypothèses de `SimulationSettings`, pas des mesures : le taux de défaites en dépend entièrement. Les vrais combats ne sont pas joués, la composition des bandes n'entre pas en compte.
- Une défaite met fin à la campagne (comme dans le jeu aujourd'hui) : les moyennes sur les graines mélangent des parties complètes et des parties courtes. La cargaison du trajet perdu est achetée mais jamais vendue : une partie courte peut afficher une marge du commerce négative, et une part du butin sans signification (« commerce à perte »).
- La part du butin est une estimation : le butin est compté au prix de base, alors que ses marchandises sont vendues au prix du marché avec la cargaison (elles sont aussi dans la marge du commerce).
- Les règles du marchand (réserve de 7 jours de salaires, hôpital sous 60 %, recrues jusqu'à 4) sont simples et fixes ; un joueur peut décider autrement.
- Les bandes sont rencontrées au hasard sur la route ; leurs patrouilles et leur errance réelles ne sont pas modélisées.
- Le temps passé en ville est nul (comme dans le jeu) ; l'hôpital et le recrutement se paient directement, sans passer par leurs fenêtres.
- Les durées de trajet sont celles du chemin NavMesh à vitesse de base ralentie par les terrains traversés ; un détour décidé par le joueur n'est pas simulé.
- Toutes les parties s'enchaînent sur le vrai calendrier (en double précision) : le rapport est identique d'une exécution à l'autre, quel que soit le jour où elle commence.

