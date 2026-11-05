using System.Text;
using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlReaderTests
{
    [Test]
    public void Constructor_StripsStringBomAndNormalizesLineBreaks()
    {
        global::yaml.YamlReader reader = global::yaml.YamlReader.FromText("\uFEFFa\r\nb\rc\n");

        Assert.That(reader.HadBom, Is.True);
        Assert.That(reader.Text, Is.EqualTo("a\nb\nc\n"));
        Assert.That(reader.Length, Is.EqualTo(6));
        Assert.That(reader.Index, Is.EqualTo(0));
        Assert.That(reader.Line, Is.EqualTo(1));
        Assert.That(reader.Column, Is.EqualTo(1));
        Assert.That(global::yaml.YamlReader.NormalizeText("x\r\ny\rz"), Is.EqualTo("x\ny\nz"));
        Assert.That(global::yaml.YamlReader.HasStringBom("\uFEFFx"), Is.True);
    }

    [Test]
    public void FromBytes_DetectsUtf8AndUtf16ByteOrderMarks()
    {
        byte[] utf8 = new byte[] { 239, 187, 191, 97, 13, 10, 98 };
        byte[] utf16Little = Combine(new byte[] { 255, 254 }, Encoding.Unicode.GetBytes("a\rb"));
        byte[] utf16Big = Combine(new byte[] { 254, 255 }, Encoding.BigEndianUnicode.GetBytes("a\r\nb"));

        global::yaml.YamlReader utf8Reader = global::yaml.YamlReader.FromBytes(utf8);
        global::yaml.YamlReader utf16LittleReader = global::yaml.YamlReader.FromBytes(utf16Little);
        global::yaml.YamlReader utf16BigReader = global::yaml.YamlReader.FromBytes(utf16Big);

        Assert.That(utf8Reader.HadBom, Is.True);
        Assert.That(utf8Reader.Text, Is.EqualTo("a\nb"));
        Assert.That(utf16LittleReader.HadBom, Is.True);
        Assert.That(utf16LittleReader.Text, Is.EqualTo("a\nb"));
        Assert.That(utf16BigReader.HadBom, Is.True);
        Assert.That(utf16BigReader.Text, Is.EqualTo("a\nb"));
    }

    [Test]
    public void ReadPeekConsumeAndAdvance_TrackNormalizedPositions()
    {
        global::yaml.YamlReader reader = global::yaml.YamlReader.FromText("a\r\nbc");

        Assert.That(reader.Current(), Is.EqualTo('a'));
        Assert.That(reader.Peek(1), Is.EqualTo('\n'));
        Assert.That(reader.Peek(99), Is.EqualTo('\0'));

        global::yaml.YamlSourceSpan start = reader.Mark();
        Assert.That(reader.Read(), Is.EqualTo('a'));
        Assert.That(reader.Index, Is.EqualTo(1));
        Assert.That(reader.Line, Is.EqualTo(1));
        Assert.That(reader.Column, Is.EqualTo(2));

        Assert.That(reader.Consume('\n'), Is.True);
        Assert.That(reader.Index, Is.EqualTo(2));
        Assert.That(reader.Line, Is.EqualTo(2));
        Assert.That(reader.Column, Is.EqualTo(1));

        Assert.That(reader.ReadText(2), Is.EqualTo("bc"));
        Assert.That(reader.End(), Is.True);
        Assert.That(reader.Current(), Is.EqualTo('\0'));
        Assert.That(reader.Read(), Is.EqualTo('\0'));
        Assert.That(reader.Consume('x'), Is.False);

        global::yaml.YamlSourceSpan span = reader.SpanFrom(start);
        Assert.That(span.StartIndex, Is.EqualTo(0));
        Assert.That(span.StartLine, Is.EqualTo(1));
        Assert.That(span.StartColumn, Is.EqualTo(1));
        Assert.That(span.EndIndex, Is.EqualTo(4));
        Assert.That(span.EndLine, Is.EqualTo(2));
        Assert.That(span.EndColumn, Is.EqualTo(3));
    }

    [Test]
    public void PeekAndAdvance_HandleIntegerBoundariesWithoutOverflow()
    {
        global::yaml.YamlReader reader = global::yaml.YamlReader.FromText("a\n");

        Assert.That(reader.Peek(int.MaxValue), Is.EqualTo('\0'));
        Assert.That(reader.Peek(int.MinValue), Is.EqualTo('\0'));

        reader.Column = int.MaxValue;
        reader.Advance();
        Assert.That(reader.Column, Is.EqualTo(int.MaxValue));

        reader.Line = int.MaxValue;
        reader.Advance();
        Assert.That(reader.Line, Is.EqualTo(int.MaxValue));
        Assert.That(reader.Column, Is.EqualTo(1));
    }

    [Test]
    public void ResetAndAdvanceBy_ReinitializeReaderState()
    {
        global::yaml.YamlReader reader = global::yaml.YamlReader.FromText("first");

        reader.AdvanceBy(99);
        Assert.That(reader.End(), Is.True);

        reader.Reset("x\ny");

        Assert.That(reader.Text, Is.EqualTo("x\ny"));
        Assert.That(reader.HadBom, Is.False);
        Assert.That(reader.Index, Is.EqualTo(0));
        Assert.That(reader.Line, Is.EqualTo(1));
        Assert.That(reader.Column, Is.EqualTo(1));

        reader.AdvanceBy(2);

        Assert.That(reader.Current(), Is.EqualTo('y'));
        Assert.That(reader.Line, Is.EqualTo(2));
        Assert.That(reader.Column, Is.EqualTo(1));
    }

    static byte[] Combine(byte[] left, byte[] right)
    {
        byte[] combined = new byte[left.Length + right.Length];
        int index = 0;
        while (index < left.Length)
        {
            combined[index] = left[index];
            index = index + 1;
        }

        int rightIndex = 0;
        while (rightIndex < right.Length)
        {
            combined[index] = right[rightIndex];
            index = index + 1;
            rightIndex = rightIndex + 1;
        }

        return combined;
    }
}
