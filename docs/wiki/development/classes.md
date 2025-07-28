
## Description des classes

### AttenuationUpdater 
La classe AttenuationUpdater prend les valeurs d'atténuation saisies par l'utilisateur pour chaque mur dans Ekahau et les met à jour dans le fichier 
JSON Ekahau correspondant.

#### UpdateEkahauValues()
- Paramètres: null
- Return: Result

La méthode UpdateEkahauValues() prend les valeurs d’atténuation saisies par l’utilisateur, qui ont été enregistrées dans le fichier JSON wall_data, et met à jour 
les valeurs d’atténuation dans le fichier wallTypes.json d'Ekahau, en respectant les valeurs d’atténuation selon l’épaisseur du mur.

### BuildingFloorCreator 
La classe BuildingFloorCreator crée le fichier buildingFloors.json d'Ekahau pour créer un étage pour chaque vue (View) du modèle Revit 
à l’intérieur du bâtiment Ekahau.

#### CreateBuildingFloorsJsonFiles(string destDir)
- Paramètres: (string) le chemin du fichier de sortie
- Return: Result

La méthode CreateBuildingFloorsJsonFiles(string destDir) extrait les informations correspondantes à partir des fichiers Ekahau 
floorPlans.json, buildings.json, et floorTypes.json et crée ensuite le fichier buildingFloors.json, en générant un étage pour chaque vue (View) 
du modèle Revit, en ajoutant les références nécessaires pour associer chaque étage au bon type de plancher et au bon bâtiment, 
tout en respectant le format requis par Ekahau

### EkahauBuildingCreator 
La classe EkahauBuildingCreator crée un bâtiment dans Ekahau.

#### CreateBuildingFloorsJsonFiles(string destDir)
- Paramètres: (string) le chemin du fichier de sortie
- Return: Result

La méthode CreateBuildingFloorsJsonFiles(string destDir) crée le fichier buildings.json, qui contient les informations et la structure 
requises par Ekahau pour représenter l’unique bâtiment qui sera utilisé pour le modèle.

### EkahauFileCreator  
La classe EkahauFileCreator prend les fichiers JSON préparés au préalable et crée le fichier final d'Ekahau.

#### CreateEsxFile(string chosenFileName)
- Paramètres: (string) le nom du fichier final
- Return: Result

La méthode CreateEsxFile(string chosenFileName) prend le dossier Template contenant tous les fichiers JSON qui composent le fichier Ekahau, le compresse et lui donne 
le nom correspondant avec l’extension .esx.

### FileTemplateCreator  
La classe FileTemplateCreator permet de sélectionner le nom et l’emplacement du fichier final Ekahau et crée un dossier avec 
les fichiers de base nécessaires à la création du fichier Ekahau.

#### CreateFileTemplate(ref string chosenFileName, string destDir)
- Paramètres: (string) le nom du fichier final Ekahau passé par référence, (string) le chemin du dossier qui contiendra les fichiers JSON
- Return: Result

La méthode CreateFileTemplate(ref string chosenFileName, string destDir) affiche une boîte de dialogue permettant à l’utilisateur de choisir le nom 
et l’emplacement du fichier final Ekahau, puis copie un dossier contenant les fichiers de base nécessaires à la création d’un fichier Ekahau. 
Ces fichiers seront modifiés ultérieurement pour contenir les informations du modèle Revit. Le reste des fichiers JSON qui composent le fichier 
final Ekahau sera créé plus tard, au cours de l’exécution du code.

#### CopyDirectory(string sourceDir, string destDir)
- Paramètres: (string) le chemin du répertoire à copier, (string) le chemin du répertoire copié
- Return: Result

La méthode CopyDirectory(string sourceDir, string destDir) crée une copie du répertoire passé en paramètre ainsi que de tous les fichiers qu’il contient, 
vers l’emplacement également passé en paramètre.

### Getters  
La classe Getters  permet d’obtenir l’ID correspondant à un type de mur Ekahau spécifique.

#### GetEkahauNameFromWallData(string revitName, string categoryFilter = null)
- Paramètres: (string) le nom d’un type de mur Revit, (string) la catégorie du mur {"walls", "Doors", "Windows"}
- Return: (string) Le nom d’un type de mur Ekahau

L’équivalence entre les murs Revit et Ekahau se réalise via un fichier JSON contenant trois catégories : {"walls", "Doors", "Windows"}.
Chaque catégorie contient le nom Revit de chaque élément disponible dans le modèle Revit ainsi que le nom du type de mur correspondant dans Ekahau.
La méthode GetEkahauNameFromWallData(string revitName, string categoryFilter = null) reçoit le nom d’un élément Revit et sa catégorie, 
effectue une recherche dans le fichier wall_data.json et renvoie le nom du mur correspondant dans Ekahau.

#### GetWallId(string revitWallName, string wallTypesJson)
- Paramètres: (string) le nom d’un type de mur Revit, (string) le contenu d’un fichier JSON au format string
- Return: (string) l’ID du mur Ekahau correspondant

La méthode GetWallId(string revitWallName, string wallTypesJson) reçoit le nom d’un type de mur Revit, effectue une recherche par expression régulière 
dans le fichier JSON au format chaîne (string) et renvoie l’ID du mur Ekahau correspondant.

#### GetWindowId(string revitName, string wallTypesJson)
- Paramètres: (string) le nom d’un type de fenêtre Revit, (string) le contenu d’un fichier JSON au format string
- Return: (string) l’ID du mur Ekahau correspondant

La méthode GetWallId(string revitWallName, string wallTypesJson) reçoit le nom d’un type de fenêtre Revit, effectue une recherche par expression régulière 
dans le fichier JSON au format chaîne (string) et renvoie l’ID du mur Ekahau correspondant.

#### GetDoorId(string revitName, string wallTypesJson)
- Paramètres: (string) le nom d’un type de porte Revit, (string) le contenu d’un fichier JSON au format string
- Return: (string) l’ID du mur Ekahau correspondant

La méthode GetWallId(string revitWallName, string wallTypesJson) reçoit le nom d’un type de porte Revit, effectue une recherche par expression régulière 
dans le fichier JSON au format chaîne (string) et renvoie l’ID du mur Ekahau correspondant.

### IDGenerator 
La classe IDGenerator assigne de nouveaux ID dans les fichiers JSON de base qui constitueront le fichier final Ekahau.

#### GenerateIDInJsonFiles(string destDir)
- Paramètres: (string) le chemin du répertoire contenant les fichiers JSON qui constitueront le fichier Ekahau
- Return: Result

Les objets contenus dans les fichiers JSON d'Ekahau possèdent un champ "id" qui permet de les identifier. 
Il s'agit d'une valeur GUID différente pour chaque nouveau fichier. Pour générer le fichier final Ekahau, 
le plugin part d’une série de fichiers JSON communs à tous les fichiers Ekahau, mais dont les champs "id" sont vides. 
La méthode GenerateIDInJsonFiles(string destDir) permet de parcourir ces fichiers de base et de générer un nouveau GUID 
dans le champ "id" pour chacun de ces fichiers.

### IDReferenceFiller 
La classe IDReferenceFiller permet de créer les références d’ID entre les différents fichiers JSON qui constitueront le fichier Ekahau.

#### CompleteJsonReferences(string destDir, string baseName)
- Paramètres: (string) le chemin du répertoire contenant les fichiers JSON, (string) le nom du fichier Ekahau
- Return: Result

Pour identifier les objets contenus dans les fichiers JSON d'Ekahau, on utilise leur champ "id". Par exemple, chaque "floor" dans Ekahau, 
qui représente une "view" dans Revit, possède une image de fond. Il est donc nécessaire, pour représenter correctement les étages, 
de générer correctement les références entre les ID des fichiers floorPlans.json, images.json et le nom des images exportées depuis Revit. 
La méthode CompleteJsonReferences(string destDir, string baseName) se charge de créer ces références, et de remplir les champs vides 
restants tels que le nom du fichier, la date de création, etc.

### ImageExporter
La classe ImageExporter exporte une image pour chaque vue sélectionnée du modèle Revit et crée un fichier JSON auxiliaire contenant 
les métadonnées des images exportées.

#### CreateViewImagesAndReport(ExternalCommandData commandData, string outputDir, List&lt;ElementId&gt; selectedViewIds)
- Paramètres: (ExternalCommandData) le modèle Revit, (string) Le chemin du répertoire contenant les fichiers auxiliaires, 
              (List&lt;ElementId&gt;) la liste des "Views" sélectionnées
- Return: null

La méthode CreateViewImagesAndReport(ExternalCommandData commandData, string outputDir, List&lt;ElementId&gt; selectedViewIds) exporte 
une image pour chaque vue sélectionnée, la stocke dans le chemin passé en paramètre, et crée au même emplacement 
un fichier JSON contenant les métadonnées des images.

### ImageJsonFileCreator  
La classe ImageJsonFileCreator crée le fichier JSON Ekahau images.json à partir des images exportées depuis Revit, 
complète les références dans le fichier Ekahau floorPlans.json et définit l’échelle du modèle Ekahau.

#### FormatImagesAndCreateJsonFile(string destDir)
- Paramètres: (string) le chemin du répertoire contenant les fichiers JSON pour le fichier Ekahau
- Return: Result

Les images de fond (maps) dans les fichiers Ekahau doivent avoir un nom spécifique : image-&lt;id&gt;. 
La méthode FormatImagesAndCreateJsonFile(string destDir) prend les images exportées depuis Revit et crée une copie dans le 
répertoire contenant les autres fichiers JSON nécessaires, puis renomme les images en image-&lt;id&gt;. 
Cette méthode utilise le fichier auxiliaire imageData.json pour compléter les informations manquantes pour chaque 
étage (view) dans le fichier Ekahau floorPlans.json et définit dans ce fichier l’échelle de chaque étage du modèle Ekahau

#### GetMetersPerUnit(string imageName, string directoryPath, JArray viewInfo)
- Paramètres: (string) Le nom de l’image exportée, (string) le chemin des fichiers auxiliaires, (JArray) les métadonnées des images
- Return: (double) l’échelle du modèle Ekahau en mètres par pixel

La méthode GetMetersPerUnit(string imageName, string directoryPath, JArray viewInfo) utilise le chemin des fichiers auxiliaires pour 
lire le fichier elements_{viewName}.json et obtenir les informations du premier mur qu’elle trouve. 
Elle récupère également les informations de l’image exportée correspondant au nom de l’image passé en paramètre. 
Après avoir recoupé toutes ces informations, elle effectue les calculs nécessaires pour déterminer l’échelle de chaque étage dans Ekahau.
Pour une explication plus détaillée de ce processus, consulter la section « Définir l’échelle dans Ekahau »

### ModelDataExporter  
La classe ModelDataExporter génère tous les fichiers auxiliaires nécessaires au traitement des informations contenues dans le modèle Revit. 
Dans un premier temps, la classe crée un fichier JSON pour chaque vue sélectionnée, contenant les informations de tous les murs 
et ouvertures (openings) de la vue correspondante. Ensuite, elle divise les murs en sections afin de générer l’espace 
nécessaire pour les portes, fenêtres et autres ouvertures, car les informations fournies par l’API de Revit ne tiennent pas compte de ces ouvertures. 
Elle crée alors un fichier JSON contenant les segments de mur pour chaque vue.
Enfin, elle exporte une image pour chaque vue sélectionnée et génère un fichier auxiliaire contenant les métadonnées des images exportées.

#### ExportModelData(ExternalCommandData commandData)
- Paramètres: (ExternalCommandData) le modèle Revit
- Return: Result

La méthode ExportModelData(ExternalCommandData commandData) récupère la liste des murs, des portes, des fenêtres et des ouvertures générales. 
Ensuite, elle détermine quelles portes, fenêtres et ouvertures sont associées à chaque mur et extrait les informations pertinentes de chaque 
mur et de chaque élément, telles que le point d’origine, le point final, la position, les dimensions, etc.
Elle crée ensuite une liste d’objets où chaque instance représente un mur, et chaque mur contient une liste de ses ouvertures correspondantes.

Ensuite, elle procède à la division des murs en sections et crée un fichier JSON contenant les informations mises à jour pour chaque vue. 
Finalement, elle exporte une image pour chaque vue sélectionnée et génère un fichier JSON auxiliaire contenant les métadonnées des images
 
### PointAndSegment  
La classe PointAndSegment crée un point ou un segment de mur avec la structure et le format spécifiques, puis les ajoute directement 
aux fichiers JSON Ekahau correspondants.

#### MakeWallPoint(string id, string floorPlanId, double x, double y)
- Paramètres: (string) l’ID du point, (string) l’ID du floor correspondant, (double) la coordonnée en x du point, (double) la coordonnée en y du point
- Return: (string) l'objet du point au format string

La méthode MakeWallPoint(string id, string floorPlanId, double x, double y) crée un objet contenant les informations d’un point avec 
la structure requise pour être utilisé dans le fichier Ekahau et le retourne au format string.

#### MakeWallSegment(string idStart, string idEnd, string wallTypeId)
- Paramètres: (string) l’ID du point initial du segment, (string) l’ID du point final du segment, (string) l’ID du type de mur correspondant
- Return: (string) l'objet du segment de mur au format string

La méthode MakeWallSegment(string idStart, string idEnd, string wallTypeId) crée un objet contenant les informations d’un segment de mur 
avec la structure requise pour être utilisé dans le fichier Ekahau et le retourne au format string.

### SegmentsListCreator 
La classe SegmentsListCreator parcourt la liste des murs et trace tous les segments de mur correspondants à chaque mur. Pour chaque segment, elle crée les points 
de départ et d’arrivée dans le fichier JSON Ekahau correspondant, puis elle crée le segment de mur dans ce même fichier JSON Ekahau en faisant 
référence aux identifiants des points préalablement définis.

Si une distance suffisamment courte existe entre deux segments, une connexion est créée entre eux. Si une distance suffisamment petite existe 
entre les extrémités des murs, une connexion est également créée entre ces extrémités.

#### FillSegmentsList(JToken elementsJson, string floorPlanId, Func&lt;double, double&gt; convertX, Func&lt;double, double&gt; convertY, string path, List&lt;string&gt; wallPointsList, List&lt;string&gt; wallSegmentsList)
- Paramètres: (JToken) l'objet qui contient tous les murs et les segments, (string) l’ID de l’étage, 
(Func&lt;double, double&gt;) fonction qui convertit la composante x de pieds en pixels, 
(Func&lt;double, double&gt;) fonction qui convertit la composante y de pieds en pixels, (string) le chemin du répertoire des fichiers JSON, 
(List&lt;string&gt;) la liste des points au format string, (List&lt;string&gt;) la liste des segments au string
- Return: null

La méthode FillSegmentsList(JToken elementsJson, string floorPlanId, Func&lt;double, double&gt; convertX, Func&lt;double, double&gt; convertY, string path, 
List&lt;string&gt; wallPointsList, List&lt;string&gt; wallSegmentsList) « trace » les segments de mur dans le fichier Ekahau. Pour chaque segment, 
elle ajoute les points de départ et d’arrivée dans la liste correspondante en respectant le format Ekahau. Immédiatement après, elle ajoute le segment à 
la liste correspondante en faisant référence aux identifiants des points précédemment ajoutés. Si possible, elle crée des connexions entre 
les segments et entre les extrémités des murs, pour cela :

Le code crée une liste auxiliaire de points qui stockera la position et l’identifiant des extrémités des murs.

Si le mur contient un seul segment, cela signifie que le mur n’a pas d’ouvertures. Dans ce cas, le code cherche dans la liste auxiliaire 
deux points situés à moins d’un pouce en utilisant le théorème de Pythagore. S’il en trouve un ou les deux, le code utilise l’identifiant 
de ces points pour générer le segment.

Si le mur contient plus d’un segment et si le code détecte qu'il est en train de « tracer » un segment intermédiaire, 
il calcule la distance entre le point final du segment précédent et le point initial du segment actuel. Si cette distance est inférieure à un pouce, 
il utilise l’identifiant du point final du segment précédent au lieu d’en créer un nouveau.

Si le code détecte qu’il est en train de « tracer » le premier segment, alors le point initial de ce segment correspond au début du mur. 
Dans ce cas, le code cherche un point proche pour établir la connexion.

Si le code détecte qu’il est en train de « tracer » le dernier segment, alors le point final de ce segment correspond à la fin du mur. Dans ce cas, le code cherche un point proche pour établir la connexion.

#### CalculateDistance(double x1, double y1, double x2, double y2)
- Paramètres: (double) coordonnée x du premier point, (double) coordonnée y du premier point, (double) coordonnée x du deuxième point, (double) coordonnée y du deuxième point
- Return: (double) distance entre les points

La méthode CalculateDistance(double x1, double y1, double x2, double y2) calcule la distance entre deux points en utilisant le théorème de Pythagore.

### TemporaryFilesCollector  
La classe TemporaryFilesCollector supprime les fichiers temporaires.

#### DeleteTemporaryFiles()
- Paramètres: null
- Return: null

La méthode DeleteTemporaryFiles() supprime le dossier temporaire contenant tous les fichiers auxiliaires ainsi que tous les fichiers qu’il contient.

### WallsInserter 
La classe WallsInserter crée le fichier JSON Ekahau qui contiendra la liste des points nécessaires pour définir les murs dans le modèle Ekahau, 
ainsi que le fichier JSON Ekahau qui contiendra la liste des murs. 

#### InsertWallAndOpeningsInEkahauFile(Document doc)
- Paramètres: (Document) le modèle Revit
- Return: Result

La méthode InsertWallAndOpeningsInEkahauFile(Document doc) charge les fichiers JSON nécessaires pour déterminer la position correcte de chaque 
point initial et final de chaque mur dans le modèle Ekahau. Elle crée une liste contenant tous les points nécessaires et une autre liste 
contenant les murs du modèle. Deux fonctions sont définies : l’une pour convertir la coordonnée X d’un point de pieds en pixels, l’autre 
pour convertir la coordonnée Y. Pour plus d’informations sur ces deux fonctions, consultez la section « Conversion des coordonnées de Revit vers Ekahau ».

La méthode remplit les listes avec les points et les murs obtenus, puis crée finalement les fichiers JSON Ekahau wallPoints.json et wallSegments.json 
dans le répertoire contenant le reste des fichiers JSON Ekahau

### WallSplitter  
Lors de l’obtention de la liste des murs et des ouvertures via l’API de Revit, la seule relation qu’une ouverture conserve avec le mur dans 
lequel elle est encastrée est son identifiant (ID). Par conséquent, même si un mur dans Revit contient une ouverture, l’API retourne uniquement 
les points de début et de fin du mur, sans prendre en compte la position ni les dimensions des ouvertures qu’il peut contenir.

La classe WallSplitter crée les ouvertures nécessaires dans le mur en le divisant en sections, afin d’obtenir une représentation graphique 
du projet plus réaliste.

#### SplitWallByOpening(string inputFileName, string outputFileName)
- Paramètres: (string) le fichier JSON qui contient les informations des murs et de leurs ouvertures, 
(string) le fichier JSON qui contiendra les informations originales ainsi que les sections créées
- Return: null

La méthode SplitWallByOpening(string inputFileName, string outputFileName) récupère la liste des murs et de leurs ouvertures à partir 
du fichier JSON passé en paramètre. Elle divise chaque mur en sections en fonction de la liste d’ouvertures qu’il contient, 
puis ajoute ces sections à la liste des ouvertures du mur.

Les segments sont ajoutés à la liste des ouvertures, car une ouverture peut représenter un vide, une porte ou une fenêtre, et aussi bien les portes, 
les fenêtres que les segments de mur doivent être « tracés » dans le modèle Ekahau. Une autre partie du code déterminera s’il s’agit d’une porte, 
d’une fenêtre, d’un segment ou d’un vide.

Finalement, la méthode trie les ouvertures par position de manière ascendante, ainsi que tous les points de début et de fin de chaque ouverture et du mur concerné.

#### LengthBetweenPoints(Point p1, Point p2)
- Paramètres: (Point) le premier point, (Point) le deuxième point
- Return: (double) la distance entre les points

La méthode LengthBetweenPoints(Point p1, Point p2) calcule la distance entre deux points en utilisant le théorème de Pythagore.

#### CenterOfElement(OpeningData element)
- Paramètres: (OpeningData) une ouverture au format objet
- Return: (Point) le point central de l’ouverture

La méthode CenterOfElement(OpeningData element) retourne le point central d’une ouverture.

#### IsCenterInsideWall(WallData wall, Point center)
- Paramètres: (WallData) un mur au format objet, (Point) un point
- Return: (bool) vrai si le point se trouve entre les extrémités du mur

La méthode IsCenterInsideWall(WallData wall, Point center) détermine si un point se trouve entre les extrémités d’un mur.

#### RecursiveWallSplit(WallData wall, List&lt;OpeningData&gt; openings, List&lt;WallData&gt; results)
- Paramètres: (WallData) un mur au format objet, (List&lt;OpeningData&gt;) la liste des ouvertures, (List&lt;WallData&gt;) la liste qui contiendra les segments
- Return: null

La méthode RecursiveWallSplit(WallData wall, List&lt;OpeningData&gt; openings, List&lt;WallData&gt; results) est une méthode récursive 
qui divise un mur en sections selon la liste des ouvertures, en suivant ces étapes :

Cas de base : Si la liste des ouvertures est vide, enregistrez le mur dans la liste des résultats. <br>
Cas récursif : Si la liste des ouvertures n’est pas vide, prenez la première ouverture et divisez le mur en deux segments. Le premier 
segment va d’une extrémité du mur à une extrémité de l’ouverture, le second segment va de l’autre extrémité du mur à l’autre extrémité de l’ouverture.
Pour chaque segment, créez une liste d’ouvertures et conservez celles dont le centre se trouve à l’intérieur des limites du segment.
Ensuite, appelez la fonction récursivement avec ces segments et les listes d’ouvertures correspondantes.

Pour plus d’informations sur ce processus, consultez la section « ».

### WindowDoorDimensions  
La classe WindowDoorDimensions est responsable d'obtenir les dimensions et la position spatiale d'éléments de type fenêtre ou porte (FamilyInstance)
dans un modèle Revit.

#### GetWindowDoorDimensions(FamilyInstance inst, double wallX1, double wallY1, double wallZ1, double wallX2, double wallY2, double wallZ2)
- Paramètres : (FamilyInstance) instance de famille {fenêtre, porte}, (double) coordonnée X du premier extrémité du mur, 
(double) coordonnée Y du premier extrémité du mur, (double) coordonnée Z du premier extrémité du mur, (double) coordonnée X du second extrémité du mur, 
(double) coordonnée Y du second extrémité du mur, (double) coordonnée Z du second extrémité du mur 
- Return: (object) objet contenant les informations de l’ouverture

La méthode GetWindowDoorDimensions(FamilyInstance inst, double wallX1, double wallY1, double wallZ1, double wallX2, double wallY2, double wallZ2) 
obtient les dimensions (largeur et hauteur) et la position spatiale (centre, points de départ et de fin) d'une instance de fenêtre ou porte 
à l'intérieur d'un mur défini par deux points.
        
La méthode récupère les dimensions de manière progressive :
Tente d'obtenir les paramètres "Width" et "Height" directement depuis l'instance.
Si non disponibles, tente de récupérer ces paramètres depuis le symbole familial (FamilySymbol) en utilisant les paramètres BuiltIn.
Si toujours indisponibles, calcule les dimensions à partir de la géométrie de l'instance.

Ensuite, elle calcule la position centrale de l'élément dans l'espace, en ajustant les coordonnées X et Y pour correspondre au mur si nécessaire.
Elle calcule également deux points (start_point et end_point) perpendiculaires à l'orientation de l'élément qui représentent l'extension 
en plan en tenant compte de la largeur.

Finalement, elle retourne un objet anonyme contenant les informations pertinentes : type, id, nom, position, dimensions et points extrêmes.
        

## Interaction entre les classes et leurs méthodes

Le diagramme suivant illustre l’interaction entre les différentes méthodes du code. Plus précisément, il montre le flux de travail général en 
indiquant à quelle méthode chaque méthode fait appel. Dans le diagramme, chaque méthode appelle celle qui se trouve à l’intérieur de sa propre figure.
Le numéro à l’intérieur des méthodes principales indique l’ordre dans lequel elles sont appelées par la méthode Export.Execute().

![Diagramme des appels](images/function_call_diagram.png)


