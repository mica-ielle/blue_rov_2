# BLUE ROV

Projet Unity de simulation sous-marine autour d'un **BlueROV2**, d'un parc de six éoliennes offshore et d'une plateforme de mise à l'eau avec câble de récupération de 300 m.

## Démarrage rapide

- Ouvrir le dossier du dépôt dans Unity Hub avec **Unity 6.6 (6000.6.2f1)**.
- Ouvrir `Assets/Scenes/BlueROV.unity` et attendre la fin de l'import des assets et de la compilation.
- Cliquer sur **Play**. La scène comprend le BlueROV2, le ponton, la rampe et le câble connecté au treuil.
- Les outils de génération de scène sont dans le menu **BLUE ROV**. Le **Lot 5** ajoute ou reconstruit la plateforme, le treuil et le raccordement du câble. La scène principale se met à niveau à son ouverture si l'ancienne version du montage est détectée.

Le projet utilise **URP** et le **Input System**. Les dépendances sont listées dans `Packages/manifest.json`.

## Contenu

- Mer animée, houle, fond marin, rochers, algues, éclairage et ambiance sous-marine.
- Six éoliennes offshore avec fondations, rotors et câbles électriques.
- Modèle BlueROV2 avec six propulseurs, phares, caméras, Rigidbody et pilotage sous-marin.
- Ponton, rampe immergée, berceau, treuil et ombilical jaune connecté à l'arrière du ROV.
- Capacité de câble de **300 m** avec déploiement automatique et rappel du robot.

## Pilotage

En mode Play, utiliser le panneau **COMMANDES ROV** en bas à droite : cliquer sur une direction l'active, cliquer à nouveau l'arrête; **STOP** coupe les commandes du panneau.

- **Z / S** : avancer / reculer; **Q / D** : translation gauche / droite.
- **E / A** : monter / descendre; **flèches gauche / droite** : tourner.
- **Manette** : stick gauche pour avancer et se décaler, stick droit horizontal pour tourner, gâchettes pour monter ou descendre.
- **C** : changer de caméra; **L** : phares.
- **H / G** : maintien de profondeur / du cap; **K** : courant.
- **R** : replacer le ROV à son point de départ; **1 à 4** : régler la puissance.
- **REMBOBINER · RAPPELER LE ROV** : le treuil rembobine à **3 m/s** et tire le robot vers le guide fixé en haut de la rampe. Le bouton devient **ARRÊTER LE REMBOBINAGE** pendant le rappel. Le câble continue à être desservi si le ROV s'éloigne, jusqu'à la limite de 300 m.
- En caméra libre : clic droit + souris pour regarder; Maj pour accélérer. Le pavé à l'écran reste utilisable dans cette vue.

Le câble est rendu par une ligne avec des points de guidage et une légère flèche. Son mécanisme de tension agit sur le Rigidbody du ROV; la bobine hélicoïdale représente le câble enroulé sur le tambour. Ce n'est pas une simulation physique de chaque brin du câble.

## Reconstruction de la scène

Dans Unity, le menu **BLUE ROV** offre ces outils d'éditeur :

1. **Lot 1 - Reconstruire la mer** : crée la scène de base et remplace la scène entière.
2. **Lot 2 - Reconstruire les éoliennes** : reconstruit le parc éolien.
3. **Lot 3 - Reconstruire le BlueROV2** : reconstruit le robot et son modèle.
4. **Lot 4 - Activer le pilotage** : ajoute le Rigidbody et les composants de commande.
5. **Lot 5 - Ajouter la rampe de lancement** : construit le ponton, la rampe, le treuil, le câble de 300 m et le rappel pilotable.

Pour refaire toute la scène, exécuter les lots dans l'ordre **1 → 2 → 3 → 4 → 5**. Le Lot 1 efface les objets des autres lots. Après avoir reconstruit le robot avec le Lot 3, relancer le Lot 5 pour replacer le ROV sur la rampe et raccorder le câble. Le Lot 5 ajoute également le pilotage s'il manque.

## Où trouver le code

- `Assets/Editor/ConstruireLot1.cs` à `ConstruireLot5.cs` : génération des éléments de scène depuis l'éditeur.
- `Assets/Scripts/PilotageROV.cs` : flottabilité, traînée, propulseurs, commandes, télémétrie et interface.
- `Assets/Scripts/OmbilicalDynamique.cs` : longueur de câble, tension, déploiement, bobinage et rappel.
- `Assets/Scripts/EquipementsROV.cs` : caméras et phares.
- `Assets/Scripts/Mer.cs`, `AmbianceSousMarine.cs`, `HeliceROV.cs`, `RotorEolienne.cs` : environnement, animation et équipements.
- `Assets/Shaders` : shaders de l'océan et de l'immersion.
- `Assets/Guide_Projet.md` : guide plus détaillé sur la structure, les scripts, les lots et les contrôles.

Les meshes et matériaux générés sont dans `Assets/Generated` et peuvent être recréés avec les outils de génération.

## Modèle BlueROV2 et provenance

Le modèle utilisé est issu de [`patrickelectric/bluerov_ros_playground`](https://github.com/patrickelectric/bluerov_ros_playground), dans `Assets/Models/BlueRov2.dae`. Les descriptions `BlueRov2.urdf` et `BlueRov2.urdf.xacro` sont également fournies. La scène utilise le modèle Collada DAE; les outils Unity ajoutent les points de poussée et le contrôle physique. La licence upstream et les références sont consignées dans [`Assets/Models/NOTICE_BlueROV2_ROS.md`](Assets/Models/NOTICE_BlueROV2_ROS.md).

Pour davantage de détails sur la méthode d'importation et la structure du projet, consulter [`Assets/Guide_Projet.md`](Assets/Guide_Projet.md).
