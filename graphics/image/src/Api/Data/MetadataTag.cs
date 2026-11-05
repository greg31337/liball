namespace image;

/// <summary>Represents one typed metadata item together with its source identity and location.</summary>
public class MetadataTag
{
    /// <summary>The stable query key for the tag.</summary>
    public string Key;
    /// <summary>The human-readable tag name.</summary>
    public string Name;
    /// <summary>The metadata standard or source family that defines the tag.</summary>
    public MetadataFamily Family;
    /// <summary>The source-specific directory or block group.</summary>
    public string Group;
    /// <summary>The source numeric tag identifier, or zero when the source uses names.</summary>
    public int NumericId;
    /// <summary>The payload kind, mirrored from <see cref="Value"/> for convenient inspection.</summary>
    public MetadataValueKind ValueType;
    /// <summary>The decoded, machine-readable value.</summary>
    public MetadataValue Value;
    /// <summary>The original encoded value bytes when retained.</summary>
    public byte[] RawValue;
    /// <summary>The human-readable rendering supplied by the parser, or an empty string when none is needed.</summary>
    public string DisplayValue;
    /// <summary>The zero-based source byte offset, or negative one when unavailable.</summary>
    public long Offset;
    /// <summary>The encoded source length in bytes when known.</summary>
    public long Length;
    /// <summary>The parser confidence from 0 to 100 when the tag was inferred.</summary>
    public int Confidence;
    /// <summary>The zero-based occurrence among repeated tags in the same directory.</summary>
    public int RepeatIndex;

    /// <summary>Creates an empty tag with initialized value and raw-byte fields.</summary>
    public MetadataTag()
    {
        Key = "";
        Name = "";
        Family = MetadataFamily.Unknown;
        Group = "";
        NumericId = 0;
        ValueType = MetadataValueKind.Empty;
        Value = new MetadataValue();
        RawValue = new byte[0];
        DisplayValue = "";
        Offset = -1;
        Length = 0;
        Confidence = 0;
        RepeatIndex = 0;
    }

    /// <summary>Creates a named tag with a decoded value.</summary>
    /// <param name="key">The stable query key.</param>
    /// <param name="name">The human-readable tag name.</param>
    /// <param name="family">The metadata family that defines the tag.</param>
    /// <param name="value">The decoded value whose kind is copied into <see cref="ValueType"/>.</param>
    /// <returns>A tag initialized with the supplied identity and value.</returns>
    internal static MetadataTag Create(string key, string name, MetadataFamily family, MetadataValue value)
    {
        MetadataTag tag = new MetadataTag();
        tag.Key = key;
        tag.Name = name;
        tag.Family = family;
        tag.ValueType = value.Kind;
        tag.Value = value;
        return tag;
    }
}
