namespace tiff;

/// <summary>Represents GDAL metadata extracted from or prepared for a TIFF file.</summary>
public class TiffGdalMetadata
{
    /// <summary>Indicates whether the metadata is present.</summary>
    public bool HasMetadata;
    /// <summary>Indicates whether the no data is present.</summary>
    public bool HasNoData;
    /// <summary>Contains the metadata xml.</summary>
    public string MetadataXml;
    /// <summary>Contains the metadata values.</summary>
    public string[] MetadataValues;
    /// <summary>Contains the metadata raw bytes.</summary>
    public byte[] MetadataRawBytes;
    /// <summary>Contains the no data text.</summary>
    public string NoDataText;
    /// <summary>Contains the no data values.</summary>
    public string[] NoDataValues;
    /// <summary>Contains the no data raw bytes.</summary>
    public byte[] NoDataRawBytes;

    /// <summary>Creates a new instance of the TIFF GDAL Metadata class.</summary>
    public TiffGdalMetadata()
    {
        HasMetadata = false;
        HasNoData = false;
        MetadataXml = "";
        MetadataValues = new string[0];
        MetadataRawBytes = new byte[0];
        NoDataText = "";
        NoDataValues = new string[0];
        NoDataRawBytes = new byte[0];
    }

    /// <summary>Creates a new instance of the TIFF GDAL Metadata class.</summary>
    /// <param name="hasMetadata">The has metadata.</param>
    /// <param name="metadataXml">The metadata xml.</param>
    /// <param name="metadataValues">The metadata values.</param>
    /// <param name="metadataRawBytes">The metadata raw bytes.</param>
    /// <param name="hasNoData">The has no data.</param>
    /// <param name="noDataText">The no data text.</param>
    /// <param name="noDataValues">The no data values.</param>
    /// <param name="noDataRawBytes">The no data raw bytes.</param>
    internal static TiffGdalMetadata Create(bool hasMetadata, string metadataXml, string[] metadataValues, byte[] metadataRawBytes, bool hasNoData, string noDataText, string[] noDataValues, byte[] noDataRawBytes)
    {
        TiffGdalMetadata value = new TiffGdalMetadata();
        value.Initialize(hasMetadata, metadataXml, metadataValues, metadataRawBytes, hasNoData, noDataText, noDataValues, noDataRawBytes);
        return value;
    }

    internal void Initialize(bool hasMetadata, string metadataXml, string[] metadataValues, byte[] metadataRawBytes, bool hasNoData, string noDataText, string[] noDataValues, byte[] noDataRawBytes)
    {
        HasMetadata = hasMetadata;
        HasNoData = hasNoData;
        MetadataXml = TiffGdalAsciiValue.NormalizeString(metadataXml);
        MetadataValues = TiffAscii.CopyStrings(metadataValues);
        MetadataRawBytes = TiffTagValue.CopyByteArray(metadataRawBytes);
        NoDataText = TiffGdalAsciiValue.NormalizeString(noDataText);
        NoDataValues = TiffAscii.CopyStrings(noDataValues);
        NoDataRawBytes = TiffTagValue.CopyByteArray(noDataRawBytes);
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF GDAL metadata.</returns>
    internal TiffGdalMetadata Copy()
    {
        return TiffGdalMetadata.Create(HasMetadata, MetadataXml, MetadataValues, MetadataRawBytes, HasNoData, NoDataText, NoDataValues, NoDataRawBytes);
    }
}
