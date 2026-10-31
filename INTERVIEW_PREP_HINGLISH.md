# WheelConvert — Complete Interview Preparation Guide (Hinglish)

Yeh document aapke **technical interviews** ke liye banaya gaya hai taaki agar interviewer aapse pooche:
> *"Apne WheelConvert project ke baare me batao aur samjhao ki code me actually kya ho raha hai?"*  
to aap deep technical understanding ke saath explain kar sako.

---

## 1. 🎯 Elevator Pitch (1-Minute Project Summary)

> **Interviewer ko kaise bolna hai:**  
> *"Sir, WheelConvert ek modern Windows 10 aur 11 utility hai jo background me system tray me silently run karti hai. Iska main purpose hai file conversion aur basic editing ko 10x fast banana without opening any heavy software. User Windows File Explorer me kisi bhi file ko select karke `Ctrl + Shift + Space` dabata hai, aur seedha mouse cursor ke paas ek transparent Radial (Pie) HUD popup hota hai jo bina window focus steal kiye format conversion (Images, PDF, Word DOCX, TXT), cropping, compression aur EXIF metadata inspection provide karta hai. Is project me maine .NET 8, WPF, Win32 Native P/Invoke, Windows Shell COM Automation, SkiaSharp aur OpenXML compression use kiya hai."*

---

## 2. 🏗️ High-Level Architecture (Kaise Kaam Karta Hai)

```
[Windows File Explorer]
         │ (User selects a file & presses Ctrl+Shift+Space)
         ▼
[HotkeyManager (Win32 RegisterHotKey)]
         │
         ▼
[WheelController (Master Orchestrator)]
         ├──> [ExplorerSelectionReader (Shell COM)] -> Finds selected file path
         ├──> [RadialMenuWindow (WPF Topmost/NoActivate)] -> Displays radial wheel at cursor
         └──> [OverlayInputHook (SetWindowsHookEx)] -> Keyboard/Mouse routing without focus loss
         │
         ▼ (User selects an action, e.g. "Convert to PDF" or "Crop")
[FileProcessor & DocumentProcessor / ImageProcessor]
         ├──> SkiaSharp (Images & Vector PDF rendering)
         ├──> Windows.Data.Pdf (PDF page rasterization)
         ├──> UglyToad.PdfPig (PDF text extraction)
         └──> System.IO.Compression (OpenXML DOCX generation)
         │
         ▼
[FileNamer] -> Saves unique non-overwriting file next to original
```

---

## 3. 📂 File-by-File Detailed Breakdown (Har File Ka Kaam)

### 🖥️ Layer 1: Application Entry & Lifecycle

#### 📄 `src/WheelConvert/App.xaml.cs`
* **Kya karta hai:** Application ka entry point hai.
* **Key Concepts:**
  1. **Single Instance via Mutex:**  
     `_singleInstance = new Mutex(true, @"Local\WheelConvert.SingleInstance", out var isFirst);`  
     *Interview Answer:* Agar user pehle se chal rahe app ko dobara open kare, to Mutex detect kar leta hai ki `isFirst == false` hai aur dusra instance start nahi hone deta.
  2. **Global Exception Shield:** `DispatcherUnhandledException` ko handle kiya hai taaki UI thread par koi unexpected issue aane par bhi system tray app silently crash na ho.
  3. **Tray Initialization:** `TrayIconService` aur `WheelController` ko boot karta hai.

#### 📄 `src/WheelConvert/Services/TrayIconService.cs`
* **Kya karta hai:** Taskbar ke corner me (near clock) notification icon aur context menu (Settings, Pause, Quit) chalata hai.
* **Key Concepts:**
  * Windows Forms ka `NotifyIcon` use karta hai.
  * Runtime par code se hi 32x32 pixel ka radial icon draw karta hai (`Graphics.FillPie`) bina kisi external `.ico` image file ke.

---

### ⌨️ Layer 2: Win32 Native Interop & Hotkeys

#### 📄 `src/WheelConvert/Services/HotkeyManager.cs`
* **Kya karta hai:** Global shortcut register karta hai jo pure Windows me kahin bhi dabane par fire ho.
* **Key Concepts:**
  * Win32 API `RegisterHotKey` aur `UnregisterHotKey` use karta hai.
  * Ek hidden **Message-Only Window** (`HWND_MESSAGE`) banata hai using WPF's `HwndSource`. Jab bhi OS `WM_HOTKEY` message bhejta hai, yeh `HotkeyPressed` event raise karta hai.

#### 📄 `src/WheelConvert/Core/Services/HotkeyParser.cs`
* **Kya karta hai:** String hotkey (jaise `"Ctrl+Shift+Space"`) ko parse karke Win32 modifiers (`MOD_CONTROL | MOD_SHIFT`) aur Virtual Key code (`VK_SPACE`) me convert karta hai.
* **Interview Point:** *"Pehle hotkey `Ctrl+Alt+Space` tha, lekin Windows me `Ctrl+Alt` ko OS internally `AltGr` treat karta hai jisse global hotkey conflict hota tha. Isliye humne default hotkey ko reliable `Ctrl+Shift+Space` banaya."*

#### 📄 `src/WheelConvert/Services/OverlayInputHook.cs`
* **Kya karta hai:** Jab radial wheel screen par dikh raha ho, yeh keyboard aur mouse inputs ko monitor karta hai.
* **Key Concepts:**
  * Win32 Low-Level Hooks: `SetWindowsHookEx(WH_KEYBOARD_LL)` aur `SetWindowsHookEx(WH_MOUSE_LL)`.
  * **Keyboard:** Arrow keys, Enter, Backspace, Esc ko intercept karta hai aur wheel navigation ke liye route karta hai.
  * **Mouse:** Agar user wheel ke bahar click kare, to `OutsideClick` event fire karta hai taaki wheel silently dismiss ho jaye.
  * **Efficiency:** Yeh hook sirf tab active rehta hai jab wheel visible ho, wheel band hote hi `UnhookWindowsHookEx` call ho jata hai taaki zero CPU overhead rahe.

#### 📄 `src/WheelConvert/Native/NativeMethods.cs` & `WindowBlur.cs`
* **Kya karta hai:** Windows P/Invoke declarations (`user32.dll`, `shcore.dll`).
* **Key Flags:**
  * `WS_EX_NOACTIVATE`: Window screen par dikhegi lekin current active app ka **focus nahi cheenegi**!
  * `WS_EX_TOOLWINDOW`: Window ko Windows `Alt + Tab` switcher se chupata hai taaki clutter na ho.
  * `GetDpiForMonitor`: Multi-monitor aur high-DPI scaling handle karta hai.

---

### 📂 Layer 3: Windows Explorer Integration

#### 📄 `src/WheelConvert.Core/Services/ExplorerSelectionReader.cs`
* **Kya karta hai:** Active File Explorer window me user ne kaunsi files select kar rakhi hain, unke exact file paths read karta hai.
* **Key Concepts:**
  * **Windows Shell COM Automation:** `Shell.Application` COM object ko dynamically instantiate karta hai.
  * `GetForegroundWindow()` se pata karta hai active window kaunsi hai, us window ke `Document.SelectedItems()` collect karta hai.
  * Zero external fragile COM DLLs — pure reflection-based COM invoke use kiya hai taaki kisi bhi Windows 10/11 version par crash na ho.

---

### 🎨 Layer 4: Radial UI & Math Engine

#### 📄 `src/WheelConvert/Core/Services/RadialGeometry.cs`
* **Kya karta hai:** Pure mathematical calculation for pie slices.
* **Key Concepts:**
  * **Geometry:** Angles are measured in degrees clockwise from 12 o'clock using `Math.Atan2(dx, -dy)`.
  * **Hit Testing:** `HitTest(dx, dy, innerRadius, outerRadius, count)` function mouse pointer ke distance aur angle se calculate karta hai ki user kaunse slice ke upar hover kar raha hai ($O(1)$ constant time).
  * **SVG/WPF Arc Path Generator:** `SegmentPath()` standard SVG/WPF mini-language (`M x,y A r,r ... L ... Z`) generate karta hai jo rounded circular slices banata hai.

#### 📄 `src/WheelConvert/Views/RadialMenuWindow.xaml` & `.cs`
* **Kya karta hai:** Screen par float hone wala radial menu window.
* **Key Concepts:**
  * `AllowsTransparency="True"` aur `Background="Transparent"` — transparent canvas window.
  * DPI aware mouse positioning: Mouse cursor ke exact position par center hota hai, lekin screen ke edges se bahar nahi nikalta (`ClampRange`).
  * Smooth animations: Scale + Opacity fade in (170ms) aur fade out (110ms) using WPF `Storyboard`.
  * Hover Dwell timer: Agar user "Convert" slice par ~400ms tak mouse rakhe, to automatically Level 2 (Formats menu) khul jata hai.

---

### ⚙️ Layer 5: Conversion & Processing Engine

#### 📄 `src/WheelConvert.Core/Services/FileProcessor.cs`
* **Kya karta hai:** Central Dispatcher — Wheel se aayi hui action ko sahi engine tak bhejta hai.
* **Key Concepts:**
  * Action check karta hai:
    - Level 2 (Format Conversion) -> `ConvertFile`
    - Compress -> `CompressFile`
    - Add BG -> `AddBackground`
    - Crop -> Throws `CropRequiredException` (taaki interactive Crop UI open ho)
    - Metadata -> Throws `MetadataViewRequiredException` (taaki Metadata viewer open ho)

#### 📄 `src/WheelConvert.Core/Services/ImageProcessor.cs`
* **Kya karta hai:** Images par high-performance operations karta hai using **SkiaSharp** (Google's open-source 2D graphic library).
* **Key Operations:**
  * `Convert`: Re-encodes bitmap between JPG, PNG, WEBP, BMP, AVIF, HEIC.
  * `Compress`: Lower quality encoding (e.g. 65%) with optimized compression.
  * `Crop`: `SKBitmap` ko specified bounding box `SKRectI` par crop karta hai.
  * `AddBackground`: Alpha transparency ko check karke solid white background add karta hai.

#### 📄 `src/WheelConvert.Core/Services/DocumentProcessor.cs`
* **Kya karta hai:** PDF aur Microsoft Word (DOCX) ka pure custom conversion engine.
* **Key Operations:**
  1. **Image ➔ PDF:** SkiaSharp ke native `SKDocument.CreatePdf` se vector PDF create karta hai.
  2. **Image ➔ DOCX:** `System.IO.Compression.ZipArchive` use karke valid OpenXML Word document banata hai aur image ko `word/media/image1.png` me embed karta hai.
  3. **PDF ➔ Images (JPG/PNG):** Windows 10/11 ki native `Windows.Data.Pdf.PdfDocument` API se har PDF page ko crisp visual image me rasterize karta hai.
  4. **PDF ➔ DOCX / TXT:** `UglyToad.PdfPig` library se text extract karke paragraphs me structure karta hai.
  5. **DOCX ➔ PDF:** `word/document.xml` se text paragraphs read karta hai aur SkiaSharp PDF canvas par text wrapping ke saath clean A4 document render karta hai.

#### 📄 `src/WheelConvert.Core/Services/MetadataReader.cs`
* **Kya karta hai:** `MetadataExtractor` library se EXIF tags, camera details, ISO, shutter speed, dimensions aur file size read karta hai.

#### 📄 `src/WheelConvert.Core/Services/FileNamer.cs`
* **Kya karta hai:** Collision-proof output path generator.
* **Interview Point:** *"Yeh function ensure karta hai ki user ka original data kabhi overwrite na ho. Agar `photo.png` pehle se exist karti hai, to yeh automatically `photo (2).png`, `photo (3).png` banata hai."*

---

### 🪟 Layer 6: User Interface Windows

#### 📄 `src/WheelConvert/Views/SettingsWindow.xaml` & `.cs`
* **Kya karta hai:** Full Settings aur User Guidance dialog jo tray icon se khulta hai.
* **Tabs:**
  1. **Settings Tab:** Hotkey change karna, output directory choose karna, wheel diameter size slider, accent color themes, and "Start on Windows boot" toggle.
  2. **How to Use Tab:** Visual 3-step guide, radial action directory, and keyboard shortcut reference.

#### 📄 `src/WheelConvert/Views/CropWindow.xaml` & `.cs`
* **Kya karta hai:** User ko visually crop box drag karne deta hai image par aur real-time selection dimensions (width × height px) display karta hai.

#### 📄 `src/WheelConvert/Views/MetadataWindow.xaml` & `.cs`
* **Kya karta hai:** Filterable DataGrid me image ke saare camera aur technical metadata display karta hai.

---

### 📦 Layer 7: Deployment & Installer

#### 📄 `installer.iss` (Inno Setup Script)
* **Kya karta hai:** Single universal setup installer (`WheelConvert-Setup.exe`) compile karta hai.
* **Key Architecture:**
  * `ArchitecturesInstallIn64BitMode=x64compatible` use karta hai.
  * Setup run hone par check karta hai: agar laptop 64-bit hai to 64-bit binary install karta hai; agar 32-bit laptop hai to automatically 32-bit binary install karta hai!

---

## 4. 💡 Top 5 Interview Questions & Answers

### Q1: *"Aapne app ko WPF me kyun banaya, Electron ya Python me kyun nahi?"*
> **Answer:**  
> *"Sir, Electron apps memory-heavy hoti hain (200MB+ RAM for simple utility) aur native OS integration slow hota hai. Python me Windows low-level hooks aur DPI-scaling sluggish hoti hai. WheelConvert background utility hai jisko instant (<50ms) popup response chahiye. .NET 8 WPF direct Win32 interop, hardware-accelerated Direct9/DirectX rendering aur low memory footprint provide karta hai."*

### Q2: *"Radial menu open hote waqt dusra app focus kyun nahi khota?"*
> **Answer:**  
> *"Humne window creation ke waqt Win32 extended window style `WS_EX_NOACTIVATE` apply kiya hai via `SetWindowLong`. Isse window topmost level par render to hoti hai, lekin OS ka active keyboard/mouse focus previous window (jaise File Explorer ya browser) par hi rehta hai."*

### Q3: *"Jab window focused nahi hai, to keyboard shortcuts (Esc, Arrows) kaise chalte hain?"*
> **Answer:**  
> *"Humne `SetWindowsHookEx(WH_KEYBOARD_LL)` use karke ek low-level keyboard hook install kiya hai jo sirf tab active hota hai jab wheel screen par open ho. Yeh hook keystrokes intercept karke WPF dispatcher ko pass karta hai aur wheel dismiss hote hi hook unhook ho jata hai."*

### Q4: *"Explorer me selected files kaise pata lagti hain bina context menu extension ke?"*
> **Answer:**  
> *"Hum Shell COM Automation use karte hain. `GetForegroundWindow()` se active Explorer window ka HWND lete hain aur `Shell.Application` ke open windows collection me match karke us window ke `Document.SelectedItems` se real paths extract karte hain."*

### Q5: *"Bina external Microsoft Word install kiye .docx file kaise generate ki?"*
> **Answer:**  
> *"DOCX actually ek zipped OpenXML package hota hai. Humne `System.IO.Compression.ZipArchive` use karke standard OpenXML XML files (`[Content_Types].xml`, `_rels/.rels`, `word/document.xml`) code se construct kiye aur images ko `word/media/` me embed kiya. Isliye user ke PC par MS Office install hona zaroori nahi hai."*
