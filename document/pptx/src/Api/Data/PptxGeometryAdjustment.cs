namespace pptx;

/// <summary>
/// Stores a named constant adjustment value for preset shape geometry.
/// </summary>
public class PptxGeometryAdjustment
{
    /// <summary>
    /// Stores the DrawingML adjustment name.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the constant adjustment value.
    /// </summary>
    public int Value;

    /// <summary>
    /// Initializes a named constant adjustment value.
    /// </summary>
    /// <param name="name">The DrawingML adjustment name.</param>
    /// <param name="value">The constant adjustment value.</param>
    public PptxGeometryAdjustment(string name, int value)
    {
        string clean = PptxDiagnostic.CleanText(name);
        if (clean.Length == 0)
        {
            throw new System.ArgumentException("Adjustment name is required.");
        }

        Name = clean;
        Value = value;
    }
}
