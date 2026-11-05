using System.Text;

namespace http.Tests;

public class HpackDataModelTests
{
    [Test]
    public void OptionsExposeBoundedProtocolAndEncoderDefaults()
    {
        HpackOptions options = new HpackOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.MaximumDynamicTableSize,
                Is.EqualTo(Http2Constants.DefaultHeaderTableSize));
            Assert.That(options.DynamicTableSize,
                Is.EqualTo(Http2Constants.DefaultHeaderTableSize));
            Assert.That(options.MaximumHeaderListSize, Is.EqualTo(65536));
            Assert.That(options.MaximumCompressedBlockSize, Is.EqualTo(65536));
            Assert.That(options.MaximumStringSize, Is.EqualTo(16384));
            Assert.That(options.MaximumHeaderCount, Is.EqualTo(100));
            Assert.That(options.IndexingMode, Is.EqualTo(HpackIndexingMode.Automatic));
            Assert.That(options.UseHuffman, Is.True);
            Assert.That(options.UseHuffmanOnlyWhenSmaller, Is.True);
            Assert.That(options.NeverIndexSensitiveFields, Is.True);
            Assert.That(options.Limits, Is.Not.Null);
        });
    }

    [Test]
    public void TableEntryRetainsByteExactDataAndSnapshotMetadata()
    {
        HpackTableEntry entry = new HpackTableEntry();
        byte[] name = Encoding.ASCII.GetBytes("authorization");
        byte[] value = [0x42, 0x65, 0x61, 0x72, 0x65, 0x72, 0x20, 0x80];

        entry.Name = name;
        entry.Value = value;
        entry.Size = name.Length + value.Length + 32;
        entry.HasIndex = true;
        entry.Index = 62;
        entry.IsStatic = false;
        entry.IsSensitive = true;
        entry.NeverIndex = true;

        Assert.Multiple(() =>
        {
            Assert.That(entry.Name, Is.SameAs(name));
            Assert.That(entry.Value, Is.SameAs(value));
            Assert.That(entry.Value[^1], Is.EqualTo(0x80));
            Assert.That(entry.Size, Is.EqualTo(name.Length + value.Length + 32));
            Assert.That(entry.HasIndex, Is.True);
            Assert.That(entry.Index, Is.EqualTo(62));
            Assert.That(entry.IsStatic, Is.False);
            Assert.That(entry.IsSensitive, Is.True);
            Assert.That(entry.NeverIndex, Is.True);
        });
    }

    [Test]
    public void ResultRetainsProgressOutputTableStateAndDiagnostics()
    {
        HpackResult result = new HpackResult();
        HttpHeaderField header = new HttpHeaderField();
        HpackTableEntry entry = new HpackTableEntry();
        byte[] encoded = [0x82, 0x84];

        header.Name = ":method";
        header.Value = Encoding.ASCII.GetBytes("GET");
        entry.Name = Encoding.ASCII.GetBytes("x-example");
        entry.Value = Encoding.ASCII.GetBytes("value");
        entry.Size = 46;
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = 2;
        result.Written = 0;
        result.RequiredSize = 4;
        result.Headers.Fields.Add(header);
        result.Bytes = encoded;
        result.HeaderListSize = 42;
        result.DynamicTable.Add(entry);
        result.DynamicTableSize = 46;
        result.DynamicTableLimit = 4096;
        result.HasErrorPosition = true;
        result.ErrorPosition = 1;
        result.IsCompressionError = true;
        result.ErrorCode = "invalid-index";
        result.ErrorMessage = "the indexed field does not exist";

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.Consumed, Is.EqualTo(2));
            Assert.That(result.Written, Is.Zero);
            Assert.That(result.RequiredSize, Is.EqualTo(4));
            Assert.That(result.Headers.Fields.Single(), Is.SameAs(header));
            Assert.That(result.Bytes, Is.SameAs(encoded));
            Assert.That(result.HeaderListSize, Is.EqualTo(42));
            Assert.That(result.DynamicTable.Single(), Is.SameAs(entry));
            Assert.That(result.DynamicTableSize, Is.EqualTo(46));
            Assert.That(result.DynamicTableLimit, Is.EqualTo(4096));
            Assert.That(result.HasErrorPosition, Is.True);
            Assert.That(result.ErrorPosition, Is.EqualTo(1));
            Assert.That(result.IsCompressionError, Is.True);
            Assert.That(result.ErrorCode, Is.EqualTo("invalid-index"));
        });
    }

    [Test]
    public void EmptyResultOwnsUsableEmptyContainers()
    {
        HpackResult result = new HpackResult();
        HpackTableEntry entry = new HpackTableEntry();

        Assert.Multiple(() =>
        {
            Assert.That(result.Headers.Fields, Is.Empty);
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(result.DynamicTable, Is.Empty);
            Assert.That(result.HasErrorPosition, Is.False);
            Assert.That(result.IsCompressionError, Is.False);
            Assert.That(result.ErrorCode, Is.Null);
            Assert.That(entry.Name, Is.Empty);
            Assert.That(entry.Value, Is.Empty);
            Assert.That(entry.HasIndex, Is.False);
        });
    }
}
