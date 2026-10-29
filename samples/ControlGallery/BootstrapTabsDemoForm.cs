using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

/// <summary>Compares the existing native tab renderer with BootstrapTabs and its compact chrome.</summary>
public sealed class BootstrapTabsDemoForm : DemoFormBase
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly BootstrapTabControl _legacy = new BootstrapTabControl();
    private readonly BootstrapTabs _tabs = new BootstrapTabs();
    private readonly ImageList _images = new ImageList();
    private readonly Bitmap _infoImage = SystemIcons.Information.ToBitmap();
    private readonly Label _status = new Label();
    private int _nextPage = 1;

    public BootstrapTabsDemoForm()
    {
        Text = "BootstrapTabs";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1050, 760);
        MinimumSize = new Size(680, 480);

        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.FlowDirection = FlowDirection.TopDown;
        _content.WrapContents = false;
        _content.Padding = new Padding(16);
        Controls.Add(_content);

        _content.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "So sánh BootstrapTabControl và BootstrapTabs trên cùng trang native. Dùng chuột, Ctrl+Tab và Tab/Shift+Tab để kiểm tra focus.",
            Margin = new Padding(0, 0, 0, 10)
        });

        _images.ImageSize = new Size(16, 16);
        _images.Images.Add("info", _infoImage);
        ConfigureTabs(_legacy);
        ConfigureTabs(_tabs);
        _legacy.TabStyle = BootstrapTabStyle.Underline;
        _legacy.HotTrack = true;
        _tabs.HotTrack = true;
        _tabs.ImageList = _images;
        _tabs.ShowToolTips = true;

        _status.AutoSize = true;
        _status.MaximumSize = new Size(970, 0);
        _status.AccessibleName = "BootstrapTabs status";
        _status.Text = "Chọn trang, thêm tab hoặc gửi yêu cầu đóng để xem vòng đời native.";
        _status.Margin = new Padding(0, 0, 0, 12);
        _content.Controls.Add(_status);

        _content.Controls.Add(CreateControls());
        _content.Controls.Add(CreateSection("BootstrapTabControl hiện có", _legacy));
        _content.Controls.Add(CreateSection("BootstrapTabs: chrome gọn", _tabs));

        _legacy.TabPages.Add(CreatePage("Tổng quan", "Control cũ giữ painter và native navigation.", false));
        _legacy.TabPages.Add(CreatePage("Chi tiết", "Di chuyển chuột qua header để so sánh HotTrack.", false));
        _legacy.TabPages.Add(CreatePage("Cài đặt", "Dùng Ctrl+Tab để chuyển trang.", false));

        _tabs.TabPages.Add(CreatePage("Cố định", "Trang này không có nút đóng; nhập liệu vẫn dùng native focus.", true));
        var closable = CreatePage("Đóng được", "Nút đóng phát request; demo tự xóa trang.", true);
        closable.ToolTipText = "Trang có thể đóng";
        _tabs.TabPages.Add(closable);
        _tabs.SetTabClosable(closable, true);
        var longPage = CreatePage("Báo cáo tổng hợp rất dài bằng tiếng Việt để kiểm tra dấu và dấu ba chấm", "Tiêu đề dài dùng native overflow và tooltip.", true);
        longPage.ImageKey = "info";
        longPage.ToolTipText = longPage.Text;
        _tabs.TabPages.Add(longPage);

        _legacy.SelectedIndexChanged += (_, _) => ReportSelection("Control cũ", _legacy);
        _tabs.SelectedIndexChanged += (_, _) => ReportSelection("BootstrapTabs", _tabs);
        _tabs.TabCloseRequested += (_, args) =>
        {
            var title = args.Page.Text;
            _tabs.TabPages.Remove(args.Page);
            args.Page.Dispose();
            _status.Text = $"Đã xử lý yêu cầu đóng '{title}' ({args.Reason}).";
        };
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _images.Dispose();
            _infoImage.Dispose();
        }
    }

    private static void ConfigureTabs(TabControl tabs)
    {
        tabs.Size = new Size(970, 215);
        tabs.Margin = new Padding(0, 4, 0, 14);
    }

    private Control CreateControls()
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Width = 980,
            Margin = new Padding(0, 0, 0, 12)
        };

        panel.Controls.Add(CreateToggle("Legacy tabs HotTrack", "HotTrack control cũ", true, value => _legacy.HotTrack = value));
        panel.Controls.Add(CreateToggle("BootstrapTabs HotTrack", "HotTrack BootstrapTabs", true, value => _tabs.HotTrack = value));
        panel.Controls.Add(CreateToggle("BootstrapTabs fill", "Fill", false, value => _tabs.Fill = value));
        panel.Controls.Add(CreateSelector("BootstrapTabs style", Enum.GetValues(typeof(BootstrapTabStyle)).Cast<object>().ToArray(), _tabs.TabStyle,
            value => _tabs.TabStyle = (BootstrapTabStyle)value));
        panel.Controls.Add(CreateSelector("BootstrapTabs variant", Enum.GetValues(typeof(BootstrapVariant)).Cast<object>().ToArray(), _tabs.Variant,
            value => _tabs.Variant = (BootstrapVariant)value));
        panel.Controls.Add(CreateButton("Add BootstrapTabs page", "Thêm trang", () => AddPage()));
        panel.Controls.Add(CreateButton("Remove BootstrapTabs page", "Xóa trang cuối", RemoveLastPage));
        panel.Controls.Add(CreateButton("Add BootstrapTabs overflow pages", "Thêm nhiều tab", () =>
        {
            for (var index = 0; index < 8; index++) AddPage();
        }));
        panel.Controls.Add(CreateButton("Close BootstrapTabs page", "Yêu cầu đóng", () =>
        {
            var page = _tabs.TabPages.Cast<TabPage>().FirstOrDefault(_tabs.IsTabClosable);
            _status.Text = page is null || !_tabs.RequestTabClose(page)
                ? "Không có trang nào được phép đóng."
                : _status.Text;
        }));
        panel.Controls.Add(CreateButton("Clear BootstrapTabs pages", "Xóa tất cả", () =>
        {
            while (_tabs.TabCount > 0) RemoveLastPage();
            _status.Text = "Không còn trang: kiểm tra empty state.";
        }));
        return panel;
    }

    private static CheckBox CreateToggle(string accessibleName, string text, bool initial, Action<bool> changed)
    {
        var toggle = new CheckBox { AutoSize = true, Text = text, Checked = initial, AccessibleName = accessibleName, Margin = new Padding(0, 5, 14, 5) };
        toggle.CheckedChanged += (_, _) => changed(toggle.Checked);
        return toggle;
    }

    private static ComboBox CreateSelector(string accessibleName, object[] values, object initial, Action<object> changed)
    {
        var selector = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            AccessibleName = accessibleName,
            Width = 135,
            Margin = new Padding(0, 2, 10, 2)
        };
        selector.Items.AddRange(values);
        selector.SelectedItem = initial;
        selector.SelectedIndexChanged += (_, _) =>
        {
            if (selector.SelectedItem is not null) changed(selector.SelectedItem);
        };
        return selector;
    }

    private static Button CreateButton(string accessibleName, string text, Action action)
    {
        var button = new Button { AutoSize = true, Text = text, AccessibleName = accessibleName, Margin = new Padding(0, 2, 8, 2) };
        button.Click += (_, _) => action();
        return button;
    }

    private static Control CreateSection(string title, TabControl tabs)
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = Padding.Empty
        };
        panel.Controls.Add(new Label { AutoSize = true, Text = title, Margin = new Padding(0, 0, 0, 4) });
        panel.Controls.Add(tabs);
        return panel;
    }

    private static TabPage CreatePage(string title, string description, bool focusable)
    {
        var page = new TabPage(title);
        page.Controls.Add(new Label { AutoSize = true, Text = description, Location = new Point(12, 12) });
        if (focusable)
        {
            page.Controls.Add(new TextBox { AccessibleName = title + " input", Width = 240, Location = new Point(12, 44), Text = "Nhập nội dung" });
            page.Controls.Add(new Button { AccessibleName = title + " action", AutoSize = true, Location = new Point(264, 42), Text = "Thực hiện" });
        }
        return page;
    }

    private void AddPage()
    {
        var page = CreatePage("Trang mới " + _nextPage++, "Thêm trang để thử native overflow và keyboard.", true);
        page.ToolTipText = page.Text;
        _tabs.TabPages.Add(page);
        _tabs.SetTabClosable(page, true);
        _status.Text = "Đã thêm '" + page.Text + "'.";
    }

    private void RemoveLastPage()
    {
        if (_tabs.TabCount == 0)
        {
            _status.Text = "Không còn trang để xóa.";
            return;
        }
        var page = _tabs.TabPages[_tabs.TabCount - 1];
        var title = page.Text;
        _tabs.TabPages.Remove(page);
        page.Dispose();
        _status.Text = "Đã xóa '" + title + "'.";
    }

    private void ReportSelection(string source, TabControl tabs)
    {
        _status.Text = $"{source}: SelectedIndexChanged = {tabs.SelectedIndex}, '{tabs.SelectedTab?.Text ?? "(trống)"}'.";
    }
}
