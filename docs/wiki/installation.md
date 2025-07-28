# Installation

1. Créer un dossier `Create` dans le dossier 'repos' de Visual Studio Community.

2. Copier le dossier `Create` (repo) et le fichier `Create.sln` à l’intérieur du dossier `Create` (local).

> Le dossier `packages/` n’est pas inclus dans le repo. Les dépendances doivent être restaurées.

### Avec Visual Studio Community

- Ouvrir le fichier `.sln` dans Visual Studio. Visual Studio détectera les packages manquants et les restaurera automatiquement.
- Si les packages ne se restaurent pas automatiquement :
    - Aller dans :
        ```
        Outils → Gestionnaire de packages NuGet → Console du gestionnaire de packages
        ```
    - Exécuter :

        ```powershell
        Update-Package -reinstall
        ```

        ou :

        ```powershell
        nuget restore
        ```

    - Compiler ensuite le projet avec `Ctrl + Maj + B`.

4. Copier le fichier 'Create.addin' dans le dossier 'Addins\<`version`>' de Revit.

5. Mettre à jour la ligne :
   
```
<Assembly>C:\Users\pelon\source\repos\Create\Create\bin\Debug\Create.dll</Assembly>
```

du fichier `Create.addin` pour qu’elle pointe vers le fichier `Create.dll` situé dans `Create\bin\Debug`.
 
Copier le contenu du dossier src\tools (repo) dans `Create\bin\Debug`.
