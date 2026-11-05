using email;

namespace email.Tests;

public class EmailTextAnalysisTests
{
    [Test]
    public void SeparatesVisibleSignatureAndQuotedReplyRegions()
    {
        string text = "Hello team,\r\n\r\nHere is the update.\r\n-- \r\nAlice\r\n"
            + "On Tuesday, Bob wrote:\r\n> old line\r\n> second line";

        EmailTextAnalysisResult result = new EmailTextProcessor().AnalyzeWithValue(text);

        Assert.That(result.GetVisibleText(), Is.EqualTo("Hello team,\r\n\r\nHere is the update."));
        Assert.That(result.GetSignatureText(), Is.EqualTo("-- \r\nAlice"));
        Assert.That(result.GetQuotedText(), Is.EqualTo("On Tuesday, Bob wrote:\r\n> old line\r\n> second line"));
        Assert.That(result.GetSnippet(), Is.EqualTo("Hello team, Here is the update."));
        Assert.That(result.GetLineCount(), Is.EqualTo(8));
        Assert.That(result.GetSignatureLineCount(), Is.EqualTo(2));
        Assert.That(result.GetQuotedLineCount(), Is.EqualTo(3));
        Assert.That(result.HasSignature(), Is.True);
        Assert.That(result.HasQuotedReply(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TextSignatureDetected), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TextQuotedReplyDetected), Is.True);
    }

    [Test]
    public void DetectsIndividualQuoteLinesWithoutDiscardingLaterVisibleText()
    {
        EmailTextAnalysisResult result = new EmailTextProcessor().AnalyzeWithValue("new answer\n> older text\nnext point");

        Assert.That(result.GetVisibleText(), Is.EqualTo("new answer\nnext point"));
        Assert.That(result.GetQuotedText(), Is.EqualTo("> older text"));
        Assert.That(result.GetQuotedLineCount(), Is.EqualTo(1));
        Assert.That(result.GetQuotedReplyOffset(), Is.EqualTo(11));
    }

    [Test]
    public void SnippetPolicyCanIncludeRegionsAndTruncatesDeterministically()
    {
        EmailTextAnalysisOptions options = new EmailTextAnalysisOptions();
        options.SetIncludeSignatureInSnippet(true);
        options.SetIncludeQuotedTextInSnippet(true);
        options.SetMaximumSnippetCharacters(24);

        EmailTextAnalysisResult result = new EmailTextProcessor().AnalyzeWithValueAndOptions("Visible words\n-- \nSigner\n> quoted material", options);

        Assert.That(result.GetSnippet(), Is.EqualTo("Visible words -- Signer…"));
        Assert.That(result.WasSnippetTruncated(), Is.True);
        Assert.That(new EmailTextProcessor().CreateSnippetWithValue("  many\n\tspaces  "), Is.EqualTo("many spaces"));
    }

    [Test]
    public void AnalysisOptionsAreSnapshotsAndBoundsAreEnforced()
    {
        EmailTextAnalysisOptions options = new EmailTextAnalysisOptions();
        options.SetMaximumCharacters(4);
        options.SetMaximumLines(1);
        EmailTextAnalysisOptions clone = options.Clone();
        options.SetMaximumCharacters(100);

        AssertLimit("MaxTextAnalysisCharacters", () => new EmailTextProcessor().AnalyzeWithValueAndOptions("12345", clone));
        AssertLimit("MaxTextAnalysisLineCount", () => new EmailTextProcessor().AnalyzeWithValueAndOptions("a\nb", clone));
        Assert.That(clone.GetMaximumCharacters(), Is.EqualTo(4));
        Assert.Throws<ArgumentNullException>(() => new EmailTextProcessor().AnalyzeWithValue(null!));
        Assert.Throws<ArgumentNullException>(() => new EmailTextProcessor().AnalyzeWithValueAndOptions("", null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumSnippetCharacters(0));
    }

    static void AssertLimit(string expectedName, TestDelegate action)
    {
        EmailLimitException exception = Assert.Throws<EmailLimitException>(action);
        Assert.That(exception.GetLimitName(), Is.EqualTo(expectedName));
    }
}
