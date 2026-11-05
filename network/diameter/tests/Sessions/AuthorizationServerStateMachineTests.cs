using System.Text;

namespace diameter.Tests;

public class AuthorizationServerStateMachineTests
{
    [Test]
    public void AuthorizedRequestOpensSessionAndSchedulesOwnedTimers()
    {
        ServerSessionTestContext context = CreateServerContext(
            "server.example;1",
            2000,
            1000,
            5000,
            4000);
        DiameterMessage request = CreateServerApplicationRequest(
            context.SessionId,
            10);

        AuthorizationServerTransitionResult result =
            context.Machine.ProcessServerAuthorizationRequest(
                context.SessionId,
                request,
                DiameterResultCodes.Success);
        request.Avps[0].Data[0] = 99;
        DiameterSessionRecord retained = context.Table.ReadSessionRecord(
            context.SessionId)!;

        Assert.Multiple(() =>
        {
            Assert.That(result.AuthorizationAnswerRequired, Is.True);
            Assert.That(result.AnswerResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(result.Session!.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(result.Session.InitialAuthorizationCompleted,
                Is.True);
            Assert.That(result.Session.AuthorizationDeadline,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(2)));
            Assert.That(result.Session.GraceDeadline,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(3)));
            Assert.That(result.Session.SessionDeadline,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(5)));
            Assert.That(retained.LastRequest!.Avps[0].Data,
                Is.EqualTo(Encoding.UTF8.GetBytes(context.SessionId)));
            Assert.That(retained.Version, Is.EqualTo(2));
        });
    }

    [Test]
    public void MultiRoundRequestCanCompleteAndDenialCleansUp()
    {
        ServerSessionTestContext multi = CreateServerContext(
            "server.example;2",
            0,
            0,
            0,
            3000);
        DiameterMessage first = CreateServerApplicationRequest(
            multi.SessionId,
            20);
        AuthorizationServerTransitionResult challenged =
            multi.Machine.ProcessServerAuthorizationRequest(
                multi.SessionId,
                first,
                DiameterResultCodes.MultiRoundAuth);
        multi.Clock.AdvanceDiscoveryTime(2);
        DiameterMessage response = CreateServerApplicationRequest(
            multi.SessionId,
            21);
        AuthorizationServerTransitionResult opened =
            multi.Machine.ProcessServerAuthorizationRequest(
                multi.SessionId,
                response,
                DiameterResultCodes.Success);

        ServerSessionTestContext denied = CreateServerContext(
            "server.example;3",
            0,
            0,
            0,
            3000);
        AuthorizationServerTransitionResult rejected =
            denied.Machine.ProcessServerAuthorizationRequest(
                denied.SessionId,
                CreateServerApplicationRequest(denied.SessionId, 22),
                DiameterResultCodes.AuthorizationRejected);

        Assert.Multiple(() =>
        {
            Assert.That(challenged.Session!.State,
                Is.EqualTo(DiameterSessionState.Pending));
            Assert.That(challenged.Session.MultiRoundDeadline,
                Is.EqualTo(multi.Clock.UtcTime.AddSeconds(1)));
            Assert.That(opened.Session!.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(opened.Session.MultiRoundDeadlinePresent, Is.False);
            Assert.That(rejected.AuthorizationAnswerRequired, Is.True);
            Assert.That(rejected.CleanedUp, Is.True);
            Assert.That(rejected.Session!.State,
                Is.EqualTo(DiameterSessionState.Closed));
            Assert.That(denied.Table.ReadSessionRecordCount(), Is.Zero);
        });
    }

    [Test]
    public void ReAuthSuccessReopensSessionAndFailureCleansUp()
    {
        ServerSessionTestContext context = CreateOpenServerContext(
            "server.example;4",
            0,
            0,
            0,
            5000);
        DiameterMessage first = CreateServerBaseRequest(
            context.SessionId,
            DiameterCommandCodes.ReAuth,
            30);
        AddServerUnsigned32(
            first,
            DiameterAvpCodes.ReAuthRequestType,
            (uint)DiameterReAuthRequestType.AuthorizeAuthenticate);
        AuthorizationServerTransitionResult pending =
            context.Machine.BeginServerReauthorization(
                context.SessionId,
                first,
                71);
        AuthorizationServerTransitionResult reopened =
            context.Machine.ProcessServerReAuthAnswer(
                context.SessionId,
                CreateServerBaseAnswer(
                    first,
                    DiameterResultCodes.Success));

        DiameterMessage second = CreateServerBaseRequest(
            context.SessionId,
            DiameterCommandCodes.ReAuth,
            31);
        AddServerUnsigned32(
            second,
            DiameterAvpCodes.ReAuthRequestType,
            (uint)DiameterReAuthRequestType.AuthorizeOnly);
        context.Machine.BeginServerReauthorization(
            context.SessionId,
            second,
            72);
        AuthorizationServerTransitionResult failed =
            context.Machine.ProcessServerReAuthAnswer(
                context.SessionId,
                CreateServerBaseAnswer(
                    second,
                    DiameterResultCodes.AuthorizationRejected));

        Assert.Multiple(() =>
        {
            Assert.That(pending.ReAuthRequestRequired, Is.True);
            Assert.That(pending.Session!.State,
                Is.EqualTo(DiameterSessionState.Reauthorizing));
            Assert.That(pending.Session.PendingRequestIdentifier,
                Is.EqualTo(71));
            Assert.That(pending.ReAuthRequestType,
                Is.EqualTo(
                    DiameterReAuthRequestType.AuthorizeAuthenticate));
            Assert.That(reopened.Session!.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(reopened.Session.ReauthorizationRequested, Is.False);
            Assert.That(failed.Code, Is.EqualTo("server-reauth-failed"));
            Assert.That(failed.CleanedUp, Is.True);
            Assert.That(context.Table.ReadSessionRecordCount(), Is.Zero);
        });
    }

    [Test]
    public void ReAuthDeliveryFailureCleansUpImmediately()
    {
        ServerSessionTestContext context = CreateOpenServerContext(
            "server.example;5",
            0,
            0,
            0,
            5000);
        DiameterMessage request = CreateServerBaseRequest(
            context.SessionId,
            DiameterCommandCodes.ReAuth,
            40);
        AddServerUnsigned32(
            request,
            DiameterAvpCodes.ReAuthRequestType,
            (uint)DiameterReAuthRequestType.AuthorizeOnly);
        context.Machine.BeginServerReauthorization(
            context.SessionId,
            request,
            81);

        AuthorizationServerTransitionResult result =
            context.Machine.FailServerRequestDelivery(context.SessionId);

        Assert.Multiple(() =>
        {
            Assert.That(result.CleanedUp, Is.True);
            Assert.That(result.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.LinkBroken));
            Assert.That(result.Code,
                Is.EqualTo("server-reauth-delivery-failed"));
            Assert.That(context.Table.ReadSessionRecordCount(), Is.Zero);
        });
    }

    [Test]
    public void AbortDeliveryFailureRetriesAndAnswerCleansUp()
    {
        ServerSessionTestContext context = CreateOpenServerContext(
            "server.example;6",
            0,
            0,
            0,
            5000);
        DiameterMessage abort = CreateServerBaseRequest(
            context.SessionId,
            DiameterCommandCodes.AbortSession,
            50);
        AuthorizationServerTransitionResult pending =
            context.Machine.BeginServerAbort(
                context.SessionId,
                abort,
                91);
        AuthorizationServerTransitionResult retry =
            context.Machine.FailServerRequestDelivery(context.SessionId);
        AuthorizationServerTransitionResult completed =
            context.Machine.ProcessServerAbortAnswer(
                context.SessionId,
                CreateServerBaseAnswer(
                    abort,
                    DiameterResultCodes.UnableToComply));

        Assert.Multiple(() =>
        {
            Assert.That(pending.AbortRequestRequired, Is.True);
            Assert.That(pending.Session!.State,
                Is.EqualTo(DiameterSessionState.Aborting));
            Assert.That(retry.Changed, Is.False);
            Assert.That(retry.AbortRequestRetryRequired, Is.True);
            Assert.That(retry.Session!.State,
                Is.EqualTo(DiameterSessionState.Aborting));
            Assert.That(completed.CleanedUp, Is.True);
            Assert.That(completed.Session!.State,
                Is.EqualTo(DiameterSessionState.Closed));
            Assert.That(context.Table.ReadSessionRecordCount(), Is.Zero);
        });
    }

    [Test]
    public void AuthorizationSessionAndMultiRoundTimersCleanUp()
    {
        ServerSessionTestContext authorization = CreateOpenServerContext(
            "server.example;7",
            1000,
            2000,
            10000,
            5000);
        authorization.Clock.AdvanceDiscoveryTime(1);
        AuthorizationServerTransitionResult grace =
            authorization.Machine.EvaluateServerAuthorizationTimers(
                authorization.SessionId);
        authorization.Clock.AdvanceDiscoveryTime(2);
        AuthorizationServerTransitionResult authExpired =
            authorization.Machine.EvaluateServerAuthorizationTimers(
                authorization.SessionId);

        ServerSessionTestContext session = CreateOpenServerContext(
            "server.example;8",
            0,
            0,
            1000,
            5000);
        session.Clock.AdvanceDiscoveryTime(1);
        AuthorizationServerTransitionResult sessionExpired =
            session.Machine.EvaluateServerAuthorizationTimers(
                session.SessionId);

        ServerSessionTestContext multi = CreateServerContext(
            "server.example;9",
            0,
            0,
            0,
            1000);
        multi.Machine.ProcessServerAuthorizationRequest(
            multi.SessionId,
            CreateServerApplicationRequest(multi.SessionId, 60),
            DiameterResultCodes.MultiRoundAuth);
        multi.Clock.AdvanceDiscoveryTime(1);
        AuthorizationServerTransitionResult multiExpired =
            multi.Machine.EvaluateServerAuthorizationTimers(
                multi.SessionId);

        Assert.Multiple(() =>
        {
            Assert.That(grace.Changed, Is.False);
            Assert.That(grace.Code, Is.EqualTo("server-timer-not-due"));
            Assert.That(authExpired.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.AuthExpired));
            Assert.That(authExpired.CleanedUp, Is.True);
            Assert.That(sessionExpired.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.SessionTimeout));
            Assert.That(sessionExpired.CleanedUp, Is.True);
            Assert.That(multiExpired.Code,
                Is.EqualTo("server-multi-round-timeout-expired"));
            Assert.That(multiExpired.CleanedUp, Is.True);
            Assert.That(authorization.Table.ReadSessionRecordCount(),
                Is.Zero);
            Assert.That(session.Table.ReadSessionRecordCount(), Is.Zero);
            Assert.That(multi.Table.ReadSessionRecordCount(), Is.Zero);
        });
    }

    [Test]
    public void TerminationCleansAnyKnownStateAndAnswersUnknownSession()
    {
        ServerSessionTestContext known = CreateOpenServerContext(
            "server.example;10",
            0,
            0,
            0,
            5000);
        DiameterMessage termination = CreateServerBaseRequest(
            known.SessionId,
            DiameterCommandCodes.SessionTermination,
            70);
        AuthorizationServerTransitionResult completed =
            known.Machine.ProcessServerTerminationRequest(
                known.SessionId,
                termination);

        ServerSessionTestContext unknown = CreateServerContext(
            "server.example;11",
            0,
            0,
            0,
            5000);
        DiameterMessage missing = CreateServerBaseRequest(
            "server.example;missing",
            DiameterCommandCodes.SessionTermination,
            71);
        AuthorizationServerTransitionResult notFound =
            unknown.Machine.ProcessServerTerminationRequest(
                "server.example;missing",
                missing);

        Assert.Multiple(() =>
        {
            Assert.That(completed.TerminationAnswerRequired, Is.True);
            Assert.That(completed.AnswerResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(completed.CleanedUp, Is.True);
            Assert.That(known.Table.ReadSessionRecordCount(), Is.Zero);
            Assert.That(notFound.TerminationAnswerRequired, Is.True);
            Assert.That(notFound.AnswerResultCode,
                Is.EqualTo(DiameterResultCodes.UnknownSessionId));
            Assert.That(notFound.Changed, Is.False);
            Assert.That(unknown.Table.ReadSessionRecordCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void AbortAnswerOutsideAbortStateIsIgnored()
    {
        ServerSessionTestContext context = CreateOpenServerContext(
            "server.example;12",
            0,
            0,
            0,
            5000);
        DiameterMessage request = CreateServerBaseRequest(
            context.SessionId,
            DiameterCommandCodes.AbortSession,
            80);

        AuthorizationServerTransitionResult result =
            context.Machine.ProcessServerAbortAnswer(
                context.SessionId,
                CreateServerBaseAnswer(
                    request,
                    DiameterResultCodes.Success));

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Changed, Is.False);
            Assert.That(result.Code,
                Is.EqualTo("server-unexpected-abort-answer-ignored"));
            Assert.That(result.Session!.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(context.Table.ReadSessionRecordCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void OpenReauthorizationAndExpiredMultiRoundFollowStateRules()
    {
        ServerSessionTestContext open = CreateOpenServerContext(
            "server.example;open-reauthorization",
            0,
            0,
            0,
            5000);
        AuthorizationServerTransitionResult reauthorized =
            open.Machine.ProcessServerAuthorizationRequest(
                open.SessionId,
                CreateServerApplicationRequest(open.SessionId, 90),
                DiameterResultCodes.Success);

        ServerSessionTestContext expired = CreateServerContext(
            "server.example;expired-multi-round",
            0,
            0,
            0,
            1000);
        expired.Machine.ProcessServerAuthorizationRequest(
            expired.SessionId,
            CreateServerApplicationRequest(expired.SessionId, 91),
            DiameterResultCodes.MultiRoundAuth);
        expired.Clock.AdvanceDiscoveryTime(1);

        Assert.Multiple(() =>
        {
            Assert.That(reauthorized.PreviousState,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(reauthorized.Session!.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(reauthorized.AuthorizationAnswerRequired, Is.True);
            Assert.That(
                () => expired.Machine.ProcessServerAuthorizationRequest(
                    expired.SessionId,
                    CreateServerApplicationRequest(
                        expired.SessionId,
                        92),
                    DiameterResultCodes.Success),
                Throws.TypeOf<DiameterSessionException>());
            Assert.That(expired.Table.ReadSessionRecord(
                expired.SessionId)!.State,
                Is.EqualTo(DiameterSessionState.Pending));
        });
    }

    [Test]
    public void InvalidServerInitiatedAnswersHaveDeterministicOutcomes()
    {
        ServerSessionTestContext reauth = CreateOpenServerContext(
            "server.example;invalid-reauth",
            0,
            0,
            0,
            5000);
        DiameterMessage reauthRequest = CreateServerBaseRequest(
            reauth.SessionId,
            DiameterCommandCodes.ReAuth,
            100);
        AddServerUnsigned32(
            reauthRequest,
            DiameterAvpCodes.ReAuthRequestType,
            (uint)DiameterReAuthRequestType.AuthorizeOnly);
        reauth.Machine.BeginServerReauthorization(
            reauth.SessionId,
            reauthRequest,
            200);
        DiameterMessage badReauthAnswer = CreateServerBaseAnswer(
            reauthRequest,
            DiameterResultCodes.Success);
        badReauthAnswer.HopByHopIdentifier =
            badReauthAnswer.HopByHopIdentifier + 1U;
        AuthorizationServerTransitionResult reauthFailure =
            reauth.Machine.ProcessServerReAuthAnswer(
                reauth.SessionId,
                badReauthAnswer);

        ServerSessionTestContext abort = CreateOpenServerContext(
            "server.example;invalid-abort",
            0,
            0,
            0,
            5000);
        DiameterMessage abortRequest = CreateServerBaseRequest(
            abort.SessionId,
            DiameterCommandCodes.AbortSession,
            101);
        abort.Machine.BeginServerAbort(
            abort.SessionId,
            abortRequest,
            201);
        DiameterMessage badAbortAnswer = CreateServerBaseAnswer(
            abortRequest,
            DiameterResultCodes.Success);
        badAbortAnswer.EndToEndIdentifier =
            badAbortAnswer.EndToEndIdentifier + 1U;

        Assert.Multiple(() =>
        {
            Assert.That(reauthFailure.CleanedUp, Is.True);
            Assert.That(reauthFailure.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.BadAnswer));
            Assert.That(reauthFailure.Code,
                Is.EqualTo("server-reauth-answer-invalid"));
            Assert.That(reauth.Table.ReadSessionRecordCount(), Is.Zero);
            Assert.That(
                () => abort.Machine.ProcessServerAbortAnswer(
                    abort.SessionId,
                    badAbortAnswer),
                Throws.TypeOf<ArgumentException>());
            Assert.That(abort.Table.ReadSessionRecord(
                abort.SessionId)!.State,
                Is.EqualTo(DiameterSessionState.Aborting));
        });
    }

    [Test]
    public void OverdueServerTimersUseTheChronologicallyFirstDeadline()
    {
        ServerSessionTestContext authorization =
            CreateOpenServerContext(
                "server.example;timer-authorization",
                1000,
                1000,
                5000,
                5000);
        authorization.Clock.AdvanceDiscoveryTime(5);
        AuthorizationServerTransitionResult authorizationResult =
            authorization.Machine.EvaluateServerAuthorizationTimers(
                authorization.SessionId);

        ServerSessionTestContext session = CreateOpenServerContext(
            "server.example;timer-session",
            1000,
            5000,
            3000,
            5000);
        session.Clock.AdvanceDiscoveryTime(6);
        AuthorizationServerTransitionResult sessionResult =
            session.Machine.EvaluateServerAuthorizationTimers(
                session.SessionId);

        Assert.Multiple(() =>
        {
            Assert.That(authorizationResult.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.AuthExpired));
            Assert.That(authorizationResult.Code,
                Is.EqualTo("server-authorization-timeout-expired"));
            Assert.That(sessionResult.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.SessionTimeout));
            Assert.That(sessionResult.Code,
                Is.EqualTo("server-session-timeout-expired"));
        });
    }

    private static ServerSessionTestContext CreateOpenServerContext(
        string sessionId,
        int authorizationLifetimeMilliseconds,
        int gracePeriodMilliseconds,
        int sessionTimeoutMilliseconds,
        int multiRoundTimeoutMilliseconds)
    {
        ServerSessionTestContext result = CreateServerContext(
            sessionId,
            authorizationLifetimeMilliseconds,
            gracePeriodMilliseconds,
            sessionTimeoutMilliseconds,
            multiRoundTimeoutMilliseconds);
        AuthorizationServerTransitionResult opened =
            result.Machine.ProcessServerAuthorizationRequest(
                sessionId,
                CreateServerApplicationRequest(sessionId, 1),
                DiameterResultCodes.Success);
        Assert.That(opened.Session!.State,
            Is.EqualTo(DiameterSessionState.Open));
        return result;
    }

    private static ServerSessionTestContext CreateServerContext(
        string sessionId,
        int authorizationLifetimeMilliseconds,
        int gracePeriodMilliseconds,
        int sessionTimeoutMilliseconds,
        int multiRoundTimeoutMilliseconds)
    {
        ServerSessionTestContext result = new ServerSessionTestContext();
        result.Clock = new FakeDiscoveryClock();
        result.Table = new SessionTable();
        result.Machine = new AuthorizationServerStateMachine(
            result.Table,
            result.Clock);
        result.SessionId = sessionId;
        DiameterSessionConfiguration configuration =
            new DiameterSessionConfiguration();
        configuration.SessionId = sessionId;
        configuration.Role = DiameterSessionRole.Server;
        configuration.ApplicationId = 4;
        configuration.PeerIdentity = CreateServerIdentity("peer.example");
        configuration.UserName = "alice@remote.example";
        configuration.AuthorizationLifetimeMilliseconds =
            authorizationLifetimeMilliseconds;
        configuration.AuthGracePeriodMilliseconds = gracePeriodMilliseconds;
        configuration.SessionTimeoutMilliseconds = sessionTimeoutMilliseconds;
        configuration.MultiRoundTimeoutMilliseconds =
            multiRoundTimeoutMilliseconds;
        result.Machine.CreateStatefulServerSession(configuration);
        return result;
    }

    private static DiameterMessage CreateServerApplicationRequest(
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
        AddServerSessionId(result, sessionId);
        return result;
    }

    private static DiameterMessage CreateServerBaseRequest(
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
        AddServerSessionId(result, sessionId);
        return result;
    }

    private static DiameterMessage CreateServerBaseAnswer(
        DiameterMessage request,
        uint resultCode)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = request.Flags & DiameterMessageFlags.Proxyable;
        result.CommandCode = request.CommandCode;
        result.ApplicationId = request.ApplicationId;
        result.HopByHopIdentifier = request.HopByHopIdentifier;
        result.EndToEndIdentifier = request.EndToEndIdentifier;
        AddServerSessionId(
            result,
            Encoding.UTF8.GetString(request.Avps[0].Data));
        AddServerUnsigned32(result, DiameterAvpCodes.ResultCode, resultCode);
        return result;
    }

    private static void AddServerSessionId(
        DiameterMessage message,
        string sessionId)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = DiameterAvpCodes.SessionId;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = Encoding.UTF8.GetBytes(sessionId);
        message.Avps.Add(avp);
    }

    private static void AddServerUnsigned32(
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

    private static DiameterIdentity CreateServerIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class ServerSessionTestContext
{
    internal FakeDiscoveryClock Clock = new FakeDiscoveryClock();
    internal SessionTable Table = new SessionTable();
    internal AuthorizationServerStateMachine Machine = null!;
    internal string SessionId = string.Empty;
}
