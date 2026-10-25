# Rapport d'équilibrage économique (étape E9)

Simulation sans rendu de **100 jours**, graine **1**, capital de départ **10 000 U.V.**, 5 villes, 10 marchandises. Le marchand achète et vend uniquement par `TradeService`, avec le calendrier et les durées de trajet réelles de la carte (chemin NavMesh, terrains compris). Ce rapport est reproductible : même graine, même résultat.

## 1. Résumé des scénarios

| Scénario | Profit net | Profit / jour | Trajets | Profit / trajet | Jours d'attente | Combats (V/D) | Fin |
|---|---:|---:|---:|---:|---:|---:|---|
| A. Marchand glouton, sans combat | 199 421 U.V. | 1 974 U.V. | 93 | 2 144 U.V. | 0 | 0 / 0 | Terminé : 100 jours écoulés. |
| B. Marchand prévoyant, sans combat | 160 683 U.V. | 1 590 U.V. | 95 | 1 691 U.V. | 0 | 0 / 0 | Terminé : 100 jours écoulés. |
| B'. Même scénario B rejoué (contrôle de graine) | 160 683 U.V. | 1 590 U.V. | 95 | 1 691 U.V. | 0 | 0 / 0 | Terminé : 100 jours écoulés. |
| C. Marchand prévoyant, avec combats | 20 858 U.V. | 1 226 U.V. | 14 | 1 489 U.V. | 0 | 3 / 1 | Défaite en combat sur la route Bourg-Val > Port-Brume (fin de la campagne). |
| E. Départ réaliste (capital = argent du plus petit butin), sans combat | 469 U.V. | 24 U.V. | 8 | 58 U.V. | 11 | 0 / 0 | Bloqué : aucun trajet rentable pendant 10 jours (ville Bourg-Val). |
| E2. Départ réaliste (capital = valeur complète du plus petit butin), sans combat | 158 667 U.V. | 1 570 U.V. | 95 | 1 670 U.V. | 0 | 0 / 0 | Terminé : 100 jours écoulés. |

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
| Huile d'olive | 42 | 92 | 306 936 U.V. | 373 530 U.V. | 66 594 U.V. | 22 % |
| Épices | 3 | 7 | 117 601 U.V. | 183 967 U.V. | 66 366 U.V. | 56 % |
| Cuir | 25 | 100 | 139 983 U.V. | 156 350 U.V. | 16 367 U.V. | 12 % |
| Outils | 3 | 14 | 38 871 U.V. | 49 101 U.V. | 10 230 U.V. | 26 % |
| Vin | 19 | 79 | 38 376 U.V. | 43 693 U.V. | 5 317 U.V. | 14 % |
| Tissu de laine | 2 | 7 | 16 016 U.V. | 18 889 U.V. | 2 873 U.V. | 18 % |
| Fer | 1 | 11 | 6 803 U.V. | 9 239 U.V. | 2 436 U.V. | 36 % |

Par route (ville de départ > ville d'arrivée) :

| Route | Trajets | Profit | Profit / jour de trajet |
|---|---:|---:|---:|
| Port-Brume > Hautmont | 2 | 62 279 U.V. | 30 681 U.V. |
| Hautmont > Aubeterre | 23 | 38 059 U.V. | 1 677 U.V. |
| Bourg-Val > Aubeterre | 22 | 37 125 U.V. | 1 581 U.V. |
| Aubeterre > Bourg-Val | 23 | 14 612 U.V. | 595 U.V. |
| Aubeterre > Hautmont | 21 | 6 871 U.V. | 331 U.V. |
| Bourg-Val > Hautmont | 1 | 6 212 U.V. | 3 223 U.V. |
| Hautmont > Bourg-Val | 1 | 2 436 U.V. | 1 263 U.V. |
| Bourg-Val > Port-Brume | 1 | 2 009 U.V. | 2 148 U.V. |
| Aubeterre > Port-Brume | 1 | 580 U.V. | 292 U.V. |

Trajets (les 12 premiers et les 4 derniers) :

| Jour | Trajet | Marchandise | Quantité | Coût | Recette | Profit | Profit prévu |
|---:|---|---|---:|---:|---:|---:|---:|
| 0 | Aubeterre > Bourg-Val | Cuir | 7 | 9 926 U.V. | 12 165 U.V. | 2 239 U.V. | 2 239 U.V. |
| 1 | Bourg-Val > Aubeterre | Huile d'olive | 3 | 9 198 U.V. | 12 585 U.V. | 3 387 U.V. | 3 387 U.V. |
| 2 | Aubeterre > Hautmont | Vin | 7 | 3 366 U.V. | 4 291 U.V. | 925 U.V. | 925 U.V. |
| 3 | Hautmont > Bourg-Val | Fer | 11 | 6 803 U.V. | 9 239 U.V. | 2 436 U.V. | 2 436 U.V. |
| 5 | Bourg-Val > Hautmont | Outils | 6 | 16 443 U.V. | 22 655 U.V. | 6 212 U.V. | 6 212 U.V. |
| 7 | Hautmont > Aubeterre | Huile d'olive | 3 | 10 248 U.V. | 12 585 U.V. | 2 337 U.V. | 2 337 U.V. |
| 8 | Aubeterre > Hautmont | Cuir | 4 | 5 435 U.V. | 6 610 U.V. | 1 175 U.V. | 1 175 U.V. |
| 9 | Hautmont > Aubeterre | Huile d'olive | 2 | 7 014 U.V. | 8 025 U.V. | 1 011 U.V. | 1 011 U.V. |
| 10 | Aubeterre > Bourg-Val | Cuir | 3 | 4 254 U.V. | 4 702 U.V. | 448 U.V. | 448 U.V. |
| 11 | Bourg-Val > Aubeterre | Tissu de laine | 5 | 10 787 U.V. | 13 281 U.V. | 2 494 U.V. | 2 494 U.V. |
| 12 | Aubeterre > Hautmont | Tissu de laine | 2 | 5 229 U.V. | 5 608 U.V. | 379 U.V. | 379 U.V. |
| 13 | Hautmont > Aubeterre | Huile d'olive | 3 | 10 248 U.V. | 12 585 U.V. | 2 337 U.V. | 2 337 U.V. |
| ... | | | | | | | |
| 96 | Aubeterre > Bourg-Val | Cuir | 4 | 5 593 U.V. | 6 174 U.V. | 581 U.V. | 581 U.V. |
| 97 | Bourg-Val > Aubeterre | Huile d'olive | 2 | 6 636 U.V. | 8 025 U.V. | 1 389 U.V. | 1 389 U.V. |
| 98 | Aubeterre > Hautmont | Vin | 4 | 1 945 U.V. | 2 189 U.V. | 244 U.V. | 244 U.V. |
| 99 | Hautmont > Aubeterre | Huile d'olive | 2 | 6 636 U.V. | 8 025 U.V. | 1 389 U.V. | 1 389 U.V. |

## 4. Évolution des prix (scénario B)

Prix médian (sans écart acheteur/vendeur) : moyenne, minimum et maximum sur la durée, par ville. `profil` = la ville produit ou consomme la marchandise ; `hors profil` = elle l'achète seulement, son stock tend vers 0. Entre parenthèses : part des jours au prix plancher (×0.5) / plafond (×2).

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
| Aubeterre | profil | 448 U.V. | 416 U.V. | 478 U.V. | 24-30 | 0 % / 0 % | 0 % |
| Hautmont | profil | 592 U.V. | 542 U.V. | 739 U.V. | 6-13 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 613 U.V. | 613 U.V. | 613 U.V. | 10-10 | 0 % / 0 % | 0 % |
| Carrefour | profil | 520 U.V. | 520 U.V. | 520 U.V. | 14-14 | 0 % / 0 % | 0 % |

**Huile d'olive** (prix de base 2 400 U.V.)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 1 920 U.V. | 1 920 U.V. | 1 920 U.V. | 24-24 | 0 % / 0 % | 0 % |
| Aubeterre | hors profil | 4 332 U.V. | 4 032 U.V. | 4 800 U.V. | 0-2 | 0 % / 19 % | 19 % |
| Hautmont | profil | 2 987 U.V. | 2 920 U.V. | 3 640 U.V. | 4-8 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 2 954 U.V. | 2 640 U.V. | 3 240 U.V. | 6-10 | 0 % / 0 % | 0 % |
| Carrefour | profil | 2 400 U.V. | 2 400 U.V. | 2 400 U.V. | 10-10 | 0 % / 0 % | 0 % |

**Tissu de laine** (prix de base 2 400 U.V.)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 2 400 U.V. | 2 400 U.V. | 2 400 U.V. | 10-10 | 0 % / 0 % | 0 % |
| Aubeterre | profil | 2 667 U.V. | 2 580 U.V. | 3 540 U.V. | 3-7 | 0 % / 0 % | 0 % |
| Hautmont | profil | 2 750 U.V. | 2 688 U.V. | 3 720 U.V. | 3-8 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 2 072 U.V. | 1 848 U.V. | 2 093 U.V. | 33-40 | 0 % / 0 % | 0 % |
| Carrefour | profil | 2 400 U.V. | 2 400 U.V. | 2 400 U.V. | 12-12 | 0 % / 0 % | 0 % |

**Cuir** (prix de base 1 500 U.V.)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 1 661 U.V. | 1 564 U.V. | 2 132 U.V. | 6-13 | 0 % / 0 % | 0 % |
| Aubeterre | profil | 1 292 U.V. | 1 200 U.V. | 1 388 U.V. | 19-24 | 0 % / 0 % | 0 % |
| Hautmont | profil | 1 543 U.V. | 1 500 U.V. | 2 175 U.V. | 4-10 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 1 642 U.V. | 1 550 U.V. | 2 108 U.V. | 8-17 | 0 % / 0 % | 0 % |
| Carrefour | profil | 1 500 U.V. | 1 500 U.V. | 1 500 U.V. | 10-10 | 0 % / 0 % | 0 % |

**Fer** (prix de base 700 U.V.)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 854 U.V. | 852 U.V. | 1 003 U.V. | 5-8 | 0 % / 0 % | 0 % |
| Aubeterre | profil | 890 U.V. | 889 U.V. | 1 015 U.V. | 4-6 | 0 % / 0 % | 0 % |
| Hautmont | profil | 554 U.V. | 539 U.V. | 616 U.V. | 39-48 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 947 U.V. | 753 U.V. | 1 076 U.V. | 5-14 | 0 % / 0 % | 0 % |
| Carrefour | profil | 700 U.V. | 700 U.V. | 700 U.V. | 10-10 | 0 % / 0 % | 0 % |

**Outils** (prix de base 3 000 U.V.)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 3 144 U.V. | 3 000 U.V. | 4 350 U.V. | 4-10 | 0 % / 0 % | 0 % |
| Aubeterre | profil | 3 184 U.V. | 3 000 U.V. | 4 650 U.V. | 3-10 | 0 % / 0 % | 0 % |
| Hautmont | profil | 3 719 U.V. | 3 450 U.V. | 4 550 U.V. | 4-9 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 2 523 U.V. | 2 400 U.V. | 2 700 U.V. | 25-30 | 0 % / 0 % | 0 % |
| Carrefour | profil | 3 000 U.V. | 3 000 U.V. | 3 000 U.V. | 8-8 | 0 % / 0 % | 0 % |

**Épices** (prix de base 15 000 U.V.)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 13 834 U.V. | 11 250 U.V. | 15 000 U.V. | 6-10 | 0 % / 0 % | 0 % |
| Aubeterre | hors profil | 30 000 U.V. | 30 000 U.V. | 30 000 U.V. | 0-0 | 0 % / 100 % | 100 % |
| Hautmont | hors profil | 29 904 U.V. | 25 200 U.V. | 30 000 U.V. | 0-2 | 0 % / 97 % | 97 % |
| Bourg-Val | profil | 22 750 U.V. | 22 750 U.V. | 22 750 U.V. | 2-2 | 0 % / 0 % | 0 % |
| Carrefour | profil | 15 000 U.V. | 15 000 U.V. | 15 000 U.V. | 4-4 | 0 % / 0 % | 0 % |

**Argent** (prix de base 23 000 U.V., cours fixe)

| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |
|---|---|---:|---:|---:|---:|---:|---:|
| Port-Brume | profil | 23 000 U.V. | 23 000 U.V. | 23 000 U.V. | 5-6 | 0 % / 0 % | 0 % |
| Aubeterre | profil | 23 000 U.V. | 23 000 U.V. | 23 000 U.V. | 6-6 | 0 % / 0 % | 0 % |
| Hautmont | profil | 23 000 U.V. | 23 000 U.V. | 23 000 U.V. | 12-20 | 0 % / 0 % | 0 % |
| Bourg-Val | profil | 23 000 U.V. | 23 000 U.V. | 23 000 U.V. | 3-3 | 0 % / 0 % | 0 % |
| Carrefour | profil | 23 000 U.V. | 23 000 U.V. | 23 000 U.V. | 6-6 | 0 % / 0 % | 0 % |

## 5. La boucle complète : acheter, voyager, combattre, revenir, vendre (scénario C)

Les combats sont tirés au sort (pas d'arène) : chance de rencontre par jour de voyage, chance de victoire réduite quand le groupe est amoindri, butin de chaque bande de la carte (tiré dans sa table, U.V. et marchandises ; une bande battue revient après le délai des réglages de retour avec un nouveau butin ; les marchandises qui ne rentrent pas dans la cargaison sont perdues, les plus chères entrent d'abord), unités perdues rachetées à la ville suivante au prix de la moins chère des recrues.

- Profit net : **20 858 U.V.** en 17 jours (sans combat : 160 683 U.V.).
- Combats : 3 victoire(s), 1 défaite(s) ; unités perdues : 2 ; valeur du butin ramassé : 13 767 U.V. (perdu faute de place : 0 U.V.) ; coût des remplacements : 6 000 U.V..
- Salaires payés : 1 520 U.V. (sans combat : 9 500 U.V.) ; dette restante : 0 U.V. (déjà retirée du profit net).
- Fin : Défaite en combat sur la route Bourg-Val > Port-Brume (fin de la campagne).

Trajets avec un combat (graine 1) : 3 victoire(s), 2 unité(s) perdue(s), butin 13 767 U.V., remplacements 6 000 U.V..

| Jour | Trajet | Résultat | Valeur du butin | Profit (vente du butin comprise) |
|---:|---|---|---:|---:|
| 5 | Bourg-Val > Hautmont (Outils) | Victoire | 6 729 U.V. | 8 401 U.V. |
| 7 | Hautmont > Aubeterre (Huile d'olive) | Victoire | 1 652 U.V. | 2 337 U.V. |
| 14 | Aubeterre > Bourg-Val (Cuir) | Victoire, 2 unité(s) perdue(s) | 5 386 U.V. | 5 164 U.V. |
| 15 | Bourg-Val > Port-Brume (Outils) | Défaite | 0 U.V. | -10 962 U.V. |

Effet de la chance (même scénario, 10 graines) :

| Graine | Profit net | Jours | Victoires | Défaites | Unités perdues | Valeur du butin | Butin perdu | Remplacements | Fin |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| 1 | 20 858 U.V. | 17 | 3 | 1 | 2 | 13 767 U.V. | 0 U.V. | 6 000 U.V. | Défaite en combat sur la route Bourg-Val > Port-Brume (fin de la campagne). |
| 2 | 4 478 U.V. | 9 | 1 | 1 | 0 | 5 117 U.V. | 0 U.V. | 0 U.V. | Défaite en combat sur la route Bourg-Val > Aubeterre (fin de la campagne). |
| 3 | 57 117 U.V. | 21 | 3 | 1 | 4 | 16 114 U.V. | 0 U.V. | 12 000 U.V. | Défaite en combat sur la route Bourg-Val > Aubeterre (fin de la campagne). |
| 4 | -8 602 U.V. | 5 | 1 | 1 | 2 | 2 209 U.V. | 0 U.V. | 6 000 U.V. | Défaite en combat sur la route Hautmont > Aubeterre (fin de la campagne). |
| 5 | 22 339 U.V. | 12 | 4 | 1 | 5 | 25 179 U.V. | 0 U.V. | 15 000 U.V. | Défaite en combat sur la route Aubeterre > Bourg-Val (fin de la campagne). |
| 6 | -9 278 U.V. | 7 | 1 | 1 | 2 | 4 728 U.V. | 0 U.V. | 6 000 U.V. | Défaite en combat sur la route Bourg-Val > Hautmont (fin de la campagne). |
| 7 | -8 121 U.V. | 7 | 0 | 1 | 0 | 0 U.V. | 0 U.V. | 0 U.V. | Défaite en combat sur la route Bourg-Val > Hautmont (fin de la campagne). |
| 8 | -7 149 U.V. | 3 | 0 | 1 | 0 | 0 U.V. | 0 U.V. | 0 U.V. | Défaite en combat sur la route Bourg-Val > Aubeterre (fin de la campagne). |
| 9 | -8 356 U.V. | 10 | 2 | 1 | 4 | 9 960 U.V. | 0 U.V. | 12 000 U.V. | Défaite en combat sur la route Bourg-Val > Hautmont (fin de la campagne). |
| 10 | 26 598 U.V. | 12 | 2 | 1 | 0 | 11 621 U.V. | 0 U.V. | 0 U.V. | Défaite en combat sur la route Aubeterre > Bourg-Val (fin de la campagne). |

Parties perdues en combat : 10 sur 10. Profit net : minimum -9 278 U.V., moyenne 8 988 U.V., maximum 57 117 U.V..

## 5 bis. Le départ de la partie manuelle

La caravane démarre avec **0 U.V.** : sans argent on ne peut rien acheter, la première somme vient du butin d'une bande (une partie en U.V., le reste en marchandises à revendre). Butin des bandes de la carte (valeur totale tirée dans la table de la bande, U.V. et marchandises au prix de base) : 1 500 U.V. à 3 000 U.V., 2 500 U.V. à 4 500 U.V., 3 000 U.V. à 6 000 U.V., 4 000 U.V. à 8 000 U.V., 4 500 U.V. à 8 500 U.V., 8 000 U.V. à 16 000 U.V.. La recrue la moins chère coûte 3 000 U.V..

Scénario E : un marchand prévoyant qui part avec la seule part en U.V. du plus petit butin (600 U.V.) finit à 1 069 U.V. en 19 jours (8 trajets) ; il double son capital au jour 10. Bloqué : aucun trajet rentable pendant 10 jours (ville Bourg-Val).

Scénario E2 : le même, en comptant aussi les marchandises de ce butin comme de l'argent (1 500 U.V.) finit à 160 167 U.V. en 101 jours (95 trajets) ; il double son capital au jour 7. Terminé : 100 jours écoulés.

## 6. Anomalies

- **Profit concentré** : les 3 meilleurs trajets sur 95 font 40 % du profit total (68 491 U.V. sur 170 183 U.V.), seuil 30 % : une seule route écrase les autres.
- Prix au plafond car stock vide (hors profil) : Épices à Aubeterre (100 % des jours).
- Prix au plafond car stock vide (hors profil) : Épices à Hautmont (97 % des jours).
- Marchandise(s) jamais rentable(s) en transport : Blé, Argent.
- Marge théorique maximale d'une unité (vendre au plafond, acheter au plancher) : 262 % du prix d'achat, au-dessus du seuil de 150 % : les bornes de prix laissent de la place à de très grosses marges.

## 7. Paramètres à ajuster d'après ce rapport

Valeurs actuelles : écart acheteur/vendeur 10 %, facteur de prix de ×0.5 à ×2, capital de départ simulé 10 000 U.V..

| Paramètre | Où | Effet sur le rapport |
|---|---|---|
| Facteurs minimum et maximum, courbe de prix | `PricingSettings` | Bornent la marge d'une unité et le rendement d'un trajet. |
| Écart acheteur/vendeur (`spreadPercent`) | `PricingSettings` | Coût fixe de chaque aller-retour ; plus il est grand, moins les petites routes rapportent. |
| Retour vers la cible (`reversionPercent`) | `EconomySettings` | Vitesse à laquelle un marché que l'on a vidé ou inondé retrouve son prix : plus il est lent, plus on épuise une route. |
| Production, consommation et stock cible par marchandise | `TownDefinition` (5 villes) | Fixent quelles routes existent et leur profondeur. |
| Stock cible par défaut des marchandises hors profil | `TownDefinition.defaultTargetStock` | Fixe à quel point une ville qui n'en a pas paie cher ce qu'on lui apporte. |
| Masse et volume maximaux de la cargaison | `CargoCapacity_Caravan` | Plafond du profit par trajet : c'est lui qui empêche un profit sans fin. |
| Prix de base, masse et volume de chaque marchandise | `GoodDefinition` | Valeur transportée par kilo et par litre, donc rentabilité relative. |
| Minutes par mètre | `CampaignClock` | Durée des trajets, donc profit par jour. |
| Butin des bandes, prix des recrues | `PartyRoster` (scène), `RecruitmentSettings` | Poids des combats par rapport au commerce. |

Pistes déduites des anomalies ci-dessus :

- Les villes paient cher ce qu'elles n'ont pas : relever `defaultTargetStock` des profils (ou borner le prix d'une marchandise hors profil) pour qu'elles ne soient pas l'unique source de marge.
- Rapprocher les profils des villes concernées (une ville qui produit ce que l'autre consomme) ou réduire l'écart acheteur/vendeur.
- Resserrer les bornes de prix (`minFactor` plus haut, `maxFactor` plus bas) ou élargir l'écart acheteur/vendeur, pour réduire la marge maximale d'un trajet.

## 8. Butin en marchandises (étape F1, en jeu)

- Chaque bande a une table de butin (`LootTable`) : une valeur totale entre un minimum et un maximum, répartie au hasard entre les U.V. et des marchandises selon des poids ; tirée une fois à la création de la bande (`BandLootSetup`, `LootRoller`).
- Les marchandises entrent dans la cargaison à la victoire, les plus chères d'abord, dans la limite de la masse et du volume ; le reste est perdu et dit à l'écran de fin de combat.
- Elles se vendent ensuite aux prix du marché comme toute cargaison. Le scénario C les simule avec le même tirage et la même règle de place.
- Garde-fou : la valeur totale d'un butin reste dans l'ordre de grandeur de la table de la bande ; à comparer, dans ce rapport, au profit du commerce.

## 9. Limites de cette simulation

- Le marchand simulé est un optimiseur à courte vue (le meilleur profit par jour du prochain trajet) : un humain peut faire mieux (enchaîner des routes) ou moins bien.
- Les combats sont un modèle tiré au sort (chance de rencontre, chance de victoire, pertes) dont les valeurs sont des hypothèses de `SimulationSettings`, pas des mesures : le taux de défaites du scénario C en dépend entièrement. Les vrais combats, eux, ne sont pas joués.
- Le profit d'un trajet avec combat comprend la vente des marchandises ramassées (elles se mélangent à la cargaison) : ces trajets sont exclus du contrôle de rendement par trajet.
- Les blessures et leur soin (régénération, hôpital) ne sont pas modélisés : un combat gagné ne coûte que les unités perdues.
- Les bandes sont supposées immobiles et rencontrées au hasard sur la route ; les patrouilles réelles ne sont pas modélisées.
- Les salaires sont ceux du vrai groupe de la scène (sa composition réelle, pas les 4 unités du modèle de combat) et sont prélevés par la vraie paie à chaque minuit : le marchand simulé ne les prévoit pas, il dépense tout son argent en marchandises. Le calendrier ne revient jamais en arrière : seul le premier scénario profite des jours offerts.
- Le temps passé en ville est nul (comme dans le jeu) ; le recrutement se paie directement, sans passer par la fenêtre de recrutement.
- Les durées de trajet sont celles du chemin NavMesh à vitesse de base ralentie par les terrains traversés ; un détour décidé par le joueur n'est pas simulé.

