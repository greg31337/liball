using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using token;

namespace token.Tests;

public class TokenKeyServiceTests
{
    [Test]
    public void EveryClassicalKeyTypeRoundTripsAcrossPublicJwkAndCoseProjections()
    {
        ITokenKeyService service = new TokenKeyService();
        using RSA rsa = RSA.Create(2048);
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey[] keys = new TokenKey[]
        {
            service.ImportJoseSymmetricKey(new byte[32], JoseAlgorithms.HmacSha256).Key!,
            service.ImportJoseRsaKey(rsa, JoseAlgorithms.RsaSha256, true).Key!,
            service.ImportJoseEc2Key(ecdsa, JoseAlgorithms.EcdsaSha256, true).Key!,
            service.ImportOkpKey(
                Enumerable.Repeat((byte)3, 32).ToArray(),
                Enumerable.Repeat((byte)4, 32).ToArray(),
                "Ed25519",
                JoseAlgorithms.Ed25519).Key!
        };

        foreach (TokenKey key in keys)
        {
            key.KeyId = "round-trip-" + key.KeyType;
            key.Use = TokenKeyUses.Signature;
            if (key.KeyType != TokenKeyTypes.Symmetric)
            {
                key.KeyOperations = new string[]
                {
                    TokenKeyOperations.Verify
                };
            }

            TokenTextResult completeJwk = service.WriteJwk(key);
            TokenTextResult publicJwk = service.WritePublicJwk(key);
            TokenTextResult privateJwk = service.WritePrivateJwk(key);
            TokenTextResult minimalJwk = service.WriteMinimalJwk(key);
            TokenBytesResult completeCose = service.WriteCoseKey(key);
            TokenBytesResult publicCose = service.WritePublicCoseKey(key);
            TokenBytesResult privateCose = service.WritePrivateCoseKey(key);
            TokenBytesResult minimalCose = service.WriteMinimalCoseKey(key);
            JwkResult parsedJwk = service.ParseJwk(completeJwk.Text!);
            CoseKeyResult parsedCose = service.ParseCoseKey(completeCose.Bytes!);
            TokenBytesResult convertedCose = service.ConvertJwkToCoseKey(completeJwk.Text!);
            TokenTextResult convertedJwk = service.ConvertCoseKeyToJwk(convertedCose.Bytes!);
            JwkResult roundTripJwk = service.ParseJwk(convertedJwk.Text!);
            TokenBytesResult originalThumbprint = service.CalculateJwkThumbprint(parsedJwk.Key!);
            TokenBytesResult roundTripThumbprint = service.CalculateJwkThumbprint(roundTripJwk.Key!);

            Assert.Multiple(() =>
            {
                Assert.That(completeJwk.Success, Is.True, key.KeyType);
                Assert.That(privateJwk.Success, Is.True, key.KeyType);
                Assert.That(minimalJwk.Success, Is.True, key.KeyType);
                Assert.That(completeCose.Success, Is.True, key.KeyType);
                Assert.That(privateCose.Success, Is.True, key.KeyType);
                Assert.That(minimalCose.Success, Is.True, key.KeyType);
                Assert.That(parsedJwk.Key!.KeyType, Is.EqualTo(key.KeyType));
                Assert.That(parsedCose.Key!.KeyType, Is.EqualTo(key.KeyType));
                Assert.That(convertedJwk.Success, Is.True, key.KeyType);
                Assert.That(
                    roundTripJwk.Key!.NativePrivateMaterial
                        || roundTripJwk.Key.SymmetricKey != null,
                    Is.True);
                Assert.That(roundTripThumbprint.Bytes, Is.EqualTo(originalThumbprint.Bytes));
                if (key.KeyType == TokenKeyTypes.Symmetric)
                {
                    Assert.That(publicJwk.Error!.Code, Is.EqualTo("public_projection_unavailable"));
                    Assert.That(publicCose.Error!.Code, Is.EqualTo("public_projection_unavailable"));
                }
                else
                {
                    Assert.That(publicJwk.Success, Is.True, key.KeyType);
                    Assert.That(publicCose.Success, Is.True, key.KeyType);
                }
            });

            service.ClearKeyMaterial(parsedJwk.Key!);
            service.ClearKeyMaterial(parsedCose.Key!);
            service.ClearKeyMaterial(roundTripJwk.Key!);
            service.ClearKeyMaterial(key);
        }
    }

    [Test]
    public void ImportsSymmetricRsaEc2AndOkpKeysThroughPublicContract()
    {
        ITokenKeyService service = new TokenKeyService();
        byte[] secret = Enumerable.Repeat((byte)7, 32).ToArray();
        using RSA rsa = RSA.Create(2048);
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        TokenKeyResult symmetric = service.ImportJoseSymmetricKey(
            secret,
            JoseAlgorithms.HmacSha256);
        TokenKeyResult rsaResult = service.ImportJoseRsaKey(
            rsa,
            JoseAlgorithms.RsaSha256,
            true);
        TokenKeyResult ecResult = service.ImportCoseEc2Key(
            ecdsa,
            CoseAlgorithms.EcdsaP256Sha256,
            true);
        TokenKeyResult okp = service.ImportOkpKey(
            Enumerable.Repeat((byte)3, 32).ToArray(),
            Enumerable.Repeat((byte)4, 32).ToArray(),
            "Ed25519",
            JoseAlgorithms.Ed25519);

        Assert.Multiple(() =>
        {
            Assert.That(symmetric.Success, Is.True);
            Assert.That(symmetric.Key!.SymmetricKey, Is.Not.SameAs(secret));
            Assert.That(rsaResult.Success, Is.True);
            Assert.That(rsaResult.Key!.NativePrivateMaterial, Is.True);
            Assert.That(ecResult.Success, Is.True);
            Assert.That(ecResult.Key!.CoseCurve, Is.EqualTo(CoseKeyLabels.CurveP256));
            Assert.That(okp.Success, Is.True);
            Assert.That(okp.Key!.CoseCurve, Is.EqualTo(CoseKeyLabels.CurveEd25519));
            Assert.That(okp.Key.OkpPrivateKey, Has.Length.EqualTo(32));
        });

        service.ClearKeyMaterial(symmetric.Key!);
        service.ClearKeyMaterial(okp.Key!);
    }

    [Test]
    public void JwkAndJwksProjectionsRoundTripThroughPublicContract()
    {
        ITokenKeyService service = new TokenKeyService();
        using RSA rsa = RSA.Create(2048);
        TokenKey key = service.ImportRsaKey(rsa, true).Key!;
        key.KeyId = "key-1";
        key.Algorithm = JoseAlgorithms.RsaSha256;

        TokenTextResult complete = service.WriteJwk(key);
        TokenTextResult publicJwk = service.WritePublicJwk(key);
        TokenTextResult privateJwk = service.WritePrivateJwk(key);
        TokenTextResult minimal = service.WriteMinimalJwk(key);
        JsonWebKeySet set = new JsonWebKeySet(new TokenKey[] { key });
        TokenTextResult publicSet = service.WritePublicJwks(set);
        TokenTextResult privateSet = service.WritePrivateJwks(set);
        TokenTextResult minimalSet = service.WriteMinimalJwks(set);
        JwkResult parsedPublic = service.ParseJwk(publicJwk.Text!);
        JwksResult parsedSet = service.ParseJwks(publicSet.Text!);

        Assert.Multiple(() =>
        {
            Assert.That(complete.Success, Is.True);
            Assert.That(complete.Text, Does.Contain("\"d\":"));
            Assert.That(publicJwk.Text, Does.Not.Contain("\"d\":"));
            Assert.That(privateJwk.Text, Does.Contain("\"d\":"));
            Assert.That(minimal.Text, Does.Not.Contain("\"kid\":"));
            Assert.That(publicSet.Success, Is.True);
            Assert.That(privateSet.Text, Does.Contain("\"d\":"));
            Assert.That(minimalSet.Text, Does.Not.Contain("\"kid\":"));
            Assert.That(parsedPublic.Key!.NativePrivateMaterial, Is.False);
            Assert.That(parsedSet.Set!.Keys, Has.Length.EqualTo(1));
        });

        service.ClearKeyMaterial(parsedPublic.Key!);
        service.ClearKeyMaterial(parsedSet.Set!.Keys[0]);
    }

    [Test]
    public void CoseProjectionsAndThumbprintsUseRequiredDeterministicMembers()
    {
        ITokenKeyService service = new TokenKeyService();
        using RSA rsa = RSA.Create(2048);
        TokenKey key = service.ImportCoseRsaKey(
            rsa,
            CoseAlgorithms.RsaSha256,
            true).Key!;
        key.KeyIdBytes = new byte[] { 1, 2, 3 };

        TokenBytesResult complete = service.WriteCoseKey(key);
        TokenBytesResult publicKey = service.WritePublicCoseKey(key);
        TokenBytesResult privateKey = service.WritePrivateCoseKey(key);
        TokenBytesResult minimal = service.WriteMinimalCoseKey(key);
        CoseKeyResult parsedPublic = service.ParseCoseKey(publicKey.Bytes!);
        CoseKeyResult parsedPrivate = service.ParseCoseKey(privateKey.Bytes!);
        CoseKeyResult parsedMinimal = service.ParseCoseKey(minimal.Bytes!);
        TokenBytesResult privateThumbprint = service.CalculateCoseKeyThumbprint(key);
        TokenBytesResult publicThumbprint = service.CalculateCoseKeyThumbprint(parsedPublic.Key!);
        TokenTextResult uri = service.CalculateCoseKeyThumbprintUri(key);

        Assert.Multiple(() =>
        {
            Assert.That(complete.Success, Is.True);
            Assert.That(parsedPublic.Key!.NativePrivateMaterial, Is.False);
            Assert.That(parsedPrivate.Key!.NativePrivateMaterial, Is.True);
            Assert.That(parsedMinimal.Key!.KeyIdBytes, Is.Null);
            Assert.That(parsedMinimal.Key.HasCoseAlgorithm, Is.False);
            Assert.That(privateThumbprint.Bytes, Is.EqualTo(publicThumbprint.Bytes));
            Assert.That(uri.Text, Does.StartWith("urn:ietf:params:oauth:ckt:sha-256:"));
        });

        service.ClearKeyMaterial(parsedPublic.Key!);
        service.ClearKeyMaterial(parsedPrivate.Key!);
        service.ClearKeyMaterial(parsedMinimal.Key!);
    }

    [Test]
    public void SymmetricCoseThumbprintHashesTheMinimalProjection()
    {
        ITokenKeyService service = new TokenKeyService();
        TokenKey key = service.ImportSymmetricKey(new byte[32]).Key!;
        key.KeyIdBytes = new byte[] { 9 };
        TokenBytesResult minimal = service.WriteMinimalCoseKey(key);
        TokenBytesResult publicProjection = service.WritePublicCoseKey(key);
        key.Algorithm = "ignored-by-thumbprints";
        TokenBytesResult thumbprint = service.CalculateCoseKeyThumbprint(key);

        Assert.Multiple(() =>
        {
            Assert.That(minimal.Success, Is.True);
            Assert.That(thumbprint.Success, Is.True);
            Assert.That(thumbprint.Bytes, Is.EqualTo(SHA256.HashData(minimal.Bytes!)));
            Assert.That(publicProjection.Error!.Code,
                Is.EqualTo("public_projection_unavailable"));
        });

        service.ClearKeyMaterial(key);
    }

    [Test]
    public void ConversionSelectionAndOperationEnforcementArePublic()
    {
        ITokenKeyService service = new TokenKeyService();
        string firstSecret = B64(Enumerable.Repeat((byte)1, 32).ToArray());
        string secondSecret = B64(Enumerable.Repeat((byte)2, 32).ToArray());
        string jwks = "{\"keys\":["
            + "{\"kty\":\"oct\",\"k\":\"" + firstSecret
            + "\",\"kid\":\"first\",\"key_ops\":[\"verify\"]},"
            + "{\"kty\":\"oct\",\"k\":\"" + secondSecret
            + "\",\"kid\":\"second\",\"key_ops\":[\"sign\"]}]}";
        JwksResult parsed = service.ParseJwks(jwks);
        TokenKeyQuery query = new TokenKeyQuery();
        query.KeyId = "first";
        query.KeyOperation = TokenKeyOperations.Verify;
        TokenKeyResult selected = service.SelectKey(parsed.Set!, query);
        TokenBytesResult cose = service.ConvertKeyToCoseKey(selected.Key!);
        TokenTextResult roundTrip = service.ConvertCoseKeyToJwk(cose.Bytes!);
        TokenResult permitted = service.EnforceKeyOperation(
            selected.Key!,
            TokenKeyOperations.Verify);
        TokenResult denied = service.EnforceCoseKeyOperation(
            selected.Key!,
            CoseKeyLabels.OperationSign);
        TokenKey macKey = service.ImportSymmetricKey(new byte[32]).Key!;
        macKey.CoseKeyOperations = new int[] { CoseKeyLabels.OperationMacVerify };
        TokenResult macPermitted = service.EnforceCoseKeyOperation(
            macKey,
            CoseKeyLabels.OperationMacVerify);

        Assert.Multiple(() =>
        {
            Assert.That(selected.Success, Is.True);
            Assert.That(selected.Key!.KeyId, Is.EqualTo("first"));
            Assert.That(cose.Success, Is.True);
            Assert.That(roundTrip.Success, Is.True);
            Assert.That(roundTrip.Text, Does.Contain("\"kid\":\"first\""));
            Assert.That(permitted.Success, Is.True);
            Assert.That(denied.Error!.Code, Is.EqualTo("key_operation_not_permitted"));
            Assert.That(macPermitted.Success, Is.True);
        });

        foreach (TokenKey key in parsed.Set!.Keys)
        {
            service.ClearKeyMaterial(key);
        }
        service.ClearKeyMaterial(macKey);
    }

    [Test]
    public void SelectionCombinesEveryJoseAndCoseCriterion()
    {
        ITokenKeyService service = new TokenKeyService();
        using ECDsa firstNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa secondNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey selected = service.ImportJoseEc2Key(
            firstNative,
            JoseAlgorithms.EcdsaSha256,
            true).Key!;
        TokenKey other = service.ImportJoseEc2Key(
            secondNative,
            JoseAlgorithms.EcdsaSha256,
            true).Key!;
        selected.KeyId = "selected";
        selected.KeyIdBytes = new byte[] { 1, 2, 3 };
        selected.CoseAlgorithm = CoseAlgorithms.EcdsaP256Sha256;
        selected.HasCoseAlgorithm = true;
        selected.Use = TokenKeyUses.Signature;
        selected.KeyOperations = new string[] { TokenKeyOperations.Verify };
        selected.CoseKeyOperations = new int[] { CoseKeyLabels.OperationVerify };
        selected.CertificateSha1Thumbprint = Enumerable.Repeat((byte)5, 20).ToArray();
        selected.CertificateSha256Thumbprint = Enumerable.Repeat((byte)6, 32).ToArray();
        other.KeyId = "other";
        JsonWebKeySet set = new JsonWebKeySet(new TokenKey[] { selected, other });
        TokenKeyQuery query = new TokenKeyQuery();
        query.KeyId = selected.KeyId;
        query.KeyIdBytes = selected.KeyIdBytes.ToArray();
        query.KeyType = TokenKeyTypes.EllipticCurve;
        query.CoseKeyType = CoseKeyLabels.KeyTypeEc2;
        query.Algorithm = JoseAlgorithms.EcdsaSha256;
        query.CoseAlgorithm = CoseAlgorithms.EcdsaP256Sha256;
        query.HasCoseAlgorithm = true;
        query.Use = TokenKeyUses.Signature;
        query.KeyOperation = TokenKeyOperations.Verify;
        query.CoseKeyOperation = CoseKeyLabels.OperationVerify;
        query.Curve = "P-256";
        query.CoseCurve = CoseKeyLabels.CurveP256;
        query.CertificateSha1Thumbprint = selected.CertificateSha1Thumbprint.ToArray();
        query.CertificateSha256Thumbprint = selected.CertificateSha256Thumbprint.ToArray();
        query.RequirePrivateMaterial = true;
        query.RequirePublicMaterial = true;

        TokenKeyResult exact = service.SelectKey(set, query);
        query.CertificateSha256Thumbprint = new byte[32];
        TokenKeyResult mismatch = service.SelectKey(set, query);
        TokenKeyQuery broad = new TokenKeyQuery();
        broad.KeyType = TokenKeyTypes.EllipticCurve;
        TokenKeyResult ambiguous = service.SelectKey(set, broad);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Key, Is.SameAs(selected));
            Assert.That(mismatch.Error!.Code, Is.EqualTo("jwks_key_not_found"));
            Assert.That(ambiguous.Error!.Code, Is.EqualTo("ambiguous_jwks_key"));
        });

        service.ClearKeyMaterial(selected);
        service.ClearKeyMaterial(other);
    }

    [Test]
    public void OperationPolicyEnforcesMetadataMaterialAndRegisteredNames()
    {
        ITokenKeyService service = new TokenKeyService();
        TokenKey restricted = service.ImportSymmetricKey(new byte[32]).Key!;
        restricted.Use = TokenKeyUses.Signature;
        restricted.KeyOperations = new string[] { TokenKeyOperations.Verify };
        restricted.CoseKeyOperations = new int[] { CoseKeyLabels.OperationVerify };
        using RSA rsa = RSA.Create(2048);
        TokenKey publicRsa = service.ImportRsaKey(rsa, false).Key!;

        TokenResult verify = service.EnforceKeyOperation(restricted, TokenKeyOperations.Verify);
        TokenResult deniedJose = service.EnforceKeyOperation(restricted, TokenKeyOperations.Sign);
        TokenResult deniedUse = service.EnforceKeyOperation(restricted, TokenKeyOperations.Encrypt);
        TokenResult deniedCose = service.EnforceCoseKeyOperation(restricted, CoseKeyLabels.OperationSign);
        TokenResult unknownJose = service.EnforceKeyOperation(restricted, "unknown");
        TokenResult unknownCose = service.EnforceCoseKeyOperation(restricted, 999999);
        TokenResult publicVerify = service.EnforceKeyOperation(publicRsa, TokenKeyOperations.Verify);
        TokenResult publicSign = service.EnforceKeyOperation(publicRsa, TokenKeyOperations.Sign);

        Assert.Multiple(() =>
        {
            Assert.That(verify.Success, Is.True);
            Assert.That(deniedJose.Error!.Code, Is.EqualTo("key_operation_not_permitted"));
            Assert.That(deniedUse.Error!.Code, Is.EqualTo("key_operation_not_permitted"));
            Assert.That(deniedCose.Error!.Code, Is.EqualTo("key_operation_not_permitted"));
            Assert.That(unknownJose.Error!.Code, Is.EqualTo("unknown_key_operation"));
            Assert.That(unknownCose.Error!.Code, Is.EqualTo("unknown_key_operation"));
            Assert.That(publicVerify.Success, Is.True);
            Assert.That(publicSign.Error!.Code, Is.EqualTo("key_operation_unavailable"));
        });

        service.ClearKeyMaterial(restricted);
        service.ClearKeyMaterial(publicRsa);
    }

    [Test]
    public void PublicImportsAndParsersRejectWeakMalformedAndConfusedKeys()
    {
        ITokenKeyService service = new TokenKeyService();
        using RSA weakRsa = RSA.Create(1024);
        using ECDsa p384 = ECDsa.Create(ECCurve.NamedCurves.nistP384);

        TokenKeyResult weakGeneric = service.ImportSymmetricKey(new byte[15]);
        TokenKeyResult weakHmac = service.ImportJoseSymmetricKey(
            new byte[31],
            JoseAlgorithms.HmacSha256);
        TokenKeyResult wrongAesSize = service.ImportCoseSymmetricKey(
            new byte[15],
            CoseAlgorithms.Aes128Gcm);
        TokenKeyResult weakRsaResult = service.ImportRsaKey(weakRsa, false);
        TokenKeyResult curveConfusion = service.ImportJoseEc2Key(
            p384,
            JoseAlgorithms.EcdsaSha256,
            false);
        TokenKeyResult shortOkp = service.ImportOkpKey(
            new byte[31],
            null,
            "Ed25519",
            JoseAlgorithms.Ed25519);
        JwkResult missingJwk = service.ParseJwk("{}");
        JwkResult duplicateJwk = service.ParseJwk(
            "{\"kty\":\"oct\",\"kty\":\"oct\",\"k\":\"AAAAAAAAAAAAAAAAAAAAAA\"}");
        JwksResult malformedJwks = service.ParseJwks("{\"keys\":[1]}");
        CoseKeyResult malformedCose = service.ParseCoseKey(new byte[] { 0xFF });
        TokenBytesResult malformedConversion = service.ConvertJwkToCoseKey("{");
        TokenTextResult malformedReverse = service.ConvertCoseKeyToJwk(new byte[] { 0xFF });

        Assert.Multiple(() =>
        {
            Assert.That(weakGeneric.Error!.Code, Is.EqualTo("weak_key"));
            Assert.That(weakHmac.Error!.Code, Is.EqualTo("weak_key"));
            Assert.That(wrongAesSize.Error!.Code, Is.EqualTo("invalid_key_size"));
            Assert.That(weakRsaResult.Error!.Code, Is.EqualTo("weak_key"));
            Assert.That(curveConfusion.Error!.Code, Is.EqualTo("algorithm_curve_mismatch"));
            Assert.That(shortOkp.Error!.Code, Is.EqualTo("invalid_okp_key"));
            Assert.That(missingJwk.Error!.Code, Is.EqualTo("missing_jwk_member"));
            Assert.That(duplicateJwk.Error!.Code, Is.EqualTo("duplicate_json_member"));
            Assert.That(malformedJwks.Error!.Code, Is.EqualTo("invalid_jwks_key"));
            Assert.That(malformedCose.Error!.Code, Is.EqualTo("invalid_cbor"));
            Assert.That(malformedConversion.Error!.Code, Is.EqualTo("invalid_json"));
            Assert.That(malformedReverse.Error!.Code, Is.EqualTo("invalid_cbor"));
        });
    }

    [Test]
    public void RemainingImportConversionThumbprintAndJwksMethodsArePublic()
    {
        ITokenKeyService service = new TokenKeyService();
        using ECDsa ecdsa = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);
        TokenKeyResult ec = service.ImportJoseEc2Key(
            ecdsa,
            JoseAlgorithms.EcdsaSha256,
            true);
        TokenTextResult converted = service.ConvertKeyToJwk(ec.Key!);
        TokenTextResult uri = service.CalculateJwkThumbprintUri(
            ec.Key!);
        TokenTextResult jwks = service.WriteJwks(
            new JsonWebKeySet(new TokenKey[] { ec.Key! }));

        using RSA rsa = RSA.Create(2048);
        CertificateRequest request = new CertificateRequest(
            "CN=public-surface",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(5));
        TokenKeyResult certificateKey =
            service.ImportCertificateChainKey(
                certificate,
                new X509Certificate2[] { certificate });
        TokenKeyResult mlDsa = service.ImportMlDsaPrivateKey(
            new byte[32],
            JoseAlgorithms.MlDsa44);

        Assert.Multiple(() =>
        {
            Assert.That(ec.Success, Is.True);
            Assert.That(converted.Success, Is.True);
            Assert.That(converted.Text,
                Does.Contain("\"kty\":\"EC\""));
            Assert.That(uri.Success, Is.True);
            Assert.That(uri.Text,
                Does.StartWith("urn:ietf:params:oauth:jwk-thumbprint:sha-256:"));
            Assert.That(jwks.Success, Is.True);
            Assert.That(jwks.Text, Does.StartWith("{\"keys\":["));
            Assert.That(certificateKey.Success, Is.True);
            Assert.That(certificateKey.Key!.CertificateChain,
                Has.Length.EqualTo(1));
            Assert.That(
                mlDsa.Success,
                Is.EqualTo(MLDsa.IsSupported));
            if (!mlDsa.Success)
            {
                Assert.That(mlDsa.Error!.Code,
                    Is.EqualTo("unsupported_algorithm"));
            }
        });

        service.ClearKeyMaterial(ec.Key!);
        service.ClearKeyMaterial(certificateKey.Key!);
        if (mlDsa.Key != null)
        {
            service.ClearKeyMaterial(mlDsa.Key);
        }
    }

    [Test]
    public void CertificateAndUnsupportedPostQuantumImportsAreStable()
    {
        ITokenKeyService service = new TokenKeyService();
        using RSA rsa = RSA.Create(2048);
        CertificateRequest request = new CertificateRequest(
            "CN=key-service",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        X509SignatureGenerator generator = X509SignatureGenerator.CreateForRSA(
            rsa,
            RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.Create(
            request.SubjectName,
            generator,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(5),
            new byte[] { 1 });

        TokenKeyResult certificateKey = service.ImportCertificateKey(certificate);
        TokenKeyResult unsupported = service.ImportMlDsaPublicKey(
            new byte[32],
            "ML-DSA-unknown");

        Assert.Multiple(() =>
        {
            Assert.That(certificateKey.Success, Is.True);
            Assert.That(certificateKey.Key!.Certificate, Is.SameAs(certificate));
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupported.Error!.Code, Is.EqualTo("unsupported_algorithm"));
        });

        if (!MLDsa.IsSupported)
        {
            TokenKeyResult unavailable = service.ImportMlDsaPublicKey(
                new byte[MLDsaAlgorithm.MLDsa44.PublicKeySizeInBytes],
                JoseAlgorithms.MlDsa44);
            Assert.That(unavailable.Error!.Code, Is.EqualTo("unsupported_algorithm"));
        }
    }

    [Test]
    public void MlDsaAkpRepresentationsConvertAndThumbprintWithoutPlatformCrypto()
    {
        ITokenKeyService service = new TokenKeyService();
        byte[] publicBytes = new byte[MLDsaAlgorithm.MLDsa44.PublicKeySizeInBytes];
        byte[] privateSeed = Enumerable.Repeat((byte)6, 32).ToArray();
        string jwk = "{\"kty\":\"AKP\",\"alg\":\"ML-DSA-44\","
            + "\"pub\":\"" + B64(publicBytes) + "\","
            + "\"priv\":\"" + B64(privateSeed) + "\",\"kid\":\"pq\"}";

        JwkResult parsed = service.ParseJwk(jwk);
        TokenTextResult publicJwk = service.WritePublicJwk(parsed.Key!);
        TokenTextResult privateJwk = service.WritePrivateJwk(parsed.Key!);
        TokenTextResult minimalJwk = service.WriteMinimalJwk(parsed.Key!);
        TokenBytesResult cose = service.ConvertJwkToCoseKey(jwk);
        CoseKeyResult parsedCose = service.ParseCoseKey(cose.Bytes!);
        TokenTextResult convertedJwk = service.ConvertCoseKeyToJwk(cose.Bytes!);
        TokenBytesResult privateJwkThumb = service.CalculateJwkThumbprint(parsed.Key!);
        TokenBytesResult publicJwkThumb = service.CalculateJwkThumbprint(
            service.ParseJwk(publicJwk.Text!).Key!);
        TokenBytesResult privateCoseThumb = service.CalculateCoseKeyThumbprint(parsed.Key!);
        TokenBytesResult publicCoseThumb = service.CalculateCoseKeyThumbprint(
            service.ParseJwk(publicJwk.Text!).Key!);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Key!.KeyType, Is.EqualTo(TokenKeyTypes.AlgorithmKeyPair));
            Assert.That(parsed.Key.AlgorithmPrivateKey, Is.EqualTo(privateSeed));
            Assert.That(publicJwk.Text, Does.Not.Contain("\"priv\":"));
            Assert.That(privateJwk.Text, Does.Contain("\"priv\":"));
            Assert.That(minimalJwk.Text, Does.Contain("\"alg\":\"ML-DSA-44\""));
            Assert.That(minimalJwk.Text, Does.Not.Contain("\"kid\":"));
            Assert.That(parsedCose.Success, Is.True);
            Assert.That(parsedCose.Key!.CoseKeyType,
                Is.EqualTo(CoseKeyLabels.KeyTypeAlgorithmKeyPair));
            Assert.That(parsedCose.Key.AlgorithmPrivateKey, Is.EqualTo(privateSeed));
            Assert.That(convertedJwk.Success, Is.True);
            Assert.That(privateJwkThumb.Bytes, Is.EqualTo(publicJwkThumb.Bytes));
            Assert.That(privateCoseThumb.Bytes, Is.EqualTo(publicCoseThumb.Bytes));
        });

        service.ClearKeyMaterial(parsed.Key!);
        service.ClearKeyMaterial(parsedCose.Key!);
    }

    [Test]
    public void ClearingRemovesOwnedSecretsAndTemporaryBuffers()
    {
        ITokenKeyService service = new TokenKeyService();
        byte[] source = Enumerable.Repeat((byte)5, 32).ToArray();
        TokenKey key = service.ImportSymmetricKey(source).Key!;
        byte[] imported = key.SymmetricKey!;
        byte[] temporary = Enumerable.Repeat((byte)9, 12).ToArray();

        service.ClearKeyMaterial(key);
        service.ClearTemporaryMaterial(temporary);

        Assert.Multiple(() =>
        {
            Assert.That(source, Is.All.EqualTo(5));
            Assert.That(imported, Is.All.Zero);
            Assert.That(key.SymmetricKey, Is.Null);
            Assert.That(temporary, Is.All.Zero);
        });
    }

    static string B64(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
