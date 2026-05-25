namespace MyristaSwitch.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(args: Environment.GetCommandLineArgs()));
    }    
}
