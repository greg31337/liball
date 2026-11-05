using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UnixTimeToolsTests
{
    [Test]
    public void FromUnixSeconds_ConvertsKnownValues()
    {
        Assert.That(UnixTimeTools.FromUnixSeconds(0), Is.EqualTo(DateTimeOffset.UnixEpoch));
        Assert.That(UnixTimeTools.FromUnixSeconds(1), Is.EqualTo(DateTimeOffset.UnixEpoch.AddSeconds(1)));
        Assert.That(UnixTimeTools.FromUnixSeconds(-1), Is.EqualTo(DateTimeOffset.UnixEpoch.AddSeconds(-1)));
    }

    [Test]
    public void FromUnixMilliseconds_ConvertsKnownValues()
    {
        Assert.That(UnixTimeTools.FromUnixMilliseconds(0), Is.EqualTo(DateTimeOffset.UnixEpoch));
        Assert.That(UnixTimeTools.FromUnixMilliseconds(1500), Is.EqualTo(DateTimeOffset.UnixEpoch.AddMilliseconds(1500)));
        Assert.That(UnixTimeTools.FromUnixMilliseconds(-1), Is.EqualTo(DateTimeOffset.UnixEpoch.AddMilliseconds(-1)));
    }

    [Test]
    public void ToUnixMethods_ConvertDateTimeOffsetValues()
    {
        DateTimeOffset time = DateTimeOffset.UnixEpoch.AddSeconds(123).AddMilliseconds(456);

        Assert.That(UnixTimeTools.ToUnixSeconds(time), Is.EqualTo(123));
        Assert.That(UnixTimeTools.ToUnixMilliseconds(time), Is.EqualTo(123456));
    }

    [Test]
    public void RangeValidation_AcceptsDateTimeOffsetUnixBounds()
    {
        Assert.That(UnixTimeTools.IsValidUnixSeconds(UnixTimeTools.MinUnixSeconds), Is.True);
        Assert.That(UnixTimeTools.IsValidUnixSeconds(UnixTimeTools.MaxUnixSeconds), Is.True);
        Assert.That(UnixTimeTools.IsValidUnixMilliseconds(UnixTimeTools.MinUnixMilliseconds), Is.True);
        Assert.That(UnixTimeTools.IsValidUnixMilliseconds(UnixTimeTools.MaxUnixMilliseconds), Is.True);
    }

    [Test]
    public void RangeValidation_RejectsValuesOutsideDateTimeOffsetUnixBounds()
    {
        Assert.That(UnixTimeTools.IsValidUnixSeconds(UnixTimeTools.MinUnixSeconds - 1), Is.False);
        Assert.That(UnixTimeTools.IsValidUnixSeconds(UnixTimeTools.MaxUnixSeconds + 1), Is.False);
        Assert.That(UnixTimeTools.IsValidUnixMilliseconds(UnixTimeTools.MinUnixMilliseconds - 1), Is.False);
        Assert.That(UnixTimeTools.IsValidUnixMilliseconds(UnixTimeTools.MaxUnixMilliseconds + 1), Is.False);
    }

    [Test]
    public void ValidateUnixSeconds_ReturnsValidationResult()
    {
        IdentifierValidationResult valid = UnixTimeTools.ValidateUnixSeconds(123);
        IdentifierValidationResult invalid = UnixTimeTools.ValidateUnixSeconds(UnixTimeTools.MaxUnixSeconds + 1);

        Assert.That(valid.Ok, Is.True);
        Assert.That(valid.NormalizedText, Is.EqualTo("123"));
        Assert.That(invalid.Ok, Is.False);
        Assert.That(invalid.Error, Is.EqualTo("unix seconds is outside DateTimeOffset range"));
    }

    [Test]
    public void ValidateUnixMilliseconds_ReturnsValidationResult()
    {
        IdentifierValidationResult valid = UnixTimeTools.ValidateUnixMilliseconds(123456);
        IdentifierValidationResult invalid = UnixTimeTools.ValidateUnixMilliseconds(UnixTimeTools.MinUnixMilliseconds - 1);

        Assert.That(valid.Ok, Is.True);
        Assert.That(valid.NormalizedText, Is.EqualTo("123456"));
        Assert.That(invalid.Ok, Is.False);
        Assert.That(invalid.Error, Is.EqualTo("unix milliseconds is outside DateTimeOffset range"));
    }

    [Test]
    public void SecondsToMilliseconds_ValidatesRangeAndConverts()
    {
        Assert.That(UnixTimeTools.SecondsToMilliseconds(123), Is.EqualTo(123000));
        Assert.That(UnixTimeTools.SecondsToMilliseconds(-123), Is.EqualTo(-123000));
        Assert.That(() => UnixTimeTools.SecondsToMilliseconds(UnixTimeTools.MaxUnixSeconds + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void MillisecondsToSeconds_FloorsNegativeValues()
    {
        Assert.That(UnixTimeTools.MillisecondsToSeconds(1999), Is.EqualTo(1));
        Assert.That(UnixTimeTools.MillisecondsToSeconds(1000), Is.EqualTo(1));
        Assert.That(UnixTimeTools.MillisecondsToSeconds(999), Is.EqualTo(0));
        Assert.That(UnixTimeTools.MillisecondsToSeconds(-1), Is.EqualTo(-1));
        Assert.That(UnixTimeTools.MillisecondsToSeconds(-999), Is.EqualTo(-1));
        Assert.That(UnixTimeTools.MillisecondsToSeconds(-1000), Is.EqualTo(-1));
        Assert.That(UnixTimeTools.MillisecondsToSeconds(-1001), Is.EqualTo(-2));
    }

    [Test]
    public void FromUnixMethods_RejectValuesOutsideDateTimeOffsetUnixBounds()
    {
        Assert.That(() => UnixTimeTools.FromUnixSeconds(UnixTimeTools.MinUnixSeconds - 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => UnixTimeTools.FromUnixMilliseconds(UnixTimeTools.MaxUnixMilliseconds + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
