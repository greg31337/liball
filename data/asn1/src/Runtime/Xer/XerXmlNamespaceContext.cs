namespace asn1;

internal class XerXmlNamespaceContext
{
    internal const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
    internal const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";
    internal System.Collections.Generic.List<XerXmlNamespaceBinding> Bindings =
        new System.Collections.Generic.List<XerXmlNamespaceBinding>();

    internal XerXmlNamespaceContext(XerXmlNamespaceBinding[] bindings)
    {
        int index = 0;
        while (index < bindings.Length)
        {
            this.Bindings.Add(new XerXmlNamespaceBinding
            {
                Found = true,
                Prefix = bindings[index].Prefix,
                NamespaceUri = bindings[index].NamespaceUri
            });
            index = index + 1;
        }

        XerXmlNamespaceBinding xml = this.FindBinding("xml");
        if (!xml.Found)
        {
            this.Bindings.Add(new XerXmlNamespaceBinding
            {
                Found = true,
                Prefix = "xml",
                NamespaceUri = XmlNamespace
            });
        }
    }

    internal XerXmlNamespaceBinding FindBinding(string prefix)
    {
        int index = this.Bindings.Count - 1;
        while (index >= 0)
        {
            XerXmlNamespaceBinding binding = this.Bindings[index];
            if (binding.Prefix == prefix)
            {
                return new XerXmlNamespaceBinding
                {
                    Found = true,
                    Prefix = binding.Prefix,
                    NamespaceUri = binding.NamespaceUri
                };
            }

            index = index - 1;
        }

        return new XerXmlNamespaceBinding { Prefix = prefix };
    }

    internal void SetBinding(string prefix, string namespaceUri)
    {
        int index = this.Bindings.Count - 1;
        while (index >= 0)
        {
            if (this.Bindings[index].Prefix == prefix)
            {
                this.Bindings[index].NamespaceUri = namespaceUri;
                return;
            }

            index = index - 1;
        }

        this.Bindings.Add(new XerXmlNamespaceBinding
        {
            Found = true,
            Prefix = prefix,
            NamespaceUri = namespaceUri
        });
    }

    internal XerXmlNamespaceBinding[] CopyBindings()
    {
        XerXmlNamespaceBinding[] result =
            new XerXmlNamespaceBinding[this.Bindings.Count];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new XerXmlNamespaceBinding
            {
                Found = true,
                Prefix = this.Bindings[index].Prefix,
                NamespaceUri = this.Bindings[index].NamespaceUri
            };
            index = index + 1;
        }

        return result;
    }
}
