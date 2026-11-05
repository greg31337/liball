using System.Text;

namespace http.Tests;

public class HpackRfc7541AppendixTests
{
    [TestCase(10, 5, "0a")]
    [TestCase(1337, 5, "1f9a0a")]
    [TestCase(42, 8, "2a")]
    public void AppendixC1IntegerExamplesAreExact(
        long value,
        int prefixWidth,
        string hexadecimal)
    {
        HpackIntegerResult encoded = HpackIntegerCodec.Encode(
            value,
            prefixWidth,
            0);
        HpackIntegerResult decoded = HpackIntegerCodec.Decode(
            Convert.FromHexString(hexadecimal),
            prefixWidth);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(encoded.Bytes,
                Is.EqualTo(Convert.FromHexString(hexadecimal)));
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decoded.Value, Is.EqualTo(value));
            Assert.That(decoded.Consumed,
                Is.EqualTo(Convert.FromHexString(hexadecimal).Length));
        });
    }

    [TestCase(
        HpackIndexingMode.Incremental,
        "custom-key",
        "custom-header",
        "400a637573746f6d2d6b65790d637573746f6d2d686561646572",
        55,
        false)]
    [TestCase(
        HpackIndexingMode.WithoutIndexing,
        ":path",
        "/sample/path",
        "040c2f73616d706c652f70617468",
        0,
        false)]
    [TestCase(
        HpackIndexingMode.NeverIndexed,
        "password",
        "secret",
        "100870617373776f726406736563726574",
        0,
        true)]
    [TestCase(
        HpackIndexingMode.Automatic,
        ":method",
        "GET",
        "82",
        0,
        false)]
    public void AppendixC2RepresentationExamplesAreExact(
        HpackIndexingMode mode,
        string name,
        string value,
        string hexadecimal,
        int tableSize,
        bool neverIndex)
    {
        HttpHeaders headers = Headers(name, value);
        HpackOptions options = RawOptions(mode);
        HpackEncoder encoder = new HpackEncoder();
        HpackDecoder decoder = new HpackDecoder();

        HpackResult encoded = encoder.EncodeHeaderBlock(headers, options);
        HpackResult decoded = decoder.DecodeHeaderBlock(
            Convert.FromHexString(hexadecimal),
            new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(encoded.Bytes,
                Is.EqualTo(Convert.FromHexString(hexadecimal)));
            Assert.That(encoded.DynamicTableSize, Is.EqualTo(tableSize));
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decoded.Headers.Fields.Single().Name, Is.EqualTo(name));
            Assert.That(decoded.Headers.Fields.Single().Value,
                Is.EqualTo(Encoding.ASCII.GetBytes(value)));
            Assert.That(decoded.Headers.Fields.Single().NeverIndex,
                Is.EqualTo(neverIndex));
            Assert.That(decoded.DynamicTableSize, Is.EqualTo(tableSize));
        });
    }

    [Test]
    public void SensitiveFieldsUseTheAppendixC2NeverIndexedRepresentation()
    {
        HttpHeaderField field = Field("password", "secret");
        field.IsSensitive = true;
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(field);
        HpackOptions options = RawOptions(HpackIndexingMode.Automatic);

        HpackResult encoded = new HpackEncoder().EncodeHeaderBlock(
            headers,
            options);
        HpackResult decoded = new HpackDecoder().DecodeHeaderBlock(
            encoded.Bytes,
            new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Bytes, Is.EqualTo(Convert.FromHexString(
                "100870617373776f726406736563726574")));
            Assert.That(encoded.DynamicTable, Is.Empty);
            Assert.That(decoded.Headers.Fields.Single().NeverIndex, Is.True);
            Assert.That(decoded.DynamicTable, Is.Empty);
        });
    }

    [Test]
    public void EveryAppendixAStaticEntryUsesItsIndexedRepresentation()
    {
        Assert.Multiple(() =>
        {
            int index = 1;
            while (index <= HpackStaticTable.Names.Length)
            {
                HttpHeaders headers = Headers(
                    HpackStaticTable.Names[index - 1],
                    HpackStaticTable.Values[index - 1]);
                HpackResult encoded = new HpackEncoder().EncodeHeaderBlock(
                    headers,
                    RawOptions(HpackIndexingMode.Automatic));
                HpackResult decoded = new HpackDecoder().DecodeHeaderBlock(
                    encoded.Bytes,
                    new HpackOptions());

                Assert.That(encoded.Bytes,
                    Is.EqualTo(new byte[] { (byte)(0x80 | index) }),
                    $"Static table index {index} was not encoded directly.");
                Assert.That(encoded.DynamicTable, Is.Empty);
                Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done));
                Assert.That(decoded.Headers.Fields.Single().Name,
                    Is.EqualTo(HpackStaticTable.Names[index - 1]));
                Assert.That(decoded.Headers.Fields.Single().Value,
                    Is.EqualTo(Encoding.ASCII.GetBytes(
                        HpackStaticTable.Values[index - 1])));
                index = index + 1;
            }
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AppendixC3AndC4RequestSequencesAreExactSharedContexts(
        bool useHuffman)
    {
        string[] raw =
        [
            "828684410f7777772e6578616d706c652e636f6d",
            "828684be58086e6f2d6361636865",
            "828785bf400a637573746f6d2d6b65790c637573746f6d2d76616c7565"
        ];
        string[] huffman =
        [
            "828684418cf1e3c2e5f23a6ba0ab90f4ff",
            "828684be5886a8eb10649cbf",
            "828785bf408825a849e95ba97d7f8925a849e95bb8e8b4bf"
        ];
        HttpHeaders[] blocks = RequestHeaders();
        int[] tableSizes = [57, 110, 164];
        string[] expected = useHuffman ? huffman : raw;
        HpackEncoder encoder = new HpackEncoder();
        HpackDecoder decoder = new HpackDecoder();
        HpackOptions options = IncrementalOptions(useHuffman);

        int index = 0;
        while (index < blocks.Length)
        {
            HpackResult encoded = encoder.EncodeHeaderBlock(
                blocks[index],
                options);
            HpackResult decoded = decoder.DecodeHeaderBlock(
                Convert.FromHexString(expected[index]),
                new HpackOptions());

            Assert.Multiple(() =>
            {
                Assert.That(encoded.Status,
                    Is.EqualTo(HttpOperationStatus.Done));
                Assert.That(Convert.ToHexString(encoded.Bytes).ToLowerInvariant(),
                    Is.EqualTo(expected[index]));
                AssertHeaders(decoded, blocks[index]);
                Assert.That(encoded.DynamicTableSize,
                    Is.EqualTo(tableSizes[index]));
                Assert.That(decoded.DynamicTableSize,
                    Is.EqualTo(tableSizes[index]));
                AssertTablesEqual(encoded.DynamicTable, decoded.DynamicTable);
            });
            index = index + 1;
        }

        AssertRequestTable(decoder.InspectDecoderTable());
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AppendixC5AndC6ResponseSequencesAreExactSharedContexts(
        bool useHuffman)
    {
        string[] raw =
        [
            "4803333032580770726976617465611d4d6f6e2c203231204f637420" +
                "323031332032303a31333a323120474d546e1768747470733a2f2f7777" +
                "772e6578616d706c652e636f6d",
            "4803333037c1c0bf",
            "88c1611d4d6f6e2c203231204f637420323031332032303a31333a3232" +
                "20474d54c05a04677a69707738666f6f3d4153444a4b48514b425a584f" +
                "5157454f50495541585157454f49553b206d61782d6167653d333630303b" +
                "2076657273696f6e3d31"
        ];
        string[] huffman =
        [
            "488264025885aec3771a4b6196d07abe941054d444a8200595040b8166" +
                "e082a62d1bff6e919d29ad171863c78f0b97c8e9ae82ae43d3",
            "4883640effc1c0bf",
            "88c16196d07abe941054d444a8200595040b8166e084a62d1bffc05a83" +
                "9bd9ab77ad94e7821dd7f2e6c7b335dfdfcd5b3960d5af27087f3672c1" +
                "ab270fb5291f9587316065c003ed4ee5b1063d5007"
        ];
        HttpHeaders[] blocks = ResponseHeaders();
        int[] tableSizes = [222, 222, 215];
        string[] expected = useHuffman ? huffman : raw;
        HpackEncoder encoder = new HpackEncoder();
        HpackDecoder decoder = new HpackDecoder();
        SynchronizeTableSize(encoder, decoder);
        HpackOptions options = IncrementalOptions(useHuffman);

        int index = 0;
        while (index < blocks.Length)
        {
            HpackResult encoded = encoder.EncodeHeaderBlock(
                blocks[index],
                options);
            HpackResult decoded = decoder.DecodeHeaderBlock(
                Convert.FromHexString(expected[index]),
                new HpackOptions());

            Assert.Multiple(() =>
            {
                Assert.That(encoded.Status,
                    Is.EqualTo(HttpOperationStatus.Done));
                Assert.That(Convert.ToHexString(encoded.Bytes).ToLowerInvariant(),
                    Is.EqualTo(expected[index]));
                AssertHeaders(decoded, blocks[index]);
                Assert.That(encoded.DynamicTableSize,
                    Is.EqualTo(tableSizes[index]));
                Assert.That(decoded.DynamicTableSize,
                    Is.EqualTo(tableSizes[index]));
                Assert.That(encoded.DynamicTableLimit, Is.EqualTo(256));
                Assert.That(decoded.DynamicTableLimit, Is.EqualTo(256));
                AssertTablesEqual(encoded.DynamicTable, decoded.DynamicTable);
            });
            index = index + 1;
        }

        AssertResponseTable(decoder.InspectDecoderTable());
    }

    static void SynchronizeTableSize(HpackEncoder encoder, HpackDecoder decoder)
    {
        HpackResult encoderLimit = encoder.UpdateEncoderTableLimit(256);
        HpackResult decoderLimit = decoder.UpdateDecoderTableLimit(256);
        HpackResult update = encoder.EncodeHeaderBlock(
            new HttpHeaders(),
            IncrementalOptions(false));
        HpackResult acknowledged = decoder.DecodeHeaderBlock(
            update.Bytes,
            new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoderLimit.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decoderLimit.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(update.Bytes,
                Is.EqualTo(Convert.FromHexString("3fe101")));
            Assert.That(acknowledged.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(acknowledged.DynamicTableLimit, Is.EqualTo(256));
        });
    }

    static HttpHeaders[] RequestHeaders()
    {
        return
        [
            Headers(
                ":method", "GET",
                ":scheme", "http",
                ":path", "/",
                ":authority", "www.example.com"),
            Headers(
                ":method", "GET",
                ":scheme", "http",
                ":path", "/",
                ":authority", "www.example.com",
                "cache-control", "no-cache"),
            Headers(
                ":method", "GET",
                ":scheme", "https",
                ":path", "/index.html",
                ":authority", "www.example.com",
                "custom-key", "custom-value")
        ];
    }

    static HttpHeaders[] ResponseHeaders()
    {
        return
        [
            Headers(
                ":status", "302",
                "cache-control", "private",
                "date", "Mon, 21 Oct 2013 20:13:21 GMT",
                "location", "https://www.example.com"),
            Headers(
                ":status", "307",
                "cache-control", "private",
                "date", "Mon, 21 Oct 2013 20:13:21 GMT",
                "location", "https://www.example.com"),
            Headers(
                ":status", "200",
                "cache-control", "private",
                "date", "Mon, 21 Oct 2013 20:13:22 GMT",
                "location", "https://www.example.com",
                "content-encoding", "gzip",
                "set-cookie",
                "foo=ASDJKHQKBZXOQWEOPIUAXQWEOIU; max-age=3600; version=1")
        ];
    }

    static void AssertRequestTable(HpackResult state)
    {
        Assert.Multiple(() =>
        {
            Assert.That(state.DynamicTable.Select(EntryText), Is.EqualTo(new[]
            {
                "custom-key: custom-value",
                "cache-control: no-cache",
                ":authority: www.example.com"
            }));
            Assert.That(state.DynamicTableSize, Is.EqualTo(164));
        });
    }

    static void AssertResponseTable(HpackResult state)
    {
        Assert.Multiple(() =>
        {
            Assert.That(state.DynamicTable.Select(EntryText), Is.EqualTo(new[]
            {
                "set-cookie: foo=ASDJKHQKBZXOQWEOPIUAXQWEOIU; " +
                    "max-age=3600; version=1",
                "content-encoding: gzip",
                "date: Mon, 21 Oct 2013 20:13:22 GMT"
            }));
            Assert.That(state.DynamicTableSize, Is.EqualTo(215));
            Assert.That(state.DynamicTableLimit, Is.EqualTo(256));
        });
    }

    static void AssertHeaders(HpackResult result, HttpHeaders expected)
    {
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
        Assert.That(result.Headers.Fields.Count,
            Is.EqualTo(expected.Fields.Count));
        int index = 0;
        while (index < expected.Fields.Count)
        {
            Assert.That(result.Headers.Fields[index].Name,
                Is.EqualTo(expected.Fields[index].Name));
            Assert.That(result.Headers.Fields[index].Value,
                Is.EqualTo(expected.Fields[index].Value));
            index = index + 1;
        }
    }

    static void AssertTablesEqual(
        List<HpackTableEntry> encoded,
        List<HpackTableEntry> decoded)
    {
        Assert.That(decoded.Count, Is.EqualTo(encoded.Count));
        int index = 0;
        while (index < encoded.Count)
        {
            Assert.That(decoded[index].Name, Is.EqualTo(encoded[index].Name));
            Assert.That(decoded[index].Value, Is.EqualTo(encoded[index].Value));
            Assert.That(decoded[index].Size, Is.EqualTo(encoded[index].Size));
            Assert.That(decoded[index].Index, Is.EqualTo(encoded[index].Index));
            index = index + 1;
        }
    }

    static string EntryText(HpackTableEntry entry)
    {
        return Encoding.ASCII.GetString(entry.Name) + ": " +
            Encoding.ASCII.GetString(entry.Value);
    }

    static HpackOptions IncrementalOptions(bool useHuffman)
    {
        HpackOptions options = new HpackOptions();
        options.IndexingMode = HpackIndexingMode.Incremental;
        options.UseHuffman = useHuffman;
        if (useHuffman)
        {
            options.UseHuffmanOnlyWhenSmaller = false;
        }

        return options;
    }

    static HpackOptions RawOptions(HpackIndexingMode mode)
    {
        HpackOptions options = new HpackOptions();
        options.IndexingMode = mode;
        options.UseHuffman = false;
        return options;
    }

    static HttpHeaders Headers(params string[] pairs)
    {
        HttpHeaders headers = new HttpHeaders();
        int index = 0;
        while (index < pairs.Length)
        {
            headers.Fields.Add(Field(pairs[index], pairs[index + 1]));
            index = index + 2;
        }

        return headers;
    }

    static HttpHeaderField Field(string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        return field;
    }
}
