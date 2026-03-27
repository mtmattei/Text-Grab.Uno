# Text-Grab: WPF to Uno Platform Migration

## Project Context

This is a migration of [Text-Grab](https://github.com/TheJoeFin/Text-Grab) (WPF OCR utility, v4.12.1) to Uno Platform.
The original WPF app lives in `Text-Grab/` and the new Uno app will be scaffolded alongside it.

**Migration spec:** `../Text-Grab-Migration-Plan.md` — the authoritative reference for all decisions, mappings, and phase plans.

## Target Stack

- **Framework:** Uno Platform (Single Project, `Uno.Sdk`)
- **Platforms:** Windows Desktop + WebAssembly (v1 scope)
- **Architecture:** MVUX (Model-View-Update-eXtended) — NOT MVVM
- **Theme:** Uno Material (MD3) with custom teal primary (#308E98), dark default
- **Navigation:** Uno.Extensions.Navigation (region-based, NavigationView shell)
- **DI/Config:** Uno.Extensions.Hosting, IWritableOptions<T>
- **Image Processing:** SkiaSharp (replaces Magick.NET and GDI+)

## Key Architectural Rules

### MVUX Pattern
- Models are `partial record` classes with `Model` suffix (e.g., `EditTextModel`)
- Use `IFeed<T>` for read-only async data, `IState<T>` for mutable state, `IListState<T>` for collections
- Commands are just methods on the Model — no ICommand, no RelayCommand, no RoutedCommand
- MVUX source generator creates ViewModels automatically — never write ViewModels by hand

### Single-Window Navigation
- The WPF app uses 15+ independent windows. This migration uses a single `Shell.xaml` with NavigationView
- WPF `Window` types become `Page` types
- WPF child windows (dialogs) become `ContentDialog` instances
- On Windows Desktop, GrabFrame may detach into a companion `AppWindow`

### Platform-Specific Code
- OCR, screen capture, hotkeys, Windows AI: `#if WINDOWS` only
- Service interfaces (`IOcrEngine`, `IScreenCaptureService`, etc.) in shared code
- Implementations in platform-specific folders or behind `#if`

### Settings
- WPF `Properties.Settings.Default` replaced by `IWritableOptions<AppSettings>` with `appsettings.json`
- On Windows first launch, migrate existing WPF settings to new format

## WPF-to-WinUI Quick Reference

### XAML Changes
- `clr-namespace:` → `using:`
- `{DynamicResource}` → `{ThemeResource}`
- `{Binding}` → `{x:Bind ViewModel.X, Mode=OneWay}` (default mode is OneTime, not OneWay!)
- `ContextMenu` → `ContextFlyout` with `MenuFlyout`
- `Menu`/`MenuItem` → `MenuBar`/`MenuBarItem`/`MenuFlyoutItem`
- `InputGestureText` → `KeyboardAccelerators` (functional, not just display)
- `Style.Triggers` → `VisualStateManager` with `StateTrigger`
- Resource paths: `/Styles/X.xaml` → `ms-appx:///Styles/X.xaml`
- Always use `BasedOn` when overriding default control styles

### Event Changes (Mouse → Pointer)
- `MouseDown` → `PointerPressed`, `MouseMove` → `PointerMoved`, `MouseUp` → `PointerReleased`
- `MouseEnter` → `PointerEntered`, `MouseLeave` → `PointerExited`
- `MouseWheel` → `PointerWheelChanged`
- All `Preview*` events → use the non-preview equivalent (no tunneling in WinUI)
- `MouseEventArgs` → `PointerRoutedEventArgs`, `KeyEventArgs` → `KeyRoutedEventArgs`

### C# Namespace Changes
- `System.Windows.*` → `Microsoft.UI.Xaml.*`
- `System.Windows.Threading.Dispatcher` → `Microsoft.UI.Dispatching.DispatcherQueue`
- `Dispatcher.Invoke()` → `DispatcherQueue.TryEnqueue()`

### Controls With No Direct Equivalent
- `DataGrid` → `ItemsRepeater` + `UniformGridLayout` (our choice for portability)
- `StatusBar` → custom Grid
- `Hyperlink` → `HyperlinkButton`
- `Label` → `TextBlock`
- WPF-UI controls → WinUI native + Uno Toolkit equivalents

## What NOT To Do

- Do NOT use `{Binding}` — use `{x:Bind}` throughout
- Do NOT write ViewModels by hand — let MVUX generate them
- Do NOT use hardcoded colors — use Material theme resources
- Do NOT create new Windows — use Page navigation or ContentDialog
- Do NOT use `Singleton<T>` — register in DI container
- Do NOT use `Properties.Settings` — use `IWritableOptions<AppSettings>`
- Do NOT use `System.Drawing` — use SkiaSharp or WinUI imaging APIs
- Do NOT add `Style.Triggers` — use VisualStateManager
- Do NOT use `DynamicResource` — use `ThemeResource`

## Build & Run

```bash
# Build all targets
dotnet build

# Run Windows Desktop
dotnet run --project TextGrab.Uno

# Run Wasm (when scaffolded)
dotnet run --project TextGrab.Uno --framework net10.0-browserwasm
```

## File Organization (Uno Project)

```
TextGrab.Uno/
├── App.xaml(.cs)
├── Shell.xaml(.cs)              # NavigationView shell
├── appsettings.json             # App configuration
├── Models/                      # MVUX Models (partial records)
│   ├── EditTextModel.cs
│   ├── GrabFrameModel.cs
│   ├── QuickLookupModel.cs
│   └── SettingsModel.cs
├── Views/                       # Pages
│   ├── EditTextPage.xaml(.cs)
│   ├── GrabFramePage.xaml(.cs)
│   ├── QuickLookupPage.xaml(.cs)
│   ├── SettingsPage.xaml(.cs)
│   └── FirstRunPage.xaml(.cs)
├── Controls/                    # Custom UserControls
│   ├── WordBorder.xaml(.cs)
│   ├── CollapsibleButton.xaml(.cs)
│   └── ...
├── Dialogs/                     # ContentDialogs
│   ├── FindReplaceDialog.xaml(.cs)
│   ├── AddRemoveDialog.xaml(.cs)
│   └── ...
├── Services/                    # Service interfaces + implementations
│   ├── IOcrEngine.cs
│   ├── IHistoryService.cs
│   └── ...
├── Platforms/
│   └── Windows/                 # #if WINDOWS implementations
│       ├── WindowsOcrEngine.cs
│       ├── ScreenCaptureService.cs
│       └── HotKeyManager.cs
├── Styles/                      # Resource dictionaries
│   └── ...
├── Shared/                      # Portable code from WPF
│   ├── StringMethods.cs
│   ├── CalculationService.cs
│   └── ...
└── Assets/
```

## References

- [Migration Spec](../Text-Grab-Migration-Plan.md) — full plan with all appendices
- [Uno Platform WPF Migration Guide](https://platform.uno/docs/articles/wpf-migration.html)
- [WPF to WinUI XAML Namespace Map](https://platform.uno/articles/wpf-to-winui-xaml-the-complete-namespace-and-control-map/)
- [WPF Architecture Patterns for MVUX](https://platform.uno/articles/wpf-architecture-patterns-rewritten-for-uno-platform-mvux/)
