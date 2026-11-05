using System.Text;
using email;

namespace email.Tests;

public class EmailTextSearchTests
{
    [Test]
    public void SearchesDecodedPlainAndRenderedHtmlBodiesInTreeOrder()
    {
        MimeEntity root = ParseSearchTree();

        EmailTextSearchResult result = new EmailTextProcessor().SearchWithRootAndQuery(root, "needle");

        Assert.That(result.GetMatchCount(), Is.EqualTo(2));
        Assert.That(result.GetSearchedEntityCount(), Is.EqualTo(2));
        Assert.That(result.GetMatch(0).GetPath().GetDisplayText(), Is.EqualTo("root/1/1"));
        Assert.That(result.GetMatch(0).IsFromHtml(), Is.False);
        Assert.That(result.GetMatch(1).GetPath().GetDisplayText(), Is.EqualTo("root/1/2"));
        Assert.That(result.GetMatch(1).IsFromHtml(), Is.True);
        Assert.That(result.GetMatch(1).GetContext(), Does.Contain("Needle html"));
        Assert.That(result.GetMatches().Any(match => match.GetContext().Contains("hidden", StringComparison.Ordinal)), Is.False);
        Assert.That(result.WasTruncated(), Is.False);
        Assert.That(root.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void TextAttachmentsAndCaseSensitivityAreExplicitPolicies()
    {
        MimeEntity root = ParseSearchTree();
        EmailTextSearchOptions attachments = new EmailTextSearchOptions();
        attachments.SetIncludeTextAttachments(true);

        EmailTextSearchResult included = new EmailTextProcessor().SearchWithRootAndQueryAndOptions(root, "needle", attachments);
        Assert.That(included.GetMatchCount(), Is.EqualTo(3));
        Assert.That(included.GetMatch(2).GetPath().GetDisplayText(), Is.EqualTo("root/2"));

        attachments.SetCaseSensitive(true);
        EmailTextSearchResult sensitive = new EmailTextProcessor().SearchWithRootAndQueryAndOptions(root, "Needle", attachments);
        Assert.That(sensitive.GetMatchCount(), Is.EqualTo(1));
        Assert.That(sensitive.GetMatch(0).IsFromHtml(), Is.True);

        attachments.SetIncludeHtml(false);
        Assert.That(new EmailTextProcessor().SearchWithRootAndQueryAndOptions(root, "Needle", attachments).GetMatchCount(), Is.Zero);
    }

    [Test]
    public void MatchAndCharacterLimitsReturnBoundedPartialResults()
    {
        MimeEntity body = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("hit hit hit", "plain", "utf-8");
        EmailTextSearchOptions matches = new EmailTextSearchOptions();
        matches.SetMaximumMatches(1);

        EmailTextSearchResult matchLimited = new EmailTextProcessor().SearchWithRootAndQueryAndOptions(body, "hit", matches);
        Assert.That(matchLimited.GetMatchCount(), Is.EqualTo(1));
        Assert.That(matchLimited.WasTruncated(), Is.True);
        Assert.That(matchLimited.GetIssues().ContainsCode(EmailIssueCode.TextSearchLimitReached), Is.True);

        EmailTextSearchOptions characters = new EmailTextSearchOptions();
        characters.SetMaximumTotalCharacters(5);
        EmailTextSearchResult characterLimited = new EmailTextProcessor().SearchWithRootAndQueryAndOptions(body, "hit", characters);
        Assert.That(characterLimited.GetSearchedCharacterCount(), Is.EqualTo(5));
        Assert.That(characterLimited.GetMatchCount(), Is.EqualTo(1));
        Assert.That(characterLimited.WasTruncated(), Is.True);
    }

    [Test]
    public void SearchOptionsAndResultsAreDefensiveAndValidateInputs()
    {
        EmailTextSearchOptions options = new EmailTextSearchOptions();
        EmailTextDecodeOptions text = options.GetTextOptions();
        text.SetMaximumDecodedBytes(5);
        Assert.That(options.GetTextOptions().GetMaximumDecodedBytes(), Is.Not.EqualTo(5));

        EmailTextSearchResult result = new EmailTextProcessor().SearchWithRootAndQueryAndOptions(new EmailEntityFactory().CreateTextWithValue("find"), "find", options);
        result.GetMatches().Clear();
        result.GetIssues().Clear();
        Assert.That(result.GetMatchCount(), Is.EqualTo(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetMatch(1));
        Assert.Throws<ArgumentNullException>(() => new EmailTextProcessor().SearchWithRootAndQuery((MimeEntity)null!, "x"));
        Assert.Throws<ArgumentNullException>(() => new EmailTextProcessor().SearchWithRootAndQuery(new EmailEntityFactory().CreateTextWithValue("x"), null!));
        Assert.Throws<ArgumentException>(() => new EmailTextProcessor().SearchWithRootAndQuery(new EmailEntityFactory().CreateTextWithValue("x"), ""));
        options.SetMaximumQueryCharacters(2);
        AssertLimit("MaxTextSearchQueryCharacters", () => new EmailTextProcessor().SearchWithRootAndQueryAndOptions(new EmailEntityFactory().CreateTextWithValue("long"), "long", options));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumMatches(0));
        Assert.Throws<ArgumentNullException>(() => options.SetHtmlOptions(null!));
        Assert.Throws<ArgumentException>(() => new EmailTextProcessor().SearchWithMessageAndQuery(new EmailMessageOperations().CreateWith(), "x"));
    }

    static MimeEntity ParseSearchTree()
    {
        const string source = "Content-Type: multipart/mixed; boundary=m\r\n\r\n"
            + "--m\r\nContent-Type: multipart/alternative; boundary=a\r\n\r\n"
            + "--a\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nplain needle\r\n"
            + "--a\r\nContent-Type: text/html; charset=utf-8\r\n\r\n<p>Needle html</p><script>needle hidden</script>\r\n--a--\r\n"
            + "--m\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Disposition: attachment; filename=note.txt\r\n\r\nattachment needle\r\n"
            + "--m--\r\n";
        EmailEntityParseResult parsed = new EmailParser().ParseEntityWithInputUsingValueArray(Encoding.UTF8.GetBytes(source));
        Assert.That(parsed.IsSuccess(), Is.True);
        return parsed.GetEntity()!;
    }

    static void AssertLimit(string expectedName, TestDelegate action)
    {
        EmailLimitException exception = Assert.Throws<EmailLimitException>(action);
        Assert.That(exception.GetLimitName(), Is.EqualTo(expectedName));
    }
}
