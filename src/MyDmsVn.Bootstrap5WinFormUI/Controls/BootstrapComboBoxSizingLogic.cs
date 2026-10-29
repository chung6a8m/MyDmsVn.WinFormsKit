using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapComboBoxSizing
{
    public BootstrapComboBoxSizing(int outerHeight, int selectionHeight, int itemHeight)
    {
        OuterHeight = outerHeight;
        SelectionHeight = selectionHeight;
        ItemHeight = itemHeight;
    }

    public int OuterHeight { get; }

    public int SelectionHeight { get; }

    public int ItemHeight { get; }
}

internal static class BootstrapComboBoxSizingLogic
{
    public static BootstrapComboBoxSizing Resolve(
        int targetOuterHeight,
        int nativeChromeHeight,
        int minimumFieldHeight)
    {
        if (targetOuterHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetOuterHeight), targetOuterHeight, "Outer height must be greater than zero.");
        }

        if (nativeChromeHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nativeChromeHeight), nativeChromeHeight, "Native chrome height cannot be negative.");
        }

        if (minimumFieldHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumFieldHeight), minimumFieldHeight, "Minimum field height must be greater than zero.");
        }

        var selectionHeight = Math.Max(minimumFieldHeight, targetOuterHeight - nativeChromeHeight);
        return new BootstrapComboBoxSizing(
            selectionHeight + nativeChromeHeight,
            selectionHeight,
            selectionHeight);
    }
}
