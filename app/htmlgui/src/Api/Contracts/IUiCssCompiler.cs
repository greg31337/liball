namespace htmlgui;

/// <summary>Resolves themes and styles and produces deterministic safe CSS rules and stylesheets.</summary>
public interface IUiCssCompiler
{
    /// <summary>Resolves a theme variant, token references, and host token overrides.</summary>
    /// <param name="theme">The optional theme to resolve.</param>
    /// <param name="hostMode">The host color mode used when the theme selects automatically.</param>
    /// <param name="requestedVariantKey">The optional explicitly requested variant key.</param>
    /// <param name="hostOverrides">The optional final token override layer.</param>
    /// <returns>The flattened theme, custom-property declarations, root rule, and diagnostics.</returns>
    UiCssCompilation ResolveTheme(UiTheme? theme, UiThemeMode hostMode, string? requestedVariantKey, UiValueEntry[]? hostOverrides);

    /// <summary>Resolves style precedence and generates rules constrained to a deterministic owner scope.</summary>
    /// <param name="rootSeed">The deterministic application root seed.</param>
    /// <param name="ownerIdentity">The stable identity of the styled owner.</param>
    /// <param name="globalStyles">The optional global style layer.</param>
    /// <param name="windowStyles">The optional window style layer.</param>
    /// <param name="pageStyles">The optional page style layer.</param>
    /// <param name="componentStyles">The optional component style layer.</param>
    /// <param name="controlStyles">The optional control style layer.</param>
    /// <param name="stateStyles">The optional state style layer.</param>
    /// <param name="inlineStyle">The optional highest-precedence inline style.</param>
    /// <returns>The generated scope, resolved declarations and responsive descriptions, scoped rules, and diagnostics.</returns>
    UiCssCompilation GenerateScopedRules(string? rootSeed, string? ownerIdentity, UiStyle[]? globalStyles, UiStyle[]? windowStyles, UiStyle[]? pageStyles, UiStyle[]? componentStyles, UiStyle[]? controlStyles, UiStyle[]? stateStyles, UiStyle? inlineStyle);

    /// <summary>Generates canonical conditional rules for responsive descriptions.</summary>
    /// <param name="responsiveRules">The responsive descriptions to compile.</param>
    /// <param name="selector">The selector receiving responsive declarations.</param>
    /// <returns>The ordered and deduplicated conditional rules.</returns>
    /// <exception cref="System.ArgumentException">Thrown when a rule, selector, feature, or declaration is invalid.</exception>
    UiCssCompilation GenerateResponsiveRules(UiResponsiveRule[]? responsiveRules, string? selector);

    /// <summary>Serializes, interns, orders, and deduplicates a complete stylesheet.</summary>
    /// <param name="rules">The rules to serialize.</param>
    /// <returns>The canonical CSS, stable hash, canonical rules, and input rule count.</returns>
    /// <exception cref="System.ArgumentException">Thrown when a rule contains invalid or unsafe CSS data.</exception>
    UiCssCompilation SerializeStylesheet(UiCssRule[]? rules);
}
