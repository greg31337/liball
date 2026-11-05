using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents unknown part store data used by the public workbook API.
/// </summary>
public class XlsxUnknownPartStore
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
    /// Contains parts.
    /// </summary>
    public List<XlsxUnknownPart> Parts;
    /// <summary>
    /// Initializes a new unknown part store with its default state.
    /// </summary>
    public XlsxUnknownPartStore()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Parts = new List<XlsxUnknownPart>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    internal void LoadFromPackage(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes)
    {
        Parts.Clear();
        IsValid = true;
        Validation = new XlsxValidationResult();
        if (package == null)
        {
            AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "unknownParts.packageNull", "Package read result must not be null."));
            return;
        }

        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (!IsSupportedPartPath(entry.Path))
            {
                string? contentType = "";
                if (contentTypes != null)
                {
                    contentType = contentTypes.GetContentType(entry.Path);
                }

                AddPart(entry.Path, contentType, entry.Data);
            }
        }
    }

    /// <summary>
    /// Adds part to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "path">The file-system path to read or write.</param>
    /// <param name = "contentType">The MIME content type recorded for the package part.</param>
    /// <param name = "data">The data value used by the operation.</param>
    internal void AddPart(string path, string? contentType, byte[] data)
    {
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(path);
        foreach (XlsxDiagnostic diagnostic in normalized.Validation.Diagnostics)
        {
            diagnostic.SetLocation(path, "", "");
            AddDiagnostic(diagnostic);
        }

        if (!normalized.Validation.IsValid)
        {
            return;
        }

        if (GetPart(normalized.Path) != null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "unknownParts.duplicatePart", "Unknown part store already contains the part.");
            diagnostic.SetLocation(normalized.Path, "", "");
            AddDiagnostic(diagnostic);
            return;
        }

        XlsxUnknownPart part = new XlsxUnknownPart();
        part.Path = normalized.Path;
        part.ContentType = contentType;
        if (data == null)
        {
            part.Data = new byte[0];
        }
        else
        {
            part.Data = data;
        }

        Parts.Add(part);
    }

    /// <summary>
    /// Returns part from the current model.
    /// </summary>
    /// <param name = "path">The file-system path to read or write.</param>
    /// <returns>The unknown part produced by the operation.</returns>
    internal XlsxUnknownPart? GetPart(string path)
    {
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(path);
        if (!normalized.Validation.IsValid)
        {
            return null;
        }

        foreach (XlsxUnknownPart part in Parts)
        {
            if (part.Path == normalized.Path)
            {
                return part;
            }
        }

        return null;
    }

    /// <summary>
    /// Performs the contains part operation using the current model state.
    /// </summary>
    /// <param name = "path">The file-system path to read or write.</param>
    /// <returns><c>true</c> when the requested condition is met; otherwise, <c>false</c>.</returns>
    internal bool ContainsPart(string path)
    {
        return GetPart(path) != null;
    }

    /// <summary>
    /// Removes part and any directly associated model references.
    /// </summary>
    /// <param name = "path">The file-system path to read or write.</param>
    /// <returns><c>true</c> when the requested condition is met; otherwise, <c>false</c>.</returns>
    internal bool RemovePart(string path)
    {
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(path);
        if (!normalized.Validation.IsValid)
        {
            return false;
        }

        int index = 0;
        while (index < Parts.Count)
        {
            if (Parts[index].Path == normalized.Path)
            {
                Parts.RemoveAt(index);
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddToEntries(List<XlsxPackageEntry>? entries)
    {
        if (entries == null)
        {
            AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "unknownParts.entriesNull", "Target entry list must not be null."));
            return;
        }

        foreach (XlsxUnknownPart part in Parts)
        {
            if (!EntryListContains(entries, part.Path))
            {
                XlsxPackageEntry entry = new XlsxPackageEntry();
                entry.Path = part.Path;
                entry.Data = part.Data;
                entry.UncompressedBytes = part.Data.Length;
                entries.Add(entry);
            }
        }
    }

    /// <summary>
    /// Adds content types to part to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "contentTypes">The content types value used by the operation.</param>
    internal void AddContentTypesToPart(XlsxContentTypesPart? contentTypes)
    {
        if (contentTypes == null)
        {
            AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "unknownParts.contentTypesNull", "Content types part must not be null."));
            return;
        }

        foreach (XlsxUnknownPart part in Parts)
        {
            string? existingContentType = contentTypes.GetContentType(part.Path);
            if (part.ContentType != null && part.ContentType.Length > 0 && existingContentType != part.ContentType)
            {
                XlsxContentTypeOverride overrideItem = new XlsxContentTypeOverride();
                overrideItem.PartName = part.Path;
                overrideItem.ContentType = part.ContentType;
                contentTypes.Overrides.Add(overrideItem);
            }
        }
    }

    internal static bool EntryListContains(List<XlsxPackageEntry> entries, string path)
    {
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(path);
        if (!normalized.Validation.IsValid)
        {
            return false;
        }

        foreach (XlsxPackageEntry entry in entries)
        {
            XlsxPackagePathResult entryPath = XlsxPackagePath.NormalizePartPath(entry.Path);
            if (entryPath.Validation.IsValid && entryPath.Path == normalized.Path)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsSupportedPartPath(string path)
    {
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(path);
        if (!normalized.Validation.IsValid)
        {
            return false;
        }

        if (normalized.Path == XlsxPackagePaths.ContentTypes)
        {
            return true;
        }

        if (normalized.Path == XlsxPackagePaths.PackageRelationships)
        {
            return true;
        }

        if (normalized.Path == XlsxPackagePaths.Workbook)
        {
            return true;
        }

        if (normalized.Path == XlsxPackagePaths.WorkbookRelationships)
        {
            return true;
        }

        if (normalized.Path == XlsxPackagePaths.CoreProperties)
        {
            return true;
        }

        if (normalized.Path == XlsxPackagePaths.ExtendedProperties)
        {
            return true;
        }

        if (normalized.Path == XlsxPackagePaths.CustomProperties)
        {
            return true;
        }

        return false;
    }
}
