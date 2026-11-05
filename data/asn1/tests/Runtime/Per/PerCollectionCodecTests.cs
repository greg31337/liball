using asn1;

namespace asn1.Tests;

public class PerCollectionCodecTests
{
    [Test]
    public void RootPreamblePlacesExtensionBitBeforePresenceMap()
    {
        PerCollectionCodec codec = CreateCollectionCodec();
        PerBitWriter writer = CreateWriter();
        PerCollectionResult encoded = codec.WriteRootPreamble(
            writer,
            true,
            true,
            [true, false, true]);
        PerBitWriteResult output = writer.GetOutput();
        PerCollectionResult decoded = codec.ReadRootPreamble(
            CreateReader(output),
            true,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.BitCount, Is.EqualTo(4));
            Assert.That(output.Data, Is.EqualTo(new byte[] { 0xd0 }));
            Assert.That(output.WrittenBitCount, Is.EqualTo(4));
            Assert.That(decoded.ExtensionPresent, Is.True);
            Assert.That(decoded.RootPresence,
                Is.EqualTo(new bool[] { true, false, true }));
        });
    }

    [Test]
    public void RootPresenceOmitsValuesEqualToDefaults()
    {
        PerCollectionCodec codec = CreateCollectionCodec();
        AsnTypeDescriptor booleanType = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Boolean
        };
        AsnFieldDescriptor optional = new AsnFieldDescriptor
        {
            Name = "optional",
            Optional = true,
            Type = booleanType
        };
        AsnFieldDescriptor equalDefault = CreateBooleanDefaultField(
            "equal",
            booleanType,
            true);
        AsnFieldDescriptor changedDefault = CreateBooleanDefaultField(
            "changed",
            booleanType,
            false);
        AsnRuntimeFieldValue[] values =
        [
            CreateBooleanField("optional", true),
            CreateBooleanField("equal", true),
            CreateBooleanField("changed", true)
        ];
        PerCollectionResult presence = codec.BuildRootPresence(
            [optional, equalDefault, changedDefault],
            values,
            new AsnReflectionCatalog());

        Assert.Multiple(() =>
        {
            Assert.That(presence.Success, Is.True);
            Assert.That(presence.RootPresence,
                Is.EqualTo(new bool[] { true, false, true }));
            Assert.That(codec.MatchesDefault(
                equalDefault,
                values[1].Value!,
                new AsnReflectionCatalog()), Is.True);
            Assert.That(codec.MatchesDefault(
                changedDefault,
                values[2].Value!,
                new AsnReflectionCatalog()), Is.False);
        });
    }

    [Test]
    public void ExtensionGroupsCollapseComponentPresenceByAdditionIndex()
    {
        PerCollectionCodec codec = CreateCollectionCodec();
        PerCollectionResult result = codec.BuildExtensionPresence(
            [false, true, false, true],
            [0, 1, 1, 2],
            3);
        PerCollectionResult allMissingGroup = codec.BuildExtensionPresence(
            [true, false, false, false],
            [0, 1, 1, 2],
            3);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ExtensionPresent, Is.True);
            Assert.That(result.ExtensionPresence,
                Is.EqualTo(new bool[] { false, true, true }));
            Assert.That(allMissingGroup.ExtensionPresence,
                Is.EqualTo(new bool[] { true, false, false }));
        });
    }

    [Test]
    public void SmallExtensionBitmapUsesNormallySmallLength()
    {
        PerCollectionCodec codec = CreateCollectionCodec();
        PerBitWriter writer = CreateWriter();
        PerCollectionResult encoded = codec.WriteExtensionBitmap(
            writer,
            [true, false, true],
            false);
        PerBitWriteResult output = writer.GetOutput();
        PerCollectionResult decoded = codec.ReadExtensionBitmap(
            CreateReader(output),
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(output.Data,
                Is.EqualTo(new byte[] { 0x05, 0x40 }));
            Assert.That(output.WrittenBitCount, Is.EqualTo(10));
            Assert.That(decoded.ExtensionPresence,
                Is.EqualTo(new bool[] { true, false, true }));
            Assert.That(decoded.BitCount, Is.EqualTo(10));
        });
    }

    [Test]
    public void LargeExtensionBitmapUsesLongNormallySmallForm()
    {
        bool[] presence = new bool[65];
        presence[0] = true;
        presence[64] = true;
        PerCollectionCodec codec = CreateCollectionCodec();
        PerBitWriter writer = CreateWriter();
        PerCollectionResult encoded = codec.WriteExtensionBitmap(
            writer,
            presence,
            false);
        PerBitWriteResult output = writer.GetOutput();
        PerCollectionResult decoded = codec.ReadExtensionBitmap(
            CreateReader(output),
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(output.WrittenBitCount, Is.EqualTo(74));
            Assert.That((output.Data[0] & 0x80), Is.EqualTo(0x80));
            Assert.That(decoded.ExtensionPresence, Is.EqualTo(presence));
            Assert.That(decoded.BitCount, Is.EqualTo(74));
        });
    }

    [Test]
    public void OpenTypeCreatesCompleteEncodingAndAlignedWrapper()
    {
        PerOpenTypeCodec codec = CreateOpenTypeCodec();
        PerOpenTypeResult complete = codec.CompleteEncoding([0xff], 2);
        PerOpenTypeResult empty = codec.CompleteEncoding([], 0);
        PerBitWriter writer = CreateWriter();
        writer.WriteBit(true);
        PerOpenTypeResult encoded = codec.WriteOpenType(
            writer,
            [0xff],
            2,
            true);
        PerBitWriteResult output = writer.GetOutput();
        PerBitReader reader = CreateReader(output);
        reader.ReadBit();
        PerOpenTypeResult decoded = codec.ReadOpenType(reader, true);

        Assert.Multiple(() =>
        {
            Assert.That(complete.Data, Is.EqualTo(new byte[] { 0xc0 }));
            Assert.That(complete.PaddingBitCount, Is.EqualTo(6));
            Assert.That(empty.Data, Is.EqualTo(new byte[] { 0x00 }));
            Assert.That(empty.PaddingBitCount, Is.EqualTo(8));
            Assert.That(encoded.Success, Is.True);
            Assert.That(output.Data,
                Is.EqualTo(new byte[] { 0x80, 0x01, 0xc0 }));
            Assert.That(decoded.Data, Is.EqualTo(new byte[] { 0xc0 }));
        });
    }

    [Test]
    public void ExtensionSectionWrapsEachPresentAdditionAsOpenType()
    {
        PerCollectionCodec codec = CreateCollectionCodec();
        bool[] presence = [true, false, true];
        byte[][] encodings = [[0x80], [], [0xa0]];
        int[] bitCounts = [1, 0, 3];
        PerBitWriter writer = CreateWriter();
        PerCollectionResult encoded = codec.WriteExtensionSection(
            writer,
            presence,
            encodings,
            bitCounts,
            false);
        PerBitWriteResult output = writer.GetOutput();
        PerCollectionResult decoded = codec.ReadExtensionSection(
            CreateReader(output),
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.BitCount, Is.EqualTo(42));
            Assert.That(decoded.ExtensionPresence, Is.EqualTo(presence));
            Assert.That(decoded.ExtensionData[0],
                Is.EqualTo(new byte[] { 0x80 }));
            Assert.That(decoded.ExtensionData[1], Is.Empty);
            Assert.That(decoded.ExtensionData[2],
                Is.EqualTo(new byte[] { 0xa0 }));
            Assert.That(decoded.BitCount, Is.EqualTo(42));
        });
    }

    [Test]
    public void ExtensionSectionRejectsAnEmptyPresenceMapBeforeWriting()
    {
        PerCollectionCodec codec = CreateCollectionCodec();
        PerBitWriter writer = CreateWriter();
        PerCollectionResult encoded = codec.WriteExtensionSection(
            writer,
            [false, false],
            [[], []],
            [0, 0],
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.False);
            Assert.That(encoded.ErrorCode, Is.EqualTo("ASNP0062"));
            Assert.That(writer.GetWrittenBitCount(), Is.Zero);
        });
    }

    [Test]
    public void ExtensionGroupPreambleCanBeWrappedAsOneAddition()
    {
        PerCollectionCodec codec = CreateCollectionCodec();
        PerBitWriter groupWriter = CreateWriter();
        codec.WriteRootPreamble(
            groupWriter,
            false,
            false,
            [true, false]);
        groupWriter.WriteBits(3, 2);
        PerBitWriteResult group = groupWriter.GetOutput();

        PerBitWriter outerWriter = CreateWriter();
        codec.WriteExtensionSection(
            outerWriter,
            [true],
            [group.Data],
            [group.WrittenBitCount],
            false);
        PerBitWriteResult outer = outerWriter.GetOutput();
        PerCollectionResult decoded = codec.ReadExtensionSection(
            CreateReader(outer),
            false);

        Assert.Multiple(() =>
        {
            Assert.That(group.WrittenBitCount, Is.EqualTo(4));
            Assert.That(decoded.ExtensionPresence,
                Is.EqualTo(new bool[] { true }));
            Assert.That(decoded.ExtensionData[0],
                Is.EqualTo(new byte[] { 0xb0 }));
        });
    }

    private static PerCollectionCodec CreateCollectionCodec()
    {
        return new PerCollectionCodec(new AsnResourceLimits());
    }

    private static PerOpenTypeCodec CreateOpenTypeCodec()
    {
        return new PerOpenTypeCodec(new AsnResourceLimits());
    }

    private static PerBitWriter CreateWriter()
    {
        return new PerBitWriter(new AsnResourceLimits());
    }

    private static PerBitReader CreateReader(PerBitWriteResult output)
    {
        PerBitReader reader = new PerBitReader(new AsnResourceLimits());
        reader.AppendInputBits(
            output.Data,
            0,
            output.WrittenBitCount,
            true);
        return reader;
    }

    private static AsnFieldDescriptor CreateBooleanDefaultField(
        string name,
        AsnTypeDescriptor type,
        bool defaultValue)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            HasDefaultValue = true,
            Type = type,
            DefaultValue = new AsnValueDescriptor
            {
                Kind = AsnValueKind.Boolean,
                Value = new AsnValue
                {
                    Kind = AsnValueKind.Boolean,
                    TextValue = defaultValue ? "TRUE" : "FALSE"
                }
            }
        };
    }

    private static AsnRuntimeFieldValue CreateBooleanField(
        string name,
        bool value)
    {
        return new AsnRuntimeFieldValue
        {
            Name = name,
            Present = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Boolean,
                HasBooleanValue = true,
                BooleanValue = value
            }
        };
    }
}
