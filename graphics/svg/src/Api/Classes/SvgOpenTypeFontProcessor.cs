namespace svg;

/// <summary>Inspects and instantiates the managed variable and color OpenType profile.</summary>
public class SvgOpenTypeFontProcessor : ISvgOpenTypeFontProcessor
{
    /// <summary>Creates a stateless processor.</summary>
    public SvgOpenTypeFontProcessor()
    {
    }

    /// <summary>Reads variation axes, palettes, and supported color glyph paints.</summary>
    /// <param name="font">A successfully parsed retained font.</param>
    /// <param name="options">The profile bounds and cancellation token.</param>
    /// <returns>The parsed profile or stable failure.</returns>
    public SvgOpenTypeFontProfile ReadProfileWithFontAndOptions(SvgFont font, SvgOpenTypeProfileOptions options)
    {
        SvgOpenTypeFontProfile result = new SvgOpenTypeFontProfile();
        if (!new SvgOpenTypeFontProcessor().ValidProfileArguments(font, options)) return new SvgOpenTypeFontProcessor().ProfileFailure(result, SvgOpenTypeInstanceStatus.InvalidArgument, "The font or profile options are invalid.", "", 0);
        SvgOpenTypeProcessorState state = new SvgOpenTypeProcessorState(font, options, result);
        if (options.CancellationToken.IsCancellationRequested) return new SvgOpenTypeFontProcessor().ProfileFailure(result, SvgOpenTypeInstanceStatus.Cancelled, "OpenType profile inspection was cancelled.", "", 0);
        if (!new SvgOpenTypeFontProcessor().ReadAxes(state)) return result;
        if (!new SvgOpenTypeFontProcessor().ReadPalettes(state)) return result;
        if (!new SvgOpenTypeFontProcessor().ReadColorGlyphs(state)) return result;
        if (result.RequiresColorCallback) return new SvgOpenTypeFontProcessor().ProfileFailure(result, SvgOpenTypeInstanceStatus.CallbackRequired, "COLR version 1 paint graphs require a caller color-font callback.", "COLR", result.UnsupportedOperator);
        result.Status = SvgOpenTypeInstanceStatus.Success;
        result.Success = true;
        return result;
    }

    /// <summary>Instantiates one glyph at explicit variation coordinates and palette.</summary>
    /// <param name="font">A successfully parsed retained font.</param>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <param name="options">The coordinates, output size, palette, and bounds.</param>
    /// <returns>The instantiated glyph or precise callback requirement.</returns>
    public SvgOpenTypeGlyphInstance InstantiateGlyphWithFontAndGlyphIndexAndOptions(SvgFont font, int glyphIndex, SvgOpenTypeInstanceOptions options)
    {
        SvgOpenTypeGlyphInstance result = new SvgOpenTypeGlyphInstance(glyphIndex);
        if (!new SvgOpenTypeFontProcessor().ValidInstanceArguments(font, glyphIndex, options)) return new SvgOpenTypeFontProcessor().InstanceFailure(result, SvgOpenTypeInstanceStatus.InvalidArgument, "The font, glyph index, or instance options are invalid.", "", 0);
        if (options.CancellationToken.IsCancellationRequested) return new SvgOpenTypeFontProcessor().InstanceFailure(result, SvgOpenTypeInstanceStatus.Cancelled, "OpenType glyph instantiation was cancelled.", "", 0);
        SvgOpenTypeFontProfile profile = new SvgOpenTypeFontProcessor().ReadProfileWithFontAndOptions(font, options.ProfileOptions);
        if (profile.Status != SvgOpenTypeInstanceStatus.Success && profile.Status != SvgOpenTypeInstanceStatus.CallbackRequired) return new SvgOpenTypeFontProcessor().InstanceFailure(result, profile.Status, "OpenType profile inspection failed.", profile.RequiredTableTag, profile.UnsupportedOperator);
        SvgOpenTypeCoordinateState? coordinates = new SvgOpenTypeFontProcessor().ResolveCoordinates(font, profile, options, result);
        if (coordinates == null) return result;
        result.Coordinates = coordinates.Coordinates;
        result.Metrics = new SvgOpenTypeFontProcessor().Metrics(font, glyphIndex, options.EmSize);
        if (result.Metrics == null) return new SvgOpenTypeFontProcessor().InstanceFailure(result, SvgOpenTypeInstanceStatus.InvalidFont, "The base glyph metrics are inconsistent.", "hmtx", 0);
        if (!new SvgOpenTypeFontProcessor().ApplyMetricVariations(font, glyphIndex, coordinates.Values, options.EmSize, result)) return result;
        if (font.OutlineKind == SvgOpenTypeOutlineKind.TrueType)
        {
            SvgTrueTypeGlyphResult decoded = new SvgTrueTypeGlyphDecoder().DecodeWithFontAndGlyphIndexAndOptions(font, glyphIndex, options.TrueTypeOptions);
            if (!decoded.Success || decoded.Outline == null) return new SvgOpenTypeFontProcessor().InstanceFailure(result, SvgOpenTypeInstanceStatus.InvalidFont, "The TrueType glyph outline could not be decoded.", "glyf", 0);
            result.TrueTypeOutline = decoded.Outline;
            if (!new SvgOpenTypeFontProcessor().ApplyTrueTypeVariations(font, glyphIndex, coordinates.Values, options, result)) return result;
        }
        else
        {
            SvgCffOutlineOptions cffOptions = new SvgCffOutlineOptions();
            cffOptions.Limits = options.CffOptions.Limits;
            cffOptions.MaximumOperands = options.CffOptions.MaximumOperands;
            cffOptions.MaximumSubroutineDepth = options.CffOptions.MaximumSubroutineDepth;
            cffOptions.MaximumSubroutineCalls = options.CffOptions.MaximumSubroutineCalls;
            cffOptions.MaximumSubroutines = options.CffOptions.MaximumSubroutines;
            cffOptions.VariationCoordinates = new System.Collections.Generic.List<double>(coordinates.Values);
            SvgCffGlyphResult decoded = new SvgCffOutlineDecoder().DecodeWithFontAndGlyphIndexAndOptions(font, glyphIndex, cffOptions);
            if (decoded.RequiresFallback) return new SvgOpenTypeFontProcessor().InstanceFailure(result, SvgOpenTypeInstanceStatus.CallbackRequired, "The CFF glyph uses a valid structure or operator outside the managed profile.", font.OutlineKind == SvgOpenTypeOutlineKind.Cff2 ? "CFF2" : "CFF ", decoded.UnsupportedOperator);
            if (!decoded.Success || decoded.Outline == null) return new SvgOpenTypeFontProcessor().InstanceFailure(result, SvgOpenTypeInstanceStatus.InvalidFont, "The CFF glyph outline could not be decoded.", font.OutlineKind == SvgOpenTypeOutlineKind.Cff2 ? "CFF2" : "CFF ", decoded.UnsupportedOperator);
            result.CffOutline = decoded.Outline;
        }
        bool hasColorGlyph = new SvgOpenTypeFontProcessor().HasColorGlyph(profile, glyphIndex);
        if (hasColorGlyph && options.PaletteIndex >= profile.Palettes.Count) return new SvgOpenTypeFontProcessor().InstanceFailure(result, SvgOpenTypeInstanceStatus.InvalidArgument, "The requested CPAL palette is unavailable.", "CPAL", 0);
        result.ColorGlyph = new SvgOpenTypeFontProcessor().SelectColorGlyph(profile, glyphIndex, options.PaletteIndex);
        if (result.ColorGlyph == null && profile.RequiresColorCallback) return new SvgOpenTypeFontProcessor().InstanceFailure(result, SvgOpenTypeInstanceStatus.CallbackRequired, "The glyph may require an unsupported COLR version 1 paint graph.", "COLR", profile.UnsupportedOperator);
        string unsupported = new SvgOpenTypeFontProcessor().UnsupportedColorTable(font, result.ColorGlyph != null);
        if (unsupported.Length != 0) return new SvgOpenTypeFontProcessor().InstanceFailure(result, SvgOpenTypeInstanceStatus.CallbackRequired, "The glyph color format requires a caller color-font callback.", unsupported, 0);
        result.Status = SvgOpenTypeInstanceStatus.Success;
        result.Success = true;
        return result;
    }

    bool ReadAxes(SvgOpenTypeProcessorState state)
    {
        SvgOpenTypeTable? table = state.Font.FindTable("fvar");
        if (table == null) return true;
        if (!new SvgOpenTypeFontProcessor().TableRange(state.Font, table) || table.Length < 16) return new SvgOpenTypeFontProcessor().ProfileInvalid(state, "The fvar table is truncated.", "fvar");
        byte[] bytes = (state.Font.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        int start = table.Offset;
        int major = new SvgOpenTypeFontProcessor().UInt16(bytes, start);
        int minor = new SvgOpenTypeFontProcessor().UInt16(bytes, start + 2);
        int axesOffset = new SvgOpenTypeFontProcessor().UInt16(bytes, start + 4);
        int axisCount = new SvgOpenTypeFontProcessor().UInt16(bytes, start + 8);
        int axisSize = new SvgOpenTypeFontProcessor().UInt16(bytes, start + 10);
        int instanceCount = new SvgOpenTypeFontProcessor().UInt16(bytes, start + 12);
        int instanceSize = new SvgOpenTypeFontProcessor().UInt16(bytes, start + 14);
        if (major != 1 || minor != 0 || axisSize < 20) return new SvgOpenTypeFontProcessor().ProfileInvalid(state, "The fvar header is invalid.", "fvar");
        if (axisCount > state.Options.Limits.MaxVariationAxes) return new SvgOpenTypeFontProcessor().ProfileLimited(state, "The fvar axis count exceeds the configured bound.", "fvar");
        long axisEnd = (long)axesOffset + (long)axisCount * axisSize;
        long instancesEnd = axisEnd + (long)instanceCount * instanceSize;
        if (axesOffset < 16 || axisEnd > table.Length || instanceSize < axisCount * 4 + 4 || instancesEnd > table.Length) return new SvgOpenTypeFontProcessor().ProfileInvalid(state, "The fvar axis or instance records are outside the table.", "fvar");
        int index = 0;
        while (index < axisCount)
        {
            if (state.Options.CancellationToken.IsCancellationRequested) return new SvgOpenTypeFontProcessor().ProfileCancelled(state);
            int offset = start + axesOffset + index * axisSize;
            SvgOpenTypeVariationAxis axis = new SvgOpenTypeVariationAxis();
            axis.Tag = new SvgOpenTypeFontProcessor().Tag(bytes, offset);
            axis.Minimum = new SvgOpenTypeFontProcessor().Fixed(bytes, offset + 4);
            axis.Default = new SvgOpenTypeFontProcessor().Fixed(bytes, offset + 8);
            axis.Maximum = new SvgOpenTypeFontProcessor().Fixed(bytes, offset + 12);
            axis.Flags = new SvgOpenTypeFontProcessor().UInt16(bytes, offset + 16);
            axis.NameIdentifier = new SvgOpenTypeFontProcessor().UInt16(bytes, offset + 18);
            axis.Hidden = (axis.Flags & 1) != 0;
            bool duplicate = false;
            int existing = 0;
            while (existing < state.Profile.Axes.Count)
            {
                if (state.Profile.Axes[existing].Tag == axis.Tag) duplicate = true;
                existing = existing + 1;
            }
            if (!new SvgOpenTypeFontProcessor().ValidTag(axis.Tag) || duplicate || axis.Minimum > axis.Default || axis.Default > axis.Maximum || (axis.Flags & 65534) != 0) return new SvgOpenTypeFontProcessor().ProfileInvalid(state, "An fvar axis record is invalid.", "fvar");
            state.Profile.Axes.Add(axis);
            index = index + 1;
        }
        return true;
    }

    bool ReadPalettes(SvgOpenTypeProcessorState state)
    {
        SvgOpenTypeTable? table = state.Font.FindTable("CPAL");
        if (table == null) return true;
        if (!new SvgOpenTypeFontProcessor().TableRange(state.Font, table) || table.Length < 12) return new SvgOpenTypeFontProcessor().ProfileInvalid(state, "The CPAL table is truncated.", "CPAL");
        byte[] bytes = (state.Font.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        int start = table.Offset;
        int version = new SvgOpenTypeFontProcessor().UInt16(bytes, start);
        int entries = new SvgOpenTypeFontProcessor().UInt16(bytes, start + 2);
        int palettes = new SvgOpenTypeFontProcessor().UInt16(bytes, start + 4);
        int records = new SvgOpenTypeFontProcessor().UInt16(bytes, start + 6);
        uint recordOffsetValue = new SvgOpenTypeFontProcessor().UInt32(bytes, start + 8);
        if (version > 1 || entries < 1 || palettes < 1 || recordOffsetValue > System.Int32.MaxValue) return new SvgOpenTypeFontProcessor().ProfileInvalid(state, "The CPAL header is invalid.", "CPAL");
        if (palettes > state.Options.MaximumPalettes || (long)entries * palettes > state.Options.MaximumPaletteColors || records > state.Options.MaximumPaletteColors) return new SvgOpenTypeFontProcessor().ProfileLimited(state, "The CPAL data exceeds the configured palette bounds.", "CPAL");
        int recordOffset = (int)recordOffsetValue;
        if ((long)12 + palettes * 2 > table.Length || (long)recordOffset + records * 4 > table.Length) return new SvgOpenTypeFontProcessor().ProfileInvalid(state, "The CPAL palette indexes or color records are outside the table.", "CPAL");
        int palette = 0;
        while (palette < palettes)
        {
            if (state.Options.CancellationToken.IsCancellationRequested) return new SvgOpenTypeFontProcessor().ProfileCancelled(state);
            int first = new SvgOpenTypeFontProcessor().UInt16(bytes, start + 12 + palette * 2);
            if ((long)first + entries > records) return new SvgOpenTypeFontProcessor().ProfileInvalid(state, "A CPAL palette range is invalid.", "CPAL");
            SvgOpenTypeColorPalette output = new SvgOpenTypeColorPalette(palette);
            int entry = 0;
            while (entry < entries)
            {
                int offset = start + recordOffset + (first + entry) * 4;
                output.Colors.Add(new SvgOpenTypePaletteColor(bytes[offset + 2], bytes[offset + 1], bytes[offset], bytes[offset + 3]));
                entry = entry + 1;
            }
            state.Profile.Palettes.Add(output);
            palette = palette + 1;
        }
        return true;
    }

    bool ReadColorGlyphs(SvgOpenTypeProcessorState state)
    {
        SvgOpenTypeTable? table = state.Font.FindTable("COLR");
        if (table == null) return true;
        if (!new SvgOpenTypeFontProcessor().TableRange(state.Font, table) || table.Length < 14) return new SvgOpenTypeFontProcessor().ProfileInvalid(state, "The COLR table is truncated.", "COLR");
        byte[] bytes = (state.Font.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        int start = table.Offset;
        int version = new SvgOpenTypeFontProcessor().UInt16(bytes, start);
        int baseCount = new SvgOpenTypeFontProcessor().UInt16(bytes, start + 2);
        uint baseOffsetValue = new SvgOpenTypeFontProcessor().UInt32(bytes, start + 4);
        uint layerOffsetValue = new SvgOpenTypeFontProcessor().UInt32(bytes, start + 8);
        int layerCount = new SvgOpenTypeFontProcessor().UInt16(bytes, start + 12);
        if (version > 1 || baseOffsetValue > System.Int32.MaxValue || layerOffsetValue > System.Int32.MaxValue) return new SvgOpenTypeFontProcessor().ProfileInvalid(state, "The COLR header is invalid.", "COLR");
        if (baseCount > state.Options.MaximumColorGlyphs || layerCount > state.Options.MaximumColorLayers) return new SvgOpenTypeFontProcessor().ProfileLimited(state, "The COLR data exceeds the configured glyph or layer bounds.", "COLR");
        int baseOffset = (int)baseOffsetValue;
        int layerOffset = (int)layerOffsetValue;
        if ((long)baseOffset + baseCount * 6 > table.Length || (long)layerOffset + layerCount * 4 > table.Length) return new SvgOpenTypeFontProcessor().ProfileInvalid(state, "The COLR v0 records are outside the table.", "COLR");
        state.Profile.ColorVersion = version;
        int baseIndex = 0;
        int preceding = -1;
        while (baseIndex < baseCount)
        {
            if (state.Options.CancellationToken.IsCancellationRequested) return new SvgOpenTypeFontProcessor().ProfileCancelled(state);
            int offset = start + baseOffset + baseIndex * 6;
            int glyph = new SvgOpenTypeFontProcessor().UInt16(bytes, offset);
            int first = new SvgOpenTypeFontProcessor().UInt16(bytes, offset + 2);
            int count = new SvgOpenTypeFontProcessor().UInt16(bytes, offset + 4);
            if (glyph <= preceding || (long)first + count > layerCount) return new SvgOpenTypeFontProcessor().ProfileInvalid(state, "A COLR base glyph record is invalid.", "COLR");
            SvgOpenTypeColorGlyph colorGlyph = new SvgOpenTypeColorGlyph(glyph);
            int layerIndex = 0;
            while (layerIndex < count)
            {
                int layerRecord = start + layerOffset + (first + layerIndex) * 4;
                SvgOpenTypeColorGlyphLayer layer = new SvgOpenTypeColorGlyphLayer();
                layer.GlyphIndex = new SvgOpenTypeFontProcessor().UInt16(bytes, layerRecord);
                layer.PaletteEntryIndex = new SvgOpenTypeFontProcessor().UInt16(bytes, layerRecord + 2);
                if (layer.PaletteEntryIndex != 65535 && (state.Profile.Palettes.Count == 0 || layer.PaletteEntryIndex >= state.Profile.Palettes[0].Colors.Count)) return new SvgOpenTypeFontProcessor().ProfileInvalid(state, "A COLR layer references an unavailable CPAL entry.", "COLR");
                colorGlyph.Layers.Add(layer);
                layerIndex = layerIndex + 1;
            }
            state.Profile.ColorGlyphs.Add(colorGlyph);
            preceding = glyph;
            baseIndex = baseIndex + 1;
        }
        if (version == 1)
        {
            if (table.Length < 34) return new SvgOpenTypeFontProcessor().ProfileInvalid(state, "The COLR version 1 header is truncated.", "COLR");
            uint baseGlyphListOffset = new SvgOpenTypeFontProcessor().UInt32(bytes, start + 14);
            uint layerListOffset = new SvgOpenTypeFontProcessor().UInt32(bytes, start + 18);
            uint clipListOffset = new SvgOpenTypeFontProcessor().UInt32(bytes, start + 22);
            uint variationMapOffset = new SvgOpenTypeFontProcessor().UInt32(bytes, start + 26);
            uint variationStoreOffset = new SvgOpenTypeFontProcessor().UInt32(bytes, start + 30);
            if (baseGlyphListOffset != 0 || layerListOffset != 0 || clipListOffset != 0 || variationMapOffset != 0 || variationStoreOffset != 0)
            {
                state.Profile.RequiresColorCallback = true;
                state.Profile.UnsupportedOperator = 1;
            }
        }
        return true;
    }

    SvgOpenTypeCoordinateState? ResolveCoordinates(SvgFont font, SvgOpenTypeFontProfile profile, SvgOpenTypeInstanceOptions options, SvgOpenTypeGlyphInstance result)
    {
        if (options.Coordinates.Count > profile.Axes.Count) return new SvgOpenTypeFontProcessor().CoordinateFailure(result, "More variation coordinates were supplied than the font defines.");
        SvgOpenTypeCoordinateState state = new SvgOpenTypeCoordinateState(profile.Axes.Count);
        int axisIndex = 0;
        while (axisIndex < profile.Axes.Count)
        {
            SvgOpenTypeVariationAxis axis = profile.Axes[axisIndex];
            double user = axis.Default;
            int found = -1;
            int input = 0;
            while (input < options.Coordinates.Count)
            {
                SvgOpenTypeVariationCoordinate coordinate = options.Coordinates[input];
                if (coordinate == null || coordinate.Tag == null || coordinate.Tag.Length != 4 || !SvgFiniteMath.IsFinite(coordinate.Value)) return new SvgOpenTypeFontProcessor().CoordinateFailure(result, "A variation coordinate is invalid.");
                if (coordinate.Tag == axis.Tag)
                {
                    if (found >= 0) return new SvgOpenTypeFontProcessor().CoordinateFailure(result, "A variation axis coordinate was duplicated.");
                    found = input;
                    user = coordinate.Value;
                }
                input = input + 1;
            }
            if (user < axis.Minimum) user = axis.Minimum;
            if (user > axis.Maximum) user = axis.Maximum;
            double normalized = user == axis.Default ? 0 : user < axis.Default ? axis.Default == axis.Minimum ? 0 : (user - axis.Default) / (axis.Default - axis.Minimum) : axis.Maximum == axis.Default ? 0 : (user - axis.Default) / (axis.Maximum - axis.Default);
            SvgOpenTypeNormalizedCoordinate output = new SvgOpenTypeNormalizedCoordinate();
            output.Tag = axis.Tag;
            output.UserValue = user;
            output.NormalizedValue = normalized;
            state.Coordinates.Add(output);
            state.Values[axisIndex] = normalized;
            axisIndex = axisIndex + 1;
        }
        int supplied = 0;
        while (supplied < options.Coordinates.Count)
        {
            bool known = false;
            axisIndex = 0;
            while (axisIndex < profile.Axes.Count)
            {
                if (options.Coordinates[supplied].Tag == profile.Axes[axisIndex].Tag) known = true;
                axisIndex = axisIndex + 1;
            }
            if (!known) return new SvgOpenTypeFontProcessor().CoordinateFailure(result, "A variation coordinate names an unknown axis.");
            supplied = supplied + 1;
        }
        if (!new SvgOpenTypeFontProcessor().ApplyAvar(font, state.Values)) return new SvgOpenTypeFontProcessor().CoordinateFailure(result, "The avar segment maps are malformed.");
        axisIndex = 0;
        while (axisIndex < state.Coordinates.Count)
        {
            state.Coordinates[axisIndex].NormalizedValue = state.Values[axisIndex];
            axisIndex = axisIndex + 1;
        }
        return state;
    }

    bool ApplyAvar(SvgFont font, double[] coordinates)
    {
        SvgOpenTypeTable? table = font.FindTable("avar");
        if (table == null) return true;
        if (!new SvgOpenTypeFontProcessor().TableRange(font, table) || table.Length < 8) return false;
        byte[] bytes = (font.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        int position = table.Offset;
        if (new SvgOpenTypeFontProcessor().UInt16(bytes, position) != 1 || new SvgOpenTypeFontProcessor().UInt16(bytes, position + 2) != 0 || new SvgOpenTypeFontProcessor().UInt16(bytes, position + 6) != coordinates.Length) return false;
        position = position + 8;
        int axis = 0;
        while (axis < coordinates.Length)
        {
            if (position + 2 > table.Offset + table.Length) return false;
            int count = new SvgOpenTypeFontProcessor().UInt16(bytes, position);
            position = position + 2;
            if (count < 3 || (long)position + count * 4 > table.Offset + table.Length) return false;
            double value = coordinates[axis];
            double previousFrom = -2;
            double previousTo = -2;
            int segment = 0;
            bool mapped = false;
            while (segment < count)
            {
                double from = new SvgOpenTypeFontProcessor().F2Dot14(bytes, position + segment * 4);
                double to = new SvgOpenTypeFontProcessor().F2Dot14(bytes, position + segment * 4 + 2);
                if (from <= previousFrom || to < -1 || to > 1) return false;
                if (!mapped && value <= from)
                {
                    if (segment == 0) coordinates[axis] = to;
                    else coordinates[axis] = previousFrom == from ? previousTo : previousTo + (value - previousFrom) * (to - previousTo) / (from - previousFrom);
                    mapped = true;
                }
                previousFrom = from;
                previousTo = to;
                segment = segment + 1;
            }
            if (!mapped) coordinates[axis] = previousTo;
            position = position + count * 4;
            axis = axis + 1;
        }
        return position <= table.Offset + table.Length;
    }

    SvgOpenTypeInstantiatedMetrics? Metrics(SvgFont font, int glyphIndex, double emSize)
    {
        if (font.Head == null || font.HorizontalMetrics == null || glyphIndex < 0 || glyphIndex >= font.HorizontalMetrics.Count) return null;
        SvgOpenTypeHorizontalMetric horizontal = font.HorizontalMetrics[glyphIndex];
        if (horizontal == null || font.Head.UnitsPerEm < 1) return null;
        SvgOpenTypeInstantiatedMetrics result = new SvgOpenTypeInstantiatedMetrics();
        result.AdvanceWidth = horizontal.AdvanceWidth;
        result.LeftSideBearing = horizontal.LeftSideBearing;
        if (font.VerticalMetrics != null && glyphIndex < font.VerticalMetrics.Count && font.VerticalMetrics[glyphIndex] != null)
        {
            result.HasVerticalMetrics = true;
            result.AdvanceHeight = font.VerticalMetrics[glyphIndex].AdvanceHeight;
            result.TopSideBearing = font.VerticalMetrics[glyphIndex].TopSideBearing;
        }
        new SvgOpenTypeFontProcessor().ScaleMetrics(result, emSize / font.Head.UnitsPerEm);
        return result;
    }

    void ScaleMetrics(SvgOpenTypeInstantiatedMetrics metrics, double scale)
    {
        metrics.ScaledAdvanceWidth = metrics.AdvanceWidth * scale;
        metrics.ScaledLeftSideBearing = metrics.LeftSideBearing * scale;
        metrics.ScaledAdvanceHeight = metrics.AdvanceHeight * scale;
        metrics.ScaledTopSideBearing = metrics.TopSideBearing * scale;
    }

    bool ApplyMetricVariations(SvgFont font, int glyphIndex, double[] coordinates, double emSize, SvgOpenTypeGlyphInstance result)
    {
        string[] tags = new string[] { "HVAR", "VVAR" };
        int index = 0;
        while (index < tags.Length)
        {
            SvgOpenTypeTable? table = font.FindTable(tags[index]);
            if (table != null)
            {
                SvgItemVariationMetrics? deltas = new SvgItemVariationEvaluator().EvaluateMetrics(font, table, glyphIndex, coordinates);
                if (deltas == null) return new SvgOpenTypeFontProcessor().InstanceFailureBoolean(result, SvgOpenTypeInstanceStatus.CallbackRequired, "The metric variation table uses a structure outside the managed profile.", tags[index], 0);
                if (tags[index] == "HVAR")
                {
                    (result.Metrics ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).AdvanceWidth = result.Metrics.AdvanceWidth + deltas.Advance;
                    result.Metrics.LeftSideBearing = result.Metrics.LeftSideBearing + deltas.Bearing;
                }
                else if ((result.Metrics ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).HasVerticalMetrics)
                {
                    result.Metrics.AdvanceHeight = result.Metrics.AdvanceHeight + deltas.Advance;
                    result.Metrics.TopSideBearing = result.Metrics.TopSideBearing + deltas.Bearing;
                }
            }
            index = index + 1;
        }
        new SvgOpenTypeFontProcessor().ScaleMetrics((result.Metrics ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), emSize / (font.Head ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).UnitsPerEm);
        return true;
    }

    bool ApplyTrueTypeVariations(SvgFont font, int glyphIndex, double[] coordinates, SvgOpenTypeInstanceOptions options, SvgOpenTypeGlyphInstance result)
    {
        SvgOpenTypeTable? table = font.FindTable("gvar");
        if (table == null) return true;
        SvgTrueTypeGlyphOutline outline = (result.TrueTypeOutline ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        SvgGvarApplyResult applied = new SvgGvarEvaluator().Apply(font, table, glyphIndex, coordinates, outline, options.TrueTypeOptions);
        if (applied == SvgGvarApplyResult.Success)
        {
            new SvgOpenTypeFontProcessor().UpdateTrueTypeBoundsAndMetrics(font, outline, options.EmSize, result);
            return true;
        }
        if (applied == SvgGvarApplyResult.LimitExceeded) return new SvgOpenTypeFontProcessor().InstanceFailureBoolean(result, SvgOpenTypeInstanceStatus.LimitExceeded, "The gvar outline exceeds configured bounds.", "gvar", 0);
        if (applied == SvgGvarApplyResult.CallbackRequired) return new SvgOpenTypeFontProcessor().InstanceFailureBoolean(result, SvgOpenTypeInstanceStatus.CallbackRequired, "The gvar tuple requires interpolation or composite processing outside the managed profile.", "gvar", 0);
        return new SvgOpenTypeFontProcessor().InstanceFailureBoolean(result, SvgOpenTypeInstanceStatus.InvalidFont, "The gvar table is malformed.", "gvar", 0);
    }

    void UpdateTrueTypeBoundsAndMetrics(SvgFont font, SvgTrueTypeGlyphOutline outline, double emSize, SvgOpenTypeGlyphInstance result)
    {
        if (outline.Points.Count > 0)
        {
            double minimumX = outline.Points[0].X;
            double maximumX = outline.Points[0].X;
            double minimumY = outline.Points[0].Y;
            double maximumY = outline.Points[0].Y;
            int index = 1;
            while (index < outline.Points.Count)
            {
                SvgTrueTypeGlyphPoint point = outline.Points[index];
                if (point.X < minimumX) minimumX = point.X;
                if (point.X > maximumX) maximumX = point.X;
                if (point.Y < minimumY) minimumY = point.Y;
                if (point.Y > maximumY) maximumY = point.Y;
                index = index + 1;
            }
            outline.XMin = (int)System.Math.Floor(minimumX);
            outline.XMax = (int)System.Math.Ceiling(maximumX);
            outline.YMin = (int)System.Math.Floor(minimumY);
            outline.YMax = (int)System.Math.Ceiling(maximumY);
        }
        SvgOpenTypeInstantiatedMetrics metrics = (result.Metrics ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        if (font.FindTable("HVAR") == null && outline.PhantomPoints.Count >= 2)
        {
            metrics.AdvanceWidth = outline.PhantomPoints[1].X - outline.PhantomPoints[0].X;
            metrics.LeftSideBearing = outline.XMin - outline.PhantomPoints[0].X;
        }
        if (font.FindTable("VVAR") == null && metrics.HasVerticalMetrics && outline.PhantomPoints.Count >= 4)
        {
            metrics.AdvanceHeight = outline.PhantomPoints[2].Y - outline.PhantomPoints[3].Y;
            metrics.TopSideBearing = outline.PhantomPoints[2].Y - outline.YMax;
        }
        new SvgOpenTypeFontProcessor().ScaleMetrics(metrics, emSize / (font.Head ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).UnitsPerEm);
    }

    SvgOpenTypeColorGlyph? SelectColorGlyph(SvgOpenTypeFontProfile profile, int glyphIndex, int paletteIndex)
    {
        SvgOpenTypeColorGlyph? source = null;
        int index = 0;
        while (index < profile.ColorGlyphs.Count)
        {
            if (profile.ColorGlyphs[index].GlyphIndex == glyphIndex) source = profile.ColorGlyphs[index];
            index = index + 1;
        }
        if (source == null) return null;
        SvgOpenTypeColorGlyph result = new SvgOpenTypeColorGlyph(glyphIndex);
        SvgOpenTypeColorPalette? palette = paletteIndex >= 0 && paletteIndex < profile.Palettes.Count ? profile.Palettes[paletteIndex] : null;
        index = 0;
        while (index < source.Layers.Count)
        {
            SvgOpenTypeColorGlyphLayer layer = new SvgOpenTypeColorGlyphLayer();
            layer.GlyphIndex = source.Layers[index].GlyphIndex;
            layer.PaletteEntryIndex = source.Layers[index].PaletteEntryIndex;
            if (layer.PaletteEntryIndex != 65535 && palette != null && layer.PaletteEntryIndex < palette.Colors.Count) layer.Color = palette.Colors[layer.PaletteEntryIndex];
            result.Layers.Add(layer);
            index = index + 1;
        }
        return result;
    }

    bool HasColorGlyph(SvgOpenTypeFontProfile profile, int glyphIndex)
    {
        int index = 0;
        while (index < profile.ColorGlyphs.Count)
        {
            if (profile.ColorGlyphs[index].GlyphIndex == glyphIndex) return true;
            index = index + 1;
        }
        return false;
    }

    string UnsupportedColorTable(SvgFont font, bool hasColorGlyph)
    {
        if (hasColorGlyph) return "";
        if (font.FindTable("SVG ") != null) return "SVG ";
        if (font.FindTable("CBDT") != null || font.FindTable("CBLC") != null) return "CBDT";
        if (font.FindTable("sbix") != null) return "sbix";
        return "";
    }

    bool ValidProfileArguments(SvgFont? font, SvgOpenTypeProfileOptions? options)
    {
        return font != null && font.Success && font.Bytes != null && options != null && options.Limits != null && options.Limits.MaxVariationAxes > 0 && options.MaximumPalettes > 0 && options.MaximumPaletteColors > 0 && options.MaximumColorGlyphs > 0 && options.MaximumColorLayers > 0;
    }

    bool ValidInstanceArguments(SvgFont? font, int glyphIndex, SvgOpenTypeInstanceOptions? options)
    {
        return font != null && font.Success && font.Bytes != null && font.MaximumProfile != null && glyphIndex >= 0 && glyphIndex < font.MaximumProfile.GlyphCount && options != null && options.Coordinates != null && options.TrueTypeOptions != null && options.CffOptions != null && options.ProfileOptions != null && SvgFiniteMath.IsFinite(options.EmSize) && options.EmSize >= 0 && options.PaletteIndex >= 0;
    }

    bool TableRange(SvgFont font, SvgOpenTypeTable table)
    {
        return table.Offset >= 0 && table.Length >= 0 && font.Bytes != null && (long)table.Offset + table.Length <= font.Bytes.Length;
    }

    bool ValidTag(string tag)
    {
        if (tag.Length != 4) return false;
        int index = 0;
        while (index < 4)
        {
            if (tag[index] < 32 || tag[index] > 126) return false;
            index = index + 1;
        }
        return true;
    }

    string Tag(byte[] bytes, int offset)
    {
        return new string(new char[] { (char)bytes[offset], (char)bytes[offset + 1], (char)bytes[offset + 2], (char)bytes[offset + 3] });
    }

    int UInt16(byte[] bytes, int offset)
    {
        return bytes[offset] << 8 | bytes[offset + 1];
    }

    uint UInt32(byte[] bytes, int offset)
    {
        return (uint)bytes[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }

    double Fixed(byte[] bytes, int offset)
    {
        int value = (int)new SvgOpenTypeFontProcessor().UInt32(bytes, offset);
        return value / 65536.0;
    }

    double F2Dot14(byte[] bytes, int offset)
    {
        int value = new SvgOpenTypeFontProcessor().UInt16(bytes, offset);
        if (value >= 32768) value = value - 65536;
        return value / 16384.0;
    }

    bool ProfileInvalid(SvgOpenTypeProcessorState state, string message, string table)
    {
        new SvgOpenTypeFontProcessor().ProfileFailure(state.Profile, SvgOpenTypeInstanceStatus.InvalidFont, message, table, 0);
        return false;
    }

    bool ProfileCancelled(SvgOpenTypeProcessorState state)
    {
        new SvgOpenTypeFontProcessor().ProfileFailure(state.Profile, SvgOpenTypeInstanceStatus.Cancelled, "OpenType profile inspection was cancelled.", "", 0);
        return false;
    }

    bool ProfileLimited(SvgOpenTypeProcessorState state, string message, string table)
    {
        new SvgOpenTypeFontProcessor().ProfileFailure(state.Profile, SvgOpenTypeInstanceStatus.LimitExceeded, message, table, 0);
        return false;
    }

    SvgOpenTypeFontProfile ProfileFailure(SvgOpenTypeFontProfile result, SvgOpenTypeInstanceStatus status, string message, string table, int operation)
    {
        result.Status = status;
        result.Success = false;
        result.RequiredTableTag = table;
        result.UnsupportedOperator = operation;
        SvgDiagnosticCode code = status == SvgOpenTypeInstanceStatus.CallbackRequired ? SvgDiagnosticCode.CallbackRequired : status == SvgOpenTypeInstanceStatus.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : status == SvgOpenTypeInstanceStatus.Cancelled ? SvgDiagnosticCode.Cancelled : SvgDiagnosticCode.InvalidFont;
        result.Diagnostics.Add(new SvgDiagnostic(status == SvgOpenTypeInstanceStatus.CallbackRequired ? SvgDiagnosticSeverity.Warning : SvgDiagnosticSeverity.Error, code, message, null));
        return result;
    }

    SvgOpenTypeGlyphInstance InstanceFailure(SvgOpenTypeGlyphInstance result, SvgOpenTypeInstanceStatus status, string message, string table, int operation)
    {
        result.Status = status;
        result.Success = false;
        result.RequiredTableTag = table;
        result.UnsupportedOperator = operation;
        SvgDiagnosticCode code = status == SvgOpenTypeInstanceStatus.CallbackRequired ? SvgDiagnosticCode.CallbackRequired : status == SvgOpenTypeInstanceStatus.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : status == SvgOpenTypeInstanceStatus.Cancelled ? SvgDiagnosticCode.Cancelled : SvgDiagnosticCode.InvalidFont;
        result.Diagnostics.Add(new SvgDiagnostic(status == SvgOpenTypeInstanceStatus.CallbackRequired ? SvgDiagnosticSeverity.Warning : SvgDiagnosticSeverity.Error, code, message, null));
        return result;
    }

    bool InstanceFailureBoolean(SvgOpenTypeGlyphInstance result, SvgOpenTypeInstanceStatus status, string message, string table, int operation)
    {
        new SvgOpenTypeFontProcessor().InstanceFailure(result, status, message, table, operation);
        return false;
    }

    SvgOpenTypeCoordinateState? CoordinateFailure(SvgOpenTypeGlyphInstance result, string message)
    {
        new SvgOpenTypeFontProcessor().InstanceFailure(result, SvgOpenTypeInstanceStatus.InvalidArgument, message, "fvar", 0);
        return null;
    }
}
