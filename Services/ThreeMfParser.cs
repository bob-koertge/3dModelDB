using System.IO.Compression;
using System.Numerics;
using System.Xml.Linq;

namespace MauiApp3.Services
{
    /// <summary>
    /// Parser for 3MF (3D Manufacturing Format) files
    /// 3MF is a ZIP archive containing XML files with 3D model data
    /// </summary>
    public class ThreeMfParser
    {
        /// <summary>
        /// Parses a 3MF file and converts it to StlParser.StlModel format for rendering
        /// </summary>
        public async Task<StlParser.StlModel?> ParseFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var archive = ZipFile.OpenRead(filePath);
                    
                    // Find the 3D model file (typically 3D/3dmodel.model)
                    var modelEntry = archive.Entries.FirstOrDefault(e => 
                        e.FullName.EndsWith(".model", StringComparison.OrdinalIgnoreCase) ||
                        e.FullName.Contains("3dmodel", StringComparison.OrdinalIgnoreCase));

                    if (modelEntry == null)
                    {
                        Console.WriteLine("No 3D model file found in 3MF archive");
                        return null;
                    }

                    using var stream = modelEntry.Open();
                    var doc = XDocument.Load(stream);

                    // Parse the XML (3MF uses specific namespaces)
                    XNamespace ns = "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";
                    
                    var model = new StlParser.StlModel();
                    var triangles = new List<StlParser.Triangle>();

                    Vector3 min = new Vector3(float.MaxValue);
                    Vector3 max = new Vector3(float.MinValue);

                    // Find all mesh objects
                    var meshes = doc.Descendants(ns + "mesh");
                    
                    foreach (var mesh in meshes)
                    {
                        // Parse vertices
                        var verticesElement = mesh.Element(ns + "vertices");
                        if (verticesElement == null) continue;

                        var vertices = new List<Vector3>();
                        foreach (var vertex in verticesElement.Elements(ns + "vertex"))
                        {
                            float x = float.Parse(vertex.Attribute("x")?.Value ?? "0");
                            float y = float.Parse(vertex.Attribute("y")?.Value ?? "0");
                            float z = float.Parse(vertex.Attribute("z")?.Value ?? "0");
                            
                            var v = new Vector3(x, y, z);
                            vertices.Add(v);

                            // Update bounds
                            min = Vector3.Min(min, v);
                            max = Vector3.Max(max, v);
                        }

                        // Parse triangles
                        var trianglesElement = mesh.Element(ns + "triangles");
                        if (trianglesElement == null) continue;

                        foreach (var triangle in trianglesElement.Elements(ns + "triangle"))
                        {
                            int v1 = int.Parse(triangle.Attribute("v1")?.Value ?? "0");
                            int v2 = int.Parse(triangle.Attribute("v2")?.Value ?? "0");
                            int v3 = int.Parse(triangle.Attribute("v3")?.Value ?? "0");

                            if (v1 < vertices.Count && v2 < vertices.Count && v3 < vertices.Count)
                            {
                                var vert1 = vertices[v1];
                                var vert2 = vertices[v2];
                                var vert3 = vertices[v3];

                                // Calculate normal
                                var edge1 = vert2 - vert1;
                                var edge2 = vert3 - vert1;
                                var normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                                triangles.Add(new StlParser.Triangle
                                {
                                    Normal = normal,
                                    Vertex1 = vert1,
                                    Vertex2 = vert2,
                                    Vertex3 = vert3
                                });
                            }
                        }
                    }

                    model.Triangles = triangles;
                    model.MinBounds = min;
                    model.MaxBounds = max;
                    model.Center = (min + max) / 2;

                    // Calculate scale to fit in a unit cube
                    Vector3 size = max - min;
                    float maxDimension = Math.Max(Math.Max(size.X, size.Y), size.Z);
                    model.Scale = maxDimension > 0 ? 1.0f / maxDimension : 1.0f;

                    Console.WriteLine($"Parsed 3MF: {triangles.Count} triangles");
                    return model;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing 3MF file: {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Validates if a file is a valid 3MF archive
        /// </summary>
        public bool IsValid3MfFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                using var archive = ZipFile.OpenRead(filePath);
                
                // Check for required files
                bool hasModelFile = archive.Entries.Any(e => 
                    e.FullName.EndsWith(".model", StringComparison.OrdinalIgnoreCase));
                
                bool hasRelsFile = archive.Entries.Any(e => 
                    e.FullName.Contains("_rels", StringComparison.OrdinalIgnoreCase));

                return hasModelFile; // .rels is optional but .model is required
            }
            catch
            {
                return false;
            }
        }
    }
}
