using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UuidValueTests
{
    [Test]
    public void Constructor_CopiesInputBytes()
    {
        byte[] bytes = ExampleBytes();
        UuidValue value = new UuidValue(bytes);

        bytes[0] = 0xFF;

        Assert.That(value.ToCompactString(), Is.EqualTo("00112233445566778899aabbccddeeff"));
    }

    [Test]
    public void ToBytes_ReturnsIndependentCopy()
    {
        UuidValue value = new UuidValue(ExampleBytes());

        byte[] copy = value.ToBytes();
        copy[0] = 0xFF;

        Assert.That(copy[0], Is.EqualTo(0xFF));
        Assert.That(value.ToBytes()[0], Is.EqualTo(0x00));
    }

    [Test]
    public void CopyTo_CopiesIntoDestinationWithoutResizing()
    {
        UuidValue value = new UuidValue(ExampleBytes());
        byte[] destination = new byte[18];
        destination[16] = 0xAA;
        destination[17] = 0xBB;

        value.CopyTo(destination);

        Assert.That(destination[0], Is.EqualTo(0x00));
        Assert.That(destination[15], Is.EqualTo(0xFF));
        Assert.That(destination[16], Is.EqualTo(0xAA));
        Assert.That(destination[17], Is.EqualTo(0xBB));
    }

    [Test]
    public void CopyTo_RejectsNullOrShortDestination()
    {
        UuidValue value = new UuidValue(ExampleBytes());

        Assert.That(() => value.CopyTo(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => value.CopyTo(new byte[15]), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void FormatMethods_ReturnCanonicalAndCompactText()
    {
        UuidValue value = new UuidValue(ExampleBytes());

        Assert.That(value.ToCanonicalString(), Is.EqualTo("00112233-4455-6677-8899-aabbccddeeff"));
        Assert.That(value.ToUpperString(), Is.EqualTo("00112233-4455-6677-8899-AABBCCDDEEFF"));
        Assert.That(value.ToCompactString(), Is.EqualTo("00112233445566778899aabbccddeeff"));
        Assert.That(value.ToCompactUpperString(), Is.EqualTo("00112233445566778899AABBCCDDEEFF"));
    }

    [Test]
    public void GuidConversion_PreservesCanonicalUuidText()
    {
        Guid guid = new Guid("00112233-4455-6677-8899-aabbccddeeff");

        UuidValue value = UuidValue.FromGuid(guid);

        Assert.That(value.ToCanonicalString(), Is.EqualTo("00112233-4455-6677-8899-aabbccddeeff"));
        Assert.That(value.ToGuid(), Is.EqualTo(guid));
    }

    [Test]
    public void ComparisonHelpers_UseCanonicalByteOrder()
    {
        UuidValue low = new UuidValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });
        UuidValue high = new UuidValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 });
        UuidValue same = new UuidValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });

        Assert.That(low.CompareTo(high), Is.LessThan(0));
        Assert.That(high.CompareTo(low), Is.GreaterThan(0));
        Assert.That(low.CompareTo(same), Is.EqualTo(0));
        Assert.That(low.EqualsValue(same), Is.True);
        Assert.That(low.EqualsValue(high), Is.False);
        Assert.That(UuidValue.Compare(null, low), Is.LessThan(0));
        Assert.That(UuidValue.Compare(low, null), Is.GreaterThan(0));
        Assert.That(UuidValue.Compare(null, null), Is.EqualTo(0));
    }

    [Test]
    public void Version_ReturnsKnownUuidVersions()
    {
        Assert.That(WithVersion(1).Version(), Is.EqualTo(UuidVersion.Version1));
        Assert.That(WithVersion(3).Version(), Is.EqualTo(UuidVersion.Version3));
        Assert.That(WithVersion(4).Version(), Is.EqualTo(UuidVersion.Version4));
        Assert.That(WithVersion(5).Version(), Is.EqualTo(UuidVersion.Version5));
        Assert.That(WithVersion(6).Version(), Is.EqualTo(UuidVersion.Version6));
        Assert.That(WithVersion(7).Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(WithVersion(8).Version(), Is.EqualTo(UuidVersion.Version8));
        Assert.That(WithVersion(2).Version(), Is.EqualTo(UuidVersion.Unknown));
        Assert.That(WithVersion(15).Version(), Is.EqualTo(UuidVersion.Unknown));
    }

    [Test]
    public void Variant_ReturnsUuidVariantCategoryFromOctetEight()
    {
        Assert.That(WithVariantByte(0x00).Variant(), Is.EqualTo(UuidVariant.Ncs));
        Assert.That(WithVariantByte(0x7F).Variant(), Is.EqualTo(UuidVariant.Ncs));
        Assert.That(WithVariantByte(0x80).Variant(), Is.EqualTo(UuidVariant.Rfc9562));
        Assert.That(WithVariantByte(0xBF).Variant(), Is.EqualTo(UuidVariant.Rfc9562));
        Assert.That(WithVariantByte(0xC0).Variant(), Is.EqualTo(UuidVariant.Microsoft));
        Assert.That(WithVariantByte(0xDF).Variant(), Is.EqualTo(UuidVariant.Microsoft));
        Assert.That(WithVariantByte(0xE0).Variant(), Is.EqualTo(UuidVariant.Future));
        Assert.That(WithVariantByte(0xFF).Variant(), Is.EqualTo(UuidVariant.Future));
    }

    [Test]
    public void NilAndMaxDetection_CheckAllBytes()
    {
        byte[] maxBytes = new byte[16];
        int index = 0;
        while (index < maxBytes.Length)
        {
            maxBytes[index] = 255;
            index = index + 1;
        }

        Assert.That(new UuidValue(new byte[16]).IsNil(), Is.True);
        Assert.That(new UuidValue(new byte[16]).IsMax(), Is.False);
        Assert.That(new UuidValue(maxBytes).IsMax(), Is.True);
        Assert.That(new UuidValue(maxBytes).IsNil(), Is.False);
        Assert.That(new UuidValue(ExampleBytes()).IsNil(), Is.False);
        Assert.That(new UuidValue(ExampleBytes()).IsMax(), Is.False);
    }

    [Test]
    public void Constructor_RejectsInvalidByteLength()
    {
        Assert.That(() => new UuidValue(new byte[15]), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new UuidValue(null), Throws.TypeOf<ArgumentNullException>());
    }

    static byte[] ExampleBytes()
    {
        return new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
    }

    static UuidValue WithVersion(int version)
    {
        byte[] bytes = ExampleBytes();
        bytes[6] = (byte)((version << 4) | 7);
        bytes[8] = 0x80;
        return new UuidValue(bytes);
    }

    static UuidValue WithVariantByte(int variantByte)
    {
        byte[] bytes = ExampleBytes();
        bytes[8] = (byte)variantByte;
        return new UuidValue(bytes);
    }
}
