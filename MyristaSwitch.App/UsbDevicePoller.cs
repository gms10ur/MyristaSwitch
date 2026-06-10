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
              Select-Object Class,FriendlyName,InstanceId,Status,@{Name='Present';Expression={$true}},@{Name='ProblemCode';Expression={$null}} |
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
        var status = GetString(item, "Status") ?? "";
        var problemCode = GetUInt32(item, "ProblemCode");
        var present = GetBoolean(item, "Present");
        devices.Add(new UsbDevice(className, friendlyName, instanceId, status, problemCode, present));
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString()
            : null;
    }

    private static uint? GetUInt32(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.TryGetUInt32(out var result) ? result : null;
    }

    private static bool GetBoolean(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) &&
            value.ValueKind is JsonValueKind.True or JsonValueKind.False &&
            value.GetBoolean();
    }
}
