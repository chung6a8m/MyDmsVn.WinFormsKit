using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapButtonTests
{
    [Test]
    public void DefaultsMatchPhase6Contract()
    {
        using var button = new BootstrapButton();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(button.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(button.Outline, Is.False);
            Assert.That(button.ButtonSize, Is.EqualTo(BootstrapButtonSize.Default));
            Assert.That(button.Icon, Is.Null);
            Assert.That(button.IconPosition, Is.EqualTo(BootstrapIconPosition.Left));
            Assert.That(button.BorderRadius, Is.EqualTo(-1));
            Assert.That(button.Loading, Is.False);
            Assert.That(button.LoadingText, Is.EqualTo(string.Empty));
            Assert.That(button.Selected, Is.False);
            Assert.That(button.TabStop, Is.True);
        }));
    }

    [Test]
    public void CustomPaintingClearsInheritedOpaqueStyleForRoundedCorners()
    {
        using var button = new StyleProbeBootstrapButton();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(button.HasStyle(ControlStyles.Opaque), Is.False,
                "Opaque skips background painting and leaves stale double-buffer pixels outside the rounded path.");
            Assert.That(button.HasStyle(ControlStyles.SupportsTransparentBackColor), Is.True);
            Assert.That(button.HasStyle(ControlStyles.OptimizedDoubleBuffer), Is.True);
        }));
    }

    [Test]
    public void LoadingSuppressesPerformClick()
    {
        using var button = new BootstrapButton();
        var clickCount = 0;
        button.Click += (_, _) => clickCount++;

        button.PerformClick();
        button.Loading = true;
        button.PerformClick();

        Assert.That(clickCount, Is.EqualTo(1));
    }

    [Test]
    public void LoadingUsesComposedBootstrapSpinner()
    {
        using var button = new BootstrapButton();
        var spinner = button.Controls.OfType<BootstrapSpinner>().Single();

        button.Loading = true;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(spinner.Visible, Is.True);
            Assert.That(spinner.Spinning, Is.True);
        }));

        button.Loading = false;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(spinner.Visible, Is.False);
            Assert.That(spinner.Spinning, Is.False);
        }));
    }

    [Test]
    public void LoadingDoesNotMutateEnabledOrSelectedState()
    {
        using var button = new BootstrapButton
        {
            Enabled = true,
            Selected = true
        };

        button.Loading = true;
        button.Loading = false;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(button.Enabled, Is.True);
            Assert.That(button.Selected, Is.True);
        }));
    }

    [Test]
    public void PreferredSizeDoesNotChangeWhenLoadingToggles()
    {
        using var button = new BootstrapButton
        {
            Text = "Save",
            LoadingText = "Saving changes...",
            AutoSize = true
        };

        var normal = button.GetPreferredSize(Size.Empty);
        button.Loading = true;
        var loading = button.GetPreferredSize(Size.Empty);

        Assert.That(loading, Is.EqualTo(normal));
    }

    [Test]
    public void ButtonSizesUseThemeControlHeights()
    {
        var metrics = BootstrapThemeMetrics.Default;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapButtonRenderLogic.GetLogicalHeight(metrics, BootstrapButtonSize.Small), Is.EqualTo(metrics.ControlHeightSmall));
            Assert.That(BootstrapButtonRenderLogic.GetLogicalHeight(metrics, BootstrapButtonSize.Default), Is.EqualTo(metrics.ControlHeight));
            Assert.That(BootstrapButtonRenderLogic.GetLogicalHeight(metrics, BootstrapButtonSize.Large), Is.EqualTo(metrics.ControlHeightLarge));
        }));
    }

    [Test]
    public void OutlineNormalStateUsesVariantForBorderAndForeground()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

        var palette = BootstrapButtonRenderLogic.ResolvePalette(
            colors,
            BootstrapVariant.Success,
            outline: true,
            enabled: true,
            selected: false,
            BootstrapButtonVisualState.Normal);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(palette.Border, Is.EqualTo(colors.Success));
            Assert.That(palette.Foreground, Is.EqualTo(colors.Success));
            Assert.That(palette.Background, Is.EqualTo(colors.Surface));
        }));
    }

    [Test]
    public void SelectedOutlineStateBecomesFilled()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

        var palette = BootstrapButtonRenderLogic.ResolvePalette(
            colors,
            BootstrapVariant.Warning,
            outline: true,
            enabled: true,
            selected: true,
            BootstrapButtonVisualState.Normal);

        Assert.That(palette.Background, Is.Not.EqualTo(colors.Surface));
        Assert.That(palette.Border, Is.EqualTo(palette.Background));
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void DisabledPaletteRemainsReadableAndDistinctFromEnabledSecondaryOutline(BootstrapThemeMode mode)
    {
        var colors = BootstrapThemeColors.CreateDefault(mode);
        var disabled = BootstrapButtonRenderLogic.ResolvePalette(
            colors,
            BootstrapVariant.Secondary,
            outline: true,
            enabled: false,
            selected: false,
            BootstrapButtonVisualState.Normal);
        var enabled = BootstrapButtonRenderLogic.ResolvePalette(
            colors,
            BootstrapVariant.Secondary,
            outline: true,
            enabled: true,
            selected: false,
            BootstrapButtonVisualState.Normal);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(disabled.Background, Is.EqualTo(colors.SurfaceSecondary));
            Assert.That(disabled.Foreground, Is.EqualTo(colors.Text));
            Assert.That(disabled.Border, Is.EqualTo(colors.Disabled));
            Assert.That(disabled.Background, Is.Not.EqualTo(enabled.Background),
                "Disabled actions must not depend on a border-only distinction from enabled Secondary outline actions.");
            Assert.That(ColorUtil.GetContrastRatio(disabled.Foreground, disabled.Background), Is.GreaterThanOrEqualTo(4.5d),
                "4.5:1 is the product readability target for disabled labels, not a WCAG requirement for inactive controls.");
        }));
    }

    [TestCase(BootstrapVariant.Primary, false, false)]
    [TestCase(BootstrapVariant.Secondary, true, true)]
    [TestCase(BootstrapVariant.Danger, false, true)]
    public void DisabledPaletteDoesNotRegainSemanticOrInteractionEmphasis(BootstrapVariant variant, bool outline, bool selected)
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Dark);
        foreach (var visualState in new[] { BootstrapButtonVisualState.Normal, BootstrapButtonVisualState.Hover, BootstrapButtonVisualState.Pressed })
        {
            var palette = BootstrapButtonRenderLogic.ResolvePalette(colors, variant, outline, enabled: false, selected, visualState);
            Assert.Multiple((Action)(() =>
            {
                Assert.That(palette.Background, Is.EqualTo(colors.SurfaceSecondary));
                Assert.That(palette.Foreground, Is.EqualTo(colors.Text));
                Assert.That(palette.Border, Is.EqualTo(colors.Disabled));
            }));
        }
    }

    [Test]
    public void DisabledPaintUsesReadableThemeTextForLabelAndIconAcrossThemeChanges()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var font = new Font("Segoe UI", 12f, FontStyle.Bold);
            var renderer = new RecordingIconRenderer();
            using var button = new StyleProbeBootstrapButton
            {
                Size = new Size(220, 56),
                Text = "MMMM",
                Font = font,
                Icon = IconDescriptor.Framework(FrameworkIconGlyph.Check),
                IconRenderer = renderer,
                Variant = BootstrapVariant.Secondary,
                Outline = true,
                Enabled = false
            };

            foreach (var mode in new[] { BootstrapThemeMode.Light, BootstrapThemeMode.Dark, BootstrapThemeMode.Light })
            {
                var theme = BootstrapTheme.CreateDefault(mode);
                BootstrapThemeManager.CurrentTheme = theme;
                renderer.LastColor = Color.Empty;
                using var bitmap = new Bitmap(button.Width, button.Height);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    button.PaintInto(graphics);
                }

                var textPixels = CountPixelsNearColor(bitmap, theme.Colors.Text,
                    Rectangle.Inflate(button.ClientRectangle, -10, -10));
                Assert.Multiple((Action)(() =>
                {
                    Assert.That(bitmap.GetPixel(8, 8).ToArgb(), Is.EqualTo(theme.Colors.SurfaceSecondary.ToArgb()), mode.ToString());
                    Assert.That(renderer.LastColor, Is.EqualTo(theme.Colors.Text), "The actual icon paint path must receive the disabled foreground.");
                    Assert.That(textPixels, Is.GreaterThanOrEqualTo(12),
                        "The label must paint with the readable foreground; the recording icon renderer paints no pixels.");
                }));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void DisabledButtonRemainsInertWhenLoadingToggles()
    {
        using var button = new BootstrapButton { Enabled = false, Selected = true };
        var clickCount = 0;
        button.Click += (_, _) => clickCount++;

        button.PerformClick();
        button.Loading = true;
        button.PerformClick();
        button.Loading = false;
        button.PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(clickCount, Is.Zero);
            Assert.That(button.Enabled, Is.False);
            Assert.That(button.Selected, Is.True);
        }));
    }

    [Test]
    public void BorderRadiusRejectsValuesBelowThemeSentinel()
    {
        using var button = new BootstrapButton();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => button.BorderRadius = -2));
        Assert.DoesNotThrow((Action)(() => button.BorderRadius = -1));
        Assert.DoesNotThrow((Action)(() => button.BorderRadius = 0));
    }

    [Test]
    public void GroupCornerRadiusOverrideDoesNotChangePublicRadius()
    {
        using var button = new BootstrapButton { BorderRadius = 12 };
        var groupRadius = new CornerRadius(12f, 0f, 0f, 12f);

        button.GroupCornerRadius = groupRadius;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(button.BorderRadius, Is.EqualTo(12));
            Assert.That(button.GetEffectiveCornerRadius(BootstrapThemeMetrics.Default), Is.EqualTo(groupRadius));
        }));
    }

    private static int CountPixelsNearColor(Bitmap bitmap, Color expected, Rectangle bounds)
    {
        var count = 0;
        for (var y = bounds.Top; y < bounds.Bottom; y++)
        {
            for (var x = bounds.Left; x < bounds.Right; x++)
            {
                var actual = bitmap.GetPixel(x, y);
                if (Math.Abs(actual.R - expected.R) <= 8 && Math.Abs(actual.G - expected.G) <= 8 && Math.Abs(actual.B - expected.B) <= 8)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private sealed class RecordingIconRenderer : IIconRenderer
    {
        public Color LastColor { get; set; }

        public bool TryRender(Graphics graphics, IconDescriptor descriptor, Rectangle bounds, Color color)
        {
            LastColor = color;
            return true;
        }
    }

    private sealed class StyleProbeBootstrapButton : BootstrapButton
    {
        public bool HasStyle(ControlStyles style)
        {
            return GetStyle(style);
        }

        public void PaintInto(Graphics graphics)
        {
            OnPaint(new PaintEventArgs(graphics, ClientRectangle));
        }
    }
}
