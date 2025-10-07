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

        // Cached objects for performance
        private readonly List<(StlParser.Triangle triangle, float depth, Vector3[] vertices)> _transformedTriangles = new();
        private SKPaint? _fillPaint;
        private SKPaint? _strokePaint;
        private readonly float _degToRadFactor = MathF.PI / 180f;

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
                    _zoom = Math.Clamp(_zoom + (e.WheelDelta * 0.01f), 0.1f, 5.0f);
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
                return;

            var info = e.Info;
            float centerX = info.Width * 0.5f;
            float centerY = info.Height * 0.5f;
            float scale = Math.Min(info.Width, info.Height) * 0.4f * _zoom;

            canvas.Translate(centerX + _panOffset.X, centerY + _panOffset.Y);

            // Create transformation matrix once
            var rotation = Matrix4x4.CreateRotationX(_rotationX * _degToRadFactor) *
                          Matrix4x4.CreateRotationY(_rotationY * _degToRadFactor) *
                          Matrix4x4.CreateRotationZ(_rotationZ * _degToRadFactor);

            // Reuse list to avoid allocations
            _transformedTriangles.Clear();
            if (_transformedTriangles.Capacity < _model.Triangles.Count)
            {
                _transformedTriangles.Capacity = _model.Triangles.Count;
            }

            // Pre-calculate light direction
            var lightDir = Vector3.Normalize(new Vector3(0.5f, -0.7f, -1));

            // Transform and cull triangles
            foreach (var triangle in _model.Triangles)
            {
                var v1 = TransformVertex(triangle.Vertex1, _model.Center, _model.Scale, rotation, scale);
                var v2 = TransformVertex(triangle.Vertex2, _model.Center, _model.Scale, rotation, scale);
                var v3 = TransformVertex(triangle.Vertex3, _model.Center, _model.Scale, rotation, scale);

                // Backface culling (early)
                var edge1X = v2.X - v1.X;
                var edge1Y = v2.Y - v1.Y;
                var edge2X = v3.X - v1.X;
                var edge2Y = v3.Y - v1.Y;
                float cross = edge1X * edge2Y - edge1Y * edge2X;
                
                if (cross < 0) continue;

                float avgDepth = (v1.Z + v2.Z + v3.Z) * 0.333333f;
                _transformedTriangles.Add((triangle, avgDepth, new[] { v1, v2, v3 }));
            }

            // Sort by depth (painter's algorithm)
            _transformedTriangles.Sort((a, b) => a.depth.CompareTo(b.depth));

            // Reuse paint objects
            _fillPaint ??= new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            _strokePaint ??= new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black.WithAlpha(30),
                StrokeWidth = 0.5f,
                IsAntialias = true
            };

            // Draw triangles
            using var path = new SKPath();
            foreach (var (triangle, _, vertices) in _transformedTriangles)
            {
                var v1 = vertices[0];
                var v2 = vertices[1];
                var v3 = vertices[2];

                // Calculate lighting
                var normal = Vector3.Normalize(Vector3.Transform(triangle.Normal, rotation));
                float lightIntensity = Math.Max(0, -Vector3.Dot(normal, lightDir));
                lightIntensity = 0.3f + (lightIntensity * 0.7f);

                // Set color
                byte colorValue = (byte)(255 * lightIntensity);
                _fillPaint.Color = new SKColor(colorValue, colorValue, colorValue);

                // Reuse path
                path.Rewind();
                path.MoveTo(v1.X, v1.Y);
                path.LineTo(v2.X, v2.Y);
                path.LineTo(v3.X, v3.Y);
                path.Close();

                canvas.DrawPath(path, _fillPaint);
                canvas.DrawPath(path, _strokePaint);
            }

            // Draw axis indicators
            DrawAxisIndicators(canvas, rotation, scale * 0.3f, info);
        }

        private Vector3 TransformVertex(Vector3 vertex, Vector3 center, float modelScale, Matrix4x4 rotation, float screenScale)
        {
            // Combine transformations for fewer operations
            var centered = (vertex - center) * modelScale;
            var rotated = Vector3.Transform(centered, rotation);
            return rotated * screenScale;
        }

        private void DrawAxisIndicators(SKCanvas canvas, Matrix4x4 rotation, float length, SKImageInfo info)
        {
            canvas.Save();
            canvas.ResetMatrix();
            canvas.Translate(50, info.Height - 50);

            var axes = new[]
            {
                (axis: Vector3.UnitX, color: SKColors.Red, label: "X"),
                (axis: Vector3.UnitY, color: SKColors.Green, label: "Y"),
                (axis: Vector3.UnitZ, color: SKColors.Blue, label: "Z")
            };

            using var linePaint = new SKPaint
            {
                StrokeWidth = 2,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };

            using var textPaint = new SKPaint
            {
                TextSize = 12,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center
            };

            foreach (var (axis, color, label) in axes)
            {
                var transformed = Vector3.Transform(axis, rotation) * length;

                linePaint.Color = color;
                canvas.DrawLine(0, 0, transformed.X, -transformed.Y, linePaint);

                textPaint.Color = color;
                canvas.DrawText(label, transformed.X, -transformed.Y - 5, textPaint);
            }

            canvas.Restore();
        }
    }
}
