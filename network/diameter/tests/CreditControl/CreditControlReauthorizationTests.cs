using System;

namespace diameter.Tests;

public class CreditControlReauthorizationTests
{
    [Test]
    public void CreditControlReAuthFactoryCreatesValidScopedRarAndRaa()
    {
        DiameterBaseCommandFactory factory =
            new DiameterBaseCommandFactory(
                new BaseCommandTestIdentifierSource());
        DiameterBaseCommandOptions options = CreateReAuthOptions();

        DiameterMessage request = factory.CreateReAuthRequest(options);
        options.Request = request;
        DiameterMessage answer = factory.CreateReAuthAnswer(options);
        DiameterDictionarySnapshot dictionary =
            new DiameterDictionary().ReadDictionarySnapshot();
        DiameterValidator validator = new DiameterValidator();
        DiameterValidationResult requestValidation = validator
            .ValidateMessage(
                request,
                dictionary,
                new DiameterLimitConfiguration());
        DiameterValidationResult answerValidation = validator
            .ValidateMessage(
                answer,
                dictionary,
                new DiameterLimitConfiguration());

        Assert.Multiple(() =>
        {
            Assert.That(request.ApplicationId,
                Is.EqualTo(DiameterApplicationIds.CreditControl));
            Assert.That(answer.ApplicationId,
                Is.EqualTo(DiameterApplicationIds.CreditControl));
            Assert.That(requestValidation.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(answerValidation.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(ReadUnsigned64(
                request,
                DiameterAvpCodes.CcSubSessionId), Is.EqualTo(9UL));
            Assert.That(ReadUnsigned32(
                request,
                DiameterAvpCodes.GsuPoolIdentifier), Is.EqualTo(77U));
            Assert.That(ReadUnsigned32(
                answer,
                DiameterAvpCodes.ServiceIdentifier), Is.EqualTo(10U));
            Assert.That(ReadUnsigned32(
                answer,
                DiameterAvpCodes.RatingGroup), Is.EqualTo(100U));
        });
    }

    [Test]
    public void ServerRarAndCcrUpdateCoordinateBothSessionMachines()
    {
        TestDiameterClock clock = CreateClock();
        TestCreditControlStore store = new TestCreditControlStore();
        SaveOpenCreditControlSession(
            store,
            CreateOpenCreditControlSession(
                DiameterSessionRole.Server,
                clock.UtcTime));
        SessionTable table = new SessionTable();
        AuthorizationServerStateMachine authorization =
            CreateOpenAuthorizationServer(table, clock);
        CreditControlServerStateMachine creditControl =
            new CreditControlServerStateMachine(
                store,
                new TestCreditControlRequestHandler(),
                clock);
        DiameterBaseCommandFactory factory =
            new DiameterBaseCommandFactory(
                new BaseCommandTestIdentifierSource());
        DiameterBaseCommandOptions options = CreateReAuthOptions();
        DiameterMessage rar = factory.CreateReAuthRequest(options);

        CreditControlReauthorizationResult pending =
            CreditControlReauthorizationController
                .BeginCreditControlServerReauthorization(
                    store,
                    authorization,
                    SessionId,
                    rar,
                    901);
        options.Request = rar;
        DiameterMessage raa = factory.CreateReAuthAnswer(options);
        CreditControlReauthorizationResult acknowledged =
            CreditControlReauthorizationController
                .ProcessCreditControlServerReAuthAnswer(
                    store,
                    authorization,
                    SessionId,
                    raa);
        DiameterCreditControlRequest update = CreateUpdateRequest();
        DiameterMessage encodedUpdate = CreateApplicationRequest(
            DiameterCommandCodes.CreditControl,
            51U);
        DiameterCreditControlResult updated =
            CreditControlReauthorizationController
                .ProcessCreditControlServerReauthorizationUpdate(
                    creditControl,
                    authorization,
                    update,
                    encodedUpdate);
        DiameterSessionRecord? baseSession =
            table.ReadSessionRecord(SessionId);

        Assert.Multiple(() =>
        {
            Assert.That(pending.ReAuthRequestRequired, Is.True);
            Assert.That(pending.Session!.ReauthorizationRequested,
                Is.True);
            Assert.That(acknowledged.CreditControlUpdateRequired,
                Is.True);
            Assert.That(updated.Successful, Is.True);
            Assert.That(updated.Session!.ReauthorizationRequested,
                Is.False);
            Assert.That(baseSession!.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(baseSession.ReauthorizationRequested, Is.False);
        });
    }

    [Test]
    public void ClientRarTriggersCoordinatedCcrUpdateAndCcaCompletion()
    {
        TestDiameterClock clock = CreateClock();
        TestCreditControlStore store = new TestCreditControlStore();
        SaveOpenCreditControlSession(
            store,
            CreateOpenCreditControlSession(
                DiameterSessionRole.Client,
                clock.UtcTime));
        SessionTable table = new SessionTable();
        AuthorizationClientStateMachine authorization =
            CreateOpenAuthorizationClient(table, clock);
        CreditControlClientStateMachine creditControl =
            new CreditControlClientStateMachine(store, clock);
        DiameterBaseCommandFactory factory =
            new DiameterBaseCommandFactory(
                new BaseCommandTestIdentifierSource());
        DiameterBaseCommandOptions options = CreateReAuthOptions();
        DiameterMessage rar = factory.CreateReAuthRequest(options);

        CreditControlReauthorizationResult accepted =
            CreditControlReauthorizationController
                .ProcessCreditControlClientReAuthRequest(
                    store,
                    authorization,
                    SessionId,
                    rar,
                    true);
        DiameterCreditControlRequest update = CreateUpdateRequest();
        DiameterMessage encodedUpdate = CreateApplicationRequest(
            DiameterCommandCodes.CreditControl,
            61U);
        DiameterCreditControlResult pending =
            CreditControlReauthorizationController
                .BeginCreditControlClientReauthorizationUpdate(
                    creditControl,
                    authorization,
                    update,
                    encodedUpdate,
                    902);
        DiameterCreditControlAnswer answer = CreateUpdateAnswer();
        DiameterMessage encodedAnswer = CreateApplicationAnswer(
            encodedUpdate,
            DiameterResultCodes.Success);
        DiameterCreditControlResult completed =
            CreditControlReauthorizationController
                .ProcessCreditControlClientReauthorizationAnswer(
                    creditControl,
                    authorization,
                    902,
                    answer,
                    encodedAnswer);
        DiameterSessionRecord? baseSession =
            table.ReadSessionRecord(SessionId);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.ReAuthAnswerRequired, Is.True);
            Assert.That(accepted.AnswerResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(accepted.CreditControlUpdateRequired, Is.True);
            Assert.That(pending.State,
                Is.EqualTo(DiameterCreditControlState.PendingUpdate));
            Assert.That(completed.State,
                Is.EqualTo(DiameterCreditControlState.Open));
            Assert.That(completed.Session!.ReauthorizationRequested,
                Is.False);
            Assert.That(baseSession!.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(baseSession.ReauthorizationRequested, Is.False);
        });
    }

    [Test]
    public void UnknownReAuthScopeDoesNotChangeEitherSession()
    {
        TestDiameterClock clock = CreateClock();
        TestCreditControlStore store = new TestCreditControlStore();
        SaveOpenCreditControlSession(
            store,
            CreateOpenCreditControlSession(
                DiameterSessionRole.Client,
                clock.UtcTime));
        SessionTable table = new SessionTable();
        AuthorizationClientStateMachine authorization =
            CreateOpenAuthorizationClient(table, clock);
        DiameterBaseCommandFactory factory =
            new DiameterBaseCommandFactory(
                new BaseCommandTestIdentifierSource());
        DiameterBaseCommandOptions options = CreateReAuthOptions();
        options.RatingGroup = 999U;
        DiameterMessage rar = factory.CreateReAuthRequest(options);

        Assert.Throws<DiameterCreditControlException>(() =>
            CreditControlReauthorizationController
                .ProcessCreditControlClientReAuthRequest(
                    store,
                    authorization,
                    SessionId,
                    rar,
                    true));
        DiameterCreditControlSession? creditSession =
            store.LoadDiameterCreditControlSession(SessionId);
        DiameterSessionRecord? baseSession =
            table.ReadSessionRecord(SessionId);

        Assert.Multiple(() =>
        {
            Assert.That(creditSession!.ReauthorizationRequested,
                Is.False);
            Assert.That(creditSession.State,
                Is.EqualTo(DiameterCreditControlState.Open));
            Assert.That(baseSession!.ReauthorizationRequested, Is.False);
            Assert.That(baseSession.State,
                Is.EqualTo(DiameterSessionState.Open));
        });
    }

    [Test]
    public void ClientRefusalFailsCreditControlAndAuthorizationSessions()
    {
        TestDiameterClock clock = CreateClock();
        TestCreditControlStore store = new TestCreditControlStore();
        SaveOpenCreditControlSession(
            store,
            CreateOpenCreditControlSession(
                DiameterSessionRole.Client,
                clock.UtcTime));
        SessionTable table = new SessionTable();
        AuthorizationClientStateMachine authorization =
            CreateOpenAuthorizationClient(table, clock);
        DiameterBaseCommandFactory factory =
            new DiameterBaseCommandFactory(
                new BaseCommandTestIdentifierSource());
        DiameterMessage rar = factory.CreateReAuthRequest(
            CreateReAuthOptions());

        CreditControlReauthorizationResult refused =
            CreditControlReauthorizationController
                .ProcessCreditControlClientReAuthRequest(
                    store,
                    authorization,
                    SessionId,
                    rar,
                    false);
        DiameterSessionRecord? baseSession =
            table.ReadSessionRecord(SessionId);

        Assert.Multiple(() =>
        {
            Assert.That(refused.ReAuthAnswerRequired, Is.True);
            Assert.That(refused.CreditControlUpdateRequired, Is.False);
            Assert.That(refused.AnswerResultCode,
                Is.EqualTo(DiameterResultCodes.UnableToComply));
            Assert.That(refused.Session!.State,
                Is.EqualTo(DiameterCreditControlState.Failed));
            Assert.That(refused.Session.ReauthorizationRequested, Is.False);
            Assert.That(baseSession!.State,
                Is.EqualTo(DiameterSessionState.Failed));
        });
    }

    [Test]
    public void FailedServerReAuthAnswerFailsCreditControlSession()
    {
        TestDiameterClock clock = CreateClock();
        TestCreditControlStore store = new TestCreditControlStore();
        SaveOpenCreditControlSession(
            store,
            CreateOpenCreditControlSession(
                DiameterSessionRole.Server,
                clock.UtcTime));
        SessionTable table = new SessionTable();
        AuthorizationServerStateMachine authorization =
            CreateOpenAuthorizationServer(table, clock);
        DiameterBaseCommandFactory factory =
            new DiameterBaseCommandFactory(
                new BaseCommandTestIdentifierSource());
        DiameterBaseCommandOptions options = CreateReAuthOptions();
        DiameterMessage rar = factory.CreateReAuthRequest(options);
        CreditControlReauthorizationController
            .BeginCreditControlServerReauthorization(
                store,
                authorization,
                SessionId,
                rar,
                903L);
        options.Request = rar;
        options.ResultCode = DiameterResultCodes.AuthorizationRejected;
        DiameterMessage raa = factory.CreateReAuthAnswer(options);

        CreditControlReauthorizationResult failed =
            CreditControlReauthorizationController
                .ProcessCreditControlServerReAuthAnswer(
                    store,
                    authorization,
                    SessionId,
                    raa);
        DiameterSessionRecord? baseSession =
            table.ReadSessionRecord(SessionId);

        Assert.Multiple(() =>
        {
            Assert.That(failed.CreditControlUpdateRequired, Is.False);
            Assert.That(failed.Session!.State,
                Is.EqualTo(DiameterCreditControlState.Failed));
            Assert.That(failed.Session.ReauthorizationRequested, Is.False);
            Assert.That(baseSession, Is.Null);
        });
    }

    private const string SessionId = "node.example;credit;reauth";

    private static TestDiameterClock CreateClock()
    {
        return new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                20,
                0,
                0,
                TimeSpan.Zero),
            0);
    }

    private static DiameterBaseCommandOptions CreateReAuthOptions()
    {
        DiameterBaseCommandOptions result =
            new DiameterBaseCommandOptions();
        result.OriginHost.Value = "server.example";
        result.OriginRealm.Value = "example";
        result.DestinationHost = CreateIdentity("client.example");
        result.DestinationRealm = CreateIdentity("remote.example");
        result.SessionId = SessionId;
        result.AuthApplicationId = DiameterApplicationIds.CreditControl;
        result.CommandApplicationId =
            DiameterApplicationIds.CreditControl;
        result.ReAuthRequestType =
            DiameterReAuthRequestType.AuthorizeOnly;
        result.CreditControlSubSessionId = 9UL;
        result.CreditControlSubSessionIdPresent = true;
        result.GsuPoolIdentifier = 77U;
        result.GsuPoolIdentifierPresent = true;
        result.ServiceIdentifier = 10U;
        result.ServiceIdentifierPresent = true;
        result.RatingGroup = 100U;
        result.RatingGroupPresent = true;
        return result;
    }

    private static DiameterCreditControlSession
        CreateOpenCreditControlSession(
            DiameterSessionRole role,
            DateTimeOffset now)
    {
        DiameterCreditControlSession result =
            new DiameterCreditControlSession();
        result.SessionId = SessionId;
        result.Role = role;
        result.State = DiameterCreditControlState.Open;
        result.ServiceContextId = "example.credit";
        result.NextRequestNumber = 1U;
        result.CreditControlSubSessionId = 9UL;
        result.CreditControlSubSessionIdPresent = true;
        result.MultipleServicesIndicator =
            DiameterMultipleServicesIndicator.Supported;
        result.MultipleServicesIndicatorPresent = true;
        result.CreatedAt = now;
        result.UpdatedAt = now;
        if (role == DiameterSessionRole.Client)
        {
            result.DestinationRealm = CreateIdentity("remote.example");
        }

        DiameterMultipleServiceCredit credit =
            new DiameterMultipleServiceCredit();
        credit.ServiceIdentifiers.Add(10U);
        credit.RatingGroup = 100U;
        credit.RatingGroupPresent = true;
        DiameterPoolReference pool = new DiameterPoolReference();
        pool.PoolIdentifier = 77U;
        pool.UnitType = DiameterCreditControlUnitType.Time;
        pool.ValueDigits = 1L;
        credit.PoolReferences.Add(pool);
        result.MultipleServiceCredits.Add(credit);
        result.PoolReferences.Add(pool);
        return result;
    }

    private static void SaveOpenCreditControlSession(
        TestCreditControlStore store,
        DiameterCreditControlSession session)
    {
        store.SaveDiameterCreditControlSession(session, 0);
    }

    private static AuthorizationServerStateMachine
        CreateOpenAuthorizationServer(
            SessionTable table,
            TestDiameterClock clock)
    {
        AuthorizationServerStateMachine result =
            new AuthorizationServerStateMachine(table, clock);
        DiameterSessionConfiguration configuration =
            CreateAuthorizationConfiguration(
                DiameterSessionRole.Server);
        result.CreateStatefulServerSession(configuration);
        result.ProcessServerAuthorizationRequest(
            SessionId,
            CreateApplicationRequest(300U, 31U),
            DiameterResultCodes.Success);
        return result;
    }

    private static AuthorizationClientStateMachine
        CreateOpenAuthorizationClient(
            SessionTable table,
            TestDiameterClock clock)
    {
        AuthorizationClientStateMachine result =
            new AuthorizationClientStateMachine(table, clock);
        DiameterSessionConfiguration configuration =
            CreateAuthorizationConfiguration(
                DiameterSessionRole.Client);
        result.CreateStatefulClientSession(configuration);
        DiameterMessage request = CreateApplicationRequest(300U, 41U);
        result.BeginInitialClientAuthorization(SessionId, request, 41);
        result.ProcessClientAuthorizationAnswer(
            SessionId,
            CreateApplicationAnswer(
                request,
                DiameterResultCodes.Success));
        return result;
    }

    private static DiameterSessionConfiguration
        CreateAuthorizationConfiguration(DiameterSessionRole role)
    {
        DiameterSessionConfiguration result =
            new DiameterSessionConfiguration();
        result.SessionId = SessionId;
        result.Role = role;
        result.ApplicationId = DiameterApplicationIds.CreditControl;
        result.UserName = "alice@remote.example";
        result.MultiRoundTimeoutMilliseconds = 5000;
        if (role == DiameterSessionRole.Client)
        {
            result.DestinationRealm = CreateIdentity("remote.example");
            result.DestinationHost = CreateIdentity("server.example");
        }

        return result;
    }

    private static DiameterCreditControlRequest CreateUpdateRequest()
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = SessionId;
        result.DestinationRealm.Value = "remote.example";
        result.ServiceContextId = "example.credit";
        result.RequestType = DiameterCreditControlRequestType.Update;
        result.RequestNumber = 1U;
        return result;
    }

    private static DiameterCreditControlAnswer CreateUpdateAnswer()
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = SessionId;
        result.RequestType = DiameterCreditControlRequestType.Update;
        result.RequestNumber = 1U;
        result.ResultCode = DiameterResultCodes.Success;
        return result;
    }

    private static DiameterMessage CreateApplicationRequest(
        uint commandCode,
        uint identifier)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.CommandCode = commandCode;
        result.ApplicationId = DiameterApplicationIds.CreditControl;
        result.HopByHopIdentifier = identifier;
        result.EndToEndIdentifier = identifier + 100U;
        result.Avps.Add(new DiameterBaseAvpFactory()
            .CreateSessionIdAvp(SessionId));
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
        result.Avps.Add(new DiameterBaseAvpFactory()
            .CreateSessionIdAvp(SessionId));
        DiameterAvp resultAvp = new DiameterAvp();
        resultAvp.Code = DiameterAvpCodes.ResultCode;
        resultAvp.Flags = DiameterAvpFlags.Mandatory;
        resultAvp.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(resultAvp.Data, resultCode);
        result.Avps.Add(resultAvp);
        return result;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static uint ReadUnsigned32(
        DiameterMessage message,
        uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code &&
                message.Avps[index].VendorId == DiameterVendorIds.Ietf)
            {
                return NetworkByteOrder.ReadUInt32(
                    message.Avps[index].Data);
            }

            index = index + 1;
        }

        throw new AssertionException("Required AVP was not found.");
    }

    private static ulong ReadUnsigned64(
        DiameterMessage message,
        uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code &&
                message.Avps[index].VendorId == DiameterVendorIds.Ietf)
            {
                return NetworkByteOrder.ReadUInt64(
                    message.Avps[index].Data);
            }

            index = index + 1;
        }

        throw new AssertionException("Required AVP was not found.");
    }
}
