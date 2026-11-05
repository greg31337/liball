
namespace UniversalHtml.Tests;

[TestFixture]
public class SpanReaderTests
{
    [Test]
    public void ReaderOwnsInputAndReportsEndWithoutSentinelAmbiguity()
    {
        char[] source = ['a', '\0', 'b'];
        SpanReader reader = SpanReader.Create(source.AsSpan());
        source[0] = 'x';

        Assert.That(reader.Peek(), Is.EqualTo('a'));
        Assert.That(reader.Read(), Is.EqualTo('a'));
        Assert.That(reader.Read(), Is.EqualTo(0));
        Assert.That(reader.Read(), Is.EqualTo('b'));
        Assert.That(reader.Read(), Is.EqualTo(-1));
        Assert.That(reader.Peek(), Is.EqualTo(-1));
        Assert.That(reader.IsEnd(), Is.True);
    }

    [Test]
    public void ReaderSupportsLookaheadAdvanceAndReconsume()
    {
        SpanReader reader = SpanReader.Create("abcd".AsSpan());

        Assert.That(reader.PeekAhead(2), Is.EqualTo('c'));
        Assert.That(reader.PeekAhead(4), Is.EqualTo(-1));
        reader.Advance(2);
        Assert.That(reader.GetPosition(), Is.EqualTo(2));
        Assert.That(reader.GetRemainingLength(), Is.EqualTo(2));
        reader.Reconsume();
        Assert.That(reader.Read(), Is.EqualTo('b'));
        reader.SetPosition(4);
        Assert.That(reader.IsEnd(), Is.True);
    }

    [Test]
    public void ReaderConsumesExactOrAsciiFoldedSpansAtomically()
    {
        SpanReader reader = SpanReader.Create("DoCtYpE html".AsSpan());

        Assert.That(reader.StartsWith("doctype".AsSpan(), false), Is.False);
        Assert.That(reader.StartsWith("doctype".AsSpan(), true), Is.True);
        Assert.That(reader.ConsumeIfSequence("doctype".AsSpan(), true), Is.True);
        Assert.That(reader.ConsumeIf(' '), Is.True);
        int position = reader.GetPosition();
        Assert.That(reader.ConsumeIfSequence("BODY".AsSpan(), true), Is.False);
        Assert.That(reader.GetPosition(), Is.EqualTo(position));
        Assert.That(reader.GetString(position, 4), Is.EqualTo("html"));
    }

    [Test]
    public void RemainingCopyAdvancesOnlyAfterSuccessfulAppend()
    {
        SpanReader reader = SpanReader.Create("value".AsSpan());
        reader.Advance(1);
        CharacterBuffer tooSmall = CharacterBuffer.Create(0, 3);
        CharacterBuffer destination = CharacterBuffer.Create(0, 8);

        Assert.That(reader.TryConsumeRemainingTo(tooSmall), Is.False);
        Assert.That(reader.GetPosition(), Is.EqualTo(1));
        Assert.That(reader.TryConsumeRemainingTo(destination), Is.True);
        Assert.That(destination.GetString(), Is.EqualTo("alue"));
        Assert.That(reader.IsEnd(), Is.True);
    }

    [Test]
    public void ResetReplacesOwnedInputAndValidatesMovement()
    {
        SpanReader reader = SpanReader.Create("old".AsSpan());
        reader.Advance(2);
        reader.Reset("new".AsSpan());

        Assert.That(reader.GetLength(), Is.EqualTo(3));
        Assert.That(reader.GetPosition(), Is.EqualTo(0));
        Assert.That(reader.GetString(0, 3), Is.EqualTo("new"));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.PeekAhead(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.Advance(4));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.SetPosition(4));
        Assert.Throws<InvalidOperationException>(reader.Reconsume);
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.GetString(2, 2));
    }
}
