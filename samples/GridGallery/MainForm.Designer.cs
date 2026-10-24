using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Demo;

partial class MainForm
{
    private IContainer _components = null!;
    private Button _lightButton = null!;
    private Button _darkButton = null!;
    private Button _resetButton = null!;
    private Button _consumerFontButton = null!;
    private Label _baseFontLabel = null!;
    private ComboBox _baseFontComboBox = null!;
    private CheckBox _reducedMotionCheckBox = null!;
    private FlowLayoutPanel _toolbar = null!;
    private Label _diagnostics = null!;
    private Label _interactionHelp = null!;
    private BootstrapSourceGridControl _grid = null!;
    private TableLayoutPanel _layout = null!;

    private void InitializeComponent()
    {
        _components = new Container();
        _lightButton = new Button();
        _darkButton = new Button();
        _resetButton = new Button();
        _consumerFontButton = new Button();
        _baseFontLabel = new Label();
        _baseFontComboBox = new ComboBox();
        _reducedMotionCheckBox = new CheckBox();
        _toolbar = new FlowLayoutPanel();
        _diagnostics = new Label();
        _interactionHelp = new Label();
        _grid = new BootstrapSourceGridControl();
        _layout = new TableLayoutPanel();
        _toolbar.SuspendLayout();
        _layout.SuspendLayout();
        SuspendLayout();

        _lightButton.AutoSize = true;
        _lightButton.Name = "lightThemeButton";
        _lightButton.Text = "Light theme";
        _lightButton.UseVisualStyleBackColor = true;
        _lightButton.Click += OnLightThemeClick;

        _darkButton.AutoSize = true;
        _darkButton.Name = "darkThemeButton";
        _darkButton.Text = "Dark theme";
        _darkButton.UseVisualStyleBackColor = true;
        _darkButton.Click += OnDarkThemeClick;

        _baseFontLabel.AutoSize = true;
        _baseFontLabel.Name = "baseFontLabel";
        _baseFontLabel.Text = "Base font";
        _baseFontLabel.TextAlign = ContentAlignment.MiddleLeft;
        _baseFontLabel.Margin = new Padding(8, 5, 3, 3);

        _baseFontComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _baseFontComboBox.Name = "baseFontComboBox";
        _baseFontComboBox.AccessibleName = "SourceGrid demo base font profile";
        _baseFontComboBox.Width = 112;
        _baseFontComboBox.Items.AddRange(new object[] { "Default", "Base 14px", "Base 16px" });
        _baseFontComboBox.SelectedIndexChanged += OnBaseFontSelectedIndexChanged;

        _reducedMotionCheckBox.AutoSize = true;
        _reducedMotionCheckBox.Name = "reducedMotionCheckBox";
        _reducedMotionCheckBox.Text = "Reduced motion";
        _reducedMotionCheckBox.Margin = new Padding(8, 4, 3, 3);
        _reducedMotionCheckBox.CheckedChanged += OnReducedMotionCheckedChanged;

        _resetButton.AutoSize = true;
        _resetButton.Name = "resetGridButton";
        _resetButton.Text = "Reset / repopulate";
        _resetButton.UseVisualStyleBackColor = true;
        _resetButton.Click += OnResetGridClick;

        _consumerFontButton.AutoSize = true;
        _consumerFontButton.Name = "consumerFontButton";
        _consumerFontButton.Text = "Use consumer font";
        _consumerFontButton.UseVisualStyleBackColor = true;
        _consumerFontButton.Click += OnConsumerFontClick;

        _toolbar.AutoSize = true;
        _toolbar.Controls.Add(_lightButton);
        _toolbar.Controls.Add(_darkButton);
        _toolbar.Controls.Add(_baseFontLabel);
        _toolbar.Controls.Add(_baseFontComboBox);
        _toolbar.Controls.Add(_reducedMotionCheckBox);
        _toolbar.Controls.Add(_resetButton);
        _toolbar.Controls.Add(_consumerFontButton);
        _toolbar.Dock = DockStyle.Fill;
        _toolbar.Name = "themeToolbar";
        _toolbar.Padding = new Padding(8, 8, 8, 4);
        _toolbar.WrapContents = true;

        _diagnostics.AutoSize = true;
        _diagnostics.Dock = DockStyle.Fill;
        _diagnostics.Name = "diagnosticsLabel";
        _diagnostics.Padding = new Padding(8, 4, 8, 8);
        _diagnostics.Text = "Runtime/theme/DPI diagnostics appear here.";

        _interactionHelp.AutoSize = true;
        _interactionHelp.Dock = DockStyle.Fill;
        _interactionHelp.Name = "interactionHelp";
        _interactionHelp.Padding = new Padding(8, 6, 8, 6);
        _interactionHelp.Text = "Editors are shared per column/configuration. Try: F2 or typing; Enter to commit; Esc to cancel; Tab/Shift+Tab; lookup Up/Down/PageUp/PageDown, mouse selection, outside click, and the read-only DisplayMember column; application deactivation; header sort, range selection, scrolling, and Light/Dark switching while editing or the lookup popup is open.";

        _grid.Dock = DockStyle.Fill;
        _grid.Name = "BootstrapSourceGrid";

        _layout.ColumnCount = 1;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _layout.Controls.Add(_toolbar, 0, 0);
        _layout.Controls.Add(_diagnostics, 0, 1);
        _layout.Controls.Add(_interactionHelp, 0, 2);
        _layout.Controls.Add(_grid, 0, 3);
        _layout.Dock = DockStyle.Fill;
        _layout.Name = "mainLayout";
        _layout.RowCount = 4;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(960, 640);
        Controls.Add(_layout);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "MyDmsVn BootstrapSourceGrid Demo";

        _toolbar.ResumeLayout(false);
        _toolbar.PerformLayout();
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ResumeLayout(false);
    }
}
