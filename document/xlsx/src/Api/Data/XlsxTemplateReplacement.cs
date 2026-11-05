using System;

namespace xlsx;

/// <summary>
/// Defines one literal template-token replacement.
/// </summary>
public class XlsxTemplateReplacement
{
    /// <summary>Stores the literal token to find.</summary>
    public string Token;
    /// <summary>Stores the replacement text.</summary>
    public string Replacement;

    /// <summary>Initializes an empty template replacement.</summary>
    public XlsxTemplateReplacement()
    {
        Token = "";
        Replacement = "";
    }
}
