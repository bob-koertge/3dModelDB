using System.Text;

namespace MauiApp3.Utilities
{
    /// <summary>
    /// Utility class to generate simple 3D models for testing
    /// </summary>
    public static class SampleModelGenerator
    {
        /// <summary>
        /// Generates a simple cube STL file
        /// </summary>
        public static async Task<string> GenerateCubeStlAsync(string filePath, float size = 1.0f)
        {
            float half = size / 2;
            
            var vertices = new[]
            {
                // Front face
                (new[] { -half, -half, half }, new[] { half, -half, half }, new[] { half, half, half }),
                (new[] { -half, -half, half }, new[] { half, half, half }, new[] { -half, half, half }),
                // Back face
                (new[] { half, -half, -half }, new[] { -half, -half, -half }, new[] { -half, half, -half }),
                (new[] { half, -half, -half }, new[] { -half, half, -half }, new[] { half, half, -half }),
                // Top face
                (new[] { -half, half, half }, new[] { half, half, half }, new[] { half, half, -half }),
                (new[] { -half, half, half }, new[] { half, half, -half }, new[] { -half, half, -half }),
                // Bottom face
                (new[] { -half, -half, -half }, new[] { half, -half, -half }, new[] { half, -half, half }),
                (new[] { -half, -half, -half }, new[] { half, -half, half }, new[] { -half, -half, half }),
                // Right face
                (new[] { half, -half, half }, new[] { half, -half, -half }, new[] { half, half, -half }),
                (new[] { half, -half, half }, new[] { half, half, -half }, new[] { half, half, half }),
                // Left face
                (new[] { -half, -half, -half }, new[] { -half, -half, half }, new[] { -half, half, half }),
                (new[] { -half, -half, -half }, new[] { -half, half, half }, new[] { -half, half, -half })
            };

            var sb = new StringBuilder();
            sb.AppendLine("solid Cube");

            foreach (var (v1, v2, v3) in vertices)
            {
                // Calculate normal (simplified - just using face direction)
                sb.AppendLine("  facet normal 0 0 0");
                sb.AppendLine("    outer loop");
                sb.AppendLine($"      vertex {v1[0]} {v1[1]} {v1[2]}");
                sb.AppendLine($"      vertex {v2[0]} {v2[1]} {v2[2]}");
                sb.AppendLine($"      vertex {v3[0]} {v3[1]} {v3[2]}");
                sb.AppendLine("    endloop");
                sb.AppendLine("  endfacet");
            }

            sb.AppendLine("endsolid Cube");

            await File.WriteAllTextAsync(filePath, sb.ToString());
            return filePath;
        }

        /// <summary>
        /// Generates a pyramid STL file
        /// </summary>
        public static async Task<string> GeneratePyramidStlAsync(string filePath, float size = 1.0f)
        {
            float half = size / 2;
            float height = size;

            var vertices = new[]
            {
                // Base (4 triangles to make a square)
                (new[] { -half, 0f, -half }, new[] { half, 0f, -half }, new[] { half, 0f, half }),
                (new[] { -half, 0f, -half }, new[] { half, 0f, half }, new[] { -half, 0f, half }),
                // Side faces (4 triangles)
                (new[] { -half, 0f, -half }, new[] { 0f, height, 0f }, new[] { half, 0f, -half }),
                (new[] { half, 0f, -half }, new[] { 0f, height, 0f }, new[] { half, 0f, half }),
                (new[] { half, 0f, half }, new[] { 0f, height, 0f }, new[] { -half, 0f, half }),
                (new[] { -half, 0f, half }, new[] { 0f, height, 0f }, new[] { -half, 0f, -half })
            };

            var sb = new StringBuilder();
            sb.AppendLine("solid Pyramid");

            foreach (var (v1, v2, v3) in vertices)
            {
                sb.AppendLine("  facet normal 0 0 0");
                sb.AppendLine("    outer loop");
                sb.AppendLine($"      vertex {v1[0]} {v1[1]} {v1[2]}");
                sb.AppendLine($"      vertex {v2[0]} {v2[1]} {v2[2]}");
                sb.AppendLine($"      vertex {v3[0]} {v3[1]} {v3[2]}");
                sb.AppendLine("    endloop");
                sb.AppendLine("  endfacet");
            }

            sb.AppendLine("endsolid Pyramid");

            await File.WriteAllTextAsync(filePath, sb.ToString());
            return filePath;
        }
    }
}
