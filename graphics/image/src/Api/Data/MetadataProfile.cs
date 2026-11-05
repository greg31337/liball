namespace image;

/// <summary>Preserves an embedded metadata packet, color profile, maker note, or preview.</summary>
public class MetadataProfile
{
    /// <summary>The zero-based profile position in <see cref="ImageMetadataDocument.Profiles"/>.</summary>
    public int Index;
    /// <summary>The display or relationship role assigned to an image-bearing profile.</summary>
    public ImageMetadataRole Role;
    /// <summary>The source container item identifier, or negative one when the profile has no item identifier.</summary>
    public long ContainerItemId;
    /// <summary>The zero-based frame position represented or described by the profile, or negative one when unavailable.</summary>
    public int SourceFrameIndex;
    /// <summary>The profile standard or purpose.</summary>
    public MetadataProfileKind Kind;
    /// <summary>The metadata family associated with the profile.</summary>
    public MetadataFamily Family;
    /// <summary>The stable query key assigned to the profile.</summary>
    public string Key;
    /// <summary>The human-readable profile name.</summary>
    public string Name;
    /// <summary>The media type of the preserved payload.</summary>
    public string MimeType;
    /// <summary>The binary payload, or an empty array when bytes were not retained.</summary>
    public byte[] Data;
    /// <summary>The decoded text for text-based profiles such as XMP.</summary>
    public string Text;
    /// <summary>The zero-based source byte offset, or negative one when unavailable.</summary>
    public long Offset;
    /// <summary>The source or reconstructed payload length in bytes.</summary>
    public long Length;
    /// <summary>The one-based part number for profiles split across container segments, or zero when not applicable.</summary>
    public int SequenceNumber;
    /// <summary>The total number of parts for a split profile, or zero when not applicable.</summary>
    public int SequenceCount;

    /// <summary>Creates an unknown, empty profile with initialized payload fields.</summary>
    public MetadataProfile()
    {
        Index = 0;
        Role = ImageMetadataRole.Unknown;
        ContainerItemId = -1;
        SourceFrameIndex = -1;
        Kind = MetadataProfileKind.Unknown;
        Family = MetadataFamily.Unknown;
        Key = "";
        Name = "";
        MimeType = "application/octet-stream";
        Data = new byte[0];
        Text = "";
        Offset = -1;
        Length = 0;
        SequenceNumber = 0;
        SequenceCount = 0;
    }
}
