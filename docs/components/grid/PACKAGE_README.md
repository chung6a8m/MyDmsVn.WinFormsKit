# MyDmsVn.BootstrapSourceGrid

Bootstrap-themed SourceGrid for native Windows Forms. `BootstrapSourceGrid` derives directly from `SourceGrid.Grid`, preserving the familiar SourceGrid programming model while default cells, headers, selection, typography, and integration-owned DPI metrics follow `MyDmsVn.Bootstrap5WinFormUI` themes.

## Requirements

- Windows
- .NET Framework 4.8 or .NET 8 for Windows
- Exact vendor package versions declared by the validated release package

## Minimal usage

```csharp
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

var grid = new BootstrapSourceGrid { Dock = DockStyle.Fill };
grid.Redim(10, 3);
grid.FixedRows = 1;
grid[0, 0] = new SourceGrid.Cells.Header();
grid[0, 1] = new SourceGrid.Cells.ColumnHeader("Name");
grid[1, 0] = new SourceGrid.Cells.RowHeader(1);
grid[1, 1] = new SourceGrid.Cells.Cell("Northwind", typeof(string));
Controls.Add(grid);

BootstrapThemeManager.CurrentTheme =
    BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
```

Theme changes update the existing control without rebuilding application data. Explicit consumer Views and Fonts remain consumer-owned and are not replaced on later theme changes.

## Bootstrap editor adapters

Create and configure one grid-owned editor, then share it across cells in the same column or
configuration:

```csharp
var editor = grid.BootstrapEditors.CreateTextBox(typeof(string));
editor.BootstrapControl.PlaceholderText = "Customer name";

for (var row = 1; row < grid.RowsCount; row++)
{
    grid[row, 1].Editor = editor;
}
```

The grid owns disposal, including editors that are never started. Do not share an editor across
grid instances. On the pinned SourceGrid baseline this restriction is documented but is not
guaranteed to throw before SourceGrid attaches the editor control.

Logical values returned for SourceGrid conversion/validation are:

```text
BootstrapTextBox          -> Text
BootstrapFormattedTextBox -> RawValue
BootstrapLookupBox        -> SelectedValue
```

## MVP boundaries

SourceGrid/native scrollbars and the SourceGrid editor lifecycle remain intact. Legacy native
editor chrome can retain OS rendering, and consumer custom editors are not replaced. Automatic
theming applies only to exact SourceGrid default View singletons. The package targets concrete
`SourceGrid.Grid`, is Windows-only, and does not provide a separate Bootstrap virtual-grid type.

See the [project documentation](https://github.com/chung6a8m/MyDmsVn.BootstrapSourceGrid/blob/e311144c27cd79a6c34aa0308ad029651c9e03c9/README.md), including the known limitations and compatibility contract.

This integration is licensed under the MIT License.
