using email;

namespace email.Tests;

public class EmailHtmlTokenizerTests
{
    [Test]
    public void TokenizesTagsTextAttributesCommentsAndDoctype()
    {
        string html = "<!DOCTYPE html><DIV ID='first' id=second disabled data-x=a&amp;b>Hi&nbsp;&#x1F600;<br/></DIV><!--ok-->";

        EmailHtmlTokenizationResult result = new EmailHtmlTokenizer().TokenizeWithHtml(html);

        Assert.That(result.GetTokenCount(), Is.EqualTo(6));
        Assert.That(result.GetToken(0).GetKind(), Is.EqualTo(EmailHtmlTokenKind.Doctype));
        Assert.That(result.GetToken(0).GetData(), Is.EqualTo("html"));

        EmailHtmlToken start = result.GetToken(1);
        Assert.That(start.GetKind(), Is.EqualTo(EmailHtmlTokenKind.StartTag));
        Assert.That(start.GetName(), Is.EqualTo("div"));
        Assert.That(start.GetAttributeCount(), Is.EqualTo(3));
        Assert.That(start.GetAttributeNamed("ID")!.GetValue(), Is.EqualTo("first"));
        Assert.That(start.GetAttributeNamed("disabled")!.HasValue(), Is.False);
        Assert.That(start.GetAttributeNamed("data-x")!.GetRawValue(), Is.EqualTo("a&amp;b"));
        Assert.That(start.GetAttributeNamed("data-x")!.GetValue(), Is.EqualTo("a&b"));
        Assert.That(start.IsMalformed(), Is.True);
        Assert.That(start.GetSourceOffset(), Is.EqualTo(15));
        Assert.That(start.GetSourceLength(), Is.EqualTo(50));

        Assert.That(result.GetToken(2).GetKind(), Is.EqualTo(EmailHtmlTokenKind.Text));
        Assert.That(result.GetToken(2).GetData(), Is.EqualTo("Hi\u00a0😀"));
        Assert.That(result.GetToken(3).GetName(), Is.EqualTo("br"));
        Assert.That(result.GetToken(3).IsSelfClosing(), Is.True);
        Assert.That(result.GetToken(4).GetKind(), Is.EqualTo(EmailHtmlTokenKind.EndTag));
        Assert.That(result.GetToken(4).GetName(), Is.EqualTo("div"));
        Assert.That(result.GetToken(5).GetKind(), Is.EqualTo(EmailHtmlTokenKind.Comment));
        Assert.That(result.GetToken(5).GetData(), Is.EqualTo("ok"));
        Assert.That(result.GetMaximumObservedDepth(), Is.EqualTo(1));
        Assert.That(result.GetMalformedMarkupCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HtmlMarkupMalformed), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void RawTextAndRcDataDoNotTokenizeEmbeddedMarkup()
    {
        string html = "<script>if (a < b) x='&amp;';</script>"
            + "<style>.x{content:'<b>'}</style>"
            + "<textarea>A&amp;<b></textarea>";

        EmailHtmlTokenizationResult result = new EmailHtmlTokenizer().TokenizeWithHtml(html);

        Assert.That(result.GetTokenCount(), Is.EqualTo(9));
        Assert.That(result.GetToken(1).GetKind(), Is.EqualTo(EmailHtmlTokenKind.Text));
        Assert.That(result.GetToken(1).IsRawText(), Is.True);
        Assert.That(result.GetToken(1).GetData(), Is.EqualTo("if (a < b) x='&amp;';"));
        Assert.That(result.GetToken(4).GetData(), Is.EqualTo(".x{content:'<b>'}"));
        Assert.That(result.GetToken(7).IsRawText(), Is.True);
        Assert.That(result.GetToken(7).GetData(), Is.EqualTo("A&<b>"));
        Assert.That(result.GetToken(8).GetKind(), Is.EqualTo(EmailHtmlTokenKind.EndTag));
        Assert.That(result.GetToken(8).GetName(), Is.EqualTo("textarea"));
        Assert.That(result.GetMalformedMarkupCount(), Is.Zero);
    }

    [Test]
    public void MalformedInputRecoversConservativelyAndReportsAggregates()
    {
        string html = "<3 &bogus; <div a=1 a=2>ok</div><p id='x'class='y'>z</p><?pi><broken title='x";

        EmailHtmlTokenizationResult result = new EmailHtmlTokenizer().TokenizeWithHtml(html);

        Assert.That(result.GetTokens().Any(token => token.GetKind() == EmailHtmlTokenKind.Text && token.GetData().Contains("<3 &bogus;")), Is.True);
        Assert.That(result.GetTokens().Any(token => token.GetKind() == EmailHtmlTokenKind.Comment && token.GetData() == "pi"), Is.True);
        Assert.That(result.GetTokens().Last().GetKind(), Is.EqualTo(EmailHtmlTokenKind.Text));
        Assert.That(result.GetTokens().Last().GetData(), Is.EqualTo("<broken title='x"));
        Assert.That(result.GetMalformedMarkupCount(), Is.EqualTo(4));
        Assert.That(result.GetMalformedEntityCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HtmlMarkupMalformed), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HtmlEntityMalformed), Is.True);
    }

    [Test]
    public void InputTokenAttributeDepthAndOutputLimitsAreEnforced()
    {
        EmailHtmlLimits input = new EmailHtmlLimits();
        input.SetMaxInputCharacters(3);
        AssertLimit("MaxHtmlInputCharacters", () => new EmailHtmlTokenizer().TokenizeWithHtmlAndLimits("four", input));

        EmailHtmlLimits tokens = new EmailHtmlLimits();
        tokens.SetMaxTokenCount(1);
        AssertLimit("MaxHtmlTokenCount", () => new EmailHtmlTokenizer().TokenizeWithHtmlAndLimits("<p>x", tokens));

        EmailHtmlLimits attributes = new EmailHtmlLimits();
        attributes.SetMaxAttributesPerTag(1);
        AssertLimit("MaxHtmlAttributesPerTag", () => new EmailHtmlTokenizer().TokenizeWithHtmlAndLimits("<p a=1 b=2>", attributes));

        EmailHtmlLimits depth = new EmailHtmlLimits();
        depth.SetMaxNestingDepth(1);
        AssertLimit("MaxHtmlNestingDepth", () => new EmailHtmlTokenizer().TokenizeWithHtmlAndLimits("<div><span>x</span></div>", depth));

        EmailHtmlLimits output = new EmailHtmlLimits();
        output.SetMaxOutputCharacters(3);
        AssertLimit("MaxHtmlOutputCharacters", () => new EmailHtmlTokenizer().TokenizeWithHtmlAndLimits("<p>four</p>", output));
    }

    [Test]
    public void ResultsExposeDefensiveCollectionsAndValidateInputs()
    {
        EmailHtmlTokenizationResult result = new EmailHtmlTokenizer().TokenizeWithHtml("<a href='x'>text</a>");
        List<EmailHtmlToken> tokens = result.GetTokens();
        List<EmailHtmlAttribute> attributes = result.GetToken(0).GetAttributes();
        EmailIssueList issues = result.GetIssues();
        tokens.Clear();
        attributes.Clear();
        issues.Clear();

        Assert.That(result.GetTokenCount(), Is.EqualTo(3));
        Assert.That(result.GetToken(0).GetAttributeCount(), Is.EqualTo(1));
        Assert.That(result.GetSourceCharacterCount(), Is.EqualTo(20));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetToken(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetToken(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetToken(0).GetAttribute(2));
        Assert.Throws<ArgumentNullException>(() => new EmailHtmlTokenizer().TokenizeWithHtml(null!));
        Assert.Throws<ArgumentNullException>(() => new EmailHtmlTokenizer().TokenizeWithHtmlAndLimits("", null!));
    }

    static void AssertLimit(string expectedName, TestDelegate action)
    {
        EmailLimitException exception = Assert.Throws<EmailLimitException>(action);
        Assert.That(exception.GetLimitName(), Is.EqualTo(expectedName));
    }
}
