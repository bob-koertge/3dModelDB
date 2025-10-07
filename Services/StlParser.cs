using System.Buffers;
using System.Numerics;
using System.Text;

namespace MauiApp3.Services
{
    /// <summary>
    /// Parser for STL (Stereolithography) files - supports both ASCII and Binary formats
    /// Optimized for performance and memory efficiency
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

        private const int BinaryHeaderSize = 80;
        private const int BinaryTriangleSize = 50;

        public async Task<StlModel?> ParseFileAsync(string filePath)
        {
            try
            {
                // Use FileStream for better memory management with large files
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                
                // Read header to determine format
                byte[] header = ArrayPool<byte>.Shared.Rent(84);
                try
                {
                    int bytesRead = await fileStream.ReadAsync(header.AsMemory(0, 84));
                    if (bytesRead < 84)
                    {
                        // File too small, try ASCII
                        fileStream.Position = 0;
                        return await ParseAsciiStlAsync(fileStream);
                    }

                    if (IsBinaryStl(header, (int)fileStream.Length))
                    {
                        fileStream.Position = 0;
                        return await ParseBinaryStlAsync(fileStream);
                    }
                    else
                    {
                        fileStream.Position = 0;
                        return await ParseAsciiStlAsync(fileStream);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(header);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing STL file: {ex.Message}");
                return null;
            }
        }

        private bool IsBinaryStl(byte[] header, int fileLength)
        {
            // Check if starts with "solid" (ASCII format)
            ReadOnlySpan<byte> solidBytes = "solid"u8;
            ReadOnlySpan<byte> headerSpan = header.AsSpan(0, Math.Min(80, header.Length));
            
            // Trim leading whitespace
            int start = 0;
            while (start < headerSpan.Length && char.IsWhiteSpace((char)headerSpan[start]))
                start++;

            if (headerSpan.Slice(start).StartsWith(solidBytes))
            {
                // Could still be binary, check triangle count
                if (fileLength >= 84)
                {
                    uint triangleCount = BitConverter.ToUInt32(header, BinaryHeaderSize);
                    long expectedSize = 84 + ((long)triangleCount * BinaryTriangleSize);
                    return fileLength == expectedSize;
                }
                return false;
            }
            return true;
        }

        private async Task<StlModel> ParseBinaryStlAsync(FileStream stream)
        {
            var model = new StlModel();
            
            // Read header and triangle count
            byte[] header = new byte[84];
            await stream.ReadAsync(header.AsMemory());
            
            uint triangleCount = BitConverter.ToUInt32(header, BinaryHeaderSize);
            model.Triangles.Capacity = (int)triangleCount;

            Vector3 min = new(float.MaxValue);
            Vector3 max = new(float.MinValue);

            // Read triangles in chunks for better performance
            const int chunkSize = 100;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BinaryTriangleSize * chunkSize);
            
            try
            {
                uint remaining = triangleCount;
                while (remaining > 0)
                {
                    uint toRead = Math.Min(remaining, chunkSize);
                    int bytesToRead = (int)(toRead * BinaryTriangleSize);
                    
                    await stream.ReadAsync(buffer.AsMemory(0, bytesToRead));
                    
                    for (uint i = 0; i < toRead; i++)
                    {
                        int offset = (int)(i * BinaryTriangleSize);
                        
                        var triangle = new Triangle
                        {
                            Normal = ReadVector3(buffer, offset),
                            Vertex1 = ReadVector3(buffer, offset + 12),
                            Vertex2 = ReadVector3(buffer, offset + 24),
                            Vertex3 = ReadVector3(buffer, offset + 36)
                        };

                        model.Triangles.Add(triangle);

                        // Update bounds
                        min = Vector3.Min(min, Vector3.Min(triangle.Vertex1, Vector3.Min(triangle.Vertex2, triangle.Vertex3)));
                        max = Vector3.Max(max, Vector3.Max(triangle.Vertex1, Vector3.Max(triangle.Vertex2, triangle.Vertex3)));
                    }
                    
                    remaining -= toRead;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            CalculateModelProperties(model, min, max);
            return model;
        }

        private Vector3 ReadVector3(byte[] buffer, int offset)
        {
            return new Vector3(
                BitConverter.ToSingle(buffer, offset),
                BitConverter.ToSingle(buffer, offset + 4),
                BitConverter.ToSingle(buffer, offset + 8)
            );
        }

        private async Task<StlModel> ParseAsciiStlAsync(FileStream stream)
        {
            var model = new StlModel();
            Vector3 min = new(float.MaxValue);
            Vector3 max = new(float.MinValue);

            using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, 4096);
            
            Triangle? currentTriangle = null;
            int vertexIndex = 0;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var trimmed = line.AsSpan().Trim();
                
                if (trimmed.StartsWith("facet normal"))
                {
                    currentTriangle = new Triangle();
                    ParseNormal(trimmed, currentTriangle);
                    vertexIndex = 0;
                }
                else if (trimmed.StartsWith("vertex") && currentTriangle != null)
                {
                    var vertex = ParseVertex(trimmed);
                    
                    switch (vertexIndex)
                    {
                        case 0: currentTriangle.Vertex1 = vertex; break;
                        case 1: currentTriangle.Vertex2 = vertex; break;
                        case 2: currentTriangle.Vertex3 = vertex; break;
                    }
                    
                    min = Vector3.Min(min, vertex);
                    max = Vector3.Max(max, vertex);
                    vertexIndex++;
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

        private void ParseNormal(ReadOnlySpan<char> line, Triangle triangle)
        {
            // Skip "facet normal "
            var values = line.Slice(12).Trim();
            
            Span<Range> ranges = stackalloc Range[3];
            int count = values.Split(ranges, ' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (count >= 3)
            {
                triangle.Normal = new Vector3(
                    float.Parse(values[ranges[0]]),
                    float.Parse(values[ranges[1]]),
                    float.Parse(values[ranges[2]])
                );
            }
        }

        private Vector3 ParseVertex(ReadOnlySpan<char> line)
        {
            // Skip "vertex "
            var values = line.Slice(6).Trim();
            
            Span<Range> ranges = stackalloc Range[3];
            int count = values.Split(ranges, ' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (count >= 3)
            {
                return new Vector3(
                    float.Parse(values[ranges[0]]),
                    float.Parse(values[ranges[1]]),
                    float.Parse(values[ranges[2]])
                );
            }
            
            return Vector3.Zero;
        }

        private void CalculateModelProperties(StlModel model, Vector3 min, Vector3 max)
        {
            model.MinBounds = min;
            model.MaxBounds = max;
            model.Center = (min + max) * 0.5f;

            // Calculate scale to fit in a unit cube
            Vector3 size = max - min;
            float maxDimension = MathF.Max(MathF.Max(size.X, size.Y), size.Z);
            model.Scale = maxDimension > 0 ? 1.0f / maxDimension : 1.0f;
        }
    }
}
