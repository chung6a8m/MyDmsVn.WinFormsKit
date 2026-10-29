using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
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
}
