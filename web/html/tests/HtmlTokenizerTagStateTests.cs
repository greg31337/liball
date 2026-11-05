
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTokenizerTagStateTests
{
    [Test]
    public void StartAndEndTagsExposeFoldedNamesAndOrderedAttributes()
    {
        string input = "<DIV id=main disabled class='a b' data-x=\"c\">text</DIV>";
        HtmlTokenizer tokenizer = CreateTokenizer(input);

        HtmlTokenReadResult startResult = tokenizer.Read();
        AssertKind(startResult, HtmlTokenKind.StartTag);
        HtmlToken start = startResult.GetToken();
        HtmlAttribute[] attributes = start.GetAttributes();

        Assert.Multiple(() =>
        {
            Assert.That(start.GetName(), Is.EqualTo("div"));
            Assert.That(start.IsSelfClosing(), Is.False);
            Assert.That(start.GetSourceSpan().GetNormalizedCharacterLength(), Is.EqualTo(45));
            Assert.That(attributes.Select(value => value.GetName()),
                Is.EqualTo(new[] { "id", "disabled", "class", "data-x" }));
            Assert.That(attributes.Select(value => value.GetValue()),
                Is.EqualTo(new[] { "main", "", "a b", "c" }));
        });

        AssertCharacter(tokenizer.Read(), "text");
        HtmlTokenReadResult end = tokenizer.Read();
        AssertKind(end, HtmlTokenKind.EndTag);
        Assert.That(end.GetToken().GetName(), Is.EqualTo("div"));
        AssertKind(tokenizer.Read(), HtmlTokenKind.EndOfFile);
    }

    [Test]
    public void SelfClosingTagsAndEndTagAttributesAreRetainedInTokens()
    {
        HtmlTokenizer tokenizer = CreateTokenizer("<img src=x /><hr disabled></hr reason=x />");

        HtmlToken image = tokenizer.Read().GetToken();
        HtmlToken horizontalRule = tokenizer.Read().GetToken();
        HtmlTokenReadResult endResult = tokenizer.Read();
        HtmlToken end = endResult.GetToken();

        Assert.Multiple(() =>
        {
            Assert.That(image.GetKind(), Is.EqualTo(HtmlTokenKind.StartTag));
            Assert.That(image.IsSelfClosing(), Is.True);
            Assert.That(image.GetAttributes()[0].GetValue(), Is.EqualTo("x"));
            Assert.That(horizontalRule.GetAttributes()[0].GetName(), Is.EqualTo("disabled"));
            Assert.That(horizontalRule.GetAttributes()[0].GetValue(), Is.Empty);
            Assert.That(end.GetKind(), Is.EqualTo(HtmlTokenKind.EndTag));
            Assert.That(end.IsSelfClosing(), Is.True);
            Assert.That(end.GetAttributes()[0].GetName(), Is.EqualTo("reason"));
            Assert.That(end.GetAttributes()[0].GetValue(), Is.EqualTo("x"));
            Assert.That(endResult.GetDiagnostics(), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void CompleteTagTokenizationIsIdenticalAtEveryChunkSplit()
    {
        string input = "<input type=\"text\" required data-x='a b' value=c />";
        for (int split = 0; split < input.Length; split++)
        {
            HtmlTokenizer tokenizer = HtmlTokenizer.Create(HtmlTokenizerOptions.CreateDefault());
            tokenizer.Feed(input.AsSpan(0, split), false);
            AssertStatus(tokenizer.Read(), HtmlTokenReadStatus.NeedMoreInput);
            tokenizer.Feed(input.AsSpan(split), true);

            HtmlTokenReadResult result = tokenizer.Read();
            AssertKind(result, HtmlTokenKind.StartTag);
            Assert.That(result.GetToken().GetName(), Is.EqualTo("input"), $"split {split}");
            Assert.That(result.GetToken().IsSelfClosing(), Is.True, $"split {split}");
            Assert.That(result.GetToken().GetAttributes().Select(value => value.GetValue()),
                Is.EqualTo(new[] { "text", "", "a b", "c" }), $"split {split}");
        }
    }

    [Test]
    public void SolidusImmediatelyAfterUnquotedValueBelongsToValue()
    {
        HtmlTokenizer tokenizer = CreateTokenizer("<img src=x/>");

        HtmlToken tag = tokenizer.Read().GetToken();

        Assert.Multiple(() =>
        {
            Assert.That(tag.IsSelfClosing(), Is.False);
            Assert.That(tag.GetAttributes(), Has.Length.EqualTo(1));
            Assert.That(tag.GetAttributes()[0].GetValue(), Is.EqualTo("x/"));
        });
    }

    [Test]
    public void EqualsBeforeNameAndMissingValueFollowTagRecovery()
    {
        HtmlTokenizer tokenizer = CreateTokenizer("<p =foo x=>");

        HtmlTokenReadResult result = tokenizer.Read();
        HtmlAttribute[] attributes = result.GetToken().GetAttributes();

        AssertKind(result, HtmlTokenKind.StartTag);
        Assert.That(attributes.Select(value => value.GetName()),
            Is.EqualTo(new[] { "=foo", "x" }));
        Assert.That(attributes.Select(value => value.GetValue()),
            Is.EqualTo(new[] { "", "" }));
        Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(2));
        Assert.That(result.GetDiagnostics().All(value =>
            value.GetCode() == HtmlDiagnosticCode.TokenizerUnexpectedCharacter), Is.True);
    }

    [Test]
    public void MissingWhitespaceAndUnexpectedSolidusRecoverWithoutLosingAttributes()
    {
        HtmlTokenizer tokenizer = CreateTokenizer("<p a='x'b=y / z=q>");

        HtmlTokenReadResult result = tokenizer.Read();
        HtmlToken token = result.GetToken();

        Assert.That(token.GetAttributes().Select(value => value.GetName()),
            Is.EqualTo(new[] { "a", "b", "z" }));
        Assert.That(token.GetAttributes().Select(value => value.GetValue()),
            Is.EqualTo(new[] { "x", "y", "q" }));
        Assert.That(token.IsSelfClosing(), Is.False);
        Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(2));
    }

    [Test]
    public void InvalidTagOpenersAreReconsumedAsData()
    {
        HtmlTokenizer tokenizer = CreateTokenizer("<1 after");

        HtmlTokenReadResult lessThan = tokenizer.Read();
        AssertCharacter(lessThan, "<");
        Assert.That(lessThan.GetDiagnostics(), Has.Length.EqualTo(1));
        AssertCharacter(tokenizer.Read(), "1 after");
        AssertKind(tokenizer.Read(), HtmlTokenKind.EndOfFile);
    }

    [Test]
    public void EofRecoveryEmitsPrefixesAndAbandonsIncompleteTags()
    {
        HtmlTokenizer lessThan = CreateTokenizer("<");
        HtmlTokenReadResult lessThanPrefix = lessThan.Read();
        AssertCharacter(lessThanPrefix, "<");
        Assert.That(lessThanPrefix.GetDiagnostics(), Has.Length.EqualTo(1));
        AssertKind(lessThan.Read(), HtmlTokenKind.EndOfFile);

        HtmlTokenizer endPrefix = CreateTokenizer("</");
        AssertCharacter(endPrefix.Read(), "</");
        AssertKind(endPrefix.Read(), HtmlTokenKind.EndOfFile);

        HtmlTokenizer incomplete = CreateTokenizer("<div id=");
        HtmlTokenReadResult eof = incomplete.Read();
        AssertKind(eof, HtmlTokenKind.EndOfFile);
        Assert.That(eof.GetDiagnostics(), Has.Length.EqualTo(1));
    }

    [Test]
    public void EmptyEndTagIsIgnoredWithDiagnostic()
    {
        HtmlTokenizer tokenizer = CreateTokenizer("</>tail");

        HtmlTokenReadResult tail = tokenizer.Read();

        AssertCharacter(tail, "tail");
        Assert.That(tail.GetDiagnostics(), Has.Length.EqualTo(1));
        AssertKind(tokenizer.Read(), HtmlTokenKind.EndOfFile);
    }

    static HtmlTokenizer CreateTokenizer(string input)
    {
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(HtmlTokenizerOptions.CreateDefault());
        tokenizer.Feed(input.AsSpan(), true);
        return tokenizer;
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
}
