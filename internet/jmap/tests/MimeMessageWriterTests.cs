using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class MimeMessageWriterTests
{
    [Test]
    public void WritesStructuredDraftWithHeadersTextAndUploadedBinaryParts()
    {
        Email email = Draft();
        email.MessageId = new List<string> { "draft@example.test" };
        email.From = new List<EmailAddress>
        {
            new EmailAddress { Name = "Joséphine", Email = "josephine@example.test" }
        };
        email.Subject = "Résumé";
        email.SentAt = "2026-09-11T14:05:06-07:00";
        email.HeaderProperties["header:X-Trace:asText:all"] = Json("[\"one\",\"two\"]");

        EmailBodyPart plain = TextPart("plain", "text/plain");
        plain.Language = new List<string> { "en-US" };
        EmailBodyPart html = TextPart("html", "text/html");
        EmailBodyPart alternative = Multipart("multipart/alternative", plain, html);

        EmailBodyPart attachment = new EmailBodyPart();
        attachment.Type = "application/octet-stream";
        attachment.BlobId = "uploaded-1";
        attachment.Name = "café.bin";
        attachment.Disposition = "attachment";
        attachment.Cid = "file@example.test";
        attachment.Location = "files/cafe.bin";
        attachment.HeaderProperties["header:X-Part"] = Json("\"metadata\"");

        email.BodyStructure = Multipart("multipart/mixed", alternative, attachment);
        email.BodyValues = new Dictionary<string, EmailBodyValue>
        {
            ["plain"] = BodyValue("hello\nworld"),
            ["html"] = BodyValue("<p>hello</p>")
        };
        byte[] uploaded = { 0, 1, 2, 253, 254, 255 };

        MimeWriteResult written = new MimeMessageWriter().WriteMessage(email,
            new Dictionary<string, byte[]> { ["uploaded-1"] = uploaded });
        MimeMessageReadResult parsed = new MimeMessageReader().ReadMessage(written.Message);
        MimeEntity root = parsed.Root!;
        MimeEntity parsedAlternative = root.SubParts![0];
        MimeEntity parsedAttachment = root.SubParts[1];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(written.MissingBlobIds, Is.Empty);
            Assert.That(parsed.HasMalformedInput, Is.False);
            Assert.That(root.Type, Is.EqualTo("multipart/mixed"));
            Assert.That(parsedAlternative.Type, Is.EqualTo("multipart/alternative"));
            Assert.That(parsedAlternative.SubParts![0].DecodedText, Is.EqualTo("hello\r\nworld"));
            Assert.That(parsedAlternative.SubParts[0].Charset, Is.EqualTo("utf-8"));
            Assert.That(parsedAlternative.SubParts[0].Language, Is.EqualTo(new[] { "en-US" }));
            Assert.That(parsedAlternative.SubParts[1].DecodedText, Is.EqualTo("<p>hello</p>"));
            Assert.That(parsedAttachment.DecodedBody, Is.EqualTo(uploaded));
            Assert.That(parsedAttachment.Name, Is.EqualTo("café.bin"));
            Assert.That(parsedAttachment.Disposition, Is.EqualTo("attachment"));
            Assert.That(parsedAttachment.ContentId, Is.EqualTo("file@example.test"));
            Assert.That(parsedAttachment.Location, Is.EqualTo("files/cafe.bin"));
            Assert.That(parsedAttachment.Headers.Any(header => header.Name == "X-Part" && header.Value == " metadata"),
                Is.True);
            Assert.That(parsed.Headers.Count(header => string.Equals(header.Name, "X-Trace", StringComparison.OrdinalIgnoreCase)),
                Is.EqualTo(2));
            Assert.That(parsed.Headers.Any(header => header.Name == "From" && header.Value!.Contains("Joséphine")), Is.True);
            Assert.That(parsed.Headers.Any(header => header.Name == "Subject" && header.Value == " Résumé"), Is.True);
            Assert.That(parsed.Headers.Any(header => header.Name == "Message-ID" && header.Value == " <draft@example.test>"),
                Is.True);
            Assert.That(parsed.Headers.Any(header => header.Name == "Date" &&
                header.Value == " Fri, 11 Sep 2026 14:05:06 -0700"), Is.True);
            Assert.That(parsed.Headers.Any(header => header.Name == "MIME-Version" && header.Value == " 1.0"), Is.True);
        }
    }

    [Test]
    public void BuildsSimpleTextHtmlAndAttachmentShape()
    {
        Email email = Draft();
        email.TextBody = new List<EmailBodyPart> { TextPart("text", "text/plain") };
        email.HtmlBody = new List<EmailBodyPart> { TextPart("html", "text/html") };
        EmailBodyPart attachment = new EmailBodyPart();
        attachment.Type = "image/png";
        attachment.BlobId = "image-1";
        attachment.Name = "pixel.png";
        email.Attachments = new List<EmailBodyPart> { attachment };
        email.BodyValues = new Dictionary<string, EmailBodyValue>
        {
            ["text"] = BodyValue("plain"),
            ["html"] = BodyValue("<b>html</b>")
        };

        MimeWriteResult written = new MimeMessageWriter().WriteMessage(email,
            new Dictionary<string, byte[]> { ["image-1"] = new byte[] { 137, 80, 78, 71 } });
        MimeMessageReadResult parsed = new MimeMessageReader().ReadMessage(written.Message);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.Root!.Type, Is.EqualTo("multipart/mixed"));
            Assert.That(parsed.Root.SubParts, Has.Count.EqualTo(2));
            Assert.That(parsed.Root.SubParts![0].Type, Is.EqualTo("multipart/alternative"));
            Assert.That(parsed.Root.SubParts[0].SubParts!.Select(part => part.Type),
                Is.EqualTo(new[] { "text/plain", "text/html" }));
            Assert.That(parsed.Root.SubParts[1].Type, Is.EqualTo("image/png"));
            Assert.That(parsed.Root.SubParts[1].Name, Is.EqualTo("pixel.png"));
        }
    }

    [Test]
    public void GeneratesRequiredMessageIdDateAndEmptyPlainBody()
    {
        Email email = Draft();
        MimeMessageWriter writer = new MimeMessageWriter();
        writer.GeneratedMessageId = "generated@example.test";
        writer.GeneratedDate = "Fri, 11 Sep 2026 21:05:06 +0000";

        MimeWriteResult written = writer.WriteMessage(email, new Dictionary<string, byte[]>());
        MimeMessageReadResult parsed = new MimeMessageReader().ReadMessage(written.Message);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.Root!.Type, Is.EqualTo("text/plain"));
            Assert.That(parsed.Root.Charset, Is.EqualTo("us-ascii"));
            Assert.That(parsed.Root.DecodedText, Is.EqualTo(""));
            Assert.That(parsed.Headers.Any(header => header.Name == "Message-ID" &&
                header.Value == " <generated@example.test>"), Is.True);
            Assert.That(parsed.Headers.Any(header => header.Name == "Date" &&
                header.Value == " Fri, 11 Sep 2026 21:05:06 +0000"), Is.True);
        }
    }

    [Test]
    public void ReportsEveryMissingUploadedBlobWithoutProducingMessage()
    {
        Email email = Draft();
        EmailBodyPart first = new EmailBodyPart { Type = "application/pdf", BlobId = "missing-1" };
        EmailBodyPart duplicate = new EmailBodyPart { Type = "image/png", BlobId = "missing-1" };
        EmailBodyPart second = new EmailBodyPart { Type = "audio/mpeg", BlobId = "missing-2" };
        email.Attachments = new List<EmailBodyPart> { first, duplicate, second };

        MimeWriteResult written = new MimeMessageWriter().WriteMessage(email,
            new Dictionary<string, byte[]> { ["available"] = new byte[] { 1 } });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(written.MissingBlobIds, Is.EqualTo(new[] { "missing-1", "missing-2" }));
            Assert.That(written.Message, Is.Empty);
        }
    }

    [Test]
    public void EnforcesStructuredAndSimpleBodyMutualExclusion()
    {
        Email email = Draft();
        email.BodyStructure = TextPart("structured", "text/plain");
        email.TextBody = new List<EmailBodyPart> { TextPart("simple", "text/plain") };
        email.BodyValues = new Dictionary<string, EmailBodyValue>
        {
            ["structured"] = BodyValue("one"),
            ["simple"] = BodyValue("two")
        };

        JmapValidationException exception = Assert.Throws<JmapValidationException>(() =>
            new MimeMessageWriter().WriteMessage(email, new Dictionary<string, byte[]>()))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.InvalidProperties, Does.Contain("bodyStructure"));
            Assert.That(exception.InvalidProperties, Does.Contain("textBody"));
        }
    }

    [Test]
    public void RejectsAmbiguousPartsAndUserTransferEncoding()
    {
        Email email = Draft();
        EmailBodyPart part = TextPart("value", "text/plain");
        part.BlobId = "blob-1";
        part.HeaderProperties["header:Content-Transfer-Encoding"] = Json("\"quoted-printable\"");
        email.BodyStructure = part;
        email.BodyValues = new Dictionary<string, EmailBodyValue> { ["value"] = BodyValue("body") };

        JmapValidationException exception = Assert.Throws<JmapValidationException>(() =>
            new MimeMessageWriter().WriteMessage(email,
                new Dictionary<string, byte[]> { ["blob-1"] = new byte[] { 1 } }))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.InvalidProperties, Does.Contain("bodyStructure/partId"));
            Assert.That(exception.InvalidProperties, Does.Contain("bodyStructure/blobId"));
            Assert.That(exception.InvalidProperties,
                Does.Contain("bodyStructure/header:Content-Transfer-Encoding"));
        }
    }

    [Test]
    public void RejectsHeaderInjectionBeforeWritingBytes()
    {
        Email email = Draft();
        email.Subject = "safe\r\nBcc: injected@example.test";

        JmapValidationException exception = Assert.Throws<JmapValidationException>(() =>
            new MimeMessageWriter().WriteMessage(email, new Dictionary<string, byte[]>()))!;

        Assert.That(exception.InvalidProperties, Does.Contain("subject"));
    }

    [Test]
    public void RejectsInvalidUnicodeBodyContentInsteadOfReplacingIt()
    {
        Email email = Draft();
        email.BodyStructure = TextPart("text", "text/plain");
        email.BodyValues = new Dictionary<string, EmailBodyValue>
        {
            ["text"] = BodyValue("broken \ud800 value")
        };

        JmapValidationException exception = Assert.Throws<JmapValidationException>(() =>
            new MimeMessageWriter().WriteMessage(email, new Dictionary<string, byte[]>()))!;

        Assert.That(exception.InvalidProperties, Does.Contain("bodyValues/text/value"));
    }

    private static Email Draft()
    {
        return new Email
        {
            MailboxIds = new Dictionary<string, bool> { ["drafts"] = true },
            Keywords = new Dictionary<string, bool>()
        };
    }

    private static EmailBodyPart TextPart(string partId, string type)
    {
        return new EmailBodyPart { PartId = partId, Type = type };
    }

    private static EmailBodyPart Multipart(string type, params EmailBodyPart[] parts)
    {
        return new EmailBodyPart { Type = type, SubParts = new List<EmailBodyPart>(parts) };
    }

    private static EmailBodyValue BodyValue(string value)
    {
        return new EmailBodyValue { Value = value };
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
