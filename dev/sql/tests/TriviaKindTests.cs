using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class TriviaKindTests
{
    [Test]
    public void TriviaKind_ValuesAreStable()
    {
        Assert.That((int)SqlTriviaKind.None, Is.EqualTo(0));
        Assert.That((int)SqlTriviaKind.Whitespace, Is.EqualTo(1));
        Assert.That((int)SqlTriviaKind.LineComment, Is.EqualTo(2));
        Assert.That((int)SqlTriviaKind.BlockComment, Is.EqualTo(3));
        Assert.That((int)SqlTriviaKind.SkippedText, Is.EqualTo(4));
    }

    [Test]
    public void TriviaKind_DistinguishesWhitespaceCommentsAndSkippedText()
    {
        Assert.That(SqlTriviaKind.Whitespace, Is.Not.EqualTo(SqlTriviaKind.LineComment));
        Assert.That(SqlTriviaKind.LineComment, Is.Not.EqualTo(SqlTriviaKind.BlockComment));
        Assert.That(SqlTriviaKind.BlockComment, Is.Not.EqualTo(SqlTriviaKind.SkippedText));
        Assert.That(SqlTriviaKind.SkippedText, Is.Not.EqualTo(SqlTriviaKind.None));
    }
}
