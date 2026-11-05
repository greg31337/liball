namespace icc;

/// <summary>Represents one adaptive-gain-curve interpolation node.</summary>
public class IccAdaptiveGainCurveNode
{
    /// <summary>Gets or sets the normalized input coordinate.</summary>
    public double Input;
    /// <summary>Gets or sets the gain output.</summary>
    public double Gain;
    /// <summary>Gets or sets the Hermite slope.</summary>
    public double Slope;

    /// <summary>Initializes a zero node.</summary>
    public IccAdaptiveGainCurveNode()
    {
        Input = 0.0;
        Gain = 0.0;
        Slope = 0.0;
    }
}
