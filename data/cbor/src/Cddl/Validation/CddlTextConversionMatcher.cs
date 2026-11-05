using System.Numerics;

namespace cbor;

internal class CddlTextConversionMatcher
{
    internal CddlCoreMatcher Matcher;
    internal CddlValidationState State;
    internal string? ErrorMessage;
    internal string? ErrorActual;

    internal CddlTextConversionMatcher(
        CddlCoreMatcher matcher,
        CddlValidationState state)
    {
        Matcher = matcher;
        State = state;
    }

    internal CddlMatchResult MatchByteEncodingControl(
        CddlNode node,
        CborValue value)
    {
        string text = value.TextStringValue ?? "";
        string name = node.ControlName ?? "";
        byte[]? bytes = null;
        if (name == CddlControlNames.Base64Url)
        {
            bytes = DecodeBase64(text, true, true, false, node);
        }
        else if (name == CddlControlNames.Base64UrlSloppy)
        {
            bytes = DecodeBase64(text, true, true, true, node);
        }
        else if (name == CddlControlNames.Base64Classic)
        {
            bytes = DecodeBase64(text, false, false, false, node);
        }
        else if (name == CddlControlNames.Base64ClassicSloppy)
        {
            bytes = DecodeBase64(text, false, false, true, node);
        }
        else if (name == CddlControlNames.Base32)
        {
            bytes = DecodeBase32(text, false, node);
        }
        else if (name == CddlControlNames.Base32Hex)
        {
            bytes = DecodeBase32(text, true, node);
        }
        else if (name == CddlControlNames.Base45)
        {
            bytes = DecodeBase45(text, node);
        }
        else if (name == CddlControlNames.Hex
            || name == CddlControlNames.HexLower
            || name == CddlControlNames.HexUpper)
        {
            bytes = DecodeHex(text, name, node);
        }
        if (bytes == null)
        {
            if (State.Diagnostics.HasErrors)
            {
                return Matcher.CreateNoIssueRejection();
            }
            return RejectConversion(node, text);
        }
        CborValue converted = new CborValue();
        converted.Kind = CborValueKind.ByteString;
        converted.ByteStringValue = bytes;
        converted.LengthKind = CborLengthKind.Definite;
        return MatchConvertedController(
            node,
            converted,
            "bytes accepted by the encoding controller",
            bytes.Length + " decoded byte(s)"
        );
    }

    internal CddlMatchResult MatchBase10Control(
        CddlNode node,
        CborValue value)
    {
        string text = value.TextStringValue ?? "";
        CborValue? converted = DecodeBase10(text, node);
        if (converted == null)
        {
            if (State.Diagnostics.HasErrors)
            {
                return Matcher.CreateNoIssueRejection();
            }
            return RejectConversion(node, text);
        }
        return MatchConvertedController(
            node,
            converted,
            "an integer accepted by the base10 controller",
            converted.IntegerValue.ToString()
        );
    }

    internal CddlMatchResult MatchConvertedController(
        CddlNode node,
        CborValue converted,
        string expected,
        string actual)
    {
        int checkpoint = State.CreateControlResultCheckpoint();
        CddlMatchResult controller = Matcher.MatchNode(
            node.Children[1],
            converted
        );
        if (controller.IsMatch)
        {
            return Matcher.CreateMatch();
        }
        State.RestoreControlResults(checkpoint);
        if (State.Diagnostics.HasErrors)
        {
            return Matcher.CreateNoIssueRejection();
        }
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.ControlMismatch,
            "The decoded text does not satisfy the CDDL conversion controller.",
            node,
            expected,
            actual
        );
        if (controller.Issue != null)
        {
            issue.Details = [controller.Issue];
        }
        return Matcher.CreateRejection(issue);
    }

    internal CddlMatchResult RejectConversion(
        CddlNode node,
        string text)
    {
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.ControlMismatch,
            ErrorMessage ?? "The text is not a valid encoded representation.",
            node,
            "a valid " + (node.ControlName ?? "conversion") + " text",
            ErrorActual ?? text
        );
        return Matcher.CreateRejection(issue);
    }

    internal byte[]? DecodeBase64(
        string text,
        bool url,
        bool unpadded,
        bool sloppy,
        CddlNode node)
    {
        int dataLength = text.Length;
        if (unpadded)
        {
            if (text.Length % 4 == 1)
            {
                SetError(
                    "The unpadded base64 text has an invalid length.",
                    "invalid base64 length"
                );
                return null;
            }
        }
        else
        {
            if (text.Length % 4 != 0)
            {
                SetError(
                    "Classic base64 text requires RFC 4648 padding.",
                    "invalid base64 padding"
                );
                return null;
            }
            int padding = 0;
            if (dataLength > 0 && text[dataLength - 1] == '=')
            {
                padding = 1;
                dataLength = dataLength - 1;
            }
            if (dataLength > 0 && text[dataLength - 1] == '=')
            {
                padding = 2;
                dataLength = dataLength - 1;
            }
            bool paddingValid = padding == 0 && dataLength % 4 == 0
                || padding == 1 && dataLength % 4 == 3
                || padding == 2 && dataLength % 4 == 2;
            if (!paddingValid)
            {
                SetError(
                    "Classic base64 text has invalid RFC 4648 padding.",
                    "invalid base64 padding"
                );
                return null;
            }
        }
        int index = 0;
        while (index < dataLength)
        {
            if (Base64Digit(text[index], url) < 0)
            {
                SetError(
                    "The text contains a character outside the selected base64 alphabet.",
                    "invalid base64 character at index " + index
                );
                return null;
            }
            index = index + 1;
        }
        int remainder = dataLength % 4;
        int outputLength = dataLength / 4 * 3
            + remainder * 6 / 8;
        if (!State.TakeEmbeddedBytes(outputLength, node))
        {
            return null;
        }
        byte[] bytes = new byte[outputLength];
        int buffer = 0;
        int bitCount = 0;
        int outputIndex = 0;
        index = 0;
        while (index < dataLength)
        {
            buffer = (buffer << 6) | Base64Digit(text[index], url);
            bitCount = bitCount + 6;
            if (bitCount >= 8)
            {
                bitCount = bitCount - 8;
                bytes[outputIndex] = (byte)(buffer >> bitCount);
                outputIndex = outputIndex + 1;
                if (bitCount == 0)
                {
                    buffer = 0;
                }
                else
                {
                    buffer = buffer & ((1 << bitCount) - 1);
                }
            }
            index = index + 1;
        }
        if (!sloppy && buffer != 0)
        {
            SetError(
                "Unused base64 bits must be zero.",
                "non-zero trailing base64 bits"
            );
            return null;
        }
        return bytes;
    }

    internal int Base64Digit(char value, bool url)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return value - 'A';
        }
        if (value >= 'a' && value <= 'z')
        {
            return value - 'a' + 26;
        }
        if (value >= '0' && value <= '9')
        {
            return value - '0' + 52;
        }
        if (url && value == '-')
        {
            return 62;
        }
        if (url && value == '_')
        {
            return 63;
        }
        if (!url && value == '+')
        {
            return 62;
        }
        if (!url && value == '/')
        {
            return 63;
        }
        return -1;
    }

    internal byte[]? DecodeBase32(
        string text,
        bool hexadecimal,
        CddlNode node)
    {
        int remainder = text.Length % 8;
        bool lengthValid = remainder == 0
            || remainder == 2
            || remainder == 4
            || remainder == 5
            || remainder == 7;
        if (!lengthValid)
        {
            SetError(
                "The unpadded base32 text has an invalid length.",
                "invalid base32 length"
            );
            return null;
        }
        int index = 0;
        while (index < text.Length)
        {
            if (Base32Digit(text[index], hexadecimal) < 0)
            {
                SetError(
                    "The text contains a character outside the selected base32 alphabet.",
                    "invalid base32 character at index " + index
                );
                return null;
            }
            index = index + 1;
        }
        int outputLength = text.Length / 8 * 5
            + remainder * 5 / 8;
        if (!State.TakeEmbeddedBytes(outputLength, node))
        {
            return null;
        }
        byte[] bytes = new byte[outputLength];
        int buffer = 0;
        int bitCount = 0;
        int outputIndex = 0;
        index = 0;
        while (index < text.Length)
        {
            buffer = (buffer << 5)
                | Base32Digit(text[index], hexadecimal);
            bitCount = bitCount + 5;
            if (bitCount >= 8)
            {
                bitCount = bitCount - 8;
                bytes[outputIndex] = (byte)((buffer >> bitCount) & 255);
                outputIndex = outputIndex + 1;
                if (bitCount == 0)
                {
                    buffer = 0;
                }
                else
                {
                    buffer = buffer & ((1 << bitCount) - 1);
                }
            }
            index = index + 1;
        }
        if (buffer != 0)
        {
            SetError(
                "Unused base32 bits must be zero.",
                "non-zero trailing base32 bits"
            );
            return null;
        }
        return bytes;
    }

    internal int Base32Digit(char value, bool hexadecimal)
    {
        if (hexadecimal)
        {
            if (value >= '0' && value <= '9')
            {
                return value - '0';
            }
            if (value >= 'A' && value <= 'V')
            {
                return value - 'A' + 10;
            }
            return -1;
        }
        if (value >= 'A' && value <= 'Z')
        {
            return value - 'A';
        }
        if (value >= '2' && value <= '7')
        {
            return value - '2' + 26;
        }
        return -1;
    }

    internal byte[]? DecodeBase45(string text, CddlNode node)
    {
        if (text.Length % 3 == 1)
        {
            SetError(
                "Base45 text cannot end with a one-character group.",
                "invalid base45 length"
            );
            return null;
        }
        int index = 0;
        while (index < text.Length)
        {
            if (Base45Digit(text[index]) < 0)
            {
                SetError(
                    "The text contains a character outside the base45 alphabet.",
                    "invalid base45 character at index " + index
                );
                return null;
            }
            index = index + 1;
        }
        int outputLength = text.Length / 3 * 2
            + text.Length % 3 / 2;
        if (!State.TakeEmbeddedBytes(outputLength, node))
        {
            return null;
        }
        byte[] bytes = new byte[outputLength];
        index = 0;
        int outputIndex = 0;
        while (index + 2 < text.Length)
        {
            int decoded = Base45Digit(text[index])
                + Base45Digit(text[index + 1]) * 45
                + Base45Digit(text[index + 2]) * 2025;
            if (decoded > 65535)
            {
                SetError(
                    "A three-character base45 group exceeds 16 bits.",
                    "out-of-range base45 group at index " + index
                );
                return null;
            }
            bytes[outputIndex] = (byte)(decoded / 256);
            bytes[outputIndex + 1] = (byte)(decoded % 256);
            outputIndex = outputIndex + 2;
            index = index + 3;
        }
        if (index < text.Length)
        {
            int decoded = Base45Digit(text[index])
                + Base45Digit(text[index + 1]) * 45;
            if (decoded > 255)
            {
                SetError(
                    "A two-character base45 group exceeds 8 bits.",
                    "out-of-range base45 group at index " + index
                );
                return null;
            }
            bytes[outputIndex] = (byte)decoded;
        }
        return bytes;
    }

    internal int Base45Digit(char value)
    {
        const string alphabet =
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";
        int index = 0;
        while (index < alphabet.Length)
        {
            if (alphabet[index] == value)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal byte[]? DecodeHex(
        string text,
        string controlName,
        CddlNode node)
    {
        if (text.Length % 2 != 0)
        {
            SetError(
                "A hexadecimal representation requires an even number of digits.",
                "odd hexadecimal length"
            );
            return null;
        }
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            int digit = HexDigit(current);
            bool wrongLowerCase = controlName == CddlControlNames.HexLower
                && current >= 'A' && current <= 'F';
            bool wrongUpperCase = controlName == CddlControlNames.HexUpper
                && current >= 'a' && current <= 'f';
            if (digit < 0 || wrongLowerCase || wrongUpperCase)
            {
                SetError(
                    "The text contains a digit outside the selected hexadecimal alphabet.",
                    "invalid hexadecimal digit at index " + index
                );
                return null;
            }
            index = index + 1;
        }
        int outputLength = text.Length / 2;
        if (!State.TakeEmbeddedBytes(outputLength, node))
        {
            return null;
        }
        byte[] bytes = new byte[outputLength];
        index = 0;
        while (index < outputLength)
        {
            bytes[index] = (byte)(HexDigit(text[index * 2]) * 16
                + HexDigit(text[index * 2 + 1]));
            index = index + 1;
        }
        return bytes;
    }

    internal int HexDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }
        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }
        return -1;
    }

    internal CborValue? DecodeBase10(string text, CddlNode node)
    {
        if (text.Length == 0)
        {
            SetError(
                "A base10 representation cannot be empty.",
                "empty base10 text"
            );
            return null;
        }
        bool negative = text[0] == '-';
        int index = negative ? 1 : 0;
        if (index >= text.Length
            || text[index] < '0'
            || text[index] > '9'
            || text[index] == '0' && text.Length - index > 1
            || negative && text[index] == '0')
        {
            SetError(
                "A base10 representation must be canonical and have no leading zeros.",
                "non-canonical base10 text"
            );
            return null;
        }
        BigInteger integer = BigInteger.Zero;
        while (index < text.Length)
        {
            char current = text[index];
            if (current < '0' || current > '9')
            {
                SetError(
                    "A base10 representation can contain only decimal digits and an optional minus sign.",
                    "invalid base10 character at index " + index
                );
                return null;
            }
            if (!State.TakeStep(node))
            {
                return null;
            }
            integer = integer * 10 + current - '0';
            index = index + 1;
        }
        if (negative)
        {
            integer = -integer;
        }
        CborValue converted = new CborValue();
        converted.Kind = integer.Sign < 0
            ? CborValueKind.NegativeInteger
            : CborValueKind.UnsignedInteger;
        converted.IntegerValue = integer;
        return converted;
    }

    internal void SetError(string message, string actual)
    {
        ErrorMessage = message;
        ErrorActual = actual;
    }
}
