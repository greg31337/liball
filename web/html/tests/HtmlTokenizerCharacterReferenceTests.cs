
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTokenizerCharacterReferenceTests
{
    [Test]
    public void QuotedAndUnquotedAttributesDecodeAllReferenceForms()
    {
        HtmlTokenReadResult result = Tokenize(
            "<p a=\"&amp;&NotEqualTilde;&#x1F4A9;\" b='&copy' c=&gt;>").Read();
        HtmlAttribute[] attributes = result.GetToken().GetAttributes();

        Assert.Multiple(() =>
        {
            Assert.That(attributes.Select(value => value.GetValue()),
                Is.EqualTo(new[] { "&≂̸💩", "©", ">" }));
            Assert.That(result.GetDiagnostics().Select(value => value.GetCode()),
                Is.EqualTo(new[]
                {
                    HtmlDiagnosticCode.CharacterReferenceMissingSemicolon
                }));
        });
    }

    [Test]
    public void AttributeRollbackPreservesLegacyNamesBeforeEqualsOrAlphanumeric()
    {
        HtmlTokenReadResult result = Tokenize(
            "<p a=\"&notit;\" b='&amp=value' c=\"&copy!\" d=&notinX>").Read();
        HtmlAttribute[] attributes = result.GetToken().GetAttributes();

        Assert.Multiple(() =>
        {
            Assert.That(attributes.Select(value => value.GetValue()),
                Is.EqualTo(new[] { "&notit;", "&amp=value", "©!", "&notinX" }));
            Assert.That(result.GetDiagnostics().Select(value => value.GetCode()),
                Is.EqualTo(new[]
                {
                    HtmlDiagnosticCode.CharacterReferenceMissingSemicolon
                }));
        });
    }

    [Test]
    public void UnknownNamedReferencesRemainLiteralInEveryAttributeForm()
    {
        string input = "<p a=\"&bogus;\" b='&bogus;' c=&bogus;>";

        HtmlTokenReadResult result = Tokenize(input).Read();

        Assert.That(result.GetToken().GetAttributes().Select(value => value.GetValue()),
            Is.EqualTo(new[] { "&bogus;", "&bogus;", "&bogus;" }));
        Assert.That(result.GetDiagnostics().Select(value => value.GetCode()),
            Is.EqualTo(new[]
            {
                HtmlDiagnosticCode.CharacterReferenceInvalid,
                HtmlDiagnosticCode.CharacterReferenceInvalid,
                HtmlDiagnosticCode.CharacterReferenceInvalid
            }));
        Assert.That(result.GetDiagnostics()[0].GetSpan().GetStart()
            .GetNormalizedCharacterOffset(), Is.EqualTo(input.IndexOf('&')));
        Assert.That(result.GetDiagnostics()[0].GetSpan()
            .GetNormalizedCharacterLength(), Is.EqualTo(7));
    }

    [Test]
    public void AttributeReferenceDecodingIsStableAtEveryChunkSplit()
    {
        string input =
            "<p a=\"x&CounterClockwiseContourIntegral;y\" b='&NotEqualTilde;' c=&amp;>";
        for (int split = 0; split < input.Length; split++)
        {
            HtmlTokenizer tokenizer = HtmlTokenizer.Create(
                HtmlTokenizerOptions.CreateDefault());
            tokenizer.Feed(input.AsSpan(0, split), false);
            HtmlTokenReadResult suspension = tokenizer.Read();
            Assert.That(suspension.GetStatus(),
                Is.EqualTo(HtmlTokenReadStatus.NeedMoreInput), $"split {split}");

            tokenizer.Feed(input.AsSpan(split), true);
            HtmlTokenReadResult result = tokenizer.Read();

            Assert.Multiple(() =>
            {
                Assert.That(result.GetToken().GetAttributes()
                    .Select(value => value.GetValue()),
                    Is.EqualTo(new[] { "x∳y", "≂̸", "&" }), $"split {split}");
                Assert.That(suspension.GetDiagnostics()
                    .Concat(result.GetDiagnostics()), Is.Empty, $"split {split}");
            });
        }
    }

    [Test]
    public void AttributeRollbackIsStableAtEveryChunkSplit()
    {
        string input = "<p a=\"&notit;\" b=&amp=value>";
        for (int split = 0; split < input.Length; split++)
        {
            HtmlTokenizer tokenizer = HtmlTokenizer.Create(
                HtmlTokenizerOptions.CreateDefault());
            tokenizer.Feed(input.AsSpan(0, split), false);
            HtmlTokenReadResult suspension = tokenizer.Read();
            tokenizer.Feed(input.AsSpan(split), true);
            HtmlTokenReadResult result = tokenizer.Read();

            Assert.Multiple(() =>
            {
                Assert.That(result.GetToken().GetAttributes()
                    .Select(value => value.GetValue()),
                    Is.EqualTo(new[] { "&notit;", "&amp=value" }),
                    $"split {split}");
                Assert.That(suspension.GetDiagnostics()
                    .Concat(result.GetDiagnostics()).Select(value => value.GetCode()),
                    Is.EqualTo(new[]
                    {
                        HtmlDiagnosticCode.TokenizerUnexpectedCharacter
                    }), $"split {split}");
            });
        }
    }

    [Test]
    public void AdditionalAllowedCharactersLeaveBareAmpersandsLiteral()
    {
        HtmlTokenReadResult result = Tokenize("<p a=\"&\" b='&' c=&>").Read();

        Assert.That(result.GetToken().GetAttributes().Select(value => value.GetValue()),
            Is.EqualTo(new[] { "&", "&", "&" }));
        Assert.That(result.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void NumericReferenceRecoveryDiagnosticsFlowThroughAttributes()
    {
        HtmlTokenReadResult result = Tokenize("<p a=\"&#x80;\" b=&#0;>").Read();

        Assert.That(result.GetToken().GetAttributes().Select(value => value.GetValue()),
            Is.EqualTo(new[] { "€", "�" }));
        Assert.That(result.GetDiagnostics().Select(value => value.GetCode()),
            Is.EqualTo(new[]
            {
                HtmlDiagnosticCode.CharacterReferenceInvalid,
                HtmlDiagnosticCode.CharacterReferenceInvalid
            }));
    }

    [Test]
    public void DataAndRcdataFlushMultiCodePointReferencesInOrder()
    {
        HtmlTokenizerContext data = CreateContext(
            HtmlTokenizerState.Data, null, HtmlTokenizerOptions.CreateDefault());
        data.Feed("&NotEqualTilde;".AsSpan(), true);

        HtmlTokenizerContext rcdata = CreateContext(
            HtmlTokenizerState.Rcdata, "title", HtmlTokenizerOptions.CreateDefault());
        rcdata.Feed("&NotEqualTilde;</title>".AsSpan(), true);

        Assert.Multiple(() =>
        {
            Assert.That(data.Read().GetToken().GetData(), Is.EqualTo("≂̸"));
            Assert.That(rcdata.Read().GetToken().GetData(), Is.EqualTo("≂̸"));
            Assert.That(rcdata.Read().GetToken().GetKind(), Is.EqualTo(HtmlTokenKind.EndTag));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void MultiCodePointReferencesSplitAtConfiguredTokenBoundaries(
        bool coalesce)
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumTokenCharacters(1);
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetLimits(limits);
        options.SetCoalesceCharacterTokens(coalesce);
        HtmlTokenizerContext context = CreateContext(
            HtmlTokenizerState.Data, null, options);
        string input = "&NotEqualTilde;";
        context.Feed(input.AsSpan(), true);

        HtmlTokenReadResult first = context.Read();
        HtmlTokenReadResult second = context.Read();

        Assert.Multiple(() =>
        {
            Assert.That(first.GetToken().GetData(), Is.EqualTo("≂"));
            Assert.That(second.GetToken().GetData(), Is.EqualTo("̸"));
            Assert.That(first.GetToken().GetSourceSpan()
                .GetNormalizedCharacterLength(), Is.EqualTo(input.Length));
            Assert.That(second.GetToken().GetSourceSpan()
                .GetNormalizedCharacterLength(), Is.EqualTo(input.Length));
            Assert.That(context.Read().GetToken().GetKind(),
                Is.EqualTo(HtmlTokenKind.EndOfFile));
        });
    }

    [Test]
    public void ExpandedAttributeReferencesHonorValueAndAggregateLimits()
    {
        HtmlLimits valueLimits = HtmlLimits.CreateDefault();
        valueLimits.SetMaximumAttributeValueCharacters(1);
        HtmlLimits tokenLimits = HtmlLimits.CreateDefault();
        tokenLimits.SetMaximumTokenCharacters(3);

        Assert.Multiple(() =>
        {
            Assert.That(Tokenize("<p a=\"&NotEqualTilde;\">", valueLimits)
                .Read().GetStatus(), Is.EqualTo(HtmlTokenReadStatus.LimitExceeded));
            Assert.That(Tokenize("<p a=\"&NotEqualTilde;\">", tokenLimits)
                .Read().GetStatus(), Is.EqualTo(HtmlTokenReadStatus.LimitExceeded));
        });
    }

    [Test]
    public void ExactExpandedAttributeLimitsStillEmitTheTag()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumAttributeValueCharacters(2);
        limits.SetMaximumTokenCharacters(4);

        HtmlTokenReadResult result = Tokenize(
            "<p a=\"&NotEqualTilde;\">", limits).Read();

        Assert.That(result.GetToken().GetAttributes()[0].GetValue(), Is.EqualTo("≂̸"));
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

    static HtmlTokenizerContext CreateContext(HtmlTokenizerState state,
        string lastStartTagName, HtmlTokenizerOptions options)
    {
        HtmlTokenizerContext context = new HtmlTokenizerContext(options,
            null, false);
        context.SetState(state);
        if (lastStartTagName != null)
        {
            context.SetLastStartTagName(lastStartTagName);
        }

        return context;
    }
}
