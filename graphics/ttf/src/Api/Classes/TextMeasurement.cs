namespace ttf;

/// <summary>Contains a scaled advance vector and exact positioned ink bounds.</summary>
public class TextMeasurement : ITextMeasurement
{
    double pointSize;
    double xAdvance;
    double yAdvance;
    TextInkBounds inkBounds;

    internal TextMeasurement(
        double pointSize,
        double xAdvance,
        double yAdvance,
        TextInkBounds inkBounds)
    {
        this.pointSize = pointSize;
        this.xAdvance = xAdvance;
        this.yAdvance = yAdvance;
        this.inkBounds = inkBounds;
    }

    /// <summary>Gets the point size used for scaling.</summary>
    /// <returns>The point size.</returns>
    public double GetPointSize()
    {
        return pointSize;
    }

    /// <summary>Gets the signed horizontal advance.</summary>
    /// <returns>The horizontal advance.</returns>
    public double GetXAdvance()
    {
        return xAdvance;
    }

    /// <summary>Gets the signed vertical advance.</summary>
    /// <returns>The vertical advance.</returns>
    public double GetYAdvance()
    {
        return yAdvance;
    }

    /// <summary>Gets the exact scaled positioned ink bounds.</summary>
    /// <returns>The immutable ink bounds.</returns>
    public TextInkBounds GetInkBounds()
    {
        return inkBounds;
    }
}
