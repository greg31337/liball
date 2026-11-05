using System.Text;

namespace http.Tests;

public class HpackHeaderBlockCodecTests
{
    [Test]
    public void IndexedFieldsPreserveOrderAndDuplicates()
    {
        HpackDynamicTable table = new HpackDynamicTable(256);

        HpackResult result = DecodeBlock(
            new byte[] { 0x82, 0x84, 0x82 }, table);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(3));
            Assert.That(result.Headers.Fields, Has.Count.EqualTo(3));
            Assert.That(result.Headers.Fields[0].Name, Is.EqualTo(":method"));
            Assert.That(result.Headers.Fields[0].Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("GET")));
            Assert.That(result.Headers.Fields[1].Name, Is.EqualTo(":path"));
            Assert.That(result.Headers.Fields[1].Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("/")));
            Assert.That(result.Headers.Fields[2].Name, Is.EqualTo(":method"));
            Assert.That(result.HeaderListSize, Is.EqualTo(122));
            Assert.That(result.DynamicTable, Is.Empty);
        });
    }

    [Test]
    public void IncrementalLiteralsSupportIndexedAndLiteralNames()
    {
        List<byte> block = new List<byte>();
        block.Add(0x41);
        AddRawString(block, "www.example.com");
        block.Add(0x40);
        AddRawString(block, "custom-key");
        AddRawString(block, "custom-value");
        block.Add(0x0f);
        block.Add(0x2f);
        AddRawString(block, "another");
        block.Add(0xbe);
        HpackDynamicTable table = new HpackDynamicTable(256);

        HpackResult result = DecodeBlock(
            block.ToArray(), table);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Headers.Fields, Has.Count.EqualTo(4));
            Assert.That(result.Headers.Fields[0].Name, Is.EqualTo(":authority"));
            Assert.That(result.Headers.Fields[0].Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("www.example.com")));
            Assert.That(result.Headers.Fields[1].Name, Is.EqualTo("custom-key"));
            Assert.That(result.Headers.Fields[1].Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("custom-value")));
            Assert.That(result.Headers.Fields[2].Name, Is.EqualTo("custom-key"));
            Assert.That(result.Headers.Fields[2].Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("another")));
            Assert.That(result.Headers.Fields[3].Name, Is.EqualTo("custom-key"));
            Assert.That(result.Headers.Fields[3].Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("custom-value")));
            Assert.That(result.DynamicTable, Has.Count.EqualTo(2));
            Assert.That(result.DynamicTable[0].Name,
                Is.EqualTo(Encoding.ASCII.GetBytes("custom-key")));
            Assert.That(result.DynamicTable[0].Index, Is.EqualTo(62));
            Assert.That(result.DynamicTable[1].Name,
                Is.EqualTo(Encoding.ASCII.GetBytes(":authority")));
            Assert.That(result.DynamicTableSize, Is.EqualTo(111));
        });
    }

    [Test]
    public void WithoutIndexingSupportsIndexedAndLiteralNamesWithoutMutation()
    {
        List<byte> block = new List<byte>();
        block.Add(0x04);
        AddRawString(block, "/other");
        block.Add(0x00);
        AddRawString(block, "x-test");
        AddRawString(block, "one");
        HpackDynamicTable table = new HpackDynamicTable(256);

        HpackResult result = DecodeBlock(
            block.ToArray(), table);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Headers.Fields, Has.Count.EqualTo(2));
            Assert.That(result.Headers.Fields[0].Name, Is.EqualTo(":path"));
            Assert.That(result.Headers.Fields[0].Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("/other")));
            Assert.That(result.Headers.Fields[1].Name, Is.EqualTo("x-test"));
            Assert.That(result.Headers.Fields[1].Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("one")));
            Assert.That(result.Headers.Fields[0].NeverIndex, Is.False);
            Assert.That(result.Headers.Fields[1].NeverIndex, Is.False);
            Assert.That(result.DynamicTable, Is.Empty);
            Assert.That(table.GetCount(), Is.Zero);
        });
    }

    [Test]
    public void NeverIndexedLiteralsPropagateTheMarkerAndPermitEmptyValues()
    {
        List<byte> block = new List<byte>();
        block.Add(0x1f);
        block.Add(0x08);
        AddRawString(block, "secret");
        block.Add(0x10);
        AddRawString(block, "x-secret");
        AddRawString(block, "");
        HpackDynamicTable table = new HpackDynamicTable(256);

        HpackResult result = DecodeBlock(
            block.ToArray(), table);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Headers.Fields, Has.Count.EqualTo(2));
            Assert.That(result.Headers.Fields[0].Name, Is.EqualTo("authorization"));
            Assert.That(result.Headers.Fields[0].Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("secret")));
            Assert.That(result.Headers.Fields[0].NeverIndex, Is.True);
            Assert.That(result.Headers.Fields[1].Name, Is.EqualTo("x-secret"));
            Assert.That(result.Headers.Fields[1].Value, Is.Empty);
            Assert.That(result.Headers.Fields[1].NeverIndex, Is.True);
            Assert.That(result.Headers.Fields[0].IsSensitive, Is.False);
            Assert.That(result.DynamicTable, Is.Empty);
        });
    }

    [Test]
    public void HuffmanStringsMatchRfcRequestExamples()
    {
        HpackDynamicTable requestTable = new HpackDynamicTable(4096);
        HpackDynamicTable customTable = new HpackDynamicTable(4096);

        HpackResult request = DecodeBlock(
            Convert.FromHexString("828684418cf1e3c2e5f23a6ba0ab90f4ff"),
            requestTable);
        HpackResult custom = DecodeBlock(
            Convert.FromHexString(
                "408825a849e95ba97d7f8925a849e95bb8e8b4bf"),
            customTable);

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(request.Headers.Fields, Has.Count.EqualTo(4));
            Assert.That(request.Headers.Fields[3].Name, Is.EqualTo(":authority"));
            Assert.That(request.Headers.Fields[3].Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("www.example.com")));
            Assert.That(request.DynamicTableSize, Is.EqualTo(57));
            Assert.That(custom.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(custom.Headers.Fields.Single().Name,
                Is.EqualTo("custom-key"));
            Assert.That(custom.Headers.Fields.Single().Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("custom-value")));
            Assert.That(custom.DynamicTableSize, Is.EqualTo(54));
        });
    }

    [Test]
    public void EmptyBlockAndEmptyRawOrHuffmanValuesAreValid()
    {
        HpackDynamicTable emptyTable = new HpackDynamicTable(256);
        HpackDynamicTable valuesTable = new HpackDynamicTable(256);

        HpackResult empty = DecodeBlock(
            ReadOnlySpan<byte>.Empty, emptyTable);
        HpackResult values = DecodeBlock(
            new byte[] { 0x04, 0x00, 0x04, 0x80 }, valuesTable);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(empty.Consumed, Is.Zero);
            Assert.That(empty.Headers.Fields, Is.Empty);
            Assert.That(values.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(values.Headers.Fields, Has.Count.EqualTo(2));
            Assert.That(values.Headers.Fields[0].Value, Is.Empty);
            Assert.That(values.Headers.Fields[1].Value, Is.Empty);
            Assert.That(values.Headers.Fields[0].Name, Is.EqualTo(":path"));
            Assert.That(values.Headers.Fields[1].Name, Is.EqualTo(":path"));
        });
    }

    [Test]
    public void InvalidFieldAndNameIndexesAreCompressionErrors()
    {
        HpackResult zero = DecodeBlock(
            new byte[] { 0x80 }, new HpackDynamicTable(256));
        HpackResult missingField = DecodeBlock(
            new byte[] { 0xbe }, new HpackDynamicTable(256));
        HpackResult missingName = DecodeBlock(
            new byte[] { 0x0f, 0x2f, 0x00 }, new HpackDynamicTable(256));

        Assert.Multiple(() =>
        {
            Assert.That(zero.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(zero.IsCompressionError, Is.True);
            Assert.That(zero.ErrorCode, Is.EqualTo("invalid-hpack-field-index"));
            Assert.That(missingField.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(missingField.ErrorCode,
                Is.EqualTo("invalid-hpack-field-index"));
            Assert.That(missingName.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(missingName.ErrorCode,
                Is.EqualTo("invalid-hpack-name-index"));
        });
    }

    [Test]
    public void TruncatedAndMalformedStringsAreCompressionErrors()
    {
        HpackResult truncated = DecodeBlock(
            new byte[] { 0x04, 0x05, (byte)'a' },
            new HpackDynamicTable(256));
        HpackResult eos = DecodeBlock(
            new byte[] { 0x00, 0x01, (byte)'x', 0x84,
                0xff, 0xff, 0xff, 0xfc },
            new HpackDynamicTable(256));

        Assert.Multiple(() =>
        {
            Assert.That(truncated.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(truncated.IsCompressionError, Is.True);
            Assert.That(truncated.ErrorCode,
                Is.EqualTo("truncated-hpack-string-data"));
            Assert.That(eos.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(eos.IsCompressionError, Is.True);
            Assert.That(eos.ErrorCode, Is.EqualTo("hpack-huffman-eos-symbol"));
            Assert.That(eos.ErrorPosition, Is.EqualTo(7));
        });
    }

    static void AddRawString(List<byte> output, string value)
    {
        byte[] octets = Encoding.ASCII.GetBytes(value);
        Assert.That(octets.Length, Is.LessThan(127));
        output.Add((byte)octets.Length);
        output.AddRange(octets);
    }

    static HpackResult DecodeBlock(
        ReadOnlySpan<byte> input,
        HpackDynamicTable table)
    {
        return HpackHeaderBlockCodec.Decode(
            input, table, 4096, false, 4096, 100, 65536, 16384);
    }
}
