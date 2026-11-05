using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UuidRfcVectorTests
{
    [Test]
    public void Rfc9562Version1Vector_ParsesVersionVariantAndTimestamp()
    {
        UuidValue value = new Uuid().ParseValue("c232ab00-9414-11ec-b3c8-9f6bdeced846");

        Assert.That(value.Version(), Is.EqualTo(UuidVersion.Version1));
        Assert.That(value.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
        Assert.That(UuidTimestamp.ExtractV1GregorianTimestamp(value), Is.EqualTo(138648505420000000));
        Assert.That(value.UnixMilliseconds(), Is.EqualTo(1645557742000));
    }

    [Test]
    public void Rfc9562Version3Vector_MatchesDnsNamespaceAndName()
    {
        UuidValue value = new Uuid().FromNameV3(UuidNamespaces.Dns, "www.example.com");

        Assert.That(value.ToCanonicalString(), Is.EqualTo("5df41881-3aed-3515-88a7-2f4a814cf09e"));
        Assert.That(value.Version(), Is.EqualTo(UuidVersion.Version3));
        Assert.That(value.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
    }

    [Test]
    public void Rfc9562Version4Vector_AppliesVersionAndVariantToRandomBytes()
    {
        byte[] bytes = HexTools.ParseUuidCompact("919108f752d133205bacf847db4148a8");

        UuidGenerator.ApplyVersionAndVariant(bytes, 4);
        UuidValue value = new UuidValue(bytes);

        Assert.That(value.ToCanonicalString(), Is.EqualTo("919108f7-52d1-4320-9bac-f847db4148a8"));
        Assert.That(value.Version(), Is.EqualTo(UuidVersion.Version4));
        Assert.That(value.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
    }

    [Test]
    public void Rfc9562Version5Vector_MatchesDnsNamespaceAndName()
    {
        UuidValue value = new Uuid().FromNameV5(UuidNamespaces.Dns, "www.example.com");

        Assert.That(value.ToCanonicalString(), Is.EqualTo("2ed6657d-e927-568b-95e1-2665a8aea6a2"));
        Assert.That(value.Version(), Is.EqualTo(UuidVersion.Version5));
        Assert.That(value.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
    }

    [Test]
    public void Rfc9562Version6Vector_MatchesVersionOneTimestampReordering()
    {
        UuidValue version1 = new Uuid().ParseValue("c232ab00-9414-11ec-b3c8-9f6bdeced846");
        UuidValue version6 = new Uuid().ParseValue("1ec9414c-232a-6b00-b3c8-9f6bdeced846");

        Assert.That(new Uuid().V1ToV6(version1).EqualsValue(version6), Is.True);
        Assert.That(new Uuid().V6ToV1(version6).EqualsValue(version1), Is.True);
        Assert.That(UuidTimestamp.ExtractV6GregorianTimestamp(version6), Is.EqualTo(138648505420000000));
        Assert.That(version6.UnixMilliseconds(), Is.EqualTo(1645557742000));
    }

    [Test]
    public void Rfc9562Version7Vector_ParsesTimestampVersionAndVariant()
    {
        UuidValue value = new Uuid().ParseValue("017f22e2-79b0-7cc3-98c4-dc0c0c07398f");

        Assert.That(value.Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(value.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
        Assert.That(value.UnixMilliseconds(), Is.EqualTo(1645557742000));
        Assert.That(value.Timestamp().Time, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1645557742000)));
    }

    [Test]
    public void GeneratedVersion4Values_ParseValidateAndRoundTrip()
    {
        int index = 0;
        while (index < 25)
        {
            UuidValue generated = new Uuid().NewV4();
            UuidParseResult parsed = new Uuid().Parse(generated.ToCanonicalString());
            IdentifierValidationResult validation = new Uuid().ValidateVersion(generated.ToCanonicalString(), UuidVersion.Version4);

            Assert.That(parsed.Ok, Is.True);
            Assert.That(parsed.Value, Is.Not.Null);
            Assert.That(parsed.Value.EqualsValue(generated), Is.True);
            Assert.That(validation.Ok, Is.True);
            Assert.That(generated.IsNil(), Is.False);
            Assert.That(generated.IsMax(), Is.False);
            index = index + 1;
        }
    }

    [Test]
    public void GeneratedVersion7Values_ParseValidateAndExposeSuppliedTimestamp()
    {
        int index = 0;
        while (index < 25)
        {
            long unixMilliseconds = 1645557742000 + index;
            UuidValue generated = new Uuid().NewV7AtUnixMilliseconds(unixMilliseconds);
            UuidParseResult parsed = new Uuid().Parse(generated.ToCanonicalString());
            IdentifierValidationResult validation = new Uuid().ValidateVersion(generated.ToCanonicalString(), UuidVersion.Version7);

            Assert.That(parsed.Ok, Is.True);
            Assert.That(parsed.Value, Is.Not.Null);
            Assert.That(parsed.Value.EqualsValue(generated), Is.True);
            Assert.That(validation.Ok, Is.True);
            Assert.That(generated.UnixMilliseconds(), Is.EqualTo(unixMilliseconds));
            Assert.That(generated.IsNil(), Is.False);
            Assert.That(generated.IsMax(), Is.False);
            index = index + 1;
        }
    }
}
