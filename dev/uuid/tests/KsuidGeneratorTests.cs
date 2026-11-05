using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class KsuidGeneratorTests
{
    [Test]
    public void NewAtUnixSeconds_WritesAdjustedTimestampAndRandomPayload()
    {
        KsuidValue value = KsuidGenerator.NewAtUnixSeconds(KsuidLimits.EpochUnixSeconds + 123456);

        Assert.That(value.AdjustedSeconds(), Is.EqualTo(123456));
        Assert.That(value.UnixSeconds(), Is.EqualTo(KsuidLimits.EpochUnixSeconds + 123456));
        Assert.That(value.ToCanonicalString(), Has.Length.EqualTo(27));
        Assert.That(ContainsNonZeroByte(value.PayloadBytes()), Is.True);
    }

    [Test]
    public void NewAt_UsesSuppliedDateTimeOffset()
    {
        DateTimeOffset time = DateTimeOffset.FromUnixTimeSeconds(KsuidLimits.EpochUnixSeconds + 123456);

        KsuidValue value = new Ksuid().NewAt(time);

        Assert.That(value.AdjustedSeconds(), Is.EqualTo(123456));
        Assert.That(value.Time(), Is.EqualTo(time));
    }

    [Test]
    public void New_UsesCurrentUnixSeconds()
    {
        long before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        KsuidValue value = new Ksuid().New();

        long after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Assert.That(value.UnixSeconds(), Is.GreaterThanOrEqualTo(before));
        Assert.That(value.UnixSeconds(), Is.LessThanOrEqualTo(after));
    }

    [Test]
    public void NewAtUnixSeconds_GeneratesDifferentPayloadsForSameTimestamp()
    {
        KsuidValue first = KsuidGenerator.NewAtUnixSeconds(KsuidLimits.EpochUnixSeconds + 123456);
        KsuidValue second = KsuidGenerator.NewAtUnixSeconds(KsuidLimits.EpochUnixSeconds + 123456);

        Assert.That(first.UnixSeconds(), Is.EqualTo(second.UnixSeconds()));
        Assert.That(first.EqualsValue(second), Is.False);
    }

    [Test]
    public void GeneratedKsuid_ParsesAndValidates()
    {
        KsuidValue value = new Ksuid().NewAtUnixSeconds(KsuidLimits.EpochUnixSeconds + 123456);

        KsuidParseResult parse = new Ksuid().Parse(value.ToCanonicalString());
        IdentifierValidationResult validation = new Ksuid().Validate(value.ToCanonicalString());

        Assert.That(parse.Ok, Is.True);
        Assert.That(parse.Value, Is.Not.Null);
        Assert.That(parse.Value.EqualsValue(value), Is.True);
        Assert.That(validation.Ok, Is.True);
        Assert.That(validation.NormalizedText, Is.EqualTo(value.ToCanonicalString()));
    }

    [Test]
    public void NewAtUnixSeconds_RejectsValuesOutsideAdjustedTimestampRange()
    {
        Assert.That(() => KsuidGenerator.NewAtUnixSeconds(KsuidLimits.EpochUnixSeconds - 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => KsuidGenerator.NewAtUnixSeconds(KsuidLimits.MaxUnixSeconds + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => KsuidGenerator.NewAt(DateTimeOffset.FromUnixTimeSeconds(KsuidLimits.EpochUnixSeconds - 1)), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static bool ContainsNonZeroByte(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] != 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
