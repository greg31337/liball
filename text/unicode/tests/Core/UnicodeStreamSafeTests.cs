using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeStreamSafeTests
{
    [Test]
    public void IsStreamSafe_AllowsAtMostThirtyConsecutiveNonStarters()
    {
        Assert.That(new UnicodeNormalization().IsStreamSafe("A" + new string('\u0301', 30)), Is.True);
        Assert.That(new UnicodeNormalization().IsStreamSafe("A" + new string('\u0301', 31)), Is.False);
    }

    [Test]
    public void IsStreamSafe_ResetsAtStartersAndCombiningGraphemeJoiner()
    {
        Assert.That(new UnicodeNormalization().IsStreamSafe(new string('\u0301', 30) + "A" + new string('\u0301', 30)), Is.True);
        Assert.That(new UnicodeNormalization().IsStreamSafe(new string('\u0301', 30) + "\u034F" + new string('\u0301', 30)), Is.True);
    }

    [Test]
    public void ToStreamSafe_InsertsCombiningGraphemeJoinerBeforeThirtyFirstNonStarter()
    {
        string text = "A" + new string('\u0301', 31);
        string expected = "A" + new string('\u0301', 30) + "\u034F\u0301";

        Assert.That(new UnicodeNormalization().ToStreamSafe(text), Is.EqualTo(expected));
        Assert.That(new UnicodeNormalization().IsStreamSafe(new UnicodeNormalization().ToStreamSafe(text)), Is.True);
    }

    [Test]
    public void ToStreamSafe_HandlesLeadingNonStarterRuns()
    {
        string text = new string('\u0301', 31);
        string expected = new string('\u0301', 30) + "\u034F\u0301";

        Assert.That(new UnicodeNormalization().ToStreamSafe(text), Is.EqualTo(expected));
    }

    [Test]
    public void ToStreamSafe_LeavesSafeTextUnchanged()
    {
        string text = "A" + new string('\u0301', 30) + "B";

        Assert.That(new UnicodeNormalization().ToStreamSafe(text), Is.EqualTo(text));
    }

    [Test]
    public void StreamSafe_UsesFullCompatibilityDecompositionCounts()
    {
        string expandingNonStarter = new string('\u0301', 29) + "\u0344";
        string starterWithTrailingNonStarter = "\u00C0" + new string('\u0301', 30);

        Assert.That(new UnicodeNormalization().IsStreamSafe(expandingNonStarter), Is.False);
        Assert.That(
            new UnicodeNormalization().ToStreamSafe(expandingNonStarter),
            Is.EqualTo(new string('\u0301', 29) + "\u034F\u0344"));
        Assert.That(new UnicodeNormalization().IsStreamSafe(starterWithTrailingNonStarter), Is.False);
        Assert.That(
            new UnicodeNormalization().ToStreamSafe(starterWithTrailingNonStarter),
            Is.EqualTo("\u00C0" + new string('\u0301', 29) + "\u034F\u0301"));
    }

    [Test]
    public void ToStreamSafe_IsStableAndPreservesNormalizationForms()
    {
        UnicodeNormalization normalization = new UnicodeNormalization();
        string nfc = "\u00C0" + new string('\u0301', 30);
        string streamSafe = normalization.ToStreamSafe(nfc);

        Assert.That(normalization.ToStreamSafe(streamSafe), Is.EqualTo(streamSafe));
        Assert.That(normalization.IsNormalized(streamSafe, UnicodeNormalizationForm.Nfc), Is.True);
        Assert.That(normalization.IsStreamSafe(streamSafe), Is.True);
    }

    [Test]
    public void StreamSafeApis_RejectNullText()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeNormalization().IsStreamSafe(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeNormalization().ToStreamSafe(null!));
    }
}
