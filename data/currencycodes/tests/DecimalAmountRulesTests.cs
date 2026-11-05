namespace currencycodes.Tests;

public class DecimalAmountRulesTests
{
    [Test]
    public void EffectiveScaleRemovesOnlyInsignificantTrailingZeroes()
    {
        decimal twoDigitsWithTrailingZeroes = new decimal(12300, 0, 0, false, 4);
        decimal negativeTwoDigitsWithTrailingZeroes = new decimal(1200, 0, 0, true, 4);
        decimal scaledZero = new decimal(0, 0, 0, false, 28);
        decimal fullPrecision = new decimal(-1, -1, -1, false, 28);

        Assert.Multiple(() =>
        {
            Assert.That(DecimalAmountRules.GetEffectiveScale(123m), Is.Zero);
            Assert.That(
                DecimalAmountRules.GetEffectiveScale(twoDigitsWithTrailingZeroes),
                Is.EqualTo(2));
            Assert.That(
                DecimalAmountRules.GetEffectiveScale(negativeTwoDigitsWithTrailingZeroes),
                Is.EqualTo(2));
            Assert.That(DecimalAmountRules.GetEffectiveScale(scaledZero), Is.Zero);
            Assert.That(DecimalAmountRules.GetEffectiveScale(fullPrecision), Is.EqualTo(28));
        });
    }

    [Test]
    public void AllowedScaleChecksExactRepresentabilityWithoutRounding()
    {
        decimal exactWithStoredScale = new decimal(12300, 0, 0, false, 4);
        decimal inexact = 1.234m;

        Assert.Multiple(() =>
        {
            Assert.That(DecimalAmountRules.HasAllowedScale(123m, 0), Is.True);
            Assert.That(DecimalAmountRules.HasAllowedScale(123.45m, 2), Is.True);
            Assert.That(DecimalAmountRules.HasAllowedScale(exactWithStoredScale, 2), Is.True);
            Assert.That(DecimalAmountRules.HasAllowedScale(exactWithStoredScale, 1), Is.False);
            Assert.That(DecimalAmountRules.HasAllowedScale(inexact, 2), Is.False);
            Assert.That(inexact, Is.EqualTo(1.234m));
        });
    }

    [Test]
    public void AllowedScaleHandlesSignsZeroAndDecimalBoundaries()
    {
        decimal smallestPositive = new decimal(1, 0, 0, false, 28);
        decimal smallestNegative = new decimal(1, 0, 0, true, 28);

        Assert.Multiple(() =>
        {
            Assert.That(DecimalAmountRules.HasAllowedScale(0m, 0), Is.True);
            Assert.That(DecimalAmountRules.HasAllowedScale(-123.45m, 2), Is.True);
            Assert.That(DecimalAmountRules.HasAllowedScale(-123.45m, 1), Is.False);
            Assert.That(DecimalAmountRules.HasAllowedScale(decimal.MaxValue, 0), Is.True);
            Assert.That(DecimalAmountRules.HasAllowedScale(decimal.MinValue, 0), Is.True);
            Assert.That(DecimalAmountRules.HasAllowedScale(smallestPositive, 27), Is.False);
            Assert.That(DecimalAmountRules.HasAllowedScale(smallestPositive, 28), Is.True);
            Assert.That(DecimalAmountRules.HasAllowedScale(smallestNegative, 27), Is.False);
            Assert.That(DecimalAmountRules.HasAllowedScale(smallestNegative, 28), Is.True);
        });
    }

    [Test]
    public void AllowedScaleRejectsUnsupportedDigitCounts()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DecimalAmountRules.HasAllowedScale(1m, -1), Is.False);
            Assert.That(DecimalAmountRules.HasAllowedScale(1m, 29), Is.False);
        });
    }

    [Test]
    public void IncrementInspectionAcceptsExactPositiveNegativeAndZeroMultiples()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DecimalAmountRules.IsMultipleOfIncrement(1.05m, 100m, 5), Is.True);
            Assert.That(DecimalAmountRules.IsMultipleOfIncrement(-1.10m, 100m, 5), Is.True);
            Assert.That(DecimalAmountRules.IsMultipleOfIncrement(0m, 100m, 5), Is.True);
            Assert.That(DecimalAmountRules.IsMultipleOfIncrement(1.25m, 100m, 25), Is.True);
            Assert.That(
                DecimalAmountRules.IsMultipleOfIncrement(decimal.MaxValue, 100m, 5),
                Is.True);
            Assert.That(
                DecimalAmountRules.IsMultipleOfIncrement(decimal.MinValue, 100m, 5),
                Is.True);
        });
    }

    [Test]
    public void IncrementInspectionRejectsValuesBetweenExactMultiples()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DecimalAmountRules.IsMultipleOfIncrement(1.01m, 100m, 5), Is.False);
            Assert.That(DecimalAmountRules.IsMultipleOfIncrement(-1.03m, 100m, 5), Is.False);
            Assert.That(DecimalAmountRules.IsMultipleOfIncrement(1.20m, 100m, 25), Is.False);
            Assert.That(DecimalAmountRules.IsMultipleOfIncrement(0.0001m, 100m, 5), Is.False);
        });
    }

    [Test]
    public void ZeroIncrementAddsNoRestrictionAndInvalidRulesAreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DecimalAmountRules.IsMultipleOfIncrement(1.234m, 100m, 0), Is.True);
            Assert.That(DecimalAmountRules.IsMultipleOfIncrement(1m, 0m, 0), Is.False);
            Assert.That(DecimalAmountRules.IsMultipleOfIncrement(1m, -100m, 5), Is.False);
            Assert.That(DecimalAmountRules.IsMultipleOfIncrement(1m, 100m, -1), Is.False);
            Assert.That(
                DecimalAmountRules.IsMultipleOfIncrement(1m, decimal.MaxValue, 1),
                Is.False);
        });
    }
}
