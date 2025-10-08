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
        /// If the file contains multiple objects, they are combined into one model
        /// </summary>
        public async Task<StlParser.StlModel?> ParseFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Console.WriteLine($"ThreeMfParser: Opening 3MF file: {filePath}");
                    
                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine($"ThreeMfParser: ERROR - File does not exist: {filePath}");
                        return null;
                    }

                    ZipArchive? archive = null;
                    try
                    {
                        archive = ZipFile.OpenRead(filePath);
                        Console.WriteLine($"ThreeMfParser: Successfully opened ZIP archive with {archive.Entries.Count} entries");
                        
                        // List all entries for debugging
                        foreach (var entry in archive.Entries)
                        {
                            Console.WriteLine($"ThreeMfParser: ZIP entry: {entry.FullName} ({entry.Length} bytes)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ThreeMfParser: ERROR - Failed to open as ZIP: {ex.Message}");
                        return null;
                    }
                    
                    using (archive)
                    {
                        // Find ALL .model files - MakerWorld files can have models in subdirectories
                        var modelEntries = archive.Entries
                            .Where(e => e.FullName.EndsWith(".model", StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        
                        if (modelEntries.Count == 0)
                        {
                            Console.WriteLine("ThreeMfParser: ERROR - No .model files found in archive");
                            Console.WriteLine("ThreeMfParser: Available entries:");
                            foreach (var entry in archive.Entries)
                            {
                                Console.WriteLine($"  - {entry.FullName}");
                            }
                            return null;
                        }

                        Console.WriteLine($"ThreeMfParser: Found {modelEntries.Count} .model file(s)");

                        var allTriangles = new List<StlParser.Triangle>();
                        Vector3 globalMin = new Vector3(float.MaxValue);
                        Vector3 globalMax = new Vector3(float.MinValue);
                        int totalObjectsProcessed = 0;

                        // Process each .model file
                        foreach (var modelEntry in modelEntries)
                        {
                            Console.WriteLine($"ThreeMfParser: Processing model file: {modelEntry.FullName}");

                            XDocument? doc = null;
                            XNamespace? detectedNs = null;
                            
                            try
                            {
                                using var stream = modelEntry.Open();
                                doc = XDocument.Load(stream);
                                Console.WriteLine($"ThreeMfParser: Successfully loaded XML document");
                                
                                // Detect the actual namespace used in the document
                                if (doc.Root != null)
                                {
                                    detectedNs = doc.Root.Name.Namespace;
                                    Console.WriteLine($"ThreeMfParser: Detected namespace: {detectedNs}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"ThreeMfParser: ERROR - Failed to parse XML from {modelEntry.FullName}: {ex.Message}");
                                continue; // Try next model file
                            }

                            // Try standard 3MF namespace first, then fall back to detected namespace
                            XNamespace ns = "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";
                            
                            // Find resources element - try with and without namespace
                            var resources = doc.Descendants(ns + "resources").FirstOrDefault();
                            
                            if (resources == null && detectedNs != null)
                            {
                                Console.WriteLine($"ThreeMfParser: Trying with detected namespace: {detectedNs}");
                                resources = doc.Descendants(detectedNs + "resources").FirstOrDefault();
                                if (resources != null)
                                {
                                    ns = detectedNs;
                                }
                            }
                            
                            if (resources == null)
                            {
                                Console.WriteLine("ThreeMfParser: Trying without namespace...");
                                resources = doc.Descendants("resources").FirstOrDefault();
                                if (resources != null)
                                {
                                    ns = resources.Name.Namespace;
                                    Console.WriteLine($"ThreeMfParser: Found resources with namespace: {ns}");
                                }
                            }
                            
                            if (resources == null)
                            {
                                Console.WriteLine($"ThreeMfParser: WARNING - No <resources> element found in {modelEntry.FullName}");
                                Console.WriteLine($"ThreeMfParser: Root element: {doc.Root?.Name}");
                                Console.WriteLine($"ThreeMfParser: Root namespace: {doc.Root?.Name.Namespace}");
                                
                                // Try to find any object directly
                                var directObjects = doc.Descendants().Where(e => e.Name.LocalName == "object").ToList();
                                if (directObjects.Any())
                                {
                                    Console.WriteLine($"ThreeMfParser: Found {directObjects.Count} object(s) without resources wrapper");
                                    // Continue with these objects
                                    resources = doc.Root;
                                }
                                else
                                {
                                    continue; // Try next model file
                                }
                            }

                            var objects = resources.Elements(ns + "object").ToList();
                            
                            // If no objects found with namespace, try without
                            if (objects.Count == 0)
                            {
                                Console.WriteLine("ThreeMfParser: No objects found with namespace, trying without...");
                                objects = resources.Elements().Where(e => e.Name.LocalName == "object").ToList();
                            }
                            
                            int objectCount = objects.Count;
                            Console.WriteLine($"ThreeMfParser: Found {objectCount} object(s) in {modelEntry.FullName}");

                            if (objectCount == 0)
                            {
                                Console.WriteLine($"ThreeMfParser: WARNING - No <object> elements found in {modelEntry.FullName}");
                                Console.WriteLine($"ThreeMfParser: Resources children: {string.Join(", ", resources.Elements().Select(e => e.Name.LocalName))}");
                                continue; // Try next model file
                            }

                            // Process each object in this model file
                            foreach (var obj in objects)
                            {
                                var objectId = obj.Attribute("id")?.Value ?? "unknown";
                                var objectName = obj.Attribute("name")?.Value ?? $"Object {objectId}";
                                var objectType = obj.Attribute("type")?.Value;
                                
                                Console.WriteLine($"ThreeMfParser: Processing object: {objectName} (ID: {objectId}, Type: {objectType ?? "mesh"})");
                                
                                // Skip non-mesh objects (components, etc.)
                                if (objectType != null && objectType != "model" && objectType != "object")
                                {
                                    Console.WriteLine($"ThreeMfParser: Skipping non-mesh object type: {objectType}");
                                    continue;
                                }
                                
                                var mesh = obj.Element(ns + "mesh");
                                if (mesh == null)
                                {
                                    mesh = obj.Elements().FirstOrDefault(e => e.Name.LocalName == "mesh");
                                }
                                
                                if (mesh == null)
                                {
                                    Console.WriteLine($"ThreeMfParser: WARNING - Object {objectName} has no <mesh> element, skipping");
                                    continue;
                                }

                                // Parse vertices
                                var verticesElement = mesh.Element(ns + "vertices");
                                if (verticesElement == null)
                                {
                                    verticesElement = mesh.Elements().FirstOrDefault(e => e.Name.LocalName == "vertices");
                                }
                                
                                if (verticesElement == null)
                                {
                                    Console.WriteLine($"ThreeMfParser: WARNING - Object {objectName} has no <vertices> element, skipping");
                                    continue;
                                }

                                var vertices = new List<Vector3>();
                                var vertexElements = verticesElement.Elements(ns + "vertex").ToList();
                                if (vertexElements.Count == 0)
                                {
                                    vertexElements = verticesElement.Elements().Where(e => e.Name.LocalName == "vertex").ToList();
                                }
                                
                                Console.WriteLine($"ThreeMfParser: Found {vertexElements.Count} vertices in object {objectName}");
                                
                                // Pre-allocate list capacity for better performance with large models
                                vertices.Capacity = vertexElements.Count;
                                
                                foreach (var vertex in vertexElements)
                                {
                                    try
                                    {
                                        var xAttr = vertex.Attribute("x") ?? vertex.Attribute("X");
                                        var yAttr = vertex.Attribute("y") ?? vertex.Attribute("Y");
                                        var zAttr = vertex.Attribute("z") ?? vertex.Attribute("Z");
                                        
                                        float x = float.Parse(xAttr?.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture);
                                        float y = float.Parse(yAttr?.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture);
                                        float z = float.Parse(zAttr?.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture);
                                        
                                        var v = new Vector3(x, y, z);
                                        vertices.Add(v);

                                        // Update bounds
                                        globalMin = Vector3.Min(globalMin, v);
                                        globalMax = Vector3.Max(globalMax, v);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"ThreeMfParser: ERROR parsing vertex: {ex.Message}");
                                    }
                                }

                                // Parse triangles
                                var trianglesElement = mesh.Element(ns + "triangles");
                                if (trianglesElement == null)
                                {
                                    trianglesElement = mesh.Elements().FirstOrDefault(e => e.Name.LocalName == "triangles");
                                }
                                
                                if (trianglesElement == null)
                                {
                                    Console.WriteLine($"ThreeMfParser: WARNING - Object {objectName} has no <triangles> element, skipping");
                                    continue;
                                }

                                var triangleElements = trianglesElement.Elements(ns + "triangle").ToList();
                                if (triangleElements.Count == 0)
                                {
                                    triangleElements = trianglesElement.Elements().Where(e => e.Name.LocalName == "triangle").ToList();
                                }
                                
                                Console.WriteLine($"ThreeMfParser: Found {triangleElements.Count} triangles in object {objectName}");
                                
                                int validTriangles = 0;
                                int invalidTriangles = 0;
                                
                                foreach (var triangle in triangleElements)
                                {
                                    try
                                    {
                                        var v1Attr = triangle.Attribute("v1") ?? triangle.Attribute("V1");
                                        var v2Attr = triangle.Attribute("v2") ?? triangle.Attribute("V2");
                                        var v3Attr = triangle.Attribute("v3") ?? triangle.Attribute("V3");
                                        
                                        int v1 = int.Parse(v1Attr?.Value ?? "0");
                                        int v2 = int.Parse(v2Attr?.Value ?? "0");
                                        int v3 = int.Parse(v3Attr?.Value ?? "0");

                                        if (v1 < vertices.Count && v2 < vertices.Count && v3 < vertices.Count)
                                        {
                                            var vert1 = vertices[v1];
                                            var vert2 = vertices[v2];
                                            var vert3 = vertices[v3];

                                            // Calculate normal
                                            var edge1 = vert2 - vert1;
                                            var edge2 = vert3 - vert1;
                                            var normal = Vector3.Cross(edge1, edge2);
                            
                                            // Only normalize if not zero vector
                                            if (normal.LengthSquared() > 0)
                                            {
                                                normal = Vector3.Normalize(normal);
                                            }

                                            allTriangles.Add(new StlParser.Triangle
                                            {
                                                Normal = normal,
                                                Vertex1 = vert1,
                                                Vertex2 = vert2,
                                                Vertex3 = vert3
                                            });
                                            validTriangles++;
                                        }
                                        else
                                        {
                                            invalidTriangles++;
                                            if (invalidTriangles <= 10) // Only log first 10 errors
                                            {
                                                Console.WriteLine($"ThreeMfParser: WARNING - Triangle vertex index out of range: v1={v1}, v2={v2}, v3={v3}, vertices.Count={vertices.Count}");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"ThreeMfParser: ERROR parsing triangle: {ex.Message}");
                                    }
                                }
                                
                                if (invalidTriangles > 10)
                                {
                                    Console.WriteLine($"ThreeMfParser: WARNING - {invalidTriangles} total invalid triangles (showing first 10)");
                                }
                                
                                if (validTriangles > 0)
                                {
                                    totalObjectsProcessed++;
                                    Console.WriteLine($"ThreeMfParser: Successfully processed object {objectName} with {validTriangles} valid triangles");
                                }
                            }
                        }

                        if (allTriangles.Count == 0)
                        {
                            Console.WriteLine($"ThreeMfParser: ERROR - No triangles found after processing {totalObjectsProcessed} object(s) from {modelEntries.Count} model file(s)");
                            return null;
                        }

                        var model = new StlParser.StlModel
                        {
                            Triangles = allTriangles,
                            MinBounds = globalMin,
                            MaxBounds = globalMax,
                            Center = (globalMin + globalMax) / 2
                        };

                        // Calculate scale to fit in a unit cube
                        Vector3 size = globalMax - globalMin;
                        float maxDimension = Math.Max(Math.Max(size.X, size.Y), size.Z);
                        model.Scale = maxDimension > 0 ? 1.0f / maxDimension : 1.0f;

                        Console.WriteLine($"ThreeMfParser: SUCCESS - Parsed 3MF with {totalObjectsProcessed} object(s) from {modelEntries.Count} model file(s), {allTriangles.Count} total triangles");
                        Console.WriteLine($"ThreeMfParser: Bounds: Min={globalMin}, Max={globalMax}, Center={model.Center}, Scale={model.Scale}");
                        return model;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ThreeMfParser: FATAL ERROR parsing 3MF file: {ex.Message}");
                    Console.WriteLine($"ThreeMfParser: Stack trace: {ex.StackTrace}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Gets the number of separate objects in a 3MF file
        /// </summary>
        public async Task<int> GetObjectCountAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var archive = ZipFile.OpenRead(filePath);
                    
                    // Find ALL .model files (not just the main one)
                    var modelEntries = archive.Entries
                        .Where(e => e.FullName.EndsWith(".model", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (modelEntries.Count == 0)
                        return 0;

                    int totalMeshCount = 0;

                    // Check each .model file for objects with meshes
                    foreach (var modelEntry in modelEntries)
                    {
                        try
                        {
                            using var stream = modelEntry.Open();
                            var doc = XDocument.Load(stream);

                            // Try standard namespace first
                            XNamespace ns = "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";
                            var resources = doc.Descendants(ns + "resources").FirstOrDefault();
                            
                            // Try with detected namespace
                            if (resources == null && doc.Root != null)
                            {
                                ns = doc.Root.Name.Namespace;
                                resources = doc.Descendants(ns + "resources").FirstOrDefault();
                            }
                            
                            // Try without namespace
                            if (resources == null)
                            {
                                resources = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "resources");
                                if (resources != null)
                                {
                                    ns = resources.Name.Namespace;
                                }
                            }
                            
                            if (resources == null)
                                continue;

                            var objects = resources.Elements(ns + "object").ToList();
                            if (objects.Count == 0)
                            {
                                objects = resources.Elements().Where(e => e.Name.LocalName == "object").ToList();
                            }
                            
                            // Count only objects with actual meshes (not component references)
                            foreach (var obj in objects)
                            {
                                var mesh = obj.Element(ns + "mesh");
                                if (mesh == null)
                                {
                                    mesh = obj.Elements().FirstOrDefault(e => e.Name.LocalName == "mesh");
                                }
                                
                                if (mesh != null)
                                {
                                    // Verify it has actual geometry
                                    var hasVertices = mesh.Descendants().Any(e => e.Name.LocalName == "vertex");
                                    var hasTriangles = mesh.Descendants().Any(e => e.Name.LocalName == "triangle");
                                    
                                    if (hasVertices && hasTriangles)
                                    {
                                        totalMeshCount++;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"ThreeMfParser: Error processing {modelEntry.FullName}: {ex.Message}");
                            // Continue with next file
                        }
                    }
                    
                    return totalMeshCount;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ThreeMfParser: Error getting object count: {ex.Message}");
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
                    System.Diagnostics.Debug.WriteLine($"ParseMultipleObjectsAsync: Opening {filePath}");
                    using var archive = ZipFile.OpenRead(filePath);
                    
                    // Find ALL .model files (not just the main one) - MakerWorld support
                    var modelEntries = archive.Entries
                        .Where(e => e.FullName.EndsWith(".model", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (modelEntries.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("ParseMultipleObjectsAsync: No .model files found in 3MF archive");
                        return results;
                    }

                    System.Diagnostics.Debug.WriteLine($"ParseMultipleObjectsAsync: Found {modelEntries.Count} .model file(s)");

                    // Process each .model file
                    foreach (var modelEntry in modelEntries)
                    {
                        System.Diagnostics.Debug.WriteLine($"ParseMultipleObjectsAsync: Processing {modelEntry.FullName}");
                        
                        using var stream = modelEntry.Open();
                        var doc = XDocument.Load(stream);

                        // Try standard namespace first, then auto-detect
                        XNamespace ns = "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";
                        
                        var resources = doc.Descendants(ns + "resources").FirstOrDefault();
                        
                        // Try with detected namespace
                        if (resources == null && doc.Root != null)
                        {
                            ns = doc.Root.Name.Namespace;
                            resources = doc.Descendants(ns + "resources").FirstOrDefault();
                        }
                        
                        // Try without namespace
                        if (resources == null)
                        {
                            resources = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "resources");
                            if (resources != null)
                            {
                                ns = resources.Name.Namespace;
                            }
                        }
                        
                        if (resources == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"ParseMultipleObjectsAsync: No resources in {modelEntry.FullName}");
                            continue;
                        }

                        var objects = resources.Elements(ns + "object").ToList();
                        if (objects.Count == 0)
                        {
                            objects = resources.Elements().Where(e => e.Name.LocalName == "object").ToList();
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"ParseMultipleObjectsAsync: Found {objects.Count} object(s) in {modelEntry.FullName}");

                        int objectIndex = 1;
                        foreach (var obj in objects)
                        {
                            var objectId = obj.Attribute("id")?.Value ?? objectIndex.ToString();
                            var objectName = obj.Attribute("name")?.Value ?? $"Object {objectId}";
                            var objectType = obj.Attribute("type")?.Value;
                            
                            // Skip component references
                            if (objectType != null && objectType != "model" && objectType != "object")
                            {
                                System.Diagnostics.Debug.WriteLine($"ParseMultipleObjectsAsync: Skipping non-mesh type: {objectType}");
                                objectIndex++;
                                continue;
                            }
                            
                            var mesh = obj.Element(ns + "mesh");
                            if (mesh == null)
                            {
                                mesh = obj.Elements().FirstOrDefault(e => e.Name.LocalName == "mesh");
                            }
                            
                            if (mesh == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"ParseMultipleObjectsAsync: Object {objectName} has no mesh, skipping");
                                objectIndex++;
                                continue;
                            }

                            var model = new StlParser.StlModel();
                            var triangles = new List<StlParser.Triangle>();

                            Vector3 min = new Vector3(float.MaxValue);
                            Vector3 max = new Vector3(float.MinValue);

                            // Parse vertices
                            var verticesElement = mesh.Element(ns + "vertices");
                            if (verticesElement == null)
                            {
                                verticesElement = mesh.Elements().FirstOrDefault(e => e.Name.LocalName == "vertices");
                            }
                            
                            if (verticesElement == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"ParseMultipleObjectsAsync: Object {objectName} has no vertices, skipping");
                                objectIndex++;
                                continue;
                            }

                            var vertices = new List<Vector3>();
                            var vertexElements = verticesElement.Elements(ns + "vertex").ToList();
                            if (vertexElements.Count == 0)
                            {
                                vertexElements = verticesElement.Elements().Where(e => e.Name.LocalName == "vertex").ToList();
                            }
                            
                            foreach (var vertex in vertexElements)
                            {
                                var xAttr = vertex.Attribute("x") ?? vertex.Attribute("X");
                                var yAttr = vertex.Attribute("y") ?? vertex.Attribute("Y");
                                var zAttr = vertex.Attribute("z") ?? vertex.Attribute("Z");
                                
                                float x = float.Parse(xAttr?.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture);
                                float y = float.Parse(yAttr?.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture);
                                float z = float.Parse(zAttr?.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture);
                                
                                var v = new Vector3(x, y, z);
                                vertices.Add(v);

                                min = Vector3.Min(min, v);
                                max = Vector3.Max(max, v);
                            }

                            // Parse triangles
                            var trianglesElement = mesh.Element(ns + "triangles");
                            if (trianglesElement == null)
                            {
                                trianglesElement = mesh.Elements().FirstOrDefault(e => e.Name.LocalName == "triangles");
                            }
                            
                            if (trianglesElement == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"ParseMultipleObjectsAsync: Object {objectName} has no triangles, skipping");
                                objectIndex++;
                                continue;
                            }

                            var triangleElements = trianglesElement.Elements(ns + "triangle").ToList();
                            if (triangleElements.Count == 0)
                            {
                                triangleElements = trianglesElement.Elements().Where(e => e.Name.LocalName == "triangle").ToList();
                            }
                            
                            foreach (var triangle in triangleElements)
                            {
                                var v1Attr = triangle.Attribute("v1") ?? triangle.Attribute("V1");
                                var v2Attr = triangle.Attribute("v2") ?? triangle.Attribute("V2");
                                var v3Attr = triangle.Attribute("v3") ?? triangle.Attribute("V3");
                                
                                int v1 = int.Parse(v1Attr?.Value ?? "0");
                                int v2 = int.Parse(v2Attr?.Value ?? "0");
                                int v3 = int.Parse(v3Attr?.Value ?? "0");

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

                            if (triangles.Count > 0)
                            {
                                model.Triangles = triangles;
                                model.MinBounds = min;
                                model.MaxBounds = max;
                                model.Center = (min + max) / 2;

                                Vector3 size = max - min;
                                float maxDimension = Math.Max(Math.Max(size.X, size.Y), size.Z);
                                model.Scale = maxDimension > 0 ? 1.0f / maxDimension : 1.0f;

                                results.Add((objectName, model));
                                System.Diagnostics.Debug.WriteLine($"ParseMultipleObjectsAsync: Parsed '{objectName}' with {triangles.Count} triangles");
                            }

                            objectIndex++;
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"ParseMultipleObjectsAsync: Successfully parsed {results.Count} object(s) from {modelEntries.Count} file(s)");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ParseMultipleObjectsAsync: Error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"ParseMultipleObjectsAsync: Stack trace: {ex.StackTrace}");
                }

                return results;
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
                {
                    Console.WriteLine($"ThreeMfParser.IsValid3MfFile: File does not exist: {filePath}");
                    return false;
                }

                using var archive = ZipFile.OpenRead(filePath);
                Console.WriteLine($"ThreeMfParser.IsValid3MfFile: Opened ZIP with {archive.Entries.Count} entries");
                
                // Check for required files
                bool hasModelFile = archive.Entries.Any(e => 
                    e.FullName.EndsWith(".model", StringComparison.OrdinalIgnoreCase));
                
                if (!hasModelFile)
                {
                    Console.WriteLine("ThreeMfParser.IsValid3MfFile: No .model file found");
                    Console.WriteLine("ThreeMfParser.IsValid3MfFile: Available files:");
                    foreach (var entry in archive.Entries.Take(20)) // Show first 20
                    {
                        Console.WriteLine($"  - {entry.FullName}");
                    }
                }
                
                bool hasRelsFile = archive.Entries.Any(e => 
                    e.FullName.Contains("_rels", StringComparison.OrdinalIgnoreCase));

                Console.WriteLine($"ThreeMfParser.IsValid3MfFile: Has .model={hasModelFile}, Has _rels={hasRelsFile}");
                
                return hasModelFile; // .rels is optional but .model is required
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ThreeMfParser.IsValid3MfFile: Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Diagnose why a 3MF file might be failing to import
        /// </summary>
        public async Task<string> DiagnoseFileAsync(string filePath)
        {
            var diagnosis = new System.Text.StringBuilder();
            
            try
            {
                diagnosis.AppendLine("=== 3MF FILE DIAGNOSIS ===");
                diagnosis.AppendLine($"File: {Path.GetFileName(filePath)}");
                diagnosis.AppendLine($"Full Path: {filePath}");
                
                if (!File.Exists(filePath))
                {
                    diagnosis.AppendLine("? ERROR: File does not exist!");
                    return diagnosis.ToString();
                }
                
                var fileInfo = new FileInfo(filePath);
                diagnosis.AppendLine($"? File Size: {fileInfo.Length:N0} bytes");
                
                try
                {
                    using var archive = ZipFile.OpenRead(filePath);
                    diagnosis.AppendLine($"? Valid ZIP archive: {archive.Entries.Count} entries");
                    
                    diagnosis.AppendLine("\nZIP Contents:");
                    foreach (var entry in archive.Entries)
                    {
                        diagnosis.AppendLine($"  - {entry.FullName} ({entry.Length} bytes)");
                    }
                    
                    // Find ALL .model files
                    var modelEntries = archive.Entries
                        .Where(e => e.FullName.EndsWith(".model", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    if (modelEntries.Count == 0)
                    {
                        diagnosis.AppendLine("\n? ERROR: No .model files found!");
                        diagnosis.AppendLine("Expected: Files ending with .model");
                        return diagnosis.ToString();
                    }
                    
                    diagnosis.AppendLine($"\n? Found {modelEntries.Count} .model file(s)");
                    
                    int totalMeshCount = 0;
                    int totalObjectCount = 0;
                    
                    foreach (var modelEntry in modelEntries)
                    {
                        diagnosis.AppendLine($"\n--- Processing: {modelEntry.FullName} ---");
                        
                        using var stream = modelEntry.Open();
                        var doc = XDocument.Load(stream);
                        
                        diagnosis.AppendLine($"? Valid XML document");
                        diagnosis.AppendLine($"Root element: {doc.Root?.Name.LocalName}");
                        diagnosis.AppendLine($"Namespace: {doc.Root?.Name.Namespace}");
                        
                        // Try to find namespace
                        XNamespace ns = doc.Root?.Name.Namespace ?? "";
                        
                        // Look for resources
                        var resources = doc.Descendants(ns + "resources").FirstOrDefault();
                        if (resources == null)
                        {
                            resources = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "resources");
                            if (resources != null)
                            {
                                ns = resources.Name.Namespace;
                            }
                        }
                        
                        if (resources == null)
                        {
                            diagnosis.AppendLine("?? WARNING: No <resources> element found");
                            diagnosis.AppendLine($"Root children: {string.Join(", ", doc.Root?.Elements().Select(e => e.Name.LocalName) ?? Enumerable.Empty<string>())}");
                            continue;
                        }
                        
                        diagnosis.AppendLine($"? Found <resources> element");
                        
                        // Find all objects
                        var objects = resources.Elements(ns + "object").ToList();
                        if (objects.Count == 0)
                        {
                            objects = resources.Elements().Where(e => e.Name.LocalName == "object").ToList();
                        }
                        
                        diagnosis.AppendLine($"? Found {objects.Count} object(s)");
                        totalObjectCount += objects.Count;
                        
                        foreach (var obj in objects)
                        {
                            var id = obj.Attribute("id")?.Value ?? "?";
                            var name = obj.Attribute("name")?.Value ?? "unnamed";
                            var type = obj.Attribute("type")?.Value ?? "model";
                            
                            diagnosis.AppendLine($"\n  Object {id}: '{name}' (type: {type})");
                            
                            // Look for mesh
                            var mesh = obj.Element(ns + "mesh");
                            if (mesh == null)
                            {
                                mesh = obj.Elements().FirstOrDefault(e => e.Name.LocalName == "mesh");
                            }
                            
                            if (mesh != null)
                            {
                                // Count vertices and triangles
                                var vertices = mesh.Descendants().Where(e => e.Name.LocalName == "vertex").Count();
                                var triangles = mesh.Descendants().Where(e => e.Name.LocalName == "triangle").Count();
                                
                                diagnosis.AppendLine($"    ? Has mesh");
                                diagnosis.AppendLine($"    - Vertices: {vertices}");
                                diagnosis.AppendLine($"    - Triangles: {triangles}");
                                
                                if (vertices > 0 && triangles > 0)
                                {
                                    totalMeshCount++;
                                }
                            }
                            else
                            {
                                diagnosis.AppendLine($"    ?? NO MESH element found");
                                
                                // Check what children it has
                                var children = obj.Elements().Select(e => e.Name.LocalName).ToList();
                                if (children.Any())
                                {
                                    diagnosis.AppendLine($"    Children: {string.Join(", ", children)}");
                                    
                                    // Check for components reference
                                    var components = obj.Element(ns + "components") ?? 
                                                   obj.Elements().FirstOrDefault(e => e.Name.LocalName == "components");
                                    if (components != null)
                                    {
                                        var componentRefs = components.Elements().Where(e => e.Name.LocalName == "component").Count();
                                        diagnosis.AppendLine($"    Has {componentRefs} component reference(s)");
                                        diagnosis.AppendLine($"    ?? This is a component reference, not actual geometry");
                                    }
                                }
                                else
                                {
                                    diagnosis.AppendLine($"    (empty object)");
                                }
                            }
                        }
                    }
                    
                    diagnosis.AppendLine($"\n=== SUMMARY ===");
                    diagnosis.AppendLine($"Total .model files: {modelEntries.Count}");
                    diagnosis.AppendLine($"Total objects found: {totalObjectCount}");
                    diagnosis.AppendLine($"Objects with meshes: {totalMeshCount}");
                    
                    if (totalMeshCount == 0)
                    {
                        diagnosis.AppendLine("\n? ERROR: No objects with mesh geometry found!");
                        diagnosis.AppendLine("?? File may contain only component references or be structured differently.");
                        diagnosis.AppendLine("?? Some 3MF files use a 'build' section with component instances.");
                    }
                    else
                    {
                        diagnosis.AppendLine($"\n? File should import successfully ({totalMeshCount} mesh object(s))");
                    }
                }
                catch (InvalidDataException)
                {
                    diagnosis.AppendLine("? ERROR: File is not a valid ZIP archive!");
                    diagnosis.AppendLine("File may be corrupted or not a real 3MF file.");
                }
            }
            catch (Exception ex)
            {
                diagnosis.AppendLine($"\n? EXCEPTION: {ex.Message}");
                diagnosis.AppendLine($"Stack trace: {ex.StackTrace}");
            }
            
            return diagnosis.ToString();
        }
    }
}
