using System.Drawing;
using DevAge.Drawing;
using MyDmsVn.BootstrapSourceGrid.Theming;

namespace MyDmsVn.BootstrapSourceGrid.Internal;

internal sealed class BootstrapSourceGridSelectionStyle
{
    internal const int SelectionOverlayAlpha = 75;

    private bool _hasApplied;
    private bool _ownsBackColor = true;
    private bool _ownsFocusBackColor = true;
    private bool _ownsBorder = true;
    private Color _lastBackColor;
    private Color _lastFocusBackColor;
    private RectangleBorder _lastBorder;
    private SourceGrid.Selection.SelectionBase? _selection;

    internal void ApplyTheme(
        SourceGrid.Selection.SelectionBase selection,
        BootstrapSourceGridThemeSnapshot snapshot,
        BootstrapSourceGridDpiMetrics dpiMetrics)
    {
        if (!ReferenceEquals(selection, _selection))
        {
            _selection = selection;
            _hasApplied = false;
            _ownsBackColor = true;
            _ownsFocusBackColor = true;
            _ownsBorder = true;
        }

        var nextBackColor = Color.FromArgb(SelectionOverlayAlpha, snapshot.SelectionBackColor);
        var nextFocusBackColor = Color.Transparent;
        var nextBorder = new RectangleBorder(
            new BorderLine(snapshot.FocusColor, dpiMetrics.FocusThickness));

        if (!_hasApplied || (_ownsBackColor && selection.BackColor.Equals(_lastBackColor)))
        {
            selection.BackColor = nextBackColor;
            _lastBackColor = nextBackColor;
        }
        else
        {
            _ownsBackColor = false;
        }

        if (!_hasApplied ||
            (_ownsFocusBackColor && selection.FocusBackColor.Equals(_lastFocusBackColor)))
        {
            selection.FocusBackColor = nextFocusBackColor;
            _lastFocusBackColor = nextFocusBackColor;
        }
        else
        {
            _ownsFocusBackColor = false;
        }

        if (!_hasApplied || (_ownsBorder && selection.Border.Equals(_lastBorder)))
        {
            selection.Border = nextBorder;
            _lastBorder = nextBorder;
        }
        else
        {
            _ownsBorder = false;
        }

        _hasApplied = true;
    }
}
