# Suivi de projet

## Semaine 1

#### Prise de connaissance du projet
- Définition de la liste des logiciels, langages et outils nécessaires au projet
- Définition de l’objectif et de la portée du projet

<!-- !!! info "Notes" -->
<!-- - Il est possible que nous révisions les exigences après le prototypage -->

<!-- !!! warning "Difficultés rencontrées" -->
<!-- - Le plugin Mermaid n'était pas reconnu : confusion entre `mkdocs-mermaid2-plugin` (pip) et `mermaid2` (plugin name) -->
<!-- - Résolu après nettoyage et configuration correcte dans `mkdocs.yml` -->

<!-- !!! abstract "Prochaines étapes" -->
<!-- - Démarrer l’analyse du problème -->
<!-- - Créer la structure de `etudes_preliminaires.md` -->

---

## Semaine 2

#### Phase d’exploration des logiciels
- Exploration de l’API de Revit
    - Analyse de la documentation officielle et des exemples fournis.
    - Découverte des classes et méthodes principales.
    - Expérimentation des mécanismes de filtrage et de sélection d’éléments dans un modèle.
    - Compréhension des flux de données et des événements que l’API permet de gérer.
- Exploration de Ekahau
    - Analyse approfondie pour comprendre l’organisation et le format des fichiers Ekahau.
- Premier prototype (naïf)

!!! warning "Difficultés rencontrées"
    - Ekahau ne propose pas d’interface API permettant la manipulation directe de ses modèles.

## Semaine 3

#### Modélisation UML
- Création de modèles UML distincts pour Revit et Ekahau dans le but de définir clairement le flux de travail et les interactions entre les deux systèmes.

## Semaine 4

#### Description formelle du projet

Après s’être réuni avec l’équipe de travail de BPA et s’être familiarisé avec les outils Revit et Ekahau, 
il est désormais possible de définir formellement le projet et sa portée. Cela permet également de mieux 
comprendre les exigences du client ainsi que de déterminer la méthodologie à suivre pour le développement du plugin.


## Semaine 5

#### Premier prototype
- Le code correspondant au premier prototype est ajouté au dépôt du projet.

## Semaine 6

#### Démonstration du premier prototype
- La démonstration du premier prototype est réalisée lors d'une réunion avec le client.
- Les fonctionnalités finales que le code devra inclure pour la partie Export sont définies.

## Semaine 7

#### Automatic scaling functionality is added.
For Ekahau to perform an analysis whose results accurately reflect reality, it is necessary to set the real-world scale within the Ekahau model. 
This is done by drawing a line anywhere on the model (map) and entering the actual length of that line. 
For example, if there is a wall that is 3 meters long, you draw a line from one end of the wall to the other and enter the value 3 as the line’s length.

Instead of doing that, the code has been modified to randomly select a wall and perform the necessary calculations to determine the model’s real-world scale. 
These calculations are possible because Revit uses feet as its internal unit of measurement, making it feasible to deduce the real scale of the Revit model 
from any element it contains.

## Semaine 8

#### An option is added to exclude the stair area in the Ekahau model.
During one of the meetings held with the team responsible for the Plugin on BPA’s side, we were informed that it is common to exclude certain elements 
from the Revit model when performing the analysis in Ekahau—specifically elevators and staircases. To achieve this automatically, 
the code has been modified to add an option in the view selection screen that allows the user to specify whether they want to add an exclusion 
zone corresponding to the stair area or include the stair area in the analysis. In the latter case, the code does not add any exclusion zone.

## Semaine 9

#### A method for mapping Revit walls to Ekahau walls is added
One of the main challenges in developing the plugin is finding a way to establish a correspondence between Revit wall types and Ekahau wall types. 
After conducting online research, it was concluded that there is no analytical method to calculate a wall’s attenuation level. Instead, 
the most common approach is to physically measure these values and extrapolate them to other types of walls that share similar characteristics.

For this reason, the correspondence between wall types in the two programs has been designed to be defined manually by the user. 
A JSON file has been created containing all the available wall, door, and window types from Revit, and each wall type includes 
a field where the corresponding Ekahau wall type can be specified. The values in this file can be edited through the Revit GUI 
to make it easier for the user to update them.

## Semaine 10

#### The crop area is determined automatically
To export the model as a BMP image, Revit uses the crop region as the model boundaries; everything inside the crop region will be exported within the image. 
After several attempts, it has been determined that there are actually at least two types of crop regions: one for the geometry (the elements contained in the model) 
and one for annotations (dimensions, texts, etc.). In cases where the annotation crop region does not include all annotations inside it, the final model in Ekahau 
will have scaling and alignment problems between the wall segments of the model and the background image.

To avoid these problems, the code has been modified so that the crop area includes all annotations within it automatically, without the user needing to worry about it.

## Semaine 11

#### Release of version NET 8.0
Starting with the 2025 version, Revit uses .NET version 8.0. Since the BPA development team uses different versions of Revit — including versions 
before and after 2025 — it is necessary to develop a .NET 8.0 version of the plugin. This week, the code using .NET 8.0 with the same functionalities as the 
.NET 4.8 version is released

## Semaine 12

## Semaine 13
