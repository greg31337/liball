using System.Text;
using email;

namespace email.Tests;

public class EmailAdversarialSecurityTests
{
    [Test]
    public void CanonicalHeaderWritingRejectsEncodedCrlfRecipientInjectionWithoutPartialOutput()
    {
        EmailHeaderBlockParseResult parsed = new EmailHeaderBlockParser().ParseWithInput(Bytes(
            "Subject: =?utf-8?Q?safe=0D=0ABcc:_victim@example.test?=\r\n\r\n"));

        EmailHeaderWriteResult written = new EmailHeaderWriter().WriteCanonicalWithHeaders(parsed.GetHeaders());

        Assert.That(written.IsSuccess(), Is.False);
        Assert.That(written.HasFatalError(), Is.True);
        Assert.That(written.GetBytes(), Is.Empty);
        Assert.That(written.GetIssues().ContainsCode(EmailIssueCode.HeaderWriteInjectionRejected), Is.True);
    }

    [Test]
    public void MultipartParserDoesNotTreatAClosingBoundaryWithTrailingPayloadAsADelimiter()
    {
        byte[] source = Bytes(
            "Content-Type: multipart/mixed; boundary=edge\r\n\r\n" +
            "--edge\r\nContent-Type: text/plain\r\n\r\ntrusted\r\n" +
            "--edge-- injected\r\nContent-Type: text/html\r\n\r\nsmuggled\r\n" +
            "--edge--\r\n");

        EmailEntityParseResult parsed = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, new EmailParseOptionsOperations().CreatePreserving());

        Assert.That(parsed.IsComplete(), Is.True);
        Assert.That(parsed.GetEntity()!.GetChildCount(), Is.EqualTo(1));
        string body = Text(parsed.GetEntity()!.GetChildAt(0).GetRawContent()!.Materialize(1000));
        Assert.That(body, Does.Contain("--edge-- injected"));
        Assert.That(body, Does.Contain("Content-Type: text/html"));
        Assert.That(body, Does.Contain("smuggled"));
    }

    [Test]
    public void HtmlSanitizerSuppressesNamespacedActiveTreesAndEntityObfuscatedSchemes()
    {
        string html = "before<svg><script>alert(1)</script><a href='javascript:x'>svg link</a></svg>" +
            "<a href='&#x6a;avascript:alert(2)'>click</a>" +
            "<img src='java&#x09;script:alert(3)' srcset='https://tracker.test/x 1x' onerror='alert(4)'>after";

        EmailHtmlSanitizeResult sanitized = new EmailHtmlProcessor().SanitizeWithHtml(html);

        Assert.That(sanitized.GetHtml(), Is.EqualTo("before<a>click</a><img>after"));
        Assert.That(sanitized.GetHtml(), Does.Not.Contain("script").And.Not.Contain("tracker").And.Not.Contain("onerror"));
        Assert.That(sanitized.GetBlockedUnsafeUrlCount(), Is.EqualTo(2));
        Assert.That(sanitized.GetIssues().ContainsCode(EmailIssueCode.HtmlUnsafeUrlBlocked), Is.True);
    }

    [Test]
    public void CorruptMboxEnvelopeLikeBodyLineCannotRemainHiddenInsideThePriorMessage()
    {
        byte[] source = Bytes(
            "From sender@example.test Fri Nov 21 09:55:06 1997\nSubject: first\n\ntrusted body\n" +
            "From forged-envelope\nSubject: attacker\n\nsmuggled body\n");
        EmailMboxReader reader = new EmailMboxReaderOperations().CreateWithInput(new MemoryStream(source, false));

        Assert.That(reader.ReadNext(), Is.True);
        EmailMboxEntry trusted = reader.GetCurrent()!;
        Assert.That(Text(trusted!.GetMessage()!.GetBody()!.GetRawContent()!.Materialize(1000)), Is.EqualTo("trusted body\n"));
        Assert.That(reader.ReadNext(), Is.True);
        EmailMboxEntry recovered = reader.GetCurrent()!;

        Assert.That(recovered!.GetIssues().ContainsCode(EmailIssueCode.MboxEnvelopeMalformed), Is.True);
        Assert.That(recovered.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("attacker"));
        Assert.That(reader.GetEntryCount(), Is.EqualTo(2));
        reader.Close();
    }

    [Test]
    public void CmsAndOpenPgpBombShapesStopAtStructuralBudgets()
    {
        byte[] nested = new byte[] { 0x05, 0x00 };
        int depth = 0;
        while (depth < 12)
        {
            nested = new byte[] { 0x30, (byte)nested.Length }.Concat(nested).ToArray();
            depth++;
        }

        EmailCmsReaderOptions cmsOptions = new EmailCmsReaderOptions();
        EmailCryptographicLimits cmsLimits = cmsOptions.GetLimits();
        cmsLimits.SetMaxAsn1Depth(4);
        cmsOptions.SetLimits(cmsLimits);
        EmailLimitException asn1 = Assert.Throws<EmailLimitException>(() => new EmailCmsReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(nested, cmsOptions));

        byte[] emptyUserIdPackets = Enumerable.Range(0, 12).SelectMany(_ => new byte[] { 0xcd, 0x00 }).ToArray();
        EmailOpenPgpReaderOptions pgpOptions = new EmailOpenPgpReaderOptions();
        EmailCryptographicLimits pgpLimits = pgpOptions.GetLimits();
        pgpLimits.SetMaxOpenPgpPacketCount(4);
        pgpOptions.SetLimits(pgpLimits);
        EmailLimitException packets = Assert.Throws<EmailLimitException>(() => new EmailOpenPgpReader().ParseWithInputAndOptions(emptyUserIdPackets, pgpOptions));

        Assert.That(asn1.GetLimitName(), Is.EqualTo("MaxAsn1Depth"));
        Assert.That(packets.GetLimitName(), Is.EqualTo("MaxOpenPgpPacketCount"));
        Assert.That(packets.GetObserved(), Is.EqualTo(5));
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
