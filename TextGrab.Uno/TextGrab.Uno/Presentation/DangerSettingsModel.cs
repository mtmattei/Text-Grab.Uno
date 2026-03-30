using Uno.Extensions.Configuration;

namespace TextGrab.Presentation;

public partial record DangerSettingsModel
{
    private readonly IWritableOptions<AppSettings> _settings;

    public DangerSettingsModel(IWritableOptions<AppSettings> settings)
    {
        _settings = settings;
    }

    public IState<bool> OverrideAiArchCheck => State<bool>.Value(this, () => _settings.Value?.OverrideAiArchCheck ?? false);
    public IState<string> StatusMessage => State<string>.Value(this, () => "");

    public async ValueTask ToggleAiArchOverride()
    {
        var current = await OverrideAiArchCheck;
        await OverrideAiArchCheck.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { OverrideAiArchCheck = !current });
    }

    public async ValueTask ResetAllSettings()
    {
        var defaults = new AppSettings();
        await _settings.UpdateAsync(_ => defaults);
        await StatusMessage.Set("All settings have been reset to defaults.", CancellationToken.None);
    }
}
