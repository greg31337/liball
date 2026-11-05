namespace sip;

internal static class SipExtensionRegistryCore
{
    internal static SipCapabilitySet AggregateCapabilities(ISipExtension[] extensions)
    {
        System.Collections.Generic.List<string> methods = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> optionTags = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> mediaTypes = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> eventPackages = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> headerNames = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> uriSchemes = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> contentCodings = new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < extensions.Length)
        {
            ISipExtension? extension = extensions[index];
            SipCapabilitySet? capabilities = extension == null ? null : extension.GetExtensionCapabilities();
            if (capabilities != null)
            {
                AddMethodCapabilities(methods, capabilities.Methods);
                AddTokenCapabilities(optionTags, capabilities.OptionTags);
                AddMediaCapabilities(mediaTypes, capabilities.MediaTypes);
                AddTokenCapabilities(eventPackages, capabilities.EventPackages);
                AddHeaderCapabilities(headerNames, capabilities.HeaderNames);
                AddSchemeCapabilities(uriSchemes, capabilities.UriSchemes);
                AddTokenCapabilities(contentCodings, capabilities.ContentCodings);
            }
            index = index + 1;
        }

        SipCapabilitySet result = new SipCapabilitySet();
        result.Methods = methods.ToArray();
        result.OptionTags = optionTags.ToArray();
        result.MediaTypes = mediaTypes.ToArray();
        result.EventPackages = eventPackages.ToArray();
        result.HeaderNames = headerNames.ToArray();
        result.UriSchemes = uriSchemes.ToArray();
        result.ContentCodings = contentCodings.ToArray();
        return result;
    }

    internal static SipCapabilitySet NormalizeCapabilities(
        SipCapabilitySet? capabilities)
    {
        System.Collections.Generic.List<string> methods =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> optionTags =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> mediaTypes =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> eventPackages =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> headerNames =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> uriSchemes =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> contentCodings =
            new System.Collections.Generic.List<string>();
        if (capabilities != null)
        {
            AddMethodCapabilities(methods, capabilities.Methods);
            AddTokenCapabilities(optionTags, capabilities.OptionTags);
            AddMediaCapabilities(mediaTypes, capabilities.MediaTypes);
            AddTokenCapabilities(
                eventPackages, capabilities.EventPackages);
            AddHeaderCapabilities(headerNames, capabilities.HeaderNames);
            AddSchemeCapabilities(uriSchemes, capabilities.UriSchemes);
            AddTokenCapabilities(contentCodings, capabilities.ContentCodings);
        }
        SipCapabilitySet result = new SipCapabilitySet();
        result.Methods = methods.ToArray();
        result.OptionTags = optionTags.ToArray();
        result.MediaTypes = mediaTypes.ToArray();
        result.EventPackages = eventPackages.ToArray();
        result.HeaderNames = headerNames.ToArray();
        result.UriSchemes = uriSchemes.ToArray();
        result.ContentCodings = contentCodings.ToArray();
        return result;
    }

    internal static bool SupportsMethodToken(ISipExtension[] extensions, string? method)
    {
        if (!SipAscii.IsToken(method)) return false;
        SipCapabilitySet capabilities = AggregateCapabilities(extensions);
        return ContainsExact(capabilities.Methods, method);
    }

    internal static bool SupportsHeaderName(ISipExtension[] extensions, string? headerName)
    {
        if (!SipAscii.IsToken(headerName)) return false;
        SipCapabilitySet capabilities = AggregateCapabilities(extensions);
        return ContainsHeaderName(capabilities.HeaderNames, headerName);
    }

    internal static bool SupportsSchemeToken(ISipExtension[] extensions, string? scheme)
    {
        if (!SipUriSyntax.IsScheme(scheme)) return false;
        SipCapabilitySet capabilities = AggregateCapabilities(extensions);
        return ContainsIgnoreCase(capabilities.UriSchemes, scheme);
    }

    internal static bool SupportsMediaTypeValue(ISipExtension[] extensions, string? mediaType)
    {
        if (!IsMediaType(mediaType)) return false;
        SipCapabilitySet capabilities = AggregateCapabilities(extensions);
        int index = 0;
        while (index < capabilities.MediaTypes.Length)
        {
            if (MediaTypeMatches(capabilities.MediaTypes[index], mediaType)) return true;
            index = index + 1;
        }
        return false;
    }

    internal static bool SupportsOptionTagToken(ISipExtension[] extensions, string? optionTag)
    {
        if (!SipAscii.IsToken(optionTag)) return false;
        SipCapabilitySet capabilities = AggregateCapabilities(extensions);
        return ContainsIgnoreCase(capabilities.OptionTags, optionTag);
    }

    internal static bool SupportsEventPackageToken(ISipExtension[] extensions, string? eventPackage)
    {
        if (!SipAscii.IsToken(eventPackage)) return false;
        SipCapabilitySet capabilities = AggregateCapabilities(extensions);
        return ContainsIgnoreCase(capabilities.EventPackages, eventPackage);
    }

    internal static bool SupportsContentCodingToken(
        ISipExtension[] extensions, string? contentCoding)
    {
        if (!SipAscii.IsToken(contentCoding)) return false;
        SipCapabilitySet capabilities = AggregateCapabilities(extensions);
        return ContainsIgnoreCase(capabilities.ContentCodings, contentCoding);
    }

    internal static SipExtensionResult ProcessRequest(ISipExtension[] extensions, SipMessage? message)
    {
        if (message == null) return CreateFailure("An extension request message is required.");
        if (message.Kind != SipMessageKind.Request)
            return CreateFailure("Only SIP requests can be dispatched to an extension.");
        bool canRespond = SipUasResponseCore.CanCreateResponse(message);
        bool suppressResponse = message.Method == SipMethod.Ack;
        if (canRespond && !suppressResponse)
        {
            SipValidationIssue[] issues =
                SipMessageValidator.ValidateMessageModel(message, true);
            if (SipMessageValidator.HasErrorIssues(issues))
            {
                return CreateProtocolResult(extensions, message,
                    SipStatusCodes.BadRequest,
                    SipReasonPhrases.BadRequest);
            }
        }
        int index = 0;
        while (index < extensions.Length)
        {
            ISipExtension? extension = extensions[index];
            SipCapabilitySet? capabilities = extension == null ? null : extension.GetExtensionCapabilities();
            if (extension != null && RequestMatchesCapabilities(message, capabilities))
            {
                SipMessage? dispatched =
                    SipSnapshotCopy.CopyMessageModel(message);
                SipExtensionResult? result = extension.ProcessExtensionRequest(
                    dispatched == null ? message : dispatched);
                if (result == null) return CreateFailure("An extension returned no processing result.");
                if (result.Handled || result.Response != null || result.ErrorMessage != null)
                {
                    if (result.Response != null)
                    {
                        if (suppressResponse)
                        {
                            return CreateFailure(
                                "An ACK request cannot receive a SIP response.");
                        }
                        SipMessage? response = SipUasResponseCore
                            .CompleteApplicationResponse(
                                message, result.Response);
                        if (response == null)
                        {
                            return CreateFailure(
                                "An extension returned an invalid SIP response.");
                        }
                        SipExtensionResult completed =
                            CopyExtensionResult(result);
                        completed.Response = response;
                        return completed;
                    }
                    return CopyExtensionResult(result);
                }
            }
            index = index + 1;
        }
        if (canRespond && message.Method == SipMethod.Options)
        {
            SipOptionsResult options = SipOptionsServiceCore.AnswerRequest(
                message, AggregateCapabilities(extensions));
            return ConvertOptionsResult(options);
        }
        if (suppressResponse)
        {
            return new SipExtensionResult();
        }
        if (canRespond)
        {
            bool extensionMethod = message.Method == SipMethod.Unknown;
            return CreateProtocolResult(extensions, message,
                extensionMethod
                    ? SipStatusCodes.NotImplemented
                    : SipStatusCodes.MethodNotAllowed,
                extensionMethod
                    ? SipReasonPhrases.NotImplemented
                    : SipReasonPhrases.MethodNotAllowed);
        }
        return new SipExtensionResult();
    }

    static SipExtensionResult CreateProtocolResult(
        ISipExtension[] extensions, SipMessage message,
        int statusCode, string reason)
    {
        SipCapabilitySet capabilities =
            AggregateCapabilities(extensions);
        SipOptionsResult options =
            SipOptionsServiceCore.CreateResponseResult(
                message, capabilities, statusCode, reason, false);
        return ConvertOptionsResult(options);
    }

    static SipExtensionResult ConvertOptionsResult(
        SipOptionsResult options)
    {
        SipExtensionResult result = new SipExtensionResult();
        result.Handled = options.Response != null;
        result.Response =
            SipSnapshotCopy.CopyMessageModel(options.Response);
        result.ErrorMessage = options.ErrorMessage;
        return result;
    }

    static SipExtensionResult CopyExtensionResult(
        SipExtensionResult source)
    {
        SipExtensionResult result = new SipExtensionResult();
        result.Handled = source.Handled;
        result.Response =
            SipSnapshotCopy.CopyMessageModel(source.Response);
        result.Event = source.Event;
        result.ErrorMessage = source.ErrorMessage;
        return result;
    }

    static void AddMethodCapabilities(System.Collections.Generic.List<string> target, string[]? source)
    {
        if (source == null) return;
        int index = 0;
        while (index < source.Length)
        {
            string? value = source[index];
            if (SipAscii.IsToken(value) && !ListContainsExact(target, value)) target.Add(value);
            index = index + 1;
        }
    }

    static void AddTokenCapabilities(System.Collections.Generic.List<string> target, string[]? source)
    {
        if (source == null) return;
        int index = 0;
        while (index < source.Length)
        {
            string? value = source[index];
            if (SipAscii.IsToken(value) && !ListContainsIgnoreCase(target, value)) target.Add(value);
            index = index + 1;
        }
    }

    static void AddSchemeCapabilities(System.Collections.Generic.List<string> target, string[]? source)
    {
        if (source == null) return;
        int index = 0;
        while (index < source.Length)
        {
            string? value = source[index];
            if (SipUriSyntax.IsScheme(value) && !ListContainsIgnoreCase(target, value)) target.Add(value);
            index = index + 1;
        }
    }

    static void AddHeaderCapabilities(System.Collections.Generic.List<string> target, string[]? source)
    {
        if (source == null) return;
        int index = 0;
        while (index < source.Length)
        {
            string? value = source[index];
            if (SipAscii.IsToken(value) && !ListContainsHeaderName(target, value)) target.Add(value);
            index = index + 1;
        }
    }

    static void AddMediaCapabilities(System.Collections.Generic.List<string> target, string[]? source)
    {
        if (source == null) return;
        int index = 0;
        while (index < source.Length)
        {
            string? value = source[index];
            if (IsMediaType(value) && !ListContainsIgnoreCase(target, value)) target.Add(value);
            index = index + 1;
        }
    }

    static bool RequestMatchesCapabilities(SipMessage message, SipCapabilitySet? capabilities)
    {
        if (capabilities == null) return false;
        if (ContainsExact(capabilities.Methods, message.MethodToken)) return true;
        SipUri? requestUri = message.RequestUri;
        if (requestUri != null && ContainsIgnoreCase(capabilities.UriSchemes, requestUri.SchemeToken)) return true;
        string? contentType = GetMessageContentType(message);
        if (MatchesAnyMediaType(capabilities.MediaTypes, contentType)) return true;
        if (MatchesAnyHeaderName(message.Headers, capabilities.HeaderNames)) return true;
        if (MatchesAnyOptionTag(message.Headers, capabilities.OptionTags)) return true;
        return MatchesEventPackage(message.Headers, capabilities.EventPackages);
    }

    static string? GetMessageContentType(SipMessage message)
    {
        if (message.ContentType != null && SipAscii.IsToken(message.ContentType.Type)
            && SipAscii.IsToken(message.ContentType.Subtype))
            return message.ContentType.Type + "/" + message.ContentType.Subtype;
        string? value = GetFirstHeaderValue(message.Headers, SipHeaderNames.ContentType,
            SipHeaderNames.CompactContentType);
        if (value == null) return null;
        int semicolon = value.IndexOf(';');
        if (semicolon >= 0) value = value.Substring(0, semicolon);
        return value.Trim();
    }

    static bool MatchesAnyMediaType(string[]? advertised, string? actual)
    {
        if (!IsMediaType(actual) || advertised == null) return false;
        int index = 0;
        while (index < advertised.Length)
        {
            if (MediaTypeMatches(advertised[index], actual)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool MatchesAnyHeaderName(SipHeader[]? headers, string[]? advertised)
    {
        if (headers == null || advertised == null) return false;
        int headerIndex = 0;
        while (headerIndex < headers.Length)
        {
            SipHeader? header = headers[headerIndex];
            if (header != null && ContainsHeaderName(advertised, header.Name)) return true;
            headerIndex = headerIndex + 1;
        }
        return false;
    }

    static bool ContainsHeaderName(string[] values, string? expected)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (SipGenericValueAccess.HeaderNamesMatch(values[index], expected)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool MatchesAnyOptionTag(SipHeader[]? headers, string[]? advertised)
    {
        if (headers == null || advertised == null) return false;
        int headerIndex = 0;
        while (headerIndex < headers.Length)
        {
            SipHeader? header = headers[headerIndex];
            if (header != null && IsOptionTagHeader(header.Name)
                && ValueContainsAdvertisedToken(header.Value, advertised)) return true;
            headerIndex = headerIndex + 1;
        }
        return false;
    }

    static bool MatchesEventPackage(SipHeader[]? headers, string[]? advertised)
    {
        string? value = GetFirstHeaderValue(headers, SipHeaderNames.Event, SipHeaderNames.CompactEvent);
        if (value == null || advertised == null) return false;
        int semicolon = value.IndexOf(';');
        if (semicolon >= 0) value = value.Substring(0, semicolon);
        return ContainsIgnoreCase(advertised, value.Trim());
    }

    static bool ValueContainsAdvertisedToken(string? value, string[] advertised)
    {
        if (value == null) return false;
        string[] values = value.Split(',');
        int index = 0;
        while (index < values.Length)
        {
            if (ContainsIgnoreCase(advertised, values[index].Trim())) return true;
            index = index + 1;
        }
        return false;
    }

    static string? GetFirstHeaderValue(SipHeader[]? headers, string longName, string compactName)
    {
        if (headers == null) return null;
        int index = 0;
        while (index < headers.Length)
        {
            SipHeader? header = headers[index];
            if (header != null && (SipAscii.EqualsIgnoreCase(header.Name, longName)
                || SipAscii.EqualsIgnoreCase(header.Name, compactName))) return header.Value;
            index = index + 1;
        }
        return null;
    }

    static bool IsOptionTagHeader(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Supported)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactSupported)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Require)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ProxyRequire)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Unsupported);
    }

    static bool IsMediaType(string? value)
    {
        if (value == null) return false;
        int slash = value.IndexOf('/');
        if (slash <= 0 || slash != value.LastIndexOf('/') || slash + 1 >= value.Length) return false;
        string type = value.Substring(0, slash);
        string subtype = value.Substring(slash + 1);
        if (type == "*" && subtype != "*") return false;
        return (type == "*" || SipAscii.IsToken(type)) && (subtype == "*" || SipAscii.IsToken(subtype));
    }

    static bool MediaTypeMatches(string? advertised, string? actual)
    {
        if (advertised == null || actual == null) return false;
        if (!IsMediaType(advertised) || !IsMediaType(actual)) return false;
        int advertisedSlash = advertised.IndexOf('/');
        int actualSlash = actual.IndexOf('/');
        string advertisedType = advertised.Substring(0, advertisedSlash);
        string advertisedSubtype = advertised.Substring(advertisedSlash + 1);
        string actualType = actual.Substring(0, actualSlash);
        string actualSubtype = actual.Substring(actualSlash + 1);
        bool typeMatches = advertisedType == "*" || SipAscii.EqualsIgnoreCase(advertisedType, actualType);
        bool subtypeMatches = advertisedSubtype == "*" || SipAscii.EqualsIgnoreCase(advertisedSubtype, actualSubtype);
        return typeMatches && subtypeMatches;
    }

    static bool ContainsExact(string[]? values, string? expected)
    {
        if (values == null || expected == null) return false;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == expected) return true;
            index = index + 1;
        }
        return false;
    }

    static bool ContainsIgnoreCase(string[]? values, string? expected)
    {
        if (values == null || expected == null) return false;
        int index = 0;
        while (index < values.Length)
        {
            if (SipAscii.EqualsIgnoreCase(values[index], expected)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool ListContainsExact(System.Collections.Generic.List<string> values, string expected)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == expected) return true;
            index = index + 1;
        }
        return false;
    }

    static bool ListContainsIgnoreCase(System.Collections.Generic.List<string> values, string expected)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (SipAscii.EqualsIgnoreCase(values[index], expected)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool ListContainsHeaderName(System.Collections.Generic.List<string> values, string expected)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (SipGenericValueAccess.HeaderNamesMatch(values[index], expected)) return true;
            index = index + 1;
        }
        return false;
    }

    static SipExtensionResult CreateFailure(string message)
    {
        SipExtensionResult result = new SipExtensionResult();
        result.ErrorMessage = message;
        return result;
    }
}
