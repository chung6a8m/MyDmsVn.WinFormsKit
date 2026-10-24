using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using MyDmsVn.BootstrapSourceGrid.Theming;
using NUnit.Framework;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
public sealed class BootstrapSourceGridThemeAdapterTests
{
    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void MapsFrameworkSemanticTokens(BootstrapThemeMode mode)
    {
        var theme = BootstrapTheme.CreateDefault(mode);
        var colors = theme.Colors;

        var snapshot = BootstrapSourceGridThemeAdapter.CreateSnapshot(theme);

        Assert.That(snapshot.CellBackColor, Is.EqualTo(colors.Surface));
        Assert.That(snapshot.AlternateCellBackColor, Is.EqualTo(colors.SurfaceSecondary));
        Assert.That(snapshot.CellForeColor, Is.EqualTo(colors.Text));
        Assert.That(snapshot.MutedForeColor, Is.EqualTo(colors.MutedText));
        Assert.That(snapshot.HeaderBackColor, Is.EqualTo(colors.SurfaceSecondary));
        Assert.That(snapshot.HeaderForeColor, Is.EqualTo(colors.Text));
        Assert.That(snapshot.BorderColor, Is.EqualTo(colors.Border));
        Assert.That(snapshot.SelectionBackColor, Is.EqualTo(colors.Primary));
        Assert.That(snapshot.SelectionForeColor, Is.EqualTo(
            ColorUtil.GetContrastingTextColor(colors.Primary, colors.Light, colors.Dark)));
        Assert.That(snapshot.FocusColor, Is.EqualTo(colors.Focus));
        Assert.That(snapshot.DisabledColor, Is.EqualTo(colors.Disabled));
        Assert.That(snapshot.HoverColor, Is.EqualTo(colors.Hover));
        Assert.That(snapshot.ActiveColor, Is.EqualTo(colors.Active));
        Assert.That(snapshot.BodyFont, Is.SameAs(theme.Typography.Body));
    }
}
