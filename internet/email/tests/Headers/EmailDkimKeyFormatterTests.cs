using System.Security.Cryptography;
using System.Text;
using email;

namespace email.Tests;

public class EmailDkimKeyFormatterTests
{
    [Test]
    public void CanonicalFormatUsesStableTagOrderAndPreservesUnknownAndRevokedState()
    {
        EmailDkimKey key = Parse("x_z=Two_Words; p=; s=email; k=RSA; v=dkim1; x_a=one");

        EmailValueFormatResult formatted = new EmailDkimKeyFormatter().FormatWithValue(key);
        EmailDkimKey reparsed = Parse(Ascii(formatted.GetBytes()));

        Assert.That(formatted.IsSuccess(), Is.True);
        Assert.That(Ascii(formatted.GetBytes()), Is.EqualTo("v=DKIM1; k=rsa; s=email; p=; x_a=one; x_z=Two_Words"));
        Assert.That(reparsed.IsRevoked(), Is.True);
        Assert.That(reparsed.GetUnknownTags().Select(value => value.GetName()), Is.EqualTo(new[] { "x_a", "x_z" }));
        Assert.That(reparsed.GetUnknownTags().Select(value => value.GetValue()), Is.EqualTo(new[] { "one", "Two_Words" }));
    }

    [Test]
    public void PreservingFormatReturnsExactSafeDnsRecordBytes()
    {
        const string source = "v=DKIM1;\r\n\tk=ed25519; x_extra=Some Value; p=";
        EmailDkimKey key = Parse(source);
        EmailValueFormatOptions options = new EmailValueFormatOptions();
        options.SetMaximumLineLength(998);

        EmailValueFormatResult formatted = new EmailDkimKeyFormatter().FormatPreservingWithValueAndOptions(key, options);

        Assert.That(formatted.IsSuccess(), Is.True);
        Assert.That(formatted.GetBytes(), Is.EqualTo(Bytes(source)));
        Assert.That(formatted.GetFoldCount(), Is.EqualTo(1));
        Assert.That(formatted.GetFinalLineColumn(), Is.EqualTo(34));
    }

    [Test]
    public void LongPublicKeysFoldDeterministicallyAndRoundTrip()
    {
        byte[] publicKey = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
        EmailDkimKey key = Parse("v=DKIM1; k=ed25519; p=" + Convert.ToBase64String(publicKey));
        EmailValueFormatOptions options = new EmailValueFormatOptions();
        options.SetMaximumLineLength(30);
        options.SetFirstLineColumn(5);

        EmailValueFormatResult formatted = key.FormatWithOptions(options);
        string output = Ascii(formatted.GetBytes());
        EmailDkimKey reparsed = Parse(output);

        Assert.That(formatted.IsSuccess(), Is.True);
        Assert.That(formatted.GetFoldCount(), Is.GreaterThanOrEqualTo(2));
        Assert.That(output.Split("\r\n").Select(value => value.Length).Max(), Is.LessThanOrEqualTo(30));
        Assert.That(reparsed.GetAlgorithmKind(), Is.EqualTo(EmailDkimKeyAlgorithmKind.Ed25519));
        Assert.That(reparsed.GetPublicKeyData(), Is.EqualTo(publicKey));
    }

    [Test]
    public void EveryImportedRsaSourceFormatsAsAUsableDnsRecord()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        byte[] spki = rsa.ExportSubjectPublicKeyInfo();
        byte[] pkcs1 = rsa.ExportRSAPublicKey();
        EmailDkimKey[] imported = new[]
        {
            new EmailDkimKeyParser().ImportSubjectPublicKeyInfoWithInput(spki).GetValue()!,
            new EmailDkimKeyParser().ImportRsaPublicKeyWithInput(pkcs1).GetValue()!,
            new EmailDkimKeyParser().ImportPemWithInput(Pem("PUBLIC KEY", spki)).GetValue()!,
            new EmailDkimKeyParser().ImportPemWithInput(Pem("RSA PUBLIC KEY", pkcs1)).GetValue()!
        };

        foreach (EmailDkimKey key in imported)
        {
            EmailValueFormatResult canonical = key.Format();
            EmailValueFormatResult preserving = key.FormatPreserving();
            EmailDkimKey reparsed = Parse(Ascii(canonical.GetBytes()));

            Assert.That(canonical.IsSuccess(), Is.True);
            Assert.That(preserving.GetBytes(), Is.EqualTo(canonical.GetBytes()));
            Assert.That(reparsed.GetAlgorithmKind(), Is.EqualTo(EmailDkimKeyAlgorithmKind.Rsa));
            Assert.That(reparsed.GetPublicKeyData(), Is.EqualTo(key.GetPublicKeyData()));
            Assert.That(reparsed.IsUsable(), Is.True);
        }
    }

    [Test]
    public void FacadeAndModelOperationsProduceTheFormatterResult()
    {
        EmailDkimKey key = Parse("v=DKIM1; p=");
        EmailValueFormatOptions options = new EmailValueFormatOptions();
        options.SetFirstLineColumn(3);

        EmailValueFormatResult direct = new EmailDkimKeyFormatter().FormatWithValueAndOptions(key, options);
        EmailValueFormatResult model = key.FormatWithOptions(options);
        EmailValueFormatResult facade = new EmailValueWriter().WriteDkimKeyWithValueAndOptions(key, options);

        Assert.That(model.GetBytes(), Is.EqualTo(direct.GetBytes()));
        Assert.That(facade.GetBytes(), Is.EqualTo(direct.GetBytes()));
        Assert.That(new EmailValueWriter().WriteDkimKeyWithValue(key).IsSuccess(), Is.True);
    }

    [Test]
    public void InjectionAndOutputLimitsFailWithoutReturningPartialRecords()
    {
        EmailDkimKey unsafeKey = UnsafeKey();
        EmailValueFormatResult canonical = unsafeKey.Format();
        EmailValueFormatResult preserving = unsafeKey.FormatPreserving();
        EmailValueFormatOptions limitedOptions = new EmailValueFormatOptions();
        limitedOptions.SetMaximumOutputBytes(5);
        EmailValueFormatResult limited = Parse("v=DKIM1; p=").FormatWithOptions(limitedOptions);

        Assert.That(canonical.GetBytes(), Is.Empty);
        Assert.That(canonical.GetIssues().ContainsCode(EmailIssueCode.DkimKeyFormattingInvalid), Is.True);
        Assert.That(preserving.GetBytes(), Is.Empty);
        Assert.That(preserving.GetIssues().ContainsCode(EmailIssueCode.DkimKeyFormattingInvalid), Is.True);
        Assert.That(limited.GetBytes(), Is.Empty);
        Assert.That(limited.GetIssues().ContainsCode(EmailIssueCode.DkimKeyOutputLimitExceeded), Is.True);
        Assert.That(limited.HasFatalError(), Is.True);
    }

    static EmailDkimKey UnsafeKey()
    {
        EmailSourcePosition position = new EmailSourcePosition(0, 1, 1);
        string unsafeValue = "safe\r\nX-Evil: yes";
        EmailSignatureTag tag = new EmailSignatureTag("x_note", unsafeValue, unsafeValue, Bytes("x_note=" + unsafeValue), Bytes(unsafeValue), position, false);
        EmailSignatureTagList tags = new EmailSignatureTagList(new List<EmailSignatureTag> { tag }, Bytes("x_note=" + unsafeValue));
        return new EmailDkimKey(tags, Bytes("x_note=" + unsafeValue), position, EmailDkimKeySourceKind.DnsRecord, EmailDkimKeyAlgorithmKind.Unknown, "rsa", Array.Empty<byte>(), null, null, null, 0, false, false, true);
    }

    static EmailDkimKey Parse(string value)
    {
        EmailDkimKeyParseResult result = new EmailDkimKeyParser().ParseWithInputAndMode(Bytes(value), EmailStructuredValueParseMode.Strict);
        Assert.That(result.HasValue(), Is.True, Describe(result.GetIssues()));
        return result.GetValue()!;
    }

    static string Pem(string label, byte[] value)
    {
        return "-----BEGIN " + label + "-----\n" + Convert.ToBase64String(value, Base64FormattingOptions.InsertLineBreaks) + "\n-----END " + label + "-----";
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Ascii(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }

    static string Describe(EmailIssueList issues)
    {
        return string.Join(" | ", issues.GetAll().Select(value => value.GetCode() + ": " + value.GetDescription()));
    }
}
