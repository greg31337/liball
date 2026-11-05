using System;
using System.Collections.Generic;

namespace odf;

internal class StyleResolutionResult
{
    internal bool IsValid;
    internal OdfStyle? Style;
    internal OdfStyle[] Sources = [];
    internal OdfNode[] EffectiveProperties = [];
    internal OdfDiagnostic[] Diagnostics = [];
}

internal static class StyleResolver
{
    internal static void ResolveRelationships(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for style relationship resolution.");
        }
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            style.ParentStyle = FindNamedStyle(
                document,
                style.ParentStyleName,
                style);
            style.NextStyle = FindNamedStyle(
                document,
                style.NextStyleName,
                style);
            style.DefaultStyle = FindDefaultStyle(document, style);
            style.ListStyleNode = FindRelatedDefinition(
                document,
                style.ListStyleName,
                "list");
            style.MasterPageNode = FindRelatedDefinition(
                document,
                style.MasterPageName,
                "master");
            style.DataStyleNode = FindRelatedDefinition(
                document,
                style.DataStyleName,
                "data");
            style.HasParentCycle = false;
            style.HasNextCycle = false;
            index = index + 1;
        }
        index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            style.HasParentCycle = DetectParentCycle(style);
            style.HasNextCycle = DetectNextCycle(style);
            index = index + 1;
        }
    }

    internal static OdfDiagnostic[] ValidateRelationships(
        OdfDocument document)
    {
        ResolveRelationships(document);
        List<OdfDiagnostic> diagnostics = new List<OdfDiagnostic>();
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.HasParentCycle)
            {
                diagnostics.Add(CreateCycleDiagnostic(document, style));
            }
            index = index + 1;
        }
        return diagnostics.ToArray();
    }

    internal static StyleResolutionResult ResolveEffectiveProperties(
        OdfDocument document,
        OdfStyle style)
    {
        if (document == null || style == null)
        {
            throw new OdfDocumentException(
                "A document and style are required for effective property resolution.");
        }
        ResolveRelationships(document);
        StyleResolutionResult result = new StyleResolutionResult();
        result.Style = style;
        List<OdfDiagnostic> diagnostics = new List<OdfDiagnostic>();
        if (style.HasParentCycle)
        {
            diagnostics.Add(CreateCycleDiagnostic(document, style));
        }
        result.Sources = BuildInheritanceSources(style);
        result.EffectiveProperties = MergeSourceProperties(result.Sources);
        result.Diagnostics = diagnostics.ToArray();
        result.IsValid = diagnostics.Count == 0;
        return result;
    }

    static OdfStyle? FindNamedStyle(
        OdfDocument document,
        string name,
        OdfStyle target)
    {
        if (name.Length == 0)
        {
            return null;
        }
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.Name == name
                && FamiliesMatch(style, target)
                && style.IsCommon
                && !style.IsDefault)
            {
                return style;
            }
            index = index + 1;
        }
        index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.Name == name
                && FamiliesMatch(style, target)
                && !style.IsDefault)
            {
                return style;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfStyle? FindDefaultStyle(
        OdfDocument document,
        OdfStyle target)
    {
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.IsDefault && FamiliesMatch(style, target))
            {
                return style;
            }
            index = index + 1;
        }
        return null;
    }

    static bool FamiliesMatch(OdfStyle first, OdfStyle second)
    {
        if (first.Family != second.Family)
        {
            return false;
        }
        return first.Family != OdfStyleFamily.Unknown
            || first.FamilyName == second.FamilyName;
    }

    static OdfNode? FindRelatedDefinition(
        OdfDocument document,
        string name,
        string relationship)
    {
        if (name.Length == 0)
        {
            return null;
        }
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode[] nodes = new OdfTreeEditor(
                document.Parts[partIndex].Nodes).TraverseAllNodes();
            int nodeIndex = 0;
            while (nodeIndex < nodes.Length)
            {
                OdfNode node = nodes[nodeIndex];
                if (RelationshipNodeMatches(node, name, relationship))
                {
                    return node;
                }
                nodeIndex = nodeIndex + 1;
            }
            partIndex = partIndex + 1;
        }
        return null;
    }

    static bool RelationshipNodeMatches(
        OdfNode node,
        string name,
        string relationship)
    {
        if (node.Kind != OdfNodeKind.Element || node.Name == null)
        {
            return false;
        }
        string nodeName = ReadStyleName(node);
        if (nodeName != name)
        {
            return false;
        }
        if (relationship == "list")
        {
            return node.Name.NamespaceName == OdfNamespaces.Text
                && (node.Name.LocalName == "list-style"
                    || node.Name.LocalName == "outline-style");
        }
        if (relationship == "master")
        {
            return node.Name.NamespaceName == OdfNamespaces.Style
                && node.Name.LocalName == "master-page";
        }
        if (relationship == "data")
        {
            return node.Name.NamespaceName == OdfNamespaces.Number
                && node.Name.LocalName.EndsWith(
                    "-style",
                    StringComparison.Ordinal);
        }
        return false;
    }

    static string ReadStyleName(OdfNode node)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.Style
                && attribute.Name.LocalName == "name")
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        return "";
    }

    static bool DetectParentCycle(OdfStyle style)
    {
        HashSet<OdfStyle> visited = new HashSet<OdfStyle>();
        OdfStyle? current = style;
        while (current != null)
        {
            if (visited.Contains(current))
            {
                return true;
            }
            visited.Add(current);
            current = current.ParentStyle;
        }
        return false;
    }

    static bool DetectNextCycle(OdfStyle style)
    {
        HashSet<OdfStyle> visited = new HashSet<OdfStyle>();
        OdfStyle? current = style;
        while (current != null)
        {
            if (visited.Contains(current))
            {
                return true;
            }
            visited.Add(current);
            current = current.NextStyle;
        }
        return false;
    }

    static OdfStyle[] BuildInheritanceSources(OdfStyle style)
    {
        List<OdfStyle> sources = new List<OdfStyle>();
        if (style.DefaultStyle != null && style.DefaultStyle != style)
        {
            sources.Add(style.DefaultStyle);
        }
        List<OdfStyle> parents = new List<OdfStyle>();
        HashSet<OdfStyle> visited = new HashSet<OdfStyle>();
        OdfStyle? current = style.ParentStyle;
        while (current != null && !visited.Contains(current))
        {
            visited.Add(current);
            parents.Add(current);
            current = current.ParentStyle;
        }
        int index = parents.Count - 1;
        while (index >= 0)
        {
            if (!sources.Contains(parents[index]))
            {
                sources.Add(parents[index]);
            }
            index = index - 1;
        }
        if (!sources.Contains(style))
        {
            sources.Add(style);
        }
        return sources.ToArray();
    }

    static OdfNode[] MergeSourceProperties(OdfStyle[] sources)
    {
        List<OdfNode> result = new List<OdfNode>();
        int sourceIndex = 0;
        while (sourceIndex < sources.Length)
        {
            OdfStyle style = sources[sourceIndex];
            if (style.Node != null)
            {
                int nodeIndex = 0;
                while (nodeIndex < style.Node.Children.Length)
                {
                    OdfNode property = style.Node.Children[nodeIndex];
                    if (IsPropertyElement(property))
                    {
                        OdfNode? existing = FindEffectiveProperty(
                            result,
                            property);
                        if (existing == null)
                        {
                            result.Add(CloneNode(property));
                        }
                        else
                        {
                            MergePropertyNode(existing, property);
                        }
                    }
                    nodeIndex = nodeIndex + 1;
                }
            }
            sourceIndex = sourceIndex + 1;
        }
        return result.ToArray();
    }

    static bool IsPropertyElement(OdfNode node)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Style
            && node.Name.LocalName.EndsWith(
                "-properties",
                StringComparison.Ordinal);
    }

    static OdfNode? FindEffectiveProperty(
        List<OdfNode> properties,
        OdfNode source)
    {
        if (source.Name == null)
        {
            return null;
        }
        int index = 0;
        while (index < properties.Count)
        {
            OdfNode property = properties[index];
            if (property.Name != null
                && property.Name.NamespaceName == source.Name.NamespaceName
                && property.Name.LocalName == source.Name.LocalName)
            {
                return property;
            }
            index = index + 1;
        }
        return null;
    }

    static void MergePropertyNode(OdfNode target, OdfNode source)
    {
        int declarationIndex = 0;
        while (declarationIndex < source.NamespaceDeclarations.Length)
        {
            MergeNamespaceDeclaration(
                target,
                source.NamespaceDeclarations[declarationIndex]);
            declarationIndex = declarationIndex + 1;
        }
        int attributeIndex = 0;
        while (attributeIndex < source.Attributes.Length)
        {
            MergePropertyAttribute(target, source.Attributes[attributeIndex]);
            attributeIndex = attributeIndex + 1;
        }
        int childIndex = 0;
        while (childIndex < source.Children.Length)
        {
            OdfNode child = source.Children[childIndex];
            if (child.Kind == OdfNodeKind.Element && child.Name != null)
            {
                ReplaceOrAppendPropertyChild(target, child);
            }
            childIndex = childIndex + 1;
        }
    }

    static void MergeNamespaceDeclaration(
        OdfNode target,
        OdfNamespaceDeclaration source)
    {
        int index = 0;
        while (index < target.NamespaceDeclarations.Length)
        {
            OdfNamespaceDeclaration declaration =
                target.NamespaceDeclarations[index];
            if (declaration.Prefix == source.Prefix)
            {
                declaration.NamespaceName = source.NamespaceName;
                return;
            }
            index = index + 1;
        }
        OdfNamespaceDeclaration[] declarations =
            new OdfNamespaceDeclaration[
                target.NamespaceDeclarations.Length + 1];
        Array.Copy(
            target.NamespaceDeclarations,
            declarations,
            target.NamespaceDeclarations.Length);
        OdfNamespaceDeclaration clone = new OdfNamespaceDeclaration();
        clone.Prefix = source.Prefix;
        clone.NamespaceName = source.NamespaceName;
        clone.Location = OdfDiagnosticFactory.CopyLocation(source.Location);
        declarations[declarations.Length - 1] = clone;
        target.NamespaceDeclarations = declarations;
    }

    static void MergePropertyAttribute(
        OdfNode target,
        OdfAttribute source)
    {
        int index = 0;
        while (index < target.Attributes.Length)
        {
            OdfAttribute attribute = target.Attributes[index];
            if (attribute.Name.NamespaceName == source.Name.NamespaceName
                && attribute.Name.LocalName == source.Name.LocalName)
            {
                attribute.Name.Prefix = source.Name.Prefix;
                attribute.Value = source.Value;
                attribute.Location = OdfDiagnosticFactory.CopyLocation(
                    source.Location);
                return;
            }
            index = index + 1;
        }
        OdfAttribute[] attributes = new OdfAttribute[
            target.Attributes.Length + 1];
        Array.Copy(target.Attributes, attributes, target.Attributes.Length);
        attributes[attributes.Length - 1] = CloneAttribute(source);
        target.Attributes = attributes;
    }

    static void ReplaceOrAppendPropertyChild(
        OdfNode target,
        OdfNode source)
    {
        int index = 0;
        while (index < target.Children.Length)
        {
            OdfNode child = target.Children[index];
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && source.Name != null
                && child.Name.NamespaceName == source.Name.NamespaceName
                && child.Name.LocalName == source.Name.LocalName)
            {
                OdfNode replacement = CloneNode(source);
                replacement.Parent = target;
                target.Children[index] = replacement;
                return;
            }
            index = index + 1;
        }
        OdfNode[] children = new OdfNode[target.Children.Length + 1];
        Array.Copy(target.Children, children, target.Children.Length);
        OdfNode clone = CloneNode(source);
        clone.Parent = target;
        children[children.Length - 1] = clone;
        target.Children = children;
    }

    static OdfNode CloneNode(OdfNode node)
    {
        return new OdfTreeEditor([]).CloneSubtree(node);
    }

    static OdfAttribute CloneAttribute(OdfAttribute source)
    {
        OdfAttribute result = new OdfAttribute();
        result.Name.NamespaceName = source.Name.NamespaceName;
        result.Name.LocalName = source.Name.LocalName;
        result.Name.Prefix = source.Name.Prefix;
        result.Value = source.Value;
        result.Location = OdfDiagnosticFactory.CopyLocation(source.Location);
        return result;
    }

    static OdfDiagnostic CreateCycleDiagnostic(
        OdfDocument document,
        OdfStyle style)
    {
        OdfLocation location = new OdfLocation();
        if (style.Node != null)
        {
            location = OdfDiagnosticFactory.CopyLocation(style.Node.Location);
        }
        location.PartPath = style.PartPath;
        OdfVersion version = OdfVersion.Unknown;
        if (document.VersionInfo != null)
        {
            version = document.VersionInfo.Version;
        }
        return OdfDiagnosticFactory.Create(
            OdfDiagnosticCodes.InvalidStyleReference,
            "The parent-style relationship for style '"
                + style.Name
                + "' contains an inheritance cycle.",
            OdfValidationSeverity.Error,
            location,
            null,
            version);
    }
}
