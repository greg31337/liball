namespace odf;

/// <summary>Provides structural and field editing for application settings.</summary>
public class OdfSettingsEditor : IOdfSettingsEditor
{
    /// <summary>Initializes an application-settings editor.</summary>
    public OdfSettingsEditor()
    {
    }

    /// <summary>Gets all top-level application settings.</summary>
    public OdfSetting[] GetApplicationSettings(OdfDocument document)
    {
        return SettingsEditingModel.GetApplicationSettings(document);
    }

    /// <summary>Gets top-level portable application settings.</summary>
    public OdfSetting[] GetPortableSettings(OdfDocument document)
    {
        return SettingsEditingModel.GetPortableSettings(document);
    }

    /// <summary>Gets top-level view and user-interface settings.</summary>
    public OdfSetting[] GetViewSettings(OdfDocument document)
    {
        return SettingsEditingModel.GetViewSettings(document);
    }

    /// <summary>Finds an unambiguously named setting anywhere in the settings tree.</summary>
    public OdfSetting? FindSetting(OdfDocument document, string name)
    {
        return SettingsEditingModel.FindSetting(document, name);
    }

    /// <summary>Creates a top-level portable setting set.</summary>
    public OdfSetting CreatePortableSettingSet(
        OdfDocument document,
        string name)
    {
        return SettingsEditingModel.CreatePortableSettingSet(document, name);
    }

    /// <summary>Creates a top-level view setting set.</summary>
    public OdfSetting CreateViewSettingSet(
        OdfDocument document,
        string name)
    {
        return SettingsEditingModel.CreateViewSettingSet(document, name);
    }

    /// <summary>Creates a child setting, set, map, or map entry.</summary>
    public OdfSetting CreateSetting(
        OdfDocument document,
        OdfSetting parent,
        OdfSettingKind kind,
        string name,
        string valueType,
        string lexicalValue)
    {
        return SettingsEditingModel.CreateSetting(
            document,
            parent,
            kind,
            name,
            valueType,
            lexicalValue);
    }

    /// <summary>Deletes a setting and its descendants.</summary>
    public void DeleteSetting(OdfDocument document, OdfSetting setting)
    {
        SettingsEditingModel.DeleteSetting(document, setting);
    }

    /// <summary>Applies direct changes made to setting data fields.</summary>
    public void ApplySettingChanges(OdfDocument document)
    {
        SettingsEditingModel.ApplySettingChanges(document);
    }
}
