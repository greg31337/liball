using token;

namespace token.Tests;

public class CwtClaimsTests
{
    const string Rfc8392Claims =
        "A70175636F61703A2F2F61732E6578616D706C652E636F6D02656572696B77037818636F61703A2F2F6C696768742E6578616D706C652E636F6D041A5612AEB0051A5610D9F0061A5610D9F007420B71";

    [Test]
    public void Rfc8392ClaimSetParsesAndRewritesExactly()
    {
        byte[] encoded = Convert.FromHexString(Rfc8392Claims);

        CwtClaimsResult parsed = CwtClaims.Read(encoded);
        TokenBytesResult written = parsed.Claims.Write();

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Claims.Get(CwtClaimLabels.Issuer).Text,
                Is.EqualTo("coap://as.example.com"));
            Assert.That(parsed.Claims.Get(CwtClaimLabels.Subject).Text,
                Is.EqualTo("erikw"));
            Assert.That(parsed.Claims.Get(CwtClaimLabels.Audience).Text,
                Is.EqualTo("coap://light.example.com"));
            Assert.That(parsed.Claims.Get(CwtClaimLabels.CwtId).Bytes,
                Is.EqualTo(new byte[] { 0x0B, 0x71 }));
            Assert.That(written.Success, Is.True);
            Assert.That(written.Bytes, Is.EqualTo(encoded));
        });
    }

    [Test]
    public void ReadParsesRegisteredAndPrivateClaims()
    {
        byte[] encoded = Convert.FromHexString(
            "A501666973737565720382636170696561646D696E041A6553F10007430102036470726976A2616EF66162F5");

        CwtClaimsResult result = CwtClaims.Read(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Claims.Values, Has.Length.EqualTo(5));
            Assert.That(result.Claims.Get(CwtClaimLabels.Issuer).Text,
                Is.EqualTo("issuer"));
            Assert.That(result.Claims.Get(CwtClaimLabels.Audience).ArrayValue
                .Select(value => value.Text),
                Is.EqualTo(new[] { "api", "admin" }));
            Assert.That(result.Claims.Get(CwtClaimLabels.ExpirationTime)
                .IntegerArgument, Is.EqualTo(1700000000UL));
            Assert.That(result.Claims.Get(CwtClaimLabels.CwtId).Bytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.Claims.GetByTextLabel("priv").MapValue[0].Value.Kind,
                Is.EqualTo(TokenCborValueKind.Null));
            Assert.That(result.Claims.GetByTextLabel("priv").MapValue[1].Value.BooleanValue,
                Is.True);
        });
    }

    [Test]
    public void WriteUsesDeterministicMapOrderingAndRoundTrips()
    {
        CwtClaims claims = new CwtClaims();
        claims.SetByTextLabel("z", Text("last"));
        claims.Set(-1, Boolean(true));
        claims.Set(CwtClaimLabels.Issuer, Text("issuer"));
        claims.Set(CwtClaimLabels.CwtId, Bytes(new byte[] { 1, 2 }));

        TokenBytesResult written = claims.Write();
        CwtClaimsResult reparsed = CwtClaims.Read(written.Bytes);
        CborItem map = CborReader.Create(written.Bytes).Read().Item;

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(written.Error, Is.Null);
            Assert.That(Convert.ToHexString(written.Bytes),
                Is.EqualTo("A401666973737565720742010220F5617A646C617374"));
            Assert.That(map.MapValue.Select(entry => entry.Key.Kind),
                Is.EqualTo(new[]
                {
                    TokenCborValueKind.UnsignedInteger,
                    TokenCborValueKind.UnsignedInteger,
                    TokenCborValueKind.NegativeInteger,
                    TokenCborValueKind.TextString
                }));
            Assert.That(reparsed.Success, Is.True);
            Assert.That(reparsed.Claims.Get(-1).BooleanValue, Is.True);
            Assert.That(reparsed.Claims.GetByTextLabel("z").Text, Is.EqualTo("last"));
        });
    }

    [Test]
    public void SetReplaceRemoveAndIntegerTextLabelsAreDistinct()
    {
        CwtClaims claims = new CwtClaims();
        CborItem replacement = Text("replacement");
        claims.Set(1, Text("integer"));
        claims.SetByTextLabel("1", Text("text"));
        claims.Set(1, replacement);

        bool removed = claims.RemoveByTextLabel("1");
        bool missing = claims.RemoveByTextLabel("missing");

        Assert.Multiple(() =>
        {
            Assert.That(claims.Values, Has.Length.EqualTo(1));
            Assert.That(claims.Get(1), Is.SameAs(replacement));
            Assert.That(claims.GetByTextLabel("1"), Is.Null);
            Assert.That(removed, Is.True);
            Assert.That(missing, Is.False);
        });
    }

    [Test]
    public void ReadOwnsByteStringDataIndependentlyOfInput()
    {
        byte[] encoded = Convert.FromHexString("A10743010203");

        CwtClaimsResult result = CwtClaims.Read(encoded);
        Array.Fill(encoded, (byte)0);
        TokenBytesResult written = result.Claims.Write();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Claims.Get(CwtClaimLabels.CwtId).Bytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(Convert.ToHexString(written.Bytes),
                Is.EqualTo("A10743010203"));
        });
    }

    [Test]
    public void ReadRejectsMalformedRootsKeysDuplicatesAndTrailingData()
    {
        CwtClaimsResult nonMap = CwtClaims.Read(
            Convert.FromHexString("80"));
        CwtClaimsResult invalidKey = CwtClaims.Read(
            Convert.FromHexString("A1410101"));
        CwtClaimsResult duplicate = CwtClaims.Read(
            Convert.FromHexString("A2016161016162"));
        CwtClaimsResult trailing = CwtClaims.Read(
            Convert.FromHexString("A0A0"));
        CwtClaimsResult malformed = CwtClaims.Read(
            Convert.FromHexString("A101"));

        Assert.Multiple(() =>
        {
            AssertFailure(nonMap, "cwt_claims_not_map");
            AssertFailure(invalidKey, "invalid_cwt_claim_key");
            AssertFailure(duplicate, "duplicate_cbor_map_key");
            AssertFailure(trailing, "trailing_cwt_claim_data");
            AssertFailure(malformed, "unexpected_end_of_cbor");
        });
    }

    [Test]
    public void PolicyLimitsApplyToReadAndWrite()
    {
        byte[] encoded = Convert.FromHexString("A16470726976A16178F5");
        TokenPolicy exact = new TokenPolicy();
        exact.MaximumTokenSizeInBytes = encoded.Length;
        TokenPolicy small = new TokenPolicy();
        small.MaximumTokenSizeInBytes = encoded.Length - 1;
        TokenPolicy shallow = new TokenPolicy();
        shallow.MaximumCborDepth = 1;
        TokenPolicy invalid = new TokenPolicy();
        invalid.MaximumCborDepth = 0;
        CwtClaims claims = CwtClaims.Read(encoded).Claims;

        CwtClaimsResult exactRead = CwtClaims.Read(encoded, exact);
        CwtClaimsResult smallRead = CwtClaims.Read(encoded, small);
        CwtClaimsResult shallowRead = CwtClaims.Read(encoded, shallow);
        CwtClaimsResult invalidRead = CwtClaims.Read(encoded, invalid);
        TokenBytesResult exactWrite = claims.WriteWithPolicy(exact);
        TokenBytesResult smallWrite = claims.WriteWithPolicy(small);
        TokenBytesResult shallowWrite = claims.WriteWithPolicy(shallow);

        Assert.Multiple(() =>
        {
            Assert.That(exactRead.Success, Is.True);
            AssertFailure(smallRead, "cbor_size_exceeded");
            AssertFailure(shallowRead, "cbor_depth_exceeded");
            AssertFailure(invalidRead, "invalid_policy");
            Assert.That(exactWrite.Success, Is.True);
            AssertBytesFailure(smallWrite, "buffer_limit_exceeded");
            AssertBytesFailure(shallowWrite, "cbor_depth_exceeded");
        });
    }

    [Test]
    public void WriteRejectsInvalidMutableModels()
    {
        CwtClaims missingValues = new CwtClaims();
        missingValues.Values = null;
        CwtClaims missingEntry = CwtClaims.Create(new CborMapEntry[] { null });
        CwtClaims invalidKey = CwtClaims.Create(new CborMapEntry[]
        {
            new CborMapEntry(Bytes(new byte[] { 1 }), Boolean(true))
        });
        CwtClaims duplicate = CwtClaims.Create(new CborMapEntry[]
        {
            new CborMapEntry(Unsigned(1), Text("one")),
            new CborMapEntry(Unsigned(1), Text("two"))
        });

        TokenBytesResult missingValuesResult = missingValues.Write();
        TokenBytesResult missingEntryResult = missingEntry.Write();
        TokenBytesResult invalidKeyResult = invalidKey.Write();
        TokenBytesResult duplicateResult = duplicate.Write();

        Assert.Multiple(() =>
        {
            AssertBytesFailure(missingValuesResult, "invalid_cwt_claims");
            AssertBytesFailure(missingEntryResult, "invalid_cbor_map");
            AssertBytesFailure(invalidKeyResult, "invalid_cwt_claim_key");
            AssertBytesFailure(duplicateResult, "duplicate_cbor_map_key");
        });
    }

    [Test]
    public void FromMapCopiesEntryArrayAndValidatesKeys()
    {
        CborMapEntry entry = new CborMapEntry(Unsigned(1), Text("issuer"));
        CborMapEntry[] entries = new CborMapEntry[] { entry };

        CwtClaimsResult result = CwtClaims.FromMap(entries);
        entries[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Claims.Values[0], Is.SameAs(entry));
            Assert.That(result.Claims.Get(1).Text, Is.EqualTo("issuer"));
        });
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

    static CborItem Boolean(bool value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Boolean;
        item.BooleanValue = value;
        return item;
    }

    static void AssertFailure(CwtClaimsResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Claims, Is.Null);
    }

    static void AssertBytesFailure(TokenBytesResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Bytes, Is.Null);
    }
}
