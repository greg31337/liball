namespace htmlgui;

/// <summary>Describes a state-to-target binding.</summary>
public class UiBinding
{
    /// <summary>The stable binding identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The source state identifier.</summary>
    public string StateId = string.Empty;
    /// <summary>The source state path.</summary>
    public string Path = string.Empty;
    /// <summary>The target node identifier.</summary>
    public string TargetNodeId = string.Empty;
    /// <summary>The target kind token.</summary>
    public string Target = string.Empty;
    /// <summary>The binding direction.</summary>
    public UiBindingMode Mode;
    /// <summary>The source update trigger.</summary>
    public UiBindingTrigger Trigger;
    /// <summary>The optional converter handler identifier.</summary>
    public string? ConverterId;
    /// <summary>The optional fallback value.</summary>
    public UiValue? FallbackValue;
    /// <summary>The optional current evaluation context.</summary>
    public UiBindingContext? Context;
    /// <summary>The optional most recent update.</summary>
    public UiBindingUpdate? Update;
    /// <summary>The optional most recent conversion result.</summary>
    public UiConversionResult? Conversion;
}

/// <summary>Describes state, scope, and origin available during binding evaluation.</summary>
public class UiBindingContext
{
    /// <summary>The binding identifier.</summary>
    public string BindingId = string.Empty;
    /// <summary>The optional component or view scope.</summary>
    public string? ScopeId;
    /// <summary>The propagation origin.</summary>
    public string Origin = string.Empty;
    /// <summary>The source version.</summary>
    public int Version;
    /// <summary>Whether the update is flowing toward the source.</summary>
    public bool ToSource;
}

/// <summary>Describes one evaluated binding update.</summary>
public class UiBindingUpdate
{
    /// <summary>The binding identifier.</summary>
    public string BindingId = string.Empty;
    /// <summary>The target node identifier.</summary>
    public string TargetNodeId = string.Empty;
    /// <summary>The target token.</summary>
    public string Target = string.Empty;
    /// <summary>The propagated value.</summary>
    public UiValue Value = new UiValue();
    /// <summary>The source version.</summary>
    public int Version;
    /// <summary>The update origin.</summary>
    public string Origin = string.Empty;
}

/// <summary>Describes exceptionless converter output.</summary>
public class UiConversionResult
{
    /// <summary>Whether conversion succeeded.</summary>
    public bool Success;
    /// <summary>The converted value.</summary>
    public UiValue? Value;
    /// <summary>The optional diagnostic.</summary>
    public UiDiagnostic? Diagnostic;
}

/// <summary>Contains one binding lifecycle or propagation result.</summary>
public class UiBindingResult
{
    /// <summary>Whether the operation and all propagation callbacks succeeded.</summary>
    public bool Success;
    /// <summary>Whether the operation attached, detached, or propagated a value.</summary>
    public bool Applied;
    /// <summary>The resulting binding definition when one was selected.</summary>
    public UiBinding? Binding;
    /// <summary>The bindings that propagated source values in deterministic order.</summary>
    public string[] BindingIds = System.Array.Empty<string>();
    /// <summary>The target updates in deterministic order.</summary>
    public UiBindingUpdate[] Updates = System.Array.Empty<UiBindingUpdate>();
    /// <summary>The ordered lifecycle, conversion, state, and observer diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}
