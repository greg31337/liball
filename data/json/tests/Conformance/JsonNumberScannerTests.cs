namespace json.Tests;

public class JsonNumberScannerTests
{
    [Test]
    public void ScannerPreservesAndClassifiesValidNumberForms()
    {
        (string Text, JsonNumberKind Kind, bool Negative, bool NegativeZero)[] cases =
        {
            ("0", JsonNumberKind.Integer, false, false),
            ("-0", JsonNumberKind.Integer, true, true),
            ("7", JsonNumberKind.Integer, false, false),
            ("-42", JsonNumberKind.Integer, true, false),
            ("1234567890", JsonNumberKind.Integer, false, false),
            ("0.0", JsonNumberKind.Fraction, false, false),
            ("-0.00", JsonNumberKind.Fraction, true, true),
            ("12.340", JsonNumberKind.Fraction, false, false),
            ("1e0", JsonNumberKind.Exponent, false, false),
            ("-0E+99", JsonNumberKind.Exponent, true, true),
            ("6E-10", JsonNumberKind.Exponent, false, false),
            ("-0.00e-2", JsonNumberKind.FractionWithExponent, true, true),
            ("1.25E+8", JsonNumberKind.FractionWithExponent, false, false)
        };

        foreach ((string text, JsonNumberKind kind, bool negative, bool negativeZero) in cases)
        {
            JsonNumberScanner scanner = ScanNumber(text);
            JsonNumber number = scanner.CreateNumber();

            Assert.Multiple(() =>
            {
                Assert.That(scanner.IsComplete(), Is.True, text);
                Assert.That(scanner.HasPendingBytes(), Is.False, text);
                Assert.That(scanner.CopyLexeme(), Is.EqualTo(text), text);
                Assert.That(scanner.CopyRawBytes(), Is.EqualTo(System.Text.Encoding.ASCII.GetBytes(text)), text);
                Assert.That(scanner.GetNumberKind(), Is.EqualTo(kind), text);
                Assert.That(scanner.IsNegative(), Is.EqualTo(negative), text);
                Assert.That(scanner.HasFraction(), Is.EqualTo(kind == JsonNumberKind.Fraction || kind == JsonNumberKind.FractionWithExponent), text);
                Assert.That(scanner.HasExponent(), Is.EqualTo(kind == JsonNumberKind.Exponent || kind == JsonNumberKind.FractionWithExponent), text);
                Assert.That(scanner.IsNegativeZero(), Is.EqualTo(negativeZero), text);
                Assert.That(number.Lexeme, Is.EqualTo(text), text);
                Assert.That(number.Kind, Is.EqualTo(kind), text);
                Assert.That(number.IsNegative, Is.EqualTo(negative), text);
                Assert.That(number.HasFraction, Is.EqualTo(scanner.HasFraction()), text);
                Assert.That(number.HasExponent, Is.EqualTo(scanner.HasExponent()), text);
                Assert.That(number.IsNegativeZero, Is.EqualTo(negativeZero), text);
            });
        }
    }

    [Test]
    public void ScannerResumesAtEverySplitAndReportsValidStoppingPrefixes()
    {
        string text = "-12.30E-4";
        bool[] canCompleteAfterByte = { false, true, true, false, true, true, false, false, true };
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(text);

        for (int split = 1; split < bytes.Length; split++)
        {
            JsonNumberScanner scanner = new JsonNumberScanner();
            for (int index = 0; index < split; index++)
            {
                Assert.That(scanner.PushByte(bytes[index]), Is.EqualTo(canCompleteAfterByte[index]), $"split {split}, byte {index}");
            }

            Assert.Multiple(() =>
            {
                Assert.That(scanner.HasPendingBytes(), Is.True, $"split {split}");
                Assert.That(scanner.CanCompleteNumber(), Is.EqualTo(canCompleteAfterByte[split - 1]), $"split {split}");
                Assert.That(scanner.CopyRawBytes(), Is.EqualTo(bytes[..split]), $"split {split}");
            });

            for (int index = split; index < bytes.Length; index++)
            {
                Assert.That(scanner.PushByte(bytes[index]), Is.EqualTo(canCompleteAfterByte[index]), $"split {split}, byte {index}");
            }

            scanner.CompleteNumber();
            Assert.Multiple(() =>
            {
                Assert.That(scanner.IsComplete(), Is.True, $"split {split}");
                Assert.That(scanner.CopyLexeme(), Is.EqualTo(text), $"split {split}");
                Assert.That(scanner.GetNumberKind(), Is.EqualTo(JsonNumberKind.FractionWithExponent), $"split {split}");
            });
        }
    }

    [Test]
    public void ScannerRejectsInvalidGrammarTransitionsImmediately()
    {
        string[] invalidCases =
        {
            "+1",
            ".1",
            "01",
            "-x",
            "--1",
            "1.a",
            "1..0",
            "1eX",
            "1e+X",
            "1e-X",
            "1e1.0",
            "1_0",
            "1x"
        };

        foreach (string text in invalidCases)
        {
            JsonNumberScanner scanner = new JsonNumberScanner();
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(text);
            JsonParseException? caught = null;
            foreach (byte value in bytes)
            {
                if (caught == null)
                {
                    caught = CatchNumberFailure(scanner, value);
                }
            }

            JsonParseException exception = caught
                ?? throw new InvalidOperationException($"Expected JsonParseException for {text}.");
            Assert.Multiple(() =>
            {
                Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidNumber), text);
                Assert.That(exception.Diagnostic.SourceExcerpt, Is.EqualTo(bytes[..exception.Diagnostic.SourceExcerpt!.Length]), text);
                Assert.That(scanner.HasPendingBytes(), Is.False, text);
                Assert.That(scanner.GetRawByteCount(), Is.Zero, text);
            });
        }
    }

    [Test]
    public void ScannerRejectsEveryIncompleteGrammarStateAtFinalInput()
    {
        string[] incompleteCases = { "", "-", "0.", "1.", "0e", "1E", "1e+", "1e-" };

        foreach (string text in incompleteCases)
        {
            JsonNumberScanner scanner = new JsonNumberScanner();
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(text);
            foreach (byte value in bytes)
            {
                scanner.PushByte(value);
            }

            JsonParseException exception = Assert.Throws<JsonParseException>(() => scanner.CompleteNumber())
                ?? throw new InvalidOperationException("Expected JsonParseException.");
            Assert.Multiple(() =>
            {
                Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidNumber), text);
                Assert.That(exception.Diagnostic.SourceExcerpt, Is.EqualTo(bytes), text);
                Assert.That(scanner.HasPendingBytes(), Is.False, text);
                Assert.That(scanner.CopyRawBytes(), Is.Empty, text);
            });
        }
    }

    [Test]
    public void ScannerGrowsForArbitraryLengthLexemesWithoutChangingSpelling()
    {
        string text = "-" + new string('9', 80) + "." + new string('0', 40) + "e+" + new string('7', 30);
        JsonNumberScanner scanner = ScanNumber(text);
        JsonNumber number = scanner.CreateNumber();

        Assert.Multiple(() =>
        {
            Assert.That(scanner.GetRawByteCount(), Is.EqualTo(text.Length));
            Assert.That(scanner.CopyLexeme(), Is.EqualTo(text));
            Assert.That(number.Lexeme, Is.EqualTo(text));
            Assert.That(number.Kind, Is.EqualTo(JsonNumberKind.FractionWithExponent));
            Assert.That(number.IsNegative, Is.True);
            Assert.That(number.IsNegativeZero, Is.False);
        });
    }

    [Test]
    public void ScannerFinalizationIsIdempotentAndResetAllowsReuse()
    {
        JsonNumberScanner scanner = ScanNumber("-0e-1");

        scanner.CompleteNumber();
        scanner.CompleteNumber();
        Assert.That(scanner.IsNegativeZero(), Is.True);

        JsonParseException afterComplete = Assert.Throws<JsonParseException>(() => scanner.PushByte(0x31))
            ?? throw new InvalidOperationException("Expected JsonParseException.");
        Assert.That(afterComplete.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidNumber));

        foreach (byte value in System.Text.Encoding.ASCII.GetBytes("42"))
        {
            scanner.PushByte(value);
        }

        scanner.ResetScanner();
        Assert.Multiple(() =>
        {
            Assert.That(scanner.GetRawByteCount(), Is.Zero);
            Assert.That(scanner.CopyLexeme(), Is.Empty);
            Assert.That(scanner.CanCompleteNumber(), Is.False);
            Assert.That(scanner.IsComplete(), Is.False);
            Assert.That(scanner.IsNegative(), Is.False);
            Assert.That(scanner.HasFraction(), Is.False);
            Assert.That(scanner.HasExponent(), Is.False);
            Assert.That(scanner.IsNegativeZero(), Is.False);
        });

        JsonNumber reused = ScanIntoExisting(scanner, "42");
        Assert.Multiple(() =>
        {
            Assert.That(reused.Lexeme, Is.EqualTo("42"));
            Assert.That(reused.Kind, Is.EqualTo(JsonNumberKind.Integer));
        });
    }

    static JsonNumberScanner ScanNumber(string text)
    {
        JsonNumberScanner scanner = new JsonNumberScanner();
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(text);
        foreach (byte value in bytes)
        {
            scanner.PushByte(value);
        }

        scanner.CompleteNumber();
        return scanner;
    }

    static JsonNumber ScanIntoExisting(JsonNumberScanner scanner, string text)
    {
        foreach (byte value in System.Text.Encoding.ASCII.GetBytes(text))
        {
            scanner.PushByte(value);
        }

        return scanner.CreateNumber();
    }

    static JsonParseException? CatchNumberFailure(JsonNumberScanner scanner, byte value)
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
