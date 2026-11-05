using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMarkdownSecurityTests
{
    [Test]
    public void UntrustedPipelineEmitsNoActiveHtmlOrUnsafeUrl()
    {
        string html = "<h2 onclick='alert(1)'>Welcome <em>friend</em></h2>"
            + "<script>evil()</script><style>body{display:none}</style>"
            + "<iframe srcdoc='<script>bad()</script>'>frame</iframe>"
            + "<svg><script>svgBad()</script><a href='javascript:x'>"
            + "svg link</a></svg><p>"
            + "<a href='jav&#x09;ascript:alert(1)'>script link</a> "
            + "<a href='data:text/html,&lt;script&gt;bad&lt;/script&gt;'>"
            + "data link</a> "
            + "<a href='https://user:secret@example.test/private'>"
            + "credential link</a> "
            + "<a href='https://safe.example/path'>safe link</a> "
            + "<img alt='Photo &lt;script&gt;alert(1)&lt;/script&gt;' "
            + "src='javascript:alert(1)' onerror='bad()'></p>"
            + "<p>&lt;script src=x&gt;literal&lt;/script&gt;</p>"
            + "<details open><summary>More</summary><p>Body</p></details>";
        HtmlLimits limits = HtmlLimits.CreateUntrusted();
        HtmlParseOptions parseOptions = HtmlParseOptions.CreateDefault();
        parseOptions.SetLimits(limits);

        HtmlSanitizeResult sanitized = HtmlSanitizer.Sanitize(html,
            HtmlSanitizerPolicy.CreateRichText().Compile(), parseOptions,
            CancellationToken.None);
        HtmlMarkdownOptions markdownOptions =
            HtmlMarkdownOptions.CreateCommonMark();
        markdownOptions.SetLimits(limits);
        HtmlMarkdownResult converted = HtmlMarkdownConverter.Convert(
            sanitized.GetNode(), markdownOptions, CancellationToken.None);
        string markdown = converted.GetMarkdown();

        Assert.Multiple(() =>
        {
            Assert.That(sanitized.IsSuccess(), Is.True);
            Assert.That(sanitized.GetReport().GetReasonCount(
                HtmlSanitizerChangeReason.SensitiveElementDropped),
                Is.GreaterThan(0));
            Assert.That(sanitized.GetReport().GetReasonCount(
                HtmlSanitizerChangeReason.UnsafeUrl),
                Is.GreaterThan(0));
            Assert.That(converted.IsSuccess(), Is.True);
            Assert.That(converted.IsComplete(), Is.True);
            Assert.That(markdownOptions.GetEnableTrustedRawHtml(), Is.False);
            Assert.That(markdown, Does.Contain(
                "[safe link](https://safe.example/path)"));
            Assert.That(markdown, Does.Contain(
                "\\<script src=x>literal\\</script>"));
            Assert.That(markdown, Does.Not.Contain("evil()"));
            Assert.That(markdown, Does.Not.Contain("svgBad()"));
            Assert.That(markdown, Does.Not.Contain("javascript:")
                .IgnoreCase);
            Assert.That(markdown, Does.Not.Contain("data:text/html")
                .IgnoreCase);
            Assert.That(markdown, Does.Not.Contain("user:secret"));
            Assert.That(ContainsUnescapedHtmlTag(markdown), Is.False);
            Assert.That(ContainsUnsafeLinkDestination(markdown), Is.False);
        });
    }

    static bool ContainsUnescapedHtmlTag(string markdown)
    {
        int index = 0;
        while (index + 1 < markdown.Length)
        {
            if (markdown[index] != '<' || IsBackslashEscaped(markdown, index))
            {
                index = index + 1;
                continue;
            }

            char next = markdown[index + 1];
            if (next == '/' || next == '!' || next == '?'
                || next >= 'A' && next <= 'Z'
                || next >= 'a' && next <= 'z')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool ContainsUnsafeLinkDestination(string markdown)
    {
        StringBuilder normalized = new StringBuilder(markdown.Length);
        int index = 0;
        while (index < markdown.Length)
        {
            char current = markdown[index];
            if (current > 32 && current != 127)
            {
                normalized.Append(char.ToLowerInvariant(current));
            }

            index = index + 1;
        }

        string value = normalized.ToString();
        return value.Contains("](javascript:", StringComparison.Ordinal)
            || value.Contains("](vbscript:", StringComparison.Ordinal)
            || value.Contains("](data:", StringComparison.Ordinal);
    }

    static bool IsBackslashEscaped(string value, int index)
    {
        int backslashes = 0;
        index = index - 1;
        while (index >= 0 && value[index] == '\\')
        {
            backslashes = backslashes + 1;
            index = index - 1;
        }

        return backslashes % 2 == 1;
    }
}
