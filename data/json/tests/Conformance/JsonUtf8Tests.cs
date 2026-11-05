namespace json.Tests;

public class JsonUtf8Tests
{
    [Test]
    public void DecoderAcceptsScalarBoundariesAndNoncharacters()
    {
        (byte[] Bytes, int Scalar)[] cases =
        {
            (new byte[] { 0x00 }, 0x0000),
            (new byte[] { 0x7F }, 0x007F),
            (new byte[] { 0xC2, 0x80 }, 0x0080),
            (new byte[] { 0xDF, 0xBF }, 0x07FF),
            (new byte[] { 0xE0, 0xA0, 0x80 }, 0x0800),
            (new byte[] { 0xE2, 0x80, 0xA8 }, 0x2028),
            (new byte[] { 0xEF, 0xB7, 0x90 }, 0xFDD0),
            (new byte[] { 0xEF, 0xBF, 0xBF }, 0xFFFF),
            (new byte[] { 0xF0, 0x90, 0x80, 0x80 }, 0x10000),
            (new byte[] { 0xF4, 0x8F, 0xBF, 0xBF }, 0x10FFFF)
        };

        foreach ((byte[] bytes, int scalar) in cases)
        {
            Assert.That(DecodeComplete(bytes), Is.EqualTo(scalar), Convert.ToHexString(bytes));
        }
    }

    [Test]
    public void DecoderRejectsMalformedUtf8Families()
    {
        byte[][] invalidCases =
        {
            new byte[] { 0x80 },
            new byte[] { 0xBF },
            new byte[] { 0xC0, 0x80 },
            new byte[] { 0xC1, 0xBF },
            new byte[] { 0xF5, 0x80, 0x80, 0x80 },
            new byte[] { 0xFF },
            new byte[] { 0xE2, 0x28, 0xA1 },
            new byte[] { 0xC2, 0x7F },
            new byte[] { 0xE0, 0x80, 0x80 },
            new byte[] { 0xF0, 0x80, 0x80, 0x80 },
            new byte[] { 0xED, 0xA0, 0x80 },
            new byte[] { 0xED, 0xBF, 0xBF },
            new byte[] { 0xF4, 0x90, 0x80, 0x80 }
        };

        foreach (byte[] bytes in invalidCases)
        {
            JsonParseException exception = Assert.Throws<JsonParseException>(() => DecodeComplete(bytes))
                ?? throw new InvalidOperationException("Expected JsonParseException.");
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidUtf8), Convert.ToHexString(bytes));
        }
    }

    [Test]
    public void DecoderRejectsEveryTruncatedSequenceLengthAndCanBeReset()
    {
        byte[][] truncatedCases =
        {
            new byte[] { 0xC2 },
            new byte[] { 0xE0 },
            new byte[] { 0xE0, 0xA0 },
            new byte[] { 0xF0 },
            new byte[] { 0xF0, 0x90 },
            new byte[] { 0xF0, 0x90, 0x80 }
        };

        foreach (byte[] bytes in truncatedCases)
        {
            Utf8Decoder decoder = new Utf8Decoder();
            foreach (byte value in bytes)
            {
                Assert.That(decoder.PushByte(value), Is.EqualTo(Utf8Decoder.NeedMoreData));
            }

            JsonParseException exception = Assert.Throws<JsonParseException>(() => decoder.CompleteDecoder())
                ?? throw new InvalidOperationException("Expected JsonParseException.");
            Assert.Multiple(() =>
            {
                Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidUtf8));
                Assert.That(decoder.HasPendingBytes(), Is.False);
                Assert.That(decoder.PushByte(0x41), Is.EqualTo(0x41));
            });
        }
    }

    [Test]
    public void DecoderRetainsPartialBytesAcrossFeeds()
    {
        Utf8Decoder decoder = new Utf8Decoder();

        Assert.That(decoder.PushByte(0xF0), Is.EqualTo(Utf8Decoder.NeedMoreData));
        Assert.That(decoder.PushByte(0x9F), Is.EqualTo(Utf8Decoder.NeedMoreData));

        Assert.Multiple(() =>
        {
            Assert.That(decoder.HasPendingBytes(), Is.True);
            Assert.That(decoder.GetPendingByteCount(), Is.EqualTo(2));
            Assert.That(decoder.CopyPendingBytes(), Is.EqualTo(new byte[] { 0xF0, 0x9F }));
        });

        Assert.That(decoder.PushByte(0x98), Is.EqualTo(Utf8Decoder.NeedMoreData));
        Assert.That(decoder.PushByte(0x80), Is.EqualTo(0x1F600));
        Assert.Multiple(() =>
        {
            Assert.That(decoder.HasPendingBytes(), Is.False);
            Assert.That(decoder.GetPendingByteCount(), Is.Zero);
            Assert.That(decoder.CopyPendingBytes(), Is.Empty);
        });
    }

    [Test]
    public void CursorReadsContiguousScalarsWithByteOffsetsAndLengths()
    {
        JsonByteQueue queue = new JsonByteQueue(32);
        queue.EnqueueBytes(new byte[] { 0x41, 0xE2, 0x82, 0xAC, 0xF0, 0x9F, 0x98, 0x80 });
        Utf8Cursor cursor = new Utf8Cursor(queue);

        Assert.That(cursor.ReadScalar(false), Is.EqualTo(0x41));
        Assert.Multiple(() =>
        {
            Assert.That(cursor.GetLastScalarByteOffset(), Is.Zero);
            Assert.That(cursor.GetLastScalarByteCount(), Is.EqualTo(1));
        });

        Assert.That(cursor.ReadScalar(false), Is.EqualTo(0x20AC));
        Assert.Multiple(() =>
        {
            Assert.That(cursor.GetLastScalarByteOffset(), Is.EqualTo(1));
            Assert.That(cursor.GetLastScalarByteCount(), Is.EqualTo(3));
        });

        Assert.That(cursor.ReadScalar(true), Is.EqualTo(0x1F600));
        Assert.Multiple(() =>
        {
            Assert.That(cursor.GetLastScalarByteOffset(), Is.EqualTo(4));
            Assert.That(cursor.GetLastScalarByteCount(), Is.EqualTo(4));
            Assert.That(queue.GetConsumedByteCount(), Is.EqualTo(8));
            Assert.That(cursor.ReadScalar(true), Is.EqualTo(Utf8Decoder.NeedMoreData));
        });
    }

    [Test]
    public void CursorResumesAtEverySplitOfAMultibyteScalar()
    {
        byte[] scalarBytes = { 0xF0, 0x9F, 0x98, 0x80 };

        for (int split = 1; split < scalarBytes.Length; split++)
        {
            JsonByteQueue queue = new JsonByteQueue(8);
            Utf8Cursor cursor = new Utf8Cursor(queue);
            queue.EnqueueBytes(new ReadOnlySpan<byte>(scalarBytes, 0, split));

            Assert.That(cursor.ReadScalar(false), Is.EqualTo(Utf8Decoder.NeedMoreData), $"split {split}");
            Assert.That(cursor.GetPendingScalarByteCount(), Is.EqualTo(split), $"split {split}");

            queue.EnqueueBytes(new ReadOnlySpan<byte>(scalarBytes, split, scalarBytes.Length - split));

            Assert.That(cursor.ReadScalar(true), Is.EqualTo(0x1F600), $"split {split}");
            Assert.Multiple(() =>
            {
                Assert.That(cursor.GetLastScalarByteOffset(), Is.Zero);
                Assert.That(cursor.GetLastScalarByteCount(), Is.EqualTo(4));
                Assert.That(cursor.HasPendingScalarBytes(), Is.False);
            });
        }
    }

    [Test]
    public void CursorRejectsTruncatedAndMalformedSplitInputThenResetsForReuse()
    {
        JsonByteQueue queue = new JsonByteQueue(8);
        Utf8Cursor cursor = new Utf8Cursor(queue);
        queue.EnqueueBytes(new byte[] { 0xE2 });

        Assert.That(cursor.ReadScalar(false), Is.EqualTo(Utf8Decoder.NeedMoreData));
        JsonParseException truncated = Assert.Throws<JsonParseException>(() => cursor.ReadScalar(true))
            ?? throw new InvalidOperationException("Expected JsonParseException.");

        cursor.ResetCursor();
        queue.EnqueueBytes(new byte[] { 0xE2 });
        Assert.That(cursor.ReadScalar(false), Is.EqualTo(Utf8Decoder.NeedMoreData));
        queue.EnqueueBytes(new byte[] { 0x28 });
        JsonParseException malformed = Assert.Throws<JsonParseException>(() => cursor.ReadScalar(false))
            ?? throw new InvalidOperationException("Expected JsonParseException.");

        cursor.ResetCursor();
        queue.EnqueueBytes(new byte[] { 0x42 });

        Assert.Multiple(() =>
        {
            Assert.That(truncated.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidUtf8));
            Assert.That(malformed.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidUtf8));
            Assert.That(cursor.ReadScalar(true), Is.EqualTo(0x42));
            Assert.That(cursor.GetLastScalarByteOffset(), Is.Zero);
            Assert.That(cursor.GetLastScalarByteCount(), Is.EqualTo(1));
        });
    }

    private static int DecodeComplete(byte[] bytes)
    {
        Utf8Decoder decoder = new Utf8Decoder();
        int result = Utf8Decoder.NeedMoreData;
        foreach (byte value in bytes)
        {
            result = decoder.PushByte(value);
        }

        decoder.CompleteDecoder();
        return result;
    }
}
