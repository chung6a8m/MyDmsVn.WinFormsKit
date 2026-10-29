using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

/// <summary>Demonstrates caller-owned admission and live native busy composition.</summary>
public sealed class BusyOverlayDemoForm : DemoFormBase
{
    private readonly Timer _clock = new Timer { Interval = 100 };
    private readonly List<Operation> _operations = new List<Operation>();
    private readonly Panel _liveContent = CreateContent("Live");
    private readonly Panel _legacyContent = CreateContent("Opaque");
    private readonly BootstrapBusyOverlayHost _host;
    private readonly BootstrapBusyHost _legacy;
    private readonly Label _diagnostics = new Label { AutoSize = true, AccessibleName = "Busy diagnostics" };
    private readonly Label _status = new Label { AutoSize = true, Text = "Sẵn sàng", AccessibleName = "Busy operation status" };
    private int _tick;

    public BusyOverlayDemoForm()
    {
        Text = "Loading / Busy Overlay";
        ClientSize = new Size(1050, 650);
        _host = new BootstrapBusyOverlayHost(_liveContent) { Dock = DockStyle.Fill };
        _legacy = new BootstrapBusyHost(_legacyContent) { Dock = DockStyle.Fill };
        var tools = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(8) };
        var slider = new TrackBar { Minimum = 0, Maximum = 100, Value = 35, TickFrequency = 10, Width = 180, AccessibleName = "Busy opacity" };
        slider.ValueChanged += (_, _) => { _host.BackdropOpacity = slider.Value / 100d; UpdateDiagnostics(); };
        tools.Controls.Add(new Label { Text = "Độ phủ 0–100%", AutoSize = true });
        tools.Controls.Add(slider);
        AddButton(tools, "Nhanh", "Busy fast", () => StartOperation(1, "Đang kiểm tra…"));
        AddButton(tools, "Tác vụ dài", "Busy long", () => StartOperation(100, "Đang cập nhật danh sách…"));
        AddButton(tools, "Lồng nhau: cũ xong trước", "Busy nested old first", () => { StartOperation(20, "Đang tải danh sách…"); StartOperation(45, "Đang kiểm tra dữ liệu…"); });
        AddButton(tools, "Lồng nhau: mới xong trước", "Busy nested new first", () => { StartOperation(45, "Đang tải danh sách…"); StartOperation(20, "Đang kiểm tra dữ liệu…"); });
        AddButton(tools, "Hủy tác vụ", "Busy cancel", CancelAll);
        AddButton(tools, "Ẩn / hiện vùng", "Busy visibility", () => _host.Visible = !_host.Visible);
        var columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        columns.Controls.Add(_host, 0, 0);
        columns.Controls.Add(_legacy, 1, 0);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.TopDown, Padding = new Padding(8) };
        footer.Controls.Add(_status);
        footer.Controls.Add(_diagnostics);
        footer.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(950, 0), Text = "Theme dùng bộ chọn của gallery. Caller đóng popup trước Begin và chặn lệnh keyboard/UIA ngay cả khi đang chờ 150 ms. Counter vẫn đổi dưới lớp phủ; host cũ bên phải là vùng nội dung riêng. SourceGrid cần nghiệm thu riêng trong consumer có adapter." });
        Controls.Add(columns);
        Controls.Add(tools);
        Controls.Add(footer);
        _host.RenderModeChanged += OnRenderModeChanged;
        _clock.Tick += OnTick;
        VisibleChanged += OnDemoVisibleChanged;
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
        UpdateDiagnostics();
    }

    private static Panel CreateContent(string caption)
    {
        var panel = new Panel { Padding = new Padding(12), AccessibleName = caption + " content" };
        var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true };
        list.Columns.Add("Dữ liệu " + caption, 200);
        list.Columns.Add("Counter", 100);
        for (var i = 0; i < 12; i++) list.Items.Add(new ListViewItem(new[] { "Mục " + (i + 1), "0" }));
        var editor = new TextBox { Dock = DockStyle.Top, Text = "Native editor " + caption };
        var choices = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
        choices.Items.AddRange(new object[] { "Lựa chọn A", "Lựa chọn B" });
        choices.SelectedIndex = 0;
        var progress = new ProgressBar { Dock = DockStyle.Bottom, Height = 24 };
        var counter = new Label { Dock = DockStyle.Bottom, Height = 30, Text = "Counter 0", AccessibleName = caption + " counter" };
        panel.Controls.Add(list); panel.Controls.Add(editor); panel.Controls.Add(choices); panel.Controls.Add(counter); panel.Controls.Add(progress);
        return panel;
    }
    private static void AddButton(Control parent, string text, string name, Action action)
    {
        var button = new Button { AutoSize = true, Text = text, AccessibleName = name };
        button.Click += (_, _) => action();
        parent.Controls.Add(button);
    }
    private void StartOperation(int duration, string message)
    {
        // Transient native popups belong to the caller, outside the child overlay tree.
        foreach (var content in new[] { _liveContent, _legacyContent })
            foreach (Control control in content.Controls)
                if (control is ComboBox combo) combo.DroppedDown = false;
        var operation = new Operation { Deadline = _tick + duration };
        operation.Live = _host.Begin(new BusyPresentation(message, () => Finish(operation, true)));
        operation.Legacy = _legacy.Begin(new BusyPresentation(message, () => Finish(operation, true)));
        _operations.Add(operation);
        SetAdmission(false);
        _status.Text = "Đang chạy: " + _operations.Count;
    }
    private void Finish(Operation operation, bool cancelled)
    {
        if (!_operations.Remove(operation)) return;
        operation.Live?.Dispose(); operation.Legacy?.Dispose();
        SetAdmission(_operations.Count == 0);
        _status.Text = cancelled ? "Đã hủy; còn " + _operations.Count : "Đã hoàn tất; còn " + _operations.Count;
    }
    private void CancelAll()
    {
        foreach (var operation in _operations.ToArray()) Finish(operation, true);
    }
    private void SetAdmission(bool admitted)
    {
        // Disable native editors as the application's keyboard/UIA admission policy;
        // periodic presentation data below keeps updating while busy.
        foreach (var content in new[] { _liveContent, _legacyContent })
            foreach (Control control in content.Controls)
                if (control is TextBox || control is ComboBox || control is ListView) control.Enabled = admitted;
    }
    private void OnTick(object? sender, EventArgs e)
    {
        _tick++;
        foreach (var content in new[] { _liveContent, _legacyContent })
            foreach (Control control in content.Controls)
            {
                if (control is ProgressBar progress) progress.Value = _tick % 101;
                if (control is Label label) label.Text = "Counter " + _tick;
                if (control is ListView list) list.Items[0].SubItems[1].Text = _tick.ToString();
            }
        foreach (var operation in _operations.ToArray()) if (_tick >= operation.Deadline) Finish(operation, false);
        UpdateDiagnostics();
    }
    private void OnDemoVisibleChanged(object? sender, EventArgs e) { if (Visible) _clock.Start(); else _clock.Stop(); }
    private void OnRenderModeChanged(object? sender, EventArgs e) => UpdateDiagnostics();
    private void UpdateDiagnostics() => _diagnostics.Text = "RenderMode: " + _host.RenderMode + " | DPI: " + DeviceDpi + " | Alpha: " + _host.BackdropOpacity.ToString("P0") + (_host.RenderMode == BootstrapBusyOverlayRenderMode.OpaqueFallback ? " | Fallback opaque: kiểm tra capability/manifest" : "");
    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e) => ApplyTheme();
    private void ApplyTheme()
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        BackColor = theme.Colors.Body; ForeColor = theme.Colors.Text;
        _liveContent.BackColor = _legacyContent.BackColor = theme.Colors.Surface;
        _liveContent.ForeColor = _legacyContent.ForeColor = theme.Colors.Text;
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _clock.Stop(); _clock.Tick -= OnTick; _clock.Dispose();
            VisibleChanged -= OnDemoVisibleChanged;
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
            _host.RenderModeChanged -= OnRenderModeChanged;
            CancelAll();
        }
        base.Dispose(disposing);
    }
    private sealed class Operation
    {
        internal int Deadline;
        internal IDisposable? Live, Legacy;
    }
}
