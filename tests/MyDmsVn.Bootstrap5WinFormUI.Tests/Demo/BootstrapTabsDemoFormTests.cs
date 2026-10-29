using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapTabsDemoFormTests
{
    [Test]
    public void GalleryRouteCreatesComparisonForm()
    {
        using var main = new MainForm();
        main.CreateControl();
        var sidebar = Descendants(main).OfType<BootstrapSidebar>().Single();
        sidebar.SelectedItem = sidebar.Items.Single(item => item.Text == "Navigation / BootstrapTabs");
        main.PerformLayout();

        Assert.That(Descendants(main).OfType<BootstrapTabsDemoForm>().Count(), Is.EqualTo(1));
    }

    [Test]
    public void ComparisonSupportsStyleHotTrackAndPageActions()
    {
        using var form = new BootstrapTabsDemoForm();
        form.Show();
        form.PerformLayout();
        var oldTabs = Descendants(form).OfType<BootstrapTabControl>().Single();
        var tabs = Descendants(form).OfType<BootstrapTabs>().Single();
        var checks = Descendants(form).OfType<CheckBox>().ToDictionary(c => c.AccessibleName ?? string.Empty);
        var buttons = Descendants(form).OfType<Button>().ToDictionary(b => b.AccessibleName ?? string.Empty);
        var styles = Descendants(form).OfType<ComboBox>().Single(c => c.AccessibleName == "BootstrapTabs style");
        var variants = Descendants(form).OfType<ComboBox>().Single(c => c.AccessibleName == "BootstrapTabs variant");

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tabs.TabStyle, Is.EqualTo(BootstrapTabStyle.Underline));
            Assert.That(tabs.HotTrack, Is.True);
            Assert.That(oldTabs.HotTrack, Is.True);
            Assert.That(tabs.TabPages.Cast<TabPage>().Any(p => p.Text.Length > 40), Is.True);
            Assert.That(tabs.TabPages.Cast<TabPage>().Any(p => p.ImageKey.Length > 0), Is.True);
            Assert.That(tabs.ShowToolTips, Is.True);
            Assert.That(tabs.TabPages.Cast<TabPage>().Any(p => Descendants(p).OfType<TextBox>().Any()), Is.True);
            Assert.That(tabs.TabPages.Cast<TabPage>().Any(p => Descendants(p).OfType<Button>().Any()), Is.True);
        }));

        checks["BootstrapTabs HotTrack"].Checked = false;
        checks["Legacy tabs HotTrack"].Checked = false;
        Assert.That(tabs.HotTrack, Is.False);
        Assert.That(oldTabs.HotTrack, Is.False);
        styles.SelectedItem = BootstrapTabStyle.Pills;
        variants.SelectedItem = BootstrapVariant.Success;
        checks["BootstrapTabs fill"].Checked = true;
        Assert.That(tabs.TabStyle, Is.EqualTo(BootstrapTabStyle.Pills));
        Assert.That(tabs.Variant, Is.EqualTo(BootstrapVariant.Success));
        Assert.That(tabs.Fill, Is.True);

        var count = tabs.TabCount;
        buttons["Add BootstrapTabs page"].PerformClick();
        Assert.That(tabs.TabCount, Is.EqualTo(count + 1));
        var added = tabs.TabPages[tabs.TabCount - 1];
        buttons["Remove BootstrapTabs page"].PerformClick();
        Assert.That(tabs.TabCount, Is.EqualTo(count));
        Assert.That(added.IsDisposed, Is.True);
        buttons["Add BootstrapTabs overflow pages"].PerformClick();
        Assert.That(tabs.TabCount, Is.GreaterThan(count + 3));
        var pagesBeforeClear = tabs.TabPages.Cast<TabPage>().ToArray();
        buttons["Clear BootstrapTabs pages"].PerformClick();
        Assert.That(tabs.TabCount, Is.Zero);
        Assert.That(pagesBeforeClear.All(page => page.IsDisposed), Is.True);
    }

    [Test]
    public void CloseRequestIsOwnerHandledAndChildrenAreDisposed()
    {
        using var form = new BootstrapTabsDemoForm();
        form.Show();
        form.PerformLayout();
        var tabs = Descendants(form).OfType<BootstrapTabs>().Single();
        var oldTabs = Descendants(form).OfType<BootstrapTabControl>().Single();
        var oldPages = oldTabs.TabPages.Cast<TabPage>().ToArray();
        var closeButton = Descendants(form).OfType<Button>().Single(b => b.AccessibleName == "Close BootstrapTabs page");
        var closePage = tabs.TabPages.Cast<TabPage>().Single(p => p.Text == "Đóng được");
        var selected = tabs.SelectedTab;
        Assert.That(tabs.IsTabClosable(closePage), Is.True);

        closeButton.PerformClick();
        Assert.That(tabs.TabPages.Contains(closePage), Is.False);
        Assert.That(tabs.SelectedTab, Is.SameAs(selected));
        Assert.That(closePage.IsDisposed, Is.True);

        var remaining = tabs.TabPages.Cast<TabPage>().ToArray();
        form.Dispose();
        Assert.That(tabs.IsDisposed, Is.True);
        Assert.That(oldTabs.IsDisposed, Is.True);
        Assert.That(remaining.All(p => p.IsDisposed), Is.True);
        Assert.That(oldPages.All(p => p.IsDisposed), Is.True);
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}
