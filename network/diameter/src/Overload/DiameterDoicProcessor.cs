using System;
using System.Collections.Generic;

namespace diameter;

internal class DiameterDoicNegotiationResult
{
    internal bool Accepted;
    internal bool Modified;
    internal bool RequestAdvertised;
    internal bool ReportingNodePresent;
    internal ulong FeatureVector;
    internal DiameterOverloadAlgorithm SelectedAlgorithm =
        DiameterOverloadAlgorithm.None;
    internal DiameterMessage Message = new DiameterMessage();
    internal string Code = string.Empty;
    internal string Description = string.Empty;
}

internal class DiameterDoicFeatureInspection
{
    internal bool Valid;
    internal bool Present;
    internal bool ExplicitVector;
    internal ulong FeatureVector = 1UL;
    internal DiameterOverloadAlgorithm SelectedAlgorithm =
        DiameterOverloadAlgorithm.Loss;
    internal string Code = string.Empty;
    internal string Description = string.Empty;
}

internal static class DiameterDoicProcessor
{
    internal static DiameterDoicNegotiationResult PrepareDiameterOverloadRequest(
        DiameterMessage? message,
        DiameterOverloadConfiguration? configuration,
        DiameterAgentRole agentRole)
    {
        DiameterDoicNegotiationResult result =
            CreateDiameterOverloadNegotiationResult(message);
        if (!result.Accepted || configuration == null)
        {
            if (configuration == null)
            {
                SetDiameterOverloadFailure(
                    result,
                    "doic-configuration-required",
                    "Diameter overload processing requires configuration.");
            }

            return result;
        }

        configuration.ValidateOverloadConfiguration();
        if (!IsDiameterAgentRoleValid(agentRole))
        {
            SetDiameterOverloadFailure(
                result,
                "doic-agent-role-invalid",
                "The Diameter agent role is invalid.");
            return result;
        }

        if ((result.Message.Flags & DiameterMessageFlags.Request) ==
            DiameterMessageFlags.None)
        {
            SetDiameterOverloadFailure(
                result,
                "doic-request-required",
                "DOIC request preparation requires a request message.");
            return result;
        }

        if (!configuration.Enabled)
        {
            result.Code = "doic-disabled-forwarded";
            result.Description =
                "The message was copied without changing overload AVPs.";
            return result;
        }

        DiameterDoicFeatureInspection inspection =
            InspectDiameterOverloadFeatures(result.Message, false);
        if (agentRole != DiameterAgentRole.None && inspection.Present &&
            !inspection.Valid)
        {
            CopyDiameterOverloadInspectionFailure(result, inspection);
            return result;
        }

        bool hadReport = RemoveDiameterOverloadReports(result.Message);
        bool originated = agentRole == DiameterAgentRole.None;
        if (originated || !inspection.Present)
        {
            RemoveDiameterSupportedFeatures(result.Message);
            ulong featureVector =
                CreateDiameterOverloadRequestFeatureVector(configuration);
            result.Message.Avps.Add(
                CreateDiameterSupportedFeaturesAvp(featureVector));
            result.FeatureVector = featureVector;
            result.Modified = true;
        }
        else
        {
            result.FeatureVector = inspection.FeatureVector;
        }

        result.Modified = result.Modified || hadReport;
        result.RequestAdvertised = true;
        result.Code = originated ?
            "doic-request-originated" :
            "doic-request-relayed";
        result.Description = originated ?
            "The local DOIC capabilities were added to the request." :
            "The request carries a DOIC capability announcement.";
        return result;
    }

    internal static DiameterDoicNegotiationResult PrepareDiameterOverloadAnswer(
        DiameterMessage? request,
        DiameterMessage? answer,
        DiameterOverloadConfiguration? configuration,
        DiameterAgentRole agentRole)
    {
        DiameterDoicNegotiationResult result =
            CreateDiameterOverloadNegotiationResult(answer);
        if (!result.Accepted || configuration == null)
        {
            if (configuration == null)
            {
                SetDiameterOverloadFailure(
                    result,
                    "doic-configuration-required",
                    "Diameter overload processing requires configuration.");
            }

            return result;
        }

        DiameterDoicNegotiationResult requestCopy =
            CreateDiameterOverloadNegotiationResult(request);
        if (!requestCopy.Accepted)
        {
            SetDiameterOverloadFailure(
                result,
                requestCopy.Code,
                requestCopy.Description);
            return result;
        }

        configuration.ValidateOverloadConfiguration();
        if (!IsDiameterAgentRoleValid(agentRole))
        {
            SetDiameterOverloadFailure(
                result,
                "doic-agent-role-invalid",
                "The Diameter agent role is invalid.");
            return result;
        }

        if ((requestCopy.Message.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            (result.Message.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None)
        {
            SetDiameterOverloadFailure(
                result,
                "doic-transaction-direction-invalid",
                "DOIC answer preparation requires a request and its answer.");
            return result;
        }

        if (!configuration.Enabled)
        {
            result.Code = "doic-disabled-forwarded";
            result.Description =
                "The message was copied without changing overload AVPs.";
            return result;
        }

        DiameterDoicFeatureInspection requestInspection =
            InspectDiameterOverloadFeatures(requestCopy.Message, false);
        if (!requestInspection.Valid)
        {
            CopyDiameterOverloadInspectionFailure(result, requestInspection);
            return result;
        }

        result.RequestAdvertised = requestInspection.Present;
        if (!requestInspection.Present)
        {
            bool removedFeatures =
                RemoveDiameterSupportedFeatures(result.Message);
            bool removedReports =
                RemoveDiameterOverloadReports(result.Message);
            result.Modified = removedFeatures || removedReports;
            result.Code = "doic-answer-withheld";
            result.Description =
                "Overload AVPs were withheld because the request did not advertise DOIC.";
            return result;
        }

        if (agentRole == DiameterAgentRole.None)
        {
            DiameterOverloadAlgorithm algorithm =
                SelectDiameterOverloadReportingAlgorithm(
                    requestInspection.FeatureVector,
                    configuration);
            ReplaceDiameterSupportedFeatures(
                result.Message,
                (ulong)(int)algorithm);
            result.Modified = true;
            result.ReportingNodePresent = true;
            result.FeatureVector = (ulong)(int)algorithm;
            result.SelectedAlgorithm = algorithm;
            result.Code = "doic-answer-originated";
            result.Description =
                "The answer announces the selected DOIC abatement algorithm.";
            return result;
        }

        DiameterDoicFeatureInspection answerInspection =
            InspectDiameterOverloadFeatures(result.Message, true);
        if (!answerInspection.Valid)
        {
            CopyDiameterOverloadInspectionFailure(result, answerInspection);
            return result;
        }

        if (answerInspection.Present)
        {
            ulong offeredAlgorithms = requestInspection.FeatureVector & 5UL;
            ulong selectedAlgorithm =
                answerInspection.FeatureVector & 5UL;
            if ((selectedAlgorithm & offeredAlgorithms) != selectedAlgorithm)
            {
                SetDiameterOverloadFailure(
                    result,
                    "doic-answer-algorithm-not-offered",
                    "The answer selected an algorithm not offered in the request.");
                return result;
            }

            result.ReportingNodePresent = true;
            result.FeatureVector = answerInspection.FeatureVector;
            result.SelectedAlgorithm = answerInspection.SelectedAlgorithm;
            result.Code = "doic-answer-relayed";
            result.Description =
                "The reporting node's overload AVPs were relayed unchanged.";
            return result;
        }

        DiameterOverloadAlgorithm localAlgorithm =
            SelectDiameterOverloadReportingAlgorithm(
                requestInspection.FeatureVector,
                configuration);
        RemoveDiameterOverloadReports(result.Message);
        result.Message.Avps.Add(
            CreateDiameterSupportedFeaturesAvp(
                (ulong)(int)localAlgorithm));
        result.Modified = true;
        result.ReportingNodePresent = true;
        result.FeatureVector = (ulong)(int)localAlgorithm;
        result.SelectedAlgorithm = localAlgorithm;
        result.Code = "doic-agent-reporting";
        result.Description =
            "The agent supplied reporting-node capability for the transaction.";
        return result;
    }

    internal static DiameterDoicFeatureInspection InspectDiameterOverloadFeatures(
        DiameterMessage message,
        bool answer)
    {
        DiameterDoicFeatureInspection result =
            new DiameterDoicFeatureInspection();
        int foundCount = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (IsDiameterIetfAvp(avp, DiameterAvpCodes.OcSupportedFeatures))
            {
                foundCount = foundCount + 1;
                if (foundCount > 1)
                {
                    SetDiameterOverloadInspectionFailure(
                        result,
                        "doic-supported-features-duplicate",
                        "A Diameter message cannot contain multiple OC-Supported-Features AVPs.");
                    return result;
                }

                DiameterValueResult decoded =
                    GroupedValueCodec.DecodeGroupedValue(
                        avp.Data,
                        null,
                        new DiameterLimitConfiguration());
                if (decoded.Status != DiameterCodecStatus.Success ||
                    decoded.ConsumedCount != avp.Data.Length)
                {
                    SetDiameterOverloadInspectionFailure(
                        result,
                        "doic-supported-features-invalid",
                        "OC-Supported-Features must contain a valid Grouped value.");
                    return result;
                }

                DiameterDoicFeatureInspection vectorInspection =
                    InspectDiameterOverloadFeatureVector(decoded.Avps, answer);
                if (!vectorInspection.Valid)
                {
                    return vectorInspection;
                }

                result.Present = true;
                result.ExplicitVector = vectorInspection.ExplicitVector;
                result.FeatureVector = vectorInspection.FeatureVector;
                result.SelectedAlgorithm =
                    vectorInspection.SelectedAlgorithm;
            }

            index = index + 1;
        }

        result.Valid = true;
        result.Code = result.Present ?
            "doic-supported-features-present" :
            "doic-supported-features-absent";
        result.Description = result.Present ?
            "The Diameter message contains a valid DOIC capability announcement." :
            "The Diameter message does not contain a DOIC capability announcement.";
        return result;
    }

    internal static DiameterDoicFeatureInspection InspectDiameterOverloadFeatureVector(
        List<DiameterAvp> children,
        bool answer)
    {
        DiameterDoicFeatureInspection result =
            new DiameterDoicFeatureInspection();
        int foundCount = 0;
        int index = 0;
        while (index < children.Count)
        {
            DiameterAvp avp = children[index];
            if (IsDiameterIetfAvp(avp, DiameterAvpCodes.OcFeatureVector))
            {
                foundCount = foundCount + 1;
                if (foundCount > 1 || avp.Data.Length != 8)
                {
                    SetDiameterOverloadInspectionFailure(
                        result,
                        "doic-feature-vector-invalid",
                        "OC-Supported-Features can contain one Unsigned64 OC-Feature-Vector.");
                    return result;
                }

                ulong value = NetworkByteOrder.ReadUInt64(avp.Data);
                if (value == 0UL || !answer && (value & 1UL) == 0UL)
                {
                    SetDiameterOverloadInspectionFailure(
                        result,
                        "doic-feature-vector-invalid",
                        "A request feature vector must be nonzero and include the loss algorithm.");
                    return result;
                }

                ulong algorithms = value & 5UL;
                if (answer && algorithms != 1UL && algorithms != 4UL)
                {
                    SetDiameterOverloadInspectionFailure(
                        result,
                        "doic-answer-algorithm-invalid",
                        "An answer feature vector must select exactly one known abatement algorithm.");
                    return result;
                }

                result.ExplicitVector = true;
                result.FeatureVector = value;
                result.SelectedAlgorithm = algorithms == 4UL ?
                    DiameterOverloadAlgorithm.Rate :
                    DiameterOverloadAlgorithm.Loss;
            }

            index = index + 1;
        }

        result.Valid = true;
        result.Code = result.ExplicitVector ?
            "doic-feature-vector-present" :
            "doic-default-algorithm";
        result.Description = result.ExplicitVector ?
            "The OC-Feature-Vector was decoded." :
            "The absent OC-Feature-Vector selects the loss algorithm.";
        return result;
    }

    internal static DiameterDoicNegotiationResult CreateDiameterOverloadNegotiationResult(
        DiameterMessage? message)
    {
        DiameterDoicNegotiationResult result =
            new DiameterDoicNegotiationResult();
        if (message == null || message.Avps == null)
        {
            SetDiameterOverloadFailure(
                result,
                "doic-message-required",
                "Diameter overload processing requires a message with AVP storage.");
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
            DiameterAvp source = message.Avps[index];
            if (source == null || source.Data == null)
            {
                SetDiameterOverloadFailure(
                    result,
                    "doic-avp-invalid",
                    "Diameter overload processing requires owned AVP payload data.");
                return result;
            }

            DiameterAvp clone = new DiameterAvp();
            clone.Code = source.Code;
            clone.Flags = source.Flags;
            clone.VendorId = source.VendorId;
            clone.Data = new byte[source.Data.Length];
            Array.Copy(source.Data, clone.Data, source.Data.Length);
            owned.Avps.Add(clone);
            index = index + 1;
        }

        result.Accepted = true;
        result.Message = owned;
        result.Code = "doic-message-copied";
        result.Description = "The Diameter message was copied for overload processing.";
        return result;
    }

    internal static ulong CreateDiameterOverloadRequestFeatureVector(
        DiameterOverloadConfiguration configuration)
    {
        ulong result = 0UL;
        int index = 0;
        while (index < configuration.SupportedAlgorithms.Count)
        {
            result = result |
                (ulong)(uint)(int)configuration.SupportedAlgorithms[index];
            index = index + 1;
        }

        return result;
    }

    internal static DiameterOverloadAlgorithm SelectDiameterOverloadReportingAlgorithm(
        ulong requestFeatureVector,
        DiameterOverloadConfiguration configuration)
    {
        ulong configured =
            (ulong)(uint)(int)configuration.ReportingAlgorithm;
        if ((requestFeatureVector & configured) == configured)
        {
            return configuration.ReportingAlgorithm;
        }

        return DiameterOverloadAlgorithm.Loss;
    }

    internal static DiameterAvp CreateDiameterSupportedFeaturesAvp(
        ulong featureVector)
    {
        DiameterAvp vector = new DiameterAvp();
        vector.Code = DiameterAvpCodes.OcFeatureVector;
        vector.Data = new byte[8];
        NetworkByteOrder.WriteUInt64(vector.Data, featureVector);
        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(vector);
        DiameterLimitConfiguration limits =
            new DiameterLimitConfiguration();
        DiameterValueResult measurement =
            GroupedValueCodec.MeasureGroupedValue(children, null, limits);
        if (measurement.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterValidationException(
                "OC-Supported-Features could not be measured.");
        }

        byte[] data = new byte[measurement.RequiredCount];
        DiameterValueResult encoding =
            GroupedValueCodec.EncodeGroupedValue(
                data,
                children,
                null,
                limits);
        if (encoding.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterValidationException(
                "OC-Supported-Features could not be encoded.");
        }

        DiameterAvp result = new DiameterAvp();
        result.Code = DiameterAvpCodes.OcSupportedFeatures;
        result.Data = data;
        return result;
    }

    internal static bool RemoveDiameterSupportedFeatures(
        DiameterMessage message)
    {
        bool removed = false;
        int index = message.Avps.Count - 1;
        while (index >= 0)
        {
            if (IsDiameterIetfAvp(
                message.Avps[index],
                DiameterAvpCodes.OcSupportedFeatures))
            {
                message.Avps.RemoveAt(index);
                removed = true;
            }

            index = index - 1;
        }

        return removed;
    }

    internal static bool RemoveDiameterOverloadReports(
        DiameterMessage message)
    {
        bool removed = false;
        int index = message.Avps.Count - 1;
        while (index >= 0)
        {
            if (IsDiameterIetfAvp(
                message.Avps[index],
                DiameterAvpCodes.OcOlr))
            {
                message.Avps.RemoveAt(index);
                removed = true;
            }

            index = index - 1;
        }

        return removed;
    }

    internal static void ReplaceDiameterSupportedFeatures(
        DiameterMessage message,
        ulong featureVector)
    {
        RemoveDiameterSupportedFeatures(message);
        message.Avps.Add(
            CreateDiameterSupportedFeaturesAvp(featureVector));
    }

    internal static bool IsDiameterIetfAvp(DiameterAvp avp, uint code)
    {
        return avp.Code == code &&
            avp.VendorId == DiameterVendorIds.Ietf &&
            (avp.Flags & DiameterAvpFlags.VendorSpecific) ==
                DiameterAvpFlags.None;
    }

    internal static bool IsDiameterAgentRoleValid(DiameterAgentRole role)
    {
        int value = (int)role;
        return value >= (int)DiameterAgentRole.None &&
            value <= (int)DiameterAgentRole.Translation;
    }

    internal static void CopyDiameterOverloadInspectionFailure(
        DiameterDoicNegotiationResult result,
        DiameterDoicFeatureInspection inspection)
    {
        SetDiameterOverloadFailure(
            result,
            inspection.Code,
            inspection.Description);
    }

    internal static void SetDiameterOverloadFailure(
        DiameterDoicNegotiationResult result,
        string code,
        string description)
    {
        result.Accepted = false;
        result.Code = code;
        result.Description = description;
    }

    internal static void SetDiameterOverloadInspectionFailure(
        DiameterDoicFeatureInspection result,
        string code,
        string description)
    {
        result.Valid = false;
        result.Code = code;
        result.Description = description;
    }
}
