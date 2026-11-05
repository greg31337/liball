using System;
using System.Globalization;
using System.Text;

namespace ftp;

internal static class FtpEndpointCodec
{
    internal static FtpEndpoint ParsePortArgument(string? argument)
    {
        int[] values = ParsePortValues(argument, "PORT");
        return CreateIpv4Endpoint(values, "PORT");
    }

    internal static string FormatPortArgument(FtpEndpoint? endpoint)
    {
        FtpEndpoint checkedEndpoint = RequireEndpoint(endpoint, FtpAddressFamily.IPv4, "PORT");
        int[] address = ParseIpv4Address(checkedEndpoint.Host, "PORT");
        int highPort = checkedEndpoint.Port / 256;
        int lowPort = checkedEndpoint.Port % 256;
        return address[0].ToString(CultureInfo.InvariantCulture) + "," +
            address[1].ToString(CultureInfo.InvariantCulture) + "," +
            address[2].ToString(CultureInfo.InvariantCulture) + "," +
            address[3].ToString(CultureInfo.InvariantCulture) + "," +
            highPort.ToString(CultureInfo.InvariantCulture) + "," +
            lowPort.ToString(CultureInfo.InvariantCulture);
    }

    internal static FtpEndpoint ParseExtendedPortArgument(string? argument)
    {
        FtpExtendedPortRequest request = ParseExtendedPortRequest(argument);
        if (!request.Supported)
        {
            throw new FtpProtocolException("The FTP EPRT network protocol is not supported.", null);
        }
        return request.Endpoint;
    }

    internal static FtpExtendedPortRequest ParseExtendedPortRequest(string? argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            throw new FtpProtocolException("An FTP EPRT argument is required.", null);
        }
        if (argument.Length > FtpDefaults.MaximumControlLineBytes)
        {
            throw new FtpProtocolException("An FTP EPRT argument exceeded the control-line limit.", null);
        }

        char delimiter = argument[0];
        ValidateDelimiter(delimiter, "EPRT");
        int protocolEnd = argument.IndexOf(delimiter, 1);
        int addressEnd = protocolEnd < 0 ? -1 : argument.IndexOf(delimiter, protocolEnd + 1);
        int portEnd = addressEnd < 0 ? -1 : argument.IndexOf(delimiter, addressEnd + 1);
        if (protocolEnd <= 1 || addressEnd <= protocolEnd + 1 ||
            portEnd <= addressEnd + 1 || portEnd != argument.Length - 1)
        {
            throw new FtpProtocolException("An FTP EPRT argument has invalid delimiter framing.", null);
        }

        int protocol = FtpCommandParser.ParseInt32InRange(
            argument.Substring(1, protocolEnd - 1), 1, int.MaxValue, "EPRT network protocol");
        string address = argument.Substring(protocolEnd + 1, addressEnd - protocolEnd - 1);
        int port = FtpCommandParser.ParseInt32InRange(
            argument.Substring(addressEnd + 1, portEnd - addressEnd - 1),
            1, 65535, "EPRT port");

        FtpExtendedPortRequest request = new FtpExtendedPortRequest();
        request.Protocol = protocol;
        request.Endpoint.Host = address;
        request.Endpoint.Port = port;
        if (protocol == 1 || protocol == 2)
        {
            request.Endpoint.AddressFamily = ParseNetworkProtocol(protocol, "EPRT");
            ValidateNumericAddress(address, request.Endpoint.AddressFamily, "EPRT");
            request.Supported = true;
        }
        return request;
    }

    internal static string FormatExtendedPortArgument(FtpEndpoint? endpoint, char delimiter)
    {
        if (endpoint == null)
        {
            throw new ArgumentNullException("endpoint");
        }
        ValidateDelimiter(delimiter, "EPRT");
        ValidatePort(endpoint.Port, "EPRT");
        int protocol = FormatNetworkProtocol(endpoint.AddressFamily, "EPRT");
        ValidateNumericAddress(endpoint.Host, endpoint.AddressFamily, "EPRT");

        string protocolText = protocol.ToString(CultureInfo.InvariantCulture);
        string portText = endpoint.Port.ToString(CultureInfo.InvariantCulture);
        if (protocolText.IndexOf(delimiter) >= 0 || endpoint.Host.IndexOf(delimiter) >= 0 ||
            portText.IndexOf(delimiter) >= 0)
        {
            throw new FtpProtocolException(
                "The FTP EPRT delimiter cannot occur in an encoded field.", null);
        }
        return delimiter + protocolText + delimiter + endpoint.Host + delimiter + portText + delimiter;
    }

    internal static string FormatExtendedPortDefault(FtpEndpoint? endpoint)
    {
        return FormatExtendedPortArgument(endpoint, '|');
    }

    internal static FtpExtendedPassiveRequest ParseExtendedPassiveArgument(string? argument)
    {
        FtpExtendedPassiveRequest request = new FtpExtendedPassiveRequest();
        if (argument == null)
        {
            return request;
        }
        if (argument.Length == 0)
        {
            throw new FtpProtocolException("An FTP EPSV argument cannot be empty.", null);
        }
        if (argument.Length > FtpDefaults.MaximumControlLineBytes)
        {
            throw new FtpProtocolException("An FTP EPSV argument exceeded the control-line limit.", null);
        }
        if (string.Equals(argument, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            request.All = true;
            return request;
        }

        int protocol = FtpCommandParser.ParseInt32InRange(
            argument, 1, int.MaxValue, "EPSV network protocol");
        request.Protocol = protocol;
        request.HasProtocol = true;
        if (protocol == 1 || protocol == 2)
        {
            request.AddressFamily = ParseNetworkProtocol(protocol, "EPSV");
        }
        else
        {
            request.Supported = false;
        }
        return request;
    }

    internal static string? FormatExtendedPassiveArgument(FtpExtendedPassiveRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentNullException("request");
        }
        if (request.All)
        {
            if (request.HasProtocol || request.Protocol != 0 ||
                request.AddressFamily != FtpAddressFamily.Unspecified)
            {
                throw new FtpProtocolException(
                    "An FTP EPSV ALL request cannot also specify a network protocol.", null);
            }
            return "ALL";
        }
        if (!request.HasProtocol)
        {
            if (request.Protocol != 0 || request.AddressFamily != FtpAddressFamily.Unspecified)
            {
                throw new FtpProtocolException(
                    "An FTP EPSV control-protocol request contains inconsistent protocol data.", null);
            }
            return null;
        }

        if (request.Protocol < 1)
        {
            throw new FtpProtocolException(
                "An FTP EPSV request contains an invalid network protocol number.", null);
        }
        if (!request.Supported)
        {
            if (request.AddressFamily != FtpAddressFamily.Unspecified ||
                request.Protocol == 1 || request.Protocol == 2)
            {
                throw new FtpProtocolException(
                    "An FTP EPSV request contains inconsistent unsupported protocol data.", null);
            }
            return request.Protocol.ToString(CultureInfo.InvariantCulture);
        }

        int expectedProtocol = FormatNetworkProtocol(request.AddressFamily, "EPSV");
        if (request.Protocol != expectedProtocol)
        {
            throw new FtpProtocolException(
                "An FTP EPSV request contains inconsistent network protocol data.", null);
        }
        return request.Protocol.ToString(CultureInfo.InvariantCulture);
    }

    internal static FtpPassiveReply ParsePassiveReply(FtpReply? reply)
    {
        FtpReply checkedReply = FtpReplyExtractor.RequireExpectedCode(
            reply, FtpReplyCodes.EnteringPassiveMode, "PASV");
        string payload = ExtractReplyPayload(checkedReply.Text, "PASV", false);
        int[] values = ParsePortValues(payload, "PASV");

        FtpPassiveReply passive = new FtpPassiveReply();
        passive.Reply = checkedReply;
        passive.Endpoint = CreateIpv4Endpoint(values, "PASV");
        return passive;
    }

    internal static FtpReply CreatePassiveReply(FtpEndpoint? endpoint)
    {
        string argument = FormatPortArgument(endpoint);
        return CreateEndpointReply(
            FtpReplyCodes.EnteringPassiveMode,
            "Entering Passive Mode (" + argument + ")");
    }

    internal static FtpPassiveReply ParseExtendedPassiveReply(FtpReply? reply)
    {
        FtpReply checkedReply = FtpReplyExtractor.RequireExpectedCode(
            reply, FtpReplyCodes.EnteringExtendedPassiveMode, "EPSV");
        string payload = ExtractReplyPayload(checkedReply.Text, "EPSV", true);
        if (payload.Length < 5)
        {
            throw new FtpProtocolException("An FTP EPSV reply is too short.", null);
        }
        char delimiter = payload[0];
        ValidateDelimiter(delimiter, "EPSV");
        if (payload[1] != delimiter || payload[2] != delimiter ||
            payload[payload.Length - 1] != delimiter)
        {
            throw new FtpProtocolException("An FTP EPSV reply has invalid delimiter framing.", null);
        }

        string portText = payload.Substring(3, payload.Length - 4);
        if (portText.IndexOf(delimiter) >= 0)
        {
            throw new FtpProtocolException("An FTP EPSV port contains its delimiter.", null);
        }
        int port = FtpCommandParser.ParseInt32InRange(portText, 1, 65535, "EPSV port");

        FtpPassiveReply passive = new FtpPassiveReply();
        passive.Reply = checkedReply;
        passive.Endpoint.Host = "";
        passive.Endpoint.Port = port;
        passive.Endpoint.AddressFamily = FtpAddressFamily.Unspecified;
        passive.Extended = true;
        return passive;
    }

    internal static FtpReply CreateExtendedPassiveReply(int port, char delimiter)
    {
        ValidatePort(port, "EPSV");
        ValidateDelimiter(delimiter, "EPSV");
        string portText = port.ToString(CultureInfo.InvariantCulture);
        if (portText.IndexOf(delimiter) >= 0)
        {
            throw new FtpProtocolException(
                "The FTP EPSV delimiter cannot occur in the encoded port.", null);
        }
        string payload = new string(delimiter, 3) + portText + delimiter;
        return CreateEndpointReply(
            FtpReplyCodes.EnteringExtendedPassiveMode,
            "Entering Extended Passive Mode (" + payload + ")");
    }

    internal static FtpNetworkProtocolReply ParseNetworkProtocolReply(FtpReply? reply)
    {
        FtpReply checkedReply = FtpReplyExtractor.RequireExpectedCode(
            reply, FtpReplyCodes.NetworkProtocolNotSupported, "network-protocol rejection");
        string payload = ExtractNetworkProtocolPayload(checkedReply.Text);

        int count = 1;
        int scan = 0;
        while (scan < payload.Length)
        {
            if (payload[scan] == ',')
            {
                count++;
            }
            scan++;
        }

        int[] protocols = new int[count];
        int start = 0;
        int protocolIndex = 0;
        int index = 0;
        while (index <= payload.Length)
        {
            if (index == payload.Length || payload[index] == ',')
            {
                protocols[protocolIndex] = FtpCommandParser.ParseInt32InRange(
                    payload.Substring(start, index - start), 1, int.MaxValue,
                    "522 network protocol");
                protocolIndex++;
                start = index + 1;
            }
            index++;
        }

        FtpNetworkProtocolReply result = new FtpNetworkProtocolReply();
        result.Reply = checkedReply;
        result.SupportedProtocols = protocols;
        return result;
    }

    internal static FtpReply CreateNetworkProtocolReply(int[]? supportedProtocols)
    {
        if (supportedProtocols == null)
        {
            throw new ArgumentNullException("supportedProtocols");
        }
        if (supportedProtocols.Length == 0)
        {
            throw new FtpProtocolException(
                "An FTP 522 reply must identify at least one supported network protocol.", null);
        }
        if (supportedProtocols.Length > FtpDefaults.MaximumControlLineBytes / 2)
        {
            throw new FtpProtocolException("An FTP 522 protocol list is too large.", null);
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < supportedProtocols.Length)
        {
            if (supportedProtocols[index] <= 0)
            {
                throw new FtpProtocolException(
                    "An FTP 522 reply contains an invalid network protocol number.", null);
            }
            if (index > 0)
            {
                builder.Append(',');
            }
            builder.Append(supportedProtocols[index].ToString(CultureInfo.InvariantCulture));
            index++;
        }

        string text = "Network protocol not supported, use (" + builder.ToString() + ")";
        if (Encoding.ASCII.GetByteCount(text) > FtpDefaults.MaximumControlLineBytes - 4)
        {
            throw new FtpProtocolException("An FTP 522 reply exceeded the control-line limit.", null);
        }
        return CreateEndpointReply(FtpReplyCodes.NetworkProtocolNotSupported, text);
    }

    internal static int[] ParsePortValues(string? text, string valueName)
    {
        if (text == null)
        {
            throw new FtpProtocolException($"An FTP {valueName} value is required.", null);
        }
        if (text.Length > FtpDefaults.MaximumControlLineBytes)
        {
            throw new FtpProtocolException($"An FTP {valueName} value is too large.", null);
        }
        int[] values = new int[6];
        int valueIndex = 0;
        int start = 0;
        int index = 0;
        while (index <= text.Length)
        {
            if (index == text.Length || text[index] == ',')
            {
                if (valueIndex >= values.Length)
                {
                    throw new FtpProtocolException(
                        $"An FTP {valueName} value must contain exactly six decimal fields.", null);
                }
                values[valueIndex] = FtpCommandParser.ParseInt32InRange(
                    text.Substring(start, index - start), 0, 255, valueName + " component");
                valueIndex++;
                start = index + 1;
            }
            index++;
        }
        if (valueIndex != values.Length)
        {
            throw new FtpProtocolException(
                $"An FTP {valueName} value must contain exactly six decimal fields.", null);
        }
        return values;
    }

    internal static FtpEndpoint CreateIpv4Endpoint(int[] values, string valueName)
    {
        int port = (values[4] * 256) + values[5];
        ValidatePort(port, valueName);
        FtpEndpoint endpoint = new FtpEndpoint();
        endpoint.Host = values[0].ToString(CultureInfo.InvariantCulture) + "." +
            values[1].ToString(CultureInfo.InvariantCulture) + "." +
            values[2].ToString(CultureInfo.InvariantCulture) + "." +
            values[3].ToString(CultureInfo.InvariantCulture);
        endpoint.Port = port;
        endpoint.AddressFamily = FtpAddressFamily.IPv4;
        return endpoint;
    }

    internal static int[] ParseIpv4Address(string address, string valueName)
    {
        if (string.IsNullOrEmpty(address))
        {
            throw new FtpProtocolException($"An FTP {valueName} IPv4 address is required.", null);
        }
        int[] values = new int[4];
        int valueIndex = 0;
        int start = 0;
        int index = 0;
        while (index <= address.Length)
        {
            if (index == address.Length || address[index] == '.')
            {
                if (valueIndex >= values.Length)
                {
                    throw new FtpProtocolException(
                        $"An FTP {valueName} IPv4 address must contain four octets.", null);
                }
                values[valueIndex] = FtpCommandParser.ParseInt32InRange(
                    address.Substring(start, index - start), 0, 255,
                    valueName + " IPv4 octet");
                valueIndex++;
                start = index + 1;
            }
            index++;
        }
        if (valueIndex != values.Length)
        {
            throw new FtpProtocolException(
                $"An FTP {valueName} IPv4 address must contain four octets.", null);
        }
        return values;
    }

    internal static FtpEndpoint RequireEndpoint(
        FtpEndpoint? endpoint, FtpAddressFamily requiredFamily, string valueName)
    {
        if (endpoint == null)
        {
            throw new ArgumentNullException("endpoint");
        }
        if (endpoint.AddressFamily != requiredFamily)
        {
            throw new FtpProtocolException(
                $"An FTP {valueName} endpoint requires the correct address family.", null);
        }
        ValidatePort(endpoint.Port, valueName);
        return endpoint;
    }

    internal static void ValidateNumericAddress(
        string address, FtpAddressFamily family, string valueName)
    {
        if (string.IsNullOrEmpty(address))
        {
            throw new FtpProtocolException($"An FTP {valueName} numeric address is required.", null);
        }
        if (family == FtpAddressFamily.IPv4)
        {
            ParseIpv4Address(address, valueName);
            return;
        }
        if (family == FtpAddressFamily.IPv6)
        {
            if (address.IndexOf('%') >= 0 || address.IndexOf('[') >= 0 ||
                address.IndexOf(']') >= 0 || Uri.CheckHostName(address) != UriHostNameType.IPv6)
            {
                throw new FtpProtocolException(
                    $"An FTP {valueName} IPv6 address is malformed.", null);
            }
            return;
        }
        throw new FtpProtocolException(
            $"An FTP {valueName} endpoint requires IPv4 or IPv6.", null);
    }

    internal static FtpAddressFamily ParseNetworkProtocol(int protocol, string valueName)
    {
        if (protocol == 1)
        {
            return FtpAddressFamily.IPv4;
        }
        if (protocol == 2)
        {
            return FtpAddressFamily.IPv6;
        }
        throw new FtpProtocolException(
            $"The FTP {valueName} network protocol is not supported.", null);
    }

    internal static int FormatNetworkProtocol(FtpAddressFamily family, string valueName)
    {
        if (family == FtpAddressFamily.IPv4)
        {
            return 1;
        }
        if (family == FtpAddressFamily.IPv6)
        {
            return 2;
        }
        throw new FtpProtocolException(
            $"An FTP {valueName} endpoint requires IPv4 or IPv6.", null);
    }

    internal static void ValidateDelimiter(char delimiter, string valueName)
    {
        if (delimiter < '!' || delimiter > '~')
        {
            throw new FtpProtocolException(
                $"An FTP {valueName} delimiter must be printable ASCII from 33 through 126.", null);
        }
    }

    internal static void ValidatePort(int port, string valueName)
    {
        if (port < 1 || port > 65535)
        {
            throw new FtpProtocolException(
                $"An FTP {valueName} TCP port must be between 1 and 65535.", null);
        }
    }

    internal static string ExtractReplyPayload(string text, string valueName, bool extended)
    {
        if (text.Length > FtpDefaults.MaximumControlLineBytes)
        {
            throw new FtpProtocolException($"An FTP {valueName} reply is too large.", null);
        }
        int opening = text.IndexOf('(');
        int closing = text.LastIndexOf(')');
        if (opening < 0 || closing <= opening)
        {
            throw new FtpProtocolException(
                $"An FTP {valueName} reply must contain a parenthesized value.", null);
        }
        if (!extended && (text.IndexOf('(', opening + 1) >= 0 ||
            text.IndexOf(')', opening + 1) != closing))
        {
            throw new FtpProtocolException(
                $"An FTP {valueName} reply contains ambiguous parentheses.", null);
        }
        return text.Substring(opening + 1, closing - opening - 1);
    }

    internal static string ExtractNetworkProtocolPayload(string text)
    {
        if (text.Length == 0 || text.Length > FtpDefaults.MaximumControlLineBytes)
        {
            throw new FtpProtocolException("An FTP 522 reply has invalid text length.", null);
        }
        int opening = text.IndexOf('(');
        int closing = text.IndexOf(')', opening + 1);
        if (opening < 0 || closing != text.Length - 1 ||
            text.IndexOf(')') != closing ||
            text.IndexOf('(', opening + 1) >= 0 || text.IndexOf(')', closing + 1) >= 0 ||
            closing == opening + 1)
        {
            throw new FtpProtocolException(
                "An FTP 522 reply must end with one nonempty parenthesized protocol list.", null);
        }
        return text.Substring(opening + 1, closing - opening - 1);
    }

    internal static FtpReply CreateEndpointReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        reply.Classification = FtpReplyClassifier.ClassifyCode(code);
        return reply;
    }
}
