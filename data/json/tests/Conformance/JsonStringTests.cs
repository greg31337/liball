namespace json.Tests;

public class JsonStringTests
{
    [Test]
    public void ScannerRecognizesEmptyAsciiAndRawUtf8Strings()
    {
        string[] cases =
        {
            "\"\"",
            "\"plain text\"",
            "\"€ 😀 \u2028 \u2029\"",
            "\"" + new string('x', 100) + "\""
        };

        foreach (string text in cases)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
            JsonStringScanner scanner = ScanString(bytes);

            Assert.Multiple(() =>
            {
                Assert.That(scanner.HasStarted(), Is.True, text);
                Assert.That(scanner.HasPendingBytes(), Is.False, text);
                Assert.That(scanner.HasPendingUtf8Bytes(), Is.False, text);
                Assert.That(scanner.IsComplete(), Is.True, text);
                Assert.That(scanner.GetRawByteCount(), Is.EqualTo(bytes.Length), text);
                Assert.That(scanner.CopyRawBytes(), Is.EqualTo(bytes), text);
                Assert.That(scanner.CopyRawContentBytes(), Is.EqualTo(bytes[1..^1]), text);
            });
            Assert.DoesNotThrow(() => scanner.CompleteString(), text);
        }
    }

    [Test]
    public void ScannerResumesAtEveryByteSplitInsideUtf8AndEscapes()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("\"A😀\\\"\\u20ACZ\"");

        for (int split = 1; split < bytes.Length; split++)
        {
            JsonStringScanner scanner = new JsonStringScanner();
            for (int index = 0; index < split; index++)
            {
                Assert.That(scanner.PushByte(bytes[index]), Is.False, $"split {split}");
            }

            Assert.Multiple(() =>
            {
                Assert.That(scanner.HasPendingBytes(), Is.True, $"split {split}");
                Assert.That(scanner.IsComplete(), Is.False, $"split {split}");
                Assert.That(scanner.CopyRawBytes(), Is.EqualTo(bytes[..split]), $"split {split}");
            });

            for (int index = split; index < bytes.Length; index++)
            {
                bool expectedComplete = index == bytes.Length - 1;
                Assert.That(scanner.PushByte(bytes[index]), Is.EqualTo(expectedComplete), $"split {split}");
            }

            Assert.That(scanner.IsComplete(), Is.True, $"split {split}");
        }
    }

    [Test]
    public void ScannerKeepsEveryLegalEscapeInsideTheString()
    {
        string text = "\"\\\"\\\\\\/\\b\\f\\n\\r\\t\\u09aF\"";
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(text);
        JsonStringScanner scanner = ScanString(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(scanner.IsComplete(), Is.True);
            Assert.That(scanner.HasPendingEscapeBytes(), Is.False);
            Assert.That(scanner.CopyRawBytes(), Is.EqualTo(bytes));
            Assert.That(scanner.CopyRawContentBytes(), Is.EqualTo(bytes[1..^1]));
        });
    }

    [Test]
    public void ScannerRejectsEveryUnescapedControlByte()
    {
        for (int value = 0; value < 0x20; value++)
        {
            JsonStringScanner scanner = new JsonStringScanner();
            scanner.PushByte(0x22);

            JsonParseException exception = Assert.Throws<JsonParseException>(() => scanner.PushByte((byte)value))
                ?? throw new InvalidOperationException("Expected JsonParseException.");
            Assert.Multiple(() =>
            {
                Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidStringCharacter), $"0x{value:X2}");
                Assert.That(exception.Diagnostic.SourceExcerpt, Is.EqualTo(new byte[] { 0x22, (byte)value }), $"0x{value:X2}");
                Assert.That(scanner.HasStarted(), Is.False, $"0x{value:X2}");
                Assert.That(scanner.GetRawByteCount(), Is.Zero, $"0x{value:X2}");
            });
        }
    }

    [Test]
    public void ScannerRejectsInvalidAndIncompleteEscapes()
    {
        AssertStringFailure("\"\\q", false, JsonDiagnosticCode.InvalidEscape);
        AssertStringFailure("\"\\u12X", false, JsonDiagnosticCode.InvalidUnicodeEscape);
        AssertStringFailure("\"\\", true, JsonDiagnosticCode.InvalidEscape);
        AssertStringFailure("\"\\u", true, JsonDiagnosticCode.InvalidUnicodeEscape);
        AssertStringFailure("\"\\u0", true, JsonDiagnosticCode.InvalidUnicodeEscape);
        AssertStringFailure("\"\\u00", true, JsonDiagnosticCode.InvalidUnicodeEscape);
        AssertStringFailure("\"\\u000", true, JsonDiagnosticCode.InvalidUnicodeEscape);
    }

    [Test]
    public void ScannerRejectsMalformedAndTruncatedRawUtf8()
    {
        byte[][] malformedCases =
        {
            new byte[] { 0x22, 0x80 },
            new byte[] { 0x22, 0xC0 },
            new byte[] { 0x22, 0xE2, 0x28 }
        };

        foreach (byte[] bytes in malformedCases)
        {
            JsonStringScanner scanner = new JsonStringScanner();
            JsonParseException? caught = null;
            foreach (byte value in bytes)
            {
                if (caught == null)
                {
                    caught = CatchStringFailure(scanner, value);
                }
            }

            JsonParseException exception = caught
                ?? throw new InvalidOperationException("Expected JsonParseException.");
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidUtf8), Convert.ToHexString(bytes));
        }

        JsonStringScanner truncated = new JsonStringScanner();
        truncated.PushByte(0x22);
        truncated.PushByte(0xF0);
        truncated.PushByte(0x9F);

        JsonParseException truncatedException = Assert.Throws<JsonParseException>(() => truncated.CompleteString())
            ?? throw new InvalidOperationException("Expected JsonParseException.");
        Assert.That(truncatedException.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidUtf8));
    }

    [Test]
    public void ScannerRejectsMissingQuotesAndCanBeReused()
    {
        JsonStringScanner scanner = new JsonStringScanner();
        JsonParseException opening = Assert.Throws<JsonParseException>(() => scanner.PushByte(0x41))
            ?? throw new InvalidOperationException("Expected JsonParseException.");

        scanner.PushByte(0x22);
        scanner.PushByte(0x41);
        JsonParseException closing = Assert.Throws<JsonParseException>(() => scanner.CompleteString())
            ?? throw new InvalidOperationException("Expected JsonParseException.");

        byte[] valid = System.Text.Encoding.ASCII.GetBytes("\"reused\"");
        foreach (byte value in valid)
        {
            scanner.PushByte(value);
        }

        Assert.Multiple(() =>
        {
            Assert.That(opening.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidStringCharacter));
            Assert.That(closing.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.UnexpectedEndOfInput));
            Assert.That(scanner.IsComplete(), Is.True);
            Assert.That(scanner.CopyRawBytes(), Is.EqualTo(valid));
        });

        scanner.ResetScanner();
        Assert.Multiple(() =>
        {
            Assert.That(scanner.HasStarted(), Is.False);
            Assert.That(scanner.HasPendingBytes(), Is.False);
            Assert.That(scanner.IsComplete(), Is.False);
            Assert.That(scanner.CopyRawBytes(), Is.Empty);
            Assert.That(scanner.CopyRawContentBytes(), Is.Empty);
        });
    }

    static JsonStringScanner ScanString(byte[] bytes)
    {
        JsonStringScanner scanner = new JsonStringScanner();
        for (int index = 0; index < bytes.Length; index++)
        {
            bool expectedComplete = index == bytes.Length - 1;
            Assert.That(scanner.PushByte(bytes[index]), Is.EqualTo(expectedComplete), Convert.ToHexString(bytes));
        }

        return scanner;
    }

    static void AssertStringFailure(string text, bool completeInput, JsonDiagnosticCode expectedCode)
    {
        JsonStringScanner scanner = new JsonStringScanner();
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(text);
        JsonParseException? caught = null;
        foreach (byte value in bytes)
        {
            if (caught == null)
            {
                caught = CatchStringFailure(scanner, value);
            }
        }

        if (completeInput && caught == null)
        {
            caught = Assert.Throws<JsonParseException>(() => scanner.CompleteString());
        }

        JsonParseException exception = caught
            ?? throw new InvalidOperationException($"Expected JsonParseException for {text}.");
        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(expectedCode), text);
            Assert.That(exception.Diagnostic.SourceExcerpt, Is.EqualTo(bytes), text);
        });
    }

    static JsonParseException? CatchStringFailure(JsonStringScanner scanner, byte value)
    {
        try
        {
            scanner.PushByte(value);
            return null;
        }
        catch (JsonParseException exception)
        {
            return exception;
        }
    }
}
