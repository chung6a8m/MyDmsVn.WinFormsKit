using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost;

internal static class RoundedPopupScenario
{
    internal static int Run(string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("An output directory is required.", nameof(outputDirectory));
        }

        Directory.CreateDirectory(outputDirectory);
        Exception? failure = null;
        using var dispatcher = new Form
        {
            ClientSize = new Size(1, 1),
            FormBorderStyle = FormBorderStyle.None,
            Location = new Point(-32000, -32000),
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual
        };
        dispatcher.Shown += (_, _) => dispatcher.BeginInvoke((Action)(() =>
        {
            try
            {
                foreach (var mode in new[] { BootstrapThemeMode.Light, BootstrapThemeMode.Dark })
                {
                    CaptureMode(outputDirectory, mode);
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                dispatcher.Close();
            }
        }));
        Application.Run(dispatcher);
        if (failure is not null)
        {
            throw new InvalidOperationException("Rounded popup scenario failed.", failure);
        }

        return 0;
    }

    private static void CaptureMode(string outputDirectory, BootstrapThemeMode mode)
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
        using var form = new PatternForm
        {
            ClientSize = new Size(760, 520),
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = ResolveLocation(new Size(760, 520)),
            Text = "Rounded popup native capture"
        };
        using var select = new BootstrapSelect
        {
            Location = new Point(32, 32),
            Width = 310,
            SearchEnabled = true,
            BorderRadius = 12
        };
        for (var index = 1; index <= 12; index++)
        {
            select.Items.Add(new BootstrapSelectItem(index, "Select item " + index));
        }

        using var lookup = new BootstrapLookupBox
        {
            Location = new Point(390, 32),
            Width = 310,
            BorderRadius = 12,
            DisplayMember = nameof(PopupRow.Name),
            ValueMember = nameof(PopupRow.Id),
            ShowRefreshButton = true,
            ShowAddNewButton = true,
            DataSource = new BindingList<PopupRow>
            {
                new PopupRow(1, "Lookup one"),
                new PopupRow(2, "Lookup two"),
                new PopupRow(3, "Lookup three"),
                new PopupRow(4, "Lookup four")
            }
        };
        lookup.ResultsGrid.BackgroundColor = Color.Fuchsia;
        form.Controls.Add(select);
        form.Controls.Add(lookup);
        form.Show();
        PumpMessages();

        select.Focus();
        InvokeInternal(select, "OpenDropDownInternal");
        PumpMessages();
        CapturePopup(outputDirectory, mode, "select", select, GetInternalRectangle(select, "DropDownBoundsForTest"));
        InvokeInternal(select, "CloseDropDownInternal", true);
        PumpMessages();

        lookup.Focus();
        lookup.OpenDropDown();
        PumpMessages();
        CapturePopup(outputDirectory, mode, "lookup", lookup, GetLookupBounds(lookup));
        lookup.CloseDropDown();
        PumpMessages();
    }

    private static void CapturePopup(
        string outputDirectory,
        BootstrapThemeMode mode,
        string controlName,
        Control owner,
        Rectangle popupBounds)
    {
        if (popupBounds.Width <= 0 || popupBounds.Height <= 0)
        {
            throw new InvalidOperationException(controlName + " popup did not expose visible native bounds.");
        }

        var captureBounds = Rectangle.Inflate(popupBounds, 12, 12);
        using var bitmap = new Bitmap(captureBounds.Width, captureBounds.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(captureBounds.Location, Point.Empty, captureBounds.Size, CopyPixelOperation.SourceCopy);
        }

        RequireCaptureContent(bitmap, controlName);
        var prefix = mode.ToString().ToLowerInvariant() + "-" + controlName;
        bitmap.Save(Path.Combine(outputDirectory, prefix + ".png"), ImageFormat.Png);
        File.WriteAllLines(Path.Combine(outputDirectory, prefix + ".txt"), new[]
        {
            "mode=" + mode,
            "control=" + controlName,
            "popup.visible=true",
            "popup.bounds=" + Format(popupBounds),
            "capture.bounds=" + Format(captureBounds),
            "owner.dpi=" + owner.DeviceDpi,
            "screen=" + Screen.FromControl(owner).DeviceName,
            "fuchsia.pixels=" + CountNear(bitmap, Color.Fuchsia, 8)
        });
    }

    private static void RequireCaptureContent(Bitmap bitmap, string controlName)
    {
        var distinct = new HashSet<int>();
        for (var y = 0; y < bitmap.Height; y += Math.Max(1, bitmap.Height / 24))
        {
            for (var x = 0; x < bitmap.Width; x += Math.Max(1, bitmap.Width / 24))
            {
                distinct.Add(bitmap.GetPixel(x, y).ToArgb());
            }
        }

        if (distinct.Count < 6)
        {
            throw new InvalidOperationException(controlName + " capture is blank or occluded.");
        }
    }

    private static Rectangle GetLookupBounds(BootstrapLookupBox lookup)
    {
        var field = typeof(BootstrapLookupBox).GetField("_dropDownController", BindingFlags.Instance | BindingFlags.NonPublic);
        var controller = field?.GetValue(lookup) ?? throw new InvalidOperationException("Lookup controller was not found.");
        var dropDownField = controller.GetType().GetField("_dropDown", BindingFlags.Instance | BindingFlags.NonPublic);
        return dropDownField?.GetValue(controller) is ToolStripDropDown dropDown && dropDown.Visible
            ? dropDown.Bounds
            : throw new InvalidOperationException("Lookup popup bounds were not found.");
    }

    private static Rectangle GetInternalRectangle(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        return property?.GetValue(instance) is Rectangle bounds
            ? bounds
            : throw new InvalidOperationException(propertyName + " was not found.");
    }

    private static void InvokeInternal(object instance, string methodName, params object[] arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method is null)
        {
            throw new InvalidOperationException(methodName + " was not found.");
        }

        method.Invoke(instance, arguments);
    }

    private static Point ResolveLocation(Size size)
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        return new Point(
            area.Left + Math.Max(0, (area.Width - size.Width) / 2),
            area.Top + Math.Max(0, (area.Height - size.Height) / 2));
    }

    private static void PumpMessages()
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < 80)
        {
            Application.DoEvents();
            Thread.Sleep(2);
        }

        Application.DoEvents();
    }

    private static string Format(Rectangle rectangle) =>
        rectangle.X + "," + rectangle.Y + "," + rectangle.Width + "," + rectangle.Height;

    private static int CountNear(Bitmap bitmap, Color expected, int tolerance)
    {
        var count = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var actual = bitmap.GetPixel(x, y);
                if (Math.Abs(actual.R - expected.R) <= tolerance &&
                    Math.Abs(actual.G - expected.G) <= tolerance &&
                    Math.Abs(actual.B - expected.B) <= tolerance)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private sealed class PopupRow
    {
        internal PopupRow(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }
    }

    private sealed class PatternForm : Form
    {
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            const int tile = 20;
            for (var y = 0; y < ClientSize.Height; y += tile)
            {
                for (var x = 0; x < ClientSize.Width; x += tile)
                {
                    using var brush = new SolidBrush(((x / tile) + (y / tile)) % 2 == 0
                        ? Color.FromArgb(248, 248, 248)
                        : Color.FromArgb(42, 46, 54));
                    e.Graphics.FillRectangle(brush, x, y, tile, tile);
                }
            }
        }
    }
}
