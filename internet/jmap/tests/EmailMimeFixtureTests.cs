using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class EmailMimeFixtureTests
{
    [Test]
    public void PlainInternationalizedFixturePopulatesEveryEmailAndLeafBodyField()
    {
        string message =
            "Received: by mx.example.test\r\n" +
            "X-Trace: first\r\n" +
            "X-Trace: second\r\n" +
            "Message-ID: <message@example.test>\r\n" +
            "In-Reply-To: <parent@example.test>\r\n" +
            "References: <root@example.test> <parent@example.test>\r\n" +
            "Sender: =?UTF-8?Q?Ren=C3=A9?= <rene@example.test>\r\n" +
            "From: Authors: =?UTF-8?B?5pel5pys6Kqe?= <nihon@example.test>, " +
            "δοκιμή@παράδειγμα.δοκιμή;\r\n" +
            "To: Recipient <recipient@example.test>\r\n" +
            "Cc: Copy <copy@example.test>\r\n" +
            "Bcc:\r\n" +
            "Reply-To: Replies <reply@example.test>\r\n" +
            "Subject: =?UTF-8?B?5pel5pys6Kqe?=\r\n" +
            "Date: Fri, 11 Sep 2026 14:05:06 -0700\r\n" +
            "List-Help: <https://lists.example.test/help>, " +
            "<mailto:list@example.test?subject=help>\r\n" +
            "Content-Type: text/plain; charset=utf-8; name=ignored.txt\r\n" +
            "Content-Disposition: inline; filename*=utf-8''note-%E2%98%83.txt\r\n" +
            "Content-ID: <body@example.test>\r\n" +
            "Content-Language: en-US, ja\r\n" +
            "Content-Location: notes/note-%E2%98%83.txt\r\n" +
            "Content-Transfer-Encoding: 8bit\r\n" +
            "\r\n" +
            "Hello, 日本語!\r\nSecond line.";
        byte[] bytes = Encoding.UTF8.GetBytes(message);
        MimeMessageReadResult parsed = new MimeMessageReader().ReadMessage(bytes);
        BodyPartMappingResult body = new BodyPartMapper().MapMessage(parsed, 0);
        HeaderPropertySelector headers = new HeaderPropertySelector();
        Email source = StoredEmail(bytes, parsed, body);
        headers.DeriveConvenienceProperties(source, parsed.Headers);
        AddSelectedHeader(source, headers, "header:X-Trace:asRaw:all");
        AddSelectedHeader(source, headers, "header:Subject:asText");
        AddSelectedHeader(source, headers, "header:To:asAddresses");
        AddSelectedHeader(source, headers, "header:From:asGroupedAddresses");
        AddSelectedHeader(source, headers, "header:References:asMessageIds");
        AddSelectedHeader(source, headers, "header:Date:asDate");
        AddSelectedHeader(source, headers, "header:List-Help:asURLs");
        source.UnknownProperties["vendorScore"] = Json("0.75");
        source.Headers![0].UnknownProperties["vendorHeader"] = Json("true");
        source.Sender![0].UnknownProperties["vendorAddress"] = Json("7");
        source.BodyStructure!.HeaderProperties["header:Content-Type:asRaw"] =
            headers.SelectProperty(source.BodyStructure.Headers, "header:Content-Type:asRaw");
        source.BodyStructure.UnknownProperties["vendorPart"] = Json("\"leaf\"");
        source.BodyValues!["1"].UnknownProperties["vendorBody"] = Json("false");

        Email email = RoundTrip(source);
        EmailBodyPart part = email.BodyStructure!;
        EmailBodyValue value = email.BodyValues!["1"];
        List<EmailAddressGroup> groups = new AddressHeaderParser().ParseGroupedAddresses(
            parsed.Headers.Single(header => header.Name == "From").Value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.HasMalformedInput, Is.False);
            Assert.That(parsed.IsLimitExceeded, Is.False);
            Assert.That(email.Id, Is.EqualTo("email-1"));
            Assert.That(email.BlobId, Is.EqualTo("message-blob"));
            Assert.That(email.ThreadId, Is.EqualTo("thread-1"));
            Assert.That(email.MailboxIds, Is.EqualTo(new Dictionary<string, bool>
            {
                ["archive"] = false,
                ["inbox"] = true
            }));
            Assert.That(email.Keywords, Is.EqualTo(new Dictionary<string, bool>
            {
                [JmapMailKeywords.Flagged] = false,
                [JmapMailKeywords.Seen] = true
            }));
            Assert.That(email.Size, Is.EqualTo(bytes.LongLength));
            Assert.That(email.ReceivedAt, Is.EqualTo("2026-09-11T21:06:00Z"));
            Assert.That(email.Headers![0].Name, Is.EqualTo("Received"));
            Assert.That(email.Headers[0].Value, Is.EqualTo(" by mx.example.test"));
            Assert.That(email.Headers[0].UnknownProperties["vendorHeader"].GetBoolean(), Is.True);
            Assert.That(email.HeaderProperties["header:X-Trace:asRaw:all"]
                .EnumerateArray().Select(item => item.GetString()),
                Is.EqualTo(new[] { " first", " second" }));
            Assert.That(email.HeaderProperties["header:Subject:asText"].GetString(),
                Is.EqualTo("日本語"));
            Assert.That(email.HeaderProperties["header:To:asAddresses"][0]
                .GetProperty("email").GetString(), Is.EqualTo("recipient@example.test"));
            Assert.That(email.HeaderProperties["header:From:asGroupedAddresses"][0]
                .GetProperty("name").GetString(), Is.EqualTo("Authors"));
            Assert.That(email.HeaderProperties["header:References:asMessageIds"]
                .EnumerateArray().Select(item => item.GetString()),
                Is.EqualTo(new[] { "root@example.test", "parent@example.test" }));
            Assert.That(email.HeaderProperties["header:Date:asDate"].GetString(),
                Is.EqualTo("2026-09-11T14:05:06-07:00"));
            Assert.That(email.HeaderProperties["header:List-Help:asURLs"]
                .EnumerateArray().Select(item => item.GetString()), Is.EqualTo(new[]
                {
                    "https://lists.example.test/help",
                    "mailto:list@example.test?subject=help"
                }));
            Assert.That(email.MessageId, Is.EqualTo(new[] { "message@example.test" }));
            Assert.That(email.InReplyTo, Is.EqualTo(new[] { "parent@example.test" }));
            Assert.That(email.References,
                Is.EqualTo(new[] { "root@example.test", "parent@example.test" }));
            Assert.That(email.Sender!.Single().Name, Is.EqualTo("René"));
            Assert.That(email.Sender![0].Email, Is.EqualTo("rene@example.test"));
            Assert.That(email.Sender![0].UnknownProperties["vendorAddress"].GetInt32(),
                Is.EqualTo(7));
            Assert.That(email.From!.Select(address => address.Email), Is.EqualTo(new[]
            {
                "nihon@example.test", "δοκιμή@παράδειγμα.δοκιμή"
            }));
            Assert.That(email.To!.Single().Name, Is.EqualTo("Recipient"));
            Assert.That(email.Cc!.Single().Email, Is.EqualTo("copy@example.test"));
            Assert.That(email.Bcc, Is.Empty);
            Assert.That(email.ReplyTo!.Single().Email, Is.EqualTo("reply@example.test"));
            Assert.That(email.Subject, Is.EqualTo("日本語"));
            Assert.That(email.SentAt, Is.EqualTo("2026-09-11T14:05:06-07:00"));
            Assert.That(email.TextBody!.Single().PartId, Is.EqualTo("1"));
            Assert.That(email.HtmlBody!.Single().PartId, Is.EqualTo("1"));
            Assert.That(email.Attachments, Is.Empty);
            Assert.That(email.HasAttachment, Is.False);
            Assert.That(email.Preview, Is.EqualTo("Hello, 日本語! Second line."));
            Assert.That(email.UnknownProperties["vendorScore"].GetDouble(), Is.EqualTo(0.75));
            Assert.That(groups.Single().Name, Is.EqualTo("Authors"));
            Assert.That(groups[0].Addresses[0].Name, Is.EqualTo("日本語"));
            Assert.That(groups[0].Addresses[1].Email,
                Is.EqualTo("δοκιμή@παράδειγμα.δοκιμή"));
            Assert.That(groups[0].UnknownProperties, Is.Empty);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(part.PartId, Is.EqualTo("1"));
            Assert.That(part.BlobId, Does.StartWith("m"));
            Assert.That(part.Size,
                Is.EqualTo(Encoding.UTF8.GetByteCount("Hello, 日本語!\r\nSecond line.")));
            Assert.That(part.HasSize, Is.True);
            Assert.That(part.Headers!.Select(header => header.Name), Does.Contain("Content-Type"));
            Assert.That(part.Name, Is.EqualTo("note-☃.txt"));
            Assert.That(part.Type, Is.EqualTo("text/plain"));
            Assert.That(part.Charset, Is.EqualTo("utf-8"));
            Assert.That(part.Disposition, Is.EqualTo("inline"));
            Assert.That(part.Cid, Is.EqualTo("body@example.test"));
            Assert.That(part.Language, Is.EqualTo(new[] { "en-US", "ja" }));
            Assert.That(part.Location, Is.EqualTo("notes/note-%E2%98%83.txt"));
            Assert.That(part.SubParts, Is.Null);
            Assert.That(part.HeaderProperties["header:Content-Type:asRaw"].GetString(),
                Does.Contain("text/plain"));
            Assert.That(part.UnknownProperties["vendorPart"].GetString(), Is.EqualTo("leaf"));
            Assert.That(value.Value, Is.EqualTo("Hello, 日本語!\nSecond line."));
            Assert.That(value.IsEncodingProblem, Is.False);
            Assert.That(value.IsTruncated, Is.False);
            Assert.That(value.UnknownProperties["vendorBody"].GetBoolean(), Is.False);
        }
    }

    [Test]
    public void NestedFixtureMapsAlternativeRelatedAttachmentsAndEmbeddedMessage()
    {
        string message =
            "Content-Type: multipart/mixed; boundary=outer\r\n\r\n" +
            "--outer\r\nContent-Type: multipart/alternative; boundary=alternative\r\n\r\n" +
            "--alternative\r\nContent-Type: text/plain\r\n\r\nplain choice\r\n" +
            "--alternative\r\nContent-Type: text/html\r\n\r\n<p>html choice</p>\r\n" +
            "--alternative--\r\n" +
            "--outer\r\nContent-Type: multipart/related; boundary=related\r\n\r\n" +
            "--related\r\nContent-Type: text/html\r\n\r\n" +
            "<img src=\"cid:logo@example.test\">\r\n" +
            "--related\r\nContent-Type: image/png\r\nContent-Disposition: inline\r\n" +
            "Content-ID: <logo@example.test>\r\nContent-Location: images/logo.png\r\n" +
            "Content-Transfer-Encoding: base64\r\n\r\nAAEC\r\n" +
            "--related--\r\n" +
            "--outer\r\nContent-Type: application/pdf\r\n" +
            "Content-Disposition: attachment; filename=report.pdf\r\n\r\nPDF\r\n" +
            "--outer\r\nContent-Type: message/rfc822\r\n\r\n" +
            "From: Nested <nested@example.test>\r\nSubject: embedded\r\n" +
            "Content-Type: text/plain\r\n\r\nnested body\r\n" +
            "--outer--\r\n";

        MimeMessageReadResult parsed = new MimeMessageReader().ReadMessage(
            Encoding.ASCII.GetBytes(message));
        BodyPartMappingResult mapped = new BodyPartMapper().MapMessage(parsed, 0);
        Email email = RoundTrip(StoredEmail(Encoding.ASCII.GetBytes(message), parsed, mapped));
        EmailBodyPart root = email.BodyStructure!;
        EmailBodyPart alternative = root.SubParts![0];
        EmailBodyPart related = root.SubParts[1];
        MimeMessageReadResult embedded = parsed.Root!.SubParts![3].EmbeddedMessage!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.HasMalformedInput, Is.False);
            Assert.That(root.Type, Is.EqualTo("multipart/mixed"));
            Assert.That(root.PartId, Is.Null);
            Assert.That(root.BlobId, Is.Null);
            Assert.That(root.HasSize, Is.True);
            Assert.That(root.SubParts, Has.Count.EqualTo(4));
            Assert.That(alternative.Type, Is.EqualTo("multipart/alternative"));
            Assert.That(alternative.SubParts!.Select(part => part.PartId),
                Is.EqualTo(new[] { "1.1", "1.2" }));
            Assert.That(related.Type, Is.EqualTo("multipart/related"));
            Assert.That(related.SubParts![0].PartId, Is.EqualTo("2.1"));
            Assert.That(related.SubParts[1].PartId, Is.EqualTo("2.2"));
            Assert.That(related.SubParts[1].Cid, Is.EqualTo("logo@example.test"));
            Assert.That(related.SubParts[1].Location, Is.EqualTo("images/logo.png"));
            Assert.That(related.SubParts[1].Size, Is.EqualTo(3));
            Assert.That(root.SubParts[2].Name, Is.EqualTo("report.pdf"));
            Assert.That(root.SubParts[2].Disposition, Is.EqualTo("attachment"));
            Assert.That(root.SubParts[3].Type, Is.EqualTo("message/rfc822"));
            Assert.That(root.SubParts[3].PartId, Is.EqualTo("4"));
            Assert.That(root.SubParts[3].SubParts, Is.Null);
            Assert.That(embedded.Headers.Select(header => header.Name),
                Is.EqualTo(new[] { "From", "Subject", "Content-Type" }));
            Assert.That(embedded.Root!.DecodedText, Is.EqualTo("nested body"));
            Assert.That(email.TextBody!.Select(part => part.PartId),
                Is.EqualTo(new[] { "1.1", "2.1" }));
            Assert.That(email.HtmlBody!.Select(part => part.PartId),
                Is.EqualTo(new[] { "1.2", "2.1" }));
            Assert.That(email.Attachments!.Select(part => part.PartId),
                Is.EqualTo(new[] { "2.2", "3", "4" }));
            Assert.That(email.BodyValues!.Keys,
                Is.EquivalentTo(new[] { "1.1", "1.2", "2.1" }));
            Assert.That(email.HasAttachment, Is.True);
            Assert.That(email.Preview, Is.EqualTo("plain choice"));
        }
    }

    [Test]
    public void MalformedFixtureRetainsBestEffortFieldsAndNullsInvalidStructuredHeaders()
    {
        string message =
            "Subject: =?UTF-8?Q?bad=GG?=\r\n" +
            "Message-ID: not-an-id\r\n" +
            "Date: 31 Feb 2026 14:05:06 +0000\r\n" +
            "List-Help: NO\r\n" +
            "From: Draft: <unfinished@example.test\r\n" +
            "Broken header line\r\n" +
            "Content-Type: text/plain; charset=x-unknown\r\n" +
            "Content-Transfer-Encoding: base64\r\n\r\nwyg=";
        MimeMessageReadResult parsed = new MimeMessageReader().ReadMessage(
            Encoding.ASCII.GetBytes(message));
        BodyPartMappingResult mapped = new BodyPartMapper().MapMessage(parsed, 0);
        HeaderPropertySelector selector = new HeaderPropertySelector();
        Email email = StoredEmail(Encoding.ASCII.GetBytes(message), parsed, mapped);
        selector.DeriveConvenienceProperties(email, parsed.Headers);
        AddSelectedHeader(email, selector, "header:List-Help:asURLs");
        email = RoundTrip(email);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.HasMalformedInput, Is.True);
            Assert.That(parsed.IsLimitExceeded, Is.False);
            Assert.That(parsed.Headers.Any(header => header.Name == "Broken header line"),
                Is.False);
            Assert.That(email.Subject, Is.EqualTo("�"));
            Assert.That(email.MessageId, Is.Null);
            Assert.That(email.SentAt, Is.Null);
            Assert.That(email.From!.Single().Email, Is.EqualTo("unfinished@example.test"));
            Assert.That(email.HeaderProperties["header:List-Help:asURLs"].ValueKind,
                Is.EqualTo(JsonValueKind.Null));
            Assert.That(email.BodyStructure!.Charset, Is.EqualTo("x-unknown"));
            Assert.That(email.BodyValues!["1"].Value, Is.EqualTo("�("));
            Assert.That(email.BodyValues["1"].IsEncodingProblem, Is.True);
            Assert.That(email.BodyValues["1"].IsTruncated, Is.False);
        }
    }

    [Test]
    public void TruncatedFixturePropagatesPartAndBodyValueLimits()
    {
        string message =
            "Content-Type: text/plain; charset=us-ascii\r\n" +
            "Content-Transfer-Encoding: base64\r\n\r\n" +
            Convert.ToBase64String(Encoding.ASCII.GetBytes("first second"));
        MimeMessageReader reader = new MimeMessageReader();
        reader.Limits.MaximumDecodedPartBytes = 5;
        MimeMessageReadResult parsed = reader.ReadMessage(Encoding.ASCII.GetBytes(message));
        BodyPartMappingResult mapped = new BodyPartMapper().MapMessage(parsed, 0);
        Email email = RoundTrip(StoredEmail(Encoding.ASCII.GetBytes(message), parsed, mapped));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.HasMalformedInput, Is.False);
            Assert.That(parsed.IsLimitExceeded, Is.True);
            Assert.That(parsed.Root!.IsTruncated, Is.True);
            Assert.That(email.BodyStructure!.Size, Is.EqualTo(5));
            Assert.That(email.BodyValues!["1"].Value, Is.EqualTo("first"));
            Assert.That(email.BodyValues["1"].IsEncodingProblem, Is.False);
            Assert.That(email.BodyValues["1"].IsTruncated, Is.True);
            Assert.That(email.Preview, Is.EqualTo("first"));
        }
    }

    private static Email StoredEmail(
        byte[] bytes,
        MimeMessageReadResult parsed,
        BodyPartMappingResult body)
    {
        return new Email
        {
            Id = "email-1",
            BlobId = "message-blob",
            ThreadId = "thread-1",
            MailboxIds = new Dictionary<string, bool>
            {
                ["inbox"] = true,
                ["archive"] = false
            },
            Keywords = new Dictionary<string, bool>
            {
                [JmapMailKeywords.Seen] = true,
                [JmapMailKeywords.Flagged] = false
            },
            Size = bytes.LongLength,
            ReceivedAt = "2026-09-11T21:06:00Z",
            Headers = parsed.Headers,
            BodyStructure = body.BodyStructure,
            BodyValues = body.BodyValues,
            TextBody = body.TextBody,
            HtmlBody = body.HtmlBody,
            Attachments = body.Attachments,
            HasAttachment = body.HasAttachment,
            Preview = body.Preview
        };
    }

    private static void AddSelectedHeader(
        Email email,
        HeaderPropertySelector selector,
        string property)
    {
        email.HeaderProperties[property] = selector.SelectProperty(email.Headers, property);
    }

    private static Email RoundTrip(Email email)
    {
        JmapJsonReader reader = new JmapJsonReader(2000000, 64);
        EmailJsonCodec codec = new EmailJsonCodec(reader);
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        codec.EncodeEmail(email, writer, "/email");
        writer.DisposeWriter();
        return codec.DecodeEmail(reader.ReadUtf8Memory(stream.ToArray()), "/email");
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
