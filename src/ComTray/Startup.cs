using Microsoft.Win32;

namespace ComTray;

static class Startup
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "ComTray";

    static string ExePath => Environment.ProcessPath ?? Application.ExecutablePath;

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
    }

    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
            key.SetValue(ValueName, $"\"{ExePath}\"");
        else
            key.DeleteValue(ValueName, false);
    }
}
