# 3D Model DB (.NET MAUI) – Rebuild / Recreation Guide

This document describes how to recreate the 3D Model DB application from scratch. It assumes familiarity with .NET, C#, and .NET MAUI.

---
## 1. Prerequisites
| Requirement | Version / Notes |
|-------------|-----------------|
| .NET SDK | 9.x |
| Visual Studio 2022 | With ".NET Multi-platform App UI" workload |
| Android SDK / Emulator | For Android builds |
| Windows 10 19041+ | For WinUI packaging & deployment |
| (macOS) Xcode 15+ | For iOS / Mac Catalyst |
| SQLite | Provided via `sqlite-net-pcl` |

Install MAUI workload if missing:
```bash
dotnet workload install maui
```

---
## 2. Create Base Project
```bash
dotnet new maui -n MauiApp3
cd MauiApp3
```
Remove default pages and add the folder structure below.

---
## 3. Project Structure
```
MauiApp3/
??? App.xaml / App.xaml.cs
??? AppShell.xaml / AppShell.xaml.cs
??? MauiProgram.cs
??? Properties/
?   ??? launchSettings.json
??? Behaviors/
??? Controls/
?   ??? Model3DViewer.cs
??? Converters/
?   ??? ValueConverters.cs
??? Models/
?   ??? Model3DFile.cs
?   ??? AttachedImage.cs
?   ??? AttachedGCode.cs
?   ??? Project.cs
?   ??? Model3DFileDb.cs
?   ??? ProjectDb.cs
??? Pages/
?   ??? MainPage.xaml / .cs
?   ??? ModelDetailPage.xaml / .cs
??? Services/
?   ??? DatabaseService.cs
?   ??? Model3DService.cs
?   ??? StlParser.cs
?   ??? ThreeMfParser.cs
?   ??? ThumbnailGenerator.cs (optional folded into Model3DService)
??? Utilities/
?   ??? SampleModelGenerator.cs (optional)
??? ViewModels/
?   ??? MainViewModel.cs
?   ??? ModelDetailViewModel.cs
?   ??? ModelGroup.cs
??? Docs/
    ??? RebuildInstructions.md (this file)
```

---
## 4. NuGet Packages
Add / confirm:
- `SkiaSharp.Views.Maui.Controls`
- `sqlite-net-pcl`
- (optional) `CommunityToolkit.Mvvm`

```bash
dotnet add package SkiaSharp.Views.Maui.Controls
dotnet add package sqlite-net-pcl
```

---
## 5. Windows launchSettings
`Properties/launchSettings.json`:
```json
{
  "profiles": {
    "Windows Machine": {
      "commandName": "MsixPackage",
      "nativeDebugging": false
    }
  }
}
```

---
## 6. Core Data Models
### Model3DFile
- Id (GUID string)
- Name, FilePath, FileType
- UploadedDate, FileSize
- ThumbnailData (byte[])
- ParsedModel (runtime only)
- Tags: `ObservableCollection<string>`
- AttachedImages: `ObservableCollection<AttachedImage>`
- AttachedGCodeFiles: `ObservableCollection<AttachedGCode>`
- ProjectId (nullable), ProjectName (cached)

### Project
- Id, Name, Description
- CreatedDate, ModifiedDate, Color (hex)
- ModelIds: `ObservableCollection<string>`

### Persistence DTOs
`Model3DFileDb` (flatten tags & attachments) and `ProjectDb` (comma-separated model ids).

### Attachments
`AttachedImage`: FileName, ImageData, AttachedDate
`AttachedGCode`: FileName, FilePath, FileSize, AttachedDate

---
## 7. Services
### DatabaseService
- Initializes SQLite (async) in `FileSystem.AppDataDirectory`
- CRUD for models / projects
- Assign / remove project relationships
- Serialize attachments & tags (JSON + comma join)

### Model3DService
- `IsSupportedFormat()` (STL / 3MF)
- `LoadModelAsync()` dispatch parse
- `GenerateThumbnailAsync()` (SkiaSharp off-screen rendering)

### StlParser
- Detect ASCII vs Binary
- Parse triangles ? positions, normals
- Compute bounds ? center, uniform scale factor

### ThreeMfParser
- Count objects (`GetObjectCountAsync`)
- Multi-object extraction (`ParseMultipleObjectsAsync`) ? list of (objectName, model)
- XML inside `/3D/` path of ZIP container

---
## 8. Grouping Support
`ModelGroup : List<Model3DFile>` with properties:
- GroupName
- GroupColor
- ProjectId

Built in `MainViewModel.UpdateGroupedModels()`:
- Group by `ProjectId ?? ""`
- Projects first, then ungrouped
- Map color from Project; ungrouped ? Gray `#666666`

---
## 9. ViewModels
### MainViewModel
State:
- `ObservableCollection<Model3DFile> Models`
- `ObservableCollection<Project> Projects`
- `ObservableCollection<string> AllTags`
- `ObservableCollection<ModelGroup> GroupedModels`
- SelectedModel / SelectedProject
- SelectedFileFilter (null = all)
- IsLoading, IsDrawerOpen, NewTagText

Commands:
- Toggle drawer, Upload model, Select model, Open detail
- Add / Remove tag
- Create / Delete project
- Assign / Remove model from project
- Reset database
- Apply file-type filter

Key Methods:
- LoadDataFromDatabaseAsync()
- RefreshDataAsync()
- FilterModelsByProjectAsync()
- ApplyFileFilterToCurrentCollectionAsync()
- UpdateGroupedModels()
- Import STL / 3MF (with multi-object handling)

### ModelDetailViewModel
- Model, ParentProject
- Tag management
- Attach/remove image (byte[] persisted)
- Attach/remove/open G-code
- Optional message to reset 3D viewer

---
## 10. UI Pages
### AppShell.xaml
Register route for `ModelDetailPage`.

### MainPage.xaml Layout
- 3-column Grid: Drawer | Grouped Model Gallery | Preview / Info Panel
- Drawer: stats, upload, project list, file filters, reset button
- Center: `CollectionView` with `IsGrouped=True`, header = project name + count
- Right: Selected model preview (static thumbnail), actions, tags panel

### Model Cards
- Thumbnail (or emoji fallback)
- Name, FileType badge, Size, Tags, Upload date
- Double-tap ? detail page

### Detail Page (optional at this stage)
- Larger preview / metadata
- Tag + attachment management

---
## 11. Converters
- `FileSizeConverter`
- `IsNullConverter`, `IsNotNullConverter`
- `ByteArrayToImageSourceConverter`

---
## 12. Behaviors (Optional)
`EventToCommandBehavior` to bridge tap ? command when not using selection.

---
## 13. Custom Control – Model3DViewer (SkiaSharp)
- Renders triangles with simple diffuse shading
- Supports rotate (drag), zoom (wheel), reset
- Used optionally (current implementation may rely on thumbnails only)

---
## 14. 3MF Multi?Object Import Flow
1. Detect extension == 3MF
2. Count objects
3. If >1 prompt user (separate vs combined)
4. If separate: create project, import each object as individual `Model3DFile` with `ProjectId`
5. Update DB + UI ? select new project

---
## 15. Thumbnail Generation
- Off-screen SKSurface
- Transform + shade triangles
- Encode PNG ? memory stream ? byte[] stored in DB

---
## 16. Persistence Strategy
- Tags ? comma join
- Attachments ? JSON serialize collections
- Projects hold list of model IDs (string join)
- On load: reconstruct observable collections

---
## 17. Group Refresh Triggers
Call `UpdateGroupedModels()` after:
- Initial load
- Add/delete model
- Assign/remove project
- Filter (project or file type)
- RefreshDataAsync
- Reset database

---
## 18. File Filters
`SelectedFileFilter`: null | `"STL"` | `"3MF"`
Applied after base model retrieval; then regroup.

---
## 19. Error Handling
- Wrap all async IO & parsing operations in try/catch
- Log via `Debug.WriteLine`
- Show user alerts via `DisplayAlert`
- Fallback if thumbnail fails

---
## 20. Optional Global Exception Hooks
In `MauiProgram.CreateMauiApp()`:
```csharp
AppDomain.CurrentDomain.UnhandledException += (s,e)=> Debug.WriteLine(e.ExceptionObject);
TaskScheduler.UnobservedTaskException += (s,e)=> Debug.WriteLine(e.Exception);
```

---
## 21. Performance Notes
- Lazy parse model when first selected if needed
- Reuse SKPaint & buffers in viewer
- Avoid rebuilding groups unless underlying collections changed

---
## 22. Testing Checklist
| Action | Expected Result |
|--------|-----------------|
| Import STL | Appears with thumbnail & metadata |
| Import single 3MF | Same as STL |
| Import multi-object 3MF | Project created + grouped models |
| Add/remove tag | Updates card + tag list |
| Create/delete project | Groups reflect change; models remain |
| Assign/remove model to project | Moves between groups |
| Apply file filter | Only matching types shown |
| Reset DB | All collections cleared |
| Open detail view | Displays extended metadata |

---
## 23. Build Commands
```bash
dotnet build
dotnet build -f net9.0-windows10.0.19041.0
dotnet build -f net9.0-android
```

---
## 24. Future Enhancements
- Live 3D interactive viewer in preview panel
- Search (name + tags)
- Export/Share model
- Sorting (date, name, size)
- Cloud sync or remote storage

---
## 25. Minimal Implementation Order
1. Create project + structure
2. Add models & DTOs
3. Implement `DatabaseService`
4. Implement parsers (STL ? 3MF)
5. Implement `Model3DService` + thumbnails
6. Converters
7. `MainViewModel` (basic list)
8. UI (MainPage) ungrouped
9. Add grouping logic
10. Add project & tag management
11. Add filters
12. Add detail page & attachments
13. Polish + error handling

---
## 26. Key Design Decisions
- Keep rendering simple (thumbnail-first approach)
- Persist only metadata & thumbnail (not geometry) for speed
- Use grouping in memory (no DB-level grouping table required)
- Decouple parsing from display (lazy load on selection if desired)

---
## 27. Troubleshooting
| Symptom | Likely Cause | Fix |
|---------|--------------|-----|
| Empty UI | DB path mismatch / not loaded | Check `FileSystem.AppDataDirectory` |
| Crash on multi-object import | 3MF parser edge case | Validate object count & null checks |
| Thumbnails missing | Rendering exception | Wrap in try/catch; allow fallback emoji |
| Group header count incorrect | Missing `UpdateGroupedModels()` call | Ensure call after every mutation |
| File filter clears everything | Case mismatch | Normalize `FileType` to upper before compare |

---
## 28. Security / Validation Notes
- Restrict accepted extensions (.stl / .3mf / optional images / gcode)
- Do not execute arbitrary content
- Consider size limits for attachments (future)

---
## 29. Cleanup / Reset DB
`ResetDatabaseAsync()`:
- Confirm twice
- Clear observable collections
- Delete DB file & re-init

---
## 30. Summary
This guide defines every layer needed to reconstruct the 3D Model DB MAUI app: data models, persistence, parsing, grouping, UI composition, and extensibility points. Follow the minimal order for fastest functional rebuild, then layer in enhancements.

---
**End of Document**
