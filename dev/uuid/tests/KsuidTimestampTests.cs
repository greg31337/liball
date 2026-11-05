using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class KsuidTimestampTests
{
    [Test]
    public void IsValidAdjustedSeconds_AcceptsThirtyTwoBitUnsignedRange()
    {
        Assert.That(KsuidTimestamp.IsValidAdjustedSeconds(0), Is.True);
        Assert.That(KsuidTimestamp.IsValidAdjustedSeconds(KsuidLimits.MaxAdjustedSeconds), Is.True);
        Assert.That(KsuidTimestamp.IsValidAdjustedSeconds(-1), Is.False);
        Assert.That(KsuidTimestamp.IsValidAdjustedSeconds(KsuidLimits.MaxAdjustedSeconds + 1), Is.False);
    }

    [Test]
    public void IsValidUnixSeconds_AcceptsKsuidEpochRange()
    {
        Assert.That(KsuidTimestamp.IsValidUnixSeconds(KsuidLimits.EpochUnixSeconds), Is.True);
        Assert.That(KsuidTimestamp.IsValidUnixSeconds(KsuidLimits.MaxUnixSeconds), Is.True);
        Assert.That(KsuidTimestamp.IsValidUnixSeconds(KsuidLimits.EpochUnixSeconds - 1), Is.False);
        Assert.That(KsuidTimestamp.IsValidUnixSeconds(KsuidLimits.MaxUnixSeconds + 1), Is.False);
    }

    [Test]
    public void ConversionHelpers_ConvertBetweenUnixAndAdjustedSeconds()
    {
        Assert.That(KsuidTimestamp.ToAdjustedSeconds(KsuidLimits.EpochUnixSeconds), Is.EqualTo(0));
        Assert.That(KsuidTimestamp.ToAdjustedSeconds(KsuidLimits.EpochUnixSeconds + 123456), Is.EqualTo(123456));
        Assert.That(KsuidTimestamp.ToUnixSeconds(0), Is.EqualTo(KsuidLimits.EpochUnixSeconds));
        Assert.That(KsuidTimestamp.ToUnixSeconds(123456), Is.EqualTo(KsuidLimits.EpochUnixSeconds + 123456));
    }

    [Test]
    public void ConversionHelpers_RejectOutOfRangeValues()
    {
        Assert.That(() => KsuidTimestamp.ToAdjustedSeconds(KsuidLimits.EpochUnixSeconds - 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => KsuidTimestamp.ToAdjustedSeconds(KsuidLimits.MaxUnixSeconds + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => KsuidTimestamp.ToUnixSeconds(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => KsuidTimestamp.ToUnixSeconds(KsuidLimits.MaxAdjustedSeconds + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void ValidateAdjustedSeconds_ReturnsValidationResult()
    {
        IdentifierValidationResult valid = KsuidTimestamp.ValidateAdjustedSeconds(123456);
        IdentifierValidationResult invalid = KsuidTimestamp.ValidateAdjustedSeconds(-1);

        Assert.That(valid.Ok, Is.True);
        Assert.That(valid.Kind, Is.EqualTo(IdentifierKind.Ksuid));
        Assert.That(valid.NormalizedText, Is.EqualTo("123456"));
        Assert.That(invalid.Ok, Is.False);
        Assert.That(invalid.Error, Is.EqualTo("KSUID adjusted seconds is outside 32-bit range"));
    }

    [Test]
    public void ValidateUnixSeconds_ReturnsValidationResult()
    {
        IdentifierValidationResult valid = KsuidTimestamp.ValidateUnixSeconds(KsuidLimits.EpochUnixSeconds + 123456);
        IdentifierValidationResult invalid = KsuidTimestamp.ValidateUnixSeconds(KsuidLimits.EpochUnixSeconds - 1);

        Assert.That(valid.Ok, Is.True);
        Assert.That(valid.Kind, Is.EqualTo(IdentifierKind.Ksuid));
        Assert.That(valid.NormalizedText, Is.EqualTo((KsuidLimits.EpochUnixSeconds + 123456).ToString()));
        Assert.That(invalid.Ok, Is.False);
        Assert.That(invalid.Error, Is.EqualTo("KSUID unix seconds is outside adjusted epoch range"));
    }

    [Test]
    public void GeneratorAndValueUseRangeHelpersAtBoundaries()
    {
        KsuidValue min = KsuidGenerator.NewAtUnixSeconds(KsuidLimits.EpochUnixSeconds);
        KsuidValue max = KsuidGenerator.NewAtUnixSeconds(KsuidLimits.MaxUnixSeconds);

        Assert.That(min.AdjustedSeconds(), Is.EqualTo(0));
        Assert.That(min.UnixSeconds(), Is.EqualTo(KsuidLimits.EpochUnixSeconds));
        Assert.That(max.AdjustedSeconds(), Is.EqualTo(KsuidLimits.MaxAdjustedSeconds));
        Assert.That(max.UnixSeconds(), Is.EqualTo(KsuidLimits.MaxUnixSeconds));
    }
}
