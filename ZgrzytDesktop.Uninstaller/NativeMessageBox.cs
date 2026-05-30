using System.Runtime.InteropServices;

namespace ZgrzytDesktop.Uninstaller;

public sealed class NativeMessageBox : IUserNotifier
{
    public const string NotFoundMessage =
        "ZgrzytDesktop nie jest zainstalowany albo deinstalator nie został znaleziony.";

    public const string Caption = "ZgrzytDesktop";

    private const uint MbOk = 0x00000000;
    private const uint MbIconError = 0x00000010;

    public void ShowNotFound()
    {
        MessageBoxW(IntPtr.Zero, NotFoundMessage, Caption, MbOk | MbIconError);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
