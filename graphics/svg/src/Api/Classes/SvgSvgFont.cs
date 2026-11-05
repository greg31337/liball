namespace svg;

/// <summary>Reads a bounded SVG 1.1 font element into deterministic glyph and kerning lookup tables.</summary>
public class SvgSvgFontReader : ISvgSvgFontReader
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgSvgFontReader()
    {
    }

    /// <summary>Reads one legacy SVG font element with secure defaults.</summary>
    /// <param name="fontNode">The legacy font element.</param>
    /// <returns>The parsed font or stable diagnostics.</returns>
    public SvgSvgFontReadResult ReadWithFontNode(SvgNode fontNode)
    {
        return new SvgSvgFontReader().ReadWithFontNodeAndOptions(fontNode, new SvgSvgFontOptions());
    }

    /// <summary>Reads one legacy SVG font element with explicit bounds.</summary>
    /// <param name="fontNode">The legacy font element.</param>
    /// <param name="options">The glyph, kerning, path, token, and cancellation bounds.</param>
    /// <returns>The parsed font or stable diagnostics.</returns>
    public SvgSvgFontReadResult ReadWithFontNodeAndOptions(SvgNode? fontNode, SvgSvgFontOptions options)
    {
        SvgSvgFontReadResult result = new SvgSvgFontReadResult();
        if (!new SvgSvgFontReader().ValidOptions(options)) return new SvgSvgFontReader().Fail(result, SvgSvgFontError.InvalidArgument, SvgDiagnosticCode.InvalidArgument, "Legacy SVG font options are invalid.", null);
        if (fontNode == null || fontNode.Kind != SvgNodeKind.Element || fontNode.ElementKind != SvgElementKind.Font)
        {
            return new SvgSvgFontReader().Fail(result, SvgSvgFontError.NotFontElement, SvgDiagnosticCode.InvalidArgument, "A legacy SVG font element is required.", fontNode == null ? null : fontNode.SourceSpan);
        }
        result.Font.SourceNode = fontNode;
        SvgNode? fontFace = new SvgSvgFontReader().FindChild(fontNode, SvgElementKind.FontFace);
        if (fontFace == null) return new SvgSvgFontReader().Fail(result, SvgSvgFontError.MissingFontFace, SvgDiagnosticCode.InvalidFont, "The legacy SVG font requires a font-face child.", fontNode.SourceSpan);
        result.Font.FontFaceNode = fontFace;
        result.Font.Family = new SvgSvgFontReader().AttributeText(fontFace, SvgCssPropertyNames.FontFamily, "").Trim();

        SvgSvgNumberValue units = new SvgSvgFontReader().Number(fontFace, "units-per-em", 1000, false, true, options);
        SvgSvgNumberValue horizontalAdvance = new SvgSvgFontReader().Number(fontNode, "horiz-adv-x", 0, true, true, options);
        SvgSvgNumberValue horizontalOriginX = new SvgSvgFontReader().Number(fontNode, "horiz-origin-x", 0, false, false, options);
        SvgSvgNumberValue horizontalOriginY = new SvgSvgFontReader().Number(fontNode, "horiz-origin-y", 0, false, false, options);
        if (!units.Success || !horizontalAdvance.Success || !horizontalOriginX.Success || !horizontalOriginY.Success)
        {
            return new SvgSvgFontReader().MetricFailure(result, new SvgSvgFontReader().FirstInvalid(units, horizontalAdvance, horizontalOriginX, horizontalOriginY));
        }
        result.Font.UnitsPerEm = units.Value;
        result.Font.HorizontalAdvance = horizontalAdvance.Value;
        result.Font.HorizontalOriginX = horizontalOriginX.Value;
        result.Font.HorizontalOriginY = horizontalOriginY.Value;

        SvgSvgNumberValue ascent = new SvgSvgFontReader().Number(fontFace, "ascent", 0, false, false, options);
        SvgSvgNumberValue verticalOriginY = new SvgSvgFontReader().Number(fontNode, "vert-origin-y", 0, false, false, options);
        if (!ascent.Success || !verticalOriginY.Success) return new SvgSvgFontReader().MetricFailure(result, !ascent.Success ? ascent.Attribute : verticalOriginY.Attribute);
        if (!ascent.Found && !verticalOriginY.Found)
        {
            result.Font.Ascent = result.Font.UnitsPerEm;
            result.Font.VerticalOriginY = result.Font.Ascent;
        }
        else if (!ascent.Found)
        {
            result.Font.VerticalOriginY = verticalOriginY.Value;
            result.Font.Ascent = result.Font.UnitsPerEm - result.Font.VerticalOriginY;
        }
        else
        {
            result.Font.Ascent = ascent.Value;
            result.Font.VerticalOriginY = verticalOriginY.Found ? verticalOriginY.Value : result.Font.Ascent;
        }
        SvgSvgNumberValue descent = new SvgSvgFontReader().Number(fontFace, "descent", result.Font.VerticalOriginY, false, false, options);
        SvgSvgNumberValue verticalOriginX = new SvgSvgFontReader().Number(fontNode, "vert-origin-x", result.Font.HorizontalAdvance / 2, false, false, options);
        SvgSvgNumberValue verticalAdvance = new SvgSvgFontReader().Number(fontNode, "vert-adv-y", result.Font.UnitsPerEm, false, true, options);
        if (!descent.Success || !verticalOriginX.Success || !verticalAdvance.Success)
        {
            return new SvgSvgFontReader().MetricFailure(result, new SvgSvgFontReader().FirstInvalid(descent, verticalOriginX, verticalAdvance, null));
        }
        result.Font.Descent = descent.Value;
        result.Font.VerticalOriginX = verticalOriginX.Value;
        result.Font.VerticalAdvance = verticalAdvance.Value;

        int childIndex = 0;
        while (childIndex < fontNode.Children.Count)
        {
            if (options.CancellationToken.IsCancellationRequested) return new SvgSvgFontReader().Fail(result, SvgSvgFontError.Cancelled, SvgDiagnosticCode.Cancelled, "Legacy SVG font parsing was cancelled.", fontNode.SourceSpan);
            SvgNode child = fontNode.Children[childIndex];
            if (child.Kind == SvgNodeKind.Element && child.ElementKind == SvgElementKind.Glyph)
            {
                if (result.Font.Glyphs.Count + (result.Font.MissingGlyph == null ? 0 : 1) >= options.MaxGlyphs)
                {
                    return new SvgSvgFontReader().Fail(result, SvgSvgFontError.LimitExceeded, SvgDiagnosticCode.InputLimitExceeded, "The legacy SVG font exceeds MaxGlyphs.", child.SourceSpan);
                }
                SvgSvgGlyph? glyph = new SvgSvgFontReader().Glyph(child, result.Font, false, options, result);
                if (glyph == null) return result;
                glyph.Index = result.Font.Glyphs.Count;
                result.Font.Glyphs.Add(glyph);
            }
            else if (child.Kind == SvgNodeKind.Element && child.ElementKind == SvgElementKind.MissingGlyph)
            {
                if (result.Font.MissingGlyph != null) return new SvgSvgFontReader().Fail(result, SvgSvgFontError.InvalidGlyph, SvgDiagnosticCode.InvalidFont, "The legacy SVG font has multiple missing-glyph elements.", child.SourceSpan);
                if (result.Font.Glyphs.Count >= options.MaxGlyphs) return new SvgSvgFontReader().Fail(result, SvgSvgFontError.LimitExceeded, SvgDiagnosticCode.InputLimitExceeded, "The legacy SVG font exceeds MaxGlyphs.", child.SourceSpan);
                result.Font.MissingGlyph = new SvgSvgFontReader().Glyph(child, result.Font, true, options, result);
                if (result.Font.MissingGlyph == null) return result;
            }
            else if (child.Kind == SvgNodeKind.Element && (child.ElementKind == SvgElementKind.HKern || child.ElementKind == SvgElementKind.VKern))
            {
                if (result.Font.HorizontalKerning.Count + result.Font.VerticalKerning.Count >= options.MaxKerningRules)
                {
                    return new SvgSvgFontReader().Fail(result, SvgSvgFontError.LimitExceeded, SvgDiagnosticCode.InputLimitExceeded, "The legacy SVG font exceeds MaxKerningRules.", child.SourceSpan);
                }
                bool vertical = child.ElementKind == SvgElementKind.VKern;
                SvgSvgKerning? kerning = new SvgSvgFontReader().Kerning(child, vertical, options, result);
                if (kerning == null) return result;
                if (vertical)
                {
                    kerning.Index = result.Font.VerticalKerning.Count;
                    result.Font.VerticalKerning.Add(kerning);
                }
                else
                {
                    kerning.Index = result.Font.HorizontalKerning.Count;
                    result.Font.HorizontalKerning.Add(kerning);
                }
            }
            childIndex = childIndex + 1;
        }
        result.Success = true;
        return result;
    }
SvgSvgGlyph? Glyph(SvgNode node, SvgSvgFont font, bool missing, SvgSvgFontOptions options, SvgSvgFontReadResult result)
    {
        SvgSvgGlyph glyph = new SvgSvgGlyph();
        glyph.SourceNode = node;
        glyph.IsMissingGlyph = missing;
        SvgAttribute? unicode = new SvgSvgFontReader().FindAttribute(node, "unicode");
        if (!missing && unicode != null)
        {
            glyph.HasUnicode = true;
            glyph.Unicode = unicode.Value;
        }
        if (!missing)
        {
            if (!new SvgSvgFontReader().ParseNames(new SvgSvgFontReader().AttributeText(node, "glyph-name", ""), glyph.Names, options.MaxTokenCharacters) || !new SvgSvgFontReader().ParseNames(new SvgSvgFontReader().AttributeText(node, SvgAttributeNames.Lang, ""), glyph.Languages, options.MaxTokenCharacters))
            {
                new SvgSvgFontReader().Fail(result, SvgSvgFontError.InvalidGlyph, SvgDiagnosticCode.InvalidFont, "A legacy SVG glyph name or language list is invalid.", node.SourceSpan);
                return null;
            }
            string orientation = new SvgSvgFontReader().AttributeText(node, "orientation", "").Trim();
            if (orientation == "h") glyph.Orientation = SvgSvgFontOrientation.Horizontal;
            else if (orientation == "v") glyph.Orientation = SvgSvgFontOrientation.Vertical;
            else if (orientation.Length != 0)
            {
                new SvgSvgFontReader().Fail(result, SvgSvgFontError.InvalidGlyph, SvgDiagnosticCode.InvalidFont, "A legacy SVG glyph orientation must be h or v.", node.SourceSpan);
                return null;
            }
            string form = new SvgSvgFontReader().AttributeText(node, "arabic-form", "").Trim();
            if (form == "initial") glyph.ArabicForm = SvgSvgArabicForm.Initial;
            else if (form == "medial") glyph.ArabicForm = SvgSvgArabicForm.Medial;
            else if (form == "terminal") glyph.ArabicForm = SvgSvgArabicForm.Terminal;
            else if (form == "isolated") glyph.ArabicForm = SvgSvgArabicForm.Isolated;
            else if (form.Length != 0)
            {
                new SvgSvgFontReader().Fail(result, SvgSvgFontError.InvalidGlyph, SvgDiagnosticCode.InvalidFont, "A legacy SVG glyph Arabic form is invalid.", node.SourceSpan);
                return null;
            }
        }
        SvgSvgNumberValue horizontalAdvance = new SvgSvgFontReader().Number(node, "horiz-adv-x", font.HorizontalAdvance, false, true, options);
        SvgSvgNumberValue verticalOriginX = new SvgSvgFontReader().Number(node, "vert-origin-x", font.VerticalOriginX, false, false, options);
        SvgSvgNumberValue verticalOriginY = new SvgSvgFontReader().Number(node, "vert-origin-y", font.VerticalOriginY, false, false, options);
        SvgSvgNumberValue verticalAdvance = new SvgSvgFontReader().Number(node, "vert-adv-y", font.VerticalAdvance, false, true, options);
        if (!horizontalAdvance.Success || !verticalOriginX.Success || !verticalOriginY.Success || !verticalAdvance.Success)
        {
            new SvgSvgFontReader().MetricFailure(result, new SvgSvgFontReader().FirstInvalid(horizontalAdvance, verticalOriginX, verticalOriginY, verticalAdvance));
            return null;
        }
        glyph.HorizontalAdvance = horizontalAdvance.Value;
        glyph.HorizontalOriginX = font.HorizontalOriginX;
        glyph.HorizontalOriginY = font.HorizontalOriginY;
        glyph.VerticalOriginX = verticalOriginX.Value;
        glyph.VerticalOriginY = verticalOriginY.Value;
        glyph.VerticalAdvance = verticalAdvance.Value;
        string pathText = new SvgSvgFontReader().AttributeText(node, SvgAttributeNames.D, "");
        SvgPathParseResult path = new SvgPathParser().Parse(pathText, options.MaxPathSegments, options.MaxTokenCharacters);
        if (!path.Success)
        {
            SvgSvgFontError error = path.Error == SvgPathParseError.SegmentLimitExceeded ? SvgSvgFontError.LimitExceeded : SvgSvgFontError.InvalidGlyph;
            SvgDiagnosticCode code = error == SvgSvgFontError.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidFont;
            new SvgSvgFontReader().Fail(result, error, code, "A legacy SVG glyph path is invalid or exceeds its bound.", node.SourceSpan);
            return null;
        }
        glyph.Outline = path.PathData;
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            if (node.Children[childIndex].Kind == SvgNodeKind.Element)
            {
                glyph.HasChildContent = true;
                break;
            }
            childIndex = childIndex + 1;
        }
        return glyph;
    }
SvgSvgKerning? Kerning(SvgNode node, bool vertical, SvgSvgFontOptions options, SvgSvgFontReadResult result)
    {
        SvgSvgKerning kerning = new SvgSvgKerning();
        kerning.SourceNode = node;
        kerning.Vertical = vertical;
        string firstUnicode = new SvgSvgFontReader().AttributeText(node, "u1", "");
        string firstNames = new SvgSvgFontReader().AttributeText(node, "g1", "");
        string secondUnicode = new SvgSvgFontReader().AttributeText(node, "u2", "");
        string secondNames = new SvgSvgFontReader().AttributeText(node, "g2", "");
        if (!new SvgSvgFontReader().ParseUnicodeRanges(firstUnicode, kerning.FirstUnicodeRanges, options.MaxTokenCharacters) || !new SvgSvgFontReader().ParseNames(firstNames, kerning.FirstGlyphNames, options.MaxTokenCharacters) || !new SvgSvgFontReader().ParseUnicodeRanges(secondUnicode, kerning.SecondUnicodeRanges, options.MaxTokenCharacters) || !new SvgSvgFontReader().ParseNames(secondNames, kerning.SecondGlyphNames, options.MaxTokenCharacters))
        {
            new SvgSvgFontReader().Fail(result, SvgSvgFontError.InvalidKerning, SvgDiagnosticCode.InvalidFont, "A legacy SVG kerning selector is invalid.", node.SourceSpan);
            return null;
        }
        if (kerning.FirstUnicodeRanges.Count + kerning.FirstGlyphNames.Count == 0 || kerning.SecondUnicodeRanges.Count + kerning.SecondGlyphNames.Count == 0)
        {
            new SvgSvgFontReader().Fail(result, SvgSvgFontError.InvalidKerning, SvgDiagnosticCode.InvalidFont, "A legacy SVG kerning rule requires selectors on both sides.", node.SourceSpan);
            return null;
        }
        SvgSvgNumberValue adjustment = new SvgSvgFontReader().Number(node, "k", 0, true, false, options);
        if (!adjustment.Success)
        {
            new SvgSvgFontReader().Fail(result, SvgSvgFontError.InvalidKerning, SvgDiagnosticCode.InvalidFont, "A legacy SVG kerning adjustment is invalid.", adjustment.Attribute == null ? node.SourceSpan : adjustment.Attribute.SourceSpan);
            return null;
        }
        kerning.Adjustment = adjustment.Value;
        return kerning;
    }
bool ParseUnicodeRanges(string text, System.Collections.Generic.List<SvgUnicodeRange> ranges, int maxTokenCharacters)
    {
        if (text.Length == 0) return true;
        int start = 0;
        while (start <= text.Length)
        {
            int end = text.IndexOf(',', start);
            if (end < 0) end = text.Length;
            string item = text.Substring(start, end - start).Trim();
            if (item.Length == 0 || item.Length > maxTokenCharacters || !new SvgSvgFontReader().ParseUnicodeRange(item, ranges)) return false;
            if (end == text.Length) break;
            start = end + 1;
        }
        return true;
    }
bool ParseUnicodeRange(string text, System.Collections.Generic.List<SvgUnicodeRange> ranges)
    {
        if (text.Length >= 3 && (text[0] == 'U' || text[0] == 'u') && text[1] == '+')
        {
            string body = text.Substring(2);
            int hyphen = body.IndexOf('-');
            int question = body.IndexOf('?');
            SvgSvgRangeValue parsed = new SvgSvgRangeValue();
            if (question >= 0)
            {
                if (hyphen >= 0 || body.Length > 6) return false;
                parsed = new SvgSvgFontReader().WildcardRange(body, question);
            }
            else if (hyphen >= 0)
            {
                SvgSvgHexValue first = new SvgSvgFontReader().Hex(body.Substring(0, hyphen));
                SvgSvgHexValue second = new SvgSvgFontReader().Hex(body.Substring(hyphen + 1));
                parsed.Success = first.Success && second.Success;
                parsed.Start = first.Value;
                parsed.End = second.Value;
            }
            else
            {
                SvgSvgHexValue single = new SvgSvgFontReader().Hex(body);
                parsed.Success = single.Success;
                parsed.Start = single.Value;
                parsed.End = single.Value;
            }
            if (!parsed.Success || !new SvgSvgFontReader().ValidScalar(parsed.Start) || !new SvgSvgFontReader().ValidScalar(parsed.End) || parsed.End < parsed.Start) return false;
            ranges.Add(new SvgUnicodeRange(parsed.Start, parsed.End));
            return true;
        }
        int scalar = new SvgSvgFontReader().SingleScalar(text);
        if (scalar < 0) return false;
        ranges.Add(new SvgUnicodeRange(scalar, scalar));
        return true;
    }
SvgSvgRangeValue WildcardRange(string text, int firstQuestion)
    {
        SvgSvgRangeValue result = new SvgSvgRangeValue();
        if (firstQuestion == 0) return result;
        int index = 0;
        while (index < text.Length)
        {
            if (index < firstQuestion)
            {
                int digit = new SvgSvgFontReader().HexDigit(text[index]);
                if (digit < 0) return result;
                result.Start = result.Start * 16 + digit;
                result.End = result.End * 16 + digit;
            }
            else
            {
                if (text[index] != '?') return result;
                result.Start = result.Start * 16;
                result.End = result.End * 16 + 15;
            }
            index = index + 1;
        }
        result.Success = true;
        return result;
    }
SvgSvgHexValue Hex(string text)
    {
        SvgSvgHexValue result = new SvgSvgHexValue();
        if (text.Length == 0 || text.Length > 6) return result;
        int index = 0;
        while (index < text.Length)
        {
            int digit = new SvgSvgFontReader().HexDigit(text[index]);
            if (digit < 0) return result;
            result.Value = result.Value * 16 + digit;
            index = index + 1;
        }
        result.Success = true;
        return result;
    }
int HexDigit(char value)
    {
        if (value >= '0' && value <= '9') return value - '0';
        if (value >= 'A' && value <= 'F') return value - 'A' + 10;
        if (value >= 'a' && value <= 'f') return value - 'a' + 10;
        return -1;
    }
int SingleScalar(string value)
    {
        if (value.Length == 1)
        {
            int scalar = value[0];
            return new SvgSvgFontReader().ValidScalar(scalar) ? scalar : -1;
        }
        if (value.Length == 2 && value[0] >= 0xD800 && value[0] <= 0xDBFF && value[1] >= 0xDC00 && value[1] <= 0xDFFF)
        {
            return 0x10000 + ((value[0] - 0xD800) << 10) + value[1] - 0xDC00;
        }
        return -1;
    }
bool ParseNames(string text, System.Collections.Generic.List<string> names, int maxTokenCharacters)
    {
        if (text.Length == 0) return true;
        int start = 0;
        while (start <= text.Length)
        {
            int end = text.IndexOf(',', start);
            if (end < 0) end = text.Length;
            string item = text.Substring(start, end - start).Trim();
            if (item.Length == 0 || item.Length > maxTokenCharacters) return false;
            names.Add(item);
            if (end == text.Length) break;
            start = end + 1;
        }
        return true;
    }
SvgSvgNumberValue Number(SvgNode node, string name, double fallback, bool required, bool nonnegative, SvgSvgFontOptions options)
    {
        SvgSvgNumberValue result = new SvgSvgNumberValue();
        result.Value = fallback;
        result.Attribute = new SvgSvgFontReader().FindAttribute(node, name);
        if (result.Attribute == null)
        {
            result.Success = !required;
            return result;
        }
        result.Found = true;
        SvgNumberParseResult parsed = SvgNumberParser.ParseNumber(result.Attribute.Value, options.MaxTokenCharacters);
        if (!parsed.Success || nonnegative && parsed.Value < 0 || name == "units-per-em" && parsed.Value <= 0)
        {
            result.Success = false;
            return result;
        }
        result.Value = parsed.Value;
        return result;
    }
SvgAttribute? FirstInvalid(SvgSvgNumberValue? first, SvgSvgNumberValue? second, SvgSvgNumberValue? third, SvgSvgNumberValue? fourth)
    {
        if (first != null && !first.Success) return first.Attribute;
        if (second != null && !second.Success) return second.Attribute;
        if (third != null && !third.Success) return third.Attribute;
        return fourth == null ? null : fourth.Attribute;
    }
SvgSvgFontReadResult MetricFailure(SvgSvgFontReadResult result, SvgAttribute? attribute)
    {
        return new SvgSvgFontReader().Fail(result, SvgSvgFontError.InvalidMetric, SvgDiagnosticCode.InvalidFont, "A required legacy SVG font metric is absent or invalid.", attribute == null ? (result.Font.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan : attribute.SourceSpan);
    }
SvgSvgFontReadResult Fail(SvgSvgFontReadResult result, SvgSvgFontError error, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        if (result.Error == SvgSvgFontError.None) result.Error = error;
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
        return result;
    }
SvgNode? FindChild(SvgNode node, SvgElementKind kind)
    {
        int index = 0;
        while (index < node.Children.Count)
        {
            if (node.Children[index].Kind == SvgNodeKind.Element && node.Children[index].ElementKind == kind) return node.Children[index];
            index = index + 1;
        }
        return null;
    }
SvgAttribute? FindAttribute(SvgNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == name) return attribute;
            index = index + 1;
        }
        return null;
    }
string AttributeText(SvgNode node, string name, string fallback)
    {
        SvgAttribute? attribute = new SvgSvgFontReader().FindAttribute(node, name);
        return attribute == null ? fallback : attribute.Value;
    }
bool ValidOptions(SvgSvgFontOptions options)
    {
        return options != null && options.MaxGlyphs > 0 && options.MaxKerningRules > 0 && options.MaxPathSegments > 0 && options.MaxTokenCharacters > 0;
    }
bool ValidScalar(int scalar)
    {
        return scalar >= 0 && scalar <= 0x10FFFF && (scalar < 0xD800 || scalar > 0xDFFF);
    }
}
