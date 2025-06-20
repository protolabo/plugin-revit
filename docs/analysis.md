# Études préliminaires

## Analyse du problème

Revit est largement utilisé dans le secteur de la construction, notamment par les architectes, les ingénieurs et les designers d'intérieur. L'Association professionnelle des designers d'intérieur du Québec (APDIQ) a d'ailleurs mis en place un programme de formation Revit en partenariat avec GRAITEC, soulignant l'importance de cet outil dans la transition numérique du secteur. Cependant, il n’existe aucun outil professionnel, qu’il soit libre ou payant, permettant la communication entre le logiciel de conception Revit et le logiciel d’analyse Ekahau. Un tel outil pourrait réduire significativement le temps nécessaire à l’analyse des réseaux Wi-Fi dans un modèle.

## Exigences

Le projet est divisé en deux parties : **"Export vers Ekahau"** et **"Import depuis Ekahau"**.  
La première partie consiste à générer un fichier compatible avec Ekahau à partir d’un modèle Revit, afin de réaliser une simulation de points d’accès Wi-Fi. Selon l’avancement de cette première phase et le temps disponible, la deuxième partie sera abordée. Celle-ci a pour objectif d’importer les informations 
des points d’accès simulés dans Ekahau vers le modèle Revit original.

### Exigences fonctionnelles – Export vers Ekahau

- 🔄 Le fichier Ekahau doit contenir **tous les murs, portes et fenêtres** du modèle Revit, ou uniquement ceux situés à l’intérieur de la zone délimitée par l’utilisateur.
- ⏳ Le plugin doit offrir à l’utilisateur la possibilité **d’exclure les escaliers** de la simulation et **d’inclure les murs des ascenseurs**, avec une option **distincte pour chaque étage** du modèle.
- ⏳ Tous les éléments exportés vers Ekahau doivent comporter une **valeur d’atténuation en dB**, soit en les faisant correspondre à un type d’élément existant dans Ekahau, soit en **calculant manuellement leur atténuation**.
- ⏳ Le fichier exporté doit respecter **la hauteur des étages**, **l’échelle du modèle Revit** et **l’épaisseur des planchers et des plafonds**.
- 🔄 Le fichier généré doit contenir **un bâtiment structuré par étages**, reflétant fidèlement l’organisation du modèle Revit.
- 🔄 Le plugin doit être facile à utiliser par des ingénieurs ne possédant pas de connaissances approfondies en programmation.
- 🔄 L’exportation doit être rapide, afin de ne pas perturber le travail quotidien des utilisateurs.
 
### Exigences non fonctionnelles – Export vers Ekahau

- ⏳ Automatiser le cadrage de la zone à exporter depuis Revit.  
- ✅ Mettre en place automatiquement l'échelle dans Ekahau.

### Exigences fonctionnelles – Import depuis Ekahau

- ⏳ Exporter depuis Ekahau vers Revit en **respectant la position exacte** des points d’accès simulés.  

### Exigences non fonctionnelles – Import depuis Ekahau

- ⏳ Modéliser les points d'accès importés dans Revit sous une famille de symbole BPA.

<br>
!!! note "Légende des symboles"
    ✅ Terminé, 🔄 En cours, ⏳ À venir

## Recherche de solutions

- À l’heure actuelle, il n’existe aucune solution permettant l’automatisation de la création de modèles Ekahau à partir de modèles Revit, laissant comme seule option leur élaboration manuelle.

<!-- ## Méthodologie -->

