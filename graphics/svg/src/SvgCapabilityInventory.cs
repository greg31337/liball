namespace svg;

internal class SvgFeatureInventory
{
    internal string Identifier(SvgFeatureFamily family, string name)
    {
        if (family < SvgFeatureFamily.Element || family > SvgFeatureFamily.HostCallback) throw new System.ArgumentOutOfRangeException("family");
        if (name == null || name.Trim().Length == 0) throw new System.ArgumentOutOfRangeException("name");
        return Prefix(family) + Normalize(name);
    }

    internal void AddLibraryEntries(SvgFeatureReport report)
    {
        AddElements(report);
        AddProperties(report);
        AddAtRules(report);
        AddSelectors(report);
        AddFilterPrimitives(report);
        AddAnimationValues(report);
        AddImageFormats(report);
        AddFontFormats(report);
        AddProcessorJobs(report);
        AddHostCallbacks(report);
    }

    void AddElements(SvgFeatureReport report)
    {
        System.Collections.Generic.List<SvgElementDefinition> current = new SvgCatalogFactory().CreateElementCatalog().GetEntries();
        int index = 0;
        while (index < current.Count)
        {
            SvgElementDefinition definition = current[index];
            SvgFeatureStatus status = SvgFeatureStatus.Implemented;
            string profile = SvgFeatureIdentifiers.ProfileSvg2;
            if (definition.Kind == SvgElementKind.Script)
            {
                status = SvgFeatureStatus.PreserveOnly;
                profile = SvgFeatureIdentifiers.ProfileLegacyPreservation;
            }
            else if (definition.RenderMode == SvgElementRenderMode.HostCallback)
            {
                status = SvgFeatureStatus.CallbackRequired;
                profile = SvgFeatureIdentifiers.ProfileHostCallback;
            }
            AddEntry(report, SvgFeatureFamily.Element, definition.LocalName, profile, status, "Capability for the individual SVG element.");
            index = index + 1;
        }
        System.Collections.Generic.List<SvgLegacyFeatureDefinition> legacy = new SvgCatalogFactory().CreateLegacyCatalog().GetElementEntries();
        index = 0;
        while (index < legacy.Count)
        {
            SvgFeatureStatus status = LegacyStatus(legacy[index].Handling);
            AddEntry(report, SvgFeatureFamily.Element, legacy[index].LocalName, SvgFeatureIdentifiers.ProfileLegacyPreservation, status, legacy[index].Detail);
            index = index + 1;
        }
    }

    void AddProperties(SvgFeatureReport report)
    {
        SvgPropertyCatalog catalog = SvgPropertyCatalog.CreateDefault();
        int index = 0;
        while (index < catalog.Entries.Count)
        {
            SvgPropertySupportEntry entry = catalog.Entries[index];
            SvgFeatureStatus status = entry.IsLegacy ? SvgFeatureStatus.PreserveOnly : SvgFeatureStatus.Implemented;
            string profile = entry.IsLegacy ? SvgFeatureIdentifiers.ProfileLegacyPreservation : SvgFeatureIdentifiers.ProfileSvg2;
            AddEntry(report, SvgFeatureFamily.CssProperty, entry.Name, profile, status, "Capability for the individual CSS property grammar and computed behavior.");
            index = index + 1;
        }
        AddEntry(report, SvgFeatureFamily.CssProperty, "custom-property", SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "CSS custom properties are parsed, cascaded, and resolved with bounds.");
        AddEntry(report, SvgFeatureFamily.CssProperty, "unknown", SvgFeatureIdentifiers.ProfileLegacyPreservation, SvgFeatureStatus.PreserveOnly, "Unknown CSS declarations can be retained but have no computed semantics.");
    }

    void AddAtRules(SvgFeatureReport report)
    {
        string[] names = new string[] { "charset", "import", "namespace", "layer", "media", "supports", "font-face", "keyframes" };
        int index = 0;
        while (index < names.Length)
        {
            AddEntry(report, SvgFeatureFamily.CssAtRule, names[index], SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Capability for the individual CSS at-rule.");
            index = index + 1;
        }
        AddEntry(report, SvgFeatureFamily.CssAtRule, "unknown", SvgFeatureIdentifiers.ProfileLegacyPreservation, SvgFeatureStatus.PreserveOnly, "Unknown at-rules are source-preserved without semantic processing.");
    }

    void AddSelectors(SvgFeatureReport report)
    {
        string[] names = new string[]
        {
            "type", "universal", "id", "class", "attribute",
            "namespace-default", "namespace-none", "namespace-any", "namespace-named",
            "attribute-exists", "attribute-equals", "attribute-includes", "attribute-dash-match", "attribute-prefix-match", "attribute-suffix-match", "attribute-substring-match",
            "attribute-case-insensitive", "attribute-case-sensitive",
            "combinator-descendant", "combinator-child", "combinator-adjacent-sibling", "combinator-general-sibling",
            "pseudo-root", "pseudo-scope", "pseudo-empty", "pseudo-first-child", "pseudo-last-child", "pseudo-only-child",
            "pseudo-nth-child", "pseudo-nth-last-child", "pseudo-first-of-type", "pseudo-last-of-type", "pseudo-only-of-type",
            "pseudo-nth-of-type", "pseudo-nth-last-of-type", "pseudo-lang", "pseudo-not", "pseudo-is", "pseudo-where", "pseudo-has",
            "pseudo-link", "pseudo-visited", "pseudo-hover", "pseudo-active", "pseudo-focus", "pseudo-target"
        };
        int index = 0;
        while (index < names.Length)
        {
            AddEntry(report, SvgFeatureFamily.CssSelector, names[index], SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Capability for the individual CSS selector form.");
            index = index + 1;
        }
        AddEntry(report, SvgFeatureFamily.CssSelector, "pseudo-unsupported-dynamic", SvgFeatureIdentifiers.ProfileLegacyPreservation, SvgFeatureStatus.PreserveOnly, "Recognized unsupported dynamic pseudo-classes are preserved but do not match.");
    }

    void AddFilterPrimitives(SvgFeatureReport report)
    {
        System.Collections.Generic.List<SvgElementDefinition> entries = new SvgCatalogFactory().CreateElementCatalog().GetEntries();
        int index = 0;
        while (index < entries.Count)
        {
            SvgElementDefinition entry = entries[index];
            if (entry.LocalName.StartsWith("fe", System.StringComparison.Ordinal) && entry.Kind != SvgElementKind.FeFuncA && entry.Kind != SvgElementKind.FeFuncB && entry.Kind != SvgElementKind.FeFuncG && entry.Kind != SvgElementKind.FeFuncR && entry.Kind != SvgElementKind.FeDistantLight && entry.Kind != SvgElementKind.FePointLight && entry.Kind != SvgElementKind.FeSpotLight && entry.Kind != SvgElementKind.FeMergeNode)
            {
                AddEntry(report, SvgFeatureFamily.FilterPrimitive, entry.LocalName, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Capability for the individual SVG filter primitive.");
            }
            index = index + 1;
        }
    }

    void AddAnimationValues(SvgFeatureReport report)
    {
        string[] names = new string[] { "number", "length", "color", "path", "points", "transform", "view-box", "paint", "string" };
        int index = 0;
        while (index < names.Length)
        {
            AddEntry(report, SvgFeatureFamily.AnimationValue, names[index], SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Capability for the typed animation value family.");
            index = index + 1;
        }
    }

    void AddImageFormats(SvgFeatureReport report)
    {
        AddEntry(report, SvgFeatureFamily.ImageFormat, "svg", SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Nested SVG images are supported by the managed document pipeline.");
        AddEntry(report, SvgFeatureFamily.ImageFormat, "png", SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "PNG is supported by the managed codec.");
        string[] callback = new string[] { "jpeg", "gif", "webp", "avif" };
        int index = 0;
        while (index < callback.Length)
        {
            AddEntry(report, SvgFeatureFamily.ImageFormat, callback[index], SvgFeatureIdentifiers.ProfileHostCallback, SvgFeatureStatus.CallbackRequired, "The image format requires a host decoder callback.");
            index = index + 1;
        }
        AddEntry(report, SvgFeatureFamily.ImageFormat, "host", SvgFeatureIdentifiers.ProfileHostCallback, SvgFeatureStatus.CallbackRequired, "Unrecognized image formats require a host decoder callback.");
    }

    void AddFontFormats(SvgFeatureReport report)
    {
        string[] names = new string[] { "svg", "truetype", "opentype", "woff", "woff2", "cff", "cff2" };
        int index = 0;
        while (index < names.Length)
        {
            AddEntry(report, SvgFeatureFamily.FontFormat, names[index], SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Capability for the managed font format or outline profile.");
            index = index + 1;
        }
        AddEntry(report, SvgFeatureFamily.FontFormat, "host", SvgFeatureIdentifiers.ProfileHostCallback, SvgFeatureStatus.CallbackRequired, "Unrecognized font formats require a host font callback.");
    }

    void AddProcessorJobs(SvgFeatureReport report)
    {
        string[] names = new string[]
        {
            "custom", "remove-comments", "remove-editor-metadata", "remove-unused-namespaces", "remove-default-attributes",
            "remove-empty-containers", "remove-unreferenced-invisible-nodes", "remove-unused-definitions", "deduplicate-definitions",
            "collapse-groups", "hoist-common-attributes", "merge-adjacent-paths", "cleanup-styles", "inline-styles",
            "resolve-custom-properties", "canonicalize-values", "prefix-ids", "minify-ids", "convert-shapes-to-paths",
            "bake-transforms", "simplify-paths", "convert-text-to-paths", "flatten-effects", "embed-images", "recolor",
            "convert-theme-colors", "resize-view-box", "convert-coordinate-system", "crop-drawing-bounds", "flatten-references",
            "extract-fragment", "split-symbols", "merge-documents", "build-sprite", "isolate-namespace"
        };
        int index = 0;
        while (index < names.Length)
        {
            bool callback = index == 0 || index == 21 || index == 22 || index == 23;
            SvgFeatureStatus status = callback ? SvgFeatureStatus.CallbackRequired : SvgFeatureStatus.Implemented;
            string profile = callback ? SvgFeatureIdentifiers.ProfileHostCallback : SvgFeatureIdentifiers.ProfileSvg2;
            AddEntry(report, SvgFeatureFamily.ProcessorJob, names[index], profile, status, "Capability for the individual document processor job.");
            index = index + 1;
        }
    }

    void AddHostCallbacks(SvgFeatureReport report)
    {
        string[] names = new string[] { "external-resource", "image-codec", "text-shaping", "foreign-content", "audio", "video", "iframe", "canvas", "navigation", "text-outline", "effect-raster", "embedded-image", "process-custom" };
        int index = 0;
        while (index < names.Length)
        {
            AddEntry(report, SvgFeatureFamily.HostCallback, names[index], SvgFeatureIdentifiers.ProfileHostCallback, SvgFeatureStatus.CallbackRequired, "This boundary requires an explicit caller callback when dynamically used.");
            index = index + 1;
        }
    }

    void AddEntry(SvgFeatureReport report, SvgFeatureFamily family, string name, string profile, SvgFeatureStatus status, string detail)
    {
        report.Add(new SvgFeatureEntry(Identifier(family, name), profile, status, detail, null));
    }

    SvgFeatureStatus LegacyStatus(SvgLegacyHandling handling)
    {
        if (handling == SvgLegacyHandling.Implemented) return SvgFeatureStatus.Implemented;
        if (handling == SvgLegacyHandling.Unsupported) return SvgFeatureStatus.Unsupported;
        return SvgFeatureStatus.PreserveOnly;
    }

    string Prefix(SvgFeatureFamily family)
    {
        if (family == SvgFeatureFamily.Element) return SvgFeatureIdentifiers.ElementPrefix;
        if (family == SvgFeatureFamily.CssProperty) return SvgFeatureIdentifiers.CssPropertyPrefix;
        if (family == SvgFeatureFamily.CssAtRule) return SvgFeatureIdentifiers.CssAtRulePrefix;
        if (family == SvgFeatureFamily.CssSelector) return SvgFeatureIdentifiers.CssSelectorPrefix;
        if (family == SvgFeatureFamily.FilterPrimitive) return SvgFeatureIdentifiers.FilterPrimitivePrefix;
        if (family == SvgFeatureFamily.AnimationValue) return SvgFeatureIdentifiers.AnimationValuePrefix;
        if (family == SvgFeatureFamily.ImageFormat) return SvgFeatureIdentifiers.ImageFormatPrefix;
        if (family == SvgFeatureFamily.FontFormat) return SvgFeatureIdentifiers.FontFormatPrefix;
        if (family == SvgFeatureFamily.ProcessorJob) return SvgFeatureIdentifiers.ProcessorJobPrefix;
        return SvgFeatureIdentifiers.HostCallbackPrefix;
    }

    string Normalize(string value)
    {
        string trimmed = value.Trim();
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        bool separator = false;
        while (index < trimmed.Length)
        {
            char character = trimmed[index];
            if (character >= 'A' && character <= 'Z') character = (char)(character + 32);
            bool accepted = character >= 'a' && character <= 'z' || character >= '0' && character <= '9';
            if (accepted)
            {
                if (separator && result.Length != 0) result.Append('-');
                result.Append(character);
                separator = false;
            }
            else separator = true;
            index = index + 1;
        }
        if (result.Length == 0) throw new System.ArgumentOutOfRangeException("name");
        return result.ToString();
    }
}
