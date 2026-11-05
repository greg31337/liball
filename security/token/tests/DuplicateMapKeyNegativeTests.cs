using System.Text;
using token;

namespace token.Tests;

public class DuplicateMapKeyNegativeTests
{
    const string HmacProtectedHeader = "eyJhbGciOiJIUzI1NiJ9";

    [Test]
    public void JoseJsonSerializationsRejectDuplicatesAtEveryDepth()
    {
        JwsFlattenedResult flattenedJws = JsonWebSignatureFlattened.Parse(
            "{\"payload\":\"AA\",\"payload\":\"AQ\",\"protected\":\""
            + HmacProtectedHeader + "\",\"signature\":\"AA\"}");
        JwsGeneralResult generalJws = JsonWebSignatureGeneral.Parse(
            "{\"payload\":\"AA\",\"signatures\":[{\"protected\":\""
            + HmacProtectedHeader
            + "\",\"header\":{\"kid\":\"one\",\"\\u006bid\":\"two\"},"
            + "\"signature\":\"AA\"}]}");
        JweFlattenedResult flattenedJwe = JsonWebEncryptionFlattened.Parse(
            "{\"ciphertext\":\"AA\",\"ciphertext\":\"AQ\"}");
        JweGeneralResult generalJwe = JsonWebEncryptionGeneral.Parse(
            "{\"recipients\":[{\"header\":{\"kid\":\"one\","
            + "\"\\u006bid\":\"two\"}}]}");

        Assert.Multiple(() =>
        {
            Assert.That(flattenedJws.Success, Is.False);
            Assert.That(flattenedJws.Token, Is.Null);
            AssertError(flattenedJws.Error, "duplicate_json_member");
            Assert.That(generalJws.Success, Is.False);
            Assert.That(generalJws.Token, Is.Null);
            AssertError(generalJws.Error, "duplicate_json_member");
            Assert.That(flattenedJwe.Success, Is.False);
            Assert.That(flattenedJwe.Token, Is.Null);
            AssertError(flattenedJwe.Error, "duplicate_json_member");
            Assert.That(generalJwe.Success, Is.False);
            Assert.That(generalJwe.Token, Is.Null);
            AssertError(generalJwe.Error, "duplicate_json_member");
        });
    }

    [Test]
    public void SignedJwtWithDuplicateClaimsIsRejectedBeforeKeyResolution()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32),
            JoseAlgorithms.HmacSha256).Key;
        TokenPolicy tokenPolicy = new TokenPolicy();
        tokenPolicy.AllowedAlgorithms = new[] { JoseAlgorithms.HmacSha256 };
        TokenTextResult signed = JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes(
                "{\"sub\":\"trusted\",\"\\u0073ub\":\"attacker\"}"),
            JoseHeader.Read("{\"alg\":\"HS256\",\"typ\":\"JWT\"}").Header,
            key,
            tokenPolicy);
        int resolverCalls = 0;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            resolverCalls += 1;
            return KeySuccess(key);
        });

        JwtValidationResult result = JwtValidator.ValidateSigned(
            signed.Text,
            resolver,
            JwtValidationPolicy.Create(tokenPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Claims, Is.Null);
            Assert.That(result.SignatureValidated, Is.False);
            AssertError(result.Error, "duplicate_json_member");
            Assert.That(resolverCalls, Is.Zero);
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void CoseMessagesRejectEquivalentDuplicateHeaderLabels()
    {
        byte[] protectedDuplicates = Convert.FromHexString(
            "D28446A20126180126A04040");
        byte[] unprotectedDuplicates = Convert.FromHexString(
            "D28440A201261801264040");

        CoseSign1Result protectedResult = CoseSign1.Read(
            protectedDuplicates);
        CoseSign1Result unprotectedResult = CoseSign1.Read(
            unprotectedDuplicates);

        Assert.Multiple(() =>
        {
            Assert.That(protectedResult.Success, Is.False);
            Assert.That(protectedResult.Message, Is.Null);
            AssertError(protectedResult.Error, "invalid_cose_protected_header");
            Assert.That(unprotectedResult.Success, Is.False);
            Assert.That(unprotectedResult.Message, Is.Null);
            AssertError(unprotectedResult.Error, "duplicate_cbor_map_key");
        });
    }

    [Test]
    public void AuthenticatedCwtWithEquivalentDuplicateClaimsIsRejected()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Sequence(32),
            CoseAlgorithms.HmacSha256).Key;
        TokenPolicy tokenPolicy = new TokenPolicy();
        tokenPolicy.AllowedAlgorithms = new[] { "HMAC 256/256" };
        CoseHeader header = new CoseHeader();
        header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Integer(CoseAlgorithms.HmacSha256));
        byte[] duplicateClaims = Convert.FromHexString(
            "A2016669737375657218016861747461636B6572");
        TokenBytesResult token = CoseMac0Operations.Create(
            duplicateClaims,
            header,
            key,
            tokenPolicy);
        int resolverCalls = 0;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            resolverCalls += 1;
            return KeySuccess(key);
        });

        CwtValidationResult result = CborWebToken.Validate(
            token.Bytes,
            resolver,
            CwtValidationPolicy.Create(tokenPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(token.Success, Is.True);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Claims, Is.Null);
            AssertError(result.Error, "duplicate_cbor_map_key");
            Assert.That(resolverCalls, Is.EqualTo(1));
        });

        key.ReleaseOwnedMaterial();
    }

    static byte[] Sequence(int length)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(index + 1);
            index += 1;
        }

        return bytes;
    }

    static CborItem Integer(int value)
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

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
        return result;
    }

    static void AssertError(TokenError error, string code)
    {
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Code, Is.EqualTo(code));
    }
}
