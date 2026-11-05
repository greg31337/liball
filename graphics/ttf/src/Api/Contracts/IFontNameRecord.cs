namespace ttf;

/// <summary>Defines inspection operations for one OpenType name record.</summary>
public interface IFontNameRecord
{
    /// <summary>Gets the record index in table order.</summary>
    /// <returns>The zero-based record index.</returns>
    int GetRecordIndex();

    /// <summary>Gets the OpenType platform identifier.</summary>
    /// <returns>The platform identifier.</returns>
    int GetPlatformId();

    /// <summary>Gets the platform-specific encoding identifier.</summary>
    /// <returns>The encoding identifier.</returns>
    int GetEncodingId();

    /// <summary>Gets the platform-specific language identifier.</summary>
    /// <returns>The language identifier.</returns>
    int GetLanguageId();

    /// <summary>Gets the OpenType name identifier.</summary>
    /// <returns>The name identifier.</returns>
    int GetNameId();

    /// <summary>Gets the selected decoder classification.</summary>
    /// <returns>The encoding classification.</returns>
    FontNameEncodingKind GetEncodingKind();

    /// <summary>Gets the encoded string length.</summary>
    /// <returns>The encoded byte length.</returns>
    int GetEncodedLength();

    /// <summary>Gets the string offset relative to the name storage area.</summary>
    /// <returns>The relative string offset.</returns>
    int GetStringOffset();

    /// <summary>Reports whether the language identifier references a format 1 language tag.</summary>
    /// <returns>True when the record uses a language-tag record.</returns>
    bool UsesLanguageTag();

    /// <summary>Gets the referenced format 1 language-tag index.</summary>
    /// <returns>The zero-based language-tag index.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The record does not reference a language-tag record.
    /// </exception>
    int GetLanguageTagIndex();

    /// <summary>Gets the resolved BCP 47 language tag.</summary>
    /// <returns>The resolved language tag, or null when unknown or invalid.</returns>
    string? GetLanguageTag();

    /// <summary>Reports whether the language identifier is valid.</summary>
    /// <returns>True when the language reference is valid.</returns>
    bool HasValidLanguage();

    /// <summary>Reports whether the encoded string has valid structural content.</summary>
    /// <returns>True when the encoded string is structurally valid.</returns>
    bool HasValidString();

    /// <summary>Reports whether this record repeats the preceding key.</summary>
    /// <returns>True when the platform, encoding, language, and name key is duplicated.</returns>
    bool IsDuplicateKey();

    /// <summary>Reports whether a decoder is available.</summary>
    /// <returns>True when the encoding is supported.</returns>
    bool IsEncodingSupported();

    /// <summary>Gets the decoded text, including replacement characters when needed.</summary>
    /// <returns>The decoded text.</returns>
    string GetDecodedValue();

    /// <summary>Reports whether decoding inserted replacement characters.</summary>
    /// <returns>True when replacement characters were inserted.</returns>
    bool HasReplacementCharacters();

    /// <summary>Gets the number of replacement characters inserted during decoding.</summary>
    /// <returns>The replacement count.</returns>
    int GetReplacementCount();

    /// <summary>Reports whether this record is eligible for localized selection.</summary>
    /// <returns>True when language and decoded text are valid and nonempty.</returns>
    bool IsValidForSelection();

    /// <summary>Gets the number of decoding diagnostic codes.</summary>
    /// <returns>The decoding diagnostic count.</returns>
    int GetDecodingDiagnosticCount();

    /// <summary>Gets one decoding diagnostic code.</summary>
    /// <param name="index">The zero-based diagnostic index.</param>
    /// <returns>The diagnostic code.</returns>
    FontDiagnosticCode GetDecodingDiagnosticCode(int index);

    /// <summary>Copies the encoded string bytes.</summary>
    /// <returns>A new array containing the encoded bytes.</returns>
    byte[] CopyEncodedBytes();
}
