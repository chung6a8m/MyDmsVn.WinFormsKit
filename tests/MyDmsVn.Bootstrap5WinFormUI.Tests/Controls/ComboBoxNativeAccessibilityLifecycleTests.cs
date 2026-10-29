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
    [Test]
    public void AccessibilityQueriesDuringSimpleRecreationDoNotCrashAndStableProvidersRemainAvailable()
    {
        var output = Path.Combine(Path.GetTempPath(), "WinFormsKit-combo-accessibility-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(TestContext.CurrentContext.TestDirectory, "MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe"),
                Arguments = "--scenario combo-accessibility \"" + output + "\"",
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true
            })!;
            var errorRead = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(15000)) { process.Kill(); Assert.Fail("Accessibility scenario timed out."); }
            Assert.That(process.ExitCode, Is.Zero, errorRead.GetAwaiter().GetResult());
            Assert.That(File.ReadAllLines(Path.Combine(output, "combo-accessibility.txt")), Does.Contain("oracle.stable-native-accessibility=pass"));
        }
        finally { Directory.Delete(output, true); }
    }
}
