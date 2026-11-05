using System;
using System.IO;

namespace odf;

/// <summary>
/// Writes package and flat document aggregates.
/// </summary>
public class OdfDocumentWriter : IOdfDocumentWriter
{
    /// <summary>
    /// Initializes a document writer.
    /// </summary>
    public OdfDocumentWriter()
    {
    }

    /// <summary>
    /// Writes a document to a file-system path.
    /// </summary>
    /// <param name="document">The document to write.</param>
    /// <param name="path">The destination path.</param>
    /// <param name="options">The save options.</param>
    public void WriteDocumentPath(
        OdfDocument document,
        string path,
        OdfSaveOptions options)
    {
        byte[] data = WriteDocumentBytes(document, options);
        File.WriteAllBytes(path, data);
    }

    /// <summary>
    /// Writes a document to a caller-supplied stream.
    /// </summary>
    /// <param name="document">The document to write.</param>
    /// <param name="output">The writable destination stream.</param>
    /// <param name="options">The save and stream-ownership options.</param>
    public void WriteDocumentStream(
        OdfDocument document,
        Stream output,
        OdfSaveOptions options)
    {
        if (output == null || !output.CanWrite)
        {
            throw new OdfDocumentException(
                "The document output stream is not writable.");
        }
        if (options == null)
        {
            throw new OdfDocumentException("Document save options are required.");
        }
        if (options.LeaveOutputOpen)
        {
            WriteOpenStream(document, output, options);
            return;
        }
        using (output)
        {
            WriteOpenStream(document, output, options);
        }
    }

    /// <summary>
    /// Writes a document to a new byte array.
    /// </summary>
    /// <param name="document">The document to write.</param>
    /// <param name="options">The save options.</param>
    /// <returns>The complete package or flat XML bytes.</returns>
    public byte[] WriteDocumentBytes(
        OdfDocument document,
        OdfSaveOptions options)
    {
        if (document == null || options == null)
        {
            throw new OdfDocumentException(
                "A document and save options are required.");
        }
        CompatibilityProfilePolicy policy =
            OdfCompatibilityProfileRules.Resolve(
                options.CompatibilityProfile);
        if (!policy.IsValid)
        {
            throw new OdfDocumentException(
                "The requested output compatibility profile is not recognized.");
        }
        if (options.TargetConformanceClass
                != OdfConformanceClass.Unknown
            && policy.ConformanceClass != OdfConformanceClass.Unknown
            && options.TargetConformanceClass
                != policy.ConformanceClass)
        {
            throw new OdfDocumentException(
                "The target conformance class conflicts with the output compatibility profile.");
        }
        CheckCancellation(options);
        MetadataDocumentModel.SynchronizeMetadata(document);
        MasterTextDocumentModel.SynchronizeMasterText(document);
        TextFlowDocumentModel.SynchronizeTextFlows(document);
        FontFaceDocumentModel.SynchronizeFontFaces(document);
        ChartSeriesModel.SynchronizeAllChartStyles(document);
        StyleDocumentModel.SynchronizeStyles(document);
        DataStyleDocumentModel.SynchronizeDataStyles(document);
        ListStyleDocumentModel.SynchronizeListStyles(document);
        StyleResourceDocumentModel.SynchronizeStyleResources(document);
        PageLayoutDocumentModel.SynchronizePageLayouts(document);
        PresentationDocumentModel.SynchronizePresentation(document);
        TextDocumentModel.SynchronizeTextBlocks(document);
        TextFlowDocumentModel.SynchronizeTextFlows(document);
        ImageResourceManager.SynchronizeImages(document);
        MediaResourceManager.SynchronizeMediaResources(document);
        EmbeddedContentManager.SynchronizeEmbeddedContent(document);
        DrawingDocumentModel.SynchronizeDrawings(document, options);
        FormDocumentModel.SynchronizeForms(document);
        XFormsDocumentModel.SynchronizeXForms(document);
        DatabaseDocumentModel.SynchronizeDatabase(document);
        SettingDocumentModel.SynchronizeSettings(document);
        ScriptDocumentModel.SynchronizeScripts(document);
        TextListDocumentModel.SynchronizeTextLists(document);
        TextFieldDocumentModel.SynchronizeTextFields(document);
        NoteDocumentModel.SynchronizeNotes(document);
        TextIndexDocumentModel.SynchronizeTextIndexes(document);
        AnnotationDocumentModel.SynchronizeAnnotations(document);
        TextChangeDocumentModel.SynchronizeChanges(document);
        TextRangeModel.LoadTextRanges(document);
        SpreadsheetSettingsModel.SynchronizeSpreadsheetSettings(
            document,
            options);
        SpreadsheetDataModel.SynchronizeSpreadsheetData(document);
        PivotTableModel.SynchronizePivotTables(document);
        SpreadsheetChangeModel.SynchronizeSpreadsheetChanges(document);
        SpreadsheetFormattingModel.SynchronizeSpreadsheetFormatting(document);
        TableDataViewModel.SynchronizeDataViews(document, options);
        TableCellStructureModel.SynchronizeCellStructures(document);
        TableCellValueModel.SynchronizeCellValues(document);
        RdfaDocumentModel.SynchronizeRdfa(document);
        RdfXmlDocumentModel.SynchronizeRdfParts(document);
        byte[] result;
        if (document.PackageKind == OdfPackageKind.Flat)
        {
            result = WriteFlatDocument(document, options);
        }
        else if (document.PackageKind == OdfPackageKind.Package)
        {
            result = WritePackageDocument(document, options);
        }
        else
        {
            throw new OdfDocumentException(
                "The document representation is not recognized.");
        }
        OdfDocumentModel.RememberSaveOptions(document, options);
        CheckCancellation(options);
        return result;
    }

    internal byte[] WriteFlatDocument(
        OdfDocument document,
        OdfSaveOptions options)
    {
        foreach (OdfPart part in document.Parts)
        {
            if (part.IsFlatDocument)
            {
                OdfXmlWriter writer = new OdfXmlWriter();
                return writer.WriteXmlBytes(part, options);
            }
        }
        if (CanCopyOriginal(document, options))
        {
            return CopyBytes(document.OriginalData);
        }
        throw new OdfDocumentException(
            "The flat document does not contain a materialized XML part.");
    }

    internal byte[] WritePackageDocument(
        OdfDocument document,
        OdfSaveOptions options)
    {
        if (document.Package == null)
        {
            if (CanCopyOriginal(document, options))
            {
                return CopyBytes(document.OriginalData);
            }
            throw new OdfDocumentException(
                "The document does not retain an editable package.");
        }
        OdfPackageWriter packageWriter = new OdfPackageWriter();
        OdfXmlWriter xmlWriter = new OdfXmlWriter();
        foreach (OdfPart part in document.Parts)
        {
            if (part.Path.Length > 0)
            {
                CheckCancellation(options);
                byte[] data = xmlWriter.WriteXmlBytes(part, options);
                if (HasPackageEntry(document.Package, part.Path))
                {
                    OdfEditResult edit = packageWriter.ReplacePackageEntry(
                        document.Package,
                        part.Path,
                        data);
                    RequireSuccessfulEdit(edit, part.Path);
                }
                else
                {
                    OdfEditResult edit = packageWriter.AddPackageEntry(
                        document.Package,
                        part.Path,
                        data,
                        OdfCompressionMethod.Deflated);
                    RequireSuccessfulEdit(edit, part.Path);
                }
            }
        }
        RdfXmlDocumentModel.WriteRdfParts(document, options);
        byte[] result = packageWriter.WritePackageBytes(
            document.Package,
            options);
        document.PackageEntries = document.Package.Entries;
        document.Manifest = document.Package.Manifest;
        return result;
    }

    internal void WriteOpenStream(
        OdfDocument document,
        Stream output,
        OdfSaveOptions options)
    {
        byte[] data = WriteDocumentBytes(document, options);
        output.Write(data, 0, data.Length);
        output.Flush();
    }

    internal bool HasPackageEntry(OdfPackage package, string path)
    {
        foreach (OdfPackageEntry entry in package.Entries)
        {
            if (entry.Path == path)
            {
                return true;
            }
        }
        return false;
    }

    internal void RequireSuccessfulEdit(OdfEditResult edit, string path)
    {
        if (edit.Status != OdfEditStatus.Success
            && edit.Status != OdfEditStatus.NoChange)
        {
            throw new OdfDocumentException(
                "The XML part '"
                + path
                + "' could not be updated in the package.");
        }
    }

    internal bool CanCopyOriginal(
        OdfDocument document,
        OdfSaveOptions options)
    {
        return document.OriginalData.Length > 0
            && options.TargetVersion == OdfVersion.Unknown
            && options.TargetConformanceClass == OdfConformanceClass.Unknown
            && (options.CompatibilityProfile.Length == 0
                || options.CompatibilityProfile
                    == OdfCompatibilityProfiles.PreserveExisting);
    }

    internal void CheckCancellation(OdfSaveOptions options)
    {
        if (options.Cancellation.IsCancellationRequested)
        {
            throw new OdfDocumentException("Document writing was cancelled.");
        }
    }

    internal byte[] CopyBytes(byte[] data)
    {
        byte[] result = new byte[data.Length];
        Array.Copy(data, result, data.Length);
        return result;
    }
}
