using System.IO;
using System.Reflection;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvCharBufferTests
{
    [Test]
    public void Create_UsesRequestedBufferSize()
    {
        CsvCharBuffer buffer = CsvCharBuffer.Create(new StringReader("abc"), 2);

        Assert.That(buffer.BufferSize, Is.EqualTo(2));
        Assert.That(buffer.Buffer.Length, Is.EqualTo(2));
        Assert.That(buffer.Position, Is.EqualTo(0));
        Assert.That(buffer.Length, Is.EqualTo(0));
        Assert.That(buffer.CharacterOffset, Is.EqualTo(0));
        Assert.That(buffer.EndOfReader, Is.False);
    }

    [Test]
    public void Create_UsesDefaultBufferSizeForInvalidSize()
    {
        CsvCharBuffer buffer = CsvCharBuffer.Create(new StringReader("abc"), 0);

        Assert.That(buffer.BufferSize, Is.EqualTo(4096));
        Assert.That(buffer.Buffer.Length, Is.EqualTo(4096));
    }

    [Test]
    public void Peek_DoesNotAdvanceOffset()
    {
        CsvCharBuffer buffer = CsvCharBuffer.Create(new StringReader("ab"), 1);

        Assert.That(buffer.Peek(), Is.EqualTo((int)'a'));
        Assert.That(buffer.Peek(), Is.EqualTo((int)'a'));
        Assert.That(buffer.CharacterOffset, Is.EqualTo(0));
        Assert.That(buffer.Read(), Is.EqualTo((int)'a'));
        Assert.That(buffer.CharacterOffset, Is.EqualTo(1));
    }

    [Test]
    public void Read_AdvancesAcrossBufferBoundaries()
    {
        CsvCharBuffer buffer = CsvCharBuffer.Create(new StringReader("abcd"), 2);

        Assert.That(buffer.Read(), Is.EqualTo((int)'a'));
        Assert.That(buffer.Read(), Is.EqualTo((int)'b'));
        Assert.That(buffer.Read(), Is.EqualTo((int)'c'));
        Assert.That(buffer.Read(), Is.EqualTo((int)'d'));
        Assert.That(buffer.Read(), Is.EqualTo(-1));
        Assert.That(buffer.CharacterOffset, Is.EqualTo(4));
        Assert.That(buffer.IsEnd(), Is.True);
    }

    [Test]
    public void ReadBlock_CopiesRequestedCharactersAcrossBufferBoundaries()
    {
        CsvCharBuffer buffer = CsvCharBuffer.Create(new StringReader("abcdef"), 2);
        char[] destination = new char[4];

        int count = buffer.ReadBlock(destination, 0, 4);

        Assert.That(count, Is.EqualTo(4));
        Assert.That(destination, Is.EqualTo(new char[] { 'a', 'b', 'c', 'd' }));
        Assert.That(buffer.CharacterOffset, Is.EqualTo(4));
        Assert.That(buffer.Read(), Is.EqualTo((int)'e'));
    }

    [Test]
    public void ReadBlock_ReturnsPartialCountAtEnd()
    {
        CsvCharBuffer buffer = CsvCharBuffer.Create(new StringReader("ab"), 3);
        char[] destination = new char[4];

        int count = buffer.ReadBlock(destination, 1, 4);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(destination[0], Is.EqualTo('\0'));
        Assert.That(destination[1], Is.EqualTo('a'));
        Assert.That(destination[2], Is.EqualTo('b'));
        Assert.That(destination[3], Is.EqualTo('\0'));
        Assert.That(buffer.Read(), Is.EqualTo(-1));
    }

    [Test]
    public void Reset_ReusesBufferWithNewReaderAndClearsPosition()
    {
        CsvCharBuffer buffer = CsvCharBuffer.Create(new StringReader("ab"), 2);
        buffer.Read();

        buffer.Reset(new StringReader("xy"));

        Assert.That(buffer.BufferSize, Is.EqualTo(2));
        Assert.That(buffer.CharacterOffset, Is.EqualTo(0));
        Assert.That(buffer.Read(), Is.EqualTo((int)'x'));
        Assert.That(buffer.Read(), Is.EqualTo((int)'y'));
        Assert.That(buffer.Read(), Is.EqualTo(-1));
    }

    [Test]
    public void CsvCharBuffer_IsInternalAndUsesInternalFields()
    {
        FieldInfo[] fields = typeof(CsvCharBuffer).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo[] publicFields = typeof(CsvCharBuffer).GetFields(BindingFlags.Public | BindingFlags.Instance);
        PropertyInfo[] properties = typeof(CsvCharBuffer).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.That(typeof(CsvCharBuffer).IsNotPublic, Is.True);
        Assert.That(fields.Length, Is.GreaterThan(0));
        Assert.That(publicFields, Is.Empty);
        Assert.That(properties, Is.Empty);
    }
}
