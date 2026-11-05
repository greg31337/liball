namespace sip;

internal static class SipRouteRouting
{
    internal static void ApplyRouteSet(SipMessage? request, SipUri? remoteTarget,
        SipRouteValue[]? routeSet)
    {
        ValidateRequest(request);
        ValidateSipUri(remoteTarget, "remoteTarget");
        ValidateRouteValues(routeSet, "routeSet");
        if (request == null || remoteTarget == null || routeSet == null)
        {
            return;
        }

        SipUri requestUri;
        SipRouteValue[] routes;
        if (routeSet.Length == 0)
        {
            requestUri = CopyUri(remoteTarget);
            routes = new SipRouteValue[0];
        }
        else if (IsLooseRoute(routeSet[0]))
        {
            requestUri = CopyUri(remoteTarget);
            routes = CopyRouteArray(routeSet);
        }
        else
        {
            SipUri firstRoute = GetRouteUri(routeSet[0], "routeSet");
            PreventSecureDowngrade(remoteTarget, firstRoute);
            requestUri = CopyUriForRequest(firstRoute);
            routes = new SipRouteValue[routeSet.Length];
            int index = 1;
            while (index < routeSet.Length)
            {
                routes[index - 1] = CopyRouteValue(routeSet[index]);
                index = index + 1;
            }
            routes[routes.Length - 1] = CreateRouteValue(remoteTarget, false);
        }

        SetRequestRouting(request, requestUri, routes);
    }

    internal static bool PreprocessIncomingRoute(SipMessage? request, SipUri? localRouteUri)
    {
        ValidateRequest(request);
        ValidateSipUri(localRouteUri, "localRouteUri");
        if (request == null || request.RequestUri == null || localRouteUri == null)
        {
            return false;
        }

        SipUri requestUri = CopyUri(request.RequestUri);
        SipRouteValue[] routes = CopyRouteArray(request.RouteValues);
        bool secure = RequiresSecureRouting(request);
        bool changed = false;
        if (SipUriComparison.CompareUriModels(requestUri, localRouteUri))
        {
            if (routes.Length == 0)
            {
                throw new SipProtocolException(
                    "A strict-routed request addressed to this route requires a trailing Route value.",
                    400, request.MethodToken);
            }
            SipUri restoredTarget = GetRouteUri(routes[routes.Length - 1], "routeValues");
            if (secure && restoredTarget.Scheme != SipUriScheme.Sips)
            {
                throw new SipProtocolException(
                    "Strict-route recovery would downgrade a secure request target.",
                    416, request.MethodToken);
            }
            requestUri = CopyUriForRequest(restoredTarget);
            routes = RemoveLastRoute(routes);
            changed = true;
        }

        if (routes.Length > 0)
        {
            SipUri topRoute = GetRouteUri(routes[0], "routeValues");
            if (SipUriComparison.CompareUriModels(topRoute, localRouteUri))
            {
                routes = RemoveFirstRoute(routes);
                changed = true;
            }
        }

        if (changed)
        {
            SetRequestRouting(request, requestUri, routes);
        }
        return changed;
    }

    internal static bool PostprocessStrictRoute(SipMessage? request)
    {
        ValidateRequest(request);
        if (request == null || request.RequestUri == null || request.RouteValues.Length == 0
            || IsLooseRoute(request.RouteValues[0]))
        {
            return false;
        }

        SipUri firstRoute = GetRouteUri(request.RouteValues[0], "routeValues");
        if (RequiresSecureRouting(request) && firstRoute.Scheme != SipUriScheme.Sips)
        {
            throw new SipProtocolException(
                "Strict-route processing would downgrade a secure request target.",
                416, request.MethodToken);
        }

        SipRouteValue[] routes = new SipRouteValue[request.RouteValues.Length];
        int index = 1;
        while (index < request.RouteValues.Length)
        {
            routes[index - 1] = CopyRouteValue(request.RouteValues[index]);
            index = index + 1;
        }
        routes[routes.Length - 1] = CreateRouteValue(request.RequestUri, false);
        SetRequestRouting(request, CopyUriForRequest(firstRoute), routes);
        return true;
    }

    internal static SipRouteValue InsertRecordRoute(SipMessage? request, SipUri? localRouteUri)
    {
        ValidateRequest(request);
        ValidateSipUri(localRouteUri, "localRouteUri");
        if (request == null || localRouteUri == null)
        {
            throw new System.ArgumentException("A request and local route URI are required.", "request");
        }

        SipUri recordedUri = CopyUri(localRouteUri);
        recordedUri.Scheme = RequiresSecureRouting(request) ? SipUriScheme.Sips : SipUriScheme.Sip;
        recordedUri.SchemeToken = null;
        recordedUri.OriginalText = null;
        EnsureLooseRoutingParameter(recordedUri);
        SipRouteValue recorded = CreateRouteValue(recordedUri, true);
        SipHeader header = CreateRouteHeader(SipHeaderNames.RecordRoute,
            new SipRouteValue[] { recorded });
        request.Headers = InsertBeforeFirstNamedHeader(request.Headers,
            header, SipHeaderNames.RecordRoute);
        request.RecordRouteValues = PrependRouteValue(request.RecordRouteValues, recorded);
        request.OriginalBytes = new byte[0];
        return recorded;
    }

    internal static SipRouteValue[] ReverseRouteSet(SipRouteValue[]? recordRoutes)
    {
        ValidateRouteValues(recordRoutes, "recordRoutes");
        if (recordRoutes == null)
        {
            return new SipRouteValue[0];
        }

        SipRouteValue[] result = new SipRouteValue[recordRoutes.Length];
        int index = 0;
        while (index < recordRoutes.Length)
        {
            result[index] = CopyRouteValue(recordRoutes[recordRoutes.Length - index - 1]);
            index = index + 1;
        }
        return result;
    }

    internal static SipRouteValue[] CopyRouteSet(SipRouteValue[]? routeSet)
    {
        ValidateRouteValues(routeSet, "routeSet");
        return routeSet == null ? new SipRouteValue[0] : CopyRouteArray(routeSet);
    }

    internal static SipUri SelectNextHop(SipMessage? request, SipUri? outboundProxy)
    {
        ValidateRequest(request);
        if (request == null || request.RequestUri == null)
        {
            throw new System.ArgumentException("A request URI is required.", "request");
        }

        SipUri selected;
        if (request.RouteValues.Length > 0)
        {
            selected = CopyUri(GetRouteUri(request.RouteValues[0], "routeValues"));
        }
        else if (outboundProxy != null)
        {
            ValidateSipUri(outboundProxy, "outboundProxy");
            selected = CopyUri(outboundProxy);
        }
        else
        {
            selected = CopyUri(request.RequestUri);
        }

        if (request.RequestUri.Scheme == SipUriScheme.Sips && selected.Scheme == SipUriScheme.Sip)
        {
            selected.Scheme = SipUriScheme.Sips;
            selected.SchemeToken = null;
            selected.OriginalText = null;
        }
        return selected;
    }

    internal static SipUri SelectRemoteTarget(SipMessage? message, SipUri? fallback,
        bool secureDialog)
    {
        if (message == null || message.ContactValues == null)
        {
            throw new System.ArgumentException("A message with a Contact collection is required.", "message");
        }

        SipUri? selected = null;
        int index = 0;
        while (index < message.ContactValues.Length)
        {
            SipContactValue? contact = message.ContactValues[index];
            SipUri? uri = contact == null || contact.Wildcard || contact.Address == null
                ? null : contact.Address.Uri;
            if (uri != null)
            {
                ValidateSipUri(uri, "contact");
                if (selected != null)
                {
                    throw new SipProtocolException(
                        "A dialog remote target requires exactly one usable Contact URI.",
                        message.StatusCode, message.MethodToken);
                }
                selected = uri;
            }
            index = index + 1;
        }

        if (selected == null)
        {
            ValidateSipUri(fallback, "fallback");
            selected = fallback;
        }
        if (selected == null)
        {
            throw new SipProtocolException("No remote target is available.",
                message.StatusCode, message.MethodToken);
        }
        if (secureDialog && selected.Scheme != SipUriScheme.Sips)
        {
            throw new SipProtocolException("A secure dialog cannot use a non-SIPS remote target.",
                416, message.MethodToken);
        }
        return CopyUri(selected);
    }

    static void ValidateRequest(SipMessage? request)
    {
        if (request == null || request.Kind != SipMessageKind.Request
            || request.RequestUri == null || request.Headers == null
            || request.RouteValues == null || request.RecordRouteValues == null)
        {
            throw new System.ArgumentException(
                "A SIP request with routing collections and a request URI is required.", "request");
        }
        ValidateSipUri(request.RequestUri, "requestUri");
        ValidateRouteValues(request.RouteValues, "routeValues");
        ValidateRouteValues(request.RecordRouteValues, "recordRouteValues");
    }

    static void ValidateRouteValues(SipRouteValue[]? values, string parameterName)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException(parameterName);
        }
        int index = 0;
        while (index < values.Length)
        {
            GetRouteUri(values[index], parameterName);
            index = index + 1;
        }
    }

    static void ValidateSipUri(SipUri? uri, string parameterName)
    {
        if (uri == null || uri.Scheme != SipUriScheme.Sip && uri.Scheme != SipUriScheme.Sips
            || SipUriValidator.ValidateUriModel(uri).Length > 0)
        {
            throw new System.ArgumentException("A valid SIP or SIPS URI is required.", parameterName);
        }
    }

    static SipUri GetRouteUri(SipRouteValue? route, string parameterName)
    {
        SipUri? uri = route == null || route.Address == null ? null : route.Address.Uri;
        ValidateSipUri(uri, parameterName);
        if (uri == null)
        {
            throw new System.ArgumentException("A route URI is required.", parameterName);
        }
        return uri;
    }

    static void PreventSecureDowngrade(SipUri protectedTarget, SipUri routingTarget)
    {
        if (protectedTarget.Scheme == SipUriScheme.Sips
            && routingTarget.Scheme != SipUriScheme.Sips)
        {
            throw new SipProtocolException(
                "Strict routing cannot downgrade a SIPS target to SIP.", 416, null);
        }
    }

    static bool RequiresSecureRouting(SipMessage request)
    {
        if (request.RequestUri != null && request.RequestUri.Scheme == SipUriScheme.Sips)
        {
            return true;
        }
        if (request.RouteValues.Length > 0)
        {
            SipUri route = GetRouteUri(request.RouteValues[0], "routeValues");
            return route.Scheme == SipUriScheme.Sips;
        }
        return false;
    }

    static bool IsLooseRoute(SipRouteValue route)
    {
        SipUri uri = GetRouteUri(route, "route");
        int index = 0;
        while (index < uri.Parameters.Length)
        {
            SipUriParameter? parameter = uri.Parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(parameter.Name, "lr"))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void EnsureLooseRoutingParameter(SipUri uri)
    {
        int index = 0;
        while (index < uri.Parameters.Length)
        {
            SipUriParameter? parameter = uri.Parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(parameter.Name, "lr"))
            {
                parameter.Value = null;
                parameter.HasValue = false;
                parameter.OriginalText = null;
                parameter.Quoted = false;
                return;
            }
            index = index + 1;
        }

        SipUriParameter added = new SipUriParameter();
        added.Name = "lr";
        SipUriParameter[] parameters = new SipUriParameter[uri.Parameters.Length + 1];
        index = 0;
        while (index < uri.Parameters.Length)
        {
            parameters[index] = uri.Parameters[index];
            index = index + 1;
        }
        parameters[parameters.Length - 1] = added;
        uri.Parameters = parameters;
    }

    static void SetRequestRouting(SipMessage request, SipUri requestUri,
        SipRouteValue[] routes)
    {
        request.RequestUri = requestUri;
        request.RouteValues = routes;
        request.Headers = ReplaceNamedHeaders(request.Headers, SipHeaderNames.Route, routes);
        request.OriginalBytes = new byte[0];
    }

    static SipHeader[] ReplaceNamedHeaders(SipHeader[] headers, string name,
        SipRouteValue[] values)
    {
        int firstIndex = FindFirstNamedHeaderIndex(headers, name);
        int matchingCount = CountNamedHeaders(headers, name);
        int addedCount = values.Length == 0 ? 0 : 1;
        SipHeader[] result = new SipHeader[headers.Length - matchingCount + addedCount];
        int insertionIndex = firstIndex;
        if (insertionIndex < 0)
        {
            insertionIndex = FindRouteInsertionIndex(headers);
        }
        int sourceIndex = 0;
        int destinationIndex = 0;
        bool inserted = false;
        while (sourceIndex < headers.Length)
        {
            if (!inserted && destinationIndex == insertionIndex && values.Length > 0)
            {
                result[destinationIndex] = CreateRouteHeader(name, values);
                destinationIndex = destinationIndex + 1;
                inserted = true;
            }
            SipHeader? header = headers[sourceIndex];
            if (header == null || !SipAscii.EqualsIgnoreCase(header.Name, name))
            {
                result[destinationIndex] = headers[sourceIndex];
                destinationIndex = destinationIndex + 1;
            }
            sourceIndex = sourceIndex + 1;
        }
        if (!inserted && values.Length > 0)
        {
            result[destinationIndex] = CreateRouteHeader(name, values);
        }
        return result;
    }

    static SipHeader[] InsertBeforeFirstNamedHeader(SipHeader[] headers, SipHeader added,
        string name)
    {
        int insertionIndex = FindFirstNamedHeaderIndex(headers, name);
        if (insertionIndex < 0)
        {
            insertionIndex = FindRouteInsertionIndex(headers);
        }
        SipHeader[] result = new SipHeader[headers.Length + 1];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (destinationIndex < result.Length)
        {
            if (destinationIndex == insertionIndex)
            {
                result[destinationIndex] = added;
            }
            else
            {
                result[destinationIndex] = headers[sourceIndex];
                sourceIndex = sourceIndex + 1;
            }
            destinationIndex = destinationIndex + 1;
        }
        return result;
    }

    static int FindFirstNamedHeaderIndex(SipHeader[] headers, string name)
    {
        int index = 0;
        while (index < headers.Length)
        {
            SipHeader? header = headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(header.Name, name))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static int CountNamedHeaders(SipHeader[] headers, string name)
    {
        int count = 0;
        int index = 0;
        while (index < headers.Length)
        {
            SipHeader? header = headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(header.Name, name))
            {
                count = count + 1;
            }
            index = index + 1;
        }
        return count;
    }

    static int FindRouteInsertionIndex(SipHeader[] headers)
    {
        int insertionIndex = 0;
        int index = 0;
        while (index < headers.Length)
        {
            SipHeader? header = headers[index];
            if (header != null && (SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.Via)
                || SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.CompactVia)))
            {
                insertionIndex = index + 1;
            }
            index = index + 1;
        }
        return insertionIndex;
    }

    static SipHeader CreateRouteHeader(string name, SipRouteValue[] values)
    {
        SipStructuredHeaderResult structured = new SipStructuredHeaderResult();
        structured.Success = true;
        structured.RouteValues = values;
        SipWriteResult formatted = SipStructuredHeaderCodec.FormatStructuredHeaderValue(
            name, structured, new SipWriteOptions());
        if (!formatted.Success)
        {
            throw new System.ArgumentException(formatted.ErrorMessage == null
                ? "The route values are invalid." : formatted.ErrorMessage, "values");
        }
        string line = System.Text.Encoding.UTF8.GetString(formatted.Bytes);
        int colon = line.IndexOf(':');
        int start = colon + 1;
        while (start < line.Length && (line[start] == ' ' || line[start] == '\t'))
        {
            start = start + 1;
        }
        int end = line.Length - 2;
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = line.Substring(start, end - start);
        return header;
    }

    static SipRouteValue[] PrependRouteValue(SipRouteValue[] values, SipRouteValue added)
    {
        SipRouteValue[] result = new SipRouteValue[values.Length + 1];
        result[0] = added;
        int index = 0;
        while (index < values.Length)
        {
            result[index + 1] = values[index];
            index = index + 1;
        }
        return result;
    }

    static SipRouteValue[] RemoveFirstRoute(SipRouteValue[] routes)
    {
        SipRouteValue[] result = new SipRouteValue[routes.Length - 1];
        int index = 1;
        while (index < routes.Length)
        {
            result[index - 1] = routes[index];
            index = index + 1;
        }
        return result;
    }

    static SipRouteValue[] RemoveLastRoute(SipRouteValue[] routes)
    {
        SipRouteValue[] result = new SipRouteValue[routes.Length - 1];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = routes[index];
            index = index + 1;
        }
        return result;
    }

    static SipRouteValue[] CopyRouteArray(SipRouteValue[] source)
    {
        SipRouteValue[] result = new SipRouteValue[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = CopyRouteValue(source[index]);
            index = index + 1;
        }
        return result;
    }

    static SipRouteValue CopyRouteValue(SipRouteValue source)
    {
        SipRouteValue copy = new SipRouteValue();
        copy.Address = CopyNameAddress(source.Address);
        copy.LooseRouting = IsLooseRoute(copy);
        return copy;
    }

    static SipRouteValue CreateRouteValue(SipUri source, bool looseRouting)
    {
        SipRouteValue route = new SipRouteValue();
        SipNameAddress address = new SipNameAddress();
        address.Uri = CopyUri(source);
        route.Address = address;
        route.LooseRouting = looseRouting;
        return route;
    }

    static SipNameAddress? CopyNameAddress(SipNameAddress? source)
    {
        if (source == null)
        {
            return null;
        }
        SipNameAddress copy = new SipNameAddress();
        copy.DisplayName = source.DisplayName;
        copy.Uri = source.Uri == null ? null : CopyUri(source.Uri);
        copy.Parameters = CopyParameterArray(source.Parameters);
        copy.OriginalText = source.OriginalText;
        return copy;
    }

    static SipUri CopyUriForRequest(SipUri source)
    {
        SipUri copy = CopyUri(source);
        copy.Headers = new SipUriHeader[0];
        copy.OriginalText = null;
        return copy;
    }

    static SipUri CopyUri(SipUri source)
    {
        SipUri copy = new SipUri();
        copy.Scheme = source.Scheme;
        copy.SchemeToken = source.SchemeToken;
        copy.User = source.User;
        copy.Password = source.Password;
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.Parameters = CopyParameterArray(source.Parameters);
        copy.Headers = CopyUriHeaderArray(source.Headers);
        copy.OpaquePart = source.OpaquePart;
        copy.OriginalText = source.OriginalText;
        return copy;
    }

    static SipUriParameter[] CopyParameterArray(SipUriParameter[]? source)
    {
        if (source == null)
        {
            return new SipUriParameter[0];
        }
        SipUriParameter[] result = new SipUriParameter[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipUriParameter item = new SipUriParameter();
            SipUriParameter? original = source[index];
            if (original != null)
            {
                item.Name = original.Name;
                item.Value = original.Value;
                item.HasValue = original.HasValue;
                item.OriginalText = original.OriginalText;
                item.Quoted = original.Quoted;
            }
            result[index] = item;
            index = index + 1;
        }
        return result;
    }

    static SipUriHeader[] CopyUriHeaderArray(SipUriHeader[]? source)
    {
        if (source == null)
        {
            return new SipUriHeader[0];
        }
        SipUriHeader[] result = new SipUriHeader[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipUriHeader item = new SipUriHeader();
            SipUriHeader? original = source[index];
            if (original != null)
            {
                item.Name = original.Name;
                item.Value = original.Value;
            }
            result[index] = item;
            index = index + 1;
        }
        return result;
    }
}
