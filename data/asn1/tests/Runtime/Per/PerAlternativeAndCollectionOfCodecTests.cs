using asn1;

namespace asn1.Tests;

public class PerAlternativeAndCollectionOfCodecTests
{
    [Test]
    public void ExtensibleEnumeratedUsesRootAndNormallySmallExtensionIndexes()
    {
        PerAlternativeCodec codec = CreateAlternativeCodec();
        PerBitWriter rootWriter = CreateWriter();
        PerAlternativeResult root = codec.WriteEnumeratedIndex(
            rootWriter,
            false,
            2,
            3,
            4,
            true,
            false);
        PerBitWriteResult rootOutput = rootWriter.GetOutput();
        PerAlternativeResult rootDecoded = codec.ReadEnumeratedIndex(
            CreateReader(rootOutput),
            3,
            4,
            true,
            false);

        PerBitWriter extensionWriter = CreateWriter();
        PerAlternativeResult extension = codec.WriteEnumeratedIndex(
            extensionWriter,
            true,
            1,
            3,
            4,
            true,
            false);
        PerBitWriteResult extensionOutput = extensionWriter.GetOutput();
        PerAlternativeResult extensionDecoded = codec.ReadEnumeratedIndex(
            CreateReader(extensionOutput),
            3,
            4,
            true,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(root.Success, Is.True);
            Assert.That(rootOutput.Data, Is.EqualTo(new byte[] { 0x40 }));
            Assert.That(rootOutput.WrittenBitCount, Is.EqualTo(3));
            Assert.That(rootDecoded.Extension, Is.False);
            Assert.That(rootDecoded.Index, Is.EqualTo(2));
            Assert.That(extension.Success, Is.True);
            Assert.That(extensionOutput.Data,
                Is.EqualTo(new byte[] { 0x81 }));
            Assert.That(extensionOutput.WrittenBitCount, Is.EqualTo(8));
            Assert.That(extensionDecoded.Extension, Is.True);
            Assert.That(extensionDecoded.Index, Is.EqualTo(1));
        });
    }

    [Test]
    public void RootChoiceWritesSelectedPayloadDirectly()
    {
        PerAlternativeCodec codec = CreateAlternativeCodec();
        PerBitWriter writer = CreateWriter();
        PerAlternativeResult encoded = codec.WriteChoiceAlternative(
            writer,
            false,
            1,
            3,
            2,
            true,
            [0xa0],
            3,
            false);
        PerBitWriteResult output = writer.GetOutput();
        PerAlternativeResult decoded = codec.ReadChoiceAlternative(
            CreateReader(output),
            3,
            2,
            true,
            [1, 3, 2],
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(output.Data, Is.EqualTo(new byte[] { 0x34 }));
            Assert.That(output.WrittenBitCount, Is.EqualTo(6));
            Assert.That(decoded.Extension, Is.False);
            Assert.That(decoded.Index, Is.EqualTo(1));
            Assert.That(decoded.Data, Is.EqualTo(new byte[] { 0xa0 }));
            Assert.That(decoded.ContentBitCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void ExtensionChoiceWrapsSelectedPayloadAsOpenType()
    {
        PerAlternativeCodec codec = CreateAlternativeCodec();
        PerBitWriter writer = CreateWriter();
        PerAlternativeResult encoded = codec.WriteChoiceAlternative(
            writer,
            true,
            2,
            3,
            4,
            true,
            [0xa0],
            3,
            false);
        PerBitWriteResult output = writer.GetOutput();
        PerAlternativeResult decoded = codec.ReadChoiceAlternative(
            CreateReader(output),
            3,
            4,
            true,
            [1, 3, 2],
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(output.Data,
                Is.EqualTo(new byte[] { 0x82, 0x01, 0xa0 }));
            Assert.That(output.WrittenBitCount, Is.EqualTo(24));
            Assert.That(decoded.Extension, Is.True);
            Assert.That(decoded.Index, Is.EqualTo(2));
            Assert.That(decoded.Data, Is.EqualTo(new byte[] { 0xa0 }));
            Assert.That(decoded.ContentBitCount, Is.EqualTo(8));
        });
    }

    [Test]
    public void SequenceOfUsesConstrainedElementLengthAndExactElementBits()
    {
        PerCollectionOfCodec codec = CreateCollectionCodec();
        PerBitWriter writer = CreateWriter();
        PerCollectionOfResult encoded = codec.WriteCollection(
            writer,
            [[0x80], [0x00], [0xc0]],
            [1, 1, 2],
            0,
            true,
            7,
            false,
            false);
        PerBitWriteResult output = writer.GetOutput();
        PerCollectionOfResult decoded = codec.ReadCollection(
            CreateReader(output),
            [1, 1, 2],
            0,
            true,
            7,
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(output.Data, Is.EqualTo(new byte[] { 0x76 }));
            Assert.That(output.WrittenBitCount, Is.EqualTo(7));
            Assert.That(decoded.ElementData[0],
                Is.EqualTo(new byte[] { 0x80 }));
            Assert.That(decoded.ElementData[1],
                Is.EqualTo(new byte[] { 0x00 }));
            Assert.That(decoded.ElementData[2],
                Is.EqualTo(new byte[] { 0xc0 }));
            Assert.That(decoded.FragmentLengths, Is.EqualTo(new[] { 3 }));
        });
    }

    [Test]
    public void SequenceOfFragmentsAtSixteenKElementBoundaries()
    {
        int count = 16385;
        byte[][] encodings = new byte[count][];
        int[] bitCounts = new int[count];
        int index = 0;
        while (index < count)
        {
            encodings[index] = [];
            index = index + 1;
        }

        PerCollectionOfCodec codec = CreateCollectionCodec();
        PerBitWriter writer = CreateWriter();
        PerCollectionOfResult encoded = codec.WriteCollection(
            writer,
            encodings,
            bitCounts,
            0,
            false,
            0,
            false,
            false);
        PerBitWriteResult output = writer.GetOutput();
        PerCollectionOfResult decoded = codec.ReadCollection(
            CreateReader(output),
            bitCounts,
            0,
            false,
            0,
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.FragmentLengths,
                Is.EqualTo(new[] { 16384, 1 }));
            Assert.That(output.Data,
                Is.EqualTo(new byte[] { 0xc1, 0x01 }));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.FragmentLengths,
                Is.EqualTo(new[] { 16384, 1 }));
        });
    }

    [Test]
    public void CanonicalSetOfSortsAndRejectsOutOfOrderEncodings()
    {
        PerCollectionOfCodec codec = CreateCollectionCodec();
        byte[][] encodings = [[0x80], [0x00], [0x40]];
        int[] bitCounts = [1, 1, 2];
        PerBitWriter canonicalWriter = CreateWriter();
        PerCollectionOfResult canonical = codec.WriteCollection(
            canonicalWriter,
            encodings,
            bitCounts,
            3,
            true,
            3,
            false,
            true);
        PerBitWriteResult canonicalOutput = canonicalWriter.GetOutput();
        PerCollectionOfResult decoded = codec.ReadCollection(
            CreateReader(canonicalOutput),
            [1, 2, 1],
            3,
            true,
            3,
            false,
            true);

        PerBitWriter sourceWriter = CreateWriter();
        codec.WriteCollection(
            sourceWriter,
            encodings,
            bitCounts,
            3,
            true,
            3,
            false,
            false);
        PerCollectionOfResult rejected = codec.ReadCollection(
            CreateReader(sourceWriter.GetOutput()),
            bitCounts,
            3,
            true,
            3,
            false,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(canonical.Success, Is.True);
            Assert.That(canonical.Order, Is.EqualTo(new[] { 1, 2, 0 }));
            Assert.That(canonicalOutput.Data,
                Is.EqualTo(new byte[] { 0x30 }));
            Assert.That(canonicalOutput.WrittenBitCount, Is.EqualTo(4));
            Assert.That(decoded.Success, Is.True);
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.ErrorCode, Is.EqualTo("ASNP0070"));
        });
    }

    private static PerAlternativeCodec CreateAlternativeCodec()
    {
        return new PerAlternativeCodec(new AsnResourceLimits());
    }

    private static PerCollectionOfCodec CreateCollectionCodec()
    {
        return new PerCollectionOfCodec(new AsnResourceLimits());
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
}
