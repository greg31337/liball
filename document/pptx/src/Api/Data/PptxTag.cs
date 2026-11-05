namespace pptx;

/// <summary>
/// Represents tag data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxTag
{
    /// <summary>
    /// Stores the name associated with this object.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the value associated with this object.
    /// </summary>
    public string Value;

    /// <summary>
    /// Initializes a new tag using the supplied name and value.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="value">The value to use.</param>
    public PptxTag(string name, string value)
    {
        Name = PptxDiagnostic.CleanText(name);
        Value = PptxDiagnostic.CleanText(value);
    }
}

