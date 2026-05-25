using System.Diagnostics;
using System.Windows.Forms;

namespace MyristaSwitch.App;

internal sealed class DisplayProfileService
{
    public int DisplayCount => Screen.AllScreens.Length;

    public bool CanSafelyRun(ScreenAction action)
    {
        if (action is ScreenAction.DoNothing)
        {
            return true;
        }

        if (Screen.AllScreens.Length <= 1 && action is ScreenAction.InternalOnly or ScreenAction.ExternalOnly)
        {
            return false;
        }

        return true;
    }

    public Task ApplyAsync(ScreenAction action, CancellationToken cancellationToken)
    {
        var argument = action switch
        {
            ScreenAction.DoNothing => null,
            ScreenAction.InternalOnly => "/internal",
            ScreenAction.Extend => "/extend",
            ScreenAction.ExternalOnly => "/external",
            ScreenAction.Duplicate => "/clone",
            _ => null
        };

        if (argument is null)
        {
            return Task.CompletedTask;
        }

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "DisplaySwitch.exe",
            Arguments = argument,
            UseShellExecute = true,
            CreateNoWindow = true
        });

        return process is null ? Task.CompletedTask : process.WaitForExitAsync(cancellationToken);
    }
}
