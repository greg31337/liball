using System;
using System.Collections.Generic;

namespace uri;

internal static class WebLinkRelationCore
{
    internal const string InvalidRelationListMessage = "The Web Link relation list is empty or uses whitespace other than required space separators.";
    internal const string InvalidRelationTypeMessage = "The Web Link relation type is neither a registered token nor an absolute URI.";
    internal const string RecoveredRelationListMessage = "Recovery accepted nonstandard whitespace around or within the Web Link relation list.";
    internal const string IgnoredRelationTypeMessage = "Recovery ignored a Web Link relation type that is neither a registered token nor an absolute URI.";

    internal static UriDiagnostic? InterpretStrictRelations(WebLink link)
    {
        List<string> relations = new List<string>();
        List<WebLinkRelationKind> relationKinds =
            new List<WebLinkRelationKind>();
        List<string> reverseRelations = new List<string>();
        List<WebLinkRelationKind> reverseRelationKinds =
            new List<WebLinkRelationKind>();
        int index = 0;
        while (index < link.Parameters.Length)
        {
            WebLinkParameter parameter = link.Parameters[index];
            List<string>? destination = null;
            List<WebLinkRelationKind>? kindDestination = null;
            if (AsciiCase.EqualsIgnoreCase(parameter.Name, "rel"))
            {
                parameter.Kind = WebLinkParameterKind.Relation;
                destination = relations;
                kindDestination = relationKinds;
            }
            else if (AsciiCase.EqualsIgnoreCase(parameter.Name, "rev"))
            {
                parameter.Kind = WebLinkParameterKind.ReverseRelation;
                destination = reverseRelations;
                kindDestination = reverseRelationKinds;
            }

            if (!parameter.Ignored &&
                destination != null &&
                kindDestination != null)
            {
                UriDiagnostic? diagnostic = ParseStrictRelationList(
                    parameter,
                    destination,
                    kindDestination);
                if (diagnostic != null)
                {
                    return diagnostic;
                }
            }

            index = index + 1;
        }

        link.Relations = relations.ToArray();
        link.RelationKinds = relationKinds.ToArray();
        link.ReverseRelations = reverseRelations.ToArray();
        link.ReverseRelationKinds = reverseRelationKinds.ToArray();
        return null;
    }

    internal static UriDiagnostic[] InterpretRecoveryRelations(WebLink link)
    {
        List<string> relations = new List<string>();
        List<WebLinkRelationKind> relationKinds =
            new List<WebLinkRelationKind>();
        List<string> reverseRelations = new List<string>();
        List<WebLinkRelationKind> reverseRelationKinds =
            new List<WebLinkRelationKind>();
        List<UriDiagnostic> diagnostics = new List<UriDiagnostic>();
        int index = 0;
        while (index < link.Parameters.Length)
        {
            WebLinkParameter parameter = link.Parameters[index];
            List<string>? destination = null;
            List<WebLinkRelationKind>? kindDestination = null;
            if (AsciiCase.EqualsIgnoreCase(parameter.Name, "rel"))
            {
                parameter.Kind = WebLinkParameterKind.Relation;
                destination = relations;
                kindDestination = relationKinds;
            }
            else if (AsciiCase.EqualsIgnoreCase(parameter.Name, "rev"))
            {
                parameter.Kind = WebLinkParameterKind.ReverseRelation;
                destination = reverseRelations;
                kindDestination = reverseRelationKinds;
            }

            if (!parameter.Ignored &&
                destination != null &&
                kindDestination != null)
            {
                ParseRecoveryRelationList(
                    parameter,
                    destination,
                    kindDestination,
                    diagnostics);
            }

            index = index + 1;
        }

        link.Relations = relations.ToArray();
        link.RelationKinds = relationKinds.ToArray();
        link.ReverseRelations = reverseRelations.ToArray();
        link.ReverseRelationKinds = reverseRelationKinds.ToArray();
        return diagnostics.ToArray();
    }

    internal static UriDiagnostic? ParseStrictRelationList(
        WebLinkParameter parameter,
        List<string> relations,
        List<WebLinkRelationKind> kinds)
    {
        int valueOffset = WebLinkParameterValueCore.GetCharacterOffset(
            parameter,
            0);
        if (!parameter.HasValue || parameter.Value.Length == 0)
        {
            return CreateRelationError(
                valueOffset,
                0,
                InvalidRelationListMessage);
        }

        string value = parameter.Value;
        if (value[0] == ' ' || value[value.Length - 1] == ' ')
        {
            int badOffset = 0;
            if (value[0] != ' ')
            {
                badOffset = value.Length - 1;
            }

            return CreateRelationError(
                WebLinkParameterValueCore.GetCharacterOffset(
                    parameter,
                    badOffset),
                1,
                InvalidRelationListMessage);
        }

        int whitespaceOffset = FindNonSpaceWhitespace(value);
        if (whitespaceOffset >= 0)
        {
            return CreateRelationError(
                WebLinkParameterValueCore.GetCharacterOffset(
                    parameter,
                    whitespaceOffset),
                1,
                InvalidRelationListMessage);
        }

        int offset = 0;
        while (offset < value.Length)
        {
            int end = offset;
            while (end < value.Length && value[end] != ' ')
            {
                end = end + 1;
            }

            string relation = value.Substring(offset, end - offset);
            UriDiagnostic? diagnostic = AppendRelationType(
                parameter,
                relation,
                offset,
                relations,
                kinds);
            if (diagnostic != null)
            {
                return diagnostic;
            }

            offset = end;
            while (offset < value.Length && value[offset] == ' ')
            {
                offset = offset + 1;
            }
        }

        return null;
    }

    internal static void ParseRecoveryRelationList(
        WebLinkParameter parameter,
        List<string> relations,
        List<WebLinkRelationKind> kinds,
        List<UriDiagnostic> diagnostics)
    {
        int valueOffset = WebLinkParameterValueCore.GetCharacterOffset(
            parameter,
            0);
        string value = parameter.Value;
        if (!parameter.HasValue || value.Length == 0)
        {
            diagnostics.Add(CreateRelationWarning(
                valueOffset,
                0,
                IgnoredRelationTypeMessage));
            return;
        }

        int invalidWhitespace = FindRecoveryWhitespaceIssue(value);
        if (invalidWhitespace >= 0)
        {
            diagnostics.Add(CreateRelationWarning(
                WebLinkParameterValueCore.GetCharacterOffset(
                    parameter,
                    invalidWhitespace),
                1,
                RecoveredRelationListMessage));
        }

        int offset = 0;
        while (offset < value.Length)
        {
            while (offset < value.Length && IsRelationWhitespace(value[offset]))
            {
                offset = offset + 1;
            }

            int end = offset;
            while (end < value.Length && !IsRelationWhitespace(value[end]))
            {
                end = end + 1;
            }

            if (end > offset)
            {
                string relation = value.Substring(offset, end - offset);
                UriDiagnostic? diagnostic = AppendRelationType(
                    parameter,
                    relation,
                    offset,
                    relations,
                    kinds);
                if (diagnostic != null)
                {
                    diagnostics.Add(CreateRelationWarning(
                        diagnostic.Range.Offset,
                        diagnostic.Range.Length,
                        IgnoredRelationTypeMessage));
                }
            }

            offset = end;
        }
    }

    internal static UriDiagnostic? AppendRelationType(
        WebLinkParameter parameter,
        string relation,
        int relationValueOffset,
        List<string> relations,
        List<WebLinkRelationKind> kinds)
    {
        if (IsRegisteredRelation(relation))
        {
            relations.Add(NormalizeRegisteredRelation(relation));
            kinds.Add(WebLinkRelationKind.Registered);
            return null;
        }

        UriParseResult parsed = UriParserCore.ParseAbsoluteUri(
            relation.AsSpan());
        if (parsed.Success)
        {
            relations.Add(relation);
            kinds.Add(WebLinkRelationKind.Extension);
            return null;
        }

        int invalidOffset = 0;
        int invalidLength = relation.Length;
        if (parsed.Diagnostic != null)
        {
            invalidOffset = parsed.Diagnostic.Range.Offset;
            invalidLength = parsed.Diagnostic.Range.Length;
        }

        int offset = WebLinkParameterValueCore.GetCharacterOffset(
            parameter,
            relationValueOffset + invalidOffset);
        int endOffset = WebLinkParameterValueCore.GetCharacterOffset(
            parameter,
            relationValueOffset + invalidOffset + invalidLength);

        return CreateRelationError(
            offset,
            endOffset - offset,
            InvalidRelationTypeMessage);
    }

    internal static bool IsRegisteredRelation(string relation)
    {
        if (relation.Length == 0 || !Ascii.IsAlphabetic(relation[0]))
        {
            return false;
        }

        int index = 1;
        while (index < relation.Length)
        {
            char value = relation[index];
            if (!Ascii.IsAlphaNumeric(value) && value != '.' && value != '-')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string NormalizeRegisteredRelation(string relation)
    {
        TextOutput output = new TextOutput(relation.Length);
        int index = 0;
        while (index < relation.Length)
        {
            output.AppendCharacter(AsciiCase.FoldToLower(relation[index]));
            index = index + 1;
        }

        return output.GetText();
    }

    internal static int FindNonSpaceWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\t')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindRecoveryWhitespaceIssue(string value)
    {
        if (IsRelationWhitespace(value[0]))
        {
            return 0;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\t')
            {
                return index;
            }

            index = index + 1;
        }

        if (IsRelationWhitespace(value[value.Length - 1]))
        {
            return value.Length - 1;
        }

        return -1;
    }

    internal static bool IsRelationWhitespace(char value)
    {
        return value == ' ' || value == '\t';
    }

    internal static UriDiagnostic CreateRelationError(
        int offset,
        int length,
        string message)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.InvalidRelationType,
            UriComponent.LinkRelation,
            offset,
            length,
            message);
    }

    internal static UriDiagnostic CreateRelationWarning(
        int offset,
        int length,
        string message)
    {
        return DiagnosticFactory.CreateWarning(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.InvalidRelationType,
            UriComponent.LinkRelation,
            offset,
            length,
            message);
    }
}
