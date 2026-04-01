# WPF → Uno Platform Migration Patterns

Comprehensive reference of patterns, gotchas, and decisions discovered during the Text-Grab WPF→Uno migration. Useful for building a reusable migration skill.

---

## Namespace Architecture
- Enums in root `TextGrab` namespace, models in `TextGrab.Models`, services in `TextGrab.Services`, shared portable code in `TextGrab.Shared`
- Added `global using TextGrab;`, `global using TextGrab.Interfaces;`, `global using TextGrab.Shared;` to GlobalUsings.cs to avoid per-file imports

## OCR Engine Abstraction Pattern
- WPF used static utility classes + `Singleton<T>`. Uno uses DI + interface-based engines.
- `IOcrEngine` returns `IOcrLinesWords` (structured with bounding boxes), not just text
- Platform-specific engines registered with `#if WINDOWS` in App.xaml.cs ConfigureServices
- Windows-specific adapter classes in `Platforms/Windows/` with `#if WINDOWS` guards
- `OcrService` facade routes to correct engine based on language kind

## Language Abstraction
- WPF `Windows.Globalization.Language` → `GlobalLang` wrapping `CultureInfo` for cross-platform
- Custom `LanguageLayoutDirection` enum replaces `Windows.Globalization.LanguageLayoutDirection`
- WPF `InputLanguageManager.Current.CurrentInputLanguage` → `CultureInfo.CurrentCulture`
- `ILanguage` interface adds `LayoutDirection` property needed by `IsRightToLeft()` extension

## Settings Migration
- WPF `Properties.Settings.Default` → `IWritableOptions<AppSettings>` via Uno.Extensions.Configuration
- `Section<AppSettings>()` in UseConfiguration registers BOTH `IOptions<AppSettings>` and `IWritableOptions<AppSettings>` — no extra registration needed
- WPF code-behind settings reads/writes → MVUX Model with `IState<T>` per setting + `_settings.UpdateAsync(s => s with { ... })`
- Pattern: initialize toggle states in `OnLoaded` with `_isLoading` guard to prevent Toggled events firing during init
- Must use `global::Uno.Extensions.Configuration.IWritableOptions<T>` because project namespace `TextGrab.Uno` collides with `Uno.Extensions`

## Settings Page Architecture
- WPF SettingsWindow (standalone window with NavigationView) → SettingsPage (nested NavigationView within main Shell)
- Each settings sub-page gets its own Model + Page + route (GeneralSettingsModel/Page, etc.)
- Nested routes: Settings → GeneralSettings (default), FullscreenGrabSettings, LanguageSettings, KeysSettings, TesseractSettings, DangerSettings
- **MUST use `<Frame uen:Region.Attached="true" />` inside the nested NavigationView** (same pattern as main Shell)

---

## NavigationView Region Navigation (CRITICAL)

This was the hardest lesson — caused hours of debugging blank pages.

### What works
```xml
<NavigationView uen:Region.Attached="true">
  <NavigationView.MenuItems>
    <NavigationViewItem uen:Region.Name="EditText" Content="Edit Text" />
    <NavigationViewItem uen:Region.Name="GrabFrame" Content="Grab Frame" />
  </NavigationView.MenuItems>
  <Frame uen:Region.Attached="true" />
</NavigationView>
```

### What does NOT work
```xml
<!-- DO NOT USE — content renders blank inside NavigationView -->
<Grid uen:Region.Attached="true" uen:Region.Navigator="Visibility">
  <Grid uen:Region.Name="EditText" />
  <Grid uen:Region.Name="GrabFrame" />
</Grid>
```

### Route registration
```csharp
routes.Register(
    new RouteMap("", View: views.FindByViewModel<ShellModel>(),
        Nested: [
            new("Shell", View: views.FindByView<ShellPage>(), IsDefault: true,
                Nested: [
                    new("EditText", View: views.FindByViewModel<EditTextModel>(), IsDefault: true),
                    new("GrabFrame", View: views.FindByViewModel<GrabFrameModel>()),
                    // ...
                ]),
        ])
);
```

### Key rules
- **`IsDefault: true`** on the Shell route is REQUIRED for automatic startup navigation
- NavigationViewItems use `uen:Region.Name="RouteName"` to map to nested routes
- Nested NavigationView (e.g., Settings sub-pages) uses the exact same Frame pattern
- `Visibility` navigator only works OUTSIDE of NavigationView (e.g., TabBar, Panel)

---

## XAML Resource Keys in Material Theme (CRITICAL)

Missing ThemeResources cause **silent runtime page failure** — no compile error, just blank page.

### Resources that DO NOT exist in Uno Material
- `CardBackgroundFillColorDefaultBrush`
- `SystemFillColorCautionBrush`
- `SystemFillColorSuccessBrush`
- `ApplicationPageBackgroundThemeBrush`
- Any `*TextBlockStyle` suffix (e.g., `TitleLargeTextBlockStyle`)

### Safe Material brush names
Auto-generated from Color keys in `ColorPaletteOverride.xaml`:
- `BackgroundBrush`, `OnBackgroundBrush`
- `SurfaceBrush`, `OnSurfaceBrush`, `SurfaceVariantBrush`
- `PrimaryBrush`, `OnPrimaryBrush`
- `ErrorBrush`, `OnErrorBrush`
- Prefer omitting `Page.Background` to inherit from parent

### Typography styles
- **Uno Material uses SHORT keys**: `TitleLarge`, `TitleMedium`, `BodyMedium`, `BodySmall`, `LabelLarge`
- **NOT** the WinUI-style long names: ~~`TitleLargeTextBlockStyle`~~, ~~`BodyTextBlockStyle`~~
- Pattern: `{Category}{Size}` — `DisplayLarge`, `HeadlineMedium`, `TitleSmall`, `BodyMedium`, `LabelSmall`
- Wrong key = blank page at runtime, no compile error

---

## WPF-UI Controls → WinUI Equivalents
| WPF-UI | WinUI / Uno |
|--------|-------------|
| `FluentWindow` | Standard `Window` (Uno doesn't need it) |
| `ToggleSwitch` | WinUI `ToggleSwitch` (identical API) |
| `SymbolIcon` with `SymbolRegular` enum | `FontIcon` with Segoe Fluent Icons glyphs |
| `HyperlinkButton` | WinUI `HyperlinkButton` (identical) |
| `MessageBox.ShowDialogAsync()` | `ContentDialog.ShowAsync()` |
| `DataGrid` | `ListView` with custom `ItemTemplate` |
| Standalone `Window` (dialog) | `ContentDialog` |

## WPF Window → ContentDialog Pattern
- WPF child windows (modal dialogs) become `ContentDialog` instances
- WPF `ShowDialog()` → `ContentDialog.ShowAsync()`
- WPF `DialogResult = true` → `ContentDialogResult.Primary`
- WPF `Owner = this` → `XamlRoot = this.XamlRoot`
- Two-pane list management (available/enabled with Add/Remove/MoveUp/MoveDown) ports directly with `ObservableCollection<T>`

## Platform-Specific UI
- Can't use `#if WINDOWS` in XAML — set `Visibility="Collapsed"` in XAML, show via `#if WINDOWS` in code-behind
- Windows-only features (startup task, global hotkeys, file explorer, Process.Start): `#if WINDOWS` in code-behind
- Non-Windows platforms: show informational text or hide sections entirely

---

## Theme Switching
- WPF `App.SetTheme()` → `global::Uno.Toolkit.UI.SystemThemeHelper.SetApplicationTheme(xamlRoot, ElementTheme)`
- **Must use `global::` prefix** because project namespace `TextGrab.Uno` collides with `Uno.Toolkit.UI` resolution
- Apply saved theme in ShellPage.Loaded handler
- Uno already has `Uno.Extensions.Toolkit.IThemeService` — don't create a custom one (name collision)

## First-Run Flow
- WPF standalone `FirstRunWindow` → **ContentDialog shown in ShellPage.Loaded**
- Route-based FirstRun navigation was abandoned — root-level sibling routes can't navigate back to Shell properly
- ContentDialog approach is simpler and avoids navigation complexity
- On completing first-run: `IWritableOptions<AppSettings>.UpdateAsync(s => s with { FirstRun = false })`

## History Service
- WPF `Singleton<HistoryService>.Instance` → DI-registered `IHistoryService` / `FileHistoryService`
- Storage: JSON file in `ApplicationData.Current.LocalFolder` (cross-platform)
- `HistoryInfo` is a partial record with Id, TextContent, CaptureDateTime, SourceMode, etc.

## In-App Notifications
- WPF Windows toast notifications → `InfoBar` overlay in ShellPage
- `InAppNotificationService` registered as singleton, `SetHost(Panel)` called in ShellPage.Loaded
- InfoBar auto-dismisses after 4 seconds
- StackPanel overlay on top of NavigationView in a Grid wrapper

## App.Host Accessibility
- `App.Host` is `protected` by default in Uno template → change to `public` so Shell/ShellPage can access DI container

---

## Key Gotchas (Alphabetical)

| Gotcha | Solution |
|--------|----------|
| `Application.Exit()` not implemented in Uno Wasm | Acceptable — only used in DangerSettings shutdown |
| `CliWrap` only on Windows | Use `Condition` in csproj for Tesseract subprocess model |
| `global::` prefix needed for `Uno.Toolkit.UI` and `Uno.Extensions.Configuration` | Project namespace `TextGrab.Uno` collides with `Uno.*` resolution |
| Pages can't nest inside Pages | Use UserControl for embedded content, Frame for navigation |
| `SoftwareBitmap` is NOT cross-platform | Use `Stream` or `byte[]` at service boundaries |
| `Windows.Foundation.Rect` IS available cross-platform | Provided by Uno.WinUI |
| WPF Button subclassing | WinUI doesn't support subclassing Button XAML root — use UserControl wrapper |
| XAML compiler stale cache | Clean obj/ folder or `dotnet clean` if XamlCompiler.exe exits with code 1 |
| `ZoomContentControl` has no `AutoFit` property | Omit it; use `MinZoomLevel`/`MaxZoomLevel` instead of `MinZoomFactor` |

## GrabFrame / Canvas Overlay Pattern
| WPF | WinUI / Uno |
|-----|-------------|
| `ZoomBorder` custom control | Uno Toolkit `ZoomContentControl` |
| `MouseDown/Move/Up` | `PointerPressed/Moved/Released` |
| `CaptureMouse()` | `CapturePointer(e.Pointer)` |
| `ContextMenu` | `ContextFlyout` with `MenuFlyout` |
| `RoutedCommand` | Direct method calls via interface |
| `Canvas.SetLeft/SetTop` | Identical in WinUI |

---

## Undo/Redo System for Canvas-Based Pages
- WPF GrabFrame had complex undo/redo tied to WPF RoutedCommands
- Uno implementation: simple state stack pattern with `Stack<List<WordBorderInfo>>`
- `PushUndo()` captures current WordBorder state before destructive operations (delete, merge, text transforms)
- `RestoreWordBorders()` clears canvas and recreates WordBorders from saved state
- `WordBorder.ToInfo()` serializes position/word to `WordBorderInfo` record
- `new WordBorder(info)` constructor recreates from `WordBorderInfo`
- Must set `wb.Host = this` after creating from info (host reference not serialized)

## WPF RoutedCommand → Direct Method Calls
- WPF `RoutedCommand` with `CommandBindings` → direct `Click` event handlers in code-behind
- WPF `ApplicationCommands.Undo/Redo` → custom undo stack (WinUI TextBox has built-in Ctrl+Z but no programmatic access)
- WPF `InputGestureText` (display only) → `KeyboardAccelerators` (functional, actually fires the command)
- Pattern: each `MenuFlyoutItem` gets a `Click` handler that calls the same method as the keyboard shortcut

## ContentDialog as Universal Dialog Pattern
- ALL WPF child windows become `ContentDialog` in Uno
- For simple forms: build UI programmatically in code-behind (StackPanel + TextBoxes)
- For complex dialogs: create dedicated XAML ContentDialog subclass in `Dialogs/` folder
- `XamlRoot = this.XamlRoot` is REQUIRED — dialog won't show without it
- Nested dialogs: a ContentDialog can show another ContentDialog (e.g., RegexManager → Delete confirmation)
- `SecondaryButton` with `args.Cancel = true` keeps dialog open (useful for Reset Defaults)

## Web Search / URL Launch Pattern
- WPF `Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })` → `Windows.System.Launcher.LaunchUriAsync(new Uri(url))`
- Works cross-platform in Uno (desktop opens browser, Wasm opens new tab)
- For email: `mailto:` URI scheme works the same way

## Gap Closure Methodology
- Don't mark phases "complete" until features are actually functional, not just scaffolded
- Use systematic XAML comparison (every `MenuFlyoutItem` in WPF vs Uno) to find missing items
- Quick wins first: menu items that just call existing `StringMethods` extensions
- Track parity percentage per feature area, not just per phase

---

## Migration Phase Summary

| Phase | What | LOC Added |
|-------|------|-----------|
| 1 | Scaffolding (Uno.Sdk, MVUX, Material, Navigation) | ~500 |
| 2 | EditText page, Shell, FindReplaceDialog, FileService, string utilities | ~3,000 |
| 3 | OCR infrastructure (3 engines, facade, models) | ~2,000 |
| 4 | GrabFrame page, WordBorder control, Canvas overlay | ~2,000 |
| 5 | Quick Simple Lookup page with ListView | ~800 |
| 6 | Settings pages (6 sub-pages, IWritableOptions, CollapsibleButton) | ~1,700 |
| 7 | FirstRun, theme, history, notifications, 3 dialogs, nav fix, screen capture, SkiaSharp, settings migration | ~2,200 |
| 8 | 248 portable unit tests | ~1,200 |
| Gap closure | EditText +15 commands, GrabFrame +undo/redo, QuickLookup +add row | ~500 |
| FullscreenGrab | Screen capture + Canvas region selection + OCR | ~350 |
| System | IHotKeyService + WindowsHotKeyService (P/Invoke RegisterHotKey) | ~170 |
| **Total** | | **~12,500** |

WPF original: ~37,000 LOC. Uno port: ~13,200 LOC (36% of code, ~75% honest feature parity).

## Feature Parity Assessment (honest, revised 2026-04-01)

Previous 90-95% estimates were inflated. Verified by reading actual code, not just counting menu items.

| Feature Area | Parity | What Works End-to-End | Remaining Gaps |
|---|---|---|---|
| EditText | **80%** | Text transforms, OCR paste, clipboard watcher, recent files, find/replace, regex, web search, QR code, font, always-on-top | AI menu, calc pane |
| GrabFrame | **60%** | Image load, OCR, undo/redo (snapshot), table mode (ResultTable), move/resize, drag-drop, search | Translation, WPF code depth |
| QuickLookup | **65%** | CSV load, search, regex, copy, add row, insert-on-copy, send-to-ETW | Barcode, web sources, history |
| Settings | **80%** | All 6 pages persist, export/import, theme | StartupOnLogin registration |
| FullscreenGrab | **70%** | Fullscreen overlay, capture, OCR, modes, keyboard | Single-click word, post-grab actions |
| System | **30%** | Hotkey service infra, background mode (minimize on close) | System tray, startup task |
| Dialogs | **90%** | FindReplace, RegexManager, BottomBar, PostGrab, settings export/import | — |
| **Overall** | **~75%** | Core user workflows work | AI, translation, barcode, calc, tray |

### What "75% honest" means
- A user can edit text, OCR images, grab frames, look up items, and configure settings
- The app builds and runs on Windows Desktop + WebAssembly
- 248 unit tests pass
- The remaining 25% is: AI features (new, not migration), translation (Windows AI API), barcode depth, calc pane (new UI), system tray (complex platform code)

## Fullscreen Grab Migration Pattern
- WPF: Borderless transparent topmost Window (`AllowsTransparency=True`, `WindowStyle=None`)
- Uno: Page that uses `AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen)` on Windows
- **Capture workflow**: Maximize window → Minimize (SW_MINIMIZE via P/Invoke) → Capture screen → Restore maximized with screenshot as background
- Canvas overlay: `Background="#60000000"` (dark translucent), selection `Border` with `Background="#20FFFFFF"` (bright cutout)
- Floating toolbar: `Border` with `Background="#E0202020"`, `CornerRadius="8"`, `ThemeShadow`, `Translation="0,0,32"`
- Keyboard shortcuts: `Page.KeyDown` handler for Esc=cancel, S=single-line, N=normal, T=table, E=toggle-ETW
- On exit (`Unloaded`): `appWindow.SetPresenter(AppWindowPresenterKind.Default)` to restore normal window
- `ShowWindow(hwnd, 6)` P/Invoke for minimize (SW_MINIMIZE=6)
- `GetAppWindow()`: `Win32Interop.GetWindowIdFromWindow(hwnd)` → `AppWindow.GetFromWindowId(windowId)`
- Non-Windows fallback: file picker or clipboard image (no screen capture API)
- OCR modes: Standard (multi-line), Single Line (join), Table (grid extraction)
- Auto-navigates back to EditText after successful OCR grab

## Global Hotkey Migration Pattern
- WPF: `HwndSource.AddHook` + `RegisterHotKey` Win32 API
- Uno: `IHotKeyService` interface + `WindowsHotKeyService` with P/Invoke
- Uses `RegisterHotKey(hwnd, id, MOD_*, VK_*)` — requires window handle
- `MOD_NOREPEAT` flag prevents auto-repeat spam
- `WM_HOTKEY` (0x0312) message must be handled in WndProc
- Hotkey IDs are constants (FullscreenGrabId=1, GrabFrameId=2, etc.)
- Service is `IDisposable` — `UnregisterAll()` on cleanup
