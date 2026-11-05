using System;
using System.Collections.Generic;

namespace xml;

internal class NamespaceContext
{
    NamespaceContext? Parent;
    NamespaceFrame Frame;
    int Depth;

    internal NamespaceContext(NamespaceContext? parent, XmlNamespaceBinding[] declarations)
    {
        Guard.NotNull(declarations, "declarations");
        Parent = parent;
        Frame = new NamespaceFrame(declarations);
        Depth = parent == null ? 0 : parent.Depth + 1;
    }

    internal NamespaceContext PushXmlNamespaceFrame(XmlNamespaceBinding[] declarations)
    {
        Guard.NotNull(declarations, "declarations");
        return new NamespaceContext(this, declarations);
    }

    internal NamespaceContext PopXmlNamespaceFrame()
    {
        NamespaceContext? parent = Parent;
        if (parent == null)
        {
            throw new InvalidOperationException("The root namespace context cannot be popped.");
        }

        return parent;
    }

    internal int CurrentXmlNamespaceDepth()
    {
        return Depth;
    }

    internal string? LookupXmlNamespaceUri(string prefix)
    {
        Guard.NotNull(prefix, "prefix");
        NamespaceContext? context = this;
        while (context != null)
        {
            XmlNamespaceBinding? binding = context.Frame.LookupLocalXmlNamespace(prefix);
            if (binding != null)
            {
                if (binding.IsUndeclaration || binding.NamespaceUri == null || binding.NamespaceUri.Length == 0)
                {
                    return null;
                }

                return binding.NamespaceUri;
            }

            context = context.Parent;
        }

        return string.Equals(prefix, "xml", StringComparison.Ordinal) ? XmlNamespaces.Xml : null;
    }

    internal string? LookupXmlPrefix(string namespaceUri)
    {
        Guard.NotNull(namespaceUri, "namespaceUri");
        XmlNamespaceBinding[] bindings = EnumerateInScopeXmlNamespaces();
        int index = 0;
        while (index < bindings.Length)
        {
            if (string.Equals(bindings[index].NamespaceUri, namespaceUri, StringComparison.Ordinal))
            {
                return bindings[index].Prefix;
            }

            index = index + 1;
        }

        return null;
    }

    internal string? DefaultXmlNamespaceUri()
    {
        return LookupXmlNamespaceUri("");
    }

    internal XmlNamespaceBinding[] EnumerateInScopeXmlNamespaces()
    {
        List<XmlNamespaceBinding> inScope = new List<XmlNamespaceBinding>();
        HashSet<string> seenPrefixes = new HashSet<string>(StringComparer.Ordinal);
        NamespaceContext? context = this;
        while (context != null)
        {
            XmlNamespaceBinding[] declarations = context.Frame.CopyLocalXmlNamespaces();
            int index = 0;
            while (index < declarations.Length)
            {
                XmlNamespaceBinding binding = declarations[index];
                string prefix = binding.Prefix == null ? "" : binding.Prefix;
                if (seenPrefixes.Add(prefix))
                {
                    string namespaceUri = binding.NamespaceUri == null ? "" : binding.NamespaceUri;
                    if (!binding.IsUndeclaration && namespaceUri.Length > 0)
                    {
                        inScope.Add(binding);
                    }
                }

                index = index + 1;
            }

            context = context.Parent;
        }

        if (!seenPrefixes.Contains("xml"))
        {
            XmlNamespaceBinding implicitXml = new XmlNamespaceBinding();
            implicitXml.Prefix = "xml";
            implicitXml.NamespaceUri = XmlNamespaces.Xml;
            implicitXml.IsUndeclaration = false;
            inScope.Add(implicitXml);
        }

        return inScope.ToArray();
    }
}
