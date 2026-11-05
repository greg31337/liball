using System.Text;

namespace http.Tests;

public class HpackMalformedExhaustiveTests
{
    [TestCase(
        "400a637573746f6d2d6b65790d637573746f6d2d686561646572")]
    [TestCase(
        "408825a849e95ba97d7f8925a849e95bb8e8b4bf")]
    public void EveryProperLiteralPrefixIsRejectedAsTruncated(
        string hexadecimal)
    {
        byte[] complete = Convert.FromHexString(hexadecimal);
        int length = 1;
        while (length < complete.Length)
        {
            HpackDecoder decoder = new HpackDecoder();
            HpackResult result = decoder.DecodeHeaderBlock(
                complete.AsSpan(0, length).ToArray(),
                new HpackOptions());
            HpackResult state = decoder.InspectDecoderTable();

            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected),
                $"Prefix length {length} was accepted.");
            Assert.That(result.IsCompressionError, Is.True,
                $"Prefix length {length} was not a compression error.");
            Assert.That(result.HasErrorPosition, Is.True);
            Assert.That(result.Consumed, Is.LessThanOrEqualTo(length));
            Assert.That(state.DynamicTable, Is.Empty,
                $"Prefix length {length} created a partial table entry.");
            length = length + 1;
        }

        HpackResult completed = new HpackDecoder().DecodeHeaderBlock(
            complete,
            new HpackOptions());
        Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done));
        Assert.That(completed.Consumed, Is.EqualTo(complete.Length));
    }

    [TestCase("ff", "truncated-hpack-integer")]
    [TestCase("0f", "truncated-hpack-name-index")]
    [TestCase("00", "truncated-hpack-string-length")]
    [TestCase("0001", "truncated-hpack-string-data")]
    [TestCase("000161", "truncated-hpack-string-length")]
    [TestCase("0401", "truncated-hpack-string-data")]
    public void EveryTruncatedComponentHasAStableDiagnostic(
        string hexadecimal,
        string errorCode)
    {
        byte[] input = Convert.FromHexString(hexadecimal);
        HpackResult result = new HpackDecoder().DecodeHeaderBlock(
            input,
            new HpackOptions());

        AssertMalformed(result, errorCode, input.Length);
    }

    [TestCase("indexed")]
    [TestCase("literal-name")]
    [TestCase("string-length")]
    [TestCase("table-size")]
    public void SignedIntegerOverflowIsRejectedInEveryRepresentation(
        string selected)
    {
        byte[] overflow;
        if (selected == "indexed")
        {
            overflow = OverflowInteger(0xff);
        }
        else if (selected == "literal-name")
        {
            overflow = OverflowInteger(0x7f);
        }
        else if (selected == "string-length")
        {
            overflow = Join([0x04], OverflowInteger(0xff));
        }
        else
        {
            overflow = OverflowInteger(0x3f);
        }

        HpackResult result = new HpackDecoder().DecodeHeaderBlock(
            overflow,
            new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.IsCompressionError, Is.True);
            Assert.That(result.HasErrorPosition, Is.True);
            Assert.That(result.ErrorCode,
                Is.EqualTo("hpack-integer-overflow"));
        });
    }

    [Test]
    public void StringLengthBeyondTheSupportedRangeIsRejectedBeforeData()
    {
        HpackIntegerResult length = HpackIntegerCodec.Encode(
            (long)int.MaxValue + 1,
            7,
            0);
        byte[] input = Join([0x04], length.Bytes);

        HpackResult result = new HpackDecoder().DecodeHeaderBlock(
            input,
            new HpackOptions());

        AssertMalformed(result, "hpack-string-length-overflow", input.Length);
    }

    [TestCase("80", "invalid-hpack-field-index")]
    [TestCase("be", "invalid-hpack-field-index")]
    [TestCase("7e00", "invalid-hpack-name-index")]
    [TestCase("0f2f00", "invalid-hpack-name-index")]
    [TestCase("1f2f00", "invalid-hpack-name-index")]
    public void ZeroMissingAndLiteralNameIndexesAreRejected(
        string hexadecimal,
        string errorCode)
    {
        byte[] input = Convert.FromHexString(hexadecimal);
        HpackResult result = new HpackDecoder().DecodeHeaderBlock(
            input,
            new HpackOptions());

        AssertMalformed(result, errorCode, input.Length);
    }

    [TestCase("8220", "invalid-hpack-table-size-update-placement")]
    [TestCase("202122", "too-many-hpack-table-size-updates")]
    [TestCase("2120", "invalid-hpack-table-size-update-sequence")]
    public void IllegalTableSizeUpdateSequencesAreCompressionErrors(
        string hexadecimal,
        string errorCode)
    {
        byte[] input = Convert.FromHexString(hexadecimal);
        HpackResult result = new HpackDecoder().DecodeHeaderBlock(
            input,
            new HpackOptions());

        AssertMalformed(result, errorCode, input.Length);
    }

    [Test]
    public void OversizedAndMissingRequiredTableUpdatesRemainPending()
    {
        HpackOptions maximum31 = new HpackOptions();
        maximum31.MaximumDynamicTableSize = 31;
        HpackDecoder oversizedDecoder = new HpackDecoder();
        HpackResult oversized = oversizedDecoder.DecodeHeaderBlock(
            new byte[] { 0x3f, 0x01 },
            maximum31);

        HpackDecoder requiredDecoder = new HpackDecoder();
        requiredDecoder.UpdateDecoderTableLimit(0);
        HpackResult missing = requiredDecoder.DecodeHeaderBlock(
            new byte[] { 0x82 },
            new HpackOptions());
        HpackResult stillMissing = requiredDecoder.DecodeHeaderBlock(
            new byte[] { 0x82 },
            new HpackOptions());
        HpackResult acknowledged = requiredDecoder.DecodeHeaderBlock(
            new byte[] { 0x20, 0x82 },
            new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(oversized.ErrorCode,
                Is.EqualTo("hpack-table-size-update-exceeds-maximum"));
            Assert.That(oversized.IsCompressionError, Is.True);
            Assert.That(missing.ErrorCode,
                Is.EqualTo("required-hpack-table-size-update"));
            Assert.That(stillMissing.ErrorCode,
                Is.EqualTo("required-hpack-table-size-update"));
            Assert.That(acknowledged.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(acknowledged.DynamicTableLimit, Is.Zero);
        });
    }

    [Test]
    public void SmallestPendingTableLimitMustBeAcknowledgedFirst()
    {
        HpackDecoder decoder = new HpackDecoder();
        decoder.UpdateDecoderTableLimit(0);
        decoder.UpdateDecoderTableLimit(256);

        HpackResult rejected = decoder.DecodeHeaderBlock(
            new byte[] { 0x3f, 0xe1, 0x01 },
            new HpackOptions());
        HpackResult accepted = decoder.DecodeHeaderBlock(
            new byte[] { 0x20, 0x3f, 0xe1, 0x01 },
            new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("invalid-hpack-table-size-update-minimum"));
            Assert.That(accepted.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(accepted.DynamicTableLimit, Is.EqualTo(256));
        });
    }

    [TestCase("0484fffffffc", "hpack-huffman-eos-symbol")]
    [TestCase("048118", "invalid-hpack-huffman-padding")]
    [TestCase("0481ff", "invalid-hpack-huffman-padding")]
    public void InvalidHuffmanCodesAndPaddingAreCompressionErrors(
        string hexadecimal,
        string errorCode)
    {
        byte[] input = Convert.FromHexString(hexadecimal);
        HpackResult result = new HpackDecoder().DecodeHeaderBlock(
            input,
            new HpackOptions());

        AssertMalformed(result, errorCode, input.Length);
    }

    [TestCase("compressed", "hpack-compressed-block-limit-exceeded")]
    [TestCase("count", "hpack-header-count-limit-exceeded")]
    [TestCase("list", "hpack-header-list-size-limit-exceeded")]
    [TestCase("raw-string", "hpack-string-limit-exceeded")]
    [TestCase("huffman-string", "hpack-string-limit-exceeded")]
    public void EveryDecodeSizeLimitFailsWithItsStableDiagnostic(
        string selected,
        string errorCode)
    {
        HpackOptions options = new HpackOptions();
        byte[] input;
        if (selected == "compressed")
        {
            options.MaximumCompressedBlockSize = 0;
            input = [0x82];
        }
        else if (selected == "count")
        {
            options.MaximumHeaderCount = 0;
            input = RawIncremental("x", "y");
        }
        else if (selected == "list")
        {
            options.MaximumHeaderListSize = 33;
            input = RawIncremental("x", "y");
        }
        else if (selected == "raw-string")
        {
            options.MaximumStringSize = 0;
            input = [0x04, 0x01, (byte)'a'];
        }
        else
        {
            options.MaximumStringSize = 0;
            input = [0x04, 0x81, 0x1f];
        }

        HpackResult result = new HpackDecoder().DecodeHeaderBlock(
            input,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(result.IsCompressionError, Is.False);
        });
    }

    [Test]
    public void SharedLimitsCanBeStricterThanOperationLimits()
    {
        HpackOptions countOptions = new HpackOptions();
        countOptions.Limits.MaximumFieldCount = 0;
        HpackOptions listOptions = new HpackOptions();
        listOptions.Limits.MaximumHeaderListSize = 33;
        HpackOptions compressedOptions = new HpackOptions();
        compressedOptions.Limits.MaximumCompressedBlockSize = 0;
        HpackOptions stringOptions = new HpackOptions();
        stringOptions.Limits.MaximumStringSize = 0;

        HpackResult count = Decode(RawIncremental("x", "y"), countOptions);
        HpackResult list = Decode(RawIncremental("x", "y"), listOptions);
        HpackResult compressed = Decode([0x82], compressedOptions);
        HpackResult text = Decode([0x04, 0x01, (byte)'a'], stringOptions);

        Assert.Multiple(() =>
        {
            Assert.That(count.ErrorCode,
                Is.EqualTo("hpack-header-count-limit-exceeded"));
            Assert.That(list.ErrorCode,
                Is.EqualTo("hpack-header-list-size-limit-exceeded"));
            Assert.That(compressed.ErrorCode,
                Is.EqualTo("hpack-compressed-block-limit-exceeded"));
            Assert.That(text.ErrorCode,
                Is.EqualTo("hpack-string-limit-exceeded"));
        });
    }

    [Test]
    public void MalformedLiteralDoesNotAlterExistingDecoderState()
    {
        HpackDecoder decoder = new HpackDecoder();
        HpackResult seeded = decoder.DecodeHeaderBlock(
            RawIncremental("x-state", "one"),
            new HpackOptions());
        HpackResult before = decoder.InspectDecoderTable();

        HpackResult rejected = decoder.DecodeHeaderBlock(
            [0x40, 0x01, (byte)'y', 0x81, 0x18],
            new HpackOptions());
        HpackResult after = decoder.InspectDecoderTable();
        HpackResult reused = decoder.DecodeHeaderBlock(
            new byte[] { 0xbe },
            new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(seeded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("invalid-hpack-huffman-padding"));
            AssertTablesEqual(before, after);
            Assert.That(reused.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(reused.Headers.Fields.Single().Name,
                Is.EqualTo("x-state"));
            Assert.That(reused.Headers.Fields.Single().Value,
                Is.EqualTo("one"u8.ToArray()));
        });
    }

    [Test]
    public void ResourceRejectionCommitsProcessedEntriesForSynchronization()
    {
        HpackDecoder decoder = new HpackDecoder();
        HpackOptions limited = new HpackOptions();
        limited.MaximumHeaderCount = 1;
        byte[] input = Join(
            RawIncremental("first", "one"),
            RawIncremental("second", "two"));

        HpackResult rejected = decoder.DecodeHeaderBlock(input, limited);
        HpackResult reused = decoder.DecodeHeaderBlock(
            new byte[] { 0xbe },
            new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.IsCompressionError, Is.False);
            Assert.That(rejected.Consumed, Is.EqualTo(input.Length));
            Assert.That(rejected.DynamicTable, Has.Count.EqualTo(2));
            Assert.That(reused.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(reused.Headers.Fields.Single().Name,
                Is.EqualTo("second"));
            Assert.That(reused.Headers.Fields.Single().Value,
                Is.EqualTo("two"u8.ToArray()));
        });
    }

    [Test]
    public void RejectedEncodingDoesNotCommitItsWorkingTable()
    {
        HpackEncoder encoder = new HpackEncoder();
        HpackOptions options = RawEncoderOptions();
        HttpHeaders seed = Headers("x-state", "one");
        HpackResult seeded = encoder.EncodeHeaderBlock(seed, options);
        HpackResult before = encoder.InspectEncoderTable();
        HpackOptions limited = RawEncoderOptions();
        limited.MaximumCompressedBlockSize = 1;
        limited.Limits.MaximumCompressedBlockSize = 1;

        HpackResult rejected = encoder.EncodeHeaderBlock(
            Headers("new-state", "two"),
            limited);
        HpackResult after = encoder.InspectEncoderTable();
        HpackResult reused = encoder.EncodeHeaderBlock(seed, options);

        Assert.Multiple(() =>
        {
            Assert.That(seeded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(rejected.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("hpack-compressed-block-limit-exceeded"));
            AssertTablesEqual(before, after);
            Assert.That(reused.Bytes, Is.EqualTo(new byte[] { 0xbe }));
        });
    }

    static void AssertMalformed(
        HpackResult result,
        string errorCode,
        int inputLength)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.IsCompressionError, Is.True);
            Assert.That(result.HasErrorPosition, Is.True);
            Assert.That(result.ErrorPosition,
                Is.InRange(0, inputLength));
            Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
        });
    }

    static void AssertTablesEqual(HpackResult left, HpackResult right)
    {
        Assert.That(right.DynamicTableSize, Is.EqualTo(left.DynamicTableSize));
        Assert.That(right.DynamicTableLimit, Is.EqualTo(left.DynamicTableLimit));
        Assert.That(right.DynamicTable.Count, Is.EqualTo(left.DynamicTable.Count));
        int index = 0;
        while (index < left.DynamicTable.Count)
        {
            Assert.That(right.DynamicTable[index].Name,
                Is.EqualTo(left.DynamicTable[index].Name));
            Assert.That(right.DynamicTable[index].Value,
                Is.EqualTo(left.DynamicTable[index].Value));
            index = index + 1;
        }
    }

    static HpackResult Decode(byte[] input, HpackOptions options)
    {
        return new HpackDecoder().DecodeHeaderBlock(input, options);
    }

    static byte[] OverflowInteger(byte first)
    {
        byte[] result = new byte[11];
        result[0] = first;
        int index = 1;
        while (index < 10)
        {
            result[index] = 0x80;
            index = index + 1;
        }

        result[10] = 0x01;
        return result;
    }

    static byte[] RawIncremental(string name, string value)
    {
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);
        byte[] valueBytes = Encoding.ASCII.GetBytes(value);
        List<byte> result = new List<byte>();
        result.Add(0x40);
        result.Add((byte)nameBytes.Length);
        result.AddRange(nameBytes);
        result.Add((byte)valueBytes.Length);
        result.AddRange(valueBytes);
        return result.ToArray();
    }

    static HpackOptions RawEncoderOptions()
    {
        HpackOptions options = new HpackOptions();
        options.IndexingMode = HpackIndexingMode.Incremental;
        options.UseHuffman = false;
        return options;
    }

    static HttpHeaders Headers(string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(field);
        return headers;
    }

    static byte[] Join(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, result, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }
}
