namespace icc;

/// <summary>
/// Defines colorimetric-intent image-state signatures.
/// </summary>
public static class IccImageStateSignatures
{
    /// <summary>The scene-colorimetry-estimates signature <c>scoe</c>.</summary>
    public const uint SceneColorimetryEstimates = 0x73636f65;
    /// <summary>The scene-appearance-estimates signature <c>sape</c>.</summary>
    public const uint SceneAppearanceEstimates = 0x73617065;
    /// <summary>The focal-plane-colorimetry-estimates signature <c>fpce</c>.</summary>
    public const uint FocalPlaneColorimetryEstimates = 0x66706365;
    /// <summary>The reflection-hardcopy-original-colorimetry signature <c>rhoc</c>.</summary>
    public const uint ReflectionHardcopyOriginalColorimetry = 0x72686f63;
    /// <summary>The reflection-print-output-colorimetry signature <c>rpoc</c>.</summary>
    public const uint ReflectionPrintOutputColorimetry = 0x72706f63;
}
