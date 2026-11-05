using fake;

namespace fake.Tests.Core;

[TestFixture]
public class GuardTests
{
    [Test]
    public void DefaultLimitsArePositiveAndOutputLimitIsLarger()
    {
        Assert.That(FakeLimits.DefaultMaximumCount, Is.GreaterThan(0));
        Assert.That(FakeLimits.DefaultMaximumOutputSize, Is.GreaterThan(FakeLimits.DefaultMaximumCount));
    }

    [Test]
    public void NotNullRejectsNullWithParameterName()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Guard.NotNull(null, "value"));

        Assert.That(exception.ParamName, Is.EqualTo("value"));
        Assert.That(exception.Message, Does.StartWith("value cannot be null."));
    }

    [Test]
    public void NotNullAcceptsValue()
    {
        Assert.DoesNotThrow(() => Guard.NotNull(new object(), "value"));
    }

    [TestCase(null, typeof(ArgumentNullException), "text cannot be null.")]
    [TestCase("", typeof(ArgumentException), "text cannot be empty.")]
    public void NotNullOrEmptyRejectsInvalidValues(string? value, Type exceptionType, string message)
    {
        Exception exception = Assert.Throws(exceptionType, () => Guard.NotNullOrEmpty(value, "text"));

        Assert.That(exception.Message, Does.StartWith(message));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t\r\n")]
    public void NotNullOrWhiteSpaceRejectsBlankValues(string value)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Guard.NotNullOrWhiteSpace(value, "text"));

        Assert.That(exception.ParamName, Is.EqualTo("text"));
        Assert.That(exception.Message, Does.StartWith("text cannot be empty or whitespace."));
    }

    [Test]
    public void GuardsRejectInvalidParameterNamesConsistently()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Guard.NotNull(new object(), " "));

        Assert.That(exception.ParamName, Is.EqualTo("parameterName"));
        Assert.That(exception.Message, Does.StartWith("Parameter names cannot be null, empty, or whitespace."));
    }

    [TestCase(0)]
    [TestCase(1)]
    public void NonNegativeAcceptsBoundaryAndPositiveValues(int value)
    {
        Assert.DoesNotThrow(() => Guard.NonNegative(value, "value"));
    }

    [Test]
    public void NonNegativeRejectsNegativeValue()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.NonNegative(-1, "value"));

        Assert.That(exception.ParamName, Is.EqualTo("value"));
        Assert.That(exception.ActualValue, Is.EqualTo(-1));
        Assert.That(exception.Message, Does.StartWith("value must be zero or greater."));
    }

    [TestCase(1)]
    [TestCase(100)]
    public void PositiveAcceptsPositiveValues(int value)
    {
        Assert.DoesNotThrow(() => Guard.Positive(value, "value"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void PositiveRejectsNonPositiveValues(int value)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Positive(value, "value"));

        Assert.That(exception.Message, Does.StartWith("value must be greater than zero."));
    }

    [TestCase(0)]
    [TestCase(10)]
    public void CountAcceptsInclusiveBoundaries(int value)
    {
        Assert.DoesNotThrow(() => Guard.Count(value, 10, "count"));
    }

    [TestCase(-1)]
    [TestCase(11)]
    public void CountRejectsValuesOutsideConfiguredMaximum(int value)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Count(value, 10, "count"));

        Assert.That(exception.ParamName, Is.EqualTo("count"));
        Assert.That(exception.ActualValue, Is.EqualTo(value));
        Assert.That(exception.Message, Does.StartWith("count must be between 0 and 10, inclusive."));
    }

    [Test]
    public void CountRejectsInvalidMaximum()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Count(0, -1, "count"));

        Assert.That(exception.ParamName, Is.EqualTo("maximum"));
    }

    [TestCase(0L)]
    [TestCase(12L)]
    public void OutputSizeAcceptsInclusiveBoundaries(long requestedSize)
    {
        int result = Guard.OutputSize(requestedSize, 12, "length");

        Assert.That(result, Is.EqualTo((int)requestedSize));
    }

    [Test]
    public void OutputSizeRejectsConfiguredLimitViolation()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.OutputSize(13, 12, "length"));

        Assert.That(exception.ParamName, Is.EqualTo("length"));
        Assert.That(exception.ActualValue, Is.EqualTo(13));
        Assert.That(exception.Message, Does.StartWith("length would produce 13 output units, exceeding the configured maximum of 12."));
    }

    [Test]
    public void OutputSizeRejectsNegativeRequestedSize()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.OutputSize(-1, 12, "length"));

        Assert.That(exception.ParamName, Is.EqualTo("length"));
        Assert.That(exception.Message, Does.StartWith("length must be zero or greater."));
    }

    [Test]
    public void OutputProductCalculatesWithinLimit()
    {
        int result = Guard.OutputSize(3, 4, 12, "count", "itemSize");

        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void OutputProductRejectsLimitWithoutIntegerOverflow()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.OutputSize(int.MaxValue, int.MaxValue, 100, "count", "itemSize"));

        Assert.That(exception.ParamName, Is.EqualTo("count"));
        Assert.That(exception.ActualValue, Is.EqualTo(4611686014132420609L));
        Assert.That(exception.Message, Does.StartWith("count would produce 4611686014132420609 output units, exceeding the configured maximum of 100."));
    }

    [Test]
    public void ExhaustedThrowsStableLibraryException()
    {
        FakeException exception = Assert.Throws<FakeException>(() => Guard.Exhausted("unique email", 7));

        Assert.That(exception.Message, Is.EqualTo("Generation operation 'unique email' exhausted its retry limit after 7 attempts."));
    }

    [Test]
    public void ExhaustedRejectsNullOperation()
    {
        Assert.Throws<ArgumentNullException>(() => Guard.Exhausted(null, 1));
    }

    [TestCase("")]
    [TestCase(" ")]
    public void ExhaustedRejectsBlankOperation(string operation)
    {
        Assert.Throws<ArgumentException>(() => Guard.Exhausted(operation, 1));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void ExhaustedRequiresPositiveAttemptCount(int attempts)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Exhausted("unique email", attempts));

        Assert.That(exception.ParamName, Is.EqualTo("attempts"));
    }
}
