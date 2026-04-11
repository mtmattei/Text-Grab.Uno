namespace TextGrab.Models;

public record AppSettings
{
    public string DefaultLaunch { get; init; } = "EditText";
    public string AppTheme { get; init; } = "System";
    public bool FirstRun { get; init; } = true;
    public bool ShowToast { get; init; } = true;
    public bool RunInTheBackground { get; init; } = false;
    public bool StartupOnLogin { get; init; } = false;
    public bool ReadBarcodesOnGrab { get; init; } = false;
    public bool CorrectToLatin { get; init; } = true;
    public bool CorrectErrors { get; init; } = true;
    public bool NeverAutoUseClipboard { get; init; } = false;
    public bool TryInsert { get; init; } = false;
    public double InsertDelay { get; init; } = 0.3;
    public bool UseTesseract { get; init; } = false;
    public string TesseractPath { get; init; } = "";
    public string LastUsedLang { get; init; } = "";

    // Font settings
    public string FontFamilySetting { get; init; } = "Segoe UI";
    public double FontSizeSetting { get; init; } = 14.0;
    public bool IsFontBold { get; init; } = false;
    public bool IsFontItalic { get; init; } = false;
    public bool IsFontUnderline { get; init; } = false;
    public bool IsFontStrikeout { get; init; } = false;

    // Edit Text Window
    public bool EditWindowIsWordWrapOn { get; init; } = true;
    public bool EditWindowIsOnTop { get; init; } = false;
    public bool EditWindowStartFullscreen { get; init; } = false;
    public bool EditWindowBottomBarIsHidden { get; init; } = false;
    public bool RestoreEtwPositions { get; init; } = false;
    public bool EtwUseMargins { get; init; } = false;
    public bool EtwShowLangPicker { get; init; } = false;
    public bool EtwShowWordCount { get; init; } = false;
    public bool EtwShowCharDetails { get; init; } = false;
    public bool ShowCursorText { get; init; } = false;

    // Fullscreen Grab
    public bool FsgShadeOverlay { get; init; } = true;
    public bool FsgSendEtwToggle { get; init; } = false;
    public string FsgDefaultMode { get; init; } = "Default";
    public bool PostGrabStayOpen { get; init; } = false;
    // Enabled post-grab action keys (comma-separated)
    public string PostGrabActionsEnabled { get; init; } = "";

    // Grab Frame
    public double GrabFrameWidth { get; init; } = 800;
    public double GrabFrameHeight { get; init; } = 450;
    public double GrabFramePositionLeft { get; init; } = 100;
    public double GrabFramePositionTop { get; init; } = 100;

    // Quick Lookup
    public string LookupFileLocation { get; init; } = "";
    public bool LookupSearchHistory { get; init; } = false;

    // History
    public bool UseHistory { get; init; } = true;

    // Hotkeys (Windows-only)
    public bool GlobalHotkeysEnabled { get; init; } = false;

    // Danger
    public bool OverrideAiArchCheck { get; init; } = false;

    // Regex
    public string RegexList { get; init; } = "";

    // Recent files (JSON array of paths)
    public string RecentFiles { get; init; } = "";

    // Other
    public string WebSearchUrl { get; init; } = "https://www.google.com/search?q=";
    public string CustomBottomBarItems { get; init; } = "";
}
