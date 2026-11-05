namespace ttf;

/// <summary>Owns an ordered collection of distinct loaded font faces.</summary>
/// <remarks>
/// A font set is application-owned and does not inspect or modify operating-system
/// font registries. Faces retain their first-addition order. Duplicate detection is
/// based on stable face content rather than object identity, file path, or package
/// identity.
/// </remarks>
public class FontSet : IFontFaceCollection, IFontSet
{
    System.Collections.Generic.List<FontFace> faces;
    System.Collections.Generic.Dictionary<
        string,
        System.Collections.Generic.List<FontFace>> faceBuckets;
    FontFamily[]? families;
    FontFallbackConfiguration fallbackConfiguration;
    UnicodeData? unicodeData;
    object syncRoot;

    /// <summary>Creates an empty application-owned font set.</summary>
    public FontSet()
    {
        faces = new System.Collections.Generic.List<FontFace>();
        faceBuckets = new System.Collections.Generic.Dictionary<
            string,
            System.Collections.Generic.List<FontFace>>(System.StringComparer.Ordinal);
        families = null;
        fallbackConfiguration = new FontFallbackConfiguration();
        unicodeData = null;
        syncRoot = new object();
    }

    /// <summary>Gets the number of distinct faces in first-addition order.</summary>
    /// <returns>The distinct face count.</returns>
    public int GetFaceCount()
    {
        lock (syncRoot)
        {
            return faces.Count;
        }
    }

    /// <summary>Gets one distinct face in first-addition order.</summary>
    /// <param name="index">The zero-based face index.</param>
    /// <returns>The retained face instance.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the set's face range.
    /// </exception>
    public FontFace GetFace(int index)
    {
        lock (syncRoot)
        {
            if (index < 0 || index >= faces.Count)
            {
                throw new System.ArgumentOutOfRangeException("index");
            }

            return faces[index];
        }
    }

    /// <summary>Gets the number of typographic family groups.</summary>
    /// <returns>The family count.</returns>
    public int GetFamilyCount()
    {
        lock (syncRoot)
        {
            EnsureFamilies();
            System.ArgumentNullException.ThrowIfNull(families);
            return families.Length;
        }
    }

    /// <summary>Gets one typographic family group in first-face order.</summary>
    /// <param name="index">The zero-based family index.</param>
    /// <returns>An immutable snapshot of the grouped faces.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the family range.
    /// </exception>
    public FontFamily GetFamily(int index)
    {
        lock (syncRoot)
        {
            EnsureFamilies();
            System.ArgumentNullException.ThrowIfNull(families);

            if (index < 0 || index >= families.Length)
            {
                throw new System.ArgumentOutOfRangeException("index");
            }

            return families[index];
        }
    }

    /// <summary>Matches a face and resolved instance from an explicit request.</summary>
    /// <param name="request">The requested family, style, and coordinates.</param>
    /// <returns>The deterministic match, or null when no requested family can match.</returns>
    /// <exception cref="System.ArgumentNullException"><c>request</c> is null.</exception>
    /// <exception cref="FontFormatException">
    /// Required face metadata or variation data is unsupported or malformed.
    /// </exception>
    public FontMatch? Match(FontRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        FontFamily[]? familySnapshot;
        string? concreteFamily;

        lock (syncRoot)
        {
            EnsureFamilies();
            familySnapshot = families;
            concreteFamily = fallbackConfiguration.ResolveGenericFamily(request);
        }

        if (concreteFamily == null)
        {
            return null;
        }

        FontRequest concreteRequest = request.HasGenericFamily()
            ? request.CopyForFamily(concreteFamily)
            : request;
        return FontMatcher.Match(familySnapshot, concreteRequest);
    }

    /// <summary>Shapes a string with configured font fallback and default options.</summary>
    /// <param name="text">The source UTF-16 text.</param>
    /// <param name="request">The requested family, style, and coordinates.</param>
    /// <returns>Ordered visual runs with their resolved font instances.</returns>
    /// <exception cref="System.ArgumentNullException"><c>text</c> or <c>request</c> is null.</exception>
    /// <exception cref="System.Text.DecoderFallbackException"><c>text</c> contains malformed UTF-16.</exception>
    /// <exception cref="FontFormatException">Required font data is unavailable or malformed.</exception>
    /// <exception cref="FontLimitException">A shaping resource limit is exceeded.</exception>
    public TextShapingResult ShapeText(
        string text,
        FontRequest request)
    {
        return TextShaper.Shape(text, this, request);
    }

    /// <summary>Shapes a string with configured font fallback and explicit options.</summary>
    /// <param name="text">The source UTF-16 text.</param>
    /// <param name="request">The requested family, style, and coordinates.</param>
    /// <param name="options">The Unicode and OpenType shaping controls.</param>
    /// <returns>Ordered visual runs with their resolved font instances.</returns>
    /// <exception cref="System.ArgumentNullException"><c>text</c>, <c>request</c>, or <c>options</c> is null.</exception>
    /// <exception cref="System.Text.DecoderFallbackException"><c>text</c> contains malformed UTF-16.</exception>
    /// <exception cref="FontFormatException">Required font data is unavailable or malformed.</exception>
    /// <exception cref="FontLimitException">A shaping resource limit is exceeded.</exception>
    public TextShapingResult ShapeTextWithOptions(
        string text,
        FontRequest request,
        TextShapingOptions options)
    {
        return TextShaper.Shape(text, this, request, options);
    }

    /// <summary>Shapes a character span with configured fallback and default options.</summary>
    /// <remarks>The span is copied before shaping and is never retained.</remarks>
    /// <param name="text">The source UTF-16 text span.</param>
    /// <param name="request">The requested family, style, and coordinates.</param>
    /// <returns>Ordered visual runs with their resolved font instances.</returns>
    /// <exception cref="System.ArgumentNullException"><c>request</c> is null.</exception>
    /// <exception cref="System.Text.DecoderFallbackException"><c>text</c> contains malformed UTF-16.</exception>
    /// <exception cref="FontFormatException">Required font data is unavailable or malformed.</exception>
    /// <exception cref="FontLimitException">A shaping resource limit is exceeded.</exception>
    public TextShapingResult ShapeSpan(
        ReadOnlySpan<char> text,
        FontRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        return TextShaper.Shape(text.ToString(), this, request);
    }

    /// <summary>Shapes a character span with configured fallback and explicit options.</summary>
    /// <remarks>The span is copied before shaping and is never retained.</remarks>
    /// <param name="text">The source UTF-16 text span.</param>
    /// <param name="request">The requested family, style, and coordinates.</param>
    /// <param name="options">The Unicode and OpenType shaping controls.</param>
    /// <returns>Ordered visual runs with their resolved font instances.</returns>
    /// <exception cref="System.ArgumentNullException"><c>request</c> or <c>options</c> is null.</exception>
    /// <exception cref="System.Text.DecoderFallbackException"><c>text</c> contains malformed UTF-16.</exception>
    /// <exception cref="FontFormatException">Required font data is unavailable or malformed.</exception>
    /// <exception cref="FontLimitException">A shaping resource limit is exceeded.</exception>
    public TextShapingResult ShapeSpanWithOptions(
        ReadOnlySpan<char> text,
        FontRequest request,
        TextShapingOptions options)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        return TextShaper.Shape(text.ToString(), this, request, options);
    }

    /// <summary>Sets the concrete family mapped to one generic family.</summary>
    /// <param name="genericFamily">The generic family identifier.</param>
    /// <param name="familyName">A concrete localized family name.</param>
    /// <exception cref="System.ArgumentNullException"><c>familyName</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>familyName</c> is empty or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>genericFamily</c> is undefined.
    /// </exception>
    public void SetGenericFamily(FontGenericFamily genericFamily, string familyName)
    {
        lock (syncRoot)
        {
            fallbackConfiguration.SetGenericFamily(genericFamily, familyName);
        }
    }

    /// <summary>Clears one configured generic-family mapping.</summary>
    /// <param name="genericFamily">The generic family identifier.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>genericFamily</c> is undefined.
    /// </exception>
    public void ClearGenericFamily(FontGenericFamily genericFamily)
    {
        lock (syncRoot)
        {
            fallbackConfiguration.ClearGenericFamily(genericFamily);
        }
    }

    /// <summary>Gets one configured concrete generic-family mapping.</summary>
    /// <param name="genericFamily">The generic family identifier.</param>
    /// <returns>The configured family name, or null.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>genericFamily</c> is undefined.
    /// </exception>
    public string? GetGenericFamily(FontGenericFamily genericFamily)
    {
        lock (syncRoot)
        {
            return fallbackConfiguration.GetGenericFamily(genericFamily);
        }
    }

    /// <summary>Adds a distinct set-wide fallback family at lowest priority.</summary>
    /// <param name="familyName">A concrete localized family name.</param>
    /// <returns>True when the family was added; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException"><c>familyName</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>familyName</c> is empty or whitespace.</exception>
    public bool AddFallbackFamily(string familyName)
    {
        lock (syncRoot)
        {
            return fallbackConfiguration.AddFallbackFamily(familyName);
        }
    }

    /// <summary>Clears every set-wide fallback family.</summary>
    public void ClearFallbackFamilies()
    {
        lock (syncRoot)
        {
            fallbackConfiguration.ClearFallbackFamilies();
        }
    }

    /// <summary>Gets the number of configured set-wide fallback families.</summary>
    /// <returns>The configured family count.</returns>
    public int GetFallbackFamilyCount()
    {
        lock (syncRoot)
        {
            return fallbackConfiguration.GetFallbackFamilyCount();
        }
    }

    /// <summary>Gets one set-wide fallback family in caller order.</summary>
    /// <param name="index">The zero-based configured index.</param>
    /// <returns>The configured concrete family name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the configured range.
    /// </exception>
    public string GetFallbackFamily(int index)
    {
        lock (syncRoot)
        {
            return fallbackConfiguration.GetFallbackFamily(index);
        }
    }

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
    public bool AddScriptFallbackFamily(string script, string familyName)
    {
        lock (syncRoot)
        {
            return fallbackConfiguration.AddScriptFallbackFamily(script, familyName);
        }
    }

    /// <summary>Clears every fallback family for one script.</summary>
    /// <param name="script">The four-letter ISO 15924 script code.</param>
    /// <exception cref="System.ArgumentNullException"><c>script</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>script</c> is invalid.</exception>
    public void ClearScriptFallbackFamilies(string script)
    {
        lock (syncRoot)
        {
            fallbackConfiguration.ClearScriptFallbackFamilies(script);
        }
    }

    /// <summary>Gets the configured fallback-family count for one script.</summary>
    /// <param name="script">The four-letter ISO 15924 script code.</param>
    /// <returns>The configured family count.</returns>
    /// <exception cref="System.ArgumentNullException"><c>script</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>script</c> is invalid.</exception>
    public int GetScriptFallbackFamilyCount(string script)
    {
        lock (syncRoot)
        {
            return fallbackConfiguration.GetScriptFallbackFamilyCount(script);
        }
    }

    /// <summary>Gets one script-specific fallback family in caller order.</summary>
    /// <param name="script">The four-letter ISO 15924 script code.</param>
    /// <param name="index">The zero-based configured index.</param>
    /// <returns>The configured concrete family name.</returns>
    /// <exception cref="System.ArgumentNullException"><c>script</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>script</c> is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the configured range.
    /// </exception>
    public string GetScriptFallbackFamily(string script, int index)
    {
        lock (syncRoot)
        {
            return fallbackConfiguration.GetScriptFallbackFamily(script, index);
        }
    }

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
    public FontFallbackList GetFallbacks(FontRequest request, string script)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        FontFamily[]? familySnapshot;
        string? primaryFamily;
        string[] scriptFamilies;
        string[] fallbackFamilies;

        lock (syncRoot)
        {
            EnsureFamilies();
            familySnapshot = families;
            primaryFamily = fallbackConfiguration.ResolveGenericFamily(request);
            scriptFamilies = fallbackConfiguration.CopyScriptFallbackFamilies(script);
            fallbackFamilies = fallbackConfiguration.CopyFallbackFamilies();
        }

        return FontFallbackResolver.Resolve(
            familySnapshot,
            request,
            primaryFamily,
            scriptFamilies,
            fallbackFamilies);
    }

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
    public FontMatch? ResolveCluster(
        FontRequest request,
        string script,
        string cluster)
    {
        if (cluster == null)
        {
            throw new System.ArgumentNullException("cluster");
        }

        if (cluster.Length == 0)
        {
            throw new System.ArgumentException(
                "A shaping cluster cannot be empty.",
                "cluster");
        }

        FontFallbackList fallbacks = GetFallbacks(request, script);
        RuneBuffer runes = Utf16Decoder.Decode(
            cluster,
            Utf16DecodingMode.Strict,
            cluster.Length);
        return FontClusterFallbackResolver.Resolve(
            fallbacks,
            runes,
            GetUnicodeData());
    }

    internal UnicodeData GetUnicodeData()
    {
        lock (syncRoot)
        {
            if (unicodeData == null)
            {
                unicodeData = UnicodeData.LoadDefault();
            }

            return unicodeData;
        }
    }

    /// <summary>Adds a loaded face unless the set already contains the same face content.</summary>
    /// <param name="face">The loaded face to retain.</param>
    /// <returns>True when the face was added; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException"><c>face</c> is null.</exception>
    public bool Add(FontFace face)
    {
        if (face == null)
        {
            throw new System.ArgumentNullException("face");
        }

        string identity = FontSetFaceIdentity.Create(face);

        lock (syncRoot)
        {
            if (ContainsEquivalent(identity, face))
            {
                return false;
            }

            faces.Add(face);
            AddToBucket(identity, face);
            return true;
        }
    }

    /// <summary>Adds every distinct face from a loaded package in package order.</summary>
    /// <param name="package">The loaded package whose faces are added.</param>
    /// <returns>The number of newly added faces.</returns>
    /// <exception cref="System.ArgumentNullException"><c>package</c> is null.</exception>
    public int AddPackage(FontPackage package)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        return AddCollection((IFontFaceCollection)package);
    }

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
    public int AddCollection(IFontFaceCollection collection)
    {
        if (collection == null)
        {
            throw new System.ArgumentNullException("collection");
        }

        int count = collection.GetFaceCount();
        if (count < 0)
        {
            throw new System.ArgumentOutOfRangeException("collection");
        }

        FontFace[] collectionFaces = new FontFace[count];
        int index = 0;

        while (index < collectionFaces.Length)
        {
            FontFace face = collection.GetFace(index);
            if (face == null)
            {
                throw new System.ArgumentException(
                    "The collection returned a null face.",
                    "collection");
            }

            collectionFaces[index] = face;
            index += 1;
        }

        return AddFaces(collectionFaces);
    }

    /// <summary>Loads and adds every distinct face from a font file.</summary>
    /// <param name="path">The path of the font file to load.</param>
    /// <returns>The number of newly added faces.</returns>
    /// <exception cref="System.ArgumentNullException"><c>path</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>path</c> is empty.</exception>
    /// <exception cref="System.IO.IOException">The file cannot be read.</exception>
    /// <exception cref="System.UnauthorizedAccessException">File access is denied.</exception>
    /// <exception cref="FontFormatException">The file is not a supported valid font.</exception>
    /// <exception cref="FontLimitException">A configured default resource limit is exceeded.</exception>
    public int AddFile(string path)
    {
        return AddFileWithOptions(path, FontLoadOptions.CreateCompatible());
    }

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
    public int AddFileWithOptions(string path, FontLoadOptions options)
    {
        return AddPackage(FontLoader.LoadFile(path, options));
    }

    /// <summary>Adds supported font files from the top level of a directory.</summary>
    /// <param name="path">The directory to scan.</param>
    /// <returns>The number of newly added faces.</returns>
    /// <exception cref="System.ArgumentNullException"><c>path</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>path</c> is empty.</exception>
    /// <exception cref="System.IO.IOException">The directory or a font file cannot be read.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Directory or file access is denied.</exception>
    /// <exception cref="FontFormatException">A discovered font file is not a supported valid font.</exception>
    /// <exception cref="FontLimitException">A configured default resource limit is exceeded.</exception>
    public int AddDirectory(string path)
    {
        return AddDirectoryWithOptions(
            path,
            FontLoadOptions.CreateCompatible());
    }

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
    public int AddDirectoryWithOptions(string path, FontLoadOptions options)
    {
        ValidateDirectoryArguments(path, options);
        options.GetCancellationToken().ThrowIfCancellationRequested();
        string[] paths = System.IO.Directory.GetFiles(path);
        System.Array.Sort(paths, System.StringComparer.Ordinal);
        System.Collections.Generic.List<FontFace> discoveredFaces =
            new System.Collections.Generic.List<FontFace>();
        int pathIndex = 0;

        while (pathIndex < paths.Length)
        {
            options.GetCancellationToken().ThrowIfCancellationRequested();
            string fontPath = paths[pathIndex];

            if (IsSupportedFontPath(fontPath))
            {
                FontPackage package = FontLoader.LoadFile(fontPath, options);
                int faceIndex = 0;

                while (faceIndex < package.GetFaceCount())
                {
                    discoveredFaces.Add(package.GetFace(faceIndex));
                    faceIndex += 1;
                }
            }

            pathIndex += 1;
        }

        return AddFaces(discoveredFaces.ToArray());
    }

    int AddFaces(FontFace[] candidates)
    {
        string[] identities = new string[candidates.Length];
        int index = 0;

        while (index < candidates.Length)
        {
            identities[index] = FontSetFaceIdentity.Create(candidates[index]);
            index += 1;
        }

        int addedCount = 0;

        lock (syncRoot)
        {
            index = 0;

            while (index < candidates.Length)
            {
                string identity = identities[index];

                if (!ContainsEquivalent(identity, candidates[index]))
                {
                    faces.Add(candidates[index]);
                    AddToBucket(identity, candidates[index]);
                    addedCount += 1;
                }

                index += 1;
            }
        }

        return addedCount;
    }

    bool ContainsEquivalent(string identity, FontFace candidate)
    {
        if (!faceBuckets.ContainsKey(identity))
        {
            return false;
        }

        System.Collections.Generic.List<FontFace> bucket = faceBuckets[identity];
        int index = 0;

        while (index < bucket.Count)
        {
            if (FontSetFaceIdentity.AreEquivalent(bucket[index], candidate))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    void AddToBucket(string identity, FontFace face)
    {
        if (!faceBuckets.ContainsKey(identity))
        {
            faceBuckets.Add(identity, new System.Collections.Generic.List<FontFace>());
        }

        faceBuckets[identity].Add(face);
        families = null;
    }

    void EnsureFamilies()
    {
        if (families == null)
        {
            families = FontFamilyGrouper.Create(faces.ToArray());
        }
    }

    static void ValidateDirectoryArguments(string path, FontLoadOptions options)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (path.Length == 0)
        {
            throw new System.ArgumentException("A font directory path cannot be empty.", "path");
        }
    }

    static bool IsSupportedFontPath(string path)
    {
        string extension = System.IO.Path.GetExtension(path);
        return string.Equals(extension, ".ttf", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".otf", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".ttc", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".otc", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".woff", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".woff2", System.StringComparison.OrdinalIgnoreCase);
    }
}
