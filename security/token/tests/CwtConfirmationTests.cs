using System.Security.Cryptography;
using token;

namespace token.Tests;

public class CwtConfirmationTests
{
    [Test]
    public void PublicCoseKeyAndKeyIdRoundTripThroughClaims()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey publicKey = EcdsaKeyImporter.Import(ecdsa, false).Key;
        publicKey.CoseAlgorithm = CoseAlgorithms.EcdsaP256Sha256;
        publicKey.HasCoseAlgorithm = true;
        byte[] keyId = new byte[] { 1, 2, 3, 4 };
        CwtClaims claims = new CwtClaims();

        TokenResult keySet = CwtConfirmation.SetProofOfPossessionKey(
            claims,
            publicKey);
        TokenResult idSet = CwtConfirmation.SetKeyId(claims, keyId);
        keyId[0] = 99;
        TokenBytesResult written = claims.Write();
        CwtClaimsResult reparsed = CwtClaims.Read(written.Bytes);
        TokenResult valid = CwtConfirmation.Validate(reparsed.Claims);
        TokenKeyResult key = CwtConfirmation.GetProofOfPossessionKey(
            reparsed.Claims);
        TokenBytesResult id = CwtConfirmation.GetKeyId(reparsed.Claims);

        ECParameters expected = ecdsa.ExportParameters(false);
        ECParameters actual = key.Key.Ecdsa.ExportParameters(false);
        Assert.Multiple(() =>
        {
            AssertSuccess(keySet);
            AssertSuccess(idSet);
            Assert.That(written.Success, Is.True);
            Assert.That(valid.Success, Is.True);
            Assert.That(key.Success, Is.True);
            Assert.That(key.Key.ContainsPrivateMaterial(), Is.False);
            Assert.That(key.Key.CoseAlgorithm,
                Is.EqualTo(CoseAlgorithms.EcdsaP256Sha256));
            Assert.That(actual.Q.X, Is.EqualTo(expected.Q.X));
            Assert.That(actual.Q.Y, Is.EqualTo(expected.Q.Y));
            Assert.That(id.Success, Is.True);
            Assert.That(id.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        });

        key.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void SymmetricKeyCanBeHandledForAnEncryptedContainingCwt()
    {
        byte[] secret = Enumerable.Repeat((byte)7, 32).ToArray();
        TokenKey symmetric = SymmetricKeyImporterOperations.Import(secret).Key;
        CwtClaims claims = new CwtClaims();

        TokenResult set = CwtConfirmation.SetProofOfPossessionKey(
            claims,
            symmetric);
        TokenKeyResult extracted = CwtConfirmation.GetProofOfPossessionKey(
            claims);

        Assert.Multiple(() =>
        {
            AssertSuccess(set);
            Assert.That(extracted.Success, Is.True);
            Assert.That(extracted.Key.SymmetricKey, Is.EqualTo(secret));
        });

        symmetric.ReleaseOwnedMaterial();
        extracted.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void DirectAsymmetricMethodRejectsPrivateKeyMaterial()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey privateKey = EcdsaKeyImporter.Import(ecdsa, true).Key;
        CwtClaims claims = new CwtClaims();

        TokenResult result = CwtConfirmation.SetProofOfPossessionKey(
            claims,
            privateKey);

        Assert.Multiple(() =>
        {
            AssertFailure(result, "confirmation_key_not_public");
            Assert.That(claims.Get(CwtClaimLabels.Confirmation), Is.Null);
        });
    }

    [Test]
    public void EncryptedCoseKeyRoundTripsAndReplacesDirectKeyMethod()
    {
        CoseEncrypt0 message = new CoseEncrypt0();
        message.Ciphertext = new byte[] { 4, 5, 6 };
        TokenBytesResult encrypted = message.Write();
        CwtClaims claims = new CwtClaims();

        TokenResult setEncrypted = CwtConfirmation.SetEncryptedKey(
            claims,
            encrypted.Bytes);
        TokenBytesResult extracted = CwtConfirmation.GetEncryptedKey(claims);
        TokenKey symmetric = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        TokenResult setDirect = CwtConfirmation.SetProofOfPossessionKey(
            claims,
            symmetric);
        TokenResult valid = CwtConfirmation.Validate(claims);
        CborItem confirmation = claims.Get(CwtClaimLabels.Confirmation);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            AssertSuccess(setEncrypted);
            Assert.That(extracted.Success, Is.True);
            Assert.That(extracted.Bytes, Is.EqualTo(encrypted.Bytes));
            AssertSuccess(setDirect);
            AssertSuccess(valid);
            Assert.That(Find(
                confirmation,
                CwtConfirmationMethodLabels.CoseKey), Is.Not.Null);
            Assert.That(Find(
                confirmation,
                CwtConfirmationMethodLabels.EncryptedCoseKey), Is.Null);
        });

        symmetric.ReleaseOwnedMaterial();
    }

    [Test]
    public void SettersPreserveUnknownConfirmationMethods()
    {
        CwtClaims claims = new CwtClaims();
        claims.Set(CwtClaimLabels.Confirmation, Map(
            new CborMapEntry(Text("example"), Boolean(true))));

        TokenResult set = CwtConfirmation.SetKeyId(
            claims,
            new byte[] { 8, 9 });
        TokenResult valid = CwtConfirmation.Validate(claims);
        CborItem confirmation = claims.Get(CwtClaimLabels.Confirmation);

        Assert.Multiple(() =>
        {
            AssertSuccess(set);
            AssertSuccess(valid);
            Assert.That(confirmation.MapValue, Has.Length.EqualTo(2));
            Assert.That(confirmation.MapValue.Single(
                entry => entry.Key.Kind == TokenCborValueKind.TextString)
                .Value.BooleanValue, Is.True);
            Assert.That(CwtConfirmationMethodLabelsOperations.GetName(
                CwtConfirmationMethodLabels.CoseKeyThumbprint),
                Is.EqualTo("ckt"));
        });
    }

    [Test]
    public void ValidationRejectsWrongRegisteredValueTypesAndTwoKeys()
    {
        CwtClaims wrongRoot = new CwtClaims();
        wrongRoot.Set(CwtClaimLabels.Confirmation, Text("not-a-map"));
        CwtClaims wrongId = ClaimsWithMethod(
            CwtConfirmationMethodLabels.KeyId,
            Text("not-bytes"));
        CwtClaims invalidKey = ClaimsWithMethod(
            CwtConfirmationMethodLabels.CoseKey,
            Map(new CborMapEntry(Unsigned(1), Unsigned(2))));

        CoseEncrypt0 encryptedMessage = new CoseEncrypt0();
        encryptedMessage.Ciphertext = new byte[] { 1 };
        CborItem encrypted = Decode(encryptedMessage.Write().Bytes);
        TokenKey symmetric = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        CwtClaims direct = new CwtClaims();
        CwtConfirmation.SetProofOfPossessionKey(direct, symmetric);
        CborItem directKey = Find(
            direct.Get(CwtClaimLabels.Confirmation),
            CwtConfirmationMethodLabels.CoseKey);
        CwtClaims twoKeys = new CwtClaims();
        twoKeys.Set(CwtClaimLabels.Confirmation, Map(
            new CborMapEntry(
                Unsigned(CwtConfirmationMethodLabels.CoseKey),
                directKey),
            new CborMapEntry(
                Unsigned(CwtConfirmationMethodLabels.EncryptedCoseKey),
                encrypted)));

        TokenResult rootResult = CwtConfirmation.Validate(wrongRoot);
        TokenResult idResult = CwtConfirmation.Validate(wrongId);
        TokenResult keyResult = CwtConfirmation.Validate(invalidKey);
        TokenResult twoResult = CwtConfirmation.Validate(twoKeys);

        Assert.Multiple(() =>
        {
            AssertFailure(rootResult, "confirmation_not_map");
            AssertFailure(idResult, "invalid_confirmation_key_id");
            AssertFailure(keyResult, "invalid_confirmation_key");
            AssertFailure(twoResult, "multiple_confirmation_keys");
        });

        symmetric.ReleaseOwnedMaterial();
    }

    [Test]
    public void MissingAndMalformedEncryptedMethodsReturnStableFailures()
    {
        CwtClaims absent = new CwtClaims();
        CwtClaims onlyId = new CwtClaims();
        CwtConfirmation.SetKeyId(onlyId, new byte[] { 1 });

        TokenKeyResult missingClaim =
            CwtConfirmation.GetProofOfPossessionKey(absent);
        TokenKeyResult missingKey =
            CwtConfirmation.GetProofOfPossessionKey(onlyId);
        TokenResult malformed = CwtConfirmation.SetEncryptedKey(
            onlyId,
            Convert.FromHexString("8101"));

        Assert.Multiple(() =>
        {
            AssertKeyFailure(missingClaim, "missing_confirmation_claim");
            AssertKeyFailure(missingKey, "missing_confirmation_key");
            AssertFailure(malformed, "invalid_encrypted_confirmation_key");
        });
    }

    static CwtClaims ClaimsWithMethod(int label, CborItem value)
    {
        CwtClaims claims = new CwtClaims();
        claims.Set(CwtClaimLabels.Confirmation, Map(
            new CborMapEntry(Unsigned(label), value)));
        return claims;
    }

    static CborItem Find(CborItem map, int label)
    {
        return map.MapValue
            .Where(entry => entry.Key.Kind == TokenCborValueKind.UnsignedInteger)
            .Where(entry => entry.Key.IntegerArgument == (ulong)label)
            .Select(entry => entry.Value)
            .SingleOrDefault();
    }

    static CborItem Decode(byte[] encoded)
    {
        return CborReader.Create(encoded).Read().Item;
    }

    static CborItem Unsigned(int value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.UnsignedInteger;
        item.IntegerArgument = (ulong)value;
        return item;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem Boolean(bool value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Boolean;
        item.BooleanValue = value;
        return item;
    }

    static CborItem Map(params CborMapEntry[] entries)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = entries;
        return item;
    }

    static void AssertSuccess(TokenResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
    }

    static void AssertFailure(TokenResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }

    static void AssertKeyFailure(TokenKeyResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Key, Is.Null);
    }
}
