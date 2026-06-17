using System.Threading;

namespace ComTray;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "ComTray.SingleInstance", out bool created);
        if (!created)
            return;

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayContext());

        GC.KeepAlive(mutex);
    }
}
