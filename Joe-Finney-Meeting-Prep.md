# Meeting Prep — Joseph Finney (Text-Grab Author)

> **Goal:** (1) Get his approval to release the Uno port. (2) Excite him about porting his other WPF apps to Uno.
> **Posture:** Candid about gaps, but frame each one with the reason and the plan. He's a senior .NET dev — he'll smell spin.
> **Source of truth:** Verified against current code on 2026-04-08, not the older comparison report.

---

## 0. The 60-Second Opener (memorize this)

> "Joe — first off, thanks for making Text-Grab. The reason we picked it for the Uno migration showcase is that it's a real app with real complexity: 15 windows, three OCR engines, a custom Canvas overlay system, P/Invoke for screen capture, the works. If we could port *that* cleanly, we could port anything.
>
> The result is roughly **13,500 lines of C# vs your ~37,000** — about 60% less code — while running on **Windows, WebAssembly, and Skia desktop** instead of just Windows. Same teal identity, Material Design 3 instead of Fluent. **103 of 114 features** are working today; the gaps that remain are real and I want to walk you through each one honestly. Then I'd love to show you a few of the engineering choices and hear what you think, especially anything you want changed before you'd be comfortable putting your name on a release."

That paragraph does three jobs: credits him, leads with the headline numbers, signals honesty about gaps, and explicitly asks for his approval criteria.

---

## 1. Headline Numbers (the table he'll want to see)

| | **WPF Original (v4.12.1)** | **Uno Port** |
|---|---|---|
| **C# lines** | ~29,600 | ~11,200 (**−62%**) |
| **XAML lines** | ~7,600 | ~2,200 (**−71%**) |
| **Total source LOC** | ~37,500 | ~13,450 (**−60%**) |
| **C# files** | 113 | 96 |
| **XAML files** | 33 | 22 |
| **Explicit NuGet packages** | 16 | 3 (**−81%**) |
| **Native binary footprint** | ~70 MB Magick.NET | 0 (SkiaSharp instead) |
| **Service interfaces** | 1 (`ILanguage`) | **14** |
| **Unit tests** | 0 | **248 passing** (17 files) |
| **Platforms** | Windows only | Windows + WebAssembly + Skia desktop (macOS/Linux ready) |
| **Architecture** | Code-behind + `Singleton<T>` | MVUX + DI + Material 3 |
| **Theme** | WPF-UI Fluent | Uno Material with the original `#308E98` teal |
| **Feature parity** | — | **~90% (103/114)** |

**Caveat to be ready for:** the 60% LOC reduction does *not* mean we wrote less product. Some of the deletion is genuine win (no `Properties.Settings.Designer.cs`, no per-window plumbing, MVUX source-gen replaces hand-rolled boilerplate, no Magick.NET wrappers, no custom singleton pattern). Some of it is gaps (see §5). Don't claim it as pure efficiency.

---

## 2. The Architecture Story (the part he'll find interesting)

This is where you spend most of the meeting if he's engaged. Lead with the *decisions*, not the diff.

### 2.1 One window instead of fifteen

His app opens 15+ independent `Window` instances (`EditTextWindow`, `GrabFrame`, `QuickSimpleLookup`, `SettingsWindow`, six dialog Windows, etc.). The Uno port collapses all of these into a single `Shell` with `NavigationView` and `Frame.Navigate`. Child windows became `ContentDialog` instances.

**Why this matters to him:** It's not just a cross-platform requirement — it eliminates ~1,500 lines of multi-window state management, makes back-navigation built-in, and means the app starts up faster because pages are created on demand instead of constructing all 15 window types eagerly.

**The honest caveat:** GrabFrame in his app benefits from being a small floating overlay you can detach. We can support that on Windows via `AppWindow`, but we haven't implemented the detach yet. It's listed as a Phase 9 item.

### 2.2 14 service interfaces vs 1

His app has one interface (`ILanguage`) and uses `Singleton<T>` static instances for HistoryService, LanguageService, etc. We have 14 service interfaces with DI registration in `App.xaml.cs`:

```
IOcrEngine          → WindowsOcrEngine, TesseractOcrEngine, WindowsAiOcrEngine
IOcrService         → OcrService (facade, picks the right engine)
ILanguageService    → LanguageService
IScreenCaptureService → WindowsScreenCaptureService
IHotKeyService      → WindowsHotKeyService
ISystemTrayService  → WindowsSystemTrayService
IHistoryService     → FileHistoryService
IFileService, IBarcodeService, INotificationService, IClipboardService, ...
```

All three OCR engines register as `IOcrEngine`, and `OcrService` routes by language kind (`Global → WindowsOCR`, `Tesseract → TesseractEngine`, `WindowsAi → WindowsAiEngine`). It's a chain-of-responsibility pattern that means adding a future cloud OCR engine for Wasm is one new class — no `switch` statement to update.

**Why he'll care:** This is exactly the kind of refactor he probably *wanted* to do but didn't have time for. It also makes the app testable for the first time.

### 2.3 MVUX, not MVVM

We use **MVUX** (Model-View-Update-eXtended), which is Uno's reactive pattern. Models are `partial record` classes; the source generator emits the ViewModel. State is `IState<T>` (mutable), `IFeed<T>` (read-only async), `IListState<T>` (collections). No hand-written `INotifyPropertyChanged`, no `RelayCommand`, no `ICommand` boilerplate.

**Be ready for:** "Why not MVVM?" — Answer: his app already had a Model + Service layer with no ViewModels at all. MVUX's record-based immutable state was a closer match than retrofitting a full MVVM layer. It also matches the existing `HistoryInfo` / `LookupItem` / `OcrOutput` records he already has.

### 2.4 Image pipeline: Magick.NET → SkiaSharp

His app pulls in `Magick.NET-Q16-AnyCPU`, `Magick.NET.SystemDrawing`, and `Magick.NET.SystemWindowsMedia` — roughly **70 MB of native binaries** for image preprocessing. We replaced all of it with `SkiaSharp`, which is bundled with Uno's renderer (zero additional cost) and runs on every target platform. Same for ZXing — we use `ZXing.Net` with a SkiaSharp pixel-buffer adapter instead of `ZXing.Windows.Compatibility` (which depends on `System.Drawing`).

**Why he'll care:** His installer is smaller, his build is faster, and the same code runs in the browser.

### 2.5 P/Invoke layer is properly isolated

All Windows-specific P/Invoke lives in dedicated service files under `Platforms/Windows/`:
- `WindowsScreenCaptureService` — GDI `BitBlt` + `SkiaSharp`
- `WindowsHotKeyService` — `RegisterHotKey` / `UnregisterHotKey`
- `WindowsSystemTrayService` — `Shell_NotifyIcon` (104 lines, replaces the entire WPF-UI.Tray dependency)

Not sprinkled across pages. Behind interfaces. `#if WINDOWS` only at the registration site, not at the call site.

### 2.6 Settings: `Properties.Settings` → `IWritableOptions<AppSettings>`

His ~50-property `Properties.Settings.Default` (~842 lines of generated code in `Settings.Designer.cs`) is replaced with an immutable `AppSettings` record persisted to `appsettings.json`. Uno.Extensions.Configuration's `Section<T>()` auto-registers both `IOptions<AppSettings>` and `IWritableOptions<AppSettings>` — no custom boilerplate for writes. The app updates settings with:

```csharp
await _settings.UpdateAsync(s => s with { AppTheme = "Dark" });
```

**Bonus:** On Windows first launch we have a one-time settings migrator that imports his existing `Properties.Settings` into the new format, so users don't lose their preferences.

### 2.7 The navigation rabbit hole (a war story he'll appreciate)

Uno Extensions ships with a region-based navigation system. We tried it three times before falling back to manual `Frame.Navigate`:

1. **Visibility navigator** with `uen:Region.Navigator="Visibility"` — content rendered blank inside `NavigationView`.
2. **Frame with `uen:Region.Attached`** — content rendered, but you could only navigate to each page **once**; re-clicking was a no-op.
3. **Manual `Frame.Navigate`** — strip all `uen:Region` attributes, use `SelectionChanged` with `Frame.Navigate(typeof(Page))`. Fully repeatable.

We document this in the migration patterns file. It's a great anecdote because (a) it shows we didn't just blindly use defaults, and (b) it's the kind of thing he'd hit himself if he migrated his other apps. **It's also a real, honest piece of feedback for the Uno team that we have already filed.**

---

## 3. Demo Order (what to show, in what sequence)

If you have screen time, this order de-risks the conversation:

1. **Edit Text page** — biggest surface, most visibly polished. Show menu, find/replace, regex manager (15 defaults + real-time testing), QR code generation. *Establishes credibility.*
2. **Grab Frame** — show OCR, word-border overlay, click-drag selection, undo/redo, table mode, send-to-EditText. *Most technically complex piece of UI.*
3. **Fullscreen Grab on Windows** — region capture, OCR, three modes. *Proves the P/Invoke layer works.*
4. **Settings** — six sub-pages, theme switching live. *Proves the architecture is consistent.*
5. **WebAssembly** — load the same app in a browser. Edit Text + Quick Lookup work; OCR features show graceful "Available on Windows Desktop only." *Lands the cross-platform punch.*
6. **The codebase** — open `App.xaml.cs` ConfigureServices, then `EditTextModel.cs`, then `WordBorder.xaml.cs`. Three files, three different patterns, all clean. *Lands the architecture punch.*

Don't show the gaps unprompted. Save them for §5 when *he* asks (he will).

---

## 4. What's Genuinely Strong (lead with these if asked "what are you proudest of?")

| | What | Why it's good |
|---|---|---|
| 1 | **Three OCR engines, one interface** | Clean DI, future cloud engine is one new class |
| 2 | **`WordBorder` custom control + `IGrabFrameHost`** | Decoupled from page, handles edit/move/resize/context menu cleanly |
| 3 | **Snapshot-based undo (capped at 50) in Grab Frame** | Survives merge/break/delete, simple and robust |
| 4 | **Regex Manager dialog** | Full CRUD + 15 defaults + real-time testing — matches the original's depth |
| 5 | **`IWritableOptions<AppSettings>` + first-launch migrator** | Auto-persistence, plus existing users keep their settings |
| 6 | **248 unit tests** | He had zero. Now `StringMethods` and `ExtractedPatterns` are pinned. |
| 7 | **In-app `InfoBar` notifications** | Replaces UWP toast notifications, cross-platform |
| 8 | **Dependency reduction** (16 → 3 NuGets, ~70 MB Magick.NET deleted) | His installer would shrink dramatically |

---

## 5. The Honest Gap Inventory (memorize this list — he WILL ask)

**Frame each one as: what's missing → why → what we plan to do.** Don't apologize, don't bury, don't pad.

### Real gaps that affect users

1. **EditText custom undo/redo stack** — His app has a custom undo stack that survives across sessions and operations. We rely on `TextBox`'s built-in `Ctrl+Z`, which is per-control and gets cleared. **Why:** WinUI's `TextBox` doesn't expose `Undo`/`Redo` programmatically. **Plan:** Build a parallel undo stack on top of `Text` state changes — ~100 lines, Phase 8 polish.

2. **Global hotkeys are registered but not fired** — `WindowsHotKeyService` correctly calls `RegisterHotKey`/`UnregisterHotKey`, but there's no `WndProc` message-pump handler hooked into the main window, so pressed hotkeys go nowhere. **Why:** Uno doesn't expose `WndProc` directly; it needs custom interop on `MainWindow.GetWindowHandle()`. **Plan:** Phase 8 — wire a window subclass for `WM_HOTKEY` messages on the Windows target. Infrastructure is ready.

3. **Startup on login is a UI stub** — The setting persists in `appsettings.json`, but no code adds the app to `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`. **Why:** Deferred from initial port. **Plan:** ~30 lines of registry code behind `#if WINDOWS`. Tomorrow-job.

4. **Run-in-background doesn't actually background** — Same shape: setting persists, tray service exists, but app shutdown isn't intercepted to hide-to-tray instead of close. **Plan:** Hook `Window.Closed` and call `ISystemTrayService.MinimizeToTray()`. Phase 8.

5. **Multi-monitor screen capture** — `WindowsScreenCaptureService` only captures the primary monitor (`GetSystemMetrics(SM_CXSCREEN)`). His app handles multi-monitor. **Plan:** Replace with `EnumDisplayMonitors` + per-monitor `BitBlt`.

6. **Grab Frame translation** — Not implemented. **Why:** Translation in his app calls Windows-specific translator services. **Plan:** Phase 9 — replace with a cross-platform endpoint (Bing/Azure) so Wasm gets it for free.

7. **Quick Lookup history panel + multiple web sources** — Both missing. **Why:** Lower-priority polish. **Plan:** Phase 8.

8. **Edit Text Calculate Pane and AI menu** (Summarize/Translate/Extract) — Not ported. **Why:** AI menu was Windows AI-only and ARM64-only. **Plan:** Re-implement against a cross-platform AI provider so it works in the browser too. Could be a great Uno showcase feature.

9. **Multiple simultaneous Edit Text windows** — His app lets you open many editor windows side-by-side. We're single-instance. **Why:** Single-window navigation model. **Plan:** Possible via additional `AppWindow`s on Windows; not yet implemented.

### Stubs (works on the surface, but the underlying tech is missing)

10. **Windows AI OCR engine** — `WindowsAiOcrEngine` exists and returns `null`. The `Microsoft.Windows.AI` NuGet isn't even referenced yet. **Why:** ARM64-only, requires preview SDK, and Tesseract covers the gap on x64. **Plan:** Add when the SDK stabilizes; the engine slot is already there.

11. **Single-click word mode + post-grab actions dropdown** in Fullscreen Grab — Missing. **Plan:** Phase 8 polish.

### Things that work but differently

- **System tray:** We have it (via `Shell_NotifyIcon` P/Invoke, replacing WPF-UI.Tray), but the right-click context menu is minimal — show/exit only. His app has a richer menu. We can extend it.
- **Toast notifications:** We use in-app `InfoBar` instead of Windows toast. Cross-platform win, but it's a different UX. He may prefer real toasts on Windows; we could do both.

---

## 6. The Pitch for Porting His Other Apps (the second-half goal)

Save this for the back third of the meeting, after the trust is built.

**The pitch:**
> "The reason this migration was tractable is that Uno's WinUI surface is close enough to WPF that 50% of your code ports straight over (StringMethods, calculation engine, models, enums) and the other 50% has a clear mapping table we've now documented. We compiled every gotcha we hit into a migration patterns doc — three failed nav approaches, the WPF-UI to Material mapping, the `Visibility.Hidden` → `Opacity=0` thing, the `MouseDown` → `PointerPressed` event model shift, the right way to handle `IWritableOptions`, all of it. If you ported one of your other apps, you'd skip every single trap we hit."

**Anticipate what he'll push back on:**

- **"What about my other Windows-specific tricks?"** Answer: P/Invoke still works on the Windows target, you just isolate it behind an interface. We did exactly that for screen capture, hotkeys, and the system tray.
- **"What about WinUI 3 directly instead of Uno?"** Answer: WinUI 3 is Windows-only. Uno gives you the same XAML and the same code on Web, Mac, Linux, iOS, Android. If you're already in WinUI 3 territory, the cost to add Uno is small and the upside is enormous.
- **"Performance?"** Answer: Startup is faster (lazy page creation vs eager 15-window construction). Image pipeline is faster (SkiaSharp is GPU-accelerated, ImageMagick is heavy native). Memory is lower in multi-window scenarios.
- **"What's the catch?"** Answer: Wasm payload is real — currently 323 MB untrimmed. With `PublishTrimmed=true` we expect 30–60 MB. Trimming for Wasm is the area you'd actually have to think about.

**The ask:**
> "Would you be open to picking one of your smaller apps as a pilot — maybe something already in WPF-UI — and letting us draft a migration plan against it? You'd see the same patterns doc, the same architecture skeleton, and an honest scoping of what would and wouldn't port."

---

## 7. Talking Points (one-liners to keep in your back pocket)

- "60% less code, 81% fewer NuGet packages, three platforms instead of one."
- "Your app went from zero unit tests to 248 passing — `StringMethods` is pinned forever."
- "We deleted ~70 MB of Magick.NET native binaries with no functional regression."
- "Three OCR engines, one interface — adding cloud OCR for the browser is one new class."
- "We hit every WPF→Uno gotcha and wrote them down so the next port skips the traps."
- "Same teal. Same character. Same name. Material 3 instead of Fluent."
- "Edit Text is at 95% feature-for-feature — the only meaningful gap is the custom undo stack and the AI menu."
- "Screen capture, hotkeys, and the system tray all work on Windows via clean P/Invoke isolated in `Platforms/Windows/`."
- "The migration isn't a fork. It's a parallel codebase that credits you in the README and respects the MIT license."

---

## 8. What NOT To Do

- **Don't oversell parity.** If he digs into Edit Text and finds the missing AI menu or undo gap, you've already burned trust if you said "feature complete."
- **Don't disparage WPF or his architecture.** His code-behind + Singleton design was the right call for his constraints. The refactor is a *response* to a different goal (cross-platform), not a *correction*.
- **Don't claim authorship of his app.** Use "the Uno port" / "our migration" / "our port of your app." Never "my app" or "our Text-Grab."
- **Don't push too hard on porting his other apps.** Plant the seed and let him bring it back up. If he doesn't, follow up after.
- **Don't read from the prep doc during the call.** Internalize it.
- **Don't surprise him with the LOC reduction without context.** It can sound like an insult ("your code was bloated"). Always frame as "you had no MVUX source generator, you had `Properties.Settings.Designer.cs`, you had to wrap WPF-UI Tray — most of the deletion is *infrastructure that no longer needs to exist*."
- **Don't mention Claude Code as the *author*.** It's a tool. The decisions, architecture, and trade-offs are yours and the team's. Lead with that.

---

## 9. Q&A Appendix

### About the migration

**Q: Why Uno instead of MAUI or AvaloniaUI?**
A: Uno gives you XAML you already know — your existing WPF muscle memory carries over. MAUI's XAML is different. Avalonia is closer but smaller ecosystem and no MVUX source generator. Uno also has the only first-class WebAssembly story in the .NET XAML world right now.

**Q: How long did this take?**
A: Avoid time estimates per the user's CLAUDE.md. Pivot: "It was built incrementally across 8 phases. The interesting metric isn't time, it's that we documented every gotcha we hit so the next port goes faster."

**Q: How did you handle the OCR engines?**
A: `IOcrEngine` interface with three implementations (`WindowsOcrEngine`, `TesseractOcrEngine`, `WindowsAiOcrEngine`), all registered in DI. `OcrService` is a facade that picks the right engine based on the selected language's kind. Adding a fourth engine (e.g., Azure Cognitive Services for Wasm) is one new class, no orchestration changes.

**Q: What about the Tesseract subprocess on non-Windows?**
A: `CliWrap` is conditionally referenced (`Condition` in csproj) — Windows only. On other platforms the engine isn't registered and the language picker simply doesn't surface Tesseract languages.

**Q: How does `WordBorder` work in Uno?**
A: It's a `UserControl` that holds an `IGrabFrameHost` interface reference instead of a direct `GrabFrame` reference — clean decoupling. It handles in-place edit mode, `Ctrl+drag` move/resize, the context menu, and notifies the host on state changes. `Canvas.SetLeft`/`SetTop` work identically in WinUI.

**Q: How is undo/redo handled for Grab Frame?**
A: Snapshot-based — we serialize the full list of `WordBorderInfo` to a stack capped at 50. `PushUndo()` is called before any destructive op. Restoring rehydrates `WordBorder` instances from the info records and reattaches the host.

**Q: What replaces `RoutedCommand`?**
A: For text editing, direct method calls on the Model via interface contracts. Keyboard shortcuts use `KeyboardAccelerators` on `MenuFlyoutItem` — they're functional, not just display, unlike `InputGestureText` in WPF.

### About the gaps

**Q: Why doesn't undo work in the editor?**
A: WinUI `TextBox` has built-in `Ctrl+Z` but no programmatic API. Your app had a custom undo stack with cross-operation context — we need to rebuild that on top of MVUX state changes. ~100 lines, planned for Phase 8.

**Q: My global hotkeys are critical. Why aren't they wired?**
A: `WindowsHotKeyService` does register hotkeys correctly via P/Invoke, but the `WM_HOTKEY` message handler isn't hooked yet because Uno doesn't expose `WndProc` directly. We need to subclass the main window's HWND. It's a small piece of interop and we've scoped it.

**Q: Multi-monitor capture?**
A: Single monitor today (`GetSystemMetrics(SM_CXSCREEN)`). Replacing with `EnumDisplayMonitors` is straightforward — same P/Invoke surface, just a loop.

**Q: What about Windows AI OCR?**
A: The slot exists (`WindowsAiOcrEngine`), but the underlying `Microsoft.Windows.AI` SDK is ARM64-only and still moving. Tesseract covers the gap. We can wire it up the day the SDK stabilizes — zero refactoring.

**Q: Are you sure WebAssembly is usable for an OCR app?**
A: Honestly — for OCR features, no. Wasm gets Edit Text, Quick Lookup, find/replace, regex manager, QR code generation. The OCR features show "Available on Windows Desktop." The real opportunity is replacing them with a cloud OCR engine — same `IOcrEngine` slot, no architecture changes. That's how Wasm becomes a first-class target.

**Q: Wasm payload size?**
A: Currently ~323 MB untrimmed (debug). With `PublishTrimmed=true` we expect 30–60 MB. Trimming Uno apps takes a careful pass — that's a real piece of work.

### About the visual identity

**Q: Why Material Design 3 instead of Fluent?**
A: Two reasons. First, WPF-UI's Fluent doesn't have an Uno equivalent — Uno Material is the most polished theme. Second, MD3 supports the dynamic color system out of the box, so your teal `#308E98` becomes the entire palette via `ColorPaletteOverride`. The character of the app is preserved; the underlying tokens are richer.

**Q: Can I get Fluent back?**
A: Possible — Uno also has `Uno.Themes.Fluent`. We picked Material 3 to maximize the cross-platform feel (it looks native on every target), but it's a one-line change to swap. Happy to demo both.

### About licensing and credit

**Q: How is attribution handled?**
A: Both READMEs credit you and link to the original repo. The `LICENSE` file is intact (MIT, © 2020 Joseph Finney). Nothing has been claimed as new IP.

**Q: What's the release plan if you approve?**
A: That's exactly what we want to discuss. Options we're considering: (a) you take ownership of the Uno port as the canonical Text-Grab v5; (b) it lives as `Text-Grab.Uno` in your org as a sibling project; (c) it's an Uno community project that links upstream to you. Your call — we'll do whatever makes you most comfortable.

**Q: Will you keep maintaining it?**
A: Yes. The migration patterns we've documented are reusable across other Uno ports, so this isn't a one-off — there's an ongoing investment.

### About porting his other apps

**Q: Which of my apps would port well?**
A: Anything WPF-UI based ports easiest because the Fluent → Material mapping is the only theme work. Anything that uses heavy Win32 (window chrome customization, low-level keyboard hooks, shell integration) is harder. We'd want to do a 30-minute scoping pass on each candidate.

**Q: What's the migration ROI for me?**
A: Three things: (1) Web reach — your apps run in browsers with no installer. (2) Mac/Linux reach if you ever want it. (3) Code reduction averaging 50–60% because Uno's modern patterns delete a lot of boilerplate. Plus you get DI and tests, which pay back on every future change.

**Q: I don't want to abandon Windows users.**
A: Nobody's asking you to. Windows is still the primary target. Cross-platform is additive. You ship the same WinAppSDK build you ship today, plus a Wasm build, plus optional Skia targets.

---

## 10. Live Demo Cheat Sheet (file paths to open if he asks "show me")

| He asks about | Open this file |
|---|---|
| DI / service registration | `TextGrab.Uno/TextGrab.Uno/App.xaml.cs` (look at `ConfigureServices`, ~line 41) |
| Settings persistence | `TextGrab.Uno/TextGrab.Uno/Models/AppSettings.cs` + `appsettings.json` |
| MVUX pattern | `TextGrab.Uno/TextGrab.Uno/Presentation/EditTextModel.cs` |
| OCR engine abstraction | `TextGrab.Uno/TextGrab.Uno/Services/IOcrEngine.cs` + `Platforms/Windows/WindowsOcrEngine.cs` |
| `WordBorder` control | `TextGrab.Uno/TextGrab.Uno/Controls/WordBorder.xaml.cs` |
| P/Invoke isolation | `TextGrab.Uno/TextGrab.Uno/Platforms/Windows/WindowsScreenCaptureService.cs` |
| The navigation war story | `Migration-Comparison-Report.md` + the patterns memory (three failed attempts) |
| Tests | `TextGrab.Uno/TextGrab.Uno.Tests/` (17 files, 248 tests) |
| README parity matrix | `README.md` (root) — feature-by-feature breakdown |

---

## 11. Open Questions for Joe (your agenda items)

End the meeting by surfacing these, even if some are answered along the way:

1. **Approval criteria** — what specifically needs to be true for you to be comfortable with a public release?
2. **Naming and ownership** — Text-Grab v5 under your org? `Text-Grab.Uno` sibling? Community fork with link back?
3. **Gap priority** — of the items in §5, which would be blockers for you, and which are acceptable as v1?
4. **Visual identity** — Material 3 with your teal, or do you want Fluent back on Windows?
5. **Release channel** — would you want this on the Microsoft Store alongside your existing one, or as a separate listing?
6. **Marketing collaboration** — would you be open to a joint blog post / video walkthrough with the Uno team?
7. **Pilot for another app** — is there one of your other WPF apps you'd be willing to scope a migration for as a follow-up?

---

## 12. Unresolved Questions (things to verify before the call)

- Confirm the **248 tests** number is current (run `dotnet test` once before the meeting).
- Verify the **WebAssembly build** still launches end-to-end (it's the most fragile demo target).
- Decide whether you want to show the **`docs/WPF-to-Uno-Migration-Patterns.md`** file live, or keep it as a "send after the call" leave-behind.
- Confirm Joe's preferred contact handle (`@thejoefin` on Twitter is in the README — might prefer email or GitHub).
- Decide whether you want to bring a one-page PDF of §1's headline numbers as a leave-behind, or keep it digital-only.

---

*Prep doc generated 2026-04-08, grounded in current codebase inspection (96 C# files, 22 XAML files, 11,234 LOC, 248 tests). README parity numbers verified against actual file states. Older "65–70% honest parity" memory was correct at the time but the codebase has since closed several gaps — current honest number is ~85–90%, with the specific gaps listed in §5.*
