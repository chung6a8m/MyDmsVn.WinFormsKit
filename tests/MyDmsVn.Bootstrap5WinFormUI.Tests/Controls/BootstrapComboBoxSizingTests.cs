using System;
using System.Drawing;
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
public sealed class BootstrapComboBoxSizingTests
{
    [SetUp]
    public void SetUp()
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
    }

    [Test]
    public void DefaultClosedHeightMatchesTextBoxAfterHandleCreation()
    {
        using var font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
        using var form = new Form { ShowInTaskbar = false, ClientSize = new Size(520, 160) };
        using var textBox = new BootstrapTextBox { Location = new Point(20, 20), Width = 210, Font = font };
        using var comboBox = new BootstrapComboBox
        {
            Location = new Point(260, 20),
            Width = 210,
            Font = font,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        comboBox.Items.AddRange(new object[] { "Alpha", "Beta" });
        comboBox.SelectedIndex = 0;
        form.Controls.Add(textBox);
        form.Controls.Add(comboBox);

        form.Show();
        PumpMessages();
        LogMetrics("default", textBox, comboBox);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(textBox.IsHandleCreated, Is.True);
            Assert.That(comboBox.IsHandleCreated, Is.True);
            Assert.That(comboBox.Height, Is.EqualTo(textBox.Height),
                "ControlHeight is an outer-height contract; native chrome must not be added on top of it.");
            Assert.That(comboBox.Bottom, Is.EqualTo(textBox.Bottom));
        }));
    }

    [TestCase(600)]
    [TestCase(430)]
    public void AutoSizeFlowLayoutContainsBottomBorderInSingleAndWrappedRows(int availableWidth)
    {
        using var font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
        using var form = new Form { ShowInTaskbar = false, ClientSize = new Size(availableWidth + 40, 260) };
        using var flow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Location = new Point(10, 10),
            MaximumSize = new Size(availableWidth, 0),
            WrapContents = true
        };
        using var textBox = new BootstrapTextBox { Width = 210, Font = font, Margin = new Padding(3) };
        using var comboBox = new BootstrapComboBox
        {
            Width = 140,
            Font = font,
            Margin = new Padding(3),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        using var button = new BootstrapButton { Width = 110, Text = "Refresh", Font = font, Margin = new Padding(3) };
        flow.Controls.Add(textBox);
        flow.Controls.Add(comboBox);
        flow.Controls.Add(button);
        form.Controls.Add(flow);

        flow.PerformLayout();
        form.Show();
        PumpMessages();
        flow.PerformLayout();
        PumpMessages();
        LogMetrics("flow-" + availableWidth, textBox, comboBox);

        foreach (Control child in flow.Controls)
        {
            Assert.That(child.Bottom + child.Margin.Bottom, Is.LessThanOrEqualTo(flow.DisplayRectangle.Bottom),
                child.GetType().Name + " extends below the FlowLayoutPanel display rectangle.");
        }
    }

    [Test]
    public void ComboOnlyPreHandleLayoutSettlesWithoutClippingOrGrowth()
    {
        using var form = new Form { ShowInTaskbar = false, ClientSize = new Size(320, 180) };
        using var flow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Location = new Point(10, 10),
            WrapContents = true,
            Width = 260
        };
        using var comboBox = new BootstrapComboBox { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
        flow.Controls.Add(comboBox);
        form.Controls.Add(flow);
        flow.PerformLayout();
        var preHandleHeight = flow.Height;

        form.Show();
        PumpMessages();
        flow.PerformLayout();
        PumpMessages();
        var settledHeight = flow.Height;
        flow.PerformLayout();
        PumpMessages();

        TestContext.Progress.WriteLine(
            $"combo-only preHandleFlow={preHandleHeight}, settledFlow={settledHeight}, combo={comboBox.Height}, preferred={comboBox.GetPreferredSize(Size.Empty)}");
        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.Bottom + comboBox.Margin.Bottom, Is.LessThanOrEqualTo(flow.DisplayRectangle.Bottom));
            Assert.That(flow.Height, Is.EqualTo(settledHeight), "Repeated layout must not accumulate native ComboBox height.");
        }));
    }

    [Test]
    public void FontAndThemeChangesDoNotAccumulateHeightOrSelectionEvents()
    {
        using var smallFont = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
        using var largeFont = new Font("Segoe UI", 16f, FontStyle.Regular, GraphicsUnit.Point);
        using var form = new Form { ShowInTaskbar = false };
        using var comboBox = new BootstrapComboBox
        {
            Font = smallFont,
            Width = 220,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        comboBox.Items.AddRange(new object[] { "Alpha", "Beta" });
        comboBox.SelectedIndex = 1;
        form.Controls.Add(comboBox);
        form.Show();
        PumpMessages();
        var defaultHeight = comboBox.Height;
        var selectionEvents = 0;
        comboBox.SelectedIndexChanged += (_, _) => selectionEvents++;

        comboBox.Font = largeFont;
        PumpMessages();
        var largeHeight = comboBox.Height;
        comboBox.Font = smallFont;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        PumpMessages();
        var restoredHeight = comboBox.Height;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        PumpMessages();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(largeHeight, Is.GreaterThan(defaultHeight));
            Assert.That(restoredHeight, Is.EqualTo(defaultHeight));
            Assert.That(comboBox.Height, Is.EqualTo(defaultHeight));
            Assert.That(comboBox.SelectedIndex, Is.EqualTo(1));
            Assert.That(selectionEvents, Is.Zero);
        }));
    }

    [Test]
    public void HandleRecreationRestoresDefaultOuterHeight()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var comboBox = new RecreatingBootstrapComboBox
        {
            Width = 220,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        form.Controls.Add(comboBox);
        form.Show();
        PumpMessages();
        var expected = comboBox.Height;

        comboBox.ForceHandleRecreation();
        PumpMessages();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.IsHandleCreated, Is.True);
            Assert.That(comboBox.Height, Is.EqualTo(expected));
            Assert.That(comboBox.Height, Is.EqualTo(DpiScaler.Scale(
                BootstrapThemeManager.CurrentTheme.Metrics.ControlHeight,
                comboBox.DeviceDpi)));
        }));
    }

    [Test]
    public void CallerRegionAndExplicitFontRemainOwnedByCaller()
    {
        using var callerFont = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Point);
        using var callerRegion = new Region(new Rectangle(0, 0, 120, 24));
        using var form = new Form { ShowInTaskbar = false };
        using var comboBox = new RecreatingBootstrapComboBox
        {
            Font = callerFont,
            Region = callerRegion,
            Width = 220
        };
        form.Controls.Add(comboBox);
        form.Show();
        PumpMessages();
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        comboBox.ForceHandleRecreation();
        PumpMessages();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.Font, Is.SameAs(callerFont));
            Assert.That(comboBox.Region, Is.SameAs(callerRegion));
            Assert.That(callerRegion.IsVisible(10, 10), Is.True);
        }));
    }

    [Test]
    public void LargeFontUsesSafeMinimumWithoutClipping()
    {
        using var largeFont = new Font("Segoe UI", 16f, FontStyle.Regular, GraphicsUnit.Point);
        using var form = new Form { ShowInTaskbar = false };
        using var comboBox = new BootstrapComboBox { Font = largeFont, Width = 240 };
        form.Controls.Add(comboBox);
        form.Show();
        PumpMessages();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.Height, Is.GreaterThan(DpiScaler.Scale(
                BootstrapThemeManager.CurrentTheme.Metrics.ControlHeight,
                comboBox.DeviceDpi)));
            Assert.That(comboBox.ItemHeight, Is.GreaterThanOrEqualTo(largeFont.Height));
        }));
    }

    [Test]
    public void SimpleModeKeepsNativeMultiLineHeightPolicy()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var comboBox = new BootstrapComboBox
        {
            DropDownStyle = ComboBoxStyle.Simple,
            Size = new Size(220, 120)
        };
        form.Controls.Add(comboBox);
        form.Show();
        PumpMessages();

        Assert.That(comboBox.Height, Is.GreaterThan(80),
            "Simple mode is a native multi-line list and must not be forced to the closed-input height.");
    }

    private static void LogMetrics(string scenario, BootstrapTextBox textBox, BootstrapComboBox comboBox)
    {
        TestContext.Progress.WriteLine(
            $"{scenario}: textHeight={textBox.Height}, comboHeight={comboBox.Height}, itemHeight={comboBox.ItemHeight}, " +
            $"preferredHeight={comboBox.PreferredHeight}, getPreferred={comboBox.GetPreferredSize(Size.Empty)}, dpi={comboBox.DeviceDpi}");
    }

    private static void PumpMessages()
    {
        for (var index = 0; index < 6; index++)
        {
            Application.DoEvents();
        }
    }

    private sealed class RecreatingBootstrapComboBox : BootstrapComboBox
    {
        internal void ForceHandleRecreation() => RecreateHandle();
    }
}
