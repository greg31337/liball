using System.Text;
using email;

namespace email.Tests;

public class EmailCanonicalWriterTests
{
    [Test]
    public void CanonicalMessageSynthesizesMimeHeadersBlankLineAndTerminalCrlf()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("subject", Bytes(" example"), EmailHeaderValueKind.Unstructured));
        EmailMessage message = new EmailMessageOperations().CreateWithHeaders(headers);
        message.SetBody(new EmailEntityFactory().CreateTextWithValue("hello"));

        EmailWriteResult result = new EmailWriter().WriteMessageWithMessageAndOptions(message, new EmailWriteOptionsOperations().CreateCanonicalInternet());
        string output = Text(result.GetBytes());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(output, Does.StartWith("Subject: example\r\n"));
        Assert.That(output, Does.Contain("Content-Type: text/plain; charset=us-ascii\r\n"));
        Assert.That(output, Does.Contain("Content-Transfer-Encoding: 7bit\r\n"));
        Assert.That(output, Does.Contain("MIME-Version: 1.0\r\n"));
        Assert.That(output, Does.EndWith("\r\n\r\nhello\r\n"));
        Assert.That(result.GetCopiedByteCount(), Is.Zero);
        Assert.That(result.GetRegeneratedByteCount(), Is.EqualTo(result.GetByteCount()));
        Assert.That(message.GetHeaders()!.Contains("MIME-Version"), Is.False);
        Assert.That(message.GetHeaders()!.Contains("Content-Type"), Is.False);
    }

    [Test]
    public void CanonicalInternetSelectsQuotedPrintableForUtf8Text()
    {
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValue("hello café");

        EmailWriteResult result = new EmailWriter().WriteEntityWithEntityAndOptions(entity, new EmailWriteOptionsOperations().CreateCanonicalInternet());
        string output = Text(result.GetBytes());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(output, Does.Contain("Content-Transfer-Encoding: quoted-printable\r\n"));
        Assert.That(output, Does.EndWith("\r\n\r\nhello caf=C3=A9\r\n"));
        Assert.That(DecodedBody(result.GetBytes()), Is.EqualTo("hello café\r\n"));
    }

    [Test]
    public void CanonicalSmtpUtf8UsesEightBitForSafeUtf8Text()
    {
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValue("café");

        EmailWriteResult result = new EmailWriter().WriteEntityWithEntityAndOptions(entity, new EmailWriteOptionsOperations().CreateCanonicalSmtpUtf8());
        byte[] output = result.GetBytes();

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(Text(output), Does.Contain("Content-Transfer-Encoding: 8bit\r\n"));
        Assert.That(Text(output), Does.EndWith("café\r\n"));
        Assert.That(DecodedBody(output), Is.EqualTo("café\r\n"));
    }

    [Test]
    public void CanonicalBinaryEntityBase64EncodesDecodedContentOnce()
    {
        MimeEntity entity = new EmailEntityFactory().CreateBinaryWithValue(new byte[] { 0, 255, 1 });

        EmailWriteResult first = new EmailWriter().WriteEntityWithEntity(entity);
        EmailWriteResult second = new EmailWriter().WriteEntityWithEntity(entity);
        string output = Text(first.GetBytes());

        Assert.That(first.IsSuccess(), Is.True);
        Assert.That(first.GetBytes(), Is.EqualTo(second.GetBytes()));
        Assert.That(output, Does.Contain("Content-Type: application/octet-stream\r\n"));
        Assert.That(output, Does.Contain("Content-Transfer-Encoding: base64\r\n"));
        Assert.That(output, Does.EndWith("\r\n\r\nAP8B\r\n"));
        Assert.That(DecodedBytes(first.GetBytes()), Is.EqualTo(new byte[] { 0, 255, 1 }));
    }

    [Test]
    public void CanonicalWritingDecodesAndReencodesExistingBase64()
    {
        MimeEntity entity = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes("Content-Transfer-Encoding: base64\r\n\r\nYQ==\r\n"), new EmailParseOptionsOperations().CreatePreserving()).GetEntity()!;

        EmailWriteResult result = new EmailWriter().WriteEntityWithEntity(entity!);
        string output = Text(result.GetBytes());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(output, Does.Contain("Content-Transfer-Encoding: base64\r\n"));
        Assert.That(output, Does.EndWith("\r\n\r\nYQ==\r\n"));
        Assert.That(DecodedBody(result.GetBytes()), Is.EqualTo("a"));
    }

    [Test]
    public void CanonicalNestedMessageHasIndependentHeadersAndOneTerminalCrlf()
    {
        EmailHeaders nestedHeaders = new EmailHeadersOperations().CreateWith();
        nestedHeaders.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Subject", Bytes(" nested"), EmailHeaderValueKind.Unstructured));
        EmailMessage nested = new EmailMessageOperations().CreateWithHeaders(nestedHeaders);
        nested.SetBody(new EmailEntityFactory().CreateTextWithValue("inside"));
        EmailHeaders outerHeaders = new EmailHeadersOperations().CreateWith();
        outerHeaders.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Subject", Bytes(" outer"), EmailHeaderValueKind.Unstructured));
        EmailMessage outer = new EmailMessageOperations().CreateWithHeaders(outerHeaders);
        outer.SetBody(new EmailEntityFactory().CreateNestedMessage(nested));

        EmailWriteResult result = new EmailWriter().WriteMessageWithMessageAndOptions(outer, new EmailWriteOptionsOperations().CreateCanonicalInternet());
        EmailParseResult reparsed = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(result.GetBytes(), new EmailParseOptionsOperations().CreatePreserving());
        EmailMessage reparsedNested = reparsed.GetMessage()!.GetBody()!.GetNestedMessage()!;

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(Text(result.GetBytes()), Does.Contain("Content-Type: message/rfc822\r\n"));
        Assert.That(Text(result.GetBytes()), Does.EndWith("inside\r\n"));
        Assert.That(Text(result.GetBytes()), Does.Not.EndWith("inside\r\n\r\n"));
        Assert.That(reparsed.IsComplete(), Is.True);
        Assert.That(reparsedNested!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("nested"));
        Assert.That(Text(reparsedNested.GetBody()!.GetRawContent()!.Materialize(100)), Is.EqualTo("inside\r\n"));
    }

    [Test]
    public void EmptyCanonicalEntityEndsAtItsHeaderBodySeparator()
    {
        MimeEntity entity = new EmailEntityFactory().CreateDefaultLeaf(new EmailContentOperations().CreateMemory(new byte[0], EmailContentOwnership.Owned, false));

        EmailWriteResult result = new EmailWriter().WriteEntityWithEntity(entity);
        string output = Text(result.GetBytes());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(output, Does.EndWith("Content-Transfer-Encoding: 7bit\r\n\r\n"));
        Assert.That(output, Does.Not.EndWith("\r\n\r\n\r\n"));
    }

    [Test]
    public void UnknownTransferEncodingMustStillMeetTheTransportConstraint()
    {
        byte[] source = Bytes("Content-Transfer-Encoding: x-custom\r\n\r\nbody");
        byte[] unsafeSource = new byte[source.Length + 1];
        Array.Copy(source, unsafeSource, source.Length);
        unsafeSource[unsafeSource.Length - 1] = 255;
        MimeEntity entity = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(unsafeSource, new EmailParseOptionsOperations().CreatePreserving()).GetEntity()!;

        EmailWriteResult result = new EmailWriter().WriteEntityWithEntityAndOptions(entity!, new EmailWriteOptionsOperations().CreateCanonicalInternet());

        AssertRejected(result);
    }

    [Test]
    public void CanonicalNestedWritingEnforcesDepthAndOutputLimitsWithoutPartialBytes()
    {
        EmailMessage deepest = MessageWithBody(new EmailEntityFactory().CreateTextWithValue("deep"));
        EmailMessage middle = MessageWithBody(new EmailEntityFactory().CreateNestedMessage(deepest));
        EmailMessage outer = MessageWithBody(new EmailEntityFactory().CreateNestedMessage(middle));
        EmailWriteOptions depthOptions = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        depthOptions.SetMaximumNestingDepth(1);
        EmailWriteOptions sizeOptions = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        sizeOptions.SetMaximumOutputBytes(10);

        EmailWriteResult depth = new EmailWriter().WriteMessageWithMessageAndOptions(outer, depthOptions);
        EmailWriteResult size = new EmailWriter().WriteMessageWithMessageAndOptions(MessageWithBody(new EmailEntityFactory().CreateTextWithValue("body")), sizeOptions);

        AssertRejected(depth);
        AssertRejected(size);
    }

    static EmailMessage MessageWithBody(MimeEntity body)
    {
        EmailMessage message = new EmailMessageOperations().CreateWith();
        message.SetBody(body);
        return message;
    }

    static string DecodedBody(byte[] entity)
    {
        return Encoding.UTF8.GetString(DecodedBytes(entity));
    }

    static byte[] DecodedBytes(byte[] entity)
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailBodyIngestOptions ingest = options.GetBodyIngestOptions();
        ingest.SetDecodeOnIngest(true);
        options.SetBodyIngestOptions(ingest);
        MimeEntity parsed = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(entity, options).GetEntity()!;
        return parsed!.GetDecodedContent()!.Materialize(1000);
    }

    static void AssertRejected(EmailWriteResult result)
    {
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetByteCount(), Is.Zero);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.WritePolicyViolation), Is.True);
    }

    static byte[] Bytes(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.UTF8.GetString(value);
    }
}
