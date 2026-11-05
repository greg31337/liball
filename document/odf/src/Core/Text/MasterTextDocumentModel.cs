using System;
using System.Collections.Generic;

namespace odf;

internal class MasterTextDocumentState
{
    internal OdfNode[] OriginalSequenceNodes = [];
}

internal static class MasterTextDocumentModel
{
    internal static void LoadMasterText(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for master-text materialization.");
        }
        LoadMasterPageContents(document);
        List<OdfPageSequence> sequences = new List<OdfPageSequence>();
        List<OdfNode> sequenceNodes = new List<OdfNode>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            if (part.Body != null)
            {
                CollectPageSequences(
                    part.Body,
                    part.Path,
                    sequences,
                    sequenceNodes);
            }
            partIndex = partIndex + 1;
        }
        document.PageSequences = sequences.ToArray();
        ResolvePageSequences(document);
        document.TitlePagePatterns = CreateTitlePagePatterns(document);
        document.MasterSubdocuments = CreateMasterSubdocuments(document);
        MasterTextDocumentState state = new MasterTextDocumentState();
        state.OriginalSequenceNodes = sequenceNodes.ToArray();
        document.MasterTextState = state;
    }

    internal static void SynchronizeMasterText(OdfDocument document)
    {
        if (document == null
            || document.PageSequences == null
            || document.TitlePagePatterns == null
            || document.MasterSubdocuments == null)
        {
            throw new OdfDocumentException(
                "Master-text collections are required for writing.");
        }
        SynchronizeSubdocuments(document.MasterSubdocuments);
        SynchronizeTitlePatterns(document.TitlePagePatterns);
        MasterTextDocumentState state = document.MasterTextState
            ?? new MasterTextDocumentState();
        RemoveDeletedSequences(
            document.PageSequences,
            state.OriginalSequenceNodes);
        List<OdfNode> nodes = new List<OdfNode>();
        int index = 0;
        while (index < document.PageSequences.Length)
        {
            OdfPageSequence sequence = document.PageSequences[index];
            if (sequence == null || sequence.Node == null)
            {
                throw new OdfEditException(
                    "A page sequence requires an underlying XML element.");
            }
            SynchronizeSequence(sequence);
            nodes.Add(sequence.Node);
            index = index + 1;
        }
        state.OriginalSequenceNodes = nodes.ToArray();
        document.MasterTextState = state;
        ResolvePageSequences(document);
        ResolveTitlePatterns(document);
    }

    static void LoadMasterPageContents(OdfDocument document)
    {
        int index = 0;
        while (index < document.MasterPages.Length)
        {
            OdfMasterPage master = document.MasterPages[index];
            List<OdfMasterPageContent> contents =
                new List<OdfMasterPageContent>();
            AddMasterContent(
                contents,
                document.TextBlocks,
                master,
                master.Header,
                OdfMasterPageContentKind.Header);
            AddMasterContent(
                contents,
                document.TextBlocks,
                master,
                master.HeaderLeft,
                OdfMasterPageContentKind.HeaderLeft);
            AddMasterContent(
                contents,
                document.TextBlocks,
                master,
                master.HeaderFirst,
                OdfMasterPageContentKind.HeaderFirst);
            AddMasterContent(
                contents,
                document.TextBlocks,
                master,
                master.Footer,
                OdfMasterPageContentKind.Footer);
            AddMasterContent(
                contents,
                document.TextBlocks,
                master,
                master.FooterLeft,
                OdfMasterPageContentKind.FooterLeft);
            AddMasterContent(
                contents,
                document.TextBlocks,
                master,
                master.FooterFirst,
                OdfMasterPageContentKind.FooterFirst);
            master.TextContents = contents.ToArray();
            index = index + 1;
        }
    }

    static void AddMasterContent(
        List<OdfMasterPageContent> contents,
        OdfTextBlock[] blocks,
        OdfMasterPage master,
        OdfNode? node,
        OdfMasterPageContentKind kind)
    {
        if (node == null)
        {
            return;
        }
        OdfMasterPageContent content = new OdfMasterPageContent();
        content.Kind = kind;
        content.MasterPage = master;
        content.TextBlocks = ContainedBlocks(blocks, node);
        content.Node = node;
        contents.Add(content);
    }

    static void CollectPageSequences(
        OdfNode node,
        string partPath,
        List<OdfPageSequence> sequences,
        List<OdfNode> nodes)
    {
        if (IsTextElement(node, "page-sequence"))
        {
            OdfPageSequence sequence = new OdfPageSequence();
            sequence.PartPath = partPath;
            sequence.Node = node;
            List<OdfPageSequenceEntry> pages =
                new List<OdfPageSequenceEntry>();
            int pageIndex = 0;
            while (pageIndex < node.Children.Length)
            {
                OdfNode pageNode = node.Children[pageIndex];
                if (IsTextElement(pageNode, "page"))
                {
                    OdfPageSequenceEntry page =
                        new OdfPageSequenceEntry();
                    page.MasterPageName = Read(
                        pageNode,
                        OdfNamespaces.Text,
                        "master-page-name");
                    page.Node = pageNode;
                    pages.Add(page);
                }
                pageIndex = pageIndex + 1;
            }
            sequence.Pages = pages.ToArray();
            sequences.Add(sequence);
            nodes.Add(node);
            return;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectPageSequences(
                node.Children[index],
                partPath,
                sequences,
                nodes);
            index = index + 1;
        }
    }

    static void ResolvePageSequences(OdfDocument document)
    {
        int sequenceIndex = 0;
        while (sequenceIndex < document.PageSequences.Length)
        {
            OdfPageSequence sequence = document.PageSequences[sequenceIndex];
            int pageIndex = 0;
            while (pageIndex < sequence.Pages.Length)
            {
                OdfPageSequenceEntry page = sequence.Pages[pageIndex];
                page.MasterPage = FindUniqueMaster(
                    document.MasterPages,
                    page.MasterPageName);
                pageIndex = pageIndex + 1;
            }
            sequenceIndex = sequenceIndex + 1;
        }
    }

    static OdfTitlePagePattern[] CreateTitlePagePatterns(
        OdfDocument document)
    {
        List<OdfTitlePagePattern> result =
            new List<OdfTitlePagePattern>();
        int sequenceIndex = 0;
        while (sequenceIndex < document.PageSequences.Length)
        {
            OdfPageSequence sequence = document.PageSequences[sequenceIndex];
            if (sequence.Pages.Length >= 2
                && sequence.Pages[0].MasterPageName
                    != sequence.Pages[1].MasterPageName)
            {
                OdfTitlePagePattern pattern = new OdfTitlePagePattern();
                pattern.TitleMasterPageName =
                    sequence.Pages[0].MasterPageName;
                pattern.FollowingMasterPageName =
                    sequence.Pages[1].MasterPageName;
                pattern.PageSequence = sequence;
                result.Add(pattern);
            }
            sequenceIndex = sequenceIndex + 1;
        }
        int masterIndex = 0;
        while (masterIndex < document.MasterPages.Length)
        {
            OdfMasterPage master = document.MasterPages[masterIndex];
            if ((master.HeaderFirst != null || master.FooterFirst != null)
                && !HasPattern(result, master.Name))
            {
                OdfTitlePagePattern pattern = new OdfTitlePagePattern();
                pattern.TitleMasterPageName = master.Name;
                pattern.FollowingMasterPageName =
                    master.NextStyleName.Length > 0
                        ? master.NextStyleName
                        : master.Name;
                result.Add(pattern);
            }
            masterIndex = masterIndex + 1;
        }
        OdfTitlePagePattern[] patterns = result.ToArray();
        ResolveTitlePatternArray(document, patterns);
        return patterns;
    }

    static bool HasPattern(
        List<OdfTitlePagePattern> patterns,
        string masterName)
    {
        int index = 0;
        while (index < patterns.Count)
        {
            if (patterns[index].TitleMasterPageName == masterName)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void ResolveTitlePatterns(OdfDocument document)
    {
        ResolveTitlePatternArray(document, document.TitlePagePatterns);
    }

    static void ResolveTitlePatternArray(
        OdfDocument document,
        OdfTitlePagePattern[] patterns)
    {
        int index = 0;
        while (index < patterns.Length)
        {
            OdfTitlePagePattern pattern = patterns[index];
            pattern.TitleMasterPage = FindUniqueMaster(
                document.MasterPages,
                pattern.TitleMasterPageName);
            pattern.FollowingMasterPage = FindUniqueMaster(
                document.MasterPages,
                pattern.FollowingMasterPageName);
            pattern.UsesFirstHeader = pattern.TitleMasterPage != null
                && pattern.TitleMasterPage.HeaderFirst != null;
            pattern.UsesFirstFooter = pattern.TitleMasterPage != null
                && pattern.TitleMasterPage.FooterFirst != null;
            index = index + 1;
        }
    }

    static OdfMasterSubdocument[] CreateMasterSubdocuments(
        OdfDocument document)
    {
        if (document.Kind != OdfDocumentKind.TextMaster
            && document.Kind != OdfDocumentKind.TextMasterTemplate)
        {
            return [];
        }
        List<OdfMasterSubdocument> result =
            new List<OdfMasterSubdocument>();
        int index = 0;
        while (index < document.TextSections.Length)
        {
            OdfTextSection section = document.TextSections[index];
            if (section.SourceNode != null && section.SourceHref.Length > 0)
            {
                OdfMasterSubdocument item = new OdfMasterSubdocument();
                item.Name = section.Name;
                item.Href = section.SourceHref;
                item.SectionName = section.SourceSectionName;
                item.FilterName = section.SourceFilterName;
                item.Section = section;
                item.Node = section.Node;
                item.SourceNode = section.SourceNode;
                item.PartPath = section.PartPath;
                result.Add(item);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void SynchronizeSubdocuments(OdfMasterSubdocument[] items)
    {
        int index = 0;
        while (index < items.Length)
        {
            OdfMasterSubdocument item = items[index];
            if (item == null || item.Section == null)
            {
                throw new OdfEditException(
                    "A master subdocument requires a linked text section.");
            }
            item.Section.Name = item.Name;
            item.Section.SourceHref = item.Href;
            item.Section.SourceSectionName = item.SectionName;
            item.Section.SourceFilterName = item.FilterName;
            index = index + 1;
        }
    }

    static void SynchronizeTitlePatterns(OdfTitlePagePattern[] patterns)
    {
        int index = 0;
        while (index < patterns.Length)
        {
            OdfTitlePagePattern pattern = patterns[index];
            if (pattern.PageSequence != null
                && pattern.PageSequence.Pages.Length >= 2)
            {
                pattern.PageSequence.Pages[0].MasterPageName =
                    pattern.TitleMasterPageName;
                pattern.PageSequence.Pages[1].MasterPageName =
                    pattern.FollowingMasterPageName;
            }
            else if (pattern.TitleMasterPage != null)
            {
                pattern.TitleMasterPage.NextStyleName =
                    pattern.FollowingMasterPageName;
            }
            index = index + 1;
        }
    }

    static void SynchronizeSequence(OdfPageSequence sequence)
    {
        OdfNode node = sequence.Node ?? new OdfNode();
        RemovePageChildren(node);
        int index = 0;
        while (index < sequence.Pages.Length)
        {
            OdfPageSequenceEntry page = sequence.Pages[index];
            OdfNode? pageNode = page.Node;
            if (pageNode == null)
            {
                pageNode = CreateTextElement("page", node.Location.PartPath);
                page.Node = pageNode;
            }
            WriteOptional(
                pageNode,
                OdfNamespaces.Text,
                "master-page-name",
                OdfNamespaces.TextPrefix,
                page.MasterPageName);
            StyleDocumentModel.AppendNode(node, pageNode);
            index = index + 1;
        }
    }

    static void RemovePageChildren(OdfNode node)
    {
        int index = node.Children.Length - 1;
        while (index >= 0)
        {
            OdfNode child = node.Children[index];
            if (IsTextElement(child, "page"))
            {
                StyleDocumentModel.RemoveNodeFromParent(child);
            }
            index = index - 1;
        }
    }

    static void RemoveDeletedSequences(
        OdfPageSequence[] sequences,
        OdfNode[] originals)
    {
        int index = 0;
        while (index < originals.Length)
        {
            OdfNode node = originals[index];
            bool retained = false;
            int sequenceIndex = 0;
            while (sequenceIndex < sequences.Length && !retained)
            {
                retained = sequences[sequenceIndex] != null
                    && sequences[sequenceIndex].Node == node;
                sequenceIndex = sequenceIndex + 1;
            }
            if (!retained)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static OdfMasterPage? FindUniqueMaster(
        OdfMasterPage[] masters,
        string name)
    {
        OdfMasterPage? result = null;
        int count = 0;
        int index = 0;
        while (index < masters.Length)
        {
            if (masters[index].Name == name)
            {
                result = masters[index];
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
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

    static string Read(OdfNode node, string namespaceName, string localName)
    {
        return StyleDocumentModel.ReadAttribute(node, namespaceName, localName);
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

    static bool IsTextElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Text
            && node.Name.LocalName == localName;
    }
}
