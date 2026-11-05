using token;

namespace token.Tests;

public class AlgorithmMetadataTests
{
    [Test]
    public void JoseMetadataSeparatesRegistryStatusFromImplementationScope()
    {
        AlgorithmMetadata recommended = AlgorithmMetadataRegistry.GetJose(JoseAlgorithms.HmacSha256);
        AlgorithmMetadata optional = AlgorithmMetadataRegistry.GetJose(JoseAlgorithms.RsaPssSha256);
        AlgorithmMetadata unavailable = AlgorithmMetadataRegistry.GetJose(JoseAlgorithms.MlDsa65);

        Assert.Multiple(() =>
        {
            Assert.That(recommended.Name, Is.EqualTo("HS256"));
            Assert.That(recommended.UsesIntegerLabel, Is.False);
            Assert.That(recommended.Known, Is.True);
            Assert.That(recommended.Supported, Is.True);
            Assert.That(recommended.Recommended, Is.True);
            Assert.That(optional.Known, Is.True);
            Assert.That(optional.Supported, Is.True);
            Assert.That(optional.Recommended, Is.False);
            Assert.That(unavailable.Known, Is.True);
            Assert.That(unavailable.Supported, Is.False);
            Assert.That(unavailable.Recommended, Is.False);
        });
    }

    [Test]
    public void JoseMetadataIdentifiesLegacyDeprecatedAndProhibitedAlgorithms()
    {
        AlgorithmMetadata none = AlgorithmMetadataRegistry.GetJose(JoseAlgorithms.None);
        AlgorithmMetadata rsaPkcs1 = AlgorithmMetadataRegistry.GetJose(JoseAlgorithms.RsaPkcs1);
        AlgorithmMetadata deprecated = AlgorithmMetadataRegistry.GetJose(JoseAlgorithms.EdDsa);
        AlgorithmMetadata prohibited = AlgorithmMetadataRegistry.GetJose(JoseAlgorithms.RsaSha1);

        Assert.Multiple(() =>
        {
            Assert.That(none.Known, Is.True);
            Assert.That(none.Supported, Is.False);
            Assert.That(none.CompatibilityOnly, Is.True);
            Assert.That(none.Prohibited, Is.False);
            Assert.That(rsaPkcs1.Supported, Is.True);
            Assert.That(rsaPkcs1.CompatibilityOnly, Is.True);
            Assert.That(rsaPkcs1.Recommended, Is.False);
            Assert.That(deprecated.Deprecated, Is.True);
            Assert.That(deprecated.CompatibilityOnly, Is.True);
            Assert.That(deprecated.Supported, Is.False);
            Assert.That(prohibited.Prohibited, Is.True);
            Assert.That(prohibited.Supported, Is.False);
            Assert.That(prohibited.CompatibilityOnly, Is.False);
        });
    }

    [Test]
    public void CoseMetadataTracksCurrentAndSupersededAssignments()
    {
        AlgorithmMetadata current = AlgorithmMetadataRegistry.GetCose(CoseAlgorithms.EcdsaP256Sha256);
        AlgorithmMetadata superseded = AlgorithmMetadataRegistry.GetCose(CoseAlgorithms.EcdsaSha256);
        AlgorithmMetadata filterOnly = AlgorithmMetadataRegistry.GetCose(CoseAlgorithms.Sha1);
        AlgorithmMetadata unavailable = AlgorithmMetadataRegistry.GetCose(CoseAlgorithms.MlDsa87);

        Assert.Multiple(() =>
        {
            Assert.That(current.Name, Is.EqualTo("ESP256"));
            Assert.That(current.Label, Is.EqualTo(-9));
            Assert.That(current.UsesIntegerLabel, Is.True);
            Assert.That(current.Known, Is.True);
            Assert.That(current.Supported, Is.True);
            Assert.That(current.Recommended, Is.True);
            Assert.That(current.Deprecated, Is.False);
            Assert.That(superseded.Supported, Is.True);
            Assert.That(superseded.Recommended, Is.False);
            Assert.That(superseded.Deprecated, Is.True);
            Assert.That(superseded.CompatibilityOnly, Is.True);
            Assert.That(filterOnly.Supported, Is.True);
            Assert.That(filterOnly.Recommended, Is.False);
            Assert.That(filterOnly.CompatibilityOnly, Is.True);
            Assert.That(unavailable.Known, Is.True);
            Assert.That(unavailable.Supported, Is.False);
            Assert.That(unavailable.Recommended, Is.True);
        });
    }

    [Test]
    public void CoseMetadataMarksDeprecatedRawAlgorithmsAndSupportedAead()
    {
        AlgorithmMetadata raw = AlgorithmMetadataRegistry.GetCose(CoseAlgorithms.Aes128Cbc);
        AlgorithmMetadata aead = AlgorithmMetadataRegistry.GetCose(CoseAlgorithms.ChaCha20Poly1305);
        AlgorithmMetadata utility = AlgorithmMetadataRegistry.GetCose(CoseAlgorithms.InitializationVectorGeneration);

        Assert.Multiple(() =>
        {
            Assert.That(raw.Known, Is.True);
            Assert.That(raw.Supported, Is.False);
            Assert.That(raw.Deprecated, Is.True);
            Assert.That(raw.CompatibilityOnly, Is.True);
            Assert.That(raw.Prohibited, Is.False);
            Assert.That(aead.Known, Is.True);
            Assert.That(aead.Supported, Is.True);
            Assert.That(aead.Recommended, Is.True);
            Assert.That(utility.Known, Is.True);
            Assert.That(utility.Supported, Is.False);
            Assert.That(utility.Recommended, Is.False);
        });
    }

    [Test]
    public void EveryRegisteredAlgorithmProducesKnownMetadata()
    {
        foreach (string name in JoseAlgorithmsOperations.GetAll())
        {
            AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetJose(name);
            Assert.Multiple(() =>
            {
                Assert.That(metadata.Known, Is.True, name);
                Assert.That(metadata.Name, Is.EqualTo(name));
                Assert.That(metadata.UsesIntegerLabel, Is.False);
                Assert.That(metadata.Recommended && metadata.Deprecated, Is.False, name);
                Assert.That(metadata.Recommended && metadata.Prohibited, Is.False, name);
            });
        }

        foreach (int label in CoseAlgorithmsOperations.GetAll())
        {
            AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetCose(label);
            Assert.Multiple(() =>
            {
                Assert.That(metadata.Known, Is.True, label.ToString());
                Assert.That(metadata.Name, Is.EqualTo(CoseAlgorithmsOperations.GetName(label)));
                Assert.That(metadata.Label, Is.EqualTo(label));
                Assert.That(metadata.UsesIntegerLabel, Is.True);
                Assert.That(metadata.Recommended && metadata.Deprecated, Is.False, label.ToString());
                Assert.That(metadata.Prohibited, Is.False, label.ToString());
            });
        }
    }

    [Test]
    public void UnknownAlgorithmsHaveNoPositiveClassification()
    {
        AlgorithmMetadata jose = AlgorithmMetadataRegistry.GetJose("not-registered");
        AlgorithmMetadata nullJose = AlgorithmMetadataRegistry.GetJose(null);
        AlgorithmMetadata cose = AlgorithmMetadataRegistry.GetCose(999999);

        AssertUnknown(jose);
        AssertUnknown(nullJose);
        AssertUnknown(cose);
        Assert.Multiple(() =>
        {
            Assert.That(jose.Name, Is.EqualTo("not-registered"));
            Assert.That(nullJose.Name, Is.Null);
            Assert.That(cose.Name, Is.Null);
            Assert.That(cose.Label, Is.EqualTo(999999));
            Assert.That(cose.UsesIntegerLabel, Is.True);
        });
    }

    [Test]
    public void MetadataResultsDoNotExposeRegistryState()
    {
        AlgorithmMetadata first = AlgorithmMetadataRegistry.GetJose(JoseAlgorithms.HmacSha256);
        first.Name = "changed";
        first.Known = false;
        first.Supported = false;
        first.Recommended = false;

        AlgorithmMetadata second = AlgorithmMetadataRegistry.GetJose(JoseAlgorithms.HmacSha256);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Name, Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(second.Known, Is.True);
            Assert.That(second.Supported, Is.True);
            Assert.That(second.Recommended, Is.True);
        });
    }

    static void AssertUnknown(AlgorithmMetadata metadata)
    {
        Assert.Multiple(() =>
        {
            Assert.That(metadata.Known, Is.False);
            Assert.That(metadata.Supported, Is.False);
            Assert.That(metadata.Recommended, Is.False);
            Assert.That(metadata.Deprecated, Is.False);
            Assert.That(metadata.Prohibited, Is.False);
            Assert.That(metadata.CompatibilityOnly, Is.False);
        });
    }
}
