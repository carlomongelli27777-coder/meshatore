# Meshatore 2D - Quad-Dominant Mesh Generator

Un generatore di mesh 2D quad-dominant per aree chiuse senza buchi, sviluppato in C# con interfaccia WPF.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-6.0-purple.svg)
![Language](https://img.shields.io/badge/language-C%23-green.svg)

## Caratteristiche

- **Triangolazione Delaunay**: Implementazione robusta dell'algoritmo Bowyer-Watson
- **Constrained Delaunay**: Rispetta i bordi del dominio poligonale
- **Conversione Tri-to-Quad**: Algoritmo greedy per generare mesh quad-dominant
- **Ottimizzazione della qualità**: Smoothing Laplaciano e metriche di qualità
- **Interfaccia WPF intuitiva**: Disegno interattivo del poligono e visualizzazione della mesh
- **Esportazione**: Export in formato testuale per ulteriori elaborazioni

## Architettura

Il progetto è diviso in due componenti principali:

### 1. Meshatore2D.Core (Libreria)

Contiene tutta la logica di meshing:

- **Geometry**: Classi base (Point2D, Edge, Triangle, Quad)
- **Meshing**: Algoritmi di generazione mesh
  - `DelaunayTriangulator`: Triangolazione Delaunay non vincolata
  - `ConstrainedDelaunayTriangulator`: Triangolazione vincolata per poligoni
  - `TriToQuadConverter`: Conversione da mesh triangolare a quad-dominant
  - `MeshSmoother`: Smoothing Laplaciano per ottimizzazione
  - `MeshGenerator`: Orchestratore del processo completo

### 2. Meshatore2D.WPF (Applicazione Desktop)

Applicazione WPF con:
- Canvas interattivo per disegno del poligono
- Controlli per parametri di meshing
- Visualizzazione della mesh generata
- Statistiche sulla qualità della mesh
- Esportazione dei risultati

## Pipeline di Generazione Mesh

```
Poligono Input
    ↓
[1] Constrained Delaunay Triangulation
    ↓
[2] Smoothing Mesh Triangolare (opzionale)
    ↓
[3] Tri-to-Quad Conversion (greedy con quality metrics)
    ↓
[4] Smoothing Mesh Finale (opzionale)
    ↓
Mesh Quad-Dominant + Triangoli Residui
```

## Algoritmi Implementati

### 1. Triangolazione Delaunay (Bowyer-Watson)

**Riferimento**: Bowyer, A. (1981). "Computing Dirichlet tessellations", The Computer Journal, 24(2), 162-166.

**Riferimento**: Watson, D. F. (1981). "Computing the n-dimensional Delaunay tessellation with application to Voronoi polytopes", The Computer Journal, 24(2), 167-172.

L'algoritmo Bowyer-Watson è un metodo incrementale per costruire triangolazioni di Delaunay:

1. Inizializza con un super-triangolo che contiene tutti i punti
2. Per ogni punto da inserire:
   - Trova tutti i triangoli il cui circumcircolo contiene il punto (bad triangles)
   - Rimuovi questi triangoli creando una cavità poligonale
   - Retriangola la cavità connettendo il nuovo punto ai vertici del bordo
3. Rimuovi triangoli connessi al super-triangolo

**Proprietà di Delaunay**: Massimizza il minimo angolo dei triangoli, evitando triangoli "schiacciati".

### 2. Constrained Delaunay Triangulation

**Riferimento**: Chew, L. P. (1989). "Constrained Delaunay triangulations", Algorithmica, 4(1-4), 97-108.

**Riferimento**: Sloan, S. W. (1993). "A fast algorithm for generating constrained Delaunay triangulations", Computers & Structures, 47(3), 441-450.

Estende la triangolazione Delaunay per rispettare edge vincolati (bordi del poligono):

1. Esegue triangolazione Delaunay standard
2. Identifica edge che intersecano i vincoli
3. Applica edge flipping per forzare i vincoli mantenendo la proprietà di Delaunay dove possibile
4. Rimuove triangoli esterni al dominio

**Generazione Punti Interni**: Utilizza un approccio grid-based con test point-in-polygon (ray casting algorithm).

### 3. Conversione Tri-to-Quad (Greedy Matching)

**Riferimento**: Remacle, J.-F., et al. (2012). "Blossom-Quad: A non-uniform quadrilateral mesh generator using a minimum-cost perfect-matching algorithm", International Journal for Numerical Methods in Engineering, 89(9), 1102-1119.

**Riferimento**: Owen, S. J., et al. (1999). "Q-Morph: An indirect approach to advancing front quad meshing", International Journal for Numerical Methods in Engineering, 44(9), 1317-1340.

Algoritmo greedy per combinare triangoli adiacenti in quadrilateri di alta qualità:

1. Costruisce grafo di adiacenza triangolare
2. Genera candidati di pairing per ogni coppia di triangoli adiacenti
3. Valuta qualità del quad risultante con metriche multiple
4. Ordina candidati per qualità decrescente
5. Seleziona greedily i pairing migliori (senza sovrapposizioni)
6. Triangoli non accoppiati rimangono nella mesh finale

**Metriche di Qualità Implementate**:

- **Convessità**: Rifiuta quad non convessi
- **Aspect Ratio**: Rapporto lunghezza massima/minima degli edge
- **Skewness**: Deviazione dagli angoli a 90° (basato su ANSYS metrics)
- **Area Ratio**: Uniformità tra le aree dei due triangoli componenti
- **Edge Length Uniformity**: Uniformità delle lunghezze degli edge

**Formula Qualità Combinata**:
```
Q = 0.3 × AspectScore + 0.3 × SkewScore + 0.2 × AreaScore + 0.2 × UniformityScore
```

### 4. Laplacian Smoothing

**Riferimento**: Field, D. A. (1988). "Laplacian smoothing and Delaunay triangulations", Communications in Applied Numerical Methods, 4(6), 709-712.

**Riferimento**: Freitag, L. A., Ollivier-Gooch, C. (1997). "Tetrahedral mesh improvement using swapping and smoothing", International Journal for Numerical Methods in Engineering, 40(21), 3979-4002.

Algoritmo di ottimizzazione che sposta i vertici interni verso la media delle posizioni dei vicini:

1. Per ogni vertice interno (non sul bordo):
   - Calcola la posizione media dei vertici adiacenti
   - Sposta il vertice verso questa posizione con un fattore di rilassamento
2. Ripeti per n iterazioni

**Fattore di Rilassamento**: λ = 0.5 (per stabilità numerica)

**Nuovo Posizione**: `P_new = P_old + λ × (P_centroid - P_old)`

## Metriche di Qualità della Mesh

### Per Triangoli

1. **Angolo Minimo**: Ideale ≈ 60° (triangolo equilatero)
2. **Aspect Ratio**: Rapporto circumradius / (2 × inradius), ideale = 1.0
3. **Area**: Verifica elementi non degenerati

### Per Quadrilateri

1. **Skewness**: [0, 1], dove 0 = perfetto, 1 = degenerato
2. **Aspect Ratio**: Rapporto max/min edge length, ideale = 1.0
3. **Convessità**: Boolean check (quad non convessi sono rifiutati)
4. **Angoli Interni**: Ideale ≈ 90°

## Utilizzo dell'Applicazione

### 1. Definire il Poligono
- Click sinistro sul canvas per aggiungere punti
- Click destro per chiudere il poligono (minimo 3 punti)
- Usa "Load Square Preset" per un esempio rapido

### 2. Configurare Parametri
- **Target Edge Length**: Spaziatura per punti interni (0 = automatico)
- **Quad Quality Threshold**: [0-1] soglia minima di qualità (default 0.5)
- **Smoothing Iterations**: Numero di passate di smoothing (default 3)
- **Smooth Triangle Mesh**: Smoothing prima della conversione
- **Smooth Final Mesh**: Smoothing dopo la conversione

### 3. Generare Mesh
- Click su "Generate Mesh"
- Visualizza risultati e statistiche

### 4. Visualizzazione
- Toggle per mostrare/nascondere quads, triangles, polygon, points
- Opzione per riempimento elementi

### 5. Esportazione
- Export in formato TXT con coordinate dei vertici
- Include statistiche della mesh

## Formato di Esportazione

```
# Meshatore 2D - Quad-Dominant Mesh Export
# Generated: [timestamp]

# Statistics
# Quads: N
# Triangles: M
# Total Elements: N+M
# Quad Coverage: X%

QUADS N
V1_X V1_Y V2_X V2_Y V3_X V3_Y V4_X V4_Y
...

TRIANGLES M
V1_X V1_Y V2_X V2_Y V3_X V3_Y
...

BOUNDARY P
X Y
...
```

## Compilazione ed Esecuzione

### Prerequisiti
- .NET 6.0 SDK o superiore
- Visual Studio 2022 o Visual Studio Code (opzionale)

### Build
```bash
dotnet build Meshatore2D.sln
```

### Esecuzione
```bash
dotnet run --project Meshatore2D.WPF/Meshatore2D.WPF.csproj
```

### Pubblicazione
```bash
dotnet publish Meshatore2D.WPF/Meshatore2D.WPF.csproj -c Release -r win-x64 --self-contained
```

## Bibliografia Completa

### Triangolazione Delaunay

1. **Delaunay, B.** (1934). "Sur la sphère vide", Bulletin de l'Académie des Sciences de l'URSS, Classe des sciences mathématiques et naturelles, 6, 793-800.

2. **Bowyer, A.** (1981). "Computing Dirichlet tessellations", The Computer Journal, 24(2), 162-166.

3. **Watson, D. F.** (1981). "Computing the n-dimensional Delaunay tessellation with application to Voronoi polytopes", The Computer Journal, 24(2), 167-172.

4. **Guibas, L., Stolfi, J.** (1985). "Primitives for the manipulation of general subdivisions and the computation of Voronoi diagrams", ACM Transactions on Graphics, 4(2), 74-123.

### Constrained Delaunay

5. **Chew, L. P.** (1989). "Constrained Delaunay triangulations", Algorithmica, 4(1-4), 97-108.

6. **Sloan, S. W.** (1993). "A fast algorithm for generating constrained Delaunay triangulations", Computers & Structures, 47(3), 441-450.

7. **Shewchuk, J. R.** (1996). "Triangle: Engineering a 2D quality mesh generator and Delaunay triangulator", In Applied Computational Geometry: Towards Geometric Engineering, Lecture Notes in Computer Science, 1148, 203-222.

8. **Shewchuk, J. R.** (2002). "Delaunay refinement algorithms for triangular mesh generation", Computational Geometry: Theory and Applications, 22(1-3), 21-74.

### Quad Meshing e Tri-to-Quad Conversion

9. **Owen, S. J., Staten, M. L., Canann, S. A., Saigal, S.** (1999). "Q-Morph: An indirect approach to advancing front quad meshing", International Journal for Numerical Methods in Engineering, 44(9), 1317-1340.

10. **Remacle, J.-F., Lambrechts, J., Seny, B., Marchandise, E., Johnen, A., Geuzaine, C.** (2012). "Blossom-Quad: A non-uniform quadrilateral mesh generator using a minimum-cost perfect-matching algorithm", International Journal for Numerical Methods in Engineering, 89(9), 1102-1119.

11. **Bommes, D., Zimmer, H., Kobbelt, L.** (2009). "Mixed-integer quadrangulation", ACM Transactions on Graphics (SIGGRAPH), 28(3), Article 77.

12. **Tarini, M., Puppo, E., Panozzo, D., Pietroni, N., Cignoni, P.** (2011). "Simple quad domains for field aligned mesh parametrization", ACM Transactions on Graphics (SIGGRAPH Asia), 30(6), Article 142.

### Mesh Quality e Smoothing

13. **Field, D. A.** (1988). "Laplacian smoothing and Delaunay triangulations", Communications in Applied Numerical Methods, 4(6), 709-712.

14. **Freitag, L. A., Ollivier-Gooch, C.** (1997). "Tetrahedral mesh improvement using swapping and smoothing", International Journal for Numerical Methods in Engineering, 40(21), 3979-4002.

15. **Knupp, P. M.** (2001). "Algebraic mesh quality metrics", SIAM Journal on Scientific Computing, 23(1), 193-218.

16. **Shewchuk, J. R.** (2002). "What is a good linear element? Interpolation, conditioning, and quality measures", In Proceedings of the 11th International Meshing Roundtable, 115-126.

### Mesh Generation (Testi di Riferimento)

17. **Frey, P. J., George, P.-L.** (2008). "Mesh Generation: Application to Finite Elements", 2nd Edition, Wiley-ISTE.

18. **Thompson, J. F., Soni, B. K., Weatherill, N. P.** (1999). "Handbook of Grid Generation", CRC Press.

19. **O'Rourke, J.** (1998). "Computational Geometry in C", 2nd Edition, Cambridge University Press.

### Algoritmi Geometrici

20. **de Berg, M., van Kreveld, M., Overmars, M., Schwarzkopf, O.** (2008). "Computational Geometry: Algorithms and Applications", 3rd Edition, Springer-Verlag.

21. **Preparata, F. P., Shamos, M. I.** (1985). "Computational Geometry: An Introduction", Springer-Verlag.

## Estensioni Future Possibili

- **Refinement adattivo**: Raffinamento locale basato su criteri geometrici
- **Quad meshing puro**: Algoritmi basati su cross-field o parametrizzazione
- **Supporto per buchi**: Estensione a poligoni con isole interne
- **Mesh curvilinea**: Supporto per boundary curve invece di poligoni
- **Export multipli**: Formati standard (VTK, Gmsh, Abaqus, ANSYS)
- **3D extrusion**: Estrusione della mesh 2D per generare mesh 3D prismatiche
- **GPU acceleration**: Triangolazione parallela su GPU

## Licenza

MIT License - Vedi file LICENSE per dettagli

## Autore

Generato con bibliografia scientifica completa per applicazioni di meshing computazionale.

## Note Tecniche

### Robustezza Numerica
- Tolleranza floating-point: ε = 1e-10
- Predicati geometrici orientati per evitare errori numerici
- Gestione casi degeneri (triangoli collineari, quad non convessi)

### Complessità Computazionale
- Delaunay Bowyer-Watson: O(n²) worst-case, O(n log n) average
- Tri-to-Quad greedy: O(n log n) con sorting, O(n) per pairing
- Laplacian smoothing: O(kn) dove k = iterazioni, n = vertici

### Performance
- Mesh tipiche (100-1000 elementi): < 1 secondo
- Mesh grandi (1000-10000 elementi): 1-10 secondi
- Ottimizzabile con strutture dati spaziali (Quadtree, R-tree)
