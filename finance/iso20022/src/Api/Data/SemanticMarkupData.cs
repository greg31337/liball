using System.Collections.Generic;

namespace iso20022;

/// <summary>Represents structured semantic markup, documentation, or one named tuple element.</summary>
public class SemanticMarkupData
{
    /// <summary>The XMI identifier when the markup is represented as a model entity.</summary>
    public string Identifier = string.Empty;
    /// <summary>The open markup or documentation type, such as <c>synonym</c> or <c>scope</c>.</summary>
    public string Type = string.Empty;
    /// <summary>The tuple element name, or an empty string for a containing markup tuple.</summary>
    public string Name = string.Empty;
    /// <summary>The tuple element or documentation content.</summary>
    public string Value = string.Empty;
    /// <summary>The ordered elements contained by a structured markup tuple.</summary>
    public List<SemanticMarkupData> Elements = new List<SemanticMarkupData>();
    /// <summary>Unrecognized fields attached to the markup.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the markup.</summary>
    public SourceLocation Location = new SourceLocation();
}
