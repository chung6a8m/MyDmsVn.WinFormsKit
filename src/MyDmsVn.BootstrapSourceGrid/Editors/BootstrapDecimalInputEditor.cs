using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using MyDmsVn.BootstrapSourceGrid.Editors.Internal;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Editors;

/// <summary>Adapts a decimal input to one SourceGrid editing lifecycle.</summary>
public sealed class BootstrapDecimalInputEditor : SourceGrid.Cells.Editors.EditorControlBase
{
    private DecimalInputProfile _profile;

    internal BootstrapDecimalInputEditor(BootstrapSourceGridControl owner, Type valueType, DecimalInputProfile profile)
        : base(ValidateArguments(owner, valueType, profile))
    {
        Owner = owner;
        UseCellViewProperties = false;
        _profile = profile;
        BootstrapControl.Profile = _profile;
        AllowNull = _profile.IsNullable;
    }

    internal BootstrapSourceGridControl Owner { get; }

    /// <summary>The Bootstrap input owned by this adapter.</summary>
    public BootstrapDecimalInput BootstrapControl => (BootstrapSourceGridDecimalInputControl)Control;

    /// <summary>Profile applied at the next admitted edit; cannot change during an active edit.</summary>
    public DecimalInputProfile Profile
    {
        get => _profile;
        set
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (IsEditing) throw new InvalidOperationException("The active decimal edit profile is frozen.");
            BootstrapControl.Profile = value;
            _profile = value;
            AllowNull = value.IsNullable;
        }
    }

    /// <inheritdoc />
    protected override Control CreateControl() => new BootstrapSourceGridDecimalInputControl();

    /// <inheritdoc />
    public override void SetEditValue(object editValue)
    {
        if (editValue != null && !(editValue is decimal))
            throw new ArgumentException("The current cell value must be decimal or null.", nameof(editValue));
        AllowNull = _profile.IsNullable;
        BootstrapControl.BeginEditSession((decimal?)editValue, _profile);
        ((BootstrapSourceGridDecimalInputControl)Control).SelectAllForGridEdit();
    }

    /// <inheritdoc />
    public override object GetEditedValue()
    {
        var result = BootstrapControl.TryParsePending();
        if (result.State != InputParseState.Valid)
            throw new FormatException($"Decimal candidate cannot be committed: {result.Failure?.Code}.");
        return result.Value!;
    }

    /// <inheritdoc />
    public override bool ApplyEdit()
    {
        var wasEditing = IsEditing;
        var context = EditCellContext;
        if (!base.ApplyEdit()) return false;
        if (wasEditing)
        {
            var accepted = context.Cell.Model.ValueModel.GetValue(context);
            if (accepted is decimal value)
                BootstrapControl.ProjectAcceptedValue(value);
            else if (accepted == null)
                BootstrapControl.ProjectAcceptedValue(null);
        }
        return true;
    }

    /// <inheritdoc />
    protected override void OnSendCharToEditor(char key)
    {
        ((BootstrapSourceGridDecimalInputControl)Control).ReplaceWithFirstEditCharacter(key);
    }

    private static Type ValidateArguments(BootstrapSourceGridControl owner, Type valueType, DecimalInputProfile profile)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (valueType == null) throw new ArgumentNullException(nameof(valueType));
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        if (valueType != typeof(decimal)) throw new ArgumentException("Decimal input requires decimal cells.", nameof(valueType));
        return valueType;
    }
}
