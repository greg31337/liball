using System.Text;

namespace http.Tests;

public class HpackEncoderTests
{
    [Test]
    public void ExactStaticMatchesUseIndexedRepresentations()
    {
        IHpackEncoder encoder = new HpackEncoder();
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(CreateField(":method", "GET"));
        headers.Fields.Add(CreateField(":path", "/"));
        headers.Fields.Add(CreateField(":method", "GET"));
        HpackOptions options = RawOptions(HpackIndexingMode.Automatic);

        HpackResult result = encoder.EncodeHeaderBlock(headers, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Bytes,
                Is.EqualTo(new byte[] { 0x82, 0x84, 0x82 }));
            Assert.That(result.Written, Is.EqualTo(3));
            Assert.That(result.RequiredSize, Is.EqualTo(3));
            Assert.That(result.HeaderListSize, Is.EqualTo(122));
            Assert.That(result.DynamicTable, Is.Empty);
        });
    }

    [Test]
    public void IncrementalEncodingReusesDynamicExactAndNameMatches()
    {
        HpackEncoder encoder = new HpackEncoder();
        HpackOptions options = RawOptions(HpackIndexingMode.Incremental);
        HttpHeaders firstHeaders = new HttpHeaders();
        firstHeaders.Fields.Add(CreateField("custom-key", "custom-value"));
        HttpHeaders changedHeaders = new HttpHeaders();
        changedHeaders.Fields.Add(CreateField("custom-key", "other"));

        HpackResult first = encoder.EncodeHeaderBlock(firstHeaders, options);
        HpackResult exact = encoder.EncodeHeaderBlock(firstHeaders, options);
        HpackResult named = encoder.EncodeHeaderBlock(changedHeaders, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(first.Bytes, Is.EqualTo(Concat(
                new byte[] { 0x40, 0x0a },
                Encoding.ASCII.GetBytes("custom-key"),
                new byte[] { 0x0c },
                Encoding.ASCII.GetBytes("custom-value"))));
            Assert.That(first.DynamicTable, Has.Count.EqualTo(1));
            Assert.That(exact.Bytes, Is.EqualTo(new byte[] { 0xbe }));
            Assert.That(exact.DynamicTable, Has.Count.EqualTo(1));
            Assert.That(named.Bytes, Is.EqualTo(Concat(
                new byte[] { 0x7e, 0x05 },
                Encoding.ASCII.GetBytes("other"))));
            Assert.That(named.DynamicTable, Has.Count.EqualTo(2));
            Assert.That(named.DynamicTable[0].Name,
                Is.EqualTo(Encoding.ASCII.GetBytes("custom-key")));
            Assert.That(named.DynamicTable[0].Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("other")));
        });
    }

    [Test]
    public void WithoutAndNeverIndexedModesUseTheirLiteralWireForms()
    {
        HttpHeaders withoutHeaders = new HttpHeaders();
        withoutHeaders.Fields.Add(CreateField(":path", "/other"));
        HttpHeaders neverHeaders = new HttpHeaders();
        neverHeaders.Fields.Add(CreateField("authorization", "secret"));

        HpackResult without = new HpackEncoder().EncodeHeaderBlock(
            withoutHeaders, RawOptions(HpackIndexingMode.WithoutIndexing));
        HpackResult never = new HpackEncoder().EncodeHeaderBlock(
            neverHeaders, RawOptions(HpackIndexingMode.NeverIndexed));
        HpackResult decodedNever = new HpackDecoder().DecodeHeaderBlock(
            never.Bytes, new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(without.Bytes, Is.EqualTo(Concat(
                new byte[] { 0x04, 0x06 },
                Encoding.ASCII.GetBytes("/other"))));
            Assert.That(without.DynamicTable, Is.Empty);
            Assert.That(never.Bytes, Is.EqualTo(Concat(
                new byte[] { 0x1f, 0x08, 0x06 },
                Encoding.ASCII.GetBytes("secret"))));
            Assert.That(never.DynamicTable, Is.Empty);
            Assert.That(decodedNever.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decodedNever.Headers.Fields.Single().NeverIndex,
                Is.True);
        });
    }

    [Test]
    public void SensitiveAndExplicitNeverIndexFieldsOverrideTheDefaultMode()
    {
        HttpHeaderField sensitive = CreateField(":method", "GET");
        sensitive.IsSensitive = true;
        HttpHeaders sensitiveHeaders = new HttpHeaders();
        sensitiveHeaders.Fields.Add(sensitive);
        HpackOptions protectedOptions = RawOptions(HpackIndexingMode.Automatic);
        HpackOptions allowedOptions = RawOptions(HpackIndexingMode.Automatic);
        allowedOptions.NeverIndexSensitiveFields = false;
        HttpHeaderField explicitNever = CreateField(":method", "GET");
        explicitNever.NeverIndex = true;
        HttpHeaders explicitHeaders = new HttpHeaders();
        explicitHeaders.Fields.Add(explicitNever);

        HpackResult protectedResult = new HpackEncoder().EncodeHeaderBlock(
            sensitiveHeaders, protectedOptions);
        HpackResult allowedResult = new HpackEncoder().EncodeHeaderBlock(
            sensitiveHeaders, allowedOptions);
        HpackResult explicitResult = new HpackEncoder().EncodeHeaderBlock(
            explicitHeaders, allowedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(protectedResult.Bytes, Is.EqualTo(Concat(
                new byte[] { 0x12, 0x03 },
                Encoding.ASCII.GetBytes("GET"))));
            Assert.That(allowedResult.Bytes, Is.EqualTo(new byte[] { 0x82 }));
            Assert.That(explicitResult.Bytes, Is.EqualTo(Concat(
                new byte[] { 0x12, 0x03 },
                Encoding.ASCII.GetBytes("GET"))));
        });
    }

    [Test]
    public void HuffmanPolicyProducesRfcBytesAndBothFormsRoundTrip()
    {
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(CreateField("custom-key", "custom-value"));
        HpackOptions huffmanOptions = new HpackOptions();
        huffmanOptions.IndexingMode = HpackIndexingMode.Incremental;
        HpackOptions rawOptions = RawOptions(HpackIndexingMode.Incremental);

        HpackResult huffman = new HpackEncoder().EncodeHeaderBlock(
            headers, huffmanOptions);
        HpackResult raw = new HpackEncoder().EncodeHeaderBlock(
            headers, rawOptions);
        HpackResult decodedHuffman = new HpackDecoder().DecodeHeaderBlock(
            huffman.Bytes, new HpackOptions());
        HpackResult decodedRaw = new HpackDecoder().DecodeHeaderBlock(
            raw.Bytes, new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(Convert.ToHexString(huffman.Bytes).ToLowerInvariant(),
                Is.EqualTo("408825a849e95ba97d7f8925a849e95bb8e8b4bf"));
            Assert.That(huffman.Bytes.Length, Is.LessThan(raw.Bytes.Length));
            Assert.That(decodedHuffman.Headers.Fields.Single().Name,
                Is.EqualTo("custom-key"));
            Assert.That(decodedHuffman.Headers.Fields.Single().Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("custom-value")));
            Assert.That(decodedRaw.Headers.Fields.Single().Name,
                Is.EqualTo("custom-key"));
            Assert.That(decodedRaw.Headers.Fields.Single().Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("custom-value")));
        });
    }

    [Test]
    public void EncoderAndDecoderShareStateAcrossConsecutiveBlocks()
    {
        HpackEncoder encoder = new HpackEncoder();
        HpackDecoder decoder = new HpackDecoder();
        HpackOptions options = RawOptions(HpackIndexingMode.Incremental);
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(CreateField("x-state", "one"));

        HpackResult firstEncoded = encoder.EncodeHeaderBlock(headers, options);
        HpackResult firstDecoded = decoder.DecodeHeaderBlock(
            firstEncoded.Bytes, new HpackOptions());
        HpackResult secondEncoded = encoder.EncodeHeaderBlock(headers, options);
        HpackResult secondDecoded = decoder.DecodeHeaderBlock(
            secondEncoded.Bytes, new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(firstDecoded.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(secondEncoded.Bytes, Is.EqualTo(new byte[] { 0xbe }));
            Assert.That(secondDecoded.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(secondDecoded.Headers.Fields.Single().Name,
                Is.EqualTo("x-state"));
            Assert.That(secondDecoded.Headers.Fields.Single().Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("one")));
        });
    }

    [Test]
    public void FailedEncodingDoesNotCommitWorkingTableChanges()
    {
        HpackEncoder encoder = new HpackEncoder();
        HpackOptions options = RawOptions(HpackIndexingMode.Incremental);
        options.MaximumCompressedBlockSize = 8;
        options.Limits.MaximumCompressedBlockSize = 8;
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(CreateField("a", "1"));
        headers.Fields.Add(CreateField("b", "2"));

        HpackResult result = encoder.EncodeHeaderBlock(headers, options);
        HpackResult state = encoder.InspectEncoderTable();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(result.ErrorCode,
                Is.EqualTo("hpack-compressed-block-limit-exceeded"));
            Assert.That(result.DynamicTable, Is.Empty);
            Assert.That(result.DynamicTableSize, Is.Zero);
            Assert.That(state.DynamicTable, Is.Empty);
            Assert.That(state.DynamicTableSize, Is.Zero);
        });
    }

    [Test]
    public void EncoderLimitUpdateAndInspectionReturnOwnedState()
    {
        HpackEncoder encoder = new HpackEncoder();
        HpackResult update = encoder.UpdateEncoderTableLimit(0);
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(CreateField("x", "y"));
        HpackResult encoded = encoder.EncodeHeaderBlock(
            headers, RawOptions(HpackIndexingMode.Incremental));
        HpackResult state = encoder.InspectEncoderTable();

        Assert.Multiple(() =>
        {
            Assert.That(update.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(update.DynamicTableLimit, Is.Zero);
            Assert.That(encoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(encoded.DynamicTable, Is.Empty);
            Assert.That(state.DynamicTableLimit, Is.Zero);
            Assert.That(state.DynamicTable, Is.Empty);
        });
    }

    static HttpHeaderField CreateField(string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        return field;
    }

    static HpackOptions RawOptions(HpackIndexingMode mode)
    {
        HpackOptions options = new HpackOptions();
        options.IndexingMode = mode;
        options.UseHuffman = false;
        return options;
    }

    static byte[] Concat(params byte[][] values)
    {
        List<byte> result = new List<byte>();
        int index = 0;
        while (index < values.Length)
        {
            result.AddRange(values[index]);
            index = index + 1;
        }

        return result.ToArray();
    }
}
