# Guide du projet BLUE ROV

Projet Unity 6.6 (6000.6.2f1), rendu URP. La scène principale est `Assets/Scenes/BlueROV.unity`.

## Dossiers

- `Assets/Scenes` : scènes Unity. `BlueROV.unity` est la scène principale; `SampleScene.unity` est un exemple.
- `Assets/Editor` : outils de génération accessibles par le menu Unity **BLUE ROV**. Ils s'exécutent dans l'éditeur et enregistrent leur résultat dans la scène.
- `Assets/Scripts` : comportements utilisés pendant l'exécution (eau, caméra, pilotage, rotors).
- `Assets/Shaders` : shaders personnalisés `Ocean.shader` (surface et houle) et `Immerge.shader` (matériaux sous-marins).
- `Assets/Generated` : meshes et matériaux créés par les outils. Ils peuvent être régénérés; évite de les modifier à la main.
- `Assets/Settings` : profils URP et post-traitement.
- `ProjectSettings` : paramètres globaux Unity, qualité et pipeline graphique.
- `Packages/manifest.json` : liste des paquets. Le projet utilise notamment URP et le nouveau Input System.

## Les outils de génération

Dans Unity, le menu **BLUE ROV** contient :

1. **Lot 1 - Reconstruire la mer** — crée une nouvelle scène avec ciel, soleil, océan, fond irrégulier, rochers, algues, caméra et particules. Cette opération remplace la scène entière.
2. **Lot 2 - Reconstruire les éoliennes** — remplace uniquement le parc : six turbines, fondations, rotors et câbles électriques.
3. **Lot 3 - Reconstruire le BlueROV2** — remplace uniquement le robot : châssis, flotteurs, six propulseurs, phares, caméras et connecteur de câble.
4. **Lot 4 - Activer le pilotage** — ajoute au ROV le Rigidbody et le script de pilotage sous-marin.
5. **Lot 5 - Ajouter la rampe de lancement** — ajoute le ponton flottant, le treuil, les garde-corps, la rampe inclinée, le berceau et l'ombilical jaune entre le ROV et le treuil. Le ROV et la caméra sont placés pour cette mise à l'eau.

Pour reconstruire toute la scène, lance les lots dans cet ordre : **1 → 2 → 3 → 4 → 5**. Le lot 1 efface les objets des autres lots. Le lot 3 recrée le ROV; relance ensuite le lot 5 pour reconnecter le câble. Le lot 4 est nécessaire pour piloter le véhicule.

Les générateurs de captures `CapturesLot1.cs`, `CapturesLot2.cs` et `CapturesLot3.cs` servent à produire des images de contrôle depuis l'éditeur en mode batch; ils ne sont pas nécessaires pour lancer le jeu.

## Scripts de jeu

- `Mer.cs` calcule la hauteur de la houle et envoie les paramètres communs au shader de l'océan. La hauteur visuelle et celle utilisée par la physique partagent le même calcul.
- `AmbianceSousMarine.cs` adapte le brouillard, la couleur ambiante, le ciel et l'intensité du soleil selon la profondeur de la caméra.
- `CameraExploration.cs` déplace la caméra libre, la garde au-dessus du fond et permet de regarder à la souris.
- `PilotageROV.cs` applique flottabilité, masse d'eau, traînée, courant et poussée des six propulseurs. Il affiche aussi la télémétrie.
- `EquipementsROV.cs` bascule entre vue poursuite, caméra du robot et caméra libre; il commande aussi les phares.
- `HeliceROV.cs` anime les hélices des propulseurs selon leur commande.
- `RotorEolienne.cs` fait tourner les pales des turbines.

Les scripts de génération sont des outils d'éditeur; les scripts ci-dessus sont des composants de scène. On peut ajuster les champs publics d'un composant dans l'Inspector sans changer le code.

## Commandes pendant Play

Lance le **Lot 5** (il ajoute maintenant aussi les composants physiques et le pilotage), puis clique sur Play. Le panneau **COMMANDES ROV** en bas à droite permet de piloter à la souris : clique une fois pour activer une direction, reclique pour l'arrêter; **STOP** coupe toutes les commandes du panneau.

- **Z / S** : avancer / reculer
- **Q / D** : aller à gauche / à droite
- **E / A** : monter / descendre
- **Flèches gauche / droite** : tourner
- **Manette** : stick gauche pour avancer et se décaler, stick droit horizontal pour tourner, gâchettes pour monter/descendre
- **C** : changer de caméra (poursuite, caméra du ROV, caméra libre)
- **L** : allumer / éteindre les phares
- **H / G** : activer / désactiver le maintien de profondeur / du cap
- **K** : activer / désactiver le courant
- **R** : replacer le ROV au départ; **1 à 4** : régler la puissance
- En caméra libre : clic droit + souris pour regarder; Maj pour accélérer. Le pavé à l'écran peut aussi commander le robot dans cette vue.

## Modèle et provenance

Le modèle 3D utilisé par la scène provient de [patrickelectric/bluerov_ros_playground](https://github.com/patrickelectric/bluerov_ros_playground) : `Assets/Models/BlueRov2.dae`. Ce Collada embarque les couleurs diffuses des différentes pièces. Le constructeur de scène le charge sans remplacer ses matériaux par un matériau uniforme.

`Assets/Models/BlueRov2.urdf` est une version URDF standard minimale avec le maillage et le joint fixe de la caméra. `BlueRov2.urdf.xacro` conserve la description ROS upstream, y compris ses éléments Gazebo/Xacro. L'URDF peut être importé comme robot articulé avec le [Unity Robotics URDF Importer](https://github.com/Unity-Technologies/URDF-Importer); la scène actuelle utilise directement le DAE. Le contrôleur de Unity applique la poussée physique via six points de poussée créés dans `Assets/Editor/ConstruireLot3.cs`; les six propulseurs visibles sont déjà dans le modèle 3D. Le lot 5 ajoute automatiquement `Rigidbody` et `PilotageROV`, place le BlueROV2 sur la partie immergée haute de la rampe et relie le connecteur arrière au treuil avec un câble dynamique. Le câble suit le robot et applique une traction douce lorsqu'il atteint sa longueur de déploiement.

Le manifeste upstream indique **LGPLv3**. Voir [NOTICE_BlueROV2_ROS.md](Models/NOTICE_BlueROV2_ROS.md) et le dépôt source pour la provenance et les modalités d'import.

