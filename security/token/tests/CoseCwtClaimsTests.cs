using System.Security.Cryptography;
using token;

namespace token.Tests;

public class CoseCwtClaimsTests
{
    [Test]
    public void ProtectedClaimsRoundTripThroughCoseHeaderCodec()
    {
        CwtClaims claims = Claims("issuer", "subject");
        claims.SetByTextLabel("tenant", Text("example"));
        CoseHeader header = new CoseHeader();
        TokenPolicy policy = new TokenPolicy();

        TokenResult set = header.SetProtectedCwtClaims(claims, policy);
        TokenBytesResult encoded = header.WriteWithPolicy(policy);
        CoseHeaderResult parsedHeader = CoseHeader.Read(
            encoded.Bytes,
            policy);
        CoseCwtClaimsResult parsed = parsedHeader.Header.GetCwtClaims(policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(set);
            Assert.That(encoded.Success, Is.True);
            Assert.That(parsedHeader.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Present, Is.True);
            Assert.That(parsed.Protected, Is.True);
            Assert.That(parsed.Claims.Get(CwtClaimLabels.Issuer).Text,
                Is.EqualTo("issuer"));
            Assert.That(parsed.Claims.Get(CwtClaimLabels.Subject).Text,
                Is.EqualTo("subject"));
            Assert.That(parsed.Claims.GetByTextLabel("tenant").Text,
                Is.EqualTo("example"));
        });
    }

    [Test]
    public void UnprotectedClaimsCanBeReadRemovedAndMoved()
    {
        CoseHeader header = new CoseHeader();
        TokenPolicy policy = new TokenPolicy();
        TokenResult set = header.SetUnprotectedCwtClaims(
            Claims("issuer", "subject"),
            policy);
        CoseCwtClaimsResult unprotected = header.GetCwtClaims(policy);
        bool removed = header.RemoveCwtClaims();
        TokenResult moved = header.SetProtectedCwtClaims(
            Claims("other", "subject"),
            policy);
        CoseCwtClaimsResult protectedClaims = header.GetCwtClaims(policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(set);
            Assert.That(unprotected.Success, Is.True);
            Assert.That(unprotected.Protected, Is.False);
            Assert.That(removed, Is.True);
            AssertSuccess(moved);
            Assert.That(protectedClaims.Protected, Is.True);
            Assert.That(protectedClaims.Claims.Get(
                CwtClaimLabels.Issuer).Text, Is.EqualTo("other"));
        });
    }

    [Test]
    public void SetterRejectsOppositeBucketAndInvalidMutableClaims()
    {
        CoseHeader header = new CoseHeader();
        TokenPolicy policy = new TokenPolicy();
        TokenResult first = header.SetProtectedCwtClaims(
            Claims("issuer", "subject"),
            policy);
        TokenResult conflict = header.SetUnprotectedCwtClaims(
            Claims("issuer", "subject"),
            policy);
        CwtClaims invalid = new CwtClaims();
        invalid.Values = null;
        TokenResult invalidResult = new CoseHeader()
            .SetProtectedCwtClaims(invalid, policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(first);
            AssertFailure(
                conflict,
                "cwt_claims_header_bucket_conflict");
            AssertFailure(invalidResult, "invalid_cwt_claims");
        });
    }

    [Test]
    public void HeaderCodecRejectsWrongValueTypeAndInvalidClaimKeys()
    {
        CborItem wrongValue = HeaderBuckets(
            Array.Empty<byte>(),
            Map(Entry(CoseHeaderLabels.CwtClaims, Text("wrong"))));
        CborItem invalidKeyClaims = Map(
            new CborMapEntry(Bytes(new byte[] { 1 }), Text("value")));
        CborItem invalidKey = HeaderBuckets(
            Array.Empty<byte>(),
            Map(Entry(CoseHeaderLabels.CwtClaims, invalidKeyClaims)));

        CoseHeaderResult wrongResult = CoseHeader.Read(Encode(wrongValue));
        CoseHeaderResult keyResult = CoseHeader.Read(Encode(invalidKey));

        Assert.Multiple(() =>
        {
            AssertHeaderFailure(
                wrongResult,
                "invalid_cose_header_parameter");
            AssertHeaderFailure(keyResult, "invalid_cwt_claim_key");
        });
    }

    [Test]
    public void PayloadComparisonChecksOnlyOverlappingClaims()
    {
        TokenPolicy policy = new TokenPolicy();
        CwtClaims payload = Claims("issuer", "subject");
        payload.SetByTextLabel("payload-only", Text("value"));
        CwtClaims matchingHeader = new CwtClaims();
        matchingHeader.Set(CwtClaimLabels.Issuer, Text("issuer"));
        matchingHeader.SetByTextLabel("header-only", Unsigned(1));
        CwtClaims mismatchingHeader = new CwtClaims();
        mismatchingHeader.Set(CwtClaimLabels.Subject, Text("different"));
        CoseHeader matching = new CoseHeader();
        CoseHeader mismatching = new CoseHeader();
        matching.SetProtectedCwtClaims(matchingHeader, policy);
        mismatching.SetProtectedCwtClaims(mismatchingHeader, policy);

        TokenResult match = CoseCwtClaims.ValidateAgainstPayload(
            matching,
            payload,
            policy);
        TokenResult mismatch = CoseCwtClaims.ValidateAgainstPayload(
            mismatching,
            payload,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(match);
            AssertFailure(mismatch, "cwt_header_claim_mismatch");
        });
    }

    [Test]
    public void CwtValidationAuthenticatesAndMatchesHeaderClaims()
    {
        CwtClaims payload = Claims("issuer", "subject");
        TokenKey key = Symmetric(32, CoseAlgorithms.HmacSha256);
        TokenPolicy tokenPolicy = TokenPolicyFor("HMAC 256/256");
        CoseHeader matchingHeader = AlgorithmHeader(
            CoseAlgorithms.HmacSha256);
        matchingHeader.SetProtectedCwtClaims(
            Claims("issuer", "subject"),
            tokenPolicy);
        CoseHeader mismatchHeader = AlgorithmHeader(
            CoseAlgorithms.HmacSha256);
        mismatchHeader.SetProtectedCwtClaims(
            Claims("wrong", "subject"),
            tokenPolicy);
        CwtProtectionLayer matchingLayer =
            CwtProtectionLayer.CreateMac0(matchingHeader, key);
        CwtProtectionLayer mismatchLayer =
            CwtProtectionLayer.CreateMac0(mismatchHeader, key);
        TokenBytesResult matching = CborWebToken.Create(
            payload,
            matchingLayer,
            false,
            tokenPolicy);
        TokenBytesResult mismatch = CborWebToken.Create(
            payload,
            mismatchLayer,
            false,
            tokenPolicy);
        CwtValidationPolicy validationPolicy = CwtValidationPolicy.Create(
            tokenPolicy);
        CwtClaims resolverClaims = null;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            resolverClaims = context.UnverifiedCwtClaims;
            return KeySuccess(key);
        });

        CwtValidationResult matchingResult = CborWebToken.Validate(
            matching.Bytes,
            resolver,
            validationPolicy);
        CwtValidationResult mismatchResult = CborWebToken.Validate(
            mismatch.Bytes,
            resolver,
            validationPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(matchingResult.Success, Is.True);
            Assert.That(resolverClaims, Is.Not.Null);
            Assert.That(resolverClaims.Get(CwtClaimLabels.Issuer).Text,
                Is.EqualTo("wrong"));
            Assert.That(mismatchResult.Success, Is.False);
            Assert.That(mismatchResult.Error.Code,
                Is.EqualTo("cwt_header_claim_mismatch"));
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void EquivalentNestedMapValuesIgnoreMapSerializationOrder()
    {
        CwtClaims payload = new CwtClaims();
        payload.Set(100, Map(
            new CborMapEntry(Text("z"), Unsigned(1)),
            new CborMapEntry(Text("a"), Unsigned(2))));
        CwtClaims replicated = new CwtClaims();
        replicated.Set(100, Map(
            new CborMapEntry(Text("a"), Unsigned(2)),
            new CborMapEntry(Text("z"), Unsigned(1))));
        CoseHeader header = new CoseHeader();
        TokenPolicy policy = new TokenPolicy();
        header.SetProtectedCwtClaims(replicated, policy);

        TokenResult result = CoseCwtClaims.ValidateAgainstPayload(
            header,
            payload,
            policy);

        AssertSuccess(result);
    }

    [Test]
    public void CwtValidationMatchesClaimsInSignerHeaders()
    {
        CwtClaims payload = Claims("issuer", "subject");
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.Import(native, true).Key;
        TokenPolicy tokenPolicy = TokenPolicyFor("ESP256");
        CoseHeader signer = AlgorithmHeader(
            CoseAlgorithms.EcdsaP256Sha256);
        signer.SetProtectedCwtClaims(
            Claims("different", "subject"),
            tokenPolicy);
        CwtProtectionLayer layer = CwtProtectionLayer.CreateSign(
            new CoseHeader(),
            new CoseHeader[] { signer },
            new TokenKey[] { key });
        TokenBytesResult token = CborWebToken.Create(
            payload,
            layer,
            false,
            tokenPolicy);

        CwtValidationResult result = CborWebToken.Validate(
            token.Bytes,
            new TokenKeyResolver(context => KeySuccess(key)),
            CwtValidationPolicy.Create(tokenPolicy));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code,
            Is.EqualTo("cwt_header_claim_mismatch"));
    }

    static CwtClaims Claims(string issuer, string subject)
    {
        CwtClaims claims = new CwtClaims();
        claims.Set(CwtClaimLabels.Issuer, Text(issuer));
        claims.Set(CwtClaimLabels.Subject, Text(subject));
        return claims;
    }

    static CoseHeader AlgorithmHeader(int algorithm)
    {
        CoseHeader header = new CoseHeader();
        header.SetProtected(CoseHeaderLabels.Algorithm, Int(algorithm));
        return header;
    }

    static TokenKey Symmetric(int size, int algorithm)
    {
        byte[] material = new byte[size];
        int index = 0;
        while (index < material.Length)
        {
            material[index] = (byte)(index + 1);
            index += 1;
        }

        return SymmetricKeyImporterOperations.ImportCose(material, algorithm).Key;
    }

    static TokenPolicy TokenPolicyFor(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
        return result;
    }

    static CborItem HeaderBuckets(byte[] protectedBytes, CborItem unprotected)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = new CborItem[]
        {
            Bytes(protectedBytes),
            unprotected
        };
        return item;
    }

    static CborMapEntry Entry(int label, CborItem value)
    {
        return new CborMapEntry(Unsigned((ulong)label), value);
    }

    static CborItem Map(params CborMapEntry[] entries)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = entries;
        return item;
    }

    static CborItem Int(int value)
    {
        CborItem item = new CborItem();
        if (value >= 0)
        {
            item.Kind = TokenCborValueKind.UnsignedInteger;
            item.IntegerArgument = (ulong)value;
        }
        else
        {
            item.Kind = TokenCborValueKind.NegativeInteger;
            item.IntegerArgument = (ulong)(-(long)value - 1L);
        }

        return item;
    }

    static CborItem Unsigned(ulong value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.UnsignedInteger;
        item.IntegerArgument = value;
        return item;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static byte[] Encode(CborItem item)
    {
        CborWriter writer = CborWriter.Create();
        TokenResult written = writer.WriteDeterministicItem(item);
        Assert.That(written.Success, Is.True);
        return writer.ToArray();
    }

    static void AssertSuccess(TokenResult result)
    {
        Assert.That(result.Success, Is.True,
            result.Error == null ? "no error" : result.Error.Code);
        Assert.That(result.Error, Is.Null);
    }

    static void AssertFailure(TokenResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }

    static void AssertHeaderFailure(CoseHeaderResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Header, Is.Null);
    }
}
