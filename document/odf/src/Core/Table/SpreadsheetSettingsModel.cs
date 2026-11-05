using System;
using System.Globalization;
using System.Text;

namespace odf;

internal class SpreadsheetSettingsState
{
    internal OdfNode? SpreadsheetNode;
    internal OdfNode? SettingsSection;
    internal OdfPart? SettingsPart;
}

internal static class SpreadsheetSettingsModel
{
    internal static void LoadSpreadsheetSettings(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for spreadsheet-settings materialization.");
        }
        SpreadsheetSettingsState state = new SpreadsheetSettingsState();
        OdfSpreadsheetSettings settings = new OdfSpreadsheetSettings();
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfPart part = document.Parts[index];
            if (part.Root != null && state.SpreadsheetNode == null)
            {
                state.SpreadsheetNode = FindElement(
                    part.Root,
                    OdfNamespaces.Office,
                    "spreadsheet");
            }
            if (part.Settings != null && state.SettingsSection == null)
            {
                state.SettingsSection = part.Settings;
                state.SettingsPart = part;
            }
            index = index + 1;
        }
        LoadSpreadsheetProtection(settings, state.SpreadsheetNode);
        LoadCalculationSettings(settings, state.SpreadsheetNode);
        LoadViewSettings(settings, state.SettingsSection);
        RememberOriginals(settings);
        document.SpreadsheetSettings = settings;
        document.SpreadsheetSettingsState = state;
    }

    internal static void SynchronizeSpreadsheetSettings(
        OdfDocument document,
        OdfSaveOptions options)
    {
        if (document == null
            || document.SpreadsheetSettings == null
            || options == null)
        {
            throw new OdfDocumentException(
                "A document, spreadsheet settings, and save options are required for writing.");
        }
        SpreadsheetSettingsState? state = document.SpreadsheetSettingsState;
        if (state == null)
        {
            LoadSpreadsheetSettings(document);
            state = document.SpreadsheetSettingsState;
        }
        if (state == null)
        {
            return;
        }
        OdfSpreadsheetSettings settings = document.SpreadsheetSettings;
        Validate(settings);
        SynchronizeSpreadsheetProtection(
            document,
            settings,
            state.SpreadsheetNode,
            options);
        SynchronizeCalculationSettings(settings, state.SpreadsheetNode);
        SynchronizeViewSettings(document, settings, state);
        RememberOriginals(settings);
    }

    static void LoadSpreadsheetProtection(
        OdfSpreadsheetSettings settings,
        OdfNode? spreadsheet)
    {
        if (spreadsheet == null)
        {
            return;
        }
        settings.StructureProtected = ReadBoolean(
            spreadsheet,
            "structure-protected",
            false);
        settings.ProtectionKey = StyleDocumentModel.ReadAttribute(
            spreadsheet,
            OdfNamespaces.Table,
            "protection-key");
        settings.ProtectionKeyDigestAlgorithm =
            StyleDocumentModel.ReadAttribute(
                spreadsheet,
                OdfNamespaces.Table,
                "protection-key-digest-algorithm");
    }

    static void SynchronizeSpreadsheetProtection(
        OdfDocument document,
        OdfSpreadsheetSettings settings,
        OdfNode? spreadsheet,
        OdfSaveOptions options)
    {
        bool changed = settings.StructureProtected
                != settings.OriginalStructureProtected
            || settings.ProtectionKey != settings.OriginalProtectionKey
            || settings.ProtectionKeyDigestAlgorithm
                != settings.OriginalProtectionKeyDigestAlgorithm;
        if (spreadsheet == null)
        {
            if (changed)
            {
                throw new OdfDocumentException(
                    "Spreadsheet structure protection requires a spreadsheet body.");
            }
            return;
        }
        if (settings.StructureProtected
            != settings.OriginalStructureProtected)
        {
            StyleDocumentModel.WriteAttribute(
                spreadsheet,
                OdfNamespaces.Table,
                "structure-protected",
                OdfNamespaces.TablePrefix,
                settings.StructureProtected ? "true" : "false");
        }
        if (settings.ProtectionKey != settings.OriginalProtectionKey)
        {
            WriteOptionalTableAttribute(
                spreadsheet,
                "protection-key",
                settings.ProtectionKey);
        }
        TableDataViewModel.WriteProtectionDigest(
            document,
            spreadsheet,
            settings.ProtectionKeyDigestAlgorithm,
            settings.OriginalProtectionKeyDigestAlgorithm,
            options);
    }

    static void LoadCalculationSettings(
        OdfSpreadsheetSettings settings,
        OdfNode? spreadsheet)
    {
        if (spreadsheet == null)
        {
            return;
        }
        OdfNode? calculation = FindDirectChild(
            spreadsheet,
            OdfNamespaces.Table,
            "calculation-settings");
        if (calculation == null)
        {
            return;
        }
        settings.HasCalculationSettings = true;
        settings.CalculationNode = calculation;
        settings.CaseSensitive = ReadBoolean(
            calculation,
            "case-sensitive",
            true);
        settings.PrecisionAsShown = ReadBoolean(
            calculation,
            "precision-as-shown",
            false);
        settings.MatchWholeCell = ReadBoolean(
            calculation,
            "search-criteria-must-apply-to-whole-cell",
            true);
        settings.AutomaticFindLabels = ReadBoolean(
            calculation,
            "automatic-find-labels",
            true);
        settings.UseRegularExpressions = ReadBoolean(
            calculation,
            "use-regular-expressions",
            true);
        settings.UseWildcards = ReadBoolean(
            calculation,
            "use-wildcards",
            false);
        settings.NullYear = ReadPositiveInteger(
            calculation,
            "null-year",
            1930);
        OdfNode? nullDate = FindDirectChild(
            calculation,
            OdfNamespaces.Table,
            "null-date");
        if (nullDate != null)
        {
            settings.HasNullDate = true;
            settings.NullDateNode = nullDate;
            settings.NullDate = ReadDefaultedAttribute(
                nullDate,
                "date-value",
                "1899-12-30");
        }
        OdfNode? iteration = FindDirectChild(
            calculation,
            OdfNamespaces.Table,
            "iteration");
        if (iteration != null)
        {
            settings.HasIteration = true;
            settings.IterationNode = iteration;
            settings.IterationEnabled =
                ReadDefaultedAttribute(iteration, "status", "disable")
                == "enable";
            settings.IterationSteps = ReadPositiveInteger(
                iteration,
                "steps",
                100);
            settings.MaximumDifference = ReadDouble(
                iteration,
                "maximum-difference",
                0.001);
        }
    }

    static void LoadViewSettings(
        OdfSpreadsheetSettings settings,
        OdfNode? section)
    {
        if (section == null)
        {
            return;
        }
        settings.ActiveTableNode = FindConfigItem(section, "ActiveTable");
        settings.SelectedTablesNode = FindConfigItem(section, "SelectedTables");
        settings.SelectedRangesNode = FindConfigItem(section, "SelectedRanges");
        settings.ActiveTable = ReadNodeText(settings.ActiveTableNode);
        settings.SelectedTables = ReadNodeText(settings.SelectedTablesNode);
        settings.SelectedRanges = ReadNodeText(settings.SelectedRangesNode);
    }

    static void SynchronizeCalculationSettings(
        OdfSpreadsheetSettings settings,
        OdfNode? spreadsheet)
    {
        PromoteChangedOptionalSettings(settings);
        if (!settings.HasCalculationSettings)
        {
            if (settings.CalculationNode != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(
                    settings.CalculationNode);
                settings.CalculationNode = null;
                settings.NullDateNode = null;
                settings.IterationNode = null;
            }
            return;
        }
        if (spreadsheet == null)
        {
            throw new OdfDocumentException(
                "Calculation settings require a spreadsheet body.");
        }
        bool isNew = settings.CalculationNode == null;
        OdfNode calculation = settings.CalculationNode
            ?? CreateElement(
                OdfNamespaces.Table,
                "calculation-settings",
                OdfNamespaces.TablePrefix,
                spreadsheet.Location.PartPath);
        if (isNew)
        {
            InsertCalculationSettings(spreadsheet, calculation);
            settings.CalculationNode = calculation;
        }
        WriteChangedBoolean(
            calculation,
            "case-sensitive",
            settings.CaseSensitive,
            settings.OriginalCaseSensitive,
            isNew);
        WriteChangedBoolean(
            calculation,
            "precision-as-shown",
            settings.PrecisionAsShown,
            settings.OriginalPrecisionAsShown,
            isNew);
        WriteChangedBoolean(
            calculation,
            "search-criteria-must-apply-to-whole-cell",
            settings.MatchWholeCell,
            settings.OriginalMatchWholeCell,
            isNew);
        WriteChangedBoolean(
            calculation,
            "automatic-find-labels",
            settings.AutomaticFindLabels,
            settings.OriginalAutomaticFindLabels,
            isNew);
        WriteChangedBoolean(
            calculation,
            "use-regular-expressions",
            settings.UseRegularExpressions,
            settings.OriginalUseRegularExpressions,
            isNew);
        WriteChangedBoolean(
            calculation,
            "use-wildcards",
            settings.UseWildcards,
            settings.OriginalUseWildcards,
            isNew);
        if (isNew || settings.NullYear != settings.OriginalNullYear)
        {
            StyleDocumentModel.WriteAttribute(
                calculation,
                OdfNamespaces.Table,
                "null-year",
                OdfNamespaces.TablePrefix,
                settings.NullYear.ToString(CultureInfo.InvariantCulture));
        }
        SynchronizeNullDate(settings, calculation);
        SynchronizeIteration(settings, calculation);
    }

    static void PromoteChangedOptionalSettings(
        OdfSpreadsheetSettings settings)
    {
        if (!settings.OriginalHasNullDate
            && !settings.HasNullDate
            && settings.NullDate != settings.OriginalNullDate)
        {
            settings.HasNullDate = true;
        }
        if (!settings.OriginalHasIteration
            && !settings.HasIteration
            && (settings.IterationEnabled
                    != settings.OriginalIterationEnabled
                || settings.IterationSteps
                    != settings.OriginalIterationSteps
                || settings.MaximumDifference
                    != settings.OriginalMaximumDifference))
        {
            settings.HasIteration = true;
        }
        if (!settings.OriginalHasCalculationSettings
            && !settings.HasCalculationSettings
            && (settings.CaseSensitive
                    != settings.OriginalCaseSensitive
                || settings.PrecisionAsShown
                    != settings.OriginalPrecisionAsShown
                || settings.MatchWholeCell
                    != settings.OriginalMatchWholeCell
                || settings.AutomaticFindLabels
                    != settings.OriginalAutomaticFindLabels
                || settings.UseRegularExpressions
                    != settings.OriginalUseRegularExpressions
                || settings.UseWildcards
                    != settings.OriginalUseWildcards
                || settings.NullYear != settings.OriginalNullYear
                || settings.HasNullDate
                || settings.HasIteration))
        {
            settings.HasCalculationSettings = true;
        }
    }

    static void SynchronizeNullDate(
        OdfSpreadsheetSettings settings,
        OdfNode calculation)
    {
        if (!settings.HasNullDate)
        {
            if (settings.NullDateNode != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(settings.NullDateNode);
                settings.NullDateNode = null;
            }
            return;
        }
        bool isNew = settings.NullDateNode == null;
        OdfNode nullDate = settings.NullDateNode
            ?? CreateElement(
                OdfNamespaces.Table,
                "null-date",
                OdfNamespaces.TablePrefix,
                calculation.Location.PartPath);
        if (isNew)
        {
            InsertCalculationChild(calculation, nullDate, 0);
            settings.NullDateNode = nullDate;
            StyleDocumentModel.WriteAttribute(
                nullDate,
                OdfNamespaces.Table,
                "value-type",
                OdfNamespaces.TablePrefix,
                "date");
        }
        if (isNew || settings.NullDate != settings.OriginalNullDate)
        {
            StyleDocumentModel.WriteAttribute(
                nullDate,
                OdfNamespaces.Table,
                "date-value",
                OdfNamespaces.TablePrefix,
                settings.NullDate);
        }
    }

    static void SynchronizeIteration(
        OdfSpreadsheetSettings settings,
        OdfNode calculation)
    {
        if (!settings.HasIteration)
        {
            if (settings.IterationNode != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(settings.IterationNode);
                settings.IterationNode = null;
            }
            return;
        }
        bool isNew = settings.IterationNode == null;
        OdfNode iteration = settings.IterationNode
            ?? CreateElement(
                OdfNamespaces.Table,
                "iteration",
                OdfNamespaces.TablePrefix,
                calculation.Location.PartPath);
        if (isNew)
        {
            InsertCalculationChild(calculation, iteration, 1);
            settings.IterationNode = iteration;
        }
        if (isNew
            || settings.IterationEnabled
                != settings.OriginalIterationEnabled)
        {
            StyleDocumentModel.WriteAttribute(
                iteration,
                OdfNamespaces.Table,
                "status",
                OdfNamespaces.TablePrefix,
                settings.IterationEnabled ? "enable" : "disable");
        }
        if (isNew || settings.IterationSteps != settings.OriginalIterationSteps)
        {
            StyleDocumentModel.WriteAttribute(
                iteration,
                OdfNamespaces.Table,
                "steps",
                OdfNamespaces.TablePrefix,
                settings.IterationSteps.ToString(
                    CultureInfo.InvariantCulture));
        }
        if (isNew
            || settings.MaximumDifference
                != settings.OriginalMaximumDifference)
        {
            StyleDocumentModel.WriteAttribute(
                iteration,
                OdfNamespaces.Table,
                "maximum-difference",
                OdfNamespaces.TablePrefix,
                settings.MaximumDifference.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
        }
    }

    static void SynchronizeViewSettings(
        OdfDocument document,
        OdfSpreadsheetSettings settings,
        SpreadsheetSettingsState state)
    {
        bool needsSection = settings.ActiveTable.Length > 0
            || settings.SelectedTables.Length > 0
            || settings.SelectedRanges.Length > 0;
        if (state.SettingsSection == null && needsSection)
        {
            CreateSettingsSection(document, state);
        }
        if (state.SettingsSection == null)
        {
            return;
        }
        OdfNode? container = null;
        SynchronizeConfigItem(
            state.SettingsSection,
            "ActiveTable",
            settings.ActiveTable,
            settings.OriginalActiveTable,
            settings.ActiveTableNode,
            container);
        settings.ActiveTableNode = FindConfigItem(
            state.SettingsSection,
            "ActiveTable");
        container = FindViewSettingsContainer(state.SettingsSection);
        SynchronizeConfigItem(
            state.SettingsSection,
            "SelectedTables",
            settings.SelectedTables,
            settings.OriginalSelectedTables,
            settings.SelectedTablesNode,
            container);
        settings.SelectedTablesNode = FindConfigItem(
            state.SettingsSection,
            "SelectedTables");
        container = FindViewSettingsContainer(state.SettingsSection);
        SynchronizeConfigItem(
            state.SettingsSection,
            "SelectedRanges",
            settings.SelectedRanges,
            settings.OriginalSelectedRanges,
            settings.SelectedRangesNode,
            container);
        settings.SelectedRangesNode = FindConfigItem(
            state.SettingsSection,
            "SelectedRanges");
    }

    static void SynchronizeConfigItem(
        OdfNode section,
        string name,
        string value,
        string original,
        OdfNode? node,
        OdfNode? container)
    {
        if (value == original && node != null)
        {
            return;
        }
        if (value.Length == 0)
        {
            if (node != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            return;
        }
        OdfNode item = node ?? CreateConfigItem(
            name,
            section.Location.PartPath);
        if (node == null)
        {
            OdfNode destination = container
                ?? EnsureViewSettingsContainer(section);
            StyleDocumentModel.AppendNode(destination, item);
        }
        WriteNodeText(item, value);
    }

    static void CreateSettingsSection(
        OdfDocument document,
        SpreadsheetSettingsState state)
    {
        OdfPart? flatPart = null;
        int index = 0;
        while (index < document.Parts.Length)
        {
            if (document.Parts[index].IsFlatDocument)
            {
                flatPart = document.Parts[index];
            }
            index = index + 1;
        }
        if (flatPart != null && flatPart.Root != null)
        {
            OdfNode section = CreateElement(
                OdfNamespaces.Office,
                "settings",
                OdfNamespaces.OfficePrefix,
                flatPart.Path);
            section.NamespaceDeclarations =
            [
                NamespaceDeclaration(
                    OdfNamespaces.ConfigPrefix,
                    OdfNamespaces.Config,
                    flatPart.Path)
            ];
            StyleDocumentModel.InsertSection(
                flatPart.Root,
                section,
                StyleDocumentModel.SectionRank("settings"));
            StyleDocumentModel.RefreshPartMapping(document, flatPart);
            state.SettingsSection = section;
            state.SettingsPart = flatPart;
            return;
        }
        if (document.PackageKind != OdfPackageKind.Package)
        {
            throw new OdfDocumentException(
                "Spreadsheet view settings require a flat root or package settings part.");
        }
        OdfPart part = CreateSettingsPart(document);
        state.SettingsPart = part;
        state.SettingsSection = part.Settings;
    }

    static OdfPart CreateSettingsPart(OdfDocument document)
    {
        string path = OdfPartNames.Settings;
        OdfNode root = CreateElement(
            OdfNamespaces.Office,
            "document-settings",
            OdfNamespaces.OfficePrefix,
            path);
        root.NamespaceDeclarations =
        [
            NamespaceDeclaration(
                OdfNamespaces.OfficePrefix,
                OdfNamespaces.Office,
                path),
            NamespaceDeclaration(
                OdfNamespaces.ConfigPrefix,
                OdfNamespaces.Config,
                path)
        ];
        string version = document.VersionInfo != null
            && document.VersionInfo.LexicalValue.Length > 0
            ? document.VersionInfo.LexicalValue
            : "1.4";
        StyleDocumentModel.WriteAttribute(
            root,
            OdfNamespaces.Office,
            "version",
            OdfNamespaces.OfficePrefix,
            version);
        OdfNode section = CreateElement(
            OdfNamespaces.Office,
            "settings",
            OdfNamespaces.OfficePrefix,
            path);
        StyleDocumentModel.AppendNode(root, section);
        OdfPart part = new OdfPart();
        part.Path = path;
        part.MediaType = OdfMediaTypes.Xml;
        part.Nodes = [root];
        part.Root = root;
        part.RootName = "document-settings";
        part.Settings = section;
        part.VersionInfo = OdfVersionRules.ParseVersion(version);
        OdfPart[] parts = new OdfPart[document.Parts.Length + 1];
        Array.Copy(document.Parts, parts, document.Parts.Length);
        parts[parts.Length - 1] = part;
        document.Parts = parts;
        OdfNode[] nodes = new OdfNode[document.Nodes.Length + 1];
        Array.Copy(document.Nodes, nodes, document.Nodes.Length);
        nodes[nodes.Length - 1] = root;
        document.Nodes = nodes;
        StyleDocumentModel.RefreshPartMapping(document, part);
        return part;
    }

    static OdfNode EnsureViewSettingsContainer(OdfNode section)
    {
        OdfNode? existing = FindViewSettingsContainer(section);
        if (existing != null)
        {
            return existing;
        }
        OdfNode result = CreateElement(
            OdfNamespaces.Config,
            "config-item-set",
            OdfNamespaces.ConfigPrefix,
            section.Location.PartPath);
        StyleDocumentModel.WriteAttribute(
            result,
            OdfNamespaces.Config,
            "name",
            OdfNamespaces.ConfigPrefix,
            "ooo:view-settings");
        StyleDocumentModel.AppendNode(section, result);
        return result;
    }

    static OdfNode? FindViewSettingsContainer(OdfNode section)
    {
        int index = 0;
        while (index < section.Children.Length)
        {
            OdfNode child = section.Children[index];
            if (IsElement(child, OdfNamespaces.Config, "config-item-set")
                && StyleDocumentModel.ReadAttribute(
                    child,
                    OdfNamespaces.Config,
                    "name") == "ooo:view-settings")
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode CreateConfigItem(string name, string partPath)
    {
        OdfNode result = CreateElement(
            OdfNamespaces.Config,
            "config-item",
            OdfNamespaces.ConfigPrefix,
            partPath);
        StyleDocumentModel.WriteAttribute(
            result,
            OdfNamespaces.Config,
            "name",
            OdfNamespaces.ConfigPrefix,
            name);
        StyleDocumentModel.WriteAttribute(
            result,
            OdfNamespaces.Config,
            "type",
            OdfNamespaces.ConfigPrefix,
            "string");
        return result;
    }

    static void InsertCalculationSettings(
        OdfNode spreadsheet,
        OdfNode calculation)
    {
        int insertion = spreadsheet.Children.Length;
        int index = 0;
        while (index < spreadsheet.Children.Length)
        {
            OdfNode child = spreadsheet.Children[index];
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && child.Name.NamespaceName == OdfNamespaces.Table
                && child.Name.LocalName != "tracked-changes")
            {
                insertion = index;
                index = spreadsheet.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        InsertChild(spreadsheet, calculation, insertion);
    }

    static void InsertCalculationChild(
        OdfNode calculation,
        OdfNode child,
        int rank)
    {
        int insertion = calculation.Children.Length;
        int index = 0;
        while (index < calculation.Children.Length)
        {
            OdfNode candidate = calculation.Children[index];
            int candidateRank = IsElement(
                candidate,
                OdfNamespaces.Table,
                "null-date") ? 0 : 1;
            if (candidate.Kind == OdfNodeKind.Element
                && candidateRank > rank)
            {
                insertion = index;
                index = calculation.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        InsertChild(calculation, child, insertion);
    }

    static void InsertChild(OdfNode parent, OdfNode child, int insertion)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, 0, children, 0, insertion);
        children[insertion] = child;
        Array.Copy(
            parent.Children,
            insertion,
            children,
            insertion + 1,
            parent.Children.Length - insertion);
        parent.Children = children;
        child.Parent = parent;
        child.Location.PartPath = parent.Location.PartPath;
        parent.WasEmptyElement = false;
    }

    static OdfNode? FindElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (IsElement(node, namespaceName, localName))
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = FindElement(
                node.Children[index],
                namespaceName,
                localName);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindDirectChild(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            if (IsElement(node.Children[index], namespaceName, localName))
            {
                return node.Children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindConfigItem(OdfNode node, string name)
    {
        if (IsElement(node, OdfNamespaces.Config, "config-item")
            && StyleDocumentModel.ReadAttribute(
                node,
                OdfNamespaces.Config,
                "name") == name)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = FindConfigItem(node.Children[index], name);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName;
    }

    static OdfNode CreateElement(
        string namespaceName,
        string localName,
        string prefix,
        string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            namespaceName,
            localName,
            prefix);
        result.Location.PartPath = partPath;
        return result;
    }

    static OdfNamespaceDeclaration NamespaceDeclaration(
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

    static string ReadDefaultedAttribute(
        OdfNode node,
        string localName,
        string defaultValue)
    {
        string value = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Table,
            localName);
        return value.Length > 0 ? value : defaultValue;
    }

    static bool ReadBoolean(
        OdfNode node,
        string localName,
        bool defaultValue)
    {
        OdfLexicalValue parsed = OdfLexicalParser.ParseBoolean(
            StyleDocumentModel.ReadAttribute(
                node,
                OdfNamespaces.Table,
                localName));
        return parsed.IsValid ? parsed.BooleanValue : defaultValue;
    }

    static long ReadPositiveInteger(
        OdfNode node,
        string localName,
        long defaultValue)
    {
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(
            StyleDocumentModel.ReadAttribute(
                node,
                OdfNamespaces.Table,
                localName));
        return parsed.IsValid && parsed.IntegerValue > 0
            ? parsed.IntegerValue
            : defaultValue;
    }

    static double ReadDouble(
        OdfNode node,
        string localName,
        double defaultValue)
    {
        OdfLexicalValue parsed = OdfLexicalParser.ParseFloat(
            StyleDocumentModel.ReadAttribute(
                node,
                OdfNamespaces.Table,
                localName));
        return parsed.IsValid ? parsed.FloatValue : defaultValue;
    }

    static void WriteChangedBoolean(
        OdfNode node,
        string localName,
        bool value,
        bool original,
        bool isNew)
    {
        if (!isNew && value == original)
        {
            return;
        }
        StyleDocumentModel.WriteAttribute(
            node,
            OdfNamespaces.Table,
            localName,
            OdfNamespaces.TablePrefix,
            value ? "true" : "false");
    }

    static void WriteOptionalTableAttribute(
        OdfNode node,
        string localName,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Table,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Table,
                localName,
                OdfNamespaces.TablePrefix,
                value);
        }
    }

    static string ReadNodeText(OdfNode? node)
    {
        if (node == null)
        {
            return "";
        }
        StringBuilder result = new StringBuilder();
        AppendNodeText(node, result);
        return result.ToString();
    }

    static void AppendNodeText(OdfNode node, StringBuilder result)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendNodeText(node.Children[index], result);
            index = index + 1;
        }
    }

    static void WriteNodeText(OdfNode node, string value)
    {
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        text.Parent = node;
        text.Location.PartPath = node.Location.PartPath;
        node.Children = [text];
        node.WasEmptyElement = false;
    }

    static void Validate(OdfSpreadsheetSettings settings)
    {
        if (settings.NullYear <= 0)
        {
            throw new OdfDocumentException(
                "The spreadsheet null year must be a positive integer.");
        }
        if (settings.HasNullDate
            && !OdfLexicalParser.ParseDate(settings.NullDate).IsValid)
        {
            throw new OdfDocumentException(
                "The spreadsheet null date is not a valid date value.");
        }
        if (settings.IterationSteps <= 0)
        {
            throw new OdfDocumentException(
                "The spreadsheet iteration step count must be a positive integer.");
        }
        if (settings.MaximumDifference < 0
            || double.IsNaN(settings.MaximumDifference))
        {
            throw new OdfDocumentException(
                "The spreadsheet iteration maximum difference must be non-negative.");
        }
        if (settings.UseRegularExpressions && settings.UseWildcards)
        {
            throw new OdfDocumentException(
                "Regular-expression and wildcard matching cannot both be enabled.");
        }
    }

    static void RememberOriginals(OdfSpreadsheetSettings settings)
    {
        settings.OriginalHasCalculationSettings =
            settings.HasCalculationSettings;
        settings.OriginalHasNullDate = settings.HasNullDate;
        settings.OriginalHasIteration = settings.HasIteration;
        settings.OriginalCaseSensitive = settings.CaseSensitive;
        settings.OriginalPrecisionAsShown = settings.PrecisionAsShown;
        settings.OriginalMatchWholeCell = settings.MatchWholeCell;
        settings.OriginalAutomaticFindLabels = settings.AutomaticFindLabels;
        settings.OriginalUseRegularExpressions =
            settings.UseRegularExpressions;
        settings.OriginalUseWildcards = settings.UseWildcards;
        settings.OriginalNullYear = settings.NullYear;
        settings.OriginalNullDate = settings.NullDate;
        settings.OriginalIterationEnabled = settings.IterationEnabled;
        settings.OriginalIterationSteps = settings.IterationSteps;
        settings.OriginalMaximumDifference = settings.MaximumDifference;
        settings.OriginalActiveTable = settings.ActiveTable;
        settings.OriginalSelectedTables = settings.SelectedTables;
        settings.OriginalSelectedRanges = settings.SelectedRanges;
        settings.OriginalStructureProtected = settings.StructureProtected;
        settings.OriginalProtectionKey = settings.ProtectionKey;
        settings.OriginalProtectionKeyDigestAlgorithm =
            settings.ProtectionKeyDigestAlgorithm;
    }
}
