namespace barcode;

/// <summary>
/// Holds one positioned human-readable text run.
/// </summary>
public class BarcodeTextRun
{
    /// <summary>
    /// The text displayed by the run.
    /// </summary>
    public string? Text;

    /// <summary>
    /// The horizontal position of the run in layout units.
    /// </summary>
    public double X;

    /// <summary>
    /// The baseline position of the run in layout units.
    /// </summary>
    public double BaselineY;

    /// <summary>
    /// The allocated width of the run in layout units.
    /// </summary>
    public double Width;

    /// <summary>
    /// The allocated height of the run in layout units.
    /// </summary>
    public double Height;
}
