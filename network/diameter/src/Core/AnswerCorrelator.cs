using System;
using System.Collections.Generic;

namespace diameter;

internal class AnswerResultSemantics
{
    internal bool Valid;
    internal uint ResultCode;
    internal DiameterExperimentalResult? ExperimentalResult;
    internal DiameterResultClass ResultClass;
    internal bool Successful;
    internal bool ProtocolError;
    internal bool ApplicationError;
    internal string Code = string.Empty;
}

internal class AnswerCorrelationResult
{
    internal bool Correlated;
    internal bool Completed;
    internal bool Late;
    internal bool Discarded;
    internal DiameterPendingRequestSnapshot? PendingRequest;
    internal DiameterAnswerResult? AnswerResult;
    internal IDiameterAnswerHandler? Handler;
    internal string Code = string.Empty;
}

internal class AnswerCorrelator
{
    internal PendingRequestTable Table;

    internal AnswerCorrelator(PendingRequestTable table)
    {
        if (table == null)
        {
            throw new ArgumentException("A pending-request table is required.");
        }

        Table = table;
    }

    internal AnswerCorrelationResult CorrelateDiameterAnswer(
        string peerIdentity,
        DiameterMessage answer)
    {
        string headerFailure = ValidateAnswerCorrelationHeader(answer);
        if (headerFailure.Length > 0)
        {
            return CreateAnswerCorrelationFailure(headerFailure);
        }

        DiameterValueResult normalizedPeer =
            NormalizeAnswerCorrelationPeer(peerIdentity);
        if (normalizedPeer.Status != DiameterCodecStatus.Success)
        {
            return CreateAnswerCorrelationFailure("invalid-answer-peer");
        }

        PendingRequestTableResult lookup = Table.ReadPendingRequest(
            normalizedPeer.Text,
            answer.HopByHopIdentifier);
        if (!lookup.Found || lookup.PendingRequest == null)
        {
            return DiagnoseUnknownAnswerCorrelation(
                normalizedPeer.Text,
                answer);
        }

        DiameterPendingRequestSnapshot pending = lookup.PendingRequest;
        string correlationFailure = ValidateAnswerCorrelationValues(
            pending,
            answer);
        if (correlationFailure.Length > 0)
        {
            return CreateAnswerCorrelationFailure(correlationFailure);
        }

        AnswerResultSemantics semantics = ReadAnswerResultSemantics(answer);
        if (!semantics.Valid)
        {
            return CreateAnswerCorrelationFailure(semantics.Code);
        }

        PendingRequestTableResult completion = Table.CompletePendingRequest(
            normalizedPeer.Text,
            answer.HopByHopIdentifier,
            answer);
        AnswerCorrelationResult result = new AnswerCorrelationResult();
        result.Correlated = completion.Found;
        result.Completed = completion.Found && !completion.Late;
        result.Late = completion.Late;
        result.Discarded = !completion.Found;
        result.PendingRequest = completion.PendingRequest;
        result.AnswerResult = completion.AnswerResult;
        result.Handler = completion.Handler;
        if (!completion.Found)
        {
            result.Code = completion.Code;
            return result;
        }

        if (completion.Late)
        {
            result.Code = "late-answer-correlated";
        }
        else
        {
            result.Code = "answer-correlated";
        }

        if (result.AnswerResult != null)
        {
            ApplyAnswerResultSemantics(result.AnswerResult, semantics);
        }

        return result;
    }

    internal string ValidateAnswerCorrelationHeader(DiameterMessage answer)
    {
        if (answer == null)
        {
            return "answer-required";
        }

        if (answer.Avps == null)
        {
            return "invalid-answer-storage";
        }

        int avpIndex = 0;
        while (avpIndex < answer.Avps.Count)
        {
            DiameterAvp avp = answer.Avps[avpIndex];
            if (avp == null || avp.Data == null)
            {
                return "invalid-answer-storage";
            }

            avpIndex = avpIndex + 1;
        }

        if (answer.Version != DiameterProtocolConstants.Version)
        {
            return "unsupported-answer-version";
        }

        if ((answer.Flags & DiameterMessageFlags.Request) !=
            DiameterMessageFlags.None)
        {
            return "answer-request-bit-set";
        }

        if ((answer.Flags & DiameterMessageFlags.PotentiallyRetransmitted) !=
            DiameterMessageFlags.None)
        {
            return "answer-retransmitted-bit-set";
        }

        int flagValue = (int)answer.Flags;
        if (flagValue < 0 || flagValue > 255 ||
            (flagValue & DiameterProtocolConstants.MessageReservedFlagMask) != 0)
        {
            return "invalid-answer-header-bits";
        }

        if (answer.CommandCode == 0 ||
            answer.CommandCode > DiameterProtocolConstants.MaximumCommandCode)
        {
            return "invalid-answer-command-code";
        }

        return string.Empty;
    }

    internal string ValidateAnswerCorrelationValues(
        DiameterPendingRequestSnapshot pending,
        DiameterMessage answer)
    {
        if (answer.CommandCode != pending.OutboundMessage.CommandCode)
        {
            return "answer-command-code-mismatch";
        }

        if (answer.ApplicationId != pending.OutboundMessage.ApplicationId)
        {
            return "answer-application-id-mismatch";
        }

        if (answer.HopByHopIdentifier != pending.HopByHopIdentifier)
        {
            return "answer-hop-by-hop-mismatch";
        }

        if (answer.EndToEndIdentifier != pending.EndToEndIdentifier)
        {
            return "answer-end-to-end-mismatch";
        }

        DiameterMessageFlags requestProxyable =
            pending.OutboundMessage.Flags & DiameterMessageFlags.Proxyable;
        DiameterMessageFlags answerProxyable =
            answer.Flags & DiameterMessageFlags.Proxyable;
        if (requestProxyable != answerProxyable)
        {
            return "answer-proxyable-bit-mismatch";
        }

        return string.Empty;
    }

    internal AnswerCorrelationResult DiagnoseUnknownAnswerCorrelation(
        string normalizedPeerIdentity,
        DiameterMessage answer)
    {
        List<DiameterPendingRequestSnapshot> snapshots =
            Table.ReadPendingRequestSnapshots();
        bool hopMismatch = false;
        bool pathMismatch = false;
        int index = 0;
        while (index < snapshots.Count)
        {
            DiameterPendingRequestSnapshot pending = snapshots[index];
            DiameterValueResult pendingPeer = ValueCodecs.NormalizeDiameterIdentity(
                pending.PeerIdentity);
            bool samePeer = pendingPeer.Status == DiameterCodecStatus.Success &&
                pendingPeer.Text == normalizedPeerIdentity;
            bool sameCommand = pending.OutboundMessage.CommandCode ==
                answer.CommandCode;
            bool sameApplication = pending.OutboundMessage.ApplicationId ==
                answer.ApplicationId;
            bool sameEndToEnd = pending.EndToEndIdentifier ==
                answer.EndToEndIdentifier;
            if (samePeer && sameCommand && sameApplication && sameEndToEnd &&
                pending.HopByHopIdentifier != answer.HopByHopIdentifier)
            {
                hopMismatch = true;
            }

            if (!samePeer && sameCommand && sameApplication && sameEndToEnd &&
                pending.HopByHopIdentifier == answer.HopByHopIdentifier)
            {
                pathMismatch = true;
            }

            index = index + 1;
        }

        if (pathMismatch)
        {
            return CreateAnswerCorrelationFailure("answer-peer-path-mismatch");
        }

        if (hopMismatch)
        {
            return CreateAnswerCorrelationFailure("answer-hop-by-hop-mismatch");
        }

        return CreateAnswerCorrelationFailure("answer-not-pending");
    }

    internal AnswerResultSemantics ReadAnswerResultSemantics(
        DiameterMessage answer)
    {
        AnswerResultSemantics result = new AnswerResultSemantics();
        int standardCount = 0;
        int experimentalCount = 0;
        int index = 0;
        while (index < answer.Avps.Count)
        {
            DiameterAvp avp = answer.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.ResultCode)
            {
                standardCount = standardCount + 1;
                if (avp.Data.Length != 4)
                {
                    result.Code = "invalid-answer-result-code";
                    return result;
                }

                result.ResultCode = NetworkByteOrder.ReadUInt32(avp.Data);
            }
            else if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.ExperimentalResult)
            {
                experimentalCount = experimentalCount + 1;
                result.ExperimentalResult =
                    ReadAnswerExperimentalResult(avp);
                if (result.ExperimentalResult == null)
                {
                    result.Code = "invalid-answer-experimental-result";
                    return result;
                }
            }

            index = index + 1;
        }

        bool error = (answer.Flags & DiameterMessageFlags.Error) !=
            DiameterMessageFlags.None;
        if (standardCount > 1 || experimentalCount > 1 ||
            standardCount + experimentalCount != 1 ||
            (error && standardCount != 1))
        {
            result.Code = "invalid-answer-result-selection";
            return result;
        }

        if (standardCount == 1)
        {
            if (result.ResultCode < 1000U || result.ResultCode >= 6000U)
            {
                result.Code = "invalid-answer-result-class";
                return result;
            }

            result.ResultClass = ClassifyAnswerResultCode(result.ResultCode);
        }
        else if (result.ExperimentalResult != null)
        {
            result.ResultClass = ClassifyAnswerResultCode(
                result.ExperimentalResult.ResultCode);
        }

        if ((result.ResultClass == DiameterResultClass.ProtocolError &&
            !error) ||
            (result.ResultClass == DiameterResultClass.TransientFailure &&
            error) ||
            (error && result.ResultClass != DiameterResultClass.ProtocolError &&
            result.ResultClass != DiameterResultClass.PermanentFailure))
        {
            result.Code = "answer-error-bit-result-mismatch";
            return result;
        }

        result.Successful = result.ResultClass == DiameterResultClass.Success;
        result.ProtocolError = error;
        result.ApplicationError = !error &&
            (result.ResultClass == DiameterResultClass.TransientFailure ||
            result.ResultClass == DiameterResultClass.PermanentFailure);
        result.Valid = true;
        result.Code = "valid-answer-result";
        return result;
    }

    internal DiameterExperimentalResult? ReadAnswerExperimentalResult(
        DiameterAvp avp)
    {
        DiameterValueResult decoded = GroupedValueCodec.DecodeGroupedValue(
            avp.Data,
            null,
            new DiameterLimitConfiguration());
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            return null;
        }

        int vendorCount = 0;
        int resultCount = 0;
        DiameterExperimentalResult result = new DiameterExperimentalResult();
        int index = 0;
        while (index < decoded.Avps.Count)
        {
            DiameterAvp child = decoded.Avps[index];
            if (child.VendorId != DiameterVendorIds.Ietf ||
                child.Data.Length != 4)
            {
                return null;
            }

            if (child.Code == DiameterAvpCodes.VendorId)
            {
                vendorCount = vendorCount + 1;
                result.VendorId = NetworkByteOrder.ReadUInt32(child.Data);
            }
            else if (child.Code == DiameterAvpCodes.ExperimentalResultCode)
            {
                resultCount = resultCount + 1;
                result.ResultCode = NetworkByteOrder.ReadUInt32(child.Data);
            }
            else
            {
                return null;
            }

            index = index + 1;
        }

        if (vendorCount != 1 || resultCount != 1 ||
            result.VendorId == 0U || result.ResultCode == 0U)
        {
            return null;
        }

        return result;
    }

    internal DiameterResultClass ClassifyAnswerResultCode(uint resultCode)
    {
        uint value = resultCode / 1000U;
        if (value == 1U)
        {
            return DiameterResultClass.Informational;
        }

        if (value == 2U)
        {
            return DiameterResultClass.Success;
        }

        if (value == 3U)
        {
            return DiameterResultClass.ProtocolError;
        }

        if (value == 4U)
        {
            return DiameterResultClass.TransientFailure;
        }

        return DiameterResultClass.PermanentFailure;
    }

    internal DiameterValueResult NormalizeAnswerCorrelationPeer(
        string peerIdentity)
    {
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = peerIdentity;
        return ValueCodecs.NormalizeDiameterIdentity(identity);
    }

    internal void ApplyAnswerResultSemantics(
        DiameterAnswerResult answer,
        AnswerResultSemantics semantics)
    {
        answer.ResultCode = semantics.ResultCode;
        if (semantics.ExperimentalResult != null)
        {
            answer.ExperimentalResult = new DiameterExperimentalResult();
            answer.ExperimentalResult.VendorId =
                semantics.ExperimentalResult.VendorId;
            answer.ExperimentalResult.ResultCode =
                semantics.ExperimentalResult.ResultCode;
        }

        answer.ResultClass = semantics.ResultClass;
        answer.Successful = semantics.Successful;
        answer.ProtocolError = semantics.ProtocolError;
        answer.ApplicationError = semantics.ApplicationError;
    }

    internal AnswerCorrelationResult CreateAnswerCorrelationFailure(
        string code)
    {
        AnswerCorrelationResult result = new AnswerCorrelationResult();
        result.Discarded = true;
        result.Code = code;
        return result;
    }
}
