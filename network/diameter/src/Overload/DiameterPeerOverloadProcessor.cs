using System;
using System.Collections.Generic;
using System.Text;

namespace diameter;

internal class DiameterPeerFeatureInspection
{
    internal bool Valid;
    internal bool Supported;
    internal ulong FeatureVector;
    internal DiameterIdentity? SourceId;
    internal DiameterOverloadAlgorithm PeerAlgorithm =
        DiameterOverloadAlgorithm.None;
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal class DiameterPeerOverloadMessageResult
{
    internal bool Accepted;
    internal bool Modified;
    internal bool PeerSupported;
    internal DiameterOverloadAlgorithm PeerAlgorithm =
        DiameterOverloadAlgorithm.None;
    internal DiameterMessage Message = new DiameterMessage();
    internal string Code = string.Empty;
    internal string Description = string.Empty;
}

internal static class DiameterPeerOverloadProcessor
{
    internal const ulong PeerReportFeature = 16UL;

    internal static DiameterPeerOverloadMessageResult PrepareDiameterPeerRequest(
        DiameterMessage? message,
        DiameterOverloadConfiguration? configuration,
        DiameterAgentRole agentRole,
        DiameterIdentity? localIdentity)
    {
        DiameterDoicNegotiationResult prepared =
            DiameterDoicProcessor.PrepareDiameterOverloadRequest(
                message,
                configuration,
                agentRole);
        DiameterPeerOverloadMessageResult result =
            CopyDiameterPeerMessageResult(prepared);
        if (!prepared.Accepted || configuration == null ||
            !configuration.Enabled || !configuration.EnablePeerReports)
        {
            return result;
        }

        DiameterIdentity identity =
            NormalizeDiameterPeerIdentity(localIdentity);
        RewriteDiameterPeerSupportedFeatures(
            result.Message,
            prepared.FeatureVector | PeerReportFeature,
            identity,
            false,
            configuration.ReportingAlgorithm);
        result.Modified = true;
        result.PeerSupported = true;
        result.PeerAlgorithm = configuration.ReportingAlgorithm;
        result.Code = "peer-overload-request-prepared";
        result.Description =
            "The request advertises peer-report support for the adjacent node.";
        return result;
    }

    internal static DiameterPeerOverloadMessageResult PrepareDiameterPeerAnswer(
        DiameterMessage? request,
        DiameterMessage? answer,
        DiameterOverloadConfiguration? configuration,
        DiameterAgentRole agentRole,
        DiameterIdentity? localIdentity,
        DiameterIdentity? upstreamPeer,
        DiameterOverloadReport? localPeerReport)
    {
        DiameterDoicNegotiationResult prepared =
            DiameterDoicProcessor.PrepareDiameterOverloadAnswer(
                request,
                answer,
                configuration,
                agentRole);
        DiameterPeerOverloadMessageResult result =
            CopyDiameterPeerMessageResult(prepared);
        if (!prepared.Accepted || configuration == null ||
            !configuration.Enabled || !configuration.EnablePeerReports ||
            request == null)
        {
            return result;
        }

        DiameterIdentity identity =
            NormalizeDiameterPeerIdentity(localIdentity);
        DiameterIdentity peer =
            NormalizeDiameterPeerIdentity(upstreamPeer);
        DiameterPeerFeatureInspection requestFeatures =
            InspectDiameterPeerFeatures(request, peer, false);
        if (!requestFeatures.Valid)
        {
            SetDiameterPeerMessageFailure(
                result,
                requestFeatures.Code,
                requestFeatures.Message);
            return result;
        }

        bool removedReport = false;
        if (agentRole != DiameterAgentRole.None ||
            !requestFeatures.Supported)
        {
            removedReport = RemoveDiameterPeerReports(result.Message);
        }

        if (!requestFeatures.Supported)
        {
            bool changedFeatures = RewriteDiameterPeerSupportedFeatures(
                result.Message,
                prepared.FeatureVector & ~PeerReportFeature,
                identity,
                false,
                configuration.ReportingAlgorithm);
            result.Modified = result.Modified || removedReport ||
                changedFeatures;
            result.Code = "peer-overload-answer-withheld";
            result.Description =
                "Peer-report information was withheld from a non-supporting peer.";
            return result;
        }

        RewriteDiameterPeerSupportedFeatures(
            result.Message,
            prepared.FeatureVector | PeerReportFeature,
            identity,
            true,
            configuration.ReportingAlgorithm);
        if (localPeerReport != null)
        {
            if (localPeerReport.SourceId == null ||
                !ValueCodecs.AreDiameterIdentitiesEqual(
                    localPeerReport.SourceId,
                    identity) ||
                localPeerReport.ApplicationId !=
                    result.Message.ApplicationId ||
                localPeerReport.Algorithm !=
                    configuration.ReportingAlgorithm)
            {
                throw new ArgumentException(
                    "A local peer report must match the local identity, application, and selected algorithm.");
            }

            result.Message.Avps.Add(
                CreateDiameterPeerReportAvp(localPeerReport));
        }

        result.Modified = true;
        result.PeerSupported = true;
        result.PeerAlgorithm = configuration.ReportingAlgorithm;
        result.Code = "peer-overload-answer-prepared";
        result.Description = localPeerReport == null ?
            "The answer advertises peer-report support." :
            "The answer advertises peer-report support and carries local peer overload state.";
        return result;
    }

    internal static DiameterPeerFeatureInspection InspectDiameterPeerFeatures(
        DiameterMessage message,
        DiameterIdentity expectedPeer,
        bool answer)
    {
        DiameterPeerFeatureInspection result =
            new DiameterPeerFeatureInspection();
        DiameterDoicFeatureInspection baseInspection =
            DiameterDoicProcessor.InspectDiameterOverloadFeatures(
                message,
                answer);
        if (!baseInspection.Valid)
        {
            SetDiameterPeerInspectionFailure(
                result,
                baseInspection.Code,
                baseInspection.Description);
            return result;
        }

        result.FeatureVector = baseInspection.FeatureVector;
        if (!baseInspection.Present ||
            (baseInspection.FeatureVector & PeerReportFeature) == 0UL)
        {
            result.Valid = true;
            result.Code = "peer-overload-feature-absent";
            result.Message = "The adjacent node did not advertise peer reports.";
            return result;
        }

        DiameterAvp? supported =
            FindDiameterSupportedFeatures(message);
        if (supported == null)
        {
            SetDiameterPeerInspectionFailure(
                result,
                "peer-overload-features-missing",
                "Peer-report support requires OC-Supported-Features.");
            return result;
        }

        DiameterValueResult decoded = DecodeDiameterGroupedAvp(supported);
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            SetDiameterPeerInspectionFailure(
                result,
                "peer-overload-features-invalid",
                "Peer-report capability data is not a valid Grouped value.");
            return result;
        }

        int sourceCount = 0;
        int algorithmCount = 0;
        int index = 0;
        while (index < decoded.Avps.Count)
        {
            DiameterAvp child = decoded.Avps[index];
            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                child,
                DiameterAvpCodes.SourceId))
            {
                sourceCount = sourceCount + 1;
                DiameterValueResult identity =
                    ValueCodecs.DecodeDiameterIdentity(child.Data);
                if (sourceCount > 1 ||
                    identity.Status != DiameterCodecStatus.Success ||
                    identity.Identity == null)
                {
                    SetDiameterPeerInspectionFailure(
                        result,
                        "peer-overload-source-invalid",
                        "Peer-report capability requires one valid SourceID.");
                    return result;
                }

                result.SourceId = identity.Identity;
            }

            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                child,
                DiameterAvpCodes.OcPeerAlgorithm))
            {
                algorithmCount = algorithmCount + 1;
                if (algorithmCount > 1 || child.Data.Length != 8)
                {
                    SetDiameterPeerInspectionFailure(
                        result,
                        "peer-overload-algorithm-invalid",
                        "Peer-report capability can contain one OC-Peer-Algo value.");
                    return result;
                }

                ulong algorithms = NetworkByteOrder.ReadUInt64(child.Data) & 5UL;
                if (algorithms != 1UL && algorithms != 4UL)
                {
                    SetDiameterPeerInspectionFailure(
                        result,
                        "peer-overload-algorithm-invalid",
                        "OC-Peer-Algo must select one known abatement algorithm.");
                    return result;
                }

                result.PeerAlgorithm = algorithms == 4UL ?
                    DiameterOverloadAlgorithm.Rate :
                    DiameterOverloadAlgorithm.Loss;
            }

            index = index + 1;
        }

        if (result.SourceId == null ||
            !ValueCodecs.AreDiameterIdentitiesEqual(
                result.SourceId,
                expectedPeer))
        {
            result.Valid = true;
            result.Code = "peer-overload-not-adjacent";
            result.Message =
                "The peer-report advertisement did not identify the adjacent peer.";
            return result;
        }

        if (answer && result.PeerAlgorithm ==
            DiameterOverloadAlgorithm.None)
        {
            SetDiameterPeerInspectionFailure(
                result,
                "peer-overload-algorithm-required",
                "A peer-report answer requires OC-Peer-Algo.");
            return result;
        }

        result.Valid = true;
        result.Supported = true;
        result.Code = "peer-overload-supported";
        result.Message = "The adjacent peer supports peer overload reports.";
        return result;
    }

    internal static bool RewriteDiameterPeerSupportedFeatures(
        DiameterMessage message,
        ulong featureVector,
        DiameterIdentity localIdentity,
        bool includePeerAlgorithm,
        DiameterOverloadAlgorithm peerAlgorithm)
    {
        DiameterAvp? supported =
            FindDiameterSupportedFeatures(message);
        if (supported == null)
        {
            return false;
        }

        DiameterValueResult decoded = DecodeDiameterGroupedAvp(supported);
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterValidationException(
                "OC-Supported-Features is not a valid Grouped value.");
        }

        List<DiameterAvp> children = new List<DiameterAvp>();
        int index = 0;
        while (index < decoded.Avps.Count)
        {
            DiameterAvp child = decoded.Avps[index];
            if (!DiameterDoicProcessor.IsDiameterIetfAvp(
                    child,
                    DiameterAvpCodes.OcFeatureVector) &&
                !DiameterDoicProcessor.IsDiameterIetfAvp(
                    child,
                    DiameterAvpCodes.SourceId) &&
                !DiameterDoicProcessor.IsDiameterIetfAvp(
                    child,
                    DiameterAvpCodes.OcPeerAlgorithm))
            {
                children.Add(CloneDiameterPeerAvp(child));
            }

            index = index + 1;
        }

        children.Add(CreateDiameterPeerUnsigned64Avp(
            DiameterAvpCodes.OcFeatureVector,
            featureVector));
        if ((featureVector & PeerReportFeature) != 0UL)
        {
            children.Add(CreateDiameterPeerIdentityAvp(
                DiameterAvpCodes.SourceId,
                localIdentity));
            if (includePeerAlgorithm)
            {
                children.Add(CreateDiameterPeerUnsigned64Avp(
                    DiameterAvpCodes.OcPeerAlgorithm,
                    (ulong)(uint)(int)peerAlgorithm));
            }
        }

        supported.Data = EncodeDiameterGroupedChildren(children);
        return true;
    }

    internal static bool RemoveDiameterPeerReports(DiameterMessage message)
    {
        bool removed = false;
        int index = message.Avps.Count - 1;
        while (index >= 0)
        {
            DiameterAvp avp = message.Avps[index];
            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                    avp,
                    DiameterAvpCodes.OcOlr) &&
                ReadDiameterOverloadReportType(avp) ==
                    (int)DiameterOverloadReportType.Peer)
            {
                message.Avps.RemoveAt(index);
                removed = true;
            }

            index = index - 1;
        }

        return removed;
    }

    internal static int ReadDiameterOverloadReportType(DiameterAvp report)
    {
        DiameterValueResult decoded = DecodeDiameterGroupedAvp(report);
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            return -1;
        }

        int found = -1;
        int index = 0;
        while (index < decoded.Avps.Count)
        {
            DiameterAvp child = decoded.Avps[index];
            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                child,
                DiameterAvpCodes.OcReportType))
            {
                if (found >= 0 || child.Data.Length != 4)
                {
                    return -1;
                }

                found = NetworkByteOrder.ReadInt32(child.Data);
            }

            index = index + 1;
        }

        return found;
    }

    internal static DiameterAvp CreateDiameterPeerReportAvp(
        DiameterOverloadReport report)
    {
        report.ValidateOverloadReport();
        if (report.ReportType != DiameterOverloadReportType.Peer ||
            report.SourceId == null)
        {
            throw new ArgumentException("A peer overload report is required.");
        }

        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateDiameterPeerUnsigned64Avp(
            DiameterAvpCodes.OcSequenceNumber,
            report.SequenceNumber));
        children.Add(CreateDiameterPeerInteger32Avp(
            DiameterAvpCodes.OcReportType,
            (int)DiameterOverloadReportType.Peer));
        if (report.Algorithm == DiameterOverloadAlgorithm.Loss)
        {
            children.Add(CreateDiameterPeerUnsigned32Avp(
                DiameterAvpCodes.OcReductionPercentage,
                report.ReductionPercentage));
        }
        else
        {
            children.Add(CreateDiameterPeerUnsigned32Avp(
                DiameterAvpCodes.OcMaximumRate,
                report.MaximumRate));
        }

        children.Add(CreateDiameterPeerUnsigned32Avp(
            DiameterAvpCodes.OcValidityDuration,
            report.ValidityDurationSeconds));
        children.Add(CreateDiameterPeerIdentityAvp(
            DiameterAvpCodes.SourceId,
            report.SourceId));
        int index = 0;
        while (index < report.ExtensionAvps.Count)
        {
            DiameterAvp extension = report.ExtensionAvps[index];
            if (IsDiameterPeerReportChild(extension))
            {
                throw new ArgumentException(
                    "Peer report extensions cannot duplicate standard report AVPs.");
            }

            children.Add(CloneDiameterPeerAvp(extension));
            index = index + 1;
        }

        DiameterAvp result = new DiameterAvp();
        result.Code = DiameterAvpCodes.OcOlr;
        result.Data = EncodeDiameterGroupedChildren(children);
        return result;
    }

    internal static DiameterOverloadReport AggregateDiameterPeerLossReports(
        List<DiameterOverloadReport>? reports,
        DiameterIdentity? localIdentity,
        uint applicationId,
        ulong sequenceNumber,
        uint validityDurationSeconds)
    {
        if (reports == null || reports.Count == 0)
        {
            throw new ArgumentException(
                "Peer overload aggregation requires reports.");
        }

        DiameterIdentity identity =
            NormalizeDiameterPeerIdentity(localIdentity);
        uint combined = 0U;
        int index = 0;
        while (index < reports.Count)
        {
            DiameterOverloadReport report = reports[index];
            if (report == null ||
                report.ReportType != DiameterOverloadReportType.Peer ||
                report.Algorithm != DiameterOverloadAlgorithm.Loss ||
                report.ApplicationId != applicationId)
            {
                throw new ArgumentException(
                    "Only matching loss peer reports can be aggregated.");
            }

            report.ValidateOverloadReport();
            uint overlap = combined * report.ReductionPercentage / 100U;
            combined = combined + report.ReductionPercentage - overlap;
            if (combined > 100U)
            {
                combined = 100U;
            }

            index = index + 1;
        }

        DiameterOverloadReport result = new DiameterOverloadReport();
        result.ReportType = DiameterOverloadReportType.Peer;
        result.Algorithm = DiameterOverloadAlgorithm.Loss;
        result.SourceId = identity;
        result.ApplicationId = applicationId;
        result.SequenceNumber = sequenceNumber;
        result.ValidityDurationSeconds = validityDurationSeconds;
        result.ReductionPercentage = combined;
        result.ReductionPercentagePresent = true;
        result.ValidateOverloadReport();
        return result;
    }

    internal static DiameterValueResult DecodeDiameterGroupedAvp(
        DiameterAvp avp)
    {
        return GroupedValueCodec.DecodeGroupedValue(
            avp.Data,
            null,
            new DiameterLimitConfiguration());
    }

    internal static byte[] EncodeDiameterGroupedChildren(
        List<DiameterAvp> children)
    {
        DiameterLimitConfiguration limits =
            new DiameterLimitConfiguration();
        DiameterValueResult measurement =
            GroupedValueCodec.MeasureGroupedValue(children, null, limits);
        if (measurement.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterValidationException(
                "A peer overload Grouped AVP could not be measured.");
        }

        byte[] result = new byte[measurement.RequiredCount];
        DiameterValueResult encoding =
            GroupedValueCodec.EncodeGroupedValue(
                result,
                children,
                null,
                limits);
        if (encoding.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterValidationException(
                "A peer overload Grouped AVP could not be encoded.");
        }

        return result;
    }

    internal static DiameterAvp? FindDiameterSupportedFeatures(
        DiameterMessage message)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                message.Avps[index],
                DiameterAvpCodes.OcSupportedFeatures))
            {
                return message.Avps[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static DiameterAvp CreateDiameterPeerUnsigned64Avp(
        uint code,
        ulong value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Data = new byte[8];
        NetworkByteOrder.WriteUInt64(result.Data, value);
        return result;
    }

    internal static DiameterAvp CreateDiameterPeerUnsigned32Avp(
        uint code,
        uint value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(result.Data, value);
        return result;
    }

    internal static DiameterAvp CreateDiameterPeerInteger32Avp(
        uint code,
        int value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Data = new byte[4];
        NetworkByteOrder.WriteInt32(result.Data, value);
        return result;
    }

    internal static DiameterAvp CreateDiameterPeerIdentityAvp(
        uint code,
        DiameterIdentity identity)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Data = Encoding.UTF8.GetBytes(identity.Value);
        return result;
    }

    internal static DiameterAvp CloneDiameterPeerAvp(DiameterAvp source)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = source.Code;
        result.Flags = source.Flags;
        result.VendorId = source.VendorId;
        result.Data = new byte[source.Data.Length];
        Array.Copy(source.Data, result.Data, source.Data.Length);
        return result;
    }

    internal static bool IsDiameterPeerReportChild(DiameterAvp avp)
    {
        if (!DiameterDoicProcessor.IsDiameterIetfAvp(avp, avp.Code))
        {
            return false;
        }

        return avp.Code == DiameterAvpCodes.OcSequenceNumber ||
            avp.Code == DiameterAvpCodes.OcReportType ||
            avp.Code == DiameterAvpCodes.OcReductionPercentage ||
            avp.Code == DiameterAvpCodes.OcValidityDuration ||
            avp.Code == DiameterAvpCodes.OcMaximumRate ||
            avp.Code == DiameterAvpCodes.SourceId;
    }

    internal static DiameterIdentity NormalizeDiameterPeerIdentity(
        DiameterIdentity? identity)
    {
        DiameterValueResult normalized =
            ValueCodecs.NormalizeDiameterIdentity(identity);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "Peer overload processing requires a valid Diameter identity.");
        }

        DiameterIdentity result = new DiameterIdentity();
        result.Value = normalized.Text;
        return result;
    }

    internal static DiameterPeerOverloadMessageResult CopyDiameterPeerMessageResult(
        DiameterDoicNegotiationResult source)
    {
        DiameterPeerOverloadMessageResult result =
            new DiameterPeerOverloadMessageResult();
        result.Accepted = source.Accepted;
        result.Modified = source.Modified;
        result.Message = source.Message;
        result.Code = source.Code;
        result.Description = source.Description;
        return result;
    }

    internal static void SetDiameterPeerMessageFailure(
        DiameterPeerOverloadMessageResult result,
        string code,
        string description)
    {
        result.Accepted = false;
        result.Code = code;
        result.Description = description;
    }

    internal static void SetDiameterPeerInspectionFailure(
        DiameterPeerFeatureInspection result,
        string code,
        string message)
    {
        result.Valid = false;
        result.Code = code;
        result.Message = message;
    }
}
