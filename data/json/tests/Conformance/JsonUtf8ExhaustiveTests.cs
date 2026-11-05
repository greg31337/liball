namespace json.Tests;

public class JsonUtf8ExhaustiveTests
{
    [Test]
    public void DecoderAcceptsScalarEncodingBoundariesAndEveryUnicodeNoncharacter()
    {
        List<int> scalars = new List<int>
        {
            0x0000,
            0x007F,
            0x0080,
            0x07FF,
            0x0800,
            0xD7FF,
            0xE000,
            0xFFFF,
            0x10000,
            0x10FFFF
        };

        for (int scalar = 0xFDD0; scalar <= 0xFDEF; scalar++)
        {
            scalars.Add(scalar);
        }

        for (int plane = 0; plane <= 16; plane++)
        {
            scalars.Add((plane << 16) | 0xFFFE);
            scalars.Add((plane << 16) | 0xFFFF);
        }

        foreach (int scalar in scalars.Distinct())
        {
            byte[] bytes = EncodeScalar(scalar);
            Assert.That(DecodeScalar(bytes), Is.EqualTo(scalar), $"U+{scalar:X}");
        }
    }

    [Test]
    public void CursorResumesEveryMultibyteBoundaryScalarAtEverySplitPoint()
    {
        int[] scalars =
        {
            0x0080,
            0x07FF,
            0x0800,
            0xD7FF,
            0xE000,
            0xFFFF,
            0x10000,
            0x10FFFF
        };

        foreach (int scalar in scalars)
        {
            byte[] bytes = EncodeScalar(scalar);
            for (int split = 1; split < bytes.Length; split++)
            {
                JsonByteQueue queue = new JsonByteQueue(8);
                Utf8Cursor cursor = new Utf8Cursor(queue);
                queue.EnqueueBytes(new ReadOnlySpan<byte>(bytes, 0, split));

                Assert.That(cursor.ReadScalar(false), Is.EqualTo(Utf8Decoder.NeedMoreData), $"U+{scalar:X}, split {split}");
                Assert.That(cursor.GetPendingScalarByteCount(), Is.EqualTo(split), $"U+{scalar:X}, split {split}");

                queue.EnqueueBytes(new ReadOnlySpan<byte>(bytes, split, bytes.Length - split));
                Assert.That(cursor.ReadScalar(true), Is.EqualTo(scalar), $"U+{scalar:X}, split {split}");
                Assert.Multiple(() =>
                {
                    Assert.That(cursor.GetLastScalarByteCount(), Is.EqualTo(bytes.Length), $"U+{scalar:X}, split {split}");
                    Assert.That(cursor.GetLastScalarByteOffset(), Is.Zero, $"U+{scalar:X}, split {split}");
                    Assert.That(cursor.HasPendingScalarBytes(), Is.False, $"U+{scalar:X}, split {split}");
                });
            }
        }
    }

    [Test]
    public void DecoderRejectsEveryForbiddenLeadingByte()
    {
        List<byte> leaders = new List<byte>();
        for (int value = 0x80; value <= 0xBF; value++)
        {
            leaders.Add((byte)value);
        }

        leaders.Add(0xC0);
        leaders.Add(0xC1);
        for (int value = 0xF5; value <= 0xFF; value++)
        {
            leaders.Add((byte)value);
        }

        foreach (byte leader in leaders)
        {
            AssertInvalidUtf8(new byte[] { leader }, $"leader 0x{leader:X2}");
        }
    }

    [Test]
    public void DecoderRejectsEveryNonContinuationByteAtEveryContinuationPosition()
    {
        byte[][] templates =
        {
            new byte[] { 0xC2, 0x80 },
            new byte[] { 0xE1, 0x80, 0x80 },
            new byte[] { 0xF1, 0x80, 0x80, 0x80 }
        };

        foreach (byte[] template in templates)
        {
            for (int position = 1; position < template.Length; position++)
            {
                for (int candidate = 0; candidate <= 0xFF; candidate++)
                {
                    if (candidate >= 0x80 && candidate <= 0xBF)
                    {
                        continue;
                    }

                    byte[] bytes = (byte[])template.Clone();
                    bytes[position] = (byte)candidate;
                    AssertInvalidUtf8(bytes, $"{Convert.ToHexString(template)}, position {position}, byte 0x{candidate:X2}");
                }
            }
        }
    }

    [Test]
    public void DecoderRejectsAllRestrictedSecondByteFamilies()
    {
        for (int second = 0x80; second <= 0x9F; second++)
        {
            AssertInvalidUtf8(new byte[] { 0xE0, (byte)second, 0x80 }, $"E0 second 0x{second:X2}");
        }

        for (int second = 0x80; second <= 0x8F; second++)
        {
            AssertInvalidUtf8(new byte[] { 0xF0, (byte)second, 0x80, 0x80 }, $"F0 second 0x{second:X2}");
        }

        for (int second = 0xA0; second <= 0xBF; second++)
        {
            for (int third = 0x80; third <= 0xBF; third++)
            {
                AssertInvalidUtf8(new byte[] { 0xED, (byte)second, (byte)third }, $"surrogate ED {second:X2} {third:X2}");
            }
        }

        for (int second = 0x90; second <= 0xBF; second++)
        {
            AssertInvalidUtf8(new byte[] { 0xF4, (byte)second, 0x80, 0x80 }, $"above range F4 second 0x{second:X2}");
            AssertInvalidUtf8(new byte[] { 0xF4, (byte)second, 0xBF, 0xBF }, $"above range F4 second 0x{second:X2}");
        }
    }

    [Test]
    public void DecoderRejectsEveryTruncatedPrefixOfBoundarySequences()
    {
        int[] scalars = { 0x0080, 0x07FF, 0x0800, 0xD7FF, 0xE000, 0xFFFF, 0x10000, 0x10FFFF };

        foreach (int scalar in scalars)
        {
            byte[] bytes = EncodeScalar(scalar);
            for (int length = 1; length < bytes.Length; length++)
            {
                byte[] prefix = bytes[..length];
                Utf8Decoder decoder = new Utf8Decoder();
                foreach (byte value in prefix)
                {
                    Assert.That(decoder.PushByte(value), Is.EqualTo(Utf8Decoder.NeedMoreData), $"U+{scalar:X}, length {length}");
                }

                JsonParseException exception = Assert.Throws<JsonParseException>(() => decoder.CompleteDecoder())
                    ?? throw new InvalidOperationException("Expected JsonParseException.");
                Assert.Multiple(() =>
                {
                    Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidUtf8), $"U+{scalar:X}, length {length}");
                    Assert.That(decoder.HasPendingBytes(), Is.False, $"U+{scalar:X}, length {length}");
                });
            }
        }
    }

    [Test]
    public void CursorRejectsRepresentativeMalformedFamiliesAtEveryChunkSplit()
    {
        byte[][] malformedCases =
        {
            new byte[] { 0xC0, 0x80 },
            new byte[] { 0xE0, 0x80, 0x80 },
            new byte[] { 0xED, 0xA0, 0x80 },
            new byte[] { 0xF0, 0x80, 0x80, 0x80 },
            new byte[] { 0xF4, 0x90, 0x80, 0x80 },
            new byte[] { 0xE2, 0x28, 0xA1 },
            new byte[] { 0xF0, 0x9F, 0x28, 0x80 }
        };

        foreach (byte[] bytes in malformedCases)
        {
            for (int split = 0; split <= bytes.Length; split++)
            {
                JsonByteQueue queue = new JsonByteQueue(8);
                Utf8Cursor cursor = new Utf8Cursor(queue);
                queue.EnqueueBytes(new ReadOnlySpan<byte>(bytes, 0, split));
                JsonParseException? caught = CatchCursorFailure(cursor, false);

                if (caught == null)
                {
                    queue.EnqueueBytes(new ReadOnlySpan<byte>(bytes, split, bytes.Length - split));
                    caught = CatchCursorFailure(cursor, true);
                }

                JsonParseException exception = caught
                    ?? throw new InvalidOperationException($"Expected JsonParseException for {Convert.ToHexString(bytes)}, split {split}.");
                Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidUtf8), $"{Convert.ToHexString(bytes)}, split {split}");
            }
        }
    }

    [Test]
    public void LexerRejectsOrAllowsOnlyOneInitialBomAcrossEverySplit()
    {
        byte[] bom = { 0xEF, 0xBB, 0xBF };
        byte[] document = bom.Concat(System.Text.Encoding.ASCII.GetBytes("null")).ToArray();

        for (int split = 0; split <= bom.Length; split++)
        {
            JsonLexer rejecting = new JsonLexer(null);
            rejecting.FeedBytes(new ReadOnlySpan<byte>(document, 0, split));
            JsonParseException? rejection = CatchLexerFailure(rejecting, false);
            if (rejection == null)
            {
                rejecting.FeedBytes(new ReadOnlySpan<byte>(document, split, document.Length - split));
                rejection = CatchLexerFailure(rejecting, true);
            }

            Assert.That(rejection?.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.BomNotAllowed), $"reject split {split}");

            JsonReaderOptions options = new JsonReaderOptions();
            options.BomPolicy = JsonBomPolicy.AllowInitial;
            JsonLexer allowing = new JsonLexer(options);
            allowing.FeedBytes(new ReadOnlySpan<byte>(document, 0, split));
            Assert.That(allowing.ReadNextToken(false), Is.EqualTo(JsonReadStatus.NeedMoreData), $"allow split {split} prefix");
            allowing.FeedBytes(new ReadOnlySpan<byte>(document, split, document.Length - split));
            Assert.That(allowing.ReadNextToken(true), Is.EqualTo(JsonReadStatus.TokenAvailable), $"allow split {split} token");
            Assert.That(allowing.GetCurrentToken()?.Kind, Is.EqualTo(JsonTokenKind.Null), $"allow split {split} kind");
            Assert.That(allowing.GetCurrentToken()?.Range?.Start?.ByteOffset, Is.EqualTo(3), $"allow split {split} offset");
            Assert.That(allowing.ReadNextToken(true), Is.EqualTo(JsonReadStatus.StreamComplete), $"allow split {split} complete");
        }
    }

    [Test]
    public void LexerRejectsRepeatedEmbeddedAndTruncatedBomSequences()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.BomPolicy = JsonBomPolicy.AllowInitial;
        byte[] bom = { 0xEF, 0xBB, 0xBF };

        AssertBomFailure(bom.Concat(bom).ToArray(), options, JsonDiagnosticCode.UnexpectedBom);
        AssertBomFailure(new byte[] { 0x20, 0xEF, 0xBB, 0xBF }, options, JsonDiagnosticCode.UnexpectedBom);
        AssertBomFailure(new byte[] { 0xEF }, options, JsonDiagnosticCode.InvalidUtf8);
        AssertBomFailure(new byte[] { 0xEF, 0xBB }, options, JsonDiagnosticCode.InvalidUtf8);

        JsonLexer stringLexer = new JsonLexer(null);
        stringLexer.FeedBytes(new byte[] { 0x22, 0xEF, 0xBB, 0xBF, 0x22 });
        Assert.That(stringLexer.ReadNextToken(true), Is.EqualTo(JsonReadStatus.TokenAvailable));
        Assert.That(stringLexer.GetCurrentToken()?.StringValue, Is.EqualTo("\uFEFF"));
    }

    static byte[] EncodeScalar(int scalar)
    {
        return System.Text.Encoding.UTF8.GetBytes(char.ConvertFromUtf32(scalar));
    }

    static int DecodeScalar(byte[] bytes)
    {
        Utf8Decoder decoder = new Utf8Decoder();
        int scalar = Utf8Decoder.NeedMoreData;
        for (int index = 0; index < bytes.Length; index++)
        {
            scalar = decoder.PushByte(bytes[index]);
            if (index < bytes.Length - 1)
            {
                Assert.That(scalar, Is.EqualTo(Utf8Decoder.NeedMoreData), Convert.ToHexString(bytes));
            }
        }

        decoder.CompleteDecoder();
        return scalar;
    }

    static void AssertInvalidUtf8(byte[] bytes, string description)
    {
        Utf8Decoder decoder = new Utf8Decoder();
        JsonParseException exception = Assert.Throws<JsonParseException>(() =>
        {
            foreach (byte value in bytes)
            {
                decoder.PushByte(value);
            }

            decoder.CompleteDecoder();
        }) ?? throw new InvalidOperationException($"Expected JsonParseException for {description}.");

        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidUtf8), description);
    }

    static JsonParseException? CatchCursorFailure(Utf8Cursor cursor, bool finalInput)
    {
        try
        {
            cursor.ReadScalar(finalInput);
            return null;
        }
        catch (JsonParseException exception)
        {
            return exception;
        }
    }

    static JsonParseException? CatchLexerFailure(JsonLexer lexer, bool finalInput)
    {
        try
        {
            lexer.ReadNextToken(finalInput);
            return null;
        }
        catch (JsonParseException exception)
        {
            return exception;
        }
    }

    static void AssertBomFailure(byte[] bytes, JsonReaderOptions options, JsonDiagnosticCode expectedCode)
    {
        JsonLexer lexer = new JsonLexer(options);
        lexer.FeedBytes(bytes);
        JsonParseException exception = Assert.Throws<JsonParseException>(() => lexer.ReadNextToken(true))
            ?? throw new InvalidOperationException("Expected JsonParseException.");
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(expectedCode), Convert.ToHexString(bytes));
    }
}
