namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Identifies how the busy backdrop is physically presented.</summary>
public enum BootstrapBusyOverlayRenderMode
{
    /// <summary>No busy presentation is visible.</summary>
    None = 0,
    /// <summary>The backdrop uses native child-window opacity.</summary>
    NativeLayered = 1,
    /// <summary>An opaque backdrop is used because native opacity is unavailable.</summary>
    OpaqueFallback = 2
}
