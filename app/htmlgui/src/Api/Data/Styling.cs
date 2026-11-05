namespace htmlgui;

/// <summary>Describes a theme and its named variants.</summary>
public class UiTheme
{
    /// <summary>The stable theme key.</summary>
    public string Key = string.Empty;
    /// <summary>The selected mode.</summary>
    public UiThemeMode Mode;
    /// <summary>The base token values.</summary>
    public UiValueEntry[] Tokens = System.Array.Empty<UiValueEntry>();
    /// <summary>The named variants.</summary>
    public UiThemeVariant[] Variants = System.Array.Empty<UiThemeVariant>();
}

/// <summary>Describes one named theme variant and optional parent.</summary>
public class UiThemeVariant
{
    /// <summary>The stable variant key.</summary>
    public string Key = string.Empty;
    /// <summary>The optional parent variant key.</summary>
    public string? ParentKey;
    /// <summary>The mode represented by the variant.</summary>
    public UiThemeMode Mode;
    /// <summary>The variant token overrides.</summary>
    public UiValueEntry[] Tokens = System.Array.Empty<UiValueEntry>();
}

/// <summary>Describes inline, scoped, or state-dependent style data.</summary>
public class UiStyle
{
    /// <summary>The stable style key.</summary>
    public string Key = string.Empty;
    /// <summary>The optional deterministic scope.</summary>
    public string? Scope;
    /// <summary>The direct declarations.</summary>
    public UiCssDeclaration[] Declarations = System.Array.Empty<UiCssDeclaration>();
    /// <summary>The nested canonical rules.</summary>
    public UiCssRule[] Rules = System.Array.Empty<UiCssRule>();
    /// <summary>The responsive overrides.</summary>
    public UiResponsiveRule[] ResponsiveRules = System.Array.Empty<UiResponsiveRule>();
}

/// <summary>Describes one CSS property/value declaration.</summary>
public class UiCssDeclaration
{
    /// <summary>The canonical property name.</summary>
    public string Property = string.Empty;
    /// <summary>The unescaped value token.</summary>
    public string Value = string.Empty;
    /// <summary>Whether the declaration is important.</summary>
    public bool Important;
    /// <summary>Whether the value passed trusted-token policy.</summary>
    public bool IsTrusted;
}

/// <summary>Describes one selector and its declarations or nested conditions.</summary>
public class UiCssRule
{
    /// <summary>The selector.</summary>
    public string Selector = string.Empty;
    /// <summary>The declarations.</summary>
    public UiCssDeclaration[] Declarations = System.Array.Empty<UiCssDeclaration>();
    /// <summary>The optional condition.</summary>
    public UiCssCondition? Condition;
    /// <summary>The optional cascade-layer name.</summary>
    public string? Layer;
}

/// <summary>Describes a media, supports, or container condition.</summary>
public class UiCssCondition
{
    /// <summary>The condition kind token.</summary>
    public string Kind = string.Empty;
    /// <summary>The optional container name.</summary>
    public string? ContainerName;
    /// <summary>The condition expression.</summary>
    public string Expression = string.Empty;
}

/// <summary>Describes declarations selected by a responsive condition.</summary>
public class UiResponsiveRule
{
    /// <summary>The responsive operator.</summary>
    public UiResponsiveOperator Operator;
    /// <summary>The optional comparison value.</summary>
    public UiLength? Length;
    /// <summary>The optional feature token.</summary>
    public string? Value;
    /// <summary>Whether a container rather than viewport is queried.</summary>
    public bool IsContainer;
    /// <summary>The declarations applied when matched.</summary>
    public UiCssDeclaration[] Declarations = System.Array.Empty<UiCssDeclaration>();
}

/// <summary>Describes enter, leave, or move animation state.</summary>
public class UiAnimation
{
    /// <summary>The stable animation key.</summary>
    public string Key = string.Empty;
    /// <summary>The animation stage.</summary>
    public UiAnimationStage Stage;
    /// <summary>The CSS animation name.</summary>
    public string Name = string.Empty;
    /// <summary>The duration in milliseconds.</summary>
    public int DurationMilliseconds;
    /// <summary>Whether reduced motion disables the animation.</summary>
    public bool DisableForReducedMotion = true;
}

/// <summary>Contains the deterministic output of a CSS compiler operation.</summary>
public class UiCssCompilation
{
    /// <summary>The resolved flattened theme when theme resolution was requested.</summary>
    public UiTheme Theme = new UiTheme();
    /// <summary>The selected theme variant key.</summary>
    public string? VariantKey;
    /// <summary>The generated deterministic style scope identifier.</summary>
    public string ScopeId = string.Empty;
    /// <summary>The resolved canonical declarations.</summary>
    public UiCssDeclaration[] Declarations = System.Array.Empty<UiCssDeclaration>();
    /// <summary>The generated canonical rules.</summary>
    public UiCssRule[] Rules = System.Array.Empty<UiCssRule>();
    /// <summary>The resolved responsive descriptions.</summary>
    public UiResponsiveRule[] ResponsiveRules = System.Array.Empty<UiResponsiveRule>();
    /// <summary>The canonical serialized stylesheet.</summary>
    public string Css = string.Empty;
    /// <summary>The stable hash of the serialized stylesheet.</summary>
    public string Hash = string.Empty;
    /// <summary>The number of rules supplied to stylesheet serialization.</summary>
    public int InputRuleCount;
    /// <summary>The ordered resolution diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
    /// <summary>Whether the compiler operation succeeded.</summary>
    public bool Success;
}
