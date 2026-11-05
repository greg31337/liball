namespace pdf;

/// <summary>
/// Configures chart-placeholder-specific drawing behavior.
/// </summary>
public interface IPdfChartPlaceholder
{
    /// <summary>
    /// Applies a defensive copy of the text style used for the placeholder label.
    /// </summary>
    /// <param name="style">The label text style.</param>
    void SetTextStyle(PdfTextStyle style);
}
