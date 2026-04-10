# Text-Grab: WPF to Uno Platform Migration Plan

## 1. Source Application Summary

**Text-Grab** is a WPF desktop OCR utility (v4.12.1) by Joseph Finney that captures text from screens, images, and files using Windows OCR, Tesseract, and Windows AI engines. It features four primary modes: Fullscreen Grab, Grab Frame (floating overlay), Edit Text Window, and Quick Simple Lookup.

| Metric | Value |
|--------|-------|
| Target Framework | `net10.0-windows10.0.22621.0` |
| Architecture | Code-behind (no MVVM), Singleton services |
| UI Library | WPF + WPF-UI (Fluent Design) v4.2.0 |
| Total C# LOC | ~25,000+ |
| Views/Windows | 6 main + 9 child windows |
| Settings Pages | 6 |
| Custom Controls | 15 (7 UserControls, 8 Windows) |
| Styles/Resources | 6 resource dictionaries |
| External Libraries | 12 NuGet packages |

---

## 2. Architecture Analysis & Migration Strategy

### 2.1 Current Architecture: Code-Behind + Singleton Services

The app has **no ViewModel layer**. All UI logic lives in code-behind files (EditTextWindow.xaml.cs alone is 3,815 lines). State is managed via:

- `Properties.Settings` (WPF app settings) accessed globally through `AppUtilities.TextGrabSettings`
- `Singleton<T>` pattern for services (HistoryService, LanguageService)
- Local fields/properties in each Window/UserControl
- Event handlers for all user interactions

### 2.2 Target Architecture: MVUX + Uno Extensions

**Decision: Migrate to MVUX (Model-View-Update-eXtended)**

Rationale:
- The app's current pattern is already "model + view" — there are Services and Models but no ViewModels
- MVUX's `IFeed<T>` / `IState<T>` map well to the app's async OCR operations and reactive settings
- MVUX's immutable record patterns suit the existing `HistoryInfo`, `LookupItem`, `OcrOutput` models
- Code generation reduces boilerplate vs hand-rolling INotifyPropertyChanged

**Target stack:**
- `Uno.Sdk` Single Project
- `Uno.Extensions.Hosting` (DI, configuration, logging)
- `Uno.Extensions.Navigation` (region-based navigation replaces multi-window)
- `Uno.Material` theme (replaces WPF-UI Fluent)
- `Uno.Toolkit` (NavigationBar, TabBar, SafeArea, AutoLayout, etc.)
- MVUX pattern with `IFeed<T>`, `IState<T>`, `IListState<T>`

### 2.3 Multi-Window to Single-Window Navigation

**This is the most fundamental architectural change.**

The WPF app opens 15+ independent windows. Uno Platform uses a single-window model with page navigation.

| WPF Window | Uno Equivalent |
|---|---|
| `EditTextWindow` (main) | `EditTextPage` — primary page |
| `FullscreenGrab` (fullscreen overlay) | **Platform-specific** — see Section 6.1 |
| `GrabFrame` (floating overlay) | `GrabFramePage` — with overlay mode |
| `QuickSimpleLookup` | `QuickLookupPage` |
| `SettingsWindow` + 6 pages | `SettingsPage` with NavigationView sub-pages |
| `FirstRunWindow` | `FirstRunPage` (shown once, then navigates away) |
| `FindAndReplaceWindow` | `FindReplaceDialog` (ContentDialog or flyout) |
| `AddOrRemoveWindow` | `AddRemoveDialog` (ContentDialog) |
| `QrCodeWindow` | `QrCodeDialog` (ContentDialog) |
| `BottomBarSettings` | `BottomBarSettingsDialog` (ContentDialog) |
| `PostGrabActionEditor` | `PostGrabActionDialog` (ContentDialog) |
| `RegexManager` | `RegexManagerDialog` (ContentDialog) |
| `RegexEditorDialog` | `RegexEditorDialog` (ContentDialog) |
| `PreviousGrabWindow` | Overlay element (not a separate page) |
| `NotifyIconWindow` | **Dropped** (system tray not cross-platform) |

**Navigation structure:**

```
Shell (NavigationView)
├── EditTextPage (default)
├── GrabFramePage
├── QuickLookupPage
├── SettingsPage
│   ├── GeneralSettings
│   ├── FullscreenGrabSettings
│   ├── LanguageSettings
│   ├── KeysSettings
│   ├── TesseractSettings
│   └── DangerSettings
└── FirstRunPage (conditional, first launch only)
```

---

## 3. Platform Dependency Audit

### 3.1 Fully Portable (no changes needed)

| File/Area | Lines | Notes |
|---|---|---|
| `StringMethods.cs` | 11,046 | Pure string manipulation |
| `CharacterUtilities.cs` | 100 | Unicode analysis |
| `NumericUtilities.cs` | 70 | Math helpers |
| `Json.cs` | 23 | System.Text.Json wrapper |
| `Singleton.cs` | 11 | Generic singleton |
| `StreamWrapper.cs` | 253 | Standard Stream |
| `CalculationService.cs` | 577 | NCalc math engine |
| `ILanguage.cs` | 46 | Interface definition |
| All `Enums.cs` | 101 | Enum definitions |
| Most `Models/` | ~500 | Data models (with minor adaptations) |
| `UndoRedoOperations/` | ~300 | Core undo/redo logic |

**Estimated portable LOC: ~13,000 (52%)**

### 3.2 Requires Abstraction / Adaptation

| File/Area | Issue | Migration Path |
|---|---|---|
| `SettingsService.cs` | `Properties.Settings` + WinRT `ApplicationData` | Replace with `Uno.Extensions.Configuration` + `IWritableOptions<T>` |
| `HistoryService.cs` | `DispatcherTimer`, GDI+ bitmap caching, `NativeMethods.DeleteObject` | Replace with `DispatcherQueue`, `SoftwareBitmap` or `SKBitmap`, file-based storage |
| `LanguageService.cs` | `Windows.Globalization.Language` | Abstract behind `ILanguage` (already exists) + platform-specific implementations |
| `FileUtilities.cs` | Mixed WinRT `StorageFile` + `System.IO` | Standardize on `System.IO` + `FilePicker` from Uno |
| `BarcodeUtilities.cs` | ZXing.Net with `System.Drawing.Bitmap` | Replace with ZXing.Net.Maui or SkiaSharp rendering |
| `PostGrabActionManager.cs` | `Wpf.Ui.Controls.SymbolRegular` | Replace with Uno Material icons |
| `SettingsImportExportUtilities.cs` | `System.Configuration.ConfigurationManager` | Replace with JSON-based settings |
| `IoUtilities.cs` | Some WPF window references | Decouple from UI layer |

### 3.3 Requires Platform-Specific Implementation (Windows-Only or Conditional)

| File/Area | Issue | Migration Path |
|---|---|---|
| `OcrUtilities.cs` (487 lines) | `Windows.Media.Ocr`, multi-engine orchestration | Abstract `IOcrEngine` interface; Windows impl uses WinRT OCR; other platforms use Tesseract or cloud API |
| `WindowsAiUtilities.cs` (610 lines) | `Microsoft.Windows.AI.*` (ARM64 only) | Windows-only feature behind `#if` or capability check; disabled on other platforms |
| `TesseractHelper.cs` (452 lines) | External Tesseract.exe via CliWrap | Portable concept but needs per-platform binary; abstract behind `IOcrEngine` |
| `ImageMethods.cs` (265 lines) | `System.Drawing.Graphics.CopyFromScreen`, GDI+, WPF `BitmapSource` | Replace with SkiaSharp for image manipulation; screen capture is platform-specific |
| `HotKeyManager.cs` (143 lines) | `user32.dll` RegisterHotKey/UnregisterHotKey | Windows-only via `#if WINDOWS`; not available cross-platform |
| `WindowUtilities.cs` (359 lines) | `user32.dll` SendInput, multi-window management | Rewrite for single-window navigation; input injection Windows-only |
| `WindowResizer.cs` (367 lines) | Win32 WM_GETMINMAXINFO, monitor APIs | **Drop** — Uno handles window sizing natively |
| `ClipboardUtilities.cs` (150 lines) | Mixed WPF + WinRT clipboard APIs | Replace with `Windows.ApplicationModel.DataTransfer.Clipboard` (supported by Uno) |
| `NotificationUtilities.cs` (65 lines) | `Microsoft.Toolkit.Uwp.Notifications` | Use Uno's notification APIs or platform-specific impl |
| `NotifyIconUtilities.cs` (147 lines) | System tray icon | **Drop** — system tray not cross-platform |
| `CursorClipper.cs` (53 lines) | `user32.dll` ClipCursor | **Drop** — not cross-platform |
| `DiagnosticsUtilities.cs` (414 lines) | Registry, Dapplo.Windows display info | Rewrite with platform-agnostic system info |
| `RegistryMonitor.cs` (375 lines) | Win32 registry change monitoring | **Drop** — theme changes handled by Uno |
| `SystemThemeUtility.cs` (33 lines) | Registry-based theme detection | Replace with Uno's built-in theme detection |
| `ImplementAppOptions.cs` (75 lines) | Registry + UWP StartupTask | Windows-only `#if WINDOWS` |
| `NativeMethods.cs` + `OSInterop.cs` | P/Invoke declarations | Windows-only `#if WINDOWS`, many become unnecessary |
| `MagickHelpers.cs` (157 lines) | Magick.NET + WPF imaging | Replace with SkiaSharp filters |
| `ColorHelper.cs` (16 lines) | `System.Drawing.Color` ↔ WPF `Color` | Replace with WinUI `Color` conversions |

### 3.4 Features to Drop or Defer

| Feature | Reason |
|---|---|
| System tray icon + background mode | Not cross-platform; revisit per-platform later |
| Global hotkeys (RegisterHotKey) | Windows-only P/Invoke; defer to Windows-specific build |
| Windows JumpList | WPF-only; no Uno equivalent |
| Toast notifications (UWP Toolkit) | Replace with in-app notifications |
| Cursor clipping during drag | Win32-only; not needed with Uno pointer handling |
| Screen capture (Graphics.CopyFromScreen) | Platform-specific; needs per-platform impl |
| Windows AI features (ARM64 only) | Keep Windows-only behind capability flag |
| Startup on login (Registry/StartupTask) | Windows-only; defer |

---

## 4. Design & Styling Migration

### 4.1 Theme System: WPF-UI Fluent -> Uno Material

The app currently uses **WPF-UI** (Wpf.Ui v4.2.0) which provides Fluent Design System controls. For Uno Platform, we'll use **Uno Material** (Material Design 3).

**Color mapping:**

| WPF-UI Resource | Uno Material Equivalent |
|---|---|
| `SystemAccentColor` (#308E98 teal) | `PrimaryColor` (custom teal) |
| `SystemAccentColorPrimary/Secondary/Tertiary` | `PrimaryContainer`, `Secondary`, `Tertiary` |
| `SolidBackgroundFillColorBaseBrush` | `SurfaceBrush` |
| `TextFillColorPrimaryBrush` | `OnSurfaceBrush` |
| `ApplicationBackgroundBrush` | `BackgroundBrush` |
| `DarkTeal` (#18474C) | `PrimaryContainerDark` (custom) |
| `Teal` (#308E98) | `PrimaryDark` (custom) |

**Custom Material palette** (derived from the app's teal identity):

```
Primary: #308E98 (Teal)
OnPrimary: #FFFFFF
PrimaryContainer: #18474C (Dark Teal)
OnPrimaryContainer: #A8E8EF
Secondary: #4DB6C0
Tertiary: #6DCFD8
Error: #BA1A1A (from DangerSettings DarkRed)
Surface: #1C1C1E (dark theme default)
```

### 4.2 Style Migration Map

| WPF Style File | Uno Replacement |
|---|---|
| `Colors.xaml` | Material color override in `App.xaml` via `MaterialTheme` + `ColorPaletteOverride` |
| `TextStyles.xaml` | Material typography (`DisplayLarge`, `HeadlineMedium`, `BodyLarge`, `LabelMedium`) |
| `ButtonStyles.xaml` | Material button styles (`FilledButtonStyle`, `OutlinedButtonStyle`, `TextButtonStyle`) + custom teal overrides |
| `TextBoxStyles.xaml` | Material TextBox styles (`FilledTextBoxStyle`, `OutlinedTextBoxStyle`) |
| `DataGridStyles.xaml` | **No DataGrid in WinUI** — replace with `ListView` + `GridView` or `ItemsRepeater` with grid layout |
| `GridViewStyles.xaml` | Lightweight styling overrides on `ListView` |

### 4.3 Control Mapping

| WPF / WPF-UI Control | Uno Platform Equivalent |
|---|---|
| `ui:FluentWindow` | `Page` (single window model) |
| `ui:TitleBar` | `NavigationBar` (Uno Toolkit) |
| `ui:NavigationView` | `NavigationView` (WinUI) with region-based navigation |
| `ui:NavigationViewItem` | `NavigationViewItem` with `TargetPageType` |
| `ui:SymbolIcon` | `SymbolIcon` (WinUI) or `FontIcon` with Material Symbols |
| `ui:ToggleSwitch` | `ToggleSwitch` (WinUI native) |
| `ui:DropDownButton` | `DropDownButton` (WinUI) |
| `ui:ImageIcon` | `ImageIcon` (WinUI) or `BitmapIcon` |
| `ui:ThemeResource` | `ThemeResource` (built-in WinUI) |
| `Menu` + `MenuItem` | `MenuBar` + `MenuBarItem` + `MenuFlyoutItem` (WinUI) |
| `DataGrid` | `ListView` with `GridView` or `ItemsRepeater` |
| `Canvas` (drawing surface) | `Canvas` (WinUI — same concept) |
| `Hyperlink` | `HyperlinkButton` |
| `CollapsibleButton` (custom) | Custom control or `Button` with `ContentTemplate` + `VisualStateManager` |
| `ZoomBorder` (custom) | `ZoomContentControl` (Uno Toolkit) or `ScrollViewer` with `ZoomMode` |
| `WordBorder` (custom) | Custom UserControl (port with WinUI control APIs) |
| `ShortcutControl` (custom) | Custom UserControl (Windows-only hotkey recording) |

### 4.4 Icon Strategy

WPF-UI uses `SymbolRegular` icons (Fluent UI System Icons). For Uno Material, use **Material Symbols** or keep Fluent icons via the `Uno.Fonts.Fluent` package.

**Recommendation:** Use Material Symbols for consistency with Material theme. Create an icon mapping table during implementation.

---

## 5. Data & State Migration

### 5.1 Settings System

**Current:** `Properties.Settings.Default` (app.config) + `ApplicationDataContainer` hybrid

**Target:** `Uno.Extensions.Configuration` with `IWritableOptions<T>`

```csharp
// New settings model (immutable record for MVUX)
public record AppSettings
{
    public string DefaultLaunch { get; init; } = "EditText";
    public string AppTheme { get; init; } = "System";
    public bool RunInTheBackground { get; init; } = false;
    public bool StartupOnLogin { get; init; } = false;
    public bool ShowToast { get; init; } = true;
    public bool FirstRun { get; init; } = true;
    public string LastUsedLang { get; init; } = "";
    public bool UseTesseract { get; init; } = false;
    public string TesseractPath { get; init; } = "";
    public string FontFamilySetting { get; init; } = "Segoe UI";
    public double FontSizeSetting { get; init; } = 14;
    // ... ~50 more properties from Properties.Settings
}
```

Store in `appsettings.json` with `IWritableOptions<AppSettings>` for runtime mutations.

### 5.2 History System

**Current:** JSON files in AppData + GDI+ bitmap caching with `GetHbitmap`/`DeleteObject`

**Target:**
- JSON files in `ApplicationData.Current.LocalFolder` (cross-platform)
- Image storage as PNG files (no GDI+ dependency)
- `IListState<HistoryInfo>` in MVUX for reactive history display

### 5.3 MVUX Model Design

Each page gets a Model class (not ViewModel — MVUX convention):

```
EditTextModel
├── IState<string> Text
├── IState<string> OpenedFilePath
├── IState<ILanguage> SelectedLanguage
├── IListState<HistoryInfo> RecentHistory
├── IState<bool> IsWordWrap
├── IState<AppSettings> Settings
└── Commands: Save, Open, OcrPaste, FindReplace, etc.

GrabFrameModel
├── IState<ImageSource> BackgroundImage
├── IListState<WordBorderInfo> WordBorders
├── IState<bool> IsFreeze
├── IState<bool> IsTableMode
├── IState<ILanguage> SelectedLanguage
├── IFeed<IEnumerable<ILanguage>> AvailableLanguages
└── Commands: Grab, Clear, Undo, Redo, etc.

QuickLookupModel
├── IListState<LookupItem> Items
├── IState<string> SearchText
├── IState<bool> IsRegexMode
├── IFeed<IEnumerable<ILanguage>> AvailableLanguages
└── Commands: Add, Delete, Save, Parse, Search

SettingsModel
├── IState<AppSettings> Settings
├── IFeed<IEnumerable<ILanguage>> InstalledLanguages
├── IFeed<IEnumerable<TessLang>> TesseractLanguages
└── Commands: ResetSettings, ClearHistory, ExportBugReport, etc.
```

### 5.4 Service Layer (DI)

Register all services in `IHostBuilder`:

```csharp
services.AddSingleton<IOcrService, OcrService>();
services.AddSingleton<IHistoryService, HistoryService>();
services.AddSingleton<ILanguageService, LanguageService>();
services.AddSingleton<ICalculationService, CalculationService>();
services.AddSingleton<IClipboardService, ClipboardService>();
services.AddSingleton<IFileService, FileService>();
services.AddSingleton<IBarcodeService, BarcodeService>();
services.AddSingleton<ISettingsService, SettingsService>();
```

---

## 6. Platform-Specific Features

### 6.1 Screen Capture (Fullscreen Grab)

This is the app's signature feature — a fullscreen overlay for region selection + OCR.

**Windows:** Use `Windows.Graphics.Capture.GraphicsCaptureItem` or `Graphics.CopyFromScreen` behind `#if WINDOWS`.

**Other platforms:** Defer. On mobile, use camera/photo picker as input source instead.

**Architecture:**
```csharp
public interface IScreenCaptureService
{
    Task<SoftwareBitmap?> CaptureScreenAsync();
    Task<SoftwareBitmap?> CaptureRegionAsync(Rect region);
    bool IsSupported { get; }
}

// Windows implementation
#if WINDOWS
public class WindowsScreenCaptureService : IScreenCaptureService { ... }
#endif
```

### 6.2 OCR Engine Abstraction

```csharp
public interface IOcrEngine
{
    string Name { get; }
    bool IsAvailable { get; }
    Task<OcrOutput> RecognizeAsync(SoftwareBitmap image, ILanguage language);
    Task<IEnumerable<ILanguage>> GetAvailableLanguagesAsync();
}

// Implementations:
// - WindowsOcrEngine (Windows.Media.Ocr)
// - TesseractOcrEngine (CLI wrapper — desktop only)
// - WindowsAiOcrEngine (Microsoft.Windows.AI — Windows ARM64 only)
// - Future: CloudOcrEngine (Azure Cognitive Services — all platforms)
```

### 6.3 Platform Feature Matrix

| Feature | Windows | WebAssembly | Android | iOS | macOS |
|---|---|---|---|---|---|
| Edit Text | Yes | Yes | Yes | Yes | Yes |
| Quick Lookup | Yes | Yes | Yes | Yes | Yes |
| Settings | Yes | Yes | Yes | Yes | Yes |
| OCR (Windows Runtime) | Yes | No | No | No | No |
| OCR (Tesseract) | Yes | No* | No* | No* | Yes |
| OCR (Cloud/future) | Yes | Yes | Yes | Yes | Yes |
| Screen Capture | Yes | No | No | No | No |
| Grab Frame (overlay) | Yes | No | No | No | No |
| Camera/Photo OCR | Yes | Yes | Yes | Yes | Yes |
| QR Code Gen | Yes | Yes | Yes | Yes | Yes |
| Global Hotkeys | Yes | No | No | No | No |
| System Tray | No** | No | No | No | No |
| Windows AI | ARM64 | No | No | No | No |
| File Drag-Drop | Yes | Partial | No | No | Yes |
| Clipboard Watch | Yes | No | No | No | No |

*Tesseract could work on desktop platforms with native binaries
**Deferred from initial migration

---

## 7. Binding & Event Handler Migration

### 7.1 Binding Pattern Changes

| WPF Pattern | WinUI/Uno Equivalent |
|---|---|
| `{Binding Property}` | `{x:Bind ViewModel.Property, Mode=OneWay}` |
| `{Binding Property, Mode=TwoWay}` | `{x:Bind ViewModel.Property, Mode=TwoWay}` |
| `{DynamicResource BrushName}` | `{ThemeResource BrushName}` |
| `{StaticResource StyleName}` | `{StaticResource StyleName}` (same) |
| `Binding ElementName=X, Path=IsChecked` | `{x:Bind X.IsChecked, Mode=TwoWay}` |
| `DataContext` implicit binding | `x:Bind` with explicit path from MVUX-generated ViewModel |
| `ItemsSource="{Binding List}"` | `ItemsSource="{x:Bind ViewModel.List}"` via MVUX feed |

### 7.2 Event Handler to Command Migration

Code-behind event handlers migrate to MVUX commands:

```csharp
// WPF code-behind (current)
private void SaveBTN_Click(object sender, RoutedEventArgs e)
{
    File.WriteAllText(path, text);
    DefaultSettings.Save();
}

// MVUX model (target)
public partial record EditTextModel
{
    public IState<string> Text => State<string>.Value(this, () => "");

    public async ValueTask Save(CancellationToken ct)
    {
        var text = await Text;
        await _fileService.SaveTextAsync(path, text);
    }
}
```

### 7.3 RoutedCommand Migration

EditTextWindow defines 20+ `RoutedCommand` objects. These become MVUX commands:

| WPF RoutedCommand | MVUX Command |
|---|---|
| `OcrPasteCommand` | `OcrPaste()` method on Model |
| `IsolateSelectionCmd` | `IsolateSelection()` method |
| `SingleLineCmd` | `MakeSingleLine()` method |
| `ToggleCaseCmd` | `ToggleCase()` method |
| `WebSearchCmd` | `WebSearch()` method |
| etc. | etc. |

Keyboard shortcuts via `KeyboardAccelerator` on `MenuFlyoutItem`:

```xml
<MenuFlyoutItem Text="Save" Command="{x:Bind ViewModel.Save}">
    <MenuFlyoutItem.KeyboardAccelerators>
        <KeyboardAccelerator Key="S" Modifiers="Control" />
    </MenuFlyoutItem.KeyboardAccelerators>
</MenuFlyoutItem>
```

---

## 8. Custom Control Migration

### 8.1 WordBorder (HIGHEST COMPLEXITY)

The most complex custom control — an editable OCR word overlay with:
- Selection states (IsEditing, IsSelected, WasRegionSelected)
- Move/resize with 4 drag handles
- Dynamic foreground color (luma-based contrast)
- Context menu with translation, search, copy
- Undo support

**Migration:**
- Port as custom `UserControl` in Uno
- Replace `Viewbox` text scaling with `TextBlock` in `ViewBox`
- Replace `DispatcherTimer` with `DispatcherQueue.CreateTimer()`
- Replace color math (stays portable)
- Translation feature: Windows-only behind capability check

### 8.2 ZoomBorder -> ZoomContentControl

Replace entirely with `ZoomContentControl` from Uno Toolkit:

```xml
<utu:ZoomContentControl
    MinZoomLevel="0.1"
    MaxZoomLevel="10"
    IsActive="True">
    <Canvas x:Name="RectanglesCanvas" />
</utu:ZoomContentControl>
```

### 8.3 CollapsibleButton

Simple port — replace WPF `DependencyProperty` with WinUI `DependencyProperty` (same API in Uno). Replace `Wpf.Ui.Controls.SymbolIcon` with WinUI `SymbolIcon` or `FontIcon`.

### 8.4 ShortcutControl

Windows-only control (keyboard hook recording). Wrap in `#if WINDOWS` and provide a disabled/hidden state on other platforms.

### 8.5 FindAndReplaceWindow -> ContentDialog

Convert from standalone window to `ContentDialog`:

```xml
<ContentDialog
    x:Name="FindReplaceDialog"
    Title="Find and Replace"
    PrimaryButtonText="Replace All"
    SecondaryButtonText="Close">
    <!-- Existing content layout migrated here -->
</ContentDialog>
```

---

## 9. NuGet Package Migration

| WPF Package | Uno Replacement | Notes |
|---|---|---|
| `WPF-UI` v4.2.0 | `Uno.Material` + `Uno.Toolkit.UI` | Full theme + control replacement |
| `WPF-UI.Tray` v4.2.0 | **Drop** | No cross-platform tray |
| `Dapplo.Windows.User32` | **Drop** | Display info from Uno APIs |
| `Microsoft.Toolkit.Uwp.Notifications` | In-app notification or platform-specific | Toast replacement |
| `Microsoft.WindowsAppSDK.AI` | Keep (Windows-only, `#if WINDOWS`) | ARM64 AI features |
| `Microsoft.WindowsAppSDK.Foundation/Runtime/WinUI` | Provided by Uno.Sdk | Already included |
| `Magick.NET-*` | `SkiaSharp` | Image manipulation |
| `ZXing.Net` + Bindings | `ZXing.Net.Maui` or keep with SkiaSharp adapter | QR code gen/read |
| `NCalcAsync` | Keep as-is | Pure .NET, fully portable |
| `Humanizer.Core` | Keep as-is | Pure .NET, fully portable |
| `CliWrap` | Keep as-is (desktop only) | Tesseract CLI wrapper |

---

## 10. Implementation Phases

### Phase 1: Foundation (Scaffold + Core)

**Goal:** Buildable Uno project with shared infrastructure

1. Scaffold new `dotnet new unoapp` with MVUX, Material theme, Navigation
2. Configure `Uno.Sdk` with target platforms: Windows Desktop, WebAssembly
3. Set up `IHostBuilder` with DI, configuration, logging
4. Port `appsettings.json` settings model (replace `Properties.Settings`)
5. Port all portable code:
   - `Enums.cs`
   - `Models/` (convert to records where appropriate)
   - `Interfaces/ILanguage.cs`
   - `StringMethods.cs`
   - `CharacterUtilities.cs`, `NumericUtilities.cs`
   - `CalculationService.cs`
   - `Json.cs`, `Singleton.cs`, `StreamWrapper.cs`
6. Define service interfaces: `IOcrEngine`, `IHistoryService`, `IClipboardService`, `IFileService`, etc.
7. Set up Material theme with custom teal palette
8. Build `Shell.xaml` with NavigationView
9. Build navigation route map
10. Verify build on all target platforms

### Phase 2: Edit Text Window

**Goal:** Primary editing experience functional

1. Create `EditTextModel` (MVUX) with `IState<string>` for text, settings feeds
2. Create `EditTextPage.xaml` — port layout (MenuBar, TextBox, StatusBar)
3. Port menu structure (File, Edit, Selection) with `MenuBar`/`MenuFlyoutItem`
4. Implement keyboard accelerators for all commands
5. Port text manipulation commands (ToggleCase, SingleLine, Unstack, etc.)
6. Implement file open/save with `FilePicker`
7. Port Find & Replace as `ContentDialog`
8. Port Add/Remove characters as `ContentDialog`
9. Port QR Code generator as `ContentDialog`
10. Implement clipboard operations
11. Implement drag-drop (file drop)
12. Port bottom bar / status bar
13. Port CalculationService integration (inline math)

### Phase 3: OCR Infrastructure

**Goal:** OCR working on Windows, abstracted for future platforms

1. Implement `IOcrEngine` interface
2. Port `WindowsOcrEngine` (WinRT OCR) — `#if WINDOWS`
3. Port `TesseractOcrEngine` (CliWrap) — desktop only
4. Port `WindowsAiOcrEngine` — `#if WINDOWS` ARM64
5. Port `LanguageService` with caching
6. Port `OcrUtilities` core logic (output formatting, table reconstruction)
7. Port OCR result models (`OcrOutput`, `OcrLinesWords`, `WinRtOcrLinesWords`, `WinAiOcrLinesWords`)
8. Implement `IOcrService` facade that orchestrates engines
9. Wire OCR paste into EditTextPage

### Phase 4: Grab Frame

**Goal:** Image-based OCR with interactive word selection

1. Create `GrabFrameModel` (MVUX) with image state, word borders, language feeds
2. Create `GrabFramePage.xaml` — port layout (toolbar, Canvas, image)
3. Replace `ZoomBorder` with `ZoomContentControl` (Uno Toolkit)
4. Port `WordBorder` custom control to WinUI UserControl
5. Port Canvas mouse interaction (draw selection, move/resize borders)
6. Port undo/redo system (`UndoRedo`, `AddWordBorder`, `RemoveWordBorder`, etc.)
7. Implement image loading (file picker, drag-drop, clipboard paste)
8. Wire OCR recognition (click-word, region select)
9. Port search functionality (regex + standard)
10. Port barcode reading (ZXing)
11. Implement history save/load

### Phase 5: Quick Simple Lookup

**Goal:** Searchable lookup table functional

1. Create `QuickLookupModel` (MVUX) with `IListState<LookupItem>`, search state
2. Create `QuickLookupPage.xaml` — port layout
3. Replace `DataGrid` with `ItemsRepeater` + `UniformGridLayout`:
   - Column headers as a fixed Grid row above the repeater
   - Each item row: Icon | ShortValue | LongValue cells
   - Inline editing via `TextBox` swap in DataTemplate (tap-to-edit)
   - Selection tracking via `IListState<LookupItem>` selection
4. Port search (standard + regex)
5. Port CSV load/save
6. Port clipboard parsing (tab/comma separated)
7. Port history integration
8. Implement enter-key modes (insert into external context)

### Phase 6: Settings

**Goal:** All settings pages working

1. Create `SettingsPage.xaml` with `NavigationView` sub-navigation
2. Port `GeneralSettings` — theme, launch mode, behaviors
3. Port `FullscreenGrabSettings` — grab mode defaults
4. Port `LanguageSettings` — language management
5. Port `KeysSettings` — keyboard shortcuts (Windows-only features)
6. Port `TesseractSettings` — Tesseract configuration
7. Port `DangerSettings` — reset, export, import

### Phase 7: Platform-Specific & Polish

**Goal:** Windows-specific features, first-run, polish

1. Implement screen capture service (`#if WINDOWS`)
2. Port `FullscreenGrab` as Windows-specific page/overlay
3. Implement GrabFrame detachable window on Windows (`AppWindow`)
4. Port `FirstRunPage` onboarding flow
5. Implement `RegexManager` dialog
6. Implement `BottomBarSettings` dialog
7. Implement `PostGrabActionEditor` dialog
8. Add in-app notification system (replace toast)
9. Image preprocessing (replace Magick.NET with SkiaSharp)
10. History service with image storage
11. Theme switching (Light/Dark/System) with hybrid teal Material palette
12. Settings migration from WPF `Properties.Settings` on Windows first launch
13. Accessibility pass
14. Performance optimization (especially Wasm payload)

### Phase 8: Tests & Stabilization

1. Port portable unit tests (StringMethodTests, CalculatorTests, LanguageTests, ExtractedPatternTests, etc.)
2. Write MVUX model tests (EditTextModel, GrabFrameModel, QuickLookupModel)
3. Write navigation integration tests
4. Wasm-specific testing (clipboard, file picker, localStorage history)
5. Windows-specific testing (OCR engines, screen capture, hotkeys)

### Phase 9: Future (Post-v1)

1. Camera-based OCR input for mobile (Android/iOS)
2. Cloud OCR integration for Wasm (Azure AI Vision)
3. macOS desktop target
4. PWA support for Wasm (service worker, offline mode)
5. System tray support on Windows (if Uno adds support)
6. Mobile-responsive layouts

---

## 11. Risk Register

| Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|
| Screen capture not portable | High | Certain | Design as Windows-first; camera fallback on mobile |
| DataGrid missing in WinUI | Medium | Certain | Use ListView+GridView or CommunityToolkit DataGrid |
| WPF-UI styles don't map 1:1 to Material | Medium | High | Accept visual differences; Material is the new identity |
| 11,000-line StringMethods.cs has hidden WPF deps | Low | Low | Already verified portable — pure regex/string ops |
| OCR engine availability varies by platform | High | Certain | IOcrEngine abstraction with graceful degradation |
| GrabFrame Canvas interactions complex to port | Medium | Medium | Canvas API is similar in WinUI; WordBorder is main risk |
| Undo/Redo system tied to WPF controls | Medium | Medium | Core logic portable; UI bindings need adaptation |
| Performance regression on Wasm/mobile | Medium | Medium | Profile early; defer heavy features to desktop |

---

## 12. Decisions (Finalized)

| # | Question | Decision |
|---|---|---|
| 1 | Target platforms | **Desktop (Windows) + WebAssembly** — no mobile for v1 |
| 2 | OCR on non-Windows | **Windows-only for v1** — OCR features disabled/hidden on Wasm; Wasm gets Edit Text + Quick Lookup |
| 3 | DataGrid replacement | **ItemsRepeater with UniformGridLayout** — inline editing via TextBox swap in DataTemplate; full control over selection; portable across Windows + Wasm |
| 4 | Screen capture | **Windows-only** — FullscreenGrab stays `#if WINDOWS`; Wasm users load images via file picker |
| 5 | Visual identity | **Hybrid** — Material Design 3 foundation with the original teal (#308E98) as Primary color and dark theme as default. Preserve the app's character while gaining Material's polish |
| 6 | Settings migration | **Migrate** — on first launch, detect existing `Properties.Settings` and import into new `appsettings.json` format. One-time migration, then new format going forward |
| 7 | Test strategy | **Port portable tests + write new** — port StringMethodTests, CalculatorTests, LanguageTests, ExtractedPatternTests (pure logic). Write new tests for MVUX models and navigation. Skip WPF-specific tests (ScreenLayout, WindowsAi) — rewrite later against new abstractions |
| 8 | Naming | **Keep "Text-Grab"** |
| 9 | Windows AI | **Windows-exclusive** — keep behind `#if WINDOWS` + ARM64 capability check. No cloud alternative for v1 |
| 10 | Multi-window | **Single-window primary + detachable GrabFrame on Windows** — NavigationView-based single window is the default. On Windows desktop, GrabFrame can pop out into a companion `AppWindow` for side-by-side OCR + editing workflow |

### Platform Feature Scope (v1)

| Feature | Windows Desktop | WebAssembly |
|---|---|---|
| Edit Text (full) | Yes | Yes |
| Quick Lookup (full) | Yes | Yes |
| Settings | Yes | Yes (minus Windows-only toggles) |
| First Run | Yes | Yes |
| OCR (Windows Runtime) | Yes | No |
| OCR (Tesseract) | Yes | No |
| OCR (Windows AI) | ARM64 only | No |
| Screen Capture | Yes | No |
| Fullscreen Grab | Yes | No |
| Grab Frame (overlay) | Yes (detachable) | Image-only mode (file picker) |
| QR Code Generation | Yes | Yes |
| File Drag-Drop | Yes | Partial (browser support) |
| Clipboard Operations | Yes | Partial (browser clipboard API) |
| Global Hotkeys | Yes (`#if WINDOWS`) | No |
| Find & Replace | Yes | Yes |
| Regex Manager | Yes | Yes |
| Calculation Service | Yes | Yes |
| History | Yes | Yes (localStorage) |
| Settings Migration | Yes (from WPF) | N/A (fresh install) |

---

## Appendix A: WPF-to-WinUI/Uno XAML Reference

> This appendix serves as the seed for the reusable **WPF-to-Uno migration skill**.

### A.1 XAML Namespace Map

| WPF Namespace | WinUI/Uno Equivalent | Notes |
|---|---|---|
| `xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"` | Same URI (unchanged) | WinUI reuses the same default namespace |
| `xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"` | Same URI (unchanged) | x: directives unchanged |
| `clr-namespace:MyApp` | `using:MyApp` | CLR namespace syntax changes to `using:` |
| `xmlns:sys="clr-namespace:System;assembly=mscorlib"` | `xmlns:sys="using:System"` | No assembly qualifier in WinUI |
| `xmlns:local="clr-namespace:Text_Grab"` | `xmlns:local="using:Text_Grab"` | |
| `xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"` | **Remove** — replace with WinUI + Uno controls | WPF-UI library not available |

### A.2 C# Namespace Map (Source: Uno Platform blog + Microsoft Learn)

| WPF / System.Windows | WinUI / Microsoft.UI | Notes |
|---|---|---|
| `System.Windows` | `Microsoft.UI.Xaml` | Root namespace shift |
| `System.Windows.Controls` | `Microsoft.UI.Xaml.Controls` | |
| `System.Windows.Controls.Primitives` | `Microsoft.UI.Xaml.Controls.Primitives` | |
| `System.Windows.Data` | `Microsoft.UI.Xaml.Data` | |
| `System.Windows.Documents` | `Microsoft.UI.Xaml.Documents` | |
| `System.Windows.Input` | `Microsoft.UI.Xaml.Input` | |
| `System.Windows.Media` | `Microsoft.UI.Xaml.Media` | |
| `System.Windows.Media.Imaging` | `Microsoft.UI.Xaml.Media.Imaging` | |
| `System.Windows.Media.Animation` | `Microsoft.UI.Xaml.Media.Animation` | |
| `System.Windows.Navigation` | `Microsoft.UI.Xaml.Navigation` | |
| `System.Windows.Shapes` | `Microsoft.UI.Xaml.Shapes` | |
| `System.Windows.Threading.Dispatcher` | `Microsoft.UI.Dispatching.DispatcherQueue` | API changes — see A.5 |
| `System.Windows.Threading.DispatcherTimer` | `Microsoft.UI.Xaml.DispatcherTimer` | Or `DispatcherQueue.CreateTimer()` |
| `System.Windows.Interop` | **Drop** — not applicable | HwndSource, InteropBitmap gone |
| `System.Windows.Forms` | **Drop** — not applicable | WinForms not available |
| `System.Windows.Markup` | `Microsoft.UI.Xaml.Markup` | |
| `System.Drawing` | `SkiaSharp` or WinUI imaging | No GDI+ on non-Windows |

### A.3 Control Map (WPF -> WinUI/Uno)

#### Direct 1:1 Replacements

| WPF Control | WinUI Control | Property Changes |
|---|---|---|
| `Window` | `Page` (single-window) or `Window` (desktop) | Remove `ResizeMode`, `WindowStyle`, `WindowState` |
| `TextBlock` | `TextBlock` | Same |
| `TextBox` | `TextBox` | `AcceptsReturn` → same; remove `AcceptsTab`* |
| `Button` | `Button` | Same |
| `ToggleButton` | `ToggleButton` | Same |
| `CheckBox` | `CheckBox` | Same |
| `RadioButton` | `RadioButton` | Same |
| `ComboBox` | `ComboBox` | Same |
| `Image` | `Image` | `Source` type changes (no `BitmapImage` from path) |
| `Border` | `Border` | Same |
| `Grid` | `Grid` | Same |
| `StackPanel` | `StackPanel` | Same |
| `Canvas` | `Canvas` | Same |
| `ScrollViewer` | `ScrollViewer` | Same |
| `Slider` | `Slider` | Same |
| `ProgressBar` | `ProgressBar` | Same |
| `ToolTip` | `ToolTip` | Same |
| `Viewbox` | `Viewbox` | Same |
| `ContentDialog` | `ContentDialog` | Must set `XamlRoot` property |
| `ListView` | `ListView` | Same |
| `ToggleSwitch` | `ToggleSwitch` | Same (WinUI native, no WPF-UI needed) |

*`AcceptsTab` exists in WinUI TextBox

#### Controls Requiring Replacement

| WPF Control | Uno Replacement | Notes |
|---|---|---|
| `Menu` + `MenuItem` | `MenuBar` + `MenuBarItem` + `MenuFlyoutItem` | Complete restructure — see A.7 |
| `DataGrid` | `ItemsRepeater` + `UniformGridLayout` | No built-in DataGrid in WinUI |
| `Hyperlink` (in docs) | `HyperlinkButton` | Different control entirely |
| `StatusBar` | Custom `Grid` or `CommandBar` | No StatusBar in WinUI |
| `Label` | `TextBlock` | WPF Label wraps TextBlock |
| `GroupBox` | `Expander` or custom `Border`+`Header` | No GroupBox in WinUI |
| `Expander` (WPF) | `Expander` (WinUI) | API differs slightly |
| `RichTextBox` | `RichEditBox` | Different API surface |
| `ContextMenu` | `MenuFlyout` | Attach via `ContextFlyout` property |

#### WPF-UI Controls -> Uno Toolkit/Material

| WPF-UI Control | Uno Equivalent | Notes |
|---|---|---|
| `ui:FluentWindow` | `Page` | Window → Page in single-window model |
| `ui:TitleBar` | `NavigationBar` (Uno Toolkit) | |
| `ui:NavigationView` | `NavigationView` (WinUI native) | |
| `ui:NavigationViewItem` | `NavigationViewItem` (WinUI native) | |
| `ui:SymbolIcon` | `SymbolIcon` or `FontIcon` (WinUI) | Map Fluent icons to Material Symbols |
| `ui:ToggleSwitch` | `ToggleSwitch` (WinUI native) | |
| `ui:DropDownButton` | `DropDownButton` (WinUI) | |
| `ui:ImageIcon` | `ImageIcon` (WinUI) | |
| `ui:ThemeResource` | `ThemeResource` (built-in) | |
| `ui:ThemesDictionary` | `MaterialTheme` (Uno Material) | |
| `ui:ControlsDictionary` | `MaterialTheme` (Uno Material) | |

#### Controls with No WinUI Equivalent (Drop or Custom)

| WPF Control | Action |
|---|---|
| `NotifyIcon` / system tray | Drop (not cross-platform) |
| `WindowChrome` | Drop (Uno handles chrome) |
| `DataGrid` (with full editing) | Build with `ItemsRepeater` |
| `StatusBar` | Build custom |
| `JumpList.JumpList` | Drop (Windows shell-only) |

### A.3b Property & Event Migration (from Uno Platform blog)

#### Property Differences

| WPF Property/Value | WinUI Equivalent | Notes |
|---|---|---|
| `Visibility.Hidden` | **Not available** | Use `Opacity="0"` for invisible-but-layout-occupying |
| `TextWrapping.WrapWithOverflow` | `TextWrapping.Wrap` | WinUI doesn't distinguish the two |
| `Focusable="True"` | `IsTabStop="True"` | Different property name, same behavior |

#### Event Migration (Mouse → Pointer Model)

| WPF Event | WinUI Event | Notes |
|---|---|---|
| `MouseLeftButtonDown` | `PointerPressed` | Pointer model replaces mouse model |
| `MouseLeftButtonUp` | `PointerReleased` | |
| `MouseRightButtonDown` | `RightTapped` | |
| `MouseEnter` | `PointerEntered` | |
| `MouseLeave` | `PointerExited` | |
| `MouseMove` | `PointerMoved` | |
| `MouseWheel` / `PreviewMouseWheel` | `PointerWheelChanged` | |
| `PreviewMouseDown` | `PointerPressed` | WinUI has NO tunneling/preview events |
| `PreviewKeyDown` | `KeyDown` | No Preview equivalent; handle in KeyDown |
| `MouseEventArgs` | `PointerRoutedEventArgs` | Different event args type |
| `KeyEventArgs` | `KeyRoutedEventArgs` | Different event args type |

> **Critical for Text-Grab:** FullscreenGrab and GrabFrame use `MouseDown`/`MouseMove`/`MouseUp` extensively on Canvas. All become `PointerPressed`/`PointerMoved`/`PointerReleased`. The `PreviewMouseWheel` on GrabFrame's ZoomBorder → `PointerWheelChanged`.

#### Common Using Pitfalls

| WPF Code | WinUI Replacement | Why It Breaks |
|---|---|---|
| `Application.Current.Dispatcher` | `App.Window.DispatcherQueue` | `Dispatcher` does not exist; use `DispatcherQueue.TryEnqueue()` |
| `Window.Current` | Custom `App.Window` static | Not supported in Windows App SDK |
| `Clipboard` in `System.Windows` | `Windows.ApplicationModel.DataTransfer.Clipboard` | Different API surface |
| `MessageBox.Show()` | `ContentDialog` with `XamlRoot` | No `MessageBox` in WinUI |

### A.3c Patterns With No WinUI Equivalent (Require Architectural Rework)

#### DataTriggers and Style.Triggers → VisualStateManager

```xml
<!-- WPF -->
<Style TargetType="Border">
  <Style.Triggers>
    <DataTrigger Binding="{Binding IsActive}" Value="True">
      <Setter Property="Background" Value="Green" />
    </DataTrigger>
  </Style.Triggers>
</Style>

<!-- WinUI / Uno — use StateTrigger -->
<Border x:Name="MyBorder">
  <VisualStateManager.VisualStateGroups>
    <VisualStateGroup>
      <VisualState x:Name="Active">
        <VisualState.StateTriggers>
          <StateTrigger IsActive="{x:Bind ViewModel.IsActive, Mode=OneWay}" />
        </VisualState.StateTriggers>
        <VisualState.Setters>
          <Setter Target="MyBorder.Background" Value="Green" />
        </VisualState.Setters>
      </VisualState>
    </VisualStateGroup>
  </VisualStateManager.VisualStateGroups>
</Border>
```

#### MultiBinding → x:Bind Function Binding

```xml
<!-- WinUI / Uno — function binding replaces MultiBinding -->
<TextBlock Text="{x:Bind local:Converters.FormatFullName(ViewModel.FirstName, ViewModel.LastName), Mode=OneWay}" />
```
```csharp
public static class Converters
{
    public static string FormatFullName(string first, string last) => $"{first} {last}";
}
```

#### RoutedUICommand → ICommand / MVUX Commands

WinUI does not support routed commands. For MVUX: just define methods on the Model. For MVVM: use `RelayCommand` from CommunityToolkit.Mvvm. WinUI also provides `StandardUICommand` and `XamlUICommand` for built-in commands (Cut, Copy, Paste, Delete).

#### Adorners → Popup / Canvas Overlay

| Adorner Use Case | WinUI Replacement |
|---|---|
| Validation indicators | `TeachingTip`, `InfoBar`, or InputValidation templates |
| Resize handles | `Popup` positioned relative to target |
| Drag preview | `DragItemsStarting` event with custom DragUI |
| Overlay decorations | `Canvas` overlay or `Popup` layer |
| Watermark / Placeholder | `TextBox.PlaceholderText` (built-in) |

> **Text-Grab impact:** WordBorder's resize handles (4 drag borders) currently rely on WPF layout tricks. In WinUI, implement with absolutely-positioned Border elements on a Canvas, or use Popup.

### A.3d Resource Dictionary Rules

```xml
<!-- WinUI / Uno — App.xaml resource structure -->
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <!-- REQUIRED: default styles — must be FIRST -->
      <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
      <!-- For Uno Material -->
      <MaterialTheme xmlns="using:Uno.Material" />
      <!-- Custom resources -->
      <ResourceDictionary Source="ms-appx:///Styles/Colors.xaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

Key rules:
- `XamlControlsResources` (or `MaterialTheme`) must be the **first** merged dictionary
- Resource paths use `ms-appx:///` protocol instead of WPF relative paths (`/Styles/Colors.xaml` → `ms-appx:///Styles/Colors.xaml`)
- `Window.Resources` does not exist in WinUI — place on root layout container or on a `Page`
- Always use `BasedOn` when overriding default styles: `BasedOn="{StaticResource DefaultButtonStyle}"`

### A.3e Implicit Style Warning

**In WinUI 3, always use `BasedOn` when overriding default control styles.** Without it, your style **replaces** the entire default style rather than extending it.

```xml
<!-- WRONG — loses all default button visual states -->
<Style TargetType="Button">
  <Setter Property="Background" Value="Red" />
</Style>

<!-- CORRECT — extends default style -->
<Style TargetType="Button" BasedOn="{StaticResource DefaultButtonStyle}">
  <Setter Property="Background" Value="Red" />
</Style>
```

### A.4 Binding & Resource Syntax

| WPF | WinUI/Uno | Notes |
|---|---|---|
| `{Binding Property}` | `{x:Bind ViewModel.Property, Mode=OneWay}` | `x:Bind` is compiled, faster; default mode is `OneTime` |
| `{Binding Property, Mode=TwoWay}` | `{x:Bind ViewModel.Property, Mode=TwoWay}` | Must specify Mode explicitly |
| `{Binding ElementName=X, Path=Y}` | `{x:Bind X.Y, Mode=OneWay}` | Direct element reference |
| `{Binding RelativeSource={RelativeSource Self}}` | `{x:Bind}` (in control context) | Simpler with x:Bind |
| `{DynamicResource Brush}` | `{ThemeResource Brush}` | ThemeResource is WinUI's dynamic equivalent |
| `{StaticResource Style}` | `{StaticResource Style}` | Same |
| `DataContext="{Binding}"` | Implicit via MVUX code generation | MVUX generates ViewModel binding context |
| `StringFormat='{}{0:N2}'` | Use `x:Bind` with converter function | No StringFormat in x:Bind — use functions instead |
| `TargetType="{x:Type Button}"` | `TargetType="Button"` | No `{x:Type}` in WinUI — use string type name |
| `DataTrigger` | `VisualStateManager` or `x:Bind` with converter | No triggers in WinUI — use VSM |
| `EventTrigger` | `Storyboard` in `VisualState` | |
| `Style.Triggers` | `VisualStateManager` | All triggers → VSM |

### A.5 Key API Migration Patterns

#### Dispatcher

```csharp
// WPF
Application.Current.Dispatcher.Invoke(() => { ... });
Application.Current.Dispatcher.BeginInvoke(() => { ... });

// WinUI / Uno
DispatcherQueue.GetForCurrentThread().TryEnqueue(() => { ... });
// Or from any context:
_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () => { ... });
```

#### DispatcherTimer

```csharp
// WPF
var timer = new System.Windows.Threading.DispatcherTimer();
timer.Interval = TimeSpan.FromMilliseconds(500);
timer.Tick += Timer_Tick;

// WinUI / Uno
var timer = new Microsoft.UI.Xaml.DispatcherTimer();
timer.Interval = TimeSpan.FromMilliseconds(500);
timer.Tick += Timer_Tick;
// Same API, different namespace
```

#### Window.Current → App.Window

```csharp
// WPF
var width = Application.Current.MainWindow.Width;

// WinUI / Uno — define in App.xaml.cs:
public static Window Window { get; private set; }
// Then use: App.Window
```

#### ContentDialog (must set XamlRoot)

```csharp
// WPF
var dialog = new MyDialog();
dialog.ShowDialog(); // modal

// WinUI / Uno
var dialog = new ContentDialog();
dialog.XamlRoot = this.Content.XamlRoot; // REQUIRED
await dialog.ShowAsync();
```

#### Clipboard

```csharp
// WPF
System.Windows.Clipboard.SetDataObject(text, true);
var text = System.Windows.Clipboard.GetText();

// WinUI / Uno
var package = new DataPackage();
package.SetText(text);
Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);

var content = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
var text = await content.GetTextAsync();
```

#### File Pickers

```csharp
// WPF
var dlg = new Microsoft.Win32.OpenFileDialog();
dlg.Filter = "Text Files|*.txt";
if (dlg.ShowDialog() == true) { var path = dlg.FileName; }

// WinUI / Uno
var picker = new FileOpenPicker();
picker.FileTypeFilter.Add(".txt");
// On Windows, must initialize with window handle:
WinRT.Interop.InitializeWithWindow.Initialize(picker, App.Window.GetWindowHandle());
var file = await picker.PickSingleFileAsync();
```

#### RoutedCommand → MVUX Command

```csharp
// WPF
public static RoutedCommand SaveCmd = new();
// In constructor: CommandBindings.Add(new CommandBinding(SaveCmd, SaveExecuted));
private void SaveExecuted(object sender, ExecutedRoutedEventArgs e) { ... }

// MVUX — just define a method on the Model:
public partial record EditTextModel
{
    public async ValueTask Save(CancellationToken ct)
    {
        var text = await Text;
        await _fileService.SaveTextAsync(_path, text);
    }
}
// XAML: <Button Command="{x:Bind ViewModel.Save}" />
```

#### Properties.Settings → IWritableOptions

```csharp
// WPF
Properties.Settings.Default.FontSize = 14;
Properties.Settings.Default.Save();

// Uno Extensions
public partial record SettingsModel(IWritableOptions<AppSettings> Settings)
{
    public async ValueTask UpdateFontSize(double size, CancellationToken ct)
    {
        await Settings.UpdateAsync(s => s with { FontSize = size });
    }
}
```

#### Singleton<T> → DI Registration

```csharp
// WPF
var history = Singleton<HistoryService>.Instance;

// Uno Extensions — register in IHostBuilder:
services.AddSingleton<IHistoryService, HistoryService>();
// Inject via constructor:
public partial record EditTextModel(IHistoryService History) { ... }
```

### A.6 Style & Trigger Migration

```xml
<!-- WPF: Style with Trigger -->
<Style TargetType="Button">
    <Setter Property="Background" Value="Gray" />
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="Teal" />
        </Trigger>
    </Style.Triggers>
</Style>

<!-- WinUI/Uno: VisualStateManager in ControlTemplate -->
<Style TargetType="Button">
    <Setter Property="Background" Value="Gray" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border Background="{TemplateBinding Background}">
                    <VisualStateManager.VisualStateGroups>
                        <VisualStateGroup x:Name="CommonStates">
                            <VisualState x:Name="PointerOver">
                                <VisualState.Setters>
                                    <Setter Target="Root.Background" Value="Teal" />
                                </VisualState.Setters>
                            </VisualState>
                        </VisualStateGroup>
                    </VisualStateManager.VisualStateGroups>
                    <ContentPresenter />
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

### A.7 Menu Migration

```xml
<!-- WPF -->
<Menu>
    <MenuItem Header="_File">
        <MenuItem Header="_Save" Click="Save_Click" InputGestureText="Ctrl+S" />
        <Separator />
        <MenuItem Header="E_xit" Click="Exit_Click" />
    </MenuItem>
</Menu>

<!-- WinUI / Uno -->
<MenuBar>
    <MenuBarItem Title="File">
        <MenuFlyoutItem Text="Save" Click="Save_Click">
            <MenuFlyoutItem.KeyboardAccelerators>
                <KeyboardAccelerator Key="S" Modifiers="Control" />
            </MenuFlyoutItem.KeyboardAccelerators>
        </MenuFlyoutItem>
        <MenuFlyoutSeparator />
        <MenuFlyoutItem Text="Exit" Click="Exit_Click" />
    </MenuBarItem>
</MenuBar>
```

Key differences:
- `MenuItem` → `MenuFlyoutItem`
- `Header` → `Text` (on MenuFlyoutItem) or `Title` (on MenuBarItem)
- `InputGestureText` → `KeyboardAccelerators` (functional, not just display)
- `Separator` → `MenuFlyoutSeparator`
- Checkable: `IsCheckable="True"` property on `ToggleMenuFlyoutItem`
- `ContextMenu` → `MenuFlyout` assigned to `ContextFlyout` property

### A.8 Text-Grab Specific Mappings (Project Log)

> Track every mapping encountered during implementation here. This grows into the skill's pattern database.

| WPF Pattern (Text-Grab) | Uno Equivalent | File | Notes |
|---|---|---|---|
| `ui:FluentWindow` base class | `Page` with `NavigationBar` | All Views | Multi-window → single-window |
| `Properties.Settings.Default` | `IWritableOptions<AppSettings>` | SettingsService | One-time migration on first launch |
| `Singleton<HistoryService>.Instance` | DI `IHistoryService` | Throughout | Constructor injection |
| `WPF DispatcherTimer` (6 instances) | `Microsoft.UI.Xaml.DispatcherTimer` | GrabFrame, EditText, FindReplace, QrCode | Namespace change only |
| `Application.Current.Dispatcher.Invoke` | `DispatcherQueue.TryEnqueue` | EditText clipboard watching | Different API |
| `ZoomBorder` (custom Border) | `ZoomContentControl` (Uno Toolkit) | GrabFrame | Drop-in replacement |
| `NativeMethods.DeleteObject` (GDI+) | SkiaSharp bitmap lifecycle | HistoryService, QrCode | No GDI+ on non-Windows |
| `Graphics.CopyFromScreen` | `IScreenCaptureService` (`#if WINDOWS`) | ImageMethods | Platform abstraction |
| `RegisterHotKey` / `UnregisterHotKey` | `#if WINDOWS` only | HotKeyManager | No cross-platform equivalent |
| 20+ `RoutedCommand` definitions | MVUX model methods | EditTextWindow | Commands become methods |
| `Microsoft.Win32.OpenFileDialog` | `FileOpenPicker` + HWND init | EditText, GrabFrame | Different API |
| `DataGrid` with inline editing | `ItemsRepeater` + tap-to-edit | QuickSimpleLookup | Full redesign |
| `System.Drawing.Bitmap` | `SoftwareBitmap` or `SKBitmap` | ImageMethods, HistoryService | Platform-dependent |
| `ObservableCollection<ButtonInfo>` | `IListState<ButtonInfo>` (MVUX) | BottomBarSettings | Reactive collection |
| WPF-UI `SymbolRegular` icons | Material Symbols or Fluent icons | Throughout | Icon mapping needed |
| `RegistryMonitor` (theme watch) | Uno theme change detection | SystemThemeUtility | Built-in with Uno |

---

## Appendix B: Complete Find-and-Replace Reference

> Copy-paste ready for batch migration. Apply in order: XAML attributes first, then code-behind.

### B.1 XAML Attribute Replacements

| Find | Replace With | Context |
|---|---|---|
| `ContextMenu=` | `ContextFlyout=` | On any UIElement |
| `{DynamicResource ` | `{ThemeResource ` | Theme-responsive references |
| `{x:Static ` | `{x:Bind ` | Static member references |
| `Visibility="Hidden"` | `Visibility="Collapsed"` | Or use `Opacity="0"` for layout |
| `MouseLeftButtonDown` | `PointerPressed` | Event handlers |
| `MouseLeftButtonUp` | `PointerReleased` | Event handlers |
| `MouseRightButtonDown` | `RightTapped` | Event handlers |
| `MouseEnter` | `PointerEntered` | Event handlers |
| `MouseLeave` | `PointerExited` | Event handlers |
| `MouseMove` | `PointerMoved` | Event handlers |
| `MouseWheel` | `PointerWheelChanged` | Event handlers |
| `PreviewMouseWheel` | `PointerWheelChanged` | No preview events in WinUI |
| `PreviewKeyDown` | `KeyDown` | No preview events in WinUI |
| `PreviewMouseDown` | `PointerPressed` | No preview events in WinUI |
| `Focusable="True"` | `IsTabStop="True"` | Focus behavior |
| `Focusable="False"` | `IsTabStop="False"` | Focus behavior |
| `TextWrapping="WrapWithOverflow"` | `TextWrapping="Wrap"` | TextBlock, TextBox |
| `MediaElement` | `MediaPlayerElement` | Media playback |
| `clr-namespace:` | `using:` | XAML namespace declarations |
| `Source="/Styles/` | `Source="ms-appx:///Styles/` | Resource dictionary paths |

### B.2 Code-Behind Replacements

| Find | Replace With |
|---|---|
| `using System.Windows;` | `using Microsoft.UI.Xaml;` |
| `using System.Windows.Controls;` | `using Microsoft.UI.Xaml.Controls;` |
| `using System.Windows.Media;` | `using Microsoft.UI.Xaml.Media;` |
| `using System.Windows.Media.Imaging;` | `using Microsoft.UI.Xaml.Media.Imaging;` |
| `using System.Windows.Media.Animation;` | `using Microsoft.UI.Xaml.Media.Animation;` |
| `using System.Windows.Data;` | `using Microsoft.UI.Xaml.Data;` |
| `using System.Windows.Input;` | `using Microsoft.UI.Xaml.Input;` |
| `using System.Windows.Shapes;` | `using Microsoft.UI.Xaml.Shapes;` |
| `using System.Windows.Documents;` | `using Microsoft.UI.Xaml.Documents;` |
| `using System.Windows.Markup;` | `using Microsoft.UI.Xaml.Markup;` |
| `using System.Windows.Navigation;` | `using Microsoft.UI.Xaml.Navigation;` |
| `using System.Windows.Automation;` | `using Microsoft.UI.Xaml.Automation;` |
| `using System.Windows.Threading;` | `using Microsoft.UI.Dispatching;` |
| `Dispatcher.Invoke(` | `DispatcherQueue.TryEnqueue(` |
| `Dispatcher.BeginInvoke(` | `DispatcherQueue.TryEnqueue(` |
| `MouseEventArgs` | `PointerRoutedEventArgs` |
| `MouseButtonEventArgs` | `PointerRoutedEventArgs` |
| `MouseWheelEventArgs` | `PointerRoutedEventArgs` |
| `KeyEventArgs` | `KeyRoutedEventArgs` |
| `RoutedUICommand` | Remove; use MVUX commands or `RelayCommand` |
| `CommandBinding` | Remove; bind `ICommand` directly in XAML |

### B.3 NuGet Packages Commonly Needed

| Package | Purpose |
|---|---|
| `CommunityToolkit.WinUI.Controls` | DataGrid, WrapPanel, DockPanel, UniformGrid |
| `CommunityToolkit.Mvvm` | RelayCommand, ObservableObject (if not using MVUX) |
| `Uno.Material` | Material Design 3 theme |
| `Uno.Toolkit.WinUI` | NavigationBar, SafeArea, AutoLayout, ZoomContentControl |
| `SkiaSharp.Views.Uno.WinUI` | Cross-platform image/canvas rendering |

---

## Appendix C: AI Prompt Template for XAML Translation

> Use this prompt with any AI coding assistant to automate the mechanical parts of WPF XAML translation. Review output carefully — triggers, DataGrid columns, and custom controls need manual follow-up.

```
You are a WPF-to-WinUI XAML migration assistant. Translate the following WPF XAML file to WinUI 3 XAML that is compatible with Uno Platform.

Apply ALL of the following rules:

NAMESPACE RULES:
- The default xmlns stays: xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
- Remove any clr-namespace references to System.Windows.* and replace with equivalent Microsoft.UI.Xaml.* or "using:" syntax
- Replace xmlns:local="clr-namespace:MyApp" with xmlns:local="using:MyApp"

RESOURCE RULES:
- Replace all {DynamicResource X} with {ThemeResource X}
- Replace {x:Static X} with {x:Bind X}
- Keep {StaticResource X} as-is
- Resource paths use ms-appx:/// protocol

CONTROL REPLACEMENTS:
- Replace <Menu> with <MenuBar>, <MenuItem> with <MenuBarItem> or <MenuFlyoutItem>
- Replace ContextMenu with ContextFlyout using <MenuFlyout>
- Replace <ToolBar> / <ToolBarTray> with <CommandBar> and <AppBarButton>
- Replace <StatusBar> with a <Grid> at the bottom
- Replace <Label> with <TextBlock>
- Replace <MediaElement> with <MediaPlayerElement>

PROPERTY REPLACEMENTS:
- Replace Visibility="Hidden" with Visibility="Collapsed"
- Replace TextWrapping="WrapWithOverflow" with TextWrapping="Wrap"
- Replace Focusable="True/False" with IsTabStop="True/False"

EVENT REPLACEMENTS:
- Replace Mouse* events with Pointer* equivalents (MouseDown→PointerPressed, MouseMove→PointerMoved, MouseUp→PointerReleased, MouseEnter→PointerEntered, MouseLeave→PointerExited, MouseWheel→PointerWheelChanged)
- Remove all Preview* tunneling events (PreviewMouseDown→PointerPressed, PreviewKeyDown→KeyDown)

TRIGGER REPLACEMENTS:
- Remove all Style.Triggers, DataTrigger, EventTrigger blocks
- Create corresponding VisualStateManager.VisualStateGroups using StateTrigger

BINDING UPGRADES:
- Convert {Binding Path=X} to {x:Bind ViewModel.X, Mode=OneWay} where possible
- Note: x:Bind default mode is OneTime, not OneWay — add Mode explicitly
- For MultiBinding, replace with x:Bind function binding
- Replace {DynamicResource} with {ThemeResource}

STYLE RULES:
- Always use BasedOn when overriding default styles
- TargetType uses string name, not {x:Type}: TargetType="Button" not TargetType="{x:Type Button}"

OUTPUT: Complete translated XAML file with a list of manual follow-up items.

Here is the WPF XAML file to translate:
[PASTE WPF XAML HERE]
```
