using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal static class SignatureCanonicalizer
{
    internal static byte[] CanonicalizeExclusive(
        DynamicXmlNode node,
        DynamicXmlNode? excluded)
    {
        StringBuilder output = new StringBuilder();
        List<NamespaceDeclarationData> rendered =
            new List<NamespaceDeclarationData>();
        AppendNode(node, excluded, rendered, output);
        return Encoding.UTF8.GetBytes(output.ToString());
    }

    internal static void AppendNode(
        DynamicXmlNode node,
        DynamicXmlNode? excluded,
        List<NamespaceDeclarationData> rendered,
        StringBuilder output)
    {
        if (object.ReferenceEquals(node, excluded) ||
            node.Kind == XmlNodeKind.Comment)
        {
            return;
        }
        if (node.Kind == XmlNodeKind.Document)
        {
            int documentIndex = 0;
            while (documentIndex < node.Children.Count)
            {
                AppendNode(node.Children[documentIndex], excluded, rendered, output);
                documentIndex++;
            }
            return;
        }
        if (node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData)
        {
            AppendText(output, node.Value);
            return;
        }
        if (node.Kind == XmlNodeKind.ProcessingInstruction)
        {
            output.Append("<?");
            output.Append(node.Name.LocalName);
            if (node.Value.Length > 0)
            {
                output.Append(' ');
                output.Append(node.Value.Replace("\r", "&#xD;"));
            }
            output.Append("?>");
            return;
        }

        List<NamespaceDeclarationData> current = CopyNamespaces(rendered);
        List<NamespaceDeclarationData> declarations =
            new List<NamespaceDeclarationData>();
        RequireNamespace(
            node.Name.Prefix,
            node.Name.NamespaceUri,
            current,
            declarations);
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Count)
        {
            QualifiedNameData attributeName = node.Attributes[attributeIndex].Name;
            if (attributeName.Prefix.Length > 0 &&
                attributeName.Prefix != "xml")
            {
                RequireNamespace(
                    attributeName.Prefix,
                    attributeName.NamespaceUri,
                    current,
                    declarations);
            }
            attributeIndex++;
        }
        SortDeclarations(declarations);
        List<XmlAttributeData> attributes = CopyAttributes(node.Attributes);
        CanonicalXmlNodeNormalizer.SortAttributes(attributes);

        output.Append('<');
        AppendQualifiedName(output, node.Name);
        int declarationIndex = 0;
        while (declarationIndex < declarations.Count)
        {
            NamespaceDeclarationData declaration = declarations[declarationIndex];
            output.Append(" xmlns");
            if (declaration.Prefix.Length > 0)
            {
                output.Append(':');
                output.Append(declaration.Prefix);
            }
            output.Append("=\"");
            AppendAttributeValue(output, declaration.NamespaceUri);
            output.Append('"');
            declarationIndex++;
        }
        attributeIndex = 0;
        while (attributeIndex < attributes.Count)
        {
            XmlAttributeData attribute = attributes[attributeIndex];
            output.Append(' ');
            AppendQualifiedName(output, attribute.Name);
            output.Append("=\"");
            AppendAttributeValue(output, attribute.Value);
            output.Append('"');
            attributeIndex++;
        }
        output.Append('>');
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            AppendNode(node.Children[childIndex], excluded, current, output);
            childIndex++;
        }
        output.Append("</");
        AppendQualifiedName(output, node.Name);
        output.Append('>');
    }

    internal static void RequireNamespace(
        string prefix,
        string namespaceUri,
        List<NamespaceDeclarationData> rendered,
        List<NamespaceDeclarationData> declarations)
    {
        if (prefix == "xml")
        {
            return;
        }
        int existing = FindPrefix(rendered, prefix);
        if (existing >= 0 && rendered[existing].NamespaceUri == namespaceUri)
        {
            return;
        }
        if (existing < 0 && prefix.Length == 0 && namespaceUri.Length == 0)
        {
            return;
        }

        NamespaceDeclarationData declaration = new NamespaceDeclarationData();
        declaration.Prefix = prefix;
        declaration.NamespaceUri = namespaceUri;
        int pending = FindPrefix(declarations, prefix);
        if (pending >= 0)
        {
            declarations[pending] = declaration;
        }
        else
        {
            declarations.Add(declaration);
        }
        if (existing >= 0)
        {
            rendered[existing] = declaration;
        }
        else
        {
            rendered.Add(declaration);
        }
    }

    internal static int FindPrefix(
        List<NamespaceDeclarationData> declarations,
        string prefix)
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

    internal static List<NamespaceDeclarationData> CopyNamespaces(
        List<NamespaceDeclarationData> source)
    {
        List<NamespaceDeclarationData> result =
            new List<NamespaceDeclarationData>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(NamespaceScopeTracker.CopyDeclaration(source[index]));
            index++;
        }
        return result;
    }

    internal static List<XmlAttributeData> CopyAttributes(
        List<XmlAttributeData> source)
    {
        List<XmlAttributeData> result = new List<XmlAttributeData>();
        int index = 0;
        while (index < source.Count)
        {
            XmlAttributeData attribute = new XmlAttributeData();
            attribute.Name = XmlPathTracker.CopyName(source[index].Name);
            attribute.Value = source[index].Value;
            attribute.IsSpecified = source[index].IsSpecified;
            attribute.Location = XmlPathTracker.CopyLocation(source[index].Location);
            result.Add(attribute);
            index++;
        }
        return result;
    }

    internal static void SortDeclarations(
        List<NamespaceDeclarationData> declarations)
    {
        int outer = 1;
        while (outer < declarations.Count)
        {
            NamespaceDeclarationData value = declarations[outer];
            int inner = outer - 1;
            while (inner >= 0 && string.CompareOrdinal(
                declarations[inner].Prefix,
                value.Prefix) > 0)
            {
                declarations[inner + 1] = declarations[inner];
                inner--;
            }
            declarations[inner + 1] = value;
            outer++;
        }
    }

    internal static void AppendQualifiedName(
        StringBuilder output,
        QualifiedNameData name)
    {
        if (name.Prefix.Length > 0)
        {
            output.Append(name.Prefix);
            output.Append(':');
        }
        output.Append(name.LocalName);
    }

    internal static void AppendAttributeValue(
        StringBuilder output,
        string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '&')
            {
                output.Append("&amp;");
            }
            else if (character == '<')
            {
                output.Append("&lt;");
            }
            else if (character == '"')
            {
                output.Append("&quot;");
            }
            else if (character == '\t')
            {
                output.Append("&#x9;");
            }
            else if (character == '\n')
            {
                output.Append("&#xA;");
            }
            else if (character == '\r')
            {
                output.Append("&#xD;");
            }
            else
            {
                output.Append(character);
            }
            index++;
        }
    }

    internal static void AppendText(StringBuilder output, string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '&')
            {
                output.Append("&amp;");
            }
            else if (character == '<')
            {
                output.Append("&lt;");
            }
            else if (character == '\r')
            {
                output.Append("&#xD;");
            }
            else if (character == '>' && index >= 2 &&
                value[index - 1] == ']' && value[index - 2] == ']')
            {
                output.Append("&gt;");
            }
            else
            {
                output.Append(character);
            }
            index++;
        }
    }
}
