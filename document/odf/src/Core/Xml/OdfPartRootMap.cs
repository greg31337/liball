using System;
using System.Collections.Generic;

namespace odf;

internal class OdfPartRootMap
{
    internal OdfNode Root = new OdfNode();
    internal string PartPath = "";
    internal string RootName = "";
    internal bool IsFlatDocument;
    internal OdfNode? Scripts;
    internal OdfNode? FontFaceDeclarations;
    internal OdfNode? Styles;
    internal OdfNode? AutomaticStyles;
    internal OdfNode? MasterStyles;
    internal OdfNode? Metadata;
    internal OdfNode? Settings;
    internal OdfNode? Body;
    internal OdfNode[] Sections = [];
    internal OdfNode[] StandardSections = [];
    internal OdfNode[] UnmappedSections = [];
}

internal static class OdfPartRootRules
{
    const string Document = "document";
    const string DocumentContent = "document-content";
    const string DocumentStyles = "document-styles";
    const string DocumentMeta = "document-meta";
    const string DocumentSettings = "document-settings";

    internal static OdfPartRootMap MapRoot(
        OdfNode[] nodes,
        string partPath)
    {
        OdfNode root = FindRoot(nodes, partPath);
        if (IsFormulaContentRoot(root, partPath))
        {
            OdfPartRootMap formulaResult = new OdfPartRootMap();
            formulaResult.Root = root;
            formulaResult.PartPath = partPath;
            formulaResult.RootName = "math";
            return formulaResult;
        }
        if (root.Name == null
            || root.Name.NamespaceName != OdfNamespaces.Office
            || !IsStandardRootName(root.Name.LocalName))
        {
            throw new OdfXmlException(
                "The XML part does not have a standard OpenDocument office root.");
        }

        string rootName = root.Name.LocalName;
        string expectedRoot = ExpectedRootName(partPath);
        if (expectedRoot.Length > 0 && expectedRoot != rootName)
        {
            throw new OdfXmlException(
                "The XML root office:"
                + rootName
                + " does not match package part '"
                + partPath
                + "'.");
        }

        OdfPartRootMap result = new OdfPartRootMap();
        result.Root = root;
        result.PartPath = partPath;
        result.RootName = rootName;
        result.IsFlatDocument = rootName == Document;
        MapSections(result);
        return result;
    }

    static bool IsFormulaContentRoot(OdfNode root, string partPath)
    {
        return root.Name != null
            && root.Name.NamespaceName == OdfNamespaces.Math
            && root.Name.LocalName == "math"
            && IsNamedPart(partPath, OdfPartNames.Content);
    }

    static OdfNode FindRoot(OdfNode[] nodes, string partPath)
    {
        OdfNode? root = null;
        foreach (OdfNode node in nodes)
        {
            if (node.Kind == OdfNodeKind.Element)
            {
                if (root != null)
                {
                    throw new OdfXmlException(
                        "XML part '"
                        + partPath
                        + "' contains more than one top-level element.");
                }
                root = node;
            }
        }
        if (root == null)
        {
            throw new OdfXmlException(
                "XML part '"
                + partPath
                + "' does not contain a document element.");
        }
        return root;
    }

    static void MapSections(OdfPartRootMap result)
    {
        List<OdfNode> sections = new List<OdfNode>();
        List<OdfNode> standard = new List<OdfNode>();
        List<OdfNode> unmapped = new List<OdfNode>();
        foreach (OdfNode child in result.Root.Children)
        {
            if (child.Kind == OdfNodeKind.Element)
            {
                sections.Add(child);
                if (MapStandardSection(result, child))
                {
                    standard.Add(child);
                }
                else
                {
                    unmapped.Add(child);
                }
            }
        }
        result.Sections = sections.ToArray();
        result.StandardSections = standard.ToArray();
        result.UnmappedSections = unmapped.ToArray();
    }

    static bool MapStandardSection(
        OdfPartRootMap result,
        OdfNode section)
    {
        if (section.Name == null
            || section.Name.NamespaceName != OdfNamespaces.Office
            || !IsAllowedSection(result.RootName, section.Name.LocalName))
        {
            return false;
        }

        string name = section.Name.LocalName;
        if (name == "scripts")
        {
            if (result.Scripts == null)
            {
                result.Scripts = section;
            }
        }
        else if (name == "font-face-decls")
        {
            if (result.FontFaceDeclarations == null)
            {
                result.FontFaceDeclarations = section;
            }
        }
        else if (name == "styles")
        {
            if (result.Styles == null)
            {
                result.Styles = section;
            }
        }
        else if (name == "automatic-styles")
        {
            if (result.AutomaticStyles == null)
            {
                result.AutomaticStyles = section;
            }
        }
        else if (name == "master-styles")
        {
            if (result.MasterStyles == null)
            {
                result.MasterStyles = section;
            }
        }
        else if (name == "meta")
        {
            if (result.Metadata == null)
            {
                result.Metadata = section;
            }
        }
        else if (name == "settings")
        {
            if (result.Settings == null)
            {
                result.Settings = section;
            }
        }
        else if (name == "body")
        {
            if (result.Body == null)
            {
                result.Body = section;
            }
        }
        return true;
    }

    static bool IsAllowedSection(string rootName, string sectionName)
    {
        if (rootName == Document)
        {
            return IsStandardSectionName(sectionName);
        }
        if (rootName == DocumentContent)
        {
            return sectionName == "scripts"
                || sectionName == "font-face-decls"
                || sectionName == "automatic-styles"
                || sectionName == "body";
        }
        if (rootName == DocumentStyles)
        {
            return sectionName == "font-face-decls"
                || sectionName == "styles"
                || sectionName == "automatic-styles"
                || sectionName == "master-styles";
        }
        if (rootName == DocumentMeta)
        {
            return sectionName == "meta";
        }
        return rootName == DocumentSettings && sectionName == "settings";
    }

    static bool IsStandardSectionName(string name)
    {
        return name == "scripts"
            || name == "font-face-decls"
            || name == "styles"
            || name == "automatic-styles"
            || name == "master-styles"
            || name == "meta"
            || name == "settings"
            || name == "body";
    }

    static bool IsStandardRootName(string name)
    {
        return name == Document
            || name == DocumentContent
            || name == DocumentStyles
            || name == DocumentMeta
            || name == DocumentSettings;
    }

    static string ExpectedRootName(string partPath)
    {
        if (IsNamedPart(partPath, OdfPartNames.Content))
        {
            return DocumentContent;
        }
        if (IsNamedPart(partPath, OdfPartNames.Styles))
        {
            return DocumentStyles;
        }
        if (IsNamedPart(partPath, OdfPartNames.Metadata))
        {
            return DocumentMeta;
        }
        if (IsNamedPart(partPath, OdfPartNames.Settings))
        {
            return DocumentSettings;
        }
        return "";
    }

    static bool IsNamedPart(string partPath, string name)
    {
        return partPath == name
            || partPath.EndsWith("/" + name, StringComparison.Ordinal);
    }
}
