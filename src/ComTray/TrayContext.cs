using System.Text.RegularExpressions;

namespace ComTray;

partial class TrayContext : ApplicationContext
{
    readonly NotifyIcon tray = new();
    readonly MessageWindow messageWindow;
    readonly System.Windows.Forms.Timer debounce = new() { Interval = 300 };
    readonly PortNameStore names = PortNameStore.Load();
    readonly Dictionary<string, PortInfo> ports = new(StringComparer.OrdinalIgnoreCase);

    Icon? currentIcon;
    int? shownNumber;

    public TrayContext()
    {
        Log.Start();

        tray.ContextMenuStrip = new ContextMenuStrip();
        tray.ContextMenuStrip.Opening += (_, _) => BuildMenu();

        // Record everything present at launch so existing ports (Bluetooth,
        // onboard serial, anything plugged in before we started) stay silent.
        foreach (var port in SerialPortScanner.Scan())
            ports[port.PortName] = port;
        Log.Line($"baseline: {string.Join(", ", ports.Keys.OrderBy(k => k))}");

        shownNumber = HighestNumber();
        UpdateIcon();
        tray.Visible = true;

        debounce.Tick += (_, _) => { debounce.Stop(); Refresh(); };

        messageWindow = new MessageWindow();
        messageWindow.DeviceChanged += OnDeviceChanged;
    }

    void OnDeviceChanged()
    {
        // Device arrival fires a burst of messages. Collapse them into one scan.
        debounce.Stop();
        debounce.Start();
    }

    void Refresh()
    {
        var scan = SerialPortScanner.Scan();
        var current = scan.ToDictionary(p => p.PortName, StringComparer.OrdinalIgnoreCase);

        var added = scan.Where(p => !ports.ContainsKey(p.PortName)).ToList();
        var removed = ports.Keys.Where(name => !current.ContainsKey(name)).ToList();

        if (added.Count == 0 && removed.Count == 0)
            return;

        Log.Line($"change: added [{string.Join(", ", added.Select(p => p.PortName))}] removed [{string.Join(", ", removed)}]");

        ports.Clear();
        foreach (var port in scan)
            ports[port.PortName] = port;

        foreach (var port in added.OrderBy(p => p.Number))
            Notify(port);

        if (added.Count > 0)
            shownNumber = added.Max(p => p.Number);
        else if (shownNumber is int n && !ports.Values.Any(p => p.Number == n))
            shownNumber = HighestNumber();

        UpdateIcon();
    }

    void Notify(PortInfo port)
    {
        tray.BalloonTipTitle = "New COM port";
        tray.BalloonTipText = Announce(port);
        tray.ShowBalloonTip(5000);
    }

    string Announce(PortInfo port)
    {
        string device = string.IsNullOrEmpty(port.Description) ? "a serial device" : port.Description;
        if (!port.IsUsb)
            return $"{port.PortName} is the new port for {device}.";

        return $"{port.PortName} is the new port for {device}, plugged into {SocketLabel(port)}.";
    }

    // The label belongs to the physical USB socket, not the cable or the COM
    // number. Whatever is plugged into that socket reports the same name.
    string SocketLabel(PortInfo port)
    {
        var custom = names.Get(port.UsbKey);
        if (!string.IsNullOrEmpty(custom))
            return custom;

        var hint = Humanize(port.LocationInfo);
        return string.IsNullOrEmpty(hint) ? "an unnamed USB port" : hint;
    }

    void UpdateIcon()
    {
        string text = shownNumber?.ToString() ?? "-";
        var icon = IconRenderer.Render(text);
        tray.Icon = icon;
        currentIcon?.Dispose();
        currentIcon = icon;
        tray.Text = shownNumber is int n ? $"COM{n} (latest)" : "No COM ports";
    }

    void BuildMenu()
    {
        var menu = tray.ContextMenuStrip!;
        menu.Items.Clear();

        menu.Items.Add(new ToolStripMenuItem(
            shownNumber is int n ? $"Latest: COM{n}" : "No COM ports detected") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());

        foreach (var port in ports.Values.OrderBy(p => p.Number))
        {
            string label = port.PortName;
            if (!string.IsNullOrEmpty(port.Description))
                label += $"  -  {port.Description}";
            if (port.IsUsb)
                label += $"  -  {SocketLabel(port)}";

            var item = new ToolStripMenuItem(label);
            if (port.IsUsb)
            {
                var captured = port;
                item.DropDownItems.Add(new ToolStripMenuItem("Name this USB port...", null, (_, _) => RenameSocket(captured)));
            }
            else
            {
                item.DropDownItems.Add(new ToolStripMenuItem("Not a USB port") { Enabled = false });
            }
            menu.Items.Add(item);
        }

        if (ports.Count > 0)
            menu.Items.Add(new ToolStripSeparator());

        var startup = new ToolStripMenuItem("Start with Windows")
        {
            Checked = Startup.IsEnabled,
            CheckOnClick = true
        };
        startup.Click += (_, _) => Startup.Set(startup.Checked);
        menu.Items.Add(startup);

        menu.Items.Add(new ToolStripMenuItem("Open activity log", null, (_, _) => Log.Open()));
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => Quit()));
    }

    void RenameSocket(PortInfo port)
    {
        string hint = Humanize(port.LocationInfo);
        string subtitle = string.IsNullOrEmpty(hint)
            ? $"{port.PortName} ({port.Description}) is plugged in here."
            : $"{port.PortName} ({port.Description}) is plugged into {hint}.";

        string current = names.Get(port.UsbKey) ?? "";
        string? result = RenameDialog.Show("Name this USB port", subtitle, current);
        if (result == null)
            return;

        if (string.IsNullOrWhiteSpace(result))
            names.Remove(port.UsbKey);
        else
            names.Set(port.UsbKey, result.Trim());

        names.Save();
    }

    void Quit()
    {
        tray.Visible = false;
        ExitThread();
    }

    int? HighestNumber()
    {
        int max = ports.Values.Select(p => p.Number).DefaultIfEmpty(-1).Max();
        return max >= 0 ? max : null;
    }

    static string Humanize(string location)
    {
        if (string.IsNullOrEmpty(location))
            return "";

        var m = HubPort().Match(location);
        if (m.Success)
            return $"USB hub {int.Parse(m.Groups[2].Value)}, port {int.Parse(m.Groups[1].Value)}";

        return location;
    }

    [GeneratedRegex(@"Port_#(\d+)\.Hub_#(\d+)")]
    private static partial Regex HubPort();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            debounce.Dispose();
            messageWindow.Dispose();
            tray.Dispose();
            currentIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
