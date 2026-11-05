using System;
using System.Net;
using System.Text;

namespace diameter;

/// <summary>
/// Constructs dictionary-valid RFC 6733 base requests and successful or application-error answers.
/// </summary>
public class DiameterBaseCommandFactory : IDiameterBaseCommandFactory
{
    internal IDiameterIdentifierSource IdentifierSource;
    internal DiameterBaseAvpFactory AvpFactory;

    /// <summary>Initializes a base command factory with its protocol identifier source.</summary>
    /// <param name="identifierSource">The source used for new request identifiers.</param>
    /// <exception cref="System.ArgumentException">Thrown when the identifier source is absent.</exception>
    public DiameterBaseCommandFactory(IDiameterIdentifierSource identifierSource)
    {
        if (identifierSource == null)
        {
            throw new ArgumentException("A base command factory requires an identifier source.");
        }

        IdentifierSource = identifierSource;
        AvpFactory = new DiameterBaseAvpFactory();
    }

    /// <summary>Creates a Capabilities-Exchange request.</summary>
    /// <param name="options">The local capabilities and origin values.</param>
    /// <returns>The constructed request.</returns>
    public DiameterMessage CreateCapabilitiesExchangeRequest(DiameterBaseCommandOptions? options)
    {
        DiameterBaseCommandOptions input = ValidateCapabilitiesOptions(options);
        DiameterMessage message = CreateRequestMessage(
            DiameterApplicationIds.Common,
            DiameterCommandCodes.CapabilitiesExchange,
            false);
        AddCapabilitiesAvps(message, input);
        return message;
    }

    /// <summary>Creates a Capabilities-Exchange answer.</summary>
    /// <param name="options">The source request, result, and local capabilities.</param>
    /// <returns>The constructed answer.</returns>
    public DiameterMessage CreateCapabilitiesExchangeAnswer(DiameterBaseCommandOptions? options)
    {
        DiameterBaseCommandOptions input = ValidateCapabilitiesOptions(options);
        DiameterMessage request = ValidateAnswerRequest(
            input,
            DiameterApplicationIds.Common,
            DiameterCommandCodes.CapabilitiesExchange);
        DiameterMessage message = CreateAnswerMessage(request, false);
        message.Avps.Add(CreateUnsigned32Avp(DiameterAvpCodes.ResultCode, input.ResultCode));
        AddCapabilitiesAvps(message, input);
        return message;
    }

    /// <summary>Creates a Device-Watchdog request.</summary>
    /// <param name="options">The local origin values.</param>
    /// <returns>The constructed request.</returns>
    public DiameterMessage CreateDeviceWatchdogRequest(DiameterBaseCommandOptions? options)
    {
        DiameterBaseCommandOptions input = ValidateOriginOptions(options);
        DiameterMessage message = CreateRequestMessage(
            DiameterApplicationIds.Common,
            DiameterCommandCodes.DeviceWatchdog,
            false);
        AddOriginAvps(message, input);
        AddOriginStateAvp(message);
        return message;
    }

    /// <summary>Creates a Device-Watchdog answer.</summary>
    /// <param name="options">The source request, result, and local origin values.</param>
    /// <returns>The constructed answer.</returns>
    public DiameterMessage CreateDeviceWatchdogAnswer(DiameterBaseCommandOptions? options)
    {
        DiameterBaseCommandOptions input = ValidateOriginOptions(options);
        DiameterMessage request = ValidateAnswerRequest(
            input,
            DiameterApplicationIds.Common,
            DiameterCommandCodes.DeviceWatchdog);
        DiameterMessage message = CreateAnswerMessage(request, false);
        AddResultAndOriginAvps(message, input);
        AddOriginStateAvp(message);
        return message;
    }

    /// <summary>Creates a Disconnect-Peer request.</summary>
    /// <param name="options">The local origin values and disconnect cause.</param>
    /// <returns>The constructed request.</returns>
    public DiameterMessage CreateDisconnectPeerRequest(DiameterBaseCommandOptions? options)
    {
        DiameterBaseCommandOptions input = ValidateOriginOptions(options);
        int cause = (int)input.DisconnectCause;
        if (cause < 0 || cause > 2)
        {
            throw new ArgumentException("A valid Disconnect-Cause is required.");
        }

        DiameterMessage message = CreateRequestMessage(
            DiameterApplicationIds.Common,
            DiameterCommandCodes.DisconnectPeer,
            false);
        AddOriginAvps(message, input);
        message.Avps.Add(CreateEnumeratedAvp(DiameterAvpCodes.DisconnectCause, cause));
        return message;
    }

    /// <summary>Creates a Disconnect-Peer answer.</summary>
    /// <param name="options">The source request, result, and local origin values.</param>
    /// <returns>The constructed answer.</returns>
    public DiameterMessage CreateDisconnectPeerAnswer(DiameterBaseCommandOptions? options)
    {
        DiameterBaseCommandOptions input = ValidateOriginOptions(options);
        DiameterMessage request = ValidateAnswerRequest(
            input,
            DiameterApplicationIds.Common,
            DiameterCommandCodes.DisconnectPeer);
        DiameterMessage message = CreateAnswerMessage(request, false);
        AddResultAndOriginAvps(message, input);
        return message;
    }

    /// <summary>Creates a Re-Auth request.</summary>
    /// <param name="options">The session, destination, application, and origin values.</param>
    /// <returns>The constructed request.</returns>
    public DiameterMessage CreateReAuthRequest(DiameterBaseCommandOptions? options)
    {
        DiameterBaseCommandOptions input = ValidateRoutedSessionOptions(options, true);
        int requestType = (int)input.ReAuthRequestType;
        if (requestType < 0 || requestType > 1)
        {
            throw new ArgumentException("A valid Re-Auth-Request-Type is required.");
        }

        if (input.CommandApplicationId !=
                DiameterApplicationIds.Common &&
            input.CommandApplicationId !=
                DiameterApplicationIds.CreditControl)
        {
            throw new ArgumentException(
                "Re-Auth supports only base or credit-control Application-Id.");
        }

        DiameterMessage message = CreateSessionRequestMessage(
            DiameterCommandCodes.ReAuth,
            input.CommandApplicationId,
            input);
        message.Avps.Add(CreateEnumeratedAvp(
            DiameterAvpCodes.ReAuthRequestType,
            requestType));
        AddCreditControlReAuthScope(message, input);
        AddOriginStateAvp(message);
        return message;
    }

    /// <summary>Creates a Re-Auth answer.</summary>
    /// <param name="options">The source request, result, and local origin values.</param>
    /// <returns>The constructed answer.</returns>
    public DiameterMessage CreateReAuthAnswer(DiameterBaseCommandOptions? options)
    {
        DiameterBaseCommandOptions input = ValidateOriginOptions(options);
        DiameterMessage? requestValue = input.Request;
        if (requestValue == null || requestValue.Avps == null ||
            (requestValue.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            requestValue.CommandCode != DiameterCommandCodes.ReAuth ||
            requestValue.ApplicationId != DiameterApplicationIds.Common &&
            requestValue.ApplicationId !=
                DiameterApplicationIds.CreditControl)
        {
            throw new ArgumentException(
                "The source request does not match the Re-Auth command.");
        }

        DiameterMessage request = requestValue;
        DiameterMessage message = CreateAnswerMessage(request, true);
        message.Avps.Add(FindAndCloneRequiredAvp(
            request,
            DiameterAvpCodes.SessionId));
        message.Avps.Add(CreateUnsigned32Avp(
            DiameterAvpCodes.ResultCode,
            input.ResultCode));
        AddOriginAvps(message, input);
        AddOriginStateAvp(message);
        CopyCreditControlReAuthScope(request, message);
        return message;
    }

    /// <summary>Creates an Abort-Session request.</summary>
    /// <param name="options">The session, destination, application, and origin values.</param>
    /// <returns>The constructed request.</returns>
    public DiameterMessage CreateAbortSessionRequest(DiameterBaseCommandOptions? options)
    {
        DiameterBaseCommandOptions input = ValidateRoutedSessionOptions(options, true);
        DiameterMessage message = CreateSessionRequestMessage(
            DiameterCommandCodes.AbortSession,
            DiameterApplicationIds.Common,
            input);
        AddOriginStateAvp(message);
        return message;
    }

    /// <summary>Creates an Abort-Session answer.</summary>
    /// <param name="options">The source request, result, and local origin values.</param>
    /// <returns>The constructed answer.</returns>
    public DiameterMessage CreateAbortSessionAnswer(DiameterBaseCommandOptions? options)
    {
        return CreateSessionAnswerMessage(options, DiameterCommandCodes.AbortSession);
    }

    /// <summary>Creates a Session-Termination request.</summary>
    /// <param name="options">The session, destination, application, termination, and origin values.</param>
    /// <returns>The constructed request.</returns>
    public DiameterMessage CreateSessionTerminationRequest(DiameterBaseCommandOptions? options)
    {
        DiameterBaseCommandOptions input = ValidateRoutedSessionOptions(options, false);
        int cause = (int)input.TerminationCause;
        if (cause < 1 || cause > 8)
        {
            throw new ArgumentException("A valid Termination-Cause is required.");
        }

        DiameterMessage message = CreateSessionRequestMessage(
            DiameterCommandCodes.SessionTermination,
            DiameterApplicationIds.Common,
            input);
        message.Avps.Add(CreateEnumeratedAvp(DiameterAvpCodes.TerminationCause, cause));
        AddOriginStateAvp(message);
        return message;
    }

    /// <summary>Creates a Session-Termination answer.</summary>
    /// <param name="options">The source request, result, and local origin values.</param>
    /// <returns>The constructed answer.</returns>
    public DiameterMessage CreateSessionTerminationAnswer(DiameterBaseCommandOptions? options)
    {
        return CreateSessionAnswerMessage(options, DiameterCommandCodes.SessionTermination);
    }

    /// <summary>Creates an Accounting request.</summary>
    /// <param name="options">The session, destination, record, and origin values.</param>
    /// <returns>The constructed request.</returns>
    public DiameterMessage CreateAccountingRequest(DiameterBaseCommandOptions? options)
    {
        DiameterBaseCommandOptions input = ValidateAccountingOptions(options);
        DiameterMessage message = CreateRequestMessage(
            DiameterApplicationIds.BaseAccounting,
            DiameterCommandCodes.Accounting,
            true);
        message.Avps.Add(AvpFactory.CreateSessionIdAvp(input.SessionId));
        AddOriginAvps(message, input);
        message.Avps.Add(AvpFactory.CreateDestinationRealmAvp(input.DestinationRealm));
        message.Avps.Add(CreateEnumeratedAvp(
            DiameterAvpCodes.AccountingRecordType,
            (int)input.AccountingRecordType));
        message.Avps.Add(CreateUnsigned32Avp(
            DiameterAvpCodes.AccountingRecordNumber,
            input.AccountingRecordNumber));
        AddOriginStateAvp(message);
        return message;
    }

    /// <summary>Creates an Accounting answer.</summary>
    /// <param name="options">The source request, result, and local origin values.</param>
    /// <returns>The constructed answer.</returns>
    public DiameterMessage CreateAccountingAnswer(DiameterBaseCommandOptions? options)
    {
        DiameterBaseCommandOptions input = ValidateOriginOptions(options);
        DiameterMessage request = ValidateAnswerRequest(
            input,
            DiameterApplicationIds.BaseAccounting,
            DiameterCommandCodes.Accounting);
        DiameterMessage message = CreateAnswerMessage(request, true);
        message.Avps.Add(FindAndCloneRequiredAvp(request, DiameterAvpCodes.SessionId));
        message.Avps.Add(CreateUnsigned32Avp(DiameterAvpCodes.ResultCode, input.ResultCode));
        AddOriginAvps(message, input);
        message.Avps.Add(FindAndCloneRequiredAvp(
            request,
            DiameterAvpCodes.AccountingRecordType));
        message.Avps.Add(FindAndCloneRequiredAvp(
            request,
            DiameterAvpCodes.AccountingRecordNumber));
        AddOriginStateAvp(message);
        return message;
    }

    internal DiameterMessage CreateSessionAnswerMessage(
        DiameterBaseCommandOptions? options,
        uint commandCode)
    {
        DiameterBaseCommandOptions input = ValidateOriginOptions(options);
        DiameterMessage request = ValidateAnswerRequest(
            input,
            DiameterApplicationIds.Common,
            commandCode);
        DiameterMessage message = CreateAnswerMessage(request, true);
        message.Avps.Add(FindAndCloneRequiredAvp(request, DiameterAvpCodes.SessionId));
        message.Avps.Add(CreateUnsigned32Avp(DiameterAvpCodes.ResultCode, input.ResultCode));
        AddOriginAvps(message, input);
        AddOriginStateAvp(message);
        return message;
    }

    internal DiameterMessage CreateSessionRequestMessage(
        uint commandCode,
        uint applicationId,
        DiameterBaseCommandOptions input)
    {
        DiameterMessage message = CreateRequestMessage(
            applicationId,
            commandCode,
            true);
        message.Avps.Add(AvpFactory.CreateSessionIdAvp(input.SessionId));
        AddOriginAvps(message, input);
        message.Avps.Add(AvpFactory.CreateDestinationRealmAvp(input.DestinationRealm));
        if (input.DestinationHost != null)
        {
            message.Avps.Add(AvpFactory.CreateDestinationHostAvp(input.DestinationHost));
        }

        message.Avps.Add(AvpFactory.CreateAuthApplicationIdAvp(input.AuthApplicationId));
        return message;
    }

    internal void AddCreditControlReAuthScope(
        DiameterMessage message,
        DiameterBaseCommandOptions input)
    {
        if (input.CommandApplicationId !=
            DiameterApplicationIds.CreditControl)
        {
            if (input.CreditControlSubSessionIdPresent ||
                input.GsuPoolIdentifierPresent ||
                input.ServiceIdentifierPresent ||
                input.RatingGroupPresent)
            {
                throw new ArgumentException(
                    "Credit-control Re-Auth selectors require Application-Id 4.");
            }

            return;
        }

        if (input.AuthApplicationId !=
            DiameterApplicationIds.CreditControl)
        {
            throw new ArgumentException(
                "Credit-control Re-Auth requires Auth-Application-Id 4.");
        }

        if (input.CreditControlSubSessionIdPresent)
        {
            message.Avps.Add(CreateUnsigned64Avp(
                DiameterAvpCodes.CcSubSessionId,
                input.CreditControlSubSessionId));
        }

        if (input.GsuPoolIdentifierPresent)
        {
            message.Avps.Add(CreateUnsigned32Avp(
                DiameterAvpCodes.GsuPoolIdentifier,
                input.GsuPoolIdentifier));
        }

        if (input.ServiceIdentifierPresent)
        {
            message.Avps.Add(CreateUnsigned32Avp(
                DiameterAvpCodes.ServiceIdentifier,
                input.ServiceIdentifier));
        }

        if (input.RatingGroupPresent)
        {
            message.Avps.Add(CreateUnsigned32Avp(
                DiameterAvpCodes.RatingGroup,
                input.RatingGroup));
        }
    }

    internal void CopyCreditControlReAuthScope(
        DiameterMessage request,
        DiameterMessage answer)
    {
        int index = 0;
        while (index < request.Avps.Count)
        {
            DiameterAvp avp = request.Avps[index];
            if (request.ApplicationId ==
                    DiameterApplicationIds.CreditControl &&
                avp.VendorId == DiameterVendorIds.Ietf &&
                (avp.Code == DiameterAvpCodes.CcSubSessionId ||
                avp.Code == DiameterAvpCodes.GsuPoolIdentifier ||
                avp.Code == DiameterAvpCodes.ServiceIdentifier ||
                avp.Code == DiameterAvpCodes.RatingGroup))
            {
                answer.Avps.Add(CloneAvp(avp));
            }

            index = index + 1;
        }
    }

    internal DiameterMessage CreateRequestMessage(
        uint applicationId,
        uint commandCode,
        bool proxyable)
    {
        DiameterMessage message = new DiameterMessage();
        message.Flags = DiameterMessageFlags.Request;
        if (proxyable)
        {
            message.Flags = message.Flags | DiameterMessageFlags.Proxyable;
        }

        message.CommandCode = commandCode;
        message.ApplicationId = applicationId;
        message.HopByHopIdentifier = IdentifierSource.CreateHopByHopIdentifier();
        message.EndToEndIdentifier = IdentifierSource.CreateEndToEndIdentifier();
        return message;
    }

    internal DiameterMessage CreateAnswerMessage(DiameterMessage request, bool proxyable)
    {
        DiameterMessage message = new DiameterMessage();
        message.Version = request.Version;
        if (proxyable)
        {
            message.Flags = DiameterMessageFlags.Proxyable;
        }

        message.CommandCode = request.CommandCode;
        message.ApplicationId = request.ApplicationId;
        message.HopByHopIdentifier = request.HopByHopIdentifier;
        message.EndToEndIdentifier = request.EndToEndIdentifier;
        return message;
    }

    internal void AddCapabilitiesAvps(
        DiameterMessage message,
        DiameterBaseCommandOptions input)
    {
        AddOriginAvps(message, input);
        int addressIndex = 0;
        while (addressIndex < input.HostAddresses.Count)
        {
            message.Avps.Add(CreateAddressAvp(input.HostAddresses[addressIndex]));
            addressIndex = addressIndex + 1;
        }

        message.Avps.Add(CreateUnsigned32Avp(DiameterAvpCodes.VendorId, input.VendorId));
        message.Avps.Add(CreateOptionalTextAvp(DiameterAvpCodes.ProductName, input.ProductName));
        AddOriginStateAvp(message);
        int vendorIndex = 0;
        while (vendorIndex < input.SupportedVendorIds.Count)
        {
            message.Avps.Add(CreateUnsigned32Avp(
                DiameterAvpCodes.SupportedVendorId,
                input.SupportedVendorIds[vendorIndex]));
            vendorIndex = vendorIndex + 1;
        }

        int authIndex = 0;
        while (authIndex < input.AuthApplicationIds.Count)
        {
            message.Avps.Add(AvpFactory.CreateAuthApplicationIdAvp(
                input.AuthApplicationIds[authIndex]));
            authIndex = authIndex + 1;
        }

        int securityIndex = 0;
        while (securityIndex < input.InbandSecurityIds.Count)
        {
            message.Avps.Add(CreateUnsigned32Avp(
                DiameterAvpCodes.InbandSecurityId,
                input.InbandSecurityIds[securityIndex]));
            securityIndex = securityIndex + 1;
        }

        int acctIndex = 0;
        while (acctIndex < input.AcctApplicationIds.Count)
        {
            message.Avps.Add(AvpFactory.CreateAcctApplicationIdAvp(
                input.AcctApplicationIds[acctIndex]));
            acctIndex = acctIndex + 1;
        }

        int vendorApplicationIndex = 0;
        while (vendorApplicationIndex < input.VendorSpecificApplications.Count)
        {
            message.Avps.Add(AvpFactory.CreateVendorSpecificApplicationIdAvp(
                input.VendorSpecificApplications[vendorApplicationIndex]));
            vendorApplicationIndex = vendorApplicationIndex + 1;
        }

        message.Avps.Add(CreateOptionalUnsigned32Avp(
            DiameterAvpCodes.FirmwareRevision,
            input.FirmwareRevision));
    }

    internal void AddOriginAvps(DiameterMessage message, DiameterBaseCommandOptions input)
    {
        message.Avps.Add(AvpFactory.CreateOriginHostAvp(input.OriginHost));
        message.Avps.Add(AvpFactory.CreateOriginRealmAvp(input.OriginRealm));
    }

    internal void AddResultAndOriginAvps(
        DiameterMessage message,
        DiameterBaseCommandOptions input)
    {
        message.Avps.Add(CreateUnsigned32Avp(DiameterAvpCodes.ResultCode, input.ResultCode));
        AddOriginAvps(message, input);
    }

    internal void AddOriginStateAvp(DiameterMessage message)
    {
        message.Avps.Add(AvpFactory.CreateOriginStateIdAvp(
            IdentifierSource.CreateOriginStateIdentifier()));
    }

    internal DiameterBaseCommandOptions ValidateCapabilitiesOptions(
        DiameterBaseCommandOptions? options)
    {
        DiameterBaseCommandOptions input = ValidateOriginOptions(options);
        if (input.HostAddresses == null || input.HostAddresses.Count == 0)
        {
            throw new ArgumentException("At least one Host-IP-Address is required.");
        }

        ValidateText(input.ProductName, "A valid Product-Name is required.");
        if (input.SupportedVendorIds == null || input.AuthApplicationIds == null ||
            input.AcctApplicationIds == null ||
            input.VendorSpecificApplications == null ||
            input.InbandSecurityIds == null)
        {
            throw new ArgumentException("Capability identifier collections cannot be absent.");
        }

        int addressIndex = 0;
        while (addressIndex < input.HostAddresses.Count)
        {
            if (input.HostAddresses[addressIndex] == null)
            {
                throw new ArgumentException("Host-IP-Address values cannot be absent.");
            }

            addressIndex = addressIndex + 1;
        }

        int applicationIndex = 0;
        while (applicationIndex < input.VendorSpecificApplications.Count)
        {
            DiameterApplicationId application =
                input.VendorSpecificApplications[applicationIndex];
            if (application == null || application.VendorId == 0U)
            {
                throw new ArgumentException(
                    "Vendor-specific applications require a nonzero Vendor-Id.");
            }

            applicationIndex = applicationIndex + 1;
        }

        return input;
    }

    internal DiameterBaseCommandOptions ValidateOriginOptions(
        DiameterBaseCommandOptions? options)
    {
        if (options == null)
        {
            throw new ArgumentException("Base command options are required.");
        }

        NormalizeIdentity(options.OriginHost, "A valid Origin-Host is required.");
        NormalizeIdentity(options.OriginRealm, "A valid Origin-Realm is required.");
        if (options.ResultCode == 0U)
        {
            throw new ArgumentException("A nonzero Result-Code is required for an answer.");
        }

        return options;
    }

    internal DiameterBaseCommandOptions ValidateRoutedSessionOptions(
        DiameterBaseCommandOptions? options,
        bool destinationHostRequired)
    {
        DiameterBaseCommandOptions input = ValidateOriginOptions(options);
        ValidateText(input.SessionId, "A valid Session-Id is required.");
        NormalizeIdentity(input.DestinationRealm, "A valid Destination-Realm is required.");
        if (destinationHostRequired || input.DestinationHost != null)
        {
            NormalizeIdentity(input.DestinationHost, "A valid Destination-Host is required.");
        }

        return input;
    }

    internal DiameterBaseCommandOptions ValidateAccountingOptions(
        DiameterBaseCommandOptions? options)
    {
        DiameterBaseCommandOptions input = ValidateRoutedSessionOptions(options, false);
        int recordType = (int)input.AccountingRecordType;
        if (recordType < 1 || recordType > 4)
        {
            throw new ArgumentException("A valid Accounting-Record-Type is required.");
        }

        return input;
    }

    internal DiameterMessage ValidateAnswerRequest(
        DiameterBaseCommandOptions input,
        uint applicationId,
        uint commandCode)
    {
        DiameterMessage? requestValue = input.Request;
        if (requestValue == null)
        {
            throw new ArgumentException("A source request is required to create an answer.");
        }

        DiameterMessage request = requestValue;
        if ((request.Flags & DiameterMessageFlags.Request) == DiameterMessageFlags.None ||
            request.ApplicationId != applicationId || request.CommandCode != commandCode)
        {
            throw new ArgumentException("The source request does not match the answer command.");
        }

        return request;
    }

    internal string NormalizeIdentity(DiameterIdentity? identity, string failureMessage)
    {
        DiameterValueResult normalized = ValueCodecs.NormalizeDiameterIdentity(identity);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(failureMessage);
        }

        return normalized.Text;
    }

    internal void ValidateText(string? value, string failureMessage)
    {
        if (value == null || value.Length == 0 || ValueCodecs.FindInvalidUtf16Index(value) >= 0)
        {
            throw new ArgumentException(failureMessage);
        }
    }

    internal DiameterAvp CreateOptionalTextAvp(uint code, string value)
    {
        ValidateText(value, "A valid UTF8String value is required.");
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.None;
        avp.Data = Encoding.UTF8.GetBytes(value);
        return avp;
    }

    internal DiameterAvp CreateUnsigned32Avp(uint code, uint value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(avp.Data, value);
        return avp;
    }

    internal DiameterAvp CreateUnsigned64Avp(uint code, ulong value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[8];
        NetworkByteOrder.WriteUInt64(avp.Data, value);
        return avp;
    }

    internal DiameterAvp CreateOptionalUnsigned32Avp(uint code, uint value)
    {
        DiameterAvp avp = CreateUnsigned32Avp(code, value);
        avp.Flags = DiameterAvpFlags.None;
        return avp;
    }

    internal DiameterAvp CreateEnumeratedAvp(uint code, int value)
    {
        return CreateUnsigned32Avp(code, (uint)value);
    }

    internal DiameterAvp CreateAddressAvp(IPAddress address)
    {
        byte[] buffer = new byte[18];
        DiameterValueResult encoded = ValueCodecs.EncodeAddress(buffer, address);
        if (encoded.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException("A valid Host-IP-Address is required.");
        }

        DiameterAvp avp = new DiameterAvp();
        avp.Code = DiameterAvpCodes.HostIpAddress;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[encoded.WrittenCount];
        Array.Copy(buffer, avp.Data, encoded.WrittenCount);
        return avp;
    }

    internal DiameterAvp FindAndCloneRequiredAvp(DiameterMessage request, uint code)
    {
        int index = 0;
        while (index < request.Avps.Count)
        {
            DiameterAvp avp = request.Avps[index];
            if (avp.Code == code && avp.VendorId == DiameterVendorIds.Ietf)
            {
                return CloneAvp(avp);
            }

            index = index + 1;
        }

        throw new ArgumentException("The source request is missing an answer correlation AVP.");
    }

    internal DiameterAvp CloneAvp(DiameterAvp source)
    {
        DiameterAvp clone = new DiameterAvp();
        clone.Code = source.Code;
        clone.Flags = source.Flags;
        clone.VendorId = source.VendorId;
        clone.Data = new byte[source.Data.Length];
        Array.Copy(source.Data, clone.Data, source.Data.Length);
        return clone;
    }
}
