namespace random.Tests;

public class GuardTests
{
    [Test]
    public void GeneralArgumentGuardsAcceptValidValues()
    {
        Assert.DoesNotThrow(() => Guard.Argument(true, "value", "invalid"));
        Assert.DoesNotThrow(() => Guard.NotNull(new object(), "value"));
        Assert.DoesNotThrow(() => Guard.NotEmpty("value", "value"));
        Assert.DoesNotThrow(() => Guard.Positive(1, "value"));
        Assert.DoesNotThrow(() => Guard.NonNegative(0, "value"));
    }

    [Test]
    public void GeneralArgumentGuardsIdentifyInvalidArgument()
    {
        AssertArgument("condition", () => Guard.Argument(false, "condition", "invalid"));
        AssertArgument("value", () => Guard.NotNull(null, "value"));
        AssertArgument("text", () => Guard.NotEmpty("", "text"));
        AssertArgument("positive", () => Guard.Positive(0, "positive"));
        AssertArgument("nonNegative", () => Guard.NonNegative(-1, "nonNegative"));
    }

    [Test]
    public void SpanLengthGuardsCoverMinimumAndExactLengths()
    {
        Assert.DoesNotThrow(() => Guard.SpanLength(5, 0, "destination"));
        Assert.DoesNotThrow(() => Guard.SpanLength(5, 5, "destination"));
        Assert.DoesNotThrow(() => Guard.ExactSpanLength(5, 5, "destination"));

        AssertArgument("destination", () => Guard.SpanLength(4, 5, "destination"));
        AssertArgument("destination", () => Guard.SpanLength(4, -1, "destination"));
        AssertArgument("destination", () => Guard.ExactSpanLength(6, 5, "destination"));
        AssertArgument("destination", () => Guard.ExactSpanLength(0, -1, "destination"));
    }

    [Test]
    public void FiniteGuardsRejectEveryNonFiniteCategory()
    {
        Assert.DoesNotThrow(() => Guard.Finite(double.MaxValue, "value"));
        Assert.DoesNotThrow(() => Guard.Finite(float.MinValue, "value"));

        AssertArgument("doubleValue", () => Guard.Finite(double.NaN, "doubleValue"));
        AssertArgument("doubleValue", () => Guard.Finite(double.PositiveInfinity, "doubleValue"));
        AssertArgument("doubleValue", () => Guard.Finite(double.NegativeInfinity, "doubleValue"));
        AssertArgument("floatValue", () => Guard.Finite(float.NaN, "floatValue"));
        AssertArgument("floatValue", () => Guard.Finite(float.PositiveInfinity, "floatValue"));
        AssertArgument("floatValue", () => Guard.Finite(float.NegativeInfinity, "floatValue"));
    }

    [Test]
    public void ProbabilityGuardsIncludeBothEndpoints()
    {
        Assert.DoesNotThrow(() => Guard.Probability(0.0, "probability"));
        Assert.DoesNotThrow(() => Guard.Probability(1.0, "probability"));
        Assert.DoesNotThrow(() => Guard.Probability(0.5f, "probability"));

        AssertArgument("probability", () => Guard.Probability(-double.Epsilon, "probability"));
        AssertArgument("probability", () => Guard.Probability(double.BitIncrement(1.0), "probability"));
        AssertArgument("probability", () => Guard.Probability(double.NaN, "probability"));
        AssertArgument("probability", () => Guard.Probability(float.PositiveInfinity, "probability"));
    }

    [Test]
    public void CountGuardsDistinguishEmptyFromPositiveDomains()
    {
        Assert.DoesNotThrow(() => Guard.Count(0, "count"));
        Assert.DoesNotThrow(() => Guard.PositiveCount(1, "dimension"));

        AssertArgument("count", () => Guard.Count(-1, "count"));
        AssertArgument("dimension", () => Guard.PositiveCount(0, "dimension"));
    }

    [Test]
    public void MatrixShapeValidatesDimensionsOverflowAndStorage()
    {
        Assert.That(Guard.MatrixElementCount(3, 4, "rows", "columns"), Is.EqualTo(12));
        Assert.DoesNotThrow(() => Guard.MatrixShape(12, 3, 4, "matrix", "rows", "columns"));

        AssertArgument("rows", () => Guard.MatrixElementCount(0, 4, "rows", "columns"));
        AssertArgument("columns", () => Guard.MatrixElementCount(3, 0, "rows", "columns"));
        AssertArgument("rows", () => Guard.MatrixElementCount(int.MaxValue, 2, "rows", "columns"));
        AssertArgument("matrix", () => Guard.MatrixShape(11, 3, 4, "matrix", "rows", "columns"));
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
