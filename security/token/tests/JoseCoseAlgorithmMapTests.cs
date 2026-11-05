using token;

namespace token.Tests;

public class JoseCoseAlgorithmMapTests
{
    [TestCase(JoseAlgorithms.HmacSha256, CoseAlgorithms.HmacSha256)]
    [TestCase(JoseAlgorithms.RsaSha384, CoseAlgorithms.RsaSha384)]
    [TestCase(JoseAlgorithms.RsaPssSha512, CoseAlgorithms.RsaPssSha512)]
    [TestCase(JoseAlgorithms.RsaOaepSha256, CoseAlgorithms.RsaOaepSha256)]
    [TestCase(JoseAlgorithms.Aes192KeyWrap, CoseAlgorithms.Aes192KeyWrap)]
    [TestCase(JoseAlgorithms.Direct, CoseAlgorithms.Direct)]
    [TestCase(JoseAlgorithms.EcdhEsAes256KeyWrap, CoseAlgorithms.EcdhEsAes256KeyWrap)]
    [TestCase(JoseAlgorithms.Aes128Gcm, CoseAlgorithms.Aes128Gcm)]
    [TestCase(JoseAlgorithms.EcdsaSecp256k1Sha256, CoseAlgorithms.EcdsaSecp256k1Sha256)]
    [TestCase(JoseAlgorithms.MlDsa44, CoseAlgorithms.MlDsa44)]
    [TestCase(JoseAlgorithms.Ed25519, CoseAlgorithms.Ed25519)]
    [TestCase(JoseAlgorithms.Ed448, CoseAlgorithms.Ed448)]
    [TestCase(JoseAlgorithms.RsaSha1, CoseAlgorithms.RsaSha1)]
    [TestCase(JoseAlgorithms.Aes256Cbc, CoseAlgorithms.Aes256Cbc)]
    [TestCase(JoseAlgorithms.Aes128Ctr, CoseAlgorithms.Aes128Ctr)]
    public void MapsExactAlgorithmEquivalentsInBothDirections(string joseName, int coseLabel)
    {
        AlgorithmMappingResult fromJose = JoseCoseAlgorithmMap.FromJose(joseName);
        AlgorithmMappingResult fromCose = JoseCoseAlgorithmMap.FromCose(coseLabel);

        Assert.Multiple(() =>
        {
            Assert.That(fromJose.Mapped, Is.True);
            Assert.That(fromJose.JoseName, Is.EqualTo(joseName));
            Assert.That(fromJose.CoseLabel, Is.EqualTo(coseLabel));
            Assert.That(fromCose.Mapped, Is.True);
            Assert.That(fromCose.JoseName, Is.EqualTo(joseName));
            Assert.That(fromCose.CoseLabel, Is.EqualTo(coseLabel));
        });
    }

    [TestCase(JoseAlgorithms.EcdsaSha256, CoseAlgorithms.EcdsaP256Sha256, CoseAlgorithms.EcdsaSha256)]
    [TestCase(JoseAlgorithms.EcdsaSha384, CoseAlgorithms.EcdsaP384Sha384, CoseAlgorithms.EcdsaSha384)]
    [TestCase(JoseAlgorithms.EcdsaSha512, CoseAlgorithms.EcdsaP521Sha512, CoseAlgorithms.EcdsaSha512)]
    public void FullySpecifiedEcdsaMappingPrefersCurrentCoseLabel(
        string joseName,
        int currentLabel,
        int legacyLabel)
    {
        AlgorithmMappingResult canonical = JoseCoseAlgorithmMap.FromJose(joseName);
        AlgorithmMappingResult legacy = JoseCoseAlgorithmMap.FromCose(legacyLabel);
        int[] labels = JoseCoseAlgorithmMap.GetCoseLabels(joseName);

        Assert.Multiple(() =>
        {
            Assert.That(canonical.Mapped, Is.True);
            Assert.That(canonical.CoseLabel, Is.EqualTo(currentLabel));
            Assert.That(legacy.Mapped, Is.True);
            Assert.That(legacy.JoseName, Is.EqualTo(joseName));
            Assert.That(legacy.CoseLabel, Is.EqualTo(legacyLabel));
            Assert.That(labels, Is.EqualTo(new int[] { currentLabel, legacyLabel }));
        });
    }

    [TestCase(JoseAlgorithms.None)]
    [TestCase(JoseAlgorithms.RsaOaepSha384)]
    [TestCase(JoseAlgorithms.EcdhEs)]
    [TestCase(JoseAlgorithms.Aes128GcmKeyWrap)]
    [TestCase(JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap)]
    [TestCase(JoseAlgorithms.Aes128CbcHmacSha256)]
    [TestCase(JoseAlgorithms.HmacSha1)]
    public void KnownJoseAlgorithmsWithoutExactCoseEquivalentAreNotMapped(string joseName)
    {
        AlgorithmMappingResult result = JoseCoseAlgorithmMap.FromJose(joseName);

        Assert.Multiple(() =>
        {
            Assert.That(JoseAlgorithmsOperations.IsRegistered(joseName), Is.True);
            Assert.That(result.Mapped, Is.False);
            Assert.That(result.JoseName, Is.EqualTo(joseName));
            Assert.That(result.CoseLabel, Is.Zero);
            Assert.That(JoseCoseAlgorithmMap.GetCoseLabels(joseName), Is.Empty);
        });
    }

    [TestCase(CoseAlgorithms.AesCcm16_64_128)]
    [TestCase(CoseAlgorithms.AesMac128_128)]
    [TestCase(CoseAlgorithms.ChaCha20Poly1305)]
    [TestCase(CoseAlgorithms.Sha256)]
    [TestCase(CoseAlgorithms.InitializationVectorGeneration)]
    public void KnownCoseAlgorithmsWithoutExactJoseEquivalentAreNotMapped(int coseLabel)
    {
        AlgorithmMappingResult result = JoseCoseAlgorithmMap.FromCose(coseLabel);

        Assert.Multiple(() =>
        {
            Assert.That(CoseAlgorithmsOperations.IsRegistered(coseLabel), Is.True);
            Assert.That(result.Mapped, Is.False);
            Assert.That(result.JoseName, Is.Null);
            Assert.That(result.CoseLabel, Is.EqualTo(coseLabel));
        });
    }

    [Test]
    public void MappingIsCaseSensitiveAndHandlesUnknownIdentifiers()
    {
        AlgorithmMappingResult lowerCase = JoseCoseAlgorithmMap.FromJose("hs256");
        AlgorithmMappingResult nullName = JoseCoseAlgorithmMap.FromJose(null);
        AlgorithmMappingResult unknownLabel = JoseCoseAlgorithmMap.FromCose(999999);

        Assert.Multiple(() =>
        {
            Assert.That(lowerCase.Mapped, Is.False);
            Assert.That(lowerCase.JoseName, Is.EqualTo("hs256"));
            Assert.That(nullName.Mapped, Is.False);
            Assert.That(nullName.JoseName, Is.Null);
            Assert.That(JoseCoseAlgorithmMap.GetCoseLabels(null), Is.Empty);
            Assert.That(unknownLabel.Mapped, Is.False);
            Assert.That(unknownLabel.JoseName, Is.Null);
            Assert.That(unknownLabel.CoseLabel, Is.EqualTo(999999));
        });
    }

    [Test]
    public void CanonicalMappingSnapshotIsCompleteAndRoundTrips()
    {
        string[] joseNames = JoseCoseAlgorithmMap.GetJoseAlgorithms();
        int[] coseLabels = JoseCoseAlgorithmMap.GetCanonicalCoseAlgorithms();

        Assert.Multiple(() =>
        {
            Assert.That(joseNames, Has.Length.EqualTo(39).And.Unique);
            Assert.That(coseLabels, Has.Length.EqualTo(39).And.Unique);
            Assert.That(coseLabels, Has.Length.EqualTo(joseNames.Length));
        });

        int index = 0;
        while (index < joseNames.Length)
        {
            AlgorithmMappingResult forward = JoseCoseAlgorithmMap.FromJose(joseNames[index]);
            AlgorithmMappingResult reverse = JoseCoseAlgorithmMap.FromCose(coseLabels[index]);
            Assert.Multiple(() =>
            {
                Assert.That(JoseAlgorithmsOperations.IsRegistered(joseNames[index]), Is.True);
                Assert.That(CoseAlgorithmsOperations.IsRegistered(coseLabels[index]), Is.True);
                Assert.That(forward.Mapped, Is.True);
                Assert.That(forward.CoseLabel, Is.EqualTo(coseLabels[index]));
                Assert.That(reverse.Mapped, Is.True);
                Assert.That(reverse.JoseName, Is.EqualTo(joseNames[index]));
            });
            index += 1;
        }
    }

    [Test]
    public void MappingSnapshotsAndResultsAreDefensive()
    {
        string[] joseNames = JoseCoseAlgorithmMap.GetJoseAlgorithms();
        int[] coseLabels = JoseCoseAlgorithmMap.GetCanonicalCoseAlgorithms();
        int[] ecdsaLabels = JoseCoseAlgorithmMap.GetCoseLabels(JoseAlgorithms.EcdsaSha256);
        AlgorithmMappingResult result = JoseCoseAlgorithmMap.FromJose(JoseAlgorithms.HmacSha256);

        joseNames[0] = "changed";
        coseLabels[0] = 999999;
        ecdsaLabels[0] = 999999;
        result.Mapped = false;
        result.JoseName = "changed";
        result.CoseLabel = 999999;

        AlgorithmMappingResult fresh = JoseCoseAlgorithmMap.FromJose(JoseAlgorithms.HmacSha256);
        Assert.Multiple(() =>
        {
            Assert.That(JoseCoseAlgorithmMap.FromJose("changed").Mapped, Is.False);
            Assert.That(JoseCoseAlgorithmMap.FromCose(999999).Mapped, Is.False);
            Assert.That(JoseCoseAlgorithmMap.FromJose(JoseAlgorithms.EcdsaSha256).CoseLabel,
                Is.EqualTo(CoseAlgorithms.EcdsaP256Sha256));
            Assert.That(fresh.Mapped, Is.True);
            Assert.That(fresh.JoseName, Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(fresh.CoseLabel, Is.EqualTo(CoseAlgorithms.HmacSha256));
        });
    }
}
