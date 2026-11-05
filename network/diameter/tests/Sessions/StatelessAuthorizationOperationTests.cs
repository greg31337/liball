using System.Text;

namespace diameter.Tests;

public class StatelessAuthorizationOperationTests
{
    [Test]
    public void ClientSuccessGrantsAccessAndLeavesNoSessionRecord()
    {
        StatelessAuthorizationTestContext context =
            CreateStatelessContext();
        DiameterSessionConfiguration configuration =
            CreateStatelessConfiguration(
                "stateless.example;1",
                DiameterSessionRole.Client);
        DiameterMessage request = CreateStatelessRequest(
            configuration.SessionId,
            10);
        AddStatelessUnsigned32(
            request,
            DiameterAvpCodes.AuthSessionState,
            (uint)DiameterAuthSessionState.NoStateMaintained);
        DiameterMessage answer = CreateStatelessAnswer(
            request,
            DiameterResultCodes.Success);
        AddStatelessUnsigned32(
            answer,
            DiameterAvpCodes.AuthSessionState,
            (uint)DiameterAuthSessionState.NoStateMaintained);
        AddStatelessUnsigned32(
            answer,
            DiameterAvpCodes.SessionTimeout,
            3);

        StatelessAuthorizationResult result =
            context.Operation.ExecuteStatelessClientAuthorization(
                configuration,
                request,
                answer);
        request.Avps[0].Data[0] = 99;
        answer.Avps[0].Data[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Completed, Is.True);
            Assert.That(result.AccessGranted, Is.True);
            Assert.That(result.StatefulSessionRequired, Is.False);
            Assert.That(result.AuthSessionState,
                Is.EqualTo(
                    DiameterAuthSessionState.NoStateMaintained));
            Assert.That(result.SessionDeadlinePresent, Is.True);
            Assert.That(result.SessionDeadline,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(3)));
            Assert.That(result.Request!.Avps[0].Data,
                Is.EqualTo(
                    Encoding.UTF8.GetBytes(configuration.SessionId)));
            Assert.That(result.Answer!.Avps[0].Data,
                Is.EqualTo(
                    Encoding.UTF8.GetBytes(configuration.SessionId)));
            Assert.That(context.Table.ReadSessionRecordCount(), Is.Zero);
        });
    }

    [Test]
    public void ClientFailureCompletesWithoutGrantOrResidualState()
    {
        StatelessAuthorizationTestContext context =
            CreateStatelessContext();
        DiameterSessionConfiguration configuration =
            CreateStatelessConfiguration(
                "stateless.example;2",
                DiameterSessionRole.Client);
        DiameterMessage request = CreateStatelessRequest(
            configuration.SessionId,
            20);
        DiameterMessage answer = CreateStatelessAnswer(
            request,
            DiameterResultCodes.AuthorizationRejected);

        StatelessAuthorizationResult result =
            context.Operation.ExecuteStatelessClientAuthorization(
                configuration,
                request,
                answer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Completed, Is.True);
            Assert.That(result.AccessGranted, Is.False);
            Assert.That(result.DisconnectRequired, Is.False);
            Assert.That(result.AnswerResultCode,
                Is.EqualTo(DiameterResultCodes.AuthorizationRejected));
            Assert.That(result.Code,
                Is.EqualTo("stateless-client-authorization-rejected"));
            Assert.That(context.Table.ReadSessionRecordCount(), Is.Zero);
        });
    }

    [Test]
    public void StatefulAnswerDirectiveIsSurfacedWithoutRetainingState()
    {
        StatelessAuthorizationTestContext context =
            CreateStatelessContext();
        DiameterSessionConfiguration configuration =
            CreateStatelessConfiguration(
                "stateless.example;3",
                DiameterSessionRole.Client);
        DiameterMessage request = CreateStatelessRequest(
            configuration.SessionId,
            30);
        DiameterMessage answer = CreateStatelessAnswer(
            request,
            DiameterResultCodes.Success);

        StatelessAuthorizationResult result =
            context.Operation.ExecuteStatelessClientAuthorization(
                configuration,
                request,
                answer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.AccessGranted, Is.True);
            Assert.That(result.StatefulSessionRequired, Is.True);
            Assert.That(result.AuthSessionState,
                Is.EqualTo(DiameterAuthSessionState.StateMaintained));
            Assert.That(result.Code,
                Is.EqualTo(
                    "stateless-client-stateful-session-required"));
            Assert.That(context.Table.ReadSessionRecordCount(), Is.Zero);
        });
    }

    [Test]
    public void ServerAnswersInStatelessModeWithoutCreatingSessionState()
    {
        StatelessAuthorizationTestContext context =
            CreateStatelessContext();
        DiameterSessionConfiguration authorizedConfiguration =
            CreateStatelessConfiguration(
                "stateless.example;4",
                DiameterSessionRole.Server);
        DiameterMessage authorizedRequest = CreateStatelessRequest(
            authorizedConfiguration.SessionId,
            40);
        AddStatelessUnsigned32(
            authorizedRequest,
            DiameterAvpCodes.AuthSessionState,
            (uint)DiameterAuthSessionState.NoStateMaintained);
        StatelessAuthorizationResult authorized =
            context.Operation.ExecuteStatelessServerAuthorization(
                authorizedConfiguration,
                authorizedRequest,
                DiameterResultCodes.Success);

        DiameterSessionConfiguration deniedConfiguration =
            CreateStatelessConfiguration(
                "stateless.example;5",
                DiameterSessionRole.Server);
        StatelessAuthorizationResult denied =
            context.Operation.ExecuteStatelessServerAuthorization(
                deniedConfiguration,
                CreateStatelessRequest(
                    deniedConfiguration.SessionId,
                    41),
                DiameterResultCodes.AuthorizationRejected);
        authorizedRequest.Avps[0].Data[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(authorized.AuthorizationAnswerRequired, Is.True);
            Assert.That(authorized.AccessGranted, Is.True);
            Assert.That(authorized.AuthSessionState,
                Is.EqualTo(
                    DiameterAuthSessionState.NoStateMaintained));
            Assert.That(authorized.Request!.Avps[0].Data,
                Is.EqualTo(
                    Encoding.UTF8.GetBytes(
                        authorizedConfiguration.SessionId)));
            Assert.That(denied.AuthorizationAnswerRequired, Is.True);
            Assert.That(denied.AccessGranted, Is.False);
            Assert.That(denied.AnswerResultCode,
                Is.EqualTo(DiameterResultCodes.AuthorizationRejected));
            Assert.That(context.Table.ReadSessionRecordCount(), Is.Zero);
        });
    }

    [Test]
    public void ClientTimeoutAndLocalServiceEndRequireOnlyLocalDisconnect()
    {
        StatelessAuthorizationTestContext timeoutContext =
            CreateStatelessContext();
        StatelessAuthorizationResult active =
            CreateGrantedStatelessAccess(
                timeoutContext,
                "stateless.example;6",
                2);
        timeoutContext.Clock.AdvanceDiscoveryTime(1);
        StatelessAuthorizationResult notDue =
            timeoutContext.Operation.EvaluateStatelessClientAccess(active);
        timeoutContext.Clock.AdvanceDiscoveryTime(1);
        StatelessAuthorizationResult expired =
            timeoutContext.Operation.EvaluateStatelessClientAccess(active);

        StatelessAuthorizationTestContext endContext =
            CreateStatelessContext();
        StatelessAuthorizationResult service =
            CreateGrantedStatelessAccess(
                endContext,
                "stateless.example;7",
                0);
        StatelessAuthorizationResult ended =
            endContext.Operation.EndStatelessClientAccess(service);

        Assert.Multiple(() =>
        {
            Assert.That(notDue.Changed, Is.False);
            Assert.That(notDue.AccessGranted, Is.True);
            Assert.That(expired.Changed, Is.True);
            Assert.That(expired.AccessGranted, Is.False);
            Assert.That(expired.DisconnectRequired, Is.True);
            Assert.That(expired.Code,
                Is.EqualTo(
                    "stateless-client-session-timeout-expired"));
            Assert.That(ended.AccessGranted, Is.False);
            Assert.That(ended.DisconnectRequired, Is.True);
            Assert.That(ended.Code,
                Is.EqualTo("stateless-client-service-ended"));
            Assert.That(
                timeoutContext.Table.ReadSessionRecordCount(),
                Is.Zero);
            Assert.That(endContext.Table.ReadSessionRecordCount(), Is.Zero);
        });
    }

    [Test]
    public void InvalidOrMultiRoundAnswersDoNotLeaveSessionState()
    {
        StatelessAuthorizationTestContext invalidContext =
            CreateStatelessContext();
        DiameterSessionConfiguration invalidConfiguration =
            CreateStatelessConfiguration(
                "stateless.example;8",
                DiameterSessionRole.Client);
        DiameterMessage invalidRequest = CreateStatelessRequest(
            invalidConfiguration.SessionId,
            50);
        DiameterMessage invalidAnswer = CreateStatelessAnswer(
            invalidRequest,
            DiameterResultCodes.Success);
        invalidAnswer.HopByHopIdentifier = 999;
        StatelessAuthorizationResult invalid =
            invalidContext.Operation.ExecuteStatelessClientAuthorization(
                invalidConfiguration,
                invalidRequest,
                invalidAnswer);

        StatelessAuthorizationTestContext multiContext =
            CreateStatelessContext();
        DiameterSessionConfiguration multiConfiguration =
            CreateStatelessConfiguration(
                "stateless.example;9",
                DiameterSessionRole.Client);
        DiameterMessage multiRequest = CreateStatelessRequest(
            multiConfiguration.SessionId,
            51);
        StatelessAuthorizationResult multi =
            multiContext.Operation.ExecuteStatelessClientAuthorization(
                multiConfiguration,
                multiRequest,
                CreateStatelessAnswer(
                    multiRequest,
                    DiameterResultCodes.MultiRoundAuth));

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Accepted, Is.False);
            Assert.That(invalid.Completed, Is.True);
            Assert.That(invalid.DisconnectRequired, Is.True);
            Assert.That(multi.Accepted, Is.True);
            Assert.That(multi.AccessGranted, Is.False);
            Assert.That(multi.Code,
                Is.EqualTo(
                    "stateless-client-multi-round-not-supported"));
            Assert.That(
                invalidContext.Table.ReadSessionRecordCount(),
                Is.Zero);
            Assert.That(multiContext.Table.ReadSessionRecordCount(), Is.Zero);
        });
    }

    [Test]
    public void RetainedSessionIdentifierAndServerMultiRoundAreRejected()
    {
        StatelessAuthorizationTestContext retainedContext =
            CreateStatelessContext();
        DiameterSessionRecord retained = new DiameterSessionRecord();
        retained.SessionId = "stateless.example;10";
        retained.Configuration = CreateStatefulServerConfiguration(
            retained.SessionId);
        retained.CreatedAt = retainedContext.Clock.UtcTime;
        retained.UpdatedAt = retainedContext.Clock.UtcTime;
        retained.StateChangedAt = retainedContext.Clock.UtcTime;
        retainedContext.Table.SaveSessionRecord(retained, 0);
        DiameterSessionConfiguration retainedConfiguration =
            CreateStatelessConfiguration(
                retained.SessionId,
                DiameterSessionRole.Server);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => retainedContext.Operation.
                    ExecuteStatelessServerAuthorization(
                        retainedConfiguration,
                        CreateStatelessRequest(retained.SessionId, 60),
                        DiameterResultCodes.Success),
                Throws.TypeOf<DiameterSessionException>());
            Assert.That(
                () => CreateStatelessContext().Operation.
                    ExecuteStatelessServerAuthorization(
                        CreateStatelessConfiguration(
                            "stateless.example;11",
                            DiameterSessionRole.Server),
                        CreateStatelessRequest(
                            "stateless.example;11",
                            61),
                        DiameterResultCodes.MultiRoundAuth),
                Throws.TypeOf<ArgumentException>());
            Assert.That(retainedContext.Table.ReadSessionRecordCount(),
                Is.EqualTo(1));
        });
    }

    private static StatelessAuthorizationResult CreateGrantedStatelessAccess(
        StatelessAuthorizationTestContext context,
        string sessionId,
        uint timeoutSeconds)
    {
        DiameterSessionConfiguration configuration =
            CreateStatelessConfiguration(
                sessionId,
                DiameterSessionRole.Client);
        DiameterMessage request = CreateStatelessRequest(sessionId, 1);
        DiameterMessage answer = CreateStatelessAnswer(
            request,
            DiameterResultCodes.Success);
        AddStatelessUnsigned32(
            answer,
            DiameterAvpCodes.AuthSessionState,
            (uint)DiameterAuthSessionState.NoStateMaintained);
        if (timeoutSeconds > 0)
        {
            AddStatelessUnsigned32(
                answer,
                DiameterAvpCodes.SessionTimeout,
                timeoutSeconds);
        }

        return context.Operation.ExecuteStatelessClientAuthorization(
            configuration,
            request,
            answer);
    }

    private static StatelessAuthorizationTestContext CreateStatelessContext()
    {
        StatelessAuthorizationTestContext result =
            new StatelessAuthorizationTestContext();
        result.Clock = new FakeDiscoveryClock();
        result.Table = new SessionTable();
        result.Operation = new StatelessAuthorizationOperation(
            result.Table,
            result.Clock);
        return result;
    }

    private static DiameterSessionConfiguration CreateStatelessConfiguration(
        string sessionId,
        DiameterSessionRole role)
    {
        DiameterSessionConfiguration result =
            new DiameterSessionConfiguration();
        result.SessionId = sessionId;
        result.Kind = DiameterSessionKind.StatelessAuthorization;
        result.Role = role;
        result.ApplicationId = 4;
        result.AuthSessionState =
            DiameterAuthSessionState.NoStateMaintained;
        result.PeerIdentity =
            CreateStatelessIdentity("peer.example");
        result.UserName = "alice@remote.example";
        if (role == DiameterSessionRole.Client)
        {
            result.DestinationRealm =
                CreateStatelessIdentity("remote.example");
        }

        return result;
    }

    private static DiameterSessionConfiguration CreateStatefulServerConfiguration(
        string sessionId)
    {
        DiameterSessionConfiguration result =
            new DiameterSessionConfiguration();
        result.SessionId = sessionId;
        result.Role = DiameterSessionRole.Server;
        result.ApplicationId = 4;
        return result;
    }

    private static DiameterMessage CreateStatelessRequest(
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
        AddStatelessSessionId(result, sessionId);
        return result;
    }

    private static DiameterMessage CreateStatelessAnswer(
        DiameterMessage request,
        uint resultCode)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = request.Flags & DiameterMessageFlags.Proxyable;
        result.CommandCode = request.CommandCode;
        result.ApplicationId = request.ApplicationId;
        result.HopByHopIdentifier = request.HopByHopIdentifier;
        result.EndToEndIdentifier = request.EndToEndIdentifier;
        AddStatelessSessionId(
            result,
            Encoding.UTF8.GetString(request.Avps[0].Data));
        AddStatelessUnsigned32(
            result,
            DiameterAvpCodes.ResultCode,
            resultCode);
        return result;
    }

    private static void AddStatelessSessionId(
        DiameterMessage message,
        string sessionId)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = DiameterAvpCodes.SessionId;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = Encoding.UTF8.GetBytes(sessionId);
        message.Avps.Add(avp);
    }

    private static void AddStatelessUnsigned32(
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

    private static DiameterIdentity CreateStatelessIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class StatelessAuthorizationTestContext
{
    internal FakeDiscoveryClock Clock = new FakeDiscoveryClock();
    internal SessionTable Table = new SessionTable();
    internal StatelessAuthorizationOperation Operation = null!;
}
