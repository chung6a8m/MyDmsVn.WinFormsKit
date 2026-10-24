using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.BootstrapSourceGrid.Editors.Internal;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Editors;

/// <summary>
/// Adapts a <see cref="BootstrapTextBox"/> to the SourceGrid editor lifecycle.
/// </summary>
public sealed class BootstrapTextBoxEditor : SourceGrid.Cells.Editors.EditorControlBase
{
    internal BootstrapTextBoxEditor(BootstrapSourceGridControl owner, Type valueType)
        : base(ValidateOwner(owner, valueType))
    {
        Owner = owner;
        UseCellViewProperties = false;
    }

    internal BootstrapSourceGridControl Owner { get; }

    /// <summary>
    /// Gets the Bootstrap text box used while a cell is being edited.
    /// </summary>
    public BootstrapTextBox BootstrapControl => (BootstrapSourceGridTextBoxControl)Control;

    /// <inheritdoc />
    protected override Control CreateControl()
    {
        return new BootstrapSourceGridTextBoxControl();
    }

    /// <inheritdoc />
    public override void SetEditValue(object editValue)
    {
        BootstrapControl.Text = IsStringConversionSupported()
            ? ValueToString(editValue)
            : ValueToDisplayString(editValue);
        ((BootstrapSourceGridTextBoxControl)Control).SelectAllForGridEdit();
    }

    /// <inheritdoc />
    public override object GetEditedValue()
    {
        return BootstrapControl.Text;
    }

    /// <inheritdoc />
    protected override void OnSendCharToEditor(char key)
    {
        ((BootstrapSourceGridTextBoxControl)Control).ReplaceWithFirstEditCharacter(key);
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
