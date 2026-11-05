namespace docx;

/// <summary>
/// Provides access to the parsed data associated with a DOCX document.
/// </summary>
public interface IDocxDocumentModel
{
    /// <summary>
    /// Returns the main document body.
    /// </summary>
    /// <returns>The main document body.</returns>
    DocxBody GetBody();

    /// <summary>
    /// Returns the document core properties.
    /// </summary>
    /// <returns>The document core properties.</returns>
    DocxCoreProperties GetCoreProperties();

    /// <summary>
    /// Returns the document custom properties.
    /// </summary>
    /// <returns>The document custom properties.</returns>
    DocxCustomProperties GetCustomProperties();

    /// <summary>
    /// Returns all headers discovered through package relationships.
    /// </summary>
    /// <returns>A new list containing the document headers.</returns>
    List<DocxHeader> GetHeaders();

    /// <summary>
    /// Returns all footers discovered through package relationships.
    /// </summary>
    /// <returns>A new list containing the document footers.</returns>
    List<DocxFooter> GetFooters();

    /// <summary>
    /// Returns the document footnotes.
    /// </summary>
    /// <returns>The document footnotes.</returns>
    DocxFootnotes GetFootnotes();

    /// <summary>
    /// Returns the document endnotes.
    /// </summary>
    /// <returns>The document endnotes.</returns>
    DocxEndnotes GetEndnotes();

    /// <summary>
    /// Returns the document comments.
    /// </summary>
    /// <returns>The document comments.</returns>
    DocxComments GetComments();

    /// <summary>
    /// Returns content controls discovered throughout parsed document stories.
    /// </summary>
    /// <returns>A new list containing the content controls.</returns>
    List<DocxContentControl> GetContentControls();

    /// <summary>
    /// Returns custom XML parts discovered through package relationships.
    /// </summary>
    /// <returns>The custom XML parts.</returns>
    DocxCustomXmlParts GetCustomXmlParts();

    /// <summary>
    /// Returns the font table package part, or null when it is absent.
    /// </summary>
    /// <returns>The font table part, or null.</returns>
    DocxPart? GetFontTablePart();

    /// <summary>
    /// Returns the theme package part, or null when it is absent.
    /// </summary>
    /// <returns>The theme part, or null.</returns>
    DocxPart? GetThemePart();

    /// <summary>
    /// Returns the web settings package part, or null when it is absent.
    /// </summary>
    /// <returns>The web settings part, or null.</returns>
    DocxPart? GetWebSettingsPart();

    /// <summary>
    /// Returns every supported text story discovered through package relationships.
    /// </summary>
    /// <returns>A new list containing the discovered stories.</returns>
    List<DocxStory> GetStories();

    /// <summary>
    /// Finds text occurrences in the document.
    /// </summary>
    /// <param name="searchText">The text to find.</param>
    /// <returns>The matching text ranges.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>searchText</c> is null.</exception>
    List<DocxTextMatch> FindText(string searchText);

    /// <summary>
    /// Returns the document extended properties.
    /// </summary>
    /// <returns>The document extended properties.</returns>
    DocxExtendedProperties GetExtendedProperties();

    /// <summary>
    /// Returns the media discovered in the document package.
    /// </summary>
    /// <returns>The document media.</returns>
    DocxMedia GetMedia();

    /// <summary>
    /// Returns the document numbering definitions.
    /// </summary>
    /// <returns>The document numbering definitions.</returns>
    DocxNumbering GetNumbering();

    /// <summary>
    /// Returns the sections in document order.
    /// </summary>
    /// <returns>The document sections.</returns>
    List<DocxSection> GetSections();

    /// <summary>
    /// Returns the document settings.
    /// </summary>
    /// <returns>The document settings.</returns>
    DocxSettings GetSettings();

    /// <summary>
    /// Returns the document styles.
    /// </summary>
    /// <returns>The document styles.</returns>
    DocxStyles GetStyles();
}
