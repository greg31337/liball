using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents workbook relationship resolver data used by the public workbook API.
/// </summary>
public class XlsxWorkbookRelationshipResolver : IXlsxWorkbookRelationshipResolver
{
    /// <summary>
    /// Initializes a new workbook relationship resolver with its default state.
    /// </summary>
    public XlsxWorkbookRelationshipResolver()
    {
    }

    /// <summary>
    /// Performs the resolve operation using the current model state.
    /// </summary>
    /// <param name = "workbook">The workbook model used by the operation.</param>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxWorkbookRelationshipResolutionResult Resolve(XlsxWorkbookPart workbook, XlsxRelationshipsPart relationships)
    {
        XlsxWorkbookRelationshipResolutionResult result = new XlsxWorkbookRelationshipResolutionResult();
        if (workbook == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.relationship.workbookNull", "Workbook part must not be null."));
            return result;
        }

        if (relationships == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.relationship.relationshipsNull", "Workbook relationships part must not be null."));
            return result;
        }

        CopyDiagnostics(result, workbook.Validation);
        CopyDiagnostics(result, relationships.Validation);
        foreach (XlsxRelationship relationship in relationships.Relationships)
        {
            XlsxWorkbookResolvedRelationship resolved = ResolveRelationship(workbook, relationship);
            AddResolvedRelationship(result, resolved);
        }

        ValidateWorkbookSheetReferences(result, workbook);
        ValidateWorkbookPivotCacheReferences(result, workbook);
        result.IsValid = result.Validation.IsValid;
        return result;
    }

    internal static XlsxWorkbookResolvedRelationship ResolveRelationship(XlsxWorkbookPart workbook, XlsxRelationship relationship)
    {
        XlsxWorkbookResolvedRelationship resolved = new XlsxWorkbookResolvedRelationship();
        resolved.Relationship = relationship;
        resolved.Id = relationship.Id;
        resolved.Type = relationship.Type;
        resolved.Target = relationship.Target;
        resolved.TargetPath = relationship.TargetPath;
        resolved.TargetFragment = relationship.TargetFragment;
        resolved.IsExternal = relationship.IsExternal;
        resolved.ExternalTarget = relationship.ExternalTarget;
        resolved.Kind = GetRelationshipKind(relationship.Type);
        if (IsSheetRelationshipKind(resolved.Kind))
        {
            XlsxWorkbookSheet? sheet = FindSheetByRelationshipId(workbook, relationship.Id);
            if (sheet != null)
            {
                resolved.Sheet = sheet;
                resolved.HasSheet = true;
                ApplySheetMetadata(sheet, resolved);
            }
        }

        return resolved;
    }

    internal static void ApplySheetMetadata(XlsxWorkbookSheet sheet, XlsxWorkbookResolvedRelationship relationship)
    {
        sheet.SheetType = GetSheetType(relationship.Kind);
        sheet.RelationshipType = relationship.Type;
        sheet.TargetPath = relationship.TargetPath;
        sheet.ContentType = GetSheetContentType(sheet.SheetType);
        sheet.HasRelationshipMetadata = true;
    }

    internal static void AddResolvedRelationship(XlsxWorkbookRelationshipResolutionResult result, XlsxWorkbookResolvedRelationship resolved)
    {
        result.Relationships.Add(resolved);
        if (resolved.Kind == XlsxWorkbookRelationshipKind.Worksheet)
        {
            result.Worksheets.Add(resolved);
        }
        else if (resolved.Kind == XlsxWorkbookRelationshipKind.Chartsheet)
        {
            result.Chartsheets.Add(resolved);
        }
        else if (resolved.Kind == XlsxWorkbookRelationshipKind.Dialogsheet)
        {
            result.Dialogsheets.Add(resolved);
        }
        else if (resolved.Kind == XlsxWorkbookRelationshipKind.Macrosheet)
        {
            result.Macrosheets.Add(resolved);
        }
        else if (resolved.Kind == XlsxWorkbookRelationshipKind.Theme)
        {
            SetSingleton(result, resolved, 1);
        }
        else if (resolved.Kind == XlsxWorkbookRelationshipKind.Styles)
        {
            SetSingleton(result, resolved, 2);
        }
        else if (resolved.Kind == XlsxWorkbookRelationshipKind.SharedStrings)
        {
            SetSingleton(result, resolved, 3);
        }
        else if (resolved.Kind == XlsxWorkbookRelationshipKind.ExternalLink)
        {
            result.ExternalLinks.Add(resolved);
        }
        else if (resolved.Kind == XlsxWorkbookRelationshipKind.Connections)
        {
            result.Connections.Add(resolved);
        }
        else if (resolved.Kind == XlsxWorkbookRelationshipKind.PivotCache)
        {
            result.PivotCaches.Add(resolved);
        }
        else if (resolved.Kind == XlsxWorkbookRelationshipKind.VbaProject)
        {
            SetSingleton(result, resolved, 4);
        }
        else
        {
            result.UnknownRelationships.Add(resolved);
        }
    }

    internal static void SetSingleton(XlsxWorkbookRelationshipResolutionResult result, XlsxWorkbookResolvedRelationship resolved, int target)
    {
        if (target == 1)
        {
            if (result.HasTheme)
            {
                AddDuplicateSingletonDiagnostic(result, resolved, "theme");
            }
            else
            {
                result.Theme = resolved;
                result.HasTheme = true;
            }
        }
        else if (target == 2)
        {
            if (result.HasStyles)
            {
                AddDuplicateSingletonDiagnostic(result, resolved, "styles");
            }
            else
            {
                result.Styles = resolved;
                result.HasStyles = true;
            }
        }
        else if (target == 3)
        {
            if (result.HasSharedStrings)
            {
                AddDuplicateSingletonDiagnostic(result, resolved, "sharedStrings");
            }
            else
            {
                result.SharedStrings = resolved;
                result.HasSharedStrings = true;
            }
        }
        else if (target == 4)
        {
            if (result.HasVbaProject)
            {
                AddDuplicateSingletonDiagnostic(result, resolved, "vbaProject");
            }
            else
            {
                result.VbaProject = resolved;
                result.HasVbaProject = true;
            }
        }
    }

    internal static void AddDuplicateSingletonDiagnostic(XlsxWorkbookRelationshipResolutionResult result, XlsxWorkbookResolvedRelationship resolved, string name)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.relationship.duplicateSingleton", "Workbook relationships contain more than one singleton relationship.");
        diagnostic.SetLocation(XlsxPackagePaths.WorkbookRelationships, "Relationship", "Type");
        diagnostic.SetRelationshipId(resolved.Id);
        result.AddDiagnostic(diagnostic);
    }

    internal static void ValidateWorkbookSheetReferences(XlsxWorkbookRelationshipResolutionResult result, XlsxWorkbookPart workbook)
    {
        foreach (XlsxWorkbookSheet sheet in workbook.Sheets)
        {
            XlsxWorkbookResolvedRelationship? resolved = result.GetRelationship(sheet.RelationshipId);
            if (resolved == null)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.relationship.sheetRelationshipMissing", "Workbook sheet references a missing relationship.");
                diagnostic.SetLocation(XlsxPackagePaths.Workbook, "sheet", "r:id");
                diagnostic.SetRelationshipId(sheet.RelationshipId);
                result.AddDiagnostic(diagnostic);
                continue;
            }

            if (!IsSheetRelationshipKind(resolved.Kind))
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.relationship.sheetRelationshipType", "Workbook sheet relationship must target a sheet part.");
                diagnostic.SetLocation(XlsxPackagePaths.WorkbookRelationships, "Relationship", "Type");
                diagnostic.SetRelationshipId(sheet.RelationshipId);
                result.AddDiagnostic(diagnostic);
            }
        }

        foreach (XlsxWorkbookResolvedRelationship relationship in result.Worksheets)
        {
            AddMissingSheetDiagnostic(result, relationship);
        }

        foreach (XlsxWorkbookResolvedRelationship relationship in result.Chartsheets)
        {
            AddMissingSheetDiagnostic(result, relationship);
        }

        foreach (XlsxWorkbookResolvedRelationship relationship in result.Dialogsheets)
        {
            AddMissingSheetDiagnostic(result, relationship);
        }

        foreach (XlsxWorkbookResolvedRelationship relationship in result.Macrosheets)
        {
            AddMissingSheetDiagnostic(result, relationship);
        }
    }

    internal static void ValidateWorkbookPivotCacheReferences(XlsxWorkbookRelationshipResolutionResult result, XlsxWorkbookPart workbook)
    {
        if (workbook.PivotCaches == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.relationship.pivotCachesNull", "Workbook pivot cache list must not be null."));
            return;
        }

        foreach (XlsxWorkbookPivotCacheReference cache in workbook.PivotCaches)
        {
            if (cache == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.relationship.pivotCacheNull", "Workbook pivot cache list must not contain null entries."));
                continue;
            }

            XlsxWorkbookResolvedRelationship? resolved = result.GetRelationship(cache.RelationshipId);
            if (resolved == null)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.relationship.pivotCacheRelationshipMissing", "Workbook pivotCache references a missing relationship.");
                diagnostic.SetLocation(XlsxPackagePaths.Workbook, "pivotCache", "r:id");
                diagnostic.SetRelationshipId(cache.RelationshipId);
                result.AddDiagnostic(diagnostic);
                continue;
            }

            if (resolved.Kind != XlsxWorkbookRelationshipKind.PivotCache)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.relationship.pivotCacheRelationshipType", "Workbook pivotCache relationship must target a pivot cache definition part.");
                diagnostic.SetLocation(XlsxPackagePaths.WorkbookRelationships, "Relationship", "Type");
                diagnostic.SetRelationshipId(cache.RelationshipId);
                result.AddDiagnostic(diagnostic);
                continue;
            }

            cache.Relationship = resolved.Relationship;
            cache.HasRelationship = true;
            cache.Target = resolved.Target;
            cache.TargetPath = resolved.TargetPath;
        }
    }

    internal static void AddMissingSheetDiagnostic(XlsxWorkbookRelationshipResolutionResult result, XlsxWorkbookResolvedRelationship relationship)
    {
        if (relationship.HasSheet)
        {
            return;
        }

        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.relationship.sheetMissing", "Workbook relationship targets a sheet part that is not listed in workbook sheets.");
        diagnostic.SetLocation(XlsxPackagePaths.WorkbookRelationships, "Relationship", "Id");
        diagnostic.SetRelationshipId(relationship.Id);
        result.AddDiagnostic(diagnostic);
    }

    internal static XlsxWorkbookSheet? FindSheetByRelationshipId(XlsxWorkbookPart workbook, string? relationshipId)
    {
        foreach (XlsxWorkbookSheet sheet in workbook.Sheets)
        {
            if (sheet.RelationshipId == relationshipId)
            {
                return sheet;
            }
        }

        return null;
    }

    internal static bool IsSheetRelationshipKind(XlsxWorkbookRelationshipKind kind)
    {
        if (kind == XlsxWorkbookRelationshipKind.Worksheet || kind == XlsxWorkbookRelationshipKind.Chartsheet || kind == XlsxWorkbookRelationshipKind.Dialogsheet || kind == XlsxWorkbookRelationshipKind.Macrosheet)
        {
            return true;
        }

        return false;
    }

    internal static XlsxSheetType GetSheetType(XlsxWorkbookRelationshipKind kind)
    {
        if (kind == XlsxWorkbookRelationshipKind.Worksheet)
        {
            return XlsxSheetType.Worksheet;
        }

        if (kind == XlsxWorkbookRelationshipKind.Chartsheet)
        {
            return XlsxSheetType.Chartsheet;
        }

        if (kind == XlsxWorkbookRelationshipKind.Macrosheet)
        {
            return XlsxSheetType.Macrosheet;
        }

        if (kind == XlsxWorkbookRelationshipKind.Dialogsheet)
        {
            return XlsxSheetType.Dialogsheet;
        }

        return XlsxSheetType.Unknown;
    }

    internal static string GetSheetContentType(XlsxSheetType sheetType)
    {
        if (sheetType == XlsxSheetType.Worksheet)
        {
            return XlsxContentTypes.Worksheet;
        }

        if (sheetType == XlsxSheetType.Chartsheet)
        {
            return XlsxContentTypes.Chartsheet;
        }

        if (sheetType == XlsxSheetType.Macrosheet)
        {
            return XlsxContentTypes.Macrosheet;
        }

        if (sheetType == XlsxSheetType.Dialogsheet)
        {
            return XlsxContentTypes.Dialogsheet;
        }

        return "";
    }

    internal static XlsxWorkbookRelationshipKind GetRelationshipKind(string? relationshipType)
    {
        string normalizedType = XlsxRelationshipTypeCompatibility.NormalizeRelationshipType(relationshipType);
        if (normalizedType == XlsxRelationshipTypes.Worksheet)
        {
            return XlsxWorkbookRelationshipKind.Worksheet;
        }

        if (normalizedType == XlsxRelationshipTypes.Chartsheet)
        {
            return XlsxWorkbookRelationshipKind.Chartsheet;
        }

        if (normalizedType == XlsxRelationshipTypes.Dialogsheet)
        {
            return XlsxWorkbookRelationshipKind.Dialogsheet;
        }

        if (normalizedType == XlsxRelationshipTypes.Macrosheet)
        {
            return XlsxWorkbookRelationshipKind.Macrosheet;
        }

        if (normalizedType == XlsxRelationshipTypes.Theme)
        {
            return XlsxWorkbookRelationshipKind.Theme;
        }

        if (normalizedType == XlsxRelationshipTypes.Styles)
        {
            return XlsxWorkbookRelationshipKind.Styles;
        }

        if (normalizedType == XlsxRelationshipTypes.SharedStrings)
        {
            return XlsxWorkbookRelationshipKind.SharedStrings;
        }

        if (normalizedType == XlsxRelationshipTypes.ExternalLink)
        {
            return XlsxWorkbookRelationshipKind.ExternalLink;
        }

        if (normalizedType == XlsxRelationshipTypes.Connections)
        {
            return XlsxWorkbookRelationshipKind.Connections;
        }

        if (normalizedType == XlsxRelationshipTypes.PivotCacheDefinition)
        {
            return XlsxWorkbookRelationshipKind.PivotCache;
        }

        if (normalizedType == XlsxRelationshipTypes.VbaProject)
        {
            return XlsxWorkbookRelationshipKind.VbaProject;
        }

        return XlsxWorkbookRelationshipKind.Unknown;
    }

    internal static void CopyDiagnostics(XlsxWorkbookRelationshipResolutionResult result, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
