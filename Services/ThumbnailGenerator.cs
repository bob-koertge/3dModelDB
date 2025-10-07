using SkiaSharp;
using System.Numerics;
using MauiApp3.Services;

namespace MauiApp3.Services
{
    /// <summary>
    /// Generates thumbnail images for 3D models - optimized version
    /// </summary>
    public class ThumbnailGenerator
    {
        private const int DefaultThumbnailSize = 200;
        private const float RotationXDeg = 30f;
        private const float RotationYDeg = 45f;
        private const float DegToRadFactor = MathF.PI / 180f;

        // Cache for thumbnail generation
        private readonly SKPaint _fillPaint;
        private readonly SKPaint _borderPaint;
        private readonly SKPaint _iconPaint;
        private readonly SKPaint _typePaint;

        public ThumbnailGenerator()
        {
            // Pre-create reusable paint objects
            _fillPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            _borderPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = new SKColor(100, 100, 100),
                StrokeWidth = 2,
                IsAntialias = true
            };

            _iconPaint = new SKPaint
            {
                Color = new SKColor(100, 100, 100),
                TextSize = 60,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center
            };

            _typePaint = new SKPaint
            {
                Color = new SKColor(0, 120, 212),
                TextSize = 16,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            };
        }

        /// <summary>
        /// Generates a thumbnail image from a parsed 3D model
        /// </summary>
        public Task<byte[]?> GenerateThumbnailAsync(StlParser.StlModel? model, int width = DefaultThumbnailSize, int height = DefaultThumbnailSize)
        {
            return Task.Run(() => GenerateThumbnail(model, width, height));
        }

        private byte[]? GenerateThumbnail(StlParser.StlModel? model, int width, int height)
        {
            try
            {
                using var surface = SKSurface.Create(new SKImageInfo(width, height));
                var canvas = surface.Canvas;
                
                // Clear with dark background
                canvas.Clear(new SKColor(30, 30, 30));

                if (model == null || model.Triangles.Count == 0)
                    return null;

                // Setup rendering
                float centerX = width * 0.5f;
                float centerY = height * 0.5f;
                float scale = Math.Min(width, height) * 0.35f;

                canvas.Translate(centerX, centerY);

                // Create transformation with fixed viewing angle
                var rotation = Matrix4x4.CreateRotationX(RotationXDeg * DegToRadFactor) *
                              Matrix4x4.CreateRotationY(RotationYDeg * DegToRadFactor);

                // Pre-calculate light direction
                var lightDir = Vector3.Normalize(new Vector3(0.5f, -0.7f, -1));

                // Transform and sort triangles
                var transformedTriangles = new List<(float depth, Vector3[] vertices, float light)>(model.Triangles.Count);

                foreach (var triangle in model.Triangles)
                {
                    var v1 = TransformVertex(triangle.Vertex1, model.Center, model.Scale, rotation, scale);
                    var v2 = TransformVertex(triangle.Vertex2, model.Center, model.Scale, rotation, scale);
                    var v3 = TransformVertex(triangle.Vertex3, model.Center, model.Scale, rotation, scale);

                    float avgDepth = (v1.Z + v2.Z + v3.Z) * 0.333333f;

                    // Calculate lighting
                    var normal = Vector3.Normalize(Vector3.Transform(triangle.Normal, rotation));
                    float lightIntensity = MathF.Max(0, -Vector3.Dot(normal, lightDir));
                    lightIntensity = 0.4f + (lightIntensity * 0.6f);

                    // Backface culling
                    float cross = (v2.X - v1.X) * (v3.Y - v1.Y) - (v2.Y - v1.Y) * (v3.X - v1.X);
                    
                    if (cross >= 0)
                    {
                        transformedTriangles.Add((avgDepth, new[] { v1, v2, v3 }, lightIntensity));
                    }
                }

                // Sort by depth
                transformedTriangles.Sort((a, b) => a.depth.CompareTo(b.depth));

                // Draw triangles
                using var path = new SKPath();
                foreach (var (_, vertices, lightIntensity) in transformedTriangles)
                {
                    var v1 = vertices[0];
                    var v2 = vertices[1];
                    var v3 = vertices[2];

                    // Create color with accent tint
                    byte baseColor = (byte)(200 * lightIntensity);
                    _fillPaint.Color = new SKColor(
                        (byte)Math.Min(255, baseColor + 20),
                        (byte)Math.Min(255, baseColor + 30),
                        (byte)Math.Min(255, baseColor + 50)
                    );

                    path.Rewind();
                    path.MoveTo(v1.X, v1.Y);
                    path.LineTo(v2.X, v2.Y);
                    path.LineTo(v3.X, v3.Y);
                    path.Close();

                    canvas.DrawPath(path, _fillPaint);
                }

                // Add subtle border
                canvas.ResetMatrix();
                canvas.DrawRect(1, 1, width - 2, height - 2, _borderPaint);

                // Convert to byte array
                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 90);
                return data.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating thumbnail: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generates a placeholder thumbnail with model info
        /// </summary>
        public byte[] GeneratePlaceholderThumbnail(string fileType, int width = DefaultThumbnailSize, int height = DefaultThumbnailSize)
        {
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            
            // Dark background
            canvas.Clear(new SKColor(30, 30, 30));

            // Draw centered icon
            canvas.DrawText("??", width * 0.5f, height * 0.5f + 20, _iconPaint);

            // Draw file type
            canvas.DrawText(fileType, width * 0.5f, height - 20, _typePaint);

            // Convert to byte array
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 90);
            return data.ToArray();
        }

        private Vector3 TransformVertex(Vector3 vertex, Vector3 center, float modelScale, Matrix4x4 rotation, float screenScale)
        {
            var centered = (vertex - center) * modelScale;
            var rotated = Vector3.Transform(centered, rotation);
            return rotated * screenScale;
        }
    }
}
