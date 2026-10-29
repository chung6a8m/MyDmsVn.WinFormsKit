using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class InputPopupRoundTwoRegressionTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void PreHandleFlowTracksMetricAndFontChangesWithoutLosingSimpleHeight(bool changeFont)
    {
        var previousTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = CreateTheme(32, 1);
            using var small = new Font("Segoe UI", 10f);
            using var large = new Font("Segoe UI", 20f);
            using var form = new Form { ShowInTaskbar = false };
            using var flow = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            using var combo = new BootstrapComboBox
            {
                IntegralHeight = false, Size = new Size(220, 120), Font = small, Margin = new Padding(3)
            };
            flow.Controls.Add(combo);
            form.Controls.Add(flow);
            flow.PerformLayout();
            var originalHeight = combo.Height;

            if (changeFont) combo.Font = large;
            else BootstrapThemeManager.CurrentTheme = CreateTheme(48, 1);
            flow.PerformLayout();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(combo.IsHandleCreated, Is.False);
                Assert.That(combo.Height, Is.GreaterThan(originalHeight));
                Assert.That(combo.Bottom + combo.Margin.Bottom, Is.LessThanOrEqualTo(flow.DisplayRectangle.Bottom));
            }));
            var allocatedHeight = flow.Height;
            flow.PerformLayout();
            Assert.That(flow.Height, Is.EqualTo(allocatedHeight));

            combo.Width = 230;
            combo.Location = new Point(6, 6);
            combo.DropDownStyle = ComboBoxStyle.Simple;
            form.Show();
            Application.DoEvents();
            Assert.That(combo.Height, Is.EqualTo(120), "Layout metrics must not replace the caller's Simple height.");
        }
        finally { BootstrapThemeManager.CurrentTheme = previousTheme; }
    }

    [TestCase(1, 2, false)]
    [TestCase(1, 2, true)]
    [TestCase(4, 8, false)]
    [TestCase(4, 8, true)]
    public void CollapsedCornerCompositesTranslucentInteriorExactlyOnce(int radius, int borderWidth, bool whiteBackdrop)
    {
        var color = Color.FromArgb(128, 255, 0, 0);
        using var actual = new Bitmap(140, 80, PixelFormat.Format32bppArgb);
        using var expected = new Bitmap(140, 80, PixelFormat.Format32bppArgb);
        using var actualGraphics = Graphics.FromImage(actual);
        using var expectedGraphics = Graphics.FromImage(expected);
        var backdrop = whiteBackdrop ? Color.White : Color.Transparent;
        actualGraphics.Clear(backdrop);
        expectedGraphics.Clear(backdrop);
        using var brush = new SolidBrush(color);
        expectedGraphics.FillRectangle(brush, 0, 0, 140, 80);

        BootstrapRoundedSurfacePainter.Paint(actualGraphics,
            BootstrapRoundedSurfaceGeometry.Resolve(new RectangleF(0, 0, 140, 80), new CornerRadius(radius), borderWidth),
            color, color);

        Assert.That(actual.GetPixel(70, 40).ToArgb(), Is.EqualTo(expected.GetPixel(70, 40).ToArgb()),
            "Interior must match one SourceOver compositing operation.");
    }

    [Test]
    public void FilledButtonWithTranslucentSemanticColorKeepsCenterAcrossRadiusThreshold()
    {
        var previousTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = CreateTheme(32, 2, Color.FromArgb(128, 255, 0, 0));
            using var form = new Form { BackColor = Color.White, ShowInTaskbar = false };
            using var button = new BootstrapButton { Size = new Size(140, 80), BorderRadius = 2, Text = "", Outline = false };
            form.Controls.Add(button);
            form.Show();
            Application.DoEvents();
            using var regular = new Bitmap(140, 80);
            button.DrawToBitmap(regular, button.ClientRectangle);
            button.BorderRadius = 1;
            using var collapsed = new Bitmap(140, 80);
            button.DrawToBitmap(collapsed, button.ClientRectangle);
            Assert.That(collapsed.GetPixel(70, 40).ToArgb(), Is.EqualTo(regular.GetPixel(70, 40).ToArgb()));
        }
        finally { BootstrapThemeManager.CurrentTheme = previousTheme; }
    }

    [TestCase(255)]
    [TestCase(128)]
    public void FractionalCollapsedBorderHasNoInnerCoverageSeam(int alpha)
    {
        using var bitmap = new Bitmap(140, 80, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        var color = Color.FromArgb(alpha, 255, 0, 0);
        BootstrapRoundedSurfacePainter.Paint(graphics,
            BootstrapRoundedSurfaceGeometry.Resolve(new RectangleF(0, 0, 140, 80), new CornerRadius(1.25f), 2.5f),
            color, color);
        var center = bitmap.GetPixel(70, 40);
        for (var x = 1; x <= 4; x++)
            Assert.That(bitmap.GetPixel(x, 40).ToArgb(), Is.EqualTo(center.ToArgb()), "No backdrop seam at inner edge.");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DifferentTranslucentColorsShareFractionalInnerCoverage(bool whiteBackdrop)
    {
        using var bitmap = new Bitmap(140, 80, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(whiteBackdrop ? Color.White : Color.Transparent);
        BootstrapRoundedSurfacePainter.Paint(graphics,
            BootstrapRoundedSurfaceGeometry.Resolve(new RectangleF(0, 0, 140, 80), new CornerRadius(1.25f), 2.5f),
            Color.FromArgb(128, 255, 0, 0), Color.FromArgb(128, 0, 0, 255));
        var edge = bitmap.GetPixel(2, 40);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(edge.A, Is.EqualTo(whiteBackdrop ? 255 : 128).Within(1));
            Assert.That(edge.G, Is.EqualTo(whiteBackdrop ? 127 : 0).Within(1));
            Assert.That(edge.R, Is.EqualTo(whiteBackdrop ? 191 : 128).Within(2));
            Assert.That(edge.B, Is.EqualTo(whiteBackdrop ? 191 : 128).Within(2));
        }));
    }

    private static BootstrapTheme CreateTheme(int controlHeight, int borderWidth, Color? primary = null)
    {
        var d = BootstrapThemeMetrics.Default;
        var c = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);
        var metrics = new BootstrapThemeMetrics(d.ControlHeightSmall, controlHeight, d.ControlHeightLarge,
            d.RadiusSmall, d.Radius, d.RadiusLarge, borderWidth, d.FocusBorderWidth,
            d.SpacingXS, d.SpacingSM, d.SpacingMD, d.SpacingLG, d.SpacingXL);
        var colors = new BootstrapThemeColors(primary ?? c.Primary, c.Secondary, c.Success, c.Danger, c.Warning,
            c.Info, c.Light, c.Dark, c.Body, c.Surface, c.SurfaceSecondary, c.Border, c.Text, c.MutedText,
            c.Disabled, c.Focus, c.Hover, c.Active);
        return new BootstrapTheme(BootstrapThemeMode.Light, colors, metrics, BootstrapThemeTypography.Default);
    }
}
