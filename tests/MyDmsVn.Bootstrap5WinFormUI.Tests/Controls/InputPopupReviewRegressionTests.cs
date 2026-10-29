using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class InputPopupReviewRegressionTests
{
    [Test]
    public void RadiusShrinkPreservesEndScrollAfterFinalPopupLayout()
    {
        using var form = new Form { Size = new Size(800, 700), ShowInTaskbar = false };
        using var select = new BootstrapSelect
        {
            Width = 320, SearchEnabled = false, MaxDropDownHeight = 500, BorderRadius = 32
        };
        for (var index = 0; index < 30; index++) select.Items.Add(new BootstrapSelectItem(index, "Item " + index));
        form.Controls.Add(select);
        form.Show();
        select.OpenDropDownInternal();
        Application.DoEvents();
        var results = select.DropDownContentForTest!.Controls.OfType<BootstrapSelectResultsView>().Single();
        results.SetScrollOffset(int.MaxValue);
        var before = results.ScrollOffset;
        var highlighted = select.HighlightedResultTextForTest;
        var handle = select.DropDownHandleForTest;
        var changes = 0;
        select.SelectionChanged += (_, _) => changes++;

        select.BorderRadius = 0;
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(results.ScrollOffset, Is.EqualTo(Math.Min(before, (30 * results.RowHeight) - results.ClientSize.Height)));
            Assert.That(results.ScrollOffset + results.ClientSize.Height, Is.EqualTo(30 * results.RowHeight));
            Assert.That(select.HighlightedResultTextForTest, Is.EqualTo(highlighted));
            Assert.That(select.DropDownHandleForTest, Is.EqualTo(handle));
            Assert.That(changes, Is.Zero);
        }));
    }

    [TestCase(1, false)]
    [TestCase(8, false)]
    [TestCase(1, true)]
    [TestCase(8, true)]
    public void ThickBorderPreservesIndependentOuterCornerCoverage(int borderWidth, bool mixedCorners)
    {
        using var bitmap = new Bitmap(140, 80, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        var radii = mixedCorners ? new CornerRadius(4, 0, 10, 4) : new CornerRadius(4);
        var geometry = BootstrapRoundedSurfaceGeometry.Resolve(new RectangleF(0, 0, 140, 80), radii, borderWidth);
        BootstrapRoundedSurfacePainter.Paint(graphics, geometry, Color.White, Color.Black);

        // Analytic circle coverage, independent of paths/Regions produced by the implementation.
        for (var y = 0; y < 4; y++)
        for (var x = 0; x < 4; x++)
        {
            var dx = 4 - (x + 0.5d);
            var dy = 4 - (y + 0.5d);
            if (Math.Sqrt((dx * dx) + (dy * dy)) > 4.75d)
                Assert.That(bitmap.GetPixel(x, y).A, Is.Zero, $"Outside rounded contour ({x},{y}), border={borderWidth}");
        }
        if (mixedCorners) Assert.That(bitmap.GetPixel(139, 0).A, Is.EqualTo(255), "Square corner stays square.");
    }

    [TestCase(2, 40)]
    [TestCase(40, 2)]
    public void UnconstrainedMeasurementFitsNarrowContentWithThickSquareBorder(int width, int height)
    {
        var defaults = BootstrapThemeMetrics.Default;
        var metrics = new BootstrapThemeMetrics(defaults.ControlHeightSmall, defaults.ControlHeight,
            defaults.ControlHeightLarge, defaults.RadiusSmall, defaults.Radius, defaults.RadiusLarge,
            6, defaults.FocusBorderWidth, defaults.SpacingXS, defaults.SpacingSM, defaults.SpacingMD,
            defaults.SpacingLG, defaults.SpacingXL);
        using var surface = new BootstrapOverlaySurface { LogicalBorderRadius = 0, LogicalContentPadding = Padding.Empty };
        using var content = new FixedPreferredControl(new Size(width, height));
        surface.AttachContent(content);
        surface.ApplyTheme(new BootstrapTheme(BootstrapThemeMode.Light,
            BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light), metrics, BootstrapThemeTypography.Default), 96);

        var preferred = surface.GetPreferredSize(Size.Empty);
        surface.Size = preferred;
        surface.PerformLayout();
        content.Parent!.PerformLayout();
        var insets = surface.GetEffectiveContentInsets(preferred);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(preferred.Width - insets.Horizontal, Is.GreaterThanOrEqualTo(width));
            Assert.That(preferred.Height - insets.Vertical, Is.GreaterThanOrEqualTo(height));
            Assert.That(content.Width, Is.GreaterThanOrEqualTo(width));
            Assert.That(content.Height, Is.GreaterThanOrEqualTo(height));
            Assert.That(surface.GetPreferredSize(Size.Empty), Is.EqualTo(preferred));
        }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SimpleModeRetainsHeightRequestedBeforeStyleAndAcrossRoundTrip(bool roundTrip)
    {
        using var form = new Form { ShowInTaskbar = false };
        using var combo = new BootstrapComboBox { IntegralHeight = false, Size = new Size(220, 120) };
        combo.Width = 230;
        combo.Location = new Point(20, 20);
        combo.DropDownStyle = ComboBoxStyle.Simple;
        form.Controls.Add(combo);
        form.Show();
        Application.DoEvents();
        Assert.That(combo.Height, Is.EqualTo(120), "Size before DropDownStyle must survive native handle creation.");
        if (!roundTrip) return;

        combo.DropDownStyle = ComboBoxStyle.DropDown;
        combo.Width = 240;
        combo.Location = new Point(25, 25);
        combo.DropDownStyle = ComboBoxStyle.Simple;
        Application.DoEvents();
        Assert.That(combo.Height, Is.EqualTo(120));
    }

    [Test]
    public void SimpleWithoutExplicitSizeRetainsNativeDefaultRequestedHeight()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var native = new ComboBox { IntegralHeight = false, DrawMode = DrawMode.OwnerDrawFixed, DropDownStyle = ComboBoxStyle.Simple };
        using var combo = new BootstrapComboBox { IntegralHeight = false, DropDownStyle = ComboBoxStyle.Simple };
        form.Controls.Add(native);
        form.Controls.Add(combo);
        form.Show();
        Application.DoEvents();
        Assert.That(combo.Height, Is.EqualTo(native.Height));
    }

    private sealed class FixedPreferredControl : Control
    {
        private readonly Size _preferred;
        internal FixedPreferredControl(Size preferred) => _preferred = preferred;
        public override Size GetPreferredSize(Size proposedSize) => _preferred;
    }
}
