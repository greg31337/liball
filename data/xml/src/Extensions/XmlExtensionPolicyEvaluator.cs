using System;

namespace xml;

internal class XmlExtensionRuntimeState
{
    internal int Depth;
    internal int InvocationCount;

    internal XmlExtensionRuntimeState()
    {
        Depth = 0;
        InvocationCount = 0;
    }
}

internal class XmlExtensionPolicyEvaluator
{
    internal XmlExtensionPolicy CopyXmlExtensionPolicy(XmlExtensionPolicy? supplied)
    {
        XmlExtensionPolicy result = new XmlExtensionPolicy();
        if (supplied == null)
        {
            result.Trust = XmlExtensionTrust.Trusted;
            result.AllowedNamespaceUris = [];
            result.AllowReentrancy = false;
            result.MaximumInvocations = 1024;
            return result;
        }

        if (supplied.Trust < XmlExtensionTrust.Denied || supplied.Trust > XmlExtensionTrust.Trusted)
        {
            throw new ArgumentOutOfRangeException("policy.Trust");
        }

        if (supplied.MaximumInvocations < 0)
        {
            throw new ArgumentOutOfRangeException("policy.MaximumInvocations");
        }

        result.Trust = supplied.Trust;
        result.AllowedNamespaceUris = CopyXmlExtensionNamespaces(supplied.AllowedNamespaceUris);
        result.AllowReentrancy = supplied.AllowReentrancy;
        result.MaximumInvocations = supplied.MaximumInvocations == 0 ? 1024 : supplied.MaximumInvocations;
        ValidateXmlExtensionAllowList(result.AllowedNamespaceUris);
        return result;
    }

    internal string[] CopyXmlExtensionNamespaces(string[]? supplied)
    {
        return supplied == null ? [] : (string[])supplied.Clone();
    }

    internal void ValidateXmlExtensionAllowList(string[]? namespaces)
    {
        string[] source = namespaces == null ? [] : namespaces;
        int index = 0;
        while (index < source.Length)
        {
            string? namespaceUri = source[index];
            if (namespaceUri == null || namespaceUri.Length == 0 || !Uri.IsWellFormedUriString(namespaceUri, UriKind.Absolute))
            {
                throw new ArgumentException("Extension allow-list entries must be absolute namespace URIs.", "policy");
            }

            int earlier = 0;
            while (earlier < index)
            {
                if (source[earlier] == namespaceUri)
                {
                    throw new ArgumentException("Extension allow-list namespace URIs must be unique.", "policy");
                }

                earlier = earlier + 1;
            }

            index = index + 1;
        }
    }

    internal void RequireXmlExtensionName(XmlExtensionPolicy policy, XmlName? name, string parameterName)
    {
        if (name == null || name.LocalName == null || !new XdmStringDatatype().IsValidXdmStringLexical(name.LocalName, XdmAtomicType.NcName))
        {
            throw new ArgumentException("An extension binding requires a valid NCName local name.", parameterName);
        }

        string namespaceUri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        if (namespaceUri.Length == 0 || !Uri.IsWellFormedUriString(namespaceUri, UriKind.Absolute))
        {
            throw new ArgumentException("An extension binding requires an absolute namespace URI.", parameterName);
        }

        string prefix = name.Prefix == null ? "" : name.Prefix;
        if (prefix.Length != 0 && !new XdmStringDatatype().IsValidXdmStringLexical(prefix, XdmAtomicType.NcName))
        {
            throw new ArgumentException("An extension binding prefix must be a valid NCName.", parameterName);
        }

        string? qualifiedName = name.QualifiedName;
        if (qualifiedName != null)
        {
            int colon = qualifiedName.IndexOf(':');
            string qualifiedPrefix = colon < 0 ? "" : qualifiedName.Substring(0, colon);
            string qualifiedLocalName = colon < 0 ? qualifiedName : qualifiedName.Substring(colon + 1);
            if (colon == 0 || colon != qualifiedName.LastIndexOf(':') || qualifiedLocalName != name.LocalName || prefix.Length != 0 && qualifiedPrefix != prefix || qualifiedPrefix.Length != 0 && !new XdmStringDatatype().IsValidXdmStringLexical(qualifiedPrefix, XdmAtomicType.NcName))
            {
                throw new ArgumentException("An extension binding qualified name is inconsistent with its prefix and local name.", parameterName);
            }
        }

        if (!IsXmlExtensionAllowed(policy, namespaceUri))
        {
            throw new ArgumentException("The extension namespace is denied by the configured trust policy.", parameterName);
        }
    }

    internal bool IsXmlExtensionAllowed(XmlExtensionPolicy policy, string namespaceUri)
    {
        if (policy.Trust == XmlExtensionTrust.Denied)
        {
            return false;
        }

        if (policy.Trust == XmlExtensionTrust.Trusted)
        {
            return true;
        }

        string[] allowed = policy.AllowedNamespaceUris == null ? [] : policy.AllowedNamespaceUris;
        int index = 0;
        while (index < allowed.Length)
        {
            if (allowed[index] == namespaceUri)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsXmlExtensionReservedNamespace(string namespaceUri)
    {
        return namespaceUri == XsltNamespaces.Xslt || namespaceUri == XmlNamespaces.Xml || namespaceUri == XmlNamespaces.Xmlns ||
            namespaceUri == XmlNamespaces.XmlSchema || namespaceUri == XmlNamespaces.XmlSchemaInstance || namespaceUri == XPathNamespaces.Functions ||
            namespaceUri == XPathNamespaces.Math || namespaceUri == XPathNamespaces.Map || namespaceUri == XPathNamespaces.Array || namespaceUri == XPathNamespaces.Errors;
    }

    internal string ReadXmlExtensionFailureMessage(AggregateException? failure)
    {
        Exception? current = failure;
        while (current != null && current.GetType().FullName == "System.AggregateException")
        {
            AggregateException aggregate = (AggregateException)current;
            current = aggregate.InnerExceptions.Count == 0 ? null : aggregate.InnerExceptions[0];
        }

        return current == null || current.Message.Length == 0 ? "The extension implementation failed." : current.Message;
    }
}
