using System.Security.Cryptography;
using token;

namespace token.Tests;

public class Rfc8392InteropTests
{
    const string Claims =
        "A70175636F61703A2F2F61732E6578616D706C652E636F6D02656572696B77037818636F61703A2F2F6C696768742E6578616D706C652E636F6D041A5612AEB0051A5610D9F0061A5610D9F007420B71";
    const string Symmetric128Key =
        "A42050231F4C4D4D3051FDC2EC0A3851D5B3830104024C53796D6D6574726963313238030A";
    const string Symmetric256Key =
        "A4205820403697DE87AF64611C1D32A05DAB0FE1FCB715A86AB435F1EC99192D795693880104024C53796D6D65747269633235360304";
    const string EcdsaKey =
        "A72358206C1382765AEC5358F117733D281C1C7BDC39884D04A45A1E6C67C858BC206C1922582060F7F1A780D8A783BFB7A2DD6B2796E8128DBBCEF9D3D168DB9529971A36E7B9215820143329CCE7868E416927599CF65A34F3CE2FFDA55A7ECA69ED8919A394D42F0F2001010202524173796D6D657472696345434453413235360326";
    const string SignedCwt =
        "D28443A10126A104524173796D6D657472696345434453413235365850A70175636F61703A2F2F61732E6578616D706C652E636F6D02656572696B77037818636F61703A2F2F6C696768742E6578616D706C652E636F6D041A5612AEB0051A5610D9F0061A5610D9F007420B7158405427C1FF28D23FBAD1F29C4C7C6A555E601D6FA29F9179BC3D7438BACACA5ACD08C8D4D4F96131680C429A01F85951ECEE743A52B9B63632C57209120E1C9E30";
    const string MacedCwt =
        "D83DD18443A10104A1044C53796D6D65747269633235365850A70175636F61703A2F2F61732E6578616D706C652E636F6D02656572696B77037818636F61703A2F2F6C696768742E6578616D706C652E636F6D041A5612AEB0051A5610D9F0061A5610D9F007420B7148093101EF6D789200";
    const string EncryptedCwt =
        "D08343A1010AA2044C53796D6D6574726963313238054D99A0D7846E762C49FFE8A63E0B5858B918A11FD81E438B7F973D9E2E119BCB22424BA0F38A80F27562F400EE1D0D6C0FDB559C02421FD384FC2EBE22D7071378B0EA7428FFF157444D45F7E6AFCDA1AAE5F6495830C58627087FC5B4974F319A8707A635DD643B";
    const string NestedCwt =
        "D08343A1010AA2044C53796D6D6574726963313238054D4A0694C0E69EE6B5956655C7B258B7F6B0914F993DE822CC47E5E57A188D7960B528A747446FE12F0E7DE05650DEC74724366763F167A29C002DFD15B34D8993391CF49BC91127F545DBA8703D66F5B7F1AE91237503D371E6333DF9708D78C4FB8A8386C8FF09DC49AF768B23179DEAB78D96490A66D5724FB33900C60799D9872FAC6DA3BDB89043D67C2A05414CE331B5B8F1ED8FF7138F45905DB2C4D5BC8045AB372BFF142631610A7E0F677B7E9B0BC73ADEFDCEE16D9D5D284C616ABEAB5D8C291CE0";
    const string FloatingPointCwt =
        "D18443A10104A1044C53796D6D65747269633235364BA106FB41D584367C20000048B8816F34C0542892";

    [Test]
    public void AppendixAKeysAndClaimsParseExactly()
    {
        CwtClaimsResult claims = CwtClaims.Read(
            Convert.FromHexString(Claims));
        CoseKeyResult key128 = CoseKey.Read(
            Convert.FromHexString(Symmetric128Key));
        CoseKeyResult key256 = CoseKey.Read(
            Convert.FromHexString(Symmetric256Key));
        CoseKeyResult conservative = CoseKey.Read(
            Convert.FromHexString(EcdsaKey));
        CoseKeyResult ecdsa = CoseKey.Read(
            Convert.FromHexString(EcdsaKey),
            true);

        Assert.Multiple(() =>
        {
            Assert.That(claims.Success, Is.True);
            Assert.That(claims.Claims.Write().Bytes,
                Is.EqualTo(Convert.FromHexString(Claims)));
            Assert.That(key128.Success, Is.True);
            Assert.That(key128.Key.SymmetricKey,
                Is.EqualTo(Convert.FromHexString(
                    "231F4C4D4D3051FDC2EC0A3851D5B383")));
            Assert.That(key256.Success, Is.True);
            Assert.That(key256.Key.SymmetricKey,
                Is.EqualTo(Convert.FromHexString(
                    "403697DE87AF64611C1D32A05DAB0FE1FCB715A86AB435F1EC99192D79569388")));
            Assert.That(conservative.Success, Is.False);
            Assert.That(conservative.Error.Code,
                Is.EqualTo("compatibility_required"));
            Assert.That(ecdsa.Success, Is.True,
                Error(ecdsa.Error));
            Assert.That(ecdsa.Key.CoseCurve,
                Is.EqualTo(CoseKeyLabels.CurveP256));
        });

        key128.Key.ReleaseOwnedMaterial();
        key256.Key.ReleaseOwnedMaterial();
        ecdsa.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void AppendixA3SignedCwtValidatesWithCompatibilityOptIn()
    {
        CoseKeyResult key = CoseKey.Read(
            Convert.FromHexString(EcdsaKey),
            true);
        CwtValidationPolicy policy = Policy(
            CoseAlgorithmsOperations.GetName(CoseAlgorithms.EcdsaSha256));
        CwtValidationResult conservative = CborWebToken.Validate(
            Convert.FromHexString(SignedCwt),
            Resolver(key.Key),
            policy);
        policy.AllowCompatibilitySignatures = true;
        CwtValidationResult compatible = CborWebToken.Validate(
            Convert.FromHexString(SignedCwt),
            Resolver(key.Key),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(conservative.Success, Is.False);
            Assert.That(conservative.Error.Code,
                Is.EqualTo("compatibility_required"));
            AssertValidated(compatible, CoseMessageKind.Sign1, false);
        });

        key.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void AppendixA4MacedCwtValidatesAndRecreatesExactly()
    {
        TokenKey key = CoseKey.Read(
            Convert.FromHexString(Symmetric256Key)).Key;
        CwtValidationPolicy policy = Policy(
            CoseAlgorithmsOperations.GetName(
                CoseAlgorithms.HmacSha256Truncated64));
        CwtValidationResult validated = CborWebToken.Validate(
            Convert.FromHexString(MacedCwt),
            Resolver(key),
            policy);
        CwtClaims claims = CwtClaims.Read(
            Convert.FromHexString(Claims)).Claims;
        CoseHeader header = Header(
            CoseAlgorithms.HmacSha256Truncated64,
            "Symmetric256");
        TokenBytesResult recreated = CborWebToken.Create(
            claims,
            CwtProtectionLayer.CreateMac0(header, key),
            true,
            policy.TokenPolicy);

        Assert.Multiple(() =>
        {
            AssertValidated(validated, CoseMessageKind.Mac0, true);
            Assert.That(recreated.Success, Is.True,
                Error(recreated.Error));
            Assert.That(recreated.Bytes,
                Is.EqualTo(Convert.FromHexString(MacedCwt)));
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void AppendixA5EncryptedCwtDecryptsAndRecreatesExactly()
    {
        RequireAesCcm();
        TokenKey key = CoseKey.Read(
            Convert.FromHexString(Symmetric128Key)).Key;
        CwtValidationPolicy policy = Policy(
            CoseAlgorithmsOperations.GetName(CoseAlgorithms.AesCcm16_64_128));
        CwtValidationResult validated = CborWebToken.Validate(
            Convert.FromHexString(EncryptedCwt),
            Resolver(key),
            policy);
        CoseHeader header = Header(
            CoseAlgorithms.AesCcm16_64_128,
            "Symmetric128");
        header.SetUnprotected(
            CoseHeaderLabels.InitializationVector,
            Bytes(Convert.FromHexString(
                "99A0D7846E762C49FFE8A63E0B")));
        TokenBytesResult recreated = CborWebToken.Create(
            CwtClaims.Read(Convert.FromHexString(Claims)).Claims,
            CwtProtectionLayer.CreateEncrypt0(header, key),
            false,
            policy.TokenPolicy);

        Assert.Multiple(() =>
        {
            AssertValidated(validated, CoseMessageKind.Encrypt0, false);
            Assert.That(recreated.Success, Is.True,
                Error(recreated.Error));
            Assert.That(recreated.Bytes,
                Is.EqualTo(Convert.FromHexString(EncryptedCwt)));
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void AppendixA6NestedCwtDecryptsAndVerifies()
    {
        RequireAesCcm();
        TokenKey encryption = CoseKey.Read(
            Convert.FromHexString(Symmetric128Key)).Key;
        TokenKey signing = CoseKey.Read(
            Convert.FromHexString(EcdsaKey),
            true).Key;
        CwtValidationPolicy policy = Policy(
            CoseAlgorithmsOperations.GetName(CoseAlgorithms.AesCcm16_64_128),
            CoseAlgorithmsOperations.GetName(CoseAlgorithms.EcdsaSha256));
        policy.AllowCompatibilitySignatures = true;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
            context.Operation == TokenKeyOperations.Decrypt
                ? KeySuccess(encryption)
                : KeySuccess(signing));

        CwtValidationResult result = CborWebToken.Validate(
            Convert.FromHexString(NestedCwt),
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True,
                Error(result.Error));
            Assert.That(result.Nested, Is.True);
            Assert.That(result.ProtectionKinds, Is.EqualTo(new[]
            {
                CoseMessageKind.Encrypt0,
                CoseMessageKind.Sign1
            }));
            AssertClaims(result.Claims);
        });

        encryption.ReleaseOwnedMaterial();
        signing.ReleaseOwnedMaterial();
    }

    [Test]
    public void AppendixA7FloatingPointIssuedAtCwtValidates()
    {
        TokenKey key = CoseKey.Read(
            Convert.FromHexString(Symmetric256Key)).Key;
        CwtValidationPolicy policy = Policy(
            CoseAlgorithmsOperations.GetName(
                CoseAlgorithms.HmacSha256Truncated64));

        CwtValidationResult result = CborWebToken.Validate(
            Convert.FromHexString(FloatingPointCwt),
            Resolver(key),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True,
                Error(result.Error));
            Assert.That(result.Claims.Get(CwtClaimLabels.IssuedAt)
                .FloatingPointValue,
                Is.EqualTo(1443944944.5));
        });

        key.ReleaseOwnedMaterial();
    }

    static CwtValidationPolicy Policy(params string[] algorithms)
    {
        CwtValidationPolicy policy = new CwtValidationPolicy();
        policy.TokenPolicy.AllowedAlgorithms = algorithms;
        policy.TokenPolicy.ClockSkewSeconds = 0;
        policy.Clock = new TokenClock(() =>
            DateTimeOffset.FromUnixTimeSeconds(1444000000));
        return policy;
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

    static CoseHeader Header(int algorithm, string keyId)
    {
        CoseHeader header = new CoseHeader();
        header.SetProtected(CoseHeaderLabels.Algorithm, Int(algorithm));
        header.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(System.Text.Encoding.ASCII.GetBytes(keyId)));
        return header;
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

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static void AssertValidated(
        CwtValidationResult result,
        CoseMessageKind kind,
        bool cwtTagged)
    {
        Assert.That(result.Success, Is.True, Error(result.Error));
        Assert.That(result.CwtTagged, Is.EqualTo(cwtTagged));
        Assert.That(result.ProtectionKinds, Is.EqualTo(new[] { kind }));
        AssertClaims(result.Claims);
    }

    static void AssertClaims(CwtClaims claims)
    {
        Assert.That(claims.Get(CwtClaimLabels.Issuer).Text,
            Is.EqualTo("coap://as.example.com"));
        Assert.That(claims.Get(CwtClaimLabels.Subject).Text,
            Is.EqualTo("erikw"));
        Assert.That(claims.Get(CwtClaimLabels.Audience).Text,
            Is.EqualTo("coap://light.example.com"));
        Assert.That(claims.Get(CwtClaimLabels.CwtId).Bytes,
            Is.EqualTo(new byte[] { 0x0B, 0x71 }));
    }

    static string Error(TokenError error)
    {
        return error == null
            ? "no error"
            : error.Code + ": " + error.Message;
    }

    static void RequireAesCcm()
    {
        if (!AesCcm.IsSupported)
        {
            Assert.Ignore("AES-CCM is unavailable on this platform.");
        }
    }
}
