namespace icc;

/// <summary>
/// Defines standard ICC reference colorimetry and Bradford adaptation constants.
/// </summary>
public static class IccStandardColorimetry
{
    /// <summary>The ICC D50 reference-white X tristimulus value.</summary>
    public const double D50X = 0.9642;
    /// <summary>The ICC D50 reference-white Y tristimulus value.</summary>
    public const double D50Y = 1.0;
    /// <summary>The ICC D50 reference-white Z tristimulus value.</summary>
    public const double D50Z = 0.8249;
    /// <summary>The ICC D50 reference-white x chromaticity.</summary>
    public const double D50ChromaticityX = 0.3457;
    /// <summary>The ICC D50 reference-white y chromaticity.</summary>
    public const double D50ChromaticityY = 0.3585;
    /// <summary>The encoded s15Fixed16 D50 X value.</summary>
    public const int D50EncodedX = 0x0000f6d6;
    /// <summary>The encoded s15Fixed16 D50 Y value.</summary>
    public const int D50EncodedY = 0x00010000;
    /// <summary>The encoded s15Fixed16 D50 Z value.</summary>
    public const int D50EncodedZ = 0x0000d32d;
    /// <summary>The normalized CIE D65 X tristimulus value.</summary>
    public const double D65X = 0.9504559270516716;
    /// <summary>The normalized CIE D65 Y tristimulus value.</summary>
    public const double D65Y = 1.0;
    /// <summary>The normalized CIE D65 Z tristimulus value.</summary>
    public const double D65Z = 1.0890577507598784;
    /// <summary>The CIE D65 x chromaticity.</summary>
    public const double D65ChromaticityX = 0.3127;
    /// <summary>The CIE D65 y chromaticity.</summary>
    public const double D65ChromaticityY = 0.3290;
    /// <summary>The CIE Lab epsilon constant.</summary>
    public const double LabEpsilon = 0.008856451679035631;
    /// <summary>The CIE Lab kappa constant.</summary>
    public const double LabKappa = 903.2962962962963;
    /// <summary>The perceptual reference medium black X value.</summary>
    public const double PerceptualBlackX = 0.00336;
    /// <summary>The perceptual reference medium black Y value.</summary>
    public const double PerceptualBlackY = 0.0034731;
    /// <summary>The perceptual reference medium black Z value.</summary>
    public const double PerceptualBlackZ = 0.00287;
    /// <summary>The Bradford matrix row-zero column-zero value.</summary>
    public const double Bradford00 = 0.8951;
    /// <summary>The Bradford matrix row-zero column-one value.</summary>
    public const double Bradford01 = 0.2664;
    /// <summary>The Bradford matrix row-zero column-two value.</summary>
    public const double Bradford02 = -0.1614;
    /// <summary>The Bradford matrix row-one column-zero value.</summary>
    public const double Bradford10 = -0.7502;
    /// <summary>The Bradford matrix row-one column-one value.</summary>
    public const double Bradford11 = 1.7135;
    /// <summary>The Bradford matrix row-one column-two value.</summary>
    public const double Bradford12 = 0.0367;
    /// <summary>The Bradford matrix row-two column-zero value.</summary>
    public const double Bradford20 = 0.0389;
    /// <summary>The Bradford matrix row-two column-one value.</summary>
    public const double Bradford21 = -0.0685;
    /// <summary>The Bradford matrix row-two column-two value.</summary>
    public const double Bradford22 = 1.0296;
}
