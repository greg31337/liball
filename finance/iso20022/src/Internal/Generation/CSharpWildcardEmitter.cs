using System.Text;

namespace iso20022;

internal static class CSharpWildcardEmitter
{
    internal static void WriteRuntime(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRawReader(builder, context);
        WriteNamespaceCapture(builder, context);
        WriteRawWriter(builder, context);
        WriteRawDocumentHelpers(builder, context);
    }

    internal static void WriteRawReader(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteMethodStart(builder, context,
            "internal static global::iso20022.RawXmlFragment ReadRawFragment(" +
            "global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "MoveToElement(reader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.RawXmlFragment fragment = " +
            "new global::iso20022.RawXmlFragment();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "fragment.NamespaceDeclarations = CaptureRawNamespaces(reader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string outerXml = reader.ReadOuterXml();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Xml.XmlDocument document = " +
            "new global::System.Xml.XmlDocument();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "document.PreserveWhitespace = true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "document.LoadXml(outerXml);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Xml.XmlElement container = document.DocumentElement;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "fragment.Xml = container.InnerXml;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "fragment.Utf8Bytes = global::System.Text.Encoding.UTF8.GetBytes(fragment.Xml);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "fragment.RootName = FindRawRootName(container);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return fragment;");
        WriteMethodEnd(builder, context);

        WriteMethodStart(builder, context,
            "internal static global::iso20022.QualifiedNameData FindRawRootName(" +
            "global::System.Xml.XmlElement container)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Xml.XmlNode node = container.FirstChild;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Xml.XmlElement root = null;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "while (node != null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (node.NodeType == global::System.Xml.XmlNodeType.Element)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "if (root != null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 5, "return null;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "root = (global::System.Xml.XmlElement)node;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "node = node.NextSibling;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (root == null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return null;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.QualifiedNameData name = " +
            "new global::iso20022.QualifiedNameData();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "name.NamespaceUri = root.NamespaceURI;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "name.LocalName = root.LocalName;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "name.Prefix = root.Prefix;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return name;");
        WriteMethodEnd(builder, context);
    }

    internal static void WriteNamespaceCapture(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteMethodStart(builder, context,
            "internal static global::System.Collections.Generic.List<" +
            "global::iso20022.NamespaceDeclarationData> CaptureRawNamespaces(" +
            "global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Xml.IXmlNamespaceResolver resolver = " +
            "(global::System.Xml.IXmlNamespaceResolver)reader;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Collections.Generic.IDictionary<string, string> values = " +
            "resolver.GetNamespacesInScope(global::System.Xml.XmlNamespaceScope.All);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Collections.Generic.List<" +
            "global::iso20022.NamespaceDeclarationData> declarations = " +
            "new global::System.Collections.Generic.List<" +
            "global::iso20022.NamespaceDeclarationData>();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "foreach (global::System.Collections.Generic.KeyValuePair<string, string> " +
            "item in values)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (item.Key != \"xml\" && item.Key != \"xmlns\")");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "global::iso20022.NamespaceDeclarationData declaration = " +
            "new global::iso20022.NamespaceDeclarationData();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "declaration.Prefix = item.Key;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "declaration.NamespaceUri = item.Value;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "declarations.Add(declaration);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "SortRawNamespaces(declarations);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return declarations;");
        WriteMethodEnd(builder, context);

        WriteMethodStart(builder, context,
            "internal static void SortRawNamespaces(" +
            "global::System.Collections.Generic.List<" +
            "global::iso20022.NamespaceDeclarationData> declarations)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "int index = 1;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "while (index < declarations.Count)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "global::iso20022.NamespaceDeclarationData value = declarations[index];");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "int destination = index;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "while (destination > 0 && global::System.String.CompareOrdinal(" +
            "value.Prefix, declarations[destination - 1].Prefix) < 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "declarations[destination] = declarations[destination - 1];");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "destination--;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "declarations[destination] = value;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "index++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        WriteMethodEnd(builder, context);
    }

    internal static void WriteRawWriter(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteMethodStart(builder, context,
            "internal static void ValidateRawFragment(" +
            "global::iso20022.RawXmlFragment fragment)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (fragment == null || (fragment.Xml.Length == 0 && " +
            "fragment.Utf8Bytes.Length == 0))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "throw new global::System.InvalidOperationException(" +
            "\"The wildcard fragment must contain XML text or UTF-8 bytes.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Xml.XmlDocument document = CreateRawDocument(fragment);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "ValidateRawRoot(fragment, document.DocumentElement);");
        WriteMethodEnd(builder, context);

        WriteMethodStart(builder, context,
            "internal static void WriteRawFragment(global::System.Xml.XmlWriter writer, " +
            "global::iso20022.RawXmlFragment fragment)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Xml.XmlDocument document = CreateRawDocument(fragment);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Xml.XmlElement container = document.DocumentElement;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "ValidateRawRoot(fragment, container);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Xml.XmlNode node = container.FirstChild;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "while (node != null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (node.NodeType == global::System.Xml.XmlNodeType.Element)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "ApplyRawNamespaceClosure((global::System.Xml.XmlElement)node, " +
            "fragment.NamespaceDeclarations);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "writer.WriteRaw(node.OuterXml);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "node = node.NextSibling;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        WriteMethodEnd(builder, context);
    }

    internal static void WriteRawDocumentHelpers(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteCreateDocument(builder, context);
        WriteApplyClosure(builder, context);
        WriteValidateRoot(builder, context);
    }

    internal static void WriteCreateDocument(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteMethodStart(builder, context,
            "internal static global::System.Xml.XmlDocument CreateRawDocument(" +
            "global::iso20022.RawXmlFragment fragment)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string xml = fragment.Xml;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (xml.Length == 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "xml = global::System.Text.Encoding.UTF8.GetString(fragment.Utf8Bytes);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Xml.XmlDocument document = " +
            "new global::System.Xml.XmlDocument();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "document.PreserveWhitespace = true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Xml.XmlElement container = document.CreateElement(" +
            "\"isoRaw\", \"Fragment\", \"urn:iso20022:internal:raw-fragment\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "document.AppendChild(container);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "int declarationIndex = 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "while (declarationIndex < fragment.NamespaceDeclarations.Count)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "SetRawNamespace(container, fragment.NamespaceDeclarations[declarationIndex]);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "declarationIndex++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "container.InnerXml = xml;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return document;");
        WriteMethodEnd(builder, context);
    }

    internal static void WriteApplyClosure(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteMethodStart(builder, context,
            "internal static void ApplyRawNamespaceClosure(" +
            "global::System.Xml.XmlElement element, " +
            "global::System.Collections.Generic.List<" +
            "global::iso20022.NamespaceDeclarationData> declarations)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Collections.Generic.List<" +
            "global::iso20022.NamespaceDeclarationData> merged = " +
            "new global::System.Collections.Generic.List<" +
            "global::iso20022.NamespaceDeclarationData>();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "int attributeIndex = element.Attributes.Count - 1;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "while (attributeIndex >= 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "global::System.Xml.XmlAttribute attribute = element.Attributes[attributeIndex];");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (attribute.NamespaceURI == \"http://www.w3.org/2000/xmlns/\")");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "global::iso20022.NamespaceDeclarationData existing = " +
            "new global::iso20022.NamespaceDeclarationData();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "if (attribute.Prefix == \"xmlns\")");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 5,
            "existing.Prefix = attribute.LocalName;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "existing.NamespaceUri = attribute.Value;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "merged.Add(existing);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "element.Attributes.Remove(attribute);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "attributeIndex--;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "int index = 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "while (index < declarations.Count)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "global::iso20022.NamespaceDeclarationData declaration = declarations[index];");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (!HasRawNamespacePrefix(merged, declaration.Prefix))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "merged.Add(declaration);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "index++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "SortRawNamespaces(merged);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "index = 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "while (index < merged.Count)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "SetRawNamespace(element, merged[index]);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "index++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        WriteMethodEnd(builder, context);

        WriteMethodStart(builder, context,
            "internal static bool HasRawNamespacePrefix(" +
            "global::System.Collections.Generic.List<" +
            "global::iso20022.NamespaceDeclarationData> declarations, string prefix)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "int index = 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "while (index < declarations.Count)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (declarations[index].Prefix == prefix)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "return true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "index++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return false;");
        WriteMethodEnd(builder, context);

        WriteMethodStart(builder, context,
            "internal static void SetRawNamespace(global::System.Xml.XmlElement element, " +
            "global::iso20022.NamespaceDeclarationData declaration)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Xml.XmlAttribute attribute;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (declaration.Prefix.Length == 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "attribute = element.OwnerDocument.CreateAttribute(string.Empty, " +
            "\"xmlns\", \"http://www.w3.org/2000/xmlns/\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "else");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "attribute = element.OwnerDocument.CreateAttribute(\"xmlns\", " +
            "declaration.Prefix, \"http://www.w3.org/2000/xmlns/\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "attribute.Value = declaration.NamespaceUri;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "element.Attributes.SetNamedItem(attribute);");
        WriteMethodEnd(builder, context);
    }

    internal static void WriteValidateRoot(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteMethodStart(builder, context,
            "internal static void ValidateRawRoot(global::iso20022.RawXmlFragment fragment, " +
            "global::System.Xml.XmlElement container)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (fragment.RootName == null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.QualifiedNameData actual = FindRawRootName(container);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (actual == null || actual.NamespaceUri != fragment.RootName.NamespaceUri || " +
            "actual.LocalName != fragment.RootName.LocalName)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "throw new global::System.Xml.XmlException(" +
            "\"The wildcard fragment does not match its declared root name.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        WriteMethodEnd(builder, context);
    }

    internal static void WriteMethodStart(
        StringBuilder builder,
        CSharpGenerationContext context,
        string declaration)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, declaration);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
    }

    internal static void WriteMethodEnd(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }
}
