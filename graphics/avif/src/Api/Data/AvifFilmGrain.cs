namespace avif;

/// <summary>Holds AV1 film-grain synthesis parameters for frame authoring.</summary>
public class AvifFilmGrain
{
    /// <summary>The unsigned sixteen-bit random seed.</summary>
    public int GrainSeed;

    /// <summary>Whether parameters replace rather than predict a stored model.</summary>
    public bool UpdateGrain;

    /// <summary>The stored reference index when parameters are predicted.</summary>
    public int ReferenceIndex;

    /// <summary>Luma scaling points as two-element value and scaling arrays.</summary>
    public int[][] YPoints;

    /// <summary>Blue-difference chroma scaling points.</summary>
    public int[][] CbPoints;

    /// <summary>Red-difference chroma scaling points.</summary>
    public int[][] CrPoints;

    /// <summary>Whether chroma scaling is derived from luma.</summary>
    public bool ChromaScalingFromLuma;

    /// <summary>The scaling shift from eight through eleven.</summary>
    public int ScalingShift;

    /// <summary>The autoregressive coefficient lag from zero through three.</summary>
    public int ArCoefficientLag;

    /// <summary>Luma autoregressive coefficients.</summary>
    public int[] ArCoefficientsY;

    /// <summary>Blue-difference chroma autoregressive coefficients.</summary>
    public int[] ArCoefficientsCb;

    /// <summary>Red-difference chroma autoregressive coefficients.</summary>
    public int[] ArCoefficientsCr;

    /// <summary>The autoregressive coefficient shift from six through nine.</summary>
    public int ArCoefficientShift;

    /// <summary>The grain scale shift from zero through three.</summary>
    public int GrainScaleShift;

    /// <summary>The blue-difference chroma multiplier.</summary>
    public int CbMultiplier;

    /// <summary>The blue-difference luma multiplier.</summary>
    public int CbLumaMultiplier;

    /// <summary>The blue-difference offset.</summary>
    public int CbOffset;

    /// <summary>The red-difference chroma multiplier.</summary>
    public int CrMultiplier;

    /// <summary>The red-difference luma multiplier.</summary>
    public int CrLumaMultiplier;

    /// <summary>The red-difference offset.</summary>
    public int CrOffset;

    /// <summary>Whether neighboring grain blocks overlap.</summary>
    public bool Overlap;

    /// <summary>Whether output is clipped to restricted range.</summary>
    public bool ClipToRestrictedRange;

    /// <summary>Creates an empty update model with normative default shifts.</summary>
    public AvifFilmGrain()
    {
        GrainSeed = 0;
        UpdateGrain = true;
        ReferenceIndex = 0;
        YPoints = System.Array.Empty<int[]>();
        CbPoints = System.Array.Empty<int[]>();
        CrPoints = System.Array.Empty<int[]>();
        ScalingShift = 8;
        ArCoefficientLag = 0;
        ArCoefficientsY = System.Array.Empty<int>();
        ArCoefficientsCb = System.Array.Empty<int>();
        ArCoefficientsCr = System.Array.Empty<int>();
        ArCoefficientShift = 6;
        GrainScaleShift = 0;
    }
}
