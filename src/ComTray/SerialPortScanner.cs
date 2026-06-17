using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace ComTray;

record PortInfo(string PortName, int Number, string Description, string UsbKey, string LocationInfo)
{
    public bool IsUsb => !string.IsNullOrEmpty(UsbKey);
}

static partial class SerialPortScanner
{
    static readonly Guid PortsClass = new("4d36e978-e325-11ce-bfc1-08002be10318");

    const int DIGCF_PRESENT = 0x02;
    const int SPDRP_FRIENDLYNAME = 0x0C;
    const int SPDRP_LOCATION_INFORMATION = 0x0D;
    const uint DICS_FLAG_GLOBAL = 1;
    const uint DIREG_DEV = 1;
    const int KEY_READ = 0x20019;
    const int CR_SUCCESS = 0;

    static readonly DEVPROPKEY DEVPKEY_Device_LocationPaths =
        new() { fmtid = new("a45c254e-df1c-4efd-8020-67d146a850e0"), pid = 37 };
    static readonly DEVPROPKEY DEVPKEY_Device_LocationInfo =
        new() { fmtid = new("a45c254e-df1c-4efd-8020-67d146a850e0"), pid = 15 };

    public static List<PortInfo> Scan()
    {
        var result = new List<PortInfo>();
        var classGuid = PortsClass;
        IntPtr set = SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT);
        if (set == IntPtr.Zero || set == new IntPtr(-1))
            return result;

        try
        {
            var data = new SP_DEVINFO_DATA();
            data.cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>();

            for (uint i = 0; SetupDiEnumDeviceInfo(set, i, ref data); i++)
            {
                string portName = ReadPortName(set, ref data);
                if (string.IsNullOrEmpty(portName))
                    continue;

                string description = CleanDescription(GetProperty(set, ref data, SPDRP_FRIENDLYNAME));
                var (usbKey, locationInfo) = FindUsbSocket(data.DevInst);
                if (string.IsNullOrEmpty(locationInfo))
                    locationInfo = GetProperty(set, ref data, SPDRP_LOCATION_INFORMATION);

                result.Add(new PortInfo(portName, ParseNumber(portName), description, usbKey, locationInfo));
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(set);
        }

        return result;
    }

    // Climb from the serial function node to the USB device it belongs to and
    // read that node's topology. The stable path identifies the physical socket
    // regardless of which cable is in it; the location info is the readable hint.
    static (string key, string locationInfo) FindUsbSocket(uint devInst)
    {
        uint node = devInst;
        for (int depth = 0; depth < 8; depth++)
        {
            string id = GetDeviceId(node);
            if (id.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase))
            {
                string path = GetNodeProperty(node, DEVPKEY_Device_LocationPaths);
                string info = GetNodeProperty(node, DEVPKEY_Device_LocationInfo);
                string key = !string.IsNullOrEmpty(path) ? path : info;
                return (key, info);
            }

            if (CM_Get_Parent(out uint parent, node, 0) != CR_SUCCESS)
                break;
            node = parent;
        }
        return ("", "");
    }

    static string ReadPortName(IntPtr set, ref SP_DEVINFO_DATA data)
    {
        IntPtr h = SetupDiOpenDevRegKey(set, ref data, DICS_FLAG_GLOBAL, 0, DIREG_DEV, KEY_READ);
        if (h == IntPtr.Zero || h == new IntPtr(-1))
            return "";

        using var key = RegistryKey.FromHandle(new SafeRegistryHandle(h, true));
        return key.GetValue("PortName") as string ?? "";
    }

    static string GetProperty(IntPtr set, ref SP_DEVINFO_DATA data, int property)
    {
        var buffer = new byte[1024];
        if (!SetupDiGetDeviceRegistryProperty(set, ref data, property, out _, buffer, (uint)buffer.Length, out uint size))
            return "";

        if (size <= 2)
            return "";

        return Encoding.Unicode.GetString(buffer, 0, (int)size).TrimEnd('\0');
    }

    static string GetDeviceId(uint node)
    {
        var sb = new StringBuilder(512);
        return CM_Get_Device_IDW(node, sb, sb.Capacity, 0) == CR_SUCCESS ? sb.ToString() : "";
    }

    static string GetNodeProperty(uint node, DEVPROPKEY key)
    {
        uint size = 0;
        CM_Get_DevNode_PropertyW(node, ref key, out _, null, ref size, 0);
        if (size == 0)
            return "";

        var buffer = new byte[size];
        if (CM_Get_DevNode_PropertyW(node, ref key, out _, buffer, ref size, 0) != CR_SUCCESS)
            return "";

        string value = Encoding.Unicode.GetString(buffer, 0, (int)size);
        int end = value.IndexOf('\0');
        return end >= 0 ? value[..end] : value;
    }

    static string CleanDescription(string friendly) => ComSuffix().Replace(friendly, "").Trim();

    static int ParseNumber(string portName)
    {
        var m = TrailingDigits().Match(portName);
        return m.Success ? int.Parse(m.Value) : 0;
    }

    [GeneratedRegex(@"\d+$")]
    private static partial Regex TrailingDigits();

    [GeneratedRegex(@"\s*\(COM\d+\)\s*$")]
    private static partial Regex ComSuffix();

    [StructLayout(LayoutKind.Sequential)]
    struct SP_DEVINFO_DATA
    {
        public uint cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct DEVPROPKEY
    {
        public Guid fmtid;
        public uint pid;
    }

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr parent, int flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiEnumDeviceInfo(IntPtr set, uint index, ref SP_DEVINFO_DATA data);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "SetupDiGetDeviceRegistryPropertyW")]
    static extern bool SetupDiGetDeviceRegistryProperty(IntPtr set, ref SP_DEVINFO_DATA data, int property,
        out uint regDataType, byte[] buffer, uint bufferSize, out uint requiredSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern IntPtr SetupDiOpenDevRegKey(IntPtr set, ref SP_DEVINFO_DATA data, uint scope, uint hwProfile, uint keyType, int access);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);

    [DllImport("cfgmgr32.dll")]
    static extern int CM_Get_Parent(out uint parent, uint devInst, uint flags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    static extern int CM_Get_Device_IDW(uint devInst, StringBuilder buffer, int length, uint flags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    static extern int CM_Get_DevNode_PropertyW(uint devInst, ref DEVPROPKEY propertyKey, out ulong propertyType,
        byte[]? buffer, ref uint size, uint flags);
}
