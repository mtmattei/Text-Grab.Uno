using Uno.Extensions.Configuration;

namespace TextGrab.Presentation;

public partial record TesseractSettingsModel
{
    private readonly IWritableOptions<AppSettings> _settings;

    public TesseractSettingsModel(IWritableOptions<AppSettings> settings)
    {
        _settings = settings;
    }

    public IState<bool> UseTesseract => State<bool>.Value(this, () => _settings.Value?.UseTesseract ?? false);
    public IState<string> TesseractPath => State<string>.Value(this, () => _settings.Value?.TesseractPath ?? "");
    public IState<bool> TesseractFound => State<bool>.Value(this, () => false);

    public async ValueTask ToggleUseTesseract()
    {
        var current = await UseTesseract;
        await UseTesseract.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { UseTesseract = !current });
    }

    public async ValueTask SetTesseractPath(string path)
    {
        await TesseractPath.Set(path, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { TesseractPath = path });

#if WINDOWS
        var found = !string.IsNullOrEmpty(path) && System.IO.File.Exists(path);
        await TesseractFound.Set(found, CancellationToken.None);
#endif
    }
}
