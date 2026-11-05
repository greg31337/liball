namespace svg;

/// <summary>Converts cascaded property text into tagged computed values.</summary>
public class SvgComputedStyleConverter : ISvgComputedStyleConverter
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgComputedStyleConverter()
    {
    }

    /// <summary>Converts one element cascade using default options.</summary>
    /// <param name="node">The source SVG element.</param>
    /// <param name="cascade">The completed cascade result.</param>
    /// <returns>The typed computed style.</returns>
    public SvgComputedStyle ConvertWithNodeAndCascade(SvgNode node, SvgCascadeResult cascade)
    {
        return new SvgComputedStyleConverter().ConvertWithNodeAndCascadeAndOptions(node, cascade, new SvgComputedStyleOptions());
    }

    /// <summary>Converts one element cascade using explicit metadata and limits.</summary>
    /// <param name="node">The source SVG element.</param>
    /// <param name="cascade">The completed cascade result.</param>
    /// <param name="options">The metadata, parent style, and limit options.</param>
    /// <returns>The typed computed style and diagnostics.</returns>
    public SvgComputedStyle ConvertWithNodeAndCascadeAndOptions(SvgNode? node, SvgCascadeResult cascade, SvgComputedStyleOptions options)
    {
        SvgComputedStyle style = new SvgComputedStyle(node, cascade);
        if (node == null || cascade == null || options == null || !new SvgComputedStyleConverter().ValidArguments(node, cascade, options))
        {
            style.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, "Computed-style conversion arguments are invalid.", node == null ? null : node.SourceSpan));
            return style;
        }
        new SvgComputedStyleConverter().AppendDiagnostics(style.Diagnostics, cascade.Diagnostics);
        style.CustomProperties = new SvgComputedStyleConverter().CopyCustomProperties(cascade.CustomProperties, options);
        bool conversionSuccess = true;
        int entryIndex = 0;
        while (entryIndex < options.Properties.Entries.Count)
        {
            SvgPropertySupportEntry entry = options.Properties.Entries[entryIndex];
            if (entry != null && !entry.IsShorthand && new SvgComputedStyleConverter().Find(style.Values, entry.Name) == null)
            {
                SvgCascadedValue? cascaded = cascade.Find(entry.Name);
                string text = cascaded == null ? entry.InitialValue : cascaded.ValueText;
                SvgSourceSpan? source = cascaded == null || cascaded.Winner == null ? null : cascaded.Winner.Source;
                bool presentationAttribute = cascaded != null && cascaded.Winner != null && cascaded.Winner.SourceKind == SvgCascadeSourceKind.PresentationAttribute;
                if (new SvgComputedStyleConverter().ImplicitlyInherited(entry, cascaded, options)) text = ((options.Parent ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Find(entry.Name) ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).CssText;
                SvgComputedConversion conversion = new SvgComputedStyleConverter().ConvertValueWithEntryAndTextAndSourceAndOptionsAndPresentationAttribute(entry, text, source, options, presentationAttribute);
                if (!conversion.Success)
                {
                    conversion = new SvgComputedStyleConverter().RecoverValue(entry, source, options);
                    if (conversion.Success)
                    {
                        (conversion.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Recovered = true;
                        style.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidCss, "A property value was invalid during computed-style conversion and recovered. Property: " + entry.Name + ".", source));
                    }
                    else
                    {
                        conversionSuccess = false;
                        conversion = new SvgComputedStyleConverter().TextValue(entry.Name, entry.InitialValue, source);
                        style.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InternalInvariant, "A property initial value could not be converted. Property: " + entry.Name + ".", source));
                    }
                }
                if (cascaded != null) new SvgComputedStyleConverter().CopyStrings(cascaded.Dependencies, (conversion.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Dependencies);
                (conversion.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IsLegacy = entry.IsLegacy;
                style.Values.Add(conversion.Value);
            }
            entryIndex = entryIndex + 1;
        }
        new SvgComputedStyleConverter().ResolveCurrentColors(style, options);
        style.Success = cascade.Success && conversionSuccess;
        return style;
    }
    bool ValidArguments(SvgNode node, SvgCascadeResult cascade, SvgComputedStyleOptions options)
    {
        return node.Kind == SvgNodeKind.Element && object.ReferenceEquals(cascade.SourceNode, node) && cascade.SourceVersion == node.Version && options.Properties != null && options.Limits != null && options.Limits.MaxDeclarations > 0 && options.Limits.MaxSelectorTokens > 0 && options.MaxPaintFallbacks >= 0;
    }
bool ImplicitlyInherited(SvgPropertySupportEntry entry, SvgCascadedValue? cascaded, SvgComputedStyleOptions options)
    {
        if (!entry.Inherited || cascaded == null || cascaded.Kind != SvgCascadedValueKind.Initial || options.Parent == null || options.Parent.Find(entry.Name) == null) return false;
        if (cascaded.Winner == null) return true;
        string keyword = cascaded.Winner.ValueText.Trim();
        return string.Equals(keyword, "inherit", System.StringComparison.OrdinalIgnoreCase) || string.Equals(keyword, "unset", System.StringComparison.OrdinalIgnoreCase);
    }
SvgComputedConversion RecoverValue(SvgPropertySupportEntry entry, SvgSourceSpan? source, SvgComputedStyleOptions options)
    {
        if (entry.Inherited && options.Parent != null)
        {
            SvgComputedValue? parent = options.Parent.Find(entry.Name);
            if (parent != null) return new SvgComputedStyleConverter().ConvertValueWithEntryAndTextAndSourceAndOptions(entry, parent.CssText, source, options);
        }
        return new SvgComputedStyleConverter().ConvertValueWithEntryAndTextAndSourceAndOptions(entry, entry.InitialValue, source, options);
    }
SvgComputedConversion ConvertValueWithEntryAndTextAndSourceAndOptions(SvgPropertySupportEntry entry, string text, SvgSourceSpan? source, SvgComputedStyleOptions options)
    {
        return new SvgComputedStyleConverter().ConvertValueWithEntryAndTextAndSourceAndOptionsAndPresentationAttribute(entry, text, source, options, false);
    }
SvgComputedConversion ConvertValueWithEntryAndTextAndSourceAndOptionsAndPresentationAttribute(SvgPropertySupportEntry entry, string? text, SvgSourceSpan? source, SvgComputedStyleOptions options, bool presentationAttribute)
    {
        string trimmed = text == null ? "" : text.Trim();
        if (trimmed.Length == 0) return new SvgComputedStyleConverter().Failure();
        if (entry.Name == SvgCssPropertyNames.Transform) return new SvgComputedStyleConverter().TransformValue(entry.Name, trimmed, source, options);
        if (entry.Name == SvgCssPropertyNames.TransformOrigin) return new SvgComputedStyleConverter().TransformOriginValue(entry.Name, trimmed, source, options, presentationAttribute);
        if (entry.Name == SvgCssPropertyNames.TransformBox) return new SvgComputedStyleConverter().TransformBoxValue(entry.Name, trimmed, source, options);
        if (entry.Name == SvgCssPropertyNames.FontFamily) return new SvgComputedStyleConverter().FontFamilyValue(entry.Name, trimmed, source, options);
        if (entry.Name == SvgCssPropertyNames.PaintOrder) return new SvgComputedStyleConverter().PaintOrderValue(entry.Name, trimmed, source);
        if (entry.Name == SvgCssPropertyNames.StrokeDashArray) return new SvgComputedStyleConverter().LengthListValue(entry.Name, trimmed, source, options);
        if (entry.Name == SvgCssPropertyNames.Cursor) return new SvgComputedStyleConverter().CursorValue(entry.Name, trimmed, source, options);
        if (new SvgComputedStyleConverter().MarkerProperty(entry.Name)) return new SvgComputedStyleConverter().ReferenceValue(entry.Name, trimmed, source, options);
        if (entry.Name == SvgCssPropertyNames.ClipPath) return new SvgComputedStyleConverter().OptionalReferenceValue(entry.Name, trimmed, source, options);
        if (new SvgComputedStyleConverter().AngleProperty(entry.Name)) return new SvgComputedStyleConverter().AngleValue(entry.Name, trimmed, source, options);
        if (new SvgComputedStyleConverter().AnyLengthProperty(entry.Name))
        {
            SvgComputedConversion length = new SvgComputedStyleConverter().AnyLengthValue(entry.Name, trimmed, source, options, presentationAttribute);
            if (length.Success) return length;
        }
        if (entry.Name == SvgCssPropertyNames.FontWeight)
        {
            SvgNumberParseResult weight = SvgNumberParser.ParseCssNumber(trimmed, options.Limits.MaxSelectorTokens);
            if (weight.Success) return new SvgComputedStyleConverter().NumberValue(entry.Name, weight.Value, source, false);
            if (new SvgComputedStyleConverter().SimpleIdentifier(trimmed)) return new SvgComputedStyleConverter().KeywordValue(entry.Name, trimmed, source);
            return new SvgComputedStyleConverter().Failure();
        }
        if (entry.ValueKind == SvgPropertyValueSupportKind.Keyword) return new SvgComputedStyleConverter().KeywordValue(entry.Name, trimmed, source);
        if (entry.ValueKind == SvgPropertyValueSupportKind.Number) return new SvgComputedStyleConverter().ParsedNumberValue(entry.Name, trimmed, source, options);
        if (entry.ValueKind == SvgPropertyValueSupportKind.Length) return new SvgComputedStyleConverter().ParsedLengthValue(entry.Name, trimmed, source, options, presentationAttribute);
        if (entry.ValueKind == SvgPropertyValueSupportKind.Color) return new SvgComputedStyleConverter().ColorValue(entry.Name, trimmed, source, options);
        if (entry.ValueKind == SvgPropertyValueSupportKind.Paint) return new SvgComputedStyleConverter().PaintValue(entry.Name, trimmed, source, options);
        return new SvgComputedStyleConverter().TextValue(entry.Name, trimmed, source);
    }
SvgComputedConversion ParsedNumberValue(string name, string text, SvgSourceSpan? source, SvgComputedStyleOptions options)
    {
        SvgNumberParseResult parsed = SvgNumberParser.ParseCssNumber(text, options.Limits.MaxSelectorTokens);
        if (!parsed.Success) return new SvgComputedStyleConverter().Failure();
        if (name == SvgCssPropertyNames.StrokeMiterLimit && parsed.Value < 1) return new SvgComputedStyleConverter().Failure();
        bool clamp = new SvgComputedStyleConverter().OpacityProperty(name);
        return new SvgComputedStyleConverter().NumberValue(name, parsed.Value, source, clamp);
    }
SvgComputedConversion NumberValue(string name, double number, SvgSourceSpan? source, bool clamp)
    {
        double computed = number;
        if (clamp && computed < 0) computed = 0;
        if (clamp && computed > 1) computed = 1;
        SvgNumberFormatResult formatted = SvgNumberFormatter.FormatShortest(computed);
        if (!formatted.Success) return new SvgComputedStyleConverter().Failure();
        SvgComputedValue value = new SvgComputedValue(name, SvgComputedValueKind.Number, formatted.Text, source);
        value.Number = computed;
        return new SvgComputedStyleConverter().Success(value);
    }
SvgComputedConversion ParsedLengthValue(string name, string text, SvgSourceSpan? source, SvgComputedStyleOptions options, bool presentationAttribute)
    {
        SvgLengthParseResult parsed = SvgLengthParser.ParseCssLength(text, true, presentationAttribute, options.Limits.MaxSelectorTokens);
        if (!parsed.Success)
        {
            SvgNumberParseResult zero = SvgNumberParser.ParseCssNumber(text, options.Limits.MaxSelectorTokens);
            if (!zero.Success || zero.Value != 0) return new SvgComputedStyleConverter().Failure();
            parsed = new SvgLengthParseResult(true, SvgLengthParseError.None, SvgNumberParseError.None, new SvgLength(0, SvgLengthUnit.Px), -1);
        }
        if (new SvgComputedStyleConverter().NonnegativeLengthProperty(name) && (parsed.Length ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value < 0) return new SvgComputedStyleConverter().Failure();
        SvgComputedValue value = new SvgComputedValue(name, SvgComputedValueKind.Length, (parsed.Length ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToText(), source);
        value.Length = parsed.Length;
        return new SvgComputedStyleConverter().Success(value);
    }
SvgComputedConversion AnyLengthValue(string name, string text, SvgSourceSpan? source, SvgComputedStyleOptions options, bool presentationAttribute)
    {
        SvgComputedConversion length = new SvgComputedStyleConverter().ParsedLengthValue(name, text, source, options, presentationAttribute);
        if (length.Success) return length;
        if (new SvgComputedStyleConverter().SimpleIdentifier(text)) return new SvgComputedStyleConverter().KeywordValue(name, text, source);
        return new SvgComputedStyleConverter().Failure();
    }
SvgComputedConversion AngleValue(string name, string text, SvgSourceSpan? source, SvgComputedStyleOptions options)
    {
        if (string.Equals(text, "auto", System.StringComparison.OrdinalIgnoreCase)) return new SvgComputedStyleConverter().KeywordValue(name, text, source);
        SvgAngleParseResult parsed = SvgScalarUnitParser.ParseAngle(text, true, options.Limits.MaxSelectorTokens);
        if (!parsed.Success) return new SvgComputedStyleConverter().Failure();
        SvgComputedValue value = new SvgComputedValue(name, SvgComputedValueKind.Angle, (parsed.Angle ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToText(), source);
        value.Angle = parsed.Angle;
        return new SvgComputedStyleConverter().Success(value);
    }
SvgComputedConversion ColorValue(string name, string text, SvgSourceSpan? source, SvgComputedStyleOptions options)
    {
        SvgColorParseResult parsed = new SvgColorParser().Parse(text, options.Limits.MaxSelectorTokens);
        if (!parsed.Success) return new SvgComputedStyleConverter().Failure();
        SvgComputedValue value = new SvgComputedValue(name, SvgComputedValueKind.Color, (parsed.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToText(), source);
        value.Color = parsed.Color;
        return new SvgComputedStyleConverter().Success(value);
    }
SvgComputedConversion PaintValue(string name, string text, SvgSourceSpan? source, SvgComputedStyleOptions options)
    {
        SvgPaintParseResult parsed = new SvgPaintParser().Parse(text, options.MaxPaintFallbacks, options.Limits.MaxSelectorTokens, false);
        if (!parsed.Success || parsed.Recovered) return new SvgComputedStyleConverter().Failure();
        SvgComputedValue value = new SvgComputedValue(name, SvgComputedValueKind.Paint, (parsed.Paint ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToText(), source);
        value.Paint = parsed.Paint;
        return new SvgComputedStyleConverter().Success(value);
    }
SvgComputedConversion KeywordValue(string name, string text, SvgSourceSpan? source)
    {
        if (!new SvgComputedStyleConverter().SimpleIdentifier(text)) return new SvgComputedStyleConverter().Failure();
        string keyword = text.ToLowerInvariant();
        SvgComputedValue value = new SvgComputedValue(name, SvgComputedValueKind.Keyword, keyword, source);
        value.Keyword = keyword;
        return new SvgComputedStyleConverter().Success(value);
    }
SvgComputedConversion TransformValue(string name, string text, SvgSourceSpan? source, SvgComputedStyleOptions options)
    {
        SvgCssTransformParseResult parsed = SvgCssTransformParser.ParseTransform(text, options.Limits.MaxDeclarations, options.Limits.MaxSelectorTokens, false);
        SvgCssTransformList transformList = parsed.TransformList;
        if (!parsed.Success || parsed.Recovered)
        {
            SvgTransformParseResult legacy = SvgTransformParser.Parse(text, options.Limits.MaxDeclarations, options.Limits.MaxSelectorTokens);
            if (!legacy.Success) return new SvgComputedStyleConverter().Failure();
            transformList = (new SvgComputedStyleConverter().ConvertLegacyTransforms(legacy.TransformList) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            if (transformList == null) return new SvgComputedStyleConverter().Failure();
        }
        SvgComputedValue value = new SvgComputedValue(name, SvgComputedValueKind.TransformList, transformList.ToText(), source);
        value.TransformList = transformList;
        return new SvgComputedStyleConverter().Success(value);
    }
SvgCssTransformList? ConvertLegacyTransforms(SvgTransformList? legacy)
    {
        if (legacy == null || legacy.Items == null) return null;
        System.Collections.Generic.List<SvgCssTransform> items = new System.Collections.Generic.List<SvgCssTransform>();
        int index = 0;
        while (index < legacy.Items.Count)
        {
            SvgCssTransform? converted = new SvgComputedStyleConverter().ConvertLegacyTransform(legacy.Items[index]);
            if (converted == null) return null;
            items.Add(converted);
            index = index + 1;
        }
        return new SvgCssTransformList(false, items);
    }
SvgCssTransform? ConvertLegacyTransform(SvgTransform? transform)
    {
        if (transform == null || transform.Values == null) return null;
        if (transform.Kind == SvgTransformKind.Matrix) return new SvgCssTransform(SvgCssTransformKind.Matrix, transform.Values, new SvgLength[0], new SvgAngle[0]);
        if (transform.Kind == SvgTransformKind.Scale) return new SvgCssTransform(SvgCssTransformKind.Scale, transform.Values, new SvgLength[0], new SvgAngle[0]);
        if (transform.Kind == SvgTransformKind.Translate)
        {
            SvgLength[] lengths = new SvgLength[transform.Values.Length];
            int index = 0;
            while (index < transform.Values.Length)
            {
                lengths[index] = new SvgLength(transform.Values[index], SvgLengthUnit.Px);
                index = index + 1;
            }
            return new SvgCssTransform(SvgCssTransformKind.Translate, new double[0], lengths, new SvgAngle[0]);
        }
        if (transform.Kind == SvgTransformKind.Rotate && transform.Values.Length == 1) return new SvgCssTransform(SvgCssTransformKind.Rotate, new double[0], new SvgLength[0], new SvgAngle[] { new SvgAngle(transform.Values[0], SvgAngleUnit.Deg) });
        if (transform.Kind == SvgTransformKind.SkewX) return new SvgCssTransform(SvgCssTransformKind.SkewX, new double[0], new SvgLength[0], new SvgAngle[] { new SvgAngle(transform.Values[0], SvgAngleUnit.Deg) });
        if (transform.Kind == SvgTransformKind.SkewY) return new SvgCssTransform(SvgCssTransformKind.SkewY, new double[0], new SvgLength[0], new SvgAngle[] { new SvgAngle(transform.Values[0], SvgAngleUnit.Deg) });
        SvgTransformMatrixResult matrix = transform.ToMatrix();
        if (!matrix.Success) return null;
        double[] values = new double[] { (matrix.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).A, matrix.Matrix.B, matrix.Matrix.C, matrix.Matrix.D, matrix.Matrix.E, matrix.Matrix.F };
        return new SvgCssTransform(SvgCssTransformKind.Matrix, values, new SvgLength[0], new SvgAngle[0]);
    }
SvgComputedConversion TransformOriginValue(string name, string text, SvgSourceSpan? source, SvgComputedStyleOptions options, bool presentationAttribute)
    {
        SvgTransformOriginParseResult parsed = SvgCssTransformParser.ParseOrigin(text, options.Limits.MaxSelectorTokens);
        if (!parsed.Success && presentationAttribute)
        {
            string compatible = new SvgComputedStyleConverter().AddPixelUnitsToNumbers(text, options.Limits.MaxSelectorTokens);
            if (compatible.Length != 0) parsed = SvgCssTransformParser.ParseOrigin(compatible, options.Limits.MaxSelectorTokens);
        }
        if (!parsed.Success) return new SvgComputedStyleConverter().Failure();
        SvgComputedValue value = new SvgComputedValue(name, SvgComputedValueKind.TransformOrigin, (parsed.Origin ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToText(), source);
        value.TransformOrigin = parsed.Origin;
        return new SvgComputedStyleConverter().Success(value);
    }
SvgComputedConversion TransformBoxValue(string name, string text, SvgSourceSpan? source, SvgComputedStyleOptions options)
    {
        SvgTransformBoxParseResult parsed = SvgCssTransformParser.ParseTransformBox(text, options.Limits.MaxSelectorTokens);
        if (!parsed.Success) return new SvgComputedStyleConverter().Failure();
        SvgComputedValue value = new SvgComputedValue(name, SvgComputedValueKind.TransformBox, SvgCssTransformRules.BoxText(parsed.TransformBox), source);
        value.TransformBox = parsed.TransformBox;
        return new SvgComputedStyleConverter().Success(value);
    }
SvgComputedConversion FontFamilyValue(string name, string text, SvgSourceSpan? source, SvgComputedStyleOptions options)
    {
        SvgFontFamilyParseResult parsed = new SvgFontFamilyParser().Parse(text, options.Limits.MaxDeclarations, options.Limits.MaxSelectorTokens);
        if (!parsed.Success || parsed.Keyword != SvgListKeyword.Values) return new SvgComputedStyleConverter().Failure();
        SvgComputedValue value = new SvgComputedValue(name, SvgComputedValueKind.FontFamilyList, text, source);
        new SvgComputedStyleConverter().CopyFontFamilies(parsed.Families, value.FontFamilies);
        return new SvgComputedStyleConverter().Success(value);
    }
SvgComputedConversion PaintOrderValue(string name, string text, SvgSourceSpan? source)
    {
        SvgPaintOrderParseResult parsed = new SvgPaintOrderParser().Parse(text);
        if (!parsed.Success) return new SvgComputedStyleConverter().Failure();
        System.Collections.Generic.List<SvgPaintOrderComponent> items = new System.Collections.Generic.List<SvgPaintOrderComponent>();
        if (parsed.Keyword == SvgListKeyword.Normal)
        {
            items.Add(SvgPaintOrderComponent.Fill);
            items.Add(SvgPaintOrderComponent.Stroke);
            items.Add(SvgPaintOrderComponent.Markers);
        }
        else if (parsed.Keyword == SvgListKeyword.Values)
        {
            new SvgComputedStyleConverter().CopyPaintOrder(parsed.Items, items);
            new SvgComputedStyleConverter().AddPaintOrder(items, SvgPaintOrderComponent.Fill);
            new SvgComputedStyleConverter().AddPaintOrder(items, SvgPaintOrderComponent.Stroke);
            new SvgComputedStyleConverter().AddPaintOrder(items, SvgPaintOrderComponent.Markers);
        }
        else return new SvgComputedStyleConverter().Failure();
        string cssText = new SvgComputedStyleConverter().PaintOrderText(items);
        SvgComputedValue value = new SvgComputedValue(name, SvgComputedValueKind.PaintOrder, cssText, source);
        new SvgComputedStyleConverter().CopyPaintOrder(items, value.PaintOrder);
        return new SvgComputedStyleConverter().Success(value);
    }
SvgComputedConversion LengthListValue(string name, string text, SvgSourceSpan? source, SvgComputedStyleOptions options)
    {
        SvgLengthListParseResult parsed = new SvgLengthListParser().ParseDashArray(text, options.Limits.MaxDeclarations, options.Limits.MaxSelectorTokens);
        if (!parsed.Success) return new SvgComputedStyleConverter().Failure();
        SvgComputedValue value = new SvgComputedValue(name, SvgComputedValueKind.LengthList, (parsed.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToText(), source);
        value.LengthList = parsed.Value;
        return new SvgComputedStyleConverter().Success(value);
    }
SvgComputedConversion ReferenceValue(string name, string text, SvgSourceSpan? source, SvgComputedStyleOptions options)
    {
        if (string.Equals(text, "none", System.StringComparison.OrdinalIgnoreCase)) return new SvgComputedStyleConverter().KeywordValue(name, text, source);
        SvgIriParseResult parsed = new SvgIriParser().ParseUrl(text, options.Limits.MaxSelectorTokens);
        if (!parsed.Success) return new SvgComputedStyleConverter().Failure();
        SvgComputedValue value = new SvgComputedValue(name, SvgComputedValueKind.Reference, "url(" + (parsed.Reference ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value + ")", source);
        value.Reference = parsed.Reference;
        return new SvgComputedStyleConverter().Success(value);
    }
SvgComputedConversion OptionalReferenceValue(string name, string text, SvgSourceSpan? source, SvgComputedStyleOptions options)
    {
        if (string.Equals(text, "none", System.StringComparison.OrdinalIgnoreCase)) return new SvgComputedStyleConverter().KeywordValue(name, text, source);
        SvgIriParseResult parsed = new SvgIriParser().ParseUrl(text, options.Limits.MaxSelectorTokens);
        if (!parsed.Success) return new SvgComputedStyleConverter().TextValue(name, text, source);
        SvgComputedValue value = new SvgComputedValue(name, SvgComputedValueKind.Reference, "url(" + (parsed.Reference ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value + ")", source);
        value.Reference = parsed.Reference;
        return new SvgComputedStyleConverter().Success(value);
    }
SvgComputedConversion CursorValue(string name, string text, SvgSourceSpan? source, SvgComputedStyleOptions options)
    {
        SvgComputedCursor? cursor = SvgComputedCursorParser.Parse(text, options.Limits.MaxDeclarations, options.Limits.MaxSelectorTokens);
        if (cursor == null) return new SvgComputedStyleConverter().Failure();
        SvgComputedValue value = new SvgComputedValue(name, SvgComputedValueKind.Cursor, cursor.CssText, source);
        value.Cursor = cursor;
        return new SvgComputedStyleConverter().Success(value);
    }
SvgComputedConversion TextValue(string name, string text, SvgSourceSpan? source)
    {
        return new SvgComputedStyleConverter().Success(new SvgComputedValue(name, SvgComputedValueKind.Text, text.Trim(), source));
    }
SvgComputedConversion Success(SvgComputedValue value)
    {
        return new SvgComputedConversion(true, value);
    }
SvgComputedConversion Failure()
    {
        return new SvgComputedConversion(false, null);
    }
bool OpacityProperty(string name)
    {
        return name == SvgCssPropertyNames.Opacity || name == SvgCssPropertyNames.FillOpacity || name == SvgCssPropertyNames.StrokeOpacity || name == SvgCssPropertyNames.StopOpacity || name == SvgCssPropertyNames.FloodOpacity;
    }
bool MarkerProperty(string name)
    {
        return name == SvgCssPropertyNames.MarkerStart || name == SvgCssPropertyNames.MarkerMid || name == SvgCssPropertyNames.MarkerEnd;
    }
bool AngleProperty(string name)
    {
        return name == SvgCssPropertyNames.GlyphOrientationHorizontal || name == SvgCssPropertyNames.GlyphOrientationVertical;
    }
bool AnyLengthProperty(string name)
    {
        return name == SvgCssPropertyNames.BaselineShift || name == SvgCssPropertyNames.FontSize || name == SvgCssPropertyNames.InlineSize || name == SvgCssPropertyNames.Kerning || name == SvgCssPropertyNames.LetterSpacing || name == SvgCssPropertyNames.WordSpacing || name == SvgCssPropertyNames.Width || name == SvgCssPropertyNames.Height || name == SvgCssPropertyNames.Rx || name == SvgCssPropertyNames.Ry;
    }
bool NonnegativeLengthProperty(string name)
    {
        return name == SvgCssPropertyNames.StrokeWidth || name == SvgCssPropertyNames.R || name == SvgCssPropertyNames.Width || name == SvgCssPropertyNames.Height || name == SvgCssPropertyNames.Rx || name == SvgCssPropertyNames.Ry || name == SvgCssPropertyNames.FontSize || name == SvgCssPropertyNames.InlineSize;
    }
string AddPixelUnitsToNumbers(string text, int maxTokenCharacters)
    {
        string[]? parts = text.Split((char[]?)null, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Length > 3) return "";
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        bool replaced = false;
        int index = 0;
        while (index < parts.Length)
        {
            if (index > 0) builder.Append(' ');
            SvgNumberParseResult number = SvgNumberParser.ParseCssNumber(parts[index], maxTokenCharacters);
            if (number.Success && number.Value != 0)
            {
                builder.Append(parts[index]);
                builder.Append("px");
                replaced = true;
            }
            else builder.Append(parts[index]);
            index = index + 1;
        }
        return replaced ? builder.ToString() : "";
    }
void ResolveCurrentColors(SvgComputedStyle style, SvgComputedStyleOptions options)
    {
        SvgComputedValue? colorValue = style.Find(SvgCssPropertyNames.Color);
        if (colorValue == null || colorValue.Kind != SvgComputedValueKind.Color || colorValue.Color == null) return;
        if (colorValue.Color.IsCurrentColor)
        {
            SvgColor? inherited = options.Parent == null ? null : options.Parent.GetColor(SvgCssPropertyNames.Color);
            if (inherited == null || inherited.IsCurrentColor)
            {
                SvgColorParseResult initial = new SvgColorParser().Parse(SvgInitialValues.Color, options.Limits.MaxSelectorTokens);
                inherited = initial.Success ? initial.Color : new SvgColor(SvgColorSpace.Srgb, 0, 0, 0, 1);
            }
            colorValue.Color = new SvgComputedStyleConverter().CopyColor(inherited);
            colorValue.CssText = colorValue.Color.ToText();
        }
        SvgColor current = colorValue.Color;
        int index = 0;
        while (index < style.Values.Count)
        {
            SvgComputedValue value = style.Values[index];
            if (value.Kind == SvgComputedValueKind.Color && value.Color != null && value.Color.IsCurrentColor)
            {
                value.Color = new SvgComputedStyleConverter().CopyColor(current);
                value.CssText = value.Color.ToText();
            }
            else if (value.Kind == SvgComputedValueKind.Paint && value.Paint != null)
            {
                new SvgComputedStyleConverter().ResolvePaintCurrentColor(value.Paint, current);
                value.CssText = value.Paint.ToText();
            }
            index = index + 1;
        }
    }
void ResolvePaintCurrentColor(SvgPaint paint, SvgColor current)
    {
        if (paint.Kind == SvgPaintKind.Color && paint.Color != null && paint.Color.IsCurrentColor) paint.Color = new SvgComputedStyleConverter().CopyColor(current);
        int index = 0;
        while (index < paint.Fallbacks.Count)
        {
            SvgPaint fallback = paint.Fallbacks[index];
            if (fallback.Kind == SvgPaintKind.Color && fallback.Color != null && fallback.Color.IsCurrentColor) fallback.Color = new SvgComputedStyleConverter().CopyColor(current);
            index = index + 1;
        }
    }
SvgColor CopyColor(SvgColor? color)
    {
        SvgColor copy = new SvgColor((color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Space, color.Red, color.Green, color.Blue, color.Alpha);
        copy.FirstMissing = color.FirstMissing;
        copy.SecondMissing = color.SecondMissing;
        copy.ThirdMissing = color.ThirdMissing;
        copy.AlphaMissing = color.AlphaMissing;
        copy.Source = color.Source;
        return copy;
    }
bool SimpleIdentifier(string text)
    {
        if (text.Length == 0) return false;
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            if (!(value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9' || value == '-' || value == '_' || value >= 128)) return false;
            index = index + 1;
        }
        return true;
    }
SvgComputedValue? Find(System.Collections.Generic.List<SvgComputedValue> values, string name)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (string.Equals(values[index].PropertyName, name, System.StringComparison.OrdinalIgnoreCase)) return values[index];
            index = index + 1;
        }
        return null;
    }
SvgCustomPropertySet CopyCustomProperties(SvgCustomPropertySet? source, SvgComputedStyleOptions options)
    {
        SvgCustomPropertySet result = new SvgCustomPropertySet();
        if (source == null) return result;
        System.Collections.Generic.List<SvgCustomPropertyValue> values = source.GetValues();
        int index = 0;
        while (index < values.Count)
        {
            result.Set(values[index].Name, values[index].RawText, options.Limits.MaxVariableExpansionTokens, options.Limits.MaxSelectorTokens);
            index = index + 1;
        }
        return result;
    }
void CopyStrings(System.Collections.Generic.List<string> source, System.Collections.Generic.List<string> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }
void CopyFontFamilies(System.Collections.Generic.List<SvgFontFamily> source, System.Collections.Generic.List<SvgFontFamily> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }
void CopyPaintOrder(System.Collections.Generic.List<SvgPaintOrderComponent> source, System.Collections.Generic.List<SvgPaintOrderComponent> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }
void AddPaintOrder(System.Collections.Generic.List<SvgPaintOrderComponent> items, SvgPaintOrderComponent component)
    {
        if (!items.Contains(component)) items.Add(component);
    }
string PaintOrderText(System.Collections.Generic.List<SvgPaintOrderComponent> items)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < items.Count)
        {
            if (index > 0) builder.Append(' ');
            if (items[index] == SvgPaintOrderComponent.Fill) builder.Append("fill");
            else if (items[index] == SvgPaintOrderComponent.Stroke) builder.Append("stroke");
            else builder.Append("markers");
            index = index + 1;
        }
        return builder.ToString();
    }
void AppendDiagnostics(System.Collections.Generic.List<SvgDiagnostic> target, System.Collections.Generic.List<SvgDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }
}
