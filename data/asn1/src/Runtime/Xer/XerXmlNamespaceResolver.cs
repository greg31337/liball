namespace asn1;

internal class XerXmlNamespaceResolver
{
    internal XerXmlCharacters Characters = new XerXmlCharacters();

    internal XerXmlQualifiedName ResolveElementName(
        string value,
        XerXmlNamespaceBinding[] bindings)
    {
        return this.ResolveName(value, bindings, true);
    }

    internal XerXmlQualifiedName ResolveAttributeName(
        string value,
        XerXmlNamespaceBinding[] bindings)
    {
        return this.ResolveName(value, bindings, false);
    }

    internal XerXmlQualifiedName ResolveQNameValue(
        string value,
        XerXmlNamespaceBinding[] bindings)
    {
        return this.ResolveName(value, bindings, true);
    }

    internal XerXmlQualifiedName ResolveName(
        string value,
        XerXmlNamespaceBinding[] bindings,
        bool useDefaultNamespace)
    {
        XerXmlQualifiedName result = new XerXmlQualifiedName
        {
            Name = value
        };
        int first = value.IndexOf(':');
        int last = value.LastIndexOf(':');
        if (first != last
            || first == 0
            || first == value.Length - 1)
        {
            result.ErrorCode = "ASNXER0027";
            result.ErrorMessage = "The XML qualified name is malformed.";
            return result;
        }

        result.Prefix = first < 0 ? "" : value.Substring(0, first);
        result.LocalName = first < 0
            ? value
            : value.Substring(first + 1);
        if (!this.Characters.IsValidNcName(result.LocalName)
            || result.Prefix.Length > 0
                && !this.Characters.IsValidNcName(result.Prefix))
        {
            result.ErrorCode = "ASNXER0027";
            result.ErrorMessage = "The XML qualified name is malformed.";
            return result;
        }

        if (result.Prefix == "xmlns")
        {
            result.ErrorCode = "ASNXER0028";
            result.ErrorMessage = "The reserved xmlns prefix cannot name an XML value.";
            return result;
        }

        string lookupPrefix = result.Prefix;
        if (lookupPrefix.Length == 0 && !useDefaultNamespace)
        {
            result.Success = true;
            return result;
        }

        XerXmlNamespaceContext context =
            new XerXmlNamespaceContext(bindings);
        XerXmlNamespaceBinding binding = context.FindBinding(lookupPrefix);
        if (lookupPrefix.Length > 0 && !binding.Found)
        {
            result.ErrorCode = "ASNXER0029";
            result.ErrorMessage = "The XML qualified name uses an undeclared prefix.";
            return result;
        }

        result.NamespaceUri = binding.Found ? binding.NamespaceUri : "";
        result.Success = true;
        return result;
    }
}
