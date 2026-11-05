namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontNames"/>.</summary>
public interface IFontNames
{
    /// <summary>Gets the requested language used for selection.</summary>
    /// <returns>The requested language, or null for default selection.</returns>
    string? GetRequestedLanguage();

    /// <summary>Gets the number of parsed name records in table order.</summary>
    /// <returns>The name record count, or zero when no supported table exists.</returns>
    int GetRecordCount();

    /// <summary>Gets one parsed name record in table order.</summary>
    /// <param name="index">The zero-based record index.</param>
    /// <returns>The immutable record result.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the record range.
    /// </exception>
    FontNameRecord GetRecord(int index);

    /// <summary>Selects any registered name identifier.</summary>
    /// <param name="nameId">The OpenType name identifier.</param>
    /// <returns>The selected name, or null when no valid record exists.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>nameId</c> is outside the unsigned 16-bit range.
    /// </exception>
    FontNameValue? GetName(int nameId);

    /// <summary>Gets the copyright notice.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetCopyright();

    /// <summary>Gets the legacy family name.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetFamilyName();

    /// <summary>Gets the legacy subfamily name.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetSubfamilyName();

    /// <summary>Gets the unique font identifier.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetUniqueIdentifier();

    /// <summary>Gets the full font name.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetFullName();

    /// <summary>Gets the version string.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetVersionString();

    /// <summary>Gets the original PostScript name.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetPostScriptName();

    /// <summary>Gets the trademark notice.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetTrademark();

    /// <summary>Gets the manufacturer name.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetManufacturer();

    /// <summary>Gets the designer name.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetDesigner();

    /// <summary>Gets the font description.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetDescription();

    /// <summary>Gets the vendor URL.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetVendorUrl();

    /// <summary>Gets the designer URL.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetDesignerUrl();

    /// <summary>Gets the license description.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetLicenseDescription();

    /// <summary>Gets the license information URL.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetLicenseUrl();

    /// <summary>Gets the typographic family name.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetTypographicFamilyName();

    /// <summary>Gets the typographic subfamily name.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetTypographicSubfamilyName();

    /// <summary>Gets the sample text.</summary>
    /// <returns>The selected name, or null.</returns>
    FontNameValue? GetSampleText();

    /// <summary>Gets the normalized PostScript name suitable for PDF output.</summary>
    /// <returns>The normalized name, or null when no PostScript name exists.</returns>
    string? GetPdfPostScriptName();
}
