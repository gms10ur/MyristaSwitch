using System.Runtime.InteropServices;
using System.Text;

namespace MyristaSwitch.App;

internal sealed class RawInputDevicePoller
{
    private const uint RidiDeviceName = 0x20000007;

    public IReadOnlySet<string> GetActiveHardwareTokens()
    {
        var deviceCount = 0U;
        var listItemSize = (uint)Marshal.SizeOf<RawInputDeviceList>();
        var result = GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, listItemSize);
        if (result != 0 || deviceCount == 0)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var bufferSize = (int)(listItemSize * deviceCount);
        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            result = GetRawInputDeviceList(buffer, ref deviceCount, listItemSize);
            if (result == uint.MaxValue)
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < deviceCount; index++)
            {
                var itemPointer = IntPtr.Add(buffer, index * (int)listItemSize);
                var item = Marshal.PtrToStructure<RawInputDeviceList>(itemPointer);
                var deviceName = GetDeviceName(item.DeviceHandle);
                var token = ExtractHardwareToken(deviceName);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    tokens.Add(token);
                }
            }

            return tokens;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static string? ExtractHardwareToken(string? deviceInstanceId)
    {
        if (string.IsNullOrWhiteSpace(deviceInstanceId))
        {
            return null;
        }

        var normalized = deviceInstanceId
            .Replace(@"\\?\", "", StringComparison.OrdinalIgnoreCase)
            .Replace('#', '\\')
            .ToUpperInvariant();

        var vidIndex = normalized.IndexOf("VID_", StringComparison.OrdinalIgnoreCase);
        var pidIndex = normalized.IndexOf("PID_", StringComparison.OrdinalIgnoreCase);
        if (vidIndex < 0 || pidIndex < 0)
        {
            return null;
        }

        var end = normalized.IndexOf('\\', pidIndex);
        if (end < 0)
        {
            end = normalized.Length;
        }

        return normalized[vidIndex..end];
    }

    private static string? GetDeviceName(IntPtr deviceHandle)
    {
        var size = 0U;
        var result = GetRawInputDeviceInfo(deviceHandle, RidiDeviceName, IntPtr.Zero, ref size);
        if (result != 0 || size == 0)
        {
            return null;
        }

        var buffer = Marshal.AllocHGlobal((int)size * 2);
        try
        {
            result = GetRawInputDeviceInfo(deviceHandle, RidiDeviceName, buffer, ref size);
            return result == uint.MaxValue ? null : Marshal.PtrToStringUni(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputDeviceList(IntPtr rawInputDeviceList, ref uint numDevices, uint size);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetRawInputDeviceInfo(IntPtr deviceHandle, uint command, IntPtr data, ref uint size);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct RawInputDeviceList
    {
        public readonly IntPtr DeviceHandle;
        public readonly uint DeviceType;
    }
}
