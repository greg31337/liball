using System;
using System.Collections.Generic;
using System.Globalization;

namespace odf;

internal static class SettingDocumentModel
{
    internal static OdfSetting MaterializeCreatedSetting(
        OdfNode node,
        OdfSettingScope scope)
    {
        return LoadSetting(node, scope);
    }

    internal static void LoadSettings(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for settings materialization.");
        }
        List<OdfSetting> settings = new List<OdfSetting>();
        List<OdfSetting> portable = new List<OdfSetting>();
        List<OdfSetting> view = new List<OdfSetting>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode? section = document.Parts[partIndex].Settings;
            if (section != null)
            {
                LoadTopLevelSets(section, settings, portable, view);
            }
            partIndex = partIndex + 1;
        }
        document.ApplicationSettings = settings.ToArray();
        document.PortableSettings = portable.ToArray();
        document.ViewSettings = view.ToArray();
        SettingDocumentState state = new SettingDocumentState();
        state.Settings = document.ApplicationSettings;
        document.SettingState = state;
    }

    internal static void SynchronizeSettings(OdfDocument document)
    {
        if (document == null || document.ApplicationSettings == null)
        {
            throw new OdfDocumentException(
                "A document and application settings are required for writing.");
        }
        int index = 0;
        while (index < document.ApplicationSettings.Length)
        {
            OdfSetting setting = document.ApplicationSettings[index];
            if (setting == null)
            {
                throw new OdfDocumentException(
                    "Application setting collections cannot contain null entries.");
            }
            SynchronizeSetting(setting);
            index = index + 1;
        }
        RefreshScopes(document);
        if (document.SettingState == null)
        {
            document.SettingState = new SettingDocumentState();
        }
        document.SettingState.Settings = document.ApplicationSettings;
    }

    static void LoadTopLevelSets(
        OdfNode section,
        List<OdfSetting> settings,
        List<OdfSetting> portable,
        List<OdfSetting> view)
    {
        int index = 0;
        while (index < section.Children.Length)
        {
            OdfNode node = section.Children[index];
            if (IsConfigElement(node, "config-item-set"))
            {
                string name = ReadConfigAttribute(node, "name");
                OdfSettingScope scope = IsViewName(name)
                    ? OdfSettingScope.View
                    : OdfSettingScope.Portable;
                OdfSetting setting = LoadSetting(node, scope);
                settings.Add(setting);
                if (scope == OdfSettingScope.View)
                {
                    view.Add(setting);
                }
                else
                {
                    portable.Add(setting);
                }
            }
            index = index + 1;
        }
    }

    static OdfSetting LoadSetting(
        OdfNode node,
        OdfSettingScope scope)
    {
        OdfSetting result = new OdfSetting();
        result.Kind = SettingKind(node);
        result.Scope = scope;
        result.Name = ReadConfigAttribute(node, "name");
        result.Attributes = node.Attributes;
        result.PartPath = node.Location.PartPath;
        result.Node = node;
        if (result.Kind == OdfSettingKind.Item)
        {
            result.ValueType = ReadConfigAttribute(node, "type");
            result.LexicalValue = TextContent(node);
            ParseValue(result);
        }
        else
        {
            List<OdfSetting> children = new List<OdfSetting>();
            int index = 0;
            while (index < node.Children.Length)
            {
                OdfNode child = node.Children[index];
                if (SettingKind(child) != OdfSettingKind.Unknown)
                {
                    children.Add(LoadSetting(child, scope));
                }
                index = index + 1;
            }
            result.Children = children.ToArray();
        }
        CaptureOriginals(result);
        return result;
    }

    static void SynchronizeSetting(OdfSetting setting)
    {
        if (setting.Node == null)
        {
            return;
        }
        WriteChangedAttribute(
            setting.Node,
            "name",
            setting.Name,
            setting.OriginalName);
        if (setting.Kind == OdfSettingKind.Item)
        {
            SynchronizeItem(setting);
        }
        else
        {
            int index = 0;
            while (index < setting.Children.Length)
            {
                OdfSetting child = setting.Children[index];
                if (child == null)
                {
                    throw new OdfDocumentException(
                        "Setting child collections cannot contain null entries.");
                }
                SynchronizeSetting(child);
                index = index + 1;
            }
        }
        CaptureOriginals(setting);
    }

    static void SynchronizeItem(OdfSetting setting)
    {
        if (setting.Node == null)
        {
            return;
        }
        bool lexicalChanged = setting.LexicalValue
            != setting.OriginalLexicalValue;
        bool typeChanged = setting.ValueType != setting.OriginalValueType;
        bool typedChanged = TypedValueChanged(setting);
        if (lexicalChanged || typeChanged)
        {
            ParseValue(setting);
        }
        else if (typedChanged)
        {
            setting.LexicalValue = FormatTypedValue(setting);
            ParseValue(setting);
        }
        WriteChangedAttribute(
            setting.Node,
            "type",
            setting.ValueType,
            setting.OriginalValueType);
        if (setting.LexicalValue != setting.OriginalLexicalValue)
        {
            SetTextContent(setting.Node, setting.LexicalValue);
        }
    }

    static void RefreshScopes(OdfDocument document)
    {
        List<OdfSetting> portable = new List<OdfSetting>();
        List<OdfSetting> view = new List<OdfSetting>();
        int index = 0;
        while (index < document.ApplicationSettings.Length)
        {
            OdfSetting setting = document.ApplicationSettings[index];
            OdfSettingScope scope = IsViewName(setting.Name)
                ? OdfSettingScope.View
                : OdfSettingScope.Portable;
            AssignScope(setting, scope);
            if (scope == OdfSettingScope.View)
            {
                view.Add(setting);
            }
            else
            {
                portable.Add(setting);
            }
            index = index + 1;
        }
        document.PortableSettings = portable.ToArray();
        document.ViewSettings = view.ToArray();
    }

    static void AssignScope(OdfSetting setting, OdfSettingScope scope)
    {
        setting.Scope = scope;
        int index = 0;
        while (index < setting.Children.Length)
        {
            AssignScope(setting.Children[index], scope);
            index = index + 1;
        }
    }

    static void ParseValue(OdfSetting setting)
    {
        setting.ValueKind = ValueKind(setting.ValueType);
        setting.IsValid = false;
        setting.StringValue = "";
        setting.BooleanValue = false;
        setting.IntegerValue = 0;
        setting.NumericValue = 0;
        setting.BinaryValue = [];
        if (setting.ValueKind == OdfSettingValueKind.String)
        {
            setting.StringValue = setting.LexicalValue;
            setting.IsValid = true;
            return;
        }
        if (setting.ValueKind == OdfSettingValueKind.DateTime)
        {
            OdfTemporalValue value = OdfLexicalParser.ParseDateTime(
                setting.LexicalValue);
            setting.StringValue = setting.LexicalValue;
            setting.IsValid = value.IsValid;
            return;
        }
        if (setting.ValueKind == OdfSettingValueKind.Boolean)
        {
            OdfLexicalValue value = OdfLexicalParser.ParseBoolean(
                setting.LexicalValue);
            setting.BooleanValue = value.BooleanValue;
            setting.IsValid = value.IsValid;
            return;
        }
        if (setting.ValueKind == OdfSettingValueKind.Short
            || setting.ValueKind == OdfSettingValueKind.Integer
            || setting.ValueKind == OdfSettingValueKind.Long)
        {
            OdfLexicalValue value = OdfLexicalParser.ParseInteger(
                setting.LexicalValue);
            setting.IntegerValue = value.IntegerValue;
            setting.IsValid = value.IsValid
                && IntegerFits(setting.ValueKind, value.IntegerValue);
            return;
        }
        if (setting.ValueKind == OdfSettingValueKind.Double)
        {
            OdfLexicalValue value = OdfLexicalParser.ParseFloat(
                setting.LexicalValue);
            setting.NumericValue = value.FloatValue;
            setting.IsValid = value.IsValid;
            return;
        }
        if (setting.ValueKind == OdfSettingValueKind.Base64Binary)
        {
            OdfLexicalValue value = OdfLexicalParser.ParseBase64(
                setting.LexicalValue);
            setting.BinaryValue = value.BinaryValue;
            setting.IsValid = value.IsValid;
        }
    }

    static OdfSettingValueKind ValueKind(string type)
    {
        if (type == "boolean")
        {
            return OdfSettingValueKind.Boolean;
        }
        if (type == "short")
        {
            return OdfSettingValueKind.Short;
        }
        if (type == "int")
        {
            return OdfSettingValueKind.Integer;
        }
        if (type == "long")
        {
            return OdfSettingValueKind.Long;
        }
        if (type == "double")
        {
            return OdfSettingValueKind.Double;
        }
        if (type == "string")
        {
            return OdfSettingValueKind.String;
        }
        if (type == "datetime")
        {
            return OdfSettingValueKind.DateTime;
        }
        if (type == "base64Binary")
        {
            return OdfSettingValueKind.Base64Binary;
        }
        return OdfSettingValueKind.Unknown;
    }

    static OdfSettingKind SettingKind(OdfNode node)
    {
        if (IsConfigElement(node, "config-item"))
        {
            return OdfSettingKind.Item;
        }
        if (IsConfigElement(node, "config-item-set"))
        {
            return OdfSettingKind.Set;
        }
        if (IsConfigElement(node, "config-item-map-indexed"))
        {
            return OdfSettingKind.IndexedMap;
        }
        if (IsConfigElement(node, "config-item-map-named"))
        {
            return OdfSettingKind.NamedMap;
        }
        if (IsConfigElement(node, "config-item-map-entry"))
        {
            return OdfSettingKind.MapEntry;
        }
        return OdfSettingKind.Unknown;
    }

    static bool IntegerFits(OdfSettingValueKind kind, long value)
    {
        if (kind == OdfSettingValueKind.Short)
        {
            return value >= short.MinValue && value <= short.MaxValue;
        }
        if (kind == OdfSettingValueKind.Integer)
        {
            return value >= int.MinValue && value <= int.MaxValue;
        }
        return true;
    }

    static bool TypedValueChanged(OdfSetting setting)
    {
        if (setting.ValueKind == OdfSettingValueKind.Boolean)
        {
            return setting.BooleanValue != setting.OriginalBooleanValue;
        }
        if (setting.ValueKind == OdfSettingValueKind.Short
            || setting.ValueKind == OdfSettingValueKind.Integer
            || setting.ValueKind == OdfSettingValueKind.Long)
        {
            return setting.IntegerValue != setting.OriginalIntegerValue;
        }
        if (setting.ValueKind == OdfSettingValueKind.Double)
        {
            return setting.NumericValue != setting.OriginalNumericValue;
        }
        if (setting.ValueKind == OdfSettingValueKind.String
            || setting.ValueKind == OdfSettingValueKind.DateTime)
        {
            return setting.StringValue != setting.OriginalStringValue;
        }
        if (setting.ValueKind == OdfSettingValueKind.Base64Binary)
        {
            return !SameBytes(
                setting.BinaryValue,
                setting.OriginalBinaryValue);
        }
        return false;
    }

    static string FormatTypedValue(OdfSetting setting)
    {
        if (setting.ValueKind == OdfSettingValueKind.Boolean)
        {
            return setting.BooleanValue ? "true" : "false";
        }
        if (setting.ValueKind == OdfSettingValueKind.Short
            || setting.ValueKind == OdfSettingValueKind.Integer
            || setting.ValueKind == OdfSettingValueKind.Long)
        {
            if (!IntegerFits(setting.ValueKind, setting.IntegerValue))
            {
                throw new OdfDocumentException(
                    "A configuration integer is outside its declared range.");
            }
            return setting.IntegerValue.ToString(
                CultureInfo.InvariantCulture);
        }
        if (setting.ValueKind == OdfSettingValueKind.Double)
        {
            return FormatDouble(setting.NumericValue);
        }
        if (setting.ValueKind == OdfSettingValueKind.String
            || setting.ValueKind == OdfSettingValueKind.DateTime)
        {
            return setting.StringValue;
        }
        if (setting.ValueKind == OdfSettingValueKind.Base64Binary)
        {
            return Convert.ToBase64String(setting.BinaryValue);
        }
        return setting.LexicalValue;
    }

    static string FormatDouble(double value)
    {
        if (double.IsPositiveInfinity(value))
        {
            return "INF";
        }
        if (double.IsNegativeInfinity(value))
        {
            return "-INF";
        }
        if (double.IsNaN(value))
        {
            return "NaN";
        }
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    static bool IsViewName(string name)
    {
        return name == "view-settings"
            || name.EndsWith(
                ":view-settings",
                StringComparison.OrdinalIgnoreCase);
    }

    static bool IsConfigElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Config
            && node.Name.LocalName == localName;
    }

    static string ReadConfigAttribute(OdfNode node, string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Config,
            localName);
    }

    static string TextContent(OdfNode node)
    {
        string result = "";
        int index = 0;
        while (index < node.Children.Length)
        {
            if (node.Children[index].Kind == OdfNodeKind.Text)
            {
                result = result + node.Children[index].Value;
            }
            index = index + 1;
        }
        return result;
    }

    static void SetTextContent(OdfNode node, string value)
    {
        List<OdfNode> children = new List<OdfNode>();
        bool inserted = false;
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (child.Kind == OdfNodeKind.Text)
            {
                if (!inserted)
                {
                    OdfNode text = new OdfNode();
                    text.Kind = OdfNodeKind.Text;
                    text.Value = value;
                    text.Parent = node;
                    text.Location.PartPath = node.Location.PartPath;
                    children.Add(text);
                    inserted = true;
                }
                child.Parent = null;
            }
            else
            {
                children.Add(child);
            }
            index = index + 1;
        }
        if (!inserted)
        {
            OdfNode text = new OdfNode();
            text.Kind = OdfNodeKind.Text;
            text.Value = value;
            text.Parent = node;
            text.Location.PartPath = node.Location.PartPath;
            children.Add(text);
        }
        node.Children = children.ToArray();
    }

    static void WriteChangedAttribute(
        OdfNode node,
        string localName,
        string value,
        string original)
    {
        if (value == original)
        {
            return;
        }
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Config,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Config,
                localName,
                OdfNamespaces.ConfigPrefix,
                value);
        }
    }

    static bool SameBytes(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }
        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static byte[] CopyBytes(byte[] value)
    {
        byte[] result = new byte[value.Length];
        Array.Copy(value, result, value.Length);
        return result;
    }

    static void CaptureOriginals(OdfSetting setting)
    {
        setting.OriginalName = setting.Name;
        setting.OriginalValueType = setting.ValueType;
        setting.OriginalLexicalValue = setting.LexicalValue;
        setting.OriginalStringValue = setting.StringValue;
        setting.OriginalBooleanValue = setting.BooleanValue;
        setting.OriginalIntegerValue = setting.IntegerValue;
        setting.OriginalNumericValue = setting.NumericValue;
        setting.OriginalBinaryValue = CopyBytes(setting.BinaryValue);
    }
}
