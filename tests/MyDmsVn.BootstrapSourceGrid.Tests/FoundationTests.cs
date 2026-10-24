using System.Threading;
using NUnit.Framework;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class FoundationTests
{
    [Test]
    public void AssemblyWinFormsGuardRunsBeforeFixtureTests()
    {
        Assert.That(WinFormsTestGuard.IsConfigured, Is.True);
    }

    [Test]
    public void VendorTypesAreLoadable()
    {
        Assert.That(typeof(SourceGrid.Grid), Is.Not.Null);
        Assert.That(typeof(MyDmsVn.Bootstrap5WinFormUI.Theme.BootstrapThemeManager), Is.Not.Null);
    }
}
