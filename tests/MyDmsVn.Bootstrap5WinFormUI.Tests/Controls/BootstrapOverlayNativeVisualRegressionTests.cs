using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapOverlayNativeVisualRegressionTests
{
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
                    Assert.That(CountDistinctSampledColors(bitmap), Is.GreaterThan(8), capture + " is blank or occluded.");
                }));
            }

            var metadata = Directory.GetFiles(outputDirectory, "*.txt")
                .SelectMany(File.ReadAllLines)
                .ToArray();
            Assert.That(metadata.Count(line => line == "popup.visible=true"), Is.EqualTo(4));
            Assert.That(metadata.Count(line => line.StartsWith("owner.dpi=", StringComparison.Ordinal)), Is.EqualTo(4));
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

    private static void RunCaptureHost(string outputDirectory)
    {
        var executable = Path.Combine(TestContext.CurrentContext.TestDirectory, "MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe");
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = "--scenario rounded-popup \"" + outputDirectory + "\"",
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

    private static int CountDistinctSampledColors(Bitmap bitmap)
    {
        var colors = new System.Collections.Generic.HashSet<int>();
        for (var y = 0; y < bitmap.Height; y += Math.Max(1, bitmap.Height / 24))
        for (var x = 0; x < bitmap.Width; x += Math.Max(1, bitmap.Width / 24))
            colors.Add(bitmap.GetPixel(x, y).ToArgb());
        return colors.Count;
    }
}
