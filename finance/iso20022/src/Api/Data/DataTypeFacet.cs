using System.Collections.Generic;

namespace iso20022;

/// <summary>Represents one lexical, numeric, enumeration, whitespace, or size facet of a data type.</summary>
public class DataTypeFacet
{
    /// <summary>The facet name exactly as defined by the source metamodel.</summary>
    public string Name = string.Empty;
    /// <summary>The exact lexical facet value.</summary>
    public string Value = string.Empty;
    /// <summary>Whether the facet has a parsed signed-integer value.</summary>
    public bool HasIntegerValue;
    /// <summary>The parsed signed-integer value when available.</summary>
    public int IntegerValue;
    /// <summary>The parsed decimal value when the facet is numeric.</summary>
    public IsoDecimalValue? DecimalValue;
    /// <summary>The ordered values of an enumeration facet.</summary>
    public List<string> EnumerationValues = new List<string>();
    /// <summary>The source location of the facet.</summary>
    public SourceLocation Location = new SourceLocation();
}
