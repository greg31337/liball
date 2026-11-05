
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTokenizerScriptStateTests
{
    [Test]
    public void ScriptDataPreservesReferencesAndNonMatchingMarkupLiterally()
    {
        HtmlTokenizerContext context = CreateContext("script");
        context.Feed("a&amp;<b</not-script></ScRiPt>tail".AsSpan(), true);
        ScriptReadResult result = ReadAll(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Characters, Is.EqualTo("a&amp;<b</not-script>tail"));
            Assert.That(result.EndTags.Select(value => value.GetName()),
                Is.EqualTo(new[] { "script" }));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void AppropriateScriptEndTagIsStableAtEveryChunkSplit()
    {
        string input = "before</ScRiPt >after";
        for (int split = 0; split <= input.Length; split++)
        {
            HtmlTokenizerContext context = CreateContext("script");
            context.Feed(input.AsSpan(0, split), false);
            ScriptReadResult beforeFinal = ReadAll(context);
            Assert.That(beforeFinal.Status,
                Is.EqualTo(HtmlTokenReadStatus.NeedMoreInput), $"split {split}");

            context.Feed(input.AsSpan(split), true);
            ScriptReadResult afterFinal = ReadAll(context);

            Assert.Multiple(() =>
            {
                Assert.That(beforeFinal.Characters + afterFinal.Characters,
                    Is.EqualTo("beforeafter"), $"split {split}");
                Assert.That(beforeFinal.EndTags.Concat(afterFinal.EndTags)
                    .Select(value => value.GetName()),
                    Is.EqualTo(new[] { "script" }), $"split {split}");
                Assert.That(beforeFinal.Diagnostics.Concat(afterFinal.Diagnostics),
                    Is.Empty, $"split {split}");
            });
        }
    }

    [TestCase("a<!--escaped-->b</script>", "a<!--escaped-->b")]
    [TestCase("a<!-not-escaped</script>", "a<!-not-escaped")]
    [TestCase("<!--x</ScRiPt>tail", "<!--xtail")]
    public void EscapeEntryDashAndEscapedEndTagStatesPreserveText(
        string input, string expectedCharacters)
    {
        HtmlTokenizerContext context = CreateContext("script");
        context.Feed(input.AsSpan(), true);

        ScriptReadResult result = ReadAll(context);

        Assert.That(result.Characters, Is.EqualTo(expectedCharacters));
        Assert.That(result.EndTags.Select(value => value.GetName()),
            Is.EqualTo(new[] { "script" }));
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void DoubleEscapedScriptEndTagRemainsTextUntilEscapeEnd()
    {
        HtmlTokenizerContext context = CreateContext("script");
        context.Feed(
            "<!--<ScRiPt>inner</sCrIpT></script>tail".AsSpan(), true);

        ScriptReadResult result = ReadAll(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Characters,
                Is.EqualTo("<!--<ScRiPt>inner</sCrIpT>tail"));
            Assert.That(result.EndTags.Select(value => value.GetName()),
                Is.EqualTo(new[] { "script" }));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void DoubleEscapedStateFamilyIsStableAtEveryChunkSplit()
    {
        string input = "<!--<ScRiPt>inner</sCrIpT></script>";
        for (int split = 0; split <= input.Length; split++)
        {
            HtmlTokenizerContext context = CreateContext("script");
            context.Feed(input.AsSpan(0, split), false);
            ScriptReadResult beforeFinal = ReadAll(context);
            context.Feed(input.AsSpan(split), true);
            ScriptReadResult afterFinal = ReadAll(context);

            Assert.Multiple(() =>
            {
                Assert.That(beforeFinal.Characters + afterFinal.Characters,
                    Is.EqualTo("<!--<ScRiPt>inner</sCrIpT>"), $"split {split}");
                Assert.That(beforeFinal.EndTags.Concat(afterFinal.EndTags)
                    .Select(value => value.GetName()),
                    Is.EqualTo(new[] { "script" }), $"split {split}");
                Assert.That(beforeFinal.Diagnostics.Concat(afterFinal.Diagnostics),
                    Is.Empty, $"split {split}");
            });
        }
    }

    [Test]
    public void NonScriptDoubleEscapeSentinelsDoNotEnterOrLeaveDoubleEscaping()
    {
        HtmlTokenizerContext entry = CreateContext("script");
        entry.Feed("<!--<style></script>".AsSpan(), true);
        ScriptReadResult entryResult = ReadAll(entry);

        HtmlTokenizerContext exit = CreateContext("script");
        exit.Feed(
            "<!--<script>x</style></script>y</script>".AsSpan(), true);
        ScriptReadResult exitResult = ReadAll(exit);

        Assert.Multiple(() =>
        {
            Assert.That(entryResult.Characters, Is.EqualTo("<!--<style>"));
            Assert.That(entryResult.EndTags, Has.Count.EqualTo(1));
            Assert.That(exitResult.Characters,
                Is.EqualTo("<!--<script>x</style></script>y"));
            Assert.That(exitResult.EndTags, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void DoubleEscapedDashDashGreaterThanReturnsToScriptData()
    {
        HtmlTokenizerContext context = CreateContext("script");
        context.Feed("<!--<script>--></script>".AsSpan(), true);

        ScriptReadResult result = ReadAll(context);

        Assert.That(result.Characters, Is.EqualTo("<!--<script>-->"));
        Assert.That(result.EndTags, Has.Count.EqualTo(1));
    }

    [Test]
    public void AppropriateEndTagCanContinueThroughTagAttributeStates()
    {
        HtmlTokenizerContext context = CreateContext("script");
        context.Feed("</script foo=bar />".AsSpan(), true);

        ScriptReadResult result = ReadAll(context);
        HtmlToken end = result.EndTags[0];

        Assert.Multiple(() =>
        {
            Assert.That(end.GetName(), Is.EqualTo("script"));
            Assert.That(end.IsSelfClosing(), Is.True);
            Assert.That(end.GetAttributes().Select(value =>
                (value.GetName(), value.GetValue())),
                Is.EqualTo(new[] { ("foo", "bar") }));
            Assert.That(result.Diagnostics.Select(value => value.GetCode()),
                Is.EqualTo(new[]
                {
                    HtmlDiagnosticCode.TokenizerUnexpectedCharacter,
                    HtmlDiagnosticCode.TokenizerUnexpectedCharacter
                }));
        });
    }

    [Test]
    public void NullCharactersAreReplacedAcrossEscapedScriptStates()
    {
        HtmlTokenizerContext context = CreateContext("script");
        context.Feed("<!--a\0--></script>".AsSpan(), true);

        ScriptReadResult result = ReadAll(context);

        Assert.That(result.Characters, Is.EqualTo("<!--a\uFFFD-->"));
        Assert.That(result.Diagnostics.Select(value => value.GetCode()),
            Is.EqualTo(new[] { HtmlDiagnosticCode.InputNullCharacter }));
    }

    [TestCase("<!--open")]
    [TestCase("<!--<script>open")]
    public void EofInCommentLikeScriptTextIsDiagnosed(string input)
    {
        HtmlTokenizerContext context = CreateContext("script");
        context.Feed(input.AsSpan(), true);

        ScriptReadResult result = ReadAll(context);

        Assert.That(result.Characters, Is.EqualTo(input));
        Assert.That(result.Diagnostics.Select(value => value.GetCode()),
            Is.EqualTo(new[] { HtmlDiagnosticCode.TokenizerUnexpectedCharacter }));
    }

    [TestCase("plain")]
    [TestCase("<!-")]
    public void EofOutsideEscapedCommentLikeTextHasNoScriptDiagnostic(string input)
    {
        HtmlTokenizerContext context = CreateContext("script");
        context.Feed(input.AsSpan(), true);

        ScriptReadResult result = ReadAll(context);

        Assert.That(result.Characters, Is.EqualTo(input));
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void ScriptTemporarySentinelDoesNotConsumeCharacterTokenBudget()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumTokenCharacters(2);
        HtmlTokenizerContext context = CreateContext("s", limits);
        context.Feed("<!--<script>x</script></s>".AsSpan(), true);

        ScriptReadResult result = ReadAll(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HtmlTokenReadStatus.Complete));
            Assert.That(result.Characters, Is.EqualTo("<!--<script>x</script>"));
            Assert.That(result.CharacterTokens.All(value =>
                value.GetData().Length <= 2), Is.True);
            Assert.That(result.EndTags.Select(value => value.GetName()),
                Is.EqualTo(new[] { "s" }));
        });
    }

    [Test]
    public void DisabledCoalescingEmitsIndividualScriptCharacters()
    {
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetCoalesceCharacterTokens(false);
        HtmlTokenizerContext context = new HtmlTokenizerContext(options,
            null, false);
        context.SetLastStartTagName("script");
        context.SetState(HtmlTokenizerState.ScriptData);
        context.Feed("<!--x</other></script>".AsSpan(), true);

        ScriptReadResult result = ReadAll(context);

        Assert.That(result.CharacterTokens.Select(value => value.GetData()),
            Is.EqualTo("<!--x</other>".Select(value => new string(value, 1))));
    }

    [Test]
    public void ScriptEndTagCandidateHonorsMaximumNameCharacters()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumNameCharacters(3);
        HtmlTokenizerContext context = CreateContext("script", limits);
        context.Feed("</script>".AsSpan(), true);

        ScriptReadResult result = ReadAll(context);

        Assert.That(result.Status, Is.EqualTo(HtmlTokenReadStatus.LimitExceeded));
    }

    [Test]
    public void ScriptTokenSpansCoverLiteralTextAndEndTagDelimiters()
    {
        string input = "<!--x</other></script>";
        HtmlTokenizerContext context = CreateContext("script");
        context.Feed(input.AsSpan(), true);

        ScriptReadResult result = ReadAll(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.CharacterTokens.First().GetSourceSpan().GetStart()
                .GetNormalizedCharacterOffset(), Is.EqualTo(0));
            Assert.That(result.EndTags[0].GetSourceSpan().GetStart()
                .GetNormalizedCharacterOffset(), Is.EqualTo(input.IndexOf("</script>")));
            Assert.That(result.EndTags[0].GetSourceSpan()
                .GetNormalizedCharacterLength(), Is.EqualTo(9));
        });
    }

    static HtmlTokenizerContext CreateContext(string lastStartTagName)
    {
        return CreateContext(lastStartTagName, HtmlLimits.CreateDefault());
    }

    static HtmlTokenizerContext CreateContext(string lastStartTagName,
        HtmlLimits limits)
    {
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetLimits(limits);
        HtmlTokenizerContext context = new HtmlTokenizerContext(options,
            null, false);
        context.SetLastStartTagName(lastStartTagName);
        context.SetState(HtmlTokenizerState.ScriptData);
        return context;
    }

    static ScriptReadResult ReadAll(HtmlTokenizerContext context)
    {
        List<HtmlToken> characterTokens = new List<HtmlToken>();
        List<HtmlToken> endTags = new List<HtmlToken>();
        List<HtmlDiagnostic> diagnostics = new List<HtmlDiagnostic>();
        HtmlTokenReadStatus status = HtmlTokenReadStatus.NeedMoreInput;
        bool reading = true;
        while (reading)
        {
            HtmlTokenReadResult next = context.Read();
            diagnostics.AddRange(next.GetDiagnostics());
            status = next.GetStatus();
            if (status != HtmlTokenReadStatus.TokenAvailable)
            {
                reading = false;
            }
            else if (next.GetToken().GetKind() == HtmlTokenKind.Character)
            {
                characterTokens.Add(next.GetToken());
            }
            else if (next.GetToken().GetKind() == HtmlTokenKind.EndTag)
            {
                endTags.Add(next.GetToken());
            }
        }

        string characters = string.Concat(characterTokens.Select(value =>
            value.GetData()));
        return new ScriptReadResult(characters, characterTokens,
            endTags, diagnostics, status);
    }

    class ScriptReadResult
    {
        public string Characters { get; }
        public List<HtmlToken> CharacterTokens { get; }
        public List<HtmlToken> EndTags { get; }
        public List<HtmlDiagnostic> Diagnostics { get; }
        public HtmlTokenReadStatus Status { get; }

        public ScriptReadResult(string characters,
            List<HtmlToken> characterTokens, List<HtmlToken> endTags,
            List<HtmlDiagnostic> diagnostics, HtmlTokenReadStatus status)
        {
            this.Characters = characters;
            this.CharacterTokens = characterTokens;
            this.EndTags = endTags;
            this.Diagnostics = diagnostics;
            this.Status = status;
        }
    }
}
