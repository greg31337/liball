using System.Text;
using System.Text.Json;
using jmap;

namespace jmap.Tests;

public class EmailJsonCodecTests
{
    [Test]
    public void EmailCodecMapsMetadataHeadersAddressesBodiesAndUnknownMembers()
    {
        string json = "{\"id\":\"e1\",\"blobId\":\"b1\",\"threadId\":\"t1\",\"mailboxIds\":{\"m2\":false,\"m1\":true},\"keywords\":{\"$seen\":true},\"size\":0,\"receivedAt\":\"2026-09-10T12:00:00Z\",\"headers\":[{\"name\":\"Subject\",\"value\":\"Hi\",\"x\":1}],\"header:X-Test:asText\":\"decoded\",\"header:X-Test:asRaw:all\":[\"a\",\"b\"],\"messageId\":[\"id@example.com\"],\"from\":[{\"name\":null,\"email\":\"a@example.com\"}],\"subject\":\"Hi\",\"bodyStructure\":{\"type\":\"multipart/mixed\",\"subParts\":[{\"partId\":\"1\",\"blobId\":\"p1\",\"size\":0,\"headers\":[],\"header:Content-Type:asRaw\":\"text/plain\",\"type\":\"text/plain\",\"charset\":\"utf-8\",\"language\":[\"en\"]}],\"futurePart\":true},\"bodyValues\":{\"1\":{\"value\":\"hello\",\"isEncodingProblem\":false,\"isTruncated\":true,\"x\":2}},\"textBody\":[{\"partId\":\"1\",\"type\":\"text/plain\"}],\"htmlBody\":[],\"attachments\":[],\"hasAttachment\":false,\"preview\":\"hello\",\"vendor\":{\"b\":2,\"a\":1}}";
        Email email = Decode(json, value => Codec().DecodeEmail(value, "/email"));

        Assert.Multiple(() =>
        {
            Assert.That(email.MailboxIds!["m2"], Is.False);
            Assert.That(email.Size, Is.Zero);
            Assert.That(email.Headers![0].Name, Is.EqualTo("Subject"));
            Assert.That(email.Headers[0].UnknownProperties["x"].GetInt32(), Is.EqualTo(1));
            Assert.That(email.HeaderProperties["header:X-Test:asText"].GetString(), Is.EqualTo("decoded"));
            Assert.That(email.UnknownProperties.ContainsKey("header:X-Test:asText"), Is.False);
            Assert.That(email.From![0].Name, Is.Null);
            Assert.That(email.BodyStructure!.SubParts![0].HasSize, Is.True);
            Assert.That(email.BodyStructure.SubParts[0].Size, Is.Zero);
            Assert.That(email.BodyStructure.SubParts[0].HeaderProperties.ContainsKey("header:Content-Type:asRaw"), Is.True);
            Assert.That(email.BodyStructure.UnknownProperties["futurePart"].GetBoolean(), Is.True);
            Assert.That(email.BodyValues!["1"].IsEncodingProblem, Is.False);
            Assert.That(email.BodyValues["1"].IsTruncated, Is.True);
        });

        string encoded = Encode(email, (item, writer) => Codec().EncodeEmail(item, writer, "/email"));
        Assert.That(encoded, Does.Contain("\"mailboxIds\":{\"m1\":true,\"m2\":false}"));
        Assert.That(encoded, Does.Contain("\"header:X-Test:asRaw:all\":[\"a\",\"b\"],\"header:X-Test:asText\":\"decoded\""));
        Assert.That(encoded, Does.Contain("\"size\":0,\"headers\":[],\"header:Content-Type:asRaw\":\"text/plain\""));
        Assert.That(encoded, Does.Contain("\"vendor\":{\"a\":1,\"b\":2}"));
    }

    [Test]
    public void AddressGroupCodecPreservesNullableNamesOrderingAndUnknownData()
    {
        string json = "[{\"name\":null,\"addresses\":[{\"name\":null,\"email\":\"one@example.com\"}]},{\"name\":\"Friends\",\"addresses\":[{\"name\":\"Two\",\"email\":null}],\"x\":true}]";
        List<EmailAddressGroup> groups = Decode(json, value => Codec().DecodeAddressGroups(value, "/groups"));

        Assert.Multiple(() =>
        {
            Assert.That(groups[0].Name, Is.Null);
            Assert.That(groups[0].Addresses[0].Email, Is.EqualTo("one@example.com"));
            Assert.That(groups[1].Addresses[0].Email, Is.Null);
            Assert.That(groups[1].UnknownProperties["x"].GetBoolean(), Is.True);
        });
        Assert.That(Encode(groups, (item, writer) => Codec().EncodeAddressGroups(item, writer, "/groups")), Is.EqualTo(json));
    }

    [Test]
    public void HeaderFormClassifierAcceptsWireTokensAndSuffixConstants()
    {
        EmailJsonCodec codec = Codec();
        Assert.Multiple(() =>
        {
            Assert.That(codec.ClassifyHeaderForm("asRaw"), Is.EqualTo(JmapHeaderForm.Raw));
            Assert.That(codec.ClassifyHeaderForm(JmapProperties.HeaderAsTextSuffix), Is.EqualTo(JmapHeaderForm.Text));
            Assert.That(codec.ClassifyHeaderForm("asAddresses"), Is.EqualTo(JmapHeaderForm.Addresses));
            Assert.That(codec.ClassifyHeaderForm("asGroupedAddresses"), Is.EqualTo(JmapHeaderForm.GroupedAddresses));
            Assert.That(codec.ClassifyHeaderForm("asMessageIds"), Is.EqualTo(JmapHeaderForm.MessageIds));
            Assert.That(codec.ClassifyHeaderForm("asDate"), Is.EqualTo(JmapHeaderForm.Date));
            Assert.That(codec.ClassifyHeaderForm("asURLs"), Is.EqualTo(JmapHeaderForm.Urls));
            Assert.That(() => codec.ClassifyHeaderForm("asNumber"), Throws.TypeOf<JmapValidationException>());
        });
    }

    [Test]
    public void EmailCreateAndCopyEncodersEmitOnlyTheirWritableWireShapes()
    {
        Email email = new Email();
        email.Id = "source";
        email.BlobId = "server-blob";
        email.ThreadId = "server-thread";
        email.MailboxIds = new Dictionary<string, bool> { { "inbox", true } };
        email.Keywords = new Dictionary<string, bool> { { "$draft", true } };
        email.Size = 123;
        email.ReceivedAt = "2026-09-10T12:00:00Z";
        email.Subject = "Draft";
        email.HasAttachment = true;
        email.Preview = "server preview";

        string create = Encode(email, (item, writer) => Codec().EncodeEmailCreate(item, writer, "/create/c1"));
        string copy = Encode(email, (item, writer) => Codec().EncodeEmailCopy(item, writer, "/create/c1"));

        Assert.Multiple(() =>
        {
            Assert.That(create, Does.Contain("\"mailboxIds\":{\"inbox\":true}"));
            Assert.That(create, Does.Contain("\"subject\":\"Draft\""));
            Assert.That(create, Does.Not.Contain("\"id\""));
            Assert.That(create, Does.Not.Contain("\"blobId\""));
            Assert.That(create, Does.Not.Contain("\"size\""));
            Assert.That(create, Does.Not.Contain("\"hasAttachment\""));
            Assert.That(copy, Is.EqualTo("{\"id\":\"source\",\"mailboxIds\":{\"inbox\":true},\"keywords\":{\"$draft\":true},\"receivedAt\":\"2026-09-10T12:00:00Z\"}"));
        });
    }

    [Test]
    public void EmailCodecRejectsInvalidNestedShapesAndDynamicHeaderStorage()
    {
        Email email = new Email();
        email.HeaderProperties.Add("not-a-header", Json("true"));

        Assert.Multiple(() =>
        {
            Assert.That(() => Decode("{\"name\":\"A\",\"email\":1}", value => Codec().DecodeAddress(value, "/address")), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Decode("{\"value\":\"x\",\"isEncodingProblem\":false}", value => Codec().DecodeBodyValue(value, "/body")), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Decode("{\"inbox\":1}", value => Codec().DecodeBooleanMap(value, "/map")), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Encode(email, (item, writer) => Codec().EncodeEmail(item, writer, "/email")), Throws.TypeOf<JmapValidationException>());
        });
    }

    private static EmailJsonCodec Codec()
    {
        return new EmailJsonCodec(new JmapJsonReader(200000, 64));
    }

    private static T Decode<T>(string json, Func<JsonElement, T> decode)
    {
        JmapJsonReader reader = new JmapJsonReader(200000, 64);
        return decode(reader.ReadUtf8Memory(Encoding.UTF8.GetBytes(json)));
    }

    private static string Encode<T>(T value, Action<T, JmapJsonWriter> encode)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        encode(value, writer);
        writer.DisposeWriter();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static JsonElement Json(string value)
    {
        using JsonDocument document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }
}
