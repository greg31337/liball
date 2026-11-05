namespace image;

/// <summary>Identifies the standard or purpose of a preserved metadata profile.</summary>
public enum MetadataProfileKind
{
    /// <summary>The profile kind is unknown.</summary>
    Unknown = 0,
    /// <summary>The profile contains EXIF metadata.</summary>
    Exif = 1,
    /// <summary>The profile contains an XMP packet.</summary>
    Xmp = 2,
    /// <summary>The profile contains IPTC metadata.</summary>
    Iptc = 3,
    /// <summary>The profile contains an ICC color profile.</summary>
    Icc = 4,
    /// <summary>The profile contains Photoshop Image Resource Blocks.</summary>
    PhotoshopImageResource = 5,
    /// <summary>The profile contains a JUMBF container.</summary>
    Jumbf = 6,
    /// <summary>The profile contains C2PA provenance data.</summary>
    C2pa = 7,
    /// <summary>The profile contains vendor-specific camera maker notes.</summary>
    MakerNote = 8,
    /// <summary>The profile contains an embedded thumbnail or preview.</summary>
    Thumbnail = 9
}
