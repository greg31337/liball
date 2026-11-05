using System.Text;
using email;

namespace email.Tests;

public class EmailCompatibilityRuleTests
{
    [Test]
    public void OutlookAndExchangeTnefAliasesAreIndependentlySelectableAndReported()
    {
        EmailParseOptions outlookOptions = CreateOptions();
        SetCompatibility(outlookOptions, EmailIssueCode.OutlookCompatibilityRecovery);
        EmailEntityParseResult outlook = Parse("Content-Type: application/x-tnef\r\n\r\nbody", outlookOptions);
        EmailEntityParseResult outlookDisabled = Parse("Content-Type: application/x-tnef\r\n\r\nbody", CreateOptions());

        Assert.That(outlook.GetEntity()!.GetKind(), Is.EqualTo(MimeEntityKind.Tnef));
        Assert.That(outlook.GetIssues().ContainsCode(EmailIssueCode.OutlookCompatibilityRecovery), Is.True);
        Assert.That(outlookDisabled.GetEntity()!.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));

        EmailParseOptions exchangeOptions = CreateOptions();
        SetCompatibility(exchangeOptions, EmailIssueCode.ExchangeCompatibilityRecovery);
        EmailEntityParseResult exchange = Parse("Content-Type: application/vnd.ms-tnef\r\n\r\nbody", exchangeOptions);
        EmailEntityParseResult exchangeDisabled = Parse("Content-Type: application/vnd.ms-tnef\r\n\r\nbody", CreateOptions());

        Assert.That(exchange.GetEntity()!.GetKind(), Is.EqualTo(MimeEntityKind.Tnef));
        Assert.That(exchange.GetIssues().ContainsCode(EmailIssueCode.ExchangeCompatibilityRecovery), Is.True);
        Assert.That(exchangeDisabled.GetEntity()!.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));
    }

    [Test]
    public void CommonMimeAliasesNormalizeOnlyWhenEnabledAndKeepRawHeaders()
    {
        const string source = "Content-Type: image/jpg\r\n\r\nimage";
        EmailParseOptions enabledOptions = CreateOptions();
        SetCompatibility(enabledOptions, EmailIssueCode.MimeTypeAliasRecovery);

        EmailEntityParseResult enabled = Parse(source, enabledOptions);
        EmailEntityParseResult disabled = Parse(source, CreateOptions());

        Assert.That(enabled.GetEntity()!.GetMediaType()!.IsType("image", "jpeg"), Is.True);
        Assert.That(enabled.GetIssues().ContainsCode(EmailIssueCode.MimeTypeAliasRecovery), Is.True);
        Assert.That(Encoding.ASCII.GetString(enabled.GetEntity()!.GetHeaders()!.GetFirst("Content-Type")!.GetOriginalRawFieldBytes()), Is.EqualTo("Content-Type: image/jpg\r\n"));
        Assert.That(disabled.GetEntity()!.GetMediaType()!.IsType("image", "jpg"), Is.True);
        Assert.That(disabled.GetIssues().ContainsCode(EmailIssueCode.MimeTypeAliasRecovery), Is.False);
    }

    [Test]
    public void BrokenEncodedWordsRemainLiteralUnlessTheirRuleIsEnabled()
    {
        const string source = "Content-Description: =?utf-8?b?QQ?=\r\n\r\nbody";
        EmailParseOptions enabledOptions = CreateOptions();
        SetCompatibility(enabledOptions, EmailIssueCode.EncodedWordCompatibilityRecovery);

        EmailEntityParseResult enabled = Parse(source, enabledOptions);
        EmailEntityParseResult disabled = Parse(source, CreateOptions());

        Assert.That(enabled.GetEntity()!.GetContentDescription(), Is.EqualTo("A"));
        Assert.That(enabled.GetIssues().ContainsCode(EmailIssueCode.EncodedWordCompatibilityRecovery), Is.True);
        Assert.That(disabled.GetEntity()!.GetContentDescription(), Is.EqualTo("=?utf-8?b?QQ?="));
        Assert.That(disabled.GetIssues().ContainsCode(EmailIssueCode.EncodedWordCompatibilityRecovery), Is.False);
    }

    [Test]
    public void BrokenRfc2231ValuesAreExposedOnlyWhenTheirRuleIsEnabled()
    {
        const string source = "Content-Type: application/octet-stream; name*1=orphan.bin\r\n\r\nbody";
        EmailParseOptions enabledOptions = CreateOptions();
        SetCompatibility(enabledOptions, EmailIssueCode.Rfc2231CompatibilityRecovery);

        EmailEntityParseResult enabled = Parse(source, enabledOptions);
        EmailEntityParseResult disabled = Parse(source, CreateOptions());

        Assert.That(enabled.GetEntity()!.GetMediaType()!.GetParameters().GetDecoded("name"), Is.Not.Null);
        Assert.That(enabled.GetIssues().ContainsCode(EmailIssueCode.Rfc2231CompatibilityRecovery), Is.True);
        Assert.That(disabled.GetEntity()!.GetMediaType()!.GetParameters().GetDecoded("name"), Is.Null);
        Assert.That(disabled.GetIssues().ContainsCode(EmailIssueCode.Rfc2231CompatibilityRecovery), Is.False);
    }

    [Test]
    public void LegacyOctetStreamNameBecomesAnAttachmentOnlyWhenEnabled()
    {
        const string source = "Content-Type: application/octet-stream; name=report.bin\r\n\r\nbody";
        EmailParseOptions enabledOptions = CreateOptions();
        SetCompatibility(enabledOptions, EmailIssueCode.LegacyAttachmentRecovery);

        EmailEntityParseResult enabled = Parse(source, enabledOptions);
        EmailEntityParseResult disabled = Parse(source, CreateOptions());

        Assert.That(enabled.GetEntity()!.GetDisposition()!.IsAttachment(), Is.True);
        Assert.That(enabled.GetIssues().ContainsCode(EmailIssueCode.LegacyAttachmentRecovery), Is.True);
        Assert.That(disabled.GetEntity()!.HasDisposition(), Is.False);
    }

    [Test]
    public void LegacyUuencodeBodiesDecodeOnIngestOnlyWhenEnabled()
    {
        const string source = "Content-Transfer-Encoding: x-uuencode\r\n\r\nbegin 644 cat.txt\r\n#0V%T\r\n`\r\nend\r\n";
        EmailParseOptions enabledOptions = CreateOptions();
        SetCompatibility(enabledOptions, EmailIssueCode.LegacyAttachmentRecovery);
        EnableDecodeOnIngest(enabledOptions);
        EmailParseOptions disabledOptions = CreateOptions();
        EnableDecodeOnIngest(disabledOptions);

        EmailEntityParseResult enabled = Parse(source, enabledOptions);
        EmailEntityParseResult disabled = Parse(source, disabledOptions);

        Assert.That(Encoding.ASCII.GetString(enabled.GetEntity()!.GetDecodedContent()!.Materialize(100)), Is.EqualTo("Cat"));
        Assert.That(enabled.GetIssues().ContainsCode(EmailIssueCode.LegacyAttachmentRecovery), Is.True);
        Assert.That(disabled.GetEntity()!.HasDecodedContent(), Is.False);
        Assert.That(disabled.GetIssues().ContainsCode(EmailIssueCode.TransferEncodingUnsupported), Is.True);
    }

    [Test]
    public void MalformedBoundaryRecoveryIsIndependentAndReported()
    {
        const string source = "Content-Type: multipart/mixed; boundary=abc\r\n\r\n--\"abc\"\r\n\r\npart\r\n--\"abc\"--\r\n";
        EmailParseOptions enabledOptions = CreateOptions();
        SetCompatibility(enabledOptions, EmailIssueCode.MimeBoundaryCompatibilityRecovery);

        EmailEntityParseResult enabled = Parse(source, enabledOptions);
        EmailEntityParseResult disabled = Parse(source, CreateOptions());

        Assert.That(enabled.GetEntity()!.GetChildCount(), Is.EqualTo(1));
        Assert.That(enabled.GetIssues().ContainsCode(EmailIssueCode.MimeBoundaryCompatibilityRecovery), Is.True);
        Assert.That(disabled.GetEntity()!.GetChildCount(), Is.Zero);
        Assert.That(disabled.GetIssues().ContainsCode(EmailIssueCode.MimeBoundaryCompatibilityRecovery), Is.False);
    }

    [Test]
    public void ExchangeAndGmailBoundaryDeviationsRequireTheirOwnProviderRules()
    {
        const string exchangeSource = "Content-Type: multipart/mixed; boundary=abc\r\nX-MS-Exchange-Organization-MessageDirectionality: Originating\r\n\r\n--\"abc\"\r\n\r\npart\r\n--\"abc\"--\r\n";
        const string gmailSource = "Content-Type: multipart/mixed; boundary=abc\r\nX-Google-Original-From: sender@example.test\r\n\r\n--\"abc\"\r\n\r\npart\r\n--\"abc\"--\r\n";
        EmailParseOptions exchangeOptions = CreateOptions();
        SetCompatibility(exchangeOptions, EmailIssueCode.ExchangeCompatibilityRecovery);
        EmailParseOptions gmailOptions = CreateOptions();
        SetCompatibility(gmailOptions, EmailIssueCode.GmailCompatibilityRecovery);

        EmailEntityParseResult exchange = Parse(exchangeSource, exchangeOptions);
        EmailEntityParseResult gmail = Parse(gmailSource, gmailOptions);

        Assert.That(exchange.GetEntity()!.GetChildCount(), Is.EqualTo(1));
        Assert.That(exchange.GetIssues().ContainsCode(EmailIssueCode.ExchangeCompatibilityRecovery), Is.True);
        Assert.That(exchange.GetIssues().ContainsCode(EmailIssueCode.GmailCompatibilityRecovery), Is.False);
        Assert.That(gmail.GetEntity()!.GetChildCount(), Is.EqualTo(1));
        Assert.That(gmail.GetIssues().ContainsCode(EmailIssueCode.GmailCompatibilityRecovery), Is.True);
        Assert.That(gmail.GetIssues().ContainsCode(EmailIssueCode.ExchangeCompatibilityRecovery), Is.False);
    }

    [Test]
    public void WholeMessageProviderHeadersControlBoundaryCompatibilityWithoutEnteringBodyHeaders()
    {
        const string source = "X-Google-Original-From: sender@example.test\r\nContent-Type: multipart/mixed; boundary=abc\r\n\r\n--\"abc\"\r\n\r\npart\r\n--\"abc\"--\r\n";
        EmailParseOptions options = CreateOptions();
        SetCompatibility(options, EmailIssueCode.GmailCompatibilityRecovery);

        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.ASCII.GetBytes(source), options);

        Assert.That(result.GetMessage()!.GetBody()!.GetChildCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.GmailCompatibilityRecovery), Is.True);
        Assert.That(result.GetMessage()!.GetBody()!.GetHeaders()!.Contains("X-Google-Original-From"), Is.False);
    }

    [Test]
    public void StrictModeCannotReenableNonstandardRecoveryThroughCompatibilityFlags()
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreateStrict();
        EmailCompatibilityOptions compatibility = new EmailCompatibilityOptions();
        compatibility.SetReturnPartialResults(true);
        options.SetCompatibilityOptions(compatibility);

        EmailEntityParseResult alias = Parse("Content-Type: image/jpg\r\n\r\nimage", options);
        EmailEntityParseResult attachment = Parse("Content-Type: application/octet-stream; name=report.bin\r\n\r\nbody", options);
        EmailEntityParseResult encodedWord = Parse("Content-Description: =?utf-8?b?QQ?=\r\n\r\nbody", options);

        Assert.That(alias.GetEntity()!.GetMediaType()!.IsType("image", "jpg"), Is.True);
        Assert.That(alias.GetIssues().ContainsCode(EmailIssueCode.MimeTypeAliasRecovery), Is.False);
        Assert.That(attachment.GetEntity()!.HasDisposition(), Is.False);
        Assert.That(encodedWord.GetEntity()!.GetContentDescription(), Is.EqualTo("=?utf-8?b?QQ?="));
        Assert.That(encodedWord.GetIssues().ContainsCode(EmailIssueCode.EncodedWordCompatibilityRecovery), Is.False);
    }

    static EmailParseOptions CreateOptions()
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreateConformingTolerant();
        EmailCompatibilityOptions compatibility = options.GetCompatibilityOptions();
        compatibility.SetAllowBrokenMimeBoundaries(false);
        compatibility.SetAllowOutlookDeviations(false);
        compatibility.SetAllowExchangeDeviations(false);
        compatibility.SetAllowGmailDeviations(false);
        compatibility.SetAllowCommonMimeTypeAliases(false);
        compatibility.SetAllowBrokenEncodedWords(false);
        compatibility.SetAllowBrokenRfc2231Parameters(false);
        compatibility.SetAllowLegacyAttachmentRecovery(false);
        options.SetCompatibilityOptions(compatibility);
        return options;
    }

    static void SetCompatibility(EmailParseOptions options, EmailIssueCode code)
    {
        EmailCompatibilityOptions compatibility = options.GetCompatibilityOptions();
        if (code == EmailIssueCode.OutlookCompatibilityRecovery)
        {
            compatibility.SetAllowOutlookDeviations(true);
        }
        else if (code == EmailIssueCode.ExchangeCompatibilityRecovery)
        {
            compatibility.SetAllowExchangeDeviations(true);
        }
        else if (code == EmailIssueCode.GmailCompatibilityRecovery)
        {
            compatibility.SetAllowGmailDeviations(true);
        }
        else if (code == EmailIssueCode.MimeTypeAliasRecovery)
        {
            compatibility.SetAllowCommonMimeTypeAliases(true);
        }
        else if (code == EmailIssueCode.EncodedWordCompatibilityRecovery)
        {
            compatibility.SetAllowBrokenEncodedWords(true);
        }
        else if (code == EmailIssueCode.Rfc2231CompatibilityRecovery)
        {
            compatibility.SetAllowBrokenRfc2231Parameters(true);
        }
        else if (code == EmailIssueCode.MimeBoundaryCompatibilityRecovery)
        {
            compatibility.SetAllowBrokenMimeBoundaries(true);
        }
        else
        {
            compatibility.SetAllowLegacyAttachmentRecovery(true);
        }

        options.SetCompatibilityOptions(compatibility);
    }

    static EmailEntityParseResult Parse(string source, EmailParseOptions options)
    {
        return new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.ASCII.GetBytes(source), options);
    }

    static void EnableDecodeOnIngest(EmailParseOptions options)
    {
        EmailBodyIngestOptions ingest = options.GetBodyIngestOptions();
        ingest.SetDecodeOnIngest(true);
        options.SetBodyIngestOptions(ingest);
    }
}
