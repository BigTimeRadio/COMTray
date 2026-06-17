using System.Diagnostics;

namespace ComTray;

// Activity only gets written when devices actually change, so this stays out of
// the way at idle. Useful for confirming the tool is seeing plug events.
static class Log
{
    static readonly string file = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ComTray", "activity.log");
    static readonly object gate = new();

    static Log()
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(file)!); }
        catch { }
    }

    public static void Start()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            if (File.Exists(file) && new FileInfo(file).Length > 1_000_000)
                File.Delete(file);
        }
        catch { }

        Line("started");
    }

    public static void Line(string message)
    {
        try
        {
            lock (gate)
                File.AppendAllText(file, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {message}{Environment.NewLine}");
        }
        catch { }
    }

    public static void Open()
    {
        try
        {
            Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
        }
        catch { }
    }
}
