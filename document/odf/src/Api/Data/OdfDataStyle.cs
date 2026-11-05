namespace odf;

/// <summary>
/// Describes an OpenDocument number, date, time, boolean, or text data style.
/// </summary>
public class OdfDataStyle
{
    /// <summary>
    /// The style name.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The optional display name.
    /// </summary>
    public string DisplayName = "";
    /// <summary>
    /// The interpreted data-style kind.
    /// </summary>
    public OdfDataStyleKind Kind;
    /// <summary>
    /// The XML part containing the style, or an empty string for a flat document.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// Whether the style belongs to an automatic-styles section.
    /// </summary>
    public bool IsAutomatic;
    /// <summary>
    /// The preserved language code.
    /// </summary>
    public string Language = "";
    /// <summary>
    /// The preserved country code.
    /// </summary>
    public string Country = "";
    /// <summary>
    /// The preserved script code.
    /// </summary>
    public string Script = "";
    /// <summary>
    /// The preserved RFC language tag.
    /// </summary>
    public string LanguageTag = "";
    /// <summary>
    /// The optional data-style title.
    /// </summary>
    public string Title = "";
    /// <summary>
    /// The preserved volatile flag.
    /// </summary>
    public string Volatile = "";
    /// <summary>
    /// The preserved automatic-order flag.
    /// </summary>
    public string AutomaticOrder = "";
    /// <summary>
    /// The preserved format source.
    /// </summary>
    public string FormatSource = "";
    /// <summary>
    /// The preserved time overflow behavior.
    /// </summary>
    public string TruncateOnOverflow = "";
    /// <summary>
    /// The preserved transliteration format.
    /// </summary>
    public string TransliterationFormat = "";
    /// <summary>
    /// The preserved transliteration language.
    /// </summary>
    public string TransliterationLanguage = "";
    /// <summary>
    /// The preserved transliteration country.
    /// </summary>
    public string TransliterationCountry = "";
    /// <summary>
    /// The preserved transliteration style.
    /// </summary>
    public string TransliterationStyle = "";
    /// <summary>
    /// All qualified root attributes and their preserved lexical values.
    /// </summary>
    public OdfStyleProperty[] Attributes = [];
    /// <summary>
    /// The text properties applied to the formatted value.
    /// </summary>
    public OdfStyleProperty[] TextProperties = [];
    /// <summary>
    /// The child content of the text-properties element.
    /// </summary>
    public OdfNode[] TextPropertyChildren = [];
    /// <summary>
    /// The underlying text-properties element, when present.
    /// </summary>
    public OdfNode? TextPropertiesNode;
    /// <summary>
    /// The ordered value-format components.
    /// </summary>
    public OdfDataStyleComponent[] Components = [];
    /// <summary>
    /// Conditional mappings applied after the primary format.
    /// </summary>
    public OdfDataStyleMap[] Maps = [];
    /// <summary>
    /// Unmodeled non-element content preserved from the style.
    /// </summary>
    public OdfNode[] PassthroughContent = [];
    /// <summary>
    /// A portable interpreted pattern assembled from the ordered components.
    /// </summary>
    public string FormatPattern = "";
    /// <summary>
    /// The underlying data-style element, when loaded or written.
    /// </summary>
    public OdfNode? Node;
    internal OdfNode? OriginalNode;
    internal OdfAttribute[] OriginalAttributes = [];
    internal OdfNode? OriginalTextPropertiesNode;
    internal OdfAttribute[] OriginalTextPropertyAttributes = [];
    internal OdfNode[] OriginalTextPropertyChildren = [];
    internal OdfNode[] OriginalComponentNodes = [];
    internal OdfNode[] OriginalMapNodes = [];
    internal OdfNode[] OriginalPassthroughContent = [];
    internal string OriginalName = "";
    internal string OriginalDisplayName = "";
    internal OdfDataStyleKind OriginalKind;
    internal string OriginalPartPath = "";
    internal bool OriginalIsAutomatic;
    internal string OriginalLanguage = "";
    internal string OriginalCountry = "";
    internal string OriginalScript = "";
    internal string OriginalLanguageTag = "";
    internal string OriginalTitle = "";
    internal string OriginalVolatile = "";
    internal string OriginalAutomaticOrder = "";
    internal string OriginalFormatSource = "";
    internal string OriginalTruncateOnOverflow = "";
    internal string OriginalTransliterationFormat = "";
    internal string OriginalTransliterationLanguage = "";
    internal string OriginalTransliterationCountry = "";
    internal string OriginalTransliterationStyle = "";
}
