namespace jpeg;

/// <summary>Controls lossless JPEG coefficient transforms, crop resolution, and metadata retention.</summary>
public class JpegTransformOptions
{
    /// <summary>Creates options that require perfect transforms, exact MCU crops, and preserve all metadata.</summary>
    public JpegTransformOptions()
    {
    }

    /// <summary>Rejects rotations, flips, and transpositions that would move partial edge MCUs.</summary>
    public bool RequirePerfectTransform = true;
    /// <summary>Selects how crop edges that are not MCU-aligned are resolved.</summary>
    public JpegMcuCropMode McuCropMode = JpegMcuCropMode.Exact;
    /// <summary>Selects metadata retained in the transformed output.</summary>
    public JpegMetadataPolicy MetadataPolicy = JpegMetadataPolicy.PreserveAll;
}
