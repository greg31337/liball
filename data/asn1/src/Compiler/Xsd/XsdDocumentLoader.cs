namespace asn1;

internal class XsdDocumentLoader
{
    internal const string SchemaNamespace =
        "http://www.w3.org/2001/XMLSchema";
    internal AsnResourceLimits Limits = new AsnResourceLimits();
    internal ResourceCounter Resources =
        new ResourceCounter(new AsnResourceLimits());
    internal XsdUriResolver Uris = new XsdUriResolver();
    internal System.Collections.Generic.List<AsnDiagnostic> Diagnostics =
        new System.Collections.Generic.List<AsnDiagnostic>();

    internal XsdDocumentLoadResult LoadDocuments(
        AsnXsdInput[] inputs,
        AsnXsdImportOptions options)
    {
        if (inputs == null)
        {
            throw new System.ArgumentNullException("inputs");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        this.Limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        this.Resources = new ResourceCounter(this.Limits);
        this.Diagnostics = new System.Collections.Generic.List<AsnDiagnostic>();
        if (!this.Resources.AllowsCollectionElements(inputs.Length))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.ResourceLimit,
                "The number of XML Schema resolver inputs exceeds the configured collection limit.",
                null);
            return this.CreateResult([], []);
        }

        System.Collections.Generic.List<XsdResolverInput> resolverInputs =
            new System.Collections.Generic.List<XsdResolverInput>();
        System.Collections.Generic.List<XsdLoadedDocument> documents =
            new System.Collections.Generic.List<XsdLoadedDocument>();
        int index = 0;
        while (index < inputs.Length)
        {
            AsnXsdInput? input = inputs[index];
            if (input == null)
            {
                this.AddDiagnostic(
                    XsdDiagnosticCodes.InvalidInput,
                    "An XML Schema resolver input is null.",
                    null);
                index = index + 1;
                continue;
            }
            string resolverUri = this.ValidateResolverInput(input);
            if (resolverUri.Length > 0)
            {
                if (this.HasResolverUri(resolverInputs, resolverUri))
                {
                    this.AddDiagnostic(
                        XsdDiagnosticCodes.DuplicateResolverUri,
                        "More than one XML Schema input uses resolver URI '"
                            + resolverUri + "'.",
                        this.CreateInputSpan(input, 0, 0));
                }
                else
                {
                    resolverInputs.Add(new XsdResolverInput
                    {
                        Name = input.Name,
                        ResolverUri = resolverUri,
                        InputIndex = index,
                        Input = input
                    });
                }
            }
            XsdLoadedDocument? document = this.LoadDocument(
                input,
                resolverUri);
            if (document != null)
            {
                documents.Add(document);
            }
            index = index + 1;
        }
        return this.CreateResult(
            documents.ToArray(),
            resolverInputs.ToArray());
    }

    internal string ValidateResolverInput(AsnXsdInput input)
    {
        if (input.Name == null || input.Name.Length == 0
            || input.Text == null)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidInput,
                "Each XML Schema input requires a non-empty name and document text.",
                null);
            return "";
        }
        if (!this.Resources.AllowsTextCharacters(input.Text.Length)
            || !this.Resources.TryChargeAllocationBytes(
                (long)input.Text.Length * 2))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.ResourceLimit,
                "The XML Schema document exceeds the configured text or allocation limit.",
                this.CreateInputSpan(input, 0, 0));
            return "";
        }
        if (input.HasBaseUri)
        {
            if (input.BaseUri == null
                || !this.Uris.IsValidAbsoluteUri(input.BaseUri))
            {
                this.AddDiagnostic(
                    XsdDiagnosticCodes.InvalidBaseUri,
                    "The supplied XML Schema base URI must be an absolute URI.",
                    this.CreateInputSpan(input, 0, 0));
                return "";
            }
            return this.Uris.NormalizeUriPath(input.BaseUri);
        }
        return input.Name;
    }

    internal XsdLoadedDocument? LoadDocument(
        AsnXsdInput input,
        string resolverUri)
    {
        if (input.Text == null || resolverUri.Length == 0)
        {
            return null;
        }
        int byteCount = System.Text.Encoding.UTF8.GetByteCount(input.Text);
        if (!this.Resources.TryChargeAllocationBytes(byteCount))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.ResourceLimit,
                "Encoding the XML Schema document exceeds the configured allocation limit.",
                this.CreateInputSpan(input, 0, 0));
            return null;
        }
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input.Text);
        XerXmlReadResult read = new XerXmlTokenReader(this.Limits).ConsumeXml(
            new AsnIncrementalInput
            {
                Data = bytes,
                IsFinalBlock = true
            });
        if (read.Status != AsnDecodeStatus.Done || !read.Complete)
        {
            this.AddDiagnostic(
                read.LimitExceeded
                    ? XsdDiagnosticCodes.ResourceLimit
                    : XsdDiagnosticCodes.MalformedXml,
                read.ErrorMessage.Length == 0
                    ? "The XML Schema document is not complete XML 1.0."
                    : read.ErrorMessage,
                this.CreateByteSpan(input, read.ViolationOffset, 0));
            return null;
        }
        if (!this.Resources.AllowsCollectionElements(read.Tokens.Length))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.ResourceLimit,
                "The XML Schema token count exceeds the configured collection limit.",
                this.CreateInputSpan(input, 0, 0));
            return null;
        }
        XerXmlToken? root = this.FindRootToken(read.Tokens);
        if (root == null || root.LocalName != "schema"
            || root.NamespaceUri != SchemaNamespace)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidSchemaRoot,
                "An XML Schema document must have an xs:schema root in the XML Schema 1.0 namespace.",
                root == null
                    ? this.CreateInputSpan(input, 0, 0)
                    : this.CreateTokenSpan(input, root));
            return null;
        }
        string baseUri = input.HasBaseUri ? resolverUri : "";
        XerXmlAttribute? xmlBase = this.FindAttribute(
            root,
            XerXmlNamespaceContext.XmlNamespace,
            "base");
        if (xmlBase != null)
        {
            if (!this.Uris.IsValidUriReference(xmlBase.Value))
            {
                this.AddDiagnostic(
                    XsdDiagnosticCodes.InvalidXmlBase,
                    "The xs:schema xml:base value is not a valid URI reference.",
                    this.CreateTokenSpan(input, root));
                return null;
            }
            baseUri = this.Uris.ResolveUri(baseUri, xmlBase.Value);
        }
        XerXmlAttribute? target = this.FindAttribute(root, "", "targetNamespace");
        XsdSchemaLocation[] locations = this.CollectSchemaLocations(
            input,
            read.Tokens,
            root,
            baseUri);
        if (!this.Resources.AllowsCollectionElements(
            root.NamespaceBindings.Length)
            || !this.Resources.AllowsCollectionElements(locations.Length))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.ResourceLimit,
                "The XML Schema namespace or location count exceeds the configured collection limit.",
                this.CreateTokenSpan(input, root));
            return null;
        }
        return new XsdLoadedDocument
        {
            StableId = new XsdIdentityBuilder().DocumentIdentity(resolverUri),
            Name = input.Name,
            ResolverUri = resolverUri,
            EffectiveBaseUri = baseUri,
            TargetNamespace = target == null ? "" : target.Value,
            NamespaceBindings = this.CopyNamespaceBindings(
                root.NamespaceBindings),
            SchemaLocations = locations,
            Tokens = read.Tokens,
            SchemaToken = root,
            Input = input
        };
    }

    internal XerXmlToken? FindRootToken(XerXmlToken[] tokens)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index].Kind == XerXmlTokenKind.StartElement
                || tokens[index].Kind == XerXmlTokenKind.EmptyElement)
            {
                return tokens[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdSchemaLocation[] CollectSchemaLocations(
        AsnXsdInput input,
        XerXmlToken[] tokens,
        XerXmlToken root,
        string rootBaseUri)
    {
        System.Collections.Generic.List<XsdSchemaLocation> result =
            new System.Collections.Generic.List<XsdSchemaLocation>();
        if (root.Kind == XerXmlTokenKind.EmptyElement)
        {
            return [];
        }
        int depth = 0;
        bool rootSeen = false;
        int index = 0;
        while (index < tokens.Length)
        {
            XerXmlToken token = tokens[index];
            if (token == root)
            {
                rootSeen = true;
                depth = 1;
                index = index + 1;
                continue;
            }
            if (!rootSeen)
            {
                index = index + 1;
                continue;
            }
            if (token.Kind == XerXmlTokenKind.StartElement)
            {
                if (depth == 1)
                {
                    this.AddSchemaLocation(
                        result,
                        input,
                        token,
                        rootBaseUri);
                }
                depth = depth + 1;
            }
            else if (token.Kind == XerXmlTokenKind.EmptyElement)
            {
                if (depth == 1)
                {
                    this.AddSchemaLocation(
                        result,
                        input,
                        token,
                        rootBaseUri);
                }
            }
            else if (token.Kind == XerXmlTokenKind.EndElement)
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    break;
                }
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal void AddSchemaLocation(
        System.Collections.Generic.List<XsdSchemaLocation> result,
        AsnXsdInput input,
        XerXmlToken token,
        string rootBaseUri)
    {
        if (token.NamespaceUri != SchemaNamespace
            || token.LocalName != "include"
                && token.LocalName != "import"
                && token.LocalName != "redefine")
        {
            return;
        }
        XerXmlAttribute? location = this.FindAttribute(
            token,
            "",
            "schemaLocation");
        XerXmlAttribute? namespaceValue = this.FindAttribute(
            token,
            "",
            "namespace");
        string baseUri = rootBaseUri;
        XerXmlAttribute? xmlBase = this.FindAttribute(
            token,
            XerXmlNamespaceContext.XmlNamespace,
            "base");
        if (xmlBase != null)
        {
            if (!this.Uris.IsValidUriReference(xmlBase.Value))
            {
                this.AddDiagnostic(
                    XsdDiagnosticCodes.InvalidXmlBase,
                    "The schema-reference xml:base value is not a valid URI reference.",
                    this.CreateTokenSpan(input, token));
                return;
            }
            baseUri = this.Uris.ResolveUri(baseUri, xmlBase.Value);
        }
        if (location == null)
        {
            if (token.LocalName != "import")
            {
                this.AddDiagnostic(
                    XsdDiagnosticCodes.InvalidSchemaLocation,
                    "XML Schema include and redefine require schemaLocation.",
                    this.CreateTokenSpan(input, token));
            }
            result.Add(new XsdSchemaLocation
            {
                Kind = token.LocalName,
                NamespaceUri = namespaceValue == null
                    ? ""
                    : namespaceValue.Value,
                BaseUri = baseUri,
                Span = this.CreateTokenSpan(input, token)
            });
            return;
        }
        if (location.Value.Length == 0
            || !this.Uris.IsValidUriReference(location.Value))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidSchemaLocation,
                "schemaLocation must be a non-empty URI reference.",
                this.CreateTokenSpan(input, token));
            return;
        }
        result.Add(new XsdSchemaLocation
        {
            Kind = token.LocalName,
            NamespaceUri = namespaceValue == null ? "" : namespaceValue.Value,
            Location = location.Value,
            BaseUri = baseUri,
            ResolvedUri = this.Uris.ResolveUri(baseUri, location.Value),
            Span = this.CreateTokenSpan(input, token)
        });
    }

    internal XerXmlAttribute? FindAttribute(
        XerXmlToken token,
        string namespaceUri,
        string localName)
    {
        int index = 0;
        while (index < token.Attributes.Length)
        {
            XerXmlAttribute candidate = token.Attributes[index];
            if (!candidate.NamespaceDeclaration
                && candidate.NamespaceUri == namespaceUri
                && candidate.LocalName == localName)
            {
                return candidate;
            }
            index = index + 1;
        }
        return null;
    }

    internal XerXmlNamespaceBinding[] CopyNamespaceBindings(
        XerXmlNamespaceBinding[] source)
    {
        XerXmlNamespaceBinding[] result =
            new XerXmlNamespaceBinding[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = new XerXmlNamespaceBinding
            {
                Found = source[index].Found,
                Prefix = source[index].Prefix,
                NamespaceUri = source[index].NamespaceUri
            };
            index = index + 1;
        }
        return result;
    }

    internal bool HasResolverUri(
        System.Collections.Generic.List<XsdResolverInput> inputs,
        string resolverUri)
    {
        int index = 0;
        while (index < inputs.Count)
        {
            if (inputs[index].ResolverUri == resolverUri)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal AsnSourceSpan CreateTokenSpan(
        AsnXsdInput input,
        XerXmlToken token)
    {
        return this.CreateByteSpan(
            input,
            token.ByteOffset,
            token.ByteLength);
    }

    internal AsnSourceSpan CreateByteSpan(
        AsnXsdInput input,
        int byteOffset,
        int byteLength)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input.Text);
        int startByte = byteOffset < 0
            ? 0
            : byteOffset > bytes.Length ? bytes.Length : byteOffset;
        int endByte = byteLength < 0 || byteLength > bytes.Length - startByte
            ? startByte
            : startByte + byteLength;
        int start = System.Text.Encoding.UTF8.GetString(
            bytes,
            0,
            startByte).Length;
        int end = System.Text.Encoding.UTF8.GetString(
            bytes,
            0,
            endByte).Length;
        return this.CreateInputSpan(input, start, end);
    }

    internal AsnSourceSpan CreateInputSpan(
        AsnXsdInput input,
        int start,
        int end)
    {
        return new AsnSourceUnit
        {
            Name = input.Name,
            Text = input.Text
        }.GetSpan(start, end);
    }

    internal void AddDiagnostic(
        int code,
        string message,
        AsnSourceSpan? span)
    {
        if (!this.Resources.TryChargeDiagnostic())
        {
            return;
        }
        this.Diagnostics.Add(new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Category = AsnDiagnosticCategory.Xsd,
            Code = DiagnosticBag.CreateCode(
                AsnDiagnosticCategory.Xsd,
                code),
            Message = message,
            Span = span
        });
    }

    internal XsdDocumentLoadResult CreateResult(
        XsdLoadedDocument[] documents,
        XsdResolverInput[] resolverInputs)
    {
        return new XsdDocumentLoadResult
        {
            Success = this.Diagnostics.Count == 0,
            Documents = documents,
            ResolverInputs = resolverInputs,
            Diagnostics = this.Diagnostics.ToArray()
        };
    }
}
