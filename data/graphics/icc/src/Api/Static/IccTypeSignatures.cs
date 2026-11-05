namespace icc;

/// <summary>
/// Defines registered ICC.1, ICC.2, legacy, vendor, and amendment tag-type signatures.
/// </summary>
public static class IccTypeSignatures
{
    /// <summary>The chromaticity type <c>chrm</c>.</summary>
    public const uint Chromaticity = 0x6368726d;
    /// <summary>The CICP type <c>cicp</c>.</summary>
    public const uint Cicp = 0x63696370;
    /// <summary>The colorant-order type <c>clro</c>.</summary>
    public const uint ColorantOrder = 0x636c726f;
    /// <summary>The colorant-table type <c>clrt</c>.</summary>
    public const uint ColorantTable = 0x636c7274;
    /// <summary>The curve type <c>curv</c>.</summary>
    public const uint Curve = 0x63757276;
    /// <summary>The data type <c>data</c>.</summary>
    public const uint Data = 0x64617461;
    /// <summary>The date/time type <c>dtim</c>.</summary>
    public const uint DateTime = 0x6474696d;
    /// <summary>The dictionary type <c>dict</c>.</summary>
    public const uint Dictionary = 0x64696374;
    /// <summary>The LUT8 type <c>mft1</c>.</summary>
    public const uint Lut8 = 0x6d667431;
    /// <summary>The LUT16 type <c>mft2</c>.</summary>
    public const uint Lut16 = 0x6d667432;
    /// <summary>The LUT A-to-B type <c>mAB </c>.</summary>
    public const uint LutAToB = 0x6d414220;
    /// <summary>The LUT B-to-A type <c>mBA </c>.</summary>
    public const uint LutBToA = 0x6d424120;
    /// <summary>The measurement type <c>meas</c>.</summary>
    public const uint Measurement = 0x6d656173;
    /// <summary>The multi-localized-Unicode type <c>mluc</c>.</summary>
    public const uint MultiLocalizedUnicode = 0x6d6c7563;
    /// <summary>The multi-process-elements type <c>mpet</c>.</summary>
    public const uint MultiProcessElements = 0x6d706574;
    /// <summary>The named-colour-2 type <c>ncl2</c>.</summary>
    public const uint NamedColor2 = 0x6e636c32;
    /// <summary>The parametric-curve type <c>para</c>.</summary>
    public const uint ParametricCurve = 0x70617261;
    /// <summary>The profile-sequence-description type <c>pseq</c>.</summary>
    public const uint ProfileSequenceDescription = 0x70736571;
    /// <summary>The profile-sequence-identifier type <c>psid</c>.</summary>
    public const uint ProfileSequenceIdentifier = 0x70736964;
    /// <summary>The response-curve-set-16 type <c>rcs2</c>.</summary>
    public const uint ResponseCurveSet16 = 0x72637332;
    /// <summary>The signed 15.16 fixed-array type <c>sf32</c>.</summary>
    public const uint S15Fixed16Array = 0x73663332;
    /// <summary>The signature type <c>sig </c>.</summary>
    public const uint Signature = 0x73696720;
    /// <summary>The text type <c>text</c>.</summary>
    public const uint Text = 0x74657874;
    /// <summary>The unsigned 16.16 fixed-array type <c>uf32</c>.</summary>
    public const uint U16Fixed16Array = 0x75663332;
    /// <summary>The unsigned 8-bit integer-array type <c>ui08</c>.</summary>
    public const uint UInt8Array = 0x75693038;
    /// <summary>The unsigned 16-bit integer-array type <c>ui16</c>.</summary>
    public const uint UInt16Array = 0x75693136;
    /// <summary>The unsigned 32-bit integer-array type <c>ui32</c>.</summary>
    public const uint UInt32Array = 0x75693332;
    /// <summary>The unsigned 64-bit integer-array type <c>ui64</c>.</summary>
    public const uint UInt64Array = 0x75693634;
    /// <summary>The viewing-conditions type <c>view</c>.</summary>
    public const uint ViewingConditions = 0x76696577;
    /// <summary>The XYZ type <c>XYZ </c>.</summary>
    public const uint Xyz = 0x58595a20;
    /// <summary>The legacy text-description type <c>desc</c>.</summary>
    public const uint TextDescription = 0x64657363;
    /// <summary>The legacy CRD-information type <c>crdi</c>.</summary>
    public const uint CrdInfo = 0x63726469;
    /// <summary>The legacy named-colour type <c>ncol</c>.</summary>
    public const uint NamedColor = 0x6e636f6c;
    /// <summary>The legacy screening type <c>scrn</c>.</summary>
    public const uint Screening = 0x7363726e;
    /// <summary>The legacy UCR/BG type <c>bfd </c>.</summary>
    public const uint UcrBg = 0x62666420;
    /// <summary>The legacy device-settings type <c>devs</c>.</summary>
    public const uint DeviceSettings = 0x64657673;
    /// <summary>The ICC.1 embedded-profile type <c>ICCp</c>.</summary>
    public const uint EmbeddedProfile = 0x49434370;
    /// <summary>The 2025 adaptive-gain-curve type <c>adgc</c>.</summary>
    public const uint AdaptiveGainCurve = 0x61646763;
    /// <summary>The Apple video-card gamma-table type <c>vcgt</c>.</summary>
    public const uint VideoCardGammaTable = 0x76636774;
    /// <summary>The Microsoft hardware-calibration type <c>MHC2</c>.</summary>
    public const uint MicrosoftHardwareCalibration = 0x4d484332;
    /// <summary>The ICC.2 float16-array type <c>fl16</c>.</summary>
    public const uint Float16Array = 0x666c3136;
    /// <summary>The ICC.2 float32-array type <c>fl32</c>.</summary>
    public const uint Float32Array = 0x666c3332;
    /// <summary>The ICC.2 float64-array type <c>fl64</c>.</summary>
    public const uint Float64Array = 0x666c3634;
    /// <summary>The ICC.2 gamut-boundary-description type <c>gbd </c>.</summary>
    public const uint GamutBoundaryDescription = 0x67626420;
    /// <summary>The ICC.2 sparse-matrix-array type <c>smat</c>.</summary>
    public const uint SparseMatrixArray = 0x736d6174;
    /// <summary>The ICC.2 spectral-viewing-conditions type <c>svcn</c>.</summary>
    public const uint SpectralViewingConditions = 0x7376636e;
    /// <summary>The ICC.2 spectral-range amendment type <c>srng</c>.</summary>
    public const uint SpectralRange = 0x73726e67;
    /// <summary>The ICC.2 tag-array type <c>tary</c>.</summary>
    public const uint TagArray = 0x74617279;
    /// <summary>The ICC.2 tag-structure type <c>tstr</c>.</summary>
    public const uint TagStructure = 0x74737472;
    /// <summary>The ICC.2 UTF-8 type <c>utf8</c>.</summary>
    public const uint Utf8 = 0x75746638;
    /// <summary>The ICC.2 UTF-16 type <c>ut16</c>.</summary>
    public const uint Utf16 = 0x75743136;
    /// <summary>The ICC.2 zipped UTF-8 type <c>zut8</c>.</summary>
    public const uint ZippedUtf8 = 0x7a757438;
    /// <summary>The ICC.2 zipped XML type <c>zxml</c>.</summary>
    public const uint ZippedXml = 0x7a786d6c;

    /// <summary>Gets the alternate upper-case zipped XML type signature.</summary>
    public const uint ZippedXmlUppercase = 0x5a584d4c;
    /// <summary>The ICC.2 embedded-height-image type <c>ehim</c>.</summary>
    public const uint EmbeddedHeightImage = 0x6568696d;
    /// <summary>The ICC.2 embedded-normal-image type <c>enim</c>.</summary>
    public const uint EmbeddedNormalImage = 0x656e696d;
}
