using System.Text;
using email;

namespace email.Tests;

public class EmailDkimHeaderCanonicalizerTests
{
    [Test]
    public void SelectsRepeatedFieldsBottomUpAndSkipsTheCurrentSignature()
    {
        EmailHeaders headers = ParseHeaders(
            "Received: A\r\n" +
            "Subject: Topic\r\n" +
            "Received:\tB\r\n" +
            "DKIM-Signature: v=1; b=OLD; h=from\r\n" +
            "Received: C\r\n" +
            "DKIM-Signature: v=1; b=NEW; h=received:received:x-missing:dkim-signature:received\r\n");
        EmailHeader currentSignature = headers.GetAt(5);
        List<string> signedNames = new List<string> { "Received", "received", "X-Missing", "DKIM-Signature", "RECEIVED" };

        EmailDkimHeaderCanonicalizationResult result = new EmailDkimCanonicalizer().CanonicalizeHeadersWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndKind(headers, signedNames, currentSignature, EmailDkimCanonicalizationKind.Simple);

        Assert.That(Ascii(result.GetCanonicalizedHeaders()), Is.EqualTo(
            "Received: C\r\n" +
            "Received:\tB\r\n" +
            "DKIM-Signature: v=1; b=OLD; h=from\r\n" +
            "Received: A\r\n" +
            "DKIM-Signature: v=1; b=; h=received:received:x-missing:dkim-signature:received"));
        Assert.Multiple(() =>
        {
            Assert.That(result.GetSelectionCount(), Is.EqualTo(5));
            Assert.That(result.GetSelection(0).GetSourceIndex(), Is.EqualTo(4));
            Assert.That(result.GetSelection(1).GetSourceIndex(), Is.EqualTo(2));
            Assert.That(result.GetSelection(2).HasHeader(), Is.False);
            Assert.That(result.GetSelection(2).GetSourceIndex(), Is.EqualTo(-1));
            Assert.That(result.GetSelection(3).GetSourceIndex(), Is.EqualTo(3));
            Assert.That(result.GetSelection(4).GetSourceIndex(), Is.Zero);
            Assert.That(result.GetMissingHeaderCount(), Is.EqualTo(1));
            Assert.That(result.WasSignatureValueBlanked(), Is.True);
        });
    }

    [Test]
    public void AppliesRelaxedFieldRulesAfterBlankingTheSignatureValue()
    {
        EmailHeaders headers = ParseHeaders(
            "A: \t X \t \r\n" +
            "B \t: \t Y \t\r\n\t Z  \r\n" +
            "DKIM-Signature: V=1;\r\n\tA=rsa-sha256; bh=KEEP; b = \r\n\tYW Jj \t ; x=Keep \t\r\n");
        EmailHeader signature = headers.GetAt(2);

        EmailDkimHeaderCanonicalizationResult result = new EmailDkimCanonicalizer().CanonicalizeHeadersWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndKind(headers, new List<string> { "A", "b" }, signature, EmailDkimCanonicalizationKind.Relaxed);

        Assert.That(Ascii(result.GetCanonicalizedHeaders()), Is.EqualTo(
            "a:X\r\n" +
            "b:Y Z\r\n" +
            "dkim-signature:V=1; A=rsa-sha256; bh=KEEP; b =; x=Keep"));
        Assert.That(result.GetSelection(1).GetSourceIndex(), Is.EqualTo(1));
    }

    [Test]
    public void SimpleModePreservesEverythingExceptTheCompleteBTagValueAndFinalEnding()
    {
        EmailHeaders headers = ParseHeaders("DKIM-Signature:\tbh=KEEP; B=UNKNOWN; b = \r\n\tYW Jj \t ; x=KEEP \t\r\n");
        EmailHeader signature = headers.GetAt(0);

        EmailDkimHeaderCanonicalizationResult result = new EmailDkimCanonicalizer().CanonicalizeHeadersWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndKind(headers, new List<string>(), signature, EmailDkimCanonicalizationKind.Simple);

        Assert.That(Ascii(result.GetCanonicalizedHeaders()), Is.EqualTo("DKIM-Signature:\tbh=KEEP; B=UNKNOWN; b =; x=KEEP \t"));
        Assert.That(result.GetSourceByteCount(), Is.EqualTo(signature.GetRawFieldBytes().Length));
        Assert.That(result.GetCanonicalizedByteCount(), Is.EqualTo(result.GetCanonicalizedHeaders().LongLength));
    }

    [Test]
    public void UsesSignedNamesAndCanonicalizationFromParsedSignature()
    {
        EmailHeaders headers = ParseHeaders(
            "From: Alice <alice@example.test>\r\n" +
            "Subject:\t Test  \r\n" +
            "DKIM-Signature: v=1; a=rsa-sha256; c=relaxed/simple; d=example.test; s=key; h=From : Subject; bh=AA==; b=BB==\r\n");
        EmailHeader signatureHeader = headers.GetAt(2);
        EmailDkimSignature signature = (EmailDkimSignature)signatureHeader.GetStructuredValue()!;

        EmailDkimHeaderCanonicalizationResult result = new EmailDkimCanonicalizer().CanonicalizeHeadersWithHeadersAndSignatureAndSignatureHeader(headers, signature!, signatureHeader);

        Assert.That(result.GetKind(), Is.EqualTo(EmailDkimCanonicalizationKind.Relaxed));
        Assert.That(Ascii(result.GetCanonicalizedHeaders()), Is.EqualTo(
            "from:Alice <alice@example.test>\r\n" +
            "subject:Test\r\n" +
            "dkim-signature:v=1; a=rsa-sha256; c=relaxed/simple; d=example.test; s=key; h=From : Subject; bh=AA==; b="));
    }

    [Test]
    public void EnforcesLimitsValidatesNamesAndReturnsDefensiveCollections()
    {
        EmailHeaders headers = ParseHeaders("From: one\r\nDKIM-Signature: b=VALUE\r\n");
        EmailHeader signature = headers.GetAt(1);
        EmailDkimHeaderCanonicalizationOptions countOptions = new EmailDkimHeaderCanonicalizationOptions();
        countOptions.SetMaximumSelectedHeaderCount(1);
        EmailDkimHeaderCanonicalizationOptions inputOptions = new EmailDkimHeaderCanonicalizationOptions();
        inputOptions.SetMaximumInputBytes(signature.GetRawFieldBytes().Length - 1);
        EmailDkimHeaderCanonicalizationOptions outputOptions = new EmailDkimHeaderCanonicalizationOptions();
        outputOptions.SetMaximumCanonicalizedBytes(1);

        EmailLimitException count = Assert.Throws<EmailLimitException>(() => new EmailDkimCanonicalizer().CanonicalizeHeadersWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndOptions(headers, new List<string> { "from", "subject" }, signature, countOptions));
        EmailLimitException input = Assert.Throws<EmailLimitException>(() => new EmailDkimCanonicalizer().CanonicalizeHeadersWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndOptions(headers, new List<string>(), signature, inputOptions));
        EmailLimitException output = Assert.Throws<EmailLimitException>(() => new EmailDkimCanonicalizer().CanonicalizeHeadersWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndOptions(headers, new List<string>(), signature, outputOptions));
        Assert.Throws<ArgumentException>(() => new EmailDkimCanonicalizer().CanonicalizeHeadersWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndKind(headers, new List<string> { "bad:name" }, signature, EmailDkimCanonicalizationKind.Simple));
        Assert.Throws<ArgumentOutOfRangeException>(() => countOptions.SetKind(EmailDkimCanonicalizationKind.Extension));

        EmailDkimHeaderCanonicalizationResult result = new EmailDkimCanonicalizer().CanonicalizeHeadersWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndKind(headers, new List<string> { "from" }, signature, EmailDkimCanonicalizationKind.Simple);
        byte[] bytes = result.GetCanonicalizedHeaders();
        bytes[0] ^= 1;
        List<EmailDkimHeaderSelection> selections = result.GetSelections();
        selections.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(count.GetLimitName(), Is.EqualTo("MaxDkimSelectedHeaderCount"));
            Assert.That(input.GetLimitName(), Is.EqualTo("MaxDkimHeaderInputBytes"));
            Assert.That(output.GetLimitName(), Is.EqualTo("MaxDkimCanonicalizedHeaderBytes"));
            Assert.That(Ascii(result.GetCanonicalizedHeaders()), Does.StartWith("From: one"));
            Assert.That(result.GetSelectionCount(), Is.EqualTo(1));
        });
    }

    static EmailHeaders ParseHeaders(string fields)
    {
        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInput(Bytes(fields + "\r\n"));
        Assert.That(result.IsComplete(), Is.True);
        return result.GetHeaders();
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Ascii(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
