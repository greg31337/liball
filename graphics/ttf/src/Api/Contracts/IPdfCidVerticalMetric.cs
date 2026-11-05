namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfCidVerticalMetric"/>.</summary>
public interface IPdfCidVerticalMetric
{
    /// <summary>Gets the vertical displacement component <c>w1y</c>.</summary>
    /// <returns>The normally negative displacement in PDF 1000-unit space.</returns>
    int GetDisplacementY();

    /// <summary>Gets the vertical position-vector component <c>v1x</c>.</summary>
    /// <returns>The horizontal origin in PDF 1000-unit space.</returns>
    int GetOriginX();

    /// <summary>Gets the vertical position-vector component <c>v1y</c>.</summary>
    /// <returns>The vertical origin in PDF 1000-unit space.</returns>
    int GetOriginY();
}
