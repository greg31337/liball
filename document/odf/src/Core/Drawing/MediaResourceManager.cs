using System.Collections.Generic;

namespace odf;

internal static class MediaResourceManager
{
    internal static void LoadMediaResources(OdfDocument document)
    {
        List<OdfMediaResource> resources = new List<OdfMediaResource>();
        int index = 0;
        while (index < document.DrawingObjects.Length)
        {
            OdfDrawingObject drawing = document.DrawingObjects[index];
            drawing.MediaResource = null;
            if (drawing.Kind == OdfDrawingKind.Media && drawing.Node != null)
            {
                OdfMediaResource resource = CreateResource(document, drawing);
                drawing.MediaResource = resource;
                resources.Add(resource);
            }
            index = index + 1;
        }
        document.MediaResources = resources.ToArray();
    }

    internal static void SynchronizeMediaResources(OdfDocument document)
    {
        int index = 0;
        while (index < document.MediaResources.Length)
        {
            OdfMediaResource resource = document.MediaResources[index];
            if (resource == null || resource.Node == null)
            {
                throw new OdfEditException(
                    "A media resource requires an underlying XML element.");
            }
            SynchronizeResource(resource);
            ResolveDescription(document, resource);
            CaptureOriginalValues(resource);
            index = index + 1;
        }
        LoadMediaResources(document);
    }

    static OdfMediaResource CreateResource(
        OdfDocument document,
        OdfDrawingObject drawing)
    {
        OdfNode node = drawing.Node ?? new OdfNode();
        OdfMediaResource result = new OdfMediaResource();
        result.Drawing = drawing;
        result.Node = node;
        result.Href = Read(node, OdfNamespaces.XLink, "href");
        result.MediaType = Read(
            node,
            OdfNamespaces.Drawing,
            "mime-type");
        result.XLinkType = Read(node, OdfNamespaces.XLink, "type");
        result.Show = Read(node, OdfNamespaces.XLink, "show");
        result.Actuate = Read(node, OdfNamespaces.XLink, "actuate");
        result.Code = Read(node, OdfNamespaces.Drawing, "code");
        result.ObjectName = Read(node, OdfNamespaces.Drawing, "object");
        result.Archive = Read(node, OdfNamespaces.Drawing, "archive");
        result.MayScriptValue = Read(
            node,
            OdfNamespaces.Drawing,
            "may-script");
        result.FrameName = Read(
            node,
            OdfNamespaces.Drawing,
            "frame-name");
        result.Parameters = CreateParameters(node);
        ResolveDescription(document, result);
        CaptureOriginalValues(result);
        return result;
    }

    static void ResolveDescription(
        OdfDocument document,
        OdfMediaResource resource)
    {
        resource.Kind = ResourceKind(resource.Node, resource.MediaType);
        resource.IsMediaTypeValid = resource.MediaType.Length == 0
            || OdfMediaTypeRules.ParseMediaType(resource.MediaType).IsValid;
        RefreshMayScript(resource);
        ResolveSource(document, resource);
        resource.IsActiveContent =
            resource.Kind == OdfMediaResourceKind.Plugin
            || resource.Kind == OdfMediaResourceKind.Applet
            || resource.Kind == OdfMediaResourceKind.FloatingFrame;
    }

    static void ResolveSource(
        OdfDocument document,
        OdfMediaResource resource)
    {
        resource.SourceKind = OdfResourceSourceKind.Unknown;
        resource.PackagePath = "";
        resource.PackageEntryExists = false;
        resource.IsContentAvailable = false;
        resource.IsEncrypted = false;
        if (resource.Href.Length == 0)
        {
            resource.IsReferenceValid =
                resource.Kind == OdfMediaResourceKind.Applet;
            return;
        }
        resource.IsReferenceValid = OdfIriResolver.IsIriReference(
            resource.Href);
        if (!resource.IsReferenceValid)
        {
            return;
        }
        if (document.PackageKind == OdfPackageKind.Package
            && OdfIriResolver.IsPackageRelativeReference(resource.Href))
        {
            resource.SourceKind = OdfResourceSourceKind.Package;
            string partPath = resource.Drawing == null
                ? ""
                : resource.Drawing.PartPath;
            string resolved = PackagePathRules.ResolveRelativeReference(
                partPath,
                resource.Href);
            resource.PackagePath = ResourcePath(resolved);
            ResolvePackageEntry(document, resource);
        }
        else
        {
            resource.SourceKind = OdfResourceSourceKind.External;
        }
    }

    static void ResolvePackageEntry(
        OdfDocument document,
        OdfMediaResource resource)
    {
        if (resource.PackagePath.Length == 0 || document.Package == null)
        {
            return;
        }
        PackagePartCatalog catalog = document.Package.EntryIndex == null
            ? new PackagePartCatalog()
            : document.Package.EntryIndex.DiscoverParts();
        int index = 0;
        while (index < catalog.AllParts.Length)
        {
            DiscoveredPackagePart part = catalog.AllParts[index];
            if (part.Entry.Path == resource.PackagePath
                && !part.Entry.IsDirectory)
            {
                resource.PackageEntryExists = true;
                resource.IsEncrypted = part.IsEncrypted;
                resource.IsContentAvailable = ContentAvailable(
                    document,
                    resource.PackagePath);
                if (resource.MediaType.Length == 0)
                {
                    resource.MediaType = part.MediaType;
                    resource.IsMediaTypeValid = resource.MediaType.Length == 0
                        || OdfMediaTypeRules.ParseMediaType(
                            resource.MediaType).IsValid;
                    resource.Kind = ResourceKind(
                        resource.Node,
                        resource.MediaType);
                }
                return;
            }
            index = index + 1;
        }
    }

    static bool ContentAvailable(OdfDocument document, string path)
    {
        if (document.Package == null || document.Package.EntryIndex == null)
        {
            return false;
        }
        EditablePackageEntry[] entries =
            document.Package.EntryIndex.GetEditableEntries();
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index].Entry.Path == path)
            {
                return entries[index].IsContentAvailable;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfMediaResourceKind ResourceKind(
        OdfNode? node,
        string mediaType)
    {
        string localName = node == null || node.Name == null
            ? ""
            : node.Name.LocalName;
        if (localName == "applet")
        {
            return OdfMediaResourceKind.Applet;
        }
        if (localName == "floating-frame")
        {
            return OdfMediaResourceKind.FloatingFrame;
        }
        if (localName == "plugin")
        {
            string normalized = mediaType.Trim();
            if (normalized.StartsWith(
                "audio/",
                global::System.StringComparison.OrdinalIgnoreCase))
            {
                return OdfMediaResourceKind.Audio;
            }
            if (normalized.StartsWith(
                "video/",
                global::System.StringComparison.OrdinalIgnoreCase))
            {
                return OdfMediaResourceKind.Video;
            }
            return OdfMediaResourceKind.Plugin;
        }
        return OdfMediaResourceKind.Unknown;
    }

    static OdfMediaParameter[] CreateParameters(OdfNode node)
    {
        List<OdfMediaParameter> result = new List<OdfMediaParameter>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsElement(child, OdfNamespaces.Drawing, "param"))
            {
                OdfMediaParameter parameter = new OdfMediaParameter();
                parameter.Name = Read(
                    child,
                    OdfNamespaces.Drawing,
                    "name");
                parameter.Value = Read(
                    child,
                    OdfNamespaces.Drawing,
                    "value");
                parameter.Node = child;
                parameter.OriginalName = parameter.Name;
                parameter.OriginalValue = parameter.Value;
                result.Add(parameter);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void SynchronizeResource(OdfMediaResource resource)
    {
        OdfNode node = resource.Node ?? new OdfNode();
        WriteChanged(node, OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix, resource.Href, resource.OriginalHref);
        WriteChanged(node, OdfNamespaces.Drawing, "mime-type", OdfNamespaces.DrawingPrefix, resource.MediaType, resource.OriginalMediaType);
        WriteChanged(node, OdfNamespaces.XLink, "type", OdfNamespaces.XLinkPrefix, resource.XLinkType, resource.OriginalXLinkType);
        WriteChanged(node, OdfNamespaces.XLink, "show", OdfNamespaces.XLinkPrefix, resource.Show, resource.OriginalShow);
        WriteChanged(node, OdfNamespaces.XLink, "actuate", OdfNamespaces.XLinkPrefix, resource.Actuate, resource.OriginalActuate);
        WriteChanged(node, OdfNamespaces.Drawing, "code", OdfNamespaces.DrawingPrefix, resource.Code, resource.OriginalCode);
        WriteChanged(node, OdfNamespaces.Drawing, "object", OdfNamespaces.DrawingPrefix, resource.ObjectName, resource.OriginalObjectName);
        WriteChanged(node, OdfNamespaces.Drawing, "archive", OdfNamespaces.DrawingPrefix, resource.Archive, resource.OriginalArchive);
        WriteMayScript(resource, node);
        WriteChanged(node, OdfNamespaces.Drawing, "frame-name", OdfNamespaces.DrawingPrefix, resource.FrameName, resource.OriginalFrameName);
        SynchronizeParameters(resource, node);
    }

    static void WriteMayScript(
        OdfMediaResource resource,
        OdfNode node)
    {
        string value = resource.MayScriptValue;
        OdfLexicalValue original = OdfLexicalParser.ParseBoolean(
            resource.OriginalMayScriptValue);
        bool originalBoolean = original.IsValid && original.BooleanValue;
        if (resource.MayScript != originalBoolean
            && value == resource.OriginalMayScriptValue)
        {
            value = resource.MayScript ? "true" : "false";
            resource.MayScriptValue = value;
        }
        WriteChanged(
            node,
            OdfNamespaces.Drawing,
            "may-script",
            OdfNamespaces.DrawingPrefix,
            value,
            resource.OriginalMayScriptValue);
    }

    static void SynchronizeParameters(
        OdfMediaResource resource,
        OdfNode node)
    {
        int index = 0;
        while (index < resource.OriginalParameterNodes.Length)
        {
            OdfNode original = resource.OriginalParameterNodes[index];
            if (!ContainsParameterNode(resource.Parameters, original))
            {
                RemoveChild(node, original);
            }
            index = index + 1;
        }
        index = 0;
        while (index < resource.Parameters.Length)
        {
            OdfMediaParameter parameter = resource.Parameters[index];
            if (parameter == null)
            {
                throw new OdfEditException(
                    "A media parameter collection cannot contain null values.");
            }
            if (parameter.Node == null)
            {
                parameter.Node = CreateParameterNode(node.Location.PartPath);
                AppendChild(node, parameter.Node);
            }
            WriteChanged(parameter.Node, OdfNamespaces.Drawing, "name", OdfNamespaces.DrawingPrefix, parameter.Name, parameter.OriginalName);
            WriteChanged(parameter.Node, OdfNamespaces.Drawing, "value", OdfNamespaces.DrawingPrefix, parameter.Value, parameter.OriginalValue);
            parameter.OriginalName = parameter.Name;
            parameter.OriginalValue = parameter.Value;
            index = index + 1;
        }
    }

    static bool ContainsParameterNode(
        OdfMediaParameter[] parameters,
        OdfNode node)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            if (parameters[index] != null && parameters[index].Node == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfNode CreateParameterNode(string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = new OdfQualifiedName();
        result.Name.NamespaceName = OdfNamespaces.Drawing;
        result.Name.LocalName = "param";
        result.Name.Prefix = OdfNamespaces.DrawingPrefix;
        result.Location.PartPath = partPath;
        result.WasEmptyElement = true;
        return result;
    }

    static void RefreshMayScript(OdfMediaResource resource)
    {
        OdfLexicalValue parsed = OdfLexicalParser.ParseBoolean(
            resource.MayScriptValue);
        resource.IsMayScriptValid = resource.MayScriptValue.Length == 0
            || parsed.IsValid;
        resource.MayScript = parsed.IsValid && parsed.BooleanValue;
    }

    static string ResourcePath(string reference)
    {
        int question = reference.IndexOf('?');
        int fragment = reference.IndexOf('#');
        int end = reference.Length;
        if (question >= 0 && question < end)
        {
            end = question;
        }
        if (fragment >= 0 && fragment < end)
        {
            end = fragment;
        }
        return reference.Substring(0, end);
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

    static void AppendChild(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        global::System.Array.Copy(
            parent.Children,
            children,
            parent.Children.Length);
        children[children.Length - 1] = child;
        child.Parent = parent;
        parent.Children = children;
        parent.WasEmptyElement = false;
    }

    static void RemoveChild(OdfNode parent, OdfNode child)
    {
        List<OdfNode> children = new List<OdfNode>();
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (parent.Children[index] != child)
            {
                children.Add(parent.Children[index]);
            }
            index = index + 1;
        }
        child.Parent = null;
        parent.Children = children.ToArray();
    }

    static void WriteChanged(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
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

    static void CaptureOriginalValues(OdfMediaResource resource)
    {
        resource.OriginalHref = resource.Href;
        resource.OriginalMediaType = resource.MediaType;
        resource.OriginalXLinkType = resource.XLinkType;
        resource.OriginalShow = resource.Show;
        resource.OriginalActuate = resource.Actuate;
        resource.OriginalCode = resource.Code;
        resource.OriginalObjectName = resource.ObjectName;
        resource.OriginalArchive = resource.Archive;
        resource.OriginalMayScriptValue = resource.MayScriptValue;
        resource.OriginalFrameName = resource.FrameName;
        resource.OriginalParameterNodes = ParameterNodes(resource.Parameters);
    }

    static OdfNode[] ParameterNodes(OdfMediaParameter[] parameters)
    {
        List<OdfNode> result = new List<OdfNode>();
        int index = 0;
        while (index < parameters.Length)
        {
            if (parameters[index] != null && parameters[index].Node != null)
            {
                result.Add(parameters[index].Node ?? new OdfNode());
            }
            index = index + 1;
        }
        return result.ToArray();
    }
}
