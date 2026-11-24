# BLUE ROV

Projet Unity sous-marin comprenant un parc de six éoliennes offshore, un BlueROV2 pilotable et un ponton avec rampe de mise à l'eau.

## Ouvrir le projet

- Version Unity recommandée : **6000.6.2f1**
- Ouvre le dossier racine du dépôt dans Unity Hub.
- La scène principale est `Assets/Scenes/BlueROV.unity`.
- Guide détaillé : [Assets/Guide_Projet.md](Assets/Guide_Projet.md).

Les dépendances se trouvent dans `Packages/manifest.json`. Le projet utilise URP et le Input System.

## Éléments de la scène

- Océan animé par houle, fond marin, rochers, algues, éclairage et brume sous-marine.
- Six éoliennes, leurs fondations et le réseau électrique sous-marin.
- ROV avec six propulseurs, phares et caméras.
- Ponton flottant, treuil, rampe immergée, berceau du ROV et ombilical visuel relié au treuil.

## Génération dans Unity

Le menu **BLUE ROV** permet de reconstruire les éléments :

1. Lot 1 : mer et scène de base (remplace toute la scène).
2. Lot 2 : parc éolien.
3. Lot 3 : BlueROV2.
4. Lot 4 : physique et pilotage.
5. Lot 5 : ponton, rampe et câble.

Pour reconstruire toute la scène, lance les lots dans l'ordre **1 → 2 → 3 → 4 → 5**. Si tu reconstruis le ROV (Lot 3), relance le Lot 5 ensuite. Les assets procéduraux se trouvent dans `Assets/Generated`.

## Commandes de pilotage

Après avoir activé le Lot 4 et démarré Play :

- Z/S : avant/arrière; Q/D : gauche/droite; E/A : monter/descendre.
- Flèches gauche/droite : tourner.
- C : changer de caméra; L : phares.
- H : maintien de profondeur; G : maintien du cap; K : courant.
- R : retour au départ; 1–4 : puissance.
- En caméra libre : clic droit + souris pour regarder, Maj pour accélérer.

L'ombilical de la rampe est un maillage visuel fixe; il n'est pas simulé comme un câble dynamique en Play.

