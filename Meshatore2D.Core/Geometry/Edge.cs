namespace Meshatore2D.Core.Geometry;

/// <summary>
/// Represents an edge between two points
/// </summary>
public class Edge
{
    public Point2D P1 { get; set; }
    public Point2D P2 { get; set; }
    public bool IsConstrained { get; set; } // For boundary edges

    public Edge(Point2D p1, Point2D p2, bool isConstrained = false)
    {
        P1 = p1;
        P2 = p2;
        IsConstrained = isConstrained;
    }

    /// <summary>
    /// Calculate edge length
    /// </summary>
    public double Length => P1.DistanceTo(P2);

    /// <summary>
    /// Calculate edge midpoint
    /// </summary>
    public Point2D Midpoint => new Point2D((P1.X + P2.X) / 2.0, (P1.Y + P2.Y) / 2.0);

    /// <summary>
    /// Check if this edge is equal to another (unoriented comparison)
    /// </summary>
    public bool Equals(Edge other, double tolerance = 1e-10)
    {
        return (P1.Equals(other.P1, tolerance) && P2.Equals(other.P2, tolerance)) ||
               (P1.Equals(other.P2, tolerance) && P2.Equals(other.P1, tolerance));
    }

    /// <summary>
    /// Check if two edges share a common vertex
    /// </summary>
    public bool SharesVertex(Edge other, double tolerance = 1e-10)
    {
        return P1.Equals(other.P1, tolerance) || P1.Equals(other.P2, tolerance) ||
               P2.Equals(other.P1, tolerance) || P2.Equals(other.P2, tolerance);
    }

    public override string ToString()
    {
        return $"Edge[{P1} -> {P2}]";
    }
}
