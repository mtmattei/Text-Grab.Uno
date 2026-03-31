namespace TextGrab.Services;

/// <summary>
/// Registers and manages global keyboard shortcuts.
/// Windows: Uses RegisterHotKey/UnregisterHotKey P/Invoke.
/// Other platforms: Not supported.
/// </summary>
public interface IHotKeyService
{
    bool IsSupported { get; }
    bool RegisterHotKey(int id, uint modifiers, uint key);
    bool UnregisterHotKey(int id);
    void UnregisterAll();
    event EventHandler<int>? HotKeyPressed;
}
