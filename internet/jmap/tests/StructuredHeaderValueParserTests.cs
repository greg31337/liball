using System.Text.Json;

namespace jmap.Tests;

public class StructuredHeaderValueParserTests
{
    [Test]
    public void ParsesMessageIdsAndContentIdAfterRemovingCfws()
    {
        StructuredHeaderValueParser parser = new StructuredHeaderValueParser();
        string value = " (first) <one@example.test>\r\n\t(second) <\"odd id\"@[IPv6:2001:db8::1]> ";

        List<string>? messageIds = parser.ParseMessageIds(value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(messageIds, Is.EqualTo(new[]
            {
                "one@example.test",
                "\"odd id\"@[IPv6:2001:db8::1]"
            }));
            Assert.That(parser.ParseContentId(" <part.1@example.test> (image)"), Is.EqualTo("part.1@example.test"));
            Assert.That(parser.ParseContentId("<one@example.test> <two@example.test>"), Is.Null);
            Assert.That(parser.ParseMessageIds("one@example.test"), Is.Null);
            Assert.That(parser.ParseMessageIds("<missing-domain>"), Is.Null);
            Assert.That(parser.ParseMessageIds("<bad,id@example.test>"), Is.Null);
            Assert.That(parser.ParseMessageIds("<one@example.test> stray"), Is.Null);
        }
    }

    [Test]
    public void ParsesAndNormalizesModernObsoleteAndLeapSecondDates()
    {
        StructuredHeaderValueParser parser = new StructuredHeaderValueParser();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parser.ParseDate("Fri, 11 Sep 2026 14:05:06 -0700"),
                Is.EqualTo("2026-09-11T14:05:06-07:00"));
            Assert.That(parser.ParseUtcDate("Fri, 11 Sep 2026 14:05:06 -0700"),
                Is.EqualTo("2026-09-11T21:05:06Z"));
            Assert.That(parser.ParseDate("11 (day) Sep 26 14 : 05 PDT"),
                Is.EqualTo("2026-09-11T14:05:00-07:00"));
            Assert.That(parser.ParseDate("1 Jan 49 00:00 GMT"),
                Is.EqualTo("2049-01-01T00:00:00+00:00"));
            Assert.That(parser.ParseDate("1 Jan 2026 00:00 XYZ"),
                Is.EqualTo("2026-01-01T00:00:00+00:00"));
            Assert.That(parser.ParseDate("31 Dec 2016 23:59:60 +0000"),
                Is.EqualTo("2016-12-31T23:59:60+00:00"));
            Assert.That(parser.ParseUtcDate("31 Dec 2016 23:59:60 +0000"),
                Is.EqualTo("2017-01-01T00:00:00Z"));
        }
    }

    [Test]
    public void RejectsSemanticallyInvalidDates()
    {
        StructuredHeaderValueParser parser = new StructuredHeaderValueParser();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parser.ParseDate("Thu, 11 Sep 2026 14:05:06 -0700"), Is.Null);
            Assert.That(parser.ParseDate("31 Feb 2026 14:05:06 +0000"), Is.Null);
            Assert.That(parser.ParseDate("11 Sep 2026 24:00:00 +0000"), Is.Null);
            Assert.That(parser.ParseDate("11 Sep 2026 14:05:06 +2460"), Is.Null);
            Assert.That(parser.ParseDate("not a date"), Is.Null);
            Assert.That(parser.ParseDate("11 Sep 2026 14:05:06 (unfinished"), Is.Null);
        }
    }

    [Test]
    public void ParsesRfc2369UrlsInPreferenceOrder()
    {
        StructuredHeaderValueParser parser = new StructuredHeaderValueParser();
        string value = " (help) <https://lists.example.test/help>,\r\n" +
            " <mailto:list@example.test?subject=help> (email help)";

        List<string>? urls = parser.ParseUrls(value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(urls, Is.EqualTo(new[]
            {
                "https://lists.example.test/help",
                "mailto:list@example.test?subject=help"
            }));
            Assert.That(parser.ParseUrls("<https://example.test/a folded path>"),
                Is.EqualTo(new[] { "https://example.test/afoldedpath" }));
            Assert.That(parser.ParseUrls("<https://example.test/first>, invalid remainder"),
                Is.EqualTo(new[] { "https://example.test/first" }));
            Assert.That(parser.ParseUrls("NO"), Is.Null);
            Assert.That(parser.ParseUrls("https://example.test/no-angles"), Is.Null);
            Assert.That(parser.ParseUrls("<relative/path>"), Is.Null);
        }
    }

    [Test]
    public void ParsesContentLocationsAsAbsoluteOrRelativeUriReferences()
    {
        StructuredHeaderValueParser parser = new StructuredHeaderValueParser();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parser.ParseContentLocation(" https://cdn.example.test/image.png "),
                Is.EqualTo("https://cdn.example.test/image.png"));
            Assert.That(parser.ParseContentLocation(" images/logo.png (inline image)"),
                Is.EqualTo("images/logo.png"));
            Assert.That(parser.ParseContentLocation("images/bad path.png"), Is.Null);
            Assert.That(parser.ParseContentLocation("images/bad%2.png"), Is.Null);
            Assert.That(parser.ParseContentLocation(" (comment only)"), Is.Null);
        }
    }

    [Test]
    public void SelectorPreservesRepeatedValueShapesAndParseFailures()
    {
        HeaderPropertySelector selector = new HeaderPropertySelector();
        List<EmailHeader> headers = new List<EmailHeader>
        {
            new EmailHeader { Name = "References", Value = " <one@example.test> <two@example.test>" },
            new EmailHeader { Name = "references", Value = " malformed" },
            new EmailHeader { Name = "Date", Value = " Fri, 11 Sep 2026 14:05:06 -0700" },
            new EmailHeader { Name = "date", Value = " Fri, 11 Sep 2026 15:05:06 -0700" },
            new EmailHeader { Name = "List-Help", Value = " <https://lists.example.test/help>" }
        };

        JsonElement messageIds = selector.SelectProperty(headers, "header:References:asMessageIds:all");
        JsonElement date = selector.SelectProperty(headers, "header:DATE:asDate");
        JsonElement urls = selector.SelectProperty(headers, "header:list-help:asURLs:all");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(messageIds.GetArrayLength(), Is.EqualTo(2));
            Assert.That(messageIds[0].EnumerateArray().Select(item => item.GetString()),
                Is.EqualTo(new[] { "one@example.test", "two@example.test" }));
            Assert.That(messageIds[1].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(date.GetString(), Is.EqualTo("2026-09-11T15:05:06-07:00"));
            Assert.That(urls.GetArrayLength(), Is.EqualTo(1));
            Assert.That(urls[0][0].GetString(), Is.EqualTo("https://lists.example.test/help"));
        }
    }

    [Test]
    public void SelectorAllowsOnlySpecifiedKnownHeadersForEachForm()
    {
        HeaderPropertySelector selector = new HeaderPropertySelector();
        string[] messageIdNames = { "Message-ID", "In-Reply-To", "References", "Resent-Message-ID" };
        string[] dateNames = { "Date", "Resent-Date" };
        string[] urlNames =
        {
            "List-Help", "List-Unsubscribe", "List-Subscribe", "List-Post", "List-Owner", "List-Archive"
        };

        foreach (string name in messageIdNames)
        {
            Assert.That(() => selector.SelectProperty(new List<EmailHeader>(), "header:" + name + ":asMessageIds"),
                Throws.Nothing, name);
        }

        foreach (string name in dateNames)
        {
            Assert.That(() => selector.SelectProperty(new List<EmailHeader>(), "header:" + name + ":asDate"),
                Throws.Nothing, name);
        }

        foreach (string name in urlNames)
        {
            Assert.That(() => selector.SelectProperty(new List<EmailHeader>(), "header:" + name + ":asURLs"),
                Throws.Nothing, name);
        }

        Assert.Multiple(() =>
        {
            Assert.That(() => selector.SelectProperty(new List<EmailHeader>(), "header:Date:asURLs"),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => selector.SelectProperty(new List<EmailHeader>(), "header:List-Help:asDate"),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => selector.SelectProperty(new List<EmailHeader>(), "header:Message-ID:asURLs"),
                Throws.TypeOf<JmapValidationException>());
        });
    }
}
