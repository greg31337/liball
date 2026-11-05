using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeEnumTests
{
    [Test]
    public void NormalizationEnums_DefineExpectedFormsAndQuickCheckResults()
    {
        Assert.That(Enum.GetNames<UnicodeNormalizationForm>(), Is.EqualTo(new string[] { "Nfc", "Nfd", "Nfkc", "Nfkd" }));
        Assert.That(Enum.GetNames<UnicodeQuickCheckResult>(), Is.EqualTo(new string[] { "Yes", "No", "Maybe" }));
    }

    [Test]
    public void PropertyEnums_DefineRepresentativeCategoriesScriptsAndBlocks()
    {
        Assert.That(Enum.GetNames<UnicodeGeneralCategory>(), Does.Contain("UppercaseLetter"));
        Assert.That(Enum.GetNames<UnicodeGeneralCategory>(), Does.Contain("NonspacingMark"));
        Assert.That(Enum.GetNames<UnicodeGeneralCategory>(), Does.Contain("OtherNotAssigned"));
        Assert.That(Enum.GetNames<UnicodeScript>(), Does.Contain("Latin"));
        Assert.That(Enum.GetNames<UnicodeScript>(), Does.Contain("Han"));
        Assert.That(Enum.GetNames<UnicodeScript>(), Does.Contain("Inherited"));
        Assert.That(Enum.GetNames<UnicodeBlock>(), Does.Contain("BasicLatin"));
        Assert.That(Enum.GetNames<UnicodeBlock>(), Does.Contain("CjkUnifiedIdeographs"));
        Assert.That(Enum.GetNames<UnicodeBlock>(), Does.Contain("NoBlock"));
        Assert.That(Enum.GetNames<UnicodeBidiClass>(), Does.Contain("LeftToRight"));
        Assert.That(Enum.GetNames<UnicodeBidiClass>(), Does.Contain("EuropeanNumber"));
        Assert.That(Enum.GetNames<UnicodeEastAsianWidth>(), Does.Contain("Narrow"));
        Assert.That(Enum.GetNames<UnicodeEastAsianWidth>(), Does.Contain("Wide"));
        Assert.That(Enum.GetNames<UnicodeBinaryProperty>(), Does.Contain("WhiteSpace"));
        Assert.That(Enum.GetNames<UnicodeBinaryProperty>(), Does.Contain("ExtendedPictographic"));
    }

    [Test]
    public void BreakClassEnums_DefineRepresentativeBoundaryClasses()
    {
        Assert.That(Enum.GetNames<UnicodeGraphemeBreakClass>(), Does.Contain("ExtendedPictographic"));
        Assert.That(Enum.GetNames<UnicodeGraphemeBreakClass>(), Does.Contain("RegionalIndicator"));
        Assert.That(Enum.GetNames<UnicodeWordBreakClass>(), Does.Contain("ALetter"));
        Assert.That(Enum.GetNames<UnicodeWordBreakClass>(), Does.Contain("HebrewLetter"));
        Assert.That(Enum.GetNames<UnicodeSentenceBreakClass>(), Does.Contain("ATerm"));
        Assert.That(Enum.GetNames<UnicodeSentenceBreakClass>(), Does.Contain("STerm"));
        Assert.That(Enum.GetNames<UnicodeLineBreakClass>(), Does.Contain("Alphabetic"));
        Assert.That(Enum.GetNames<UnicodeLineBreakClass>(), Does.Contain("ZeroWidthJoiner"));
    }

    [Test]
    public void CasingIdentifierSecurityAndPdfEnums_DefineExpectedModesAndFlags()
    {
        Assert.That(Enum.GetNames<UnicodeCaseMappingMode>(), Is.EqualTo(new string[] { "Simple", "Full", "LocaleSensitive", "Fold" }));
        Assert.That(Enum.GetNames<UnicodeCompareKeyMode>(), Is.EqualTo(new string[] { "OrdinalFold", "NfcFold", "NfkcFold" }));
        Assert.That(Enum.GetNames<UnicodeIdentifierStatus>(), Does.Contain("InvalidStart"));
        Assert.That(Enum.GetNames<UnicodeIdentifierStatus>(), Does.Contain("NeedsNormalization"));
        Assert.That(Enum.GetNames<UnicodeIdentifierStatus>(), Does.Contain("InvalidScalar"));
        Assert.That(Convert.ToInt32(UnicodeSecurityFlag.MixedScript), Is.EqualTo(1));
        Assert.That(Convert.ToInt32(UnicodeSecurityFlag.Confusable), Is.EqualTo(2));
        Assert.That(Convert.ToInt32(UnicodeSecurityFlag.VariationSelector), Is.EqualTo(128));
        Assert.That(Enum.GetNames<UnicodeRestrictionLevel>(), Is.EqualTo(new string[] { "Ascii", "SingleScriptRestrictive", "HighlyRestrictive", "ModeratelyRestrictive", "MinimallyRestrictive", "Unrestricted" }));
        Assert.That(Enum.GetNames<UnicodePdfBidiDirection>(), Is.EqualTo(new string[] { "LeftToRight", "RightToLeft", "Neutral" }));
        Assert.That(Enum.GetNames<UnicodePdfCMapStatus>(), Does.Contain("DuplicateCharacterCode"));
        Assert.That(Enum.GetNames<UnicodePdfDiagnosticStatus>(), Does.Contain("MissingUnicodeMap"));
        Assert.That(Enum.GetNames<UnicodePdfTextEncoding>(), Is.EqualTo(new string[] { "PdfDocEncoding", "Utf16BeWithBom" }));
    }
}
