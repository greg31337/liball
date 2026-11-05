namespace image;

/// <summary>Controls the algorithm and metadata ranges selected for metadata-only hashing.</summary>
public class ImageMetadataHashOptions
{
    /// <summary>The algorithm name: <c>SHA-256</c>, <c>SHA-1</c>, or <c>MD5</c>.</summary>
    public string AlgorithmName;
    /// <summary>Indicates whether metadata directory and tag ranges are included.</summary>
    public bool IncludeDirectories;
    /// <summary>Indicates whether preserved profile ranges are included.</summary>
    public bool IncludeProfiles;
    /// <summary>Indicates whether recognized raw metadata block ranges are included.</summary>
    public bool IncludeRawBlocks;
    /// <summary>The accepted metadata families, or an empty array to accept every family.</summary>
    public MetadataFamily[] Families;
    /// <summary>The accepted profile kinds, or an empty array to accept every non-image profile kind.</summary>
    public MetadataProfileKind[] ProfileKinds;
    /// <summary>The accepted raw block types, or an empty array to accept every metadata-bearing block type.</summary>
    public string[] RawBlockTypes;

    /// <summary>Creates a SHA-256 selection containing all safely separable metadata categories.</summary>
    public ImageMetadataHashOptions()
    {
        AlgorithmName = "SHA-256";
        IncludeDirectories = true;
        IncludeProfiles = true;
        IncludeRawBlocks = true;
        Families = new MetadataFamily[0];
        ProfileKinds = new MetadataProfileKind[0];
        RawBlockTypes = new string[0];
    }
}
