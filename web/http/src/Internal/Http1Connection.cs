using System.Collections.Generic;
using System.Text;

namespace http;

internal class Http1ConnectionResult
{
    internal HttpOperationStatus Status;
    internal Http1ConnectionDecision Decision = new Http1ConnectionDecision();
    internal bool IsUpgradeRequested;
    internal List<string> UpgradeProtocols = new List<string>();
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal class Http1ConnectionOptionsResult
{
    internal HttpOperationStatus Status;
    internal bool HasClose;
    internal bool HasKeepAlive;
    internal bool HasUpgrade;
    internal List<string> UpgradeProtocols = new List<string>();
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal static class Http1Connection
{
    internal static Http1ConnectionResult Determine(
        HttpHeaders headers,
        HttpProtocolVersion protocolVersion,
        Http1FramingDecision framing,
        bool isProxyRecipient,
        bool isResponse,
        int statusCode,
        HttpLimits limits)
    {
        Http1ConnectionResult result = new Http1ConnectionResult();
        Http1ConnectionOptionsResult parsed = ParseOptions(headers, limits);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = parsed.ErrorCode;
            result.ErrorMessage = parsed.ErrorMessage;
            return result;
        }

        bool analyzeUpgrade =
            isResponse && statusCode == HttpStatusCodes.SwitchingProtocols ||
            !isResponse && protocolVersion == HttpProtocolVersion.Http11 &&
            parsed.HasUpgrade;
        if (analyzeUpgrade)
        {
            Http1ConnectionOptionsResult upgrades = ParseUpgrades(headers, limits);
            if (upgrades.Status != HttpOperationStatus.Done)
            {
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorCode = upgrades.ErrorCode;
                result.ErrorMessage = upgrades.ErrorMessage;
                return result;
            }

            if (!isResponse && !parsed.HasClose &&
                upgrades.UpgradeProtocols.Count != 0)
            {
                result.IsUpgradeRequested = true;
                CopyProtocols(upgrades.UpgradeProtocols, result.UpgradeProtocols);
            }

            if (isResponse && protocolVersion == HttpProtocolVersion.Http11 &&
                parsed.HasUpgrade && !parsed.HasClose &&
                upgrades.UpgradeProtocols.Count != 0)
            {
                return Upgrade(result, upgrades);
            }
        }

        if (framing.IsTunnel)
        {
            result.Status = HttpOperationStatus.Done;
            result.Decision.ConnectionMode = Http1ConnectionMode.Tunneled;
            result.Decision.IsTunnel = true;
            result.Decision.Reason =
                "A successful CONNECT response changed the connection into a tunnel.";
            return result;
        }

        if (parsed.HasClose)
        {
            return Close(
                result,
                "The close connection option requires closure after this message.");
        }

        if (!framing.IsSelfDelimited)
        {
            return Close(
                result,
                "The message is delimited by connection closure and cannot be reused.");
        }

        if (protocolVersion == HttpProtocolVersion.Http11)
        {
            return Persist(
                result,
                "HTTP/1.1 persistence is the default for a self-delimited message.");
        }

        if (protocolVersion == HttpProtocolVersion.Http10 && parsed.HasKeepAlive &&
            (!isProxyRecipient || isResponse))
        {
            return Persist(
                result,
                "The HTTP/1.0 keep-alive option enables persistence for this recipient.");
        }

        if (protocolVersion == HttpProtocolVersion.Http10 && parsed.HasKeepAlive &&
            isProxyRecipient && !isResponse)
        {
            return Close(
                result,
                "A proxy cannot maintain persistence with an HTTP/1.0 client request.");
        }

        return Close(
            result,
            "HTTP/1.0 closes after the response unless keep-alive is applicable.");
    }

    static void CopyProtocols(List<string> source, List<string> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }

    internal static void MarkUnusable(
        Http1ConnectionDecision decision,
        string reason)
    {
        decision.ConnectionMode = Http1ConnectionMode.Unusable;
        decision.IsPersistent = false;
        decision.MustClose = true;
        decision.IsUpgrade = false;
        decision.IsTunnel = false;
        decision.IsReusable = false;
        decision.ProtocolName = null;
        decision.Reason = reason;
    }

    static Http1ConnectionOptionsResult ParseOptions(
        HttpHeaders headers,
        HttpLimits limits)
    {
        Http1ConnectionOptionsResult result = new Http1ConnectionOptionsResult();
        int remainingOptions = limits.MaximumListMemberCount;
        int fieldIndex = 0;
        while (fieldIndex < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[fieldIndex];
            if (Ascii.EqualsStringsIgnoreCase(field.Name, HttpHeaderNames.Connection))
            {
                FieldValueParseResult parsed = FieldValueParser.ParseConnection(
                    field.Value,
                    limits.MaximumFieldValueSize,
                    remainingOptions);
                if (parsed.IsLimitExceeded)
                {
                    return RejectOptions(
                        result,
                        "http1-connection-option-limit-exceeded",
                        "The Connection field contains too many options.");
                }

                if (parsed.Status != HttpOperationStatus.Done)
                {
                    return RejectOptions(
                        result,
                        "invalid-http1-connection",
                        "The Connection field value is malformed.");
                }

                int optionIndex = 0;
                while (optionIndex < parsed.Values.Count)
                {
                    string option = Encoding.ASCII.GetString(parsed.Values[optionIndex]);
                    if (Ascii.EqualsStringsIgnoreCase(option, "close"))
                    {
                        result.HasClose = true;
                    }

                    if (Ascii.EqualsStringsIgnoreCase(option, "keep-alive"))
                    {
                        result.HasKeepAlive = true;
                    }

                    if (Ascii.EqualsStringsIgnoreCase(option, "upgrade"))
                    {
                        result.HasUpgrade = true;
                    }

                    optionIndex = optionIndex + 1;
                }

                remainingOptions = remainingOptions - parsed.Values.Count;
            }

            fieldIndex = fieldIndex + 1;
        }

        result.Status = HttpOperationStatus.Done;
        return result;
    }

    static Http1ConnectionOptionsResult ParseUpgrades(
        HttpHeaders headers,
        HttpLimits limits)
    {
        Http1ConnectionOptionsResult result = new Http1ConnectionOptionsResult();
        int remainingProtocols = limits.MaximumListMemberCount;
        int fieldIndex = 0;
        while (fieldIndex < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[fieldIndex];
            if (Ascii.EqualsStringsIgnoreCase(field.Name, HttpHeaderNames.Upgrade))
            {
                FieldValueParseResult parsed = FieldValueParser.ParseUpgrade(
                    field.Value,
                    limits.MaximumFieldValueSize,
                    remainingProtocols);
                if (parsed.IsLimitExceeded)
                {
                    return RejectOptions(
                        result,
                        "http1-upgrade-protocol-limit-exceeded",
                        "The Upgrade field contains too many protocols.");
                }

                if (parsed.Status != HttpOperationStatus.Done)
                {
                    return RejectOptions(
                        result,
                        "invalid-http1-upgrade",
                        "The Upgrade field value is malformed.");
                }

                int protocolIndex = 0;
                while (protocolIndex < parsed.Values.Count)
                {
                    result.UpgradeProtocols.Add(
                        Encoding.ASCII.GetString(parsed.Values[protocolIndex]));
                    protocolIndex = protocolIndex + 1;
                }

                remainingProtocols = remainingProtocols - parsed.Values.Count;
            }

            fieldIndex = fieldIndex + 1;
        }

        result.Status = HttpOperationStatus.Done;
        return result;
    }

    static Http1ConnectionResult Upgrade(
        Http1ConnectionResult result,
        Http1ConnectionOptionsResult upgrades)
    {
        result.Status = HttpOperationStatus.Done;
        result.Decision.ConnectionMode = Http1ConnectionMode.Upgraded;
        result.Decision.IsPersistent = false;
        result.Decision.MustClose = false;
        result.Decision.IsUpgrade = true;
        result.Decision.IsTunnel = false;
        result.Decision.IsReusable = false;
        result.Decision.ProtocolName = upgrades.UpgradeProtocols[0];
        result.Decision.Reason =
            "The 101 response changed the connection to the selected protocol.";
        return result;
    }

    static Http1ConnectionResult Persist(
        Http1ConnectionResult result,
        string reason)
    {
        result.Status = HttpOperationStatus.Done;
        result.Decision.ConnectionMode = Http1ConnectionMode.Persistent;
        result.Decision.IsPersistent = true;
        result.Decision.MustClose = false;
        result.Decision.IsReusable = true;
        result.Decision.Reason = reason;
        return result;
    }

    static Http1ConnectionResult Close(
        Http1ConnectionResult result,
        string reason)
    {
        result.Status = HttpOperationStatus.Done;
        result.Decision.ConnectionMode = Http1ConnectionMode.Close;
        result.Decision.IsPersistent = false;
        result.Decision.MustClose = true;
        result.Decision.IsReusable = false;
        result.Decision.Reason = reason;
        return result;
    }

    static Http1ConnectionOptionsResult RejectOptions(
        Http1ConnectionOptionsResult result,
        string code,
        string message)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }

}
