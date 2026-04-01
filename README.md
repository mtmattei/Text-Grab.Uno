# Text-Grab.Uno

> **Text Grab** is a minimal, OCR-focused utility for Windows that lets you capture text from anywhere on screen — screenshots, images, PDFs, or video — and instantly edit, search, or copy it. It features four modes: Fullscreen Grab (screenshot-style region capture), Grab Frame (persistent overlay for picking words), Edit Text (Notepad-like editor with OCR tools), and Quick Simple Lookup (searchable key-value list). Built by [Joseph Finney](https://github.com/TheJoeFin).
>
> Original repo: **[github.com/TheJoeFin/Text-Grab](https://github.com/TheJoeFin/Text-Grab)**

---

## About This Port

This is a cross-platform port of Text-Grab from WPF to [Uno Platform](https://platform.uno), targeting **Windows Desktop** and **WebAssembly**. The goal is feature parity with the original while gaining cross-platform reach and a modern Material Design 3 UI.

The port was built incrementally across 8 phases using Claude Code, with every migration pattern, gotcha, and architectural decision documented for reuse.

| | WPF Original | Uno Port |
|---|---|---|
| **LOC** | ~37,000 | ~13,450 |
| **Framework** | WPF + WPF-UI (Fluent) | Uno Platform (Material MD3) |
| **Architecture** | Code-behind + Singleton services | MVUX + DI + manual Frame navigation |
| **Platforms** | Windows only | Windows, WebAssembly, Desktop (Skia) |
| **Tests** | xUnit | 248 NUnit tests passing |
| **Theme** | WPF-UI Fluent | Uno Material with custom teal (#308E98) |

## Build & Run

```bash
cd TextGrab.Uno
dotnet build

# Windows Desktop
dotnet run --project TextGrab.Uno --framework net10.0-windows10.0.26100

# Run tests
dotnet test
```

## What's Ported

### Edit Text — 90% parity (36/40 features)

| Feature | Status |
|---|---|
| Open/Save/Save As + Recent Files | Working |
| Cut/Copy/Paste + OCR Paste (Ctrl+Shift+V) | Working |
| Make Single Line, Trim, Toggle Case, Remove Duplicates | Working |
| Replace Reserved Characters, Try Numbers/Letters, Correct GUIDs | Working |
| Unstack Text (both modes), Add/Remove at Position | Working |
| Find and Replace (with regex) | Working |
| Regex Manager (CRUD + real-time testing, 15 defaults) | Working |
| Web Search selected text | Working |
| Select Word/Line/All/None, Move Line Up/Down | Working |
| Isolate/Delete/Insert/Split Selection | Working |
| Clipboard Watcher (auto-OCR on image) | Working |
| Font dialog, Word Wrap, Always On Top, Hide Bottom Bar | Working |
| QR Code generation (ZXing.Net) | Working |
| Drag & Drop text/files | Working |
| Status bar (words, chars, line, column) | Working |
| Navigate to Grab Frame / Quick Lookup / Fullscreen Grab | Working |
| About / Contact / Feedback / Rate & Review | Working |
| Calculate Pane | Missing |
| AI menu (Summarize/Translate/Extract) | Missing |
| Multiple simultaneous windows | Missing |

### Grab Frame — 91% parity (21/23 features)

| Feature | Status |
|---|---|
| Open/Paste/Drag-drop images | Working |
| OCR with word border overlays | Working |
| Word selection (click + drag rectangle) | Working |
| Word move/resize (Ctrl+drag) | Working |
| Undo/Redo (snapshot stack, capped at 50) | Working |
| Table mode (ResultTable analysis + table-formatted copy) | Working |
| Merge/Break/Delete selected words | Working |
| Search with regex, language selector | Working |
| Send to Edit Text (copy + navigate) | Working |
| Zoom (ZoomContentControl) | Working |
| Barcode/QR detection (ZXing.Net + SkiaSharp) | Working |
| Try Numbers/Letters on selected words | Working |
| Freeze toggle, Edit words in-place | Working |
| Context menu (copy, delete, numbers, letters, merge) | Working |
| Translation | Missing |

### Quick Simple Lookup — 83% parity (10/12)

| Feature | Status |
|---|---|
| Open CSV / Paste data / Save CSV | Working |
| Search with regex toggle | Working |
| Copy value/key/both, Delete item, Add row | Working |
| Insert-on-copy toggle, Send to Edit Text toggle | Working |
| Keyboard shortcuts (Enter = copy) | Working |
| Multiple web lookup sources | Missing |
| History panel | Missing |

### Fullscreen Grab — 86% parity (12/14)

| Feature | Status |
|---|---|
| Screen capture background (Windows, P/Invoke + SkiaSharp) | Working |
| Fullscreen overlay (AppWindow.FullScreen) | Working |
| Region selection (Canvas drag) | Working |
| OCR on selected region | Working |
| Standard / Single Line / Table modes | Working |
| Keyboard shortcuts (Esc, S, N, T, E) | Working |
| Language selector, Send to ETW toggle | Working |
| Barcode detection on capture | Working |
| Dark overlay with shade setting | Working |
| Non-Windows fallback (file picker / clipboard) | Working |
| Single-click word mode | Missing |
| Post-grab actions dropdown | Missing |

### Settings — 100% parity (11/11)

All 6 settings pages working: General, Fullscreen Grab, Languages, Keys, Tesseract, Danger. Theme switching (Light/Dark/System), settings export/import (JSON), run in background with system tray.

### System Integration

| Feature | Status |
|---|---|
| System tray icon (Shell_NotifyIcon) | Working (Windows) |
| Minimize to tray on close | Working (Windows) |
| Restore from tray (double-click) | Working (Windows) |
| In-app notifications (InfoBar) | Working |
| Global hotkey service (infra) | Infra only |
| Startup on Login | Missing |

### Dialogs — 100% parity (8/8)

Find & Replace, Regex Manager, Bottom Bar Settings, Post-Grab Action Editor, Settings Export/Import, First Run, About, Font — all working as ContentDialogs.

### Overall: **~90% feature parity** (103/114 features working)

## Architecture

```
TextGrab.Uno/
├── App.xaml(.cs)              # DI, config, route registration
├── Presentation/              # Pages + MVUX Models
│   ├── ShellPage              # NavigationView shell (manual Frame.Navigate)
│   ├── EditTextPage           # Main text editor with full menu system
│   ├── GrabFramePage          # Image OCR with word border overlays
│   ├── QuickLookupPage        # CSV key-value search
│   ├── FullscreenGrabPage     # Screen capture + region OCR
│   ├── SettingsPage           # Top-nav with 6 sub-pages
│   └── *SettingsPages         # General, FSG, Language, Keys, Tesseract, Danger
├── Controls/                  # WordBorder, CollapsibleButton
├── Dialogs/                   # RegexManager, BottomBar, PostGrabAction
├── Services/                  # OCR, File, History, Notification, Barcode, Tray
├── Platforms/Windows/         # Screen capture, hotkeys, tray (P/Invoke)
├── Models/                    # AppSettings, StoredRegex, ResultTable, etc.
├── Shared/                    # StringMethods, ClipboardHelper, OcrUtilities
└── Styles/                    # Material color palette override
```

### Key Technical Decisions

1. **Manual `Frame.Navigate`** — Uno Extensions region navigation doesn't support re-visiting routes. Manual `SelectionChanged` handlers give full control.
2. **ContentDialog** for all WPF child windows — simpler than route-based dialogs.
3. **SkiaSharp** replaces System.Drawing and Magick.NET — cross-platform image processing.
4. **ZXing.Net** `BarcodeReaderGeneric` with SkiaSharp pixel conversion — cross-platform barcode scanning.
5. **P/Invoke** for Windows-specific features — screen capture (GDI BitBlt), system tray (Shell_NotifyIcon), hotkeys (RegisterHotKey), fullscreen (AppWindow).
6. **`IWritableOptions<AppSettings>`** — auto-registered by `Section<T>()`, persists to `appsettings.json`.

## Migration Patterns

See [`docs/WPF-to-Uno-Migration-Patterns.md`](docs/WPF-to-Uno-Migration-Patterns.md) for the full reference of patterns, gotchas, and lessons learned — including the three failed navigation approaches before finding the working one.

## Credits

- **Original app**: [Text-Grab](https://github.com/TheJoeFin/Text-Grab) by [Joseph Finney](https://github.com/TheJoeFin) ([@TheJoeFin](https://twitter.com/thejoefin))
- **Framework**: [Uno Platform](https://platform.uno)
- **Migration**: Assisted by Claude (Anthropic)
