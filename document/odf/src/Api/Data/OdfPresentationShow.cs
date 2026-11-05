namespace odf;

/// <summary>Describes a named custom presentation show.</summary>
public class OdfPresentationShow
{
    /// <summary>The custom show name.</summary>
    public string Name = "";
    /// <summary>The preserved comma-separated page-name list.</summary>
    public string Pages = "";
    /// <summary>The parsed page names in show order.</summary>
    public string[] SlideNames = [];
    /// <summary>The resolved slides in show order.</summary>
    public OdfSlide[] Slides = [];
    /// <summary>Whether every listed page resolves uniquely.</summary>
    public bool IsPageListValid;
    /// <summary>The underlying custom-show element.</summary>
    public OdfNode? Node;
    internal string OriginalName = "";
    internal string OriginalPages = "";
    internal string[] OriginalSlideNames = [];
    internal OdfSlide[] OriginalSlides = [];
}
