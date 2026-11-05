using System.Text;

namespace http.Tests;

public class HpackEncoderTableSizeTests
{
    [Test]
    public void LimitReductionEmitsOneLeadingUpdateAndClearsBothTables()
    {
        HpackEncoder encoder = new HpackEncoder();
        HpackDecoder decoder = new HpackDecoder();
        HpackOptions options = RawIncrementalOptions();
        HttpHeaders headers = HeadersWith("x-state", "one");
        HpackResult initial = encoder.EncodeHeaderBlock(headers, options);
        decoder.DecodeHeaderBlock(initial.Bytes, new HpackOptions());

        HpackResult encoderUpdate = encoder.UpdateEncoderTableLimit(0);
        decoder.UpdateDecoderTableLimit(0);
        HpackResult updateBlock = encoder.EncodeHeaderBlock(
            new HttpHeaders(), options);
        HpackResult decodedUpdate = decoder.DecodeHeaderBlock(
            updateBlock.Bytes, new HpackOptions());
        HpackResult following = encoder.EncodeHeaderBlock(
            new HttpHeaders(), options);

        Assert.Multiple(() =>
        {
            Assert.That(encoderUpdate.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(encoderUpdate.DynamicTableLimit, Is.Zero);
            Assert.That(encoderUpdate.DynamicTable, Is.Empty);
            Assert.That(updateBlock.Bytes, Is.EqualTo(new byte[] { 0x20 }));
            Assert.That(decodedUpdate.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decodedUpdate.DynamicTableLimit, Is.Zero);
            Assert.That(decodedUpdate.DynamicTable, Is.Empty);
            Assert.That(following.Bytes, Is.Empty);
        });
    }

    [Test]
    public void MultipleLimitChangesEmitSmallestThenFinalSize()
    {
        HpackEncoder encoder = new HpackEncoder();
        HpackDecoder decoder = new HpackDecoder();
        encoder.UpdateEncoderTableLimit(0);
        encoder.UpdateEncoderTableLimit(256);
        decoder.UpdateDecoderTableLimit(0);
        decoder.UpdateDecoderTableLimit(256);

        HpackResult encoded = encoder.EncodeHeaderBlock(
            new HttpHeaders(), RawIncrementalOptions());
        HpackResult decoded = decoder.DecodeHeaderBlock(
            encoded.Bytes, new HpackOptions());
        HpackResult following = encoder.EncodeHeaderBlock(
            new HttpHeaders(), RawIncrementalOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(encoded.Bytes, Is.EqualTo(
                new byte[] { 0x20, 0x3f, 0xe1, 0x01 }));
            Assert.That(encoded.DynamicTableLimit, Is.EqualTo(256));
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decoded.DynamicTableLimit, Is.EqualTo(256));
            Assert.That(following.Bytes, Is.Empty);
        });
    }

    [Test]
    public void IncreaseAfterSynchronizationEmitsOnlyTheNewFinalSize()
    {
        HpackEncoder encoder = new HpackEncoder();
        HpackDecoder decoder = new HpackDecoder();
        encoder.UpdateEncoderTableLimit(0);
        decoder.UpdateDecoderTableLimit(0);
        HpackResult reduction = encoder.EncodeHeaderBlock(
            new HttpHeaders(), RawIncrementalOptions());
        decoder.DecodeHeaderBlock(reduction.Bytes, new HpackOptions());

        encoder.UpdateEncoderTableLimit(256);
        decoder.UpdateDecoderTableLimit(256);
        HpackResult increase = encoder.EncodeHeaderBlock(
            new HttpHeaders(), RawIncrementalOptions());
        HpackResult decoded = decoder.DecodeHeaderBlock(
            increase.Bytes, new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(increase.Bytes,
                Is.EqualTo(new byte[] { 0x3f, 0xe1, 0x01 }));
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decoded.DynamicTableLimit, Is.EqualTo(256));
        });
    }

    [Test]
    public void RequestedActiveSizeIsEmittedOnceAndSharedAcrossBlocks()
    {
        HpackEncoder encoder = new HpackEncoder();
        HpackDecoder decoder = new HpackDecoder();
        HpackOptions options = RawIncrementalOptions();
        options.DynamicTableSize = 128;
        HttpHeaders headers = HeadersWith(":method", "GET");

        HpackResult first = encoder.EncodeHeaderBlock(headers, options);
        HpackResult firstDecoded = decoder.DecodeHeaderBlock(
            first.Bytes, new HpackOptions());
        HpackResult second = encoder.EncodeHeaderBlock(headers, options);
        HpackResult secondDecoded = decoder.DecodeHeaderBlock(
            second.Bytes, new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(first.Bytes,
                Is.EqualTo(new byte[] { 0x3f, 0x61, 0x82 }));
            Assert.That(first.DynamicTableLimit, Is.EqualTo(128));
            Assert.That(firstDecoded.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(firstDecoded.DynamicTableLimit, Is.EqualTo(128));
            Assert.That(second.Bytes, Is.EqualTo(new byte[] { 0x82 }));
            Assert.That(secondDecoded.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(secondDecoded.DynamicTableLimit, Is.EqualTo(128));
        });
    }

    [Test]
    public void FailedBlockRetainsThePendingUpdateForRetry()
    {
        HpackEncoder encoder = new HpackEncoder();
        encoder.UpdateEncoderTableLimit(0);
        HpackOptions limited = RawIncrementalOptions();
        limited.MaximumCompressedBlockSize = 0;
        limited.Limits.MaximumCompressedBlockSize = 0;

        HpackResult failed = encoder.EncodeHeaderBlock(
            new HttpHeaders(), limited);
        HpackResult retry = encoder.EncodeHeaderBlock(
            new HttpHeaders(), RawIncrementalOptions());
        HpackResult following = encoder.EncodeHeaderBlock(
            new HttpHeaders(), RawIncrementalOptions());

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(failed.Bytes, Is.Empty);
            Assert.That(failed.DynamicTableLimit, Is.Zero);
            Assert.That(retry.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(retry.Bytes, Is.EqualTo(new byte[] { 0x20 }));
            Assert.That(following.Bytes, Is.Empty);
        });
    }

    [Test]
    public void InvalidLimitChangeDoesNotMutateOrScheduleEncoderState()
    {
        HpackEncoder encoder = new HpackEncoder();

        HpackResult invalid = encoder.UpdateEncoderTableLimit(-1);
        HpackResult encoded = encoder.EncodeHeaderBlock(
            new HttpHeaders(), RawIncrementalOptions());
        HpackResult state = encoder.InspectEncoderTable();

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(invalid.ErrorCode,
                Is.EqualTo("invalid-hpack-dynamic-table-size"));
            Assert.That(encoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(encoded.Bytes, Is.Empty);
            Assert.That(state.DynamicTableLimit, Is.EqualTo(4096));
        });
    }

    [Test]
    public void DynamicIndexesRemainSynchronizedAcrossUnrelatedBlocks()
    {
        HpackEncoder encoder = new HpackEncoder();
        HpackDecoder decoder = new HpackDecoder();
        HpackOptions options = RawIncrementalOptions();
        HttpHeaders firstHeaders = HeadersWith("x-first", "one");
        HttpHeaders secondHeaders = HeadersWith("x-second", "two");

        HpackResult first = encoder.EncodeHeaderBlock(firstHeaders, options);
        HpackResult second = encoder.EncodeHeaderBlock(secondHeaders, options);
        HpackResult repeated = encoder.EncodeHeaderBlock(firstHeaders, options);
        decoder.DecodeHeaderBlock(first.Bytes, new HpackOptions());
        decoder.DecodeHeaderBlock(second.Bytes, new HpackOptions());
        HpackResult decoded = decoder.DecodeHeaderBlock(
            repeated.Bytes, new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Bytes, Is.EqualTo(new byte[] { 0xbf }));
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decoded.Headers.Fields.Single().Name,
                Is.EqualTo("x-first"));
            Assert.That(decoded.Headers.Fields.Single().Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("one")));
            Assert.That(decoded.DynamicTable, Has.Count.EqualTo(2));
        });
    }

    static HpackOptions RawIncrementalOptions()
    {
        HpackOptions options = new HpackOptions();
        options.IndexingMode = HpackIndexingMode.Incremental;
        options.UseHuffman = false;
        return options;
    }

    static HttpHeaders HeadersWith(string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(field);
        return headers;
    }
}
