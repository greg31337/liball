namespace jmap.Tests;

public class EmailTextMatcherTests
{
    [Test]
    public void ParsesIndependentTokensQuotedPhrasesAndEscapes()
    {
        List<EmailSearchTerm> terms = new EmailTextMatcher().ParseTerms(
            "alpha \"two   words\" 'don\\'t stop' tail");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(terms.Select(term => term.Text), Is.EqualTo(new[]
            {
                "alpha",
                "two words",
                "don't stop",
                "tail"
            }));
            Assert.That(terms.Select(term => term.IsPhrase), Is.EqualTo(new[]
            {
                false,
                true,
                true,
                false
            }));
        }
    }

    [Test]
    public void MatchesAllTokensIndependentlyWithUnicodeCaseAndNormalization()
    {
        EmailTextMatcher matcher = new EmailTextMatcher();

        EmailTextMatchResult reordered = matcher.MatchPlainText("東京 then Cafe\u0301", "CAFÉ 東京");
        EmailTextMatchResult missing = matcher.MatchPlainText("東京 then Cafe\u0301", "CAFÉ absent");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reordered.IsMatch, Is.True);
            Assert.That(reordered.SearchText, Is.EqualTo("東京 then Café"));
            Assert.That(reordered.Ranges, Has.Count.EqualTo(2));
            Assert.That(missing.IsMatch, Is.False);
            Assert.That(missing.Ranges, Is.Empty);
        }
    }

    [Test]
    public void PhraseMatchingPreservesWordOrderAndNormalizesWhitespace()
    {
        EmailTextMatcher matcher = new EmailTextMatcher();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(matcher.MatchPlainText("The quick\n\tbrown fox", "\"QUICK brown\"").IsMatch, Is.True);
            Assert.That(matcher.MatchPlainText("The quick brown fox", "\"brown quick\"").IsMatch, Is.False);
            Assert.That(matcher.MatchPlainText("brown ... quick", "quick brown").IsMatch, Is.True);
            Assert.That(matcher.MatchPlainText("She said \"hi\"", "'said \\\"hi\\\"'").IsMatch, Is.True);
        }
    }

    [Test]
    public void ReportsEveryCaseInsensitiveOccurrenceAsMergedRanges()
    {
        EmailTextMatchResult result = new EmailTextMatcher().MatchPlainText("Alpha beta ALPHA", "alpha");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsMatch, Is.True);
            Assert.That(result.Ranges, Has.Count.EqualTo(2));
            Assert.That(result.Ranges[0].Start, Is.EqualTo(0));
            Assert.That(result.Ranges[0].Length, Is.EqualTo(5));
            Assert.That(result.Ranges[1].Start, Is.EqualTo(11));
            Assert.That(result.Ranges[1].Length, Is.EqualTo(5));
        }
    }

    [Test]
    public void ExtractsOnlyPresentedHtmlTextAndPresentationAttributes()
    {
        string html = "<head><title>secret title</title><style>.private{display:none}</style></head>" +
            "<body title='Inbox &amp; mail'><script>alert('secret')</script>" +
            "<p>Hello <img src='tracker' onerror='evil()' alt='Café &amp; tea'> world</p>" +
            "<!-- hidden comment --><template>template secret</template></body>";
        EmailTextMatcher matcher = new EmailTextMatcher();

        string text = matcher.ExtractHtmlText(html);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Is.EqualTo("Inbox & mail Hello Café & tea world"));
            Assert.That(matcher.ExtractHtmlText("before<script/>hidden after self-close</script>after"),
                Is.EqualTo("before after"));
            Assert.That(matcher.MatchHtmlText(html, "inbox café world").IsMatch, Is.True);
            Assert.That(matcher.MatchHtmlText(html, "secret").IsMatch, Is.False);
            Assert.That(matcher.MatchHtmlText(html, "tracker onerror evil private hidden template").IsMatch,
                Is.False);
        }
    }

    [Test]
    public void DecodesEntitiesAndKeepsNonTagLessThanSignsAsText()
    {
        string html = "<p title=\"A &#x1F642;\">2 &lt; 3 and 4 < 5 &copy; 2026</p>";

        string text = new EmailTextMatcher().ExtractHtmlText(html);

        Assert.That(text, Is.EqualTo("A 🙂 2 < 3 and 4 < 5 © 2026"));
    }

    [Test]
    public void MatchesDecodedHeaderValuesByCaseInsensitiveFieldName()
    {
        List<EmailHeader> headers = new List<EmailHeader>
        {
            new EmailHeader { Name = "Subject", Value = " =?UTF-8?Q?caf=C3=A9_status?=" },
            new EmailHeader { Name = "X-Trace", Value = "plain value" }
        };
        EmailTextMatcher matcher = new EmailTextMatcher();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(matcher.MatchesHeaderList(headers, "subject", "CAFÉ status"), Is.True);
            Assert.That(matcher.MatchesHeaderList(headers, "SUBJECT", null), Is.True);
            Assert.That(matcher.MatchesHeaderList(headers, "X-Trace", "café"), Is.False);
            Assert.That(matcher.MatchesHeaderList(headers, "Missing", null), Is.False);
        }
    }

    [Test]
    public void BuildsEmailSearchTextFromDecodedHeadersAddressesAndSafeBodyText()
    {
        EmailBodyPart html = new EmailBodyPart { PartId = "html", Type = "text/html" };
        EmailBodyPart binary = new EmailBodyPart { PartId = "binary", Type = "application/pdf" };
        Email email = new Email
        {
            Headers = new List<EmailHeader>
            {
                new EmailHeader { Name = "Subject", Value = "=?UTF-8?Q?caf=C3=A9?=" },
                new EmailHeader { Name = "X-Private", Value = "not indexed" }
            },
            To = new List<EmailAddress>
            {
                new EmailAddress { Name = "Zoë", Email = "zoe@example.test" }
            },
            BodyStructure = new EmailBodyPart
            {
                Type = "multipart/mixed",
                SubParts = new List<EmailBodyPart> { html, binary }
            },
            BodyValues = new Dictionary<string, EmailBodyValue>
            {
                ["html"] = new EmailBodyValue
                {
                    Value = "<p>Visible body</p><script>hidden code</script><img alt='diagram'>"
                },
                ["binary"] = new EmailBodyValue { Value = "PDF metadata" }
            }
        };
        EmailTextMatcher matcher = new EmailTextMatcher();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(matcher.MatchEmailSearchText(email, "CAFÉ zoë visible diagram").IsMatch, Is.True);
            Assert.That(matcher.MatchEmailBodyText(email, "visible diagram").IsMatch, Is.True);
            Assert.That(matcher.MatchEmailSearchText(email, "private").IsMatch, Is.False);
            Assert.That(matcher.MatchEmailBodyText(email, "hidden").IsMatch, Is.False);
            Assert.That(matcher.MatchEmailBodyText(email, "PDF").IsMatch, Is.False);
        }
    }

    [Test]
    public void ReplacesMalformedUnicodeAndRemovesControlsWithoutThrowing()
    {
        string malformed = "before" + new string(new[] { '\uD800' }) + "\0after";

        EmailTextMatchResult result = new EmailTextMatcher().MatchPlainText(malformed, "before after");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsMatch, Is.True);
            Assert.That(result.SearchText, Is.EqualTo("before�after"));
        }
    }
}
