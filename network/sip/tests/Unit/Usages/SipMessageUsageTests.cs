namespace sip.Tests;

public class SipMessageUsageTests
{
    [Test]
    public void OutOfDialogSendBuildsMessageAndReportsDelivery()
    {
        SipMessageClient client = new SipMessageClient();
        SipInstantMessageRequest request = Request("sip:bob@example.com", "hello");
        request.CorrelationId = "message-1";

        SipInstantMessageEvent created = client.SendInstantMessage(request);
        SipMessage response = Response(created.Message!, SipStatusCodes.Accepted);
        SipInstantMessageEvent delivered =
            client.ReceiveInstantMessageResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(created.Kind,
                Is.EqualTo(SipInstantMessageEventKind.RequestCreated));
            Assert.That(created.Message!.Method, Is.EqualTo(SipMethod.Message));
            Assert.That(created.Message.Body,
                Is.EqualTo(System.Text.Encoding.UTF8.GetBytes("hello")));
            Assert.That(created.Message.ContentType!.Type, Is.EqualTo("text"));
            Assert.That(Header(created.Message, SipHeaderNames.MaxForwards),
                Is.EqualTo("70"));
            Assert.That(created.Message.From!.Uri!.User, Is.EqualTo("alice"));
            Assert.That(created.Final, Is.False);
            Assert.That(delivered.Kind,
                Is.EqualTo(SipInstantMessageEventKind.Delivered));
            Assert.That(delivered.Success, Is.True);
            Assert.That(delivered.Final, Is.True);
            Assert.That(delivered.StatusCode, Is.EqualTo(202));
            Assert.That(delivered.CorrelationId, Is.EqualTo("message-1"));
        });
    }

    [Test]
    public void ConcurrentResponsesAreCorrelatedOutOfOrder()
    {
        SipMessageClient client = new SipMessageClient();
        SipInstantMessageEvent first = client.SendInstantMessage(
            Request("sip:first@example.com", "one"));
        SipInstantMessageRequest secondRequest =
            Request("sip:second@example.com", "two");
        secondRequest.CorrelationId = "second";
        SipInstantMessageEvent second = client.SendInstantMessage(secondRequest);

        SipInstantMessageEvent secondResult = client.ReceiveInstantMessageResponse(
            Response(second.Message!, SipStatusCodes.Ok));
        SipInstantMessageEvent firstResult = client.ReceiveInstantMessageResponse(
            Response(first.Message!, SipStatusCodes.NotFound));

        Assert.Multiple(() =>
        {
            Assert.That(secondResult.Success, Is.True);
            Assert.That(secondResult.CorrelationId, Is.EqualTo("second"));
            Assert.That(firstResult.Success, Is.False);
            Assert.That(firstResult.StatusCode, Is.EqualTo(404));
        });
    }

    [Test]
    public void SendValidatesContentAndSizePolicy()
    {
        SipMessageClient client = new SipMessageClient();
        SipInstantMessageRequest missingType =
            Request("sip:bob@example.com", "body");
        missingType.ContentType = null;
        SipInstantMessageRequest tooLarge =
            Request("sip:bob@example.com", "body");
        tooLarge.Options.MaxBodyBytes = 3;

        Assert.Multiple(() =>
        {
            Assert.That(() => client.SendInstantMessage(missingType),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => client.SendInstantMessage(tooLarge),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void RedirectUsesHighestQualityContactAndPreservesIdentity()
    {
        SipMessageClient client = new SipMessageClient();
        SipInstantMessageEvent created = client.SendInstantMessage(
            Request("sip:original@example.com", "hello"));
        string originalCallId = Header(created.Message!, SipHeaderNames.CallId)!;
        string originalFrom = Header(created.Message!, SipHeaderNames.From)!;
        SipMessage redirect = Response(
            created.Message!, SipStatusCodes.MovedTemporarily);
        redirect.ContactValues = new SipContactValue[]
        {
            Contact("sip:lower@example.net", 100),
            Contact("sip:preferred@example.net", 900)
        };

        SipInstantMessageEvent retry =
            client.ReceiveInstantMessageResponse(redirect);

        Assert.Multiple(() =>
        {
            Assert.That(retry.Kind,
                Is.EqualTo(SipInstantMessageEventKind.RetryCreated));
            Assert.That(retry.RedirectCount, Is.EqualTo(1));
            Assert.That(retry.Message!.RequestUri!.User, Is.EqualTo("preferred"));
            Assert.That(retry.Message.CSeq!.SequenceNumber, Is.EqualTo(2));
            Assert.That(Header(retry.Message, SipHeaderNames.CallId),
                Is.EqualTo(originalCallId));
            Assert.That(Header(retry.Message, SipHeaderNames.From),
                Is.EqualTo(originalFrom));
        });
    }

    [Test]
    public void RedirectPolicyAndSipsDowngradeProduceFinalFailure()
    {
        SipMessageClient disabledClient = new SipMessageClient();
        SipInstantMessageRequest disabledRequest =
            Request("sip:bob@example.com", "hello");
        disabledRequest.Options.FollowRedirects = false;
        SipInstantMessageEvent disabled =
            disabledClient.SendInstantMessage(disabledRequest);
        SipMessage disabledResponse = Response(
            disabled.Message!, SipStatusCodes.MovedTemporarily);
        disabledResponse.ContactValues = new SipContactValue[]
        {
            Contact("sip:new@example.net", 1000)
        };

        SipInstantMessageEvent disabledResult =
            disabledClient.ReceiveInstantMessageResponse(
            disabledResponse);

        SipMessageClient secureClient = new SipMessageClient();
        SipInstantMessageEvent secure = secureClient.SendInstantMessage(
            Request("sips:bob@example.com", "hello"));
        SipMessage downgrade = Response(
            secure.Message!, SipStatusCodes.MovedTemporarily);
        downgrade.ContactValues = new SipContactValue[]
        {
            Contact("sip:insecure@example.net", 1000)
        };
        SipInstantMessageEvent secureResult =
            secureClient.ReceiveInstantMessageResponse(downgrade);

        Assert.Multiple(() =>
        {
            Assert.That(disabledResult.Kind,
                Is.EqualTo(SipInstantMessageEventKind.Failed));
            Assert.That(disabledResult.Final, Is.True);
            Assert.That(secureResult.Kind,
                Is.EqualTo(SipInstantMessageEventKind.Failed));
            Assert.That(secureResult.ErrorMessage, Does.Contain("downgrade"));
        });
    }

    [Test]
    public void AuthenticationChallengeCreatesCorrelatedRetry()
    {
        RecordingMessageAuthenticationClient authentication =
            new RecordingMessageAuthenticationClient();
        SipMessageClient client = new SipMessageClient();
        client.ConfigureInstantMessageAuthenticationClient(authentication);
        SipInstantMessageRequest request =
            Request("sip:bob@example.com", "protected");
        request.AuthenticationContextId = "auth-context";
        SipInstantMessageEvent created = client.SendInstantMessage(request);
        SipMessage challenge = Response(
            created.Message!, SipStatusCodes.Unauthorized);
        AddHeader(challenge, SipHeaderNames.WwwAuthenticate,
            "Digest realm=\"example.com\", nonce=\"one\", algorithm=SHA-256");

        SipInstantMessageEvent retry =
            client.ReceiveInstantMessageResponse(challenge);
        SipInstantMessageEvent delivered = client.ReceiveInstantMessageResponse(
            Response(retry.Message!, SipStatusCodes.Ok));

        Assert.Multiple(() =>
        {
            Assert.That(retry.AuthenticationAttemptCount, Is.EqualTo(1));
            Assert.That(retry.Message!.CSeq!.SequenceNumber, Is.EqualTo(2));
            Assert.That(Header(retry.Message, SipHeaderNames.Authorization),
                Is.EqualTo("Digest test-response"));
            Assert.That(authentication.AuthorizationCorrelation,
                Is.EqualTo("auth-context"));
            Assert.That(authentication.VerificationCount, Is.EqualTo(1));
            Assert.That(delivered.Kind,
                Is.EqualTo(SipInstantMessageEventKind.Delivered));
        });
    }

    [Test]
    public void InDialogSendUsesDialogRoutingAndSequence()
    {
        SipInstantMessageRequest request =
            Request("sip:ignored@example.com", "inside");
        request.Dialog = Dialog();
        SipMessageClient client = new SipMessageClient();

        SipInstantMessageEvent created = client.SendInstantMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(created.InDialog, Is.True);
            Assert.That(created.Message!.RequestUri!.Host,
                Is.EqualTo("device.example.net"));
            Assert.That(created.Message.RouteValues, Has.Length.EqualTo(1));
            Assert.That(created.Message.RouteValues[0].Address!.Uri!.Host,
                Is.EqualTo("proxy.example.net"));
            Assert.That(created.Message.CSeq!.SequenceNumber, Is.EqualTo(43));
            Assert.That(Header(created.Message, SipHeaderNames.CallId),
                Is.EqualTo("dialog-call"));
            Assert.That(Header(created.Message, SipHeaderNames.From),
                Does.Contain("local-tag"));
            Assert.That(Header(created.Message, SipHeaderNames.To),
                Does.Contain("remote-tag"));
        });
    }

    [Test]
    public void ServerAcceptsMessageAndQueuesReceivedEvent()
    {
        SipMessage request = OutboundMessage("hello");
        SipMessageServer server = new SipMessageServer();

        SipInstantMessageEvent received = server.ReceiveInstantMessage(
            request, new SipInstantMessageServerOptions());
        SipInstantMessageEvent queued =
            server.PollReceivedInstantMessageEvent();

        Assert.Multiple(() =>
        {
            Assert.That(received.Kind,
                Is.EqualTo(SipInstantMessageEventKind.Received));
            Assert.That(received.Success, Is.True);
            Assert.That(received.StatusCode, Is.EqualTo(200));
            Assert.That(received.Response!.CSeq!.Method,
                Is.EqualTo(SipMethod.Message));
            Assert.That(queued.Message!.Body,
                Is.EqualTo(System.Text.Encoding.UTF8.GetBytes("hello")));
            Assert.That(queued.CorrelationId,
                Is.EqualTo(Header(request, SipHeaderNames.CallId)));
        });
    }

    [Test]
    public void ServerEnforcesBodySizeAndAcceptedMediaTypes()
    {
        SipMessageServer server = new SipMessageServer();
        SipInstantMessageServerOptions small =
            new SipInstantMessageServerOptions();
        small.MaxBodyBytes = 4;
        SipInstantMessageEvent tooLarge = server.ReceiveInstantMessage(
            OutboundMessage("hello"), small);

        SipInstantMessageServerOptions types =
            new SipInstantMessageServerOptions();
        types.AcceptedContentTypes = new SipMediaType[]
        {
            MediaType("application", "json")
        };
        SipInstantMessageEvent unsupported = server.ReceiveInstantMessage(
            OutboundMessage("hello"), types);

        Assert.Multiple(() =>
        {
            Assert.That(tooLarge.StatusCode, Is.EqualTo(413));
            Assert.That(tooLarge.Success, Is.False);
            Assert.That(unsupported.StatusCode, Is.EqualTo(415));
            Assert.That(Header(unsupported.Response!, SipHeaderNames.Accept),
                Is.EqualTo("application/json"));
        });
    }

    [Test]
    public void ServerAppliesDialogAndAuthenticationPolicies()
    {
        SipMessage inDialog = InDialogMessage();
        SipMessageServer dialogServer = new SipMessageServer();
        SipInstantMessageServerOptions dialogPolicy =
            new SipInstantMessageServerOptions();
        dialogPolicy.AllowInDialog = false;
        SipInstantMessageEvent dialogRejected =
            dialogServer.ReceiveInstantMessage(inDialog, dialogPolicy);

        RecordingMessageAuthenticationServer authentication =
            new RecordingMessageAuthenticationServer();
        SipMessageServer protectedServer = new SipMessageServer();
        protectedServer.ConfigureInstantMessageAuthenticationServer(
            authentication, new SipNonceOptions());
        SipInstantMessageServerOptions protectedPolicy =
            new SipInstantMessageServerOptions();
        protectedPolicy.RequireAuthentication = true;
        SipMessage unauthenticated = OutboundMessage("protected");
        SipInstantMessageEvent challenged = protectedServer.ReceiveInstantMessage(
            unauthenticated, protectedPolicy);
        AddHeader(unauthenticated, SipHeaderNames.Authorization,
            "Digest test-response");
        SipInstantMessageEvent accepted = protectedServer.ReceiveInstantMessage(
            unauthenticated, protectedPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(dialogRejected.StatusCode, Is.EqualTo(481));
            Assert.That(dialogRejected.InDialog, Is.True);
            Assert.That(challenged.StatusCode, Is.EqualTo(401));
            Assert.That(Header(challenged.Response!,
                SipHeaderNames.WwwAuthenticate), Does.StartWith("Digest"));
            Assert.That(accepted.Kind,
                Is.EqualTo(SipInstantMessageEventKind.Received));
            Assert.That(accepted.Success, Is.True);
        });
    }

    [Test]
    public void EmptyBodyAndProvisionalResponseKeepOrderedDetachedEvents()
    {
        SipMessageClient client = new SipMessageClient();
        SipInstantMessageRequest request =
            Request("sip:bob@example.com", "");
        request.ContentType = null;
        request.CorrelationId = "message-events";

        SipInstantMessageEvent created = client.SendInstantMessage(request);
        request.Target!.Host = "changed.example.net";
        SipInstantMessageEvent provisional =
            client.ReceiveInstantMessageResponse(
                Response(created.Message!, 180));
        SipInstantMessageEvent delivered =
            client.ReceiveInstantMessageResponse(
                Response(created.Message!, SipStatusCodes.Ok));
        SipInstantMessageEvent queuedCreated =
            client.PollInstantMessageEvent();
        SipInstantMessageEvent queuedProvisional =
            client.PollInstantMessageEvent();
        SipInstantMessageEvent queuedDelivered =
            client.PollInstantMessageEvent();
        SipInstantMessageEvent empty = client.PollInstantMessageEvent();

        Assert.Multiple(() =>
        {
            Assert.That(created.Message!.Body, Is.Empty);
            Assert.That(created.Message.ContentType, Is.Null);
            Assert.That(created.Message.RequestUri!.Host,
                Is.EqualTo("example.com"));
            Assert.That(provisional.Kind,
                Is.EqualTo(SipInstantMessageEventKind.ProvisionalResponse));
            Assert.That(provisional.Final, Is.False);
            Assert.That(provisional.StatusCode, Is.EqualTo(180));
            Assert.That(delivered.Kind,
                Is.EqualTo(SipInstantMessageEventKind.Delivered));
            Assert.That(queuedCreated.Kind,
                Is.EqualTo(SipInstantMessageEventKind.RequestCreated));
            Assert.That(queuedProvisional.Kind,
                Is.EqualTo(SipInstantMessageEventKind.ProvisionalResponse));
            Assert.That(queuedDelivered.Kind,
                Is.EqualTo(SipInstantMessageEventKind.Delivered));
            Assert.That(queuedDelivered.CorrelationId,
                Is.EqualTo("message-events"));
            Assert.That(empty.Kind,
                Is.EqualTo(SipInstantMessageEventKind.None));
        });
    }

    [Test]
    public void MissingAuthenticationClientAndRetryLimitFailFinally()
    {
        SipMessageClient unconfigured = new SipMessageClient();
        SipInstantMessageRequest unconfiguredRequest =
            Request("sip:bob@example.com", "protected");
        unconfiguredRequest.CorrelationId = "missing-auth";
        SipInstantMessageEvent first =
            unconfigured.SendInstantMessage(unconfiguredRequest);
        SipMessage firstChallenge = Response(
            first.Message!, SipStatusCodes.Unauthorized);
        AddHeader(firstChallenge, SipHeaderNames.WwwAuthenticate,
            "Digest realm=\"example.com\", nonce=\"one\"");
        SipInstantMessageEvent missing =
            unconfigured.ReceiveInstantMessageResponse(firstChallenge);

        SipMessageClient limited = new SipMessageClient();
        limited.ConfigureInstantMessageAuthenticationClient(
            new RecordingMessageAuthenticationClient());
        SipInstantMessageRequest limitedRequest =
            Request("sip:bob@example.com", "protected");
        limitedRequest.Options.MaxAuthenticationAttempts = 0;
        SipInstantMessageEvent second =
            limited.SendInstantMessage(limitedRequest);
        SipMessage secondChallenge = Response(
            second.Message!, SipStatusCodes.Unauthorized);
        AddHeader(secondChallenge, SipHeaderNames.WwwAuthenticate,
            "Digest realm=\"example.com\", nonce=\"two\"");
        SipInstantMessageEvent exhausted =
            limited.ReceiveInstantMessageResponse(secondChallenge);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Kind,
                Is.EqualTo(SipInstantMessageEventKind.Failed));
            Assert.That(missing.Final, Is.True);
            Assert.That(missing.CorrelationId, Is.EqualTo("missing-auth"));
            Assert.That(missing.ErrorMessage,
                Does.Contain("authentication client"));
            Assert.That(exhausted.Kind,
                Is.EqualTo(SipInstantMessageEventKind.Failed));
            Assert.That(exhausted.AuthenticationAttemptCount, Is.Zero);
            Assert.That(exhausted.ErrorMessage,
                Does.Contain("retry limit"));
        });
    }

    [Test]
    public void RedirectFailuresCoverMissingContactAndMaximumDepth()
    {
        SipMessageClient missingContactClient = new SipMessageClient();
        SipInstantMessageEvent missingContactRequest =
            missingContactClient.SendInstantMessage(
                Request("sip:bob@example.com", "redirect"));
        SipInstantMessageEvent missingContact =
            missingContactClient.ReceiveInstantMessageResponse(
                Response(missingContactRequest.Message!,
                    SipStatusCodes.MovedTemporarily));

        SipMessageClient limitedClient = new SipMessageClient();
        SipInstantMessageRequest limitedRequest =
            Request("sip:bob@example.com", "redirect");
        limitedRequest.Options.MaxRedirects = 0;
        SipInstantMessageEvent limitedCreated =
            limitedClient.SendInstantMessage(limitedRequest);
        SipMessage redirect = Response(limitedCreated.Message!,
            SipStatusCodes.MovedTemporarily);
        redirect.ContactValues = new SipContactValue[]
        {
            Contact("sip:next@example.net", 1000)
        };
        SipInstantMessageEvent limited =
            limitedClient.ReceiveInstantMessageResponse(redirect);

        Assert.Multiple(() =>
        {
            Assert.That(missingContact.Kind,
                Is.EqualTo(SipInstantMessageEventKind.Failed));
            Assert.That(missingContact.ErrorMessage,
                Does.Contain("Contact"));
            Assert.That(limited.Kind,
                Is.EqualTo(SipInstantMessageEventKind.Failed));
            Assert.That(limited.RedirectCount, Is.Zero);
            Assert.That(limited.ErrorMessage,
                Does.Contain("redirect policy"));
        });
    }

    [Test]
    public void ServerRejectsOutOfDialogAndNonMessageRequests()
    {
        SipMessageServer server = new SipMessageServer();
        SipInstantMessageServerOptions policy =
            new SipInstantMessageServerOptions();
        policy.AllowOutOfDialog = false;
        SipInstantMessageEvent outside = server.ReceiveInstantMessage(
            OutboundMessage("outside"), policy);

        SipMessage wrongMethod = OutboundMessage("wrong method");
        wrongMethod.Method = SipMethod.Options;
        wrongMethod.MethodToken = SipMethods.Options;
        wrongMethod.CSeq!.Method = SipMethod.Options;
        wrongMethod.CSeq.MethodToken = SipMethods.Options;
        SipHeader cseq = wrongMethod.Headers.First(header =>
            string.Equals(header.Name, SipHeaderNames.CSeq,
                StringComparison.OrdinalIgnoreCase));
        cseq.Value = "1 OPTIONS";
        wrongMethod.OriginalBytes = Array.Empty<byte>();
        SipInstantMessageEvent wrong = server.ReceiveInstantMessage(
            wrongMethod, new SipInstantMessageServerOptions());

        Assert.Multiple(() =>
        {
            Assert.That(outside.Kind,
                Is.EqualTo(SipInstantMessageEventKind.Failed));
            Assert.That(outside.StatusCode, Is.EqualTo(481));
            Assert.That(outside.InDialog, Is.False);
            Assert.That(wrong.StatusCode, Is.EqualTo(405));
            Assert.That(Header(wrong.Response!, SipHeaderNames.Allow),
                Is.EqualTo(SipMethods.Message));
        });
    }

    static SipInstantMessageRequest Request(string target, string body)
    {
        SipInstantMessageRequest request = new SipInstantMessageRequest();
        request.Sender = Uri("sip:alice@example.com");
        request.Target = Uri(target);
        request.Body = System.Text.Encoding.UTF8.GetBytes(body);
        request.ContentType = MediaType("text", "plain");
        return request;
    }

    static SipMessage OutboundMessage(string body)
    {
        SipMessageClient client = new SipMessageClient();
        return client.SendInstantMessage(
            Request("sip:bob@example.com", body)).Message!;
    }

    static SipMessage InDialogMessage()
    {
        SipInstantMessageRequest request =
            Request("sip:ignored@example.com", "inside");
        request.Dialog = Dialog();
        return new SipMessageClient().SendInstantMessage(request).Message!;
    }

    static SipDialogSnapshot Dialog()
    {
        SipDialogId id = new SipDialogId();
        id.CallId = "dialog-call";
        id.LocalTag = "local-tag";
        id.RemoteTag = "remote-tag";
        SipRouteValue route = new SipRouteValue();
        route.Address = new SipNameAddress();
        route.Address.Uri = Uri("sip:proxy.example.net;lr");
        route.LooseRouting = true;
        SipDialogSnapshot dialog = new SipDialogSnapshot();
        dialog.Id = id;
        dialog.State = SipDialogState.Confirmed;
        dialog.LocalUri = Uri("sip:alice@example.com");
        dialog.RemoteUri = Uri("sip:bob@example.com");
        dialog.LocalTarget = Uri("sip:alice@device.example.com");
        dialog.RemoteTarget = Uri("sip:bob@device.example.net");
        dialog.RouteSet = new SipRouteValue[] { route };
        dialog.LocalCSeq = 42;
        dialog.RemoteCSeq = 19;
        return dialog;
    }

    static SipMessage Response(SipMessage request, int status)
    {
        string reason = status == 200 ? SipReasonPhrases.Ok
            : status == 202 ? SipReasonPhrases.Accepted
            : status == 302 ? SipReasonPhrases.MovedTemporarily
            : status == 401 ? SipReasonPhrases.Unauthorized
            : status == 404 ? SipReasonPhrases.NotFound : "Response";
        return SipTransactionMessageFactory.CreateRequestResponse(
            request, status, reason, "remote-tag");
    }

    static SipContactValue Contact(string text, int quality)
    {
        SipContactValue contact = new SipContactValue();
        contact.Address = new SipNameAddress();
        contact.Address.Uri = Uri(text);
        contact.QualityThousandths = quality;
        return contact;
    }

    static SipMediaType MediaType(string type, string subtype)
    {
        SipMediaType mediaType = new SipMediaType();
        mediaType.Type = type;
        mediaType.Subtype = subtype;
        return mediaType;
    }

    static SipUri Uri(string text)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            System.Text.Encoding.UTF8.GetBytes(text), new SipParseOptions());
        return parsed.Uri!;
    }

    static string? Header(SipMessage message, string name)
    {
        return message.Headers.FirstOrDefault(header =>
            string.Equals(header.Name, name,
                StringComparison.OrdinalIgnoreCase))?.Value;
    }

    static void AddHeader(SipMessage message, string name, string value)
    {
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = value;
        message.Headers = message.Headers.Append(header).ToArray();
    }
}

public class RecordingMessageAuthenticationClient : ISipAuthenticationClient
{
    public string? AuthorizationCorrelation;
    public int VerificationCount;

    public SipAuthenticationResult AuthorizeRequest(
        SipAuthenticationRequest request)
    {
        AuthorizationCorrelation = request.CorrelationId;
        SipMessage message = SipSnapshotCopy.CopyMessageModel(request.Message)!;
        SipHeader header = new SipHeader();
        header.Name = request.Scope == SipCredentialScope.Proxy
            ? SipHeaderNames.ProxyAuthorization
            : SipHeaderNames.Authorization;
        header.Value = "Digest test-response";
        message.Headers = message.Headers.Append(header).ToArray();
        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = SipAuthenticationStatus.Success;
        result.RetryAllowed = true;
        result.Message = message;
        return result;
    }

    public SipAuthenticationResult VerifyAuthenticationInfo(
        SipAuthenticationRequest request)
    {
        VerificationCount = VerificationCount + 1;
        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = SipAuthenticationStatus.Success;
        return result;
    }
}

public class RecordingMessageAuthenticationServer : ISipAuthenticationServer
{
    public SipAuthenticationResult CreateAuthenticationChallenge(
        SipAuthenticationRequest request, SipNonceOptions options)
    {
        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = SipAuthenticationStatus.ChallengeRequired;
        result.Message = SipTransactionMessageFactory.CreateRequestResponse(
            request.Message!, SipStatusCodes.Unauthorized,
            SipReasonPhrases.Unauthorized, "server-tag");
        SipHeader header = new SipHeader();
        header.Name = SipHeaderNames.WwwAuthenticate;
        header.Value = "Digest realm=\"example.com\", nonce=\"one\"";
        result.Message.Headers = result.Message.Headers.Append(header).ToArray();
        return result;
    }

    public SipAuthenticationResult VerifyAuthentication(
        SipAuthenticationRequest request)
    {
        bool found = request.Message!.Headers.Any(header =>
            string.Equals(header.Name, SipHeaderNames.Authorization,
                StringComparison.OrdinalIgnoreCase));
        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = found ? SipAuthenticationStatus.Success
            : SipAuthenticationStatus.ChallengeRequired;
        return result;
    }

    public SipAuthenticationResult VerifyBearerAuthentication(
        SipAuthenticationRequest request, ISipTokenValidator tokenValidator)
    {
        return new SipAuthenticationResult();
    }

    public SipAuthenticationResult VerifyCertificateAuthentication(
        SipAuthenticationRequest request, SipConnectionInfo connection,
        ISipCertificatePolicy certificatePolicy)
    {
        return new SipAuthenticationResult();
    }

    public SipAuthorizationDecision AuthorizeAuthenticatedOperation(
        SipAuthenticationRequest request,
        SipAuthenticationResult authentication,
        ISipAuthorizationPolicy authorizationPolicy)
    {
        return new SipAuthorizationDecision();
    }
}
