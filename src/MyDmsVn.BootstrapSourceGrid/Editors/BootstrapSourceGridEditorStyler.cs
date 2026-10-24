using MyDmsVn.BootstrapSourceGrid.Views;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Editors;

internal static class BootstrapSourceGridEditorStyler
{
    internal static void RefreshActiveEditor(BootstrapSourceGridControl grid)
    {
        var position = grid.Selection.ActivePosition;
        if (position.IsEmpty())
        {
            return;
        }

        var cell = grid.GetCell(position.Row, position.Column);
        if (cell is null ||
            cell.Editor is not SourceGrid.Cells.Editors.EditorControlBase editor ||
            !editor.IsEditing ||
            !editor.UseCellViewProperties)
        {
            return;
        }

        var backColor = cell.View.BackColor;
        var foreColor = cell.View.ForeColor;
        if (cell.View is BootstrapSourceGridCellView)
        {
            var snapshot = grid.CurrentThemeSnapshot;
            backColor = (position.Row & 1) == 0
                ? snapshot.CellBackColor
                : snapshot.AlternateCellBackColor;
            foreColor = grid.Enabled
                ? snapshot.CellForeColor
                : snapshot.DisabledColor;
        }

        editor.Control.BackColor = backColor;
        editor.Control.ForeColor = foreColor;
        editor.Control.Font = cell.View.Font ?? grid.Font;
    }
}
