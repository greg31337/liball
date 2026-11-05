namespace docx;

/// <summary>
/// Identifies a standard extended document property.
/// </summary>
public enum DocxExtendedPropertyKind
{
    /// <summary>The template property.</summary>
    Template = 0,
    /// <summary>The manager property.</summary>
    Manager = 1,
    /// <summary>The company property.</summary>
    Company = 2,
    /// <summary>The page-count property.</summary>
    Pages = 3,
    /// <summary>The word-count property.</summary>
    Words = 4,
    /// <summary>The character-count property.</summary>
    Characters = 5,
    /// <summary>The presentation-format property.</summary>
    PresentationFormat = 6,
    /// <summary>The line-count property.</summary>
    Lines = 7,
    /// <summary>The paragraph-count property.</summary>
    Paragraphs = 8,
    /// <summary>The slide-count property.</summary>
    Slides = 9,
    /// <summary>The note-count property.</summary>
    Notes = 10,
    /// <summary>The total-editing-time property.</summary>
    TotalTime = 11,
    /// <summary>The hidden-slide-count property.</summary>
    HiddenSlides = 12,
    /// <summary>The multimedia-clip-count property.</summary>
    MultimediaClips = 13,
    /// <summary>The scale-crop property.</summary>
    ScaleCrop = 14,
    /// <summary>The links-up-to-date property.</summary>
    LinksUpToDate = 15,
    /// <summary>The characters-with-spaces property.</summary>
    CharactersWithSpaces = 16,
    /// <summary>The shared-document property.</summary>
    SharedDocument = 17,
    /// <summary>The hyperlink-base property.</summary>
    HyperlinkBase = 18,
    /// <summary>The hyperlinks-changed property.</summary>
    HyperlinksChanged = 19,
    /// <summary>The application-version property.</summary>
    ApplicationVersion = 20,
    /// <summary>The application property.</summary>
    Application = 21,
    /// <summary>The document-security property.</summary>
    DocumentSecurity = 22,
    /// <summary>The heading-pairs vector XML.</summary>
    HeadingPairsXml = 23,
    /// <summary>The titles-of-parts vector XML.</summary>
    TitlesOfPartsXml = 24
}
