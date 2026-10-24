using System.Threading;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridConstructionTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void PublicTypeHasRequiredIdentity()
    {
        Assert.That(typeof(BootstrapSourceGridControl).FullName,
            Is.EqualTo("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid"));
        Assert.That(typeof(BootstrapSourceGridControl).BaseType, Is.EqualTo(typeof(SourceGrid.Grid)));
    }

    [Test]
    public void CanConstructAndDisposeBeforeHandleCreation()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            Assert.That(grid.IsHandleCreated, Is.False);
        }
    }
}
