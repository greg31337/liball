using System.Text;
using email;

namespace email.Tests;

public class EmailPartialMessageAssemblyTests
{
    [Test]
    public void OutOfOrderFragmentsAssembleAndMergeHeadersByRfc2046Rules()
    {
        const string first = "From: inner@example.test\r\nX-Inner-Drop: yes\r\nSubject: Inner subject\r\nMessage-ID: <inner@example.test>\r\nMIME-Version: 1.0\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Transfer-Encoding: 7bit\r\n\r\nfirst line\r\n";
        const string second = "second line\r\n";
        EmailMessage fragmentOne = Fragment("set-a", 1, 0, first, "From: outer@example.test\r\nTo: recipient@example.test\r\nDate: Tue, 10 Jun 2025 12:00:00 +0000\r\nX-Outer: kept\r\nSubject: Wrapper one\r\nMessage-ID: <wrapper-one@example.test>\r\nMIME-Version: 1.0\r\n");
        EmailMessage fragmentTwo = Fragment("set-a", 2, 2, second, "From: ignored@example.test\r\nX-Later: discarded\r\nSubject: Wrapper two\r\n");

        EmailPartialMessageAssemblyResult result = new EmailPartialMessageAssembler().AssembleWithFragments(new List<EmailMessage> { fragmentTwo, fragmentOne });

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsSequenceComplete(), Is.True);
        Assert.That(result.GetFragmentAt(0).GetFragmentNumber(), Is.EqualTo(1));
        Assert.That(result.GetFragmentAt(1).GetFragmentNumber(), Is.EqualTo(2));
        Assert.That(Text(result.GetInnerBytes()!), Is.EqualTo(first + second));
        Assert.That(result.HasInnerMessage(), Is.True);
        Assert.That(result.HasMessage(), Is.True);
        Assert.That(result.HasReassembledBytes(), Is.True);

        EmailMessage message = result.GetMessage()!;
        Assert.That(message!.GetHeaders()!.GetFirst("From")!.GetDecodedValue(), Is.EqualTo("outer@example.test"));
        Assert.That(message.GetHeaders()!.Contains("To"), Is.True);
        Assert.That(message.GetHeaders()!.Contains("Date"), Is.True);
        Assert.That(message.GetHeaders()!.GetFirst("X-Outer")!.GetDecodedValue(), Is.EqualTo("kept"));
        Assert.That(message.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("Inner subject"));
        Assert.That(message.GetHeaders()!.GetFirst("Message-ID")!.GetDecodedValue(), Is.EqualTo("<inner@example.test>"));
        Assert.That(message.GetHeaders()!.GetFirst("Content-Type")!.GetDecodedValue(), Does.Contain("text/plain"));
        Assert.That(message.GetHeaders()!.Contains("X-Inner-Drop"), Is.False);
        Assert.That(message.GetHeaders()!.Contains("X-Later"), Is.False);
        Assert.That(Text(message.GetBody()!.GetContent()!.Materialize(100)), Is.EqualTo("first line\r\nsecond line\r\n"));

        EmailParseResult reparsed = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(result.GetReassembledBytes()!, new EmailParseOptionsOperations().CreateStrict());
        Assert.That(reparsed.IsSuccess(), Is.True);
        Assert.That(reparsed.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("Inner subject"));
    }

    [Test]
    public void MissingAndDuplicateNumbersPreventAmbiguousAssembly()
    {
        EmailPartialMessageAssemblyOptions options = WithoutReparse();
        EmailPartialMessageAssemblyResult missing = new EmailPartialMessageAssembler().AssembleWithFragmentsAndOptions(new List<EmailMessage>
        {
            Fragment("missing", 3, 3, "three", ""),
            Fragment("missing", 1, 0, "one\r\n", "")
        }, options);
        EmailPartialMessageAssemblyResult duplicate = new EmailPartialMessageAssembler().AssembleWithFragmentsAndOptions(new List<EmailMessage>
        {
            Fragment("duplicate", 1, 0, "first\r\n", ""),
            Fragment("duplicate", 1, 0, "other\r\n", ""),
            Fragment("duplicate", 2, 2, "last", "")
        }, options);

        Assert.That(missing.IsSuccess(), Is.False);
        Assert.That(missing.IsAssembled(), Is.False);
        Assert.That(missing.GetMissingFragmentNumbers(), Is.EqualTo(new List<long> { 2 }));
        Assert.That(missing.GetIssues().ContainsCode(EmailIssueCode.PartialMessageMissingFragment), Is.True);
        Assert.That(duplicate.IsSuccess(), Is.False);
        Assert.That(duplicate.IsAssembled(), Is.False);
        Assert.That(duplicate.GetDuplicateFragmentNumbers(), Is.EqualTo(new List<long> { 1 }));
        Assert.That(duplicate.GetIssues().ContainsCode(EmailIssueCode.PartialMessageDuplicateFragment), Is.True);
    }

    [Test]
    public void ConflictingTotalsAndInvalidFragmentBoundariesAreRejected()
    {
        EmailPartialMessageAssemblyOptions options = WithoutReparse();
        EmailPartialMessageAssemblyResult totals = new EmailPartialMessageAssembler().AssembleWithFragmentsAndOptions(new List<EmailMessage>
        {
            Fragment("totals", 1, 2, "one\r\n", ""),
            Fragment("totals", 2, 3, "two", "")
        }, options);
        EmailPartialMessageAssemblyResult boundary = new EmailPartialMessageAssembler().AssembleWithFragmentsAndOptions(new List<EmailMessage>
        {
            Fragment("boundary", 1, 0, "not-at-a-line-boundary", ""),
            Fragment("boundary", 2, 2, "last", "")
        }, options);

        Assert.That(totals.IsAssembled(), Is.False);
        Assert.That(totals.GetIssues().ContainsCode(EmailIssueCode.PartialMessageTotalMismatch), Is.True);
        Assert.That(boundary.IsAssembled(), Is.False);
        Assert.That(boundary.GetIssues().ContainsCode(EmailIssueCode.PartialMessageFragmentBoundaryInvalid), Is.True);
    }

    [Test]
    public void ReparseCanBeDisabledWhilePreservingExactOrderedBytes()
    {
        EmailPartialMessageAssemblyOptions options = WithoutReparse();
        EmailPartialMessageAssemblyResult result = new EmailPartialMessageAssembler().AssembleWithFragmentsAndOptions(new List<EmailMessage>
        {
            Fragment("raw", 2, 2, "body", ""),
            Fragment("raw", 1, 0, "Subject: raw\r\n\r\n", "")
        }, options);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.WasReparseRequested(), Is.False);
        Assert.That(Text(result.GetInnerBytes()!), Is.EqualTo("Subject: raw\r\n\r\nbody"));
        Assert.That(result.HasInnerMessage(), Is.False);
        Assert.That(result.HasMessage(), Is.False);
        Assert.That(result.HasReassembledBytes(), Is.False);
    }

    [Test]
    public void StrictReparseFailureKeepsTheSuccessfullyAssembledBytes()
    {
        EmailPartialMessageAssemblyOptions options = new EmailPartialMessageAssemblyOptions();
        options.SetParseOptions(new EmailParseOptionsOperations().CreateStrict());
        EmailPartialMessageAssemblyResult result = new EmailPartialMessageAssembler().AssembleWithFragmentsAndOptions(new List<EmailMessage>
        {
            Fragment("invalid-inner", 1, 1, "Broken Header\r\n\r\nbody", "")
        }, options);

        Assert.That(result.IsSequenceComplete(), Is.True);
        Assert.That(result.IsAssembled(), Is.True);
        Assert.That(Text(result.GetInnerBytes()!), Is.EqualTo("Broken Header\r\n\r\nbody"));
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.HasMessage(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.PartialMessageReparseFailed), Is.True);
    }

    [Test]
    public void CollectorSeparatesIdsAndPreservesFirstSeenGroupOrder()
    {
        EmailPartialMessageCollector collector = new EmailPartialMessageCollectorOperations().CreateWithOptions(WithoutReparse());
        collector.Add(Fragment("second-id", 1, 1, "second", ""));
        collector.Add(Fragment("first-id", 1, 1, "first", ""));

        Assert.That(collector.GetGroupCount(), Is.EqualTo(2));
        Assert.That(collector.GetGroupIds(), Is.EqualTo(new List<string> { "second-id", "first-id" }));
        Assert.That(collector.GetFragmentCount("second-id"), Is.EqualTo(1));
        Assert.That(collector.AssembleAll()[0].GetFragmentId(), Is.EqualTo("second-id"));
        Assert.That(Text(collector.Assemble("first-id").GetInnerBytes()!), Is.EqualTo("first"));
    }

    [Test]
    public void FragmentCountAndByteLimitsAreEnforced()
    {
        EmailPartialMessageAssemblyOptions countOptions = WithoutReparse();
        countOptions.SetMaximumFragmentCount(1);
        EmailPartialMessageCollector collector = new EmailPartialMessageCollectorOperations().CreateWithOptions(countOptions);
        collector.Add(Fragment("count", 1, 1, "one", ""));

        EmailLimitException countError = Assert.Throws<EmailLimitException>(() => collector.Add(Fragment("other", 1, 1, "two", "")));

        EmailPartialMessageAssemblyOptions fragmentOptions = WithoutReparse();
        fragmentOptions.SetMaximumFragmentBytes(3);
        EmailLimitException fragmentError = Assert.Throws<EmailLimitException>(() => new EmailPartialMessageAssembler().AssembleWithFragmentsAndOptions(new List<EmailMessage>
        {
            Fragment("fragment-size", 1, 1, "four", "")
        }, fragmentOptions));

        EmailPartialMessageAssemblyOptions totalOptions = WithoutReparse();
        totalOptions.SetMaximumReassembledBytes(5);
        EmailLimitException totalError = Assert.Throws<EmailLimitException>(() => new EmailPartialMessageAssembler().AssembleWithFragmentsAndOptions(new List<EmailMessage>
        {
            Fragment("total-size", 1, 0, "one\r\n", ""),
            Fragment("total-size", 2, 2, "two", "")
        }, totalOptions));

        Assert.That(countError.GetLimitName(), Is.EqualTo("MaxPartialFragmentCount"));
        Assert.That(fragmentError.GetLimitName(), Is.EqualTo("MaxPartialFragmentBytes"));
        Assert.That(totalError.GetLimitName(), Is.EqualTo("MaxReassembledBytes"));
    }

    [Test]
    public void OptionsAndResultCollectionsUseDefensiveCopies()
    {
        EmailPartialMessageAssemblyOptions options = WithoutReparse();
        EmailPartialMessageCollector collector = new EmailPartialMessageCollectorOperations().CreateWithOptions(options);
        options.SetMaximumFragmentCount(1);
        collector.Add(Fragment("copy", 1, 1, "body", ""));
        List<string> ids = collector.GetGroupIds();
        ids.Clear();
        EmailPartialMessageAssemblyResult result = collector.Assemble("copy");
        byte[] bytes = result.GetInnerBytes()!;
        bytes![0] = (byte)'X';
        List<EmailPartialMessageFragment> fragments = result.GetFragments();
        fragments.Clear();

        Assert.That(collector.GetOptions().GetMaximumFragmentCount(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxPartialFragmentCount));
        Assert.That(collector.GetGroupCount(), Is.EqualTo(1));
        Assert.That(Text(result.GetInnerBytes()!), Is.EqualTo("body"));
        Assert.That(result.GetFragmentCount(), Is.EqualTo(1));
    }

    static EmailMessage Fragment(string id, long number, long total, string body, string outerHeaders)
    {
        string totalParameter = total > 0 ? "; total=" + total : "";
        string source = outerHeaders + "Content-Type: message/partial; id=\"" + id + "\"; number=" + number + totalParameter + "\r\n\r\n" + body;
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.HasMessage(), Is.True);
        Assert.That(result.GetMessage()!.GetBody()!.GetKind(), Is.EqualTo(MimeEntityKind.PartialMessage));
        return result.GetMessage()!;
    }

    static EmailPartialMessageAssemblyOptions WithoutReparse()
    {
        EmailPartialMessageAssemblyOptions options = new EmailPartialMessageAssemblyOptions();
        options.SetReparse(false);
        return options;
    }

    static byte[] Bytes(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.UTF8.GetString(value);
    }
}
