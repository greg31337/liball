namespace languagecodes;

/// <summary>Contains one typed error or warning produced by language validation.</summary>
public class LanguageValidationIssue
{
    /// <summary>The severity of the issue.</summary>
    public LanguageValidationSeverity Severity;

    /// <summary>The typed validation issue.</summary>
    public LanguageValidationError Error;

    /// <summary>The offending code, subtag, or language-tag component.</summary>
    public string? Component;

    /// <summary>A stable human-readable explanation of the issue.</summary>
    public string? Message;
}
