# Modele BlueROV2

- Maillage et modele ROS d'origine : [patrickelectric/bluerov_ros_playground](https://github.com/patrickelectric/bluerov_ros_playground), fichiers `model/BlueRov2.dae` et `model/BlueRov2.urdf.xacro`.
- Le manifeste upstream declare la licence **LGPLv3** (`package.xml`). Voir le depot source pour les conditions completes.
- Le DAE contient des couleurs diffuses integrees et ne depend pas de textures externes. Ne pas remplacer ses materiaux dans Unity.
- `BlueRov2.urdf` est une version URDF standard minimale avec le maillage et le joint fixe de camera. Les balises Gazebo/Xacro ont ete omises. `BlueRov2.urdf.xacro` est la source upstream conservee sans modification.

## Import dans Unity

La scene importe directement `BlueRov2.dae` depuis `Assets/Models/`. Unity lit les materiaux Collada lors de l'import. `Assets/Editor/ConstruireLot3.cs` instancie le modele dans le BlueROV2 et conserve les materiaux importes. Le rig physique Unity ajoute six points d'application de poussee; le rendu CAD contient deja les propulseurs visuels.

Pour importer la description comme robot articule avec le menu contextuel Unity, installer le [Unity Robotics URDF Importer](https://github.com/Unity-Technologies/URDF-Importer), puis importer `BlueRov2.urdf`. Cette dependance n'est pas requise pour la scene du projet : le DAE est utilise directement.
