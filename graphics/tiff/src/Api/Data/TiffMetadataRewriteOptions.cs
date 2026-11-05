namespace tiff;

/// <summary>Controls metadata changes and encoded image-data pass-through during an image rewrite.</summary>
/// <remarks>Updates replace copied metadata values. Removals are applied after updates. Structural image-layout tags cannot be changed through this model.</remarks>
public class TiffMetadataRewriteOptions
{
    /// <summary>Contains metadata values to add or replace.</summary>
    public TiffTagValue[] Updates;
    /// <summary>Contains metadata tag identifiers to remove after applying updates.</summary>
    public int[] RemovedTagIds;
    /// <summary>Indicates whether unregistered metadata tags are copied exactly.</summary>
    public bool PreserveUnknownMetadata;
    /// <summary>Indicates whether encoded strips or tiles must be copied without decoding.</summary>
    public bool PassThroughRawImageData;

    /// <summary>Creates rewrite options that preserve unknown metadata and pass through encoded image data.</summary>
    public TiffMetadataRewriteOptions()
    {
        Updates = new TiffTagValue[0];
        RemovedTagIds = new int[0];
        PreserveUnknownMetadata = true;
        PassThroughRawImageData = true;
    }

    internal TiffMetadataRewriteOptions Copy()
    {
        TiffMetadataRewriteOptions copy = new TiffMetadataRewriteOptions();
        copy.Updates = TiffImageDefinition.CopyTags(Updates);
        copy.RemovedTagIds = CopyIntegers(RemovedTagIds);
        copy.PreserveUnknownMetadata = PreserveUnknownMetadata;
        copy.PassThroughRawImageData = PassThroughRawImageData;
        return copy;
    }

    internal static int[] CopyIntegers(int[] values)
    {
        if (values == null)
        {
            return new int[0];
        }

        int[] copy = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }
}
