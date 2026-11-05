using languagecodes;

namespace languagecodes.Tests;

public class AsciiCodeTests
{
    [TestCase('A')]
    [TestCase('Z')]
    [TestCase('a')]
    [TestCase('z')]
    public void AsciiLettersIncludeUpperAndLowerBoundaries(char value)
    {
        Assert.That(AsciiCode.IsAsciiLetter(value), Is.True);
    }

    [TestCase('@')]
    [TestCase('[')]
    [TestCase('`')]
    [TestCase('{')]
    [TestCase('0')]
    [TestCase('\u00e9')]
    public void NonAsciiLettersAndNonlettersAreRejected(char value)
    {
        Assert.That(AsciiCode.IsAsciiLetter(value), Is.False);
    }

    [Test]
    public void ToLowerAsciiChangesOnlyUppercaseAsciiLetters()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AsciiCode.ToLowerAscii('A'), Is.EqualTo('a'));
            Assert.That(AsciiCode.ToLowerAscii('Z'), Is.EqualTo('z'));
            Assert.That(AsciiCode.ToLowerAscii('m'), Is.EqualTo('m'));
            Assert.That(AsciiCode.ToLowerAscii('-'), Is.EqualTo('-'));
            Assert.That(AsciiCode.ToLowerAscii('\u00c9'), Is.EqualTo('\u00c9'));
        });
    }

    [Test]
    public void LowercaseAsciiWritesIntoCallerDestination()
    {
        char[] buffer = new char[5];

        bool converted = AsciiCode.LowercaseAscii("EN-Us".AsSpan(), buffer.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(converted, Is.True);
            Assert.That(new string(buffer), Is.EqualTo("en-us"));
        });
    }

    [Test]
    public void LowercaseAsciiRejectsShortDestinationWithoutWriting()
    {
        char[] buffer = new[] { 'x', 'x' };

        bool converted = AsciiCode.LowercaseAscii("ENG".AsSpan(), buffer.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(converted, Is.False);
            Assert.That(buffer, Is.EqualTo(new[] { 'x', 'x' }));
        });
    }

    [TestCase("", 0)]
    [TestCase("e", 0)]
    [TestCase("en", 2)]
    [TestCase("eng", 3)]
    [TestCase("engl", 0)]
    public void CodeLengthClassificationIsExplicit(string value, int expected)
    {
        Assert.That((int)AsciiCode.ClassifyLength(value.AsSpan()), Is.EqualTo(expected));
    }

    [Test]
    public void SeparatorScanningFindsHyphensAndUnderscoresFromStartIndex()
    {
        string value = "en-Latn_US";

        Assert.Multiple(() =>
        {
            Assert.That(AsciiCode.FindTagSeparator(value.AsSpan(), 0), Is.EqualTo(2));
            Assert.That(AsciiCode.FindTagSeparator(value.AsSpan(), 3), Is.EqualTo(7));
            Assert.That(AsciiCode.FindTagSeparator(value.AsSpan(), 8), Is.EqualTo(-1));
            Assert.That(AsciiCode.FindTagSeparator(value.AsSpan(), -1), Is.EqualTo(-1));
            Assert.That(AsciiCode.FindTagSeparator(value.AsSpan(), value.Length), Is.EqualTo(-1));
        });
    }

    [TestCase("en")]
    [TestCase("EN")]
    [TestCase("eng")]
    [TestCase("eNg")]
    public void LexicalValidationAcceptsTwoOrThreeAsciiLetters(string value)
    {
        Assert.That(AsciiCode.IsLexicallyValid(value.AsSpan()), Is.True);
    }

    [TestCase("")]
    [TestCase("e")]
    [TestCase("engl")]
    [TestCase("e1")]
    [TestCase("en-")]
    [TestCase("\u00e9n")]
    public void LexicalValidationRejectsWrongShapeOrCharacterSet(string value)
    {
        Assert.That(AsciiCode.IsLexicallyValid(value.AsSpan()), Is.False);
    }
}
