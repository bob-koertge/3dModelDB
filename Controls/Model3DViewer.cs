using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System.Numerics;
using MauiApp3.Services;

namespace MauiApp3.Controls
{
    public class Model3DViewer : SKCanvasView
    {
        private StlParser.StlModel? _model;
        private float _rotationX = 30;
        private float _rotationY = 45;
        private float _rotationZ = 0;
        private float _zoom = 1.0f;
        private Vector2 _panOffset = Vector2.Zero;
        private Vector2? _lastTouchPoint;
        private bool _isRotating;
        private bool _isPanning;

        public Model3DViewer()
        {
            EnableTouchEvents = true;
            PaintSurface += OnPaintSurface;
            Touch += OnTouch;
        }

        public void LoadModel(StlParser.StlModel model)
        {
            _model = model;
            InvalidateSurface();
        }

        public void ResetView()
        {
            _rotationX = 30;
            _rotationY = 45;
            _rotationZ = 0;
            _zoom = 1.0f;
            _panOffset = Vector2.Zero;
            InvalidateSurface();
        }

        private void OnTouch(object? sender, SKTouchEventArgs e)
        {
            switch (e.ActionType)
            {
                case SKTouchAction.Pressed:
                    _lastTouchPoint = new Vector2(e.Location.X, e.Location.Y);
                    // Determine if rotating or panning (e.g., two fingers for pan)
                    _isRotating = true;
                    e.Handled = true;
                    break;

                case SKTouchAction.Moved:
                    if (_lastTouchPoint.HasValue && _isRotating)
                    {
                        var current = new Vector2(e.Location.X, e.Location.Y);
                        var delta = current - _lastTouchPoint.Value;

                        _rotationY += delta.X * 0.5f;
                        _rotationX += delta.Y * 0.5f;

                        _lastTouchPoint = current;
                        InvalidateSurface();
                    }
                    e.Handled = true;
                    break;

                case SKTouchAction.Released:
                case SKTouchAction.Cancelled:
                    _lastTouchPoint = null;
                    _isRotating = false;
                    _isPanning = false;
                    e.Handled = true;
                    break;

                case SKTouchAction.WheelChanged:
                    _zoom += e.WheelDelta * 0.01f;
                    _zoom = Math.Clamp(_zoom, 0.1f, 5.0f);
                    InvalidateSurface();
                    e.Handled = true;
                    break;
            }
        }

        private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            if (_model == null || _model.Triangles.Count == 0)
            {
                return;
            }

            var info = e.Info;
            float centerX = info.Width / 2f;
            float centerY = info.Height / 2f;
            float scale = Math.Min(info.Width, info.Height) * 0.4f * _zoom;

            canvas.Translate(centerX + _panOffset.X, centerY + _panOffset.Y);

            // Create transformation matrices
            var rotation = Matrix4x4.CreateRotationX(DegToRad(_rotationX)) *
                          Matrix4x4.CreateRotationY(DegToRad(_rotationY)) *
                          Matrix4x4.CreateRotationZ(DegToRad(_rotationZ));

            // Prepare triangles with depth for sorting
            var transformedTriangles = new List<(StlParser.Triangle triangle, float depth, Vector3[] vertices)>();

            foreach (var triangle in _model.Triangles)
            {
                var v1 = TransformVertex(triangle.Vertex1, _model.Center, _model.Scale, rotation, scale);
                var v2 = TransformVertex(triangle.Vertex2, _model.Center, _model.Scale, rotation, scale);
                var v3 = TransformVertex(triangle.Vertex3, _model.Center, _model.Scale, rotation, scale);

                float avgDepth = (v1.Z + v2.Z + v3.Z) / 3;
                transformedTriangles.Add((triangle, avgDepth, new[] { v1, v2, v3 }));
            }

            // Sort by depth (painter's algorithm - far to near)
            transformedTriangles.Sort((a, b) => a.depth.CompareTo(b.depth));

            // Draw triangles
            foreach (var (triangle, _, vertices) in transformedTriangles)
            {
                var v1 = vertices[0];
                var v2 = vertices[1];
                var v3 = vertices[2];

                // Calculate lighting (simple directional lighting)
                var normal = Vector3.Normalize(Vector3.Transform(triangle.Normal, rotation));
                var lightDir = Vector3.Normalize(new Vector3(0.5f, -0.7f, -1));
                float lightIntensity = Math.Max(0, Vector3.Dot(normal, -lightDir));
                lightIntensity = 0.3f + (lightIntensity * 0.7f); // Ambient + diffuse

                // Backface culling
                var edge1 = new Vector2(v2.X - v1.X, v2.Y - v1.Y);
                var edge2 = new Vector2(v3.X - v1.X, v3.Y - v1.Y);
                float cross = edge1.X * edge2.Y - edge1.Y * edge2.X;
                
                if (cross < 0) continue; // Skip back-facing triangles

                // Create color based on lighting
                byte colorValue = (byte)(255 * lightIntensity);
                var color = new SKColor(colorValue, colorValue, colorValue);

                using var path = new SKPath();
                path.MoveTo(v1.X, v1.Y);
                path.LineTo(v2.X, v2.Y);
                path.LineTo(v3.X, v3.Y);
                path.Close();

                using var fillPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = color,
                    IsAntialias = true
                };

                using var strokePaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.Black.WithAlpha(30),
                    StrokeWidth = 0.5f,
                    IsAntialias = true
                };

                canvas.DrawPath(path, fillPaint);
                canvas.DrawPath(path, strokePaint);
            }

            // Draw axis indicators (optional)
            DrawAxisIndicators(canvas, rotation, scale * 0.3f, info);
        }

        private Vector3 TransformVertex(Vector3 vertex, Vector3 center, float modelScale, Matrix4x4 rotation, float screenScale)
        {
            // Center the model
            var centered = vertex - center;
            
            // Apply model scale to fit unit cube
            centered *= modelScale;
            
            // Apply rotation
            var rotated = Vector3.Transform(centered, rotation);
            
            // Apply screen scale
            return rotated * screenScale;
        }

        private void DrawAxisIndicators(SKCanvas canvas, Matrix4x4 rotation, float length, SKImageInfo info)
        {
            // Draw small axis indicators in the corner
            canvas.Save();
            canvas.ResetMatrix();
            canvas.Translate(50, info.Height - 50);

            var axes = new[]
            {
                (Vector3.UnitX, SKColors.Red, "X"),
                (Vector3.UnitY, SKColors.Green, "Y"),
                (Vector3.UnitZ, SKColors.Blue, "Z")
            };

            foreach (var (axis, color, label) in axes)
            {
                var transformed = Vector3.Transform(axis, rotation) * length;

                using var paint = new SKPaint
                {
                    Color = color,
                    StrokeWidth = 2,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke
                };

                canvas.DrawLine(0, 0, transformed.X, -transformed.Y, paint);

                using var textPaint = new SKPaint
                {
                    Color = color,
                    TextSize = 12,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                };

                canvas.DrawText(label, transformed.X, -transformed.Y - 5, textPaint);
            }

            canvas.Restore();
        }

        private float DegToRad(float degrees) => degrees * (float)Math.PI / 180f;
    }
}
