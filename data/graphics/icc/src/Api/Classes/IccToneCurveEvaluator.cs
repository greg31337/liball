namespace icc;

/// <summary>Evaluates, inverts, samples, and analyzes tone curves.</summary>
public class IccToneCurveEvaluator : IIccToneCurveEvaluator
{
    internal ServiceAvailability Core;

    /// <summary>Initializes a tone-curve evaluator.</summary>
    public IccToneCurveEvaluator()
    {
        Core = new ServiceAvailability("Tone-curve evaluation");
    }

    /// <summary>Evaluates an ICC tone curve.</summary>
    /// <param name="curve">The curve to evaluate.</param>
    /// <param name="input">The finite input coordinate.</param>
    /// <returns>The evaluated value.</returns>
    public double EvaluateCurve(IccCurve curve, double input)
    {
        return CurveMath.EvaluateCurve(curve, input);
    }

    /// <summary>Evaluates all three channels of an adaptive gain curve.</summary>
    /// <param name="curve">The adaptive gain curve.</param>
    /// <param name="inputs">The red, green, and blue inputs.</param>
    /// <returns>The red, green, and blue gains.</returns>
    public double[] EvaluateAdaptiveGainCurve(IccAdaptiveGainCurve curve, double[] inputs)
    {
        return CurveMath.EvaluateAdaptiveGainCurve(curve, inputs);
    }

    /// <summary>Attempts to invert an ICC curve.</summary>
    /// <param name="curve">The curve to invert.</param>
    /// <param name="output">The finite output value.</param>
    /// <returns>The inverse input and operation outcome.</returns>
    public IccCurveInversionResult InvertCurve(IccCurve curve, double output)
    {
        return CurveInverter.InvertCurve(curve, output);
    }

    /// <summary>Samples a curve evenly across its operational domain.</summary>
    /// <param name="curve">The curve to sample.</param>
    /// <param name="sampleCount">The sample count.</param>
    /// <returns>The sampled values.</returns>
    public double[] SampleCurve(IccCurve curve, int sampleCount)
    {
        return CurveAnalyzer.SampleCurve(curve, sampleCount);
    }

    /// <summary>Creates a sampled approximation of a curve.</summary>
    /// <param name="curve">The source curve.</param>
    /// <param name="sampleCount">The sample count.</param>
    /// <returns>The independent sampled curve.</returns>
    public IccCurve ResampleCurve(IccCurve curve, int sampleCount)
    {
        return CurveAnalyzer.ResampleCurve(curve, sampleCount);
    }

    /// <summary>Evaluates a curve's local slope.</summary>
    /// <param name="curve">The curve to differentiate.</param>
    /// <param name="input">The finite input coordinate.</param>
    /// <returns>The local slope.</returns>
    public double EvaluateCurveSlope(IccCurve curve, double input)
    {
        return CurveAnalyzer.EvaluateCurveSlope(curve, input);
    }

    /// <summary>Analyzes a curve over its operational domain.</summary>
    /// <param name="curve">The curve to analyze.</param>
    /// <param name="sampleCount">The analysis sample count.</param>
    /// <returns>The curve analysis.</returns>
    public IccCurveAnalysis AnalyzeCurve(IccCurve curve, int sampleCount)
    {
        return CurveAnalyzer.AnalyzeCurve(curve, sampleCount);
    }

    /// <summary>Evaluates a sampled curve.</summary>
    /// <param name="samples">The curve samples.</param>
    /// <param name="input">The normalized input.</param>
    /// <returns>The evaluated value.</returns>
    public double EvaluateSampledCurve(double[] samples, double input)
    {
        return CurveMath.EvaluateSampledCurve(samples, input);
    }

    /// <summary>Inverts a sampled curve.</summary>
    /// <param name="samples">The curve samples.</param>
    /// <param name="output">The normalized output.</param>
    /// <returns>The corresponding input.</returns>
    public double InvertSampledCurve(double[] samples, double output)
    {
        return CurveInverter.InvertSampledCurve(samples, output);
    }

    /// <summary>Samples a gamma curve.</summary>
    /// <param name="gamma">The gamma exponent.</param>
    /// <param name="sampleCount">The sample count.</param>
    /// <returns>The samples.</returns>
    public double[] SampleGammaCurve(double gamma, int sampleCount)
    {
        return CurveAnalyzer.SampleGammaCurve(gamma, sampleCount);
    }

    /// <summary>Analyzes a sampled curve.</summary>
    /// <param name="samples">The curve samples.</param>
    /// <returns>The analysis result.</returns>
    public IccOperationResult AnalyzeSampledCurve(double[] samples)
    {
        return CurveAnalyzer.AnalyzeSampledCurve(samples);
    }
}
