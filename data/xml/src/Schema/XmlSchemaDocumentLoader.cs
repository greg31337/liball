using System;
using System.Collections.Generic;

namespace xml;

internal class XmlSchemaDocumentLoader
{
    internal XmlSchemaValidationOptions Options;
    internal IXmlResourceResolver Resolver;
    internal XmlResolverPolicy Policy;
    internal List<XmlDocument> Documents;
    internal List<XmlSchemaModule> Modules;
    internal Dictionary<string, XmlDocument> DocumentsByLocation;
    internal Dictionary<string, XmlSchemaModule> ModulesByKey;
    internal int ExternalResourceCount;
    internal int CurrentResourceDepth;
    internal int MaximumExternalResources;
    internal int MaximumResourceDepth;
    internal int AnonymousDocumentCount;

    internal XmlSchemaDocumentLoader(XmlSchemaValidationOptions options, IXmlResourceResolver resolver)
    {
        Guard.NotNull(options, "options");
        Guard.NotNull(resolver, "resolver");
        Options = options;
        Policy = options.ResolverPolicy == null ? CreateDefaultXmlSchemaResolverPolicy() : options.ResolverPolicy;
        ValidateXmlSchemaResolverPolicy();
        XmlCatalogEntry[]? catalogs = Policy.CatalogEntries;
        Resolver = catalogs == null || catalogs.Length == 0 ? resolver : new ResourceManager(resolver, Policy);
        Documents = new List<XmlDocument>();
        Modules = new List<XmlSchemaModule>();
        DocumentsByLocation = new Dictionary<string, XmlDocument>(StringComparer.Ordinal);
        ModulesByKey = new Dictionary<string, XmlSchemaModule>(StringComparer.Ordinal);
        ExternalResourceCount = 0;
        CurrentResourceDepth = 0;
        AnonymousDocumentCount = 0;
        XmlSecurityLimits? limits = options.SecurityLimits;
        MaximumExternalResources = limits == null ? 64 : limits.MaximumExternalResources;
        MaximumResourceDepth = limits == null ? 16 : limits.MaximumResourceDepth;
        if (Policy.MaximumDepth < MaximumResourceDepth)
        {
            MaximumResourceDepth = Policy.MaximumDepth;
        }
    }

    internal XmlSchemaSet CompileXmlSchemaInputs(XmlInput[] schemas)
    {
        Guard.NotNull(schemas, "schemas");
        int index = 0;
        while (index < schemas.Length)
        {
            XmlInput? input = schemas[index];
            if (input == null)
            {
                throw new ArgumentException("Schema input entries cannot be null.", "schemas");
            }

            LoadRootXmlSchemaInput(input);
            index = index + 1;
        }

        XmlSchemaModule[] compiledModules = Modules.ToArray();
        XmlSchemaSimpleTypeCompiler simpleCompiler = new XmlSchemaSimpleTypeCompiler(compiledModules);
        simpleCompiler.CompileXmlSchemaSimpleTypes();
        XmlSchemaComplexTypeCompiler complexCompiler = new XmlSchemaComplexTypeCompiler(compiledModules, simpleCompiler);
        complexCompiler.CompileXmlSchemaComplexTypes();
        XmlSchemaDeclarationCompiler declarationCompiler = new XmlSchemaDeclarationCompiler(compiledModules, simpleCompiler, complexCompiler);
        declarationCompiler.CompileXmlSchemaDeclarations();
        CompiledXmlSchemaSimpleType[] simpleTypes = simpleCompiler.Types.ToArray();
        CompiledXmlSchemaComplexType[] complexTypes = complexCompiler.Types.ToArray();
        FreezeLoadedXmlSchemaDocuments();
        XmlSchemaSet result = new XmlSchemaSet();
        result.SchemaDocuments = Documents.ToArray();
        result.TargetNamespaces = CollectCompiledXmlSchemaNamespaces(compiledModules);
        CompiledXmlSchemaSet compiledSet = new CompiledXmlSchemaSet(compiledModules, simpleTypes, complexTypes, complexCompiler.Groups.ToArray(), complexCompiler.AttributeGroups.ToArray(), declarationCompiler.Elements.ToArray(), declarationCompiler.Attributes.ToArray(), declarationCompiler.IdentityConstraints.ToArray(), declarationCompiler.Notations.ToArray());
        result.CompiledSet = compiledSet;
        result.CompiledSchemaSet = compiledSet;
        result.Diagnostics = [];
        result.IsImmutable = true;
        return result;
    }

    internal XmlSchemaModule LoadRootXmlSchemaInput(XmlInput input)
    {
        string? location = NormalizeRootXmlSchemaLocation(input.BaseUri);
        XmlDocument document;
        if (location != null && DocumentsByLocation.ContainsKey(location))
        {
            document = DocumentsByLocation[location];
        }
        else
        {
            document = ParseXmlSchemaInput(input);
            RegisterPhysicalXmlSchemaDocument(document, location);
        }

        return RegisterCompiledXmlSchemaModule(document, location, XmlSchemaDirectiveKind.Root, null, null);
    }

    internal XmlSchemaModule? LoadReferencedXmlSchema(XmlSchemaModule parent, XmlSchemaDirective directive)
    {
        string? schemaLocation = directive.SchemaLocation;
        if (schemaLocation == null)
        {
            return null;
        }

        string? baseUri = new TreeNavigation().GetXmlNodeBaseUriOperation(directive.Node);
        string requestedLocation = ResolveXmlSchemaLocation(schemaLocation, baseUri);
        XmlDocument document;
        string actualLocation;
        if (DocumentsByLocation.ContainsKey(requestedLocation))
        {
            document = DocumentsByLocation[requestedLocation];
            actualLocation = requestedLocation;
            string? cachedImportNamespace = directive.Kind == XmlSchemaDirectiveKind.Import ? directive.NamespaceUri : null;
            return RegisterCompiledXmlSchemaModule(document, actualLocation, directive.Kind, parent.EffectiveTargetNamespace, cachedImportNamespace);
        }
        else
        {
            EnterXmlSchemaResource(directive.Node);
            XmlResolvedResource resource = ResolveExternalXmlSchemaResource(schemaLocation, baseUri, directive.Node);
            actualLocation = NormalizeResolvedXmlSchemaLocation(resource, requestedLocation, directive.Node);
            XmlInput? resolvedInput = resource.Input;
            if (resolvedInput == null)
            {
                throw CreateXmlSchemaResourceError(directive.Node, XmlErrorCodes.ResourceNotFound, "The schema resolver allowed a resource without supplying input.");
            }

            if (DocumentsByLocation.ContainsKey(actualLocation))
            {
                document = DocumentsByLocation[actualLocation];
            }
            else
            {
                if (resolvedInput.BaseUri == null)
                {
                    resolvedInput.BaseUri = actualLocation;
                }

                if (resource.OwnsResource)
                {
                    resolvedInput.OwnsResource = true;
                }

                document = ParseXmlSchemaInput(resolvedInput);
                RegisterPhysicalXmlSchemaDocument(document, actualLocation);
            }

            if (!DocumentsByLocation.ContainsKey(requestedLocation))
            {
                DocumentsByLocation.Add(requestedLocation, document);
            }

            string? loadedImportNamespace = directive.Kind == XmlSchemaDirectiveKind.Import ? directive.NamespaceUri : null;
            XmlSchemaModule loaded = RegisterCompiledXmlSchemaModule(document, actualLocation, directive.Kind, parent.EffectiveTargetNamespace, loadedImportNamespace);
            LeaveXmlSchemaResource();
            return loaded;
        }
    }

    internal XmlSchemaModule RegisterCompiledXmlSchemaModule(XmlDocument document, string? location, XmlSchemaDirectiveKind relation, string? parentNamespace, string? importNamespace)
    {
        XmlNode root = RequireXmlSchemaRoot(document);
        string? declaredNamespace = ReadXmlSchemaTargetNamespace(root);
        string? effectiveNamespace = ResolveEffectiveXmlSchemaNamespace(root, declaredNamespace, relation, parentNamespace, importNamespace);
        bool chameleon = relation != XmlSchemaDirectiveKind.Root && relation != XmlSchemaDirectiveKind.Import && declaredNamespace == null && effectiveNamespace != null;
        string key = CreateCompiledXmlSchemaModuleKey(location, effectiveNamespace, document);
        if (ModulesByKey.ContainsKey(key))
        {
            return ModulesByKey[key];
        }

        XmlSchemaModule module = new XmlSchemaModule(document, location, declaredNamespace, effectiveNamespace, relation, chameleon);
        ModulesByKey.Add(key, module);
        Modules.Add(module);
        ProcessCompiledXmlSchemaDirectives(module, root);
        module.IsComplete = true;
        return module;
    }

    internal void ProcessCompiledXmlSchemaDirectives(XmlSchemaModule module, XmlNode root)
    {
        List<XmlSchemaDirective> directives = new List<XmlSchemaDirective>();
        XmlNode[] children = root.Children == null ? [] : root.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            XmlSchemaDirectiveKind kind = ClassifyXmlSchemaDirective(child);
            if (kind != XmlSchemaDirectiveKind.Root)
            {
                XmlSchemaDirective directive = CreateCompiledXmlSchemaDirective(child, kind, module.EffectiveTargetNamespace);
                directives.Add(directive);
                directive.TargetModule = LoadReferencedXmlSchema(module, directive);
            }

            index = index + 1;
        }

        module.Directives = directives.ToArray();
    }

    internal XmlSchemaDirective CreateCompiledXmlSchemaDirective(XmlNode node, XmlSchemaDirectiveKind kind, string? parentNamespace)
    {
        string? locationValue = FindXmlSchemaAttribute(node, "schemaLocation", "");
        string? schemaLocation = locationValue == null ? null : NormalizeXmlSchemaAnyUri(node, locationValue, "schemaLocation", true);
        if (kind != XmlSchemaDirectiveKind.Import && schemaLocation == null)
        {
            throw CreateXmlSchemaError(node, "The xs:" + XmlSchemaDirectiveLocalName(kind) + " directive requires a non-empty schemaLocation attribute.");
        }

        string? namespaceValue = FindXmlSchemaAttribute(node, "namespace", "");
        string? namespaceUri = namespaceValue == null ? null : NormalizeXmlSchemaAnyUri(node, namespaceValue, "namespace", false);
        if (kind == XmlSchemaDirectiveKind.Import && string.Equals(namespaceUri, parentNamespace, StringComparison.Ordinal))
        {
            throw CreateXmlSchemaError(node, "An xs:import directive must import a namespace different from the enclosing schema target namespace.");
        }

        return new XmlSchemaDirective(kind, node, schemaLocation, namespaceUri);
    }

    internal XmlSchemaDirectiveKind ClassifyXmlSchemaDirective(XmlNode node)
    {
        if (node.Kind != XmlNodeKind.Element || XmlSchemaNodeNamespace(node) != XmlNamespaces.XmlSchema)
        {
            return XmlSchemaDirectiveKind.Root;
        }

        string localName = XmlSchemaNodeLocalName(node);
        if (localName == "include")
        {
            return XmlSchemaDirectiveKind.Include;
        }

        if (localName == "import")
        {
            return XmlSchemaDirectiveKind.Import;
        }

        if (localName == "redefine")
        {
            return XmlSchemaDirectiveKind.Redefine;
        }

        if (localName == "override")
        {
            return XmlSchemaDirectiveKind.Override;
        }

        return XmlSchemaDirectiveKind.Root;
    }

    internal string? ResolveEffectiveXmlSchemaNamespace(XmlNode root, string? declaredNamespace, XmlSchemaDirectiveKind relation, string? parentNamespace, string? importNamespace)
    {
        if (relation == XmlSchemaDirectiveKind.Root)
        {
            return declaredNamespace;
        }

        if (relation == XmlSchemaDirectiveKind.Import)
        {
            if (!string.Equals(declaredNamespace, importNamespace, StringComparison.Ordinal))
            {
                throw CreateXmlSchemaError(root, "The imported schema target namespace does not match the xs:import namespace.");
            }

            return declaredNamespace;
        }

        if (declaredNamespace == null)
        {
            return parentNamespace;
        }

        if (!string.Equals(declaredNamespace, parentNamespace, StringComparison.Ordinal))
        {
            throw CreateXmlSchemaError(root, "An included, redefined, or overridden schema must have no target namespace or the same target namespace as its including schema.");
        }

        return declaredNamespace;
    }

    internal XmlNode RequireXmlSchemaRoot(XmlDocument document)
    {
        XmlNode? root = document.DocumentElement;
        if (root == null || root.Kind != XmlNodeKind.Element || XmlSchemaNodeNamespace(root) != XmlNamespaces.XmlSchema || XmlSchemaNodeLocalName(root) != "schema")
        {
            throw CreateXmlSchemaError(root, "An XML Schema document must have an xs:schema document element.");
        }

        return root;
    }

    internal string? ReadXmlSchemaTargetNamespace(XmlNode root)
    {
        string? value = FindXmlSchemaAttribute(root, "targetNamespace", "");
        if (value == null)
        {
            return null;
        }

        return NormalizeXmlSchemaAnyUri(root, value, "targetNamespace", false);
    }

    internal string NormalizeXmlSchemaAnyUri(XmlNode node, string value, string attributeName, bool requireLocation)
    {
        XdmAtomicValue parsed = new XdmAnyUriDatatype().ParseXdmAnyUri(value);
        string normalized = parsed.StringValue == null ? "" : parsed.StringValue;
        if (normalized.Length == 0)
        {
            string purpose = requireLocation ? "resource location" : "namespace name";
            throw CreateXmlSchemaError(node, "The " + attributeName + " attribute must provide a non-empty " + purpose + ".");
        }

        if (requireLocation && UriTools.HasFragment(normalized))
        {
            throw CreateXmlSchemaError(node, "Schema locations must not contain fragment identifiers.");
        }

        return normalized;
    }

    internal XmlResolvedResource ResolveExternalXmlSchemaResource(string schemaLocation, string? baseUri, XmlNode node)
    {
        if (Policy.Access != XmlResourceAccess.Allowed)
        {
            throw CreateXmlSchemaResourceError(node, XmlErrorCodes.ResourceDenied, "External schema resource access is denied by policy.");
        }

        XmlResolvedResource? resource = Resolver.ResolveXmlResource(XmlResourceKind.Schema, null, schemaLocation, baseUri, Policy);
        if (resource == null)
        {
            throw CreateXmlSchemaResourceError(node, XmlErrorCodes.ResourceNotFound, "The schema resolver did not return a resource.");
        }

        if (resource.Access != XmlResourceAccess.Allowed)
        {
            throw CreateXmlSchemaResourceError(node, XmlErrorCodes.ResourceDenied, "The schema resolver denied the requested resource.");
        }

        if (resource.Kind != XmlResourceKind.Schema)
        {
            throw CreateXmlSchemaResourceError(node, XmlErrorCodes.ResourceNotFound, "The schema resolver returned a resource of the wrong kind.");
        }

        return resource;
    }

    internal void EnterXmlSchemaResource(XmlNode node)
    {
        int resourceCount = ExternalResourceCount + 1;
        if (resourceCount > MaximumExternalResources)
        {
            throw CreateXmlSchemaResourceError(node, XmlErrorCodes.LimitExceeded, "The configured external schema resource count was exceeded.");
        }

        int depth = CurrentResourceDepth + 1;
        if (depth > MaximumResourceDepth)
        {
            throw CreateXmlSchemaResourceError(node, XmlErrorCodes.LimitExceeded, "The configured nested schema resource depth was exceeded.");
        }

        ExternalResourceCount = resourceCount;
        CurrentResourceDepth = depth;
    }

    internal void LeaveXmlSchemaResource()
    {
        CurrentResourceDepth = CurrentResourceDepth - 1;
    }

    internal XmlDocument ParseXmlSchemaInput(XmlInput input)
    {
        XmlParserOptions parserOptions = new XmlParserOptions();
        parserOptions.Version = XmlVersion.Xml10;
        parserOptions.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        parserOptions.DtdProcessing = XmlDtdProcessing.Prohibit;
        parserOptions.EntityHandling = XmlEntityHandling.Expand;
        parserOptions.ValidationMode = XmlValidationMode.None;
        parserOptions.PreservePrefixes = true;
        parserOptions.PreserveSourceLocations = true;
        parserOptions.SecurityLimits = Options.SecurityLimits;
        parserOptions.ResolverPolicy = Policy;
        return new XmlParser().ParseXmlDocument(input, parserOptions, Resolver, null, null);
    }

    internal void RegisterPhysicalXmlSchemaDocument(XmlDocument document, string? location)
    {
        if (!Documents.Contains(document))
        {
            Documents.Add(document);
        }

        if (location != null && !DocumentsByLocation.ContainsKey(location))
        {
            DocumentsByLocation.Add(location, document);
        }
    }

    internal string NormalizeResolvedXmlSchemaLocation(XmlResolvedResource resource, string requestedLocation, XmlNode node)
    {
        string location = resource.AbsoluteUri == null ? requestedLocation : resource.AbsoluteUri;
        Uri uri = UriTools.CreateAbsolute(location);
        if (UriTools.HasFragment(uri.OriginalString))
        {
            throw CreateXmlSchemaResourceError(node, XmlErrorCodes.ResourceDenied, "Resolved schema resource URIs must not contain fragment identifiers.");
        }

        return UriTools.CanonicalAbsoluteUri(uri);
    }

    internal string ResolveXmlSchemaLocation(string schemaLocation, string? baseUri)
    {
        Uri resolved;
        if (baseUri == null)
        {
            resolved = UriTools.CreateAbsolute(schemaLocation);
        }
        else
        {
            resolved = UriTools.ResolveReferenceWithFragmentPolicy(UriTools.CreateAbsolute(baseUri), schemaLocation, UriFragmentPolicy.Reject);
        }

        return UriTools.CanonicalAbsoluteUri(resolved);
    }

    internal string? NormalizeRootXmlSchemaLocation(string? baseUri)
    {
        if (baseUri == null || baseUri.Length == 0)
        {
            return null;
        }

        return UriTools.CanonicalAbsoluteUri(UriTools.CreateAbsolute(baseUri));
    }

    internal string CreateCompiledXmlSchemaModuleKey(string? location, string? effectiveNamespace, XmlDocument document)
    {
        string documentKey;
        if (location == null)
        {
            AnonymousDocumentCount = AnonymousDocumentCount + 1;
            documentKey = "anonymous:" + AnonymousDocumentCount;
        }
        else
        {
            documentKey = location;
        }

        string namespaceKey = effectiveNamespace == null ? "<absent>" : effectiveNamespace;
        return documentKey + "\u001F" + namespaceKey;
    }

    internal string[] CollectCompiledXmlSchemaNamespaces(XmlSchemaModule[] compiledModules)
    {
        List<string> namespaces = new List<string>();
        int index = 0;
        while (index < compiledModules.Length)
        {
            string? optionalValue = compiledModules[index].EffectiveTargetNamespace;
            string value = optionalValue == null ? "" : optionalValue;
            if (!ContainsCompiledXmlSchemaNamespace(namespaces, value))
            {
                namespaces.Add(value);
            }

            index = index + 1;
        }

        return namespaces.ToArray();
    }

    internal bool ContainsCompiledXmlSchemaNamespace(List<string> namespaces, string value)
    {
        int index = 0;
        while (index < namespaces.Count)
        {
            if (string.Equals(namespaces[index], value, StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void FreezeLoadedXmlSchemaDocuments()
    {
        TreeContentMutation mutation = new TreeContentMutation(new TreeMutation());
        int index = 0;
        while (index < Documents.Count)
        {
            XmlDocument document = Documents[index];
            XmlNode? root = document.DocumentNode;
            if (root != null)
            {
                mutation.FreezeXmlTreeOperation(root, true);
            }

            document.IsReadOnly = true;
            index = index + 1;
        }
    }

    internal string? FindXmlSchemaAttribute(XmlNode node, string localName, string namespaceUri)
    {
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            if (attribute.Kind == XmlNodeKind.Attribute && XmlSchemaNodeLocalName(attribute) == localName && XmlSchemaNodeNamespace(attribute) == namespaceUri)
            {
                return attribute.Value == null ? "" : attribute.Value;
            }

            index = index + 1;
        }

        return null;
    }

    internal string XmlSchemaNodeLocalName(XmlNode node)
    {
        XmlName? name = node.Name;
        return name == null || name.LocalName == null ? "" : name.LocalName;
    }

    internal string XmlSchemaNodeNamespace(XmlNode node)
    {
        XmlName? name = node.Name;
        return name == null || name.NamespaceUri == null ? "" : name.NamespaceUri;
    }

    internal string XmlSchemaDirectiveLocalName(XmlSchemaDirectiveKind kind)
    {
        if (kind == XmlSchemaDirectiveKind.Include)
        {
            return "include";
        }

        if (kind == XmlSchemaDirectiveKind.Import)
        {
            return "import";
        }

        if (kind == XmlSchemaDirectiveKind.Redefine)
        {
            return "redefine";
        }

        return "override";
    }

    internal XmlResolverPolicy CreateDefaultXmlSchemaResolverPolicy()
    {
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = XmlResourceAccess.Denied;
        policy.AllowRedirects = false;
        policy.MaximumRedirects = 0;
        policy.MaximumDepth = 16;
        policy.MaximumResourceBytes = 67108864;
        policy.MaximumWorkUnits = 67108864;
        policy.CacheResources = false;
        return policy;
    }

    internal void ValidateXmlSchemaResolverPolicy()
    {
        Guard.NonNegativeInt(Policy.MaximumRedirects, "options.ResolverPolicy.MaximumRedirects");
        Guard.NonNegativeInt(Policy.MaximumDepth, "options.ResolverPolicy.MaximumDepth");
        Guard.NonNegative(Policy.MaximumResourceBytes, "options.ResolverPolicy.MaximumResourceBytes");
        Guard.NonNegative(Policy.MaximumWorkUnits, "options.ResolverPolicy.MaximumWorkUnits");
        XmlSecurityLimits? limits = Options.SecurityLimits;
        if (limits != null)
        {
            Guard.NonNegativeInt(limits.MaximumExternalResources, "options.SecurityLimits.MaximumExternalResources");
            Guard.NonNegativeInt(limits.MaximumResourceDepth, "options.SecurityLimits.MaximumResourceDepth");
        }
    }

    internal XmlSchemaException CreateXmlSchemaError(XmlNode? node, string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.SchemaFailure;
        diagnostic.Category = XmlErrorCategory.Schema;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = node == null ? null : node.Range;
        SourceLocation? location = diagnostic.Range == null ? null : diagnostic.Range.Start;
        return new XmlSchemaException(message, diagnostic, location);
    }

    internal XmlResourceException CreateXmlSchemaResourceError(XmlNode node, string code, string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = code == XmlErrorCodes.LimitExceeded ? XmlErrorCategory.Limit : XmlErrorCategory.Resource;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = node.Range;
        SourceLocation? location = node.Range == null ? null : node.Range.Start;
        return new XmlResourceException(message, diagnostic, location);
    }
}
