namespace languagecodes;

/// <summary>Defines detailed and allocation-conscious validation for language codes and tags.</summary>
public interface ILanguageValidator
{
    /// <summary>Validates a language code lexically, by assignment, and against caller-selected policy.</summary>
    /// <param name="code">The language code to validate.</param>
    /// <param name="options">The assignment and acceptance policies.</param>
    /// <returns>Detailed well-formedness, assignment, validity, canonicality, and issue data.</returns>
    LanguageValidationResult ValidateCode(string? code, LanguageValidationOptions options);

    /// <summary>Checks a character span for lexical validity and assignment without allocating a code string.</summary>
    /// <param name="code">The language-code characters to check.</param>
    /// <param name="options">The assignment and acceptance policies.</param>
    /// <returns>True when the span is assigned and accepted; otherwise false.</returns>
    bool ValidateCodeSpan(global::System.ReadOnlySpan<char> code, LanguageValidationOptions options);

    /// <summary>Checks whether a code is assigned in a required or automatically recognized code form.</summary>
    /// <param name="code">The language code to check.</param>
    /// <param name="form">The required code form, or <see cref="LanguageCodeForm.Unknown"/> to recognize any form.</param>
    /// <returns>True when the code resolves to an assigned entity; otherwise false.</returns>
    bool IsAssignedCode(string? code, LanguageCodeForm form);

    /// <summary>Validates an RFC 5646 language tag lexically, against IANA assignment, and against policy.</summary>
    /// <param name="tag">The language tag to validate.</param>
    /// <param name="options">The assignment, entity, and canonicality policies.</param>
    /// <returns>Detailed well-formedness, assignment, validity, canonicality, and issue data.</returns>
    LanguageValidationResult ValidateTag(string? tag, LanguageValidationOptions options);
}
