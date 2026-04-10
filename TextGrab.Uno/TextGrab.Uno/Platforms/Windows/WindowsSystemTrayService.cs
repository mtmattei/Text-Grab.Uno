#if WINDOWS
using System.Drawing;
using System.Runtime.InteropServices;

namespace TextGrab.Services;

/// <summary>
/// Windows system tray (NotifyIcon) via Shell_NotifyIcon P/Invoke.
/// Shows a tray icon when RunInBackground is enabled.
/// </summary>
public class WindowsSystemTrayService : ISystemTrayService, IDisposable
{
    private const int WM_USER_TRAYICON = 0x0400 + 1;
    private const int NIM_ADD = 0x00;
    private const int NIM_DELETE = 0x02;
    private const int NIM_MODIFY = 0x01;
    private const int NIF_MESSAGE = 0x01;
    private const int NIF_ICON = 0x02;
    private const int NIF_TIP = 0x04;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;

    private NOTIFYICONDATA _nid;
    private bool _isVisible;
    private nint _hwnd;

    public bool IsSupported => true;

    public event EventHandler? ShowWindowRequested;
    public event EventHandler? ExitRequested;

    public void SetWindowHandle(nint hwnd)
    {
        _hwnd = hwnd;
    }

    public void ShowTrayIcon(string tooltip = "Text Grab")
    {
        if (_hwnd == 0 || _isVisible) return;

        _nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_USER_TRAYICON,
            hIcon = LoadIcon(0, 32512), // IDI_APPLICATION default icon
            szTip = tooltip,
        };

        Shell_NotifyIcon(NIM_ADD, ref _nid);
        _isVisible = true;
    }

    public void HideTrayIcon()
    {
        if (!_isVisible) return;
        Shell_NotifyIcon(NIM_DELETE, ref _nid);
        _isVisible = false;
    }

    /// <summary>
    /// Call from WndProc when WM_USER_TRAYICON is received.
    /// </summary>
    public void HandleTrayMessage(nint lParam)
    {
        int msg = (int)(lParam & 0xFFFF);
        switch (msg)
        {
            case WM_LBUTTONDBLCLK:
                ShowWindowRequested?.Invoke(this, EventArgs.Empty);
                break;
            case WM_RBUTTONUP:
                ExitRequested?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    public void Dispose()
    {
        HideTrayIcon();
    }

    [DllImport("shell32.dll")]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll")]
    private static extern nint LoadIcon(nint hInstance, int lpIconName);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public nint hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public nint hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }
}
#endif
