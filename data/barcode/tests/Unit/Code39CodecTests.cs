namespace barcode.Tests;

public class Code39CodecTests
{
    [Test]
    public void StandardEncodingProducesValuesAndFramedPatterns()
    {
        string text = "CODE 39-.$/+%";

        Code39EncodedData encoded = Code39Encoder.Encode(text, Code39CharacterMode.Standard);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Text, Is.EqualTo(text));
            Assert.That(encoded.EncodedText, Is.EqualTo(text));
            Assert.That(encoded.Values, Is.EqualTo(new int[]
            {
                12, 24, 13, 14, 38, 3, 9, 36, 37, 39, 40, 41, 42
            }));
            Assert.That(encoded.PatternBits, Has.Length.EqualTo(text.Length + 2));
            Assert.That(encoded.PatternBits[0], Is.EqualTo(0x094));
            Assert.That(encoded.PatternBits[1], Is.EqualTo(0x148));
            Assert.That(encoded.PatternBits[^2], Is.EqualTo(0x02A));
            Assert.That(encoded.PatternBits[^1], Is.EqualTo(0x094));
            Assert.That(encoded.CharacterMode, Is.EqualTo(Code39CharacterMode.Standard));
        });
    }

    [Test]
    public void StandardPatternsRoundTripWithoutInterpretingShiftCharacters()
    {
        string text = "ABC$%/+ 123";
        Code39EncodedData encoded = Code39Encoder.Encode(text, Code39CharacterMode.Standard);

        Code39DecodedData decoded = Code39Decoder.DecodePatterns(
            encoded.PatternBits,
            Code39CharacterMode.Standard);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Text, Is.EqualTo(text));
            Assert.That(decoded.EncodedText, Is.EqualTo(text));
            Assert.That(decoded.Values, Is.EqualTo(encoded.Values));
            Assert.That(decoded.PatternBits, Is.EqualTo(encoded.PatternBits));
            Assert.That(decoded.PatternBits, Is.Not.SameAs(encoded.PatternBits));
            Assert.That(decoded.CharacterMode, Is.EqualTo(Code39CharacterMode.Standard));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("lower")]
    [TestCase("ABC*")]
    [TestCase("ABC_")]
    [TestCase("ABC\0")]
    [TestCase("CAFÉ")]
    public void StandardEncodingRejectsTextOutsideTheBaseAlphabet(string? text)
    {
        Assert.That(
            () => Code39Encoder.Encode(text!, Code39CharacterMode.Standard),
            Throws.TypeOf<BarcodeEncodingException>());
    }

    [Test]
    public void FullAsciiRoundTripsEverySevenBitCharacter()
    {
        char[] characters = new char[128];
        int value = 0;
        while (value < characters.Length)
        {
            characters[value] = (char)value;
            value++;
        }

        string text = new string(characters);
        Code39EncodedData encoded = Code39Encoder.Encode(text, Code39CharacterMode.FullAscii);
        Code39DecodedData decoded = Code39Decoder.DecodePatterns(
            encoded.PatternBits,
            Code39CharacterMode.FullAscii);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Text, Is.EqualTo(text));
            Assert.That(decoded.EncodedText, Is.EqualTo(encoded.EncodedText));
            Assert.That(decoded.Values, Is.EqualTo(encoded.Values));
            Assert.That(encoded.EncodedText, Is.Not.Empty);
            Assert.That(encoded.EncodedText.All(Code39Patterns.IsStandardDataCharacter), Is.True);
            Assert.That(encoded.EncodedText, Does.Not.Contain("*"));
        });
    }

    [TestCase(0, "%U")]
    [TestCase(1, "$A")]
    [TestCase(26, "$Z")]
    [TestCase(27, "%A")]
    [TestCase(31, "%E")]
    [TestCase(32, " ")]
    [TestCase(33, "/A")]
    [TestCase(36, "/D")]
    [TestCase(37, "/E")]
    [TestCase(43, "/K")]
    [TestCase(45, "-")]
    [TestCase(46, ".")]
    [TestCase(47, "/O")]
    [TestCase(48, "0")]
    [TestCase(57, "9")]
    [TestCase(58, "/Z")]
    [TestCase(59, "%F")]
    [TestCase(63, "%J")]
    [TestCase(64, "%V")]
    [TestCase(65, "A")]
    [TestCase(90, "Z")]
    [TestCase(91, "%K")]
    [TestCase(95, "%O")]
    [TestCase(96, "%W")]
    [TestCase(97, "+A")]
    [TestCase(122, "+Z")]
    [TestCase(123, "%P")]
    [TestCase(127, "%T")]
    public void FullAsciiUsesCanonicalShiftAssignments(int asciiValue, string expectedEncoded)
    {
        string text = new string((char)asciiValue, 1);

        Code39EncodedData encoded = Code39Encoder.Encode(text, Code39CharacterMode.FullAscii);
        Code39DecodedData decoded = Code39Decoder.DecodeDataCharacters(
            expectedEncoded,
            Code39CharacterMode.FullAscii);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.EncodedText, Is.EqualTo(expectedEncoded));
            Assert.That(decoded.Text, Is.EqualTo(text));
            Assert.That(decoded.EncodedText, Is.EqualTo(expectedEncoded));
        });
    }

    [Test]
    public void FullAsciiLiteralShiftCharactersUseUnambiguousSlashPairs()
    {
        Code39EncodedData encoded = Code39Encoder.Encode("$%/+", Code39CharacterMode.FullAscii);
        Code39DecodedData decoded = Code39Decoder.DecodeDataCharacters(
            encoded.EncodedText,
            Code39CharacterMode.FullAscii);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.EncodedText, Is.EqualTo("/D/E/O/K"));
            Assert.That(decoded.Text, Is.EqualTo("$%/+"));
        });
    }

    [Test]
    public void FullAsciiEncodingRejectsMissingEmptyAndNonAsciiText()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => Code39Encoder.Encode(null!, Code39CharacterMode.FullAscii),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Code39Encoder.Encode("", Code39CharacterMode.FullAscii),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Code39Encoder.Encode("é", Code39CharacterMode.FullAscii),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Code39Encoder.Encode("\u0080", Code39CharacterMode.FullAscii),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [TestCase("$")]
    [TestCase("%")]
    [TestCase("/")]
    [TestCase("+")]
    [TestCase("ABC+")]
    [TestCase("$0")]
    [TestCase("%X")]
    [TestCase("/P")]
    [TestCase("+0")]
    [TestCase("++")]
    public void FullAsciiDecoderStrictlyRejectsMalformedShiftSequences(string encodedText)
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                Code39Decoder.TryDecodeDataCharacters(encodedText, Code39CharacterMode.FullAscii),
                Is.Null);
            Assert.That(
                () => Code39Decoder.DecodeDataCharacters(encodedText, Code39CharacterMode.FullAscii),
                Throws.TypeOf<BarcodeDecodingException>()
                    .With.Message.EqualTo("Code 39 data characters are malformed."));
        });
    }

    [Test]
    public void PatternDecoderRejectsMissingFramingInternalAsteriskAndUnknownPatterns()
    {
        int startStop = Code39Patterns.StartStopPatternBits;
        int a = Code39Patterns.GetCharacterPatternBits('A');
        int b = Code39Patterns.GetCharacterPatternBits('B');

        Assert.Multiple(() =>
        {
            Assert.That(Code39Decoder.TryDecodePatterns(new int[0], Code39CharacterMode.Standard), Is.Null);
            Assert.That(Code39Decoder.TryDecodePatterns(new int[] { startStop, startStop }, Code39CharacterMode.Standard), Is.Null);
            Assert.That(Code39Decoder.TryDecodePatterns(new int[] { a, b, startStop }, Code39CharacterMode.Standard), Is.Null);
            Assert.That(Code39Decoder.TryDecodePatterns(new int[] { startStop, a, b }, Code39CharacterMode.Standard), Is.Null);
            Assert.That(Code39Decoder.TryDecodePatterns(new int[] { startStop, startStop, startStop }, Code39CharacterMode.Standard), Is.Null);
            Assert.That(Code39Decoder.TryDecodePatterns(new int[] { startStop, 0, startStop }, Code39CharacterMode.Standard), Is.Null);
            Assert.That(Code39Decoder.TryDecodePatterns(new int[] { startStop, 0x200, startStop }, Code39CharacterMode.Standard), Is.Null);
            Assert.That(
                () => Code39Decoder.DecodePatterns(new int[] { startStop, 0, startStop }, Code39CharacterMode.Standard),
                Throws.TypeOf<BarcodeDecodingException>());
        });
    }

    [Test]
    public void DataCharacterDecoderRejectsMissingEmptyAndNonAlphabetInput()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => Code39Decoder.TryDecodeDataCharacters(null!, Code39CharacterMode.Standard),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(Code39Decoder.TryDecodeDataCharacters("", Code39CharacterMode.Standard), Is.Null);
            Assert.That(Code39Decoder.TryDecodeDataCharacters("abc", Code39CharacterMode.Standard), Is.Null);
            Assert.That(Code39Decoder.TryDecodeDataCharacters("ABC*", Code39CharacterMode.Standard), Is.Null);
            Assert.That(Code39Decoder.TryDecodeDataCharacters("ABC_", Code39CharacterMode.Standard), Is.Null);
            Assert.That(
                () => Code39Decoder.DecodeDataCharacters("", Code39CharacterMode.Standard),
                Throws.TypeOf<BarcodeDecodingException>());
        });
    }

    [Test]
    public void EncodedAndDecodedArraysAreIndependent()
    {
        Code39EncodedData encoded = Code39Encoder.Encode("ABC", Code39CharacterMode.Standard);
        Code39DecodedData decoded = Code39Decoder.DecodePatterns(
            encoded.PatternBits,
            Code39CharacterMode.Standard);
        decoded.Values[0] = 42;
        decoded.PatternBits[1] = 0;

        Code39EncodedData later = Code39Encoder.Encode("ABC", Code39CharacterMode.Standard);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Values[0], Is.EqualTo(10));
            Assert.That(encoded.PatternBits[1], Is.EqualTo(0x109));
            Assert.That(later.Values, Is.EqualTo(new int[] { 10, 11, 12 }));
            Assert.That(later.PatternBits[1], Is.EqualTo(0x109));
        });
    }

    [Test]
    public void CodecRejectsUnknownCharacterModesAndNullPatternInput()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => Code39Encoder.Encode("ABC", (Code39CharacterMode)99),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Code39Decoder.TryDecodePatterns(null!, Code39CharacterMode.Standard),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Code39Decoder.TryDecodePatterns(new int[0], (Code39CharacterMode)99),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Code39Decoder.TryDecodeDataCharacters("ABC", (Code39CharacterMode)99),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }
}
