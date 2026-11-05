using System.Globalization;
using System.Text;

namespace iso20022;

internal class GenerationIdentifierNamer
{
    internal const int MaximumIdentifierLength = 128;

    internal string CreateTypeName(string value)
    {
        return CreateIdentifier(value, true);
    }

    internal string CreateMemberName(string value)
    {
        return CreateIdentifier(value, true);
    }

    internal string CreateNamespaceRoot(string value)
    {
        string root = value.Trim();
        if (root.Length == 0)
        {
            root = "Iso20022.Generated";
        }

        string[] segments = root.Split('.');
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < segments.Length)
        {
            string segment = CreateIdentifier(segments[index], true);
            if (result.Length > 0)
            {
                result.Append('.');
            }
            result.Append(segment);
            index++;
        }
        return result.ToString();
    }

    internal string EscapeIdentifier(string value)
    {
        if (IsKeyword(value))
        {
            return "@" + value;
        }
        return value;
    }

    internal string AppendDisambiguator(string value, string disambiguator)
    {
        int availableLength = MaximumIdentifierLength - disambiguator.Length - 1;
        string prefix = value;
        if (availableLength < 1)
        {
            availableLength = 1;
        }
        if (prefix.Length > availableLength)
        {
            prefix = prefix.Substring(0, availableLength);
        }
        return prefix + "_" + disambiguator;
    }

    internal string CreateIdentifier(string value, bool capitalizeWords)
    {
        string normalized = value.Normalize(NormalizationForm.FormC);
        StringBuilder result = new StringBuilder();
        bool wordStart = true;
        int index = 0;
        while (index < normalized.Length)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(normalized, index);
            int characterLength = 1;
            if (char.IsSurrogatePair(normalized, index))
            {
                characterLength = 2;
            }

            string character = normalized.Substring(index, characterLength);
            if (IsLetterCategory(category))
            {
                if (capitalizeWords && wordStart)
                {
                    character = character.ToUpperInvariant();
                }
                result.Append(character);
                wordStart = false;
            }
            else if (category == UnicodeCategory.DecimalDigitNumber)
            {
                if (result.Length == 0)
                {
                    result.Append('_');
                }
                result.Append(character);
                wordStart = false;
            }
            else if (IsCombiningCategory(category) && result.Length > 0)
            {
                result.Append(character);
            }
            else
            {
                wordStart = true;
            }
            index += characterLength;
        }

        if (result.Length == 0)
        {
            result.Append('_');
        }

        string identifier = EscapeIdentifier(result.ToString());
        if (identifier.Length <= MaximumIdentifierLength)
        {
            return identifier;
        }

        string hash = GenerationStableHash.CreateHex(identifier).Substring(0, 24);
        return AppendDisambiguator(identifier, hash);
    }

    internal bool IsLetterCategory(UnicodeCategory category)
    {
        switch (category)
        {
            case UnicodeCategory.UppercaseLetter:
            case UnicodeCategory.LowercaseLetter:
            case UnicodeCategory.TitlecaseLetter:
            case UnicodeCategory.ModifierLetter:
            case UnicodeCategory.OtherLetter:
                return true;
            default:
                return false;
        }
    }

    internal bool IsCombiningCategory(UnicodeCategory category)
    {
        switch (category)
        {
            case UnicodeCategory.NonSpacingMark:
            case UnicodeCategory.SpacingCombiningMark:
            case UnicodeCategory.Format:
                return true;
            default:
                return false;
        }
    }

    internal bool IsKeyword(string value)
    {
        switch (value)
        {
            case "abstract":
            case "add":
            case "alias":
            case "and":
            case "as":
            case "ascending":
            case "async":
            case "await":
            case "base":
            case "bool":
            case "break":
            case "by":
            case "byte":
            case "case":
            case "catch":
            case "char":
            case "checked":
            case "class":
            case "const":
            case "continue":
            case "decimal":
            case "default":
            case "delegate":
            case "descending":
            case "do":
            case "double":
            case "dynamic":
            case "else":
            case "enum":
            case "equals":
            case "event":
            case "explicit":
            case "extern":
            case "false":
            case "file":
            case "finally":
            case "fixed":
            case "float":
            case "for":
            case "foreach":
            case "from":
            case "get":
            case "global":
            case "goto":
            case "group":
            case "if":
            case "implicit":
            case "in":
            case "init":
            case "int":
            case "interface":
            case "internal":
            case "into":
            case "is":
            case "join":
            case "let":
            case "lock":
            case "long":
            case "managed":
            case "namespace":
            case "nameof":
            case "new":
            case "not":
            case "notnull":
            case "null":
            case "object":
            case "on":
            case "operator":
            case "or":
            case "orderby":
            case "out":
            case "override":
            case "params":
            case "partial":
            case "private":
            case "protected":
            case "public":
            case "readonly":
            case "record":
            case "ref":
            case "remove":
            case "required":
            case "return":
            case "sbyte":
            case "scoped":
            case "sealed":
            case "select":
            case "set":
            case "short":
            case "sizeof":
            case "stackalloc":
            case "static":
            case "string":
            case "struct":
            case "switch":
            case "this":
            case "throw":
            case "true":
            case "try":
            case "typeof":
            case "uint":
            case "ulong":
            case "unchecked":
            case "unmanaged":
            case "unsafe":
            case "ushort":
            case "using":
            case "value":
            case "var":
            case "virtual":
            case "void":
            case "volatile":
            case "when":
            case "where":
            case "while":
            case "with":
            case "yield":
                return true;
            default:
                return false;
        }
    }
}
