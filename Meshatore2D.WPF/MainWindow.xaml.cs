using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Meshatore2D.Core.Geometry;
using Meshatore2D.Core.Meshing;
using Microsoft.Win32;

namespace Meshatore2D.WPF;

public partial class MainWindow : Window
{
    private List<Point2D> _polygonPoints = new();
    private bool _polygonClosed = false;
    private MeshResult? _meshResult = null;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_polygonClosed)
            return;

        Point pos = e.GetPosition(MeshCanvas);
        Point2D point = new Point2D(pos.X, pos.Y, _polygonPoints.Count);

        _polygonPoints.Add(point);
        UpdatePolygonDisplay();
        UpdateStatus();
    }

    private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_polygonPoints.Count < 3)
        {
            MessageBox.Show("At least 3 points are required to close the polygon.",
                          "Invalid Polygon", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _polygonClosed = true;
        UpdatePolygonDisplay();
        UpdateStatus();
        GenerateMeshButton.IsEnabled = true;
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        // Could show preview line here
    }

    private void ClearPolygon_Click(object sender, RoutedEventArgs e)
    {
        _polygonPoints.Clear();
        _polygonClosed = false;
        _meshResult = null;
        MeshCanvas.Children.Clear();
        UpdateStatus();
        GenerateMeshButton.IsEnabled = false;
        ExportButton.IsEnabled = false;
        StatsText.Text = "No mesh generated yet.";
    }

    private void LoadPreset_Click(object sender, RoutedEventArgs e)
    {
        ClearPolygon_Click(sender, e);

        // Create a square preset
        double centerX = MeshCanvas.ActualWidth / 2;
        double centerY = MeshCanvas.ActualHeight / 2;
        double size = Math.Min(MeshCanvas.ActualWidth, MeshCanvas.ActualHeight) * 0.6;

        _polygonPoints.Add(new Point2D(centerX - size / 2, centerY - size / 2, 0));
        _polygonPoints.Add(new Point2D(centerX + size / 2, centerY - size / 2, 1));
        _polygonPoints.Add(new Point2D(centerX + size / 2, centerY + size / 2, 2));
        _polygonPoints.Add(new Point2D(centerX - size / 2, centerY + size / 2, 3));

        _polygonClosed = true;
        UpdatePolygonDisplay();
        UpdateStatus();
        GenerateMeshButton.IsEnabled = true;
    }

    private void GenerateMesh_Click(object sender, RoutedEventArgs e)
    {
        if (!_polygonClosed || _polygonPoints.Count < 3)
            return;

        try
        {
            StatusText.Text = "Generating mesh...";
            StatusText.Foreground = new SolidColorBrush(Colors.Blue);

            // Parse parameters
            double edgeLength = 0;
            if (!string.IsNullOrWhiteSpace(EdgeLengthTextBox.Text))
            {
                if (!double.TryParse(EdgeLengthTextBox.Text, out edgeLength))
                    edgeLength = 0;
            }

            int smoothingIterations = 3;
            if (!string.IsNullOrWhiteSpace(SmoothingIterationsTextBox.Text))
            {
                if (!int.TryParse(SmoothingIterationsTextBox.Text, out smoothingIterations))
                    smoothingIterations = 3;
            }

            // If edge length is 0, calculate auto from polygon size
            if (edgeLength <= 0)
            {
                double minX = _polygonPoints.Min(p => p.X);
                double maxX = _polygonPoints.Max(p => p.X);
                double minY = _polygonPoints.Min(p => p.Y);
                double maxY = _polygonPoints.Max(p => p.Y);
                double diagLength = Math.Sqrt((maxX - minX) * (maxX - minX) + (maxY - minY) * (maxY - minY));
                edgeLength = diagLength / 10; // 10 divisions
            }

            var options = new MeshOptions
            {
                TargetEdgeLength = edgeLength,
                QuadQualityThreshold = QualitySlider.Value,
                SmoothTriangleMesh = SmoothTriMeshCheckBox.IsChecked ?? true,
                SmoothFinalMesh = SmoothFinalMeshCheckBox.IsChecked ?? true,
                SmoothingIterations = smoothingIterations
            };

            var generator = new MeshGenerator();
            _meshResult = generator.GenerateMesh(_polygonPoints, options);

            DisplayMesh();
            DisplayStatistics();

            StatusText.Text = "Mesh generated successfully!";
            StatusText.Foreground = new SolidColorBrush(Colors.Green);
            ExportButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating mesh: {ex.Message}",
                          "Generation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Error generating mesh.";
            StatusText.Foreground = new SolidColorBrush(Colors.Red);
        }
    }

    private void UpdatePolygonDisplay()
    {
        MeshCanvas.Children.Clear();

        if (_polygonPoints.Count == 0)
            return;

        // Draw polygon edges
        for (int i = 0; i < _polygonPoints.Count; i++)
        {
            Point2D p1 = _polygonPoints[i];
            Point2D p2 = _polygonPoints[(i + 1) % _polygonPoints.Count];

            if (!_polygonClosed && i == _polygonPoints.Count - 1)
                break;

            Line line = new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            MeshCanvas.Children.Add(line);
        }

        // Draw points
        foreach (var point in _polygonPoints)
        {
            Ellipse circle = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = Brushes.Red,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            Canvas.SetLeft(circle, point.X - 4);
            Canvas.SetTop(circle, point.Y - 4);
            MeshCanvas.Children.Add(circle);
        }
    }

    private void DisplayMesh()
    {
        MeshCanvas.Children.Clear();

        if (_meshResult == null)
            return;

        bool fillElements = FillElementsCheckBox.IsChecked ?? false;
        bool showQuads = ShowQuadsCheckBox.IsChecked ?? true;
        bool showTriangles = ShowTrianglesCheckBox.IsChecked ?? true;
        bool showPolygon = ShowPolygonCheckBox.IsChecked ?? true;
        bool showPoints = ShowPointsCheckBox.IsChecked ?? true;

        // Draw quads
        if (showQuads)
        {
            foreach (var quad in _meshResult.Quads)
            {
                Polygon poly = new Polygon
                {
                    Stroke = Brushes.Blue,
                    StrokeThickness = 1.5,
                    Fill = fillElements ? new SolidColorBrush(Color.FromArgb(30, 0, 0, 255)) : null
                };

                poly.Points.Add(new Point(quad.V1.X, quad.V1.Y));
                poly.Points.Add(new Point(quad.V2.X, quad.V2.Y));
                poly.Points.Add(new Point(quad.V3.X, quad.V3.Y));
                poly.Points.Add(new Point(quad.V4.X, quad.V4.Y));

                MeshCanvas.Children.Add(poly);
            }
        }

        // Draw triangles
        if (showTriangles)
        {
            foreach (var triangle in _meshResult.Triangles)
            {
                Polygon poly = new Polygon
                {
                    Stroke = Brushes.Green,
                    StrokeThickness = 1.5,
                    Fill = fillElements ? new SolidColorBrush(Color.FromArgb(30, 0, 255, 0)) : null
                };

                poly.Points.Add(new Point(triangle.V1.X, triangle.V1.Y));
                poly.Points.Add(new Point(triangle.V2.X, triangle.V2.Y));
                poly.Points.Add(new Point(triangle.V3.X, triangle.V3.Y));

                MeshCanvas.Children.Add(poly);
            }
        }

        // Draw polygon boundary
        if (showPolygon)
        {
            for (int i = 0; i < _meshResult.BoundaryPoints.Count; i++)
            {
                Point2D p1 = _meshResult.BoundaryPoints[i];
                Point2D p2 = _meshResult.BoundaryPoints[(i + 1) % _meshResult.BoundaryPoints.Count];

                Line line = new Line
                {
                    X1 = p1.X,
                    Y1 = p1.Y,
                    X2 = p2.X,
                    Y2 = p2.Y,
                    Stroke = Brushes.Black,
                    StrokeThickness = 3
                };
                MeshCanvas.Children.Add(line);
            }
        }

        // Draw boundary points
        if (showPoints)
        {
            foreach (var point in _meshResult.BoundaryPoints)
            {
                Ellipse circle = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.Red,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(circle, point.X - 3);
                Canvas.SetTop(circle, point.Y - 3);
                MeshCanvas.Children.Add(circle);
            }
        }
    }

    private void DisplayStatistics()
    {
        if (_meshResult == null)
        {
            StatsText.Text = "No mesh generated yet.";
            return;
        }

        StatsText.Text = $"""
            Elements:
              Quads: {_meshResult.Quads.Count}
              Triangles: {_meshResult.Triangles.Count}
              Total: {_meshResult.TotalElements}

            Coverage:
              Quad %: {_meshResult.QuadPercentage:F1}%

            Quality:
              Avg Quad Quality: {_meshResult.AvgQuadQuality:F3}
              Avg Quad Aspect: {_meshResult.AvgQuadAspectRatio:F2}
              Avg Tri Quality: {_meshResult.AvgTriangleQuality:F3}

            Original:
              Init Triangles: {_meshResult.InitialTriangleCount}
            """;
    }

    private void UpdateDisplay(object sender, RoutedEventArgs e)
    {
        DisplayMesh();
    }

    private void UpdateStatus()
    {
        PointCountText.Text = _polygonPoints.Count.ToString();

        if (_polygonClosed)
        {
            StatusText.Text = $"Polygon closed with {_polygonPoints.Count} points. Ready to generate mesh.";
            StatusText.Foreground = new SolidColorBrush(Colors.Green);
        }
        else if (_polygonPoints.Count > 0)
        {
            StatusText.Text = $"{_polygonPoints.Count} points added. Right-click to close polygon.";
            StatusText.Foreground = new SolidColorBrush(Colors.Orange);
        }
        else
        {
            StatusText.Text = "Define a polygon to start...";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_meshResult == null)
            return;

        SaveFileDialog dialog = new SaveFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            FileName = "mesh.txt"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                ExportMesh(dialog.FileName);
                MessageBox.Show($"Mesh exported successfully to:\n{dialog.FileName}",
                              "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting mesh: {ex.Message}",
                              "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExportMesh(string filename)
    {
        if (_meshResult == null)
            return;

        using (StreamWriter writer = new StreamWriter(filename))
        {
            writer.WriteLine("# Meshatore 2D - Quad-Dominant Mesh Export");
            writer.WriteLine($"# Generated: {DateTime.Now}");
            writer.WriteLine();

            writer.WriteLine("# Statistics");
            writer.WriteLine($"# Quads: {_meshResult.Quads.Count}");
            writer.WriteLine($"# Triangles: {_meshResult.Triangles.Count}");
            writer.WriteLine($"# Total Elements: {_meshResult.TotalElements}");
            writer.WriteLine($"# Quad Coverage: {_meshResult.QuadPercentage:F1}%");
            writer.WriteLine();

            // Export quads
            writer.WriteLine("# Quadrilaterals (V1_X V1_Y V2_X V2_Y V3_X V3_Y V4_X V4_Y)");
            writer.WriteLine($"QUADS {_meshResult.Quads.Count}");
            foreach (var quad in _meshResult.Quads)
            {
                writer.WriteLine($"{quad.V1.X:F6} {quad.V1.Y:F6} " +
                               $"{quad.V2.X:F6} {quad.V2.Y:F6} " +
                               $"{quad.V3.X:F6} {quad.V3.Y:F6} " +
                               $"{quad.V4.X:F6} {quad.V4.Y:F6}");
            }

            writer.WriteLine();

            // Export triangles
            writer.WriteLine("# Triangles (V1_X V1_Y V2_X V2_Y V3_X V3_Y)");
            writer.WriteLine($"TRIANGLES {_meshResult.Triangles.Count}");
            foreach (var triangle in _meshResult.Triangles)
            {
                writer.WriteLine($"{triangle.V1.X:F6} {triangle.V1.Y:F6} " +
                               $"{triangle.V2.X:F6} {triangle.V2.Y:F6} " +
                               $"{triangle.V3.X:F6} {triangle.V3.Y:F6}");
            }

            writer.WriteLine();

            // Export boundary
            writer.WriteLine("# Boundary Points (X Y)");
            writer.WriteLine($"BOUNDARY {_meshResult.BoundaryPoints.Count}");
            foreach (var point in _meshResult.BoundaryPoints)
            {
                writer.WriteLine($"{point.X:F6} {point.Y:F6}");
            }
        }
    }
}
