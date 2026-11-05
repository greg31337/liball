using System;

namespace raw;

/// <summary>Contains caller-supplied provenance metadata for a newly encoded CR2 file.</summary>
public class RawCr2WriteMetadata
{
    /// <summary>The software identifier written to TIFF Software.</summary>
    public string Software;
    /// <summary>The creator written to TIFF Artist, or an empty string.</summary>
    public string Artist;
    /// <summary>The rights statement written to TIFF Copyright, or an empty string.</summary>
    public string Copyright;
    /// <summary>The caller-supplied original capture timestamp.</summary>
    public RawExifTimestamp? OriginalTime;

    /// <summary>Creates empty CR2 provenance metadata.</summary>
    public RawCr2WriteMetadata()
    {
        Software = string.Empty;
        Artist = string.Empty;
        Copyright = string.Empty;
        OriginalTime = null;
    }
}

/// <summary>Supplies exact stored Bayer samples, previews, and reviewed Canon metadata for new CR2 encoding.</summary>
public class RawCr2WriteInput
{
    /// <summary>The stored sensor width.</summary>
    public int Width;
    /// <summary>The stored sensor height.</summary>
    public int Height;
    /// <summary>The source distance between row starts in samples.</summary>
    public int RowStrideSamples;
    /// <summary>Exact unsigned stored sensor samples.</summary>
    public ushort[] StoredSamples;
    /// <summary>The required IFD0 baseline-JPEG preview.</summary>
    public RawThumbnail? PrimaryPreview;
    /// <summary>The required IFD1 baseline-JPEG thumbnail.</summary>
    public RawThumbnail? Thumbnail;
    /// <summary>The required IFD2 baseline-JPEG preview.</summary>
    public RawThumbnail? SecondaryPreview;
    /// <summary>The exact reviewed Canon CameraSettings words; unrecognized values are caller-owned.</summary>
    public short[] CanonCameraSettings;
    /// <summary>The exact reviewed Canon ColorData words; unrecognized values are caller-owned.</summary>
    public ushort[] CanonColorData;
    /// <summary>The exact caller-supplied Canon ModelID written to the reviewed maker-note field.</summary>
    public uint CanonModelId;
    /// <summary>Caller-supplied provenance metadata.</summary>
    public RawCr2WriteMetadata Metadata;

    /// <summary>Creates an empty CR2 write input.</summary>
    public RawCr2WriteInput()
    {
        Width = 0;
        Height = 0;
        RowStrideSamples = 0;
        StoredSamples = System.Array.Empty<ushort>();
        PrimaryPreview = null;
        Thumbnail = null;
        SecondaryPreview = null;
        CanonCameraSettings = System.Array.Empty<short>();
        CanonColorData = System.Array.Empty<ushort>();
        CanonModelId = 0;
        Metadata = new RawCr2WriteMetadata();
    }
}
