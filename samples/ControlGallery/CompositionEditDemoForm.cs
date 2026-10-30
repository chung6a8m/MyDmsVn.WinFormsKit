using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

/// <summary>UIC-02: a local edit draft, validation and committed snapshot without product workflows.</summary>
public sealed class CompositionEditDemoForm : DemoFormBase
{
    private readonly TableLayoutPanel _layout = new TableLayoutPanel();
    private readonly Panel _editorScroll = new Panel();
    private readonly TableLayoutPanel _fields = new TableLayoutPanel();
    private readonly TableLayoutPanel _footer = new TableLayoutPanel();
    private readonly FlowLayoutPanel _commands = new FlowLayoutPanel();
    private readonly BootstrapTextBox _code = new BootstrapTextBox();
    private readonly BootstrapTextBox _name = new BootstrapTextBox();
    private readonly BootstrapComboBox _group = new BootstrapComboBox();
    private readonly BootstrapCheckBox _active = new BootstrapCheckBox();
    private readonly BootstrapTextBox _notes = new BootstrapTextBox();
    private readonly BootstrapLabel _codeError = new BootstrapLabel();
    private readonly BootstrapLabel _nameError = new BootstrapLabel();
    private readonly BootstrapLabel _validationSummary = new BootstrapLabel();
    private readonly BootstrapLabel _saveFeedback = new BootstrapLabel();
    private readonly List<BootstrapLabel> _wrappingLabels = new List<BootstrapLabel>();
    private Snapshot _committed = new Snapshot("NHOM-001", "Nhóm tham khảo tiếng Việt", 0, true, "Dữ liệu minh họa cục bộ.");
    private bool _showValidation;
    private int _saveCount;

    public CompositionEditDemoForm()
    {
        Text = "UIC-02 — Compact Edit Form";
        ClientSize = new Size(720, 620);
        _layout.Dock = DockStyle.Fill;
        _layout.Padding = new Padding(12);
        _layout.ColumnCount = 1;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _layout.RowCount = 3;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(_layout);

        var title = CreateLabel("Thông tin nhóm tham khảo — bản nháp chỉ lưu trong bộ nhớ", "EditHeading");
        title.Margin = new Padding(0, 0, 0, 12);
        _layout.Controls.Add(title, 0, 0);

        _editorScroll.Name = "EditorScroll";
        _editorScroll.Dock = DockStyle.Fill;
        _editorScroll.AutoScroll = true;
        _editorScroll.Margin = Padding.Empty;
        _fields.Dock = DockStyle.Top;
        _fields.AutoSize = true;
        _fields.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _fields.ColumnCount = 1;
        _fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _fields.Margin = Padding.Empty;
        _editorScroll.Controls.Add(_fields);
        _layout.Controls.Add(_editorScroll, 0, 1);

        ConfigureInput(_code, "CodeInput", "Mã nhận diện nhóm tham khảo");
        ConfigureInput(_name, "NameInput", "Tên hiển thị đầy đủ bằng tiếng Việt");
        _group.Name = "GroupInput";
        _group.AccessibleName = "Nhóm phân loại";
        _group.DropDownStyle = ComboBoxStyle.DropDownList;
        _group.Dock = DockStyle.Top;
        _group.Items.AddRange(new object[] { "Tham khảo chung", "Nhóm tài liệu và quy trình nội bộ" });
        _active.Name = "ActiveInput";
        _active.AccessibleName = "Đang hoạt động";
        _active.Text = "Đang hoạt động";
        ConfigureInput(_notes, "NotesInput", "Ghi chú bổ sung");
        _notes.Multiline = true;
        _notes.WordWrap = true;
        _notes.ScrollBars = ScrollBars.Vertical;
        _notes.Height = 100;

        ConfigureError(_codeError, "CodeError", "Lỗi mã nhận diện");
        ConfigureError(_nameError, "NameError", "Lỗi tên hiển thị");
        AddField("Mã nhận diện nhóm tham khảo (bắt buộc)", _code, _codeError);
        AddField("Tên hiển thị đầy đủ bằng tiếng Việt (bắt buộc)", _name, _nameError);
        AddField("Nhóm phân loại", _group);
        AddField("Trạng thái", _active);
        AddField("Ghi chú bổ sung cho nhóm tham khảo", _notes);
        ConfigureError(_validationSummary, "ValidationSummary", "Tổng hợp lỗi nhập liệu");
        AddRow(_validationSummary);

        _footer.Dock = DockStyle.Fill;
        _footer.AutoSize = true;
        _footer.ColumnCount = 1;
        _footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _footer.Margin = new Padding(0, 8, 0, 0);
        _saveFeedback.Name = "SaveFeedback";
        _saveFeedback.AccessibleName = "Kết quả lưu trong bộ nhớ";
        _saveFeedback.AutoSize = true;
        _saveFeedback.Dock = DockStyle.Top;
        _saveFeedback.Text = "Chưa lưu thay đổi.";
        _saveFeedback.Margin = new Padding(0, 0, 0, 6);
        _footer.Controls.Add(_saveFeedback);
        _wrappingLabels.Add(_saveFeedback);
        _commands.AutoSize = true;
        _commands.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _commands.Dock = DockStyle.Fill;
        _commands.WrapContents = true;
        _commands.Margin = Padding.Empty;
        var save = new BootstrapButton { Name = "SaveButton", Text = "Lưu thông tin", AccessibleName = "Lưu thông tin", AutoSize = true, TabIndex = 0 };
        var cancel = new BootstrapButton { Name = "CancelButton", Text = "Hủy thay đổi", AccessibleName = "Hủy thay đổi", AutoSize = true, Outline = true, Variant = BootstrapVariant.Secondary, TabIndex = 1 };
        save.Click += (_, _) => Save();
        cancel.Click += (_, _) => RestoreCommitted();
        _commands.Controls.Add(save);
        _commands.Controls.Add(cancel);
        _footer.Controls.Add(_commands);
        _layout.Controls.Add(_footer, 0, 2);

        _code.TextChanged += (_, _) => RefreshValidation();
        _name.TextChanged += (_, _) => RefreshValidation();
        _editorScroll.SizeChanged += (_, _) => ResizeLabels();
        _layout.SizeChanged += (_, _) => ResizeLabels();
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        RestoreCommitted();
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }

    private static void ConfigureInput(BootstrapTextBox input, string name, string caption)
    {
        input.Name = name;
        input.AccessibleName = caption;
        input.Dock = DockStyle.Top;
        input.Margin = new Padding(0, 0, 0, 4);
    }

    private BootstrapLabel CreateLabel(string text, string name)
    {
        var label = new BootstrapLabel { Name = name, Text = text, AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(0, 4, 0, 4), TabStop = false };
        _wrappingLabels.Add(label);
        return label;
    }

    private void ConfigureError(BootstrapLabel label, string name, string caption)
    {
        label.Name = name;
        label.AccessibleName = caption;
        label.AutoSize = true;
        label.Dock = DockStyle.Top;
        label.TextRole = BootstrapTextRole.Danger;
        label.Margin = new Padding(0, 0, 0, 6);
        label.Visible = false;
        _wrappingLabels.Add(label);
    }

    private void AddField(string caption, Control input, BootstrapLabel? error = null)
    {
        AddRow(CreateLabel(caption, input.Name + "Label"));
        input.TabIndex = _fields.Controls.Count;
        AddRow(input);
        if (error is not null) AddRow(error);
    }

    private void AddRow(Control control)
    {
        var row = _fields.RowCount++;
        _fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _fields.Controls.Add(control, 0, row);
    }

    private void Save()
    {
        _showValidation = true;
        RefreshValidation();
        if (string.IsNullOrWhiteSpace(_code.Text) || string.IsNullOrWhiteSpace(_name.Text))
        {
            var first = string.IsNullOrWhiteSpace(_code.Text) ? _code : _name;
            _editorScroll.ScrollControlIntoView(first);
            first.Focus();
            return;
        }
        _committed = new Snapshot(_code.Text, _name.Text, _group.SelectedIndex, _active.Checked, _notes.Text);
        _saveCount++;
        _showValidation = false;
        RefreshValidation();
        _saveFeedback.Text = $"Đã lưu trong bộ nhớ — lần {_saveCount}.";
    }

    private void RestoreCommitted()
    {
        _showValidation = false;
        _code.Text = _committed.Code;
        _name.Text = _committed.Name;
        _group.SelectedIndex = _committed.Group;
        _active.Checked = _committed.Active;
        _notes.Text = _committed.Notes;
        RefreshValidation();
    }

    private void RefreshValidation()
    {
        var codeInvalid = _showValidation && string.IsNullOrWhiteSpace(_code.Text);
        var nameInvalid = _showValidation && string.IsNullOrWhiteSpace(_name.Text);
        _code.ValidationState = codeInvalid ? BootstrapValidationState.Invalid : BootstrapValidationState.None;
        _name.ValidationState = nameInvalid ? BootstrapValidationState.Invalid : BootstrapValidationState.None;
        _codeError.Text = codeInvalid ? "Vui lòng nhập mã nhận diện để phân biệt nhóm tham khảo với những nhóm khác." : string.Empty;
        _nameError.Text = nameInvalid ? "Vui lòng nhập tên hiển thị đầy đủ để người dùng nhận biết đúng nhóm tham khảo." : string.Empty;
        _codeError.Visible = codeInvalid;
        _nameError.Visible = nameInvalid;
        _validationSummary.Text = codeInvalid && nameInvalid ? "Chưa thể lưu: cần nhập mã nhận diện và tên hiển thị." :
            codeInvalid ? "Chưa thể lưu: cần nhập mã nhận diện." : nameInvalid ? "Chưa thể lưu: cần nhập tên hiển thị." : string.Empty;
        _validationSummary.Visible = codeInvalid || nameInvalid;
    }

    private void ResizeLabels()
    {
        var fieldWidth = Math.Max(1, _editorScroll.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
        foreach (var label in _wrappingLabels)
        {
            var width = label.Parent == _fields ? fieldWidth : Math.Max(1, _layout.ClientSize.Width - _layout.Padding.Horizontal);
            label.MaximumSize = new Size(width, 0);
        }
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e) => ApplyTheme(e.NewTheme);

    private void ApplyTheme(BootstrapTheme theme)
    {
        foreach (var surface in new Control[] { this, _layout, _editorScroll, _fields, _footer, _commands })
        {
            surface.BackColor = theme.Colors.Body;
            surface.ForeColor = theme.Colors.Text;
        }
        ResizeLabels();
    }

    private sealed class Snapshot
    {
        public Snapshot(string code, string name, int group, bool active, string notes)
        {
            Code = code;
            Name = name;
            Group = group;
            Active = active;
            Notes = notes;
        }
        public string Code { get; }
        public string Name { get; }
        public int Group { get; }
        public bool Active { get; }
        public string Notes { get; }
    }
}
