using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UuidNamespacesTests
{
    [Test]
    public void NamespaceFactories_ReturnStandardUuidNamespaceValues()
    {
        Assert.That(UuidNamespaces.Dns.ToCanonicalString(), Is.EqualTo("6ba7b810-9dad-11d1-80b4-00c04fd430c8"));
        Assert.That(UuidNamespaces.Url.ToCanonicalString(), Is.EqualTo("6ba7b811-9dad-11d1-80b4-00c04fd430c8"));
        Assert.That(UuidNamespaces.Oid.ToCanonicalString(), Is.EqualTo("6ba7b812-9dad-11d1-80b4-00c04fd430c8"));
        Assert.That(UuidNamespaces.X500.ToCanonicalString(), Is.EqualTo("6ba7b814-9dad-11d1-80b4-00c04fd430c8"));
    }

    [Test]
    public void NamespaceFactories_ReturnIndependentValues()
    {
        UuidValue first = UuidNamespaces.Dns;
        UuidValue second = UuidNamespaces.Dns;
        byte[] bytes = first.ToBytes();

        bytes[0] = 0;

        Assert.That(first.EqualsValue(second), Is.True);
        Assert.That(first.ToCanonicalString(), Is.EqualTo("6ba7b810-9dad-11d1-80b4-00c04fd430c8"));
        Assert.That(second.ToCanonicalString(), Is.EqualTo("6ba7b810-9dad-11d1-80b4-00c04fd430c8"));
    }

    [Test]
    public void NamespaceFactories_WorkWithNameBasedGenerators()
    {
        UuidValue version3 = new Uuid().FromNameV3(UuidNamespaces.Dns, "www.example.com");
        UuidValue version5 = new Uuid().FromNameV5(UuidNamespaces.Dns, "www.example.com");

        Assert.That(version3.ToCanonicalString(), Is.EqualTo("5df41881-3aed-3515-88a7-2f4a814cf09e"));
        Assert.That(version5.ToCanonicalString(), Is.EqualTo("2ed6657d-e927-568b-95e1-2665a8aea6a2"));
    }
}
