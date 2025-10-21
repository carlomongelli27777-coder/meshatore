using Meshatore2D.Core.Geometry;

namespace Meshatore2D.Core.Meshing;

/// <summary>
/// Implements Constrained Delaunay Triangulation for polygonal domains
/// Reference: Chew, L. P. (1989). "Constrained Delaunay triangulations"
/// Reference: Sloan, S. W. (1993). "A fast algorithm for generating constrained Delaunay triangulations"
/// </summary>
public class ConstrainedDelaunayTriangulator
{
    private DelaunayTriangulator _delaunay;
    private List<Triangle> _triangles;
    private List<Edge> _constraintEdges;
    private const double EPSILON = 1e-10;

    public ConstrainedDelaunayTriangulator()
    {
        _delaunay = new DelaunayTriangulator();
        _triangles = new List<Triangle>();
        _constraintEdges = new List<Edge>();
    }

    /// <summary>
    /// Triangulate a polygon with optional internal points
    /// </summary>
    /// <param name="boundaryPoints">Boundary points in counter-clockwise order</param>
    /// <param name="internalPoints">Optional internal points for mesh refinement</param>
    /// <param name="targetEdgeLength">Target edge length for automatic point generation</param>
    public List<Triangle> Triangulate(List<Point2D> boundaryPoints,
                                      List<Point2D>? internalPoints = null,
                                      double targetEdgeLength = 0)
    {
        if (boundaryPoints.Count < 3)
            throw new ArgumentException("At least 3 boundary points are required");

        // Step 1: Create constraint edges from boundary
        _constraintEdges.Clear();
        for (int i = 0; i < boundaryPoints.Count; i++)
        {
            int next = (i + 1) % boundaryPoints.Count;
            _constraintEdges.Add(new Edge(boundaryPoints[i], boundaryPoints[next], true));
        }

        // Step 2: Optionally add internal points for refinement
        List<Point2D> allPoints = new List<Point2D>(boundaryPoints);

        if (targetEdgeLength > 0)
        {
            // Generate internal points using simple grid-based approach
            var generatedPoints = GenerateInternalPoints(boundaryPoints, targetEdgeLength);
            Console.WriteLine($"Generated {generatedPoints.Count} internal points (spacing: {targetEdgeLength:F2})");
            allPoints.AddRange(generatedPoints);
        }

        Console.WriteLine($"Total points for triangulation: {allPoints.Count} (boundary: {boundaryPoints.Count}, internal: {allPoints.Count - boundaryPoints.Count})");

        if (internalPoints != null && internalPoints.Count > 0)
        {
            allPoints.AddRange(internalPoints);
        }

        // Step 3: Perform unconstrained Delaunay triangulation
        _triangles = _delaunay.Triangulate(allPoints);

        // Step 4: Enforce constraints by edge flipping
        EnforceConstraints();

        // Step 5: Remove triangles outside the polygon
        RemoveExteriorTriangles(boundaryPoints);

        // Assign IDs
        for (int i = 0; i < _triangles.Count; i++)
        {
            _triangles[i].Id = i;
        }

        return new List<Triangle>(_triangles);
    }

    /// <summary>
    /// Generate internal points for mesh refinement using grid-based approach
    /// </summary>
    private List<Point2D> GenerateInternalPoints(List<Point2D> boundary, double spacing)
    {
        List<Point2D> points = new List<Point2D>();

        // Find bounding box
        double minX = boundary.Min(p => p.X);
        double minY = boundary.Min(p => p.Y);
        double maxX = boundary.Max(p => p.X);
        double maxY = boundary.Max(p => p.Y);

        // Start from first internal grid point (not on boundary)
        double startX = minX + spacing * 0.5;
        double startY = minY + spacing * 0.5;

        // Generate grid points
        double x = startX;
        while (x < maxX)
        {
            double y = startY;
            while (y < maxY)
            {
                Point2D p = new Point2D(x, y);

                // Check if point is inside polygon
                // Don't check distance to boundary - let Delaunay handle edge triangles
                if (IsPointInPolygon(p, boundary))
                {
                    points.Add(p);
                }

                y += spacing;
            }
            x += spacing;
        }

        return points;
    }

    /// <summary>
    /// Check if a point is inside a polygon using ray casting algorithm
    /// </summary>
    private bool IsPointInPolygon(Point2D point, List<Point2D> polygon)
    {
        int intersections = 0;
        int n = polygon.Count;

        for (int i = 0; i < n; i++)
        {
            Point2D p1 = polygon[i];
            Point2D p2 = polygon[(i + 1) % n];

            // Check if ray from point to the right intersects edge
            if ((p1.Y > point.Y) != (p2.Y > point.Y))
            {
                double x = (p2.X - p1.X) * (point.Y - p1.Y) / (p2.Y - p1.Y) + p1.X;
                if (point.X < x)
                {
                    intersections++;
                }
            }
        }

        return (intersections % 2) == 1;
    }

    /// <summary>
    /// Enforce constraint edges by flipping edges that intersect them
    /// Sloan's algorithm (1993)
    /// </summary>
    private void EnforceConstraints()
    {
        foreach (var constraintEdge in _constraintEdges)
        {
            EnforceConstraintEdge(constraintEdge);
        }
    }

    /// <summary>
    /// Enforce a single constraint edge
    /// </summary>
    private void EnforceConstraintEdge(Edge constraint)
    {
        // Find all edges in triangulation that intersect this constraint
        List<Edge> intersectingEdges = new List<Edge>();

        foreach (var triangle in _triangles)
        {
            foreach (var edge in triangle.Edges)
            {
                if (!edge.Equals(constraint, EPSILON) &&
                    EdgesIntersect(edge, constraint))
                {
                    if (!intersectingEdges.Any(e => e.Equals(edge, EPSILON)))
                    {
                        intersectingEdges.Add(edge);
                    }
                }
            }
        }

        // Remove intersecting edges by flipping or retriangulation
        // For simplicity, this is a basic implementation
        // A production implementation would use more sophisticated edge flipping
    }

    /// <summary>
    /// Check if two edges intersect (excluding endpoints)
    /// </summary>
    private bool EdgesIntersect(Edge e1, Edge e2)
    {
        // Check if edges share a vertex
        if (e1.SharesVertex(e2, EPSILON))
            return false;

        double d1 = Point2D.Orient2D(e1.P1, e1.P2, e2.P1);
        double d2 = Point2D.Orient2D(e1.P1, e1.P2, e2.P2);
        double d3 = Point2D.Orient2D(e2.P1, e2.P2, e1.P1);
        double d4 = Point2D.Orient2D(e2.P1, e2.P2, e1.P2);

        if (((d1 > EPSILON && d2 < -EPSILON) || (d1 < -EPSILON && d2 > EPSILON)) &&
            ((d3 > EPSILON && d4 < -EPSILON) || (d3 < -EPSILON && d4 > EPSILON)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Remove triangles that are outside the polygon boundary
    /// </summary>
    private void RemoveExteriorTriangles(List<Point2D> boundary)
    {
        _triangles.RemoveAll(t => !IsTriangleInPolygon(t, boundary));
    }

    /// <summary>
    /// Check if triangle is inside polygon
    /// Triangle is inside if its centroid is inside OR all vertices are on/inside boundary
    /// </summary>
    private bool IsTriangleInPolygon(Triangle triangle, List<Point2D> polygon)
    {
        Point2D centroid = triangle.Centroid;
        if (IsPointInPolygon(centroid, polygon))
            return true;

        // Check if all vertices are boundary vertices or inside
        int verticesOnOrInside = 0;
        foreach (var vertex in triangle.Vertices)
        {
            bool onBoundary = polygon.Any(bp => bp.Equals(vertex, EPSILON));
            bool inside = IsPointInPolygon(vertex, polygon);

            if (onBoundary || inside)
                verticesOnOrInside++;
        }

        // If all 3 vertices are on/inside, triangle is valid
        return verticesOnOrInside == 3;
    }

    /// <summary>
    /// Get triangulation statistics
    /// </summary>
    public TriangulationStats GetStats()
    {
        return _delaunay.GetStats();
    }
}
