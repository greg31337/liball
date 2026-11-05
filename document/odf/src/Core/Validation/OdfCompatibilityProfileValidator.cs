using System.Collections.Generic;

namespace odf;

internal class OdfCompatibilityProfileValidator
{
    string Profile;
    OdfConformanceClass ConformanceClass;
    List<OdfDiagnostic> Diagnostics = new List<OdfDiagnostic>();
    CompatibilityProfilePolicy Policy = new CompatibilityProfilePolicy();
    OdfVersion Version;

    internal OdfCompatibilityProfileValidator(
        string profile,
        OdfConformanceClass conformanceClass)
    {
        Profile = profile;
        ConformanceClass = conformanceClass;
    }

    internal OdfDiagnostic[] ValidateDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfValidationException(
                "A document is required for compatibility-profile validation.");
        }
        Diagnostics = new List<OdfDiagnostic>();
        string selected = Profile;
        if (selected.Length == 0)
        {
            selected = OdfVersionRules.StandardProfile;
        }
        Policy = OdfCompatibilityProfileRules.Resolve(selected);
        Version = SelectVersion(document);
        if (!Policy.IsValid)
        {
            AddDiagnostic(
                "The selected compatibility profile is not recognized.",
                null);
            return Diagnostics.ToArray();
        }
        if (ConformanceClass != OdfConformanceClass.Unknown
            && Policy.ConformanceClass != OdfConformanceClass.Unknown
            && ConformanceClass != Policy.ConformanceClass)
        {
            AddDiagnostic(
                "The selected conformance class conflicts with the compatibility profile.",
                null);
        }
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            ValidatePart(document.Parts[partIndex]);
            partIndex = partIndex + 1;
        }
        if (document.Manifest != null)
        {
            ValidateManifestNode(
                document.Manifest.Root,
                "/manifest:manifest[1]");
        }
        return Diagnostics.ToArray();
    }

    void ValidateManifestNode(
        OdfManifestNode node,
        string nodePath)
    {
        if (!OdfCompatibilityProfileRules.AllowsNamespace(
            Policy,
            node.Name.NamespaceName))
        {
            OdfLocation location = new OdfLocation();
            location.PartPath = OdfPartNames.Manifest;
            location.NodePath = nodePath;
            AddDiagnostic(
                "The manifest element namespace is not permitted by compatibility profile '"
                    + Policy.Identifier + "'.",
                location);
            return;
        }
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[attributeIndex];
            if (attribute.Name.NamespaceName
                    != "http://www.w3.org/2000/xmlns/"
                && !OdfCompatibilityProfileRules.AllowsNamespace(
                Policy,
                attribute.Name.NamespaceName))
            {
                OdfLocation location = new OdfLocation();
                location.PartPath = OdfPartNames.Manifest;
                location.NodePath = nodePath + "/@"
                    + QualifiedName(attribute.Name);
                AddDiagnostic(
                    "The manifest attribute namespace is not permitted by compatibility profile '"
                        + Policy.Identifier + "'.",
                    location);
            }
            attributeIndex = attributeIndex + 1;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            OdfManifestNode child = node.Children[childIndex];
            string childPath = nodePath + "/"
                + QualifiedName(child.Name) + "["
                + (childIndex + 1) + "]";
            ValidateManifestNode(child, childPath);
            childIndex = childIndex + 1;
        }
    }

    void ValidatePart(OdfPart part)
    {
        int index = 0;
        while (index < part.Nodes.Length)
        {
            OdfNode node = part.Nodes[index];
            string path = "/" + NodeName(node) + "["
                + (index + 1) + "]";
            ValidateNode(node, part.Path, path);
            index = index + 1;
        }
    }

    void ValidateNode(
        OdfNode node,
        string partPath,
        string nodePath)
    {
        if (node.Name != null
            && !OdfCompatibilityProfileRules.AllowsNamespace(
                Policy,
                node.Name.NamespaceName))
        {
            AddDiagnostic(
                "The element namespace is not permitted by compatibility profile '"
                    + Policy.Identifier + "'.",
                NodeLocation(node, partPath, nodePath));
            return;
        }
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[attributeIndex];
            if (!OdfCompatibilityProfileRules.AllowsNamespace(
                Policy,
                attribute.Name.NamespaceName))
            {
                OdfLocation location = OdfDiagnosticFactory.CopyLocation(
                    attribute.Location);
                location.PartPath = partPath;
                location.NodePath = nodePath + "/@"
                    + QualifiedName(attribute.Name);
                AddDiagnostic(
                    "The attribute namespace is not permitted by compatibility profile '"
                        + Policy.Identifier + "'.",
                    location);
            }
            attributeIndex = attributeIndex + 1;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            OdfNode child = node.Children[childIndex];
            string childPath = nodePath + "/" + NodeName(child) + "["
                + (childIndex + 1) + "]";
            ValidateNode(child, partPath, childPath);
            childIndex = childIndex + 1;
        }
    }

    void AddDiagnostic(string message, OdfLocation? location)
    {
        Diagnostics.Add(OdfDiagnosticFactory.Create(
            OdfDiagnosticCodes.FeatureNotAvailable,
            message,
            OdfValidationSeverity.Error,
            location,
            null,
            Version));
    }

    OdfLocation NodeLocation(
        OdfNode node,
        string partPath,
        string nodePath)
    {
        OdfLocation result = OdfDiagnosticFactory.CopyLocation(node.Location);
        result.PartPath = partPath;
        result.NodePath = nodePath;
        result.XmlId = node.XmlId;
        return result;
    }

    string NodeName(OdfNode node)
    {
        if (node.Name == null)
        {
            return "node";
        }
        return QualifiedName(node.Name);
    }

    string QualifiedName(OdfQualifiedName name)
    {
        if (name.Prefix.Length == 0)
        {
            return name.LocalName;
        }
        return name.Prefix + ":" + name.LocalName;
    }

    OdfVersion SelectVersion(OdfDocument document)
    {
        if (document.VersionInfo != null)
        {
            return document.VersionInfo.Version;
        }
        return OdfVersion.Unknown;
    }
}
