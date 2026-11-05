namespace sip;

internal class SipTransferServiceCore
{
    ISipTransferAuthorizationPolicy? authorizationPolicy;

    internal void ConfigureAuthorizationPolicy(
        ISipTransferAuthorizationPolicy configuredAuthorizationPolicy)
    {
        if (configuredAuthorizationPolicy == null)
        {
            throw new System.ArgumentNullException("authorizationPolicy");
        }
        authorizationPolicy = configuredAuthorizationPolicy;
    }

    internal SipTransferResult Evaluate(SipTransferRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (request.Message == null)
        {
            throw new System.ArgumentException(
                "A SIP message is required.", "request");
        }
        if (request.Dialogs == null)
        {
            throw new System.ArgumentException(
                "The dialog collection is required.", "request");
        }

        SipTransferResult result = new SipTransferResult();
        result.CorrelationId = request.CorrelationId;
        SipMessage message = request.Message;
        if (message.Kind != SipMessageKind.Request)
        {
            return Fail(result, SipStatusCodes.BadRequest,
                "Transfer correlation requires a SIP request.");
        }

        SipTransferHeaderParse parsed = ParseHeaders(message);
        if (parsed.ErrorMessage != null)
        {
            return Fail(result, parsed.StatusCode, parsed.ErrorMessage);
        }
        if (request.RequireReferredBy && parsed.ReferredBy == null)
        {
            return Fail(result, SipStatusCodes.ProvideReferrerIdentity,
                "A Referred-By identity is required by application policy.");
        }

        SipTransferOperation operation = SelectOperation(
            request.Operation, message, parsed);
        if (operation == SipTransferOperation.None)
        {
            return Fail(result, SipStatusCodes.BadRequest,
                "The request does not contain valid transfer semantics.");
        }
        result.Operation = operation;
        result.ReferTo = SipSnapshotCopy.CopyNameAddressModel(parsed.ReferTo);
        result.ReferredBy = SipSnapshotCopy.CopyNameAddressModel(
            parsed.ReferredBy);

        SipExtensionHeaderValue? reference = parsed.Replaces;
        if (reference == null)
        {
            reference = parsed.Join;
        }
        if (reference != null)
        {
            SipDialogSnapshot? matched = MatchDialog(
                reference, request.Dialogs);
            if (matched == null)
            {
                return Fail(result,
                    SipStatusCodes.CallOrTransactionDoesNotExist,
                    "The dialog reference does not identify exactly one dialog.");
            }
            if (matched.State == SipDialogState.Terminated)
            {
                return Fail(result, SipStatusCodes.Decline,
                    "The referenced dialog has terminated.");
            }
            if (parsed.Replaces != null
                && matched.State == SipDialogState.Early
                && matched.Role != SipDialogRole.Uac)
            {
                return Fail(result,
                    SipStatusCodes.CallOrTransactionDoesNotExist,
                    "An early dialog not initiated locally cannot be replaced.");
            }
            if (parsed.Replaces != null && parsed.Replaces.HasFlag
                && matched.State == SipDialogState.Confirmed)
            {
                return Fail(result, SipStatusCodes.BusyHere,
                    "The Replaces early-only condition does not match a confirmed dialog.");
            }
            result.ReferencedDialog = SipSnapshotCopy.CopyDialogSnapshot(
                matched);
            result.EarlyOnly = parsed.Replaces != null
                && parsed.Replaces.HasFlag;
        }

        if (parsed.TargetDialog != null)
        {
            SipDialogSnapshot? target = MatchDialog(
                parsed.TargetDialog, request.Dialogs);
            if (target != null && target.State != SipDialogState.Terminated)
            {
                result.TargetDialog = SipSnapshotCopy.CopyDialogSnapshot(
                    target);
            }
        }

        result.Valid = true;
        ApplyAuthorization(request, message, result);
        return result;
    }

    void ApplyAuthorization(SipTransferRequest request, SipMessage message,
        SipTransferResult result)
    {
        if (authorizationPolicy == null)
        {
            return;
        }
        SipTransferAuthorizationContext context =
            new SipTransferAuthorizationContext();
        context.Message = SipSnapshotCopy.CopyMessageModel(message);
        context.Operation = result.Operation;
        context.ReferTo = SipSnapshotCopy.CopyNameAddressModel(
            result.ReferTo);
        context.ReferredBy = SipSnapshotCopy.CopyNameAddressModel(
            result.ReferredBy);
        context.ReferencedDialog = SipSnapshotCopy.CopyDialogSnapshot(
            result.ReferencedDialog);
        context.TargetDialog = SipSnapshotCopy.CopyDialogSnapshot(
            result.TargetDialog);
        context.AuthenticatedIdentity = request.AuthenticatedIdentity;
        context.CorrelationId = request.CorrelationId;
        SipAuthorizationDecision decision =
            authorizationPolicy.AuthorizeTransfer(context);
        result.AuthorizationEvaluated = true;
        if (decision == null)
        {
            result.StatusCode = SipStatusCodes.ServerInternalError;
            result.ErrorMessage =
                "The transfer authorization policy returned no decision.";
            return;
        }
        result.Authorization = CopyAuthorizationDecision(decision);
        result.Authorized = decision.Allowed;
        if (!decision.Allowed)
        {
            result.StatusCode = decision.StatusCode >= 300
                && decision.StatusCode <= 699
                ? decision.StatusCode : SipStatusCodes.Forbidden;
            result.ErrorMessage = decision.Reason == null
                || decision.Reason.Length == 0
                ? "Application policy denied the call-control operation."
                : decision.Reason;
        }
    }

    static SipTransferHeaderParse ParseHeaders(SipMessage message)
    {
        SipTransferHeaderParse result = new SipTransferHeaderParse();
        int referredByCount = 0;
        int replacesCount = 0;
        int joinCount = 0;
        int targetCount = 0;
        int referToCount = 0;
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.ReferredBy))
            {
                referredByCount = referredByCount + 1;
                SipExtensionHeaderValue? value = ParseHeader(
                    SipHeaderNames.ReferredBy, header.Value);
                if (value == null || value.Address == null)
                {
                    return HeaderError("The Referred-By value is malformed.");
                }
                result.ReferredBy = value.Address;
            }
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.Replaces))
            {
                replacesCount = replacesCount + 1;
                result.Replaces = ParseHeader(
                    SipHeaderNames.Replaces, header.Value);
                if (result.Replaces == null)
                {
                    return HeaderError("The Replaces value is malformed.");
                }
            }
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.Join))
            {
                joinCount = joinCount + 1;
                result.Join = ParseHeader(SipHeaderNames.Join, header.Value);
                if (result.Join == null)
                {
                    return HeaderError("The Join value is malformed.");
                }
            }
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.TargetDialog))
            {
                targetCount = targetCount + 1;
                result.TargetDialog = ParseHeader(
                    SipHeaderNames.TargetDialog, header.Value);
                if (result.TargetDialog == null)
                {
                    return HeaderError("The Target-Dialog value is malformed.");
                }
            }
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.ReferTo))
            {
                referToCount = referToCount + 1;
            }
            index = index + 1;
        }
        if (referredByCount > 1 || replacesCount > 1
            || joinCount > 1 || targetCount > 1)
        {
            return HeaderError(
                "Transfer-related header fields cannot be repeated.");
        }
        bool refer = IsMethod(message, SipMethod.Refer, SipMethods.Refer);
        bool invite = IsMethod(message, SipMethod.Invite, SipMethods.Invite);
        bool subscribe = IsMethod(
            message, SipMethod.Subscribe, SipMethods.Subscribe);
        if (refer)
        {
            if (replacesCount > 0)
            {
                return HeaderError(
                    "A Replaces header field is valid only on an INVITE request.");
            }
            SipReferHeaderParse referHeaders =
                SipReferCore.ParseReferHeaders(message);
            if (!referHeaders.Success || referHeaders.ReferTo == null)
            {
                return HeaderError(referHeaders.ErrorMessage == null
                    ? "The Refer-To value is malformed."
                    : referHeaders.ErrorMessage);
            }
            result.ReferTo = referHeaders.ReferTo;
            SipExtensionHeaderValue? embedded = ParseEmbeddedReplaces(
                referHeaders.ReferTo.Uri);
            if (EmbeddedReplacesCount(referHeaders.ReferTo.Uri) > 1)
            {
                return HeaderError(
                    "A Refer-To URI cannot contain multiple Replaces values.");
            }
            if (EmbeddedReplacesCount(referHeaders.ReferTo.Uri) == 1
                && embedded == null)
            {
                return HeaderError(
                    "The Refer-To Replaces value is malformed.");
            }
            if (embedded != null)
            {
                result.Replaces = embedded;
            }
        }
        else if (referToCount > 0)
        {
            return HeaderError("Refer-To is valid only on a REFER request.");
        }
        if (result.Replaces != null && !invite && !refer)
        {
            return HeaderError(
                "Replaces is valid only on INVITE or inside REFER Refer-To.");
        }
        if (result.Join != null && !invite)
        {
            return HeaderError("Join is valid only on an INVITE request.");
        }
        if (result.Replaces != null && result.Join != null)
        {
            return HeaderError(
                "Replaces and Join have contradictory call-control semantics.");
        }
        if (result.TargetDialog != null
            && !invite && !refer && !subscribe)
        {
            return HeaderError(
                "Target-Dialog is valid only on a dialog-forming request.");
        }
        return result;
    }

    static SipTransferOperation SelectOperation(
        SipTransferOperation requested, SipMessage message,
        SipTransferHeaderParse parsed)
    {
        bool refer = IsMethod(message, SipMethod.Refer, SipMethods.Refer);
        bool invite = IsMethod(message, SipMethod.Invite, SipMethods.Invite);
        if (requested != SipTransferOperation.None)
        {
            if (requested == SipTransferOperation.BlindTransfer
                && refer && parsed.Replaces == null)
                return requested;
            if (requested == SipTransferOperation.AttendedTransfer
                && parsed.Replaces != null && (refer || invite))
                return requested;
            if (requested == SipTransferOperation.CallPickup
                && parsed.Replaces != null && invite)
                return requested;
            if (requested == SipTransferOperation.DialogJoin
                && parsed.Join != null && invite)
                return requested;
            if (requested == SipTransferOperation.TargetDialogAssociation
                && parsed.TargetDialog != null)
                return requested;
            return SipTransferOperation.None;
        }
        if (parsed.Join != null)
        {
            return SipTransferOperation.DialogJoin;
        }
        if (refer && parsed.Replaces != null)
        {
            return SipTransferOperation.AttendedTransfer;
        }
        if (refer)
        {
            return SipTransferOperation.BlindTransfer;
        }
        if (invite && parsed.Replaces != null)
        {
            return parsed.ReferredBy == null
                ? SipTransferOperation.CallPickup
                : SipTransferOperation.AttendedTransfer;
        }
        if (parsed.TargetDialog != null)
        {
            return SipTransferOperation.TargetDialogAssociation;
        }
        return SipTransferOperation.None;
    }

    static SipExtensionHeaderValue? ParseHeader(
        string name, string? value)
    {
        SipExtensionHeaderResult parsed =
            SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                name, value, new SipParseOptions());
        return parsed.Success && parsed.Values.Length == 1
            ? parsed.Values[0] : null;
    }

    static SipExtensionHeaderValue? ParseEmbeddedReplaces(SipUri? uri)
    {
        int index = 0;
        while (uri != null && uri.Headers != null
            && index < uri.Headers.Length)
        {
            SipUriHeader? header = uri.Headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.Replaces))
            {
                return ParseHeader(SipHeaderNames.Replaces, header.Value);
            }
            index = index + 1;
        }
        return null;
    }

    static int EmbeddedReplacesCount(SipUri? uri)
    {
        int count = 0;
        int index = 0;
        while (uri != null && uri.Headers != null
            && index < uri.Headers.Length)
        {
            SipUriHeader? header = uri.Headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.Replaces))
            {
                count = count + 1;
            }
            index = index + 1;
        }
        return count;
    }

    static SipDialogSnapshot? MatchDialog(
        SipExtensionHeaderValue reference, SipDialogSnapshot[] dialogs)
    {
        SipDialogSnapshot? match = null;
        int count = 0;
        int index = 0;
        while (index < dialogs.Length)
        {
            SipDialogSnapshot? candidate = dialogs[index];
            SipDialogId? id = candidate == null ? null : candidate.Id;
            if (id != null && id.CallId == reference.Token
                && TagMatches(id.LocalTag, reference.SecondaryToken)
                && TagMatches(id.RemoteTag, reference.TertiaryToken))
            {
                count = count + 1;
                match = candidate;
            }
            index = index + 1;
        }
        return count == 1 ? match : null;
    }

    static bool TagMatches(string? dialogTag, string? referenceTag)
    {
        return dialogTag == referenceTag
            || referenceTag == "0"
                && (dialogTag == null || dialogTag == "0");
    }

    static bool IsMethod(
        SipMessage message, SipMethod method, string token)
    {
        return (message.Method == method
                || message.Method == SipMethod.Unknown)
            && SipAscii.EqualsIgnoreCase(message.MethodToken, token);
    }

    static SipTransferHeaderParse HeaderError(string message)
    {
        SipTransferHeaderParse result = new SipTransferHeaderParse();
        result.StatusCode = SipStatusCodes.BadRequest;
        result.ErrorMessage = message;
        return result;
    }

    static SipTransferResult Fail(
        SipTransferResult result, int statusCode, string message)
    {
        result.StatusCode = statusCode;
        result.ErrorMessage = message;
        return result;
    }

    static SipAuthorizationDecision CopyAuthorizationDecision(
        SipAuthorizationDecision source)
    {
        SipAuthorizationDecision copy = new SipAuthorizationDecision();
        copy.Allowed = source.Allowed;
        copy.Identity = source.Identity;
        copy.Reason = source.Reason;
        copy.StatusCode = source.StatusCode;
        copy.Scopes = source.Scopes == null
            ? new string[0] : (string[])source.Scopes.Clone();
        return copy;
    }
}
