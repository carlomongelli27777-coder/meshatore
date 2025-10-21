using Meshatore2D.Core.Geometry;

namespace Meshatore2D.Core.Meshing;

/// <summary>
/// Mesh smoothing and optimization algorithms
/// Reference: Field, D. A. (1988). "Laplacian smoothing and Delaunay triangulations"
/// Reference: Freitag, L. A., Ollivier-Gooch, C. (1997). "Tetrahedral mesh improvement"
/// </summary>
public class MeshSmoother
{
    private const double EPSILON = 1e-10;
    private const double RELAXATION_FACTOR = 0.5; // For stability

    /// <summary>
    /// Apply Laplacian smoothing to triangular mesh
    /// </summary>
    /// <param name="triangles">Input triangular mesh</param>
    /// <param name="boundaryPoints">Fixed boundary points</param>
    /// <param name="iterations">Number of smoothing iterations</param>
    public void SmoothTriangleMesh(List<Triangle> triangles,
                                   List<Point2D> boundaryPoints,
                                   int iterations = 5)
    {
        for (int iter = 0; iter < iterations; iter++)
        {
            // Build vertex-to-triangles map
            var vertexMap = BuildVertexTriangleMap(triangles);

            // Smooth each interior vertex
            foreach (var kvp in vertexMap)
            {
                Point2D vertex = kvp.Key;

                // Skip boundary vertices
                if (IsBoundaryVertex(vertex, boundaryPoints))
                    continue;

                // Calculate new position as average of neighbors
                Point2D newPos = CalculateLaplacianPosition(vertex, kvp.Value);

                // Update vertex position in all triangles (with relaxation)
                UpdateVertexPosition(vertex, newPos, kvp.Value, RELAXATION_FACTOR);
            }
        }
    }

    /// <summary>
    /// Apply smoothing to quad-dominant mesh
    /// </summary>
    public void SmoothQuadMesh(List<Quad> quads,
                               List<Triangle> triangles,
                               List<Point2D> boundaryPoints,
                               int iterations = 5)
    {
        for (int iter = 0; iter < iterations; iter++)
        {
            // Build vertex maps for both quads and triangles
            var vertexQuadMap = BuildVertexQuadMap(quads);
            var vertexTriMap = BuildVertexTriangleMap(triangles);

            // Combine all unique vertices
            HashSet<Point2D> allVertices = new HashSet<Point2D>(new Point2DComparer());
            foreach (var v in vertexQuadMap.Keys) allVertices.Add(v);
            foreach (var v in vertexTriMap.Keys) allVertices.Add(v);

            // Smooth each interior vertex
            foreach (var vertex in allVertices)
            {
                // Skip boundary vertices
                if (IsBoundaryVertex(vertex, boundaryPoints))
                    continue;

                // Get all neighboring vertices
                List<Point2D> neighbors = GetNeighborVertices(vertex, vertexQuadMap, vertexTriMap);

                if (neighbors.Count == 0)
                    continue;

                // Calculate new position
                double avgX = neighbors.Average(p => p.X);
                double avgY = neighbors.Average(p => p.Y);
                Point2D newPos = new Point2D(avgX, avgY);

                // Update position with relaxation
                double newX = vertex.X + RELAXATION_FACTOR * (newPos.X - vertex.X);
                double newY = vertex.Y + RELAXATION_FACTOR * (newPos.Y - vertex.Y);

                vertex.X = newX;
                vertex.Y = newY;
            }
        }
    }

    /// <summary>
    /// Build map from vertex to all triangles containing it
    /// </summary>
    private Dictionary<Point2D, List<Triangle>> BuildVertexTriangleMap(List<Triangle> triangles)
    {
        var map = new Dictionary<Point2D, List<Triangle>>(new Point2DComparer());

        foreach (var triangle in triangles)
        {
            foreach (var vertex in triangle.Vertices)
            {
                // Find existing vertex or add new one
                Point2D key = FindOrAddVertex(map, vertex);

                if (!map.ContainsKey(key))
                    map[key] = new List<Triangle>();

                map[key].Add(triangle);
            }
        }

        return map;
    }

    /// <summary>
    /// Build map from vertex to all quads containing it
    /// </summary>
    private Dictionary<Point2D, List<Quad>> BuildVertexQuadMap(List<Quad> quads)
    {
        var map = new Dictionary<Point2D, List<Quad>>(new Point2DComparer());

        foreach (var quad in quads)
        {
            foreach (var vertex in quad.Vertices)
            {
                Point2D key = FindOrAddVertex(map, vertex);

                if (!map.ContainsKey(key))
                    map[key] = new List<Quad>();

                map[key].Add(quad);
            }
        }

        return map;
    }

    /// <summary>
    /// Find existing vertex in dictionary or add new one
    /// </summary>
    private Point2D FindOrAddVertex<T>(Dictionary<Point2D, T> dict, Point2D vertex)
    {
        foreach (var key in dict.Keys)
        {
            if (key.Equals(vertex, EPSILON))
                return key;
        }
        return vertex;
    }

    /// <summary>
    /// Check if vertex is on boundary
    /// </summary>
    private bool IsBoundaryVertex(Point2D vertex, List<Point2D> boundaryPoints)
    {
        return boundaryPoints.Any(bp => bp.Equals(vertex, EPSILON));
    }

    /// <summary>
    /// Calculate new vertex position using Laplacian smoothing
    /// </summary>
    private Point2D CalculateLaplacianPosition(Point2D vertex, List<Triangle> adjacentTriangles)
    {
        // Get all neighbor vertices
        HashSet<Point2D> neighbors = new HashSet<Point2D>(new Point2DComparer());

        foreach (var triangle in adjacentTriangles)
        {
            foreach (var v in triangle.Vertices)
            {
                if (!v.Equals(vertex, EPSILON))
                    neighbors.Add(v);
            }
        }

        if (neighbors.Count == 0)
            return vertex;

        // Average position of neighbors
        double avgX = neighbors.Average(p => p.X);
        double avgY = neighbors.Average(p => p.Y);

        return new Point2D(avgX, avgY);
    }

    /// <summary>
    /// Update vertex position in all associated triangles
    /// </summary>
    private void UpdateVertexPosition(Point2D oldVertex, Point2D newVertex,
                                      List<Triangle> triangles, double relaxation)
    {
        // Calculate relaxed position
        double newX = oldVertex.X + relaxation * (newVertex.X - oldVertex.X);
        double newY = oldVertex.Y + relaxation * (newVertex.Y - oldVertex.Y);

        // Update the vertex (modifies in place)
        oldVertex.X = newX;
        oldVertex.Y = newY;
    }

    /// <summary>
    /// Get neighbor vertices from quad and triangle maps
    /// </summary>
    private List<Point2D> GetNeighborVertices(Point2D vertex,
                                               Dictionary<Point2D, List<Quad>> quadMap,
                                               Dictionary<Point2D, List<Triangle>> triMap)
    {
        HashSet<Point2D> neighbors = new HashSet<Point2D>(new Point2DComparer());

        // Get neighbors from quads
        if (quadMap.ContainsKey(vertex))
        {
            foreach (var quad in quadMap[vertex])
            {
                foreach (var v in quad.Vertices)
                {
                    if (!v.Equals(vertex, EPSILON))
                        neighbors.Add(v);
                }
            }
        }

        // Get neighbors from triangles
        if (triMap.ContainsKey(vertex))
        {
            foreach (var triangle in triMap[vertex])
            {
                foreach (var v in triangle.Vertices)
                {
                    if (!v.Equals(vertex, EPSILON))
                        neighbors.Add(v);
                }
            }
        }

        return neighbors.ToList();
    }

    /// <summary>
    /// Custom comparer for Point2D to handle floating point comparison
    /// </summary>
    private class Point2DComparer : IEqualityComparer<Point2D>
    {
        public bool Equals(Point2D? x, Point2D? y)
        {
            if (x == null || y == null) return false;
            return x.Equals(y, EPSILON);
        }

        public int GetHashCode(Point2D obj)
        {
            // Round to avoid precision issues
            int xHash = ((int)(obj.X / EPSILON)).GetHashCode();
            int yHash = ((int)(obj.Y / EPSILON)).GetHashCode();
            return xHash ^ yHash;
        }
    }
}
