using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class JmapServerTests
{
    [Test]
    public void ConstructorRejectsMissingConfigurationDependencies()
    {
        Assert.That(() => new JmapServer(null!, new JmapServerServices()),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("options"));
        Assert.That(() => new JmapServer(new JmapServerOptions(), null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("services"));
    }

    [Test]
    public async Task SessionEndpointReturnsValidatedJsonCachingAndSelectedLanguage()
    {
        FakeJmapSessionService sessions = new FakeJmapSessionService(
            PublicApiTestHelper.CoreSession("session-state"));
        JmapServerServices services = new JmapServerServices
        {
            SessionService = sessions
        };
        IJmapServer server = new JmapServer(new JmapServerOptions(), services);
        JmapEndpointRequest request = Request("GET");
        request.Context.AcceptLanguage = "en;q=0.4, de-DE;q=0.9";

        JmapEndpointResponse response = await server.ProcessSessionEndpointAsync(
            request, CancellationToken.None);
        JmapSession decoded = new JmapJsonCodec().DecodeSessionMemory(response.BodyBytes!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(response.ContentType, Is.EqualTo(JmapMediaTypes.Json));
            Assert.That(response.HasContentLength, Is.True);
            Assert.That(response.ContentLength, Is.EqualTo(response.BodyBytes!.Length));
            Assert.That(response.Headers["Cache-Control"], Is.EqualTo("private, no-cache"));
            Assert.That(response.Headers["Content-Language"], Is.EqualTo("de-DE"));
            Assert.That(decoded.State, Is.EqualTo("session-state"));
            Assert.That(server.GetServerOptions(), Is.Not.Null);
            Assert.That(server.GetServerServices(), Is.SameAs(services));
        }
    }

    [Test]
    public async Task ApiEndpointDispatchesEchoAndClassifiesMalformedInputAsProblems()
    {
        JmapServerServices services = new JmapServerServices
        {
            SessionService = new FakeJmapSessionService(
                PublicApiTestHelper.CoreSession("session-state"))
        };
        IJmapServer server = new JmapServer(new JmapServerOptions(), services);
        JmapEndpointRequest echo = ApiRequest(
            "{\"using\":[\"urn:ietf:params:jmap:core\"],\"methodCalls\":[[\"Core/echo\",{\"answer\":42},\"c1\"]]}");
        JmapEndpointRequest malformed = ApiRequest("{not-json");
        JmapEndpointRequest wrongShape = ApiRequest("[]");

        JmapEndpointResponse echoed = await server.ProcessApiEndpointAsync(
            echo, CancellationToken.None);
        JmapEndpointResponse notJson = await server.ProcessApiEndpointAsync(
            malformed, CancellationToken.None);
        JmapEndpointResponse notRequest = await server.ProcessApiEndpointAsync(
            wrongShape, CancellationToken.None);
        JmapResponse decoded = new JmapJsonCodec().DecodeResponseMemory(echoed.BodyBytes!);
        JmapProblem jsonProblem = new JmapJsonCodec().DecodeProblemMemory(notJson.BodyBytes!);
        JmapProblem requestProblem = new JmapJsonCodec().DecodeProblemMemory(notRequest.BodyBytes!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(echoed.StatusCode, Is.EqualTo(200));
            Assert.That(echoed.Headers["Cache-Control"], Is.EqualTo("no-store"));
            Assert.That(decoded.MethodResponses[0].Name, Is.EqualTo(JmapMethods.CoreEcho));
            Assert.That(decoded.MethodResponses[0].Arguments.GetProperty("answer").GetInt32(), Is.EqualTo(42));
            Assert.That(decoded.SessionState, Is.EqualTo("session-state"));
            Assert.That(notJson.StatusCode, Is.EqualTo(400));
            Assert.That(notJson.ContentType, Is.EqualTo(JmapMediaTypes.ProblemJson));
            Assert.That(jsonProblem.Kind, Is.EqualTo(JmapProblemKind.NotJson));
            Assert.That(requestProblem.Kind, Is.EqualTo(JmapProblemKind.NotRequest));
        }
    }

    [Test]
    public async Task UploadAndDownloadEndpointsPreserveBufferedMetadataAndBinaryStream()
    {
        FakeJmapBlobService blobs = new FakeJmapBlobService
        {
            UploadResponse = new BlobUploadResponse
            {
                AccountId = "account-1",
                BlobId = "blob-1",
                Type = JmapMediaTypes.OctetStream,
                Size = 4
            },
            DownloadContent = new JmapDownloadContent
            {
                Content = new MemoryStream(new byte[] { 1, 2, 3, 4 }),
                MediaType = JmapMediaTypes.OctetStream,
                FileName = "data.bin",
                Size = 4,
                HasSize = true
            }
        };
        JmapServerServices services = new JmapServerServices
        {
            SessionService = new FakeJmapSessionService(
                PublicApiTestHelper.CoreSession("session-state")),
            BlobService = blobs
        };
        IJmapServer server = new JmapServer(new JmapServerOptions(), services);
        byte[] uploaded = new byte[] { 1, 2, 3, 4 };
        JmapEndpointRequest upload = Request("POST");
        upload.ContentType = JmapMediaTypes.OctetStream;
        upload.Body = new MemoryStream(uploaded);
        upload.ContentLength = uploaded.Length;
        upload.HasContentLength = true;
        upload.RouteValues.Add(JmapProperties.AccountId, "account-1");

        JmapEndpointResponse stored = await server.ProcessUploadEndpointAsync(
            upload, CancellationToken.None);
        JmapEndpointRequest download = Request("GET");
        download.RouteValues.Add(JmapProperties.AccountId, "account-1");
        download.RouteValues.Add(JmapProperties.BlobId, "blob-1");
        download.QueryParameters.Add(JmapProperties.Type, JmapMediaTypes.OctetStream);
        download.QueryParameters.Add(JmapProperties.Name, "data.bin");
        JmapEndpointResponse opened = await server.ProcessDownloadEndpointAsync(
            download, CancellationToken.None);

        using MemoryStream copy = new MemoryStream();
        await opened.BodyStream!.CopyToAsync(copy);
        BlobUploadResponse uploadResult = new JmapJsonCodec().Push.DecodeBlobUploadResponse(
            JsonDocument.Parse(stored.BodyBytes!).RootElement);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stored.StatusCode, Is.EqualTo(201));
            Assert.That(stored.HasContentLength, Is.True);
            Assert.That(uploadResult.BlobId, Is.EqualTo("blob-1"));
            Assert.That(uploadResult.Size, Is.EqualTo(4));
            Assert.That(opened.StatusCode, Is.EqualTo(200));
            Assert.That(opened.BodyBytes, Is.Null);
            Assert.That(opened.BodyStream, Is.SameAs(blobs.DownloadContent!.Content));
            Assert.That(copy.ToArray(), Is.EqualTo(uploaded));
            Assert.That(opened.Headers["Cache-Control"], Does.StartWith("private, immutable"));
            Assert.That(opened.Headers["Content-Disposition"], Does.Contain("data.bin"));
        }
    }

    [Test]
    public async Task EventSourceStreamsPublishedStateAndFansOutToExistingPublisher()
    {
        FakeJmapStatePublisher external = new FakeJmapStatePublisher();
        JmapServerServices services = new JmapServerServices
        {
            SessionService = new FakeJmapSessionService(
                PublicApiTestHelper.CoreSession("session-state")),
            StatePublisher = external
        };
        IJmapServer server = new JmapServer(new JmapServerOptions(), services);
        JmapEndpointRequest request = Request("GET");
        request.QueryParameters.Add(JmapProperties.Types, JmapDataTypes.Email);
        request.QueryParameters.Add(JmapProperties.CloseAfter, "state");
        request.QueryParameters.Add(JmapProperties.Ping, "30");

        JmapEndpointResponse response = await server.ProcessEventSourceEndpointAsync(
            request, CancellationToken.None);
        JmapStatePublication publication = new JmapStatePublication
        {
            EventId = "event-1"
        };
        publication.StateChange.Changed.Add("account-1", new Dictionary<string, string>
        {
            [JmapDataTypes.Email] = "email-state"
        });
        await services.StatePublisher!.PublishStateChangeAsync(
            publication, CancellationToken.None);
        using StreamReader reader = new StreamReader(response.BodyStream!, Encoding.UTF8);
        string wire = await reader.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(5));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(response.ContentType, Is.EqualTo(JmapMediaTypes.EventStream));
            Assert.That(response.BodyBytes, Is.Null);
            Assert.That(response.HasContentLength, Is.False);
            Assert.That(response.Headers["Cache-Control"], Is.EqualTo("no-cache"));
            Assert.That(wire, Does.Contain("id: event-1"));
            Assert.That(wire, Does.Contain("event: state"));
            Assert.That(wire, Does.Contain("email-state"));
            Assert.That(external.Publications, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task EndpointMethodAuthenticationMediaAndSizeFailuresUseHttpResponses()
    {
        JmapServerOptions options = new JmapServerOptions
        {
            MaxRequestBytes = 20
        };
        JmapServerServices services = new JmapServerServices
        {
            SessionService = new FakeJmapSessionService(
                PublicApiTestHelper.CoreSession("session-state"))
        };
        IJmapServer server = new JmapServer(options, services);
        JmapEndpointRequest wrongMethod = Request("DELETE");
        JmapEndpointRequest unauthenticated = Request("GET");
        unauthenticated.Context.UserId = null;
        JmapEndpointRequest wrongMedia = ApiRequest("{}");
        wrongMedia.ContentType = "text/plain";
        JmapEndpointRequest tooLarge = ApiRequest(new string(' ', 21));

        JmapEndpointResponse method = await server.ProcessSessionEndpointAsync(
            wrongMethod, CancellationToken.None);
        JmapEndpointResponse auth = await server.ProcessEventSourceEndpointAsync(
            unauthenticated, CancellationToken.None);
        JmapEndpointResponse media = await server.ProcessApiEndpointAsync(
            wrongMedia, CancellationToken.None);
        JmapEndpointResponse size = await server.ProcessApiEndpointAsync(
            tooLarge, CancellationToken.None);
        JmapProblem sizeProblem = new JmapJsonCodec().DecodeProblemMemory(size.BodyBytes!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(method.StatusCode, Is.EqualTo(405));
            Assert.That(method.Headers["Allow"], Is.EqualTo("GET"));
            Assert.That(auth.StatusCode, Is.EqualTo(401));
            Assert.That(media.StatusCode, Is.EqualTo(415));
            Assert.That(size.StatusCode, Is.EqualTo(400));
            Assert.That(sizeProblem.Kind, Is.EqualTo(JmapProblemKind.Limit));
            Assert.That(sizeProblem.Limit, Is.EqualTo(JmapProperties.MaxSizeRequest));
        }
    }

    [Test]
    public void SubmissionPolicyErrorsUseTheRequestLanguageOnlyForUserVisibleText()
    {
        RecordingStringLocalizer localizer = new RecordingStringLocalizer();
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        method.Errors = new ErrorFactory(localizer);
        EmailSubmissionSetResponse response = new EmailSubmissionSetResponse
        {
            NotCreated = new Dictionary<string, JmapSetError>
            {
                ["submission-1"] = new JmapSetError
                {
                    Type = JmapErrorCodes.ForbiddenToSend,
                    Description = "Sending is blocked."
                },
                ["submission-2"] = new JmapSetError
                {
                    Type = JmapErrorCodes.InvalidEmail,
                    Description = "Invalid message."
                }
            }
        };
        JmapRequestContext context = new JmapRequestContext
        {
            AcceptLanguage = "fr-CA, en;q=0.5"
        };

        method.LocalizeUserVisibleSetErrors(response, context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.NotCreated["submission-1"].Description,
                Is.EqualTo("fr-CA:submission.sendingBlocked"));
            Assert.That(response.NotCreated["submission-2"].Description,
                Is.EqualTo("Invalid message."));
            Assert.That(localizer.Calls, Is.EqualTo(1));
            Assert.That(localizer.AcceptLanguage, Is.EqualTo("fr-CA, en;q=0.5"));
        }
    }

    private static JmapEndpointRequest Request(string method)
    {
        JmapEndpointRequest request = new JmapEndpointRequest();
        request.Method = method;
        request.Context = PublicApiTestHelper.AuthenticatedContext();
        return request;
    }

    private static JmapEndpointRequest ApiRequest(string json)
    {
        return PublicApiTestHelper.JsonEndpointRequest(json);
    }

    private class RecordingStringLocalizer : IJmapStringLocalizer
    {
        public int Calls;
        public string? AcceptLanguage;

        public string LocalizeJmapString(string key, string? acceptLanguage)
        {
            Calls = Calls + 1;
            AcceptLanguage = acceptLanguage;
            return "fr-CA:" + key;
        }
    }
}
