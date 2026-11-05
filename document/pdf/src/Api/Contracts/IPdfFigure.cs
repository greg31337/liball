namespace pdf;

/// <summary>
/// Configures figure-specific layout behavior.
/// </summary>
public interface IPdfFigure
{
    /// <summary>
    /// Sets the spacing between figure content and its caption.
    /// </summary>
    /// <param name="spacing">The non-negative finite spacing in PDF points.</param>
    void SetCaptionSpacing(double spacing);
}
