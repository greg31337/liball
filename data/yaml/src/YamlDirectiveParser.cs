using System.Collections.Generic;

namespace yaml;

internal class YamlDirectiveParseResult
{
    internal YamlEventKind Kind;
    internal YamlDirective Directive;
    internal bool Valid;
    internal bool Reserved;
    internal bool Warning;
    internal string DiagnosticCode;
    internal string DiagnosticMessage;

    internal YamlDirectiveParseResult()
    {
        Kind = YamlEventKind.ReservedDirective;
        Directive = new YamlDirective();
        Valid = true;
        Reserved = false;
        Warning = false;
        DiagnosticCode = "";
        DiagnosticMessage = "";
    }
}

internal static class YamlDirectiveParser
{
    internal static YamlDirectiveParseResult Parse(string? text, YamlSourceSpan? span)
    {
        string value = text == null ? "" : text;
        YamlSourceSpan sourceSpan = span == null ? YamlSourceSpan.Empty() : span;
        int contentEnd = ContentEnd(value);
        int index = value.Length > 0 && value[0] == '%' ? 1 : 0;
        int nameStart = index;
        while (index < contentEnd && !IsDirectiveWhitespace(value[index]))
        {
            index = index + 1;
        }

        string name = value.Substring(nameStart, index - nameStart);
        while (index < contentEnd && IsDirectiveWhitespace(value[index]))
        {
            index = index + 1;
        }

        string arguments = value.Substring(index, contentEnd - index).TrimEnd();
        if (name == "YAML")
        {
            return ParseYamlDirective(arguments, sourceSpan);
        }

        if (name == "TAG")
        {
            return ParseTagDirective(arguments, sourceSpan);
        }

        YamlDirectiveParseResult reserved = new YamlDirectiveParseResult();
        reserved.Reserved = true;
        reserved.Directive = YamlDirective.ReservedDirective(name, arguments, sourceSpan);
        if (name == "")
        {
            SetError(reserved, "YAMLP006", "Directive name must not be empty.");
            reserved.Reserved = false;
        }

        return reserved;
    }

    internal static YamlDirectiveParseResult ParseYamlDirective(string arguments, YamlSourceSpan span)
    {
        YamlDirectiveParseResult result = new YamlDirectiveParseResult();
        result.Kind = YamlEventKind.YamlVersionDirective;
        List<string> values = Arguments(arguments);
        string versionText = values.Count == 0 ? arguments : values[0];
        YamlVersion version = Version(versionText);
        result.Directive = YamlDirective.YamlVersionDirective(version, versionText, span);

        if (values.Count != 1 || !IsVersionSyntax(versionText))
        {
            SetError(result, "YAMLP006", "YAML directive must contain exactly one major.minor version number.");
        }
        else if (version == YamlVersion.Unknown)
        {
            if (IsCompatibleFutureMinorVersion(versionText))
            {
                result.Directive = YamlDirective.YamlVersionDirective(YamlVersion.Version12, versionText, span);
                result.Warning = true;
                result.DiagnosticCode = "YAMLP008";
                result.DiagnosticMessage = "YAML version '" + versionText + "' is newer than 1.2 and is processed using YAML 1.2 rules.";
            }
            else
            {
                SetError(result, "YAMLP008", "YAML version '" + versionText + "' is not supported.");
            }
        }

        return result;
    }

    internal static YamlDirectiveParseResult ParseTagDirective(string arguments, YamlSourceSpan span)
    {
        YamlDirectiveParseResult result = new YamlDirectiveParseResult();
        result.Kind = YamlEventKind.TagDirective;
        List<string> values = Arguments(arguments);
        string handle = values.Count > 0 ? values[0] : "";
        string prefix = values.Count > 1 ? values[1] : "";
        result.Directive = YamlDirective.TagDirective(handle, prefix, span);

        if (values.Count != 2 || !IsTagHandle(handle) || !IsTagPrefix(prefix))
        {
            SetError(result, "YAMLP009", "TAG directive must contain a valid handle and a non-empty prefix.");
        }

        return result;
    }

    internal static int ContentEnd(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '#' && index > 0 && IsDirectiveWhitespace(text[index - 1]))
            {
                int end = index;
                while (end > 0 && IsDirectiveWhitespace(text[end - 1]))
                {
                    end = end - 1;
                }

                return end;
            }

            index = index + 1;
        }

        return text.Length;
    }

    internal static List<string> Arguments(string text)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < text.Length)
        {
            while (index < text.Length && IsDirectiveWhitespace(text[index]))
            {
                index = index + 1;
            }

            int start = index;
            while (index < text.Length && !IsDirectiveWhitespace(text[index]))
            {
                index = index + 1;
            }

            if (index > start)
            {
                result.Add(text.Substring(start, index - start));
            }
        }

        return result;
    }

    internal static bool IsVersionSyntax(string text)
    {
        if (text == null || text.Length < 3)
        {
            return false;
        }

        int dotIndex = -1;
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == '.')
            {
                if (dotIndex >= 0 || index == 0 || index == text.Length - 1)
                {
                    return false;
                }

                dotIndex = index;
            }
            else if (current < '0' || current > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return dotIndex > 0;
    }

    internal static YamlVersion Version(string text)
    {
        if (text == "1.1")
        {
            return YamlVersion.Version11;
        }

        if (text == "1.2")
        {
            return YamlVersion.Version12;
        }

        return YamlVersion.Unknown;
    }

    internal static bool IsCompatibleFutureMinorVersion(string text)
    {
        if (text == null || text.Length < 3 || text[0] != '1' || text[1] != '.')
        {
            return false;
        }

        int index = 2;
        while (index < text.Length && text[index] == '0')
        {
            index = index + 1;
        }

        int digits = text.Length - index;
        if (digits > 1)
        {
            return true;
        }

        return digits == 1 && text[index] > '2' && text[index] <= '9';
    }

    internal static bool IsTagHandle(string handle)
    {
        if (handle == "!" || handle == "!!")
        {
            return true;
        }

        if (handle == null || handle.Length < 3 || handle[0] != '!' || handle[handle.Length - 1] != '!')
        {
            return false;
        }

        int index = 1;
        while (index < handle.Length - 1)
        {
            char current = handle[index];
            if (!char.IsLetterOrDigit(current) && current != '-')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsTagPrefix(string prefix)
    {
        if (prefix == null || prefix == "")
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            if (IsDirectiveWhitespace(prefix[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsDirectiveWhitespace(char value)
    {
        return value == ' ' || value == '\t';
    }

    internal static void SetError(YamlDirectiveParseResult result, string code, string message)
    {
        result.Valid = false;
        result.DiagnosticCode = code;
        result.DiagnosticMessage = message;
    }
}
