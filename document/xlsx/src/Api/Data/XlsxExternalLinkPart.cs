using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML external link part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxExternalLinkPart
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
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Stores the relationship id value used by this model.
    /// </summary>
    public string? RelationshipId;
    /// <summary>
    /// Stores the target value used by this model.
    /// </summary>
    public string? Target;
    /// <summary>
    /// Stores the target mode value used by this model.
    /// </summary>
    public string? TargetMode;
    /// <summary>
    /// Indicates whether relationship is present and should be written.
    /// </summary>
    public bool HasRelationship;
    /// <summary>
    /// Stores the relationship value used by this model.
    /// </summary>
    public XlsxRelationship? Relationship;
    /// <summary>
    /// Contains sheet names.
    /// </summary>
    public List<string> SheetNames;
    /// <summary>
    /// Initializes a new external link part with its default state.
    /// </summary>
    public XlsxExternalLinkPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "/xl/externalLinks/externalLink1.xml";
        RawXml = "";
        RelationshipId = "";
        Target = "";
        TargetMode = "";
        HasRelationship = false;
        Relationship = null;
        SheetNames = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    /// <summary>
    /// Performs the resolve external book relationship operation using the current model state.
    /// </summary>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    internal void ResolveExternalBookRelationship(XlsxRelationshipsPart relationships)
    {
        if (relationships == null)
        {
            return;
        }

        XlsxRelationship? relationship = relationships.GetFirstByType(XlsxRelationshipTypes.ExternalLinkPath);
        if (relationship == null && RelationshipId != null && RelationshipId.Length > 0)
        {
            relationship = relationships.GetRelationship(RelationshipId);
        }

        if (relationship == null)
        {
            return;
        }

        Relationship = relationship;
        HasRelationship = true;
        RelationshipId = relationship.Id;
        Target = relationship.Target;
        TargetMode = relationship.TargetMode;
    }
}
