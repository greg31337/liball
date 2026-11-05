using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class TextCursorTests
{
    [Test]
    public void Constructor_StoresTextAndStartsAtZero()
    {
        TextCursor cursor = new TextCursor("abc");

        Assert.That(cursor.GetText(), Is.EqualTo("abc"));
        Assert.That(cursor.GetLength(), Is.EqualTo(3));
        Assert.That(cursor.GetPosition(), Is.EqualTo(0));
        Assert.That(cursor.IsEnd(), Is.False);
    }

    [Test]
    public void Constructor_RequiresText()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new TextCursor(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("text"));
    }

    [Test]
    public void Create_ReturnsCursor()
    {
        TextCursor cursor = TextCursor.Create("abc");

        Assert.That(cursor.GetText(), Is.EqualTo("abc"));
    }

    [Test]
    public void PeekAndRead_ReturnCharactersAndAdvanceOnlyOnRead()
    {
        TextCursor cursor = TextCursor.Create("ab");

        Assert.That(cursor.Peek(), Is.EqualTo('a'));
        Assert.That(cursor.GetPosition(), Is.EqualTo(0));
        Assert.That(cursor.Read(), Is.EqualTo('a'));
        Assert.That(cursor.GetPosition(), Is.EqualTo(1));
        Assert.That(cursor.Read(), Is.EqualTo('b'));
        Assert.That(cursor.IsEnd(), Is.True);
    }

    [Test]
    public void PeekAndRead_RejectEnd()
    {
        TextCursor cursor = TextCursor.Create("");

        Assert.Throws<FormatException>(() => cursor.Peek());
        Assert.Throws<FormatException>(() => cursor.Read());
    }

    [Test]
    public void Match_ConsumesOnlyExpectedCharacter()
    {
        TextCursor cursor = TextCursor.Create("abc");

        Assert.That(cursor.Match('a'), Is.True);
        Assert.That(cursor.GetPosition(), Is.EqualTo(1));
        Assert.That(cursor.Match('c'), Is.False);
        Assert.That(cursor.GetPosition(), Is.EqualTo(1));
        Assert.That(cursor.Match('b'), Is.True);
        Assert.That(cursor.GetPosition(), Is.EqualTo(2));
    }

    [Test]
    public void Expect_ConsumesExpectedCharacterAndRejectsMismatch()
    {
        TextCursor cursor = TextCursor.Create("ab");

        cursor.Expect('a');
        Assert.That(cursor.GetPosition(), Is.EqualTo(1));
        Assert.Throws<FormatException>(() => cursor.Expect('c'));
        Assert.That(cursor.GetPosition(), Is.EqualTo(1));
    }

    [Test]
    public void CanReadAndAdvance_HandleBounds()
    {
        TextCursor cursor = TextCursor.Create("abcd");

        Assert.That(cursor.CanRead(4), Is.True);
        Assert.That(cursor.CanRead(5), Is.False);
        cursor.Advance(2);
        Assert.That(cursor.GetPosition(), Is.EqualTo(2));
        Assert.That(cursor.CanRead(2), Is.True);
        Assert.That(cursor.CanRead(3), Is.False);
        Assert.That(cursor.CanRead(int.MaxValue), Is.False);
        Assert.Throws<FormatException>(() => cursor.Advance(3));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => cursor.CanRead(-1))!.ParamName, Is.EqualTo("count"));
    }

    [Test]
    public void ReadFixedDigits_ReturnsValueAndAdvances()
    {
        TextCursor cursor = TextCursor.Create("2024-02");

        Assert.That(cursor.ReadFixedDigits(4), Is.EqualTo(2024));
        Assert.That(cursor.GetPosition(), Is.EqualTo(4));
        cursor.Expect('-');
        Assert.That(cursor.ReadFixedDigits(2), Is.EqualTo(2));
        Assert.That(cursor.IsEnd(), Is.True);
    }

    [Test]
    public void ReadFixedDigits_RejectsInvalidLengthNonDigitsAndShortText()
    {
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => TextCursor.Create("12").ReadFixedDigits(0))!.ParamName, Is.EqualTo("length"));
        Assert.Throws<FormatException>(() => TextCursor.Create("1x").ReadFixedDigits(2));
        Assert.Throws<FormatException>(() => TextCursor.Create("1").ReadFixedDigits(2));
    }

    [Test]
    public void ReadDigits_ReadsVariableLengthDigits()
    {
        TextCursor cursor = TextCursor.Create("123abc");

        Assert.That(cursor.ReadDigits(), Is.EqualTo(123));
        Assert.That(cursor.GetPosition(), Is.EqualTo(3));
        Assert.That(cursor.ReadRemaining(), Is.EqualTo("abc"));
    }

    [Test]
    public void ReadDigits_RequiresAtLeastOneDigit()
    {
        Assert.Throws<FormatException>(() => TextCursor.Create("abc").ReadDigits());
        Assert.Throws<FormatException>(() => TextCursor.Create("").ReadDigits());
    }

    [Test]
    public void ReadUntil_StopsBeforeDelimiterAndLeavesDelimiterUnread()
    {
        TextCursor cursor = TextCursor.Create("abc/def");

        Assert.That(cursor.ReadUntil('/'), Is.EqualTo("abc"));
        Assert.That(cursor.GetPosition(), Is.EqualTo(3));
        cursor.Expect('/');
        Assert.That(cursor.ReadRemaining(), Is.EqualTo("def"));
        Assert.That(cursor.IsEnd(), Is.True);
    }

    [Test]
    public void ReadRemaining_ReturnsRestAndMovesToEnd()
    {
        TextCursor cursor = TextCursor.Create("abcdef");
        cursor.Advance(2);

        Assert.That(cursor.ReadRemaining(), Is.EqualTo("cdef"));
        Assert.That(cursor.IsEnd(), Is.True);
    }

    [Test]
    public void RequireEnd_RejectsTrailingText()
    {
        TextCursor cursor = TextCursor.Create("a");

        Assert.Throws<FormatException>(() => cursor.RequireEnd());
        cursor.Read();
        cursor.RequireEnd();
    }

    [TestCase('0', true)]
    [TestCase('9', true)]
    [TestCase('/', false)]
    [TestCase('a', false)]
    public void IsDigit_IdentifiesAsciiDigits(char character, bool expected)
    {
        Assert.That(TextCursor.IsDigit(character), Is.EqualTo(expected));
    }
}
