namespace sip;

internal static class SipIdentityServiceCore
{
    internal static SipIdentityResult Inspect(SipMessage? message)
    {
        if (message == null)
        {
            return CreateFailure(SipStatusCodes.BadRequest,
                "A SIP message is required.");
        }
        System.Collections.Generic.List<string> privacy =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<SipExtensionHeaderValue> asserted =
            new System.Collections.Generic.List<SipExtensionHeaderValue>();
        System.Collections.Generic.List<SipExtensionHeaderValue> preferred =
            new System.Collections.Generic.List<SipExtensionHeaderValue>();
        System.Collections.Generic.List<SipExtensionHeaderValue> history =
            new System.Collections.Generic.List<SipExtensionHeaderValue>();
        System.Collections.Generic.List<SipExtensionHeaderValue> locations =
            new System.Collections.Generic.List<SipExtensionHeaderValue>();
        System.Collections.Generic.List<SipExtensionHeaderValue> priorities =
            new System.Collections.Generic.List<SipExtensionHeaderValue>();
        System.Collections.Generic.List<SipExtensionHeaderValue> acceptedPriorities =
            new System.Collections.Generic.List<SipExtensionHeaderValue>();
        System.Collections.Generic.List<SipHeader> locationErrors =
            new System.Collections.Generic.List<SipHeader>();
        System.Collections.Generic.List<SipHeader> identityHeaders =
            new System.Collections.Generic.List<SipHeader>();
        System.Collections.Generic.List<string> passports =
            new System.Collections.Generic.List<string>();
        SipExtensionHeaderValue? session = null;
        string? geolocationRouting = null;

        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null)
            {
                SipIdentityHeaderInspection inspected = InspectHeader(
                    header, privacy, asserted, preferred, history,
                    locations, priorities, acceptedPriorities,
                    locationErrors, identityHeaders, passports);
                if (inspected.ErrorMessage != null)
                {
                    return CreateFailure(inspected.StatusCode,
                        inspected.ErrorMessage);
                }
                if (inspected.SessionIdentifier != null)
                {
                    if (session != null)
                    {
                        return CreateFailure(SipStatusCodes.BadRequest,
                            "Session-ID cannot be repeated.");
                    }
                    session = inspected.SessionIdentifier;
                }
                if (inspected.GeolocationRouting != null)
                {
                    if (geolocationRouting != null)
                    {
                        return CreateFailure(SipStatusCodes.BadRequest,
                            "Geolocation-Routing cannot be repeated.");
                    }
                    geolocationRouting = inspected.GeolocationRouting;
                }
            }
            index = index + 1;
        }

        if (!ValidatePrivacyTokens(privacy.ToArray()))
        {
            return CreateFailure(SipStatusCodes.BadRequest,
                "The Privacy field contains duplicates or combines none with another value.");
        }
        if (!ValidateAssertedIdentityFamilies(asserted.ToArray())
            || !ValidateAssertedIdentityFamilies(preferred.ToArray()))
        {
            return CreateFailure(SipStatusCodes.BadRequest,
                "Asserted and preferred identity fields permit at most one SIP identity and one telephone identity.");
        }
        if (!ValidateHistoryInformation(history.ToArray()))
        {
            return CreateFailure(SipStatusCodes.BadRequest,
                "Each History-Info value requires a valid index parameter.");
        }

        SipIdentityMetadata metadata = new SipIdentityMetadata();
        metadata.Privacy = privacy.ToArray();
        metadata.AssertedIdentities = CopyExtensionValues(asserted.ToArray());
        metadata.PreferredIdentities = CopyExtensionValues(preferred.ToArray());
        metadata.ConnectedIdentity = SipSnapshotCopy.CopyNameAddressModel(
            message.Kind == SipMessageKind.Request ? message.From : message.To);
        metadata.HistoryInformation = CopyExtensionValues(history.ToArray());
        metadata.SessionIdentifier = CopySingleExtensionValue(session);
        metadata.Geolocations = CopyExtensionValues(locations.ToArray());
        metadata.GeolocationRouting = geolocationRouting;
        metadata.GeolocationErrors = CopyHeaders(locationErrors.ToArray());
        metadata.ResourcePriorities = CopyExtensionValues(priorities.ToArray());
        metadata.AcceptedResourcePriorities = CopyExtensionValues(
            acceptedPriorities.ToArray());
        metadata.IdentityHeaders = CopyHeaders(identityHeaders.ToArray());
        metadata.Passports = passports.ToArray();

        SipIdentityResult result = new SipIdentityResult();
        result.Success = true;
        result.StatusCode = SipStatusCodes.Ok;
        result.Metadata = metadata;
        return result;
    }

    internal static SipIdentityResult AddHeaders(
        SipMessage? message, SipIdentityMetadata? metadata)
    {
        if (message == null || metadata == null)
        {
            return CreateFailure(SipStatusCodes.BadRequest,
                "A message and identity metadata are required.");
        }
        if (!MetadataArraysArePresent(metadata)
            || !ValidatePrivacyTokens(metadata.Privacy)
            || !ValidateAssertedIdentityFamilies(metadata.AssertedIdentities)
            || !ValidateAssertedIdentityFamilies(metadata.PreferredIdentities)
            || !ValidateHistoryInformation(metadata.HistoryInformation)
            || !ValidateGeolocationRouting(metadata.GeolocationRouting))
        {
            return CreateFailure(SipStatusCodes.BadRequest,
                "The identity metadata is invalid.");
        }

        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(message);
        if (copy == null)
        {
            return CreateFailure(SipStatusCodes.BadRequest,
                "The SIP message could not be copied.");
        }
        if (metadata.Privacy.Length > 0)
        {
            SipExtensionHeaderValue[] values =
                CreateTokenValues(metadata.Privacy);
            string? text = FormatExtensionFamily(
                SipHeaderNames.Privacy, values);
            if (text == null) return InvalidMetadataFailure();
            InsertBeforeContentLength(copy, CreateHeader(
                SipHeaderNames.Privacy, text));
        }
        if (!AddExtensionFamily(copy, SipHeaderNames.PAssertedIdentity,
                metadata.AssertedIdentities)) return InvalidMetadataFailure();
        if (!AddExtensionFamily(copy, SipHeaderNames.PPreferredIdentity,
                metadata.PreferredIdentities)) return InvalidMetadataFailure();
        if (!AddExtensionFamily(copy, SipHeaderNames.HistoryInfo,
                metadata.HistoryInformation)) return InvalidMetadataFailure();
        if (metadata.SessionIdentifier != null
            && !AddExtensionFamily(copy, SipHeaderNames.SessionId,
                new SipExtensionHeaderValue[] {
                    metadata.SessionIdentifier })) return InvalidMetadataFailure();
        if (!AddExtensionFamily(copy, SipHeaderNames.Geolocation,
                metadata.Geolocations)) return InvalidMetadataFailure();
        if (metadata.GeolocationRouting != null)
            InsertBeforeContentLength(copy, CreateHeader(
                SipHeaderNames.GeolocationRouting,
                metadata.GeolocationRouting));
        int locationErrorIndex = 0;
        while (locationErrorIndex < metadata.GeolocationErrors.Length)
        {
            SipHeader? header = metadata.GeolocationErrors[locationErrorIndex];
            if (!ValidateNamedOpaqueHeader(
                    header, SipHeaderNames.GeolocationError))
                return InvalidMetadataFailure();
            InsertBeforeContentLength(copy, CopyHeader(header));
            locationErrorIndex = locationErrorIndex + 1;
        }
        if (!AddExtensionFamily(copy, SipHeaderNames.ResourcePriority,
                metadata.ResourcePriorities)) return InvalidMetadataFailure();
        if (!AddExtensionFamilyWithCodec(copy,
                SipHeaderNames.AcceptResourcePriority,
                SipHeaderNames.ResourcePriority,
                metadata.AcceptedResourcePriorities))
            return InvalidMetadataFailure();

        int headerIndex = 0;
        while (headerIndex < metadata.IdentityHeaders.Length)
        {
            SipHeader? header = metadata.IdentityHeaders[headerIndex];
            if (!ValidateOpaqueIdentityHeader(header))
                return InvalidMetadataFailure();
            InsertBeforeContentLength(copy, CopyHeader(header));
            headerIndex = headerIndex + 1;
        }
        SipIdentityResult inspected = Inspect(copy);
        if (!inspected.Success) return inspected;
        inspected.Message = copy;
        return inspected;
    }

    internal static SipIdentityResult ApplyPrivacy(
        SipMessage? message, bool trustedDestination)
    {
        SipIdentityResult inspected = Inspect(message);
        if (!inspected.Success || inspected.Metadata == null
            || message == null) return inspected;
        if (HasUnsupportedCriticalPrivacy(inspected.Metadata.Privacy))
        {
            return CreateFailure(SipStatusCodes.ServerInternalError,
                "A critical Privacy extension is not supported.");
        }
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(message);
        if (copy == null)
        {
            return CreateFailure(SipStatusCodes.BadRequest,
                "The SIP message could not be copied.");
        }
        bool none = ContainsToken(inspected.Metadata.Privacy, "none");
        bool removeAsserted = !trustedDestination && !none
            && (ContainsToken(inspected.Metadata.Privacy, "id")
                || ContainsToken(inspected.Metadata.Privacy, "header")
                || ContainsToken(inspected.Metadata.Privacy, "user"));
        bool removeIdentity = !trustedDestination && !none
            && (ContainsToken(inspected.Metadata.Privacy, "header")
                || ContainsToken(inspected.Metadata.Privacy, "user"));
        bool removeHistory = !trustedDestination && !none
            && (ContainsToken(inspected.Metadata.Privacy, "history")
                || ContainsToken(inspected.Metadata.Privacy, "header"));

        System.Collections.Generic.List<SipHeader> retained =
            new System.Collections.Generic.List<SipHeader>();
        int index = 0;
        while (index < copy.Headers.Length)
        {
            SipHeader header = copy.Headers[index];
            bool remove = IsPreferredIdentityName(header.Name)
                || removeAsserted && IsAssertedIdentityName(header.Name)
                || removeIdentity && IsOpaqueIdentityName(header.Name)
                || removeHistory && SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.HistoryInfo);
            if (!remove) retained.Add(header);
            index = index + 1;
        }
        copy.Headers = retained.ToArray();
        copy.OriginalBytes = new byte[0];
        SipIdentityResult filtered = Inspect(copy);
        if (!filtered.Success) return filtered;
        filtered.Message = copy;
        return filtered;
    }

    internal static SipIdentityResult UpdateConnectedIdentity(
        SipMessage? message, SipNameAddress? identity)
    {
        if (message == null || identity == null || identity.Uri == null
            || SipUriValidator.ValidateUriModel(identity.Uri).Length > 0)
        {
            return CreateFailure(SipStatusCodes.BadRequest,
                "A message and valid connected identity are required.");
        }
        SipNameAddress? current = message.Kind == SipMessageKind.Request
            ? message.From : message.To;
        if (current == null)
        {
            return CreateFailure(SipStatusCodes.BadRequest,
                "The message has no identity address to update.");
        }
        SipNameAddress? updated = SipSnapshotCopy.CopyNameAddressModel(identity);
        if (updated == null)
        {
            return CreateFailure(SipStatusCodes.BadRequest,
                "The connected identity could not be copied.");
        }
        updated.Parameters = CopyParameters(current.Parameters);
        string name = message.Kind == SipMessageKind.Request
            ? SipHeaderNames.From : SipHeaderNames.To;
        SipStructuredHeaderResult structured = new SipStructuredHeaderResult();
        structured.Success = true;
        structured.NameAddress = updated;
        SipWriteResult formatted =
            SipStructuredHeaderCodec.FormatStructuredHeaderValue(
                name, structured, new SipWriteOptions());
        if (!formatted.Success)
        {
            return CreateFailure(SipStatusCodes.BadRequest,
                formatted.ErrorMessage == null
                    ? "The connected identity is invalid."
                    : formatted.ErrorMessage);
        }
        string? value = ExtractFormattedValue(formatted.Bytes);
        if (value == null)
        {
            return CreateFailure(SipStatusCodes.BadRequest,
                "The connected identity could not be formatted.");
        }
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(message);
        if (copy == null)
        {
            return CreateFailure(SipStatusCodes.BadRequest,
                "The SIP message could not be copied.");
        }
        ReplaceNamedHeader(copy, name, value);
        if (message.Kind == SipMessageKind.Request)
            copy.From = updated;
        else
            copy.To = updated;
        SipIdentityResult result = Inspect(copy);
        if (!result.Success) return result;
        result.Message = copy;
        return result;
    }

    internal static SipIdentityVerificationResult Verify(
        SipMessage? message, ISipIdentityVerificationPolicy? policy)
    {
        if (message == null || message.Kind != SipMessageKind.Request)
        {
            return CreateVerificationFailure(
                "SIP Identity verification requires a request.");
        }
        if (policy == null)
        {
            return CreateVerificationFailure(
                "An identity verification policy is required.");
        }
        SipIdentityResult inspected = Inspect(message);
        if (!inspected.Success || inspected.Metadata == null)
        {
            return CreateVerificationFailure(inspected.ErrorMessage == null
                ? "The message identity metadata is invalid."
                : inspected.ErrorMessage);
        }
        SipIdentityVerificationRequest request =
            new SipIdentityVerificationRequest();
        request.Message = SipSnapshotCopy.CopyMessageModel(message);
        request.Metadata = CopyMetadata(inspected.Metadata);
        request.IdentityHeaders = CopyHeaders(
            inspected.Metadata.IdentityHeaders);
        request.Passports = CopyStrings(inspected.Metadata.Passports);
        SipIdentityVerificationResult? decision =
            policy.VerifyIdentityAssertion(request);
        if (decision == null)
        {
            return CreateVerificationFailure(
                "The identity verification policy returned no decision.");
        }
        SipIdentityVerificationResult result =
            new SipIdentityVerificationResult();
        result.PolicyEvaluated = true;
        result.Accepted = decision.Accepted;
        result.Verified = decision.Verified;
        result.Identity = decision.Identity;
        result.StatusCode = decision.StatusCode;
        result.Reason = decision.Reason;
        result.Metadata = CopyMetadata(inspected.Metadata);
        return result;
    }

    static SipIdentityHeaderInspection InspectHeader(SipHeader header,
        System.Collections.Generic.List<string> privacy,
        System.Collections.Generic.List<SipExtensionHeaderValue> asserted,
        System.Collections.Generic.List<SipExtensionHeaderValue> preferred,
        System.Collections.Generic.List<SipExtensionHeaderValue> history,
        System.Collections.Generic.List<SipExtensionHeaderValue> locations,
        System.Collections.Generic.List<SipExtensionHeaderValue> priorities,
        System.Collections.Generic.List<SipExtensionHeaderValue> acceptedPriorities,
        System.Collections.Generic.List<SipHeader> locationErrors,
        System.Collections.Generic.List<SipHeader> identityHeaders,
        System.Collections.Generic.List<string> passports)
    {
        SipIdentityHeaderInspection result =
            new SipIdentityHeaderInspection();
        string? name = header.Name;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Privacy))
        {
            SipExtensionHeaderResult parsed = ParseExtension(
                SipHeaderNames.Privacy, header.Value);
            if (!parsed.Success) return HeaderFailure(name, parsed.ErrorMessage);
            AddTokens(privacy, parsed.Values);
        }
        else if (IsAssertedIdentityName(name))
        {
            SipExtensionHeaderResult parsed = ParseExtension(
                SipHeaderNames.PAssertedIdentity, header.Value);
            if (!parsed.Success) return HeaderFailure(name, parsed.ErrorMessage);
            AddExtensionValues(asserted, parsed.Values);
        }
        else if (IsPreferredIdentityName(name))
        {
            SipExtensionHeaderResult parsed = ParseExtension(
                SipHeaderNames.PPreferredIdentity, header.Value);
            if (!parsed.Success) return HeaderFailure(name, parsed.ErrorMessage);
            AddExtensionValues(preferred, parsed.Values);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.HistoryInfo))
        {
            SipExtensionHeaderResult parsed = ParseExtension(
                SipHeaderNames.HistoryInfo, header.Value);
            if (!parsed.Success) return HeaderFailure(name, parsed.ErrorMessage);
            AddExtensionValues(history, parsed.Values);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.SessionId))
        {
            SipExtensionHeaderResult parsed = ParseExtension(
                SipHeaderNames.SessionId, header.Value);
            if (!parsed.Success || parsed.Values.Length != 1
                || !ValidateSessionIdentifier(parsed.Values[0]))
                return HeaderFailure(name, parsed.ErrorMessage);
            result.SessionIdentifier = CopySingleExtensionValue(
                parsed.Values[0]);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Geolocation))
        {
            SipExtensionHeaderResult parsed = ParseExtension(
                SipHeaderNames.Geolocation, header.Value);
            if (!parsed.Success) return HeaderFailure(name, parsed.ErrorMessage);
            AddExtensionValues(locations, parsed.Values);
        }
        else if (SipAscii.EqualsIgnoreCase(
            name, SipHeaderNames.GeolocationRouting))
        {
            if (!ValidateGeolocationRouting(header.Value))
                return HeaderFailure(name, "The field value is not a token.");
            result.GeolocationRouting = header.Value == null
                ? null : header.Value.Trim();
        }
        else if (SipAscii.EqualsIgnoreCase(
            name, SipHeaderNames.GeolocationError))
        {
            if (!ValidateNamedOpaqueHeader(
                    header, SipHeaderNames.GeolocationError))
                return HeaderFailure(name, "The field value is empty or unsafe.");
            locationErrors.Add(CopyHeader(header));
        }
        else if (SipAscii.EqualsIgnoreCase(
            name, SipHeaderNames.ResourcePriority))
        {
            SipExtensionHeaderResult parsed = ParseExtension(
                SipHeaderNames.ResourcePriority, header.Value);
            if (!parsed.Success) return HeaderFailure(name, parsed.ErrorMessage);
            AddExtensionValues(priorities, parsed.Values);
        }
        else if (SipAscii.EqualsIgnoreCase(
            name, SipHeaderNames.AcceptResourcePriority))
        {
            SipExtensionHeaderResult parsed = ParseExtension(
                SipHeaderNames.ResourcePriority, header.Value);
            if (!parsed.Success) return HeaderFailure(name, parsed.ErrorMessage);
            AddExtensionValues(acceptedPriorities, parsed.Values);
        }
        else if (IsOpaqueIdentityName(name))
        {
            if (!ValidateOpaqueIdentityHeader(header))
                return HeaderFailure(name, "The field value is empty or unsafe.");
            identityHeaders.Add(CopyHeader(header));
            if (IsIdentityName(name))
            {
                string? passport = ExtractPassport(header.Value);
                if (passport != null) passports.Add(passport);
            }
        }
        return result;
    }

    static SipExtensionHeaderResult ParseExtension(
        string name, string? value)
    {
        return SipExtensionHeaderCodec.ParseExtensionHeaderValue(
            name, value, new SipParseOptions());
    }

    static SipIdentityHeaderInspection HeaderFailure(
        string? name, string? message)
    {
        SipIdentityHeaderInspection result =
            new SipIdentityHeaderInspection();
        result.StatusCode = IsIdentityName(name)
            ? SipStatusCodes.InvalidIdentityHeader
            : SipStatusCodes.BadRequest;
        result.ErrorMessage = (name == null
            ? "An identity header" : name) + " is invalid: "
            + (message == null ? "the field value is malformed." : message);
        return result;
    }

    static bool MetadataArraysArePresent(SipIdentityMetadata metadata)
    {
        return metadata.Privacy != null
            && metadata.AssertedIdentities != null
            && metadata.PreferredIdentities != null
            && metadata.HistoryInformation != null
            && metadata.Geolocations != null
            && metadata.GeolocationErrors != null
            && metadata.ResourcePriorities != null
            && metadata.AcceptedResourcePriorities != null
            && metadata.IdentityHeaders != null
            && metadata.Passports != null;
    }

    static bool ValidatePrivacyTokens(string[] tokens)
    {
        bool none = false;
        int index = 0;
        while (index < tokens.Length)
        {
            if (!SipAscii.IsToken(tokens[index])) return false;
            int prior = 0;
            while (prior < index)
            {
                if (SipAscii.EqualsIgnoreCase(tokens[prior], tokens[index]))
                    return false;
                prior = prior + 1;
            }
            if (SipAscii.EqualsIgnoreCase(tokens[index], "none")) none = true;
            index = index + 1;
        }
        return !none || tokens.Length == 1;
    }

    static bool ValidateAssertedIdentityFamilies(
        SipExtensionHeaderValue[] values)
    {
        if (values == null || values.Length > 2) return false;
        bool sip = false;
        bool telephone = false;
        int index = 0;
        while (index < values.Length)
        {
            SipExtensionHeaderValue? value = values[index];
            SipUri? uri = value == null ? null : value.Uri;
            if (value == null || value.Address == null || uri == null
                || SipUriValidator.ValidateUriModel(uri).Length > 0)
                return false;
            if (uri.Scheme == SipUriScheme.Sip
                || uri.Scheme == SipUriScheme.Sips)
            {
                if (sip) return false;
                sip = true;
            }
            else if (uri.Scheme == SipUriScheme.Tel)
            {
                if (telephone) return false;
                telephone = true;
            }
            else
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool ValidateHistoryInformation(
        SipExtensionHeaderValue[] values)
    {
        if (values == null) return false;
        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            SipExtensionHeaderValue? value = values[valueIndex];
            if (value == null || value.Parameters == null) return false;
            int indexCount = 0;
            int parameterIndex = 0;
            while (parameterIndex < value.Parameters.Length)
            {
                SipUriParameter? parameter = value.Parameters[parameterIndex];
                if (parameter != null && SipAscii.EqualsIgnoreCase(
                    parameter.Name, "index"))
                {
                    indexCount = indexCount + 1;
                    if (!parameter.HasValue
                        || !IsHistoryIndex(parameter.Value)) return false;
                }
                parameterIndex = parameterIndex + 1;
            }
            if (indexCount != 1) return false;
            valueIndex = valueIndex + 1;
        }
        return true;
    }

    static bool IsHistoryIndex(string? value)
    {
        if (value == null || value.Length == 0 || value[0] != '1')
            return false;
        int index = 1;
        while (index < value.Length)
        {
            if (value[index] != '.' || index + 1 >= value.Length
                || value[index + 1] < '1' || value[index + 1] > '9')
                return false;
            index = index + 2;
            while (index < value.Length && value[index] >= '0'
                && value[index] <= '9') index = index + 1;
        }
        return true;
    }

    static bool ValidateGeolocationRouting(string? value)
    {
        return value == null || SipAscii.IsToken(value.Trim());
    }

    static bool ValidateSessionIdentifier(SipExtensionHeaderValue value)
    {
        if (!IsSessionUuid(value.Token)) return false;
        int remoteCount = 0;
        int index = 0;
        while (index < value.Parameters.Length)
        {
            SipUriParameter parameter = value.Parameters[index];
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "remote"))
            {
                remoteCount = remoteCount + 1;
                if (!parameter.HasValue || !IsSessionUuid(parameter.Value))
                    return false;
            }
            index = index + 1;
        }
        return remoteCount <= 1;
    }

    static bool IsSessionUuid(string? value)
    {
        if (value == null || value.Length != 32) return false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (!(character >= '0' && character <= '9')
                && !(character >= 'a' && character <= 'f')
                && !(character >= 'A' && character <= 'F')) return false;
            index = index + 1;
        }
        return true;
    }

    static bool AddExtensionFamily(
        SipMessage message, string name, SipExtensionHeaderValue[] values)
    {
        if (values == null) return false;
        if (values.Length == 0) return true;
        if (name == SipHeaderNames.SessionId
            && (values.Length != 1
                || !ValidateSessionIdentifier(values[0]))) return false;
        string? text = FormatExtensionFamily(name, values);
        if (text == null) return false;
        InsertBeforeContentLength(message, CreateHeader(name, text));
        return true;
    }

    static bool AddExtensionFamilyWithCodec(SipMessage message,
        string name, string codecName, SipExtensionHeaderValue[] values)
    {
        if (values == null) return false;
        if (values.Length == 0) return true;
        string? text = FormatExtensionFamily(codecName, values);
        if (text == null) return false;
        InsertBeforeContentLength(message, CreateHeader(name, text));
        return true;
    }

    static string? FormatExtensionFamily(
        string name, SipExtensionHeaderValue[] values)
    {
        if (!ValidateExtensionModels(name, values)) return null;
        SipExtensionHeaderResult parsed = new SipExtensionHeaderResult();
        parsed.Success = true;
        parsed.Values = CopyExtensionValues(values);
        SipWriteResult formatted =
            SipExtensionHeaderCodec.FormatExtensionHeaderValue(
                name, parsed, new SipWriteOptions());
        if (!formatted.Success) return null;
        string? value = ExtractFormattedValue(formatted.Bytes);
        if (value == null) return null;
        SipExtensionHeaderResult verified = ParseExtension(name, value);
        return verified.Success ? value : null;
    }

    static bool ValidateExtensionModels(
        string name, SipExtensionHeaderValue[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            SipExtensionHeaderValue? value = values[index];
            if (value == null || value.Parameters == null) return false;
            bool addressFamily = name == SipHeaderNames.PAssertedIdentity
                || name == SipHeaderNames.PPreferredIdentity
                || name == SipHeaderNames.HistoryInfo
                || name == SipHeaderNames.Geolocation;
            if (addressFamily)
            {
                if (!ValidateAddressValue(value)) return false;
            }
            else if (!SipAscii.IsToken(value.Token))
            {
                return false;
            }
            int parameterIndex = 0;
            while (parameterIndex < value.Parameters.Length)
            {
                SipUriParameter? parameter = value.Parameters[parameterIndex];
                if (parameter == null || !SipAscii.IsToken(parameter.Name)
                    || parameter.HasValue
                        && (parameter.Value == null
                            || parameter.Value.Length == 0)) return false;
                parameterIndex = parameterIndex + 1;
            }
            index = index + 1;
        }
        return true;
    }

    static bool ValidateAddressValue(SipExtensionHeaderValue value)
    {
        SipNameAddress? address = value.Address;
        if (address == null || address.Uri == null
            || SipUriValidator.ValidateUriModel(address.Uri).Length > 0
            || address.Parameters == null) return false;
        int index = 0;
        while (index < address.Parameters.Length)
        {
            SipUriParameter? parameter = address.Parameters[index];
            if (parameter == null || !SipAscii.IsToken(parameter.Name)
                || parameter.HasValue
                    && (parameter.Value == null
                        || parameter.Value.Length == 0)) return false;
            index = index + 1;
        }
        return true;
    }

    static bool ValidateOpaqueIdentityHeader(SipHeader? header)
    {
        if (header == null || !IsOpaqueIdentityName(header.Name)
            || header.Value == null || header.Value.Trim().Length == 0)
            return false;
        int index = 0;
        while (index < header.Value.Length)
        {
            if (header.Value[index] == '\r' || header.Value[index] == '\n')
                return false;
            index = index + 1;
        }
        return true;
    }

    static bool ValidateNamedOpaqueHeader(
        SipHeader? header, string expectedName)
    {
        if (header == null
            || !SipAscii.EqualsIgnoreCase(header.Name, expectedName)
            || header.Value == null || header.Value.Trim().Length == 0)
            return false;
        int index = 0;
        while (index < header.Value.Length)
        {
            if (header.Value[index] == '\r' || header.Value[index] == '\n')
                return false;
            index = index + 1;
        }
        return true;
    }

    static string? ExtractPassport(string? value)
    {
        if (value == null) return null;
        int semicolon = value.IndexOf(';');
        string candidate = (semicolon < 0
            ? value : value.Substring(0, semicolon)).Trim();
        int dots = 0;
        int firstDot = -1;
        int secondDot = -1;
        int index = 0;
        while (index < candidate.Length)
        {
            char character = candidate[index];
            if (character == '.')
            {
                dots = dots + 1;
                if (firstDot < 0) firstDot = index;
                else if (secondDot < 0) secondDot = index;
            }
            else if (!(character >= 'a' && character <= 'z')
                && !(character >= 'A' && character <= 'Z')
                && !(character >= '0' && character <= '9')
                && character != '-' && character != '_'
                && character != '+' && character != '/'
                && character != '=') return null;
            index = index + 1;
        }
        if (candidate.Length < 3 || dots != 2
            || secondDot == candidate.Length - 1) return null;
        bool compact = firstDot == 0 && secondDot == 1;
        bool full = firstDot > 0 && secondDot > firstDot + 1;
        return compact || full ? candidate : null;
    }

    static void AddTokens(System.Collections.Generic.List<string> target,
        SipExtensionHeaderValue[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            string? token = values[index].Token;
            if (token != null) target.Add(token);
            index = index + 1;
        }
    }

    static void AddExtensionValues(
        System.Collections.Generic.List<SipExtensionHeaderValue> target,
        SipExtensionHeaderValue[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            SipExtensionHeaderValue? copied = CopySingleExtensionValue(
                values[index]);
            if (copied != null) target.Add(copied);
            index = index + 1;
        }
    }

    static SipExtensionHeaderValue[] CreateTokenValues(string[] tokens)
    {
        SipExtensionHeaderValue[] values =
            new SipExtensionHeaderValue[tokens.Length];
        int index = 0;
        while (index < tokens.Length)
        {
            values[index] = new SipExtensionHeaderValue();
            values[index].Token = tokens[index];
            index = index + 1;
        }
        return values;
    }

    static bool ContainsToken(string[] values, string expected)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (SipAscii.EqualsIgnoreCase(values[index], expected)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool HasUnsupportedCriticalPrivacy(string[] values)
    {
        if (!ContainsToken(values, "critical")) return false;
        int index = 0;
        while (index < values.Length)
        {
            string value = values[index];
            bool known = SipAscii.EqualsIgnoreCase(value, "critical")
                || SipAscii.EqualsIgnoreCase(value, "none")
                || SipAscii.EqualsIgnoreCase(value, "user")
                || SipAscii.EqualsIgnoreCase(value, "header")
                || SipAscii.EqualsIgnoreCase(value, "session")
                || SipAscii.EqualsIgnoreCase(value, "id")
                || SipAscii.EqualsIgnoreCase(value, "history");
            if (!known) return true;
            index = index + 1;
        }
        return false;
    }

    static void InsertBeforeContentLength(
        SipMessage message, SipHeader added)
    {
        int insertion = message.Headers.Length;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (insertion == message.Headers.Length && header != null
                && (SipAscii.EqualsIgnoreCase(
                        header.Name, SipHeaderNames.ContentLength)
                    || SipAscii.EqualsIgnoreCase(
                        header.Name, SipHeaderNames.CompactContentLength)))
                insertion = index;
            index = index + 1;
        }
        SipHeader[] headers = new SipHeader[message.Headers.Length + 1];
        index = 0;
        while (index < insertion)
        {
            headers[index] = message.Headers[index];
            index = index + 1;
        }
        headers[insertion] = added;
        while (index < message.Headers.Length)
        {
            headers[index + 1] = message.Headers[index];
            index = index + 1;
        }
        message.Headers = headers;
        message.OriginalBytes = new byte[0];
    }

    static void ReplaceNamedHeader(
        SipMessage message, string name, string value)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (SipGenericValueAccess.HeaderNamesMatch(header.Name, name))
            {
                header.Name = name;
                header.Value = value;
                header.OriginalBytes = new byte[0];
                message.OriginalBytes = new byte[0];
                return;
            }
            index = index + 1;
        }
    }

    static SipHeader CreateHeader(string name, string value)
    {
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = value;
        return header;
    }

    static SipHeader CopyHeader(SipHeader source)
    {
        SipHeader copy = new SipHeader();
        copy.Name = source.Name;
        copy.Value = source.Value;
        copy.OriginalBytes = CopyBytes(source.OriginalBytes);
        return copy;
    }

    static SipHeader[] CopyHeaders(SipHeader[] source)
    {
        SipHeader[] copies = new SipHeader[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copies[index] = CopyHeader(source[index]);
            index = index + 1;
        }
        return copies;
    }

    static SipExtensionHeaderValue[] CopyExtensionValues(
        SipExtensionHeaderValue[] source)
    {
        return SipSnapshotCopy.CopyExtensionHeaderValues(source);
    }

    static SipExtensionHeaderValue? CopySingleExtensionValue(
        SipExtensionHeaderValue? source)
    {
        if (source == null) return null;
        SipExtensionHeaderValue[] copied = CopyExtensionValues(
            new SipExtensionHeaderValue[] { source });
        return copied[0];
    }

    static SipIdentityMetadata CopyMetadata(SipIdentityMetadata source)
    {
        SipIdentityMetadata copy = new SipIdentityMetadata();
        copy.Privacy = CopyStrings(source.Privacy);
        copy.AssertedIdentities = CopyExtensionValues(
            source.AssertedIdentities);
        copy.PreferredIdentities = CopyExtensionValues(
            source.PreferredIdentities);
        copy.ConnectedIdentity = SipSnapshotCopy.CopyNameAddressModel(
            source.ConnectedIdentity);
        copy.HistoryInformation = CopyExtensionValues(
            source.HistoryInformation);
        copy.SessionIdentifier = CopySingleExtensionValue(
            source.SessionIdentifier);
        copy.Geolocations = CopyExtensionValues(source.Geolocations);
        copy.GeolocationRouting = source.GeolocationRouting;
        copy.GeolocationErrors = CopyHeaders(source.GeolocationErrors);
        copy.ResourcePriorities = CopyExtensionValues(
            source.ResourcePriorities);
        copy.AcceptedResourcePriorities = CopyExtensionValues(
            source.AcceptedResourcePriorities);
        copy.IdentityHeaders = CopyHeaders(source.IdentityHeaders);
        copy.Passports = CopyStrings(source.Passports);
        return copy;
    }

    static SipUriParameter[] CopyParameters(SipUriParameter[] source)
    {
        SipUriParameter[] copy = new SipUriParameter[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipUriParameter parameter = new SipUriParameter();
            parameter.Name = source[index].Name;
            parameter.Value = source[index].Value;
            parameter.HasValue = source[index].HasValue;
            parameter.OriginalText = source[index].OriginalText;
            parameter.Quoted = source[index].Quoted;
            copy[index] = parameter;
            index = index + 1;
        }
        return copy;
    }

    static string[] CopyStrings(string[] source)
    {
        string[] copy = new string[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }

    static byte[] CopyBytes(byte[]? source)
    {
        if (source == null) return new byte[0];
        byte[] copy = new byte[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }

    static string? ExtractFormattedValue(byte[] bytes)
    {
        string text = System.Text.Encoding.UTF8.GetString(bytes);
        int colon = text.IndexOf(':');
        if (colon < 0) return null;
        return text.Substring(colon + 1).Trim(' ', '\t', '\r', '\n');
    }

    static bool IsIdentityName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Identity)
            || SipAscii.EqualsIgnoreCase(
                name, SipHeaderNames.CompactIdentity);
    }

    static bool IsOpaqueIdentityName(string? name)
    {
        return IsIdentityName(name)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.IdentityInfo);
    }

    static bool IsAssertedIdentityName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(
            name, SipHeaderNames.PAssertedIdentity);
    }

    static bool IsPreferredIdentityName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(
            name, SipHeaderNames.PPreferredIdentity);
    }

    static SipIdentityResult InvalidMetadataFailure()
    {
        return CreateFailure(SipStatusCodes.BadRequest,
            "The identity metadata cannot be formatted safely.");
    }

    static SipIdentityResult CreateFailure(int statusCode, string message)
    {
        SipIdentityResult result = new SipIdentityResult();
        result.StatusCode = statusCode;
        result.ErrorMessage = message;
        return result;
    }

    static SipIdentityVerificationResult CreateVerificationFailure(
        string message)
    {
        SipIdentityVerificationResult result =
            new SipIdentityVerificationResult();
        result.StatusCode = SipStatusCodes.InvalidIdentityHeader;
        result.Reason = message;
        return result;
    }
}

internal class SipIdentityHeaderInspection
{
    internal SipExtensionHeaderValue? SessionIdentifier;
    internal string? GeolocationRouting;
    internal int StatusCode = SipStatusCodes.BadRequest;
    internal string? ErrorMessage;
}
