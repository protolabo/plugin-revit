# Division des murs en segments

## Division des murs

Supposons que nous ayons un mur avec deux fenêtres, comme illustré sur l’image: 

![représentation d’un mur avec deux fenêtres](images/wall_split/two_windows.png)

Par conséquent, si l’on trace le mur et les fenêtres de cette manière, la section du mur correspondant aux fenêtres contiendra à la fois 
le matériau du vitrage de la fenêtre et celui du mur, et l’analyse de la propagation du signal Internet lors de la simulation dans Ekahau 
donnera des résultats imprécis.

![mur avec deux fenêtres sans ouvertures](images/wall_split/two_windows_above.png)

Pour cette raison, il est nécessaire de diviser le mur en sections, comme illustré sur la figure.

![mur avec deux fenêtres et ouvertures](images/wall_split/two_windows_splited.png)

Pour cela, nous utiliserons une fonction récursive qui divise le mur en segments en respectant les limites des ouvertures intégrées dans le mur. 
Pour comprendre le fonctionnement de cette fonction, nous analyserons le mur présenté précédemment. L’algorithme de la fonction, ainsi que les 
paramètres qu’elle reçoit, seront simplifiés pour une meilleure compréhension.

- La fonction reçoit un mur accompagné de la liste des ouvertures qu’il contient. Le mur est défini par ses points de départ et d’arrivée, 
et chaque ouverture est définie par ses points de départ, intermédiaire et d’arrivée.

- La fonction prend la première ouverture de la liste et divise le mur en deux segments : l’un allant d’une extrémité du mur à une 
extrémité de l’ouverture, et l’autre allant de l’autre extrémité du mur à l’autre extrémité de l’ouverture. Pour le moment, il n’est pas important 
que les ouvertures soient ordonnées. Pour cet exemple, nous supposerons que la première ouverture de la liste est en réalité la deuxième ouverture 
de gauche à droite. Pour diviser correctement le mur en deux segments, il est nécessaire de déterminer quelle extrémité du mur est la plus proche 
de chaque extrémité de l’ouverture, comme illustré sur la figure :

![division incorrecte du mur](images/wall_split/two_windows_wrong.png)
![division correcte du mur](images/wall_split/two_windows_rigth.png)

Ce fragment de code permet de s’assurer que le mur est segmenté correctement:
```csharp
  double openStart = axis == "x" ? opening.start_point.x : opening.start_point.y;
  double openEnd = axis == "x" ? opening.end_point.x : opening.end_point.y;
  double startVal = axis == "x" ? wall.start.x : wall.start.y;
  double endVal = axis == "x" ? wall.end.x : wall.end.y;

  double distStart = Math.Min(Math.Abs(startVal - openStart), Math.Abs(startVal - openEnd));
  double distEnd = Math.Min(Math.Abs(endVal - openStart), Math.Abs(endVal - openEnd));

  WallData wall1, wall2;

  // Determines whether the opening is closer to the start or the end of the wall.
  if (distStart < distEnd)
  {
      // Split wall in segments according to the opening
      double cut = Math.Abs(startVal - openStart) < Math.Abs(startVal - openEnd) ? openStart : openEnd;

      // code continues...
```

- Après avoir divisé le mur, le code supprime l’ouverture de la liste des ouvertures. Pour chaque segment du mur, 
le code crée une nouvelle liste d’ouvertures et détermine quelles ouvertures correspondent à quel segment du mur.
Ensuite, elle appelle la fonction récursivement avec chaque segment de mur créé et sa liste correspondante.

- Le segment de droite ne contient aucune ouverture, tandis que le segment de gauche en contient une. Par conséquent, 
le segment de droite correspond au cas de base de la fonction récursive et s’arrête, en ajoutant ce segment à la liste 
des résultats, tandis que le segment de gauche doit à nouveau diviser le mur.

![division restante du mur](images/wall_split/two_windows_rigth_2.png)

- Le mur est de nouveau divisé en deux segments, et une nouvelle liste d’ouvertures est créée pour chaque segment. 
Étant donné qu’il ne reste plus d’ouvertures, les deux listes seront vides et les deux segments correspondront au 
cas de base lors de l’appel récursif de la fonction. Par conséquent, les deux segments sont ajoutés à la liste des 
résultats et l’exécution de la fonction récursive se termine. On obtient ainsi le mur complètement segmenté.

![résultat final](images/wall_split/two_windows_splited.png)

### Tri des ouvertures

Pour faciliter l’interconnexion des segments, nous devons trier les murs de manière ascendante, ainsi que leurs points de départ et d’arrivée. 
Supposons que le mur précédent ne contienne pas deux fenêtres, mais une fenêtre et un “vide” que le concepteur a inséré pour que le propriétaire 
de la maison puisse y placer un électroménager. La représentation graphique serait alors la suivante :

![mur avec une fenêtre et une ouverture](images/wall_split/window_opening.png)

Étant donné que les listes de murs et d’ouvertures ne conservent pas de relation directe avec le modèle graphique, il est possible que la 
liste des ouvertures du mur ressemble à ceci :

```json
    "openings" : [
      {
          "type" : "wall",
          "start_point" : 4,
          "end_point" : 6
      },
      {
          "type" : "void",
          "start_point" : 6,
          "end_point" : 8
      },
      {
          "type" : "window",
          "start_point" : 4,
          "end_point" : 2
      },
        {
          "type" : "wall",
          "start_point" : 0,
          "end_point" : 2
      },
        {
          "type" : "wall",
          "start_point" : 10,
          "end_point" : 8
      }
    ]
```
Après avoir divisé le mur en segments, le code s’assure que les ouvertures ainsi que leurs points de départ et d’arrivée sont stockés dans le bon ordre.

```json
    "openings" : [
      {
          "type" : "wall",
          "start_point" : 0,
          "end_point" : 2
      },
      {
          "type" : "window",
          "start_point" : 2,
          "end_point" : 4
      },
      {
          "type" : "wall",
          "start_point" : 4,
          "end_point" : 6
      },
      {
          "type" : "void",
          "start_point" : 6,
          "end_point" : 8
      },
      {
          "type" : "wall",
          "start_point" : 8,
          "end_point" : 10
      }
    ]
```



## Interconnexion entre murs et segments
L’interconnexion des murs et de leurs segments se fait en deux étapes.

### Interconnexion entre segments
Pour commencer, analysons comment connecter les différents segments de mur entre eux. 

Pour créer un mur dans Ekahau, il est nécessaire d’ajouter deux points dans le fichier wallPoints.json avec leurs ID respectifs, 
puis de créer un segment dans le fichier wallSegments.json en faisant référence aux ID de ces points.

Pour cette étape, le premier segment n’est pas pris en compte car ce segment contient le point initial du mur, qui sera utilisé ultérieurement 
pour tenter de connecter le mur avec d’autres murs du modèle. Supposons que le premier segment ait déjà été placé, 
c’est-à-dire que ses points initial et final sont déjà enregistrés dans le fichier correspondant.

Pour le mur de l’exemple, on constate que le point final du premier segment est suffisamment proche du point initial du second segment 
(en fait, ils sont au même point). Donc, au lieu de créer un nouveau point initial pour le second segment, on prend simplement l’ID 
correspondant au point final du premier segment, on crée un nouveau point correspondant au point final du second segment, puis on crée le segment. 
Ainsi, pour tracer les deux segments, seuls trois points sont nécessaires, et les murs seront interconnectés.

Ce processus se répète successivement jusqu’à la fin de la liste des segments.

Étant donné que les vides ne sont pas tracés, la distance entre le point final du troisième segment et le point initial du 
cinquième (puisque le vide correspond au quatrième segment) ne permet pas de réaliser l’interconnexion entre les segments. 
Par conséquent, pour le cinquième segment, il est nécessaire de créer les points initial et final dans le fichier JSON correspondant.

### Interconnexion entre murs 
Pour réaliser la connexion entre murs, on utilise une liste auxiliaire qui contient les points initial et final de tous les murs tracés. 
Ces points correspondent au point initial du premier segment et au point final du dernier segment. Cette liste contient les coordonnées 
du point ainsi que son ID correspondant

Pour réaliser l’interconnexion entre murs, il existe deux cas.

- Le mur ne contient qu’un seul segment.

Si le mur ne contient qu’un seul segment, cela signifie qu’il n’a pas d’ouvertures. Dans ce cas, les points initial et final du 
segment correspondent aux points initial et final du mur. Pour ce type de murs, on recherche dans la liste auxiliaire s’il existe 
un point proche de chaque extrémité permettant de réaliser la connexion. S’il n’existe pas de point suffisamment proche pour l’un 
des points d’extrémité, un nouveau point est créé dans le fichier JSON et ce point est ajouté à la liste auxiliaire.

- Le mur contient plus d’une ouverture

Si le mur contient plus d’un segment, alors le point initial du premier segment correspond au point initial du mur. Par conséquent, 
une recherche est effectuée dans la liste auxiliaire pour vérifier s’il existe un point suffisamment proche permettant de réaliser la connexion. 
S’il n’y a pas de point suffisamment proche, un nouveau point est créé dans le fichier JSON et ce point est ajouté à la liste auxiliaire.

D’autre part, nous savons que le point final du dernier segment correspond au point final du mur. Par conséquent, une recherche est 
effectuée dans la liste auxiliaire pour vérifier s’il existe un point suffisamment proche pour créer la connexion. En cas de point trouvé, 
le dernier segment du fichier JSON est supprimé (car ce segment est créé ailleurs dans le code) et un nouveau segment est créé en utilisant 
le point trouvé à la place du point final du dernier segment. Si aucun point suffisamment proche n’est trouvé dans la liste auxiliaire, 
aucune action n’est réalisée, car le dernier segment de tout mur est toujours tracé.


