using Microsoft.Extensions.DependencyInjection;

namespace TextGrab.Shared;

/// <summary>
/// Unified service resolution for code-behind pages.
/// Replaces per-page GetService helpers and raw ((App)Application.Current) casts.
/// </summary>
public static class ServiceExtensions
{
    public static T? GetService<T>(this FrameworkElement element) where T : class
        => ((App)Application.Current).Host?.Services.GetService<T>();
}
