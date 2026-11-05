using System.Text;
using token;

namespace token.Tests;

public class Rfc7520InteropTests
{
    const string SignaturePayload =
        "It\u2019s a dangerous business, Frodo, going out your door. You step "
        + "onto the road, and if you don't keep your feet, there\u2019s no knowing "
        + "where you might be swept off to.";

    const string EncryptionPlaintext =
        "You can trust us to stick with you through thick and thin\u2013to the "
        + "bitter end. And you can trust us to keep any secret of yours\u2013closer "
        + "than you keep it yourself. But you cannot trust us to let you face "
        + "trouble alone, and go off without a word. We are your friends, Frodo.";

    [Test]
    public void Section44HmacCompactAndJsonSerializationsVerify()
    {
        TokenKey key = ReadKey(
            "{\"kty\":\"oct\",\"kid\":\"018c0ae5-4d9b-471b-bfd6-eef314bc7037\","
            + "\"use\":\"sig\",\"alg\":\"HS256\","
            + "\"k\":\"hJtXIZ2uSN5kbQfbtTNWbpdmhkV8FJG-Onbc6mxCcYg\"}");
        string payload =
            "SXTigJlzIGEgZGFuZ2Vyb3VzIGJ1c2luZXNzLCBGcm9kbywgZ29pbmcgb3V0IH"
            + "lvdXIgZG9vci4gWW91IHN0ZXAgb250byB0aGUgcm9hZCwgYW5kIGlmIHlvdSBk"
            + "b24ndCBrZWVwIHlvdXIgZmVldCwgdGhlcmXigJlzIG5vIGtub3dpbmcgd2hlcm"
            + "UgeW91IG1pZ2h0IGJlIHN3ZXB0IG9mZiB0by4";
        string protectedHeader =
            "eyJhbGciOiJIUzI1NiIsImtpZCI6IjAxOGMwYWU1LTRkOWItNDcxYi1iZmQ2"
            + "LWVlZjMxNGJjNzAzNyJ9";
        string signature = "s0h6KThzkfBBBkLspW1h84VsJZFTsPPqMDA7g1Md7p0";
        string compact = protectedHeader + "." + payload + "." + signature;
        string flattened = "{\"payload\":\"" + payload
            + "\",\"protected\":\"" + protectedHeader
            + "\",\"signature\":\"" + signature + "\"}";
        string general = "{\"payload\":\"" + payload
            + "\",\"signatures\":[{\"protected\":\"" + protectedHeader
            + "\",\"signature\":\"" + signature + "\"}]}";
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);

        JwsCompactResult compactResult = JsonWebSignature.Verify(
            compact,
            key,
            policy);
        JwsFlattenedResult flattenedResult =
            JsonWebSignatureFlattened.Verify(flattened, key, policy);
        JwsGeneralResult generalResult = JsonWebSignatureGeneral.Verify(
            general,
            new TokenKey[] { key },
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(compactResult.Success, Is.True,
                Error(compactResult.Error));
            Assert.That(flattenedResult.Success, Is.True,
                Error(flattenedResult.Error));
            Assert.That(generalResult.Success, Is.True,
                Error(generalResult.Error));
            Assert.That(compactResult.Token.Payload,
                Is.EqualTo(Encoding.UTF8.GetBytes(SignaturePayload)));
            Assert.That(flattenedResult.Token.Payload,
                Is.EqualTo(Encoding.UTF8.GetBytes(SignaturePayload)));
            Assert.That(generalResult.Token.Payload,
                Is.EqualTo(Encoding.UTF8.GetBytes(SignaturePayload)));
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void Section47UnprotectedOnlyJwsVerifies()
    {
        TokenKey key = ReadKey(
            "{\"kty\":\"oct\",\"kid\":\"018c0ae5-4d9b-471b-bfd6-eef314bc7037\","
            + "\"use\":\"sig\",\"alg\":\"HS256\","
            + "\"k\":\"hJtXIZ2uSN5kbQfbtTNWbpdmhkV8FJG-Onbc6mxCcYg\"}");
        string vector =
            "{\"payload\":\"SXTigJlzIGEgZGFuZ2Vyb3VzIGJ1c2luZXNzLCBGcm9kby"
            + "wgZ29pbmcgb3V0IHlvdXIgZG9vci4gWW91IHN0ZXAgb250byB0aGUgcm9hZCwg"
            + "YW5kIGlmIHlvdSBkb24ndCBrZWVwIHlvdXIgZmVldCwgdGhlcmXigJlzIG5vIG"
            + "tub3dpbmcgd2hlcmUgeW91IG1pZ2h0IGJlIHN3ZXB0IG9mZiB0by4\","
            + "\"header\":{\"alg\":\"HS256\","
            + "\"kid\":\"018c0ae5-4d9b-471b-bfd6-eef314bc7037\"},"
            + "\"signature\":\"xuLifqLGiblpv9zBpuZczWhNj1gARaLV3UxvxhJxZuk\"}";

        JwsFlattenedResult result = JsonWebSignatureFlattened.Verify(
            vector,
            key,
            Policy(JoseAlgorithms.HmacSha256));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Error(result.Error));
            Assert.That(result.Token.ProtectedSegment, Is.Empty);
            Assert.That(result.Token.ProtectedHeader.Parameters.Members,
                Is.Empty);
            Assert.That(result.Token.Algorithm,
                Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(result.Token.Payload,
                Is.EqualTo(Encoding.UTF8.GetBytes(SignaturePayload)));
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void Section56DirectAesGcmCompactDecrypts()
    {
        TokenKey key = ReadKey(
            "{\"kty\":\"oct\",\"kid\":\"77c7e2b8-6e13-45cf-8672-617b5b45243a\","
            + "\"use\":\"enc\",\"alg\":\"A128GCM\","
            + "\"k\":\"XctOhJAkA-pD9Lh7ZgW_2A\"}");
        string vector =
            "eyJhbGciOiJkaXIiLCJraWQiOiI3N2M3ZTJiOC02ZTEzLTQ1Y2YtODY3Mi02"
            + "MTdiNWI0NTI0M2EiLCJlbmMiOiJBMTI4R0NNIn0..refa467QzzKx6QAB."
            + "JW_i_f52hww_ELQPGaYyeAB6HYGcR559l9TYnSovc23XJoBcW29rHP8yZOZG7Y"
            + "hLpT1bjFuvZPjQS-m0IFtVcXkZXdH_lr_FrdYt9HRUYkshtrMmIUAyGmUnd9zM"
            + "DB2n0cRDIHAzFVeJUDxkUwVAE7_YGRPdcqMyiBoCO-FBdE-Nceb4h3-FtBP-c_"
            + "BIwCPTjb9o0SbdcdREEMJMyZBH8ySWMVi1gPD9yxi-aQpGbSv_F9N4IZAxscj5"
            + "g-NJsUPbjk29-s7LJAGb15wEBtXphVCgyy53CoIKLHHeJHXex45Uz9aKZSRSIn"
            + "ZI-wjsY0yu3cT4_aQ3i1o-tiE-F8Ios61EKgyIQ4CWao8PFMj8TTnp."
            + "vbb32Xvllea2OtmHAdccRQ";
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);

        JweCompactResult result = JsonWebEncryption.Decrypt(
            vector,
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Error(result.Error));
            Assert.That(result.Token.Plaintext,
                Is.EqualTo(Encoding.UTF8.GetBytes(EncryptionPlaintext)));
            Assert.That(result.Token.Algorithm,
                Is.EqualTo(JoseAlgorithms.Direct));
            Assert.That(result.Token.EncryptionAlgorithm,
                Is.EqualTo(JoseAlgorithms.Aes128Gcm));
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void Section512UnprotectedOnlyJweDecrypts()
    {
        TokenKey key = ReadKey(
            "{\"kty\":\"oct\",\"kid\":\"81b20965-8332-43d9-a468-82160ad91ac8\","
            + "\"use\":\"enc\",\"alg\":\"A128KW\","
            + "\"k\":\"GZy6sIZ6wl9NJOKB-jnmVQ\"}");
        string vector =
            "{\"unprotected\":{\"alg\":\"A128KW\","
            + "\"kid\":\"81b20965-8332-43d9-a468-82160ad91ac8\","
            + "\"enc\":\"A128GCM\"},"
            + "\"encrypted_key\":\"244YHfO_W7RMpQW81UjQrZcq5LSyqiPv\","
            + "\"iv\":\"YihBoVOGsR1l7jCD\","
            + "\"ciphertext\":\"qtPIMMaOBRgASL10dNQhOa7Gqrk7Eal1vwht7R4TT1u"
            + "q-arsVCPaIeFwQfzrSS6oEUWbBtxEasE0vC6r7sphyVziMCVJEuRJyoAHFSP3eq"
            + "QPb4Ic1SDSqyXjw_L3svybhHYUGyQuTmUQEDjgjJfBOifwHIsDsRPeBz1Nomqei"
            + "fVPq5GTCWFo5k_MNIQURR2Wj0AHC2k7JZfu2iWjUHLF8ExFZLZ4nlmsvJu_mvi"
            + "fMYiikfNfsZAudISOa6O73yPZtL04k_1FI7WDfrb2w7OqKLWDXzlpcxohPVOLQw"
            + "pA3mFNRKdY-bQz4Z4KX9lfz1cne31N4-8BKmojpw-OdQjKdLOGkC445Fb_K1tlD"
            + "QXw2sBF\",\"tag\":\"e2m0Vm7JvjK2VpCKXS-kyg\"}";
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);

        JweFlattenedResult result = JsonWebEncryptionFlattened.Decrypt(
            vector,
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Error(result.Error));
            Assert.That(result.Token.ProtectedSegment, Is.Empty);
            Assert.That(result.Token.ProtectedHeader.Parameters.Members,
                Is.Empty);
            Assert.That(result.Token.Plaintext,
                Is.EqualTo(Encoding.UTF8.GetBytes(EncryptionPlaintext)));
            Assert.That(result.Token.Algorithm,
                Is.EqualTo(JoseAlgorithms.Aes128KeyWrap));
            Assert.That(result.Token.EncryptionAlgorithm,
                Is.EqualTo(JoseAlgorithms.Aes128Gcm));
        });

        key.ReleaseOwnedMaterial();
    }

    static TokenKey ReadKey(string json)
    {
        JwkResult result = JsonWebKey.Read(json);
        Assert.That(result.Success, Is.True, Error(result.Error));
        return result.Key;
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy result = new TokenPolicy();
        result.AllowedAlgorithms = algorithms;
        return result;
    }

    static string Error(TokenError error)
    {
        return error == null ? "no error" : error.Code + ": " + error.Message;
    }
}
