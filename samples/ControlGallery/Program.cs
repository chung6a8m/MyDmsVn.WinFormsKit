using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
#if NET8_0_OR_GREATER
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
#endif
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base16Px);
        if (args.Length == 2 && args[0] == "--busy-overlay-proof")
        {
            BusyOverlayDemoProof.Run(args[1]);
            return;
        }
        if (args.Length == 2 && args[0] == "--composition-proof")
        {
            CompositionDemoProof.Run(args[1]);
            return;
        }
        Application.Run(new MainForm());
    }
}
