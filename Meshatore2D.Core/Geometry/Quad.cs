namespace Meshatore2D.Core.Geometry;

/// <summary>
/// Represents a quadrilateral element in the mesh
/// Vertices are ordered counter-clockwise
/// </summary>
public class Quad
{
    public Point2D V1 { get; set; }
    public Point2D V2 { get; set; }
    public Point2D V3 { get; set; }
    public Point2D V4 { get; set; }
    public int Id { get; set; }

    public Quad(Point2D v1, Point2D v2, Point2D v3, Point2D v4, int id = -1)
    {
        V1 = v1;
        V2 = v2;
        V3 = v3;
        V4 = v4;
        Id = id;
    }

    /// <summary>
    /// Get quad vertices as array
    /// </summary>
    public Point2D[] Vertices => new[] { V1, V2, V3, V4 };

    /// <summary>
    /// Get quad edges
    /// </summary>
    public Edge[] Edges => new[]
    {
        new Edge(V1, V2),
        new Edge(V2, V3),
        new Edge(V3, V4),
        new Edge(V4, V1)
    };

    /// <summary>
    /// Calculate quad area (sum of two triangles)
    /// </summary>
    public double Area
    {
        get
        {
            // Split into two triangles: (V1,V2,V3) and (V1,V3,V4)
            double area1 = 0.5 * Math.Abs(
                (V2.X - V1.X) * (V3.Y - V1.Y) -
                (V3.X - V1.X) * (V2.Y - V1.Y)
            );
            double area2 = 0.5 * Math.Abs(
                (V3.X - V1.X) * (V4.Y - V1.Y) -
                (V4.X - V1.X) * (V3.Y - V1.Y)
            );
            return area1 + area2;
        }
    }

    /// <summary>
    /// Calculate quad centroid
    /// </summary>
    public Point2D Centroid => new Point2D(
        (V1.X + V2.X + V3.X + V4.X) / 4.0,
        (V1.Y + V2.Y + V3.Y + V4.Y) / 4.0
    );

    /// <summary>
    /// Check if quad is convex
    /// All cross products should have the same sign
    /// </summary>
    public bool IsConvex
    {
        get
        {
            double cross1 = Point2D.Orient2D(V1, V2, V3);
            double cross2 = Point2D.Orient2D(V2, V3, V4);
            double cross3 = Point2D.Orient2D(V3, V4, V1);
            double cross4 = Point2D.Orient2D(V4, V1, V2);

            // Check if all have same sign
            return (cross1 > 0 && cross2 > 0 && cross3 > 0 && cross4 > 0) ||
                   (cross1 < 0 && cross2 < 0 && cross3 < 0 && cross4 < 0);
        }
    }

    /// <summary>
    /// Calculate aspect ratio quality metric
    /// Ratio of maximum edge length to minimum edge length
    /// Perfect square has ratio = 1
    /// </summary>
    public double AspectRatio
    {
        get
        {
            var edges = Edges;
            double maxLen = edges.Max(e => e.Length);
            double minLen = edges.Min(e => e.Length);

            return minLen > 1e-10 ? maxLen / minLen : double.MaxValue;
        }
    }

    /// <summary>
    /// Calculate skewness (deviation from ideal 90-degree angles)
    /// Returns value between 0 (perfect) and 1 (degenerate)
    /// Based on ANSYS mesh quality metrics
    /// </summary>
    public double Skewness
    {
        get
        {
            if (!IsConvex) return 1.0;

            // Calculate internal angles
            double angle1 = CalculateAngle(V4, V1, V2);
            double angle2 = CalculateAngle(V1, V2, V3);
            double angle3 = CalculateAngle(V2, V3, V4);
            double angle4 = CalculateAngle(V3, V4, V1);

            // Find maximum deviation from 90 degrees
            double maxDev = new[] { angle1, angle2, angle3, angle4 }
                .Max(a => Math.Abs(a - Math.PI / 2.0));

            return maxDev / (Math.PI / 2.0); // Normalize to [0,1]
        }
    }

    /// <summary>
    /// Calculate angle at vertex b formed by points a-b-c
    /// </summary>
    private double CalculateAngle(Point2D a, Point2D b, Point2D c)
    {
        double dx1 = a.X - b.X;
        double dy1 = a.Y - b.Y;
        double dx2 = c.X - b.X;
        double dy2 = c.Y - b.Y;

        double dot = dx1 * dx2 + dy1 * dy2;
        double len1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);
        double len2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);

        if (len1 < 1e-10 || len2 < 1e-10) return 0;

        return Math.Acos(Math.Clamp(dot / (len1 * len2), -1.0, 1.0));
    }

    public override string ToString()
    {
        return $"Quad[{V1}, {V2}, {V3}, {V4}]";
    }
}
