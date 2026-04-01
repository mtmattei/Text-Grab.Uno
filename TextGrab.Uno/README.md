# Text-Grab.Uno

Cross-platform port of [Text-Grab](https://github.com/TheJoeFin/Text-Grab) (WPF OCR utility by Joseph Finney) to [Uno Platform](https://platform.uno).

## Overview

Text-Grab is a desktop OCR utility that captures text from screens, images, and files. This project ports the WPF app to Uno Platform for cross-platform support (Windows Desktop + WebAssembly).

| Metric | Value |
|--------|-------|
| **WPF Original** | ~37,000 LOC |
| **Uno Port** | ~13,450 LOC |
| **Unit Tests** | 248 passing |
| **Targets** | Windows Desktop, WebAssembly, Desktop (Skia), .NET generic |
| **Architecture** | MVUX + manual Frame navigation |
| **Theme** | Uno Material (MD3) with custom teal primary (#308E98) |

## Build & Run

```bash
# Build all targets
cd TextGrab.Uno
dotnet build

# Run Windows Desktop
dotnet run --project TextGrab.Uno --framework net10.0-windows10.0.26100

# Run tests
dotnet test
```

## Architecture

- **Uno.Sdk** Single Project with MVUX, Material, Navigation, Toolkit
- **Manual Frame.Navigate** for NavigationView (Uno Extensions regions don't support re-visiting routes)
- **Settings** via `IWritableOptions<AppSettings>` with `appsettings.json` persistence
- **OCR engines**: Windows Runtime OCR, Tesseract (CLI), Windows AI — all behind `IOcrEngine` interface
- **Platform-specific** services (`#if WINDOWS`): screen capture, hotkeys, system tray

## Feature Parity Matrix (~87%)

### Edit Text Window

| Feature | WPF | Uno | Status |
|---|---|---|---|
| Open/Save/Save As | Yes | Yes | Working |
| Recent Files menu | Yes | Yes | Working |
| Copy All and Close | Yes | Yes | Working |
| Undo/Redo | Built-in | Built-in | Working |
| Cut/Copy/Paste | Yes | Yes | Working |
| OCR Paste (Ctrl+Shift+V) | Yes | Yes | Working |
| Make Single Line | Yes | Yes | Working |
| Trim Each Line | Yes | Yes | Working |
| Toggle Case (Shift+F3) | Yes | Yes | Working |
| Remove Duplicate Lines | Yes | Yes | Working |
| Replace Reserved Characters | Yes | Yes | Working |
| Try To Numbers / Letters | Yes | Yes | Working |
| Correct GUIDs | Yes | Yes | Working |
| Unstack Text (both modes) | Yes | Yes | Working |
| Add/Remove at Position | Yes | Yes | Working |
| Find and Replace (regex) | Yes | Yes | Working |
| Regex Manager | Yes | Yes | Working |
| Web Search selected text | Yes | Yes | Working |
| Select Word/Line/All/None | Yes | Yes | Working |
| Move Line Up/Down | Yes | Yes | Working |
| Isolate/Delete/Insert Selection | Yes | Yes | Working |
| Split Before/After Selection | Yes | Yes | Working |
| Delete All Instances | Yes | Yes | Working |
| OCR from Image File | Yes | Yes | Working |
| OCR from Clipboard | Yes | Yes | Working |
| Clipboard Watcher (auto-OCR) | Yes | Yes | Working |
| Word Wrap toggle | Yes | Yes | Working |
| Font dialog | Yes | Yes | Working |
| Bottom Bar Settings | Yes | Yes | Working |
| Always On Top | Yes | Yes | Working (Windows) |
| Hide Bottom Bar | Yes | Yes | Working |
| QR Code generation | Yes | Yes | Working |
| About / Contact / Feedback | Yes | Yes | Working |
| Navigate to other pages | Yes | Yes | Working |
| Drag & Drop text/files | Yes | Yes | Working |
| Status bar (words/chars/line) | Yes | Yes | Working |
| Calculate Pane | Yes | No | Missing |
| AI menu (Summarize/Translate) | Yes | No | Missing |
| Multiple windows | Yes | No | Missing (single-window) |

### Grab Frame

| Feature | WPF | Uno | Status |
|---|---|---|---|
| Open/Paste Image | Yes | Yes | Working |
| Drag-drop image files | Yes | Yes | Working |
| OCR with word borders | Yes | Yes | Working |
| Word selection (click/drag) | Yes | Yes | Working |
| Word move/resize | Yes | Yes | Working |
| Undo/Redo (snapshot stack) | Yes | Yes | Working |
| Table mode (ResultTable) | Yes | Yes | Working |
| Merge/Break/Delete words | Yes | Yes | Working |
| Search with regex | Yes | Yes | Working |
| Language selection | Yes | Yes | Working |
| Send to Edit Text | Yes | Yes | Working |
| Zoom (ZoomContentControl) | Yes | Yes | Working |
| Barcode detection | Yes | Yes | Working |
| Try Numbers/Letters on words | Yes | Yes | Working |
| Freeze toggle | Yes | Yes | Working |
| Edit words in-place | Yes | Yes | Working |
| Translation | Yes | No | Missing |

### Quick Simple Lookup

| Feature | WPF | Uno | Status |
|---|---|---|---|
| Open CSV / Paste data | Yes | Yes | Working |
| Search with regex toggle | Yes | Yes | Working |
| Copy selected value/key/both | Yes | Yes | Working |
| Delete item / Add row | Yes | Yes | Working |
| Save CSV | Yes | Yes | Working |
| Insert-on-copy toggle | Yes | Yes | Working |
| Send to Edit Text toggle | Yes | Yes | Working |
| Keyboard (Enter=copy) | Yes | Yes | Working |
| Multiple web lookup sources | Yes | No | Missing |
| History panel | Yes | No | Missing |

### Fullscreen Grab

| Feature | WPF | Uno | Status |
|---|---|---|---|
| Screen capture background | Yes | Yes | Working (Windows) |
| Fullscreen overlay mode | Yes | Yes | Working (AppWindow) |
| Region selection (drag) | Yes | Yes | Working |
| OCR on region | Yes | Yes | Working |
| Standard/SingleLine/Table modes | Yes | Yes | Working |
| Keyboard shortcuts (Esc/S/N/T/E) | Yes | Yes | Working |
| Language selector | Yes | Yes | Working |
| Send to ETW toggle | Yes | Yes | Working |
| Barcode detection | Yes | Yes | Working |
| Dark overlay with shade setting | Yes | Yes | Working |
| Non-Windows fallback | N/A | Yes | Working |
| Single-click word mode | Yes | No | Missing |
| Post-grab actions dropdown | Yes | No | Missing |

### Settings

| Feature | WPF | Uno | Status |
|---|---|---|---|
| General (theme/launch/toast/etc) | Yes | Yes | Working |
| Fullscreen Grab settings | Yes | Yes | Working |
| Language settings | Yes | Yes | Working |
| Keyboard shortcuts page | Yes | Yes | Working (UI) |
| Tesseract settings | Yes | Yes | Working |
| Danger (reset/export/import) | Yes | Yes | Working |
| Settings export/import JSON | Yes | Yes | Working |
| Theme switching (Light/Dark/System) | Yes | Yes | Working |
| Run in Background (tray) | Yes | Yes | Working (Windows) |
| System tray icon | Yes | Yes | Working (Windows) |

### System Integration

| Feature | WPF | Uno | Status |
|---|---|---|---|
| System tray icon | Yes | Yes | Working (Windows) |
| Minimize to tray on close | Yes | Yes | Working (Windows) |
| Restore from tray | Yes | Yes | Working (Windows) |
| In-app notifications | Toast | InfoBar | Working |
| Global hotkey service | Yes | Yes | Infra only |

### Dialogs

| Feature | WPF | Uno | Status |
|---|---|---|---|
| Find and Replace | Yes | Yes | Working |
| Regex Manager (CRUD + test) | Yes | Yes | Working |
| Bottom Bar Settings | Yes | Yes | Working |
| Post-Grab Action Editor | Yes | Yes | Working |
| Settings Export/Import | Yes | Yes | Working |
| First Run welcome | Yes | Yes | Working |
| About dialog | Yes | Yes | Working |
| Font dialog | Yes | Yes | Working |

### Summary

| Category | Working | Missing | Parity |
|---|---|---|---|
| Edit Text | 36/40 | 4 | 90% |
| Grab Frame | 21/23 | 2 | 91% |
| Quick Lookup | 10/12 | 2 | 83% |
| Fullscreen Grab | 12/14 | 2 | 86% |
| Settings | 11/11 | 0 | 100% |
| System | 5/6 | 1 | 83% |
| Dialogs | 8/8 | 0 | 100% |
| **Overall** | **103/114** | **11** | **~90%** |

## Migration Patterns

See [`WPF-to-Uno-Migration-Patterns.md`](docs/WPF-to-Uno-Migration-Patterns.md) for detailed patterns, gotchas, and lessons learned during the migration.

## Key Technical Decisions

1. **Manual Frame.Navigate** over Uno Extensions region navigation (regions don't support re-visiting routes)
2. **ContentDialog** for all WPF child windows/dialogs
3. **SkiaSharp** replaces System.Drawing and Magick.NET
4. **ZXing.Net** BarcodeReaderGeneric with SkiaSharp pixel conversion for cross-platform barcode scanning
5. **P/Invoke** for Windows-specific features (screen capture, system tray, hotkeys)
6. **IWritableOptions<AppSettings>** for persistent settings (auto-registered by `Section<T>()`)

## Credits

- Original app: [Text-Grab](https://github.com/TheJoeFin/Text-Grab) by Joseph Finney
- Framework: [Uno Platform](https://platform.uno)
- Migration assisted by Claude (Anthropic)
