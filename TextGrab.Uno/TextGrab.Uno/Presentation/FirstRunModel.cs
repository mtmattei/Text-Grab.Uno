using Uno.Extensions.Configuration;

namespace TextGrab.Presentation;

public partial record FirstRunModel
{
    private readonly INavigator _navigator;
    private readonly IWritableOptions<AppSettings> _settings;

    public FirstRunModel(
        INavigator navigator,
        IWritableOptions<AppSettings> settings)
    {
        _navigator = navigator;
        _settings = settings;
    }

    public IState<string> DefaultLaunch => State<string>.Value(this, () => _settings.Value?.DefaultLaunch ?? "EditText");
    public IState<bool> ShowToast => State<bool>.Value(this, () => _settings.Value?.ShowToast ?? true);
    public IState<bool> RunInTheBackground => State<bool>.Value(this, () => _settings.Value?.RunInTheBackground ?? false);
    public IState<bool> StartupOnLogin => State<bool>.Value(this, () => _settings.Value?.StartupOnLogin ?? false);

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

    public async ValueTask CompleteFirstRun()
    {
        await _settings.UpdateAsync(s => s with { FirstRun = false });
    }

    public async ValueTask NavigateToShell()
    {
        await CompleteFirstRun();
        await _navigator.NavigateRouteAsync(this, "EditText");
    }

    public async ValueTask NavigateToSettings()
    {
        await CompleteFirstRun();
        await _navigator.NavigateRouteAsync(this, "Settings");
    }
}
