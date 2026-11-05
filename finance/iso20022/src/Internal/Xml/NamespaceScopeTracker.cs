using System;
using System.Collections.Generic;
using System.Xml;

namespace iso20022;

internal class NamespaceScopeTracker
{
    List<List<NamespaceDeclarationData>> scopes = new List<List<NamespaceDeclarationData>>();

    internal NamespaceScopeTracker()
    {
        scopes.Add(new List<NamespaceDeclarationData>());
    }

    internal void EnterScope()
    {
        scopes.Add(new List<NamespaceDeclarationData>());
    }

    internal void ExitScope()
    {
        if (scopes.Count <= 1)
        {
            throw new InvalidOperationException("No element namespace scope is available to exit.");
        }

        scopes.RemoveAt(scopes.Count - 1);
    }

    internal void DeclareNamespace(string? prefix, string? namespaceUri)
    {
        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        ValidateDeclaration(prefix, namespaceUri);
        List<NamespaceDeclarationData> currentScope = scopes[scopes.Count - 1];
        int index = 0;
        while (index < currentScope.Count)
        {
            if (currentScope[index].Prefix == prefix)
            {
                throw new XmlException("The namespace prefix is declared more than once in the same scope.");
            }

            index++;
        }

        NamespaceDeclarationData declaration = new NamespaceDeclarationData();
        declaration.Prefix = prefix;
        declaration.NamespaceUri = namespaceUri;
        currentScope.Add(declaration);
    }

    internal string? ResolvePrefix(string? prefix)
    {
        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        if (prefix == "xml")
        {
            return Iso20022Namespaces.Xml;
        }

        if (prefix == "xmlns")
        {
            return Iso20022Namespaces.Xmlns;
        }

        int scopeIndex = scopes.Count - 1;
        while (scopeIndex >= 0)
        {
            List<NamespaceDeclarationData> scope = scopes[scopeIndex];
            int declarationIndex = scope.Count - 1;
            while (declarationIndex >= 0)
            {
                NamespaceDeclarationData declaration = scope[declarationIndex];
                if (declaration.Prefix == prefix)
                {
                    if (declaration.NamespaceUri.Length == 0 && prefix.Length > 0)
                    {
                        return null;
                    }

                    return declaration.NamespaceUri;
                }

                declarationIndex--;
            }

            scopeIndex--;
        }

        if (prefix.Length == 0)
        {
            return string.Empty;
        }

        return null;
    }

    internal List<NamespaceDeclarationData> CaptureDeclarations()
    {
        List<NamespaceDeclarationData> declarations = new List<NamespaceDeclarationData>();
        int scopeIndex = 0;
        while (scopeIndex < scopes.Count)
        {
            List<NamespaceDeclarationData> scope = scopes[scopeIndex];
            int declarationIndex = 0;
            while (declarationIndex < scope.Count)
            {
                NamespaceDeclarationData declaration = scope[declarationIndex];
                int existingIndex = FindPrefixIndex(declarations, declaration.Prefix);
                if (declaration.NamespaceUri.Length == 0 && declaration.Prefix.Length > 0)
                {
                    if (existingIndex >= 0)
                    {
                        declarations.RemoveAt(existingIndex);
                    }
                }
                else if (existingIndex >= 0)
                {
                    declarations[existingIndex] = CopyDeclaration(declaration);
                }
                else
                {
                    declarations.Add(CopyDeclaration(declaration));
                }

                declarationIndex++;
            }

            scopeIndex++;
        }

        return declarations;
    }

    internal int GetScopeDepth()
    {
        return scopes.Count - 1;
    }

    internal static void ValidateDeclaration(string prefix, string namespaceUri)
    {
        if (prefix.Length > 0 && !XmlNameLexical.IsNcName(prefix.AsSpan()))
        {
            throw new XmlException("A namespace prefix must be a valid XML NCName.");
        }

        if (prefix == "xmlns" || namespaceUri == Iso20022Namespaces.Xmlns)
        {
            throw new XmlException("The xmlns prefix and namespace cannot be rebound.");
        }

        if (prefix == "xml" && namespaceUri != Iso20022Namespaces.Xml)
        {
            throw new XmlException("The xml prefix must use the registered XML namespace.");
        }

        if (prefix != "xml" && namespaceUri == Iso20022Namespaces.Xml)
        {
            throw new XmlException("The registered XML namespace must use the xml prefix.");
        }

        if (prefix.Length > 0 && namespaceUri.Length == 0)
        {
            throw new XmlException("A non-default namespace prefix cannot be bound to an empty namespace.");
        }

        if (namespaceUri.Length > 0 && !UriLexical.IsNamespaceUri(namespaceUri.AsSpan()))
        {
            throw new XmlException("A namespace declaration must contain a valid URI reference.");
        }
    }

    internal static int FindPrefixIndex(List<NamespaceDeclarationData> declarations, string prefix)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            if (declarations[index].Prefix == prefix)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal static NamespaceDeclarationData CopyDeclaration(NamespaceDeclarationData declaration)
    {
        NamespaceDeclarationData copy = new NamespaceDeclarationData();
        copy.Prefix = declaration.Prefix;
        copy.NamespaceUri = declaration.NamespaceUri;
        return copy;
    }
}
