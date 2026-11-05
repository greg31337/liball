using System.Text.Json;

namespace jmap.Tests;

public class EmailHeaderCreationTests
{
    [Test]
    public void DerivesEveryEmailConveniencePropertyUsingTheLastHeader()
    {
        HeaderPropertySelector selector = new HeaderPropertySelector();
        Email email = new Email { Subject = "stale" };
        List<EmailHeader> headers = new List<EmailHeader>
        {
            Header("Message-ID", " <message@example.test>"),
            Header("In-Reply-To", " <parent@example.test>"),
            Header("References", " <first@example.test> <second@example.test>"),
            Header("Sender", " Sender <sender@example.test>"),
            Header("From", " Author <author@example.test>"),
            Header("To", " to@example.test"),
            Header("Cc", " cc@example.test"),
            Header("Bcc", ""),
            Header("Reply-To", " Replies <reply@example.test>"),
            Header("Subject", " first"),
            Header("subject", " =?UTF-8?Q?caf=C3=A9?="),
            Header("Date", " Fri, 11 Sep 2026 14:05:06 -0700")
        };

        selector.DeriveConvenienceProperties(email, headers);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(email.MessageId, Is.EqualTo(new[] { "message@example.test" }));
            Assert.That(email.InReplyTo, Is.EqualTo(new[] { "parent@example.test" }));
            Assert.That(email.References, Is.EqualTo(new[] { "first@example.test", "second@example.test" }));
            Assert.That(email.Sender!.Single().Email, Is.EqualTo("sender@example.test"));
            Assert.That(email.From!.Single().Name, Is.EqualTo("Author"));
            Assert.That(email.To!.Single().Email, Is.EqualTo("to@example.test"));
            Assert.That(email.Cc!.Single().Email, Is.EqualTo("cc@example.test"));
            Assert.That(email.Bcc, Is.Empty);
            Assert.That(email.ReplyTo!.Single().Email, Is.EqualTo("reply@example.test"));
            Assert.That(email.Subject, Is.EqualTo("café"));
            Assert.That(email.SentAt, Is.EqualTo("2026-09-11T14:05:06-07:00"));
        }

        selector.DeriveConvenienceProperties(email, null);
        Assert.Multiple(() =>
        {
            Assert.That(email.MessageId, Is.Null);
            Assert.That(email.From, Is.Null);
            Assert.That(email.Subject, Is.Null);
            Assert.That(email.SentAt, Is.Null);
        });
    }

    [Test]
    public void CreationRejectsDuplicateForbiddenAndMalformedHeaderProperties()
    {
        Email email = Draft();
        email.From = new List<EmailAddress> { new EmailAddress { Email = "author@example.test" } };
        email.HeaderProperties["header:from:asAddresses"] = Json("[{\"name\":null,\"email\":\"other@example.test\"}]");
        email.HeaderProperties["header:X-Trace"] = Json("\"first\"");
        email.HeaderProperties["header:x-trace:asText:all"] = Json("[\"second\"]");
        email.HeaderProperties["header:Content-Language"] = Json("\"en\"");
        email.HeaderProperties["header:To:asDate"] = Json("\"2026-09-11T14:05:06Z\"");
        email.HeaderProperties["header:X-Bad:all:asText"] = Json("\"bad suffix\"");
        email.HeaderProperties["header:X-Date:asDate"] = Json("17");

        List<string> invalid = new EmailValidator().ValidateCreate(email);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invalid, Does.Contain("from"));
            Assert.That(invalid, Does.Contain("header:from:asAddresses"));
            Assert.That(invalid, Does.Contain("header:X-Trace"));
            Assert.That(invalid, Does.Contain("header:x-trace:asText:all"));
            Assert.That(invalid, Does.Contain("header:Content-Language"));
            Assert.That(invalid, Does.Contain("header:To:asDate"));
            Assert.That(invalid, Does.Contain("header:X-Bad:all:asText"));
            Assert.That(invalid, Does.Contain("header:X-Date:asDate"));
        }
    }

    [Test]
    public void CreationValidatesEachDynamicHeaderValueShape()
    {
        Email valid = Draft();
        valid.HeaderProperties["header:X-Raw"] = Json("\"raw\"");
        valid.HeaderProperties["header:X-Text:asText:all"] = Json("[\"one\",\"two\"]");
        valid.HeaderProperties["header:X-Addresses:asAddresses"] =
            Json("[{\"name\":null,\"email\":\"person@example.test\"}]");
        valid.HeaderProperties["header:X-Groups:asGroupedAddresses"] =
            Json("[{\"name\":\"Team\",\"addresses\":[]}]");
        valid.HeaderProperties["header:X-Ids:asMessageIds"] = Json("[\"id@example.test\"]");
        valid.HeaderProperties["header:X-Date:asDate"] = Json("\"2026-09-11T14:05:06Z\"");
        valid.HeaderProperties["header:X-Urls:asURLs"] = Json("[\"https://example.test/help\"]");

        Email invalid = Draft();
        invalid.MessageId = new List<string> { "not-an-id" };
        invalid.From = new List<EmailAddress> { new EmailAddress() };
        invalid.SentAt = "not-a-date";
        invalid.HeaderProperties["header:X-Raw"] = Json("null");
        invalid.HeaderProperties["header:X-Addresses:asAddresses"] = Json("[{\"name\":null}]");
        invalid.HeaderProperties["header:X-Groups:asGroupedAddresses"] = Json("[{\"name\":null}]");
        invalid.HeaderProperties["header:X-Ids:asMessageIds"] = Json("[\"not-an-id\"]");
        invalid.HeaderProperties["header:X-Date:asDate"] = Json("\"not-a-date\"");
        invalid.HeaderProperties["header:X-Urls:asURLs"] = Json("[\"relative/path\"]");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(new EmailValidator().ValidateCreate(valid), Is.Empty);
            Assert.That(new EmailValidator().ValidateCreate(invalid), Is.SupersetOf(new[]
            {
                "messageId/0",
                "from/0",
                "sentAt",
                "header:X-Raw",
                "header:X-Addresses:asAddresses",
                "header:X-Groups:asGroupedAddresses",
                "header:X-Ids:asMessageIds",
                "header:X-Date:asDate",
                "header:X-Urls:asURLs"
            }));
        }
    }

    [Test]
    public void BodyPartValidationDetectsConvenienceAndRootHeaderCollisions()
    {
        Email email = Draft();
        email.Subject = "Top-level subject";
        email.HeaderProperties["header:X-Root"] = Json("\"top\"");
        EmailBodyPart body = new EmailBodyPart
        {
            Type = "text/plain",
            Charset = "utf-8",
            Cid = "part@example.test",
            Location = "part.txt"
        };
        body.HeaderProperties["header:Content-Type"] = Json("\"text/html\"");
        body.HeaderProperties["header:Content-ID:asMessageIds"] = Json("[\"other@example.test\"]");
        body.HeaderProperties["header:Content-Transfer-Encoding"] = Json("\"base64\"");
        body.HeaderProperties["header:Content-Transfer-Encoding-Note"] = Json("\"allowed\"");
        body.HeaderProperties["header:Subject:asText"] = Json("\"body subject\"");
        body.HeaderProperties["header:x-root"] = Json("\"body\"");
        email.BodyStructure = body;

        List<string> invalid = new EmailValidator().ValidateCreate(email);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invalid, Does.Contain("bodyStructure/type"));
            Assert.That(invalid, Does.Contain("bodyStructure/charset"));
            Assert.That(invalid, Does.Contain("bodyStructure/cid"));
            Assert.That(invalid, Does.Contain("bodyStructure/header:Content-Type"));
            Assert.That(invalid, Does.Contain("bodyStructure/header:Content-ID:asMessageIds"));
            Assert.That(invalid, Does.Contain("bodyStructure/header:Content-Transfer-Encoding"));
            Assert.That(invalid, Does.Not.Contain("bodyStructure/header:Content-Transfer-Encoding-Note"));
            Assert.That(invalid, Does.Contain("subject"));
            Assert.That(invalid, Does.Contain("bodyStructure/header:Subject:asText"));
            Assert.That(invalid, Does.Contain("header:X-Root"));
            Assert.That(invalid, Does.Contain("bodyStructure/header:x-root"));
        }
    }

    [Test]
    public void EmailCreateEncodingIncludesDynamicAndMessageIdConvenienceHeaders()
    {
        Email email = Draft();
        email.MessageId = new List<string> { "message@example.test" };
        email.InReplyTo = new List<string> { "parent@example.test" };
        email.References = new List<string> { "root@example.test", "parent@example.test" };
        email.HeaderProperties["header:X-Trace:asText"] = Json("\"trace\"");

        string encoded = EncodeCreate(email);
        using JsonDocument document = JsonDocument.Parse(encoded);
        JsonElement root = document.RootElement;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.GetProperty("header:X-Trace:asText").GetString(), Is.EqualTo("trace"));
            Assert.That(root.GetProperty("messageId")[0].GetString(), Is.EqualTo("message@example.test"));
            Assert.That(root.GetProperty("inReplyTo")[0].GetString(), Is.EqualTo("parent@example.test"));
            Assert.That(root.GetProperty("references").GetArrayLength(), Is.EqualTo(2));
        }
    }

    private static Email Draft()
    {
        return new Email
        {
            MailboxIds = new Dictionary<string, bool> { ["drafts"] = true },
            Keywords = new Dictionary<string, bool>()
        };
    }

    private static EmailHeader Header(string name, string value)
    {
        return new EmailHeader { Name = name, Value = value };
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static string EncodeCreate(Email email)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        EmailJsonCodec codec = new EmailJsonCodec(new JmapJsonReader(1024 * 1024, 64));
        codec.EncodeEmailCreate(email, writer, "/email");
        writer.DisposeWriter();
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}
