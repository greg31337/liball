using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>Validates workbook security limits and applies active-content policies.</summary>
public class XlsxWorkbookSecurityService : IXlsxWorkbookSecurityService
{
    /// <summary>Initializes a workbook security service.</summary>
    public XlsxWorkbookSecurityService()
    {
    }

    /// <summary>Validates resource counts and applies configured preserve, remove, or reject policies.</summary>
    /// <param name="workbook">The workbook to evaluate.</param>
    /// <param name="options">The security limits and content policies.</param>
    /// <returns>Detected counts, removals, and diagnostics.</returns>
    public XlsxSecurityPolicyResult Apply(XlsxWorkbook workbook, XlsxSecurityOptions? options)
    {
        XlsxSecurityPolicyResult result = new XlsxSecurityPolicyResult();
        if (workbook == null)
        {
            result.AddError("security.workbookNull", "Workbook must not be null.");
            return result;
        }
        XlsxSecurityOptions effectiveOptions = options == null ? new XlsxSecurityOptions() : options;
        XlsxValidationResult optionValidation = effectiveOptions.Validate();
        foreach (XlsxDiagnostic diagnostic in optionValidation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        if (!result.IsValid) { return result; }

        result.ImageCount = CountImages(workbook);
        result.CommentCount = CountComments(workbook);
        result.FormulaCount = CountFormulas(workbook);
        result.ExternalRelationshipCount = CountExternalRelationships(workbook);
        ValidateResourceCounts(result, effectiveOptions);

        int macroCount = workbook.CountMacros();
        if (macroCount == 0 && XlsxWorkbook.IsMacroEnabledPackageVariant(workbook.PackageVariant)) { macroCount = 1; }
        int oleCount = CountOleObjects(workbook);
        int externalImageCount = CountRelationships(workbook, XlsxRelationshipTypes.Image, true);
        int externalLinkCount = CountExternalLinks(workbook);
        int connectionCount = HasConnections(workbook) ? 1 : 0;
        int queryTableCount = CountQueryTables(workbook);
        int customXmlCount = CountCustomXmlParts(workbook);
        int unknownActiveCount = CountUnknownActiveParts(workbook);
        RejectDetectedContent(result, effectiveOptions.MacroPolicy, macroCount, "security.macrosRejected", "Workbook contains macros.");
        RejectDetectedContent(result, effectiveOptions.OleObjectPolicy, oleCount, "security.oleObjectsRejected", "Workbook contains OLE objects or embedded packages.");
        RejectDetectedContent(result, effectiveOptions.ExternalImagePolicy, externalImageCount, "security.externalImagesRejected", "Workbook contains external image relationships.");
        RejectDetectedContent(result, effectiveOptions.ExternalLinkPolicy, externalLinkCount, "security.externalLinksRejected", "Workbook contains external links.");
        RejectDetectedContent(result, effectiveOptions.ConnectionPolicy, connectionCount, "security.connectionsRejected", "Workbook contains connections.");
        RejectDetectedContent(result, effectiveOptions.QueryTablePolicy, queryTableCount, "security.queryTablesRejected", "Workbook contains query tables.");
        RejectDetectedContent(result, effectiveOptions.CustomXmlPolicy, customXmlCount, "security.customXmlRejected", "Workbook contains custom XML parts.");
        RejectDetectedContent(result, effectiveOptions.UnknownActiveContentPolicy, unknownActiveCount, "security.unknownActiveContentRejected", "Workbook contains unknown active content.");
        if (!result.IsValid) { return result; }

        if (effectiveOptions.MacroPolicy == XlsxSecurityContentPolicy.Remove && macroCount > 0)
        {
            XlsxValidationResult removal = workbook.RemoveMacros();
            foreach (XlsxDiagnostic diagnostic in removal.Diagnostics) { result.AddDiagnostic(diagnostic); }
            RemovePackagePartsWithPath(workbook, XlsxPackagePaths.VbaProject);
            result.MacrosRemoved = macroCount;
        }
        if (effectiveOptions.OleObjectPolicy == XlsxSecurityContentPolicy.Remove && oleCount > 0) { result.OleObjectsRemoved = RemoveOleObjects(workbook); }
        if (effectiveOptions.ExternalImagePolicy == XlsxSecurityContentPolicy.Remove && externalImageCount > 0) { result.ExternalImagesRemoved = RemoveRelationships(workbook, XlsxRelationshipTypes.Image, true); }
        if (effectiveOptions.ExternalLinkPolicy == XlsxSecurityContentPolicy.Remove && externalLinkCount > 0) { result.ExternalLinksRemoved = RemoveExternalLinks(workbook); }
        if (effectiveOptions.ConnectionPolicy == XlsxSecurityContentPolicy.Remove && connectionCount > 0) { result.ConnectionsRemoved = RemoveConnections(workbook); }
        if (effectiveOptions.QueryTablePolicy == XlsxSecurityContentPolicy.Remove && queryTableCount > 0) { result.QueryTablesRemoved = RemoveQueryTables(workbook); }
        if (effectiveOptions.CustomXmlPolicy == XlsxSecurityContentPolicy.Remove && customXmlCount > 0) { result.CustomXmlPartsRemoved = RemoveCustomXmlParts(workbook); }
        if (effectiveOptions.UnknownActiveContentPolicy == XlsxSecurityContentPolicy.Remove && unknownActiveCount > 0) { result.UnknownActivePartsRemoved = RemoveUnknownActiveParts(workbook); }
        SyncRelationshipParts(workbook, result);
        result.IsValid = result.Validation.IsValid;
        return result;
    }

    internal static void ValidateResourceCounts(XlsxSecurityPolicyResult result, XlsxSecurityOptions options)
    {
        if (result.ImageCount > options.MaxImages) { result.AddError("security.maxImagesExceeded", "Workbook image count exceeds the configured limit."); }
        if (result.CommentCount > options.MaxComments) { result.AddError("security.maxCommentsExceeded", "Workbook comment count exceeds the configured limit."); }
        if (result.FormulaCount > options.MaxFormulas) { result.AddError("security.maxFormulasExceeded", "Workbook formula count exceeds the configured limit."); }
        if (result.ExternalRelationshipCount > options.MaxExternalRelationships) { result.AddError("security.maxExternalRelationshipsExceeded", "Workbook external relationship count exceeds the configured limit."); }
    }

    internal static void RejectDetectedContent(XlsxSecurityPolicyResult result, XlsxSecurityContentPolicy policy, int count, string code, string message)
    {
        if (policy == XlsxSecurityContentPolicy.Reject && count > 0) { result.AddError(code, message); }
    }

    internal static int CountComments(XlsxWorkbook workbook)
    {
        int count = 0;
        if (workbook.Comments != null)
        {
            foreach (XlsxCommentsPart part in workbook.Comments) { if (part != null && part.Comments != null) { count = count + part.Comments.Count; } }
        }
        if (workbook.ThreadedComments != null)
        {
            foreach (XlsxThreadedCommentsPart part in workbook.ThreadedComments) { if (part != null && part.Comments != null) { count = count + part.Comments.Count; } }
        }
        int packageCount = CountPackageXmlElements(workbook, XlsxContentTypes.Comments, "comment") + CountPackageXmlElements(workbook, XlsxContentTypes.ThreadedComments, "threadedComment");
        return packageCount > count ? packageCount : count;
    }

    internal static int CountImages(XlsxWorkbook workbook)
    {
        int count = workbook.Images == null ? 0 : workbook.Images.Count;
        int packageCount = 0;
        if (workbook.PackageParts != null)
        {
            foreach (XlsxPackagePart part in workbook.PackageParts)
            {
                if (part != null && part.ContentType != null && part.ContentType.StartsWith("image/", StringComparison.Ordinal)) { packageCount = packageCount + 1; }
            }
        }
        return packageCount > count ? packageCount : count;
    }

    internal static int CountFormulas(XlsxWorkbook workbook)
    {
        int count = 0;
        if (workbook.Worksheets == null) { return count; }
        foreach (XlsxWorksheetPart worksheet in workbook.Worksheets)
        {
            if (worksheet == null || worksheet.Rows == null) { continue; }
            foreach (XlsxWorksheetRow row in worksheet.Rows)
            {
                if (row == null || row.Cells == null) { continue; }
                foreach (XlsxWorksheetCell cell in row.Cells) { if (cell != null && cell.HasFormula) { count = count + 1; } }
            }
        }
        int packageCount = CountPackageXmlElements(workbook, XlsxContentTypes.Worksheet, "f");
        return packageCount > count ? packageCount : count;
    }

    internal static int CountExternalRelationships(XlsxWorkbook workbook)
    {
        int count = CountExternalRelationshipsInPart(workbook.PackageRelationships) + CountExternalRelationshipsInPart(workbook.WorkbookRelationships);
        if (workbook.RelationshipParts == null) { return count; }
        foreach (XlsxRelationshipsPart part in workbook.RelationshipParts)
        {
            if (part == workbook.PackageRelationships || part == workbook.WorkbookRelationships) { continue; }
            count = count + CountExternalRelationshipsInPart(part);
        }
        return count;
    }

    internal static int CountExternalRelationshipsInPart(XlsxRelationshipsPart part)
    {
        int count = 0;
        if (part == null || part.Relationships == null) { return count; }
        foreach (XlsxRelationship relationship in part.Relationships) { if (IsExternalRelationship(relationship)) { count = count + 1; } }
        return count;
    }

    internal static int CountRelationships(XlsxWorkbook workbook, string relationshipType, bool externalOnly)
    {
        int count = CountRelationshipsInPart(workbook.PackageRelationships, relationshipType, externalOnly) + CountRelationshipsInPart(workbook.WorkbookRelationships, relationshipType, externalOnly);
        if (workbook.RelationshipParts == null) { return count; }
        foreach (XlsxRelationshipsPart part in workbook.RelationshipParts)
        {
            if (part == workbook.PackageRelationships || part == workbook.WorkbookRelationships) { continue; }
            count = count + CountRelationshipsInPart(part, relationshipType, externalOnly);
        }
        return count;
    }

    internal static int CountRelationshipsInPart(XlsxRelationshipsPart part, string relationshipType, bool externalOnly)
    {
        int count = 0;
        if (part == null || part.Relationships == null) { return count; }
        foreach (XlsxRelationship relationship in part.Relationships)
        {
            if (relationship == null || relationship.Type != relationshipType) { continue; }
            if (!externalOnly || IsExternalRelationship(relationship)) { count = count + 1; }
        }
        return count;
    }

    internal static bool IsExternalRelationship(XlsxRelationship relationship)
    {
        return relationship != null && (relationship.IsExternal || relationship.TargetMode == "External");
    }

    internal static int CountOleObjects(XlsxWorkbook workbook)
    {
        int count = workbook.EmbeddedObjects == null ? 0 : workbook.EmbeddedObjects.Count;
        int packageCount = CountPackagePartsWithPrefix(workbook, XlsxPackagePaths.EmbeddingsDirectory);
        int unknownCount = CountUnknownPartsWithPrefix(workbook, XlsxPackagePaths.EmbeddingsDirectory);
        if (packageCount > count) { count = packageCount; }
        if (unknownCount > count) { count = unknownCount; }
        int relationshipCount = CountRelationships(workbook, XlsxRelationshipTypes.OleObject, false) + CountRelationships(workbook, XlsxRelationshipTypes.EmbeddedPackage, false);
        if (relationshipCount > count) { count = relationshipCount; }
        return count;
    }

    internal static int CountExternalLinks(XlsxWorkbook workbook)
    {
        int count = workbook.ExternalLinks == null ? 0 : workbook.ExternalLinks.Count;
        if (count == 0) { count = CountRelationships(workbook, XlsxRelationshipTypes.ExternalLink, false); }
        count = count + CountRelationships(workbook, XlsxRelationshipTypes.ExternalLinkPath, false);
        return count;
    }

    internal static bool HasConnections(XlsxWorkbook workbook)
    {
        if (workbook.HasConnections) { return true; }
        if (workbook.Connections != null && (workbook.Connections.RawXml.Length > 0 || workbook.Connections.Connections.Count > 0)) { return true; }
        return CountUnknownPartsWithPath(workbook, XlsxPackagePaths.Connections) > 0 || CountPackagePartsWithPath(workbook, XlsxPackagePaths.Connections) > 0 || CountRelationships(workbook, XlsxRelationshipTypes.Connections, false) > 0;
    }

    internal static int CountQueryTables(XlsxWorkbook workbook)
    {
        int count = workbook.QueryTables == null ? 0 : workbook.QueryTables.Count;
        int unknownCount = CountUnknownPartsWithPrefix(workbook, XlsxPackagePaths.QueryTablesDirectory);
        int packageCount = CountPackagePartsWithPrefix(workbook, XlsxPackagePaths.QueryTablesDirectory);
        int relationshipCount = CountRelationships(workbook, XlsxRelationshipTypes.QueryTable, false);
        if (unknownCount > count) { count = unknownCount; }
        if (packageCount > count) { count = packageCount; }
        if (relationshipCount > count) { count = relationshipCount; }
        return count;
    }

    internal static int CountCustomXmlParts(XlsxWorkbook workbook)
    {
        int count = workbook.CustomXmlParts == null ? 0 : workbook.CustomXmlParts.Count;
        int unknownCount = CountUnknownPartsWithPrefix(workbook, XlsxPackagePaths.CustomXmlDirectory);
        int packageCount = CountPackagePartsWithPrefix(workbook, XlsxPackagePaths.CustomXmlDirectory);
        int relationshipCount = CountRelationships(workbook, XlsxRelationshipTypes.CustomXml, false);
        if (unknownCount > count) { count = unknownCount; }
        if (packageCount > count) { count = packageCount; }
        if (relationshipCount > count) { count = relationshipCount; }
        return count;
    }

    internal static int CountUnknownPartsWithPrefix(XlsxWorkbook workbook, string prefix)
    {
        int count = 0;
        if (workbook.UnknownParts == null || workbook.UnknownParts.Parts == null) { return count; }
        foreach (XlsxUnknownPart part in workbook.UnknownParts.Parts) { if (part != null && part.Path.StartsWith(prefix, StringComparison.Ordinal)) { count = count + 1; } }
        return count;
    }

    internal static int CountUnknownPartsWithPath(XlsxWorkbook workbook, string path)
    {
        int count = 0;
        if (workbook.UnknownParts == null || workbook.UnknownParts.Parts == null) { return count; }
        foreach (XlsxUnknownPart part in workbook.UnknownParts.Parts) { if (part != null && part.Path == path) { count = count + 1; } }
        return count;
    }

    internal static int CountPackagePartsWithPrefix(XlsxWorkbook workbook, string prefix)
    {
        int count = 0;
        if (workbook.PackageParts == null) { return count; }
        foreach (XlsxPackagePart part in workbook.PackageParts) { if (part != null && part.Path.StartsWith(prefix, StringComparison.Ordinal)) { count = count + 1; } }
        return count;
    }

    internal static int CountPackagePartsWithPath(XlsxWorkbook workbook, string path)
    {
        int count = 0;
        if (workbook.PackageParts == null) { return count; }
        foreach (XlsxPackagePart part in workbook.PackageParts) { if (part != null && part.Path == path) { count = count + 1; } }
        return count;
    }

    internal static int CountPackageXmlElements(XlsxWorkbook workbook, string contentType, string localName)
    {
        int count = 0;
        if (workbook.PackageParts == null) { return count; }
        foreach (XlsxPackagePart part in workbook.PackageParts)
        {
            if (part == null || part.ContentType != contentType || part.Data == null) { continue; }
            count = count + CountXmlStartElements(part.Data, localName);
        }
        return count;
    }

    internal static int CountXmlStartElements(byte[] data, string localName)
    {
        string xml = System.Text.Encoding.UTF8.GetString(data);
        string marker = "<" + localName;
        int count = 0;
        int index = 0;
        while (index < xml.Length)
        {
            int found = xml.IndexOf(marker, index, StringComparison.Ordinal);
            if (found < 0) { return count; }
            int following = found + marker.Length;
            if (following < xml.Length && (xml[following] == '>' || xml[following] == '/' || xml[following] == ' ' || xml[following] == '\t' || xml[following] == '\r' || xml[following] == '\n')) { count = count + 1; }
            index = following;
        }
        return count;
    }

    internal static int CountUnknownActiveParts(XlsxWorkbook workbook)
    {
        int count = 0;
        if (workbook.UnknownParts != null && workbook.UnknownParts.Parts != null)
        {
            foreach (XlsxUnknownPart part in workbook.UnknownParts.Parts) { if (IsUnknownActivePart(part)) { count = count + 1; } }
        }
        int packageCount = 0;
        if (workbook.PackageParts != null)
        {
            foreach (XlsxPackagePart part in workbook.PackageParts) { if (part != null && IsUnknownActivePath(part.Path, part.ContentType)) { packageCount = packageCount + 1; } }
        }
        return packageCount > count ? packageCount : count;
    }

    internal static bool IsUnknownActivePart(XlsxUnknownPart part)
    {
        if (part == null) { return false; }
        return IsUnknownActivePath(part.Path, part.ContentType);
    }

    internal static bool IsUnknownActivePath(string? partPath, string? partContentType)
    {
        string path = XlsxCellValueConverter.NormalizeText(partPath).ToLowerInvariant();
        string contentType = XlsxCellValueConverter.NormalizeText(partContentType).ToLowerInvariant();
        if (path == XlsxPackagePaths.VbaProject.ToLowerInvariant() || path.StartsWith(XlsxPackagePaths.EmbeddingsDirectory.ToLowerInvariant(), StringComparison.Ordinal)) { return false; }
        if (contentType.Contains("executable", StringComparison.Ordinal) || contentType.Contains("script", StringComparison.Ordinal) || contentType.Contains("x-msdownload", StringComparison.Ordinal) || contentType.Contains("x-shockwave-flash", StringComparison.Ordinal) || contentType.Contains("java-archive", StringComparison.Ordinal)) { return true; }
        return path.EndsWith(".exe", StringComparison.Ordinal) || path.EndsWith(".dll", StringComparison.Ordinal) || path.EndsWith(".com", StringComparison.Ordinal) || path.EndsWith(".bat", StringComparison.Ordinal) || path.EndsWith(".cmd", StringComparison.Ordinal) || path.EndsWith(".ps1", StringComparison.Ordinal) || path.EndsWith(".js", StringComparison.Ordinal) || path.EndsWith(".vbs", StringComparison.Ordinal) || path.EndsWith(".jar", StringComparison.Ordinal) || path.EndsWith(".swf", StringComparison.Ordinal);
    }

    internal static int RemoveOleObjects(XlsxWorkbook workbook)
    {
        int count = CountOleObjects(workbook);
        XlsxWorkbookSanitizeResult sanitize = new XlsxWorkbookSanitizeResult();
        workbook.RemoveOleObjects(sanitize);
        RemoveRelationships(workbook, XlsxRelationshipTypes.OleObject, false);
        RemoveRelationships(workbook, XlsxRelationshipTypes.EmbeddedPackage, false);
        RemovePackagePartsWithPrefix(workbook, XlsxPackagePaths.EmbeddingsDirectory);
        return count;
    }

    internal static int RemoveExternalLinks(XlsxWorkbook workbook)
    {
        int count = CountExternalLinks(workbook);
        workbook.RemoveExternalLinks();
        RemoveRelationships(workbook, XlsxRelationshipTypes.ExternalLinkPath, false);
        workbook.RemoveUnknownPartsByPrefix(XlsxPackagePaths.ExternalLinksDirectory);
        RemovePackagePartsWithPrefix(workbook, XlsxPackagePaths.ExternalLinksDirectory);
        return count;
    }

    internal static int RemoveConnections(XlsxWorkbook workbook)
    {
        workbook.Connections = new XlsxWorkbookConnectionsPart();
        workbook.HasConnections = false;
        workbook.RemoveUnknownPart(XlsxPackagePaths.Connections);
        RemovePackagePartsWithPath(workbook, XlsxPackagePaths.Connections);
        RemoveRelationships(workbook, XlsxRelationshipTypes.Connections, false);
        return 1;
    }

    internal static int RemoveQueryTables(XlsxWorkbook workbook)
    {
        int count = CountQueryTables(workbook);
        if (workbook.QueryTables != null) { workbook.QueryTables.Clear(); }
        workbook.RemoveUnknownPartsByPrefix(XlsxPackagePaths.QueryTablesDirectory);
        RemovePackagePartsWithPrefix(workbook, XlsxPackagePaths.QueryTablesDirectory);
        RemoveRelationships(workbook, XlsxRelationshipTypes.QueryTable, false);
        return count;
    }

    internal static int RemoveCustomXmlParts(XlsxWorkbook workbook)
    {
        int count = CountCustomXmlParts(workbook);
        workbook.RemoveCustomXmlParts();
        RemovePackagePartsWithPrefix(workbook, XlsxPackagePaths.CustomXmlDirectory);
        RemoveRelationships(workbook, XlsxRelationshipTypes.CustomXmlProperties, false);
        return count;
    }

    internal static int RemoveUnknownActiveParts(XlsxWorkbook workbook)
    {
        int removed = CountUnknownActiveParts(workbook);
        if (workbook.UnknownParts != null && workbook.UnknownParts.Parts != null)
        {
            int index = 0;
            while (index < workbook.UnknownParts.Parts.Count)
            {
                if (IsUnknownActivePart(workbook.UnknownParts.Parts[index]))
                {
                    workbook.UnknownParts.Parts.RemoveAt(index);
                    continue;
                }
                index = index + 1;
            }
        }
        if (workbook.PackageParts != null)
        {
            int index = 0;
            while (index < workbook.PackageParts.Count)
            {
                XlsxPackagePart part = workbook.PackageParts[index];
                if (part != null && IsUnknownActivePath(part.Path, part.ContentType))
                {
                    workbook.PackageParts.RemoveAt(index);
                    continue;
                }
                index = index + 1;
            }
        }
        return removed;
    }

    internal static void RemovePackagePartsWithPrefix(XlsxWorkbook workbook, string prefix)
    {
        if (workbook.PackageParts == null) { return; }
        int index = 0;
        while (index < workbook.PackageParts.Count)
        {
            XlsxPackagePart part = workbook.PackageParts[index];
            if (part != null && part.Path.StartsWith(prefix, StringComparison.Ordinal))
            {
                workbook.PackageParts.RemoveAt(index);
                continue;
            }
            index = index + 1;
        }
    }

    internal static void RemovePackagePartsWithPath(XlsxWorkbook workbook, string path)
    {
        if (workbook.PackageParts == null) { return; }
        int index = 0;
        while (index < workbook.PackageParts.Count)
        {
            XlsxPackagePart part = workbook.PackageParts[index];
            if (part != null && part.Path == path)
            {
                workbook.PackageParts.RemoveAt(index);
                continue;
            }
            index = index + 1;
        }
    }

    internal static int RemoveRelationships(XlsxWorkbook workbook, string relationshipType, bool externalOnly)
    {
        int removed = RemoveRelationshipsInPart(workbook.PackageRelationships, relationshipType, externalOnly) + RemoveRelationshipsInPart(workbook.WorkbookRelationships, relationshipType, externalOnly);
        if (workbook.RelationshipParts == null) { return removed; }
        foreach (XlsxRelationshipsPart part in workbook.RelationshipParts)
        {
            if (part == workbook.PackageRelationships || part == workbook.WorkbookRelationships) { continue; }
            removed = removed + RemoveRelationshipsInPart(part, relationshipType, externalOnly);
        }
        return removed;
    }

    internal static int RemoveRelationshipsInPart(XlsxRelationshipsPart part, string relationshipType, bool externalOnly)
    {
        int removed = 0;
        if (part == null || part.Relationships == null) { return removed; }
        int index = 0;
        while (index < part.Relationships.Count)
        {
            XlsxRelationship relationship = part.Relationships[index];
            if (relationship != null && relationship.Type == relationshipType && (!externalOnly || IsExternalRelationship(relationship)))
            {
                part.Relationships.RemoveAt(index);
                removed = removed + 1;
                continue;
            }
            index = index + 1;
        }
        return removed;
    }

    internal static void SyncRelationshipParts(XlsxWorkbook workbook, XlsxSecurityPolicyResult result)
    {
        if (workbook.RelationshipParts == null || workbook.UnknownParts == null) { return; }
        foreach (XlsxRelationshipsPart part in workbook.RelationshipParts)
        {
            if (part == null || part.PartPath.Length == 0 || part == workbook.PackageRelationships || part == workbook.WorkbookRelationships) { continue; }
            XlsxRelationshipsWriteResult write = new XlsxPartRelationshipsWriter().WriteBytes(part);
            foreach (XlsxDiagnostic diagnostic in write.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
            if (write.IsValid)
            {
                workbook.UnknownParts.RemovePart(part.PartPath);
                workbook.UnknownParts.AddPart(part.PartPath, XlsxContentTypes.Relationships, write.Bytes);
            }
        }
    }
}
