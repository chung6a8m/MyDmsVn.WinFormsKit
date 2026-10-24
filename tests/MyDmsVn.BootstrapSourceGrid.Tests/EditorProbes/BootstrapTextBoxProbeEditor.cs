using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.BootstrapSourceGrid.Tests.EditorProbes;

internal class BootstrapTextBoxProbeEditor : SourceGrid.Cells.Editors.EditorControlBase
{
    internal BootstrapTextBoxProbeEditor()
        : base(typeof(string))
    {
        UseCellViewProperties = false;
    }

    internal BootstrapTextBox BootstrapControl => (BootstrapTextBox)Control;

    internal bool IsEditorDisposed { get; private set; }

    internal bool IsControlDisposed => ((DisposalTrackingBootstrapTextBox)Control).DisposeCalled;

    internal int SelectionStart => ((DisposalTrackingBootstrapTextBox)Control).InnerSelectionStart;

    internal int SelectionLength => ((DisposalTrackingBootstrapTextBox)Control).InnerSelectionLength;

    protected override Control CreateControl()
    {
        return new DisposalTrackingBootstrapTextBox();
    }

    public override void SetEditValue(object editValue)
    {
        BootstrapControl.Text = editValue?.ToString() ?? string.Empty;
        BootstrapControl.SelectAll();
    }

    public override object GetEditedValue()
    {
        return BootstrapControl.Text;
    }

    protected override void OnSendCharToEditor(char key)
    {
        BootstrapControl.Text = key.ToString();
        ((DisposalTrackingBootstrapTextBox)Control).PlaceCaretAfterText();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsEditorDisposed = true;
        }

        base.Dispose(disposing);
    }

    private sealed class DisposalTrackingBootstrapTextBox : BootstrapTextBox
    {
        internal bool DisposeCalled { get; private set; }

        internal int InnerSelectionStart => Editor.SelectionStart;

        internal int InnerSelectionLength => Editor.SelectionLength;

        internal void PlaceCaretAfterText()
        {
            Editor.SelectionStart = Editor.TextLength;
            Editor.SelectionLength = 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCalled = true;
            }

            base.Dispose(disposing);
        }
    }
}
