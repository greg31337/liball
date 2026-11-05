using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace cbor;

internal class CddlTextControlGenerator
{
    internal CddlControlGenerator Owner;
    internal CddlValueGeneratorCore Core;
    internal CddlGenerationState State;

    internal CddlTextControlGenerator(CddlControlGenerator owner)
    {
        Owner = owner;
        Core = owner.Core;
        State = owner.State;
    }

    internal CborValue? GenerateTextControlValue(CddlNode node)
    {
        string name = node.ControlName ?? "";
        if (name == CddlControlNames.Base10)
        {
            return GenerateBase10Value(node);
        }
        if (name == CddlControlNames.Printf)
        {
            return GeneratePrintfValue(node);
        }
        if (name == CddlControlNames.Json)
        {
            return GenerateJsonValue(node);
        }
        if (name == CddlControlNames.Join)
        {
            return GenerateJoinValue(node);
        }
        return GenerateByteEncodingValue(node);
    }

    internal CborValue? GenerateByteEncodingValue(CddlNode node)
    {
        CborValue? controller = Core.GenerateNodeValue(node.Children[1]);
        if (controller == null
            || controller.Kind != CborValueKind.ByteString)
        {
            State.FailGeneration(
                "CDDL_GENERATION_CONTROL_NOT_CONSTRUCTIVE",
                "The text conversion controller did not generate bytes.",
                node
            );
            return null;
        }
        string? encoded = EncodeConversionBytes(
            node.ControlName ?? "",
            controller.ByteStringValue ?? []
        );
        if (encoded == null)
        {
            State.FailGeneration(
                "CDDL_GENERATION_CONTROL_NOT_CONSTRUCTIVE",
                "The text conversion has no constructive encoder.",
                node
            );
            return null;
        }
        return AcceptTextValue(node, encoded);
    }

    internal string? EncodeConversionBytes(
        string name,
        byte[] bytes)
    {
        if (name == CddlControlNames.Base64Url
            || name == CddlControlNames.Base64UrlSloppy)
        {
            return System.Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
        if (name == CddlControlNames.Base64Classic
            || name == CddlControlNames.Base64ClassicSloppy)
        {
            return System.Convert.ToBase64String(bytes);
        }
        if (name == CddlControlNames.Base32)
        {
            return EncodeBase32Bytes(bytes, false);
        }
        if (name == CddlControlNames.Base32Hex)
        {
            return EncodeBase32Bytes(bytes, true);
        }
        if (name == CddlControlNames.Base45)
        {
            return EncodeBase45Bytes(bytes);
        }
        if (name == CddlControlNames.HexUpper)
        {
            return EncodeHexBytes(bytes, true);
        }
        if (name == CddlControlNames.Hex
            || name == CddlControlNames.HexLower)
        {
            return EncodeHexBytes(bytes, false);
        }
        return null;
    }

    internal string EncodeBase32Bytes(byte[] bytes, bool hexadecimal)
    {
        string alphabet = hexadecimal
            ? "0123456789ABCDEFGHIJKLMNOPQRSTUV"
            : "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        StringBuilder result = new StringBuilder();
        int buffer = 0;
        int bits = 0;
        int index = 0;
        while (index < bytes.Length)
        {
            buffer = (buffer << 8) | bytes[index];
            bits = bits + 8;
            while (bits >= 5)
            {
                bits = bits - 5;
                result.Append(alphabet[(buffer >> bits) & 31]);
            }
            index = index + 1;
        }
        if (bits > 0)
        {
            result.Append(alphabet[(buffer << (5 - bits)) & 31]);
        }
        return result.ToString();
    }

    internal string EncodeBase45Bytes(byte[] bytes)
    {
        const string alphabet =
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index + 1 < bytes.Length)
        {
            int value = bytes[index] * 256 + bytes[index + 1];
            result.Append(alphabet[value % 45]);
            result.Append(alphabet[(value / 45) % 45]);
            result.Append(alphabet[value / 2025]);
            index = index + 2;
        }
        if (index < bytes.Length)
        {
            int value = bytes[index];
            result.Append(alphabet[value % 45]);
            result.Append(alphabet[value / 45]);
        }
        return result.ToString();
    }

    internal string EncodeHexBytes(byte[] bytes, bool upper)
    {
        string alphabet = upper
            ? "0123456789ABCDEF"
            : "0123456789abcdef";
        char[] result = new char[bytes.Length * 2];
        int index = 0;
        while (index < bytes.Length)
        {
            result[index * 2] = alphabet[bytes[index] >> 4];
            result[index * 2 + 1] = alphabet[bytes[index] & 15];
            index = index + 1;
        }
        return new string(result);
    }

    internal CborValue? GenerateBase10Value(CddlNode node)
    {
        CborValue? controller = Core.GenerateNodeValue(node.Children[1]);
        if (controller == null || !Owner.IsIntegerControlValue(controller))
        {
            State.FailGeneration(
                "CDDL_GENERATION_CONTROL_NOT_CONSTRUCTIVE",
                "The .base10 controller did not generate an integer.",
                node
            );
            return null;
        }
        return AcceptTextValue(
            node,
            controller.IntegerValue.ToString(CultureInfo.InvariantCulture)
        );
    }

    internal CborValue? GenerateJsonValue(CddlNode node)
    {
        CborValue? controller = Core.GenerateNodeValue(node.Children[1]);
        if (controller == null)
        {
            return null;
        }
        if (!CanConvertJsonValue(controller, 0))
        {
            State.FailGeneration(
                "CDDL_GENERATION_CONTROL_NOT_CONSTRUCTIVE",
                "The generated controller is outside the JSON data model.",
                node
            );
            return null;
        }
        CborJsonOptions options = new CborJsonOptions();
        options.Policy = CborJsonPolicy.Strict;
        options.MaxNestingDepth = State.Options.MaxNestingDepth;
        string json = new CborJsonConverter().ConvertToJson(
            controller,
            options
        );
        return AcceptTextValue(node, json);
    }

    internal bool CanConvertJsonValue(CborValue value, int depth)
    {
        if (depth > State.Options.MaxNestingDepth)
        {
            return false;
        }
        if (value.Kind == CborValueKind.UnsignedInteger
            || value.Kind == CborValueKind.NegativeInteger
            || value.Kind == CborValueKind.TextString)
        {
            return true;
        }
        if (value.Kind == CborValueKind.SimpleValue)
        {
            return value.SimpleValue == CborSimpleValues.False
                || value.SimpleValue == CborSimpleValues.True
                || value.SimpleValue == CborSimpleValues.Null;
        }
        if (value.Kind == CborValueKind.Float)
        {
            return value.FloatValue != null
                && !double.IsNaN(value.FloatValue.Value)
                && !double.IsInfinity(value.FloatValue.Value);
        }
        if (value.Kind == CborValueKind.Array && value.ArrayItems != null)
        {
            int index = 0;
            while (index < value.ArrayItems.Count)
            {
                if (!CanConvertJsonValue(
                    value.ArrayItems[index],
                    depth + 1
                ))
                {
                    return false;
                }
                index = index + 1;
            }
            return true;
        }
        if (value.Kind == CborValueKind.Map && value.MapEntries != null)
        {
            int index = 0;
            while (index < value.MapEntries.Count)
            {
                CborMapEntry entry = value.MapEntries[index];
                if (entry.Key == null || entry.Value == null
                    || entry.Key.Kind != CborValueKind.TextString
                    || !CanConvertJsonValue(entry.Value, depth + 1))
                {
                    return false;
                }
                index = index + 1;
            }
            return true;
        }
        return false;
    }

    internal CborValue? GenerateJoinValue(CddlNode node)
    {
        CborValue? target = Core.GenerateNodeValue(node.Children[0]);
        CborValue? controller = Core.GenerateNodeValue(node.Children[1]);
        if (target == null || controller == null
            || controller.Kind != CborValueKind.Array
            || controller.ArrayItems == null)
        {
            State.FailGeneration(
                "CDDL_GENERATION_CONTROL_NOT_CONSTRUCTIVE",
                "The .join controller did not generate an array.",
                node
            );
            return null;
        }
        List<byte> joined = new List<byte>();
        int index = 0;
        while (index < controller.ArrayItems.Count)
        {
            byte[]? bytes = Owner.FindStringBytes(
                controller.ArrayItems[index]
            );
            if (bytes == null
                || bytes.Length > State.Options.MaxCollectionItems
                    - joined.Count)
            {
                State.FailGeneration(
                    "CDDL_GENERATION_CONTROL_NOT_CONSTRUCTIVE",
                    "The .join elements are not bounded strings.",
                    node
                );
                return null;
            }
            joined.AddRange(bytes);
            index = index + 1;
        }
        CborValue candidate = new CborValue();
        candidate.LengthKind = CborLengthKind.Definite;
        if (target.Kind == CborValueKind.ByteString)
        {
            candidate.Kind = CborValueKind.ByteString;
            candidate.ByteStringValue = joined.ToArray();
        }
        else if (target.Kind == CborValueKind.TextString)
        {
            CborUtf8Result decoded = new CborUtf8().ProcessChunk(
                joined.ToArray(),
                true
            );
            if (decoded.Status != CborReadStatus.Done)
            {
                State.FailGeneration(
                    "CDDL_GENERATION_CONTROL_NOT_CONSTRUCTIVE",
                    "The joined text is not valid UTF-8.",
                    node
                );
                return null;
            }
            candidate.Kind = CborValueKind.TextString;
            candidate.TextStringValue = decoded.Text;
        }
        else
        {
            State.FailGeneration(
                "CDDL_GENERATION_CONTROL_NOT_CONSTRUCTIVE",
                "The .join target is not a string.",
                node
            );
            return null;
        }
        return Owner.AcceptControlCandidate(node, candidate);
    }

    internal CborValue? GeneratePrintfValue(CddlNode node)
    {
        CborValue? controller = Core.GenerateNodeValue(node.Children[1]);
        if (controller == null
            || controller.Kind != CborValueKind.Array
            || controller.ArrayItems == null
            || controller.ArrayItems.Count == 0
            || controller.ArrayItems[0].Kind != CborValueKind.TextString)
        {
            State.FailGeneration(
                "CDDL_GENERATION_CONTROL_NOT_CONSTRUCTIVE",
                "The .printf controller did not generate a fixed argument array.",
                node
            );
            return null;
        }
        string format = controller.ArrayItems[0].TextStringValue ?? "";
        CddlPrintfParser parser = new CddlPrintfParser(format);
        if (!parser.ParseFormat()
            || parser.ArgumentCount != controller.ArrayItems.Count)
        {
            State.FailGeneration(
                "CDDL_GENERATION_CONTROL_NOT_CONSTRUCTIVE",
                parser.Error ?? "The .printf arguments do not match the format.",
                node
            );
            return null;
        }
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < parser.Parts.Count)
        {
            CddlPrintfPart part = parser.Parts[index];
            if (part.Specifier == '\0')
            {
                result.Append(part.Literal);
            }
            else
            {
                string? formatted = FormatPrintfValue(
                    part,
                    controller.ArrayItems[part.ArgumentIndex]
                );
                if (formatted == null)
                {
                    State.FailGeneration(
                        "CDDL_GENERATION_CONTROL_NOT_CONSTRUCTIVE",
                        "A generated .printf argument is incompatible with its directive.",
                        node
                    );
                    return null;
                }
                result.Append(formatted);
            }
            index = index + 1;
        }
        return AcceptTextValue(node, result.ToString());
    }

    internal string? FormatPrintfValue(
        CddlPrintfPart part,
        CborValue value)
    {
        if (part.Specifier == 's')
        {
            if (value.Kind != CborValueKind.TextString)
            {
                return null;
            }
            string text = value.TextStringValue ?? "";
            if (part.Precision >= 0)
            {
                text = TruncatePrintfText(text, part.Precision);
            }
            return ApplyPrintfTextWidth(part, text);
        }
        if (part.Specifier == 'c')
        {
            if (!Owner.IsIntegerControlValue(value)
                || value.IntegerValue < BigInteger.Zero
                || value.IntegerValue > new BigInteger(1114111))
            {
                return null;
            }
            int scalar = (int)value.IntegerValue;
            if (scalar >= 55296 && scalar <= 57343)
            {
                return null;
            }
            return ApplyPrintfTextWidth(
                part,
                char.ConvertFromUtf32(scalar)
            );
        }
        return FormatPrintfInteger(part, value);
    }

    internal string? FormatPrintfInteger(
        CddlPrintfPart part,
        CborValue value)
    {
        if (!Owner.IsIntegerControlValue(value))
        {
            return null;
        }
        bool signed = part.Specifier == 'd' || part.Specifier == 'i';
        if (!signed && value.IntegerValue.Sign < 0)
        {
            return null;
        }
        BigInteger magnitude = value.IntegerValue;
        string sign = "";
        if (signed && magnitude.Sign < 0)
        {
            sign = "-";
            magnitude = BigInteger.Abs(magnitude);
        }
        else if (signed && part.ShowSign)
        {
            sign = "+";
        }
        else if (signed && part.SpaceSign)
        {
            sign = " ";
        }
        int numberBase = FindPrintfIntegerBase(part.Specifier);
        bool upper = part.Specifier == 'X' || part.Specifier == 'B';
        string digits = CreatePrintfDigits(magnitude, numberBase, upper);
        if (part.Precision == 0 && magnitude.IsZero)
        {
            digits = "";
        }
        if (part.Precision > digits.Length)
        {
            digits = new string('0', part.Precision - digits.Length)
                + digits;
        }
        string prefix = "";
        if (part.Alternate && part.Specifier == 'o'
            && (digits.Length == 0 || digits[0] != '0'))
        {
            prefix = "0";
        }
        else if (part.Alternate && !magnitude.IsZero
            && (part.Specifier == 'x' || part.Specifier == 'X'))
        {
            prefix = part.Specifier == 'X' ? "0X" : "0x";
        }
        else if (part.Alternate && !magnitude.IsZero
            && (part.Specifier == 'b' || part.Specifier == 'B'))
        {
            prefix = part.Specifier == 'B' ? "0B" : "0b";
        }
        int contentLength = sign.Length + prefix.Length + digits.Length;
        if (part.Width > contentLength
            && part.ZeroPadded
            && !part.LeftAligned
            && part.Precision < 0)
        {
            digits = new string('0', part.Width - contentLength) + digits;
        }
        return ApplyPrintfAsciiWidth(part, sign + prefix + digits);
    }

    internal int FindPrintfIntegerBase(char specifier)
    {
        if (specifier == 'o')
        {
            return 8;
        }
        if (specifier == 'x' || specifier == 'X')
        {
            return 16;
        }
        if (specifier == 'b' || specifier == 'B')
        {
            return 2;
        }
        return 10;
    }

    internal string CreatePrintfDigits(
        BigInteger value,
        int numberBase,
        bool upper)
    {
        if (numberBase == 10)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        if (value.IsZero)
        {
            return "0";
        }
        string alphabet = upper
            ? "0123456789ABCDEF"
            : "0123456789abcdef";
        StringBuilder reversed = new StringBuilder();
        BigInteger remaining = value;
        while (remaining > BigInteger.Zero)
        {
            BigInteger remainder = remaining % numberBase;
            reversed.Append(alphabet[(int)remainder]);
            remaining = remaining / numberBase;
        }
        StringBuilder result = new StringBuilder(reversed.Length);
        int index = reversed.Length - 1;
        while (index >= 0)
        {
            result.Append(reversed[index]);
            index = index - 1;
        }
        return result.ToString();
    }

    internal string ApplyPrintfAsciiWidth(
        CddlPrintfPart part,
        string value)
    {
        if (part.Width <= value.Length)
        {
            return value;
        }
        string padding = new string(' ', part.Width - value.Length);
        return part.LeftAligned ? value + padding : padding + value;
    }

    internal string ApplyPrintfTextWidth(
        CddlPrintfPart part,
        string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (part.Width <= byteCount)
        {
            return value;
        }
        string padding = new string(' ', part.Width - byteCount);
        return part.LeftAligned ? value + padding : padding + value;
    }

    internal string TruncatePrintfText(string value, int maximumBytes)
    {
        int offset = 0;
        int bytes = 0;
        while (offset < value.Length)
        {
            int characterLength = 1;
            int characterBytes;
            char current = value[offset];
            if (char.IsHighSurrogate(current)
                && offset + 1 < value.Length
                && char.IsLowSurrogate(value[offset + 1]))
            {
                characterLength = 2;
                characterBytes = 4;
            }
            else if (current <= 0x7f)
            {
                characterBytes = 1;
            }
            else if (current <= 0x7ff)
            {
                characterBytes = 2;
            }
            else
            {
                characterBytes = 3;
            }
            if (bytes > maximumBytes - characterBytes)
            {
                break;
            }
            bytes = bytes + characterBytes;
            offset = offset + characterLength;
        }
        return value.Substring(0, offset);
    }

    internal CborValue? AcceptTextValue(CddlNode node, string text)
    {
        if (Encoding.UTF8.GetByteCount(text)
            > State.Options.MaxCollectionItems)
        {
            State.FailGeneration(
                "CDDL_GENERATION_COLLECTION_LIMIT",
                "The generated controlled text exceeds the size limit.",
                node
            );
            return null;
        }
        CborValue candidate = new CborValue();
        candidate.Kind = CborValueKind.TextString;
        candidate.TextStringValue = text;
        candidate.LengthKind = CborLengthKind.Definite;
        return Owner.AcceptControlCandidate(node, candidate);
    }
}
