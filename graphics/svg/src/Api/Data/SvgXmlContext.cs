namespace svg;

/// <summary>Describes inherited white-space behavior without changing DOM text.</summary>
public class SvgTextWhitespacePolicy
{
    /// <summary>The requested SVG compatibility profile.</summary>
    public SvgTextWhitespaceProfile Profile;
    /// <summary>The nearest inherited XML white-space intention.</summary>
    public SvgXmlSpaceMode XmlSpace;
    /// <summary>The authored <c>xml:space</c> attribute, or <c>null</c> for the default.</summary>
    public SvgAttribute? SourceAttribute;
    /// <summary>Whether a computed CSS <c>white-space</c> value takes priority over this legacy policy.</summary>
    public bool CssWhiteSpaceHasPriority;
    /// <summary>Whether newline characters are removed by the legacy algorithm.</summary>
    public bool RemoveNewLines;
    /// <summary>Whether newline characters are converted to spaces by the legacy algorithm.</summary>
    public bool ConvertNewLinesToSpaces;
    /// <summary>Whether tab characters are converted to spaces by the legacy algorithm.</summary>
    public bool ConvertTabsToSpaces;
    /// <summary>Whether leading and trailing spaces are removed by the legacy algorithm.</summary>
    public bool TrimSpaces;
    /// <summary>Whether contiguous spaces are collapsed by the legacy algorithm.</summary>
    public bool CollapseSpaces;

    internal SvgTextWhitespacePolicy(SvgTextWhitespaceProfile profile, SvgXmlSpaceMode xmlSpace, SvgAttribute? sourceAttribute)
    {
        Profile = profile;
        XmlSpace = xmlSpace;
        SourceAttribute = sourceAttribute;
        CssWhiteSpaceHasPriority = profile == SvgTextWhitespaceProfile.Svg2;
        bool preserve = xmlSpace == SvgXmlSpaceMode.Preserve;
        RemoveNewLines = !preserve;
        ConvertNewLinesToSpaces = preserve;
        ConvertTabsToSpaces = true;
        TrimSpaces = !preserve;
        CollapseSpaces = !preserve;
    }
}
