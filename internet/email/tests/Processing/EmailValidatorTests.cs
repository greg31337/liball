using System.Text;
using email;

namespace email.Tests;

public class EmailValidatorTests
{
    [Test]
    public void ConformingMessagePassesSyntaxAndSemanticValidation()
    {
        EmailMessage message = ParseMessage(
            "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\n"
            + "From: Alice <alice@example.test>\r\n"
            + "MIME-Version: 1.0\r\n"
            + "Content-Type: multipart/mixed; boundary=outer\r\n\r\n"
            + "--outer\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Transfer-Encoding: quoted-printable\r\n\r\nplain\r\n"
            + "--outer\r\nContent-Type: multipart/related; boundary=rel; start=\"<root@example.test>\"\r\n\r\n"
            + "--rel\r\nContent-Type: text/html; charset=utf-8\r\nContent-ID: <root@example.test>\r\n\r\n<img src='cid:logo@example.test'>\r\n"
            + "--rel\r\nContent-Type: image/png\r\nContent-ID: <logo@example.test>\r\n\r\npng\r\n"
            + "--rel--\r\n"
            + "--outer--\r\n");

        EmailValidationResult result = new EmailValidator().ValidateWithMessage(message);

        Assert.That(result.IsValid(), Is.True);
        Assert.That(result.IsSyntaxValid(), Is.True);
        Assert.That(result.IsSemanticallyValid(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.GetValidatedEntityCount(), Is.EqualTo(5));
        Assert.That(result.GetValidatedHeaderCount(), Is.GreaterThan(5));
    }

    [Test]
    public void InvalidBoundaryIsASyntaxIssueWithEntityEvidence()
    {
        MimeEntity root = ParsePartialEntity(
            "Content-Type: multipart/mixed; boundary=outer\r\n\r\n"
            + "--outer\r\nContent-Type: multipart/alternative; boundary=\"bad[\"\r\n\r\npayload\r\n"
            + "--outer--\r\n");

        EmailValidationResult result = new EmailValidator().ValidateWithEntity(root);
        EmailIssue issue = FindIssue(result.GetSyntaxIssues(), EmailIssueCode.ValidationMimeBoundaryInvalid);

        Assert.That(result.IsSyntaxValid(), Is.False);
        Assert.That(issue, Is.Not.Null);
        Assert.That(issue.HasEntityPath(), Is.True);
        Assert.That(issue.GetEntityPath()!.GetDisplayText(), Is.EqualTo("root/1"));
        Assert.That(issue.GetFieldName(), Is.EqualTo("Content-Type"));
        Assert.That(issue.HasSourcePosition(), Is.True);
        Assert.That(issue.HasRecoveryDescription(), Is.True);
        Assert.That(result.GetSemanticIssues().ContainsCode(EmailIssueCode.ValidationMimeBoundaryInvalid), Is.False);
    }

    [Test]
    public void EncodedBodySyntaxIsValidatedWithTransferFieldEvidence()
    {
        MimeEntity entity = ParsePartialEntity("Content-Type: application/octet-stream\r\nContent-Transfer-Encoding: base64\r\n\r\n%%%invalid%%%");

        EmailValidationResult result = new EmailValidator().ValidateWithEntity(entity);
        EmailIssue issue = FindIssue(result.GetSyntaxIssues(), EmailIssueCode.Base64InvalidCharacter);

        Assert.That(result.IsSyntaxValid(), Is.False);
        Assert.That(issue, Is.Not.Null);
        Assert.That(issue.GetEntityPath()!.GetDisplayText(), Is.EqualTo("root"));
        Assert.That(issue.GetFieldName(), Is.EqualTo("Content-Transfer-Encoding"));
        Assert.That(issue.HasRecoveryDescription(), Is.True);
    }

    [Test]
    public void SemanticDefectsAreDistinctAndValidationDoesNotRepairOrDirtyTheMessage()
    {
        const string source = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\n"
            + "From: Alice <alice@example.test>\r\n"
            + "Content-Type: multipart/related; boundary=rel; start=\"<missing@example.test>\"\r\n"
            + "Content-Transfer-Encoding: base64\r\n\r\n"
            + "--rel\r\nContent-Type: text/plain; charset=x-unknown\r\nContent-Transfer-Encoding: x-custom\r\nContent-ID: <same@example.test>\r\n\r\none\r\n"
            + "--rel\r\nContent-Type: application/octet-stream\r\nContent-ID: <same@EXAMPLE.TEST>\r\n\r\ntwo\r\n"
            + "--rel--\r\n";
        EmailMessage message = ParseMessage(source);
        MimeEntity root = message.GetBody()!;
        MimeEntity first = root!.GetChildAt(0);
        MimeEntity second = root.GetChildAt(1);
        EmailHeader contentType = root.GetHeaders()!.GetFirst("Content-Type")!;
        bool hadStructuredCache = contentType!.HasStructuredValueCache();
        byte[] before = new EmailWriter().ToArrayWithMessageAndOptions(message, new EmailWriteOptionsOperations().CreateExactPreservation());

        EmailValidationResult result = new EmailValidator().ValidateWithMessage(message);
        byte[] after = new EmailWriter().ToArrayWithMessageAndOptions(message, new EmailWriteOptionsOperations().CreateExactPreservation());

        Assert.That(result.IsSyntaxValid(), Is.True);
        Assert.That(result.IsSemanticallyValid(), Is.False);
        Assert.That(result.GetSemanticIssues().ContainsCode(EmailIssueCode.ValidationMimeVersionMissing), Is.True);
        Assert.That(result.GetSemanticIssues().ContainsCode(EmailIssueCode.ValidationContainerTransferEncoding), Is.True);
        Assert.That(result.GetSemanticIssues().ContainsCode(EmailIssueCode.ValidationRelatedStartUnresolved), Is.True);
        Assert.That(result.GetSemanticIssues().ContainsCode(EmailIssueCode.ValidationDuplicateContentId), Is.True);
        Assert.That(result.GetSemanticIssues().ContainsCode(EmailIssueCode.ValidationCharsetUnsupported), Is.True);
        Assert.That(result.GetSemanticIssues().ContainsCode(EmailIssueCode.ValidationTransferEncodingUnsupported), Is.True);
        Assert.That(FindIssue(result.GetSemanticIssues(), EmailIssueCode.ValidationDuplicateContentId).GetEntityPath()!.GetDisplayText(), Is.EqualTo("root/2"));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
        Assert.That(first.GetParent(), Is.SameAs(root));
        Assert.That(second.GetParent(), Is.SameAs(root));
        Assert.That(contentType.HasStructuredValueCache(), Is.EqualTo(hadStructuredCache));
        Assert.That(after, Is.EqualTo(before));
    }

    [Test]
    public void SpecializedAndSecuritySemanticsAreReportedWithoutSyntaxValidation()
    {
        MimeEntity partial = ParsePartialEntity("Content-Type: message/partial; id=fragment\r\n\r\npart");
        MimeEntity external = ParsePartialEntity("Content-Type: message/external-body; access-type=unknown; permission=execute\r\n\r\nContent-Type: text/plain\r\n\r\n");
        MimeEntity signedContent = new EmailEntityFactory().CreateTextWithValue("signed");
        MimeEntity signature = new EmailEntityFactory().CreateBinaryWithValueAndTypeAndSubtype(new byte[] { 1 }, "application", "pgp-signature");
        MimeEntity signed = SecurityContainer(signedContent, signature);
        EmailHeaders nestedHeaders = ParseHeaders("Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: Nested <nested@example.test>\r\n\r\n");
        MimeEntity nested = new EmailEntityFactory().CreateNestedMessage(new EmailMessageOperations().CreateWithHeaders(nestedHeaders));
        MimeEntity alternative = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("only") });
        MimeEntity root = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { partial, external, signed, nested, alternative });
        EmailValidationOptions options = new EmailValidationOptions();
        options.SetValidateSyntax(false);

        EmailValidationResult result = new EmailValidator().ValidateWithEntityAndOptions(root, options);

        Assert.That(result.GetSyntaxIssues().GetCount(), Is.Zero);
        Assert.That(result.GetSemanticIssues().ContainsCode(EmailIssueCode.ValidationPartialMessageMetadata), Is.True);
        Assert.That(result.GetSemanticIssues().ContainsCode(EmailIssueCode.ValidationExternalBodyMetadata), Is.True);
        Assert.That(result.GetSemanticIssues().ContainsCode(EmailIssueCode.ValidationSecurityProtocolMissing), Is.True);
        Assert.That(result.GetSemanticIssues().ContainsCode(EmailIssueCode.ValidationNestedMessageBodyMissing), Is.True);
        Assert.That(result.GetSemanticIssues().ContainsCode(EmailIssueCode.ValidationMessageBodyMissing), Is.True);
        Assert.That(result.GetSemanticIssues().ContainsCode(EmailIssueCode.ValidationAlternativeStructure), Is.True);
        Assert.That(result.IsSemanticallyValid(), Is.False);
    }

    [Test]
    public void SyntaxAndSemanticPoliciesCanBeSelectedIndependently()
    {
        MimeEntity alternative = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("only") });
        EmailValidationResult both = new EmailValidator().ValidateWithEntity(alternative);
        EmailValidationOptions semanticsOnlyOptions = new EmailValidationOptions();
        semanticsOnlyOptions.SetValidateSyntax(false);
        EmailValidationResult semanticsOnly = new EmailValidator().ValidateWithEntityAndOptions(alternative, semanticsOnlyOptions);
        EmailValidationOptions syntaxOnlyOptions = new EmailValidationOptions();
        syntaxOnlyOptions.SetValidateSemantics(false);
        EmailValidationResult syntaxOnly = new EmailValidator().ValidateWithEntityAndOptions(alternative, syntaxOnlyOptions);

        Assert.That(both.GetSyntaxIssues().ContainsCode(EmailIssueCode.ValidationMimeBoundaryMissing), Is.True);
        Assert.That(both.GetSemanticIssues().ContainsCode(EmailIssueCode.ValidationAlternativeStructure), Is.True);
        Assert.That(semanticsOnly.GetSyntaxIssues().GetCount(), Is.Zero);
        Assert.That(semanticsOnly.IsValid(), Is.True);
        Assert.That(syntaxOnly.GetSemanticIssues().GetCount(), Is.Zero);
        Assert.That(syntaxOnly.IsValid(), Is.False);
    }

    [Test]
    public void OptionsResultsArgumentsAndTraversalLimitsAreDefensive()
    {
        EmailValidationOptions options = new EmailValidationOptions();
        EmailHeaderValidationOptions messageHeaders = options.GetMessageHeaderOptions();
        messageHeaders.SetRequireDate(false);
        messageHeaders.SetRequireFrom(false);
        options.SetMessageHeaderOptions(messageHeaders);
        options.SetRequireMimeVersion(false);
        EmailValidationOptions clone = options.Clone();
        messageHeaders.SetRequireDate(true);
        EmailValidationResult valid = new EmailValidator().ValidateWithMessageAndOptions(ParseMessage("\r\nbody"), clone);
        EmailIssueList returned = valid.GetIssues();
        returned.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Error, EmailIssueCode.ValidationFailed, "caller copy"));

        Assert.That(valid.IsValid(), Is.True);
        Assert.That(valid.GetIssues().GetCount(), Is.Zero);
        Assert.That(clone.GetMessageHeaderOptions().GetRequireDate(), Is.False);
        Assert.Throws<ArgumentNullException>(() => new EmailValidator().ValidateWithMessage((EmailMessage)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailValidator().ValidateWithEntity((MimeEntity)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailValidator().ValidateWithMessageAndOptions(new EmailMessageOperations().CreateWith(), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumDepth(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumEntityCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumContentBytes(0));
        Assert.Throws<ArgumentException>(() => options.SetMessageHeaderOptions(options.GetEntityHeaderOptions()));
        Assert.Throws<ArgumentException>(() => options.SetEntityHeaderOptions(options.GetMessageHeaderOptions()));

        MimeEntity deep = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("child") });
        EmailValidationOptions limited = new EmailValidationOptions();
        limited.SetMaximumDepth(0);
        Assert.That(Assert.Throws<EmailLimitException>(() => new EmailValidator().ValidateWithEntityAndOptions(deep, limited)).GetLimitName(), Is.EqualTo("MaxTraversalDepth"));
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.ASCII.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.HasMessage(), Is.True);
        return result.GetMessage()!;
    }

    static MimeEntity ParsePartialEntity(string source)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.ASCII.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.HasEntity(), Is.True);
        return result.GetEntity()!;
    }

    static EmailHeaders ParseHeaders(string source)
    {
        return new EmailHeaderBlockParser().ParseWithInput(Encoding.ASCII.GetBytes(source)).GetHeaders();
    }

    static MimeEntity SecurityContainer(MimeEntity content, MimeEntity signature)
    {
        EmailMediaType mediaType = new EmailMediaTypeParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes("multipart/signed; boundary=s"), EmailStructuredValueParseMode.Strict).GetValue();
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Signed, new EmailHeadersOperations().CreateWith(), mediaType, null, new List<MimeEntity> { content, signature }, null);
    }

    static EmailIssue FindIssue(EmailIssueList issues, EmailIssueCode code)
    {
        for (int index = 0; index < issues.GetCount(); index++)
        {
            if (issues.GetAt(index).GetCode() == code)
            {
                return issues.GetAt(index);
            }
        }

        return null!;
    }
}
