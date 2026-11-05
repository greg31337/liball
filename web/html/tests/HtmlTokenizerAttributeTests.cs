
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTokenizerAttributeTests
{
    [Test]
    public void FoldedDuplicateAttributesAreDiscardedAtEveryChunkSplit()
    {
        string input = "<p ID=first iD=second class=x Id=third>";
        for (int split = 0; split < input.Length; split++)
        {
            HtmlTokenizer tokenizer = HtmlTokenizer.Create(HtmlTokenizerOptions.CreateDefault());
            tokenizer.Feed(input.AsSpan(0, split), false);
            HtmlTokenReadResult suspension = tokenizer.Read();
            Assert.That(suspension.GetStatus(), Is.EqualTo(HtmlTokenReadStatus.NeedMoreInput));
            tokenizer.Feed(input.AsSpan(split), true);

            HtmlTokenReadResult result = tokenizer.Read();
            HtmlAttribute[] attributes = result.GetToken().GetAttributes();
            HtmlDiagnosticCode[] diagnosticCodes = suspension.GetDiagnostics()
                .Concat(result.GetDiagnostics()).Select(value => value.GetCode()).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(attributes.Select(value => value.GetName()),
                    Is.EqualTo(new[] { "id", "class" }), $"split {split}");
                Assert.That(attributes.Select(value => value.GetValue()),
                    Is.EqualTo(new[] { "first", "x" }), $"split {split}");
                Assert.That(diagnosticCodes,
                    Is.EqualTo(new[]
                    {
                        HtmlDiagnosticCode.TokenizerDuplicateAttribute,
                        HtmlDiagnosticCode.TokenizerDuplicateAttribute
                    }), $"split {split}");
            });
        }
    }

    [Test]
    public void DuplicateAttributeIsDiagnosedWhenAnIncompleteTagEndsAtEof()
    {
        HtmlTokenizer tokenizer = Tokenize("<p a=1 A", HtmlLimits.CreateDefault(), true);

        HtmlTokenReadResult result = tokenizer.Read();

        Assert.That(result.GetToken().GetKind(), Is.EqualTo(HtmlTokenKind.EndOfFile));
        Assert.That(result.GetDiagnostics().Select(value => value.GetCode()),
            Is.EqualTo(new[]
            {
                HtmlDiagnosticCode.TokenizerDuplicateAttribute,
                HtmlDiagnosticCode.TokenizerUnexpectedCharacter
            }));
    }

    [Test]
    public void AttributeSpansCoverNamesAndValuesInOriginalAndNormalizedInput()
    {
        string input = "<p\r\n ID = \"one\" disabled data-x=two>";
        HtmlToken token = Tokenize(input, HtmlLimits.CreateDefault(), true).Read().GetToken();
        HtmlAttribute[] attributes = token.GetAttributes();

        int idStart = input.IndexOf("ID", StringComparison.Ordinal);
        int idEnd = input.IndexOf('"', input.IndexOf('"') + 1) + 1;
        int disabledStart = input.IndexOf("disabled", StringComparison.Ordinal);
        int disabledEnd = disabledStart + "disabled".Length;
        int dataStart = input.IndexOf("data-x", StringComparison.Ordinal);
        int dataEnd = input.IndexOf('>');

        AssertSpan(attributes[0].GetSourceSpan(), idStart - 1, idEnd - 1, idStart, idEnd);
        AssertSpan(attributes[1].GetSourceSpan(), disabledStart - 1,
            disabledEnd - 1, disabledStart, disabledEnd);
        AssertSpan(attributes[2].GetSourceSpan(), dataStart - 1,
            dataEnd - 1, dataStart, dataEnd);
    }

    [Test]
    public void DisabledSourceTrackingReturnsUnknownAttributeSpans()
    {
        HtmlToken token = Tokenize("<p id=x>", HtmlLimits.CreateDefault(), false)
            .Read().GetToken();

        Assert.That(token.GetAttributes()[0].GetSourceSpan().IsKnown(), Is.False);
    }

    [Test]
    public void NullCharactersAreReplacedInTagAndAttributeData()
    {
        HtmlTokenizer tokenizer = Tokenize("<D\0V A\0B='x\0y'>",
            HtmlLimits.CreateDefault(), true);

        HtmlTokenReadResult result = tokenizer.Read();
        HtmlToken token = result.GetToken();

        Assert.Multiple(() =>
        {
            Assert.That(token.GetName(), Is.EqualTo("d\uFFFDv"));
            Assert.That(token.GetAttributes()[0].GetName(), Is.EqualTo("a\uFFFDb"));
            Assert.That(token.GetAttributes()[0].GetValue(), Is.EqualTo("x\uFFFDy"));
            Assert.That(result.GetDiagnostics().Select(value => value.GetCode()),
                Is.EqualTo(new[]
                {
                    HtmlDiagnosticCode.InputNullCharacter,
                    HtmlDiagnosticCode.InputNullCharacter,
                    HtmlDiagnosticCode.InputNullCharacter
                }));
        });
    }

    [Test]
    public void MaximumAttributesPerTagStopsBeforeAnExtraAttribute()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumAttributesPerTag(2);

        AssertLimitExceeded("<p a b c>", limits);
    }

    [TestCase("<abcd>")]
    [TestCase("<p abcd>")]
    public void MaximumNameCharactersAppliesToTagAndAttributeNames(string input)
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumNameCharacters(3);

        AssertLimitExceeded(input, limits);
    }

    [Test]
    public void MaximumAttributeValueCharactersStopsLongValues()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumAttributeValueCharacters(3);

        AssertLimitExceeded("<p a=abcd>", limits);
    }

    [Test]
    public void MaximumTokenCharactersIncludesAllTagNamesAndValues()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumTokenCharacters(3);

        AssertLimitExceeded("<p a=bc>", limits);
    }

    [Test]
    public void ExactTagLimitsStillEmitTheCompletedToken()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumAttributesPerTag(2);
        limits.SetMaximumNameCharacters(3);
        limits.SetMaximumAttributeValueCharacters(3);
        limits.SetMaximumTokenCharacters(9);

        HtmlTokenReadResult result = Tokenize("<abc xy=123 z>", limits, true).Read();

        Assert.That(result.GetStatus(), Is.EqualTo(HtmlTokenReadStatus.TokenAvailable));
        Assert.That(result.GetToken().GetAttributes().Select(value => value.GetName()),
            Is.EqualTo(new[] { "xy", "z" }));
    }

    static HtmlTokenizer Tokenize(string input, HtmlLimits limits, bool trackLocations)
    {
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetLimits(limits);
        options.SetTrackSourceLocations(trackLocations);
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(options);
        tokenizer.Feed(input.AsSpan(), true);
        return tokenizer;
    }

    static void AssertLimitExceeded(string input, HtmlLimits limits)
    {
        HtmlTokenizer tokenizer = Tokenize(input, limits, true);

        Assert.That(tokenizer.Read().GetStatus(), Is.EqualTo(HtmlTokenReadStatus.LimitExceeded));
        Assert.That(tokenizer.Read().GetStatus(), Is.EqualTo(HtmlTokenReadStatus.LimitExceeded));
    }

    static void AssertSpan(HtmlSourceSpan span, int normalizedStart, int normalizedEnd,
        int originalStart, int originalEnd)
    {
        Assert.Multiple(() =>
        {
            Assert.That(span.IsKnown(), Is.True);
            Assert.That(span.GetStart().GetNormalizedCharacterOffset(), Is.EqualTo(normalizedStart));
            Assert.That(span.GetEnd().GetNormalizedCharacterOffset(), Is.EqualTo(normalizedEnd));
            Assert.That(span.GetStart().GetCharacterOffset(), Is.EqualTo(originalStart));
            Assert.That(span.GetEnd().GetCharacterOffset(), Is.EqualTo(originalEnd));
        });
    }
}
