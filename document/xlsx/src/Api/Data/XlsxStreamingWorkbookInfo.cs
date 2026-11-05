using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents streaming workbook info data used by the public workbook API.
/// </summary>
public class XlsxStreamingWorkbookInfo
{
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
    /// Stores the relationship resolution value used by this model.
    /// </summary>
    public XlsxWorkbookRelationshipResolutionResult RelationshipResolution;
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
    /// Indicates whether styles is present and should be written.
    /// </summary>
    public bool HasStyles;
    /// <summary>
    /// Indicates whether theme is present and should be written.
    /// </summary>
    public bool HasTheme;
    /// <summary>
    /// Indicates whether shared strings is present and should be written.
    /// </summary>
    public bool HasSharedStrings;
    /// <summary>
    /// Indicates whether vba project is present and should be written.
    /// </summary>
    public bool HasVbaProject;
    /// <summary>
    /// Stores the number of package entry items processed by the operation.
    /// </summary>
    public int PackageEntryCount;
    /// <summary>
    /// Contains total compressed bytes.
    /// </summary>
    public long TotalCompressedBytes;
    /// <summary>
    /// Contains total uncompressed bytes.
    /// </summary>
    public long TotalUncompressedBytes;
    /// <summary>
    /// Stores the metadata bytes loaded value used by this model.
    /// </summary>
    public long MetadataBytesLoaded;
    /// <summary>
    /// Stores the number of skipped sheet entry items processed by the operation.
    /// </summary>
    public int SkippedSheetEntryCount;
    /// <summary>
    /// Contains skipped sheet bytes.
    /// </summary>
    public long SkippedSheetBytes;
    /// <summary>
    /// Stores the number of skipped sheet relationship entry items processed by the operation.
    /// </summary>
    public int SkippedSheetRelationshipEntryCount;
    /// <summary>
    /// Contains skipped sheet relationship bytes.
    /// </summary>
    public long SkippedSheetRelationshipBytes;
    /// <summary>
    /// Initializes a new streaming workbook info with its default state.
    /// </summary>
    public XlsxStreamingWorkbookInfo()
    {
        PackageVariant = XlsxPackageVariant.Xlsx;
        WorkbookPart = new XlsxWorkbookPart();
        PackageRelationships = new XlsxRelationshipsPart();
        WorkbookRelationships = new XlsxRelationshipsPart();
        RelationshipResolution = new XlsxWorkbookRelationshipResolutionResult();
        CoreProperties = new XlsxCorePropertiesPart();
        HasCoreProperties = false;
        ExtendedProperties = new XlsxExtendedPropertiesPart();
        HasExtendedProperties = false;
        CustomProperties = new XlsxCustomPropertiesPart();
        HasCustomProperties = false;
        HasStyles = false;
        HasTheme = false;
        HasSharedStrings = false;
        HasVbaProject = false;
        PackageEntryCount = 0;
        TotalCompressedBytes = 0;
        TotalUncompressedBytes = 0;
        MetadataBytesLoaded = 0;
        SkippedSheetEntryCount = 0;
        SkippedSheetBytes = 0;
        SkippedSheetRelationshipEntryCount = 0;
        SkippedSheetRelationshipBytes = 0;
    }
}
