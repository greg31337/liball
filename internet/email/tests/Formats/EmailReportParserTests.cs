using System.Text;
using email;

namespace email.Tests;

public class EmailReportParserTests
{
    [Test]
    public void RecognizesGenericReportRolesAndPreservesAdditionalParts()
    {
        MimeEntity root = ParseEntity(
            "Content-Type: multipart/report; boundary=r; report-type=x-custom\r\n\r\n"
            + "--r\r\nContent-Type: text/plain\r\n\r\nHuman explanation\r\n"
            + "--r\r\nContent-Type: message/x-custom\r\n\r\nX-State: custom\r\n"
            + "--r\r\nContent-Type: text/rfc822-headers\r\n\r\nSubject: original\r\n"
            + "--r\r\nContent-Type: application/octet-stream\r\n\r\nextra\r\n"
            + "--r--\r\n");

        EmailReportParseResult result = new EmailReportParser().ParseWithRoot(root);
        EmailMultipartReport report = result.GetReport()!;

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(report!.GetRoot(), Is.SameAs(root));
        Assert.That(report.HasReportType(), Is.True);
        Assert.That(report.GetReportType(), Is.EqualTo("x-custom"));
        Assert.That(report.HasHumanReadablePart(), Is.True);
        Assert.That(report.GetHumanReadablePart(), Is.SameAs(root.GetChildAt(0)));
        Assert.That(report.GetMachineReadablePart(), Is.SameAs(root.GetChildAt(1)));
        Assert.That(report.GetOriginalContentPart(), Is.SameAs(root.GetChildAt(2)));
        Assert.That(report.GetAdditionalPartCount(), Is.EqualTo(1));
        Assert.That(report.GetAdditionalPartAt(0), Is.SameAs(root.GetChildAt(3)));
        Assert.That(report.IsTypeConsistent(), Is.True);
        Assert.That(report.IsDeliveryStatus(), Is.False);
        EmailReportFieldBlockParseResult fields = new EmailReportParser().ParseFieldBlocksWithReport(report);
        Assert.That(fields.GetBlockCount(), Is.EqualTo(1));
        Assert.That(fields.GetBlockAt(0).GetFirstField("X-State")!.GetDecodedValue(), Is.EqualTo("custom"));
        report.GetAdditionalParts().Clear();
        Assert.That(report.GetAdditionalPartCount(), Is.EqualTo(1));
    }

    [Test]
    public void AcceptsGlobalKnownVariantsAndDiagnosesMissingOrMismatchedTypes()
    {
        MimeEntity global = ParseEntity(
            "Content-Type: multipart/report; boundary=r; report-type=delivery-status\r\n\r\n"
            + "--r\r\nContent-Type: text/plain\r\n\r\nhuman\r\n"
            + "--r\r\nContent-Type: message/global-delivery-status\r\n\r\nReporting-MTA: dns; mx.example\r\n"
            + "--r--\r\n");
        EmailReportParseResult globalResult = new EmailReportParser().ParseWithRoot(global);
        Assert.That(globalResult.IsSuccess(), Is.True);
        Assert.That(globalResult.GetReport()!.IsDeliveryStatus(), Is.True);
        Assert.That(globalResult.GetReport()!.IsTypeConsistent(), Is.True);

        MimeEntity missing = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("report", new List<MimeEntity>
        {
            new EmailEntityFactory().CreateTextWithValue("human"),
            ContentEntity("message/delivery-status", "Reporting-MTA: dns; mx.example\r\n")
        });
        EmailReportParseResult missingResult = new EmailReportParser().ParseWithRoot(missing);
        Assert.That(missingResult.HasReport(), Is.True);
        Assert.That(missingResult.IsSuccess(), Is.False);
        Assert.That(missingResult.GetReport()!.HasReportType(), Is.True);
        Assert.That(missingResult.GetReport()!.GetReportType(), Is.EqualTo("delivery-status"));
        Assert.That(missingResult.GetIssues().ContainsCode(EmailIssueCode.ReportTypeMissing), Is.True);

        MimeEntity mismatch = ParseEntity(
            "Content-Type: multipart/report; boundary=r; report-type=delivery-status\r\n\r\n"
            + "--r\r\nContent-Type: text/plain\r\n\r\nhuman\r\n"
            + "--r\r\nContent-Type: message/feedback-report\r\n\r\nFeedback-Type: abuse\r\n"
            + "--r--\r\n");
        EmailReportParseResult mismatchResult = new EmailReportParser().ParseWithRoot(mismatch);
        Assert.That(mismatchResult.GetReport()!.IsTypeConsistent(), Is.False);
        Assert.That(mismatchResult.GetIssues().ContainsCode(EmailIssueCode.ReportTypeMismatch), Is.True);
    }

    [Test]
    public void NonReportsAndIncompleteReportStructuresReturnEvidenceRichResults()
    {
        EmailReportParseResult nonReport = new EmailReportParser().ParseWithRoot(new EmailEntityFactory().CreateTextWithValue("ordinary"));
        Assert.That(nonReport.HasReport(), Is.False);
        Assert.That(nonReport.IsSuccess(), Is.False);
        Assert.That(nonReport.GetIssues().ContainsCode(EmailIssueCode.ReportStructureMalformed), Is.True);

        MimeEntity incomplete = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("report", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("human") });
        EmailReportParseResult incompleteResult = new EmailReportParser().ParseWithRoot(incomplete);
        Assert.That(incompleteResult.HasReport(), Is.True);
        Assert.That(incompleteResult.GetReport()!.HasReportType(), Is.False);
        Assert.That(incompleteResult.GetReport()!.HasMachineReadablePart(), Is.False);
        Assert.That(incompleteResult.GetIssues().ContainsCode(EmailIssueCode.ReportStructureMalformed), Is.True);

        EmailReportFieldBlockParseResult fields = new EmailReportParser().ParseFieldBlocksWithReport(incompleteResult.GetReport()!);
        Assert.That(fields.GetBlockCount(), Is.EqualTo(0));
        Assert.That(fields.GetIssues().ContainsCode(EmailIssueCode.ReportStructureMalformed), Is.True);
    }

    [Test]
    public void FieldBlocksPreserveOrderingFoldingExtensionsAndExactBytes()
    {
        const string source = "Reporting-MTA: dns; mx.example\r\n"
            + "X-Trace: one\r\n\tcontinued\r\n\r\n"
            + "Final-Recipient: rfc822; user@example.test\n"
            + "Action: failed\nX-Extension: raw\n";
        EmailSourcePosition initial = new EmailSourcePosition(50, 4, 1);

        EmailReportFieldBlockParseResult result = new EmailReportFieldBlockParser().ParseWithInputAndOptionsAndInitialPosition(Encoding.ASCII.GetBytes(source), new EmailReportParseOptions(), initial);
        EmailReportFieldBlock first = result.GetBlockAt(0);
        EmailReportFieldBlock second = result.GetBlockAt(1);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetBlockCount(), Is.EqualTo(2));
        Assert.That(first.GetFieldCount(), Is.EqualTo(2));
        Assert.That(first.GetFirstField("X-Trace")!.GetDecodedValue(), Is.EqualTo("one\tcontinued"));
        Assert.That(Ascii(first.GetFirstField("X-Trace")!.GetOriginalRawFieldBytes()), Is.EqualTo("X-Trace: one\r\n\tcontinued\r\n"));
        Assert.That(Ascii(first.GetSeparatorBytes()), Is.EqualTo("\r\n"));
        Assert.That(first.GetSourcePosition().GetByteOffset(), Is.EqualTo(50));
        Assert.That(second.GetFieldCount(), Is.EqualTo(3));
        Assert.That(second.GetFirstField("X-Extension")!.GetDecodedValue(), Is.EqualTo("raw"));
        Assert.That(Ascii(second.GetRawBytes()), Is.EqualTo("Final-Recipient: rfc822; user@example.test\nAction: failed\nX-Extension: raw\n"));
        Assert.That(second.GetSeparatorBytes(), Is.Empty);

        byte[] returnedRaw = first.GetRawBytes();
        returnedRaw[0] = 0;
        EmailHeaders returnedFields = first.GetFields();
        returnedFields.Clear();
        List<EmailReportFieldBlock> returnedBlocks = result.GetBlocks();
        returnedBlocks.Clear();
        Assert.That(first.GetFieldCount(), Is.EqualTo(2));
        Assert.That(first.GetRawBytes()[0], Is.EqualTo((byte)'R'));
        Assert.That(result.GetBlockCount(), Is.EqualTo(2));
    }

    [Test]
    public void ParsesTransferDecodedMachinePartsAndFlagsMalformedFields()
    {
        string payload = Convert.ToBase64String(Encoding.ASCII.GetBytes("A: one\r\n\r\nB: two\r\n"));
        MimeEntity encoded = ParseEntity(
            "Content-Type: message/delivery-status\r\nContent-Transfer-Encoding: base64\r\n\r\n" + payload);

        EmailReportFieldBlockParseResult decoded = new EmailReportFieldBlockParser().ParseWithEntity(encoded);

        Assert.That(decoded.GetBlockCount(), Is.EqualTo(2));
        Assert.That(decoded.GetBlockAt(0).GetFirstField("A")!.GetDecodedValue(), Is.EqualTo("one"));
        Assert.That(decoded.GetBlockAt(1).GetFirstField("B")!.GetDecodedValue(), Is.EqualTo("two"));

        EmailReportFieldBlockParseResult malformed = new EmailReportFieldBlockParser().ParseWithInput(Encoding.ASCII.GetBytes("Broken field\r\n\r\n"));
        Assert.That(malformed.GetIssues().ContainsCode(EmailIssueCode.ReportFieldBlockMalformed), Is.True);
        Assert.That(malformed.GetBlockAt(0).GetRawBytes(), Is.EqualTo(Encoding.ASCII.GetBytes("Broken field\r\n")));
    }

    [Test]
    public void ReportAndFieldLimitsAreEnforcedBeforePartialResultsEscape()
    {
        EmailReportParseOptions options = new EmailReportParseOptions();
        options.SetMaximumReportBytes(3);
        AssertLimit("MaxReportBytes", () => new EmailReportFieldBlockParser().ParseWithInputAndOptions(Encoding.ASCII.GetBytes("A: one\r\n"), options));

        options = new EmailReportParseOptions();
        options.SetMaximumFieldBlockCount(1);
        AssertLimit("MaxReportFieldBlocks", () => new EmailReportFieldBlockParser().ParseWithInputAndOptions(Encoding.ASCII.GetBytes("A: 1\r\n\r\nB: 2\r\n"), options));

        options = new EmailReportParseOptions();
        options.SetMaximumFieldsPerBlock(1);
        AssertLimit("MaxReportFieldsPerBlock", () => new EmailReportFieldBlockParser().ParseWithInputAndOptions(Encoding.ASCII.GetBytes("A: 1\r\nB: 2\r\n"), options));

        options = new EmailReportParseOptions();
        options.SetMaximumFieldCount(1);
        AssertLimit("MaxReportFields", () => new EmailReportFieldBlockParser().ParseWithInputAndOptions(Encoding.ASCII.GetBytes("A: 1\r\n\r\nB: 2\r\n"), options));

        options = new EmailReportParseOptions();
        options.SetMaximumFieldBlockBytes(3);
        AssertLimit("MaxReportFieldBlockBytes", () => new EmailReportFieldBlockParser().ParseWithInputAndOptions(Encoding.ASCII.GetBytes("A: one\r\n"), options));

        options = new EmailReportParseOptions();
        options.SetMaximumLineBytes(3);
        AssertLimit("MaxReportLineBytes", () => new EmailReportFieldBlockParser().ParseWithInputAndOptions(Encoding.ASCII.GetBytes("A: one\r\n"), options));

        MimeEntity report = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("report", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("one"), new EmailEntityFactory().CreateTextWithValue("two") });
        options = new EmailReportParseOptions();
        options.SetMaximumPartCount(1);
        AssertLimit("MaxReportParts", () => new EmailReportParser().ParseWithRootAndOptions(report, options));
    }

    [Test]
    public void OptionsAndPublicInputsAreValidatedAndSnapshotsAreIndependent()
    {
        EmailReportParseOptions options = new EmailReportParseOptions();
        options.SetMaximumReportBytes(10);
        options.SetMaximumPartCount(11);
        options.SetMaximumFieldBlockCount(12);
        options.SetMaximumFieldCount(13);
        options.SetMaximumFieldsPerBlock(14);
        options.SetMaximumFieldBlockBytes(15);
        options.SetMaximumLineBytes(16);
        options.SetMode(EmailStructuredValueParseMode.Strict);
        EmailReportParseOptions clone = options.Clone();
        options.SetMaximumReportBytes(20);
        Assert.That(clone.GetMaximumReportBytes(), Is.EqualTo(10));
        Assert.That(clone.GetMaximumPartCount(), Is.EqualTo(11));
        Assert.That(clone.GetMaximumFieldBlockCount(), Is.EqualTo(12));
        Assert.That(clone.GetMaximumFieldCount(), Is.EqualTo(13));
        Assert.That(clone.GetMaximumFieldsPerBlock(), Is.EqualTo(14));
        Assert.That(clone.GetMaximumFieldBlockBytes(), Is.EqualTo(15));
        Assert.That(clone.GetMaximumLineBytes(), Is.EqualTo(16));
        Assert.That(clone.GetMode(), Is.EqualTo(EmailStructuredValueParseMode.Strict));

        Assert.Throws<ArgumentNullException>(() => new EmailReportParser().ParseWithRoot((MimeEntity)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailReportParser().ParseWithRootAndOptions(new EmailEntityFactory().CreateTextWithValue("x"), null!));
        Assert.Throws<ArgumentNullException>(() => new EmailReportParser().ParseWithMessage((EmailMessage)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailReportFieldBlockParser().ParseWithInputAndOptions(Encoding.ASCII.GetBytes("A: 1"), null!));
        Assert.Throws<ArgumentNullException>(() => new EmailReportFieldBlockParser().ParseWithEntity((MimeEntity)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumReportBytes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumPartCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumFieldBlockCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumFieldCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumFieldsPerBlock(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumFieldBlockBytes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumLineBytes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMode((EmailStructuredValueParseMode)10));
    }

    static MimeEntity ParseEntity(string source)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Encoding.ASCII.GetBytes(source));
        Assert.That(result.HasEntity(), Is.True);
        return result.GetEntity()!;
    }

    static MimeEntity ContentEntity(string mediaType, string content)
    {
        EmailMediaType parsed = new EmailMediaTypeParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(mediaType), EmailStructuredValueParseMode.Strict).GetValue();
        MimeEntityKind kind = parsed.IsType("message", "delivery-status") ? MimeEntityKind.DeliveryStatus : MimeEntityKind.Leaf;
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(kind, new EmailHeadersOperations().CreateWith(), parsed, null, "7bit", null, null, null, null, null, new EmailContentOperations().CreateMemoryCopy(Encoding.ASCII.GetBytes(content), false), new List<MimeEntity>(), null, null, null, null, new EmailIssueList());
    }

    static string Ascii(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }

    static void AssertLimit(string expectedName, TestDelegate action)
    {
        EmailLimitException exception = Assert.Throws<EmailLimitException>(action);
        Assert.That(exception.GetLimitName(), Is.EqualTo(expectedName));
    }
}
