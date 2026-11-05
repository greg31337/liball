using websocket;

namespace websocket.Tests;

public class DeflateHuffmanTests
{
    [Test]
    public void BuilderAssignsCanonicalCodesFromRfcExample()
    {
        int[] lengths = [3, 3, 3, 3, 3, 2, 4, 4];

        var result = DeflateHuffmanBuilder.BuildTable(
            lengths,
            15,
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Table.CodeLengths, Is.EqualTo(lengths));
            Assert.That(result.Table.Codes,
                Is.EqualTo(new[] { 2, 3, 4, 5, 6, 0, 14, 15 }));
            Assert.That(result.Table.MaximumCodeLength, Is.EqualTo(4));
        });
    }

    [Test]
    public void BuilderRejectsOversubscribedAndIncompleteTrees()
    {
        var oversubscribed = DeflateHuffmanBuilder.BuildTable(
            [1, 1, 1],
            15,
            false,
            true);
        var incomplete = DeflateHuffmanBuilder.BuildTable(
            [2, 2],
            15,
            false,
            true);
        var invalidLength = DeflateHuffmanBuilder.BuildTable(
            [16],
            15,
            false,
            true);
        var permittedSingle = DeflateHuffmanBuilder.BuildTable(
            [1, 0],
            15,
            false,
            true);
        var permittedEmpty = DeflateHuffmanBuilder.BuildTable(
            [0],
            15,
            true,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(oversubscribed.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(incomplete.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(invalidLength.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(permittedSingle.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(permittedEmpty.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(permittedEmpty.Table.MaximumCodeLength, Is.Zero);
        });
    }

    [Test]
    public void FixedTablesContainRfcCodeRangesAndEndOfBlock()
    {
        var literals = DeflateHuffmanTables.CreateFixedLiteralLengthTable();
        var distances = DeflateHuffmanTables.CreateFixedDistanceTable();

        Assert.Multiple(() =>
        {
            Assert.That(literals.CodeLengths[0], Is.EqualTo(8));
            Assert.That(literals.Codes[0], Is.EqualTo(48));
            Assert.That(literals.Codes[143], Is.EqualTo(191));
            Assert.That(literals.CodeLengths[144], Is.EqualTo(9));
            Assert.That(literals.Codes[144], Is.EqualTo(400));
            Assert.That(literals.CodeLengths[256], Is.EqualTo(7));
            Assert.That(literals.Codes[256], Is.Zero);
            Assert.That(literals.CodeLengths[280], Is.EqualTo(8));
            Assert.That(literals.Codes[280], Is.EqualTo(192));
            Assert.That(distances.CodeLengths, Is.All.EqualTo(5));
            Assert.That(distances.Codes[31], Is.EqualTo(31));
        });
    }

    [Test]
    public void FixedDecoderReadsLiteralAndEndOfBlockAcrossChunks()
    {
        var table = DeflateHuffmanTables.CreateFixedLiteralLengthTable();
        var writer = new DeflateBitWriter();
        var encoded = new byte[2];
        var firstWrite = writer.WriteHuffmanCode(
            table.Codes[65],
            table.CodeLengths[65],
            encoded);
        var secondWrite = writer.WriteHuffmanCode(
            table.Codes[256],
            table.CodeLengths[256],
            encoded.AsSpan(firstWrite.ProducedCount));
        var finish = writer.FinishWriterByte(
            encoded.AsSpan(firstWrite.ProducedCount + secondWrite.ProducedCount));
        int encodedCount = firstWrite.ProducedCount +
            secondWrite.ProducedCount +
            finish.ProducedCount;
        var reader = new DeflateBitReader();
        var decoder = new DeflateHuffmanDecoder(table);

        var literal = decoder.ReadSymbol(reader, encoded.AsSpan(0, 1));
        var incomplete = decoder.ReadSymbol(reader, []);
        var end = decoder.ReadSymbol(
            reader,
            encoded.AsSpan(1, encodedCount - 1));

        Assert.Multiple(() =>
        {
            Assert.That(literal.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(literal.Symbol, Is.EqualTo(65));
            Assert.That(incomplete.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(end.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(end.Symbol, Is.EqualTo(256));
        });
    }

    [Test]
    public void DecoderRejectsAPathMissingFromSingleSymbolTree()
    {
        var table = DeflateHuffmanBuilder.BuildTable(
            [1, 0],
            15,
            false,
            true).Table;
        var decoder = new DeflateHuffmanDecoder(table);
        var reader = new DeflateBitReader();

        var invalid = decoder.ReadSymbol(reader, new byte[] { 1 });
        var repeated = decoder.ReadSymbol(reader, []);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(repeated.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
        });
    }
}
