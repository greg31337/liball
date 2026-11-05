using token;

namespace token.Tests;

public class TokenRegistryOperationsTests
{
    [Test]
    public void EveryRegisteredAlgorithmHasSelfConsistentMetadataAndMappings()
    {
        ITokenRegistryOperations registry = new TokenRegistryOperations();

        foreach (string algorithm in registry.GetJoseAlgorithms())
        {
            AlgorithmMetadata metadata = registry.GetJoseAlgorithmMetadata(algorithm);
            int[] mappings = registry.GetMappedCoseAlgorithms(algorithm);
            Assert.Multiple(() =>
            {
                Assert.That(metadata.Known, Is.True, algorithm);
                Assert.That(metadata.Name, Is.EqualTo(algorithm));
                Assert.That(metadata.UsesIntegerLabel, Is.False);
                if (mappings.Length > 0)
                {
                    Assert.That(registry.MapJoseAlgorithmToCose(algorithm).CoseLabel,
                        Is.EqualTo(mappings[0]));
                }
            });

            foreach (int mapping in mappings)
            {
                AlgorithmMappingResult reverse = registry.MapCoseAlgorithmToJose(mapping);
                Assert.That(reverse.JoseName, Is.EqualTo(algorithm), algorithm);
            }
        }

        foreach (int algorithm in registry.GetCoseAlgorithms())
        {
            AlgorithmMetadata metadata = registry.GetCoseAlgorithmMetadata(algorithm);
            AlgorithmMappingResult mapping = registry.MapCoseAlgorithmToJose(algorithm);
            Assert.Multiple(() =>
            {
                Assert.That(metadata.Known, Is.True, algorithm.ToString());
                Assert.That(metadata.Label, Is.EqualTo(algorithm));
                Assert.That(metadata.UsesIntegerLabel, Is.True);
                Assert.That(metadata.Name, Is.EqualTo(registry.GetCoseAlgorithmName(algorithm)));
                if (mapping.Mapped)
                {
                    Assert.That(registry.GetMappedCoseAlgorithms(mapping.JoseName!),
                        Does.Contain(algorithm));
                }
            });
        }
    }

    [Test]
    public void AlgorithmMetadataExposesRegistryPolicyAndFullySpecifiedStatus()
    {
        ITokenRegistryOperations registry = new TokenRegistryOperations();

        AlgorithmMetadata recommended = registry.GetJoseAlgorithmMetadata(JoseAlgorithms.HmacSha256);
        AlgorithmMetadata fullySpecified = registry.GetJoseAlgorithmMetadata(JoseAlgorithms.Ed25519);
        AlgorithmMetadata deprecated = registry.GetJoseAlgorithmMetadata(JoseAlgorithms.EdDsa);
        AlgorithmMetadata prohibited = registry.GetJoseAlgorithmMetadata(JoseAlgorithms.RsaSha1);
        AlgorithmMetadata currentCose = registry.GetCoseAlgorithmMetadata(CoseAlgorithms.EcdsaP256Sha256);
        AlgorithmMetadata legacyCose = registry.GetCoseAlgorithmMetadata(CoseAlgorithms.EcdsaSha256);
        AlgorithmMetadata unknown = registry.GetCoseAlgorithmMetadata(999999);

        Assert.Multiple(() =>
        {
            Assert.That(recommended.Known, Is.True);
            Assert.That(recommended.Supported, Is.True);
            Assert.That(recommended.Recommended, Is.True);
            Assert.That(fullySpecified.Known, Is.True);
            Assert.That(fullySpecified.FullySpecified, Is.True);
            Assert.That(deprecated.Deprecated, Is.True);
            Assert.That(deprecated.CompatibilityOnly, Is.True);
            Assert.That(deprecated.FullySpecified, Is.False);
            Assert.That(prohibited.Prohibited, Is.True);
            Assert.That(currentCose.Name, Is.EqualTo("ESP256"));
            Assert.That(currentCose.FullySpecified, Is.True);
            Assert.That(legacyCose.Deprecated, Is.True);
            Assert.That(legacyCose.FullySpecified, Is.False);
            Assert.That(unknown.Known, Is.False);
            Assert.That(unknown.FullySpecified, Is.False);
        });
    }

    [Test]
    public void AlgorithmRegistryExposesCategoriesAndDefensiveSnapshots()
    {
        ITokenRegistryOperations registry = new TokenRegistryOperations();
        string[] joseAlgorithms = registry.GetJoseAlgorithms();
        int[] coseAlgorithms = registry.GetCoseAlgorithms();
        string originalJose = joseAlgorithms[0];
        int originalCose = coseAlgorithms[0];

        joseAlgorithms[0] = "changed";
        coseAlgorithms[0] = 999999;

        Assert.Multiple(() =>
        {
            Assert.That(registry.GetJoseAlgorithms(), Does.Contain(originalJose));
            Assert.That(registry.GetJoseAlgorithms(), Does.Not.Contain("changed"));
            Assert.That(registry.GetCoseAlgorithms(), Does.Contain(originalCose));
            Assert.That(registry.GetCoseAlgorithms(), Does.Not.Contain(999999));
            Assert.That(registry.IsJoseSignatureAlgorithm(JoseAlgorithms.HmacSha256), Is.True);
            Assert.That(registry.IsJoseKeyManagementAlgorithm(JoseAlgorithms.RsaOaep), Is.True);
            Assert.That(registry.IsJoseContentEncryptionAlgorithm(JoseAlgorithms.Aes256Gcm), Is.True);
            Assert.That(registry.IsJoseJwkOnlyAlgorithm(JoseAlgorithms.HmacSha1), Is.True);
            Assert.That(registry.IsJoseSignatureAlgorithm("hs256"), Is.False);
        });
    }

    [Test]
    public void FullySpecifiedAlgorithmSnapshotsIncludeCurrentAndExcludePolymorphicIdentifiers()
    {
        ITokenRegistryOperations registry = new TokenRegistryOperations();
        string[] jose = registry.GetFullySpecifiedJoseAlgorithms();
        int[] cose = registry.GetFullySpecifiedCoseAlgorithms();

        Assert.Multiple(() =>
        {
            Assert.That(jose, Does.Contain(JoseAlgorithms.EcdsaSha256));
            Assert.That(jose, Does.Contain(JoseAlgorithms.Ed25519));
            Assert.That(jose, Does.Contain(JoseAlgorithms.MlDsa87));
            Assert.That(jose, Does.Not.Contain(JoseAlgorithms.EdDsa));
            Assert.That(jose, Does.Not.Contain(JoseAlgorithms.EcdhEs));
            Assert.That(cose, Does.Contain(CoseAlgorithms.EcdsaP256Sha256));
            Assert.That(cose, Does.Contain(CoseAlgorithms.Ed448));
            Assert.That(cose, Does.Contain(CoseAlgorithms.MlDsa44));
            Assert.That(cose, Does.Not.Contain(CoseAlgorithms.EcdsaSha256));
            Assert.That(cose, Does.Not.Contain(CoseAlgorithms.EdDsa));
            Assert.That(cose, Does.Not.Contain(CoseAlgorithms.EcdhEsHkdfSha256));
        });

        jose[0] = "changed";
        cose[0] = 999999;
        Assert.Multiple(() =>
        {
            Assert.That(registry.GetFullySpecifiedJoseAlgorithms(), Does.Not.Contain("changed"));
            Assert.That(registry.GetFullySpecifiedCoseAlgorithms(), Does.Not.Contain(999999));
        });
    }

    [Test]
    public void AlgorithmMappingsPreferFullySpecifiedCoseLabelsAndRecognizeLegacyLabels()
    {
        ITokenRegistryOperations registry = new TokenRegistryOperations();

        AlgorithmMappingResult canonical = registry.MapJoseAlgorithmToCose(JoseAlgorithms.EcdsaSha256);
        AlgorithmMappingResult legacy = registry.MapCoseAlgorithmToJose(CoseAlgorithms.EcdsaSha256);
        int[] labels = registry.GetMappedCoseAlgorithms(JoseAlgorithms.EcdsaSha256);
        AlgorithmMappingResult unknownJose = registry.MapJoseAlgorithmToCose("unknown");
        AlgorithmMappingResult unknownCose = registry.MapCoseAlgorithmToJose(999999);

        Assert.Multiple(() =>
        {
            Assert.That(canonical.Mapped, Is.True);
            Assert.That(canonical.CoseLabel, Is.EqualTo(CoseAlgorithms.EcdsaP256Sha256));
            Assert.That(legacy.Mapped, Is.True);
            Assert.That(legacy.JoseName, Is.EqualTo(JoseAlgorithms.EcdsaSha256));
            Assert.That(labels, Is.EqualTo(new int[]
            {
                CoseAlgorithms.EcdsaP256Sha256,
                CoseAlgorithms.EcdsaSha256
            }));
            Assert.That(unknownJose.Mapped, Is.False);
            Assert.That(unknownCose.Mapped, Is.False);
        });
    }

    [Test]
    public void JoseJwtAndJwkRegistriesExposeCurrentIdentifiers()
    {
        ITokenRegistryOperations registry = new TokenRegistryOperations();

        Assert.Multiple(() =>
        {
            Assert.That(registry.GetJoseHeaderNames(), Does.Contain(JoseHeaderNames.Algorithm));
            Assert.That(registry.GetJoseHeaderNames(), Does.Contain(JoseHeaderNames.X509CertificateChain));
            Assert.That(registry.IsJoseHeaderForJws(JoseHeaderNames.Base64UrlEncodePayload), Is.True);
            Assert.That(registry.IsJoseHeaderForJwe(JoseHeaderNames.EncryptionAlgorithm), Is.True);
            Assert.That(registry.GetJoseCompressionAlgorithms(), Is.EqualTo(new[] { JoseCompressionAlgorithms.Deflate }));
            Assert.That(registry.GetJoseMediaTypes(), Does.Contain(JoseMediaTypes.Jwt));
            Assert.That(registry.NormalizeJoseMediaType("JWT"), Is.EqualTo(JoseMediaTypes.Jwt));
            Assert.That(registry.NormalizeJoseMediaType("APPLICATION/JWT"), Is.EqualTo(JoseMediaTypes.Jwt));
            Assert.That(registry.GetJwtClaimNames(), Does.Contain(JwtClaimNames.Issuer));
            Assert.That(registry.GetJwtClaimNames(), Does.Contain(JwtClaimNames.SelectiveDisclosureAlgorithm));
            Assert.That(registry.GetJwkParameterNames(), Does.Contain(JwkNames.RsaModulus));
            Assert.That(registry.GetJwkKeyTypes(), Is.EquivalentTo(new[] { "oct", "RSA", "EC", "OKP", "AKP" }));
            Assert.That(registry.GetJwkKeyUses(), Is.EquivalentTo(new[] { "sig", "enc" }));
            Assert.That(registry.GetJwkKeyOperations(), Does.Contain(TokenKeyOperations.DeriveBits));
            Assert.That(registry.GetJwkCurveNames(), Does.Contain("P-256"));
            Assert.That(registry.GetJwkCurveNames(), Does.Contain("Ed25519"));
        });
    }

    [Test]
    public void CoseRegistryExposesAlgorithmHeaderAndKeyMetadata()
    {
        ITokenRegistryOperations registry = new TokenRegistryOperations();

        Assert.Multiple(() =>
        {
            Assert.That(registry.GetCoseAlgorithmName(CoseAlgorithms.EcdsaP384Sha384), Is.EqualTo("ESP384"));
            Assert.That(registry.GetCoseAlgorithmName(999999), Is.Null);
            Assert.That(registry.GetCoseHeaderLabels(), Does.Contain(CoseHeaderLabels.Algorithm));
            Assert.That(registry.GetCoseHeaderName(CoseHeaderLabels.Algorithm), Is.EqualTo("alg"));
            Assert.That(registry.GetCoseHeaderName(999999), Is.Null);
            Assert.That(registry.GetCoseAlgorithmHeaderLabels(), Does.Contain(CoseHeaderLabels.EphemeralKey));
            Assert.That(registry.GetCoseAlgorithmHeaderName(CoseHeaderLabels.EphemeralKey), Is.EqualTo("ephemeral key"));
            Assert.That(registry.GetCoseKeyTypes(), Does.Contain(CoseKeyLabels.KeyTypeRsa));
            Assert.That(registry.GetCoseKeyTypeName(CoseKeyLabels.KeyTypeEc2), Is.EqualTo("EC2"));
            Assert.That(registry.GetCoseCommonKeyParameters(), Does.Contain(CoseKeyLabels.KeyId));
            Assert.That(registry.GetCoseKeyParameters(CoseKeyLabels.KeyTypeRsa), Does.Contain(CoseKeyLabels.RsaModulus));
            Assert.That(
                registry.GetCoseKeyParameterName(CoseKeyLabels.KeyTypeRsa, CoseKeyLabels.RsaModulus),
                Is.EqualTo("n"));
            Assert.That(registry.GetCoseKeyParameters(999999), Is.Empty);
            Assert.That(registry.GetCoseKeyOperations(), Does.Contain(CoseKeyLabels.OperationWrapKey));
            Assert.That(registry.GetCoseKeyOperationName(CoseKeyLabels.OperationVerify), Is.EqualTo("verify"));
            Assert.That(registry.GetCoseCurves(), Does.Contain(CoseKeyLabels.CurveP521));
            Assert.That(registry.GetCoseCurveName(CoseKeyLabels.CurveP521), Is.EqualTo("P-521"));
        });
    }

    [Test]
    public void EveryIntegerRegistryEntryRoundTripsToAName()
    {
        ITokenRegistryOperations registry = new TokenRegistryOperations();

        foreach (int label in registry.GetCoseHeaderLabels())
        {
            Assert.That(registry.GetCoseHeaderName(label), Is.Not.Null, label.ToString());
        }

        foreach (int label in registry.GetCoseAlgorithmHeaderLabels())
        {
            Assert.That(registry.GetCoseAlgorithmHeaderName(label), Is.Not.Null, label.ToString());
        }

        foreach (int keyType in registry.GetCoseKeyTypes())
        {
            Assert.That(registry.GetCoseKeyTypeName(keyType), Is.Not.Null, keyType.ToString());
            foreach (int parameter in registry.GetCoseCommonKeyParameters())
            {
                Assert.That(registry.GetCoseKeyParameterName(keyType, parameter),
                    Is.Not.Null,
                    keyType + ":" + parameter);
            }

            foreach (int parameter in registry.GetCoseKeyParameters(keyType))
            {
                Assert.That(registry.GetCoseKeyParameterName(keyType, parameter),
                    Is.Not.Null,
                    keyType + ":" + parameter);
            }
        }

        foreach (int operation in registry.GetCoseKeyOperations())
        {
            Assert.That(registry.GetCoseKeyOperationName(operation),
                Is.Not.Null,
                operation.ToString());
        }

        foreach (int curve in registry.GetCoseCurves())
        {
            Assert.That(registry.GetCoseCurveName(curve), Is.Not.Null, curve.ToString());
        }

        foreach (int claim in registry.GetCwtClaimLabels())
        {
            Assert.That(registry.GetCwtClaimName(claim), Is.Not.Null, claim.ToString());
        }

        foreach (int method in registry.GetCwtConfirmationMethodLabels())
        {
            Assert.That(registry.GetCwtConfirmationMethodName(method), Is.Not.Null, method.ToString());
        }
    }

    [Test]
    public void CwtRegistryAndClaimMappingsRoundTripRegisteredClaims()
    {
        ITokenRegistryOperations registry = new TokenRegistryOperations();

        ClaimMappingResult fromJwt = registry.MapJwtClaimToCwt(JwtClaimNames.JwtId);
        ClaimMappingResult fromCwt = registry.MapCwtClaimToJwt(CwtClaimLabels.CwtId);
        ClaimMappingResult unknownJwt = registry.MapJwtClaimToCwt("unknown");
        ClaimMappingResult unknownCwt = registry.MapCwtClaimToJwt(999999);

        Assert.Multiple(() =>
        {
            Assert.That(registry.GetCwtClaimLabels(), Does.Contain(CwtClaimLabels.Issuer));
            Assert.That(registry.GetCwtClaimName(CwtClaimLabels.Issuer), Is.EqualTo("iss"));
            Assert.That(registry.GetCwtConfirmationMethodLabels(), Does.Contain(CwtConfirmationMethodLabels.JwkThumbprint));
            Assert.That(
                registry.GetCwtConfirmationMethodName(CwtConfirmationMethodLabels.JwkThumbprint),
                Is.EqualTo("jkt"));
            Assert.That(fromJwt.Mapped, Is.True);
            Assert.That(fromJwt.CwtLabel, Is.EqualTo(CwtClaimLabels.CwtId));
            Assert.That(fromCwt.Mapped, Is.True);
            Assert.That(fromCwt.JwtName, Is.EqualTo(JwtClaimNames.JwtId));
            Assert.That(unknownJwt.Mapped, Is.False);
            Assert.That(unknownCwt.Mapped, Is.False);
            Assert.That(registry.GetCwtClaimName(999999), Is.Null);
        });
    }

    [Test]
    public void RegistrySnapshotsAndMetadataResultsAreIndependent()
    {
        ITokenRegistryOperations registry = new TokenRegistryOperations();
        string[] headers = registry.GetJoseHeaderNames();
        string[] claims = registry.GetJwtClaimNames();
        string[] parameters = registry.GetJwkParameterNames();
        int[] coseHeaders = registry.GetCoseHeaderLabels();
        int[] cwtClaims = registry.GetCwtClaimLabels();
        AlgorithmMetadata metadata = registry.GetJoseAlgorithmMetadata(JoseAlgorithms.HmacSha256);

        headers[0] = "changed";
        claims[0] = "changed";
        parameters[0] = "changed";
        coseHeaders[0] = 999999;
        cwtClaims[0] = 999999;
        metadata.Known = false;

        Assert.Multiple(() =>
        {
            Assert.That(registry.GetJoseHeaderNames(), Does.Not.Contain("changed"));
            Assert.That(registry.GetJwtClaimNames(), Does.Not.Contain("changed"));
            Assert.That(registry.GetJwkParameterNames(), Does.Not.Contain("changed"));
            Assert.That(registry.GetCoseHeaderLabels(), Does.Not.Contain(999999));
            Assert.That(registry.GetCwtClaimLabels(), Does.Not.Contain(999999));
            Assert.That(registry.GetJoseAlgorithmMetadata(JoseAlgorithms.HmacSha256).Known, Is.True);
        });
    }

    [Test]
    public void RegistryMappingsRejectNullNamesAndLookupsHandleUnknownValues()
    {
        ITokenRegistryOperations registry = new TokenRegistryOperations();

        Assert.Multiple(() =>
        {
            Assert.That(() => registry.MapJoseAlgorithmToCose(null!), Throws.ArgumentNullException);
            Assert.That(() => registry.GetMappedCoseAlgorithms(null!), Throws.ArgumentNullException);
            Assert.That(() => registry.MapJwtClaimToCwt(null!), Throws.ArgumentNullException);
            Assert.That(registry.GetJoseAlgorithmMetadata(null!).Known, Is.False);
            Assert.That(registry.IsJoseSignatureAlgorithm(null!), Is.False);
            Assert.That(registry.IsJoseHeaderForJws(null!), Is.False);
            Assert.That(registry.NormalizeJoseMediaType(null), Is.Null);
            Assert.That(registry.GetCoseKeyTypeName(999999), Is.Null);
            Assert.That(registry.GetCoseCurveName(999999), Is.Null);
            Assert.That(registry.GetCwtConfirmationMethodName(999999), Is.Null);
        });
    }
}
