using System;
using System.Collections.Generic;

namespace uri;

internal static class WebLinkAttributeCore
{
    internal const string MissingAttributeValueMessage = "The Web Link standard attribute requires an equals sign and value.";
    internal const string InvalidAnchorMessage = "The Web Link anchor is not a valid URI-reference.";
    internal const string InvalidMediaTypeMessage = "The Web Link type attribute is not a valid type-name and subtype-name pair.";
    internal const string RecoveredMissingAttributeValueMessage = "Recovery retained a Web Link standard attribute without its required value.";
    internal const string RecoveredInvalidAnchorMessage = "Recovery retained but did not apply a Web Link anchor that is not a valid URI-reference.";
    internal const string RecoveredInvalidMediaTypeMessage = "Recovery retained but did not apply an invalid Web Link type attribute.";

    internal static UriDiagnostic? InterpretStrictAttributes(WebLink link)
    {
        UriDiagnostic[] diagnostics = InterpretAttributes(link, false);
        if (diagnostics.Length == 0)
        {
            return null;
        }

        return diagnostics[0];
    }

    internal static UriDiagnostic[] InterpretRecoveryAttributes(WebLink link)
    {
        return InterpretAttributes(link, true);
    }

    internal static UriDiagnostic[] InterpretAttributes(
        WebLink link,
        bool recovery)
    {
        List<string> hrefLanguages = new List<string>();
        List<UriDiagnostic> diagnostics = new List<UriDiagnostic>();
        int index = 0;
        while (index < link.Parameters.Length)
        {
            WebLinkParameter parameter = link.Parameters[index];
            if (AsciiCase.EqualsIgnoreCase(parameter.Name, "anchor"))
            {
                parameter.Kind = WebLinkParameterKind.Anchor;
                if (!parameter.Ignored)
                {
                    ApplyAnchor(link, parameter, recovery, diagnostics);
                }
            }
            else if (AsciiCase.EqualsIgnoreCase(parameter.Name, "hreflang"))
            {
                parameter.Kind = WebLinkParameterKind.HrefLang;
                if (!parameter.Ignored &&
                    CheckRequiredValue(parameter, recovery, diagnostics))
                {
                    UriDiagnostic? languageDiagnostic =
                        WebLinkLanguageTagCore.ValidateParameterLanguage(
                            parameter,
                            recovery);
                    if (languageDiagnostic != null)
                    {
                        diagnostics.Add(languageDiagnostic);
                    }
                    else
                    {
                        hrefLanguages.Add(parameter.Value);
                    }
                }
            }
            else if (AsciiCase.EqualsIgnoreCase(parameter.Name, "media"))
            {
                parameter.Kind = WebLinkParameterKind.Media;
                if (!parameter.Ignored &&
                    CheckRequiredValue(parameter, recovery, diagnostics))
                {
                    link.HasMedia = true;
                    link.Media = parameter.Value;
                }
            }
            else if (AsciiCase.EqualsIgnoreCase(parameter.Name, "title"))
            {
                parameter.Kind = WebLinkParameterKind.Title;
                if (!parameter.Ignored &&
                    CheckRequiredValue(parameter, recovery, diagnostics))
                {
                    link.HasTitle = true;
                    link.Title = parameter.Value;
                }
            }
            else if (AsciiCase.EqualsIgnoreCase(parameter.Name, "title*"))
            {
                parameter.Kind = WebLinkParameterKind.TitleExtended;
                if (!parameter.Ignored)
                {
                    CheckRequiredValue(parameter, recovery, diagnostics);
                }
            }
            else if (AsciiCase.EqualsIgnoreCase(parameter.Name, "type"))
            {
                parameter.Kind = WebLinkParameterKind.Type;
                if (!parameter.Ignored)
                {
                    ApplyMediaType(link, parameter, recovery, diagnostics);
                }
            }

            index = index + 1;
        }

        link.HrefLanguages = hrefLanguages.ToArray();
        return diagnostics.ToArray();
    }

    internal static void ApplyAnchor(
        WebLink link,
        WebLinkParameter parameter,
        bool recovery,
        List<UriDiagnostic> diagnostics)
    {
        if (!CheckRequiredValue(parameter, recovery, diagnostics))
        {
            return;
        }

        UriParseResult parsed = UriParserCore.ParseReference(
            parameter.Value.AsSpan());
        if (!parsed.Success)
        {
            int valueOffset = 0;
            int valueLength = parameter.Value.Length;
            if (parsed.Diagnostic != null)
            {
                valueOffset = parsed.Diagnostic.Range.Offset;
                valueLength = parsed.Diagnostic.Range.Length;
            }

            AddAttributeDiagnostic(
                diagnostics,
                recovery,
                UriComponent.LinkContext,
                WebLinkParameterValueCore.GetCharacterOffset(
                    parameter,
                    valueOffset),
                GetSourceLength(parameter, valueOffset, valueLength),
                InvalidAnchorMessage,
                RecoveredInvalidAnchorMessage);
            return;
        }

        link.HasAnchor = true;
        link.Anchor = parameter.Value;
    }

    internal static void ApplyMediaType(
        WebLink link,
        WebLinkParameter parameter,
        bool recovery,
        List<UriDiagnostic> diagnostics)
    {
        if (!CheckRequiredValue(parameter, recovery, diagnostics))
        {
            return;
        }

        int invalidOffset = FindInvalidMediaTypeOffset(parameter.Value);
        if (invalidOffset >= 0)
        {
            int length = 1;
            if (invalidOffset == parameter.Value.Length)
            {
                length = 0;
            }

            AddAttributeDiagnostic(
                diagnostics,
                recovery,
                UriComponent.LinkParameter,
                WebLinkParameterValueCore.GetCharacterOffset(
                    parameter,
                    invalidOffset),
                length,
                InvalidMediaTypeMessage,
                RecoveredInvalidMediaTypeMessage);
            return;
        }

        link.HasType = true;
        link.Type = parameter.Value;
    }

    internal static bool CheckRequiredValue(
        WebLinkParameter parameter,
        bool recovery,
        List<UriDiagnostic> diagnostics)
    {
        if (parameter.HasValue)
        {
            return true;
        }

        AddAttributeDiagnostic(
            diagnostics,
            recovery,
            UriComponent.LinkParameter,
            parameter.Range.Offset + parameter.Range.Length,
            0,
            MissingAttributeValueMessage,
            RecoveredMissingAttributeValueMessage);
        return false;
    }

    internal static int FindInvalidMediaTypeOffset(string value)
    {
        if (value.Length == 0)
        {
            return 0;
        }

        int slashOffset = value.IndexOf('/');
        if (slashOffset < 0)
        {
            return value.Length;
        }

        if (slashOffset == 0)
        {
            return 0;
        }

        if (slashOffset == value.Length - 1)
        {
            return value.Length;
        }

        if (slashOffset > 127 || value.Length - slashOffset - 1 > 127)
        {
            return value.Length;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (index == slashOffset)
            {
                index = index + 1;
                continue;
            }

            if (!IsMediaTypeNameCharacter(value[index]))
            {
                return index;
            }

            if ((index == 0 || index == slashOffset + 1) &&
                !Ascii.IsAlphaNumeric(value[index]))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsMediaTypeNameCharacter(char value)
    {
        return Ascii.IsAlphaNumeric(value) ||
            value == '!' ||
            value == '#' ||
            value == '$' ||
            value == '&' ||
            value == '-' ||
            value == '^' ||
            value == '_' ||
            value == '.' ||
            value == '+';
    }

    internal static int GetSourceLength(
        WebLinkParameter parameter,
        int valueOffset,
        int valueLength)
    {
        int startOffset = WebLinkParameterValueCore.GetCharacterOffset(
            parameter,
            valueOffset);
        int endOffset = WebLinkParameterValueCore.GetCharacterOffset(
            parameter,
            valueOffset + valueLength);
        return endOffset - startOffset;
    }

    internal static void AddAttributeDiagnostic(
        List<UriDiagnostic> diagnostics,
        bool recovery,
        UriComponent component,
        int offset,
        int length,
        string errorMessage,
        string recoveryMessage)
    {
        if (recovery)
        {
            diagnostics.Add(DiagnosticFactory.CreateWarning(
                DiagnosticDomain.WebLink,
                UriDiagnosticCode.InvalidLinkParameter,
                component,
                offset,
                length,
                recoveryMessage));
            return;
        }

        diagnostics.Add(DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.InvalidLinkParameter,
            component,
            offset,
            length,
            errorMessage));
    }
}
