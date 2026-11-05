namespace docx;

/// <summary>
/// Reads and updates document properties, settings, and supporting configuration parts.
/// </summary>
public interface IDocxPropertySettingsManagement
{
    /// <summary>Returns the core properties.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The core properties.</returns>
    DocxCoreProperties GetCoreProperties(DocxDocument document);

    /// <summary>Sets one core property.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="kind">The property kind.</param>
    /// <param name="value">The property value.</param>
    void SetCoreProperty(DocxDocument document, DocxCorePropertyKind kind, string value);

    /// <summary>Returns the extended properties.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The extended properties.</returns>
    DocxExtendedProperties GetExtendedProperties(DocxDocument document);

    /// <summary>Sets one extended property.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="kind">The property kind.</param>
    /// <param name="value">The property value or vector XML.</param>
    void SetExtendedProperty(DocxDocument document, DocxExtendedPropertyKind kind, string value);

    /// <summary>Returns custom properties in document order.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the custom properties.</returns>
    List<DocxCustomProperty> GetCustomProperties(DocxDocument document);

    /// <summary>Finds a custom property by name.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The matching property, or null.</returns>
    DocxCustomProperty? FindCustomProperty(DocxDocument document, string name);

    /// <summary>Creates or updates a custom property.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="name">The property name.</param>
    /// <param name="type">The variant type local name.</param>
    /// <param name="value">The serialized value.</param>
    /// <returns>The created or updated property.</returns>
    DocxCustomProperty SetCustomProperty(DocxDocument document, string name, string type, string value);

    /// <summary>Updates custom-property identity and link metadata.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="property">The property to update.</param>
    /// <param name="name">The replacement name.</param>
    /// <param name="formatId">The replacement format identifier.</param>
    /// <param name="linkTarget">The replacement link target.</param>
    void SetCustomPropertyMetadata(DocxDocument document, DocxCustomProperty property, string name, string formatId, string linkTarget);

    /// <summary>Removes a custom property by name.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="name">The property name.</param>
    /// <returns>True when a property was removed.</returns>
    bool RemoveCustomProperty(DocxDocument document, string name);

    /// <summary>Returns the document settings.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The settings.</returns>
    DocxSettings GetDocumentSettings(DocxDocument document);

    /// <summary>Sets the document view.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="view">The WordprocessingML view value.</param>
    void SetDocumentView(DocxDocument document, string view);

    /// <summary>Sets zoom mode and percentage.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="view">The zoom view value, or an empty string.</param>
    /// <param name="percent">The zoom percentage text, or an empty string.</param>
    void SetDocumentZoom(DocxDocument document, string view, string percent);

    /// <summary>Sets automatic hyphenation options.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="automatic">Whether automatic hyphenation is enabled.</param>
    /// <param name="zone">The hyphenation zone.</param>
    /// <param name="consecutiveLimit">The consecutive hyphen limit.</param>
    /// <param name="excludeCapitalizedWords">Whether capitalized words are excluded.</param>
    void SetHyphenationOptions(DocxDocument document, bool automatic, string zone, string consecutiveLimit, bool excludeCapitalizedWords);

    /// <summary>Sets spelling and grammar proof states.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="spelling">The spelling proof state.</param>
    /// <param name="grammar">The grammar proof state.</param>
    void SetProofingState(DocxDocument document, string spelling, string grammar);

    /// <summary>Sets the default tab stop.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="value">The tab stop value.</param>
    void SetDefaultTabStop(DocxDocument document, string value);

    /// <summary>Sets revision tracking behavior.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="trackRevisions">Whether revisions are tracked.</param>
    /// <param name="excludeMoves">Whether moves are recorded as ordinary insertions and deletions.</param>
    /// <param name="excludeFormatting">Whether formatting changes are not tracked.</param>
    void SetRevisionBehavior(DocxDocument document, bool trackRevisions, bool excludeMoves, bool excludeFormatting);

    /// <summary>Sets whether fields update when the document opens.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="enabled">Whether fields update.</param>
    void SetFieldUpdateOnOpen(DocxDocument document, bool enabled);

    /// <summary>Sets a document protection edit mode and enforcement.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="editMode">The WordprocessingML edit mode.</param>
    /// <param name="enforced">Whether protection is enforced.</param>
    void SetDocumentProtection(DocxDocument document, string editMode, bool enforced);

    /// <summary>Enables or disables enforced read-only protection.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="readOnly">Whether read-only protection is enforced.</param>
    void SetDocumentReadOnly(DocxDocument document, bool readOnly);

    /// <summary>Removes document protection.</summary>
    /// <param name="document">The document to edit.</param>
    void ClearDocumentProtection(DocxDocument document);

    /// <summary>Sets theme font languages.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="language">The default language.</param>
    /// <param name="eastAsiaLanguage">The East Asian language.</param>
    /// <param name="bidiLanguage">The bidirectional language.</param>
    void SetDocumentLanguage(DocxDocument document, string language, string eastAsiaLanguage, string bidiLanguage);

    /// <summary>Returns compatibility settings in document order.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the settings.</returns>
    List<DocxCompatibilitySetting> GetCompatibilitySettings(DocxDocument document);

    /// <summary>Finds a compatibility setting by name.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="name">The setting name.</param>
    /// <returns>The matching setting, or null.</returns>
    DocxCompatibilitySetting? FindCompatibilitySetting(DocxDocument document, string name);

    /// <summary>Creates or updates a compatibility setting.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="name">The setting name.</param>
    /// <param name="uri">The compatibility namespace URI.</param>
    /// <param name="value">The setting value.</param>
    /// <returns>The created or updated setting.</returns>
    DocxCompatibilitySetting SetCompatibilitySetting(DocxDocument document, string name, string uri, string value);

    /// <summary>Removes a compatibility setting by name.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="name">The setting name.</param>
    /// <returns>True when a setting was removed.</returns>
    bool RemoveCompatibilitySetting(DocxDocument document, string name);

    /// <summary>Returns font-table XML, or an empty string when absent.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The font-table XML.</returns>
    string GetFontTableXml(DocxDocument document);

    /// <summary>Creates or replaces the font-table part.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="xml">The font-table XML.</param>
    void SetFontTableXml(DocxDocument document, string xml);

    /// <summary>Returns theme XML, or an empty string when absent.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The theme XML.</returns>
    string GetThemeXml(DocxDocument document);

    /// <summary>Creates or replaces the theme part.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="xml">The theme XML.</param>
    void SetThemeXml(DocxDocument document, string xml);

    /// <summary>Returns web-settings XML, or an empty string when absent.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The web-settings XML.</returns>
    string GetWebSettingsXml(DocxDocument document);

    /// <summary>Creates or replaces the web-settings part.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="xml">The web-settings XML.</param>
    void SetWebSettingsXml(DocxDocument document, string xml);
}
