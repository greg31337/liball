namespace svg;

internal class SvgStyleImportState
{
    internal SvgLoadOptions Options;
    internal System.Func<string, bool>? MediaMatches;
    internal System.Collections.Generic.List<SvgCollectedStyleSheet> StyleSheets;
    internal System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    internal long Characters;
    internal int Rules;
    internal int Declarations;
    internal int RequestCount;
    internal int LoadedCount;
    internal int ResourceCount;
    internal long EncodedBytes;
    internal SvgResourceCache Cache;
    internal bool CancellationReported;

    internal SvgStyleImportState(SvgLoadOptions options, System.Func<string, bool>? mediaMatches, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        Options = options;
        MediaMatches = mediaMatches;
        StyleSheets = new System.Collections.Generic.List<SvgCollectedStyleSheet>();
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>(diagnostics);
        Characters = 0;
        Rules = 0;
        Declarations = 0;
        RequestCount = 0;
        LoadedCount = 0;
        ResourceCount = 0;
        EncodedBytes = 0;
        Cache = new SvgResourceCache(options.Limits.Resources.MaxCount, options.Limits.Resources.MaxTotalBytes);
        CancellationReported = false;
    }

    internal void ReserveExisting(System.Collections.Generic.List<SvgCollectedStyleSheet> entries)
    {
        int index = 0;
        while (index < entries.Count)
        {
            SvgStyleSheet sheet = entries[index].StyleSheet;
            Characters = Characters + sheet.SourceText.Length;
            Rules = Rules + CountRules(sheet.Rules);
            Declarations = Declarations + CountDeclarations(sheet.Rules);
            index = index + 1;
        }
    }

    internal void Expand(SvgCollectedStyleSheet entry, int depth, System.Collections.Generic.List<string> activeUris)
    {
        SvgStyleSheet sheet = entry.StyleSheet;
        if (StopForCancellation(entry.ReferenceSource))
        {
            entry.CollectionOrder = StyleSheets.Count;
            StyleSheets.Add(entry);
            return;
        }
        int index = 0;
        while (index < sheet.Rules.Count)
        {
            SvgCssRule rule = sheet.Rules[index];
            if (StopForCancellation(rule.Source)) break;
            if (IsImport(rule)) ResolveImport(sheet, rule, depth, activeUris);
            index = index + 1;
        }
        entry.CollectionOrder = StyleSheets.Count;
        StyleSheets.Add(entry);
    }

    void ResolveImport(SvgStyleSheet parentSheet, SvgCssRule rule, int parentDepth, System.Collections.Generic.List<string> activeUris)
    {
        SvgCssAtRule? import = rule.AtRule;
        if (StopForCancellation(rule.Source)) return;
        if ((import ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ImportMedia.Length != 0 && MediaMatches != null)
        {
            bool matches = MediaMatches(import.ImportMedia);
            if (StopForCancellation(rule.Source) || !matches) return;
        }
        int depth = parentDepth + 1;
        if (depth > Options.Limits.Css.MaxImportDepth)
        {
            AddError(SvgDiagnosticCode.InputLimitExceeded, "The stylesheet import depth exceeds MaxImportDepth.", rule.Source);
            return;
        }
        if (import.ImportUri.Length > Options.Limits.Resources.MaxUriCharacters || !new SvgStyleSheetImportResolver().ValidUriText(import.ImportUri))
        {
            AddError(SvgDiagnosticCode.InvalidReference, "The stylesheet import URI is invalid or exceeds MaxUriCharacters.", rule.Source);
            return;
        }
        string parentUri = SourceUri(parentSheet);
        string normalizedUri = new SvgStyleSheetImportResolver().NormalizeUri(parentUri, import.ImportUri);
        if (normalizedUri.Length > Options.Limits.Resources.MaxUriCharacters)
        {
            AddError(SvgDiagnosticCode.InputLimitExceeded, "The resolved stylesheet URI exceeds MaxUriCharacters.", rule.Source);
            return;
        }
        if (!new SvgStyleSheetImportResolver().PolicyAllows(Options.ResourcePolicy, parentUri, import.ImportUri, normalizedUri))
        {
            AddError(SvgDiagnosticCode.ResourceDenied, "The resource policy denied the stylesheet import URI.", rule.Source);
            return;
        }
        SvgResourceRequest request = new SvgResourceRequest(import.ImportUri, SvgMimeTypes.Css, Options.CancellationToken);
        request.NormalizedUri = normalizedUri;
        request.BaseUri = parentUri;
        request.ResourceKind = SvgResourceKind.StyleSheet;
        request.Source = rule.Source;
        request.ReferenceDepth = depth;
        request.ResourceIndex = rule.SourceOrder;
        SvgResourceAcquisitionOptions acquisitionOptions = new SvgResourceAcquisitionOptions();
        acquisitionOptions.ResourceProvider = Options.ResourceProvider;
        acquisitionOptions.Limits = Options.Limits.Resources;
        acquisitionOptions.CancellationToken = Options.CancellationToken;
        acquisitionOptions.CopyInputData = Options.CopyInputData;
        acquisitionOptions.MaxBytes = Options.Limits.Resources.MaxBytesPerResource;
        acquisitionOptions.AlreadyLoadedBytes = EncodedBytes;
        acquisitionOptions.DecodeDataUrls = false;
        acquisitionOptions.Cache = Cache;
        acquisitionOptions.ReferenceStack = activeUris;
        acquisitionOptions.AlreadyLoadedCount = ResourceCount;
        SvgResourceAcquisitionResult acquisition = new SvgResourceResolver().Resolve(request, acquisitionOptions);
        RequestCount = RequestCount + acquisition.RequestCount;
        ResourceCount = ResourceCount + acquisition.BudgetCount;
        AppendDiagnostics(acquisition.Diagnostics);
        if (Options.CancellationToken.IsCancellationRequested) CancellationReported = true;
        if (!acquisition.Success) return;
        string finalUri = acquisition.FinalUri;
        if (!new SvgStyleSheetImportResolver().ValidUriText(finalUri) || finalUri.Length > Options.Limits.Resources.MaxUriCharacters || !new SvgStyleSheetImportResolver().PolicyAllows(Options.ResourcePolicy, parentUri, finalUri, finalUri))
        {
            AddError(SvgDiagnosticCode.ResourceDenied, "The resource policy denied the final stylesheet URI.", rule.Source);
            return;
        }
        if (!IsCssMediaType(acquisition.EffectiveMediaType))
        {
            AddError(SvgDiagnosticCode.ResourceFailed, "The imported resource media type is not text/css.", rule.Source);
            return;
        }
        byte[] bytes = acquisition.Bytes;
        LoadedCount = LoadedCount + acquisition.LoadedCount;
        if (acquisition.LoadedCount != 0) EncodedBytes = EncodedBytes + bytes.Length;
        string css = DecodeCss(bytes);
        SvgCssLimits? remaining = RemainingCssLimits(css.Length, (rule.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (remaining == null) return;
        SvgSourceUri? source = acquisition.ResourceSource;
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource(css, remaining, parentSheet.Origin, (source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        AppendDiagnostics(parsed.Diagnostics);
        if (!parsed.Success || parsed.StyleSheet == null) return;
        SvgStyleSheet importedSheet = parsed.StyleSheet;
        Characters = Characters + css.Length;
        Rules = Rules + CountRules(importedSheet.Rules);
        Declarations = Declarations + CountDeclarations(importedSheet.Rules);
        import.ImportResolvedUri = finalUri;
        import.ImportedStyleSheet = importedSheet;
        SvgCollectedStyleSheet importedEntry = new SvgCollectedStyleSheet(importedSheet, SvgStyleSheetSourceKind.Imported, null, rule.Source, import.ImportMedia, 0);
        importedEntry.ResolvedUri = finalUri;
        importedEntry.ImportDepth = depth;
        activeUris.Add(finalUri);
        Expand(importedEntry, depth, activeUris);
        activeUris.RemoveAt(activeUris.Count - 1);
    }

    bool StopForCancellation(SvgSourceSpan? source)
    {
        if (!Options.CancellationToken.IsCancellationRequested) return false;
        if (!CancellationReported)
        {
            AddError(SvgDiagnosticCode.Cancelled, "Stylesheet import loading was cancelled.", source);
            CancellationReported = true;
        }
        return true;
    }

    SvgCssLimits? RemainingCssLimits(int characters, SvgSourceSpan source)
    {
        SvgCssLimits limits = Options.Limits.Css;
        if (characters > limits.MaxCharacters - Characters || Rules >= limits.MaxRules || Declarations >= limits.MaxDeclarations)
        {
            AddError(SvgDiagnosticCode.InputLimitExceeded, "The cumulative imported stylesheet limit was exceeded.", source);
            return null;
        }
        SvgCssLimits remaining = new SvgCssLimits();
        remaining.MaxCharacters = limits.MaxCharacters - Characters;
        remaining.MaxTokens = limits.MaxTokens;
        remaining.MaxRules = limits.MaxRules - Rules;
        remaining.MaxDeclarations = limits.MaxDeclarations - Declarations;
        remaining.MaxSelectorTokens = limits.MaxSelectorTokens;
        remaining.MaxBlockDepth = limits.MaxBlockDepth;
        remaining.MaxImportDepth = limits.MaxImportDepth;
        remaining.MaxVariableExpansionTokens = limits.MaxVariableExpansionTokens;
        return remaining;
    }

    static string DecodeCss(byte[] bytes)
    {
        int offset = bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf ? 3 : 0;
        return System.Text.Encoding.UTF8.GetString(bytes, offset, bytes.Length - offset);
    }

    static bool IsCssMediaType(string mediaType)
    {
        if (mediaType.Length == 0) return true;
        int semicolon = mediaType.IndexOf(';');
        if (semicolon >= 0) mediaType = mediaType.Substring(0, semicolon);
        mediaType = mediaType.Trim();
        return string.Equals(mediaType, SvgMimeTypes.Css, System.StringComparison.OrdinalIgnoreCase);
    }

    void AppendDiagnostics(System.Collections.Generic.List<SvgDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            Diagnostics.Add(source[index]);
            index = index + 1;
        }
    }

    void AddError(SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
    }

    static bool IsImport(SvgCssRule rule)
    {
        return rule != null && rule.IsValid && rule.Kind == SvgCssRuleKind.AtRule && rule.AtRule != null && rule.AtRule.IsValid && rule.AtRule.Kind == SvgCssAtRuleKind.Import;
    }

    static string SourceUri(SvgStyleSheet sheet)
    {
        if (sheet.Source == null || sheet.Source.Source == null) return "";
        return sheet.Source.Source.Uri;
    }

    static int CountRules(System.Collections.Generic.List<SvgCssRule> rules)
    {
        int count = 0;
        System.Collections.Generic.List<SvgCssRule> stack = new System.Collections.Generic.List<SvgCssRule>(rules);
        while (stack.Count > 0)
        {
            int last = stack.Count - 1;
            SvgCssRule rule = stack[last];
            stack.RemoveAt(last);
            if (rule == null) continue;
            count = count + 1;
            if (rule.AtRule != null && rule.AtRule.Keyframes != null) count = count + rule.AtRule.Keyframes.Keyframes.Count;
            int index = 0;
            while (index < rule.ChildRules.Count)
            {
                stack.Add(rule.ChildRules[index]);
                index = index + 1;
            }
        }
        return count;
    }

    static int CountDeclarations(System.Collections.Generic.List<SvgCssRule> rules)
    {
        int count = 0;
        System.Collections.Generic.List<SvgCssRule> stack = new System.Collections.Generic.List<SvgCssRule>(rules);
        while (stack.Count > 0)
        {
            int last = stack.Count - 1;
            SvgCssRule rule = stack[last];
            stack.RemoveAt(last);
            if (rule == null) continue;
            count = count + rule.Declarations.Count;
            count = count + CountExpandedDeclarations(rule.Declarations);
            if (rule.AtRule != null && rule.AtRule.Keyframes != null)
            {
                int keyframeIndex = 0;
                while (keyframeIndex < rule.AtRule.Keyframes.Keyframes.Count)
                {
                    count = count + rule.AtRule.Keyframes.Keyframes[keyframeIndex].Declarations.Count;
                    count = count + CountExpandedDeclarations(rule.AtRule.Keyframes.Keyframes[keyframeIndex].Declarations);
                    keyframeIndex = keyframeIndex + 1;
                }
            }
            int index = 0;
            while (index < rule.ChildRules.Count)
            {
                stack.Add(rule.ChildRules[index]);
                index = index + 1;
            }
        }
        return count;
    }

    static int CountExpandedDeclarations(System.Collections.Generic.List<SvgCssDeclaration> declarations)
    {
        int count = 0;
        int index = 0;
        while (index < declarations.Count)
        {
            SvgCssDeclaration declaration = declarations[index];
            if (declaration != null && declaration.ExpandedDeclarations.Count > 1) count = count + declaration.ExpandedDeclarations.Count - 1;
            index = index + 1;
        }
        return count;
    }
}
