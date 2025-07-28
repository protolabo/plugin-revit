# Systèmes de coordonnées Revit et Ekahau

## Conversion des coordonnées de Revit vers Ekahau

Pour convertir les coordonnées de Revit en coordonnées utilisables dans Ekahau, deux éléments essentiels doivent être pris en compte :

- Revit utilise un système de coordonnées absolu en **pieds**.
- Ekahau utilise un système de coordonnées absolu en **pixels**.

### Exemple

![Axis Revit vs Ekahau](images/axis.png)

Dans l'image ci-dessus :

- À gauche, nous avons une vue hypothétique de Revit. Le système de coordonnées absolu y est représenté par les flèches **verte** (axe Y) et **rouge** (axe X), ainsi qu’un **Crop Region** (région de découpe). Étant donné que l'origine est **absolue** et commune à toutes les vues, le centre de la région de découpe ne coïncide pas nécessairement avec l’origine du système de coordonnées.
- À droite, nous voyons une **image** (Map) dans Ekahau. Remarquez que l'origine de la carte se trouve dans le coin supérieur gauche, ce qui signifie qu’il **n’existe pas de coordonnées négatives dans Ekahau**.

La taille de l’image dépend de la taille définie par l’utilisateur dans Revit. Pour cet exemple, nous avons exporté une image de **1500 x 1350 pixels**, afin de respecter les proportions de la vue dans Revit.

#### Calcul d’un point

Dans cet exemple, nous avons placé un point dans Revit à la position **(-2, -3)**.

- La largeur totale de la région de découpe est de **10 pieds**.
- La hauteur totale est de **9 pieds**.

Pour convertir cette position vers Ekahau, il faut calculer la proportion du point par rapport à la taille de la Crop Region, en partant de la **position de l’origine de l’image Ekahau** (coin supérieur gauche).

##### Calcul des proportions

- Pour **X** : entre -6 et -2, il y a 4 pieds → 4 / 10 = **0,4** (soit 40 %)
- Pour **Y** : entre 3 et -3, il y a 6 pieds → 6 / 9 = **2/3**

##### Conversion en pixels

- Pour **X** : 0,4 × 1500 = **600 pixels**
- Pour **Y** : (2/3) × 1350 = **900 pixels**

#### Conclusion

La position du point **(-2, -3)** dans Revit correspond à la position **(600, 900)** dans Ekahau.

### Correspondance d’échelle Revit - Ekahau 

#### Définir l’échelle dans Ekahau

Pour définir l’échelle dans Ekahau, il suffit d’ajouter une ligne `"metersPerUnit"` avec la valeur appropriée pour chaque étage dans le fichier `floorPlans.json`.

Cette valeur se calcule en divisant la taille réelle (en mètres) d’un élément connu — comme un mur, une porte ou une fenêtre — par sa longueur en pixels sur l’image.

##### Exemple

Prenons l’exemple précédent : supposons qu’il y ait un mur qui va du point **(-2, -3)** jusqu’à l’origine absolue **(0, 0)**.

- La position du point **(-2, -3)** dans Ekahau est **(600, 900)** pixels.
- L’origine (0, 0) correspond à **(900, 450)** pixels.

###### 1. Longueur réelle du mur en pieds

On utilise le théorème de Pythagore :

Longueur = √[(-2)² + (-3)²]  
Longueur = √(4 + 9) = √13 ≈ **3,6056 pieds**

Converti en mètres :  
3,6056 × 0,3048 ≈ **1,0998 mètres**

###### 2. Longueur du mur en pixels

Longueur = √[(900 - 600)² + (900 - 450)²]  
Longueur = √(300² + 450²) = √(202500 + 90000)  
Longueur ≈ **540,83 pixels**

###### 3. Calcul de l’échelle

metersPerUnit = 1,0998 / 540,83 ≈ **0,002033**

##### Conclusion

Dans cet exemple, la valeur à insérer dans `floorPlans.json` serait :

```json
"metersPerUnit": 0.002033
```

![result scale basic model](images/scale_basic.png)
![result scale advance model](images/scale_advance.png)
