namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontFace"/>.</summary>
public interface IFontFace
{
    /// <summary>Gets this face's zero-based package index.</summary>
    /// <returns>The face index.</returns>
    int GetFaceIndex();

    /// <summary>Gets the face's SFNT scaler type.</summary>
    /// <returns>The big-endian scaler type.</returns>
    uint GetScalerType();

    /// <summary>Gets the validated outline technology.</summary>
    /// <returns>The outline kind, or unknown when it could not be established.</returns>
    FontOutlineKind GetOutlineKind();

    /// <summary>Gets the count of bounded tables in this face.</summary>
    /// <returns>The bounded table count.</returns>
    int GetTableCount();

    /// <summary>Reports whether this face contains a bounded table.</summary>
    /// <param name="tag">The big-endian table tag.</param>
    /// <returns>True when the table is present; otherwise false.</returns>
    bool HasTable(int tag);

    /// <summary>Copies one preserved bounded table.</summary>
    /// <param name="tag">The big-endian table tag.</param>
    /// <returns>A new array containing the table bytes.</returns>
    /// <exception cref="System.ArgumentException">The table is not present.</exception>
    /// <exception cref="System.InvalidOperationException">
    /// Raw table preservation was disabled when the package was loaded.
    /// </exception>
    byte[] CopyTable(int tag);

    /// <summary>Gets descriptive results using deterministic default name selection.</summary>
    /// <returns>The cached description result.</returns>
    FontDescription GetDescription();

    /// <summary>Gets descriptive results with names selected for a language.</summary>
    /// <param name="languageTag">The preferred BCP 47 language tag.</param>
    /// <returns>A description containing localized names.</returns>
    /// <exception cref="System.ArgumentNullException"><c>languageTag</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>languageTag</c> is invalid.</exception>
    FontDescription GetLocalizedDescription(string languageTag);

    /// <summary>Gets names using deterministic default language fallback.</summary>
    /// <returns>The cached names result.</returns>
    FontNames GetNames();

    /// <summary>Gets names selected for a preferred language.</summary>
    /// <param name="languageTag">The preferred BCP 47 language tag.</param>
    /// <returns>A localized names result.</returns>
    /// <exception cref="System.ArgumentNullException"><c>languageTag</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>languageTag</c> is invalid.</exception>
    FontNames GetLocalizedNames(string languageTag);

    /// <summary>Gets style, family, range, and optical classification metadata.</summary>
    /// <returns>The cached classification result.</returns>
    FontClassification GetClassification();

    /// <summary>Gets merged Unicode coverage without expanding individual values.</summary>
    /// <returns>The cached coverage result.</returns>
    FontCoverage GetCoverage();

    /// <summary>Gets the ranked Unicode and variation character mapping.</summary>
    /// <returns>The cached immutable character map.</returns>
    FontCharacterMap GetCharacterMap();

    /// <summary>Gets glyph names, provenance, and Unicode identities.</summary>
    /// <returns>The cached immutable glyph catalog.</returns>
    /// <exception cref="FontFormatException">
    /// Required glyph-count or present glyph-name data is malformed or unsupported.
    /// </exception>
    FontGlyphCatalog GetGlyphCatalog();

    /// <summary>Gets BASE baseline and legacy metric-table inspection data.</summary>
    /// <returns>The cached immutable metric inspection.</returns>
    FontMetricInspection GetMetricInspection();

    /// <summary>Gets variable-font metadata using deterministic default name selection.</summary>
    /// <returns>The cached localized variation model.</returns>
    /// <exception cref="FontFormatException">
    /// A present variation metadata table is malformed or unsupported.
    /// </exception>
    FontVariations GetVariations();

    /// <summary>Gets variable-font metadata with names selected for a language.</summary>
    /// <param name="languageTag">The preferred BCP 47 language tag.</param>
    /// <returns>A localized variation model.</returns>
    /// <exception cref="System.ArgumentNullException"><c>languageTag</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>languageTag</c> is invalid.</exception>
    /// <exception cref="FontFormatException">
    /// A present variation metadata table is malformed or unsupported.
    /// </exception>
    FontVariations GetLocalizedVariations(string languageTag);

    /// <summary>Gets CPAL palettes using deterministic default name selection.</summary>
    /// <returns>The cached palette model.</returns>
    /// <exception cref="FontFormatException">
    /// A present CPAL table is malformed or unsupported.
    /// </exception>
    FontColorPalettes GetColorPalettes();

    /// <summary>Gets CPAL palettes with labels selected for a language.</summary>
    /// <param name="languageTag">The preferred BCP 47 language tag.</param>
    /// <returns>A localized palette model.</returns>
    /// <exception cref="System.ArgumentNullException"><c>languageTag</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>languageTag</c> is invalid.</exception>
    /// <exception cref="FontFormatException">
    /// A present CPAL table is malformed or unsupported.
    /// </exception>
    FontColorPalettes GetLocalizedColorPalettes(string languageTag);

    /// <summary>Gets public GSUB and GPOS layout discovery.</summary>
    /// <returns>The cached immutable layout inspection.</returns>
    /// <exception cref="FontFormatException">
    /// A present layout table is malformed or unsupported.
    /// </exception>
    FontLayoutInspection GetLayoutInspection();

    /// <summary>Gets read-only GDEF, BASE, JSTF, and MATH inspection.</summary>
    /// <returns>The cached immutable advanced-layout inspection.</returns>
    /// <exception cref="FontFormatException">
    /// A present advanced-layout table is malformed or unsupported.
    /// </exception>
    FontAdvancedLayoutInspection GetAdvancedLayoutInspection();

    /// <summary>Gets validated unscaled metric-source metadata.</summary>
    /// <returns>The cached metric result.</returns>
    FontMetrics GetMetrics();

    /// <summary>Selects global line metrics and returns all coordinates in design units.</summary>
    /// <param name="policy">The explicit line-metric selection policy.</param>
    /// <returns>The selected design-unit metrics.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>policy</c> is undefined.</exception>
    /// <exception cref="FontFormatException">A required metric source is unavailable.</exception>
    FontDesignMetrics GetDesignMetrics(FontMetricSelectionPolicy policy);

    /// <summary>Selects global line metrics and scales coordinates to a point size.</summary>
    /// <param name="policy">The explicit line-metric selection policy.</param>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>The scaled metrics.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>policy</c> is undefined or <c>pointSize</c> is negative or non-finite.
    /// </exception>
    /// <exception cref="FontFormatException">A required metric source is unavailable.</exception>
    FontScaledMetrics GetScaledMetrics(FontMetricSelectionPolicy policy, double pointSize);

    /// <summary>Gets one glyph's horizontal and vertical metrics in design units.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <param name="glyphYMaximum">
    /// The glyph ink maximum y coordinate used to resolve vertical origins and bearings.
    /// </param>
    /// <returns>The design-unit glyph metrics.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>glyphId</c> is outside the font.</exception>
    /// <exception cref="FontFormatException">Required metric tables are unavailable.</exception>
    GlyphMetrics GetDesignGlyphMetrics(int glyphId, int glyphYMaximum);

    /// <summary>Gets one glyph's horizontal and vertical metrics scaled to a point size.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <param name="glyphYMaximum">
    /// The glyph ink maximum y coordinate used to resolve vertical origins and bearings.
    /// </param>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>The scaled glyph metrics.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>glyphId</c> is outside the font or <c>pointSize</c> is negative or non-finite.
    /// </exception>
    /// <exception cref="FontFormatException">Required metrics are unavailable.</exception>
    ScaledGlyphMetrics GetScaledGlyphMetrics(int glyphId, int glyphYMaximum, double pointSize);

    /// <summary>Creates or gets the default, unvaried font instance.</summary>
    /// <returns>The stable default instance for this face.</returns>
    /// <exception cref="FontFormatException">
    /// The face does not contain a supported glyph-count table.
    /// </exception>
    FontInstance CreateDefaultInstance();

    /// <summary>Creates an instance from user-scale variation coordinates.</summary>
    /// <param name="coordinates">The explicit coordinates; omitted axes use defaults.</param>
    /// <returns>An immutable font instance at the normalized location.</returns>
    /// <exception cref="System.ArgumentNullException"><c>coordinates</c> is null.</exception>
    /// <exception cref="System.ArgumentException">
    /// A supplied coordinate does not correspond to an axis in this face.
    /// </exception>
    /// <exception cref="FontFormatException">
    /// Required glyph-count or variation metadata is unavailable.
    /// </exception>
    FontInstance CreateInstance(FontCoordinates coordinates);

    /// <summary>Gets the number of named variation instances.</summary>
    /// <returns>The <c>fvar</c> instance count, or zero for a static face.</returns>
    /// <exception cref="FontFormatException">
    /// The face contains an unsupported or malformed <c>fvar</c> table.
    /// </exception>
    int GetNamedInstanceCount();

    /// <summary>Creates an instance at one named <c>fvar</c> location.</summary>
    /// <param name="index">The zero-based named-instance index.</param>
    /// <returns>The resolved named instance.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    /// <exception cref="FontFormatException">
    /// Required glyph-count or variation metadata is unavailable.
    /// </exception>
    FontInstance CreateNamedInstance(int index);

    /// <summary>Gets interpreted embedding rights without overriding restrictions.</summary>
    /// <returns>The cached rights result, or null when OS/2 is unavailable.</returns>
    FontEmbeddingRights? GetEmbeddingRights();

    /// <summary>Gets bounded metadata for a table by directory index.</summary>
    /// <param name="index">The zero-based table index.</param>
    /// <returns>The table result.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the table list.</exception>
    FontTableInfo GetTableInfo(int index);

    /// <summary>Finds bounded table metadata by tag.</summary>
    /// <param name="tag">The big-endian table tag.</param>
    /// <returns>The table result, or null when the table is absent.</returns>
    FontTableInfo? FindTableInfo(int tag);

    /// <summary>Reports whether the face passed structural directory validation.</summary>
    /// <returns>True when semantic access and standalone emission are safe.</returns>
    bool IsStructurallyValid();

    /// <summary>Gets the standalone whole-font checksum validation result.</summary>
    /// <returns>The checksum-adjustment validation state.</returns>
    FontChecksumValidationState GetChecksumValidationState();

    /// <summary>Copies this selected face as a deterministic standalone SFNT font.</summary>
    /// <returns>A new array containing the standalone SFNT bytes.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The selected inspect-only face is not structurally valid.
    /// </exception>
    byte[] CopyStandaloneSfntBytes();
}
