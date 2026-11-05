using System.Text;
using token;

namespace token.Tests;

public class CompactTokenNegativeTests
{
    [TestCase("")]
    [TestCase("one")]
    [TestCase("one.two")]
    [TestCase("one.two.three.four")]
    [TestCase("one.two.three.four.five.six")]
    public void PublicParsersRejectUnsupportedSegmentCounts(string text)
    {
        JwsCompactResult jws = JsonWebSignature.Parse(text);
        JweCompactResult jwe = JsonWebEncryption.Parse(text);

        Assert.Multiple(() =>
        {
            AssertJwsFailure(jws, "invalid_compact_token");
            AssertJweFailure(jwe, "invalid_compact_token");
        });
    }

    [Test]
    public void RepresentationSpecificParsersRejectTheOtherCompactShape()
    {
        string threeSegments = "e30.AA.AA";
        string fiveSegments = "e30..AA.AA.AA";

        JweCompactResult jwe = JsonWebEncryption.Parse(threeSegments);
        JwsCompactResult jws = JsonWebSignature.Parse(fiveSegments);

        Assert.Multiple(() =>
        {
            AssertJweFailure(jwe, "invalid_jwe_compact");
            AssertJwsFailure(jws, "invalid_jws_compact");
        });
    }

    [Test]
    public void JwsRejectsInvalidBase64UrlInEverySegment()
    {
        string header = EncodedHeader("{\"alg\":\"HS256\"}");
        string[] invalid = new[]
        {
            "e31.AA.AA",
            header + ".AB.AA",
            header + ".AA.AB",
            "e30=.AA.AA",
            header + ".AA=.AA",
            header + ".AA.AA="
        };

        int index = 0;
        while (index < invalid.Length)
        {
            AssertJwsFailure(
                JsonWebSignature.Parse(invalid[index]),
                "invalid_compact_token");
            index += 1;
        }
    }

    [Test]
    public void JweRejectsInvalidBase64UrlInEverySegment()
    {
        string header = EncodedHeader(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\"}");
        string[] nonCanonical = new[]
        {
            "e31..AA.AA.AA",
            header + ".AB.AA.AA.AA",
            header + "..AB.AA.AA",
            header + "..AA.AB.AA",
            header + "..AA.AA.AB"
        };
        string[] padded = new[]
        {
            "e30=..AA.AA.AA",
            header + ".AA=.AA.AA.AA",
            header + "..AA=.AA.AA",
            header + "..AA.AA=.AA",
            header + "..AA.AA.AA="
        };

        AssertInvalidJweTokens(nonCanonical);
        AssertInvalidJweTokens(padded);
    }

    [Test]
    public void JwtValidationRejectsMalformedCompactInputBeforeKeyResolution()
    {
        string jwsHeader = EncodedHeader(
            "{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
        string jweHeader = EncodedHeader(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\","
            + "\"typ\":\"JWT\"}");
        string[] invalid = new[]
        {
            "one.two",
            jwsHeader + ".AB.AA",
            jwsHeader + ".AA.AA=",
            jweHeader + "..AB.AA.AA",
            jweHeader + "..AA.AA.AA="
        };
        int resolverCalls = 0;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            resolverCalls += 1;
            TokenKeyResult unused = new TokenKeyResult();
            unused.Error = new TokenError("unused", "unused", -1);
            return unused;
        });
        TokenPolicy tokenPolicy = new TokenPolicy();
        tokenPolicy.AllowedAlgorithms = new[]
        {
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm
        };
        JwtValidationPolicy policy = JwtValidationPolicy.Create(tokenPolicy);

        int index = 0;
        while (index < invalid.Length)
        {
            JwtValidationResult result = JwtValidator.Validate(
                invalid[index],
                resolver,
                policy);
            AssertJwtFailure(result, "invalid_compact_token");
            index += 1;
        }

        Assert.That(resolverCalls, Is.Zero);
    }

    static string EncodedHeader(string json)
    {
        return Base64Url.Encode(Encoding.UTF8.GetBytes(json)).Text;
    }

    static void AssertInvalidJweTokens(string[] tokens)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            AssertJweFailure(
                JsonWebEncryption.Parse(tokens[index]),
                "invalid_compact_token");
            index += 1;
        }
    }

    static void AssertJwsFailure(JwsCompactResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Token, Is.Null);
        AssertError(result.Error, code);
    }

    static void AssertJweFailure(JweCompactResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Token, Is.Null);
        AssertError(result.Error, code);
    }

    static void AssertJwtFailure(JwtValidationResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Claims, Is.Null);
        Assert.That(result.SignatureValidated, Is.False);
        Assert.That(result.EncryptionValidated, Is.False);
        AssertError(result.Error, code);
    }

    static void AssertError(TokenError error, string code)
    {
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Code, Is.EqualTo(code));
    }
}
