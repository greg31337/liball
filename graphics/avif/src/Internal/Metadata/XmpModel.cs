using System.Collections.Generic;

namespace avif;

internal class XmpParseResult
{
    internal AvifResultCode Code;
    internal XmpParsedData? Parsed;

    internal XmpParseResult(AvifResultCode code)
    {
        Code = code;
        Parsed = null;
    }
}

internal class XmpParsedData
{
    internal byte[] Data;
    internal List<XmpField> Fields;

    internal XmpParsedData()
    {
        Data = System.Array.Empty<byte>();
        Fields = new List<XmpField>();
    }
}

internal class XmpField
{
    internal string NamespaceUri;
    internal string Name;
    internal string Value;

    internal XmpField(string namespaceUri, string name, string value)
    {
        NamespaceUri = namespaceUri;
        Name = name;
        Value = value;
    }
}

internal class XmpNamespace
{
    internal string Prefix;
    internal string Uri;

    internal XmpNamespace(string prefix, string uri)
    {
        Prefix = prefix;
        Uri = uri;
    }
}

internal class XmpAttribute
{
    internal string RawName;
    internal string Prefix;
    internal string LocalName;
    internal string NamespaceUri;
    internal string Value;

    internal XmpAttribute(string rawName, string value)
    {
        RawName = rawName;
        Value = value;
        Prefix = string.Empty;
        LocalName = string.Empty;
        NamespaceUri = string.Empty;
    }
}

internal class XmpNode
{
    internal string RawName;
    internal string Prefix;
    internal string LocalName;
    internal string NamespaceUri;
    internal XmpNode? Parent;
    internal List<XmpNamespace> Namespaces;
    internal List<XmpAttribute> Attributes;
    internal List<XmpNode> Children;
    internal System.Text.StringBuilder Text;

    internal XmpNode(string rawName, XmpNode? parent)
    {
        RawName = rawName;
        Parent = parent;
        Prefix = string.Empty;
        LocalName = string.Empty;
        NamespaceUri = string.Empty;
        Namespaces = new List<XmpNamespace>();
        Attributes = new List<XmpAttribute>();
        Children = new List<XmpNode>();
        Text = new System.Text.StringBuilder();
    }
}

internal class XmpQualifiedName
{
    internal bool Valid;
    internal string Prefix;
    internal string LocalName;

    internal XmpQualifiedName()
    {
        Prefix = string.Empty;
        LocalName = string.Empty;
    }
}

internal class XmpStringResult
{
    internal bool Valid;
    internal string Value;

    internal XmpStringResult()
    {
        Value = string.Empty;
    }
}
