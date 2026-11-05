using System;
using System.Collections.Generic;
using System.Text;

namespace diameter.Tests;

public class DiameterPeerOverloadTests
{
    [Test]
    public void RequestAdvertisesPeerSupportAndAgentReplacesSourceIdentity()
    {
        DiameterOverloadConfiguration configuration = CreateConfiguration();
        DiameterMessage request = CreateRequest();
        request.Avps.Add(CreateSupportedFeatures(
            17UL,
            "origin.example",
            DiameterOverloadAlgorithm.None));

        DiameterPeerOverloadMessageResult result =
            DiameterPeerOverloadProcessor.PrepareDiameterPeerRequest(
                request,
                configuration,
                DiameterAgentRole.Relay,
                CreateIdentity("agent.example"));
        DiameterPeerFeatureInspection inspection =
            DiameterPeerOverloadProcessor.InspectDiameterPeerFeatures(
                result.Message,
                CreateIdentity("AGENT.EXAMPLE"),
                false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.PeerSupported, Is.True);
            Assert.That(inspection.Valid, Is.True);
            Assert.That(inspection.Supported, Is.True);
            Assert.That(inspection.FeatureVector, Is.EqualTo(17UL));
            Assert.That(inspection.SourceId!.Value,
                Is.EqualTo("agent.example"));
            Assert.That(ReadSourceIdentity(request),
                Is.EqualTo("origin.example"));
            Assert.That(CountSupportedChild(
                result.Message,
                DiameterAvpCodes.OcPeerAlgorithm), Is.Zero);
        });
    }

    [Test]
    public void PeerSupportRequiresTheAdjacentSourceAndAnswerAlgorithm()
    {
        DiameterMessage request = CreateRequest();
        request.Avps.Add(CreateSupportedFeatures(
            17UL,
            "other.example",
            DiameterOverloadAlgorithm.None));
        DiameterPeerFeatureInspection wrongSource =
            DiameterPeerOverloadProcessor.InspectDiameterPeerFeatures(
                request,
                CreateIdentity("peer.example"),
                false);
        DiameterMessage answer = CreateAnswer();
        answer.Avps.Add(CreateSupportedFeatures(
            17UL,
            "peer.example",
            DiameterOverloadAlgorithm.None));
        DiameterPeerFeatureInspection missingAlgorithm =
            DiameterPeerOverloadProcessor.InspectDiameterPeerFeatures(
                answer,
                CreateIdentity("peer.example"),
                true);

        Assert.Multiple(() =>
        {
            Assert.That(wrongSource.Valid, Is.True);
            Assert.That(wrongSource.Supported, Is.False);
            Assert.That(wrongSource.Code,
                Is.EqualTo("peer-overload-not-adjacent"));
            Assert.That(missingAlgorithm.Valid, Is.False);
            Assert.That(missingAlgorithm.Code,
                Is.EqualTo("peer-overload-algorithm-required"));
        });
    }

    [Test]
    public void AgentStripsDownstreamPeerReportAndOriginatesAggregateUpstream()
    {
        DiameterOverloadConfiguration configuration = CreateConfiguration();
        DiameterMessage request = CreateRequest();
        request.Avps.Add(CreateSupportedFeatures(
            17UL,
            "upstream.example",
            DiameterOverloadAlgorithm.None));
        DiameterMessage answer = CreateAnswer();
        answer.Avps.Add(CreateSupportedFeatures(
            17UL,
            "downstream.example",
            DiameterOverloadAlgorithm.Loss));
        answer.Avps.Add(CreateReportAvp(DiameterOverloadReportType.Host));
        answer.Avps.Add(DiameterPeerOverloadProcessor.CreateDiameterPeerReportAvp(
            CreatePeerReport("downstream.example", 4U, 3UL, 20U)));
        List<DiameterOverloadReport> inputs =
            new List<DiameterOverloadReport>();
        inputs.Add(CreatePeerReport("one.example", 4U, 4UL, 10U));
        inputs.Add(CreatePeerReport("two.example", 4U, 5UL, 20U));
        DiameterOverloadReport aggregate =
            DiameterPeerOverloadProcessor.AggregateDiameterPeerLossReports(
                inputs,
                CreateIdentity("agent.example"),
                4U,
                6UL,
                30U);

        DiameterPeerOverloadMessageResult result =
            DiameterPeerOverloadProcessor.PrepareDiameterPeerAnswer(
                request,
                answer,
                configuration,
                DiameterAgentRole.Relay,
                CreateIdentity("agent.example"),
                CreateIdentity("upstream.example"),
                aggregate);
        DiameterPeerFeatureInspection inspection =
            DiameterPeerOverloadProcessor.InspectDiameterPeerFeatures(
                result.Message,
                CreateIdentity("agent.example"),
                true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.PeerSupported, Is.True);
            Assert.That(inspection.Supported, Is.True);
            Assert.That(inspection.PeerAlgorithm,
                Is.EqualTo(DiameterOverloadAlgorithm.Loss));
            Assert.That(CountReports(
                result.Message,
                DiameterOverloadReportType.Host), Is.EqualTo(1));
            Assert.That(CountReports(
                result.Message,
                DiameterOverloadReportType.Peer), Is.EqualTo(1));
            Assert.That(ReadPeerReduction(result.Message), Is.EqualTo(28U));
            Assert.That(ReadPeerSource(result.Message),
                Is.EqualTo("agent.example"));
            Assert.That(CountReports(
                answer,
                DiameterOverloadReportType.Peer), Is.EqualTo(1));
        });
    }

    [Test]
    public void AgentWithholdsPeerInformationFromUnsupportedUpstream()
    {
        DiameterOverloadConfiguration configuration = CreateConfiguration();
        DiameterMessage request = CreateRequest();
        request.Avps.Add(CreateSupportedFeatures(
            1UL,
            null,
            DiameterOverloadAlgorithm.None));
        DiameterMessage answer = CreateAnswer();
        answer.Avps.Add(CreateSupportedFeatures(
            17UL,
            "downstream.example",
            DiameterOverloadAlgorithm.Loss));
        answer.Avps.Add(CreateReportAvp(DiameterOverloadReportType.Host));
        answer.Avps.Add(DiameterPeerOverloadProcessor.CreateDiameterPeerReportAvp(
            CreatePeerReport("downstream.example", 4U, 1UL, 50U)));

        DiameterPeerOverloadMessageResult result =
            DiameterPeerOverloadProcessor.PrepareDiameterPeerAnswer(
                request,
                answer,
                configuration,
                DiameterAgentRole.Proxy,
                CreateIdentity("agent.example"),
                CreateIdentity("upstream.example"),
                null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.PeerSupported, Is.False);
            Assert.That(ReadFeatureVector(result.Message), Is.EqualTo(1UL));
            Assert.That(CountSupportedChild(
                result.Message,
                DiameterAvpCodes.SourceId), Is.Zero);
            Assert.That(CountReports(
                result.Message,
                DiameterOverloadReportType.Peer), Is.Zero);
            Assert.That(CountReports(
                result.Message,
                DiameterOverloadReportType.Host), Is.EqualTo(1));
        });
    }

    [Test]
    public void PeerReportEncodingOwnsExtensionsAndUsesTheSelectedAlgorithm()
    {
        DiameterOverloadReport report = CreatePeerReport(
            "peer.example",
            4U,
            9UL,
            60U);
        DiameterAvp extension = CreateAvp(900U, new byte[] { 7, 8 });
        report.ExtensionAvps.Add(extension);

        DiameterAvp encoded =
            DiameterPeerOverloadProcessor.CreateDiameterPeerReportAvp(report);
        DiameterOverloadReport rate = CreatePeerReport(
            "peer.example",
            4U,
            10UL,
            0U);
        rate.Algorithm = DiameterOverloadAlgorithm.Rate;
        rate.ReductionPercentagePresent = false;
        rate.MaximumRate = 250U;
        rate.MaximumRatePresent = true;
        DiameterAvp encodedRate =
            DiameterPeerOverloadProcessor.CreateDiameterPeerReportAvp(rate);
        extension.Data[0] = 1;
        List<DiameterAvp> children = DecodeChildren(encoded);
        List<DiameterAvp> rateChildren = DecodeChildren(encodedRate);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUnsigned64Child(
                children,
                DiameterAvpCodes.OcSequenceNumber), Is.EqualTo(9UL));
            Assert.That(ReadInteger32Child(
                children,
                DiameterAvpCodes.OcReportType),
                Is.EqualTo((int)DiameterOverloadReportType.Peer));
            Assert.That(ReadUnsigned32Child(
                children,
                DiameterAvpCodes.OcReductionPercentage), Is.EqualTo(60U));
            Assert.That(ReadUnsigned32Child(
                children,
                DiameterAvpCodes.OcValidityDuration), Is.EqualTo(30U));
            Assert.That(ReadTextChild(children, DiameterAvpCodes.SourceId),
                Is.EqualTo("peer.example"));
            Assert.That(FindChild(children, 900U).Data,
                Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(ReadUnsigned32Child(
                rateChildren,
                DiameterAvpCodes.OcMaximumRate), Is.EqualTo(250U));
            Assert.That(CountChild(
                rateChildren,
                DiameterAvpCodes.OcReductionPercentage), Is.Zero);
        });
    }

    [Test]
    public void StateIgnoresSpoofedSourceAndAppliesAdjacentPeerScope()
    {
        OverloadTestClock clock = new OverloadTestClock();
        SequenceOverloadRandom random = new SequenceOverloadRandom(1);
        DiameterOverloadState state = new DiameterOverloadState(
            CreateConfiguration(),
            clock,
            random);
        DiameterOverloadReport report = CreatePeerReport(
            "peer.example",
            4U,
            1UL,
            50U);
        DiameterOverloadReportUpdate ignored =
            state.ProcessDiameterPeerOverloadReport(
                report,
                CreateIdentity("attacker.example"),
                clock.UtcTime);
        DiameterOverloadReportUpdate cached =
            state.ProcessDiameterPeerOverloadReport(
                report,
                CreateIdentity("PEER.EXAMPLE"),
                clock.UtcTime);
        state.ProcessDiameterPeerOverloadReport(
            CreatePeerReport("peer.example", 5U, 1UL, 100U),
            CreateIdentity("peer.example"),
            clock.UtcTime);
        report.SourceId!.Value = "mutated.example";
        DiameterAdmissionDecision decision =
            state.EvaluateDiameterLossAdmission(CreateAdmissionRequest(
                clock,
                "peer.example",
                DiameterOverloadScope.Peer));

        Assert.Multiple(() =>
        {
            Assert.That(ignored.Accepted, Is.True);
            Assert.That(ignored.Updated, Is.False);
            Assert.That(ignored.Code,
                Is.EqualTo("peer-overload-source-ignored"));
            Assert.That(cached.Updated, Is.True);
            Assert.That(state.ReadActiveDiameterOverloadReports(),
                Has.Count.EqualTo(2));
            Assert.That(decision.Action,
                Is.EqualTo(DiameterAdmissionAction.Divert));
            Assert.That(decision.Reports, Has.Count.EqualTo(1));
            Assert.That(decision.Reports[0].ApplicationId, Is.EqualTo(4U));
            Assert.That(decision.Reports[0].SourceId!.Value,
                Is.EqualTo("peer.example"));
            Assert.That(random.ReadCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ApplicationScopeEvaluatesHostBeforePeerAbatement()
    {
        OverloadTestClock clock = new OverloadTestClock();
        SequenceOverloadRandom random = new SequenceOverloadRandom();
        DiameterOverloadState state = new DiameterOverloadState(
            CreateConfiguration(),
            clock,
            random);
        state.ProcessDiameterOverloadReport(
            CreateHostReport("server.example", 100U),
            clock.UtcTime);
        state.ProcessDiameterPeerOverloadReport(
            CreatePeerReport("peer.example", 4U, 1UL, 50U),
            CreateIdentity("peer.example"),
            clock.UtcTime);

        DiameterAdmissionDecision decision =
            state.EvaluateDiameterLossAdmission(CreateAdmissionRequest(
                clock,
                "peer.example",
                DiameterOverloadScope.Application));

        Assert.Multiple(() =>
        {
            Assert.That(decision.Reports, Has.Count.EqualTo(2));
            Assert.That(decision.Reports[0].ReportType,
                Is.EqualTo(DiameterOverloadReportType.Host));
            Assert.That(decision.Reports[1].ReportType,
                Is.EqualTo(DiameterOverloadReportType.Peer));
            Assert.That(decision.OverloadApplied, Is.True);
            Assert.That(random.ReadCount, Is.Zero);
        });
    }

    private static DiameterOverloadConfiguration CreateConfiguration()
    {
        DiameterOverloadConfiguration result =
            new DiameterOverloadConfiguration();
        result.Enabled = true;
        result.EnablePeerReports = true;
        return result;
    }

    private static DiameterMessage CreateRequest()
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.CommandCode = 300U;
        result.ApplicationId = 4U;
        return result;
    }

    private static DiameterMessage CreateAnswer()
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Proxyable;
        result.CommandCode = 300U;
        result.ApplicationId = 4U;
        return result;
    }

    private static DiameterAvp CreateSupportedFeatures(
        ulong vector,
        string? source,
        DiameterOverloadAlgorithm algorithm)
    {
        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateUnsigned64Avp(
            DiameterAvpCodes.OcFeatureVector,
            vector));
        if (source != null)
        {
            children.Add(CreateAvp(
                DiameterAvpCodes.SourceId,
                Encoding.UTF8.GetBytes(source)));
        }

        if (algorithm != DiameterOverloadAlgorithm.None)
        {
            children.Add(CreateUnsigned64Avp(
                DiameterAvpCodes.OcPeerAlgorithm,
                (ulong)(uint)(int)algorithm));
        }

        return CreateGroupedAvp(
            DiameterAvpCodes.OcSupportedFeatures,
            children);
    }

    private static DiameterAvp CreateReportAvp(
        DiameterOverloadReportType type)
    {
        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateInteger32Avp(
            DiameterAvpCodes.OcReportType,
            (int)type));
        return CreateGroupedAvp(DiameterAvpCodes.OcOlr, children);
    }

    private static DiameterOverloadReport CreatePeerReport(
        string peer,
        uint applicationId,
        ulong sequence,
        uint reduction)
    {
        DiameterOverloadReport result = new DiameterOverloadReport();
        result.ReportType = DiameterOverloadReportType.Peer;
        result.SourceId = CreateIdentity(peer);
        result.ApplicationId = applicationId;
        result.SequenceNumber = sequence;
        result.ValidityDurationSeconds = 30U;
        result.ReductionPercentage = reduction;
        result.ReductionPercentagePresent = true;
        return result;
    }

    private static DiameterOverloadReport CreateHostReport(
        string host,
        uint reduction)
    {
        DiameterOverloadReport result = new DiameterOverloadReport();
        result.ReportType = DiameterOverloadReportType.Host;
        result.Host = CreateIdentity(host);
        result.ApplicationId = 4U;
        result.SequenceNumber = 1UL;
        result.ValidityDurationSeconds = 30U;
        result.ReductionPercentage = reduction;
        result.ReductionPercentagePresent = true;
        return result;
    }

    private static DiameterAdmissionRequest CreateAdmissionRequest(
        OverloadTestClock clock,
        string peer,
        DiameterOverloadScope scope)
    {
        DiameterAdmissionRequest result = new DiameterAdmissionRequest();
        result.Request.Message.Flags = DiameterMessageFlags.Request;
        result.Request.Message.CommandCode = 300U;
        result.Request.Message.ApplicationId = 4U;
        result.Request.ApplicationId = 4U;
        result.Request.DestinationRealm = CreateIdentity("example");
        result.Request.DestinationHost = CreateIdentity("server.example");
        result.Request.ReceivedAt = clock.UtcTime;
        result.Candidate = new DiameterRouteCandidate();
        result.Candidate.PeerIdentity = CreateIdentity(peer);
        result.Scope = scope;
        result.EvaluatedAt = clock.UtcTime;
        return result;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static DiameterAvp CreateAvp(uint code, byte[] data)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Data = data;
        return result;
    }

    private static DiameterAvp CreateUnsigned64Avp(uint code, ulong value)
    {
        DiameterAvp result = CreateAvp(code, new byte[8]);
        NetworkByteOrder.WriteUInt64(result.Data, value);
        return result;
    }

    private static DiameterAvp CreateInteger32Avp(uint code, int value)
    {
        DiameterAvp result = CreateAvp(code, new byte[4]);
        NetworkByteOrder.WriteInt32(result.Data, value);
        return result;
    }

    private static DiameterAvp CreateGroupedAvp(
        uint code,
        List<DiameterAvp> children)
    {
        byte[] data =
            DiameterPeerOverloadProcessor.EncodeDiameterGroupedChildren(
                children);
        return CreateAvp(code, data);
    }

    private static List<DiameterAvp> DecodeChildren(DiameterAvp avp)
    {
        DiameterValueResult decoded =
            DiameterPeerOverloadProcessor.DecodeDiameterGroupedAvp(avp);
        Assert.That(decoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return decoded.Avps;
    }

    private static DiameterAvp FindTopLevelAvp(
        DiameterMessage message,
        uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (DiameterDoicProcessor.IsDiameterIetfAvp(avp, code))
            {
                return avp;
            }

            index = index + 1;
        }

        throw new InvalidOperationException("Expected AVP was not found.");
    }

    private static DiameterAvp FindChild(
        List<DiameterAvp> children,
        uint code)
    {
        int index = 0;
        while (index < children.Count)
        {
            DiameterAvp avp = children[index];
            if (DiameterDoicProcessor.IsDiameterIetfAvp(avp, code))
            {
                return avp;
            }

            index = index + 1;
        }

        throw new InvalidOperationException("Expected child AVP was not found.");
    }

    private static int CountChild(
        List<DiameterAvp> children,
        uint code)
    {
        int result = 0;
        int index = 0;
        while (index < children.Count)
        {
            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                children[index],
                code))
            {
                result = result + 1;
            }

            index = index + 1;
        }

        return result;
    }

    private static int CountSupportedChild(
        DiameterMessage message,
        uint code)
    {
        List<DiameterAvp> children = DecodeChildren(FindTopLevelAvp(
            message,
            DiameterAvpCodes.OcSupportedFeatures));
        int result = 0;
        int index = 0;
        while (index < children.Count)
        {
            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                children[index],
                code))
            {
                result = result + 1;
            }

            index = index + 1;
        }

        return result;
    }

    private static int CountReports(
        DiameterMessage message,
        DiameterOverloadReportType type)
    {
        int result = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                    avp,
                    DiameterAvpCodes.OcOlr) &&
                DiameterPeerOverloadProcessor.ReadDiameterOverloadReportType(
                    avp) == (int)type)
            {
                result = result + 1;
            }

            index = index + 1;
        }

        return result;
    }

    private static ulong ReadFeatureVector(DiameterMessage message)
    {
        return ReadUnsigned64Child(
            DecodeChildren(FindTopLevelAvp(
                message,
                DiameterAvpCodes.OcSupportedFeatures)),
            DiameterAvpCodes.OcFeatureVector);
    }

    private static string ReadSourceIdentity(DiameterMessage message)
    {
        return ReadTextChild(
            DecodeChildren(FindTopLevelAvp(
                message,
                DiameterAvpCodes.OcSupportedFeatures)),
            DiameterAvpCodes.SourceId);
    }

    private static uint ReadPeerReduction(DiameterMessage message)
    {
        return ReadUnsigned32Child(
            ReadPeerReportChildren(message),
            DiameterAvpCodes.OcReductionPercentage);
    }

    private static string ReadPeerSource(DiameterMessage message)
    {
        return ReadTextChild(
            ReadPeerReportChildren(message),
            DiameterAvpCodes.SourceId);
    }

    private static List<DiameterAvp> ReadPeerReportChildren(
        DiameterMessage message)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                    avp,
                    DiameterAvpCodes.OcOlr) &&
                DiameterPeerOverloadProcessor.ReadDiameterOverloadReportType(
                    avp) == (int)DiameterOverloadReportType.Peer)
            {
                return DecodeChildren(avp);
            }

            index = index + 1;
        }

        throw new InvalidOperationException("Peer report was not found.");
    }

    private static ulong ReadUnsigned64Child(
        List<DiameterAvp> children,
        uint code)
    {
        return NetworkByteOrder.ReadUInt64(FindChild(children, code).Data);
    }

    private static uint ReadUnsigned32Child(
        List<DiameterAvp> children,
        uint code)
    {
        return NetworkByteOrder.ReadUInt32(FindChild(children, code).Data);
    }

    private static int ReadInteger32Child(
        List<DiameterAvp> children,
        uint code)
    {
        return NetworkByteOrder.ReadInt32(FindChild(children, code).Data);
    }

    private static string ReadTextChild(
        List<DiameterAvp> children,
        uint code)
    {
        return Encoding.UTF8.GetString(FindChild(children, code).Data);
    }
}
