namespace odf;

/// <summary>Provides structural and field editing for application settings.</summary>
public interface IOdfSettingsEditor
{
    /// <summary>Gets all top-level application settings.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array containing the settings.</returns>
    public OdfSetting[] GetApplicationSettings(OdfDocument document);
    /// <summary>Gets top-level portable application settings.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array containing the portable settings.</returns>
    public OdfSetting[] GetPortableSettings(OdfDocument document);
    /// <summary>Gets top-level view and user-interface settings.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array containing the view settings.</returns>
    public OdfSetting[] GetViewSettings(OdfDocument document);
    /// <summary>Finds an unambiguously named setting anywhere in the settings tree.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="name">The setting name.</param>
    /// <returns>The unique match, or <c>null</c>.</returns>
    public OdfSetting? FindSetting(OdfDocument document, string name);
    /// <summary>Creates a top-level portable setting set.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="name">The non-view setting-set name.</param>
    /// <returns>The created setting set.</returns>
    public OdfSetting CreatePortableSettingSet(
        OdfDocument document,
        string name);
    /// <summary>Creates a top-level view setting set.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="name">The view setting-set name.</param>
    /// <returns>The created setting set.</returns>
    public OdfSetting CreateViewSettingSet(
        OdfDocument document,
        string name);
    /// <summary>Creates a child setting, set, map, or map entry.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="parent">The owning setting container.</param>
    /// <param name="kind">The child setting kind.</param>
    /// <param name="name">The child name, which may be empty for an indexed-map entry.</param>
    /// <param name="valueType">The item type, or an empty string for containers.</param>
    /// <param name="lexicalValue">The item text, or an empty string for containers.</param>
    /// <returns>The created child setting.</returns>
    public OdfSetting CreateSetting(
        OdfDocument document,
        OdfSetting parent,
        OdfSettingKind kind,
        string name,
        string valueType,
        string lexicalValue);
    /// <summary>Deletes a setting and its descendants.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="setting">The setting to delete.</param>
    public void DeleteSetting(OdfDocument document, OdfSetting setting);
    /// <summary>Applies direct changes made to setting data fields.</summary>
    /// <param name="document">The document to modify.</param>
    public void ApplySettingChanges(OdfDocument document);
}
