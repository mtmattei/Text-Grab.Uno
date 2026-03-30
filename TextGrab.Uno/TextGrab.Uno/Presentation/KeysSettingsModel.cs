using Uno.Extensions.Configuration;

namespace TextGrab.Presentation;

public partial record KeysSettingsModel
{
    private readonly IWritableOptions<AppSettings> _settings;

    public KeysSettingsModel(IWritableOptions<AppSettings> settings)
    {
        _settings = settings;
    }

    public IState<bool> RunInTheBackground => State<bool>.Value(this, () => _settings.Value?.RunInTheBackground ?? false);
    public IState<bool> GlobalHotkeysEnabled => State<bool>.Value(this, () => _settings.Value?.GlobalHotkeysEnabled ?? false);

    public async ValueTask ToggleRunInBackground()
    {
        var current = await RunInTheBackground;
        await RunInTheBackground.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { RunInTheBackground = !current });
    }

    public async ValueTask ToggleGlobalHotkeys()
    {
        var current = await GlobalHotkeysEnabled;
        await GlobalHotkeysEnabled.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { GlobalHotkeysEnabled = !current });
    }
}
