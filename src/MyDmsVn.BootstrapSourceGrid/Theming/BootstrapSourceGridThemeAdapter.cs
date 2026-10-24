using System;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.BootstrapSourceGrid.Theming;

internal static class BootstrapSourceGridThemeAdapter
{
    public static BootstrapSourceGridThemeSnapshot CreateSnapshot(BootstrapTheme theme)
    {
        if (theme is null)
        {
            throw new ArgumentNullException(nameof(theme));
        }

        var colors = theme.Colors;
        var selectionForeColor = ColorUtil.GetContrastingTextColor(
            colors.Primary,
            colors.Light,
            colors.Dark);

        return new BootstrapSourceGridThemeSnapshot(
            cellBackColor: colors.Surface,
            alternateCellBackColor: colors.SurfaceSecondary,
            cellForeColor: colors.Text,
            mutedForeColor: colors.MutedText,
            headerBackColor: colors.SurfaceSecondary,
            headerForeColor: colors.Text,
            borderColor: colors.Border,
            selectionBackColor: colors.Primary,
            selectionForeColor: selectionForeColor,
            focusColor: colors.Focus,
            disabledColor: colors.Disabled,
            hoverColor: colors.Hover,
            activeColor: colors.Active,
            bodyFont: theme.Typography.Body);
    }
}
