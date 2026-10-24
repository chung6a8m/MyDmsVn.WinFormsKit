using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.BootstrapSourceGrid.Editors.Internal;

internal sealed class BootstrapSourceGridDecimalInputControl : BootstrapDecimalInput
{
    internal BootstrapSourceGridDecimalInputControl()
    {
        Editor.TextAlign = HorizontalAlignment.Right;
        Editor.AutoSize = false;
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (Editor == null) return;
        var height = Math.Max(Editor.PreferredHeight, TextRenderer.MeasureText("123", Editor.Font).Height);
        Editor.SetBounds(Editor.Left, Math.Max(0, (ClientSize.Height - height) / 2), Editor.Width, height);
    }

    internal void SelectAllForGridEdit() => Editor.SelectAll();

    internal void ReplaceWithFirstEditCharacter(char value) => Editor.SelectedText = value.ToString();

    internal TextBox NativeEditor => Editor;

    internal void ProcessDecimalEditCommand(Keys keys) => OnEditorKeyDown(new KeyEventArgs(keys));
}
