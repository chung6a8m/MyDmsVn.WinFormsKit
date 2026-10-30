using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

/// <summary>A consumer-readable, local-only filter/results composition (UIC-01).</summary>
public sealed class CompositionListDemoForm : DemoFormBase
{
    private readonly TableLayoutPanel _layout = new TableLayoutPanel();
    private readonly FlowLayoutPanel _scenarioBar = new FlowLayoutPanel();
    private readonly FlowLayoutPanel _filters = new FlowLayoutPanel();
    private readonly Label _scenarioLabel = new Label();
    private readonly Label _title = new Label();
    private readonly Label _summary = new Label();
    private readonly BootstrapTextBox _search = new BootstrapTextBox();
    private readonly BootstrapComboBox _status = new BootstrapComboBox();
    private readonly BootstrapComboBox _scenario = new BootstrapComboBox();
    private readonly BootstrapButton _refresh = new BootstrapButton();
    private readonly BootstrapButton _clear = new BootstrapButton();
    private readonly BootstrapDataGridView _grid = new BootstrapDataGridView();
    private DataTable? _visibleRows;
    private bool _clearing;

    public CompositionListDemoForm()
    {
        Text = "UIC-01 — Filter Results";
        ClientSize = new Size(900, 600);
        _layout.Dock = DockStyle.Fill;
        _layout.Padding = new Padding(12);
        _layout.ColumnCount = 1;
        _layout.RowCount = 5;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < 4; row++) _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(_layout);

        ConfigureFlow(_scenarioBar);
        _scenarioLabel.Text = "Kịch bản demo (ngoài mẫu):";
        _scenarioLabel.AutoSize = true;
        _scenarioLabel.Margin = new Padding(0, 7, 8, 6);
        _scenario.Name = "ScenarioSelector";
        _scenario.AccessibleName = "Kịch bản dữ liệu minh họa";
        _scenario.DropDownStyle = ComboBoxStyle.DropDownList;
        _scenario.Width = 210;
        _scenario.Items.AddRange(new object[] { "Đã tải", "Dataset rỗng", "Đang tải", "Không khả dụng" });
        _scenario.SelectedIndex = 0;
        _scenarioBar.Controls.Add(_scenarioLabel);
        _scenarioBar.Controls.Add(_scenario);

        _title.Text = "Danh sách tham chiếu — tìm kiếm và lọc trạng thái";
        _title.AutoSize = true;
        _title.Dock = DockStyle.Fill;
        _title.Margin = new Padding(0, 8, 0, 8);
        ConfigureFlow(_filters);
        _search.Name = "SearchInput";
        _search.AccessibleName = "Tìm theo mã, tên hoặc nhóm";
        _search.PlaceholderText = "Tìm mã, tên hoặc nhóm…";
        _search.Width = 210;
        _search.TabIndex = 0;
        _status.Name = "StatusFilter";
        _status.AccessibleName = "Lọc trạng thái";
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Width = 190;
        _status.TabIndex = 1;
        _status.Items.AddRange(new object[] { "Tất cả trạng thái", "Đang hoạt động", "Ngừng hoạt động" });
        _status.SelectedIndex = 0;
        ConfigureButton(_refresh, "RefreshButton", "Tải lại", 2);
        ConfigureButton(_clear, "ClearFiltersButton", "Xóa bộ lọc", 3);
        _filters.Controls.Add(_search);
        _filters.Controls.Add(_status);
        _filters.Controls.Add(_refresh);
        _filters.Controls.Add(_clear);
        _summary.Name = "ResultSummary";
        _summary.AccessibleName = "Trạng thái kết quả truy vấn";
        _summary.AutoSize = true;
        _summary.Dock = DockStyle.Fill;
        _summary.Margin = new Padding(0, 6, 0, 8);

        _grid.Name = "ResultsGrid";
        _grid.AccessibleName = "Kết quả tham chiếu chỉ đọc";
        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        AddColumn("Code", "Mã tham chiếu", 210);
        AddColumn("Name", "Tên nhận diện", 280);
        AddColumn("Group", "Nhóm", 170);
        AddColumn("Status", "Trạng thái", 160);
        _layout.Controls.Add(_scenarioBar, 0, 0);
        _layout.Controls.Add(_title, 0, 1);
        _layout.Controls.Add(_filters, 0, 2);
        _layout.Controls.Add(_summary, 0, 3);
        _layout.Controls.Add(_grid, 0, 4);

        _search.TextChanged += OnQueryChanged;
        _search.KeyDown += OnSearchKeyDown;
        _status.SelectedIndexChanged += OnQueryChanged;
        _scenario.SelectedIndexChanged += OnQueryChanged;
        _refresh.Click += OnQueryChanged;
        _clear.Click += OnClearFilters;
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
        ApplyQuery();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
            _grid.DataSource = null;
            _visibleRows?.Dispose();
            _visibleRows = null;
        }
        base.Dispose(disposing);
    }

    private static void ConfigureFlow(FlowLayoutPanel panel)
    {
        panel.AutoSize = true;
        panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        panel.Dock = DockStyle.Fill;
        panel.WrapContents = true;
        panel.Margin = Padding.Empty;
    }

    private static void ConfigureButton(BootstrapButton button, string name, string text, int tabIndex)
    {
        button.Name = name;
        button.Text = text;
        button.AccessibleName = text;
        button.AutoSize = true;
        button.TabIndex = tabIndex;
    }

    private void AddColumn(string property, string caption, int width)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = property + "Column", DataPropertyName = property, HeaderText = caption,
            Width = width, SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private void OnQueryChanged(object? sender, EventArgs e)
    {
        if (!_clearing) ApplyQuery();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;
        ApplyQuery();
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private void OnClearFilters(object? sender, EventArgs e)
    {
        _clearing = true;
        try
        {
            _search.Clear();
            _status.SelectedIndex = 0;
        }
        finally { _clearing = false; }
        ApplyQuery();
        _search.Focus();
    }

    private void ApplyQuery()
    {
        var selectedCode = _grid.CurrentRow?.Cells[0].Value as string;
        var rows = new DataTable("References") { Locale = CultureInfo.InvariantCulture };
        foreach (var name in new[] { "Code", "Name", "Group", "Status" }) rows.Columns.Add(name, typeof(string));
        _grid.Loading = _scenario.SelectedIndex == 2;
        if (_scenario.SelectedIndex == 2)
        {
            _grid.LoadingText = "Đang tải dữ liệu tham chiếu…";
            _summary.Text = "Đang tải — chưa biết số kết quả.";
        }
        else if (_scenario.SelectedIndex == 3)
        {
            _grid.EmptyStateText = "Không thể tải dữ liệu. Chọn kịch bản Đã tải để thử lại.";
            _summary.Text = "Không khả dụng — chưa biết số kết quả.";
        }
        else
        {
            var dataset = _scenario.SelectedIndex == 1 ? Array.Empty<string[]>() : SampleRows;
            var query = _search.Text.Trim(); // Read a normalized query; never rewrite the native edit buffer.
            foreach (var row in dataset)
            {
                var matchesText = query.Length == 0 || string.Join(" ", row).IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
                var matchesStatus = _status.SelectedIndex == 0 || row[3] == (_status.SelectedIndex == 1 ? "Đang hoạt động" : "Ngừng hoạt động");
                if (matchesText && matchesStatus) rows.Rows.Add(row);
            }
            _grid.EmptyStateText = dataset.Length == 0 ? "Chưa có dữ liệu tham chiếu." : "Không có kết quả phù hợp bộ lọc.";
            _summary.Text = $"{rows.Rows.Count} / {dataset.Length} kết quả";
        }
        var prior = _visibleRows;
        _visibleRows = rows;
        _grid.DataSource = rows;
        prior?.Dispose();
        if (selectedCode is not null)
        {
            foreach (DataGridViewRow row in _grid.Rows)
                if (Equals(row.Cells[0].Value, selectedCode)) { _grid.CurrentCell = row.Cells[0]; break; }
        }
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e) => ApplyTheme(e.NewTheme);

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        foreach (var panel in new Control[] { _layout, _scenarioBar, _filters })
        {
            panel.BackColor = theme.Colors.Body;
            panel.ForeColor = theme.Colors.Text;
        }
        foreach (var label in new[] { _title, _scenarioLabel, _summary })
        {
            label.BackColor = theme.Colors.Body;
            label.ForeColor = label == _title ? theme.Colors.Text : theme.Colors.MutedText;
        }
        DemoDataGridRowMetrics.Apply(_grid, theme);
    }

    private static readonly string[][] SampleRows =
    {
        new[] { "REF-KHO-MIEN-NAM-2026-000001", "Kho trung chuyển và phân phối khu vực phía Nam", "Vận hành kho", "Đang hoạt động" },
        new[] { "REF-VAT-TU-2026-000002", "Vật tư và thiết bị phục vụ kiểm kê kho", "Kho vật tư", "Đang hoạt động" },
        new[] { "REF-VAN-PHONG-2026-000003", "Văn phòng điều phối với tên nhận diện tiếng Việt dài", "Hành chính", "Ngừng hoạt động" },
        new[] { "REF-LUU-TRU-2026-000004", "Kho lưu trữ hồ sơ và chứng từ tham chiếu", "Lưu trữ", "Ngừng hoạt động" }
    };
}
