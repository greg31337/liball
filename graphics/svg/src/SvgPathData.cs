namespace svg;

/// <summary>Provides shared SVG path command grammar facts.</summary>
internal static class SvgPathCommandRules
{
    /// <summary>Tests whether a command kind is defined.</summary>
    /// <param name="kind">The command kind.</param>
    /// <returns><c>true</c> for a defined path command.</returns>
    public static bool IsKnown(SvgPathCommandKind kind)
    {
        return kind >= SvgPathCommandKind.MoveTo && kind <= SvgPathCommandKind.ArcTo;
    }

    /// <summary>Returns the numeric payload count for one command.</summary>
    /// <param name="kind">The command kind.</param>
    /// <returns>The exact payload count, or <c>-1</c>.</returns>
    public static int ParameterCount(SvgPathCommandKind kind)
    {
        if (kind == SvgPathCommandKind.ClosePath) return 0;
        if (kind == SvgPathCommandKind.HorizontalLineTo || kind == SvgPathCommandKind.VerticalLineTo) return 1;
        if (kind == SvgPathCommandKind.MoveTo || kind == SvgPathCommandKind.LineTo || kind == SvgPathCommandKind.SmoothQuadraticCurveTo) return 2;
        if (kind == SvgPathCommandKind.SmoothCubicCurveTo || kind == SvgPathCommandKind.QuadraticCurveTo) return 4;
        if (kind == SvgPathCommandKind.CubicCurveTo) return 6;
        if (kind == SvgPathCommandKind.ArcTo) return 7;
        return -1;
    }
}
