using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.BootstrapSourceGrid.Theming;

internal sealed class BootstrapSourceGridThemeSnapshot
{
    public BootstrapSourceGridThemeSnapshot(
        Color cellBackColor,
        Color alternateCellBackColor,
        Color cellForeColor,
        Color mutedForeColor,
        Color headerBackColor,
        Color headerForeColor,
        Color borderColor,
        Color selectionBackColor,
        Color selectionForeColor,
        Color focusColor,
        Color disabledColor,
        Color hoverColor,
        Color activeColor,
        BootstrapFontToken bodyFont)
    {
        CellBackColor = cellBackColor;
        AlternateCellBackColor = alternateCellBackColor;
        CellForeColor = cellForeColor;
        MutedForeColor = mutedForeColor;
        HeaderBackColor = headerBackColor;
        HeaderForeColor = headerForeColor;
        BorderColor = borderColor;
        SelectionBackColor = selectionBackColor;
        SelectionForeColor = selectionForeColor;
        FocusColor = focusColor;
        DisabledColor = disabledColor;
        HoverColor = hoverColor;
        ActiveColor = activeColor;
        BodyFont = bodyFont;
    }

    public Color CellBackColor { get; }

    public Color AlternateCellBackColor { get; }

    public Color CellForeColor { get; }

    public Color MutedForeColor { get; }

    public Color HeaderBackColor { get; }

    public Color HeaderForeColor { get; }

    public Color BorderColor { get; }

    public Color SelectionBackColor { get; }

    public Color SelectionForeColor { get; }

    public Color FocusColor { get; }

    public Color DisabledColor { get; }

    public Color HoverColor { get; }

    public Color ActiveColor { get; }

    public BootstrapFontToken BodyFont { get; }
}
