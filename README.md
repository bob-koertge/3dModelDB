# 3D Model DB

A cross-platform 3D model viewer and management application built with .NET MAUI that supports STL and 3MF file formats.

![Platform Support](https://img.shields.io/badge/platform-Android%20%7C%20iOS%20%7C%20macOS%20%7C%20Windows-blue)
![.NET Version](https://img.shields.io/badge/.NET-9.0-purple)
![License: Polyform Noncommercial](https://img.shields.io/badge/license-Polyform%20Noncommercial-green.svg)

## ?? Table of Contents

- [Features](#features)
- [Prerequisites](#prerequisites)
- [Platform-Specific Setup](#platform-specific-setup)
  - [Windows](#windows)
  - [macOS](#macos)
  - [Android](#android)
  - [iOS](#ios)
- [Building the Project](#building-the-project)
- [Running the Application](#running-the-application)
- [Project Structure](#project-structure)
- [Technologies Used](#technologies-used)
- [Contributing](#contributing)
- [License](#license)

## ? Features

- ?? **Interactive 3D Viewer** - Rotate, zoom, and pan 3D models
- ?? **Multi-Format Support** - Import and view STL (ASCII & Binary) and 3MF files
- ??? **Thumbnail Generation** - Auto-generated thumbnails for quick preview
- ??? **Tag Management** - Organize models with custom tags
- ?? **Model Information** - View triangle count, file size, and metadata
- ?? **Detail View** - Double-click models for detailed inspection
- ?? **Model Library** - Grid-based model management
- ?? **Optimized Performance** - Object pooling, async operations, memory-efficient parsing

## ?? Prerequisites

### Required Software

- **[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** - Latest version
- **Visual Studio 2022** (17.8 or later) or **Visual Studio Code** with C# Dev Kit

### Platform-Specific Requirements

#### Windows Development
- Windows 10 version 1809 or higher (build 17763 or later)
- Visual Studio 2022 with:
  - .NET Multi-platform App UI development workload
  - Windows 11 SDK (10.0.22621.0 or later)

#### macOS Development
- macOS 13 (Ventura) or later
- Xcode 15.0 or later
- Visual Studio for Mac 2022 or Visual Studio Code
- Apple Developer account (for device deployment)

#### Android Development
- Android SDK API 21 (Android 5.0) or higher
- Android Emulator or physical device
- Java Development Kit (JDK) 11 or later

#### iOS Development
- macOS 13 (Ventura) or later
- Xcode 15.0 or later
- Apple Developer account (for device deployment)
- iOS 15.0 or higher

## ??? Platform-Specific Setup

### Windows

#### Visual Studio 2022

1. **Install Visual Studio 2022**
   - Download from [visualstudio.microsoft.com](https://visualstudio.microsoft.com/)
   - During installation, select **.NET Multi-platform App UI development** workload

2. **Verify .NET MAUI Installation**
   ```powershell
   dotnet workload list
   ```
   Ensure `maui` is listed. If not, install it:
   ```powershell
   dotnet workload install maui
   ```

3. **Clone the Repository**
   ```powershell
   git clone https://github.com/bob-koertge/3dModelDB.git
   cd 3dModelDB
   ```

4. **Restore Dependencies**
   ```powershell
   dotnet restore
   ```

#### Visual Studio Code

1. **Install Extensions**
   - C# Dev Kit
   - .NET MAUI extension

2. **Install .NET MAUI Workload**
   ```powershell
   dotnet workload install maui
   ```

### macOS

#### Visual Studio for Mac

1. **Install Visual Studio for Mac**
   - Download from [visualstudio.microsoft.com/vs/mac](https://visualstudio.microsoft.com/vs/mac/)
   - Install .NET Multi-platform App UI workload

2. **Install Xcode**
   ```bash
   # Install from Mac App Store or
   xcode-select --install
   ```

3. **Clone the Repository**
   ```bash
   git clone https://github.com/bob-koertge/3dModelDB.git
   cd 3dModelDB
   ```

4. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

#### Visual Studio Code

1. **Install .NET 9 SDK**
   ```bash
   # Download from dotnet.microsoft.com or use Homebrew
   brew install --cask dotnet-sdk
   ```

2. **Install MAUI Workload**
   ```bash
   dotnet workload install maui
   ```

3. **Install Xcode Command Line Tools**
   ```bash
   xcode-select --install
   ```

### Android

#### Setup Android SDK

1. **Through Visual Studio**
   - Visual Studio will automatically install Android SDK
   - Navigate to Tools > Android > Android SDK Manager

2. **Manual Installation**
   ```bash
   # Install Android SDK via command line
   dotnet workload install android
   ```

3. **Create Android Emulator**
   - Open Android Device Manager in Visual Studio
   - Create a new virtual device (recommended: Pixel 5 API 34)

4. **Enable USB Debugging** (for physical devices)
   - On your Android device: Settings > About Phone > Tap "Build Number" 7 times
   - Settings > Developer Options > Enable USB Debugging

### iOS

#### Setup iOS Development

1. **Install Xcode**
   ```bash
   # Via Mac App Store or
   xcode-select --install
   ```

2. **Accept Xcode License**
   ```bash
   sudo xcodebuild -license accept
   ```

3. **Pair iOS Device** (for physical device testing)
   - Connect iPhone/iPad via USB
   - Open Xcode > Window > Devices and Simulators
   - Trust the device when prompted

4. **Configure Provisioning Profile**
   - Open Xcode
   - Sign in with Apple ID (Preferences > Accounts)
   - Project will auto-generate development certificate

## ?? Building the Project

### Command Line

#### Build for Specific Platform

**Windows:**
```powershell
dotnet build -f net9.0-windows10.0.19041.0
```

**Android:**
```bash
dotnet build -f net9.0-android
```

**iOS:**
```bash
dotnet build -f net9.0-ios
```

**macOS (Mac Catalyst):**
```bash
dotnet build -f net9.0-maccatalyst
```

#### Build All Platforms
```bash
dotnet build
```

### Visual Studio

1. Open `MauiApp3.sln` in Visual Studio 2022
2. Select target framework from dropdown:
   - **Windows Machine** for Windows
   - **Android Emulator** or **Android Device** for Android
   - **iOS Simulator** or **iOS Device** for iOS
   - **Mac Catalyst** for macOS
3. Build > Build Solution (Ctrl+Shift+B)

### Visual Studio Code

1. Open folder in VS Code
2. Press `F1` and type `.NET: Build`
3. Or use terminal: `dotnet build`

## ?? Running the Application

### Visual Studio 2022

1. Set the startup project to **MauiApp3**
2. Select target platform/device from dropdown
3. Press **F5** or click **Start Debugging**

### Visual Studio Code

```bash
# Windows
dotnet run -f net9.0-windows10.0.19041.0

# Android (with emulator running)
dotnet run -f net9.0-android

# iOS (with simulator running)
dotnet run -f net9.0-ios

# macOS
dotnet run -f net9.0-maccatalyst
```

### Command Line with Specific Device

**Android - List Devices:**
```bash
adb devices
```

**Android - Run on Specific Device:**
```bash
dotnet build -t:Run -f net9.0-android -p:AndroidDevice=<device-id>
```

**iOS - List Simulators:**
```bash
xcrun simctl list devices
```

**iOS - Run on Simulator:**
```bash
dotnet build -t:Run -f net9.0-ios
```

## ?? Platform-Specific Run Instructions

### Windows

**Debug Mode:**
```powershell
dotnet run -f net9.0-windows10.0.19041.0 --configuration Debug
```

**Release Mode:**
```powershell
dotnet run -f net9.0-windows10.0.19041.0 --configuration Release
```

### Android Emulator

1. **Start Emulator:**
   ```bash
   # List available emulators
   emulator -list-avds
   
   # Start emulator
   emulator -avd <emulator-name>
   ```

2. **Run App:**
   ```bash
   dotnet build -t:Run -f net9.0-android
   ```

### Android Device

1. **Enable USB Debugging** on device
2. **Connect via USB**
3. **Verify connection:**
   ```bash
   adb devices
   ```
4. **Run app:**
   ```bash
   dotnet build -t:Run -f net9.0-android
   ```

### iOS Simulator

```bash
# Build and run on simulator
dotnet build -t:Run -f net9.0-ios
```

### iOS Device

1. **Pair device** in Xcode
2. **Trust computer** on device
3. **Build and deploy:**
   ```bash
   dotnet build -t:Run -f net9.0-ios -p:RuntimeIdentifier=ios-arm64
   ```

### macOS (Mac Catalyst)

```bash
# Run on local Mac
dotnet run -f net9.0-maccatalyst
```

## ??? Project Structure

```
3dModelDB/
??? Behaviors/              # XAML behaviors
?   ??? EventToCommandBehavior.cs
??? Controls/               # Custom UI controls
?   ??? Model3DViewer.cs   # 3D rendering control
??? Converters/            # Value converters
?   ??? ValueConverters.cs
??? Models/                # Data models
?   ??? Model3DFile.cs
??? Pages/                 # UI pages
?   ??? ModelDetailPage.xaml
?   ??? ModelDetailPage.xaml.cs
??? Platforms/             # Platform-specific code
?   ??? Android/
?   ??? iOS/
?   ??? MacCatalyst/
?   ??? Windows/
??? Resources/             # App resources
?   ??? AppIcon/
?   ??? Fonts/
?   ??? Images/
?   ??? Styles/
??? Services/              # Business logic
?   ??? Model3DService.cs
?   ??? StlParser.cs
?   ??? ThreeMfParser.cs
?   ??? ThumbnailGenerator.cs
??? Utilities/             # Helper classes
?   ??? SampleModelGenerator.cs
??? ViewModels/            # MVVM ViewModels
?   ??? MainViewModel.cs
?   ??? ModelDetailViewModel.cs
??? App.xaml               # App configuration
??? AppShell.xaml          # Shell navigation
??? MainPage.xaml          # Main UI
??? MauiProgram.cs         # App startup
??? README.md              # This file
```

## ?? Technologies Used

- **.NET 9** - Latest .NET framework
- **.NET MAUI** - Cross-platform UI framework
- **SkiaSharp** - 2D graphics rendering
- **C# 12** - Latest C# language features
- **MVVM Pattern** - Clean architecture
- **XAML** - UI markup

### Key NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Maui.Controls | 9.0.x | MAUI framework |
| SkiaSharp.Views.Maui.Controls | 3.119.1 | 3D rendering |
| Microsoft.Extensions.Logging.Debug | 9.0.5 | Debugging |

## ?? Performance Features

- **Object Pooling** - Reuses paint objects for 90% fewer allocations
- **Async Operations** - Non-blocking file I/O and parsing
- **Streaming** - Handles large files efficiently
- **ArrayPool** - Memory-efficient buffer management
- **Span<T>** - Zero-copy string operations
- **Optimized Rendering** - 50% faster frame rates

## ?? Usage

### Uploading Models

1. Click **"?? Upload Model"** in the left drawer
2. Select an STL or 3MF file
3. Wait for parsing and thumbnail generation
4. Model appears in the grid

### Viewing Models

1. **Single-click** to select a model
2. **Double-click** to open detailed view
3. **Drag** in 3D viewer to rotate
4. **Scroll** to zoom
5. Click **"??"** to reset view

### Managing Tags

1. Select a model
2. Type tag name in the input field
3. Press **Enter** or click **"+"**
4. Click **"×"** on tags to remove

## ?? Troubleshooting

### Build Errors

**Error: NETSDK1005 - Assets file not found**
```bash
dotnet restore
```

**Error: Unable to find package 'SkiaSharp'**
```bash
dotnet nuget locals all --clear
dotnet restore
```

### Android Issues

**Emulator won't start:**
- Enable Hardware Acceleration (Intel HAXM or AMD Hypervisor)
- Check Hyper-V is disabled (Windows)

**App crashes on launch:**
- Check minimum Android API level (21)
- Verify AndroidManifest.xml permissions

### iOS Issues

**Code signing error:**
- Open Xcode and sign in with Apple ID
- Trust developer certificate on device

**Simulator not found:**
```bash
xcrun simctl list devices
```

### Windows Issues

**WinUI 3 deployment fails:**
- Install latest Windows SDK
- Run as Administrator

## ?? Additional Resources

- [.NET MAUI Documentation](https://learn.microsoft.com/dotnet/maui/)
- [SkiaSharp Documentation](https://learn.microsoft.com/xamarin/xamarin-forms/user-interface/graphics/skiasharp/)
- [STL File Format](https://en.wikipedia.org/wiki/STL_(file_format))
- [3MF File Format](https://3mf.io/)

## ?? Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## ?? License

Copyright © 2025 Bob Koertge

This project is licensed under the MIT License - see the LICENSE file for details.

## ?? Author

**Bob Koertge**
- GitHub: [@bob-koertge](https://github.com/bob-koertge)
- Repository: [3dModelDB](https://github.com/bob-koertge/3dModelDB)

## ?? Acknowledgments

- .NET MAUI Team for the excellent framework
- SkiaSharp contributors for 2D graphics rendering
- The open-source community

## ?? Support

For issues, questions, or suggestions:
- Open an issue on [GitHub](https://github.com/bob-koertge/3dModelDB/issues)
- Check existing issues before creating new ones

---

**Built with ?? using .NET MAUI**
