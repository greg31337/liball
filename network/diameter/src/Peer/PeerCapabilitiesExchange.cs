using System;
using System.Collections.Generic;

namespace diameter;

internal class PeerCapabilitiesExchangeResult
{
    internal DiameterValidationResult Validation = new DiameterValidationResult();
    internal DiameterPeerCapabilities Capabilities = new DiameterPeerCapabilities();
    internal uint ResultCode;
    internal bool Accepted;
}

internal class PeerCapabilitiesExchange
{
    internal DiameterBaseCommandFactory BaseCommands;
    internal DiameterValidator Validator = new DiameterValidator();
    internal DiameterDictionarySnapshot DictionarySnapshot;
    internal DiameterLimitConfiguration Limits;
    internal IDiameterClock Clock;

    internal PeerCapabilitiesExchange(
        IDiameterIdentifierSource identifierSource,
        IDiameterClock clock,
        DiameterLimitConfiguration limits)
    {
        if (identifierSource == null || clock == null || limits == null)
        {
            throw new ArgumentException(
                "Capabilities exchange requires identifiers, a clock, and limits.");
        }

        limits.ValidateConfiguration();
        BaseCommands = new DiameterBaseCommandFactory(identifierSource);
        Clock = clock;
        Limits = CloneCapabilitiesLimits(limits);
        DictionarySnapshot = new DiameterDictionary().ReadDictionarySnapshot();
    }

    internal DiameterMessage CreateCapabilitiesExchangeRequest(
        DiameterPeerCapabilities capabilities)
    {
        ValidateLocalPeerCapabilities(capabilities);
        DiameterBaseCommandOptions options = CreateCapabilitiesCommandOptions(capabilities);
        return BaseCommands.CreateCapabilitiesExchangeRequest(options);
    }

    internal DiameterBaseCommandOptions CreateCapabilitiesCommandOptions(
        DiameterPeerCapabilities capabilities)
    {
        DiameterBaseCommandOptions options = new DiameterBaseCommandOptions();
        options.OriginHost.Value = capabilities.OriginHost.Value;
        options.OriginRealm.Value = capabilities.OriginRealm.Value;
        int addressIndex = 0;
        while (addressIndex < capabilities.HostAddresses.Count)
        {
            options.HostAddresses.Add(capabilities.HostAddresses[addressIndex]);
            addressIndex = addressIndex + 1;
        }

        options.VendorId = capabilities.VendorId;
        options.ProductName = capabilities.ProductName;
        options.FirmwareRevision = capabilities.FirmwareRevision;
        int vendorIndex = 0;
        while (vendorIndex < capabilities.SupportedVendorIds.Count)
        {
            options.SupportedVendorIds.Add(capabilities.SupportedVendorIds[vendorIndex]);
            vendorIndex = vendorIndex + 1;
        }

        int authIndex = 0;
        while (authIndex < capabilities.AuthApplicationIds.Count)
        {
            options.AuthApplicationIds.Add(capabilities.AuthApplicationIds[authIndex]);
            authIndex = authIndex + 1;
        }

        int accountingIndex = 0;
        while (accountingIndex < capabilities.AcctApplicationIds.Count)
        {
            options.AcctApplicationIds.Add(capabilities.AcctApplicationIds[accountingIndex]);
            accountingIndex = accountingIndex + 1;
        }

        int applicationIndex = 0;
        while (applicationIndex < capabilities.VendorSpecificApplications.Count)
        {
            options.VendorSpecificApplications.Add(
                PeerTableCloner.ClonePeerTableApplication(
                    capabilities.VendorSpecificApplications[applicationIndex]));
            applicationIndex = applicationIndex + 1;
        }

        int securityIndex = 0;
        while (securityIndex < capabilities.InbandSecurityIds.Count)
        {
            options.InbandSecurityIds.Add(capabilities.InbandSecurityIds[securityIndex]);
            securityIndex = securityIndex + 1;
        }

        return options;
    }

    internal DiameterMessage CreateCapabilitiesExchangeAnswer(
        DiameterMessage? request,
        DiameterPeerCapabilities localCapabilities)
    {
        ValidateLocalPeerCapabilities(localCapabilities);
        PeerCapabilitiesExchangeResult remote =
            ParseCapabilitiesExchangeRequest(request);
        if (remote.Validation.Status != DiameterValidationStatus.Valid)
        {
            throw new DiameterPeerException(
                "A valid Capabilities-Exchange request is required to create an answer.");
        }

        List<DiameterApplicationId> common = ReadCommonPeerApplications(
            localCapabilities,
            remote.Capabilities);
        uint resultCode = DiameterResultCodes.Success;
        if (common.Count == 0)
        {
            resultCode = DiameterResultCodes.NoCommonApplication;
        }
        else if (!HasCommonPeerSecurity(
            localCapabilities,
            remote.Capabilities))
        {
            resultCode = DiameterResultCodes.NoCommonSecurity;
        }

        DiameterBaseCommandOptions options =
            CreateCapabilitiesCommandOptions(localCapabilities);
        options.Request = request;
        options.ResultCode = resultCode;
        return BaseCommands.CreateCapabilitiesExchangeAnswer(options);
    }

    internal PeerCapabilitiesExchangeResult ProcessCapabilitiesExchangeAnswer(
        DiameterMessage? answer,
        DiameterMessage? request,
        DiameterPeerCapabilities localCapabilities)
    {
        ValidateLocalPeerCapabilities(localCapabilities);
        if (answer != null && answer.Version != DiameterProtocolConstants.Version)
        {
            return CreateCapabilitiesAnswerFailure(
                answer,
                "unsupported-diameter-version",
                "The Capabilities-Exchange answer uses an unsupported Diameter version.",
                DiameterResultCodes.UnsupportedVersion,
                null,
                -1);
        }

        if (answer != null &&
            ((answer.Flags & DiameterMessageFlags.Request) != DiameterMessageFlags.None ||
            answer.ApplicationId != DiameterApplicationIds.Common ||
            answer.CommandCode != DiameterCommandCodes.CapabilitiesExchange))
        {
            return CreateCapabilitiesAnswerFailure(
                answer,
                "capabilities-answer-header-mismatch",
                "The message is not a Capabilities-Exchange answer.",
                DiameterResultCodes.InvalidHeaderBits,
                null,
                -1);
        }

        if (answer != null && answer.Avps == null)
        {
            return CreateCapabilitiesAnswerFailure(
                answer,
                "avp-list-required",
                "The Capabilities-Exchange answer requires an AVP collection.",
                DiameterResultCodes.MissingAvp,
                null,
                -1);
        }

        if (answer != null)
        {
            int nullIndex = 0;
            while (nullIndex < answer.Avps.Count)
            {
                if (answer.Avps[nullIndex] == null)
                {
                    return CreateCapabilitiesAnswerFailure(
                        answer,
                        "avp-required",
                        "The Capabilities-Exchange answer contains an absent AVP.",
                        DiameterResultCodes.InvalidAvpValue,
                        null,
                        nullIndex);
                }

                nullIndex = nullIndex + 1;
            }
        }

        DiameterValidationResult validation = Validator.ValidateMessage(
            answer,
            DictionarySnapshot,
            Limits);
        PeerCapabilitiesExchangeResult result = new PeerCapabilitiesExchangeResult();
        result.Validation = validation;
        if (validation.Status != DiameterValidationStatus.Valid || answer == null)
        {
            validation.ErrorAnswer = null;
            return result;
        }

        if (request == null ||
            (request.Flags & DiameterMessageFlags.Request) == DiameterMessageFlags.None ||
            request.ApplicationId != DiameterApplicationIds.Common ||
            request.CommandCode != DiameterCommandCodes.CapabilitiesExchange ||
            answer.HopByHopIdentifier != request.HopByHopIdentifier ||
            answer.EndToEndIdentifier != request.EndToEndIdentifier)
        {
            return CreateCapabilitiesAnswerFailure(
                answer,
                "capabilities-answer-correlation-mismatch",
                "The Capabilities-Exchange answer does not match its request.",
                DiameterResultCodes.InvalidHeaderBits,
                null,
                -1);
        }

        PeerCapabilitiesExchangeResult parsed = ReadPeerCapabilities(answer);
        if (parsed.Validation.Status != DiameterValidationStatus.Valid)
        {
            parsed.Validation.ErrorAnswer = null;
            return parsed;
        }

        parsed.Validation = validation;
        parsed.ResultCode = ReadCapabilitiesAnswerResultCode(answer);
        parsed.Capabilities.CommonApplications = ReadCommonPeerApplications(
            localCapabilities,
            parsed.Capabilities);
        bool successful = Validator.ClassifyResultCode(parsed.ResultCode) ==
            DiameterResultClass.Success;
        if (successful && parsed.Capabilities.CommonApplications.Count == 0)
        {
            parsed.ResultCode = DiameterResultCodes.NoCommonApplication;
            successful = false;
        }
        else if (successful && !HasCommonPeerSecurity(
            localCapabilities,
            parsed.Capabilities))
        {
            parsed.ResultCode = DiameterResultCodes.NoCommonSecurity;
            successful = false;
        }

        parsed.Accepted = successful;
        return parsed;
    }

    internal List<DiameterApplicationId> ReadCommonPeerApplications(
        DiameterPeerCapabilities localCapabilities,
        DiameterPeerCapabilities remoteCapabilities)
    {
        ValidateLocalPeerCapabilities(localCapabilities);
        ValidateLocalPeerCapabilities(remoteCapabilities);
        List<DiameterApplicationId> localApplications =
            new List<DiameterApplicationId>();
        List<DiameterApplicationId> remoteApplications =
            new List<DiameterApplicationId>();
        AppendUniquePeerApplications(localApplications, localCapabilities);
        AppendUniquePeerApplications(remoteApplications, remoteCapabilities);
        List<DiameterApplicationId> result = new List<DiameterApplicationId>();
        bool localRelay = ContainsCapabilityIdentifier(
            localCapabilities.AuthApplicationIds,
            DiameterApplicationIds.Relay);
        bool remoteRelay = ContainsCapabilityIdentifier(
            remoteCapabilities.AuthApplicationIds,
            DiameterApplicationIds.Relay);
        if (remoteRelay)
        {
            AppendApplicationCopies(result, localApplications);
        }
        else if (localRelay)
        {
            AppendApplicationCopies(result, remoteApplications);
        }
        else
        {
            int index = 0;
            while (index < localApplications.Count)
            {
                DiameterApplicationId application = localApplications[index];
                if (ContainsSupportedPeerApplication(
                    remoteCapabilities,
                    application))
                {
                    result.Add(PeerTableCloner.ClonePeerTableApplication(application));
                }

                index = index + 1;
            }
        }

        if ((localRelay || remoteRelay) && result.Count == 0)
        {
            DiameterApplicationId relay = new DiameterApplicationId();
            relay.Id = DiameterApplicationIds.Relay;
            result.Add(relay);
        }

        return result;
    }

    internal void AppendUniquePeerApplications(
        List<DiameterApplicationId> destination,
        DiameterPeerCapabilities source)
    {
        int authIndex = 0;
        while (authIndex < source.AuthApplicationIds.Count)
        {
            DiameterApplicationId application = new DiameterApplicationId();
            application.Id = source.AuthApplicationIds[authIndex];
            if (!ContainsCommonPeerApplication(destination, application))
            {
                destination.Add(application);
            }

            authIndex = authIndex + 1;
        }

        int accountingIndex = 0;
        while (accountingIndex < source.AcctApplicationIds.Count)
        {
            DiameterApplicationId application = new DiameterApplicationId();
            application.Id = source.AcctApplicationIds[accountingIndex];
            application.IsAccounting = true;
            if (!ContainsCommonPeerApplication(destination, application))
            {
                destination.Add(application);
            }

            accountingIndex = accountingIndex + 1;
        }

        int vendorIndex = 0;
        while (vendorIndex < source.VendorSpecificApplications.Count)
        {
            DiameterApplicationId application =
                source.VendorSpecificApplications[vendorIndex];
            if (!ContainsCommonPeerApplication(destination, application))
            {
                destination.Add(PeerTableCloner.ClonePeerTableApplication(application));
            }

            vendorIndex = vendorIndex + 1;
        }
    }

    internal void AppendApplicationCopies(
        List<DiameterApplicationId> destination,
        List<DiameterApplicationId> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            DiameterApplicationId application = source[index];
            if (!ContainsCommonPeerApplication(destination, application))
            {
                destination.Add(PeerTableCloner.ClonePeerTableApplication(application));
            }

            index = index + 1;
        }
    }

    internal bool ContainsSupportedPeerApplication(
        DiameterPeerCapabilities capabilities,
        DiameterApplicationId candidate)
    {
        if (!candidate.IsAccounting && ContainsCapabilityIdentifier(
            capabilities.AuthApplicationIds,
            candidate.Id))
        {
            return true;
        }

        if (candidate.IsAccounting && ContainsCapabilityIdentifier(
            capabilities.AcctApplicationIds,
            candidate.Id))
        {
            return true;
        }

        int index = 0;
        while (index < capabilities.VendorSpecificApplications.Count)
        {
            DiameterApplicationId application =
                capabilities.VendorSpecificApplications[index];
            if (application.Id == candidate.Id &&
                application.IsAccounting == candidate.IsAccounting)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsCapabilityIdentifier(List<uint> values, uint value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasCommonPeerSecurity(
        DiameterPeerCapabilities localCapabilities,
        DiameterPeerCapabilities remoteCapabilities)
    {
        ValidateLocalPeerCapabilities(localCapabilities);
        ValidateLocalPeerCapabilities(remoteCapabilities);
        if (localCapabilities.InbandSecurityIds.Count == 0)
        {
            return remoteCapabilities.InbandSecurityIds.Count == 0 ||
                ContainsCapabilityIdentifier(
                    remoteCapabilities.InbandSecurityIds,
                    0U);
        }

        if (remoteCapabilities.InbandSecurityIds.Count == 0)
        {
            return ContainsCapabilityIdentifier(
                localCapabilities.InbandSecurityIds,
                0U);
        }

        int index = 0;
        while (index < localCapabilities.InbandSecurityIds.Count)
        {
            if (ContainsCapabilityIdentifier(
                remoteCapabilities.InbandSecurityIds,
                localCapabilities.InbandSecurityIds[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsCommonPeerApplication(
        List<DiameterApplicationId> applications,
        DiameterApplicationId candidate)
    {
        int index = 0;
        while (index < applications.Count)
        {
            DiameterApplicationId application = applications[index];
            if (application.Id == candidate.Id &&
                application.IsAccounting == candidate.IsAccounting)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal uint ReadCapabilitiesAnswerResultCode(DiameterMessage answer)
    {
        int index = 0;
        while (index < answer.Avps.Count)
        {
            DiameterAvp avp = answer.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.ResultCode)
            {
                return NetworkByteOrder.ReadUInt32(avp.Data);
            }

            index = index + 1;
        }

        return DiameterResultCodes.UnableToComply;
    }

    internal PeerCapabilitiesExchangeResult ParseCapabilitiesExchangeRequest(
        DiameterMessage? message)
    {
        if (message != null && message.Version != DiameterProtocolConstants.Version)
        {
            return CreatePeerCapabilitiesFailure(
                message,
                "unsupported-diameter-version",
                "The Capabilities-Exchange request uses an unsupported Diameter version.",
                DiameterResultCodes.UnsupportedVersion,
                null,
                -1);
        }

        if (message != null &&
            ((message.Flags & DiameterMessageFlags.Request) == DiameterMessageFlags.None ||
            message.ApplicationId != DiameterApplicationIds.Common ||
            message.CommandCode != DiameterCommandCodes.CapabilitiesExchange))
        {
            return CreatePeerCapabilitiesFailure(
                message,
                "capabilities-request-header-mismatch",
                "The message is not a Capabilities-Exchange request.",
                DiameterResultCodes.InvalidHeaderBits,
                null,
                -1);
        }

        if (message != null && message.Avps == null)
        {
            return CreatePeerCapabilitiesFailure(
                message,
                "avp-list-required",
                "The Capabilities-Exchange request requires an AVP collection.",
                DiameterResultCodes.MissingAvp,
                null,
                -1);
        }

        if (message != null)
        {
            int nullIndex = 0;
            while (nullIndex < message.Avps.Count)
            {
                if (message.Avps[nullIndex] == null)
                {
                    return CreatePeerCapabilitiesFailure(
                        message,
                        "avp-required",
                        "The Capabilities-Exchange request contains an absent AVP.",
                        DiameterResultCodes.InvalidAvpValue,
                        null,
                        nullIndex);
                }

                nullIndex = nullIndex + 1;
            }
        }

        DiameterValidationResult validation = Validator.ValidateMessage(
            message,
            DictionarySnapshot,
            Limits);
        PeerCapabilitiesExchangeResult result = new PeerCapabilitiesExchangeResult();
        result.Validation = validation;
        if (validation.Status != DiameterValidationStatus.Valid || message == null)
        {
            return result;
        }

        PeerCapabilitiesExchangeResult parsed = ReadPeerCapabilities(message);
        if (parsed.Validation.Status != DiameterValidationStatus.Valid)
        {
            return parsed;
        }

        parsed.Validation = validation;
        parsed.ResultCode = DiameterResultCodes.Success;
        parsed.Accepted = true;
        return parsed;
    }

    internal PeerCapabilitiesExchangeResult ReadPeerCapabilities(DiameterMessage message)
    {
        PeerCapabilitiesExchangeResult result = new PeerCapabilitiesExchangeResult();
        DiameterPeerCapabilities capabilities = new DiameterPeerCapabilities();
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf)
            {
                if (avp.Code == DiameterAvpCodes.OriginHost)
                {
                    DiameterValueResult value = ValueCodecs.DecodeDiameterIdentity(avp.Data);
                    capabilities.OriginHost.Value = value.Text;
                }
                else if (avp.Code == DiameterAvpCodes.OriginRealm)
                {
                    DiameterValueResult value = ValueCodecs.DecodeDiameterIdentity(avp.Data);
                    capabilities.OriginRealm.Value = value.Text;
                }
                else if (avp.Code == DiameterAvpCodes.HostIpAddress)
                {
                    DiameterValueResult value = ValueCodecs.DecodeAddress(avp.Data);
                    if (value.Address != null)
                    {
                        capabilities.HostAddresses.Add(value.Address);
                    }
                }
                else if (avp.Code == DiameterAvpCodes.VendorId)
                {
                    capabilities.VendorId = NetworkByteOrder.ReadUInt32(avp.Data);
                }
                else if (avp.Code == DiameterAvpCodes.ProductName)
                {
                    DiameterValueResult value = ValueCodecs.DecodeUtf8String(avp.Data);
                    if (value.Text.Length == 0)
                    {
                        return CreatePeerCapabilitiesFailure(
                            message,
                            "invalid-product-name",
                            "Product-Name must not be empty.",
                            DiameterResultCodes.InvalidAvpValue,
                            avp,
                            index);
                    }

                    capabilities.ProductName = value.Text;
                }
                else if (avp.Code == DiameterAvpCodes.OriginStateId)
                {
                    capabilities.OriginStateId = NetworkByteOrder.ReadUInt32(avp.Data);
                }
                else if (avp.Code == DiameterAvpCodes.SupportedVendorId)
                {
                    capabilities.SupportedVendorIds.Add(
                        NetworkByteOrder.ReadUInt32(avp.Data));
                }
                else if (avp.Code == DiameterAvpCodes.AuthApplicationId)
                {
                    capabilities.AuthApplicationIds.Add(
                        NetworkByteOrder.ReadUInt32(avp.Data));
                }
                else if (avp.Code == DiameterAvpCodes.AcctApplicationId)
                {
                    capabilities.AcctApplicationIds.Add(
                        NetworkByteOrder.ReadUInt32(avp.Data));
                }
                else if (avp.Code == DiameterAvpCodes.InbandSecurityId)
                {
                    uint securityId = NetworkByteOrder.ReadUInt32(avp.Data);
                    if (securityId > 1U)
                    {
                        return CreatePeerCapabilitiesFailure(
                            message,
                            "invalid-inband-security-id",
                            "Inband-Security-Id must identify a supported mechanism.",
                            DiameterResultCodes.InvalidAvpValue,
                            avp,
                            index);
                    }

                    capabilities.InbandSecurityIds.Add(securityId);
                }
                else if (avp.Code == DiameterAvpCodes.FirmwareRevision)
                {
                    capabilities.FirmwareRevision = NetworkByteOrder.ReadUInt32(avp.Data);
                }
                else if (avp.Code == DiameterAvpCodes.VendorSpecificApplicationId)
                {
                    DiameterApplicationId? application =
                        ParseVendorSpecificApplication(avp);
                    if (application == null)
                    {
                        return CreatePeerCapabilitiesFailure(
                            message,
                            "invalid-vendor-specific-application",
                            "A vendor-specific application must contain one application identifier.",
                            DiameterResultCodes.InvalidAvpValue,
                            avp,
                            index);
                    }

                    capabilities.VendorSpecificApplications.Add(application);
                }
            }

            index = index + 1;
        }

        capabilities.UpdatedAt = Clock.ReadUtcTime();
        result.Capabilities = capabilities;
        return result;
    }

    internal DiameterApplicationId? ParseVendorSpecificApplication(DiameterAvp avp)
    {
        DiameterValueResult grouped = AvpDecoder.DecodeAvpSequence(avp.Data);
        if (grouped.Status != DiameterCodecStatus.Success)
        {
            return null;
        }

        DiameterApplicationId result = new DiameterApplicationId();
        int applicationCount = 0;
        int index = 0;
        while (index < grouped.Avps.Count)
        {
            DiameterAvp child = grouped.Avps[index];
            if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.VendorId)
            {
                result.VendorId = NetworkByteOrder.ReadUInt32(child.Data);
            }
            else if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.AuthApplicationId)
            {
                result.Id = NetworkByteOrder.ReadUInt32(child.Data);
                result.IsAccounting = false;
                applicationCount = applicationCount + 1;
            }
            else if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.AcctApplicationId)
            {
                result.Id = NetworkByteOrder.ReadUInt32(child.Data);
                result.IsAccounting = true;
                applicationCount = applicationCount + 1;
            }

            index = index + 1;
        }

        if (result.VendorId == 0U || applicationCount != 1)
        {
            return null;
        }

        return result;
    }

    internal void ValidateLocalPeerCapabilities(DiameterPeerCapabilities capabilities)
    {
        if (capabilities == null || capabilities.OriginHost == null ||
            capabilities.OriginRealm == null || capabilities.HostAddresses == null ||
            capabilities.SupportedVendorIds == null ||
            capabilities.AuthApplicationIds == null ||
            capabilities.AcctApplicationIds == null ||
            capabilities.VendorSpecificApplications == null ||
            capabilities.InbandSecurityIds == null)
        {
            throw new ArgumentException("Local capability values must not be absent.");
        }

        int applicationIndex = 0;
        while (applicationIndex < capabilities.VendorSpecificApplications.Count)
        {
            DiameterApplicationId application =
                capabilities.VendorSpecificApplications[applicationIndex];
            if (application == null || application.VendorId == 0U)
            {
                throw new ArgumentException(
                    "Vendor-specific applications require a nonzero Vendor-Id.");
            }

            applicationIndex = applicationIndex + 1;
        }

        int securityIndex = 0;
        while (securityIndex < capabilities.InbandSecurityIds.Count)
        {
            if (capabilities.InbandSecurityIds[securityIndex] > 1U)
            {
                throw new ArgumentException(
                    "Inband security identifiers must identify supported mechanisms.");
            }

            securityIndex = securityIndex + 1;
        }
    }

    internal PeerCapabilitiesExchangeResult CreatePeerCapabilitiesFailure(
        DiameterMessage? request,
        string code,
        string message,
        uint resultCode,
        DiameterAvp? avp,
        int avpIndex)
    {
        DiameterValidationIssue issue = new DiameterValidationIssue();
        issue.Severity = DiameterIssueSeverity.Error;
        issue.Code = code;
        issue.Message = message;
        issue.ResultCode = resultCode;
        issue.AvpIndex = avpIndex;
        if (avp != null)
        {
            issue.AvpCode = avp.Code;
            issue.AvpVendorId = avp.VendorId;
            issue.OffendingAvp = avp;
            issue.FailedAvp = new ValidationResultBuilder(
                DictionarySnapshot).CreateFailedWrapper(avp, 0U, 0U);
        }

        DiameterErrorAnswerRequest error = new DiameterErrorAnswerRequest();
        error.Request = request;
        error.Issue = issue;
        error.ResultCode = resultCode;
        error.ErrorMessage = message;
        error.FailedAvp = issue.FailedAvp;
        DiameterValidationResult validation = new DiameterValidationResult();
        validation.Status = DiameterValidationStatus.Invalid;
        validation.Issues.Add(issue);
        validation.ErrorAnswer = error;
        PeerCapabilitiesExchangeResult result = new PeerCapabilitiesExchangeResult();
        result.Validation = validation;
        return result;
    }

    internal PeerCapabilitiesExchangeResult CreateCapabilitiesAnswerFailure(
        DiameterMessage? answer,
        string code,
        string message,
        uint resultCode,
        DiameterAvp? avp,
        int avpIndex)
    {
        PeerCapabilitiesExchangeResult result = CreatePeerCapabilitiesFailure(
            answer,
            code,
            message,
            resultCode,
            avp,
            avpIndex);
        result.Validation.ErrorAnswer = null;
        return result;
    }

    internal DiameterLimitConfiguration CloneCapabilitiesLimits(
        DiameterLimitConfiguration source)
    {
        DiameterLimitConfiguration result = new DiameterLimitConfiguration();
        result.MaximumMessageSize = source.MaximumMessageSize;
        result.MaximumAvpCount = source.MaximumAvpCount;
        result.MaximumGroupedDepth = source.MaximumGroupedDepth;
        result.MaximumGroupedAvpCount = source.MaximumGroupedAvpCount;
        result.MaximumGroupedBytes = source.MaximumGroupedBytes;
        result.MaximumCodecIssueCount = source.MaximumCodecIssueCount;
        result.MaximumConnectionCount = source.MaximumConnectionCount;
        result.MaximumPendingRequests = source.MaximumPendingRequests;
        result.IngressQueueCapacity = source.IngressQueueCapacity;
        result.EgressQueueCapacity = source.EgressQueueCapacity;
        result.ApplicationQueueCapacity = source.ApplicationQueueCapacity;
        return result;
    }
}
