namespace ttf;

/// <summary>Provides localized access to the standard OpenType font names.</summary>
public class FontNames : IFontNames
{
    SfntNameTable? table;
    string? requestedLanguage;
    string? pdfPostScriptName;

    internal FontNames(
        SfntNameTable? table,
        string? requestedLanguage,
        string? pdfPostScriptName)
    {
        this.table = table;
        this.requestedLanguage = requestedLanguage;
        this.pdfPostScriptName = pdfPostScriptName;
    }

    /// <summary>Gets the requested language used for selection.</summary>
    /// <returns>The requested language, or null for default selection.</returns>
    public string? GetRequestedLanguage() { return requestedLanguage; }

    /// <summary>Gets the number of parsed name records in table order.</summary>
    /// <returns>The name record count, or zero when no supported table exists.</returns>
    public int GetRecordCount()
    {
        return table == null ? 0 : table.GetRecordCount();
    }

    /// <summary>Gets one parsed name record in table order.</summary>
    /// <param name="index">The zero-based record index.</param>
    /// <returns>The immutable record result.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the record range.
    /// </exception>
    public FontNameRecord GetRecord(int index)
    {
        if (table == null || index < 0 || index >= table.GetRecordCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return new FontNameRecord(index, table);
    }

    /// <summary>Selects any registered name identifier.</summary>
    /// <param name="nameId">The OpenType name identifier.</param>
    /// <returns>The selected name, or null when no valid record exists.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>nameId</c> is outside the unsigned 16-bit range.
    /// </exception>
    public FontNameValue? GetName(int nameId)
    {
        if (nameId < 0 || nameId > 65535)
        {
            throw new System.ArgumentOutOfRangeException("nameId");
        }

        if (table == null) { return null; }
        SfntLocalizedName? selected = requestedLanguage == null
            ? table.SelectDefaultName(nameId)
            : table.SelectName(nameId, requestedLanguage);
        if (selected == null) { return null; }
        return new FontNameValue(
            GetRecord(selected.GetRecordIndex()),
            selected.GetMatchedLanguageRange(),
            ConvertMatchKind(selected.GetMatchKind()));
    }

    /// <summary>Gets the copyright notice.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetCopyright() { return GetName(0); }

    /// <summary>Gets the legacy family name.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetFamilyName() { return GetName(1); }

    /// <summary>Gets the legacy subfamily name.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetSubfamilyName() { return GetName(2); }

    /// <summary>Gets the unique font identifier.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetUniqueIdentifier() { return GetName(3); }

    /// <summary>Gets the full font name.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetFullName() { return GetName(4); }

    /// <summary>Gets the version string.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetVersionString() { return GetName(5); }

    /// <summary>Gets the original PostScript name.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetPostScriptName() { return GetName(6); }

    /// <summary>Gets the trademark notice.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetTrademark() { return GetName(7); }

    /// <summary>Gets the manufacturer name.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetManufacturer() { return GetName(8); }

    /// <summary>Gets the designer name.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetDesigner() { return GetName(9); }

    /// <summary>Gets the font description.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetDescription() { return GetName(10); }

    /// <summary>Gets the vendor URL.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetVendorUrl() { return GetName(11); }

    /// <summary>Gets the designer URL.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetDesignerUrl() { return GetName(12); }

    /// <summary>Gets the license description.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetLicenseDescription() { return GetName(13); }

    /// <summary>Gets the license information URL.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetLicenseUrl() { return GetName(14); }

    /// <summary>Gets the typographic family name.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetTypographicFamilyName() { return GetName(16); }

    /// <summary>Gets the typographic subfamily name.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetTypographicSubfamilyName() { return GetName(17); }

    /// <summary>Gets the sample text.</summary>
    /// <returns>The selected name, or null.</returns>
    public FontNameValue? GetSampleText() { return GetName(19); }

    /// <summary>Gets the normalized PostScript name suitable for PDF output.</summary>
    /// <returns>The normalized name, or null when no PostScript name exists.</returns>
    public string? GetPdfPostScriptName() { return pdfPostScriptName; }

    static FontNameMatchKind ConvertMatchKind(SfntLocalizedNameMatchKind kind)
    {
        if (kind == SfntLocalizedNameMatchKind.ExactLanguage)
        {
            return FontNameMatchKind.ExactLanguage;
        }
        if (kind == SfntLocalizedNameMatchKind.ParentLanguage)
        {
            return FontNameMatchKind.ParentLanguage;
        }
        if (kind == SfntLocalizedNameMatchKind.EnglishFallback)
        {
            return FontNameMatchKind.EnglishFallback;
        }
        return FontNameMatchKind.FirstValidRecord;
    }
}
