using System;
using System.Text;

namespace iso20022;

internal static class XmlInputRepairProcessor
{
    internal static XmlInputRepairResult PrepareText(string input, MessageReadOptions options)
    {
        XmlInputRepairResult result = new XmlInputRepairResult();
        result.Text = input;
        if (options.NamespaceMode != NamespaceMode.Lenient)
        {
            return result;
        }

        StringBuilder repaired = new StringBuilder();
        int copyStart = 0;
        int searchStart = 0;
        int declarationCount = 0;
        int declarationStart = FindDeclarationStart(input, searchStart);
        while (declarationStart >= 0)
        {
            int declarationEnd = FindDeclarationEnd(input, declarationStart);
            string declaration = input.Substring(declarationStart, declarationEnd - declarationStart);
            repaired.Append(input, copyStart, declarationStart - copyStart);
            bool validLeadingDeclaration = declarationCount == 0 && IsLeadingPosition(input, declarationStart) && IsValidDeclaration(declaration);
            if (validLeadingDeclaration)
            {
                repaired.Append(declaration);
            }
            else
            {
                result.Repairs.Add(CreateDeclarationRepair(input, declarationStart, declaration));
                result.Changed = true;
            }

            declarationCount++;
            copyStart = declarationEnd;
            searchStart = declarationEnd;
            declarationStart = FindDeclarationStart(input, searchStart);
        }

        if (result.Changed)
        {
            repaired.Append(input, copyStart, input.Length - copyStart);
            result.Text = repaired.ToString();
        }

        return result;
    }

    internal static XmlInputRepairResult PrepareBytes(byte[] input, MessageReadOptions options)
    {
        XmlInputRepairResult result = new XmlInputRepairResult();
        result.Bytes = input;
        if (options.NamespaceMode != NamespaceMode.Lenient)
        {
            return result;
        }

        string text = DecodeBytes(input);
        XmlInputRepairResult textResult = PrepareText(text, options);
        result.Text = textResult.Text;
        result.Changed = textResult.Changed;
        result.Repairs = textResult.Repairs;
        if (result.Changed)
        {
            result.Bytes = Encoding.UTF8.GetBytes(result.Text);
        }

        return result;
    }

    internal static int FindDeclarationStart(string input, int startIndex)
    {
        int index = input.IndexOf('<', startIndex);
        while (index >= 0)
        {
            if (StartsAt(input, index, "<!--"))
            {
                index = FindAfterToken(input, index + 4, "-->");
            }
            else if (StartsAt(input, index, "<![CDATA["))
            {
                index = FindAfterToken(input, index + 9, "]]>");
            }
            else if (StartsAt(input, index, "<?"))
            {
                int boundaryIndex = index + 5;
                if (index + 5 <= input.Length && string.Compare(input, index, "<?xml", 0, 5, StringComparison.OrdinalIgnoreCase) == 0 &&
                    (boundaryIndex >= input.Length || IsXmlWhitespace(input[boundaryIndex]) || input[boundaryIndex] == '?' || input[boundaryIndex] == '>'))
                {
                    return index;
                }

                index = FindAfterToken(input, index + 2, "?>");
            }
            else
            {
                index = FindTagEnd(input, index + 1);
            }

            index = input.IndexOf('<', index);
        }

        return -1;
    }

    internal static bool StartsAt(string input, int startIndex, string token)
    {
        if (startIndex + token.Length > input.Length)
        {
            return false;
        }

        return string.CompareOrdinal(input, startIndex, token, 0, token.Length) == 0;
    }

    internal static int FindAfterToken(string input, int startIndex, string token)
    {
        int index = input.IndexOf(token, startIndex, StringComparison.Ordinal);
        if (index < 0)
        {
            return input.Length;
        }

        return index + token.Length;
    }

    internal static int FindTagEnd(string input, int startIndex)
    {
        char quote = '\0';
        int index = startIndex;
        while (index < input.Length)
        {
            char character = input[index];
            if (quote == '\0' && (character == '\'' || character == '"'))
            {
                quote = character;
            }
            else if (quote != '\0' && character == quote)
            {
                quote = '\0';
            }
            else if (quote == '\0' && character == '>')
            {
                return index + 1;
            }

            index++;
        }

        return input.Length;
    }

    internal static int FindDeclarationEnd(string input, int declarationStart)
    {
        int terminator = input.IndexOf("?>", declarationStart + 5, StringComparison.Ordinal);
        if (terminator >= 0)
        {
            return terminator + 2;
        }

        int closingBracket = input.IndexOf('>', declarationStart + 5);
        if (closingBracket >= 0)
        {
            return closingBracket + 1;
        }

        return input.Length;
    }

    internal static bool IsLeadingPosition(string input, int declarationStart)
    {
        if (declarationStart == 0)
        {
            return true;
        }

        return declarationStart == 1 && input[0] == '\uFEFF';
    }

    internal static bool IsValidDeclaration(string declaration)
    {
        if (!declaration.StartsWith("<?xml", StringComparison.Ordinal) || !declaration.EndsWith("?>", StringComparison.Ordinal))
        {
            return false;
        }

        XmlDeclarationCursor cursor = new XmlDeclarationCursor(declaration.Substring(5, declaration.Length - 7));
        if (!cursor.SkipWhitespace())
        {
            return false;
        }

        string? version = cursor.ReadAttributeValue("version");
        if (version != "1.0")
        {
            return false;
        }

        bool hasWhitespace = cursor.SkipWhitespace();
        if (cursor.IsAtEnd())
        {
            return true;
        }

        if (!hasWhitespace)
        {
            return false;
        }

        if (cursor.StartsWithName("encoding"))
        {
            string? encoding = cursor.ReadAttributeValue("encoding");
            if (encoding == null || !IsEncodingName(encoding))
            {
                return false;
            }

            hasWhitespace = cursor.SkipWhitespace();
            if (cursor.IsAtEnd())
            {
                return true;
            }

            if (!hasWhitespace)
            {
                return false;
            }
        }

        if (cursor.StartsWithName("standalone"))
        {
            string? standalone = cursor.ReadAttributeValue("standalone");
            if (standalone != "yes" && standalone != "no")
            {
                return false;
            }

            cursor.SkipWhitespace();
            return cursor.IsAtEnd();
        }

        return false;
    }

    internal static bool IsEncodingName(string value)
    {
        if (value.Length == 0 || !IsAsciiLetter(value[0]))
        {
            return false;
        }

        int index = 1;
        while (index < value.Length)
        {
            char character = value[index];
            if (!IsAsciiLetter(character) && !IsAsciiDigit(character) && character != '.' && character != '_' && character != '-')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool IsAsciiLetter(char value)
    {
        return (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
    }

    internal static bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal static MessageRepair CreateDeclarationRepair(string input, int declarationStart, string declaration)
    {
        MessageRepair repair = new MessageRepair();
        repair.Code = "xml.malformed_declaration_removed";
        repair.Description = "A malformed, misplaced, or duplicate XML declaration was removed before parsing.";
        repair.OriginalValue = declaration;
        repair.RepairedValue = string.Empty;
        repair.Location = CreateLocation(input, declarationStart);
        return repair;
    }

    internal static SourceLocation CreateLocation(string input, int offset)
    {
        SourceLocation location = new SourceLocation();
        location.HasLineInformation = true;
        location.LineNumber = 1;
        location.ColumnNumber = 1;
        location.HasCharacterOffset = true;
        location.CharacterOffset = offset;
        int index = 0;
        while (index < offset)
        {
            if (input[index] == '\n')
            {
                location.LineNumber++;
                location.ColumnNumber = 1;
            }
            else
            {
                location.ColumnNumber++;
            }

            index++;
        }

        return location;
    }

    internal static string DecodeBytes(byte[] input)
    {
        if (input.Length >= 4 && input[0] == 0xFF && input[1] == 0xFE && input[2] == 0x00 && input[3] == 0x00)
        {
            return Encoding.UTF32.GetString(input, 4, input.Length - 4);
        }

        if (input.Length >= 4 && input[0] == 0x00 && input[1] == 0x00 && input[2] == 0xFE && input[3] == 0xFF)
        {
            return new UTF32Encoding(true, true).GetString(input, 4, input.Length - 4);
        }

        if (input.Length >= 3 && input[0] == 0xEF && input[1] == 0xBB && input[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(input, 3, input.Length - 3);
        }

        if (input.Length >= 2 && input[0] == 0xFF && input[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(input, 2, input.Length - 2);
        }

        if (input.Length >= 2 && input[0] == 0xFE && input[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(input, 2, input.Length - 2);
        }

        if (input.Length >= 4 && input[0] == 0x3C && input[1] == 0x00 && input[2] == 0x3F && input[3] == 0x00)
        {
            return Encoding.Unicode.GetString(input);
        }

        if (input.Length >= 4 && input[0] == 0x00 && input[1] == 0x3C && input[2] == 0x00 && input[3] == 0x3F)
        {
            return Encoding.BigEndianUnicode.GetString(input);
        }

        return Encoding.UTF8.GetString(input);
    }

    internal static bool IsXmlWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }
}
