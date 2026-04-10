# Fullscreen Grab: Gap Analysis & Implementation Plan

**Date:** 2026-04-09
**Scope:** FullscreenGrabPage (Uno) vs FullscreenGrab (WPF)
**Current parity estimate:** ~40-50% feature-complete

---

## Critical Bugs (Must Fix)

### BUG-1: Region capture returns entire screen, not selected region
**Severity:** CRITICAL — core feature is broken
**Root cause:** `CaptureRegionAsync(region)` calls GDI `BitBlt` against the live desktop DC, but the app is fullscreen at that point — BitBlt captures the app's own window, not the desktop underneath. When this returns null/garbage, the fallback at line 291 passes the *entire* `_capturedScreen` to OCR instead of cropping to the user's selection.
**Fix:** Don't recapture. Crop the already-captured `_capturedScreen` PNG using SkiaSharp (`SKBitmap.Decode` → `ExtractSubset`). This is faster and correct.
**Files:** `FullscreenGrabPage.xaml.cs:275-295`, `WindowsScreenCaptureService.cs` (leave as-is for full capture)

### BUG-2: No DPI scaling on region coordinates
**Severity:** HIGH — selection misalignment on HiDPI displays
**Root cause:** UI selection coordinates are in DIPs (device-independent pixels). The captured screenshot is in physical pixels. On 150% scaling, a 100x100 DIP selection covers 150x150 physical pixels at a different offset.
**Fix:** Get the DPI scale factor from `XamlRoot.RasterizationScale` and multiply region coordinates before cropping.
**Files:** `FullscreenGrabPage.xaml.cs`

### BUG-3: Send to Edit Text is a TODO stub
**Severity:** MEDIUM — toggle exists but does nothing
**Root cause:** Line 351 has `// Navigate to EditText — TODO: pass text data`
**Fix:** After OCR, navigate to EditText route and pass the captured text. Use navigation data or a shared service.
**Files:** `FullscreenGrabPage.xaml.cs:350-353`

---

## Feature Gaps (Prioritized)

### Tier 1 — High Impact, Moderate Effort

| # | Feature | WPF Location | Effort | Notes |
|---|---------|-------------|--------|-------|
| F1 | **Crop from captured screenshot** | N/A (new) | ~30 lines | SkiaSharp decode + ExtractSubset. Fixes BUG-1. |
| F2 | **DPI-aware coordinates** | FullscreenGrab.cs:381-388 | ~10 lines | `XamlRoot.RasterizationScale` multiply. Fixes BUG-2. |
| F3 | **Send to Edit Text (working)** | FullscreenGrab.cs:828-850 | ~15 lines | Navigate with text parameter. Fixes BUG-3. |
| F4 | **Settings button in toolbar** | FullscreenGrab.xaml:304-313 | ~5 lines XAML | Already have Settings_Click handler. |
| F5 | **Table mode OCR** | FullscreenGrab.cs:755-756 | ~20 lines | Need `RecognizeAsTableAsync` or table formatter on `IOcrLinesWords` |

### Tier 2 — Medium Impact, Moderate Effort

| # | Feature | WPF Location | Effort | Notes |
|---|---------|-------------|--------|-------|
| F6 | **Freeze toggle** | FullscreenGrab.cs:336-361 | ~40 lines | Re-capture screen, refresh BackgroundImage, toggle overlay opacity |
| F7 | **Single-click word selection** | FullscreenGrab.cs:745-754 | ~30 lines | Detect small click (<5px), run OCR on full image, find word at point from bounding boxes |
| F8 | **Zoom (mouse wheel)** | FullscreenGrab.cs:1279-1355 | ~60 lines | ScaleTransform + TranslateTransform on BackgroundImage+Canvas |
| F9 | **Numeric keyboard shortcuts** | FullscreenGrab.cs:194-226 | ~20 lines | 1-9 for language selection |
| F10 | **Language persistence** | FullscreenGrab.cs:474 | ~10 lines | Save/restore last used language from settings |

### Tier 3 — Lower Priority / Phase 9+

| # | Feature | Notes |
|---|---------|-------|
| F11 | Post-grab actions dropdown | Full action framework with Fix GUIDs, Trim Lines, etc. |
| F12 | Grab Frame placement mode | Draw region → spawn GrabFrame page at those coords |
| F13 | Multi-monitor capture | Replace GetSystemMetrics with EnumDisplayMonitors |
| F14 | Edge pan on zoom | Timer-based auto-pan when cursor near screen edge |
| F15 | Edit Last Grab | History integration |
| F16 | Toolbar auto-hide | Show on hover, collapse otherwise |

---

## Migration Gotchas Discovered

### 1. GDI BitBlt can't capture behind a fullscreen WinUI window
**Pattern:** WPF minimizes → captures → maximizes. Works because WPF windows are transparent to GDI. In WinUI/Uno with `AppWindow.SetPresenter(FullScreen)`, the fullscreen window covers the desktop DC. BitBlt captures the app itself.
**Solution:** Capture once before going fullscreen, then crop from the saved image. Never re-capture while fullscreen.
**Migration skill rule:** "When porting fullscreen overlay patterns, capture the screenshot BEFORE entering fullscreen, store it, and crop from memory for region selection."

### 2. DPI scaling differs between WPF and WinUI
**Pattern:** WPF uses `VisualTreeHelper.GetDpi(this)` and `PresentationSource.CompositionTarget.TransformToDevice`. WinUI uses `XamlRoot.RasterizationScale` (simpler, single value).
**Migration skill rule:** "Replace WPF DPI APIs (`GetDpi`, `TransformToDevice`) with `XamlRoot.RasterizationScale`. It returns a single scale factor (1.0 = 100%, 1.5 = 150%). Multiply DIP coordinates by this value to get physical pixels."

### 3. ManipulationDelta works for toolbar drag in WinUI (WPF had none)
**Pattern:** WPF's FullscreenGrab had no draggable toolbar. WinUI ManipulationDelta + TranslateTransform provides a clean drag implementation that WPF lacked.
**Migration skill rule:** "For draggable floating UI in WinUI, use `ManipulationMode=TranslateX,TranslateY` on the element + `TranslateTransform` as RenderTransform. ManipulationDelta gives delta translation. Clamp to page bounds. Does not interfere with child control clicks."

### 4. NavigationView pane hiding for immersive modes
**Pattern:** WPF uses separate borderless Windows for fullscreen overlays. Uno single-window apps need to hide the NavigationView shell.
**Migration skill rule:** "For immersive/fullscreen pages in a NavigationView shell, set `IsPaneVisible=false`, `PaneDisplayMode=LeftMinimal`, `IsPaneToggleButtonVisible=false` when navigating to the immersive page. Restore in `ContentFrame.Navigated` when leaving. This gives a clean, chrome-free overlay."

### 5. SkiaSharp is the universal image manipulation layer
**Pattern:** WPF uses System.Drawing (GDI+) for image cropping. Uno can't use System.Drawing cross-platform. SkiaSharp's `SKBitmap.Decode()` + `ExtractSubset()` replaces all GDI+ cropping.
**Migration skill rule:** "Replace all System.Drawing image operations (Bitmap, Graphics.DrawImage, Clone with Rectangle) with SkiaSharp equivalents. For cropping: `SKBitmap.Decode(stream)` → `bitmap.ExtractSubset(skBitmap, SKRectI)` → `SKImage.FromBitmap()` → `Encode()`. Works on all platforms."

### 6. Region capture pattern: capture-once-crop-many
**Pattern:** The correct architecture for screen-capture overlays is: capture full screen once (while app is hidden), go fullscreen with the screenshot, crop from the in-memory image for each selection. Never try to recapture while the overlay is visible.
**Migration skill rule:** "Screen capture overlays should follow capture-once-crop-many: (1) minimize app, (2) capture full desktop, (3) restore/fullscreen, (4) show captured image as background, (5) crop from stored image for region selections. Recapturing from a visible overlay will capture the overlay itself."

---

## Implementation Plan (This Session)

### Phase A: Fix Critical Bugs (BUG-1, BUG-2, BUG-3)
1. Add `CropRegionFromScreenshot()` method using SkiaSharp
2. Apply DPI scaling via `XamlRoot.RasterizationScale`
3. Replace `CaptureRegionAsync` call with SkiaSharp crop
4. Implement Send to ETW navigation with text data

### Phase B: Add Tier 1 Features (F4, F5)
5. Add Settings button to floating toolbar
6. Add table mode awareness (if engine is Tesseract, hide Table radio)

### Phase C: Add Tier 2 High-Value Features (F6, F7)
7. Freeze toggle (F key, toolbar button)
8. Single-click word selection (detect small clicks)

### Phase D: Build & Test
9. Build, launch, verify region capture works correctly
10. Test DPI scaling, freeze, word selection

---

## Verification Checklist
- [ ] Draw a region → OCR returns text from ONLY the selected region (not full screen)
- [ ] On 150% DPI display, selection rectangle matches captured text accurately
- [ ] Send to ETW toggle → navigates to EditText with captured text
- [ ] Settings button opens Settings page
- [ ] F key toggles freeze (re-captures without overlay dimming)
- [ ] Small click (<5px) → detects and copies single word at click point
- [ ] Esc → returns to EditText with sidebar restored
- [ ] Table mode radio hidden when Tesseract language selected
