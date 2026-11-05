namespace sip;

internal class SipInfoPackageFrameworkCore
{
    object gate = new object();
    System.Collections.Generic.List<SipInfoPackageEntry> packages =
        new System.Collections.Generic.List<SipInfoPackageEntry>();
    System.Collections.Generic.List<SipInfoNegotiationEntry> negotiations =
        new System.Collections.Generic.List<SipInfoNegotiationEntry>();
    ISipDialogLayer? dialogLayer;
    ISipInfoPackage? legacyHandler;

    internal void ConfigureDialogLayer(ISipDialogLayer configuredDialogLayer)
    {
        lock (gate)
        {
            if (configuredDialogLayer == null)
            {
                throw new System.ArgumentNullException("dialogLayer");
            }
            dialogLayer = configuredDialogLayer;
        }
    }

    internal void ConfigureLegacyHandler(ISipInfoPackage configuredHandler)
    {
        lock (gate)
        {
            if (configuredHandler == null)
            {
                throw new System.ArgumentNullException("handler");
            }
            legacyHandler = configuredHandler;
        }
    }

    internal bool RegisterPackage(
        string package, ISipInfoPackage handler, SipMediaType[] contentTypes)
    {
        lock (gate)
        {
            ValidateRegistration(package, handler, contentTypes);
            if (FindPackageIndex(package) >= 0)
            {
                return false;
            }
            SipInfoPackageEntry entry = new SipInfoPackageEntry();
            entry.Package = package;
            entry.Handler = handler;
            entry.ContentTypes = CopyMediaTypes(contentTypes);
            packages.Add(entry);
            return true;
        }
    }

    internal bool RemovePackage(string package)
    {
        lock (gate)
        {
            if (!SipAscii.IsToken(package))
            {
                throw new System.ArgumentException(
                    "A valid case-sensitive INFO package name is required.",
                    "package");
            }
            int index = FindPackageIndex(package);
            if (index < 0)
            {
                return false;
            }
            packages.RemoveAt(index);
            return true;
        }
    }

    internal SipMessage AddAdvertisement(SipMessage message)
    {
        lock (gate)
        {
            if (message == null)
            {
                throw new System.ArgumentNullException("message");
            }
            if (!CanAdvertiseOnMessage(message))
            {
                throw new System.ArgumentException(
                    "Recv-Info is not permitted on this SIP message.",
                    "message");
            }
            SipMessage copy = CopyRequiredMessage(message);
            RemoveNamedHeaders(copy, SipHeaderNames.ReceiveInfo);
            AppendHeader(copy, SipHeaderNames.ReceiveInfo,
                JoinRegisteredPackageNames());
            return copy;
        }
    }

    internal SipInfoNegotiation UpdateNegotiation(
        SipDialogId dialogId, SipMessage message)
    {
        lock (gate)
        {
            ValidateDialogIdentifier(dialogId);
            if (message == null)
            {
                throw new System.ArgumentNullException("message");
            }
            SipInfoPackageParse parsed = ParseReceiveInfoHeaders(message);
            if (!parsed.Success)
            {
                throw new System.ArgumentException(
                    parsed.ErrorMessage == null
                        ? "The Recv-Info value is malformed."
                        : parsed.ErrorMessage, "message");
            }
            int index = FindNegotiationIndex(dialogId);
            if (index < 0)
            {
                SipInfoNegotiationEntry added =
                    new SipInfoNegotiationEntry();
                added.DialogId = CopyDialogId(dialogId);
                negotiations.Add(added);
                index = negotiations.Count - 1;
            }
            SipInfoNegotiationEntry entry = negotiations[index];
            if (parsed.Found)
            {
                entry.RemoteSupportsInfoPackages = true;
                entry.RemotePackages = CopyStrings(parsed.Names);
                entry.CorrelationId = ReadCallId(message);
            }
            return CreateNegotiation(entry);
        }
    }

    internal SipInfoNegotiation GetNegotiation(SipDialogId dialogId)
    {
        lock (gate)
        {
            ValidateDialogIdentifier(dialogId);
            int index = FindNegotiationIndex(dialogId);
            if (index < 0)
            {
                SipInfoNegotiation empty = new SipInfoNegotiation();
                empty.DialogId = CopyDialogId(dialogId);
                empty.LocalPackages = GetRegisteredPackageNames();
                return empty;
            }
            return CreateNegotiation(negotiations[index]);
        }
    }

    internal SipInfoResult Send(SipInfoRequest request)
    {
        lock (gate)
        {
            ValidateSendRequest(request);
            if (dialogLayer == null)
            {
                throw new System.InvalidOperationException(
                    "An INFO dialog layer has not been configured.");
            }
            bool legacy = request.Package == null;
            string? packageName = null;
            if (!legacy)
            {
                SipInfoPackageParse parsed = ParseSinglePackageValue(
                    request.Package);
                if (!parsed.Success || parsed.Name == null)
                {
                    throw new System.ArgumentException(
                        "The Info-Package value is malformed.", "request");
                }
                packageName = parsed.Name;
                if (!RemoteAcceptsPackage(request.DialogId, packageName))
                {
                    return CreateLocalFailure(request,
                        SipStatusCodes.BadInfoPackage,
                        "The remote endpoint has not advertised this INFO package.");
                }
            }
            else if (!request.Options.AllowLegacyInfo)
            {
                return CreateLocalFailure(request,
                    SipStatusCodes.BadInfoPackage,
                    "Legacy INFO usage is disabled by policy.");
            }

            SipDialogId operationDialog = request.DialogId == null
                ? throw new System.InvalidOperationException(
                    "The INFO dialog identifier is unavailable.")
                : request.DialogId;
            SipMessage message = dialogLayer.CreateRequest(
                operationDialog, SipMethods.Info);
            message.Body = (byte[])request.Body.Clone();
            message.ContentType = CopyMediaType(request.ContentType);
            if (!legacy && message.Body.Length > 0)
            {
                SipContentDisposition disposition =
                    new SipContentDisposition();
                disposition.Disposition = SipBodyDisposition.Unknown;
                disposition.DispositionToken = "info-package";
                message.ContentDisposition = disposition;
            }
            ReplaceBodyHeaders(message);
            if (!legacy)
            {
                AppendHeader(message, SipHeaderNames.InfoPackage,
                    request.Package == null ? "" : request.Package);
            }
            SipInfoResult result = new SipInfoResult();
            result.Request = CopyInfoRequest(request);
            result.Message = CopyRequiredMessage(message);
            result.DialogId = CopyDialogId(operationDialog);
            result.Package = packageName;
            result.Legacy = legacy;
            result.Accepted = true;
            result.CorrelationId = request.CorrelationId;
            return result;
        }
    }

    internal SipInfoResult Receive(
        SipDialogId dialogId, SipMessage message, SipInfoOptions options)
    {
        lock (gate)
        {
            ValidateDialogIdentifier(dialogId);
            ValidateReceiveOptions(options);
            if (message == null)
            {
                throw new System.ArgumentNullException("message");
            }
            bool canRespond = SipUasResponseCore.CanCreateResponse(message);
            if (!IsInfoRequest(message))
            {
                return CreateProtocolFailure(dialogId, message,
                    canRespond ? SipStatusCodes.MethodNotAllowed : 0,
                    "An INFO request is required.", false, null, false);
            }
            SipValidationIssue[] issues =
                SipMessageValidator.ValidateMessageModel(message, true);
            if (SipMessageValidator.HasErrorIssues(issues))
            {
                string issueMessage = "The INFO request is malformed.";
                if (issues.Length > 0)
                {
                    string? reportedMessage = issues[0].Message;
                    if (reportedMessage != null)
                    {
                        issueMessage = reportedMessage;
                    }
                }
                return CreateProtocolFailure(dialogId, message,
                    canRespond ? SipStatusCodes.BadRequest : 0,
                    issueMessage, false, null, false);
            }
            if (!MessageMatchesDialog(message, dialogId))
            {
                return CreateProtocolFailure(dialogId, message,
                    SipStatusCodes.CallOrTransactionDoesNotExist,
                    "The INFO request does not belong to the dialog.",
                    false, null, false);
            }
            if (HasHeader(message, SipHeaderNames.ReceiveInfo))
            {
                return CreateProtocolFailure(dialogId, message,
                    SipStatusCodes.BadRequest,
                    "An INFO request cannot contain Recv-Info.",
                    false, null, false);
            }
            if (message.Body.Length > options.MaxBodyBytes)
            {
                return CreateProtocolFailure(dialogId, message,
                    SipStatusCodes.RequestEntityTooLarge,
                    "The INFO body exceeds the configured size policy.",
                    false, null, false);
            }
            if (message.Body.Length > 0
                && !IsValidMediaType(message.ContentType))
            {
                return CreateProtocolFailure(dialogId, message,
                    SipStatusCodes.UnsupportedMediaType,
                    "A valid Content-Type is required for the INFO body.",
                    false, null, false);
            }
            SipInfoPackageParse package = ParseInfoPackageHeader(message);
            if (!package.Success)
            {
                return CreateProtocolFailure(dialogId, message,
                    SipStatusCodes.BadRequest,
                    package.ErrorMessage == null
                        ? "The Info-Package value is malformed."
                        : package.ErrorMessage, false, null, false);
            }
            if (!package.Found)
            {
                return ReceiveLegacyInfo(
                    dialogId, message, options);
            }
            if (package.Name == null)
            {
                return CreateProtocolFailure(dialogId, message,
                    SipStatusCodes.BadRequest,
                    "The Info-Package name is missing.", false, null, false);
            }
            int packageIndex = FindPackageIndex(package.Name);
            if (packageIndex < 0)
            {
                return CreateProtocolFailure(dialogId, message,
                    SipStatusCodes.BadInfoPackage,
                    "The INFO package was not advertised.", false,
                    package.Name, true);
            }
            if (message.Body.Length > 0
                && !HasInfoPackageDisposition(message.ContentDisposition))
            {
                return CreateProtocolFailure(dialogId, message,
                    SipStatusCodes.BadRequest,
                    "An INFO package body requires Content-Disposition info-package.",
                    false, package.Name, false);
            }
            SipInfoPackageEntry entry = packages[packageIndex];
            if (message.ContentType != null
                && !AcceptsMediaType(
                    message.ContentType, entry.ContentTypes))
            {
                SipInfoResult unsupported = CreateProtocolFailure(
                    dialogId, message, SipStatusCodes.UnsupportedMediaType,
                    "The media type is not supported by this INFO package.",
                    false, package.Name, false);
                AddAcceptHeader(unsupported.Response, entry.ContentTypes);
                return unsupported;
            }
            SipInfoRequest dispatched = CreateDispatchRequest(
                dialogId, message, package.Name, options);
            return DispatchToHandler(entry.Handler, dispatched, message,
                false, package.Name);
        }
    }

    SipInfoResult ReceiveLegacyInfo(
        SipDialogId dialogId, SipMessage message, SipInfoOptions options)
    {
        if (!options.AllowLegacyInfo)
        {
            return CreateProtocolFailure(dialogId, message,
                SipStatusCodes.BadInfoPackage,
                "Legacy INFO usage is disabled by policy.", true,
                null, true);
        }
        if (message.ContentType != null
            && !AcceptsMediaType(
                message.ContentType, options.LegacyContentTypes))
        {
            SipInfoResult unsupported = CreateProtocolFailure(
                dialogId, message, SipStatusCodes.UnsupportedMediaType,
                "The legacy INFO media type is not accepted.", true,
                null, false);
            AddAcceptHeader(unsupported.Response,
                options.LegacyContentTypes);
            return unsupported;
        }
        SipInfoRequest dispatched = CreateDispatchRequest(
            dialogId, message, null, options);
        if (legacyHandler == null)
        {
            SipInfoResult accepted = CreateProtocolSuccess(
                dispatched, message, true, null);
            return accepted;
        }
        return DispatchToHandler(legacyHandler, dispatched,
            message, true, null);
    }

    SipInfoResult DispatchToHandler(
        ISipInfoPackage? handler, SipInfoRequest request,
        SipMessage message, bool legacy, string? package)
    {
        if (handler == null)
        {
            return CreateProtocolFailure(request.DialogId, message,
                SipStatusCodes.ServerInternalError,
                "The INFO package has no handler.", legacy, package, false);
        }
        SipInfoResult handled = handler.ProcessInfo(CopyInfoRequest(request));
        if (handled == null)
        {
            return CreateProtocolFailure(request.DialogId, message,
                SipStatusCodes.ServerInternalError,
                "The INFO package handler returned no result.",
                legacy, package, false);
        }
        int status = handled.Response == null
            ? handled.StatusCode : handled.Response.StatusCode;
        if (handled.Accepted && (status < 200 || status >= 300))
        {
            status = SipStatusCodes.Ok;
        }
        if (!handled.Accepted && (status < 300 || status > 699))
        {
            status = SipStatusCodes.ServerInternalError;
        }
        SipMessage? response = null;
        if (handled.Response != null)
        {
            if (HasInfoPackageDisposition(
                    handled.Response.ContentDisposition))
            {
                return CreateProtocolFailure(request.DialogId, message,
                    SipStatusCodes.ServerInternalError,
                    "An INFO response cannot contain an Info Package body.",
                    legacy, package, false);
            }
            response = SipUasResponseCore.CompleteApplicationResponse(
                message, handled.Response);
        }
        if (response == null)
        {
            response = SipUasResponseCore.CreateResponseMessage(
                message, status, ReasonPhrase(status));
        }
        SipInfoResult result = new SipInfoResult();
        result.Request = CopyInfoRequest(request);
        result.Message = CopyRequiredMessage(message);
        result.DialogId = request.DialogId == null
            ? null : CopyDialogId(request.DialogId);
        result.Package = package;
        result.Legacy = legacy;
        result.Accepted = handled.Accepted;
        result.StatusCode = status;
        result.Response = SipSnapshotCopy.CopyMessageModel(response);
        result.ErrorMessage = handled.ErrorMessage;
        result.CorrelationId = request.CorrelationId;
        return result;
    }

    static SipInfoResult CreateProtocolSuccess(
        SipInfoRequest request, SipMessage message, bool legacy,
        string? package)
    {
        SipInfoResult result = new SipInfoResult();
        result.Request = CopyInfoRequest(request);
        result.Message = CopyRequiredMessage(message);
        result.DialogId = request.DialogId == null
            ? null : CopyDialogId(request.DialogId);
        result.Package = package;
        result.Legacy = legacy;
        result.Accepted = true;
        result.StatusCode = SipStatusCodes.Ok;
        result.Response = SipUasResponseCore.CreateResponseMessage(
            message, SipStatusCodes.Ok, SipReasonPhrases.Ok);
        result.CorrelationId = request.CorrelationId;
        return result;
    }

    SipInfoResult CreateProtocolFailure(
        SipDialogId? dialogId, SipMessage message, int statusCode,
        string error, bool legacy, string? package, bool addReceiveInfo)
    {
        SipInfoResult result = new SipInfoResult();
        result.Message = SipSnapshotCopy.CopyMessageModel(message);
        result.DialogId = dialogId == null ? null : CopyDialogId(dialogId);
        result.Package = package;
        result.Legacy = legacy;
        result.StatusCode = statusCode;
        result.ErrorMessage = error;
        result.CorrelationId = ReadCallId(message);
        if (statusCode > 0)
        {
            result.Response = SipUasResponseCore.CreateResponseMessage(
                message, statusCode, ReasonPhrase(statusCode));
            if (result.Response != null
                && statusCode == SipStatusCodes.MethodNotAllowed)
            {
                AppendHeader(result.Response, SipHeaderNames.Allow,
                    SipMethods.Info);
            }
            if (result.Response != null && addReceiveInfo)
            {
                AppendHeader(result.Response, SipHeaderNames.ReceiveInfo,
                    JoinRegisteredPackageNames());
            }
        }
        return result;
    }

    static SipInfoResult CreateLocalFailure(
        SipInfoRequest request, int statusCode, string error)
    {
        SipInfoResult result = new SipInfoResult();
        result.Request = CopyInfoRequest(request);
        result.DialogId = request.DialogId == null
            ? null : CopyDialogId(request.DialogId);
        result.Package = request.Package;
        result.Legacy = request.Package == null;
        result.StatusCode = statusCode;
        result.ErrorMessage = error;
        result.CorrelationId = request.CorrelationId;
        return result;
    }

    static SipInfoRequest CreateDispatchRequest(
        SipDialogId dialogId, SipMessage message, string? package,
        SipInfoOptions options)
    {
        SipInfoRequest request = new SipInfoRequest();
        request.DialogId = CopyDialogId(dialogId);
        request.Package = package;
        request.Body = message.Body == null
            ? new byte[0] : (byte[])message.Body.Clone();
        request.ContentType = CopyMediaType(message.ContentType);
        request.Options = CopyOptions(options);
        request.CorrelationId = ReadCallId(message);
        return request;
    }

    static void ValidateRegistration(
        string? package, ISipInfoPackage? handler,
        SipMediaType[]? contentTypes)
    {
        if (!SipAscii.IsToken(package) || handler == null
            || contentTypes == null || contentTypes.Length == 0)
        {
            throw new System.ArgumentException(
                "An INFO package name, handler, and media type are required.",
                "registration");
        }
        int index = 0;
        while (index < contentTypes.Length)
        {
            if (!IsValidMediaType(contentTypes[index]))
            {
                throw new System.ArgumentException(
                    "Every INFO package media type must be valid.",
                    "registration");
            }
            index = index + 1;
        }
    }

    static void ValidateSendRequest(SipInfoRequest? request)
    {
        if (request == null || request.DialogId == null
            || request.Body == null || request.Options == null)
        {
            throw new System.ArgumentNullException("request");
        }
        ValidateDialogIdentifier(request.DialogId);
        ValidateReceiveOptions(request.Options);
        if (request.Body.Length > request.Options.MaxBodyBytes)
        {
            throw new System.ArgumentException(
                "The INFO body exceeds the configured size policy.",
                "request");
        }
        if (request.Body.Length > 0
            && !IsValidMediaType(request.ContentType)
            || request.ContentType != null
                && !IsValidMediaType(request.ContentType))
        {
            throw new System.ArgumentException(
                "A valid Content-Type is required for the INFO body.",
                "request");
        }
    }

    static void ValidateReceiveOptions(SipInfoOptions? options)
    {
        if (options == null || options.MaxBodyBytes < 0
            || options.LegacyContentTypes == null)
        {
            throw new System.ArgumentException(
                "Valid INFO body and legacy policy is required.",
                "options");
        }
        int index = 0;
        while (index < options.LegacyContentTypes.Length)
        {
            if (!IsValidMediaType(options.LegacyContentTypes[index]))
            {
                throw new System.ArgumentException(
                    "Every legacy INFO media type must be valid.",
                    "options");
            }
            index = index + 1;
        }
    }

    static void ValidateDialogIdentifier(SipDialogId? dialogId)
    {
        if (dialogId == null || dialogId.CallId == null
            || dialogId.CallId.Length == 0
            || dialogId.LocalTag == null || dialogId.LocalTag.Length == 0
            || dialogId.RemoteTag == null || dialogId.RemoteTag.Length == 0)
        {
            throw new System.ArgumentException(
                "A complete INFO dialog identifier is required.",
                "dialogId");
        }
    }

    bool RemoteAcceptsPackage(SipDialogId? dialogId, string package)
    {
        int index = FindNegotiationIndex(dialogId);
        if (index < 0
            || !negotiations[index].RemoteSupportsInfoPackages)
        {
            return false;
        }
        int packageIndex = 0;
        while (packageIndex < negotiations[index].RemotePackages.Length)
        {
            if (negotiations[index].RemotePackages[packageIndex] == package)
            {
                return true;
            }
            packageIndex = packageIndex + 1;
        }
        return false;
    }

    int FindPackageIndex(string? package)
    {
        int index = 0;
        while (index < packages.Count)
        {
            if (packages[index].Package == package)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    int FindNegotiationIndex(SipDialogId? dialogId)
    {
        int index = 0;
        while (dialogId != null && index < negotiations.Count)
        {
            if (DialogIdentifiersEqual(
                    negotiations[index].DialogId, dialogId))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    SipInfoNegotiation CreateNegotiation(
        SipInfoNegotiationEntry entry)
    {
        SipInfoNegotiation result = new SipInfoNegotiation();
        result.DialogId = CopyDialogId(entry.DialogId);
        result.LocalPackages = GetRegisteredPackageNames();
        result.RemotePackages = CopyStrings(entry.RemotePackages);
        result.RemoteSupportsInfoPackages =
            entry.RemoteSupportsInfoPackages;
        result.CorrelationId = entry.CorrelationId;
        return result;
    }

    string[] GetRegisteredPackageNames()
    {
        string[] result = new string[packages.Count];
        int index = 0;
        while (index < packages.Count)
        {
            result[index] = packages[index].Package;
            index = index + 1;
        }
        return result;
    }

    string JoinRegisteredPackageNames()
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;
        while (index < packages.Count)
        {
            if (index > 0)
            {
                text.Append(", ");
            }
            text.Append(packages[index].Package);
            index = index + 1;
        }
        return text.ToString();
    }

    static SipInfoPackageParse ParseReceiveInfoHeaders(SipMessage message)
    {
        SipInfoPackageParse result = new SipInfoPackageParse();
        System.Collections.Generic.List<string> names =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.ReceiveInfo))
            {
                result.Found = true;
                string value = header.Value == null
                    ? "" : header.Value.Trim();
                if (value.Length > 0)
                {
                    SipHeaderPrimitiveResult split =
                        SipHeaderPrimitives.SplitCommaListText(
                            value, SipDefaults.MaxParameterCount);
                    if (!split.Success)
                    {
                        result.ErrorMessage = split.ErrorMessage;
                        return result;
                    }
                    int valueIndex = 0;
                    while (valueIndex < split.TextValues.Length)
                    {
                        SipInfoPackageParse parsed =
                            ParseSinglePackageValue(
                                split.TextValues[valueIndex]);
                        if (!parsed.Success || parsed.Name == null)
                        {
                            result.ErrorMessage =
                                "A Recv-Info package value is malformed.";
                            return result;
                        }
                        if (!ContainsExactName(names, parsed.Name))
                        {
                            names.Add(parsed.Name);
                        }
                        valueIndex = valueIndex + 1;
                    }
                }
            }
            index = index + 1;
        }
        result.Success = true;
        result.Names = names.ToArray();
        return result;
    }

    static SipInfoPackageParse ParseInfoPackageHeader(SipMessage message)
    {
        SipInfoPackageParse result = new SipInfoPackageParse();
        int count = 0;
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.InfoPackage))
            {
                count = count + 1;
                SipInfoPackageParse parsed =
                    ParseSinglePackageValue(header.Value);
                if (!parsed.Success)
                {
                    parsed.Found = true;
                    return parsed;
                }
                result = parsed;
                result.Found = true;
            }
            index = index + 1;
        }
        if (count > 1)
        {
            result.Success = false;
            result.Found = true;
            result.ErrorMessage =
                "An INFO request can contain only one Info-Package header.";
            return result;
        }
        result.Success = true;
        return result;
    }

    static SipInfoPackageParse ParseSinglePackageValue(string? value)
    {
        SipInfoPackageParse result = new SipInfoPackageParse();
        if (value == null)
        {
            result.ErrorMessage = "An Info-Package value is required.";
            return result;
        }
        string trimmed = value.Trim();
        int semicolon = trimmed.IndexOf(';');
        string name = semicolon < 0
            ? trimmed : trimmed.Substring(0, semicolon).Trim();
        if (!SipAscii.IsToken(name))
        {
            result.ErrorMessage =
                "The Info-Package name must be a token.";
            return result;
        }
        if (semicolon >= 0)
        {
            SipHeaderPrimitiveResult parameters =
                SipHeaderPrimitives.ParseParameterSequenceText(
                    trimmed, semicolon, SipDefaults.MaxParameterCount);
            if (!parameters.Success)
            {
                result.ErrorMessage = parameters.ErrorMessage;
                return result;
            }
            if (HasDuplicateParameters(parameters.Parameters))
            {
                result.ErrorMessage =
                    "An Info-Package parameter cannot be repeated.";
                return result;
            }
        }
        result.Success = true;
        result.Name = name;
        result.Value = trimmed;
        return result;
    }

    static bool HasDuplicateParameters(SipUriParameter[] parameters)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            int other = index + 1;
            while (other < parameters.Length)
            {
                if (SipAscii.EqualsIgnoreCase(
                        parameters[index].Name, parameters[other].Name))
                {
                    return true;
                }
                other = other + 1;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsExactName(
        System.Collections.Generic.List<string> values, string value)
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

    static bool CanAdvertiseOnMessage(SipMessage message)
    {
        if (message.Kind == SipMessageKind.Request)
        {
            return message.Method == SipMethod.Invite
                || message.Method == SipMethod.Options
                || message.Method == SipMethod.Prack
                || message.Method == SipMethod.Update
                || message.Method == SipMethod.Register;
        }
        if (message.Kind != SipMessageKind.Response
            || message.StatusCode < 100 || message.StatusCode >= 300
            || message.CSeq == null)
        {
            return false;
        }
        return message.CSeq.Method == SipMethod.Invite
            || message.CSeq.Method == SipMethod.Options
            || message.CSeq.Method == SipMethod.Prack
            || message.CSeq.Method == SipMethod.Update;
    }

    static bool IsInfoRequest(SipMessage message)
    {
        return message.Kind == SipMessageKind.Request
            && message.Method == SipMethod.Info
            && SipAscii.EqualsIgnoreCase(
                message.MethodToken, SipMethods.Info);
    }

    static bool MessageMatchesDialog(
        SipMessage message, SipDialogId dialogId)
    {
        return ReadCallId(message) == dialogId.CallId
            && ReadTag(message.To) == dialogId.LocalTag
            && ReadTag(message.From) == dialogId.RemoteTag;
    }

    static bool DialogIdentifiersEqual(
        SipDialogId left, SipDialogId right)
    {
        return left.CallId == right.CallId
            && left.LocalTag == right.LocalTag
            && left.RemoteTag == right.RemoteTag;
    }

    static string? ReadTag(SipNameAddress? address)
    {
        int index = 0;
        while (address != null && address.Parameters != null
            && index < address.Parameters.Length)
        {
            SipUriParameter? parameter = address.Parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(
                    parameter.Name, "tag"))
            {
                return parameter.Value;
            }
            index = index + 1;
        }
        return null;
    }

    static string? ReadCallId(SipMessage message)
    {
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.CallId))
            {
                return header.Value;
            }
            index = index + 1;
        }
        return null;
    }

    static bool HasHeader(SipMessage message, string name)
    {
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, name))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HasInfoPackageDisposition(
        SipContentDisposition? disposition)
    {
        return disposition != null && SipAscii.EqualsIgnoreCase(
            disposition.DispositionToken, "info-package");
    }

    static bool AcceptsMediaType(
        SipMediaType contentType, SipMediaType[] accepted)
    {
        if (accepted.Length == 0)
        {
            return true;
        }
        int index = 0;
        while (index < accepted.Length)
        {
            SipMediaType candidate = accepted[index];
            bool typeMatches = candidate.Type == "*"
                || SipAscii.EqualsIgnoreCase(
                    candidate.Type, contentType.Type);
            bool subtypeMatches = candidate.Subtype == "*"
                || SipAscii.EqualsIgnoreCase(
                    candidate.Subtype, contentType.Subtype);
            if (typeMatches && subtypeMatches)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IsValidMediaType(SipMediaType? contentType)
    {
        return contentType != null
            && (contentType.Type == "*"
                || SipAscii.IsToken(contentType.Type))
            && (contentType.Subtype == "*"
                || SipAscii.IsToken(contentType.Subtype))
            && contentType.Parameters != null;
    }

    static void ReplaceBodyHeaders(SipMessage message)
    {
        RemoveNamedHeaders(message, SipHeaderNames.ContentLength);
        RemoveNamedHeaders(message, SipHeaderNames.ContentType);
        RemoveNamedHeaders(message, SipHeaderNames.ContentDisposition);
        if (message.ContentType != null)
        {
            AppendHeader(message, SipHeaderNames.ContentType,
                FormatMediaType(message.ContentType));
        }
        if (message.ContentDisposition != null)
        {
            AppendHeader(message, SipHeaderNames.ContentDisposition,
                message.ContentDisposition.DispositionToken == null
                    ? "info-package"
                    : message.ContentDisposition.DispositionToken);
        }
        AppendHeader(message, SipHeaderNames.ContentLength,
            message.Body.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        message.OriginalBytes = new byte[0];
    }

    static string FormatMediaType(SipMediaType mediaType)
    {
        SipCoreHeaderResult parsed = new SipCoreHeaderResult();
        parsed.Success = true;
        parsed.MediaType = mediaType;
        SipWriteResult formatted = SipCoreHeaderCodec.FormatCoreHeaderValue(
            SipHeaderNames.ContentType, parsed, new SipWriteOptions());
        if (!formatted.Success)
        {
            throw new System.ArgumentException(
                "The INFO Content-Type is invalid.", "request");
        }
        string line = System.Text.Encoding.UTF8.GetString(formatted.Bytes);
        int colon = line.IndexOf(':');
        return colon < 0 ? line.Trim()
            : line.Substring(colon + 1).Trim();
    }

    static void AddAcceptHeader(
        SipMessage? response, SipMediaType[] mediaTypes)
    {
        if (response == null || mediaTypes.Length == 0)
        {
            return;
        }
        System.Text.StringBuilder value = new System.Text.StringBuilder();
        int index = 0;
        while (index < mediaTypes.Length)
        {
            if (index > 0)
            {
                value.Append(", ");
            }
            value.Append(mediaTypes[index].Type);
            value.Append('/');
            value.Append(mediaTypes[index].Subtype);
            index = index + 1;
        }
        AppendHeader(response, SipHeaderNames.Accept, value.ToString());
    }

    static void RemoveNamedHeaders(SipMessage message, string name)
    {
        System.Collections.Generic.List<SipHeader> kept =
            new System.Collections.Generic.List<SipHeader>();
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && !SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, name))
            {
                kept.Add(header);
            }
            index = index + 1;
        }
        message.Headers = kept.ToArray();
        message.OriginalBytes = new byte[0];
    }

    static void AppendHeader(
        SipMessage message, string name, string value)
    {
        SipUasResponseCore.AppendResponseHeader(message, name, value);
    }

    static SipInfoRequest CopyInfoRequest(SipInfoRequest source)
    {
        SipInfoRequest copy = new SipInfoRequest();
        copy.DialogId = source.DialogId == null
            ? null : CopyDialogId(source.DialogId);
        copy.Package = source.Package;
        copy.Body = source.Body == null
            ? new byte[0] : (byte[])source.Body.Clone();
        copy.ContentType = CopyMediaType(source.ContentType);
        copy.Options = CopyOptions(source.Options);
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    static SipInfoOptions CopyOptions(SipInfoOptions? source)
    {
        SipInfoOptions copy = new SipInfoOptions();
        if (source != null)
        {
            copy.MaxBodyBytes = source.MaxBodyBytes;
            copy.AllowLegacyInfo = source.AllowLegacyInfo;
            copy.LegacyContentTypes =
                CopyMediaTypes(source.LegacyContentTypes);
        }
        return copy;
    }

    static SipMediaType[] CopyMediaTypes(SipMediaType[]? source)
    {
        if (source == null)
        {
            return new SipMediaType[0];
        }
        SipMediaType[] copy = new SipMediaType[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipMediaType? value = CopyMediaType(source[index]);
            copy[index] = value == null ? new SipMediaType() : value;
            index = index + 1;
        }
        return copy;
    }

    static SipMediaType? CopyMediaType(SipMediaType? source)
    {
        if (source == null)
        {
            return null;
        }
        SipOfferAnswer holder = new SipOfferAnswer();
        holder.ContentType = source;
        SipOfferAnswer? copied = SipSnapshotCopy.CopyOfferAnswer(holder);
        return copied == null ? null : copied.ContentType;
    }

    static SipDialogId CopyDialogId(SipDialogId source)
    {
        SipDialogId copy = new SipDialogId();
        copy.CallId = source.CallId;
        copy.LocalTag = source.LocalTag;
        copy.RemoteTag = source.RemoteTag;
        return copy;
    }

    static string[] CopyStrings(string[]? source)
    {
        if (source == null)
        {
            return new string[0];
        }
        string[] copy = new string[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }

    static SipMessage CopyRequiredMessage(SipMessage source)
    {
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(source);
        if (copy == null)
        {
            throw new System.InvalidOperationException(
                "The INFO message could not be copied.");
        }
        return copy;
    }

    static string ReasonPhrase(int statusCode)
    {
        switch (statusCode)
        {
            case SipStatusCodes.Ok:
                return SipReasonPhrases.Ok;
            case SipStatusCodes.BadRequest:
                return SipReasonPhrases.BadRequest;
            case SipStatusCodes.MethodNotAllowed:
                return SipReasonPhrases.MethodNotAllowed;
            case SipStatusCodes.RequestEntityTooLarge:
                return SipReasonPhrases.RequestEntityTooLarge;
            case SipStatusCodes.UnsupportedMediaType:
                return SipReasonPhrases.UnsupportedMediaType;
            case SipStatusCodes.BadInfoPackage:
                return SipReasonPhrases.BadInfoPackage;
            case SipStatusCodes.CallOrTransactionDoesNotExist:
                return SipReasonPhrases.CallOrTransactionDoesNotExist;
            case SipStatusCodes.NotAcceptableHere:
                return SipReasonPhrases.NotAcceptableHere;
            default:
                return SipReasonPhrases.ServerInternalError;
        }
    }
}
