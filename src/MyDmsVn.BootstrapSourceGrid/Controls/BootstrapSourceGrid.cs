using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using MyDmsVn.BootstrapSourceGrid.Editors;
using MyDmsVn.BootstrapSourceGrid.Internal;
using MyDmsVn.BootstrapSourceGrid.Theming;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Bootstrap-themed SourceGrid while preserving SourceGrid grid behavior.
/// </summary>
[ToolboxItem(true)]
public class BootstrapSourceGrid : SourceGrid.Grid
{
    private bool _initialized;
    private bool _settingThemeFont;
    private bool _themeSubscribed;
    private int _themeRefreshPending;
    private readonly int _owningThreadId;
    private bool _useThemeFont = true;
    private Font? _themeFont;
    private BootstrapFontToken? _themeFontToken;
    private BootstrapSourceGridThemeSnapshot _themeSnapshot;
    private BootstrapSourceGridDpiMetrics _dpiMetrics;
    private readonly BootstrapSourceGridStyleApplicator _styleApplicator;
    private readonly BootstrapSourceGridSelectionStyle _selectionStyle;
    private readonly BootstrapSourceGridEditorRegistry _editorRegistry;

    /// <summary>
    /// Initializes a new Bootstrap-themed SourceGrid.
    /// </summary>
    public BootstrapSourceGrid()
    {
        _owningThreadId = Thread.CurrentThread.ManagedThreadId;
        Name = nameof(BootstrapSourceGrid);
        var theme = BootstrapThemeManager.CurrentTheme;
        _themeSnapshot = BootstrapSourceGridThemeAdapter.CreateSnapshot(theme);
        _dpiMetrics = BootstrapSourceGridDpiMetrics.FromTheme(theme, CurrentDpi);
        _styleApplicator = new BootstrapSourceGridStyleApplicator(
            _themeSnapshot,
            _dpiMetrics);
        _selectionStyle = new BootstrapSourceGridSelectionStyle();
        _editorRegistry = new BootstrapSourceGridEditorRegistry(this);
        _initialized = true;
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont(_themeSnapshot.BodyFont);
        ApplyBootstrapTheme();
    }

    internal BootstrapSourceGridThemeSnapshot CurrentThemeSnapshot => _themeSnapshot;

    internal BootstrapSourceGridDpiMetrics CurrentDpiMetrics => _dpiMetrics;

    internal bool IsThemeSubscribed => _themeSubscribed;

    internal Font? OwnedThemeFont => _themeFont;

    /// <summary>
    /// Gets the registry that creates Bootstrap editor adapters owned by this grid.
    /// </summary>
    /// <remarks>
    /// Create one editor per column or configuration and assign that shared instance to
    /// multiple cells in this grid. This grid owns disposal. Do not use an editor created
    /// here with another grid.
    /// </remarks>
    public BootstrapSourceGridEditorRegistry BootstrapEditors => _editorRegistry;

    internal BootstrapSourceGridEditorRegistry EditorRegistry => _editorRegistry;

    internal int CurrentDpi => IsHandleCreated && DeviceDpi > 0
        ? DeviceDpi
        : MyDmsVn.Bootstrap5WinFormUI.Rendering.DpiScaler.DefaultDpi;

    /// <inheritdoc />
    public override SourceGrid.Cells.ICellVirtual GetCell(int row, int column)
    {
        var cell = base.GetCell(row, column);
        _styleApplicator.ApplyDefaultView(cell);
        return cell;
    }

    /// <inheritdoc />
    protected override SourceGrid.Selection.SelectionBase CreateSelectionObject()
    {
        var selection = base.CreateSelectionObject();
        if (_initialized)
        {
            selection.BindToGrid(this);
            try
            {
                ApplySelectionTheme(selection, BootstrapThemeManager.CurrentTheme);
            }
            finally
            {
                selection.UnBindToGrid();
            }
        }

        return selection;
    }

    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (!_initialized)
        {
            return;
        }

        if (!_settingThemeFont)
        {
            _useThemeFont = false;
            DisposeThemeFont();
        }

        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        RefreshDpiMetrics(CurrentDpi);
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (ApplyPendingThemeChange())
        {
            return;
        }

        RefreshDpiMetrics(CurrentDpi);
    }

    /// <inheritdoc />
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Home || keyData == Keys.End)
        {
            var args = new KeyEventArgs(keyData);
            OnKeyDown(args);
            if (args.Handled)
            {
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <inheritdoc />
    public override void ProcessSpecialGridKey(KeyEventArgs e)
    {
        if (!e.Handled &&
            e.KeyData == (Keys.Tab | Keys.Shift) &&
            (SpecialKeys & SourceGrid.GridSpecialKeys.Tab) == SourceGrid.GridSpecialKeys.Tab)
        {
            e.Handled = ProcessReverseTab();
            return;
        }

        if (!e.Handled &&
            ((e.KeyData == Keys.Home && TryMoveToRowBoundary(first: true)) ||
             (e.KeyData == Keys.End && TryMoveToRowBoundary(first: false))))
        {
            e.Handled = true;
            return;
        }

        base.ProcessSpecialGridKey(e);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && e.KeyCode == Keys.F2)
        {
            FocusCanonicalActivePosition();
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _editorRegistry.Dispose();

            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }

            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (Thread.CurrentThread.ManagedThreadId == _owningThreadId)
        {
            Interlocked.Exchange(ref _themeRefreshPending, 0);
            ApplyThemeChange();
            return;
        }

        Interlocked.Exchange(ref _themeRefreshPending, 1);
        if (!IsHandleCreated)
        {
            return;
        }

        try
        {
            PostThemeChange(() => ApplyPendingThemeChange());
        }
        catch (InvalidOperationException) when (IsDisposed || Disposing)
        {
            // Disposal can race a queued application-level theme change.
        }
        catch (InvalidOperationException)
        {
            // The pending bit was armed before posting so a replacement handle can drain it.
        }
    }

    internal virtual void PostThemeChange(Action callback)
    {
        BeginInvoke(callback);
    }

    private bool ApplyPendingThemeChange()
    {
        if (Interlocked.Exchange(ref _themeRefreshPending, 0) == 0)
        {
            return false;
        }

        ApplyThemeChange();
        return true;
    }

    private void ApplyThemeChange()
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        ApplyBootstrapTheme();
        Invalidate();
    }

    internal virtual void ApplyBootstrapTheme()
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        _themeSnapshot = BootstrapSourceGridThemeAdapter.CreateSnapshot(theme);
        _dpiMetrics = BootstrapSourceGridDpiMetrics.FromTheme(theme, CurrentDpi);
        BackColor = _themeSnapshot.CellBackColor;
        ForeColor = _themeSnapshot.CellForeColor;
        _styleApplicator.ApplyTheme(_themeSnapshot, _dpiMetrics);
        _selectionStyle.ApplyTheme(
            (SourceGrid.Selection.SelectionBase)Selection,
            _themeSnapshot,
            _dpiMetrics);
        if (_useThemeFont)
        {
            ApplyThemeFont(_themeSnapshot.BodyFont);
        }

        BootstrapSourceGridEditorStyler.RefreshActiveEditor(this);
    }

    internal void RefreshDpiMetrics(int dpi)
    {
        var effectiveDpi = dpi > 0
            ? dpi
            : MyDmsVn.Bootstrap5WinFormUI.Rendering.DpiScaler.DefaultDpi;
        _dpiMetrics = BootstrapSourceGridDpiMetrics.FromTheme(
            BootstrapThemeManager.CurrentTheme,
            effectiveDpi);
        _styleApplicator.ApplyTheme(_themeSnapshot, _dpiMetrics);
        _selectionStyle.ApplyTheme(
            (SourceGrid.Selection.SelectionBase)Selection,
            _themeSnapshot,
            _dpiMetrics);
        PerformLayout();
        Invalidate();
    }

    private void ApplySelectionTheme(
        SourceGrid.Selection.SelectionBase selection,
        BootstrapTheme theme)
    {
        _selectionStyle.ApplyTheme(
            selection,
            _themeSnapshot,
            BootstrapSourceGridDpiMetrics.FromTheme(theme, CurrentDpi));
    }

    private void ApplyThemeFont(BootstrapFontToken token)
    {
        if (ThemeFontMatches(token))
        {
            return;
        }

        var nextFont = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previous = _themeFont;
        // Different requested families can resolve to the same installed fallback font.
        if (previous is not null && previous.Equals(nextFont))
        {
            nextFont.Dispose();
            _themeFontToken = token;
            return;
        }

        _settingThemeFont = true;
        try
        {
            Font = nextFont;
        }
        catch
        {
            nextFont.Dispose();
            throw;
        }
        finally
        {
            _settingThemeFont = false;
        }

        if (!ReferenceEquals(Font, nextFont))
        {
            nextFont.Dispose();
            _themeFontToken = token;
            return;
        }

        _themeFont = nextFont;
        _themeFontToken = token;
        previous?.Dispose();
    }

    private bool ThemeFontMatches(BootstrapFontToken token)
    {
        return _themeFont is not null &&
            _themeFontToken is not null &&
            string.Equals(_themeFontToken.FontFamilyName, token.FontFamilyName, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(_themeFontToken.SizeInPoints - token.SizeInPoints) < 0.01f &&
            _themeFontToken.Style == token.Style;
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        _themeFontToken = null;
        font?.Dispose();
    }

    private bool ProcessReverseTab()
    {
        var active = Selection.ActivePosition;
        if (!active.IsEmpty())
        {
            var context = new SourceGrid.CellContext(this, active);
            if (context.Cell is not null &&
                context.IsEditing() &&
                !context.EndEdit(false))
            {
                return true;
            }
        }

        if (!Selection.MoveActiveCell(0, -1, -1, int.MaxValue))
        {
            FindForm()?.SelectNextControl(this, false, true, true, true);
        }

        return true;
    }

    private bool TryMoveToRowBoundary(bool first)
    {
        var active = Selection.ActivePosition;
        if (active.IsEmpty())
        {
            return false;
        }

        var context = new SourceGrid.CellContext(this, active);
        if (context.Cell is not null && context.IsEditing())
        {
            return false;
        }

        var column = first ? 0 : ColumnsCount - 1;
        var limit = first ? ColumnsCount : -1;
        var step = first ? 1 : -1;
        var previousTarget = SourceGrid.Position.Empty;
        for (; column != limit; column += step)
        {
            if (!Columns.IsColumnVisible(column))
            {
                continue;
            }

            var target = PositionToStartPosition(
                new SourceGrid.Position(active.Row, column));
            if (target.IsEmpty() || target == previousTarget)
            {
                continue;
            }

            previousTarget = target;
            if (!Columns.IsColumnVisible(target.Column))
            {
                continue;
            }

            if (!Selection.CanReceiveFocus(target))
            {
                continue;
            }

            Selection.Focus(target, true);
            return true;
        }

        return false;
    }

    private void FocusCanonicalActivePosition()
    {
        var active = Selection.ActivePosition;
        if (active.IsEmpty())
        {
            return;
        }

        var canonical = PositionToStartPosition(active);
        if (!canonical.IsEmpty() && canonical != active)
        {
            Selection.Focus(canonical, true);
        }
    }
}
