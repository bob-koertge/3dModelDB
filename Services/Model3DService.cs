using MauiApp3.Models;

namespace MauiApp3.Services
{
    /// <summary>
    /// Service for handling 3D model file operations
    /// </summary>
    public class Model3DService
    {
        private readonly StlParser _stlParser;
        private readonly ThreeMfParser _threeMfParser;
        private readonly ThumbnailGenerator _thumbnailGenerator;

        public Model3DService()
        {
            _stlParser = new StlParser();
            _threeMfParser = new ThreeMfParser();
            _thumbnailGenerator = new ThumbnailGenerator();
        }

        /// <summary>
        /// Validates if a file is a supported 3D model format
        /// </summary>
        public bool IsSupportedFormat(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension == ".stl" || extension == ".3mf";
        }

        /// <summary>
        /// Loads and parses an STL file
        /// </summary>
        public async Task<StlParser.StlModel?> LoadStlModelAsync(string filePath)
        {
            return await _stlParser.ParseFileAsync(filePath);
        }

        /// <summary>
        /// Loads and parses a 3MF file
        /// </summary>
        public async Task<StlParser.StlModel?> Load3MfModelAsync(string filePath)
        {
            return await _threeMfParser.ParseFileAsync(filePath);
        }

        /// <summary>
        /// Loads a 3D model file (auto-detects format)
        /// </summary>
        public async Task<StlParser.StlModel?> LoadModelAsync(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            return extension switch
            {
                ".stl" => await LoadStlModelAsync(filePath),
                ".3mf" => await Load3MfModelAsync(filePath),
                _ => null
            };
        }

        /// <summary>
        /// Generates a thumbnail for a 3D model
        /// </summary>
        public async Task<byte[]?> GenerateThumbnailAsync(string filePath, StlParser.StlModel? model = null)
        {
            try
            {
                // If model not provided, try to load it
                if (model == null && File.Exists(filePath))
                {
                    model = await LoadModelAsync(filePath);
                }

                // Generate thumbnail from model
                if (model != null)
                {
                    return await _thumbnailGenerator.GenerateThumbnailAsync(model, 200, 200);
                }

                // Fallback to placeholder
                var fileType = Path.GetExtension(filePath).ToUpperInvariant().TrimStart('.');
                return _thumbnailGenerator.GeneratePlaceholderThumbnail(fileType, 200, 200);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating thumbnail: {ex.Message}");
                // Return placeholder on error
                var fileType = Path.GetExtension(filePath).ToUpperInvariant().TrimStart('.');
                return _thumbnailGenerator.GeneratePlaceholderThumbnail(fileType, 200, 200);
            }
        }

        /// <summary>
        /// Parses an STL file and extracts model information
        /// </summary>
        public async Task<Model3DInfo?> ParseStlAsync(string filePath)
        {
            var model = await _stlParser.ParseFileAsync(filePath);
            if (model == null) return null;

            return new Model3DInfo
            {
                TriangleCount = model.Triangles.Count,
                VertexCount = model.Triangles.Count * 3,
                BoundingBox = new BoundingBox
                {
                    MinX = model.MinBounds.X,
                    MinY = model.MinBounds.Y,
                    MinZ = model.MinBounds.Z,
                    MaxX = model.MaxBounds.X,
                    MaxY = model.MaxBounds.Y,
                    MaxZ = model.MaxBounds.Z
                }
            };
        }

        /// <summary>
        /// Parses a 3MF file and extracts model information
        /// </summary>
        public async Task<Model3DInfo?> Parse3mfAsync(string filePath)
        {
            var model = await _threeMfParser.ParseFileAsync(filePath);
            if (model == null) return null;

            return new Model3DInfo
            {
                TriangleCount = model.Triangles.Count,
                VertexCount = model.Triangles.Count * 3,
                BoundingBox = new BoundingBox
                {
                    MinX = model.MinBounds.X,
                    MinY = model.MinBounds.Y,
                    MinZ = model.MinBounds.Z,
                    MaxX = model.MaxBounds.X,
                    MaxY = model.MaxBounds.Y,
                    MaxZ = model.MaxBounds.Z
                }
            };
        }

        /// <summary>
        /// Exports a 3D model to a different format
        /// </summary>
        public async Task<bool> ExportModelAsync(Model3DFile model, string targetFormat, string outputPath)
        {
            await Task.Delay(100); // Simulate processing
            // TODO: Implement format conversion
            return false;
        }

        /// <summary>
        /// Calculates the bounding box of a 3D model
        /// </summary>
        public async Task<BoundingBox?> GetBoundingBoxAsync(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            var info = extension switch
            {
                ".stl" => await ParseStlAsync(filePath),
                ".3mf" => await Parse3mfAsync(filePath),
                _ => null
            };
            
            return info?.BoundingBox;
        }
    }

    /// <summary>
    /// Information about a parsed 3D model
    /// </summary>
    public class Model3DInfo
    {
        public int TriangleCount { get; set; }
        public int VertexCount { get; set; }
        public BoundingBox? BoundingBox { get; set; }
        public string? MaterialInfo { get; set; }
    }

    /// <summary>
    /// Represents a 3D bounding box
    /// </summary>
    public class BoundingBox
    {
        public float MinX { get; set; }
        public float MinY { get; set; }
        public float MinZ { get; set; }
        public float MaxX { get; set; }
        public float MaxY { get; set; }
        public float MaxZ { get; set; }

        public float Width => MaxX - MinX;
        public float Height => MaxY - MinY;
        public float Depth => MaxZ - MinZ;
    }
}
