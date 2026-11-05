using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents a workbook and coordinates its OOXML parts, worksheet models, validation, and package load/save operations.
/// </summary>
/// <remarks>
/// Workbook operations return result objects instead of throwing for expected validation failures. Check <c>IsValid</c> and inspect validation diagnostics before using returned bytes or models.
/// </remarks>
public class XlsxWorkbook
{
    /// <summary>
    /// Stores the SpreadsheetML conformance family declared by the package.
    /// </summary>
    public XlsxPackageConformance Conformance;
    /// <summary>
    /// Contains the content type declarations read from the package.
    /// </summary>
    public XlsxContentTypesPart ContentTypes;
    /// <summary>
    /// Contains every physical package part in normalized path order.
    /// </summary>
    public List<XlsxPackagePart> PackageParts;
    /// <summary>
    /// Contains every relationships part discovered in the package.
    /// </summary>
    public List<XlsxRelationshipsPart> RelationshipParts;
    /// <summary>
    /// Contains package thumbnail and preview metadata.
    /// </summary>
    public XlsxPackagePreviewMetadata PreviewMetadata;
    /// <summary>
    /// Stores the package variant value used by this model.
    /// </summary>
    public XlsxPackageVariant PackageVariant;
    /// <summary>
    /// Stores the workbook part value used by this model.
    /// </summary>
    public XlsxWorkbookPart WorkbookPart;
    /// <summary>
    /// Contains package relationships.
    /// </summary>
    public XlsxRelationshipsPart PackageRelationships;
    /// <summary>
    /// Contains workbook relationships.
    /// </summary>
    public XlsxRelationshipsPart WorkbookRelationships;
    /// <summary>
    /// Contains core properties.
    /// </summary>
    public XlsxCorePropertiesPart CoreProperties;
    /// <summary>
    /// Indicates whether core properties is present and should be written.
    /// </summary>
    public bool HasCoreProperties;
    /// <summary>
    /// Contains extended properties.
    /// </summary>
    public XlsxExtendedPropertiesPart ExtendedProperties;
    /// <summary>
    /// Indicates whether extended properties is present and should be written.
    /// </summary>
    public bool HasExtendedProperties;
    /// <summary>
    /// Contains custom properties.
    /// </summary>
    public XlsxCustomPropertiesPart CustomProperties;
    /// <summary>
    /// Indicates whether custom properties is present and should be written.
    /// </summary>
    public bool HasCustomProperties;
    /// <summary>
    /// Contains styles.
    /// </summary>
    public XlsxStylesPart Styles;
    /// <summary>
    /// Indicates whether styles is present and should be written.
    /// </summary>
    public bool HasStyles;
    /// <summary>
    /// Stores the theme value used by this model.
    /// </summary>
    public XlsxThemePart Theme;
    /// <summary>
    /// Indicates whether theme is present and should be written.
    /// </summary>
    public bool HasTheme;
    /// <summary>
    /// Contains the workbook shared string table.
    /// </summary>
    public XlsxSharedStringTablePart SharedStrings;
    /// <summary>
    /// Indicates whether a shared string table is present and should be written.
    /// </summary>
    public bool HasSharedStrings;
    /// <summary>
    /// Contains the workbook calculation chain.
    /// </summary>
    public XlsxCalculationChainPart CalculationChain;
    /// <summary>
    /// Indicates whether a calculation chain is present and should be written.
    /// </summary>
    public bool HasCalculationChain;
    /// <summary>
    /// Contains worksheets.
    /// </summary>
    public List<XlsxWorksheetPart> Worksheets;
    /// <summary>
    /// Contains chartsheet parts.
    /// </summary>
    public List<XlsxChartsheetPart> Chartsheets;
    /// <summary>
    /// Contains workbook metadata for macro sheets.
    /// </summary>
    public List<XlsxWorkbookSheet> Macrosheets;
    /// <summary>
    /// Contains workbook metadata for dialog sheets.
    /// </summary>
    public List<XlsxWorkbookSheet> Dialogsheets;
    /// <summary>
    /// Contains spreadsheet drawing parts.
    /// </summary>
    public List<XlsxDrawingPart> Drawings;
    /// <summary>
    /// Contains package image parts.
    /// </summary>
    public List<XlsxImagePart> Images;
    /// <summary>
    /// Contains legacy comments parts.
    /// </summary>
    public List<XlsxCommentsPart> Comments;
    /// <summary>
    /// Contains threaded comments parts.
    /// </summary>
    public List<XlsxThreadedCommentsPart> ThreadedComments;
    /// <summary>
    /// Contains charts.
    /// </summary>
    public List<XlsxChartPart> Charts;
    /// <summary>
    /// Contains pivot cache definitions.
    /// </summary>
    public List<XlsxPivotCacheDefinitionPart> PivotCacheDefinitions;
    /// <summary>
    /// Contains pivot table definitions.
    /// </summary>
    public List<XlsxPivotTableDefinitionPart> PivotTableDefinitions;
    /// <summary>
    /// Contains slicer caches.
    /// </summary>
    public List<XlsxSlicerTimelinePart> SlicerCaches;
    /// <summary>
    /// Contains slicers.
    /// </summary>
    public List<XlsxSlicerTimelinePart> Slicers;
    /// <summary>
    /// Contains timeline caches.
    /// </summary>
    public List<XlsxSlicerTimelinePart> TimelineCaches;
    /// <summary>
    /// Contains timelines.
    /// </summary>
    public List<XlsxSlicerTimelinePart> Timelines;
    /// <summary>
    /// Contains external links.
    /// </summary>
    public List<XlsxExternalLinkPart> ExternalLinks;
    /// <summary>
    /// Contains connections.
    /// </summary>
    public XlsxWorkbookConnectionsPart Connections;
    /// <summary>
    /// Indicates whether connections is present and should be written.
    /// </summary>
    public bool HasConnections;
    /// <summary>
    /// Contains query tables.
    /// </summary>
    public List<XlsxQueryTablePart> QueryTables;
    /// <summary>
    /// Contains custom xml parts.
    /// </summary>
    public List<XlsxCustomXmlPart> CustomXmlParts;
    /// <summary>
    /// Stores the vba project value used by this model.
    /// </summary>
    public XlsxVbaProjectPart VbaProject;
    /// <summary>
    /// Indicates whether vba project is present and should be written.
    /// </summary>
    public bool HasVbaProject;
    /// <summary>
    /// Contains embedded objects.
    /// </summary>
    public List<XlsxEmbeddedObjectPart> EmbeddedObjects;
    /// <summary>
    /// Contains in cell picture metadata parts.
    /// </summary>
    public List<XlsxInCellPictureMetadataPart> InCellPictureMetadataParts;
    /// <summary>
    /// Contains digital signatures.
    /// </summary>
    public List<XlsxDigitalSignaturePart> DigitalSignatures;
    /// <summary>
    /// Indicates whether digital signatures is present and should be written.
    /// </summary>
    public bool HasDigitalSignatures;
    /// <summary>
    /// Contains unknown parts.
    /// </summary>
    public XlsxUnknownPartStore UnknownParts;
    /// <summary>
    /// Initializes a new workbook with its default state.
    /// </summary>
    public XlsxWorkbook()
    {
        Conformance = XlsxPackageConformance.Transitional;
        ContentTypes = new XlsxContentTypesPart();
        PackageParts = new List<XlsxPackagePart>();
        RelationshipParts = new List<XlsxRelationshipsPart>();
        PreviewMetadata = new XlsxPackagePreviewMetadata();
        PackageVariant = XlsxPackageVariant.Xlsx;
        WorkbookPart = new XlsxWorkbookPart();
        PackageRelationships = new XlsxRelationshipsPart();
        WorkbookRelationships = new XlsxRelationshipsPart();
        CoreProperties = new XlsxCorePropertiesPart();
        HasCoreProperties = false;
        ExtendedProperties = new XlsxExtendedPropertiesPart();
        HasExtendedProperties = false;
        CustomProperties = new XlsxCustomPropertiesPart();
        HasCustomProperties = false;
        Styles = new XlsxStylesPart();
        HasStyles = false;
        Theme = new XlsxThemePart();
        HasTheme = false;
        SharedStrings = new XlsxSharedStringTablePart();
        HasSharedStrings = false;
        CalculationChain = new XlsxCalculationChainPart();
        HasCalculationChain = false;
        Worksheets = new List<XlsxWorksheetPart>();
        Chartsheets = new List<XlsxChartsheetPart>();
        Macrosheets = new List<XlsxWorkbookSheet>();
        Dialogsheets = new List<XlsxWorkbookSheet>();
        Drawings = new List<XlsxDrawingPart>();
        Images = new List<XlsxImagePart>();
        Comments = new List<XlsxCommentsPart>();
        ThreadedComments = new List<XlsxThreadedCommentsPart>();
        Charts = new List<XlsxChartPart>();
        PivotCacheDefinitions = new List<XlsxPivotCacheDefinitionPart>();
        PivotTableDefinitions = new List<XlsxPivotTableDefinitionPart>();
        SlicerCaches = new List<XlsxSlicerTimelinePart>();
        Slicers = new List<XlsxSlicerTimelinePart>();
        TimelineCaches = new List<XlsxSlicerTimelinePart>();
        Timelines = new List<XlsxSlicerTimelinePart>();
        ExternalLinks = new List<XlsxExternalLinkPart>();
        Connections = new XlsxWorkbookConnectionsPart();
        HasConnections = false;
        QueryTables = new List<XlsxQueryTablePart>();
        CustomXmlParts = new List<XlsxCustomXmlPart>();
        VbaProject = new XlsxVbaProjectPart();
        HasVbaProject = false;
        EmbeddedObjects = new List<XlsxEmbeddedObjectPart>();
        InCellPictureMetadataParts = new List<XlsxInCellPictureMetadataPart>();
        DigitalSignatures = new List<XlsxDigitalSignaturePart>();
        HasDigitalSignatures = false;
        UnknownParts = new XlsxUnknownPartStore();
    }

    /// <summary>
    /// Creates a minimal workbook with one worksheet and the required package relationships.
    /// </summary>
    /// <returns>The workbook produced by the operation.</returns>
    internal static XlsxWorkbook Create()
    {
        XlsxWorkbook workbook = new XlsxWorkbook();
        workbook.WorkbookPart.Sheets.Add(CreateSheet("Sheet1", 1, "rId1"));
        workbook.PackageRelationships = CreatePackageRelationships();
        workbook.WorkbookRelationships = CreateWorkbookRelationships();
        return workbook;
    }

    internal XlsxPackagePart? GetPackagePart(string path)
    {
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(path);
        if (!normalized.Validation.IsValid)
        {
            return null;
        }
        foreach (XlsxPackagePart part in PackageParts)
        {
            if (part != null && part.Path == normalized.Path)
            {
                return part;
            }
        }
        return null;
    }

    internal XlsxValidationResult ReplacePackagePart(XlsxPackagePart part)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (part == null)
        {
            result.AddError("workbook.packagePartNull", "Replacement package part must not be null.");
            return result;
        }
        if (part.Data == null)
        {
            result.AddError("workbook.packagePartDataNull", "Replacement package part data must not be null.");
            return result;
        }
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(part.Path);
        CopyDiagnostics(result, normalized.Validation);
        if (!result.IsValid)
        {
            return result;
        }
        part.Path = normalized.Path;
        int index = 0;
        bool replaced = false;
        while (index < PackageParts.Count)
        {
            if (PackageParts[index].Path == part.Path)
            {
                PackageParts[index] = part;
                replaced = true;
                break;
            }
            index = index + 1;
        }
        if (!replaced)
        {
            PackageParts.Add(part);
        }
        if (UnknownParts == null)
        {
            UnknownParts = new XlsxUnknownPartStore();
        }
        UnknownParts.RemovePart(part.Path);
        UnknownParts.AddPart(part.Path, part.ContentType, part.Data);
        ApplyPackagePartReplacement(part, result);
        return result;
    }

    internal void ApplyPackagePartReplacement(XlsxPackagePart packagePart, XlsxValidationResult result)
    {
        string? contentType = packagePart.ContentType;
        if ((contentType == null || contentType.Length == 0) && ContentTypes != null)
        {
            contentType = ContentTypes.GetContentType(packagePart.Path);
        }
        if (contentType == XlsxContentTypes.SharedStrings)
        {
            SharedStrings = new XlsxSharedStringTableReader().ReadBytes(packagePart.Data);
            SharedStrings.PartPath = packagePart.Path;
            HasSharedStrings = true;
            CopyDiagnostics(result, SharedStrings.Validation);
        }
        else if (contentType == XlsxContentTypes.CalculationChain)
        {
            CalculationChain = new XlsxCalculationChainPartReader().ReadBytes(packagePart.Data);
            CalculationChain.PartPath = packagePart.Path;
            HasCalculationChain = true;
            CopyDiagnostics(result, CalculationChain.Validation);
        }
        else if (contentType == XlsxContentTypes.Chartsheet)
        {
            XlsxChartsheetPart chartsheet = new XlsxChartsheetPartReader().ReadString(Encoding.UTF8.GetString(packagePart.Data));
            chartsheet.PartPath = packagePart.Path;
            ReplaceChartsheetAggregate(chartsheet);
            CopyDiagnostics(result, chartsheet.Validation);
        }
        else if (contentType == XlsxContentTypes.Drawing)
        {
            XlsxDrawingPart drawing = new XlsxDrawingPartReader().ReadString(Encoding.UTF8.GetString(packagePart.Data));
            drawing.PartPath = packagePart.Path;
            ReplaceDrawingAggregate(drawing);
            CopyDiagnostics(result, drawing.Validation);
        }
        else if (contentType != null && contentType.StartsWith("image/", System.StringComparison.Ordinal))
        {
            XlsxImagePart image = new XlsxImagePartReader().ReadBytesWithContentType(packagePart.Path, contentType, packagePart.Data);
            ReplaceImageAggregate(image);
            CopyDiagnostics(result, image.Validation);
        }
        else if (contentType == XlsxContentTypes.Comments)
        {
            XlsxCommentsPart comments = new XlsxCommentsPartReader().ReadBytes(packagePart.Data);
            comments.PartPath = packagePart.Path;
            ReplaceCommentsAggregate(comments);
            CopyDiagnostics(result, comments.Validation);
        }
        else if (contentType == XlsxContentTypes.ThreadedComments)
        {
            XlsxThreadedCommentsPart comments = new XlsxThreadedCommentsPartReader().ReadBytes(packagePart.Data);
            comments.PartPath = packagePart.Path;
            ReplaceThreadedCommentsAggregate(comments);
            CopyDiagnostics(result, comments.Validation);
        }
        else if (contentType == XlsxContentTypes.Connections)
        {
            Connections = new XlsxWorkbookConnectionsPartReader().ReadBytes(packagePart.Path, packagePart.Data);
            HasConnections = true;
            CopyDiagnostics(result, Connections.Validation);
        }
        else if (contentType == XlsxContentTypes.CoreProperties)
        {
            CoreProperties = new XlsxCorePropertiesReader().ReadBytes(packagePart.Data);
            HasCoreProperties = true;
            CopyDiagnostics(result, CoreProperties.Validation);
        }
        else if (contentType == XlsxContentTypes.ExtendedProperties)
        {
            ExtendedProperties = new XlsxExtendedPropertiesReader().ReadBytes(packagePart.Data);
            HasExtendedProperties = true;
            CopyDiagnostics(result, ExtendedProperties.Validation);
        }
        else if (contentType == XlsxContentTypes.CustomProperties)
        {
            CustomProperties = new XlsxCustomPropertiesReader().ReadBytes(packagePart.Data);
            HasCustomProperties = true;
            CopyDiagnostics(result, CustomProperties.Validation);
        }
    }

    internal void ReplaceChartsheetAggregate(XlsxChartsheetPart replacement)
    {
        int index = 0;
        while (index < Chartsheets.Count)
        {
            if (Chartsheets[index].PartPath == replacement.PartPath)
            {
                Chartsheets[index] = replacement;
                return;
            }
            index = index + 1;
        }
        Chartsheets.Add(replacement);
    }

    internal void ReplaceDrawingAggregate(XlsxDrawingPart replacement)
    {
        int index = 0;
        while (index < Drawings.Count)
        {
            if (Drawings[index].PartPath == replacement.PartPath)
            {
                Drawings[index] = replacement;
                return;
            }
            index = index + 1;
        }
        Drawings.Add(replacement);
    }

    internal void ReplaceImageAggregate(XlsxImagePart replacement)
    {
        int index = 0;
        while (index < Images.Count)
        {
            if (Images[index].PartPath == replacement.PartPath)
            {
                Images[index] = replacement;
                return;
            }
            index = index + 1;
        }
        Images.Add(replacement);
    }

    internal void ReplaceCommentsAggregate(XlsxCommentsPart replacement)
    {
        int index = 0;
        while (index < Comments.Count)
        {
            if (Comments[index].PartPath == replacement.PartPath)
            {
                Comments[index] = replacement;
                return;
            }
            index = index + 1;
        }
        Comments.Add(replacement);
    }

    internal void ReplaceThreadedCommentsAggregate(XlsxThreadedCommentsPart replacement)
    {
        int index = 0;
        while (index < ThreadedComments.Count)
        {
            if (ThreadedComments[index].PartPath == replacement.PartPath)
            {
                ThreadedComments[index] = replacement;
                return;
            }
            index = index + 1;
        }
        ThreadedComments.Add(replacement);
    }

    /// <summary>
    /// Adds sheet to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookSheetOperationResult AddSheet(string name)
    {
        XlsxWorkbookSheetOperationResult result = new XlsxWorkbookSheetOperationResult();
        XlsxValidationResult nameValidation = ValidateNewSheetName(name);
        CopyDiagnostics(result, nameValidation);
        if (!result.IsValid)
        {
            return result;
        }

        uint sheetId = GetNextSheetId();
        string relationshipId = GetNextWorkbookRelationshipId();
        XlsxWorkbookSheet sheet = CreateSheet(name, sheetId, relationshipId);
        WorkbookPart.Sheets.Add(sheet);
        WorkbookRelationships.Relationships.Add(CreateWorksheetRelationship(relationshipId, "worksheets/sheet" + sheetId.ToString(CultureInfo.InvariantCulture) + ".xml"));
        result.Sheet = sheet;
        return result;
    }

    /// <summary>
    /// Adds raw image to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "data">The data value used by the operation.</param>
    /// <param name = "contentType">The MIME content type recorded for the package part.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxRawImageInsertResult AddRawImage(byte[] data, string contentType)
    {
        XlsxRawImageInsertResult result = new XlsxRawImageInsertResult();
        string format = XlsxImagePartReader.DetectImageFormat(data);
        string contentFormat = XlsxImagePartReader.GetImageFormatFromContentType(contentType);
        if (data == null)
        {
            result.AddError("image.dataNull", "Image data must not be null.");
            return result;
        }

        if (data.Length == 0)
        {
            result.AddError("image.dataEmpty", "Image data must not be empty.");
            return result;
        }

        if (contentType != null && contentType.Length > 0)
        {
            if (contentFormat.Length == 0)
            {
                result.AddError("image.contentTypeUnsupported", "Image content type is not supported.");
                return result;
            }

            if (format.Length > 0 && contentFormat != format)
            {
                result.AddError("image.contentTypeMismatch", "Image content type does not match the detected image data format.");
                return result;
            }

            format = contentFormat;
        }

        if (format.Length == 0)
        {
            result.AddError("image.formatUnsupported", "Image data format is not supported.");
            return result;
        }

        string extension = XlsxImagePartReader.GetExtensionForFormat(format);
        string partPath = GetNextImagePartPath(extension);
        string? resolvedContentType = contentType;
        if (resolvedContentType == null || resolvedContentType.Length == 0)
        {
            resolvedContentType = XlsxImagePartReader.GetContentTypeForFormat(format);
        }

        return AddRawImagePart(partPath, data, resolvedContentType);
    }

    /// <summary>
    /// Adds raw image part to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "data">The data value used by the operation.</param>
    /// <param name = "contentType">The MIME content type recorded for the package part.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxRawImageInsertResult AddRawImagePart(string partPath, byte[] data, string contentType)
    {
        XlsxRawImageInsertResult result = new XlsxRawImageInsertResult();
        if (UnknownParts == null)
        {
            UnknownParts = new XlsxUnknownPartStore();
        }

        XlsxImagePart part = new XlsxImagePartReader().ReadBytesWithContentType(partPath, contentType, data);
        CopyImageDiagnostics(result, part.Validation);
        if (!result.IsValid)
        {
            return result;
        }

        if (!IsMediaPartPath(part.PartPath))
        {
            XlsxDiagnostic diagnostic = result.AddError("image.partPathNotMedia", "Raw image parts must be stored under /xl/media/.");
            diagnostic.SetLocation(part.PartPath, "", "");
            return result;
        }

        if (UnknownParts.ContainsPart(part.PartPath))
        {
            XlsxDiagnostic diagnostic = result.AddError("image.partDuplicate", "Workbook already contains an image part at the requested path.");
            diagnostic.SetLocation(part.PartPath, "", "");
            return result;
        }

        XlsxImagePartWriteResult writeValidation = XlsxImagePartWriter.ValidateImagePart(part);
        CopyImageDiagnostics(result, writeValidation.Validation);
        if (!result.IsValid)
        {
            return result;
        }

        UnknownParts.AddPart(part.PartPath, part.ContentType, part.Data);
        CopyImageDiagnostics(result, UnknownParts.Validation);
        if (!result.IsValid)
        {
            return result;
        }

        result.ImagePart = part;
        result.HasImagePart = true;
        result.PartPath = part.PartPath;
        result.ContentType = part.ContentType;
        return result;
    }

    internal XlsxRawImageInsertResult ReplaceRawImagePart(string partPath, byte[] data, string contentType)
    {
        XlsxRawImageInsertResult result = new XlsxRawImageInsertResult();
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(partPath);
        CopyImageDiagnostics(result, normalized.Validation);
        if (!result.IsValid)
        {
            return result;
        }
        bool exists = UnknownParts != null && UnknownParts.ContainsPart(normalized.Path);
        if (!exists)
        {
            foreach (XlsxImagePart existingImage in Images)
            {
                if (existingImage != null && existingImage.PartPath == normalized.Path) { exists = true; }
            }
        }
        if (!exists)
        {
            XlsxDiagnostic diagnostic = result.AddError("image.partNotFound", "The image part to replace was not found.");
            diagnostic.SetLocation(normalized.Path, "", "");
            return result;
        }
        XlsxImagePart part = new XlsxImagePartReader().ReadBytesWithContentType(normalized.Path, contentType, data);
        CopyImageDiagnostics(result, part.Validation);
        if (!result.IsValid)
        {
            return result;
        }
        if (UnknownParts == null) { UnknownParts = new XlsxUnknownPartStore(); }
        UnknownParts.RemovePart(part.PartPath);
        UnknownParts.AddPart(part.PartPath, part.ContentType, part.Data);
        CopyImageDiagnostics(result, UnknownParts.Validation);
        if (!result.IsValid) { return result; }
        ReplaceImageAggregate(part);
        result.ImagePart = part;
        result.HasImagePart = true;
        result.PartPath = part.PartPath;
        result.ContentType = part.ContentType;
        return result;
    }

    internal string GetNextImagePartPath(string extension)
    {
        string normalizedExtension = extension;
        if (normalizedExtension == null || normalizedExtension.Length == 0)
        {
            normalizedExtension = "bin";
        }

        if (normalizedExtension.StartsWith("."))
        {
            normalizedExtension = normalizedExtension.Substring(1);
        }

        if (UnknownParts == null)
        {
            UnknownParts = new XlsxUnknownPartStore();
        }

        int index = 1;
        while (true)
        {
            string path = XlsxPackagePaths.MediaDirectory + "image" + index.ToString(CultureInfo.InvariantCulture) + "." + normalizedExtension;
            if (!UnknownParts.ContainsPart(path))
            {
                return path;
            }

            index = index + 1;
        }
    }

    internal static bool IsMediaPartPath(string partPath)
    {
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(partPath);
        if (!normalized.Validation.IsValid)
        {
            return false;
        }

        return normalized.Path.StartsWith(XlsxPackagePaths.MediaDirectory, System.StringComparison.Ordinal);
    }

    internal static void CopyImageDiagnostics(XlsxRawImageInsertResult target, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Adds worksheet image to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "worksheet">The worksheet model used by the operation.</param>
    /// <param name = "data">The data value used by the operation.</param>
    /// <param name = "contentType">The MIME content type recorded for the package part.</param>
    /// <param name = "fromCell">The from cell value used by the operation.</param>
    /// <param name = "toCell">The to cell value used by the operation.</param>
    /// <param name = "name">The name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxDrawingImagePlacementResult AddWorksheetImage(XlsxWorksheetPart worksheet, byte[] data, string contentType, string fromCell, string toCell, string name)
    {
        XlsxDrawingImagePlacementResult result = new XlsxDrawingImagePlacementResult();
        if (worksheet == null)
        {
            result.AddError("worksheetImage.worksheetNull", "Worksheet image placement requires a worksheet part.");
            return result;
        }

        if (UnknownParts == null)
        {
            UnknownParts = new XlsxUnknownPartStore();
        }

        XlsxRawImageInsertResult imageResult = AddRawImage(data, contentType);
        CopyPlacementDiagnostics(result, imageResult.Validation);
        if (!result.IsValid)
        {
            return result;
        }

        string drawingPath = GetWorksheetDrawingPartPath(worksheet);
        string worksheetDrawingRelationshipId = GetWorksheetDrawingRelationshipId(worksheet);
        string imageRelationshipId = "rIdImage1";
        XlsxDrawingPart drawingPart = new XlsxDrawingPart();
        drawingPart.PartPath = drawingPath;
        XlsxDrawingImagePlacementResult anchorResult = drawingPart.AddTwoCellImage(imageRelationshipId, fromCell, toCell, name, "");
        CopyPlacementDiagnostics(result, anchorResult.Validation);
        if (!result.IsValid)
        {
            return result;
        }

        worksheet.HasDrawing = true;
        if (worksheet.Drawing == null)
        {
            worksheet.Drawing = new XlsxSheetDrawingReference();
        }

        worksheet.Drawing.RawXml = "";
        worksheet.Drawing.RelationshipId = worksheetDrawingRelationshipId;
        worksheet.Drawing.Target = GetWorksheetDrawingRelationshipTarget(drawingPath);
        worksheet.Drawing.TargetPath = drawingPath;
        XlsxRelationshipsPart worksheetRelationships = new XlsxRelationshipsPart();
        worksheetRelationships.SourcePartPath = worksheet.PartPath;
        worksheetRelationships.PartPath = XlsxPackagePath.GetRelationshipPartPath(worksheet.PartPath);
        worksheetRelationships.Relationships.Add(CreateRelationship(worksheetDrawingRelationshipId, XlsxRelationshipTypes.Drawing, GetWorksheetDrawingRelationshipTarget(drawingPath)));
        XlsxRelationshipsPart drawingRelationships = new XlsxRelationshipsPart();
        drawingRelationships.SourcePartPath = drawingPath;
        drawingRelationships.PartPath = XlsxPackagePath.GetRelationshipPartPath(drawingPath);
        drawingRelationships.Relationships.Add(CreateRelationship(imageRelationshipId, XlsxRelationshipTypes.Image, GetDrawingImageRelationshipTarget(imageResult.PartPath)));
        XlsxDrawingPartWriteResult drawingWrite = new XlsxDrawingPartWriter().WriteBytes(drawingPart);
        CopyPlacementDiagnostics(result, drawingWrite.Validation);
        XlsxRelationshipsWriteResult worksheetRelationshipsWrite = new XlsxPartRelationshipsWriter().WriteBytes(worksheetRelationships);
        CopyPlacementDiagnostics(result, worksheetRelationshipsWrite.Validation);
        XlsxRelationshipsWriteResult drawingRelationshipsWrite = new XlsxPartRelationshipsWriter().WriteBytes(drawingRelationships);
        CopyPlacementDiagnostics(result, drawingRelationshipsWrite.Validation);
        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteBytes(worksheet);
        CopyPlacementDiagnostics(result, worksheetWrite.Validation);
        if (!result.IsValid)
        {
            return result;
        }

        UnknownParts.RemovePart(worksheet.PartPath);
        UnknownParts.RemovePart(worksheetRelationships.PartPath);
        UnknownParts.RemovePart(drawingPath);
        UnknownParts.RemovePart(drawingRelationships.PartPath);
        UnknownParts.AddPart(worksheet.PartPath, XlsxContentTypes.Worksheet, worksheetWrite.Bytes);
        UnknownParts.AddPart(worksheetRelationships.PartPath, XlsxContentTypes.Relationships, worksheetRelationshipsWrite.Bytes);
        UnknownParts.AddPart(drawingPath, XlsxContentTypes.Drawing, drawingWrite.Bytes);
        UnknownParts.AddPart(drawingRelationships.PartPath, XlsxContentTypes.Relationships, drawingRelationshipsWrite.Bytes);
        CopyPlacementDiagnostics(result, UnknownParts.Validation);
        result.DrawingPart = drawingPart;
        result.HasDrawingPart = true;
        result.Anchor = anchorResult.Anchor;
        result.HasAnchor = anchorResult.HasAnchor;
        result.ImagePart = imageResult.ImagePart;
        result.HasImagePart = imageResult.HasImagePart;
        result.WorksheetRelationships = worksheetRelationships;
        result.HasWorksheetRelationships = true;
        result.DrawingRelationships = drawingRelationships;
        result.HasDrawingRelationships = true;
        return result;
    }

    internal string GetWorksheetDrawingPartPath(XlsxWorksheetPart worksheet)
    {
        if (worksheet != null && worksheet.HasDrawing && worksheet.Drawing != null && worksheet.Drawing.TargetPath != null && worksheet.Drawing.TargetPath.Length > 0)
        {
            return worksheet.Drawing.TargetPath;
        }

        return GetNextDrawingPartPath();
    }

    internal string GetNextDrawingPartPath()
    {
        if (UnknownParts == null)
        {
            UnknownParts = new XlsxUnknownPartStore();
        }

        int index = 1;
        while (true)
        {
            string path = XlsxPackagePaths.DrawingsDirectory + "drawing" + index.ToString(CultureInfo.InvariantCulture) + ".xml";
            if (!UnknownParts.ContainsPart(path))
            {
                return path;
            }

            index = index + 1;
        }
    }

    internal static string GetWorksheetDrawingRelationshipId(XlsxWorksheetPart worksheet)
    {
        if (worksheet != null && worksheet.HasDrawing && worksheet.Drawing != null && worksheet.Drawing.RelationshipId != null && worksheet.Drawing.RelationshipId.Length > 0)
        {
            return worksheet.Drawing.RelationshipId;
        }

        return "rIdDrawing1";
    }

    internal static XlsxRelationship CreateRelationship(string id, string type, string target)
    {
        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = id;
        relationship.Type = type;
        relationship.Target = target;
        return relationship;
    }

    internal static string GetWorksheetDrawingRelationshipTarget(string drawingPath)
    {
        string fileName = GetFileName(drawingPath);
        return "../drawings/" + fileName;
    }

    internal static string GetDrawingImageRelationshipTarget(string imagePath)
    {
        string fileName = GetFileName(imagePath);
        return "../media/" + fileName;
    }

    internal static string GetFileName(string path)
    {
        if (path == null)
        {
            return "";
        }

        int slashIndex = path.LastIndexOf('/');
        if (slashIndex < 0 || slashIndex == path.Length - 1)
        {
            return path;
        }

        return path.Substring(slashIndex + 1);
    }

    internal static void CopyPlacementDiagnostics(XlsxDrawingImagePlacementResult target, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Returns sheet from the current model.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <returns>The workbook sheet produced by the operation.</returns>
    internal XlsxWorkbookSheet? GetSheet(string name)
    {
        return WorkbookPart.GetSheetByName(name);
    }

    /// <summary>
    /// Returns sheet at from the current model.
    /// </summary>
    /// <param name = "index">The index value used by the operation.</param>
    /// <returns>The workbook sheet produced by the operation.</returns>
    internal XlsxWorkbookSheet? GetSheetAt(int index)
    {
        if (index < 0 || index >= WorkbookPart.Sheets.Count)
        {
            return null;
        }

        return WorkbookPart.Sheets[index];
    }

    /// <summary>
    /// Removes sheet and any directly associated model references.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxValidationResult RemoveSheet(string name)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        int index = GetSheetIndex(name);
        if (index < 0)
        {
            result.AddError("workbook.sheet.notFound", "Workbook sheet was not found.");
            return result;
        }

        if (WorkbookPart.Sheets.Count <= 1)
        {
            result.AddError("workbook.sheet.removeLast", "Workbook must retain at least one sheet.");
            return result;
        }

        RemoveWorkbookSheetAt(index);
        return result;
    }

    /// <summary>
    /// Performs the list external links operation using the current model state.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    internal List<XlsxExternalLinkPart> ListExternalLinks()
    {
        if (ExternalLinks == null)
        {
            ExternalLinks = new List<XlsxExternalLinkPart>();
        }

        return ExternalLinks;
    }

    /// <summary>
    /// Returns connections from the current model.
    /// </summary>
    /// <returns>The workbook connections part produced by the operation.</returns>
    internal XlsxWorkbookConnectionsPart GetConnections()
    {
        if (Connections == null)
        {
            Connections = new XlsxWorkbookConnectionsPart();
            HasConnections = false;
        }

        return Connections;
    }

    /// <summary>
    /// Performs the list query tables operation using the current model state.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    internal List<XlsxQueryTablePart> ListQueryTables()
    {
        if (QueryTables == null)
        {
            QueryTables = new List<XlsxQueryTablePart>();
        }

        return QueryTables;
    }

    /// <summary>
    /// Performs the list custom xml parts operation using the current model state.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    internal List<XlsxCustomXmlPart> ListCustomXmlParts()
    {
        if (CustomXmlParts == null)
        {
            CustomXmlParts = new List<XlsxCustomXmlPart>();
        }

        return CustomXmlParts;
    }

    /// <summary>
    /// Performs the list embedded objects operation using the current model state.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    internal List<XlsxEmbeddedObjectPart> ListEmbeddedObjects()
    {
        if (EmbeddedObjects == null)
        {
            EmbeddedObjects = new List<XlsxEmbeddedObjectPart>();
        }

        return EmbeddedObjects;
    }

    /// <summary>
    /// Performs the list in cell picture metadata parts operation using the current model state.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    internal List<XlsxInCellPictureMetadataPart> ListInCellPictureMetadataParts()
    {
        if (InCellPictureMetadataParts == null)
        {
            InCellPictureMetadataParts = new List<XlsxInCellPictureMetadataPart>();
        }

        return InCellPictureMetadataParts;
    }

    internal XlsxEmbeddedObjectPart AddOleObject(XlsxWorksheetPart worksheet, XlsxRelationshipsPart relationships, byte[] data, string programId, uint shapeId)
    {
        string partPath = GetNextEmbeddedObjectPath("oleObject", "bin");
        return AddRawEmbeddedObject(worksheet, relationships, partPath, XlsxContentTypes.OleObject, data, true, programId, shapeId);
    }

    internal XlsxEmbeddedObjectPart AddEmbeddedPackage(XlsxWorksheetPart worksheet, XlsxRelationshipsPart relationships, byte[] data, string contentType, string extension, string programId, uint shapeId)
    {
        string partPath = GetNextEmbeddedObjectPath("embeddedPackage", extension);
        return AddRawEmbeddedObject(worksheet, relationships, partPath, contentType, data, false, programId, shapeId);
    }

    internal XlsxEmbeddedObjectPart AddRawEmbeddedObject(XlsxWorksheetPart worksheet, XlsxRelationshipsPart relationships, string partPath, string contentType, byte[] data, bool oleObject, string programId, uint shapeId)
    {
        XlsxEmbeddedObjectPart part = new XlsxEmbeddedObjectPartReader().ReadBytes(partPath, contentType, data);
        part.IsOleObject = oleObject;
        part.IsEmbeddedPackage = !oleObject;
        part.ProgramId = XlsxCellValueConverter.NormalizeText(programId);
        part.ShapeId = shapeId;
        if (worksheet == null) { part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "embeddedObject.worksheetNull", "Embedded object authoring requires a worksheet.")); }
        if (relationships == null) { part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "embeddedObject.relationshipsNull", "Embedded object authoring requires worksheet relationships.")); }
        if (!part.PartPath.StartsWith(XlsxPackagePaths.EmbeddingsDirectory, System.StringComparison.Ordinal)) { part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "embeddedObject.pathInvalid", "Embedded object parts must be stored under /xl/embeddings/.")); }
        if ((UnknownParts != null && UnknownParts.ContainsPart(part.PartPath)) || ContainsEmbeddedObjectPath(part.PartPath)) { part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "embeddedObject.partDuplicate", "An embedded object already exists at the requested path.")); }
        if (!part.IsValid || worksheet == null || relationships == null) { return part; }

        string relationshipId = GetNextRelationshipId(relationships);
        XlsxRelationship relationship = CreateRelationship(relationshipId, oleObject ? XlsxRelationshipTypes.OleObject : XlsxRelationshipTypes.EmbeddedPackage, "../embeddings/" + GetFileName(part.PartPath));
        relationship.TargetPath = part.PartPath;
        relationships.Relationships.Add(relationship);
        XlsxWorksheetEmbeddedObjectReference reference = new XlsxWorksheetEmbeddedObjectReference();
        reference.RelationshipId = relationshipId;
        reference.ProgramId = part.ProgramId.Length == 0 ? "Package" : part.ProgramId;
        reference.ShapeId = shapeId;
        reference.HasShapeId = true;
        reference.IsOleObject = oleObject;
        worksheet.EmbeddedObjectReferences.Add(reference);
        part.RelationshipId = relationshipId;
        part.SourcePartPath = worksheet.PartPath;
        if (UnknownParts == null) { UnknownParts = new XlsxUnknownPartStore(); }
        UnknownParts.AddPart(part.PartPath, part.ContentType, part.Data);
        EmbeddedObjects.Add(part);
        ReplaceWorksheetAggregate(worksheet);
        StoreWorksheetAndRelationships(worksheet, relationships, part.Validation);
        return part;
    }

    internal XlsxValidationResult RemoveEmbeddedObject(XlsxWorksheetPart worksheet, XlsxRelationshipsPart relationships, string partPath)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (worksheet == null) { result.AddError("embeddedObject.worksheetNull", "Embedded object removal requires a worksheet."); return result; }
        if (relationships == null) { result.AddError("embeddedObject.relationshipsNull", "Embedded object removal requires worksheet relationships."); return result; }
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(partPath);
        CopyDiagnostics(result, normalized.Validation);
        if (!result.IsValid) { return result; }
        bool removed = false;
        int index = 0;
        while (index < EmbeddedObjects.Count)
        {
            if (EmbeddedObjects[index].PartPath == normalized.Path) { EmbeddedObjects.RemoveAt(index); removed = true; continue; }
            index = index + 1;
        }
        if (!removed) { result.AddError("embeddedObject.partNotFound", "The embedded object part was not found."); return result; }
        RemoveUnknownPart(normalized.Path);
        RemovePackagePartByPath(normalized.Path);
        List<string> removedRelationshipIds = new List<string>();
        index = 0;
        while (index < relationships.Relationships.Count)
        {
            XlsxRelationship relationship = relationships.Relationships[index];
            XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget(relationships.SourcePartPath, relationship.Target, false);
            if (target.Validation.IsValid && target.Path == normalized.Path)
            {
                removedRelationshipIds.Add(XlsxCellValueConverter.NormalizeText(relationship.Id));
                relationships.Relationships.RemoveAt(index);
                continue;
            }
            index = index + 1;
        }
        index = 0;
        while (index < worksheet.EmbeddedObjectReferences.Count)
        {
            if (ContainsText(removedRelationshipIds, worksheet.EmbeddedObjectReferences[index].RelationshipId)) { worksheet.EmbeddedObjectReferences.RemoveAt(index); continue; }
            index = index + 1;
        }
        StoreWorksheetAndRelationships(worksheet, relationships, result);
        return result;
    }

    internal XlsxVbaProjectPart SetVbaProject(byte[] data)
    {
        XlsxVbaProjectPart part = new XlsxVbaProjectPartReader().ReadBytes(XlsxPackagePaths.VbaProject, data);
        if (!part.IsValid) { return part; }
        if (UnknownParts == null) { UnknownParts = new XlsxUnknownPartStore(); }
        UnknownParts.RemovePart(part.PartPath);
        UnknownParts.AddPart(part.PartPath, part.ContentType, part.Data);
        RemoveRelationshipsByType(WorkbookRelationships, XlsxRelationshipTypes.VbaProject);
        XlsxRelationship relationship = CreateRelationship(GetNextRelationshipId(WorkbookRelationships), XlsxRelationshipTypes.VbaProject, "vbaProject.bin");
        relationship.TargetPath = part.PartPath;
        WorkbookRelationships.Relationships.Add(relationship);
        VbaProject = part;
        HasVbaProject = true;
        if (PackageVariant == XlsxPackageVariant.Xltx) { PackageVariant = XlsxPackageVariant.Xltm; }
        else if (PackageVariant == XlsxPackageVariant.Xlsx) { PackageVariant = XlsxPackageVariant.Xlsm; }
        return part;
    }

    internal XlsxDrawingObjectOperationResult AddFormControl(XlsxDrawingPart drawing, XlsxRelationshipsPart relationships, byte[] controlProperties, string contentType, string fromCell, string extentCxEmu, string extentCyEmu, string name)
    {
        XlsxDrawingObjectOperationResult result = new XlsxDrawingObjectOperationResult();
        if (drawing == null) { result.AddError("formControl.drawingNull", "Form control authoring requires a drawing part."); return result; }
        if (relationships == null) { result.AddError("formControl.relationshipsNull", "Form control authoring requires drawing relationships."); return result; }
        if (controlProperties == null) { result.AddError("formControl.dataNull", "Form control property bytes must not be null."); return result; }
        string partPath = GetNextControlPropertiesPath();
        string relationshipId = GetNextRelationshipId(relationships);
        XlsxDrawingObjectOperationResult anchorResult = drawing.AddOneCellFormControl(relationshipId, fromCell, extentCxEmu, extentCyEmu, name);
        foreach (XlsxDiagnostic diagnostic in anchorResult.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        if (!result.IsValid) { return result; }
        XlsxRelationship relationship = CreateRelationship(relationshipId, XlsxRelationshipTypes.ControlProperties, "../ctrlProps/" + GetFileName(partPath));
        relationship.TargetPath = partPath;
        relationships.Relationships.Add(relationship);
        if (UnknownParts == null) { UnknownParts = new XlsxUnknownPartStore(); }
        string resolvedContentType = XlsxCellValueConverter.NormalizeText(contentType);
        if (resolvedContentType.Length == 0) { resolvedContentType = XlsxContentTypes.ControlProperties; }
        UnknownParts.AddPart(partPath, resolvedContentType, controlProperties);
        ReplaceDrawingAggregate(drawing);
        StoreDrawingAndRelationships(drawing, relationships, result);
        result.Anchor = anchorResult.Anchor;
        result.HasAnchor = anchorResult.HasAnchor;
        return result;
    }

    internal XlsxDrawingObjectOperationResult RemoveFormControl(XlsxDrawingPart drawing, XlsxRelationshipsPart relationships, int controlIndex)
    {
        XlsxDrawingObjectOperationResult invalid = new XlsxDrawingObjectOperationResult();
        if (drawing == null) { invalid.AddError("formControl.drawingNull", "Form control removal requires a drawing part."); return invalid; }
        if (relationships == null) { invalid.AddError("formControl.relationshipsNull", "Form control removal requires drawing relationships."); return invalid; }
        XlsxDrawingObjectOperationResult result = drawing.RemoveFormControl(controlIndex);
        if (!result.IsValid || result.Anchor == null) { return result; }
        string relationshipId = result.Anchor.RelationshipId;
        int index = 0;
        while (index < relationships.Relationships.Count)
        {
            XlsxRelationship relationship = relationships.Relationships[index];
            if (relationship.Id == relationshipId)
            {
                XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget(relationships.SourcePartPath, relationship.Target, false);
                if (target.Validation.IsValid) { RemoveUnknownPart(target.Path); RemovePackagePartByPath(target.Path); }
                relationships.Relationships.RemoveAt(index);
                continue;
            }
            index = index + 1;
        }
        ReplaceDrawingAggregate(drawing);
        StoreDrawingAndRelationships(drawing, relationships, result);
        return result;
    }

    internal string GetNextControlPropertiesPath()
    {
        int index = 1;
        while (true)
        {
            string path = XlsxPackagePaths.ControlPropertiesDirectory + "ctrlProp" + index.ToString(CultureInfo.InvariantCulture) + ".xml";
            if (UnknownParts == null || !UnknownParts.ContainsPart(path)) { return path; }
            index = index + 1;
        }
    }

    internal void StoreDrawingAndRelationships(XlsxDrawingPart drawing, XlsxRelationshipsPart relationships, XlsxDrawingObjectOperationResult result)
    {
        if (relationships.SourcePartPath.Length == 0) { relationships.SourcePartPath = drawing.PartPath; }
        if (relationships.PartPath.Length == 0) { relationships.PartPath = XlsxPackagePath.GetRelationshipPartPath(drawing.PartPath); }
        XlsxDrawingPartWriteResult drawingWrite = new XlsxDrawingPartWriter().WriteBytes(drawing);
        foreach (XlsxDiagnostic diagnostic in drawingWrite.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        XlsxRelationshipsWriteResult relationshipsWrite = new XlsxPartRelationshipsWriter().WriteBytes(relationships);
        foreach (XlsxDiagnostic diagnostic in relationshipsWrite.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        if (!result.IsValid) { return; }
        UnknownParts.RemovePart(drawing.PartPath);
        UnknownParts.RemovePart(relationships.PartPath);
        UnknownParts.AddPart(drawing.PartPath, XlsxContentTypes.Drawing, drawingWrite.Bytes);
        UnknownParts.AddPart(relationships.PartPath, XlsxContentTypes.Relationships, relationshipsWrite.Bytes);
        ReplaceRelationshipAggregate(relationships);
    }

    internal XlsxValidationResult RemoveVbaProject()
    {
        return RemoveMacros();
    }

    internal XlsxInCellPictureMetadataPart AddInCellPictureMetadata(string partPath, string contentType, byte[] data)
    {
        XlsxInCellPictureMetadataPart part = new XlsxInCellPictureMetadataPartReader().ReadBytes(partPath, contentType, data);
        if (!part.IsSheetMetadata && !part.IsRichDataPart) { part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "inCellPictureMetadata.pathInvalid", "In-cell picture metadata must use /xl/metadata.xml or a path under /xl/richData/.")); }
        if (UnknownParts != null && UnknownParts.ContainsPart(part.PartPath)) { part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "inCellPictureMetadata.partDuplicate", "An in-cell picture metadata part already exists at the requested path.")); }
        if (!part.IsValid) { return part; }
        if (UnknownParts == null) { UnknownParts = new XlsxUnknownPartStore(); }
        UnknownParts.AddPart(part.PartPath, part.ContentType, part.Data);
        InCellPictureMetadataParts.Add(part);
        return part;
    }

    internal XlsxValidationResult RemoveInCellPictureMetadata(string partPath)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(partPath);
        CopyDiagnostics(result, normalized.Validation);
        if (!result.IsValid) { return result; }
        bool removed = false;
        int index = 0;
        while (index < InCellPictureMetadataParts.Count)
        {
            if (InCellPictureMetadataParts[index].PartPath == normalized.Path) { InCellPictureMetadataParts.RemoveAt(index); removed = true; continue; }
            index = index + 1;
        }
        if (!removed) { result.AddError("inCellPictureMetadata.partNotFound", "The in-cell picture metadata part was not found."); return result; }
        RemoveUnknownPart(normalized.Path);
        RemoveUnknownPart(XlsxPackagePath.GetRelationshipPartPath(normalized.Path));
        RemovePackagePartByPath(normalized.Path);
        return result;
    }

    internal string GetNextEmbeddedObjectPath(string prefix, string extension)
    {
        string normalizedExtension = XlsxCellValueConverter.NormalizeText(extension);
        if (normalizedExtension.StartsWith(".")) { normalizedExtension = normalizedExtension.Substring(1); }
        if (normalizedExtension.Length == 0) { normalizedExtension = "bin"; }
        int index = 1;
        while (true)
        {
            string path = XlsxPackagePaths.EmbeddingsDirectory + prefix + index.ToString(CultureInfo.InvariantCulture) + "." + normalizedExtension;
            if ((UnknownParts == null || !UnknownParts.ContainsPart(path)) && !ContainsEmbeddedObjectPath(path)) { return path; }
            index = index + 1;
        }
    }

    internal bool ContainsEmbeddedObjectPath(string partPath)
    {
        foreach (XlsxEmbeddedObjectPart part in EmbeddedObjects) { if (part.PartPath == partPath) { return true; } }
        return false;
    }

    internal void ReplaceWorksheetAggregate(XlsxWorksheetPart worksheet)
    {
        int index = 0;
        while (index < Worksheets.Count)
        {
            if (Worksheets[index].PartPath == worksheet.PartPath) { Worksheets[index] = worksheet; return; }
            index = index + 1;
        }
        Worksheets.Add(worksheet);
    }

    internal void StoreWorksheetAndRelationships(XlsxWorksheetPart worksheet, XlsxRelationshipsPart relationships, XlsxValidationResult validation)
    {
        if (relationships.SourcePartPath.Length == 0) { relationships.SourcePartPath = worksheet.PartPath; }
        if (relationships.PartPath.Length == 0) { relationships.PartPath = XlsxPackagePath.GetRelationshipPartPath(worksheet.PartPath); }
        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteBytes(worksheet);
        CopyDiagnostics(validation, worksheetWrite.Validation);
        XlsxRelationshipsWriteResult relationshipsWrite = new XlsxPartRelationshipsWriter().WriteBytes(relationships);
        CopyDiagnostics(validation, relationshipsWrite.Validation);
        if (!validation.IsValid) { return; }
        UnknownParts.RemovePart(worksheet.PartPath);
        UnknownParts.RemovePart(relationships.PartPath);
        UnknownParts.AddPart(worksheet.PartPath, XlsxContentTypes.Worksheet, worksheetWrite.Bytes);
        UnknownParts.AddPart(relationships.PartPath, XlsxContentTypes.Relationships, relationshipsWrite.Bytes);
        ReplaceRelationshipAggregate(relationships);
    }

    internal void ReplaceRelationshipAggregate(XlsxRelationshipsPart relationships)
    {
        int index = 0;
        while (index < RelationshipParts.Count)
        {
            if (RelationshipParts[index].PartPath == relationships.PartPath) { RelationshipParts[index] = relationships; return; }
            index = index + 1;
        }
        RelationshipParts.Add(relationships);
    }

    internal void RemovePackagePartByPath(string partPath)
    {
        int index = 0;
        while (index < PackageParts.Count)
        {
            if (PackageParts[index].Path == partPath) { PackageParts.RemoveAt(index); continue; }
            index = index + 1;
        }
    }

    internal static bool ContainsText(List<string> values, string value)
    {
        foreach (string item in values) { if (item == value) { return true; } }
        return false;
    }

    /// <summary>
    /// Performs the list digital signatures operation using the current model state.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    internal List<XlsxDigitalSignaturePart> ListDigitalSignatures()
    {
        if (DigitalSignatures == null)
        {
            DigitalSignatures = new List<XlsxDigitalSignaturePart>();
            HasDigitalSignatures = false;
        }

        return DigitalSignatures;
    }

    /// <summary>
    /// Removes custom xml parts and any directly associated model references.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxValidationResult RemoveCustomXmlParts()
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (PackageRelationships != null && PackageRelationships.Relationships != null)
        {
            int index = 0;
            while (index < PackageRelationships.Relationships.Count)
            {
                XlsxRelationship relationship = PackageRelationships.Relationships[index];
                if (relationship != null && relationship.Type == XlsxRelationshipTypes.CustomXml)
                {
                    RemoveCustomXmlRelationshipParts(relationship);
                    PackageRelationships.Relationships.RemoveAt(index);
                    continue;
                }

                index = index + 1;
            }
        }

        RemoveUnknownPartsByPrefix(XlsxPackagePaths.CustomXmlDirectory);
        if (CustomXmlParts != null)
        {
            CustomXmlParts.Clear();
        }

        return result;
    }

    /// <summary>
    /// Removes macros and any directly associated model references.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxValidationResult RemoveMacros()
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (WorkbookRelationships != null && WorkbookRelationships.Relationships != null)
        {
            int index = 0;
            while (index < WorkbookRelationships.Relationships.Count)
            {
                XlsxRelationship relationship = WorkbookRelationships.Relationships[index];
                if (relationship != null && relationship.Type == XlsxRelationshipTypes.VbaProject)
                {
                    RemoveVbaProjectUnknownParts(relationship);
                    WorkbookRelationships.Relationships.RemoveAt(index);
                    continue;
                }

                index = index + 1;
            }
        }

        if (VbaProject != null)
        {
            RemoveUnknownPart(VbaProject.PartPath);
        }

        RemoveUnknownPart(XlsxPackagePaths.VbaProject);
        VbaProject = new XlsxVbaProjectPart();
        HasVbaProject = false;
        PackageVariant = GetMacroFreePackageVariant(PackageVariant);
        return result;
    }

    /// <summary>
    /// Performs the strip macros operation using the current model state.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxValidationResult StripMacros()
    {
        return RemoveMacros();
    }

    internal void RemoveVbaProjectUnknownParts(XlsxRelationship relationship)
    {
        if (relationship == null)
        {
            return;
        }

        XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, relationship.Target, relationship.IsExternal || relationship.TargetMode == "External");
        if (!target.Validation.IsValid || target.IsExternal)
        {
            return;
        }

        RemoveUnknownPart(target.Path);
        RemoveUnknownPart(XlsxPackagePath.GetRelationshipPartPath(target.Path));
    }

    internal static XlsxPackageVariant GetMacroFreePackageVariant(XlsxPackageVariant packageVariant)
    {
        if (packageVariant == XlsxPackageVariant.Xltm)
        {
            return XlsxPackageVariant.Xltx;
        }

        return XlsxPackageVariant.Xlsx;
    }

    internal void RemoveCustomXmlRelationshipParts(XlsxRelationship relationship)
    {
        if (relationship == null)
        {
            return;
        }

        XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget("/", relationship.Target, relationship.IsExternal || relationship.TargetMode == "External");
        if (!target.Validation.IsValid || target.IsExternal)
        {
            return;
        }

        RemoveUnknownPart(target.Path);
        RemoveUnknownPart(XlsxPackagePath.GetRelationshipPartPath(target.Path));
    }

    /// <summary>
    /// Removes external links and any directly associated model references.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxValidationResult RemoveExternalLinks()
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (WorkbookPart != null && WorkbookPart.ExternalReferences != null)
        {
            WorkbookPart.ExternalReferences.Clear();
        }

        if (WorkbookRelationships != null && WorkbookRelationships.Relationships != null)
        {
            int index = 0;
            while (index < WorkbookRelationships.Relationships.Count)
            {
                XlsxRelationship relationship = WorkbookRelationships.Relationships[index];
                if (relationship != null && relationship.Type == XlsxRelationshipTypes.ExternalLink)
                {
                    RemoveExternalLinkUnknownParts(relationship);
                    WorkbookRelationships.Relationships.RemoveAt(index);
                    continue;
                }

                index = index + 1;
            }
        }

        if (ExternalLinks != null)
        {
            foreach (XlsxExternalLinkPart part in ExternalLinks)
            {
                if (part != null)
                {
                    RemoveUnknownPart(part.PartPath);
                    RemoveUnknownPart(XlsxPackagePath.GetRelationshipPartPath(part.PartPath));
                }
            }

            ExternalLinks.Clear();
        }

        return result;
    }

    internal void RemoveExternalLinkUnknownParts(XlsxRelationship relationship)
    {
        if (relationship == null)
        {
            return;
        }

        XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, relationship.Target, relationship.IsExternal || relationship.TargetMode == "External");
        if (!target.Validation.IsValid || target.IsExternal)
        {
            return;
        }

        RemoveUnknownPart(target.Path);
        RemoveUnknownPart(XlsxPackagePath.GetRelationshipPartPath(target.Path));
    }

    internal void RemoveUnknownPart(string partPath)
    {
        if (UnknownParts == null)
        {
            return;
        }

        if (partPath == null || partPath.Length == 0)
        {
            return;
        }

        UnknownParts.RemovePart(partPath);
    }

    internal void RemoveUnknownPartsByPrefix(string partPathPrefix)
    {
        if (UnknownParts == null || UnknownParts.Parts == null || partPathPrefix == null || partPathPrefix.Length == 0)
        {
            return;
        }

        int index = 0;
        while (index < UnknownParts.Parts.Count)
        {
            XlsxUnknownPart part = UnknownParts.Parts[index];
            if (part != null && part.Path != null && part.Path.StartsWith(partPathPrefix, System.StringComparison.Ordinal))
            {
                UnknownParts.Parts.RemoveAt(index);
                continue;
            }

            index = index + 1;
        }
    }

    /// <summary>
    /// Performs the sanitize operation using the current model state.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookSanitizeResult Sanitize()
    {
        return SanitizeWithOptions(new XlsxWorkbookSanitizeOptions());
    }

    /// <summary>
    /// Performs the sanitize operation using the current model state.
    /// </summary>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookSanitizeResult SanitizeWithOptions(XlsxWorkbookSanitizeOptions options)
    {
        XlsxWorkbookSanitizeResult result = new XlsxWorkbookSanitizeResult();
        XlsxWorkbookSanitizeOptions effectiveOptions = options;
        if (effectiveOptions == null)
        {
            effectiveOptions = new XlsxWorkbookSanitizeOptions();
        }

        if (effectiveOptions.RemoveMacros)
        {
            int macroCount = CountMacros();
            XlsxValidationResult removeResult = RemoveMacros();
            XlsxWorkbookSanitizeResult.AddDiagnostics(result, removeResult);
            result.MacrosRemoved = macroCount;
        }

        if (effectiveOptions.RemoveExternalLinks)
        {
            int externalLinkCount = CountExternalLinks();
            XlsxValidationResult removeResult = RemoveExternalLinks();
            XlsxWorkbookSanitizeResult.AddDiagnostics(result, removeResult);
            result.ExternalLinksRemoved = externalLinkCount;
        }

        if (effectiveOptions.RemoveComments)
        {
            RemoveWorkbookComments(result);
        }

        if (effectiveOptions.RemoveHiddenSheets)
        {
            RemoveHiddenSheetsForSanitizer(result);
        }

        if (effectiveOptions.RemoveCustomXmlParts)
        {
            int customXmlCount = CountCustomXmlParts();
            XlsxValidationResult removeResult = RemoveCustomXmlParts();
            XlsxWorkbookSanitizeResult.AddDiagnostics(result, removeResult);
            result.CustomXmlPartsRemoved = customXmlCount;
        }

        if (effectiveOptions.RemovePersonalMetadata)
        {
            RemovePersonalMetadata(result);
        }

        if (effectiveOptions.RemoveOleObjects)
        {
            RemoveOleObjects(result);
        }

        ScrubUnsafeRelationshipsFromUnknownParts(result, effectiveOptions);
        ScrubUnsafeWorksheetXmlFromUnknownParts(result, effectiveOptions);
        if (effectiveOptions.RemoveUnknownParts)
        {
            RemoveUnknownPackageParts(result, effectiveOptions);
        }

        result.IsValid = result.Validation.IsValid;
        return result;
    }

    /// <summary>
    /// Performs the compare to operation using the current model state.
    /// </summary>
    /// <param name = "other">The other value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookCompareResult CompareTo(XlsxWorkbook other)
    {
        return XlsxWorkbookComparer.Compare(this, other, new XlsxWorkbookCompareOptions());
    }

    /// <summary>
    /// Performs the compare to operation using the current model state.
    /// </summary>
    /// <param name = "other">The other value used by the operation.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookCompareResult CompareToWithOptions(XlsxWorkbook other, XlsxWorkbookCompareOptions options)
    {
        return XlsxWorkbookComparer.Compare(this, other, options);
    }

    /// <summary>
    /// Checks the current model or options and returns all validation diagnostics found.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxValidationResult Validate()
    {
        return ValidateUsingWithOptions(new XlsxWorkbookValidator(), new XlsxWorkbookValidationOptions());
    }

    /// <summary>
    /// Checks the current model or options and returns all validation diagnostics found.
    /// </summary>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxValidationResult ValidateWithOptions(XlsxWorkbookValidationOptions options)
    {
        return ValidateUsingWithOptions(new XlsxWorkbookValidator(), options);
    }

    /// <summary>
    /// Checks the current model with a supplied validation implementation.
    /// </summary>
    /// <param name = "validator">The validation implementation used by the operation.</param>
    /// <returns>A result containing the validity flag and diagnostics.</returns>
    internal XlsxValidationResult ValidateUsing(IXlsxWorkbookValidator validator)
    {
        return ValidateUsingWithOptions(validator, new XlsxWorkbookValidationOptions());
    }

    /// <summary>
    /// Checks the current model with a supplied validation implementation and options.
    /// </summary>
    /// <param name = "validator">The validation implementation used by the operation.</param>
    /// <param name = "options">The options that control validation behavior and limits.</param>
    /// <returns>A result containing the validity flag and diagnostics.</returns>
    internal XlsxValidationResult ValidateUsingWithOptions(IXlsxWorkbookValidator validator, XlsxWorkbookValidationOptions options)
    {
        if (validator == null)
        {
            XlsxValidationResult result = new XlsxValidationResult();
            result.AddError("workbook.validatorNull", "Workbook validator must not be null.");
            return result;
        }

        XlsxWorkbookValidationOptions effectiveOptions = options;
        if (effectiveOptions == null)
        {
            effectiveOptions = new XlsxWorkbookValidationOptions();
        }

        return validator.ValidateWorkbookWithOptions(this, effectiveOptions);
    }

    /// <summary>
    /// Performs the repair operation using the current model state.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookRepairResult Repair()
    {
        return RepairUsingWithOptions(new XlsxWorkbookRepairer(), new XlsxWorkbookRepairOptions());
    }

    /// <summary>
    /// Performs the repair operation using the current model state.
    /// </summary>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookRepairResult RepairWithOptions(XlsxWorkbookRepairOptions options)
    {
        return RepairUsingWithOptions(new XlsxWorkbookRepairer(), options);
    }

    /// <summary>
    /// Repairs the current model with a supplied repair implementation.
    /// </summary>
    /// <param name = "repairer">The repair implementation used by the operation.</param>
    /// <returns>A result containing the repaired workbook, repairs, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookRepairResult RepairUsing(IXlsxWorkbookRepairer repairer)
    {
        return RepairUsingWithOptions(repairer, new XlsxWorkbookRepairOptions());
    }

    /// <summary>
    /// Repairs the current model with a supplied repair implementation and options.
    /// </summary>
    /// <param name = "repairer">The repair implementation used by the operation.</param>
    /// <param name = "options">The options that control repair behavior.</param>
    /// <returns>A result containing the repaired workbook, repairs, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookRepairResult RepairUsingWithOptions(IXlsxWorkbookRepairer repairer, XlsxWorkbookRepairOptions options)
    {
        if (repairer == null)
        {
            XlsxWorkbookRepairResult result = new XlsxWorkbookRepairResult();
            result.Workbook = this;
            result.AddError("workbook.repairerNull", "Workbook repairer must not be null.");
            return result;
        }

        XlsxWorkbookRepairOptions effectiveOptions = options;
        if (effectiveOptions == null)
        {
            effectiveOptions = new XlsxWorkbookRepairOptions();
        }

        return repairer.RepairWorkbookWithOptions(this, effectiveOptions);
    }

    /// <summary>
    /// Performs the count macros operation using the current model state.
    /// </summary>
    /// <returns>The int value produced by the operation.</returns>
    internal int CountMacros()
    {
        int count = 0;
        if (HasVbaProject)
        {
            count = count + 1;
        }

        if (VbaProject != null && VbaProject.Data != null && VbaProject.Data.Length > 0 && !HasVbaProject)
        {
            count = count + 1;
        }

        return count + CountRelationshipsByType(WorkbookRelationships, XlsxRelationshipTypes.VbaProject);
    }

    /// <summary>
    /// Performs the count external links operation using the current model state.
    /// </summary>
    /// <returns>The int value produced by the operation.</returns>
    internal int CountExternalLinks()
    {
        int count = CountRelationshipsByType(WorkbookRelationships, XlsxRelationshipTypes.ExternalLink);
        if (WorkbookPart != null && WorkbookPart.ExternalReferences != null)
        {
            count = count + WorkbookPart.ExternalReferences.Count;
        }

        if (ExternalLinks != null)
        {
            count = count + ExternalLinks.Count;
        }

        return count;
    }

    /// <summary>
    /// Performs the count custom xml parts operation using the current model state.
    /// </summary>
    /// <returns>The int value produced by the operation.</returns>
    internal int CountCustomXmlParts()
    {
        int count = CountRelationshipsByType(PackageRelationships, XlsxRelationshipTypes.CustomXml);
        if (CustomXmlParts != null)
        {
            count = count + CustomXmlParts.Count;
        }

        return count;
    }

    internal void RemoveWorkbookComments(XlsxWorkbookSanitizeResult result)
    {
        if (Worksheets == null)
        {
            return;
        }

        foreach (XlsxWorksheetPart worksheet in Worksheets)
        {
            RemoveWorksheetComments(result, worksheet);
        }

        int removedUnknownComments = RemoveUnknownPartsByPrefixAndCount(XlsxPackagePaths.CommentsDirectory);
        result.UnknownPartsRemoved = result.UnknownPartsRemoved + removedUnknownComments;
        int removedThreadedComments = RemoveUnknownPartsByPrefixAndCount("/xl/threadedComments/");
        result.UnknownPartsRemoved = result.UnknownPartsRemoved + removedThreadedComments;
        if (Comments != null)
        {
            Comments.Clear();
        }
        if (ThreadedComments != null)
        {
            ThreadedComments.Clear();
        }
    }

    internal void RemoveWorksheetComments(XlsxWorkbookSanitizeResult result, XlsxWorksheetPart worksheet)
    {
        if (worksheet == null)
        {
            return;
        }

        if (worksheet.CommentsPart != null)
        {
            if (worksheet.CommentsPart.Comments != null)
            {
                result.CommentsRemoved = result.CommentsRemoved + worksheet.CommentsPart.Comments.Count;
            }

            RemoveUnknownPart(worksheet.CommentsPart.PartPath);
            RemoveUnknownPart(XlsxPackagePath.GetRelationshipPartPath(worksheet.CommentsPart.PartPath));
        }

        if (worksheet.LegacyCommentDrawingPart != null)
        {
            if (worksheet.LegacyCommentDrawingPart.Drawings != null)
            {
                result.CommentDrawingsRemoved = result.CommentDrawingsRemoved + worksheet.LegacyCommentDrawingPart.Drawings.Count;
            }

            RemoveUnknownPart(worksheet.LegacyCommentDrawingPart.PartPath);
            RemoveUnknownPart(XlsxPackagePath.GetRelationshipPartPath(worksheet.LegacyCommentDrawingPart.PartPath));
        }

        worksheet.CommentsPart = new XlsxCommentsPart();
        worksheet.HasCommentsPart = false;
        worksheet.LegacyCommentDrawingPart = new XlsxLegacyCommentDrawingPart();
        worksheet.HasLegacyCommentDrawingPart = false;
        RemoveUnknownXmlEntriesByLocalName(worksheet.UnknownXml, "legacyDrawing");
        RemoveUnknownXmlEntriesByLocalName(worksheet.UnknownXml, "legacyDrawingHF");
        RemoveUnknownXmlEntriesByLocalName(worksheet.UnknownXml, "threadedComments");
    }

    internal void RemoveHiddenSheetsForSanitizer(XlsxWorkbookSanitizeResult result)
    {
        if (WorkbookPart == null || WorkbookPart.Sheets == null)
        {
            result.AddError("workbookSanitize.sheetsNull", "Workbook sheets list must not be null.");
            return;
        }

        int index = WorkbookPart.Sheets.Count - 1;
        while (index >= 0)
        {
            XlsxWorkbookSheet sheet = WorkbookPart.Sheets[index];
            if (IsHiddenWorkbookSheet(sheet))
            {
                if (WorkbookPart.Sheets.Count > 1)
                {
                    RemoveWorkbookSheetAt(index);
                    result.HiddenSheetsRemoved = result.HiddenSheetsRemoved + 1;
                }
                else
                {
                    sheet.State = "";
                    result.HiddenSheetsUnhidden = result.HiddenSheetsUnhidden + 1;
                    result.AddWarning("workbookSanitize.hiddenSheetRetained", "Sanitizer retained and unhid the last remaining workbook sheet.");
                }
            }

            index = index - 1;
        }
    }

    internal void RemoveWorkbookSheetAt(int index)
    {
        if (WorkbookPart == null || WorkbookPart.Sheets == null || index < 0 || index >= WorkbookPart.Sheets.Count)
        {
            return;
        }

        XlsxWorkbookSheet sheet = WorkbookPart.Sheets[index];
        string targetPath = GetWorkbookRelationshipTargetPath(sheet.RelationshipId);
        if (targetPath != null && targetPath.Length > 0)
        {
            RemoveSheetAggregatesByPath(targetPath);
        }

        WorkbookPart.Sheets.RemoveAt(index);
        RemoveWorkbookRelationship(sheet.RelationshipId);
        RemapDefinedNamesForRemovedSheet(index);
        RemapWorkbookViewsForRemovedSheet(index);
    }

    internal void RemoveSheetAggregatesByPath(string partPath)
    {
        RemoveWorksheetPartByPath(partPath);
        int index = 0;
        while (index < Chartsheets.Count)
        {
            XlsxChartsheetPart chartsheet = Chartsheets[index];
            if (chartsheet != null && chartsheet.PartPath == partPath)
            {
                Chartsheets.RemoveAt(index);
                continue;
            }
            index = index + 1;
        }

        index = 0;
        while (index < Macrosheets.Count)
        {
            XlsxWorkbookSheet macrosheet = Macrosheets[index];
            if (macrosheet != null && macrosheet.TargetPath == partPath)
            {
                Macrosheets.RemoveAt(index);
                continue;
            }
            index = index + 1;
        }

        index = 0;
        while (index < Dialogsheets.Count)
        {
            XlsxWorkbookSheet dialogsheet = Dialogsheets[index];
            if (dialogsheet != null && dialogsheet.TargetPath == partPath)
            {
                Dialogsheets.RemoveAt(index);
                continue;
            }
            index = index + 1;
        }

        string relationshipPartPath = XlsxPackagePath.GetRelationshipPartPath(partPath);
        RemoveRelationshipAggregateByPath(relationshipPartPath);
        RemoveUnknownPart(partPath);
        RemoveUnknownPart(relationshipPartPath);
        RemovePackagePartByPath(partPath);
        RemovePackagePartByPath(relationshipPartPath);
    }

    internal void RemoveRelationshipAggregateByPath(string partPath)
    {
        int index = 0;
        while (index < RelationshipParts.Count)
        {
            XlsxRelationshipsPart relationships = RelationshipParts[index];
            if (relationships != null && relationships.PartPath == partPath)
            {
                RelationshipParts.RemoveAt(index);
                continue;
            }
            index = index + 1;
        }
    }

    internal void RemapDefinedNamesForRemovedSheet(int removedIndex)
    {
        if (WorkbookPart.DefinedNames == null)
        {
            return;
        }

        int index = 0;
        while (index < WorkbookPart.DefinedNames.Count)
        {
            XlsxWorkbookDefinedName definedName = WorkbookPart.DefinedNames[index];
            if (definedName != null && definedName.HasLocalSheetId)
            {
                if (definedName.LocalSheetId == removedIndex)
                {
                    WorkbookPart.DefinedNames.RemoveAt(index);
                    continue;
                }
                if (definedName.LocalSheetId > removedIndex)
                {
                    definedName.LocalSheetId = definedName.LocalSheetId - 1;
                }
            }
            index = index + 1;
        }
    }

    internal void RemapWorkbookViewsForRemovedSheet(int removedIndex)
    {
        if (WorkbookPart.WorkbookViews == null)
        {
            return;
        }

        int remainingCount = WorkbookPart.Sheets.Count;
        foreach (XlsxWorkbookView view in WorkbookPart.WorkbookViews)
        {
            if (view == null)
            {
                continue;
            }
            if (view.HasActiveTab)
            {
                view.ActiveTab = RemapWorkbookViewIndexForRemovedSheet(view.ActiveTab, removedIndex, remainingCount);
            }
            if (view.HasFirstSheet)
            {
                view.FirstSheet = RemapWorkbookViewIndexForRemovedSheet(view.FirstSheet, removedIndex, remainingCount);
            }
        }
    }

    internal static uint RemapWorkbookViewIndexForRemovedSheet(uint value, int removedIndex, int remainingCount)
    {
        uint removed = (uint)removedIndex;
        if (value > removed)
        {
            return value - 1;
        }
        if (value == removed && value >= (uint)remainingCount)
        {
            return (uint)(remainingCount - 1);
        }
        return value;
    }

    internal void RemoveWorksheetPartByPath(string partPath)
    {
        if (Worksheets == null || partPath == null || partPath.Length == 0)
        {
            return;
        }

        int index = 0;
        while (index < Worksheets.Count)
        {
            XlsxWorksheetPart worksheet = Worksheets[index];
            if (worksheet != null && worksheet.PartPath == partPath)
            {
                Worksheets.RemoveAt(index);
                continue;
            }

            index = index + 1;
        }
    }

    internal string GetWorkbookRelationshipTargetPath(string? relationshipId)
    {
        if (WorkbookRelationships == null || WorkbookRelationships.Relationships == null || relationshipId == null || relationshipId.Length == 0)
        {
            return "";
        }

        XlsxRelationship? relationship = WorkbookRelationships.GetRelationship(relationshipId);
        if (relationship == null)
        {
            return "";
        }

        XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, relationship.Target, relationship.IsExternal || relationship.TargetMode == "External");
        if (!target.Validation.IsValid || target.IsExternal)
        {
            return "";
        }

        return target.Path;
    }

    internal void RemovePersonalMetadata(XlsxWorkbookSanitizeResult result)
    {
        if (HasCoreProperties || HasCorePropertiesContent(CoreProperties))
        {
            result.PersonalMetadataPartsRemoved = result.PersonalMetadataPartsRemoved + 1;
        }

        if (HasExtendedProperties || HasExtendedPropertiesContent(ExtendedProperties))
        {
            result.PersonalMetadataPartsRemoved = result.PersonalMetadataPartsRemoved + 1;
        }

        if (HasCustomProperties || HasCustomPropertiesContent(CustomProperties))
        {
            result.PersonalMetadataPartsRemoved = result.PersonalMetadataPartsRemoved + 1;
        }

        CoreProperties = new XlsxCorePropertiesPart();
        HasCoreProperties = false;
        ExtendedProperties = new XlsxExtendedPropertiesPart();
        HasExtendedProperties = false;
        CustomProperties = new XlsxCustomPropertiesPart();
        HasCustomProperties = false;
        RemoveUnknownPart(XlsxPackagePaths.CoreProperties);
        RemoveUnknownPart(XlsxPackagePaths.ExtendedProperties);
        RemoveUnknownPart(XlsxPackagePaths.CustomProperties);
        result.RelationshipsRemoved = result.RelationshipsRemoved + RemoveRelationshipsByType(PackageRelationships, XlsxRelationshipTypes.CoreProperties);
        result.RelationshipsRemoved = result.RelationshipsRemoved + RemoveRelationshipsByType(PackageRelationships, XlsxRelationshipTypes.ExtendedProperties);
        result.RelationshipsRemoved = result.RelationshipsRemoved + RemoveRelationshipsByType(PackageRelationships, XlsxRelationshipTypes.CustomProperties);
    }

    internal void RemoveOleObjects(XlsxWorkbookSanitizeResult result)
    {
        if (EmbeddedObjects != null)
        {
            result.OleObjectsRemoved = result.OleObjectsRemoved + EmbeddedObjects.Count;
            EmbeddedObjects.Clear();
        }

        int removed = RemoveUnknownPartsByPrefixAndCount(XlsxPackagePaths.EmbeddingsDirectory);
        result.UnknownPartsRemoved = result.UnknownPartsRemoved + removed;
        if (Worksheets != null)
        {
            foreach (XlsxWorksheetPart worksheet in Worksheets)
            {
                if (worksheet != null)
                {
                    RemoveUnknownXmlEntriesByLocalName(worksheet.UnknownXml, "oleObjects");
                }
            }
        }
    }

    internal void ScrubUnsafeRelationshipsFromUnknownParts(XlsxWorkbookSanitizeResult result, XlsxWorkbookSanitizeOptions options)
    {
        if (UnknownParts == null || UnknownParts.Parts == null)
        {
            return;
        }

        foreach (XlsxUnknownPart part in UnknownParts.Parts)
        {
            if (part == null || part.Path == null || !part.Path.EndsWith(".rels", System.StringComparison.Ordinal))
            {
                continue;
            }

            XlsxRelationshipsPart relationships = new XlsxPartRelationshipsReader().ReadBytes(part.Path, part.Data, new XlsxReaderOptions());
            if (!relationships.IsValid)
            {
                continue;
            }

            int removed = RemoveUnsafeRelationships(relationships, options);
            if (removed > 0)
            {
                XlsxRelationshipsWriteResult write = new XlsxPartRelationshipsWriter().WriteBytes(relationships);
                if (write.IsValid)
                {
                    part.Data = write.Bytes;
                    result.RelationshipsRemoved = result.RelationshipsRemoved + removed;
                }
            }
        }
    }

    internal void ScrubUnsafeWorksheetXmlFromUnknownParts(XlsxWorkbookSanitizeResult result, XlsxWorkbookSanitizeOptions options)
    {
        if (UnknownParts == null || UnknownParts.Parts == null)
        {
            return;
        }

        foreach (XlsxUnknownPart part in UnknownParts.Parts)
        {
            if (part == null || part.Data == null || !IsWorksheetLikePart(part))
            {
                continue;
            }

            string xml = Encoding.UTF8.GetString(part.Data);
            if (xml.Length == 0)
            {
                continue;
            }

            XmlDocument document = new XmlDocument();
            document.PreserveWhitespace = true;
            document.LoadXml(xml);
            int removed = 0;
            if (options.RemoveComments)
            {
                removed = removed + RemoveElementsByLocalName(document, "legacyDrawing");
                removed = removed + RemoveElementsByLocalName(document, "legacyDrawingHF");
            }

            if (options.RemoveOleObjects)
            {
                removed = removed + RemoveElementsByLocalName(document, "oleObjects");
            }

            if (removed > 0)
            {
                part.Data = Encoding.UTF8.GetBytes(document.OuterXml);
            }
        }
    }

    internal int RemoveUnsafeRelationships(XlsxRelationshipsPart relationships, XlsxWorkbookSanitizeOptions options)
    {
        if (relationships == null || relationships.Relationships == null)
        {
            return 0;
        }

        int removed = 0;
        int index = 0;
        while (index < relationships.Relationships.Count)
        {
            XlsxRelationship relationship = relationships.Relationships[index];
            if (IsUnsafeRelationship(relationship, options))
            {
                relationships.Relationships.RemoveAt(index);
                removed = removed + 1;
                continue;
            }

            index = index + 1;
        }

        return removed;
    }

    internal static bool IsUnsafeRelationship(XlsxRelationship relationship, XlsxWorkbookSanitizeOptions options)
    {
        if (relationship == null || relationship.Type == null)
        {
            return false;
        }

        if (options.RemoveMacros && relationship.Type == XlsxRelationshipTypes.VbaProject)
        {
            return true;
        }

        if (options.RemoveExternalLinks && (relationship.Type == XlsxRelationshipTypes.ExternalLink || relationship.Type == XlsxRelationshipTypes.ExternalLinkPath || relationship.IsExternal || relationship.TargetMode == "External"))
        {
            return true;
        }

        if (options.RemoveComments && (relationship.Type == XlsxRelationshipTypes.Comments || relationship.Type == XlsxRelationshipTypes.ThreadedComments || relationship.Type == XlsxRelationshipTypes.VmlDrawing))
        {
            return true;
        }

        if (options.RemoveCustomXmlParts && (relationship.Type == XlsxRelationshipTypes.CustomXml || relationship.Type == XlsxRelationshipTypes.CustomXmlProperties))
        {
            return true;
        }

        if (options.RemovePersonalMetadata && (relationship.Type == XlsxRelationshipTypes.CoreProperties || relationship.Type == XlsxRelationshipTypes.ExtendedProperties || relationship.Type == XlsxRelationshipTypes.CustomProperties))
        {
            return true;
        }

        if (options.RemoveOleObjects && (relationship.Type == XlsxRelationshipTypes.OleObject || relationship.Type == XlsxRelationshipTypes.EmbeddedPackage))
        {
            return true;
        }

        return false;
    }

    internal void RemoveUnknownPackageParts(XlsxWorkbookSanitizeResult result, XlsxWorkbookSanitizeOptions options)
    {
        if (UnknownParts == null || UnknownParts.Parts == null)
        {
            return;
        }

        int index = 0;
        while (index < UnknownParts.Parts.Count)
        {
            XlsxUnknownPart part = UnknownParts.Parts[index];
            if (part == null || !ShouldPreserveKnownPackagePart(part, options))
            {
                UnknownParts.Parts.RemoveAt(index);
                result.UnknownPartsRemoved = result.UnknownPartsRemoved + 1;
                continue;
            }

            index = index + 1;
        }
    }

    internal bool ShouldPreserveKnownPackagePart(XlsxUnknownPart part, XlsxWorkbookSanitizeOptions options)
    {
        if (part == null)
        {
            return false;
        }

        if (IsUnsafePackagePart(part, options))
        {
            return false;
        }

        if (IsKnownPreservableContentType(part.ContentType))
        {
            return true;
        }

        if (part.Path != null && part.Path.EndsWith(".rels", System.StringComparison.Ordinal))
        {
            return true;
        }

        return IsActiveWorkbookRelationshipTarget(part.Path);
    }

    internal static bool IsUnsafePackagePart(XlsxUnknownPart part, XlsxWorkbookSanitizeOptions options)
    {
        if (part == null)
        {
            return false;
        }

        string path = XlsxCellValueConverter.NormalizeText(part.Path);
        string contentType = XlsxCellValueConverter.NormalizeText(part.ContentType);
        if (options.RemoveMacros && (path == XlsxPackagePaths.VbaProject || contentType == XlsxContentTypes.VbaProject))
        {
            return true;
        }

        if (options.RemoveExternalLinks && (path.StartsWith(XlsxPackagePaths.ExternalLinksDirectory, System.StringComparison.Ordinal) || contentType == XlsxContentTypes.ExternalLink))
        {
            return true;
        }

        if (options.RemoveComments && (path.StartsWith(XlsxPackagePaths.CommentsDirectory, System.StringComparison.Ordinal) || path.StartsWith("/xl/threadedComments/", System.StringComparison.Ordinal) || contentType == XlsxContentTypes.Comments || contentType == XlsxContentTypes.ThreadedComments || contentType == XlsxContentTypes.VmlDrawing))
        {
            return true;
        }

        if (options.RemoveCustomXmlParts && (path.StartsWith(XlsxPackagePaths.CustomXmlDirectory, System.StringComparison.Ordinal) || contentType == XlsxContentTypes.CustomXmlProperties))
        {
            return true;
        }

        if (options.RemovePersonalMetadata && (path == XlsxPackagePaths.CoreProperties || path == XlsxPackagePaths.ExtendedProperties || path == XlsxPackagePaths.CustomProperties || contentType == XlsxContentTypes.CoreProperties || contentType == XlsxContentTypes.ExtendedProperties || contentType == XlsxContentTypes.CustomProperties))
        {
            return true;
        }

        if (options.RemoveOleObjects && (path.StartsWith(XlsxPackagePaths.EmbeddingsDirectory, System.StringComparison.Ordinal) || contentType == XlsxContentTypes.OleObject || contentType == XlsxContentTypes.EmbeddedSpreadsheetPackage))
        {
            return true;
        }

        return false;
    }

    internal bool IsActiveWorkbookRelationshipTarget(string? path)
    {
        if (path == null || path.Length == 0 || WorkbookRelationships == null || WorkbookRelationships.Relationships == null)
        {
            return false;
        }

        foreach (XlsxRelationship relationship in WorkbookRelationships.Relationships)
        {
            if (relationship == null)
            {
                continue;
            }

            XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, relationship.Target, relationship.IsExternal || relationship.TargetMode == "External");
            if (target.Validation.IsValid && !target.IsExternal && target.Path == path)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsKnownPreservableContentType(string? contentType)
    {
        if (contentType == XlsxContentTypes.Worksheet || contentType == XlsxContentTypes.Chartsheet || contentType == XlsxContentTypes.Styles || contentType == XlsxContentTypes.SharedStrings || contentType == XlsxContentTypes.Theme)
        {
            return true;
        }

        if (contentType == XlsxContentTypes.Drawing || contentType == XlsxContentTypes.Chart || contentType == XlsxContentTypes.Table || contentType == XlsxContentTypes.PivotCacheDefinition || contentType == XlsxContentTypes.PivotCacheRecords || contentType == XlsxContentTypes.PivotTable)
        {
            return true;
        }

        if (contentType == XlsxContentTypes.Slicer || contentType == XlsxContentTypes.SlicerCache || contentType == XlsxContentTypes.Timeline || contentType == XlsxContentTypes.TimelineCache || contentType == XlsxContentTypes.QueryTable)
        {
            return true;
        }

        return false;
    }

    internal static bool IsWorksheetLikePart(XlsxUnknownPart part)
    {
        if (part == null)
        {
            return false;
        }

        if (part.ContentType == XlsxContentTypes.Worksheet || part.ContentType == XlsxContentTypes.Chartsheet)
        {
            return true;
        }

        if (part.Path != null && (part.Path.StartsWith(XlsxPackagePaths.WorksheetsDirectory, System.StringComparison.Ordinal) || part.Path.StartsWith(XlsxPackagePaths.ChartsheetsDirectory, System.StringComparison.Ordinal)) && part.Path.EndsWith(".xml", System.StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    internal static int RemoveElementsByLocalName(XmlDocument document, string localName)
    {
        if (document == null || document.DocumentElement == null || localName == null || localName.Length == 0)
        {
            return 0;
        }

        return RemoveElementsByLocalName(document.DocumentElement, localName);
    }

    internal static int RemoveElementsByLocalName(XmlNode node, string localName)
    {
        if (node == null || node.ChildNodes == null)
        {
            return 0;
        }

        int removed = 0;
        int index = node.ChildNodes.Count - 1;
        while (index >= 0)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null && child.NodeType == XmlNodeType.Element && child.LocalName == localName)
            {
                node.RemoveChild(child);
                removed = removed + 1;
            }
            else if (child != null)
            {
                removed = removed + RemoveElementsByLocalName(child, localName);
            }

            index = index - 1;
        }

        return removed;
    }

    internal static void RemoveUnknownXmlEntriesByLocalName(List<string> unknownXml, string localName)
    {
        if (unknownXml == null || localName == null || localName.Length == 0)
        {
            return;
        }

        int index = unknownXml.Count - 1;
        while (index >= 0)
        {
            string xml = unknownXml[index];
            if (xml != null && GetXmlRootLocalName(xml) == localName)
            {
                unknownXml.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    internal static string GetXmlRootLocalName(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return "";
        }

        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        return reader.LocalName;
                    }

                    reader.Read();
                }
            }
        }

        return "";
    }

    internal int RemoveUnknownPartsByPrefixAndCount(string partPathPrefix)
    {
        if (UnknownParts == null || UnknownParts.Parts == null || partPathPrefix == null || partPathPrefix.Length == 0)
        {
            return 0;
        }

        int removed = 0;
        int index = 0;
        while (index < UnknownParts.Parts.Count)
        {
            XlsxUnknownPart part = UnknownParts.Parts[index];
            if (part != null && part.Path != null && part.Path.StartsWith(partPathPrefix, System.StringComparison.Ordinal))
            {
                UnknownParts.Parts.RemoveAt(index);
                removed = removed + 1;
                continue;
            }

            index = index + 1;
        }

        return removed;
    }

    internal static int CountRelationshipsByType(XlsxRelationshipsPart relationships, string relationshipType)
    {
        int count = 0;
        if (relationships == null || relationships.Relationships == null)
        {
            return count;
        }

        foreach (XlsxRelationship relationship in relationships.Relationships)
        {
            if (relationship != null && relationship.Type == relationshipType)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static int RemoveRelationshipsByType(XlsxRelationshipsPart relationships, string relationshipType)
    {
        int removed = 0;
        if (relationships == null || relationships.Relationships == null)
        {
            return removed;
        }

        int index = 0;
        while (index < relationships.Relationships.Count)
        {
            XlsxRelationship relationship = relationships.Relationships[index];
            if (relationship != null && relationship.Type == relationshipType)
            {
                relationships.Relationships.RemoveAt(index);
                removed = removed + 1;
                continue;
            }

            index = index + 1;
        }

        return removed;
    }

    internal static bool IsHiddenWorkbookSheet(XlsxWorkbookSheet sheet)
    {
        if (sheet == null || sheet.State == null)
        {
            return false;
        }

        return sheet.State == "hidden" || sheet.State == "veryHidden";
    }

    internal static bool HasCorePropertiesContent(XlsxCorePropertiesPart part)
    {
        if (part == null)
        {
            return false;
        }

        if (part.Creator.Length > 0 || part.LastModifiedBy.Length > 0 || part.Title.Length > 0 || part.Subject.Length > 0 || part.Keywords.Length > 0 || part.Description.Length > 0 || part.Identifier.Length > 0)
        {
            return true;
        }

        return part.HasCreated || part.HasModified;
    }

    internal static bool HasExtendedPropertiesContent(XlsxExtendedPropertiesPart part)
    {
        if (part == null)
        {
            return false;
        }

        if (part.Manager.Length > 0 || part.Company.Length > 0 || part.Template.Length > 0)
        {
            return true;
        }

        return part.HasDocSecurity || part.HasSharedDoc || part.HasHyperlinksChanged;
    }

    internal static bool HasCustomPropertiesContent(XlsxCustomPropertiesPart part)
    {
        if (part == null)
        {
            return false;
        }

        return part.Properties != null && part.Properties.Count > 0;
    }

    /// <summary>
    /// Performs the reorder sheet operation using the current model state.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <param name = "newIndex">The new index value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxValidationResult ReorderSheet(string name, int newIndex)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        int oldIndex = GetSheetIndex(name);
        if (oldIndex < 0)
        {
            result.AddError("workbook.sheet.notFound", "Workbook sheet was not found.");
            return result;
        }

        if (newIndex < 0 || newIndex >= WorkbookPart.Sheets.Count)
        {
            result.AddError("workbook.sheet.indexOutOfRange", "Workbook sheet target index is outside the sheet collection.");
            return result;
        }

        XlsxWorkbookSheet sheet = WorkbookPart.Sheets[oldIndex];
        WorkbookPart.Sheets.RemoveAt(oldIndex);
        WorkbookPart.Sheets.Insert(newIndex, sheet);
        RemapDefinedNamesForReorderedSheet(oldIndex, newIndex);
        RemapWorkbookViewsForReorderedSheet(oldIndex, newIndex);
        return result;
    }

    /// <summary>
    /// Performs the rename sheet operation using the current model state.
    /// </summary>
    /// <param name = "oldName">The old name value used by the operation.</param>
    /// <param name = "newName">The new name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxValidationResult RenameSheet(string oldName, string newName)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        XlsxWorkbookSheet? sheet = GetSheet(oldName);
        if (sheet == null)
        {
            result.AddError("workbook.sheet.notFound", "Workbook sheet was not found.");
            return result;
        }

        if (oldName == newName)
        {
            return result;
        }

        XlsxValidationResult nameValidation = ValidateNewSheetName(newName);
        CopyDiagnostics(result, nameValidation);
        if (!result.IsValid)
        {
            return result;
        }

        sheet.Name = newName;
        return result;
    }

    /// <summary>
    /// Performs the hide sheet operation using the current model state.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <param name = "state">The state value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxValidationResult HideSheet(string name, string state)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        XlsxWorkbookSheet? sheet = GetSheet(name);
        if (sheet == null)
        {
            result.AddError("workbook.sheet.notFound", "Workbook sheet was not found.");
            return result;
        }

        if (!IsValidSheetState(state))
        {
            result.AddError("workbook.sheet.stateInvalid", "Workbook sheet state must be visible, hidden, or veryHidden.");
            return result;
        }

        bool hide = state == "hidden" || state == "veryHidden";
        sheet.State = state;
        if (hide)
        {
            RemapWorkbookViewsForHiddenSheet(GetSheetIndex(name));
        }
        return result;
    }

    /// <summary>
    /// Checks sheet names and returns diagnostics rather than throwing for expected invalid input.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxValidationResult ValidateSheetNames()
    {
        XlsxValidationResult result = new XlsxValidationResult();
        Dictionary<string, bool> sheetNames = new Dictionary<string, bool>(System.StringComparer.OrdinalIgnoreCase);
        foreach (XlsxWorkbookSheet sheet in WorkbookPart.Sheets)
        {
            if (sheet.Name == null || sheet.Name.Length == 0)
            {
                result.AddError("workbook.sheet.nameMissing", "Workbook sheet must have a name.");
                continue;
            }

            if (!XlsxWorkbookPartWriter.IsValidSheetName(sheet.Name))
            {
                result.AddError("workbook.sheet.nameInvalid", "Workbook sheet name contains invalid characters or exceeds the length limit.");
                continue;
            }

            if (sheetNames.ContainsKey(sheet.Name))
            {
                result.AddError("workbook.sheet.nameDuplicate", "Workbook sheet names must be unique.");
                continue;
            }

            sheetNames.Add(sheet.Name, true);
        }

        return result;
    }

    internal void RemapDefinedNamesForReorderedSheet(int oldIndex, int newIndex)
    {
        if (WorkbookPart.DefinedNames == null)
        {
            return;
        }

        foreach (XlsxWorkbookDefinedName definedName in WorkbookPart.DefinedNames)
        {
            if (definedName != null && definedName.HasLocalSheetId)
            {
                definedName.LocalSheetId = RemapSheetIndexForReorder(definedName.LocalSheetId, oldIndex, newIndex);
            }
        }
    }

    internal void RemapWorkbookViewsForReorderedSheet(int oldIndex, int newIndex)
    {
        if (WorkbookPart.WorkbookViews == null)
        {
            return;
        }

        foreach (XlsxWorkbookView view in WorkbookPart.WorkbookViews)
        {
            if (view == null)
            {
                continue;
            }
            if (view.HasActiveTab)
            {
                view.ActiveTab = RemapWorkbookViewIndexForReorderedSheet(view.ActiveTab, oldIndex, newIndex);
            }
            if (view.HasFirstSheet)
            {
                view.FirstSheet = RemapWorkbookViewIndexForReorderedSheet(view.FirstSheet, oldIndex, newIndex);
            }
        }
    }

    internal static int RemapSheetIndexForReorder(int value, int oldIndex, int newIndex)
    {
        if (value == oldIndex)
        {
            return newIndex;
        }
        if (oldIndex < newIndex && value > oldIndex && value <= newIndex)
        {
            return value - 1;
        }
        if (newIndex < oldIndex && value >= newIndex && value < oldIndex)
        {
            return value + 1;
        }
        return value;
    }

    internal static uint RemapWorkbookViewIndexForReorderedSheet(uint value, int oldIndex, int newIndex)
    {
        if (value > int.MaxValue)
        {
            return value;
        }
        return (uint)RemapSheetIndexForReorder((int)value, oldIndex, newIndex);
    }

    internal void RemapWorkbookViewsForHiddenSheet(int hiddenIndex)
    {
        if (WorkbookPart.WorkbookViews == null)
        {
            return;
        }

        int firstVisible = GetFirstVisibleWorkbookSheetIndex();
        if (firstVisible < 0)
        {
            return;
        }
        foreach (XlsxWorkbookView view in WorkbookPart.WorkbookViews)
        {
            if (view == null)
            {
                continue;
            }
            if (view.HasActiveTab && view.ActiveTab == (uint)hiddenIndex)
            {
                view.ActiveTab = (uint)firstVisible;
            }
            if (view.HasFirstSheet && view.FirstSheet == (uint)hiddenIndex)
            {
                view.FirstSheet = (uint)firstVisible;
            }
        }
    }

    internal int GetFirstVisibleWorkbookSheetIndex()
    {
        int index = 0;
        while (index < WorkbookPart.Sheets.Count)
        {
            XlsxWorkbookSheet sheet = WorkbookPart.Sheets[index];
            if (sheet != null && !IsHiddenWorkbookSheet(sheet))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal XlsxValidationResult ValidateNewSheetName(string name)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (name == null || name.Length == 0)
        {
            result.AddError("workbook.sheet.nameMissing", "Workbook sheet must have a name.");
            return result;
        }

        if (!XlsxWorkbookPartWriter.IsValidSheetName(name))
        {
            result.AddError("workbook.sheet.nameInvalid", "Workbook sheet name contains invalid characters or exceeds the length limit.");
            return result;
        }

        foreach (XlsxWorkbookSheet sheet in WorkbookPart.Sheets)
        {
            if (string.Equals(sheet.Name, name, System.StringComparison.OrdinalIgnoreCase))
            {
                result.AddError("workbook.sheet.nameDuplicate", "Workbook sheet names must be unique.");
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// Performs the preserve cached formula values operation using the current model state.
    /// </summary>
    /// <param name = "worksheets">The worksheets value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookRecalculationResult PreserveCachedFormulaValues(List<XlsxWorksheetPart> worksheets)
    {
        return RecalculateWorkbook(XlsxWorkbookRecalculationMode.PreserveCachedValues, worksheets);
    }

    /// <summary>
    /// Performs the recalculate supported formulas operation using the current model state.
    /// </summary>
    /// <param name = "worksheets">The worksheets value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookRecalculationResult RecalculateSupportedFormulas(List<XlsxWorksheetPart> worksheets)
    {
        return RecalculateWorkbook(XlsxWorkbookRecalculationMode.RecalculateSupportedFormulas, worksheets);
    }

    /// <summary>
    /// Performs the recalculate changed formulas operation using the current model state.
    /// </summary>
    /// <param name = "worksheets">The worksheets value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookRecalculationResult RecalculateChangedFormulas(List<XlsxWorksheetPart> worksheets)
    {
        return RecalculateWorkbook(XlsxWorkbookRecalculationMode.RecalculateChangedFormulas, worksheets);
    }

    /// <summary>
    /// Performs the mark for excel recalculation operation using the current model state.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookRecalculationResult MarkForExcelRecalculation()
    {
        return RecalculateWorkbook(XlsxWorkbookRecalculationMode.MarkForExcelRecalculation, new List<XlsxWorksheetPart>());
    }

    internal XlsxWorkbookRecalculationResult RecalculateWorkbook(XlsxWorkbookRecalculationMode mode, List<XlsxWorksheetPart> worksheets)
    {
        XlsxWorkbookRecalculationResult result = new XlsxWorkbookRecalculationResult();
        if (mode == XlsxWorkbookRecalculationMode.MarkForExcelRecalculation)
        {
            MarkWorkbookForExcelRecalculation(result);
            return result;
        }

        if (worksheets == null)
        {
            result.AddError("workbookRecalculation.worksheetsNull", "Workbook recalculation requires a worksheet part list.");
            return result;
        }

        if (mode == XlsxWorkbookRecalculationMode.PreserveCachedValues)
        {
            ConfigureWorkbookCalculationProperties("manual", false, false);
            result.PreservedFormulaCount = CountWorkbookFormulaCells(worksheets);
            return result;
        }

        ConfigureWorkbookCalculationProperties("auto", false, false);
        foreach (XlsxWorksheetPart worksheet in worksheets)
        {
            RecalculateWorksheetFormulas(result, worksheet, mode);
        }

        return result;
    }

    internal void RecalculateWorksheetFormulas(XlsxWorkbookRecalculationResult result, XlsxWorksheetPart worksheet, XlsxWorkbookRecalculationMode mode)
    {
        if (worksheet == null || worksheet.Rows == null)
        {
            result.AddError("workbookRecalculation.worksheetInvalid", "Workbook recalculation worksheet parts must not be null and must contain rows.");
            return;
        }

        List<XlsxWorksheetCell> formulaCells = GetWorksheetFormulaCells(worksheet);
        foreach (XlsxWorksheetCell cell in formulaCells)
        {
            if (ShouldRecalculateFormulaCell(cell, mode))
            {
                XlsxFormulaCalculationResult calculation = worksheet.CalculateCellFormulaWithSpill(cell.Reference);
                CopyFormulaCalculationDiagnostics(result, calculation);
                if (calculation.IsValid)
                {
                    result.RecalculatedFormulaCount = result.RecalculatedFormulaCount + calculation.CalculatedFormulaCount;
                    result.SpillCellCount = result.SpillCellCount + calculation.CalculatedSpillCellCount;
                    if (mode == XlsxWorkbookRecalculationMode.RecalculateChangedFormulas)
                    {
                        result.ChangedFormulaCount = result.ChangedFormulaCount + 1;
                    }
                }
            }
            else
            {
                result.PreservedFormulaCount = result.PreservedFormulaCount + 1;
            }
        }
    }

    internal static List<XlsxWorksheetCell> GetWorksheetFormulaCells(XlsxWorksheetPart worksheet)
    {
        List<XlsxWorksheetCell> cells = new List<XlsxWorksheetCell>();
        foreach (XlsxWorksheetRow row in worksheet.Rows)
        {
            if (row != null && row.Cells != null)
            {
                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    if (XlsxWorksheetPart.IsCalculableFormulaCell(cell))
                    {
                        cells.Add(cell);
                    }
                }
            }
        }

        return cells;
    }

    internal static bool ShouldRecalculateFormulaCell(XlsxWorksheetCell cell, XlsxWorkbookRecalculationMode mode)
    {
        if (mode == XlsxWorkbookRecalculationMode.RecalculateSupportedFormulas)
        {
            return true;
        }

        if (mode == XlsxWorkbookRecalculationMode.RecalculateChangedFormulas)
        {
            return FormulaCellNeedsRecalculation(cell);
        }

        return false;
    }

    internal static bool FormulaCellNeedsRecalculation(XlsxWorksheetCell cell)
    {
        if (!XlsxWorksheetPart.IsCalculableFormulaCell(cell))
        {
            return false;
        }

        if (cell.FormulaInfo == null || cell.FormulaInfo.CachedValue == null || cell.FormulaInfo.CachedValue.Length == 0)
        {
            return true;
        }

        string formulaText = XlsxWorksheetPart.GetFormulaText(cell);
        if (cell.Formula != null && cell.Formula.Length > 0 && formulaText != cell.Formula)
        {
            return true;
        }

        return false;
    }

    internal int CountWorkbookFormulaCells(List<XlsxWorksheetPart> worksheets)
    {
        int count = 0;
        foreach (XlsxWorksheetPart worksheet in worksheets)
        {
            if (worksheet != null && worksheet.Rows != null)
            {
                foreach (XlsxWorksheetRow row in worksheet.Rows)
                {
                    if (row != null && row.Cells != null)
                    {
                        foreach (XlsxWorksheetCell cell in row.Cells)
                        {
                            if (XlsxWorksheetPart.IsCalculableFormulaCell(cell))
                            {
                                count = count + 1;
                            }
                        }
                    }
                }
            }
        }

        return count;
    }

    internal void MarkWorkbookForExcelRecalculation(XlsxWorkbookRecalculationResult result)
    {
        ConfigureWorkbookCalculationProperties("auto", true, true);
        result.MarkedForExcelRecalculation = true;
    }

    internal void ConfigureWorkbookCalculationProperties(string calcMode, bool fullCalcOnLoad, bool forceFullCalc)
    {
        if (WorkbookPart == null)
        {
            WorkbookPart = new XlsxWorkbookPart();
        }

        if (WorkbookPart.CalculationProperties == null)
        {
            WorkbookPart.CalculationProperties = new XlsxWorkbookCalculationProperties();
        }

        WorkbookPart.HasCalculationProperties = true;
        WorkbookPart.CalculationProperties.CalcMode = XlsxCellValueConverter.NormalizeText(calcMode);
        WorkbookPart.CalculationProperties.FullCalcOnLoad = fullCalcOnLoad;
        WorkbookPart.CalculationProperties.HasFullCalcOnLoad = true;
        WorkbookPart.CalculationProperties.ForceFullCalc = forceFullCalc;
        WorkbookPart.CalculationProperties.HasForceFullCalc = true;
    }

    internal static void CopyFormulaCalculationDiagnostics(XlsxWorkbookRecalculationResult result, XlsxFormulaCalculationResult calculation)
    {
        foreach (XlsxDiagnostic diagnostic in calculation.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void PopulatePerformanceCounters(XlsxPerformanceCounters counters, XlsxWorkbook workbook)
    {
        if (counters == null || workbook == null)
        {
            return;
        }

        if (workbook.WorkbookPart != null && workbook.WorkbookPart.Sheets != null)
        {
            counters.WorksheetCount = workbook.WorkbookPart.Sheets.Count;
        }
        else if (workbook.Worksheets != null)
        {
            counters.WorksheetCount = workbook.Worksheets.Count;
        }

        counters.RelationshipCount = CountRelationshipItems(workbook.PackageRelationships) + CountRelationshipItems(workbook.WorkbookRelationships);
        if (workbook.UnknownParts != null && workbook.UnknownParts.Parts != null)
        {
            counters.UnknownPartCount = workbook.UnknownParts.Parts.Count;
        }

        PopulateWorksheetPerformanceCounters(counters, workbook.Worksheets);
        PopulateStylePerformanceCounters(counters, workbook.Styles);
    }

    internal static void FinishPerformanceCounters(XlsxPerformanceCounters? counters, long startTicks)
    {
        if (counters != null)
        {
            counters.Finish(startTicks);
        }
    }

    internal static void PopulateWorksheetPerformanceCounters(XlsxPerformanceCounters counters, List<XlsxWorksheetPart>? worksheets)
    {
        if (counters == null || worksheets == null)
        {
            return;
        }

        foreach (XlsxWorksheetPart worksheet in worksheets)
        {
            if (worksheet == null || worksheet.Rows == null)
            {
                continue;
            }

            counters.RowCount = counters.RowCount + worksheet.Rows.Count;
            foreach (XlsxWorksheetRow row in worksheet.Rows)
            {
                if (row == null || row.Cells == null)
                {
                    continue;
                }

                counters.CellCount = counters.CellCount + row.Cells.Count;
                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    if (cell != null && (cell.HasFormula || (cell.Formula != null && cell.Formula.Length > 0)))
                    {
                        counters.FormulaCount = counters.FormulaCount + 1;
                    }
                }
            }
        }
    }

    internal static void PopulateStylePerformanceCounters(XlsxPerformanceCounters counters, XlsxStylesPart styles)
    {
        if (counters == null || styles == null)
        {
            return;
        }

        counters.StyleCount = CountNumberFormats(styles.NumberFormats) + CountRawStyles(styles.Fonts) + CountRawStyles(styles.Fills) + CountRawStyles(styles.Borders) + CountCellFormats(styles.CellStyleFormats) + CountCellFormats(styles.CellFormats) + CountRawStyles(styles.DifferentialFormats);
    }

    internal static int CountRelationshipItems(XlsxRelationshipsPart relationships)
    {
        if (relationships == null || relationships.Relationships == null)
        {
            return 0;
        }

        return relationships.Relationships.Count;
    }

    internal static int CountNumberFormats(List<XlsxNumberFormatDefinition> items)
    {
        if (items == null)
        {
            return 0;
        }

        return items.Count;
    }

    internal static int CountRawStyles(List<XlsxRawStyleElement> items)
    {
        if (items == null)
        {
            return 0;
        }

        return items.Count;
    }

    internal static int CountCellFormats(List<XlsxCellFormatDefinition> items)
    {
        if (items == null)
        {
            return 0;
        }

        return items.Count;
    }

    /// <summary>
    /// Opens and validates an OOXML workbook package from a file path.
    /// </summary>
    /// <param name = "filePath">The file-system path to read or write.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxWorkbookOpenResult OpenFile(string filePath)
    {
        return OpenFile(filePath, new XlsxReaderOptions());
    }

    /// <summary>
    /// Opens and validates an OOXML workbook package from a file path.
    /// </summary>
    /// <param name = "filePath">The file-system path to read or write.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxWorkbookOpenResult OpenFile(string filePath, XlsxReaderOptions options)
    {
        byte[] bytes = File.ReadAllBytes(filePath);
        return OpenBytes(bytes, options);
    }

    /// <summary>
    /// Opens and validates an OOXML workbook package from an in-memory byte array.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxWorkbookOpenResult OpenBytes(byte[] bytes)
    {
        return OpenBytes(bytes, new XlsxReaderOptions());
    }

    /// <summary>
    /// Opens and validates an OOXML workbook package from an in-memory byte array.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxWorkbookOpenResult OpenBytes(byte[] bytes, XlsxReaderOptions options)
    {
        if (bytes == null || bytes.Length == 0)
        {
            XlsxWorkbookOpenResult result = new XlsxWorkbookOpenResult();
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.bytesEmpty", "Workbook bytes must not be empty."));
            return result;
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return OpenStream(stream, options);
        }
    }

    /// <summary>
    /// Opens and validates an OOXML workbook package from read-only memory.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxWorkbookOpenResult OpenMemory(ReadOnlyMemory<byte> bytes)
    {
        return OpenMemoryWithOptions(bytes, new XlsxReaderOptions());
    }

    /// <summary>
    /// Opens and validates an OOXML workbook package from read-only memory.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxWorkbookOpenResult OpenMemoryWithOptions(ReadOnlyMemory<byte> bytes, XlsxReaderOptions options)
    {
        return OpenBytes(bytes.ToArray(), options);
    }

    /// <summary>
    /// Opens and validates an OOXML workbook package from a read-only byte span.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxWorkbookOpenResult OpenSpan(ReadOnlySpan<byte> bytes)
    {
        return OpenSpanWithOptions(bytes, new XlsxReaderOptions());
    }

    /// <summary>
    /// Opens and validates an OOXML workbook package from a read-only byte span.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxWorkbookOpenResult OpenSpanWithOptions(ReadOnlySpan<byte> bytes, XlsxReaderOptions options)
    {
        return OpenBytes(bytes.ToArray(), options);
    }

    /// <summary>
    /// Opens and validates an OOXML workbook package from a readable stream.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxWorkbookOpenResult OpenStream(Stream stream)
    {
        return OpenStream(stream, new XlsxReaderOptions());
    }

    /// <summary>
    /// Opens and validates an OOXML workbook package from a readable stream.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxWorkbookOpenResult OpenStream(Stream stream, XlsxReaderOptions options)
    {
        XlsxReaderOptions readerOptions = options;
        if (readerOptions == null)
        {
            readerOptions = new XlsxReaderOptions();
        }

        XlsxWorkbookOpenResult result = new XlsxWorkbookOpenResult();
        XlsxPerformanceCounters? statistics = null;
        long statisticsStart = 0;
        if (readerOptions.CollectStatistics)
        {
            statistics = XlsxPerformanceCounters.Start(readerOptions.Statistics, "workbook.open");
            readerOptions.Statistics = statistics;
            result.Statistics = statistics;
            statisticsStart = System.Environment.TickCount64;
            if (stream != null && stream.CanSeek)
            {
                statistics.InputBytes = stream.Length - stream.Position;
            }
        }

        if (stream == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.streamNull", "Workbook stream must not be null."));
            FinishPerformanceCounters(statistics, statisticsStart);
            return result;
        }

        if (!stream.CanRead)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.streamNotReadable", "Workbook input stream must be readable."));
            FinishPerformanceCounters(statistics, statisticsStart);
            return result;
        }

        if (XlsxEncryptionDetector.IsEncryptedPackageStream(stream))
        {
            if (readerOptions.Password != null && readerOptions.Password.Length > 0)
            {
                byte[] encryptedBytes;
                using (MemoryStream encryptedBuffer = new MemoryStream())
                {
                    stream.CopyTo(encryptedBuffer);
                    encryptedBytes = encryptedBuffer.ToArray();
                }
                char[] passwordCharacters = readerOptions.Password.ToCharArray();
                XlsxEncryptedPackageResult decrypted = XlsxEncryptedPackageCryptography.Decrypt(encryptedBytes, passwordCharacters);
                Array.Clear(passwordCharacters, 0, passwordCharacters.Length);
                if (!decrypted.IsValid)
                {
                    CopyDiagnostics(result, decrypted.Validation);
                    FinishPerformanceCounters(statistics, statisticsStart);
                    return result;
                }
                FinishPerformanceCounters(statistics, statisticsStart);
                return OpenBytes(decrypted.Bytes, readerOptions);
            }
            else
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.encryptedPackageUnsupported", "Encrypted OOXML workbooks are detected, but decryption is not enabled."));
            }

            FinishPerformanceCounters(statistics, statisticsStart);
            return result;
        }

        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadStream(stream, readerOptions);
        CopyDiagnostics(result, package.Validation);
        XlsxContentTypesPart contentTypes = ReadContentTypes(package, result);
        XlsxWorkbook workbook = new XlsxWorkbook();
        workbook.ContentTypes = contentTypes;
        workbook.PackageParts = EnumeratePackageParts(package, contentTypes);
        workbook.PackageVariant = DetectPackageVariant(contentTypes);
        workbook.WorkbookPart = ReadWorkbookPart(package, result);
        workbook.Conformance = DetectPackageConformance(workbook.WorkbookPart);
        workbook.PackageRelationships = ReadPackageRelationships(package, readerOptions, result);
        workbook.WorkbookRelationships = ReadWorkbookRelationships(package, readerOptions, result);
        workbook.RelationshipParts = EnumerateRelationshipParts(package, workbook.PackageRelationships, workbook.WorkbookRelationships, readerOptions, result);
        workbook.PreviewMetadata = ReadPackagePreviewMetadata(package, contentTypes, workbook.PackageRelationships);
        XlsxWorkbookRelationshipResolutionResult relationshipResolution = new XlsxWorkbookRelationshipResolver().Resolve(workbook.WorkbookPart, workbook.WorkbookRelationships);
        CopyDiagnostics(result, relationshipResolution.Validation);
        if (IsMacroEnabledPackageVariant(workbook.PackageVariant) && !readerOptions.AllowMacroEnabledPackages)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Warning, "workbook.open.macroEnabledPackage", "Workbook package is macro-enabled."));
        }

        ReadDocumentProperties(workbook, package, result);
        if (!readerOptions.MetadataOnly && relationshipResolution.HasStyles)
        {
            workbook.Styles = ReadStylesPart(package, relationshipResolution, result);
            workbook.HasStyles = true;
        }

        if (!readerOptions.MetadataOnly && relationshipResolution.HasTheme)
        {
            workbook.Theme = ReadThemePart(package, relationshipResolution, result);
            workbook.HasTheme = true;
        }

        if (!readerOptions.MetadataOnly && !readerOptions.StylesOnly && relationshipResolution.HasSharedStrings)
        {
            workbook.SharedStrings = ReadSharedStringsPart(package, relationshipResolution, readerOptions, result);
            workbook.HasSharedStrings = workbook.SharedStrings != null && workbook.SharedStrings.IsValid;
        }

        if (ShouldLoadWorksheetParts(readerOptions))
        {
            workbook.Worksheets = ReadWorksheetParts(package, relationshipResolution, readerOptions, result);
        }

        if (!readerOptions.MetadataOnly && !readerOptions.StylesOnly)
        {
            workbook.CalculationChain = ReadCalculationChainPart(package, contentTypes, result);
            workbook.HasCalculationChain = HasPackagePartWithContentType(package, contentTypes, XlsxPackagePaths.CalculationChain, XlsxContentTypes.CalculationChain);
            workbook.Chartsheets = ReadChartsheetParts(package, relationshipResolution, result);
            workbook.Macrosheets = ReadSheetMetadata(relationshipResolution.Macrosheets);
            workbook.Dialogsheets = ReadSheetMetadata(relationshipResolution.Dialogsheets);
            workbook.Drawings = ReadDrawingParts(package, contentTypes, result);
            workbook.Images = ReadImageParts(package, contentTypes, result);
            workbook.Comments = ReadCommentsParts(package, contentTypes, result);
            workbook.ThreadedComments = ReadThreadedCommentsParts(package, contentTypes, result);
            workbook.Charts = ReadChartParts(package, contentTypes, result);
            workbook.PivotCacheDefinitions = ReadPivotCacheDefinitionParts(package, contentTypes, result);
            workbook.PivotTableDefinitions = ReadPivotTableDefinitionParts(package, contentTypes, readerOptions, result);
            workbook.SlicerCaches = ReadSlicerTimelineParts(package, contentTypes, result, XlsxContentTypes.SlicerCache, XlsxPackagePaths.SlicerCachesDirectory, "slicerCacheDefinition");
            workbook.Slicers = ReadSlicerTimelineParts(package, contentTypes, result, XlsxContentTypes.Slicer, XlsxPackagePaths.SlicersDirectory, "slicers");
            workbook.TimelineCaches = ReadSlicerTimelineParts(package, contentTypes, result, XlsxContentTypes.TimelineCache, XlsxPackagePaths.TimelineCachesDirectory, "timelineCacheDefinition");
            workbook.Timelines = ReadSlicerTimelineParts(package, contentTypes, result, XlsxContentTypes.Timeline, XlsxPackagePaths.TimelinesDirectory, "timelines");
            workbook.ExternalLinks = ReadExternalLinkParts(package, contentTypes, readerOptions, result);
            AddExternalLinkRelationshipParts(workbook.ExternalLinks, package, relationshipResolution, readerOptions, result);
            workbook.Connections = ReadWorkbookConnectionsPart(package, contentTypes, result);
            workbook.HasConnections = workbook.Connections != null && workbook.Connections.RawXml != null && workbook.Connections.RawXml.Length > 0;
            workbook.QueryTables = ReadQueryTableParts(package, contentTypes, result);
            workbook.CustomXmlParts = ReadCustomXmlParts(package, contentTypes, result);
            workbook.VbaProject = ReadVbaProjectPart(package, relationshipResolution, result);
            workbook.HasVbaProject = workbook.VbaProject != null && workbook.VbaProject.Data != null && workbook.VbaProject.Data.Length > 0;
            workbook.EmbeddedObjects = ReadEmbeddedObjectParts(package, contentTypes, result);
            workbook.InCellPictureMetadataParts = ReadInCellPictureMetadataParts(package, contentTypes, result);
            workbook.DigitalSignatures = ReadDigitalSignatureParts(package, contentTypes, workbook.PackageRelationships, readerOptions, result);
            workbook.HasDigitalSignatures = workbook.DigitalSignatures != null && workbook.DigitalSignatures.Count > 0;
        }

        if (readerOptions.PreserveUnknownParts && !readerOptions.MetadataOnly && !readerOptions.StylesOnly)
        {
            workbook.UnknownParts.LoadFromPackage(package, contentTypes);
            CopyDiagnostics(result, workbook.UnknownParts.Validation);
        }

        XlsxSecurityPolicyResult securityResult = new XlsxWorkbookSecurityService().Apply(workbook, readerOptions.SecurityOptions);
        CopyDiagnostics(result, securityResult.Validation);

        result.Workbook = workbook;
        if (statistics != null)
        {
            statistics.PackageEntryCount = package.EntryCount;
            statistics.TotalCompressedBytes = package.TotalCompressedBytes;
            statistics.TotalUncompressedBytes = package.TotalUncompressedBytes;
            PopulatePerformanceCounters(statistics, workbook);
            FinishPerformanceCounters(statistics, statisticsStart);
        }

        result.IsValid = result.Validation.IsValid;
        return result;
    }

    /// <summary>
    /// Validates the workbook, composes its package parts, and writes the package to a file.
    /// </summary>
    /// <param name = "filePath">The file-system path to read or write.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookSaveResult SaveFile(string filePath)
    {
        return SaveFileWithOptions(filePath, new XlsxWriterOptions());
    }

    /// <summary>
    /// Validates the workbook, composes its package parts, and writes the package to a file.
    /// </summary>
    /// <param name = "filePath">The file-system path to read or write.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookSaveResult SaveFileWithOptions(string filePath, XlsxWriterOptions options)
    {
        XlsxWorkbookSaveResult result = SaveBytesWithOptions(options);
        if (result.IsValid)
        {
            File.WriteAllBytes(filePath, result.Bytes);
        }

        return result;
    }

    /// <summary>
    /// Validates the workbook, composes its package parts, and returns a complete OOXML package as bytes.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookSaveResult SaveBytes()
    {
        return SaveBytesWithOptions(new XlsxWriterOptions());
    }

    /// <summary>
    /// Validates the workbook, composes its package parts, and returns a complete OOXML package as bytes.
    /// </summary>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookSaveResult SaveBytesWithOptions(XlsxWriterOptions options)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxWorkbookSaveResult result = SaveStreamWithOptions(stream, options);
            if (result.IsValid)
            {
                result.Bytes = stream.ToArray();
                if (result.Statistics != null && result.Statistics.Enabled)
                {
                    result.Statistics.OutputBytes = result.Bytes.Length;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Validates the workbook, composes its package parts, and writes the package to a caller-provided stream.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookSaveResult SaveStream(Stream stream)
    {
        return SaveStreamWithOptions(stream, new XlsxWriterOptions());
    }

    /// <summary>
    /// Validates the workbook, composes its package parts, and writes the package to a caller-provided stream.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookSaveResult SaveStreamWithOptions(Stream stream, XlsxWriterOptions options)
    {
        XlsxWorkbookSaveResult result = new XlsxWorkbookSaveResult();
        XlsxWriterOptions writerOptions = options;
        if (writerOptions == null)
        {
            writerOptions = new XlsxWriterOptions();
        }

        XlsxPerformanceCounters? statistics = null;
        long statisticsStart = 0;
        if (writerOptions.CollectStatistics)
        {
            statistics = XlsxPerformanceCounters.Start(writerOptions.Statistics, "workbook.save");
            writerOptions.Statistics = statistics;
            result.Statistics = statistics;
            statisticsStart = System.Environment.TickCount64;
        }

        if (stream == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.save.streamNull", "Workbook output stream must not be null."));
            FinishPerformanceCounters(statistics, statisticsStart);
            return result;
        }

        if (!stream.CanWrite)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.save.streamNotWritable", "Workbook output stream must be writable."));
            FinishPerformanceCounters(statistics, statisticsStart);
            return result;
        }

        List<XlsxPackageEntry> entries = BuildPackageEntriesWithOptions(result, writerOptions);
        if (statistics != null)
        {
            statistics.PackageEntryCount = entries.Count;
            PopulatePerformanceCounters(statistics, this);
        }

        if (!result.IsValid)
        {
            FinishPerformanceCounters(statistics, statisticsStart);
            return result;
        }

        XlsxZipPackageWriteResult writeResult = new XlsxZipPackageWriter().WriteStream(stream, entries, writerOptions);
        CopyDiagnostics(result, writeResult.Validation);
        if (statistics != null)
        {
            statistics.OutputBytes = writeResult.TotalUncompressedBytes;
            statistics.TotalUncompressedBytes = writeResult.TotalUncompressedBytes;
            if (stream.CanSeek)
            {
                statistics.OutputBytes = stream.Length;
            }

            FinishPerformanceCounters(statistics, statisticsStart);
        }

        result.IsValid = result.Validation.IsValid;
        return result;
    }

    internal List<XlsxPackageEntry> BuildPackageEntries(XlsxWorkbookSaveResult result)
    {
        return BuildPackageEntriesWithOptions(result, new XlsxWriterOptions());
    }

    internal List<XlsxPackageEntry> BuildPackageEntriesWithOptions(XlsxWorkbookSaveResult result, XlsxWriterOptions options)
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        XlsxPackageVariant packageVariant = ResolveSavePackageVariant(options);
        XlsxContentTypesPart contentTypes = BuildContentTypesPartForVariant(packageVariant);
        XlsxRelationshipsPart packageRelationships = PackageRelationships;
        if (packageRelationships == null || packageRelationships.Relationships.Count == 0)
        {
            packageRelationships = CreatePackageRelationships();
        }

        XlsxRelationshipsPart workbookRelationships = WorkbookRelationships;
        if (workbookRelationships == null || workbookRelationships.Relationships.Count == 0)
        {
            workbookRelationships = CreateWorkbookRelationships();
        }

        bool writeStyles = HasStyles || HasStylesContent();
        if (writeStyles)
        {
            AddPackageRelationshipIfMissing(workbookRelationships, GetNextRelationshipId(workbookRelationships), XlsxRelationshipTypes.Styles, "styles.xml");
        }

        bool writeTheme = HasTheme || HasThemeContent();
        if (writeTheme)
        {
            AddPackageRelationshipIfMissing(workbookRelationships, GetNextRelationshipId(workbookRelationships), XlsxRelationshipTypes.Theme, "theme/theme1.xml");
        }

        bool writeSharedStrings = HasSharedStrings || HasSharedStringsContent();
        if (writeSharedStrings)
        {
            AddPackageRelationshipIfMissing(workbookRelationships, GetNextRelationshipId(workbookRelationships), XlsxRelationshipTypes.SharedStrings, GetWorkbookRelativeTarget(SharedStrings.PartPath));
        }
        bool writeCalculationChain = HasCalculationChain || HasCalculationChainContent();
        if (writeCalculationChain)
        {
            AddPackageRelationshipIfMissing(workbookRelationships, GetNextRelationshipId(workbookRelationships), XlsxRelationshipTypes.CalculationChain, GetWorkbookRelativeTarget(CalculationChain.PartPath));
        }
        bool writeConnections = HasConnections || HasConnectionsContent();
        if (writeConnections)
        {
            AddPackageRelationshipIfMissing(workbookRelationships, GetNextRelationshipId(workbookRelationships), XlsxRelationshipTypes.Connections, GetWorkbookRelativeTarget(Connections.PartPath));
        }

        AddContentTypesForWorkbookRelationships(contentTypes, workbookRelationships);
        if (HasCoreProperties || HasCorePropertiesContent(CoreProperties))
        {
            AddCorePropertiesParts(entries, contentTypes, packageRelationships, result);
        }

        if (HasExtendedProperties || HasExtendedPropertiesContent(ExtendedProperties))
        {
            AddExtendedPropertiesParts(entries, contentTypes, packageRelationships, result);
        }

        if (HasCustomProperties || HasCustomPropertiesContent(CustomProperties))
        {
            AddCustomPropertiesParts(entries, contentTypes, packageRelationships, result);
        }

        if (UnknownParts != null)
        {
            UnknownParts.AddContentTypesToPart(contentTypes);
        }

        AddWorkbookEntry(entries, result);
        AddRelationshipsEntry(entries, packageRelationships, result);
        AddWorkbookRelationshipsEntry(entries, workbookRelationships, result);
        if (writeStyles)
        {
            AddStylesParts(entries, contentTypes, workbookRelationships, result);
        }

        if (writeTheme)
        {
            AddThemeParts(entries, contentTypes, workbookRelationships, result);
        }

        if (writeSharedStrings)
        {
            AddSharedStringsPart(entries, contentTypes, result);
        }
        if (writeCalculationChain)
        {
            AddCalculationChainPart(entries, contentTypes, result);
        }
        if (writeConnections)
        {
            AddConnectionsPart(entries, contentTypes, result);
        }

        AddWorksheetEntries(entries, workbookRelationships);
        AddChartsheetEntries(entries, workbookRelationships, result);
        AddAggregatePartEntries(entries, contentTypes, result);
        if (UnknownParts != null)
        {
            UnknownParts.AddToEntries(entries);
            CopyDiagnostics(result, UnknownParts.Validation);
        }

        AddContentTypesEntry(entries, contentTypes, result);
        return entries;
    }

    internal bool HasStylesContent()
    {
        if (Styles == null)
        {
            return false;
        }

        if (Styles.NumberFormats.Count > 0 || Styles.Fonts.Count > 0 || Styles.Fills.Count > 0 || Styles.Borders.Count > 0)
        {
            return true;
        }

        if (Styles.CellStyleFormats.Count > 0 || Styles.CellFormats.Count > 0 || Styles.DifferentialFormats.Count > 0)
        {
            return true;
        }

        if (Styles.HasTableStyles || Styles.HasColors || Styles.HasExtensionList)
        {
            return true;
        }

        if (Styles.UnknownRootAttributes.Count > 0 || Styles.UnknownXml.Count > 0)
        {
            return true;
        }

        return false;
    }

    internal bool HasThemeContent()
    {
        if (Theme == null)
        {
            return false;
        }

        if (Theme.HasColorScheme || Theme.HasFontScheme || Theme.HasFormatScheme)
        {
            return true;
        }

        if (Theme.RawXml != null && Theme.RawXml.Length > 0)
        {
            return true;
        }

        if (Theme.UnknownRootAttributes.Count > 0 || Theme.UnknownXml.Count > 0)
        {
            return true;
        }

        return false;
    }

    internal bool HasSharedStringsContent()
    {
        return SharedStrings != null && SharedStrings.Items != null && (SharedStrings.Items.Count > 0 || SharedStrings.UnknownXml.Count > 0 || SharedStrings.UnknownRootAttributes.Count > 0);
    }

    internal bool HasCalculationChainContent()
    {
        return CalculationChain != null && CalculationChain.Cells != null && (CalculationChain.Cells.Count > 0 || CalculationChain.UnknownXml.Count > 0 || CalculationChain.UnknownRootAttributes.Count > 0);
    }

    internal bool HasConnectionsContent()
    {
        return Connections != null && ((Connections.RawXml != null && Connections.RawXml.Length > 0) || (Connections.Connections != null && Connections.Connections.Count > 0));
    }

    internal static string GetWorkbookRelativeTarget(string path)
    {
        if (path == null)
        {
            return "";
        }
        if (path.StartsWith("/xl/", System.StringComparison.Ordinal))
        {
            return path.Substring(4);
        }
        return path;
    }

    internal XlsxPackageVariant ResolveSavePackageVariant(XlsxWriterOptions options)
    {
        if (options != null && options.PackageVariant != XlsxPackageVariant.Xlsx)
        {
            return options.PackageVariant;
        }

        if (HasVbaProject || HasVbaProjectRelationship())
        {
            if (PackageVariant == XlsxPackageVariant.Xltm || PackageVariant == XlsxPackageVariant.Xltx)
            {
                return XlsxPackageVariant.Xltm;
            }

            return XlsxPackageVariant.Xlsm;
        }

        if (PackageVariant == XlsxPackageVariant.Xltx)
        {
            return XlsxPackageVariant.Xltx;
        }

        return XlsxPackageVariant.Xlsx;
    }

    internal bool HasVbaProjectRelationship()
    {
        if (WorkbookRelationships == null || WorkbookRelationships.Relationships == null)
        {
            return false;
        }

        foreach (XlsxRelationship relationship in WorkbookRelationships.Relationships)
        {
            if (relationship != null && relationship.Type == XlsxRelationshipTypes.VbaProject)
            {
                return true;
            }
        }

        return false;
    }

    internal XlsxContentTypesPart BuildContentTypesPart()
    {
        return BuildContentTypesPartForVariant(ResolveSavePackageVariant(new XlsxWriterOptions()));
    }

    internal XlsxContentTypesPart BuildContentTypesPartForVariant(XlsxPackageVariant packageVariant)
    {
        XlsxContentTypesPart contentTypes = new XlsxContentTypesPart();
        XlsxContentTypeDefault relsDefault = new XlsxContentTypeDefault();
        relsDefault.Extension = "rels";
        relsDefault.ContentType = XlsxContentTypes.Relationships;
        contentTypes.Defaults.Add(relsDefault);
        XlsxContentTypeDefault xmlDefault = new XlsxContentTypeDefault();
        xmlDefault.Extension = "xml";
        xmlDefault.ContentType = XlsxContentTypes.Xml;
        contentTypes.Defaults.Add(xmlDefault);
        AddOverride(contentTypes, XlsxPackagePaths.Workbook, GetWorkbookContentType(packageVariant));
        return contentTypes;
    }

    internal static string GetWorkbookContentType(XlsxPackageVariant packageVariant)
    {
        if (packageVariant == XlsxPackageVariant.Xlsm)
        {
            return XlsxContentTypes.MacroEnabledWorkbook;
        }

        if (packageVariant == XlsxPackageVariant.Xltx)
        {
            return XlsxContentTypes.TemplateWorkbook;
        }

        if (packageVariant == XlsxPackageVariant.Xltm)
        {
            return XlsxContentTypes.MacroEnabledTemplateWorkbook;
        }

        return XlsxContentTypes.Workbook;
    }

    internal void AddContentTypesForWorkbookRelationships(XlsxContentTypesPart contentTypes, XlsxRelationshipsPart relationships)
    {
        foreach (XlsxRelationship relationship in relationships.Relationships)
        {
            string relationshipType = XlsxRelationshipTypeCompatibility.NormalizeRelationshipType(relationship.Type);
            XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, relationship.Target, relationship.IsExternal || relationship.TargetMode == "External");
            if (!target.Validation.IsValid || target.IsExternal)
            {
                continue;
            }

            if (relationshipType == XlsxRelationshipTypes.Worksheet)
            {
                AddOverride(contentTypes, target.Path, XlsxContentTypes.Worksheet);
            }
            else if (relationshipType == XlsxRelationshipTypes.Chartsheet)
            {
                AddOverride(contentTypes, target.Path, XlsxContentTypes.Chartsheet);
            }
            else if (relationshipType == XlsxRelationshipTypes.Macrosheet)
            {
                AddOverride(contentTypes, target.Path, XlsxContentTypes.Macrosheet);
            }
            else if (relationshipType == XlsxRelationshipTypes.Dialogsheet)
            {
                AddOverride(contentTypes, target.Path, XlsxContentTypes.Dialogsheet);
            }
            else if (relationshipType == XlsxRelationshipTypes.Styles)
            {
                AddOverride(contentTypes, target.Path, XlsxContentTypes.Styles);
            }
            else if (relationshipType == XlsxRelationshipTypes.Theme)
            {
                AddOverride(contentTypes, target.Path, XlsxContentTypes.Theme);
            }
            else if (relationshipType == XlsxRelationshipTypes.SharedStrings)
            {
                AddOverride(contentTypes, target.Path, XlsxContentTypes.SharedStrings);
            }
            else if (relationshipType == XlsxRelationshipTypes.CalculationChain)
            {
                AddOverride(contentTypes, target.Path, XlsxContentTypes.CalculationChain);
            }
            else if (relationshipType == XlsxRelationshipTypes.PivotCacheDefinition)
            {
                AddOverride(contentTypes, target.Path, XlsxContentTypes.PivotCacheDefinition);
            }
            else if (relationshipType == XlsxRelationshipTypes.ExternalLink)
            {
                AddOverride(contentTypes, target.Path, XlsxContentTypes.ExternalLink);
            }
            else if (relationshipType == XlsxRelationshipTypes.Connections)
            {
                AddOverride(contentTypes, target.Path, XlsxContentTypes.Connections);
            }
            else if (relationshipType == XlsxRelationshipTypes.VbaProject)
            {
                AddOverride(contentTypes, target.Path, XlsxContentTypes.VbaProject);
            }
        }
    }

    internal static void AddOverride(XlsxContentTypesPart contentTypes, string partName, string contentType)
    {
        if (contentTypes.GetOverride(partName) != null)
        {
            return;
        }

        XlsxContentTypeOverride item = new XlsxContentTypeOverride();
        item.PartName = partName;
        item.ContentType = contentType;
        contentTypes.Overrides.Add(item);
    }

    internal void AddCorePropertiesParts(List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes, XlsxRelationshipsPart packageRelationships, XlsxWorkbookSaveResult result)
    {
        XlsxCorePropertiesWriteResult writeResult = new XlsxCorePropertiesWriter().WriteBytes(CoreProperties);
        CopyDiagnostics(result, writeResult.Validation);
        if (writeResult.IsValid)
        {
            entries.Add(CreateEntry(XlsxPackagePaths.CoreProperties, writeResult.Bytes));
            AddOverride(contentTypes, XlsxPackagePaths.CoreProperties, XlsxContentTypes.CoreProperties);
            AddPackageRelationshipIfMissing(packageRelationships, "rIdCore", XlsxRelationshipTypes.CoreProperties, "docProps/core.xml");
        }
    }

    internal void AddExtendedPropertiesParts(List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes, XlsxRelationshipsPart packageRelationships, XlsxWorkbookSaveResult result)
    {
        XlsxExtendedPropertiesWriteResult writeResult = new XlsxExtendedPropertiesWriter().WriteBytes(ExtendedProperties);
        CopyDiagnostics(result, writeResult.Validation);
        if (writeResult.IsValid)
        {
            entries.Add(CreateEntry(XlsxPackagePaths.ExtendedProperties, writeResult.Bytes));
            AddOverride(contentTypes, XlsxPackagePaths.ExtendedProperties, XlsxContentTypes.ExtendedProperties);
            AddPackageRelationshipIfMissing(packageRelationships, "rIdApp", XlsxRelationshipTypes.ExtendedProperties, "docProps/app.xml");
        }
    }

    internal void AddCustomPropertiesParts(List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes, XlsxRelationshipsPart packageRelationships, XlsxWorkbookSaveResult result)
    {
        XlsxCustomPropertiesWriteResult writeResult = new XlsxCustomPropertiesWriter().WriteBytes(CustomProperties);
        CopyDiagnostics(result, writeResult.Validation);
        if (writeResult.IsValid)
        {
            entries.Add(CreateEntry(XlsxPackagePaths.CustomProperties, writeResult.Bytes));
            AddOverride(contentTypes, XlsxPackagePaths.CustomProperties, XlsxContentTypes.CustomProperties);
            AddPackageRelationshipIfMissing(packageRelationships, "rIdCustom", XlsxRelationshipTypes.CustomProperties, "docProps/custom.xml");
        }
    }

    internal void AddSharedStringsPart(List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes, XlsxWorkbookSaveResult result)
    {
        XlsxSharedStringTableWriteResult write = new XlsxSharedStringTableWriter().WriteBytes(SharedStrings);
        CopyDiagnostics(result, write.Validation);
        if (write.IsValid)
        {
            entries.Add(CreateEntry(SharedStrings.PartPath, write.Bytes));
            AddOverride(contentTypes, SharedStrings.PartPath, XlsxContentTypes.SharedStrings);
        }
    }

    internal void AddCalculationChainPart(List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes, XlsxWorkbookSaveResult result)
    {
        XlsxCalculationChainPartWriteResult write = new XlsxCalculationChainPartWriter().WriteBytes(CalculationChain);
        CopyDiagnostics(result, write.Validation);
        if (write.IsValid)
        {
            entries.Add(CreateEntry(CalculationChain.PartPath, write.Bytes));
            AddOverride(contentTypes, CalculationChain.PartPath, XlsxContentTypes.CalculationChain);
        }
    }

    internal void AddConnectionsPart(List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes, XlsxWorkbookSaveResult result)
    {
        XlsxWorkbookConnectionsPartWriteResult write = new XlsxWorkbookConnectionsPartWriter().WriteBytes(Connections);
        CopyDiagnostics(result, write.Validation);
        if (write.IsValid)
        {
            entries.Add(CreateEntry(Connections.PartPath, write.Bytes));
            AddOverride(contentTypes, Connections.PartPath, XlsxContentTypes.Connections);
        }
    }

    internal void AddAggregatePartEntries(List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes, XlsxWorkbookSaveResult result)
    {
        AddDrawingAggregateEntries(entries, contentTypes, result);
        AddImageAggregateEntries(entries, contentTypes, result);
        AddCommentsAggregateEntries(entries, contentTypes, result);
        AddThreadedCommentsAggregateEntries(entries, contentTypes, result);
    }

    internal void AddDrawingAggregateEntries(List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes, XlsxWorkbookSaveResult result)
    {
        foreach (XlsxDrawingPart part in Drawings)
        {
            XlsxDrawingPartWriteResult write = new XlsxDrawingPartWriter().WriteBytes(part);
            CopyDiagnostics(result, write.Validation);
            if (write.IsValid)
            {
                entries.Add(CreateEntry(part.PartPath, write.Bytes));
                AddOverride(contentTypes, part.PartPath, XlsxContentTypes.Drawing);
            }
        }
    }

    internal void AddImageAggregateEntries(List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes, XlsxWorkbookSaveResult result)
    {
        foreach (XlsxImagePart part in Images)
        {
            XlsxImagePartWriteResult write = new XlsxImagePartWriter().WriteBytes(part);
            CopyDiagnostics(result, write.Validation);
            if (write.IsValid)
            {
                entries.Add(CreateEntry(part.PartPath, write.Bytes));
                AddOverride(contentTypes, part.PartPath, part.ContentType);
            }
        }
    }

    internal void AddCommentsAggregateEntries(List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes, XlsxWorkbookSaveResult result)
    {
        foreach (XlsxCommentsPart part in Comments)
        {
            XlsxCommentsPartWriteResult write = new XlsxCommentsPartWriter().WriteBytes(part);
            CopyDiagnostics(result, write.Validation);
            if (write.IsValid)
            {
                entries.Add(CreateEntry(part.PartPath, write.Bytes));
                AddOverride(contentTypes, part.PartPath, XlsxContentTypes.Comments);
            }
        }
    }

    internal void AddThreadedCommentsAggregateEntries(List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes, XlsxWorkbookSaveResult result)
    {
        foreach (XlsxThreadedCommentsPart part in ThreadedComments)
        {
            XlsxThreadedCommentsPartWriteResult write = new XlsxThreadedCommentsPartWriter().WriteBytes(part);
            CopyDiagnostics(result, write.Validation);
            if (write.IsValid)
            {
                entries.Add(CreateEntry(part.PartPath, write.Bytes));
                AddOverride(contentTypes, part.PartPath, XlsxContentTypes.ThreadedComments);
            }
        }
    }

    internal void AddStylesParts(List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes, XlsxRelationshipsPart workbookRelationships, XlsxWorkbookSaveResult result)
    {
        XlsxStylesPartWriteResult writeResult = new XlsxStylesPartWriter().WriteBytes(Styles);
        CopyDiagnostics(result, writeResult.Validation);
        if (writeResult.IsValid)
        {
            entries.Add(CreateEntry(XlsxPackagePaths.Styles, writeResult.Bytes));
            AddOverride(contentTypes, XlsxPackagePaths.Styles, XlsxContentTypes.Styles);
            AddPackageRelationshipIfMissing(workbookRelationships, GetNextRelationshipId(workbookRelationships), XlsxRelationshipTypes.Styles, "styles.xml");
        }
    }

    internal void AddThemeParts(List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes, XlsxRelationshipsPart workbookRelationships, XlsxWorkbookSaveResult result)
    {
        XlsxThemePartWriteResult writeResult = new XlsxThemePartWriter().WriteBytes(Theme);
        CopyDiagnostics(result, writeResult.Validation);
        if (writeResult.IsValid)
        {
            entries.Add(CreateEntry(XlsxPackagePaths.Theme, writeResult.Bytes));
            AddOverride(contentTypes, XlsxPackagePaths.Theme, XlsxContentTypes.Theme);
            AddPackageRelationshipIfMissing(workbookRelationships, GetNextRelationshipId(workbookRelationships), XlsxRelationshipTypes.Theme, "theme/theme1.xml");
        }
    }

    internal void AddWorkbookEntry(List<XlsxPackageEntry> entries, XlsxWorkbookSaveResult result)
    {
        XlsxWorkbookPartWriteResult writeResult = new XlsxWorkbookPartWriter().WriteBytes(WorkbookPart);
        CopyDiagnostics(result, writeResult.Validation);
        if (writeResult.IsValid)
        {
            entries.Add(CreateEntry(XlsxPackagePaths.Workbook, writeResult.Bytes));
        }
    }

    internal void AddRelationshipsEntry(List<XlsxPackageEntry> entries, XlsxRelationshipsPart relationships, XlsxWorkbookSaveResult result)
    {
        XlsxRelationshipsWriteResult writeResult = new XlsxPartRelationshipsWriter().WriteBytes(relationships);
        CopyDiagnostics(result, writeResult.Validation);
        if (writeResult.IsValid)
        {
            entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships, writeResult.Bytes));
        }
    }

    internal void AddWorkbookRelationshipsEntry(List<XlsxPackageEntry> entries, XlsxRelationshipsPart relationships, XlsxWorkbookSaveResult result)
    {
        XlsxRelationshipsWriteResult writeResult = new XlsxPartRelationshipsWriter().WriteBytes(relationships);
        CopyDiagnostics(result, writeResult.Validation);
        if (writeResult.IsValid)
        {
            entries.Add(CreateEntry(XlsxPackagePaths.WorkbookRelationships, writeResult.Bytes));
        }
    }

    internal void AddContentTypesEntry(List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes, XlsxWorkbookSaveResult result)
    {
        XlsxContentTypesWriteResult writeResult = new XlsxContentTypesWriter().WriteBytes(contentTypes);
        CopyDiagnostics(result, writeResult.Validation);
        if (writeResult.IsValid)
        {
            entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes, writeResult.Bytes));
        }
    }

    internal void AddWorksheetEntries(List<XlsxPackageEntry> entries, XlsxRelationshipsPart relationships)
    {
        foreach (XlsxRelationship relationship in relationships.Relationships)
        {
            if (relationship.Type != XlsxRelationshipTypes.Worksheet)
            {
                continue;
            }

            XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, relationship.Target, relationship.IsExternal || relationship.TargetMode == "External");
            if (!target.Validation.IsValid || target.IsExternal)
            {
                continue;
            }

            if (UnknownParts != null && UnknownParts.ContainsPart(target.Path))
            {
                continue;
            }

            entries.Add(CreateEntry(target.Path, Encoding.UTF8.GetBytes(CreateMinimumWorksheetXml())));
        }
    }

    internal void AddChartsheetEntries(List<XlsxPackageEntry> entries, XlsxRelationshipsPart relationships, XlsxWorkbookSaveResult result)
    {
        foreach (XlsxRelationship relationship in relationships.Relationships)
        {
            if (relationship.Type != XlsxRelationshipTypes.Chartsheet)
            {
                continue;
            }

            XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, relationship.Target, relationship.IsExternal || relationship.TargetMode == "External");
            if (!target.Validation.IsValid || target.IsExternal)
            {
                continue;
            }

            XlsxChartsheetPart? chartsheet = FindChartsheetPart(target.Path);
            if (chartsheet != null)
            {
                XlsxChartsheetPartWriteResult write = new XlsxChartsheetPartWriter().WriteBytes(chartsheet);
                CopyDiagnostics(result, write.Validation);
                if (write.IsValid)
                {
                    entries.Add(CreateEntry(target.Path, write.Bytes));
                }
                continue;
            }

            if (UnknownParts != null && UnknownParts.ContainsPart(target.Path))
            {
                continue;
            }

            entries.Add(CreateEntry(target.Path, Encoding.UTF8.GetBytes(CreateMinimumChartsheetXml())));
        }
    }

    internal XlsxChartsheetPart? FindChartsheetPart(string path)
    {
        foreach (XlsxChartsheetPart chartsheet in Chartsheets)
        {
            if (chartsheet != null && chartsheet.PartPath == path)
            {
                return chartsheet;
            }
        }
        return null;
    }

    internal static string CreateMinimumWorksheetXml()
    {
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData /></worksheet>";
    }

    internal static string CreateMinimumChartsheetXml()
    {
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?><chartsheet xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" />";
    }

    internal static XlsxPackageEntry CreateEntry(string path, byte[] bytes)
    {
        XlsxPackageEntry entry = new XlsxPackageEntry();
        entry.Path = path;
        if (bytes == null)
        {
            entry.Data = new byte[0];
        }
        else
        {
            entry.Data = bytes;
        }

        entry.UncompressedBytes = entry.Data.Length;
        return entry;
    }

    internal static XlsxWorkbookSheet CreateSheet(string name, uint sheetId, string relationshipId)
    {
        return CreateSheet(name, sheetId, relationshipId, XlsxSheetType.Worksheet);
    }

    internal static XlsxWorkbookSheet CreateSheet(string name, uint sheetId, string relationshipId, XlsxSheetType sheetType)
    {
        XlsxWorkbookSheet sheet = new XlsxWorkbookSheet();
        sheet.Name = name;
        sheet.SheetId = sheetId;
        sheet.RelationshipId = relationshipId;
        sheet.SheetType = sheetType;
        sheet.ContentType = XlsxWorkbookRelationshipResolver.GetSheetContentType(sheetType);
        return sheet;
    }

    internal uint GetNextSheetId()
    {
        uint maxSheetId = 0;
        foreach (XlsxWorkbookSheet sheet in WorkbookPart.Sheets)
        {
            if (sheet.SheetId > maxSheetId)
            {
                maxSheetId = sheet.SheetId;
            }
        }

        return maxSheetId + 1;
    }

    internal string GetNextWorkbookRelationshipId()
    {
        int index = 1;
        while (true)
        {
            string relationshipId = "rId" + index.ToString(CultureInfo.InvariantCulture);
            if (WorkbookRelationships.GetRelationship(relationshipId) == null)
            {
                return relationshipId;
            }

            index = index + 1;
        }
    }

    internal int GetSheetIndex(string name)
    {
        int index = 0;
        while (index < WorkbookPart.Sheets.Count)
        {
            if (WorkbookPart.Sheets[index].Name == name)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool RemoveWorkbookRelationship(string? relationshipId)
    {
        int index = 0;
        while (index < WorkbookRelationships.Relationships.Count)
        {
            if (WorkbookRelationships.Relationships[index].Id == relationshipId)
            {
                WorkbookRelationships.Relationships.RemoveAt(index);
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static XlsxRelationship CreateWorksheetRelationship(string relationshipId, string target)
    {
        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = relationshipId;
        relationship.Type = XlsxRelationshipTypes.Worksheet;
        relationship.Target = target;
        return relationship;
    }

    internal static bool IsValidSheetState(string? state)
    {
        if (state == null || state.Length == 0 || state == "visible" || state == "hidden" || state == "veryHidden")
        {
            return true;
        }

        return false;
    }

    internal static XlsxRelationshipsPart CreatePackageRelationships()
    {
        XlsxRelationshipsPart relationships = new XlsxRelationshipsPart();
        relationships.SourcePartPath = "/";
        relationships.PartPath = XlsxPackagePaths.PackageRelationships;
        XlsxRelationship workbook = new XlsxRelationship();
        workbook.Id = "rId1";
        workbook.Type = XlsxRelationshipTypes.OfficeDocument;
        workbook.Target = "xl/workbook.xml";
        relationships.Relationships.Add(workbook);
        return relationships;
    }

    internal static XlsxRelationshipsPart CreateWorkbookRelationships()
    {
        XlsxRelationshipsPart relationships = new XlsxRelationshipsPart();
        relationships.SourcePartPath = XlsxPackagePaths.Workbook;
        relationships.PartPath = XlsxPackagePaths.WorkbookRelationships;
        XlsxRelationship worksheet = new XlsxRelationship();
        worksheet.Id = "rId1";
        worksheet.Type = XlsxRelationshipTypes.Worksheet;
        worksheet.Target = "worksheets/sheet1.xml";
        relationships.Relationships.Add(worksheet);
        return relationships;
    }

    internal static void AddPackageRelationshipIfMissing(XlsxRelationshipsPart relationships, string relationshipId, string type, string target)
    {
        if (relationships.GetFirstByType(type) != null)
        {
            return;
        }

        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = relationshipId;
        relationship.Type = type;
        relationship.Target = target;
        relationships.Relationships.Add(relationship);
    }

    internal static string GetNextRelationshipId(XlsxRelationshipsPart relationships)
    {
        int index = 1;
        while (true)
        {
            string relationshipId = "rId" + index.ToString(CultureInfo.InvariantCulture);
            if (relationships.GetRelationship(relationshipId) == null)
            {
                return relationshipId;
            }

            index = index + 1;
        }
    }

    internal static XlsxContentTypesPart ReadContentTypes(XlsxZipPackageReadResult package, XlsxWorkbookOpenResult result)
    {
        XlsxPackageEntry? entry = package.GetEntry(XlsxPackagePaths.ContentTypes);
        if (entry == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.contentTypesMissing", "Workbook package is missing [Content_Types].xml."));
            return new XlsxContentTypesPart();
        }

        XlsxContentTypesPart part = new XlsxContentTypesReader().ReadBytes(entry.Data);
        CopyDiagnostics(result, part.Validation);
        return part;
    }

    internal static List<XlsxPackagePart> EnumeratePackageParts(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes)
    {
        List<XlsxPackagePart> parts = new List<XlsxPackagePart>();
        if (package == null || package.Entries == null)
        {
            return parts;
        }

        foreach (XlsxPackageEntry entry in package.Entries)
        {
            XlsxPackagePart part = new XlsxPackagePart();
            part.Path = entry.Path;
            part.ContentType = contentTypes.GetContentType(entry.Path);
            part.CompressedBytes = entry.CompressedBytes;
            part.UncompressedBytes = entry.UncompressedBytes;
            part.IsRelationshipPart = IsRelationshipPartPath(entry.Path);
            part.Data = entry.Data;
            parts.Add(part);
        }

        parts.Sort(ComparePackagePartsByPath);
        return parts;
    }

    internal static List<XlsxRelationshipsPart> EnumerateRelationshipParts(XlsxZipPackageReadResult package, XlsxRelationshipsPart packageRelationships, XlsxRelationshipsPart workbookRelationships, XlsxReaderOptions options, XlsxWorkbookOpenResult result)
    {
        List<XlsxRelationshipsPart> parts = new List<XlsxRelationshipsPart>();
        if (package == null || package.Entries == null)
        {
            return parts;
        }

        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (!IsRelationshipPartPath(entry.Path))
            {
                continue;
            }

            XlsxRelationshipsPart part;
            if (entry.Path == XlsxPackagePaths.PackageRelationships)
            {
                part = packageRelationships;
            }
            else if (entry.Path == XlsxPackagePaths.WorkbookRelationships)
            {
                part = workbookRelationships;
            }
            else
            {
                part = new XlsxPartRelationshipsReader().ReadBytes(entry.Path, entry.Data, options);
                CopyDiagnostics(result, part.Validation);
            }

            parts.Add(part);
        }

        parts.Sort(CompareRelationshipPartsByPath);
        return parts;
    }

    internal static XlsxPackagePreviewMetadata ReadPackagePreviewMetadata(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxRelationshipsPart packageRelationships)
    {
        XlsxPackagePreviewMetadata metadata = new XlsxPackagePreviewMetadata();
        if (package == null || packageRelationships == null || packageRelationships.Relationships == null)
        {
            return metadata;
        }

        foreach (XlsxRelationship relationship in packageRelationships.Relationships)
        {
            if (relationship.Type != XlsxRelationshipTypes.Thumbnail || relationship.IsExternal)
            {
                continue;
            }

            string targetPath = relationship.TargetPath;
            if (targetPath == null || targetPath.Length == 0)
            {
                XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget("/", relationship.Target, false);
                if (!target.Validation.IsValid || target.IsExternal)
                {
                    continue;
                }

                targetPath = target.Path;
            }

            XlsxPackageEntry? entry = package.GetEntry(targetPath);
            if (entry == null)
            {
                continue;
            }

            metadata.HasThumbnail = true;
            metadata.RelationshipId = relationship.Id;
            metadata.PartPath = entry.Path;
            metadata.ContentType = contentTypes.GetContentType(entry.Path);
            metadata.ByteLength = entry.UncompressedBytes;
            metadata.Data = entry.Data;
            return metadata;
        }

        return metadata;
    }

    internal static XlsxPackageConformance DetectPackageConformance(XlsxWorkbookPart workbookPart)
    {
        if (workbookPart == null)
        {
            return XlsxPackageConformance.Unknown;
        }

        if (workbookPart.NamespaceUri == XlsxNamespaces.StrictSpreadsheet)
        {
            return XlsxPackageConformance.Strict;
        }

        if (workbookPart.NamespaceUri == XlsxNamespaces.Spreadsheet)
        {
            return XlsxPackageConformance.Transitional;
        }

        return XlsxPackageConformance.Unknown;
    }

    internal static bool IsRelationshipPartPath(string path)
    {
        if (path == null || !path.EndsWith(".rels"))
        {
            return false;
        }

        return path == XlsxPackagePaths.PackageRelationships || path.IndexOf("/_rels/") >= 0;
    }

    internal static int ComparePackagePartsByPath(XlsxPackagePart left, XlsxPackagePart right)
    {
        return string.Compare(left.Path, right.Path, System.StringComparison.Ordinal);
    }

    internal static int CompareRelationshipPartsByPath(XlsxRelationshipsPart left, XlsxRelationshipsPart right)
    {
        return string.Compare(left.PartPath, right.PartPath, System.StringComparison.Ordinal);
    }

    internal static XlsxPackageVariant DetectPackageVariant(XlsxContentTypesPart contentTypes)
    {
        if (contentTypes == null)
        {
            return XlsxPackageVariant.Xlsx;
        }

        XlsxContentTypeOverride? workbookOverride = contentTypes.GetOverride(XlsxPackagePaths.Workbook);
        if (workbookOverride == null)
        {
            return XlsxPackageVariant.Xlsx;
        }

        if (workbookOverride.ContentType == XlsxContentTypes.MacroEnabledWorkbook)
        {
            return XlsxPackageVariant.Xlsm;
        }

        if (workbookOverride.ContentType == XlsxContentTypes.TemplateWorkbook)
        {
            return XlsxPackageVariant.Xltx;
        }

        if (workbookOverride.ContentType == XlsxContentTypes.MacroEnabledTemplateWorkbook)
        {
            return XlsxPackageVariant.Xltm;
        }

        return XlsxPackageVariant.Xlsx;
    }

    internal static bool IsMacroEnabledPackageVariant(XlsxPackageVariant packageVariant)
    {
        return packageVariant == XlsxPackageVariant.Xlsm || packageVariant == XlsxPackageVariant.Xltm;
    }

    internal static bool ShouldLoadWorksheetParts(XlsxReaderOptions options)
    {
        if (options == null)
        {
            return false;
        }

        if (options.MetadataOnly || options.StylesOnly)
        {
            return false;
        }

        return options.ValuesOnly || options.FormulasOnly || (options.SelectedSheets != null && options.SelectedSheets.Count > 0) || (options.SelectedRanges != null && options.SelectedRanges.Count > 0);
    }

    internal static List<XlsxWorksheetPart> ReadWorksheetParts(XlsxZipPackageReadResult package, XlsxWorkbookRelationshipResolutionResult relationshipResolution, XlsxReaderOptions options, XlsxWorkbookOpenResult result)
    {
        List<XlsxWorksheetPart> worksheets = new List<XlsxWorksheetPart>();
        if (package == null || relationshipResolution == null)
        {
            return worksheets;
        }

        foreach (XlsxWorkbookResolvedRelationship resolved in relationshipResolution.Worksheets)
        {
            if (resolved == null || !ShouldLoadSheet(resolved.Sheet, options))
            {
                continue;
            }

            XlsxPackageEntry? entry = package.GetEntry(resolved.TargetPath);
            if (entry == null)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.worksheetMissing", "Workbook worksheet relationship target is missing.");
                diagnostic.SetLocation(resolved.TargetPath, "", "");
                diagnostic.SetRelationshipId(resolved.Id);
                result.AddDiagnostic(diagnostic);
                continue;
            }

            XlsxWorksheetPart worksheet = new XlsxWorksheetPartReader().ReadBytes(entry.Data);
            worksheet.PartPath = entry.Path;
            ApplyWorksheetLoadFilters(worksheet, options, result);
            CopyWorksheetDiagnostics(result, worksheet.Validation);
            worksheets.Add(worksheet);
        }

        return worksheets;
    }

    internal static bool ShouldLoadSheet(XlsxWorkbookSheet? sheet, XlsxReaderOptions options)
    {
        if (options == null || options.SelectedSheets == null || options.SelectedSheets.Count == 0)
        {
            return true;
        }

        if (sheet == null)
        {
            return false;
        }

        foreach (string selectedSheet in options.SelectedSheets)
        {
            if (string.Equals(sheet.Name, selectedSheet, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static void ApplyWorksheetLoadFilters(XlsxWorksheetPart worksheet, XlsxReaderOptions options, XlsxWorkbookOpenResult result)
    {
        if (worksheet == null || options == null)
        {
            return;
        }

        List<XlsxA1RangeReferenceValue> selectedRanges = ParseSelectedRanges(options, result);
        foreach (XlsxWorksheetRow row in worksheet.Rows)
        {
            int cellIndex = 0;
            while (cellIndex < row.Cells.Count)
            {
                XlsxWorksheetCell cell = row.Cells[cellIndex];
                if (!ShouldKeepCell(cell, options, selectedRanges))
                {
                    row.Cells.RemoveAt(cellIndex);
                    continue;
                }

                if (options.ValuesOnly)
                {
                    ClearCellFormula(cell);
                }

                cellIndex = cellIndex + 1;
            }
        }
    }

    internal static bool ShouldKeepCell(XlsxWorksheetCell cell, XlsxReaderOptions options, List<XlsxA1RangeReferenceValue> selectedRanges)
    {
        if (cell == null)
        {
            return false;
        }

        if (options != null && options.FormulasOnly && !cell.HasFormula && (cell.Formula == null || cell.Formula.Length == 0))
        {
            return false;
        }

        if (selectedRanges != null && selectedRanges.Count > 0 && !CellIsInSelectedRanges(cell.Reference, selectedRanges))
        {
            return false;
        }

        return true;
    }

    internal static void ClearCellFormula(XlsxWorksheetCell cell)
    {
        if (cell == null)
        {
            return;
        }

        cell.Formula = "";
        cell.FormulaInfo = new XlsxCellFormula();
        cell.HasFormula = false;
    }

    internal static List<XlsxA1RangeReferenceValue> ParseSelectedRanges(XlsxReaderOptions options, XlsxWorkbookOpenResult result)
    {
        List<XlsxA1RangeReferenceValue> ranges = new List<XlsxA1RangeReferenceValue>();
        if (options == null || options.SelectedRanges == null)
        {
            return ranges;
        }

        foreach (string selectedRange in options.SelectedRanges)
        {
            string rangeText = StripSheetQualifier(selectedRange);
            XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(rangeText);
            if (!range.IsValid)
            {
                foreach (XlsxDiagnostic diagnostic in range.Validation.Diagnostics)
                {
                    result.AddDiagnostic(diagnostic);
                }

                continue;
            }

            ranges.Add(range.Range);
        }

        return ranges;
    }

    internal static string StripSheetQualifier(string reference)
    {
        string text = XlsxCellValueConverter.NormalizeText(reference);
        int bangIndex = text.LastIndexOf('!');
        if (bangIndex >= 0 && bangIndex < text.Length - 1)
        {
            return text.Substring(bangIndex + 1);
        }

        return text;
    }

    internal static bool CellIsInSelectedRanges(string? reference, List<XlsxA1RangeReferenceValue> ranges)
    {
        XlsxA1CellReferenceResult cell = XlsxA1CellReferenceConverter.Parse(reference);
        if (!cell.IsValid)
        {
            return false;
        }

        foreach (XlsxA1RangeReferenceValue range in ranges)
        {
            if (CellIsInRange(cell.Reference, range))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool CellIsInRange(XlsxCellReferenceValue cell, XlsxA1RangeReferenceValue range)
    {
        if (cell == null || range == null)
        {
            return false;
        }

        int firstRow = range.First.Row;
        int lastRow = range.Last.Row;
        int firstColumn = range.First.Column;
        int lastColumn = range.Last.Column;
        if (firstRow > lastRow)
        {
            int swap = firstRow;
            firstRow = lastRow;
            lastRow = swap;
        }

        if (firstColumn > lastColumn)
        {
            int swap = firstColumn;
            firstColumn = lastColumn;
            lastColumn = swap;
        }

        return cell.Row >= firstRow && cell.Row <= lastRow && cell.Column >= firstColumn && cell.Column <= lastColumn;
    }

    internal static void CopyWorksheetDiagnostics(XlsxWorkbookOpenResult target, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }

    internal static XlsxVbaProjectPart ReadVbaProjectPart(XlsxZipPackageReadResult package, XlsxWorkbookRelationshipResolutionResult relationshipResolution, XlsxWorkbookOpenResult result)
    {
        if (package == null || relationshipResolution == null || !relationshipResolution.HasVbaProject || relationshipResolution.VbaProject == null)
        {
            return new XlsxVbaProjectPart();
        }

        XlsxPackageEntry? entry = package.GetEntry(relationshipResolution.VbaProject.TargetPath);
        if (entry == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.vbaProjectMissing", "Workbook VBA project relationship target is missing.");
            diagnostic.SetLocation(relationshipResolution.VbaProject.TargetPath, "", "");
            result.AddDiagnostic(diagnostic);
            return new XlsxVbaProjectPart();
        }

        XlsxVbaProjectPart part = new XlsxVbaProjectPartReader().ReadBytes(entry.Path, entry.Data);
        CopyDiagnostics(result, part.Validation);
        return part;
    }

    internal static XlsxWorkbookPart ReadWorkbookPart(XlsxZipPackageReadResult package, XlsxWorkbookOpenResult result)
    {
        XlsxPackageEntry? entry = package.GetEntry(XlsxPackagePaths.Workbook);
        if (entry == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.workbookMissing", "Workbook package is missing xl/workbook.xml."));
            return new XlsxWorkbookPart();
        }

        XlsxWorkbookPart part = new XlsxWorkbookPartReader().ReadBytes(entry.Data, new XlsxReaderOptions());
        CopyDiagnostics(result, part.Validation);
        return part;
    }

    internal static XlsxStylesPart ReadStylesPart(XlsxZipPackageReadResult package, XlsxWorkbookRelationshipResolutionResult relationshipResolution, XlsxWorkbookOpenResult result)
    {
        if (relationshipResolution.Styles == null)
        {
            return new XlsxStylesPart();
        }

        XlsxPackageEntry? entry = package.GetEntry(relationshipResolution.Styles.TargetPath);
        if (entry == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.stylesMissing", "Workbook styles relationship target is missing."));
            return new XlsxStylesPart();
        }

        XlsxStylesPart part = new XlsxStylesPartReader().ReadBytes(entry.Data);
        CopyDiagnostics(result, part.Validation);
        return part;
    }

    internal static XlsxThemePart ReadThemePart(XlsxZipPackageReadResult package, XlsxWorkbookRelationshipResolutionResult relationshipResolution, XlsxWorkbookOpenResult result)
    {
        if (relationshipResolution.Theme == null)
        {
            return new XlsxThemePart();
        }

        XlsxPackageEntry? entry = package.GetEntry(relationshipResolution.Theme.TargetPath);
        if (entry == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.themeMissing", "Workbook theme relationship target is missing."));
            return new XlsxThemePart();
        }

        XlsxThemePart part = new XlsxThemePartReader().ReadBytes(entry.Data);
        CopyDiagnostics(result, part.Validation);
        return part;
    }

    internal static XlsxSharedStringTablePart ReadSharedStringsPart(XlsxZipPackageReadResult package, XlsxWorkbookRelationshipResolutionResult relationshipResolution, XlsxReaderOptions options, XlsxWorkbookOpenResult result)
    {
        if (relationshipResolution.SharedStrings == null)
        {
            return new XlsxSharedStringTablePart();
        }
        XlsxPackageEntry? entry = package.GetEntry(relationshipResolution.SharedStrings.TargetPath);
        if (entry == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.sharedStringsMissing", "Workbook shared strings relationship target is missing."));
            return new XlsxSharedStringTablePart();
        }
        XlsxSharedStringTablePart part = new XlsxSharedStringTableReader().ReadBytesWithOptions(entry.Data, options);
        part.PartPath = entry.Path;
        CopyDiagnostics(result, part.Validation);
        return part;
    }

    internal static XlsxCalculationChainPart ReadCalculationChainPart(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxWorkbookOpenResult result)
    {
        XlsxPackageEntry? entry = FindPackagePartByPathOrContentType(package, contentTypes, XlsxPackagePaths.CalculationChain, XlsxContentTypes.CalculationChain);
        if (entry == null)
        {
            return new XlsxCalculationChainPart();
        }
        XlsxCalculationChainPart part = new XlsxCalculationChainPartReader().ReadBytes(entry.Data);
        part.PartPath = entry.Path;
        CopyDiagnostics(result, part.Validation);
        return part;
    }

    internal static List<XlsxChartsheetPart> ReadChartsheetParts(XlsxZipPackageReadResult package, XlsxWorkbookRelationshipResolutionResult relationshipResolution, XlsxWorkbookOpenResult result)
    {
        List<XlsxChartsheetPart> parts = new List<XlsxChartsheetPart>();
        foreach (XlsxWorkbookResolvedRelationship resolved in relationshipResolution.Chartsheets)
        {
            XlsxPackageEntry? entry = package.GetEntry(resolved.TargetPath);
            if (entry == null)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.chartsheetMissing", "Workbook chartsheet relationship target is missing.");
                diagnostic.SetLocation(resolved.TargetPath, "", "");
                result.AddDiagnostic(diagnostic);
                continue;
            }
            XlsxChartsheetPart part = new XlsxChartsheetPartReader().ReadString(Encoding.UTF8.GetString(entry.Data));
            part.PartPath = entry.Path;
            CopyDiagnostics(result, part.Validation);
            parts.Add(part);
        }
        return parts;
    }

    internal static List<XlsxWorkbookSheet> ReadSheetMetadata(List<XlsxWorkbookResolvedRelationship> relationships)
    {
        List<XlsxWorkbookSheet> sheets = new List<XlsxWorkbookSheet>();
        foreach (XlsxWorkbookResolvedRelationship relationship in relationships)
        {
            if (relationship != null && relationship.HasSheet && relationship.Sheet != null)
            {
                sheets.Add(relationship.Sheet);
            }
        }
        return sheets;
    }

    internal static List<XlsxDrawingPart> ReadDrawingParts(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxWorkbookOpenResult result)
    {
        List<XlsxDrawingPart> parts = new List<XlsxDrawingPart>();
        foreach (XlsxPackageEntry entry in package.Entries)
        {
            string? contentType = contentTypes.GetContentType(entry.Path);
            if (contentType != XlsxContentTypes.Drawing && !entry.Path.StartsWith(XlsxPackagePaths.DrawingsDirectory, System.StringComparison.Ordinal))
            {
                continue;
            }
            if (!entry.Path.EndsWith(".xml", System.StringComparison.Ordinal) || entry.Path.Contains("/_rels/"))
            {
                continue;
            }
            XlsxDrawingPart part = new XlsxDrawingPartReader().ReadString(Encoding.UTF8.GetString(entry.Data));
            part.PartPath = entry.Path;
            CopyDiagnostics(result, part.Validation);
            parts.Add(part);
        }
        return parts;
    }

    internal static List<XlsxImagePart> ReadImageParts(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxWorkbookOpenResult result)
    {
        List<XlsxImagePart> parts = new List<XlsxImagePart>();
        foreach (XlsxPackageEntry entry in package.Entries)
        {
            string? contentType = contentTypes.GetContentType(entry.Path);
            if (!entry.Path.StartsWith(XlsxPackagePaths.MediaDirectory, System.StringComparison.Ordinal) || contentType == null || !contentType.StartsWith("image/", System.StringComparison.Ordinal))
            {
                continue;
            }
            XlsxImagePart part = new XlsxImagePartReader().ReadBytesWithContentType(entry.Path, contentType, entry.Data);
            CopyDiagnostics(result, part.Validation);
            parts.Add(part);
        }
        return parts;
    }

    internal static List<XlsxCommentsPart> ReadCommentsParts(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxWorkbookOpenResult result)
    {
        List<XlsxCommentsPart> parts = new List<XlsxCommentsPart>();
        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (contentTypes.GetContentType(entry.Path) != XlsxContentTypes.Comments)
            {
                continue;
            }
            XlsxCommentsPart part = new XlsxCommentsPartReader().ReadBytes(entry.Data);
            part.PartPath = entry.Path;
            CopyDiagnostics(result, part.Validation);
            parts.Add(part);
        }
        return parts;
    }

    internal static List<XlsxThreadedCommentsPart> ReadThreadedCommentsParts(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxWorkbookOpenResult result)
    {
        List<XlsxThreadedCommentsPart> parts = new List<XlsxThreadedCommentsPart>();
        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (contentTypes.GetContentType(entry.Path) != XlsxContentTypes.ThreadedComments)
            {
                continue;
            }
            XlsxThreadedCommentsPart part = new XlsxThreadedCommentsPartReader().ReadBytes(entry.Data);
            part.PartPath = entry.Path;
            CopyDiagnostics(result, part.Validation);
            parts.Add(part);
        }
        return parts;
    }

    internal static XlsxPackageEntry? FindPackagePartByPathOrContentType(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, string path, string contentType)
    {
        XlsxPackageEntry? direct = package.GetEntry(path);
        if (direct != null)
        {
            return direct;
        }
        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (contentTypes.GetContentType(entry.Path) == contentType)
            {
                return entry;
            }
        }
        return null;
    }

    internal static bool HasPackagePartWithContentType(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, string path, string contentType)
    {
        return FindPackagePartByPathOrContentType(package, contentTypes, path, contentType) != null;
    }

    internal static List<XlsxChartPart> ReadChartParts(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxWorkbookOpenResult result)
    {
        List<XlsxChartPart> parts = new List<XlsxChartPart>();
        if (package == null)
        {
            return parts;
        }

        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (!IsChartPartEntry(entry, contentTypes))
            {
                continue;
            }

            XlsxChartPart part = new XlsxChartPartReader().ReadBytesFromPart(entry.Path, entry.Data);
            CopyDiagnostics(result, part.Validation);
            parts.Add(part);
        }

        return parts;
    }

    internal static bool IsChartPartEntry(XlsxPackageEntry entry, XlsxContentTypesPart contentTypes)
    {
        if (entry == null)
        {
            return false;
        }

        if (contentTypes != null && contentTypes.GetContentType(entry.Path) == XlsxContentTypes.Chart)
        {
            return true;
        }

        if (entry.Path != null && entry.Path.StartsWith("/xl/charts/", System.StringComparison.Ordinal) && entry.Path.EndsWith(".xml", System.StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    internal static List<XlsxPivotCacheDefinitionPart> ReadPivotCacheDefinitionParts(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxWorkbookOpenResult result)
    {
        List<XlsxPivotCacheDefinitionPart> parts = new List<XlsxPivotCacheDefinitionPart>();
        if (package == null)
        {
            return parts;
        }

        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (!IsPivotCacheDefinitionEntry(entry, contentTypes))
            {
                continue;
            }

            XlsxPivotCacheDefinitionPart part = new XlsxPivotCacheDefinitionReader().ReadBytes(entry.Path, entry.Data);
            CopyDiagnostics(result, part.Validation);
            parts.Add(part);
        }

        return parts;
    }

    internal static bool IsPivotCacheDefinitionEntry(XlsxPackageEntry entry, XlsxContentTypesPart contentTypes)
    {
        if (entry == null)
        {
            return false;
        }

        if (contentTypes != null && contentTypes.GetContentType(entry.Path) == XlsxContentTypes.PivotCacheDefinition)
        {
            return true;
        }

        if (entry.Path != null && entry.Path.StartsWith("/xl/pivotCache/pivotCacheDefinition", System.StringComparison.Ordinal) && entry.Path.EndsWith(".xml", System.StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    internal static List<XlsxPivotTableDefinitionPart> ReadPivotTableDefinitionParts(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxReaderOptions options, XlsxWorkbookOpenResult result)
    {
        List<XlsxPivotTableDefinitionPart> parts = new List<XlsxPivotTableDefinitionPart>();
        if (package == null)
        {
            return parts;
        }

        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (!IsPivotTableDefinitionEntry(entry, contentTypes))
            {
                continue;
            }

            XlsxPivotTableDefinitionPart part = new XlsxPivotTableDefinitionReader().ReadBytes(entry.Path, entry.Data);
            CopyDiagnostics(result, part.Validation);
            ResolvePivotTableCacheRelationship(package, entry, options, result, part);
            parts.Add(part);
        }

        return parts;
    }

    internal static void ResolvePivotTableCacheRelationship(XlsxZipPackageReadResult package, XlsxPackageEntry entry, XlsxReaderOptions options, XlsxWorkbookOpenResult result, XlsxPivotTableDefinitionPart part)
    {
        string relationshipPartPath = XlsxPackagePath.GetRelationshipPartPath(entry.Path);
        XlsxPackageEntry? relationshipEntry = package.GetEntry(relationshipPartPath);
        if (relationshipEntry == null)
        {
            return;
        }

        XlsxRelationshipsPart relationships = new XlsxPartRelationshipsReader().ReadBytes(entry.Path, relationshipEntry.Data, options);
        CopyDiagnostics(result, relationships.Validation);
        XlsxPivotTableCacheRelationshipResult cacheResult = part.ResolveCacheRelationship(relationships);
        CopyDiagnostics(result, cacheResult.Validation);
    }

    internal static bool IsPivotTableDefinitionEntry(XlsxPackageEntry entry, XlsxContentTypesPart contentTypes)
    {
        if (entry == null)
        {
            return false;
        }

        if (contentTypes != null && contentTypes.GetContentType(entry.Path) == XlsxContentTypes.PivotTable)
        {
            return true;
        }

        if (entry.Path != null && entry.Path.StartsWith(XlsxPackagePaths.PivotTablesDirectory + "pivotTable", System.StringComparison.Ordinal) && entry.Path.EndsWith(".xml", System.StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    internal static List<XlsxSlicerTimelinePart> ReadSlicerTimelineParts(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxWorkbookOpenResult result, string contentType, string directoryPath, string rootElementName)
    {
        List<XlsxSlicerTimelinePart> parts = new List<XlsxSlicerTimelinePart>();
        if (package == null)
        {
            return parts;
        }

        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (!IsSlicerTimelineEntry(entry, contentTypes, contentType, directoryPath))
            {
                continue;
            }

            XlsxSlicerTimelinePart part = new XlsxSlicerTimelinePartReader().ReadBytes(entry.Path, entry.Data, rootElementName);
            CopyDiagnostics(result, part.Validation);
            parts.Add(part);
        }

        return parts;
    }

    internal static bool IsSlicerTimelineEntry(XlsxPackageEntry entry, XlsxContentTypesPart contentTypes, string contentType, string directoryPath)
    {
        if (entry == null)
        {
            return false;
        }

        if (contentTypes != null && contentTypes.GetContentType(entry.Path) == contentType)
        {
            return true;
        }

        if (entry.Path != null && entry.Path.StartsWith(directoryPath, System.StringComparison.Ordinal) && entry.Path.EndsWith(".xml", System.StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    internal static XlsxWorkbookConnectionsPart ReadWorkbookConnectionsPart(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxWorkbookOpenResult result)
    {
        if (package == null)
        {
            return new XlsxWorkbookConnectionsPart();
        }

        XlsxPackageEntry? entry = package.GetEntry(XlsxPackagePaths.Connections);
        if (entry == null)
        {
            foreach (XlsxPackageEntry packageEntry in package.Entries)
            {
                if (contentTypes != null && contentTypes.GetContentType(packageEntry.Path) == XlsxContentTypes.Connections)
                {
                    entry = packageEntry;
                    break;
                }
            }
        }

        if (entry == null)
        {
            return new XlsxWorkbookConnectionsPart();
        }

        XlsxWorkbookConnectionsPart part = new XlsxWorkbookConnectionsPartReader().ReadBytes(entry.Path, entry.Data);
        part.RawXml = entry.GetText();
        CopyDiagnostics(result, part.Validation);
        return part;
    }

    internal static List<XlsxQueryTablePart> ReadQueryTableParts(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxWorkbookOpenResult result)
    {
        List<XlsxQueryTablePart> parts = new List<XlsxQueryTablePart>();
        if (package == null)
        {
            return parts;
        }

        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (!IsQueryTableEntry(entry, contentTypes))
            {
                continue;
            }

            XlsxQueryTablePart part = new XlsxQueryTablePartReader().ReadBytes(entry.Path, entry.Data);
            part.RawXml = entry.GetText();
            CopyDiagnostics(result, part.Validation);
            parts.Add(part);
        }

        return parts;
    }

    internal static bool IsQueryTableEntry(XlsxPackageEntry entry, XlsxContentTypesPart contentTypes)
    {
        if (entry == null)
        {
            return false;
        }

        if (contentTypes != null && contentTypes.GetContentType(entry.Path) == XlsxContentTypes.QueryTable)
        {
            return true;
        }

        if (entry.Path != null && entry.Path.StartsWith(XlsxPackagePaths.QueryTablesDirectory + "queryTable", System.StringComparison.Ordinal) && entry.Path.EndsWith(".xml", System.StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    internal static List<XlsxCustomXmlPart> ReadCustomXmlParts(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxWorkbookOpenResult result)
    {
        List<XlsxCustomXmlPart> parts = new List<XlsxCustomXmlPart>();
        if (package == null)
        {
            return parts;
        }

        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (!IsCustomXmlEntry(entry, contentTypes))
            {
                continue;
            }

            string? contentType = "";
            if (contentTypes != null)
            {
                contentType = contentTypes.GetContentType(entry.Path);
            }

            XlsxCustomXmlPart part = new XlsxCustomXmlPartReader().ReadBytes(entry.Path, contentType, entry.Data);
            CopyDiagnostics(result, part.Validation);
            parts.Add(part);
        }

        return parts;
    }

    internal static bool IsCustomXmlEntry(XlsxPackageEntry entry, XlsxContentTypesPart? contentTypes)
    {
        if (entry == null || entry.Path == null)
        {
            return false;
        }

        if (!entry.Path.StartsWith(XlsxPackagePaths.CustomXmlDirectory, System.StringComparison.Ordinal))
        {
            return false;
        }

        if (entry.Path.StartsWith(XlsxPackagePaths.CustomXmlDirectory + "_rels/", System.StringComparison.Ordinal))
        {
            return false;
        }

        if (contentTypes != null && contentTypes.GetContentType(entry.Path) == XlsxContentTypes.CustomXmlProperties)
        {
            return true;
        }

        if (entry.Path.EndsWith(".xml", System.StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    internal static List<XlsxEmbeddedObjectPart> ReadEmbeddedObjectParts(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxWorkbookOpenResult result)
    {
        List<XlsxEmbeddedObjectPart> parts = new List<XlsxEmbeddedObjectPart>();
        if (package == null)
        {
            return parts;
        }

        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (!IsEmbeddedObjectEntry(entry))
            {
                continue;
            }

            string? contentType = "";
            if (contentTypes != null)
            {
                contentType = contentTypes.GetContentType(entry.Path);
            }

            XlsxEmbeddedObjectPart part = new XlsxEmbeddedObjectPartReader().ReadBytes(entry.Path, contentType, entry.Data);
            CopyDiagnostics(result, part.Validation);
            parts.Add(part);
        }

        return parts;
    }

    internal static bool IsEmbeddedObjectEntry(XlsxPackageEntry entry)
    {
        if (entry == null || entry.Path == null)
        {
            return false;
        }

        if (!entry.Path.StartsWith(XlsxPackagePaths.EmbeddingsDirectory, System.StringComparison.Ordinal))
        {
            return false;
        }

        if (entry.Path.StartsWith(XlsxPackagePaths.EmbeddingsDirectory + "_rels/", System.StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    internal static List<XlsxInCellPictureMetadataPart> ReadInCellPictureMetadataParts(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxWorkbookOpenResult result)
    {
        List<XlsxInCellPictureMetadataPart> parts = new List<XlsxInCellPictureMetadataPart>();
        if (package == null)
        {
            return parts;
        }

        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (!IsInCellPictureMetadataEntry(entry))
            {
                continue;
            }

            string? contentType = "";
            if (contentTypes != null)
            {
                contentType = contentTypes.GetContentType(entry.Path);
            }

            XlsxInCellPictureMetadataPart part = new XlsxInCellPictureMetadataPartReader().ReadBytes(entry.Path, contentType, entry.Data);
            CopyDiagnostics(result, part.Validation);
            parts.Add(part);
        }

        return parts;
    }

    internal static bool IsInCellPictureMetadataEntry(XlsxPackageEntry entry)
    {
        if (entry == null || entry.Path == null)
        {
            return false;
        }

        if (entry.Path == XlsxPackagePaths.SheetMetadata)
        {
            return true;
        }

        if (!entry.Path.StartsWith(XlsxPackagePaths.RichDataDirectory, System.StringComparison.Ordinal))
        {
            return false;
        }

        if (entry.Path.StartsWith(XlsxPackagePaths.RichDataDirectory + "_rels/", System.StringComparison.Ordinal))
        {
            return false;
        }

        return entry.Path.EndsWith(".xml", System.StringComparison.Ordinal);
    }

    internal static List<XlsxDigitalSignaturePart> ReadDigitalSignatureParts(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxRelationshipsPart packageRelationships, XlsxReaderOptions options, XlsxWorkbookOpenResult result)
    {
        List<XlsxDigitalSignaturePart> parts = new List<XlsxDigitalSignaturePart>();
        if (package == null)
        {
            return parts;
        }

        bool hasOriginRelationship = false;
        if (packageRelationships != null && packageRelationships.Relationships != null)
        {
            foreach (XlsxRelationship relationship in packageRelationships.Relationships)
            {
                if (relationship != null && relationship.Type == XlsxRelationshipTypes.DigitalSignatureOrigin)
                {
                    hasOriginRelationship = true;
                    ValidateDigitalSignatureOriginRelationship(package, relationship, result);
                }
            }
        }

        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (!IsDigitalSignatureEntry(entry))
            {
                continue;
            }

            string? contentType = "";
            if (contentTypes != null)
            {
                contentType = contentTypes.GetContentType(entry.Path);
            }

            XlsxDigitalSignaturePart part = new XlsxDigitalSignaturePartReader().ReadBytes(entry.Path, contentType, entry.Data);
            CopyDiagnostics(result, part.Validation);
            parts.Add(part);
        }

        if (parts.Count > 0 || hasOriginRelationship)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Warning, "workbook.open.digitalSignaturesPresent", "Workbook package contains digital signature parts; saving changes may invalidate signatures."));
        }

        ValidateDigitalSignatureRelationships(package, options, result);
        return parts;
    }

    internal static bool IsDigitalSignatureEntry(XlsxPackageEntry entry)
    {
        if (entry == null || entry.Path == null)
        {
            return false;
        }

        if (!entry.Path.StartsWith(XlsxPackagePaths.DigitalSignaturesDirectory, System.StringComparison.Ordinal))
        {
            return false;
        }

        if (entry.Path.StartsWith(XlsxPackagePaths.DigitalSignaturesDirectory + "_rels/", System.StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    internal static void ValidateDigitalSignatureOriginRelationship(XlsxZipPackageReadResult package, XlsxRelationship relationship, XlsxWorkbookOpenResult result)
    {
        XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget("/", relationship.Target, relationship.IsExternal || relationship.TargetMode == "External");
        if (!target.Validation.IsValid || target.IsExternal)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "digitalSignature.originRelationshipTargetInvalid", "Digital signature origin relationship target is invalid.");
            diagnostic.SetLocation(XlsxPackagePaths.PackageRelationships, "Relationship", "Target");
            diagnostic.SetRelationshipId(relationship.Id);
            result.AddDiagnostic(diagnostic);
            return;
        }

        if (package.GetEntry(target.Path) == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "digitalSignature.originPartMissing", "Digital signature origin relationship target is missing.");
            diagnostic.SetLocation(target.Path, "", "");
            diagnostic.SetRelationshipId(relationship.Id);
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void ValidateDigitalSignatureRelationships(XlsxZipPackageReadResult package, XlsxReaderOptions options, XlsxWorkbookOpenResult result)
    {
        XlsxPackageEntry? originRelationshipsEntry = package.GetEntry(XlsxPackagePath.GetRelationshipPartPath(XlsxPackagePaths.DigitalSignatureOrigin));
        if (originRelationshipsEntry == null)
        {
            return;
        }

        XlsxRelationshipsPart relationships = new XlsxPartRelationshipsReader().ReadBytes(XlsxPackagePaths.DigitalSignatureOrigin, originRelationshipsEntry.Data, options);
        CopyDiagnostics(result, relationships.Validation);
        foreach (XlsxRelationship relationship in relationships.Relationships)
        {
            if (relationship.Type != XlsxRelationshipTypes.DigitalSignature)
            {
                continue;
            }

            if (relationship.TargetPath == null || relationship.TargetPath.Length == 0 || package.GetEntry(relationship.TargetPath) == null)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "digitalSignature.signaturePartMissing", "Digital signature relationship target is missing.");
                diagnostic.SetLocation(originRelationshipsEntry.Path, "Relationship", "Target");
                diagnostic.SetRelationshipId(relationship.Id);
                result.AddDiagnostic(diagnostic);
            }
        }
    }

    internal static List<XlsxExternalLinkPart> ReadExternalLinkParts(XlsxZipPackageReadResult package, XlsxContentTypesPart contentTypes, XlsxReaderOptions options, XlsxWorkbookOpenResult result)
    {
        List<XlsxExternalLinkPart> parts = new List<XlsxExternalLinkPart>();
        if (package == null)
        {
            return parts;
        }

        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (!IsExternalLinkEntry(entry, contentTypes))
            {
                continue;
            }

            XlsxExternalLinkPart part = new XlsxExternalLinkPartReader().ReadBytes(entry.Path, entry.Data);
            CopyDiagnostics(result, part.Validation);
            ResolveExternalLinkRelationship(package, entry, options, result, part);
            parts.Add(part);
        }

        return parts;
    }

    internal static void AddExternalLinkRelationshipParts(List<XlsxExternalLinkPart> parts, XlsxZipPackageReadResult package, XlsxWorkbookRelationshipResolutionResult relationshipResolution, XlsxReaderOptions options, XlsxWorkbookOpenResult result)
    {
        if (parts == null || package == null || relationshipResolution == null)
        {
            return;
        }

        foreach (XlsxWorkbookResolvedRelationship resolved in relationshipResolution.ExternalLinks)
        {
            if (resolved == null || resolved.TargetPath == null || resolved.TargetPath.Length == 0)
            {
                continue;
            }

            if (ContainsExternalLinkPart(parts, resolved.TargetPath))
            {
                continue;
            }

            XlsxPackageEntry? entry = package.GetEntry(resolved.TargetPath);
            if (entry == null)
            {
                continue;
            }

            XlsxExternalLinkPart part = new XlsxExternalLinkPartReader().ReadBytes(entry.Path, entry.Data);
            CopyDiagnostics(result, part.Validation);
            ResolveExternalLinkRelationship(package, entry, options, result, part);
            parts.Add(part);
        }
    }

    internal static bool ContainsExternalLinkPart(List<XlsxExternalLinkPart> parts, string partPath)
    {
        foreach (XlsxExternalLinkPart part in parts)
        {
            if (part != null && part.PartPath == partPath)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsExternalLinkEntry(XlsxPackageEntry entry, XlsxContentTypesPart contentTypes)
    {
        if (entry == null)
        {
            return false;
        }

        if (contentTypes != null && contentTypes.GetContentType(entry.Path) == XlsxContentTypes.ExternalLink)
        {
            return true;
        }

        if (entry.Path != null && entry.Path.StartsWith(XlsxPackagePaths.ExternalLinksDirectory + "externalLink", System.StringComparison.Ordinal) && entry.Path.EndsWith(".xml", System.StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    internal static void ResolveExternalLinkRelationship(XlsxZipPackageReadResult package, XlsxPackageEntry entry, XlsxReaderOptions options, XlsxWorkbookOpenResult result, XlsxExternalLinkPart part)
    {
        string relationshipPartPath = XlsxPackagePath.GetRelationshipPartPath(entry.Path);
        XlsxPackageEntry? relationshipEntry = package.GetEntry(relationshipPartPath);
        if (relationshipEntry == null)
        {
            return;
        }

        XlsxRelationshipsPart relationships = new XlsxPartRelationshipsReader().ReadBytes(entry.Path, relationshipEntry.Data, options);
        CopyDiagnostics(result, relationships.Validation);
        part.ResolveExternalBookRelationship(relationships);
    }

    internal static XlsxRelationshipsPart ReadPackageRelationships(XlsxZipPackageReadResult package, XlsxReaderOptions options, XlsxWorkbookOpenResult result)
    {
        XlsxPackageEntry? entry = package.GetEntry(XlsxPackagePaths.PackageRelationships);
        if (entry == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.packageRelationshipsMissing", "Workbook package is missing _rels/.rels."));
            return new XlsxRelationshipsPart();
        }

        XlsxRelationshipsPart part = new XlsxPackageRelationshipsReader().ReadBytes(entry.Data, options);
        CopyDiagnostics(result, part.Validation);
        return part;
    }

    internal static XlsxRelationshipsPart ReadWorkbookRelationships(XlsxZipPackageReadResult package, XlsxReaderOptions options, XlsxWorkbookOpenResult result)
    {
        XlsxPackageEntry? entry = package.GetEntry(XlsxPackagePaths.WorkbookRelationships);
        if (entry == null)
        {
            return CreateWorkbookRelationships();
        }

        XlsxRelationshipsPart part = new XlsxPartRelationshipsReader().ReadBytes(XlsxPackagePaths.WorkbookRelationships, entry.Data, options);
        CopyDiagnostics(result, part.Validation);
        return part;
    }

    internal static void ReadDocumentProperties(XlsxWorkbook workbook, XlsxZipPackageReadResult package, XlsxWorkbookOpenResult result)
    {
        XlsxPackageEntry? coreEntry = package.GetEntry(XlsxPackagePaths.CoreProperties);
        if (coreEntry != null)
        {
            workbook.CoreProperties = new XlsxCorePropertiesReader().ReadBytes(coreEntry.Data);
            workbook.HasCoreProperties = true;
            CopyDiagnostics(result, workbook.CoreProperties.Validation);
        }

        XlsxPackageEntry? appEntry = package.GetEntry(XlsxPackagePaths.ExtendedProperties);
        if (appEntry != null)
        {
            workbook.ExtendedProperties = new XlsxExtendedPropertiesReader().ReadBytes(appEntry.Data);
            workbook.HasExtendedProperties = true;
            CopyDiagnostics(result, workbook.ExtendedProperties.Validation);
        }

        XlsxPackageEntry? customEntry = package.GetEntry(XlsxPackagePaths.CustomProperties);
        if (customEntry != null)
        {
            workbook.CustomProperties = new XlsxCustomPropertiesReader().ReadBytes(customEntry.Data);
            workbook.HasCustomProperties = true;
            CopyDiagnostics(result, workbook.CustomProperties.Validation);
        }
    }

    internal static void CopyDiagnostics(XlsxWorkbookOpenResult target, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }

    internal static void CopyDiagnostics(XlsxWorkbookSaveResult target, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }

    internal static void CopyDiagnostics(XlsxWorkbookSheetOperationResult target, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }

    internal static void CopyDiagnostics(XlsxValidationResult target, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }
}
