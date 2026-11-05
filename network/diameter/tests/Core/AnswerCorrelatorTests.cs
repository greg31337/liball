using System;
using System.Threading;
using System.Threading.Tasks;

namespace diameter.Tests;

public class AnswerCorrelatorTests
{
    [Test]
    public void SuccessfulAnswerCorrelatesAndCompletesPendingRequest()
    {
        AnswerCorrelationTestContext context =
            CreateAnswerCorrelationTestContext(101, 5000, true);
        DiameterMessage answer = CreateAnswerCorrelationTestAnswer(
            context.Request,
            DiameterResultCodes.Success,
            DiameterMessageFlags.Proxyable);

        AnswerCorrelationResult result = context.Correlator.CorrelateDiameterAnswer(
            "PEER.EXAMPLE",
            answer);
        answer.Avps[0].Data[3] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(result.Correlated, Is.True);
            Assert.That(result.Completed, Is.True);
            Assert.That(result.Late, Is.False);
            Assert.That(result.Discarded, Is.False);
            Assert.That(result.Code, Is.EqualTo("answer-correlated"));
            Assert.That(result.Handler, Is.SameAs(context.Handler));
            Assert.That(result.PendingRequest!.RequestIdentifier,
                Is.EqualTo(context.Registration.PendingRequest!.RequestIdentifier));
            Assert.That(result.AnswerResult, Is.Not.Null);
            Assert.That(result.AnswerResult!.AnswerReceived, Is.True);
            Assert.That(result.AnswerResult.Correlated, Is.True);
            Assert.That(result.AnswerResult.Successful, Is.True);
            Assert.That(result.AnswerResult.ResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(result.AnswerResult.ResultClass,
                Is.EqualTo(DiameterResultClass.Success));
            Assert.That(result.AnswerResult.ProtocolError, Is.False);
            Assert.That(result.AnswerResult.ApplicationError, Is.False);
            Assert.That(NetworkByteOrder.ReadUInt32(
                result.AnswerResult.Answer!.Avps[0].Data),
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(context.Table.ReadPendingRequestCount(), Is.Zero);
        });
    }

    [Test]
    public void HeaderFailuresAreDiscardedWithoutConsumingPendingRequest()
    {
        AnswerCorrelationTestContext context =
            CreateAnswerCorrelationTestContext(102, 5000, true);
        DiameterMessage answer = CreateAnswerCorrelationTestAnswer(
            context.Request,
            DiameterResultCodes.Success,
            DiameterMessageFlags.Proxyable);
        answer.Flags = answer.Flags | DiameterMessageFlags.Request;
        AnswerCorrelationResult requestBit =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        answer.Flags = DiameterMessageFlags.Proxyable |
            DiameterMessageFlags.PotentiallyRetransmitted;
        AnswerCorrelationResult retransmitted =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        answer.Flags = DiameterMessageFlags.Proxyable;
        answer.Version = 2;
        AnswerCorrelationResult version =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        answer.Version = DiameterProtocolConstants.Version;
        answer.Flags = (DiameterMessageFlags)1;
        AnswerCorrelationResult reserved =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        answer.Flags = DiameterMessageFlags.Proxyable;
        answer.Avps = null!;
        AnswerCorrelationResult storage =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);

        Assert.Multiple(() =>
        {
            Assert.That(requestBit.Code, Is.EqualTo("answer-request-bit-set"));
            Assert.That(retransmitted.Code,
                Is.EqualTo("answer-retransmitted-bit-set"));
            Assert.That(version.Code,
                Is.EqualTo("unsupported-answer-version"));
            Assert.That(reserved.Code,
                Is.EqualTo("invalid-answer-header-bits"));
            Assert.That(storage.Code, Is.EqualTo("invalid-answer-storage"));
            Assert.That(requestBit.Discarded, Is.True);
            Assert.That(context.Table.ReadPendingRequestCount(), Is.EqualTo(1));
            Assert.That(context.Table.ReadPendingRequestTombstoneCount(), Is.Zero);
        });
    }

    [Test]
    public void CorrelationRejectsCommandApplicationIdentifiersProxyBitAndPath()
    {
        AnswerCorrelationTestContext context =
            CreateAnswerCorrelationTestContext(103, 5000, true);
        DiameterMessage answer = CreateAnswerCorrelationTestAnswer(
            context.Request,
            DiameterResultCodes.Success,
            DiameterMessageFlags.Proxyable);
        answer.CommandCode = 301;
        AnswerCorrelationResult command =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        answer.CommandCode = context.Request.CommandCode;
        answer.ApplicationId = 5;
        AnswerCorrelationResult application =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        answer.ApplicationId = context.Request.ApplicationId;
        answer.EndToEndIdentifier = answer.EndToEndIdentifier + 1;
        AnswerCorrelationResult endToEnd =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        answer.EndToEndIdentifier = context.Request.EndToEndIdentifier;
        answer.Flags = DiameterMessageFlags.None;
        AnswerCorrelationResult proxy =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        answer.Flags = DiameterMessageFlags.Proxyable;
        answer.HopByHopIdentifier = answer.HopByHopIdentifier + 1;
        AnswerCorrelationResult hopByHop =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        answer.HopByHopIdentifier = context.Request.HopByHopIdentifier;
        AnswerCorrelationResult path =
            context.Correlator.CorrelateDiameterAnswer("other.example", answer);

        Assert.Multiple(() =>
        {
            Assert.That(command.Code,
                Is.EqualTo("answer-command-code-mismatch"));
            Assert.That(application.Code,
                Is.EqualTo("answer-application-id-mismatch"));
            Assert.That(endToEnd.Code,
                Is.EqualTo("answer-end-to-end-mismatch"));
            Assert.That(proxy.Code,
                Is.EqualTo("answer-proxyable-bit-mismatch"));
            Assert.That(hopByHop.Code,
                Is.EqualTo("answer-hop-by-hop-mismatch"));
            Assert.That(path.Code,
                Is.EqualTo("answer-peer-path-mismatch"));
            Assert.That(context.Table.ReadPendingRequestCount(), Is.EqualTo(1));
            Assert.That(context.Table.ReadPendingRequestTombstoneCount(), Is.Zero);
        });
    }

    [Test]
    public void StandardResultSelectionAndErrorBitMustBeSemanticallyValid()
    {
        AnswerCorrelationTestContext context =
            CreateAnswerCorrelationTestContext(104, 5000, true);
        DiameterMessage answer = CreateAnswerCorrelationTestAnswer(
            context.Request,
            DiameterResultCodes.Success,
            DiameterMessageFlags.Proxyable);
        answer.Avps.Clear();
        AnswerCorrelationResult missing =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        DiameterAvp malformed = CreateAnswerCorrelationResultAvp(
            DiameterResultCodes.Success);
        malformed.Data = new byte[3];
        answer.Avps.Add(malformed);
        AnswerCorrelationResult invalidLength =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        answer.Avps.Clear();
        answer.Avps.Add(CreateAnswerCorrelationResultAvp(
            DiameterResultCodes.Success));
        answer.Avps.Add(CreateAnswerCorrelationResultAvp(
            DiameterResultCodes.Success));
        AnswerCorrelationResult duplicate =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        SetAnswerCorrelationStandardResult(
            answer,
            DiameterResultCodes.UnableToDeliver);
        AnswerCorrelationResult missingErrorBit =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        answer.Flags = DiameterMessageFlags.Proxyable |
            DiameterMessageFlags.Error;
        SetAnswerCorrelationStandardResult(
            answer,
            DiameterResultCodes.AuthenticationRejected);
        AnswerCorrelationResult transientErrorBit =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        SetAnswerCorrelationStandardResult(
            answer,
            DiameterResultCodes.UnableToDeliver);
        AnswerCorrelationResult protocol =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Code,
                Is.EqualTo("invalid-answer-result-selection"));
            Assert.That(invalidLength.Code,
                Is.EqualTo("invalid-answer-result-code"));
            Assert.That(duplicate.Code,
                Is.EqualTo("invalid-answer-result-selection"));
            Assert.That(missingErrorBit.Code,
                Is.EqualTo("answer-error-bit-result-mismatch"));
            Assert.That(transientErrorBit.Code,
                Is.EqualTo("answer-error-bit-result-mismatch"));
            Assert.That(protocol.Completed, Is.True);
            Assert.That(protocol.AnswerResult!.ErrorAnswer, Is.True);
            Assert.That(protocol.AnswerResult.ProtocolError, Is.True);
            Assert.That(protocol.AnswerResult.ApplicationError, Is.False);
            Assert.That(protocol.AnswerResult.ResultClass,
                Is.EqualTo(DiameterResultClass.ProtocolError));
            Assert.That(context.Table.ReadPendingRequestCount(), Is.Zero);
        });
    }

    [Test]
    public void PermanentFailuresSeparateApplicationAndProtocolErrorOutcomes()
    {
        AnswerCorrelationTestContext applicationContext =
            CreateAnswerCorrelationTestContext(105, 5000, true);
        DiameterMessage applicationAnswer = CreateAnswerCorrelationTestAnswer(
            applicationContext.Request,
            DiameterResultCodes.UnableToComply,
            DiameterMessageFlags.Proxyable);
        AnswerCorrelationResult application =
            applicationContext.Correlator.CorrelateDiameterAnswer(
                "peer.example",
                applicationAnswer);
        AnswerCorrelationTestContext protocolContext =
            CreateAnswerCorrelationTestContext(106, 5000, true);
        DiameterMessage protocolAnswer = CreateAnswerCorrelationTestAnswer(
            protocolContext.Request,
            DiameterResultCodes.UnableToComply,
            DiameterMessageFlags.Proxyable | DiameterMessageFlags.Error);
        AnswerCorrelationResult protocol =
            protocolContext.Correlator.CorrelateDiameterAnswer(
                "peer.example",
                protocolAnswer);

        Assert.Multiple(() =>
        {
            Assert.That(application.Completed, Is.True);
            Assert.That(application.AnswerResult!.ApplicationError, Is.True);
            Assert.That(application.AnswerResult.ProtocolError, Is.False);
            Assert.That(application.AnswerResult.Successful, Is.False);
            Assert.That(protocol.Completed, Is.True);
            Assert.That(protocol.AnswerResult!.ProtocolError, Is.True);
            Assert.That(protocol.AnswerResult.ApplicationError, Is.False);
            Assert.That(protocol.AnswerResult.ResultClass,
                Is.EqualTo(DiameterResultClass.PermanentFailure));
        });
    }

    [Test]
    public void ExperimentalResultIsValidatedOwnedAndClassified()
    {
        AnswerCorrelationTestContext context =
            CreateAnswerCorrelationTestContext(107, 5000, true);
        DiameterMessage answer = CreateAnswerCorrelationTestAnswer(
            context.Request,
            DiameterResultCodes.Success,
            DiameterMessageFlags.Proxyable);
        DiameterExperimentalResult experimental = new DiameterExperimentalResult();
        experimental.VendorId = 10415;
        experimental.ResultCode = 5000123;
        DiameterAvp experimentalAvp =
            new DiameterBaseAvpFactory().CreateExperimentalResultAvp(experimental);
        answer.Avps.Clear();
        answer.Avps.Add(experimentalAvp);
        answer.Flags = DiameterMessageFlags.Proxyable |
            DiameterMessageFlags.Error;
        AnswerCorrelationResult errorBit =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        answer.Flags = DiameterMessageFlags.Proxyable;
        answer.Avps.Add(CreateAnswerCorrelationResultAvp(
            DiameterResultCodes.Success));
        AnswerCorrelationResult both =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        answer.Avps.Clear();
        DiameterAvp malformed = new DiameterAvp();
        malformed.Code = DiameterAvpCodes.ExperimentalResult;
        malformed.Data = new byte[] { 1, 2, 3, 4 };
        answer.Avps.Add(malformed);
        AnswerCorrelationResult invalid =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        answer.Avps.Clear();
        answer.Avps.Add(experimentalAvp);
        AnswerCorrelationResult accepted =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);
        experimental.ResultCode = 2001;

        Assert.Multiple(() =>
        {
            Assert.That(errorBit.Code,
                Is.EqualTo("invalid-answer-result-selection"));
            Assert.That(both.Code,
                Is.EqualTo("invalid-answer-result-selection"));
            Assert.That(invalid.Code,
                Is.EqualTo("invalid-answer-experimental-result"));
            Assert.That(accepted.Completed, Is.True);
            Assert.That(accepted.AnswerResult!.ResultCode, Is.Zero);
            Assert.That(accepted.AnswerResult.ExperimentalResult, Is.Not.Null);
            Assert.That(accepted.AnswerResult.ExperimentalResult!.VendorId,
                Is.EqualTo(10415));
            Assert.That(accepted.AnswerResult.ExperimentalResult.ResultCode,
                Is.EqualTo(5000123));
            Assert.That(accepted.AnswerResult.ResultClass,
                Is.EqualTo(DiameterResultClass.PermanentFailure));
            Assert.That(accepted.AnswerResult.ApplicationError, Is.True);
        });
    }

    [Test]
    public void TimedOutAnswerIsLateAndUnknownAnswerIsDiscarded()
    {
        AnswerCorrelationTestContext context =
            CreateAnswerCorrelationTestContext(108, 1000, true);
        context.Clock.AdvanceDiscoveryTime(1);
        context.Table.ExpirePendingRequests();
        DiameterMessage answer = CreateAnswerCorrelationTestAnswer(
            context.Request,
            DiameterResultCodes.Success,
            DiameterMessageFlags.Proxyable);

        AnswerCorrelationResult late = context.Correlator.CorrelateDiameterAnswer(
            "peer.example",
            answer);
        answer.HopByHopIdentifier = 999;
        answer.EndToEndIdentifier = 999;
        AnswerCorrelationResult unknown =
            context.Correlator.CorrelateDiameterAnswer("peer.example", answer);

        Assert.Multiple(() =>
        {
            Assert.That(late.Correlated, Is.True);
            Assert.That(late.Completed, Is.False);
            Assert.That(late.Late, Is.True);
            Assert.That(late.Code, Is.EqualTo("late-answer-correlated"));
            Assert.That(late.Handler, Is.SameAs(context.Handler));
            Assert.That(late.AnswerResult!.Late, Is.True);
            Assert.That(late.AnswerResult.Successful, Is.True);
            Assert.That(unknown.Correlated, Is.False);
            Assert.That(unknown.Discarded, Is.True);
            Assert.That(unknown.Code, Is.EqualTo("answer-not-pending"));
            Assert.That(context.Table.ReadPendingRequestCount(), Is.Zero);
        });
    }

    [Test]
    public void ConcurrentAnswersProduceOneCompletionAndOneLateOutcome()
    {
        AnswerCorrelationTestContext context =
            CreateAnswerCorrelationTestContext(109, 5000, true);
        DiameterMessage firstAnswer = CreateAnswerCorrelationTestAnswer(
            context.Request,
            DiameterResultCodes.Success,
            DiameterMessageFlags.Proxyable);
        DiameterMessage secondAnswer = CreateAnswerCorrelationTestAnswer(
            context.Request,
            DiameterResultCodes.Success,
            DiameterMessageFlags.Proxyable);
        Barrier barrier = new Barrier(3);
        Task<AnswerCorrelationResult> firstTask = Task.Run(() =>
        {
            barrier.SignalAndWait();
            return context.Correlator.CorrelateDiameterAnswer(
                "peer.example",
                firstAnswer);
        });
        Task<AnswerCorrelationResult> secondTask = Task.Run(() =>
        {
            barrier.SignalAndWait();
            return context.Correlator.CorrelateDiameterAnswer(
                "peer.example",
                secondAnswer);
        });
        barrier.SignalAndWait();
        Task.WaitAll(firstTask, secondTask);
        int completed = (firstTask.Result.Completed ? 1 : 0) +
            (secondTask.Result.Completed ? 1 : 0);
        int late = (firstTask.Result.Late ? 1 : 0) +
            (secondTask.Result.Late ? 1 : 0);

        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(late, Is.EqualTo(1));
            Assert.That(firstTask.Result.Correlated, Is.True);
            Assert.That(secondTask.Result.Correlated, Is.True);
            Assert.That(context.Table.ReadPendingRequestCount(), Is.Zero);
            Assert.That(context.Table.ReadPendingRequestTombstoneCount(),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void ResultClassBoundariesAndConflictingSelectionsAreExhaustive()
    {
        AnswerCorrelationTestContext informationalContext =
            CreateAnswerCorrelationTestContext(110, 5000, true);
        AnswerCorrelationResult informational =
            informationalContext.Correlator.CorrelateDiameterAnswer(
                "peer.example",
                CreateAnswerCorrelationTestAnswer(
                    informationalContext.Request,
                    1000U,
                    DiameterMessageFlags.Proxyable));
        AnswerCorrelationTestContext transientContext =
            CreateAnswerCorrelationTestContext(111, 5000, true);
        AnswerCorrelationResult transient =
            transientContext.Correlator.CorrelateDiameterAnswer(
                "peer.example",
                CreateAnswerCorrelationTestAnswer(
                    transientContext.Request,
                    4000U,
                    DiameterMessageFlags.Proxyable));
        AnswerCorrelationTestContext lowContext =
            CreateAnswerCorrelationTestContext(112, 5000, true);
        AnswerCorrelationResult low =
            lowContext.Correlator.CorrelateDiameterAnswer(
                "peer.example",
                CreateAnswerCorrelationTestAnswer(
                    lowContext.Request,
                    999U,
                    DiameterMessageFlags.Proxyable));
        AnswerCorrelationTestContext highContext =
            CreateAnswerCorrelationTestContext(113, 5000, true);
        AnswerCorrelationResult high =
            highContext.Correlator.CorrelateDiameterAnswer(
                "peer.example",
                CreateAnswerCorrelationTestAnswer(
                    highContext.Request,
                    6000U,
                    DiameterMessageFlags.Proxyable));
        AnswerCorrelationTestContext conflictContext =
            CreateAnswerCorrelationTestContext(114, 5000, true);
        DiameterMessage conflictAnswer = CreateAnswerCorrelationTestAnswer(
            conflictContext.Request,
            DiameterResultCodes.UnableToDeliver,
            DiameterMessageFlags.Proxyable | DiameterMessageFlags.Error);
        DiameterExperimentalResult experimental =
            new DiameterExperimentalResult();
        experimental.VendorId = 10415U;
        experimental.ResultCode = 5000123U;
        conflictAnswer.Avps.Add(
            new DiameterBaseAvpFactory().CreateExperimentalResultAvp(
                experimental));
        AnswerCorrelationResult conflict =
            conflictContext.Correlator.CorrelateDiameterAnswer(
                "peer.example",
                conflictAnswer);

        Assert.Multiple(() =>
        {
            Assert.That(informational.Completed, Is.True);
            Assert.That(informational.AnswerResult!.ResultClass,
                Is.EqualTo(DiameterResultClass.Informational));
            Assert.That(informational.AnswerResult.Successful, Is.False);
            Assert.That(informational.AnswerResult.ApplicationError,
                Is.False);
            Assert.That(transient.Completed, Is.True);
            Assert.That(transient.AnswerResult!.ResultClass,
                Is.EqualTo(DiameterResultClass.TransientFailure));
            Assert.That(transient.AnswerResult.ApplicationError, Is.True);
            Assert.That(low.Code,
                Is.EqualTo("invalid-answer-result-class"));
            Assert.That(high.Code,
                Is.EqualTo("invalid-answer-result-class"));
            Assert.That(conflict.Code,
                Is.EqualTo("invalid-answer-result-selection"));
            Assert.That(lowContext.Table.ReadPendingRequestCount(),
                Is.EqualTo(1));
            Assert.That(highContext.Table.ReadPendingRequestCount(),
                Is.EqualTo(1));
            Assert.That(conflictContext.Table.ReadPendingRequestCount(),
                Is.EqualTo(1));
        });
    }

    private static AnswerCorrelationTestContext
        CreateAnswerCorrelationTestContext(
            uint hopByHopIdentifier,
            int timeoutMilliseconds,
            bool reportLateAnswers)
    {
        AnswerCorrelationTestContext result = new AnswerCorrelationTestContext();
        result.Table = new PendingRequestTable(result.Clock, 8, 5000);
        result.Correlator = new AnswerCorrelator(result.Table);
        result.Request.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.Request.CommandCode = 300;
        result.Request.ApplicationId = 4;
        result.Request.HopByHopIdentifier = hopByHopIdentifier;
        result.Request.EndToEndIdentifier = hopByHopIdentifier + 1000;
        DiameterSendRequest sendRequest = new DiameterSendRequest();
        sendRequest.Request = result.Request;
        sendRequest.TimeoutMilliseconds = timeoutMilliseconds;
        sendRequest.ReportLateAnswers = reportLateAnswers;
        DiameterIdentity peerIdentity = new DiameterIdentity();
        peerIdentity.Value = "peer.example";
        result.Registration = result.Table.RegisterPendingRequest(
            sendRequest,
            peerIdentity,
            result.Request,
            result.Handler,
            true);
        return result;
    }

    private static DiameterMessage CreateAnswerCorrelationTestAnswer(
        DiameterMessage request,
        uint resultCode,
        DiameterMessageFlags flags)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = flags;
        result.CommandCode = request.CommandCode;
        result.ApplicationId = request.ApplicationId;
        result.HopByHopIdentifier = request.HopByHopIdentifier;
        result.EndToEndIdentifier = request.EndToEndIdentifier;
        result.Avps.Add(CreateAnswerCorrelationResultAvp(resultCode));
        return result;
    }

    private static DiameterAvp CreateAnswerCorrelationResultAvp(uint resultCode)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = DiameterAvpCodes.ResultCode;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(result.Data, resultCode);
        return result;
    }

    private static void SetAnswerCorrelationStandardResult(
        DiameterMessage answer,
        uint resultCode)
    {
        answer.Avps.Clear();
        answer.Avps.Add(CreateAnswerCorrelationResultAvp(resultCode));
    }
}

internal class AnswerCorrelationTestContext
{
    internal FakeDiscoveryClock Clock = new FakeDiscoveryClock();
    internal PendingRequestTable Table = null!;
    internal AnswerCorrelator Correlator = null!;
    internal PendingRequestTestHandler Handler = new PendingRequestTestHandler();
    internal DiameterMessage Request = new DiameterMessage();
    internal PendingRequestTableResult Registration =
        new PendingRequestTableResult();
}
