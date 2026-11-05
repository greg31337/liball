using System.Collections.Generic;

namespace uri;

internal static class WebLinkDuplicateParameterCore
{
    internal const string DuplicateParameterMessage = "The repeated Web Link singleton parameter was ignored.";

    internal static UriDiagnostic[] MarkIgnoredDuplicates(WebLink link)
    {
        List<UriDiagnostic> diagnostics = new List<UriDiagnostic>();
        bool hasRelation = false;
        bool hasReverseRelation = false;
        bool hasAnchor = false;
        bool hasMedia = false;
        bool hasTitle = false;
        bool hasExtendedTitle = false;
        bool hasType = false;
        int index = 0;
        while (index < link.Parameters.Length)
        {
            WebLinkParameter parameter = link.Parameters[index];
            bool duplicate = false;
            if (AsciiCase.EqualsIgnoreCase(parameter.Name, "rel"))
            {
                duplicate = hasRelation;
                hasRelation = true;
            }
            else if (AsciiCase.EqualsIgnoreCase(parameter.Name, "rev"))
            {
                duplicate = hasReverseRelation;
                hasReverseRelation = true;
            }
            else if (AsciiCase.EqualsIgnoreCase(parameter.Name, "anchor"))
            {
                duplicate = hasAnchor;
                hasAnchor = true;
            }
            else if (AsciiCase.EqualsIgnoreCase(parameter.Name, "media"))
            {
                duplicate = hasMedia;
                hasMedia = true;
            }
            else if (AsciiCase.EqualsIgnoreCase(parameter.Name, "title"))
            {
                duplicate = hasTitle;
                hasTitle = true;
            }
            else if (AsciiCase.EqualsIgnoreCase(parameter.Name, "title*"))
            {
                duplicate = hasExtendedTitle;
                hasExtendedTitle = true;
            }
            else if (AsciiCase.EqualsIgnoreCase(parameter.Name, "type"))
            {
                duplicate = hasType;
                hasType = true;
            }

            if (duplicate)
            {
                parameter.Ignored = true;
                diagnostics.Add(DiagnosticFactory.CreateWarning(
                    DiagnosticDomain.WebLink,
                    UriDiagnosticCode.DuplicateParameter,
                    UriComponent.LinkParameter,
                    parameter.Range.Offset,
                    parameter.Range.Length,
                    DuplicateParameterMessage));
            }

            index = index + 1;
        }

        return diagnostics.ToArray();
    }
}
