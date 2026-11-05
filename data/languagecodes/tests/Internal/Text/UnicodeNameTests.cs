using languagecodes;

namespace languagecodes.Tests;

public class UnicodeNameTests
{
    [Test]
    public void NullEmptyAndSeparatorOnlyInputsNormalizeToEmpty()
    {
        Assert.Multiple(() =>
        {
            Assert.That(UnicodeName.Normalize(null), Is.EqualTo(string.Empty));
            Assert.That(UnicodeName.Normalize(string.Empty), Is.EqualTo(string.Empty));
            Assert.That(UnicodeName.Normalize(" \t\r\n---,() "), Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void CanonicallyEquivalentUnicodeProducesTheSameKey()
    {
        string composed = UnicodeName.Normalize("Caf\u00e9");
        string decomposed = UnicodeName.Normalize("Cafe\u0301");

        Assert.Multiple(() =>
        {
            Assert.That(composed, Is.EqualTo("CAF\u00c9"));
            Assert.That(decomposed, Is.EqualTo(composed));
        });
    }

    [Test]
    public void CompatibilityCharactersUseTheFixedNormalizationForm()
    {
        string normalized = UnicodeName.Normalize("\uff26\uff52\uff45\uff4e\uff43\uff48 \ufb03");

        Assert.That(normalized, Is.EqualTo("FRENCH FFI"));
    }

    [Test]
    public void CaseFoldingIsInvariantUnderTurkishCulture()
    {
        global::System.Globalization.CultureInfo originalCulture =
            global::System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            global::System.Globalization.CultureInfo.CurrentCulture =
                new global::System.Globalization.CultureInfo("tr-TR");

            Assert.That(UnicodeName.Normalize("i I"), Is.EqualTo("I I"));
        }
        finally
        {
            global::System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void WhitespaceAndPunctuationRunsFoldToSingleSpaces()
    {
        string normalized = UnicodeName.Normalize(
            "  Middle\t---English,\u00a0(1100\u20131500)  ");

        Assert.That(normalized, Is.EqualTo("MIDDLE ENGLISH 1100 1500"));
    }

    [Test]
    public void ApostropheVariantsNormalizeAsNameSeparators()
    {
        Assert.Multiple(() =>
        {
            Assert.That(UnicodeName.Normalize("N'Ko"), Is.EqualTo("N KO"));
            Assert.That(UnicodeName.Normalize("N\u2019Ko"), Is.EqualTo("N KO"));
        });
    }

    [Test]
    public void NonpunctuationSymbolsArePreserved()
    {
        Assert.That(UnicodeName.Normalize("A+B"), Is.EqualTo("A+B"));
    }
}
