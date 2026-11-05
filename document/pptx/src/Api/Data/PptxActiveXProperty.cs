namespace pptx;

/// <summary>
/// Represents one named inline property persisted by an ActiveX control.
/// </summary>
public class PptxActiveXProperty
{
    /// <summary>
    /// Stores the persisted property name.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the persisted property value.
    /// </summary>
    public string Value;

    /// <summary>
    /// Initializes an empty ActiveX property.
    /// </summary>
    public PptxActiveXProperty()
    {
        Name = "";
        Value = "";
    }
}
