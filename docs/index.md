# Projet IFT3150: Développement d’un plugin Revit pour l’intégration des données Ekahau

> **Thèmes**: Génie logiciel, CAD, Intégation logicielle  
> **Superviseur**: Louis-Edouard Lafontant  
> **Collaborateurs:** Bouthillette Parizeau (BPA)  

## Informations importantes

!!! info "Dates importantes"
    - **Description du projet** : 25 mai 2025
    <!-- - **Foire 1: Prototypage** : 9-13 juin 2025 --> 
    <!-- - **Foire 2: Version beta** : 14-18 juillet 2025  -->
    - **Présentation et rapport** : 8 août 2025

## Équipe

- Arman Nunez
- Erick Oswaldo de la Cruz Diaz

## Description du projet 

### Contexte
Dans le domaine de l’ingénierie, l’utilisation d’outils de modélisation tels que AutoCAD, Revit ou MATLAB est fréquente. La conception d’un projet d’ingénierie complet nécessite souvent plusieurs outils spécialisés.
En télécommunication, par exemple, Revit et Ekahau sont couramment utilisés de manière complémentaire : Revit pour la conception architecturale, et Ekahau pour la simulation et le positionnement optimal des points d’accès réseau (Wi-Fi). Toutefois, l’échange de données entre ces deux logiciels demeure problématique en raison d’un manque de compatibilité directe.

### Problématique ou motivations
La simulation pour l’analyse des réseaux Wi-Fi dans Ekahau représente une tâche particulièrement longue et fastidieuse. En effet, cette opération nécessite une intervention manuelle laborieuse et minutieuse, ce qui ralentit significativement le processus global de conception et d’optimisation des infrastructures de communication. Actuellement, la seule assistance disponible consiste en l’exportation d’un plan 2D depuis Revit, qui sert de base pour la définition des éléments dans Ekahau.

Cependant, cette méthode reste insuffisante pour automatiser efficacement les nombreuses étapes répétitives impliquées dans l’intégration des données. L’objectif de ce projet est donc de développer un plugin capable d’automatiser ces tâches répétitives, sans nécessiter d’analyse approfondie par un opérateur humain. Cette automatisation via code permettra d’augmenter considérablement la productivité de l’équipe en charge du modélisation et de l’analyse des réseaux, tout en améliorant la cohérence et la qualité des résultats.

### Proposition et objectifs

Pour contourner ce problème, il est proposé de développer un plugin intégré au logiciel Revit permettant l’exportation du modèle au format de fichier Ekahau. Ce plugin offrira la possibilité de sélectionner les plans du modèle à exporter, ainsi que de filtrer chaque élément contenu dans celui-ci. Le fichier exporté contiendra, par conséquent, tous les éléments nécessaires à la réalisation de l’analyse et de la simulation dans Ekahau, sans qu’il soit nécessaire de les définir manuellement.


## Échéancier

!!! info
    Le suivi complet est disponible dans la page [Suivi de projet](suivi.md).

| Jalon (*Milestone*)            | Date prévue   | Livrable                            | Statut      |
|--------------------------------|---------------|-------------------------------------|-------------|
| Ouverture de projet            | 9 mai         |                                     | ✅ Terminé  |
| Prototype 1                    | 23 mai        | Maquette + Flux d'activités         | 🔄 En cours |
| Analyse des exigences          | 30 mai        | Rapport des exigences               | 🔄 En cours |
| Prototype 1                    | 30 mai        | Exploration de l’API de Revit       | 🔄 En cours |
| Modèle de donneés              | 6 juin        | Diagramme UML ou entité-association | 🔄 En cours |
| Prototype 1                    | 23 mai        | Exploration des fichiers Ekahau     | 🔄 En cours |
| Prototype 2                    | 4 juillet     | Prototype finale + Flux             | ⏳ À venir  |
 
<!-- | Architecture                   | 30 mai        | Diagramme UML ou modèle C4          | ⏳ À venir  | -->
<!-- | Modèle de donneés              | 6 juin        | Diagramme UML ou entité-association | ⏳ À venir  | -->
<!-- | Revue de conception            | 6 juin        | Feedback encadrant + ajustements    | ⏳ À venir  | -->
<!-- | Implémentation v1              | 20 juin       | Application v1                      | ⏳ À venir  | -->
<!-- | Implémentation v2 + tests      | 11 juillet    | Application v2 + Tests              | ⏳ À venir  | -->
<!-- | Implémentation v3              | 1er août      | Version finale                      | ⏳ À venir  | -->
<!-- | Tests                          | 11-31 juillet | Plan + Résultats intermédiaires     | ⏳ À venir  | -->
<!-- | Évaluation finale              | 8 août        | Analyse des résultats + Discussion  | ⏳ À venir  | -->
<!-- | Présentation + Rapport         | 15 août       | Présentation + Rapport              | ⏳ À venir  | -->
