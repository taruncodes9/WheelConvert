# WheelConvert

Radial (pie) menu file converter for Windows 10 1809+ / Windows 11.  
Select files in Explorer → press a hotkey → pick an action from a sleek radial wheel.  
Free/OSS libraries only. No admin needed.

![.NET 8](https://img.shields.io/badge/.NET-8.0-purple) ![Windows](https://img.shields.io/badge/OS-Windows%2010%2F11-blue) ![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Radial wheel UI** — appears at your cursor, never steals focus from your current app
- **Two-level menu** — Level 1: action picker → Level 2: format picker (for Convert)
- **Format conversions**:
  - **Images** (JPG, PNG, WEBP, BMP, HEIC, AVIF, TIFF) → Convert to other formats, **PDF**, or **DOCX**
  - **PDF** → Convert to **JPG**, **PNG**, **DOCX**, or **TXT**
  - **DOCX** → Convert to **PDF** or **TXT**
- **Compress** — re-encodes images at lower quality for smaller files
- **Crop** — interactive crop window with drag-to-select rectangle
- **Add Background** — replaces transparency with solid white
- **Metadata viewer** — shows all EXIF/file metadata (camera, date, dimensions, etc.)
- **System tray** — lives in the notification area with Settings / Pause / Quit
- **Single instance** — only one copy runs at a time
- **Multi-monitor & DPI aware** — handles mixed-DPI setups correctly
- **Keyboard navigation** — arrow keys, Enter, Esc, Backspace all work on the wheel

## Build & Run

```
dotnet build WheelConvert.sln
dotnet run --project src/WheelConvert
dotnet test
```

> **Note:** Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). No admin or Visual Studio needed.

## Installation (Universal Installer for Any Laptop)

WheelConvert provides a single **Universal Setup File** that supports **both 64-bit and 32-bit Windows 10 and Windows 11**:

📍 **Setup File Location:**
```
publish\installer\WheelConvert-Setup.exe
```

### Installation Steps for Users:
1. **Double-click `WheelConvert-Setup.exe`**.
2. The Setup Wizard will open:
   - It **automatically detects** if the laptop is 64-bit or 32-bit and installs the matching optimized binary.
   - Choose whether you want a **Desktop shortcut** and whether to **Launch automatically on Windows startup**.
3. Click **Install**.
4. On the final screen, leave **"Launch WheelConvert"** checked and click **Finish**.
5. WheelConvert starts immediately and runs quietly in the system tray (near your taskbar clock).

---

## How to Use WheelConvert

Once installed and running:
1. Open **Windows File Explorer** and **select any file** (or multiple files) you want to convert or edit.
2. Press the hotkey on your keyboard:
   ```
   Ctrl + Shift + Space
   ```
3. A sleek **radial pie menu** will appear right at your mouse cursor:
   - **Convert**: Hover your mouse over the "Convert" slice (~400ms) or click it to reveal target formats (PNG, JPG, WEBP, PDF, etc.). Click your desired format to convert instantly.
   - **Crop**: Opens an interactive crop preview window where you can drag a rectangle to crop the image.
   - **Compress**: Re-encodes the image at a reduced file size without changing format.
   - **Add BG**: Removes transparency by blending with a clean white background.
   - **Metadata**: Opens a window showing camera model, capture date, resolution, and EXIF tags.
4. Converted files are saved right next to the original files with unique names (never overwriting).

---

## Standalone Executables (No Install Needed)

If you prefer to run WheelConvert portably without running an installer:
- **64-bit Windows 10/11**: `publish\x64\WheelConvert.exe`
- **32-bit Windows 10**: `publish\x86\WheelConvert.exe`

Just copy either `.exe` and double-click to run.

## How to Use

1. **Select a file** (or multiple files) in Explorer
2. Press **Ctrl+Shift+Space**
3. The radial wheel appears at your cursor
4. **Hover or click** an action:
   - **Convert** → hover ~400ms to open format picker → click target format (PNG, JPG, WEBP…)
   - **Compress** → creates a `_compressed` file at 65% quality
   - **Crop** → opens interactive crop window, drag a rectangle, click Crop
   - **Add BG** → replaces transparency with white background
   - **Metadata** → opens a viewer showing all EXIF/file properties
5. Output file is saved next to the original (never overwrites)

## Tech Stack

| Layer | Technology | License |
|---|---|---|
| **UI Framework** | WPF (.NET 8, `net8.0-windows10.0.17763.0`) | MIT |
| **MVVM** | [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) 8.3.2 | MIT |
| **Image Processing** | [SkiaSharp](https://github.com/mono/SkiaSharp) 2.88.7 | MIT |
| **Metadata Reading** | [MetadataExtractor](https://github.com/drewnoakes/metadata-extractor-dotnet) 2.8.1 | Apache 2.0 |
| **Tray Icon** | WinForms `NotifyIcon` (built-in) | MIT |
| **Explorer Integration** | Shell COM Automation (`Shell.Application`) via reflection | N/A (OS) |
| **Global Hotkey** | Win32 `RegisterHotKey` / `WM_HOTKEY` | N/A (OS) |
| **Input Hooks** | Win32 low-level keyboard/mouse hooks (`SetWindowsHookEx`) | N/A (OS) |
| **Window Effects** | `SetWindowCompositionAttribute` (optional acrylic blur) | N/A (OS) |

## Architecture

```
WheelConvert.sln
├── src/
│   ├── WheelConvert/                    # WPF app (UI + Win32 interop)
│   │   ├── App.xaml.cs                  # Entry point, single instance, tray setup
│   │   ├── Services/
│   │   │   ├── HotkeyManager.cs         # Global hotkey registration (RegisterHotKey)
│   │   │   ├── OverlayInputHook.cs      # Keyboard/mouse hooks while wheel is visible
│   │   │   ├── TrayIconService.cs       # System tray icon + context menu
│   │   │   └── WheelController.cs       # Glue: hotkey → Explorer read → wheel → actions
│   │   ├── ViewModels/
│   │   │   └── RadialMenuViewModel.cs   # Two-level menu state + navigation
│   │   ├── Views/
│   │   │   ├── RadialMenuWindow.xaml     # Transparent overlay window for the wheel
│   │   │   ├── CropWindow.xaml          # Interactive crop rectangle on image preview
│   │   │   └── MetadataWindow.xaml      # EXIF/metadata DataGrid viewer
│   │   └── Native/
│   │       ├── NativeMethods.cs         # P/Invoke declarations
│   │       └── WindowBlur.cs            # Optional acrylic blur effect
│   │
│   └── WheelConvert.Core/              # Pure logic (no UI dependency)
│       ├── Models/
│       │   ├── AppSettings.cs           # Hotkey, wheel size, accent color, etc.
│       │   └── WheelModels.cs           # WheelItem, WheelAction, FileContext
│       └── Services/
│           ├── ExplorerSelectionReader.cs # Reads selected files from Explorer via COM
│           ├── FileKinds.cs              # Extension → FileKind mapping
│           ├── FileNamer.cs              # Unique output paths (never overwrites)
│           ├── FileProcessor.cs          # Dispatches wheel actions to processors
│           ├── FormatCatalog.cs          # Valid conversion targets per file type
│           ├── HotkeyParser.cs           # Parses "Ctrl+Shift+Space" → modifiers + key
│           ├── ImageProcessor.cs         # SkiaSharp: convert, compress, crop, add BG
│           ├── MenuDefinitions.cs        # Level-1 wheel items
│           ├── MetadataReader.cs         # MetadataExtractor: EXIF, dimensions, camera
│           ├── RadialGeometry.cs         # Pure math: arcs, hit-testing, segment paths
│           └── SettingsStore.cs          # JSON settings persistence
│
└── tests/
    └── WheelConvert.Tests/              # xUnit tests (74 passing)
        ├── FileNamerTests.cs
        └── SettingsStoreTests.cs
```

## Progress

- [x] Step 1: solution scaffold, tray icon, single instance, settings store, FileNamer + tests
- [x] Step 2: radial wheel UI + hotkey
- [ ] Step 3: Shift+drag + drop
- [x] Step 4: Explorer selection reader (reads selected files via Shell COM)
- [x] Step 5: converters (SkiaSharp image & PDF, DocumentProcessor DOCX & PDF/TXT, MetadataExtractor)
- [x] Step 6: Settings & User Guidance window (hotkey customization, destination folder, wheel diameter, theme colors, startup option, built-in guide)
