using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class TextScanningTests
{
    [Test]
    public void VersionChar_ClassifiesAsciiCharactersUsedByVersionSyntax()
    {
        Assert.That(VersionChar.IsDigit('7'), Is.True);
        Assert.That(VersionChar.IsLetter('A'), Is.True);
        Assert.That(VersionChar.IsLetter('z'), Is.True);
        Assert.That(VersionChar.IsHyphen('-'), Is.True);
        Assert.That(VersionChar.IsDot('.'), Is.True);
        Assert.That(VersionChar.IsPlus('+'), Is.True);
        Assert.That(VersionChar.IsUnderscore('_'), Is.True);
        Assert.That(VersionChar.IsStar('*'), Is.True);
        Assert.That(VersionChar.IsX('x'), Is.True);
        Assert.That(VersionChar.IsX('X'), Is.True);
        Assert.That(VersionChar.IsV('v'), Is.True);
        Assert.That(VersionChar.IsV('V'), Is.True);
        Assert.That(VersionChar.IsOpenBracket('['), Is.True);
        Assert.That(VersionChar.IsCloseBracket(']'), Is.True);
        Assert.That(VersionChar.IsOpenParenthesis('('), Is.True);
        Assert.That(VersionChar.IsCloseParenthesis(')'), Is.True);
        Assert.That(VersionChar.IsComma(','), Is.True);
        Assert.That(VersionChar.IsPipe('|'), Is.True);
        Assert.That(VersionChar.IsComparisonOperator('<'), Is.True);
        Assert.That(VersionChar.IsComparisonOperator('>'), Is.True);
        Assert.That(VersionChar.IsComparisonOperator('='), Is.True);
        Assert.That(VersionChar.IsComparisonOperator('!'), Is.True);
        Assert.That(VersionChar.IsWhitespace('\t'), Is.True);
        Assert.That(VersionChar.IsAscii('\u0080'), Is.False);
    }

    [Test]
    public void VersionScanner_TracksCursorAndSourceLocation()
    {
        VersionScanner scanner = new VersionScanner("a\r\nb\nc");

        Assert.That(scanner.GetIndex(), Is.EqualTo(0));
        Assert.That(scanner.GetLineNumber(), Is.EqualTo(1));
        Assert.That(scanner.GetColumnNumber(), Is.EqualTo(1));

        Assert.That(scanner.Advance(), Is.EqualTo('a'));
        Assert.That(scanner.GetIndex(), Is.EqualTo(1));
        Assert.That(scanner.GetLineNumber(), Is.EqualTo(1));
        Assert.That(scanner.GetColumnNumber(), Is.EqualTo(2));

        Assert.That(scanner.Advance(), Is.EqualTo('\r'));
        Assert.That(scanner.GetLineNumber(), Is.EqualTo(2));
        Assert.That(scanner.GetColumnNumber(), Is.EqualTo(1));

        Assert.That(scanner.Advance(), Is.EqualTo('\n'));
        Assert.That(scanner.GetLineNumber(), Is.EqualTo(2));
        Assert.That(scanner.GetColumnNumber(), Is.EqualTo(1));

        Assert.That(scanner.Advance(), Is.EqualTo('b'));
        Assert.That(scanner.GetLineNumber(), Is.EqualTo(2));
        Assert.That(scanner.GetColumnNumber(), Is.EqualTo(2));

        scanner.AdvanceBy(2);
        Assert.That(scanner.GetLineNumber(), Is.EqualTo(3));
        Assert.That(scanner.GetColumnNumber(), Is.EqualTo(2));
        Assert.That(scanner.IsAtEnd(), Is.True);
        Assert.That(scanner.Peek(), Is.EqualTo(VersionChar.EndOfInput));
    }

    [Test]
    public void VersionScanner_CreatesTokenFromMarkedSpan()
    {
        VersionScanner scanner = new VersionScanner("abc123");
        int startIndex = scanner.GetIndex();
        int startLine = scanner.GetLineNumber();
        int startColumn = scanner.GetColumnNumber();

        scanner.AdvanceBy(3);
        VersionToken token = scanner.CreateToken(VersionTokenKind.TextIdentifier, startIndex, startLine, startColumn);

        Assert.That(token.GetKind(), Is.EqualTo(VersionTokenKind.TextIdentifier));
        Assert.That(token.GetText(), Is.EqualTo("abc"));
        Assert.That(token.GetStartIndex(), Is.EqualTo(0));
        Assert.That(token.GetEndIndex(), Is.EqualTo(3));
        Assert.That(token.GetLength(), Is.EqualTo(3));
        Assert.That(token.GetLineNumber(), Is.EqualTo(1));
        Assert.That(token.GetColumnNumber(), Is.EqualTo(1));
    }

    [Test]
    public void VersionScanner_ReadToken_ClassifiesVersionRangeLexemes()
    {
        VersionScanner scanner = new VersionScanner("1.2.x || >=3.0-alpha");

        AssertToken(scanner.ReadToken(), VersionTokenKind.NumericIdentifier, "1", 0, 1, 1);
        AssertToken(scanner.ReadToken(), VersionTokenKind.Separator, ".", 1, 1, 2);
        AssertToken(scanner.ReadToken(), VersionTokenKind.NumericIdentifier, "2", 2, 1, 3);
        AssertToken(scanner.ReadToken(), VersionTokenKind.Separator, ".", 3, 1, 4);
        AssertToken(scanner.ReadToken(), VersionTokenKind.Wildcard, "x", 4, 1, 5);
        AssertToken(scanner.ReadToken(), VersionTokenKind.Whitespace, " ", 5, 1, 6);
        AssertToken(scanner.ReadToken(), VersionTokenKind.Operator, "||", 6, 1, 7);
        AssertToken(scanner.ReadToken(), VersionTokenKind.Whitespace, " ", 8, 1, 9);
        AssertToken(scanner.ReadToken(), VersionTokenKind.Operator, ">=", 9, 1, 10);
        AssertToken(scanner.ReadToken(), VersionTokenKind.NumericIdentifier, "3", 11, 1, 12);
        AssertToken(scanner.ReadToken(), VersionTokenKind.Separator, ".", 12, 1, 13);
        AssertToken(scanner.ReadToken(), VersionTokenKind.NumericIdentifier, "0", 13, 1, 14);
        AssertToken(scanner.ReadToken(), VersionTokenKind.Separator, "-", 14, 1, 15);
        AssertToken(scanner.ReadToken(), VersionTokenKind.TextIdentifier, "alpha", 15, 1, 16);
        AssertToken(scanner.ReadToken(), VersionTokenKind.End, "", 20, 1, 21);
    }

    static void AssertToken(VersionToken token, VersionTokenKind kind, string text, int startIndex, int lineNumber, int columnNumber)
    {
        Assert.That(token.GetKind(), Is.EqualTo(kind));
        Assert.That(token.GetText(), Is.EqualTo(text));
        Assert.That(token.GetStartIndex(), Is.EqualTo(startIndex));
        Assert.That(token.GetLineNumber(), Is.EqualTo(lineNumber));
        Assert.That(token.GetColumnNumber(), Is.EqualTo(columnNumber));
    }
}
