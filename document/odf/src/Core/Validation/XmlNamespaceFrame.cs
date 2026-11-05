using System;
using System.Collections.Generic;

namespace odf;

internal class XmlNamespaceFrame
{
    Dictionary<string, string> Bindings =
        new Dictionary<string, string>(StringComparer.Ordinal);
    HashSet<string> LocalPrefixes =
        new HashSet<string>(StringComparer.Ordinal);

    internal XmlNamespaceFrame(XmlNamespaceFrame? parent)
    {
        if (parent != null)
        {
            foreach (string prefix in parent.Bindings.Keys)
            {
                Bindings.Add(prefix, parent.Bindings[prefix]);
            }
        }
        else
        {
            Bindings.Add("xml", OdfNamespaces.Xml);
        }
    }

    internal bool ContainsLocalPrefix(string prefix)
    {
        return LocalPrefixes.Contains(prefix);
    }

    internal void AddBinding(string prefix, string namespaceName)
    {
        LocalPrefixes.Add(prefix);
        Bindings[prefix] = namespaceName;
    }

    internal string FindNamespace(string prefix)
    {
        if (Bindings.ContainsKey(prefix))
        {
            return Bindings[prefix];
        }
        return "";
    }

    internal bool HasBinding(string prefix)
    {
        return Bindings.ContainsKey(prefix);
    }
}
