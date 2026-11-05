using System;
using System.Collections.Generic;

namespace odf;

internal class StyleResourceDocumentState
{
    internal OdfNode[] OriginalNodes = [];
}

internal static class StyleResourceDocumentModel
{
    internal static void LoadStyleResources(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for style-resource materialization.");
        }
        List<OdfStyleResource> resources = new List<OdfStyleResource>();
        List<OdfNode> nodes = new List<OdfNode>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            OdfNode[] partNodes = new OdfTreeEditor(part.Nodes)
                .TraverseAllNodes();
            int nodeIndex = 0;
            while (nodeIndex < partNodes.Length)
            {
                OdfNode node = partNodes[nodeIndex];
                if (IsStyleResourceNode(node))
                {
                    resources.Add(CreateStyleResource(part, node));
                    nodes.Add(node);
                }
                nodeIndex = nodeIndex + 1;
            }
            partIndex = partIndex + 1;
        }
        document.StyleResources = resources.ToArray();
        StyleResourceDocumentState state =
            new StyleResourceDocumentState();
        state.OriginalNodes = nodes.ToArray();
        document.StyleResourceState = state;
    }

    internal static void SynchronizeStyleResources(OdfDocument document)
    {
        if (document == null || document.StyleResources == null)
        {
            throw new OdfDocumentException(
                "A document and style-resource collection are required for writing.");
        }
        StyleResourceDocumentState state = document.StyleResourceState
            ?? new StyleResourceDocumentState();
        RemoveDeletedResources(document.StyleResources, state.OriginalNodes);
        List<OdfNode> nodes = new List<OdfNode>();
        int index = 0;
        while (index < document.StyleResources.Length)
        {
            OdfStyleResource resource = document.StyleResources[index];
            if (resource == null)
            {
                throw new OdfDocumentException(
                    "The style-resource collection contains a null resource.");
            }
            SynchronizeStyleResource(document, resource);
            if (resource.OriginalNode != null)
            {
                nodes.Add(resource.OriginalNode);
            }
            index = index + 1;
        }
        state.OriginalNodes = nodes.ToArray();
        document.StyleResourceState = state;
    }

    static OdfStyleResource CreateStyleResource(
        OdfPart part,
        OdfNode node)
    {
        OdfStyleResource result = new OdfStyleResource();
        result.Kind = ParseStyleResourceKind(node);
        result.Name = ReadResourceName(node, result.Kind);
        result.DisplayName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Drawing,
            "display-name");
        result.Href = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.XLink,
            "href");
        result.PartPath = part.Path;
        result.Attributes =
            StylePropertyDocumentModel.ReadPropertyAttributes(node);
        result.OriginalAttributes =
            StylePropertyDocumentModel.AttributeNodes(result.Attributes);
        result.Content =
            StylePropertyDocumentModel.PropertyChildren(node);
        result.OriginalContent =
            StylePropertyDocumentModel.CopyNodes(result.Content);
        result.ContainerNode = node.Parent;
        result.Node = node;
        result.OriginalNode = node;
        SnapshotStyleResource(result);
        return result;
    }

    static void SynchronizeStyleResource(
        OdfDocument document,
        OdfStyleResource resource)
    {
        if (resource.Attributes == null || resource.Content == null)
        {
            throw new OdfDocumentException(
                "Style-resource attribute and content collections are required for writing.");
        }
        OdfNode? node = resource.OriginalNode;
        bool isNew = node == null;
        if (node != null
            && IsNestedResource(resource.Kind)
            && node.Parent == null
            && resource.ContainerNode == resource.OriginalContainerNode
            && !ResourceHasTypedChanges(resource))
        {
            resource.Node = node;
            return;
        }
        OdfNode parent = ResolveResourceParent(document, resource);
        if (node == null)
        {
            node = CreateResourceElement(
                resource.Kind,
                parent.Location.PartPath);
            StyleDocumentModel.AppendNode(parent, node);
            resource.OriginalNode = node;
        }
        else if (node.Parent != parent)
        {
            StyleDocumentModel.RemoveNodeFromParent(node);
            StyleDocumentModel.AppendNode(parent, node);
        }
        ApplyResourceKind(resource, node, isNew);
        EnsureElementNamespace(node);
        ApplyResourceFields(resource);
        StylePropertyDocumentModel.SynchronizeElementAttributes(
            node,
            resource.Attributes,
            resource.OriginalAttributes);
        resource.OriginalAttributes =
            StylePropertyDocumentModel.AttributeNodes(resource.Attributes);
        resource.Name = ReadResourceName(node, resource.Kind);
        resource.DisplayName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Drawing,
            "display-name");
        resource.Href = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.XLink,
            "href");
        if (!NodesMatch(resource.Content, resource.OriginalContent))
        {
            SynchronizeResourceContent(node, resource.Content);
        }
        resource.Node = node;
        resource.ContainerNode = parent;
        resource.PartPath = parent.Location.PartPath;
        resource.OriginalContent =
            StylePropertyDocumentModel.CopyNodes(resource.Content);
        SnapshotStyleResource(resource);
    }

    static bool ResourceHasTypedChanges(OdfStyleResource resource)
    {
        return resource.Kind != resource.OriginalKind
            || resource.Name != resource.OriginalName
            || resource.DisplayName != resource.OriginalDisplayName
            || resource.Href != resource.OriginalHref
            || !AttributesMatchOriginal(
                resource.Attributes,
                resource.OriginalAttributes)
            || !NodesMatch(resource.Content, resource.OriginalContent);
    }

    static bool AttributesMatchOriginal(
        OdfStyleProperty[] properties,
        OdfAttribute[] attributes)
    {
        if (properties.Length != attributes.Length)
        {
            return false;
        }
        int index = 0;
        while (index < properties.Length)
        {
            OdfStyleProperty property = properties[index];
            if (property == null
                || property.Name == null
                || property.OriginalAttribute != attributes[index]
                || property.Name.NamespaceName
                    != property.OriginalNamespaceName
                || property.Name.LocalName != property.OriginalLocalName
                || property.Name.Prefix != property.OriginalPrefix
                || property.LexicalValue != property.OriginalLexicalValue)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static OdfNode ResolveResourceParent(
        OdfDocument document,
        OdfStyleResource resource)
    {
        if (IsNestedResource(resource.Kind))
        {
            OdfNode? container = resource.ContainerNode
                ?? resource.OriginalContainerNode;
            if (container == null)
            {
                throw new OdfDocumentException(
                    "A nested background-image or sound resource requires an owning element.");
            }
            return container;
        }
        string partPath = document.PackageKind == OdfPackageKind.Flat
            ? ""
            : OdfPartNames.Styles;
        OdfPart? part = StyleDocumentModel.FindPart(document, partPath);
        if (part == null)
        {
            part = StyleDocumentModel.CreateStylesPart(document);
        }
        if (part.Styles == null)
        {
            part.Styles = StyleDocumentModel.CreateStyleSection(
                document,
                part,
                false);
        }
        return part.Styles;
    }

    static void ApplyResourceKind(
        OdfStyleResource resource,
        OdfNode node,
        bool isNew)
    {
        if (!isNew && resource.Kind == resource.OriginalKind)
        {
            return;
        }
        OdfQualifiedName name = ResourceElementName(resource.Kind);
        if (node.Name == null)
        {
            node.Name = new OdfQualifiedName();
        }
        node.Kind = OdfNodeKind.Element;
        node.Name.NamespaceName = name.NamespaceName;
        node.Name.LocalName = name.LocalName;
        node.Name.Prefix = name.Prefix;
    }

    static void ApplyResourceFields(OdfStyleResource resource)
    {
        string nameNamespace = ResourceNameNamespace(resource.Kind);
        string namePrefix = nameNamespace == OdfNamespaces.Style
            ? OdfNamespaces.StylePrefix
            : OdfNamespaces.DrawingPrefix;
        resource.Attributes = ApplyConvenienceAttribute(
            resource.Attributes,
            nameNamespace,
            "name",
            namePrefix,
            resource.Name,
            resource.OriginalName);
        resource.Attributes = ApplyConvenienceAttribute(
            resource.Attributes,
            OdfNamespaces.Drawing,
            "display-name",
            OdfNamespaces.DrawingPrefix,
            resource.DisplayName,
            resource.OriginalDisplayName);
        resource.Attributes = ApplyConvenienceAttribute(
            resource.Attributes,
            OdfNamespaces.XLink,
            "href",
            OdfNamespaces.XLinkPrefix,
            resource.Href,
            resource.OriginalHref);
    }

    static OdfStyleProperty[] ApplyConvenienceAttribute(
        OdfStyleProperty[] properties,
        string namespaceName,
        string localName,
        string prefix,
        string value,
        string originalValue)
    {
        if (value == originalValue)
        {
            return properties;
        }
        List<OdfStyleProperty> result = new List<OdfStyleProperty>();
        OdfStyleProperty? existing = null;
        int index = 0;
        while (index < properties.Length)
        {
            OdfStyleProperty property = properties[index];
            if (property == null || property.Name == null)
            {
                throw new OdfDocumentException(
                    "A resource attribute and qualified name are required for writing.");
            }
            if (property.Name.NamespaceName == namespaceName
                && property.Name.LocalName == localName)
            {
                existing = property;
            }
            else
            {
                result.Add(property);
            }
            index = index + 1;
        }
        if (value.Length > 0)
        {
            if (existing == null)
            {
                existing = new OdfStyleProperty();
                existing.Name.NamespaceName = namespaceName;
                existing.Name.LocalName = localName;
                existing.Name.Prefix = prefix;
            }
            existing.LexicalValue = value;
            result.Add(existing);
        }
        return result.ToArray();
    }

    static OdfNode CreateResourceElement(
        OdfStyleResourceKind kind,
        string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = ResourceElementName(kind);
        result.Location.PartPath = partPath;
        EnsureElementNamespace(result);
        return result;
    }

    static OdfQualifiedName ResourceElementName(OdfStyleResourceKind kind)
    {
        if (kind == OdfStyleResourceKind.BackgroundImage)
        {
            return StyleDocumentModel.QualifiedName(
                OdfNamespaces.Style,
                "background-image",
                OdfNamespaces.StylePrefix);
        }
        if (kind == OdfStyleResourceKind.Sound)
        {
            return StyleDocumentModel.QualifiedName(
                OdfNamespaces.Presentation,
                "sound",
                OdfNamespaces.PresentationPrefix);
        }
        if (kind == OdfStyleResourceKind.PresentationLayout)
        {
            return StyleDocumentModel.QualifiedName(
                OdfNamespaces.Style,
                "presentation-page-layout",
                OdfNamespaces.StylePrefix);
        }
        return StyleDocumentModel.QualifiedName(
            OdfNamespaces.Drawing,
            DrawingResourceLocalName(kind),
            OdfNamespaces.DrawingPrefix);
    }

    static string DrawingResourceLocalName(OdfStyleResourceKind kind)
    {
        if (kind == OdfStyleResourceKind.Hatch)
        {
            return "hatch";
        }
        if (kind == OdfStyleResourceKind.FillImage)
        {
            return "fill-image";
        }
        if (kind == OdfStyleResourceKind.Opacity)
        {
            return "opacity";
        }
        if (kind == OdfStyleResourceKind.StrokeDash)
        {
            return "stroke-dash";
        }
        if (kind == OdfStyleResourceKind.Marker)
        {
            return "marker";
        }
        return "gradient";
    }

    static string ResourceNameNamespace(OdfStyleResourceKind kind)
    {
        if (kind == OdfStyleResourceKind.PresentationLayout)
        {
            return OdfNamespaces.Style;
        }
        return OdfNamespaces.Drawing;
    }

    static string ReadResourceName(
        OdfNode node,
        OdfStyleResourceKind kind)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            ResourceNameNamespace(kind),
            "name");
    }

    static bool IsNestedResource(OdfStyleResourceKind kind)
    {
        return kind == OdfStyleResourceKind.BackgroundImage
            || kind == OdfStyleResourceKind.Sound;
    }

    static bool IsStyleResourceNode(OdfNode node)
    {
        if (node.Kind != OdfNodeKind.Element || node.Name == null)
        {
            return false;
        }
        string namespaceName = node.Name.NamespaceName;
        string localName = node.Name.LocalName;
        if (namespaceName == OdfNamespaces.Drawing)
        {
            return localName == "gradient"
                || localName == "hatch"
                || localName == "fill-image"
                || localName == "opacity"
                || localName == "stroke-dash"
                || localName == "marker";
        }
        if (namespaceName == OdfNamespaces.Svg)
        {
            return localName == "linearGradient"
                || localName == "radialGradient";
        }
        if (namespaceName == OdfNamespaces.Style)
        {
            return localName == "background-image"
                || localName == "presentation-page-layout";
        }
        return namespaceName == OdfNamespaces.Presentation
            && localName == "sound";
    }

    static OdfStyleResourceKind ParseStyleResourceKind(OdfNode node)
    {
        if (node.Name == null)
        {
            return OdfStyleResourceKind.Gradient;
        }
        string name = node.Name.LocalName;
        if (name == "hatch")
        {
            return OdfStyleResourceKind.Hatch;
        }
        if (name == "fill-image")
        {
            return OdfStyleResourceKind.FillImage;
        }
        if (name == "opacity")
        {
            return OdfStyleResourceKind.Opacity;
        }
        if (name == "stroke-dash")
        {
            return OdfStyleResourceKind.StrokeDash;
        }
        if (name == "marker")
        {
            return OdfStyleResourceKind.Marker;
        }
        if (name == "background-image")
        {
            return OdfStyleResourceKind.BackgroundImage;
        }
        if (name == "sound")
        {
            return OdfStyleResourceKind.Sound;
        }
        if (name == "presentation-page-layout")
        {
            return OdfStyleResourceKind.PresentationLayout;
        }
        return OdfStyleResourceKind.Gradient;
    }

    static void SynchronizeResourceContent(
        OdfNode node,
        OdfNode[] content)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            if (!ContainsNode(content, node.Children[index]))
            {
                node.Children[index].Parent = null;
            }
            index = index + 1;
        }
        index = 0;
        while (index < content.Length)
        {
            OdfNode child = content[index];
            if (child == null)
            {
                throw new OdfDocumentException(
                    "Resource content cannot contain a null node.");
            }
            if (child.Parent != null && child.Parent != node)
            {
                StyleDocumentModel.RemoveNodeFromParent(child);
            }
            child.Parent = node;
            child.Location.PartPath = node.Location.PartPath;
            index = index + 1;
        }
        node.Children = StylePropertyDocumentModel.CopyNodes(content);
        node.WasEmptyElement = content.Length == 0;
    }

    static void EnsureElementNamespace(OdfNode node)
    {
        if (node.Name == null || node.Name.NamespaceName.Length == 0)
        {
            return;
        }
        OdfNode? current = node;
        while (current != null)
        {
            int index = 0;
            while (index < current.NamespaceDeclarations.Length)
            {
                OdfNamespaceDeclaration declaration =
                    current.NamespaceDeclarations[index];
                if (declaration.Prefix == node.Name.Prefix
                    && declaration.NamespaceName == node.Name.NamespaceName)
                {
                    return;
                }
                index = index + 1;
            }
            current = current.Parent;
        }
        OdfNamespaceDeclaration[] declarations =
            new OdfNamespaceDeclaration[
                node.NamespaceDeclarations.Length + 1];
        Array.Copy(
            node.NamespaceDeclarations,
            declarations,
            node.NamespaceDeclarations.Length);
        OdfNamespaceDeclaration created = new OdfNamespaceDeclaration();
        created.Prefix = node.Name.Prefix;
        created.NamespaceName = node.Name.NamespaceName;
        created.Location.PartPath = node.Location.PartPath;
        declarations[declarations.Length - 1] = created;
        node.NamespaceDeclarations = declarations;
    }

    static void RemoveDeletedResources(
        OdfStyleResource[] resources,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            if (!ResourceRetainsNode(resources, node) && node.Parent != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static bool ResourceRetainsNode(
        OdfStyleResource[] resources,
        OdfNode node)
    {
        int index = 0;
        while (index < resources.Length)
        {
            if (resources[index] != null
                && resources[index].OriginalNode == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsNode(OdfNode[] nodes, OdfNode target)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool NodesMatch(OdfNode[] first, OdfNode[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static void SnapshotStyleResource(OdfStyleResource resource)
    {
        resource.OriginalKind = resource.Kind;
        resource.OriginalName = resource.Name;
        resource.OriginalDisplayName = resource.DisplayName;
        resource.OriginalHref = resource.Href;
        resource.OriginalPartPath = resource.PartPath;
        resource.OriginalContainerNode = resource.ContainerNode;
    }
}
