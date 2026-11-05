namespace pop3;

internal class Pop3ApopDigest
{
    internal Pop3ApopRequest CreateRequest(
        string timestamp,
        Pop3Credentials credentials,
        bool useUtf8)
    {
        if (credentials == null)
        {
            throw new System.ArgumentNullException("credentials");
        }
        ValidateTimestamp(timestamp);
        ValidateCredential(credentials.Username, "Username", useUtf8, true);
        ValidateCredential(credentials.Password, "Password", useUtf8, false);

        byte[] timestampBytes = System.Text.Encoding.ASCII.GetBytes(timestamp);
        System.Text.Encoding passwordEncoding;
        if (useUtf8)
        {
            passwordEncoding = new System.Text.UTF8Encoding(false, true);
        }
        else
        {
            passwordEncoding = System.Text.Encoding.ASCII;
        }
        byte[] passwordBytes = passwordEncoding.GetBytes(credentials.Password);
        byte[] digestInput = new byte[timestampBytes.Length + passwordBytes.Length];
        System.Array.Copy(timestampBytes, 0, digestInput, 0, timestampBytes.Length);
        System.Array.Copy(passwordBytes, 0, digestInput, timestampBytes.Length, passwordBytes.Length);
        byte[] digestBytes = System.Security.Cryptography.MD5.HashData(digestInput);
        string digest = FormatLowercaseHex(digestBytes);

        System.Array.Clear(passwordBytes, 0, passwordBytes.Length);
        System.Array.Clear(digestInput, 0, digestInput.Length);
        System.Array.Clear(digestBytes, 0, digestBytes.Length);

        Pop3ApopRequest request = new Pop3ApopRequest();
        request.Username = credentials.Username;
        request.Timestamp = timestamp;
        request.Digest = digest;
        return request;
    }

    void ValidateTimestamp(string timestamp)
    {
        if (timestamp == null)
        {
            throw new System.ArgumentNullException("timestamp");
        }
        if (timestamp.Length < 5 || timestamp[0] != '<' || timestamp[timestamp.Length - 1] != '>')
        {
            throw new System.IO.InvalidDataException("An APOP challenge must be a bracketed message identifier.");
        }

        int atIndex = -1;
        int index = 1;
        while (index < timestamp.Length - 1)
        {
            char character = timestamp[index];
            if (character < 33 || character > 126 || character == '<' || character == '>')
            {
                throw new System.IO.InvalidDataException("An APOP challenge contains an invalid character.");
            }
            if (character == '@')
            {
                if (atIndex >= 0)
                {
                    throw new System.IO.InvalidDataException("An APOP challenge contains more than one at-sign.");
                }
                atIndex = index;
            }
            index++;
        }
        if (atIndex <= 1 || atIndex >= timestamp.Length - 2)
        {
            throw new System.IO.InvalidDataException(
                "An APOP challenge must contain nonempty local and domain parts.");
        }
    }

    void ValidateCredential(string value, string fieldName, bool useUtf8, bool rejectWhitespace)
    {
        if (value == null)
        {
            throw new System.ArgumentException("An APOP " + fieldName + " value cannot be null.", "credentials");
        }
        if (value.Length == 0)
        {
            throw new System.ArgumentException("An APOP " + fieldName + " value cannot be empty.", "credentials");
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (System.Char.IsControl(character) ||
                (rejectWhitespace && System.Char.IsWhiteSpace(character)))
            {
                throw new System.ArgumentException(
                    "An APOP " + fieldName + " value contains an invalid character.",
                    "credentials");
            }
            if (!useUtf8 && character > 127)
            {
                throw new System.ArgumentException(
                    "An APOP " + fieldName + " value requires UTF8 USER support.",
                    "credentials");
            }
            if (character >= 55296 && character <= 56319)
            {
                if (index + 1 >= value.Length || value[index + 1] < 56320 || value[index + 1] > 57343)
                {
                    throw new System.ArgumentException(
                        "An APOP " + fieldName + " value must contain valid Unicode.",
                        "credentials");
                }
                index += 2;
            }
            else if (character >= 56320 && character <= 57343)
            {
                throw new System.ArgumentException(
                    "An APOP " + fieldName + " value must contain valid Unicode.",
                    "credentials");
            }
            else
            {
                index++;
            }
        }
    }

    string FormatLowercaseHex(byte[] value)
    {
        char[] characters = new char[value.Length * 2];
        int index = 0;
        while (index < value.Length)
        {
            characters[index * 2] = GetHexCharacter(value[index] >> 4);
            characters[index * 2 + 1] = GetHexCharacter(value[index] & 15);
            index++;
        }
        return new string(characters);
    }

    char GetHexCharacter(int value)
    {
        if (value < 10)
        {
            return (char)('0' + value);
        }
        return (char)('a' + value - 10);
    }
}
