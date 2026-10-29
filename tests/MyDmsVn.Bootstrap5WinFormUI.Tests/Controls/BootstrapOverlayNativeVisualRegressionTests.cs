using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapOverlayNativeVisualRegressionTests
{
    [Test]
    public void CaptureColorOracleFindsContentBetweenCoarseGridPoints()
    {
        using var bitmap = new Bitmap(120, 120);
        using (var graphics = Graphics.FromImage(bitmap)) graphics.Clear(Color.White);
        for (var index = 1; index <= 8; index++) bitmap.SetPixel(1, index, Color.FromArgb(index * 20, 0, 0));
        Assert.That(CountDistinctColors(bitmap), Is.EqualTo(9), "Sparse sampling must not misclassify real pixels between sample points as a blank capture.");
        var hostOracle = Assembly.Load("MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost")
            .GetType("MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.RoundedPopupScenario")!
            .GetMethod("RequireCaptureContent", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.DoesNotThrow((Action)(() => hostOracle.Invoke(null, new object[] { bitmap, "literal sparse-content fixture" })));
    }

    [TestCase(0xE7EAED, false, true)]
    [TestCase(0x41484E, true, true)]
    [TestCase(0xFFFFFF, false, false)]
    [TestCase(0xF8F8F8, false, false)]
    [TestCase(0x000000, true, false)]
    public void CornerOracleAcceptsAntialiasedBorderButRejectsSurfaceAndBackdrop(int rgb, bool dark, bool expected)
    {
        var assembly = Assembly.Load("MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost");
        var method = assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.RoundedPopupScenario")!
            .GetMethod("IsBorderLike", BindingFlags.Static | BindingFlags.NonPublic)!;
        var actual = Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
        var border = dark ? Color.FromArgb(73, 80, 87) : Color.FromArgb(222, 226, 230);
        var surface = dark ? Color.FromArgb(43, 48, 53) : Color.White;
        Assert.That(method.Invoke(null, new object[] { actual, border, surface }), Is.EqualTo(expected));
    }

    [Test]
    public void DisplayedInputRowsKeepNativeHeightAndFlowContainmentAtActualDpi()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "WinFormsKit-input-row-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        try
        {
            RunCaptureHost(outputDirectory, "input-row");
            var captures = Directory.GetFiles(outputDirectory, "*.png");
            Assert.That(captures, Has.Length.EqualTo(4));
            foreach (var capture in captures)
            {
                using var bitmap = new Bitmap(capture);
                Assert.That(CountTransparentPixels(bitmap), Is.Zero, capture);
                Assert.That(CountDistinctColors(bitmap), Is.GreaterThan(8), capture + " is blank or occluded.");
            }
            var metadata = Directory.GetFiles(outputDirectory, "*.txt").SelectMany(File.ReadAllLines).ToArray();
            Assert.That(metadata.Count(line => line == "oracle.native-dpi=pass"), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line == "oracle.equal-height=pass"), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line == "oracle.flow-containment=pass"), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line == "oracle.stable-layout=pass"), Is.EqualTo(4));
        }
        finally { Directory.Delete(outputDirectory, true); }
    }

    [Test]
    public void DisplayedSelectAndLookupPopupsProduceNonBlankOpaqueDesktopCaptures()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "WinFormsKit-rounded-popup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        try
        {
            RunCaptureHost(outputDirectory);
            var captures = Directory.GetFiles(outputDirectory, "*.png").OrderBy(path => path, StringComparer.Ordinal).ToArray();
            Assert.That(captures, Has.Length.EqualTo(4), "Expected Light/Dark captures for both Select and Lookup.");
            foreach (var capture in captures)
            {
                using var bitmap = new Bitmap(capture);
                Assert.Multiple((Action)(() =>
                {
                    Assert.That(bitmap.Width, Is.GreaterThan(80), capture);
                    Assert.That(bitmap.Height, Is.GreaterThan(40), capture);
                    Assert.That(CountTransparentPixels(bitmap), Is.Zero, capture + " must be an opaque desktop capture.");
                    Assert.That(CountDistinctColors(bitmap), Is.GreaterThan(8), capture + " is blank or occluded.");
                }));
            }

            var metadata = Directory.GetFiles(outputDirectory, "*.txt")
                .SelectMany(File.ReadAllLines)
                .ToArray();
            foreach (var metadataPath in Directory.GetFiles(outputDirectory, "*.txt"))
            {
                var values = File.ReadAllLines(metadataPath).Select(line => line.Split(new[] { '=' }, 2)).ToDictionary(parts => parts[0], parts => parts[1]);
                Assert.That(values["owner.dpi"], Is.EqualTo(values["owner.windowDpi"]), "Managed/native owner DPI must agree: " + metadataPath);
                Assert.That(values["owner.dpi"], Is.EqualTo(values["popup.windowDpi"]), "Owner/popup DPI must agree: " + metadataPath);
            }
            Assert.That(metadata.Count(line => line == "popup.visible=true"), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line.StartsWith("owner.dpi=", StringComparison.Ordinal)), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line.StartsWith("popup.windowDpi=", StringComparison.Ordinal)), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line.StartsWith("process.awareness=", StringComparison.Ordinal)), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line.StartsWith("thread.awareness=", StringComparison.Ordinal)), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line.StartsWith("popup.awareness=", StringComparison.Ordinal)), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line == "thread.awareness=PerMonitorV2"), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line == "popup.awareness=PerMonitorV2"), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line.StartsWith("awareness.source=", StringComparison.Ordinal)), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line.StartsWith("popup.region.bounds=", StringComparison.Ordinal)), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line.StartsWith("content.bounds=", StringComparison.Ordinal)), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line.StartsWith("footer.bounds=", StringComparison.Ordinal)), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line == "oracle.edge-continuity=pass"), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line == "oracle.corner-symmetry=pass"), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line == "oracle.shell-band=pass"), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line == "oracle.content-containment=pass"), Is.EqualTo(4));
        }
        finally
        {
            try
            {
                Directory.Delete(outputDirectory, true);
            }
            catch (IOException)
            {
                TestContext.Progress.WriteLine("Native capture directory retained: " + outputDirectory);
            }
        }
    }

    private static void RunCaptureHost(string outputDirectory, string scenario = "rounded-popup")
    {
        var executable = Path.Combine(TestContext.CurrentContext.TestDirectory, "MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe");
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = "--scenario " + scenario + " \"" + outputDirectory + "\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        });
        Assert.That(process, Is.Not.Null);
        if (!process!.WaitForExit(30000))
        {
            process.Kill();
            Assert.Fail("The rounded-popup capture host did not finish within 30 seconds.");
        }

        Assert.That(process.ExitCode, Is.EqualTo(0), process.StandardError.ReadToEnd());
    }

    private static int CountTransparentPixels(Bitmap bitmap)
    {
        var count = 0;
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width; x++)
            if (bitmap.GetPixel(x, y).A == 0) count++;
        return count;
    }

    private static int CountDistinctColors(Bitmap bitmap)
    {
        var colors = new System.Collections.Generic.HashSet<int>();
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width; x++)
        {
            colors.Add(bitmap.GetPixel(x, y).ToArgb());
            if (colors.Count > 8) return colors.Count;
        }
        return colors.Count;
    }
}
