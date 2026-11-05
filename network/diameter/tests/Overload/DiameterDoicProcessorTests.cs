using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class DiameterDoicProcessorTests
{
    [Test]
    public void OriginatingRequestAdvertisesAlgorithmsAndRemovesReports()
    {
        DiameterOverloadConfiguration configuration =
            CreateEnabledConfiguration(true);
        DiameterMessage request = CreateRequest();
        DiameterAvp applicationAvp = CreateAvp(900U, new byte[] { 1, 2, 3 });
        request.Avps.Add(applicationAvp);
        request.Avps.Add(
            DiameterDoicProcessor.CreateDiameterSupportedFeaturesAvp(1UL));
        request.Avps.Add(CreateAvp(DiameterAvpCodes.OcOlr, new byte[] { 4 }));

        DiameterDoicNegotiationResult result =
            DiameterDoicProcessor.PrepareDiameterOverloadRequest(
                request,
                configuration,
                DiameterAgentRole.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Modified, Is.True);
            Assert.That(result.RequestAdvertised, Is.True);
            Assert.That(result.FeatureVector, Is.EqualTo(5UL));
            Assert.That(CountIetfAvps(
                result.Message,
                DiameterAvpCodes.OcSupportedFeatures), Is.EqualTo(1));
            Assert.That(CountIetfAvps(
                result.Message,
                DiameterAvpCodes.OcOlr), Is.Zero);
            Assert.That(ReadFeatureVector(result.Message), Is.EqualTo(5UL));
            Assert.That(request.Avps.Count, Is.EqualTo(3));
            Assert.That(result.Message.Avps[0], Is.Not.SameAs(applicationAvp));
            Assert.That(result.Message.Avps[0].Data,
                Is.Not.SameAs(applicationAvp.Data));
        });
    }

    [Test]
    public void RelayPreservesExistingCapabilityAndOpaqueExtensions()
    {
        DiameterOverloadConfiguration configuration =
            CreateEnabledConfiguration(true);
        DiameterMessage request = CreateRequest();
        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateUnsigned64Avp(
            DiameterAvpCodes.OcFeatureVector,
            5UL));
        children.Add(CreateAvp(999U, new byte[] { 7, 8, 9 }));
        DiameterAvp supported = CreateGroupedAvp(
            DiameterAvpCodes.OcSupportedFeatures,
            children);
        request.Avps.Add(supported);
        request.Avps.Add(CreateAvp(DiameterAvpCodes.OcOlr, new byte[] { 1 }));

        DiameterDoicNegotiationResult result =
            DiameterDoicProcessor.PrepareDiameterOverloadRequest(
                request,
                configuration,
                DiameterAgentRole.Relay);
        DiameterAvp relayed = FindIetfAvp(
            result.Message,
            DiameterAvpCodes.OcSupportedFeatures);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Code, Is.EqualTo("doic-request-relayed"));
            Assert.That(result.FeatureVector, Is.EqualTo(5UL));
            Assert.That(relayed.Data, Is.EqualTo(supported.Data));
            Assert.That(relayed.Data, Is.Not.SameAs(supported.Data));
            Assert.That(CountIetfAvps(
                result.Message,
                DiameterAvpCodes.OcOlr), Is.Zero);
        });
    }

    [Test]
    public void AgentBridgesDoicWithoutExposingItToUnsupportedEndpoint()
    {
        DiameterOverloadConfiguration configuration =
            CreateEnabledConfiguration(true);
        DiameterMessage upstreamRequest = CreateRequest();

        DiameterDoicNegotiationResult forwardedRequest =
            DiameterDoicProcessor.PrepareDiameterOverloadRequest(
                upstreamRequest,
                configuration,
                DiameterAgentRole.Relay);
        DiameterMessage downstreamAnswer = CreateAnswer();
        downstreamAnswer.Avps.Add(
            DiameterDoicProcessor.CreateDiameterSupportedFeaturesAvp(4UL));
        downstreamAnswer.Avps.Add(
            CreateAvp(DiameterAvpCodes.OcOlr, new byte[] { 9 }));
        DiameterDoicNegotiationResult forwardedAnswer =
            DiameterDoicProcessor.PrepareDiameterOverloadAnswer(
                upstreamRequest,
                downstreamAnswer,
                configuration,
                DiameterAgentRole.Relay);

        Assert.Multiple(() =>
        {
            Assert.That(forwardedRequest.Accepted, Is.True);
            Assert.That(forwardedRequest.Modified, Is.True);
            Assert.That(ReadFeatureVector(forwardedRequest.Message),
                Is.EqualTo(5UL));
            Assert.That(forwardedAnswer.Accepted, Is.True);
            Assert.That(forwardedAnswer.Code,
                Is.EqualTo("doic-answer-withheld"));
            Assert.That(CountIetfAvps(
                forwardedAnswer.Message,
                DiameterAvpCodes.OcSupportedFeatures), Is.Zero);
            Assert.That(CountIetfAvps(
                forwardedAnswer.Message,
                DiameterAvpCodes.OcOlr), Is.Zero);
        });
    }

    [Test]
    public void ReportingNodeSelectsOneOfferedAlgorithmAndKeepsReport()
    {
        DiameterOverloadConfiguration configuration =
            CreateEnabledConfiguration(true);
        DiameterMessage request = CreateRequest();
        request.Avps.Add(
            DiameterDoicProcessor.CreateDiameterSupportedFeaturesAvp(5UL));
        DiameterMessage answer = CreateAnswer();
        answer.Avps.Add(
            DiameterDoicProcessor.CreateDiameterSupportedFeaturesAvp(1UL));
        DiameterAvp report = CreateAvp(
            DiameterAvpCodes.OcOlr,
            new byte[] { 10, 11 });
        answer.Avps.Add(report);

        DiameterDoicNegotiationResult result =
            DiameterDoicProcessor.PrepareDiameterOverloadAnswer(
                request,
                answer,
                configuration,
                DiameterAgentRole.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.ReportingNodePresent, Is.True);
            Assert.That(result.SelectedAlgorithm,
                Is.EqualTo(DiameterOverloadAlgorithm.Rate));
            Assert.That(ReadFeatureVector(result.Message), Is.EqualTo(4UL));
            Assert.That(CountIetfAvps(
                result.Message,
                DiameterAvpCodes.OcOlr), Is.EqualTo(1));
            Assert.That(FindIetfAvp(
                result.Message,
                DiameterAvpCodes.OcOlr).Data, Is.EqualTo(report.Data));
            Assert.That(ReadFeatureVector(answer), Is.EqualTo(1UL));
        });
    }

    [Test]
    public void AbsentRequestVectorNegotiatesTheDefaultLossAlgorithm()
    {
        DiameterOverloadConfiguration configuration =
            CreateEnabledConfiguration(true);
        DiameterMessage request = CreateRequest();
        request.Avps.Add(CreateGroupedAvp(
            DiameterAvpCodes.OcSupportedFeatures,
            new List<DiameterAvp>()));
        DiameterMessage answer = CreateAnswer();

        DiameterDoicNegotiationResult result =
            DiameterDoicProcessor.PrepareDiameterOverloadAnswer(
                request,
                answer,
                configuration,
                DiameterAgentRole.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.SelectedAlgorithm,
                Is.EqualTo(DiameterOverloadAlgorithm.Loss));
            Assert.That(ReadFeatureVector(result.Message), Is.EqualTo(1UL));
        });
    }

    [Test]
    public void AnswerWithholdsOverloadAvpsFromNonReactingNode()
    {
        DiameterOverloadConfiguration configuration =
            CreateEnabledConfiguration(false);
        DiameterMessage request = CreateRequest();
        DiameterMessage answer = CreateAnswer();
        answer.Avps.Add(
            DiameterDoicProcessor.CreateDiameterSupportedFeaturesAvp(1UL));
        answer.Avps.Add(CreateAvp(DiameterAvpCodes.OcOlr, new byte[] { 1 }));
        DiameterAvp vendorReuse = CreateAvp(
            DiameterAvpCodes.OcSupportedFeatures,
            new byte[] { 2 });
        vendorReuse.Flags = DiameterAvpFlags.VendorSpecific;
        vendorReuse.VendorId = 10415U;
        answer.Avps.Add(vendorReuse);
        answer.Avps.Add(CreateAvp(901U, new byte[] { 3 }));

        DiameterDoicNegotiationResult result =
            DiameterDoicProcessor.PrepareDiameterOverloadAnswer(
                request,
                answer,
                configuration,
                DiameterAgentRole.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Code, Is.EqualTo("doic-answer-withheld"));
            Assert.That(result.ReportingNodePresent, Is.False);
            Assert.That(CountIetfAvps(
                result.Message,
                DiameterAvpCodes.OcSupportedFeatures), Is.Zero);
            Assert.That(CountIetfAvps(
                result.Message,
                DiameterAvpCodes.OcOlr), Is.Zero);
            Assert.That(result.Message.Avps.Count, Is.EqualTo(2));
            Assert.That(result.Message.Avps[0].VendorId, Is.EqualTo(10415U));
            Assert.That(answer.Avps.Count, Is.EqualTo(4));
        });
    }

    [Test]
    public void AgentRelaysNegotiatedAnswerWithoutChangingOverloadAvps()
    {
        DiameterOverloadConfiguration configuration =
            CreateEnabledConfiguration(true);
        DiameterMessage request = CreateRequest();
        request.Avps.Add(
            DiameterDoicProcessor.CreateDiameterSupportedFeaturesAvp(5UL));
        DiameterMessage answer = CreateAnswer();
        DiameterAvp supported =
            DiameterDoicProcessor.CreateDiameterSupportedFeaturesAvp(4UL);
        DiameterAvp report = CreateAvp(
            DiameterAvpCodes.OcOlr,
            new byte[] { 21, 22, 23 });
        answer.Avps.Add(supported);
        answer.Avps.Add(report);

        DiameterDoicNegotiationResult result =
            DiameterDoicProcessor.PrepareDiameterOverloadAnswer(
                request,
                answer,
                configuration,
                DiameterAgentRole.Proxy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Modified, Is.False);
            Assert.That(result.Code, Is.EqualTo("doic-answer-relayed"));
            Assert.That(result.SelectedAlgorithm,
                Is.EqualTo(DiameterOverloadAlgorithm.Rate));
            Assert.That(result.Message.Avps[0].Data, Is.EqualTo(supported.Data));
            Assert.That(result.Message.Avps[1].Data, Is.EqualTo(report.Data));
            Assert.That(result.Message.Avps[0].Data,
                Is.Not.SameAs(supported.Data));
        });
    }

    [Test]
    public void AgentSuppliesReportingCapabilityForNonSupportingEndpoint()
    {
        DiameterOverloadConfiguration configuration =
            CreateEnabledConfiguration(true);
        DiameterMessage request = CreateRequest();
        request.Avps.Add(
            DiameterDoicProcessor.CreateDiameterSupportedFeaturesAvp(5UL));
        DiameterMessage answer = CreateAnswer();
        answer.Avps.Add(CreateAvp(DiameterAvpCodes.OcOlr, new byte[] { 1 }));
        answer.Avps.Add(CreateAvp(902U, new byte[] { 2 }));

        DiameterDoicNegotiationResult result =
            DiameterDoicProcessor.PrepareDiameterOverloadAnswer(
                request,
                answer,
                configuration,
                DiameterAgentRole.Relay);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Modified, Is.True);
            Assert.That(result.Code, Is.EqualTo("doic-agent-reporting"));
            Assert.That(result.ReportingNodePresent, Is.True);
            Assert.That(ReadFeatureVector(result.Message), Is.EqualTo(4UL));
            Assert.That(CountIetfAvps(
                result.Message,
                DiameterAvpCodes.OcOlr), Is.Zero);
            Assert.That(result.Message.Avps[0].Code, Is.EqualTo(902U));
        });
    }

    [Test]
    public void DisabledProcessorForwardsUnknownOverloadAvpsUnchanged()
    {
        DiameterOverloadConfiguration configuration =
            new DiameterOverloadConfiguration();
        DiameterMessage request = CreateRequest();
        request.Avps.Add(CreateAvp(DiameterAvpCodes.OcOlr, new byte[] { 1 }));

        DiameterDoicNegotiationResult result =
            DiameterDoicProcessor.PrepareDiameterOverloadRequest(
                request,
                configuration,
                DiameterAgentRole.Relay);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Modified, Is.False);
            Assert.That(result.Code, Is.EqualTo("doic-disabled-forwarded"));
            Assert.That(result.Message.Avps.Count, Is.EqualTo(1));
            Assert.That(result.Message.Avps[0].Code,
                Is.EqualTo(DiameterAvpCodes.OcOlr));
            Assert.That(result.Message.Avps[0].Data,
                Is.Not.SameAs(request.Avps[0].Data));
        });
    }

    [Test]
    public void InvalidFeatureNegotiationIsRejected()
    {
        DiameterOverloadConfiguration configuration =
            CreateEnabledConfiguration(true);
        DiameterMessage invalidRequest = CreateRequest();
        invalidRequest.Avps.Add(
            DiameterDoicProcessor.CreateDiameterSupportedFeaturesAvp(4UL));
        DiameterDoicNegotiationResult invalidOffer =
            DiameterDoicProcessor.PrepareDiameterOverloadAnswer(
                invalidRequest,
                CreateAnswer(),
                configuration,
                DiameterAgentRole.None);

        DiameterMessage lossRequest = CreateRequest();
        lossRequest.Avps.Add(
            DiameterDoicProcessor.CreateDiameterSupportedFeaturesAvp(1UL));
        DiameterMessage rateAnswer = CreateAnswer();
        rateAnswer.Avps.Add(
            DiameterDoicProcessor.CreateDiameterSupportedFeaturesAvp(4UL));
        DiameterDoicNegotiationResult unofferedSelection =
            DiameterDoicProcessor.PrepareDiameterOverloadAnswer(
                lossRequest,
                rateAnswer,
                configuration,
                DiameterAgentRole.Relay);

        Assert.Multiple(() =>
        {
            Assert.That(invalidOffer.Accepted, Is.False);
            Assert.That(invalidOffer.Code,
                Is.EqualTo("doic-feature-vector-invalid"));
            Assert.That(unofferedSelection.Accepted, Is.False);
            Assert.That(unofferedSelection.Code,
                Is.EqualTo("doic-answer-algorithm-not-offered"));
        });
    }

    private static DiameterOverloadConfiguration CreateEnabledConfiguration(
        bool rate)
    {
        DiameterOverloadConfiguration result =
            new DiameterOverloadConfiguration();
        result.Enabled = true;
        if (rate)
        {
            result.SupportedAlgorithms.Add(
                DiameterOverloadAlgorithm.Rate);
            result.ReportingAlgorithm =
                DiameterOverloadAlgorithm.Rate;
        }

        return result;
    }

    private static DiameterMessage CreateRequest()
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.CommandCode = 300U;
        result.ApplicationId = 4U;
        result.HopByHopIdentifier = 11U;
        result.EndToEndIdentifier = 12U;
        return result;
    }

    private static DiameterMessage CreateAnswer()
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Proxyable;
        result.CommandCode = 300U;
        result.ApplicationId = 4U;
        result.HopByHopIdentifier = 11U;
        result.EndToEndIdentifier = 12U;
        return result;
    }

    private static DiameterAvp CreateAvp(uint code, byte[] data)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = data;
        return result;
    }

    private static DiameterAvp CreateUnsigned64Avp(uint code, ulong value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Data = new byte[8];
        NetworkByteOrder.WriteUInt64(result.Data, value);
        return result;
    }

    private static DiameterAvp CreateGroupedAvp(
        uint code,
        List<DiameterAvp> children)
    {
        DiameterLimitConfiguration limits =
            new DiameterLimitConfiguration();
        DiameterValueResult measurement =
            GroupedValueCodec.MeasureGroupedValue(children, null, limits);
        byte[] data = new byte[measurement.RequiredCount];
        DiameterValueResult encoding =
            GroupedValueCodec.EncodeGroupedValue(
                data,
                children,
                null,
                limits);
        Assert.That(encoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Data = data;
        return result;
    }

    private static int CountIetfAvps(DiameterMessage message, uint code)
    {
        int result = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                message.Avps[index],
                code))
            {
                result = result + 1;
            }

            index = index + 1;
        }

        return result;
    }

    private static DiameterAvp FindIetfAvp(
        DiameterMessage message,
        uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                message.Avps[index],
                code))
            {
                return message.Avps[index];
            }

            index = index + 1;
        }

        throw new InvalidOperationException("The expected AVP was not found.");
    }

    private static ulong ReadFeatureVector(DiameterMessage message)
    {
        DiameterAvp supported = FindIetfAvp(
            message,
            DiameterAvpCodes.OcSupportedFeatures);
        DiameterValueResult decoded =
            GroupedValueCodec.DecodeGroupedValue(
                supported.Data,
                null,
                new DiameterLimitConfiguration());
        int index = 0;
        while (index < decoded.Avps.Count)
        {
            DiameterAvp avp = decoded.Avps[index];
            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                avp,
                DiameterAvpCodes.OcFeatureVector))
            {
                return NetworkByteOrder.ReadUInt64(avp.Data);
            }

            index = index + 1;
        }

        return 1UL;
    }
}
