
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlByteDecoderTests
{
    [Test]
    public void DecodedInputRejectsUnrepresentableMappingLengths()
    {
        Assert.Throws<ArgumentException>(() => new HtmlDecodedInput(true,
            "utf-8", "", int.MaxValue, new int[] { 0 }, new int[0],
            new HtmlDecodingError[0]));

        HtmlDecodedInputBuilder builder = new HtmlDecodedInputBuilder(
            "utf-8", 0, 0);
        Assert.Throws<InsufficientMemoryException>(() =>
            builder.EnsureCharacterCapacity(int.MaxValue));
    }

    [Test]
    public void Utf8DecodesEverySequenceLengthAndMapsByteBoundaries()
    {
        byte[] bytes =
        [
            0x41,
            0xC2, 0xA2,
            0xE2, 0x82, 0xAC,
            0xF0, 0x9F, 0x92, 0xA9,
            0x5A,
        ];

        HtmlDecodedInput result = HtmlByteDecoder.Decode(bytes, "utf8");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSupported(), Is.True);
            Assert.That(result.GetEncodingName(), Is.EqualTo("UTF-8"));
            Assert.That(result.GetString(), Is.EqualTo("A¢€💩Z"));
            Assert.That(result.GetCharacterCount(), Is.EqualTo(6));
            Assert.That(result.GetByteCount(), Is.EqualTo(11));
            Assert.That(result.HadErrors(), Is.False);
            Assert.That(result.GetByteOffset(0), Is.EqualTo(0));
            Assert.That(result.GetByteOffset(1), Is.EqualTo(1));
            Assert.That(result.GetByteOffset(2), Is.EqualTo(3));
            Assert.That(result.GetByteOffset(3), Is.EqualTo(6));
            Assert.That(result.GetByteOffset(4), Is.EqualTo(6));
            Assert.That(result.GetByteOffset(5), Is.EqualTo(10));
            Assert.That(result.GetByteOffset(6), Is.EqualTo(11));
            Assert.That(result.GetCharacterOffset(0), Is.EqualTo(0));
            Assert.That(result.GetCharacterOffset(2), Is.EqualTo(1));
            Assert.That(result.GetCharacterOffset(5), Is.EqualTo(2));
            Assert.That(result.GetCharacterOffset(9), Is.EqualTo(3));
            Assert.That(result.GetCharacterOffset(10), Is.EqualTo(5));
            Assert.That(result.GetCharacterOffset(11), Is.EqualTo(6));
        });
    }

    [Test]
    public void Utf8UsesReplacementForEachWhatwgDecoderError()
    {
        byte[] bytes = [0xE1, 0x80, 0x41, 0xC0, 0xAF, 0xF0, 0x9F];

        HtmlDecodedInput result = HtmlByteDecoder.Decode(bytes, "UTF-8");

        Assert.That(result.GetString(), Is.EqualTo("�A���"));
        AssertErrors(result, new[] { (0, 2), (3, 1), (4, 1), (5, 2) });
        Assert.That(result.GetCharacterOffset(1), Is.Zero);
        Assert.That(result.GetCharacterOffset(2), Is.EqualTo(1));
        Assert.That(result.GetByteOffset(1), Is.EqualTo(2));
        Assert.That(result.GetByteOffset(5), Is.EqualTo(7));
    }

    [TestCase(new byte[] { 0xE0, 0x9F, 0x80 }, "���")]
    [TestCase(new byte[] { 0xED, 0xA0, 0x80 }, "���")]
    [TestCase(new byte[] { 0xF4, 0x90, 0x80, 0x80 }, "����")]
    [TestCase(new byte[] { 0xF5, 0x80, 0x80, 0x80 }, "����")]
    [TestCase(new byte[] { 0xC2 }, "�")]
    public void Utf8RejectsOverlongSurrogateOutOfRangeAndIncompleteSequences(byte[] bytes, string expected)
    {
        HtmlDecodedInput result = HtmlByteDecoder.Decode(bytes, "utf-8");

        Assert.That(result.GetString(), Is.EqualTo(expected));
        Assert.That(result.GetErrorCount(), Is.EqualTo(expected.Length));
    }

    [TestCase(false, "utf-16")]
    [TestCase(false, "utf-16le")]
    [TestCase(true, "utf-16be")]
    public void Utf16DecodesBmpAndSurrogatePairs(bool bigEndian, string label)
    {
        byte[] littleEndian = [0x41, 0x00, 0x3D, 0xD8, 0xA9, 0xDC];
        byte[] bigEndianBytes = [0x00, 0x41, 0xD8, 0x3D, 0xDC, 0xA9];
        byte[] bytes = bigEndian ? bigEndianBytes : littleEndian;

        HtmlDecodedInput result = HtmlByteDecoder.Decode(bytes, label);

        Assert.That(result.GetString(), Is.EqualTo("A💩"));
        Assert.That(result.GetEncodingName(), Is.EqualTo(bigEndian ? "UTF-16BE" : "UTF-16LE"));
        Assert.That(result.GetByteOffset(0), Is.Zero);
        Assert.That(result.GetByteOffset(1), Is.EqualTo(2));
        Assert.That(result.GetByteOffset(2), Is.EqualTo(2));
        Assert.That(result.GetByteOffset(3), Is.EqualTo(6));
        Assert.That(result.GetCharacterOffset(5), Is.EqualTo(1));
        Assert.That(result.GetCharacterOffset(6), Is.EqualTo(3));
        Assert.That(result.HadErrors(), Is.False);
    }

    [Test]
    public void Utf16ReplacesUnpairedSurrogatesAndOddFinalByte()
    {
        byte[] bytes = [0x00, 0xD8, 0x41, 0x00, 0x00, 0xDC, 0x42];

        HtmlDecodedInput result = HtmlByteDecoder.Decode(bytes, "utf-16le");

        Assert.That(result.GetString(), Is.EqualTo("�A��"));
        AssertErrors(result, new[] { (0, 2), (4, 2), (6, 1) });
    }

    [Test]
    public void Utf16IncompleteSurrogateConsumesItsOddTrailingByteInOneError()
    {
        HtmlDecodedInput result = HtmlByteDecoder.Decode(new byte[] { 0x00, 0xD8, 0x41 }, "utf-16le");

        Assert.That(result.GetString(), Is.EqualTo("�"));
        AssertErrors(result, new[] { (0, 3) });
    }

    [Test]
    public void Windows1252DecodesTheCompleteControlRangeAndReportsUndefinedBytes()
    {
        byte[] bytes = Enumerable.Range(0x80, 0x20).Select(value => (byte)value).ToArray();
        const string expected = "€�‚ƒ„…†‡ˆ‰Š‹Œ�Ž��‘’“”•–—˜™š›œ�žŸ";

        HtmlDecodedInput result = HtmlByteDecoder.Decode(bytes, "windows-1252");

        Assert.That(result.GetString(), Is.EqualTo(expected));
        AssertErrors(result, new[] { (1, 1), (13, 1), (15, 1), (16, 1), (29, 1) });
        Assert.That(result.GetByteOffset(32), Is.EqualTo(32));
        Assert.That(result.GetCharacterOffset(31), Is.EqualTo(31));
    }

    [TestCase("ascii")]
    [TestCase("us-ascii")]
    [TestCase("iso-8859-1")]
    [TestCase("latin1")]
    public void AsciiAndIso88591WebLabelsUseWindows1252(string label)
    {
        HtmlDecodedInput result = HtmlByteDecoder.Decode(new byte[] { 0x41, 0x80, 0xE9 }, label);

        Assert.That(result.GetEncodingName(), Is.EqualTo("windows-1252"));
        Assert.That(result.GetString(), Is.EqualTo("A€é"));
        Assert.That(result.HadErrors(), Is.False);
    }

    [Test]
    public void BomOverridesTheLabelIsRemovedAndRemainsInTheOffsetMap()
    {
        byte[] bytes = [0xFF, 0xFE, 0x41, 0x00];

        HtmlDecodedInput result = HtmlByteDecoder.Decode(bytes, "utf-8");

        Assert.That(result.GetEncodingName(), Is.EqualTo("UTF-16LE"));
        Assert.That(result.GetString(), Is.EqualTo("A"));
        Assert.That(result.GetByteOffset(0), Is.EqualTo(2));
        Assert.That(result.GetByteOffset(1), Is.EqualTo(4));
        Assert.That(result.GetCharacterOffset(0), Is.Zero);
        Assert.That(result.GetCharacterOffset(1), Is.Zero);
        Assert.That(result.GetCharacterOffset(2), Is.Zero);
        Assert.That(result.GetCharacterOffset(4), Is.EqualTo(1));
    }

    [Test]
    public void DetectionResultCanBeReusedForDecoding()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, 0xE2, 0x82, 0xAC];
        HtmlEncodingDetectionResult detection = HtmlEncodingDetector.Detect(bytes, "windows-1252");

        HtmlDecodedInput result = HtmlByteDecoder.Decode(bytes, detection);

        Assert.That(result.GetEncodingName(), Is.EqualTo("UTF-8"));
        Assert.That(result.GetString(), Is.EqualTo("€"));
        Assert.That(result.GetByteOffset(0), Is.EqualTo(3));
        Assert.That(result.GetByteOffset(1), Is.EqualTo(6));
    }

    [Test]
    public void DefaultDecodeUsesTheHtmlEncodingDetector()
    {
        byte[] declaration = "<meta charset=utf-8>"u8.ToArray();
        byte[] bytes = new byte[declaration.Length + 3];
        declaration.CopyTo(bytes, 0);
        new byte[] { 0xE2, 0x82, 0xAC }.CopyTo(bytes, declaration.Length);

        HtmlDecodedInput result = HtmlByteDecoder.Decode(bytes);

        Assert.That(result.GetEncodingName(), Is.EqualTo("UTF-8"));
        Assert.That(result.GetString(), Is.EqualTo("<meta charset=utf-8>€"));
    }

    [Test]
    public void UnsupportedLabelsFailWhileLegacyLabelsUseTheirImplementedDecoder()
    {
        HtmlDecodedInput invalid = HtmlByteDecoder.Decode(new byte[] { 0x41 }, "not-an-encoding");
        HtmlDecodedInput deferred = HtmlByteDecoder.Decode(new byte[] { 0x41 }, "shift_jis");

        Assert.That(invalid.IsSupported(), Is.False);
        Assert.That(invalid.GetEncodingName(), Is.Empty);
        Assert.That(invalid.GetString(), Is.Empty);
        Assert.That(deferred.IsSupported(), Is.True);
        Assert.That(deferred.GetEncodingName(), Is.EqualTo("Shift_JIS"));
        Assert.That(deferred.GetString(), Is.EqualTo("A"));
        Assert.That(deferred.GetErrorCount(), Is.Zero);
    }

    [Test]
    public void EmptyInputAndResultSnapshotsAreStable()
    {
        HtmlDecodedInput empty = HtmlByteDecoder.Decode(ReadOnlySpan<byte>.Empty, "utf-8");
        HtmlDecodedInput malformed = HtmlByteDecoder.Decode(new byte[] { 0xFF }, "utf-8");
        HtmlDecodingError[] snapshot = malformed.GetErrors();
        snapshot[0] = new HtmlDecodingError(10, 1);

        Assert.That(empty.GetString(), Is.Empty);
        Assert.That(empty.GetByteOffset(0), Is.Zero);
        Assert.That(empty.GetCharacterOffset(0), Is.Zero);
        Assert.That(malformed.GetErrors()[0].GetByteOffset(), Is.Zero);
        Assert.That(malformed.GetErrors()[0].GetCode(), Is.EqualTo(HtmlDiagnosticCode.EncodingInvalidSequence));
    }

    [Test]
    public void ValidatesInvalidArgumentsAndMappingOffsets()
    {
        HtmlDecodedInput result = HtmlByteDecoder.Decode(new byte[] { 0x41 }, "utf-8");
        HtmlEncodingDetectionResult impossible = new HtmlEncodingDetectionResult(
            "UTF-8", HtmlEncodingConfidence.Certain, HtmlEncodingSource.Bom, 2, -1);

        Assert.Throws<ArgumentNullException>(() => HtmlByteDecoder.Decode(ReadOnlySpan<byte>.Empty, (string)null));
        Assert.Throws<ArgumentNullException>(() => HtmlByteDecoder.Decode(ReadOnlySpan<byte>.Empty, (HtmlEncodingDetectionResult)null));
        Assert.Throws<ArgumentException>(() => HtmlByteDecoder.Decode(new byte[] { 0xEF }, impossible));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetByteOffset(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetByteOffset(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetCharacterOffset(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetCharacterOffset(2));
    }

    static void AssertErrors(HtmlDecodedInput result, (int Offset, int Length)[] expected)
    {
        HtmlDecodingError[] errors = result.GetErrors();
        Assert.That(errors, Has.Length.EqualTo(expected.Length));
        for (int index = 0; index < errors.Length; index++)
        {
            Assert.That(errors[index].GetByteOffset(), Is.EqualTo(expected[index].Offset));
            Assert.That(errors[index].GetByteLength(), Is.EqualTo(expected[index].Length));
        }
    }
}
