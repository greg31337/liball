using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class KsuidBehaviorTests
{
    [Test]
    public void CanonicalText_IsAlwaysTwentySevenCharacters()
    {
        KsuidValue zero = new Ksuid().ParseValue("000000000000000000000000000");
        KsuidValue generated = new Ksuid().NewAtUnixSeconds(KsuidLimits.EpochUnixSeconds + 123456);

        Assert.That(zero.ToCanonicalString(), Has.Length.EqualTo(27));
        Assert.That(generated.ToCanonicalString(), Has.Length.EqualTo(27));
    }

    [Test]
    public void Validation_RejectsCharactersOutsideBase62Alphabet()
    {
        IdentifierValidationResult underscore = new Ksuid().Validate("00000000000000000000000000_");
        IdentifierValidationResult hyphen = new Ksuid().Validate("00000000000000000000000000-");

        Assert.That(underscore.Ok, Is.False);
        Assert.That(underscore.Error, Is.EqualTo("invalid Base62 character at index 26"));
        Assert.That(hyphen.Ok, Is.False);
        Assert.That(hyphen.Error, Is.EqualTo("invalid Base62 character at index 26"));
    }

    [Test]
    public void ParsedTimestamp_MatchesFirstFourBigEndianBytes()
    {
        byte[] bytes = new byte[20];
        BigEndianTools.WriteUInt32(bytes, 0, 123456U);

        KsuidValue value = new Ksuid().ParseValue(new KsuidValue(bytes).ToCanonicalString());

        Assert.That(value.AdjustedSeconds(), Is.EqualTo(123456));
        Assert.That(value.UnixSeconds(), Is.EqualTo(KsuidLimits.EpochUnixSeconds + 123456));
    }

    [Test]
    public void PayloadBytes_AreAlwaysSixteenBytes()
    {
        KsuidValue zero = new Ksuid().ParseValue("000000000000000000000000000");
        KsuidValue generated = new Ksuid().NewAtUnixSeconds(KsuidLimits.EpochUnixSeconds + 123456);

        Assert.That(zero.PayloadBytes(), Has.Length.EqualTo(16));
        Assert.That(generated.PayloadBytes(), Has.Length.EqualTo(16));
    }

    [Test]
    public void BinaryAndTextOrder_FollowTimestampThenPayloadBytes()
    {
        KsuidValue early = new KsuidValue(Bytes(1, 0));
        KsuidValue late = new KsuidValue(Bytes(2, 0));
        KsuidValue sameTimestampLowPayload = new KsuidValue(Bytes(2, 1));
        KsuidValue sameTimestampHighPayload = new KsuidValue(Bytes(2, 2));

        Assert.That(early.CompareTo(late), Is.LessThan(0));
        Assert.That(sameTimestampLowPayload.CompareTo(sameTimestampHighPayload), Is.LessThan(0));
        Assert.That(string.CompareOrdinal(early.ToCanonicalString(), late.ToCanonicalString()), Is.LessThan(0));
        Assert.That(string.CompareOrdinal(sameTimestampLowPayload.ToCanonicalString(), sameTimestampHighPayload.ToCanonicalString()), Is.LessThan(0));
    }

    [Test]
    public void MaximumTimestamp_SortsAfterMinimumTimestamp()
    {
        byte[] minBytes = new byte[20];
        byte[] maxBytes = new byte[20];
        BigEndianTools.WriteUInt32(maxBytes, 0, 4294967295U);

        KsuidValue min = new KsuidValue(minBytes);
        KsuidValue max = new KsuidValue(maxBytes);

        Assert.That(min.CompareTo(max), Is.LessThan(0));
        Assert.That(string.CompareOrdinal(min.ToCanonicalString(), max.ToCanonicalString()), Is.LessThan(0));
        Assert.That(max.AdjustedSeconds(), Is.EqualTo(KsuidLimits.MaxAdjustedSeconds));
        Assert.That(max.UnixSeconds(), Is.EqualTo(KsuidLimits.MaxUnixSeconds));
    }

    static byte[] Bytes(uint adjustedSeconds, byte lastPayloadByte)
    {
        byte[] bytes = new byte[20];
        BigEndianTools.WriteUInt32(bytes, 0, adjustedSeconds);
        bytes[19] = lastPayloadByte;
        return bytes;
    }
}
