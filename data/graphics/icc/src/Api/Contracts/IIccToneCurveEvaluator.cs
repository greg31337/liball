namespace icc;

/// <summary>
/// Evaluates, inverts, samples, and checks ICC tone curves.
/// </summary>
public interface IIccToneCurveEvaluator
{
    /// <summary>
    /// Evaluates an identity, gamma, sampled, parametric, segmented, or single-sampled ICC curve.
    /// </summary>
    /// <remarks>
    /// Normalized ICC.1 curves clip inputs to their zero-to-one endpoints. Single-sampled curves use their
    /// declared clipping or linear-extension policy. Segmented formula end segments provide their own extrapolation.
    /// </remarks>
    /// <param name="curve">The curve representation to evaluate.</param>
    /// <param name="input">The finite input coordinate.</param>
    /// <returns>The evaluated curve value.</returns>
    double EvaluateCurve(IccCurve curve, double input);

    /// <summary>
    /// Evaluates the red, green, and blue channels of an adaptive gain curve.
    /// </summary>
    /// <remarks>Inputs outside a channel's node domain use that channel's nearest endpoint gain.</remarks>
    /// <param name="curve">The adaptive gain curve.</param>
    /// <param name="inputs">The three finite red, green, and blue input coordinates.</param>
    /// <returns>The three interpolated gain values.</returns>
    double[] EvaluateAdaptiveGainCurve(IccAdaptiveGainCurve curve, double[] inputs);

    /// <summary>
    /// Attempts to invert a monotonic ICC curve at one output value.
    /// </summary>
    /// <remarks>
    /// Duplicate-output plateaus map to the midpoint of their input interval. Constant and non-monotonic curves
    /// return an explicit unsuccessful result instead of an arbitrary input.
    /// </remarks>
    /// <param name="curve">The curve to invert.</param>
    /// <param name="output">The finite output value.</param>
    /// <returns>The inverse input and operation outcome.</returns>
    IccCurveInversionResult InvertCurve(IccCurve curve, double output);

    /// <summary>Samples a curve evenly across its operational input domain.</summary>
    /// <param name="curve">The curve to sample.</param>
    /// <param name="sampleCount">The number of samples, including both endpoints.</param>
    /// <returns>The evaluated sample values.</returns>
    double[] SampleCurve(IccCurve curve, int sampleCount);

    /// <summary>Creates an independent sampled approximation of a curve.</summary>
    /// <param name="curve">The source curve.</param>
    /// <param name="sampleCount">The number of samples, including both endpoints.</param>
    /// <returns>A sampled curve preserving an explicit single-sampled domain when present.</returns>
    IccCurve ResampleCurve(IccCurve curve, int sampleCount);

    /// <summary>Evaluates the local slope of a curve.</summary>
    /// <remarks>At an internal sampled knot, the returned slope is the mean of its adjoining linear segments.</remarks>
    /// <param name="curve">The curve to differentiate.</param>
    /// <param name="input">The finite input coordinate.</param>
    /// <returns>The local output slope.</returns>
    double EvaluateCurveSlope(IccCurve curve, double input);

    /// <summary>Analyzes a curve over its operational input domain.</summary>
    /// <param name="curve">The curve to analyze.</param>
    /// <param name="sampleCount">The number of analysis samples, including both endpoints.</param>
    /// <returns>Monotonicity, continuity, domain, range, slope, and round-trip statistics.</returns>
    IccCurveAnalysis AnalyzeCurve(IccCurve curve, int sampleCount);

    /// <summary>
    /// Evaluates a sampled curve using one-dimensional interpolation.
    /// </summary>
    /// <remarks>Inputs outside the normalized domain use the nearest endpoint sample.</remarks>
    /// <param name="samples">The ordered curve samples.</param>
    /// <param name="input">The normalized input value.</param>
    /// <returns>The evaluated output value.</returns>
    double EvaluateSampledCurve(double[] samples, double input);

    /// <summary>
    /// Inverts a monotonic sampled curve.
    /// </summary>
    /// <remarks>Duplicate-output plateaus map to the midpoint of their input interval.</remarks>
    /// <param name="samples">The ordered monotonic curve samples.</param>
    /// <param name="output">The normalized output value.</param>
    /// <returns>The corresponding normalized input value.</returns>
    double InvertSampledCurve(double[] samples, double output);

    /// <summary>
    /// Produces evenly spaced samples of a gamma curve.
    /// </summary>
    /// <param name="gamma">The gamma exponent.</param>
    /// <param name="sampleCount">The requested sample count.</param>
    /// <returns>The sampled curve values.</returns>
    double[] SampleGammaCurve(double gamma, int sampleCount);

    /// <summary>
    /// Checks whether a sampled curve can be safely inverted.
    /// </summary>
    /// <param name="samples">The ordered curve samples.</param>
    /// <returns>The analysis outcome.</returns>
    IccOperationResult AnalyzeSampledCurve(double[] samples);
}
