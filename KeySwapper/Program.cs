namespace KeySwapper;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        using var mutex = new Mutex(true, "KeySwapper_SingleInstance_5f2c", out bool isFirst);
        if (!isFirst)
        {
            MessageBox.Show("Key Swapper is already running. Look for its icon in the system tray.",
                "Key Swapper", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.System);
        using (var g = Graphics.FromHwnd(IntPtr.Zero))
            Theme.Scale = g.DpiX / 96f;

        Application.Run(new TrayContext(startHidden: args.Contains("--tray")));
    }
}
