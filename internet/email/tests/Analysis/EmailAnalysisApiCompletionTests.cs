using System.Text;
using email;

namespace email.Tests;

public class EmailAnalysisApiCompletionTests
{
    [Test]
    public void QueryConveniencesPreserveFieldDateAndBooleanSemantics()
    {
        EmailMessage message = ParseMessage(
            "Date: Fri, 21 Nov 1997 10:00:00 -0600\r\n"
            + "From: sender@example.test\r\n"
            + "X-State: ready\r\n\r\n"
            + "body");
        EmailQueryOperations operations = new EmailQueryOperations();
        EmailQuery raw = operations.RawFieldEquals("X-State", "ready");
        Assert.That(raw.Matches(message), Is.True);
        Assert.That(raw.HasFieldName(), Is.True);
        Assert.That(raw.GetFieldName(), Is.EqualTo("X-State"));
        Assert.That(raw.HasText(), Is.True);
        Assert.That(raw.GetText(), Is.EqualTo("ready"));

        EmailDateTime same = Date("Fri, 21 Nov 1997 16:00:00 +0000");
        Assert.That(operations.DateBeforeWithFieldNameAndValue("Date", Date("Fri, 21 Nov 1997 16:00:01 +0000")).Matches(message), Is.True);
        Assert.That(operations.DateOnOrAfterWithFieldNameAndValue("Date", same).Matches(message), Is.True);
        Assert.That(operations.DateEqualsWithFieldNameAndValue("Date", same).Matches(message), Is.True);

        EmailQueryOptions options = new EmailQueryOptions();
        List<EmailQuery> both = new List<EmailQuery> { operations.HasField("From"), raw };
        List<EmailQuery> either = new List<EmailQuery> { operations.HasField("Missing"), raw };
        Assert.That(operations.AllWithQueriesAndOptions(both, options).Matches(message), Is.True);
        Assert.That(operations.OrWithQueriesAndOptions(either, options).Matches(message), Is.True);
        Assert.That(operations.AnyWithQueriesAndOptions(either, options).Matches(message), Is.True);
        Assert.That(operations.AllWithQueriesAndOptions(either, options).Matches(message), Is.False);
    }

    [Test]
    public void SummaryAndClassificationAccessorsExposeMeasuredWorkflowEvidence()
    {
        EmailMessage message = ParseMessage(
            "Auto-Submitted: auto-generated\r\n"
            + "Content-Type: multipart/related; boundary=r\r\n\r\n"
            + "--r\r\nContent-Type: text/html; charset=utf-8\r\n\r\n<img src='cid:logo@example.test'>\r\n"
            + "--r\r\nContent-Type: image/png\r\nContent-ID: <logo@example.test>\r\n\r\npng\r\n"
            + "--r--\r\n");
        message.AddIssue(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Fatal, EmailIssueCode.ValidationFailed, "fatal workflow issue"));

        EmailMessageSummary summary = new EmailMessageAnalyzer().SummarizeWithMessageAndOptions(message, new EmailMessageAnalysisOptions());
        Assert.That(summary.GetInlineResourceCount(), Is.EqualTo(1));
        Assert.That(summary.GetFatalIssueCount(), Is.EqualTo(1));

        EmailMessageClassification classification = new EmailMessageClassifier().ClassifyWithMessage(message);
        EmailMessageClassificationResult automated = classification.GetResult(EmailMessageClassificationKind.Automated);
        EmailMessageClassificationReason reason = automated.GetReasonAt(0);
        Assert.That(automated, Is.SameAs(classification.GetAutomated()));
        Assert.That(reason.GetDescription(), Is.Not.Empty);
        Assert.That(reason.HasFieldName(), Is.True);
        Assert.That(reason.GetFieldName(), Is.EqualTo("Auto-Submitted"));
    }

    [Test]
    public void ThreadIdentityNodeAndGraphStatusAccessorsReflectAResolvedParent()
    {
        EmailMessage parent = ParseMessage("Message-ID: <parent@example.test>\r\nSubject: topic\r\n\r\nparent");
        EmailMessage child = ParseMessage(
            "Message-ID: <child@example.test>\r\n"
            + "In-Reply-To: <parent@example.test>\r\n"
            + "References: <parent@example.test>\r\n"
            + "Subject: Re: topic\r\n\r\nchild");
        EmailThreadAnalyzer analyzer = new EmailThreadAnalyzer();
        EmailThreadIdentity identity = analyzer.ExtractWithMessage(child);

        Assert.That(identity.GetMessageIdentifier()!.GetIdLeft(), Is.EqualTo("child"));
        Assert.That(identity.GetInReplyToAt(0).GetIdLeft(), Is.EqualTo("parent"));
        Assert.That(identity.GetInReplyTo().Single().GetIdLeft(), Is.EqualTo("parent"));
        Assert.That(identity.IsSuccess(), Is.True);

        EmailThreadGraph graph = analyzer.BuildGraphWithMessages(new List<EmailMessage> { child, parent });
        EmailThreadNode childNode = graph.GetNodes().Single(node => node.GetMessage() == child);
        Assert.That(childNode.GetParent()!.GetMessage(), Is.SameAs(parent));
        Assert.That(childNode.IsSuccess(), Is.True);
        Assert.That(graph.IsSuccess(), Is.True);
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.UTF8.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.HasMessage(), Is.True);
        return result.GetMessage()!;
    }

    static EmailDateTime Date(string source)
    {
        EmailDateParseResult result = new EmailDateParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(source), EmailDateParseMode.Strict);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetValue()!;
    }
}
