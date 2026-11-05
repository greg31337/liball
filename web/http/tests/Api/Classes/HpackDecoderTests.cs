using System.Text;

namespace http.Tests;

public class HpackDecoderTests
{
    [Test]
    public void PublicDecoderExposesDecodedFieldsAndDynamicTableState()
    {
        IHpackDecoder decoder = new HpackDecoder();
        byte[] block = Convert.FromHexString(
            "828684418cf1e3c2e5f23a6ba0ab90f4ff");

        HpackResult result = decoder.DecodeHeaderBlock(block, new HpackOptions());
        HpackResult inspection = decoder.InspectDecoderTable();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(block.Length));
            Assert.That(result.Headers.Fields, Has.Count.EqualTo(4));
            Assert.That(result.Headers.Fields[3].Name, Is.EqualTo(":authority"));
            Assert.That(result.Headers.Fields[3].Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("www.example.com")));
            Assert.That(result.DynamicTable, Has.Count.EqualTo(1));
            Assert.That(result.DynamicTableSize, Is.EqualTo(57));
            Assert.That(inspection.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(inspection.DynamicTable, Has.Count.EqualTo(1));
            Assert.That(inspection.DynamicTable[0].Index, Is.EqualTo(62));
        });
    }

    [Test]
    public void DecoderTableLimitUpdateWaitsForPeerUpdateBeforeClearingState()
    {
        HpackDecoder decoder = new HpackDecoder();
        decoder.DecodeHeaderBlock(
            Convert.FromHexString("418cf1e3c2e5f23a6ba0ab90f4ff"),
            new HpackOptions());

        HpackResult update = decoder.UpdateDecoderTableLimit(0);
        HpackResult beforePeerUpdate = decoder.InspectDecoderTable();
        HpackResult decoded = decoder.DecodeHeaderBlock(
            new byte[] { 0x20 }, new HpackOptions());
        HpackResult afterPeerUpdate = decoder.InspectDecoderTable();

        Assert.Multiple(() =>
        {
            Assert.That(update.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(update.DynamicTableLimit, Is.EqualTo(4096));
            Assert.That(update.DynamicTableSize, Is.EqualTo(57));
            Assert.That(beforePeerUpdate.DynamicTable, Has.Count.EqualTo(1));
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decoded.DynamicTableLimit, Is.Zero);
            Assert.That(decoded.DynamicTable, Is.Empty);
            Assert.That(afterPeerUpdate.DynamicTableLimit, Is.Zero);
            Assert.That(afterPeerUpdate.DynamicTable, Is.Empty);
        });
    }

    [Test]
    public void DecoderRetainsDynamicEntriesAcrossHeaderBlocks()
    {
        HpackDecoder decoder = new HpackDecoder();
        HpackResult inserted = decoder.DecodeHeaderBlock(
            Convert.FromHexString(
                "408825a849e95ba97d7f8925a849e95bb8e8b4bf"),
            new HpackOptions());
        HpackResult indexed = decoder.DecodeHeaderBlock(
            new byte[] { 0xbe }, new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(inserted.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(inserted.DynamicTable, Has.Count.EqualTo(1));
            Assert.That(indexed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(indexed.Headers.Fields.Single().Name,
                Is.EqualTo("custom-key"));
            Assert.That(indexed.Headers.Fields.Single().Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("custom-value")));
            Assert.That(indexed.DynamicTable, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void SettingsReductionRequiresALeadingPeerUpdate()
    {
        HpackDecoder decoder = new HpackDecoder();
        decoder.UpdateDecoderTableLimit(0);

        HpackResult missing = decoder.DecodeHeaderBlock(
            new byte[] { 0x82 }, new HpackOptions());
        HpackResult acknowledged = decoder.DecodeHeaderBlock(
            new byte[] { 0x20, 0x82 }, new HpackOptions());
        HpackResult following = decoder.DecodeHeaderBlock(
            new byte[] { 0x84 }, new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(missing.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(missing.IsCompressionError, Is.True);
            Assert.That(missing.ErrorCode,
                Is.EqualTo("required-hpack-table-size-update"));
            Assert.That(acknowledged.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(acknowledged.DynamicTableLimit, Is.Zero);
            Assert.That(following.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(following.Headers.Fields.Single().Name,
                Is.EqualTo(":path"));
        });
    }

    [Test]
    public void MultipleSettingsChangesRetainTheSmallestPendingMaximum()
    {
        HpackDecoder invalidDecoder = new HpackDecoder();
        invalidDecoder.UpdateDecoderTableLimit(0);
        invalidDecoder.UpdateDecoderTableLimit(256);
        HpackResult invalid = invalidDecoder.DecodeHeaderBlock(
            new byte[] { 0x3f, 0xe1, 0x01 }, new HpackOptions());

        HpackDecoder validDecoder = new HpackDecoder();
        validDecoder.UpdateDecoderTableLimit(0);
        validDecoder.UpdateDecoderTableLimit(256);
        HpackResult valid = validDecoder.DecodeHeaderBlock(
            new byte[] { 0x20, 0x3f, 0xe1, 0x01 }, new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(invalid.ErrorCode,
                Is.EqualTo("invalid-hpack-table-size-update-minimum"));
            Assert.That(valid.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(valid.DynamicTableLimit, Is.EqualTo(256));
        });
    }

    [Test]
    public void PeerUpdateCannotExceedTheLatestSettingsMaximum()
    {
        HpackDecoder decoder = new HpackDecoder();
        decoder.UpdateDecoderTableLimit(128);

        HpackResult result = decoder.DecodeHeaderBlock(
            new byte[] { 0x3f, 0x62 }, new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.IsCompressionError, Is.True);
            Assert.That(result.ErrorCode,
                Is.EqualTo("hpack-table-size-update-exceeds-maximum"));
            Assert.That(result.DynamicTableLimit, Is.EqualTo(4096));
        });
    }
}
