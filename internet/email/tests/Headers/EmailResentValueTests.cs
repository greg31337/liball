using System.Text;
using email;

namespace email.Tests;

public class EmailResentValueTests
{
    [Test]
    public void ParsesResentDateWithOriginalSourcePosition()
    {
        EmailMetadataParserOptions options = new EmailMetadataParserOptions();
        options.SetMode(EmailStructuredValueParseMode.Strict);

        EmailMetadataParseResult result = new EmailMetadataParser().ParseResentFieldWithFieldNameAndInputAndOptionsAndInitialPosition("Resent-Date", Bytes("Fri, 21 Nov 1997 09:55:06 -0600"), options, new EmailSourcePosition(100, 5, 9));

        Assert.That(result.IsSuccess(), Is.True);
        EmailResentField value = result.GetResentField()!;
        Assert.That(value!.GetKind(), Is.EqualTo(EmailResentFieldKind.Date));
        Assert.That(value.HasDate(), Is.True);
        Assert.That(value.GetDate()!.GetYear(), Is.EqualTo(1997));
        Assert.That(value.GetDate()!.GetSourcePosition().GetByteOffset(), Is.EqualTo(100));
        Assert.That(Ascii(value.GetRawBytes()), Is.EqualTo("Fri, 21 Nov 1997 09:55:06 -0600"));
    }

    [TestCase("Resent-From", EmailResentFieldKind.From)]
    [TestCase("Resent-To", EmailResentFieldKind.To)]
    [TestCase("Resent-Cc", EmailResentFieldKind.Cc)]
    public void ParsesResentAddressLists(string fieldName, EmailResentFieldKind kind)
    {
        EmailMetadataParseResult result = new EmailMetadataParser().ParseResentFieldWithFieldNameAndInputAndMode(fieldName, Bytes("Alice <alice@example.test>, bob@example.test"), EmailStructuredValueParseMode.Strict);

        EmailResentField value = result.GetResentField()!;
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(value!.GetKind(), Is.EqualTo(kind));
        Assert.That(value.HasAddresses(), Is.True);
        Assert.That(value.GetAddresses()!.GetMailboxCount(), Is.EqualTo(2));
        Assert.That(value.GetAddresses()!.GetAt(0).GetMailbox()!.GetLocalPart(), Is.EqualTo("alice"));
    }

    [Test]
    public void EnforcesSingleMailboxShapeForResentSender()
    {
        EmailMetadataParseResult tolerant = new EmailMetadataParser().ParseResentFieldWithFieldNameAndInputAndMode("Resent-Sender", Bytes("a@example.test, b@example.test"), EmailStructuredValueParseMode.Tolerant);
        EmailMetadataParseResult strict = new EmailMetadataParser().ParseResentFieldWithFieldNameAndInputAndMode("Resent-Sender", Bytes("a@example.test, b@example.test"), EmailStructuredValueParseMode.Strict);

        Assert.That(tolerant.GetResentField()!.GetAddresses()!.GetMailboxCount(), Is.EqualTo(2));
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.ResentFieldMalformed), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void AllowsEmptyResentBccValue()
    {
        EmailMetadataParseResult result = new EmailMetadataParser().ParseResentFieldWithFieldNameAndInputAndMode("Resent-Bcc", Bytes(" (undisclosed) "), EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetResentField()!.HasAddresses(), Is.True);
        Assert.That(result.GetResentField()!.GetAddresses()!.GetCount(), Is.Zero);
        Assert.That(Ascii(result.GetResentField()!.GetRawBytes()), Is.EqualTo(" (undisclosed) "));
    }

    [Test]
    public void ParsesResentMessageId()
    {
        EmailMetadataParseResult result = new EmailMetadataParser().ParseResentFieldWithFieldNameAndInputAndMode("Resent-Message-ID", Bytes("<resent.123@example.test>"), EmailStructuredValueParseMode.Strict);

        EmailResentField value = result.GetResentField()!;
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(value!.GetKind(), Is.EqualTo(EmailResentFieldKind.MessageId));
        Assert.That(value.HasMessageId(), Is.True);
        Assert.That(value.GetMessageId()!.GetIdLeft(), Is.EqualTo("resent.123"));
        Assert.That(value.GetMessageId()!.GetIdRight(), Is.EqualTo("example.test"));
    }

    [Test]
    public void MarksObsoleteResentReplyToWithoutLosingAddresses()
    {
        EmailMetadataParseResult tolerant = new EmailMetadataParser().ParseResentFieldWithFieldNameAndInputAndMode("Resent-Reply-To", Bytes("reply@example.test"), EmailStructuredValueParseMode.Tolerant);
        EmailMetadataParseResult strict = new EmailMetadataParser().ParseResentFieldWithFieldNameAndInputAndMode("Resent-Reply-To", Bytes("reply@example.test"), EmailStructuredValueParseMode.Strict);

        Assert.That(tolerant.GetResentField()!.GetKind(), Is.EqualTo(EmailResentFieldKind.ReplyTo));
        Assert.That(tolerant.GetResentField()!.UsesObsoleteSyntax(), Is.True);
        Assert.That(tolerant.GetResentField()!.GetAddresses()!.GetMailboxCount(), Is.EqualTo(1));
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.ResentFieldObsolete), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void PreservesUnknownResentExtensionField()
    {
        const string raw = "opaque extension bytes";

        EmailMetadataParseResult result = new EmailMetadataParser().ParseResentFieldWithFieldNameAndInputAndMode("Resent-X-Trace", Bytes(raw), EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetResentField()!.GetKind(), Is.EqualTo(EmailResentFieldKind.Extension));
        Assert.That(result.GetResentField()!.GetOriginalFieldName(), Is.EqualTo("Resent-X-Trace"));
        Assert.That(Ascii(result.GetResentField()!.GetRawBytes()), Is.EqualTo(raw));
    }

    [Test]
    public void ResentBlockRetainsOrderAndDuplicateKinds()
    {
        EmailResentField first = new EmailMetadataParser().ParseResentFieldWithFieldNameAndInputAndMode("Resent-To", Bytes("one@example.test"), EmailStructuredValueParseMode.Strict).GetResentField()!;
        EmailResentField second = new EmailMetadataParser().ParseResentFieldWithFieldNameAndInputAndMode("Resent-Cc", Bytes("two@example.test"), EmailStructuredValueParseMode.Strict).GetResentField()!;
        EmailResentField third = new EmailMetadataParser().ParseResentFieldWithFieldNameAndInputAndMode("Resent-To", Bytes("three@example.test"), EmailStructuredValueParseMode.Strict).GetResentField()!;

        EmailResentBlock block = new EmailResentBlock(new List<EmailResentField> { first, second, third }!);

        Assert.That(block.GetCount(), Is.EqualTo(3));
        Assert.That(block.GetAt(1).GetKind(), Is.EqualTo(EmailResentFieldKind.Cc));
        Assert.That(block.GetFirst(EmailResentFieldKind.To), Is.SameAs(first));
        Assert.That(block.GetAllByKind(EmailResentFieldKind.To), Is.EqualTo(new[] { first, third }));
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
