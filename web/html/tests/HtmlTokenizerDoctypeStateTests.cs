
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTokenizerDoctypeStateTests
{
    [Test]
    public void DoctypeNameIsAsciiFoldedAndMissingIdentifiersRemainMissing()
    {
        string input = "<!dOcTyPe HTML>";

        HtmlTokenReadResult result = Tokenize(input).Read();

        AssertDoctype(result, true, "html", false, "", false, "", false);
        Assert.That(result.GetDiagnostics(), Is.Empty);
        Assert.That(result.GetToken().GetSourceSpan().GetNormalizedCharacterLength(),
            Is.EqualTo(input.Length));
    }

    [Test]
    public void PublicAndSystemIdentifiersPreserveTheirContents()
    {
        HtmlTokenReadResult result = Tokenize(
            "<!DOCTYPE html PUBLIC '-//Example//DTD HTML Test//EN' \"urn:Example:DTD\">")
            .Read();

        AssertDoctype(result, true, "html", true,
            "-//Example//DTD HTML Test//EN", true, "urn:Example:DTD", false);
        Assert.That(result.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SystemIdentifierCanUseSingleQuotes()
    {
        HtmlTokenReadResult result = Tokenize(
            "<!DOCTYPE html SYSTEM 'about:legacy-compat'>").Read();

        AssertDoctype(result, true, "html", false, "", true,
            "about:legacy-compat", false);
        Assert.That(result.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void EmptyIdentifiersRemainDistinctFromMissingIdentifiers()
    {
        HtmlTokenReadResult result = Tokenize(
            "<!DOCTYPE html PUBLIC \"\" ''>").Read();

        AssertDoctype(result, true, "html", true, "", true, "", false);
        Assert.That(result.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompleteDoctypeIsStableAtEveryChunkSplit()
    {
        string input = "<!DoCtYpE html PuBlIc \"public-id\" 'system-id'>";
        for (int split = 0; split <= input.Length; split++)
        {
            HtmlTokenizer tokenizer = HtmlTokenizer.Create(
                HtmlTokenizerOptions.CreateDefault());
            tokenizer.Feed(input.AsSpan(0, split), false);
            HtmlTokenReadResult first = tokenizer.Read();
            Assert.That(first.GetStatus(), Is.AnyOf(
                HtmlTokenReadStatus.NeedMoreInput,
                HtmlTokenReadStatus.TokenAvailable), $"split {split}");

            tokenizer.Feed(input.AsSpan(split), true);
            HtmlTokenReadResult result = first.GetStatus()
                == HtmlTokenReadStatus.TokenAvailable ? first : tokenizer.Read();

            AssertDoctype(result, true, "html", true, "public-id",
                true, "system-id", false);
            Assert.That(first.GetDiagnostics().Concat(result == first
                ? new HtmlDiagnostic[0] : result.GetDiagnostics()), Is.Empty,
                $"split {split}");
        }
    }

    [Test]
    public void SystemKeywordIsStableAtEveryChunkSplit()
    {
        string input = "<!DOCTYPE html SyStEm \"system-id\">";
        for (int split = 0; split <= input.Length; split++)
        {
            HtmlTokenizer tokenizer = HtmlTokenizer.Create(
                HtmlTokenizerOptions.CreateDefault());
            tokenizer.Feed(input.AsSpan(0, split), false);
            HtmlTokenReadResult first = tokenizer.Read();
            tokenizer.Feed(input.AsSpan(split), true);
            HtmlTokenReadResult result = first.GetStatus()
                == HtmlTokenReadStatus.TokenAvailable ? first : tokenizer.Read();

            AssertDoctype(result, true, "html", false, "", true,
                "system-id", false);
            Assert.That(first.GetDiagnostics().Concat(result == first
                ? new HtmlDiagnostic[0] : result.GetDiagnostics()), Is.Empty,
                $"split {split}");
        }
    }

    [TestCase("<!DOCTYPEhtml>")]
    [TestCase("<!DOCTYPE html PUBLIC\"public-id\">")]
    [TestCase("<!DOCTYPE html SYSTEM'system-id'>")]
    [TestCase("<!DOCTYPE html PUBLIC \"public-id\"'system-id'>")]
    public void MissingWhitespaceRecoversWithoutForcingQuirks(string input)
    {
        HtmlTokenReadResult result = Tokenize(input).Read();

        Assert.That(result.GetToken().IsForceQuirks(), Is.False);
        AssertMalformedDiagnostics(result, 1);
    }

    [TestCase("<!DOCTYPE>", false, "")]
    [TestCase("<!DOCTYPE html PUBLIC>", true, "html")]
    [TestCase("<!DOCTYPE html SYSTEM>", true, "html")]
    [TestCase("<!DOCTYPE html nonsense>", true, "html")]
    [TestCase("<!DOCTYPE html PUBLIC unquoted>", true, "html")]
    [TestCase("<!DOCTYPE html SYSTEM unquoted>", true, "html")]
    public void MalformedDoctypesEnterForceQuirksRecovery(
        string input, bool hasName, string name)
    {
        HtmlTokenReadResult result = Tokenize(input).Read();

        AssertDoctype(result, hasName, name, false, "", false, "", true);
        AssertMalformedDiagnostics(result, 1);
    }

    [TestCase("<!DOCTYPE html PUBLIC \"public>", true, "public")]
    [TestCase("<!DOCTYPE html SYSTEM \"system>", false, "system")]
    public void AbruptIdentifierClosesPreserveDataAndForceQuirks(
        string input, bool isPublic, string identifier)
    {
        HtmlTokenReadResult result = Tokenize(input).Read();
        HtmlToken token = result.GetToken();

        Assert.Multiple(() =>
        {
            Assert.That(token.IsForceQuirks(), Is.True);
            Assert.That(isPublic ? token.GetPublicIdentifier()
                : token.GetSystemIdentifier(), Is.EqualTo(identifier));
        });
        AssertMalformedDiagnostics(result, 1);
    }

    [Test]
    public void JunkAfterCompletedSystemIdentifierIsIgnoredWithoutForceQuirks()
    {
        HtmlTokenReadResult result = Tokenize(
            "<!DOCTYPE html SYSTEM \"system-id\" junk>").Read();

        AssertDoctype(result, true, "html", false, "", true,
            "system-id", false);
        AssertMalformedDiagnostics(result, 1);
    }

    [Test]
    public void MalformedDoctypeDiagnosticEndsAtTheOffendingCharacter()
    {
        string input = "<!DOCTYPE html SYSTEM \"id\"x>tail";

        HtmlTokenReadResult result = Tokenize(input).Read();

        Assert.That(result.GetDiagnostics()[0].GetSpan()
            .GetNormalizedCharacterLength(), Is.EqualTo(input.IndexOf('x') + 1));
    }

    [TestCase("<!DOCTYPE", false, "", false, "", false, "")]
    [TestCase("<!DOCTYPE html", true, "html", false, "", false, "")]
    [TestCase("<!DOCTYPE html PUBLIC \"public", true, "html", true,
        "public", false, "")]
    [TestCase("<!DOCTYPE html SYSTEM 'system", true, "html", false,
        "", true, "system")]
    public void EofInDoctypeEmitsPreservedFieldsAndForcesQuirks(
        string input, bool hasName, string name,
        bool hasPublicIdentifier, string publicIdentifier,
        bool hasSystemIdentifier, string systemIdentifier)
    {
        HtmlTokenReadResult result = Tokenize(input).Read();

        AssertDoctype(result, hasName, name, hasPublicIdentifier,
            publicIdentifier, hasSystemIdentifier, systemIdentifier, true);
        AssertMalformedDiagnostics(result, 1);
        Assert.That(Tokenize(input).Read().GetToken().GetSourceSpan()
            .GetNormalizedCharacterLength(), Is.EqualTo(input.Length));
    }

    [Test]
    public void EofInBogusDoctypeRetainsExistingForceQuirksDecision()
    {
        HtmlTokenReadResult forced = Tokenize("<!DOCTYPE html invalid").Read();
        HtmlTokenReadResult notForced = Tokenize(
            "<!DOCTYPE html SYSTEM \"id\" invalid").Read();

        Assert.Multiple(() =>
        {
            Assert.That(forced.GetToken().IsForceQuirks(), Is.True);
            Assert.That(notForced.GetToken().IsForceQuirks(), Is.False);
            AssertMalformedDiagnostics(forced, 1);
            AssertMalformedDiagnostics(notForced, 1);
        });
    }

    [Test]
    public void NullCharactersAreReplacedInDoctypeFields()
    {
        HtmlTokenReadResult result = Tokenize(
            "<!DOCTYPE h\0tml PUBLIC \"p\0\" \"s\0\">").Read();

        AssertDoctype(result, true, "h\uFFFDtml", true, "p\uFFFD",
            true, "s\uFFFD", false);
        Assert.That(result.GetDiagnostics().Select(value => value.GetCode()),
            Is.EqualTo(new[]
            {
                HtmlDiagnosticCode.InputNullCharacter,
                HtmlDiagnosticCode.InputNullCharacter,
                HtmlDiagnosticCode.InputNullCharacter
            }));
    }

    [Test]
    public void DoctypeNameHonorsMaximumNameCharacters()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumNameCharacters(3);

        AssertLimitExceeded("<!DOCTYPE html>", limits);
    }

    [Test]
    public void DoctypeFieldsShareMaximumTokenCharacters()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumTokenCharacters(6);

        AssertLimitExceeded("<!DOCTYPE h PUBLIC \"abc\" \"xyz\">", limits);
    }

    [Test]
    public void ExactDoctypeLimitsStillEmitTheCompletedToken()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumNameCharacters(4);
        limits.SetMaximumTokenCharacters(10);

        HtmlTokenReadResult result = Tokenize(
            "<!DOCTYPE html PUBLIC \"abc\" \"xyz\">", limits).Read();

        AssertDoctype(result, true, "html", true, "abc", true, "xyz", false);
    }

    static HtmlTokenizer Tokenize(string input)
    {
        return Tokenize(input, HtmlLimits.CreateDefault());
    }

    static HtmlTokenizer Tokenize(string input, HtmlLimits limits)
    {
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetLimits(limits);
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(options);
        tokenizer.Feed(input.AsSpan(), true);
        return tokenizer;
    }

    static void AssertDoctype(HtmlTokenReadResult result,
        bool hasName, string name,
        bool hasPublicIdentifier, string publicIdentifier,
        bool hasSystemIdentifier, string systemIdentifier,
        bool forceQuirks)
    {
        HtmlToken token = result.GetToken();
        Assert.Multiple(() =>
        {
            Assert.That(result.GetStatus(), Is.EqualTo(HtmlTokenReadStatus.TokenAvailable));
            Assert.That(token.GetKind(), Is.EqualTo(HtmlTokenKind.Doctype));
            Assert.That(token.HasName(), Is.EqualTo(hasName));
            Assert.That(token.GetName(), Is.EqualTo(name));
            Assert.That(token.HasPublicIdentifier(), Is.EqualTo(hasPublicIdentifier));
            Assert.That(token.GetPublicIdentifier(), Is.EqualTo(publicIdentifier));
            Assert.That(token.HasSystemIdentifier(), Is.EqualTo(hasSystemIdentifier));
            Assert.That(token.GetSystemIdentifier(), Is.EqualTo(systemIdentifier));
            Assert.That(token.IsForceQuirks(), Is.EqualTo(forceQuirks));
        });
    }

    static void AssertMalformedDiagnostics(HtmlTokenReadResult result, int count)
    {
        Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(count));
        Assert.That(result.GetDiagnostics().All(value =>
            value.GetCode() == HtmlDiagnosticCode.TokenizerMalformedDoctype), Is.True);
    }

    static void AssertLimitExceeded(string input, HtmlLimits limits)
    {
        HtmlTokenizer tokenizer = Tokenize(input, limits);

        Assert.That(tokenizer.Read().GetStatus(),
            Is.EqualTo(HtmlTokenReadStatus.LimitExceeded));
        Assert.That(tokenizer.Read().GetStatus(),
            Is.EqualTo(HtmlTokenReadStatus.LimitExceeded));
    }
}
