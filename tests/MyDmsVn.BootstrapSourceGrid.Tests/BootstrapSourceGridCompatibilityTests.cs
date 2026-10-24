using System.Threading;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridCompatibilityTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void CommonConcreteGridApisRetainSourceGridBehavior()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            grid.Redim(4, 3);
            grid[0, 0] = new SourceGrid.Cells.Cell("Northwind");

            Assert.That(grid.RowsCount, Is.EqualTo(4));
            Assert.That(grid.ColumnsCount, Is.EqualTo(3));
            Assert.That(grid[0, 0].Value, Is.EqualTo("Northwind"));

            grid.Rows[0].Height = 32;
            grid.Columns[0].Width = 120;

            Assert.That(grid.Rows[0].Height, Is.EqualTo(32));
            Assert.That(grid.Columns[0].Width, Is.EqualTo(120));
        }
    }

    [Test]
    public void CoveredPositionsResolveToAssignedSpannedCell()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            grid.Redim(4, 4);
            var cell = new SourceGrid.Cells.Cell("Spanned");
            grid[1, 1] = cell;
            cell.RowSpan = 2;
            cell.ColumnSpan = 2;

            Assert.That(grid.GetCell(1, 2), Is.SameAs(cell));
            Assert.That(grid.GetCell(2, 1), Is.SameAs(cell));
            Assert.That(grid.GetCell(2, 2), Is.SameAs(cell));
        }
    }
}
