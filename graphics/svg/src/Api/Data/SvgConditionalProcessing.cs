namespace svg;

/// <summary>Configures conditional processing against caller-supplied host capabilities and language preferences.</summary>
public class SvgConditionalProcessingOptions
{
    /// <summary>The exact SVG 1.1 feature strings supported by the host.</summary>
    public SvgCapabilitySet SupportedFeatures;
    /// <summary>The exact extension IRIs supported by the host.</summary>
    public SvgCapabilitySet SupportedExtensions;
    /// <summary>The ordered BCP 47 language preferences, most preferred first.</summary>
    public System.Collections.Generic.List<string> LanguagePreferences;
    /// <summary>The current-element catalog used to determine attribute applicability.</summary>
    public SvgElementCatalog ElementCatalog;
    /// <summary>The bounded reference and conditional-processing limits.</summary>
    public SvgReferenceLimits Limits;
    /// <summary>The maximum tokens accepted in one conditional-processing attribute.</summary>
    public int MaxListItems;
    /// <summary>The maximum characters accepted in one capability or language token.</summary>
    public int MaxTokenCharacters;

    /// <summary>Creates an empty host environment with secure processing defaults.</summary>
    public SvgConditionalProcessingOptions()
    {
        SupportedFeatures = new SvgCapabilitySet();
        SupportedExtensions = new SvgCapabilitySet();
        LanguagePreferences = new System.Collections.Generic.List<string>();
        ElementCatalog = new SvgCatalogFactory().CreateElementCatalog();
        Limits = new SvgReferenceLimits();
        MaxListItems = 1000000;
        MaxTokenCharacters = 4096;
    }
}

/// <summary>Contains the evaluated conditional-processing state for one element.</summary>
public class SvgConditionalResult
{
    /// <summary>The source element.</summary>
    public SvgNode? Node;
    /// <summary>The source element version captured during evaluation.</summary>
    public long NodeVersion;
    /// <summary>Whether conditional-processing attributes apply to the source element.</summary>
    public bool Applicable;
    /// <summary>The authored required-features attribute, or <c>null</c>.</summary>
    public SvgAttribute? RequiredFeaturesSource;
    /// <summary>The parsed required feature strings.</summary>
    public System.Collections.Generic.List<SvgTextToken> RequiredFeatures;
    /// <summary>The unsupported required feature strings.</summary>
    public System.Collections.Generic.List<SvgTextToken> MissingFeatures;
    /// <summary>The authored required-extensions attribute, or <c>null</c>.</summary>
    public SvgAttribute? RequiredExtensionsSource;
    /// <summary>The parsed required extension IRIs.</summary>
    public System.Collections.Generic.List<SvgTextToken> RequiredExtensions;
    /// <summary>The unsupported required extension IRIs.</summary>
    public System.Collections.Generic.List<SvgTextToken> MissingExtensions;
    /// <summary>The authored system-language attribute, or <c>null</c>.</summary>
    public SvgAttribute? SystemLanguageSource;
    /// <summary>The parsed normalized system-language tags.</summary>
    public System.Collections.Generic.List<SvgTextToken> SystemLanguages;
    /// <summary>The matched normalized authored language tag, or an empty string.</summary>
    public string MatchedLanguage;
    /// <summary>The matched normalized host language preference, or an empty string.</summary>
    public string MatchedLanguagePreference;
    /// <summary>The matched host preference index, or <c>-1</c>.</summary>
    public int LanguagePreferenceIndex;
    /// <summary>The number of bounded capability and language comparisons performed.</summary>
    public int Checks;
    /// <summary>Whether every applicable authored condition evaluated to true.</summary>
    public bool Matches;
    /// <summary>Located syntax and limit diagnostics in deterministic order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether evaluation completed without an error diagnostic.</summary>
    public bool Success;

    internal SvgConditionalResult(SvgNode? node)
    {
        Node = node;
        NodeVersion = node == null ? 0 : node.Version;
        Applicable = false;
        RequiredFeaturesSource = null;
        RequiredFeatures = new System.Collections.Generic.List<SvgTextToken>();
        MissingFeatures = new System.Collections.Generic.List<SvgTextToken>();
        RequiredExtensionsSource = null;
        RequiredExtensions = new System.Collections.Generic.List<SvgTextToken>();
        MissingExtensions = new System.Collections.Generic.List<SvgTextToken>();
        SystemLanguageSource = null;
        SystemLanguages = new System.Collections.Generic.List<SvgTextToken>();
        MatchedLanguage = "";
        MatchedLanguagePreference = "";
        LanguagePreferenceIndex = -1;
        Checks = 0;
        Matches = false;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }

    /// <summary>Checks whether the source element retains the captured version.</summary>
    /// <returns><c>true</c> when the evaluated snapshot is current.</returns>
    internal bool IsCurrent()
    {
        return Node != null && Node.Version == NodeVersion;
    }
}

/// <summary>Contains ordered conditional evaluations and the selected child of one switch element.</summary>
public class SvgSwitchResult
{
    /// <summary>The source switch element.</summary>
    public SvgNode? SwitchElement;
    /// <summary>The switch element version captured during selection.</summary>
    public long SwitchVersion;
    /// <summary>The evaluated direct child elements in document order, excluding style and script.</summary>
    public System.Collections.Generic.List<SvgConditionalResult> Candidates;
    /// <summary>The selected child, or <c>null</c> when no child matched.</summary>
    public SvgNode? SelectedChild;
    /// <summary>The selected child's zero-based element-candidate index, or <c>-1</c>.</summary>
    public int SelectedIndex;
    /// <summary>The number of bounded capability and language comparisons performed.</summary>
    public int Checks;
    /// <summary>Located syntax and limit diagnostics in deterministic order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether selection completed without an error diagnostic.</summary>
    public bool Success;

    internal SvgSwitchResult(SvgNode? node)
    {
        SwitchElement = node;
        SwitchVersion = node == null ? 0 : node.Version;
        Candidates = new System.Collections.Generic.List<SvgConditionalResult>();
        SelectedChild = null;
        SelectedIndex = -1;
        Checks = 0;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }

    /// <summary>Checks whether the switch and every evaluated candidate retain their captured versions.</summary>
    /// <returns><c>true</c> when the selection snapshot is current.</returns>
    internal bool IsCurrent()
    {
        if (SwitchElement == null || SwitchElement.Version != SwitchVersion) return false;
        int index = 0;
        while (index < Candidates.Count)
        {
            if (Candidates[index] == null || !Candidates[index].IsCurrent()) return false;
            index = index + 1;
        }
        return true;
    }
}
