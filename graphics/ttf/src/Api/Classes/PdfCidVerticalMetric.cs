namespace ttf;

/// <summary>Contains one PDF vertical displacement and position vector.</summary>
public class PdfCidVerticalMetric : IPdfCidVerticalMetric
{
    int displacementY;
    int originX;
    int originY;

    internal PdfCidVerticalMetric(
        int displacementY,
        int originX,
        int originY)
    {
        this.displacementY = displacementY;
        this.originX = originX;
        this.originY = originY;
    }

    /// <summary>Gets the vertical displacement component <c>w1y</c>.</summary>
    /// <returns>The normally negative displacement in PDF 1000-unit space.</returns>
    public int GetDisplacementY()
    {
        return displacementY;
    }

    /// <summary>Gets the vertical position-vector component <c>v1x</c>.</summary>
    /// <returns>The horizontal origin in PDF 1000-unit space.</returns>
    public int GetOriginX()
    {
        return originX;
    }

    /// <summary>Gets the vertical position-vector component <c>v1y</c>.</summary>
    /// <returns>The vertical origin in PDF 1000-unit space.</returns>
    public int GetOriginY()
    {
        return originY;
    }
}
