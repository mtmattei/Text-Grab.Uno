#if WINDOWS
namespace TextGrab.Services;

/// <summary>
/// IOcrEngine implementation using Windows AI (Microsoft.Windows.AI.Imaging).
/// Requires Windows 11 ARM64+ with AI features installed.
///
/// NOTE: This engine is currently a stub. To activate:
/// 1. Add Microsoft.Windows.AI NuGet package
/// 2. Uncomment WinAiOcrLinesWords.cs
/// 3. Uncomment the implementation below
/// </summary>
public class WindowsAiOcrEngine : IOcrEngine
{
    public string Name => "Windows AI";
    public OcrEngineKind Kind => OcrEngineKind.Windows;

    public bool IsAvailable
    {
        get
        {
            // TODO: Uncomment when Windows AI SDK is added
            // try
            // {
            //     return RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            //         && Microsoft.Windows.AI.Imaging.TextRecognizer.GetReadyState()
            //             == Microsoft.Windows.AI.AIFeatureReadyState.Ready;
            // }
            // catch { }
            return false;
        }
    }

    public Task<IOcrLinesWords?> RecognizeAsync(Stream imageStream, ILanguage language, CancellationToken ct = default)
    {
        // TODO: Implement when Windows AI SDK is added
        // 1. Convert stream to SoftwareBitmap
        // 2. Create TextRecognizer
        // 3. Recognize and wrap as WinAiOcrLinesWords
        return Task.FromResult<IOcrLinesWords?>(null);
    }

    public Task<IReadOnlyList<ILanguage>> GetAvailableLanguagesAsync(CancellationToken ct = default)
    {
        if (!IsAvailable)
            return Task.FromResult<IReadOnlyList<ILanguage>>([]);

        // Windows AI supports all languages (model-based, not language-pack-based)
        return Task.FromResult<IReadOnlyList<ILanguage>>(new List<ILanguage> { new WindowsAiLang() });
    }
}
#endif
