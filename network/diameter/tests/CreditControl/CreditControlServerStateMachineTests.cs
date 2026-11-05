using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class CreditControlServerStateMachineTests
{
    [Test]
    public void ServerPersistsInitialUpdateAndTerminationDecisions()
    {
        CreditControlServerTestContext context = CreateServerContext();
        DiameterCreditControlRequest initial = CreateServerRequest(
            DiameterCreditControlRequestType.Initial,
            0U);

        DiameterCreditControlResult opened =
            context.Machine.ProcessCreditControlServerRequest(initial);
        initial.UserName = "changed@example.net";
        context.Clock.UtcTime = context.Clock.UtcTime.AddSeconds(1);
        DiameterCreditControlResult updated =
            context.Machine.ProcessCreditControlServerRequest(
                CreateServerRequest(
                    DiameterCreditControlRequestType.Update,
                    1U));
        context.Clock.UtcTime = context.Clock.UtcTime.AddSeconds(1);
        DiameterCreditControlResult closed =
            context.Machine.ProcessCreditControlServerRequest(
                CreateServerRequest(
                    DiameterCreditControlRequestType.Termination,
                    2U));
        DiameterCreditControlSession? persisted =
            context.Store.LoadDiameterCreditControlSession(
                "server.example;credit;1");

        Assert.Multiple(() =>
        {
            Assert.That(opened.State,
                Is.EqualTo(DiameterCreditControlState.Open));
            Assert.That(opened.Successful, Is.True);
            Assert.That(opened.ContinueService, Is.True);
            Assert.That(opened.Session!.NextRequestNumber, Is.EqualTo(1U));
            Assert.That(updated.State,
                Is.EqualTo(DiameterCreditControlState.Open));
            Assert.That(updated.Session!.NextRequestNumber, Is.EqualTo(2U));
            Assert.That(closed.State,
                Is.EqualTo(DiameterCreditControlState.Closed));
            Assert.That(closed.TerminateService, Is.True);
            Assert.That(context.Handler.Requests, Has.Count.EqualTo(3));
            Assert.That(context.Handler.Requests[0].UserName,
                Is.EqualTo("alice@remote.example"));
            Assert.That(persisted!.State,
                Is.EqualTo(DiameterCreditControlState.Closed));
            Assert.That(persisted.Version, Is.EqualTo(3));
            Assert.That(persisted.LastRequest!.RequestType,
                Is.EqualTo(DiameterCreditControlRequestType.Termination));
            Assert.That(persisted.LastAnswer!.RequestNumber, Is.EqualTo(2U));
        });
    }

    [Test]
    public void PersistedDuplicatesReplayWithoutAnotherRatingDecision()
    {
        CreditControlServerTestContext context = CreateServerContext();
        DiameterCreditControlRequest initial = CreateServerRequest(
            DiameterCreditControlRequestType.Initial,
            0U);
        DiameterCreditControlResult first =
            context.Machine.ProcessCreditControlServerRequest(initial);
        DiameterCreditControlAnswer returnedAnswer =
            context.Handler.LastAnswer!;
        returnedAnswer.UserName = "mutated-answer";
        CreditControlServerStateMachine recovered =
            new CreditControlServerStateMachine(
                context.Store,
                context.Handler,
                context.Clock);

        DiameterCreditControlResult duplicateInitial =
            recovered.ProcessCreditControlServerRequest(
                CreateServerRequest(
                    DiameterCreditControlRequestType.Initial,
                    0U));
        recovered.ProcessCreditControlServerRequest(
            CreateServerRequest(
                DiameterCreditControlRequestType.Update,
                1U));
        DiameterCreditControlRequest termination = CreateServerRequest(
            DiameterCreditControlRequestType.Termination,
            2U);
        DiameterCreditControlResult terminationResult =
            recovered.ProcessCreditControlServerRequest(termination);
        CreditControlServerStateMachine secondRecovery =
            new CreditControlServerStateMachine(
                context.Store,
                context.Handler,
                context.Clock);
        DiameterCreditControlResult duplicateTermination =
            secondRecovery.ProcessCreditControlServerRequest(
                CreateServerRequest(
                    DiameterCreditControlRequestType.Termination,
                    2U));

        Assert.Multiple(() =>
        {
            Assert.That(first.Duplicate, Is.False);
            Assert.That(duplicateInitial.Duplicate, Is.True);
            Assert.That(duplicateInitial.Retransmission, Is.True);
            Assert.That(duplicateInitial.Code,
                Is.EqualTo("credit-control-answer-replayed"));
            Assert.That(duplicateInitial.Answer!.UserName,
                Is.EqualTo("rated-user"));
            Assert.That(terminationResult.State,
                Is.EqualTo(DiameterCreditControlState.Closed));
            Assert.That(duplicateTermination.Duplicate, Is.True);
            Assert.That(duplicateTermination.Answer!.RequestNumber,
                Is.EqualTo(2U));
            Assert.That(duplicateTermination.State,
                Is.EqualTo(DiameterCreditControlState.Closed));
            Assert.That(context.Handler.Requests, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void UnknownSkippedAndPostTerminalRequestsBypassRating()
    {
        CreditControlServerTestContext context = CreateServerContext();
        DiameterCreditControlResult unknown =
            context.Machine.ProcessCreditControlServerRequest(
                CreateServerRequest(
                    DiameterCreditControlRequestType.Update,
                    1U));
        context.Machine.ProcessCreditControlServerRequest(
            CreateServerRequest(
                DiameterCreditControlRequestType.Initial,
                0U));
        DiameterCreditControlResult skipped =
            context.Machine.ProcessCreditControlServerRequest(
                CreateServerRequest(
                    DiameterCreditControlRequestType.Update,
                    2U));
        context.Machine.ProcessCreditControlServerRequest(
            CreateServerRequest(
                DiameterCreditControlRequestType.Termination,
                1U));
        DiameterCreditControlResult closed =
            context.Machine.ProcessCreditControlServerRequest(
                CreateServerRequest(
                    DiameterCreditControlRequestType.Update,
                    2U));

        Assert.Multiple(() =>
        {
            Assert.That(unknown.ResultCode,
                Is.EqualTo(DiameterResultCodes.UnknownSessionId));
            Assert.That(skipped.ResultCode,
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(skipped.Code,
                Is.EqualTo("credit-control-request-out-of-order"));
            Assert.That(closed.ResultCode,
                Is.EqualTo(DiameterResultCodes.UnknownSessionId));
            Assert.That(context.Handler.Requests, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void ReusedRequestNumberWithDifferentDataIsNotReRated()
    {
        CreditControlServerTestContext context = CreateServerContext();
        context.Machine.ProcessCreditControlServerRequest(
            CreateServerRequest(
                DiameterCreditControlRequestType.Initial,
                0U));
        DiameterCreditControlRequest collision = CreateServerRequest(
            DiameterCreditControlRequestType.Initial,
            0U);
        collision.UserName = "mallory@remote.example";

        DiameterCreditControlResult rejected =
            context.Machine.ProcessCreditControlServerRequest(collision);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.ResultCode,
                Is.EqualTo(DiameterResultCodes.UnableToComply));
            Assert.That(rejected.Code,
                Is.EqualTo("credit-control-request-key-collision"));
            Assert.That(rejected.Duplicate, Is.True);
            Assert.That(context.Handler.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void RatingRejectionIsPersistedAndReplayedIdempotently()
    {
        CreditControlServerTestContext context = CreateServerContext();
        context.Handler.ResultCode =
            DiameterResultCodes.CreditLimitReached;
        DiameterCreditControlRequest request = CreateServerRequest(
            DiameterCreditControlRequestType.Initial,
            0U);

        DiameterCreditControlResult rejected =
            context.Machine.ProcessCreditControlServerRequest(request);
        DiameterCreditControlResult replayed =
            new CreditControlServerStateMachine(
                context.Store,
                context.Handler,
                context.Clock)
                .ProcessCreditControlServerRequest(
                    CreateServerRequest(
                        DiameterCreditControlRequestType.Initial,
                        0U));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.State,
                Is.EqualTo(DiameterCreditControlState.Failed));
            Assert.That(rejected.Successful, Is.False);
            Assert.That(rejected.TerminateService, Is.True);
            Assert.That(replayed.Duplicate, Is.True);
            Assert.That(replayed.ResultCode,
                Is.EqualTo(DiameterResultCodes.CreditLimitReached));
            Assert.That(context.Handler.Requests, Has.Count.EqualTo(1));
        });
    }

    private static CreditControlServerTestContext CreateServerContext()
    {
        CreditControlServerTestContext result =
            new CreditControlServerTestContext();
        result.Clock = new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                16,
                0,
                0,
                TimeSpan.Zero),
            0);
        result.Store = new TestCreditControlStore();
        result.Handler = new TestCreditControlRequestHandler();
        result.Machine = new CreditControlServerStateMachine(
            result.Store,
            result.Handler,
            result.Clock);
        return result;
    }

    private static DiameterCreditControlRequest CreateServerRequest(
        DiameterCreditControlRequestType requestType,
        uint requestNumber)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = "server.example;credit;1";
        result.DestinationRealm.Value = "local.example";
        result.DestinationHost = new DiameterIdentity();
        result.DestinationHost.Value = "credit.local.example";
        result.PeerIdentity = new DiameterIdentity();
        result.PeerIdentity.Value = "peer.remote.example";
        result.ServiceContextId = "example.service";
        result.UserName = "alice@remote.example";
        result.RequestType = requestType;
        result.RequestNumber = requestNumber;
        result.OriginStateIdentifier = 71U;
        result.OriginStateIdentifierPresent = true;
        result.CorrelationId = new byte[] { 4, 5, 6 };
        result.ServiceIdentifiers.Add(7U);
        if (requestType == DiameterCreditControlRequestType.Termination)
        {
            result.TerminationCause = DiameterTerminationCause.Logout;
            result.TerminationCausePresent = true;
        }

        return result;
    }
}

internal class CreditControlServerTestContext
{
    internal TestDiameterClock Clock = null!;
    internal TestCreditControlStore Store = null!;
    internal TestCreditControlRequestHandler Handler = null!;
    internal CreditControlServerStateMachine Machine = null!;
}

internal class TestCreditControlRequestHandler :
    ICreditControlRequestHandler
{
    internal List<DiameterCreditControlRequest> Requests =
        new List<DiameterCreditControlRequest>();
    internal uint ResultCode = DiameterResultCodes.Success;
    internal DiameterCreditControlAnswer? LastAnswer;

    public DiameterCreditControlAnswer HandleDiameterCreditControlRequest(
        DiameterCreditControlRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException("A rating request is required.");
        }

        Requests.Add(
            CreditControlDataCloner.CloneCreditControlRequest(request));
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = request.SessionId;
        result.RequestType = request.RequestType;
        result.RequestNumber = request.RequestNumber;
        result.ResultCode = ResultCode;
        result.UserName = "rated-user";
        result.Disposition = ResultCode >= 2000U && ResultCode < 3000U ?
            DiameterCreditControlDisposition.Granted :
            DiameterCreditControlDisposition.Failed;
        result.GrantedServiceUnit = new DiameterServiceUnit();
        result.GrantedServiceUnit.Time = 60U;
        result.GrantedServiceUnit.TimePresent = true;
        LastAnswer = result;
        return result;
    }
}
