using System.Text.Json;

namespace MyristaSwitch.App;

internal sealed class AppSettings
{
    public bool AutomationEnabled { get; set; }
    public string? KeyboardInstanceId { get; set; }
    public string? MouseInstanceId { get; set; }
    public ScreenAction ConnectedAction { get; set; } = ScreenAction.Extend;
    public ScreenAction DisconnectedAction { get; set; } = ScreenAction.InternalOnly;
    public int PollIntervalSeconds { get; set; } = 1;
    public bool RequireBothDevices { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; }

    public static string SettingsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyristaSwitch");

    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}
