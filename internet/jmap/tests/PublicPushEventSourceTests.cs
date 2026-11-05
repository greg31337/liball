using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace jmap.Tests;

public class PublicPushEventSourceTests
{
    [Test]
    public async Task PublicEventSourcePreservesUnknownVendorDataTypes()
    {
        const string vendorDataType = "https://vendor.example.test/jmap/Widget";
        JmapServerServices services = new JmapServerServices
        {
            SessionService = new FakeJmapSessionService(Session())
        };
        IJmapServer server = new JmapServer(new JmapServerOptions(), services);
        ServerTransport transport = new ServerTransport(server, new FakeJmapClock(), false);
        JmapEventSourceReader reader = new JmapEventSourceReader(
            Session().EventSourceUrl!, transport);
        await reader.ConnectEventSourceAsync(
            new EventSourceOptions
            {
                Types = new List<string> { vendorDataType },
                CloseAfterState = true
            },
            CancellationToken.None);
        JmapStatePublication publication = new JmapStatePublication { EventId = "vendor-event-1" };
        publication.StateChange.Changed["account-1"] = new Dictionary<string, string>
        {
            [vendorDataType] = "widget-state-9"
        };

        await services.StatePublisher!.PublishStateChangeAsync(
            publication, CancellationToken.None);
        JmapEvent received = await reader.ReadEventSourceAsync(CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(5));
        reader.CloseEventSource();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(received.Kind, Is.EqualTo(JmapEventKind.State));
            Assert.That(received.Id, Is.EqualTo("vendor-event-1"));
            Assert.That(received.StateChange!.Changed["account-1"][vendorDataType],
                Is.EqualTo("widget-state-9"));
        }
    }

    [Test]
    public async Task UnencryptedPushLifecycleVerifiesPrivatelyAndCoalescesAfterRetry()
    {
        PushFixture fixture = new PushFixture(false);
        fixture.Handler.ConfirmDuringVerification = true;
        fixture.Handler.Statuses.Enqueue(HttpStatusCode.ServiceUnavailable);
        fixture.Handler.Statuses.Enqueue(HttpStatusCode.TooManyRequests);
        fixture.Handler.Statuses.Enqueue(HttpStatusCode.NoContent);

        PushSubscriptionSetResponse created = await fixture.Client.SetPushSubscriptionsAsync(
            CreateRequest(false), CancellationToken.None);
        string id = created.Created!["created"].Id!;
        PushSubscriptionGetResponse fetched = await fixture.Client.GetPushSubscriptionsAsync(
            new PushSubscriptionGetRequest { Ids = new List<string> { id } },
            CancellationToken.None);

        await fixture.PublishAsync("event-1", "email-1", "delivery-1", false);
        await fixture.PublishAsync("event-2", "email-2", "delivery-2", true);
        fixture.Clock.Advance(TimeSpan.FromSeconds(121));
        await fixture.PublishAsync("event-3", "email-3", "delivery-3", false);

        StateChange first = fixture.Codec.DecodeStateChangeBytes(fixture.Handler.Posts[1].Body);
        StateChange retried = fixture.Codec.DecodeStateChangeBytes(fixture.Handler.Posts[2].Body);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(created.Created["created"].Url, Is.Null);
            Assert.That(created.Created["created"].Keys, Is.Null);
            Assert.That(created.Created["created"].VerificationCode, Is.Null);
            Assert.That(created.Created["created"].Expires, Is.Null);
            Assert.That(fetched.List, Has.Count.EqualTo(1));
            Assert.That(fetched.List[0].Url, Is.Null);
            Assert.That(fetched.List[0].Keys, Is.Null);
            Assert.That(fixture.Handler.BackendWasCommitted, Is.True);
            Assert.That(fixture.Handler.PostCountBeforeConfirmation, Is.EqualTo(1));
            Assert.That(fixture.Handler.BadVerificationError, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(first.Changed["account-1"], Is.EqualTo(new Dictionary<string, string>
            {
                [JmapDataTypes.Email] = "email-1"
            }));
            Assert.That(retried.Changed["account-1"], Is.EqualTo(new Dictionary<string, string>
            {
                [JmapDataTypes.Email] = "email-3",
                [JmapDataTypes.EmailDelivery] = "delivery-2"
            }));
            Assert.That(fixture.Handler.Posts[1].Headers["TTL"], Is.EqualTo("123"));
            Assert.That(fixture.Handler.Posts[1].Headers["Urgency"], Is.EqualTo("normal"));
            Assert.That(fixture.Handler.Posts[1].Headers["Topic"], Has.Length.EqualTo(32));
            Assert.That(fixture.Handler.Posts[1].ContentType, Is.EqualTo("application/json; charset=utf-8"));
            Assert.That(fixture.Handler.Posts[1].ContentEncoding, Is.Empty);
        }

        fixture.Clock.Advance(TimeSpan.FromSeconds(180));
        await fixture.PublishAsync("event-4", "email-4", "delivery-4", true);
        Assert.That(fixture.Handler.Posts, Has.Count.EqualTo(3),
            "credential expiry must stop delivery");

        PushFixture destroyFixture = new PushFixture(false);
        destroyFixture.Handler.ConfirmDuringVerification = true;
        PushSubscriptionSetResponse destroyCreated =
            await destroyFixture.Client.SetPushSubscriptionsAsync(
                CreateRequest(false), CancellationToken.None);
        string destroyedId = destroyCreated.Created!["created"].Id!;
        PushSubscriptionSetResponse destroyed = await destroyFixture.Client.SetPushSubscriptionsAsync(
            new PushSubscriptionSetRequest { Destroy = new List<string> { destroyedId } },
            CancellationToken.None);
        await destroyFixture.PublishAsync("event-5", "email-5", "delivery-5", true);
        Assert.That(destroyed.Destroyed, Is.EqualTo(new[] { destroyedId }));
        Assert.That(destroyFixture.Handler.Posts, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task EncryptedPushUsesWebPushRecordsForVerificationAndStateDelivery()
    {
        PushFixture fixture = new PushFixture(false);

        PushSubscriptionSetResponse created = await fixture.Client.SetPushSubscriptionsAsync(
            CreateRequest(true), CancellationToken.None);
        string id = created.Created!["created"].Id!;
        string verificationCode = fixture.PushService.GetVerificationCode(id);
        await fixture.Client.SetPushSubscriptionsAsync(
            VerificationRequest(id, verificationCode), CancellationToken.None);
        await fixture.PublishAsync("encrypted-event", "encrypted-email", "encrypted-delivery", true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.Handler.Posts, Has.Count.EqualTo(2));
            Assert.That(fixture.Handler.Posts[0].ContentEncoding, Is.EqualTo(new[] { "aes128gcm" }));
            Assert.That(fixture.Handler.Posts[1].ContentEncoding, Is.EqualTo(new[] { "aes128gcm" }));
            Assert.That(fixture.Handler.Posts[0].Body, Has.Length.GreaterThan(102));
            Assert.That(fixture.Handler.Posts[1].Body, Has.Length.GreaterThan(102));
            Assert.That(fixture.Handler.Posts[0].Body[16], Is.Zero);
            Assert.That(fixture.Handler.Posts[0].Body[17], Is.Zero);
            Assert.That(fixture.Handler.Posts[0].Body[18], Is.EqualTo(16));
            Assert.That(fixture.Handler.Posts[0].Body[19], Is.Zero);
            Assert.That(fixture.Handler.Posts[0].Body[20], Is.EqualTo(65));
            Assert.That(System.Text.Encoding.UTF8.GetString(fixture.Handler.Posts[1].Body),
                Does.Not.Contain("encrypted-email"));
            Assert.That(created.Created["created"].Url, Is.Null);
            Assert.That(created.Created["created"].Keys, Is.Null);
            Assert.That(created.Created["created"].VerificationCode, Is.Null);
        }
    }

    [Test]
    public async Task PublicEventSourceFiltersClosesReconnectsAndEmitsPings()
    {
        FakeJmapClock clock = new FakeJmapClock();
        JmapServerOptions serverOptions = new JmapServerOptions
        {
            MinimumEventSourcePingSeconds = 0
        };
        JmapServerServices services = new JmapServerServices
        {
            SessionService = new FakeJmapSessionService(Session()),
            Clock = clock
        };
        IJmapServer server = new JmapServer(serverOptions, services);
        ServerTransport transport = new ServerTransport(server, clock, false);
        JmapEventSourceReader reader = new JmapEventSourceReader(
            Session().EventSourceUrl!, transport);
        await reader.ConnectEventSourceAsync(
            new EventSourceOptions
            {
                Types = new List<string> { JmapDataTypes.Email },
                CloseAfterState = true
            },
            CancellationToken.None);

        await PublishAsync(services, "event-1", "email-1", "mailbox-1");
        JmapEvent first = await reader.ReadEventSourceAsync(CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(5));
        Task<JmapEvent> reconnecting = reader.ReadEventSourceAsync(CancellationToken.None);
        await WaitForAsync(() => transport.EventSourceRequests >= 2);
        await PublishAsync(services, "event-2", "email-2", "mailbox-2");
        JmapEvent second = await reconnecting.WaitAsync(TimeSpan.FromSeconds(5));
        reader.CloseEventSource();

        JmapServerServices pingServices = new JmapServerServices
        {
            SessionService = new FakeJmapSessionService(Session())
        };
        IJmapServer pingServer = new JmapServer(
            new JmapServerOptions { MinimumEventSourcePingSeconds = 0 },
            pingServices);
        ServerTransport pingTransport = new ServerTransport(
            pingServer, new FakeJmapClock(), false);
        JmapEventSourceReader pingReader = new JmapEventSourceReader(
            Session().EventSourceUrl!, pingTransport);
        await pingReader.ConnectEventSourceAsync(
            new EventSourceOptions { PingIntervalSeconds = 1 },
            CancellationToken.None);
        JmapEvent ping = await pingReader.ReadEventSourceAsync(CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(5));
        pingReader.CloseEventSource();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.Kind, Is.EqualTo(JmapEventKind.State));
            Assert.That(first.Id, Is.EqualTo("event-1"));
            Assert.That(first.StateChange!.Changed["account-1"], Is.EqualTo(new Dictionary<string, string>
            {
                [JmapDataTypes.Email] = "email-1"
            }));
            Assert.That(second.Id, Is.EqualTo("event-2"));
            Assert.That(second.StateChange!.Changed["account-1"].Keys,
                Is.EqualTo(new[] { JmapDataTypes.Email }));
            Assert.That(transport.LastEventIds, Is.EqualTo(new string?[] { null, "event-1" }));
            Assert.That(transport.CancelledStreams, Is.EqualTo(2));
            Assert.That(ping.Kind, Is.EqualTo(JmapEventKind.Ping));
            Assert.That(ping.Id, Is.Null);
            Assert.That(ping.HasPingIntervalSeconds, Is.True);
            Assert.That(ping.PingIntervalSeconds, Is.EqualTo(1));
            Assert.That(pingTransport.CancelledStreams, Is.EqualTo(1));
        }
    }

    private static PushSubscriptionSetRequest CreateRequest(bool encrypted)
    {
        PushSubscription subscription = new PushSubscription
        {
            DeviceClientId = encrypted ? "encrypted-device" : "plain-device",
            Url = "https://push.example.test/events",
            Types = new List<string> { JmapDataTypes.Email, JmapDataTypes.EmailDelivery }
        };
        if (encrypted)
        {
            subscription.Keys = new PushSubscriptionKeys
            {
                P256dh = "BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4",
                Auth = "BTBZMqHH6r4Tts7J_aSIgg"
            };
        }

        return new PushSubscriptionSetRequest
        {
            Create = new Dictionary<string, PushSubscription> { ["created"] = subscription }
        };
    }

    private static PushSubscriptionSetRequest VerificationRequest(string id, string code)
    {
        JmapPatchObject patch = new JmapPatchObject();
        patch.Values[JmapProperties.VerificationCode] = JsonSerializer.SerializeToElement(code);
        return new PushSubscriptionSetRequest
        {
            Update = new Dictionary<string, JmapPatchObject> { [id] = patch }
        };
    }

    private static JmapSession Session()
    {
        return PublicApiTestHelper.CoreSession("session-state");
    }

    private static JmapStatePublication Publication(
        string eventId,
        string emailState,
        string deliveryState,
        bool hasNewEmailDelivery)
    {
        JmapStatePublication publication = new JmapStatePublication
        {
            EventId = eventId,
            HasNewEmailDelivery = hasNewEmailDelivery
        };
        publication.StateChange.Changed["account-1"] = new Dictionary<string, string>
        {
            [JmapDataTypes.Email] = emailState,
            [JmapDataTypes.EmailDelivery] = deliveryState
        };
        return publication;
    }

    private static Task PublishAsync(
        JmapServerServices services,
        string eventId,
        string emailState,
        string mailboxState)
    {
        JmapStatePublication publication = new JmapStatePublication { EventId = eventId };
        publication.StateChange.Changed["account-1"] = new Dictionary<string, string>
        {
            [JmapDataTypes.Email] = emailState,
            [JmapDataTypes.Mailbox] = mailboxState
        };
        return services.StatePublisher!.PublishStateChangeAsync(
            publication, CancellationToken.None);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (!condition() && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        Assert.That(condition(), Is.True);
    }

    private sealed class PushFixture
    {
        public FakeJmapClock Clock = new FakeJmapClock();
        public StatefulPushService PushService = new StatefulPushService();
        public CapturingPushHandler Handler = new CapturingPushHandler();
        public IJmapCodec Codec = new JmapJsonCodec();
        public JmapServerServices Services;
        public IPushClient Client;

        public PushFixture(bool revoked)
        {
            Services = new JmapServerServices
            {
                SessionService = new FakeJmapSessionService(Session()),
                PushService = PushService,
                Clock = Clock,
                PushHttpClient = new HttpClient(Handler)
            };
            IJmapServer server = new JmapServer(
                new JmapServerOptions { PushTimeToLiveSeconds = 123 }, Services);
            ServerTransport transport = new ServerTransport(server, Clock, revoked);
            Client = new JmapClient(ClientOptions(), transport);
            Handler.Client = Client;
            Handler.PushService = PushService;
            Handler.Publisher = Services.StatePublisher;
        }

        public Task PublishAsync(
            string eventId,
            string emailState,
            string deliveryState,
            bool hasNewEmailDelivery)
        {
            return Services.StatePublisher!.PublishStateChangeAsync(
                Publication(eventId, emailState, deliveryState, hasNewEmailDelivery),
                CancellationToken.None);
        }
    }

    private sealed class CapturedPost
    {
        public byte[] Body = Array.Empty<byte>();
        public Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string? ContentType;
        public List<string> ContentEncoding = new List<string>();
    }

    private sealed class CapturingPushHandler : HttpMessageHandler
    {
        public Queue<HttpStatusCode> Statuses = new Queue<HttpStatusCode>();
        public List<CapturedPost> Posts = new List<CapturedPost>();
        public IPushClient? Client;
        public StatefulPushService? PushService;
        public IJmapStatePublisher? Publisher;
        public bool ConfirmDuringVerification;
        public bool BackendWasCommitted;
        public int PostCountBeforeConfirmation;
        public JmapSetErrorKind BadVerificationError;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CapturedPost post = new CapturedPost();
            post.Body = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                post.Headers[header.Key] = string.Join(",", header.Value);
            }

            post.ContentType = request.Content.Headers.ContentType!.ToString();
            post.ContentEncoding.AddRange(request.Content.Headers.ContentEncoding);
            Posts.Add(post);

            bool isVerification = false;
            if (ConfirmDuringVerification && post.ContentEncoding.Count == 0)
            {
                using JsonDocument document = JsonDocument.Parse(post.Body);
                JsonElement type;
                isVerification = document.RootElement.TryGetProperty("@type", out type) &&
                    type.GetString() == "PushVerification";
            }

            if (isVerification)
            {
                IJmapCodec codec = new JmapJsonCodec();
                PushVerification verification = codec.DecodePushVerificationBytes(post.Body);
                BackendWasCommitted = PushService!.Contains(verification.PushSubscriptionId!);
                await Publisher!.PublishStateChangeAsync(
                    Publication("pre-verification", "blocked", "blocked", true),
                    cancellationToken);
                PostCountBeforeConfirmation = Posts.Count;
                PushSubscriptionSetResponse bad = await Client!.SetPushSubscriptionsAsync(
                    VerificationRequest(
                        verification.PushSubscriptionId!,
                        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"),
                    cancellationToken);
                BadVerificationError = bad.NotUpdated![verification.PushSubscriptionId!].Kind;
                await Client.SetPushSubscriptionsAsync(
                    VerificationRequest(
                        verification.PushSubscriptionId!,
                        verification.VerificationCode!),
                    cancellationToken);
            }

            HttpStatusCode status = Statuses.Count == 0 ?
                HttpStatusCode.NoContent : Statuses.Dequeue();
            HttpResponseMessage response = new HttpResponseMessage(status);
            if (status == HttpStatusCode.TooManyRequests)
            {
                response.Headers.RetryAfter = new RetryConditionHeaderValue(
                    TimeSpan.FromSeconds(120));
            }

            return response;
        }
    }

    private sealed class StatefulPushService : IPushService
    {
        public Dictionary<string, PushSubscription> Stored = new Dictionary<string, PushSubscription>(StringComparer.Ordinal);
        public Dictionary<string, string> Owners = new Dictionary<string, string>(StringComparer.Ordinal);
        public int NextId;

        public Task<PushSubscriptionGetResponse> GetStoredPushSubscriptionsAsync(
            PushSubscriptionGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PushSubscriptionGetResponse response = new PushSubscriptionGetResponse();
            IEnumerable<string> ids = request.Ids == null ? Stored.Keys : request.Ids;
            foreach (string id in ids)
            {
                if (Stored.ContainsKey(id) && Owners[id] == context.CredentialId)
                {
                    response.List.Add(Clone(Stored[id]));
                }
                else if (request.Ids != null)
                {
                    response.NotFound.Add(id);
                }
            }

            return Task.FromResult(response);
        }

        public Task<PushSubscriptionSetResponse> SetStoredPushSubscriptionsAsync(
            PushSubscriptionSetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PushSubscriptionSetResponse response = new PushSubscriptionSetResponse();
            if (request.Create != null)
            {
                response.Created = new Dictionary<string, PushSubscription>();
                foreach (KeyValuePair<string, PushSubscription> item in request.Create)
                {
                    NextId = NextId + 1;
                    string id = "push-" + NextId;
                    PushSubscription stored = Clone(item.Value);
                    stored.Id = id;
                    Stored[id] = stored;
                    Owners[id] = context.CredentialId!;
                    response.Created[item.Key] = new PushSubscription
                    {
                        Id = id,
                        Expires = item.Value.Expires
                    };
                }
            }

            if (request.Update != null)
            {
                foreach (KeyValuePair<string, JmapPatchObject> item in request.Update)
                {
                    if (Stored.ContainsKey(item.Key) && Owners[item.Key] == context.CredentialId)
                    {
                        if (response.Updated == null)
                        {
                            response.Updated = new Dictionary<string, JsonElement>();
                        }

                        response.Updated[item.Key] = JsonSerializer.SerializeToElement<object?>(null);
                    }
                    else
                    {
                        if (response.NotUpdated == null)
                        {
                            response.NotUpdated = new Dictionary<string, JmapSetError>();
                        }

                        response.NotUpdated[item.Key] = new JmapSetError
                        {
                            Type = JmapErrorCodes.NotFound
                        };
                    }
                }
            }

            if (request.Destroy != null)
            {
                foreach (string id in request.Destroy)
                {
                    if (Stored.ContainsKey(id) && Owners[id] == context.CredentialId)
                    {
                        Stored.Remove(id);
                        Owners.Remove(id);
                        if (response.Destroyed == null)
                        {
                            response.Destroyed = new List<string>();
                        }

                        response.Destroyed.Add(id);
                    }
                    else
                    {
                        if (response.NotDestroyed == null)
                        {
                            response.NotDestroyed = new Dictionary<string, JmapSetError>();
                        }

                        response.NotDestroyed[id] = new JmapSetError
                        {
                            Type = JmapErrorCodes.NotFound
                        };
                    }
                }
            }

            return Task.FromResult(response);
        }

        public bool Contains(string id)
        {
            return Stored.ContainsKey(id);
        }

        public string GetVerificationCode(string id)
        {
            return Stored[id].VerificationCode!;
        }

        private static PushSubscription Clone(PushSubscription value)
        {
            return new PushSubscription
            {
                Id = value.Id,
                DeviceClientId = value.DeviceClientId,
                Url = value.Url,
                Keys = value.Keys == null ? null : new PushSubscriptionKeys
                {
                    P256dh = value.Keys.P256dh,
                    Auth = value.Keys.Auth
                },
                VerificationCode = value.VerificationCode,
                Expires = value.Expires,
                Types = value.Types == null ? null : new List<string>(value.Types)
            };
        }
    }

    private sealed class ServerTransport : IJmapTransport
    {
        public IJmapServer Server;
        public IJmapCodec Codec = new JmapJsonCodec();
        public FakeJmapClock Clock;
        public bool Revoked;
        public int EventSourceRequests;
        public int CancelledStreams;
        public List<string?> LastEventIds = new List<string?>();

        public ServerTransport(IJmapServer server, FakeJmapClock clock, bool revoked)
        {
            Server = server;
            Clock = clock;
            Revoked = revoked;
        }

        public async Task<JmapSession> GetTransportSessionAsync(
            string sessionUrl,
            CancellationToken cancellationToken)
        {
            JmapEndpointResponse response = await Server.ProcessSessionEndpointAsync(
                new JmapEndpointRequest
                {
                    Method = "GET",
                    Context = Context()
                },
                cancellationToken);
            return Codec.DecodeSessionMemory(response.BodyBytes!);
        }

        public async Task<JmapResponse> SendTransportApiRequestAsync(
            string apiUrl,
            JmapRequest request,
            CancellationToken cancellationToken)
        {
            byte[] body = Codec.EncodeRequestBytes(request);
            JmapEndpointResponse response = await Server.ProcessApiEndpointAsync(
                new JmapEndpointRequest
                {
                    Method = "POST",
                    ContentType = JmapMediaTypes.Json,
                    Body = new MemoryStream(body, false),
                    ContentLength = body.Length,
                    HasContentLength = true,
                    Context = Context()
                },
                cancellationToken);
            Assert.That(response.StatusCode, Is.EqualTo(200));
            return Codec.DecodeResponseMemory(response.BodyBytes!);
        }

        public Task<BlobUploadResponse> SendTransportUploadAsync(
            string uploadUrl,
            BlobUploadRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<BlobDownloadResponse> SendTransportDownloadAsync(
            string downloadUrl,
            BlobDownloadRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public async Task<Stream> OpenTransportEventSourceAsync(
            string eventSourceUrl,
            string? lastEventId,
            CancellationToken cancellationToken)
        {
            EventSourceRequests = EventSourceRequests + 1;
            LastEventIds.Add(lastEventId);
            Uri uri = new Uri(eventSourceUrl);
            JmapEndpointRequest request = new JmapEndpointRequest
            {
                Method = "GET",
                Context = Context()
            };
            foreach (string item in uri.Query.TrimStart('?').Split(
                '&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] pair = item.Split('=', 2);
                request.QueryParameters[Uri.UnescapeDataString(pair[0])] =
                    pair.Length == 1 ? "" : Uri.UnescapeDataString(pair[1]);
            }

            if (lastEventId != null)
            {
                request.Headers["Last-Event-ID"] = lastEventId;
            }

            CancellationTokenSource requestCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            JmapEndpointResponse response = await Server.ProcessEventSourceEndpointAsync(
                request, requestCancellation.Token);
            Assert.That(response.StatusCode, Is.EqualTo(200));
            return new CancelOnDisposeStream(
                response.BodyStream!, requestCancellation, this);
        }

        private JmapRequestContext Context()
        {
            return new JmapRequestContext
            {
                UserId = "user-1",
                CredentialId = "credential-1",
                CredentialIsRevoked = Revoked,
                HasCredentialExpiresUtc = true,
                CredentialExpiresUtc = new DateTimeOffset(
                    2026, 9, 10, 12, 5, 0, TimeSpan.Zero),
                IsSecure = true
            };
        }
    }

    private sealed class CancelOnDisposeStream : Stream
    {
        public Stream Inner;
        public CancellationTokenSource Cancellation;
        public ServerTransport Owner;
        public bool IsDisposed;

        public CancelOnDisposeStream(
            Stream inner,
            CancellationTokenSource cancellation,
            ServerTransport owner)
        {
            Inner = inner;
            Cancellation = cancellation;
            Owner = owner;
        }

        public override bool CanRead => Inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Inner.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            return Inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                Owner.CancelledStreams = Owner.CancelledStreams + 1;
                Cancellation.Cancel();
                Inner.Dispose();
                Cancellation.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private static JmapClientOptions ClientOptions()
    {
        JmapClientOptions options = new JmapClientOptions();
        options.DiscoveryOptions.SessionUrl = "https://jmap.example.test/session";
        return options;
    }
}
