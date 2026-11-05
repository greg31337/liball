namespace json.Tests;

public class JsonLiteralTests
{
    [Test]
    public void ScannerRecognizesLiteralKindsValuesAndRawBytes()
    {
        (string Text, JsonTokenKind Kind, bool BooleanValue)[] cases =
        {
            ("true", JsonTokenKind.Boolean, true),
            ("false", JsonTokenKind.Boolean, false),
            ("null", JsonTokenKind.Null, false)
        };

        foreach ((string text, JsonTokenKind kind, bool booleanValue) in cases)
        {
            JsonLiteralScanner scanner = ScanLiteral(text);

            Assert.Multiple(() =>
            {
                Assert.That(scanner.HasPendingBytes(), Is.False, text);
                Assert.That(scanner.IsComplete(), Is.True, text);
                Assert.That(scanner.GetTokenKind(), Is.EqualTo(kind), text);
                Assert.That(scanner.GetBooleanValue(), Is.EqualTo(booleanValue), text);
                Assert.That(scanner.GetRawByteCount(), Is.EqualTo(text.Length), text);
                Assert.That(scanner.CopyRawBytes(), Is.EqualTo(System.Text.Encoding.ASCII.GetBytes(text)), text);
            });
            Assert.DoesNotThrow(() => scanner.CompleteLiteral(), text);
        }
    }

    [Test]
    public void ScannerResumesAtEveryLiteralSplit()
    {
        string[] literals = { "true", "false", "null" };

        foreach (string literal in literals)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(literal);
            for (int split = 1; split < bytes.Length; split++)
            {
                JsonLiteralScanner scanner = new JsonLiteralScanner();
                for (int index = 0; index < split; index++)
                {
                    Assert.That(scanner.PushByte(bytes[index]), Is.False, $"{literal} split {split}");
                }

                Assert.Multiple(() =>
                {
                    Assert.That(scanner.HasPendingBytes(), Is.True, $"{literal} split {split}");
                    Assert.That(scanner.IsComplete(), Is.False, $"{literal} split {split}");
                    Assert.That(scanner.CopyRawBytes(), Is.EqualTo(bytes[..split]), $"{literal} split {split}");
                });

                for (int index = split; index < bytes.Length; index++)
                {
                    bool expectedComplete = index == bytes.Length - 1;
                    Assert.That(scanner.PushByte(bytes[index]), Is.EqualTo(expectedComplete), $"{literal} split {split}");
                }

                Assert.That(scanner.IsComplete(), Is.True, $"{literal} split {split}");
            }
        }
    }

    [Test]
    public void ScannerRejectsInvalidCasingAndMisspellingsImmediately()
    {
        string[] invalidLiterals = { "T", "F", "N", "tRue", "falsE", "nulL", "truX", "faX", "nuX" };

        foreach (string literal in invalidLiterals)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(literal);
            JsonLiteralScanner scanner = new JsonLiteralScanner();
            JsonParseException? caught = null;

            foreach (byte value in bytes)
            {
                if (caught == null)
                {
                    caught = CatchLiteralFailure(scanner, value);
                }
            }

            JsonParseException exception = caught
                ?? throw new InvalidOperationException($"Expected JsonParseException for {literal}.");
            Assert.Multiple(() =>
            {
                Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidLiteral), literal);
                Assert.That(exception.Diagnostic.SourceExcerpt, Is.EqualTo(bytes[..exception.Diagnostic.SourceExcerpt!.Length]), literal);
                Assert.That(scanner.HasPendingBytes(), Is.False, literal);
                Assert.That(scanner.IsComplete(), Is.False, literal);
            });
        }
    }

    [Test]
    public void ScannerRejectsEveryTruncatedLiteralPrefix()
    {
        string[] literals = { "true", "false", "null" };

        foreach (string literal in literals)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(literal);
            for (int length = 1; length < bytes.Length; length++)
            {
                JsonLiteralScanner scanner = new JsonLiteralScanner();
                for (int index = 0; index < length; index++)
                {
                    scanner.PushByte(bytes[index]);
                }

                JsonParseException exception = Assert.Throws<JsonParseException>(() => scanner.CompleteLiteral())
                    ?? throw new InvalidOperationException("Expected JsonParseException.");
                Assert.Multiple(() =>
                {
                    Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidLiteral), $"{literal} length {length}");
                    Assert.That(exception.Diagnostic.SourceExcerpt, Is.EqualTo(bytes[..length]), $"{literal} length {length}");
                    Assert.That(scanner.HasPendingBytes(), Is.False, $"{literal} length {length}");
                });
            }
        }
    }

    [Test]
    public void ScannerCanBeResetAndReusedAfterSuccessOrFailure()
    {
        JsonLiteralScanner scanner = ScanLiteral("true");

        scanner.ResetScanner();

        Assert.Multiple(() =>
        {
            Assert.That(scanner.IsComplete(), Is.False);
            Assert.That(scanner.HasPendingBytes(), Is.False);
            Assert.That(scanner.GetTokenKind(), Is.EqualTo(JsonTokenKind.None));
            Assert.That(scanner.GetRawByteCount(), Is.Zero);
            Assert.That(scanner.CopyRawBytes(), Is.Empty);
        });

        Assert.Throws<JsonParseException>(() => scanner.PushByte(0x54));
        byte[] nullBytes = System.Text.Encoding.ASCII.GetBytes("null");
        foreach (byte value in nullBytes)
        {
            scanner.PushByte(value);
        }

        Assert.Multiple(() =>
        {
            Assert.That(scanner.IsComplete(), Is.True);
            Assert.That(scanner.GetTokenKind(), Is.EqualTo(JsonTokenKind.Null));
            Assert.That(scanner.CopyRawBytes(), Is.EqualTo(nullBytes));
        });
    }

    static JsonLiteralScanner ScanLiteral(string literal)
    {
        JsonLiteralScanner scanner = new JsonLiteralScanner();
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(literal);
        for (int index = 0; index < bytes.Length; index++)
        {
            bool expectedComplete = index == bytes.Length - 1;
            Assert.That(scanner.PushByte(bytes[index]), Is.EqualTo(expectedComplete), literal);
        }

        return scanner;
    }

    static JsonParseException? CatchLiteralFailure(JsonLiteralScanner scanner, byte value)
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
