using xml;

namespace xml.Tests;

[TestFixture]
public class ValueBufferTests
{
    [Test]
    public void CharacterBufferAppendsAcrossGrowthAndCopiesContent()
    {
        ValueBuffer buffer = new ValueBuffer(1);
        buffer.AppendCharacter('<');
        buffer.AppendText("root");
        buffer.AppendCharacters(">value</root>".AsSpan());

        Span<char> destination = new char[buffer.CharacterCount()];
        buffer.CopyCharacters(destination);

        Assert.That(buffer.GetText(), Is.EqualTo("<root>value</root>"));
        Assert.That(buffer.WrittenCharacters().ToString(), Is.EqualTo("<root>value</root>"));
        Assert.That(destination.ToString(), Is.EqualTo("<root>value</root>"));
        Assert.That(buffer.CharacterCapacity(), Is.GreaterThanOrEqualTo(buffer.CharacterCount()));
    }

    [Test]
    public void CharacterBufferClearsWrittenContentForReuse()
    {
        ValueBuffer buffer = new ValueBuffer(0);
        buffer.AppendText("secret");
        int capacity = buffer.CharacterCapacity();

        buffer.ClearCharacters();

        Assert.That(buffer.CharacterCount(), Is.Zero);
        Assert.That(buffer.GetText(), Is.Empty);
        Assert.That(buffer.Characters[0], Is.EqualTo('\0'));
        buffer.AppendText("next");
        Assert.That(buffer.GetText(), Is.EqualTo("next"));
        Assert.That(buffer.CharacterCapacity(), Is.EqualTo(capacity));
    }

    [Test]
    public void CharacterBufferRejectsInvalidCapacityAndSmallDestination()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ValueBuffer(-1));
        ValueBuffer buffer = new ValueBuffer(0);
        buffer.AppendText("abc");
        Assert.Throws<ArgumentException>(() => buffer.CopyCharacters(new char[2]));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.EnsureCharacterCapacity(-1));
    }

    [Test]
    public void ByteBufferAppendsAcrossGrowthAndReturnsIndependentCopy()
    {
        ByteBuffer buffer = new ByteBuffer(1);
        buffer.AppendByte(0xEF);
        buffer.AppendBytes(new byte[] { 0xBB, 0xBF, 0x3C });

        Span<byte> destination = new byte[buffer.ByteCount()];
        buffer.CopyBytes(destination);
        byte[] copy = buffer.GetByteArray();
        copy[0] = 0;

        Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF, 0x3C }));
        Assert.That(buffer.WrittenBytes().ToArray(), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF, 0x3C }));
        Assert.That(buffer.WrittenBytes()[0], Is.EqualTo(0xEF));
        Assert.That(buffer.ByteCapacity(), Is.GreaterThanOrEqualTo(buffer.ByteCount()));
    }

    [Test]
    public void ByteBufferClearsWrittenContentForReuse()
    {
        ByteBuffer buffer = new ByteBuffer(0);
        buffer.AppendBytes(new byte[] { 1, 2, 3 });
        int capacity = buffer.ByteCapacity();

        buffer.ClearBytes();

        Assert.That(buffer.ByteCount(), Is.Zero);
        Assert.That(buffer.Bytes[0], Is.Zero);
        buffer.AppendByte(4);
        Assert.That(buffer.GetByteArray(), Is.EqualTo(new byte[] { 4 }));
        Assert.That(buffer.ByteCapacity(), Is.EqualTo(capacity));
    }

    [Test]
    public void ByteBufferRejectsInvalidCapacityAndSmallDestination()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ByteBuffer(-1));
        ByteBuffer buffer = new ByteBuffer(0);
        buffer.AppendBytes(new byte[] { 1, 2, 3 });
        Assert.Throws<ArgumentException>(() => buffer.CopyBytes(new byte[2]));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.EnsureByteCapacity(-1));
    }
}
