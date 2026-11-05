using System;
using System.Collections.Generic;

namespace odf;

internal static class PresentationSlideEditingModel
{
    internal static OdfSlide CreateSlide(
        OdfDocument document,
        string name)
    {
        RequireDocument(document);
        return InsertSlide(document, document.Slides.Length, name);
    }

    internal static OdfSlide InsertSlide(
        OdfDocument document,
        int insertionIndex,
        string name)
    {
        RequireDocument(document);
        if (insertionIndex < 0 || insertionIndex > document.Slides.Length)
        {
            throw new OdfEditException(
                "The slide insertion index is outside the presentation.");
        }
        SynchronizeForStructureEdit(document);
        OdfNode presentation = RequirePresentation(document);
        OdfNode slide = CreateSlideNode(document, presentation, name);
        int childIndex = SlideChildInsertionIndex(
            presentation,
            insertionIndex);
        InsertChild(presentation, childIndex, slide);
        RefreshPresentation(document);
        return RequireSlideByNode(document, slide);
    }

    internal static void DeleteSlide(
        OdfDocument document,
        OdfSlide slide)
    {
        RequireOwnedSlide(document, slide);
        SynchronizeForStructureEdit(document);
        OdfNode node = slide.Node ?? new OdfNode();
        OdfNode? parent = node.Parent;
        if (parent == null)
        {
            throw new OdfEditException(
                "A detached presentation slide cannot be deleted.");
        }
        List<string> identifiers = new List<string>();
        CollectIdentifiers(node, identifiers);
        RemoveDeletedReferences(document, node, identifiers);
        RemoveChild(parent, node);
        RefreshPresentation(document);
    }

    internal static OdfSlide ReorderSlide(
        OdfDocument document,
        OdfSlide slide,
        int destinationIndex)
    {
        RequireOwnedSlide(document, slide);
        if (destinationIndex < 0
            || destinationIndex >= document.Slides.Length)
        {
            throw new OdfEditException(
                "The slide destination index is outside the presentation.");
        }
        SynchronizeForStructureEdit(document);
        OdfNode node = slide.Node ?? new OdfNode();
        OdfNode? parent = node.Parent;
        if (parent == null)
        {
            throw new OdfEditException(
                "A detached presentation slide cannot be reordered.");
        }
        RemoveChild(parent, node);
        int childIndex = SlideChildInsertionIndex(
            parent,
            destinationIndex);
        InsertChild(parent, childIndex, node);
        RefreshPresentation(document);
        return RequireSlideByNode(document, node);
    }

    internal static OdfSlide DuplicateSlide(
        OdfDocument document,
        OdfSlide slide)
    {
        RequireOwnedSlide(document, slide);
        return CopySlide(document, slide, slide.Index + 1);
    }

    internal static OdfSlide CopySlide(
        OdfDocument document,
        OdfSlide slide,
        int insertionIndex)
    {
        RequireOwnedSlide(document, slide);
        if (insertionIndex < 0 || insertionIndex > document.Slides.Length)
        {
            throw new OdfEditException(
                "The copied slide insertion index is outside the presentation.");
        }
        SynchronizeForStructureEdit(document);
        OdfNode source = slide.Node ?? new OdfNode();
        OdfNode clone = new OdfTreeEditor([]).CloneSubtree(source);
        DrawingEditingModel.PrepareCopiedIdentifiers(document, clone);
        SetPartPath(clone, source.Location.PartPath);
        OdfNode presentation = RequirePresentation(document);
        int childIndex = SlideChildInsertionIndex(
            presentation,
            insertionIndex);
        InsertChild(presentation, childIndex, clone);
        RefreshPresentation(document);
        return RequireSlideByNode(document, clone);
    }

    internal static void SynchronizeForStructureEdit(OdfDocument document)
    {
        PresentationDocumentModel.SynchronizePresentation(document);
        TextDocumentModel.SynchronizeTextBlocks(document);
        DrawingDocumentModel.SynchronizeDrawings(
            document,
            document.SaveOptions);
    }

    internal static void RefreshPresentation(OdfDocument document)
    {
        TextDocumentModel.LoadTextBlocks(document);
        TextFlowDocumentModel.LoadTextFlows(document);
        DrawingDocumentModel.LoadDrawings(document);
        ImageResourceManager.LoadImages(document);
        MediaResourceManager.LoadMediaResources(document);
        EmbeddedContentManager.LoadEmbeddedContent(document);
        DrawingResourceManager.RefreshOrphans(document);
        PresentationDocumentModel.LoadPresentation(document);
        MasterTextDocumentModel.LoadMasterText(document);
        TextListDocumentModel.LoadTextLists(document);
        TextFieldDocumentModel.LoadTextFields(document);
        NoteDocumentModel.LoadNotes(document);
        TextRangeModel.LoadTextRanges(document);
        AnnotationDocumentModel.LoadAnnotations(document);
        TextIndexDocumentModel.LoadTextIndexes(document);
        TableDocumentModel.LoadTables(document);
    }

    static OdfNode CreateSlideNode(
        OdfDocument document,
        OdfNode presentation,
        string name)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Drawing,
            "page",
            OdfNamespaces.DrawingPrefix);
        result.Location.PartPath = presentation.Location.PartPath;
        string slideName = UniqueSlideName(document, name);
        StyleDocumentModel.WriteAttribute(
            result,
            OdfNamespaces.Drawing,
            "name",
            OdfNamespaces.DrawingPrefix,
            slideName);
        StyleDocumentModel.WriteAttribute(
            result,
            OdfNamespaces.Drawing,
            "id",
            OdfNamespaces.DrawingPrefix,
            UniqueSlideIdentifier(document));
        return result;
    }

    static string UniqueSlideName(
        OdfDocument document,
        string requested)
    {
        string basis = requested == null || requested.Length == 0
            ? "Slide " + (document.Slides.Length + 1).ToString(
                global::System.Globalization.CultureInfo.InvariantCulture)
            : requested;
        if (!SlideNameExists(document, basis))
        {
            return basis;
        }
        int number = 2;
        string candidate = basis + " Copy 2";
        while (SlideNameExists(document, candidate))
        {
            number = number + 1;
            candidate = basis + " Copy " + number.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture);
        }
        return candidate;
    }

    static string UniqueSlideIdentifier(OdfDocument document)
    {
        int number = document.Slides.Length + 1;
        string candidate = "slide-" + number.ToString(
            global::System.Globalization.CultureInfo.InvariantCulture);
        while (IdentifierExists(document, candidate))
        {
            number = number + 1;
            candidate = "slide-" + number.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture);
        }
        return candidate;
    }

    static bool SlideNameExists(OdfDocument document, string name)
    {
        int index = 0;
        while (index < document.Slides.Length)
        {
            if (document.Slides[index].Name == name)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IdentifierExists(OdfDocument document, string identifier)
    {
        int index = 0;
        while (index < document.DrawingObjects.Length)
        {
            OdfDrawingObject drawing = document.DrawingObjects[index];
            if (drawing.Id == identifier || drawing.XmlId == identifier)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static int SlideChildInsertionIndex(
        OdfNode presentation,
        int slideIndex)
    {
        int pageIndex = 0;
        int lastPageEnd = -1;
        int declarationEnd = 0;
        int index = 0;
        while (index < presentation.Children.Length)
        {
            OdfNode child = presentation.Children[index];
            if (IsSlideNode(child))
            {
                if (pageIndex == slideIndex)
                {
                    return index;
                }
                pageIndex = pageIndex + 1;
                lastPageEnd = index + 1;
            }
            else if (pageIndex == 0 && IsDeclarationNode(child))
            {
                declarationEnd = index + 1;
            }
            index = index + 1;
        }
        return lastPageEnd >= 0 ? lastPageEnd : declarationEnd;
    }

    static void CollectIdentifiers(
        OdfNode node,
        List<string> identifiers)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (IsIdentifierAttribute(attribute)
                && attribute.Value.Length > 0
                && !Contains(identifiers, attribute.Value))
            {
                identifiers.Add(attribute.Value);
            }
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            CollectIdentifiers(node.Children[index], identifiers);
            index = index + 1;
        }
    }

    static void RemoveDeletedReferences(
        OdfDocument document,
        OdfNode deleted,
        List<string> identifiers)
    {
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode[] nodes = document.Parts[partIndex].Nodes;
            int nodeIndex = 0;
            while (nodeIndex < nodes.Length)
            {
                RemoveNodeReferences(
                    nodes[nodeIndex],
                    deleted,
                    identifiers);
                nodeIndex = nodeIndex + 1;
            }
            partIndex = partIndex + 1;
        }
    }

    static void RemoveNodeReferences(
        OdfNode node,
        OdfNode deleted,
        List<string> identifiers)
    {
        if (node == deleted)
        {
            return;
        }
        List<OdfAttribute> retained = new List<OdfAttribute>();
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            bool remove = false;
            if (IsIdentifierListReference(attribute))
            {
                attribute.Value = RemoveIdentifierTokens(
                    attribute.Value,
                    identifiers);
                remove = attribute.Value.Length == 0;
            }
            else if (ReferencesDeletedIdentifier(attribute, identifiers))
            {
                remove = true;
            }
            if (!remove)
            {
                retained.Add(attribute);
            }
            index = index + 1;
        }
        node.Attributes = retained.ToArray();
        index = 0;
        while (index < node.Children.Length)
        {
            RemoveNodeReferences(
                node.Children[index],
                deleted,
                identifiers);
            index = index + 1;
        }
    }

    static bool ReferencesDeletedIdentifier(
        OdfAttribute attribute,
        List<string> identifiers)
    {
        if (IsIdentifierListReference(attribute))
        {
            return false;
        }
        string value = attribute.Value;
        if (attribute.Name.NamespaceName == OdfNamespaces.XLink
            && attribute.Name.LocalName == "href"
            && value.Length > 1
            && value[0] == '#')
        {
            value = value.Substring(1);
        }
        else if (!IsDirectIdentifierReference(attribute))
        {
            return false;
        }
        return Contains(identifiers, value);
    }

    static string RemoveIdentifierTokens(
        string value,
        List<string> identifiers)
    {
        global::System.Text.StringBuilder result =
            new global::System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length
                && global::System.Char.IsWhiteSpace(value[index]))
            {
                index = index + 1;
            }
            int start = index;
            while (index < value.Length
                && !global::System.Char.IsWhiteSpace(value[index]))
            {
                index = index + 1;
            }
            if (index > start)
            {
                string token = value.Substring(start, index - start);
                if (!Contains(identifiers, token))
                {
                    if (result.Length > 0)
                    {
                        result.Append(' ');
                    }
                    result.Append(token);
                }
            }
        }
        return result.ToString();
    }

    static bool IsIdentifierListReference(OdfAttribute attribute)
    {
        return attribute.Name.NamespaceName == OdfNamespaces.Drawing
            && attribute.Name.LocalName == "nav-order";
    }

    static bool IsDirectIdentifierReference(OdfAttribute attribute)
    {
        if (attribute.Name.NamespaceName == OdfNamespaces.Drawing)
        {
            string localName = attribute.Name.LocalName;
            return localName == "copy-of"
                || localName == "caption-id"
                || localName == "start-shape"
                || localName == "end-shape";
        }
        return attribute.Name.NamespaceName == OdfNamespaces.Smil
            && attribute.Name.LocalName == "targetElement";
    }

    static bool IsIdentifierAttribute(OdfAttribute attribute)
    {
        return attribute.Name.NamespaceName == OdfNamespaces.Drawing
            && attribute.Name.LocalName == "id"
            || attribute.Name.NamespaceName == OdfNamespaces.Xml
            && attribute.Name.LocalName == "id";
    }

    static bool Contains(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void SetPartPath(OdfNode node, string partPath)
    {
        node.Location.PartPath = partPath;
        int index = 0;
        while (index < node.Attributes.Length)
        {
            node.Attributes[index].Location.PartPath = partPath;
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            SetPartPath(node.Children[index], partPath);
            index = index + 1;
        }
    }

    internal static void InsertChild(
        OdfNode parent,
        int insertionIndex,
        OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, children, insertionIndex);
        children[insertionIndex] = child;
        Array.Copy(
            parent.Children,
            insertionIndex,
            children,
            insertionIndex + 1,
            parent.Children.Length - insertionIndex);
        child.Parent = parent;
        parent.Children = children;
        parent.WasEmptyElement = false;
    }

    static void RemoveChild(OdfNode parent, OdfNode child)
    {
        int match = ChildIndex(parent, child);
        if (match < 0)
        {
            throw new OdfEditException(
                "The presentation does not contain the requested slide.");
        }
        OdfNode[] children = new OdfNode[parent.Children.Length - 1];
        Array.Copy(parent.Children, children, match);
        Array.Copy(
            parent.Children,
            match + 1,
            children,
            match,
            parent.Children.Length - match - 1);
        parent.Children = children;
        child.Parent = null;
    }

    static int ChildIndex(OdfNode parent, OdfNode child)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (parent.Children[index] == child)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static OdfNode RequirePresentation(OdfDocument document)
    {
        OdfNode? body = document.Body;
        if (body != null)
        {
            int index = 0;
            while (index < body.Children.Length)
            {
                if (IsElement(
                    body.Children[index],
                    OdfNamespaces.Office,
                    "presentation"))
                {
                    return body.Children[index];
                }
                index = index + 1;
            }
        }
        throw new OdfEditException(
            "The document has no presentation body for slides.");
    }

    internal static OdfSlide RequireSlideByNode(
        OdfDocument document,
        OdfNode node)
    {
        int index = 0;
        while (index < document.Slides.Length)
        {
            if (document.Slides[index].Node == node)
            {
                return document.Slides[index];
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The edited slide could not be materialized.");
    }

    internal static void RequireOwnedSlide(
        OdfDocument document,
        OdfSlide slide)
    {
        RequireDocument(document);
        if (slide == null || slide.Node == null)
        {
            throw new OdfEditException(
                "A materialized presentation slide is required.");
        }
        int index = 0;
        while (index < document.Slides.Length)
        {
            if (document.Slides[index] == slide)
            {
                return;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The slide does not belong to the supplied document.");
    }

    static void RequireDocument(OdfDocument document)
    {
        if (document == null || document.Slides == null)
        {
            throw new OdfEditException(
                "A materialized presentation document is required.");
        }
        if (document.Kind != OdfDocumentKind.Presentation
            && document.Kind != OdfDocumentKind.PresentationTemplate)
        {
            throw new OdfEditException(
                "Slide operations require a presentation document.");
        }
    }

    static bool IsSlideNode(OdfNode node)
    {
        return IsElement(node, OdfNamespaces.Drawing, "page");
    }

    static bool IsDeclarationNode(OdfNode node)
    {
        if (node.Name == null
            || node.Name.NamespaceName != OdfNamespaces.Presentation)
        {
            return false;
        }
        string localName = node.Name.LocalName;
        return localName == "header-decl"
            || localName == "footer-decl"
            || localName == "date-time-decl";
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
}
