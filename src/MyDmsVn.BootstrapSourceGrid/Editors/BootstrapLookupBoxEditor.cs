using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.BootstrapSourceGrid.Editors.Internal;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Editors;

/// <summary>
/// Adapts a <see cref="BootstrapLookupBox"/> to the SourceGrid editor lifecycle.
/// </summary>
public sealed class BootstrapLookupBoxEditor : SourceGrid.Cells.Editors.EditorControlBase
{
    internal BootstrapLookupBoxEditor(BootstrapSourceGridControl owner, Type valueType)
        : base(ValidateOwner(owner, valueType))
    {
        Owner = owner;
        UseCellViewProperties = false;
        var control = (BootstrapSourceGridLookupBoxControl)Control;
        control.OwnerEditCompletionRequested = CompleteGridEdit;
        control.OwnerNavigationRequested = ContinueGridNavigation;
        control.SelectionCommitted += OnSelectionCommitted;
    }

    internal BootstrapSourceGridControl Owner { get; }

    /// <summary>
    /// Gets the Bootstrap lookup box used while a cell is being edited.
    /// </summary>
    public BootstrapLookupBox BootstrapControl =>
        (BootstrapSourceGridLookupBoxControl)Control;

    /// <inheritdoc />
    protected override Control CreateControl()
    {
        return new BootstrapSourceGridLookupBoxControl();
    }

    /// <inheritdoc />
    public override void SetEditValue(object editValue)
    {
        BootstrapControl.SelectedValue = editValue;
    }

    /// <inheritdoc />
    public override object GetEditedValue()
    {
        return BootstrapControl.SelectedValue!;
    }

    /// <inheritdoc />
    protected override void OnSendCharToEditor(char key)
    {
        ((BootstrapSourceGridLookupBoxControl)Control).ReplaceWithFirstEditCharacter(key);
    }

    private void OnSelectionCommitted(
        object? sender,
        BootstrapLookupSelectionCommittedEventArgs e)
    {
        if (!IsEditing ||
            e.Reason == BootstrapLookupCommitReason.Programmatic ||
            e.Reason == BootstrapLookupCommitReason.Clear)
        {
            return;
        }

        EditCellContext.EndEdit(false);
    }

    private bool ContinueGridNavigation(bool reverse)
    {
        if (IsEditing && !EditCellContext.EndEdit(false))
        {
            return true;
        }

        if (Grid is null)
        {
            return false;
        }

        var args = new KeyEventArgs(reverse ? Keys.Shift | Keys.Tab : Keys.Tab);
        Grid.ProcessSpecialGridKey(args);
        return args.Handled;
    }

    private bool CompleteGridEdit(bool cancel)
    {
        if (!IsEditing)
        {
            return false;
        }

        EditCellContext.EndEdit(cancel);
        return true;
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
