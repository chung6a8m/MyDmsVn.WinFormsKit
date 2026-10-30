using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using MyDmsVn.BootstrapSourceGrid.Editors;
using GridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Demo;

/// <summary>Caller-owned SourceGrid admission with the public live overlay API.</summary>
public sealed class BusyOverlayDemoForm : Form
{
    private readonly Timer _clock = new Timer { Interval = 100 };
    private readonly List<Operation> _operations = new List<Operation>();
    private readonly Label _diagnostics = new Label { Dock = DockStyle.Bottom, Height = 45, Name = "busyDiagnostics" };
    private readonly Button _apply = new Button { Dock = DockStyle.Bottom, Height = 32, Text = "Áp dụng (Ctrl+S)", Name = "busyApply" };
    private int _tick;
    internal GridControl Grid { get; } = new GridControl { Dock = DockStyle.Fill, Name = "busyGrid" };
    internal BootstrapBusyOverlayHost Host { get; }
    internal TrackBar OpacitySlider { get; } = new TrackBar { Minimum = 0, Maximum = 100, Value = 35, Width = 150, AccessibleName = "Độ phủ" };
    internal ProgressBar Progress { get; } = new ProgressBar { Dock = DockStyle.Bottom, Height = 24 };
    internal BootstrapTextBoxEditor TextEditor { get; }
    internal SourceGrid.Cells.Editors.ComboBox ChoiceEditor { get; }
    internal int OperationCount => _operations.Count;
    internal int AppliedCount { get; private set; }

    public BusyOverlayDemoForm()
    {
        Text = "SourceGrid / Live Busy Overlay";
        ClientSize = new Size(1080, 720);
        StartPosition = FormStartPosition.CenterScreen;
        TextEditor = Grid.BootstrapEditors.CreateTextBox(typeof(string));
        ChoiceEditor = new SourceGrid.Cells.Editors.ComboBox(typeof(string), new[] { "A", "B", "C" }, true);
        Grid.Redim(40, 3); Grid.FixedRows = 1;
        Grid[0, 0] = new SourceGrid.Cells.ColumnHeader("Live marker");
        Grid[0, 1] = new SourceGrid.Cells.ColumnHeader("Bootstrap editor");
        Grid[0, 2] = new SourceGrid.Cells.ColumnHeader("Native ComboBox");
        for (var row = 1; row < 40; row++)
        {
            Grid[row, 0] = new SourceGrid.Cells.Cell("Tick 0") { Editor = null };
            Grid[row, 1] = new SourceGrid.Cells.Cell("Mục " + row) { Editor = TextEditor };
            Grid[row, 2] = new SourceGrid.Cells.Cell("A") { Editor = ChoiceEditor };
            Grid.Rows[row].Height = 34;
        }
        Grid.Rows[0].Height = 34;
        Grid.Columns[0].Width = 260; Grid.Columns[1].Width = 340; Grid.Columns[2].Width = 300;
        var content = new Panel();
        content.Controls.Add(Grid); content.Controls.Add(Progress); content.Controls.Add(_apply);
        Host = new BootstrapBusyOverlayHost(content) { Dock = DockStyle.Fill, Name = "sourceGridBusyHost" };
        var tools = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 112, Padding = new Padding(6), WrapContents = true };
        tools.Controls.Add(new Label { Text = "Độ phủ", AutoSize = true }); tools.Controls.Add(OpacitySlider);
        OpacitySlider.ValueChanged += (_, _) => { Host.BackdropOpacity = OpacitySlider.Value / 100d; UpdateDiagnostics(); };
        Add(tools, "Tác vụ dài", "busyLong", () => StartOperation(100, "Đang cập nhật dữ liệu…"));
        Add(tools, "Nhanh", "busyFast", () => StartOperation(1, "Đang kiểm tra…"));
        Add(tools, "Cũ xong trước", "busyOldFirst", () => { StartOperation(20, "Tải dữ liệu…"); StartOperation(45, "Kiểm tra…"); });
        Add(tools, "Mới xong trước", "busyNewFirst", () => { StartOperation(45, "Tải dữ liệu…"); StartOperation(20, "Kiểm tra…"); });
        Add(tools, "Hủy", "busyCancel", CancelAll);
        Add(tools, "Ẩn / hiện", "busyVisibility", () => Host.Visible = !Host.Visible);
        Add(tools, "Light", "busyLight", () => SetTheme(BootstrapThemeMode.Light));
        Add(tools, "Dark", "busyDark", () => SetTheme(BootstrapThemeMode.Dark));
        Controls.Add(Host); Controls.Add(tools); Controls.Add(_diagnostics);
        _apply.Click += (_, _) => TryApply();
        _clock.Tick += OnTick;
        Host.RenderModeChanged += OnModeChanged;
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(); UpdateDiagnostics();
    }

    private static void Add(Control parent, string text, string name, Action action)
    {
        var button = new Button { Text = text, Name = name, AutoSize = true };
        button.Click += (_, _) => action(); parent.Controls.Add(button);
    }
    internal bool StartOperation(int ticks, string message)
    {
        // The caller owns top-level popups and edit policy. Cancel before disabling
        // so SourceGrid does not commit an in-flight candidate on focus validation.
        ChoiceEditor.Control.DroppedDown = false;
        var position = Grid.Selection.ActivePosition;
        if (!position.IsEmpty())
        {
            var context = new SourceGrid.CellContext(Grid, position, Grid[position]);
            if (context.IsEditing() && !context.EndEdit(true)) return false;
        }
        var operation = new Operation { Deadline = _tick + ticks };
        _operations.Add(operation);
        SetAdmission(false);
        try { operation.Token = Host.Begin(new BusyPresentation(message, () => Finish(operation))); }
        catch { _operations.Remove(operation); SetAdmission(_operations.Count == 0); throw; }
        UpdateDiagnostics(); return true;
    }
    internal void CancelNewest() { if (_operations.Count != 0) Finish(_operations[_operations.Count - 1]); }
    internal void CancelAll() { foreach (var operation in _operations.ToArray()) Finish(operation); }
    private void Finish(Operation operation)
    {
        if (!_operations.Remove(operation)) return;
        operation.Token?.Dispose(); SetAdmission(_operations.Count == 0); UpdateDiagnostics();
    }
    private void SetAdmission(bool admitted) { Grid.Enabled = admitted; _apply.Enabled = admitted; }
    internal bool TryApply()
    {
        // Business/keyboard/UIA dispatch must use this guard even while presentation is pending.
        if (_operations.Count != 0) return false;
        AppliedCount++; UpdateDiagnostics(); return true;
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.S)) { TryApply(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    internal void AdvanceContent()
    {
        _tick++;
        Grid[1, 0].Value = "Live " + (_tick % 2 == 0 ? "22222222 " : "88888888 ") + _tick;
        Progress.Value = _tick % 101;
        foreach (var operation in _operations.ToArray()) if (_tick >= operation.Deadline) Finish(operation);
        UpdateDiagnostics();
    }
    private void OnTick(object? sender, EventArgs e) => AdvanceContent();
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible) _clock.Start(); else _clock.Stop();
    }
    internal void StopClock() => _clock.Stop();
    private static void SetTheme(BootstrapThemeMode mode) => BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
    private void OnModeChanged(object? sender, EventArgs e) => UpdateDiagnostics();
    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e) => ApplyTheme();
    private void ApplyTheme()
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        BackColor = Host.Content.BackColor = colors.Surface; ForeColor = colors.Text;
        _diagnostics.BackColor = colors.Surface; _diagnostics.ForeColor = colors.Text;
    }
    private void UpdateDiagnostics() => _diagnostics.Text = "RenderMode: " + Host.RenderMode + " | DPI: " + DeviceDpi + " | Alpha: " + Host.BackdropOpacity.ToString("P0") + " | Tác vụ: " + _operations.Count + " | Áp dụng: " + AppliedCount + "\nCaller hủy editor/popup trước Begin; khóa grid và lệnh từ pending. Live marker vẫn đổi. OpaqueFallback cần kiểm tra manifest/capability.";
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _clock.Stop(); _clock.Tick -= OnTick; _clock.Dispose();
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
            Host.RenderModeChanged -= OnModeChanged;
            CancelAll();
            // Unlike registry-owned Bootstrap editors, this native editor is caller-owned.
            ChoiceEditor.Control.Dispose();
            ChoiceEditor.Dispose();
        }
        base.Dispose(disposing);
    }
    private sealed class Operation { internal int Deadline; internal IDisposable? Token; }
}
