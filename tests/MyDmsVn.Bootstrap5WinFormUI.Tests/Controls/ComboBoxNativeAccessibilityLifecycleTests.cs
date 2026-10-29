using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class ComboBoxNativeAccessibilityLifecycleTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void AccessibilityQueriesDuringSimpleRecreationDoNotCrashAndStableProvidersRemainAvailable(bool primeProvider)
    {
        var output = Path.Combine(Path.GetTempPath(), "WinFormsKit-combo-accessibility-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(TestContext.CurrentContext.TestDirectory, "MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe"),
                Arguments = "--scenario " + (primeProvider ? "combo-accessibility-primed" : "combo-accessibility") + " \"" + output + "\"",
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true
            })!;
            var errorRead = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(15000))
            {
                process.Kill();
                if (!process.WaitForExit(5000)) throw new TimeoutException("Accessibility helper cleanup could not be confirmed.");
                Assert.Fail("Accessibility scenario timed out; helper termination confirmed.");
            }
            Assert.That(process.ExitCode, Is.Zero, errorRead.GetAwaiter().GetResult());
            Assert.That(File.ReadAllLines(Path.Combine(output, "combo-accessibility.txt")), Does.Contain("oracle.stable-native-accessibility=pass"));
        }
        finally { Directory.Delete(output, true); }
    }
}
