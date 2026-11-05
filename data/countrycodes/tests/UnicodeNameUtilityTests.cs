using System.Globalization;
using System.Text;
using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class UnicodeNameUtilityTests
{
    [Test]
    public void DisplayNormalizationProducesNfcWithoutChangingDisplayContent()
    {
        string input = "  Co\u0302te-d’Ivoire  ";
        string? normalized = UnicodeNameUtility.NormalizeDisplayName(input);

        Assert.That(normalized, Is.EqualTo("  Côte-d’Ivoire  "));
        Assert.That(normalized, Is.Not.Null);
        Assert.That(normalized!.IsNormalized(NormalizationForm.FormC), Is.True);
    }

    [Test]
    public void DefaultSearchFoldsProduceStableWordKeys()
    {
        string? key = UnicodeNameUtility.CreateMatchKey(
            "  Côte-d’Ivoire\t",
            true,
            true,
            true,
            true);

        Assert.That(key, Is.EqualTo("cote d ivoire"));
        Assert.That(key, Is.Not.Null);
        Assert.That(key!.IsNormalized(NormalizationForm.FormC), Is.True);
    }

    [Test]
    public void WhitespaceFoldingCanBeEnabledOrDisabled()
    {
        string input = "  New\u00A0\tZealand  ";

        Assert.That(
            UnicodeNameUtility.CreateMatchKey(input, true, false, false, false),
            Is.EqualTo("new zealand"));
        Assert.That(
            UnicodeNameUtility.CreateMatchKey(input, false, false, false, false),
            Is.EqualTo("  new\u00A0\tzealand  "));
    }

    [Test]
    public void PunctuationFoldingCanBeEnabledOrDisabled()
    {
        string input = "Guinea—Bissau, (West)";

        Assert.That(
            UnicodeNameUtility.CreateMatchKey(input, true, true, false, false),
            Is.EqualTo("guinea bissau west"));
        Assert.That(
            UnicodeNameUtility.CreateMatchKey(input, true, false, false, false),
            Is.EqualTo("guinea—bissau, (west)"));
    }

    [Test]
    public void CompatibilityFoldingCanBeEnabledOrDisabled()
    {
        string input = "ＦＩＪＩ oﬃcial ①";

        Assert.That(
            UnicodeNameUtility.CreateMatchKey(input, true, false, true, false),
            Is.EqualTo("fiji official 1"));
        Assert.That(
            UnicodeNameUtility.CreateMatchKey(input, true, false, false, false),
            Is.EqualTo("ｆｉｊｉ oﬃcial ①"));
    }

    [Test]
    public void DiacriticFoldingCanBeEnabledOrDisabled()
    {
        string input = "São Tomé";

        Assert.That(
            UnicodeNameUtility.CreateMatchKey(input, true, false, false, true),
            Is.EqualTo("sao tome"));
        Assert.That(
            UnicodeNameUtility.CreateMatchKey(input, true, false, false, false),
            Is.EqualTo("são tomé"));
    }

    [Test]
    public void SupplementaryCharactersArePreservedAndMalformedUtf16IsRejected()
    {
        Assert.That(
            UnicodeNameUtility.CreateMatchKey("A😀B", true, false, false, false),
            Is.EqualTo("a😀b"));
        Assert.That(UnicodeNameUtility.NormalizeDisplayName("\uD800"), Is.Null);
        Assert.That(
            UnicodeNameUtility.CreateMatchKey("x\uDC00", true, true, true, true),
            Is.Null);
    }

    [Test]
    public void NullAndEmptyInputsHaveExplicitResults()
    {
        Assert.That(UnicodeNameUtility.NormalizeDisplayName(null), Is.Null);
        Assert.That(UnicodeNameUtility.CreateMatchKey(null, true, true, true, true), Is.Null);
        Assert.That(UnicodeNameUtility.NormalizeDisplayName(string.Empty), Is.Empty);
        Assert.That(
            UnicodeNameUtility.CreateMatchKey(string.Empty, true, true, true, true),
            Is.Empty);
    }

    [Test]
    public void MatchKeysDoNotDependOnAmbientCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");

            Assert.That(
                UnicodeNameUtility.CreateMatchKey("INDIA ÉIRE", true, true, true, true),
                Is.EqualTo("india eire"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
