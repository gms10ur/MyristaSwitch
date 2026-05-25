using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace MyristaSwitch.App;

internal sealed class UsbDevicePoller
{
    public async Task<IReadOnlyList<UsbDevice>> GetPresentInputDevicesAsync(CancellationToken cancellationToken)
    {
        const string script = """
            Get-PnpDevice -PresentOnly |
              Where-Object { $_.Class -in @('Keyboard','Mouse','HIDClass') } |
              Select-Object Class,FriendlyName,InstanceId |
              ConvertTo-Json -Compress
            """;

        var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encodedCommand}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;
        var devices = new List<UsbDevice>();

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                AddDevice(devices, item);
            }
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            AddDevice(devices, root);
        }

        return devices
            .Where(device => !string.IsNullOrWhiteSpace(device.InstanceId))
            .OrderBy(device => device.ClassName)
            .ThenBy(device => device.FriendlyName)
            .ToList();
    }

    private static void AddDevice(List<UsbDevice> devices, JsonElement item)
    {
        var className = GetString(item, "Class") ?? "Unknown";
        var friendlyName = GetString(item, "FriendlyName") ?? "";
        var instanceId = GetString(item, "InstanceId") ?? "";
        devices.Add(new UsbDevice(className, friendlyName, instanceId));
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString()
            : null;
    }
}
