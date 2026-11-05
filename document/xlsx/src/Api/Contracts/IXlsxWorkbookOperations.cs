using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxWorkbook"/> data.
/// </summary>
public interface IXlsxWorkbookOperations
{
    /// <summary>
    /// Reads the version and kind from an encryption info stream.
    /// </summary>
    /// <param name="bytes">The complete encryption info stream bytes.</param>
    /// <returns>The parsed encryption information and validation diagnostics.</returns>
    XlsxEncryptionInfo ReadEncryptionInfo(byte[] bytes);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.Create through the Create operation.
    /// </summary>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbook Create();
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.AddSheet through the AddSheet operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookSheetOperationResult AddSheet(XlsxWorkbook data, string name);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.AddRawImage through the AddRawImage operation.
    /// </summary>
    /// <param name="model">The xlsx workbook data to operate on.</param>
    /// <param name="data">The data value used by the operation.</param>
    /// <param name="contentType">The content type value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxRawImageInsertResult AddRawImage(XlsxWorkbook model, byte[] data, string contentType);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.AddRawImagePart through the AddRawImagePart operation.
    /// </summary>
    /// <param name="model">The xlsx workbook data to operate on.</param>
    /// <param name="partPath">The part path value used by the operation.</param>
    /// <param name="data">The data value used by the operation.</param>
    /// <param name="contentType">The content type value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxRawImageInsertResult AddRawImagePart(XlsxWorkbook model, string partPath, byte[] data, string contentType);
    /// <summary>Replaces an existing raw image part without decoding it.</summary>
    /// <param name="model">The workbook aggregate to update.</param>
    /// <param name="partPath">The existing media part path.</param>
    /// <param name="data">The replacement image bytes.</param>
    /// <param name="contentType">The replacement image content type.</param>
    /// <returns>The replacement image and validation diagnostics.</returns>
    XlsxRawImageInsertResult ReplaceRawImagePart(XlsxWorkbook model, string partPath, byte[] data, string contentType);
    /// <summary>
    /// Returns a physical package part from the workbook aggregate.
    /// </summary>
    /// <param name="model">The workbook aggregate to inspect.</param>
    /// <param name="partPath">The normalized or package-relative part path.</param>
    /// <returns>The package part when found; otherwise, <c>null</c>.</returns>
    XlsxPackagePart? GetPackagePart(XlsxWorkbook model, string partPath);
    /// <summary>
    /// Replaces or adds a physical package part and refreshes any matching typed aggregate part.
    /// </summary>
    /// <param name="model">The workbook aggregate to update.</param>
    /// <param name="part">The complete replacement part.</param>
    /// <returns>Validation diagnostics for the replacement.</returns>
    XlsxValidationResult ReplacePackagePart(XlsxWorkbook model, XlsxPackagePart part);
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
    XlsxDrawingImagePlacementResult AddWorksheetImage(XlsxWorkbook model, XlsxWorksheetPart worksheet, byte[] data, string contentType, string fromCell, string toCell, string name);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.GetSheet through the GetSheet operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookSheet? GetSheet(XlsxWorkbook data, string name);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.GetSheetAt through the GetSheetAt operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="index">The index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookSheet? GetSheetAt(XlsxWorkbook data, int index);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RemoveSheet through the RemoveSheet operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxValidationResult RemoveSheet(XlsxWorkbook data, string name);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ListExternalLinks through the ListExternalLinks operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    List<XlsxExternalLinkPart> ListExternalLinks(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.GetConnections through the GetConnections operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookConnectionsPart GetConnections(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ListQueryTables through the ListQueryTables operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    List<XlsxQueryTablePart> ListQueryTables(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ListCustomXmlParts through the ListCustomXmlParts operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    List<XlsxCustomXmlPart> ListCustomXmlParts(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ListEmbeddedObjects through the ListEmbeddedObjects operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    List<XlsxEmbeddedObjectPart> ListEmbeddedObjects(XlsxWorkbook data);
    /// <summary>Adds an opaque OLE compound object and its worksheet relationship.</summary>
    /// <param name="data">The workbook to update.</param><param name="worksheet">The owning worksheet.</param><param name="relationships">The worksheet relationships.</param><param name="bytes">The raw OLE bytes.</param><param name="programId">The OLE program identifier.</param><param name="shapeId">The worksheet shape identifier.</param>
    /// <returns>The authored embedded object part.</returns>
    XlsxEmbeddedObjectPart AddOleObject(XlsxWorkbook data, XlsxWorksheetPart worksheet, XlsxRelationshipsPart relationships, byte[] bytes, string programId, uint shapeId);
    /// <summary>Adds an opaque embedded package and its worksheet relationship.</summary>
    /// <param name="data">The workbook to update.</param><param name="worksheet">The owning worksheet.</param><param name="relationships">The worksheet relationships.</param><param name="bytes">The raw package bytes.</param><param name="contentType">The package content type.</param><param name="extension">The package extension.</param><param name="programId">The OLE program identifier.</param><param name="shapeId">The worksheet shape identifier.</param>
    /// <returns>The authored embedded object part.</returns>
    XlsxEmbeddedObjectPart AddEmbeddedPackage(XlsxWorkbook data, XlsxWorksheetPart worksheet, XlsxRelationshipsPart relationships, byte[] bytes, string contentType, string extension, string programId, uint shapeId);
    /// <summary>Adds an embedded object from raw bytes at an explicit package path.</summary>
    /// <param name="data">The workbook to update.</param><param name="worksheet">The owning worksheet.</param><param name="relationships">The worksheet relationships.</param><param name="partPath">The explicit package path.</param><param name="contentType">The content type.</param><param name="bytes">The opaque payload.</param><param name="oleObject">Whether the payload is an OLE object.</param><param name="programId">The program identifier.</param><param name="shapeId">The worksheet shape identifier.</param>
    /// <returns>The authored embedded object part.</returns>
    XlsxEmbeddedObjectPart AddRawEmbeddedObject(XlsxWorkbook data, XlsxWorksheetPart worksheet, XlsxRelationshipsPart relationships, string partPath, string contentType, byte[] bytes, bool oleObject, string programId, uint shapeId);
    /// <summary>Removes an embedded object, its worksheet reference, relationship, and payload.</summary>
    /// <param name="data">The workbook to update.</param><param name="worksheet">The owning worksheet.</param><param name="relationships">The worksheet relationships.</param><param name="partPath">The embedded object path.</param>
    /// <returns>Validation diagnostics for the operation.</returns>
    XlsxValidationResult RemoveEmbeddedObject(XlsxWorkbook data, XlsxWorksheetPart worksheet, XlsxRelationshipsPart relationships, string partPath);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ListInCellPictureMetadataParts through the ListInCellPictureMetadataParts operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    List<XlsxInCellPictureMetadataPart> ListInCellPictureMetadataParts(XlsxWorkbook data);
    /// <summary>Adds opaque in-cell-picture metadata.</summary>
    /// <param name="data">The workbook to update.</param><param name="partPath">The metadata path.</param><param name="contentType">The metadata content type.</param><param name="bytes">The raw XML bytes.</param>
    /// <returns>The authored metadata part.</returns>
    XlsxInCellPictureMetadataPart AddInCellPictureMetadata(XlsxWorkbook data, string partPath, string contentType, byte[] bytes);
    /// <summary>Removes an in-cell-picture metadata part.</summary>
    /// <param name="data">The workbook to update.</param><param name="partPath">The metadata path.</param>
    /// <returns>Validation diagnostics for the operation.</returns>
    XlsxValidationResult RemoveInCellPictureMetadata(XlsxWorkbook data, string partPath);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ListDigitalSignatures through the ListDigitalSignatures operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    List<XlsxDigitalSignaturePart> ListDigitalSignatures(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RemoveCustomXmlParts through the RemoveCustomXmlParts operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxValidationResult RemoveCustomXmlParts(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RemoveMacros through the RemoveMacros operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxValidationResult RemoveMacros(XlsxWorkbook data);
    /// <summary>Adds or replaces the opaque VBA project and workbook relationship.</summary>
    /// <param name="data">The workbook to update.</param><param name="bytes">The raw VBA project bytes.</param>
    /// <returns>The authored VBA project part.</returns>
    XlsxVbaProjectPart SetVbaProject(XlsxWorkbook data, byte[] bytes);
    /// <summary>Removes the VBA project, its relationship, and macro-enabled package state.</summary>
    /// <param name="data">The workbook to update.</param>
    /// <returns>Validation diagnostics for the operation.</returns>
    XlsxValidationResult RemoveVbaProject(XlsxWorkbook data);
    /// <summary>Adds a form-control anchor, relationship, and opaque control-properties part.</summary>
    /// <param name="data">The workbook to update.</param><param name="drawing">The owning drawing.</param><param name="relationships">The drawing relationships.</param><param name="controlProperties">The raw control-properties bytes.</param><param name="contentType">The control-properties content type.</param><param name="fromCell">The starting cell.</param><param name="extentCxEmu">The horizontal extent.</param><param name="extentCyEmu">The vertical extent.</param><param name="name">The control name.</param>
    /// <returns>The authored form control and validation diagnostics.</returns>
    XlsxDrawingObjectOperationResult AddFormControl(XlsxWorkbook data, XlsxDrawingPart drawing, XlsxRelationshipsPart relationships, byte[] controlProperties, string contentType, string fromCell, string extentCxEmu, string extentCyEmu, string name);
    /// <summary>Removes a form-control anchor, relationship, and control-properties part.</summary>
    /// <param name="data">The workbook to update.</param><param name="drawing">The owning drawing.</param><param name="relationships">The drawing relationships.</param><param name="controlIndex">The zero-based form-control index.</param>
    /// <returns>The removed form control and validation diagnostics.</returns>
    XlsxDrawingObjectOperationResult RemoveFormControl(XlsxWorkbook data, XlsxDrawingPart drawing, XlsxRelationshipsPart relationships, int controlIndex);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.StripMacros through the StripMacros operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxValidationResult StripMacros(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RemoveExternalLinks through the RemoveExternalLinks operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxValidationResult RemoveExternalLinks(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.Sanitize through the Sanitize operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookSanitizeResult Sanitize(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.SanitizeWithOptions through the SanitizeWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookSanitizeResult SanitizeWithOptions(XlsxWorkbook data, XlsxWorkbookSanitizeOptions options);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.CompareTo through the CompareTo operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="other">The other value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookCompareResult CompareTo(XlsxWorkbook data, XlsxWorkbook other);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.CompareToWithOptions through the CompareToWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="other">The other value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookCompareResult CompareToWithOptions(XlsxWorkbook data, XlsxWorkbook other, XlsxWorkbookCompareOptions options);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.Validate through the Validate operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxValidationResult Validate(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ValidateWithOptions through the ValidateWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxValidationResult ValidateWithOptions(XlsxWorkbook data, XlsxWorkbookValidationOptions options);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ValidateUsing through the ValidateUsing operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="validator">The validator value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxValidationResult ValidateUsing(XlsxWorkbook data, IXlsxWorkbookValidator validator);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ValidateUsingWithOptions through the ValidateUsingWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="validator">The validator value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxValidationResult ValidateUsingWithOptions(XlsxWorkbook data, IXlsxWorkbookValidator validator, XlsxWorkbookValidationOptions options);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.Repair through the Repair operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookRepairResult Repair(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RepairWithOptions through the RepairWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookRepairResult RepairWithOptions(XlsxWorkbook data, XlsxWorkbookRepairOptions options);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RepairUsing through the RepairUsing operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="repairer">The repairer value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookRepairResult RepairUsing(XlsxWorkbook data, IXlsxWorkbookRepairer repairer);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RepairUsingWithOptions through the RepairUsingWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="repairer">The repairer value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookRepairResult RepairUsingWithOptions(XlsxWorkbook data, IXlsxWorkbookRepairer repairer, XlsxWorkbookRepairOptions options);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.CountMacros through the CountMacros operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    int CountMacros(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.CountExternalLinks through the CountExternalLinks operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    int CountExternalLinks(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.CountCustomXmlParts through the CountCustomXmlParts operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    int CountCustomXmlParts(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ReorderSheet through the ReorderSheet operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="newIndex">The new index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxValidationResult ReorderSheet(XlsxWorkbook data, string name, int newIndex);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RenameSheet through the RenameSheet operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="oldName">The old name value used by the operation.</param>
    /// <param name="newName">The new name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxValidationResult RenameSheet(XlsxWorkbook data, string oldName, string newName);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.HideSheet through the HideSheet operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="state">The state value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxValidationResult HideSheet(XlsxWorkbook data, string name, string state);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.ValidateSheetNames through the ValidateSheetNames operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxValidationResult ValidateSheetNames(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.PreserveCachedFormulaValues through the PreserveCachedFormulaValues operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="worksheets">The worksheets value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookRecalculationResult PreserveCachedFormulaValues(XlsxWorkbook data, List<XlsxWorksheetPart> worksheets);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RecalculateSupportedFormulas through the RecalculateSupportedFormulas operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="worksheets">The worksheets value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookRecalculationResult RecalculateSupportedFormulas(XlsxWorkbook data, List<XlsxWorksheetPart> worksheets);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.RecalculateChangedFormulas through the RecalculateChangedFormulas operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="worksheets">The worksheets value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookRecalculationResult RecalculateChangedFormulas(XlsxWorkbook data, List<XlsxWorksheetPart> worksheets);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.MarkForExcelRecalculation through the MarkForExcelRecalculation operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookRecalculationResult MarkForExcelRecalculation(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.OpenFile through the OpenFile operation.
    /// </summary>
    /// <param name="filePath">The file path value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookOpenResult OpenFile(string filePath);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.OpenFile through the OpenFileWithOptions operation.
    /// </summary>
    /// <param name="filePath">The file path value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookOpenResult OpenFileWithOptions(string filePath, XlsxReaderOptions options);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.OpenBytes through the OpenBytes operation.
    /// </summary>
    /// <param name="bytes">The bytes value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookOpenResult OpenBytes(byte[] bytes);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.OpenBytes through the OpenBytesWithOptions operation.
    /// </summary>
    /// <param name="bytes">The bytes value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookOpenResult OpenBytesWithOptions(byte[] bytes, XlsxReaderOptions options);
    /// <summary>
    /// Opens an OOXML workbook package from read-only memory.
    /// </summary>
    /// <param name="bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookOpenResult OpenMemory(ReadOnlyMemory<byte> bytes);
    /// <summary>
    /// Opens an OOXML workbook package from read-only memory using reader options.
    /// </summary>
    /// <param name="bytes">The complete OOXML package bytes to read.</param>
    /// <param name="options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookOpenResult OpenMemoryWithOptions(ReadOnlyMemory<byte> bytes, XlsxReaderOptions options);
    /// <summary>
    /// Opens an OOXML workbook package from a read-only byte span.
    /// </summary>
    /// <param name="bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookOpenResult OpenSpan(ReadOnlySpan<byte> bytes);
    /// <summary>
    /// Opens an OOXML workbook package from a read-only byte span using reader options.
    /// </summary>
    /// <param name="bytes">The complete OOXML package bytes to read.</param>
    /// <param name="options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookOpenResult OpenSpanWithOptions(ReadOnlySpan<byte> bytes, XlsxReaderOptions options);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.OpenStream through the OpenStream operation.
    /// </summary>
    /// <param name="stream">The stream value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookOpenResult OpenStream(Stream stream);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.OpenStream through the OpenStreamWithOptions operation.
    /// </summary>
    /// <param name="stream">The stream value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookOpenResult OpenStreamWithOptions(Stream stream, XlsxReaderOptions options);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.SaveFile through the SaveFile operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="filePath">The file path value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookSaveResult SaveFile(XlsxWorkbook data, string filePath);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.SaveFileWithOptions through the SaveFileWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="filePath">The file path value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookSaveResult SaveFileWithOptions(XlsxWorkbook data, string filePath, XlsxWriterOptions options);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.SaveBytes through the SaveBytes operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookSaveResult SaveBytes(XlsxWorkbook data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.SaveBytesWithOptions through the SaveBytesWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookSaveResult SaveBytesWithOptions(XlsxWorkbook data, XlsxWriterOptions options);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.SaveStream through the SaveStream operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="stream">The stream value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookSaveResult SaveStream(XlsxWorkbook data, Stream stream);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbook"/>.SaveStreamWithOptions through the SaveStreamWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx workbook data to operate on.</param>
    /// <param name="stream">The stream value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookSaveResult SaveStreamWithOptions(XlsxWorkbook data, Stream stream, XlsxWriterOptions options);
}
