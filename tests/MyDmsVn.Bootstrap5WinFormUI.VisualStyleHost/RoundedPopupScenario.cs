using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost;

internal static class RoundedPopupScenario
{
    private const int CaptureMargin = 12;

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
            TopMost = true,
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
        var selectDropDown = GetDropDown(select);
        var selectContent = GetInternalControl(select, "DropDownContentForTest");
        CapturePopup(
            outputDirectory,
            mode,
            "select",
            select,
            selectDropDown,
            ScreenBounds(selectContent),
            Rectangle.Empty,
            expectFuchsiaContent: false);
        InvokeInternal(select, "CloseDropDownInternal", true);
        PumpMessages();

        lookup.Focus();
        lookup.OpenDropDown();
        PumpMessages();
        var lookupDropDown = GetDropDown(lookup);
        var footer = lookup.ResultsGrid.Parent!.Controls.Cast<Control>()
            .Single(control => !ReferenceEquals(control, lookup.ResultsGrid));
        CapturePopup(
            outputDirectory,
            mode,
            "lookup",
            lookup,
            lookupDropDown,
            ScreenBounds(lookup.ResultsGrid),
            ScreenBounds(footer),
            expectFuchsiaContent: true);
        lookup.CloseDropDown();
        PumpMessages();
    }

    private static void CapturePopup(
        string outputDirectory,
        BootstrapThemeMode mode,
        string controlName,
        Control owner,
        ToolStripDropDown popup,
        Rectangle contentBounds,
        Rectangle footerBounds,
        bool expectFuchsiaContent)
    {
        var popupBounds = popup.Bounds;
        if (popupBounds.Width <= 0 || popupBounds.Height <= 0)
        {
            throw new InvalidOperationException(controlName + " popup did not expose visible native bounds.");
        }

        // Keep only this diagnostic HWND above unrelated desktop apps during capture.
        Require(SetWindowPos(popup.Handle, new IntPtr(-1), 0, 0, 0, 0, 0x0013),
            controlName + ": cannot establish an unobscured capture window.");
        PumpMessages();

        var captureBounds = Rectangle.Inflate(popupBounds, CaptureMargin, CaptureMargin);
        using var bitmap = new Bitmap(captureBounds.Width, captureBounds.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(captureBounds.Location, Point.Empty, captureBounds.Size, CopyPixelOperation.SourceCopy);
        }

        RequireCaptureContent(bitmap, controlName);
        var prefix = mode.ToString().ToLowerInvariant() + "-" + controlName;
        bitmap.Save(Path.Combine(outputDirectory, prefix + ".png"), ImageFormat.Png);
        var popupInCapture = new Rectangle(CaptureMargin, CaptureMargin, popupBounds.Width, popupBounds.Height);
        var contentInCapture = OffsetToCapture(contentBounds, captureBounds);
        var theme = BootstrapThemeManager.CurrentTheme;
        var logicalRadius = GetBorderRadiusForCapture(owner);
        var radius = Math.Max(0f, logicalRadius < 0
            ? theme.Metrics.Radius * owner.DeviceDpi / 96f
            : logicalRadius * owner.DeviceDpi / 96f);
        RequireEdgeContinuity(bitmap, popupInCapture, theme.Colors.Border, radius, controlName);
        RequireCornerSymmetry(bitmap, popupInCapture, theme.Colors.Border, theme.Colors.Surface, radius, controlName);
        RequireCleanShellBand(bitmap, popupInCapture, contentInCapture, radius, expectFuchsiaContent, controlName);
        File.WriteAllLines(Path.Combine(outputDirectory, prefix + ".txt"), new[]
        {
            "mode=" + mode,
            "control=" + controlName,
            "popup.visible=true",
            "popup.bounds=" + Format(popupBounds),
            "capture.bounds=" + Format(captureBounds),
            "owner.dpi=" + owner.DeviceDpi,
            "owner.windowDpi=" + GetDpiForWindow(owner.Handle),
            "popup.windowDpi=" + GetDpiForWindow(popup.Handle),
            "process.awareness=" + GetProcessAwareness(),
            "thread.awareness=" + FormatAwarenessContext(GetThreadDpiAwarenessContext()),
            "popup.awareness=" + FormatAwarenessContext(GetWindowDpiAwarenessContext(popup.Handle)),
            "awareness.source=" + AwarenessSource,
            "popup.region.bounds=" + Format(GetRegionBounds(popup)),
            "content.bounds=" + Format(contentBounds),
            "footer.bounds=" + (footerBounds.IsEmpty ? "none" : Format(footerBounds)),
            "screen=" + Screen.FromControl(owner).DeviceName,
            "fuchsia.pixels=" + CountNear(bitmap, Color.Fuchsia, 8),
            "oracle.edge-continuity=pass",
            "oracle.corner-symmetry=pass",
            "oracle.shell-band=pass",
            "oracle.content-containment=pass"
        });
    }

    private static void RequireEdgeContinuity(
        Bitmap bitmap,
        Rectangle popup,
        Color border,
        float radius,
        string controlName)
    {
        var inset = Math.Max(2, (int)Math.Ceiling(radius) + 1);
        var horizontalLength = Math.Max(0, popup.Width - (inset * 2));
        var verticalLength = Math.Max(0, popup.Height - (inset * 2));
        Require(horizontalLength > 0 && verticalLength > 0, controlName + ": popup is too small for an edge oracle.");
        RequireBorderRatio(bitmap, popup.Left + inset, popup.Top, horizontalLength, 1, border, controlName + " top");
        RequireBorderRatio(bitmap, popup.Left + inset, popup.Bottom - 1, horizontalLength, 1, border, controlName + " bottom");
        RequireBorderRatio(bitmap, popup.Left, popup.Top + inset, 1, verticalLength, border, controlName + " left");
        RequireBorderRatio(bitmap, popup.Right - 1, popup.Top + inset, 1, verticalLength, border, controlName + " right");
    }

    private static void RequireBorderRatio(
        Bitmap bitmap,
        int x,
        int y,
        int width,
        int height,
        Color border,
        string edge)
    {
        var total = width * height;
        var matching = 0;
        for (var sampleY = y; sampleY < y + height; sampleY++)
        for (var sampleX = x; sampleX < x + width; sampleX++)
            if (IsNear(bitmap.GetPixel(sampleX, sampleY), border, 24)) matching++;
        Require(matching >= Math.Ceiling(total * 0.9d), edge + ": border continuity dropped below 90%." );
    }

    private static void RequireCornerSymmetry(
        Bitmap bitmap,
        Rectangle popup,
        Color border,
        Color surface,
        float radius,
        string controlName)
    {
        var size = Math.Min(Math.Min(popup.Width / 2, popup.Height / 2), Math.Max(5, (int)Math.Ceiling(radius) + 3));
        var topLeftHorizontal = FindBorderIntercept(bitmap, popup, size, Corner.TopLeft, horizontal: true, border, surface);
        var topRightHorizontal = FindBorderIntercept(bitmap, popup, size, Corner.TopRight, horizontal: true, border, surface);
        var bottomLeftHorizontal = FindBorderIntercept(bitmap, popup, size, Corner.BottomLeft, horizontal: true, border, surface);
        var bottomRightHorizontal = FindBorderIntercept(bitmap, popup, size, Corner.BottomRight, horizontal: true, border, surface);
        var topLeftVertical = FindBorderIntercept(bitmap, popup, size, Corner.TopLeft, horizontal: false, border, surface);
        var bottomLeftVertical = FindBorderIntercept(bitmap, popup, size, Corner.BottomLeft, horizontal: false, border, surface);
        var topRightVertical = FindBorderIntercept(bitmap, popup, size, Corner.TopRight, horizontal: false, border, surface);
        var bottomRightVertical = FindBorderIntercept(bitmap, popup, size, Corner.BottomRight, horizontal: false, border, surface);
        Require(Math.Abs(topLeftHorizontal - topRightHorizontal) <= 1, controlName + ": top corner intercepts differ by more than one pixel (" + topLeftHorizontal + ", " + topRightHorizontal + ").");
        Require(Math.Abs(bottomLeftHorizontal - bottomRightHorizontal) <= 1, controlName + ": bottom corner intercepts differ by more than one pixel (" + bottomLeftHorizontal + ", " + bottomRightHorizontal + ").");
        Require(Math.Abs(topLeftVertical - bottomLeftVertical) <= 1, controlName + ": left corner intercepts differ by more than one pixel (" + topLeftVertical + ", " + bottomLeftVertical + ").");
        Require(Math.Abs(topRightVertical - bottomRightVertical) <= 1, controlName + ": right corner intercepts differ by more than one pixel (" + topRightVertical + ", " + bottomRightVertical + ").");
    }

    private static int FindBorderIntercept(
        Bitmap bitmap,
        Rectangle popup,
        int size,
        Corner corner,
        bool horizontal,
        Color border,
        Color surface)
    {
        const int requiredRun = 3;
        for (var offset = 0; offset <= size - requiredRun; offset++)
        {
            var runMatches = true;
            for (var step = 0; step < requiredRun; step++)
            {
                var coordinate = offset + step;
                var x = horizontal ? coordinate : 0;
                var y = horizontal ? 0 : coordinate;
                var sampleX = corner == Corner.TopLeft || corner == Corner.BottomLeft
                    ? popup.Left + x
                    : popup.Right - 1 - x;
                var sampleY = corner == Corner.TopLeft || corner == Corner.TopRight
                    ? popup.Top + y
                    : popup.Bottom - 1 - y;
                if (!IsBorderLike(bitmap.GetPixel(sampleX, sampleY), border, surface))
                {
                    runMatches = false;
                    break;
                }
            }

            if (runMatches) return offset;
        }

        throw new InvalidOperationException("Rounded border intercept was not found at " + corner + ".");
    }

    private static void RequireCleanShellBand(
        Bitmap bitmap,
        Rectangle popup,
        Rectangle content,
        float radius,
        bool expectFuchsiaContent,
        string controlName)
    {
        var fuchsia = 0;
        var fuchsiaOutsideContent = 0;
        var blackBandPixels = 0;
        var backdropInsideShell = 0;
        const int band = 4;
        for (var y = popup.Top; y < popup.Bottom; y++)
        for (var x = popup.Left; x < popup.Right; x++)
        {
            var color = bitmap.GetPixel(x, y);
            var isFuchsia = IsNear(color, Color.Fuchsia, 8);
            if (isFuchsia)
            {
                fuchsia++;
                if (!content.Contains(x, y)) fuchsiaOutsideContent++;
            }

            var inShellBand = x < popup.Left + band || x >= popup.Right - band ||
                y < popup.Top + band || y >= popup.Bottom - band;
            if (inShellBand && color.R < 16 && color.G < 16 && color.B < 16) blackBandPixels++;
            if (inShellBand && isFuchsia) fuchsiaOutsideContent++;
            var localX = x - popup.Left + 0.5f;
            var localY = y - popup.Top + 0.5f;
            var centerX = Math.Max(radius, Math.Min(popup.Width - radius, localX));
            var centerY = Math.Max(radius, Math.Min(popup.Height - radius, localY));
            var dx = localX - centerX;
            var dy = localY - centerY;
            var safelyInside = (dx * dx) + (dy * dy) < Math.Max(0f, radius - 1f) * Math.Max(0f, radius - 1f);
            if (inShellBand && safelyInside &&
                (color.ToArgb() == Color.FromArgb(248, 248, 248).ToArgb() ||
                 color.ToArgb() == Color.FromArgb(42, 46, 54).ToArgb())) backdropInsideShell++;
        }

        Require(blackBandPixels == 0, controlName + ": black fringe detected in the shell band.");
        Require(backdropInsideShell == 0, controlName + ": backdrop leaked through the shell band.");
        Require(fuchsiaOutsideContent == 0, controlName + ": opaque child color escaped the safe content rectangle.");
        Require(expectFuchsiaContent ? fuchsia > 100 : fuchsia == 0, controlName + ": unexpected fuchsia content observation.");
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

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static ToolStripDropDown GetDropDown(Control owner)
    {
        var field = owner.GetType().GetField("_dropDownController", BindingFlags.Instance | BindingFlags.NonPublic);
        var controller = field?.GetValue(owner) ?? throw new InvalidOperationException(owner.GetType().Name + " controller was not found.");
        var dropDownField = controller.GetType().GetField("_dropDown", BindingFlags.Instance | BindingFlags.NonPublic);
        return dropDownField?.GetValue(controller) is ToolStripDropDown dropDown && dropDown.Visible
            ? dropDown
            : throw new InvalidOperationException(owner.GetType().Name + " popup was not found.");
    }

    private static Control GetInternalControl(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        return property?.GetValue(instance) is Control control
            ? control
            : throw new InvalidOperationException(propertyName + " was not found.");
    }

    private static int GetBorderRadiusForCapture(Control owner)
    {
        return owner switch
        {
            BootstrapSelect select => select.BorderRadius,
            BootstrapLookupBox lookup => lookup.BorderRadius,
            _ => throw new InvalidOperationException(owner.GetType().Name + " does not expose a capture radius.")
        };
    }

    private static Rectangle ScreenBounds(Control control)
    {
        return control.RectangleToScreen(control.ClientRectangle);
    }

    private static Rectangle OffsetToCapture(Rectangle screenBounds, Rectangle captureBounds)
    {
        if (screenBounds.IsEmpty)
        {
            return Rectangle.Empty;
        }

        screenBounds.Offset(-captureBounds.X, -captureBounds.Y);
        return screenBounds;
    }

    private static Rectangle GetRegionBounds(Control popup)
    {
        if (popup.Region is null)
        {
            return popup.ClientRectangle;
        }

        using var bitmap = new Bitmap(1, 1);
        using var graphics = Graphics.FromImage(bitmap);
        return Rectangle.Ceiling(popup.Region.GetBounds(graphics));
    }

    private static string GetProcessAwareness()
    {
        using var process = Process.GetCurrentProcess();
        var result = GetProcessDpiAwareness(process.Handle, out var awareness);
        return result == 0 ? FormatAwareness(awareness) : "HRESULT:" + result;
    }

    private static string FormatAwarenessContext(IntPtr context)
    {
        if (AreDpiAwarenessContextsEqual(context, new IntPtr(-4))) return "PerMonitorV2";
        if (AreDpiAwarenessContextsEqual(context, new IntPtr(-3))) return "PerMonitor";
        if (AreDpiAwarenessContextsEqual(context, new IntPtr(-2))) return "System";
        if (AreDpiAwarenessContextsEqual(context, new IntPtr(-1))) return "Unaware";
        if (AreDpiAwarenessContextsEqual(context, new IntPtr(-5))) return "UnawareGdiScaled";
        return "Unknown:" + context;
    }

    private static string FormatAwareness(int awareness)
    {
        return awareness switch
        {
            0 => "Unaware",
            1 => "System",
            2 => "PerMonitor",
            _ => "Unknown:" + awareness
        };
    }

    private static bool IsNear(Color actual, Color expected, int tolerance)
    {
        return Math.Abs(actual.R - expected.R) <= tolerance &&
            Math.Abs(actual.G - expected.G) <= tolerance &&
            Math.Abs(actual.B - expected.B) <= tolerance;
    }

    private static bool IsBorderLike(Color actual, Color border, Color surface)
    {
        if (!IsNear(actual, border, 8))
        {
            return false;
        }

        var borderDistance = ColorDistanceSquared(actual, border);
        return borderDistance <= ColorDistanceSquared(actual, surface) &&
            borderDistance <= ColorDistanceSquared(actual, Color.FromArgb(248, 248, 248)) &&
            borderDistance <= ColorDistanceSquared(actual, Color.FromArgb(42, 46, 54));
    }

    private static int ColorDistanceSquared(Color left, Color right)
    {
        var red = left.R - right.R;
        var green = left.G - right.G;
        var blue = left.B - right.B;
        return (red * red) + (green * green) + (blue * blue);
    }

    private static string AwarenessSource
    {
        get
        {
#if NET8_0_OR_GREATER
            return "Application.SetHighDpiMode(PerMonitorV2)";
#else
            return "App.config:System.Windows.Forms.DpiAwareness=PerMonitorV2";
#endif
        }
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

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern IntPtr GetThreadDpiAwarenessContext();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDpiAwarenessContext(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AreDpiAwarenessContextsEqual(IntPtr first, IntPtr second);

    [DllImport("shcore.dll")]
    private static extern int GetProcessDpiAwareness(IntPtr process, out int awareness);

    private enum Corner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
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
