using System.Text;
using email;

namespace email.Tests;

public class EmailQueryTests
{
    [Test]
    public void RawAndDecodedFieldQueriesUseDistinctRepresentationsAndOrdinalOptions()
    {
        EmailMessage message = ParseMessage(
            "From: sender@example.test\r\n"
            + "Subject: =?utf-8?Q?Caf=C3=A9_Quarterly?=\r\n"
            + "X-Fold: alpha\r\n beta\r\n"
            + "X-Tag: MixedCase\r\n\r\nbody");

        Assert.That(new EmailQueryOperations().HasField("subject").Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().HasField("subject").GetKind(), Is.EqualTo(EmailQueryKind.HasField));
        Assert.That(new EmailQueryOperations().RawFieldContains("Subject", "=?utf-8?Q?").Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().RawFieldContains("Subject", "Café").Matches(message), Is.False);
        Assert.That(new EmailQueryOperations().DecodedFieldEquals("Subject", "Café Quarterly").Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().RawFieldContains("X-Fold", "\r\n beta").Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().DecodedFieldEquals("X-Fold", "alpha beta").Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().DecodedFieldContains("X-Tag", "mixedcase").Matches(message), Is.True);

        EmailQueryOptions sensitive = new EmailQueryOptions();
        sensitive.SetCaseSensitive(true);
        Assert.That(new EmailQueryOperations().DecodedFieldContains("X-Tag", "mixedcase").MatchesWithOptions(message, sensitive), Is.False);
        Assert.That(new EmailQueryOperations().DecodedFieldContains("X-Tag", "MixedCase").MatchesWithOptions(message, sensitive), Is.True);
    }

    [Test]
    public void AddressQueriesHandleGroupsFieldSelectionAndInternationalDomains()
    {
        EmailMessage message = ParseMessage(
            "From: Alice <alice@example.test>\r\n"
            + "To: Team: Bob <bob@xn--bcher-kva.example>, carol@example.test;\r\n"
            + "Cc: duplicate@EXAMPLE.TEST\r\n\r\nbody");

        Assert.That(new EmailQueryOperations().AddressEqualsWithValue(Address("alice@example.test")).Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().AddressEqualsWithFieldNameAndValue("To", Address("bob@xn--bcher-kva.example")).Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().AddressEqualsWithFieldNameAndValue("From", Address("bob@xn--bcher-kva.example")).Matches(message), Is.False);
        Assert.That(new EmailQueryOperations().AddressDomainEqualsWithDomain("bücher.example").Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().AddressDomainEqualsWithFieldNameAndDomain("Cc", "example.test").Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().AddressDomainEqualsWithFieldNameAndDomain("To", "missing.example").Matches(message), Is.False);
    }

    [Test]
    public void DateQueriesCompareInstantsAndUseInclusiveExclusiveRanges()
    {
        EmailMessage message = ParseMessage(
            "Date: Fri, 21 Nov 1997 10:00:00 -0600\r\n"
            + "Resent-Date: Fri, 21 Nov 1997 17:00:00 +0000\r\n"
            + "Resent-From: sender@example.test\r\n"
            + "From: sender@example.test\r\n\r\nbody");
        EmailDateTime sameInstant = Date("Fri, 21 Nov 1997 16:00:00 +0000");

        Assert.That(new EmailQueryOperations().DateEqualsWithValue(sameInstant).Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().DateBeforeWithValue(Date("Fri, 21 Nov 1997 16:00:01 +0000")).Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().DateOnOrAfterWithValue(Date("Fri, 21 Nov 1997 16:00:00 +0000")).Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().DateBetweenWithEarliestInclusiveAndLatestExclusive(Date("Fri, 21 Nov 1997 15:59:59 +0000"), Date("Fri, 21 Nov 1997 16:00:01 +0000")).Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().DateBetweenWithFieldNameAndEarliestInclusiveAndLatestExclusive("Resent-Date", Date("Fri, 21 Nov 1997 17:00:00 +0000"), Date("Fri, 21 Nov 1997 18:00:00 +0000")).Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().DateBetweenWithEarliestInclusiveAndLatestExclusive(Date("Fri, 21 Nov 1997 16:00:01 +0000"), Date("Fri, 21 Nov 1997 17:00:00 +0000")).Matches(message), Is.False);

        EmailMessage unknownZone = ParseMessage("Date: Fri, 21 Nov 1997 16:00:00 -0000\r\nFrom: sender@example.test\r\n\r\nbody");
        Assert.That(new EmailQueryOperations().DateEqualsWithValue(sameInstant).Matches(unknownZone), Is.False);
        EmailQueryOptions localDates = new EmailQueryOptions();
        localDates.SetRequireKnownDateZone(false);
        Assert.That(new EmailQueryOperations().DateEqualsWithValue(sameInstant).MatchesWithOptions(unknownZone, localDates), Is.True);
    }

    [Test]
    public void MimeAttachmentAndBodyQueriesUseDecodedProjections()
    {
        EmailMessage message = ParseMessage(
            "From: sender@example.test\r\n"
            + "Content-Type: multipart/mixed; boundary=m\r\n\r\n"
            + "--m\r\nContent-Type: text/plain; charset=us-ascii\r\nContent-Transfer-Encoding: quoted-printable\r\n\r\nHello=20World\r\n"
            + "--m\r\nContent-Type: text/plain; charset=us-ascii\r\nContent-Disposition: attachment; filename=Notes.TXT\r\n\r\nsecret appendix\r\n"
            + "--m\r\nContent-Type: application/pdf\r\nContent-Disposition: attachment; filename=Report.PDF\r\n\r\nPDF\r\n"
            + "--m--\r\n");

        Assert.That(new EmailQueryOperations().MimeType("application", "pdf").Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().MimeTopLevelType("APPLICATION").Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().MimeType("image", "png").Matches(message), Is.False);
        Assert.That(new EmailQueryOperations().HasAttachment().Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().AttachmentMimeType("application", "pdf").Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().AttachmentFilenameContains("report.pdf").Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().BodyContains("hello world").Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().BodyContains("secret appendix").Matches(message), Is.False);

        EmailQueryOptions attachments = new EmailQueryOptions();
        attachments.SetIncludeTextAttachments(true);
        Assert.That(new EmailQueryOperations().BodyContains("secret appendix").MatchesWithOptions(message, attachments), Is.True);
        attachments.SetCaseSensitive(true);
        Assert.That(new EmailQueryOperations().AttachmentFilenameContains("report.pdf").MatchesWithOptions(message, attachments), Is.False);
    }

    [Test]
    public void IssueQueriesInspectAccumulatedCodesAndSeverity()
    {
        EmailMessage message = ParseMessage("From: sender@example.test\r\n\r\nbody");
        message.AddIssue(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Error, EmailIssueCode.ValidationFailed, "test issue"));

        Assert.That(new EmailQueryOperations().HasIssue(EmailIssueCode.ValidationFailed).Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().HasIssue(EmailIssueCode.HtmlMarkupMalformed).Matches(message), Is.False);
        Assert.That(new EmailQueryOperations().HasIssueSeverityAtLeast(EmailIssueSeverity.Warning).Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().HasIssueSeverityAtLeast(EmailIssueSeverity.Fatal).Matches(message), Is.False);
    }

    [Test]
    public void SecurityQueriesReportStructureWithoutClaimingVerification()
    {
        EmailMessage signed = ParseMessage(
            "From: sender@example.test\r\n"
            + "Content-Type: multipart/signed; boundary=s; protocol=application/pgp-signature\r\n\r\n"
            + "--s\r\nContent-Type: text/plain\r\n\r\nsigned body\r\n"
            + "--s\r\nContent-Type: application/pgp-signature\r\n\r\nsignature\r\n--s--\r\n");
        EmailMessage encrypted = ParseMessage(
            "From: sender@example.test\r\n"
            + "Content-Type: multipart/encrypted; boundary=e; protocol=application/pgp-encrypted\r\n\r\n"
            + "--e\r\nContent-Type: application/pgp-encrypted\r\n\r\nVersion: 1\r\n"
            + "--e\r\nContent-Type: application/octet-stream\r\n\r\nencrypted\r\n--e--\r\n");
        EmailMessage report = new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(new EmailHeadersOperations().CreateWith(), new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("report", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("report") }), new EmailEnvelopeOperations().CreateWith(), null, new EmailIssueList());
        EmailMessage plain = ParseMessage("From: sender@example.test\r\n\r\nplain");

        Assert.That(new EmailQueryOperations().HasSecurityState(EmailQuerySecurityState.Signed).Matches(signed), Is.True);
        Assert.That(new EmailQueryOperations().HasSecurityState(EmailQuerySecurityState.Encrypted).Matches(encrypted), Is.True);
        Assert.That(new EmailQueryOperations().HasSecurityState(EmailQuerySecurityState.SignedAndEncrypted).Matches(signed), Is.False);
        Assert.That(new EmailQueryOperations().HasSecurityState(EmailQuerySecurityState.Report).Matches(report), Is.True);
        Assert.That(new EmailQueryOperations().HasSecurityState(EmailQuerySecurityState.None).Matches(plain), Is.True);
        Assert.That(new EmailQueryOperations().HasSecurityState(EmailQuerySecurityState.None).Matches(signed), Is.False);
    }

    [Test]
    public void EvaluationLimitsAndInvalidQueryInputsFailExplicitly()
    {
        EmailMessage headers = ParseMessage("X-One: 1234\r\nSubject: target\r\nFrom: sender@example.test\r\n\r\nlong body");
        EmailQueryOptions options = new EmailQueryOptions();
        options.SetMaximumQueryCharacters(3);
        AssertLimit("MaxQueryCharacters", () => new EmailQueryOperations().DecodedFieldContains("Subject", "target").MatchesWithOptions(headers, options));
        options.SetMaximumQueryCharacters(100);
        options.SetMaximumHeaderCount(1);
        AssertLimit("MaxQueryHeaders", () => new EmailQueryOperations().HasField("Subject").MatchesWithOptions(headers, options));
        options.SetMaximumHeaderCount(10);
        options.SetMaximumFieldCharacters(3);
        AssertLimit("MaxQueryFieldCharacters", () => new EmailQueryOperations().RawFieldContains("X-One", "missing").MatchesWithOptions(headers, options));
        options.SetMaximumFieldCharacters(100);
        options.SetMaximumBodyCharacters(3);
        AssertLimit("MaxQueryBodyCharacters", () => new EmailQueryOperations().BodyContains("missing").MatchesWithOptions(headers, options));
        options.SetMaximumBodyCharacters(100);
        options.SetMaximumEntityCount(1);
        AssertLimit("MaxTraversalEntityCount", () => new EmailQueryOperations().MimeType("text", "plain").MatchesWithOptions(ParseMessage("Content-Type: multipart/mixed; boundary=m\r\n\r\n--m\r\nContent-Type: text/plain\r\n\r\nbody\r\n--m--\r\n"), options));

        EmailQueryOptions clone = options.Clone();
        options.SetCaseSensitive(true);
        Assert.That(clone.GetCaseSensitive(), Is.False);
        Assert.Throws<ArgumentNullException>(() => new EmailQueryOperations().HasField(null!));
        Assert.Throws<ArgumentException>(() => new EmailQueryOperations().BodyContains(""));
        Assert.Throws<ArgumentException>(() => new EmailQueryOperations().MimeType("application/invalid", "pdf"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailQueryOperations().HasIssue(EmailIssueCode.Unknown));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailQueryOperations().HasSecurityState((EmailQuerySecurityState)10));
        Assert.Throws<ArgumentException>(() => new EmailQueryOperations().DateBetweenWithEarliestInclusiveAndLatestExclusive(Date("Fri, 21 Nov 1997 17:00:00 +0000"), Date("Fri, 21 Nov 1997 16:00:00 +0000")));
        Assert.Throws<ArgumentNullException>(() => new EmailQueryOperations().HasAttachment().Matches(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumEntityCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumBodyBytes(0));
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.UTF8.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetMessage()!;
    }

    static EmailAddress Address(string value)
    {
        EmailAddressParseResult result = new EmailAddressParser().ParseMailbox(Encoding.UTF8.GetBytes(value), EmailAddressParseMode.Strict);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetMailbox()!;
    }

    static EmailDateTime Date(string value)
    {
        EmailDateParseResult result = new EmailDateParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(value), EmailDateParseMode.Strict);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetValue()!;
    }

    static void AssertLimit(string expectedName, TestDelegate action)
    {
        EmailLimitException exception = Assert.Throws<EmailLimitException>(action);
        Assert.That(exception.GetLimitName(), Is.EqualTo(expectedName));
    }
}
