using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridDesignerContractTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void ControlIsAvailableInToolboxWithPublicParameterlessConstruction()
    {
        var controlType = typeof(BootstrapSourceGridControl);
        var toolboxItem = controlType.GetCustomAttribute<ToolboxItemAttribute>(true);
        var publicConstructors = controlType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

        Assert.That(toolboxItem, Is.Not.Null);
        Assert.That(toolboxItem!.Equals(new ToolboxItemAttribute(true)), Is.True);
        Assert.That(controlType.GetConstructor(Type.EmptyTypes), Is.Not.Null);
        Assert.That(publicConstructors.Length, Is.EqualTo(1));
        Assert.That(publicConstructors[0].GetParameters(), Is.Empty);
    }

    [Test]
    public void ConstructorDoesNotRequireParentHandleOrRuntimeServices()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            Assert.That(grid.IsHandleCreated, Is.False);
            Assert.That(grid.Parent, Is.Null);
            Assert.That(grid.Site, Is.Null);
            Assert.That(grid.CurrentThemeSnapshot, Is.Not.Null);
            Assert.That(grid.IsThemeSubscribed, Is.True);
        }
    }

    [Test]
    public void BootstrapInternalHelpersAreNotExposedAsBrowsablePublicProperties()
    {
        var declaredPublicProperties = typeof(BootstrapSourceGridControl)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        var bootstrapHelperProperties = declaredPublicProperties
            .Where(property =>
                property.Name.IndexOf("Theme", StringComparison.OrdinalIgnoreCase) >= 0 ||
                property.Name.IndexOf("Dpi", StringComparison.OrdinalIgnoreCase) >= 0 ||
                property.Name.IndexOf("StyleApplicator", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();

        Assert.That(bootstrapHelperProperties, Is.Empty);
    }
}
