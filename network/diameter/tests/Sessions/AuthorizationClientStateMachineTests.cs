using System;
using System.Text;

namespace diameter.Tests;

public class AuthorizationClientStateMachineTests
{
    [Test]
    public void InitialAuthorizationSuccessOpensSessionAndSchedulesTimers()
    {
        ClientSessionTestContext context = CreateClientContext(
            "client.example;1");
        DiameterMessage request = CreateApplicationRequest(
            context.SessionId,
            10);

        AuthorizationClientTransitionResult pending =
            context.Machine.BeginInitialClientAuthorization(
                context.SessionId,
                request,
                71);
        DiameterMessage answer = CreateApplicationAnswer(
            request,
            DiameterResultCodes.Success);
        AddUnsigned32(
            answer,
            DiameterAvpCodes.AuthSessionState,
            (uint)DiameterAuthSessionState.StateMaintained);
        AddUnsigned32(
            answer,
            DiameterAvpCodes.AuthorizationLifetime,
            10);
        AddUnsigned32(answer, DiameterAvpCodes.AuthGracePeriod, 5);
        AddUnsigned32(answer, DiameterAvpCodes.SessionTimeout, 20);
        AddUnsigned32(
            answer,
            DiameterAvpCodes.ReAuthRequestType,
            (uint)DiameterReAuthRequestType.AuthorizeAuthenticate);

        AuthorizationClientTransitionResult opened =
            context.Machine.ProcessClientAuthorizationAnswer(
                context.SessionId,
                answer);
        request.Avps[0].Data[0] = 99;
        answer.Avps[0].Data[0] = 99;
        DiameterSessionRecord retained = context.Table.ReadSessionRecord(
            context.SessionId)!;

        Assert.Multiple(() =>
        {
            Assert.That(pending.Session!.State,
                Is.EqualTo(DiameterSessionState.Pending));
            Assert.That(pending.Session.PendingRequestIdentifier,
                Is.EqualTo(71));
            Assert.That(opened.Session!.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(opened.PreviousState,
                Is.EqualTo(DiameterSessionState.Pending));
            Assert.That(opened.Session.InitialAuthorizationCompleted,
                Is.True);
            Assert.That(opened.Session.PendingRequestIdentifier, Is.Zero);
            Assert.That(opened.Session.AuthorizationDeadline,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(10)));
            Assert.That(opened.Session.GraceDeadline,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(15)));
            Assert.That(opened.Session.SessionDeadline,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(20)));
            Assert.That(opened.Session.ReAuthRequestType,
                Is.EqualTo(
                    DiameterReAuthRequestType.AuthorizeAuthenticate));
            Assert.That(retained.LastRequest!.Avps[0].Data,
                Is.EqualTo(Encoding.UTF8.GetBytes(context.SessionId)));
            Assert.That(retained.LastAnswer!.Avps[0].Data,
                Is.EqualTo(Encoding.UTF8.GetBytes(context.SessionId)));
            Assert.That(retained.Version, Is.EqualTo(3));
        });
    }

    [Test]
    public void MultiRoundChallengeCanContinueAndComplete()
    {
        ClientSessionTestContext context = CreateClientContext(
            "client.example;2");
        DiameterMessage first = CreateApplicationRequest(
            context.SessionId,
            20);
        context.Machine.BeginInitialClientAuthorization(
            context.SessionId,
            first,
            81);
        DiameterMessage challenge = CreateApplicationAnswer(
            first,
            DiameterResultCodes.MultiRoundAuth);
        AddUnsigned32(
            challenge,
            DiameterAvpCodes.MultiRoundTimeOut,
            3);

        AuthorizationClientTransitionResult challenged =
            context.Machine.ProcessClientAuthorizationAnswer(
                context.SessionId,
                challenge);
        context.Clock.AdvanceDiscoveryTime(2);
        DiameterMessage response = CreateApplicationRequest(
            context.SessionId,
            21);
        AuthorizationClientTransitionResult continued =
            context.Machine.ContinueClientMultiRoundAuthorization(
                context.SessionId,
                response,
                82);
        DiameterMessage success = CreateApplicationAnswer(
            response,
            DiameterResultCodes.Success);
        AuthorizationClientTransitionResult completed =
            context.Machine.ProcessClientAuthorizationAnswer(
                context.SessionId,
                success);

        Assert.Multiple(() =>
        {
            Assert.That(challenged.Code,
                Is.EqualTo("client-multi-round-required"));
            Assert.That(challenged.Session!.State,
                Is.EqualTo(DiameterSessionState.Pending));
            Assert.That(challenged.Session.MultiRoundDeadline,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(1)));
            Assert.That(continued.Session!.MultiRoundDeadlinePresent,
                Is.False);
            Assert.That(continued.Session.PendingRequestIdentifier,
                Is.EqualTo(82));
            Assert.That(completed.Session!.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(completed.Session.InitialAuthorizationCompleted,
                Is.True);
        });
    }

    [Test]
    public void LifetimeExpiryRequestsReauthorizationAndSuccessResetsTimers()
    {
        ClientSessionTestContext context = CreateOpenClientContext(
            "client.example;3",
            2,
            5,
            20);
        context.Clock.AdvanceDiscoveryTime(2);

        AuthorizationClientTransitionResult expired =
            context.Machine.EvaluateClientAuthorizationTimers(
                context.SessionId);
        DiameterMessage request = CreateApplicationRequest(
            context.SessionId,
            31);
        AuthorizationClientTransitionResult pending =
            context.Machine.BeginClientReauthorization(
                context.SessionId,
                request,
                91);
        DiameterMessage answer = CreateApplicationAnswer(
            request,
            DiameterResultCodes.Success);
        AddUnsigned32(
            answer,
            DiameterAvpCodes.AuthorizationLifetime,
            4);
        AddUnsigned32(answer, DiameterAvpCodes.AuthGracePeriod, 1);
        AddUnsigned32(answer, DiameterAvpCodes.SessionTimeout, 8);
        AddUnsigned32(
            answer,
            DiameterAvpCodes.ReAuthRequestType,
            (uint)DiameterReAuthRequestType.AuthorizeOnly);
        AuthorizationClientTransitionResult reopened =
            context.Machine.ProcessClientAuthorizationAnswer(
                context.SessionId,
                answer);

        Assert.Multiple(() =>
        {
            Assert.That(expired.AuthorizationRequestRequired, Is.True);
            Assert.That(expired.Session!.State,
                Is.EqualTo(DiameterSessionState.Reauthorizing));
            Assert.That(expired.Session.ReauthorizationRequested, Is.True);
            Assert.That(expired.ReAuthRequestType,
                Is.EqualTo(
                    DiameterReAuthRequestType.AuthorizeAuthenticate));
            Assert.That(pending.Session!.PendingRequestIdentifier,
                Is.EqualTo(91));
            Assert.That(reopened.Session!.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(reopened.Session.ReauthorizationRequested, Is.False);
            Assert.That(reopened.Session.AuthorizationDeadline,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(4)));
            Assert.That(reopened.Session.GraceDeadline,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(5)));
            Assert.That(reopened.Session.SessionDeadline,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(8)));
        });
    }

    [Test]
    public void GraceSessionAndMultiRoundTimersChooseRequiredTerminalAction()
    {
        ClientSessionTestContext grace = CreateOpenClientContext(
            "client.example;4",
            1,
            2,
            10);
        grace.Clock.AdvanceDiscoveryTime(1);
        grace.Machine.EvaluateClientAuthorizationTimers(grace.SessionId);
        grace.Clock.AdvanceDiscoveryTime(2);
        AuthorizationClientTransitionResult graceExpired =
            grace.Machine.EvaluateClientAuthorizationTimers(grace.SessionId);

        ClientSessionTestContext timeout = CreateOpenClientContext(
            "client.example;5",
            0,
            0,
            2);
        timeout.Clock.AdvanceDiscoveryTime(2);
        AuthorizationClientTransitionResult sessionExpired =
            timeout.Machine.EvaluateClientAuthorizationTimers(
                timeout.SessionId);

        ClientSessionTestContext multi = CreateClientContext(
            "client.example;6");
        DiameterMessage request = CreateApplicationRequest(
            multi.SessionId,
            40);
        multi.Machine.BeginInitialClientAuthorization(
            multi.SessionId,
            request,
            101);
        DiameterMessage challenge = CreateApplicationAnswer(
            request,
            DiameterResultCodes.MultiRoundAuth);
        AddUnsigned32(
            challenge,
            DiameterAvpCodes.MultiRoundTimeOut,
            1);
        multi.Machine.ProcessClientAuthorizationAnswer(
            multi.SessionId,
            challenge);
        multi.Clock.AdvanceDiscoveryTime(1);
        AuthorizationClientTransitionResult multiExpired =
            multi.Machine.EvaluateClientAuthorizationTimers(
                multi.SessionId);

        Assert.Multiple(() =>
        {
            Assert.That(graceExpired.TerminationRequestRequired, Is.True);
            Assert.That(graceExpired.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.AuthExpired));
            Assert.That(graceExpired.Session!.State,
                Is.EqualTo(DiameterSessionState.Terminating));
            Assert.That(sessionExpired.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.SessionTimeout));
            Assert.That(sessionExpired.Code,
                Is.EqualTo("client-session-timeout-expired"));
            Assert.That(multiExpired.Session!.State,
                Is.EqualTo(DiameterSessionState.Failed));
            Assert.That(multiExpired.DisconnectRequired, Is.True);
        });
    }

    [Test]
    public void TerminationRequestAndAnswerCloseSessionDeterministically()
    {
        ClientSessionTestContext context = CreateOpenClientContext(
            "client.example;7",
            0,
            0,
            20);
        DiameterMessage request = CreateBaseRequest(
            context.SessionId,
            DiameterCommandCodes.SessionTermination,
            50);

        AuthorizationClientTransitionResult pending =
            context.Machine.BeginClientSessionTermination(
                context.SessionId,
                request,
                111,
                DiameterTerminationCause.Logout);
        DiameterMessage answer = CreateBaseAnswer(
            request,
            DiameterResultCodes.Success);
        AuthorizationClientTransitionResult closed =
            context.Machine.ProcessClientTerminationAnswer(
                context.SessionId,
                answer);

        Assert.Multiple(() =>
        {
            Assert.That(pending.Session!.State,
                Is.EqualTo(DiameterSessionState.Terminating));
            Assert.That(pending.Session.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.Logout));
            Assert.That(closed.Session!.State,
                Is.EqualTo(DiameterSessionState.Closed));
            Assert.That(closed.DisconnectRequired, Is.True);
            Assert.That(closed.Session.PendingRequestIdentifier, Is.Zero);
            Assert.That(closed.Session.AuthorizationDeadlinePresent,
                Is.False);
            Assert.That(closed.Session.SessionDeadlinePresent, Is.False);
        });
    }

    [Test]
    public void ReAuthAndAbortRequestsExposeRequiredAnswersAndActions()
    {
        ClientSessionTestContext reauth = CreateOpenClientContext(
            "client.example;8",
            0,
            0,
            20);
        DiameterMessage rar = CreateBaseRequest(
            reauth.SessionId,
            DiameterCommandCodes.ReAuth,
            60);
        AddUnsigned32(
            rar,
            DiameterAvpCodes.ReAuthRequestType,
            (uint)DiameterReAuthRequestType.AuthorizeAuthenticate);
        AuthorizationClientTransitionResult accepted =
            reauth.Machine.ProcessClientReAuthRequest(
                reauth.SessionId,
                rar,
                true);

        ClientSessionTestContext abort = CreateOpenClientContext(
            "client.example;9",
            0,
            0,
            20);
        DiameterMessage asr = CreateBaseRequest(
            abort.SessionId,
            DiameterCommandCodes.AbortSession,
            61);
        AuthorizationClientTransitionResult complied =
            abort.Machine.ProcessClientAbortRequest(
                abort.SessionId,
                asr,
                true);

        ClientSessionTestContext refusal = CreateOpenClientContext(
            "client.example;10",
            0,
            0,
            20);
        DiameterMessage refusedAsr = CreateBaseRequest(
            refusal.SessionId,
            DiameterCommandCodes.AbortSession,
            62);
        AuthorizationClientTransitionResult refused =
            refusal.Machine.ProcessClientAbortRequest(
                refusal.SessionId,
                refusedAsr,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.ReAuthAnswerRequired, Is.True);
            Assert.That(accepted.AnswerResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(accepted.AuthorizationRequestRequired, Is.True);
            Assert.That(accepted.Session!.ReauthorizationRequested, Is.True);
            Assert.That(accepted.ReAuthRequestType,
                Is.EqualTo(
                    DiameterReAuthRequestType.AuthorizeAuthenticate));
            Assert.That(accepted.Session.ReAuthRequestType,
                Is.EqualTo(
                    DiameterReAuthRequestType.AuthorizeAuthenticate));
            Assert.That(complied.AbortAnswerRequired, Is.True);
            Assert.That(complied.TerminationRequestRequired, Is.True);
            Assert.That(complied.DisconnectRequired, Is.True);
            Assert.That(complied.Session!.State,
                Is.EqualTo(DiameterSessionState.Aborting));
            Assert.That(complied.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.Administrative));
            Assert.That(refused.Changed, Is.False);
            Assert.That(refused.AnswerResultCode,
                Is.EqualTo(DiameterResultCodes.UnableToComply));
            Assert.That(refused.Session!.State,
                Is.EqualTo(DiameterSessionState.Open));
        });
    }

    [Test]
    public void AuthorizationFailuresAndBadAnswersReachExplicitOutcomes()
    {
        ClientSessionTestContext denied = CreateClientContext(
            "client.example;11");
        DiameterMessage deniedRequest = CreateApplicationRequest(
            denied.SessionId,
            70);
        denied.Machine.BeginInitialClientAuthorization(
            denied.SessionId,
            deniedRequest,
            121);
        DiameterMessage denial = CreateApplicationAnswer(
            deniedRequest,
            DiameterResultCodes.AuthorizationRejected);
        AuthorizationClientTransitionResult rejected =
            denied.Machine.ProcessClientAuthorizationAnswer(
                denied.SessionId,
                denial);

        ClientSessionTestContext invalid = CreateClientContext(
            "client.example;12");
        DiameterMessage invalidRequest = CreateApplicationRequest(
            invalid.SessionId,
            71);
        invalid.Machine.BeginInitialClientAuthorization(
            invalid.SessionId,
            invalidRequest,
            122);
        DiameterMessage badAnswer = CreateApplicationAnswer(
            invalidRequest,
            DiameterResultCodes.Success);
        AddUnsigned32(
            badAnswer,
            DiameterAvpCodes.AuthorizationLifetime,
            10);
        AuthorizationClientTransitionResult malformed =
            invalid.Machine.ProcessClientAuthorizationAnswer(
                invalid.SessionId,
                badAnswer);

        ClientSessionTestContext sendFailure = CreateClientContext(
            "client.example;13");
        DiameterMessage failedRequest = CreateApplicationRequest(
            sendFailure.SessionId,
            72);
        sendFailure.Machine.BeginInitialClientAuthorization(
            sendFailure.SessionId,
            failedRequest,
            123);
        AuthorizationClientTransitionResult failed =
            sendFailure.Machine.FailClientAuthorizationOperation(
                sendFailure.SessionId);

        ClientSessionTestContext service = CreateOpenClientContext(
            "client.example;14",
            0,
            0,
            20);
        AuthorizationClientTransitionResult notProvided =
            service.Machine.RejectClientAuthorizedService(
                service.SessionId);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Session!.State,
                Is.EqualTo(DiameterSessionState.Failed));
            Assert.That(rejected.DisconnectRequired, Is.True);
            Assert.That(malformed.Session!.State,
                Is.EqualTo(DiameterSessionState.Terminating));
            Assert.That(malformed.TerminationRequestRequired, Is.True);
            Assert.That(malformed.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.BadAnswer));
            Assert.That(failed.Session!.State,
                Is.EqualTo(DiameterSessionState.Failed));
            Assert.That(notProvided.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.ServiceNotProvided));
            Assert.Throws<DiameterSessionException>(() =>
                sendFailure.Machine.FailClientAuthorizationOperation(
                    sendFailure.SessionId));
        });
    }

    [Test]
    public void OverdueTerminalTimersUseTheChronologicallyFirstDeadline()
    {
        ClientSessionTestContext grace = CreateOpenClientContext(
            "client.example;timer-grace",
            1,
            1,
            5);
        grace.Clock.AdvanceDiscoveryTime(5);
        AuthorizationClientTransitionResult graceResult =
            grace.Machine.EvaluateClientAuthorizationTimers(
                grace.SessionId);

        ClientSessionTestContext session = CreateOpenClientContext(
            "client.example;timer-session",
            1,
            5,
            3);
        session.Clock.AdvanceDiscoveryTime(5);
        AuthorizationClientTransitionResult sessionResult =
            session.Machine.EvaluateClientAuthorizationTimers(
                session.SessionId);

        ClientSessionTestContext multi = CreateOpenClientContext(
            "client.example;timer-multi",
            0,
            0,
            5);
        DiameterMessage request = CreateApplicationRequest(
            multi.SessionId,
            90);
        multi.Machine.BeginClientReauthorization(
            multi.SessionId,
            request,
            190);
        DiameterMessage challenge = CreateApplicationAnswer(
            request,
            DiameterResultCodes.MultiRoundAuth);
        AddUnsigned32(
            challenge,
            DiameterAvpCodes.MultiRoundTimeOut,
            1);
        multi.Machine.ProcessClientAuthorizationAnswer(
            multi.SessionId,
            challenge);
        multi.Clock.AdvanceDiscoveryTime(5);
        AuthorizationClientTransitionResult multiResult =
            multi.Machine.EvaluateClientAuthorizationTimers(
                multi.SessionId);

        Assert.Multiple(() =>
        {
            Assert.That(graceResult.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.AuthExpired));
            Assert.That(graceResult.Code,
                Is.EqualTo("client-authorization-grace-expired"));
            Assert.That(sessionResult.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.SessionTimeout));
            Assert.That(sessionResult.Code,
                Is.EqualTo("client-session-timeout-expired"));
            Assert.That(multiResult.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.BadAnswer));
            Assert.That(multiResult.Code,
                Is.EqualTo("client-multi-round-timeout"));
        });
    }

    [Test]
    public void CorrelationRefusalAndAlreadyPendingBranchesAreExplicit()
    {
        ClientSessionTestContext invalid = CreateClientContext(
            "client.example;invalid-correlation");
        DiameterMessage request = CreateApplicationRequest(
            invalid.SessionId,
            100);
        invalid.Machine.BeginInitialClientAuthorization(
            invalid.SessionId,
            request,
            200);
        DiameterMessage mismatched = CreateApplicationAnswer(
            request,
            DiameterResultCodes.Success);
        mismatched.HopByHopIdentifier =
            mismatched.HopByHopIdentifier + 1U;
        AuthorizationClientTransitionResult correlationFailure =
            invalid.Machine.ProcessClientAuthorizationAnswer(
                invalid.SessionId,
                mismatched);

        ClientSessionTestContext refused = CreateOpenClientContext(
            "client.example;refused-reauth",
            0,
            0,
            20);
        DiameterMessage reauth = CreateBaseRequest(
            refused.SessionId,
            DiameterCommandCodes.ReAuth,
            101);
        AddUnsigned32(
            reauth,
            DiameterAvpCodes.ReAuthRequestType,
            (uint)DiameterReAuthRequestType.AuthorizeOnly);
        AuthorizationClientTransitionResult reauthRefused =
            refused.Machine.ProcessClientReAuthRequest(
                refused.SessionId,
                reauth,
                false);

        ClientSessionTestContext abort = CreateOpenClientContext(
            "client.example;repeated-abort",
            0,
            0,
            20);
        DiameterMessage firstAbort = CreateBaseRequest(
            abort.SessionId,
            DiameterCommandCodes.AbortSession,
            102);
        abort.Machine.ProcessClientAbortRequest(
            abort.SessionId,
            firstAbort,
            true);
        DiameterMessage secondAbort = CreateBaseRequest(
            abort.SessionId,
            DiameterCommandCodes.AbortSession,
            103);
        AuthorizationClientTransitionResult repeatedAbort =
            abort.Machine.ProcessClientAbortRequest(
                abort.SessionId,
                secondAbort,
                true);

        ClientSessionTestContext termination = CreateOpenClientContext(
            "client.example;invalid-termination",
            0,
            0,
            20);
        DiameterMessage terminationRequest = CreateBaseRequest(
            termination.SessionId,
            DiameterCommandCodes.SessionTermination,
            104);
        termination.Machine.BeginClientSessionTermination(
            termination.SessionId,
            terminationRequest,
            204,
            DiameterTerminationCause.Logout);
        DiameterMessage terminationAnswer = CreateBaseAnswer(
            terminationRequest,
            DiameterResultCodes.Success);
        terminationAnswer.EndToEndIdentifier =
            terminationAnswer.EndToEndIdentifier + 1U;
        AuthorizationClientTransitionResult terminationFailure =
            termination.Machine.ProcessClientTerminationAnswer(
                termination.SessionId,
                terminationAnswer);

        Assert.Multiple(() =>
        {
            Assert.That(correlationFailure.Session!.State,
                Is.EqualTo(DiameterSessionState.Failed));
            Assert.That(correlationFailure.Code,
                Is.EqualTo("client-authorization-answer-invalid"));
            Assert.That(reauthRefused.ReAuthAnswerRequired, Is.True);
            Assert.That(reauthRefused.AnswerResultCode,
                Is.EqualTo(DiameterResultCodes.UnableToComply));
            Assert.That(reauthRefused.Session!.State,
                Is.EqualTo(DiameterSessionState.Failed));
            Assert.That(repeatedAbort.Changed, Is.False);
            Assert.That(repeatedAbort.AbortAnswerRequired, Is.True);
            Assert.That(repeatedAbort.Code,
                Is.EqualTo("client-abort-already-pending"));
            Assert.That(terminationFailure.Session!.State,
                Is.EqualTo(DiameterSessionState.Failed));
            Assert.That(terminationFailure.Code,
                Is.EqualTo("client-termination-answer-invalid"));
        });
    }

    private static ClientSessionTestContext CreateOpenClientContext(
        string sessionId,
        uint authorizationLifetime,
        uint gracePeriod,
        uint sessionTimeout)
    {
        ClientSessionTestContext result = CreateClientContext(sessionId);
        DiameterMessage request = CreateApplicationRequest(sessionId, 1);
        result.Machine.BeginInitialClientAuthorization(
            sessionId,
            request,
            1);
        DiameterMessage answer = CreateApplicationAnswer(
            request,
            DiameterResultCodes.Success);
        if (authorizationLifetime > 0)
        {
            AddUnsigned32(
                answer,
                DiameterAvpCodes.AuthorizationLifetime,
                authorizationLifetime);
            AddUnsigned32(
                answer,
                DiameterAvpCodes.AuthGracePeriod,
                gracePeriod);
            AddUnsigned32(
                answer,
                DiameterAvpCodes.ReAuthRequestType,
                (uint)DiameterReAuthRequestType.AuthorizeAuthenticate);
        }

        if (sessionTimeout > 0)
        {
            AddUnsigned32(
                answer,
                DiameterAvpCodes.SessionTimeout,
                sessionTimeout);
        }

        AuthorizationClientTransitionResult opened =
            result.Machine.ProcessClientAuthorizationAnswer(
                sessionId,
                answer);
        Assert.That(opened.Session!.State,
            Is.EqualTo(DiameterSessionState.Open));
        return result;
    }

    private static ClientSessionTestContext CreateClientContext(
        string sessionId)
    {
        ClientSessionTestContext result = new ClientSessionTestContext();
        result.Clock = new FakeDiscoveryClock();
        result.Table = new SessionTable();
        result.Machine = new AuthorizationClientStateMachine(
            result.Table,
            result.Clock);
        result.SessionId = sessionId;
        DiameterSessionConfiguration configuration =
            new DiameterSessionConfiguration();
        configuration.SessionId = sessionId;
        configuration.ApplicationId = 4;
        configuration.DestinationRealm = CreateIdentity("remote.example");
        configuration.DestinationHost =
            CreateIdentity("service.remote.example");
        configuration.PeerIdentity = CreateIdentity("peer.example");
        configuration.UserName = "alice@remote.example";
        configuration.MultiRoundTimeoutMilliseconds = 5000;
        result.Machine.CreateStatefulClientSession(configuration);
        return result;
    }

    private static DiameterMessage CreateApplicationRequest(
        string sessionId,
        uint identifier)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.CommandCode = 300;
        result.ApplicationId = 4;
        result.HopByHopIdentifier = identifier;
        result.EndToEndIdentifier = identifier + 100;
        AddSessionId(result, sessionId);
        return result;
    }

    private static DiameterMessage CreateApplicationAnswer(
        DiameterMessage request,
        uint resultCode)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = request.Flags & DiameterMessageFlags.Proxyable;
        result.CommandCode = request.CommandCode;
        result.ApplicationId = request.ApplicationId;
        result.HopByHopIdentifier = request.HopByHopIdentifier;
        result.EndToEndIdentifier = request.EndToEndIdentifier;
        AddSessionId(
            result,
            Encoding.UTF8.GetString(request.Avps[0].Data));
        AddUnsigned32(result, DiameterAvpCodes.ResultCode, resultCode);
        return result;
    }

    private static DiameterMessage CreateBaseRequest(
        string sessionId,
        uint commandCode,
        uint identifier)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.CommandCode = commandCode;
        result.ApplicationId = DiameterApplicationIds.Common;
        result.HopByHopIdentifier = identifier;
        result.EndToEndIdentifier = identifier + 100;
        AddSessionId(result, sessionId);
        return result;
    }

    private static DiameterMessage CreateBaseAnswer(
        DiameterMessage request,
        uint resultCode)
    {
        return CreateApplicationAnswer(request, resultCode);
    }

    private static void AddSessionId(
        DiameterMessage message,
        string sessionId)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = DiameterAvpCodes.SessionId;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = Encoding.UTF8.GetBytes(sessionId);
        message.Avps.Add(avp);
    }

    private static void AddUnsigned32(
        DiameterMessage message,
        uint code,
        uint value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(avp.Data, value);
        message.Avps.Add(avp);
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class ClientSessionTestContext
{
    internal FakeDiscoveryClock Clock = new FakeDiscoveryClock();
    internal SessionTable Table = new SessionTable();
    internal AuthorizationClientStateMachine Machine = null!;
    internal string SessionId = string.Empty;
}
