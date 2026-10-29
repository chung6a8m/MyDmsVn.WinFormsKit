using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Rendering;

internal readonly struct BootstrapRoundedSurfaceGeometry
{
    private const float FitTolerance = 0.001f;
    private readonly bool _hasUsableStroke;

    private BootstrapRoundedSurfaceGeometry(
        RectangleF outerBounds,
        RectangleF strokeBounds,
        RectangleF innerBounds,
        CornerRadius outerRadius,
        CornerRadius strokeRadius,
        CornerRadius innerRadius,
        float borderWidth,
        bool hasUsableStroke,
        bool hasUsableInterior)
    {
        OuterBounds = outerBounds;
        StrokeBounds = strokeBounds;
        InnerBounds = innerBounds;
        OuterRadius = outerRadius;
        StrokeRadius = strokeRadius;
        InnerRadius = innerRadius;
        BorderWidth = borderWidth;
        _hasUsableStroke = hasUsableStroke;
        HasUsableInterior = hasUsableInterior;
    }

    internal RectangleF OuterBounds { get; }

    internal RectangleF StrokeBounds { get; }

    internal RectangleF InnerBounds { get; }

    internal CornerRadius OuterRadius { get; }

    internal CornerRadius StrokeRadius { get; }

    internal CornerRadius InnerRadius { get; }

    internal float BorderWidth { get; }

    internal bool HasUsableInterior { get; }

    internal static BootstrapRoundedSurfaceGeometry Resolve(
        RectangleF outerBounds,
        CornerRadius outerRadius,
        float borderWidth)
    {
        ValidateFiniteNonNegative(borderWidth, nameof(borderWidth));
        if (!IsUsable(outerBounds))
        {
            return new BootstrapRoundedSurfaceGeometry(
                RectangleF.Empty,
                RectangleF.Empty,
                RectangleF.Empty,
                CornerRadius.Empty,
                CornerRadius.Empty,
                CornerRadius.Empty,
                borderWidth,
                hasUsableStroke: false,
                hasUsableInterior: false);
        }

        var normalizedOuterRadius = outerRadius.NormalizeTo(outerBounds);
        var strokeInset = borderWidth / 2f;
        var strokeBounds = Inset(outerBounds, strokeInset);
        var innerBounds = Inset(outerBounds, borderWidth);
        var strokeRadius = Subtract(normalizedOuterRadius, strokeInset);
        var innerRadius = Subtract(normalizedOuterRadius, borderWidth);
        var hasUsableStroke = IsUsable(strokeBounds) && Fits(strokeRadius, strokeBounds);
        var hasUsableInterior = IsUsable(innerBounds) && Fits(innerRadius, innerBounds);
        if (!hasUsableInterior)
        {
            innerBounds = RectangleF.Empty;
        }

        return new BootstrapRoundedSurfaceGeometry(
            outerBounds,
            strokeBounds,
            innerBounds,
            normalizedOuterRadius,
            strokeRadius,
            innerRadius,
            borderWidth,
            hasUsableStroke,
            hasUsableInterior);
    }

    internal Padding GetDecorationInsets(float rasterGuardPixels)
    {
        ValidateFiniteNonNegative(rasterGuardPixels, nameof(rasterGuardPixels));
        if (!IsUsable(OuterBounds))
        {
            return Padding.Empty;
        }

        if (!HasUsableInterior)
        {
            return EmptyContentInsets(OuterBounds);
        }

        var topLeft = ResolveCornerInset(OuterRadius.TopLeft, BorderWidth, rasterGuardPixels);
        var topRight = ResolveCornerInset(OuterRadius.TopRight, BorderWidth, rasterGuardPixels);
        var bottomRight = ResolveCornerInset(OuterRadius.BottomRight, BorderWidth, rasterGuardPixels);
        var bottomLeft = ResolveCornerInset(OuterRadius.BottomLeft, BorderWidth, rasterGuardPixels);
        var left = Math.Max(topLeft, bottomLeft);
        var top = Math.Max(topLeft, topRight);
        var right = Math.Max(topRight, bottomRight);
        var bottom = Math.Max(bottomLeft, bottomRight);
        if (left + right >= OuterBounds.Width || top + bottom >= OuterBounds.Height)
        {
            return EmptyContentInsets(OuterBounds);
        }

        return new Padding(left, top, right, bottom);
    }

    internal GraphicsPath CreateOuterPath()
    {
        return IsUsable(OuterBounds)
            ? RoundedPath.Create(OuterBounds, OuterRadius)
            : new GraphicsPath();
    }

    internal GraphicsPath CreateOuterEnvelopePath(float allowance)
    {
        ValidateFiniteNonNegative(allowance, nameof(allowance));
        if (!IsUsable(OuterBounds))
        {
            return new GraphicsPath();
        }

        var envelopeBounds = RectangleF.Inflate(OuterBounds, allowance, allowance);
        var envelopeRadius = new CornerRadius(
            OuterRadius.TopLeft + allowance,
            OuterRadius.TopRight + allowance,
            OuterRadius.BottomRight + allowance,
            OuterRadius.BottomLeft + allowance);
        return RoundedPath.Create(envelopeBounds, envelopeRadius);
    }

    internal GraphicsPath CreateStrokePath()
    {
        return _hasUsableStroke
            ? RoundedPath.Create(StrokeBounds, StrokeRadius)
            : new GraphicsPath();
    }

    internal GraphicsPath CreateInnerPath()
    {
        return HasUsableInterior
            ? RoundedPath.Create(InnerBounds, InnerRadius)
            : new GraphicsPath();
    }

    private static RectangleF Inset(RectangleF bounds, float amount)
    {
        var width = bounds.Width - (amount * 2f);
        var height = bounds.Height - (amount * 2f);
        if (width <= 0f || height <= 0f)
        {
            return RectangleF.Empty;
        }

        return new RectangleF(bounds.Left + amount, bounds.Top + amount, width, height);
    }

    private static CornerRadius Subtract(CornerRadius radius, float amount)
    {
        return new CornerRadius(
            Math.Max(0f, radius.TopLeft - amount),
            Math.Max(0f, radius.TopRight - amount),
            Math.Max(0f, radius.BottomRight - amount),
            Math.Max(0f, radius.BottomLeft - amount));
    }

    private static bool Fits(CornerRadius radius, RectangleF bounds)
    {
        return radius.TopLeft + radius.TopRight <= bounds.Width + FitTolerance &&
            radius.BottomLeft + radius.BottomRight <= bounds.Width + FitTolerance &&
            radius.TopLeft + radius.BottomLeft <= bounds.Height + FitTolerance &&
            radius.TopRight + radius.BottomRight <= bounds.Height + FitTolerance;
    }

    private static int ResolveCornerInset(float radius, float borderWidth, float guard)
    {
        var minimum = borderWidth + guard;
        var tangent = radius - (Math.Max(0f, radius - minimum) / (float)Math.Sqrt(2d));
        return (int)Math.Ceiling(Math.Max(minimum, tangent));
    }

    private static Padding EmptyContentInsets(RectangleF bounds)
    {
        return new Padding(
            (int)Math.Ceiling(bounds.Width / 2f),
            (int)Math.Ceiling(bounds.Height / 2f),
            (int)Math.Ceiling(bounds.Width / 2f),
            (int)Math.Ceiling(bounds.Height / 2f));
    }

    private static bool IsUsable(RectangleF bounds)
    {
        return bounds.Width > 0f && bounds.Height > 0f &&
            IsFinite(bounds.Left) && IsFinite(bounds.Top) &&
            IsFinite(bounds.Width) && IsFinite(bounds.Height);
    }

    private static void ValidateFiniteNonNegative(float value, string parameterName)
    {
        if (!IsFinite(value) || value < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and non-negative.");
        }
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
