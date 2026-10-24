using System;
using System.Drawing;
using System.Threading;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridFontTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void DefaultConstructionUsesCurrentThemeBodyFont()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var theme = CreateThemeWithBodyFont(
            BootstrapThemeMode.Light,
            new BootstrapFontToken("Segoe UI", 10.5f, FontStyle.Bold));

        try
        {
            BootstrapThemeManager.CurrentTheme = theme;

            using (var grid = new BootstrapSourceGridControl())
            {
                AssertFontMatches(grid.Font, theme.Typography.Body);
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void ThemeSwitchReplacesThemeOwnedFont()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var light = CreateThemeWithBodyFont(
            BootstrapThemeMode.Light,
            new BootstrapFontToken("Segoe UI", 9f));
        var dark = CreateThemeWithBodyFont(
            BootstrapThemeMode.Dark,
            new BootstrapFontToken("Segoe UI", 11f, FontStyle.Bold));

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var grid = new BootstrapSourceGridControl())
            {
                var previousFont = grid.Font;

                BootstrapThemeManager.CurrentTheme = dark;

                Assert.That(grid.Font, Is.Not.SameAs(previousFont));
                AssertFontMatches(grid.Font, dark.Typography.Body);
                Action usePreviousFont = () =>
                {
                    _ = previousFont.GetHeight();
                };
                Assert.Throws<ArgumentException>(usePreviousFont);
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void DifferentUnavailableFamiliesReuseResolvedGridFont()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var first = CreateThemeWithBodyFont(
            BootstrapThemeMode.Light,
            new BootstrapFontToken("MissingSourceGridFontFamilyOne", 11f));
        var second = CreateThemeWithBodyFont(
            BootstrapThemeMode.Dark,
            new BootstrapFontToken("MissingSourceGridFontFamilyTwo", 11f));

        try
        {
            BootstrapThemeManager.CurrentTheme = first;
            using (var grid = new BootstrapSourceGridControl())
            {
                var fallbackFont = grid.Font;
                Assert.That(fallbackFont.Name, Is.Not.EqualTo(first.Typography.Body.FontFamilyName));

                BootstrapThemeManager.CurrentTheme = second;

                Assert.That(grid.Font, Is.SameAs(fallbackFont));
                Assert.That(grid.OwnedThemeFont, Is.SameAs(fallbackFont));
                Assert.DoesNotThrow((Action)(() => fallbackFont.GetHeight()));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void ConsumerAssignedFontSurvivesThemeSwitch()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = CreateThemeWithBodyFont(
            BootstrapThemeMode.Dark,
            new BootstrapFontToken("Segoe UI", 12f));

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var consumerFont = new Font("Arial", 10f, FontStyle.Italic))
            using (var grid = new BootstrapSourceGridControl())
            {
                grid.Font = consumerFont;

                BootstrapThemeManager.CurrentTheme = dark;

                Assert.That(grid.Font, Is.SameAs(consumerFont));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void DisposeDoesNotDisposeConsumerFont()
    {
        using (var consumerFont = new Font("Arial", 10f))
        {
            var grid = new BootstrapSourceGridControl
            {
                Font = consumerFont,
            };

            grid.Dispose();

            Assert.That(consumerFont.GetHeight(), Is.GreaterThan(0f));
        }
    }

    private static BootstrapTheme CreateThemeWithBodyFont(
        BootstrapThemeMode mode,
        BootstrapFontToken bodyFont)
    {
        var defaults = BootstrapTheme.CreateDefault(mode);
        var typography = defaults.Typography;
        return new BootstrapTheme(
            mode,
            defaults.Colors,
            defaults.Metrics,
            new BootstrapThemeTypography(
                bodyFont,
                typography.BodySmall,
                typography.Label,
                typography.HeadingSmall,
                typography.HeadingMedium));
    }

    private static void AssertFontMatches(Font font, BootstrapFontToken token)
    {
        Assert.That(font.Name, Is.EqualTo(token.FontFamilyName).IgnoreCase);
        Assert.That(font.SizeInPoints, Is.EqualTo(token.SizeInPoints).Within(0.01f));
        Assert.That(font.Style, Is.EqualTo(token.Style));
    }
}
