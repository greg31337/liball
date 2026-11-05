namespace ttf;

/// <summary>Contains one preserved CFF stem-hint operand pair.</summary>
/// <remarks>
/// The pair is exposed as encoded design-space data. It is not applied by a
/// hint interpreter or rasterizer.
/// </remarks>
public class CffStemHint : ICffStemHint
{
    bool vertical;
    double positionDelta;
    double width;

    internal CffStemHint(Type2StemHint hint)
    {
        vertical = hint.IsVertical();
        positionDelta = hint.GetPositionDelta();
        width = hint.GetWidth();
    }

    /// <summary>Reports whether this is a vertical stem hint.</summary>
    /// <returns>True for a vertical stem; false for a horizontal stem.</returns>
    public bool IsVertical() { return vertical; }

    /// <summary>Gets the encoded relative stem position.</summary>
    /// <returns>The position delta in design units.</returns>
    public double GetPositionDelta() { return positionDelta; }

    /// <summary>Gets the encoded stem width.</summary>
    /// <returns>The width in design units.</returns>
    public double GetWidth() { return width; }
}
