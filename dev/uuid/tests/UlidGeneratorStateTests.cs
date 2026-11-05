using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UlidGeneratorStateTests
{
    [Test]
    public void NextAtUnixMilliseconds_IncrementsRandomnessWithinSameMillisecond()
    {
        UlidGeneratorState state = new UlidGeneratorState();

        UlidValue first = state.NextAtUnixMilliseconds(123456789);
        UlidValue second = state.NextAtUnixMilliseconds(123456789);
        UlidValue third = state.NextAtUnixMilliseconds(123456789);

        Assert.That(first.UnixMilliseconds(), Is.EqualTo(123456789));
        Assert.That(second.UnixMilliseconds(), Is.EqualTo(123456789));
        Assert.That(first.CompareTo(second), Is.LessThan(0));
        Assert.That(second.CompareTo(third), Is.LessThan(0));
    }

    [Test]
    public void NextAtUnixMilliseconds_UsesNewTimestampWhenTimeAdvances()
    {
        UlidGeneratorState state = new UlidGeneratorState();

        UlidValue first = state.NextAtUnixMilliseconds(123);
        UlidValue second = state.NextAtUnixMilliseconds(124);

        Assert.That(first.UnixMilliseconds(), Is.EqualTo(123));
        Assert.That(second.UnixMilliseconds(), Is.EqualTo(124));
        Assert.That(first.CompareTo(second), Is.LessThan(0));
    }

    [Test]
    public void NextAtUnixMilliseconds_RejectsClockRollback()
    {
        UlidGeneratorState state = new UlidGeneratorState();

        state.NextAtUnixMilliseconds(123);

        IdentifierException exception = Assert.Throws<IdentifierException>(() => state.NextAtUnixMilliseconds(122));

        Assert.That(exception.Code, Is.EqualTo(IdentifierErrorCode.ClockRollback));
        Assert.That(exception.Message, Is.EqualTo("ULID timestamp moved backwards"));
    }

    [Test]
    public void NextAtUnixMilliseconds_RejectsRandomnessOverflow()
    {
        UlidGeneratorState state = UlidGeneratorState.FromLastValue(new UlidValue(MaxRandomnessBytes(123)));

        IdentifierException exception = Assert.Throws<IdentifierException>(() => state.NextAtUnixMilliseconds(123));

        Assert.That(exception.Code, Is.EqualTo(IdentifierErrorCode.RandomFieldOverflow));
        Assert.That(exception.Message, Is.EqualTo("ULID randomness overflow"));
    }

    [Test]
    public void NextAtUnixMilliseconds_RejectsTimestampsOutsideFortyEightBitRange()
    {
        UlidGeneratorState state = new UlidGeneratorState();

        IdentifierException negative = Assert.Throws<IdentifierException>(() => state.NextAtUnixMilliseconds(-1));
        IdentifierException overflow = Assert.Throws<IdentifierException>(() => state.NextAtUnixMilliseconds(BigEndianTools.MaxUInt48 + 1));

        Assert.That(negative.Code, Is.EqualTo(IdentifierErrorCode.TimestampOverflow));
        Assert.That(negative.Message, Is.EqualTo("ULID timestamp is outside the 48-bit range"));
        Assert.That(overflow.Code, Is.EqualTo(IdentifierErrorCode.TimestampOverflow));
        Assert.That(overflow.Message, Is.EqualTo("ULID timestamp is outside the 48-bit range"));
    }

    [Test]
    public void FromLastValue_RejectsNullAndDefensivelyCopiesTheSeed()
    {
        byte[] bytes = BaseBytes(123);
        bytes[15] = 1;
        UlidValue seed = new UlidValue(bytes);
        UlidGeneratorState state = UlidGeneratorState.FromLastValue(seed);
        bytes[15] = 200;

        UlidValue next = state.NextAtUnixMilliseconds(123);
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => UlidGeneratorState.FromLastValue(null));

        Assert.That(next.RandomnessBytes()[9], Is.EqualTo(2));
        Assert.That(exception.ParamName, Is.EqualTo("lastValue"));
    }

    [Test]
    public void NextAt_UsesTheSuppliedDateTimeOffset()
    {
        UlidGeneratorState state = new UlidGeneratorState();

        UlidValue value = state.NextAt(DateTimeOffset.FromUnixTimeMilliseconds(123456789));

        Assert.That(value.UnixMilliseconds(), Is.EqualTo(123456789));
    }

    [Test]
    public void IncrementRandomness_IncrementsLeastSignificantRandomByte()
    {
        byte[] bytes = BaseBytes(123);
        bytes[15] = 41;

        bool ok = UlidGenerator.IncrementRandomness(bytes);

        Assert.That(ok, Is.True);
        Assert.That(bytes[15], Is.EqualTo(42));
        Assert.That(BigEndianTools.ReadUInt48(bytes, 0), Is.EqualTo(123));
    }

    [Test]
    public void IncrementRandomness_CarriesAcrossAllRandomnessBytes()
    {
        byte[] bytes = BaseBytes(123);
        bytes[6] = 1;
        int index = 7;
        while (index < 16)
        {
            bytes[index] = 255;
            index = index + 1;
        }

        bool ok = UlidGenerator.IncrementRandomness(bytes);

        Assert.That(ok, Is.True);
        Assert.That(bytes[6], Is.EqualTo(2));
        Assert.That(bytes[7], Is.EqualTo(0));
        Assert.That(bytes[15], Is.EqualTo(0));
        Assert.That(BigEndianTools.ReadUInt48(bytes, 0), Is.EqualTo(123));
    }

    [Test]
    public void IncrementRandomness_ReturnsFalseWhenAllRandomnessBitsOverflow()
    {
        byte[] bytes = MaxRandomnessBytes(123);

        bool ok = UlidGenerator.IncrementRandomness(bytes);

        Assert.That(ok, Is.False);
        Assert.That(BigEndianTools.ReadUInt48(bytes, 0), Is.EqualTo(123));
        Assert.That(new UlidValue(bytes).RandomnessBytes(), Is.EqualTo(new byte[10]));
    }

    [Test]
    public void IncrementRandomness_RejectsNullAndWrongLengthBuffers()
    {
        ArgumentNullException nullBytes = Assert.Throws<ArgumentNullException>(() => UlidGenerator.IncrementRandomness(null));
        ArgumentException wrongLength = Assert.Throws<ArgumentException>(() => UlidGenerator.IncrementRandomness(new byte[15]));

        Assert.That(nullBytes.ParamName, Is.EqualTo("bytes"));
        Assert.That(wrongLength.ParamName, Is.EqualTo("bytes"));
        Assert.That(wrongLength.Message, Does.StartWith("ULID byte array must be 16 bytes"));
    }

    [Test]
    public void NewMonotonic_GeneratesValidUlid()
    {
        Ulid ulid = new Ulid();

        UlidValue first = ulid.NewMonotonic();
        UlidValue second = ulid.NewMonotonic();
        UlidParseResult parsed = ulid.Parse(first.ToCanonicalString());

        Assert.That(parsed.Ok, Is.True);
        Assert.That(parsed.Value, Is.Not.Null);
        Assert.That(parsed.Value.EqualsValue(first), Is.True);
        Assert.That(first.CompareTo(second), Is.LessThan(0));
    }

    static byte[] BaseBytes(long unixMilliseconds)
    {
        byte[] bytes = new byte[16];
        BigEndianTools.WriteUInt48(bytes, 0, unixMilliseconds);
        return bytes;
    }

    static byte[] MaxRandomnessBytes(long unixMilliseconds)
    {
        byte[] bytes = BaseBytes(unixMilliseconds);
        int index = 6;
        while (index < 16)
        {
            bytes[index] = 255;
            index = index + 1;
        }

        return bytes;
    }
}
