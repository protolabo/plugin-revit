# Études préliminaires

## Analyse du problème

- Revit est largement utilisé dans le secteur de la construction, notamment par les architectes, les ingénieurs et les designers d'intérieur. L'Association professionnelle des designers d'intérieur du Québec (APDIQ) a d'ailleurs mis en place un programme de formation Revit en partenariat avec GRAITEC, soulignant l'importance de cet outil dans la transition numérique du secteur. Cependant, il n’existe aucun outil professionnel, qu’il soit libre ou payant, permettant la communication entre le logiciel de conception Revit et le logiciel d’analyse Ekahau. Un tel outil pourrait réduire significativement le temps nécessaire à l’analyse des réseaux Wi-Fi dans un modèle.

## Exigences

### Fonctionnelles
- Développer un plugin Revit capable d’exporter des plans sélectionnés au format Ekahau.
- Permettre la sélection et le filtrage des éléments du modèle à exporter.
- Générer un fichier Ekahau contenant tous les éléments nécessaires à l’analyse et la simulation Wi-Fi.
- Le fichier exporté doit respecter les formats attendus par Ekahau pour assurer la compatibilité.

### Non fonctionnelles
- Le plugin doit être facile à utiliser par les ingénieurs sans connaissances approfondies en programmation.
- L’exportation doit être rapide pour ne pas ralentir le travail quotidien.

## Recherche de solutions

- À l’heure actuelle, il n’existe aucune solution permettant l’automatisation de la création de modèles Ekahau à partir de modèles Revit, laissant comme seule option leur élaboration manuelle.

## Méthodologie

