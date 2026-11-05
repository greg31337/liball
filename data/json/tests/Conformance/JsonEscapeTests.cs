namespace json.Tests;

public class JsonEscapeTests
{
    [Test]
    public void DecoderRejectsUndefinedUnicodePolicy()
    {
        JsonParseException exception = Assert.Throws<JsonParseException>(() => new JsonEscapeDecoder((JsonUnicodePolicy)99))
            ?? throw new InvalidOperationException("Expected JsonParseException.");

        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
    }

    [Test]
    public void DecoderHandlesEverySimpleEscape()
    {
        (string Escape, int Value)[] cases =
        {
            ("\\\"", 0x22),
            ("\\\\", 0x5C),
            ("\\/", 0x2F),
            ("\\b", 0x08),
            ("\\f", 0x0C),
            ("\\n", 0x0A),
            ("\\r", 0x0D),
            ("\\t", 0x09)
        };

        JsonEscapeDecoder decoder = new JsonEscapeDecoder(JsonUnicodePolicy.UnicodeScalar);
        foreach ((string escape, int value) in cases)
        {
            PushEscape(decoder, escape);
            Assert.That(decoder.GetDecodedValueAt(decoder.GetDecodedValueCount() - 1), Is.EqualTo(value), escape);
        }

        Assert.Multiple(() =>
        {
            Assert.That(decoder.CopyDecodedValues(), Is.EqualTo(cases.Select(item => item.Value).ToArray()));
            Assert.That(decoder.CopyRawBytes(), Is.EqualTo(System.Text.Encoding.ASCII.GetBytes(string.Concat(cases.Select(item => item.Escape)))));
            Assert.That(decoder.HasPendingEscapeBytes(), Is.False);
            Assert.That(decoder.HasPendingHighSurrogate(), Is.False);
        });
    }

    [Test]
    public void DecoderAcceptsFourHexadecimalDigitsInEitherCase()
    {
        (string Escape, int Value)[] cases =
        {
            ("\\u0000", 0x0000),
            ("\\u002F", 0x002F),
            ("\\u00aF", 0x00AF),
            ("\\u2028", 0x2028),
            ("\\uFfFf", 0xFFFF)
        };

        JsonEscapeDecoder decoder = new JsonEscapeDecoder(JsonUnicodePolicy.UnicodeScalar);
        foreach ((string escape, int value) in cases)
        {
            PushEscape(decoder, escape);
            Assert.That(decoder.GetDecodedValueAt(decoder.GetDecodedValueCount() - 1), Is.EqualTo(value), escape);
        }

        decoder.CompleteDecoder();
        Assert.That(decoder.CopyDecodedValues(), Is.EqualTo(cases.Select(item => item.Value).ToArray()));
    }

    [Test]
    public void DecoderCombinesSurrogateBoundaryPairsAcrossEverySplit()
    {
        (string Text, int Scalar)[] cases =
        {
            ("\\uD800\\uDC00", 0x10000),
            ("\\udbff\\udfff", 0x10FFFF),
            ("\\uD83D\\uDE00", 0x1F600)
        };

        foreach ((string text, int scalar) in cases)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(text);
            for (int split = 1; split < bytes.Length; split++)
            {
                JsonEscapeDecoder decoder = new JsonEscapeDecoder(JsonUnicodePolicy.UnicodeScalar);
                for (int index = 0; index < split; index++)
                {
                    decoder.PushByte(bytes[index]);
                }

                if (split < 6 || split > 6)
                {
                    Assert.That(decoder.HasPendingEscapeBytes(), Is.True, $"{text} split {split}");
                }
                else
                {
                    Assert.That(decoder.HasPendingHighSurrogate(), Is.True, $"{text} split {split}");
                }

                for (int index = split; index < bytes.Length; index++)
                {
                    decoder.PushByte(bytes[index]);
                }

                decoder.CompleteDecoder();
                Assert.That(decoder.CopyDecodedValues(), Is.EqualTo(new int[] { scalar }), $"{text} split {split}");
            }
        }
    }

    [Test]
    public void SyntaxCompatiblePolicyRetainsLoneAndRepeatedSurrogates()
    {
        JsonEscapeDecoder decoder = new JsonEscapeDecoder(JsonUnicodePolicy.SyntaxCompatible);

        PushEscape(decoder, "\\uD800");
        Assert.That(decoder.CopyDecodedValues(), Is.Empty);

        PushEscape(decoder, "\\n");
        Assert.That(decoder.CopyDecodedValues(), Is.EqualTo(new int[] { 0xD800, 0x0A }));

        PushEscape(decoder, "\\uDC00");
        PushEscape(decoder, "\\uD801");
        PushEscape(decoder, "\\uD802");
        Assert.That(decoder.CopyDecodedValues(), Is.EqualTo(new int[] { 0xD800, 0x0A, 0xDC00, 0xD801 }));

        decoder.CompleteDecoder();
        Assert.Multiple(() =>
        {
            Assert.That(decoder.CopyDecodedValues(), Is.EqualTo(new int[] { 0xD800, 0x0A, 0xDC00, 0xD801, 0xD802 }));
            Assert.That(decoder.HasPendingHighSurrogate(), Is.False);
        });
    }

    [Test]
    public void UnicodeScalarPolicyRejectsEveryUnpairedSurrogateShape()
    {
        AssertSurrogateFailure("\\uDC00", false);
        AssertSurrogateFailure("\\uD800", true);
        AssertSurrogateFailure("\\uD800\\n", false);
        AssertSurrogateFailure("\\uD800\\u0041", false);
        AssertSurrogateFailure("\\uD800\\uD801", false);
        AssertSurrogateFailure("\\uDC00\\uD800", false);
    }

    [Test]
    public void DecoderRejectsInvalidAndTruncatedEscapeSpellings()
    {
        AssertEscapeFailure("x", false, JsonDiagnosticCode.InvalidEscape);
        AssertEscapeFailure("\\q", false, JsonDiagnosticCode.InvalidEscape);
        AssertEscapeFailure("\\u12X", false, JsonDiagnosticCode.InvalidUnicodeEscape);
        AssertEscapeFailure("\\", true, JsonDiagnosticCode.InvalidEscape);
        AssertEscapeFailure("\\u", true, JsonDiagnosticCode.InvalidUnicodeEscape);
        AssertEscapeFailure("\\u0", true, JsonDiagnosticCode.InvalidUnicodeEscape);
        AssertEscapeFailure("\\u00", true, JsonDiagnosticCode.InvalidUnicodeEscape);
        AssertEscapeFailure("\\u000", true, JsonDiagnosticCode.InvalidUnicodeEscape);
    }

    [Test]
    public void DecoderGrowsOutputBuffersAndCanBeResetAndReused()
    {
        JsonEscapeDecoder decoder = new JsonEscapeDecoder(JsonUnicodePolicy.UnicodeScalar);
        string escapes = string.Concat(Enumerable.Repeat("\\n", 40));
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(escapes);
        foreach (byte value in bytes)
        {
            decoder.PushByte(value);
        }

        Assert.Multiple(() =>
        {
            Assert.That(decoder.GetRawByteCount(), Is.EqualTo(80));
            Assert.That(decoder.GetDecodedValueCount(), Is.EqualTo(40));
            Assert.That(decoder.CopyRawBytes(), Is.EqualTo(bytes));
            Assert.That(decoder.CopyDecodedValues(), Is.All.EqualTo(0x0A));
            Assert.That(() => decoder.GetDecodedValueAt(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => decoder.GetDecodedValueAt(40), Throws.TypeOf<ArgumentOutOfRangeException>());
        });

        decoder.ResetDecoder();
        PushEscape(decoder, "\\u0041");

        Assert.Multiple(() =>
        {
            Assert.That(decoder.CopyRawBytes(), Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("\\u0041")));
            Assert.That(decoder.CopyDecodedValues(), Is.EqualTo(new int[] { 0x41 }));
            Assert.That(decoder.HasPendingEscapeBytes(), Is.False);
        });
    }

    static void PushEscape(JsonEscapeDecoder decoder, string escape)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(escape);
        for (int index = 0; index < bytes.Length; index++)
        {
            bool expectedComplete = index == bytes.Length - 1;
            Assert.That(decoder.PushByte(bytes[index]), Is.EqualTo(expectedComplete), escape);
        }
    }

    static void AssertSurrogateFailure(string text, bool completeInput)
    {
        AssertEscapeFailure(text, completeInput, JsonDiagnosticCode.InvalidSurrogatePair);
    }

    static void AssertEscapeFailure(string text, bool completeInput, JsonDiagnosticCode expectedCode)
    {
        JsonEscapeDecoder decoder = new JsonEscapeDecoder(JsonUnicodePolicy.UnicodeScalar);
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(text);
        JsonParseException? caught = null;
        foreach (byte value in bytes)
        {
            if (caught == null)
            {
                caught = CatchEscapeFailure(decoder, value);
            }
        }

        if (completeInput && caught == null)
        {
            caught = Assert.Throws<JsonParseException>(() => decoder.CompleteDecoder());
        }

        JsonParseException exception = caught
            ?? throw new InvalidOperationException($"Expected JsonParseException for {text}.");
        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(expectedCode), text);
            Assert.That(exception.Diagnostic.SourceExcerpt, Is.EqualTo(bytes[..exception.Diagnostic.SourceExcerpt!.Length]), text);
            Assert.That(decoder.GetRawByteCount(), Is.Zero, text);
            Assert.That(decoder.GetDecodedValueCount(), Is.Zero, text);
            Assert.That(decoder.HasPendingEscapeBytes(), Is.False, text);
            Assert.That(decoder.HasPendingHighSurrogate(), Is.False, text);
        });
    }

    static JsonParseException? CatchEscapeFailure(JsonEscapeDecoder decoder, byte value)
    {
        try
        {
            decoder.PushByte(value);
            return null;
        }
        catch (JsonParseException exception)
        {
            return exception;
        }
    }
}
