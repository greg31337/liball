using System;
using System.Collections.Generic;

namespace diameter;

internal class PeerCapabilitiesUpdatePending
{
    internal uint HopByHopIdentifier;
    internal uint EndToEndIdentifier;
}

internal class PeerCapabilitiesUpdateResult
{
    internal bool Handled;
    internal bool Accepted;
    internal bool DisconnectRequired;
    internal bool SecurityRestartRequired;
    internal uint ResultCode;
    internal DiameterCapabilityChange CapabilityChange;
    internal DiameterMessage? Message;
    internal DiameterValidationResult Validation = new DiameterValidationResult();
    internal DiameterPeerSnapshot Peer = new DiameterPeerSnapshot();
    internal string Code = string.Empty;
}

internal class PeerCapabilitiesUpdateController
{
    internal PeerTable Table;
    internal PeerCapabilitiesExchange Exchange;
    internal object UpdateLock = new object();
    internal Dictionary<string, PeerCapabilitiesUpdatePending> Pending =
        new Dictionary<string, PeerCapabilitiesUpdatePending>(
            StringComparer.OrdinalIgnoreCase);

    internal PeerCapabilitiesUpdateController(
        PeerTable table,
        IDiameterIdentifierSource identifierSource,
        DiameterLimitConfiguration limits)
    {
        if (table == null || identifierSource == null || limits == null)
        {
            throw new ArgumentException(
                "A peer table, identifier source, and limits are required.");
        }

        Table = table;
        Exchange = new PeerCapabilitiesExchange(
            identifierSource,
            table.Clock,
            limits);
    }

    internal PeerCapabilitiesUpdateResult BeginPeerCapabilitiesUpdate(
        string identity,
        DiameterPeerCapabilities localCapabilities)
    {
        Exchange.ValidateLocalPeerCapabilities(localCapabilities);
        DiameterPeerCapabilities ownedLocal =
            PeerTableCloner.ClonePeerTableCapabilities(localCapabilities);
        string key = Table.ReadPeerTableKey(identity);
        lock (UpdateLock)
        {
            lock (Table.TableLock)
            {
                DiameterPeerSnapshot snapshot =
                    Table.ReadRequiredPeerTableEntry(key).Snapshot;
                PeerCapabilitiesUpdateResult result =
                    CreatePeerCapabilitiesUpdateResult(
                        snapshot,
                        "capabilities-update-not-started");
                if (!IsPeerCapabilitiesUpdateOpen(snapshot))
                {
                    result.Code = "capabilities-update-peer-not-open";
                    return result;
                }

                if (!Exchange.ContainsCapabilityIdentifier(
                    ownedLocal.AuthApplicationIds,
                    DiameterApplicationIds.CapabilitiesUpdate) ||
                    !Exchange.ContainsCapabilityIdentifier(
                        snapshot.Capabilities.AuthApplicationIds,
                        DiameterApplicationIds.CapabilitiesUpdate))
                {
                    result.Code = "capabilities-update-not-supported";
                    return result;
                }

                if (Pending.ContainsKey(key))
                {
                    result.Code = "capabilities-update-already-pending";
                    return result;
                }

                DiameterMessage request =
                    CreatePeerCapabilitiesUpdateRequestMessage(ownedLocal);
                PeerCapabilitiesUpdatePending pending =
                    new PeerCapabilitiesUpdatePending();
                pending.HopByHopIdentifier = request.HopByHopIdentifier;
                pending.EndToEndIdentifier = request.EndToEndIdentifier;
                Pending.Add(key, pending);
                result.Handled = true;
                result.Accepted = true;
                result.Message = request;
                result.Code = "capabilities-update-request-created";
                return result;
            }
        }
    }

    internal PeerCapabilitiesUpdateResult HandlePeerCapabilitiesUpdateRequest(
        string identity,
        DiameterMessage request,
        DiameterPeerCapabilities localCapabilities)
    {
        Exchange.ValidateLocalPeerCapabilities(localCapabilities);
        DiameterPeerCapabilities ownedLocal =
            PeerTableCloner.ClonePeerTableCapabilities(localCapabilities);
        PeerCapabilitiesExchangeResult parsed =
            ParsePeerCapabilitiesUpdateRequest(request);
        string key = Table.ReadPeerTableKey(identity);
        lock (UpdateLock)
        {
            lock (Table.TableLock)
            {
                DiameterPeerSnapshot snapshot =
                    Table.ReadRequiredPeerTableEntry(key).Snapshot;
                PeerCapabilitiesUpdateResult result =
                    CreatePeerCapabilitiesUpdateResult(
                        snapshot,
                        "capabilities-update-request-rejected");
                result.Validation = parsed.Validation;
                if (!parsed.Accepted)
                {
                    result.Code = "invalid-capabilities-update-request";
                    return result;
                }

                if (!IsPeerCapabilitiesUpdateOpen(snapshot) ||
                    !Exchange.ContainsCapabilityIdentifier(
                        ownedLocal.AuthApplicationIds,
                        DiameterApplicationIds.CapabilitiesUpdate) ||
                    !Exchange.ContainsCapabilityIdentifier(
                        snapshot.Capabilities.AuthApplicationIds,
                        DiameterApplicationIds.CapabilitiesUpdate))
                {
                    result.Handled = true;
                    result.ResultCode = DiameterResultCodes.UnableToComply;
                    result.Message = CreatePeerCapabilitiesUpdateAnswerMessage(
                        request,
                        result.ResultCode,
                        ownedLocal);
                    result.Code = "capabilities-update-not-supported";
                    return result;
                }

                DiameterPeerCapabilities proposed = parsed.Capabilities;
                if (!ValueCodecs.AreDiameterIdentitiesEqual(
                        snapshot.Configuration.Identity,
                        proposed.OriginHost) ||
                    !ValueCodecs.AreDiameterRealmsEqual(
                        snapshot.Configuration.Realm,
                        proposed.OriginRealm))
                {
                    result.Handled = true;
                    result.ResultCode = DiameterResultCodes.UnableToComply;
                    result.Message = CreatePeerCapabilitiesUpdateAnswerMessage(
                        request,
                        result.ResultCode,
                        ownedLocal);
                    result.Code = "capabilities-update-peer-mismatch";
                    return result;
                }

                if (proposed.InbandSecurityIds.Count > 0 &&
                    !ArePeerCapabilityIdentifiersEqual(
                        proposed.InbandSecurityIds,
                        snapshot.Capabilities.InbandSecurityIds))
                {
                    result.Handled = true;
                    result.DisconnectRequired = true;
                    result.SecurityRestartRequired = true;
                    result.ResultCode = DiameterResultCodes.UnableToComply;
                    result.CapabilityChange =
                        DiameterCapabilityChange.SecurityMechanisms;
                    result.Message = CreatePeerCapabilitiesUpdateAnswerMessage(
                        request,
                        result.ResultCode,
                        ownedLocal);
                    result.Code = "capabilities-update-security-restart-required";
                    return result;
                }

                proposed.InbandSecurityIds.Clear();
                CopyPeerCapabilityIdentifiers(
                    proposed.InbandSecurityIds,
                    snapshot.Capabilities.InbandSecurityIds);
                proposed.CommonApplications = Exchange.ReadCommonPeerApplications(
                    ownedLocal,
                    proposed);
                if (proposed.CommonApplications.Count == 0)
                {
                    result.Handled = true;
                    result.DisconnectRequired = true;
                    result.ResultCode = DiameterResultCodes.NoCommonApplication;
                    result.Message = CreatePeerCapabilitiesUpdateAnswerMessage(
                        request,
                        result.ResultCode,
                        ownedLocal);
                    result.Code = "capabilities-update-no-common-application";
                    return result;
                }

                result.CapabilityChange = ReadPeerCapabilitiesUpdateChange(
                    snapshot.Capabilities,
                    proposed);
                DiameterPeerSnapshot updated = Table.UpdatePeerTableCapabilities(
                    key,
                    proposed);
                result.Handled = true;
                result.Accepted = true;
                result.ResultCode = DiameterResultCodes.Success;
                result.Message = CreatePeerCapabilitiesUpdateAnswerMessage(
                    request,
                    result.ResultCode,
                    ownedLocal);
                result.Peer = updated;
                result.Code = "capabilities-update-accepted";
                return result;
            }
        }
    }

    internal PeerCapabilitiesUpdateResult ProcessPeerCapabilitiesUpdateAnswer(
        string identity,
        DiameterMessage answer)
    {
        PeerCapabilitiesExchangeResult parsed =
            ParsePeerCapabilitiesUpdateAnswer(answer);
        string key = Table.ReadPeerTableKey(identity);
        lock (UpdateLock)
        {
            lock (Table.TableLock)
            {
                DiameterPeerSnapshot snapshot =
                    Table.ReadRequiredPeerTableEntry(key).Snapshot;
                PeerCapabilitiesUpdateResult result =
                    CreatePeerCapabilitiesUpdateResult(
                        snapshot,
                        "capabilities-update-answer-rejected");
                result.Validation = parsed.Validation;
                if (!parsed.Accepted)
                {
                    result.Code = "invalid-capabilities-update-answer";
                    return result;
                }

                if (!Pending.ContainsKey(key))
                {
                    result.Code = "capabilities-update-answer-not-pending";
                    return result;
                }

                PeerCapabilitiesUpdatePending pending = Pending[key];
                if (answer.HopByHopIdentifier != pending.HopByHopIdentifier ||
                    answer.EndToEndIdentifier != pending.EndToEndIdentifier)
                {
                    result.Code = "capabilities-update-answer-not-correlated";
                    return result;
                }

                Pending.Remove(key);
                result.Handled = true;
                result.ResultCode = parsed.ResultCode;
                result.Accepted = Exchange.Validator.ClassifyResultCode(
                    parsed.ResultCode) == DiameterResultClass.Success;
                if (parsed.ResultCode == DiameterResultCodes.NoCommonApplication)
                {
                    result.DisconnectRequired = true;
                    result.Code = "capabilities-update-answer-no-common-application";
                }
                else if (result.Accepted)
                {
                    result.Code = "capabilities-update-answer-accepted";
                }
                else
                {
                    result.Code = "capabilities-update-answer-failed";
                }

                return result;
            }
        }
    }

    internal PeerCapabilitiesExchangeResult ParsePeerCapabilitiesUpdateRequest(
        DiameterMessage message)
    {
        if (message == null || message.Version != DiameterProtocolConstants.Version ||
            (message.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            message.ApplicationId != DiameterApplicationIds.CapabilitiesUpdate ||
            message.CommandCode != DiameterCommandCodes.CapabilitiesUpdate)
        {
            return Exchange.CreatePeerCapabilitiesFailure(
                message,
                "capabilities-update-request-header-mismatch",
                "The message is not a Capabilities-Update request.",
                DiameterResultCodes.InvalidHeaderBits,
                null,
                -1);
        }

        DiameterValidationResult validation = Exchange.Validator.ValidateMessage(
            message,
            Exchange.DictionarySnapshot,
            Exchange.Limits);
        PeerCapabilitiesExchangeResult result =
            new PeerCapabilitiesExchangeResult();
        result.Validation = validation;
        if (validation.Status != DiameterValidationStatus.Valid)
        {
            return result;
        }

        PeerCapabilitiesExchangeResult capabilities =
            Exchange.ReadPeerCapabilities(message);
        if (capabilities.Validation.Status == DiameterValidationStatus.Invalid)
        {
            return capabilities;
        }

        capabilities.Validation = validation;
        capabilities.Accepted = true;
        capabilities.ResultCode = DiameterResultCodes.Success;
        return capabilities;
    }

    internal PeerCapabilitiesExchangeResult ParsePeerCapabilitiesUpdateAnswer(
        DiameterMessage answer)
    {
        if (answer == null || answer.Version != DiameterProtocolConstants.Version ||
            (answer.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None ||
            answer.ApplicationId != DiameterApplicationIds.CapabilitiesUpdate ||
            answer.CommandCode != DiameterCommandCodes.CapabilitiesUpdate)
        {
            return Exchange.CreateCapabilitiesAnswerFailure(
                answer,
                "capabilities-update-answer-header-mismatch",
                "The message is not a Capabilities-Update answer.",
                DiameterResultCodes.InvalidHeaderBits,
                null,
                -1);
        }

        DiameterValidationResult validation = Exchange.Validator.ValidateMessage(
            answer,
            Exchange.DictionarySnapshot,
            Exchange.Limits);
        PeerCapabilitiesExchangeResult result =
            new PeerCapabilitiesExchangeResult();
        result.Validation = validation;
        if (validation.Status != DiameterValidationStatus.Valid)
        {
            validation.ErrorAnswer = null;
            return result;
        }

        result.Accepted = true;
        result.ResultCode = Exchange.ReadCapabilitiesAnswerResultCode(answer);
        return result;
    }

    internal DiameterMessage CreatePeerCapabilitiesUpdateRequestMessage(
        DiameterPeerCapabilities capabilities)
    {
        Exchange.ValidateLocalPeerCapabilities(capabilities);
        DiameterMessage message = Exchange.BaseCommands.CreateRequestMessage(
            DiameterApplicationIds.CapabilitiesUpdate,
            DiameterCommandCodes.CapabilitiesUpdate,
            false);
        AddPeerCapabilitiesUpdateAvps(message, capabilities);
        return message;
    }

    internal DiameterMessage CreatePeerCapabilitiesUpdateAnswerMessage(
        DiameterMessage request,
        uint resultCode,
        DiameterPeerCapabilities localCapabilities)
    {
        DiameterMessage message = Exchange.BaseCommands.CreateAnswerMessage(
            request,
            false);
        DiameterBaseCommandOptions options =
            Exchange.CreateCapabilitiesCommandOptions(localCapabilities);
        Exchange.BaseCommands.AddOriginAvps(message, options);
        message.Avps.Add(Exchange.BaseCommands.CreateUnsigned32Avp(
            DiameterAvpCodes.ResultCode,
            resultCode));
        return message;
    }

    internal void AddPeerCapabilitiesUpdateAvps(
        DiameterMessage message,
        DiameterPeerCapabilities capabilities)
    {
        DiameterBaseCommandOptions options =
            Exchange.CreateCapabilitiesCommandOptions(capabilities);
        Exchange.BaseCommands.AddOriginAvps(message, options);
        int addressIndex = 0;
        while (addressIndex < options.HostAddresses.Count)
        {
            message.Avps.Add(Exchange.BaseCommands.CreateAddressAvp(
                options.HostAddresses[addressIndex]));
            addressIndex = addressIndex + 1;
        }

        message.Avps.Add(Exchange.BaseCommands.CreateUnsigned32Avp(
            DiameterAvpCodes.VendorId,
            options.VendorId));
        message.Avps.Add(Exchange.BaseCommands.CreateOptionalTextAvp(
            DiameterAvpCodes.ProductName,
            options.ProductName));
        Exchange.BaseCommands.AddOriginStateAvp(message);
        int vendorIndex = 0;
        while (vendorIndex < options.SupportedVendorIds.Count)
        {
            message.Avps.Add(Exchange.BaseCommands.CreateUnsigned32Avp(
                DiameterAvpCodes.SupportedVendorId,
                options.SupportedVendorIds[vendorIndex]));
            vendorIndex = vendorIndex + 1;
        }

        int authIndex = 0;
        while (authIndex < options.AuthApplicationIds.Count)
        {
            message.Avps.Add(Exchange.BaseCommands.AvpFactory.CreateAuthApplicationIdAvp(
                options.AuthApplicationIds[authIndex]));
            authIndex = authIndex + 1;
        }

        int accountingIndex = 0;
        while (accountingIndex < options.AcctApplicationIds.Count)
        {
            message.Avps.Add(Exchange.BaseCommands.AvpFactory.CreateAcctApplicationIdAvp(
                options.AcctApplicationIds[accountingIndex]));
            accountingIndex = accountingIndex + 1;
        }

        int applicationIndex = 0;
        while (applicationIndex < options.VendorSpecificApplications.Count)
        {
            message.Avps.Add(
                Exchange.BaseCommands.AvpFactory.CreateVendorSpecificApplicationIdAvp(
                    options.VendorSpecificApplications[applicationIndex]));
            applicationIndex = applicationIndex + 1;
        }

        message.Avps.Add(Exchange.BaseCommands.CreateOptionalUnsigned32Avp(
            DiameterAvpCodes.FirmwareRevision,
            options.FirmwareRevision));
    }

    internal DiameterCapabilityChange ReadPeerCapabilitiesUpdateChange(
        DiameterPeerCapabilities current,
        DiameterPeerCapabilities updated)
    {
        DiameterCapabilityChange result = DiameterCapabilityChange.None;
        if (!ArePeerCapabilityAddressesEqual(
            current.HostAddresses,
            updated.HostAddresses))
        {
            result = AddPeerCapabilityChange(
                result,
                DiameterCapabilityChange.HostAddresses);
        }

        if (current.VendorId != updated.VendorId ||
            !ArePeerCapabilityIdentifiersEqual(
                current.SupportedVendorIds,
                updated.SupportedVendorIds))
        {
            result = AddPeerCapabilityChange(
                result,
                DiameterCapabilityChange.VendorInformation);
        }

        if (!string.Equals(
            current.ProductName,
            updated.ProductName,
            StringComparison.Ordinal))
        {
            result = AddPeerCapabilityChange(
                result,
                DiameterCapabilityChange.ProductName);
        }

        if (current.FirmwareRevision != updated.FirmwareRevision)
        {
            result = AddPeerCapabilityChange(
                result,
                DiameterCapabilityChange.FirmwareRevision);
        }

        if (!ArePeerCapabilityIdentifiersEqual(
                current.AuthApplicationIds,
                updated.AuthApplicationIds) ||
            !ArePeerCapabilityIdentifiersEqual(
                current.AcctApplicationIds,
                updated.AcctApplicationIds) ||
            !ArePeerCapabilityApplicationsEqual(
                current.VendorSpecificApplications,
                updated.VendorSpecificApplications))
        {
            result = AddPeerCapabilityChange(
                result,
                DiameterCapabilityChange.Applications);
        }

        if (!ArePeerCapabilityIdentifiersEqual(
            current.InbandSecurityIds,
            updated.InbandSecurityIds))
        {
            result = AddPeerCapabilityChange(
                result,
                DiameterCapabilityChange.SecurityMechanisms);
        }

        return result;
    }

    internal bool ArePeerCapabilityIdentifiersEqual(
        List<uint> first,
        List<uint> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        bool[] matched = new bool[second.Count];
        int firstIndex = 0;
        while (firstIndex < first.Count)
        {
            int secondIndex = 0;
            bool found = false;
            while (secondIndex < second.Count && !found)
            {
                if (!matched[secondIndex] &&
                    first[firstIndex] == second[secondIndex])
                {
                    matched[secondIndex] = true;
                    found = true;
                }

                secondIndex = secondIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            firstIndex = firstIndex + 1;
        }

        return true;
    }

    internal bool ArePeerCapabilityAddressesEqual(
        List<System.Net.IPAddress> first,
        List<System.Net.IPAddress> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        bool[] matched = new bool[second.Count];
        int firstIndex = 0;
        while (firstIndex < first.Count)
        {
            int secondIndex = 0;
            bool found = false;
            while (secondIndex < second.Count && !found)
            {
                if (!matched[secondIndex] &&
                    first[firstIndex].Equals(second[secondIndex]))
                {
                    matched[secondIndex] = true;
                    found = true;
                }

                secondIndex = secondIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            firstIndex = firstIndex + 1;
        }

        return true;
    }

    internal bool ArePeerCapabilityApplicationsEqual(
        List<DiameterApplicationId> first,
        List<DiameterApplicationId> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        bool[] matched = new bool[second.Count];
        int firstIndex = 0;
        while (firstIndex < first.Count)
        {
            int secondIndex = 0;
            bool found = false;
            while (secondIndex < second.Count && !found)
            {
                DiameterApplicationId left = first[firstIndex];
                DiameterApplicationId right = second[secondIndex];
                if (!matched[secondIndex] && left.Id == right.Id &&
                    left.VendorId == right.VendorId &&
                    left.IsAccounting == right.IsAccounting)
                {
                    matched[secondIndex] = true;
                    found = true;
                }

                secondIndex = secondIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            firstIndex = firstIndex + 1;
        }

        return true;
    }

    internal void CopyPeerCapabilityIdentifiers(
        List<uint> destination,
        List<uint> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    internal DiameterCapabilityChange AddPeerCapabilityChange(
        DiameterCapabilityChange current,
        DiameterCapabilityChange added)
    {
        return (DiameterCapabilityChange)((int)current | (int)added);
    }

    internal bool IsPeerCapabilitiesUpdateOpen(DiameterPeerSnapshot snapshot)
    {
        return (snapshot.State == DiameterPeerState.InitiatorOpen ||
            snapshot.State == DiameterPeerState.ResponderOpen) &&
            snapshot.Connection != null && snapshot.Connection.Open;
    }

    internal PeerCapabilitiesUpdateResult CreatePeerCapabilitiesUpdateResult(
        DiameterPeerSnapshot snapshot,
        string code)
    {
        PeerCapabilitiesUpdateResult result =
            new PeerCapabilitiesUpdateResult();
        result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        result.Code = code;
        return result;
    }
}
