namespace Meshatore2D.Core.Geometry;

/// <summary>
/// Represents a 2D point with double precision coordinates
/// </summary>
public class Point2D
{
    public double X { get; set; }
    public double Y { get; set; }
    public int Id { get; set; }

    public Point2D(double x, double y, int id = -1)
    {
        X = x;
        Y = y;
        Id = id;
    }

    /// <summary>
    /// Calculate Euclidean distance to another point
    /// </summary>
    public double DistanceTo(Point2D other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Calculate squared distance (faster, no sqrt)
    /// </summary>
    public double DistanceSquaredTo(Point2D other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Check if point is equal to another within tolerance
    /// </summary>
    public bool Equals(Point2D other, double tolerance = 1e-10)
    {
        return Math.Abs(X - other.X) < tolerance && Math.Abs(Y - other.Y) < tolerance;
    }

    public override string ToString()
    {
        return $"({X:F4}, {Y:F4})";
    }

    /// <summary>
    /// Calculate cross product for orientation test
    /// Returns positive if c is left of line ab, negative if right, zero if collinear
    /// </summary>
    public static double Orient2D(Point2D a, Point2D b, Point2D c)
    {
        return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    }
}
