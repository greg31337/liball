using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class KsuidValueTests
{
    [Test]
    public void Constructor_CopiesInputBytes()
    {
        byte[] bytes = ExampleBytes();
        KsuidValue value = new KsuidValue(bytes);

        bytes[0] = 0xFF;

        Assert.That(value.ToBytes(), Is.EqualTo(ExampleBytes()));
    }

    [Test]
    public void ToBytes_ReturnsIndependentCopy()
    {
        KsuidValue value = new KsuidValue(ExampleBytes());

        byte[] copy = value.ToBytes();
        copy[0] = 0xFF;

        Assert.That(copy[0], Is.EqualTo(0xFF));
        Assert.That(value.ToBytes()[0], Is.EqualTo(0x00));
    }

    [Test]
    public void CopyTo_CopiesIntoDestinationWithoutResizing()
    {
        KsuidValue value = new KsuidValue(ExampleBytes());
        byte[] destination = new byte[22];
        destination[20] = 0xAA;
        destination[21] = 0xBB;

        value.CopyTo(destination);

        Assert.That(destination[0], Is.EqualTo(0x00));
        Assert.That(destination[19], Is.EqualTo(0xC7));
        Assert.That(destination[20], Is.EqualTo(0xAA));
        Assert.That(destination[21], Is.EqualTo(0xBB));
    }

    [Test]
    public void CopyTo_RejectsNullOrShortDestination()
    {
        KsuidValue value = new KsuidValue(ExampleBytes());

        Assert.That(() => value.CopyTo(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => value.CopyTo(new byte[19]), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void TimestampAccessors_ReadAdjustedAndUnixSeconds()
    {
        KsuidValue value = new KsuidValue(ExampleBytes());

        Assert.That(value.AdjustedSeconds(), Is.EqualTo(123456));
        Assert.That(value.UnixSeconds(), Is.EqualTo(KsuidLimits.EpochUnixSeconds + 123456));
        Assert.That(value.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(KsuidLimits.EpochUnixSeconds + 123456)));
    }

    [Test]
    public void PayloadBytes_ReturnsIndependentLastSixteenBytes()
    {
        KsuidValue value = new KsuidValue(ExampleBytes());

        byte[] payload = value.PayloadBytes();
        payload[0] = 0xFF;

        Assert.That(payload, Is.EqualTo(new byte[] { 0xFF, 0xDE, 0xAA, 0xD7, 0x13, 0xF8, 0xA5, 0x9E, 0x5C, 0xA5, 0x7C, 0x2B, 0x47, 0x72, 0x6F, 0xC7 }));
        Assert.That(value.PayloadBytes(), Is.EqualTo(new byte[] { 0x1D, 0xDE, 0xAA, 0xD7, 0x13, 0xF8, 0xA5, 0x9E, 0x5C, 0xA5, 0x7C, 0x2B, 0x47, 0x72, 0x6F, 0xC7 }));
    }

    [Test]
    public void ComparisonHelpers_UseBinaryLexicographicOrder()
    {
        KsuidValue low = new KsuidValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });
        KsuidValue high = new KsuidValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 });
        KsuidValue same = new KsuidValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });

        Assert.That(low.CompareTo(high), Is.LessThan(0));
        Assert.That(high.CompareTo(low), Is.GreaterThan(0));
        Assert.That(low.CompareTo(same), Is.EqualTo(0));
        Assert.That(low.EqualsValue(same), Is.True);
        Assert.That(low.EqualsValue(high), Is.False);
        Assert.That(KsuidValue.Compare(null, low), Is.LessThan(0));
        Assert.That(KsuidValue.Compare(low, null), Is.GreaterThan(0));
        Assert.That(KsuidValue.Compare(null, null), Is.EqualTo(0));
    }

    [Test]
    public void Constructor_RejectsInvalidByteLength()
    {
        Assert.That(() => new KsuidValue(new byte[19]), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new KsuidValue(null), Throws.TypeOf<ArgumentNullException>());
    }

    static byte[] ExampleBytes()
    {
        return new byte[] { 0x00, 0x01, 0xE2, 0x40, 0x1D, 0xDE, 0xAA, 0xD7, 0x13, 0xF8, 0xA5, 0x9E, 0x5C, 0xA5, 0x7C, 0x2B, 0x47, 0x72, 0x6F, 0xC7 };
    }
}
