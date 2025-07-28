# Développement

## Description des fichiers Ekahau

Un fichier Ekahau est une **archive ZIP** avec l’extension `.esx`. Ce fichier compressé contient les fichiers suivants :
```
applicationProfiles.json
attenuationAreaTypes.json
buildingFloors.json
buildings.json
deviceProfiles.json
floorPlans.json
floorTypes.json
images.json
networkCapacitySettings.json
project.json
projectConfiguration.json
projectHistory.json
requirements.json
usageProfiles.json
wallPoints.json
wallSegments.json
wallTypes.json
```

> Comme ces informations sont publiques, seuls les fichiers consultés ou modifiés par le code seront expliqués, et seules les parties pertinentes de chaque fichier seront montrées.

### `applicationProfiles.json`
> Ce fichier n’est ni modifié ni consulté par le code.

### `attenuationAreaTypes.json`
> Ce fichier n’est ni modifié ni consulté par le code.

### `buildingFloors.json`
Ekahau permet de créer un bâtiment en utilisant les différents étages (views) du modèle Revit, ce qui permet de simuler la propagation d’internet depuis les points d’accès à travers les différents étages. Ce fichier contient les informations nécessaires pour représenter chaque niveau du bâtiment. Ce fichier est constitué d’une liste de étages (floors) ; ci-dessous se trouve un exemple d’une telle instance de la liste.

```json
{
    "floorPlanId": "3f5f840b-3173-4bee-a2e0-e9003e74318b",
    "buildingId": "f4a0687e-7203-45ca-9e02-0f0b9538250a",
    "floorTypeId": "1b3ed0d7-4bb9-47c4-b3f1-993ba66ad628",
    "floorNumber": 2,
    "height": 2.5,
    "thickness": 0.5,
    "id": "3d5bd4cf-2e87-46f2-906d-729ca9c90eeb",
    "status": "CREATED"
}
```

### `buildings.json`
Contient la liste des bâtiments (dans notre cas, il y en aura toujours un seul).

### `deviceProfiles.json`
> Ce fichier n’est ni modifié ni consulté par le code.

### `floorPlans.json`
Chaque vue dans le modèle Revit est représentée par un étage dans Ekahau. Ce fichier contient les informations de chaque étage, y compris l’image de fond correspondante. 
Ce fichier est constitué d’une liste de étages (views). Ci-dessous se trouve un exemple d’une telle instance de la liste.
```json
{
    "name": "exported_view - Floor Plan - Level 1.bmp",
    "width": 1500.0,
    "height": 1267.0,
    "metersPerUnit": 0.02683082676480316,
    "imageId": "294e9d7c-18d4-4345-b29e-75208499a2da",
    "gpsReferencePoints": [],
    "floorPlanType": "FSPL",
    "cropMinX": 0.0,
    "cropMinY": 0.0,
    "cropMaxX": 1500.0,
    "cropMaxY": 1267.0,
    "rotateUpDirection": "UP",
    "tags": [],
    "id": "51213b74-6cd4-496d-8669-155f9f02607c",
    "status": "CREATED"
}
```

### `floorTypes.json`
Ce fichier contient les caractéristiques de propagation du signal pour différents types de planchers que l’on trouve dans des bâtiments courants tels que des bureaux, 
des hôtels, etc. Comme le type de plancher varie selon chaque modèle Revit, le code en sélectionne un par défaut, et l’utilisateur doit choisir le type de plancher 
correct une fois que le fichier est ouvert avec Ekahau.

### `images.json`
Ekahau enregistre un fichier contenant la liste des images qui seront utilisées dans le modèle comme fonds.
Ce fichier contient cette liste ; le champ « id » fait référence au nom de l’image, donc le nom du fichier image est : image-&lt;id&gt;.
Ce fichier est constitué d’une liste de images ; ci-dessous se trouve un exemple d’une telle instance de la liste.
```json
{
"imageFormat": "BMP",
"resolutionWidth": 1500.0,
"resolutionHeight": 1400.0,
"id": "e3657068-273e-4836-8ea7-bad33db464f5",
"status": "CREATED"
}
```

### `networkCapacitySettings.json`
> Ce fichier n’est ni modifié ni consulté par le code.

### `project.json`
Contient les informations générales du fichier : name, version, date de création, date de modification, etc. La date de création est ajoutée manuellement au moment de la création du fichier à l’aide du plugin.

### `projectConfiguration.json`
Ce fichier n’est ni modifié ni consulté par le code.

### `projectHistory.json`
> Ce fichier n’est ni modifié ni consulté par le code.
Ce fichier contient l’historique des modifications apportées au modèle.

### `requirements.json`
> Ce fichier n’est ni modifié ni consulté par le code.

Ce fichier contient l’ensemble des normes techniques des réseaux locaux (LAN) et spécifie l’ensemble des protocoles de contrôle d’accès au média (MAC) 
et de la couche physique (PHY) pour la mise en œuvre de la communication informatique sans fil (WLAN) sur chaque étage, en fonction du profil sélectionné. (*Exemple* IEEE802_11). 

### `usageProfiles.json`
> Ce fichier n’est ni modifié ni consulté par le code.

### `wallPoints.json`
Ce fichier contient la liste de tous les points correspondant au début et à la fin de tous les éléments de la simulation (murs, portes, fenêtres, etc.), pour chaque étage. Ce fichier est constitué d’une liste de wallPoints. Ci-dessous se trouve un exemple d’une telle instance de la liste.
```json
{
    "location": {
    "floorPlanId": "51213b74-6cd4-496d-8669-155f9f02607c",
    "coord": { "x": 841.31512950818, "y": 573.585663952959 }
    },
    "id": "9209f1a2-c61f-46fa-b9f7-8581fd97dd02",
    "status": "CREATED"
}
```

### `wallSegments.json`
Ce fichier contient une liste de segments reliant deux points pour former les éléments de la simulation (murs, portes, fenêtres, etc.). 
Ce fichier est constitué d’une liste de wallSegments. Ci-dessous se trouve un exemple d’une telle instance de la liste.
```json
{
    "wallPoints": ["9209f1a2-c61f-46fa-b9f7-8581fd97dd02", "8c76e652-0717-4fc1-bdd7-f51de423d4d5"],
    "wallTypeId": "e2713a0a-d747-45b1-8b5f-efa334b32348",
    "originType": "WALL_TOOL",
    "id": "ada4bd7b-d3be-42bd-8ce8-c4bbb715a9c6",
    "status": "CREATED"
},
```

### `wallTypes.json`
Ce fichier contient les informations pour chaque type de mur, comme son facteur d’atténuation et son épaisseur. 
Pour trouver le facteur d’atténuation affiché dans Ekahau, il est nécessaire de multiplier le facteur d’atténuation par l’épaisseur du mur.
Ce fichier est constitué d’une liste de murs. Ci-dessous se trouve un exemple d’une telle instance de la liste.
```json
{
    "name": "Window, Thick",
    "key": "ThickWindow",
    "color": "#ADE1FF",
    "propagationProperties": [
    {
        "band": "SIX",
        "attenuationFactor": 200.0,
        "reflectionCoefficient": 0.6944,
        "diffractionCoefficient": 11.0
    },
    {
        "band": "TWO",
        "attenuationFactor": 200.0,
        "reflectionCoefficient": 0.6944,
        "diffractionCoefficient": 11.0
    },
    {
        "band": "FIVE",
        "attenuationFactor": 200.0,
        "reflectionCoefficient": 0.6944,
        "diffractionCoefficient": 11.0
    }
    ],
    "thickness": 0.015,
    "lowerEdge": 0.0,
    "keybindNumber": 9,
    "id": "9624a855-0f43-45eb-abc1-3998111c54f9",
    "status": "CREATED"
},
```