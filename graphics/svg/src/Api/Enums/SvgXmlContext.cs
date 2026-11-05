namespace svg;

/// <summary>Defines inherited XML white-space intentions.</summary>
public enum SvgXmlSpaceMode
{
    /// <summary>Application default white-space processing is requested.</summary>
    Default = 0,
    /// <summary>All white-space characters are intended to be preserved.</summary>
    Preserve = 1,
    /// <summary>The nearest authored value is not an XML-defined value.</summary>
    Invalid = 2
}

/// <summary>Defines the SVG text white-space compatibility profile.</summary>
public enum SvgTextWhitespaceProfile
{
    /// <summary>SVG 2 processing where computed CSS white-space takes priority.</summary>
    Svg2 = 0,
    /// <summary>SVG 1.1 compatibility processing driven by <c>xml:space</c>.</summary>
    Svg11 = 1
}
