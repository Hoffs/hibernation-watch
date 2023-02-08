using System.Runtime.InteropServices;
using System.Text;

namespace HibernationWatch;

public class WinApi
{
    // Message loop
    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetMessage(out Message lpMsg, IntPtr hwnd, int wMsgFilterMin, int wMsgFilterMax);

    [DllImport("user32.dll")]
    public static extern int TranslateMessage(Message lpMsg);

    [DllImport("user32.dll")]
    public static extern int DispatchMessage(Message lpMsg);
}

// From System.Windows.Forms source
public struct Message
{
    IntPtr hWnd;
    int msg;
    IntPtr wparam;
    IntPtr lparam;
    IntPtr result;


    public IntPtr HWnd
    {
        get { return hWnd; }
        set { hWnd = value; }
    }

    public int Msg
    {
        get { return msg; }
        set { msg = value; }
    }

    public IntPtr WParam
    {
        get { return wparam; }
        set { wparam = value; }
    }

    public IntPtr LParam
    {
        get { return lparam; }
        set { lparam = value; }
    }

    public IntPtr Result
    {
        get { return result; }
        set { result = value; }
    }
}