using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapLookupPopupTests
{
    [Test]
    public void LookupEmbeddedGridStaysBorderlessAcrossThemeAndReopen()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var standalone = new BootstrapDataGridView();
            using var host = new Form();
            using var lookup = CreateLookup();
            host.Controls.Add(lookup);
            host.Show();

            lookup.OpenDropDown();
            Application.DoEvents();
            lookup.CloseDropDown();
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            lookup.OpenDropDown();
            Application.DoEvents();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(standalone.BorderStyle, Is.EqualTo(BorderStyle.FixedSingle));
                Assert.That(lookup.ResultsGrid.BorderStyle, Is.EqualTo(BorderStyle.None));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void OwnedPopupMetricsFollowControllerDpiWithoutNativeDpiAssumptions()
    {
        using var host = new Form { Size = new Size(700, 600) };
        using var lookup = CreateLookup();
        host.Controls.Add(lookup);
        host.Show();
        lookup.OpenDropDown();
        var content = (BootstrapLookupDropDownContent)lookup.ResultsGrid.Parent!;
        var footer = content.Controls.Cast<Control>().Single(control => !ReferenceEquals(control, lookup.ResultsGrid));
        var controller = GetController(lookup);

        foreach (var dpi in new[] { 96, 120, 144 })
        {
            controller.ApplyOwnerDpiChange(dpi);
            Assert.That(footer.Height, Is.EqualTo(DpiScaler.Scale(32, dpi)), $"effective dpi {dpi}");
        }
    }

    [Test]
    public void OpenPopupRadiusChangesWithoutRecreatingHandleOrSelection()
    {
        var products = new BindingList<Product>();
        for (var index = 1; index <= 30; index++)
        {
            products.Add(new Product(index, $"Item {index:00}"));
        }

        using var host = new Form { Size = new Size(800, 700), StartPosition = FormStartPosition.Manual, Location = new Point(80, 80) };
        using var lookup = new CaretLookupBox
        {
            Width = 280,
            MaxDropDownHeight = 180,
            DisplayMember = "Name",
            ValueMember = "Id",
            DataSource = products,
            SearchDebounceMilliseconds = 0
        };
        host.Controls.Add(lookup);
        host.Show();
        lookup.Text = "Item";
        lookup.OpenDropDown();
        Application.DoEvents();
        Assert.That(lookup.ResultsGrid.Rows.Count, Is.GreaterThan(15));
        lookup.ResultsGrid.FirstDisplayedScrollingRowIndex = 15;
        lookup.SetCaret(2, 2);
        var content = (BootstrapLookupDropDownContent)lookup.ResultsGrid.Parent!;
        var surface = (BootstrapOverlaySurface)content.Parent!.Parent!;
        var popup = (BootstrapOverlayDropDown)surface.TopLevelControl!;
        var popupHandle = popup.Handle;
        var highlighted = lookup.HighlightedItem;
        var query = lookup.Text;
        var selectedValue = lookup.SelectedValue;
        var valueChanged = 0;
        var committed = 0;
        var gridSelectionChanged = 0;
        lookup.SelectedValueChanged += (_, _) => valueChanged++;
        lookup.SelectionCommitted += (_, _) => committed++;
        lookup.ResultsGrid.SelectionChanged += (_, _) => gridSelectionChanged++;

        lookup.BorderRadius = 0;
        Application.DoEvents();
        var squareCornerVisible = popup.Region!.IsVisible(1f, 1f);
        lookup.BorderRadius = 12;
        Application.DoEvents();
        var roundedCornerVisible = popup.Region!.IsVisible(1f, 1f);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(popup.Handle, Is.EqualTo(popupHandle));
            Assert.That(lookup.IsDropDownOpen, Is.True);
            Assert.That(squareCornerVisible, Is.True);
            Assert.That(roundedCornerVisible, Is.False);
            Assert.That(lookup.ResultsGrid.FirstDisplayedScrollingRowIndex, Is.EqualTo(15),
                "A radius-only refresh must not scroll back to the highlighted row.");
            Assert.That(lookup.ResultsGrid.CurrentCell?.RowIndex, Is.EqualTo(0));
            Assert.That(lookup.HighlightedItem, Is.SameAs(highlighted));
            Assert.That(lookup.SelectedValue, Is.EqualTo(selectedValue));
            Assert.That(lookup.Text, Is.EqualTo(query));
            Assert.That(lookup.Caret, Is.EqualTo((2, 2)));
            Assert.That(valueChanged, Is.Zero);
            Assert.That(committed, Is.Zero);
            Assert.That(gridSelectionChanged, Is.Zero);
        }));
    }

    [Test]
    public void OpenPopupReappliesSurfaceAndContentWhenThemeChanges()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            using var host = new Form();
            using var lookup = CreateLookup();
            host.Controls.Add(lookup);
            host.Show();
            lookup.OpenDropDown();
            Application.DoEvents();
            var content = (BootstrapLookupDropDownContent)lookup.ResultsGrid.Parent!;
            var surface = GetSurface(content);
            var darkBase = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            var dark = new BootstrapTheme(
                BootstrapThemeMode.Dark,
                darkBase.Colors,
                darkBase.Metrics,
                new BootstrapThemeTypography(
                    new BootstrapFontToken("Segoe UI", 11f, FontStyle.Bold),
                    darkBase.Typography.BodySmall,
                    darkBase.Typography.Label,
                    darkBase.Typography.HeadingSmall,
                    darkBase.Typography.HeadingMedium));

            BootstrapThemeManager.CurrentTheme = dark;
            Application.DoEvents();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(lookup.IsDropDownOpen, Is.True);
                Assert.That(surface.BackColor, Is.EqualTo(dark.Colors.Surface));
                Assert.That(content.Font.SizeInPoints, Is.EqualTo(11f).Within(0.05f));
                Assert.That(content.Font.Style, Is.EqualTo(FontStyle.Bold));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void DefaultThemeSwitchKeepsLookupAndPopupLabelFontsUsable()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            using var host = new Form();
            using var lookup = CreateLookup();
            host.Controls.Add(lookup);
            host.Show();
            lookup.OpenDropDown();
            Application.DoEvents();
            var content = (BootstrapLookupDropDownContent)lookup.ResultsGrid.Parent!;
            var surface = GetSurface(content);
            var labels = Descendants(host)
                .Concat(Descendants(surface))
                .OfType<Label>()
                .Distinct()
                .ToArray();

            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

            Assert.That(labels, Is.Not.Empty);
            Assert.That(labels.All(HasUsableFont), Is.True);
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void ThemeSwitchWithUnavailableRequestedFontKeepsLookupAndPopupLabelFontsUsable()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        var unavailableFamily = "BootstrapFontMissing-" + Guid.NewGuid().ToString("N");
        try
        {
            BootstrapThemeManager.CurrentTheme = CreateThemeWithBodyFont(
                BootstrapThemeMode.Light,
                unavailableFamily);
            using var host = new Form();
            using var lookup = CreateLookup();
            host.Controls.Add(lookup);
            host.Show();
            lookup.OpenDropDown();
            Application.DoEvents();
            var content = (BootstrapLookupDropDownContent)lookup.ResultsGrid.Parent!;
            var surface = GetSurface(content);
            var labels = Descendants(host)
                .Concat(Descendants(surface))
                .OfType<Label>()
                .Distinct()
                .ToArray();

            Assert.That(lookup.Font.Name, Is.Not.EqualTo(unavailableFamily).IgnoreCase);

            BootstrapThemeManager.CurrentTheme = CreateThemeWithBodyFont(
                BootstrapThemeMode.Dark,
                unavailableFamily);

            Assert.That(labels, Is.Not.Empty);
            Assert.That(labels.All(HasUsableFont), Is.True);
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void OpenPopupReappliesMetricsAndBoundsWhenOwnerDpiChanges()
    {
        using var host = new Form { Size = new Size(700, 600) };
        using var lookup = CreateLookup();
        lookup.MaxDropDownHeight = 80;
        lookup.DataSource = new BindingList<Product>
        {
            new(1, "One"), new(2, "Two"), new(3, "Three"), new(4, "Four"),
            new(5, "Five"), new(6, "Six"), new(7, "Seven"), new(8, "Eight")
        };
        host.Controls.Add(lookup);
        host.Show();
        lookup.OpenDropDown();
        Application.DoEvents();
        var content = (BootstrapLookupDropDownContent)lookup.ResultsGrid.Parent!;
        var surface = GetSurface(content);
        var controller = GetController(lookup);

        controller.ApplyOwnerDpiChange(96);
        Application.DoEvents();
        var surfaceAt96Dpi = surface;
        var heightAt96Dpi = surface.Height;

        controller.ApplyOwnerDpiChange(192);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.IsDropDownOpen, Is.True);
            Assert.That(GetSurface(content), Is.SameAs(surfaceAt96Dpi));
            Assert.That(heightAt96Dpi, Is.EqualTo(80));
            Assert.That(surface.Height, Is.EqualTo(160));
        }));
    }

    [Test]
    public void OpenPopupScalesLogicalFixedColumnDimensionsWhenDpiChanges()
    {
        using var host = new Form { Size = new Size(700, 600) };
        using var lookup = CreateLookup();
        lookup.Columns.Add(new BootstrapLookupColumnDefinition
        {
            DataPropertyName = "Name",
            HeaderText = "Product",
            Width = 120,
            MinimumWidth = 40
        });
        host.Controls.Add(lookup);
        host.Show();
        lookup.OpenDropDown();
        var controller = GetController(lookup);

        controller.ApplyOwnerDpiChange(96);
        var widthAt96 = lookup.ResultsGrid.Columns[0].Width;
        var minimumAt96 = lookup.ResultsGrid.Columns[0].MinimumWidth;
        controller.ApplyOwnerDpiChange(192);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(widthAt96, Is.EqualTo(120));
            Assert.That(minimumAt96, Is.EqualTo(40));
            Assert.That(lookup.ResultsGrid.Columns[0].Width, Is.EqualTo(240));
            Assert.That(lookup.ResultsGrid.Columns[0].MinimumWidth, Is.EqualTo(80));
        }));
    }

    [Test]
    public void PublicOpenCloseAndCancelHaveDistinctStateSemantics()
    {
        using var host = new Form();
        using var lookup = CreateLookup();
        host.Controls.Add(lookup);
        host.Show();
        lookup.SelectValue(1);
        lookup.Text = "pending";
        lookup.OpenDropDown();
        Assert.That(lookup.IsDropDownOpen, Is.True);

        lookup.CloseDropDown();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.IsDropDownOpen, Is.False);
            Assert.That(lookup.Text, Is.EqualTo("pending"));
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
        }));

        lookup.OpenDropDown();
        lookup.CancelPendingEdit();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.IsDropDownOpen, Is.False);
            Assert.That(lookup.Text, Is.EqualTo("Coffee"));
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
        }));
        host.Close();
    }

    [Test]
    public void RefreshRaisesRequestWithoutCommittingOrChangingPendingText()
    {
        using var lookup = CreateLookup();
        lookup.SelectValue(1);
        lookup.Text = "cof";
        var refreshes = 0;
        lookup.RefreshRequested += (_, e) => { refreshes++; Assert.That(e.QueryText, Is.EqualTo("cof")); };
        lookup.RefreshResults();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(refreshes, Is.EqualTo(1));
            Assert.That(lookup.Text, Is.EqualTo("cof"));
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
        }));
    }

    [Test]
    public void SameValuePresentationChangeRepositionsOpenAutosizedPopup()
    {
        var source = new BindingList<Product> { new(1, "A") };
        using var host = new Form { Size = new Size(600, 400) };
        using var lookup = new BootstrapLookupBox
        {
            Width = 180,
            DropDownWidth = 180,
            MaxDropDownHeight = 300,
            DisplayMember = "Name",
            ValueMember = "Id",
            DataSource = source,
            SearchDebounceMilliseconds = 0
        };
        lookup.Columns.Add(new BootstrapLookupColumnDefinition
        {
            DataPropertyName = "Name",
            HeaderText = "Product",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        host.Controls.Add(lookup);
        host.Show();
        lookup.OpenDropDown();
        Application.DoEvents();
        var content = (BootstrapLookupDropDownContent)lookup.ResultsGrid.Parent!;
        var initialHeight = GetSurface(content).Height;

        source[0] = new Product(1, new string('W', 80));
        Application.DoEvents();

        var horizontalScrollBar = lookup.ResultsGrid.Controls.OfType<HScrollBar>().Single();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(horizontalScrollBar.Visible, Is.True);
            Assert.That(GetSurface(content).Height, Is.GreaterThan(initialHeight));
            Assert.That(lookup.ResultsGrid.GetRowDisplayRectangle(0, false).Bottom, Is.LessThanOrEqualTo(horizontalScrollBar.Top));
        }));
    }

    private static BootstrapLookupBox CreateLookup() => new BootstrapLookupBox
    {
        DisplayMember = "Name",
        ValueMember = "Id",
        DataSource = new BindingList<Product> { new(1, "Coffee"), new(2, "Tea") }
    };

    private static BootstrapOverlaySurface GetSurface(BootstrapLookupDropDownContent content) =>
        (BootstrapOverlaySurface)content.Parent!.Parent!;

    private static BootstrapTheme CreateThemeWithBodyFont(
        BootstrapThemeMode mode,
        string fontFamilyName)
    {
        var defaults = BootstrapTheme.CreateDefault(mode);
        return new BootstrapTheme(
            mode,
            defaults.Colors,
            defaults.Metrics,
            new BootstrapThemeTypography(
                new BootstrapFontToken(fontFamilyName, 9f),
                defaults.Typography.BodySmall,
                defaults.Typography.Label,
                defaults.Typography.HeadingSmall,
                defaults.Typography.HeadingMedium));
    }

    private static bool HasUsableFont(Label label)
    {
        IntPtr handle = IntPtr.Zero;
        try
        {
            handle = label.Font.ToHfont();
            return handle != IntPtr.Zero;
        }
        catch (ArgumentException)
        {
            return false;
        }
        finally
        {
            if (handle != IntPtr.Zero)
            {
                DeleteObject(handle);
            }
        }
    }

    private static System.Collections.Generic.IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static BootstrapLookupDropDownController GetController(BootstrapLookupBox lookup)
    {
        var field = typeof(BootstrapLookupBox).GetField("_dropDownController", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (BootstrapLookupDropDownController)field.GetValue(lookup)!;
    }

    private sealed class Product
    {
        internal Product(int id, string name) { Id = id; Name = name; }
        public int Id { get; }
        public string Name { get; }
    }

    private sealed class CaretLookupBox : BootstrapLookupBox
    {
        internal (int Start, int Length) Caret => (Editor.SelectionStart, Editor.SelectionLength);

        internal void SetCaret(int start, int length)
        {
            Editor.SelectionStart = start;
            Editor.SelectionLength = length;
        }
    }

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr handle);
}
