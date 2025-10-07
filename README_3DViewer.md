# 3D Model Viewer Application

## Overview
This .NET MAUI application provides a three-panel interface for managing and viewing 3D models (STL and 3MF files) with **integrated real-time 3D rendering**.

## Features

### 1. **Left Drawer Panel (Collapsible)**
- Toggle button to collapse/expand the drawer
- Library statistics showing total model count
- Quick action button to upload new models
- File filters for STL and 3MF files
- 250px width when open, 0px when collapsed

### 2. **Center Grid View**
- Displays all uploaded 3D models in a responsive 2-column grid
- Each model card shows:
  - Thumbnail placeholder (ready for actual thumbnails)
  - File name
  - File type badge (STL/3MF)
  - File size
  - Upload date
- Visual highlight for selected model
- Empty state with upload prompt when no models exist
- **Takes 60% of screen width** (larger for better browsing)

### 3. **Right Renderer Panel - LIVE 3D VIEWER** ?
- **Real-time 3D model rendering** using SkiaSharp
- Interactive controls:
  - **Rotate**: Click and drag to rotate the model
  - **Zoom**: Mouse wheel to zoom in/out
  - **Reset View**: Button to return to default view
- Visual features:
  - Directional lighting for depth perception
  - Backface culling for performance
  - Wireframe overlay for detail
  - XYZ axis indicators
- Model information display:
  - File name
  - File type
  - File size
  - Triangle count
- Action buttons:
  - Export
  - Delete
- **Takes 40% of screen width** (optimized for viewing)

## Architecture

### Models
- **Model3DFile**: Represents a 3D model file with metadata
  - Id, Name, FilePath, FileType, UploadedDate, FileSize, ThumbnailData, ParsedModel

### ViewModels
- **MainViewModel**: Main application view model
  - Manages model collection
  - Handles drawer toggle
  - Processes file uploads
  - Tracks selected model
  - Implements INotifyPropertyChanged for data binding
  - Loads and parses STL files

### Services
- **Model3DService**: Handles 3D model operations
  - File format validation
  - STL parsing
  - Model information extraction
- **StlParser**: Parses STL files (both ASCII and Binary formats)
  - Extracts triangles, vertices, and normals
  - Calculates bounding boxes
  - Auto-scales models for rendering

### Controls
- **Model3DViewer**: Custom SkiaSharp-based 3D viewer
  - Real-time rendering
  - Touch/mouse interaction
  - Lighting and shading
  - Axis indicators

### Converters
- **FileSizeConverter**: Converts bytes to human-readable format (B, KB, MB, GB)
- **BoolToWidthConverter**: Converts boolean to drawer width
- **IsNullConverter**: Checks if value is null
- **IsNotNullConverter**: Checks if value is not null

## Usage

### Upload Models
1. Click the "?? Upload Model" button in the left drawer
2. Select an STL or 3MF file from your system
3. The file will be parsed and loaded
4. The model will appear in the center grid
5. A notification will show the number of triangles loaded

### View Models in 3D
1. Click on any model card in the center grid
2. The 3D model will render in the right panel
3. **Interact with the model:**
   - Click and drag to rotate
   - Use mouse wheel to zoom
   - Click the ?? button to reset the view

### Toggle Drawer
- Click the "?" hamburger button at the top of the center panel
- The drawer will smoothly collapse or expand

### Delete Models
1. Select a model from the grid
2. Click the "Delete" button in the right panel's action section

## Technical Implementation

### 3D Rendering
- **Engine**: SkiaSharp (cross-platform 2D graphics)
- **Rendering Algorithm**: 
  - Painter's algorithm (depth sorting)
  - Backface culling
  - Simple directional lighting
  - Wireframe overlay
- **Transformation**: Matrix-based 3D transformations
- **Performance**: Optimized for models with up to ~100K triangles

### STL File Support
- **Binary STL**: Full support
- **ASCII STL**: Full support
- **Features**:
  - Automatic format detection
  - Vertex extraction
  - Normal calculation
  - Bounding box computation
  - Auto-centering and scaling

### 3MF File Support
- **Status**: Parser stub created (TODO: implement ZIP/XML parsing)
- **Format**: 3MF uses ZIP compression with XML content

## Dependencies

```xml
<PackageReference Include="SkiaSharp.Views.Maui.Controls" Version="3.119.1" />
<PackageReference Include="Microsoft.Maui.Controls" Version="$(MauiVersion)" />
<PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="9.0.5" />
```

## File Structure
```
MauiApp3/
??? Models/
?   ??? Model3DFile.cs
??? ViewModels/
?   ??? MainViewModel.cs
??? Services/
?   ??? Model3DService.cs
?   ??? StlParser.cs
??? Controls/
?   ??? Model3DViewer.cs
??? Converters/
?   ??? ValueConverters.cs
??? Utilities/
?   ??? SampleModelGenerator.cs
??? MainPage.xaml
??? MainPage.xaml.cs
??? App.xaml
??? App.xaml.cs
??? AppShell.xaml
??? AppShell.xaml.cs
??? MauiProgram.cs
??? GlobalXmlns.cs
```

## Customization

### Colors
The application uses a dark theme with the following colors:
- Background: #1E1E1E, #2C2C2C, #1A1A1A
- Accent: #0078D4 (Microsoft Blue)
- Text: White, #CCCCCC, #AAAAAA
- Borders: #444444

To customize colors, modify the color values in MainPage.xaml.

### Drawer Width
To change the drawer width, modify the `DrawerWidth` property in `MainViewModel.cs`:
```csharp
DrawerWidth = value ? 250 : 0; // Change 250 to your desired width
```

### Panel Proportions
To change the center/right panel ratio, modify MainPage.xaml:
```xaml
<ColumnDefinition Width="3*"/>  <!-- Center: 60% -->
<ColumnDefinition Width="2*"/>  <!-- Right: 40% -->
```

### Grid Columns
To change the number of columns in the model grid, modify the `Span` property in MainPage.xaml:
```xaml
<GridItemsLayout Orientation="Vertical" Span="2"/> <!-- Change 2 to desired column count -->
```

### 3D Viewer Settings
Customize rendering in `Controls/Model3DViewer.cs`:
```csharp
private float _rotationX = 30;  // Initial X rotation
private float _rotationY = 45;  // Initial Y rotation
private float _zoom = 1.0f;     // Initial zoom level
```

## Performance Tips

1. **Large Models**: Models with >100K triangles may be slow. Consider implementing:
   - Level-of-detail (LOD) rendering
   - Frustum culling
   - Triangle decimation

2. **Memory**: Large STL files are loaded entirely into memory. For huge files:
   - Implement streaming parser
   - Use progressive loading

3. **Rendering**: The viewer uses CPU-based rendering. For better performance:
   - Consider OpenGL/DirectX backends
   - Implement GPU acceleration

## Future Enhancements

### Planned Features
- [ ] 3MF file parsing
- [ ] Thumbnail generation
- [ ] Model export to different formats
- [ ] Measurement tools
- [ ] Section views
- [ ] Multiple model comparison
- [ ] Animation and rotation presets
- [ ] Material/color customization
- [ ] Print time estimation
- [ ] Model repair tools

### Advanced Rendering
- [ ] Phong/Blinn-Phong shading
- [ ] Shadows
- [ ] Ambient occlusion
- [ ] Grid floor
- [ ] Model slicing preview
- [ ] Multiple light sources

## Testing

### Generate Test Models
Use the `SampleModelGenerator` utility:
```csharp
await SampleModelGenerator.GenerateCubeStlAsync("test_cube.stl", 2.0f);
await SampleModelGenerator.GeneratePyramidStlAsync("test_pyramid.stl", 2.0f);
```

### Sample Data
The application includes sample placeholder data:
- sample_cube.stl (virtual)
- sphere_model.3mf (virtual)
- complex_part.stl (virtual)

Upload real STL files to see actual 3D rendering!

## Requirements
- .NET 9.0
- .NET MAUI workload
- SkiaSharp 3.119.1+
- Target platforms: Windows, iOS, Android, macOS

## Known Limitations
1. **3MF**: Only STL is currently supported for rendering
2. **Performance**: Very large models (>500K triangles) may cause lag
3. **Colors**: Models are rendered in grayscale with lighting
4. **Touch**: Multi-touch gestures not fully implemented

## Credits
- **SkiaSharp**: Cross-platform 2D graphics by Mono Project
- **STL Format**: 3D Systems stereolithography format
- **.NET MAUI**: Microsoft's cross-platform app framework
