using Meshatore2D.Core.Geometry;

namespace Meshatore2D.Core.Meshing;

/// <summary>
/// Main mesh generator orchestrating the complete meshing pipeline
/// Pipeline: Boundary Input -> Delaunay Triangulation -> Tri-to-Quad Conversion -> Smoothing
/// </summary>
public class MeshGenerator
{
    private ConstrainedDelaunayTriangulator _triangulator;
    private TriToQuadConverter _converter;
    private MeshSmoother _smoother;

    public MeshGenerator()
    {
        _triangulator = new ConstrainedDelaunayTriangulator();
        _converter = new TriToQuadConverter();
        _smoother = new MeshSmoother();
    }

    /// <summary>
    /// Generate quad-dominant mesh for a closed polygon
    /// </summary>
    /// <param name="boundaryPoints">Polygon boundary points (counter-clockwise)</param>
    /// <param name="options">Meshing options</param>
    /// <returns>Generated mesh result</returns>
    public MeshResult GenerateMesh(List<Point2D> boundaryPoints, MeshOptions? options = null)
    {
        if (boundaryPoints.Count < 3)
            throw new ArgumentException("At least 3 boundary points required");

        options ??= new MeshOptions();

        var result = new MeshResult();
        result.BoundaryPoints = new List<Point2D>(boundaryPoints);

        // Calculate target edge length if not specified
        double targetEdgeLength = options.TargetEdgeLength;
        if (targetEdgeLength <= 0)
        {
            // Auto-calculate based on bounding box size
            double minX = boundaryPoints.Min(p => p.X);
            double maxX = boundaryPoints.Max(p => p.X);
            double minY = boundaryPoints.Min(p => p.Y);
            double maxY = boundaryPoints.Max(p => p.Y);
            double width = maxX - minX;
            double height = maxY - minY;
            double avgSize = (width + height) / 2.0;
            targetEdgeLength = avgSize / 8.0; // Create ~8x8 grid (64 internal points)
            Console.WriteLine($"Auto-calculated edge length: {targetEdgeLength:F2} (domain: {width:F0}x{height:F0})");
        }

        // Step 1: Triangulation
        Console.WriteLine($"Step 1: Delaunay Triangulation (edge length: {targetEdgeLength:F2})...");
        var triangles = _triangulator.Triangulate(
            boundaryPoints,
            null,
            targetEdgeLength
        );
        result.InitialTriangleCount = triangles.Count;

        // Step 2: Optional smoothing of triangle mesh
        if (options.SmoothTriangleMesh)
        {
            Console.WriteLine("Step 2: Smoothing triangular mesh...");
            _smoother.SmoothTriangleMesh(triangles, boundaryPoints, options.SmoothingIterations);
        }

        // Step 3: Tri-to-Quad conversion
        Console.WriteLine("Step 3: Converting to quad-dominant mesh...");
        var (quads, remainingTriangles) = _converter.Convert(triangles, options.QuadQualityThreshold);
        result.Quads = quads;
        result.Triangles = remainingTriangles;

        // Step 4: Optional smoothing of final mesh
        if (options.SmoothFinalMesh)
        {
            Console.WriteLine("Step 4: Smoothing final mesh...");
            _smoother.SmoothQuadMesh(quads, remainingTriangles, boundaryPoints, options.SmoothingIterations);
        }

        // Calculate statistics
        result.CalculateStatistics();

        Console.WriteLine("Mesh generation complete!");
        Console.WriteLine(result.GetSummary());

        return result;
    }
}

/// <summary>
/// Options for mesh generation
/// </summary>
public class MeshOptions
{
    /// <summary>
    /// Target edge length for internal point generation (0 = auto)
    /// </summary>
    public double TargetEdgeLength { get; set; } = 0;

    /// <summary>
    /// Quality threshold for quad formation (0-1, higher = stricter)
    /// </summary>
    public double QuadQualityThreshold { get; set; } = 0.3;

    /// <summary>
    /// Enable smoothing of triangle mesh before conversion
    /// </summary>
    public bool SmoothTriangleMesh { get; set; } = true;

    /// <summary>
    /// Enable smoothing of final quad-dominant mesh
    /// </summary>
    public bool SmoothFinalMesh { get; set; } = true;

    /// <summary>
    /// Number of smoothing iterations
    /// </summary>
    public int SmoothingIterations { get; set; } = 3;
}

/// <summary>
/// Result of mesh generation
/// </summary>
public class MeshResult
{
    public List<Point2D> BoundaryPoints { get; set; } = new();
    public List<Quad> Quads { get; set; } = new();
    public List<Triangle> Triangles { get; set; } = new();
    public int InitialTriangleCount { get; set; }

    // Statistics
    public int TotalElements => Quads.Count + Triangles.Count;
    public double QuadPercentage { get; private set; }
    public double AvgQuadQuality { get; private set; }
    public double AvgQuadAspectRatio { get; private set; }
    public double AvgTriangleQuality { get; private set; }

    public void CalculateStatistics()
    {
        if (TotalElements > 0)
        {
            QuadPercentage = 100.0 * Quads.Count / TotalElements;
        }

        if (Quads.Count > 0)
        {
            AvgQuadQuality = Quads.Average(q => 1.0 - q.Skewness);
            AvgQuadAspectRatio = Quads.Average(q => q.AspectRatio);
        }

        if (Triangles.Count > 0)
        {
            AvgTriangleQuality = Triangles.Average(t => t.MinAngle / 60.0); // Normalized to equilateral
        }
    }

    public string GetSummary()
    {
        return $"""
            === Mesh Generation Summary ===
            Initial Triangles: {InitialTriangleCount}
            Final Elements: {TotalElements} ({Quads.Count} quads, {Triangles.Count} triangles)
            Quad Coverage: {QuadPercentage:F1}%
            Avg Quad Quality: {AvgQuadQuality:F3}
            Avg Quad Aspect Ratio: {AvgQuadAspectRatio:F2}
            Avg Triangle Quality: {AvgTriangleQuality:F3}
            """;
    }
}
