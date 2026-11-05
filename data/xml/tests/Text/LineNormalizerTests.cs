using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class LineNormalizerTests
{
    [Test]
    public void Xml10NormalizesCrAndCrLfButPreservesNelAndLineSeparator()
    {
        XmlDecodedInput decoded = DecodeCharacters("A\r\nB\rC\nD\u0085E\u2028F", XmlEncodingEntityKind.Document);

        XmlLineNormalizationResult result = LineNormalizer.NormalizeXmlLineEndings(decoded, XmlVersion.Xml10, XmlEncodingEntityKind.Document);

        Assert.That(result.Text, Is.EqualTo("A\nB\nC\nD\u0085E\u2028F"));
        AssertLocations(result,
            [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1],
            [1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 4, 4],
            [1, 2, 1, 2, 1, 2, 1, 2, 3, 4, 5, 6]);
    }

    [Test]
    public void Xml11NormalizesEveryRequiredLineEndingSequence()
    {
        string raw = "A\r\nB\r\u0085C\rD\u0085E\u2028F\nG";
        string expected = "A\nB\nC\nD\nE\nF\nG";
        XmlDecodedInput document = DecodeCharacters(raw, XmlEncodingEntityKind.Document);
        XmlDecodedInput external = DecodeCharacters(raw, XmlEncodingEntityKind.ExternalParsedEntity);

        XmlLineNormalizationResult documentResult = LineNormalizer.NormalizeXmlLineEndings(document, XmlVersion.Xml11, XmlEncodingEntityKind.Document);
        XmlLineNormalizationResult externalResult = LineNormalizer.NormalizeXmlLineEndings(external, XmlVersion.Xml11, XmlEncodingEntityKind.ExternalParsedEntity);

        Assert.That(documentResult.Text, Is.EqualTo(expected));
        Assert.That(externalResult.Text, Is.EqualTo(expected));
        Assert.That(documentResult.Lines, Is.EqualTo(new int[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7 }));
        Assert.That(documentResult.Columns, Is.EqualTo(new int[] { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 }));
    }

    [Test]
    public void Utf8ByteLocationsIncludeBomAndCollapsedRawSequences()
    {
        string raw = "A\r\n\u00A2\r" + char.ConvertFromUtf32(0x1F600);
        Encoding encoding = new UTF8Encoding(true, true);
        XmlDecodedInput decoded = DecodeBytes(JoinBytes(encoding.GetPreamble(), encoding.GetBytes(raw)), null);

        Assert.That(decoded.RawByteOffsets, Is.EqualTo(new long[] { 3, 4, 5, 6, 8, 9, 9, 13 }));

        XmlLineNormalizationResult result = LineNormalizer.NormalizeXmlLineEndings(decoded, XmlVersion.Xml10, XmlEncodingEntityKind.Document);

        Assert.That(result.Text, Is.EqualTo("A\n\u00A2\n" + char.ConvertFromUtf32(0x1F600)));
        AssertLocations(result,
            [3, 4, 6, 8, 9, 9, 13],
            [1, 1, 2, 2, 3, 3, 3],
            [1, 2, 1, 2, 1, 1, 2]);
    }

    [Test]
    public void Utf16ByteLocationsAdvancePastCollapsedCrNel()
    {
        string raw = "A\r\u0085B";
        Encoding encoding = new UnicodeEncoding(true, true, true);
        XmlDecodedInput decoded = DecodeBytes(JoinBytes(encoding.GetPreamble(), encoding.GetBytes(raw)), null);

        Assert.That(decoded.RawByteOffsets, Is.EqualTo(new long[] { 2, 4, 6, 8, 10 }));

        XmlLineNormalizationResult result = LineNormalizer.NormalizeXmlLineEndings(decoded, XmlVersion.Xml11, XmlEncodingEntityKind.Document);

        Assert.That(result.Text, Is.EqualTo("A\nB"));
        AssertLocations(result,
            [2, 4, 8, 10],
            [1, 1, 2, 2],
            [1, 2, 1, 2]);
    }

    [Test]
    public void SourceLocationsUseNormalizedOffsetsAndScalarColumns()
    {
        string raw = char.ConvertFromUtf32(0x1F600) + "X\rY";
        XmlDecodedInput decoded = DecodeCharacters(raw, XmlEncodingEntityKind.Document);
        XmlLineNormalizationResult result = LineNormalizer.NormalizeXmlLineEndings(decoded, XmlVersion.Xml10, XmlEncodingEntityKind.Document);

        Assert.That(result.Text, Is.EqualTo(char.ConvertFromUtf32(0x1F600) + "X\nY"));
        AssertLocations(result,
            [-1, -1, -1, -1, -1, -1],
            [1, 1, 1, 1, 2, 2],
            [1, 1, 2, 3, 1, 2]);

        SourceLocation location = result.GetNormalizedSourceLocation(4, "urn:source");
        Assert.That(location.SourceUri, Is.EqualTo("urn:source"));
        Assert.That(location.CharacterOffset, Is.EqualTo(4));
        Assert.That(location.ByteOffset, Is.EqualTo(-1));
        Assert.That(location.Line, Is.EqualTo(2));
        Assert.That(location.Column, Is.EqualTo(1));
    }

    [Test]
    public void Xml11RejectsNelAndLineSeparatorInsideDeclarationsBeforeNormalization()
    {
        XmlDecodedInput nel = DecodeCharacters("<?xml version='1.1'\u0085?><r/>", XmlEncodingEntityKind.Document);
        XmlDecodedInput lineSeparator = DecodeCharacters("<?xml version='1.1'\u2028?><r/>", XmlEncodingEntityKind.Document);
        XmlDecodedInput afterDeclaration = DecodeCharacters("<?xml version='1.1'?>\u0085<r/>", XmlEncodingEntityKind.Document);

        Assert.That(() => LineNormalizer.NormalizeXmlLineEndings(nel, XmlVersion.Xml11, XmlEncodingEntityKind.Document), Throws.TypeOf<FormatException>().With.Message.Contains("declaration"));
        Assert.That(() => LineNormalizer.NormalizeXmlLineEndings(lineSeparator, XmlVersion.Xml11, XmlEncodingEntityKind.Document), Throws.TypeOf<FormatException>().With.Message.Contains("declaration"));
        Assert.That(LineNormalizer.NormalizeXmlLineEndings(afterDeclaration, XmlVersion.Xml11, XmlEncodingEntityKind.Document).Text, Is.EqualTo("<?xml version='1.1'?>\n<r/>"));
    }

    [Test]
    public void EmptyInputAndInvalidRequestsHaveDeterministicLocations()
    {
        XmlDecodedInput empty = DecodeCharacters(string.Empty, XmlEncodingEntityKind.Document);
        XmlLineNormalizationResult result = LineNormalizer.NormalizeXmlLineEndings(empty, XmlVersion.Xml10, XmlEncodingEntityKind.Document);

        Assert.That(result.Text, Is.Empty);
        AssertLocations(result, [-1], [1], [1]);
        Assert.That(() => result.GetNormalizedSourceLocation(-1, null), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => result.GetNormalizedSourceLocation(1, null), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LineNormalizer.NormalizeXmlLineEndings(empty, (XmlVersion)2, XmlEncodingEntityKind.Document), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LineNormalizer.NormalizeXmlLineEndings(empty, XmlVersion.Xml10, (XmlEncodingEntityKind)2), Throws.TypeOf<ArgumentOutOfRangeException>());

        empty.RawByteOffsets = [];
        Assert.That(() => LineNormalizer.NormalizeXmlLineEndings(empty, XmlVersion.Xml10, XmlEncodingEntityKind.Document), Throws.ArgumentException);
    }

    private static XmlDecodedInput DecodeCharacters(string text, XmlEncodingEntityKind entityKind)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = text;
        return new XmlInputDecoder(input, entityKind, EncodingPolicyKind.UnicodeOnly, 4).DecodeXmlInput();
    }

    private static XmlDecodedInput DecodeBytes(byte[] bytes, string? encodingName)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.ByteMemory;
        input.Bytes = bytes;
        input.EncodingName = encodingName;
        return new XmlInputDecoder(input, XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, 3).DecodeXmlInput();
    }

    private static byte[] JoinBytes(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, 0, result, 0, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }

    private static void AssertLocations(XmlLineNormalizationResult result, long[] byteOffsets, int[] lines, int[] columns)
    {
        Assert.That(result.RawByteOffsets, Is.EqualTo(byteOffsets));
        Assert.That(result.Lines, Is.EqualTo(lines));
        Assert.That(result.Columns, Is.EqualTo(columns));
        Assert.That(byteOffsets.Length, Is.EqualTo(result.Text.Length + 1));
        int offset = 0;
        while (offset < byteOffsets.Length)
        {
            SourceLocation location = result.GetNormalizedSourceLocation(offset, null);
            Assert.That(location.CharacterOffset, Is.EqualTo(offset), "offset " + offset);
            Assert.That(location.ByteOffset, Is.EqualTo(byteOffsets[offset]), "offset " + offset);
            Assert.That(location.Line, Is.EqualTo(lines[offset]), "offset " + offset);
            Assert.That(location.Column, Is.EqualTo(columns[offset]), "offset " + offset);
            offset = offset + 1;
        }
    }
}
