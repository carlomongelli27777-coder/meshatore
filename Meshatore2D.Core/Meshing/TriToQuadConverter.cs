using Meshatore2D.Core.Geometry;

namespace Meshatore2D.Core.Meshing;

/// <summary>
/// Converts triangular mesh to quad-dominant mesh
/// Reference: Remacle, J.-F., et al. (2012). "Blossom-Quad: A non-uniform quadrilateral mesh generator"
/// Reference: Owen, S., et al. (1999). "Q-Morph: An indirect approach to advancing front quad meshing"
/// This implementation uses a greedy approach with quality metrics
/// </summary>
public class TriToQuadConverter
{
    private const double EPSILON = 1e-10;
    private const double MIN_CONVEXITY_ANGLE = 10.0; // degrees
    private const double MAX_SKEWNESS = 0.7;
    private const double MAX_ASPECT_RATIO = 4.0;

    /// <summary>
    /// Triangle adjacency information
    /// </summary>
    private class TriangleNode
    {
        public Triangle Triangle { get; set; }
        public List<TriangleNode> Neighbors { get; set; }
        public bool IsProcessed { get; set; }

        public TriangleNode(Triangle triangle)
        {
            Triangle = triangle;
            Neighbors = new List<TriangleNode>();
            IsProcessed = false;
        }
    }

    /// <summary>
    /// Pairing candidate with quality score
    /// </summary>
    private class PairingCandidate
    {
        public TriangleNode T1 { get; set; }
        public TriangleNode T2 { get; set; }
        public Quad Quad { get; set; }
        public double QualityScore { get; set; }

        public PairingCandidate(TriangleNode t1, TriangleNode t2, Quad quad, double score)
        {
            T1 = t1;
            T2 = t2;
            Quad = quad;
            QualityScore = score;
        }
    }

    /// <summary>
    /// Convert triangular mesh to quad-dominant mesh
    /// Returns both quads and remaining unpaired triangles
    /// </summary>
    public (List<Quad> quads, List<Triangle> triangles) Convert(List<Triangle> triangles,
                                                                  double qualityThreshold = 0.5)
    {
        if (triangles.Count == 0)
            return (new List<Quad>(), new List<Triangle>());

        // Step 1: Build triangle adjacency graph
        List<TriangleNode> nodes = BuildAdjacencyGraph(triangles);

        // Step 2: Generate pairing candidates and sort by quality
        List<PairingCandidate> candidates = GeneratePairingCandidates(nodes);
        candidates = candidates
            .Where(c => c.QualityScore >= qualityThreshold)
            .OrderByDescending(c => c.QualityScore)
            .ToList();

        // Step 3: Greedily pair triangles to form quads
        List<Quad> quads = new List<Quad>();
        foreach (var candidate in candidates)
        {
            if (!candidate.T1.IsProcessed && !candidate.T2.IsProcessed)
            {
                quads.Add(candidate.Quad);
                candidate.T1.IsProcessed = true;
                candidate.T2.IsProcessed = true;
            }
        }

        // Step 4: Collect remaining unpaired triangles
        List<Triangle> remainingTriangles = nodes
            .Where(n => !n.IsProcessed)
            .Select(n => n.Triangle)
            .ToList();

        // Assign IDs
        for (int i = 0; i < quads.Count; i++)
            quads[i].Id = i;

        for (int i = 0; i < remainingTriangles.Count; i++)
            remainingTriangles[i].Id = i;

        return (quads, remainingTriangles);
    }

    /// <summary>
    /// Build adjacency graph for triangles
    /// Two triangles are adjacent if they share an edge
    /// </summary>
    private List<TriangleNode> BuildAdjacencyGraph(List<Triangle> triangles)
    {
        List<TriangleNode> nodes = triangles.Select(t => new TriangleNode(t)).ToList();

        // Find adjacent triangles
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = i + 1; j < nodes.Count; j++)
            {
                if (TrianglesShareEdge(nodes[i].Triangle, nodes[j].Triangle, out _))
                {
                    nodes[i].Neighbors.Add(nodes[j]);
                    nodes[j].Neighbors.Add(nodes[i]);
                }
            }
        }

        return nodes;
    }

    /// <summary>
    /// Check if two triangles share an edge and return the shared edge
    /// </summary>
    private bool TrianglesShareEdge(Triangle t1, Triangle t2, out Edge? sharedEdge)
    {
        sharedEdge = null;

        foreach (var e1 in t1.Edges)
        {
            foreach (var e2 in t2.Edges)
            {
                if (e1.Equals(e2, EPSILON))
                {
                    sharedEdge = e1;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Generate all possible pairing candidates with quality scores
    /// </summary>
    private List<PairingCandidate> GeneratePairingCandidates(List<TriangleNode> nodes)
    {
        List<PairingCandidate> candidates = new List<PairingCandidate>();

        foreach (var node in nodes)
        {
            foreach (var neighbor in node.Neighbors)
            {
                // Avoid duplicate pairs
                if (node.Triangle.Id < neighbor.Triangle.Id)
                {
                    var quad = CreateQuadFromTriangles(node.Triangle, neighbor.Triangle);
                    if (quad != null)
                    {
                        double score = EvaluateQuadQuality(quad);
                        candidates.Add(new PairingCandidate(node, neighbor, quad, score));
                    }
                }
            }
        }

        return candidates;
    }

    /// <summary>
    /// Create a quad from two adjacent triangles
    /// Returns null if quad would be invalid
    /// </summary>
    private Quad? CreateQuadFromTriangles(Triangle t1, Triangle t2)
    {
        if (!TrianglesShareEdge(t1, t2, out Edge? sharedEdge))
            return null;

        if (sharedEdge == null)
            return null;

        // Find the four vertices of the quad
        Point2D[] t1Verts = t1.Vertices;
        Point2D[] t2Verts = t2.Vertices;

        List<Point2D> quadVerts = new List<Point2D>();

        // Find vertices not on shared edge from t1
        foreach (var v in t1Verts)
        {
            if (!v.Equals(sharedEdge.P1, EPSILON) && !v.Equals(sharedEdge.P2, EPSILON))
            {
                quadVerts.Add(v);
                break;
            }
        }

        // Add shared edge vertices in order
        quadVerts.Add(sharedEdge.P1);

        // Find vertices not on shared edge from t2
        foreach (var v in t2Verts)
        {
            if (!v.Equals(sharedEdge.P1, EPSILON) && !v.Equals(sharedEdge.P2, EPSILON))
            {
                quadVerts.Add(v);
                break;
            }
        }

        quadVerts.Add(sharedEdge.P2);

        if (quadVerts.Count != 4)
            return null;

        return new Quad(quadVerts[0], quadVerts[1], quadVerts[2], quadVerts[3]);
    }

    /// <summary>
    /// Evaluate quad quality using multiple metrics
    /// Returns score between 0 (worst) and 1 (perfect)
    /// Based on ANSYS and commercial mesher quality metrics
    /// </summary>
    private double EvaluateQuadQuality(Quad quad)
    {
        // Metric 1: Convexity check
        if (!quad.IsConvex)
            return 0.0; // Reject non-convex quads

        // Metric 2: Aspect ratio (normalized)
        double aspectRatio = quad.AspectRatio;
        if (aspectRatio > MAX_ASPECT_RATIO)
            return 0.0;

        double aspectScore = 1.0 - Math.Min(1.0, (aspectRatio - 1.0) / (MAX_ASPECT_RATIO - 1.0));

        // Metric 3: Skewness (deviation from 90-degree angles)
        double skewness = quad.Skewness;
        if (skewness > MAX_SKEWNESS)
            return 0.0;

        double skewScore = 1.0 - skewness;

        // Metric 4: Area ratio (prefer quads with similar triangle areas)
        Triangle t1 = new Triangle(quad.V1, quad.V2, quad.V3);
        Triangle t2 = new Triangle(quad.V1, quad.V3, quad.V4);
        double areaRatio = Math.Min(t1.Area, t2.Area) / Math.Max(t1.Area, t2.Area);
        double areaScore = areaRatio; // 1.0 when equal, 0.0 when very different

        // Metric 5: Edge length uniformity
        var edges = quad.Edges;
        double maxLen = edges.Max(e => e.Length);
        double minLen = edges.Min(e => e.Length);
        double lengthUniformity = minLen / maxLen;

        // Weighted combination of metrics
        double qualityScore = 0.3 * aspectScore +
                             0.3 * skewScore +
                             0.2 * areaScore +
                             0.2 * lengthUniformity;

        return Math.Clamp(qualityScore, 0.0, 1.0);
    }

    /// <summary>
    /// Get conversion statistics
    /// </summary>
    public ConversionStats GetStats(List<Quad> quads, List<Triangle> triangles)
    {
        int totalElements = quads.Count + triangles.Count;
        double quadPercentage = totalElements > 0 ? (100.0 * quads.Count / totalElements) : 0;

        var stats = new ConversionStats
        {
            QuadCount = quads.Count,
            TriangleCount = triangles.Count,
            TotalElements = totalElements,
            QuadPercentage = quadPercentage
        };

        if (quads.Count > 0)
        {
            stats.MinQuadQuality = quads.Min(q => 1.0 - q.Skewness);
            stats.MaxQuadQuality = quads.Max(q => 1.0 - q.Skewness);
            stats.AvgQuadQuality = quads.Average(q => 1.0 - q.Skewness);
            stats.MinAspectRatio = quads.Min(q => q.AspectRatio);
            stats.MaxAspectRatio = quads.Max(q => q.AspectRatio);
            stats.AvgAspectRatio = quads.Average(q => q.AspectRatio);
        }

        return stats;
    }
}

/// <summary>
/// Statistics about tri-to-quad conversion
/// </summary>
public class ConversionStats
{
    public int QuadCount { get; set; }
    public int TriangleCount { get; set; }
    public int TotalElements { get; set; }
    public double QuadPercentage { get; set; }
    public double MinQuadQuality { get; set; }
    public double MaxQuadQuality { get; set; }
    public double AvgQuadQuality { get; set; }
    public double MinAspectRatio { get; set; }
    public double MaxAspectRatio { get; set; }
    public double AvgAspectRatio { get; set; }

    public override string ToString()
    {
        return $"Conversion: {QuadCount} quads ({QuadPercentage:F1}%), {TriangleCount} triangles\n" +
               $"Quad Quality - Min: {MinQuadQuality:F3}, Avg: {AvgQuadQuality:F3}, Max: {MaxQuadQuality:F3}\n" +
               $"Aspect Ratio - Min: {MinAspectRatio:F2}, Avg: {AvgAspectRatio:F2}, Max: {MaxAspectRatio:F2}";
    }
}
