namespace sip;

internal class SipWebSocketHandshake
{
    const string Magic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    internal static string CreateKey()
    {
        byte[] bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        return System.Convert.ToBase64String(bytes);
    }

    internal static string CreateClientRequest(string path, string host,
        string? origin, string key)
    {
        string result = "GET " + path + " HTTP/1.1\r\n"
            + "Host: " + host + "\r\n"
            + "Upgrade: websocket\r\n"
            + "Connection: Upgrade\r\n"
            + "Sec-WebSocket-Key: " + key + "\r\n"
            + "Sec-WebSocket-Version: 13\r\n"
            + "Sec-WebSocket-Protocol: "
            + SipTransportNames.WebSocketSubprotocol + "\r\n";
        if (origin != null)
        {
            result = result + "Origin: " + origin + "\r\n";
        }
        return result + "\r\n";
    }

    internal static string? ValidateClientRequest(string header, string path,
        string? expectedOrigin)
    {
        string[] lines = SplitLines(header);
        string? host = GetHeader(lines, "Host");
        if (lines.Length == 0
            || !System.String.Equals(lines[0], "GET " + path + " HTTP/1.1",
                System.StringComparison.Ordinal)
            || host == null || host.Length == 0
            || !HeaderHasToken(lines, "Upgrade", "websocket")
            || !HeaderHasToken(lines, "Connection", "Upgrade")
            || !System.String.Equals(GetHeader(lines, "Sec-WebSocket-Version"),
                "13", System.StringComparison.Ordinal)
            || !HeaderHasExactToken(lines, "Sec-WebSocket-Protocol",
                SipTransportNames.WebSocketSubprotocol))
        {
            return null;
        }
        string? key = GetHeader(lines, "Sec-WebSocket-Key");
        if (!ValidKey(key))
        {
            return null;
        }
        if (expectedOrigin != null
            && !System.String.Equals(GetHeader(lines, "Origin"), expectedOrigin,
                System.StringComparison.Ordinal))
        {
            return null;
        }
        return key;
    }

    internal static string CreateServerResponse(string key)
    {
        return "HTTP/1.1 101 Switching Protocols\r\n"
            + "Upgrade: websocket\r\n"
            + "Connection: Upgrade\r\n"
            + "Sec-WebSocket-Accept: " + CreateAccept(key) + "\r\n"
            + "Sec-WebSocket-Protocol: "
            + SipTransportNames.WebSocketSubprotocol + "\r\n\r\n";
    }

    internal static bool ValidateServerResponse(string header, string key)
    {
        string[] lines = SplitLines(header);
        return lines.Length > 0 && IsSwitchingProtocols(lines[0])
            && HeaderHasToken(lines, "Upgrade", "websocket")
            && HeaderHasToken(lines, "Connection", "Upgrade")
            && System.String.Equals(GetHeader(lines, "Sec-WebSocket-Accept"),
                CreateAccept(key), System.StringComparison.Ordinal)
            && System.String.Equals(GetHeader(lines, "Sec-WebSocket-Protocol"),
                SipTransportNames.WebSocketSubprotocol,
                System.StringComparison.Ordinal);
    }

    internal static string? ReadHeader(System.IO.Stream stream, int maximumBytes)
    {
        System.Collections.Generic.List<byte> bytes =
            new System.Collections.Generic.List<byte>();
        while (bytes.Count < maximumBytes)
        {
            int value = stream.ReadByte();
            if (value < 0)
            {
                return null;
            }
            bytes.Add((byte)value);
            int count = bytes.Count;
            if (count >= 4 && bytes[count - 4] == 13 && bytes[count - 3] == 10
                && bytes[count - 2] == 13 && bytes[count - 1] == 10)
            {
                return System.Text.Encoding.ASCII.GetString(bytes.ToArray());
            }
        }
        return null;
    }

    static string CreateAccept(string key)
    {
        byte[] source = System.Text.Encoding.ASCII.GetBytes(key + Magic);
        byte[] digest = System.Security.Cryptography.SHA1.HashData(source);
        return System.Convert.ToBase64String(digest);
    }

    static string[] SplitLines(string value)
    {
        return value.Split(new string[] { "\r\n" },
            System.StringSplitOptions.None);
    }

    static bool IsSwitchingProtocols(string line)
    {
        return (line.StartsWith("HTTP/1.1 101 ",
                    System.StringComparison.Ordinal)
                || System.String.Equals(line, "HTTP/1.1 101",
                    System.StringComparison.Ordinal));
    }

    static string? GetHeader(string[] lines, string name)
    {
        string? result = null;
        int index = 1;
        while (index < lines.Length)
        {
            int colon = lines[index].IndexOf(':');
            if (colon > 0 && System.String.Equals(
                lines[index].Substring(0, colon).Trim(), name,
                System.StringComparison.OrdinalIgnoreCase))
            {
                if (result != null)
                {
                    return null;
                }
                result = lines[index].Substring(colon + 1).Trim();
            }
            index = index + 1;
        }
        return result;
    }

    static bool HeaderHasToken(string[] lines, string name, string token)
    {
        string? value = GetHeader(lines, name);
        if (value == null)
        {
            return false;
        }
        string[] tokens = value.Split(',');
        int index = 0;
        while (index < tokens.Length)
        {
            if (System.String.Equals(tokens[index].Trim(), token,
                System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HeaderHasExactToken(string[] lines, string name, string token)
    {
        string? value = GetHeader(lines, name);
        if (value == null)
        {
            return false;
        }
        string[] tokens = value.Split(',');
        int index = 0;
        while (index < tokens.Length)
        {
            if (System.String.Equals(tokens[index].Trim(), token,
                System.StringComparison.Ordinal))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ValidKey(string? value)
    {
        if (value == null || value.Length != 24
            || value[22] != '=' || value[23] != '=')
        {
            return false;
        }
        int index = 0;
        while (index < 22)
        {
            char character = value[index];
            if (!(character >= 'A' && character <= 'Z')
                && !(character >= 'a' && character <= 'z')
                && !(character >= '0' && character <= '9')
                && character != '+' && character != '/')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }
}
