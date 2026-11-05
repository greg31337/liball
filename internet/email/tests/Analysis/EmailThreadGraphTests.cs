using System.Text;
using email;

namespace email.Tests;

public class EmailThreadGraphTests
{
    [Test]
    public void GraphResolvesForwardParentsAndPreservesRootAndChildInputOrder()
    {
        EmailMessage earlyChild = ParseMessage("Message-ID: <early@example.test>\r\nReferences: <root-a@example.test>\r\n\r\nbody");
        EmailMessage rootB = ParseMessage("Message-ID: <root-b@example.test>\r\n\r\nbody");
        EmailMessage rootA = ParseMessage("Message-ID: <root-a@example.test>\r\n\r\nbody");
        EmailMessage lateChild = ParseMessage("Message-ID: <late@example.test>\r\nIn-Reply-To: <root-a@example.test>\r\n\r\nbody");

        EmailThreadGraph graph = new EmailThreadAnalyzer().BuildGraphWithMessages(new List<EmailMessage> { earlyChild, rootB, rootA, lateChild });

        Assert.That(graph.GetNodeCount(), Is.EqualTo(4));
        Assert.That(graph.GetNodeAt(0).GetMessage(), Is.SameAs(earlyChild));
        Assert.That(graph.GetNodeAt(0).GetInputIndex(), Is.EqualTo(0));
        Assert.That(graph.GetRootCount(), Is.EqualTo(2));
        Assert.That(graph.GetRootAt(0).GetMessage(), Is.SameAs(rootB));
        Assert.That(graph.GetRootAt(1).GetMessage(), Is.SameAs(rootA));
        Assert.That(graph.GetNodeAt(0).GetParent(), Is.SameAs(graph.GetNodeAt(2)));
        Assert.That(graph.GetNodeAt(3).GetParent(), Is.SameAs(graph.GetNodeAt(2)));
        Assert.That(graph.GetNodeAt(2).GetChildCount(), Is.EqualTo(2));
        Assert.That(graph.GetNodeAt(2).GetChildAt(0), Is.SameAs(graph.GetNodeAt(0)));
        Assert.That(graph.GetNodeAt(2).GetChildAt(1), Is.SameAs(graph.GetNodeAt(3)));
        Assert.That(graph.GetIssues().GetCount(), Is.EqualTo(0));
    }

    [Test]
    public void DuplicateIdentifiersAreDiagnosedAndFirstOwnerWinsLookups()
    {
        EmailMessage first = ParseMessage("Message-ID: <same@EXAMPLE.TEST>\r\n\r\nfirst");
        EmailMessage duplicate = ParseMessage("Message-ID: <same@example.test>\r\n\r\nduplicate");
        EmailMessage child = ParseMessage("Message-ID: <child@example.test>\r\nReferences: <same@example.test>\r\n\r\nchild");

        EmailThreadGraph graph = new EmailThreadAnalyzer().BuildGraphWithMessages(new List<EmailMessage> { first, duplicate, child });

        Assert.That(graph.GetRootCount(), Is.EqualTo(2));
        Assert.That(graph.GetNodeAt(2).GetParent(), Is.SameAs(graph.GetNodeAt(0)));
        Assert.That(graph.GetIssues().ContainsCode(EmailIssueCode.ThreadDuplicateMessageId), Is.True);
        Assert.That(graph.GetNodeAt(0).GetIssues().ContainsCode(EmailIssueCode.ThreadDuplicateMessageId), Is.True);
        Assert.That(graph.GetNodeAt(1).GetIssues().ContainsCode(EmailIssueCode.ThreadDuplicateMessageId), Is.True);
    }

    [Test]
    public void MissingParentsRemainRootsWithFieldSpecificDiagnostics()
    {
        EmailMessage reference = ParseMessage("Message-ID: <reference@example.test>\r\nReferences: <missing@example.test>\r\n\r\nbody");
        EmailMessage reply = ParseMessage("Message-ID: <reply@example.test>\r\nIn-Reply-To: <also-missing@example.test>\r\n\r\nbody");

        EmailThreadGraph graph = new EmailThreadAnalyzer().BuildGraphWithMessages(new List<EmailMessage> { reference, reply });

        Assert.That(graph.GetRootCount(), Is.EqualTo(2));
        Assert.That(graph.GetIssues().GetCount(), Is.EqualTo(2));
        Assert.That(graph.GetNodeAt(0).GetIssues().GetAt(0).GetFieldName(), Is.EqualTo("References"));
        Assert.That(graph.GetNodeAt(1).GetIssues().GetAt(0).GetFieldName(), Is.EqualTo("In-Reply-To"));
        Assert.That(graph.GetIssues().ContainsCode(EmailIssueCode.ThreadParentMissing), Is.True);
    }

    [Test]
    public void CycleClosingEdgeIsDroppedAndEveryParticipantIsDiagnosed()
    {
        EmailMessage first = ParseMessage("Message-ID: <a@example.test>\r\nReferences: <b@example.test>\r\n\r\na");
        EmailMessage second = ParseMessage("Message-ID: <b@example.test>\r\nReferences: <c@example.test>\r\n\r\nb");
        EmailMessage third = ParseMessage("Message-ID: <c@example.test>\r\nReferences: <a@example.test>\r\n\r\nc");

        EmailThreadGraph graph = new EmailThreadAnalyzer().BuildGraphWithMessages(new List<EmailMessage> { first, second, third });

        Assert.That(graph.GetRootCount(), Is.EqualTo(1));
        Assert.That(graph.GetRootAt(0), Is.SameAs(graph.GetNodeAt(2)));
        Assert.That(graph.GetNodeAt(2).GetChildAt(0), Is.SameAs(graph.GetNodeAt(1)));
        Assert.That(graph.GetNodeAt(1).GetChildAt(0), Is.SameAs(graph.GetNodeAt(0)));
        Assert.That(graph.GetIssues().ContainsCode(EmailIssueCode.ThreadCycleDetected), Is.True);
        Assert.That(graph.GetNodeAt(0).GetIssues().ContainsCode(EmailIssueCode.ThreadCycleDetected), Is.True);
        Assert.That(graph.GetNodeAt(1).GetIssues().ContainsCode(EmailIssueCode.ThreadCycleDetected), Is.True);
        Assert.That(graph.GetNodeAt(2).GetIssues().ContainsCode(EmailIssueCode.ThreadCycleDetected), Is.True);
    }

    [Test]
    public void SubjectFallbackCreatesStableFlatGroupsAndIsAlwaysDiagnosed()
    {
        EmailMessage first = ParseMessage("Subject: Re: Topic\r\n\r\nfirst");
        EmailMessage second = ParseMessage("Subject: Fwd: Topic\r\n\r\nsecond");
        EmailMessage other = ParseMessage("Subject: Other\r\n\r\nother");
        List<EmailMessage> messages = new List<EmailMessage> { first, second, other };

        EmailThreadGraph graph = new EmailThreadAnalyzer().BuildGraphWithMessages(messages);

        Assert.That(graph.GetRootCount(), Is.EqualTo(2));
        Assert.That(graph.GetRootAt(0), Is.SameAs(graph.GetNodeAt(0)));
        Assert.That(graph.GetRootAt(1), Is.SameAs(graph.GetNodeAt(2)));
        Assert.That(graph.GetNodeAt(1).GetParent(), Is.SameAs(graph.GetNodeAt(0)));
        Assert.That(graph.GetNodeAt(0).GetChildCount(), Is.EqualTo(1));
        Assert.That(graph.GetIssues().GetCount(), Is.EqualTo(3));
        Assert.That(graph.GetIssues().ContainsCode(EmailIssueCode.ThreadSubjectFallbackUsed), Is.True);

        EmailThreadOptions disabled = new EmailThreadOptions();
        disabled.SetEnableSubjectFallback(false);
        EmailThreadGraph withoutFallback = new EmailThreadAnalyzer().BuildGraphWithMessagesAndOptions(messages, disabled);
        Assert.That(withoutFallback.GetRootCount(), Is.EqualTo(3));
        Assert.That(withoutFallback.GetIssues().ContainsCode(EmailIssueCode.ThreadSubjectFallbackUsed), Is.False);
    }

    [Test]
    public void GraphResultsAndNodeCollectionsAreDefensive()
    {
        EmailThreadGraph graph = new EmailThreadAnalyzer().BuildGraphWithMessages(new List<EmailMessage>
        {
            ParseMessage("Message-ID: <root@example.test>\r\n\r\nroot"),
            ParseMessage("Message-ID: <child@example.test>\r\nReferences: <root@example.test>\r\n\r\nchild")
        });
        List<EmailThreadNode> nodes = graph.GetNodes();
        List<EmailThreadNode> roots = graph.GetRoots();
        List<EmailThreadNode> children = graph.GetRootAt(0).GetChildren();
        EmailIssueList issues = graph.GetIssues();
        nodes.Clear();
        roots.Clear();
        children.Clear();
        issues.Clear();

        Assert.That(graph.GetNodeCount(), Is.EqualTo(2));
        Assert.That(graph.GetRootCount(), Is.EqualTo(1));
        Assert.That(graph.GetRootAt(0).GetChildCount(), Is.EqualTo(1));
        Assert.That(graph.GetNodeAt(0).IsRoot(), Is.True);
        Assert.That(graph.GetNodeAt(1).HasParent(), Is.True);
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.GetNodeAt(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.GetRootAt(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.GetRootAt(0).GetChildAt(1));
    }

    [Test]
    public void GraphLimitsOptionsAndInputsAreValidated()
    {
        EmailMessage first = ParseMessage("Message-ID: <first@example.test>\r\n\r\nfirst");
        EmailMessage second = ParseMessage("Message-ID: <second@example.test>\r\n\r\nsecond");
        List<EmailMessage> messages = new List<EmailMessage> { first, second };

        EmailThreadOptions options = new EmailThreadOptions();
        options.SetMaximumGraphMessageCount(1);
        AssertLimit("MaxThreadGraphMessages", () => new EmailThreadAnalyzer().BuildGraphWithMessagesAndOptions(messages, options));

        options = new EmailThreadOptions();
        options.SetMaximumGraphIdentifierCount(1);
        AssertLimit("MaxThreadGraphIdentifiers", () => new EmailThreadAnalyzer().BuildGraphWithMessagesAndOptions(messages, options));

        options = new EmailThreadOptions();
        options.SetMaximumGraphMessageCount(5);
        options.SetMaximumGraphIdentifierCount(6);
        EmailThreadOptions clone = options.Clone();
        options.SetMaximumGraphMessageCount(7);
        options.SetMaximumGraphIdentifierCount(8);
        Assert.That(clone.GetMaximumGraphMessageCount(), Is.EqualTo(5));
        Assert.That(clone.GetMaximumGraphIdentifierCount(), Is.EqualTo(6));

        Assert.That(new EmailThreadAnalyzer().BuildGraphWithMessages(new List<EmailMessage>()).GetNodeCount(), Is.EqualTo(0));
        Assert.Throws<ArgumentNullException>(() => new EmailThreadAnalyzer().BuildGraphWithMessages(null!));
        Assert.Throws<ArgumentNullException>(() => new EmailThreadAnalyzer().BuildGraphWithMessagesAndOptions(messages, null!));
        Assert.Throws<ArgumentException>(() => new EmailThreadAnalyzer().BuildGraphWithMessages(new List<EmailMessage> { null! }));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumGraphMessageCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumGraphIdentifierCount(0));
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.UTF8.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetMessage()!;
    }

    static void AssertLimit(string expectedName, TestDelegate action)
    {
        EmailLimitException exception = Assert.Throws<EmailLimitException>(action);
        Assert.That(exception.GetLimitName(), Is.EqualTo(expectedName));
    }
}
