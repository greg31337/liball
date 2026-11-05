using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownInputNormalizerTests
{
    [Test]
    public void Normalize_ReturnsSameTextWhenNoNullCharactersExist()
    {
        string source = "alpha\nbeta";
        string normalized = MarkdownInputNormalizer.Normalize(source);

        Assert.That(normalized, Is.EqualTo(source));
        Assert.That(object.ReferenceEquals(source, normalized), Is.True);
    }

    [Test]
    public void Normalize_ReplacesEveryNullCharacterWithReplacementCharacter()
    {
        string normalized = MarkdownInputNormalizer.Normalize("a\0b\0");

        Assert.That(normalized.Length, Is.EqualTo(4));
        Assert.That(normalized[0], Is.EqualTo('a'));
        Assert.That(normalized[1], Is.EqualTo(MarkdownInputNormalizer.NullReplacementCharacter));
        Assert.That(normalized[2], Is.EqualTo('b'));
        Assert.That(normalized[3], Is.EqualTo(MarkdownInputNormalizer.NullReplacementCharacter));
    }

    [Test]
    public void ContainsNullCharacter_ReportsPresence()
    {
        Assert.That(MarkdownInputNormalizer.ContainsNullCharacter("abc"), Is.False);
        Assert.That(MarkdownInputNormalizer.ContainsNullCharacter("a\0c"), Is.True);
    }

    [Test]
    public void Normalizer_RejectsNullSource()
    {
        Assert.That(() => MarkdownInputNormalizer.Normalize(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownInputNormalizer.ReplaceNullCharacters(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownInputNormalizer.ContainsNullCharacter(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
