using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.BootstrapSourceGrid.Editors;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Demo;

internal sealed class DemoGridContent
{
    private static readonly int[] BaselineColumnWidths =
        { 54, 140, 150, 160, 150, 130, 170, 170, 150, 250 };

    private readonly int[] _lastAppliedColumnWidths = new int[BaselineColumnWidths.Length];

    internal void Populate(
        BootstrapSourceGridControl grid,
        BootstrapTextBoxEditor textEditor,
        BootstrapFormattedTextBoxEditor formattedEditor,
        BootstrapLookupBoxEditor lookupEditor,
        DemoLookupItem[] lookupItems,
        SourceGrid.Cells.Controllers.CustomEvents lookupValueEvents)
    {
        const int rows = 40;
        const int columns = 10;
        // A same-size Redim keeps existing rows and spans, including their sorted positions.
        grid.Redim(0, 0);
        grid.Redim(rows, columns);
        grid.FixedRows = 1;
        grid.FixedColumns = 1;
        grid.Selection.EnableMultiSelection = true;
        grid[0, 0] = new SourceGrid.Cells.Header();
        grid[0, 1] = new SourceGrid.Cells.ColumnHeader("Name");
        grid[0, 2] = new SourceGrid.Cells.ColumnHeader("Bootstrap text");
        grid[0, 3] = new SourceGrid.Cells.ColumnHeader("Bootstrap formatted");
        grid[0, 4] = new SourceGrid.Cells.ColumnHeader("DateTime factory");
        grid[0, 5] = new SourceGrid.Cells.ColumnHeader("Boolean factory");
        grid[0, 6] = new SourceGrid.Cells.ColumnHeader("Bootstrap lookup");
        grid[0, 7] = new SourceGrid.Cells.ColumnHeader("Lookup display");
        grid[0, 8] = new SourceGrid.Cells.ColumnHeader("DateTimePicker");
        grid[0, 9] = new SourceGrid.Cells.ColumnHeader("Notes / scenarios");

        for (var row = 1; row < rows; row++)
        {
            grid[row, 0] = new SourceGrid.Cells.RowHeader(row);
            grid[row, 1] = new SourceGrid.Cells.Cell($"Item {row:00}", typeof(string));
            grid[row, 2] = new SourceGrid.Cells.Cell($"Customer {row:00}", typeof(string))
            {
                Editor = textEditor,
            };
            grid[row, 3] = new SourceGrid.Cells.Cell(row * 1234.5m, typeof(decimal))
            {
                Editor = formattedEditor,
            };
            grid[row, 4] = new SourceGrid.Cells.Cell(
                new DateTime(2026, 9, 1).AddDays(row),
                typeof(DateTime));
            grid[row, 5] = new SourceGrid.Cells.Cell((row & 1) == 0, typeof(bool));
            var lookupItem = lookupItems[(row - 1) % lookupItems.Length];
            var lookupId = lookupItem.Id;
            var lookupCell = new SourceGrid.Cells.Cell(lookupId, typeof(int))
            {
                Editor = lookupEditor,
            };
            lookupCell.AddController(lookupValueEvents);
            grid[row, 6] = lookupCell;
            grid[row, 7] = new SourceGrid.Cells.Cell(
                lookupItem.Name,
                typeof(string))
            {
                Editor = null,
            };

            if (row == 3)
            {
                grid[row, 8] = new SourceGrid.Cells.Cell(
                    "Two-column editable span",
                    typeof(string))
                {
                    ColumnSpan = 2,
                };
            }
            else
            {
                grid[row, 8] = new SourceGrid.Cells.Cell(
                    new DateTime(2026, 9, 1).AddDays(row),
                    new SourceGrid.Cells.Editors.DateTimePicker());
                grid[row, 9] = new SourceGrid.Cells.Cell(
                    "Theme, selection, and scrolling row",
                    typeof(string));
            }
        }

        var readOnlyCell = (SourceGrid.Cells.Cell)grid[1, 9];
        readOnlyCell.Value = "Read-only: editor disabled";
        readOnlyCell.Editor!.EnableEdit = false;

        var customView = new SourceGrid.Cells.Views.Cell
        {
            BackColor = Color.LemonChiffon,
            ForeColor = Color.DarkSlateBlue,
        };
        grid[2, 9].Value = "Consumer custom View (theme opt-out)";
        grid[2, 9].View = customView;

        grid.Rows[0].Height = 32;
        for (var column = 0; column < BaselineColumnWidths.Length; column++)
        {
            var width = BaselineColumnWidths[column];
            grid.Columns[column].Width = width;
            _lastAppliedColumnWidths[column] = width;
        }
    }

    internal void ApplyTypographyLayout(
        BootstrapSourceGridControl grid,
        BootstrapTextBoxEditor textEditor,
        BootstrapFormattedTextBoxEditor formattedEditor,
        BootstrapLookupBoxEditor lookupEditor)
    {
        var padding = grid.CurrentDpiMetrics.CellPadding;
        var headerTextHeight = TextRenderer.MeasureText("Bootstrap formatted", grid.Font).Height;
        var rowTextHeight = TextRenderer.MeasureText("Item 01", grid.Font).Height;
        var headerHeight = Math.Max(32, headerTextHeight + 2 * padding);
        var rowHeight = Math.Max(rowTextHeight + 2 * padding,
            Math.Max(textEditor.Control.PreferredSize.Height,
                Math.Max(formattedEditor.Control.PreferredSize.Height,
                    lookupEditor.Control.PreferredSize.Height)));

        if (grid.Rows[0].Height != headerHeight)
        {
            grid.Rows[0].Height = headerHeight;
        }

        for (var column = 1; column < grid.ColumnsCount; column++)
        {
            var position = new SourceGrid.Position(0, column);
            var header = grid[0, column];
            var requiredWidth = header.View.Measure(
                new SourceGrid.CellContext(grid, position, header), Size.Empty).Width;
            var targetWidth = Math.Max(BaselineColumnWidths[column], requiredWidth);
            // A different current width indicates that the user resized this demo column.
            if (grid.Columns[column].Width != _lastAppliedColumnWidths[column])
            {
                continue;
            }

            if (grid.Columns[column].Width != targetWidth)
            {
                grid.Columns[column].Width = targetWidth;
            }

            _lastAppliedColumnWidths[column] = targetWidth;
        }

        for (var row = 1; row < grid.RowsCount; row++)
        {
            if (grid.Rows[row].Height != rowHeight)
            {
                grid.Rows[row].Height = rowHeight;
            }
        }
    }
}

internal sealed class DemoLookupItem
{
    private DemoLookupItem(int id, string name, string region)
    {
        Id = id;
        Name = name;
        Region = region;
    }

    public int Id { get; }

    public string Name { get; }

    public string Region { get; }

    internal static DemoLookupItem[] CreateSampleData()
    {
        return new[]
        {
            new DemoLookupItem(1, "Northwind Traders", "Europe"),
            new DemoLookupItem(2, "Contoso", "North America"),
            new DemoLookupItem(3, "Adventure Works", "North America"),
            new DemoLookupItem(4, "Tailspin Toys", "Asia"),
            new DemoLookupItem(5, "Fabrikam", "Europe"),
            new DemoLookupItem(6, "Woodgrove Bank", "Oceania"),
            new DemoLookupItem(7, "Litware", "Asia"),
            new DemoLookupItem(8, "Proseware", "Europe"),
            new DemoLookupItem(9, "Wide World Importers", "Africa"),
            new DemoLookupItem(10, "Humongous Insurance", "North America"),
            new DemoLookupItem(11, "Consolidated Messenger", "Asia"),
            new DemoLookupItem(12, "Fourth Coffee", "South America"),
        };
    }
}
