namespace icc;

/// <summary>Represents one response-curve-set measurement unit.</summary>
public class IccResponseCurve
{
    /// <summary>Gets or sets the measurement-unit signature.</summary>
    public uint MeasurementUnitSignature;
    /// <summary>Gets or sets maximum-colorant XYZ values by channel.</summary>
    public IccXyzNumber[] MaximumColorantXyz;
    /// <summary>Gets or sets normalized device codes.</summary>
    public ushort[][] DeviceCodes;
    /// <summary>Gets or sets measurement values corresponding to device codes.</summary>
    public double[][] Measurements;

    /// <summary>Initializes an empty response curve.</summary>
    public IccResponseCurve()
    {
        MeasurementUnitSignature = 0;
        MaximumColorantXyz = new IccXyzNumber[0];
        DeviceCodes = new ushort[0][];
        Measurements = new double[0][];
    }
}
