using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost;

// This is a diagnostic host, deliberately separate from the production painter.
internal static class TabsHoverScenario
{
    private const int WmPaint = 0x000F;
    private const int WmPrint = 0x0317;
    private const int WmPrintClient = 0x0318;
    private const int WmEraseBkgnd = 0x0014;
    private const int WmMouseMove = 0x0200;
    private const int PrfClient = 0x0004;
    private const int PrfChildren = 0x0010;
    private const int PrfEraseBkgnd = 0x0008;
    private static readonly Stopwatch Clock = new Stopwatch();
    private static readonly List<string> Trace = new List<string>();

    internal static int Run(string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory)) throw new ArgumentException("An output directory is required.", nameof(outputDirectory));
        Directory.CreateDirectory(outputDirectory);
        Clock.Restart();
        var previousCursor = Cursor.Position;
        var result = "PROBE BLOCKED";
        var suppliedPaint = false;
        var printClient = false;
        var drawOutsidePaint = 0;
        var invariant = false;
        var dirtyClip = false;
        var selectedPixels = false;
        var overflowClickable = false;
        var realHoverObserved = false;
        var compositionProof = false;
        var productionProof = false;
        var productionTargetHits = 0;
        try
        {
            using var images = new ImageList { ImageSize = new Size(16, 16) };
            using var image = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(image))
            {
                graphics.Clear(Color.RoyalBlue);
                images.Images.Add(image);
            }
            using var form = new Form
            {
                Text = "Tabs native paint proof",
                ClientSize = new Size(900, 670),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(80, 80),
                ShowInTaskbar = false
            };
            using var native = new ProbeTabControl { Bounds = new Rectangle(25, 25, 390, 170), HotTrack = true };
            using var hot = new BootstrapTabControl { Bounds = new Rectangle(460, 25, 390, 170), HotTrack = true };
            using var cold = new BootstrapTabControl { Bounds = new Rectangle(25, 240, 390, 170), HotTrack = false };
            using var paintProbe = new ProbeTabControl { Bounds = new Rectangle(460, 240, 390, 170), HotTrack = true, DrawMode = TabDrawMode.OwnerDrawFixed };
            using var production = new ProductionProbeTabs { Bounds = new Rectangle(25, 455, 825, 170), HotTrack = true };
            foreach (TabControl tabs in new TabControl[] { native, hot, cold, paintProbe, production })
            {
                tabs.Font = form.Font;
                tabs.ImageList = images;
                tabs.ShowToolTips = true;
                Populate(tabs);
                form.Controls.Add(tabs);
            }
            hot.SetTabClosable(hot.TabPages[1], true);
            cold.SetTabClosable(cold.TabPages[1], true);
            production.SetTabClosable(production.TabPages[1], true);
            form.Show();
            Pump();
            using var nativeHoverProbe = new HoverWindowProbe(native);
            using var hotHoverProbe = new HoverWindowProbe(hot);
            using var coldHoverProbe = new HoverWindowProbe(cold);
            using var paintHoverProbe = new HoverWindowProbe(paintProbe);
            using var productionHoverProbe = new HoverWindowProbe(production);
            Write("environment", Environment.OSVersion + "; " + IntPtr.Size * 8 + " bit; runtime " + Environment.Version + "; DeviceDpi=" + form.DeviceDpi + "; nativeDpi=" + GetDpiForWindow(form.Handle));
            Write("window", "formVisible=" + IsWindowVisible(form.Handle) + "; foreground=" + GetForegroundWindow() + "; form=" + form.Handle);
            Write("overflow-child", "probe=" + FindWindowEx(paintProbe.Handle, IntPtr.Zero, "msctls_updown32", null));
            Write("overflow-child", "production=" + FindWindowEx(production.Handle, IntPtr.Zero, "msctls_updown32", null));
            foreach (TabControl tabs in new TabControl[] { native, hot, cold, paintProbe, production })
                Snapshot(tabs, "before");
            var hotBefore = Signature(hot);
            var coldBefore = Signature(cold);
            var productionBefore = Signature(production);
            Capture(form, Path.Combine(outputDirectory, "before.png"));

            // Cursor.Position sends actual desktop pointer movement, not an artificial MouseMove event.
            foreach (TabControl tabs in new TabControl[] { native, hot, cold, paintProbe, production })
            {
                // The third fixed-width Bootstrap header is covered by the native overflow child.
                var hoverSequence = tabs is BootstrapTabControl || tabs is BootstrapTabs ? new[] { 0, 1, 0 } : new[] { 0, 1, 2 };
                foreach (var index in hoverSequence)
                {
                    var rect = tabs.GetTabRect(index);
                    Cursor.Position = tabs.PointToScreen(new Point(rect.Left + Math.Min(10, rect.Width / 2), rect.Top + rect.Height / 2));
                    Thread.Sleep(25);
                    Pump();
                    var actualWindow = WindowFromPoint(Cursor.Position);
                    if (tabs == production && actualWindow == production.Handle) productionTargetHits++;
                    Write("hover-target", tabs.GetType().Name + "; intendedIndex=" + index + "; handle=" + tabs.Handle + "; actualWindow=" + actualWindow + "; rect=" + rect);
                }
                Snapshot(tabs, "after");
            }
            Capture(form, Path.Combine(outputDirectory, "after.png"));
            realHoverObserved = nativeHoverProbe.MoveCount >= 3 && hotHoverProbe.MoveCount >= 3 &&
                                coldHoverProbe.MoveCount >= 3 && paintHoverProbe.MoveCount >= 3 &&
                                hotHoverProbe.ManagedMatches >= 3 && coldHoverProbe.ManagedMatches >= 3;
            Write("real-hover-summary", "native=" + nativeHoverProbe.MoveCount + "; hot=" + hotHoverProbe.MoveCount +
                "; cold=" + coldHoverProbe.MoveCount + "; paintProbe=" + paintHoverProbe.MoveCount +
                "; hotManagedMatches=" + hotHoverProbe.ManagedMatches + "; coldManagedMatches=" + coldHoverProbe.ManagedMatches +
                "; nativeDirty=" + nativeHoverProbe.DirtyCount + "; hotDirty=" + hotHoverProbe.DirtyCount +
                "; coldDirty=" + coldHoverProbe.DirtyCount + "; pass=" + realHoverObserved);
            invariant = hotBefore == Signature(hot) && coldBefore == Signature(cold);
            Write("hover-invariant", invariant ? "PASS" : "FAIL");
            productionProof = RunProductionProof(production, productionHoverProbe, productionTargetHits, productionBefore, outputDirectory);

            using (var surface = new Bitmap(paintProbe.Width + 40, paintProbe.Height + 40, PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(surface))
            {
                graphics.Clear(Color.Fuchsia);
                var hdc = graphics.GetHdc();
                try
                {
                    paintProbe.StartSuppliedPaint();
                    // Saved DC origin and clip exercise a target whose origin is not the screen.
                    var saved = SaveDC(hdc);
                    SetViewportOrgEx(hdc, 17, 11, out _);
                    IntersectClipRect(hdc, 17, 11, paintProbe.Width + 17, paintProbe.Height + 11);
                    SendMessage(paintProbe.Handle, WmPaint, hdc, IntPtr.Zero);
                    paintProbe.EndSuppliedPaint();
                    suppliedPaint = paintProbe.SuppliedDrawCount > 0;
                    Write("supplied-paint", "draw=" + paintProbe.SuppliedDrawCount + "; outside=" + paintProbe.OutsidePaintDrawCount + "; depth=" + paintProbe.MaxPaintDepth);
                    SendMessage(paintProbe.Handle, WmPrintClient, hdc, (IntPtr)0x00000004);
                    printClient = paintProbe.PrintDrawCount > 0;
                    RestoreDC(hdc, saved);
                }
                finally { graphics.ReleaseHdc(hdc); }
                var selected = paintProbe.SelectedDrawBounds;
                selected.Offset(17, 11);
                selectedPixels = CountNotColor(surface, Color.Fuchsia, selected) > 12;
                Write("selected-pixels", "bounds=" + selected + "; nativePixels=" + CountNotColor(surface, Color.Fuchsia, selected) + "; painted=" + selectedPixels);
                using (var overlayGraphics = Graphics.FromImage(surface))
                using (var brush = new SolidBrush(Color.Lime))
                    overlayGraphics.FillRectangle(brush, 18, 45, 8, 3);
                Write("completion-chrome", "pixel=" + surface.GetPixel(20, 46) + "; after native pass in same buffer");
                surface.Save(Path.Combine(outputDirectory, "supplied-paint.png"));
            }
            using (var dirtySurface = new Bitmap(paintProbe.Width + 40, paintProbe.Height + 40, PixelFormat.Format32bppArgb))
            using (var dirtyGraphics = Graphics.FromImage(dirtySurface))
            {
                dirtyGraphics.Clear(Color.Fuchsia);
                var hdc = dirtyGraphics.GetHdc();
                try
                {
                    var saved = SaveDC(hdc);
                    SetViewportOrgEx(hdc, 17, 11, out _);
                    var header = paintProbe.GetTabRect(0);
                    IntersectClipRect(hdc, header.Left, header.Top, header.Right, header.Bottom);
                    SendMessage(paintProbe.Handle, WmPaint, hdc, IntPtr.Zero);
                    RestoreDC(hdc, saved);
                }
                finally { dirtyGraphics.ReleaseHdc(hdc); }
                dirtyClip = CountNotColor(dirtySurface, Color.Fuchsia, new Rectangle(17, 11, paintProbe.GetTabRect(0).Width, paintProbe.GetTabRect(0).Height)) > 12 &&
                            CountNotColor(dirtySurface, Color.Fuchsia, new Rectangle(120, 80, 40, 40)) == 0;
                Write("dirty-clip", "headerPainted=" + (CountNotColor(dirtySurface, Color.Fuchsia, new Rectangle(17, 11, paintProbe.GetTabRect(0).Width, paintProbe.GetTabRect(0).Height)) > 12) + "; pageUntouched=" + (CountNotColor(dirtySurface, Color.Fuchsia, new Rectangle(120, 80, 40, 40)) == 0) + "; pass=" + dirtyClip);
                dirtySurface.Save(Path.Combine(outputDirectory, "dirty-header.png"));
            }
            compositionProof = RunCompositionProof(paintProbe, outputDirectory);
            var upDown = FindWindowEx(paintProbe.Handle, IntPtr.Zero, "msctls_updown32", null);
            if (upDown != IntPtr.Zero && GetWindowRect(upDown, out var upDownBounds))
            {
                var beforeScroll = paintProbe.GetTabRect(0);
                Cursor.Position = new Point(upDownBounds.Right - 9, (upDownBounds.Top + upDownBounds.Bottom) / 2);
                Pump();
                MouseEvent(0x0002, 0, 0, 0, UIntPtr.Zero);
                MouseEvent(0x0004, 0, 0, 0, UIntPtr.Zero);
                Pump();
                var afterScroll = paintProbe.GetTabRect(0);
                overflowClickable = beforeScroll != afterScroll;
                Write("overflow-click", "child=" + upDown + "; bounds=" + upDownBounds + "; before=" + beforeScroll + "; after=" + afterScroll + "; moved=" + overflowClickable);
            }
            drawOutsidePaint = paintProbe.OutsidePaintDrawCount;
            Capture(form, Path.Combine(outputDirectory, "final.png"));
            result = suppliedPaint && printClient && drawOutsidePaint == 0 && invariant && dirtyClip && selectedPixels && overflowClickable && realHoverObserved && compositionProof && productionProof ? "PROBE PASS" : "PROBE BLOCKED";
            Write("gate", "suppliedPaint=" + suppliedPaint + "; printClientDispatched=" + printClient + "; outsidePaintDraw=" + drawOutsidePaint + "; hoverInvariant=" + invariant + "; dirtyClip=" + dirtyClip + "; selectedPixels=" + selectedPixels + "; overflowClickable=" + overflowClickable + "; realHoverObserved=" + realHoverObserved + "; compositionProof=" + compositionProof + "; productionProof=" + productionProof);
        }
        finally
        {
            Cursor.Position = previousCursor;
            File.WriteAllLines(Path.Combine(outputDirectory, "trace.txt"), Trace, new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(outputDirectory, "metadata.json"),
                "{\"candidate\":\"" + Escape(RunGitHead()) + "\",\"os\":\"" + Escape(Environment.OSVersion.ToString()) +
                "\",\"bitness\":" + (IntPtr.Size * 8) + ",\"runtime\":\"" + Escape(Environment.Version.ToString()) +
                "\",\"probeResult\":\"" + result + "\",\"fullGate\":\"OPEN\",\"flicker\":\"NOT REPRODUCED\",\"elapsedMs\":" + Clock.ElapsedMilliseconds + "}", new UTF8Encoding(false));
        }
        return result == "PROBE PASS" ? 0 : 2;
    }

    private static string RunGitHead()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("git", "rev-parse HEAD")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            });
            if (process == null || !process.WaitForExit(2000) || process.ExitCode != 0) return "unknown";
            return process.StandardOutput.ReadToEnd().Trim();
        }
        catch { return "unknown"; }
    }
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    private static void Write(string kind, string value) => Trace.Add(Clock.ElapsedMilliseconds + "\t" + kind + "\t" + value);
    private static void Pump()
    {
        if (Clock.Elapsed > TimeSpan.FromSeconds(29)) throw new TimeoutException("Tabs hover scenario exceeded 29 seconds.");
        for (var i = 0; i < 3; i++) Application.DoEvents();
    }
    private static void Populate(TabControl tabs)
    {
        var names = new[] { "Trang đầu", "Đóng được", "Dữ liệu có ảnh", "Một tiêu đề tiếng Việt khá dài", "Overflow 5", "Overflow 6", "Overflow 7", "Overflow 8", "Overflow 9", "Overflow 10" };
        for (var i = 0; i < names.Length; i++)
            tabs.TabPages.Add(new TabPage(names[i]) { ImageIndex = i == 2 ? 0 : -1, ToolTipText = names[i] });
    }
    private static void Snapshot(TabControl tabs, string stage)
    {
        var rects = new StringBuilder();
        for (var i = 0; i < tabs.TabCount; i++) rects.Append(i).Append(':').Append(tabs.GetTabRect(i)).Append(';');
        Write("snapshot", stage + "; " + tabs.GetType().Name + "; handle=" + tabs.Handle + "; selected=" + tabs.SelectedIndex + "; font=" + tabs.Font + "; itemSize=" + tabs.ItemSize + "; rects=" + rects);
    }
    private static string Signature(TabControl tabs)
    {
        var value = tabs.Handle + ";" + tabs.SelectedIndex + ";" + tabs.Font.Name + ";" + tabs.Font.Size + ";" + tabs.ItemSize;
        for (var i = 0; i < tabs.TabCount; i++) value += ";" + tabs.GetTabRect(i);
        return value;
    }
    private static void Capture(Form form, string path)
    {
        using var bitmap = new Bitmap(form.Width, form.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(form.Location, Point.Empty, bitmap.Size);
        bitmap.Save(path, ImageFormat.Png);
    }
    private static int CountNotColor(Bitmap bitmap, Color color, Rectangle area)
    {
        var count = 0;
        var bounds = Rectangle.Intersect(new Rectangle(Point.Empty, bitmap.Size), area);
        for (var y = bounds.Top; y < bounds.Bottom; y++)
        for (var x = bounds.Left; x < bounds.Right; x++)
            if (bitmap.GetPixel(x, y).ToArgb() != color.ToArgb()) count++;
        return count;
    }

    // Prototype only: native owns the first pass, then chrome owns the remaining header gaps.
    // The bitmap is the supplied DC; no screen DC is acquired by this method.
    private static bool RunCompositionProof(ProbeTabControl probe, string outputDirectory)
    {
        var offset = new Point(17, 11);
        var client = probe.ClientRectangle;
        var page = probe.DisplayRectangle;
        var child = FindWindowEx(probe.Handle, IntPtr.Zero, "msctls_updown32", null);
        var childBounds = Rectangle.Empty;
        if (child != IntPtr.Zero && GetWindowRect(child, out var screenChild))
        {
            var topLeft = probe.PointToClient(new Point(screenChild.Left, screenChild.Top));
            var bottomRight = probe.PointToClient(new Point(screenChild.Right, screenChild.Bottom));
            childBounds = Rectangle.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
        }
        using var surface = new Bitmap(client.Width + 40, client.Height + 40, PixelFormat.Format32bppArgb);
        using (var clear = Graphics.FromImage(surface)) clear.Clear(Color.Fuchsia);
        var hdc = Graphics.FromImage(surface);
        try
        {
            var target = hdc.GetHdc();
            try
            {
                var saved = SaveDC(target);
                if (saved == 0) throw new InvalidOperationException("SaveDC failed for composition prototype.");
                try
                {
                    SetViewportOrgEx(target, offset.X, offset.Y, out _);
                    IntersectClipRect(target, client.Left, client.Top, client.Right, client.Bottom);
                    probe.StartSuppliedPaint();
                    try { SendMessage(probe.Handle, WmPaint, target, IntPtr.Zero); }
                    finally { probe.EndSuppliedPaint(); }
                    var actualBounds = probe.SuppliedDrawBounds;
                    var before = new int[surface.Width * surface.Height];
                    var pixelSaved = SaveDC(target);
                    try
                    {
                        SetViewportOrgEx(target, 0, 0, out _);
                        for (var y = 0; y < surface.Height; y++)
                        for (var x = 0; x < surface.Width; x++)
                            before[y * surface.Width + x] = GetPixel(target, x, y);
                    }
                    finally { RestoreDC(target, pixelSaved); }
                    var bandBottom = Math.Max(1, Math.Min(client.Bottom, page.Top));
                    var headerBand = Rectangle.FromLTRB(client.Left, client.Top, client.Right, bandBottom);
                    var chromeSaved = SaveDC(target);
                    if (chromeSaved == 0) throw new InvalidOperationException("SaveDC failed for chrome completion.");
                    try
                    {
                        IntersectClipRect(target, headerBand.Left, headerBand.Top, headerBand.Right, headerBand.Bottom);
                        ExcludeClipRect(target, page.Left, page.Top, page.Right, page.Bottom);
                        foreach (var bounds in actualBounds)
                            ExcludeClipRect(target, bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
                        if (!childBounds.IsEmpty)
                            ExcludeClipRect(target, childBounds.Left, childBounds.Top, childBounds.Right, childBounds.Bottom);
                        var brush = CreateSolidBrush(ColorTranslator.ToWin32(Color.Lime));
                        if (brush == IntPtr.Zero) throw new InvalidOperationException("CreateSolidBrush failed.");
                        var fillBounds = new NativeRect { Left = headerBand.Left, Top = headerBand.Top, Right = headerBand.Right, Bottom = headerBand.Bottom };
                        try { FillRect(target, ref fillBounds, brush); }
                        finally { DeleteObject(brush); }
                    }
                    finally { RestoreDC(target, chromeSaved); }
                    var headerChanged = CountDifferent(before, target, surface.Size, Shift(headerBand, offset));
                    var selected = probe.SelectedDrawBounds;
                    var selectedChanged = CountDifferent(before, target, surface.Size, Shift(selected, offset));
                    var pageChanged = CountDifferent(before, target, surface.Size, Shift(page, offset));
                    var childChanged = CountDifferent(before, target, surface.Size, Shift(childBounds, offset));
                    var outsideChanged = CountDifferent(before, target, surface.Size, new Rectangle(0, 0, offset.X, surface.Height));
                    Write("composition", "actualDrawBounds=" + string.Join("|", actualBounds) + "; selected=" + selected +
                        "; page=" + page + "; child=" + childBounds + "; headerChanged=" + headerChanged +
                        "; selectedChanged=" + selectedChanged + "; pageChanged=" + pageChanged +
                        "; childChanged=" + childChanged + "; outsideChanged=" + outsideChanged);
                    var printProof = RunPrintProof(probe, outputDirectory);
                    return probe.SuppliedDrawCount > 0 && actualBounds.Count > 0 && !selected.IsEmpty &&
                           child != IntPtr.Zero && !childBounds.IsEmpty && headerChanged > 0 &&
                           selectedChanged == 0 && pageChanged == 0 && childChanged == 0 && outsideChanged == 0 && printProof;
                }
                finally { RestoreDC(target, saved); }
            }
            finally { hdc.ReleaseHdc(); }
        }
        finally { hdc.Dispose(); }
    }

    private static bool RunPrintProof(ProbeTabControl probe, string outputDirectory)
    {
        var flags = new[] { 0, PrfClient, PrfClient | PrfChildren | PrfEraseBkgnd };
        var valid = true;
        foreach (var flag in flags)
        {
            using var bitmap = new Bitmap(probe.Width + 40, probe.Height + 40, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Fuchsia);
            var target = graphics.GetHdc();
            try
            {
                var saved = SaveDC(target);
                try
                {
                    SetViewportOrgEx(target, 17, 11, out _);
                    IntersectClipRect(target, 0, 0, probe.Width, probe.Height);
                    probe.ResetPrintCounts();
                    SendMessage(probe.Handle, WmPrint, target, (IntPtr)flag);
                    Write("WM_PRINT-result", "flags=" + flag + "; print=" + probe.PrintMessageCount +
                        "; printClient=" + probe.PrintClientMessageCount + "; draw=" + probe.PrintDrawCount +
                        "; maxDepth=" + probe.MaxPrintDepth);
                    valid &= probe.PrintMessageCount == 1 && probe.MaxPrintDepth <= 2;
                    if (flag == 0) valid &= probe.PrintDrawCount == 0;
                }
                finally { RestoreDC(target, saved); }
            }
            finally { graphics.ReleaseHdc(target); }
            var painted = CountNotColor(bitmap, Color.Fuchsia, new Rectangle(17, 11, probe.Width, probe.Height));
            var outside = CountNotColor(bitmap, Color.Fuchsia, new Rectangle(0, 0, 17, bitmap.Height));
            Write("WM_PRINT-pixels", "flags=" + flag + "; targetPixels=" + painted + "; outsideClipPixels=" + outside);
            valid &= outside == 0 && (flag == 0 ? painted == 0 : painted > 0);
            bitmap.Save(Path.Combine(outputDirectory, "print-" + flag + ".png"));
        }
        return valid;
    }

    private static Rectangle Shift(Rectangle area, Point offset)
    {
        if (area.IsEmpty) return Rectangle.Empty;
        area.Offset(offset);
        return area;
    }
    private static int CountDifferent(int[] before, IntPtr target, Size size, Rectangle area)
    {
        var changed = 0;
        var bounds = Rectangle.Intersect(new Rectangle(Point.Empty, size), area);
        var saved = SaveDC(target);
        try
        {
            SetViewportOrgEx(target, 0, 0, out _);
            for (var y = bounds.Top; y < bounds.Bottom; y++)
            for (var x = bounds.Left; x < bounds.Right; x++)
                if (before[y * size.Width + x] != GetPixel(target, x, y)) changed++;
        }
        finally { RestoreDC(target, saved); }
        return changed;
    }

    private static bool RunProductionProof(ProductionProbeTabs probe, HoverWindowProbe hover, int targetHits, string before, string outputDirectory)
    {
        probe.Invalidate();
        probe.Update();
        Pump();
        var screenPaint = probe.ScreenPaintCount;
        var screenDraw = probe.ScreenDrawCount;
        var child = FindWindowEx(probe.Handle, IntPtr.Zero, "msctls_updown32", null);
        var printPixels = 0;
        var printOutside = 0;
        using (var bitmap = new Bitmap(probe.Width + 40, probe.Height + 40, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Fuchsia);
            var target = graphics.GetHdc();
            try
            {
                var saved = SaveDC(target);
                try
                {
                    SetViewportOrgEx(target, 17, 11, out _);
                    IntersectClipRect(target, 0, 0, probe.Width, probe.Height);
                    SendMessage(probe.Handle, WmPrintClient, target, (IntPtr)PrfClient);
                }
                finally { RestoreDC(target, saved); }
            }
            finally { graphics.ReleaseHdc(target); }
            printPixels = CountNotColor(bitmap, Color.Fuchsia, new Rectangle(17, 11, probe.Width, probe.Height));
            printOutside = CountNotColor(bitmap, Color.Fuchsia, new Rectangle(0, 0, 17, bitmap.Height));
            bitmap.Save(Path.Combine(outputDirectory, "production-printclient.png"));
        }
        var drawBitmapPixels = 0;
        var drawBitmapError = "none";
        using (var bitmap = new Bitmap(probe.Width, probe.Height, PixelFormat.Format32bppArgb))
        {
            using (var graphics = Graphics.FromImage(bitmap)) graphics.Clear(Color.Fuchsia);
            try
            {
                probe.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                drawBitmapPixels = CountNotColor(bitmap, Color.Fuchsia, new Rectangle(Point.Empty, bitmap.Size));
                bitmap.Save(Path.Combine(outputDirectory, "production-draw-to-bitmap.png"));
            }
            catch (Exception exception)
            {
                drawBitmapError = exception.GetType().Name + ": " + exception.Message;
            }
        }
        var invariant = before == Signature(probe);
        var pass = screenPaint > 0 && screenDraw > 0 && child != IntPtr.Zero &&
                   targetHits == 3 && hover.MoveCount >= 3 && hover.ManagedMatches >= 3 && invariant &&
                   probe.PrintClientCount > 0 && probe.PrintDrawCount > 0 && printPixels > 0 &&
                   printOutside == 0 && drawBitmapPixels > 0 && probe.OutsideDrawCount == 0;
        Write("production-proof", "screenPaint=" + screenPaint + "; screenDraw=" + screenDraw +
            "; hoverTargetHits=" + targetHits + "; hoverMoves=" + hover.MoveCount + "; hoverMatches=" + hover.ManagedMatches +
            "; child=" + child + "; invariant=" + invariant +
            "; printClient=" + probe.PrintClientCount + "; printDraw=" + probe.PrintDrawCount +
            "; printPixels=" + printPixels + "; printOutside=" + printOutside +
            "; drawToBitmapPixels=" + drawBitmapPixels + "; drawToBitmapError=" + drawBitmapError +
            "; outsideDraw=" + probe.OutsideDrawCount + "; pass=" + pass);
        return pass;
    }

    private sealed class ProductionProbeTabs : BootstrapTabs
    {
        private int _paintDepth;
        private int _printDepth;
        internal int ScreenPaintCount { get; private set; }
        internal int ScreenDrawCount { get; private set; }
        internal int PrintClientCount { get; private set; }
        internal int PrintDrawCount { get; private set; }
        internal int OutsideDrawCount { get; private set; }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmPaint)
            {
                _paintDepth++;
                if (message.WParam == IntPtr.Zero) ScreenPaintCount++;
                Write("production-WM_PAINT", "depth=" + _paintDepth + "; supplied=" + (message.WParam != IntPtr.Zero) + "; handle=" + Handle);
                try { base.WndProc(ref message); }
                finally { _paintDepth--; }
                return;
            }
            if (message.Msg == WmPrint || message.Msg == WmPrintClient)
            {
                _printDepth++;
                if (message.Msg == WmPrintClient) PrintClientCount++;
                Write(message.Msg == WmPrint ? "production-WM_PRINT" : "production-WM_PRINTCLIENT",
                    "depth=" + _printDepth + "; flags=" + message.LParam + "; hdc=" + message.WParam + "; handle=" + Handle);
                try { base.WndProc(ref message); }
                finally { _printDepth--; }
                return;
            }
            base.WndProc(ref message);
        }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            Write("production-DrawItem", "index=" + e.Index + "; bounds=" + e.Bounds +
                "; paintDepth=" + _paintDepth + "; printDepth=" + _printDepth);
            if (_paintDepth > 0) ScreenDrawCount++;
            if (_printDepth > 0) PrintDrawCount++;
            if (_paintDepth == 0 && _printDepth == 0) OutsideDrawCount++;
            base.OnDrawItem(e);
        }
    }

    private sealed class ProbeTabControl : TabControl
    {
        private int _paintDepth;
        private int _printDepth;
        private bool _supplied;
        internal int SuppliedDrawCount { get; private set; }
        internal int PrintDrawCount { get; private set; }
        internal int OutsidePaintDrawCount { get; private set; }
        internal int MaxPaintDepth { get; private set; }
        internal int MaxPrintDepth { get; private set; }
        internal int PrintMessageCount { get; private set; }
        internal int PrintClientMessageCount { get; private set; }
        internal List<Rectangle> SuppliedDrawBounds { get; } = new List<Rectangle>();
        internal Rectangle SelectedDrawBounds { get; private set; }
        internal void StartSuppliedPaint() { _supplied = true; SuppliedDrawCount = 0; SuppliedDrawBounds.Clear(); SelectedDrawBounds = Rectangle.Empty; }
        internal void EndSuppliedPaint() => _supplied = false;
        internal void ResetPrintCounts() { PrintMessageCount = 0; PrintClientMessageCount = 0; PrintDrawCount = 0; MaxPrintDepth = 0; }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmPaint)
            {
                _paintDepth++;
                MaxPaintDepth = Math.Max(MaxPaintDepth, _paintDepth);
                Write("WM_PAINT", "depth=" + _paintDepth + "; supplied=" + (m.WParam != IntPtr.Zero) + "; handle=" + Handle);
                try { base.WndProc(ref m); }
                finally { _paintDepth--; }
                return;
            }
            if (m.Msg == WmEraseBkgnd) Write("WM_ERASEBKGND", "handle=" + Handle);
            if (m.Msg == WmPrint)
            {
                _printDepth++;
                PrintMessageCount++;
                MaxPrintDepth = Math.Max(MaxPrintDepth, _printDepth);
                Write("WM_PRINT", "depth=" + _printDepth + "; flags=" + m.LParam + "; hdc=" + m.WParam + "; handle=" + Handle);
                try { base.WndProc(ref m); }
                finally { _printDepth--; }
                return;
            }
            if (m.Msg == WmPrintClient)
            {
                _printDepth++;
                PrintClientMessageCount++;
                MaxPrintDepth = Math.Max(MaxPrintDepth, _printDepth);
                Write("WM_PRINTCLIENT", "depth=" + _printDepth + "; flags=" + m.LParam + "; hdc=" + m.WParam + "; handle=" + Handle);
                try { base.WndProc(ref m); }
                finally { _printDepth--; }
                return;
            }
            base.WndProc(ref m);
        }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            Write("DrawItem", "index=" + e.Index + "; bounds=" + e.Bounds + "; paintDepth=" + _paintDepth + "; printDepth=" + _printDepth + "; supplied=" + _supplied + "; hdc=" + e.Graphics.GetHdc());
            // GetHdc must be balanced before the normal event pipeline continues.
            e.Graphics.ReleaseHdc();
            if (_supplied) SuppliedDrawCount++;
            if (_supplied) SuppliedDrawBounds.Add(e.Bounds);
            if (_supplied && e.Index == SelectedIndex) SelectedDrawBounds = e.Bounds;
            if (_printDepth > 0) PrintDrawCount++;
            if (_paintDepth == 0 && _printDepth == 0) OutsidePaintDrawCount++;
            using (var brush = new SolidBrush(Color.Navy)) e.Graphics.FillRectangle(brush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, TabPages[e.Index].Text, Font, e.Bounds, Color.White, TextFormatFlags.EndEllipsis);
            base.OnDrawItem(e);
        }
    }

    private sealed class HoverWindowProbe : NativeWindow, IDisposable
    {
        private readonly TabControl _owner;
        internal int MoveCount { get; private set; }
        internal int ManagedMatches { get; private set; }
        internal int DirtyCount { get; private set; }
        internal HoverWindowProbe(TabControl owner)
        {
            _owner = owner;
            AssignHandle(owner.Handle);
        }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg != WmMouseMove)
            {
                base.WndProc(ref message);
                return;
            }
            var value = message.LParam.ToInt64();
            var point = new Point(unchecked((short)(value & 0xffff)), unchecked((short)((value >> 16) & 0xffff)));
            var hit = new TabHitTestInfo { Point = point };
            var nativeIndex = SendMessage(_owner.Handle, 0x130D, IntPtr.Zero, ref hit).ToInt32();
            base.WndProc(ref message);
            var dirty = new NativeRect();
            var hasDirty = GetUpdateRect(_owner.Handle, ref dirty, false);
            var managedIndex = ManagedHoverIndex(_owner);
            MoveCount++;
            if (managedIndex == Convert.ToString(nativeIndex)) ManagedMatches++;
            if (hasDirty) DirtyCount++;
            Write("WM_MOUSEMOVE", _owner.GetType().Name + "; point=" + point + "; nativeHover=" + nativeIndex +
                "; managedHover=" + managedIndex + "; dirty=" + (hasDirty ? dirty.ToString() : "none") +
                "; handle=" + _owner.Handle);
        }
        public void Dispose() => ReleaseHandle();
    }

    private static string ManagedHoverIndex(TabControl control)
    {
        var ownerType = control is BootstrapTabControl ? typeof(BootstrapTabControl) :
            control is BootstrapTabs ? typeof(BootstrapTabs) : null;
        if (ownerType is null) return "native-only";
        var field = ownerType.GetField("_interaction", BindingFlags.Instance | BindingFlags.NonPublic);
        var interaction = field?.GetValue(control);
        var property = interaction?.GetType().GetProperty("HoveredIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null) return Convert.ToString(property.GetValue(interaction, null)) ?? "unknown";
        var legacy = ownerType.GetField("_hoveredIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        return legacy == null ? "unknown" : Convert.ToString(legacy.GetValue(control)) ?? "unknown";
    }

    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref TabHitTestInfo hit);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetUpdateRect(IntPtr hWnd, ref NativeRect bounds, [MarshalAs(UnmanagedType.Bool)] bool erase);
    [DllImport("gdi32.dll")] private static extern int SaveDC(IntPtr hdc);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool RestoreDC(IntPtr hdc, int savedDc);
    [DllImport("gdi32.dll")] private static extern int IntersectClipRect(IntPtr hdc, int left, int top, int right, int bottom);
    [DllImport("gdi32.dll")] private static extern int ExcludeClipRect(IntPtr hdc, int left, int top, int right, int bottom);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetViewportOrgEx(IntPtr hdc, int x, int y, out Point previous);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateSolidBrush(int color);
    [DllImport("gdi32.dll")] private static extern int GetPixel(IntPtr hdc, int x, int y);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DeleteObject(IntPtr handle);
    [DllImport("user32.dll")] private static extern int FillRect(IntPtr hdc, ref NativeRect bounds, IntPtr brush);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string? windowName);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect bounds);
    [DllImport("user32.dll", EntryPoint = "mouse_event")] private static extern void MouseEvent(uint flags, uint x, uint y, uint data, UIntPtr extraInfo);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(IntPtr window);
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect
    {
        internal int Left, Top, Right, Bottom;
        public override string ToString() => "{" + Left + "," + Top + "," + Right + "," + Bottom + "}";
    }
    [StructLayout(LayoutKind.Sequential)] private struct TabHitTestInfo
    {
        internal Point Point;
        internal uint Flags;
    }
}
