using System;
using System.Collections.Generic;

namespace odf;

internal class OdfNamespaceScope
{
    static readonly string[] KnownNamespaceNames =
    [
        OdfNamespaces.Animation,
        OdfNamespaces.Chart,
        OdfNamespaces.Config,
        OdfNamespaces.Database,
        OdfNamespaces.DublinCore,
        OdfNamespaces.Dr3d,
        OdfNamespaces.Drawing,
        OdfNamespaces.Fo,
        OdfNamespaces.Form,
        OdfNamespaces.Grddl,
        OdfNamespaces.Math,
        OdfNamespaces.Metadata,
        OdfNamespaces.Number,
        OdfNamespaces.Office,
        OdfNamespaces.Presentation,
        OdfNamespaces.Script,
        OdfNamespaces.Smil,
        OdfNamespaces.Style,
        OdfNamespaces.Svg,
        OdfNamespaces.Table,
        OdfNamespaces.Text,
        OdfNamespaces.Manifest,
        OdfNamespaces.DigitalSignature,
        OdfNamespaces.OpenFormula,
        OdfNamespaces.LegacyFormula,
        OdfNamespaces.XForms,
        OdfNamespaces.Xhtml,
        OdfNamespaces.XLink,
        OdfNamespaces.Xml,
        OdfNamespaces.XmlDigitalSignature,
        OdfNamespaces.XmlEncryption,
        OdfNamespaces.Xades,
        OdfNamespaces.Rdf,
        OdfNamespaces.Rdfs,
        OdfNamespaces.Owl,
        OdfNamespaces.OdfMetadata,
        OdfNamespaces.PackageMetadata
    ];
    static readonly string[] KnownPrefixes =
    [
        OdfNamespaces.AnimationPrefix,
        OdfNamespaces.ChartPrefix,
        OdfNamespaces.ConfigPrefix,
        OdfNamespaces.DatabasePrefix,
        OdfNamespaces.DublinCorePrefix,
        OdfNamespaces.Dr3dPrefix,
        OdfNamespaces.DrawingPrefix,
        OdfNamespaces.FoPrefix,
        OdfNamespaces.FormPrefix,
        OdfNamespaces.GrddlPrefix,
        OdfNamespaces.MathPrefix,
        OdfNamespaces.MetadataPrefix,
        OdfNamespaces.NumberPrefix,
        OdfNamespaces.OfficePrefix,
        OdfNamespaces.PresentationPrefix,
        OdfNamespaces.ScriptPrefix,
        OdfNamespaces.SmilPrefix,
        OdfNamespaces.StylePrefix,
        OdfNamespaces.SvgPrefix,
        OdfNamespaces.TablePrefix,
        OdfNamespaces.TextPrefix,
        OdfNamespaces.ManifestPrefix,
        OdfNamespaces.DigitalSignaturePrefix,
        OdfNamespaces.OpenFormulaPrefix,
        OdfNamespaces.LegacyFormulaPrefix,
        OdfNamespaces.XFormsPrefix,
        OdfNamespaces.XhtmlPrefix,
        OdfNamespaces.XLinkPrefix,
        OdfNamespaces.XmlPrefix,
        OdfNamespaces.XmlDigitalSignaturePrefix,
        OdfNamespaces.XmlEncryptionPrefix,
        OdfNamespaces.XadesPrefix,
        OdfNamespaces.RdfPrefix,
        OdfNamespaces.RdfsPrefix,
        OdfNamespaces.OwlPrefix,
        OdfNamespaces.OdfMetadataPrefix,
        OdfNamespaces.PackageMetadataPrefix
    ];

    Dictionary<string, string> bindings;
    List<string> prefixOrder;
    HashSet<string> localPrefixes;
    List<OdfNamespaceDeclaration> declarations;
    internal OdfNamespaceScope? Parent;
    internal bool PreservePrefixes;

    internal OdfNamespaceScope(
        OdfNamespaceScope? parent,
        bool preservePrefixes)
    {
        Parent = parent;
        PreservePrefixes = preservePrefixes;
        bindings = new Dictionary<string, string>(StringComparer.Ordinal);
        prefixOrder = new List<string>();
        localPrefixes = new HashSet<string>(StringComparer.Ordinal);
        declarations = new List<OdfNamespaceDeclaration>();
        if (parent == null)
        {
            bindings.Add(OdfNamespaces.XmlPrefix, OdfNamespaces.Xml);
            prefixOrder.Add(OdfNamespaces.XmlPrefix);
        }
        else
        {
            foreach (string prefix in parent.prefixOrder)
            {
                prefixOrder.Add(prefix);
                bindings.Add(prefix, parent.bindings[prefix]);
            }
        }
    }

    internal void AddDeclaration(OdfNamespaceDeclaration? declaration)
    {
        if (declaration == null
            || !OdfNameRules.IsNamespaceDeclarationValue(declaration))
        {
            throw new OdfXmlException("The XML namespace declaration is invalid or uses a reserved binding.");
        }
        if (localPrefixes.Contains(declaration.Prefix))
        {
            throw new OdfXmlException("An XML namespace prefix is declared more than once in the same scope.");
        }

        if (!bindings.ContainsKey(declaration.Prefix))
        {
            prefixOrder.Add(declaration.Prefix);
            bindings.Add(declaration.Prefix, declaration.NamespaceName);
        }
        else
        {
            bindings[declaration.Prefix] = declaration.NamespaceName;
        }
        localPrefixes.Add(declaration.Prefix);
        declarations.Add(new OdfNamespaceDeclaration
        {
            Prefix = declaration.Prefix,
            NamespaceName = declaration.NamespaceName
        });
    }

    internal string? LookupNamespace(string prefix)
    {
        if (!IsLookupPrefix(prefix))
        {
            throw new OdfXmlException("The XML namespace prefix is invalid or reserved.");
        }
        if (bindings.ContainsKey(prefix))
        {
            return bindings[prefix];
        }
        return null;
    }

    internal string? LookupPrefix(
        string namespaceName,
        bool allowDefault)
    {
        ValidateNamespaceName(namespaceName);
        if (namespaceName.Length == 0)
        {
            return "";
        }
        if (namespaceName == OdfNamespaces.Xml)
        {
            return OdfNamespaces.XmlPrefix;
        }

        string standardPrefix = GetPreferredPrefix(namespaceName);
        if (!PreservePrefixes
            && standardPrefix.Length > 0
            && IsBoundToNamespace(standardPrefix, namespaceName))
        {
            return standardPrefix;
        }
        if (allowDefault && IsBoundToNamespace("", namespaceName))
        {
            return "";
        }

        int index = prefixOrder.Count - 1;
        while (index >= 0)
        {
            string prefix = prefixOrder[index];
            if ((allowDefault || prefix.Length > 0)
                && IsBoundToNamespace(prefix, namespaceName))
            {
                return prefix;
            }
            index--;
        }
        return null;
    }

    internal string SelectPrefix(
        string namespaceName,
        string requestedPrefix,
        bool allowDefault)
    {
        ValidateNamespaceName(namespaceName);
        if (namespaceName.Length == 0)
        {
            return "";
        }
        if (namespaceName == OdfNamespaces.Xml)
        {
            return OdfNamespaces.XmlPrefix;
        }

        if (PreservePrefixes
            && IsPrefixCandidate(requestedPrefix, allowDefault)
            && IsPrefixAvailable(requestedPrefix, namespaceName))
        {
            return requestedPrefix;
        }

        string? existingPrefix = LookupPrefix(namespaceName, allowDefault);
        if (PreservePrefixes && existingPrefix != null)
        {
            return existingPrefix;
        }

        string standardPrefix = GetPreferredPrefix(namespaceName);
        if (standardPrefix.Length > 0
            && IsPrefixAvailable(standardPrefix, namespaceName))
        {
            return standardPrefix;
        }
        if (existingPrefix != null)
        {
            return existingPrefix;
        }
        if (!PreservePrefixes
            && standardPrefix.Length == 0
            && IsPrefixCandidate(requestedPrefix, allowDefault)
            && IsPrefixAvailable(requestedPrefix, namespaceName))
        {
            return requestedPrefix;
        }
        if (allowDefault
            && requestedPrefix.Length == 0
            && IsPrefixAvailable("", namespaceName))
        {
            return "";
        }

        string prefixBase = standardPrefix;
        if (prefixBase.Length == 0
            && IsPrefixCandidate(requestedPrefix, false))
        {
            prefixBase = requestedPrefix;
        }
        if (prefixBase.Length == 0)
        {
            prefixBase = "ns";
        }
        string candidate = prefixBase;
        int suffix = 1;
        while (!IsPrefixAvailable(candidate, namespaceName))
        {
            candidate = $"{prefixBase}{suffix}";
            suffix++;
        }
        return candidate;
    }

    internal OdfQualifiedName ResolveQualifiedName(
        string lexicalName,
        bool useDefaultNamespace)
    {
        if (!OdfNameRules.IsQName(lexicalName))
        {
            throw new OdfXmlException("The lexical XML qualified name is invalid.");
        }

        string prefix = "";
        string localName = lexicalName;
        string namespaceName = "";
        int colon = lexicalName.IndexOf(':');
        if (colon >= 0)
        {
            prefix = lexicalName.Substring(0, colon);
            localName = lexicalName.Substring(colon + 1);
            string? resolvedNamespace = LookupNamespace(prefix);
            if (resolvedNamespace == null || resolvedNamespace.Length == 0)
            {
                throw new OdfXmlException("The XML qualified name uses an unbound namespace prefix.");
            }
            namespaceName = resolvedNamespace;
        }
        else if (useDefaultNamespace)
        {
            string? defaultNamespace = LookupNamespace("");
            if (defaultNamespace != null)
            {
                namespaceName = defaultNamespace;
            }
        }

        return new OdfQualifiedName
        {
            NamespaceName = namespaceName,
            LocalName = localName,
            Prefix = prefix
        };
    }

    internal OdfNamespaceDeclaration[] GetDeclarations()
    {
        OdfNamespaceDeclaration[] result = new OdfNamespaceDeclaration[declarations.Count];
        int index = 0;
        while (index < declarations.Count)
        {
            OdfNamespaceDeclaration declaration = declarations[index];
            result[index] = new OdfNamespaceDeclaration
            {
                Prefix = declaration.Prefix,
                NamespaceName = declaration.NamespaceName
            };
            index++;
        }
        return result;
    }

    internal static bool IsKnownNamespaceName(string namespaceName)
    {
        if (namespaceName == null || namespaceName.Length == 0)
        {
            return true;
        }
        int index = 0;
        while (index < KnownNamespaceNames.Length)
        {
            if (KnownNamespaceNames[index] == namespaceName)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IsLookupPrefix(string prefix)
    {
        if (prefix == null)
        {
            return false;
        }
        if (prefix.Length > 0 && !OdfNameRules.IsNcName(prefix))
        {
            return false;
        }
        if (prefix == "xmlns")
        {
            return false;
        }
        return !prefix.StartsWith("xml", StringComparison.OrdinalIgnoreCase)
            || prefix == OdfNamespaces.XmlPrefix;
    }

    static bool IsPrefixCandidate(
        string prefix,
        bool allowDefault)
    {
        if (prefix == null)
        {
            return false;
        }
        if (prefix.Length == 0)
        {
            return allowDefault;
        }
        if (!OdfNameRules.IsNcName(prefix) || prefix == "xmlns")
        {
            return false;
        }
        return !prefix.StartsWith("xml", StringComparison.OrdinalIgnoreCase);
    }

    bool IsPrefixAvailable(
        string prefix,
        string namespaceName)
    {
        return !bindings.ContainsKey(prefix)
            || bindings[prefix] == namespaceName;
    }

    bool IsBoundToNamespace(
        string prefix,
        string namespaceName)
    {
        return bindings.ContainsKey(prefix)
            && bindings[prefix] == namespaceName;
    }

    static void ValidateNamespaceName(string namespaceName)
    {
        if (namespaceName == null
            || namespaceName == "http://www.w3.org/2000/xmlns/")
        {
            throw new OdfXmlException("The XML namespace name is invalid or reserved.");
        }
    }

    static string GetPreferredPrefix(string namespaceName)
    {
        int index = 0;
        while (index < KnownNamespaceNames.Length)
        {
            if (KnownNamespaceNames[index] == namespaceName)
            {
                return KnownPrefixes[index];
            }
            index++;
        }
        return "";
    }
}
