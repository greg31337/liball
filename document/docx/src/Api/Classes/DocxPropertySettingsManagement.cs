using System.Xml;

namespace docx;

/// <summary>
/// Reads and updates document properties, settings, and supporting configuration parts.
/// </summary>
public class DocxPropertySettingsManagement : IDocxPropertySettingsManagement
{
    /// <summary>Creates a property and settings management service.</summary>
    public DocxPropertySettingsManagement()
    {
    }

    /// <summary>Returns the core properties.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The core properties.</returns>
    public DocxCoreProperties GetCoreProperties(DocxDocument document)
    {
        ValidateDocument(document);
        return document.GetCoreProperties();
    }

    /// <summary>Sets one core property.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="kind">The property kind.</param>
    /// <param name="value">The property value.</param>
    public void SetCoreProperty(DocxDocument document, DocxCorePropertyKind kind, string value)
    {
        ValidateDocument(document);
        DocxCoreProperties properties = document.GetOrCreateCoreProperties();

        switch (kind)
        {
            case DocxCorePropertyKind.Title:
                properties.SetTitle(value);
                break;
            case DocxCorePropertyKind.Subject:
                properties.SetSubject(value);
                break;
            case DocxCorePropertyKind.Creator:
                properties.SetCreator(value);
                break;
            case DocxCorePropertyKind.Keywords:
                properties.SetKeywords(value);
                break;
            case DocxCorePropertyKind.Description:
                properties.SetDescription(value);
                break;
            case DocxCorePropertyKind.LastModifiedBy:
                properties.SetLastModifiedBy(value);
                break;
            case DocxCorePropertyKind.Revision:
                properties.SetRevision(value);
                break;
            case DocxCorePropertyKind.Created:
                properties.SetCreated(value);
                break;
            case DocxCorePropertyKind.Modified:
                properties.SetModified(value);
                break;
            case DocxCorePropertyKind.Category:
                properties.SetCategory(value);
                break;
            case DocxCorePropertyKind.ContentStatus:
                properties.SetContentStatus(value);
                break;
            case DocxCorePropertyKind.Version:
                properties.SetVersion(value);
                break;
            default:
                throw new ArgumentOutOfRangeException("kind");
        }

        document.WriteCorePropertiesPart();
    }

    /// <summary>Returns the extended properties.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The extended properties.</returns>
    public DocxExtendedProperties GetExtendedProperties(DocxDocument document)
    {
        ValidateDocument(document);
        return document.GetExtendedProperties();
    }

    /// <summary>Sets one extended property.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="kind">The property kind.</param>
    /// <param name="value">The property value or vector XML.</param>
    public void SetExtendedProperty(DocxDocument document, DocxExtendedPropertyKind kind, string value)
    {
        ValidateDocument(document);
        DocxExtendedProperties properties = document.GetOrCreateExtendedProperties();

        switch (kind)
        {
            case DocxExtendedPropertyKind.Template:
                properties.SetTemplate(value);
                break;
            case DocxExtendedPropertyKind.Manager:
                properties.SetManager(value);
                break;
            case DocxExtendedPropertyKind.Company:
                properties.SetCompany(value);
                break;
            case DocxExtendedPropertyKind.Pages:
                properties.SetPages(value);
                break;
            case DocxExtendedPropertyKind.Words:
                properties.SetWords(value);
                break;
            case DocxExtendedPropertyKind.Characters:
                properties.SetCharacters(value);
                break;
            case DocxExtendedPropertyKind.PresentationFormat:
                properties.SetPresentationFormat(value);
                break;
            case DocxExtendedPropertyKind.Lines:
                properties.SetLines(value);
                break;
            case DocxExtendedPropertyKind.Paragraphs:
                properties.SetParagraphs(value);
                break;
            case DocxExtendedPropertyKind.Slides:
                properties.SetSlides(value);
                break;
            case DocxExtendedPropertyKind.Notes:
                properties.SetNotes(value);
                break;
            case DocxExtendedPropertyKind.TotalTime:
                properties.SetTotalTime(value);
                break;
            case DocxExtendedPropertyKind.HiddenSlides:
                properties.SetHiddenSlides(value);
                break;
            case DocxExtendedPropertyKind.MultimediaClips:
                properties.SetMMClips(value);
                break;
            case DocxExtendedPropertyKind.ScaleCrop:
                properties.SetScaleCrop(value);
                break;
            case DocxExtendedPropertyKind.LinksUpToDate:
                properties.SetLinksUpToDate(value);
                break;
            case DocxExtendedPropertyKind.CharactersWithSpaces:
                properties.SetCharactersWithSpaces(value);
                break;
            case DocxExtendedPropertyKind.SharedDocument:
                properties.SetSharedDoc(value);
                break;
            case DocxExtendedPropertyKind.HyperlinkBase:
                properties.SetHyperlinkBase(value);
                break;
            case DocxExtendedPropertyKind.HyperlinksChanged:
                properties.SetHyperlinksChanged(value);
                break;
            case DocxExtendedPropertyKind.ApplicationVersion:
                properties.SetAppVersion(value);
                break;
            case DocxExtendedPropertyKind.Application:
                properties.SetApplication(value);
                break;
            case DocxExtendedPropertyKind.DocumentSecurity:
                properties.SetDocSecurity(value);
                break;
            case DocxExtendedPropertyKind.HeadingPairsXml:
                properties.SetHeadingPairsXml(value);
                break;
            case DocxExtendedPropertyKind.TitlesOfPartsXml:
                properties.SetTitlesOfPartsXml(value);
                break;
            default:
                throw new ArgumentOutOfRangeException("kind");
        }

        document.WriteExtendedPropertiesPart();
    }

    /// <summary>Returns custom properties in document order.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the custom properties.</returns>
    public List<DocxCustomProperty> GetCustomProperties(DocxDocument document)
    {
        ValidateDocument(document);
        return new List<DocxCustomProperty>(document.GetCustomProperties().Properties);
    }

    /// <summary>Finds a custom property by name.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The matching property, or null.</returns>
    public DocxCustomProperty? FindCustomProperty(DocxDocument document, string name)
    {
        ValidateDocument(document);
        return document.GetCustomProperties().FindPropertyByName(name);
    }

    /// <summary>Creates or updates a custom property.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="name">The property name.</param>
    /// <param name="type">The variant type local name.</param>
    /// <param name="value">The serialized value.</param>
    /// <returns>The created or updated property.</returns>
    public DocxCustomProperty SetCustomProperty(DocxDocument document, string name, string type, string value)
    {
        ValidateDocument(document);
        DocxCustomProperties properties = document.GetOrCreateCustomProperties();
        DocxCustomProperty property = properties.SetProperty(name, type, value);
        document.WriteCustomPropertiesPart();
        return property;
    }

    /// <summary>Updates custom-property identity and link metadata.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="property">The property to update.</param>
    /// <param name="name">The replacement name.</param>
    /// <param name="formatId">The replacement format identifier.</param>
    /// <param name="linkTarget">The replacement link target.</param>
    public void SetCustomPropertyMetadata(DocxDocument document, DocxCustomProperty property, string name, string formatId, string linkTarget)
    {
        ValidateDocument(document);

        if (property == null)
        {
            throw new ArgumentNullException("property");
        }

        if (!document.GetCustomProperties().Properties.Contains(property))
        {
            throw new ArgumentException("Custom property does not belong to the document.", "property");
        }

        property.SetName(name);
        property.SetFormatId(formatId);
        property.SetLinkTarget(linkTarget);
        document.WriteCustomPropertiesPart();
    }

    /// <summary>Removes a custom property by name.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="name">The property name.</param>
    /// <returns>True when a property was removed.</returns>
    public bool RemoveCustomProperty(DocxDocument document, string name)
    {
        ValidateDocument(document);
        bool removed = document.GetCustomProperties().RemoveProperty(name);

        if (removed)
        {
            document.WriteCustomPropertiesPart();
        }

        return removed;
    }

    /// <summary>Returns the document settings.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The settings.</returns>
    public DocxSettings GetDocumentSettings(DocxDocument document)
    {
        ValidateDocument(document);
        return document.GetSettings();
    }

    /// <summary>Sets the document view.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="view">The WordprocessingML view value.</param>
    public void SetDocumentView(DocxDocument document, string view)
    {
        DocxSettings settings = GetWritableSettings(document);
        settings.SetView(view);
        document.WriteSettingsPart();
    }

    /// <summary>Sets zoom mode and percentage.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="view">The zoom view value, or an empty string.</param>
    /// <param name="percent">The zoom percentage text, or an empty string.</param>
    public void SetDocumentZoom(DocxDocument document, string view, string percent)
    {
        DocxSettings settings = GetWritableSettings(document);
        settings.SetZoom(view, percent);
        document.WriteSettingsPart();
    }

    /// <summary>Sets automatic hyphenation options.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="automatic">Whether automatic hyphenation is enabled.</param>
    /// <param name="zone">The hyphenation zone.</param>
    /// <param name="consecutiveLimit">The consecutive hyphen limit.</param>
    /// <param name="excludeCapitalizedWords">Whether capitalized words are excluded.</param>
    public void SetHyphenationOptions(DocxDocument document, bool automatic, string zone, string consecutiveLimit, bool excludeCapitalizedWords)
    {
        DocxSettings settings = GetWritableSettings(document);
        settings.SetAutoHyphenation(automatic);
        settings.SetHyphenationZone(zone);
        settings.SetConsecutiveHyphenLimit(consecutiveLimit);
        settings.SetDoNotHyphenateCaps(excludeCapitalizedWords);
        document.WriteSettingsPart();
    }

    /// <summary>Sets spelling and grammar proof states.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="spelling">The spelling proof state.</param>
    /// <param name="grammar">The grammar proof state.</param>
    public void SetProofingState(DocxDocument document, string spelling, string grammar)
    {
        DocxSettings settings = GetWritableSettings(document);
        settings.SetProofState(spelling, grammar);
        document.WriteSettingsPart();
    }

    /// <summary>Sets the default tab stop.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="value">The tab stop value.</param>
    public void SetDefaultTabStop(DocxDocument document, string value)
    {
        DocxSettings settings = GetWritableSettings(document);
        settings.SetDefaultTabStop(value);
        document.WriteSettingsPart();
    }

    /// <summary>Sets revision tracking behavior.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="trackRevisions">Whether revisions are tracked.</param>
    /// <param name="excludeMoves">Whether moves are recorded as ordinary insertions and deletions.</param>
    /// <param name="excludeFormatting">Whether formatting changes are not tracked.</param>
    public void SetRevisionBehavior(DocxDocument document, bool trackRevisions, bool excludeMoves, bool excludeFormatting)
    {
        DocxSettings settings = GetWritableSettings(document);
        settings.SetTrackRevisions(trackRevisions);
        settings.SetDoNotTrackMoves(excludeMoves);
        settings.SetDoNotTrackFormatting(excludeFormatting);
        document.WriteSettingsPart();
    }

    /// <summary>Sets whether fields update when the document opens.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="enabled">Whether fields update.</param>
    public void SetFieldUpdateOnOpen(DocxDocument document, bool enabled)
    {
        DocxSettings settings = GetWritableSettings(document);
        settings.SetUpdateFields(enabled);
        document.WriteSettingsPart();
    }

    /// <summary>Sets a document protection edit mode and enforcement.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="editMode">The WordprocessingML edit mode.</param>
    /// <param name="enforced">Whether protection is enforced.</param>
    public void SetDocumentProtection(DocxDocument document, string editMode, bool enforced)
    {
        DocxSettings settings = GetWritableSettings(document);
        settings.SetProtection(editMode, enforced);
        document.WriteSettingsPart();
    }

    /// <summary>Enables or disables enforced read-only protection.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="readOnly">Whether read-only protection is enforced.</param>
    public void SetDocumentReadOnly(DocxDocument document, bool readOnly)
    {
        DocxSettings settings = GetWritableSettings(document);

        if (readOnly)
        {
            settings.SetProtection("readOnly", true);
        }
        else
        {
            settings.ClearProtection();
        }

        document.WriteSettingsPart();
    }

    /// <summary>Removes document protection.</summary>
    /// <param name="document">The document to edit.</param>
    public void ClearDocumentProtection(DocxDocument document)
    {
        DocxSettings settings = GetWritableSettings(document);
        settings.ClearProtection();
        document.WriteSettingsPart();
    }

    /// <summary>Sets theme font languages.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="language">The default language.</param>
    /// <param name="eastAsiaLanguage">The East Asian language.</param>
    /// <param name="bidiLanguage">The bidirectional language.</param>
    public void SetDocumentLanguage(DocxDocument document, string language, string eastAsiaLanguage, string bidiLanguage)
    {
        DocxSettings settings = GetWritableSettings(document);
        settings.SetThemeFontLanguages(language, eastAsiaLanguage, bidiLanguage);
        document.WriteSettingsPart();
    }

    /// <summary>Returns compatibility settings in document order.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the settings.</returns>
    public List<DocxCompatibilitySetting> GetCompatibilitySettings(DocxDocument document)
    {
        ValidateDocument(document);
        return new List<DocxCompatibilitySetting>(document.GetSettings().CompatibilitySettings.Settings);
    }

    /// <summary>Finds a compatibility setting by name.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="name">The setting name.</param>
    /// <returns>The matching setting, or null.</returns>
    public DocxCompatibilitySetting? FindCompatibilitySetting(DocxDocument document, string name)
    {
        ValidateDocument(document);
        return document.GetSettings().CompatibilitySettings.FindSettingByName(name);
    }

    /// <summary>Creates or updates a compatibility setting.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="name">The setting name.</param>
    /// <param name="uri">The compatibility namespace URI.</param>
    /// <param name="value">The setting value.</param>
    /// <returns>The created or updated setting.</returns>
    public DocxCompatibilitySetting SetCompatibilitySetting(DocxDocument document, string name, string uri, string value)
    {
        DocxSettings settings = GetWritableSettings(document);
        DocxCompatibilitySetting setting = settings.GetOrCreateCompatibilitySettings().SetSetting(name, uri, value);
        document.WriteSettingsPart();
        return setting;
    }

    /// <summary>Removes a compatibility setting by name.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="name">The setting name.</param>
    /// <returns>True when a setting was removed.</returns>
    public bool RemoveCompatibilitySetting(DocxDocument document, string name)
    {
        ValidateDocument(document);
        bool removed = document.GetSettings().CompatibilitySettings.RemoveSetting(name);

        if (removed)
        {
            document.WriteSettingsPart();
        }

        return removed;
    }

    /// <summary>Returns font-table XML, or an empty string when absent.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The font-table XML.</returns>
    public string GetFontTableXml(DocxDocument document)
    {
        ValidateDocument(document);
        return GetPartText(document.GetFontTablePart());
    }

    /// <summary>Creates or replaces the font-table part.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="xml">The font-table XML.</param>
    public void SetFontTableXml(DocxDocument document, string xml)
    {
        ValidateDocument(document);
        ValidateXmlRoot(xml, "fonts", DocxConstants.NamespaceWordprocessingMl, "");
        document.FontTablePart = SetAuxiliaryPart(document, document.FontTablePart, "/word/fontTable.xml", DocxConstants.ContentTypeFontTable, DocxConstants.RelationshipTypeFontTable, xml);
    }

    /// <summary>Returns theme XML, or an empty string when absent.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The theme XML.</returns>
    public string GetThemeXml(DocxDocument document)
    {
        ValidateDocument(document);
        return GetPartText(document.GetThemePart());
    }

    /// <summary>Creates or replaces the theme part.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="xml">The theme XML.</param>
    public void SetThemeXml(DocxDocument document, string xml)
    {
        ValidateDocument(document);
        ValidateXmlRoot(xml, "theme", DocxConstants.NamespaceDrawingMl, DocxConstants.NamespaceStrictDrawingMl);
        document.ThemePart = SetAuxiliaryPart(document, document.ThemePart, "/word/theme/theme1.xml", DocxConstants.ContentTypeTheme, DocxConstants.RelationshipTypeTheme, xml);
    }

    /// <summary>Returns web-settings XML, or an empty string when absent.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The web-settings XML.</returns>
    public string GetWebSettingsXml(DocxDocument document)
    {
        ValidateDocument(document);
        return GetPartText(document.GetWebSettingsPart());
    }

    /// <summary>Creates or replaces the web-settings part.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="xml">The web-settings XML.</param>
    public void SetWebSettingsXml(DocxDocument document, string xml)
    {
        ValidateDocument(document);
        ValidateXmlRoot(xml, "webSettings", DocxConstants.NamespaceWordprocessingMl, "");
        document.WebSettingsPart = SetAuxiliaryPart(document, document.WebSettingsPart, "/word/webSettings.xml", DocxConstants.ContentTypeWebSettings, DocxConstants.RelationshipTypeWebSettings, xml);
    }

    internal static void ValidateDocument(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }
    }

    internal static DocxSettings GetWritableSettings(DocxDocument document)
    {
        ValidateDocument(document);
        return document.GetOrCreateSettings();
    }

    internal static string GetPartText(DocxPart? part)
    {
        if (part == null)
        {
            return "";
        }

        return part.GetText();
    }

    internal static void ValidateXmlRoot(string xml, string localName, string namespaceUri, string alternateNamespaceUri)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.LoadXml(xml);
        XmlElement? root = document.DocumentElement;

        if (root == null || root.LocalName != localName || (root.NamespaceURI != namespaceUri && root.NamespaceURI != alternateNamespaceUri))
        {
            throw new ArgumentException("XML root element is not valid for the requested part.", "xml");
        }
    }

    internal static DocxPart? SetAuxiliaryPart(DocxDocument document, DocxPart? part, string partName, string contentType, string relationshipType, string xml)
    {
        if (part == null)
        {
            DocxRelationshipSet relationships = document.GetOrCreateMainDocumentRelationshipSet();
            string target = document.GetMainDocumentRelativeTarget(partName);
            relationships.CreateRelationship(relationshipType, target, false);
            document.WriteRelationshipSetPart(relationships);
            document.Package.AddXmlPartWithContent(partName, contentType, xml);
        }
        else
        {
            partName = part.PartName;
            contentType = part.ContentType;
            document.Package.ReplaceXmlPart(partName, contentType, xml);
        }

        return document.Package.FindPart(partName);
    }
}
