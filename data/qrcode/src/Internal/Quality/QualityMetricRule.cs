namespace qrcode;

internal class QualityMetricRule
{
    internal string Name = "";
    internal string? Unit;
    internal double MinimumValue;
    internal double MaximumValue = 1;
    // Inclusive boundaries, ordered from grade 4 down to grade 1.
    // Equal boundaries also support pass/fail metrics (grades 4 and 0).
    internal double[] Boundaries = new double[0];
    internal bool HigherIsBetter = true;
    internal bool RequiresCalibratedInputs = true;
    internal bool RequiredForOverall = true;
}
