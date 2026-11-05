namespace ttf;

/// <summary>Describes one immutable OpenType name record and its decoded value.</summary>
public class FontNameRecord : IFontNameRecord
{
    int recordIndex;
    SfntNameTable table;
    SfntNameRecord record;
    SfntDecodedName decodedName;

    internal FontNameRecord(int recordIndex, SfntNameTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (recordIndex < 0 || recordIndex >= table.GetRecordCount())
        {
            throw new System.ArgumentOutOfRangeException("recordIndex");
        }

        this.recordIndex = recordIndex;
        this.table = table;
        record = table.GetRecord(recordIndex);
        decodedName = table.GetDecodedName(recordIndex);
    }

    /// <summary>Gets the record index in table order.</summary>
    /// <returns>The zero-based record index.</returns>
    public int GetRecordIndex() { return recordIndex; }

    /// <summary>Gets the OpenType platform identifier.</summary>
    /// <returns>The platform identifier.</returns>
    public int GetPlatformId() { return record.GetPlatformId(); }

    /// <summary>Gets the platform-specific encoding identifier.</summary>
    /// <returns>The encoding identifier.</returns>
    public int GetEncodingId() { return record.GetEncodingId(); }

    /// <summary>Gets the platform-specific language identifier.</summary>
    /// <returns>The language identifier.</returns>
    public int GetLanguageId() { return record.GetLanguageId(); }

    /// <summary>Gets the OpenType name identifier.</summary>
    /// <returns>The name identifier.</returns>
    public int GetNameId() { return record.GetNameId(); }

    /// <summary>Gets the selected decoder classification.</summary>
    /// <returns>The encoding classification.</returns>
    public FontNameEncodingKind GetEncodingKind()
    {
        SfntNameEncodingKind kind = record.GetEncodingKind();

        if (kind == SfntNameEncodingKind.Utf16BigEndian)
        {
            return FontNameEncodingKind.Utf16BigEndian;
        }

        if (kind == SfntNameEncodingKind.MacintoshRoman)
        {
            return FontNameEncodingKind.MacintoshRoman;
        }

        if (kind == SfntNameEncodingKind.MacintoshLegacy)
        {
            return FontNameEncodingKind.MacintoshLegacy;
        }

        if (kind == SfntNameEncodingKind.WindowsCodePage936)
        {
            return FontNameEncodingKind.WindowsCodePage936;
        }

        if (kind == SfntNameEncodingKind.WindowsCodePage950)
        {
            return FontNameEncodingKind.WindowsCodePage950;
        }

        if (kind == SfntNameEncodingKind.WindowsCodePage949)
        {
            return FontNameEncodingKind.WindowsCodePage949;
        }

        if (kind == SfntNameEncodingKind.WindowsCodePage932)
        {
            return FontNameEncodingKind.WindowsCodePage932;
        }

        return FontNameEncodingKind.Unknown;
    }

    /// <summary>Gets the encoded string length.</summary>
    /// <returns>The encoded byte length.</returns>
    public int GetEncodedLength() { return record.GetLength(); }

    /// <summary>Gets the string offset relative to the name storage area.</summary>
    /// <returns>The relative string offset.</returns>
    public int GetStringOffset() { return record.GetStringOffset(); }

    /// <summary>Reports whether the language identifier references a format 1 language tag.</summary>
    /// <returns>True when the record uses a language-tag record.</returns>
    public bool UsesLanguageTag() { return record.UsesLanguageTag(); }

    /// <summary>Gets the referenced format 1 language-tag index.</summary>
    /// <returns>The zero-based language-tag index.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The record does not reference a language-tag record.
    /// </exception>
    public int GetLanguageTagIndex() { return record.GetLanguageTagIndex(); }

    /// <summary>Gets the resolved BCP 47 language tag.</summary>
    /// <returns>The resolved language tag, or null when unknown or invalid.</returns>
    public string? GetLanguageTag()
    {
        return table.GetResolvedLanguageTagForRecord(recordIndex);
    }

    /// <summary>Reports whether the language identifier is valid.</summary>
    /// <returns>True when the language reference is valid.</returns>
    public bool HasValidLanguage() { return record.HasValidLanguage(); }

    /// <summary>Reports whether the encoded string has valid structural content.</summary>
    /// <returns>True when the encoded string is structurally valid.</returns>
    public bool HasValidString() { return record.HasValidString(); }

    /// <summary>Reports whether this record repeats the preceding key.</summary>
    /// <returns>True when the platform, encoding, language, and name key is duplicated.</returns>
    public bool IsDuplicateKey() { return record.IsDuplicateKey(); }

    /// <summary>Reports whether a decoder is available.</summary>
    /// <returns>True when the encoding is supported.</returns>
    public bool IsEncodingSupported() { return decodedName.IsEncodingSupported(); }

    /// <summary>Gets the decoded text, including replacement characters when needed.</summary>
    /// <returns>The decoded text.</returns>
    public string GetDecodedValue() { return decodedName.GetValue(); }

    /// <summary>Reports whether decoding inserted replacement characters.</summary>
    /// <returns>True when replacement characters were inserted.</returns>
    public bool HasReplacementCharacters()
    {
        return decodedName.HasReplacementCharacters();
    }

    /// <summary>Gets the number of replacement characters inserted during decoding.</summary>
    /// <returns>The replacement count.</returns>
    public int GetReplacementCount() { return decodedName.GetReplacementCount(); }

    /// <summary>Reports whether this record is eligible for localized selection.</summary>
    /// <returns>True when language and decoded text are valid and nonempty.</returns>
    public bool IsValidForSelection()
    {
        return record.HasValidLanguage() && decodedName.IsValid();
    }

    /// <summary>Gets the number of decoding diagnostic codes.</summary>
    /// <returns>The decoding diagnostic count.</returns>
    public int GetDecodingDiagnosticCount() { return decodedName.GetDiagnosticCount(); }

    /// <summary>Gets one decoding diagnostic code.</summary>
    /// <param name="index">The zero-based diagnostic index.</param>
    /// <returns>The diagnostic code.</returns>
    public FontDiagnosticCode GetDecodingDiagnosticCode(int index)
    {
        return decodedName.GetDiagnosticCode(index);
    }

    /// <summary>Copies the encoded string bytes.</summary>
    /// <returns>A new array containing the encoded bytes.</returns>
    public byte[] CopyEncodedBytes() { return record.CopyStringBytes(); }
}
