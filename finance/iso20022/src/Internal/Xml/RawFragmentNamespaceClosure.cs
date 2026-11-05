using System;
using System.Collections.Generic;
using System.Xml;

namespace iso20022;

internal static class RawFragmentNamespaceClosure
{
    internal static void ApplyNamespaceClosure(RawXmlFragment? fragment, NamespaceScopeTracker? tracker)
    {
        if (fragment == null)
        {
            throw new ArgumentNullException("fragment");
        }

        if (tracker == null)
        {
            throw new ArgumentNullException("tracker");
        }

        if (fragment.RootName != null)
        {
            ValidateRootBinding(fragment.RootName, tracker);
        }

        List<NamespaceDeclarationData> declarations = tracker.CaptureDeclarations();
        fragment.NamespaceDeclarations = declarations;
    }

    internal static void ValidateRootBinding(QualifiedNameData rootName, NamespaceScopeTracker tracker)
    {
        string? resolvedNamespace = tracker.ResolvePrefix(rootName.Prefix);
        if (resolvedNamespace == null || resolvedNamespace != rootName.NamespaceUri)
        {
            throw new XmlException("The raw fragment root name is not bound by the active namespace scope.");
        }
    }
}
