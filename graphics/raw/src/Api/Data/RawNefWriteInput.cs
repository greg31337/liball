using System;

namespace raw;

/// <summary>Contains caller-supplied provenance and reviewed Nikon fields for a newly encoded NEF file.</summary>
public class RawNefWriteMetadata
{
    /// <summary>The software identifier written to TIFF Software.</summary>
    public string Software;
    /// <summary>The creator written to TIFF Artist, or an empty string.</summary>
    public string Artist;
    /// <summary>The rights statement written to TIFF Copyright, or an empty string.</summary>
    public string Copyright;
    /// <summary>The caller-supplied original capture timestamp.</summary>
    public RawExifTimestamp? OriginalTime;
    /// <summary>The Nikon white-balance name written to the reviewed maker-note field.</summary>
    public string WhiteBalance;
    /// <summary>The ISO sensitivity written to the reviewed maker-note field.</summary>
    public ushort IsoSensitivity;

    /// <summary>Creates empty NEF provenance metadata.</summary>
    public RawNefWriteMetadata()
    {
        Software = string.Empty;
        Artist = string.Empty;
        Copyright = string.Empty;
        OriginalTime = null;
        WhiteBalance = string.Empty;
        IsoSensitivity = 0;
    }
}

/// <summary>Supplies exact stored Bayer samples, a JPEG preview, and reviewed Nikon metadata for new NEF encoding.</summary>
public class RawNefWriteInput
{
    /// <summary>The stored sensor width.</summary>
    public int Width;
    /// <summary>The stored sensor height.</summary>
    public int Height;
    /// <summary>The source distance between row starts in samples.</summary>
    public int RowStrideSamples;
    /// <summary>Exact unsigned stored sensor samples.</summary>
    public ushort[] StoredSamples;
    /// <summary>The required baseline-JPEG SubIFD preview.</summary>
    public RawThumbnail? Preview;
    /// <summary>Caller-supplied provenance and reviewed Nikon metadata.</summary>
    public RawNefWriteMetadata Metadata;

    /// <summary>Creates an empty NEF write input.</summary>
    public RawNefWriteInput()
    {
        Width = 0;
        Height = 0;
        RowStrideSamples = 0;
        StoredSamples = System.Array.Empty<ushort>();
        Preview = null;
        Metadata = new RawNefWriteMetadata();
    }
}
