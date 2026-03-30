using Uno.Extensions.Configuration;

namespace TextGrab.Presentation;

public partial record FullscreenGrabSettingsModel
{
    private readonly IWritableOptions<AppSettings> _settings;

    public FullscreenGrabSettingsModel(IWritableOptions<AppSettings> settings)
    {
        _settings = settings;
    }

    public IState<string> FsgDefaultMode => State<string>.Value(this, () => _settings.Value?.FsgDefaultMode ?? "Default");
    public IState<bool> FsgSendEtwToggle => State<bool>.Value(this, () => _settings.Value?.FsgSendEtwToggle ?? false);
    public IState<bool> FsgShadeOverlay => State<bool>.Value(this, () => _settings.Value?.FsgShadeOverlay ?? true);
    public IState<bool> TryInsert => State<bool>.Value(this, () => _settings.Value?.TryInsert ?? false);
    public IState<double> InsertDelay => State<double>.Value(this, () => _settings.Value?.InsertDelay ?? 0.3);

    public async ValueTask SetDefaultMode(string mode)
    {
        await FsgDefaultMode.Set(mode, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { FsgDefaultMode = mode });
    }

    public async ValueTask ToggleSendEtw()
    {
        var current = await FsgSendEtwToggle;
        await FsgSendEtwToggle.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { FsgSendEtwToggle = !current });
    }

    public async ValueTask ToggleShadeOverlay()
    {
        var current = await FsgShadeOverlay;
        await FsgShadeOverlay.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { FsgShadeOverlay = !current });
    }

    public async ValueTask ToggleTryInsert()
    {
        var current = await TryInsert;
        await TryInsert.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { TryInsert = !current });
    }

    public async ValueTask SetInsertDelay(double delay)
    {
        await InsertDelay.Set(delay, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { InsertDelay = delay });
    }
}
