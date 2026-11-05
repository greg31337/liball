using System.Collections.Generic;

namespace odf;

internal static class OdfStyleNameRules
{
    internal static OdfStyleNameValue ParseStyleName(string? lexicalValue)
    {
        OdfStyleNameValue result = NewResult(lexicalValue);
        if (OdfNameRules.IsNcName(lexicalValue))
        {
            result.Names = [lexicalValue ?? ""];
            result.IsValid = true;
        }
        return result;
    }

    internal static OdfStyleNameValue ParseStyleNameReference(string? lexicalValue)
    {
        OdfStyleNameValue result = NewResult(lexicalValue);
        result.IsReference = true;
        if (lexicalValue == "")
        {
            result.IsValid = true;
        }
        else if (OdfNameRules.IsNcName(lexicalValue))
        {
            result.Names = [lexicalValue ?? ""];
            result.IsValid = true;
        }
        return result;
    }

    internal static OdfStyleNameValue ParseStyleNameList(string? lexicalValue)
    {
        OdfStyleNameValue result = NewResult(lexicalValue);
        result.IsList = true;
        if (lexicalValue == null)
        {
            return result;
        }

        string[] candidates = lexicalValue.Split(
            [' ', '\t', '\r', '\n'],
            global::System.StringSplitOptions.RemoveEmptyEntries);
        List<string> names = new List<string>();
        int index = 0;
        while (index < candidates.Length)
        {
            if (!OdfNameRules.IsNcName(candidates[index]))
            {
                return result;
            }
            names.Add(candidates[index]);
            index = index + 1;
        }
        result.Names = names.ToArray();
        result.IsValid = true;
        return result;
    }

    internal static string FormatStyleName(OdfStyleNameValue? value)
    {
        if (value == null)
        {
            return "";
        }
        if (!value.IsValid)
        {
            return value.LexicalValue;
        }
        if (value.IsList)
        {
            return string.Join(" ", value.Names);
        }
        if (value.Names.Length == 0)
        {
            return "";
        }
        return value.Names[0];
    }

    static OdfStyleNameValue NewResult(string? lexicalValue)
    {
        OdfStyleNameValue result = new OdfStyleNameValue();
        result.LexicalValue = lexicalValue ?? "";
        return result;
    }
}
