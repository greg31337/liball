namespace icc;

/// <summary>Represents decoded inspection metadata from a Microsoft <c>MHC2</c> payload.</summary>
public class IccMicrosoftHardwareCalibration
{
    /// <summary>Gets or sets the common number of entries in each one-dimensional lookup table.</summary>
    public int LutEntryCount;
    /// <summary>Gets or sets the ST.2086 minimum luminance in nits.</summary>
    public double MinimumLuminance;
    /// <summary>Gets or sets the ST.2086 peak luminance in nits.</summary>
    public double PeakLuminance;
    /// <summary>Gets or sets the row-major 3-by-4 XYZ adjustment matrix, or an empty array for identity.</summary>
    public double[] Matrix;
    /// <summary>Gets or sets the red calibration lookup table, or an empty array for identity.</summary>
    public double[] RedLut;
    /// <summary>Gets or sets the green calibration lookup table, or an empty array for identity.</summary>
    public double[] GreenLut;
    /// <summary>Gets or sets the blue calibration lookup table, or an empty array for identity.</summary>
    public double[] BlueLut;

    /// <summary>Initializes empty Microsoft hardware-calibration metadata.</summary>
    public IccMicrosoftHardwareCalibration()
    {
        LutEntryCount = 0;
        MinimumLuminance = 0.0;
        PeakLuminance = 0.0;
        Matrix = new double[0];
        RedLut = new double[0];
        GreenLut = new double[0];
        BlueLut = new double[0];
    }
}
