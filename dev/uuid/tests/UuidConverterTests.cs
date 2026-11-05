using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UuidConverterTests
{
    [Test]
    public void V1ToV6_ReordersGregorianTimestampAndPreservesClockSequenceAndNode()
    {
        UuidValue version1 = new Uuid().ParseValue("c232ab00-9414-11ec-b3c8-9f6bdeced846");

        UuidValue version6 = UuidConverter.V1ToV6(version1);

        Assert.That(version6.ToCanonicalString(), Is.EqualTo("1ec9414c-232a-6b00-b3c8-9f6bdeced846"));
        Assert.That(version6.Version(), Is.EqualTo(UuidVersion.Version6));
        Assert.That(version6.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
        Assert.That(UuidConverter.V6Timestamp(version6.ToBytes()), Is.EqualTo(UuidConverter.V1Timestamp(version1.ToBytes())));
    }

    [Test]
    public void V6ToV1_RestoresVersionOneLayout()
    {
        UuidValue version6 = new Uuid().ParseValue("1ec9414c-232a-6b00-b3c8-9f6bdeced846");

        UuidValue version1 = UuidConverter.V6ToV1(version6);

        Assert.That(version1.ToCanonicalString(), Is.EqualTo("c232ab00-9414-11ec-b3c8-9f6bdeced846"));
        Assert.That(version1.Version(), Is.EqualTo(UuidVersion.Version1));
        Assert.That(version1.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
    }

    [Test]
    public void ConversionRoundTripsPreserveOriginalValue()
    {
        UuidValue version1 = new Uuid().ParseValue("c232ab00-9414-11ec-b3c8-9f6bdeced846");

        UuidValue roundTrip = UuidConverter.V6ToV1(UuidConverter.V1ToV6(version1));

        Assert.That(roundTrip.EqualsValue(version1), Is.True);
    }

    [Test]
    public void UuidFacade_DelegatesToConverters()
    {
        UuidValue version1 = new Uuid().ParseValue("c232ab00-9414-11ec-b3c8-9f6bdeced846");
        UuidValue version6 = new Uuid().ParseValue("1ec9414c-232a-6b00-b3c8-9f6bdeced846");

        Assert.That(new Uuid().V1ToV6(version1).EqualsValue(version6), Is.True);
        Assert.That(new Uuid().V6ToV1(version6).EqualsValue(version1), Is.True);
    }

    [Test]
    public void Converters_RejectNullWrongVersionAndWrongVariant()
    {
        UuidValue version4 = new Uuid().ParseValue("01890f13-66b8-4f7a-9f2b-6f3f9c2d8a1b");
        UuidValue wrongVariantV1 = new Uuid().ParseValue("c232ab00-9414-11ec-73c8-9f6bdeced846");
        UuidValue wrongVariantV6 = new Uuid().ParseValue("1ec9414c-232a-6b00-73c8-9f6bdeced846");

        Assert.That(() => UuidConverter.V1ToV6(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => UuidConverter.V1ToV6(version4), Throws.TypeOf<IdentifierException>());
        Assert.That(() => UuidConverter.V6ToV1(version4), Throws.TypeOf<IdentifierException>());
        Assert.That(() => UuidConverter.V1ToV6(wrongVariantV1), Throws.TypeOf<IdentifierException>());
        Assert.That(() => UuidConverter.V6ToV1(wrongVariantV6), Throws.TypeOf<IdentifierException>());
    }
}
