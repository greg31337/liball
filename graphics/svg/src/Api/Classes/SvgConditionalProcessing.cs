namespace svg;

/// <summary>Evaluates SVG conditional-processing attributes and selects switch alternatives.</summary>
public class SvgConditionalProcessing : ISvgConditionalProcessing
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgConditionalProcessing()
    {
    }

    /// <summary>Evaluates the conditional-processing attributes on one element using an empty host environment.</summary>
    /// <param name="node">The element to evaluate.</param>
    /// <returns>The evaluated conditional state.</returns>
    public SvgConditionalResult EvaluateWithNode(SvgNode node)
    {
        return new SvgConditionalProcessing().EvaluateWithNodeAndOptions(node, new SvgConditionalProcessingOptions());
    }

    /// <summary>Evaluates the conditional-processing attributes on one element.</summary>
    /// <param name="node">The element to evaluate.</param>
    /// <param name="options">The host capabilities, preferences, catalog, and limits.</param>
    /// <returns>The evaluated conditional state.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the node is not an element.</exception>
    public SvgConditionalResult EvaluateWithNodeAndOptions(SvgNode node, SvgConditionalProcessingOptions options)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (options == null) throw new System.ArgumentNullException("options");
        if (node.Kind != SvgNodeKind.Element) throw new System.ArgumentOutOfRangeException("node");
        SvgConditionalProcessingState state = new SvgConditionalProcessingState();
        SvgConditionalResult result = new SvgConditionalProcessing().EvaluateCore(node, options, state);
        result.Checks = state.Checks;
        return result;
    }

    /// <summary>Selects the switch child for an empty host environment.</summary>
    /// <param name="switchElement">The switch element.</param>
    /// <returns>The ordered candidate evaluations and selected child.</returns>
    public SvgSwitchResult SelectSwitchWithSwitchElement(SvgNode switchElement)
    {
        return new SvgConditionalProcessing().SelectSwitchWithSwitchElementAndOptions(switchElement, new SvgConditionalProcessingOptions());
    }

    /// <summary>Selects the best matching direct switch child using ordered host language preferences.</summary>
    /// <param name="switchElement">The switch element.</param>
    /// <param name="options">The host capabilities, preferences, catalog, and limits.</param>
    /// <returns>The ordered candidate evaluations and selected child.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the node is not an SVG switch element.</exception>
    public SvgSwitchResult SelectSwitchWithSwitchElementAndOptions(SvgNode switchElement, SvgConditionalProcessingOptions options)
    {
        if (switchElement == null) throw new System.ArgumentNullException("switchElement");
        if (options == null) throw new System.ArgumentNullException("options");
        if (switchElement.Kind != SvgNodeKind.Element || switchElement.ElementKind != SvgElementKind.Switch) throw new System.ArgumentOutOfRangeException("switchElement");
        SvgSwitchResult result = new SvgSwitchResult(switchElement);
        if (!new SvgConditionalProcessing().ValidOptions(options))
        {
            new SvgConditionalProcessing().AddError(result.Diagnostics, SvgDiagnosticCode.InvalidArgument, "Conditional-processing options are invalid.", switchElement.SourceSpan);
            return result;
        }

        SvgConditionalProcessingState state = new SvgConditionalProcessingState();
        SvgConditionalResult? bestLanguage = null;
        int bestLanguageIndex = -1;
        SvgConditionalResult? firstFallback = null;
        int firstFallbackIndex = -1;
        int childIndex = 0;
        while (childIndex < switchElement.Children.Count)
        {
            SvgNode child = switchElement.Children[childIndex];
            if (new SvgConditionalProcessing().IsSwitchCandidate(child))
            {
                SvgConditionalResult candidate = new SvgConditionalProcessing().EvaluateCore(child, options, state);
                int candidateIndex = result.Candidates.Count;
                result.Candidates.Add(candidate);
                new SvgConditionalProcessing().AppendDiagnostics(candidate.Diagnostics, result.Diagnostics);
                if (candidate.Success && candidate.Matches)
                {
                    if (candidate.SystemLanguageSource != null)
                    {
                        if (candidate.LanguagePreferenceIndex >= 0 && (bestLanguage == null || candidate.LanguagePreferenceIndex < bestLanguage.LanguagePreferenceIndex))
                        {
                            bestLanguage = candidate;
                            bestLanguageIndex = candidateIndex;
                        }
                    }
                    else if (firstFallback == null)
                    {
                        firstFallback = candidate;
                        firstFallbackIndex = candidateIndex;
                    }
                }
                if (state.LimitExceeded) break;
            }
            childIndex = childIndex + 1;
        }

        result.Checks = state.Checks;
        if (!state.LimitExceeded)
        {
            SvgConditionalResult? selected = bestLanguage == null ? firstFallback : bestLanguage;
            result.SelectedChild = selected == null ? null : selected.Node;
            result.SelectedIndex = bestLanguage == null ? firstFallbackIndex : bestLanguageIndex;
        }
        result.Success = !new SvgConditionalProcessing().HasError(result.Diagnostics);
        return result;
    }
SvgConditionalResult EvaluateCore(SvgNode node, SvgConditionalProcessingOptions options, SvgConditionalProcessingState state)
    {
        SvgConditionalResult result = new SvgConditionalResult(node);
        int initialChecks = state.Checks;
        if (!new SvgConditionalProcessing().ValidOptions(options))
        {
            new SvgConditionalProcessing().AddError(result.Diagnostics, SvgDiagnosticCode.InvalidArgument, "Conditional-processing options are invalid.", node.SourceSpan);
            return result;
        }

        result.Applicable = new SvgConditionalProcessing().IsApplicable(node, options.ElementCatalog);
        if (!result.Applicable)
        {
            result.Matches = true;
            result.Success = true;
            return result;
        }

        result.RequiredExtensionsSource = new SvgConditionalProcessing().FindAttribute(node, SvgAttributeNames.RequiredExtensions);
        result.RequiredFeaturesSource = new SvgConditionalProcessing().FindAttribute(node, SvgAttributeNames.RequiredFeatures);
        result.SystemLanguageSource = new SvgConditionalProcessing().FindAttribute(node, SvgAttributeNames.SystemLanguage);
        bool extensionsMatch = new SvgConditionalProcessing().EvaluateCapabilities(result.RequiredExtensionsSource, options.SupportedExtensions, result.RequiredExtensions, result.MissingExtensions, "requiredExtensions", options, state, result.Diagnostics);
        bool featuresMatch = new SvgConditionalProcessing().EvaluateCapabilities(result.RequiredFeaturesSource, options.SupportedFeatures, result.RequiredFeatures, result.MissingFeatures, "requiredFeatures", options, state, result.Diagnostics);
        bool languageMatch = new SvgConditionalProcessing().EvaluateLanguages(result, options, state);
        result.Checks = state.Checks - initialChecks;
        result.Success = !new SvgConditionalProcessing().HasError(result.Diagnostics);
        result.Matches = result.Success && extensionsMatch && featuresMatch && languageMatch;
        return result;
    }
bool EvaluateCapabilities(SvgAttribute? attribute, SvgCapabilitySet supported, System.Collections.Generic.List<SvgTextToken> parsedItems, System.Collections.Generic.List<SvgTextToken> missingItems, string name, SvgConditionalProcessingOptions options, SvgConditionalProcessingState state, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        if (attribute == null) return true;
        SvgTextTokenListParseResult parsed = new SvgFeatureStringParser().Parse(attribute.Value, options.MaxListItems, options.MaxTokenCharacters);
        new SvgConditionalProcessing().AppendTokens(parsed.Items, parsedItems);
        if (!parsed.Success)
        {
            SvgDiagnosticCode code = new SvgConditionalProcessing().IsLimitError(parsed.Error) ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue;
            new SvgConditionalProcessing().AddError(diagnostics, code, "The " + name + " value is invalid.", attribute.SourceSpan);
            return false;
        }

        bool matches = true;
        int index = 0;
        while (index < parsed.Items.Count)
        {
            if (!new SvgConditionalProcessing().TakeCheck(options, state, diagnostics, attribute.SourceSpan)) return false;
            SvgTextToken item = parsed.Items[index];
            if (!supported.Contains(item.Value))
            {
                missingItems.Add(item);
                matches = false;
            }
            index = index + 1;
        }
        return matches;
    }
bool EvaluateLanguages(SvgConditionalResult result, SvgConditionalProcessingOptions options, SvgConditionalProcessingState state)
    {
        if (result.SystemLanguageSource == null) return true;
        SvgTextTokenListParseResult parsed = new SvgLanguageListParser().Parse(result.SystemLanguageSource.Value, options.MaxListItems, options.MaxTokenCharacters);
        new SvgConditionalProcessing().AppendTokens(parsed.Items, result.SystemLanguages);
        if (!parsed.Success)
        {
            SvgDiagnosticCode code = new SvgConditionalProcessing().IsLimitError(parsed.Error) ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue;
            new SvgConditionalProcessing().AddError(result.Diagnostics, code, "The systemLanguage value is invalid.", result.SystemLanguageSource.SourceSpan);
            return false;
        }

        int preferenceIndex = 0;
        while (preferenceIndex < options.LanguagePreferences.Count)
        {
            string? preference = new SvgConditionalProcessing().NormalizePreference(options.LanguagePreferences[preferenceIndex], options.MaxTokenCharacters);
            if (preference == null)
            {
                new SvgConditionalProcessing().AddError(result.Diagnostics, SvgDiagnosticCode.InvalidArgument, "A host language preference is invalid.", (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
                return false;
            }
            int languageIndex = 0;
            while (languageIndex < parsed.Items.Count)
            {
                if (!new SvgConditionalProcessing().TakeCheck(options, state, result.Diagnostics, result.SystemLanguageSource.SourceSpan)) return false;
                string language = parsed.Items[languageIndex].Value;
                if (new SvgConditionalProcessing().LanguageMatches(preference, language))
                {
                    result.MatchedLanguage = language;
                    result.MatchedLanguagePreference = preference;
                    result.LanguagePreferenceIndex = preferenceIndex;
                    return true;
                }
                languageIndex = languageIndex + 1;
            }
            preferenceIndex = preferenceIndex + 1;
        }
        return false;
    }
string? NormalizePreference(string? value, int maxTokenCharacters)
    {
        if (value == null) return null;
        SvgTextTokenListParseResult parsed = new SvgLanguageListParser().Parse(value, 1, maxTokenCharacters);
        if (!parsed.Success || parsed.Items.Count != 1) return null;
        return parsed.Items[0].Value;
    }
bool LanguageMatches(string preference, string language)
    {
        if (string.Equals(preference, language, System.StringComparison.OrdinalIgnoreCase)) return true;
        if (language.Length <= preference.Length || language[preference.Length] != '-') return false;
        return language.StartsWith(preference, System.StringComparison.OrdinalIgnoreCase);
    }
bool TakeCheck(SvgConditionalProcessingOptions options, SvgConditionalProcessingState state, System.Collections.Generic.List<SvgDiagnostic> diagnostics, SvgSourceSpan? source)
    {
        if (state.Checks >= options.Limits.MaxConditionalChecks)
        {
            if (!state.LimitExceeded) new SvgConditionalProcessing().AddError(diagnostics, SvgDiagnosticCode.InputLimitExceeded, "Conditional processing exceeded MaxConditionalChecks.", source);
            state.LimitExceeded = true;
            return false;
        }
        state.Checks = state.Checks + 1;
        return true;
    }
bool ValidOptions(SvgConditionalProcessingOptions options)
    {
        if (options.SupportedFeatures == null || options.SupportedExtensions == null || options.LanguagePreferences == null || options.ElementCatalog == null || options.Limits == null) return false;
        if (options.Limits.MaxConditionalChecks < 1 || options.MaxListItems < 1 || options.MaxTokenCharacters < 1) return false;
        int index = 0;
        while (index < options.LanguagePreferences.Count)
        {
            if (new SvgConditionalProcessing().NormalizePreference(options.LanguagePreferences[index], options.MaxTokenCharacters) == null) return false;
            index = index + 1;
        }
        return true;
    }
bool IsApplicable(SvgNode node, SvgElementCatalog catalog)
    {
        if (node.NamespaceUri != SvgNamespaces.Svg) return false;
        SvgElementDefinition? definition = catalog.Find(node.ElementKind);
        return definition == null || definition.AllowsConditionalAttributes;
    }
bool IsSwitchCandidate(SvgNode? node)
    {
        if (node == null || node.Kind != SvgNodeKind.Element) return false;
        return node.ElementKind != SvgElementKind.Style && node.ElementKind != SvgElementKind.Script;
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
bool IsLimitError(SvgIriParseError error)
    {
        return error == SvgIriParseError.ItemLimitExceeded || error == SvgIriParseError.TokenTooLong;
    }
void AppendTokens(System.Collections.Generic.List<SvgTextToken> source, System.Collections.Generic.List<SvgTextToken> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
void AppendDiagnostics(System.Collections.Generic.List<SvgDiagnostic> source, System.Collections.Generic.List<SvgDiagnostic> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
bool HasError(System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Severity == SvgDiagnosticSeverity.Error) return true;
            index = index + 1;
        }
        return false;
    }
void AddError(System.Collections.Generic.List<SvgDiagnostic> diagnostics, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
    }
}
