using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class InputPopupRoundThreeRegressionTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void SimpleStyleKeepsCallerHeightAssignedDuringHandleCreated(bool assignSize)
    {
        using var form = new Form { AutoScaleMode = AutoScaleMode.None, ShowInTaskbar = false };
        using var native = new ComboBox { IntegralHeight = false, Size = new Size(220, 120) };
        using var combo = new BootstrapComboBox { IntegralHeight = false, Size = new Size(220, 120), Left = 240 };
        form.Controls.Add(native);
        form.Controls.Add(combo);
        form.Show();
        Application.DoEvents();
        var nativeCallbacks = 0;
        var toolkitCallbacks = 0;
        EventHandler nativeHandler = (_, _) => { if (native.DropDownStyle == ComboBoxStyle.Simple) { nativeCallbacks++; AssignHeight(native, assignSize); } };
        EventHandler toolkitHandler = (_, _) => { if (combo.DropDownStyle == ComboBoxStyle.Simple) { toolkitCallbacks++; AssignHeight(combo, assignSize); } };
        native.HandleCreated += nativeHandler;
        combo.HandleCreated += toolkitHandler;
        native.DropDownStyle = ComboBoxStyle.Simple;
        combo.DropDownStyle = ComboBoxStyle.Simple;
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(nativeCallbacks, Is.EqualTo(1));
            Assert.That(toolkitCallbacks, Is.EqualTo(1));
            Assert.That(native.Height, Is.EqualTo(200));
            Assert.That(combo.Height, Is.EqualTo(native.Height), "Style-change restore must preserve the HandleCreated caller's newer request.");
        }));
        native.HandleCreated -= nativeHandler;
        combo.HandleCreated -= toolkitHandler;
        native.DropDownStyle = combo.DropDownStyle = ComboBoxStyle.DropDown;
        native.DropDownStyle = combo.DropDownStyle = ComboBoxStyle.Simple;
        Application.DoEvents();
        Assert.That(combo.Height, Is.EqualTo(native.Height), "The newer request must survive a later style roundtrip.");
    }

    [TestCase(2f, 2f)]
    [TestCase(2f, 1.5f)]
    public void OutlineButtonExportKeepsAllEdgesAtPixelBoundsWhenDestinationDpiDiffers(float xScale, float yScale)
    {
        var previousTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            var d = BootstrapThemeMetrics.Default;
            var metrics = new BootstrapThemeMetrics(d.ControlHeightSmall, d.ControlHeight, d.ControlHeightLarge,
                d.RadiusSmall, d.Radius, d.RadiusLarge, 2, d.FocusBorderWidth,
                d.SpacingXS, d.SpacingSM, d.SpacingMD, d.SpacingLG, d.SpacingXL);
            var theme = new BootstrapTheme(BootstrapThemeMode.Light, BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light),
                metrics, BootstrapThemeTypography.Default);
            BootstrapThemeManager.CurrentTheme = theme;
            using var host = new ExportHost();
            using var button = new BootstrapButton { Outline = true, Text = "", Size = new Size(140, 80), BorderRadius = 2 };
            using var probe = new Bitmap(1, 1, PixelFormat.Format32bppPArgb);
            using var regular = new Bitmap(140, 80, PixelFormat.Format32bppPArgb);
            using var collapsed = new Bitmap(140, 80, PixelFormat.Format32bppPArgb);
            regular.SetResolution(probe.HorizontalResolution * xScale, probe.VerticalResolution * yScale);
            collapsed.SetResolution(probe.HorizontalResolution * xScale, probe.VerticalResolution * yScale);
            host.Render(button, regular);
            button.BorderRadius = 1;
            host.Render(button, collapsed);
            foreach (var point in new[] { new Point(0, 40), new Point(139, 40), new Point(70, 0), new Point(70, 79) })
            {
                Assert.That(regular.GetPixel(point.X, point.Y).ToArgb(), Is.EqualTo(theme.Colors.Primary.ToArgb()), "Vector edge baseline at " + point);
                Assert.That(collapsed.GetPixel(point.X, point.Y).ToArgb(), Is.EqualTo(regular.GetPixel(point.X, point.Y).ToArgb()), "Pixel-mapped bitmap edge at " + point);
            }
            Assert.That(collapsed.GetPixel(70, 40).ToArgb(), Is.EqualTo(theme.Colors.Surface.ToArgb()));
        }
        finally { BootstrapThemeManager.CurrentTheme = previousTheme; }
    }

    private static void AssignHeight(ComboBox combo, bool assignSize)
    {
        if (assignSize) combo.Size = new Size(230, 200);
        else combo.Height = 200;
    }

    private sealed class ExportHost : Control
    {
        internal void Render(Control control, Bitmap bitmap)
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.PageUnit = GraphicsUnit.Pixel;
            graphics.Clear(Color.White);
            graphics.SetClip(control.ClientRectangle);
            using var args = new PaintEventArgs(graphics, control.ClientRectangle);
            InvokePaint(control, args);
        }
    }
}
