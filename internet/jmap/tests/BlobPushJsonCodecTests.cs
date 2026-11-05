using System.Text;
using System.Text.Json;
using jmap;

namespace jmap.Tests;

public class BlobPushJsonCodecTests
{
    [Test]
    public void BlobUploadAndCopyCodecsMapMetadataPartialResultsAndErrors()
    {
        BlobUploadResponse upload = Decode(
            "{\"accountId\":\"a\",\"blobId\":\"b1\",\"type\":\"image/png\",\"size\":0,\"scan\":\"clean\"}",
            Codec().DecodeBlobUploadResponse);
        Assert.Multiple(() =>
        {
            Assert.That(upload.Size, Is.Zero);
            Assert.That(upload.UnknownProperties["scan"].GetString(), Is.EqualTo("clean"));
        });
        Assert.That(Encode(upload, Codec().EncodeBlobUploadResponse), Is.EqualTo("{\"accountId\":\"a\",\"blobId\":\"b1\",\"type\":\"image/png\",\"size\":0,\"scan\":\"clean\"}"));

        BlobCopyRequest request = Decode(
            "{\"fromAccountId\":\"from\",\"accountId\":\"to\",\"blobIds\":[\"b1\",\"b2\"],\"x\":1}",
            Codec().DecodeBlobCopyRequest);
        Assert.That(request.BlobIds, Is.EqualTo(new[] { "b1", "b2" }));
        Assert.That(Encode(request, Codec().EncodeBlobCopyRequest), Does.Contain("\"blobIds\":[\"b1\",\"b2\"]"));

        BlobCopyResponse response = Decode(
            "{\"fromAccountId\":\"from\",\"accountId\":\"to\",\"copied\":{\"b2\":\"n2\",\"b1\":\"n1\"},\"notCopied\":{\"b3\":{\"type\":\"notFound\"}}}",
            Codec().DecodeBlobCopyResponse);
        Assert.That(response.Copied!["b1"], Is.EqualTo("n1"));
        Assert.That(response.NotCopied!["b3"].Kind, Is.EqualTo(JmapSetErrorKind.NotFound));
        Assert.That(Encode(response, Codec().EncodeBlobCopyResponse), Does.Contain("\"copied\":{\"b1\":\"n1\",\"b2\":\"n2\"}"));
    }

    [Test]
    public void StateChangeCodecMapsNestedStateMapsAndStableOrdering()
    {
        string json = "{\"@type\":\"StateChange\",\"changed\":{\"account-b\":{\"Email\":\"e2\"},\"account-a\":{\"Mailbox\":\"m1\",\"Email\":\"e1\"}},\"sequence\":7}";
        StateChange change = Decode(json, Codec().DecodeStateChange);

        Assert.Multiple(() =>
        {
            Assert.That(change.Type, Is.EqualTo("StateChange"));
            Assert.That(change.Changed["account-a"]["Email"], Is.EqualTo("e1"));
            Assert.That(change.UnknownProperties["sequence"].GetInt32(), Is.EqualTo(7));
        });
        Assert.That(
            Encode(change, Codec().EncodeStateChange),
            Is.EqualTo("{\"@type\":\"StateChange\",\"changed\":{\"account-a\":{\"Email\":\"e1\",\"Mailbox\":\"m1\"},\"account-b\":{\"Email\":\"e2\"}},\"sequence\":7}"));
    }

    [Test]
    public void PushSubscriptionAndVerificationCodecsMapPrivateAndOptionalFields()
    {
        string subscriptionJson = "{\"id\":\"p1\",\"deviceClientId\":\"device\",\"url\":\"https://push.example/callback\",\"keys\":{\"p256dh\":\"public\",\"auth\":\"secret\",\"x\":1},\"verificationCode\":\"verify\",\"expires\":\"2026-09-17T12:00:00Z\",\"types\":[\"Email\",\"Mailbox\"],\"future\":true}";
        PushSubscription subscription = Decode(subscriptionJson, value => Codec().DecodePushSubscription(value, "/subscription"));

        Assert.Multiple(() =>
        {
            Assert.That(subscription.Keys!.P256dh, Is.EqualTo("public"));
            Assert.That(subscription.Keys.UnknownProperties.ContainsKey("x"), Is.True);
            Assert.That(subscription.Types, Is.EqualTo(new[] { "Email", "Mailbox" }));
            Assert.That(subscription.UnknownProperties.ContainsKey("future"), Is.True);
        });
        Assert.That(Encode(subscription, (item, writer) => Codec().EncodePushSubscription(item, writer, "/subscription")), Is.EqualTo(subscriptionJson));

        string verificationJson = "{\"@type\":\"PushVerification\",\"pushSubscriptionId\":\"p1\",\"verificationCode\":\"verify\",\"x\":1}";
        PushVerification verification = Decode(verificationJson, Codec().DecodePushVerification);
        Assert.That(verification.Type, Is.EqualTo("PushVerification"));
        Assert.That(verification.UnknownProperties.ContainsKey("x"), Is.True);
        Assert.That(Encode(verification, Codec().EncodePushVerification), Is.EqualTo(verificationJson));
    }

    [Test]
    public void PushGetAndSetCodecsUseAccountlessStandardShapesAndWritableCreates()
    {
        PushSubscriptionGetRequest getRequest = Decode(
            "{\"ids\":null,\"properties\":[\"id\",\"deviceClientId\"],\"x\":1}",
            Codec().DecodePushGetRequest);
        Assert.That(getRequest.Ids, Is.Null);
        Assert.That(Encode(getRequest, Codec().EncodePushGetRequest), Is.EqualTo("{\"properties\":[\"id\",\"deviceClientId\"],\"x\":1}"));

        PushSubscriptionGetResponse getResponse = Decode(
            "{\"list\":[{\"id\":\"p1\",\"deviceClientId\":\"device\"}],\"notFound\":[\"missing\"]}",
            Codec().DecodePushGetResponse);
        Assert.That(getResponse.List[0].Id, Is.EqualTo("p1"));
        Assert.That(Encode(getResponse, Codec().EncodePushGetResponse), Does.Contain("\"notFound\":[\"missing\"]"));

        string setJson = "{\"create\":{\"c1\":{\"deviceClientId\":\"device\",\"url\":\"https://push.example/callback\",\"keys\":{\"p256dh\":\"public\",\"auth\":\"secret\"},\"expires\":\"2026-09-17T12:00:00Z\",\"types\":[\"Email\"]}},\"update\":{\"p1\":{\"expires\":\"2026-09-18T12:00:00Z\"}},\"destroy\":[\"p2\"]}";
        PushSubscriptionSetRequest setRequest = Decode(setJson, Codec().DecodePushSetRequest);
        Assert.That(setRequest.Create!["c1"].Url, Does.StartWith("https://"));
        Assert.That(setRequest.Update!["p1"].Values.ContainsKey("expires"), Is.True);
        Assert.That(Encode(setRequest, Codec().EncodePushSetRequest), Is.EqualTo(setJson));

        PushSubscriptionSetResponse setResponse = Decode(
            "{\"created\":{\"c1\":{\"id\":\"p3\"}},\"updated\":{\"p1\":null},\"destroyed\":[\"p2\"],\"notCreated\":{\"c2\":{\"type\":\"invalidProperties\"}}}",
            Codec().DecodePushSetResponse);
        Assert.That(setResponse.Created!["c1"].Id, Is.EqualTo("p3"));
        Assert.That(setResponse.NotCreated!["c2"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
        Assert.That(Encode(setResponse, Codec().EncodePushSetResponse), Does.Contain("\"updated\":{\"p1\":null}"));
    }

    [Test]
    public void EventPayloadCodecClassifiesAndParsesStatePingCommentAndUnknownRecords()
    {
        BlobPushJsonCodec codec = Codec();
        string stateData = "{\"@type\":\"StateChange\",\"changed\":{\"a\":{\"Email\":\"s1\"}}}";
        JmapEvent state = codec.DecodeEventRecord("state", "42", stateData, null, 5000, true);
        JmapEvent ping = codec.DecodeEventRecord("ping", null, "{\"interval\":0}", null, 0, false);
        JmapEvent comment = codec.DecodeEventRecord(null, null, null, "keep-alive", 0, false);
        JmapEvent unknown = codec.DecodeEventRecord("future", null, "opaque", null, 0, false);

        Assert.Multiple(() =>
        {
            Assert.That(state.Kind, Is.EqualTo(JmapEventKind.State));
            Assert.That(state.StateChange!.Changed["a"]["Email"], Is.EqualTo("s1"));
            Assert.That(state.RetryMilliseconds, Is.EqualTo(5000));
            Assert.That(state.HasRetryMilliseconds, Is.True);
            Assert.That(ping.Kind, Is.EqualTo(JmapEventKind.Ping));
            Assert.That(ping.PingIntervalSeconds, Is.Zero);
            Assert.That(ping.HasPingIntervalSeconds, Is.True);
            Assert.That(comment.Kind, Is.EqualTo(JmapEventKind.Comment));
            Assert.That(comment.Comment, Is.EqualTo("keep-alive"));
            Assert.That(unknown.Kind, Is.EqualTo(JmapEventKind.Unknown));
            Assert.That(unknown.Data, Is.EqualTo("opaque"));
            Assert.That(codec.ClassifyEventName("state"), Is.EqualTo(JmapEventKind.State));
            Assert.That(codec.ClassifyEventName("ping"), Is.EqualTo(JmapEventKind.Ping));
            Assert.That(codec.ClassifyEventName("new"), Is.EqualTo(JmapEventKind.Unknown));
        });
        Assert.That(codec.EncodeStateEventData(state.StateChange!), Is.EqualTo(stateData));
        Assert.That(codec.EncodePingEventData(0), Is.EqualTo("{\"interval\":0}"));
    }

    [Test]
    public void BlobPushAndEventCodecsRejectInvalidRequiredShapesAndUnsignedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Decode("{\"accountId\":\"a\",\"blobId\":\"b\",\"type\":\"x\",\"size\":-1}", Codec().DecodeBlobUploadResponse), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Decode("{\"@type\":\"StateChange\",\"changed\":{\"a\":{\"Email\":1}}}", Codec().DecodeStateChange), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Decode("{\"p256dh\":\"p\"}", value => Codec().DecodePushKeys(value, "/keys")), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Codec().DecodePingEventData("{\"interval\":-1}"), Throws.TypeOf<JmapProtocolException>());
        });
    }

    private static BlobPushJsonCodec Codec()
    {
        return new BlobPushJsonCodec(new JmapJsonReader(200000, 64));
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
}
