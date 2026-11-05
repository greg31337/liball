namespace tiff;

/// <summary>Represents IPTC block used by the public TIFF API.</summary>
public class TiffIptcBlock
{
    /// <summary>Contains the record number.</summary>
    public byte RecordNumber;
    /// <summary>Contains the data set number.</summary>
    public byte DataSetNumber;
    /// <summary>Contains the data.</summary>
    public byte[] Data;

    /// <summary>Creates a new instance of the TIFF IPTC Block class.</summary>
    public TiffIptcBlock()
    {
        RecordNumber = 0;
        DataSetNumber = 0;
        Data = new byte[0];
    }

    /// <summary>Creates a new instance of the TIFF IPTC Block class.</summary>
    /// <param name="recordNumber">The record number.</param>
    /// <param name="dataSetNumber">The data set number.</param>
    /// <param name="data">The data.</param>
    internal static TiffIptcBlock Create(byte recordNumber, byte dataSetNumber, byte[] data)
    {
        TiffIptcBlock value = new TiffIptcBlock();
        value.Initialize(recordNumber, dataSetNumber, data);
        return value;
    }

    internal void Initialize(byte recordNumber, byte dataSetNumber, byte[] data)
    {
        RecordNumber = recordNumber;
        DataSetNumber = dataSetNumber;
        Data = TiffTagValue.CopyByteArray(data);
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF IPTC block.</returns>
    internal TiffIptcBlock Copy()
    {
        return TiffIptcBlock.Create(RecordNumber, DataSetNumber, Data);
    }
}
