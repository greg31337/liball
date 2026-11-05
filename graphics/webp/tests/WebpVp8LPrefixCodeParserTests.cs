using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8LPrefixCodeParserTests
{
    [Test]
    public void BuildTable_ConstructsCanonicalAndReversedCodes()
    {
        WebpVp8LPrefixCodeTable table = WebpVp8LPrefixCodeParser.BuildTable(new int[] { 1, 2, 3, 3 });

        Assert.That(table.AlphabetSize, Is.EqualTo(4));
        Assert.That(table.MaxCodeLength, Is.EqualTo(3));
        Assert.That(table.IsSingleSymbol, Is.False);
        AssertEntry(table, 0, 1, 0, 0);
        AssertEntry(table, 1, 2, 2, 1);
        AssertEntry(table, 2, 3, 6, 3);
        AssertEntry(table, 3, 3, 7, 7);
    }

    [Test]
    public void DecodeSymbol_UsesReversedCodesForLsbFirstReader()
    {
        WebpVp8LPrefixCodeTable table = WebpVp8LPrefixCodeParser.BuildTable(new int[] { 1, 2, 3, 3 });
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(0, 1);
        writer.WriteBits(1, 2);
        writer.WriteBits(3, 3);
        writer.WriteBits(7, 3);

        WebpVp8LBitReader reader = new WebpVp8LBitReader(writer.ToArray());

        Assert.That(table.DecodeSymbol(reader), Is.EqualTo(0));
        Assert.That(table.DecodeSymbol(reader), Is.EqualTo(1));
        Assert.That(table.DecodeSymbol(reader), Is.EqualTo(2));
        Assert.That(table.DecodeSymbol(reader), Is.EqualTo(3));
    }

    [Test]
    public void BuildTable_AcceptsSingleSymbolLengthOne()
    {
        WebpVp8LPrefixCodeTable table = WebpVp8LPrefixCodeParser.BuildTable(new int[] { 0, 1, 0 });

        Assert.That(table.IsSingleSymbol, Is.True);
        Assert.That(table.DecodeSymbol(new WebpVp8LBitReader(Array.Empty<byte>())), Is.EqualTo(1));
    }

    [Test]
    public void BuildTable_RejectsEmptyIncompleteAndOversubscribedTrees()
    {
        Assert.Throws<ArgumentException>(() => WebpVp8LPrefixCodeParser.BuildTable(new int[] { 0, 0 }));
        Assert.Throws<ArgumentException>(() => WebpVp8LPrefixCodeParser.BuildTable(new int[] { 2, 0 }));
        Assert.Throws<ArgumentException>(() => WebpVp8LPrefixCodeParser.BuildTable(new int[] { 1, 1, 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LPrefixCodeParser.BuildTable(new int[] { 16 }));
    }

    [Test]
    public void ParseCodeLengths_ReadsSimpleSingleSymbolCode()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 1);
        writer.WriteBits(1, 1);

        WebpVp8LCodeLengthsParseResult result = WebpVp8LPrefixCodeParser.ParseCodeLengths(new WebpVp8LBitReader(writer.ToArray()), 4);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.CodeLengths, Is.EqualTo(new int[] { 0, 1, 0, 0 }));
        Assert.That(result.Table!.IsSingleSymbol, Is.True);
        Assert.That(result.Table.DecodeSymbol(new WebpVp8LBitReader(Array.Empty<byte>())), Is.EqualTo(1));
    }

    [Test]
    public void ParseCodeLengths_ReadsSimpleTwoSymbolCode()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(1, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(200, 8);
        writer.WriteBits(201, 8);

        WebpVp8LCodeLengthsParseResult result = WebpVp8LPrefixCodeParser.ParseCodeLengths(new WebpVp8LBitReader(writer.ToArray()), 256);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.CodeLengths[200], Is.EqualTo(1));
        Assert.That(result.CodeLengths[201], Is.EqualTo(1));
        Assert.That(result.Table!.Entries, Has.Count.EqualTo(2));
    }

    [Test]
    public void ParseCodeLengths_ReadsNormalSingleCodeLengthTree()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        WriteNormalHeaderWithCodeLengthSymbols(writer, false);
        writer.WriteBits(0, 1);

        WebpVp8LCodeLengthsParseResult result = WebpVp8LPrefixCodeParser.ParseCodeLengths(new WebpVp8LBitReader(writer.ToArray()), 2);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.CodeLengths, Is.EqualTo(new int[] { 1, 1 }));
        Assert.That(result.Table!.Entries, Has.Count.EqualTo(2));
    }

    [Test]
    public void ParseCodeLengths_AppliesZeroRepeatCode()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        WriteNormalHeaderWithCodeLengthSymbols(writer, true);
        writer.WriteBits(0, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 3);
        writer.WriteBits(0, 1);

        WebpVp8LCodeLengthsParseResult result = WebpVp8LPrefixCodeParser.ParseCodeLengths(new WebpVp8LBitReader(writer.ToArray()), 4);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.CodeLengths, Is.EqualTo(new int[] { 0, 0, 0, 1 }));
        Assert.That(result.Table!.IsSingleSymbol, Is.True);
    }

    [Test]
    public void ParseCodeLengths_ReportsRepeatThatExceedsMaxSymbol()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 4);
        writer.WriteBits(1, 3);
        writer.WriteBits(0, 3);
        writer.WriteBits(0, 3);
        writer.WriteBits(0, 3);
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 3);

        WebpVp8LCodeLengthsParseResult result = WebpVp8LPrefixCodeParser.ParseCodeLengths(new WebpVp8LBitReader(writer.ToArray()), 2);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("repeat"));
    }

    static void WriteNormalHeaderWithCodeLengthSymbols(WebpBitWriter writer, bool includeRepeatZero)
    {
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 4);
        if (includeRepeatZero)
        {
            writer.WriteBits(1, 3);
        }
        else
        {
            writer.WriteBits(0, 3);
        }

        writer.WriteBits(0, 3);
        writer.WriteBits(0, 3);
        writer.WriteBits(1, 3);
    }

    static void AssertEntry(WebpVp8LPrefixCodeTable table, int symbol, int length, int canonicalCode, int reversedCode)
    {
        WebpVp8LPrefixCodeEntry entry = FindEntry(table, symbol);

        Assert.That(entry.CodeLength, Is.EqualTo(length));
        Assert.That(entry.CanonicalCode, Is.EqualTo(canonicalCode));
        Assert.That(entry.ReversedCode, Is.EqualTo(reversedCode));
    }

    static WebpVp8LPrefixCodeEntry FindEntry(WebpVp8LPrefixCodeTable table, int symbol)
    {
        int index = 0;
        while (index < table.Entries.Count)
        {
            if (table.Entries[index].Symbol == symbol)
            {
                return table.Entries[index];
            }

            index = index + 1;
        }

        throw new ArgumentException("symbol was not present in the prefix table", "symbol");
    }
}
