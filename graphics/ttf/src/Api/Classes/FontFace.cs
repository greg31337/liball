namespace ttf;

/// <summary>Represents one immutable face selected from a loaded font package.</summary>
public class FontFace : IFontFace
{
    int faceIndex;
    uint scalerType;
    SfntValidationResult validation;
    SfntTableCache tableCache;
    SfntVerticalMetrics? verticalMetrics;
    FontNames? names;
    FontClassification? classification;
    FontCoverage? coverage;
    FontCharacterMap? characterMap;
    FontGlyphCatalog? glyphCatalog;
    FontMetricInspection? metricInspection;
    FontVariations? variations;
    FontColorPalettes? colorPalettes;
    FontLayoutInspection? layoutInspection;
    FontAdvancedLayoutInspection? advancedLayoutInspection;
    FontMetrics? metrics;
    FontEmbeddingRights? embeddingRights;
    FontDescription? description;
    FontInstance? defaultInstance;

    internal FontFace(
        int faceIndex,
        uint scalerType,
        SfntValidationResult validation,
        SfntTableCache tableCache)
    {
        if (faceIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("faceIndex");
        }

        if (validation == null)
        {
            throw new System.ArgumentNullException("validation");
        }

        if (tableCache == null)
        {
            throw new System.ArgumentNullException("tableCache");
        }

        this.faceIndex = faceIndex;
        this.scalerType = scalerType;
        this.validation = validation;
        this.tableCache = tableCache;
    }

    /// <summary>Gets this face's zero-based package index.</summary>
    /// <returns>The face index.</returns>
    public int GetFaceIndex()
    {
        return faceIndex;
    }

    /// <summary>Gets the face's SFNT scaler type.</summary>
    /// <returns>The big-endian scaler type.</returns>
    public uint GetScalerType()
    {
        return scalerType;
    }

    /// <summary>Gets the validated outline technology.</summary>
    /// <returns>The outline kind, or unknown when it could not be established.</returns>
    public FontOutlineKind GetOutlineKind()
    {
        return validation.IsValid() ?
            validation.GetOutlineKind() : FontOutlineKind.Unknown;
    }

    /// <summary>Gets the count of bounded tables in this face.</summary>
    /// <returns>The bounded table count.</returns>
    public int GetTableCount()
    {
        return tableCache.GetRawTableCount();
    }

    /// <summary>Reports whether this face contains a bounded table.</summary>
    /// <param name="tag">The big-endian table tag.</param>
    /// <returns>True when the table is present; otherwise false.</returns>
    public bool HasTable(int tag)
    {
        return tableCache.FindRawTable(tag) != null;
    }

    /// <summary>Copies one preserved bounded table.</summary>
    /// <param name="tag">The big-endian table tag.</param>
    /// <returns>A new array containing the table bytes.</returns>
    /// <exception cref="System.ArgumentException">The table is not present.</exception>
    /// <exception cref="System.InvalidOperationException">
    /// Raw table preservation was disabled when the package was loaded.
    /// </exception>
    public byte[] CopyTable(int tag)
    {
        SfntTableData? table = tableCache.FindRawTable(tag);

        if (table == null)
        {
            throw new System.ArgumentException("The requested table is not present.", "tag");
        }

        return table.CopyRawData();
    }

    /// <summary>Gets descriptive results using deterministic default name selection.</summary>
    /// <returns>The cached description result.</returns>
    public FontDescription GetDescription()
    {
        EnsureSemanticAccess();

        lock (tableCache)
        {
            if (description == null)
            {
                description = new FontDescription(
                    faceIndex,
                    scalerType,
                    validation.GetOutlineKind(),
                    GetNames(),
                    GetClassification(),
                    GetCoverage(),
                    GetMetrics(),
                    GetEmbeddingRights());
            }
            return description;
        }
    }

    /// <summary>Gets descriptive results with names selected for a language.</summary>
    /// <param name="languageTag">The preferred BCP 47 language tag.</param>
    /// <returns>A description containing localized names.</returns>
    /// <exception cref="System.ArgumentNullException"><c>languageTag</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>languageTag</c> is invalid.</exception>
    public FontDescription GetLocalizedDescription(string languageTag)
    {
        EnsureSemanticAccess();
        return new FontDescription(
            faceIndex,
            scalerType,
            validation.GetOutlineKind(),
            GetLocalizedNames(languageTag),
            GetClassification(),
            GetCoverage(),
            GetMetrics(),
            GetEmbeddingRights());
    }

    /// <summary>Gets names using deterministic default language fallback.</summary>
    /// <returns>The cached names result.</returns>
    public FontNames GetNames()
    {
        EnsureSemanticAccess();

        lock (tableCache)
        {
            if (names == null) { names = CreateNames(null); }
            return names;
        }
    }

    /// <summary>Gets names selected for a preferred language.</summary>
    /// <param name="languageTag">The preferred BCP 47 language tag.</param>
    /// <returns>A localized names result.</returns>
    /// <exception cref="System.ArgumentNullException"><c>languageTag</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>languageTag</c> is invalid.</exception>
    public FontNames GetLocalizedNames(string languageTag)
    {
        EnsureSemanticAccess();
        SfntNameSelector.ValidateLanguageTag(languageTag);
        return CreateNames(languageTag);
    }

    /// <summary>Gets style, family, range, and optical classification metadata.</summary>
    /// <returns>The cached classification result.</returns>
    public FontClassification GetClassification()
    {
        EnsureSemanticAccess();

        lock (tableCache)
        {
            if (classification == null)
            {
                classification = new FontClassification(
                    GetOs2Table(),
                    GetHeadTable(),
                    GetPostTable(),
                    GetCmapMapping());
            }
            return classification;
        }
    }

    /// <summary>Gets merged Unicode coverage without expanding individual values.</summary>
    /// <returns>The cached coverage result.</returns>
    public FontCoverage GetCoverage()
    {
        EnsureSemanticAccess();

        lock (tableCache)
        {
            if (coverage == null)
            {
                SfntCmapMapping? mapping = GetCmapMapping();
                int count = mapping == null ? 0 : mapping.GetCoverageRangeCount();
                FontCoverageRange[] ranges = new FontCoverageRange[count];
                int index = 0;
                while (index < count)
                {
                    System.ArgumentNullException.ThrowIfNull(mapping);
                    SfntCmapCoverageRange range = mapping.GetCoverageRange(index);
                    ranges[index] = new FontCoverageRange(
                        range.GetStartUnicodeValue(),
                        range.GetEndUnicodeValue());
                    index += 1;
                }
                coverage = new FontCoverage(
                    ranges,
                    mapping != null && mapping.IsSymbolMapping());
            }
            return coverage;
        }
    }

    /// <summary>Gets the ranked Unicode and variation character mapping.</summary>
    /// <returns>The cached immutable character map.</returns>
    public FontCharacterMap GetCharacterMap()
    {
        EnsureSemanticAccess();

        lock (tableCache)
        {
            if (characterMap == null)
            {
                characterMap = new FontCharacterMap(this);
            }

            return characterMap;
        }
    }

    /// <summary>Gets glyph names, provenance, and Unicode identities.</summary>
    /// <returns>The cached immutable glyph catalog.</returns>
    /// <exception cref="FontFormatException">
    /// Required glyph-count or present glyph-name data is malformed or unsupported.
    /// </exception>
    public FontGlyphCatalog GetGlyphCatalog()
    {
        EnsureSemanticAccess();

        lock (tableCache)
        {
            if (glyphCatalog == null)
            {
                glyphCatalog = new FontGlyphCatalog(this);
            }

            return glyphCatalog;
        }
    }

    /// <summary>Gets BASE baseline and legacy metric-table inspection data.</summary>
    /// <returns>The cached immutable metric inspection.</returns>
    public FontMetricInspection GetMetricInspection()
    {
        EnsureSemanticAccess();

        lock (tableCache)
        {
            if (metricInspection == null)
            {
                metricInspection = new FontMetricInspection(this);
            }

            return metricInspection;
        }
    }

    /// <summary>Gets variable-font metadata using deterministic default name selection.</summary>
    /// <returns>The cached localized variation model.</returns>
    /// <exception cref="FontFormatException">
    /// A present variation metadata table is malformed or unsupported.
    /// </exception>
    public FontVariations GetVariations()
    {
        EnsureSemanticAccess();

        lock (tableCache)
        {
            if (variations == null)
            {
                variations = new FontVariations(this, GetNames(), null);
            }
            return variations;
        }
    }

    /// <summary>Gets variable-font metadata with names selected for a language.</summary>
    /// <param name="languageTag">The preferred BCP 47 language tag.</param>
    /// <returns>A localized variation model.</returns>
    /// <exception cref="System.ArgumentNullException"><c>languageTag</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>languageTag</c> is invalid.</exception>
    /// <exception cref="FontFormatException">
    /// A present variation metadata table is malformed or unsupported.
    /// </exception>
    public FontVariations GetLocalizedVariations(string languageTag)
    {
        EnsureSemanticAccess();
        FontNames localizedNames = GetLocalizedNames(languageTag);
        return new FontVariations(this, localizedNames, languageTag);
    }

    /// <summary>Gets CPAL palettes using deterministic default name selection.</summary>
    /// <returns>The cached palette model.</returns>
    /// <exception cref="FontFormatException">
    /// A present CPAL table is malformed or unsupported.
    /// </exception>
    public FontColorPalettes GetColorPalettes()
    {
        EnsureSemanticAccess();

        lock (tableCache)
        {
            if (colorPalettes == null)
            {
                colorPalettes = new FontColorPalettes(this, GetNames(), null);
            }
            return colorPalettes;
        }
    }

    /// <summary>Gets CPAL palettes with labels selected for a language.</summary>
    /// <param name="languageTag">The preferred BCP 47 language tag.</param>
    /// <returns>A localized palette model.</returns>
    /// <exception cref="System.ArgumentNullException"><c>languageTag</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>languageTag</c> is invalid.</exception>
    /// <exception cref="FontFormatException">
    /// A present CPAL table is malformed or unsupported.
    /// </exception>
    public FontColorPalettes GetLocalizedColorPalettes(string languageTag)
    {
        EnsureSemanticAccess();
        FontNames localizedNames = GetLocalizedNames(languageTag);
        return new FontColorPalettes(this, localizedNames, languageTag);
    }

    /// <summary>Gets public GSUB and GPOS layout discovery.</summary>
    /// <returns>The cached immutable layout inspection.</returns>
    /// <exception cref="FontFormatException">
    /// A present layout table is malformed or unsupported.
    /// </exception>
    public FontLayoutInspection GetLayoutInspection()
    {
        EnsureSemanticAccess();
        lock (tableCache)
        {
            if (layoutInspection == null)
            {
                layoutInspection = new FontLayoutInspection(this);
            }
            return layoutInspection;
        }
    }

    /// <summary>Gets read-only GDEF, BASE, JSTF, and MATH inspection.</summary>
    /// <returns>The cached immutable advanced-layout inspection.</returns>
    /// <exception cref="FontFormatException">
    /// A present advanced-layout table is malformed or unsupported.
    /// </exception>
    public FontAdvancedLayoutInspection GetAdvancedLayoutInspection()
    {
        EnsureSemanticAccess();
        lock (tableCache)
        {
            if (advancedLayoutInspection == null)
            {
                advancedLayoutInspection = new FontAdvancedLayoutInspection(this);
            }
            return advancedLayoutInspection;
        }
    }

    /// <summary>Gets validated unscaled metric-source metadata.</summary>
    /// <returns>The cached metric result.</returns>
    public FontMetrics GetMetrics()
    {
        EnsureSemanticAccess();

        lock (tableCache)
        {
            if (metrics == null)
            {
                metrics = new FontMetrics(
                    GetHeadTable(),
                    GetMaxpTable(),
                    GetHheaTable(),
                    GetVheaTable(),
                    GetOs2Table(),
                    GetPostTable());
            }
            return metrics;
        }
    }

    /// <summary>Selects global line metrics and returns all coordinates in design units.</summary>
    /// <param name="policy">The explicit line-metric selection policy.</param>
    /// <returns>The selected design-unit metrics.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>policy</c> is undefined.</exception>
    /// <exception cref="FontFormatException">A required metric source is unavailable.</exception>
    public FontDesignMetrics GetDesignMetrics(FontMetricSelectionPolicy policy)
    {
        EnsureSemanticAccess();
        return FontMetricResolver.CreateGlobal(this, policy);
    }

    /// <summary>Selects global line metrics and scales coordinates to a point size.</summary>
    /// <param name="policy">The explicit line-metric selection policy.</param>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>The scaled metrics.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>policy</c> is undefined or <c>pointSize</c> is negative or non-finite.
    /// </exception>
    /// <exception cref="FontFormatException">A required metric source is unavailable.</exception>
    public FontScaledMetrics GetScaledMetrics(
        FontMetricSelectionPolicy policy,
        double pointSize)
    {
        EnsureSemanticAccess();
        return GetDesignMetrics(policy).Scale(pointSize);
    }

    /// <summary>Gets one glyph's horizontal and vertical metrics in design units.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <param name="glyphYMaximum">
    /// The glyph ink maximum y coordinate used to resolve vertical origins and bearings.
    /// </param>
    /// <returns>The design-unit glyph metrics.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>glyphId</c> is outside the font.</exception>
    /// <exception cref="FontFormatException">Required metric tables are unavailable.</exception>
    public GlyphMetrics GetDesignGlyphMetrics(int glyphId, int glyphYMaximum)
    {
        EnsureSemanticAccess();
        return FontMetricResolver.CreateGlyph(this, glyphId, glyphYMaximum);
    }

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
    public ScaledGlyphMetrics GetScaledGlyphMetrics(
        int glyphId,
        int glyphYMaximum,
        double pointSize)
    {
        EnsureSemanticAccess();
        SfntHeadTable? head = GetHeadTable();
        if (head == null)
        {
            throw FontFormatException.Create("Scaled glyph metrics require a supported head table.");
        }
        return GetDesignGlyphMetrics(glyphId, glyphYMaximum).Scale(
            head.GetUnitsPerEm(),
            pointSize);
    }

    /// <summary>Creates or gets the default, unvaried font instance.</summary>
    /// <returns>The stable default instance for this face.</returns>
    /// <exception cref="FontFormatException">
    /// The face does not contain a supported glyph-count table.
    /// </exception>
    public FontInstance CreateDefaultInstance()
    {
        EnsureSemanticAccess();

        lock (tableCache)
        {
            if (defaultInstance == null)
            {
                FontCoordinates coordinates = new FontCoordinates();
                defaultInstance = new FontInstance(
                    this,
                    coordinates.Normalize(this));
            }

            return defaultInstance;
        }
    }

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
    public FontInstance CreateInstance(FontCoordinates coordinates)
    {
        EnsureSemanticAccess();

        if (coordinates == null)
        {
            throw new System.ArgumentNullException("coordinates");
        }

        NormalizedFontCoordinates normalized = coordinates.Normalize(this);

        if (normalized.IsDefaultUserLocation())
        {
            return CreateDefaultInstance();
        }

        return new FontInstance(this, normalized);
    }

    /// <summary>Gets the number of named variation instances.</summary>
    /// <returns>The <c>fvar</c> instance count, or zero for a static face.</returns>
    /// <exception cref="FontFormatException">
    /// The face contains an unsupported or malformed <c>fvar</c> table.
    /// </exception>
    public int GetNamedInstanceCount()
    {
        EnsureSemanticAccess();
        SfntFvarTable? table = GetFvarTable();

        if (table == null)
        {
            if (HasTable(FontTags.Fvar))
            {
                throw FontFormatException.Create(
                    "Named-instance selection requires a supported fvar table.");
            }

            return 0;
        }

        return table.GetInstanceCount();
    }

    /// <summary>Creates an instance at one named <c>fvar</c> location.</summary>
    /// <param name="index">The zero-based named-instance index.</param>
    /// <returns>The resolved named instance.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    /// <exception cref="FontFormatException">
    /// Required glyph-count or variation metadata is unavailable.
    /// </exception>
    public FontInstance CreateNamedInstance(int index)
    {
        EnsureSemanticAccess();
        SfntFvarTable? table = GetFvarTable();

        if (table == null || index < 0 || index >= table.GetInstanceCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        SfntFvarInstance selected = table.GetInstance(index);
        FontCoordinates coordinates = new FontCoordinates();
        int axisIndex = 0;

        while (axisIndex < table.GetAxisCount())
        {
            coordinates.Set(
                table.GetAxis(axisIndex).GetTag(),
                selected.GetCoordinate(axisIndex));
            axisIndex += 1;
        }

        NormalizedFontCoordinates normalized = coordinates.Normalize(this);

        if (normalized.GetNamedInstanceIndex() == index &&
            normalized.IsDefaultUserLocation())
        {
            return CreateDefaultInstance();
        }

        return new FontInstance(
            this,
            normalized.SelectNamedInstance(index));
    }

    /// <summary>Gets interpreted embedding rights without overriding restrictions.</summary>
    /// <returns>The cached rights result, or null when OS/2 is unavailable.</returns>
    public FontEmbeddingRights? GetEmbeddingRights()
    {
        EnsureSemanticAccess();

        lock (tableCache)
        {
            SfntOs2Table? table = GetOs2Table();
            if (table == null) { return null; }
            if (embeddingRights == null)
            {
                embeddingRights = new FontEmbeddingRights(table.GetEmbeddingRights());
            }
            return embeddingRights;
        }
    }

    /// <summary>Gets bounded metadata for a table by directory index.</summary>
    /// <param name="index">The zero-based table index.</param>
    /// <returns>The table result.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the table list.</exception>
    public FontTableInfo GetTableInfo(int index)
    {
        return new FontTableInfo(tableCache.GetRawTable(index), tableCache);
    }

    /// <summary>Finds bounded table metadata by tag.</summary>
    /// <param name="tag">The big-endian table tag.</param>
    /// <returns>The table result, or null when the table is absent.</returns>
    public FontTableInfo? FindTableInfo(int tag)
    {
        SfntTableData? table = tableCache.FindRawTable(tag);
        return table == null ? null : new FontTableInfo(table, tableCache);
    }

    /// <summary>Reports whether the face passed structural directory validation.</summary>
    /// <returns>True when semantic access and standalone emission are safe.</returns>
    public bool IsStructurallyValid()
    {
        return validation.IsValid();
    }

    /// <summary>Gets the standalone whole-font checksum validation result.</summary>
    /// <returns>The checksum-adjustment validation state.</returns>
    public FontChecksumValidationState GetChecksumValidationState()
    {
        return validation.GetChecksumValidationState();
    }

    /// <summary>Copies this selected face as a deterministic standalone SFNT font.</summary>
    /// <returns>A new array containing the standalone SFNT bytes.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The selected inspect-only face is not structurally valid.
    /// </exception>
    public byte[] CopyStandaloneSfntBytes()
    {
        return SfntFaceWriter.Write(this, tableCache.GetLimits());
    }

    void EnsureSemanticAccess()
    {
        if (!validation.IsValid())
        {
            throw new System.InvalidOperationException(
                "Semantic access is unavailable for a structurally invalid inspect-only face.");
        }
    }

    internal SfntTableStore GetTableStore()
    {
        return tableCache.GetRawTableStore();
    }

    internal SfntTableCache GetTableCache()
    {
        return tableCache;
    }

    internal SfntParsedTableView? GetHeadTableView()
    {
        return SfntHeadTableParser.FindView(tableCache);
    }

    internal SfntHeadTable? GetHeadTable()
    {
        return SfntHeadTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetMaxpTableView()
    {
        return SfntMaxpTableParser.FindView(tableCache);
    }

    internal SfntMaxpTable? GetMaxpTable()
    {
        SfntMaxpTable? table = SfntMaxpTableParser.FindTable(tableCache);

        if (table == null)
        {
            return null;
        }

        FontOutlineKind outlineKind = validation.GetOutlineKind();

        if (outlineKind == FontOutlineKind.TrueType &&
            table.GetKind() != SfntMaxpKind.TrueType)
        {
            throw FontFormatException.Create(
                "TrueType outlines require the version 1.0 maxp form.");
        }

        if ((outlineKind == FontOutlineKind.Cff1 ||
            outlineKind == FontOutlineKind.Cff2) &&
            table.GetKind() != SfntMaxpKind.Cff)
        {
            throw FontFormatException.Create(
                "CFF outlines require the version 0.5 maxp form.");
        }

        return table;
    }

    internal SfntParsedTableView? GetLocaTableView()
    {
        return SfntLocaTableParser.FindView(tableCache);
    }

    internal SfntLocaTable? GetLocaTable()
    {
        SfntLocaTable? table = SfntLocaTableParser.FindTable(tableCache);

        if (table != null)
        {
            GetHeadTable();
            GetMaxpTable();
        }

        return table;
    }

    internal SfntGlyfRecord GetGlyfRecord(int glyphId)
    {
        SfntLocaTable? table = GetLocaTable();

        if (table == null)
        {
            throw FontFormatException.Create(
                "TrueType glyph data requires a supported loca table.");
        }

        return table.GetGlyphRecord(glyphId);
    }

    internal SfntSimpleGlyph? GetSimpleGlyph(int glyphId)
    {
        SfntLocaTable? table = GetLocaTable();

        if (table == null)
        {
            throw FontFormatException.Create(
                "Simple TrueType glyph data requires a supported loca table.");
        }

        return table.GetSimpleGlyph(glyphId);
    }

    internal SfntGlyphOutline? GetSimpleGlyphOutline(int glyphId)
    {
        SfntSimpleGlyph? glyph = GetSimpleGlyph(glyphId);
        return glyph == null ? null : glyph.GetNormalizedOutline();
    }

    internal SfntCompositeGlyph? GetCompositeGlyph(int glyphId)
    {
        SfntLocaTable? table = GetLocaTable();

        if (table == null)
        {
            throw FontFormatException.Create(
                "Composite TrueType glyph data requires a supported loca table.");
        }

        return table.GetCompositeGlyph(glyphId);
    }

    internal SfntResolvedGlyph GetResolvedGlyph(int glyphId)
    {
        SfntLocaTable? table = GetLocaTable();

        if (table == null)
        {
            throw FontFormatException.Create(
                "Resolved TrueType glyph data requires a supported loca table.");
        }

        return table.GetResolvedGlyph(glyphId);
    }

    internal SfntBoundsComparison GetGlyphBoundsComparison(
        int glyphId,
        SfntBoundsPolicy policy)
    {
        SfntLocaTable? table = GetLocaTable();

        if (table == null)
        {
            throw FontFormatException.Create(
                "TrueType glyph bounds require a supported loca table.");
        }

        return table.GetGlyphBoundsComparison(glyphId, policy);
    }

    internal SfntBoundsComparison GetGlobalOutlineBoundsComparison(
        SfntBoundsPolicy policy)
    {
        SfntLocaTable? table = GetLocaTable();
        SfntHeadTable? head = GetHeadTable();

        if (table == null || head == null)
        {
            throw FontFormatException.Create(
                "TrueType global outline bounds require supported loca and head tables.");
        }

        return table.GetGlobalBoundsComparison(head, policy);
    }

    internal SfntParsedTableView? GetHheaTableView()
    {
        return SfntHheaTableParser.FindView(tableCache);
    }

    internal SfntHheaTable? GetHheaTable()
    {
        SfntHheaTable? table = SfntHheaTableParser.FindTable(tableCache);

        if (table == null)
        {
            return null;
        }

        SfntMaxpTable? maxp = GetMaxpTable();

        if (maxp != null &&
            table.GetMetricCount() > maxp.GetGlyphCount())
        {
            throw FontFormatException.Create(
                "The hhea horizontal metric count exceeds the maxp glyph count.");
        }

        return table;
    }

    internal SfntParsedTableView? GetHmtxTableView()
    {
        return SfntHmtxTableParser.FindView(tableCache);
    }

    internal SfntHmtxTable? GetHmtxTable()
    {
        SfntHmtxTable? table = SfntHmtxTableParser.FindTable(tableCache);

        if (table != null)
        {
            GetMaxpTable();
        }

        return table;
    }

    internal SfntParsedTableView? GetVheaTableView()
    {
        return SfntVheaTableParser.FindView(tableCache);
    }

    internal SfntVheaTable? GetVheaTable()
    {
        SfntVheaTable? table = SfntVheaTableParser.FindTable(tableCache);

        if (table == null)
        {
            return null;
        }

        SfntMaxpTable? maxp = GetMaxpTable();

        if (maxp != null &&
            table.GetMetricCount() > maxp.GetGlyphCount())
        {
            throw FontFormatException.Create(
                "The vhea vertical metric count exceeds the maxp glyph count.");
        }

        return table;
    }

    internal SfntParsedTableView? GetVmtxTableView()
    {
        return SfntVmtxTableParser.FindView(tableCache);
    }

    internal SfntVmtxTable? GetVmtxTable()
    {
        SfntVmtxTable? table = SfntVmtxTableParser.FindTable(tableCache);

        if (table != null)
        {
            GetMaxpTable();
        }

        return table;
    }

    internal SfntParsedTableView? GetVorgTableView()
    {
        return SfntVorgTableParser.FindView(tableCache);
    }

    internal SfntVorgTable? GetVorgTable()
    {
        return SfntVorgTableParser.FindTable(tableCache);
    }

    internal SfntVerticalMetrics GetVerticalMetrics()
    {
        lock (tableCache)
        {
            if (verticalMetrics == null)
            {
                GetHeadTable();
                GetMaxpTable();
                verticalMetrics = SfntVerticalMetricsResolver.Create(
                    tableCache,
                    validation.GetOutlineKind());
            }

            return verticalMetrics;
        }
    }

    internal SfntParsedTableView? GetOs2TableView()
    {
        return SfntOs2TableParser.FindView(tableCache);
    }

    internal SfntOs2Table? GetOs2Table()
    {
        SfntOs2Table? table = SfntOs2TableParser.FindTable(tableCache);

        if (table != null)
        {
            SfntHeadTable? head = GetHeadTable();

            if (head == null)
            {
                throw FontFormatException.Create(
                    "The OS/2 table requires a supported head table.");
            }

            table.VerifyHeadStyle(head);
        }

        return table;
    }

    internal SfntEmbeddingRightsReport GetSfntEmbeddingRights()
    {
        SfntOs2Table? table = GetOs2Table();

        if (table == null)
        {
            throw FontFormatException.Create(
                "Embedding rights require a supported OS/2 table.");
        }

        return table.GetEmbeddingRights();
    }

    FontNames CreateNames(string? languageTag)
    {
        SfntNameTable? table = GetNameTable();
        SfntPostScriptName? postScriptName = table == null
            ? null
            : table.GetPostScriptName();
        return new FontNames(
            table,
            languageTag,
            postScriptName == null ? null : postScriptName.GetNormalizedName());
    }

    internal SfntParsedTableView? GetNameTableView()
    {
        return SfntNameTableParser.FindView(tableCache);
    }

    internal SfntNameTable? GetNameTable()
    {
        return SfntNameTableParser.FindTable(tableCache);
    }

    internal SfntPostScriptName? GetPostScriptName()
    {
        SfntNameTable? nameTable = GetNameTable();
        return nameTable == null ? null : nameTable.GetPostScriptName();
    }

    internal SfntParsedTableView? GetCmapTableView()
    {
        return SfntCmapTableParser.FindView(tableCache);
    }

    internal SfntCmapTable? GetCmapTable()
    {
        return SfntCmapTableParser.FindTable(tableCache);
    }

    internal SfntCmapMapping? GetCmapMapping()
    {
        SfntCmapTable? table = GetCmapTable();
        return table == null ? null : table.GetMapping();
    }

    internal SfntParsedTableView? GetKernTableView()
    {
        return SfntKernTableParser.FindView(tableCache);
    }

    internal SfntKernTable? GetKernTable()
    {
        return SfntKernTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetBaseTableView()
    {
        return SfntBaseTableParser.FindView(tableCache);
    }

    internal SfntBaseTable? GetBaseTable()
    {
        return SfntBaseTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetJstfTableView()
    {
        return SfntJstfTableParser.FindView(tableCache);
    }

    internal SfntJstfTable? GetJstfTable()
    {
        return SfntJstfTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetMathTableView()
    {
        return SfntMathTableParser.FindView(tableCache);
    }

    internal SfntMathTable? GetMathTable()
    {
        return SfntMathTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetGdefTableView()
    {
        return SfntGdefTableParser.FindView(tableCache);
    }

    internal SfntGdefTable? GetGdefTable()
    {
        return SfntGdefTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetGsubTableView()
    {
        return SfntGsubTableParser.FindView(tableCache);
    }

    internal SfntGsubTable? GetGsubTable()
    {
        return SfntGsubTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetGposTableView()
    {
        return SfntGposTableParser.FindView(tableCache);
    }

    internal SfntGposTable? GetGposTable()
    {
        return SfntGposTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetFvarTableView()
    {
        return SfntFvarTableParser.FindView(tableCache);
    }

    internal SfntFvarTable? GetFvarTable()
    {
        return SfntFvarTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetGvarTableView()
    {
        return SfntGvarTableParser.FindView(tableCache);
    }

    internal SfntGvarTable? GetGvarTable()
    {
        return SfntGvarTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetCvarTableView()
    {
        return SfntCvarTableParser.FindView(tableCache);
    }

    internal SfntCvarTable? GetCvarTable()
    {
        return SfntCvarTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetHvarTableView()
    {
        return SfntMetricVariationsTableParser.FindHvarView(tableCache);
    }

    internal SfntMetricVariationsTable? GetHvarTable()
    {
        return SfntMetricVariationsTableParser.FindHvarTable(tableCache);
    }

    internal SfntParsedTableView? GetVvarTableView()
    {
        return SfntMetricVariationsTableParser.FindVvarView(tableCache);
    }

    internal SfntMetricVariationsTable? GetVvarTable()
    {
        return SfntMetricVariationsTableParser.FindVvarTable(tableCache);
    }

    internal SfntParsedTableView? GetMvarTableView()
    {
        return SfntMvarTableParser.FindView(tableCache);
    }

    internal SfntMvarTable? GetMvarTable()
    {
        return SfntMvarTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetAvarTableView()
    {
        return SfntAvarTableParser.FindView(tableCache);
    }

    internal SfntAvarTable? GetAvarTable()
    {
        return SfntAvarTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetStatTableView()
    {
        return SfntStatTableParser.FindView(tableCache);
    }

    internal SfntStatTable? GetStatTable()
    {
        return SfntStatTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetCpalTableView()
    {
        return SfntCpalTableParser.FindView(tableCache);
    }

    internal SfntCpalTable? GetCpalTable()
    {
        return SfntCpalTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetColrTableView()
    {
        return SfntColrTableParser.FindView(tableCache);
    }

    internal SfntColrTable? GetColrTable()
    {
        return SfntColrTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetGaspTableView()
    {
        return SfntGaspTableParser.FindView(tableCache);
    }

    internal SfntGaspTable? GetGaspTable()
    {
        return SfntGaspTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetHdmxTableView()
    {
        return SfntHdmxTableParser.FindView(tableCache);
    }

    internal SfntHdmxTable? GetHdmxTable()
    {
        return SfntHdmxTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetVdmxTableView()
    {
        return SfntVdmxTableParser.FindView(tableCache);
    }

    internal SfntVdmxTable? GetVdmxTable()
    {
        return SfntVdmxTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetLtshTableView()
    {
        return SfntLtshTableParser.FindView(tableCache);
    }

    internal SfntLtshTable? GetLtshTable()
    {
        return SfntLtshTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetMetaTableView()
    {
        return SfntMetaTableParser.FindView(tableCache);
    }

    internal SfntMetaTable? GetMetaTable()
    {
        return SfntMetaTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetPcltTableView()
    {
        return SfntPcltTableParser.FindView(tableCache);
    }

    internal SfntPcltTable? GetPcltTable()
    {
        return SfntPcltTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetMergTableView()
    {
        return SfntMergTableParser.FindView(tableCache);
    }

    internal SfntMergTable? GetMergTable()
    {
        return SfntMergTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetDsigTableView()
    {
        return SfntDsigTableParser.FindView(tableCache);
    }

    internal SfntDsigTable? GetDsigTable()
    {
        return SfntDsigTableParser.FindTable(tableCache);
    }

    internal int[] GetUnicodeCandidates(int glyphId)
    {
        SfntCmapMapping? mapping = GetCmapMapping();

        if (mapping == null)
        {
            return new int[0];
        }

        return mapping.GetUnicodeCandidates(glyphId);
    }

    internal SfntParsedTableView? GetPostTableView()
    {
        return SfntPostTableParser.FindView(tableCache);
    }

    internal SfntPostTable? GetPostTable()
    {
        return SfntPostTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetCffTableView()
    {
        return SfntCffTableParser.FindView(tableCache);
    }

    internal SfntCffTable? GetCffTable()
    {
        return SfntCffTableParser.FindTable(tableCache);
    }

    internal SfntParsedTableView? GetCff2TableView()
    {
        return SfntCff2TableParser.FindView(tableCache);
    }

    internal SfntCff2Table? GetCff2Table()
    {
        return SfntCff2TableParser.FindTable(tableCache);
    }

    internal int GetTableDiagnosticCount()
    {
        return tableCache.GetRawTableStore().GetDiagnosticCount();
    }

    internal int GetValidationDiagnosticCount()
    {
        return validation.GetDiagnosticCount();
    }

    internal FontDiagnostic GetValidationDiagnostic(int index)
    {
        return validation.GetDiagnostic(index);
    }

    internal FontDiagnostic GetTableDiagnostic(int index)
    {
        return tableCache.GetRawTableStore().GetDiagnostic(index);
    }
}
