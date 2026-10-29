using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Rendering;

[TestFixture]
public sealed class BootstrapRoundedSurfacePainterTests
{
    [TestCase(96)]
    [TestCase(120)]
    [TestCase(144)]
    [TestCase(168)]
    [TestCase(192)]
    public void HalfPixelPolicyKeepsCornerCoverageSymmetric(int dpi)
    {
        var scale = dpi / 96f;
        var size = (int)Math.Ceiling(36f * scale) + 4;
        using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        var geometry = BootstrapRoundedSurfaceGeometry.Resolve(
            new RectangleF(2f, 2f, size - 4f, size - 4f),
            new CornerRadius(6f * scale),
            scale);

        BootstrapRoundedSurfacePainter.Paint(graphics, geometry, Color.White, Color.Black);

        var band = Math.Max(5, (int)Math.Ceiling(9f * scale));
        var topLeft = CountPartialAlpha(bitmap, new Rectangle(0, 0, band, band));
        var topRight = CountPartialAlpha(bitmap, new Rectangle(size - band, 0, band, band));
        var bottomLeft = CountPartialAlpha(bitmap, new Rectangle(0, size - band, band, band));
        var bottomRight = CountPartialAlpha(bitmap, new Rectangle(size - band, size - band, band, band));
        var coverage = FindCoverageBounds(bitmap);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(topLeft, Is.GreaterThan(0), "The shape must contain real anti-aliased coverage.");
            Assert.That(topRight, Is.GreaterThan(0));
            Assert.That(bottomLeft, Is.GreaterThan(0));
            Assert.That(bottomRight, Is.GreaterThan(0));
            Assert.That(Math.Abs(coverage.Left - (bitmap.Width - coverage.Right)), Is.LessThanOrEqualTo(1));
            Assert.That(Math.Abs(coverage.Top - (bitmap.Height - coverage.Bottom)), Is.LessThanOrEqualTo(1));
        }));
    }

    [Test]
    public void PainterRestoresGraphicsState()
    {
        using var bitmap = new Bitmap(80, 40, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.HighSpeed;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.TranslateTransform(3f, 4f);
        var smoothingMode = graphics.SmoothingMode;
        var pixelOffsetMode = graphics.PixelOffsetMode;
        var before = graphics.Transform.Elements;
        var geometry = BootstrapRoundedSurfaceGeometry.Resolve(
            new RectangleF(0f, 0f, 60f, 30f), new CornerRadius(6f), 2f);

        BootstrapRoundedSurfacePainter.Paint(graphics, geometry, Color.White, Color.Black);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(graphics.SmoothingMode, Is.EqualTo(smoothingMode));
            Assert.That(graphics.PixelOffsetMode, Is.EqualTo(pixelOffsetMode));
            Assert.That(graphics.Transform.Elements, Is.EqualTo(before));
        }));
    }

    [Test]
    public void DegenerateContourPaintsOnlyInsideOuterPathWithoutThrowing()
    {
        using var bitmap = new Bitmap(16, 12, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        var geometry = BootstrapRoundedSurfaceGeometry.Resolve(
            new RectangleF(2f, 2f, 12f, 8f),
            new CornerRadius(0f, 8f, 0f, 0f),
            2f);

        Assert.DoesNotThrow((Action)(() => BootstrapRoundedSurfacePainter.Paint(
            graphics, geometry, Color.White, Color.Black)));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(bitmap.GetPixel(0, 0).A, Is.Zero);
            Assert.That(bitmap.GetPixel(3, 8).A, Is.GreaterThan(0));
            Assert.That(bitmap.GetPixel(8, 6).A, Is.GreaterThan(0));
        }));
    }

    private static int CountPartialAlpha(Bitmap bitmap, Rectangle bounds)
    {
        var count = 0;
        for (var y = bounds.Top; y < bounds.Bottom; y++)
        for (var x = bounds.Left; x < bounds.Right; x++)
        {
            var alpha = bitmap.GetPixel(x, y).A;
            if (alpha > 0 && alpha < 255) count++;
        }

        return count;
    }

    private static Rectangle FindCoverageBounds(Bitmap bitmap)
    {
        var left = bitmap.Width;
        var top = bitmap.Height;
        var right = -1;
        var bottom = -1;
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width; x++)
        {
            if (bitmap.GetPixel(x, y).A == 0) continue;
            left = Math.Min(left, x);
            top = Math.Min(top, y);
            right = Math.Max(right, x);
            bottom = Math.Max(bottom, y);
        }

        return right < left ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }
}
