using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapTabCloseController : IDisposable
{
    private readonly TabControl _owner;
    private readonly Func<TabPage, TabCloseReason, bool> _requestClose;
    private readonly HashSet<TabPage> _closablePages = new();
    private ContextMenuStrip? _menu;
    private ToolStripMenuItem? _menuItem;
    private TabPage? _menuPage;
    private TabPage? _keyboardMenuPage;
    private bool _suppressMouseUp;

    internal BootstrapTabCloseController(TabControl owner, Func<TabPage, TabCloseReason, bool> requestClose)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _requestClose = requestClose ?? throw new ArgumentNullException(nameof(requestClose));
    }

    internal void SetClosable(TabPage page, bool closable)
    {
        if (closable) { _closablePages.Add(page); EnsureMenu(); }
        else _closablePages.Remove(page);
        UpdateMenuPage(_owner.SelectedTab ?? page);
    }

    internal bool IsClosable(TabPage page) => page != null && _owner.TabPages.Contains(page) && _closablePages.Contains(page);

    internal Rectangle GetButtonBounds(int index)
    {
        if (index < 0 || index >= _owner.TabCount || !IsClosable(_owner.TabPages[index])) return Rectangle.Empty;
        var header = _owner.GetTabRect(index);
        var dpi = _owner.DeviceDpi > 0 ? _owner.DeviceDpi : DpiScaler.DefaultDpi;
        var size = Math.Min(header.Height - 4, Math.Max(12, DpiScaler.Scale(16, dpi)));
        return new Rectangle(header.Right - size - 4, header.Top + (header.Height - size) / 2, size, size);
    }

    internal bool TryHandleMouseDown(Point location)
    {
        if (!_owner.Enabled) return false;
        for (var index = 0; index < _owner.TabCount; index++)
        {
            var page = _owner.TabPages[index];
            if (!IsClosable(page) || !GetButtonBounds(index).Contains(location)) continue;
            _suppressMouseUp = true;
            _requestClose(page, TabCloseReason.Pointer);
            return true;
        }
        return false;
    }

    internal bool TryHandleMouseUp()
    {
        if (!_suppressMouseUp) return false;
        _suppressMouseUp = false;
        return true;
    }

    internal bool TryProcessShortcut(Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.W) && _owner.SelectedTab is TabPage selected)
            return _requestClose(selected, TabCloseReason.Keyboard);
        if (keyData != (Keys.Shift | Keys.F10) || _owner.SelectedTab is not TabPage contextPage ||
            !IsClosable(contextPage) || _menu is null) return false;
        _keyboardMenuPage = contextPage;
        UpdateMenuPage(contextPage);
        _menu.Show(_owner, _owner.GetTabRect(_owner.SelectedIndex).Location);
        return true;
    }

    internal void PageRemoved(TabPage page)
    {
        _closablePages.Remove(page);
        if (ReferenceEquals(_menuPage, page)) UpdateMenuPage(_owner.SelectedTab);
        if (ReferenceEquals(_keyboardMenuPage, page)) _keyboardMenuPage = null;
    }

    internal void SelectionChanged() => UpdateMenuPage(_owner.SelectedTab);

    internal void PagePresentationChanged(TabPage page)
    {
        if (ReferenceEquals(_menuPage, page)) UpdateMenuPage(page);
    }

    public void Dispose()
    {
        _closablePages.Clear();
        if (ReferenceEquals(_owner.ContextMenuStrip, _menu)) _owner.ContextMenuStrip = null;
        _menu?.Dispose();
        _menu = null;
        _menuItem = null;
        _menuPage = null;
        _keyboardMenuPage = null;
    }

    private void EnsureMenu()
    {
        if (_menu != null) return;
        _menu = new ContextMenuStrip();
        _menuItem = new ToolStripMenuItem("Close tab");
        _menuItem.Click += (_, _) =>
        {
            var page = _menuPage ?? _owner.SelectedTab;
            if (page != null) _requestClose(page, TabCloseReason.Accessibility);
        };
        _menu.Items.Add(_menuItem);
        _menu.Opening += (_, args) =>
        {
            if (_keyboardMenuPage != null)
            {
                UpdateMenuPage(_keyboardMenuPage);
                _keyboardMenuPage = null;
            }
            else
            {
                var point = _owner.PointToClient(Control.MousePosition);
                var index = HitTest(point);
                UpdateMenuPage(index >= 0 ? _owner.TabPages[index] : _owner.SelectedTab);
            }
            if (_menuPage == null || !IsClosable(_menuPage)) args.Cancel = true;
        };
        _menu.Closed += (_, _) => _keyboardMenuPage = null;
        if (_owner.ContextMenuStrip == null) _owner.ContextMenuStrip = _menu;
    }

    private int HitTest(Point point)
    {
        for (var index = 0; index < _owner.TabCount; index++)
            if (_owner.GetTabRect(index).Contains(point)) return index;
        return -1;
    }

    private void UpdateMenuPage(TabPage? page)
    {
        _menuPage = page;
        if (_menuItem == null) return;
        _menuItem.Enabled = page != null && IsClosable(page);
        _menuItem.AccessibleName = "Close tab " + (page?.Text ?? string.Empty);
    }
}
