using System.Runtime.InteropServices;

namespace ComTray;

class MessageWindow : NativeWindow, IDisposable
{
    const int WM_DEVICECHANGE = 0x0219;
    const int DBT_DEVTYP_DEVICEINTERFACE = 5;
    const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;
    const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4;

    public event Action? DeviceChanged;

    IntPtr registration;

    public MessageWindow()
    {
        CreateHandle(new CreateParams());
        Register();
    }

    // Ask Windows to deliver arrival and removal events straight to this window
    // rather than depending on the WM_DEVICECHANGE broadcast finding us.
    void Register()
    {
        var filter = new DEV_BROADCAST_DEVICEINTERFACE
        {
            dbcc_size = Marshal.SizeOf<DEV_BROADCAST_DEVICEINTERFACE>(),
            dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE
        };

        IntPtr buffer = Marshal.AllocHGlobal(filter.dbcc_size);
        try
        {
            Marshal.StructureToPtr(filter, buffer, false);
            registration = RegisterDeviceNotification(Handle, buffer,
                DEVICE_NOTIFY_WINDOW_HANDLE | DEVICE_NOTIFY_ALL_INTERFACE_CLASSES);
            Log.Line(registration == IntPtr.Zero
                ? $"RegisterDeviceNotification failed, error {Marshal.GetLastWin32Error()}"
                : "device notifications registered");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_DEVICECHANGE)
        {
            Log.Line($"WM_DEVICECHANGE event 0x{(long)m.WParam:X4}");
            DeviceChanged?.Invoke();
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (registration != IntPtr.Zero)
        {
            UnregisterDeviceNotification(registration);
            registration = IntPtr.Zero;
        }

        if (Handle != IntPtr.Zero)
            DestroyHandle();
    }

    [StructLayout(LayoutKind.Sequential)]
    struct DEV_BROADCAST_DEVICEINTERFACE
    {
        public int dbcc_size;
        public int dbcc_devicetype;
        public int dbcc_reserved;
        public Guid dbcc_classguid;
        public short dbcc_name;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr filter, int flags);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool UnregisterDeviceNotification(IntPtr handle);
}
