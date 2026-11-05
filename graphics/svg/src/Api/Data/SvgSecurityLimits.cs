namespace svg;

/// <summary>Defines bounded input and container limits.</summary>
public class SvgInputLimits
{
    /// <summary>The maximum encoded input byte count.</summary>
    public long MaxInputBytes;
    /// <summary>The maximum decoded UTF-16 character count.</summary>
    public long MaxInputCharacters;
    /// <summary>The maximum decompressed SVGZ byte count.</summary>
    public long MaxGzipOutputBytes;
    /// <summary>The maximum decompressed-to-compressed size ratio.</summary>
    public int MaxGzipExpansionRatio;
    /// <summary>The maximum character count in one XML token.</summary>
    public int MaxXmlTokenCharacters;

    /// <summary>Creates secure input defaults.</summary>
    public SvgInputLimits()
    {
        MaxInputBytes = 33554432;
        MaxInputCharacters = 33554432;
        MaxGzipOutputBytes = 67108864;
        MaxGzipExpansionRatio = 100;
        MaxXmlTokenCharacters = 16777216;
    }
}

/// <summary>Defines bounded XML and editable-tree limits.</summary>
public class SvgTreeLimits
{
    /// <summary>The maximum XML element nesting depth.</summary>
    public int MaxDepth;
    /// <summary>The maximum total node count.</summary>
    public int MaxNodes;
    /// <summary>The maximum attributes on one element.</summary>
    public int MaxAttributesPerElement;
    /// <summary>The maximum total attribute count.</summary>
    public int MaxTotalAttributes;
    /// <summary>The maximum cumulative text character count.</summary>
    public long MaxTextCharacters;
    /// <summary>The maximum XML qualified-name character count.</summary>
    public int MaxNameCharacters;

    /// <summary>Creates secure tree defaults.</summary>
    public SvgTreeLimits()
    {
        MaxDepth = 128;
        MaxNodes = 250000;
        MaxAttributesPerElement = 256;
        MaxTotalAttributes = 500000;
        MaxTextCharacters = 16777216;
        MaxNameCharacters = 1024;
    }
}

/// <summary>Defines bounded CSS parsing and cascade limits.</summary>
public class SvgCssLimits
{
    /// <summary>The maximum cumulative stylesheet character count.</summary>
    public long MaxCharacters;
    /// <summary>The maximum CSS token count.</summary>
    public int MaxTokens;
    /// <summary>The maximum rule count.</summary>
    public int MaxRules;
    /// <summary>The maximum declaration count.</summary>
    public int MaxDeclarations;
    /// <summary>The maximum tokens in one selector.</summary>
    public int MaxSelectorTokens;
    /// <summary>The maximum nested CSS block depth.</summary>
    public int MaxBlockDepth;
    /// <summary>The maximum imported stylesheet depth.</summary>
    public int MaxImportDepth;
    /// <summary>The maximum tokens produced by one custom-property expansion.</summary>
    public int MaxVariableExpansionTokens;

    /// <summary>Creates secure CSS defaults.</summary>
    public SvgCssLimits()
    {
        MaxCharacters = 8388608;
        MaxTokens = 1000000;
        MaxRules = 50000;
        MaxDeclarations = 250000;
        MaxSelectorTokens = 4096;
        MaxBlockDepth = 64;
        MaxImportDepth = 16;
        MaxVariableExpansionTokens = 100000;
    }
}

/// <summary>Defines bounded reference traversal and expansion limits.</summary>
public class SvgReferenceLimits
{
    /// <summary>The maximum nested reference depth.</summary>
    public int MaxDepth;
    /// <summary>The maximum references resolved in one operation.</summary>
    public int MaxCount;
    /// <summary>The maximum expanded use instances.</summary>
    public int MaxUseInstances;
    /// <summary>The maximum paint-server inheritance depth.</summary>
    public int MaxPaintServerDepth;
    /// <summary>The maximum effect inheritance depth.</summary>
    public int MaxEffectDepth;
    /// <summary>The maximum conditional-processing checks.</summary>
    public int MaxConditionalChecks;

    /// <summary>Creates secure reference defaults.</summary>
    public SvgReferenceLimits()
    {
        MaxDepth = 64;
        MaxCount = 100000;
        MaxUseInstances = 100000;
        MaxPaintServerDepth = 32;
        MaxEffectDepth = 32;
        MaxConditionalChecks = 1000000;
    }
}

/// <summary>Defines bounded external and data resource limits.</summary>
public class SvgResourceLimits
{
    /// <summary>The maximum resolved resource count.</summary>
    public int MaxCount;
    /// <summary>The maximum encoded bytes in one resource.</summary>
    public long MaxBytesPerResource;
    /// <summary>The maximum encoded bytes across all resources.</summary>
    public long MaxTotalBytes;
    /// <summary>The maximum decoded bytes in one data URL.</summary>
    public long MaxDataUrlBytes;
    /// <summary>The maximum redirect count for one resource.</summary>
    public int MaxRedirects;
    /// <summary>The maximum URI character count.</summary>
    public int MaxUriCharacters;
    /// <summary>The maximum elapsed callback time in milliseconds.</summary>
    public int MaxCallbackMilliseconds;

    /// <summary>Creates secure resource defaults.</summary>
    public SvgResourceLimits()
    {
        MaxCount = 256;
        MaxBytesPerResource = 33554432;
        MaxTotalBytes = 134217728;
        MaxDataUrlBytes = 16777216;
        MaxRedirects = 5;
        MaxUriCharacters = 8192;
        MaxCallbackMilliseconds = 30000;
    }
}

/// <summary>Defines bounded geometry and path-processing limits.</summary>
public class SvgGeometryLimits
{
    /// <summary>The maximum parsed path segment count.</summary>
    public int MaxPathSegments;
    /// <summary>The maximum coordinate point count.</summary>
    public int MaxPoints;
    /// <summary>The maximum segments produced by flattening.</summary>
    public int MaxFlattenedSegments;
    /// <summary>The maximum adaptive subdivision depth.</summary>
    public int MaxSubdivisionDepth;
    /// <summary>The maximum intersection result count.</summary>
    public int MaxIntersections;
    /// <summary>The maximum planar edges used by Boolean operations.</summary>
    public int MaxBooleanEdges;
    /// <summary>The maximum nodes visited by one hit test.</summary>
    public int MaxHitTestNodes;

    /// <summary>Creates secure geometry defaults.</summary>
    public SvgGeometryLimits()
    {
        MaxPathSegments = 1000000;
        MaxPoints = 1000000;
        MaxFlattenedSegments = 2000000;
        MaxSubdivisionDepth = 32;
        MaxIntersections = 1000000;
        MaxBooleanEdges = 2000000;
        MaxHitTestNodes = 1000000;
    }
}

/// <summary>Defines bounded font loading and shaping limits.</summary>
public class SvgFontLimits
{
    /// <summary>The maximum loaded font count.</summary>
    public int MaxFonts;
    /// <summary>The maximum bytes in one font.</summary>
    public long MaxBytesPerFont;
    /// <summary>The maximum bytes across all fonts.</summary>
    public long MaxTotalBytes;
    /// <summary>The maximum table count in one font.</summary>
    public int MaxTablesPerFont;
    /// <summary>The maximum glyph count in one font.</summary>
    public int MaxGlyphsPerFont;
    /// <summary>The maximum points in one expanded glyph outline.</summary>
    public int MaxPointsPerGlyph;
    /// <summary>The maximum glyphs produced by one shaping operation.</summary>
    public int MaxShapingGlyphs;
    /// <summary>The maximum variation axes in one font.</summary>
    public int MaxVariationAxes;

    /// <summary>Creates secure font defaults.</summary>
    public SvgFontLimits()
    {
        MaxFonts = 128;
        MaxBytesPerFont = 33554432;
        MaxTotalBytes = 134217728;
        MaxTablesPerFont = 4096;
        MaxGlyphsPerFont = 65535;
        MaxPointsPerGlyph = 1000000;
        MaxShapingGlyphs = 100000;
        MaxVariationAxes = 64;
    }
}

/// <summary>Defines bounded filter graph and evaluation limits.</summary>
public class SvgFilterLimits
{
    /// <summary>The maximum primitives in one filter graph.</summary>
    public int MaxPrimitives;
    /// <summary>The maximum filter-region area in pixels.</summary>
    public long MaxAreaPixels;
    /// <summary>The maximum kernel element count.</summary>
    public int MaxKernelElements;
    /// <summary>The maximum Gaussian blur standard deviation.</summary>
    public int MaxBlurStandardDeviation;
    /// <summary>The maximum turbulence octave count.</summary>
    public int MaxTurbulenceOctaves;
    /// <summary>The maximum simultaneously planned offscreen surfaces.</summary>
    public int MaxOffscreenSurfaces;

    /// <summary>Creates secure filter defaults.</summary>
    public SvgFilterLimits()
    {
        MaxPrimitives = 256;
        MaxAreaPixels = 16777216;
        MaxKernelElements = 65536;
        MaxBlurStandardDeviation = 4096;
        MaxTurbulenceOctaves = 10;
        MaxOffscreenSurfaces = 64;
    }
}

/// <summary>Defines bounded raster surface and PNG limits.</summary>
public class SvgRasterLimits
{
    /// <summary>The maximum raster or decoded image width.</summary>
    public int MaxWidth;
    /// <summary>The maximum raster or decoded image height.</summary>
    public int MaxHeight;
    /// <summary>The maximum pixels in one surface.</summary>
    public long MaxSurfacePixels;
    /// <summary>The maximum bytes in one pixel buffer.</summary>
    public long MaxPixelBytes;
    /// <summary>The maximum PNG chunk count.</summary>
    public int MaxPngChunks;
    /// <summary>The maximum decompressed PNG data bytes.</summary>
    public long MaxPngDecodedBytes;

    /// <summary>Creates secure raster defaults.</summary>
    public SvgRasterLimits()
    {
        MaxWidth = 32768;
        MaxHeight = 32768;
        MaxSurfacePixels = 100000000;
        MaxPixelBytes = 400000000;
        MaxPngChunks = 100000;
        MaxPngDecodedBytes = 400000000;
    }
}

/// <summary>Defines bounded declarative-animation timeline limits.</summary>
public class SvgTimelineLimits
{
    /// <summary>The maximum animation element count.</summary>
    public int MaxAnimations;
    /// <summary>The maximum generated animation instance count.</summary>
    public int MaxInstances;
    /// <summary>The maximum keyframe count.</summary>
    public int MaxKeyframes;
    /// <summary>The maximum injected or derived event-time count.</summary>
    public int MaxEventTimes;
    /// <summary>The maximum finite repeat count used for expansion.</summary>
    public int MaxRepeatCount;
    /// <summary>The maximum animation work units in one sample.</summary>
    public int MaxSampleWork;

    /// <summary>Creates secure timeline defaults.</summary>
    public SvgTimelineLimits()
    {
        MaxAnimations = 100000;
        MaxInstances = 1000000;
        MaxKeyframes = 100000;
        MaxEventTimes = 100000;
        MaxRepeatCount = 1000000;
        MaxSampleWork = 10000000;
    }
}

/// <summary>Describes validation of a security-limit configuration.</summary>
public class SvgSecurityLimitValidation
{
    /// <summary>Indicates whether every configured limit is positive.</summary>
    public bool Success;
    /// <summary>The invalid limit group, or an empty string after success.</summary>
    public string GroupName;
    /// <summary>The invalid field name, or an empty string after success.</summary>
    public string FieldName;
    /// <summary>The invalid value, or zero after success.</summary>
    public long Value;
    /// <summary>The validation message.</summary>
    public string Message;

    /// <summary>Creates a security-limit validation result.</summary>
    /// <param name="success">Whether validation succeeded.</param>
    /// <param name="groupName">The invalid group name, or an empty string.</param>
    /// <param name="fieldName">The invalid field name, or an empty string.</param>
    /// <param name="value">The invalid value, or zero.</param>
    /// <param name="message">The validation message.</param>
    public SvgSecurityLimitValidation(bool success, string groupName, string fieldName, long value, string message)
    {
        Success = success;
        GroupName = groupName;
        FieldName = fieldName;
        Value = value;
        Message = message;
    }
}

/// <summary>Aggregates all security and work limits used by public operations.</summary>
public class SvgSecurityLimits
{
    /// <summary>Input and container limits.</summary>
    public SvgInputLimits Input;
    /// <summary>XML and editable-tree limits.</summary>
    public SvgTreeLimits Tree;
    /// <summary>CSS parsing and cascade limits.</summary>
    public SvgCssLimits Css;
    /// <summary>Reference traversal and expansion limits.</summary>
    public SvgReferenceLimits References;
    /// <summary>External and data resource limits.</summary>
    public SvgResourceLimits Resources;
    /// <summary>Geometry and path-processing limits.</summary>
    public SvgGeometryLimits Geometry;
    /// <summary>Font loading and shaping limits.</summary>
    public SvgFontLimits Fonts;
    /// <summary>Filter graph and evaluation limits.</summary>
    public SvgFilterLimits Filters;
    /// <summary>Raster surface and PNG limits.</summary>
    public SvgRasterLimits Raster;
    /// <summary>Declarative-animation timeline limits.</summary>
    public SvgTimelineLimits Timeline;

    /// <summary>Creates a complete set of secure defaults.</summary>
    public SvgSecurityLimits()
    {
        Input = new SvgInputLimits();
        Tree = new SvgTreeLimits();
        Css = new SvgCssLimits();
        References = new SvgReferenceLimits();
        Resources = new SvgResourceLimits();
        Geometry = new SvgGeometryLimits();
        Fonts = new SvgFontLimits();
        Filters = new SvgFilterLimits();
        Raster = new SvgRasterLimits();
        Timeline = new SvgTimelineLimits();
    }

    /// <summary>Validates every group and field in deterministic declaration order.</summary>
    /// <returns>Success or the first nonpositive or missing limit.</returns>
    internal SvgSecurityLimitValidation Validate()
    {
        SvgSecurityLimitValidation result;

        if (Input == null)
        {
            return new SvgSecurityLimitValidation(false, "Input", "Input", -1, "The Input limit group is required.");
        }
        result = ValidateGroup(
            "Input",
            ["MaxInputBytes", "MaxInputCharacters", "MaxGzipOutputBytes", "MaxGzipExpansionRatio", "MaxXmlTokenCharacters"],
            [Input.MaxInputBytes, Input.MaxInputCharacters, Input.MaxGzipOutputBytes, Input.MaxGzipExpansionRatio, Input.MaxXmlTokenCharacters]);
        if (!result.Success)
        {
            return result;
        }

        if (Tree == null)
        {
            return new SvgSecurityLimitValidation(false, "Tree", "Tree", -1, "The Tree limit group is required.");
        }
        result = ValidateGroup(
            "Tree",
            ["MaxDepth", "MaxNodes", "MaxAttributesPerElement", "MaxTotalAttributes", "MaxTextCharacters", "MaxNameCharacters"],
            [Tree.MaxDepth, Tree.MaxNodes, Tree.MaxAttributesPerElement, Tree.MaxTotalAttributes, Tree.MaxTextCharacters, Tree.MaxNameCharacters]);
        if (!result.Success)
        {
            return result;
        }

        if (Css == null)
        {
            return new SvgSecurityLimitValidation(false, "Css", "Css", -1, "The Css limit group is required.");
        }
        result = ValidateGroup(
            "Css",
            ["MaxCharacters", "MaxTokens", "MaxRules", "MaxDeclarations", "MaxSelectorTokens", "MaxBlockDepth", "MaxImportDepth", "MaxVariableExpansionTokens"],
            [Css.MaxCharacters, Css.MaxTokens, Css.MaxRules, Css.MaxDeclarations, Css.MaxSelectorTokens, Css.MaxBlockDepth, Css.MaxImportDepth, Css.MaxVariableExpansionTokens]);
        if (!result.Success)
        {
            return result;
        }

        if (References == null)
        {
            return new SvgSecurityLimitValidation(false, "References", "References", -1, "The References limit group is required.");
        }
        result = ValidateGroup(
            "References",
            ["MaxDepth", "MaxCount", "MaxUseInstances", "MaxPaintServerDepth", "MaxEffectDepth", "MaxConditionalChecks"],
            [References.MaxDepth, References.MaxCount, References.MaxUseInstances, References.MaxPaintServerDepth, References.MaxEffectDepth, References.MaxConditionalChecks]);
        if (!result.Success)
        {
            return result;
        }

        if (Resources == null)
        {
            return new SvgSecurityLimitValidation(false, "Resources", "Resources", -1, "The Resources limit group is required.");
        }
        result = ValidateGroup(
            "Resources",
            ["MaxCount", "MaxBytesPerResource", "MaxTotalBytes", "MaxDataUrlBytes", "MaxRedirects", "MaxUriCharacters", "MaxCallbackMilliseconds"],
            [Resources.MaxCount, Resources.MaxBytesPerResource, Resources.MaxTotalBytes, Resources.MaxDataUrlBytes, Resources.MaxRedirects, Resources.MaxUriCharacters, Resources.MaxCallbackMilliseconds]);
        if (!result.Success)
        {
            return result;
        }

        if (Geometry == null)
        {
            return new SvgSecurityLimitValidation(false, "Geometry", "Geometry", -1, "The Geometry limit group is required.");
        }
        result = ValidateGroup(
            "Geometry",
            ["MaxPathSegments", "MaxPoints", "MaxFlattenedSegments", "MaxSubdivisionDepth", "MaxIntersections", "MaxBooleanEdges", "MaxHitTestNodes"],
            [Geometry.MaxPathSegments, Geometry.MaxPoints, Geometry.MaxFlattenedSegments, Geometry.MaxSubdivisionDepth, Geometry.MaxIntersections, Geometry.MaxBooleanEdges, Geometry.MaxHitTestNodes]);
        if (!result.Success)
        {
            return result;
        }

        if (Fonts == null)
        {
            return new SvgSecurityLimitValidation(false, "Fonts", "Fonts", -1, "The Fonts limit group is required.");
        }
        result = ValidateGroup(
            "Fonts",
            ["MaxFonts", "MaxBytesPerFont", "MaxTotalBytes", "MaxTablesPerFont", "MaxGlyphsPerFont", "MaxPointsPerGlyph", "MaxShapingGlyphs", "MaxVariationAxes"],
            [Fonts.MaxFonts, Fonts.MaxBytesPerFont, Fonts.MaxTotalBytes, Fonts.MaxTablesPerFont, Fonts.MaxGlyphsPerFont, Fonts.MaxPointsPerGlyph, Fonts.MaxShapingGlyphs, Fonts.MaxVariationAxes]);
        if (!result.Success)
        {
            return result;
        }

        if (Filters == null)
        {
            return new SvgSecurityLimitValidation(false, "Filters", "Filters", -1, "The Filters limit group is required.");
        }
        result = ValidateGroup(
            "Filters",
            ["MaxPrimitives", "MaxAreaPixels", "MaxKernelElements", "MaxBlurStandardDeviation", "MaxTurbulenceOctaves", "MaxOffscreenSurfaces"],
            [Filters.MaxPrimitives, Filters.MaxAreaPixels, Filters.MaxKernelElements, Filters.MaxBlurStandardDeviation, Filters.MaxTurbulenceOctaves, Filters.MaxOffscreenSurfaces]);
        if (!result.Success)
        {
            return result;
        }

        if (Raster == null)
        {
            return new SvgSecurityLimitValidation(false, "Raster", "Raster", -1, "The Raster limit group is required.");
        }
        result = ValidateGroup(
            "Raster",
            ["MaxWidth", "MaxHeight", "MaxSurfacePixels", "MaxPixelBytes", "MaxPngChunks", "MaxPngDecodedBytes"],
            [Raster.MaxWidth, Raster.MaxHeight, Raster.MaxSurfacePixels, Raster.MaxPixelBytes, Raster.MaxPngChunks, Raster.MaxPngDecodedBytes]);
        if (!result.Success)
        {
            return result;
        }

        if (Timeline == null)
        {
            return new SvgSecurityLimitValidation(false, "Timeline", "Timeline", -1, "The Timeline limit group is required.");
        }
        result = ValidateGroup(
            "Timeline",
            ["MaxAnimations", "MaxInstances", "MaxKeyframes", "MaxEventTimes", "MaxRepeatCount", "MaxSampleWork"],
            [Timeline.MaxAnimations, Timeline.MaxInstances, Timeline.MaxKeyframes, Timeline.MaxEventTimes, Timeline.MaxRepeatCount, Timeline.MaxSampleWork]);
        if (!result.Success)
        {
            return result;
        }

        return new SvgSecurityLimitValidation(true, "", "", 0, "All security limits are valid.");
    }

    internal static SvgSecurityLimitValidation ValidateGroup(string groupName, string[] fieldNames, long[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] <= 0)
            {
                return new SvgSecurityLimitValidation(
                    false,
                    groupName,
                    fieldNames[index],
                    values[index],
                    "Security limits must be greater than zero.");
            }
            index = index + 1;
        }
        return new SvgSecurityLimitValidation(true, "", "", 0, "All limits in the group are valid.");
    }
}
