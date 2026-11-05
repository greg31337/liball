using System.Text;
using System.Text.Json;
using raw;

namespace raw.Tests;

public class RawSidecarCodecTests
{
    [Test]
    public void VersionOneRoundTripRetainsEveryDescriptorFieldAndArbitraryCfa()
    {
        RawDescriptor source = RawDescriptor.CreateArbitraryCfa(
            9,
            4,
            10,
            16,
            RawByteOrder.BigEndian,
            3,
            2,
            new RawCfaColor[]
            {
                RawCfaColor.Cyan,
                RawCfaColor.Magenta,
                RawCfaColor.Yellow,
                RawCfaColor.Green2,
                RawCfaColor.White,
                RawCfaColor.Red
            });
        source.BitOrder = RawBitOrder.LeastSignificantBitFirst;
        source.Layout = RawSampleLayout.Planar;
        source.Orientation = RawOrientation.Rotate90Clockwise;
        source.RowStrideBytes = 20;
        source.PlaneStrideBytes = 80;
        source.DataOffset = 7;
        source.DataLength = 83;
        source.AllowTrailingBytes = true;

        byte[] json = RawSidecarCodec.Write(new RawSidecar(RawSidecarCodec.CurrentSchemaVersion, source, null), new RawLimits());
        RawSidecar result = RawSidecarCodec.Read(json, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.SchemaVersion, Is.EqualTo(1));
            Assert.That(result.UnknownProperties, Is.Empty);
            Assert.That(result.Descriptor.Width, Is.EqualTo(source.Width));
            Assert.That(result.Descriptor.Height, Is.EqualTo(source.Height));
            Assert.That(result.Descriptor.BitDepth, Is.EqualTo(source.BitDepth));
            Assert.That(result.Descriptor.StoredWordBits, Is.EqualTo(source.StoredWordBits));
            Assert.That(result.Descriptor.SampleKind, Is.EqualTo(source.SampleKind));
            Assert.That(result.Descriptor.Packing, Is.EqualTo(source.Packing));
            Assert.That(result.Descriptor.ByteOrder, Is.EqualTo(source.ByteOrder));
            Assert.That(result.Descriptor.BitOrder, Is.EqualTo(source.BitOrder));
            Assert.That(result.Descriptor.Layout, Is.EqualTo(source.Layout));
            Assert.That(result.Descriptor.ChannelCount, Is.EqualTo(source.ChannelCount));
            Assert.That(result.Descriptor.Channels.Select(channel => channel.Name), Is.EqualTo(source.Channels.Select(channel => channel.Name)));
            Assert.That(result.Descriptor.Channels.Select(channel => channel.Kind), Is.EqualTo(source.Channels.Select(channel => channel.Kind)));
            Assert.That(result.Descriptor.Photometric, Is.EqualTo(source.Photometric));
            Assert.That((result.Descriptor.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Layout, Is.EqualTo(RawCfaLayout.Arbitrary));
            Assert.That(result.Descriptor.Cfa.RepeatWidth, Is.EqualTo(3));
            Assert.That(result.Descriptor.Cfa.RepeatHeight, Is.EqualTo(2));
            Assert.That(result.Descriptor.Cfa.Colors, Is.EqualTo((source.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Colors));
            Assert.That(result.Descriptor.Orientation, Is.EqualTo(source.Orientation));
            Assert.That(result.Descriptor.RowStrideBytes, Is.EqualTo(20));
            Assert.That(result.Descriptor.PlaneStrideBytes, Is.EqualTo(80));
            Assert.That(result.Descriptor.DataOffset, Is.EqualTo(7));
            Assert.That(result.Descriptor.DataLength, Is.EqualTo(83));
            Assert.That(result.Descriptor.AllowTrailingBytes, Is.True);
            Assert.That(result.Descriptor.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void UnknownTopLevelPropertiesSurviveReadWriteReadWithTheirJsonTypes()
    {
        Dictionary<string, JsonElement> extensions = new(StringComparer.Ordinal)
        {
            ["camera"] = Element("{\"make\":\"Example\",\"ids\":[1,2,3]}"),
            ["enabled"] = Element("true"),
            ["note"] = Element("null")
        };
        RawSidecar original = new(1, RawDescriptor.CreatePackedBayer(5, 3, RawPacking.Raw12, RawCfaLayout.Grbg), extensions);

        RawSidecar first = RawSidecarCodec.Read(RawSidecarCodec.Write(original, new RawLimits()), new RawLimits());
        RawSidecar second = RawSidecarCodec.Read(RawSidecarCodec.Write(first, new RawLimits()), new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(second.UnknownProperties.Keys, Is.EquivalentTo(new string[] { "camera", "enabled", "note" }));
            Assert.That(second.UnknownProperties["camera"].GetProperty("make").GetString(), Is.EqualTo("Example"));
            Assert.That(second.UnknownProperties["camera"].GetProperty("ids").EnumerateArray().Select(value => value.GetInt32()), Is.EqualTo(new int[] { 1, 2, 3 }));
            Assert.That(second.UnknownProperties["enabled"].GetBoolean(), Is.True);
            Assert.That(second.UnknownProperties["note"].ValueKind, Is.EqualTo(JsonValueKind.Null));
        });
    }

    [Test]
    public void SchemaAndDescriptorMembersAreStrictWhileRootExtensionsAreAllowed()
    {
        byte[] valid = RawSidecarCodec.Write(GraySidecar(), new RawLimits());
        string text = Encoding.UTF8.GetString(valid);
        byte[] future = Encoding.UTF8.GetBytes(text.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2", StringComparison.Ordinal));
        byte[] unknownDescriptor = Encoding.UTF8.GetBytes(text.Replace("\"width\": 2,", "\"width\": 2,\n    \"widht\": 2,", StringComparison.Ordinal));
        byte[] duplicateRoot = Encoding.UTF8.GetBytes(text.Substring(0, text.LastIndexOf('}')) + ",\n  \"schemaVersion\": 1\n}");
        byte[] missingVersion = Encoding.UTF8.GetBytes(text.Replace("  \"schemaVersion\": 1,\n", string.Empty, StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.Throws<RawUnsupportedException>(() => RawSidecarCodec.Read(future, new RawLimits()));
            Assert.Throws<RawFormatException>(() => RawSidecarCodec.Read(unknownDescriptor, new RawLimits()));
            Assert.Throws<RawFormatException>(() => RawSidecarCodec.Read(duplicateRoot, new RawLimits()));
            Assert.Throws<RawFormatException>(() => RawSidecarCodec.Read(missingVersion, new RawLimits()));
        });
    }

    [Test]
    public void MalformedJsonWrongTypesAndUnknownEnumNamesAreRejected()
    {
        string valid = Encoding.UTF8.GetString(RawSidecarCodec.Write(GraySidecar(), new RawLimits()));
        byte[] wrongType = Encoding.UTF8.GetBytes(valid.Replace("\"width\": 2", "\"width\": \"2\"", StringComparison.Ordinal));
        byte[] wrongEnum = Encoding.UTF8.GetBytes(valid.Replace("\"packing\": \"ByteAligned\"", "\"packing\": \"Mystery\"", StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(() => RawSidecarCodec.Read(Encoding.UTF8.GetBytes("{"), new RawLimits()), Throws.InstanceOf<JsonException>());
            Assert.Throws<RawFormatException>(() => RawSidecarCodec.Read(wrongType, new RawLimits()));
            Assert.Throws<RawFormatException>(() => RawSidecarCodec.Read(wrongEnum, new RawLimits()));
        });
    }

    [Test]
    public void InputAndOutputHonorMetadataAndAllocationLimitsExactly()
    {
        byte[] json = RawSidecarCodec.Write(GraySidecar(), new RawLimits());
        RawLimits metadataRead = new();
        metadataRead.MaxMetadataBytes = json.Length - 1;
        RawLimits allocationRead = new();
        allocationRead.MaxAllocationBytes = json.Length - 1;
        RawLimits fileRead = new();
        fileRead.MaxFileSizeBytes = json.Length - 1;
        RawLimits exact = new();
        exact.MaxMetadataBytes = json.Length;
        RawLimits metadataWrite = new();
        metadataWrite.MaxMetadataBytes = json.Length - 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawLimitException>(() => RawSidecarCodec.Read(json, metadataRead));
            Assert.Throws<RawLimitException>(() => RawSidecarCodec.Read(json, allocationRead));
            Assert.Throws<RawLimitException>(() => RawSidecarCodec.Read(json, fileRead));
            Assert.That(RawSidecarCodec.Read(json, exact).Descriptor.Width, Is.EqualTo(2));
            Assert.Throws<RawLimitException>(() => RawSidecarCodec.Write(GraySidecar(), metadataWrite));
        });
    }

    [Test]
    public void JsonDepthAndTopLevelPropertyCountUseConfiguredLimits()
    {
        Dictionary<string, JsonElement> extensions = new(StringComparer.Ordinal)
        {
            ["nested"] = Element("{\"one\":{\"two\":{\"three\":1}}}")
        };
        byte[] nested = RawSidecarCodec.Write(new RawSidecar(1, GraySidecar().Descriptor, extensions), new RawLimits());
        RawLimits depth = new();
        depth.MaxRecursionDepth = 2;
        RawLimits properties = new();
        properties.MaxTagCount = 2;

        Assert.Multiple(() =>
        {
            Assert.That(() => RawSidecarCodec.Read(nested, depth), Throws.InstanceOf<JsonException>());
            Assert.Throws<RawLimitException>(() => RawSidecarCodec.Read(nested, properties));
        });
    }

    [Test]
    public void StreamOverloadsLeaveCallerOwnedStreamsOpenAndRejectOneBytePastLimit()
    {
        byte[] json = RawSidecarCodec.Write(GraySidecar(), new RawLimits());
        using MemoryStream input = new(json);
        using MemoryStream output = new();

        RawSidecar read = RawSidecarCodec.Read(input, new RawLimits());
        RawSidecarCodec.Write(output, read, new RawLimits());
        input.Position = 0;
        output.Position = 0;
        RawLimits tooSmall = new();
        tooSmall.MaxMetadataBytes = json.Length - 1;

        Assert.Multiple(() =>
        {
            Assert.That(input.ReadByte(), Is.EqualTo(json[0]));
            Assert.That(output.ReadByte(), Is.EqualTo((byte)'{'));
            Assert.Throws<RawLimitException>(() => RawSidecarCodec.Read(new MemoryStream(json), tooSmall));
        });
    }

    [Test]
    public void InvalidDescriptorsVersionsAndReservedExtensionNamesCannotBeWritten()
    {
        RawSidecar invalidDescriptor = GraySidecar();
        invalidDescriptor.Descriptor.DataLength--;
        RawSidecar invalidVersion = GraySidecar();
        invalidVersion.SchemaVersion = 2;
        RawSidecar reserved = GraySidecar();
        reserved.UnknownProperties.Add("descriptor", Element("false"));

        Assert.Multiple(() =>
        {
            Assert.Throws<RawWriteException>(() => RawSidecarCodec.Write(invalidDescriptor, new RawLimits()));
            Assert.Throws<RawWriteException>(() => RawSidecarCodec.Write(invalidVersion, new RawLimits()));
            Assert.Throws<RawWriteException>(() => RawSidecarCodec.Write(reserved, new RawLimits()));
        });
    }

    private static RawSidecar GraySidecar()
    {
        return new RawSidecar(1, RawDescriptor.CreateGray(2, 3, 12, 16, RawByteOrder.LittleEndian), null);
    }

    private static JsonElement Element(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
