using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.BootstrapSourceGrid.Editors.Internal;

internal sealed class BootstrapSourceGridTextBoxControl : BootstrapTextBox
{
    internal void SelectAllForGridEdit()
    {
        Editor.SelectAll();
    }

    internal void ReplaceWithFirstEditCharacter(char value)
    {
        Editor.Text = value.ToString();
        Editor.SelectionStart = Editor.TextLength;
        Editor.SelectionLength = 0;
    }
}
