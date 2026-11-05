namespace odf;

/// <summary>
/// Describes a three-component vector used by three-dimensional drawings.
/// </summary>
public class OdfThreeDimensionalVector
{
    /// <summary>The preserved lexical vector value.</summary>
    public string LexicalValue = "";
    /// <summary>The first vector component.</summary>
    public double X;
    /// <summary>The second vector component.</summary>
    public double Y;
    /// <summary>The third vector component.</summary>
    public double Z;
    /// <summary>Whether the vector was specified.</summary>
    public bool IsSpecified;
    /// <summary>Whether the specified vector is valid.</summary>
    public bool IsValid;
    internal string OriginalLexicalValue = "";
    internal string OriginalCanonicalValue = "";
}
