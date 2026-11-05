using System.Text;
using token;

namespace token.Tests;

public class TokenDetectorTests
{
    const string Jws =
        "eyJhbGciOiJIUzI1NiJ9.e30.AA";
    const string Jwe =
        "eyJhbGciOiJkaXIiLCJlbmMiOiJBMTI4R0NNIn0..AA..AA";
    const string ProtectedJws = "eyJhbGciOiJIUzI1NiJ9";
    const string ProtectedJwe =
        "eyJhbGciOiJkaXIiLCJlbmMiOiJBMTI4R0NNIn0";

    [Test]
    public void DetectRecognizesCompactJoseAndSelectiveDisclosureJwt()
    {
        TokenKindResult jws = TokenDetector.Detect(Jws);
        TokenKindResult jwe = TokenDetector.Detect(Jwe);
        TokenKindResult sdJwt = TokenDetector.Detect(Jws + "~");

        Assert.Multiple(() =>
        {
            AssertDetected(jws, TokenKind.JwsCompact);
            AssertDetected(jwe, TokenKind.JweCompact);
            AssertDetected(sdJwt, TokenKind.SdJwt);
        });
    }

    [Test]
    public void DetectRecognizesFlattenedAndGeneralJoseJson()
    {
        string flattenedJws =
            "{\"payload\":\"e30\",\"protected\":\""
            + ProtectedJws
            + "\",\"signature\":\"AA\"}";
        string generalJws =
            "{\"payload\":\"e30\",\"signatures\":[{\"protected\":\""
            + ProtectedJws
            + "\",\"signature\":\"AA\"}]}";
        string flattenedJwe =
            "{\"protected\":\""
            + ProtectedJwe
            + "\",\"iv\":\"AA\",\"ciphertext\":\"\",\"tag\":\"AA\"}";
        string generalJwe =
            "{\"protected\":\""
            + ProtectedJwe
            + "\",\"recipients\":[{\"encrypted_key\":\"\"}],"
            + "\"iv\":\"AA\",\"ciphertext\":\"\",\"tag\":\"AA\"}";

        Assert.Multiple(() =>
        {
            AssertDetected(
                TokenDetector.Detect(flattenedJws),
                TokenKind.JwsJson);
            AssertDetected(
                TokenDetector.Detect(generalJws),
                TokenKind.JwsJson);
            AssertDetected(
                TokenDetector.Detect(flattenedJwe),
                TokenKind.JweJson);
            AssertDetected(
                TokenDetector.Detect(generalJwe),
                TokenKind.JweJson);
        });
    }

    [Test]
    public void DetectBytesRecognizesTaggedCoseAndCwt()
    {
        byte[] cose = new byte[]
        {
            0xD2, 0x84, 0x40, 0xA0, 0x41, 0x01, 0x40
        };
        byte[] cwt = new byte[]
        {
            0xD8, 0x3D, 0xD2, 0x84, 0x40, 0xA0, 0x41, 0x01, 0x40
        };

        Assert.Multiple(() =>
        {
            AssertDetected(TokenDetector.DetectBytes(cose), TokenKind.Cose);
            AssertDetected(TokenDetector.DetectBytes(cwt), TokenKind.Cwt);
        });
    }

    [Test]
    public void DetectBytesAlsoRecognizesUtf8TextTokens()
    {
        TokenKindResult result = TokenDetector.DetectBytes(
            Encoding.UTF8.GetBytes(Jws));

        AssertDetected(result, TokenKind.JwsCompact);
    }

    [Test]
    public void UnknownInputsCompleteDetectionWithoutGuessing()
    {
        byte[] untaggedCose = new byte[]
        {
            0x84, 0x40, 0xA0, 0x41, 0x01, 0x40
        };
        byte[] malformedCwt = new byte[]
        {
            0xD8, 0x3D, 0xD2, 0x80
        };

        TokenKindResult text = TokenDetector.Detect("opaque token");
        TokenKindResult compact = TokenDetector.Detect("AA.AA.AA");
        TokenKindResult binary = TokenDetector.DetectBytes(
            new byte[] { 0xFF, 0xFE });
        TokenKindResult untagged = TokenDetector.DetectBytes(untaggedCose);
        TokenKindResult malformed = TokenDetector.DetectBytes(malformedCwt);

        Assert.Multiple(() =>
        {
            AssertUnknown(text);
            AssertUnknown(compact);
            AssertUnknown(binary);
            AssertUnknown(untagged);
            AssertUnknown(malformed);
        });
    }

    [Test]
    public void DetectionRejectsInvalidTextAndOversizedBinaryInput()
    {
        TokenKindResult invalidText = TokenDetector.Detect("\uD800");
        TokenKindResult oversized = TokenDetector.DetectBytes(
            new byte[1048577]);

        Assert.Multiple(() =>
        {
            Assert.That(invalidText.Success, Is.False);
            Assert.That(invalidText.Error.Code, Is.EqualTo("invalid_utf16"));
            Assert.That(invalidText.Kind, Is.EqualTo(TokenKind.Unknown));
            Assert.That(oversized.Success, Is.False);
            Assert.That(oversized.Error.Code, Is.EqualTo("token_too_large"));
            Assert.That(oversized.Error.Position, Is.EqualTo(1048576));
            Assert.That(oversized.Kind, Is.EqualTo(TokenKind.Unknown));
        });
    }

    [Test]
    public void PublicMethodsRejectNullInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => TokenDetector.Detect(null),
                Throws.ArgumentNullException);
            Assert.That(
                () => TokenDetector.DetectBytes(null),
                Throws.ArgumentNullException);
        });
    }

    static void AssertDetected(TokenKindResult result, TokenKind kind)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Kind, Is.EqualTo(kind));
        });
    }

    static void AssertUnknown(TokenKindResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Kind, Is.EqualTo(TokenKind.Unknown));
        });
    }
}
