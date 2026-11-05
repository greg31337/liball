using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class ObjectIdValueTests
{
    [Test]
    public void Constructor_CopiesInputBytes()
    {
        byte[] bytes = ExampleBytes();
        ObjectIdValue value = new ObjectIdValue(bytes);

        bytes[0] = 0xFF;

        Assert.That(value.ToBytes(), Is.EqualTo(ExampleBytes()));
    }

    [Test]
    public void ToBytes_ReturnsIndependentCopy()
    {
        ObjectIdValue value = new ObjectIdValue(ExampleBytes());

        byte[] copy = value.ToBytes();
        copy[0] = 0xFF;

        Assert.That(copy[0], Is.EqualTo(0xFF));
        Assert.That(value.ToBytes()[0], Is.EqualTo(0x5F));
    }

    [Test]
    public void CopyTo_CopiesIntoDestinationWithoutResizing()
    {
        ObjectIdValue value = new ObjectIdValue(ExampleBytes());
        byte[] destination = new byte[14];
        destination[12] = 0xAA;
        destination[13] = 0xBB;

        value.CopyTo(destination);

        Assert.That(destination[0], Is.EqualTo(0x5F));
        Assert.That(destination[11], Is.EqualTo(0x03));
        Assert.That(destination[12], Is.EqualTo(0xAA));
        Assert.That(destination[13], Is.EqualTo(0xBB));
    }

    [Test]
    public void CopyTo_RejectsNullOrShortDestination()
    {
        ObjectIdValue value = new ObjectIdValue(ExampleBytes());

        Assert.That(() => value.CopyTo(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => value.CopyTo(new byte[11]), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void TimestampAccessors_ReadFirstFourBytesAsUnixSeconds()
    {
        ObjectIdValue value = new ObjectIdValue(ExampleBytes());

        Assert.That(value.UnixSeconds(), Is.EqualTo(1600000000));
        Assert.That(value.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(1600000000)));
    }

    [Test]
    public void Formatting_ReturnsCanonicalHexText()
    {
        ObjectIdValue value = new ObjectIdValue(ExampleBytes());

        Assert.That(value.ToCanonicalString(), Is.EqualTo("5f5e10001122334455010203"));
        Assert.That(value.ToHexString(), Is.EqualTo("5f5e10001122334455010203"));
        Assert.That(value.ToUpperHexString(), Is.EqualTo("5F5E10001122334455010203"));
    }

    [Test]
    public void ComparisonHelpers_UseBinaryLexicographicOrder()
    {
        ObjectIdValue low = new ObjectIdValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });
        ObjectIdValue high = new ObjectIdValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 });
        ObjectIdValue same = new ObjectIdValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });

        Assert.That(low.CompareTo(high), Is.LessThan(0));
        Assert.That(high.CompareTo(low), Is.GreaterThan(0));
        Assert.That(low.CompareTo(same), Is.EqualTo(0));
        Assert.That(low.EqualsValue(same), Is.True);
        Assert.That(low.EqualsValue(high), Is.False);
        Assert.That(low.EqualsBytes(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }), Is.True);
        Assert.That(ObjectIdValue.Compare(null, low), Is.LessThan(0));
        Assert.That(ObjectIdValue.Compare(low, null), Is.GreaterThan(0));
        Assert.That(ObjectIdValue.Compare(null, null), Is.EqualTo(0));
    }

    [Test]
    public void Constructor_RejectsInvalidByteLength()
    {
        Assert.That(() => new ObjectIdValue(new byte[11]), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new ObjectIdValue(new byte[13]), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new ObjectIdValue(null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Timestamp_SupportsMaximumUInt32Value()
    {
        ObjectIdValue value = new ObjectIdValue(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 1, 2, 3, 4, 5, 0xFF, 0xFF, 0xFF });

        Assert.That(value.UnixSeconds(), Is.EqualTo(4294967295));
        Assert.That(value.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(4294967295)));
    }

    static byte[] ExampleBytes()
    {
        return new byte[] { 0x5F, 0x5E, 0x10, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x01, 0x02, 0x03 };
    }
}
