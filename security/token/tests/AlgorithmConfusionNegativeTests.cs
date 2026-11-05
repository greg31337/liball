using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class AlgorithmConfusionNegativeTests
{
    [Test]
    public void JwtValidationDoesNotTreatRsaMaterialAsAnHmacSecret()
    {
        using RSA privateRsa = RSA.Create(2048);
        using RSA publicRsa = RSA.Create();
        RSAParameters publicParameters = privateRsa.ExportParameters(false);
        publicRsa.ImportParameters(publicParameters);
        TokenKey rsaPublic = RsaKeyImporterOperations.ImportJose(
            publicRsa,
            JoseAlgorithms.RsaSha256,
            false).Key;
        TokenKey attackerHmac = SymmetricKeyImporterOperations.ImportJose(
            publicParameters.Modulus,
            JoseAlgorithms.HmacSha256).Key;
        TokenPolicy tokenPolicy = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult malicious = JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes("{\"sub\":\"attacker\"}"),
            Header("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"),
            attackerHmac,
            tokenPolicy);

        JwtValidationResult result = JwtValidator.ValidateSigned(
            malicious.Text,
            Resolver(rsaPublic),
            JwtValidationPolicy.Create(tokenPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(malicious.Success, Is.True);
            AssertJwtFailure(result, "invalid_key_type");
        });

        attackerHmac.ReleaseOwnedMaterial();
    }

    [Test]
    public void JwtValidationRejectsSymmetricKeysForRsaAndEcAlgorithms()
    {
        using RSA rsa = RSA.Create(2048);
        using ECDsa ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey rsaPrivate = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha256,
            true).Key;
        TokenKey ecPrivate = EcdsaKeyImporter.ImportJose(
            ec,
            JoseAlgorithms.EcdsaSha256,
            true).Key;
        TokenKey symmetric = SymmetricKeyImporterOperations.Import(
            Sequence(32, 80)).Key;
        TokenPolicy rsaPolicy = Policy(JoseAlgorithms.RsaSha256);
        TokenPolicy ecPolicy = Policy(JoseAlgorithms.EcdsaSha256);
        byte[] claims = Encoding.UTF8.GetBytes("{\"sub\":\"subject\"}");
        TokenTextResult rsaToken = JsonWebSignature.Sign(
            claims,
            Header("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"),
            rsaPrivate,
            rsaPolicy);
        TokenTextResult ecToken = JsonWebSignature.Sign(
            claims,
            Header("{\"alg\":\"ES256\",\"typ\":\"JWT\"}"),
            ecPrivate,
            ecPolicy);

        JwtValidationResult rsaResult = JwtValidator.ValidateSigned(
            rsaToken.Text,
            Resolver(symmetric),
            JwtValidationPolicy.Create(rsaPolicy));
        JwtValidationResult ecResult = JwtValidator.ValidateSigned(
            ecToken.Text,
            Resolver(symmetric),
            JwtValidationPolicy.Create(ecPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(rsaToken.Success, Is.True);
            Assert.That(ecToken.Success, Is.True);
            AssertJwtFailure(rsaResult, "invalid_key_type");
            AssertJwtFailure(ecResult, "invalid_key_type");
        });

        symmetric.ReleaseOwnedMaterial();
    }

    [Test]
    public void JwtValidationRejectsConflictingKeyAlgorithmMetadata()
    {
        TokenKey signingKey = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32, 1),
            JoseAlgorithms.HmacSha256).Key;
        TokenKey conflictingKey = SymmetricKeyImporterOperations.ImportJose(
            Sequence(48, 40),
            JoseAlgorithms.HmacSha384).Key;
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult token = JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes("{\"sub\":\"subject\"}"),
            Header("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"),
            signingKey,
            policy);

        JwtValidationResult result = JwtValidator.ValidateSigned(
            token.Text,
            Resolver(conflictingKey),
            JwtValidationPolicy.Create(policy));

        Assert.Multiple(() =>
        {
            Assert.That(token.Success, Is.True);
            AssertJwtFailure(result, "conflicting_key_algorithm");
        });

        signingKey.ReleaseOwnedMaterial();
        conflictingKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void CwtValidationRejectsKeysFromTheWrongAlgorithmFamily()
    {
        using ECDsa ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey symmetric = SymmetricKeyImporterOperations.ImportCose(
            Sequence(32, 1),
            CoseAlgorithms.HmacSha256).Key;
        TokenKey ecPrivate = EcdsaKeyImporter.ImportCose(
            ec,
            CoseAlgorithms.EcdsaP256Sha256,
            true).Key;
        TokenPolicy tokenPolicy = Policy("HMAC 256/256", "ESP256");
        CwtClaims claims = new CwtClaims();
        claims.Set(CwtClaimLabels.Subject, Text("subject"));
        TokenBytesResult maced = CborWebToken.Create(
            claims,
            symmetric,
            tokenPolicy);
        TokenBytesResult signed = CborWebToken.Create(
            claims,
            ecPrivate,
            tokenPolicy);
        CwtValidationPolicy validationPolicy = CwtValidationPolicy.Create(
            tokenPolicy);

        CwtValidationResult macResult = CborWebToken.Validate(
            maced.Bytes,
            Resolver(ecPrivate),
            validationPolicy);
        CwtValidationResult signatureResult = CborWebToken.Validate(
            signed.Bytes,
            Resolver(symmetric),
            validationPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(maced.Success, Is.True);
            Assert.That(signed.Success, Is.True);
            AssertCwtFailure(macResult, "invalid_key_type");
            AssertCwtFailure(signatureResult, "invalid_key_type");
        });

        symmetric.ReleaseOwnedMaterial();
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static JoseHeader Header(string json)
    {
        JoseHeaderResult result = JoseHeader.Read(json);
        Assert.That(result.Success, Is.True);
        return result.Header;
    }

    static byte[] Sequence(int length, int start)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(start + index);
            index += 1;
        }

        return bytes;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static TokenKeyResolver Resolver(TokenKey key)
    {
        return new TokenKeyResolver(context => KeySuccess(key));
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
        return result;
    }

    static void AssertJwtFailure(JwtValidationResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Claims, Is.Null);
        Assert.That(result.SignatureValidated, Is.False);
        Assert.That(result.EncryptionValidated, Is.False);
        AssertError(result.Error, code);
    }

    static void AssertCwtFailure(CwtValidationResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Claims, Is.Null);
        Assert.That(result.Headers, Is.Empty);
        AssertError(result.Error, code);
    }

    static void AssertError(TokenError error, string code)
    {
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Code, Is.EqualTo(code));
    }
}
