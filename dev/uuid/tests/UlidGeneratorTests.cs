using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UlidGeneratorTests
{
    [Test]
    public void NewAtUnixMilliseconds_WritesTimestampAndRandomness()
    {
        UlidValue value = UlidGenerator.NewAtUnixMilliseconds(1469922850259);

        Assert.That(value.UnixMilliseconds(), Is.EqualTo(1469922850259));
        Assert.That(value.ToCanonicalString(), Has.Length.EqualTo(26));
        Assert.That(value.ToCanonicalString().Substring(0, 10), Is.EqualTo("01ARZ3NDEK"));
        Assert.That(ContainsNonZeroByte(value.RandomnessBytes()), Is.True);
    }

    [Test]
    public void NewAt_UsesSuppliedDateTimeOffset()
    {
        DateTimeOffset time = DateTimeOffset.FromUnixTimeMilliseconds(1469922850259);

        UlidValue value = new Ulid().NewAt(time);

        Assert.That(value.UnixMilliseconds(), Is.EqualTo(1469922850259));
        Assert.That(value.Time(), Is.EqualTo(time));
    }

    [Test]
    public void New_UsesCurrentUnixMilliseconds()
    {
        long before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        UlidValue value = new Ulid().New();

        long after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Assert.That(value.UnixMilliseconds(), Is.GreaterThanOrEqualTo(before));
        Assert.That(value.UnixMilliseconds(), Is.LessThanOrEqualTo(after));
    }

    [Test]
    public void NewAtUnixMilliseconds_GeneratesDifferentRandomnessForSameTimestamp()
    {
        UlidValue first = UlidGenerator.NewAtUnixMilliseconds(123456789);
        UlidValue second = UlidGenerator.NewAtUnixMilliseconds(123456789);

        Assert.That(first.UnixMilliseconds(), Is.EqualTo(second.UnixMilliseconds()));
        Assert.That(first.EqualsValue(second), Is.False);
    }

    [Test]
    public void GeneratedUlid_ParsesAndValidates()
    {
        UlidValue value = new Ulid().NewAtUnixMilliseconds(1469922850259);

        UlidParseResult parse = new Ulid().Parse(value.ToCanonicalString());
        IdentifierValidationResult validation = new Ulid().Validate(value.ToCanonicalString());

        Assert.That(parse.Ok, Is.True);
        Assert.That(parse.Value, Is.Not.Null);
        Assert.That(parse.Value.EqualsValue(value), Is.True);
        Assert.That(validation.Ok, Is.True);
        Assert.That(validation.NormalizedText, Is.EqualTo(value.ToCanonicalString()));
    }

    [Test]
    public void NewAtUnixMilliseconds_RejectsValuesOutsideFortyEightBitUnixRange()
    {
        Assert.That(() => UlidGenerator.NewAtUnixMilliseconds(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => UlidGenerator.NewAtUnixMilliseconds(BigEndianTools.MaxUInt48 + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => UlidGenerator.NewAt(DateTimeOffset.UnixEpoch.AddMilliseconds(-1)), Throws.TypeOf<ArgumentOutOfRangeException>());
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
