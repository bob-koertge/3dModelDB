using SkiaSharp;
using System.Numerics;
using MauiApp3.Services;

namespace MauiApp3.Services
{
    /// <summary>
    /// Generates thumbnail images for 3D models
    /// </summary>
    public class ThumbnailGenerator
    {
        /// <summary>
        /// Generates a thumbnail image from a parsed 3D model
        /// </summary>
        public async Task<byte[]?> GenerateThumbnailAsync(StlParser.StlModel model, int width = 200, int height = 200)
        {
            return await Task.Run(() =>
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
                    float centerX = width / 2f;
                    float centerY = height / 2f;
                    float scale = Math.Min(width, height) * 0.35f;

                    canvas.Translate(centerX, centerY);

                    // Create transformation with fixed viewing angle for thumbnails
                    var rotation = Matrix4x4.CreateRotationX(DegToRad(30)) *
                                  Matrix4x4.CreateRotationY(DegToRad(45)) *
                                  Matrix4x4.CreateRotationZ(DegToRad(0));

                    // Transform and sort triangles
                    var transformedTriangles = new List<(float depth, Vector3[] vertices, float light)>();

                    foreach (var triangle in model.Triangles)
                    {
                        var v1 = TransformVertex(triangle.Vertex1, model.Center, model.Scale, rotation, scale);
                        var v2 = TransformVertex(triangle.Vertex2, model.Center, model.Scale, rotation, scale);
                        var v3 = TransformVertex(triangle.Vertex3, model.Center, model.Scale, rotation, scale);

                        float avgDepth = (v1.Z + v2.Z + v3.Z) / 3;

                        // Calculate lighting
                        var normal = Vector3.Normalize(Vector3.Transform(triangle.Normal, rotation));
                        var lightDir = Vector3.Normalize(new Vector3(0.5f, -0.7f, -1));
                        float lightIntensity = Math.Max(0, Vector3.Dot(normal, -lightDir));
                        lightIntensity = 0.4f + (lightIntensity * 0.6f);

                        // Backface culling
                        var edge1 = new Vector2(v2.X - v1.X, v2.Y - v1.Y);
                        var edge2 = new Vector2(v3.X - v1.X, v3.Y - v1.Y);
                        float cross = edge1.X * edge2.Y - edge1.Y * edge2.X;
                        
                        if (cross >= 0)
                        {
                            transformedTriangles.Add((avgDepth, new[] { v1, v2, v3 }, lightIntensity));
                        }
                    }

                    // Sort by depth
                    transformedTriangles.Sort((a, b) => a.depth.CompareTo(b.depth));

                    // Draw triangles
                    foreach (var (_, vertices, lightIntensity) in transformedTriangles)
                    {
                        var v1 = vertices[0];
                        var v2 = vertices[1];
                        var v3 = vertices[2];

                        // Create color with accent tint
                        byte baseColor = (byte)(200 * lightIntensity);
                        var color = new SKColor(
                            (byte)Math.Min(255, baseColor + 20),  // Slight blue tint
                            (byte)Math.Min(255, baseColor + 30),
                            (byte)Math.Min(255, baseColor + 50)
                        );

                        using var path = new SKPath();
                        path.MoveTo(v1.X, v1.Y);
                        path.LineTo(v2.X, v2.Y);
                        path.LineTo(v3.X, v3.Y);
                        path.Close();

                        using var paint = new SKPaint
                        {
                            Style = SKPaintStyle.Fill,
                            Color = color,
                            IsAntialias = true
                        };

                        canvas.DrawPath(path, paint);
                    }

                    // Add subtle border
                    using var borderPaint = new SKPaint
                    {
                        Style = SKPaintStyle.Stroke,
                        Color = new SKColor(100, 100, 100),
                        StrokeWidth = 2,
                        IsAntialias = true
                    };
                    canvas.ResetMatrix();
                    canvas.DrawRect(1, 1, width - 2, height - 2, borderPaint);

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
            });
        }

        /// <summary>
        /// Generates a placeholder thumbnail with model info
        /// </summary>
        public byte[] GeneratePlaceholderThumbnail(string fileType, int width = 200, int height = 200)
        {
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            
            // Dark background
            canvas.Clear(new SKColor(30, 30, 30));

            // Draw centered icon
            using var iconPaint = new SKPaint
            {
                Color = new SKColor(100, 100, 100),
                TextSize = 60,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center
            };
            
            canvas.DrawText("??", width / 2f, height / 2f + 20, iconPaint);

            // Draw file type
            using var typePaint = new SKPaint
            {
                Color = new SKColor(0, 120, 212),
                TextSize = 16,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            };
            
            canvas.DrawText(fileType, width / 2f, height - 20, typePaint);

            // Convert to byte array
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 90);
            return data.ToArray();
        }

        private Vector3 TransformVertex(Vector3 vertex, Vector3 center, float modelScale, Matrix4x4 rotation, float screenScale)
        {
            var centered = vertex - center;
            centered *= modelScale;
            var rotated = Vector3.Transform(centered, rotation);
            return rotated * screenScale;
        }

        private float DegToRad(float degrees) => degrees * (float)Math.PI / 180f;
    }
}
