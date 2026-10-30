using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;
[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public sealed class BootstrapBusyOverlayLayoutTests
{
    [TestCase(96), TestCase(120), TestCase(144), TestCase(192)]
    public void LongMessageAndCustomFontRemainWithinClientAndReserveCancel(int dpi)
    {
        using var font = new Font("Segoe UI", 20);
        foreach (var mode in new[] { BootstrapThemeMode.Light, BootstrapThemeMode.Dark })
        foreach (var size in new[] { Size.Empty, new Size(1, 1), new Size(20, 10), new Size(120, 70), new Size(400, 220), new Size(1000, 800) })
        {
            var layout = BootstrapBusyOverlayLayoutLogic.Calculate(size, font, new string('W', 400), "Hủy tác vụ dài", true, dpi, BootstrapTheme.CreateDefault(mode).Metrics);
            AssertInside(layout.Card, new Rectangle(Point.Empty, size));
            AssertInside(layout.Cancel, new Rectangle(Point.Empty, layout.Card.Size));
            AssertInside(layout.Viewport, new Rectangle(Point.Empty, layout.Card.Size));
            Assert.That(layout.Card.Width, Is.LessThanOrEqualTo(480 * dpi / 96));
            Assert.That(layout.Cancel.Height, Is.EqualTo(System.Math.Min(28 * dpi / 96, size.Height)));
            Assert.That(layout.Message.Height, Is.GreaterThanOrEqualTo(layout.Viewport.Height));
        }
    }
    [TestCase(96), TestCase(120), TestCase(144), TestCase(192)]
    public void CancelWidthShrinksSpacingBeforeClippingCaptionAtNarrowClientThresholds(int dpi)
    {
        using var font = new Font("Segoe UI", 20f * dpi / 96);
        foreach (var caption in new[] { "Cancel", "Hủy tác vụ" })
        {
            var desiredWidth = System.Math.Max(DpiScaler.Scale(80, dpi), TextRenderer.MeasureText(caption, font).Width + DpiScaler.Scale(24, dpi));
            var previous = 0;
            foreach (var logicalWidth in new[] { 119, 120, 143, 144 })
            {
                var client = DpiScaler.Scale(new Size(logicalWidth, 220), dpi);
                var layout = BootstrapBusyOverlayLayoutLogic.Calculate(client, font, "Busy", caption, true, dpi, BootstrapThemeMetrics.Default);
                Assert.That(layout.Cancel.Width, Is.EqualTo(System.Math.Min(client.Width, desiredWidth)), $"dpi={dpi}; client={client}; caption={caption}; desired={desiredWidth}");
                Assert.That(layout.Cancel.Width, Is.GreaterThanOrEqualTo(previous), "A wider client must not reduce caption capacity");
                AssertInside(layout.Card, new Rectangle(Point.Empty, client));
                AssertInside(layout.Cancel, new Rectangle(Point.Empty, layout.Card.Size));
                Assert.That(layout.Card.Width, Is.LessThanOrEqualTo(DpiScaler.Scale(480, dpi)));
                previous = layout.Cancel.Width;
            }
        }
    }
    private static void AssertInside(Rectangle child, Rectangle parent)
    {
        Assert.That(child.Width, Is.GreaterThanOrEqualTo(0)); Assert.That(child.Height, Is.GreaterThanOrEqualTo(0));
        Assert.That(child.Left, Is.GreaterThanOrEqualTo(parent.Left)); Assert.That(child.Top, Is.GreaterThanOrEqualTo(parent.Top));
        Assert.That(child.Right, Is.LessThanOrEqualTo(parent.Right)); Assert.That(child.Bottom, Is.LessThanOrEqualTo(parent.Bottom));
    }
}
