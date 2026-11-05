using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UuidNameGeneratorTests
{
    [Test]
    public void FromNameV3_MatchesRfcDnsExample()
    {
        UuidValue dnsNamespace = new Uuid().ParseValue("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        UuidValue value = UuidGenerator.FromNameV3(dnsNamespace, "www.example.com");

        Assert.That(value.ToCanonicalString(), Is.EqualTo("5df41881-3aed-3515-88a7-2f4a814cf09e"));
        Assert.That(value.Version(), Is.EqualTo(UuidVersion.Version3));
        Assert.That(value.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
    }

    [Test]
    public void FromNameV3_IsDeterministicForSameNamespaceAndName()
    {
        UuidValue dnsNamespace = new Uuid().ParseValue("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        UuidValue first = UuidGenerator.FromNameV3(dnsNamespace, "www.example.com");
        UuidValue second = UuidGenerator.FromNameV3(dnsNamespace, "www.example.com");

        Assert.That(first.EqualsValue(second), Is.True);
    }

    [Test]
    public void FromNameV3_ChangesWhenNameChanges()
    {
        UuidValue dnsNamespace = new Uuid().ParseValue("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        UuidValue first = UuidGenerator.FromNameV3(dnsNamespace, "www.example.com");
        UuidValue second = UuidGenerator.FromNameV3(dnsNamespace, "example.com");

        Assert.That(first.EqualsValue(second), Is.False);
    }

    [Test]
    public void FromNameV3_UsesUtf8NameBytes()
    {
        UuidValue dnsNamespace = new Uuid().ParseValue("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        UuidValue composed = UuidGenerator.FromNameV3(dnsNamespace, "caf\u00E9");
        UuidValue asciiFallback = UuidGenerator.FromNameV3(dnsNamespace, "caf?");

        Assert.That(composed.EqualsValue(asciiFallback), Is.False);
        Assert.That(composed.Version(), Is.EqualTo(UuidVersion.Version3));
        Assert.That(composed.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
    }

    [Test]
    public void FromNameV3_FacadeDelegatesToGenerator()
    {
        UuidValue dnsNamespace = new Uuid().ParseValue("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        UuidValue value = new Uuid().FromNameV3(dnsNamespace, "www.example.com");

        Assert.That(value.ToCanonicalString(), Is.EqualTo("5df41881-3aed-3515-88a7-2f4a814cf09e"));
    }

    [Test]
    public void FromNameV3_RejectsNullArguments()
    {
        UuidValue dnsNamespace = new Uuid().ParseValue("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        Assert.That(() => UuidGenerator.FromNameV3(null, "www.example.com"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => UuidGenerator.FromNameV3(dnsNamespace, null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void FromNameV5_MatchesRfcDnsExample()
    {
        UuidValue dnsNamespace = new Uuid().ParseValue("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        UuidValue value = UuidGenerator.FromNameV5(dnsNamespace, "www.example.com");

        Assert.That(value.ToCanonicalString(), Is.EqualTo("2ed6657d-e927-568b-95e1-2665a8aea6a2"));
        Assert.That(value.Version(), Is.EqualTo(UuidVersion.Version5));
        Assert.That(value.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
    }

    [Test]
    public void FromNameV5_IsDeterministicForSameNamespaceAndName()
    {
        UuidValue dnsNamespace = new Uuid().ParseValue("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        UuidValue first = UuidGenerator.FromNameV5(dnsNamespace, "www.example.com");
        UuidValue second = UuidGenerator.FromNameV5(dnsNamespace, "www.example.com");

        Assert.That(first.EqualsValue(second), Is.True);
    }

    [Test]
    public void FromNameV5_DiffersFromVersion3ForSameNamespaceAndName()
    {
        UuidValue dnsNamespace = new Uuid().ParseValue("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        UuidValue version3 = UuidGenerator.FromNameV3(dnsNamespace, "www.example.com");
        UuidValue version5 = UuidGenerator.FromNameV5(dnsNamespace, "www.example.com");

        Assert.That(version3.EqualsValue(version5), Is.False);
    }

    [Test]
    public void FromNameV5_FacadeDelegatesToGenerator()
    {
        UuidValue dnsNamespace = new Uuid().ParseValue("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        UuidValue value = new Uuid().FromNameV5(dnsNamespace, "www.example.com");

        Assert.That(value.ToCanonicalString(), Is.EqualTo("2ed6657d-e927-568b-95e1-2665a8aea6a2"));
    }

    [Test]
    public void FromNameV5_RejectsNullArguments()
    {
        UuidValue dnsNamespace = new Uuid().ParseValue("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        Assert.That(() => UuidGenerator.FromNameV5(null, "www.example.com"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => UuidGenerator.FromNameV5(dnsNamespace, null), Throws.TypeOf<ArgumentNullException>());
    }
}
