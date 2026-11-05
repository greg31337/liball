using System;

namespace odf;

internal static class SettingsEditingModel
{
    internal static OdfSetting[] GetApplicationSettings(OdfDocument document)
    {
        RequireSettingsDocument(document);
        return CopySettings(document.ApplicationSettings);
    }

    internal static OdfSetting[] GetPortableSettings(OdfDocument document)
    {
        RequireSettingsDocument(document);
        return CopySettings(document.PortableSettings);
    }

    internal static OdfSetting[] GetViewSettings(OdfDocument document)
    {
        RequireSettingsDocument(document);
        return CopySettings(document.ViewSettings);
    }

    internal static OdfSetting? FindSetting(
        OdfDocument document,
        string name)
    {
        RequireSettingsDocument(document);
        if (name == null || name.Length == 0)
        {
            return null;
        }
        int count = 0;
        int index = 0;
        while (index < document.ApplicationSettings.Length)
        {
            count = count + CountNamedSettings(
                document.ApplicationSettings[index],
                name);
            if (count > 1)
            {
                return null;
            }
            index = index + 1;
        }
        if (count == 0)
        {
            return null;
        }
        index = 0;
        while (index < document.ApplicationSettings.Length)
        {
            OdfSetting? result = FindFirstNamedSetting(
                document.ApplicationSettings[index],
                name);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    internal static OdfSetting CreatePortableSettingSet(
        OdfDocument document,
        string name)
    {
        RequireSettingsDocument(document);
        RequireSettingName(name);
        if (IsViewSettingName(name))
        {
            throw new OdfEditException(
                "A view-settings name cannot be used for a portable setting set.");
        }
        return CreateTopLevelSet(
            document,
            name,
            OdfSettingScope.Portable);
    }

    internal static OdfSetting CreateViewSettingSet(
        OdfDocument document,
        string name)
    {
        RequireSettingsDocument(document);
        RequireSettingName(name);
        if (!IsViewSettingName(name))
        {
            throw new OdfEditException(
                "A view setting-set name must be 'view-settings' or end in ':view-settings'.");
        }
        return CreateTopLevelSet(document, name, OdfSettingScope.View);
    }

    internal static OdfSetting CreateSetting(
        OdfDocument document,
        OdfSetting parent,
        OdfSettingKind kind,
        string name,
        string valueType,
        string lexicalValue)
    {
        RequireCurrentSetting(document, parent);
        ValidateChildSetting(parent, kind, name, valueType, lexicalValue);
        OdfNode parentNode = parent.Node ?? new OdfNode();
        OdfNode node = CreateConfigNode(
            SettingLocalName(kind),
            parent.PartPath);
        WriteConfigAttribute(node, "name", name);
        if (kind == OdfSettingKind.Item)
        {
            WriteConfigAttribute(node, "type", valueType);
            AppendSettingText(node, lexicalValue);
        }
        StyleDocumentModel.AppendNode(parentNode, node);
        OdfSetting created =
            SettingDocumentModel.MaterializeCreatedSetting(
                node,
                parent.Scope);
        parent.Children = AppendSettingChild(parent.Children, created);
        return created;
    }

    internal static void DeleteSetting(
        OdfDocument document,
        OdfSetting setting)
    {
        RequireCurrentSetting(document, setting);
        OdfNode node = setting.Node ?? new OdfNode();
        StyleDocumentModel.RemoveNodeFromParent(node);
        int topIndex = IndexOfTopSetting(document, setting);
        if (topIndex >= 0)
        {
            document.ApplicationSettings = RemoveSettingAt(
                document.ApplicationSettings,
                topIndex);
            RefreshSettingScopes(document);
            UpdateSettingState(document);
            return;
        }
        OdfSetting? parent = FindSettingParent(document, setting);
        if (parent == null)
        {
            throw new OdfEditException(
                "The setting does not have a current parent.");
        }
        int childIndex = IndexOfSettingChild(parent.Children, setting);
        parent.Children = RemoveSettingAt(parent.Children, childIndex);
    }

    internal static void ApplySettingChanges(OdfDocument document)
    {
        RequireSettingsDocument(document);
        SettingDocumentModel.SynchronizeSettings(document);
    }

    static void RequireSettingsDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfEditException("A document is required.");
        }
    }

    static OdfSetting[] CopySettings(OdfSetting[] source)
    {
        OdfSetting[] result = new OdfSetting[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    static int CountNamedSettings(
        OdfSetting setting,
        string name)
    {
        int count = 0;
        if (setting.Name == name)
        {
            count = count + 1;
        }
        int index = 0;
        while (index < setting.Children.Length)
        {
            count = count + CountNamedSettings(setting.Children[index], name);
            index = index + 1;
        }
        return count;
    }

    static OdfSetting? FindFirstNamedSetting(
        OdfSetting setting,
        string name)
    {
        if (setting.Name == name)
        {
            return setting;
        }
        int index = 0;
        while (index < setting.Children.Length)
        {
            OdfSetting? result = FindFirstNamedSetting(
                setting.Children[index],
                name);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfSetting CreateTopLevelSet(
        OdfDocument document,
        string name,
        OdfSettingScope scope)
    {
        OdfNode section = EnsureSettingsSection(document);
        OdfNode node = CreateConfigNode(
            "config-item-set",
            section.Location.PartPath);
        WriteConfigAttribute(node, "name", name);
        StyleDocumentModel.AppendNode(section, node);
        OdfSetting created =
            SettingDocumentModel.MaterializeCreatedSetting(node, scope);
        document.ApplicationSettings = AppendSettingChild(
            document.ApplicationSettings,
            created);
        RefreshSettingScopes(document);
        UpdateSettingState(document);
        return created;
    }

    static OdfNode EnsureSettingsSection(OdfDocument document)
    {
        OdfPart? part = FindSettingsPart(document);
        if (part == null)
        {
            part = CreateSettingsPart(document);
        }
        if (part.Settings != null)
        {
            return part.Settings;
        }
        OdfNode? root = part.Root;
        if (root == null)
        {
            throw new OdfEditException(
                "A settings part requires a materialized XML root.");
        }
        OdfNode section = CreateOfficeNode(
            "settings",
            part.Path);
        section.NamespaceDeclarations =
        [
            CreateSettingNamespaceDeclaration(
                OdfNamespaces.ConfigPrefix,
                OdfNamespaces.Config,
                part.Path)
        ];
        StyleDocumentModel.InsertSection(
            root,
            section,
            StyleDocumentModel.SectionRank("settings"));
        StyleDocumentModel.RefreshPartMapping(document, part);
        return section;
    }

    static OdfPart? FindSettingsPart(OdfDocument document)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfPart part = document.Parts[index];
            if (part.Settings != null)
            {
                return part;
            }
            if (document.PackageKind == OdfPackageKind.Flat
                && part.IsFlatDocument)
            {
                return part;
            }
            if (document.PackageKind == OdfPackageKind.Package
                && part.Path == OdfPartNames.Settings)
            {
                return part;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfPart CreateSettingsPart(OdfDocument document)
    {
        if (document.PackageKind != OdfPackageKind.Package)
        {
            throw new OdfEditException(
                "A flat document requires a materialized document root.");
        }
        string version = "1.4";
        if (document.VersionInfo != null
            && document.VersionInfo.LexicalValue.Length > 0)
        {
            version = document.VersionInfo.LexicalValue;
        }
        OdfNode root = CreateOfficeNode(
            "document-settings",
            OdfPartNames.Settings);
        root.NamespaceDeclarations =
        [
            CreateSettingNamespaceDeclaration(
                OdfNamespaces.OfficePrefix,
                OdfNamespaces.Office,
                OdfPartNames.Settings),
            CreateSettingNamespaceDeclaration(
                OdfNamespaces.ConfigPrefix,
                OdfNamespaces.Config,
                OdfPartNames.Settings)
        ];
        WriteOfficeAttribute(root, "version", version);
        OdfPart part = new OdfPart();
        part.Path = OdfPartNames.Settings;
        part.MediaType = OdfMediaTypes.Xml;
        part.Nodes = [root];
        part.Root = root;
        part.RootName = "document-settings";
        part.VersionInfo = OdfVersionRules.ParseVersion(version);
        AppendSettingsPart(document, part);
        return part;
    }

    static void AppendSettingsPart(OdfDocument document, OdfPart part)
    {
        OdfPart[] parts = new OdfPart[document.Parts.Length + 1];
        Array.Copy(document.Parts, parts, document.Parts.Length);
        parts[parts.Length - 1] = part;
        document.Parts = parts;
        OdfNode[] nodes = new OdfNode[document.Nodes.Length + 1];
        Array.Copy(document.Nodes, nodes, document.Nodes.Length);
        nodes[nodes.Length - 1] = part.Root ?? new OdfNode();
        document.Nodes = nodes;
    }

    static OdfNode CreateOfficeNode(string localName, string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Office,
            localName,
            OdfNamespaces.OfficePrefix);
        result.Location.PartPath = partPath;
        return result;
    }

    static OdfNode CreateConfigNode(string localName, string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Config,
            localName,
            OdfNamespaces.ConfigPrefix);
        result.Location.PartPath = partPath;
        return result;
    }

    static OdfNamespaceDeclaration CreateSettingNamespaceDeclaration(
        string prefix,
        string namespaceName,
        string partPath)
    {
        OdfNamespaceDeclaration result = new OdfNamespaceDeclaration();
        result.Prefix = prefix;
        result.NamespaceName = namespaceName;
        result.Location.PartPath = partPath;
        return result;
    }

    static void WriteConfigAttribute(
        OdfNode node,
        string localName,
        string value)
    {
        if (value != null && value.Length > 0)
        {
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Config,
                localName,
                OdfNamespaces.ConfigPrefix,
                value);
        }
    }

    static void WriteOfficeAttribute(
        OdfNode node,
        string localName,
        string value)
    {
        StyleDocumentModel.WriteAttribute(
            node,
            OdfNamespaces.Office,
            localName,
            OdfNamespaces.OfficePrefix,
            value);
    }

    static void AppendSettingText(OdfNode parent, string value)
    {
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        StyleDocumentModel.AppendNode(parent, text);
    }

    static void RequireCurrentSetting(
        OdfDocument document,
        OdfSetting setting)
    {
        RequireSettingsDocument(document);
        if (setting == null || setting.Node == null
            || !ContainsSetting(document, setting))
        {
            throw new OdfEditException(
                "The setting does not belong to the document.");
        }
    }

    static bool ContainsSetting(
        OdfDocument document,
        OdfSetting setting)
    {
        int index = 0;
        while (index < document.ApplicationSettings.Length)
        {
            if (ContainsSettingBranch(document.ApplicationSettings[index], setting))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsSettingBranch(
        OdfSetting candidate,
        OdfSetting setting)
    {
        if (candidate == setting) return true;
        int index = 0;
        while (index < candidate.Children.Length)
        {
            if (ContainsSettingBranch(candidate.Children[index], setting))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RequireSettingName(string name)
    {
        if (name == null || name.Length == 0)
        {
            throw new OdfEditException("A setting name is required.");
        }
    }

    static void ValidateChildSetting(
        OdfSetting parent,
        OdfSettingKind kind,
        string name,
        string valueType,
        string lexicalValue)
    {
        if (kind == OdfSettingKind.Unknown)
        {
            throw new OdfEditException("A recognized setting kind is required.");
        }
        if (name == null || valueType == null || lexicalValue == null)
        {
            throw new OdfEditException("Setting values cannot be null.");
        }
        if (kind == OdfSettingKind.MapEntry)
        {
            if (parent.Kind != OdfSettingKind.IndexedMap
                && parent.Kind != OdfSettingKind.NamedMap)
            {
                throw new OdfEditException(
                    "A map entry must be created inside a settings map.");
            }
            if (parent.Kind == OdfSettingKind.NamedMap && name.Length == 0)
            {
                throw new OdfEditException(
                    "A named-map entry requires a name.");
            }
        }
        else
        {
            if (parent.Kind != OdfSettingKind.Set
                && parent.Kind != OdfSettingKind.MapEntry)
            {
                throw new OdfEditException(
                    "This setting container cannot contain the selected child kind.");
            }
            RequireSettingName(name);
        }
        if (kind == OdfSettingKind.Item)
        {
            if (!IsKnownSettingType(valueType))
            {
                throw new OdfEditException(
                    "A standard configuration item type is required.");
            }
        }
        else if (valueType.Length > 0 || lexicalValue.Length > 0)
        {
            throw new OdfEditException(
                "Setting containers cannot have a value type or lexical value.");
        }
    }

    static bool IsKnownSettingType(string valueType)
    {
        return valueType == "boolean"
            || valueType == "short"
            || valueType == "int"
            || valueType == "long"
            || valueType == "double"
            || valueType == "string"
            || valueType == "datetime"
            || valueType == "base64Binary";
    }

    static string SettingLocalName(OdfSettingKind kind)
    {
        if (kind == OdfSettingKind.Item) return "config-item";
        if (kind == OdfSettingKind.Set) return "config-item-set";
        if (kind == OdfSettingKind.IndexedMap) return "config-item-map-indexed";
        if (kind == OdfSettingKind.NamedMap) return "config-item-map-named";
        if (kind == OdfSettingKind.MapEntry) return "config-item-map-entry";
        throw new OdfEditException("A recognized setting kind is required.");
    }

    static bool IsViewSettingName(string name)
    {
        return name == "view-settings"
            || name.EndsWith(
                ":view-settings",
                StringComparison.OrdinalIgnoreCase);
    }

    static OdfSetting[] AppendSettingChild(
        OdfSetting[] source,
        OdfSetting item)
    {
        OdfSetting[] result = new OdfSetting[source.Length + 1];
        Array.Copy(source, result, source.Length);
        result[result.Length - 1] = item;
        return result;
    }

    static int IndexOfTopSetting(
        OdfDocument document,
        OdfSetting setting)
    {
        int index = 0;
        while (index < document.ApplicationSettings.Length)
        {
            if (document.ApplicationSettings[index] == setting) return index;
            index = index + 1;
        }
        return -1;
    }

    static int IndexOfSettingChild(
        OdfSetting[] children,
        OdfSetting setting)
    {
        int index = 0;
        while (index < children.Length)
        {
            if (children[index] == setting) return index;
            index = index + 1;
        }
        return -1;
    }

    static OdfSetting[] RemoveSettingAt(
        OdfSetting[] source,
        int removedIndex)
    {
        OdfSetting[] result = new OdfSetting[source.Length - 1];
        Array.Copy(source, 0, result, 0, removedIndex);
        Array.Copy(source, removedIndex + 1, result, removedIndex, source.Length - removedIndex - 1);
        return result;
    }

    static OdfSetting? FindSettingParent(
        OdfDocument document,
        OdfSetting setting)
    {
        int index = 0;
        while (index < document.ApplicationSettings.Length)
        {
            OdfSetting? result = FindParentBranch(
                document.ApplicationSettings[index],
                setting);
            if (result != null) return result;
            index = index + 1;
        }
        return null;
    }

    static OdfSetting? FindParentBranch(
        OdfSetting candidate,
        OdfSetting setting)
    {
        int index = 0;
        while (index < candidate.Children.Length)
        {
            if (candidate.Children[index] == setting) return candidate;
            OdfSetting? result = FindParentBranch(
                candidate.Children[index],
                setting);
            if (result != null) return result;
            index = index + 1;
        }
        return null;
    }

    static void RefreshSettingScopes(OdfDocument document)
    {
        int portableCount = 0;
        int viewCount = 0;
        int index = 0;
        while (index < document.ApplicationSettings.Length)
        {
            if (IsViewSettingName(document.ApplicationSettings[index].Name))
                viewCount = viewCount + 1;
            else
                portableCount = portableCount + 1;
            index = index + 1;
        }
        OdfSetting[] portable = new OdfSetting[portableCount];
        OdfSetting[] view = new OdfSetting[viewCount];
        int portableIndex = 0;
        int viewIndex = 0;
        index = 0;
        while (index < document.ApplicationSettings.Length)
        {
            OdfSetting setting = document.ApplicationSettings[index];
            OdfSettingScope scope = IsViewSettingName(setting.Name)
                ? OdfSettingScope.View
                : OdfSettingScope.Portable;
            AssignSettingScope(setting, scope);
            if (scope == OdfSettingScope.View)
            {
                view[viewIndex] = setting;
                viewIndex = viewIndex + 1;
            }
            else
            {
                portable[portableIndex] = setting;
                portableIndex = portableIndex + 1;
            }
            index = index + 1;
        }
        document.PortableSettings = portable;
        document.ViewSettings = view;
    }

    static void AssignSettingScope(
        OdfSetting setting,
        OdfSettingScope scope)
    {
        setting.Scope = scope;
        int index = 0;
        while (index < setting.Children.Length)
        {
            AssignSettingScope(setting.Children[index], scope);
            index = index + 1;
        }
    }

    static void UpdateSettingState(OdfDocument document)
    {
        if (document.SettingState == null)
        {
            document.SettingState = new SettingDocumentState();
        }
        document.SettingState.Settings = document.ApplicationSettings;
    }
}
