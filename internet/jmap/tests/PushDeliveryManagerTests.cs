using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace jmap.Tests;

public class PushDeliveryManagerTests
{
    [Test]
    public void DeliveryCoalescesLatestStatesAndAppliesSubscriptionAndEmailDeliveryFilters()
    {
        CapturingHandler handler = new CapturingHandler();
        FakeJmapClock clock = new FakeJmapClock();
        PushDeliveryManager manager = Manager(handler, clock);
        PushSubscription subscription = Subscription(null);
        subscription.Types = new List<string> { JmapDataTypes.Mailbox, JmapDataTypes.Email, JmapDataTypes.EmailDelivery };
        RegisterVerified(manager, subscription, Context());
        JmapStatePublication first = Publication(false);
        first.StateChange.Changed["account-1"] = new Dictionary<string, string>
        {
            [JmapDataTypes.Mailbox] = "m1",
            [JmapDataTypes.Email] = "e1",
            [JmapDataTypes.EmailDelivery] = "d1",
            [JmapDataTypes.Identity] = "i1"
        };
        JmapStatePublication second = Publication(true);
        second.StateChange.Changed["account-1"] = new Dictionary<string, string>
        {
            [JmapDataTypes.Email] = "e2",
            [JmapDataTypes.EmailDelivery] = "d2"
        };

        manager.QueuePublication(first);
        manager.QueuePublication(second);
        manager.Flush(CancellationToken.None);
        StateChange delivered = new JmapJsonCodec().DecodeStateChangeBytes(handler.Bodies[0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handler.Calls, Is.EqualTo(1));
            Assert.That(delivered.Changed["account-1"], Is.EqualTo(new Dictionary<string, string>
            {
                [JmapDataTypes.Mailbox] = "m1",
                [JmapDataTypes.Email] = "e2",
                [JmapDataTypes.EmailDelivery] = "d2"
            }));
            Assert.That(handler.Headers[0]["TTL"], Is.EqualTo("123"));
            Assert.That(handler.Headers[0]["Urgency"], Is.EqualTo("normal"));
            Assert.That(handler.Headers[0]["Topic"], Has.Length.EqualTo(32));
            Assert.That(handler.ContentTypes[0], Is.EqualTo("application/json; charset=utf-8"));
            Assert.That(handler.ContentEncodings[0], Is.Empty);
        }
    }

    [Test]
    public void EveryCoreAndMailStateTypeIsDeliveredAndEmailDeliveryRequiresNewMail()
    {
        CapturingHandler handler = new CapturingHandler();
        PushDeliveryManager manager = Manager(handler, new FakeJmapClock());
        RegisterVerified(manager, Subscription(null), Context());
        JmapStatePublication publication = Publication(false);
        publication.StateChange.Changed["account-1"] = AllStates();

        manager.QueuePublication(publication);
        manager.Flush(CancellationToken.None);
        StateChange withoutDelivery = new JmapJsonCodec().DecodeStateChangeBytes(handler.Bodies[0]);
        publication.HasNewEmailDelivery = true;
        manager.QueuePublication(publication);
        manager.Flush(CancellationToken.None);
        StateChange withDelivery = new JmapJsonCodec().DecodeStateChangeBytes(handler.Bodies[1]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(withoutDelivery.Changed["account-1"].Keys, Is.EquivalentTo(new[]
            {
                JmapDataTypes.Mailbox,
                JmapDataTypes.Thread,
                JmapDataTypes.Email,
                JmapDataTypes.Identity,
                JmapDataTypes.EmailSubmission,
                JmapDataTypes.VacationResponse
            }));
            Assert.That(withDelivery.Changed["account-1"].Keys, Is.EquivalentTo(new[]
            {
                JmapDataTypes.Mailbox,
                JmapDataTypes.Thread,
                JmapDataTypes.Email,
                JmapDataTypes.EmailDelivery,
                JmapDataTypes.Identity,
                JmapDataTypes.EmailSubmission,
                JmapDataTypes.VacationResponse
            }));
        }
    }

    [Test]
    public void UnverifiedAndExpiredSubscriptionsNeverReceiveStateChanges()
    {
        CapturingHandler handler = new CapturingHandler();
        FakeJmapClock clock = new FakeJmapClock();
        PushDeliveryManager manager = Manager(handler, clock);
        JmapRequestContext context = Context();
        PushSubscription subscription = Subscription(null);
        PushSubscriptionSecurity security = new PushSubscriptionSecurity();
        string code = security.GenerateVerificationCode();
        PushSubscriptionSecurityState state = security.Capture(subscription, context, code);
        manager.RegisterTarget("push-1", state, context);
        manager.QueuePublication(EmailPublication("e1"));
        manager.Flush(CancellationToken.None);
        security.ConfirmVerification(state, code);
        context.HasCredentialExpiresUtc = true;
        context.CredentialExpiresUtc = clock.CurrentUtcDateTime.AddSeconds(-1);
        manager.QueuePublication(EmailPublication("e2"));
        manager.Flush(CancellationToken.None);

        Assert.That(handler.Calls, Is.Zero);
    }

    [Test]
    public void RetryAfterBackoffRetainsAndFurtherCoalescesPendingState()
    {
        CapturingHandler handler = new CapturingHandler();
        handler.Statuses.Enqueue(HttpStatusCode.TooManyRequests);
        handler.Statuses.Enqueue(HttpStatusCode.NoContent);
        handler.RetryAfterSeconds = 120;
        FakeJmapClock clock = new FakeJmapClock();
        PushDeliveryManager manager = Manager(handler, clock);
        RegisterVerified(manager, Subscription(null), Context());

        manager.QueuePublication(EmailPublication("e1"));
        manager.Flush(CancellationToken.None);
        manager.QueuePublication(EmailPublication("e2"));
        manager.Flush(CancellationToken.None);
        clock.Advance(TimeSpan.FromSeconds(121));
        manager.Flush(CancellationToken.None);
        manager.Flush(CancellationToken.None);
        StateChange retried = new JmapJsonCodec().DecodeStateChangeBytes(handler.Bodies[1]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handler.Calls, Is.EqualTo(2));
            Assert.That(retried.Changed["account-1"][JmapDataTypes.Email], Is.EqualTo("e2"));
        }
    }

    [Test]
    public void EncryptedDeliveryUsesAes128GcmRecordAndPushHeaders()
    {
        CapturingHandler handler = new CapturingHandler();
        PushDeliveryManager manager = Manager(handler, new FakeJmapClock());
        PushSubscription subscription = Subscription(new PushSubscriptionKeys
        {
            P256dh = "BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4",
            Auth = "BTBZMqHH6r4Tts7J_aSIgg"
        });
        RegisterVerified(manager, subscription, Context());

        manager.QueuePublication(EmailPublication("e1"));
        manager.Flush(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handler.Calls, Is.EqualTo(1));
            Assert.That(handler.ContentEncodings[0], Is.EqualTo(new[] { "aes128gcm" }));
            Assert.That(handler.Bodies[0], Has.Length.GreaterThan(86 + 16));
            Assert.That(handler.Bodies[0][16], Is.EqualTo(0));
            Assert.That(handler.Bodies[0][17], Is.EqualTo(0));
            Assert.That(handler.Bodies[0][18], Is.EqualTo(16));
            Assert.That(handler.Bodies[0][19], Is.EqualTo(0));
            Assert.That(handler.Bodies[0][20], Is.EqualTo(65));
        }
    }

    [Test]
    public void BuiltInVerificationSenderEncryptsWhenSubscriptionHasKeys()
    {
        CapturingHandler handler = new CapturingHandler();
        PushDeliveryManager manager = Manager(handler, new FakeJmapClock());
        PushSubscription subscription = Subscription(new PushSubscriptionKeys
        {
            P256dh = "BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4",
            Auth = "BTBZMqHH6r4Tts7J_aSIgg"
        });
        PushVerification verification = new PushVerification
        {
            PushSubscriptionId = "push-1",
            VerificationCode = "verification-code"
        };

        manager.SendPushVerification(subscription, verification, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handler.Calls, Is.EqualTo(1));
            Assert.That(handler.ContentEncodings[0], Is.EqualTo(new[] { "aes128gcm" }));
            Assert.That(handler.Headers[0]["TTL"], Is.EqualTo("123"));
            Assert.That(handler.Headers[0]["Urgency"], Is.EqualTo("normal"));
            Assert.That(handler.Headers[0]["Topic"], Has.Length.EqualTo(32));
        }
    }

    private static PushDeliveryManager Manager(CapturingHandler handler, FakeJmapClock clock)
    {
        HttpClient client = new HttpClient(handler);
        JmapServerOptions options = new JmapServerOptions { PushTimeToLiveSeconds = 123 };
        return new PushDeliveryManager(client, options, clock);
    }

    private static void RegisterVerified(PushDeliveryManager manager, PushSubscription subscription, JmapRequestContext context)
    {
        PushSubscriptionSecurity security = new PushSubscriptionSecurity();
        string code = security.GenerateVerificationCode();
        PushSubscriptionSecurityState state = security.Capture(subscription, context, code);
        security.ConfirmVerification(state, code);
        manager.RegisterTarget("push-1", state, context);
    }

    private static PushSubscription Subscription(PushSubscriptionKeys? keys)
    {
        return new PushSubscription
        {
            Id = "push-1",
            DeviceClientId = "device-hash",
            Url = "https://push.example.test/events",
            Keys = keys
        };
    }

    private static JmapRequestContext Context()
    {
        return new JmapRequestContext { CredentialId = "credential-1" };
    }

    private static JmapStatePublication Publication(bool hasNewEmailDelivery)
    {
        return new JmapStatePublication { HasNewEmailDelivery = hasNewEmailDelivery };
    }

    private static JmapStatePublication EmailPublication(string state)
    {
        JmapStatePublication publication = Publication(false);
        publication.StateChange.Changed["account-1"] = new Dictionary<string, string> { [JmapDataTypes.Email] = state };
        return publication;
    }

    private static Dictionary<string, string> AllStates()
    {
        return new Dictionary<string, string>
        {
            [JmapDataTypes.Mailbox] = "m1",
            [JmapDataTypes.Thread] = "t1",
            [JmapDataTypes.Email] = "e1",
            [JmapDataTypes.EmailDelivery] = "d1",
            [JmapDataTypes.Identity] = "i1",
            [JmapDataTypes.EmailSubmission] = "s1",
            [JmapDataTypes.VacationResponse] = "v1"
        };
    }

    private class CapturingHandler : HttpMessageHandler
    {
        public int Calls;
        public int RetryAfterSeconds;
        public Queue<HttpStatusCode> Statuses = new Queue<HttpStatusCode>();
        public List<byte[]> Bodies = new List<byte[]>();
        public List<Dictionary<string, string>> Headers = new List<Dictionary<string, string>>();
        public List<string?> ContentTypes = new List<string?>();
        public List<List<string>> ContentEncodings = new List<List<string>>();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls = Calls + 1;
            Bodies.Add(await request.Content!.ReadAsByteArrayAsync(cancellationToken));
            Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                headers[header.Key] = string.Join(",", header.Value);
            }

            Headers.Add(headers);
            ContentTypes.Add(request.Content.Headers.ContentType!.ToString());
            ContentEncodings.Add(new List<string>(request.Content.Headers.ContentEncoding));
            HttpStatusCode status = Statuses.Count == 0 ? HttpStatusCode.NoContent : Statuses.Dequeue();
            HttpResponseMessage response = new HttpResponseMessage(status);
            if (status == HttpStatusCode.TooManyRequests && RetryAfterSeconds > 0)
            {
                response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(RetryAfterSeconds));
            }

            return response;
        }
    }
}
