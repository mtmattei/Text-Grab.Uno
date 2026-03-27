#if WINDOWS
using CliWrap;
using CliWrap.Buffered;
using System.Text;

namespace TextGrab.Services;

/// <summary>
/// IOcrEngine implementation using Tesseract OCR via subprocess (CliWrap).
/// Only available when tesseract.exe is installed locally.
/// </summary>
public class TesseractOcrEngine : IOcrEngine
{
    private readonly IOptions<AppSettings> _settings;
    private string? _resolvedPath;

    private static readonly string[] _searchPaths =
    [
        @"%LOCALAPPDATA%\Tesseract-OCR\tesseract.exe",
        @"%LOCALAPPDATA%\Programs\Tesseract-OCR\tesseract.exe",
        @"C:\Program Files\Tesseract-OCR\tesseract.exe",
    ];

    public TesseractOcrEngine(IOptions<AppSettings> settings)
    {
        _settings = settings;
    }

    public string Name => "Tesseract";
    public OcrEngineKind Kind => OcrEngineKind.Tesseract;

    public bool IsAvailable
    {
        get
        {
            if (!_settings.Value.UseTesseract)
                return false;

            return !string.IsNullOrEmpty(GetTesseractPath());
        }
    }

    public async Task<IOcrLinesWords?> RecognizeAsync(Stream imageStream, ILanguage language, CancellationToken ct = default)
    {
        string tesseractPath = GetTesseractPath();
        if (string.IsNullOrEmpty(tesseractPath))
            return null;

        // Write stream to temp file (Tesseract requires file input)
        string tempPath = Path.Combine(Path.GetTempPath(), $"textgrab_{Guid.NewGuid():N}.png");
        try
        {
            await using (var fs = File.Create(tempPath))
            {
                await imageStream.CopyToAsync(fs, ct);
            }

            string langTag = language is TessLang tessLang ? tessLang.RawTag : "eng";

            var result = await Cli.Wrap(tesseractPath)
                .WithValidation(CommandResultValidation.None)
                .WithArguments(args => args
                    .Add(tempPath)
                    .Add("-")
                    .Add("-l")
                    .Add(langTag))
                .ExecuteBufferedAsync(Encoding.UTF8, ct);

            string text = result.StandardOutput;

            // Tesseract plain text doesn't provide bounding boxes.
            // Return a SimpleOcrLinesWords with one line per text line.
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => (IOcrLine)new SimpleOcrLine
                {
                    Text = line.TrimEnd('\r'),
                    Words = line.TrimEnd('\r').Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(w => (IOcrWord)new SimpleOcrWord { Text = w })
                        .ToArray()
                })
                .ToArray();

            return new SimpleOcrLinesWords
            {
                Text = text,
                Lines = lines,
                Angle = 0,
            };
        }
        finally
        {
            try { File.Delete(tempPath); } catch { /* best effort cleanup */ }
        }
    }

    public async Task<IReadOnlyList<ILanguage>> GetAvailableLanguagesAsync(CancellationToken ct = default)
    {
        string tesseractPath = GetTesseractPath();
        if (string.IsNullOrEmpty(tesseractPath))
            return [];

        try
        {
            var result = await Cli.Wrap(tesseractPath)
                .WithValidation(CommandResultValidation.None)
                .WithArguments(args => args.Add("--list-langs"))
                .ExecuteBufferedAsync(ct);

            if (string.IsNullOrWhiteSpace(result.StandardOutput))
                return [new TessLang("eng")];

            var languages = result.StandardOutput
                .Split(Environment.NewLine)
                .Where(item => item.Length < 30 && !string.IsNullOrWhiteSpace(item) && item != "osd")
                .Select(tag => (ILanguage)new TessLang(tag))
                .ToList();

            return languages;
        }
        catch
        {
            return [new TessLang("eng")];
        }
    }

    private string GetTesseractPath()
    {
        if (_resolvedPath is not null)
            return _resolvedPath;

        // Check settings first
        string settingsPath = _settings.Value.TesseractPath;
        if (!string.IsNullOrWhiteSpace(settingsPath) && File.Exists(settingsPath))
        {
            _resolvedPath = settingsPath;
            return _resolvedPath;
        }

        // Search known locations
        foreach (string rawPath in _searchPaths)
        {
            string expanded = Environment.ExpandEnvironmentVariables(rawPath);
            if (File.Exists(expanded))
            {
                _resolvedPath = expanded;
                return _resolvedPath;
            }
        }

        _resolvedPath = string.Empty;
        return _resolvedPath;
    }
}
#endif
