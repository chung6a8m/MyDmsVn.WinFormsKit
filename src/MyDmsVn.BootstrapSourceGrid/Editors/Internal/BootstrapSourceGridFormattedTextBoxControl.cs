using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.BootstrapSourceGrid.Editors.Internal;

internal sealed class BootstrapSourceGridFormattedTextBoxControl : BootstrapFormattedTextBox
{
    internal void SelectAllForGridEdit()
    {
        Editor.SelectAll();
    }

    internal void ReplaceWithFirstEditCharacter(char value)
    {
        Editor.SelectedText = value.ToString();
    }

    internal void ProcessFormattedEditCommand(Keys keys)
    {
        OnEditorKeyDown(new KeyEventArgs(keys));
    }
}
