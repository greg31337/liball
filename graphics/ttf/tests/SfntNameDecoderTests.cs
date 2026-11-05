namespace ttf.Tests;

internal class SfntNameDecoderTests
{
    [Test]
    public void DecodesUnicodeAndWindowsUtf16BigEndianNames()
    {
        SfntDecodedName unicode = Decode(
            0,
            4,
            0,
            SfntNameEncodingKind.Utf16BigEndian,
            new byte[] { 0, 0x41, 0xD8, 0x3D, 0xDE, 0 });
        SfntDecodedName windows = Decode(
            3,
            1,
            0x0409,
            SfntNameEncodingKind.Utf16BigEndian,
            new byte[] { 0, 0x57, 0, 0x69, 0, 0x6E });

        Assert.Multiple(() =>
        {
            Assert.That(unicode.GetValue(), Is.EqualTo("A\U0001F600"));
            Assert.That(unicode.IsValid(), Is.True);
            Assert.That(unicode.IsEncodingSupported(), Is.True);
            Assert.That(unicode.HasReplacementCharacters(), Is.False);
            Assert.That(unicode.GetReplacementCount(), Is.Zero);
            Assert.That(unicode.GetDiagnosticCount(), Is.Zero);
            Assert.That(windows.GetValue(), Is.EqualTo("Win"));
            Assert.That(windows.IsValid(), Is.True);
        });
    }

    [TestCaseSource(nameof(MalformedUtf16Names))]
    public void ReplacesMalformedUtf16Sequences(
        byte[] bytes,
        string expected,
        int replacementCount)
    {
        SfntDecodedName decoded = Decode(
            3,
            10,
            0x0409,
            SfntNameEncodingKind.Utf16BigEndian,
            bytes);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.GetValue(), Is.EqualTo(expected));
            Assert.That(decoded.IsValid(), Is.False);
            Assert.That(decoded.IsEncodingSupported(), Is.True);
            Assert.That(decoded.HasReplacementCharacters(), Is.True);
            Assert.That(decoded.GetReplacementCount(), Is.EqualTo(replacementCount));
            Assert.That(decoded.GetDiagnosticCount(), Is.EqualTo(2));
            Assert.That(decoded.GetDiagnosticCode(0),
                Is.EqualTo(FontDiagnosticCode.InvalidNameEncoding));
            Assert.That(decoded.GetDiagnosticCode(1),
                Is.EqualTo(FontDiagnosticCode.StringDecodingReplacement));
        });
    }

    [Test]
    public void DecodesCompleteMacintoshRomanMapping()
    {
        byte[] bytes = new byte[256];

        for (int index = 0; index < bytes.Length; index += 1)
        {
            bytes[index] = (byte)index;
        }

        SfntDecodedName decoded = Decode(
            1,
            0,
            0,
            SfntNameEncodingKind.MacintoshRoman,
            bytes);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.GetValue().Length, Is.EqualTo(256));
            Assert.That(decoded.GetValue()[0x41], Is.EqualTo('A'));
            Assert.That(decoded.GetValue()[0x80], Is.EqualTo('\u00C4'));
            Assert.That(decoded.GetValue()[0x8E], Is.EqualTo('\u00E9'));
            Assert.That(decoded.GetValue()[0xDB], Is.EqualTo('\u20AC'));
            Assert.That(decoded.GetValue()[0xF0], Is.EqualTo('\uF8FF'));
            Assert.That(decoded.GetValue()[0xFF], Is.EqualTo('\u02C7'));
            Assert.That(decoded.IsValid(), Is.True);
        });
    }

    [TestCase(936, SfntNameEncodingKind.WindowsCodePage936,
        new byte[] { 0xD6, 0xD0 }, "\u4E2D")]
    [TestCase(932, SfntNameEncodingKind.WindowsCodePage932,
        new byte[] { 0x82, 0xA0 }, "\u3042")]
    [TestCase(950, SfntNameEncodingKind.WindowsCodePage950,
        new byte[] { 0xA4, 0x40 }, "\u4E00")]
    [TestCase(949, SfntNameEncodingKind.WindowsCodePage949,
        new byte[] { 0xB0, 0xA1 }, "\uAC00")]
    public void DecodesLegacyWindowsCodePages(
        int codePage,
        SfntNameEncodingKind kind,
        byte[] bytes,
        string expected)
    {
        int encodingId = codePage == 932 ? 2 :
            codePage == 936 ? 3 : codePage == 950 ? 4 : 5;
        SfntDecodedName decoded = Decode(3, encodingId, 0, kind, bytes);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.GetValue(), Is.EqualTo(expected));
            Assert.That(decoded.IsEncodingSupported(), Is.True);
            Assert.That(decoded.IsValid(), Is.True);
            Assert.That(decoded.GetDiagnosticCount(), Is.Zero);
        });
    }

    [Test]
    public void UsesReplacementFallbackForInvalidLegacySequences()
    {
        SfntDecodedName decoded = Decode(
            3,
            3,
            0x0804,
            SfntNameEncodingKind.WindowsCodePage936,
            new byte[] { 0x41, 0x81 });

        Assert.Multiple(() =>
        {
            Assert.That(decoded.GetValue(), Is.EqualTo("A\uFFFD"));
            Assert.That(decoded.GetReplacementCount(), Is.EqualTo(1));
            Assert.That(decoded.GetDiagnosticCode(0),
                Is.EqualTo(FontDiagnosticCode.InvalidNameEncoding));
            Assert.That(decoded.GetDiagnosticCode(1),
                Is.EqualTo(FontDiagnosticCode.StringDecodingReplacement));
        });
    }

    [Test]
    public void DecodesMacintoshScriptAndLanguageSpecificVariants()
    {
        SfntDecodedName cyrillic = Decode(
            1,
            7,
            32,
            SfntNameEncodingKind.MacintoshLegacy,
            new byte[] { 0x80 });
        SfntDecodedName icelandic = Decode(
            1,
            0,
            15,
            SfntNameEncodingKind.MacintoshRoman,
            new byte[] { 0xA0 });
        SfntDecodedName centralEuropean = Decode(
            1,
            0,
            24,
            SfntNameEncodingKind.MacintoshRoman,
            new byte[] { 0x81 });

        Assert.Multiple(() =>
        {
            Assert.That(cyrillic.GetValue(), Is.EqualTo("\u0410"));
            Assert.That(cyrillic.IsValid(), Is.True);
            Assert.That(icelandic.GetValue(), Is.EqualTo("\u00DD"));
            Assert.That(icelandic.IsValid(), Is.True);
            Assert.That(centralEuropean.GetValue(), Is.EqualTo("\u0100"));
            Assert.That(centralEuropean.IsValid(), Is.True);
        });
    }

    [Test]
    public void ReportsUnsupportedAndEmptyNamesWithoutInventingText()
    {
        SfntNameRecord unsupportedRecord = CreateRecord(
            1,
            9,
            0,
            SfntNameEncodingKind.MacintoshLegacy,
            new byte[] { 0x80 });
        SfntDecodedName unsupportedMacintosh = SfntNameDecoder.Decode(
            unsupportedRecord);
        SfntDecodedName unknown = Decode(
            2,
            0,
            0,
            SfntNameEncodingKind.Unknown,
            new byte[] { 0x41 });
        SfntDecodedName empty = Decode(
            0,
            4,
            0,
            SfntNameEncodingKind.Utf16BigEndian,
            Array.Empty<byte>());

        Assert.Multiple(() =>
        {
            Assert.That(unsupportedMacintosh.GetValue(), Is.Empty);
            Assert.That(unsupportedMacintosh.IsEncodingSupported(), Is.False);
            Assert.That(unsupportedRecord.HasSupportedEncoding(), Is.False);
            Assert.That(unsupportedMacintosh.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(unsupportedMacintosh.GetDiagnosticCode(0),
                Is.EqualTo(FontDiagnosticCode.InvalidNameEncoding));
            Assert.That(unknown.IsEncodingSupported(), Is.False);
            Assert.That(unknown.GetDiagnosticCode(0),
                Is.EqualTo(FontDiagnosticCode.InvalidNameEncoding));
            Assert.That(empty.IsEncodingSupported(), Is.True);
            Assert.That(empty.IsValid(), Is.False);
            Assert.That(empty.GetDiagnosticCode(0),
                Is.EqualTo(FontDiagnosticCode.InvalidNameEncoding));
        });
    }

    [Test]
    public void NameTableCachesDecodedResultsAndChecksIndexes()
    {
        SfntNameRecord record = CreateRecord(
            3,
            1,
            0x0409,
            SfntNameEncodingKind.Utf16BigEndian,
            new byte[] { 0, 0x41 });
        SfntNameTable table = new SfntNameTable(
            0,
            18,
            2,
            new SfntNameRecord[] { record },
            Array.Empty<SfntLanguageTagRecord>(),
            0,
            0,
            0);

        SfntDecodedName first = table.GetDecodedName(0);
        SfntDecodedName second = table.GetDecodedName(0);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetValue(), Is.EqualTo("A"));
            Assert.That(second, Is.SameAs(first));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetDecodedName(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetDecodedName(1));
        });
    }

    [Test]
    public void DecodingDoesNotRegisterTheCodePageProviderGlobally()
    {
        bool availableBefore = IsCodePageGloballyAvailable(936);

        SfntDecodedName decoded = Decode(
            3,
            3,
            0x0804,
            SfntNameEncodingKind.WindowsCodePage936,
            new byte[] { 0xD6, 0xD0 });

        bool availableAfter = IsCodePageGloballyAvailable(936);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.GetValue(), Is.EqualTo("\u4E2D"));
            Assert.That(availableAfter, Is.EqualTo(availableBefore));
        });
    }

    [Test]
    public void DecoderAndDiagnosticAccessorsRejectInvalidArguments()
    {
        SfntDecodedName decoded = Decode(
            3,
            3,
            0,
            SfntNameEncodingKind.WindowsCodePage936,
            new byte[] { 0x81 });

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => SfntNameDecoder.Decode(null!));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SfntDecodedName("value", true, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => decoded.GetDiagnosticCode(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => decoded.GetDiagnosticCode(2));
        });
    }

    static IEnumerable<TestCaseData> MalformedUtf16Names()
    {
        yield return new TestCaseData(
            new byte[] { 0, 0x41, 0 },
            "A\uFFFD",
            1);
        yield return new TestCaseData(
            new byte[] { 0xD8, 0, 0, 0x41 },
            "\uFFFDA",
            1);
        yield return new TestCaseData(
            new byte[] { 0xDC, 0, 0xD8, 0 },
            "\uFFFD\uFFFD",
            2);
    }

    static SfntDecodedName Decode(
        int platformId,
        int encodingId,
        int languageId,
        SfntNameEncodingKind kind,
        byte[] bytes)
    {
        return SfntNameDecoder.Decode(CreateRecord(
            platformId,
            encodingId,
            languageId,
            kind,
            bytes));
    }

    static SfntNameRecord CreateRecord(
        int platformId,
        int encodingId,
        int languageId,
        SfntNameEncodingKind kind,
        byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new SfntNameRecord(
            platformId,
            encodingId,
            languageId,
            1,
            bytes.Length,
            0,
            kind,
            false,
            true,
            bytes.Length != 0,
            false,
            data.GetSlice(0, bytes.Length));
    }

    static bool IsCodePageGloballyAvailable(int codePage)
    {
        try
        {
            System.Text.Encoding.GetEncoding(codePage);
            return true;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }
}
