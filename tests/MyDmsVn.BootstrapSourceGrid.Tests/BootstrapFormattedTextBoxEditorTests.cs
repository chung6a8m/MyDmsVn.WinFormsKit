using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using MyDmsVn.BootstrapSourceGrid.Editors;
using MyDmsVn.BootstrapSourceGrid.Editors.Internal;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapFormattedTextBoxEditorTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void SetEditValueInitializesRawValue()
    {
        using (var fixture = new FormattedEditorFixture("12345678", "87654321"))
        {
            fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.General;
            fixture.Editor.BootstrapControl.GeneralOptions.Blocks = new[] { 4, 4 };

            fixture.Editor.SetEditValue("12345678");

            Assert.That(fixture.Editor.BootstrapControl.RawValue, Is.EqualTo("12345678"));
            Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo("1234 5678"));
        }
    }

    [Test]
    public void GetEditedValueReturnsRawValueNotFormattedText()
    {
        using (var fixture = new FormattedEditorFixture("12345678", "87654321"))
        {
            fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.General;
            fixture.Editor.BootstrapControl.GeneralOptions.Blocks = new[] { 4, 4 };
            fixture.Editor.BootstrapControl.RawValue = "12345678";

            Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo("1234 5678"));
            Assert.That(fixture.Editor.GetEditedValue(), Is.EqualTo("12345678"));
        }
    }

    [Test]
    public void CommitUsesSourceGridTypedConversion()
    {
        using (var fixture = new FormattedEditorFixture(1, 2, typeof(int)))
        {
            fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.Numeral;
            var context = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.RawValue = "42";

            Assert.That(context.EndEdit(false), Is.True);
            Assert.That(fixture.Cells[0].Value, Is.TypeOf<int>());
            Assert.That(fixture.Cells[0].Value, Is.EqualTo(42));
        }
    }

    [Test]
    public void CancelRestoresOriginalLogicalValue()
    {
        using (var fixture = new FormattedEditorFixture("12345678", "87654321"))
        {
            fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.General;
            fixture.Editor.BootstrapControl.GeneralOptions.Blocks = new[] { 4, 4 };
            var context = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.RawValue = "99998888";

            Assert.That(context.EndEdit(true), Is.True);
            Assert.That(fixture.Cells[0].Value, Is.EqualTo("12345678"));
            Assert.That(fixture.Editor.BootstrapControl.RawValue, Is.EqualTo("12345678"));
            Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo("1234 5678"));
        }
    }

    [Test]
    public void SendFirstCharacterUsesFormattedInputSemantics()
    {
        using (var fixture = new FormattedEditorFixture("12345678", "87654321"))
        {
            fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.General;
            fixture.Editor.BootstrapControl.GeneralOptions.Blocks = new[] { 4, 4 };
            var context = fixture.StartEdit(0);

            try
            {
                fixture.Editor.SendCharToEditor('9');

                Assert.That(fixture.Editor.BootstrapControl.RawValue, Is.EqualTo("9"));
                Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo("9"));
            }
            finally
            {
                context.EndEdit(true);
            }
        }
    }

    [Test]
    public void NoneModeRoundTripsRawInputThroughSourceGridConversion()
    {
        using (var fixture = new FormattedEditorFixture(1, 2, typeof(int)))
        {
            fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.None;
            var context = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.RawValue = "42";

            Assert.That(context.EndEdit(false), Is.True);
            Assert.That(fixture.Cells[0].Value, Is.TypeOf<int>());
            Assert.That(fixture.Cells[0].Value, Is.EqualTo(42));
        }
    }

    [Test]
    public void GeneralModeCommitsRawValueInsteadOfDecoratedDisplayText()
    {
        using (var fixture = new FormattedEditorFixture(12, 34, typeof(int)))
        {
            fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.General;
            fixture.Editor.BootstrapControl.GeneralOptions.Blocks = new[] { 2, 2 };
            var context = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.RawValue = "1234";

            Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo("12 34"));
            Assert.That(context.EndEdit(false), Is.True);
            Assert.That(fixture.Cells[0].Value, Is.TypeOf<int>());
            Assert.That(fixture.Cells[0].Value, Is.EqualTo(1234));
        }
    }

    [Test]
    public void NumeralModeFormatsDisplayAndCommitsCanonicalDecimal()
    {
        using (var fixture = new FormattedEditorFixture(1.5m, 2.5m, typeof(decimal)))
        {
            fixture.Editor.CultureInfo = CultureInfo.InvariantCulture;
            fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.Numeral;
            var context = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.RawValue = "1234567.89";

            Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo("1,234,567.89"));
            Assert.That(fixture.Editor.BootstrapControl.RawValue, Is.EqualTo("1234567.89"));
            Assert.That(context.EndEdit(false), Is.True);
            Assert.That(fixture.Cells[0].Value, Is.TypeOf<decimal>());
            Assert.That(fixture.Cells[0].Value, Is.EqualTo(1234567.89m));
        }
    }

    [Test]
    public void NumeralModeUnchangedCommaDecimalValueRoundTripsWithoutCorruption()
    {
        var originalCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("vi-VN");
            using (var fixture = new FormattedEditorFixture(1.5m, 2.5m, typeof(decimal)))
            {
                fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.Numeral;

                var context = fixture.StartEdit(0);

                Assert.That(fixture.Editor.CultureInfo, Is.Null);
                Assert.That(fixture.Editor.BootstrapControl.RawValue, Is.EqualTo("1.5"));
                Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo("1.5"));
                Assert.That(context.EndEdit(false), Is.True);
                Assert.That(fixture.Cells[0].Value, Is.TypeOf<decimal>());
                Assert.That(fixture.Cells[0].Value, Is.EqualTo(1.5m));
            }
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void DateModeUsesSourceGridConverterForCanonicalRawValue()
    {
        var original = new DateTime(2026, 8, 31);
        var edited = new DateTime(2026, 9, 1);
        using (var fixture = new FormattedEditorFixture(original, edited, typeof(DateTime)))
        {
            fixture.Editor.TypeConverter = new CanonicalDateTypeConverter();
            fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.Date;
            var context = fixture.StartEdit(0);

            Assert.That(fixture.Editor.BootstrapControl.RawValue, Is.EqualTo("31082026"));
            Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo("31/08/2026"));
            fixture.Editor.BootstrapControl.RawValue = "01092026";
            Assert.That(context.EndEdit(false), Is.True);
            Assert.That(fixture.Cells[0].Value, Is.TypeOf<DateTime>());
            Assert.That(fixture.Cells[0].Value, Is.EqualTo(edited));
        }
    }

    [Test]
    public void TimeModeUsesSourceGridConverterForCanonicalRawValue()
    {
        var original = new TimeSpan(12, 30, 0);
        var edited = new TimeSpan(23, 59, 0);
        using (var fixture = new FormattedEditorFixture(original, edited, typeof(TimeSpan)))
        {
            fixture.Editor.TypeConverter = new CanonicalTimeTypeConverter();
            fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.Time;
            var context = fixture.StartEdit(0);

            Assert.That(fixture.Editor.BootstrapControl.RawValue, Is.EqualTo("1230"));
            Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo("12:30"));
            fixture.Editor.BootstrapControl.RawValue = "2359";
            Assert.That(context.EndEdit(false), Is.True);
            Assert.That(fixture.Cells[0].Value, Is.TypeOf<TimeSpan>());
            Assert.That(fixture.Cells[0].Value, Is.EqualTo(edited));
        }
    }

    [Test]
    public void InvalidRawValueRemainsUnderSourceGridValidation()
    {
        using (var fixture = new FormattedEditorFixture(1, 2, typeof(int)))
        {
            var editExceptionCount = 0;
            fixture.Editor.EditException += (_, _) => editExceptionCount++;
            fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.None;
            var context = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.RawValue = "not-a-number";

            Assert.That(context.EndEdit(false), Is.False);
            Assert.That(fixture.Editor.IsEditing, Is.True);
            Assert.That(fixture.Cells[0].Value, Is.EqualTo(1));
            Assert.That(editExceptionCount, Is.EqualTo(1));
        }
    }

    [Test]
    public void BeginEditSelectsEntireFormattedDisplay()
    {
        using (var fixture = new FormattedEditorFixture("12345678", "87654321"))
        {
            fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.General;
            fixture.Editor.BootstrapControl.GeneralOptions.Blocks = new[] { 4, 4 };
            var context = fixture.StartEdit(0);

            try
            {
                var nativeEditor = GetNativeEditor(fixture.Editor.BootstrapControl);

                Assert.That(nativeEditor.SelectionStart, Is.Zero);
                Assert.That(nativeEditor.SelectionLength, Is.EqualTo("1234 5678".Length));
            }
            finally
            {
                context.EndEdit(true);
            }
        }
    }

    [TestCase(BootstrapInputFormatMode.General, "12345678", '9', "9", "9")]
    [TestCase(BootstrapInputFormatMode.Numeral, "1234567.89", '9', "9", "9")]
    [TestCase(BootstrapInputFormatMode.Date, "31082026", '1', "1", "1")]
    [TestCase(BootstrapInputFormatMode.Time, "1230", '2', "2", "2")]
    public void FirstCharacterUsesFormattedCaretMapping(
        BootstrapInputFormatMode mode,
        string initialRawValue,
        char firstCharacter,
        string expectedRawValue,
        string expectedDisplayValue)
    {
        using (var fixture = new FormattedEditorFixture(initialRawValue, string.Empty))
        {
            fixture.Editor.BootstrapControl.FormatMode = mode;
            fixture.Editor.BootstrapControl.GeneralOptions.Blocks = new[] { 4, 4 };
            var context = fixture.StartEdit(0);

            try
            {
                fixture.Editor.SendCharToEditor(firstCharacter);
                var nativeEditor = GetNativeEditor(fixture.Editor.BootstrapControl);

                Assert.That(fixture.Editor.BootstrapControl.RawValue, Is.EqualTo(expectedRawValue));
                Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo(expectedDisplayValue));
                Assert.That(nativeEditor.SelectionStart, Is.EqualTo(expectedDisplayValue.Length));
                Assert.That(nativeEditor.SelectionLength, Is.Zero);
            }
            finally
            {
                context.EndEdit(true);
            }
        }
    }

    [Test]
    public void FirstCharacterEditRemainsUndoableAndRedoableWithinSourceGridSession()
    {
        using (var fixture = new FormattedEditorFixture("12345678", string.Empty))
        {
            fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.General;
            fixture.Editor.BootstrapControl.GeneralOptions.Blocks = new[] { 4, 4 };
            var context = fixture.StartEdit(0);
            fixture.Editor.SendCharToEditor('9');
            var control = (BootstrapSourceGridFormattedTextBoxControl)fixture.Editor.Control;

            control.ProcessFormattedEditCommand(Keys.Control | Keys.Z);

            Assert.That(fixture.Editor.IsEditing, Is.True);
            Assert.That(fixture.Editor.BootstrapControl.RawValue, Is.EqualTo("12345678"));
            Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo("1234 5678"));

            control.ProcessFormattedEditCommand(Keys.Control | Keys.Y);

            Assert.That(fixture.Editor.IsEditing, Is.True);
            Assert.That(fixture.Editor.BootstrapControl.RawValue, Is.EqualTo("9"));
            Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo("9"));
            context.EndEdit(true);
        }
    }

    [Test]
    public void CancelAfterMultipleFormattedChangesRestoresOriginalLogicalAndDisplayValues()
    {
        using (var fixture = new FormattedEditorFixture("12345678", string.Empty))
        {
            fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.General;
            fixture.Editor.BootstrapControl.GeneralOptions.Blocks = new[] { 4, 4 };
            var context = fixture.StartEdit(0);
            fixture.Editor.SendCharToEditor('9');
            var nativeEditor = GetNativeEditor(fixture.Editor.BootstrapControl);
            nativeEditor.Select(nativeEditor.TextLength, 0);
            nativeEditor.SelectedText = "8";

            Assert.That(fixture.Editor.BootstrapControl.RawValue, Is.EqualTo("98"));
            Assert.That(context.EndEdit(true), Is.True);
            Assert.That(fixture.Cells[0].Value, Is.EqualTo("12345678"));
            Assert.That(fixture.Editor.BootstrapControl.RawValue, Is.EqualTo("12345678"));
            Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo("1234 5678"));
        }
    }

    [Test]
    public void ActiveEditFollowsRuntimeThemeWithoutCellViewPropertyInjection()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        using (var customFont = new Font(FontFamily.GenericMonospace, 17f, FontStyle.Bold))
        {
            try
            {
                BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
                using (var fixture = new FormattedEditorFixture("12345678", string.Empty))
                {
                    fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.General;
                    fixture.Editor.BootstrapControl.GeneralOptions.Blocks = new[] { 4, 4 };
                    var customView = new SourceGrid.Cells.Views.Cell
                    {
                        BackColor = Color.Red,
                        ForeColor = Color.Lime,
                        Font = customFont,
                    };
                    fixture.Cells[0].View = customView;
                    var context = fixture.StartEdit(0);

                    try
                    {
                        BootstrapThemeManager.CurrentTheme =
                            BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
                        Application.DoEvents();
                        var theme = BootstrapThemeManager.CurrentTheme;
                        var nativeEditor = GetNativeEditor(fixture.Editor.BootstrapControl);

                        Assert.That(fixture.Editor.UseCellViewProperties, Is.False);
                        Assert.That(nativeEditor.BackColor, Is.EqualTo(theme.Colors.Surface));
                        Assert.That(nativeEditor.ForeColor, Is.EqualTo(theme.Colors.Text));
                        Assert.That(nativeEditor.BackColor, Is.Not.EqualTo(customView.BackColor));
                        Assert.That(nativeEditor.ForeColor, Is.Not.EqualTo(customView.ForeColor));
                        Assert.That(nativeEditor.Font.Name, Is.EqualTo(theme.Typography.Body.FontFamilyName));
                        Assert.That(
                            nativeEditor.Font.SizeInPoints,
                            Is.EqualTo(theme.Typography.Body.SizeInPoints).Within(0.01f));
                        Assert.That(nativeEditor.Font.Style, Is.EqualTo(theme.Typography.Body.Style));
                    }
                    finally
                    {
                        context.EndEdit(true);
                    }
                }
            }
            finally
            {
                BootstrapThemeManager.CurrentTheme = originalTheme;
            }
        }
    }

    [Test]
    public void OneConfiguredEditorEditsMultipleCellsSequentiallyInOneGrid()
    {
        using (var fixture = new FormattedEditorFixture("12345678", "87654321"))
        {
            fixture.Editor.BootstrapControl.FormatMode = BootstrapInputFormatMode.General;
            fixture.Editor.BootstrapControl.GeneralOptions.Blocks = new[] { 4, 4 };
            var control = fixture.Editor.BootstrapControl;

            var firstContext = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.RawValue = "11112222";
            Assert.That(firstContext.EndEdit(false), Is.True);

            var secondContext = fixture.StartEdit(1);
            fixture.Editor.BootstrapControl.RawValue = "33334444";
            Assert.That(secondContext.EndEdit(false), Is.True);

            Assert.That(fixture.Cells[0].Value, Is.EqualTo("11112222"));
            Assert.That(fixture.Cells[1].Value, Is.EqualTo("33334444"));
            Assert.That(fixture.Cells[0].Editor, Is.SameAs(fixture.Editor));
            Assert.That(fixture.Cells[1].Editor, Is.SameAs(fixture.Editor));
            Assert.That(fixture.Editor.BootstrapControl, Is.SameAs(control));
            Assert.That(fixture.Editor.BootstrapControl.FormatMode, Is.EqualTo(BootstrapInputFormatMode.General));
            Assert.That(fixture.Editor.BootstrapControl.GeneralOptions.Blocks, Is.EqualTo(new[] { 4, 4 }));
        }
    }

    private static TextBox GetNativeEditor(BootstrapFormattedTextBox control)
    {
        foreach (Control child in control.Controls)
        {
            if (child is TextBox textBox)
            {
                return textBox;
            }
        }

        throw new InvalidOperationException("BootstrapFormattedTextBox native editor was not found.");
    }

    private sealed class FormattedEditorFixture : IDisposable
    {
        internal FormattedEditorFixture(object firstValue, object secondValue, Type? valueType = null)
        {
            Form = new Form
            {
                ClientSize = new Size(240, 90),
            };
            Grid = new BootstrapSourceGridControl
            {
                Dock = DockStyle.Fill,
            };
            var declaredType = valueType ?? typeof(string);
            Editor = Grid.BootstrapEditors.CreateFormattedTextBox(declaredType);
            Cells = new[]
            {
                new SourceGrid.Cells.Cell(firstValue, declaredType) { Editor = Editor },
                new SourceGrid.Cells.Cell(secondValue, declaredType) { Editor = Editor },
            };

            Grid.Redim(2, 1);
            Grid.Rows[0].Height = 30;
            Grid.Rows[1].Height = 30;
            Grid[0, 0] = Cells[0];
            Grid[1, 0] = Cells[1];
            Form.Controls.Add(Grid);
            Form.Show();
            Grid.Focus();
        }

        internal Form Form { get; }

        internal BootstrapSourceGridControl Grid { get; }

        internal BootstrapFormattedTextBoxEditor Editor { get; }

        internal SourceGrid.Cells.Cell[] Cells { get; }

        internal SourceGrid.CellContext StartEdit(int row)
        {
            var position = new SourceGrid.Position(row, 0);
            Assert.That(Grid.Selection.Focus(position, true), Is.True);
            var context = new SourceGrid.CellContext(Grid, position, Cells[row]);
            Grid.GetCell(position).View.Measure(context, Size.Empty);
            context.StartEdit();
            Assert.That(Editor.IsEditing, Is.True);
            return context;
        }

        public void Dispose()
        {
            if (Editor.IsEditing)
            {
                Editor.EditCellContext.EndEdit(true);
            }

            Form.Close();
            Form.Dispose();
            Grid.Dispose();
        }
    }

    private sealed class CanonicalDateTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override object? ConvertFrom(
            ITypeDescriptorContext? context,
            CultureInfo? culture,
            object value)
        {
            if (value is string rawValue)
            {
                return DateTime.ParseExact(rawValue, "ddMMyyyy", CultureInfo.InvariantCulture);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object? ConvertTo(
            ITypeDescriptorContext? context,
            CultureInfo? culture,
            object? value,
            Type destinationType)
        {
            if (destinationType == typeof(string) && value is DateTime date)
            {
                return date.ToString("ddMMyyyy", CultureInfo.InvariantCulture);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    private sealed class CanonicalTimeTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override object? ConvertFrom(
            ITypeDescriptorContext? context,
            CultureInfo? culture,
            object value)
        {
            if (value is string rawValue && rawValue.Length == 4)
            {
                var hours = int.Parse(rawValue.Substring(0, 2), CultureInfo.InvariantCulture);
                var minutes = int.Parse(rawValue.Substring(2, 2), CultureInfo.InvariantCulture);
                return new TimeSpan(hours, minutes, 0);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object? ConvertTo(
            ITypeDescriptorContext? context,
            CultureInfo? culture,
            object? value,
            Type destinationType)
        {
            if (destinationType == typeof(string) && value is TimeSpan time)
            {
                return $"{time.Hours:00}{time.Minutes:00}";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
