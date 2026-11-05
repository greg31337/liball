using token;

namespace token.Tests;

public class CborMapSorterTests
{
    [Test]
    public void SortOrdersByEncodedLengthThenLexicographically()
    {
        CborMapEntry integer100 = Entry(Unsigned(100), Text("integer100"));
        CborMapEntry textA = Entry(Text("a"), Text("textA"));
        CborMapEntry emptyText = Entry(Text(string.Empty), Text("emptyText"));
        CborMapEntry integer10 = Entry(Unsigned(10), Text("integer10"));
        CborMapEntry emptyBytes = Entry(Bytes(Array.Empty<byte>()), Text("emptyBytes"));
        CborMapEntry[] entries = new CborMapEntry[] { integer100, textA, emptyText, integer10, emptyBytes };

        CborMapSortResult result = CborMapSorter.Sort(entries);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Entries, Is.EqualTo(new CborMapEntry[]
            {
                integer10,
                emptyBytes,
                emptyText,
                integer100,
                textA
            }));
            Assert.That(entries, Is.EqualTo(new CborMapEntry[] { integer100, textA, emptyText, integer10, emptyBytes }));
        });
    }

    [Test]
    public void SortUsesUnsignedLexicographicByteComparisonForEqualLengths()
    {
        CborMapEntry simpleFalse = Entry(Boolean(false), Null());
        CborMapEntry emptyText = Entry(Text(string.Empty), Null());
        CborMapEntry minusOne = Entry(Negative(0), Null());
        CborMapEntry one = Entry(Unsigned(1), Null());

        CborMapSortResult result = CborMapSorter.Sort(new CborMapEntry[]
        {
            simpleFalse,
            emptyText,
            minusOne,
            one
        });

        Assert.That(result.Entries, Is.EqualTo(new CborMapEntry[] { one, minusOne, emptyText, simpleFalse }));
    }

    [Test]
    public void SortUsesDeterministicEncodingForNestedContainerKeys()
    {
        CborItem firstMapKey = Map(new CborMapEntry[]
        {
            Entry(Text("b"), Unsigned(1)),
            Entry(Text("a"), Unsigned(2))
        });
        CborItem secondMapKey = Map(new CborMapEntry[]
        {
            Entry(Text("a"), Unsigned(2)),
            Entry(Text("b"), Unsigned(1))
        });
        CborMapEntry first = Entry(firstMapKey, Text("first"));
        CborMapEntry second = Entry(secondMapKey, Text("second"));

        CborMapSortResult result = CborMapSorter.Sort(new CborMapEntry[] { first, second });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Entries[0], Is.SameAs(first));
            Assert.That(result.Entries[1], Is.SameAs(second));
        });
    }

    [Test]
    public void DeterministicMapWritingMatchesRfc8949Ordering()
    {
        CborMapEntry[] entries = new CborMapEntry[]
        {
            Entry(Unsigned(100), Null()),
            Entry(Text("a"), Null()),
            Entry(Text(string.Empty), Null()),
            Entry(Unsigned(10), Null()),
            Entry(Bytes(Array.Empty<byte>()), Null())
        };
        CborWriter writer = CborWriter.Create();

        TokenResult result = writer.WriteDeterministicMap(entries);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[]
            {
                0xA5,
                0x0A, 0xF6,
                0x40, 0xF6,
                0x60, 0xF6,
                0x18, 0x64, 0xF6,
                0x61, 0x61, 0xF6
            }));
        });
    }

    [Test]
    public void DeterministicItemRecursivelySortsNestedMaps()
    {
        CborItem nestedMap = Map(new CborMapEntry[]
        {
            Entry(Text("z"), Unsigned(1)),
            Entry(Text("a"), Unsigned(2))
        });
        CborItem rootMap = Map(new CborMapEntry[]
        {
            Entry(Text("nested"), nestedMap)
        });
        CborWriter writer = CborWriter.Create();

        writer.WriteDeterministicItem(rootMap);

        CborReadResult read = CborReader.Create(writer.ToArray()).Read();
        Assert.Multiple(() =>
        {
            Assert.That(read.Item.MapValue[0].Value.MapValue[0].Key.Text, Is.EqualTo("a"));
            Assert.That(read.Item.MapValue[0].Value.MapValue[1].Key.Text, Is.EqualTo("z"));
        });
    }

    [Test]
    public void OrdinaryMapWritingStillPreservesSuppliedOrder()
    {
        CborMapEntry[] entries = new CborMapEntry[]
        {
            Entry(Text("z"), Unsigned(1)),
            Entry(Text("a"), Unsigned(2))
        };
        CborWriter writer = CborWriter.Create();

        writer.WriteMap(entries);

        CborReadResult read = CborReader.Create(writer.ToArray()).Read();
        Assert.That(read.Item.MapValue.Select(entry => entry.Key.Text), Is.EqualTo(new string[] { "z", "a" }));
    }

    [Test]
    public void SortKeepsEqualEncodedKeysStableForLaterDuplicateValidation()
    {
        CborMapEntry first = Entry(Unsigned(1), Text("first"));
        CborMapEntry second = Entry(Unsigned(1), Text("second"));

        CborMapSortResult result = CborMapSorter.Sort(new CborMapEntry[] { first, second });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Entries[0], Is.SameAs(first));
            Assert.That(result.Entries[1], Is.SameAs(second));
        });
    }

    [Test]
    public void SortAndDeterministicWriteRejectInvalidEntriesAtomically()
    {
        CborMapEntry[] entries = new CborMapEntry[] { Entry(Unsigned(1), Null()), null };
        CborMapSortResult sorted = CborMapSorter.Sort(entries);
        CborWriter writer = CborWriter.Create();
        writer.WriteNull();

        TokenResult written = writer.WriteDeterministicMap(entries);

        Assert.Multiple(() =>
        {
            Assert.That(sorted.Success, Is.False);
            Assert.That(sorted.Error.Code, Is.EqualTo("invalid_cbor_map"));
            Assert.That(sorted.Error.Position, Is.EqualTo(1));
            Assert.That(written.Success, Is.False);
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xF6 }));
        });
    }

    [Test]
    public void SortRejectsNullArray()
    {
        Assert.That(() => CborMapSorter.Sort(null), Throws.ArgumentNullException);
    }

    static CborMapEntry Entry(CborItem key, CborItem value)
    {
        return new CborMapEntry(key, value);
    }

    static CborItem Unsigned(ulong value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.UnsignedInteger;
        item.IntegerArgument = value;
        return item;
    }

    static CborItem Negative(ulong argument)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.NegativeInteger;
        item.IntegerArgument = argument;
        return item;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static CborItem Boolean(bool value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Boolean;
        item.BooleanValue = value;
        return item;
    }

    static CborItem Null()
    {
        return new CborItem();
    }

    static CborItem Map(CborMapEntry[] entries)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = entries;
        return item;
    }
}
