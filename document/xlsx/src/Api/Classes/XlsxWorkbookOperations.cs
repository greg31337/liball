using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxWorkbook"/> data.
/// </summary>
public class XlsxWorkbookOperations : IXlsxWorkbookOperations
{
    /// <summary>
    /// Initializes a new xlsx workbook operations instance.
    /// </summary>
    public XlsxWorkbookOperations()
    {
    }

    /// <summary>
    /// Reads the version and kind from an encryption info stream.
    /// </summary>
    /// <param name="bytes">The complete encryption info stream bytes.</param>
    /// <returns>The parsed encryption information and validation diagnostics.</returns>
    public XlsxEncryptionInfo ReadEncryptionInfo(byte[] bytes)
    {
        return XlsxEncryptionDetector.ReadEncryptionInfo(bytes);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.Create through the Create operation.
    /// </summary>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbook Create()
    {
        return XlsxWorkbook.Create();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.AddSheet through the AddSheet operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookSheetOperationResult AddSheet(XlsxWorkbook data, string name)
    {
        return data.AddSheet(name);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.AddRawImage through the AddRawImage operation.
    /// </summary>
    /// <param name="model">The xlsx workbook data to operate on.</param>
    /// <param name="data">The data value used by the operation.</param>
    /// <param name="contentType">The content type value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxRawImageInsertResult AddRawImage(XlsxWorkbook model, byte[] data, string contentType)
    {
        return model.AddRawImage(data, contentType);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.AddRawImagePart through the AddRawImagePart operation.
    /// </summary>
    /// <param name="model">The xlsx workbook data to operate on.</param>
    /// <param name="partPath">The part path value used by the operation.</param>
    /// <param name="data">The data value used by the operation.</param>
    /// <param name="contentType">The content type value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxRawImageInsertResult AddRawImagePart(XlsxWorkbook model, string partPath, byte[] data, string contentType)
    {
        return model.AddRawImagePart(partPath, data, contentType);
    }

    /// <summary>Replaces an existing raw image part without decoding it.</summary>
    /// <param name="model">The workbook aggregate.</param><param name="partPath">The existing part path.</param><param name="data">The replacement bytes.</param><param name="contentType">The content type.</param>
    /// <returns>The replacement image and validation diagnostics.</returns>
    public XlsxRawImageInsertResult ReplaceRawImagePart(XlsxWorkbook model, string partPath, byte[] data, string contentType)
    {
        return model.ReplaceRawImagePart(partPath, data, contentType);
    }

    /// <summary>
    /// Returns a physical package part from the workbook aggregate.
    /// </summary>
    /// <param name="model">The workbook aggregate to inspect.</param>
    /// <param name="partPath">The normalized or package-relative part path.</param>
    /// <returns>The package part when found; otherwise, <c>null</c>.</returns>
    public XlsxPackagePart? GetPackagePart(XlsxWorkbook model, string partPath)
    {
        return model.GetPackagePart(partPath);
    }

    /// <summary>
    /// Replaces or adds a physical package part and refreshes any matching typed aggregate part.
    /// </summary>
    /// <param name="model">The workbook aggregate to update.</param>
    /// <param name="part">The complete replacement part.</param>
    /// <returns>Validation diagnostics for the replacement.</returns>
    public XlsxValidationResult ReplacePackagePart(XlsxWorkbook model, XlsxPackagePart part)
    {
        return model.ReplacePackagePart(part);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.AddWorksheetImage through the AddWorksheetImage operation.
    /// </summary>
    /// <param name="model">The xlsx workbook data to operate on.</param>
    /// <param name="worksheet">The worksheet value used by the operation.</param>
    /// <param name="data">The data value used by the operation.</param>
    /// <param name="contentType">The content type value used by the operation.</param>
    /// <param name="fromCell">The from cell value used by the operation.</param>
    /// <param name="toCell">The to cell value used by the operation.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDrawingImagePlacementResult AddWorksheetImage(XlsxWorkbook model, XlsxWorksheetPart worksheet, byte[] data, string contentType, string fromCell, string toCell, string name)
    {
        return model.AddWorksheetImage(worksheet, data, contentType, fromCell, toCell, name);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.GetSheet through the GetSheet operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookSheet? GetSheet(XlsxWorkbook data, string name)
    {
        return data.GetSheet(name);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.GetSheetAt through the GetSheetAt operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="index">The index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookSheet? GetSheetAt(XlsxWorkbook data, int index)
    {
        return data.GetSheetAt(index);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RemoveSheet through the RemoveSheet operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxValidationResult RemoveSheet(XlsxWorkbook data, string name)
    {
        return data.RemoveSheet(name);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ListExternalLinks through the ListExternalLinks operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public List<XlsxExternalLinkPart> ListExternalLinks(XlsxWorkbook data)
    {
        return data.ListExternalLinks();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.GetConnections through the GetConnections operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookConnectionsPart GetConnections(XlsxWorkbook data)
    {
        return data.GetConnections();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ListQueryTables through the ListQueryTables operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public List<XlsxQueryTablePart> ListQueryTables(XlsxWorkbook data)
    {
        return data.ListQueryTables();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ListCustomXmlParts through the ListCustomXmlParts operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public List<XlsxCustomXmlPart> ListCustomXmlParts(XlsxWorkbook data)
    {
        return data.ListCustomXmlParts();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ListEmbeddedObjects through the ListEmbeddedObjects operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public List<XlsxEmbeddedObjectPart> ListEmbeddedObjects(XlsxWorkbook data)
    {
        return data.ListEmbeddedObjects();
    }

    /// <summary>Adds an OLE object.</summary>
    /// <param name="data">The workbook.</param><param name="worksheet">The worksheet.</param><param name="relationships">The relationships.</param><param name="bytes">The raw bytes.</param><param name="programId">The program identifier.</param><param name="shapeId">The shape identifier.</param>
    /// <returns>The authored object.</returns>
    public XlsxEmbeddedObjectPart AddOleObject(XlsxWorkbook data, XlsxWorksheetPart worksheet, XlsxRelationshipsPart relationships, byte[] bytes, string programId, uint shapeId)
    {
        return data.AddOleObject(worksheet, relationships, bytes, programId, shapeId);
    }

    /// <summary>Adds an embedded package.</summary>
    /// <param name="data">The workbook.</param><param name="worksheet">The worksheet.</param><param name="relationships">The relationships.</param><param name="bytes">The raw bytes.</param><param name="contentType">The content type.</param><param name="extension">The extension.</param><param name="programId">The program identifier.</param><param name="shapeId">The shape identifier.</param>
    /// <returns>The authored object.</returns>
    public XlsxEmbeddedObjectPart AddEmbeddedPackage(XlsxWorkbook data, XlsxWorksheetPart worksheet, XlsxRelationshipsPart relationships, byte[] bytes, string contentType, string extension, string programId, uint shapeId)
    {
        return data.AddEmbeddedPackage(worksheet, relationships, bytes, contentType, extension, programId, shapeId);
    }

    /// <summary>Adds an explicit raw embedded object.</summary>
    /// <param name="data">The workbook.</param><param name="worksheet">The worksheet.</param><param name="relationships">The relationships.</param><param name="partPath">The path.</param><param name="contentType">The content type.</param><param name="bytes">The raw bytes.</param><param name="oleObject">Whether the object is OLE.</param><param name="programId">The program identifier.</param><param name="shapeId">The shape identifier.</param>
    /// <returns>The authored object.</returns>
    public XlsxEmbeddedObjectPart AddRawEmbeddedObject(XlsxWorkbook data, XlsxWorksheetPart worksheet, XlsxRelationshipsPart relationships, string partPath, string contentType, byte[] bytes, bool oleObject, string programId, uint shapeId)
    {
        return data.AddRawEmbeddedObject(worksheet, relationships, partPath, contentType, bytes, oleObject, programId, shapeId);
    }

    /// <summary>Removes an embedded object.</summary>
    /// <param name="data">The workbook.</param><param name="worksheet">The worksheet.</param><param name="relationships">The relationships.</param><param name="partPath">The part path.</param>
    /// <returns>Validation diagnostics.</returns>
    public XlsxValidationResult RemoveEmbeddedObject(XlsxWorkbook data, XlsxWorksheetPart worksheet, XlsxRelationshipsPart relationships, string partPath)
    {
        return data.RemoveEmbeddedObject(worksheet, relationships, partPath);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ListInCellPictureMetadataParts through the ListInCellPictureMetadataParts operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public List<XlsxInCellPictureMetadataPart> ListInCellPictureMetadataParts(XlsxWorkbook data)
    {
        return data.ListInCellPictureMetadataParts();
    }

    /// <summary>Adds in-cell-picture metadata.</summary>
    /// <param name="data">The workbook.</param><param name="partPath">The path.</param><param name="contentType">The content type.</param><param name="bytes">The raw bytes.</param>
    /// <returns>The authored metadata.</returns>
    public XlsxInCellPictureMetadataPart AddInCellPictureMetadata(XlsxWorkbook data, string partPath, string contentType, byte[] bytes)
    {
        return data.AddInCellPictureMetadata(partPath, contentType, bytes);
    }

    /// <summary>Removes in-cell-picture metadata.</summary>
    /// <param name="data">The workbook.</param><param name="partPath">The path.</param>
    /// <returns>Validation diagnostics.</returns>
    public XlsxValidationResult RemoveInCellPictureMetadata(XlsxWorkbook data, string partPath)
    {
        return data.RemoveInCellPictureMetadata(partPath);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ListDigitalSignatures through the ListDigitalSignatures operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public List<XlsxDigitalSignaturePart> ListDigitalSignatures(XlsxWorkbook data)
    {
        return data.ListDigitalSignatures();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RemoveCustomXmlParts through the RemoveCustomXmlParts operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxValidationResult RemoveCustomXmlParts(XlsxWorkbook data)
    {
        return data.RemoveCustomXmlParts();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RemoveMacros through the RemoveMacros operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxValidationResult RemoveMacros(XlsxWorkbook data)
    {
        return data.RemoveMacros();
    }

    /// <summary>Adds or replaces a VBA project.</summary>
    /// <param name="data">The workbook.</param><param name="bytes">The raw VBA bytes.</param>
    /// <returns>The authored project.</returns>
    public XlsxVbaProjectPart SetVbaProject(XlsxWorkbook data, byte[] bytes)
    {
        return data.SetVbaProject(bytes);
    }

    /// <summary>Removes a VBA project.</summary>
    /// <param name="data">The workbook.</param>
    /// <returns>Validation diagnostics.</returns>
    public XlsxValidationResult RemoveVbaProject(XlsxWorkbook data)
    {
        return data.RemoveVbaProject();
    }

    /// <summary>Adds a package-aware form control.</summary>
    /// <param name="data">The workbook.</param><param name="drawing">The drawing.</param><param name="relationships">The relationships.</param><param name="controlProperties">The raw control properties.</param><param name="contentType">The content type.</param><param name="fromCell">The starting cell.</param><param name="extentCxEmu">The horizontal extent.</param><param name="extentCyEmu">The vertical extent.</param><param name="name">The control name.</param>
    /// <returns>The authored form control.</returns>
    public XlsxDrawingObjectOperationResult AddFormControl(XlsxWorkbook data, XlsxDrawingPart drawing, XlsxRelationshipsPart relationships, byte[] controlProperties, string contentType, string fromCell, string extentCxEmu, string extentCyEmu, string name)
    {
        return data.AddFormControl(drawing, relationships, controlProperties, contentType, fromCell, extentCxEmu, extentCyEmu, name);
    }

    /// <summary>Removes a package-aware form control.</summary>
    /// <param name="data">The workbook.</param><param name="drawing">The drawing.</param><param name="relationships">The relationships.</param><param name="controlIndex">The control index.</param>
    /// <returns>The removed form control.</returns>
    public XlsxDrawingObjectOperationResult RemoveFormControl(XlsxWorkbook data, XlsxDrawingPart drawing, XlsxRelationshipsPart relationships, int controlIndex)
    {
        return data.RemoveFormControl(drawing, relationships, controlIndex);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.StripMacros through the StripMacros operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxValidationResult StripMacros(XlsxWorkbook data)
    {
        return data.StripMacros();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RemoveExternalLinks through the RemoveExternalLinks operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxValidationResult RemoveExternalLinks(XlsxWorkbook data)
    {
        return data.RemoveExternalLinks();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.Sanitize through the Sanitize operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookSanitizeResult Sanitize(XlsxWorkbook data)
    {
        return data.Sanitize();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.SanitizeWithOptions through the SanitizeWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookSanitizeResult SanitizeWithOptions(XlsxWorkbook data, XlsxWorkbookSanitizeOptions options)
    {
        return data.SanitizeWithOptions(options);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.CompareTo through the CompareTo operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="other">The other value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookCompareResult CompareTo(XlsxWorkbook data, XlsxWorkbook other)
    {
        return data.CompareTo(other);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.CompareToWithOptions through the CompareToWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="other">The other value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookCompareResult CompareToWithOptions(XlsxWorkbook data, XlsxWorkbook other, XlsxWorkbookCompareOptions options)
    {
        return data.CompareToWithOptions(other, options);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.Validate through the Validate operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxValidationResult Validate(XlsxWorkbook data)
    {
        return data.Validate();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ValidateWithOptions through the ValidateWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxValidationResult ValidateWithOptions(XlsxWorkbook data, XlsxWorkbookValidationOptions options)
    {
        return data.ValidateWithOptions(options);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ValidateUsing through the ValidateUsing operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="validator">The validator value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxValidationResult ValidateUsing(XlsxWorkbook data, IXlsxWorkbookValidator validator)
    {
        return data.ValidateUsing(validator);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ValidateUsingWithOptions through the ValidateUsingWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="validator">The validator value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxValidationResult ValidateUsingWithOptions(XlsxWorkbook data, IXlsxWorkbookValidator validator, XlsxWorkbookValidationOptions options)
    {
        return data.ValidateUsingWithOptions(validator, options);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.Repair through the Repair operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookRepairResult Repair(XlsxWorkbook data)
    {
        return data.Repair();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RepairWithOptions through the RepairWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookRepairResult RepairWithOptions(XlsxWorkbook data, XlsxWorkbookRepairOptions options)
    {
        return data.RepairWithOptions(options);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RepairUsing through the RepairUsing operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="repairer">The repairer value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookRepairResult RepairUsing(XlsxWorkbook data, IXlsxWorkbookRepairer repairer)
    {
        return data.RepairUsing(repairer);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RepairUsingWithOptions through the RepairUsingWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="repairer">The repairer value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookRepairResult RepairUsingWithOptions(XlsxWorkbook data, IXlsxWorkbookRepairer repairer, XlsxWorkbookRepairOptions options)
    {
        return data.RepairUsingWithOptions(repairer, options);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.CountMacros through the CountMacros operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public int CountMacros(XlsxWorkbook data)
    {
        return data.CountMacros();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.CountExternalLinks through the CountExternalLinks operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public int CountExternalLinks(XlsxWorkbook data)
    {
        return data.CountExternalLinks();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.CountCustomXmlParts through the CountCustomXmlParts operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public int CountCustomXmlParts(XlsxWorkbook data)
    {
        return data.CountCustomXmlParts();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ReorderSheet through the ReorderSheet operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="newIndex">The new index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxValidationResult ReorderSheet(XlsxWorkbook data, string name, int newIndex)
    {
        return data.ReorderSheet(name, newIndex);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RenameSheet through the RenameSheet operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="oldName">The old name value used by the operation.</param>
    /// <param name="newName">The new name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxValidationResult RenameSheet(XlsxWorkbook data, string oldName, string newName)
    {
        return data.RenameSheet(oldName, newName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.HideSheet through the HideSheet operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="state">The state value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxValidationResult HideSheet(XlsxWorkbook data, string name, string state)
    {
        return data.HideSheet(name, state);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ValidateSheetNames through the ValidateSheetNames operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxValidationResult ValidateSheetNames(XlsxWorkbook data)
    {
        return data.ValidateSheetNames();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.PreserveCachedFormulaValues through the PreserveCachedFormulaValues operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="worksheets">The worksheets value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookRecalculationResult PreserveCachedFormulaValues(XlsxWorkbook data, List<XlsxWorksheetPart> worksheets)
    {
        return data.PreserveCachedFormulaValues(worksheets);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RecalculateSupportedFormulas through the RecalculateSupportedFormulas operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="worksheets">The worksheets value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookRecalculationResult RecalculateSupportedFormulas(XlsxWorkbook data, List<XlsxWorksheetPart> worksheets)
    {
        return data.RecalculateSupportedFormulas(worksheets);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RecalculateChangedFormulas through the RecalculateChangedFormulas operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="worksheets">The worksheets value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookRecalculationResult RecalculateChangedFormulas(XlsxWorkbook data, List<XlsxWorksheetPart> worksheets)
    {
        return data.RecalculateChangedFormulas(worksheets);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.MarkForExcelRecalculation through the MarkForExcelRecalculation operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookRecalculationResult MarkForExcelRecalculation(XlsxWorkbook data)
    {
        return data.MarkForExcelRecalculation();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.OpenFile through the OpenFile operation.
    /// </summary>
    /// <param name="filePath">The file path value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookOpenResult OpenFile(string filePath)
    {
        return XlsxWorkbook.OpenFile(filePath);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.OpenFile through the OpenFileWithOptions operation.
    /// </summary>
    /// <param name="filePath">The file path value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookOpenResult OpenFileWithOptions(string filePath, XlsxReaderOptions options)
    {
        return XlsxWorkbook.OpenFile(filePath, options);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.OpenBytes through the OpenBytes operation.
    /// </summary>
    /// <param name="bytes">The bytes value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookOpenResult OpenBytes(byte[] bytes)
    {
        return XlsxWorkbook.OpenBytes(bytes);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.OpenBytes through the OpenBytesWithOptions operation.
    /// </summary>
    /// <param name="bytes">The bytes value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookOpenResult OpenBytesWithOptions(byte[] bytes, XlsxReaderOptions options)
    {
        return XlsxWorkbook.OpenBytes(bytes, options);
    }

    /// <summary>
    /// Opens an OOXML workbook package from read-only memory.
    /// </summary>
    /// <param name="bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookOpenResult OpenMemory(ReadOnlyMemory<byte> bytes)
    {
        return XlsxWorkbook.OpenMemory(bytes);
    }

    /// <summary>
    /// Opens an OOXML workbook package from read-only memory using reader options.
    /// </summary>
    /// <param name="bytes">The complete OOXML package bytes to read.</param>
    /// <param name="options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookOpenResult OpenMemoryWithOptions(ReadOnlyMemory<byte> bytes, XlsxReaderOptions options)
    {
        return XlsxWorkbook.OpenMemoryWithOptions(bytes, options);
    }

    /// <summary>
    /// Opens an OOXML workbook package from a read-only byte span.
    /// </summary>
    /// <param name="bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookOpenResult OpenSpan(ReadOnlySpan<byte> bytes)
    {
        return XlsxWorkbook.OpenSpan(bytes);
    }

    /// <summary>
    /// Opens an OOXML workbook package from a read-only byte span using reader options.
    /// </summary>
    /// <param name="bytes">The complete OOXML package bytes to read.</param>
    /// <param name="options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookOpenResult OpenSpanWithOptions(ReadOnlySpan<byte> bytes, XlsxReaderOptions options)
    {
        return XlsxWorkbook.OpenSpanWithOptions(bytes, options);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.OpenStream through the OpenStream operation.
    /// </summary>
    /// <param name="stream">The stream value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookOpenResult OpenStream(Stream stream)
    {
        return XlsxWorkbook.OpenStream(stream);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.OpenStream through the OpenStreamWithOptions operation.
    /// </summary>
    /// <param name="stream">The stream value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookOpenResult OpenStreamWithOptions(Stream stream, XlsxReaderOptions options)
    {
        return XlsxWorkbook.OpenStream(stream, options);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.SaveFile through the SaveFile operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="filePath">The file path value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookSaveResult SaveFile(XlsxWorkbook data, string filePath)
    {
        return data.SaveFile(filePath);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.SaveFileWithOptions through the SaveFileWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="filePath">The file path value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookSaveResult SaveFileWithOptions(XlsxWorkbook data, string filePath, XlsxWriterOptions options)
    {
        return data.SaveFileWithOptions(filePath, options);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.SaveBytes through the SaveBytes operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookSaveResult SaveBytes(XlsxWorkbook data)
    {
        return data.SaveBytes();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.SaveBytesWithOptions through the SaveBytesWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookSaveResult SaveBytesWithOptions(XlsxWorkbook data, XlsxWriterOptions options)
    {
        return data.SaveBytesWithOptions(options);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.SaveStream through the SaveStream operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="stream">The stream value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookSaveResult SaveStream(XlsxWorkbook data, Stream stream)
    {
        return data.SaveStream(stream);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.SaveStreamWithOptions through the SaveStreamWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="stream">The stream value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookSaveResult SaveStreamWithOptions(XlsxWorkbook data, Stream stream, XlsxWriterOptions options)
    {
        return data.SaveStreamWithOptions(stream, options);
    }
}
