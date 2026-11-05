using System;

namespace websocket;

internal class WebSocketSubprotocolOfferResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal int FieldCount;
    internal string[] Protocols = Array.Empty<string>();
}

internal class WebSocketSubprotocolSelectionResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal bool HasSelection;
    internal string SelectedProtocol = "";
}

internal static class WebSocketSubprotocolNegotiator
{
    internal static WebSocketSubprotocolOfferResult ParseClientOffers(
        WebSocketHeaderField[]? fields,
        int maximumProtocolCount)
    {
        WebSocketSubprotocolOfferResult result = new WebSocketSubprotocolOfferResult();
        HeaderTokenValuesResult values = HeaderFieldLookup.ReadTokenValues(
            fields,
            WebSocketProtocolConstants.ProtocolHeaderName,
            maximumProtocolCount);
        result.Status = values.Status;
        result.FieldCount = values.FieldCount;
        if (values.Status != WebSocketParseStatus.Complete)
        {
            return result;
        }

        if ((values.FieldCount != 0 && values.Tokens.Length == 0) ||
            !ValidateProtocols(values.Tokens))
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
            return result;
        }

        result.Protocols = values.Tokens;
        return result;
    }

    internal static bool ValidateProtocols(string[]? protocols)
    {
        if (protocols == null)
        {
            return false;
        }

        int protocolIndex = 0;
        while (protocolIndex < protocols.Length)
        {
            string protocol = protocols[protocolIndex];
            if (protocol == null || !AsciiToken.IsValid(protocol))
            {
                return false;
            }

            int comparisonIndex = protocolIndex + 1;
            while (comparisonIndex < protocols.Length)
            {
                string comparison = protocols[comparisonIndex];
                if (comparison == null || EqualsCaseSensitive(protocol, comparison))
                {
                    return false;
                }

                comparisonIndex = comparisonIndex + 1;
            }

            protocolIndex = protocolIndex + 1;
        }

        return true;
    }

    internal static string SelectFirstMatch(string[] preferred, string[] available)
    {
        if (!ValidateProtocols(preferred) || !ValidateProtocols(available))
        {
            return "";
        }

        int preferredIndex = 0;
        while (preferredIndex < preferred.Length)
        {
            string candidate = preferred[preferredIndex];
            if (ContainsProtocol(available, candidate))
            {
                return candidate;
            }

            preferredIndex = preferredIndex + 1;
        }

        return "";
    }

    internal static WebSocketSubprotocolSelectionResult Choose(
        string[]? clientProtocols,
        string[]? serverProtocols,
        IWebSocketSubprotocolSelector? selector)
    {
        WebSocketSubprotocolSelectionResult result = new WebSocketSubprotocolSelectionResult();
        if (clientProtocols == null ||
            serverProtocols == null ||
            !ValidateProtocols(clientProtocols) ||
            !ValidateProtocols(serverProtocols) ||
            selector == null)
        {
            return result;
        }

        string[] clientCopy = CopyProtocols(clientProtocols);
        string[] serverCopy = CopyProtocols(serverProtocols);
        string selected = selector.SelectSubprotocol(clientCopy, serverCopy);
        if (selected == null ||
            (selected.Length != 0 &&
                (!AsciiToken.IsValid(selected) ||
                    !ContainsProtocol(clientProtocols, selected) ||
                    !ContainsProtocol(serverProtocols, selected))))
        {
            return result;
        }

        result.Status = WebSocketParseStatus.Complete;
        if (selected.Length != 0)
        {
            result.HasSelection = true;
            result.SelectedProtocol = selected;
        }

        return result;
    }

    static string[] CopyProtocols(string[] protocols)
    {
        string[] copy = new string[protocols.Length];
        int index = 0;
        while (index < protocols.Length)
        {
            copy[index] = protocols[index];
            index = index + 1;
        }

        return copy;
    }

    static bool ContainsProtocol(string[] protocols, ReadOnlySpan<char> candidate)
    {
        int protocolIndex = 0;
        while (protocolIndex < protocols.Length)
        {
            if (EqualsCaseSensitive(protocols[protocolIndex], candidate))
            {
                return true;
            }

            protocolIndex = protocolIndex + 1;
        }

        return false;
    }

    static bool EqualsCaseSensitive(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
