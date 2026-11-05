using System;

namespace xlsx;

/// <summary>
/// Describes one externally sourced or externally targeted workbook item.
/// </summary>
public class XlsxExternalContentItem
{
    /// <summary>Stores the external-content kind.</summary>
    public XlsxExternalContentKind Kind;
    /// <summary>Stores the package part containing the reference.</summary>
    public string SourcePartPath;
    /// <summary>Stores a cell, range, connection, or query name when available.</summary>
    public string SourceReference;
    /// <summary>Stores the relationship identifier when available.</summary>
    public string RelationshipId;
    /// <summary>Stores the external target or formula reference.</summary>
    public string Target;

    /// <summary>Initializes an empty external-content item.</summary>
    public XlsxExternalContentItem()
    {
        Kind = XlsxExternalContentKind.ExternalLink;
        SourcePartPath = "";
        SourceReference = "";
        RelationshipId = "";
        Target = "";
    }
}
