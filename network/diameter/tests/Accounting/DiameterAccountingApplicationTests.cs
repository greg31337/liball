using System;

namespace diameter.Tests;

public class DiameterAccountingApplicationTests
{
    [Test]
    public void ConstructorRejectsEveryMissingIdentityAndDependency()
    {
        DiameterIdentity host =
            CreateAccountingApplicationIdentity("accounting.example");
        DiameterIdentity realm =
            CreateAccountingApplicationIdentity("example");
        DiameterBaseAvpFactory avps = new DiameterBaseAvpFactory();
        TestDiameterClock clock = new TestDiameterClock(
            DateTimeOffset.FromUnixTimeSeconds(1000),
            0);
        DiameterBaseCommandFactory commands =
            new DiameterBaseCommandFactory(
                new AccountingServerIdentifierSource());
        ScriptedAccountingRequestClient requests =
            new ScriptedAccountingRequestClient();
        DuplicateRequestStore duplicates =
            new DuplicateRequestStore(clock, 64, 60000);
        RecordingAccountingHandler handler =
            new RecordingAccountingHandler();
        DiameterInMemoryAccountingRecordStore records =
            new DiameterInMemoryAccountingRecordStore();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new DiameterAccountingApplication(
                    null, realm, avps, commands, requests, duplicates,
                    handler, records, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterAccountingApplication(
                    host, null, avps, commands, requests, duplicates,
                    handler, records, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterAccountingApplication(
                    host, realm, null, commands, requests, duplicates,
                    handler, records, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterAccountingApplication(
                    host, realm, avps, null, requests, duplicates,
                    handler, records, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterAccountingApplication(
                    host, realm, avps, commands, null, duplicates,
                    handler, records, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterAccountingApplication(
                    host, realm, avps, commands, requests, null,
                    handler, records, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterAccountingApplication(
                    host, realm, avps, commands, requests, duplicates,
                    null, records, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterAccountingApplication(
                    host, realm, avps, commands, requests, duplicates,
                    handler, null, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterAccountingApplication(
                    host, realm, avps, commands, requests, duplicates,
                    handler, records, null));
        });
    }

    [Test]
    public void CapabilityIsOwnedAndAdvertisesBaseAccounting()
    {
        AccountingApplicationTestContext context =
            CreateAccountingApplicationContext();
        IDiameterAccountingApplication application =
            context.Application;

        DiameterApplicationId first =
            application.ReadDiameterAccountingCapability();
        first.Id = 99U;
        first.IsAccounting = false;
        DiameterApplicationId second =
            application.ReadDiameterAccountingCapability();

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Id,
                Is.EqualTo(DiameterApplicationIds.BaseAccounting));
            Assert.That(second.VendorId,
                Is.EqualTo(DiameterVendorIds.Ietf));
            Assert.That(second.IsAccounting, Is.True);
        });
    }

    [Test]
    public void ApplicationComposesOutboundClientAndAnswerCorrelation()
    {
        AccountingApplicationTestContext context =
            CreateAccountingApplicationContext();
        DiameterAccountingRequest request =
            CreateAccountingApplicationClientRequest("application-client;1");

        DiameterAccountingResult sent =
            context.Application.SendDiameterAccountingEvent(request);
        context.RequestClient.CompleteAccountingRequest(
            0,
            DiameterResultCodes.Success,
            0,
            0);
        DiameterAccountingRecord record =
            context.Application.ReadDiameterAccountingRecords()[0];

        Assert.Multiple(() =>
        {
            Assert.That(sent.Accepted, Is.True);
            Assert.That(context.RequestClient.Handlers[0],
                Is.SameAs(context.Application));
            Assert.That(record.State,
                Is.EqualTo(DiameterAccountingState.Closed));
            Assert.That(record.Acknowledged, Is.True);
            Assert.That(record.ResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
        });
    }

    [Test]
    public void ApplicationProcessesInboundRequestAndOwnsPublicOutcome()
    {
        AccountingApplicationTestContext context =
            CreateAccountingApplicationContext();
        DiameterMessage request = CreateAccountingApplicationServerRequest(
            context,
            "application-server;1",
            0);

        DiameterAccountingProcessingResult result =
            context.Application.ProcessDiameterAccountingRequest(
                request,
                CreateAccountingApplicationIdentity(
                    "transport.remote.example"));
        request.Avps[0].Data[0] = (byte)'X';

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.ApplicationInvoked, Is.True);
            Assert.That(result.Duplicate, Is.False);
            Assert.That(result.Request.SessionId,
                Is.EqualTo("application-server;1"));
            Assert.That(result.Request.PeerIdentity!.Value,
                Is.EqualTo("transport.remote.example"));
            Assert.That(result.Answer, Is.Not.Null);
            Assert.That(result.Answer!.ResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(result.Answer.Message, Is.Not.Null);
            Assert.That(result.Answer.Message!.CommandCode,
                Is.EqualTo(DiameterCommandCodes.Accounting));
            Assert.That(context.Handler.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ApplicationReplaysDuplicateWithoutSecondHandoff()
    {
        AccountingApplicationTestContext context =
            CreateAccountingApplicationContext();
        DiameterMessage request = CreateAccountingApplicationServerRequest(
            context,
            "application-duplicate;1",
            0);
        context.Application.ProcessDiameterAccountingRequest(request, null);
        DiameterMessage duplicate =
            AccountingRecordCloner.CloneOptionalAccountingMessage(request)!;
        duplicate.HopByHopIdentifier =
            duplicate.HopByHopIdentifier + 100U;
        duplicate.Flags = duplicate.Flags |
            DiameterMessageFlags.PotentiallyRetransmitted;

        DiameterAccountingProcessingResult result =
            context.Application.ProcessDiameterAccountingRequest(
                duplicate,
                null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Duplicate, Is.True);
            Assert.That(result.Replayed, Is.True);
            Assert.That(result.ApplicationInvoked, Is.False);
            Assert.That(result.Answer, Is.Not.Null);
            Assert.That(result.Answer!.Message, Is.Not.Null);
            Assert.That(result.Answer.Message!.HopByHopIdentifier,
                Is.EqualTo(duplicate.HopByHopIdentifier));
            Assert.That(context.Handler.Requests, Has.Count.EqualTo(1));
        });
    }

    private static AccountingApplicationTestContext
        CreateAccountingApplicationContext()
    {
        AccountingApplicationTestContext result =
            new AccountingApplicationTestContext();
        result.Clock = new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                12,
                0,
                0,
                TimeSpan.Zero),
            0);
        result.Identifiers = new AccountingServerIdentifierSource();
        result.CommandFactory =
            new DiameterBaseCommandFactory(result.Identifiers);
        result.RequestClient = new ScriptedAccountingRequestClient();
        result.Handler = new RecordingAccountingHandler();
        result.Application = new DiameterAccountingApplication(
            CreateAccountingApplicationIdentity("accounting.example"),
            CreateAccountingApplicationIdentity("example"),
            new DiameterBaseAvpFactory(),
            result.CommandFactory,
            result.RequestClient,
            new DuplicateRequestStore(result.Clock, 64, 60000),
            result.Handler,
            new DiameterInMemoryAccountingRecordStore(),
            result.Clock);
        return result;
    }

    private static DiameterAccountingRequest
        CreateAccountingApplicationClientRequest(string sessionId)
    {
        DiameterAccountingRequest result =
            new DiameterAccountingRequest();
        result.SessionId = sessionId;
        result.ApplicationId = DiameterApplicationIds.BaseAccounting;
        result.DestinationRealm =
            CreateAccountingApplicationIdentity("remote.example");
        result.UserName = "alice@remote.example";
        return result;
    }

    private static DiameterMessage CreateAccountingApplicationServerRequest(
        AccountingApplicationTestContext context,
        string sessionId,
        uint recordNumber)
    {
        DiameterBaseCommandOptions options =
            new DiameterBaseCommandOptions();
        options.OriginHost =
            CreateAccountingApplicationIdentity("client.remote.example");
        options.OriginRealm =
            CreateAccountingApplicationIdentity("remote.example");
        options.DestinationRealm =
            CreateAccountingApplicationIdentity("example");
        options.SessionId = sessionId;
        options.AccountingRecordType =
            DiameterAccountingRecordType.Event;
        options.AccountingRecordNumber = recordNumber;
        DiameterMessage result =
            context.CommandFactory.CreateAccountingRequest(options);
        result.Avps.Add(new DiameterBaseAvpFactory()
            .CreateAcctApplicationIdAvp(
                DiameterApplicationIds.BaseAccounting));
        return result;
    }

    private static DiameterIdentity CreateAccountingApplicationIdentity(
        string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class AccountingApplicationTestContext
{
    internal TestDiameterClock Clock = null!;
    internal AccountingServerIdentifierSource Identifiers = null!;
    internal DiameterBaseCommandFactory CommandFactory = null!;
    internal ScriptedAccountingRequestClient RequestClient = null!;
    internal RecordingAccountingHandler Handler = null!;
    internal DiameterAccountingApplication Application = null!;
}
