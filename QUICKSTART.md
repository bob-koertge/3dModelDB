# Quick Start Guide - 3D Model DB

A concise guide to get you up and running quickly on any platform.

## ? 5-Minute Setup

### 1. Prerequisites

**All Platforms:**
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

**Platform-Specific:**
- **Windows**: Windows 10 1809+ (build 17763+)
- **macOS**: macOS 13+ with Xcode 15+
- **Android**: Android SDK API 21+
- **iOS**: Xcode 15+ and Apple Developer account

### 2. Clone & Restore

```bash
git clone https://github.com/bob-koertge/3dModelDB.git
cd 3dModelDB
dotnet restore
```

### 3. Run on Your Platform

**Windows:**
```powershell
dotnet run -f net9.0-windows10.0.19041.0
```

**macOS:**
```bash
dotnet run -f net9.0-maccatalyst
```

**Android** (with emulator running):
```bash
dotnet run -f net9.0-android
```

**iOS** (with simulator running):
```bash
dotnet run -f net9.0-ios
```

## ?? Platform-Specific Quick Commands

### Windows Development

```powershell
# Build
dotnet build -f net9.0-windows10.0.19041.0

# Run Debug
dotnet run -f net9.0-windows10.0.19041.0 -c Debug

# Run Release
dotnet run -f net9.0-windows10.0.19041.0 -c Release

# Publish
dotnet publish -f net9.0-windows10.0.19041.0 -c Release
```

### macOS Development

```bash
# Build
dotnet build -f net9.0-maccatalyst

# Run
dotnet run -f net9.0-maccatalyst

# Create app bundle
dotnet build -f net9.0-maccatalyst -c Release

# Publish
dotnet publish -f net9.0-maccatalyst -c Release
```

### Android Development

```bash
# List emulators
emulator -list-avds

# Start emulator
emulator -avd <emulator-name> &

# List connected devices
adb devices

# Build
dotnet build -f net9.0-android

# Run on emulator/device
dotnet build -t:Run -f net9.0-android

# Install APK
dotnet build -t:Install -f net9.0-android

# Create APK
dotnet publish -f net9.0-android -c Release
```

### iOS Development

```bash
# List simulators
xcrun simctl list devices

# Boot simulator (if not running)
xcrun simctl boot "<simulator-name>"

# Build
dotnet build -f net9.0-ios

# Run on simulator
dotnet build -t:Run -f net9.0-ios

# Run on device (arm64)
dotnet build -t:Run -f net9.0-ios -p:RuntimeIdentifier=ios-arm64

# Create IPA
dotnet publish -f net9.0-ios -c Release
```

## ?? First Run Checklist

### All Platforms
- [ ] .NET 9 SDK installed: `dotnet --version` (should show 9.x.x)
- [ ] MAUI workload installed: `dotnet workload list` (should include `maui`)
- [ ] Repository cloned
- [ ] Dependencies restored: `dotnet restore`
- [ ] Build successful: `dotnet build`

### Windows Only
- [ ] Visual Studio 2022 with MAUI workload
- [ ] Windows 11 SDK installed

### macOS Only
- [ ] Xcode installed and licensed: `xcode-select --install`
- [ ] Xcode license accepted: `sudo xcodebuild -license accept`

### Android Only
- [ ] Android SDK installed
- [ ] Emulator created or device connected
- [ ] USB debugging enabled (for device)
- [ ] Device visible: `adb devices`

### iOS Only
- [ ] Xcode installed and licensed
- [ ] Signed in with Apple ID in Xcode
- [ ] Simulator available or device paired
- [ ] Trusted computer on device (for physical device)

## ??? Common Commands Reference

### Workload Management

```bash
# List installed workloads
dotnet workload list

# Install MAUI workload
dotnet workload install maui

# Update workloads
dotnet workload update

# Restore workloads
dotnet workload restore
```

### Project Management

```bash
# Restore packages
dotnet restore

# Clean build artifacts
dotnet clean

# Build all platforms
dotnet build

# Build specific platform
dotnet build -f net9.0-<platform>

# Publish for distribution
dotnet publish -f net9.0-<platform> -c Release
```

### Device Management

**Android:**
```bash
# List devices
adb devices

# Install APK manually
adb install path/to/app.apk

# View logs
adb logcat

# Restart ADB
adb kill-server && adb start-server
```

**iOS:**
```bash
# List simulators
xcrun simctl list devices

# Boot simulator
xcrun simctl boot "<simulator-id>"

# Install app on simulator
xcrun simctl install booted path/to/app.app

# Open simulator
open -a Simulator
```

## ?? Troubleshooting Quick Fixes

### Build Fails

```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build
```

### MAUI Workload Issues

```bash
# Uninstall and reinstall MAUI
dotnet workload uninstall maui
dotnet workload install maui

# Or repair
dotnet workload repair
```

### Android Emulator Won't Start

**Windows:**
```powershell
# Check Hyper-V (disable if using Intel HAXM)
bcdedit /set hypervisorlaunchtype off

# Or install Intel HAXM
# Download from: https://github.com/intel/haxm/releases
```

**macOS/Linux:**
```bash
# Grant permissions
sudo chmod -R 755 $ANDROID_SDK_ROOT

# Check emulator
$ANDROID_SDK_ROOT/emulator/emulator -list-avds
```

### iOS Simulator Issues

```bash
# Reset simulator
xcrun simctl erase all

# Restart CoreSimulatorService
sudo killall -9 com.apple.CoreSimulator.CoreSimulatorService

# Reboot simulator
xcrun simctl shutdown all
xcrun simctl boot "<simulator-id>"
```

### Visual Studio Issues

**Windows:**
```powershell
# Repair Visual Studio
# Go to: Visual Studio Installer > More > Repair

# Or reinstall MAUI workload
# Go to: Visual Studio Installer > Modify > Individual Components
# Search for: .NET MAUI
```

**macOS:**
```bash
# Clear VS Mac cache
rm -rf ~/Library/Caches/VisualStudio

# Reset preferences
defaults delete com.microsoft.visual-studio
```

## ?? Platform-Specific Testing

### Windows

- **Run as Admin** if deployment fails
- **Enable Developer Mode**: Settings > Update & Security > For developers
- **Test on different Windows versions** if possible

### macOS

- **Test on different screen sizes** using Catalyst sizing classes
- **Check App Sandbox** permissions if file access fails
- **Test on both Intel and Apple Silicon** if possible

### Android

- **Test on different API levels**: 21, 24, 28, 31, 34
- **Test different screen sizes**: Phone, Tablet, Foldable
- **Check permissions** in AndroidManifest.xml
- **Test on physical device** for performance

### iOS

- **Test on different iOS versions**: 15.0+
- **Test different device sizes**: iPhone SE, iPhone 15, iPad
- **Check Info.plist** for required permissions
- **Test on physical device** for performance and camera access

## ?? Development Tips

### Hot Reload

Enable XAML Hot Reload for faster development:

**Visual Studio:**
- Tools > Options > XAML Hot Reload > Enable XAML Hot Reload

**Command Line:**
```bash
dotnet watch run -f net9.0-<platform>
```

### Debugging

**Set breakpoints** in:
- ViewModels for business logic
- Code-behind for UI interactions
- Services for file operations

**Use logging:**
```csharp
Console.WriteLine($"Debug: {message}");
Debug.WriteLine($"Debug: {message}");
```

### Performance

**Profile the app:**
- Visual Studio: Debug > Performance Profiler
- Check memory usage, CPU, and allocations

**Optimize builds:**
```bash
# Release build with optimizations
dotnet build -c Release -f net9.0-<platform>
```

## ?? Distribution

### Windows (MSIX Package)

```powershell
dotnet publish -f net9.0-windows10.0.19041.0 -c Release -p:WindowsPackageType=MSIX
```

### macOS (App Bundle)

```bash
dotnet publish -f net9.0-maccatalyst -c Release
# Output: bin/Release/net9.0-maccatalyst/publish/MauiApp3.app
```

### Android (APK/AAB)

```bash
# APK for sideloading
dotnet publish -f net9.0-android -c Release

# AAB for Google Play Store
dotnet publish -f net9.0-android -c Release -p:AndroidPackageFormat=aab
```

### iOS (IPA)

```bash
# Ad-hoc distribution
dotnet publish -f net9.0-ios -c Release -p:BuildIpa=true

# App Store distribution
# Use Xcode for final archive and upload
```

## ?? Next Steps

1. **Explore the app** - Upload STL/3MF files and test features
2. **Read the full README** - Comprehensive documentation
3. **Check optimization docs** - Performance improvements
4. **Review the code** - Learn MAUI patterns
5. **Contribute** - Submit PRs for improvements

## ?? Useful Links

- [Full README](README.md)
- [.NET MAUI Docs](https://learn.microsoft.com/dotnet/maui/)
- [GitHub Repository](https://github.com/bob-koertge/3dModelDB)
- [Issue Tracker](https://github.com/bob-koertge/3dModelDB/issues)

---

**Happy Coding! ??**
