using System.Text;
using email;

namespace email.Tests;

public class EmailHtmlProcessorTests
{
    [Test]
    public void ConvertsBlocksEntitiesAndCollapsedWhitespace()
    {
        string html = "<!doctype html><html><head><title>Hidden</title></head><body>"
            + "<h1> Welcome&nbsp; </h1><p>Hello <strong>world</strong>.<br>Next   line.</p>"
            + "<div>Tail</div></body></html>";

        EmailHtmlTextResult result = new EmailHtmlProcessor().ToTextWithHtml(html);

        Assert.That(result.GetText(), Is.EqualTo("Welcome\n\nHello world.\nNext line.\n\nTail"));
        Assert.That(result.GetSkippedHiddenElementCount(), Is.EqualTo(1));
        Assert.That(result.GetTokenizationResult().GetTokenCount(), Is.GreaterThan(0));
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void ConvertsNestedListsAndOrderedValues()
    {
        string html = "<ul><li>One</li><li>Two<ul><li>Nested</li></ul></li></ul>"
            + "<ol start='3'><li>A</li><li value='7'>B</li></ol>";

        string text = new EmailHtmlProcessor().ToTextWithHtml(html).GetText();

        Assert.That(text, Is.EqualTo(
            "- One\n" +
            "- Two\n" +
            "  - Nested\n" +
            "3. A\n" +
            "7. B"));
    }

    [Test]
    public void PreservesBlockQuotesAndPreformattedWhitespace()
    {
        string html = "<blockquote><p>First<br>Second</p><blockquote>Nested</blockquote></blockquote>"
            + "<pre>\n  a\r\n b</pre>";

        string text = new EmailHtmlProcessor().ToTextWithHtml(html).GetText();

        Assert.That(text, Is.EqualTo(
            "> First\n" +
            "> Second\n\n" +
            ">> Nested\n\n" +
            "  a\n" +
            " b"));
    }

    [Test]
    public void SkipsHiddenRawAndInlineStyledContent()
    {
        string html = "Visible <script>bad <b>&amp;</script><style>.bad{}</style>"
            + "<div hidden>secret<span>nested</span></div>"
            + "<div aria-hidden='true'>aria</div><p style='display : none'>css</p>"
            + "<div hidden>still hidden</wrong>also hidden</div>"
            + "<img alt='Logo'> done";

        EmailHtmlTextResult result = new EmailHtmlProcessor().ToTextWithHtml(html);

        Assert.That(result.GetText(), Is.EqualTo("Visible Logo done"));
        Assert.That(result.GetSkippedHiddenElementCount(), Is.EqualTo(6));
        Assert.That(result.GetAppendedImageAlternativeCount(), Is.EqualTo(1));
    }

    [Test]
    public void EmitsUsefulLinkTargetsAndImageAlternatives()
    {
        string html = "<p>Visit <a href='https://example.test/a?x=1&amp;y=2'>our site</a>, "
            + "<a href='https://same'>https://same</a>, <a href='#local'>section</a>, "
            + "<a href='https://empty'></a>.</p><img alt='Chart &amp; graph'>";

        EmailHtmlTextResult result = new EmailHtmlProcessor().ToTextWithHtml(html);

        Assert.That(result.GetText(), Is.EqualTo(
            "Visit our site <https://example.test/a?x=1&y=2>, https://same, section, https://empty.\n\nChart & graph"));
        Assert.That(result.GetAppendedLinkTargetCount(), Is.EqualTo(2));
        Assert.That(result.GetAppendedImageAlternativeCount(), Is.EqualTo(1));

        EmailHtmlTextOptions suppressed = new EmailHtmlTextOptions();
        suppressed.SetIncludeLinkTargets(false);
        suppressed.SetIncludeImageAlternativeText(false);
        Assert.That(new EmailHtmlProcessor().ToTextWithHtmlAndOptions(html, suppressed).GetText(), Is.EqualTo(
            "Visit our site, https://same, section, ."));
    }

    [Test]
    public void ConvertsTableRowsAndCells()
    {
        string html = "<table><tr><th>A</th><th>B</th></tr><tr><td>1</td><td>2</td></tr></table>";

        Assert.That(new EmailHtmlProcessor().ToTextWithHtml(html).GetText(), Is.EqualTo("A\tB\n1\t2"));
    }

    [Test]
    public void MalformedMarkupRemainsVisibleAndDiagnosticsAreRetained()
    {
        EmailHtmlTextResult result = new EmailHtmlProcessor().ToTextWithHtml("Hello <broken title='x");

        Assert.That(result.GetText(), Is.EqualTo("Hello <broken title='x"));
        Assert.That(result.GetTokenizationResult().GetMalformedMarkupCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HtmlMarkupMalformed), Is.True);
        result.GetIssues().Clear();
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HtmlMarkupMalformed), Is.True);
    }

    [Test]
    public void MimeOverloadDecodesTransferEncodingAndCharsetFirst()
    {
        MimeEntity entity = ParseEntity(
            "Content-Type: text/html; charset=windows-1252\r\nContent-Transfer-Encoding: base64\r\n\r\n",
            Encoding.ASCII.GetBytes(Convert.ToBase64String(new byte[] { 0x3c, 0x70, 0x3e, 0x63, 0x61, 0x66, 0xe9, 0x3c, 0x2f, 0x70, 0x3e })));

        EmailHtmlTextResult result = new EmailHtmlProcessor().ToTextWithEntity(entity);

        Assert.That(result.GetText(), Is.EqualTo("café"));
        Assert.That(result.HasSourceTextDecodeResult(), Is.True);
        Assert.That(result.GetSourceTextDecodeResult()!.GetSelectedCharset(), Is.EqualTo("windows-1252"));
        Assert.Throws<ArgumentException>(() => new EmailHtmlProcessor().ToTextWithEntity(new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("plain", "plain", "utf-8")));
    }

    [Test]
    public void InputUrlAndOutputLimitsAreEnforcedAndOptionsAreSnapshots()
    {
        EmailHtmlLimits limits = new EmailHtmlLimits();
        limits.SetMaxInputCharacters(20);
        limits.SetMaxOutputCharacters(3);
        limits.SetMaxUrlCharacters(3);
        EmailHtmlTextOptions options = new EmailHtmlTextOptions();
        options.SetLimits(limits);
        EmailHtmlTextOptions clone = options.Clone();
        limits.SetMaxOutputCharacters(100);
        options.SetIncludeLinkTargets(false);

        AssertLimit("MaxHtmlInputCharacters", () => new EmailHtmlProcessor().ToTextWithHtmlAndOptions("123456789012345678901", clone));
        AssertLimit("MaxHtmlOutputCharacters", () => new EmailHtmlProcessor().ToTextWithHtmlAndOptions("four", clone));
        AssertLimit("MaxHtmlUrlCharacters", () => new EmailHtmlProcessor().ToTextWithHtmlAndOptions("<a href='long'>x</a>", clone));
        Assert.That(new EmailHtmlProcessor().ToTextWithHtmlAndOptions("<script>x</script>ok", clone).GetText(), Is.EqualTo("ok"));
        Assert.That(clone.GetIncludeLinkTargets(), Is.True);
        Assert.That(clone.GetLimits().GetMaxOutputCharacters(), Is.EqualTo(3));
        Assert.Throws<ArgumentNullException>(() => new EmailHtmlProcessor().ToTextWithHtml((string)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailHtmlProcessor().ToTextWithHtmlAndOptions("", null!));
        Assert.Throws<ArgumentNullException>(() => options.SetLimits(null!));
    }

    static void AssertLimit(string expectedName, TestDelegate action)
    {
        EmailLimitException exception = Assert.Throws<EmailLimitException>(action);
        Assert.That(exception.GetLimitName(), Is.EqualTo(expectedName));
    }

    static MimeEntity ParseEntity(string headers, byte[] body)
    {
        byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
        byte[] source = new byte[headerBytes.Length + body.Length];
        Array.Copy(headerBytes, source, headerBytes.Length);
        Array.Copy(body, 0, source, headerBytes.Length, body.Length);
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(source);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetEntity()!;
    }
}
