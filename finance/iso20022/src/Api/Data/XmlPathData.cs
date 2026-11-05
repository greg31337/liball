using System.Collections.Generic;

namespace iso20022;

/// <summary>Represents a qualified path through an XML document.</summary>
public class XmlPathData
{
    /// <summary>The ordered qualified-name segments.</summary>
    public List<QualifiedNameData> Segments = new List<QualifiedNameData>();
    /// <summary>A stable display form of the path.</summary>
    public string Value = string.Empty;
}
