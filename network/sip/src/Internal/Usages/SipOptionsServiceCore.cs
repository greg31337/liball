namespace sip;

internal static class SipOptionsServiceCore
{
    static readonly SipProtocolTokenGenerator Tokens =
        new SipProtocolTokenGenerator(
            new SipCryptographicRandomSource());

    internal static SipOptionsResult CreateQuery(
        SipUri? target, SipCancellation? cancellation)
    {
        if (cancellation == null)
        {
            return CreateFailure("A cancellation request is required.");
        }
        if (cancellation.Requested)
        {
            return CreateFailure(cancellation.Reason == null
                ? "The OPTIONS query was cancelled."
                : cancellation.Reason);
        }
        if (target == null
            || SipUriValidator.ValidateUriModel(target).Length > 0
            || target.Headers == null || target.Headers.Length > 0)
        {
            return CreateFailure("A valid OPTIONS target is required.");
        }
        SipWriteOptions writeOptions = new SipWriteOptions();
        writeOptions.PreserveOriginal = false;
        SipWriteResult formatted =
            SipUriFormatter.FormatUriModel(target, writeOptions);
        if (!formatted.Success)
        {
            return CreateFailure("The OPTIONS target cannot be formatted.");
        }
        string targetText =
            System.Text.Encoding.ASCII.GetString(formatted.Bytes);
        bool secure = target.Scheme == SipUriScheme.Sips;
        string scheme = secure ? "sips" : "sip";
        string transport = secure
            ? SipTransportNames.Tls : SipTransportNames.Udp;
        System.Text.StringBuilder text =
            new System.Text.StringBuilder();
        text.Append(SipMethods.Options);
        text.Append(" ");
        text.Append(targetText);
        text.Append(" SIP/2.0\r\nVia: SIP/2.0/");
        text.Append(transport);
        text.Append(" options.invalid;branch=");
        text.Append(Tokens.CreateBranch());
        text.Append(";rport\r\nMax-Forwards: 70\r\nFrom: <");
        text.Append(scheme);
        text.Append(":options@options.invalid>;tag=");
        text.Append(Tokens.CreateTag());
        text.Append("\r\nTo: <");
        text.Append(targetText);
        text.Append(">\r\nCall-ID: ");
        text.Append(Tokens.CreateOpaqueToken());
        text.Append("@options.invalid\r\nCSeq: 1 OPTIONS");
        text.Append("\r\nContent-Length: 0\r\n\r\n");
        SipParseResult parsed = SipMessageParser
            .ParseDatagramMessageBytes(
                System.Text.Encoding.ASCII.GetBytes(text.ToString()),
                new SipParseOptions());
        if (parsed.Status != SipParseStatus.Complete
            || parsed.Message == null)
        {
            return CreateFailure(parsed.ErrorMessage == null
                ? "The OPTIONS request could not be constructed."
                : parsed.ErrorMessage);
        }
        SipOptionsResult result = new SipOptionsResult();
        result.Success = true;
        result.Request =
            SipSnapshotCopy.CopyMessageModel(parsed.Message);
        return result;
    }

    internal static SipOptionsResult AnswerRequest(
        SipMessage? request, SipCapabilitySet? capabilities)
    {
        SipCapabilitySet advertised = PrepareCapabilities(capabilities);
        if (request == null || request.Kind != SipMessageKind.Request)
        {
            SipOptionsResult missing = CreateFailure(
                "An OPTIONS request is required.");
            missing.Capabilities = advertised;
            return missing;
        }
        SipValidationIssue[] issues =
            SipMessageValidator.ValidateMessageModel(request, true);
        if (SipMessageValidator.HasErrorIssues(issues))
        {
            return CreateResponseResult(request, advertised,
                SipStatusCodes.BadRequest,
                SipReasonPhrases.BadRequest, false);
        }
        if (request.Method != SipMethod.Options
            || request.MethodToken != SipMethods.Options)
        {
            return CreateResponseResult(request, advertised,
                SipStatusCodes.MethodNotAllowed,
                SipReasonPhrases.MethodNotAllowed, false);
        }
        return CreateResponseResult(request, advertised,
            SipStatusCodes.Ok, SipReasonPhrases.Ok, true);
    }

    internal static SipOptionsResult CreateResponseResult(
        SipMessage request, SipCapabilitySet capabilities,
        int statusCode, string reason, bool success)
    {
        SipCapabilitySet advertised = PrepareCapabilities(capabilities);
        SipMessage? response = SipUasResponseCore.CreateResponseMessage(
            request, statusCode, reason);
        SipOptionsResult result = new SipOptionsResult();
        result.Success = success && response != null;
        result.Capabilities = advertised;
        result.Response =
            SipSnapshotCopy.CopyMessageModel(response);
        if (!result.Success)
        {
            result.ErrorMessage = response == null
                ? "The OPTIONS response could not be generated."
                : reason;
        }
        if (response != null)
        {
            AddCapabilityHeaders(response, advertised);
            result.Response =
                SipSnapshotCopy.CopyMessageModel(response);
        }
        return result;
    }

    internal static SipCapabilitySet PrepareCapabilities(
        SipCapabilitySet? capabilities)
    {
        SipCapabilitySet normalized =
            SipExtensionRegistryCore.NormalizeCapabilities(capabilities);
        if (!ContainsMethod(normalized.Methods, SipMethods.Options))
        {
            string[] methods =
                new string[normalized.Methods.Length + 1];
            int index = 0;
            while (index < normalized.Methods.Length)
            {
                methods[index] = normalized.Methods[index];
                index = index + 1;
            }
            methods[index] = SipMethods.Options;
            normalized.Methods = methods;
        }
        return normalized;
    }

    static void AddCapabilityHeaders(
        SipMessage response, SipCapabilitySet capabilities)
    {
        SipUasResponseCore.AppendResponseHeader(response,
            SipHeaderNames.Allow, JoinValues(capabilities.Methods));
        if (capabilities.OptionTags.Length > 0)
        {
            SipUasResponseCore.AppendResponseHeader(response,
                SipHeaderNames.Supported,
                JoinValues(capabilities.OptionTags));
        }
        if (capabilities.MediaTypes.Length > 0)
        {
            SipUasResponseCore.AppendResponseHeader(response,
                SipHeaderNames.Accept,
                JoinValues(capabilities.MediaTypes));
        }
        if (capabilities.EventPackages.Length > 0)
        {
            SipUasResponseCore.AppendResponseHeader(response,
                SipHeaderNames.AllowEvents,
                JoinValues(capabilities.EventPackages));
        }
    }

    static bool ContainsMethod(string[] methods, string expected)
    {
        int index = 0;
        while (index < methods.Length)
        {
            if (methods[index] == expected)
            {
                return true;
            }
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
            if (index > 0)
            {
                builder.Append(", ");
            }
            builder.Append(values[index]);
            index = index + 1;
        }
        return builder.ToString();
    }

    static SipOptionsResult CreateFailure(string message)
    {
        SipOptionsResult result = new SipOptionsResult();
        result.ErrorMessage = message;
        return result;
    }
}
