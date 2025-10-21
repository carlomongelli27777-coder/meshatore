namespace Meshatore2D.Core.Geometry;

/// <summary>
/// Represents a triangular element in the mesh
/// Based on Delaunay triangulation literature (Shewchuk, 2002)
/// </summary>
public class Triangle
{
    public Point2D V1 { get; set; }
    public Point2D V2 { get; set; }
    public Point2D V3 { get; set; }
    public int Id { get; set; }

    // Circumcircle properties (cached for Delaunay algorithm)
    private Point2D? _circumcenter;
    private double? _circumradiusSquared;

    public Triangle(Point2D v1, Point2D v2, Point2D v3, int id = -1)
    {
        V1 = v1;
        V2 = v2;
        V3 = v3;
        Id = id;
    }

    /// <summary>
    /// Get triangle vertices as array
    /// </summary>
    public Point2D[] Vertices => new[] { V1, V2, V3 };

    /// <summary>
    /// Get triangle edges
    /// </summary>
    public Edge[] Edges => new[]
    {
        new Edge(V1, V2),
        new Edge(V2, V3),
        new Edge(V3, V1)
    };

    /// <summary>
    /// Calculate triangle area using cross product
    /// </summary>
    public double Area
    {
        get
        {
            double area = 0.5 * Math.Abs(
                (V2.X - V1.X) * (V3.Y - V1.Y) -
                (V3.X - V1.X) * (V2.Y - V1.Y)
            );
            return area;
        }
    }

    /// <summary>
    /// Calculate triangle centroid
    /// </summary>
    public Point2D Centroid => new Point2D(
        (V1.X + V2.X + V3.X) / 3.0,
        (V1.Y + V2.Y + V3.Y) / 3.0
    );

    /// <summary>
    /// Calculate circumcenter (center of circumscribed circle)
    /// Algorithm from "Computational Geometry in C" by O'Rourke
    /// </summary>
    public Point2D Circumcenter
    {
        get
        {
            if (_circumcenter != null) return _circumcenter;

            double dx1 = V2.X - V1.X;
            double dy1 = V2.Y - V1.Y;
            double dx2 = V3.X - V1.X;
            double dy2 = V3.Y - V1.Y;

            double d = 2 * (dx1 * dy2 - dy1 * dx2);

            if (Math.Abs(d) < 1e-10)
            {
                // Degenerate triangle, return centroid
                _circumcenter = Centroid;
                return _circumcenter;
            }

            double len1Sq = dx1 * dx1 + dy1 * dy1;
            double len2Sq = dx2 * dx2 + dy2 * dy2;

            double cx = V1.X + (dy2 * len1Sq - dy1 * len2Sq) / d;
            double cy = V1.Y + (dx1 * len2Sq - dx2 * len1Sq) / d;

            _circumcenter = new Point2D(cx, cy);
            return _circumcenter;
        }
    }

    /// <summary>
    /// Calculate squared circumradius (for Delaunay test)
    /// </summary>
    public double CircumradiusSquared
    {
        get
        {
            if (_circumradiusSquared.HasValue) return _circumradiusSquared.Value;

            Point2D center = Circumcenter;
            _circumradiusSquared = V1.DistanceSquaredTo(center);
            return _circumradiusSquared.Value;
        }
    }

    /// <summary>
    /// Check if point is inside circumcircle (Delaunay criterion)
    /// </summary>
    public bool PointInCircumcircle(Point2D p, double tolerance = 1e-10)
    {
        Point2D center = Circumcenter;
        double distSq = p.DistanceSquaredTo(center);
        return distSq < CircumradiusSquared - tolerance;
    }

    /// <summary>
    /// Check if triangle contains a specific point
    /// </summary>
    public bool ContainsPoint(Point2D p, double tolerance = 1e-10)
    {
        // Barycentric coordinate test
        double denom = (V2.Y - V3.Y) * (V1.X - V3.X) + (V3.X - V2.X) * (V1.Y - V3.Y);

        if (Math.Abs(denom) < tolerance) return false;

        double a = ((V2.Y - V3.Y) * (p.X - V3.X) + (V3.X - V2.X) * (p.Y - V3.Y)) / denom;
        double b = ((V3.Y - V1.Y) * (p.X - V3.X) + (V1.X - V3.X) * (p.Y - V3.Y)) / denom;
        double c = 1 - a - b;

        return a >= -tolerance && b >= -tolerance && c >= -tolerance;
    }

    /// <summary>
    /// Check if triangle contains vertex
    /// </summary>
    public bool ContainsVertex(Point2D p, double tolerance = 1e-10)
    {
        return V1.Equals(p, tolerance) || V2.Equals(p, tolerance) || V3.Equals(p, tolerance);
    }

    /// <summary>
    /// Calculate minimum angle in triangle (quality metric)
    /// </summary>
    public double MinAngle
    {
        get
        {
            double a = V2.DistanceTo(V3);
            double b = V3.DistanceTo(V1);
            double c = V1.DistanceTo(V2);

            // Law of cosines
            double angle1 = Math.Acos((b * b + c * c - a * a) / (2 * b * c));
            double angle2 = Math.Acos((a * a + c * c - b * b) / (2 * a * c));
            double angle3 = Math.Acos((a * a + b * b - c * c) / (2 * a * b));

            return Math.Min(angle1, Math.Min(angle2, angle3)) * 180.0 / Math.PI;
        }
    }

    /// <summary>
    /// Calculate aspect ratio (quality metric)
    /// Ratio of circumradius to twice the inradius
    /// Perfect equilateral triangle has ratio = 1
    /// </summary>
    public double AspectRatio
    {
        get
        {
            double a = V2.DistanceTo(V3);
            double b = V3.DistanceTo(V1);
            double c = V1.DistanceTo(V2);
            double s = (a + b + c) / 2.0; // semi-perimeter

            if (Area < 1e-10) return double.MaxValue;

            double inradius = Area / s;
            double circumradius = Math.Sqrt(CircumradiusSquared);

            return circumradius / (2.0 * inradius);
        }
    }

    public override string ToString()
    {
        return $"Triangle[{V1}, {V2}, {V3}]";
    }
}
