using token;

namespace token.Tests;

public class CborLimitTests
{
    [Test]
    public void ReaderEnforcesDepthAcrossArraysMapsAndTags()
    {
        byte[] encoded = new byte[] { 0x81, 0xA1, 0x00, 0xC0, 0x81, 0x01 };
        CborReader allowedReader = CborReader.Create(encoded, encoded.Length, 4);
        CborReader rejectedReader = CborReader.Create(encoded, encoded.Length, 3);

        CborReadResult allowed = allowedReader.Read();
        CborReadResult rejected = rejectedReader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Success, Is.True);
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Error.Code, Is.EqualTo("cbor_depth_exceeded"));
            Assert.That(rejected.Error.Position, Is.EqualTo(4));
            Assert.That(rejectedReader.GetPosition(), Is.Zero);
        });
    }

    [Test]
    public void ReaderEnforcesDepthForIndefiniteContainers()
    {
        byte[] encoded = new byte[] { 0x9F, 0x9F, 0x01, 0xFF, 0xFF };
        CborReader reader = CborReader.Create(encoded, encoded.Length, 1);

        CborReadResult result = reader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(result.Error.Code, Is.EqualTo("cbor_depth_exceeded"));
            Assert.That(result.Error.Position, Is.EqualTo(1));
            Assert.That(reader.GetPosition(), Is.Zero);
        });
    }

    [Test]
    public void ReaderResetsDepthForEachSequentialTopLevelItem()
    {
        byte[] encoded = new byte[] { 0x81, 0x01, 0x81, 0x02 };
        CborReader reader = CborReader.Create(encoded, encoded.Length, 1);

        CborReadResult first = reader.Read();
        CborReadResult second = reader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(second.Item.ArrayValue[0].IntegerArgument, Is.EqualTo(2));
            Assert.That(reader.GetRemainingLength(), Is.Zero);
        });
    }

    [Test]
    public void ReaderAppliesEncodedSizeBeforeRecursiveParsing()
    {
        byte[] encoded = new byte[] { 0x81, 0x01 };
        CborReader reader = CborReader.Create(encoded, 1, 10);

        CborReadResult result = reader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(result.Error.Code, Is.EqualTo("cbor_size_exceeded"));
            Assert.That(result.Error.Position, Is.EqualTo(1));
            Assert.That(reader.GetPosition(), Is.Zero);
        });
    }

    [Test]
    public void ZeroDepthAllowsScalarsAndRejectsContainers()
    {
        CborReadResult scalarRead = CborReader.Create(new byte[] { 0x01 }, 1, 0).Read();
        CborReadResult arrayRead = CborReader.Create(new byte[] { 0x80 }, 1, 0).Read();
        CborWriter scalarWriter = CborWriter.Create(10, 0);
        CborWriter arrayWriter = CborWriter.Create(10, 0);

        TokenResult scalarWrite = scalarWriter.WriteItem(Unsigned(1));
        TokenResult arrayWrite = arrayWriter.WriteItem(ArrayItem(Unsigned(1)));

        Assert.Multiple(() =>
        {
            Assert.That(scalarRead.Success, Is.True);
            Assert.That(arrayRead.Error.Code, Is.EqualTo("cbor_depth_exceeded"));
            Assert.That(scalarWrite.Success, Is.True);
            Assert.That(arrayWrite.Error.Code, Is.EqualTo("cbor_depth_exceeded"));
            Assert.That(arrayWriter.GetLength(), Is.Zero);
        });
    }

    [Test]
    public void WriterEnforcesDepthAtomicallyAcrossArraysMapsAndTags()
    {
        CborItem item = ArrayItem(Map(new CborMapEntry[]
        {
            new CborMapEntry(Unsigned(0), Tag(0, ArrayItem(Unsigned(1))))
        }));
        CborWriter allowedWriter = CborWriter.Create(100, 4);
        CborWriter rejectedWriter = CborWriter.Create(100, 3);
        rejectedWriter.WriteNull();

        TokenResult allowed = allowedWriter.WriteItem(item);
        TokenResult rejected = rejectedWriter.WriteItem(item);

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Success, Is.True);
            Assert.That(rejected.Error.Code, Is.EqualTo("cbor_depth_exceeded"));
            Assert.That(rejected.Error.Position, Is.EqualTo(4));
            Assert.That(rejectedWriter.ToArray(), Is.EqualTo(new byte[] { 0xF6 }));
        });
    }

    [Test]
    public void WriterStopsCyclicModelsAtTheRecursionLimit()
    {
        CborItem cyclic = new CborItem();
        cyclic.Kind = TokenCborValueKind.Array;
        cyclic.ArrayValue = new CborItem[] { cyclic };
        CborWriter writer = CborWriter.Create();

        TokenResult result = writer.WriteItem(cyclic);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("cbor_depth_exceeded"));
            Assert.That(writer.GetLength(), Is.Zero);
        });
    }

    [Test]
    public void DefaultReaderStopsAtItsRecursionLimit()
    {
        byte[] encoded = new byte[66];
        int index = 0;
        while (index < 65)
        {
            encoded[index] = 0x81;
            index += 1;
        }

        encoded[65] = 0x00;

        CborReadResult result = CborReader.Create(encoded).Read();

        Assert.Multiple(() =>
        {
            Assert.That(result.Error.Code, Is.EqualTo("cbor_depth_exceeded"));
            Assert.That(result.Error.Position, Is.EqualTo(64));
        });
    }

    [Test]
    public void MapHelpersStopCyclicKeysAtTheirDepthLimit()
    {
        CborItem cyclic = new CborItem();
        cyclic.Kind = TokenCborValueKind.Array;
        cyclic.ArrayValue = new CborItem[] { cyclic };
        CborMapEntry[] entries = new CborMapEntry[] { new CborMapEntry(cyclic, Unsigned(1)) };

        bool equal = CborMapKeyComparer.AreEqual(cyclic, cyclic, 2);
        CborMapSortResult sorted = CborMapSorter.Sort(entries, 2);

        Assert.Multiple(() =>
        {
            Assert.That(equal, Is.False);
            Assert.That(sorted.Success, Is.False);
            Assert.That(sorted.Error.Code, Is.EqualTo("cbor_depth_exceeded"));
        });
    }

    [Test]
    public void DeterministicWriterAppliesDepthLimitWhileEncodingMapKeys()
    {
        CborItem key = ArrayItem(ArrayItem(Unsigned(1)));
        CborMapEntry[] entries = new CborMapEntry[] { new CborMapEntry(key, Unsigned(2)) };
        CborWriter writer = CborWriter.Create(100, 2);
        writer.WriteNull();

        TokenResult result = writer.WriteDeterministicMap(entries);

        Assert.Multiple(() =>
        {
            Assert.That(result.Error.Code, Is.EqualTo("cbor_depth_exceeded"));
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xF6 }));
        });
    }

    [Test]
    public void ContainerTemporaryEncodingUsesRemainingOutputSize()
    {
        CborWriter writer = CborWriter.Create(3, 10);
        writer.WriteNull();

        TokenResult result = writer.WriteItem(ArrayItem(Unsigned(1), Unsigned(2)));

        Assert.Multiple(() =>
        {
            Assert.That(result.Error.Code, Is.EqualTo("buffer_limit_exceeded"));
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xF6 }));
        });
    }

    [Test]
    public void ExplicitLimitApisRejectNegativeLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => CborReader.Create(Array.Empty<byte>(), 0, -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => CborWriter.Create(0, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => CborMapKeyComparer.AreEqual(Unsigned(1), Unsigned(1), -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => CborMapKeyComparer.Validate(Array.Empty<CborMapEntry>(), -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => CborMapSorter.Sort(Array.Empty<CborMapEntry>(), -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    static CborItem Unsigned(ulong value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.UnsignedInteger;
        item.IntegerArgument = value;
        return item;
    }

    static CborItem ArrayItem(params CborItem[] items)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = items;
        return item;
    }

    static CborItem Map(CborMapEntry[] entries)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = entries;
        return item;
    }

    static CborItem Tag(ulong tag, CborItem value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Tag;
        item.TagValue = tag;
        item.TaggedValue = value;
        return item;
    }
}
