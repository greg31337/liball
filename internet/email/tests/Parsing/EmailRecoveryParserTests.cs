using System.Text;
using email;

namespace email.Tests;

public class EmailRecoveryParserTests
{
    [Test]
    public void TolerantModeRecoversAMissingSeparatorAtTheFirstBodyLikeLine()
    {
        const string source = "Subject: recovered\r\nThis is the body\r\nsecond line";
        int bodyStart = source.IndexOf("This is", StringComparison.Ordinal);

        EmailParseResult tolerant = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving());
        EmailParseResult strict = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreateStrict());

        Assert.That(tolerant.IsComplete(), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.HeaderBlockIncomplete), Is.True);
        Assert.That(tolerant.GetIssues().GetCountBySeverity(EmailIssueSeverity.Warning), Is.EqualTo(1));
        Assert.That(tolerant.GetMessage()!.GetHeaders()!.GetCount(), Is.EqualTo(1));
        Assert.That(Text(tolerant.GetMessage()!.GetBody()!.GetRawContent()!.Materialize(100)), Is.EqualTo("This is the body\r\nsecond line"));
        Assert.That(tolerant.GetMessage()!.GetSourceInfo()!.HasSeparatorRange(), Is.False);
        Assert.That(tolerant.GetMessage()!.GetSourceInfo()!.GetHeaderBlockRange()!.GetLength(), Is.EqualTo(bodyStart));
        Assert.That(tolerant.GetMessage()!.GetSourceInfo()!.GetBodyRange()!.GetStartOffset(), Is.EqualTo(bodyStart));

        Assert.That(strict.IsComplete(), Is.False);
        Assert.That(strict.HasMessage(), Is.False);
        Assert.That(strict.GetIssues().ContainsCode(EmailIssueCode.HeaderBlockIncomplete), Is.True);
    }

    [Test]
    public void OrphanFoldsAndBareNewlinesAreRetainedWithPolicyDrivenSeverity()
    {
        byte[] source = Bytes(" orphan\nSubject: okay\n\nbody");
        EmailParseResult tolerant = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, new EmailParseOptionsOperations().CreatePreserving());
        EmailParseResult strict = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, new EmailParseOptionsOperations().CreateStrict());
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(tolerant.GetMessage()!.GetHeaders()!.GetAt(0).GetName(), Is.Empty);
        Assert.That(Text(tolerant.GetMessage()!.GetHeaders()!.GetAt(0).GetRawValue()), Is.EqualTo(" orphan"));
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.HeaderContinuationWithoutField), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.HeaderNonCanonicalLineEnding), Is.True);
        Assert.That(tolerant.GetIssues().GetCountBySeverity(EmailIssueSeverity.Error), Is.Zero);

        Assert.That(strict.IsComplete(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(strict.GetMessage()!.GetHeaders()!.GetAt(0).GetName(), Is.Empty);
        Assert.That(strict.GetIssues().GetCountBySeverity(EmailIssueSeverity.Error), Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void CompatibilityFlagsCanRejectBareLinesAndOrphansInTolerantMode()
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailCompatibilityOptions compatibility = options.GetCompatibilityOptions();
        compatibility.SetAllowBareLineEndings(false);
        compatibility.SetAllowOrphanHeaderContinuations(false);
        options.SetCompatibilityOptions(compatibility);

        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(" orphan\n\nbody"), options);

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetIssues().GetCountBySeverity(EmailIssueSeverity.Error), Is.EqualTo(3));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderContinuationWithoutField), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderNonCanonicalLineEnding), Is.True);
    }

    [Test]
    public void OverlongHeaderLinesAreRetainedBelowTheConfiguredHardLimit()
    {
        string source = "Subject: " + new string('x', 990) + "\r\n\r\nbody";
        EmailParseResult tolerant = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving());
        EmailParseResult strict = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreateStrict());
        EmailIssue issue = tolerant.GetIssues().GetAll().Single(value => value.GetCode() == EmailIssueCode.HeaderLineTooLong);

        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(tolerant.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetRawValue().Length, Is.EqualTo(991));
        Assert.That(issue.GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
        Assert.That(issue.GetSourcePosition()!.GetByteOffset(), Is.EqualTo(998));
        Assert.That(strict.IsComplete(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(strict.GetIssues().ContainsCode(EmailIssueCode.HeaderLineTooLong), Is.True);
    }

    [Test]
    public void DuplicateSingletonsArePreservedAndTheFirstRemainsEffective()
    {
        byte[] source = Bytes("Subject: first\r\nSubject: second\r\n\r\nbody");
        EmailParseResult tolerant = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, new EmailParseOptionsOperations().CreatePreserving());
        EmailParseResult strict = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, new EmailParseOptionsOperations().CreateStrict());
        EmailParseOptions rejectingOptions = new EmailParseOptionsOperations().CreatePreserving();
        EmailCompatibilityOptions rejectingCompatibility = rejectingOptions.GetCompatibilityOptions();
        rejectingCompatibility.SetAllowDuplicateSingletonHeaders(false);
        rejectingOptions.SetCompatibilityOptions(rejectingCompatibility);
        EmailParseResult rejecting = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, rejectingOptions);

        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(tolerant.GetMessage()!.GetHeaders()!.GetCountNamed("Subject"), Is.EqualTo(2));
        Assert.That(tolerant.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("first"));
        Assert.That(tolerant.GetIssues().GetCountBySeverity(EmailIssueSeverity.Warning), Is.EqualTo(1));
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.HeaderDuplicateSingleton), Is.True);

        Assert.That(strict.IsComplete(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(strict.GetMessage()!.GetHeaders()!.GetCountNamed("Subject"), Is.EqualTo(2));
        Assert.That(strict.GetIssues().GetCountBySeverity(EmailIssueSeverity.Error), Is.EqualTo(1));
        Assert.That(rejecting.IsComplete(), Is.True);
        Assert.That(rejecting.IsSuccess(), Is.False);
        Assert.That(rejecting.GetIssues().GetCountBySeverity(EmailIssueSeverity.Error), Is.EqualTo(1));
    }

    [Test]
    public void TruncatedTransferEncodingRecoversAUsefulPrefixAccordingToPolicy()
    {
        byte[] source = Bytes("Content-Transfer-Encoding: base64\r\n\r\nQQ");
        EmailParseOptions tolerantOptions = DecodeOptions(new EmailParseOptionsOperations().CreatePreserving());
        EmailParseOptions strictOptions = DecodeOptions(new EmailParseOptionsOperations().CreateStrict());
        EmailParseOptions rejectingOptions = new EmailParseOptionsOperations().CreatePreserving();
        EmailCompatibilityOptions rejectingCompatibility = rejectingOptions.GetCompatibilityOptions();
        rejectingCompatibility.SetAllowTruncatedInput(false);
        rejectingOptions.SetCompatibilityOptions(rejectingCompatibility);
        rejectingOptions = DecodeOptions(rejectingOptions);
        EmailParseResult tolerant = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, tolerantOptions);
        EmailParseResult strict = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, strictOptions);
        EmailParseResult rejecting = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, rejectingOptions);

        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(Text(tolerant.GetMessage()!.GetBody()!.GetDecodedContent()!.Materialize(10)), Is.EqualTo("A"));
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.Base64TruncatedQuantum), Is.True);
        Assert.That(tolerant.GetIssues().GetCountBySeverity(EmailIssueSeverity.Warning), Is.EqualTo(1));

        Assert.That(strict.IsComplete(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(Text(strict.GetMessage()!.GetBody()!.GetDecodedContent()!.Materialize(10)), Is.EqualTo("A"));
        Assert.That(strict.GetIssues().GetCountBySeverity(EmailIssueSeverity.Error), Is.EqualTo(1));
        Assert.That(rejecting.IsComplete(), Is.True);
        Assert.That(rejecting.IsSuccess(), Is.False);
        Assert.That(rejecting.GetIssues().GetCountBySeverity(EmailIssueSeverity.Error), Is.EqualTo(1));
    }

    [Test]
    public void BoundaryAfterClosingIsRetainedAsEpilogueAndReported()
    {
        const string source = "Content-Type: multipart/mixed; boundary=x\r\n\r\n--x\r\n\r\none\r\n--x--\r\n--x\r\nextra";
        EmailEntityParseResult tolerant = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving());
        EmailEntityParseResult strict = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreateStrict());

        Assert.That(tolerant.IsComplete(), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(tolerant.GetEntity()!.GetChildCount(), Is.EqualTo(1));
        Assert.That(Text(tolerant.GetEntity()!.GetEpilogue()!.Materialize(100)), Is.EqualTo("--x\r\nextra"));
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.MimeUnexpectedBoundary), Is.True);
        Assert.That(tolerant.GetIssues().GetCountBySeverity(EmailIssueSeverity.Warning), Is.EqualTo(1));

        Assert.That(strict.IsComplete(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(strict.GetIssues().ContainsCode(EmailIssueCode.MimeUnexpectedBoundary), Is.True);
        Assert.That(strict.GetIssues().GetCountBySeverity(EmailIssueSeverity.Error), Is.EqualTo(1));
    }

    static EmailParseOptions DecodeOptions(EmailParseOptions options)
    {
        EmailBodyIngestOptions ingest = new EmailBodyIngestOptions();
        ingest.SetDecodeOnIngest(true);
        options.SetBodyIngestOptions(ingest);
        return options;
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
