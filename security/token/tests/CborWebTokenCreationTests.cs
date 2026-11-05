using System.Security.Cryptography;
using token;

namespace token.Tests;

public class CborWebTokenCreationTests
{
    [Test]
    public void AutomaticCreationSelectsSign1Mac0AndEncrypt0()
    {
        CwtClaims claims = Claims();
        TokenBytesResult claimsBytes = claims.Write();
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey signing = EcdsaKeyImporter.Import(ecdsa, true).Key;
        signing.CoseAlgorithm = CoseAlgorithms.EcdsaP256Sha256;
        signing.HasCoseAlgorithm = true;
        TokenKey mac = Symmetric(32, CoseAlgorithms.HmacSha256);
        TokenKey encryption = Symmetric(16, CoseAlgorithms.Aes128Gcm);
        TokenPolicy policy = Policy("ESP256", "HMAC 256/256", "A128GCM");

        TokenBytesResult signed = CborWebToken.Create(
            claims,
            signing,
            policy);
        TokenBytesResult maced = CborWebToken.Create(
            claims,
            mac,
            policy);
        TokenBytesResult encrypted = CborWebToken.Create(
            claims,
            encryption,
            policy);
        CoseSign1Result verified = CoseSign1Operations.Verify(
            signed.Bytes,
            signing,
            policy);
        CoseMac0Result macVerified = CoseMac0Operations.Verify(
            maced.Bytes,
            mac,
            policy);
        CoseEncrypt0Result decrypted = CoseEncrypt0Operations.Decrypt(
            encrypted.Bytes,
            encryption,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(signed);
            AssertSuccess(maced);
            AssertSuccess(encrypted);
            Assert.That(Decode(signed.Bytes).TagValue, Is.EqualTo(18));
            Assert.That(Decode(maced.Bytes).TagValue, Is.EqualTo(17));
            Assert.That(Decode(encrypted.Bytes).TagValue, Is.EqualTo(16));
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Message.SignatureVerified, Is.True);
            Assert.That(verified.Message.Payload, Is.EqualTo(claimsBytes.Bytes));
            Assert.That(macVerified.Success, Is.True);
            Assert.That(macVerified.Message.TagVerified, Is.True);
            Assert.That(macVerified.Message.Payload, Is.EqualTo(claimsBytes.Bytes));
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Message.AuthenticationTagVerified, Is.True);
            Assert.That(decrypted.Message.Plaintext, Is.EqualTo(claimsBytes.Bytes));
        });

        mac.ReleaseOwnedMaterial();
        encryption.ReleaseOwnedMaterial();
    }

    [Test]
    public void ExplicitLayerBindsExternalAuthenticatedData()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey signing = EcdsaKeyImporter.Import(ecdsa, true).Key;
        byte[] external = new byte[] { 4, 5, 6 };
        CwtProtectionLayer layer = CwtProtectionLayer.CreateSign1(
            AlgorithmHeader(CoseAlgorithms.EcdsaP256Sha256),
            signing);
        layer.ExternalAuthenticatedData = external;
        TokenPolicy policy = Policy("ESP256");

        TokenBytesResult created = CborWebToken.Create(
            Claims(),
            layer,
            false,
            policy);
        CoseSign1Result verified =
            CoseSign1Operations.VerifyWithExternalAuthenticatedData(
                created.Bytes,
                external,
                signing,
                false,
                policy);
        CoseSign1Result wrong =
            CoseSign1Operations.VerifyWithExternalAuthenticatedData(
                created.Bytes,
                new byte[] { 9 },
                signing,
                false,
                policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Message.SignatureVerified, Is.True);
            Assert.That(wrong.Success, Is.False);
            Assert.That(wrong.Error.Code, Is.EqualTo("signature_mismatch"));
        });
    }

    [Test]
    public void MultiPartySignMacAndEncryptFormsAreCreated()
    {
        CwtClaims claims = Claims();
        TokenBytesResult claimsBytes = claims.Write();
        using ECDsa firstNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa secondNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey first = EcdsaKeyImporter.Import(firstNative, true).Key;
        TokenKey second = EcdsaKeyImporter.Import(secondNative, true).Key;
        CwtProtectionLayer sign = CwtProtectionLayer.CreateSign(
            new CoseHeader(),
            new CoseHeader[]
            {
                AlgorithmHeader(CoseAlgorithms.EcdsaP256Sha256),
                AlgorithmHeader(CoseAlgorithms.EcdsaP256Sha256)
            },
            new TokenKey[] { first, second });

        TokenKey macKey = Symmetric(32, CoseAlgorithms.Direct);
        CwtProtectionLayer mac = CwtProtectionLayer.CreateMac(
            AlgorithmHeader(CoseAlgorithms.HmacSha256),
            new CoseHeader[] { RecipientHeader(CoseAlgorithms.Direct) },
            new TokenKey[] { macKey },
            null);

        TokenKey encryptionKey = Symmetric(16, CoseAlgorithms.Direct);
        CwtProtectionLayer encrypt = CwtProtectionLayer.CreateEncrypt(
            AlgorithmHeader(CoseAlgorithms.Aes128Gcm),
            new CoseHeader[] { RecipientHeader(CoseAlgorithms.Direct) },
            new TokenKey[] { encryptionKey },
            null);
        TokenPolicy policy = Policy(
            "ESP256",
            "HMAC 256/256",
            "A128GCM",
            "direct");

        TokenBytesResult signed = CborWebToken.Create(
            claims,
            sign,
            false,
            policy);
        TokenBytesResult maced = CborWebToken.Create(
            claims,
            mac,
            false,
            policy);
        TokenBytesResult encrypted = CborWebToken.Create(
            claims,
            encrypt,
            false,
            policy);
        CoseSignResult parsedSign = CoseSign.Read(signed.Bytes);
        CoseMacResult parsedMac = CoseMac.Read(maced.Bytes);
        CoseSignResult verifiedSign = CoseSignOperations.Verify(
            signed.Bytes,
            new TokenKey[] { first, second },
            policy);
        CoseMacResult verifiedMac = CoseMacOperations.Verify(
            maced.Bytes,
            0,
            macKey,
            policy);
        CoseEncryptResult decrypted = CoseEncryptOperations.Decrypt(
            encrypted.Bytes,
            0,
            encryptionKey,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(signed);
            AssertSuccess(maced);
            AssertSuccess(encrypted);
            Assert.That(parsedSign.Success, Is.True);
            Assert.That(parsedSign.Message.Signatures, Has.Length.EqualTo(2));
            Assert.That(parsedSign.Message.Payload, Is.EqualTo(claimsBytes.Bytes));
            Assert.That(verifiedSign.Success, Is.True);
            Assert.That(verifiedSign.Message.Signatures.All(
                signature => signature.SignatureVerified), Is.True);
            Assert.That(parsedMac.Success, Is.True);
            Assert.That(parsedMac.Message.Recipients, Has.Length.EqualTo(1));
            Assert.That(parsedMac.Message.Payload, Is.EqualTo(claimsBytes.Bytes));
            Assert.That(verifiedMac.Success, Is.True);
            Assert.That(verifiedMac.Message.TagVerified, Is.True);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Message.Plaintext, Is.EqualTo(claimsBytes.Bytes));
        });

        macKey.ReleaseOwnedMaterial();
        encryptionKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void NestedSignedThenEncryptedCwtUsesTagsAndContentType()
    {
        CwtClaims claims = Claims();
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey signing = EcdsaKeyImporter.Import(ecdsa, true).Key;
        TokenKey encryption = Symmetric(16, CoseAlgorithms.Aes128Gcm);
        CwtProtectionLayer inner = CwtProtectionLayer.CreateSign1(
            AlgorithmHeader(CoseAlgorithms.EcdsaP256Sha256),
            signing);
        CwtProtectionLayer outer = CwtProtectionLayer.CreateEncrypt0(
            AlgorithmHeader(CoseAlgorithms.Aes128Gcm),
            encryption);
        TokenPolicy policy = Policy("ESP256", "A128GCM");

        TokenBytesResult created = CborWebToken.CreateNested(
            claims,
            new CwtProtectionLayer[] { inner, outer },
            true,
            policy);
        CborItem cwt = Decode(created.Bytes);
        byte[] outerBytes = Encode(cwt.TaggedValue);
        CoseEncrypt0Result decrypted = CoseEncrypt0Operations.Decrypt(
            outerBytes,
            encryption,
            policy);
        CoseSign1Result verified = CoseSign1Operations.Verify(
            decrypted.Message.Plaintext,
            signing,
            policy);
        CwtClaimsResult parsedClaims = CwtClaims.Read(
            verified.Message.Payload);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(cwt.Kind, Is.EqualTo(TokenCborValueKind.Tag));
            Assert.That(cwt.TagValue, Is.EqualTo(61));
            Assert.That(cwt.TaggedValue.TagValue, Is.EqualTo(16));
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Message.Header.ContentTypeText,
                Is.EqualTo("application/cwt"));
            Assert.That(Decode(decrypted.Message.Plaintext).TagValue,
                Is.EqualTo(18));
            Assert.That(verified.Success, Is.True);
            Assert.That(parsedClaims.Success, Is.True);
            Assert.That(parsedClaims.Claims.Get(CwtClaimLabels.Issuer).Text,
                Is.EqualTo("issuer"));
        });

        encryption.ReleaseOwnedMaterial();
    }

    [Test]
    public void CreationRejectsMissingAlgorithmsLayersAndInvalidModels()
    {
        CwtClaims claims = Claims();
        TokenKey noAlgorithm = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        TokenKey unsupported = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        unsupported.CoseAlgorithm = CoseAlgorithms.Direct;
        unsupported.HasCoseAlgorithm = true;
        CwtProtectionLayer invalid = new CwtProtectionLayer();
        invalid.Keys = Array.Empty<TokenKey>();
        CwtProtectionLayer unknown = new CwtProtectionLayer();
        unknown.Kind = (CwtProtectionKind)999;
        unknown.Keys = new TokenKey[] { noAlgorithm };
        TokenPolicy policy = Policy("direct");

        TokenBytesResult missingAlgorithm = CborWebToken.Create(
            claims,
            noAlgorithm,
            policy);
        TokenBytesResult invalidAlgorithm = CborWebToken.Create(
            claims,
            unsupported,
            policy);
        TokenBytesResult noLayers = CborWebToken.CreateNested(
            claims,
            Array.Empty<CwtProtectionLayer>(),
            false,
            policy);
        TokenBytesResult invalidModel = CborWebToken.Create(
            claims,
            invalid,
            false,
            policy);
        TokenBytesResult invalidKind = CborWebToken.Create(
            claims,
            unknown,
            false,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(missingAlgorithm, "missing_cwt_algorithm");
            AssertFailure(invalidAlgorithm, "invalid_cwt_algorithm");
            AssertFailure(noLayers, "missing_cwt_protection");
            AssertFailure(invalidModel, "invalid_cwt_protection_keys");
            AssertFailure(invalidKind, "invalid_cwt_protection_kind");
        });

        noAlgorithm.ReleaseOwnedMaterial();
        unsupported.ReleaseOwnedMaterial();
    }

    [Test]
    public void CreationAppliesPolicyLimitsAndConfirmationValidation()
    {
        TokenKey key = Symmetric(32, CoseAlgorithms.HmacSha256);
        CwtClaims claims = Claims();
        CwtClaims invalidConfirmation = Claims();
        invalidConfirmation.Set(
            CwtClaimLabels.Confirmation,
            Text("not-a-map"));
        TokenPolicy small = Policy("HMAC 256/256");
        small.MaximumTokenSizeInBytes = 10;
        TokenPolicy regular = Policy("HMAC 256/256");

        TokenBytesResult limited = CborWebToken.Create(claims, key, small);
        TokenBytesResult invalid = CborWebToken.Create(
            invalidConfirmation,
            key,
            regular);

        Assert.Multiple(() =>
        {
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Error.Code,
                Is.AnyOf("buffer_limit_exceeded", "cbor_size_exceeded"));
            AssertFailure(invalid, "confirmation_not_map");
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void NestedCreationRejectsAConflictingOuterContentType()
    {
        TokenKey innerKey = Symmetric(32, CoseAlgorithms.HmacSha256);
        TokenKey outerKey = Symmetric(16, CoseAlgorithms.Aes128Gcm);
        CwtProtectionLayer inner = CwtProtectionLayer.CreateMac0(
            AlgorithmHeader(CoseAlgorithms.HmacSha256),
            innerKey);
        CoseHeader outerHeader = AlgorithmHeader(CoseAlgorithms.Aes128Gcm);
        outerHeader.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("application/example"));
        CwtProtectionLayer outer = CwtProtectionLayer.CreateEncrypt0(
            outerHeader,
            outerKey);

        TokenBytesResult result = CborWebToken.CreateNested(
            Claims(),
            new CwtProtectionLayer[] { inner, outer },
            false,
            Policy("HMAC 256/256", "A128GCM"));

        AssertFailure(result, "invalid_nested_cwt_content_type");
        innerKey.ReleaseOwnedMaterial();
        outerKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void SymmetricConfirmationKeyRequiresAnEncryptedLayer()
    {
        CwtClaims claims = Claims();
        TokenKey confirmation = Symmetric(32, CoseAlgorithms.HmacSha256);
        TokenResult confirmationSet =
            CwtConfirmation.SetProofOfPossessionKey(claims, confirmation);
        TokenKey macKey = Symmetric(32, CoseAlgorithms.HmacSha256);
        TokenKey encryptionKey = Symmetric(16, CoseAlgorithms.Aes128Gcm);
        CwtProtectionLayer mac = CwtProtectionLayer.CreateMac0(
            AlgorithmHeader(CoseAlgorithms.HmacSha256),
            macKey);
        CwtProtectionLayer encryption = CwtProtectionLayer.CreateEncrypt0(
            AlgorithmHeader(CoseAlgorithms.Aes128Gcm),
            encryptionKey);

        TokenBytesResult unencrypted = CborWebToken.Create(
            claims,
            mac,
            false,
            Policy("HMAC 256/256"));
        TokenBytesResult encrypted = CborWebToken.Create(
            claims,
            encryption,
            false,
            Policy("A128GCM"));

        Assert.Multiple(() =>
        {
            Assert.That(confirmationSet.Success, Is.True);
            AssertFailure(
                unencrypted,
                "symmetric_confirmation_key_not_encrypted");
            AssertSuccess(encrypted);
        });

        confirmation.ReleaseOwnedMaterial();
        macKey.ReleaseOwnedMaterial();
        encryptionKey.ReleaseOwnedMaterial();
    }

    static CwtClaims Claims()
    {
        CwtClaims claims = new CwtClaims();
        claims.Set(CwtClaimLabels.Issuer, Text("issuer"));
        claims.Set(CwtClaimLabels.Subject, Text("subject"));
        return claims;
    }

    static CoseHeader AlgorithmHeader(int algorithm)
    {
        CoseHeader header = new CoseHeader();
        header.SetProtected(CoseHeaderLabels.Algorithm, Int(algorithm));
        return header;
    }

    static CoseHeader RecipientHeader(int algorithm)
    {
        CoseHeader header = new CoseHeader();
        header.SetUnprotected(CoseHeaderLabels.Algorithm, Int(algorithm));
        return header;
    }

    static TokenKey Symmetric(int size, int algorithm)
    {
        byte[] bytes = new byte[size];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(index + 1);
            index += 1;
        }

        return SymmetricKeyImporterOperations.ImportCose(bytes, algorithm).Key;
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static CborItem Decode(byte[] bytes)
    {
        return CborReader.Create(bytes).Read().Item;
    }

    static byte[] Encode(CborItem item)
    {
        CborWriter writer = CborWriter.Create();
        TokenResult result = writer.WriteDeterministicItem(item);
        Assert.That(result.Success, Is.True);
        return writer.ToArray();
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

    static CborItem Text(string text)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = text;
        return item;
    }

    static void AssertSuccess(TokenBytesResult result)
    {
        Assert.That(result.Success, Is.True,
            result.Error == null ? "no error" : result.Error.Code);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Bytes, Is.Not.Null);
    }

    static void AssertFailure(TokenBytesResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Bytes, Is.Null);
    }
}
