using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

// Bounded evidence route inside the actual Gallery executable; no consumer/runtime API.
internal static class CompositionDemoProof
{
    internal static void Run(string output)
    {
        if (!Path.IsPathRooted(output)) throw new ArgumentException("An absolute evidence directory is required.", nameof(output));
        Directory.CreateDirectory(output);
        var metadata = Path.Combine(output, "composition-proof.txt");
        File.WriteAllText(metadata,
            "UTC=" + DateTime.UtcNow.ToString("O") + Environment.NewLine +
            "OS=" + Environment.OSVersion + "; Runtime=" + RuntimeInformation.FrameworkDescription +
            "; Bits=" + (IntPtr.Size * 8) + "; Executable=" + Application.ExecutablePath + Environment.NewLine,
            new UTF8Encoding(false));
        using var form = new MainForm();
        using var timer = new Timer { Interval = 300 };
        var elapsed = Stopwatch.StartNew();
        var steps = new List<Action>();
        var step = 0;
        var pendingCapture = false;
        string captureName = "";
        Form? current = null;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(10, 10);
        form.TopMost = true;

        void Prepare(string name, BootstrapThemeMode theme, Size size, int tabIndex, string state)
        {
            BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(theme, DemoTypographyPreset.Base16Px);
            form.Size = size;
            form.Location = Screen.FromControl(form).WorkingArea.Location;
            var sidebar = Descendants(form).OfType<BootstrapSidebar>().Single();
            sidebar.SelectedItem = sidebar.Items.Single(item => item.Text == "Compositions");
            var tabs = Descendants(form).OfType<TabControl>().Single();
            if (tabs.TabPages.Count != 2) throw new InvalidOperationException("Both composition pages must be registered in MainForm.");
            tabs.SelectedIndex = tabIndex;
            current = tabs.SelectedTab!.Controls.OfType<Form>().Single();
            if (tabIndex == 0)
            {
                var scenario = Named<BootstrapComboBox>(current, "ScenarioSelector");
                scenario.SelectedIndex = state == "unavailable" ? 3 : 0;
                Named<BootstrapTextBox>(current, "SearchInput").Text = state == "no-matches" ? "không-khớp-mẫu-nào" : "";
            }
            else
            {
                Named<BootstrapButton>(current, "CancelButton").PerformClick();
                if (state == "invalid")
                {
                    Named<BootstrapTextBox>(current, "CodeInput").Text = "";
                    Named<BootstrapTextBox>(current, "NameInput").Text = "";
                    Named<BootstrapButton>(current, "SaveButton").PerformClick();
                }
            }
            form.PerformLayout();
            form.Activate();
            SetForegroundWindow(form.Handle);
            captureName = name;
        }

        foreach (var theme in new[] { BootstrapThemeMode.Light, BootstrapThemeMode.Dark })
        {
            foreach (var small in new[] { false, true })
            {
                var size = small ? new Size(900, 600) : new Size(1280, 800);
                var prefix = theme + "-" + (small ? "small" : "normal");
                steps.Add(() => Prepare(prefix + "-uic01-loaded", theme, size, 0, "loaded"));
                steps.Add(() => Prepare(prefix + "-uic02-valid", theme, size, 1, "valid"));
                if (small)
                {
                    steps.Add(() => Prepare(prefix + "-uic01-no-matches", theme, size, 0, "no-matches"));
                    steps.Add(() => Prepare(prefix + "-uic01-unavailable", theme, size, 0, "unavailable"));
                    steps.Add(() => Prepare(prefix + "-uic02-invalid", theme, size, 1, "invalid"));
                }
            }
        }

        form.Shown += (_, _) => timer.Start();
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                if (elapsed.Elapsed > TimeSpan.FromSeconds(25)) throw new TimeoutException("Composition proof exceeded its 25-second deadline.");
                if (pendingCapture)
                {
                    Capture(form, current!, output, metadata, captureName);
                    pendingCapture = false;
                }
                else if (step < steps.Count)
                {
                    steps[step++]();
                    pendingCapture = true;
                }
                else
                {
                    File.AppendAllText(metadata, "Result=CAPTURED; captures=" + steps.Count + Environment.NewLine, new UTF8Encoding(false));
                    form.Close();
                    return;
                }
                timer.Start();
            }
            catch (Exception exception)
            {
                Environment.ExitCode = 1;
                File.AppendAllText(metadata, "Result=FAIL; " + exception + Environment.NewLine, new UTF8Encoding(false));
                form.Close();
            }
        };
        Application.Run(form);
    }

    private static void Capture(Form form, Form content, string output, string metadata, string name)
    {
        if (GetForegroundWindow() != form.Handle) throw new InvalidOperationException("Gallery is not foreground; refusing an unrelated screen capture.");
        if (!Screen.FromControl(form).Bounds.Contains(form.Bounds)) throw new InvalidOperationException("Gallery outer bounds do not fit the current monitor.");
        form.Update();
        var data = new StringBuilder();
        data.AppendLine("Capture=" + name + "; Theme=" + BootstrapThemeManager.CurrentTheme.Mode);
        data.AppendLine("FormOuter=" + form.Bounds + "; FormClient=" + form.ClientSize + "; DeviceDpi=" + form.DeviceDpi + "; WindowDpi=" + GetDpiForWindow(form.Handle));
        data.AppendLine("Monitor=" + Screen.FromControl(form).Bounds + "; WorkArea=" + Screen.FromControl(form).WorkingArea);
        data.AppendLine("Content=" + content.GetType().Name + "; Client=" + content.ClientSize + "; ScreenBounds=" + content.RectangleToScreen(content.ClientRectangle) + "; DeviceDpi=" + content.DeviceDpi);
        foreach (var control in Descendants(content).Where(control => !string.IsNullOrEmpty(control.Name)))
        {
            data.AppendLine(control.Name + "; Bounds=" + control.Bounds + "; Screen=" + control.RectangleToScreen(control.ClientRectangle) +
                "; Visible=" + control.Visible + "; DeviceDpi=" + control.DeviceDpi + "; Text=" + control.Text.Replace("\r", "\\r").Replace("\n", "\\n"));
        }
        var grid = Descendants(content).OfType<BootstrapDataGridView>().SingleOrDefault();
        if (grid is not null) data.AppendLine("Rows=" + grid.Rows.Count + "; RowTemplateHeight=" + grid.RowTemplate.Height + "; ReadOnly=" + grid.ReadOnly);
        var commands = Descendants(content).FirstOrDefault(control => control.Controls.OfType<BootstrapButton>().Any(button => button.Name == "SaveButton"));
        if (commands is not null)
            data.AppendLine("Footer=" + commands.Parent!.Bounds + "; FooterScreen=" + commands.Parent.RectangleToScreen(commands.Parent.ClientRectangle));
        using (var bitmap = new Bitmap(form.Width, form.Height))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(form.Location, Point.Empty, form.Size);
            bitmap.Save(Path.Combine(output, name + ".png"), ImageFormat.Png);
        }
        File.AppendAllText(metadata, data + Environment.NewLine, new UTF8Encoding(false));
    }

    private static T Named<T>(Control root, string name) where T : Control => Descendants(root).OfType<T>().Single(control => control.Name == name);

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
}
