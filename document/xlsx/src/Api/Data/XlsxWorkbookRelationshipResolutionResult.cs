using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a workbook relationship resolution operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorkbookRelationshipResolutionResult
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
    /// Contains relationships.
    /// </summary>
    public List<XlsxWorkbookResolvedRelationship> Relationships;
    /// <summary>
    /// Contains worksheets.
    /// </summary>
    public List<XlsxWorkbookResolvedRelationship> Worksheets;
    /// <summary>
    /// Contains chartsheets.
    /// </summary>
    public List<XlsxWorkbookResolvedRelationship> Chartsheets;
    /// <summary>
    /// Contains dialogsheets.
    /// </summary>
    public List<XlsxWorkbookResolvedRelationship> Dialogsheets;
    /// <summary>
    /// Contains macrosheets.
    /// </summary>
    public List<XlsxWorkbookResolvedRelationship> Macrosheets;
    /// <summary>
    /// Contains external links.
    /// </summary>
    public List<XlsxWorkbookResolvedRelationship> ExternalLinks;
    /// <summary>
    /// Contains connections.
    /// </summary>
    public List<XlsxWorkbookResolvedRelationship> Connections;
    /// <summary>
    /// Contains pivot caches.
    /// </summary>
    public List<XlsxWorkbookResolvedRelationship> PivotCaches;
    /// <summary>
    /// Contains unknown relationships.
    /// </summary>
    public List<XlsxWorkbookResolvedRelationship> UnknownRelationships;
    /// <summary>
    /// Stores the theme value used by this model.
    /// </summary>
    public XlsxWorkbookResolvedRelationship? Theme;
    /// <summary>
    /// Indicates whether theme is present and should be written.
    /// </summary>
    public bool HasTheme;
    /// <summary>
    /// Contains styles.
    /// </summary>
    public XlsxWorkbookResolvedRelationship? Styles;
    /// <summary>
    /// Indicates whether styles is present and should be written.
    /// </summary>
    public bool HasStyles;
    /// <summary>
    /// Contains shared strings.
    /// </summary>
    public XlsxWorkbookResolvedRelationship? SharedStrings;
    /// <summary>
    /// Indicates whether shared strings is present and should be written.
    /// </summary>
    public bool HasSharedStrings;
    /// <summary>
    /// Stores the vba project value used by this model.
    /// </summary>
    public XlsxWorkbookResolvedRelationship? VbaProject;
    /// <summary>
    /// Indicates whether vba project is present and should be written.
    /// </summary>
    public bool HasVbaProject;
    /// <summary>
    /// Initializes a new workbook relationship resolution result with its default state.
    /// </summary>
    public XlsxWorkbookRelationshipResolutionResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Relationships = new List<XlsxWorkbookResolvedRelationship>();
        Worksheets = new List<XlsxWorkbookResolvedRelationship>();
        Chartsheets = new List<XlsxWorkbookResolvedRelationship>();
        Dialogsheets = new List<XlsxWorkbookResolvedRelationship>();
        Macrosheets = new List<XlsxWorkbookResolvedRelationship>();
        ExternalLinks = new List<XlsxWorkbookResolvedRelationship>();
        Connections = new List<XlsxWorkbookResolvedRelationship>();
        PivotCaches = new List<XlsxWorkbookResolvedRelationship>();
        UnknownRelationships = new List<XlsxWorkbookResolvedRelationship>();
        Theme = null;
        HasTheme = false;
        Styles = null;
        HasStyles = false;
        SharedStrings = null;
        HasSharedStrings = false;
        VbaProject = null;
        HasVbaProject = false;
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
    /// <returns>The workbook resolved relationship produced by the operation.</returns>
    internal XlsxWorkbookResolvedRelationship? GetRelationship(string? id)
    {
        foreach (XlsxWorkbookResolvedRelationship relationship in Relationships)
        {
            if (relationship.Id == id)
            {
                return relationship;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns first by kind from the current model.
    /// </summary>
    /// <param name = "kind">The kind value used by the operation.</param>
    /// <returns>The workbook resolved relationship produced by the operation.</returns>
    internal XlsxWorkbookResolvedRelationship? GetFirstByKind(XlsxWorkbookRelationshipKind kind)
    {
        foreach (XlsxWorkbookResolvedRelationship relationship in Relationships)
        {
            if (relationship.Kind == kind)
            {
                return relationship;
            }
        }

        return null;
    }
}
