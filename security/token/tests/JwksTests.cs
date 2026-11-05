using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using token;

namespace token.Tests;

public class JwksTests
{
    [Test]
    public void ReadParsesOrderedMixedKeySet()
    {
        using RSA rsa = RSA.Create(2048);
        RSAParameters rsaParameters = rsa.ExportParameters(false);
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ECParameters ecParameters = ecdsa.ExportParameters(false);
        string json = "{\"issuer\":\"ignored-extension\",\"keys\":["
            + "{\"kty\":\"oct\",\"k\":\"" + B64(new byte[32])
            + "\",\"kid\":\"symmetric\"},"
            + "{\"kty\":\"RSA\",\"n\":\"" + UInt(rsaParameters.Modulus)
            + "\",\"e\":\"" + UInt(rsaParameters.Exponent)
            + "\",\"kid\":\"rsa\"},"
            + "{\"kty\":\"EC\",\"crv\":\"P-256\",\"x\":\""
            + B64(ecParameters.Q.X) + "\",\"y\":\"" + B64(ecParameters.Q.Y)
            + "\",\"kid\":\"ec\"},"
            + "{\"kty\":\"OKP\",\"crv\":\"Ed25519\",\"x\":\""
            + B64(Enumerable.Repeat((byte)3, 32).ToArray())
            + "\",\"kid\":\"okp\"}]}";

        JwksResult result = JsonWebKeySet.Read(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Set, Is.Not.Null);
            Assert.That(result.Set.Keys, Has.Length.EqualTo(4));
            Assert.That(
                result.Set.Keys.Select(key => key.KeyId),
                Is.EqualTo(new[] { "symmetric", "rsa", "ec", "okp" }));
            Assert.That(result.Set.Keys[0].KeyType, Is.EqualTo(TokenKeyTypes.Symmetric));
            Assert.That(result.Set.Keys[1].KeyType, Is.EqualTo(TokenKeyTypes.Rsa));
            Assert.That(result.Set.Keys[2].KeyType, Is.EqualTo(TokenKeyTypes.EllipticCurve));
            Assert.That(result.Set.Keys[3].KeyType, Is.EqualTo(TokenKeyTypes.OctetKeyPair));
        });

        result.Set.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadUtf8AndEmptySetsSucceed()
    {
        JwksResult text = JsonWebKeySet.Read("{\"keys\":[]}");
        JwksResult utf8 = JsonWebKeySet.Read(Encoding.UTF8.GetBytes("{\"keys\":[]}"));

        Assert.Multiple(() =>
        {
            Assert.That(text.Success, Is.True);
            Assert.That(text.Set.Keys, Is.Empty);
            Assert.That(utf8.Success, Is.True);
            Assert.That(utf8.Set.Keys, Is.Empty);
        });
    }

    [Test]
    public void ReadRejectsMissingWrongTypeAndNonObjectKeys()
    {
        JwksResult missing = JsonWebKeySet.Read("{}");
        JwksResult nullKeys = JsonWebKeySet.Read("{\"keys\":null}");
        JwksResult objectKeys = JsonWebKeySet.Read("{\"keys\":{}}");
        JwksResult item = JsonWebKeySet.Read("{\"keys\":[1]}");

        Assert.Multiple(() =>
        {
            AssertFailure(missing, "missing_jwks_keys");
            AssertFailure(nullKeys, "invalid_jwks_keys");
            AssertFailure(objectKeys, "invalid_jwks_keys");
            AssertFailure(item, "invalid_jwks_key");
        });
    }

    [Test]
    public void ReadUsesStrictDuplicateDetectionAtEveryDepth()
    {
        JwksResult duplicateSetMember = JsonWebKeySet.Read(
            "{\"keys\":[],\"keys\":[]}");
        JwksResult duplicateKeyMember = JsonWebKeySet.Read(
            "{\"keys\":[{\"kty\":\"oct\",\"kty\":\"oct\","
            + "\"k\":\"" + B64(new byte[32]) + "\"}]}");

        Assert.Multiple(() =>
        {
            AssertFailure(duplicateSetMember, "duplicate_json_member");
            AssertFailure(duplicateKeyMember, "duplicate_json_member");
        });
    }

    [Test]
    public void ReadPropagatesNestedJwkFailureAfterEarlierValidKey()
    {
        string json = "{\"keys\":["
            + "{\"kty\":\"oct\",\"k\":\"" + B64(new byte[32]) + "\"},"
            + "{\"kty\":\"RSA\",\"n\":\"AAE\",\"e\":\"Aw\"}]}";

        JwksResult result = JsonWebKeySet.Read(json);

        AssertFailure(result, "invalid_unsigned_integer");
    }

    [Test]
    public void WriteProducesDeterministicCompactSetAndPreservesOrder()
    {
        TokenKey first = SymmetricKeyImporterOperations.Import(
            Enumerable.Repeat((byte)1, 32).ToArray()).Key;
        first.KeyId = "first";
        TokenKey second = SymmetricKeyImporterOperations.Import(
            Enumerable.Repeat((byte)2, 32).ToArray()).Key;
        second.KeyId = "second";
        JsonWebKeySet set = new JsonWebKeySet(new TokenKey[] { first, second });

        TokenTextResult written = set.Write();
        TokenTextResult repeated = JsonWebKeySet.Write(set.Keys);
        JsonObjectResult json = JsonObject.Read(written.Text);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(written.Error, Is.Null);
            Assert.That(written.Text, Is.EqualTo(repeated.Text));
            Assert.That(written.Text, Does.StartWith("{\"keys\":[{"));
            Assert.That(written.Text, Does.EndWith("}]}"));
            Assert.That(json.Object.Members, Has.Length.EqualTo(1));
            Assert.That(json.Object.Get("keys").ArrayValue, Has.Length.EqualTo(2));
            Assert.That(
                json.Object.Get("keys").ArrayValue[0].ObjectValue.Get("kid").StringValue,
                Is.EqualTo("first"));
            Assert.That(
                json.Object.Get("keys").ArrayValue[1].ObjectValue.Get("kid").StringValue,
                Is.EqualTo("second"));
        });

        SymmetricKeyImporterOperations.Clear(first);
        SymmetricKeyImporterOperations.Clear(second);
    }

    [Test]
    public void WriteAndReadRoundTripPublicAndPrivateKeys()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKey rsaKey = RsaKeyImporterOperations.Import(rsa).Key;
        rsaKey.KeyId = "rsa";
        TokenKey symmetric = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        symmetric.KeyId = "oct";

        TokenTextResult written = JsonWebKeySet.Write(
            new TokenKey[] { rsaKey, symmetric });
        JwksResult parsed = JsonWebKeySet.Read(written.Text);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Set.Keys, Has.Length.EqualTo(2));
            Assert.That(parsed.Set.Keys[0].KeyType, Is.EqualTo(TokenKeyTypes.Rsa));
            Assert.That(parsed.Set.Keys[0].NativePrivateMaterial, Is.False);
            Assert.That(parsed.Set.Keys[1].KeyType, Is.EqualTo(TokenKeyTypes.Symmetric));
            Assert.That(parsed.Set.Keys[1].ContainsPrivateMaterial(), Is.True);
        });

        parsed.Set.ReleaseOwnedMaterial();
        SymmetricKeyImporterOperations.Clear(symmetric);
    }

    [Test]
    public void WriteEmptySetUsesCanonicalShape()
    {
        JsonWebKeySet set = JsonWebKeySet.CreateEmpty();

        TokenTextResult result = set.Write();

        Assert.That(result.Text, Is.EqualTo("{\"keys\":[]}"));
    }

    [Test]
    public void ConstructorCopiesKeyReferenceArray()
    {
        TokenKey first = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        TokenKey second = SymmetricKeyImporterOperations.Import(
            Enumerable.Repeat((byte)1, 32).ToArray()).Key;
        TokenKey[] supplied = new TokenKey[] { first };
        JsonWebKeySet set = new JsonWebKeySet(supplied);
        supplied[0] = second;

        Assert.Multiple(() =>
        {
            Assert.That(set.Keys, Is.Not.SameAs(supplied));
            Assert.That(set.Keys[0], Is.SameAs(first));
        });

        SymmetricKeyImporterOperations.Clear(first);
        SymmetricKeyImporterOperations.Clear(second);
    }

    [Test]
    public void ReleaseOwnedMaterialClearsEveryParsedKey()
    {
        string json = "{\"keys\":["
            + "{\"kty\":\"oct\",\"k\":\"" + B64(new byte[32]) + "\"},"
            + "{\"kty\":\"OKP\",\"crv\":\"Ed25519\",\"x\":\""
            + B64(new byte[32]) + "\",\"d\":\""
            + B64(Enumerable.Repeat((byte)9, 32).ToArray()) + "\"}]}";
        JwksResult parsed = JsonWebKeySet.Read(json);
        byte[] symmetric = parsed.Set.Keys[0].SymmetricKey;
        byte[] okpPrivate = parsed.Set.Keys[1].OkpPrivateKey;

        parsed.Set.ReleaseOwnedMaterial();
        parsed.Set.ReleaseOwnedMaterial();

        Assert.Multiple(() =>
        {
            Assert.That(symmetric, Is.All.Zero);
            Assert.That(okpPrivate, Is.All.Zero);
            Assert.That(parsed.Set.Keys[0].SymmetricKey, Is.Null);
            Assert.That(parsed.Set.Keys[1].OkpPrivateKey, Is.Null);
        });
    }

    [Test]
    public void WriteRejectsNullEntriesAndPropagatesJwkFailures()
    {
        TokenKey invalid = new TokenKey();
        TokenTextResult nullEntry = JsonWebKeySet.Write(new TokenKey[] { null });
        TokenTextResult invalidEntry = JsonWebKeySet.Write(new TokenKey[] { invalid });

        Assert.Multiple(() =>
        {
            AssertWriteFailure(nullEntry, "invalid_jwks_key");
            AssertWriteFailure(invalidEntry, "missing_key_material");
        });
    }

    [Test]
    public void SetApisRejectNullArguments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => JsonWebKeySet.Read((string)null), Throws.ArgumentNullException);
            Assert.That(() => JsonWebKeySet.Read((byte[])null), Throws.ArgumentNullException);
            Assert.That(() => JsonWebKeySet.Write(null), Throws.ArgumentNullException);
            Assert.That(() => new JsonWebKeySet(null), Throws.ArgumentNullException);
            Assert.That(() => JsonWebKeySet.CreateEmpty().Select(null), Throws.ArgumentNullException);
            Assert.That(() => JsonWebKeySet.CreateEmpty().Resolve(null), Throws.ArgumentNullException);
        });
    }

    [Test]
    public void SelectReturnsUniqueKeyMatchingEveryJoseCriterion()
    {
        TokenKey first = SelectionKey("first", TokenKeyTypes.Rsa);
        TokenKey selected = SelectionKey("selected", TokenKeyTypes.EllipticCurve);
        selected.Algorithm = JoseAlgorithms.EcdsaSha256;
        selected.Use = TokenKeyUses.Signature;
        selected.KeyOperations = new string[]
        {
            TokenKeyOperations.Sign,
            TokenKeyOperations.Verify
        };
        selected.Curve = "P-256";
        selected.CertificateSha1Thumbprint = Enumerable.Repeat((byte)5, 20).ToArray();
        selected.CertificateSha256Thumbprint = Enumerable.Repeat((byte)6, 32).ToArray();
        TokenKey last = SelectionKey("last", TokenKeyTypes.EllipticCurve);
        last.Algorithm = JoseAlgorithms.EcdsaSha256;
        last.Use = TokenKeyUses.Encryption;
        last.KeyOperations = new string[] { TokenKeyOperations.Verify };
        last.Curve = "P-256";
        last.CertificateSha1Thumbprint = selected.CertificateSha1Thumbprint.ToArray();
        last.CertificateSha256Thumbprint = selected.CertificateSha256Thumbprint.ToArray();
        JsonWebKeySet set = new JsonWebKeySet(new TokenKey[] { first, selected, last });
        TokenKeyQuery query = new TokenKeyQuery();
        query.KeyId = "selected";
        query.KeyType = TokenKeyTypes.EllipticCurve;
        query.Algorithm = JoseAlgorithms.EcdsaSha256;
        query.Use = TokenKeyUses.Signature;
        query.KeyOperation = TokenKeyOperations.Verify;
        query.Curve = "P-256";
        query.CertificateSha1Thumbprint = selected.CertificateSha1Thumbprint.ToArray();
        query.CertificateSha256Thumbprint = selected.CertificateSha256Thumbprint.ToArray();

        TokenKeyResult result = set.Select(query);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Key, Is.SameAs(selected));
        });
    }

    [Test]
    public void ResolveUsesIdentifiersAndDeclaredAlgorithms()
    {
        TokenKey selected = SelectionKey("selected", TokenKeyTypes.Symmetric);
        TokenKey conflicting = SelectionKey("selected", TokenKeyTypes.Rsa);
        conflicting.Algorithm = JoseAlgorithms.RsaSha256;
        JsonWebKeySet set = new JsonWebKeySet(
            new TokenKey[] { selected, conflicting });
        TokenKeyResolutionContext context = new TokenKeyResolutionContext();
        context.KeyId = "selected";
        context.Algorithm = JoseAlgorithms.HmacSha256;
        context.Operation = TokenKeyOperations.Verify;

        TokenKeyResult result = set.Resolve(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Key, Is.SameAs(selected));
        });
    }

    [Test]
    public void ResolveTreatsAMissingBinaryIdentifierAsANonMatch()
    {
        TokenKey key = SelectionKey(
            "text-only",
            TokenKeyTypes.Symmetric);
        JsonWebKeySet set = new JsonWebKeySet(
            new TokenKey[] { key });
        TokenKeyResolutionContext context =
            new TokenKeyResolutionContext();
        context.KeyIdBytes = new byte[] { 1, 2, 3 };

        TokenKeyResult result = set.Resolve(context);

        AssertSelectionFailure(result, "jwks_key_not_found");
    }

    [Test]
    public void SelectAppliesEachRequestedJoseFilter()
    {
        TokenKey key = SelectionKey("key", TokenKeyTypes.EllipticCurve);
        key.Algorithm = JoseAlgorithms.EcdsaSha256;
        key.Use = TokenKeyUses.Signature;
        key.KeyOperations = new string[] { TokenKeyOperations.Verify };
        key.Curve = "P-256";
        key.CertificateSha1Thumbprint = Enumerable.Repeat((byte)1, 20).ToArray();
        key.CertificateSha256Thumbprint = Enumerable.Repeat((byte)2, 32).ToArray();
        JsonWebKeySet set = new JsonWebKeySet(new TokenKey[] { key });
        TokenKeyQuery[] mismatches = new TokenKeyQuery[]
        {
            Query("other", null, null, null, null, null, null, null),
            Query(null, TokenKeyTypes.Rsa, null, null, null, null, null, null),
            Query(null, null, JoseAlgorithms.EcdsaSha384, null, null, null, null, null),
            Query(null, null, null, TokenKeyUses.Encryption, null, null, null, null),
            Query(null, null, null, null, TokenKeyOperations.Sign, null, null, null),
            Query(null, null, null, null, null, "P-384", null, null),
            Query(null, null, null, null, null, null, new byte[20], null),
            Query(null, null, null, null, null, null, null, new byte[32])
        };

        Assert.Multiple(() =>
        {
            foreach (TokenKeyQuery query in mismatches)
            {
                AssertSelectionFailure(set.Select(query), "jwks_key_not_found");
            }
        });
    }

    [Test]
    public void SelectUsesCalculatedCertificateThumbprints()
    {
        using RSA rsa = RSA.Create(2048);
        CertificateRequest request = new CertificateRequest(
            "CN=jwks-selection",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        DateTimeOffset start = DateTimeOffset.UtcNow.AddMinutes(-1);
        X509SignatureGenerator generator = X509SignatureGenerator.CreateForRSA(
            rsa,
            RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.Create(
            request.SubjectName,
            generator,
            start,
            start.AddDays(1),
            new byte[] { 3 });
        TokenKey key = SelectionKey("certificate", TokenKeyTypes.Rsa);
        key.Certificate = certificate;
        JsonWebKeySet set = new JsonWebKeySet(new TokenKey[] { key });
        TokenKeyQuery query = new TokenKeyQuery();
        query.CertificateSha1Thumbprint = certificate.GetCertHash(HashAlgorithmName.SHA1);
        query.CertificateSha256Thumbprint = certificate.GetCertHash(HashAlgorithmName.SHA256);

        TokenKeyResult result = set.Select(query);

        Assert.That(result.Key, Is.SameAs(key));
    }

    [Test]
    public void SelectFailsWhenNoKeyMatches()
    {
        JsonWebKeySet set = new JsonWebKeySet(new TokenKey[]
        {
            SelectionKey("first", TokenKeyTypes.Rsa),
            SelectionKey("second", TokenKeyTypes.EllipticCurve)
        });
        TokenKeyQuery query = new TokenKeyQuery();
        query.KeyId = "missing";

        TokenKeyResult result = set.Select(query);

        AssertSelectionFailure(result, "jwks_key_not_found");
    }

    [Test]
    public void SelectFailsWhenMoreThanOneKeyMatches()
    {
        TokenKey first = SelectionKey("first", TokenKeyTypes.Rsa);
        TokenKey second = SelectionKey("second", TokenKeyTypes.Rsa);
        first.Use = TokenKeyUses.Signature;
        second.Use = TokenKeyUses.Signature;
        JsonWebKeySet set = new JsonWebKeySet(new TokenKey[] { first, second });
        TokenKeyQuery query = new TokenKeyQuery();
        query.KeyType = TokenKeyTypes.Rsa;
        query.Use = TokenKeyUses.Signature;

        TokenKeyResult result = set.Select(query);

        AssertSelectionFailure(result, "ambiguous_jwks_key");
    }

    [Test]
    public void SelectHandlesEmptyMutableAndNullEntrySets()
    {
        JsonWebKeySet empty = JsonWebKeySet.CreateEmpty();
        JsonWebKeySet unset = JsonWebKeySet.CreateEmpty();
        unset.Keys = null;
        JsonWebKeySet nullEntry = new JsonWebKeySet(new TokenKey[] { null });

        Assert.Multiple(() =>
        {
            AssertSelectionFailure(empty.Select(new TokenKeyQuery()), "jwks_key_not_found");
            AssertSelectionFailure(unset.Select(new TokenKeyQuery()), "jwks_key_not_found");
            AssertSelectionFailure(nullEntry.Select(new TokenKeyQuery()), "jwks_key_not_found");
        });
    }

    [Test]
    public void SelectWithEmptyQueryRequiresExactlyOneKey()
    {
        TokenKey key = SelectionKey("only", TokenKeyTypes.Rsa);
        JsonWebKeySet singleton = new JsonWebKeySet(new TokenKey[] { key });
        JsonWebKeySet multiple = new JsonWebKeySet(new TokenKey[]
        {
            key,
            SelectionKey("other", TokenKeyTypes.Rsa)
        });

        TokenKeyResult selected = singleton.Select(new TokenKeyQuery());
        TokenKeyResult ambiguous = multiple.Select(new TokenKeyQuery());

        Assert.Multiple(() =>
        {
            Assert.That(selected.Success, Is.True);
            Assert.That(selected.Key, Is.SameAs(key));
            AssertSelectionFailure(ambiguous, "ambiguous_jwks_key");
        });
    }

    static string UInt(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length - 1 && bytes[index] == 0)
        {
            index++;
        }

        return B64(bytes.AsSpan(index).ToArray());
    }

    static string B64(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    static void AssertFailure(JwksResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Set, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
        });
    }

    static void AssertWriteFailure(TokenTextResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
        });
    }

    static TokenKey SelectionKey(string keyId, string keyType)
    {
        TokenKey key = new TokenKey();
        key.KeyId = keyId;
        key.KeyType = keyType;
        return key;
    }

    static TokenKeyQuery Query(
        string keyId,
        string keyType,
        string algorithm,
        string use,
        string operation,
        string curve,
        byte[] sha1,
        byte[] sha256)
    {
        TokenKeyQuery query = new TokenKeyQuery();
        query.KeyId = keyId;
        query.KeyType = keyType;
        query.Algorithm = algorithm;
        query.Use = use;
        query.KeyOperation = operation;
        query.Curve = curve;
        query.CertificateSha1Thumbprint = sha1;
        query.CertificateSha256Thumbprint = sha256;
        return query;
    }

    static void AssertSelectionFailure(TokenKeyResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Key, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Position, Is.EqualTo(-1));
        });
    }
}
