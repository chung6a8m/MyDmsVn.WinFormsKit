using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.BootstrapSourceGrid.Editors;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapEditorRegistryTests
{
    [Test]
    public void RegistryAndGridExposeTheSupportedPublicCreationSurface()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(typeof(BootstrapSourceGridEditorRegistry).IsPublic, Is.True);
            AssertPublicProperty(
                typeof(BootstrapSourceGridControl),
                "BootstrapEditors",
                typeof(BootstrapSourceGridEditorRegistry));
            AssertPublicCreateMethod("CreateTextBox", typeof(BootstrapTextBoxEditor));
            AssertPublicCreateMethod(
                "CreateFormattedTextBox",
                typeof(BootstrapFormattedTextBoxEditor));
            AssertPublicCreateMethod("CreateLookupBox", typeof(BootstrapLookupBoxEditor));
            AssertPublicProperty(
                typeof(BootstrapTextBoxEditor),
                "BootstrapControl",
                typeof(BootstrapTextBox));
            AssertPublicProperty(
                typeof(BootstrapFormattedTextBoxEditor),
                "BootstrapControl",
                typeof(BootstrapFormattedTextBox));
            AssertPublicProperty(
                typeof(BootstrapLookupBoxEditor),
                "BootstrapControl",
                typeof(BootstrapLookupBox));
        }));
    }

    [TestCase(typeof(BootstrapTextBoxEditor))]
    [TestCase(typeof(BootstrapFormattedTextBoxEditor))]
    [TestCase(typeof(BootstrapLookupBoxEditor))]
    public void AdapterConstructorsAreNotPublic(Type adapterType)
    {
        Assert.That(
            adapterType.GetConstructors(BindingFlags.Instance | BindingFlags.Public),
            Is.Empty);
    }

    [TestCase("CreateTextBox")]
    [TestCase("CreateFormattedTextBox")]
    [TestCase("CreateLookupBox")]
    public void CreateMethodsRejectNullValueTypeImmediately(string methodName)
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            Action create = methodName switch
            {
                "CreateTextBox" => () => grid.BootstrapEditors.CreateTextBox(null!),
                "CreateFormattedTextBox" => () =>
                    grid.BootstrapEditors.CreateFormattedTextBox(null!),
                "CreateLookupBox" => () => grid.BootstrapEditors.CreateLookupBox(null!),
                _ => throw new ArgumentOutOfRangeException(nameof(methodName)),
            };

            Assert.That(
                create,
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("valueType"));
        }
    }

    [TestCase("CreateTextBox")]
    [TestCase("CreateFormattedTextBox")]
    [TestCase("CreateLookupBox")]
    public void DisposedRegistryRejectsCreateMethods(string methodName)
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            var registry = grid.BootstrapEditors;
            registry.Dispose();
            Assert.That((Action)(() => registry.Dispose()), Throws.Nothing);

            Assert.That(
                CreateEditorAction(registry, methodName),
                Throws.TypeOf<ObjectDisposedException>()
                    .With.Property("ObjectName")
                    .EqualTo(nameof(BootstrapSourceGridEditorRegistry)));
        }
    }

    [TestCase("CreateTextBox")]
    [TestCase("CreateFormattedTextBox")]
    [TestCase("CreateLookupBox")]
    public void DisposedGridRejectsRegistryCreateMethods(string methodName)
    {
        var grid = new BootstrapSourceGridControl();
        var registry = grid.BootstrapEditors;
        grid.Dispose();

        Assert.That(
            CreateEditorAction(registry, methodName),
            Throws.TypeOf<ObjectDisposedException>()
                .With.Property("ObjectName")
                .EqualTo(nameof(BootstrapSourceGridEditorRegistry)));
    }

    [Test]
    public void RegistryConstructorRejectsNullOwner()
    {
        Action create = () => new BootstrapSourceGridEditorRegistry(null!);

        Assert.That(
            create,
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("owner"));
    }

    [Test]
    public void RegistryCreatedAdaptersRetainTheirCreatingGridAsOwner()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            var text = grid.BootstrapEditors.CreateTextBox(typeof(string));
            var formatted = grid.BootstrapEditors.CreateFormattedTextBox(typeof(decimal));
            var lookup = grid.BootstrapEditors.CreateLookupBox(typeof(int));

            Assert.Multiple((Action)(() =>
            {
                Assert.That(text.Owner, Is.SameAs(grid));
                Assert.That(formatted.Owner, Is.SameAs(grid));
                Assert.That(lookup.Owner, Is.SameAs(grid));
            }));
        }
    }

    [TestCase("Text")]
    [TestCase("Formatted")]
    [TestCase("Lookup")]
    public void AdapterConstructorsRejectNullOwner(string adapterKind)
    {
        Action create = adapterKind switch
        {
            "Text" => () => new BootstrapTextBoxEditor(null!, typeof(string)),
            "Formatted" => () =>
                new BootstrapFormattedTextBoxEditor(null!, typeof(decimal)),
            "Lookup" => () => new BootstrapLookupBoxEditor(null!, typeof(int)),
            _ => throw new ArgumentOutOfRangeException(nameof(adapterKind)),
        };

        Assert.That(
            create,
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("owner"));
    }

    [Test]
    public void TenThousandCellsShareOneTextEditorControlForOneConfiguration()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            const int rows = 100;
            const int columns = 100;
            const int editorColumn = 37;
            var editor = grid.BootstrapEditors.CreateTextBox(typeof(string));
            var bootstrapControls = new HashSet<System.Windows.Forms.Control>();
            grid.Redim(rows, columns);

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var cell = new SourceGrid.Cells.Cell($"{row}:{column}");
                    if (column == editorColumn)
                    {
                        cell.Editor = editor;
                        bootstrapControls.Add(((BootstrapTextBoxEditor)cell.Editor).Control);
                    }

                    grid[row, column] = cell;
                }
            }

            Assert.Multiple((Action)(() =>
            {
                Assert.That(grid.RowsCount * grid.ColumnsCount, Is.EqualTo(10_000));
                Assert.That(bootstrapControls, Has.Count.EqualTo(1));
                Assert.That(bootstrapControls, Does.Contain(editor.BootstrapControl));
            }));
        }
    }

    [Test]
    public void ThousandsOfCellsUseExactlyThreeExplicitConfigurationEditors()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            const int rows = 1_000;
            const int columns = 3;
            var text = grid.BootstrapEditors.CreateTextBox(typeof(string));
            var formatted = grid.BootstrapEditors.CreateFormattedTextBox(typeof(decimal));
            var lookup = grid.BootstrapEditors.CreateLookupBox(typeof(int));
            var configuredEditors = new SourceGrid.Cells.Editors.EditorBase[]
            {
                text,
                formatted,
                lookup,
            };
            var assignedEditors = new HashSet<SourceGrid.Cells.Editors.EditorBase>();
            var assignedControls = new HashSet<System.Windows.Forms.Control>();
            grid.Redim(rows, columns);

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var editor = configuredEditors[column];
                    grid[row, column] = new SourceGrid.Cells.Cell { Editor = editor };
                    assignedEditors.Add(grid[row, column].Editor);
                    assignedControls.Add(((SourceGrid.Cells.Editors.EditorControlBase)
                        grid[row, column].Editor).Control);
                }
            }

            Assert.Multiple((Action)(() =>
            {
                Assert.That(grid.RowsCount * grid.ColumnsCount, Is.EqualTo(3_000));
                Assert.That(assignedEditors, Is.EquivalentTo(configuredEditors));
                Assert.That(assignedControls, Has.Count.EqualTo(3));
            }));
        }
    }

    private static void AssertPublicCreateMethod(string name, Type returnType)
    {
        var method = typeof(BootstrapSourceGridEditorRegistry).GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(Type) },
            modifiers: null);

        Assert.That(method, Is.Not.Null, $"Missing public {name}(Type).");
        if (method is null)
        {
            return;
        }

        Assert.That(method.ReturnType, Is.EqualTo(returnType));
    }

    private static Action CreateEditorAction(
        BootstrapSourceGridEditorRegistry registry,
        string methodName)
    {
        return methodName switch
        {
            "CreateTextBox" => () => registry.CreateTextBox(typeof(string)),
            "CreateFormattedTextBox" => () => registry.CreateFormattedTextBox(typeof(decimal)),
            "CreateLookupBox" => () => registry.CreateLookupBox(typeof(int)),
            _ => throw new ArgumentOutOfRangeException(nameof(methodName)),
        };
    }

    private static void AssertPublicProperty(Type ownerType, string name, Type propertyType)
    {
        var property = ownerType.GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        Assert.That(property, Is.Not.Null, $"Missing public {ownerType.Name}.{name}.");
        if (property is null)
        {
            return;
        }

        Assert.That(property.PropertyType, Is.EqualTo(propertyType));
        Assert.That(property.CanRead, Is.True);
        Assert.That(property.SetMethod, Is.Null);
        Assert.That(property.GetMethod!.IsPublic, Is.True);
    }
}
