using Meshatore2D.Core.Geometry;

namespace Meshatore2D.Core.Meshing;

/// <summary>
/// Implements Delaunay triangulation using Bowyer-Watson algorithm
/// Reference: Bowyer, A. (1981). "Computing Dirichlet tessellations"
/// Reference: Watson, D. F. (1981). "Computing the n-dimensional Delaunay tessellation"
/// </summary>
public class DelaunayTriangulator
{
    private List<Triangle> _triangles;
    private List<Point2D> _points;
    private const double EPSILON = 1e-10;

    public DelaunayTriangulator()
    {
        _triangles = new List<Triangle>();
        _points = new List<Point2D>();
    }

    /// <summary>
    /// Triangulate a set of points using Bowyer-Watson algorithm
    /// </summary>
    public List<Triangle> Triangulate(List<Point2D> points)
    {
        if (points.Count < 3)
            throw new ArgumentException("At least 3 points are required for triangulation");

        _points = new List<Point2D>(points);
        _triangles = new List<Triangle>();

        // Step 1: Create super-triangle that contains all points
        Triangle superTriangle = CreateSuperTriangle(points);
        _triangles.Add(superTriangle);

        // Step 2: Add points one by one
        foreach (var point in points)
        {
            AddPoint(point);
        }

        // Step 3: Remove triangles that share vertices with super-triangle
        RemoveSuperTriangleElements(superTriangle);

        // Assign IDs to triangles
        for (int i = 0; i < _triangles.Count; i++)
        {
            _triangles[i].Id = i;
        }

        return new List<Triangle>(_triangles);
    }

    /// <summary>
    /// Create a super-triangle that encompasses all points
    /// </summary>
    private Triangle CreateSuperTriangle(List<Point2D> points)
    {
        // Find bounding box
        double minX = points.Min(p => p.X);
        double minY = points.Min(p => p.Y);
        double maxX = points.Max(p => p.X);
        double maxY = points.Max(p => p.Y);

        double dx = maxX - minX;
        double dy = maxY - minY;
        double deltaMax = Math.Max(dx, dy);
        double midX = (minX + maxX) / 2.0;
        double midY = (minY + maxY) / 2.0;

        // Create super-triangle vertices (much larger than bounding box)
        Point2D p1 = new Point2D(midX - 20 * deltaMax, midY - deltaMax, -1);
        Point2D p2 = new Point2D(midX, midY + 20 * deltaMax, -2);
        Point2D p3 = new Point2D(midX + 20 * deltaMax, midY - deltaMax, -3);

        return new Triangle(p1, p2, p3, -1);
    }

    /// <summary>
    /// Add a point to the triangulation (Bowyer-Watson algorithm)
    /// </summary>
    private void AddPoint(Point2D point)
    {
        List<Edge> polygon = new List<Edge>();
        List<Triangle> badTriangles = new List<Triangle>();

        // Step 1: Find all triangles whose circumcircle contains the point
        foreach (var triangle in _triangles)
        {
            if (triangle.PointInCircumcircle(point, EPSILON))
            {
                badTriangles.Add(triangle);
            }
        }

        // Step 2: Find the boundary of the polygonal hole (bad triangles)
        foreach (var triangle in badTriangles)
        {
            var edges = triangle.Edges;
            foreach (var edge in edges)
            {
                bool isShared = false;

                // Check if this edge is shared with another bad triangle
                foreach (var otherTriangle in badTriangles)
                {
                    if (triangle == otherTriangle) continue;

                    var otherEdges = otherTriangle.Edges;
                    foreach (var otherEdge in otherEdges)
                    {
                        if (edge.Equals(otherEdge, EPSILON))
                        {
                            isShared = true;
                            break;
                        }
                    }

                    if (isShared) break;
                }

                // If edge is not shared, it's part of the polygon boundary
                if (!isShared)
                {
                    polygon.Add(edge);
                }
            }
        }

        // Step 3: Remove bad triangles
        foreach (var triangle in badTriangles)
        {
            _triangles.Remove(triangle);
        }

        // Step 4: Create new triangles from the polygon edges to the new point
        foreach (var edge in polygon)
        {
            Triangle newTriangle = new Triangle(edge.P1, edge.P2, point);
            _triangles.Add(newTriangle);
        }
    }

    /// <summary>
    /// Remove triangles connected to super-triangle vertices
    /// </summary>
    private void RemoveSuperTriangleElements(Triangle superTriangle)
    {
        _triangles.RemoveAll(t =>
            t.ContainsVertex(superTriangle.V1, EPSILON) ||
            t.ContainsVertex(superTriangle.V2, EPSILON) ||
            t.ContainsVertex(superTriangle.V3, EPSILON)
        );
    }

    /// <summary>
    /// Get triangulation statistics
    /// </summary>
    public TriangulationStats GetStats()
    {
        if (_triangles.Count == 0)
            return new TriangulationStats();

        return new TriangulationStats
        {
            TriangleCount = _triangles.Count,
            PointCount = _points.Count,
            MinAngle = _triangles.Min(t => t.MinAngle),
            MaxAngle = _triangles.Max(t => t.MinAngle),
            AvgAngle = _triangles.Average(t => t.MinAngle),
            MinAspectRatio = _triangles.Min(t => t.AspectRatio),
            MaxAspectRatio = _triangles.Max(t => t.AspectRatio),
            AvgAspectRatio = _triangles.Average(t => t.AspectRatio)
        };
    }
}

/// <summary>
/// Statistics about the triangulation quality
/// </summary>
public class TriangulationStats
{
    public int TriangleCount { get; set; }
    public int PointCount { get; set; }
    public double MinAngle { get; set; }
    public double MaxAngle { get; set; }
    public double AvgAngle { get; set; }
    public double MinAspectRatio { get; set; }
    public double MaxAspectRatio { get; set; }
    public double AvgAspectRatio { get; set; }

    public override string ToString()
    {
        return $"Triangulation: {TriangleCount} triangles, {PointCount} points\n" +
               $"Min Angle: {MinAngle:F2}°, Avg: {AvgAngle:F2}°, Max: {MaxAngle:F2}°\n" +
               $"Aspect Ratio - Min: {MinAspectRatio:F2}, Avg: {AvgAspectRatio:F2}, Max: {MaxAspectRatio:F2}";
    }
}
