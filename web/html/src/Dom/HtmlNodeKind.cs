namespace html;

internal enum HtmlNamespace
{
    None = 0,
    Html = 1,
    Svg = 2,
    MathMl = 3,
    XLink = 4,
    Xml = 5,
    Xmlns = 6,
    Other = 7
}

internal static class HtmlNamespaceData
{
    internal static string GetUri(HtmlNamespace value)
    {
        switch (value)
        {
            case HtmlNamespace.None:
                return "";
            case HtmlNamespace.Html:
                return "http://www.w3.org/1999/xhtml";
            case HtmlNamespace.Svg:
                return "http://www.w3.org/2000/svg";
            case HtmlNamespace.MathMl:
                return "http://www.w3.org/1998/Math/MathML";
            case HtmlNamespace.XLink:
                return "http://www.w3.org/1999/xlink";
            case HtmlNamespace.Xml:
                return "http://www.w3.org/XML/1998/namespace";
            case HtmlNamespace.Xmlns:
                return "http://www.w3.org/2000/xmlns/";
            case HtmlNamespace.Other:
                return "";
            default:
                throw new System.ArgumentOutOfRangeException("value", (int)value,
                    "Unknown HTML namespace.");
        }
    }

    internal static HtmlNamespace GetKind(string namespaceUri)
    {
        if (namespaceUri == null)
        {
            throw new System.ArgumentNullException("namespaceUri");
        }

        if (namespaceUri.Length == 0)
        {
            return HtmlNamespace.None;
        }

        if (namespaceUri == GetUri(HtmlNamespace.Html))
        {
            return HtmlNamespace.Html;
        }

        if (namespaceUri == GetUri(HtmlNamespace.Svg))
        {
            return HtmlNamespace.Svg;
        }

        if (namespaceUri == GetUri(HtmlNamespace.MathMl))
        {
            return HtmlNamespace.MathMl;
        }

        if (namespaceUri == GetUri(HtmlNamespace.XLink))
        {
            return HtmlNamespace.XLink;
        }

        if (namespaceUri == GetUri(HtmlNamespace.Xml))
        {
            return HtmlNamespace.Xml;
        }

        if (namespaceUri == GetUri(HtmlNamespace.Xmlns))
        {
            return HtmlNamespace.Xmlns;
        }

        return HtmlNamespace.Other;
    }
}
