
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTokenizerOptionsTests
{
    [Test]
    public void PresetsAreValidAndUntrustedUsesTighterLimits()
    {
        HtmlTokenizerOptions defaults = HtmlTokenizerOptions.CreateDefault();
        HtmlTokenizerOptions untrusted = HtmlTokenizerOptions.CreateUntrusted();

        Assert.DoesNotThrow(defaults.Validate);
        Assert.DoesNotThrow(untrusted.Validate);
        Assert.That(defaults.GetTrackSourceLocations(), Is.True);
        Assert.That(defaults.GetCoalesceCharacterTokens(), Is.True);
        Assert.That(defaults.GetCancellationToken(), Is.EqualTo(CancellationToken.None));
        Assert.That(untrusted.GetLimits().GetMaximumTokens(),
            Is.LessThan(defaults.GetLimits().GetMaximumTokens()));
    }

    [Test]
    public void OptionsCopyLimitsAndMutableSettingsIndependently()
    {
        HtmlLimits suppliedLimits = HtmlLimits.CreateDefault();
        suppliedLimits.SetMaximumTokens(12);
        CancellationTokenSource source = new CancellationTokenSource();
        HtmlTokenizerOptions original = new HtmlTokenizerOptions();
        original.SetLimits(suppliedLimits);
        original.SetCancellationToken(source.Token);
        original.SetTrackSourceLocations(false);
        original.SetCoalesceCharacterTokens(false);

        suppliedLimits.SetMaximumTokens(20);
        HtmlLimits returnedLimits = original.GetLimits();
        returnedLimits.SetMaximumTokens(30);
        HtmlTokenizerOptions copy = original.Copy();
        copy.SetTrackSourceLocations(true);
        HtmlLimits copyLimits = copy.GetLimits();
        copyLimits.SetMaximumTokens(40);
        copy.SetLimits(copyLimits);

        Assert.Multiple(() =>
        {
            Assert.That(original.GetLimits().GetMaximumTokens(), Is.EqualTo(12));
            Assert.That(original.GetTrackSourceLocations(), Is.False);
            Assert.That(original.GetCoalesceCharacterTokens(), Is.False);
            Assert.That(original.GetCancellationToken(), Is.EqualTo(source.Token));
            Assert.That(copy.GetLimits().GetMaximumTokens(), Is.EqualTo(40));
            Assert.That(copy.GetTrackSourceLocations(), Is.True);
        });
    }

    [Test]
    public void SetLimitsRejectsMissingOrInvalidLimits()
    {
        HtmlTokenizerOptions options = new HtmlTokenizerOptions();
        HtmlLimits invalid = HtmlLimits.CreateDefault();

        Assert.Throws<ArgumentNullException>(() => options.SetLimits(null));
        Assert.Throws<ArgumentOutOfRangeException>(() => invalid.SetMaximumTokens(0));
    }
}
