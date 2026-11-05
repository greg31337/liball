namespace molecule;


/// <summary>
/// Controls deterministic two-dimensional coordinate normalization.
/// </summary>
public class CoordinateNormalizationOptions
{
    /// <summary>
    /// The scale policy applied after centering and principal-axis rotation.
    /// </summary>
    public CoordinateNormalizationScalePolicy ScalePolicy;
    /// <summary>
    /// The requested mean bond length when <see cref="CoordinateNormalizationScalePolicy.AverageBondLength"/> is selected.
    /// </summary>
    public double TargetAverageBondLength;

    /// <summary>
    /// Initializes options that preserve scale while centering and rotating coordinates.
    /// </summary>
    public CoordinateNormalizationOptions()
    {
        ScalePolicy = CoordinateNormalizationScalePolicy.Preserve;
        TargetAverageBondLength = 1.5;
    }
}
