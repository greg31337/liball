using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML relationships part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxRelationshipsPart
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the source part path value used by this model.
    /// </summary>
    public string SourcePartPath;
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Contains relationships.
    /// </summary>
    public List<XlsxRelationship> Relationships;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Stores the number of relationship element items processed by the operation.
    /// </summary>
    public int RelationshipElementCount;
    /// <summary>
    /// Initializes a new relationships part with its default state.
    /// </summary>
    public XlsxRelationshipsPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        SourcePartPath = "";
        PartPath = "";
        Relationships = new List<XlsxRelationship>();
        UnknownXml = new List<string>();
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        RelationshipElementCount = 0;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    /// <summary>
    /// Returns relationship from the current model.
    /// </summary>
    /// <param name = "id">The id value used by the operation.</param>
    /// <returns>The relationship produced by the operation.</returns>
    internal XlsxRelationship? GetRelationship(string? id)
    {
        foreach (XlsxRelationship relationship in Relationships)
        {
            if (relationship.Id == id)
            {
                return relationship;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns first by type from the current model.
    /// </summary>
    /// <param name = "relationshipType">The relationship type value used by the operation.</param>
    /// <returns>The relationship produced by the operation.</returns>
    internal XlsxRelationship? GetFirstByType(string relationshipType)
    {
        foreach (XlsxRelationship relationship in Relationships)
        {
            if (XlsxRelationshipTypeCompatibility.MatchesRelationshipType(relationship.Type, relationshipType))
            {
                return relationship;
            }
        }

        return null;
    }
}
