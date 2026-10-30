using System;
using System.Windows.Forms;

namespace MyDmsVn.BootstrapSourceGrid.Demo;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
#if NET8_0_OR_GREATER
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
#endif
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        if (args.Length == 4 && args[0] == "--scenario" &&
            (args[1] == "sourcegrid-busy-live" || args[1] == "sourcegrid-busy-fallback"))
            return BusyOverlayScenario.Run(args[2], args[3], args[1].EndsWith("fallback", StringComparison.Ordinal));
        Application.Run(new MainForm());
        return 0;
    }
}
