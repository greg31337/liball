namespace odf;

/// <summary>
/// Describes the named preset attached to enhanced geometry.
/// </summary>
public class OdfShapePreset
{
    /// <summary>The preserved <c>draw:type</c> value.</summary>
    public string Name = "";
    /// <summary>Whether the geometry explicitly declares a preset.</summary>
    public bool IsSpecified;
    /// <summary>Whether the special non-primitive preset is selected.</summary>
    public bool IsNonPrimitive;
    internal string OriginalName = "";
}
