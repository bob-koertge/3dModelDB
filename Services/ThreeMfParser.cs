using System.IO.Compression;
using System.Numerics;
using System.Xml.Linq;

namespace MauiApp3.Services
{
    /// <summary>
    /// Parser for 3MF (3D Manufacturing Format) files
    /// Supports both standard and MakerWorld 3MF structures
    /// </summary>
    public class ThreeMfParser
    {
        private const string Standard3MFNamespace = "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";

        /// <summary>
        /// Parses a 3MF file and combines all objects into a single model
        /// </summary>
        public async Task<StlParser.StlModel?> ParseFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"File not found: {filePath}");
                        return null;
                    }

                    using var archive = ZipFile.OpenRead(filePath);
                    var modelEntries = archive.Entries
                        .Where(e => e.FullName.EndsWith(".model", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    if (modelEntries.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("No .model files found in archive");
                        return null;
                    }

                    var allTriangles = new List<StlParser.Triangle>();
                    var globalMin = new Vector3(float.MaxValue);
                    var globalMax = new Vector3(float.MinValue);

                    foreach (var modelEntry in modelEntries)
                    {
                        ProcessModelEntry(modelEntry, ref allTriangles, ref globalMin, ref globalMax);
                    }

                    if (allTriangles.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("No triangles found in any model file");
                        return null;
                    }

                    var size = globalMax - globalMin;
                    var maxDimension = Math.Max(Math.Max(size.X, size.Y), size.Z);

                    return new StlParser.StlModel
                    {
                        Triangles = allTriangles,
                        MinBounds = globalMin,
                        MaxBounds = globalMax,
                        Center = (globalMin + globalMax) / 2,
                        Scale = maxDimension > 0 ? 1.0f / maxDimension : 1.0f
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Parse error: {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Gets the count of mesh objects in the 3MF file
        /// </summary>
        public async Task<int> GetObjectCountAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var archive = ZipFile.OpenRead(filePath);
                    var modelEntries = archive.Entries
                        .Where(e => e.FullName.EndsWith(".model", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (modelEntries.Count == 0) return 0;

                    int totalMeshCount = 0;

                    foreach (var modelEntry in modelEntries)
                    {
                        using var stream = modelEntry.Open();
                        var doc = XDocument.Load(stream);
                        var ns = DetectNamespace(doc);
                        var resources = FindResources(doc, ns);

                        if (resources == null) continue;

                        var objects = GetObjects(resources, ns);
                        
                        foreach (var obj in objects)
                        {
                            var mesh = FindMesh(obj, ns);
                            if (mesh != null && HasGeometry(mesh))
                            {
                                totalMeshCount++;
                            }
                        }
                    }
                    
                    return totalMeshCount;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Object count error: {ex.Message}");
                    return 0;
                }
            });
        }

        /// <summary>
        /// Parses individual objects from a 3MF file as separate models
        /// </summary>
        public async Task<List<(string objectName, StlParser.StlModel model)>> ParseMultipleObjectsAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var results = new List<(string, StlParser.StlModel)>();

                try
                {
                    using var archive = ZipFile.OpenRead(filePath);
                    var modelEntries = archive.Entries
                        .Where(e => e.FullName.EndsWith(".model", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (modelEntries.Count == 0) return results;

                    foreach (var modelEntry in modelEntries)
                    {
                        using var stream = modelEntry.Open();
                        var doc = XDocument.Load(stream);
                        var ns = DetectNamespace(doc);
                        var resources = FindResources(doc, ns);

                        if (resources == null) continue;

                        var objects = GetObjects(resources, ns);
                        
                        int objectIndex = 1;
                        foreach (var obj in objects)
                        {
                            var parsedModel = ParseSingleObject(obj, ns, objectIndex);
                            if (parsedModel != null)
                            {
                                var objectId = obj.Attribute("id")?.Value ?? objectIndex.ToString();
                                var objectName = obj.Attribute("name")?.Value ?? $"Object {objectId}";
                                results.Add((objectName, parsedModel));
                            }
                            objectIndex++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Multi-object parse error: {ex.Message}");
                }

                return results;
            });
        }

        #region Private Helper Methods

        private void ProcessModelEntry(ZipArchiveEntry modelEntry, ref List<StlParser.Triangle> triangles, 
                                      ref Vector3 globalMin, ref Vector3 globalMax)
        {
            try
            {
                using var stream = modelEntry.Open();
                var doc = XDocument.Load(stream);
                var ns = DetectNamespace(doc);
                var resources = FindResources(doc, ns);

                if (resources == null) return;

                var objects = GetObjects(resources, ns);

                foreach (var obj in objects)
                {
                    var objectType = obj.Attribute("type")?.Value;
                    if (objectType != null && objectType != "model" && objectType != "object")
                        continue;

                    ParseObjectGeometry(obj, ns, triangles, ref globalMin, ref globalMax);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Model entry processing error: {ex.Message}");
            }
        }

        private void ParseObjectGeometry(XElement obj, XNamespace ns, List<StlParser.Triangle> triangles,
                                        ref Vector3 globalMin, ref Vector3 globalMax)
        {
            var mesh = FindMesh(obj, ns);
            if (mesh == null) return;

            var vertices = ParseVertices(mesh, ns, ref globalMin, ref globalMax);
            if (vertices.Count == 0) return;

            ParseTriangles(mesh, ns, vertices, triangles);
        }

        private StlParser.StlModel? ParseSingleObject(XElement obj, XNamespace ns, int index)
        {
            var objectType = obj.Attribute("type")?.Value;
            if (objectType != null && objectType != "model" && objectType != "object")
                return null;

            var mesh = FindMesh(obj, ns);
            if (mesh == null) return null;

            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            var vertices = ParseVertices(mesh, ns, ref min, ref max);
            
            if (vertices.Count == 0) return null;

            var triangles = new List<StlParser.Triangle>();
            ParseTriangles(mesh, ns, vertices, triangles);

            if (triangles.Count == 0) return null;

            var size = max - min;
            var maxDimension = Math.Max(Math.Max(size.X, size.Y), size.Z);

            return new StlParser.StlModel
            {
                Triangles = triangles,
                MinBounds = min,
                MaxBounds = max,
                Center = (min + max) / 2,
                Scale = maxDimension > 0 ? 1.0f / maxDimension : 1.0f
            };
        }

        private List<Vector3> ParseVertices(XElement mesh, XNamespace ns, ref Vector3 min, ref Vector3 max)
        {
            var vertices = new List<Vector3>();
            var verticesElement = mesh.Element(ns + "vertices") ?? 
                                 mesh.Elements().FirstOrDefault(e => e.Name.LocalName == "vertices");

            if (verticesElement == null) return vertices;

            var vertexElements = verticesElement.Elements(ns + "vertex").ToList();
            if (vertexElements.Count == 0)
            {
                vertexElements = verticesElement.Elements().Where(e => e.Name.LocalName == "vertex").ToList();
            }

            vertices.Capacity = vertexElements.Count;

            foreach (var vertex in vertexElements)
            {
                try
                {
                    var x = float.Parse((vertex.Attribute("x") ?? vertex.Attribute("X"))?.Value ?? "0", 
                                       System.Globalization.CultureInfo.InvariantCulture);
                    var y = float.Parse((vertex.Attribute("y") ?? vertex.Attribute("Y"))?.Value ?? "0", 
                                       System.Globalization.CultureInfo.InvariantCulture);
                    var z = float.Parse((vertex.Attribute("z") ?? vertex.Attribute("Z"))?.Value ?? "0", 
                                       System.Globalization.CultureInfo.InvariantCulture);
                    
                    var v = new Vector3(x, y, z);
                    vertices.Add(v);
                    min = Vector3.Min(min, v);
                    max = Vector3.Max(max, v);
                }
                catch { /* Skip invalid vertices */ }
            }

            return vertices;
        }

        private void ParseTriangles(XElement mesh, XNamespace ns, List<Vector3> vertices, List<StlParser.Triangle> triangles)
        {
            var trianglesElement = mesh.Element(ns + "triangles") ?? 
                                  mesh.Elements().FirstOrDefault(e => e.Name.LocalName == "triangles");

            if (trianglesElement == null) return;

            var triangleElements = trianglesElement.Elements(ns + "triangle").ToList();
            if (triangleElements.Count == 0)
            {
                triangleElements = trianglesElement.Elements().Where(e => e.Name.LocalName == "triangle").ToList();
            }

            foreach (var triangle in triangleElements)
            {
                try
                {
                    var v1 = int.Parse((triangle.Attribute("v1") ?? triangle.Attribute("V1"))?.Value ?? "0");
                    var v2 = int.Parse((triangle.Attribute("v2") ?? triangle.Attribute("V2"))?.Value ?? "0");
                    var v3 = int.Parse((triangle.Attribute("v3") ?? triangle.Attribute("V3"))?.Value ?? "0");

                    if (v1 < vertices.Count && v2 < vertices.Count && v3 < vertices.Count)
                    {
                        var vert1 = vertices[v1];
                        var vert2 = vertices[v2];
                        var vert3 = vertices[v3];

                        var edge1 = vert2 - vert1;
                        var edge2 = vert3 - vert1;
                        var normal = Vector3.Cross(edge1, edge2);
                        
                        if (normal.LengthSquared() > 0)
                        {
                            normal = Vector3.Normalize(normal);
                        }

                        triangles.Add(new StlParser.Triangle
                        {
                            Normal = normal,
                            Vertex1 = vert1,
                            Vertex2 = vert2,
                            Vertex3 = vert3
                        });
                    }
                }
                catch { /* Skip invalid triangles */ }
            }
        }

        private XNamespace DetectNamespace(XDocument doc)
        {
            return doc.Root?.Name.Namespace ?? Standard3MFNamespace;
        }

        private XElement? FindResources(XDocument doc, XNamespace ns)
        {
            var resources = doc.Descendants(ns + "resources").FirstOrDefault();
            
            if (resources == null && doc.Root != null)
            {
                resources = doc.Descendants(doc.Root.Name.Namespace + "resources").FirstOrDefault();
            }
            
            if (resources == null)
            {
                resources = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "resources");
            }

            return resources;
        }

        private List<XElement> GetObjects(XElement resources, XNamespace ns)
        {
            var objects = resources.Elements(ns + "object").ToList();
            if (objects.Count == 0)
            {
                objects = resources.Elements().Where(e => e.Name.LocalName == "object").ToList();
            }
            return objects;
        }

        private XElement? FindMesh(XElement obj, XNamespace ns)
        {
            return obj.Element(ns + "mesh") ?? 
                   obj.Elements().FirstOrDefault(e => e.Name.LocalName == "mesh");
        }

        private bool HasGeometry(XElement mesh)
        {
            var hasVertices = mesh.Descendants().Any(e => e.Name.LocalName == "vertex");
            var hasTriangles = mesh.Descendants().Any(e => e.Name.LocalName == "triangle");
            return hasVertices && hasTriangles;
        }

        #endregion
    }
}
