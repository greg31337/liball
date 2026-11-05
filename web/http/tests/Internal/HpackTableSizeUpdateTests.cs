namespace http.Tests;

public class HpackTableSizeUpdateTests
{
    [Test]
    public void OneOrTwoLeadingUpdatesResizeTheTableAndPermitFields()
    {
        HpackDynamicTable oneTable = new HpackDynamicTable(4096);
        HpackDynamicTable twoTable = new HpackDynamicTable(4096);

        HpackResult one = HpackHeaderBlockCodec.Decode(
            new byte[] { 0x3f, 0x61, 0x82 },
            oneTable,
            256,
            false,
            256,
            100,
            65536,
            16384);
        HpackResult two = HpackHeaderBlockCodec.Decode(
            new byte[] { 0x20, 0x3f, 0x61, 0x82 },
            twoTable,
            256,
            false,
            256,
            100,
            65536,
            16384);

        Assert.Multiple(() =>
        {
            Assert.That(one.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(one.Consumed, Is.EqualTo(3));
            Assert.That(one.DynamicTableLimit, Is.EqualTo(128));
            Assert.That(one.Headers.Fields.Single().Name, Is.EqualTo(":method"));
            Assert.That(two.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(two.Consumed, Is.EqualTo(4));
            Assert.That(two.DynamicTableLimit, Is.EqualTo(128));
            Assert.That(two.Headers.Fields, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void UpdateAfterAFieldIsACompressionError()
    {
        HpackResult result = HpackHeaderBlockCodec.Decode(
            new byte[] { 0x82, 0x20 },
            new HpackDynamicTable(4096),
            4096,
            false,
            4096,
            100,
            65536,
            16384);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.IsCompressionError, Is.True);
            Assert.That(result.ErrorPosition, Is.EqualTo(1));
            Assert.That(result.ErrorCode,
                Is.EqualTo("invalid-hpack-table-size-update-placement"));
        });
    }

    [Test]
    public void MoreThanTwoUpdatesOrDescendingUpdatesAreCompressionErrors()
    {
        HpackResult descending = HpackHeaderBlockCodec.Decode(
            new byte[] { 0x3f, 0x61, 0x20 },
            new HpackDynamicTable(4096),
            256,
            false,
            256,
            100,
            65536,
            16384);
        HpackResult three = HpackHeaderBlockCodec.Decode(
            new byte[] { 0x20, 0x21, 0x22 },
            new HpackDynamicTable(4096),
            256,
            false,
            256,
            100,
            65536,
            16384);

        Assert.Multiple(() =>
        {
            Assert.That(descending.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(descending.IsCompressionError, Is.True);
            Assert.That(descending.ErrorCode,
                Is.EqualTo("invalid-hpack-table-size-update-sequence"));
            Assert.That(three.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(three.IsCompressionError, Is.True);
            Assert.That(three.ErrorCode,
                Is.EqualTo("too-many-hpack-table-size-updates"));
        });
    }

    [Test]
    public void UpdateCannotExceedTheSettingsMaximum()
    {
        HpackResult result = HpackHeaderBlockCodec.Decode(
            new byte[] { 0x3f, 0x62 },
            new HpackDynamicTable(4096),
            128,
            false,
            128,
            100,
            65536,
            16384);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.IsCompressionError, Is.True);
            Assert.That(result.Consumed, Is.EqualTo(2));
            Assert.That(result.ErrorCode,
                Is.EqualTo("hpack-table-size-update-exceeds-maximum"));
        });
    }

    [Test]
    public void RequiredUpdateMustAcknowledgeTheSmallestPendingMaximumFirst()
    {
        HpackResult missing = HpackHeaderBlockCodec.Decode(
            new byte[] { 0x82 },
            new HpackDynamicTable(4096),
            256,
            true,
            0,
            100,
            65536,
            16384);
        HpackResult wrongFirst = HpackHeaderBlockCodec.Decode(
            new byte[] { 0x3f, 0xe1, 0x01 },
            new HpackDynamicTable(4096),
            256,
            true,
            0,
            100,
            65536,
            16384);
        HpackResult valid = HpackHeaderBlockCodec.Decode(
            new byte[] { 0x20, 0x3f, 0xe1, 0x01, 0x82 },
            new HpackDynamicTable(4096),
            256,
            true,
            0,
            100,
            65536,
            16384);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(missing.ErrorCode,
                Is.EqualTo("required-hpack-table-size-update"));
            Assert.That(wrongFirst.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(wrongFirst.ErrorCode,
                Is.EqualTo("invalid-hpack-table-size-update-minimum"));
            Assert.That(valid.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(valid.DynamicTableLimit, Is.EqualTo(256));
            Assert.That(valid.Headers.Fields, Has.Count.EqualTo(1));
        });
    }
}
