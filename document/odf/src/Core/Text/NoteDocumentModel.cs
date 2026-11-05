using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class NoteDocumentState
{
    internal OdfNode[] OriginalNoteNodes = [];
    internal OdfNode[] OriginalConfigurationNodes = [];
}

internal static class NoteDocumentModel
{
    internal static void LoadNotes(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for note materialization.");
        }
        List<OdfNote> notes = new List<OdfNote>();
        List<OdfNoteConfiguration> configurations =
            new List<OdfNoteConfiguration>();
        List<OdfNode> noteNodes = new List<OdfNode>();
        List<OdfNode> configurationNodes = new List<OdfNode>();
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
                CollectNotes(
                    document,
                    part.Body,
                    part.Path,
                    notes,
                    noteNodes);
            }
            partIndex = partIndex + 1;
        }
        document.Notes = notes.ToArray();
        document.NoteConfigurations = configurations.ToArray();
        ResolveNoteRelationships(document);
        NoteDocumentState state = new NoteDocumentState();
        state.OriginalNoteNodes = noteNodes.ToArray();
        state.OriginalConfigurationNodes = configurationNodes.ToArray();
        document.NoteState = state;
    }

    internal static void SynchronizeNotes(OdfDocument document)
    {
        if (document == null
            || document.Notes == null
            || document.NoteConfigurations == null)
        {
            throw new OdfDocumentException(
                "Note and note-configuration collections are required for writing.");
        }
        NoteDocumentState state = document.NoteState
            ?? new NoteDocumentState();
        RemoveDeletedNotes(document.Notes, state.OriginalNoteNodes);
        RemoveDeletedConfigurations(
            document.NoteConfigurations,
            state.OriginalConfigurationNodes);
        List<OdfNode> configurationNodes = new List<OdfNode>();
        int index = 0;
        while (index < document.NoteConfigurations.Length)
        {
            OdfNoteConfiguration configuration =
                document.NoteConfigurations[index];
            if (configuration == null)
            {
                throw new OdfDocumentException(
                    "The note-configuration collection contains a null configuration.");
            }
            SynchronizeConfiguration(document, configuration);
            if (configuration.Node != null)
            {
                configurationNodes.Add(configuration.Node);
            }
            index = index + 1;
        }
        List<OdfNode> noteNodes = new List<OdfNode>();
        index = 0;
        while (index < document.Notes.Length)
        {
            OdfNote note = document.Notes[index];
            if (note != null && note.Run != null && note.Run.Node != null)
            {
                note.Node = note.Run.Node;
                note.CitationNode = FindTextChild(
                    note.Run.Node,
                    "note-citation");
                note.BodyNode = FindTextChild(note.Run.Node, "note-body");
            }
            if (note == null || note.Node == null)
            {
                throw new OdfEditException(
                    "A note requires an underlying XML element.");
            }
            ApplyNoteFields(note);
            noteNodes.Add(note.Node);
            index = index + 1;
        }
        state.OriginalNoteNodes = noteNodes.ToArray();
        state.OriginalConfigurationNodes = configurationNodes.ToArray();
        document.NoteState = state;
        ResolveNoteRelationships(document);
    }

    internal static void ResolveNoteRelationships(OdfDocument document)
    {
        int index = 0;
        while (index < document.Notes.Length)
        {
            OdfNote note = document.Notes[index];
            note.Configuration = null;
            note.HasAmbiguousConfiguration = false;
            int count = 0;
            int configurationIndex = 0;
            while (configurationIndex < document.NoteConfigurations.Length)
            {
                OdfNoteConfiguration configuration =
                    document.NoteConfigurations[configurationIndex];
                if (configuration.Kind == note.Kind)
                {
                    note.Configuration = configuration;
                    count = count + 1;
                }
                configurationIndex = configurationIndex + 1;
            }
            if (count != 1)
            {
                note.Configuration = null;
                note.HasAmbiguousConfiguration = count > 1;
            }
            AssignNoteNumber(document, note, index);
            index = index + 1;
        }
    }

    static void CollectConfigurations(
        OdfNode? styles,
        string partPath,
        List<OdfNoteConfiguration> configurations,
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
            if (IsTextElement(child, "notes-configuration"))
            {
                configurations.Add(CreateConfiguration(child, partPath));
                nodes.Add(child);
            }
            index = index + 1;
        }
    }

    static void CollectNotes(
        OdfDocument document,
        OdfNode node,
        string partPath,
        List<OdfNote> notes,
        List<OdfNode> nodes)
    {
        if (IsTextElement(node, "note"))
        {
            notes.Add(CreateNote(document, node, partPath));
            nodes.Add(node);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectNotes(
                document,
                node.Children[index],
                partPath,
                notes,
                nodes);
            index = index + 1;
        }
    }

    static OdfNoteConfiguration CreateConfiguration(
        OdfNode node,
        string partPath)
    {
        OdfNoteConfiguration result = new OdfNoteConfiguration();
        result.Kind = ParseNoteKind(Read(
            node,
            OdfNamespaces.Text,
            "note-class"));
        result.CitationStyleName = Read(
            node,
            OdfNamespaces.Text,
            "citation-style-name");
        result.CitationBodyStyleName = Read(
            node,
            OdfNamespaces.Text,
            "citation-body-style-name");
        result.DefaultStyleName = Read(
            node,
            OdfNamespaces.Text,
            "default-style-name");
        result.MasterPageName = Read(
            node,
            OdfNamespaces.Text,
            "master-page-name");
        result.StartValue = Read(
            node,
            OdfNamespaces.Text,
            "start-value");
        result.NumberFormat = Read(
            node,
            OdfNamespaces.Style,
            "num-format");
        result.NumberPrefix = Read(
            node,
            OdfNamespaces.Style,
            "num-prefix");
        result.NumberSuffix = Read(
            node,
            OdfNamespaces.Style,
            "num-suffix");
        result.NumberLetterSync = Read(
            node,
            OdfNamespaces.Style,
            "num-letter-sync");
        result.StartNumberingAt = Read(
            node,
            OdfNamespaces.Text,
            "start-numbering-at");
        result.FootnotesPosition = Read(
            node,
            OdfNamespaces.Text,
            "footnotes-position");
        result.ForwardNoticeNode = FindTextChild(
            node,
            "note-continuation-notice-forward");
        result.BackwardNoticeNode = FindTextChild(
            node,
            "note-continuation-notice-backward");
        result.ForwardNotice = result.ForwardNoticeNode == null
            ? ""
            : ExtractText(result.ForwardNoticeNode);
        result.BackwardNotice = result.BackwardNoticeNode == null
            ? ""
            : ExtractText(result.BackwardNoticeNode);
        result.OriginalForwardNotice = result.ForwardNotice;
        result.OriginalBackwardNotice = result.BackwardNotice;
        result.Attributes = node.Attributes;
        result.PartPath = partPath;
        result.Node = node;
        return result;
    }

    static OdfNote CreateNote(
        OdfDocument document,
        OdfNode node,
        string partPath)
    {
        OdfNote result = new OdfNote();
        result.Kind = ParseNoteKind(Read(
            node,
            OdfNamespaces.Text,
            "note-class"));
        result.Id = Read(node, OdfNamespaces.Text, "id");
        result.CitationNode = FindTextChild(node, "note-citation");
        result.BodyNode = FindTextChild(node, "note-body");
        if (result.CitationNode != null)
        {
            result.CitationLabel = Read(
                result.CitationNode,
                OdfNamespaces.Text,
                "label");
            result.CitationText = ExtractText(result.CitationNode);
        }
        if (result.BodyNode != null)
        {
            result.TextBlocks = ContainedBlocks(
                document.TextBlocks,
                result.BodyNode);
        }
        result.CitationBlock = FindCitationBlock(
            document.TextBlocks,
            node);
        result.Run = FindNoteRun(document.TextBlocks, node);
        result.OriginalCitationText = result.CitationText;
        result.PartPath = partPath;
        result.Node = node;
        return result;
    }

    static OdfTextRun? FindNoteRun(
        OdfTextBlock[] blocks,
        OdfNode node)
    {
        int index = 0;
        while (index < blocks.Length)
        {
            OdfTextRun? result = FindNoteRunInTree(
                blocks[index].Runs,
                node);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfTextRun? FindNoteRunInTree(
        OdfTextRun[] runs,
        OdfNode node)
    {
        int index = 0;
        while (index < runs.Length)
        {
            OdfTextRun run = runs[index];
            if (run.Node == node)
            {
                return run;
            }
            OdfTextRun? nested = FindNoteRunInTree(run.Runs, node);
            if (nested != null)
            {
                return nested;
            }
            index = index + 1;
        }
        return null;
    }

    static void AssignNoteNumber(
        OdfDocument document,
        OdfNote note,
        int noteIndex)
    {
        int start = ParseStartingInteger(
            note.Configuration == null
                ? ""
                : note.Configuration.StartValue,
            1);
        int previousIndex = noteIndex - 1;
        OdfNote? previous = null;
        while (previousIndex >= 0 && previous == null)
        {
            OdfNote candidate = document.Notes[previousIndex];
            if (candidate.Kind == note.Kind
                && SameNumberingScope(document, candidate, note))
            {
                previous = candidate;
            }
            previousIndex = previousIndex - 1;
        }
        note.Ordinal = previous == null ? start : previous.Ordinal + 1;
        if (note.Configuration == null)
        {
            note.ComputedCitation = note.Ordinal.ToString();
        }
        else
        {
            note.ComputedCitation = note.Configuration.NumberPrefix
                + FormatNumber(
                    note.Ordinal,
                    note.Configuration.NumberFormat,
                    note.Configuration.NumberLetterSync)
                + note.Configuration.NumberSuffix;
        }
    }

    static bool SameNumberingScope(
        OdfDocument document,
        OdfNote first,
        OdfNote second)
    {
        string scope = second.Configuration == null
            ? "document"
            : second.Configuration.StartNumberingAt;
        if (scope != "chapter")
        {
            if (scope == "page")
            {
                return PageNumber(document, first)
                    == PageNumber(document, second);
            }
            return true;
        }
        return ChapterNumber(document, first)
            == ChapterNumber(document, second);
    }

    static int ChapterNumber(OdfDocument document, OdfNote note)
    {
        int chapter = 0;
        int index = 0;
        while (index < document.TextBlocks.Length)
        {
            OdfTextBlock block = document.TextBlocks[index];
            if (block.IsHeading && block.OutlineLevel == 1)
            {
                chapter = chapter + 1;
            }
            if (block == note.CitationBlock)
            {
                return chapter;
            }
            index = index + 1;
        }
        return chapter;
    }

    static int PageNumber(OdfDocument document, OdfNote note)
    {
        int page = 0;
        List<OdfNode> countedSoftBreaks = new List<OdfNode>();
        int index = 0;
        while (index < document.TextBreaks.Length)
        {
            OdfTextBreak item = document.TextBreaks[index];
            if (IsPageBreak(item.Kind)
                && BreakPrecedesNote(document, item, note))
            {
                if (item.Kind != OdfTextBreakKind.SoftPage
                    || item.Node == null
                    || !countedSoftBreaks.Contains(item.Node))
                {
                    page = page + 1;
                    if (item.Kind == OdfTextBreakKind.SoftPage
                        && item.Node != null)
                    {
                        countedSoftBreaks.Add(item.Node);
                    }
                }
            }
            index = index + 1;
        }
        return page;
    }

    static bool IsPageBreak(OdfTextBreakKind kind)
    {
        return kind == OdfTextBreakKind.SoftPage
            || kind == OdfTextBreakKind.PageBefore
            || kind == OdfTextBreakKind.PageAfter
            || kind == OdfTextBreakKind.EvenPageBefore
            || kind == OdfTextBreakKind.EvenPageAfter
            || kind == OdfTextBreakKind.OddPageBefore
            || kind == OdfTextBreakKind.OddPageAfter;
    }

    static bool BreakPrecedesNote(
        OdfDocument document,
        OdfTextBreak item,
        OdfNote note)
    {
        int breakBlock = BlockPosition(document.TextBlocks, item.Block);
        int noteBlock = BlockPosition(
            document.TextBlocks,
            note.CitationBlock);
        if (breakBlock < 0 || noteBlock < 0)
        {
            return false;
        }
        if (breakBlock < noteBlock)
        {
            return true;
        }
        if (breakBlock > noteBlock)
        {
            return false;
        }
        if (item.Kind == OdfTextBreakKind.PageBefore
            || item.Kind == OdfTextBreakKind.EvenPageBefore
            || item.Kind == OdfTextBreakKind.OddPageBefore)
        {
            return true;
        }
        if (item.Kind != OdfTextBreakKind.SoftPage
            || item.Node == null
            || note.Node == null
            || item.Block == null
            || item.Block.Node == null)
        {
            return false;
        }
        OdfNode[] nodes = new OdfTreeEditor([item.Block.Node])
            .TraverseAllNodes();
        int breakNode = NodePosition(nodes, item.Node);
        int noteNode = NodePosition(nodes, note.Node);
        return breakNode >= 0 && noteNode >= 0 && breakNode < noteNode;
    }

    static int BlockPosition(
        OdfTextBlock[] blocks,
        OdfTextBlock? target)
    {
        int index = 0;
        while (index < blocks.Length)
        {
            if (blocks[index] == target)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static int NodePosition(OdfNode[] nodes, OdfNode target)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index] == target)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static void SynchronizeConfiguration(
        OdfDocument document,
        OdfNoteConfiguration configuration)
    {
        OdfNode? node = configuration.Node;
        if (node == null)
        {
            OdfNode section = EnsureStylesSection(document, configuration);
            node = CreateTextElement(
                "notes-configuration",
                section.Location.PartPath);
            StyleDocumentModel.AppendNode(section, node);
            configuration.Node = node;
            configuration.PartPath = node.Location.PartPath;
        }
        ApplyPreservedAttributes(node, configuration.Attributes);
        WriteOptional(node, OdfNamespaces.Text, "note-class", OdfNamespaces.TextPrefix, NoteKindText(configuration.Kind));
        WriteOptional(node, OdfNamespaces.Text, "citation-style-name", OdfNamespaces.TextPrefix, configuration.CitationStyleName);
        WriteOptional(node, OdfNamespaces.Text, "citation-body-style-name", OdfNamespaces.TextPrefix, configuration.CitationBodyStyleName);
        WriteOptional(node, OdfNamespaces.Text, "default-style-name", OdfNamespaces.TextPrefix, configuration.DefaultStyleName);
        WriteOptional(node, OdfNamespaces.Text, "master-page-name", OdfNamespaces.TextPrefix, configuration.MasterPageName);
        WriteOptional(node, OdfNamespaces.Text, "start-value", OdfNamespaces.TextPrefix, configuration.StartValue);
        WriteOptional(node, OdfNamespaces.Style, "num-format", OdfNamespaces.StylePrefix, configuration.NumberFormat);
        WriteOptional(node, OdfNamespaces.Style, "num-prefix", OdfNamespaces.StylePrefix, configuration.NumberPrefix);
        WriteOptional(node, OdfNamespaces.Style, "num-suffix", OdfNamespaces.StylePrefix, configuration.NumberSuffix);
        WriteOptional(node, OdfNamespaces.Style, "num-letter-sync", OdfNamespaces.StylePrefix, configuration.NumberLetterSync);
        WriteOptional(node, OdfNamespaces.Text, "start-numbering-at", OdfNamespaces.TextPrefix, configuration.StartNumberingAt);
        WriteOptional(node, OdfNamespaces.Text, "footnotes-position", OdfNamespaces.TextPrefix, configuration.FootnotesPosition);
        configuration.ForwardNoticeNode = SynchronizeNotice(
            node,
            configuration.ForwardNoticeNode,
            "note-continuation-notice-forward",
            configuration.ForwardNotice,
            configuration.OriginalForwardNotice);
        configuration.BackwardNoticeNode = SynchronizeNotice(
            node,
            configuration.BackwardNoticeNode,
            "note-continuation-notice-backward",
            configuration.BackwardNotice,
            configuration.OriginalBackwardNotice);
        configuration.OriginalForwardNotice = configuration.ForwardNotice;
        configuration.OriginalBackwardNotice = configuration.BackwardNotice;
        configuration.Attributes = node.Attributes;
    }

    static OdfNode? SynchronizeNotice(
        OdfNode configuration,
        OdfNode? notice,
        string localName,
        string value,
        string originalValue)
    {
        if (notice == null && value.Length > 0)
        {
            notice = CreateTextElement(
                localName,
                configuration.Location.PartPath);
            StyleDocumentModel.AppendNode(configuration, notice);
        }
        if (notice != null && value != originalValue)
        {
            ReplaceText(notice, value);
        }
        return notice;
    }

    static void ApplyNoteFields(OdfNote note)
    {
        OdfNode node = note.Node ?? new OdfNode();
        WriteOptional(node, OdfNamespaces.Text, "id", OdfNamespaces.TextPrefix, note.Id);
        WriteOptional(node, OdfNamespaces.Text, "note-class", OdfNamespaces.TextPrefix, NoteKindText(note.Kind));
        OdfNode? citation = note.CitationNode;
        if (citation == null)
        {
            citation = CreateTextElement(
                "note-citation",
                node.Location.PartPath);
            PrependChild(node, citation);
            note.CitationNode = citation;
        }
        WriteOptional(citation, OdfNamespaces.Text, "label", OdfNamespaces.TextPrefix, note.CitationLabel);
        if (note.CitationText != note.OriginalCitationText)
        {
            ReplaceText(citation, note.CitationText);
            note.OriginalCitationText = note.CitationText;
        }
    }

    static OdfNode EnsureStylesSection(
        OdfDocument document,
        OdfNoteConfiguration configuration)
    {
        string partPath = document.PackageKind == OdfPackageKind.Flat
            ? ""
            : configuration.PartPath.Length > 0
                ? configuration.PartPath
                : OdfPartNames.Styles;
        OdfPart? part = StyleDocumentModel.FindPart(document, partPath);
        if (part == null)
        {
            part = StyleDocumentModel.CreateStylesPart(document);
        }
        OdfNode? styles = part.Styles;
        if (styles == null)
        {
            styles = StyleDocumentModel.CreateStyleSection(
                document,
                part,
                false);
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

    static OdfTextBlock? FindCitationBlock(
        OdfTextBlock[] blocks,
        OdfNode note)
    {
        int index = 0;
        while (index < blocks.Length)
        {
            OdfTextBlock block = blocks[index];
            if (block.Node != null
                && IsDescendant(note, block.Node)
                && !IsDescendant(block.Node, note))
            {
                return block;
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

    static OdfNode? FindTextChild(OdfNode parent, string localName)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (IsTextElement(child, localName))
            {
                return child;
            }
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
        if (value.Length == 0)
        {
            node.Children = [];
            return;
        }
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        text.Parent = node;
        text.Location.PartPath = node.Location.PartPath;
        node.Children = [text];
        node.WasEmptyElement = false;
    }

    static string FormatNumber(
        int value,
        string format,
        string letterSync)
    {
        if (format == "a")
        {
            return FormatAlphabetic(value, false, letterSync == "true");
        }
        if (format == "A")
        {
            return FormatAlphabetic(value, true, letterSync == "true");
        }
        if (format == "i")
        {
            return FormatRoman(value, false);
        }
        if (format == "I")
        {
            return FormatRoman(value, true);
        }
        return value.ToString();
    }

    static string FormatAlphabetic(
        int value,
        bool upper,
        bool synchronized)
    {
        if (value <= 0)
        {
            return value.ToString();
        }
        if (!synchronized)
        {
            int length = (value - 1) / 26 + 1;
            char first = upper ? 'A' : 'a';
            char letter = (char)(first + (value - 1) % 26);
            return new string(letter, length);
        }
        StringBuilder result = new StringBuilder();
        int current = value;
        while (current > 0)
        {
            current = current - 1;
            char first = upper ? 'A' : 'a';
            result.Insert(0, (char)(first + current % 26));
            current = current / 26;
        }
        return result.ToString();
    }

    static string FormatRoman(int value, bool upper)
    {
        if (value <= 0 || value > 3999)
        {
            return value.ToString();
        }
        int[] values =
        [
            1000, 900, 500, 400, 100, 90, 50,
            40, 10, 9, 5, 4, 1
        ];
        string[] symbols =
        [
            "M", "CM", "D", "CD", "C", "XC", "L",
            "XL", "X", "IX", "V", "IV", "I"
        ];
        StringBuilder result = new StringBuilder();
        int current = value;
        int index = 0;
        while (index < values.Length)
        {
            while (current >= values[index])
            {
                result.Append(symbols[index]);
                current = current - values[index];
            }
            index = index + 1;
        }
        string text = result.ToString();
        return upper ? text : text.ToLowerInvariant();
    }

    static int ParseStartingInteger(string value, int defaultValue)
    {
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(value);
        if (!parsed.IsValid
            || parsed.IntegerValue < 0
            || parsed.IntegerValue > int.MaxValue)
        {
            return defaultValue;
        }
        return (int)parsed.IntegerValue;
    }

    static OdfNoteKind ParseNoteKind(string value)
    {
        if (value == "footnote")
        {
            return OdfNoteKind.Footnote;
        }
        if (value == "endnote")
        {
            return OdfNoteKind.Endnote;
        }
        return OdfNoteKind.Unknown;
    }

    static string NoteKindText(OdfNoteKind kind)
    {
        if (kind == OdfNoteKind.Footnote)
        {
            return "footnote";
        }
        if (kind == OdfNoteKind.Endnote)
        {
            return "endnote";
        }
        return "";
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

    static void PrependChild(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        children[0] = child;
        Array.Copy(parent.Children, 0, children, 1, parent.Children.Length);
        parent.Children = children;
        child.Parent = parent;
        child.Location.PartPath = parent.Location.PartPath;
        parent.WasEmptyElement = false;
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

    static bool IsTextElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Text
            && node.Name.LocalName == localName;
    }

    static string Read(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            namespaceName,
            localName);
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
            StyleDocumentModel.RemoveAttribute(
                node,
                namespaceName,
                localName);
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

    static void RemoveDeletedNotes(
        OdfNote[] notes,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            bool retained = false;
            int noteIndex = 0;
            while (noteIndex < notes.Length && !retained)
            {
                retained = notes[noteIndex] != null
                    && notes[noteIndex].Node == node;
                noteIndex = noteIndex + 1;
            }
            if (!retained)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static void RemoveDeletedConfigurations(
        OdfNoteConfiguration[] configurations,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            bool retained = false;
            int configurationIndex = 0;
            while (configurationIndex < configurations.Length && !retained)
            {
                retained = configurations[configurationIndex] != null
                    && configurations[configurationIndex].Node == node;
                configurationIndex = configurationIndex + 1;
            }
            if (!retained)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }
}
