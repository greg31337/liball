namespace unicode;

/// <summary>
/// Provides Unicode mapping and validation for one IDNA-adjacent label without DNS or Punycode processing.
/// </summary>
public interface IUnicodeIdna
{
    /// <summary>
    /// Applies nontransitional Unicode compatibility mapping and NFC normalization to one label.
    /// </summary>
    /// <param name="label">The UTF-16 label to prepare.</param>
    /// <returns>The mapped and normalized Unicode label.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>label</c> is <c>null</c>.</exception>
    string PrepareLabel(string label);

    /// <summary>
    /// Prepares and validates one Unicode label with contextual, bidirectional, and security checks.
    /// </summary>
    /// <param name="label">The UTF-16 label to validate.</param>
    /// <returns>The prepared label, validation diagnostics, and security observations.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>label</c> is <c>null</c>.</exception>
    UnicodeIdnaValidationResult ValidateLabel(string label);

    /// <summary>
    /// Reports whether one Unicode label passes validation after Unicode preparation.
    /// </summary>
    /// <param name="label">The UTF-16 label to validate.</param>
    /// <returns><c>true</c> when the prepared label is valid; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>label</c> is <c>null</c>.</exception>
    bool IsLabelValid(string label);
}
