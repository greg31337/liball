using System.Text;

namespace http.Tests;

public class HpackStaticTableLookupTests
{
    [Test]
    public void IndexLookupReturnsEveryOneBasedStaticEntry()
    {
        Assert.Multiple(() =>
        {
            for (int index = 1; index <= HpackStaticTable.Names.Length; index++)
            {
                byte[] expectedName = Encoding.ASCII.GetBytes(
                    HpackStaticTable.Names[index - 1]);
                byte[] expectedValue = Encoding.ASCII.GetBytes(
                    HpackStaticTable.Values[index - 1]);
                HpackTableEntry entry = HpackStaticTableLookup.LookupByIndex(index);

                Assert.That(entry.HasIndex, Is.True,
                    $"Static index {index} was not found.");
                Assert.That(entry.Index, Is.EqualTo(index));
                Assert.That(entry.IsStatic, Is.True);
                Assert.That(entry.Name, Is.EqualTo(expectedName));
                Assert.That(entry.Value, Is.EqualTo(expectedValue));
                Assert.That(entry.Size,
                    Is.EqualTo(expectedName.Length + expectedValue.Length + 32));
                Assert.That(entry.IsSensitive, Is.False);
                Assert.That(entry.NeverIndex, Is.False);
            }
        });
    }

    [Test]
    public void IndexLookupRejectsZeroNegativeAndOutOfRangeIndexes()
    {
        HpackTableEntry zero = HpackStaticTableLookup.LookupByIndex(0);
        HpackTableEntry negative = HpackStaticTableLookup.LookupByIndex(-1);
        HpackTableEntry minimum = HpackStaticTableLookup.LookupByIndex(int.MinValue);
        HpackTableEntry tooLarge = HpackStaticTableLookup.LookupByIndex(
            HpackStaticTable.Names.Length + 1);
        HpackTableEntry maximum = HpackStaticTableLookup.LookupByIndex(int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(zero.HasIndex, Is.False);
            Assert.That(negative.HasIndex, Is.False);
            Assert.That(minimum.HasIndex, Is.False);
            Assert.That(tooLarge.HasIndex, Is.False);
            Assert.That(maximum.HasIndex, Is.False);
            Assert.That(zero.Name, Is.Empty);
            Assert.That(zero.Value, Is.Empty);
            Assert.That(zero.IsStatic, Is.False);
        });
    }

    [Test]
    public void ExactLookupFindsEveryNameAndValuePair()
    {
        Assert.Multiple(() =>
        {
            for (int index = 1; index <= HpackStaticTable.Names.Length; index++)
            {
                byte[] name = Encoding.ASCII.GetBytes(
                    HpackStaticTable.Names[index - 1]);
                byte[] value = Encoding.ASCII.GetBytes(
                    HpackStaticTable.Values[index - 1]);

                HpackTableEntry entry = HpackStaticTableLookup.FindExact(name, value);

                Assert.That(entry.HasIndex, Is.True,
                    $"Static pair at index {index} was not found.");
                Assert.That(entry.Index, Is.EqualTo(index));
            }
        });
    }

    [Test]
    public void ExactLookupDistinguishesDuplicateNamesByValue()
    {
        HpackTableEntry get = HpackStaticTableLookup.FindExact(
            ":method"u8, "GET"u8);
        HpackTableEntry post = HpackStaticTableLookup.FindExact(
            ":method"u8, "POST"u8);
        HpackTableEntry slash = HpackStaticTableLookup.FindExact(
            ":path"u8, "/"u8);
        HpackTableEntry indexHtml = HpackStaticTableLookup.FindExact(
            ":path"u8, "/index.html"u8);

        Assert.Multiple(() =>
        {
            Assert.That(get.Index, Is.EqualTo(2));
            Assert.That(post.Index, Is.EqualTo(3));
            Assert.That(slash.Index, Is.EqualTo(4));
            Assert.That(indexHtml.Index, Is.EqualTo(5));
        });
    }

    [Test]
    public void NameLookupChoosesTheLowestStaticIndex()
    {
        HpackTableEntry method = HpackStaticTableLookup.FindName(":method"u8);
        HpackTableEntry path = HpackStaticTableLookup.FindName(":path"u8);
        HpackTableEntry scheme = HpackStaticTableLookup.FindName(":scheme"u8);
        HpackTableEntry status = HpackStaticTableLookup.FindName(":status"u8);
        HpackTableEntry encoding = HpackStaticTableLookup.FindName(
            "accept-encoding"u8);

        Assert.Multiple(() =>
        {
            Assert.That(method.Index, Is.EqualTo(2));
            Assert.That(path.Index, Is.EqualTo(4));
            Assert.That(scheme.Index, Is.EqualTo(6));
            Assert.That(status.Index, Is.EqualTo(8));
            Assert.That(encoding.Index, Is.EqualTo(16));
            Assert.That(encoding.Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("gzip, deflate")));
        });
    }

    [Test]
    public void LookupIsByteExactAndCaseSensitive()
    {
        HpackTableEntry upperName = HpackStaticTableLookup.FindName(":METHOD"u8);
        HpackTableEntry lowerValue = HpackStaticTableLookup.FindExact(
            ":method"u8, "get"u8);
        HpackTableEntry binaryValue = HpackStaticTableLookup.FindExact(
            "age"u8, new byte[] { 0x80 });
        HpackTableEntry emptyName = HpackStaticTableLookup.FindName(
            ReadOnlySpan<byte>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(upperName.HasIndex, Is.False);
            Assert.That(lowerValue.HasIndex, Is.False);
            Assert.That(binaryValue.HasIndex, Is.False);
            Assert.That(emptyName.HasIndex, Is.False);
        });
    }

    [Test]
    public void LookupReturnsOwnedSnapshots()
    {
        HpackTableEntry first = HpackStaticTableLookup.LookupByIndex(16);
        first.Name[0] = (byte)'X';
        first.Value[0] = (byte)'X';

        HpackTableEntry second = HpackStaticTableLookup.LookupByIndex(16);
        HpackTableEntry exact = HpackStaticTableLookup.FindExact(
            "accept-encoding"u8, "gzip, deflate"u8);

        Assert.Multiple(() =>
        {
            Assert.That(second.Name,
                Is.EqualTo(Encoding.ASCII.GetBytes("accept-encoding")));
            Assert.That(second.Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("gzip, deflate")));
            Assert.That(exact.Index, Is.EqualTo(16));
            Assert.That(second.Name, Is.Not.SameAs(first.Name));
            Assert.That(second.Value, Is.Not.SameAs(first.Value));
        });
    }
}
