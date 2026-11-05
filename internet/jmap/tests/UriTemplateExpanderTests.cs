namespace jmap.Tests;

public class UriTemplateExpanderTests
{
    [Test]
    public void UploadTemplateExpandsRequiredAccountVariable()
    {
        UriTemplateExpander expander = new UriTemplateExpander();

        string result = expander.ExpandUploadUrl(
            "https://jmap.example.test/upload/{accountId}/",
            "account_1");

        Assert.That(result, Is.EqualTo("https://jmap.example.test/upload/account_1/"));
    }

    [Test]
    public void DownloadTemplateStrictlyPercentEncodesUtf8AndReservedCharacters()
    {
        UriTemplateExpander expander = new UriTemplateExpander();

        string result = expander.ExpandDownloadUrl(
            "https://jmap.example.test/download/{accountId}/{blobId}/{name}?accept={type}",
            "account/one %2F",
            "blób/✓",
            "image/png; charset=utf-8",
            "résumé 100%.txt");

        Assert.That(
            result,
            Is.EqualTo("https://jmap.example.test/download/account%2Fone%20%252F/bl%C3%B3b%2F%E2%9C%93/r%C3%A9sum%C3%A9%20100%25.txt?accept=image%2Fpng%3B%20charset%3Dutf-8"));
    }

    [Test]
    public void EventSourceTemplateExpandsEveryRequiredVariable()
    {
        UriTemplateExpander expander = new UriTemplateExpander();

        string selectedTypes = expander.ExpandEventSourceUrl(
            "https://jmap.example.test/events?types={types}&closeafter={closeafter}&ping={ping}",
            "Email,Mailbox",
            "state",
            "30");
        string everyType = expander.ExpandEventSourceUrl(
            "https://jmap.example.test/events?types={types}&closeafter={closeafter}&ping={ping}",
            "*",
            "no",
            "0");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(selectedTypes, Is.EqualTo("https://jmap.example.test/events?types=Email%2CMailbox&closeafter=state&ping=30"));
            Assert.That(everyType, Is.EqualTo("https://jmap.example.test/events?types=%2A&closeafter=no&ping=0"));
        }
    }

    [Test]
    public void EmptyValuesAreDefinedAndRepeatedVariablesRemainStable()
    {
        UriTemplateExpander expander = new UriTemplateExpander();

        string result = expander.ExpandUploadUrl("https://example.test/{accountId}/again/{accountId}", "");

        Assert.That(result, Is.EqualTo("https://example.test//again/"));
    }

    [Test]
    public void EveryUnreservedCharacterRemainsLiteral()
    {
        UriTemplateExpander expander = new UriTemplateExpander();

        string result = expander.ExpandUploadUrl("https://example.test/{accountId}", "AZaz09-._~");

        Assert.That(result, Is.EqualTo("https://example.test/AZaz09-._~"));
    }

    [Test]
    public void TemplateLiteralsPreserveTripletsAndEncodeUnicode()
    {
        UriTemplateExpander expander = new UriTemplateExpander();

        string result = expander.ExpandUploadUrl("https://example.test/café/%2f/{accountId}", "a");

        Assert.That(result, Is.EqualTo("https://example.test/caf%C3%A9/%2f/a"));
    }

    [Test]
    public void MissingExpansionValueReportsTheEndpointPath()
    {
        UriTemplateExpander expander = new UriTemplateExpander();

        JmapProtocolException exception = Assert.Throws<JmapProtocolException>(() =>
            expander.ExpandUploadUrl("https://example.test/{accountId}/{future}", "a"))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.Message, Does.Contain("future"));
            Assert.That(exception.Path, Is.EqualTo("/uploadUrl"));
        }
    }

    [TestCase("https://example.test/upload")]
    [TestCase("https://example.test/{other}")]
    public void MissingRequiredEndpointVariableIsRejected(string template)
    {
        UriTemplateExpander expander = new UriTemplateExpander();

        JmapProtocolException exception = Assert.Throws<JmapProtocolException>(() =>
            expander.ExpandUploadUrl(template, "a"))!;

        Assert.That(exception.Path, Is.EqualTo("/uploadUrl"));
    }

    [TestCase("https://example.test/{blobId}/{type}/{name}", "accountId")]
    [TestCase("https://example.test/{accountId}/{type}/{name}", "blobId")]
    [TestCase("https://example.test/{accountId}/{blobId}/{name}", "type")]
    [TestCase("https://example.test/{accountId}/{blobId}/{type}", "name")]
    public void DownloadTemplateRequiresEveryRfcVariable(string template, string missingVariable)
    {
        UriTemplateExpander expander = new UriTemplateExpander();

        JmapProtocolException exception = Assert.Throws<JmapProtocolException>(() =>
            expander.ExpandDownloadUrl(template, "a", "b", "text/plain", "file.txt"))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.Message, Does.Contain(missingVariable));
            Assert.That(exception.Path, Is.EqualTo("/downloadUrl"));
        }
    }

    [TestCase("https://example.test/?closeafter={closeafter}&ping={ping}", "types")]
    [TestCase("https://example.test/?types={types}&ping={ping}", "closeafter")]
    [TestCase("https://example.test/?types={types}&closeafter={closeafter}", "ping")]
    public void EventSourceTemplateRequiresEveryRfcVariable(string template, string missingVariable)
    {
        UriTemplateExpander expander = new UriTemplateExpander();

        JmapProtocolException exception = Assert.Throws<JmapProtocolException>(() =>
            expander.ExpandEventSourceUrl(template, "*", "no", "0"))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.Message, Does.Contain(missingVariable));
            Assert.That(exception.Path, Is.EqualTo("/eventSourceUrl"));
        }
    }

    [TestCase("https://example.test/{+accountId}")]
    [TestCase("https://example.test/{accountId,other}")]
    [TestCase("https://example.test/{accountId:2}")]
    [TestCase("https://example.test/{accountId*}")]
    [TestCase("https://example.test/{}")]
    [TestCase("https://example.test/{account-id}")]
    [TestCase("https://example.test/{accountId")]
    [TestCase("https://example.test/accountId}")]
    [TestCase("https://example.test/{{accountId}}")]
    [TestCase("https://example.test/%GG/{accountId}")]
    [TestCase("https://example.test/raw space/{accountId}")]
    [TestCase("https://example.test/\u0085/{accountId}")]
    public void NonLevelOneOrMalformedTemplatesAreRejected(string template)
    {
        UriTemplateExpander expander = new UriTemplateExpander();

        Assert.That(
            () => expander.ExpandUploadUrl(template, "a"),
            Throws.TypeOf<JmapProtocolException>());
    }

    [Test]
    public void MissingAndInvalidVariableValuesAreRejected()
    {
        UriTemplateExpander expander = new UriTemplateExpander();
        string invalidUnicode = new string(new[] { '\ud800' });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => expander.ExpandUploadUrl("https://example.test/{accountId}", null),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(
                () => expander.ExpandUploadUrl("https://example.test/{accountId}", invalidUnicode),
                Throws.TypeOf<JmapProtocolException>());
        }
    }
}
