using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapInputShellContourTests
{
    private static readonly Color ParentColor = Color.Fuchsia;

    [Test]
    public void EquivalentInputMetricsProduceSameOuterCornerContour()
    {
        using var form = new Form { BackColor = ParentColor, ClientSize = new Size(1000, 180), ShowInTaskbar = false };
        var controls = CreateComparableControls();
        var x = 10;
        foreach (var control in controls)
        {
            control.Location = new Point(x, 20);
            form.Controls.Add(control);
            x += 190;
        }
        form.Show();
        Application.DoEvents();

        var masks = controls.Select(CaptureParentMask).ToArray();

        for (var index = 1; index < masks.Length; index++)
        {
            Assert.That(masks[index], Is.EqualTo(masks[0]), controls[index].GetType().Name);
        }
    }

    [Test]
    public void FocusDoesNotMoveOuterBoundsOrNativeTextBaseline()
    {
        using var form = new Form { BackColor = ParentColor, ClientSize = new Size(500, 180), ShowInTaskbar = false };
        using var input = new BootstrapNumericBox
        {
            Location = new Point(20, 20),
            Size = new Size(220, 40),
            BorderRadius = 12
        };
        form.Controls.Add(input);
        form.Show();
        Application.DoEvents();
        var bounds = input.Bounds;
        var editorBounds = input.Controls.Cast<Control>().First().Bounds;
        var unfocusedMask = CaptureParentMask(input);

        Assert.That(input.Controls.Cast<Control>().First().Focus(), Is.True);
        Application.DoEvents();
        var focusedMask = CaptureParentMask(input);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.Bounds, Is.EqualTo(bounds));
            Assert.That(input.Controls.Cast<Control>().First().Bounds, Is.EqualTo(editorBounds));
            Assert.That(focusedMask, Is.EqualTo(unfocusedMask));
        }));
    }

    [Test]
    public void ExplicitSizeAndConnectedCornerOverridesRemainEffectiveAfterResize()
    {
        using var form = new Form { BackColor = ParentColor, ClientSize = new Size(360, 180), ShowInTaskbar = false };
        using var input = new BootstrapNumericBox
        {
            Location = new Point(20, 20),
            Size = new Size(240, 48)
        };
        ((IBootstrapConnectedControl)input).ConnectedCornerRadius = new CornerRadius(0, 12, 12, 0);
        form.Controls.Add(input);
        form.Show();
        Application.DoEvents();

        input.Size = new Size(260, 52);
        form.BackColor = Color.Lime;
        form.BackColor = ParentColor;
        Application.DoEvents();
        using var bitmap = Capture(input);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.Size, Is.EqualTo(new Size(260, 52)));
            Assert.That(bitmap.GetPixel(0, 0).ToArgb(), Is.Not.EqualTo(ParentColor.ToArgb()), "Square connected corner must remain square.");
            Assert.That(bitmap.GetPixel(bitmap.Width - 1, 0).ToArgb(), Is.EqualTo(ParentColor.ToArgb()), "Rounded connected corner must expose the repainted parent.");
        }));
    }

    private static Control[] CreateComparableControls()
    {
        return new Control[]
        {
            new BootstrapTextBox { Size = new Size(180, 40), BorderRadius = 12 },
            new BootstrapNumericBox { Size = new Size(180, 40), BorderRadius = 12 },
            new BootstrapDatePicker { Size = new Size(180, 40), BorderRadius = 12 },
            new BootstrapSelect { Size = new Size(180, 40), BorderRadius = 12 },
            new BootstrapButton { Size = new Size(180, 40), BorderRadius = 12, Text = "Action" }
        };
    }

    private static IReadOnlyList<bool> CaptureParentMask(Control control)
    {
        using var bitmap = Capture(control);
        var mask = new List<bool>();
        for (var y = 0; y < 16; y++)
        {
            for (var x = 0; x < 16; x++)
            {
                mask.Add(bitmap.GetPixel(x, y).ToArgb() == ParentColor.ToArgb());
            }
        }

        return mask;
    }

    private static Bitmap Capture(Control control)
    {
        var bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, control.ClientRectangle);
        return bitmap;
    }
}
