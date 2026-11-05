namespace sip;

internal static class SipMessageValidator
{
    internal static SipValidationIssue[] ValidateMessageModel(SipMessage? message, bool strict)
    {
        System.Collections.Generic.List<SipValidationIssue> issues =
            new System.Collections.Generic.List<SipValidationIssue>();
        if (message == null)
        {
            issues.Add(CreateIssue("message-required", "A SIP message is required.",
                "message", null, strict));
            return issues.ToArray();
        }

        ValidateStartLine(message, issues, strict);
        if (message.Headers == null)
        {
            issues.Add(CreateIssue("headers-required", "The message header collection cannot be null.",
                "headers", null, strict));
        }
        else
        {
            ValidateHeaderModels(message.Headers, issues, strict);
            ValidateMandatoryHeaders(message, issues, strict);
            ValidateSingletonHeaders(message.Headers, issues, strict);
            ValidateCSeqMethod(message, issues, strict);
            ValidateBodyHeaders(message, issues, strict);
        }
        ValidateUriRules(message, issues, strict);
        if (message.Body == null)
            issues.Add(CreateIssue("body-required", "The message body byte collection cannot be null.",
                "body", null, strict));
        if (message.OriginalBytes == null)
            issues.Add(CreateIssue("original-bytes-required",
                "The original message byte collection cannot be null.", "originalBytes", null, strict));
        return issues.ToArray();
    }

    internal static bool HasErrorIssues(SipValidationIssue[] issues)
    {
        int index = 0;
        while (index < issues.Length)
        {
            if (issues[index].Severity == SipValidationSeverity.Error) return true;
            index = index + 1;
        }
        return false;
    }

    static void ValidateStartLine(SipMessage message,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (!SipAscii.EqualsIgnoreCase(message.ProtocolVersion, SipTransportNames.ProtocolVersion))
            issues.Add(CreateIssue("protocol-version", "The SIP protocol version must be SIP/2.0.",
                "protocolVersion", null, strict));
        if (message.Kind == SipMessageKind.Request)
        {
            if (!SipAscii.IsToken(message.MethodToken))
                issues.Add(CreateIssue("request-method", "A request requires a valid method token.",
                    "methodToken", null, strict));
            SipMethod tokenMethod = GetMethod(message.MethodToken);
            if (message.Method == SipMethod.Unknown && tokenMethod != SipMethod.Unknown
                || message.Method != SipMethod.Unknown && tokenMethod != message.Method)
                issues.Add(CreateIssue("request-method-model",
                    "The recognized request method and method token do not agree.",
                    "method", null, strict));
            if (message.RequestUri == null)
                issues.Add(CreateIssue("request-uri", "A request requires a request URI.",
                    "requestUri", null, strict));
            if (message.StatusCode != 0 || message.StatusClass != SipStatusClass.Unknown
                || message.ReasonPhrase != null)
                issues.Add(CreateIssue("request-response-fields",
                    "A request cannot carry response status fields.", "statusCode", null, strict));
        }
        else if (message.Kind == SipMessageKind.Response)
        {
            if (message.RequestUri != null || message.Method != SipMethod.Unknown || message.MethodToken != null)
                issues.Add(CreateIssue("response-request-fields",
                    "A response cannot carry request method or URI fields.", "requestUri", null, strict));
            if (message.StatusCode < 100 || message.StatusCode > 699)
                issues.Add(CreateIssue("response-status",
                    "A response status code must be from 100 through 699.", "statusCode", null, strict));
            SipStatusClass expected = GetStatusClass(message.StatusCode);
            if (message.StatusClass != expected)
                issues.Add(CreateIssue("response-status-class",
                    "The response status class does not agree with its status code.",
                    "statusClass", null, strict));
            if (message.ReasonPhrase == null)
                issues.Add(CreateIssue("response-reason",
                    "A response requires a reason phrase value, which may be empty.",
                    "reasonPhrase", null, strict));
        }
        else
        {
            issues.Add(CreateIssue("message-kind",
                "The message kind must identify a request or response.", "kind", null, strict));
        }
    }

    static void ValidateHeaderModels(SipHeader[] headers,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        int index = 0;
        while (index < headers.Length)
        {
            SipHeader? header = headers[index];
            SipValidationIssue[] headerIssues = SipHeaderValidator.ValidateHeaderModel(header);
            int issueIndex = 0;
            while (issueIndex < headerIssues.Length)
            {
                SipValidationIssue source = headerIssues[issueIndex];
                string code = source.Code == null ? "header-model" : source.Code;
                string message = source.Message == null ? "The header field is invalid." : source.Message;
                string? headerName = header == null ? null : header.Name;
                issues.Add(CreateIssue(code, message, "headers[" + index.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + "]", headerName, strict));
                issueIndex = issueIndex + 1;
            }
            index = index + 1;
        }
    }

    static void ValidateMandatoryHeaders(SipMessage message,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        SipHeader[] headers = message.Headers;
        AddMissingHeader(headers, SipHeaderNames.Via, issues, strict);
        AddMissingHeader(headers, SipHeaderNames.From, issues, strict);
        AddMissingHeader(headers, SipHeaderNames.To, issues, strict);
        AddMissingHeader(headers, SipHeaderNames.CallId, issues, strict);
        AddMissingHeader(headers, SipHeaderNames.CSeq, issues, strict);
        if (message.Kind == SipMessageKind.Request)
        {
            AddMissingHeader(headers, SipHeaderNames.MaxForwards, issues, strict);
            if (message.MethodToken == SipMethods.Invite)
                AddMissingHeader(headers, SipHeaderNames.Contact, issues, strict);
        }
        if (message.Kind == SipMessageKind.Response && message.StatusClass == SipStatusClass.Successful
            && message.CSeq != null && message.CSeq.MethodToken == SipMethods.Invite)
            AddMissingHeader(headers, SipHeaderNames.Contact, issues, strict);
        if (message.Kind == SipMessageKind.Response
            && CountHeaderGroup(headers, SipHeaderNames.MaxForwards) > 0)
            issues.Add(CreateIssue("response-max-forwards",
                "Max-Forwards is a request header field and cannot appear in a response.",
                "headers", SipHeaderNames.MaxForwards, strict));
    }

    static void ValidateSingletonHeaders(SipHeader[] headers,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        AddSingletonHeader(headers, SipHeaderNames.From, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.To, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.CallId, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.CSeq, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.MaxForwards, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.ContentLength, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.ContentType, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.ContentDisposition, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.Date, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.Expires, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.MinExpires, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.RetryAfter, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.Server, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.UserAgent, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.Subject, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.Event, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.SubscriptionState, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.SipETag, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.SipIfMatch, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.ReferTo, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.ReferredBy, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.ReferSub, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.Replaces, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.Join, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.TargetDialog, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.RSeq, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.RAck, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.SessionExpires, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.MinSE, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.SessionId, issues, strict);
        AddSingletonHeader(headers, SipHeaderNames.PPreferredIdentity, issues, strict);
    }

    static void ValidateCSeqMethod(SipMessage message,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (message.Kind != SipMessageKind.Request
            || CountHeaderGroup(message.Headers, SipHeaderNames.CSeq) == 0) return;
        if (message.CSeq == null || !SipAscii.IsToken(message.CSeq.MethodToken))
        {
            issues.Add(CreateIssue("cseq-model",
                "The parsed CSeq method is unavailable or invalid.", "cSeq",
                SipHeaderNames.CSeq, strict));
            return;
        }
        if (message.MethodToken != message.CSeq.MethodToken)
            issues.Add(CreateIssue("cseq-method",
                "The request method must match the method in CSeq.", "cSeq.methodToken",
                SipHeaderNames.CSeq, strict));
        SipMethod cseqTokenMethod = GetMethod(message.CSeq.MethodToken);
        if (message.CSeq.Method != cseqTokenMethod)
            issues.Add(CreateIssue("cseq-method-model",
                "The recognized CSeq method and method token do not agree.", "cSeq.method",
                SipHeaderNames.CSeq, strict));
    }

    static void ValidateBodyHeaders(SipMessage message,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        int bodyLength = message.Body == null ? 0 : message.Body.Length;
        int contentTypeCount = CountHeaderGroup(message.Headers, SipHeaderNames.ContentType);
        if (bodyLength > 0 && contentTypeCount == 0)
            issues.Add(CreateIssue("content-type-required",
                "A message with a body requires a Content-Type header field.",
                "headers", SipHeaderNames.ContentType, strict));

        string? lengthText = GetFirstHeaderValue(message.Headers, SipHeaderNames.ContentLength);
        if (lengthText != null)
        {
            SipNumericParseResult length = SipNumericParser.ParseNonNegativeInt32Text(lengthText, int.MaxValue);
            if (!length.Success || length.Value != bodyLength)
                issues.Add(CreateIssue("content-length",
                    "Content-Length must equal the number of message body bytes.",
                    "headers", SipHeaderNames.ContentLength, strict));
        }
    }

    static void ValidateUriRules(SipMessage message,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        bool secureTarget = false;
        if (message.RequestUri != null)
        {
            if (SipUriValidator.ValidateUriModel(message.RequestUri).Length > 0)
                issues.Add(CreateIssue("request-uri", "The request URI model is invalid.",
                    "requestUri", null, strict));
            if (message.RequestUri.Headers.Length > 0)
                issues.Add(CreateIssue("request-uri-headers",
                    "A SIP Request-URI cannot contain URI headers.",
                    "requestUri.headers", null, strict));
            secureTarget = message.RequestUri.Scheme == SipUriScheme.Sips;
        }

        if (message.RouteValues == null)
            issues.Add(CreateIssue("route-values-required",
                "The parsed Route value collection cannot be null.", "routeValues",
                SipHeaderNames.Route, strict));
        else
        {
            int routeIndex = 0;
            while (routeIndex < message.RouteValues.Length)
            {
                SipRouteValue? route = message.RouteValues[routeIndex];
                SipUri? uri = route == null || route.Address == null ? null : route.Address.Uri;
                if (!IsSipOrSipsUri(uri))
                    issues.Add(CreateIssue("route-uri",
                        "Route values require SIP or SIPS URIs.", "routeValues",
                        SipHeaderNames.Route, strict));
                if (routeIndex == 0 && uri != null && uri.Scheme == SipUriScheme.Sips)
                    secureTarget = true;
                routeIndex = routeIndex + 1;
            }
        }

        ValidateRecordRouteUris(message.RecordRouteValues, issues, strict);
        ValidateContactUris(message, secureTarget, issues, strict);
    }

    static void ValidateRecordRouteUris(SipRouteValue[]? routes,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (routes == null)
        {
            issues.Add(CreateIssue("record-route-values-required",
                "The parsed Record-Route value collection cannot be null.", "recordRouteValues",
                SipHeaderNames.RecordRoute, strict));
            return;
        }
        int index = 0;
        while (index < routes.Length)
        {
            SipRouteValue? route = routes[index];
            SipUri? uri = route == null || route.Address == null ? null : route.Address.Uri;
            if (!IsSipOrSipsUri(uri))
                issues.Add(CreateIssue("record-route-uri",
                    "Record-Route values require SIP or SIPS URIs.", "recordRouteValues",
                    SipHeaderNames.RecordRoute, strict));
            index = index + 1;
        }
    }

    static void ValidateContactUris(SipMessage message, bool secureTarget,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (message.ContactValues == null)
        {
            issues.Add(CreateIssue("contact-values-required",
                "The parsed Contact value collection cannot be null.", "contactValues",
                SipHeaderNames.Contact, strict));
            return;
        }
        bool wildcard = false;
        int index = 0;
        while (index < message.ContactValues.Length)
        {
            SipContactValue? contact = message.ContactValues[index];
            if (contact == null)
                issues.Add(CreateIssue("contact-value", "A Contact value cannot be null.",
                    "contactValues", SipHeaderNames.Contact, strict));
            else if (contact.Wildcard)
                wildcard = true;
            else
            {
                SipUri? uri = contact.Address == null ? null : contact.Address.Uri;
                if (!IsSipOrSipsUri(uri))
                    issues.Add(CreateIssue("contact-uri",
                        "Contact values require SIP or SIPS URIs.", "contactValues",
                        SipHeaderNames.Contact, strict));
                else if (secureTarget && uri != null && uri.Scheme != SipUriScheme.Sips)
                    issues.Add(CreateIssue("sips-contact",
                        "A SIPS request target or top Route requires SIPS Contact values.",
                        "contactValues", SipHeaderNames.Contact, strict));
            }
            index = index + 1;
        }
        if (wildcard && message.ContactValues.Length != 1)
            issues.Add(CreateIssue("contact-wildcard",
                "A wildcard Contact cannot be combined with other Contact values.",
                "contactValues", SipHeaderNames.Contact, strict));
        if (wildcard && message.Kind == SipMessageKind.Request
            && message.MethodToken != SipMethods.Register)
            issues.Add(CreateIssue("contact-wildcard-method",
                "A wildcard Contact is valid only in a REGISTER request.",
                "contactValues", SipHeaderNames.Contact, strict));
    }

    static bool IsSipOrSipsUri(SipUri? uri)
    {
        return uri != null && (uri.Scheme == SipUriScheme.Sip || uri.Scheme == SipUriScheme.Sips)
            && SipUriValidator.ValidateUriModel(uri).Length == 0;
    }

    static void AddMissingHeader(SipHeader[] headers, string name,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (CountHeaderGroup(headers, name) == 0)
            issues.Add(CreateIssue("header-missing", "The " + name + " header field is required.",
                "headers", name, strict));
    }

    static void AddSingletonHeader(SipHeader[] headers, string name,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (CountHeaderGroup(headers, name) > 1)
            issues.Add(CreateIssue("header-duplicate",
                "The " + name + " header field cannot be repeated.",
                "headers", name, strict));
    }

    static int CountHeaderGroup(SipHeader[] headers, string name)
    {
        int count = 0;
        int index = 0;
        while (index < headers.Length)
        {
            SipHeader? header = headers[index];
            if (header != null && HeaderNameMatches(header.Name, name)) count = count + 1;
            index = index + 1;
        }
        return count;
    }

    static string? GetFirstHeaderValue(SipHeader[] headers, string name)
    {
        int index = 0;
        while (index < headers.Length)
        {
            SipHeader? header = headers[index];
            if (header != null && HeaderNameMatches(header.Name, name)) return header.Value;
            index = index + 1;
        }
        return null;
    }

    static bool HeaderNameMatches(string? actual, string expected)
    {
        if (SipAscii.EqualsIgnoreCase(actual, expected)) return true;
        if (SipAscii.EqualsIgnoreCase(expected, SipHeaderNames.Via))
            return SipAscii.EqualsIgnoreCase(actual, SipHeaderNames.CompactVia);
        if (SipAscii.EqualsIgnoreCase(expected, SipHeaderNames.From))
            return SipAscii.EqualsIgnoreCase(actual, SipHeaderNames.CompactFrom);
        if (SipAscii.EqualsIgnoreCase(expected, SipHeaderNames.To))
            return SipAscii.EqualsIgnoreCase(actual, SipHeaderNames.CompactTo);
        if (SipAscii.EqualsIgnoreCase(expected, SipHeaderNames.CallId))
            return SipAscii.EqualsIgnoreCase(actual, SipHeaderNames.CompactCallId);
        if (SipAscii.EqualsIgnoreCase(expected, SipHeaderNames.Contact))
            return SipAscii.EqualsIgnoreCase(actual, SipHeaderNames.CompactContact);
        if (SipAscii.EqualsIgnoreCase(expected, SipHeaderNames.ContentLength))
            return SipAscii.EqualsIgnoreCase(actual, SipHeaderNames.CompactContentLength);
        if (SipAscii.EqualsIgnoreCase(expected, SipHeaderNames.ContentType))
            return SipAscii.EqualsIgnoreCase(actual, SipHeaderNames.CompactContentType);
        if (SipAscii.EqualsIgnoreCase(expected, SipHeaderNames.Subject))
            return SipAscii.EqualsIgnoreCase(actual, SipHeaderNames.CompactSubject);
        if (SipAscii.EqualsIgnoreCase(expected, SipHeaderNames.Event))
            return SipAscii.EqualsIgnoreCase(actual, SipHeaderNames.CompactEvent);
        if (SipAscii.EqualsIgnoreCase(expected, SipHeaderNames.ReferTo))
            return SipAscii.EqualsIgnoreCase(actual, SipHeaderNames.CompactReferTo);
        if (SipAscii.EqualsIgnoreCase(expected, SipHeaderNames.ReferredBy))
            return SipAscii.EqualsIgnoreCase(actual, SipHeaderNames.CompactReferredBy);
        if (SipAscii.EqualsIgnoreCase(expected, SipHeaderNames.SessionExpires))
            return SipAscii.EqualsIgnoreCase(actual, SipHeaderNames.CompactSessionExpires);
        return false;
    }

    static SipMethod GetMethod(string? method)
    {
        switch (method)
        {
            case SipMethods.Ack: return SipMethod.Ack;
            case SipMethods.Bye: return SipMethod.Bye;
            case SipMethods.Cancel: return SipMethod.Cancel;
            case SipMethods.Info: return SipMethod.Info;
            case SipMethods.Invite: return SipMethod.Invite;
            case SipMethods.Message: return SipMethod.Message;
            case SipMethods.Notify: return SipMethod.Notify;
            case SipMethods.Options: return SipMethod.Options;
            case SipMethods.Prack: return SipMethod.Prack;
            case SipMethods.Publish: return SipMethod.Publish;
            case SipMethods.Refer: return SipMethod.Refer;
            case SipMethods.Register: return SipMethod.Register;
            case SipMethods.Subscribe: return SipMethod.Subscribe;
            case SipMethods.Update: return SipMethod.Update;
            default: return SipMethod.Unknown;
        }
    }

    static SipStatusClass GetStatusClass(int statusCode)
    {
        switch (statusCode / 100)
        {
            case 1: return SipStatusClass.Provisional;
            case 2: return SipStatusClass.Successful;
            case 3: return SipStatusClass.Redirection;
            case 4: return SipStatusClass.ClientError;
            case 5: return SipStatusClass.ServerError;
            case 6: return SipStatusClass.GlobalFailure;
            default: return SipStatusClass.Unknown;
        }
    }

    static SipValidationIssue CreateIssue(string code, string message, string path,
        string? headerName, bool strict)
    {
        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = strict ? SipValidationSeverity.Error : SipValidationSeverity.Warning;
        issue.Code = code;
        issue.Message = message;
        issue.Path = path;
        issue.HeaderName = headerName;
        return issue;
    }
}
