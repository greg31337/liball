using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UlidValueTests
{
    [Test]
    public void Constructor_CopiesInputBytes()
    {
        byte[] bytes = ExampleBytes();
        UlidValue value = new UlidValue(bytes);

        bytes[0] = 0xFF;

        Assert.That(value.ToBytes(), Is.EqualTo(ExampleBytes()));
    }

    [Test]
    public void ToBytes_ReturnsIndependentCopy()
    {
        UlidValue value = new UlidValue(ExampleBytes());

        byte[] copy = value.ToBytes();
        copy[0] = 0xFF;

        Assert.That(copy[0], Is.EqualTo(0xFF));
        Assert.That(value.ToBytes()[0], Is.EqualTo(0x01));
    }

    [Test]
    public void UuidCompatibleBytes_ReturnIndependentCopy()
    {
        UlidValue value = new UlidValue(ExampleBytes());

        byte[] uuidBytes = value.ToUuidBytes();
        uuidBytes[0] = 0xFF;

        Assert.That(uuidBytes[0], Is.EqualTo(0xFF));
        Assert.That(value.ToUuidBytes(), Is.EqualTo(ExampleBytes()));
    }

    [Test]
    public void CopyTo_CopiesIntoDestinationWithoutResizing()
    {
        UlidValue value = new UlidValue(ExampleBytes());
        byte[] destination = new byte[18];
        destination[16] = 0xAA;
        destination[17] = 0xBB;

        value.CopyTo(destination);

        Assert.That(destination[0], Is.EqualTo(0x01));
        Assert.That(destination[15], Is.EqualTo(0x5B));
        Assert.That(destination[16], Is.EqualTo(0xAA));
        Assert.That(destination[17], Is.EqualTo(0xBB));
    }

    [Test]
    public void CopyTo_RejectsNullOrShortDestination()
    {
        UlidValue value = new UlidValue(ExampleBytes());

        Assert.That(() => value.CopyTo(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => value.CopyTo(new byte[15]), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void TimestampAccessors_ReadFirstFortyEightBitsAsUnixMilliseconds()
    {
        UlidValue value = new UlidValue(ExampleBytes());

        Assert.That(value.UnixMilliseconds(), Is.EqualTo(1469922850259));
        Assert.That(value.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1469922850259)));
    }

    [Test]
    public void RandomnessBytes_ReturnsIndependentLastTenBytes()
    {
        UlidValue value = new UlidValue(ExampleBytes());

        byte[] randomness = value.RandomnessBytes();
        randomness[0] = 0;

        Assert.That(randomness, Is.EqualTo(new byte[] { 0x00, 0x76, 0x4C, 0x61, 0xEF, 0xB9, 0x93, 0x02, 0xBD, 0x5B }));
        Assert.That(value.RandomnessBytes(), Is.EqualTo(new byte[] { 0xD6, 0x76, 0x4C, 0x61, 0xEF, 0xB9, 0x93, 0x02, 0xBD, 0x5B }));
    }

    [Test]
    public void ToString_ReturnsCanonicalUppercaseUlidText()
    {
        UlidValue value = new UlidValue(ExampleBytes());

        Assert.That(value.ToCanonicalString(), Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
    }

    [Test]
    public void ComparisonHelpers_UseBinaryLexicographicOrder()
    {
        UlidValue low = new UlidValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });
        UlidValue high = new UlidValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 });
        UlidValue same = new UlidValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });

        Assert.That(low.CompareTo(high), Is.LessThan(0));
        Assert.That(high.CompareTo(low), Is.GreaterThan(0));
        Assert.That(low.CompareTo(same), Is.EqualTo(0));
        Assert.That(low.EqualsValue(same), Is.True);
        Assert.That(low.EqualsValue(high), Is.False);
        Assert.That(UlidValue.Compare(null, low), Is.LessThan(0));
        Assert.That(UlidValue.Compare(low, null), Is.GreaterThan(0));
        Assert.That(UlidValue.Compare(null, null), Is.EqualTo(0));
    }

    [Test]
    public void Constructor_RejectsInvalidByteLength()
    {
        Assert.That(() => new UlidValue(new byte[15]), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new UlidValue(null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void FromUuidBytes_CopiesRawUuidCompatibleBytes()
    {
        byte[] bytes = ExampleBytes();

        UlidValue value = UlidValue.FromUuidBytes(bytes);
        bytes[0] = 0xFF;

        Assert.That(value.ToUuidBytes(), Is.EqualTo(ExampleBytes()));
        Assert.That(value.ToCanonicalString(), Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
    }

    static byte[] ExampleBytes()
    {
        return new byte[] { 0x01, 0x56, 0x3E, 0x3A, 0xB5, 0xD3, 0xD6, 0x76, 0x4C, 0x61, 0xEF, 0xB9, 0x93, 0x02, 0xBD, 0x5B };
    }
}
