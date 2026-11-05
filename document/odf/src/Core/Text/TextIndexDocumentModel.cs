using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class TextIndexDocumentState
{
    internal OdfNode[] OriginalIndexNodes = [];
    internal OdfNode[] OriginalConfigurationNodes = [];
    internal OdfNode[] OriginalEntryNodes = [];
}

internal static class TextIndexDocumentModel
{
    internal static void LoadTextIndexes(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for index materialization.");
        }
        List<OdfTextIndex> indexes = new List<OdfTextIndex>();
        List<OdfBibliographyConfiguration> configurations =
            new List<OdfBibliographyConfiguration>();
        List<OdfBibliographyEntry> entries =
            new List<OdfBibliographyEntry>();
        List<OdfNode> indexNodes = new List<OdfNode>();
        List<OdfNode> configurationNodes = new List<OdfNode>();
        List<OdfNode> entryNodes = new List<OdfNode>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            CollectConfigurations(
                part.Styles,
                part.Path,
                configurations,
                configurationNodes);
            if (part.Body != null)
            {
                CollectContent(
                    document,
                    part.Body,
                    part.Path,
                    indexes,
                    entries,
                    indexNodes,
                    entryNodes);
            }
            partIndex = partIndex + 1;
        }
        document.TextIndexes = indexes.ToArray();
        document.BibliographyConfigurations = configurations.ToArray();
        document.BibliographyEntries = entries.ToArray();
        TextIndexDocumentState state = new TextIndexDocumentState();
        state.OriginalIndexNodes = indexNodes.ToArray();
        state.OriginalConfigurationNodes = configurationNodes.ToArray();
        state.OriginalEntryNodes = entryNodes.ToArray();
        document.TextIndexState = state;
    }

    internal static void SynchronizeTextIndexes(OdfDocument document)
    {
        if (document == null
            || document.TextIndexes == null
            || document.BibliographyConfigurations == null
            || document.BibliographyEntries == null)
        {
            throw new OdfDocumentException(
                "Index and bibliography collections are required for writing.");
        }
        TextIndexDocumentState state = document.TextIndexState
            ?? new TextIndexDocumentState();
        RemoveDeletedIndexes(document.TextIndexes, state.OriginalIndexNodes);
        RemoveDeletedConfigurations(
            document.BibliographyConfigurations,
            state.OriginalConfigurationNodes);
        RemoveDeletedEntries(
            document.BibliographyEntries,
            state.OriginalEntryNodes);
        List<OdfNode> indexNodes = new List<OdfNode>();
        int index = 0;
        while (index < document.TextIndexes.Length)
        {
            OdfTextIndex item = document.TextIndexes[index];
            if (item == null || item.Node == null)
            {
                throw new OdfEditException(
                    "A text index requires an underlying XML element.");
            }
            SynchronizeIndex(item);
            indexNodes.Add(item.Node);
            index = index + 1;
        }
        List<OdfNode> configurationNodes = new List<OdfNode>();
        index = 0;
        while (index < document.BibliographyConfigurations.Length)
        {
            OdfBibliographyConfiguration configuration =
                document.BibliographyConfigurations[index];
            if (configuration == null)
            {
                throw new OdfDocumentException(
                    "The bibliography-configuration collection contains null.");
            }
            SynchronizeConfiguration(document, configuration);
            if (configuration.Node != null)
            {
                configurationNodes.Add(configuration.Node);
            }
            index = index + 1;
        }
        List<OdfNode> entryNodes = new List<OdfNode>();
        index = 0;
        while (index < document.BibliographyEntries.Length)
        {
            OdfBibliographyEntry entry = document.BibliographyEntries[index];
            if (entry == null || entry.Node == null)
            {
                throw new OdfEditException(
                    "A bibliography entry requires an underlying XML element.");
            }
            SynchronizeEntry(entry);
            entryNodes.Add(entry.Node);
            index = index + 1;
        }
        state.OriginalIndexNodes = indexNodes.ToArray();
        state.OriginalConfigurationNodes = configurationNodes.ToArray();
        state.OriginalEntryNodes = entryNodes.ToArray();
        document.TextIndexState = state;
    }

    static void CollectConfigurations(
        OdfNode? styles,
        string partPath,
        List<OdfBibliographyConfiguration> configurations,
        List<OdfNode> nodes)
    {
        if (styles == null)
        {
            return;
        }
        int index = 0;
        while (index < styles.Children.Length)
        {
            OdfNode child = styles.Children[index];
            if (IsTextElement(child, "bibliography-configuration"))
            {
                configurations.Add(CreateConfiguration(child, partPath));
                nodes.Add(child);
            }
            index = index + 1;
        }
    }

    static void CollectContent(
        OdfDocument document,
        OdfNode node,
        string partPath,
        List<OdfTextIndex> indexes,
        List<OdfBibliographyEntry> entries,
        List<OdfNode> indexNodes,
        List<OdfNode> entryNodes)
    {
        OdfTextIndexKind kind = IndexKind(node);
        if (kind != OdfTextIndexKind.Unknown)
        {
            indexes.Add(CreateIndex(document, node, partPath, kind));
            indexNodes.Add(node);
        }
        if (IsTextElement(node, "bibliography-mark"))
        {
            entries.Add(CreateEntry(document, node, partPath));
            entryNodes.Add(node);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectContent(
                document,
                node.Children[index],
                partPath,
                indexes,
                entries,
                indexNodes,
                entryNodes);
            index = index + 1;
        }
    }

    static OdfTextIndex CreateIndex(
        OdfDocument document,
        OdfNode node,
        string partPath,
        OdfTextIndexKind kind)
    {
        OdfTextIndex result = new OdfTextIndex();
        result.Kind = kind;
        result.Name = Read(node, OdfNamespaces.Text, "name");
        result.StyleName = Read(node, OdfNamespaces.Text, "style-name");
        result.Protected = Read(node, OdfNamespaces.Text, "protected");
        result.Attributes = node.Attributes;
        result.Source = CreateSource(
            FindTextChild(node, SourceElementName(kind)),
            kind);
        result.BodyNode = FindTextChild(node, "index-body");
        if (result.BodyNode != null)
        {
            result.BodyBlocks = ContainedBlocks(
                document.TextBlocks,
                result.BodyNode);
        }
        result.PartPath = partPath;
        result.Node = node;
        return result;
    }

    static OdfTextIndexSource? CreateSource(
        OdfNode? node,
        OdfTextIndexKind kind)
    {
        if (node == null)
        {
            return null;
        }
        OdfTextIndexSource result = new OdfTextIndexSource();
        result.Kind = kind;
        result.OutlineLevel = Read(node, OdfNamespaces.Text, "outline-level");
        result.IndexScope = Read(node, OdfNamespaces.Text, "index-scope");
        result.RelativeTabStopPosition = Read(
            node,
            OdfNamespaces.Text,
            "relative-tab-stop-position");
        result.UseIndexMarks = Read(node, OdfNamespaces.Text, "use-index-marks");
        result.UseIndexSourceStyles = Read(
            node,
            OdfNamespaces.Text,
            "use-index-source-styles");
        result.IndexName = Read(node, OdfNamespaces.Text, "index-name");
        result.CaptionSequenceName = Read(
            node,
            OdfNamespaces.Text,
            "caption-sequence-name");
        result.CaptionSequenceFormat = Read(
            node,
            OdfNamespaces.Text,
            "caption-sequence-format");
        result.MainEntryStyleName = Read(
            node,
            OdfNamespaces.Text,
            "main-entry-style-name");
        result.SortAlgorithm = Read(node, OdfNamespaces.Text, "sort-algorithm");
        result.Language = Read(node, OdfNamespaces.Fo, "language");
        result.Country = Read(node, OdfNamespaces.Fo, "country");
        result.TitleNode = FindTextChild(node, "index-title-template");
        if (result.TitleNode != null)
        {
            result.Title = ExtractText(result.TitleNode);
            result.TitleStyleName = Read(
                result.TitleNode,
                OdfNamespaces.Text,
                "style-name");
        }
        result.OriginalTitle = result.Title;
        result.Templates = CreateTemplates(node);
        result.SourceStyles = CreateSourceStyles(node);
        result.Attributes = node.Attributes;
        result.Node = node;
        return result;
    }

    static OdfIndexEntryTemplate[] CreateTemplates(OdfNode source)
    {
        List<OdfIndexEntryTemplate> result =
            new List<OdfIndexEntryTemplate>();
        int index = 0;
        while (index < source.Children.Length)
        {
            OdfNode child = source.Children[index];
            if (IsEntryTemplate(child))
            {
                OdfIndexEntryTemplate template = new OdfIndexEntryTemplate();
                template.OutlineLevel = Read(
                    child,
                    OdfNamespaces.Text,
                    "outline-level");
                template.BibliographyType = Read(
                    child,
                    OdfNamespaces.Text,
                    "bibliography-type");
                template.StyleName = Read(
                    child,
                    OdfNamespaces.Text,
                    "style-name");
                template.Components = CreateComponents(child);
                template.Attributes = child.Attributes;
                template.Node = child;
                result.Add(template);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfIndexEntryComponent[] CreateComponents(OdfNode template)
    {
        List<OdfIndexEntryComponent> result =
            new List<OdfIndexEntryComponent>();
        int index = 0;
        while (index < template.Children.Length)
        {
            OdfNode child = template.Children[index];
            OdfIndexEntryComponentKind kind = ComponentKind(child);
            if (kind != OdfIndexEntryComponentKind.Unknown)
            {
                OdfIndexEntryComponent component =
                    new OdfIndexEntryComponent();
                component.Kind = kind;
                component.StyleName = Read(
                    child,
                    OdfNamespaces.Text,
                    "style-name");
                component.Text = ExtractText(child);
                component.OriginalText = component.Text;
                component.Display = Read(child, OdfNamespaces.Text, "display");
                component.OutlineLevel = Read(
                    child,
                    OdfNamespaces.Text,
                    "outline-level");
                component.BibliographyDataField = Read(
                    child,
                    OdfNamespaces.Text,
                    "bibliography-data-field");
                component.TabType = Read(child, OdfNamespaces.Style, "type");
                component.TabPosition = Read(
                    child,
                    OdfNamespaces.Style,
                    "position");
                component.LeaderCharacter = Read(
                    child,
                    OdfNamespaces.Style,
                    "leader-char");
                component.Attributes = child.Attributes;
                component.Node = child;
                result.Add(component);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfIndexSourceStyle[] CreateSourceStyles(OdfNode source)
    {
        List<OdfIndexSourceStyle> result =
            new List<OdfIndexSourceStyle>();
        int index = 0;
        while (index < source.Children.Length)
        {
            OdfNode child = source.Children[index];
            if (IsTextElement(child, "index-source-styles"))
            {
                OdfIndexSourceStyle group = new OdfIndexSourceStyle();
                group.OutlineLevel = Read(
                    child,
                    OdfNamespaces.Text,
                    "outline-level");
                List<string> names = new List<string>();
                int styleIndex = 0;
                while (styleIndex < child.Children.Length)
                {
                    OdfNode style = child.Children[styleIndex];
                    if (IsTextElement(style, "index-source-style"))
                    {
                        names.Add(Read(
                            style,
                            OdfNamespaces.Text,
                            "style-name"));
                    }
                    styleIndex = styleIndex + 1;
                }
                group.StyleNames = names.ToArray();
                group.Node = child;
                result.Add(group);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfBibliographyConfiguration CreateConfiguration(
        OdfNode node,
        string partPath)
    {
        OdfBibliographyConfiguration result =
            new OdfBibliographyConfiguration();
        result.Prefix = Read(node, OdfNamespaces.Text, "prefix");
        result.Suffix = Read(node, OdfNamespaces.Text, "suffix");
        result.NumberedEntries = Read(
            node,
            OdfNamespaces.Text,
            "numbered-entries");
        result.SortByPosition = Read(
            node,
            OdfNamespaces.Text,
            "sort-by-position");
        result.SortAlgorithm = Read(node, OdfNamespaces.Text, "sort-algorithm");
        result.Language = Read(node, OdfNamespaces.Fo, "language");
        result.Country = Read(node, OdfNamespaces.Fo, "country");
        List<OdfBibliographySortKey> keys =
            new List<OdfBibliographySortKey>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsTextElement(child, "sort-key"))
            {
                OdfBibliographySortKey key = new OdfBibliographySortKey();
                key.Key = Read(child, OdfNamespaces.Text, "key");
                key.SortAscending = Read(
                    child,
                    OdfNamespaces.Text,
                    "sort-ascending");
                key.Node = child;
                keys.Add(key);
            }
            index = index + 1;
        }
        result.SortKeys = keys.ToArray();
        result.Attributes = node.Attributes;
        result.PartPath = partPath;
        result.Node = node;
        return result;
    }

    static OdfBibliographyEntry CreateEntry(
        OdfDocument document,
        OdfNode node,
        string partPath)
    {
        OdfBibliographyEntry result = new OdfBibliographyEntry();
        result.Identifier = Read(node, OdfNamespaces.Text, "identifier");
        result.BibliographyType = Read(
            node,
            OdfNamespaces.Text,
            "bibliography-type");
        result.Author = Read(node, OdfNamespaces.Text, "author");
        result.Title = Read(node, OdfNamespaces.Text, "title");
        result.Year = Read(node, OdfNamespaces.Text, "year");
        result.Url = Read(node, OdfNamespaces.Text, "url");
        result.DisplayText = ExtractText(node);
        result.OriginalDisplayText = result.DisplayText;
        result.Attributes = node.Attributes;
        result.TextBlock = FindContainingBlock(document.TextBlocks, node);
        result.Run = FindRun(document.TextBlocks, node);
        result.PartPath = partPath;
        result.Node = node;
        return result;
    }

    static void SynchronizeIndex(OdfTextIndex item)
    {
        OdfNode node = item.Node ?? new OdfNode();
        ApplyPreservedAttributes(node, item.Attributes);
        WriteOptional(node, OdfNamespaces.Text, "name", OdfNamespaces.TextPrefix, item.Name);
        WriteOptional(node, OdfNamespaces.Text, "style-name", OdfNamespaces.TextPrefix, item.StyleName);
        WriteOptional(node, OdfNamespaces.Text, "protected", OdfNamespaces.TextPrefix, item.Protected);
        if (item.Source != null)
        {
            SynchronizeSource(item.Source);
        }
        item.Attributes = node.Attributes;
    }

    static void SynchronizeSource(OdfTextIndexSource source)
    {
        if (source.Node == null)
        {
            throw new OdfEditException(
                "An index source requires an underlying XML element.");
        }
        OdfNode node = source.Node;
        ApplyPreservedAttributes(node, source.Attributes);
        WriteOptional(node, OdfNamespaces.Text, "outline-level", OdfNamespaces.TextPrefix, source.OutlineLevel);
        WriteOptional(node, OdfNamespaces.Text, "index-scope", OdfNamespaces.TextPrefix, source.IndexScope);
        WriteOptional(node, OdfNamespaces.Text, "relative-tab-stop-position", OdfNamespaces.TextPrefix, source.RelativeTabStopPosition);
        WriteOptional(node, OdfNamespaces.Text, "use-index-marks", OdfNamespaces.TextPrefix, source.UseIndexMarks);
        WriteOptional(node, OdfNamespaces.Text, "use-index-source-styles", OdfNamespaces.TextPrefix, source.UseIndexSourceStyles);
        WriteOptional(node, OdfNamespaces.Text, "index-name", OdfNamespaces.TextPrefix, source.IndexName);
        WriteOptional(node, OdfNamespaces.Text, "caption-sequence-name", OdfNamespaces.TextPrefix, source.CaptionSequenceName);
        WriteOptional(node, OdfNamespaces.Text, "caption-sequence-format", OdfNamespaces.TextPrefix, source.CaptionSequenceFormat);
        WriteOptional(node, OdfNamespaces.Text, "main-entry-style-name", OdfNamespaces.TextPrefix, source.MainEntryStyleName);
        WriteOptional(node, OdfNamespaces.Text, "sort-algorithm", OdfNamespaces.TextPrefix, source.SortAlgorithm);
        WriteOptional(node, OdfNamespaces.Fo, "language", OdfNamespaces.FoPrefix, source.Language);
        WriteOptional(node, OdfNamespaces.Fo, "country", OdfNamespaces.FoPrefix, source.Country);
        if (source.TitleNode != null)
        {
            WriteOptional(source.TitleNode, OdfNamespaces.Text, "style-name", OdfNamespaces.TextPrefix, source.TitleStyleName);
            if (source.Title != source.OriginalTitle)
            {
                ReplaceText(source.TitleNode, source.Title);
                source.OriginalTitle = source.Title;
            }
        }
        else if (source.Title.Length > 0)
        {
            source.TitleNode = CreateTextElement(
                "index-title-template",
                node.Location.PartPath);
            WriteOptional(source.TitleNode, OdfNamespaces.Text, "style-name", OdfNamespaces.TextPrefix, source.TitleStyleName);
            ReplaceText(source.TitleNode, source.Title);
            StyleDocumentModel.AppendNode(node, source.TitleNode);
            source.OriginalTitle = source.Title;
        }
        RemoveEntryTemplateChildren(node);
        int index = 0;
        while (index < source.Templates.Length)
        {
            SynchronizeTemplate(source, source.Templates[index]);
            index = index + 1;
        }
        RemoveTextChildren(node, "index-source-styles");
        index = 0;
        while (index < source.SourceStyles.Length)
        {
            SynchronizeSourceStyle(source, source.SourceStyles[index]);
            index = index + 1;
        }
        source.Attributes = node.Attributes;
    }

    static void SynchronizeTemplate(
        OdfTextIndexSource source,
        OdfIndexEntryTemplate template)
    {
        OdfNode? node = template.Node;
        if (node == null)
        {
            node = CreateTextElement(
                EntryTemplateElementName(source.Kind),
                source.Node == null ? "" : source.Node.Location.PartPath);
            if (source.Node != null)
            {
                StyleDocumentModel.AppendNode(source.Node, node);
            }
            template.Node = node;
        }
        ApplyPreservedAttributes(node, template.Attributes);
        WriteOptional(node, OdfNamespaces.Text, "outline-level", OdfNamespaces.TextPrefix, template.OutlineLevel);
        WriteOptional(node, OdfNamespaces.Text, "bibliography-type", OdfNamespaces.TextPrefix, template.BibliographyType);
        WriteOptional(node, OdfNamespaces.Text, "style-name", OdfNamespaces.TextPrefix, template.StyleName);
        if (node.Parent == null && source.Node != null)
        {
            StyleDocumentModel.AppendNode(source.Node, node);
        }
        RemoveEntryComponentChildren(node);
        int index = 0;
        while (index < template.Components.Length)
        {
            SynchronizeComponent(template, template.Components[index]);
            index = index + 1;
        }
        template.Attributes = node.Attributes;
    }

    static void SynchronizeComponent(
        OdfIndexEntryTemplate template,
        OdfIndexEntryComponent component)
    {
        OdfNode? node = component.Node;
        if (node == null)
        {
            node = CreateTextElement(
                ComponentElementName(component.Kind),
                template.Node == null ? "" : template.Node.Location.PartPath);
            if (template.Node != null)
            {
                StyleDocumentModel.AppendNode(template.Node, node);
            }
            component.Node = node;
        }
        ApplyPreservedAttributes(node, component.Attributes);
        WriteOptional(node, OdfNamespaces.Text, "style-name", OdfNamespaces.TextPrefix, component.StyleName);
        WriteOptional(node, OdfNamespaces.Text, "display", OdfNamespaces.TextPrefix, component.Display);
        WriteOptional(node, OdfNamespaces.Text, "outline-level", OdfNamespaces.TextPrefix, component.OutlineLevel);
        WriteOptional(node, OdfNamespaces.Text, "bibliography-data-field", OdfNamespaces.TextPrefix, component.BibliographyDataField);
        WriteOptional(node, OdfNamespaces.Style, "type", OdfNamespaces.StylePrefix, component.TabType);
        WriteOptional(node, OdfNamespaces.Style, "position", OdfNamespaces.StylePrefix, component.TabPosition);
        WriteOptional(node, OdfNamespaces.Style, "leader-char", OdfNamespaces.StylePrefix, component.LeaderCharacter);
        if (component.Text != component.OriginalText)
        {
            ReplaceText(node, component.Text);
            component.OriginalText = component.Text;
        }
        if (node.Parent == null && template.Node != null)
        {
            StyleDocumentModel.AppendNode(template.Node, node);
        }
        component.Attributes = node.Attributes;
    }

    static void SynchronizeSourceStyle(
        OdfTextIndexSource source,
        OdfIndexSourceStyle group)
    {
        if (group.Node == null)
        {
            group.Node = CreateTextElement(
                "index-source-styles",
                source.Node == null ? "" : source.Node.Location.PartPath);
        }
        WriteOptional(group.Node, OdfNamespaces.Text, "outline-level", OdfNamespaces.TextPrefix, group.OutlineLevel);
        RemoveTextChildren(group.Node, "index-source-style");
        int index = 0;
        while (index < group.StyleNames.Length)
        {
            OdfNode style = CreateTextElement(
                "index-source-style",
                group.Node.Location.PartPath);
            WriteOptional(style, OdfNamespaces.Text, "style-name", OdfNamespaces.TextPrefix, group.StyleNames[index]);
            StyleDocumentModel.AppendNode(group.Node, style);
            index = index + 1;
        }
        if (group.Node.Parent == null && source.Node != null)
        {
            StyleDocumentModel.AppendNode(source.Node, group.Node);
        }
    }

    static void SynchronizeConfiguration(
        OdfDocument document,
        OdfBibliographyConfiguration configuration)
    {
        OdfNode? node = configuration.Node;
        if (node == null)
        {
            OdfNode styles = EnsureStylesSection(document, configuration.PartPath);
            node = CreateTextElement(
                "bibliography-configuration",
                styles.Location.PartPath);
            StyleDocumentModel.AppendNode(styles, node);
            configuration.Node = node;
            configuration.PartPath = node.Location.PartPath;
        }
        ApplyPreservedAttributes(node, configuration.Attributes);
        WriteOptional(node, OdfNamespaces.Text, "prefix", OdfNamespaces.TextPrefix, configuration.Prefix);
        WriteOptional(node, OdfNamespaces.Text, "suffix", OdfNamespaces.TextPrefix, configuration.Suffix);
        WriteOptional(node, OdfNamespaces.Text, "numbered-entries", OdfNamespaces.TextPrefix, configuration.NumberedEntries);
        WriteOptional(node, OdfNamespaces.Text, "sort-by-position", OdfNamespaces.TextPrefix, configuration.SortByPosition);
        WriteOptional(node, OdfNamespaces.Text, "sort-algorithm", OdfNamespaces.TextPrefix, configuration.SortAlgorithm);
        WriteOptional(node, OdfNamespaces.Fo, "language", OdfNamespaces.FoPrefix, configuration.Language);
        WriteOptional(node, OdfNamespaces.Fo, "country", OdfNamespaces.FoPrefix, configuration.Country);
        RemoveTextChildren(node, "sort-key");
        int index = 0;
        while (index < configuration.SortKeys.Length)
        {
            OdfBibliographySortKey key = configuration.SortKeys[index];
            OdfNode keyNode = CreateTextElement("sort-key", node.Location.PartPath);
            WriteOptional(keyNode, OdfNamespaces.Text, "key", OdfNamespaces.TextPrefix, key.Key);
            WriteOptional(keyNode, OdfNamespaces.Text, "sort-ascending", OdfNamespaces.TextPrefix, key.SortAscending);
            StyleDocumentModel.AppendNode(node, keyNode);
            key.Node = keyNode;
            index = index + 1;
        }
        configuration.Attributes = node.Attributes;
    }

    static void SynchronizeEntry(OdfBibliographyEntry entry)
    {
        OdfNode node = entry.Node ?? new OdfNode();
        ApplyPreservedAttributes(node, entry.Attributes);
        WriteOptional(node, OdfNamespaces.Text, "identifier", OdfNamespaces.TextPrefix, entry.Identifier);
        WriteOptional(node, OdfNamespaces.Text, "bibliography-type", OdfNamespaces.TextPrefix, entry.BibliographyType);
        WriteOptional(node, OdfNamespaces.Text, "author", OdfNamespaces.TextPrefix, entry.Author);
        WriteOptional(node, OdfNamespaces.Text, "title", OdfNamespaces.TextPrefix, entry.Title);
        WriteOptional(node, OdfNamespaces.Text, "year", OdfNamespaces.TextPrefix, entry.Year);
        WriteOptional(node, OdfNamespaces.Text, "url", OdfNamespaces.TextPrefix, entry.Url);
        if (entry.DisplayText != entry.OriginalDisplayText)
        {
            ReplaceText(node, entry.DisplayText);
            entry.OriginalDisplayText = entry.DisplayText;
        }
        entry.Attributes = node.Attributes;
    }

    static OdfNode EnsureStylesSection(OdfDocument document, string path)
    {
        string partPath = document.PackageKind == OdfPackageKind.Flat
            ? ""
            : path.Length > 0 ? path : OdfPartNames.Styles;
        OdfPart? part = StyleDocumentModel.FindPart(document, partPath);
        if (part == null)
        {
            part = StyleDocumentModel.CreateStylesPart(document);
        }
        OdfNode? styles = part.Styles;
        if (styles == null)
        {
            styles = StyleDocumentModel.CreateStyleSection(document, part, false);
        }
        return styles;
    }

    static OdfTextBlock[] ContainedBlocks(
        OdfTextBlock[] blocks,
        OdfNode ancestor)
    {
        List<OdfTextBlock> result = new List<OdfTextBlock>();
        int index = 0;
        while (index < blocks.Length)
        {
            OdfTextBlock block = blocks[index];
            if (block.Node != null && IsDescendant(block.Node, ancestor))
            {
                result.Add(block);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfTextBlock? FindContainingBlock(
        OdfTextBlock[] blocks,
        OdfNode node)
    {
        int index = 0;
        while (index < blocks.Length)
        {
            OdfTextBlock block = blocks[index];
            if (block.Node != null && IsDescendant(node, block.Node))
            {
                return block;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfTextRun? FindRun(OdfTextBlock[] blocks, OdfNode node)
    {
        int index = 0;
        while (index < blocks.Length)
        {
            OdfTextRun? result = FindNestedRun(blocks[index].Runs, node);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfTextRun? FindNestedRun(OdfTextRun[] runs, OdfNode node)
    {
        int index = 0;
        while (index < runs.Length)
        {
            OdfTextRun run = runs[index];
            if (run.Node == node)
            {
                return run;
            }
            OdfTextRun? nested = FindNestedRun(run.Runs, node);
            if (nested != null)
            {
                return nested;
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsDescendant(OdfNode node, OdfNode ancestor)
    {
        OdfNode? current = node.Parent;
        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    static void RemoveTextChildren(OdfNode parent, string localName)
    {
        int index = parent.Children.Length - 1;
        while (index >= 0)
        {
            OdfNode child = parent.Children[index];
            if (IsTextElement(child, localName))
            {
                StyleDocumentModel.RemoveNodeFromParent(child);
            }
            index = index - 1;
        }
    }

    static void RemoveEntryTemplateChildren(OdfNode parent)
    {
        int index = parent.Children.Length - 1;
        while (index >= 0)
        {
            OdfNode child = parent.Children[index];
            if (IsEntryTemplate(child))
            {
                StyleDocumentModel.RemoveNodeFromParent(child);
            }
            index = index - 1;
        }
    }

    static void RemoveEntryComponentChildren(OdfNode parent)
    {
        int index = parent.Children.Length - 1;
        while (index >= 0)
        {
            OdfNode child = parent.Children[index];
            if (ComponentKind(child) != OdfIndexEntryComponentKind.Unknown)
            {
                StyleDocumentModel.RemoveNodeFromParent(child);
            }
            index = index - 1;
        }
    }

    static void RemoveDeletedIndexes(
        OdfTextIndex[] current,
        OdfNode[] originals)
    {
        int index = 0;
        while (index < originals.Length)
        {
            OdfNode node = originals[index];
            bool retained = false;
            int currentIndex = 0;
            while (currentIndex < current.Length && !retained)
            {
                retained = current[currentIndex] != null
                    && current[currentIndex].Node == node;
                currentIndex = currentIndex + 1;
            }
            if (!retained)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static void RemoveDeletedConfigurations(
        OdfBibliographyConfiguration[] current,
        OdfNode[] originals)
    {
        int index = 0;
        while (index < originals.Length)
        {
            OdfNode node = originals[index];
            bool retained = false;
            int currentIndex = 0;
            while (currentIndex < current.Length && !retained)
            {
                retained = current[currentIndex] != null
                    && current[currentIndex].Node == node;
                currentIndex = currentIndex + 1;
            }
            if (!retained)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static void RemoveDeletedEntries(
        OdfBibliographyEntry[] current,
        OdfNode[] originals)
    {
        int index = 0;
        while (index < originals.Length)
        {
            OdfNode node = originals[index];
            bool retained = false;
            int currentIndex = 0;
            while (currentIndex < current.Length && !retained)
            {
                retained = current[currentIndex] != null
                    && current[currentIndex].Node == node;
                currentIndex = currentIndex + 1;
            }
            if (!retained)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static OdfTextIndexKind IndexKind(OdfNode node)
    {
        if (!IsTextElementNode(node))
        {
            return OdfTextIndexKind.Unknown;
        }
        string name = node.Name == null ? "" : node.Name.LocalName;
        if (name == "table-of-content") return OdfTextIndexKind.TableOfContent;
        if (name == "alphabetical-index") return OdfTextIndexKind.Alphabetical;
        if (name == "bibliography") return OdfTextIndexKind.Bibliography;
        if (name == "user-index") return OdfTextIndexKind.User;
        if (name == "table-index") return OdfTextIndexKind.Table;
        if (name == "illustration-index") return OdfTextIndexKind.Illustration;
        if (name == "object-index") return OdfTextIndexKind.Object;
        return OdfTextIndexKind.Unknown;
    }

    static string SourceElementName(OdfTextIndexKind kind)
    {
        if (kind == OdfTextIndexKind.TableOfContent) return "table-of-content-source";
        if (kind == OdfTextIndexKind.Alphabetical) return "alphabetical-index-source";
        if (kind == OdfTextIndexKind.Bibliography) return "bibliography-source";
        if (kind == OdfTextIndexKind.User) return "user-index-source";
        if (kind == OdfTextIndexKind.Table) return "table-index-source";
        if (kind == OdfTextIndexKind.Illustration) return "illustration-index-source";
        if (kind == OdfTextIndexKind.Object) return "object-index-source";
        return "";
    }

    static string EntryTemplateElementName(OdfTextIndexKind kind)
    {
        if (kind == OdfTextIndexKind.TableOfContent) return "table-of-content-entry-template";
        if (kind == OdfTextIndexKind.Alphabetical) return "alphabetical-index-entry-template";
        if (kind == OdfTextIndexKind.Bibliography) return "bibliography-entry-template";
        if (kind == OdfTextIndexKind.User) return "user-index-entry-template";
        if (kind == OdfTextIndexKind.Table) return "table-index-entry-template";
        if (kind == OdfTextIndexKind.Illustration) return "illustration-index-entry-template";
        if (kind == OdfTextIndexKind.Object) return "object-index-entry-template";
        return "index-entry-template";
    }

    static OdfIndexEntryComponentKind ComponentKind(OdfNode node)
    {
        if (!IsTextElementNode(node)) return OdfIndexEntryComponentKind.Unknown;
        string name = node.Name == null ? "" : node.Name.LocalName;
        if (name == "index-entry-chapter") return OdfIndexEntryComponentKind.Chapter;
        if (name == "index-entry-text") return OdfIndexEntryComponentKind.Text;
        if (name == "index-entry-page-number") return OdfIndexEntryComponentKind.PageNumber;
        if (name == "index-entry-span") return OdfIndexEntryComponentKind.Span;
        if (name == "index-entry-tab-stop") return OdfIndexEntryComponentKind.TabStop;
        if (name == "index-entry-bibliography") return OdfIndexEntryComponentKind.Bibliography;
        if (name == "index-entry-link-start") return OdfIndexEntryComponentKind.LinkStart;
        if (name == "index-entry-link-end") return OdfIndexEntryComponentKind.LinkEnd;
        return OdfIndexEntryComponentKind.Unknown;
    }

    static string ComponentElementName(OdfIndexEntryComponentKind kind)
    {
        if (kind == OdfIndexEntryComponentKind.Chapter) return "index-entry-chapter";
        if (kind == OdfIndexEntryComponentKind.Text) return "index-entry-text";
        if (kind == OdfIndexEntryComponentKind.PageNumber) return "index-entry-page-number";
        if (kind == OdfIndexEntryComponentKind.Span) return "index-entry-span";
        if (kind == OdfIndexEntryComponentKind.TabStop) return "index-entry-tab-stop";
        if (kind == OdfIndexEntryComponentKind.Bibliography) return "index-entry-bibliography";
        if (kind == OdfIndexEntryComponentKind.LinkStart) return "index-entry-link-start";
        if (kind == OdfIndexEntryComponentKind.LinkEnd) return "index-entry-link-end";
        return "index-entry-span";
    }

    static bool IsEntryTemplate(OdfNode node)
    {
        return IsTextElementNode(node)
            && node.Name != null
            && node.Name.LocalName.EndsWith("-entry-template", StringComparison.Ordinal);
    }

    static OdfNode? FindTextChild(OdfNode parent, string localName)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (IsTextElement(child, localName)) return child;
            index = index + 1;
        }
        return null;
    }

    static string ExtractText(OdfNode node)
    {
        StringBuilder result = new StringBuilder();
        AppendText(result, node);
        return result.ToString();
    }

    static void AppendText(StringBuilder result, OdfNode node)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
            return;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendText(result, node.Children[index]);
            index = index + 1;
        }
    }

    static void ReplaceText(OdfNode node, string value)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            node.Children[index].Parent = null;
            index = index + 1;
        }
        node.Children = [];
        if (value.Length > 0)
        {
            OdfNode text = new OdfNode();
            text.Kind = OdfNodeKind.Text;
            text.Value = value;
            text.Location.PartPath = node.Location.PartPath;
            StyleDocumentModel.AppendNode(node, text);
        }
    }

    static OdfNode CreateTextElement(string localName, string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Text,
            localName,
            OdfNamespaces.TextPrefix);
        result.Location.PartPath = partPath;
        return result;
    }

    static void ApplyPreservedAttributes(
        OdfNode node,
        OdfAttribute[] attributes)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            OdfAttribute attribute = attributes[index];
            StyleDocumentModel.WriteAttribute(
                node,
                attribute.Name.NamespaceName,
                attribute.Name.LocalName,
                attribute.Name.Prefix,
                attribute.Value);
            index = index + 1;
        }
    }

    static void WriteOptional(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(node, namespaceName, localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                namespaceName,
                localName,
                prefix,
                value);
        }
    }

    static string Read(OdfNode node, string namespaceName, string localName)
    {
        return StyleDocumentModel.ReadAttribute(node, namespaceName, localName);
    }

    static bool IsTextElement(OdfNode node, string localName)
    {
        return IsTextElementNode(node)
            && node.Name != null
            && node.Name.LocalName == localName;
    }

    static bool IsTextElementNode(OdfNode node)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Text;
    }
}
