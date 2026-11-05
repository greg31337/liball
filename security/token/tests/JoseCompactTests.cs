using System.Text;
using token;

namespace token.Tests;

public class JoseCompactTests
{
    [Test]
    public void ParseDecodesJwsSegmentsAndPreservesExactEncodedViews()
    {
        string tokenText = "eyJhbGciOiJIUzI1NiJ9.cGF5bG9hZA.c2lnbmF0dXJl";

        JoseCompactParseResult result = JoseCompact.Parse(tokenText);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Serialization.Kind, Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(result.Serialization.SegmentCount, Is.EqualTo(3));
            Assert.That(result.Serialization.GetText(), Is.SameAs(tokenText));
            Assert.That(result.Serialization.GetEncodedSegment(0)
                .SequenceEqual("eyJhbGciOiJIUzI1NiJ9"), Is.True);
            Assert.That(Encoding.UTF8.GetString(
                result.Serialization.GetDecodedSegment(0)),
                Is.EqualTo("{\"alg\":\"HS256\"}"));
            Assert.That(Encoding.UTF8.GetString(
                result.Serialization.GetDecodedSegment(1)),
                Is.EqualTo("payload"));
            Assert.That(Encoding.UTF8.GetString(
                result.Serialization.GetDecodedSegment(2)),
                Is.EqualTo("signature"));
        });
    }

    [Test]
    public void ParseDecodesJweShapeIncludingEmptyEncryptedKey()
    {
        string tokenText = "aGVhZGVy..aXY.Y2lwaGVydGV4dA.dGFn";

        JoseCompactParseResult result = JoseCompact.Parse(tokenText);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Serialization.Kind, Is.EqualTo(TokenKind.JweCompact));
            Assert.That(result.Serialization.SegmentCount, Is.EqualTo(5));
            Assert.That(result.Serialization.GetDecodedSegment(0).ToArray(),
                Is.EqualTo(Encoding.ASCII.GetBytes("header")));
            Assert.That(result.Serialization.GetDecodedSegment(1).ToArray(), Is.Empty);
            Assert.That(result.Serialization.GetDecodedSegment(2).ToArray(),
                Is.EqualTo(Encoding.ASCII.GetBytes("iv")));
            Assert.That(result.Serialization.GetDecodedSegment(3).ToArray(),
                Is.EqualTo(Encoding.ASCII.GetBytes("ciphertext")));
            Assert.That(result.Serialization.GetDecodedSegment(4).ToArray(),
                Is.EqualTo(Encoding.ASCII.GetBytes("tag")));
        });
    }

    [Test]
    public void ParsedSegmentArrayCopiesDoNotAliasStoredBytes()
    {
        JoseCompactSerialization serialization = JoseCompact.Parse("AQID...BAUG.")
            .Serialization;

        byte[] copy = serialization.GetDecodedSegmentBytes(0);
        copy[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.EqualTo(new byte[] { 99, 2, 3 }));
            Assert.That(serialization.GetDecodedSegment(0).ToArray(),
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(serialization.GetDecodedSegmentBytes(3),
                Is.EqualTo(new byte[] { 4, 5, 6 }));
        });
    }

    [TestCase("AA.A.AA", 4)]
    [TestCase("AB.AA.AA", 1)]
    [TestCase("AA.AB.AA", 4)]
    public void ParseRejectsNonCanonicalBase64urlSegments(
        string text,
        int position)
    {
        JoseCompactParseResult result = JoseCompact.Parse(text);

        AssertParseFailure(result, position);
    }

    [Test]
    public void ParsePropagatesCompactShapeAndAlphabetFailures()
    {
        JoseCompactParseResult count = JoseCompact.Parse("one.two");
        JoseCompactParseResult alphabet = JoseCompact.Parse("AA.A+.AA");

        Assert.Multiple(() =>
        {
            AssertParseFailure(count, 7);
            AssertParseFailure(alphabet, 4);
        });
    }

    [Test]
    public void WriteJwsProducesCanonicalCompactSerialization()
    {
        TokenTextResult result = JoseCompact.WriteJws(
            Encoding.UTF8.GetBytes("{\"alg\":\"HS256\"}"),
            Encoding.UTF8.GetBytes("payload"),
            Encoding.UTF8.GetBytes("signature"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Text, Is.EqualTo(
                "eyJhbGciOiJIUzI1NiJ9.cGF5bG9hZA.c2lnbmF0dXJl"));
        });
    }

    [Test]
    public void WriteJwePreservesEmptySegmentsAndUsesUrlSafeEncoding()
    {
        TokenTextResult result = JoseCompact.WriteJwe(
            new byte[] { 251, 255 },
            Array.Empty<byte>(),
            new byte[] { 1, 2 },
            new byte[] { 3 },
            new byte[] { 4, 5 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo("-_8..AQI.Aw.BAU"));
            Assert.That(result.Text, Does.Not.Contain("="));
            Assert.That(JoseCompact.Parse(result.Text).Success, Is.True);
        });
    }

    [Test]
    public void SpanWritersSerializeOnlyTheRequestedSlices()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("!header!payload!signature!");

        TokenTextResult jws = JoseCompact.WriteJws(
            bytes.AsSpan(1, 6),
            bytes.AsSpan(8, 7),
            bytes.AsSpan(16, 9));
        TokenTextResult jwe = JoseCompact.WriteJwe(
            bytes.AsSpan(1, 6),
            ReadOnlySpan<byte>.Empty,
            bytes.AsSpan(8, 2),
            bytes.AsSpan(10, 5),
            bytes.AsSpan(16, 3));

        Assert.Multiple(() =>
        {
            Assert.That(jws.Text, Is.EqualTo(
                "aGVhZGVy.cGF5bG9hZA.c2lnbmF0dXJl"));
            Assert.That(jwe.Text, Is.EqualTo(
                "aGVhZGVy..cGE.eWxvYWQ.c2ln"));
        });
    }

    [Test]
    public void GenericWriteRoundTripsBinarySegments()
    {
        byte[][] segments = new byte[5][];
        int segmentIndex = 0;
        while (segmentIndex < segments.Length)
        {
            segments[segmentIndex] = new byte[segmentIndex * 7];
            int byteIndex = 0;
            while (byteIndex < segments[segmentIndex].Length)
            {
                segments[segmentIndex][byteIndex] = (byte)(segmentIndex * 53 + byteIndex * 29);
                byteIndex++;
            }

            segmentIndex++;
        }

        TokenTextResult written = JoseCompact.Write(segments);
        JoseCompactParseResult parsed = JoseCompact.Parse(written.Text);

        Assert.That(written.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        segmentIndex = 0;
        while (segmentIndex < segments.Length)
        {
            Assert.That(parsed.Serialization.GetDecodedSegment(segmentIndex).ToArray(),
                Is.EqualTo(segments[segmentIndex]));
            segmentIndex++;
        }
    }

    [Test]
    public void WriteRejectsInvalidCountsAndNullSegments()
    {
        TokenTextResult two = JoseCompact.Write(
            new byte[][] { Array.Empty<byte>(), Array.Empty<byte>() });
        TokenTextResult four = JoseCompact.Write(new byte[][]
        {
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            Array.Empty<byte>()
        });
        TokenTextResult missing = JoseCompact.Write(new byte[][]
        {
            Array.Empty<byte>(),
            null,
            Array.Empty<byte>()
        });

        Assert.Multiple(() =>
        {
            AssertWriteFailure(two, "invalid_compact_segment_count", 2);
            AssertWriteFailure(four, "invalid_compact_segment_count", 4);
            AssertWriteFailure(missing, "invalid_compact_segment", 1);
        });
    }

    [Test]
    public void ScanFindsThreeJwsSegments()
    {
        string tokenText = "eyJhbGciOiJIUzI1NiJ9.cGF5bG9hZA.c2lnbmF0dXJl";

        JoseCompactScanResult result = JoseCompact.Scan(tokenText);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Segments.SegmentCount, Is.EqualTo(3));
            Assert.That(result.Segments.GetText(), Is.SameAs(tokenText));
            Assert.That(result.Segments.GetSegment(0).SequenceEqual("eyJhbGciOiJIUzI1NiJ9"), Is.True);
            Assert.That(result.Segments.GetSegment(1).SequenceEqual("cGF5bG9hZA"), Is.True);
            Assert.That(result.Segments.GetSegment(2).SequenceEqual("c2lnbmF0dXJl"), Is.True);
        });
    }

    [Test]
    public void ScanFindsFiveJweSegmentsAndBoundaries()
    {
        JoseCompactScanResult result = JoseCompact.Scan("a.bb.ccc.dddd.eeeee");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Segments.SegmentCount, Is.EqualTo(5));
            Assert.That(result.Segments.GetSegmentStart(0), Is.EqualTo(0));
            Assert.That(result.Segments.GetSegmentStart(1), Is.EqualTo(2));
            Assert.That(result.Segments.GetSegmentStart(4), Is.EqualTo(14));
            Assert.That(result.Segments.GetSegmentLength(0), Is.EqualTo(1));
            Assert.That(result.Segments.GetSegmentLength(3), Is.EqualTo(4));
            Assert.That(result.Segments.GetSegment(4).SequenceEqual("eeeee"), Is.True);
        });
    }

    [Test]
    public void ScanAcceptsEveryBase64UrlCharacter()
    {
        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

        JoseCompactScanResult result = JoseCompact.Scan(alphabet + "." + alphabet + "." + alphabet);

        Assert.That(result.Success, Is.True);
    }

    [TestCase("..")]
    [TestCase("....")]
    [TestCase("header..signature")]
    [TestCase("header.encryptedKey.iv..tag")]
    public void ScanPreservesEmptySegmentsForLaterSemanticValidation(string text)
    {
        JoseCompactScanResult result = JoseCompact.Scan(text);

        Assert.That(result.Success, Is.True);
    }

    [TestCase("", 0)]
    [TestCase("one", 3)]
    [TestCase("one.two", 7)]
    [TestCase("one.two.three.four", 18)]
    [TestCase("one.two.three.four.five.six", 23)]
    public void ScanRejectsUnsupportedSegmentCounts(string text, int position)
    {
        JoseCompactScanResult result = JoseCompact.Scan(text);

        AssertInvalidCompactToken(result, position);
    }

    [TestCase("a.b=.c", 3)]
    [TestCase("a.b+c.d", 3)]
    [TestCase("a.b/c.d", 3)]
    [TestCase("a.b c.d", 3)]
    [TestCase("a.b%2Ec.d", 3)]
    [TestCase("a.b:c.d", 3)]
    [TestCase("a.bé.c", 3)]
    public void ScanRejectsCharactersOutsideBase64UrlAlphabet(string text, int position)
    {
        JoseCompactScanResult result = JoseCompact.Scan(text);

        AssertInvalidCompactToken(result, position);
    }

    [Test]
    public void SegmentAccessRejectsInvalidIndexes()
    {
        JoseCompactSegments segments = JoseCompact.Scan("a.b.c").Segments;

        Assert.Multiple(() =>
        {
            Assert.That(() => segments.GetSegment(-1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => segments.GetSegmentStart(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => segments.GetSegmentLength(3), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void ParsedSegmentAccessRejectsInvalidIndexes()
    {
        JoseCompactSerialization serialization = JoseCompact.Parse("AA.AA.AA")
            .Serialization;

        Assert.Multiple(() =>
        {
            Assert.That(() => serialization.GetEncodedSegment(3),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => serialization.GetDecodedSegment(-1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => serialization.GetDecodedSegmentBytes(3),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void ScanRejectsNull()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => JoseCompact.Scan(null), Throws.ArgumentNullException);
            Assert.That(() => JoseCompact.Parse(null), Throws.ArgumentNullException);
            Assert.That(() => JoseCompact.Write(null), Throws.ArgumentNullException);
            Assert.That(
                () => JoseCompact.WriteJws(null, Array.Empty<byte>(), Array.Empty<byte>()),
                Throws.ArgumentNullException);
            Assert.That(
                () => JoseCompact.WriteJws(Array.Empty<byte>(), null, Array.Empty<byte>()),
                Throws.ArgumentNullException);
            Assert.That(
                () => JoseCompact.WriteJws(Array.Empty<byte>(), Array.Empty<byte>(), null),
                Throws.ArgumentNullException);
            Assert.That(
                () => JoseCompact.WriteJwe(
                    Array.Empty<byte>(),
                    null,
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    Array.Empty<byte>()),
                Throws.ArgumentNullException);
        });
    }

    static void AssertInvalidCompactToken(JoseCompactScanResult result, int position)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Segments, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("invalid_compact_token"));
            Assert.That(result.Error.Position, Is.EqualTo(position));
        });
    }

    static void AssertParseFailure(JoseCompactParseResult result, int position)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Serialization, Is.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo("invalid_compact_token"));
        Assert.That(result.Error.Position, Is.EqualTo(position));
    }

    static void AssertWriteFailure(
        TokenTextResult result,
        string code,
        int position)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Error.Position, Is.EqualTo(position));
    }
}
