# Quick Reference Guide for AI Assistants

## ?? Quick Start Understanding

### What is this application?
A desktop 3D model management app (like iTunes for 3D printing files)

### Tech Stack
- .NET MAUI 9 (cross-platform)
- C# 12
- SQLite (local database)
- MVVM architecture
- SkiaSharp (rendering)

### Main Files to Know
- `MainPage.xaml` / `MainViewModel.cs` - Main app screen
- `ModelDetailPage.xaml` / `ModelDetailViewModel.cs` - Detail view
- `DatabaseService.cs` - All database operations
- `Model3DService.cs` - 3D file operations
- `Model3DFile.cs` - Core data model

## ??? Common Tasks & Where to Look

### Adding New Features

| Task | Files to Modify |
|------|----------------|
| New UI element | `MainPage.xaml` or `ModelDetailPage.xaml` |
| New command/logic | `MainViewModel.cs` or `ModelDetailViewModel.cs` |
| New data field | `Models/Model3DFile.cs` + `Models/Model3DFileDb.cs` + `DatabaseService.cs` |
| New service | `Services/` folder + register in `MauiProgram.cs` |
| New page | `Pages/` folder + register in `MauiProgram.cs` + add route in `AppShell.xaml` |

### Common Code Patterns

**Property with Change Notification:**
```csharp
private string _fieldName;
public string PropertyName
{
    get => _fieldName;
    set => SetProperty(ref _fieldName, value);
}
```

**Async Command:**
```csharp
public ICommand MyCommand { get; }

// In constructor:
MyCommand = new Command(async () => await MyMethodAsync());

// Method:
private async Task MyMethodAsync()
{
    try
    {
        // Do work
    }
    catch (Exception ex)
    {
        await ShowAlertAsync("Error", $"Failed: {ex.Message}");
    }
}
```

**Database Operation:**
```csharp
private async Task SaveDataAsync()
{
    try
    {
        await _databaseService.SaveModelAsync(model);
    }
    catch (Exception ex)
    {
        await ShowAlertAsync("Error", $"Failed to save: {ex.Message}");
    }
}
```

**XAML Binding:**
```xaml
<!-- One-way binding -->
<Label Text="{Binding PropertyName}" />

<!-- Two-way binding -->
<Entry Text="{Binding PropertyName, Mode=TwoWay}" />

<!-- Command binding -->
<Button Command="{Binding CommandName}" />

<!-- Conditional visibility -->
<Frame IsVisible="{Binding HasProject}" />

<!-- Ancestor binding -->
<Button Command="{Binding Source={RelativeSource AncestorType={x:Type vm:MainViewModel}}, Path=CommandName}" />
```

## ?? Common Issues & Fixes

### Issue: Property not updating UI
**Fix:** Ensure `INotifyPropertyChanged` is implemented and `OnPropertyChanged()` is called

### Issue: Command not executing
**Fix:** Check `CanExecute` logic and call `ChangeCanExecute()` when conditions change

### Issue: Project name not showing
**Fix:** Ensure `ProjectName` is updated when `ProjectId` changes (see `UpdateAllProjectNames()`)

### Issue: Field assignment bypassing logic
**Fix:** Use property setter (`Model = value`) not field (`_model = value`)

### Issue: UI not updating after async operation
**Fix:** Use `TaskScheduler.FromCurrentSynchronizationContext()` or `MainThread.BeginInvokeOnMainThread()`

## ?? Important Design Decisions

### Why PropertyName is separate from ProjectId?
- **Performance:** Avoid database lookups on every UI render
- **Caching:** Pre-computed value for fast binding
- **Consistency:** Updated via `UpdateAllProjectNames()` after data loads

### Why use TaskCompletionSource in dialogs?
- **Async/Await:** Allows awaiting dialog result
- **Type Safety:** Returns strongly-typed result
- **Clean Code:** No callbacks or events needed

### Why SkiaSharp instead of 3D API?
- **Cross-platform:** Works on all MAUI platforms
- **Control:** Custom rendering pipeline
- **Performance:** Lightweight for static thumbnails
- **Compatibility:** No platform-specific 3D APIs needed

### Why SQLite over other databases?
- **Local-first:** No server required
- **Cross-platform:** Works everywhere
- **Lightweight:** Small footprint
- **Mature:** Well-tested .NET support
- **Embedded:** Included in app package

### Why MVVM pattern?
- **Testability:** ViewModels testable without UI
- **Separation:** UI and logic decoupled
- **Data Binding:** Automatic UI updates
- **Maintainability:** Clear responsibilities
- **MAUI Best Practice:** Framework designed for MVVM

## ?? UI Color Reference

```csharp
// Backgrounds
"#1E1E1E"  // Primary (darkest)
"#2C2C2C"  // Secondary
"#3C3C3C"  // Tertiary
"#4C4C4C"  // Hover

// Accents
"#0078D4"  // Blue (primary action)
"#107C10"  // Green (projects, success)
"#CA5010"  // Orange (warning)
"#C42B1C"  // Red (delete, danger)

// Text
"#FFFFFF"  // White (primary)
"#AAAAAA"  // Light gray (secondary)
"#888888"  // Medium gray (tertiary)
"#666666"  // Dark gray (disabled)
"#444444"  // Border
```

## ?? Key NuGet Packages

```xml
<PackageReference Include="Microsoft.Maui.Controls" Version="9.0.x" />
<PackageReference Include="SkiaSharp.Views.Maui.Controls" Version="3.119.1" />
<PackageReference Include="sqlite-net-pcl" Version="1.9.x" />
<PackageReference Include="SQLitePCLRaw.bundle_green" Version="2.1.x" />
```

## ?? Data Flow Cheat Sheet

### Upload Model
```
User ? FilePicker ? Parse ? Generate Thumbnail ? 
Add to Collection ? Save to DB ? UI Updates
```

### Assign to Project
```
User ? ProjectSelectorDialog ? Select Project ? 
Update Model ? Save to DB ? Update Project ? 
Refresh View ? Badge Appears
```

### Filter by Project
```
User ? Click Project ? SelectedProject Changes ? 
FilterModelsByProjectAsync ? Models.Clear ? 
Load Filtered Models ? Update ProjectNames ? 
UI Updates
```

### Delete Model
```
User ? Click Delete ? Confirmation ? Models.Remove ? 
Delete from DB ? UI Updates
```

## ?? When Modifying the App

### Adding a new model property:
1. Add to `Model3DFile.cs` (with INotifyPropertyChanged)
2. Add to `Model3DFileDb.cs` (database entity)
3. Update `DatabaseService.cs` mapping methods
4. Update XAML if displaying in UI
5. Test database migration (may need to reset DB)

### Adding a new command:
1. Declare `ICommand` property in ViewModel
2. Initialize in constructor: `new Command(async () => await Method())`
3. Implement async method
4. Add button/trigger in XAML with `Command="{Binding CommandName}"`
5. Handle errors with try/catch and `ShowAlertAsync`

### Adding a new page:
1. Create XAML + code-behind in `Pages/`
2. Create ViewModel in `ViewModels/`
3. Register in `MauiProgram.cs` DI container
4. Add route in `AppShell.xaml`
5. Navigate with `Shell.Current.GoToAsync("routeName")`

### Debugging tips:
- Check Debug console for `Debug.WriteLine` statements
- Use breakpoints in async methods (not async void)
- Verify DI registration in `MauiProgram.cs`
- Check XAML binding errors in Output window
- Ensure `x:DataType` matches ViewModel type

## ?? Finding Things in the Code

### "Where is the model grid?"
`MainPage.xaml` - Center column, CollectionView with GridItemsLayout

### "Where is the 3D viewer?"
`ModelDetailPage.xaml` - Left column, `Model3DViewer` control

### "Where are projects created?"
`MainViewModel.cs` - `CreateProject()` method

### "Where is the database initialized?"
`DatabaseService.cs` - Constructor calls `InitializeDatabaseAsync()`

### "Where are STL files parsed?"
`Services/StlParser.cs` - `ParseStlAsync()` method

### "Where are thumbnails generated?"
`Services/ThumbnailGenerator.cs` - `GenerateThumbnailAsync()` method

### "Where is the project badge in grid?"
`MainPage.xaml` - Line ~420, Frame with `IsVisible="{Binding ProjectId}"`

### "Where is navigation handled?"
`AppShell.xaml` - Shell routes defined
`MainViewModel.cs` - `Shell.Current.GoToAsync()` calls

## ?? Things to Remember

1. **Always use property setters in constructors** (not field assignment)
2. **Update ProjectName when ProjectId changes** (caching pattern)
3. **Call `UpdateAllProjectNames()` after bulk data loads**
4. **Use `TaskScheduler.FromCurrentSynchronizationContext()` for UI updates in ContinueWith**
5. **Register new services in `MauiProgram.cs`**
6. **Use `IsVisible` for conditional rendering (better than Opacity)**
7. **Async all the way** - no sync-over-async
8. **Database operations are async** - always await
9. **Error handling** - try/catch with user-friendly messages
10. **Property changes trigger UI updates** - use `OnPropertyChanged()`

## ?? Quick Help

### "How do I add a new UI element?"
? Add to XAML with data binding, property in ViewModel

### "How do I save data to database?"
? `await _databaseService.SaveModelAsync(model)`

### "How do I navigate to another page?"
? `await Shell.Current.GoToAsync("PageName")`

### "How do I show a dialog?"
? Create a ContentPage with `TaskCompletionSource`, push as modal

### "How do I bind a collection to UI?"
? `ObservableCollection<T>` in ViewModel, `ItemsSource="{Binding Collection}"` in XAML

### "How do I filter the model list?"
? See `FilterModelsByProjectAsync()` - clear Models, add filtered items

### "How do I add a command to a button?"
? `ICommand` property + `new Command(() => Method())` + `Command="{Binding CommandName}"`

### "How do I show/hide UI based on condition?"
? `IsVisible="{Binding BoolProperty}"` or with converter

---

This quick reference should help AI assistants quickly understand and modify the application! ??
