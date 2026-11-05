namespace sip;

internal class SipEventPackageRegistryCore
{
    object gate = new object();
    System.Collections.Generic.List<SipEventPackageEntry> packages =
        new System.Collections.Generic.List<SipEventPackageEntry>();
    System.Collections.Generic.List<SipEventPackageState> states =
        new System.Collections.Generic.List<SipEventPackageState>();

    internal SipEventPackageRegistryCore()
    {
        AddBuiltIn(SipEventPackages.Presence,
            new string[] { SipMediaTypes.Pidf, SipMediaTypes.PidfDiff,
                SipMediaTypes.ResourceListMetaInformation }, true);
        AddBuiltIn(SipEventPackages.WatcherInformation,
            new string[] { SipMediaTypes.WatcherInfo,
                SipMediaTypes.ResourceListMetaInformation }, true);
        AddBuiltIn(SipEventPackages.Dialog,
            new string[] { SipMediaTypes.DialogInfo,
                SipMediaTypes.ResourceListMetaInformation }, true);
        AddBuiltIn(SipEventPackages.Registration,
            new string[] { SipMediaTypes.RegistrationInfo,
                SipMediaTypes.ResourceListMetaInformation }, true);
        AddBuiltIn(SipEventPackages.MessageSummary,
            new string[] { SipMediaTypes.MessageSummary,
                SipMediaTypes.ResourceListMetaInformation }, true);
        AddBuiltIn(SipEventPackages.ResourceList,
            new string[] { SipMediaTypes.ResourceLists,
                SipMediaTypes.ResourceListMetaInformation }, true);
    }

    internal bool RegisterPackage(
        string eventPackage, ISipEventPackage handler, SipMediaType[] contentTypes)
    {
        ValidateRegistration(eventPackage, handler, contentTypes);
        lock (gate)
        {
            if (FindPackageIndex(eventPackage) >= 0) return false;
            SipEventPackageEntry entry = new SipEventPackageEntry();
            entry.EventPackage = eventPackage;
            entry.Handler = handler;
            entry.ContentTypes = CopyMediaTypes(contentTypes);
            packages.Add(entry);
            return true;
        }
    }

    internal bool RemovePackage(string eventPackage)
    {
        ValidatePackageToken(eventPackage);
        lock (gate)
        {
            int index = FindPackageIndex(eventPackage);
            if (index < 0 || packages[index].BuiltIn) return false;
            packages.RemoveAt(index);
            RemovePackageStates(eventPackage);
            return true;
        }
    }

    internal SipEventPackageRegistration[] GetRegistrations()
    {
        lock (gate)
        {
            SipEventPackageRegistration[] result =
                new SipEventPackageRegistration[packages.Count];
            int index = 0;
            while (index < packages.Count)
            {
                SipEventPackageEntry entry = packages[index];
                SipEventPackageRegistration registration =
                    new SipEventPackageRegistration();
                registration.EventPackage = entry.EventPackage;
                registration.ContentTypes = CopyMediaTypes(entry.ContentTypes);
                registration.BuiltIn = entry.BuiltIn;
                registration.SupportsResourceLists = entry.SupportsResourceLists;
                result[index] = registration;
                index = index + 1;
            }
            return result;
        }
    }

    internal SipEventPackageResult ProcessBody(SipEventPackageRequest request)
    {
        if (request == null) throw new System.ArgumentNullException("request");
        SipEventPackageRequest detached = CopyRequest(request);
        if (request.EventPackage == null || !SipAscii.IsToken(request.EventPackage))
            return CreateFailure(detached, SipStatusCodes.BadEvent,
                "A valid event package token is required.", false);
        SipEventPackageEntry? entry = FindPackageCopy(request.EventPackage);
        if (entry == null)
            return CreateFailure(detached, SipStatusCodes.BadEvent,
                "The event package is not registered.", false);
        if (request.ContentType == null
            || !ContainsMediaType(entry.ContentTypes, request.ContentType))
            return CreateFailure(detached, SipStatusCodes.UnsupportedMediaType,
                "The event body media type is not accepted by this package.", entry.BuiltIn);
        if (request.Body == null || request.Body.Length == 0)
            return CreateFailure(detached, SipStatusCodes.BadRequest,
                "An event package body is required.", entry.BuiltIn);
        if (entry.BuiltIn && request.Options == null)
            return CreateFailure(detached, SipStatusCodes.BadRequest,
                "Event body parse options are required.", true);
        if (!entry.BuiltIn)
        {
            ISipEventPackage handler = entry.Handler == null
                ? throw new System.InvalidOperationException(
                    "The custom event package handler is unavailable.")
                : entry.Handler;
            SipEventPackageResult custom = handler.ProcessEventBody(detached);
            if (custom == null)
                throw new System.InvalidOperationException(
                    "An event package handler returned no result.");
            custom.Request = detached;
            custom.EventPackage = detached.EventPackage;
            custom.ContentType = CopyMediaType(detached.ContentType);
            custom.CorrelationId = detached.CorrelationId;
            return custom;
        }
        return ProcessBuiltIn(detached, entry);
    }

    internal SipEventPackageState? GetState(string subscriptionId)
    {
        ValidateSubscriptionIdentifier(subscriptionId);
        lock (gate)
        {
            int index = FindStateIndex(subscriptionId);
            return index < 0 ? null : CopyState(states[index]);
        }
    }

    internal bool ClearState(string subscriptionId)
    {
        ValidateSubscriptionIdentifier(subscriptionId);
        lock (gate)
        {
            int index = FindStateIndex(subscriptionId);
            if (index < 0) return false;
            states.RemoveAt(index);
            return true;
        }
    }

    SipEventPackageResult ProcessBuiltIn(
        SipEventPackageRequest request, SipEventPackageEntry entry)
    {
        if (request.Options == null)
            return CreateFailure(request, SipStatusCodes.BadRequest,
                "Event body parse options are required.", true);
        SipParseResult parsed = SipXmlBodyParser.ParseBodyBytes(
            request.Body, request.ContentType, request.Options);
        if (parsed.Status != SipParseStatus.Complete || parsed.XmlBody == null)
            return CreateFailure(request, SipStatusCodes.BadRequest,
                parsed.ErrorMessage == null
                    ? "The event package body is malformed." : parsed.ErrorMessage, true);
        SipEventPackageResult result = DescribeBuiltInBody(request, parsed.XmlBody);
        if (!BodyMatchesPackage(entry.EventPackage, parsed.XmlBody, result.ResourceList))
            return CreateFailure(request, SipStatusCodes.UnsupportedMediaType,
                "The event body does not match the registered package.", true);
        SipEventPackageResult? stateFailure = CheckAndStoreState(result);
        return stateFailure == null ? result : stateFailure;
    }

    SipEventPackageResult DescribeBuiltInBody(
        SipEventPackageRequest request, SipXmlBody body)
    {
        SipEventPackageResult result = new SipEventPackageResult();
        result.Request = CopyRequest(request);
        result.EventPackage = request.EventPackage;
        result.ContentType = CopyMediaType(request.ContentType);
        result.XmlBody = CopyXmlBody(body);
        result.Accepted = true;
        result.StatusCode = SipStatusCodes.Ok;
        result.BuiltIn = true;
        result.FullState = true;
        result.CorrelationId = request.CorrelationId;
        if (body.XmlText != null && !IsMessageSummary(body.MediaType))
        {
            SipXmlScanResult scan = new SipXmlScanner(
                body.XmlText, request.Options.MaxXmlDepth).ScanDocument();
            string? subtype = body.MediaType == null ? null : body.MediaType.Subtype;
            if (SipAscii.EqualsIgnoreCase(subtype, "pidf-diff+xml"))
            {
                result.FullState = scan.RootName == "pidf-full";
                result.Entity = SipXmlMediaProfiles.FindAttributeValue(scan, "entity");
                result.Version = ParseUnsigned32(
                    SipXmlMediaProfiles.FindAttributeValue(scan, "version"));
            }
            else if (SipAscii.EqualsIgnoreCase(subtype, "watcherinfo+xml")
                || SipAscii.EqualsIgnoreCase(subtype, "dialog-info+xml")
                || SipAscii.EqualsIgnoreCase(subtype, "reginfo+xml"))
            {
                result.FullState = SipXmlMediaProfiles.FindAttributeValue(scan, "state") == "full";
                result.Version = ParseUnsigned32(
                    SipXmlMediaProfiles.FindAttributeValue(scan, "version"));
                result.Entity = SipXmlMediaProfiles.FindAttributeValue(scan, "entity");
            }
            else if (SipAscii.EqualsIgnoreCase(subtype, "rlmi+xml"))
            {
                string? fullState = SipXmlMediaProfiles.FindAttributeValue(scan, "fullState");
                result.FullState = fullState == "true" || fullState == "1";
                result.Version = ParseUnsigned32(
                    SipXmlMediaProfiles.FindAttributeValue(scan, "version"));
                result.Entity = SipXmlMediaProfiles.FindAttributeValue(scan, "uri");
                result.ResourceList = true;
            }
            else
            {
                result.Entity = SipXmlMediaProfiles.FindAttributeValue(scan, "entity");
                result.ResourceList = SipAscii.EqualsIgnoreCase(subtype, "resource-lists+xml");
            }
        }
        result.PartialState = !result.FullState;
        return result;
    }

    SipEventPackageResult? CheckAndStoreState(SipEventPackageResult result)
    {
        SipEventPackageRequest request = result.Request == null
            ? throw new System.InvalidOperationException(
                "The event package request snapshot is unavailable.")
            : result.Request;
        if (request.SubscriptionId == null || request.SubscriptionId.Length == 0)
        {
            if (result.PartialState)
                return CreateFailure(request, SipStatusCodes.Conflict,
                    "A partial event body requires established subscription state.", true);
            return null;
        }
        lock (gate)
        {
            int index = FindStateIndex(request.SubscriptionId);
            SipEventPackageState? current = index < 0 ? null : states[index];
            if (current == null && result.PartialState)
                return CreateFailure(request, SipStatusCodes.Conflict,
                    "The first event body for a subscription must contain full state.", true);
            if (current != null && current.EventPackage != result.EventPackage)
                return CreateFailure(request, SipStatusCodes.Conflict,
                    "The subscription event package cannot change.", true);
            if (current != null && current.Entity != null && result.Entity != null
                && current.Entity != result.Entity)
                return CreateFailure(request, SipStatusCodes.Conflict,
                    "The event body entity cannot change within a subscription.", true);
            if (current != null && current.Version >= 0 && result.Version >= 0)
            {
                if (result.PartialState && result.Version != current.Version + 1)
                    return CreateFailure(request, SipStatusCodes.Conflict,
                        "A partial event body must have the next contiguous version.", true);
                if (result.FullState && result.Version <= current.Version)
                    return CreateFailure(request, SipStatusCodes.Conflict,
                        "A full event body must have a newer version.", true);
            }
            SipEventPackageState stored = CreateState(result);
            if (index < 0) states.Add(stored);
            else states[index] = stored;
            return null;
        }
    }

    void AddBuiltIn(string eventPackage, string[] mediaTypes, bool resourceLists)
    {
        SipEventPackageEntry entry = new SipEventPackageEntry();
        entry.EventPackage = eventPackage;
        entry.BuiltIn = true;
        entry.SupportsResourceLists = resourceLists;
        entry.ContentTypes = new SipMediaType[mediaTypes.Length];
        int index = 0;
        while (index < mediaTypes.Length)
        {
            entry.ContentTypes[index] = CreateMediaType(mediaTypes[index]);
            index = index + 1;
        }
        packages.Add(entry);
    }

    static bool BodyMatchesPackage(string eventPackage, SipXmlBody body, bool resourceList)
    {
        if (resourceList) return true;
        string? subtype = body.MediaType == null ? null : body.MediaType.Subtype;
        if (eventPackage == SipEventPackages.Presence)
            return SipAscii.EqualsIgnoreCase(subtype, "pidf+xml")
                || SipAscii.EqualsIgnoreCase(subtype, "pidf-diff+xml");
        if (eventPackage == SipEventPackages.WatcherInformation)
            return SipAscii.EqualsIgnoreCase(subtype, "watcherinfo+xml");
        if (eventPackage == SipEventPackages.Dialog)
            return SipAscii.EqualsIgnoreCase(subtype, "dialog-info+xml");
        if (eventPackage == SipEventPackages.Registration)
            return SipAscii.EqualsIgnoreCase(subtype, "reginfo+xml");
        if (eventPackage == SipEventPackages.MessageSummary)
            return SipAscii.EqualsIgnoreCase(subtype, "simple-message-summary");
        if (eventPackage == SipEventPackages.ResourceList)
            return SipAscii.EqualsIgnoreCase(subtype, "resource-lists+xml");
        return false;
    }

    static bool IsMessageSummary(SipMediaType? mediaType)
    {
        return mediaType != null
            && SipAscii.EqualsIgnoreCase(mediaType.Type, "application")
            && SipAscii.EqualsIgnoreCase(mediaType.Subtype, "simple-message-summary");
    }

    static long ParseUnsigned32(string? value)
    {
        if (value == null || value.Length == 0) return -1;
        long result = 0;
        int index = 0;
        while (index < value.Length)
        {
            int digit = value[index] - '0';
            if (digit < 0 || digit > 9) return -1;
            result = result * 10 + digit;
            if (result > 4294967295L) return -1;
            index = index + 1;
        }
        return result;
    }

    SipEventPackageEntry? FindPackageCopy(string eventPackage)
    {
        lock (gate)
        {
            int index = FindPackageIndex(eventPackage);
            if (index < 0) return null;
            SipEventPackageEntry source = packages[index];
            SipEventPackageEntry copy = new SipEventPackageEntry();
            copy.EventPackage = source.EventPackage;
            copy.Handler = source.Handler;
            copy.ContentTypes = CopyMediaTypes(source.ContentTypes);
            copy.BuiltIn = source.BuiltIn;
            copy.SupportsResourceLists = source.SupportsResourceLists;
            return copy;
        }
    }

    int FindPackageIndex(string eventPackage)
    {
        int index = 0;
        while (index < packages.Count)
        {
            if (packages[index].EventPackage == eventPackage) return index;
            index = index + 1;
        }
        return -1;
    }

    int FindStateIndex(string subscriptionId)
    {
        int index = 0;
        while (index < states.Count)
        {
            if (states[index].SubscriptionId == subscriptionId) return index;
            index = index + 1;
        }
        return -1;
    }

    void RemovePackageStates(string eventPackage)
    {
        int index = states.Count - 1;
        while (index >= 0)
        {
            if (states[index].EventPackage == eventPackage) states.RemoveAt(index);
            index = index - 1;
        }
    }

    static bool ContainsMediaType(SipMediaType[] contentTypes, SipMediaType candidate)
    {
        int index = 0;
        while (index < contentTypes.Length)
        {
            SipMediaType current = contentTypes[index];
            if (SipAscii.EqualsIgnoreCase(current.Type, candidate.Type)
                && SipAscii.EqualsIgnoreCase(current.Subtype, candidate.Subtype)) return true;
            index = index + 1;
        }
        return false;
    }

    static void ValidateRegistration(
        string eventPackage, ISipEventPackage handler, SipMediaType[] contentTypes)
    {
        ValidatePackageToken(eventPackage);
        if (handler == null) throw new System.ArgumentNullException("handler");
        if (contentTypes == null) throw new System.ArgumentNullException("contentTypes");
        if (contentTypes.Length == 0)
            throw new System.ArgumentException(
                "At least one event body media type is required.", "contentTypes");
        int index = 0;
        while (index < contentTypes.Length)
        {
            SipMediaType? mediaType = contentTypes[index];
            if (mediaType == null || !SipAscii.IsToken(mediaType.Type)
                || !SipAscii.IsToken(mediaType.Subtype))
                throw new System.ArgumentException(
                    "Every event body media type requires valid type and subtype tokens.",
                    "contentTypes");
            int prior = 0;
            while (prior < index)
            {
                if (ContainsMediaType(
                    new SipMediaType[] { contentTypes[prior] }, mediaType))
                    throw new System.ArgumentException(
                        "An event body media type cannot be registered twice.",
                        "contentTypes");
                prior = prior + 1;
            }
            index = index + 1;
        }
    }

    static void ValidatePackageToken(string eventPackage)
    {
        if (eventPackage == null || !SipAscii.IsToken(eventPackage))
            throw new System.ArgumentException(
                "A valid case-sensitive event package token is required.", "eventPackage");
    }

    static void ValidateSubscriptionIdentifier(string subscriptionId)
    {
        if (subscriptionId == null || subscriptionId.Length == 0)
            throw new System.ArgumentException(
                "A subscription identifier is required.", "subscriptionId");
    }

    static SipEventPackageResult CreateFailure(
        SipEventPackageRequest request, int statusCode, string message, bool builtIn)
    {
        SipEventPackageResult result = new SipEventPackageResult();
        result.Request = CopyRequest(request);
        result.EventPackage = request.EventPackage;
        result.ContentType = CopyMediaType(request.ContentType);
        result.StatusCode = statusCode;
        result.BuiltIn = builtIn;
        result.ErrorMessage = message;
        result.CorrelationId = request.CorrelationId;
        return result;
    }

    static SipEventPackageState CreateState(SipEventPackageResult result)
    {
        SipEventPackageRequest request = result.Request == null
            ? throw new System.InvalidOperationException(
                "The event package request snapshot is unavailable.")
            : result.Request;
        SipEventPackageState state = new SipEventPackageState();
        state.SubscriptionId = request.SubscriptionId;
        state.EventPackage = result.EventPackage;
        state.ContentType = CopyMediaType(result.ContentType);
        state.XmlBody = CopyXmlBody(result.XmlBody);
        state.FullState = result.FullState;
        state.ResourceList = result.ResourceList;
        state.Version = result.Version;
        state.Entity = result.Entity;
        state.CorrelationId = result.CorrelationId;
        return state;
    }

    static SipEventPackageState CopyState(SipEventPackageState source)
    {
        SipEventPackageState copy = new SipEventPackageState();
        copy.SubscriptionId = source.SubscriptionId;
        copy.EventPackage = source.EventPackage;
        copy.ContentType = CopyMediaType(source.ContentType);
        copy.XmlBody = CopyXmlBody(source.XmlBody);
        copy.FullState = source.FullState;
        copy.ResourceList = source.ResourceList;
        copy.Version = source.Version;
        copy.Entity = source.Entity;
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    static SipEventPackageRequest CopyRequest(SipEventPackageRequest source)
    {
        SipEventPackageRequest copy = new SipEventPackageRequest();
        copy.SubscriptionId = source.SubscriptionId;
        copy.EventPackage = source.EventPackage;
        copy.Body = source.Body == null ? new byte[0] : CopyBytes(source.Body);
        copy.ContentType = CopyMediaType(source.ContentType);
        copy.Options = CopyParseOptions(source.Options);
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    static SipParseOptions CopyParseOptions(SipParseOptions? source)
    {
        SipParseOptions copy = new SipParseOptions();
        if (source == null) return copy;
        copy.Strict = source.Strict;
        copy.PreserveOriginal = source.PreserveOriginal;
        copy.AllowHeaderFolding = source.AllowHeaderFolding;
        copy.AllowCompatibleWhitespace = source.AllowCompatibleWhitespace;
        copy.MaxMessageBytes = source.MaxMessageBytes;
        copy.MaxBodyBytes = source.MaxBodyBytes;
        copy.MaxHeaderBytes = source.MaxHeaderBytes;
        copy.MaxStartLineBytes = source.MaxStartLineBytes;
        copy.MaxHeaderLineBytes = source.MaxHeaderLineBytes;
        copy.MaxHeaderCount = source.MaxHeaderCount;
        copy.MaxUriBytes = source.MaxUriBytes;
        copy.MaxParameterCount = source.MaxParameterCount;
        copy.MaxListValueCount = source.MaxListValueCount;
        copy.MaxMimeDepth = source.MaxMimeDepth;
        copy.MaxXmlDepth = source.MaxXmlDepth;
        return copy;
    }

    static SipXmlBody? CopyXmlBody(SipXmlBody? source)
    {
        if (source == null) return null;
        SipXmlBody copy = new SipXmlBody();
        copy.MediaType = CopyMediaType(source.MediaType);
        copy.RootName = source.RootName;
        copy.NamespaceUri = source.NamespaceUri;
        copy.XmlText = source.XmlText;
        copy.OriginalBytes = source.OriginalBytes == null
            ? new byte[0] : CopyBytes(source.OriginalBytes);
        return copy;
    }

    static SipMediaType[] CopyMediaTypes(SipMediaType[] source)
    {
        SipMediaType[] result = new SipMediaType[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipMediaType? copy = CopyMediaType(source[index]);
            result[index] = copy == null ? new SipMediaType() : copy;
            index = index + 1;
        }
        return result;
    }

    static SipMediaType? CopyMediaType(SipMediaType? source)
    {
        if (source == null) return null;
        SipMediaType copy = new SipMediaType();
        copy.Type = source.Type;
        copy.Subtype = source.Subtype;
        copy.QualityThousandths = source.QualityThousandths;
        copy.OriginalText = source.OriginalText;
        if (source.Parameters == null)
        {
            copy.Parameters = new SipUriParameter[0];
            return copy;
        }
        copy.Parameters = new SipUriParameter[source.Parameters.Length];
        int index = 0;
        while (index < source.Parameters.Length)
        {
            SipUriParameter parameter = new SipUriParameter();
            SipUriParameter? original = source.Parameters[index];
            if (original != null)
            {
                parameter.Name = original.Name;
                parameter.HasValue = original.HasValue;
                parameter.Value = original.Value;
            }
            copy.Parameters[index] = parameter;
            index = index + 1;
        }
        return copy;
    }

    static SipMediaType CreateMediaType(string value)
    {
        int slash = value.IndexOf('/');
        SipMediaType mediaType = new SipMediaType();
        mediaType.Type = value.Substring(0, slash);
        mediaType.Subtype = value.Substring(slash + 1);
        return mediaType;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }
}
