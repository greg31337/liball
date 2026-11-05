namespace sip;

internal static class SipCapabilityNegotiationCore
{
    internal static SipCapabilityNegotiation Inspect(SipMessage? message)
    {
        if (message == null)
        {
            return CreateInspectionFailure("A SIP message is required.");
        }

        System.Collections.Generic.List<string> methods =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> supported =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> required =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> proxyRequired =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> unsupported =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> mediaTypes =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> eventPackages =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<SipExtensionHeaderValue> accept =
            new System.Collections.Generic.List<SipExtensionHeaderValue>();
        System.Collections.Generic.List<SipExtensionHeaderValue> reject =
            new System.Collections.Generic.List<SipExtensionHeaderValue>();
        System.Collections.Generic.List<string> dispositions =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<SipExtensionHeaderValue> featureCaps =
            new System.Collections.Generic.List<SipExtensionHeaderValue>();

        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null)
            {
                string? error = InspectHeader(header, methods, supported,
                    required, proxyRequired, unsupported, mediaTypes,
                    eventPackages, accept, reject, dispositions, featureCaps);
                if (error != null)
                {
                    return CreateInspectionFailure(error);
                }
            }
            index = index + 1;
        }

        SipCapabilitySet remote = new SipCapabilitySet();
        remote.Methods = methods.ToArray();
        remote.OptionTags = supported.ToArray();
        remote.MediaTypes = mediaTypes.ToArray();
        remote.EventPackages = eventPackages.ToArray();
        remote = SipExtensionRegistryCore.NormalizeCapabilities(remote);

        SipCallerPreferences preferences = new SipCallerPreferences();
        preferences.AcceptContacts = SipSnapshotCopy.CopyExtensionHeaderValues(
            accept.ToArray());
        preferences.RejectContacts = SipSnapshotCopy.CopyExtensionHeaderValues(
            reject.ToArray());
        preferences.RequestDispositions = dispositions.ToArray();

        SipCapabilityNegotiation result = new SipCapabilityNegotiation();
        result.Compatible = true;
        result.StatusCode = SipStatusCodes.Ok;
        result.RemoteCapabilities = remote;
        result.NegotiatedCapabilities = new SipCapabilitySet();
        result.RequiredOptionTags = required.ToArray();
        result.ProxyRequiredOptionTags = proxyRequired.ToArray();
        result.UnsupportedOptionTags = unsupported.ToArray();
        result.CallerPreferences = preferences;
        result.FeatureCapabilities =
            SipSnapshotCopy.CopyExtensionHeaderValues(featureCaps.ToArray());
        return result;
    }

    internal static SipCapabilityNegotiation Negotiate(
        SipMessage? message, SipCapabilitySet? localCapabilities)
    {
        SipCapabilityNegotiation result = Inspect(message);
        if (!result.Compatible || message == null)
        {
            return result;
        }
        if (localCapabilities == null)
        {
            return SetNegotiationFailure(result, SipStatusCodes.BadRequest,
                "Local capabilities are required.");
        }

        SipCapabilitySet local =
            SipExtensionRegistryCore.NormalizeCapabilities(localCapabilities);
        SipCapabilitySet remote = result.RemoteCapabilities == null
            ? new SipCapabilitySet() : result.RemoteCapabilities;
        SipCapabilitySet mutual = new SipCapabilitySet();
        mutual.Methods = IntersectMethods(local.Methods, remote.Methods);
        mutual.OptionTags = IntersectTokens(
            local.OptionTags, remote.OptionTags);
        mutual.MediaTypes = IntersectMediaTypes(
            local.MediaTypes, remote.MediaTypes);
        mutual.EventPackages = IntersectTokens(
            local.EventPackages, remote.EventPackages);
        mutual.HeaderNames = IntersectHeaderNames(
            local.HeaderNames, remote.HeaderNames);
        mutual.UriSchemes = IntersectTokens(
            local.UriSchemes, remote.UriSchemes);
        result.NegotiatedCapabilities = mutual;

        if (message.Kind == SipMessageKind.Request
            && !ContainsExact(local.Methods, message.MethodToken))
        {
            return SetNegotiationFailure(result,
                SipStatusCodes.MethodNotAllowed,
                "The request method is not supported locally.");
        }

        System.Collections.Generic.List<string> missing =
            new System.Collections.Generic.List<string>();
        AddMissingTokens(missing, result.RequiredOptionTags,
            local.OptionTags);
        if (missing.Count > 0)
        {
            result.UnsupportedOptionTags = MergeTokens(
                result.UnsupportedOptionTags, missing.ToArray());
            return SetNegotiationFailure(result,
                SipStatusCodes.BadExtension,
                "The request requires unsupported SIP extensions.");
        }

        if (message.Body.Length > 0
            && !MessageBodyIsSupported(message, local.MediaTypes))
        {
            return SetNegotiationFailure(result,
                SipStatusCodes.UnsupportedMediaType,
                "The request body media type is not supported locally.");
        }

        string? eventPackage = ReadEventPackage(message);
        if (eventPackage != null
            && !ContainsIgnoreCase(local.EventPackages, eventPackage))
        {
            return SetNegotiationFailure(result, SipStatusCodes.BadEvent,
                "The event package is not supported locally.");
        }
        return result;
    }

    internal static SipCapabilityMessageResult AddAdvertisement(
        SipMessage? message, SipCapabilitySet? capabilities,
        SipExtensionHeaderValue[]? featureCapabilities)
    {
        if (message == null || capabilities == null
            || featureCapabilities == null)
        {
            return CreateMessageFailure(
                "A message, capabilities, and Feature-Caps array are required.");
        }
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(message);
        if (copy == null)
        {
            return CreateMessageFailure("The SIP message could not be copied.");
        }
        SipCapabilitySet normalized =
            SipExtensionRegistryCore.NormalizeCapabilities(capabilities);
        if (normalized.Methods.Length > 0)
        {
            InsertBeforeContentLength(copy, SipHeaderNames.Allow,
                JoinValues(normalized.Methods));
        }
        if (normalized.OptionTags.Length > 0)
        {
            InsertBeforeContentLength(copy, SipHeaderNames.Supported,
                JoinValues(normalized.OptionTags));
        }
        if (normalized.MediaTypes.Length > 0)
        {
            InsertBeforeContentLength(copy, SipHeaderNames.Accept,
                JoinValues(normalized.MediaTypes));
        }
        if (normalized.EventPackages.Length > 0)
        {
            InsertBeforeContentLength(copy, SipHeaderNames.AllowEvents,
                JoinValues(normalized.EventPackages));
        }
        if (featureCapabilities.Length > 0)
        {
            string? value = FormatExtensionValues(
                SipHeaderNames.FeatureCaps, featureCapabilities);
            if (value == null)
            {
                return CreateMessageFailure(
                    "The Feature-Caps values are invalid.");
            }
            InsertBeforeNamedHeader(copy, SipHeaderNames.FeatureCaps,
                SipHeaderNames.FeatureCaps, value);
        }
        return CreateMessageSuccess(copy);
    }

    internal static SipCapabilityMessageResult AddPreferences(
        SipMessage? message, SipCallerPreferences? preferences)
    {
        if (message == null || preferences == null)
        {
            return CreateMessageFailure(
                "A request and caller preferences are required.");
        }
        if (message.Kind != SipMessageKind.Request)
        {
            return CreateMessageFailure(
                "Caller preferences can only be added to a SIP request.");
        }
        if (preferences.AcceptContacts == null
            || preferences.RejectContacts == null
            || preferences.RequestDispositions == null)
        {
            return CreateMessageFailure(
                "Caller preference arrays cannot be null.");
        }

        string? accept = FormatOptionalPreferenceValues(
            SipHeaderNames.AcceptContact, preferences.AcceptContacts);
        string? reject = FormatOptionalPreferenceValues(
            SipHeaderNames.RejectContact, preferences.RejectContacts);
        string? disposition = FormatDispositionValues(
            preferences.RequestDispositions);
        if (accept == "" || reject == "" || disposition == "")
        {
            return CreateMessageFailure("The caller preferences are invalid.");
        }
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(message);
        if (copy == null)
        {
            return CreateMessageFailure("The SIP request could not be copied.");
        }
        if (accept != null)
        {
            InsertBeforeContentLength(copy,
                SipHeaderNames.AcceptContact, accept);
        }
        if (reject != null)
        {
            InsertBeforeContentLength(copy,
                SipHeaderNames.RejectContact, reject);
        }
        if (disposition != null)
        {
            InsertBeforeContentLength(copy,
                SipHeaderNames.RequestDisposition, disposition);
        }
        return CreateMessageSuccess(copy);
    }

    internal static SipCallerPreferenceMatch MatchPreferences(
        SipMessage? request, SipContactValue? contact)
    {
        SipCallerPreferenceMatch result = new SipCallerPreferenceMatch();
        if (request == null || request.Kind != SipMessageKind.Request)
        {
            result.ErrorMessage = "A SIP request is required.";
            return result;
        }
        if (contact == null || contact.Wildcard || contact.Address == null)
        {
            result.ErrorMessage = "A non-wildcard callee contact is required.";
            return result;
        }

        SipCapabilityNegotiation inspected = Inspect(request);
        if (!inspected.Compatible || inspected.CallerPreferences == null)
        {
            result.ErrorMessage = inspected.ErrorMessage;
            return result;
        }
        SipCallerPreferences preferences = inspected.CallerPreferences;
        result.RequestDispositions = CopyStrings(
            preferences.RequestDispositions);
        SipUriParameter[] features = SelectFeatureParameters(
            contact.Address.Parameters);
        if (HasDuplicateFeatures(features))
        {
            result.ErrorMessage =
                "The callee contact repeats a feature parameter.";
            return result;
        }
        if (features.Length == 0)
        {
            result.Eligible = true;
            result.ExplicitMatch = true;
            result.Immune = true;
            result.ScoreThousandths = 1000;
            return result;
        }

        System.Collections.Generic.List<SipExtensionHeaderValue>
            matchedReject =
                new System.Collections.Generic.List<SipExtensionHeaderValue>();
        int index = 0;
        while (index < preferences.RejectContacts.Length)
        {
            SipPreferenceScore score = ScorePreference(
                preferences.RejectContacts[index], features, true);
            if (!score.Contradiction && score.ExplicitTerms == score.Terms)
            {
                matchedReject.Add(preferences.RejectContacts[index]);
            }
            index = index + 1;
        }
        if (matchedReject.Count > 0)
        {
            result.Rejected = true;
            result.MatchedRejectContacts =
                SipSnapshotCopy.CopyExtensionHeaderValues(
                    matchedReject.ToArray());
            return result;
        }

        System.Collections.Generic.List<SipExtensionHeaderValue>
            matchedAccept =
                new System.Collections.Generic.List<SipExtensionHeaderValue>();
        int totalScore = 0;
        int scoreCount = 0;
        bool everyExplicit = true;
        index = 0;
        while (index < preferences.AcceptContacts.Length)
        {
            SipExtensionHeaderValue preference =
                preferences.AcceptContacts[index];
            SipPreferenceScore score = ScorePreference(
                preference, features, false);
            bool required = HasValuelessParameter(
                preference.Parameters, "require");
            bool explicitMatch = HasValuelessParameter(
                preference.Parameters, "explicit");
            if (score.Contradiction)
            {
                if (required)
                {
                    result.ErrorMessage =
                        "A required Accept-Contact preference did not match.";
                    return result;
                }
                everyExplicit = false;
            }
            else
            {
                int preferenceScore = score.Terms == 0 ? 1000
                    : score.ExplicitTerms * 1000 / score.Terms;
                if (explicitMatch && preferenceScore < 1000)
                {
                    if (required)
                    {
                        result.ErrorMessage =
                            "A required explicit Accept-Contact preference did not match.";
                        return result;
                    }
                    preferenceScore = 0;
                }
                if (preferenceScore < 1000)
                {
                    everyExplicit = false;
                }
                totalScore = totalScore + preferenceScore;
                scoreCount = scoreCount + 1;
                matchedAccept.Add(preference);
            }
            index = index + 1;
        }

        result.Eligible = true;
        result.ExplicitMatch = everyExplicit;
        result.ScoreThousandths = preferences.AcceptContacts.Length == 0
            ? 1000 : scoreCount == 0 ? 0 : totalScore / scoreCount;
        result.MatchedAcceptContacts =
            SipSnapshotCopy.CopyExtensionHeaderValues(
                matchedAccept.ToArray());
        return result;
    }

    static string? InspectHeader(SipHeader header,
        System.Collections.Generic.List<string> methods,
        System.Collections.Generic.List<string> supported,
        System.Collections.Generic.List<string> required,
        System.Collections.Generic.List<string> proxyRequired,
        System.Collections.Generic.List<string> unsupported,
        System.Collections.Generic.List<string> mediaTypes,
        System.Collections.Generic.List<string> eventPackages,
        System.Collections.Generic.List<SipExtensionHeaderValue> accept,
        System.Collections.Generic.List<SipExtensionHeaderValue> reject,
        System.Collections.Generic.List<string> dispositions,
        System.Collections.Generic.List<SipExtensionHeaderValue> featureCaps)
    {
        string? name = header.Name;
        string? value = header.Value;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Allow))
        {
            SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                SipHeaderNames.Allow, value, new SipParseOptions());
            if (!parsed.Success) return HeaderError(name, parsed.ErrorMessage);
            AddExactValues(methods, parsed.Methods);
        }
        else if (IsSupportedName(name))
        {
            SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                SipHeaderNames.Supported, value, new SipParseOptions());
            if (!parsed.Success) return HeaderError(name, parsed.ErrorMessage);
            AddTokenValues(supported, parsed.OptionTags);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Require))
        {
            SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                SipHeaderNames.Require, value, new SipParseOptions());
            if (!parsed.Success) return HeaderError(name, parsed.ErrorMessage);
            AddTokenValues(required, parsed.OptionTags);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ProxyRequire))
        {
            SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                SipHeaderNames.ProxyRequire, value, new SipParseOptions());
            if (!parsed.Success) return HeaderError(name, parsed.ErrorMessage);
            AddTokenValues(proxyRequired, parsed.OptionTags);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Unsupported))
        {
            SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                SipHeaderNames.Unsupported, value, new SipParseOptions());
            if (!parsed.Success) return HeaderError(name, parsed.ErrorMessage);
            AddTokenValues(unsupported, parsed.OptionTags);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Accept))
        {
            SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                SipHeaderNames.Accept, value, new SipParseOptions());
            if (!parsed.Success) return HeaderError(name, parsed.ErrorMessage);
            AddMediaValues(mediaTypes, parsed.MediaTypes);
        }
        else if (IsEventName(name))
        {
            SipExtensionHeaderResult parsed =
                SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                    SipHeaderNames.Event, value, new SipParseOptions());
            if (!parsed.Success) return HeaderError(name, parsed.ErrorMessage);
        }
        else if (IsAllowEventsName(name))
        {
            SipExtensionHeaderResult parsed =
                SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                    SipHeaderNames.AllowEvents, value, new SipParseOptions());
            if (!parsed.Success) return HeaderError(name, parsed.ErrorMessage);
            AddExtensionTokens(eventPackages, parsed.Values);
        }
        else if (IsAcceptContactName(name))
        {
            SipExtensionHeaderResult parsed =
                SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                    SipHeaderNames.AcceptContact, value,
                    new SipParseOptions());
            if (!parsed.Success || !ValidatePreferenceValues(
                    parsed.Values, true))
                return HeaderError(name, parsed.ErrorMessage);
            AddExtensionValues(accept, parsed.Values);
        }
        else if (IsRejectContactName(name))
        {
            SipExtensionHeaderResult parsed =
                SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                    SipHeaderNames.RejectContact, value,
                    new SipParseOptions());
            if (!parsed.Success || !ValidatePreferenceValues(
                    parsed.Values, false))
                return HeaderError(name, parsed.ErrorMessage);
            AddExtensionValues(reject, parsed.Values);
        }
        else if (IsRequestDispositionName(name))
        {
            SipExtensionHeaderResult parsed =
                SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                    SipHeaderNames.RequestDisposition, value,
                    new SipParseOptions());
            if (!parsed.Success) return HeaderError(name, parsed.ErrorMessage);
            AddExtensionTokens(dispositions, parsed.Values);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.FeatureCaps))
        {
            SipExtensionHeaderResult parsed =
                SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                    SipHeaderNames.FeatureCaps, value,
                    new SipParseOptions());
            if (!parsed.Success || !ValidateFeatureCaps(parsed.Values))
                return HeaderError(name, parsed.ErrorMessage);
            AddExtensionValues(featureCaps, parsed.Values);
        }
        return null;
    }

    static bool ValidatePreferenceValues(
        SipExtensionHeaderValue[] values, bool accept)
    {
        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            SipExtensionHeaderValue value = values[valueIndex];
            if (value.Token != "*") return false;
            int parameterIndex = 0;
            while (parameterIndex < value.Parameters.Length)
            {
                SipUriParameter parameter = value.Parameters[parameterIndex];
                if (SipAscii.EqualsIgnoreCase(parameter.Name, "require")
                    || SipAscii.EqualsIgnoreCase(parameter.Name, "explicit"))
                {
                    if (!accept || parameter.HasValue) return false;
                }
                else if (SipAscii.EqualsIgnoreCase(parameter.Name, "q"))
                {
                    if (!accept || !parameter.HasValue
                        || !SipHeaderPrimitives.ParseQualityText(
                            parameter.Value).Success) return false;
                }
                parameterIndex = parameterIndex + 1;
            }
            valueIndex = valueIndex + 1;
        }
        return true;
    }

    static bool ValidateFeatureCaps(SipExtensionHeaderValue[] values)
    {
        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            SipExtensionHeaderValue value = values[valueIndex];
            if (value.Token != "*") return false;
            int parameterIndex = 0;
            while (parameterIndex < value.Parameters.Length)
            {
                string? name = value.Parameters[parameterIndex].Name;
                if (name == null || name.Length < 2 || name[0] != '+')
                    return false;
                parameterIndex = parameterIndex + 1;
            }
            valueIndex = valueIndex + 1;
        }
        return true;
    }

    static SipPreferenceScore ScorePreference(
        SipExtensionHeaderValue preference, SipUriParameter[] features,
        bool requirePresence)
    {
        SipPreferenceScore result = new SipPreferenceScore();
        int index = 0;
        while (index < preference.Parameters.Length)
        {
            SipUriParameter wanted = preference.Parameters[index];
            if (IsFeatureParameter(wanted))
            {
                result.Terms = result.Terms + 1;
                SipUriParameter? actual = FindParameter(
                    features, wanted.Name);
                if (actual == null)
                {
                    if (requirePresence)
                    {
                        result.Contradiction = true;
                    }
                }
                else if (FeatureValuesCompatible(wanted, actual))
                {
                    result.ExplicitTerms = result.ExplicitTerms + 1;
                }
                else
                {
                    result.Contradiction = true;
                }
            }
            index = index + 1;
        }
        return result;
    }

    static bool FeatureValuesCompatible(
        SipUriParameter wanted, SipUriParameter actual)
    {
        string wantedValue = wanted.HasValue && wanted.Value != null
            ? wanted.Value : "TRUE";
        string actualValue = actual.HasValue && actual.Value != null
            ? actual.Value : "TRUE";
        string[] wantedValues = wantedValue.Split(',');
        string[] actualValues = actualValue.Split(',');
        int wantedIndex = 0;
        while (wantedIndex < wantedValues.Length)
        {
            int actualIndex = 0;
            while (actualIndex < actualValues.Length)
            {
                if (FeatureAlternativeMatches(
                    wantedValues[wantedIndex].Trim(),
                    actualValues[actualIndex].Trim()))
                {
                    return true;
                }
                actualIndex = actualIndex + 1;
            }
            wantedIndex = wantedIndex + 1;
        }
        return false;
    }

    static bool FeatureAlternativeMatches(string wanted, string actual)
    {
        bool wantedNegative = wanted.Length > 0 && wanted[0] == '!';
        bool actualNegative = actual.Length > 0 && actual[0] == '!';
        string wantedValue = wantedNegative ? wanted.Substring(1) : wanted;
        string actualValue = actualNegative ? actual.Substring(1) : actual;
        bool equal;
        if (wantedValue.Length > 1 && wantedValue[0] == '<'
            && wantedValue[wantedValue.Length - 1] == '>'
            || actualValue.Length > 1 && actualValue[0] == '<'
                && actualValue[actualValue.Length - 1] == '>')
        {
            equal = wantedValue == actualValue;
        }
        else if (wantedValue.Length > 0 && wantedValue[0] == '#'
            || actualValue.Length > 0 && actualValue[0] == '#')
        {
            equal = NumericRangesOverlap(wantedValue, actualValue);
        }
        else
        {
            equal = SipAscii.EqualsIgnoreCase(wantedValue, actualValue);
        }
        if (wantedNegative && actualNegative) return true;
        if (wantedNegative || actualNegative) return !equal;
        return equal;
    }

    static bool NumericRangesOverlap(string wanted, string actual)
    {
        SipFeatureNumericRange wantedRange = ParseNumericRange(wanted);
        SipFeatureNumericRange actualRange = ParseNumericRange(actual);
        if (!wantedRange.Success || !actualRange.Success) return false;
        return wantedRange.Minimum <= actualRange.Maximum
            && actualRange.Minimum <= wantedRange.Maximum;
    }

    static SipFeatureNumericRange ParseNumericRange(string text)
    {
        SipFeatureNumericRange result = new SipFeatureNumericRange();
        if (text.Length == 0) return result;
        if (text[0] != '#')
        {
            SipFeatureNumber plain = ParseFeatureNumber(text);
            if (!plain.Success) return result;
            result.Success = true;
            result.Minimum = plain.Value;
            result.Maximum = plain.Value;
            return result;
        }
        string value = text.Substring(1);
        if (value.StartsWith(">="))
        {
            SipFeatureNumber number = ParseFeatureNumber(value.Substring(2));
            if (!number.Success) return result;
            result.Success = true;
            result.Minimum = number.Value;
            result.Maximum = long.MaxValue;
            return result;
        }
        if (value.StartsWith("<="))
        {
            SipFeatureNumber number = ParseFeatureNumber(value.Substring(2));
            if (!number.Success) return result;
            result.Success = true;
            result.Minimum = long.MinValue;
            result.Maximum = number.Value;
            return result;
        }
        if (value.StartsWith("=")) value = value.Substring(1);
        int colon = value.IndexOf(':');
        if (colon >= 0)
        {
            SipFeatureNumber minimum = ParseFeatureNumber(
                value.Substring(0, colon));
            SipFeatureNumber maximum = ParseFeatureNumber(
                value.Substring(colon + 1));
            if (!minimum.Success || !maximum.Success
                || minimum.Value > maximum.Value) return result;
            result.Success = true;
            result.Minimum = minimum.Value;
            result.Maximum = maximum.Value;
            return result;
        }
        SipFeatureNumber exact = ParseFeatureNumber(value);
        if (!exact.Success) return result;
        result.Success = true;
        result.Minimum = exact.Value;
        result.Maximum = exact.Value;
        return result;
    }

    static SipFeatureNumber ParseFeatureNumber(string text)
    {
        SipFeatureNumber result = new SipFeatureNumber();
        if (text.Length == 0) return result;
        bool negative = false;
        int index = 0;
        if (text[0] == '+' || text[0] == '-')
        {
            negative = text[0] == '-';
            index = 1;
        }
        if (index == text.Length) return result;
        long whole = 0;
        int wholeDigits = 0;
        while (index < text.Length && text[index] != '.')
        {
            int digit = text[index] - '0';
            if (digit < 0 || digit > 9 || whole > 900000000000L)
                return result;
            whole = whole * 10 + digit;
            wholeDigits = wholeDigits + 1;
            index = index + 1;
        }
        if (wholeDigits == 0) return result;
        if (whole > 9000000000000L) return result;
        long fraction = 0;
        int fractionDigits = 0;
        if (index < text.Length)
        {
            index = index + 1;
            while (index < text.Length)
            {
                int digit = text[index] - '0';
                if (digit < 0 || digit > 9) return result;
                if (fractionDigits < 6)
                {
                    fraction = fraction * 10 + digit;
                    fractionDigits = fractionDigits + 1;
                }
                index = index + 1;
            }
        }
        while (fractionDigits < 6)
        {
            fraction = fraction * 10;
            fractionDigits = fractionDigits + 1;
        }
        long scaled = whole * 1000000L + fraction;
        result.Success = true;
        result.Value = negative ? -scaled : scaled;
        return result;
    }

    static SipUriParameter[] SelectFeatureParameters(
        SipUriParameter[]? parameters)
    {
        System.Collections.Generic.List<SipUriParameter> result =
            new System.Collections.Generic.List<SipUriParameter>();
        if (parameters == null) return result.ToArray();
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter? parameter = parameters[index];
            if (parameter != null && IsFeatureParameter(parameter))
            {
                result.Add(parameter);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static bool IsFeatureParameter(SipUriParameter parameter)
    {
        string? name = parameter.Name;
        if (name == null || name.Length == 0) return false;
        if (name[0] == '+') return true;
        return SipAscii.EqualsIgnoreCase(name, "audio")
            || SipAscii.EqualsIgnoreCase(name, "automata")
            || SipAscii.EqualsIgnoreCase(name, "class")
            || SipAscii.EqualsIgnoreCase(name, "duplex")
            || SipAscii.EqualsIgnoreCase(name, "data")
            || SipAscii.EqualsIgnoreCase(name, "control")
            || SipAscii.EqualsIgnoreCase(name, "mobility")
            || SipAscii.EqualsIgnoreCase(name, "description")
            || SipAscii.EqualsIgnoreCase(name, "events")
            || SipAscii.EqualsIgnoreCase(name, "priority")
            || SipAscii.EqualsIgnoreCase(name, "methods")
            || SipAscii.EqualsIgnoreCase(name, "schemes")
            || SipAscii.EqualsIgnoreCase(name, "application")
            || SipAscii.EqualsIgnoreCase(name, "video")
            || SipAscii.EqualsIgnoreCase(name, "language")
            || SipAscii.EqualsIgnoreCase(name, "type")
            || SipAscii.EqualsIgnoreCase(name, "isfocus")
            || SipAscii.EqualsIgnoreCase(name, "actor")
            || SipAscii.EqualsIgnoreCase(name, "text")
            || SipAscii.EqualsIgnoreCase(name, "extensions");
    }

    static bool HasValuelessParameter(
        SipUriParameter[] parameters, string name)
    {
        SipUriParameter? parameter = FindParameter(parameters, name);
        return parameter != null && !parameter.HasValue;
    }

    static bool HasDuplicateFeatures(SipUriParameter[] features)
    {
        int index = 0;
        while (index < features.Length)
        {
            int other = index + 1;
            while (other < features.Length)
            {
                if (SipAscii.EqualsIgnoreCase(
                    features[index].Name, features[other].Name)) return true;
                other = other + 1;
            }
            index = index + 1;
        }
        return false;
    }

    static SipUriParameter? FindParameter(
        SipUriParameter[] parameters, string? name)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            if (SipAscii.EqualsIgnoreCase(parameters[index].Name, name))
                return parameters[index];
            index = index + 1;
        }
        return null;
    }

    static string? FormatOptionalPreferenceValues(
        string name, SipExtensionHeaderValue[] values)
    {
        if (values.Length == 0) return null;
        if (!ValidatePreferenceValues(values,
            name == SipHeaderNames.AcceptContact)) return "";
        string? value = FormatExtensionValues(name, values);
        return value == null ? "" : value;
    }

    static string? FormatDispositionValues(string[] values)
    {
        if (values.Length == 0) return null;
        SipExtensionHeaderValue[] modeled =
            new SipExtensionHeaderValue[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (!SipAscii.IsToken(values[index])) return "";
            modeled[index] = new SipExtensionHeaderValue();
            modeled[index].Token = values[index];
            index = index + 1;
        }
        string? value = FormatExtensionValues(
            SipHeaderNames.RequestDisposition, modeled);
        return value == null ? "" : value;
    }

    static string? FormatExtensionValues(
        string name, SipExtensionHeaderValue[] values)
    {
        if (values.Length == 0) return null;
        if (name == SipHeaderNames.FeatureCaps
            && !ValidateFeatureCaps(values)) return null;
        if (!ValidateExtensionModels(values)) return null;
        SipExtensionHeaderResult parsed = new SipExtensionHeaderResult();
        parsed.Success = true;
        parsed.Values = SipSnapshotCopy.CopyExtensionHeaderValues(values);
        SipWriteResult formatted =
            SipExtensionHeaderCodec.FormatExtensionHeaderValue(
                name, parsed, new SipWriteOptions());
        if (!formatted.Success) return null;
        string text = System.Text.Encoding.UTF8.GetString(formatted.Bytes);
        int colon = text.IndexOf(':');
        if (colon < 0 || text.Length < colon + 4) return null;
        string value = text.Substring(colon + 1).Trim(' ', '\t', '\r', '\n');
        SipExtensionHeaderResult validated =
            SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                name, value, new SipParseOptions());
        return validated.Success ? value : null;
    }

    static bool ValidateExtensionModels(SipExtensionHeaderValue[] values)
    {
        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            SipExtensionHeaderValue? value = values[valueIndex];
            if (value == null || !SipAscii.IsToken(value.Token)
                || value.Parameters == null) return false;
            int parameterIndex = 0;
            while (parameterIndex < value.Parameters.Length)
            {
                SipUriParameter? parameter = value.Parameters[parameterIndex];
                if (parameter == null || !SipAscii.IsToken(parameter.Name)
                    || parameter.HasValue
                        && (parameter.Value == null
                            || parameter.Value.Length == 0)) return false;
                int previous = 0;
                while (previous < parameterIndex)
                {
                    if (SipAscii.EqualsIgnoreCase(
                        value.Parameters[previous].Name, parameter.Name))
                        return false;
                    previous = previous + 1;
                }
                parameterIndex = parameterIndex + 1;
            }
            valueIndex = valueIndex + 1;
        }
        return true;
    }

    static void InsertBeforeContentLength(
        SipMessage message, string name, string value)
    {
        InsertBeforeNamedHeader(message, SipHeaderNames.ContentLength,
            name, value);
    }

    static void InsertBeforeNamedHeader(
        SipMessage message, string beforeName, string name, string value)
    {
        SipHeader added = new SipHeader();
        added.Name = name;
        added.Value = value;
        int insertion = message.Headers.Length;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (insertion == message.Headers.Length && header != null
                && (SipAscii.EqualsIgnoreCase(header.Name, beforeName)
                    || beforeName == SipHeaderNames.ContentLength
                        && SipAscii.EqualsIgnoreCase(header.Name,
                            SipHeaderNames.CompactContentLength)))
            {
                insertion = index;
            }
            index = index + 1;
        }
        SipHeader[] updated = new SipHeader[message.Headers.Length + 1];
        index = 0;
        while (index < insertion)
        {
            updated[index] = message.Headers[index];
            index = index + 1;
        }
        updated[insertion] = added;
        while (index < message.Headers.Length)
        {
            updated[index + 1] = message.Headers[index];
            index = index + 1;
        }
        message.Headers = updated;
        message.OriginalBytes = new byte[0];
    }

    static void AddMediaValues(
        System.Collections.Generic.List<string> target,
        SipMediaType[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            SipMediaType value = values[index];
            string text = ((value.Type == null ? "" : value.Type)
                + "/" + (value.Subtype == null ? "" : value.Subtype))
                .ToLowerInvariant();
            if (!ListContainsIgnoreCase(target, text)) target.Add(text);
            index = index + 1;
        }
    }

    static void AddExtensionTokens(
        System.Collections.Generic.List<string> target,
        SipExtensionHeaderValue[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            string? token = values[index].Token;
            if (token != null && !ListContainsIgnoreCase(target, token))
                target.Add(token);
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
            SipExtensionHeaderValue[] copied =
                SipSnapshotCopy.CopyExtensionHeaderValues(
                    new SipExtensionHeaderValue[] { values[index] });
            target.Add(copied[0]);
            index = index + 1;
        }
    }

    static void AddExactValues(
        System.Collections.Generic.List<string> target, string[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!ListContainsExact(target, values[index]))
                target.Add(values[index]);
            index = index + 1;
        }
    }

    static void AddTokenValues(
        System.Collections.Generic.List<string> target, string[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!ListContainsIgnoreCase(target, values[index]))
                target.Add(values[index]);
            index = index + 1;
        }
    }

    static void AddMissingTokens(
        System.Collections.Generic.List<string> target,
        string[] required, string[] supported)
    {
        int index = 0;
        while (index < required.Length)
        {
            if (!ContainsIgnoreCase(supported, required[index])
                && !ListContainsIgnoreCase(target, required[index]))
                target.Add(required[index]);
            index = index + 1;
        }
    }

    static string[] IntersectMethods(string[] local, string[] remote)
    {
        System.Collections.Generic.List<string> result =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < remote.Length)
        {
            if (ContainsExact(local, remote[index])
                && !ListContainsExact(result, remote[index]))
                result.Add(remote[index]);
            index = index + 1;
        }
        return result.ToArray();
    }

    static string[] IntersectTokens(string[] local, string[] remote)
    {
        System.Collections.Generic.List<string> result =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < remote.Length)
        {
            if (ContainsIgnoreCase(local, remote[index])
                && !ListContainsIgnoreCase(result, remote[index]))
                result.Add(remote[index]);
            index = index + 1;
        }
        return result.ToArray();
    }

    static string[] IntersectHeaderNames(string[] local, string[] remote)
    {
        System.Collections.Generic.List<string> result =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < remote.Length)
        {
            if (ContainsHeaderName(local, remote[index])
                && !ListContainsHeaderName(result, remote[index]))
                result.Add(remote[index]);
            index = index + 1;
        }
        return result.ToArray();
    }

    static string[] IntersectMediaTypes(string[] local, string[] remote)
    {
        System.Collections.Generic.List<string> result =
            new System.Collections.Generic.List<string>();
        int remoteIndex = 0;
        while (remoteIndex < remote.Length)
        {
            int localIndex = 0;
            while (localIndex < local.Length)
            {
                string? intersection = MediaTypeIntersection(
                    local[localIndex], remote[remoteIndex]);
                if (intersection != null
                    && !ListContainsIgnoreCase(result, intersection))
                    result.Add(intersection);
                localIndex = localIndex + 1;
            }
            remoteIndex = remoteIndex + 1;
        }
        return result.ToArray();
    }

    static string? MediaTypeIntersection(string left, string right)
    {
        int leftSlash = left.IndexOf('/');
        int rightSlash = right.IndexOf('/');
        if (leftSlash <= 0 || rightSlash <= 0) return null;
        string leftType = left.Substring(0, leftSlash);
        string leftSubtype = left.Substring(leftSlash + 1);
        string rightType = right.Substring(0, rightSlash);
        string rightSubtype = right.Substring(rightSlash + 1);
        if (leftType != "*" && rightType != "*"
            && !SipAscii.EqualsIgnoreCase(leftType, rightType)) return null;
        if (leftSubtype != "*" && rightSubtype != "*"
            && !SipAscii.EqualsIgnoreCase(leftSubtype, rightSubtype))
            return null;
        string type = rightType == "*" ? leftType : rightType;
        string subtype = rightSubtype == "*" ? leftSubtype : rightSubtype;
        return (type + "/" + subtype).ToLowerInvariant();
    }

    static bool MessageBodyIsSupported(
        SipMessage message, string[] supported)
    {
        if (message.ContentType == null || message.ContentType.Type == null
            || message.ContentType.Subtype == null) return false;
        string actual = message.ContentType.Type + "/"
            + message.ContentType.Subtype;
        int index = 0;
        while (index < supported.Length)
        {
            if (MediaTypeIntersection(supported[index], actual) != null)
                return true;
            index = index + 1;
        }
        return false;
    }

    static string? ReadEventPackage(SipMessage message)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && IsEventName(header.Name))
            {
                SipExtensionHeaderResult parsed =
                    SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                        SipHeaderNames.Event, header.Value,
                        new SipParseOptions());
                if (parsed.Success && parsed.Values.Length == 1)
                    return parsed.Values[0].Token;
            }
            index = index + 1;
        }
        return null;
    }

    static string[] MergeTokens(string[] first, string[] second)
    {
        System.Collections.Generic.List<string> result =
            new System.Collections.Generic.List<string>();
        AddTokenValues(result, first);
        AddTokenValues(result, second);
        return result.ToArray();
    }

    static bool ContainsExact(string[] values, string? expected)
    {
        if (expected == null) return false;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == expected) return true;
            index = index + 1;
        }
        return false;
    }

    static bool ContainsIgnoreCase(string[] values, string? expected)
    {
        if (expected == null) return false;
        int index = 0;
        while (index < values.Length)
        {
            if (SipAscii.EqualsIgnoreCase(values[index], expected)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool ContainsHeaderName(string[] values, string expected)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (SipGenericValueAccess.HeaderNamesMatch(
                values[index], expected)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool ListContainsExact(
        System.Collections.Generic.List<string> values, string expected)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == expected) return true;
            index = index + 1;
        }
        return false;
    }

    static bool ListContainsIgnoreCase(
        System.Collections.Generic.List<string> values, string expected)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (SipAscii.EqualsIgnoreCase(values[index], expected)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool ListContainsHeaderName(
        System.Collections.Generic.List<string> values, string expected)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (SipGenericValueAccess.HeaderNamesMatch(
                values[index], expected)) return true;
            index = index + 1;
        }
        return false;
    }

    static string JoinValues(string[] values)
    {
        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0) builder.Append(", ");
            builder.Append(values[index]);
            index = index + 1;
        }
        return builder.ToString();
    }

    static string[] CopyStrings(string[] values)
    {
        string[] copy = new string[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static bool IsSupportedName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Supported)
            || SipAscii.EqualsIgnoreCase(
                name, SipHeaderNames.CompactSupported);
    }

    static bool IsAllowEventsName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.AllowEvents)
            || SipAscii.EqualsIgnoreCase(
                name, SipHeaderNames.CompactAllowEvents);
    }

    static bool IsAcceptContactName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.AcceptContact)
            || SipAscii.EqualsIgnoreCase(
                name, SipHeaderNames.CompactAcceptContact);
    }

    static bool IsRejectContactName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RejectContact)
            || SipAscii.EqualsIgnoreCase(
                name, SipHeaderNames.CompactRejectContact);
    }

    static bool IsRequestDispositionName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(
                name, SipHeaderNames.RequestDisposition)
            || SipAscii.EqualsIgnoreCase(
                name, SipHeaderNames.CompactRequestDisposition);
    }

    static bool IsEventName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Event)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactEvent);
    }

    static string HeaderError(string? name, string? error)
    {
        return (name == null ? "A capability header" : name)
            + " is invalid: " + (error == null
                ? "the field value is malformed." : error);
    }

    static SipCapabilityNegotiation CreateInspectionFailure(string message)
    {
        SipCapabilityNegotiation result = new SipCapabilityNegotiation();
        result.StatusCode = SipStatusCodes.BadRequest;
        result.ErrorMessage = message;
        result.RemoteCapabilities = new SipCapabilitySet();
        result.NegotiatedCapabilities = new SipCapabilitySet();
        result.CallerPreferences = new SipCallerPreferences();
        return result;
    }

    static SipCapabilityNegotiation SetNegotiationFailure(
        SipCapabilityNegotiation result, int statusCode, string message)
    {
        result.Compatible = false;
        result.StatusCode = statusCode;
        result.ErrorMessage = message;
        return result;
    }

    static SipCapabilityMessageResult CreateMessageFailure(string message)
    {
        SipCapabilityMessageResult result =
            new SipCapabilityMessageResult();
        result.ErrorMessage = message;
        return result;
    }

    static SipCapabilityMessageResult CreateMessageSuccess(SipMessage message)
    {
        SipCapabilityMessageResult result =
            new SipCapabilityMessageResult();
        result.Success = true;
        result.Message = message;
        return result;
    }
}

internal class SipPreferenceScore
{
    internal int Terms;
    internal int ExplicitTerms;
    internal bool Contradiction;
}

internal class SipFeatureNumericRange
{
    internal bool Success;
    internal long Minimum;
    internal long Maximum;
}

internal class SipFeatureNumber
{
    internal bool Success;
    internal long Value;
}
