namespace unicode;

/// <summary>
/// Provides stable diagnostic rule identifiers for Unicode security checks.
/// </summary>
public static class UnicodeSecurityRules
{
    /// <summary>
    /// The stable diagnostic rule identifier for invisible.
    /// </summary>
    public const string InvisibleRule = UnicodeSecurity.InvisibleRule;
    /// <summary>
    /// The stable diagnostic rule identifier for default Ignorable.
    /// </summary>
    public const string DefaultIgnorableRule = UnicodeSecurity.DefaultIgnorableRule;
    /// <summary>
    /// The stable diagnostic rule identifier for variation Selector.
    /// </summary>
    public const string VariationSelectorRule = UnicodeSecurity.VariationSelectorRule;
    /// <summary>
    /// The stable diagnostic rule identifier for join Control.
    /// </summary>
    public const string JoinControlRule = UnicodeSecurity.JoinControlRule;
    /// <summary>
    /// The stable diagnostic rule identifier for bidi Control.
    /// </summary>
    public const string BidiControlRule = UnicodeSecurity.BidiControlRule;
    /// <summary>
    /// The stable diagnostic rule identifier for identifier bidi safety.
    /// </summary>
    public const string IdentifierBidiRule = UnicodeSecurity.IdentifierBidiRule;
    /// <summary>
    /// The stable diagnostic rule identifier for label bidi safety.
    /// </summary>
    public const string LabelBidiRule = UnicodeSecurity.LabelBidiRule;
}
