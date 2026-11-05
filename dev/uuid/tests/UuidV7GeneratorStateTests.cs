using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UuidV7GeneratorStateTests
{
    [Test]
    public void NextAtUnixMilliseconds_IncrementsTailWithinSameMillisecond()
    {
        UuidV7GeneratorState state = new UuidV7GeneratorState();

        UuidValue first = state.NextAtUnixMilliseconds(123456789);
        UuidValue second = state.NextAtUnixMilliseconds(123456789);
        UuidValue third = state.NextAtUnixMilliseconds(123456789);

        Assert.That(BigEndianTools.ReadUInt48(first.ToBytes(), 0), Is.EqualTo(123456789));
        Assert.That(BigEndianTools.ReadUInt48(second.ToBytes(), 0), Is.EqualTo(123456789));
        Assert.That(first.CompareTo(second), Is.LessThan(0));
        Assert.That(second.CompareTo(third), Is.LessThan(0));
        Assert.That(second.Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(second.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
    }

    [Test]
    public void NextAtUnixMilliseconds_UsesNewTimestampWhenTimeAdvances()
    {
        UuidV7GeneratorState state = new UuidV7GeneratorState();

        UuidValue first = state.NextAtUnixMilliseconds(123);
        UuidValue second = state.NextAtUnixMilliseconds(124);

        Assert.That(BigEndianTools.ReadUInt48(first.ToBytes(), 0), Is.EqualTo(123));
        Assert.That(BigEndianTools.ReadUInt48(second.ToBytes(), 0), Is.EqualTo(124));
        Assert.That(first.CompareTo(second), Is.LessThan(0));
    }

    [Test]
    public void NextAtUnixMilliseconds_RejectsClockRollback()
    {
        UuidV7GeneratorState state = new UuidV7GeneratorState();

        state.NextAtUnixMilliseconds(123);

        IdentifierException exception = Assert.Throws<IdentifierException>(() => state.NextAtUnixMilliseconds(122));

        Assert.That(exception.Code, Is.EqualTo(IdentifierErrorCode.ClockRollback));
        Assert.That(exception.Message, Is.EqualTo("UUIDv7 timestamp moved backwards"));
    }

    [Test]
    public void NextAtUnixMilliseconds_RejectsRandomTailOverflow()
    {
        byte[] bytes = MaxTailBytes(123);
        UuidV7GeneratorState state = UuidV7GeneratorState.FromLastValue(new UuidValue(bytes));

        IdentifierException exception = Assert.Throws<IdentifierException>(() => state.NextAtUnixMilliseconds(123));

        Assert.That(exception.Code, Is.EqualTo(IdentifierErrorCode.RandomFieldOverflow));
        Assert.That(exception.Message, Is.EqualTo("UUIDv7 random tail overflow"));
    }

    [Test]
    public void NextAtUnixMilliseconds_RejectsTimestampBoundariesOutsideUuidV7Range()
    {
        UuidV7GeneratorState state = new UuidV7GeneratorState();

        IdentifierException negative = Assert.Throws<IdentifierException>(() => state.NextAtUnixMilliseconds(-1));
        IdentifierException overflow = Assert.Throws<IdentifierException>(() => state.NextAtUnixMilliseconds(BigEndianTools.MaxUInt48 + 1));

        Assert.That(negative.Code, Is.EqualTo(IdentifierErrorCode.TimestampOverflow));
        Assert.That(negative.Message, Is.EqualTo("UUIDv7 timestamp is outside the 48-bit range"));
        Assert.That(overflow.Code, Is.EqualTo(IdentifierErrorCode.TimestampOverflow));
        Assert.That(overflow.Message, Is.EqualTo("UUIDv7 timestamp is outside the 48-bit range"));
    }

    [Test]
    public void FromLastValue_RejectsNullWrongVersionAndWrongVariantSeeds()
    {
        UuidValue version4 = new Uuid().ParseValue("00000000-0000-4000-8000-000000000000");
        UuidValue wrongVariant = new Uuid().ParseValue("00000000-0000-7000-0000-000000000000");

        ArgumentNullException nullValue = Assert.Throws<ArgumentNullException>(() => UuidV7GeneratorState.FromLastValue(null));
        IdentifierException wrongVersion = Assert.Throws<IdentifierException>(() => UuidV7GeneratorState.FromLastValue(version4));
        IdentifierException variant = Assert.Throws<IdentifierException>(() => UuidV7GeneratorState.FromLastValue(wrongVariant));

        Assert.That(nullValue.ParamName, Is.EqualTo("lastValue"));
        Assert.That(wrongVersion.Code, Is.EqualTo(IdentifierErrorCode.InvalidUuidVersion));
        Assert.That(wrongVersion.Message, Is.EqualTo("last value must be a UUIDv7 value with RFC 9562 variant bits"));
        Assert.That(variant.Code, Is.EqualTo(IdentifierErrorCode.InvalidUuidVersion));
        Assert.That(variant.Message, Is.EqualTo("last value must be a UUIDv7 value with RFC 9562 variant bits"));
    }

    [Test]
    public void NextAt_UsesTheSuppliedDateTimeOffset()
    {
        UuidV7GeneratorState state = new UuidV7GeneratorState();

        UuidValue value = state.NextAt(DateTimeOffset.FromUnixTimeMilliseconds(123456789));

        Assert.That(value.UnixMilliseconds(), Is.EqualTo(123456789));
    }

    [Test]
    public void IncrementV7RandomTail_IncrementsLeastSignificantTailByte()
    {
        byte[] bytes = BaseV7Bytes(123);
        bytes[15] = 41;

        bool ok = UuidGenerator.IncrementV7RandomTail(bytes);

        Assert.That(ok, Is.True);
        Assert.That(bytes[15], Is.EqualTo(42));
        Assert.That(BigEndianTools.ReadUInt48(bytes, 0), Is.EqualTo(123));
        Assert.That(new UuidValue(bytes).Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(new UuidValue(bytes).Variant(), Is.EqualTo(UuidVariant.Rfc9562));
    }

    [Test]
    public void IncrementV7RandomTail_CarriesAcrossVariantPayloadAndVersionPayload()
    {
        byte[] bytes = BaseV7Bytes(123);
        bytes[6] = 0x70;
        bytes[7] = 0x00;
        bytes[8] = 0xBF;
        int index = 9;
        while (index < 16)
        {
            bytes[index] = 255;
            index = index + 1;
        }

        bool ok = UuidGenerator.IncrementV7RandomTail(bytes);

        Assert.That(ok, Is.True);
        Assert.That(bytes[6], Is.EqualTo(0x70));
        Assert.That(bytes[7], Is.EqualTo(0x01));
        Assert.That(bytes[8], Is.EqualTo(0x80));
        Assert.That(bytes[15], Is.EqualTo(0x00));
    }

    [Test]
    public void IncrementV7RandomTail_IncrementsVariantPayloadAfterLowerBytesCarry()
    {
        byte[] bytes = BaseV7Bytes(123);
        int index = 9;
        while (index < bytes.Length)
        {
            bytes[index] = 255;
            index = index + 1;
        }

        bool ok = UuidGenerator.IncrementV7RandomTail(bytes);

        Assert.That(ok, Is.True);
        Assert.That(bytes[8], Is.EqualTo(0x81));
        Assert.That(bytes[9], Is.EqualTo(0));
        Assert.That(new UuidValue(bytes).Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(new UuidValue(bytes).Variant(), Is.EqualTo(UuidVariant.Rfc9562));
    }

    [Test]
    public void IncrementV7RandomTail_IncrementsVersionPayloadAfterVariantAndLowerBytesCarry()
    {
        byte[] bytes = BaseV7Bytes(123);
        bytes[7] = 255;
        bytes[8] = 0xBF;
        int index = 9;
        while (index < bytes.Length)
        {
            bytes[index] = 255;
            index = index + 1;
        }

        bool ok = UuidGenerator.IncrementV7RandomTail(bytes);

        Assert.That(ok, Is.True);
        Assert.That(bytes[6], Is.EqualTo(0x71));
        Assert.That(bytes[7], Is.EqualTo(0));
        Assert.That(bytes[8], Is.EqualTo(0x80));
        Assert.That(new UuidValue(bytes).Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(new UuidValue(bytes).Variant(), Is.EqualTo(UuidVariant.Rfc9562));
    }

    [Test]
    public void IncrementV7RandomTail_RejectsNullAndWrongLengthBuffers()
    {
        ArgumentNullException nullBytes = Assert.Throws<ArgumentNullException>(() => UuidGenerator.IncrementV7RandomTail(null));
        ArgumentException wrongLength = Assert.Throws<ArgumentException>(() => UuidGenerator.IncrementV7RandomTail(new byte[15]));

        Assert.That(nullBytes.ParamName, Is.EqualTo("bytes"));
        Assert.That(wrongLength.ParamName, Is.EqualTo("bytes"));
        Assert.That(wrongLength.Message, Does.StartWith("UUID byte array must be 16 bytes"));
    }

    [Test]
    public void IncrementV7RandomTail_ReturnsFalseWhenAllTailBitsOverflow()
    {
        byte[] bytes = MaxTailBytes(123);

        bool ok = UuidGenerator.IncrementV7RandomTail(bytes);

        Assert.That(ok, Is.False);
        Assert.That(new UuidValue(bytes).Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(new UuidValue(bytes).Variant(), Is.EqualTo(UuidVariant.Rfc9562));
    }

    [Test]
    public void NewMonotonicV7_GeneratesValidVersion7Uuid()
    {
        Uuid uuid = new Uuid();

        UuidValue first = uuid.NewMonotonicV7();
        UuidValue second = uuid.NewMonotonicV7();

        Assert.That(first.Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(first.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
        Assert.That(second.Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(second.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
        Assert.That(first.CompareTo(second), Is.LessThan(0));
    }

    static byte[] BaseV7Bytes(long unixMilliseconds)
    {
        byte[] bytes = new byte[16];
        BigEndianTools.WriteUInt48(bytes, 0, unixMilliseconds);
        UuidGenerator.ApplyVersionAndVariant(bytes, 7);
        return bytes;
    }

    static byte[] MaxTailBytes(long unixMilliseconds)
    {
        byte[] bytes = BaseV7Bytes(unixMilliseconds);
        bytes[6] = 0x7F;
        bytes[7] = 0xFF;
        bytes[8] = 0xBF;
        int index = 9;
        while (index < 16)
        {
            bytes[index] = 255;
            index = index + 1;
        }

        return bytes;
    }
}
