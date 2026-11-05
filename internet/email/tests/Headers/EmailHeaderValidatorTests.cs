using System.Text;
using email;

namespace email.Tests;

public class EmailHeaderValidatorTests
{
    [Test]
    public void ValidatesAConformingRfcMessageHeaderSet()
    {
        EmailHeaders headers = Parse("Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: Alice <alice@example.com>\r\nTo: Bob <bob@example.com>\r\nMessage-ID: <one@example.com>\r\nSubject: hello\r\n\r\n");

        EmailHeaderValidationResult result = new EmailHeaderValidator().ValidateWithHeaders(headers);

        Assert.That(result.IsValid(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.GetValidatedHeaderCount(), Is.EqualTo(5));
        Assert.That(result.GetResentBlockCount(), Is.Zero);
        Assert.That(result.RequiresSmtpUtf8(), Is.False);
        Assert.That(result.HasFatalError(), Is.False);
    }

    [Test]
    public void ReportsMissingRequiredFieldsAndDuplicateSingletons()
    {
        EmailHeaders headers = Parse("Subject: one\r\nsubject: two\r\n\r\n");

        EmailHeaderValidationResult result = new EmailHeaderValidator().ValidateWithHeaders(headers);

        Assert.That(result.IsValid(), Is.False);
        Assert.That(CountCode(result.GetIssues(), EmailIssueCode.HeaderValidationRequiredFieldMissing), Is.EqualTo(2));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationCardinality), Is.True);
    }

    [Test]
    public void AllowsRepeatedCommentsAndKeywordsFields()
    {
        EmailHeaders headers = BaseHeaders();
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("Comments", Bytes(" first")));
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("Comments", Bytes(" second")));
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Keywords", Bytes(" alpha"), EmailHeaderValueKind.Keywords));
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Keywords", Bytes(" beta"), EmailHeaderValueKind.Keywords));

        EmailHeaderValidationResult result = new EmailHeaderValidator().ValidateWithHeaders(headers);

        Assert.That(result.IsValid(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationCardinality), Is.False);
    }

    [Test]
    public void MessageIdCanBeRequiredByPolicy()
    {
        EmailHeaders headers = BaseHeaders();
        EmailHeaderValidationOptions options = new EmailHeaderValidationOptions();
        options.SetRequireMessageId(true);

        EmailHeaderValidationResult missing = new EmailHeaderValidator().ValidateWithHeadersAndOptions(headers, options);
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Message-ID", Bytes(" <one@example.com>"), EmailHeaderValueKind.MessageId));
        EmailHeaderValidationResult present = new EmailHeaderValidator().ValidateWithHeadersAndOptions(headers, options);

        Assert.That(missing.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationRequiredFieldMissing), Is.True);
        Assert.That(present.IsValid(), Is.True);
    }

    [Test]
    public void StrictStructuredValidationReportsMalformedKnownValues()
    {
        EmailHeaders headers = Parse("Date: definitely not a date\r\nFrom: Alice <alice@example.com>, broken <\r\nContent-Type: text plain\r\n\r\n");

        EmailHeaderValidationResult result = new EmailHeaderValidator().ValidateWithHeaders(headers);

        Assert.That(result.IsValid(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateMalformed), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.AddressMalformed), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MediaTypeMalformed), Is.True);
    }

    [Test]
    public void ReportsInvalidNamesValuesNulAndPhysicalLineLength()
    {
        EmailHeader invalidName = Preserved(Bytes("Bad Name"), Bytes(" value"));
        EmailHeader nul = Preserved(Bytes("X-Nul"), Bytes(" one\0two"));
        EmailHeader bareLineFeed = Preserved(Bytes("X-Lf"), Bytes(" one\n two"));
        EmailHeader longLine = Preserved(Bytes("X-Long"), Bytes(" 1234567890"));
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { invalidName, nul, bareLineFeed, longLine });
        EmailHeaderValidationOptions options = NonRequiredOptions();
        options.SetMaximumLineLength(10);

        EmailHeaderValidationResult result = new EmailHeaderValidator().ValidateWithHeadersAndOptions(headers, options);

        Assert.That(result.IsValid(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationInvalidName), Is.True);
        Assert.That(CountCode(result.GetIssues(), EmailIssueCode.HeaderValidationInvalidValue), Is.EqualTo(2));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationLineTooLong), Is.True);
    }

    [Test]
    public void ObsoleteControlBytesRequireExplicitPolicy()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { Preserved(Bytes("X-Control"), new byte[] { 32, 111, 110, 101, 1, 116, 119, 111 }) });
        EmailHeaderValidationOptions strictOptions = NonRequiredOptions();
        EmailHeaderValidationOptions obsoleteOptions = NonRequiredOptions();
        obsoleteOptions.SetAllowObsoleteSyntax(true);

        EmailHeaderValidationResult strict = new EmailHeaderValidator().ValidateWithHeadersAndOptions(headers, strictOptions);
        EmailHeaderValidationResult obsolete = new EmailHeaderValidator().ValidateWithHeadersAndOptions(headers, obsoleteOptions);

        Assert.That(strict.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationInvalidValue), Is.True);
        Assert.That(obsolete.IsValid(), Is.True);
    }

    [Test]
    public void ReportsBarePhysicalFieldEndings()
    {
        EmailHeaders headers = Parse("X-Test: value\n\n");
        EmailHeaderValidationOptions options = NonRequiredOptions();

        EmailHeaderValidationResult result = new EmailHeaderValidator().ValidateWithHeadersAndOptions(headers, options);

        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationInvalidValue), Is.True);
        Assert.That(result.IsValid(), Is.False);
    }

    [Test]
    public void ExactConfiguredLineLengthIsAccepted()
    {
        EmailHeaderValidationOptions options = NonRequiredOptions();
        options.SetMaximumLineLength(10);
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { new EmailHeaderOperations().CreateWithNameAndRawValue("X", Bytes(" 1234567")) });

        EmailHeaderValidationResult result = new EmailHeaderValidator().ValidateWithHeadersAndOptions(headers, options);

        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationLineTooLong), Is.False);
        Assert.That(result.IsValid(), Is.True);
    }

    [Test]
    public void SmtpUtf8PolicyDistinguishesRequirementFromInvalidUtf8()
    {
        EmailHeaders international = Parse("Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: José <jose@example.com>\r\n\r\n");
        EmailHeaderValidationResult asciiPolicy = new EmailHeaderValidator().ValidateWithHeaders(international);
        EmailHeaderValidationOptions utf8Options = new EmailHeaderValidationOptions();
        utf8Options.SetAllowSmtpUtf8(true);
        EmailHeaderValidationResult utf8Policy = new EmailHeaderValidator().ValidateWithHeadersAndOptions(international, utf8Options);

        EmailHeaders invalid = BaseHeaders();
        invalid.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Subject", new byte[] { 32, 195, 40 }, EmailHeaderValueKind.Unstructured));
        EmailHeaderValidationResult invalidUtf8 = new EmailHeaderValidator().ValidateWithHeadersAndOptions(invalid, utf8Options);

        Assert.That(asciiPolicy.RequiresSmtpUtf8(), Is.True);
        Assert.That(asciiPolicy.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationSmtpUtf8Required), Is.True);
        Assert.That(utf8Policy.RequiresSmtpUtf8(), Is.True);
        Assert.That(utf8Policy.IsValid(), Is.True);
        Assert.That(invalidUtf8.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationInvalidUtf8), Is.True);
        Assert.That(invalidUtf8.IsValid(), Is.False);
    }

    [Test]
    public void MimeEntityContextAllowsContentFieldsButRejectsMessageFields()
    {
        EmailHeaders contentOnly = Parse("Content-Type: text/plain; charset=utf-8\r\nContent-Transfer-Encoding: quoted-printable\r\nContent-Disposition: inline\r\n\r\n");
        EmailHeaderValidationOptions options = new EmailHeaderValidationOptions();
        options.SetContext(EmailHeaderValidationContext.MimeEntity);

        EmailHeaderValidationResult valid = new EmailHeaderValidator().ValidateWithHeadersAndOptions(contentOnly, options);
        contentOnly.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Date", Bytes(" Fri, 21 Nov 1997 09:55:06 -0600"), EmailHeaderValueKind.Date));
        contentOnly.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("MIME-Version", Bytes(" 1.0")));
        EmailHeaderValidationResult invalid = new EmailHeaderValidator().ValidateWithHeadersAndOptions(contentOnly, options);

        Assert.That(valid.IsValid(), Is.True);
        Assert.That(valid.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationRequiredFieldMissing), Is.False);
        Assert.That(CountCode(invalid.GetIssues(), EmailIssueCode.HeaderValidationContext), Is.EqualTo(2));
        Assert.That(invalid.IsValid(), Is.False);
    }

    [Test]
    public void MessageContextAllowsMimeFieldsAndValidatesMimeSpecificSyntax()
    {
        EmailHeaders headers = BaseHeaders();
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("MIME-Version", Bytes(" 2.0")));
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("Content-Transfer-Encoding", Bytes(" base64; bad")));

        EmailHeaderValidationResult invalid = new EmailHeaderValidator().ValidateWithHeaders(headers);
        headers.GetFirst("MIME-Version")!.SetValue(" (standard) 1.0");
        headers.GetFirst("Content-Transfer-Encoding")!.SetValue(" base64 (transport)");
        EmailHeaderValidationResult valid = new EmailHeaderValidator().ValidateWithHeaders(headers);

        Assert.That(CountCode(invalid.GetIssues(), EmailIssueCode.HeaderValidationMimeValue), Is.EqualTo(2));
        Assert.That(valid.IsValid(), Is.True);
    }

    [Test]
    public void BccFieldsCanBeRejectedByTransportPolicy()
    {
        const string source = "Resent-Date: Fri, 21 Nov 1997 10:00:00 -0600\r\nResent-From: Alice <a@example.com>\r\nResent-Bcc: Hidden <resent@example.com>\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: Origin <origin@example.com>\r\nBcc: Hidden <main@example.com>\r\n\r\n";
        EmailHeaders headers = Parse(source);
        EmailHeaderValidationResult allowed = new EmailHeaderValidator().ValidateWithHeaders(headers);
        EmailHeaderValidationOptions options = new EmailHeaderValidationOptions();
        options.SetRequireBccStripped(true);

        EmailHeaderValidationResult rejected = new EmailHeaderValidator().ValidateWithHeadersAndOptions(headers, options);

        Assert.That(allowed.IsValid(), Is.True);
        Assert.That(CountCode(rejected.GetIssues(), EmailIssueCode.HeaderValidationBccPolicy), Is.EqualTo(2));
        Assert.That(rejected.IsValid(), Is.False);
    }

    [Test]
    public void MainFromAndSenderMustFollowMailboxCardinalityRules()
    {
        EmailHeaders multiple = Parse("Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: Alice <a@example.com>, Bob <b@example.com>\r\n\r\n");
        EmailHeaderValidationResult missingSender = new EmailHeaderValidator().ValidateWithHeaders(multiple);
        multiple.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Sender", Bytes(" Alice <a@example.com>"), EmailHeaderValueKind.Mailbox));
        EmailHeaderValidationResult withSender = new EmailHeaderValidator().ValidateWithHeaders(multiple);

        EmailHeaders single = BaseHeaders();
        single.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Sender", Bytes(" Alice <alice@example.com>"), EmailHeaderValueKind.Mailbox));
        EmailHeaderValidationResult prohibitedSender = new EmailHeaderValidator().ValidateWithHeaders(single);

        Assert.That(missingSender.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationCardinality), Is.True);
        Assert.That(withSender.IsValid(), Is.True);
        Assert.That(prohibitedSender.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationCardinality), Is.True);
        Assert.That(prohibitedSender.IsValid(), Is.False);
    }

    [Test]
    public void ValidatesOneOrMultipleContiguousResentBlocks()
    {
        const string oneBlock = "Resent-Date: Fri, 21 Nov 1997 10:00:00 -0600\r\nResent-From: Alice <a@example.com>, Bob <b@example.com>\r\nResent-Sender: Alice <a@example.com>\r\nResent-To: Carol <c@example.com>\r\nResent-Message-ID: <resent1@example.com>\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: Origin <origin@example.com>\r\n\r\n";
        const string twoBlocks = "Resent-Date: Fri, 21 Nov 1997 11:00:00 -0600\r\nResent-From: New <new@example.com>\r\nResent-Date: Fri, 21 Nov 1997 10:00:00 -0600\r\nResent-From: Old <old@example.com>\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: Origin <origin@example.com>\r\n\r\n";

        EmailHeaderValidationResult one = new EmailHeaderValidator().ValidateWithHeaders(Parse(oneBlock));
        EmailHeaderValidationResult two = new EmailHeaderValidator().ValidateWithHeaders(Parse(twoBlocks));

        Assert.That(one.IsValid(), Is.True);
        Assert.That(one.GetResentBlockCount(), Is.EqualTo(1));
        Assert.That(two.IsValid(), Is.True);
        Assert.That(two.GetResentBlockCount(), Is.EqualTo(2));
    }

    [Test]
    public void ReportsIncompleteDuplicatedOutOfOrderAndNoncontiguousResentFields()
    {
        const string source = "Resent-From: First <first@example.com>\r\nResent-Date: Fri, 21 Nov 1997 10:00:00 -0600\r\nResent-To: One <one@example.com>\r\nResent-To: Two <two@example.com>\r\nSubject: separator\r\nResent-Message-ID: <late@example.com>\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: Origin <origin@example.com>\r\n\r\n";

        EmailHeaderValidationResult result = new EmailHeaderValidator().ValidateWithHeaders(Parse(source));

        Assert.That(result.IsValid(), Is.False);
        Assert.That(result.GetResentBlockCount(), Is.EqualTo(2));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationResentBlock), Is.True);
        Assert.That(CountCode(result.GetIssues(), EmailIssueCode.HeaderValidationResentBlock), Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void ResentSenderRelationshipIsValidatedWithinEachBlock()
    {
        const string missing = "Resent-Date: Fri, 21 Nov 1997 10:00:00 -0600\r\nResent-From: Alice <a@example.com>, Bob <b@example.com>\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: Origin <origin@example.com>\r\n\r\n";
        const string prohibited = "Resent-Date: Fri, 21 Nov 1997 10:00:00 -0600\r\nResent-From: Alice <a@example.com>\r\nResent-Sender: Alice <a@example.com>\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: Origin <origin@example.com>\r\n\r\n";

        Assert.That(new EmailHeaderValidator().ValidateWithHeaders(Parse(missing)).GetIssues().ContainsCode(EmailIssueCode.HeaderValidationResentBlock), Is.True);
        Assert.That(new EmailHeaderValidator().ValidateWithHeaders(Parse(prohibited)).GetIssues().ContainsCode(EmailIssueCode.HeaderValidationResentBlock), Is.True);
    }

    [Test]
    public void ObsoleteResentReplyToCanBeAllowedExplicitly()
    {
        const string source = "Resent-Date: Fri, 21 Nov 1997 10:00:00 -0600\r\nResent-From: Alice <a@example.com>\r\nResent-Reply-To: Reply <reply@example.com>\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: Origin <origin@example.com>\r\n\r\n";
        EmailHeaders headers = Parse(source);
        EmailHeaderValidationResult rejected = new EmailHeaderValidator().ValidateWithHeaders(headers);
        EmailHeaderValidationOptions options = new EmailHeaderValidationOptions();
        options.SetAllowObsoleteSyntax(true);
        EmailHeaderValidationResult allowed = new EmailHeaderValidator().ValidateWithHeadersAndOptions(headers, options);

        Assert.That(rejected.IsValid(), Is.False);
        Assert.That(rejected.GetIssues().ContainsCode(EmailIssueCode.ResentFieldObsolete), Is.True);
        Assert.That(allowed.IsValid(), Is.True);
    }

    [Test]
    public void StructuredValidationCanBeDisabledWithoutMutatingHeaderCaches()
    {
        EmailHeaders headers = Parse("Date: invalid\r\nFrom: invalid\r\nContent-Type: text/plain\r\n\r\n");
        EmailHeader contentType = headers.GetFirst("Content-Type")!;
        EmailHeaderValidationOptions options = new EmailHeaderValidationOptions();
        options.SetValidateStructuredValues(false);

        EmailHeaderValidationResult skipped = new EmailHeaderValidator().ValidateWithHeadersAndOptions(headers, options);

        Assert.That(skipped.IsValid(), Is.True);
        Assert.That(contentType!.HasStructuredValueCache(), Is.False);
        new EmailHeaderValidator().ValidateWithHeaders(headers);
        Assert.That(contentType.HasStructuredValueCache(), Is.False);
    }

    [Test]
    public void FromSenderRelationshipStillAppliesWhenStructuredDiagnosticsAreDisabled()
    {
        EmailHeaders headers = Parse("Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: Alice <a@example.com>, Bob <b@example.com>\r\n\r\n");
        EmailHeaderValidationOptions options = new EmailHeaderValidationOptions();
        options.SetValidateStructuredValues(false);

        EmailHeaderValidationResult result = new EmailHeaderValidator().ValidateWithHeadersAndOptions(headers, options);

        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationCardinality), Is.True);
        Assert.That(result.IsValid(), Is.False);
    }

    [Test]
    public void OptionsAndResultsAreDefensiveAndArgumentsAreValidated()
    {
        EmailHeaderValidationOptions options = new EmailHeaderValidationOptions();
        options.SetAllowSmtpUtf8(true);
        options.SetRequireMessageId(true);
        options.SetRequireBccStripped(true);
        EmailHeaderValidationOptions clone = options.Clone();
        options.SetRequireMessageId(false);
        EmailHeaderValidationResult result = new EmailHeaderValidator().ValidateWithHeadersAndOptions(BaseHeaders(), clone);
        EmailIssueList issues = result.GetIssues();
        issues.Clear();

        Assert.That(clone.GetRequireMessageId(), Is.True);
        Assert.That(clone.GetRequireBccStripped(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationRequiredFieldMissing), Is.True);
        Assert.Throws<ArgumentNullException>(() => new EmailHeaderValidator().ValidateWithHeaders(null!));
        Assert.Throws<ArgumentNullException>(() => new EmailHeaderValidator().ValidateWithHeadersAndOptions(BaseHeaders(), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetContext((EmailHeaderValidationContext)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumLineLength(999));
    }

    static EmailHeaders Parse(string source)
    {
        return new EmailHeaderBlockParser().ParseWithInput(Bytes(source)).GetHeaders();
    }

    static EmailHeaders BaseHeaders()
    {
        return Parse("Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: Alice <alice@example.com>\r\n\r\n");
    }

    static EmailHeaderValidationOptions NonRequiredOptions()
    {
        EmailHeaderValidationOptions options = new EmailHeaderValidationOptions();
        options.SetRequireDate(false);
        options.SetRequireFrom(false);
        return options;
    }

    static EmailHeader Preserved(byte[] name, byte[] value)
    {
        return new EmailHeaderOperations().CreateWithOriginalNameBytesAndOriginalRawValueAndLineEndingAndSourceRangeAndValuePositionAndValueKindAndParseModeAndIssues(name, value, EmailLineEnding.CrLf, null!, new EmailSourcePosition(0, 1, 1), EmailHeaderValueKind.Unknown, EmailStructuredValueParseMode.Tolerant, new EmailIssueList());
    }

    static int CountCode(EmailIssueList issues, EmailIssueCode code)
    {
        int count = 0;
        for (int index = 0; index < issues.GetCount(); index++)
        {
            if (issues.GetAt(index).GetCode() == code)
            {
                count++;
            }
        }

        return count;
    }

    static byte[] Bytes(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }
}
