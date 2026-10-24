using System;
using System.Collections.Generic;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Editors;

/// <summary>
/// Creates and owns Bootstrap editor adapters for one <see cref="BootstrapSourceGridControl"/>.
/// </summary>
/// <remarks>
/// Create one editor per column or configuration and share it between cells in the owning
/// grid. The owning grid disposes every editor created by this registry. An editor created
/// by one grid must not be used by another grid. Disposing either the registry or its owning
/// grid permanently prevents this registry from creating or registering more editors.
/// </remarks>
public sealed class BootstrapSourceGridEditorRegistry : IDisposable
{
    private readonly List<SourceGrid.Cells.Editors.EditorControlBase> _ownedEditors = new();
    private readonly BootstrapSourceGridControl _owner;
    private bool _disposed;

    internal BootstrapSourceGridEditorRegistry(BootstrapSourceGridControl owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <summary>
    /// Creates a grid-owned Bootstrap text-box editor for the specified value type.
    /// </summary>
    /// <param name="valueType">The SourceGrid value type converted when editing commits.</param>
    /// <returns>A shared editor owned by this registry's grid.</returns>
    /// <exception cref="ObjectDisposedException">The registry has been disposed.</exception>
    public BootstrapTextBoxEditor CreateTextBox(Type valueType)
    {
        ThrowIfDisposed();

        if (valueType is null)
        {
            throw new ArgumentNullException(nameof(valueType));
        }

        return Register(new BootstrapTextBoxEditor(_owner, valueType));
    }

    /// <summary>
    /// Creates a grid-owned Bootstrap formatted-text-box editor for the specified value type.
    /// </summary>
    /// <param name="valueType">The SourceGrid value type converted when editing commits.</param>
    /// <returns>A shared editor owned by this registry's grid.</returns>
    /// <exception cref="ObjectDisposedException">The registry has been disposed.</exception>
    public BootstrapFormattedTextBoxEditor CreateFormattedTextBox(Type valueType)
    {
        ThrowIfDisposed();

        if (valueType is null)
        {
            throw new ArgumentNullException(nameof(valueType));
        }

        return Register(new BootstrapFormattedTextBoxEditor(_owner, valueType));
    }

    /// <summary>Creates a grid-owned decimal input editor for one field configuration.</summary>
    public BootstrapDecimalInputEditor CreateDecimalInput(Type valueType, DecimalInputProfile profile)
    {
        ThrowIfDisposed();
        if (valueType == null) throw new ArgumentNullException(nameof(valueType));
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        return Register(new BootstrapDecimalInputEditor(_owner, valueType, profile));
    }

    /// <summary>
    /// Creates a grid-owned Bootstrap lookup-box editor for the specified value type.
    /// </summary>
    /// <param name="valueType">The SourceGrid value type converted when editing commits.</param>
    /// <returns>A shared editor owned by this registry's grid.</returns>
    /// <exception cref="ObjectDisposedException">The registry has been disposed.</exception>
    public BootstrapLookupBoxEditor CreateLookupBox(Type valueType)
    {
        ThrowIfDisposed();

        if (valueType is null)
        {
            throw new ArgumentNullException(nameof(valueType));
        }

        return Register(new BootstrapLookupBoxEditor(_owner, valueType));
    }

    internal T Register<T>(T editor)
        where T : SourceGrid.Cells.Editors.EditorControlBase
    {
        ThrowIfDisposed();

        if (editor is null)
        {
            throw new ArgumentNullException(nameof(editor));
        }

        _ownedEditors.Add(editor);
        return editor;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var editor in _ownedEditors)
        {
            editor.Control.Dispose();
            editor.Dispose();
        }

        _ownedEditors.Clear();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(BootstrapSourceGridEditorRegistry));
        }
    }
}
