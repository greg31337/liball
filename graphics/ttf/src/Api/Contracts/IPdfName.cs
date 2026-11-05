namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfName"/>.</summary>
public interface IPdfName
{
    /// <summary>Gets the decoded name value.</summary>
    /// <returns>The original validated value.</returns>
    string GetValue();

    /// <summary>Gets the escaped name without its leading slash.</summary>
    /// <returns>The PDF-escaped byte representation.</returns>
    string GetEscapedValue();

    /// <summary>Gets the complete serialized PDF name token.</summary>
    /// <returns>A slash followed by the escaped value.</returns>
    string GetToken();

    /// <summary>Gets the decoded value's UTF-8 byte length.</summary>
    /// <returns>A value from one through 127.</returns>
    int GetByteLength();
}
