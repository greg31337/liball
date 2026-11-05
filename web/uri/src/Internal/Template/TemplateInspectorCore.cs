using System;
using System.Collections.Generic;

namespace uri;

internal static class TemplateInspectorCore
{
    internal const string MissingTemplateMessage = "A URI Template is required.";
    internal const string InvalidTemplateModelMessage = "The URI Template model is inconsistent.";

    internal static string[] ListVariables(UriTemplate? template)
    {
        ValidateTemplate(template);
        if (template == null)
        {
            throw new ArgumentException(MissingTemplateMessage);
        }

        List<string> names = new List<string>();
        int partIndex = 0;
        while (partIndex < template.Parts.Length)
        {
            UriTemplatePart part = template.Parts[partIndex];
            int variableIndex = 0;
            while (variableIndex < part.Variables.Length)
            {
                string name = part.Variables[variableIndex].Name;
                if (!ContainsName(names, name))
                {
                    names.Add(name);
                }

                variableIndex = variableIndex + 1;
            }

            partIndex = partIndex + 1;
        }

        return names.ToArray();
    }

    internal static UriTemplateVariable[] ListOccurrences(UriTemplate? template)
    {
        ValidateTemplate(template);
        if (template == null)
        {
            throw new ArgumentException(MissingTemplateMessage);
        }

        List<UriTemplateVariable> occurrences =
            new List<UriTemplateVariable>();
        int partIndex = 0;
        while (partIndex < template.Parts.Length)
        {
            UriTemplatePart part = template.Parts[partIndex];
            int variableIndex = 0;
            while (variableIndex < part.Variables.Length)
            {
                occurrences.Add(part.Variables[variableIndex]);
                variableIndex = variableIndex + 1;
            }

            partIndex = partIndex + 1;
        }

        return occurrences.ToArray();
    }

    internal static UriTemplateLevel DetermineLevel(UriTemplate? template)
    {
        ValidateTemplate(template);
        if (template == null)
        {
            throw new ArgumentException(MissingTemplateMessage);
        }

        UriTemplateLevel level = UriTemplateLevel.Level1;
        int partIndex = 0;
        while (partIndex < template.Parts.Length)
        {
            UriTemplatePart part = template.Parts[partIndex];
            if (part.Kind == UriTemplatePartKind.Expression)
            {
                int variableIndex = 0;
                while (variableIndex < part.Variables.Length)
                {
                    UriTemplateVariable variable = part.Variables[variableIndex];
                    if (variable.HasPrefix || variable.Explode)
                    {
                        return UriTemplateLevel.Level4;
                    }

                    variableIndex = variableIndex + 1;
                }

                if (part.Variables.Length > 1 ||
                    IsLevel3Operator(part.Operator))
                {
                    level = UriTemplateLevel.Level3;
                }
                else if (level == UriTemplateLevel.Level1 &&
                    IsLevel2Operator(part.Operator))
                {
                    level = UriTemplateLevel.Level2;
                }
            }

            partIndex = partIndex + 1;
        }

        return level;
    }

    internal static bool ContainsName(List<string> names, string name)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (names[index] == name)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsLevel2Operator(UriTemplateOperator value)
    {
        return value == UriTemplateOperator.Reserved ||
            value == UriTemplateOperator.Fragment;
    }

    internal static bool IsLevel3Operator(UriTemplateOperator value)
    {
        return value == UriTemplateOperator.Label ||
            value == UriTemplateOperator.PathSegment ||
            value == UriTemplateOperator.PathParameter ||
            value == UriTemplateOperator.Query ||
            value == UriTemplateOperator.QueryContinuation;
    }

    internal static void ValidateTemplate(UriTemplate? template)
    {
        if (template == null)
        {
            throw new ArgumentException(MissingTemplateMessage);
        }

        if (template.Parts == null)
        {
            throw new ArgumentException(InvalidTemplateModelMessage);
        }

        int partIndex = 0;
        while (partIndex < template.Parts.Length)
        {
            UriTemplatePart? part = template.Parts[partIndex];
            if (part == null || part.Variables == null)
            {
                throw new ArgumentException(InvalidTemplateModelMessage);
            }

            int variableIndex = 0;
            while (variableIndex < part.Variables.Length)
            {
                UriTemplateVariable? variable = part.Variables[variableIndex];
                if (variable == null || variable.Name == null)
                {
                    throw new ArgumentException(InvalidTemplateModelMessage);
                }

                variableIndex = variableIndex + 1;
            }

            partIndex = partIndex + 1;
        }
    }
}
