namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontSet"/>.</summary>
public interface IFontSet
{
    /// <summary>Gets the number of typographic family groups.</summary>
    /// <returns>The family count.</returns>
    int GetFamilyCount();

    /// <summary>Gets one typographic family group in first-face order.</summary>
    /// <param name="index">The zero-based family index.</param>
    /// <returns>An immutable snapshot of the grouped faces.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the family range.
    /// </exception>
    FontFamily GetFamily(int index);

    /// <summary>Matches a face and resolved instance from an explicit request.</summary>
    /// <param name="request">The requested family, style, and coordinates.</param>
    /// <returns>The deterministic match, or null when no requested family can match.</returns>
    /// <exception cref="System.ArgumentNullException"><c>request</c> is null.</exception>
    /// <exception cref="FontFormatException">
    /// Required face metadata or variation data is unsupported or malformed.
    /// </exception>
    FontMatch? Match(FontRequest request);

    /// <summary>Shapes a string with configured font fallback and default options.</summary>
    /// <param name="text">The source UTF-16 text.</param>
    /// <param name="request">The requested family, style, and coordinates.</param>
    /// <returns>Ordered visual runs with their resolved font instances.</returns>
    TextShapingResult ShapeText(string text, FontRequest request);

    /// <summary>Shapes a string with configured font fallback and explicit options.</summary>
    /// <param name="text">The source UTF-16 text.</param>
    /// <param name="request">The requested family, style, and coordinates.</param>
    /// <param name="options">The Unicode and OpenType shaping controls.</param>
    /// <returns>Ordered visual runs with their resolved font instances.</returns>
    TextShapingResult ShapeTextWithOptions(
        string text,
        FontRequest request,
        TextShapingOptions options);

    /// <summary>Shapes a character span with configured fallback and default options.</summary>
    /// <param name="text">The source UTF-16 text span.</param>
    /// <param name="request">The requested family, style, and coordinates.</param>
    /// <returns>Ordered visual runs with their resolved font instances.</returns>
    TextShapingResult ShapeSpan(
        ReadOnlySpan<char> text,
        FontRequest request);

    /// <summary>Shapes a character span with configured fallback and explicit options.</summary>
    /// <param name="text">The source UTF-16 text span.</param>
    /// <param name="request">The requested family, style, and coordinates.</param>
    /// <param name="options">The Unicode and OpenType shaping controls.</param>
    /// <returns>Ordered visual runs with their resolved font instances.</returns>
    TextShapingResult ShapeSpanWithOptions(
        ReadOnlySpan<char> text,
        FontRequest request,
        TextShapingOptions options);

    /// <summary>Sets the concrete family mapped to one generic family.</summary>
    /// <param name="genericFamily">The generic family identifier.</param>
    /// <param name="familyName">A concrete localized family name.</param>
    /// <exception cref="System.ArgumentNullException"><c>familyName</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>familyName</c> is empty or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>genericFamily</c> is undefined.
    /// </exception>
    void SetGenericFamily(FontGenericFamily genericFamily, string familyName);

    /// <summary>Clears one configured generic-family mapping.</summary>
    /// <param name="genericFamily">The generic family identifier.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>genericFamily</c> is undefined.
    /// </exception>
    void ClearGenericFamily(FontGenericFamily genericFamily);

    /// <summary>Gets one configured concrete generic-family mapping.</summary>
    /// <param name="genericFamily">The generic family identifier.</param>
    /// <returns>The configured family name, or null.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>genericFamily</c> is undefined.
    /// </exception>
    string? GetGenericFamily(FontGenericFamily genericFamily);

    /// <summary>Adds a distinct set-wide fallback family at lowest priority.</summary>
    /// <param name="familyName">A concrete localized family name.</param>
    /// <returns>True when the family was added; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException"><c>familyName</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>familyName</c> is empty or whitespace.</exception>
    bool AddFallbackFamily(string familyName);

    /// <summary>Clears every set-wide fallback family.</summary>
    void ClearFallbackFamilies();

    /// <summary>Gets the number of configured set-wide fallback families.</summary>
    /// <returns>The configured family count.</returns>
    int GetFallbackFamilyCount();

    /// <summary>Gets one set-wide fallback family in caller order.</summary>
    /// <param name="index">The zero-based configured index.</param>
    /// <returns>The configured concrete family name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the configured range.
    /// </exception>
    string GetFallbackFamily(int index);

    /// <summary>Adds a distinct script-specific fallback family at lowest priority.</summary>
    /// <param name="script">The four-letter ISO 15924 script code.</param>
    /// <param name="familyName">A concrete localized family name.</param>
    /// <returns>True when the family was added; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>script</c> or <c>familyName</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// <c>script</c> is invalid or <c>familyName</c> is empty or whitespace.
    /// </exception>
    bool AddScriptFallbackFamily(string script, string familyName);

    /// <summary>Clears every fallback family for one script.</summary>
    /// <param name="script">The four-letter ISO 15924 script code.</param>
    /// <exception cref="System.ArgumentNullException"><c>script</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>script</c> is invalid.</exception>
    void ClearScriptFallbackFamilies(string script);

    /// <summary>Gets the configured fallback-family count for one script.</summary>
    /// <param name="script">The four-letter ISO 15924 script code.</param>
    /// <returns>The configured family count.</returns>
    /// <exception cref="System.ArgumentNullException"><c>script</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>script</c> is invalid.</exception>
    int GetScriptFallbackFamilyCount(string script);

    /// <summary>Gets one script-specific fallback family in caller order.</summary>
    /// <param name="script">The four-letter ISO 15924 script code.</param>
    /// <param name="index">The zero-based configured index.</param>
    /// <returns>The configured concrete family name.</returns>
    /// <exception cref="System.ArgumentNullException"><c>script</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>script</c> is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the configured range.
    /// </exception>
    string GetScriptFallbackFamily(string script, int index);

    /// <summary>Resolves the configured ordered match list for one script.</summary>
    /// <remarks>
    /// The requested concrete or generic family is resolved first, followed by
    /// script-specific families and then set-wide fallback families. Configuration
    /// is owned only by this font set.
    /// </remarks>
    /// <param name="request">The family, style, and coordinate request.</param>
    /// <param name="script">The four-letter ISO 15924 script code.</param>
    /// <returns>An immutable ordered list of distinct available matches.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>request</c> or <c>script</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException"><c>script</c> is invalid.</exception>
    FontFallbackList GetFallbacks(FontRequest request, string script);

    /// <summary>Resolves one indivisible shaping cluster to a single font match.</summary>
    /// <remarks>
    /// Configured matches with complete cluster coverage are preferred in fallback
    /// order. A base character followed by a variation selector requires support
    /// for that exact variation sequence. Default-ignorable format controls do not
    /// require glyphs. When no match covers the complete cluster, the match covering
    /// the most required scalars is returned, with fallback order breaking ties.
    /// The supplied cluster is never divided between matches, so callers may pass a
    /// larger shaping-unsafe span when it must remain indivisible.
    /// </remarks>
    /// <param name="request">The family, style, and coordinate request.</param>
    /// <param name="script">The four-letter ISO 15924 script code.</param>
    /// <param name="cluster">The nonempty UTF-16 shaping cluster.</param>
    /// <returns>The best single configured match, or null when none is available.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>request</c>, <c>script</c>, or <c>cluster</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException"><c>cluster</c> is empty.</exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// <c>cluster</c> contains an unpaired UTF-16 surrogate.
    /// </exception>
    FontMatch? ResolveCluster(FontRequest request, string script, string cluster);

    /// <summary>Adds a loaded face unless the set already contains the same face content.</summary>
    /// <param name="face">The loaded face to retain.</param>
    /// <returns>True when the face was added; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException"><c>face</c> is null.</exception>
    bool Add(FontFace face);

    /// <summary>Adds every distinct face from a loaded package in package order.</summary>
    /// <param name="package">The loaded package whose faces are added.</param>
    /// <returns>The number of newly added faces.</returns>
    /// <exception cref="System.ArgumentNullException"><c>package</c> is null.</exception>
    int AddPackage(FontPackage package);

    /// <summary>Adds every distinct face from an ordered face collection.</summary>
    /// <remarks>
    /// The collection is read completely before this set is changed, so a failed
    /// read cannot partially add its faces.
    /// </remarks>
    /// <param name="collection">The face collection to snapshot and add.</param>
    /// <returns>The number of newly added faces.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>collection</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// The collection returns a null face.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// The collection returns a negative face count.
    /// </exception>
    int AddCollection(IFontFaceCollection collection);

    /// <summary>Loads and adds every distinct face from a font file.</summary>
    /// <param name="path">The path of the font file to load.</param>
    /// <returns>The number of newly added faces.</returns>
    /// <exception cref="System.ArgumentNullException"><c>path</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>path</c> is empty.</exception>
    /// <exception cref="System.IO.IOException">The file cannot be read.</exception>
    /// <exception cref="System.UnauthorizedAccessException">File access is denied.</exception>
    /// <exception cref="FontFormatException">The file is not a supported valid font.</exception>
    /// <exception cref="FontLimitException">A configured default resource limit is exceeded.</exception>
    int AddFile(string path);

    /// <summary>Loads and adds every distinct face from a font file.</summary>
    /// <param name="path">The path of the font file to load.</param>
    /// <param name="options">The loading policies and limits.</param>
    /// <returns>The number of newly added faces.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>path</c> or <c>options</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException"><c>path</c> is empty.</exception>
    /// <exception cref="System.IO.IOException">The file cannot be read.</exception>
    /// <exception cref="System.UnauthorizedAccessException">File access is denied.</exception>
    /// <exception cref="FontFormatException">The file is not a supported valid font.</exception>
    /// <exception cref="FontLimitException">A configured resource limit is exceeded.</exception>
    int AddFileWithOptions(string path, FontLoadOptions options);

    /// <summary>Adds supported font files from the top level of a directory.</summary>
    /// <param name="path">The directory to scan.</param>
    /// <returns>The number of newly added faces.</returns>
    /// <exception cref="System.ArgumentNullException"><c>path</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>path</c> is empty.</exception>
    /// <exception cref="System.IO.IOException">The directory or a font file cannot be read.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Directory or file access is denied.</exception>
    /// <exception cref="FontFormatException">A discovered font file is not a supported valid font.</exception>
    /// <exception cref="FontLimitException">A configured default resource limit is exceeded.</exception>
    int AddDirectory(string path);

    /// <summary>Adds supported font files from the top level of a directory.</summary>
    /// <remarks>
    /// Files with <c>.ttf</c>, <c>.otf</c>, <c>.ttc</c>, <c>.otc</c>,
    /// <c>.woff</c>, or <c>.woff2</c> extensions are loaded in ordinal path order.
    /// Extension matching is case-insensitive.
    /// </remarks>
    /// <param name="path">The directory to scan.</param>
    /// <param name="options">The loading policies and limits applied to each file.</param>
    /// <returns>The number of newly added faces.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>path</c> or <c>options</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException"><c>path</c> is empty.</exception>
    /// <exception cref="System.IO.IOException">The directory or a font file cannot be read.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Directory or file access is denied.</exception>
    /// <exception cref="FontFormatException">A discovered font file is not a supported valid font.</exception>
    /// <exception cref="FontLimitException">A configured resource limit is exceeded.</exception>
    int AddDirectoryWithOptions(string path, FontLoadOptions options);
}
