using System.ComponentModel;

namespace HibernationWatch;

public static class WinMessagePump
{
    public static Thread Start(CancellationToken cancellationToken)
    {
        var messagePump = new Thread(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = WinApi.GetMessage(out var msg, IntPtr.Zero, 0, 0);
                if (result == 0) break;
                if (result == -1) throw new Win32Exception();
                WinApi.TranslateMessage(msg);
                WinApi.DispatchMessage(msg);
            }
        });

        messagePump.Start();

        return messagePump;
    }
}