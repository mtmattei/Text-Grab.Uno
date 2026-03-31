#if WINDOWS
using System.Runtime.InteropServices;

namespace TextGrab.Services;

/// <summary>
/// Windows global hotkey registration using RegisterHotKey P/Invoke.
/// Hotkeys work system-wide even when the app is in the background.
/// </summary>
public class WindowsHotKeyService : IHotKeyService, IDisposable
{
    private readonly List<int> _registeredIds = [];
    private nint _hwnd;

    // Standard hotkey IDs
    public const int FullscreenGrabId = 1;
    public const int GrabFrameId = 2;
    public const int EditTextId = 3;
    public const int QuickLookupId = 4;

    // Modifier constants
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    public bool IsSupported => true;

    public event EventHandler<int>? HotKeyPressed;

    public void SetWindowHandle(nint hwnd)
    {
        _hwnd = hwnd;
    }

    public bool RegisterHotKey(int id, uint modifiers, uint key)
    {
        if (_hwnd == 0) return false;

        bool result = NativeRegisterHotKey(_hwnd, id, modifiers | MOD_NOREPEAT, key);
        if (result)
            _registeredIds.Add(id);
        return result;
    }

    public bool UnregisterHotKey(int id)
    {
        if (_hwnd == 0) return false;

        bool result = NativeUnregisterHotKey(_hwnd, id);
        if (result)
            _registeredIds.Remove(id);
        return result;
    }

    public void UnregisterAll()
    {
        foreach (var id in _registeredIds.ToList())
            UnregisterHotKey(id);
    }

    /// <summary>
    /// Call from WndProc when WM_HOTKEY (0x0312) is received.
    /// </summary>
    public void HandleHotKeyMessage(int hotkeyId)
    {
        HotKeyPressed?.Invoke(this, hotkeyId);
    }

    public void Dispose()
    {
        UnregisterAll();
    }

    [DllImport("user32.dll", EntryPoint = "RegisterHotKey")]
    private static extern bool NativeRegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", EntryPoint = "UnregisterHotKey")]
    private static extern bool NativeUnregisterHotKey(nint hWnd, int id);
}
#endif
