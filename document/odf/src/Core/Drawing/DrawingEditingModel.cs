using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal static class DrawingEditingModel
{
    internal static OdfDrawingObject CreateDrawing(
        OdfDocument document,
        OdfDrawingObject? parent,
        OdfDrawingKind kind,
        string name)
    {
        RequireDocument(document);
        OdfNode container = RequireContainer(document, parent);
        DrawingElementName elementName = ElementName(kind);
        if (elementName.LocalName.Length == 0)
        {
            throw new OdfEditException(
                "The requested drawing kind cannot be created.");
        }
        OdfNode node = new OdfNode();
        node.Kind = OdfNodeKind.Element;
        node.Name = StyleDocumentModel.QualifiedName(
            elementName.NamespaceName,
            elementName.LocalName,
            elementName.Prefix);
        node.Location.PartPath = parent == null
            ? BodyPartPath(document)
            : parent.PartPath;
        if (name.Length > 0)
        {
            SetAttribute(
                node,
                OdfNamespaces.Drawing,
                "name",
                OdfNamespaces.DrawingPrefix,
                name);
        }
        AppendChild(container, node);
        RefreshAll(document);
        return RequireDrawingByNode(document, node);
    }

    internal static void RemoveDrawing(
        OdfDocument document,
        OdfDrawingObject drawing)
    {
        RequireOwnedDrawing(document, drawing);
        OdfNode node = drawing.Node ?? new OdfNode();
        OdfNode? parent = node.Parent;
        if (parent == null)
        {
            throw new OdfEditException(
                "A root XML node cannot be removed as a drawing object.");
        }
        RemoveChild(parent, node);
        RefreshAll(document);
    }

    internal static OdfDrawingObject CopyDrawing(
        OdfDocument document,
        OdfDrawingObject drawing,
        OdfDrawingObject targetParent)
    {
        RequireOwnedDrawing(document, drawing);
        RequireOwnedDrawing(document, targetParent);
        OdfNode sourceNode = drawing.Node ?? new OdfNode();
        OdfNode clone = new OdfTreeEditor([]).CloneSubtree(sourceNode);
        PrepareCopiedIdentifiers(document, clone);
        AppendChild(targetParent.Node ?? new OdfNode(), clone);
        RefreshAll(document);
        return RequireDrawingByNode(document, clone);
    }

    internal static OdfDrawingObject ImportDrawing(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        OdfDrawingObject drawing,
        OdfDrawingObject targetParent)
    {
        RequireOwnedDrawing(sourceDocument, drawing);
        RequireOwnedDrawing(targetDocument, targetParent);
        OdfNode sourceNode = drawing.Node ?? new OdfNode();
        OdfNode clone = new OdfTreeEditor([]).CloneSubtree(sourceNode);
        DrawingResourceManager.CopyDrawingDependencies(
            sourceDocument,
            targetDocument,
            drawing,
            clone,
            targetParent.PartPath);
        PrepareCopiedIdentifiers(targetDocument, clone);
        AppendChild(targetParent.Node ?? new OdfNode(), clone);
        RefreshAll(targetDocument);
        return RequireDrawingByNode(targetDocument, clone);
    }

    internal static OdfDrawingObject RenameIdentifier(
        OdfDocument document,
        OdfDrawingObject drawing,
        string newIdentifier)
    {
        RequireOwnedDrawing(document, drawing);
        if (!IsIdentifier(newIdentifier))
        {
            throw new OdfEditException(
                "A drawing identifier must be a non-empty XML name.");
        }
        int index = 0;
        while (index < document.DrawingObjects.Length)
        {
            OdfDrawingObject candidate = document.DrawingObjects[index];
            if (candidate != drawing
                && (candidate.Id == newIdentifier
                    || candidate.XmlId == newIdentifier))
            {
                throw new OdfEditException(
                    "The requested drawing identifier is already in use.");
            }
            index = index + 1;
        }
        string oldIdentifier = drawing.Id.Length > 0
            ? drawing.Id
            : drawing.XmlId;
        if (oldIdentifier.Length == 0)
        {
            drawing.Id = newIdentifier;
        }
        else if (drawing.Id.Length > 0)
        {
            drawing.Id = newIdentifier;
        }
        else
        {
            drawing.XmlId = newIdentifier;
        }
        UpdateTypedReferences(
            document.DrawingObjects,
            oldIdentifier,
            newIdentifier);
        DrawingDocumentModel.SynchronizeDrawings(
            document,
            document.SaveOptions);
        RefreshResources(document);
        DrawingResourceManager.RefreshOrphans(document);
        PresentationDocumentModel.LoadPresentation(document);
        return RequireDrawingByNode(
            document,
            drawing.Node ?? new OdfNode());
    }

    internal static void ApplyChanges(OdfDocument document)
    {
        RequireDocument(document);
        ImageResourceManager.SynchronizeImages(document);
        MediaResourceManager.SynchronizeMediaResources(document);
        EmbeddedContentManager.SynchronizeEmbeddedContent(document);
        DrawingDocumentModel.SynchronizeDrawings(
            document,
            document.SaveOptions);
        RefreshResources(document);
        DrawingResourceManager.RefreshOrphans(document);
        PresentationDocumentModel.LoadPresentation(document);
    }

    internal static void RefreshAll(OdfDocument document)
    {
        DrawingDocumentModel.LoadDrawings(document);
        RefreshResources(document);
        DrawingResourceManager.RefreshOrphans(document);
        PresentationDocumentModel.LoadPresentation(document);
    }

    internal static OdfDrawingObject RequireDrawingByNode(
        OdfDocument document,
        OdfNode node)
    {
        int index = 0;
        while (index < document.DrawingObjects.Length)
        {
            if (document.DrawingObjects[index].Node == node)
            {
                return document.DrawingObjects[index];
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The edited drawing could not be materialized.");
    }

    static void RefreshResources(OdfDocument document)
    {
        ImageResourceManager.LoadImages(document);
        MediaResourceManager.LoadMediaResources(document);
        EmbeddedContentManager.LoadEmbeddedContent(document);
    }

    internal static void PrepareCopiedIdentifiers(
        OdfDocument document,
        OdfNode clone)
    {
        List<string> oldIdentifiers = new List<string>();
        List<string> newIdentifiers = new List<string>();
        List<string> assignedNames = new List<string>();
        AssignCopiedValues(
            document,
            clone,
            oldIdentifiers,
            newIdentifiers,
            assignedNames);
        RemapCopiedReferences(clone, oldIdentifiers, newIdentifiers);
    }

    static void AssignCopiedValues(
        OdfDocument document,
        OdfNode node,
        List<string> oldIdentifiers,
        List<string> newIdentifiers,
        List<string> assignedNames)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (IsIdentifierAttribute(attribute)
                && attribute.Value.Length > 0)
            {
                string oldValue = attribute.Value;
                string newValue = UniqueIdentifier(
                    document,
                    oldValue,
                    newIdentifiers);
                attribute.Value = newValue;
                if (attribute.Name.NamespaceName == OdfNamespaces.Xml
                    && attribute.Name.LocalName == "id")
                {
                    node.XmlId = newValue;
                }
                oldIdentifiers.Add(oldValue);
                newIdentifiers.Add(newValue);
            }
            else if (attribute.Name.NamespaceName == OdfNamespaces.Drawing
                && attribute.Name.LocalName == "name"
                && attribute.Value.Length > 0)
            {
                string newName = UniqueDrawingName(
                    document,
                    attribute.Value,
                    assignedNames);
                attribute.Value = newName;
                assignedNames.Add(newName);
            }
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            AssignCopiedValues(
                document,
                node.Children[index],
                oldIdentifiers,
                newIdentifiers,
                assignedNames);
            index = index + 1;
        }
    }

    static void RemapCopiedReferences(
        OdfNode node,
        List<string> oldIdentifiers,
        List<string> newIdentifiers)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (IsDirectIdentifierReference(attribute))
            {
                attribute.Value = MappedIdentifier(
                    attribute.Value,
                    oldIdentifiers,
                    newIdentifiers);
            }
            else if (attribute.Name.NamespaceName == OdfNamespaces.XLink
                && attribute.Name.LocalName == "href"
                && attribute.Value.StartsWith(
                    "#",
                    global::System.StringComparison.Ordinal))
            {
                string mapped = MappedIdentifier(
                    attribute.Value.Substring(1),
                    oldIdentifiers,
                    newIdentifiers);
                attribute.Value = "#" + mapped;
            }
            else if (attribute.Name.NamespaceName == OdfNamespaces.Drawing
                && attribute.Name.LocalName == "nav-order")
            {
                attribute.Value = RemapIdentifierList(
                    attribute.Value,
                    oldIdentifiers,
                    newIdentifiers);
            }
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            RemapCopiedReferences(
                node.Children[index],
                oldIdentifiers,
                newIdentifiers);
            index = index + 1;
        }
    }

    static void UpdateTypedReferences(
        OdfDrawingObject[] drawings,
        string oldIdentifier,
        string newIdentifier)
    {
        if (oldIdentifier.Length == 0)
        {
            return;
        }
        int index = 0;
        while (index < drawings.Length)
        {
            OdfDrawingReference[] references = drawings[index].References;
            int referenceIndex = 0;
            while (referenceIndex < references.Length)
            {
                OdfDrawingReference reference = references[referenceIndex];
                if (reference.Target == oldIdentifier)
                {
                    reference.Target = newIdentifier;
                }
                else if (reference.Kind == OdfDrawingReferenceKind.Hyperlink
                    && reference.Target == "#" + oldIdentifier)
                {
                    reference.Target = "#" + newIdentifier;
                }
                referenceIndex = referenceIndex + 1;
            }
            index = index + 1;
        }
    }

    static string RemapIdentifierList(
        string value,
        List<string> oldIdentifiers,
        List<string> newIdentifiers)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length
                && global::System.Char.IsWhiteSpace(value[index]))
            {
                if (result.Length > 0)
                {
                    result.Append(value[index]);
                }
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
                result.Append(MappedIdentifier(
                    value.Substring(start, index - start),
                    oldIdentifiers,
                    newIdentifiers));
            }
        }
        return result.ToString();
    }

    static string MappedIdentifier(
        string value,
        List<string> oldIdentifiers,
        List<string> newIdentifiers)
    {
        int index = 0;
        while (index < oldIdentifiers.Count)
        {
            if (oldIdentifiers[index] == value)
            {
                return newIdentifiers[index];
            }
            index = index + 1;
        }
        return value;
    }

    static string UniqueIdentifier(
        OdfDocument document,
        string requested,
        List<string> assigned)
    {
        if (!IdentifierExists(document, requested)
            && !Contains(assigned, requested))
        {
            return requested;
        }
        int number = 2;
        string candidate = requested + "-copy-2";
        while (IdentifierExists(document, candidate)
            || Contains(assigned, candidate))
        {
            number = number + 1;
            candidate = requested + "-copy-" + number.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture);
        }
        return candidate;
    }

    static string UniqueDrawingName(
        OdfDocument document,
        string requested,
        List<string> assigned)
    {
        if (!DrawingNameExists(document, requested)
            && !Contains(assigned, requested))
        {
            return requested;
        }
        int number = 2;
        string candidate = requested + " Copy 2";
        while (DrawingNameExists(document, candidate)
            || Contains(assigned, candidate))
        {
            number = number + 1;
            candidate = requested + " Copy " + number.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture);
        }
        return candidate;
    }

    static bool IdentifierExists(OdfDocument document, string value)
    {
        int index = 0;
        while (index < document.DrawingObjects.Length)
        {
            if (document.DrawingObjects[index].Id == value
                || document.DrawingObjects[index].XmlId == value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool DrawingNameExists(OdfDocument document, string value)
    {
        int index = 0;
        while (index < document.DrawingObjects.Length)
        {
            if (document.DrawingObjects[index].Name == value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
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

    static bool IsIdentifier(string value)
    {
        if (value == null || value.Length == 0
            || !IsIdentifierStart(value[0]))
        {
            return false;
        }
        int index = 1;
        while (index < value.Length)
        {
            char character = value[index];
            if (!IsIdentifierStart(character)
                && !global::System.Char.IsDigit(character)
                && character != '-'
                && character != '.')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsIdentifierStart(char character)
    {
        return character == '_'
            || global::System.Char.IsLetter(character);
    }

    static bool IsIdentifierAttribute(OdfAttribute attribute)
    {
        return attribute.Name.NamespaceName == OdfNamespaces.Drawing
            && attribute.Name.LocalName == "id"
            || attribute.Name.NamespaceName == OdfNamespaces.Xml
            && attribute.Name.LocalName == "id";
    }

    static bool IsDirectIdentifierReference(OdfAttribute attribute)
    {
        if (attribute.Name.NamespaceName == OdfNamespaces.Smil
            && attribute.Name.LocalName == "targetElement")
        {
            return true;
        }
        if (attribute.Name.NamespaceName != OdfNamespaces.Drawing)
        {
            return false;
        }
        string localName = attribute.Name.LocalName;
        return localName == "copy-of"
            || localName == "caption-id"
            || localName == "start-shape"
            || localName == "end-shape";
    }

    static DrawingElementName ElementName(OdfDrawingKind kind)
    {
        DrawingElementName result = new DrawingElementName();
        result.NamespaceName = OdfNamespaces.Drawing;
        result.Prefix = OdfNamespaces.DrawingPrefix;
        if (kind == OdfDrawingKind.Page) result.LocalName = "page";
        else if (kind == OdfDrawingKind.Layer) result.LocalName = "layer";
        else if (kind == OdfDrawingKind.Frame) result.LocalName = "frame";
        else if (kind == OdfDrawingKind.Group) result.LocalName = "g";
        else if (kind == OdfDrawingKind.Rectangle) result.LocalName = "rect";
        else if (kind == OdfDrawingKind.Ellipse) result.LocalName = "ellipse";
        else if (kind == OdfDrawingKind.Line) result.LocalName = "line";
        else if (kind == OdfDrawingKind.Polyline) result.LocalName = "polyline";
        else if (kind == OdfDrawingKind.Polygon) result.LocalName = "polygon";
        else if (kind == OdfDrawingKind.RegularPolygon) result.LocalName = "regular-polygon";
        else if (kind == OdfDrawingKind.Path) result.LocalName = "path";
        else if (kind == OdfDrawingKind.Connector) result.LocalName = "connector";
        else if (kind == OdfDrawingKind.Measure) result.LocalName = "measure";
        else if (kind == OdfDrawingKind.Caption) result.LocalName = "caption";
        else if (kind == OdfDrawingKind.Control) result.LocalName = "control";
        else if (kind == OdfDrawingKind.TextBox) result.LocalName = "text-box";
        else if (kind == OdfDrawingKind.CustomShape) result.LocalName = "custom-shape";
        else if (kind == OdfDrawingKind.Image) result.LocalName = "image";
        else if (kind == OdfDrawingKind.Media) result.LocalName = "plugin";
        else if (kind == OdfDrawingKind.EmbeddedObject) result.LocalName = "object";
        else if (kind == OdfDrawingKind.Scene3D) result.LocalName = "scene";
        else if (kind == OdfDrawingKind.Cube) result.LocalName = "cube";
        else if (kind == OdfDrawingKind.Sphere) result.LocalName = "sphere";
        else if (kind == OdfDrawingKind.Rotate) result.LocalName = "rotate";
        else if (kind == OdfDrawingKind.Extrude) result.LocalName = "extrude";
        if (kind == OdfDrawingKind.Scene3D
            || kind == OdfDrawingKind.Cube
            || kind == OdfDrawingKind.Sphere
            || kind == OdfDrawingKind.Rotate
            || kind == OdfDrawingKind.Extrude)
        {
            result.NamespaceName = OdfNamespaces.Dr3d;
            result.Prefix = OdfNamespaces.Dr3dPrefix;
        }
        return result;
    }

    static OdfNode RequireContainer(
        OdfDocument document,
        OdfDrawingObject? parent)
    {
        if (parent != null)
        {
            RequireOwnedDrawing(document, parent);
            return parent.Node ?? new OdfNode();
        }
        if (document.Body == null)
        {
            throw new OdfEditException(
                "The document has no body for a root drawing object.");
        }
        return document.Body;
    }

    static string BodyPartPath(OdfDocument document)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            if (document.Parts[index].Body == document.Body)
            {
                return document.Parts[index].Path;
            }
            index = index + 1;
        }
        return document.PackageKind == OdfPackageKind.Flat
            ? ""
            : OdfPartNames.Content;
    }

    static void RequireOwnedDrawing(
        OdfDocument document,
        OdfDrawingObject drawing)
    {
        RequireDocument(document);
        if (drawing == null || drawing.Node == null)
        {
            throw new OdfEditException(
                "A materialized drawing object is required.");
        }
        int index = 0;
        while (index < document.DrawingObjects.Length)
        {
            if (document.DrawingObjects[index] == drawing)
            {
                return;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The drawing object does not belong to the document.");
    }

    static void RequireDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for drawing editing.");
        }
    }

    static void SetAttribute(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                attribute.Value = value;
                return;
            }
            index = index + 1;
        }
        OdfAttribute added = new OdfAttribute();
        added.Name.NamespaceName = namespaceName;
        added.Name.LocalName = localName;
        added.Name.Prefix = prefix;
        added.Value = value;
        OdfAttribute[] attributes = new OdfAttribute[
            node.Attributes.Length + 1];
        Array.Copy(node.Attributes, attributes, node.Attributes.Length);
        attributes[attributes.Length - 1] = added;
        node.Attributes = attributes;
    }

    static void AppendChild(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, children, parent.Children.Length);
        child.Parent = parent;
        children[children.Length - 1] = child;
        parent.Children = children;
        parent.WasEmptyElement = false;
    }

    static void RemoveChild(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length - 1];
        int sourceIndex = 0;
        int targetIndex = 0;
        while (sourceIndex < parent.Children.Length)
        {
            if (parent.Children[sourceIndex] != child)
            {
                children[targetIndex] = parent.Children[sourceIndex];
                targetIndex = targetIndex + 1;
            }
            sourceIndex = sourceIndex + 1;
        }
        child.Parent = null;
        parent.Children = children;
    }
}

internal class DrawingElementName
{
    internal string NamespaceName = "";
    internal string LocalName = "";
    internal string Prefix = "";
}
