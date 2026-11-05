namespace pop3;

internal class Pop3ResponseParser
{
    internal Pop3Response ParseResponse(System.ReadOnlySpan<byte> line, bool isGreeting, int maximumSaslLineOctets)
    {
        if (maximumSaslLineOctets < 2)
        {
            throw new System.ArgumentOutOfRangeException("maximumSaslLineOctets");
        }
        if (line.Length == 0)
        {
            throw new System.IO.InvalidDataException("A POP3 response cannot be empty.");
        }

        Pop3Response response = new Pop3Response();
        int textStart;
        if (StartsWith(line, "+OK"))
        {
            ValidateNormalResponseLength(line.Length);
            response.Kind = Pop3ResponseKind.Positive;
            textStart = GetNormalTextStart(line, 3);
        }
        else if (StartsWith(line, "-ERR"))
        {
            ValidateNormalResponseLength(line.Length);
            response.Kind = Pop3ResponseKind.Negative;
            textStart = GetNormalTextStart(line, 4);
        }
        else if (line.Length >= 2 && line[0] == 43 && line[1] == 32)
        {
            if (line.Length > maximumSaslLineOctets - 2)
            {
                throw new System.IO.InvalidDataException("A POP3 SASL continuation exceeds its configured octet limit.");
            }
            response.Kind = Pop3ResponseKind.SaslContinuation;
            textStart = 2;
        }
        else
        {
            throw new System.IO.InvalidDataException("A POP3 response has an invalid status indicator.");
        }

        System.ReadOnlySpan<byte> textBytes = line.Slice(textStart, line.Length - textStart);
        ValidateTextBytes(textBytes);
        response.Text = DecodeText(textBytes);
        if (response.Kind != Pop3ResponseKind.SaslContinuation)
        {
            Pop3ResponseCodeParser responseCodeParser = new Pop3ResponseCodeParser();
            response.ResponseCodeLevels = responseCodeParser.ParseResponseCode(textBytes);
        }
        if (isGreeting && response.Kind == Pop3ResponseKind.Positive)
        {
            response.GreetingTimestamp = FindGreetingTimestamp(response.Text);
        }
        return response;
    }

    internal byte[] FormatResponse(Pop3Response response, int maximumSaslLineOctets)
    {
        if (response == null)
        {
            throw new System.ArgumentNullException("response");
        }
        if (maximumSaslLineOctets < 2)
        {
            throw new System.ArgumentOutOfRangeException("maximumSaslLineOctets");
        }
        if (response.Text == null)
        {
            throw new System.IO.InvalidDataException("POP3 response text cannot be null.");
        }

        string text = AddResponseCode(response);
        text = AddGreetingTimestamp(response, text);
        ValidateTextCharacters(text);
        byte[] textBytes = EncodeText(text);
        byte[] marker;
        bool includeSeparator;
        int maximumContentOctets;

        switch (response.Kind)
        {
            case Pop3ResponseKind.Positive:
                marker = new byte[] { 43, 79, 75 };
                includeSeparator = textBytes.Length > 0;
                maximumContentOctets = Pop3Defaults.MaximumResponseLineOctets - 2;
                break;
            case Pop3ResponseKind.Negative:
                marker = new byte[] { 45, 69, 82, 82 };
                includeSeparator = textBytes.Length > 0;
                maximumContentOctets = Pop3Defaults.MaximumResponseLineOctets - 2;
                break;
            case Pop3ResponseKind.SaslContinuation:
                marker = new byte[] { 43 };
                includeSeparator = true;
                maximumContentOctets = maximumSaslLineOctets - 2;
                break;
            default:
                throw new System.IO.InvalidDataException("The POP3 response kind is invalid.");
        }

        int separatorLength = includeSeparator ? 1 : 0;
        if (textBytes.Length > maximumContentOctets - marker.Length - separatorLength)
        {
            throw new System.IO.InvalidDataException("A POP3 response exceeds its permitted octet count.");
        }

        byte[] line = new byte[marker.Length + separatorLength + textBytes.Length];
        System.Array.Copy(marker, 0, line, 0, marker.Length);
        int writeIndex = marker.Length;
        if (includeSeparator)
        {
            line[writeIndex] = 32;
            writeIndex++;
        }
        System.Array.Copy(textBytes, 0, line, writeIndex, textBytes.Length);
        return line;
    }

    bool StartsWith(System.ReadOnlySpan<byte> line, string marker)
    {
        if (line.Length < marker.Length)
        {
            return false;
        }

        int index = 0;
        while (index < marker.Length)
        {
            if (line[index] != marker[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }

    int GetNormalTextStart(System.ReadOnlySpan<byte> line, int markerLength)
    {
        if (line.Length == markerLength)
        {
            return markerLength;
        }
        if (line[markerLength] != 32)
        {
            throw new System.IO.InvalidDataException("A POP3 status indicator must be followed by a space or the end of the line.");
        }
        return markerLength + 1;
    }

    void ValidateNormalResponseLength(int contentOctets)
    {
        if (contentOctets > Pop3Defaults.MaximumResponseLineOctets - 2)
        {
            throw new System.IO.InvalidDataException("A POP3 response exceeds 512 octets including CRLF.");
        }
    }

    void ValidateTextBytes(System.ReadOnlySpan<byte> text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] < 32 || text[index] == 127)
            {
                throw new System.IO.InvalidDataException("POP3 response text contains a control octet.");
            }
            index++;
        }
    }

    void ValidateTextCharacters(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] < 32 || text[index] == 127)
            {
                throw new System.IO.InvalidDataException("POP3 response text contains a control character.");
            }
            index++;
        }
    }

    string DecodeText(System.ReadOnlySpan<byte> text)
    {
        System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding(false, true);
        return encoding.GetString(text);
    }

    byte[] EncodeText(string text)
    {
        System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding(false, true);
        return encoding.GetBytes(text);
    }

    string? FindGreetingTimestamp(string text)
    {
        int searchStart = GetTimestampSearchStart(text);
        int start = text.IndexOf('<', searchStart);
        if (start < 0)
        {
            return null;
        }

        int end = text.IndexOf('>', start + 1);
        if (end < 0)
        {
            throw new System.IO.InvalidDataException("A POP3 greeting contains an unterminated timestamp.");
        }

        string timestamp = text.Substring(start, end - start + 1);
        ValidateGreetingTimestamp(timestamp);
        if (text.IndexOf('<', end + 1) >= 0)
        {
            throw new System.IO.InvalidDataException("A POP3 greeting contains more than one timestamp.");
        }
        return timestamp;
    }

    string AddResponseCode(Pop3Response response)
    {
        if (response.ResponseCodeLevels == null)
        {
            throw new System.IO.InvalidDataException("POP3 response code levels cannot be null.");
        }
        if (response.ResponseCodeLevels.Length == 0)
        {
            return response.Text;
        }
        if (response.Kind == Pop3ResponseKind.SaslContinuation)
        {
            throw new System.IO.InvalidDataException("A POP3 SASL continuation cannot contain a response code.");
        }

        Pop3ResponseCodeParser responseCodeParser = new Pop3ResponseCodeParser();
        byte[] formattedCode = responseCodeParser.FormatResponseCode(response.ResponseCodeLevels);
        byte[] textBytes = EncodeText(response.Text);
        string[] existingLevels = responseCodeParser.ParseResponseCode(textBytes);
        if (existingLevels.Length > 0)
        {
            if (!responseCodeParser.ResponseCodesEqual(response.ResponseCodeLevels, existingLevels))
            {
                throw new System.IO.InvalidDataException("POP3 response text contains a different response code.");
            }
            return response.Text;
        }

        string code = System.Text.Encoding.ASCII.GetString(formattedCode);
        if (response.Text.Length == 0)
        {
            return code;
        }
        return code + " " + response.Text;
    }

    string AddGreetingTimestamp(Pop3Response response, string text)
    {
        if (response.GreetingTimestamp == null)
        {
            return text;
        }
        if (response.Kind != Pop3ResponseKind.Positive)
        {
            throw new System.IO.InvalidDataException("Only a positive POP3 greeting can contain a timestamp.");
        }

        ValidateGreetingTimestamp(response.GreetingTimestamp);
        string? existingTimestamp = FindGreetingTimestamp(text);
        if (existingTimestamp != null)
        {
            if (existingTimestamp == response.GreetingTimestamp)
            {
                return text;
            }
            throw new System.IO.InvalidDataException(
                "POP3 greeting text contains a different timestamp.");
        }
        if (text.Length == 0)
        {
            return response.GreetingTimestamp;
        }
        return text + " " + response.GreetingTimestamp;
    }

    int GetTimestampSearchStart(string text)
    {
        if (text.Length == 0 || text[0] != '[')
        {
            return 0;
        }

        int end = text.IndexOf(']');
        if (end < 0)
        {
            return 0;
        }
        return end + 1;
    }

    void ValidateGreetingTimestamp(string timestamp)
    {
        if (timestamp.Length < 5 || timestamp[0] != '<' || timestamp[timestamp.Length - 1] != '>')
        {
            throw new System.IO.InvalidDataException("A POP3 greeting timestamp must be a bracketed message identifier.");
        }

        int atIndex = -1;
        int index = 1;
        while (index < timestamp.Length - 1)
        {
            char value = timestamp[index];
            if (value < 33 || value > 126 || value == '<' || value == '>')
            {
                throw new System.IO.InvalidDataException("A POP3 greeting timestamp contains an invalid character.");
            }
            if (value == '@')
            {
                if (atIndex >= 0)
                {
                    throw new System.IO.InvalidDataException("A POP3 greeting timestamp contains more than one at-sign.");
                }
                atIndex = index;
            }
            index++;
        }

        if (atIndex <= 1 || atIndex >= timestamp.Length - 2)
        {
            throw new System.IO.InvalidDataException("A POP3 greeting timestamp must contain nonempty local and domain parts.");
        }
    }
}
