using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class BlobAndPushModelsTests
{
    [Test]
    public void BinaryTransferModelsKeepStreamsAndDistinguishUnknownLengthFromZeroLength()
    {
        MemoryStream uploadStream = new MemoryStream(Encoding.UTF8.GetBytes("blob data"));
        BlobUploadRequest upload = new BlobUploadRequest();
        upload.AccountId = "account-1";
        upload.MediaType = "text/plain";
        upload.Content = uploadStream;
        upload.ContentLength = uploadStream.Length;
        upload.HasContentLength = true;

        BlobUploadRequest unknownLength = new BlobUploadRequest();
        unknownLength.Content = new MemoryStream();

        MemoryStream downloadStream = new MemoryStream(new byte[] { 0, 1, 2 });
        BlobDownloadResponse download = new BlobDownloadResponse();
        download.Content = downloadStream;
        download.MediaType = JmapMediaTypes.OctetStream;
        download.FileName = "data.bin";
        download.ContentLength = 3;
        download.HasContentLength = true;
        download.CacheControl = "private, immutable, max-age=31536000";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(upload.Content, Is.SameAs(uploadStream));
            Assert.That(upload.ContentLength, Is.EqualTo(9));
            Assert.That(upload.HasContentLength, Is.True);
            Assert.That(unknownLength.ContentLength, Is.Zero);
            Assert.That(unknownLength.HasContentLength, Is.False);
            Assert.That(download.Content, Is.SameAs(downloadStream));
            Assert.That(download.ContentLength, Is.EqualTo(3));
            Assert.That(download.HasContentLength, Is.True);
            Assert.That(download.CacheControl, Does.Contain("immutable"));
        }
    }

    [Test]
    public void UploadAndCopyModelsPreserveMetadataPartialResultsAndUnknownFields()
    {
        BlobUploadResponse upload = new BlobUploadResponse();
        upload.AccountId = "target";
        upload.BlobId = "blob-new";
        upload.Type = "image/png";
        upload.Size = 2048;
        upload.UnknownProperties.Add("vendorScan", Json("\"clean\""));

        BlobCopyRequest request = new BlobCopyRequest();
        request.FromAccountId = "source";
        request.AccountId = "target";
        request.BlobIds.Add("blob-1");
        request.BlobIds.Add("blob-2");

        JmapSetError error = new JmapSetError();
        error.Type = JmapErrorCodes.NotFound;
        error.Kind = JmapSetErrorKind.NotFound;

        BlobCopyResponse response = new BlobCopyResponse();
        response.FromAccountId = "source";
        response.AccountId = "target";
        response.Copied = new Dictionary<string, string>();
        response.Copied.Add("blob-1", "copied-1");
        response.NotCopied = new Dictionary<string, JmapSetError>();
        response.NotCopied.Add("blob-2", error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(upload.UnknownProperties["vendorScan"].GetString(), Is.EqualTo("clean"));
            Assert.That(request.BlobIds, Is.EqualTo(new[] { "blob-1", "blob-2" }));
            Assert.That(response.Copied["blob-1"], Is.EqualTo("copied-1"));
            Assert.That(response.NotCopied["blob-2"], Is.SameAs(error));
            Assert.That(new BlobCopyResponse().Copied, Is.Null);
            Assert.That(new BlobCopyResponse().NotCopied, Is.Null);
        }
    }

    [Test]
    public void StateChangePreservesAccountTypeStatesAndExtensionData()
    {
        StateChange change = new StateChange();
        Dictionary<string, string> firstAccount = new Dictionary<string, string>();
        firstAccount.Add(JmapDataTypes.Email, "email-state");
        firstAccount.Add(JmapDataTypes.EmailDelivery, "delivery-state");
        change.Changed.Add("account-1", firstAccount);
        change.Changed.Add("account-2", new Dictionary<string, string>
        {
            { JmapDataTypes.Mailbox, "mailbox-state" }
        });
        change.UnknownProperties.Add("vendorSequence", Json("27"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(change.Type, Is.EqualTo("StateChange"));
            Assert.That(change.Changed["account-1"][JmapDataTypes.Email], Is.EqualTo("email-state"));
            Assert.That(change.Changed["account-1"][JmapDataTypes.EmailDelivery], Is.EqualTo("delivery-state"));
            Assert.That(change.Changed["account-2"][JmapDataTypes.Mailbox], Is.EqualTo("mailbox-state"));
            Assert.That(change.UnknownProperties["vendorSequence"].GetInt32(), Is.EqualTo(27));
        }
    }

    [Test]
    public void PushSubscriptionModelsRepresentEncryptionFilteringAndVerification()
    {
        PushSubscriptionKeys keys = new PushSubscriptionKeys();
        keys.P256dh = "public-key";
        keys.Auth = "auth-secret";

        PushSubscription subscription = new PushSubscription();
        subscription.Id = "push-1";
        subscription.DeviceClientId = "device-1";
        subscription.Url = "https://push.example/callback";
        subscription.Keys = keys;
        subscription.VerificationCode = "verification-code";
        subscription.Expires = "2026-09-17T12:00:00Z";
        subscription.Types = new List<string> { JmapDataTypes.Mailbox, JmapDataTypes.Email };

        PushVerification verification = new PushVerification();
        verification.PushSubscriptionId = subscription.Id;
        verification.VerificationCode = "verification-code";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(subscription.Keys, Is.SameAs(keys));
            Assert.That(subscription.Keys.P256dh, Is.EqualTo("public-key"));
            Assert.That(subscription.Types, Is.EqualTo(new[] { JmapDataTypes.Mailbox, JmapDataTypes.Email }));
            Assert.That(verification.Type, Is.EqualTo("PushVerification"));
            Assert.That(verification.PushSubscriptionId, Is.EqualTo("push-1"));
            Assert.That(new PushSubscription().Keys, Is.Null);
            Assert.That(new PushSubscription().Types, Is.Null);
        }
    }

    [Test]
    public void PushGetAndSetModelsPreserveProjectionAndPerRecordOutcomes()
    {
        PushSubscriptionGetRequest getRequest = new PushSubscriptionGetRequest();
        getRequest.Ids = null;
        getRequest.Properties = new List<string> { JmapProperties.Id, JmapProperties.DeviceClientId };

        PushSubscriptionGetResponse getResponse = new PushSubscriptionGetResponse();
        PushSubscription subscription = new PushSubscription();
        subscription.Id = "push-1";
        getResponse.List.Add(subscription);
        getResponse.NotFound.Add("missing");

        JmapPatchObject patch = new JmapPatchObject();
        patch.Values.Add(JmapProperties.Expires, Json("\"2026-09-18T12:00:00Z\""));
        PushSubscriptionSetRequest setRequest = new PushSubscriptionSetRequest();
        setRequest.Update = new Dictionary<string, JmapPatchObject>();
        setRequest.Update.Add("push-1", patch);

        PushSubscriptionSetResponse setResponse = new PushSubscriptionSetResponse();
        setResponse.Updated = new Dictionary<string, JsonElement>();
        setResponse.Updated.Add("push-1", Json("null"));
        setResponse.Destroyed = new List<string> { "push-old" };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(getRequest.Ids, Is.Null);
            Assert.That(getRequest.Properties, Is.EqualTo(new[] { JmapProperties.Id, JmapProperties.DeviceClientId }));
            Assert.That(getResponse.List.Single(), Is.SameAs(subscription));
            Assert.That(getResponse.NotFound, Is.EqualTo(new[] { "missing" }));
            Assert.That(setRequest.Update["push-1"], Is.SameAs(patch));
            Assert.That(setResponse.Updated["push-1"].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(setResponse.Destroyed, Is.EqualTo(new[] { "push-old" }));
            Assert.That(setResponse.NotCreated, Is.Null);
            Assert.That(setResponse.NotUpdated, Is.Null);
            Assert.That(setResponse.NotDestroyed, Is.Null);
        }
    }

    [Test]
    public void EventSourceModelsCarryFilteringResumeAndParsedEventMetadata()
    {
        EventSourceOptions options = new EventSourceOptions();
        options.Types = new List<string> { JmapDataTypes.Email };
        options.CloseAfterState = true;
        options.PingIntervalSeconds = 30;
        options.LastEventId = "event-41";

        StateChange state = new StateChange();
        JmapEvent stateEvent = new JmapEvent();
        stateEvent.Kind = JmapEventKind.State;
        stateEvent.Name = "state";
        stateEvent.Id = "event-42";
        stateEvent.Data = "{\"@type\":\"StateChange\",\"changed\":{}}";
        stateEvent.StateChange = state;
        stateEvent.RetryMilliseconds = 5000;
        stateEvent.HasRetryMilliseconds = true;

        JmapEvent ping = new JmapEvent();
        ping.Kind = JmapEventKind.Ping;
        ping.Name = "ping";
        ping.Data = "{\"interval\":30}";
        ping.PingIntervalSeconds = 30;
        ping.HasPingIntervalSeconds = true;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.Types, Is.EqualTo(new[] { JmapDataTypes.Email }));
            Assert.That(options.CloseAfterState, Is.True);
            Assert.That(options.LastEventId, Is.EqualTo("event-41"));
            Assert.That(stateEvent.StateChange, Is.SameAs(state));
            Assert.That(stateEvent.Id, Is.EqualTo("event-42"));
            Assert.That(stateEvent.RetryMilliseconds, Is.EqualTo(5000));
            Assert.That(stateEvent.HasRetryMilliseconds, Is.True);
            Assert.That(ping.PingIntervalSeconds, Is.EqualTo(30));
            Assert.That(ping.HasPingIntervalSeconds, Is.True);
            Assert.That(ping.Id, Is.Null);
        }
    }

    [Test]
    public void RequiredBlobAndPushCollectionsAreIndependent()
    {
        BlobCopyRequest firstCopy = new BlobCopyRequest();
        BlobCopyRequest secondCopy = new BlobCopyRequest();
        firstCopy.BlobIds.Add("blob-1");

        StateChange firstChange = new StateChange();
        StateChange secondChange = new StateChange();
        firstChange.Changed.Add("account-1", new Dictionary<string, string>());

        PushSubscriptionGetResponse firstGet = new PushSubscriptionGetResponse();
        PushSubscriptionGetResponse secondGet = new PushSubscriptionGetResponse();
        firstGet.NotFound.Add("missing");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondCopy.BlobIds, Is.Empty);
            Assert.That(secondChange.Changed, Is.Empty);
            Assert.That(secondGet.NotFound, Is.Empty);
            Assert.That(firstCopy.BlobIds, Is.Not.SameAs(secondCopy.BlobIds));
            Assert.That(firstChange.Changed, Is.Not.SameAs(secondChange.Changed));
            Assert.That(firstGet.NotFound, Is.Not.SameAs(secondGet.NotFound));
        }
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
