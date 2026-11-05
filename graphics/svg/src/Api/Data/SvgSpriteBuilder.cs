namespace svg;

/// <summary>Configures accessible symbol metadata.</summary>
public class SvgSpriteAccessibilityOptions
{
    /// <summary>Whether a direct source title is retained when no explicit title is supplied.</summary>
    public bool PreserveSourceTitle;
    /// <summary>Whether a direct source description is retained when no explicit description is supplied.</summary>
    public bool PreserveSourceDescription;
    /// <summary>Whether source role, focus, and ARIA attributes are retained.</summary>
    public bool PreserveSourceAttributes;
    /// <summary>An explicit title, or <c>null</c> to use source-title policy.</summary>
    public string? Title;
    /// <summary>An explicit description, or <c>null</c> to use source-description policy.</summary>
    public string? Description;
    /// <summary>An explicit role, or <c>null</c> to preserve or infer the role.</summary>
    public string? Role;
    /// <summary>Whether the generated symbol is hidden from accessibility APIs.</summary>
    public bool Decorative;

    /// <summary>Creates source-preserving non-decorative accessibility defaults.</summary>
    public SvgSpriteAccessibilityOptions()
    {
        PreserveSourceTitle = true;
        PreserveSourceDescription = true;
        PreserveSourceAttributes = true;
        Title = null;
        Description = null;
        Role = null;
        Decorative = false;
    }

    internal SvgSpriteAccessibilityOptions Copy()
    {
        SvgSpriteAccessibilityOptions result = new SvgSpriteAccessibilityOptions();
        result.PreserveSourceTitle = PreserveSourceTitle;
        result.PreserveSourceDescription = PreserveSourceDescription;
        result.PreserveSourceAttributes = PreserveSourceAttributes;
        result.Title = Title;
        result.Description = Description;
        result.Role = Role;
        result.Decorative = Decorative;
        return result;
    }
}

/// <summary>Configures bounded deterministic sprite construction.</summary>
public class SvgSpriteBuilderOptions
{
    /// <summary>Whether each isolated source copy receives the safe processing preset.</summary>
    public bool OptimizeSafely;
    /// <summary>Whether missing viewBox data is inferred from resolved drawing bounds or viewport dimensions.</summary>
    public bool InferViewBox;
    /// <summary>The prefix used for automatically generated symbol names.</summary>
    public string GeneratedNamePrefix;
    /// <summary>The positive maximum number of symbols.</summary>
    public int MaximumSymbols;
    /// <summary>The positive maximum combined source-node count.</summary>
    public int MaximumNodes;
    /// <summary>The positive maximum mutations allowed in each processing pass set.</summary>
    public int MaximumChanges;
    /// <summary>The positive maximum CSS characters examined during ID-reference isolation.</summary>
    public long MaximumCssCharacters;
    /// <summary>The positive maximum CSS tokens examined during ID-reference isolation.</summary>
    public int MaximumCssTokens;
    /// <summary>The default accessibility policy copied for inputs without an explicit policy.</summary>
    public SvgSpriteAccessibilityOptions Accessibility;

    /// <summary>Creates safe bounded sprite defaults.</summary>
    public SvgSpriteBuilderOptions()
    {
        OptimizeSafely = true;
        InferViewBox = true;
        GeneratedNamePrefix = "sprite-";
        MaximumSymbols = 1000;
        MaximumNodes = 1000000;
        MaximumChanges = 100000;
        MaximumCssCharacters = 4000000;
        MaximumCssTokens = 1000000;
        Accessibility = new SvgSpriteAccessibilityOptions();
    }
}

/// <summary>Maps one source identifier to its isolated sprite identifier.</summary>
public class SvgSpriteIdentifierMapping
{
    /// <summary>The identifier in the imported source copy.</summary>
    public string SourceIdentifier;
    /// <summary>The identifier used inside the generated symbol.</summary>
    public string SpriteIdentifier;

    /// <summary>Creates one identifier mapping.</summary>
    /// <param name="sourceIdentifier">The source identifier.</param>
    /// <param name="spriteIdentifier">The isolated identifier.</param>
    public SvgSpriteIdentifierMapping(string sourceIdentifier, string spriteIdentifier)
    {
        SourceIdentifier = sourceIdentifier;
        SpriteIdentifier = spriteIdentifier;
    }
}

/// <summary>Describes one generated symbol and its source-to-sprite mappings.</summary>
public class SvgSpriteManifestEntry
{
    /// <summary>The zero-based input order.</summary>
    public int InputIndex;
    /// <summary>The caller-requested symbol name, or an empty string for an automatic name.</summary>
    public string RequestedName;
    /// <summary>The unique generated symbol identifier.</summary>
    public string SymbolIdentifier;
    /// <summary>The normalized source fragment identifier, or an empty string for the whole document.</summary>
    public string FragmentIdentifier;
    /// <summary>The preserved or inferred viewBox text, or an empty string.</summary>
    public string ViewBox;
    /// <summary>The preserved preserveAspectRatio text, or an empty string.</summary>
    public string PreserveAspectRatio;
    /// <summary>The generated accessible title, or an empty string.</summary>
    public string Title;
    /// <summary>The generated accessible description, or an empty string.</summary>
    public string Description;
    /// <summary>Whether the symbol is marked decorative.</summary>
    public bool Decorative;
    /// <summary>Whether safe optimization was requested for the input.</summary>
    public bool SafelyOptimized;
    /// <summary>The source document version observed by the builder.</summary>
    public long SourceVersion;
    /// <summary>The retained source-copy node count before symbol attachment.</summary>
    public int NodeCount;
    /// <summary>The deterministic identifier mappings in document order.</summary>
    public System.Collections.Generic.List<SvgSpriteIdentifierMapping> IdentifierMappings;

    internal SvgSpriteManifestEntry()
    {
        InputIndex = 0;
        RequestedName = "";
        SymbolIdentifier = "";
        FragmentIdentifier = "";
        ViewBox = "";
        PreserveAspectRatio = "";
        Title = "";
        Description = "";
        Decorative = false;
        SafelyOptimized = false;
        SourceVersion = 0;
        NodeCount = 0;
        IdentifierMappings = new System.Collections.Generic.List<SvgSpriteIdentifierMapping>();
    }
}

/// <summary>Contains generated sprite output, mappings, and processing diagnostics.</summary>
public class SvgSpriteBuildResult
{
    /// <summary>The generated editable sprite document, or <c>null</c> after failure.</summary>
    public SvgDocument? Document;
    /// <summary>The ordered generated-symbol manifest.</summary>
    public System.Collections.Generic.List<SvgSpriteManifestEntry> Manifest;
    /// <summary>Diagnostics produced by safe optimization, resolution, and isolation.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The stable sprite-builder failure.</summary>
    public SvgSpriteBuilderError Error;
    /// <summary>The stable human-readable outcome message.</summary>
    public string Message;
    /// <summary>Whether sprite construction completed successfully.</summary>
    public bool Success;

    internal SvgSpriteBuildResult()
    {
        Document = null;
        Manifest = new System.Collections.Generic.List<SvgSpriteManifestEntry>();
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Error = SvgSpriteBuilderError.None;
        Message = "";
        Success = false;
    }
}

/// <summary>Builds optimized reusable symbols from documents and identified fragments.</summary>
/// <remarks>Every input is cloned before extraction, optimization, and identifier rewriting, so source documents are never mutated.</remarks>
public class SvgSpriteBuilder
{
    System.Collections.Generic.List<SvgSpriteBuilderInput> inputs;
    SvgDocumentApi documents;
    SvgNodeApi nodes;

    /// <summary>The mutable bounded construction options.</summary>
    public SvgSpriteBuilderOptions Options;
    /// <summary>The first sticky input or build failure.</summary>
    public SvgSpriteBuilderError Error;
    /// <summary>The stable human-readable failure message.</summary>
    public string Message;

    /// <summary>Creates a sprite builder with safe deterministic defaults.</summary>
    public SvgSpriteBuilder()
    {
        inputs = new System.Collections.Generic.List<SvgSpriteBuilderInput>();
        documents = new SvgDocumentApi();
        nodes = new SvgNodeApi();
        Options = new SvgSpriteBuilderOptions();
        Error = SvgSpriteBuilderError.None;
        Message = "";
    }

    /// <summary>Creates a sprite builder using caller-owned mutable options.</summary>
    /// <param name="options">The bounded sprite construction options.</param>
    /// <returns>A new sprite builder.</returns>
    internal static SvgSpriteBuilder Create(SvgSpriteBuilderOptions options)
    {
        SvgSpriteBuilder result = new SvgSpriteBuilder();
        result.Options = options;
        if (options == null) result.Fail(SvgSpriteBuilderError.InvalidArgument, "Sprite builder options are required.");
        return result;
    }

    /// <summary>Tests whether no sprite-builder operation has failed.</summary>
    /// <returns><c>true</c> when the builder has no sticky failure.</returns>
    internal bool Succeeded()
    {
        return Error == SvgSpriteBuilderError.None;
    }

    /// <summary>Returns the number of queued document and fragment inputs.</summary>
    /// <returns>The queued input count.</returns>
    internal int GetCount()
    {
        return inputs.Count;
    }

    /// <summary>Clears the sticky failure without removing queued inputs.</summary>
    /// <returns>This builder.</returns>
    internal SvgSpriteBuilder ClearError()
    {
        Error = SvgSpriteBuilderError.None;
        Message = "";
        return this;
    }

    /// <summary>Removes every queued input and clears the sticky failure.</summary>
    /// <returns>This builder.</returns>
    internal SvgSpriteBuilder Clear()
    {
        inputs.Clear();
        return ClearError();
    }

    /// <summary>Adds a complete document using an automatic symbol name.</summary>
    /// <param name="document">The source document.</param>
    /// <returns>This builder.</returns>
    internal SvgSpriteBuilder Add(SvgDocument document)
    {
        return AddDocumentWithAccessibility(document, "", null);
    }

    /// <summary>Adds a complete document using a requested symbol name.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="symbolName">The requested symbol name.</param>
    /// <returns>This builder.</returns>
    internal SvgSpriteBuilder AddNamed(SvgDocument document, string symbolName)
    {
        return AddDocumentWithAccessibility(document, symbolName, null);
    }

    /// <summary>Adds a complete document using an automatic symbol name.</summary>
    /// <param name="document">The source document.</param>
    /// <returns>This builder.</returns>
    internal SvgSpriteBuilder AddDocument(SvgDocument document)
    {
        return AddDocumentWithAccessibility(document, "", null);
    }

    /// <summary>Adds a complete document using a requested symbol name.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="symbolName">The requested symbol name.</param>
    /// <returns>This builder.</returns>
    internal SvgSpriteBuilder AddNamedDocument(SvgDocument document, string symbolName)
    {
        return AddDocumentWithAccessibility(document, symbolName, null);
    }

    /// <summary>Adds a complete document with requested naming and accessibility metadata.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="symbolName">The requested symbol name.</param>
    /// <param name="accessibility">Per-symbol accessibility options, or <c>null</c> for builder defaults.</param>
    /// <returns>This builder.</returns>
    internal SvgSpriteBuilder AddDocumentWithAccessibility(SvgDocument document, string symbolName, SvgSpriteAccessibilityOptions? accessibility)
    {
        return Queue(document, symbolName, "", accessibility);
    }

    /// <summary>Adds one identified fragment using an automatic symbol name.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="fragmentIdentifier">The source ID with or without a leading number sign.</param>
    /// <returns>This builder.</returns>
    internal SvgSpriteBuilder AddFragment(SvgDocument document, string fragmentIdentifier)
    {
        return AddFragmentWithAccessibility(document, fragmentIdentifier, "", null);
    }

    /// <summary>Adds one identified fragment using a requested symbol name.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="fragmentIdentifier">The source ID with or without a leading number sign.</param>
    /// <param name="symbolName">The requested symbol name.</param>
    /// <returns>This builder.</returns>
    internal SvgSpriteBuilder AddNamedFragment(SvgDocument document, string fragmentIdentifier, string symbolName)
    {
        return AddFragmentWithAccessibility(document, fragmentIdentifier, symbolName, null);
    }

    /// <summary>Adds one identified fragment with requested naming and accessibility metadata.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="fragmentIdentifier">The source ID with or without a leading number sign.</param>
    /// <param name="symbolName">The requested symbol name.</param>
    /// <param name="accessibility">Per-symbol accessibility options, or <c>null</c> for builder defaults.</param>
    /// <returns>This builder.</returns>
    internal SvgSpriteBuilder AddFragmentWithAccessibility(SvgDocument document, string? fragmentIdentifier, string symbolName, SvgSpriteAccessibilityOptions? accessibility)
    {
        if (fragmentIdentifier == null) return Fail(SvgSpriteBuilderError.InvalidArgument, "A fragment identifier is required.");
        string normalized = NormalizeFragment(fragmentIdentifier);
        if (normalized.Length == 0) return Fail(SvgSpriteBuilderError.InvalidArgument, "A nonempty fragment identifier is required.");
        return Queue(document, symbolName, normalized, accessibility);
    }

    /// <summary>Builds a new editable sprite document and ordered mapping manifest.</summary>
    /// <returns>The complete sprite output or an atomic failure result.</returns>
    internal SvgSpriteBuildResult Build()
    {
        SvgSpriteBuildResult result = new SvgSpriteBuildResult();
        if (Error != SvgSpriteBuilderError.None) return FailedResult(result);
        if (!ValidOptions())
        {
            Fail(SvgSpriteBuilderError.InvalidArgument, "Sprite builder options and limits are invalid.");
            return FailedResult(result);
        }
        if (inputs.Count > Options.MaximumSymbols)
        {
            Fail(SvgSpriteBuilderError.SymbolLimitExceeded, "Sprite construction exceeded MaximumSymbols.");
            return FailedResult(result);
        }
        SvgBuilder outputBuilder = new SvgBuilder();
        outputBuilder.BeginElement(SvgElementKind.Defs);
        SvgNode? defs = outputBuilder.Current;
        outputBuilder.EndElement();
        if (!outputBuilder.Succeeded())
        {
            Fail(SvgSpriteBuilderError.MutationFailed, outputBuilder.Message);
            return FailedResult(result);
        }
        System.Collections.Generic.HashSet<string> names = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        int totalNodes = 0;
        int index = 0;
        while (index < inputs.Count && Error == SvgSpriteBuilderError.None)
        {
            SvgSpriteBuilderInput input = inputs[index];
            string symbolIdentifier = UniqueSymbolName(input.RequestedName, index, names);
            SvgSpriteManifestEntry? entry = BuildInput(input, index, symbolIdentifier, outputBuilder.Document, defs, result.Diagnostics);
            if (entry != null)
            {
                totalNodes = totalNodes + entry.NodeCount;
                if (totalNodes > Options.MaximumNodes) Fail(SvgSpriteBuilderError.NodeLimitExceeded, "Sprite construction exceeded MaximumNodes.");
                else result.Manifest.Add(entry);
            }
            index = index + 1;
        }
        if (Error != SvgSpriteBuilderError.None) return FailedResult(result);
        result.Document = outputBuilder.Document;
        result.Error = SvgSpriteBuilderError.None;
        result.Message = "";
        result.Success = true;
        return result;
    }

    internal SvgSpriteBuilder Queue(SvgDocument document, string symbolName, string fragmentIdentifier, SvgSpriteAccessibilityOptions? accessibility)
    {
        if (Error != SvgSpriteBuilderError.None) return this;
        if (document == null || document.DocumentElement == null || symbolName == null) return Fail(SvgSpriteBuilderError.InvalidArgument, "A rooted source document and symbol name are required.");
        if (Options != null && Options.MaximumSymbols > 0 && inputs.Count >= Options.MaximumSymbols) return Fail(SvgSpriteBuilderError.SymbolLimitExceeded, "Sprite construction exceeded MaximumSymbols.");
        SvgSpriteAccessibilityOptions? copy = accessibility == null ? null : accessibility.Copy();
        inputs.Add(new SvgSpriteBuilderInput(document, symbolName, fragmentIdentifier, copy));
        return this;
    }

    internal SvgSpriteManifestEntry? BuildInput(SvgSpriteBuilderInput input, int inputIndex, string symbolIdentifier, SvgDocument? output, SvgNode? defs, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        SvgNode? semantic = input.FragmentIdentifier.Length == 0 ? input.Document.DocumentElement : input.Document.Index.GetById(input.FragmentIdentifier);
        if (semantic == null)
        {
            Fail(SvgSpriteBuilderError.FragmentNotFound, "The requested sprite fragment was not found.");
            AddDiagnostic(diagnostics, SvgDiagnosticCode.InvalidReference, Message);
            return null;
        }
        string viewBox = ValidViewBox(Attribute(semantic, "viewBox"));
        if (viewBox.Length == 0) viewBox = ValidViewBox(Attribute(input.Document.DocumentElement, "viewBox"));
        string aspectRatio = Attribute(semantic, "preserveAspectRatio");
        if (aspectRatio.Length == 0) aspectRatio = Attribute(input.Document.DocumentElement, "preserveAspectRatio");
        SvgSpriteAccessibilityOptions accessibility = input.Accessibility == null ? Options.Accessibility.Copy() : input.Accessibility.Copy();
        string sourceTitle = DirectText(semantic, SvgElementKind.Title);
        string sourceDescription = DirectText(semantic, SvgElementKind.Desc);
        if (sourceTitle.Length == 0 && !object.ReferenceEquals(semantic, input.Document.DocumentElement)) sourceTitle = DirectText(input.Document.DocumentElement, SvgElementKind.Title);
        if (sourceDescription.Length == 0 && !object.ReferenceEquals(semantic, input.Document.DocumentElement)) sourceDescription = DirectText(input.Document.DocumentElement, SvgElementKind.Desc);
        SvgDocument? working = CloneDocument(input.Document);
        if (working == null)
        {
            Fail(SvgSpriteBuilderError.MutationFailed, "The source document could not be cloned.");
            return null;
        }
        if (CountNodes(working.DocumentNode) > Options.MaximumNodes)
        {
            Fail(SvgSpriteBuilderError.NodeLimitExceeded, "A sprite source exceeded MaximumNodes.");
            return null;
        }
        if (input.FragmentIdentifier.Length != 0 && !Extract(working, input.FragmentIdentifier, diagnostics)) return null;
        SvgNode? extracted = input.FragmentIdentifier.Length == 0 ? null : working.Index.GetById(input.FragmentIdentifier);
        if (extracted != null && extracted.ElementKind == SvgElementKind.Symbol && !FlattenSymbol(extracted)) return null;
        if (Options.OptimizeSafely)
        {
            SvgProcessOptions optimize = new SvgProcessPresets().SafeDefault();
            optimize.MaximumChanges = Options.MaximumChanges;
            SvgProcessResult optimized = working.ProcessWithOptions(optimize);
            CopyDiagnostics(optimized.Diagnostics, diagnostics);
            if (!optimized.Success)
            {
                Fail(SvgSpriteBuilderError.OptimizationFailed, "Safe sprite-source optimization failed.");
                return null;
            }
        }
        if (viewBox.Length == 0 && Options.InferViewBox) viewBox = InferViewBox(working, diagnostics);
        System.Collections.Generic.List<SvgSpriteIdentifierMapping> mappings = CollectMappings(working.DocumentElement, symbolIdentifier + "-");
        SvgIdPrefixOptions identifierOptions = new SvgIdPrefixOptions();
        identifierOptions.Prefix = symbolIdentifier + "-";
        identifierOptions.MaximumCssCharacters = Options.MaximumCssCharacters;
        identifierOptions.MaximumCssTokens = Options.MaximumCssTokens;
        SvgProcessOptions isolation = new SvgProcessOptions();
        isolation.MaximumChanges = Options.MaximumChanges;
        isolation.Jobs.Add(new SvgProcessJobs().PrefixIdsWithOptions(identifierOptions));
        SvgProcessResult isolated = working.ProcessWithOptions(isolation);
        CopyDiagnostics(isolated.Diagnostics, diagnostics);
        if (!isolated.Success)
        {
            Fail(SvgSpriteBuilderError.IsolationFailed, "Sprite identifier and reference isolation failed.");
            return null;
        }
        if (!ScopeStyleSheets(working, symbolIdentifier, diagnostics)) return null;
        SvgNode symbol = documents.CreateElement(SvgElementNames.Symbol, "", SvgElementNames.Symbol, SvgNamespaces.Svg);
        if (!Accept(nodes.AppendChild(defs, symbol))) return null;
        if (!SetAttribute(symbol, SvgAttributeNames.Id, symbolIdentifier)) return null;
        if (viewBox.Length != 0 && !SetAttribute(symbol, SvgAttributeNames.ViewBox, viewBox)) return null;
        if (aspectRatio.Length != 0 && !SetAttribute(symbol, SvgAttributeNames.PreserveAspectRatio, aspectRatio)) return null;
        if (!CopyRootAttributes(working.DocumentElement, symbol)) return null;
        string title = accessibility.Title == null ? accessibility.PreserveSourceTitle ? sourceTitle : "" : accessibility.Title;
        string description = accessibility.Description == null ? accessibility.PreserveSourceDescription ? sourceDescription : "" : accessibility.Description;
        if (!ApplyAccessibility(semantic, symbol, symbolIdentifier, accessibility, title, description, mappings)) return null;
        int retained = 0;
        int childIndex = 0;
        while (childIndex < (working.DocumentElement ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Children.Count)
        {
            SvgNode child = working.DocumentElement.Children[childIndex];
            if (child.ElementKind != SvgElementKind.Title && child.ElementKind != SvgElementKind.Desc && child.ElementKind != SvgElementKind.Metadata)
            {
                SvgNode imported = documents.ImportNode((output ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), child, true);
                if (!Accept(nodes.AppendChild(symbol, imported))) return null;
                retained = retained + CountNodes(imported);
            }
            childIndex = childIndex + 1;
        }
        SvgSpriteManifestEntry entry = new SvgSpriteManifestEntry();
        entry.InputIndex = inputIndex;
        entry.RequestedName = input.RequestedName;
        entry.SymbolIdentifier = symbolIdentifier;
        entry.FragmentIdentifier = input.FragmentIdentifier;
        entry.ViewBox = viewBox;
        entry.PreserveAspectRatio = aspectRatio;
        entry.Title = accessibility.Decorative ? "" : title;
        entry.Description = accessibility.Decorative ? "" : description;
        entry.Decorative = accessibility.Decorative;
        entry.SafelyOptimized = Options.OptimizeSafely;
        entry.SourceVersion = input.Document.Version;
        entry.NodeCount = retained;
        entry.IdentifierMappings = mappings;
        return entry;
    }

    internal bool ScopeStyleSheets(SvgDocument document, string symbolIdentifier, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add((document.DocumentElement ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        int index = 0;
        while (index < pending.Count)
        {
            SvgNode node = pending[index];
            if (node.ElementKind == SvgElementKind.Style)
            {
                string css = nodes.GetTextContent(node);
                SvgCssLimits limits = new SvgCssLimits();
                limits.MaxCharacters = Options.MaximumCssCharacters;
                limits.MaxTokens = Options.MaximumCssTokens;
                limits.MaxSelectorTokens = Options.MaximumCssTokens;
                SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, limits);
                CopyDiagnostics(parsed.Diagnostics, diagnostics);
                if (!parsed.Success || parsed.StyleSheet == null || parsed.UnknownSyntax.Count != 0)
                {
                    Fail(SvgSpriteBuilderError.IsolationFailed, "A sprite stylesheet could not be isolated safely.");
                    return false;
                }
                System.Collections.Generic.List<long> insertions = new System.Collections.Generic.List<long>();
                CollectSelectorInsertions(parsed.StyleSheet.Rules, insertions);
                string scoped = InsertScopes(css, insertions, "#" + CssIdentifier(symbolIdentifier) + " ");
                if (scoped != css && !Accept(nodes.SetTextContent(node, scoped))) return false;
            }
            int childIndex = 0;
            while (childIndex < node.Children.Count)
            {
                pending.Add(node.Children[childIndex]);
                childIndex = childIndex + 1;
            }
            index = index + 1;
        }
        return true;
    }

    internal void CollectSelectorInsertions(System.Collections.Generic.List<SvgCssRule> rules, System.Collections.Generic.List<long> result)
    {
        int index = 0;
        while (index < rules.Count)
        {
            SvgCssRule rule = rules[index];
            if (rule.Selectors != null)
            {
                int selectorIndex = 0;
                while (selectorIndex < rule.Selectors.Items.Count)
                {
                    SvgComplexSelector selector = rule.Selectors.Items[selectorIndex];
                    if (selector.Source != null) result.Add(selector.Source.StartOffset);
                    selectorIndex = selectorIndex + 1;
                }
            }
            CollectSelectorInsertions(rule.ChildRules, result);
            index = index + 1;
        }
    }

    internal string InsertScopes(string value, System.Collections.Generic.List<long> insertions, string scope)
    {
        string result = value;
        while (insertions.Count != 0)
        {
            int largestIndex = 0;
            int index = 1;
            while (index < insertions.Count)
            {
                if (insertions[index] > insertions[largestIndex]) largestIndex = index;
                index = index + 1;
            }
            long sourceOffset = insertions[largestIndex];
            insertions.RemoveAt(largestIndex);
            if (sourceOffset >= 0 && sourceOffset <= result.Length)
            {
                int offset = (int)sourceOffset;
                result = result.Substring(0, offset) + scope + result.Substring(offset);
            }
        }
        return result;
    }

    internal string CssIdentifier(string value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool safe = character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z' || character == '_' || character == '-' || character >= '0' && character <= '9' && index > 0 && !(index == 1 && value[0] == '-');
            if (safe) result.Append(character);
            else
            {
                result.Append('\\');
                result.Append(((int)character).ToString("x", System.Globalization.CultureInfo.InvariantCulture));
                result.Append(' ');
            }
            index = index + 1;
        }
        return result.ToString();
    }

    internal bool Extract(SvgDocument document, string fragmentIdentifier, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        SvgFragmentExtractionProcessOptions extraction = new SvgFragmentExtractionProcessOptions();
        extraction.FragmentIdentifier = fragmentIdentifier;
        extraction.MaximumNodes = Options.MaximumNodes;
        SvgProcessOptions process = new SvgProcessOptions();
        process.MaximumChanges = Options.MaximumChanges;
        process.Jobs.Add(new SvgProcessJobs().ExtractFragmentWithOptions(extraction));
        SvgProcessResult extracted = document.ProcessWithOptions(process);
        CopyDiagnostics(extracted.Diagnostics, diagnostics);
        if (extracted.Success) return true;
        Fail(SvgSpriteBuilderError.MutationFailed, "Sprite fragment extraction failed.");
        return false;
    }

    internal bool FlattenSymbol(SvgNode symbol)
    {
        SvgNode group = documents.CreateElement(SvgElementNames.G, "", SvgElementNames.G, SvgNamespaces.Svg);
        int attributeIndex = 0;
        while (attributeIndex < symbol.Attributes.Count)
        {
            SvgAttribute attribute = symbol.Attributes[attributeIndex];
            if (attribute.NamespaceUri.Length != 0 || attribute.LocalName != SvgAttributeNames.ViewBox && attribute.LocalName != SvgAttributeNames.PreserveAspectRatio)
            {
                SvgAttribute copy = documents.CreateAttribute(attribute.QualifiedName, attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, attribute.Value);
                if (!Accept(nodes.AppendAttribute(group, copy))) return false;
            }
            attributeIndex = attributeIndex + 1;
        }
        int childIndex = 0;
        while (childIndex < symbol.Children.Count)
        {
            SvgNode child = nodes.CloneNode(symbol.Children[childIndex], true);
            if (!Accept(nodes.AppendChild(group, child))) return false;
            childIndex = childIndex + 1;
        }
        if (!Accept(nodes.ReplaceChild(symbol.Parent, symbol, group))) return false;
        return true;
    }

    internal SvgDocument? CloneDocument(SvgDocument source)
    {
        SvgDocument result = documents.CreateDocument();
        result.BaseUri = source.BaseUri;
        result.ContentType = source.ContentType;
        result.ProcessingMode = source.ProcessingMode;
        result.IsFragment = source.IsFragment;
        int index = 0;
        while (index < source.DocumentNode.Children.Count)
        {
            SvgNode imported = documents.ImportNode(result, source.DocumentNode.Children[index], true);
            SvgMutationResult mutation = nodes.AppendChild(result.DocumentNode, imported);
            if (!mutation.Success) return null;
            index = index + 1;
        }
        return result;
    }

    internal string InferViewBox(SvgDocument document, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        SvgResolveOptions resolveOptions = new SvgResolveOptions();
        resolveOptions.ViewportWidth = Dimension(document.DocumentElement, SvgAttributeNames.Width, 300);
        resolveOptions.ViewportHeight = Dimension(document.DocumentElement, SvgAttributeNames.Height, 150);
        resolveOptions.StyleOptions.StyleEnvironment.ViewportWidth = resolveOptions.ViewportWidth;
        resolveOptions.StyleOptions.StyleEnvironment.ViewportHeight = resolveOptions.ViewportHeight;
        SvgResolvedDocument resolved = new SvgDocumentResolver().ResolveWithOptions(document, resolveOptions);
        CopyDiagnostics(resolved.Diagnostics, diagnostics);
        if (resolved.Success)
        {
            SvgDocumentBoundsOptions boundsOptions = new SvgDocumentBoundsOptions();
            boundsOptions.ClipToViewports = false;
            SvgDocumentBoundsResult bounds = resolved.GetDrawingBounds(boundsOptions);
            if (bounds.Success && bounds.Bounds != null && !bounds.Bounds.IsEmpty && bounds.Bounds.Width > 0 && bounds.Bounds.Height > 0) return new SvgViewBox(bounds.Bounds.X, bounds.Bounds.Y, bounds.Bounds.Width, bounds.Bounds.Height).ToText();
        }
        double width = Dimension(document.DocumentElement, SvgAttributeNames.Width, 300);
        double height = Dimension(document.DocumentElement, SvgAttributeNames.Height, 150);
        return new SvgViewBox(0, 0, width, height).ToText();
    }

    internal double Dimension(SvgNode? root, string name, double fallback)
    {
        string value = Attribute(root, name);
        SvgNumberParseResult parsed = SvgNumberParser.ParseNumber(value, 4096);
        return parsed.Success && parsed.Value > 0 ? parsed.Value : fallback;
    }

    internal System.Collections.Generic.List<SvgSpriteIdentifierMapping> CollectMappings(SvgNode? root, string prefix)
    {
        System.Collections.Generic.List<SvgSpriteIdentifierMapping> result = new System.Collections.Generic.List<SvgSpriteIdentifierMapping>();
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        int rootIndex = 0;
        while (rootIndex < (root ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Children.Count)
        {
            pending.Add(root.Children[rootIndex]);
            rootIndex = rootIndex + 1;
        }
        int index = 0;
        while (index < pending.Count)
        {
            SvgNode node = pending[index];
            string identifier = Attribute(node, SvgAttributeNames.Id);
            if (identifier.Length != 0) result.Add(new SvgSpriteIdentifierMapping(identifier, identifier.StartsWith(prefix, System.StringComparison.Ordinal) ? identifier : prefix + identifier));
            int childIndex = 0;
            while (childIndex < node.Children.Count)
            {
                pending.Add(node.Children[childIndex]);
                childIndex = childIndex + 1;
            }
            index = index + 1;
        }
        return result;
    }

    internal bool ApplyAccessibility(SvgNode source, SvgNode symbol, string symbolIdentifier, SvgSpriteAccessibilityOptions accessibility, string title, string description, System.Collections.Generic.List<SvgSpriteIdentifierMapping> mappings)
    {
        if (accessibility.Decorative)
        {
            if (!SetAttribute(symbol, "aria-hidden", "true")) return false;
            return SetAttribute(symbol, "role", "presentation");
        }
        if (accessibility.PreserveSourceAttributes)
        {
            int index = 0;
            while (index < source.Attributes.Count)
            {
                SvgAttribute attribute = source.Attributes[index];
                bool accessible = attribute.NamespaceUri.Length == 0 && (attribute.LocalName.StartsWith("aria-", System.StringComparison.Ordinal) || attribute.LocalName == "role" || attribute.LocalName == "tabindex" || attribute.LocalName == "focusable");
                if (accessible && attribute.LocalName != "aria-hidden" && attribute.LocalName != "aria-labelledby" && attribute.LocalName != "aria-describedby")
                {
                    if (!SetAttribute(symbol, attribute.LocalName, attribute.Value)) return false;
                }
                else if (accessible && (attribute.LocalName == "aria-labelledby" || attribute.LocalName == "aria-describedby"))
                {
                    if (!SetAttribute(symbol, attribute.LocalName, RewriteWords(attribute.Value, mappings))) return false;
                }
                index = index + 1;
            }
        }
        if (accessibility.Role != null)
        {
            if (accessibility.Role.Length == 0)
            {
                if (!RemoveAttribute(symbol, "role")) return false;
            }
            else if (!SetAttribute(symbol, "role", accessibility.Role)) return false;
        }
        if (title.Length != 0)
        {
            string identifier = UniqueInternalId(symbolIdentifier + "-accessible-title", mappings);
            if (!AddDescriptionNode(symbol, SvgElementNames.Title, identifier, title)) return false;
            if (!SetAttribute(symbol, "aria-labelledby", identifier)) return false;
            if (Attribute(symbol, "role").Length == 0 && !SetAttribute(symbol, "role", "img")) return false;
        }
        if (description.Length != 0)
        {
            string identifier = UniqueInternalId(symbolIdentifier + "-accessible-description", mappings);
            if (!AddDescriptionNode(symbol, SvgElementNames.Desc, identifier, description)) return false;
            if (!SetAttribute(symbol, "aria-describedby", identifier)) return false;
        }
        return true;
    }

    internal bool AddDescriptionNode(SvgNode symbol, string localName, string identifier, string text)
    {
        SvgNode element = documents.CreateElement(localName, "", localName, SvgNamespaces.Svg);
        if (!Accept(nodes.AppendChild(symbol, element))) return false;
        if (!SetAttribute(element, SvgAttributeNames.Id, identifier)) return false;
        return Accept(nodes.AppendChild(element, documents.CreateText(text)));
    }

    internal string UniqueInternalId(string requested, System.Collections.Generic.List<SvgSpriteIdentifierMapping> mappings)
    {
        string result = requested;
        int suffix = 2;
        while (MappingContains(mappings, result))
        {
            result = requested + "-" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
            suffix = suffix + 1;
        }
        return result;
    }

    internal bool MappingContains(System.Collections.Generic.List<SvgSpriteIdentifierMapping> mappings, string identifier)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            if (mappings[index].SpriteIdentifier == identifier) return true;
            index = index + 1;
        }
        return false;
    }

    internal string RewriteWords(string value, System.Collections.Generic.List<SvgSpriteIdentifierMapping> mappings)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (char.IsWhiteSpace(value[index]))
            {
                result.Append(value[index]);
                index = index + 1;
                continue;
            }
            int start = index;
            while (index < value.Length && !char.IsWhiteSpace(value[index])) index = index + 1;
            string word = value.Substring(start, index - start);
            string replacement = word;
            int mappingIndex = 0;
            while (mappingIndex < mappings.Count)
            {
                if (mappings[mappingIndex].SourceIdentifier == word) replacement = mappings[mappingIndex].SpriteIdentifier;
                mappingIndex = mappingIndex + 1;
            }
            result.Append(replacement);
        }
        return result.ToString();
    }

    internal bool CopyRootAttributes(SvgNode? root, SvgNode symbol)
    {
        int index = 0;
        while (index < (root ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Attributes.Count)
        {
            SvgAttribute attribute = root.Attributes[index];
            bool structural = attribute.NamespaceUri == SvgNamespaces.Xmlns || attribute.LocalName == SvgAttributeNames.Id || attribute.LocalName == SvgAttributeNames.ViewBox || attribute.LocalName == SvgAttributeNames.PreserveAspectRatio || attribute.LocalName == SvgAttributeNames.Width || attribute.LocalName == SvgAttributeNames.Height || attribute.LocalName == SvgAttributeNames.X || attribute.LocalName == SvgAttributeNames.Y || attribute.LocalName == "version" || attribute.LocalName == "baseProfile" || attribute.LocalName.StartsWith("aria-", System.StringComparison.Ordinal) || attribute.LocalName == "role" || attribute.LocalName == "tabindex" || attribute.LocalName == "focusable";
            if (!structural)
            {
                SvgAttribute copy = documents.CreateAttribute(attribute.QualifiedName, attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, attribute.Value);
                if (!Accept(nodes.AppendAttribute(symbol, copy))) return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal string UniqueSymbolName(string requested, int index, System.Collections.Generic.HashSet<string> names)
    {
        string basis = requested.Length == 0 ? Options.GeneratedNamePrefix + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) : NormalizeName(requested);
        if (basis.Length == 0) basis = Options.GeneratedNamePrefix + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        string result = basis;
        int suffix = 2;
        while (names.Contains(result))
        {
            result = basis + "-" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
            suffix = suffix + 1;
        }
        names.Add(result);
        return result;
    }

    internal string NormalizeName(string value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        bool separator = false;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsLetterOrDigit(character) || character == '_' || character == '-' || character == '.')
            {
                result.Append(character);
                separator = false;
            }
            else if (!separator && result.Length != 0)
            {
                result.Append('-');
                separator = true;
            }
            index = index + 1;
        }
        while (result.Length != 0 && result[result.Length - 1] == '-') result.Length = result.Length - 1;
        return result.ToString();
    }

    internal string NormalizeFragment(string value)
    {
        string result = value.Trim();
        return result.Length != 0 && result[0] == '#' ? result.Substring(1) : result;
    }

    internal string ValidViewBox(string value)
    {
        if (value.Length == 0) return "";
        SvgViewBoxParseResult parsed = SvgViewBoxParser.ParseViewBox(value, 4096);
        return parsed.Success ? value : "";
    }

    internal string DirectText(SvgNode? parent, SvgElementKind kind)
    {
        int index = 0;
        while (index < (parent ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Children.Count)
        {
            if (parent.Children[index].ElementKind == kind) return nodes.GetTextContent(parent.Children[index]);
            index = index + 1;
        }
        return "";
    }

    internal string Attribute(SvgNode? node, string localName)
    {
        if (node == null) return "";
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == localName) return attribute.Value;
            index = index + 1;
        }
        return "";
    }

    internal bool SetAttribute(SvgNode element, string localName, string value)
    {
        int index = 0;
        while (index < element.Attributes.Count)
        {
            SvgAttribute current = element.Attributes[index];
            if (current.NamespaceUri.Length == 0 && current.LocalName == localName)
            {
                if (current.Value == value) return true;
                SvgAttribute replacement = documents.CreateAttribute(localName, "", localName, "", value);
                return Accept(nodes.ReplaceAttribute(element, current, replacement));
            }
            index = index + 1;
        }
        SvgAttribute attribute = documents.CreateAttribute(localName, "", localName, "", value);
        return Accept(nodes.AppendAttribute(element, attribute));
    }

    internal bool RemoveAttribute(SvgNode element, string localName)
    {
        int index = 0;
        while (index < element.Attributes.Count)
        {
            SvgAttribute attribute = element.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == localName) return Accept(nodes.RemoveAttribute(element, attribute));
            index = index + 1;
        }
        return true;
    }

    internal int CountNodes(SvgNode root)
    {
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(root);
        int index = 0;
        while (index < pending.Count)
        {
            SvgNode node = pending[index];
            int childIndex = 0;
            while (childIndex < node.Children.Count)
            {
                pending.Add(node.Children[childIndex]);
                childIndex = childIndex + 1;
            }
            index = index + 1;
        }
        return pending.Count;
    }

    internal void CopyDiagnostics(System.Collections.Generic.List<SvgDiagnostic> source, System.Collections.Generic.List<SvgDiagnostic> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }

    internal void AddDiagnostic(System.Collections.Generic.List<SvgDiagnostic> diagnostics, SvgDiagnosticCode code, string message)
    {
        diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, null));
    }

    internal bool Accept(SvgMutationResult mutation)
    {
        if (mutation.Success) return true;
        return FailBoolean(SvgSpriteBuilderError.MutationFailed, mutation.Message);
    }

    internal bool FailBoolean(SvgSpriteBuilderError error, string message)
    {
        Fail(error, message);
        return false;
    }

    internal SvgSpriteBuilder Fail(SvgSpriteBuilderError error, string? message)
    {
        if (Error == SvgSpriteBuilderError.None)
        {
            Error = error;
            Message = (message ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        }
        return this;
    }

    internal SvgSpriteBuildResult FailedResult(SvgSpriteBuildResult result)
    {
        result.Document = null;
        result.Manifest.Clear();
        result.Error = Error;
        result.Message = Message;
        result.Success = false;
        return result;
    }

    internal bool ValidOptions()
    {
        return Options != null && Options.GeneratedNamePrefix != null && Options.GeneratedNamePrefix.Length != 0 && Options.MaximumSymbols > 0 && Options.MaximumNodes > 0 && Options.MaximumChanges > 0 && Options.MaximumCssCharacters > 0 && Options.MaximumCssTokens > 0 && Options.Accessibility != null;
    }
}
