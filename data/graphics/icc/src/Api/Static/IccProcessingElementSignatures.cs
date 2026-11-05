namespace icc;

/// <summary>
/// Defines ICC.1 and ICC.2 multi-process element signatures.
/// </summary>
public static class IccProcessingElementSignatures
{
    /// <summary>The curve-set element <c>cvst</c>.</summary>
    public const uint CurveSet = 0x63767374;
    /// <summary>The matrix element <c>matf</c>.</summary>
    public const uint Matrix = 0x6d617466;
    /// <summary>The CLUT element <c>clut</c>.</summary>
    public const uint Clut = 0x636c7574;
    /// <summary>The begin ACS element <c>bACS</c>.</summary>
    public const uint BeginAcs = 0x62414353;
    /// <summary>The end ACS element <c>eACS</c>.</summary>
    public const uint EndAcs = 0x65414353;
    /// <summary>The ICC.2 calculator element <c>calc</c>.</summary>
    public const uint Calculator = 0x63616c63;
    /// <summary>The ICC.2 extended CLUT element <c>xclt</c>.</summary>
    public const uint ExtendedClut = 0x78636c74;
    /// <summary>The ICC.2 sparse-matrix element <c>smet</c>.</summary>
    public const uint SparseMatrix = 0x736d6574;
    /// <summary>The ICC.2 tint-array element <c>tint</c>.</summary>
    public const uint TintArray = 0x74696e74;
    /// <summary>The ICC.2 tone-map element <c>tmap</c>.</summary>
    public const uint ToneMap = 0x746d6170;
    /// <summary>The ICC.2 XYZ-to-Jab element <c>XtoJ</c>.</summary>
    public const uint XyzToJab = 0x58746f4a;
    /// <summary>The ICC.2 Jab-to-XYZ element <c>JtoX</c>.</summary>
    public const uint JabToXyz = 0x4a746f58;
    /// <summary>The ICC.2 emission CLUT element <c>eclt</c>.</summary>
    public const uint EmissionClut = 0x65636c74;
    /// <summary>The ICC.2 emission matrix element <c>emtx</c>.</summary>
    public const uint EmissionMatrix = 0x656d7478;
    /// <summary>The ICC.2 emission observer element <c>eobs</c>.</summary>
    public const uint EmissionObserver = 0x656f6273;
    /// <summary>The ICC.2 inverse emission matrix element <c>iemx</c>.</summary>
    public const uint InverseEmissionMatrix = 0x69656d78;
    /// <summary>The ICC.2 reflectance CLUT element <c>rclt</c>.</summary>
    public const uint ReflectanceClut = 0x72636c74;
    /// <summary>The ICC.2 reflectance observer element <c>robs</c>.</summary>
    public const uint ReflectanceObserver = 0x726f6273;
}
