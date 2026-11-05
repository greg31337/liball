
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTokenizerDataStateTests
{
    [Test]
    public void DataStateDecodesReferencesWithoutCommittingAmbiguousPrefixes()
    {
        HtmlTokenizerContext context = CreateContext(HtmlTokenizerState.Data, null);
        context.Feed("A&amp".AsSpan(), false);

        AssertCharacter(context.Read(), "A");
        AssertStatus(context.Read(), HtmlTokenReadStatus.NeedMoreInput);
        context.Feed(";B&#x80;".AsSpan(), true);

        HtmlTokenReadResult decoded = context.Read();
        AssertCharacter(decoded, "&B€");
        Assert.That(decoded.GetDiagnostics().Select(value => value.GetCode()),
            Is.EqualTo(new[] { HtmlDiagnosticCode.CharacterReferenceInvalid }));
        Assert.That(decoded.GetToken().GetSourceSpan().GetNormalizedCharacterLength(),
            Is.EqualTo(12));
        AssertKind(context.Read(), HtmlTokenKind.EndOfFile);
    }

    [Test]
    public void CharacterReferencesDecodeIdenticallyAtEveryChunkSplit()
    {
        string input = "x&CounterClockwiseContourIntegral;y&#x80;z";
        string expected = "x∳y€z";

        for (int split = 0; split <= input.Length; split++)
        {
            HtmlTokenizerContext context = CreateContext(HtmlTokenizerState.Data, null);
            context.Feed(input.AsSpan(0, split), false);
            string actual = ReadCharacterDataUntilPause(context);
            context.Feed(input.AsSpan(split), true);
            actual = actual + ReadCharacterDataUntilPause(context);

            Assert.That(actual, Is.EqualTo(expected), $"split {split}");
        }
    }

    [Test]
    public void UnknownReferencesRemainLiteralAndCarryTheirDiagnosticOnce()
    {
        HtmlTokenizerContext context = CreateContext(HtmlTokenizerState.Data, null);
        context.Feed("x&bogus;y".AsSpan(), true);

        HtmlTokenReadResult text = context.Read();

        AssertCharacter(text, "x&bogus;y");
        Assert.That(text.GetDiagnostics().Select(value => value.GetCode()),
            Is.EqualTo(new[] { HtmlDiagnosticCode.CharacterReferenceInvalid }));
        Assert.That(context.Read().GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void RcdataDecodesReferencesAndEmitsCaseFoldedAppropriateEndTag()
    {
        HtmlTokenizerContext context = CreateContext(HtmlTokenizerState.Rcdata, "TITLE");
        context.Feed("A&amp;</TiTlE>&copy;".AsSpan(), true);

        AssertCharacter(context.Read(), "A&");
        HtmlTokenReadResult end = context.Read();
        AssertKind(end, HtmlTokenKind.EndTag);
        Assert.That(end.GetToken().GetName(), Is.EqualTo("title"));
        Assert.That(end.GetToken().GetSourceSpan().GetNormalizedCharacterLength(), Is.EqualTo(8));
        Assert.That(context.GetState(), Is.EqualTo(HtmlTokenizerState.Data));
        AssertCharacter(context.Read(), "©");
        AssertKind(context.Read(), HtmlTokenKind.EndOfFile);
    }

    [Test]
    public void AppropriateEndTagCanContainTrailingWhitespaceOrSolidus()
    {
        HtmlTokenizerContext whitespace = CreateContext(HtmlTokenizerState.Rcdata, "title");
        whitespace.Feed("</title \t>".AsSpan(), true);
        HtmlTokenReadResult normal = whitespace.Read();

        AssertKind(normal, HtmlTokenKind.EndTag);
        Assert.That(normal.GetToken().IsSelfClosing(), Is.False);

        HtmlTokenizerContext solidus = CreateContext(HtmlTokenizerState.RawText, "style");
        solidus.Feed("</style/>".AsSpan(), true);
        HtmlTokenReadResult selfClosing = solidus.Read();

        AssertKind(selfClosing, HtmlTokenKind.EndTag);
        Assert.That(selfClosing.GetToken().IsSelfClosing(), Is.True);
    }

    [Test]
    public void RcdataEndTagRecognitionSuspendsAtEveryAmbiguousSplit()
    {
        string input = "</title>";
        for (int split = 0; split < input.Length; split++)
        {
            HtmlTokenizerContext context = CreateContext(HtmlTokenizerState.Rcdata, "title");
            context.Feed(input.AsSpan(0, split), false);
            AssertStatus(context.Read(), HtmlTokenReadStatus.NeedMoreInput);

            context.Feed(input.AsSpan(split), true);
            HtmlTokenReadResult result = context.Read();
            AssertKind(result, HtmlTokenKind.EndTag);
            Assert.That(result.GetToken().GetName(), Is.EqualTo("title"), $"split {split}");
        }
    }

    [Test]
    public void RawTextPreservesReferencesAndNonMatchingEndTagsLiterally()
    {
        HtmlTokenizerContext context = CreateContext(HtmlTokenizerState.RawText, "style");
        context.Feed("x&amp;</script>y</style>".AsSpan(), true);

        AssertCharacter(context.Read(), "x&amp;</script>y");
        HtmlTokenReadResult end = context.Read();
        AssertKind(end, HtmlTokenKind.EndTag);
        Assert.That(end.GetToken().GetName(), Is.EqualTo("style"));
    }

    [Test]
    public void PlainTextTreatsReferencesAndMarkupAsCharacterData()
    {
        HtmlTokenizerContext context = CreateContext(HtmlTokenizerState.PlainText, null);
        context.Feed("&amp;</title><b>".AsSpan(), true);

        AssertCharacter(context.Read(), "&amp;</title><b>");
        AssertKind(context.Read(), HtmlTokenKind.EndOfFile);
    }

    [Test]
    public void FinalIncompleteOrNonDelimitedEndTagCandidateRemainsLiteral()
    {
        HtmlTokenizerContext incomplete = CreateContext(HtmlTokenizerState.Rcdata, "title");
        incomplete.Feed("</title".AsSpan(), true);
        AssertCharacter(incomplete.Read(), "</title");

        HtmlTokenizerContext longer = CreateContext(HtmlTokenizerState.RawText, "title");
        longer.Feed("</titlex>".AsSpan(), true);
        AssertCharacter(longer.Read(), "</titlex>");
    }

    [Test]
    public void LastStartTagAndStateContractsValidateAndTrackEmission()
    {
        HtmlTokenizerContext context = CreateContext(HtmlTokenizerState.Data, null);
        HtmlToken start = HtmlToken.CreateStartTag("textarea", Array.Empty<HtmlAttribute>(),
            false, HtmlSourceSpan.CreateUnknown());

        Assert.That(context.TryEmitToken(start), Is.True);
        context.SetState(HtmlTokenizerState.Rcdata);
        context.Feed("</TEXTAREA>".AsSpan(), true);
        AssertKind(context.Read(), HtmlTokenKind.StartTag);
        AssertKind(context.Read(), HtmlTokenKind.EndTag);

        Assert.Throws<ArgumentNullException>(() => context.SetLastStartTagName(null));
        Assert.Throws<ArgumentException>(() => context.SetLastStartTagName(""));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.SetState((HtmlTokenizerState)99));
        context.ClearLastStartTagName();
        Assert.That(context.IsAppropriateEndTagName("textarea".AsSpan()), Is.False);
    }

    static HtmlTokenizerContext CreateContext(HtmlTokenizerState state, string lastStartTagName)
    {
        HtmlTokenizerContext context = new HtmlTokenizerContext(
            HtmlTokenizerOptions.CreateDefault(), null, false);
        context.SetState(state);
        if (lastStartTagName != null)
        {
            context.SetLastStartTagName(lastStartTagName);
        }

        return context;
    }

    static void AssertCharacter(HtmlTokenReadResult result, string data)
    {
        AssertKind(result, HtmlTokenKind.Character);
        Assert.That(result.GetToken().GetData(), Is.EqualTo(data));
    }

    static void AssertKind(HtmlTokenReadResult result, HtmlTokenKind kind)
    {
        Assert.That(result.GetStatus(), Is.EqualTo(HtmlTokenReadStatus.TokenAvailable));
        Assert.That(result.GetToken().GetKind(), Is.EqualTo(kind));
    }

    static void AssertStatus(HtmlTokenReadResult result, HtmlTokenReadStatus status)
    {
        Assert.That(result.GetStatus(), Is.EqualTo(status));
        Assert.That(result.HasToken(), Is.False);
    }

    static string ReadCharacterDataUntilPause(HtmlTokenizerContext context)
    {
        string result = "";
        bool reading = true;
        while (reading)
        {
            HtmlTokenReadResult next = context.Read();
            if (next.GetStatus() != HtmlTokenReadStatus.TokenAvailable)
            {
                reading = false;
            }
            else if (next.GetToken().GetKind() == HtmlTokenKind.Character)
            {
                result = result + next.GetToken().GetData();
            }
            else if (next.GetToken().GetKind() == HtmlTokenKind.EndOfFile)
            {
                reading = false;
            }
        }

        return result;
    }
}
