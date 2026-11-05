using email;

namespace email.Tests;

public class EmailHtmlSanitizerTests
{
    [Test]
    public void RetainsAllowlistedMarkupAndEscapesDecodedContent()
    {
        string html = "<DIV class='x' id='bad' onclick='evil()' style='color:red' data-x='z'>"
            + "<B>safe &amp; <em title='&quot;x&quot;'>sound</em></B></DIV>";

        EmailHtmlSanitizeResult result = new EmailHtmlProcessor().SanitizeWithHtml(html);

        Assert.That(result.GetHtml(), Is.EqualTo("<div class=\"x\"><b>safe &amp; <em title=\"&quot;x&quot;\">sound</em></b></div>"));
        Assert.That(result.GetMutationCount(), Is.EqualTo(4));
        Assert.That(result.GetMutations().All(mutation => mutation.GetKind() == EmailHtmlMutationKind.RemovedAttribute), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HtmlSanitized), Is.True);
        Assert.That(result.WasModified(), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void SuppressesActiveSubtreesButRetainsChildrenOfUnknownContainers()
    {
        string html = "before<script>alert(1)<b>x</b></script><iframe><p>frame</p></iframe>"
            + "<unknown><strong>kept</strong></unknown>after";

        EmailHtmlSanitizeResult result = new EmailHtmlProcessor().SanitizeWithHtml(html);

        Assert.That(result.GetHtml(), Is.EqualTo("before<strong>kept</strong>after"));
        Assert.That(result.GetMutations().Count(mutation => mutation.GetKind() == EmailHtmlMutationKind.RemovedElement), Is.EqualTo(4));
        Assert.That(result.GetHtml(), Does.Not.Contain("alert"));
        Assert.That(result.GetHtml(), Does.Not.Contain("frame"));
    }

    [Test]
    public void AppliesNavigationResourceAndCidUrlPolicies()
    {
        string html = "<a href='java&#x73;cript:alert(1)' onclick='x'>bad</a>"
            + "<a href=' HTTPS://example.test/x '>ok</a>"
            + "<img src='https://remote.test/x' alt='remote'>"
            + "<img src='CID:Logo@example.test' alt='inline'>"
            + "<img src='data:image/svg+xml,%3Csvg%3E' alt='data'>";

        EmailHtmlSanitizeResult result = new EmailHtmlProcessor().SanitizeWithHtml(html);

        Assert.That(result.GetHtml(), Is.EqualTo("<a>bad</a><a href=\"https://example.test/x\">ok</a>"
            + "<img alt=\"remote\"><img src=\"cid:Logo@example.test\" alt=\"inline\"><img alt=\"data\">"));
        Assert.That(result.GetBlockedRemoteResourceCount(), Is.EqualTo(1));
        Assert.That(result.GetBlockedUnsafeUrlCount(), Is.EqualTo(2));
        Assert.That(result.GetPreservedCidResourceCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HtmlRemoteContentBlocked), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HtmlUnsafeUrlBlocked), Is.True);
        Assert.That(result.GetMutations().Any(mutation => mutation.GetKind() == EmailHtmlMutationKind.RewrittenAttribute), Is.True);
    }

    [Test]
    public void ResourceAndRasterDataPoliciesRequireExplicitOptIn()
    {
        EmailHtmlSanitizeOptions options = new EmailHtmlSanitizeOptions();
        options.SetAllowRemoteResources(true);
        options.SetAllowSafeDataImages(true);
        options.SetAllowCidResources(false);

        EmailHtmlSanitizeResult result = new EmailHtmlProcessor().SanitizeWithHtmlAndOptions(
            "<img src='//cdn.test/a'><img src='/local'><img src='data:image/png;base64,AA=='>"
            + "<img src='data:image/pngx;base64,AA=='><img src='cid:logo'>", options);

        Assert.That(result.GetHtml(), Is.EqualTo("<img src=\"//cdn.test/a\"><img src=\"/local\">"
            + "<img src=\"data:image/png;base64,AA==\"><img><img>"));
        Assert.That(result.GetBlockedRemoteResourceCount(), Is.Zero);
        Assert.That(result.GetBlockedUnsafeUrlCount(), Is.EqualTo(2));
        Assert.That(result.GetPreservedCidResourceCount(), Is.Zero);
    }

    [Test]
    public void ProducesBalancedFragmentsAndReportsStructuralRepairs()
    {
        EmailHtmlSanitizeResult result = new EmailHtmlProcessor().SanitizeWithHtml("<p><strong>x</p><!--safe--></strong><em/>");

        Assert.That(result.GetHtml(), Is.EqualTo("<p><strong>x</strong></p><em></em>"));
        Assert.That(result.GetMutations().Count(mutation => mutation.GetKind() == EmailHtmlMutationKind.RebalancedMarkup), Is.EqualTo(3));
        Assert.That(result.GetMutations().Count(mutation => mutation.GetKind() == EmailHtmlMutationKind.RemovedComment), Is.EqualTo(1));

        EmailHtmlSanitizeOptions comments = new EmailHtmlSanitizeOptions();
        comments.SetPreserveComments(true);
        Assert.That(new EmailHtmlProcessor().SanitizeWithHtmlAndOptions("a<!--safe-->b", comments).GetHtml(), Is.EqualTo("a<!--safe-->b"));
    }

    [Test]
    public void MimeOverloadDecodesTextBeforeSanitizing()
    {
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("<p onclick='x'>café</p>", "html", "utf-8");

        EmailHtmlSanitizeResult result = new EmailHtmlProcessor().SanitizeWithEntity(entity);

        Assert.That(result.GetHtml(), Is.EqualTo("<p>café</p>"));
        Assert.That(result.HasSourceTextDecodeResult(), Is.True);
        Assert.That(result.GetSourceTextDecodeResult()!.GetSelectedCharset(), Is.EqualTo("utf-8"));
        Assert.Throws<ArgumentException>(() => new EmailHtmlProcessor().SanitizeWithEntity(new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("plain", "plain", "utf-8")));
    }

    [Test]
    public void EnforcesOutputUrlMutationAndDataLimits()
    {
        EmailHtmlLimits shortOutput = new EmailHtmlLimits();
        shortOutput.SetMaxOutputCharacters(2);
        EmailHtmlSanitizeOptions outputOptions = WithLimits(shortOutput);
        Assert.That(new EmailHtmlProcessor().SanitizeWithHtmlAndOptions("<script>long hidden text</script>ok", outputOptions).GetHtml(), Is.EqualTo("ok"));
        AssertLimit("MaxHtmlOutputCharacters", () => new EmailHtmlProcessor().SanitizeWithHtmlAndOptions("abc", outputOptions));

        EmailHtmlLimits shortUrl = new EmailHtmlLimits();
        shortUrl.SetMaxUrlCharacters(3);
        AssertLimit("MaxHtmlUrlCharacters", () => new EmailHtmlProcessor().SanitizeWithHtmlAndOptions("<a href='long'>x</a>", WithLimits(shortUrl)));

        EmailHtmlSanitizeOptions mutations = new EmailHtmlSanitizeOptions();
        mutations.SetMaximumMutationCount(1);
        AssertLimit("MaxHtmlMutationCount", () => new EmailHtmlProcessor().SanitizeWithHtmlAndOptions("<x></x>", mutations));

        EmailHtmlLimits oneDataUrl = new EmailHtmlLimits();
        oneDataUrl.SetMaxDataUriCount(1);
        EmailHtmlSanitizeOptions data = WithLimits(oneDataUrl);
        data.SetAllowSafeDataImages(true);
        AssertLimit("MaxHtmlDataUriCount", () => new EmailHtmlProcessor().SanitizeWithHtmlAndOptions("<img src='data:image/png,A'><img src='data:image/png,B'>", data));
    }

    [Test]
    public void OptionsAndResultsUseDefensiveSnapshotsAndValidateInputs()
    {
        EmailHtmlLimits limits = new EmailHtmlLimits();
        limits.SetMaxOutputCharacters(50);
        EmailHtmlSanitizeOptions options = new EmailHtmlSanitizeOptions();
        options.SetLimits(limits);
        limits.SetMaxOutputCharacters(1);

        EmailHtmlSanitizeResult result = new EmailHtmlProcessor().SanitizeWithHtmlAndOptions("<x>safe</x>", options);
        List<EmailHtmlMutation> mutations = result.GetMutations();
        EmailIssueList issues = result.GetIssues();
        mutations.Clear();
        issues.Clear();

        Assert.That(result.GetMutationCount(), Is.EqualTo(2));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HtmlSanitized), Is.True);
        Assert.That(options.GetLimits().GetMaxOutputCharacters(), Is.EqualTo(50));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetMutation(-1));
        Assert.Throws<ArgumentNullException>(() => new EmailHtmlProcessor().SanitizeWithHtml((string)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailHtmlProcessor().SanitizeWithHtmlAndOptions("", null!));
        Assert.Throws<ArgumentNullException>(() => options.SetLimits(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumMutationCount(0));
    }

    static EmailHtmlSanitizeOptions WithLimits(EmailHtmlLimits limits)
    {
        EmailHtmlSanitizeOptions options = new EmailHtmlSanitizeOptions();
        options.SetLimits(limits);
        return options;
    }

    static void AssertLimit(string expectedName, TestDelegate action)
    {
        EmailLimitException exception = Assert.Throws<EmailLimitException>(action);
        Assert.That(exception.GetLimitName(), Is.EqualTo(expectedName));
    }
}
