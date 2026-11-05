using languagecodes;

namespace languagecodes.Tests;

public class LanguageExceptionTests
{
    [Test]
    public void CodeNotFoundExceptionPreservesMessageAndCanBeThrown()
    {
        LanguageCodeNotFoundException exception = new LanguageCodeNotFoundException(
            "No language code matched 'zzz'.");

        Assert.That(exception.Message, Is.EqualTo("No language code matched 'zzz'."));
        Assert.Throws<LanguageCodeNotFoundException>(() => throw exception);
    }

    [Test]
    public void AmbiguousNameExceptionPreservesMessageAndCanBeThrown()
    {
        AmbiguousLanguageNameException exception = new AmbiguousLanguageNameException(
            "The name matches more than one language.");

        Assert.That(exception.Message, Is.EqualTo("The name matches more than one language."));
        Assert.Throws<AmbiguousLanguageNameException>(() => throw exception);
    }

    [Test]
    public void InvalidCodeExceptionPreservesMessageAndCanBeThrown()
    {
        InvalidLanguageCodeException exception = new InvalidLanguageCodeException(
            "The language code contains a digit.");

        Assert.That(exception.Message, Is.EqualTo("The language code contains a digit."));
        Assert.Throws<InvalidLanguageCodeException>(() => throw exception);
    }

    [Test]
    public void InvalidTagExceptionPreservesMessageAndCanBeThrown()
    {
        InvalidLanguageTagException exception = new InvalidLanguageTagException(
            "The language tag contains duplicate variants.");

        Assert.That(exception.Message, Is.EqualTo("The language tag contains duplicate variants."));
        Assert.Throws<InvalidLanguageTagException>(() => throw exception);
    }
}
