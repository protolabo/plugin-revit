# Évaluation

## Plan de test

- Tests unitaires des fonctions principales du code

## Critères d'évaluation

L’évaluation du plugin sera réalisée manuellement sur des modèles Revit disponibles sur le site officiel Autodesk.  
Pour garantir le bon fonctionnement du plugin, d’autres sources que le site officiel Autodesk peuvent également être consultées.

### Liste des fichiers


| Modèle                                                     | Origine                        | Statut        |
|------------------------------------------------------------|--------------------------------|---------------|
| rac_basic_sample_project.rvt                               | Site officiel Autodesk         | Non conforme  |
| rac_advanced_sample_project.rvt                            | Site officiel Autodesk         | Non conforme  |


## Analyse des résultats

- Discuter des résultats obtenus lors des tests.

## Open Issues

### Problèmes ouverts par Exigence

**Exigence -** *Le fichier Ekahau doit contenir tous les murs, portes et fenêtres du modèle Revit, ou uniquement ceux situés à l’intérieur de la zone délimitée par l’utilisateur.* 
<div style="margin-left: 2em;">
    <b>Problèm -</b> Le code ne détecte pas toutes les portes, par exemple <i>Slider_Door</i> <br>
    <b>Problèm -</b> Il reste à effectuer des tests sur des modèles contenant des portes dans des murs inclinés. <br>
    <b>Problèm -</b> La représentation graphique de certains murs dans Revit apparaît incomplète en raison de la superposition d’autres éléments tels que les murs-rideaux (Curtain Walls) ou les garde-corps (Rails), tandis que l’API ne reflète pas ce changement. Il est nécessaire de trouver un moyen d’exporter correctement le mur sectionné. <br>
    <b>Problèm -</b> Pour le moment, le code obtient les dimensions des portes et fenêtres de différentes manières selon le type d’élément. Idéalement, il faudrait disposer d’une méthode unique et unifiée pour le faire.

</div>

**Exigence -** *Le fichier généré doit contenir un bâtiment structuré par étages, reflétant fidèlement l’organisation du modèle Revit.* 
<div style="margin-left: 2em;">
    <b>Problèm -</b> Vérifier que les étages sont ordonnés correctement (du premier étage au dernier, de manière ascendante) dans Ekahau. <br>
    <b>Problèm -</b> Pour le moment, la hauteur des étages ainsi que l’épaisseur des planchers et des plafonds dans le fichier Ekahau exporté ne correspondent pas au modèle Revit.

</div>

**Exigence -** *Le plugin doit être facile à utiliser par des ingénieurs ne possédant pas de connaissances approfondies en programmation.* 
<div style="margin-left: 2em;">
    <b>Problèm -</b> Il n’existe actuellement aucun moyen d’annuler l’exportation du modèle une fois le processus lancé.

</div>

**Exigence -** *L’exportation doit être rapide, afin de ne pas perturber le travail quotidien des utilisateurs.* 
<div style="margin-left: 2em;">
    <b>Problèm -</b> Le code génère de nombreux fichiers temporaires qui ne sont pas supprimés après la fin du processus d’exportation.  
    La création et la manipulation de ces fichiers ralentissent l’exécution du code.  
    Il est donc nécessaire d’éviter de générer ces fichiers, et s’ils sont indispensables, de les supprimer à la fin du processus.

</div>

