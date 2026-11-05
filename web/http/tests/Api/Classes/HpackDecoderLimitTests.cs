using System.Text;

namespace http.Tests;

public class HpackDecoderLimitTests
{
    [Test]
    public void CompressedBlockLimitRejectsBeforeChangingDecoderState()
    {
        HpackDecoder decoder = new HpackDecoder();
        HpackOptions options = new HpackOptions();
        options.MaximumCompressedBlockSize = 100;
        options.Limits.MaximumCompressedBlockSize = 1;

        HpackResult result = decoder.DecodeHeaderBlock(
            new byte[] { 0x82, 0x84 }, options);
        HpackResult state = decoder.InspectDecoderTable();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.Consumed, Is.Zero);
            Assert.That(result.ErrorPosition, Is.EqualTo(1));
            Assert.That(result.IsCompressionError, Is.False);
            Assert.That(result.ErrorCode,
                Is.EqualTo("hpack-compressed-block-limit-exceeded"));
            Assert.That(state.DynamicTable, Is.Empty);
        });
    }

    [Test]
    public void HeaderCountLimitDecodesTheWholeBlockAndRetainsTableState()
    {
        HpackDecoder decoder = new HpackDecoder();
        HpackOptions limited = new HpackOptions();
        limited.MaximumHeaderCount = 1;
        byte[] block =
        [
            0x40, 0x01, (byte)'a', 0x01, (byte)'1',
            0x40, 0x01, (byte)'b', 0x01, (byte)'2'
        ];

        HpackResult result = decoder.DecodeHeaderBlock(block, limited);
        HpackResult later = decoder.DecodeHeaderBlock(
            new byte[] { 0xbe }, new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.Consumed, Is.EqualTo(block.Length));
            Assert.That(result.Headers.Fields, Has.Count.EqualTo(1));
            Assert.That(result.HeaderListSize, Is.EqualTo(68));
            Assert.That(result.DynamicTable, Has.Count.EqualTo(2));
            Assert.That(result.IsCompressionError, Is.False);
            Assert.That(result.ErrorCode,
                Is.EqualTo("hpack-header-count-limit-exceeded"));
            Assert.That(later.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(later.Headers.Fields.Single().Name, Is.EqualTo("b"));
            Assert.That(later.Headers.Fields.Single().Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("2")));
        });
    }

    [Test]
    public void HeaderListSizeUsesRfcAccountingAndAcceptsTheExactBoundary()
    {
        HpackOptions exactOptions = new HpackOptions();
        exactOptions.MaximumHeaderListSize = 80;
        HpackOptions limitedOptions = new HpackOptions();
        limitedOptions.MaximumHeaderListSize = 79;

        HpackResult exact = new HpackDecoder().DecodeHeaderBlock(
            new byte[] { 0x82, 0x84 }, exactOptions);
        HpackResult limited = new HpackDecoder().DecodeHeaderBlock(
            new byte[] { 0x82, 0x84 }, limitedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(exact.Headers.Fields, Has.Count.EqualTo(2));
            Assert.That(exact.HeaderListSize, Is.EqualTo(80));
            Assert.That(limited.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(limited.Consumed, Is.EqualTo(2));
            Assert.That(limited.Headers.Fields, Has.Count.EqualTo(1));
            Assert.That(limited.HeaderListSize, Is.EqualTo(80));
            Assert.That(limited.IsCompressionError, Is.False);
            Assert.That(limited.ErrorCode,
                Is.EqualTo("hpack-header-list-size-limit-exceeded"));
        });
    }

    [Test]
    public void RawAndHuffmanStringsUseTheDecodedStringLimit()
    {
        HpackOptions options = new HpackOptions();
        options.MaximumStringSize = 5;
        byte[] raw =
        [
            0x04, 0x06, (byte)'a', (byte)'b', (byte)'c',
            (byte)'d', (byte)'e', (byte)'f'
        ];
        HpackHuffmanResult encoded = HpackHuffmanCodec.Encode("aaaaaa"u8);
        List<byte> huffman = new List<byte>();
        huffman.Add(0x04);
        huffman.Add((byte)(0x80 | encoded.Bytes.Length));
        huffman.AddRange(encoded.Bytes);

        HpackResult rawResult = new HpackDecoder().DecodeHeaderBlock(
            raw, options);
        HpackResult huffmanResult = new HpackDecoder().DecodeHeaderBlock(
            huffman.ToArray(), options);

        Assert.Multiple(() =>
        {
            Assert.That(rawResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rawResult.IsCompressionError, Is.False);
            Assert.That(rawResult.ErrorCode,
                Is.EqualTo("hpack-string-limit-exceeded"));
            Assert.That(huffmanResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(huffmanResult.IsCompressionError, Is.False);
            Assert.That(huffmanResult.ErrorCode,
                Is.EqualTo("hpack-string-limit-exceeded"));
        });
    }

    [Test]
    public void MalformedIntegerRemainsACompressionError()
    {
        byte[] excessive = new byte[11];
        excessive[0] = 0xff;
        int index = 1;
        while (index < 10)
        {
            excessive[index] = 0x80;
            index = index + 1;
        }

        HpackResult result = new HpackDecoder().DecodeHeaderBlock(
            excessive, new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.IsCompressionError, Is.True);
            Assert.That(result.ErrorCode,
                Is.EqualTo("hpack-integer-length-exceeded"));
        });
    }

    [Test]
    public void IncrementalInsertionCannotExceedTheActiveDynamicTableBound()
    {
        HpackDecoder decoder = new HpackDecoder();
        decoder.UpdateDecoderTableLimit(40);
        byte[] block =
        [
            0x3f, 0x09,
            0x40, 0x04, (byte)'n', (byte)'a', (byte)'m', (byte)'e',
            0x05, (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e'
        ];

        HpackResult result = decoder.DecodeHeaderBlock(
            block, new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.DynamicTableLimit, Is.EqualTo(40));
            Assert.That(result.DynamicTableSize, Is.Zero);
            Assert.That(result.DynamicTable, Is.Empty);
            Assert.That(result.Headers.Fields.Single().Name,
                Is.EqualTo("name"));
        });
    }

    [Test]
    public void InvalidLimitConfigurationIsNotACompressionError()
    {
        HpackOptions options = new HpackOptions();
        options.MaximumHeaderCount = -1;

        HpackResult result = new HpackDecoder().DecodeHeaderBlock(
            Array.Empty<byte>(), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.IsCompressionError, Is.False);
            Assert.That(result.HasErrorPosition, Is.False);
            Assert.That(result.ErrorCode,
                Is.EqualTo("invalid-hpack-decode-limits"));
        });
    }
}
