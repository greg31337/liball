using System;
using System.Collections.Generic;
using System.Text;

namespace diameter;

internal class DiameterLoadMessageResult
{
    internal bool Accepted;
    internal bool Modified;
    internal DiameterMessage Message = new DiameterMessage();
    internal string Code = string.Empty;
    internal string Description = string.Empty;
}

internal static class DiameterLoadProcessor
{
    internal static DiameterLoadReport CreateDiameterLoadReport(
        DiameterLoadSource source,
        DiameterIdentity? localIdentity,
        ulong loadValue,
        DateTimeOffset createdAt,
        int reportIntervalMilliseconds)
    {
        if (createdAt == default(DateTimeOffset) ||
            reportIntervalMilliseconds <= 0)
        {
            throw new ArgumentException(
                "Load report creation requires time and a positive interval.");
        }

        DiameterIdentity identity = NormalizeDiameterLoadIdentity(
            localIdentity);
        DiameterLoadReport result = new DiameterLoadReport();
        result.Source = source;
        result.SourceId = identity;
        result.LoadValue = loadValue;
        result.CreatedAt = createdAt;
        result.ExpiresAt = createdAt.AddMilliseconds(
            reportIntervalMilliseconds);
        result.ValidateLoadReport();
        return result;
    }

    internal static DiameterAvp CreateDiameterLoadAvp(
        DiameterLoadReport? report)
    {
        if (report == null)
        {
            throw new ArgumentException("A load report is required.");
        }

        report.ValidateLoadReport();
        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateDiameterLoadInteger32Avp(
            DiameterAvpCodes.LoadType,
            (int)report.Source));
        children.Add(CreateDiameterLoadUnsigned64Avp(
            DiameterAvpCodes.LoadValue,
            report.LoadValue));
        children.Add(CreateDiameterLoadIdentityAvp(
            DiameterAvpCodes.SourceId,
            report.SourceId));
        int index = 0;
        while (index < report.ExtensionAvps.Count)
        {
            DiameterAvp extension = report.ExtensionAvps[index];
            if (IsDiameterStandardLoadChild(extension))
            {
                throw new ArgumentException(
                    "Load extensions cannot duplicate standard Load AVPs.");
            }

            children.Add(CloneDiameterLoadAvp(extension));
            index = index + 1;
        }

        DiameterAvp result = new DiameterAvp();
        result.Code = DiameterAvpCodes.Load;
        result.Data = EncodeDiameterLoadChildren(children);
        return result;
    }

    internal static DiameterLoadMessageResult PrepareDiameterLoadAnswer(
        DiameterMessage? answer,
        DiameterOverloadConfiguration? configuration,
        DiameterAgentRole agentRole,
        DiameterIdentity? localIdentity,
        DiameterLoadReport? localReport)
    {
        DiameterLoadMessageResult result =
            CloneDiameterLoadMessage(answer);
        if (!result.Accepted || configuration == null)
        {
            if (configuration == null)
            {
                SetDiameterLoadMessageFailure(
                    result,
                    "load-configuration-required",
                    "Load processing requires configuration.");
            }

            return result;
        }

        configuration.ValidateOverloadConfiguration();
        int role = (int)agentRole;
        if (role < (int)DiameterAgentRole.None ||
            role > (int)DiameterAgentRole.Translation ||
            (result.Message.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None)
        {
            SetDiameterLoadMessageFailure(
                result,
                "load-answer-context-invalid",
                "Load reports require an answer and a valid agent role.");
            return result;
        }

        if (!configuration.EnableLoadReports)
        {
            result.Code = "load-disabled-forwarded";
            result.Description =
                "The answer was copied without changing Load AVPs.";
            return result;
        }

        bool agent = agentRole != DiameterAgentRole.None;
        bool removed = false;
        if (agent)
        {
            removed = RemoveDiameterPeerLoadReports(result.Message);
        }

        if (localReport != null)
        {
            DiameterIdentity identity = NormalizeDiameterLoadIdentity(
                localIdentity);
            if (agent && localReport.Source != DiameterLoadSource.Peer ||
                !agent && localReport.Source != DiameterLoadSource.Host ||
                !ValueCodecs.AreDiameterIdentitiesEqual(
                    localReport.SourceId,
                    identity))
            {
                SetDiameterLoadMessageFailure(
                    result,
                    "load-report-source-invalid",
                    "Local load reports require the local identity and reporting role.");
                return result;
            }

            result.Message.Avps.Add(CreateDiameterLoadAvp(localReport));
            result.Modified = true;
        }

        result.Modified = result.Modified || removed;
        result.Code = agent ?
            "load-agent-answer-prepared" :
            "load-endpoint-answer-prepared";
        result.Description = agent ?
            "Peer load reports were replaced with local agent load information." :
            "Local endpoint load information was added to the answer.";
        return result;
    }

    internal static bool RemoveDiameterPeerLoadReports(
        DiameterMessage message)
    {
        bool removed = false;
        int index = message.Avps.Count - 1;
        while (index >= 0)
        {
            DiameterAvp avp = message.Avps[index];
            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                    avp,
                    DiameterAvpCodes.Load) &&
                ReadDiameterLoadSource(avp) ==
                    (int)DiameterLoadSource.Peer)
            {
                message.Avps.RemoveAt(index);
                removed = true;
            }

            index = index - 1;
        }

        return removed;
    }

    internal static int ReadDiameterLoadSource(DiameterAvp load)
    {
        DiameterValueResult decoded = DecodeDiameterLoadChildren(load);
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            return -1;
        }

        int result = -1;
        int index = 0;
        while (index < decoded.Avps.Count)
        {
            DiameterAvp child = decoded.Avps[index];
            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                child,
                DiameterAvpCodes.LoadType))
            {
                if (result >= 0 || child.Data.Length != 4)
                {
                    return -1;
                }

                result = NetworkByteOrder.ReadInt32(child.Data);
            }

            index = index + 1;
        }

        return result;
    }

    internal static DiameterValueResult DecodeDiameterLoadChildren(
        DiameterAvp load)
    {
        return GroupedValueCodec.DecodeGroupedValue(
            load.Data,
            null,
            new DiameterLimitConfiguration());
    }

    internal static byte[] EncodeDiameterLoadChildren(
        List<DiameterAvp> children)
    {
        DiameterLimitConfiguration limits =
            new DiameterLimitConfiguration();
        DiameterValueResult measurement =
            GroupedValueCodec.MeasureGroupedValue(children, null, limits);
        if (measurement.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterValidationException(
                "A Load Grouped AVP could not be measured.");
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
                "A Load Grouped AVP could not be encoded.");
        }

        return result;
    }

    internal static DiameterAvp CreateDiameterLoadInteger32Avp(
        uint code,
        int value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Data = new byte[4];
        NetworkByteOrder.WriteInt32(result.Data, value);
        return result;
    }

    internal static DiameterAvp CreateDiameterLoadUnsigned64Avp(
        uint code,
        ulong value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Data = new byte[8];
        NetworkByteOrder.WriteUInt64(result.Data, value);
        return result;
    }

    internal static DiameterAvp CreateDiameterLoadIdentityAvp(
        uint code,
        DiameterIdentity identity)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Data = Encoding.UTF8.GetBytes(identity.Value);
        return result;
    }

    internal static bool IsDiameterStandardLoadChild(DiameterAvp avp)
    {
        if (!DiameterDoicProcessor.IsDiameterIetfAvp(avp, avp.Code))
        {
            return false;
        }

        return avp.Code == DiameterAvpCodes.LoadType ||
            avp.Code == DiameterAvpCodes.LoadValue ||
            avp.Code == DiameterAvpCodes.SourceId;
    }

    internal static DiameterIdentity NormalizeDiameterLoadIdentity(
        DiameterIdentity? identity)
    {
        DiameterValueResult normalized =
            ValueCodecs.NormalizeDiameterIdentity(identity);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "Load processing requires a valid Diameter identity.");
        }

        DiameterIdentity result = new DiameterIdentity();
        result.Value = normalized.Text;
        return result;
    }

    internal static DiameterAvp CloneDiameterLoadAvp(DiameterAvp source)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = source.Code;
        result.Flags = source.Flags;
        result.VendorId = source.VendorId;
        result.Data = new byte[source.Data.Length];
        Array.Copy(source.Data, result.Data, source.Data.Length);
        return result;
    }

    internal static DiameterLoadMessageResult CloneDiameterLoadMessage(
        DiameterMessage? message)
    {
        DiameterLoadMessageResult result =
            new DiameterLoadMessageResult();
        if (message == null || message.Avps == null)
        {
            SetDiameterLoadMessageFailure(
                result,
                "load-message-required",
                "Load processing requires a message with AVP storage.");
            return result;
        }

        DiameterMessage owned = new DiameterMessage();
        owned.Version = message.Version;
        owned.Flags = message.Flags;
        owned.CommandCode = message.CommandCode;
        owned.ApplicationId = message.ApplicationId;
        owned.HopByHopIdentifier = message.HopByHopIdentifier;
        owned.EndToEndIdentifier = message.EndToEndIdentifier;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp == null || avp.Data == null)
            {
                SetDiameterLoadMessageFailure(
                    result,
                    "load-avp-invalid",
                    "Load processing requires owned AVP payload data.");
                return result;
            }

            owned.Avps.Add(CloneDiameterLoadAvp(avp));
            index = index + 1;
        }

        result.Accepted = true;
        result.Message = owned;
        result.Code = "load-message-copied";
        result.Description = "The answer was copied for Load processing.";
        return result;
    }

    internal static void SetDiameterLoadMessageFailure(
        DiameterLoadMessageResult result,
        string code,
        string description)
    {
        result.Accepted = false;
        result.Code = code;
        result.Description = description;
    }
}
