using System;
using System.Globalization;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using MyDmsVn.BootstrapSourceGrid.Editors.Internal;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Editors;

/// <summary>
/// Adapts a <see cref="BootstrapFormattedTextBox"/> to the SourceGrid editor lifecycle.
/// </summary>
public sealed class BootstrapFormattedTextBoxEditor : SourceGrid.Cells.Editors.EditorControlBase
{
    internal BootstrapFormattedTextBoxEditor(BootstrapSourceGridControl owner, Type valueType)
        : base(ValidateOwner(owner, valueType))
    {
        Owner = owner;
        UseCellViewProperties = false;
    }

    internal BootstrapSourceGridControl Owner { get; }

    /// <summary>
    /// Gets the Bootstrap formatted text box used while a cell is being edited.
    /// </summary>
    public BootstrapFormattedTextBox BootstrapControl =>
        (BootstrapSourceGridFormattedTextBoxControl)Control;

    /// <inheritdoc />
    protected override Control CreateControl()
    {
        return new BootstrapSourceGridFormattedTextBoxControl();
    }

    /// <inheritdoc />
    public override void SetEditValue(object editValue)
    {
        var sourceGridValue = IsStringConversionSupported()
            ? ValueToString(editValue)
            : ValueToDisplayString(editValue);
        BootstrapControl.RawValue = ConvertSourceGridValueToCanonicalRaw(sourceGridValue);
        ((BootstrapSourceGridFormattedTextBoxControl)Control).SelectAllForGridEdit();
    }

    /// <inheritdoc />
    public override object GetEditedValue()
    {
        return ConvertCanonicalRawToSourceGridValue(BootstrapControl.RawValue);
    }

    /// <inheritdoc />
    protected override void OnSendCharToEditor(char key)
    {
        ((BootstrapSourceGridFormattedTextBoxControl)Control).ReplaceWithFirstEditCharacter(key);
    }

    private string ConvertSourceGridValueToCanonicalRaw(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        if (BootstrapControl.FormatMode != BootstrapInputFormatMode.Numeral)
        {
            return value;
        }

        var decimalSeparator = GetConversionCulture().NumberFormat.NumberDecimalSeparator;
        return decimalSeparator == "."
            ? value
            : value.Replace(decimalSeparator, ".");
    }

    private string ConvertCanonicalRawToSourceGridValue(string rawValue)
    {
        if (BootstrapControl.FormatMode != BootstrapInputFormatMode.Numeral)
        {
            return rawValue;
        }

        var decimalSeparator = GetConversionCulture().NumberFormat.NumberDecimalSeparator;
        return decimalSeparator == "."
            ? rawValue
            : rawValue.Replace(".", decimalSeparator);
    }

    private CultureInfo GetConversionCulture()
    {
        return CultureInfo ?? System.Globalization.CultureInfo.CurrentCulture;
    }

    private static Type ValidateOwner(BootstrapSourceGridControl owner, Type valueType)
    {
        if (owner is null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        return valueType;
    }
}
