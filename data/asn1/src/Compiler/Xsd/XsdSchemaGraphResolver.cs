namespace asn1;

internal class XsdSchemaGraphResolver
{
    internal AsnResourceLimits Limits = new AsnResourceLimits();
    internal ResourceCounter Resources =
        new ResourceCounter(new AsnResourceLimits());
    internal XsdLoadedDocument[] Documents = [];
    internal System.Collections.Generic.List<XsdDocumentInstance> Instances =
        new System.Collections.Generic.List<XsdDocumentInstance>();
    internal System.Collections.Generic.List<XsdSchemaGraphEdge> Edges =
        new System.Collections.Generic.List<XsdSchemaGraphEdge>();
    internal System.Collections.Generic.List<XsdComponentDefinition> Components =
        new System.Collections.Generic.List<XsdComponentDefinition>();
    internal System.Collections.Generic.List<AsnDiagnostic> Diagnostics =
        new System.Collections.Generic.List<AsnDiagnostic>();
    internal System.Collections.Generic.List<string> ActiveInstances =
        new System.Collections.Generic.List<string>();
    internal XsdIdentityBuilder Identities = new XsdIdentityBuilder();

    internal XsdSchemaGraphResult ResolveGraph(
        XsdDocumentLoadResult loaded,
        AsnXsdImportOptions options)
    {
        if (loaded == null)
        {
            throw new System.ArgumentNullException("loaded");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        this.Limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        this.Resources = new ResourceCounter(this.Limits);
        this.Documents = loaded.Documents;
        this.Instances = new System.Collections.Generic.List<XsdDocumentInstance>();
        this.Edges = new System.Collections.Generic.List<XsdSchemaGraphEdge>();
        this.Components =
            new System.Collections.Generic.List<XsdComponentDefinition>();
        this.Diagnostics = new System.Collections.Generic.List<AsnDiagnostic>();
        this.ActiveInstances = new System.Collections.Generic.List<string>();
        if (!loaded.Success)
        {
            return this.CreateGraphResult();
        }

        System.Collections.Generic.List<string> referencedDocuments =
            this.FindReferencedDocuments();
        int index = 0;
        while (index < this.Documents.Length)
        {
            if (!this.ContainsText(
                referencedDocuments,
                this.Documents[index].StableId))
            {
                this.VisitDocument(
                    this.Documents[index],
                    this.Documents[index].TargetNamespace);
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.Documents.Length)
        {
            if (!this.HasDocumentInstance(this.Documents[index].StableId))
            {
                this.VisitDocument(
                    this.Documents[index],
                    this.Documents[index].TargetNamespace);
            }
            index = index + 1;
        }
        this.ValidateComponentDuplicates();
        this.ValidateRedefinitions();
        return this.CreateGraphResult();
    }

    internal System.Collections.Generic.List<string> FindReferencedDocuments()
    {
        System.Collections.Generic.List<string> result =
            new System.Collections.Generic.List<string>();
        int documentIndex = 0;
        while (documentIndex < this.Documents.Length)
        {
            XsdLoadedDocument document = this.Documents[documentIndex];
            int locationIndex = 0;
            while (locationIndex < document.SchemaLocations.Length)
            {
                XsdLoadedDocument? target = this.FindDocumentByResolverUri(
                    document.SchemaLocations[locationIndex].ResolvedUri);
                if (target != null
                    && !this.ContainsText(result, target.StableId))
                {
                    result.Add(target.StableId);
                }
                locationIndex = locationIndex + 1;
            }
            documentIndex = documentIndex + 1;
        }
        return result;
    }

    internal XsdDocumentInstance? VisitDocument(
        XsdLoadedDocument document,
        string effectiveNamespace)
    {
        string instanceId = this.Identities.InstanceIdentity(
            document.StableId,
            effectiveNamespace);
        XsdDocumentInstance? existing = this.FindInstance(instanceId);
        if (existing != null)
        {
            return existing;
        }
        if (!this.Resources.AllowsCollectionElements(
            this.Instances.Count + 1)
            || !this.Resources.TryEnterNesting())
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.GraphLimit,
                "Resolving the XML Schema graph exceeds the configured graph or nesting limit.",
                this.DocumentSpan(document),
                []);
            return null;
        }
        XsdDocumentInstance instance = new XsdDocumentInstance
        {
            StableId = instanceId,
            DocumentId = document.StableId,
            EffectiveTargetNamespace = effectiveNamespace,
            Document = document
        };
        this.Instances.Add(instance);
        this.ActiveInstances.Add(instanceId);
        instance.Components = this.CollectComponents(instance);
        int locationIndex = 0;
        while (locationIndex < document.SchemaLocations.Length)
        {
            this.ResolveLocation(
                instance,
                document.SchemaLocations[locationIndex]);
            locationIndex = locationIndex + 1;
        }
        this.ActiveInstances.RemoveAt(this.ActiveInstances.Count - 1);
        this.Resources.LeaveNesting();
        return instance;
    }

    internal void ResolveLocation(
        XsdDocumentInstance source,
        XsdSchemaLocation location)
    {
        if (location.ResolvedUri.Length == 0)
        {
            return;
        }
        XsdLoadedDocument? target = this.FindDocumentByResolverUri(
            location.ResolvedUri);
        if (target == null)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.MissingResolverInput,
                "No supplied AsnXsdInput has resolver URI '"
                    + location.ResolvedUri + "'.",
                location.Span,
                []);
            return;
        }
        string targetNamespace = target.TargetNamespace;
        bool chameleon = false;
        if (location.Kind == "include" || location.Kind == "redefine")
        {
            if (targetNamespace.Length == 0)
            {
                targetNamespace = source.EffectiveTargetNamespace;
                chameleon = true;
            }
            else if (targetNamespace != source.EffectiveTargetNamespace)
            {
                this.AddNamespaceMismatch(
                    location,
                    target,
                    "XML Schema include and redefine require matching target namespaces.");
            }
        }
        else if (location.Kind == "import")
        {
            if (location.NamespaceUri != target.TargetNamespace)
            {
                this.AddNamespaceMismatch(
                    location,
                    target,
                    "The import namespace does not match the imported schema targetNamespace.");
            }
            if (source.EffectiveTargetNamespace.Length > 0
                && source.EffectiveTargetNamespace == target.TargetNamespace)
            {
                this.AddNamespaceMismatch(
                    location,
                    target,
                    "A schema must include rather than import its own target namespace.");
            }
        }
        string targetInstanceId = this.Identities.InstanceIdentity(
            target.StableId,
            targetNamespace);
        bool cycle = this.ContainsText(
            this.ActiveInstances,
            targetInstanceId);
        if (!this.Resources.AllowsCollectionElements(this.Edges.Count + 1))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.GraphLimit,
                "The XML Schema edge count exceeds the configured collection limit.",
                location.Span,
                []);
            return;
        }
        this.Edges.Add(new XsdSchemaGraphEdge
        {
            Kind = location.Kind,
            SourceInstanceId = source.StableId,
            TargetInstanceId = targetInstanceId,
            Chameleon = chameleon,
            Cycle = cycle,
            Location = location
        });
        if (cycle)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.ReferenceCycle,
                "The XML Schema include, import, or redefine graph contains a cycle.",
                location.Span,
                this.RelatedDocumentLocation(
                    "The cyclic target schema is declared here.",
                    target));
            return;
        }
        this.VisitDocument(target, targetNamespace);
    }

    internal void AddNamespaceMismatch(
        XsdSchemaLocation location,
        XsdLoadedDocument target,
        string message)
    {
        this.AddDiagnostic(
            XsdDiagnosticCodes.NamespaceMismatch,
            message,
            location.Span,
            this.RelatedDocumentLocation(
                "The referenced schema target namespace is declared here.",
                target));
    }

    internal XsdComponentDefinition[] CollectComponents(
        XsdDocumentInstance instance)
    {
        XsdLoadedDocument document = instance.Document
            ?? new XsdLoadedDocument();
        System.Collections.Generic.List<XsdComponentDefinition> result =
            new System.Collections.Generic.List<XsdComponentDefinition>();
        if (document.SchemaToken == null
            || document.SchemaToken.Kind == XerXmlTokenKind.EmptyElement)
        {
            return [];
        }
        int depth = 0;
        int redefineDepth = -1;
        bool rootSeen = false;
        int index = 0;
        while (index < document.Tokens.Length)
        {
            XerXmlToken token = document.Tokens[index];
            if (token == document.SchemaToken)
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
                if (depth == 1 && token.NamespaceUri
                    == XsdDocumentLoader.SchemaNamespace
                    && token.LocalName == "redefine")
                {
                    redefineDepth = 2;
                }
                else if (depth == 1
                    || redefineDepth >= 0 && depth == redefineDepth)
                {
                    this.AddComponent(
                        result,
                        instance,
                        token,
                        redefineDepth >= 0 && depth == redefineDepth);
                }
                depth = depth + 1;
            }
            else if (token.Kind == XerXmlTokenKind.EmptyElement)
            {
                if (depth == 1
                    || redefineDepth >= 0 && depth == redefineDepth)
                {
                    this.AddComponent(
                        result,
                        instance,
                        token,
                        redefineDepth >= 0 && depth == redefineDepth);
                }
            }
            else if (token.Kind == XerXmlTokenKind.EndElement)
            {
                depth = depth - 1;
                if (redefineDepth >= 0 && depth < redefineDepth)
                {
                    redefineDepth = -1;
                }
                if (depth == 0)
                {
                    break;
                }
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal void AddComponent(
        System.Collections.Generic.List<XsdComponentDefinition> result,
        XsdDocumentInstance instance,
        XerXmlToken token,
        bool redefinition)
    {
        string kind = this.ComponentKind(token);
        if (kind.Length == 0)
        {
            return;
        }
        XerXmlAttribute? name = new XsdDocumentLoader().FindAttribute(
            token,
            "",
            "name");
        XsdLoadedDocument document = instance.Document
            ?? new XsdLoadedDocument();
        AsnSourceSpan? span = document.Input == null
            ? null
            : new XsdDocumentLoader().CreateTokenSpan(document.Input, token);
        if (name == null || name.Value.Length == 0)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidComponent,
                "A global XML Schema component requires a name.",
                span,
                []);
            return;
        }
        int duplicateOrdinal = this.CountComponentKey(
            result,
            kind,
            name.Value);
        XsdComponentDefinition component = new XsdComponentDefinition
        {
            StableId = this.Identities.ComponentIdentity(
                instance.StableId,
                kind,
                name.Value,
                duplicateOrdinal),
            Kind = kind,
            Name = name.Value,
            NamespaceUri = instance.EffectiveTargetNamespace,
            SourceDocumentId = document.StableId,
            SourceInstanceId = instance.StableId,
            Redefinition = redefinition,
            Token = token,
            Span = span
        };
        result.Add(component);
        if (!this.Resources.AllowsCollectionElements(
            this.Components.Count + 1))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.GraphLimit,
                "The XML Schema component count exceeds the configured collection limit.",
                span,
                []);
            return;
        }
        this.Components.Add(component);
    }

    internal string ComponentKind(XerXmlToken token)
    {
        if (token.NamespaceUri != XsdDocumentLoader.SchemaNamespace)
        {
            return "";
        }
        switch (token.LocalName)
        {
            case "simpleType":
            case "complexType":
            case "element":
            case "attribute":
            case "group":
            case "attributeGroup":
            case "notation":
                return token.LocalName;
            default:
                return "";
        }
    }

    internal int CountComponentKey(
        System.Collections.Generic.List<XsdComponentDefinition> components,
        string kind,
        string name)
    {
        int count = 0;
        int index = 0;
        while (index < components.Count)
        {
            if (components[index].Kind == kind
                && components[index].Name == name)
            {
                count = count + 1;
            }
            index = index + 1;
        }
        return count;
    }

    internal void ValidateComponentDuplicates()
    {
        int index = 0;
        while (index < this.Components.Count)
        {
            XsdComponentDefinition current = this.Components[index];
            int earlier = 0;
            while (earlier < index)
            {
                XsdComponentDefinition previous = this.Components[earlier];
                if (current.Kind == previous.Kind
                    && current.Name == previous.Name
                    && current.NamespaceUri == previous.NamespaceUri
                    && !this.IsLegalRedefinition(current, previous)
                    && !this.IsLegalRedefinition(previous, current))
                {
                    this.AddDiagnostic(
                        XsdDiagnosticCodes.DuplicateComponent,
                        "The XML Schema component {"
                            + current.NamespaceUri + "}"
                            + current.Name + " is declared more than once as "
                            + current.Kind + ".",
                        current.Span,
                        this.RelatedComponentLocation(
                            "The first component declaration is here.",
                            previous));
                    break;
                }
                earlier = earlier + 1;
            }
            index = index + 1;
        }
    }

    internal bool IsLegalRedefinition(
        XsdComponentDefinition redefinition,
        XsdComponentDefinition original)
    {
        if (!redefinition.Redefinition
            || original.Redefinition
            || redefinition.SourceInstanceId == original.SourceInstanceId)
        {
            return false;
        }
        int index = 0;
        while (index < this.Edges.Count)
        {
            XsdSchemaGraphEdge edge = this.Edges[index];
            if (edge.Kind == "redefine"
                && edge.SourceInstanceId == redefinition.SourceInstanceId
                && edge.TargetInstanceId == original.SourceInstanceId)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void ValidateRedefinitions()
    {
        int index = 0;
        while (index < this.Components.Count)
        {
            XsdComponentDefinition component = this.Components[index];
            if (component.Redefinition
                && !this.HasRedefinedOriginal(component))
            {
                this.AddDiagnostic(
                    XsdDiagnosticCodes.MissingRedefinedComponent,
                    "The redefined XML Schema component has no matching component in the referenced schema.",
                    component.Span,
                    []);
            }
            index = index + 1;
        }
    }

    internal bool HasRedefinedOriginal(XsdComponentDefinition component)
    {
        int edgeIndex = 0;
        while (edgeIndex < this.Edges.Count)
        {
            XsdSchemaGraphEdge edge = this.Edges[edgeIndex];
            if (edge.Kind == "redefine"
                && edge.SourceInstanceId == component.SourceInstanceId)
            {
                int componentIndex = 0;
                while (componentIndex < this.Components.Count)
                {
                    XsdComponentDefinition candidate =
                        this.Components[componentIndex];
                    if (!candidate.Redefinition
                        && candidate.SourceInstanceId == edge.TargetInstanceId
                        && candidate.Kind == component.Kind
                        && candidate.Name == component.Name
                        && candidate.NamespaceUri == component.NamespaceUri)
                    {
                        return true;
                    }
                    componentIndex = componentIndex + 1;
                }
            }
            edgeIndex = edgeIndex + 1;
        }
        return false;
    }

    internal XsdLoadedDocument? FindDocumentByResolverUri(string resolverUri)
    {
        int index = 0;
        while (index < this.Documents.Length)
        {
            if (this.Documents[index].ResolverUri == resolverUri)
            {
                return this.Documents[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdDocumentInstance? FindInstance(string stableId)
    {
        int index = 0;
        while (index < this.Instances.Count)
        {
            if (this.Instances[index].StableId == stableId)
            {
                return this.Instances[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal bool HasDocumentInstance(string documentId)
    {
        int index = 0;
        while (index < this.Instances.Count)
        {
            if (this.Instances[index].DocumentId == documentId)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal bool ContainsText(
        System.Collections.Generic.List<string> values,
        string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal AsnSourceSpan? DocumentSpan(XsdLoadedDocument document)
    {
        return document.Input == null || document.SchemaToken == null
            ? null
            : new XsdDocumentLoader().CreateTokenSpan(
                document.Input,
                document.SchemaToken);
    }

    internal AsnDiagnosticRelatedLocation[] RelatedDocumentLocation(
        string message,
        XsdLoadedDocument document)
    {
        AsnSourceSpan? span = this.DocumentSpan(document);
        return span == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = message,
                Span = span
            }];
    }

    internal AsnDiagnosticRelatedLocation[] RelatedComponentLocation(
        string message,
        XsdComponentDefinition component)
    {
        return component.Span == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = message,
                Span = component.Span
            }];
    }

    internal void AddDiagnostic(
        int code,
        string message,
        AsnSourceSpan? span,
        AsnDiagnosticRelatedLocation[] related)
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
            Span = span,
            RelatedLocations = related
        });
    }

    internal XsdSchemaGraphResult CreateGraphResult()
    {
        return new XsdSchemaGraphResult
        {
            Success = this.Diagnostics.Count == 0,
            DocumentInstances = this.Instances.ToArray(),
            Edges = this.Edges.ToArray(),
            Components = this.Components.ToArray(),
            Diagnostics = this.Diagnostics.ToArray()
        };
    }
}
