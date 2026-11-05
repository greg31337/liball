namespace svg;

/// <summary>Selects the SVG conformance profile used by validation.</summary>
public enum SvgValidationProfile
{
    /// <summary>Validates current SVG 2 content and reports legacy features.</summary>
    Svg2 = 0,
    /// <summary>Validates SVG 1.1 compatibility content and rejects known SVG 2-only features.</summary>
    Svg11Compatibility = 1,
    /// <summary>Preserves unknown and no-namespace content with warnings.</summary>
    LegacyPreservation = 2
}

/// <summary>Identifies the conformance rule that produced a validation issue.</summary>
public enum SvgValidationRule
{
    /// <summary>The document container or root structure is invalid.</summary>
    DocumentStructure = 0,
    /// <summary>An element or attribute namespace is invalid in context.</summary>
    Namespace = 1,
    /// <summary>An element child or text node violates its parent's content model.</summary>
    ContentModel = 2,
    /// <summary>An attribute is not applicable to its element.</summary>
    AttributeApplicability = 3,
    /// <summary>A required element attribute is absent.</summary>
    RequiredAttribute = 4,
    /// <summary>A feature is deprecated in the selected profile.</summary>
    DeprecatedContent = 5,
    /// <summary>A recognized legacy feature has explicit compatibility handling.</summary>
    LegacyContent = 6,
    /// <summary>An SVG element or unqualified attribute name is unknown.</summary>
    UnknownContent = 7,
    /// <summary>A known feature is unavailable in the selected profile.</summary>
    ProfileContent = 8,
    /// <summary>An authored attribute does not match its typed grammar.</summary>
    TypedValue = 9,
    /// <summary>A parsed value is outside its permitted semantic range.</summary>
    ValueRange = 10,
    /// <summary>A list has invalid syntax, cardinality, or item values.</summary>
    ListValue = 11,
    /// <summary>Path data is malformed or exceeds configured geometry limits.</summary>
    PathData = 12,
    /// <summary>An animation timing or interpolation-control value is invalid.</summary>
    TimingValue = 13,
    /// <summary>A CSS selector is malformed or uses an unsupported selector feature.</summary>
    CssSelector = 14,
    /// <summary>A CSS at-rule is malformed or unsupported.</summary>
    CssAtRule = 15,
    /// <summary>A CSS declaration has malformed component syntax.</summary>
    CssDeclaration = 16,
    /// <summary>A CSS property is unknown or its value does not match the property grammar.</summary>
    CssProperty = 17,
    /// <summary>A syntactically valid CSS value is unsupported by the active property catalog.</summary>
    CssUnsupportedValue = 18,
    /// <summary>A custom property is cyclic or otherwise invalid at computed-value time.</summary>
    CssVariable = 19,
    /// <summary>A supported declaration failed typed computed-value conversion.</summary>
    CssComputedValue = 20,
    /// <summary>More than one element declares the same nonempty ID.</summary>
    DuplicateId = 21,
    /// <summary>A same-document reference does not identify an available element.</summary>
    MissingReference = 22,
    /// <summary>A reference identifies an element of an incompatible kind.</summary>
    WrongReferenceType = 23,
    /// <summary>A bounded reference chain re-enters an active target.</summary>
    ReferenceCycle = 24,
    /// <summary>An href or xlink:href value or chain is invalid.</summary>
    HrefReference = 25,
    /// <summary>A paint URL does not identify a usable paint server.</summary>
    PaintReference = 26,
    /// <summary>An effect URL does not identify a usable effect resource.</summary>
    EffectReference = 27,
    /// <summary>A filter primitive input does not identify an available input surface.</summary>
    FilterInput = 28,
    /// <summary>An animation does not identify a valid target or target value slot.</summary>
    AnimationTarget = 29,
    /// <summary>Acquired resource media metadata disagrees about the resource type.</summary>
    ResourceMediaType = 30,
    /// <summary>A script element retains executable content or an external script reference.</summary>
    ScriptContent = 31,
    /// <summary>An event-handler attribute retains executable content.</summary>
    EventHandler = 32,
    /// <summary>A URI uses a script-capable scheme.</summary>
    ScriptUrl = 33,
    /// <summary>Host-managed foreign content is present.</summary>
    ForeignContent = 34,
    /// <summary>An imported stylesheet is external or denied by resource policy.</summary>
    ExternalStyleSheet = 35,
    /// <summary>An image or embedded-media resource is external or denied by resource policy.</summary>
    ExternalImage = 36,
    /// <summary>A font resource is external or denied by resource policy.</summary>
    ExternalFont = 37,
    /// <summary>A data URL uses an active or resource-incompatible media type.</summary>
    UnsafeDataUrl = 38,
    /// <summary>An external link requires host navigation.</summary>
    ExternalLink = 39,
    /// <summary>The authored CSS declaration count exceeds its security bound.</summary>
    ExcessiveDeclarations = 40,
    /// <summary>An interactive, image, or explicitly semantic element has no accessible name.</summary>
    MissingAccessibleName = 41,
    /// <summary>An element that explicitly requires descriptive context has no accessible description.</summary>
    MissingAccessibleDescription = 42,
    /// <summary>An ARIA state conflicts with the element's role, focusability, or other authored state.</summary>
    AriaContradiction = 43,
    /// <summary>A link is excluded from sequential keyboard navigation.</summary>
    KeyboardAccessibility = 44,
    /// <summary>An authored title supplies accessibility semantics that must be preserved.</summary>
    TitlePreservationRisk = 45,
    /// <summary>An authored viewBox supplies scalable coordinate semantics that must be preserved.</summary>
    ViewBoxPreservationRisk = 46
}

/// <summary>Defines stable validation-operation failures.</summary>
public enum SvgValidationError
{
    /// <summary>Validation completed without an operational failure.</summary>
    None = 0,
    /// <summary>The document or options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The configured node or issue limit was exceeded.</summary>
    LimitExceeded = 2
}
