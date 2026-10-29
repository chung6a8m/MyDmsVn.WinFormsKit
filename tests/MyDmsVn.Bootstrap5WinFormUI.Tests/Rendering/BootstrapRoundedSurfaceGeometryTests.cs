using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Rendering;

[TestFixture]
public sealed class BootstrapRoundedSurfaceGeometryTests
{
    [Test]
    public void FocusStrokeKeepsOuterContourStable()
    {
        var bounds = new RectangleF(0f, 0f, 140f, 80f);
        var radius = new CornerRadius(6f);

        var normal = BootstrapRoundedSurfaceGeometry.Resolve(bounds, radius, 1f);
        var focused = BootstrapRoundedSurfaceGeometry.Resolve(bounds, radius, 2.5f);

        using var normalOuter = normal.CreateOuterPath();
        using var focusedOuter = focused.CreateOuterPath();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(normal.OuterBounds, Is.EqualTo(focused.OuterBounds));
            Assert.That(normal.OuterRadius, Is.EqualTo(focused.OuterRadius));
            Assert.That(normalOuter.GetBounds(), Is.EqualTo(focusedOuter.GetBounds()));
            Assert.That(focused.StrokeBounds.Left, Is.GreaterThan(normal.StrokeBounds.Left));
            Assert.That(focused.InnerBounds.Left, Is.GreaterThan(normal.InnerBounds.Left));
        }));
    }

    [TestCase(1f, 6f)]
    [TestCase(2f, 16f)]
    [TestCase(1.25f, 6f)]
    [TestCase(2.5f, 16f)]
    public void DecorationRectangleStaysInsideInnerContour(float borderWidth, float radius)
    {
        var geometry = BootstrapRoundedSurfaceGeometry.Resolve(
            new RectangleF(0f, 0f, 140f, 80f),
            new CornerRadius(radius),
            borderWidth);
        var insets = geometry.GetDecorationInsets(1f);
        var safe = RectangleF.FromLTRB(
            geometry.OuterBounds.Left + insets.Left,
            geometry.OuterBounds.Top + insets.Top,
            geometry.OuterBounds.Right - insets.Right,
            geometry.OuterBounds.Bottom - insets.Bottom);
        using var inner = geometry.CreateInnerPath();

        Assert.That(geometry.HasUsableInterior, Is.True);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(inner.IsVisible(safe.Left + 0.01f, safe.Top + 0.01f), Is.True);
            Assert.That(inner.IsVisible(safe.Right - 0.01f, safe.Top + 0.01f), Is.True);
            Assert.That(inner.IsVisible(safe.Left + 0.01f, safe.Bottom - 0.01f), Is.True);
            Assert.That(inner.IsVisible(safe.Right - 0.01f, safe.Bottom - 0.01f), Is.True);
            Assert.That(inner.IsVisible(safe.Left + (safe.Width / 2f), safe.Top + (safe.Height / 2f)), Is.True);
        }));
    }

    [Test]
    public void TinyBoundsNeverProduceNegativeContent()
    {
        var geometry = BootstrapRoundedSurfaceGeometry.Resolve(
            new RectangleF(0f, 0f, 2f, 1f),
            new CornerRadius(16f),
            2f);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(geometry.HasUsableInterior, Is.False);
            Assert.That(geometry.InnerBounds.Width, Is.Zero);
            Assert.That(geometry.InnerBounds.Height, Is.Zero);
            Assert.That(geometry.GetDecorationInsets(1f), Is.EqualTo(PaddingFromBounds(geometry.OuterBounds)));
        }));
        using var inner = geometry.CreateInnerPath();
        Assert.That(inner.PointCount, Is.Zero);
    }

    [Test]
    public void ZeroRadiusHasSymmetricEdges()
    {
        var geometry = BootstrapRoundedSurfaceGeometry.Resolve(
            new RectangleF(2f, 3f, 140f, 80f),
            CornerRadius.Empty,
            1.25f);

        using var outer = geometry.CreateOuterPath();
        using var inner = geometry.CreateInnerPath();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(geometry.OuterRadius, Is.EqualTo(CornerRadius.Empty));
            Assert.That(geometry.StrokeRadius, Is.EqualTo(CornerRadius.Empty));
            Assert.That(geometry.InnerRadius, Is.EqualTo(CornerRadius.Empty));
            Assert.That(outer.GetBounds(), Is.EqualTo(geometry.OuterBounds));
            Assert.That(inner.GetBounds(), Is.EqualTo(geometry.InnerBounds));
            Assert.That(geometry.GetDecorationInsets(1f), Is.EqualTo(new Padding(3)));
        }));
    }

    [Test]
    public void AsymmetricCornersRemainContained()
    {
        var geometry = BootstrapRoundedSurfaceGeometry.Resolve(
            new RectangleF(0f, 0f, 140f, 80f),
            new CornerRadius(16f, 0f, 6f, 2f),
            2f);
        var insets = geometry.GetDecorationInsets(1f);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(geometry.HasUsableInterior, Is.True);
            Assert.That(insets.Left, Is.GreaterThan(insets.Right));
            Assert.That(insets.Top, Is.GreaterThan(insets.Bottom));
            Assert.That(geometry.InnerRadius.TopRight, Is.Zero);
            Assert.That(geometry.InnerRadius.TopLeft, Is.EqualTo(14f));
        }));
    }

    [Test]
    public void DerivedRadiusThatWouldRenormalizeMarksInteriorUnusable()
    {
        var geometry = BootstrapRoundedSurfaceGeometry.Resolve(
            new RectangleF(0f, 0f, 12f, 8f),
            new CornerRadius(0f, 8f, 0f, 0f),
            2f);

        Assert.That(geometry.HasUsableInterior, Is.False,
            "The 6px derived top-right radius cannot fit the 4px inner height and must not be silently normalized.");
        using var inner = geometry.CreateInnerPath();
        Assert.That(inner.PointCount, Is.Zero);
    }

    [Test]
    public void InvalidBorderAndGuardAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapRoundedSurfaceGeometry.Resolve(
            new RectangleF(0f, 0f, 10f, 10f), new CornerRadius(2f), -0.1f)));
        var geometry = BootstrapRoundedSurfaceGeometry.Resolve(
            new RectangleF(0f, 0f, 10f, 10f), new CornerRadius(2f), 1f);
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => geometry.GetDecorationInsets(-0.1f)));
    }

    private static Padding PaddingFromBounds(RectangleF bounds) => new Padding(
        (int)Math.Ceiling(bounds.Width / 2f),
        (int)Math.Ceiling(bounds.Height / 2f),
        (int)Math.Ceiling(bounds.Width / 2f),
        (int)Math.Ceiling(bounds.Height / 2f));
}
