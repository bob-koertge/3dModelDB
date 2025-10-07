using System.Numerics;
using System.Text;

namespace MauiApp3.Services
{
    /// <summary>
    /// Parser for STL (Stereolithography) files - supports both ASCII and Binary formats
    /// </summary>
    public class StlParser
    {
        public class Triangle
        {
            public Vector3 Normal { get; set; }
            public Vector3 Vertex1 { get; set; }
            public Vector3 Vertex2 { get; set; }
            public Vector3 Vertex3 { get; set; }
        }

        public class StlModel
        {
            public List<Triangle> Triangles { get; set; } = new();
            public Vector3 MinBounds { get; set; }
            public Vector3 MaxBounds { get; set; }
            public Vector3 Center { get; set; }
            public float Scale { get; set; } = 1.0f;
        }

        public async Task<StlModel?> ParseFileAsync(string filePath)
        {
            try
            {
                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                
                // Check if binary or ASCII
                if (IsBinaryStl(fileBytes))
                {
                    return ParseBinaryStl(fileBytes);
                }
                else
                {
                    return ParseAsciiStl(fileBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing STL file: {ex.Message}");
                return null;
            }
        }

        private bool IsBinaryStl(byte[] data)
        {
            if (data.Length < 84) return false;
            
            // Check if starts with "solid" (ASCII format)
            string header = Encoding.ASCII.GetString(data, 0, Math.Min(80, data.Length));
            if (header.TrimStart().StartsWith("solid", StringComparison.OrdinalIgnoreCase))
            {
                // Could still be binary with "solid" in header, check further
                if (data.Length >= 84)
                {
                    uint triangleCount = BitConverter.ToUInt32(data, 80);
                    long expectedSize = 84 + ((long)triangleCount * 50);
                    return data.Length == expectedSize;
                }
                return false;
            }
            return true;
        }

        private StlModel ParseBinaryStl(byte[] data)
        {
            var model = new StlModel();
            
            // Skip 80-byte header
            int offset = 80;
            
            // Read number of triangles
            uint triangleCount = BitConverter.ToUInt32(data, offset);
            offset += 4;

            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            for (uint i = 0; i < triangleCount; i++)
            {
                var triangle = new Triangle();
                
                // Normal vector (12 bytes)
                triangle.Normal = new Vector3(
                    BitConverter.ToSingle(data, offset),
                    BitConverter.ToSingle(data, offset + 4),
                    BitConverter.ToSingle(data, offset + 8)
                );
                offset += 12;

                // Vertex 1 (12 bytes)
                triangle.Vertex1 = new Vector3(
                    BitConverter.ToSingle(data, offset),
                    BitConverter.ToSingle(data, offset + 4),
                    BitConverter.ToSingle(data, offset + 8)
                );
                offset += 12;

                // Vertex 2 (12 bytes)
                triangle.Vertex2 = new Vector3(
                    BitConverter.ToSingle(data, offset),
                    BitConverter.ToSingle(data, offset + 4),
                    BitConverter.ToSingle(data, offset + 8)
                );
                offset += 12;

                // Vertex 3 (12 bytes)
                triangle.Vertex3 = new Vector3(
                    BitConverter.ToSingle(data, offset),
                    BitConverter.ToSingle(data, offset + 4),
                    BitConverter.ToSingle(data, offset + 8)
                );
                offset += 12;

                // Skip attribute byte count (2 bytes)
                offset += 2;

                model.Triangles.Add(triangle);

                // Update bounds
                UpdateBounds(ref min, ref max, triangle.Vertex1);
                UpdateBounds(ref min, ref max, triangle.Vertex2);
                UpdateBounds(ref min, ref max, triangle.Vertex3);
            }

            CalculateModelProperties(model, min, max);
            return model;
        }

        private StlModel ParseAsciiStl(byte[] data)
        {
            var model = new StlModel();
            string content = Encoding.ASCII.GetString(data);
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            Triangle? currentTriangle = null;
            int vertexIndex = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                if (trimmed.StartsWith("facet normal"))
                {
                    currentTriangle = new Triangle();
                    var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5)
                    {
                        currentTriangle.Normal = new Vector3(
                            float.Parse(parts[2]),
                            float.Parse(parts[3]),
                            float.Parse(parts[4])
                        );
                    }
                    vertexIndex = 0;
                }
                else if (trimmed.StartsWith("vertex") && currentTriangle != null)
                {
                    var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        var vertex = new Vector3(
                            float.Parse(parts[1]),
                            float.Parse(parts[2]),
                            float.Parse(parts[3])
                        );

                        switch (vertexIndex)
                        {
                            case 0: currentTriangle.Vertex1 = vertex; break;
                            case 1: currentTriangle.Vertex2 = vertex; break;
                            case 2: currentTriangle.Vertex3 = vertex; break;
                        }
                        
                        UpdateBounds(ref min, ref max, vertex);
                        vertexIndex++;
                    }
                }
                else if (trimmed.StartsWith("endfacet") && currentTriangle != null)
                {
                    model.Triangles.Add(currentTriangle);
                    currentTriangle = null;
                }
            }

            CalculateModelProperties(model, min, max);
            return model;
        }

        private void UpdateBounds(ref Vector3 min, ref Vector3 max, Vector3 vertex)
        {
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);
        }

        private void CalculateModelProperties(StlModel model, Vector3 min, Vector3 max)
        {
            model.MinBounds = min;
            model.MaxBounds = max;
            model.Center = (min + max) / 2;

            // Calculate scale to fit in a unit cube
            Vector3 size = max - min;
            float maxDimension = Math.Max(Math.Max(size.X, size.Y), size.Z);
            model.Scale = maxDimension > 0 ? 1.0f / maxDimension : 1.0f;
        }
    }
}
