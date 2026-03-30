using Uno.Extensions.Configuration;

namespace TextGrab.Presentation;

public partial record GeneralSettingsModel
{
    private readonly IWritableOptions<AppSettings> _settings;

    public GeneralSettingsModel(IWritableOptions<AppSettings> settings)
    {
        _settings = settings;
    }

    // Theme
    public IState<string> AppTheme => State<string>.Value(this, () => _settings.Value?.AppTheme ?? "System");

    // Default launch
    public IState<string> DefaultLaunch => State<string>.Value(this, () => _settings.Value?.DefaultLaunch ?? "EditText");

    // Toggles
    public IState<bool> ShowToast => State<bool>.Value(this, () => _settings.Value?.ShowToast ?? true);
    public IState<bool> RunInTheBackground => State<bool>.Value(this, () => _settings.Value?.RunInTheBackground ?? false);
    public IState<bool> StartupOnLogin => State<bool>.Value(this, () => _settings.Value?.StartupOnLogin ?? false);
    public IState<bool> ReadBarcodesOnGrab => State<bool>.Value(this, () => _settings.Value?.ReadBarcodesOnGrab ?? false);
    public IState<bool> CorrectErrors => State<bool>.Value(this, () => _settings.Value?.CorrectErrors ?? true);
    public IState<bool> CorrectToLatin => State<bool>.Value(this, () => _settings.Value?.CorrectToLatin ?? true);
    public IState<bool> NeverAutoUseClipboard => State<bool>.Value(this, () => _settings.Value?.NeverAutoUseClipboard ?? false);
    public IState<bool> TryInsert => State<bool>.Value(this, () => _settings.Value?.TryInsert ?? false);
    public IState<bool> UseHistory => State<bool>.Value(this, () => _settings.Value?.UseHistory ?? true);

    // Insert delay
    public IState<double> InsertDelay => State<double>.Value(this, () => _settings.Value?.InsertDelay ?? 0.3);

    // Web search
    public IState<string> WebSearchUrl => State<string>.Value(this, () => _settings.Value?.WebSearchUrl ?? "https://www.google.com/search?q=");

    // --- Commands ---

    public async ValueTask SetTheme(string theme)
    {
        await AppTheme.Set(theme, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { AppTheme = theme });
    }

    public async ValueTask SetDefaultLaunch(string launch)
    {
        await DefaultLaunch.Set(launch, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { DefaultLaunch = launch });
    }

    public async ValueTask ToggleShowToast()
    {
        var current = await ShowToast;
        await ShowToast.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { ShowToast = !current });
    }

    public async ValueTask ToggleRunInBackground()
    {
        var current = await RunInTheBackground;
        await RunInTheBackground.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { RunInTheBackground = !current });
    }

    public async ValueTask ToggleStartupOnLogin()
    {
        var current = await StartupOnLogin;
        await StartupOnLogin.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { StartupOnLogin = !current });
    }

    public async ValueTask ToggleBarcodes()
    {
        var current = await ReadBarcodesOnGrab;
        await ReadBarcodesOnGrab.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { ReadBarcodesOnGrab = !current });
    }

    public async ValueTask ToggleCorrectErrors()
    {
        var current = await CorrectErrors;
        await CorrectErrors.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { CorrectErrors = !current });
    }

    public async ValueTask ToggleCorrectToLatin()
    {
        var current = await CorrectToLatin;
        await CorrectToLatin.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { CorrectToLatin = !current });
    }

    public async ValueTask ToggleNeverAutoClipboard()
    {
        var current = await NeverAutoUseClipboard;
        await NeverAutoUseClipboard.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { NeverAutoUseClipboard = !current });
    }

    public async ValueTask ToggleTryInsert()
    {
        var current = await TryInsert;
        await TryInsert.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { TryInsert = !current });
    }

    public async ValueTask ToggleUseHistory()
    {
        var current = await UseHistory;
        await UseHistory.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { UseHistory = !current });
    }

    public async ValueTask SetInsertDelay(double delay)
    {
        await InsertDelay.Set(delay, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { InsertDelay = delay });
    }

    public async ValueTask SetWebSearchUrl(string url)
    {
        await WebSearchUrl.Set(url, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { WebSearchUrl = url });
    }
}
