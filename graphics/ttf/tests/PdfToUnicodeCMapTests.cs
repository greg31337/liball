using ttf;

namespace ttf.Tests;

internal class PdfToUnicodeCMapTests
{
    [Test]
    public void WritesSortedBfCharAndSequentialBfRangeMappings()
    {
        PdfToUnicodeMapping supplementary = Mapping(7, 0x1F600);
        int[] copiedSequence = supplementary.GetUnicodeSequence();
        copiedSequence[0] = 0;

        PdfToUnicodeCMap cmap = PdfToUnicodeCMapWriter.Build(
            1,
            new PdfToUnicodeMapping[]
            {
                supplementary,
                Mapping(2, 0x42),
                Mapping(4, 0x66, 0x69),
                Mapping(1, 0x41)
            });
        string text = cmap.GetText();
        byte[] bytes = cmap.GetBytes();
        bytes[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(cmap.GetCodeByteCount(), Is.EqualTo(1));
            Assert.That(cmap.GetCodeSpaceMinimum(), Is.Zero);
            Assert.That(cmap.GetCodeSpaceMaximum(), Is.EqualTo(255));
            Assert.That(cmap.GetMappingCount(), Is.EqualTo(4));
            Assert.That(cmap.GetBfCharBlockCount(), Is.EqualTo(1));
            Assert.That(cmap.GetBfRangeBlockCount(), Is.EqualTo(1));
            Assert.That(text, Does.Contain(
                "1 begincodespacerange\n<00> <FF>\nendcodespacerange"));
            Assert.That(text, Does.Contain(
                "2 beginbfchar\n<04> <00660069>\n<07> <D83DDE00>\nendbfchar"));
            Assert.That(text, Does.Contain(
                "1 beginbfrange\n<01> <02> <0041>\nendbfrange"));
            Assert.That(text, Does.Not.Contain("FEFF"));
            Assert.That(text, Does.Not.Contain("\r"));
            Assert.That(text, Does.EndWith(
                "CMapName currentdict /CMap defineresource pop\nend\nend\n"));
            Assert.That(System.Text.Encoding.ASCII.GetString(cmap.GetBytes()),
                Is.EqualTo(text));
            Assert.That(cmap.GetBytes()[0], Is.EqualTo((byte)'/'));
            Assert.That(supplementary.GetUnicodeScalar(0),
                Is.EqualTo(0x1F600));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                supplementary.GetUnicodeScalar(1));
        });
    }

    [Test]
    public void WritesArrayRangesForSequencesAndSupplementaryScalars()
    {
        PdfToUnicodeCMap sequences = PdfToUnicodeCMapWriter.Build(
            2,
            new PdfToUnicodeMapping[]
            {
                Mapping(0x0101, 0x1F600),
                Mapping(0x0100, 0x66, 0x69)
            });
        PdfToUnicodeCMap supplementary = PdfToUnicodeCMapWriter.Build(
            2,
            new PdfToUnicodeMapping[]
            {
                Mapping(0x0200, 0x1F600),
                Mapping(0x0201, 0x1F601)
            });

        Assert.Multiple(() =>
        {
            Assert.That(sequences.GetCodeSpaceMaximum(),
                Is.EqualTo(65535));
            Assert.That(sequences.GetText(), Does.Contain(
                "<0100> <0101> [<00660069> <D83DDE00>]"));
            Assert.That(supplementary.GetText(), Does.Contain(
                "<0200> <0201> [<D83DDE00> <D83DDE01>]"));
            Assert.That(supplementary.GetText(),
                Does.Not.Contain("<0200> <0201> <D83DDE00>"));
        });
    }

    [Test]
    public void ChunksEveryBlockAndRangeAtOneHundredEntries()
    {
        PdfToUnicodeMapping[] isolated = new PdfToUnicodeMapping[205];
        int index = 0;

        while (index < isolated.Length)
        {
            isolated[index] = Mapping(index * 2, 0x1000 + index);
            index += 1;
        }

        PdfToUnicodeCMap bfchar = PdfToUnicodeCMapWriter.Build(2, isolated);
        PdfToUnicodeMapping[] paired = new PdfToUnicodeMapping[202];
        index = 0;

        while (index < 101)
        {
            int code = index * 3;
            paired[index * 2] = Mapping(code, 0x41, index);
            paired[index * 2 + 1] = Mapping(code + 1, 0x42, index);
            index += 1;
        }

        PdfToUnicodeCMap bfrange = PdfToUnicodeCMapWriter.Build(2, paired);
        PdfToUnicodeMapping[] consecutive = new PdfToUnicodeMapping[205];
        index = 0;

        while (index < consecutive.Length)
        {
            consecutive[index] = Mapping(index, 0x400 + index);
            index += 1;
        }

        PdfToUnicodeCMap splitRanges = PdfToUnicodeCMapWriter.Build(
            2,
            consecutive);

        Assert.Multiple(() =>
        {
            Assert.That(bfchar.GetBfCharBlockCount(), Is.EqualTo(3));
            Assert.That(Count(bfchar.GetText(), "100 beginbfchar"),
                Is.EqualTo(2));
            Assert.That(Count(bfchar.GetText(), "5 beginbfchar"),
                Is.EqualTo(1));
            Assert.That(bfrange.GetBfRangeBlockCount(), Is.EqualTo(2));
            Assert.That(Count(bfrange.GetText(), "100 beginbfrange"),
                Is.EqualTo(1));
            Assert.That(Count(bfrange.GetText(), "1 beginbfrange"),
                Is.EqualTo(1));
            Assert.That(splitRanges.GetText(),
                Does.Contain("3 beginbfrange"));
            Assert.That(splitRanges.GetText(),
                Does.Contain("<0000> <0063> <0400>"));
            Assert.That(splitRanges.GetText(),
                Does.Contain("<0064> <00C7> <0464>"));
            Assert.That(splitRanges.GetText(),
                Does.Contain("<00C8> <00CC> <04C8>"));
        });
    }

    [Test]
    public void ProducesIdenticalOutputRegardlessOfInputOrder()
    {
        PdfToUnicodeMapping first = Mapping(1, 0x41);
        PdfToUnicodeMapping second = Mapping(2, 0x42);
        PdfToUnicodeMapping third = Mapping(5, 0x1F642);

        string ordered = PdfToUnicodeCMapWriter.Build(
            2,
            new PdfToUnicodeMapping[] { first, second, third }).GetText();
        string shuffled = PdfToUnicodeCMapWriter.Build(
            2,
            new PdfToUnicodeMapping[] { third, first, second }).GetText();

        Assert.That(shuffled, Is.EqualTo(ordered));
    }

    [Test]
    public void ValidatesMappingsCodesAndEmptyCmaps()
    {
        PdfToUnicodeCMap empty = PdfToUnicodeCMapWriter.Build(
            1,
            Array.Empty<PdfToUnicodeMapping>());

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetMappingCount(), Is.Zero);
            Assert.That(empty.GetBfCharBlockCount(), Is.Zero);
            Assert.That(empty.GetBfRangeBlockCount(), Is.Zero);
            Assert.That(empty.GetText(), Does.Contain("begincmap"));
            Assert.That(empty.GetText(), Does.Not.Contain("beginbfchar"));
            Assert.That(empty.GetText(), Does.Not.Contain("beginbfrange"));
            Assert.Throws<ArgumentNullException>(() =>
                new PdfToUnicodeMapping(0, null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PdfToUnicodeMapping(-1, new int[] { 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PdfToUnicodeMapping(65536, new int[] { 0 }));
            Assert.Throws<ArgumentException>(() =>
                new PdfToUnicodeMapping(0, Array.Empty<int>()));
            Assert.Throws<ArgumentException>(() =>
                new PdfToUnicodeMapping(0, new int[] { 0xD800 }));
            Assert.Throws<ArgumentException>(() =>
                new PdfToUnicodeMapping(0, new int[] { 0x110000 }));
            Assert.Throws<ArgumentNullException>(() =>
                PdfToUnicodeCMapWriter.Build(1, null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfToUnicodeCMapWriter.Build(
                    0,
                    Array.Empty<PdfToUnicodeMapping>()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfToUnicodeCMapWriter.Build(
                    1,
                    new PdfToUnicodeMapping[] { Mapping(256, 0x41) }));
            Assert.Throws<ArgumentException>(() =>
                PdfToUnicodeCMapWriter.Build(
                    2,
                    new PdfToUnicodeMapping[] { Mapping(1, 0x41), null! }));
            Assert.Throws<ArgumentException>(() =>
                PdfToUnicodeCMapWriter.Build(
                    2,
                    new PdfToUnicodeMapping[]
                    {
                        Mapping(1, 0x41),
                        Mapping(1, 0x42)
                    }));
        });
    }

    static PdfToUnicodeMapping Mapping(
        int code,
        params int[] unicodeSequence)
    {
        return new PdfToUnicodeMapping(code, unicodeSequence);
    }

    static int Count(string text, string value)
    {
        int count = 0;
        int offset = 0;

        while (offset < text.Length)
        {
            int found = text.IndexOf(
                value,
                offset,
                StringComparison.Ordinal);

            if (found < 0) { return count; }
            count += 1;
            offset = found + value.Length;
        }

        return count;
    }
}
