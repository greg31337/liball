namespace sip;

internal class SipRedirectServerCore
{
    internal SipRedirectResult Create(SipMessage? request,
        SipRouteDecision? decision)
    {
        if (!ValidRequest(request))
        {
            return CreateFailure(request, SipStatusCodes.BadRequest,
                SipReasonPhrases.BadRequest);
        }
        if (request == null)
        {
            return CreateFailure(null, SipStatusCodes.BadRequest,
                SipReasonPhrases.BadRequest);
        }
        if (decision == null)
        {
            return CreateFailure(request,
                SipStatusCodes.ServerInternalError,
                SipReasonPhrases.ServerInternalError);
        }
        if (decision.RejectStatusCode != 0)
        {
            int rejection = decision.RejectStatusCode >= 400
                && decision.RejectStatusCode <= 699
                ? decision.RejectStatusCode
                : SipStatusCodes.ServerInternalError;
            return CreateFailure(request, rejection,
                SelectReason(decision.Reason, rejection));
        }
        if (!decision.Redirect && decision.Mode != SipProxyMode.Redirect)
        {
            return CreateFailure(request,
                SipStatusCodes.ServerInternalError,
                "The route policy did not select redirect processing.");
        }
        if (decision.RedirectStatusCode < 300
            || decision.RedirectStatusCode > 399)
        {
            return CreateFailure(request,
                SipStatusCodes.ServerInternalError,
                "The route policy selected an invalid redirect status.");
        }
        if (decision.Targets == null || decision.Targets.Length == 0)
        {
            return CreateFailure(request, SipStatusCodes.NotFound,
                SipReasonPhrases.NotFound);
        }

        SipContactValue[] contacts =
            new SipContactValue[decision.Targets.Length];
        int index = 0;
        while (index < decision.Targets.Length)
        {
            SipProxyTarget? target = decision.Targets[index];
            if (!ValidTarget(target))
            {
                return CreateFailure(request,
                    SipStatusCodes.ServerInternalError,
                    "The route policy returned an invalid redirect target.");
            }
            if (request.RequestUri != null
                && request.RequestUri.Scheme == SipUriScheme.Sips
                && target != null && target.Uri != null
                && target.Uri.Scheme != SipUriScheme.Sips)
            {
                return CreateFailure(request,
                    SipStatusCodes.UnsupportedUriScheme,
                    SipReasonPhrases.UnsupportedUriScheme);
            }
            contacts[index] = CreateContact(target);
            index = index + 1;
        }

        int statusCode = decision.RedirectStatusCode;
        string reason = SelectReason(decision.Reason, statusCode);
        SipMessage? responseValue =
            SipUasResponseCore.CreateResponseMessage(
                request, statusCode, reason);
        if (responseValue == null)
        {
            return CreateFailure(request,
                SipStatusCodes.ServerInternalError,
                "The redirect response could not be generated.");
        }
        SipMessage response = responseValue;
        response.ContactValues =
            SipSnapshotCopy.CopyContactArray(contacts);
        if (!AppendContactHeader(response, contacts))
        {
            return CreateFailure(request,
                SipStatusCodes.ServerInternalError,
                "The redirect contacts could not be formatted.");
        }
        SipRedirectResult result = new SipRedirectResult();
        result.Success = true;
        result.Response = SipSnapshotCopy.CopyMessageModel(response);
        result.Contacts = SipSnapshotCopy.CopyContactArray(contacts);
        return result;
    }

    static bool ValidRequest(SipMessage? request)
    {
        if (request == null || request.Kind != SipMessageKind.Request)
        {
            return false;
        }
        SipValidationIssue[] issues =
            SipMessageValidator.ValidateMessageModel(request, true);
        return !SipMessageValidator.HasErrorIssues(issues);
    }

    static bool ValidTarget(SipProxyTarget? target)
    {
        return target != null && target.Uri != null
            && (target.Uri.Scheme == SipUriScheme.Sip
                || target.Uri.Scheme == SipUriScheme.Sips)
            && SipUriValidator.ValidateUriModel(target.Uri).Length == 0
            && target.QualityThousandths >= 0
            && target.QualityThousandths <= 1000
            && target.ExpiresSeconds >= -1;
    }

    static SipContactValue CreateContact(SipProxyTarget? target)
    {
        SipContactValue contact = new SipContactValue();
        SipNameAddress address = new SipNameAddress();
        if (target != null && target.Uri != null)
        {
            address.Uri =
                SipUriModelCopy.CreateNormalizedCopy(target.Uri);
            contact.QualityThousandths = target.QualityThousandths;
            contact.ExpiresSeconds = target.ExpiresSeconds;
        }
        contact.Address = address;
        return contact;
    }

    static bool AppendContactHeader(SipMessage response,
        SipContactValue[] contacts)
    {
        SipStructuredHeaderResult model = new SipStructuredHeaderResult();
        model.Success = true;
        model.ContactValues = contacts;
        SipWriteResult formatted = SipStructuredHeaderCodec
            .FormatStructuredHeaderValue(SipHeaderNames.Contact, model,
                new SipWriteOptions());
        if (!formatted.Success)
        {
            return false;
        }
        SipHeaderParseResult parsed = SipHeaderParser.ParseHeaderBytes(
            formatted.Bytes, new SipParseOptions());
        if (parsed.Status != SipParseStatus.Complete
            || parsed.Header == null)
        {
            return false;
        }
        SipHeader[] headers = new SipHeader[response.Headers.Length + 1];
        int index = 0;
        while (index < response.Headers.Length)
        {
            headers[index] = response.Headers[index];
            index = index + 1;
        }
        headers[index] = parsed.Header;
        response.Headers = headers;
        response.OriginalBytes = new byte[0];
        return true;
    }

    SipRedirectResult CreateFailure(SipMessage? request,
        int statusCode, string reason)
    {
        string safeReason = SanitizeReason(reason,
            SipReasonPhrases.ServerInternalError);
        SipRedirectResult result = new SipRedirectResult();
        result.ErrorMessage = safeReason;
        if (CanCreateResponse(request) && request != null)
        {
            SipMessage? response =
                SipUasResponseCore.CreateResponseMessage(
                    request, statusCode, safeReason);
            result.Response = SipSnapshotCopy.CopyMessageModel(response);
        }
        return result;
    }

    static bool CanCreateResponse(SipMessage? request)
    {
        return request != null && request.Kind == SipMessageKind.Request
            && request.ViaValues != null && request.ViaValues.Length > 0
            && request.From != null && request.To != null
            && request.CSeq != null;
    }

    static string SelectReason(string? configured, int statusCode)
    {
        if (configured != null && configured.Length > 0)
        {
            return SanitizeReason(configured, "Redirect");
        }
        switch (statusCode)
        {
            case SipStatusCodes.MultipleChoices:
                return SipReasonPhrases.MultipleChoices;
            case SipStatusCodes.MovedPermanently:
                return SipReasonPhrases.MovedPermanently;
            case SipStatusCodes.MovedTemporarily:
                return SipReasonPhrases.MovedTemporarily;
            case SipStatusCodes.UseProxy:
                return SipReasonPhrases.UseProxy;
            case SipStatusCodes.AlternativeService:
                return SipReasonPhrases.AlternativeService;
            case SipStatusCodes.BadRequest:
                return SipReasonPhrases.BadRequest;
            case SipStatusCodes.Forbidden:
                return SipReasonPhrases.Forbidden;
            case SipStatusCodes.NotFound:
                return SipReasonPhrases.NotFound;
            case SipStatusCodes.UnsupportedUriScheme:
                return SipReasonPhrases.UnsupportedUriScheme;
            case SipStatusCodes.ServiceUnavailable:
                return SipReasonPhrases.ServiceUnavailable;
            default:
                return statusCode >= 300 && statusCode <= 399
                    ? "Redirect" : SipReasonPhrases.ServerInternalError;
        }
    }

    static string SanitizeReason(string source, string fallback)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < source.Length && builder.Length < 256)
        {
            char value = source[index];
            if (value >= ' ' && value != '\u007f')
            {
                builder.Append(value);
            }
            index = index + 1;
        }
        string result = builder.ToString().Trim();
        return result.Length == 0 ? fallback : result;
    }
}
