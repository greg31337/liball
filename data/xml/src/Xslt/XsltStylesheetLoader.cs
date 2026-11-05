using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XsltStylesheetLoader
{
    internal XsltCompileOptions Options;
    internal IXmlResourceResolver Resolver;
    internal XmlResolverPolicy Policy;
    internal ResolverGuard ResourceGuard;
    internal List<XsltStylesheetModule> Modules;
    internal List<string> ActiveLocations;
    internal int ExternalResourceCount;
    internal int MaximumExternalResources;
    internal int MaximumResourceDepth;
    internal XsltPackageCompilationContext PackageContext;
    internal bool IsLibraryPackage;
    internal XsltStaticPreprocessor StaticProcessor;
    internal XPathFunctionBinding[] StylesheetFunctions;

    internal XsltStylesheetLoader(XsltCompileOptions options, IXmlResourceResolver resolver, XsltPackageCompilationContext packageContext, bool isLibraryPackage)
    {
        Guard.NotNull(options, "options");
        Guard.NotNull(resolver, "resolver");
        Options = CopyXsltCompileOptions(options);
        ValidateXsltExtensionOptions(Options);
        Policy = Options.ResolverPolicy == null ? CreateDefaultXsltResolverPolicy() : Options.ResolverPolicy;
        XmlCatalogEntry[]? catalogs = Policy.CatalogEntries;
        Resolver = catalogs == null || catalogs.Length == 0 ? resolver : new ResourceManager(resolver, Policy);
        ResourceGuard = new ResolverGuard(Policy, null);
        Modules = new List<XsltStylesheetModule>();
        ActiveLocations = new List<string>();
        ExternalResourceCount = 0;
        XmlSecurityLimits? limits = Options.SecurityLimits;
        MaximumExternalResources = limits == null ? 64 : limits.MaximumExternalResources;
        MaximumResourceDepth = limits == null ? 16 : limits.MaximumResourceDepth;
        PackageContext = packageContext;
        IsLibraryPackage = isLibraryPackage;
        StaticProcessor = new XsltStaticPreprocessor(this);
        StylesheetFunctions = [];
        if (Policy.MaximumDepth < MaximumResourceDepth)
        {
            MaximumResourceDepth = Policy.MaximumDepth;
        }
    }

    internal XsltExecutable CompileXsltStylesheet(XmlInput stylesheet)
    {
        Guard.NotNull(stylesheet, "stylesheet");
        XmlInput input = CopyXsltInput(stylesheet, stylesheet.BaseUri == null ? Options.BaseUri : stylesheet.BaseUri);
        string? location = NormalizePrincipalXsltLocation(input.BaseUri);
        input.BaseUri = location;
        XmlDocument document = ParseXsltDocument(input, true, null, location);
        XsltStylesheetModule principal = RegisterXsltModule(document, XsltStylesheetModuleRelation.Principal, location, null);
        XsltStylesheetPlan plan = new XsltStylesheetPlan(principal, Modules.ToArray());
        new XsltStylesheetAssembler(this).AssembleXsltStylesheet(plan);
        StaticProcessor.CompleteXsltStaticVariables(plan);
        new XsltStaticContextCompiler(this).CompileXsltStaticContexts(plan);
        new XsltFormatCompiler(this).CompileXsltFormats(plan);
        new XsltDeclarationCompiler(this).CompileXsltDeclarations(plan);
        XsltModeCompiler modeCompiler = new XsltModeCompiler(this);
        modeCompiler.CompileXsltModes(plan);
        XsltFunctionCompiler functionCompiler = new XsltFunctionCompiler(this);
        functionCompiler.DeclareXsltFunctions(plan);
        XsltPackagePlan packagePlan = new XsltPackageCompiler(this).CompileXsltPackage(plan);
        plan.Package = packagePlan;
        modeCompiler.BindXsltModes(plan);
        functionCompiler.BindXsltFunctions(plan);
        new XsltVariableCompiler(this).CompileXsltVariables(plan);
        new XsltValueTemplateCompiler(this).CompileXsltValueTemplates(plan);
        new XsltVariableCompiler(this).CompleteXsltVariableDependencies(plan);
        new XsltInstructionCompiler(this).CompileXsltInstructions(plan);
        new XsltTemplateCompiler(this).CompileXsltTemplates(plan);
        new XsltAttributeSetCompiler(this).CompileXsltAttributeSets(plan);
        new XsltKeyCompiler(this).CompileXsltKeys(plan);
        new XsltAccumulatorCompiler(this).CompileXsltAccumulators(plan);
        new XsltPatternCompiler(this).CompileXsltPatterns(plan);
        new XsltTemplateRuleCompiler(this).CompileXsltTemplateRules(plan);
        functionCompiler.CompileXsltFunctionBodies(plan);
        FreezeXsltDocuments();
        return new XsltExecutableBuilder(this).BuildXsltExecutable(plan, packagePlan);
    }

    internal XmlName? CreateXsltPackagePublicName(string? name)
    {
        if (name == null)
        {
            return Options.PackageName;
        }

        XmlName result = new XmlName();
        result.LocalName = name;
        result.QualifiedName = name;
        return result;
    }

    internal XsltStylesheetModule RegisterXsltModule(XmlDocument document, XsltStylesheetModuleRelation relation, string? location, XmlNode? referenceNode)
    {
        XmlNode root = RequireXsltDocumentElement(document, referenceNode, location);
        XsltStylesheetModuleKind kind = ClassifyXsltModuleRoot(root, relation, location);
        StaticProcessor.ProcessXsltRootShadowAttributes(root, location);
        string version = ReadXsltModuleVersion(root, kind, location);
        XsltStylesheetModule module = new XsltStylesheetModule(document, root, kind, relation, version, location);
        Modules.Add(module);
        if (location != null)
        {
            ActiveLocations.Add(location);
        }

        ProcessXsltModuleReferences(module);
        if (location != null)
        {
            ActiveLocations.RemoveAt(ActiveLocations.Count - 1);
        }

        module.IsComplete = true;
        return module;
    }

    internal void ProcessXsltModuleReferences(XsltStylesheetModule module)
    {
        List<XsltStylesheetReference> references = new List<XsltStylesheetReference>();
        if (!StaticProcessor.ProcessXsltModuleRoot(module.Root, module))
        {
            RemoveXsltModuleRootChildren(module.Root);
            module.References = [];
            return;
        }

        if (module.Kind == XsltStylesheetModuleKind.Simplified)
        {
            StaticProcessor.ProcessXsltElementTree(module.Root, module);
            module.References = [];
            return;
        }

        XmlNode[] children = module.Root.Children == null ? [] : module.Root.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element && !StaticProcessor.ProcessXsltElementTree(child, module))
            {
                new TreeMutation().RemoveXmlTreeNodeOperation(module.Root, child);
                index = index + 1;
                continue;
            }

            XsltStylesheetModuleRelation relation = ClassifyXsltModuleReference(child);
            if (relation != XsltStylesheetModuleRelation.Principal)
            {
                string? hrefValue = FindXsltAttribute(child, "href", "");
                if (hrefValue == null || hrefValue.Trim().Length == 0)
                {
                    throw CreateXsltStaticError("An xsl:include or xsl:import declaration requires a non-empty href attribute.", XsltErrorCodes.XTSE0010, child, module.BaseUri);
                }

                string href = hrefValue.Trim();
                string? baseUri = ReadXsltNodeBaseUri(child, module.BaseUri);
                XsltStylesheetReference reference = new XsltStylesheetReference(module, relation, child, href, baseUri);
                references.Add(reference);
                reference.Target = LoadReferencedXsltModule(reference);
                reference.Target.Origin = reference;
            }

            index = index + 1;
        }

        module.References = references.ToArray();
    }

    internal void RemoveXsltModuleRootChildren(XmlNode root)
    {
        XmlNode[] children = root.Children == null ? [] : root.Children;
        TreeMutation mutation = new TreeMutation();
        int index = 0;
        while (index < children.Length)
        {
            mutation.RemoveXmlTreeNodeOperation(root, children[index]);
            index = index + 1;
        }
    }

    internal XsltStylesheetModule LoadReferencedXsltModule(XsltStylesheetReference reference)
    {
        EnterXsltExternalResource(reference.Node);
        XmlResolvedResource resource = ResolveExternalXsltResource(reference);
        string location = resource.AbsoluteUri == null ? "" : resource.AbsoluteUri;
        if (location.Length == 0)
        {
            throw CreateXsltStaticError("The stylesheet resolver did not provide an absolute module URI.", XsltErrorCodes.XTSE0165, reference.Node, reference.BaseUri);
        }

        if (ContainsXsltLocation(ActiveLocations, location))
        {
            string code = reference.Relation == XsltStylesheetModuleRelation.Import ? XsltErrorCodes.XTSE0210 : XsltErrorCodes.XTSE0180;
            throw CreateXsltStaticError("A stylesheet module directly or indirectly references itself.", code, reference.Node, location);
        }

        XmlInput? optionalInput = resource.Input;
        if (optionalInput == null)
        {
            throw CreateXsltStaticError("The stylesheet resolver allowed a module without supplying input.", XsltErrorCodes.XTSE0165, reference.Node, location);
        }

        XmlInput input = CopyXsltInput(optionalInput, location);
        if (resource.OwnsResource)
        {
            input.OwnsResource = true;
        }

        XmlDocument document = ParseXsltDocument(input, false, reference.Node, location);
        XsltStylesheetModule module = RegisterXsltModule(document, reference.Relation, location, reference.Node);
        return module;
    }

    internal XmlResolvedResource ResolveExternalXsltResource(XsltStylesheetReference reference)
    {
        Task<XmlResolvedResource> pending = Task.Run(() => ResolveExternalXsltResourceCore(reference));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion || pending.Result == null)
        {
            throw CreateXsltStaticError("The referenced stylesheet module could not be retrieved.", XsltErrorCodes.XTSE0165, reference.Node, reference.BaseUri);
        }

        return pending.Result;
    }

    internal XmlResolvedResource ResolveExternalXsltResourceCore(XsltStylesheetReference reference)
    {
        using (ResolverGuardScope scope = ResourceGuard.BeginXmlResource(reference.Href, reference.BaseUri))
        {
            XmlResolvedResource resource = Resolver.ResolveXmlResource(XmlResourceKind.Stylesheet, null, reference.Href, reference.BaseUri, Policy);
            ResourceGuard.ValidateXmlResolvedResource(scope, XmlResourceKind.Stylesheet, resource);
            return resource;
        }
    }

    internal XmlDocument ParseXsltDocument(XmlInput input, bool principal, XmlNode? referenceNode, string? location)
    {
        XmlParserOptions parserOptions = new XmlParserOptions();
        parserOptions.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        parserOptions.DtdProcessing = XmlDtdProcessing.Prohibit;
        parserOptions.PreserveComments = true;
        parserOptions.PreserveProcessingInstructions = true;
        parserOptions.PreservePrefixes = true;
        parserOptions.PreserveSourceLocations = true;
        parserOptions.RetainNamespaceDeclarations = true;
        parserOptions.SecurityLimits = Options.SecurityLimits;
        parserOptions.ResolverPolicy = Policy;
        Task<XmlDocument> pending = Task.Run(() => new XmlParser().ParseXmlDocument(input, parserOptions, Resolver, null, null));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion || pending.Result == null)
        {
            string code = principal ? XsltErrorCodes.XTSE0010 : XsltErrorCodes.XTSE0165;
            throw CreateXsltStaticError("The stylesheet module is not a well-formed XML document.", code, referenceNode, location);
        }

        return pending.Result;
    }

    internal XmlNode RequireXsltDocumentElement(XmlDocument document, XmlNode? referenceNode, string? location)
    {
        XmlNode? root = document.DocumentElement;
        if (root == null)
        {
            throw CreateXsltStaticError("A stylesheet module requires a document element.", XsltErrorCodes.XTSE0010, referenceNode, location);
        }

        return root;
    }

    internal XsltStylesheetModuleKind ClassifyXsltModuleRoot(XmlNode root, XsltStylesheetModuleRelation relation, string? location)
    {
        string namespaceUri = XsltNodeNamespace(root);
        string localName = XsltNodeLocalName(root);
        if (namespaceUri == XsltNamespaces.Xslt && localName == "stylesheet")
        {
            return XsltStylesheetModuleKind.Stylesheet;
        }

        if (namespaceUri == XsltNamespaces.Xslt && localName == "transform")
        {
            return XsltStylesheetModuleKind.Transform;
        }

        if (namespaceUri == XsltNamespaces.Xslt && localName == "package" && relation == XsltStylesheetModuleRelation.Principal)
        {
            return XsltStylesheetModuleKind.Package;
        }

        if (namespaceUri != XsltNamespaces.Xslt)
        {
            return XsltStylesheetModuleKind.Simplified;
        }

        string code = relation == XsltStylesheetModuleRelation.Principal ? XsltErrorCodes.XTSE0010 : XsltErrorCodes.XTSE0165;
        throw CreateXsltStaticError("The document element does not identify a stylesheet module.", code, root, location);
    }

    internal string ReadXsltModuleVersion(XmlNode root, XsltStylesheetModuleKind kind, string? location)
    {
        string attributeNamespace = kind == XsltStylesheetModuleKind.Simplified ? XsltNamespaces.Xslt : "";
        string? value = FindXsltAttribute(root, "version", attributeNamespace);
        string code = kind == XsltStylesheetModuleKind.Simplified ? XsltErrorCodes.XTSE0150 : XsltErrorCodes.XTSE0110;
        if (value == null || !IsValidXsltDecimal(value.Trim()))
        {
            throw CreateXsltStaticError("The stylesheet module requires a decimal version attribute.", code, root, location);
        }

        return value.Trim();
    }

    internal bool IsValidXsltDecimal(string value)
    {
        int index = 0;
        if (index < value.Length && (value[index] == '+' || value[index] == '-'))
        {
            index = index + 1;
        }

        bool digit = false;
        bool point = false;
        while (index < value.Length)
        {
            char character = value[index];
            if (character >= '0' && character <= '9')
            {
                digit = true;
            }
            else if (character == '.' && !point)
            {
                point = true;
            }
            else
            {
                return false;
            }

            index = index + 1;
        }

        return digit;
    }

    internal XsltStylesheetModuleRelation ClassifyXsltModuleReference(XmlNode node)
    {
        if (node.Kind != XmlNodeKind.Element || XsltNodeNamespace(node) != XsltNamespaces.Xslt)
        {
            return XsltStylesheetModuleRelation.Principal;
        }

        string localName = XsltNodeLocalName(node);
        if (localName == "include")
        {
            return XsltStylesheetModuleRelation.Include;
        }

        if (localName == "import")
        {
            return XsltStylesheetModuleRelation.Import;
        }

        return XsltStylesheetModuleRelation.Principal;
    }

    internal string? ReadXsltNodeBaseUri(XmlNode node, string? fallback)
    {
        Task<string?> pending = Task.Run(() => new TreeNavigation().GetXmlNodeBaseUriOperation(node));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltStaticError("The stylesheet module contains an invalid base URI.", XsltErrorCodes.XTSE0165, node, fallback);
        }

        return pending.Result == null ? fallback : pending.Result;
    }

    internal void EnterXsltExternalResource(XmlNode node)
    {
        int count = ExternalResourceCount + 1;
        if (count > MaximumExternalResources)
        {
            throw CreateXsltStaticError("The stylesheet module count exceeds the configured external resource limit.", XsltErrorCodes.XTSE0165, node, null);
        }

        int depth = ActiveLocations.Count;
        if (depth > MaximumResourceDepth)
        {
            throw CreateXsltStaticError("The stylesheet module nesting exceeds the configured resource depth.", XsltErrorCodes.XTSE0165, node, null);
        }

        ExternalResourceCount = count;
    }

    internal bool ContainsXsltLocation(List<string> locations, string value)
    {
        int index = 0;
        while (index < locations.Count)
        {
            if (string.Equals(locations[index], value, StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string? NormalizePrincipalXsltLocation(string? value)
    {
        if (value == null)
        {
            return null;
        }

        Task<string> pending = Task.Run(() => UriTools.CanonicalAbsoluteUri(UriTools.CreateAbsolute(value)));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltStaticError("The principal stylesheet base URI must be absolute.", XsltErrorCodes.XTSE0010, null, value);
        }

        return pending.Result;
    }

    internal void FreezeXsltDocuments()
    {
        TreeContentMutation mutation = new TreeContentMutation(new TreeMutation());
        int index = 0;
        while (index < Modules.Count)
        {
            XmlDocument document = Modules[index].Document;
            XmlNode? root = document.DocumentNode;
            if (root != null && !document.IsReadOnly)
            {
                mutation.FreezeXmlTreeOperation(root, true);
                document.IsReadOnly = true;
            }

            index = index + 1;
        }
    }

    internal string? FindXsltAttribute(XmlNode node, string localName, string namespaceUri)
    {
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            if (attribute.Kind == XmlNodeKind.Attribute && XsltNodeLocalName(attribute) == localName && XsltNodeNamespace(attribute) == namespaceUri)
            {
                return attribute.Value == null ? "" : attribute.Value;
            }

            index = index + 1;
        }

        return null;
    }

    internal string XsltNodeLocalName(XmlNode node)
    {
        XmlName? name = node.Name;
        return name == null || name.LocalName == null ? "" : name.LocalName;
    }

    internal string XsltNodeNamespace(XmlNode node)
    {
        XmlName? name = node.Name;
        return name == null || name.NamespaceUri == null ? "" : name.NamespaceUri;
    }

    internal XmlInput CopyXsltInput(XmlInput source, string? baseUri)
    {
        XmlInput result = new XmlInput();
        result.Kind = source.Kind;
        result.Text = source.Text;
        result.Characters = source.Characters;
        result.Bytes = source.Bytes;
        result.ByteStream = source.ByteStream;
        result.TextReader = source.TextReader;
        result.BaseUri = baseUri;
        result.MediaType = source.MediaType;
        result.EncodingName = source.EncodingName;
        result.Encoding = source.Encoding;
        result.OwnsResource = source.OwnsResource;
        return result;
    }

    internal XsltCompileOptions CopyXsltCompileOptions(XsltCompileOptions source)
    {
        XsltCompileOptions result = new XsltCompileOptions();
        result.ConformanceLevel = source.ConformanceLevel;
        result.RecoveryPolicy = source.RecoveryPolicy;
        result.BaseUri = source.BaseUri;
        result.PackageName = CopyXsltOptionName(source.PackageName);
        result.Packages = CopyXsltPackageReferences(source.Packages);
        result.StaticParameters = CopyXsltParameters(source.StaticParameters);
        result.ExtensionPolicy = new XmlExtensionPolicyEvaluator().CopyXmlExtensionPolicy(source.ExtensionPolicy);
        result.Extensions = CopyXsltExtensionBindings(source.Extensions);
        result.Collations = CopyXsltCollationBindings(source.Collations, result.ExtensionPolicy);
        result.Functions = CopyXsltFunctionBindings(source.Functions);
        result.SerializationMethods = CopyXsltSerializationMethodBindings(source.SerializationMethods);
        result.SchemaSet = CopyXsltSchemaSet(source.SchemaSet);
        result.ResolverPolicy = source.ResolverPolicy == null ? null : CopyXsltResolverPolicy(source.ResolverPolicy);
        result.SecurityLimits = source.SecurityLimits == null ? null : CopyXsltSecurityLimits(source.SecurityLimits);
        result.AnalyzeStreamability = source.AnalyzeStreamability;
        result.CompilePackagesSeparately = source.CompilePackagesSeparately;
        return result;
    }

    internal XmlResolverPolicy CopyXsltResolverPolicy(XmlResolverPolicy source)
    {
        XmlResolverPolicy result = new XmlResolverPolicy();
        result.Access = source.Access;
        result.AllowedSchemes = source.AllowedSchemes == null ? null : (string[])source.AllowedSchemes.Clone();
        result.AllowedRoots = source.AllowedRoots == null ? null : (string[])source.AllowedRoots.Clone();
        result.AllowRedirects = source.AllowRedirects;
        result.MaximumRedirects = source.MaximumRedirects;
        result.MaximumDepth = source.MaximumDepth;
        result.MaximumResourceBytes = source.MaximumResourceBytes;
        result.MaximumWorkUnits = source.MaximumWorkUnits;
        result.CacheResources = source.CacheResources;
        result.CatalogEntries = CopyXsltCatalogEntries(source.CatalogEntries);
        result.Accept = source.Accept;
        result.AcceptLanguage = source.AcceptLanguage;
        return result;
    }

    internal XmlName? CopyXsltOptionName(XmlName? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlName result = new XmlName();
        result.Prefix = source.Prefix;
        result.LocalName = source.LocalName;
        result.NamespaceUri = source.NamespaceUri;
        result.QualifiedName = source.QualifiedName;
        return result;
    }

    internal XsltPackageReference[]? CopyXsltPackageReferences(XsltPackageReference[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XsltPackageReference[] result = new XsltPackageReference[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XsltPackageReference item = source[index];
            XsltPackageReference copy = new XsltPackageReference();
            copy.Name = CopyXsltOptionName(item.Name);
            copy.Version = item.Version;
            copy.LocationUri = item.LocationUri;
            copy.Visibility = item.Visibility;
            copy.IsOptional = item.IsOptional;
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal XsltParameter[]? CopyXsltParameters(XsltParameter[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XsltParameter[] result = new XsltParameter[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XsltParameter item = source[index];
            XsltParameter copy = new XsltParameter();
            copy.Name = CopyXsltOptionName(item.Name);
            copy.Value = item.Value == null ? null : new XdmSequenceOperations().FreezeXdmSequenceStorage(item.Value);
            copy.DeclaredType = item.DeclaredType;
            copy.DeclaredOccurrence = item.DeclaredOccurrence;
            copy.IsTunnel = item.IsTunnel;
            copy.IsStatic = item.IsStatic;
            copy.IsRequired = item.IsRequired;
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal XsltExtensionBinding[]? CopyXsltExtensionBindings(XsltExtensionBinding[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XsltExtensionBinding[] result = new XsltExtensionBinding[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XsltExtensionBinding item = source[index];
            XsltExtensionBinding copy = new XsltExtensionBinding();
            copy.Name = CopyXsltOptionName(item.Name);
            copy.BindingKey = item.BindingKey;
            copy.MinimumVersion = item.MinimumVersion;
            copy.IsInstruction = item.IsInstruction;
            copy.IsFunction = item.IsFunction;
            copy.FunctionImplementation = item.FunctionImplementation;
            copy.InstructionImplementation = item.InstructionImplementation;
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal XPathCollationBinding[]? CopyXsltCollationBindings(XPathCollationBinding[]? source, XmlExtensionPolicy policy)
    {
        if (source == null)
        {
            return null;
        }

        XPathCollationBinding[] result = new XPathCollationBinding[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XPathCollationBinding item = source[index];
            XPathCollationBinding copy = new XPathCollationBinding();
            copy.Uri = item.Uri;
            if (item.Uri != null && !new XmlExtensionPolicyEvaluator().IsXmlExtensionAllowed(policy, item.Uri))
            {
                throw new ArgumentException("The XSLT collation URI is denied by the extension trust policy.", "options");
            }

            copy.Implementation = item.Implementation == null || item.Implementation.GetType().FullName == "xml.XPathExtensionCollation" ? item.Implementation : new XPathExtensionCollation(item.Implementation);
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal void ValidateXsltExtensionOptions(XsltCompileOptions options)
    {
        XmlExtensionPolicy policy = options.ExtensionPolicy == null ? new XmlExtensionPolicyEvaluator().CopyXmlExtensionPolicy(null) : options.ExtensionPolicy;
        ValidateXsltFunctionRegistrations(options.Functions, policy);
        ValidateXsltInstructionRegistrations(options.Extensions, policy);
        ValidateXsltCollationRegistrations(options.Collations, policy);
        ValidateXsltSerializationRegistrations(options.SerializationMethods, policy);
    }

    internal void ValidateXsltFunctionRegistrations(XPathFunctionBinding[]? supplied, XmlExtensionPolicy policy)
    {
        XPathFunctionBinding[] bindings = supplied == null ? [] : supplied;
        int index = 0;
        while (index < bindings.Length)
        {
            XPathFunctionBinding? binding = bindings[index];
            if (binding == null || binding.Implementation == null)
            {
                throw new ArgumentException("An XSLT XPath extension function requires an implementation.", "options");
            }

            new XdmFunctionOperations().ValidateXPathFunctionBinding(binding);
            new XmlExtensionPolicyEvaluator().RequireXmlExtensionName(policy, binding.Name, "options");
            if (binding.Name != null && new XmlExtensionPolicyEvaluator().IsXmlExtensionReservedNamespace(binding.Name.NamespaceUri == null ? "" : binding.Name.NamespaceUri))
            {
                throw new ArgumentException("An XSLT extension function cannot use a reserved namespace.", "options");
            }
            int earlier = 0;
            while (earlier < index)
            {
                XmlName? left = bindings[earlier].Name;
                XmlName? right = binding.Name;
                if (left != null && right != null && left.LocalName == right.LocalName && (left.NamespaceUri == null ? "" : left.NamespaceUri) == (right.NamespaceUri == null ? "" : right.NamespaceUri) && bindings[earlier].Arity == binding.Arity)
                {
                    throw new ArgumentException("XSLT XPath extension function name and arity pairs must be unique.", "options");
                }

                earlier = earlier + 1;
            }

            index = index + 1;
        }
    }

    internal void ValidateXsltInstructionRegistrations(XsltExtensionBinding[]? supplied, XmlExtensionPolicy policy)
    {
        XsltExtensionBinding[] bindings = supplied == null ? [] : supplied;
        int index = 0;
        while (index < bindings.Length)
        {
            XsltExtensionBinding? binding = bindings[index];
            if (binding == null || !binding.IsFunction && !binding.IsInstruction)
            {
                throw new ArgumentException("An XSLT extension binding requires a declared kind.", "options");
            }

            new XmlExtensionPolicyEvaluator().RequireXmlExtensionName(policy, binding.Name, "options");
            if (binding.Name != null && new XmlExtensionPolicyEvaluator().IsXmlExtensionReservedNamespace(binding.Name.NamespaceUri == null ? "" : binding.Name.NamespaceUri))
            {
                throw new ArgumentException("An XSLT extension binding cannot use a reserved namespace.", "options");
            }
            if (binding.BindingKey != null && binding.BindingKey.Length == 0)
            {
                throw new ArgumentException("An XSLT extension binding key cannot be empty.", "options");
            }

            int earlier = 0;
            while (earlier < index)
            {
                XmlName? left = bindings[earlier].Name;
                XmlName? right = binding.Name;
                bool sameName = left != null && right != null && left.LocalName == right.LocalName && (left.NamespaceUri == null ? "" : left.NamespaceUri) == (right.NamespaceUri == null ? "" : right.NamespaceUri);
                if (sameName && (bindings[earlier].IsFunction && binding.IsFunction || bindings[earlier].IsInstruction && binding.IsInstruction))
                {
                    throw new ArgumentException("XSLT extension names must be unique within each extension kind.", "options");
                }

                earlier = earlier + 1;
            }

            index = index + 1;
        }
    }

    internal void ValidateXsltCollationRegistrations(XPathCollationBinding[]? supplied, XmlExtensionPolicy policy)
    {
        XPathCollationBinding[] bindings = supplied == null ? [] : supplied;
        int index = 0;
        while (index < bindings.Length)
        {
            XPathCollationBinding? binding = bindings[index];
            string? uri = binding == null ? null : binding.Uri;
            if (binding == null || binding.Implementation == null || uri == null || !Uri.IsWellFormedUriString(uri, UriKind.Absolute) || !new XmlExtensionPolicyEvaluator().IsXmlExtensionAllowed(policy, uri))
            {
                throw new ArgumentException("An XSLT collation requires an allowed absolute URI and implementation.", "options");
            }

            int earlier = 0;
            while (earlier < index)
            {
                if (bindings[earlier].Uri == uri)
                {
                    throw new ArgumentException("XSLT collation URIs must be unique.", "options");
                }

                earlier = earlier + 1;
            }

            index = index + 1;
        }
    }

    internal void ValidateXsltSerializationRegistrations(XmlSerializationMethodBinding[]? supplied, XmlExtensionPolicy policy)
    {
        XmlSerializationMethodBinding[] bindings = supplied == null ? [] : supplied;
        int index = 0;
        while (index < bindings.Length)
        {
            XmlSerializationMethodBinding? binding = bindings[index];
            if (binding == null || binding.Implementation == null)
            {
                throw new ArgumentException("An extension serialization method requires an implementation.", "options");
            }

            new XmlExtensionPolicyEvaluator().RequireXmlExtensionName(policy, binding.Name, "options");
            if (binding.Name != null && new XmlExtensionPolicyEvaluator().IsXmlExtensionReservedNamespace(binding.Name.NamespaceUri == null ? "" : binding.Name.NamespaceUri))
            {
                throw new ArgumentException("An extension serialization method cannot use a reserved namespace.", "options");
            }
            int earlier = 0;
            while (earlier < index)
            {
                XmlName? left = bindings[earlier].Name;
                XmlName? right = binding.Name;
                if (left != null && right != null && left.LocalName == right.LocalName && (left.NamespaceUri == null ? "" : left.NamespaceUri) == (right.NamespaceUri == null ? "" : right.NamespaceUri))
                {
                    throw new ArgumentException("Extension serialization method names must be unique.", "options");
                }

                earlier = earlier + 1;
            }

            index = index + 1;
        }
    }

    internal XPathFunctionBinding[]? CopyXsltFunctionBindings(XPathFunctionBinding[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XPathFunctionBinding[] result = new XPathFunctionBinding[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XPathFunctionBinding item = source[index];
            XPathFunctionBinding copy = new XPathFunctionBinding();
            copy.Name = CopyXsltOptionName(item.Name);
            copy.Arity = item.Arity;
            copy.ParameterTypes = item.ParameterTypes == null ? null : (XdmAtomicType[])item.ParameterTypes.Clone();
            copy.ParameterOccurrences = item.ParameterOccurrences == null ? null : (XdmOccurrence[])item.ParameterOccurrences.Clone();
            copy.ResultType = item.ResultType;
            copy.ResultOccurrence = item.ResultOccurrence;
            copy.BindingKey = item.BindingKey;
            copy.Implementation = item.Implementation;
            copy.IsExtensionBinding = true;
            copy.IsDeterministic = item.IsDeterministic;
            copy.ParameterTypeSyntax = item.ParameterTypeSyntax;
            copy.ResultTypeSyntax = item.ResultTypeSyntax;
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal XmlSerializationMethodBinding[]? CopyXsltSerializationMethodBindings(XmlSerializationMethodBinding[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlSerializationMethodBinding[] result = new XmlSerializationMethodBinding[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XmlSerializationMethodBinding item = source[index];
            XmlSerializationMethodBinding copy = new XmlSerializationMethodBinding();
            copy.Name = CopyXsltOptionName(item.Name);
            copy.Implementation = item.Implementation;
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal XmlSchemaSet? CopyXsltSchemaSet(XmlSchemaSet? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlSchemaSet result = new XmlSchemaSet();
        result.SchemaDocuments = source.SchemaDocuments == null ? null : (XmlDocument[])source.SchemaDocuments.Clone();
        result.TargetNamespaces = source.TargetNamespaces == null ? null : (string[])source.TargetNamespaces.Clone();
        result.CompiledSet = source.CompiledSet;
        result.CompiledSchemaSet = source.CompiledSchemaSet;
        result.Diagnostics = CopyXsltOptionDiagnostics(source.Diagnostics);
        result.IsImmutable = source.IsImmutable;
        return result;
    }

    internal XmlDiagnostic[]? CopyXsltOptionDiagnostics(XmlDiagnostic[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlDiagnostic[] result = new XmlDiagnostic[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XmlDiagnostic item = source[index];
            XmlDiagnostic copy = new XmlDiagnostic();
            copy.Code = item.Code;
            copy.Category = item.Category;
            copy.Severity = item.Severity;
            copy.Message = item.Message;
            copy.Range = CopyXsltOptionRange(item.Range);
            copy.RelatedLocation = CopyXsltOptionLocation(item.RelatedLocation);
            copy.EntityStack = item.EntityStack == null ? null : (string[])item.EntityStack.Clone();
            copy.IncludeStack = item.IncludeStack == null ? null : (string[])item.IncludeStack.Clone();
            copy.TemplateStack = item.TemplateStack == null ? null : (string[])item.TemplateStack.Clone();
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal XmlCatalogEntry[]? CopyXsltCatalogEntries(XmlCatalogEntry[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlCatalogEntry[] result = new XmlCatalogEntry[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XmlCatalogEntry item = source[index];
            XmlCatalogEntry copy = new XmlCatalogEntry();
            copy.Kind = item.Kind;
            copy.PublicIdentifier = item.PublicIdentifier;
            copy.SystemIdentifier = item.SystemIdentifier;
            copy.Uri = item.Uri;
            copy.TargetUri = item.TargetUri;
            copy.RewritePrefix = item.RewritePrefix;
            copy.Suffix = item.Suffix;
            copy.CatalogUri = item.CatalogUri;
            copy.BaseUri = item.BaseUri;
            copy.PreferPublic = item.PreferPublic;
            copy.Entries = CopyXsltCatalogEntries(item.Entries);
            copy.Range = CopyXsltOptionRange(item.Range);
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal SourceRange? CopyXsltOptionRange(SourceRange? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceRange result = new SourceRange();
        result.Start = CopyXsltOptionLocation(source.Start);
        result.End = CopyXsltOptionLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXsltOptionLocation(SourceLocation? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = source.SourceUri;
        result.CharacterOffset = source.CharacterOffset;
        result.ByteOffset = source.ByteOffset;
        result.Line = source.Line;
        result.Column = source.Column;
        return result;
    }

    internal XmlSecurityLimits CopyXsltSecurityLimits(XmlSecurityLimits source)
    {
        XmlSecurityLimits result = new XmlSecurityLimits();
        result.MaximumDepth = source.MaximumDepth;
        result.MaximumNodeCount = source.MaximumNodeCount;
        result.MaximumAttributesPerElement = source.MaximumAttributesPerElement;
        result.MaximumNameLength = source.MaximumNameLength;
        result.MaximumTextLength = source.MaximumTextLength;
        result.MaximumTokenLength = source.MaximumTokenLength;
        result.MaximumInputCharacters = source.MaximumInputCharacters;
        result.MaximumInputBytes = source.MaximumInputBytes;
        result.MaximumDtdCharacters = source.MaximumDtdCharacters;
        result.MaximumEntityDepth = source.MaximumEntityDepth;
        result.MaximumEntityCount = source.MaximumEntityCount;
        result.MaximumEntityCharacters = source.MaximumEntityCharacters;
        result.MaximumEntityAmplification = source.MaximumEntityAmplification;
        result.MaximumExternalResources = source.MaximumExternalResources;
        result.MaximumResourceDepth = source.MaximumResourceDepth;
        result.MaximumResourceBytes = source.MaximumResourceBytes;
        result.CancellationPollingInterval = source.CancellationPollingInterval;
        return result;
    }

    internal XmlResolverPolicy CreateDefaultXsltResolverPolicy()
    {
        XmlResolverPolicy result = new XmlResolverPolicy();
        result.Access = XmlResourceAccess.Denied;
        result.AllowedSchemes = [];
        result.MaximumDepth = 16;
        result.MaximumResourceBytes = 67108864;
        result.MaximumWorkUnits = 67108864;
        return result;
    }

    internal XsltStaticException CreateXsltStaticError(string message, string code, XmlNode? node, string? location)
    {
        SourceRange? range = node == null ? null : node.Range;
        if (range == null && location != null)
        {
            SourceLocation source = new SourceLocation();
            source.SourceUri = location;
            source.ByteOffset = -1;
            source.Line = 1;
            source.Column = 1;
            range = new SourceRange();
            range.Start = source;
            range.End = source;
        }

        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = code.StartsWith("XTTE", System.StringComparison.Ordinal) ? XmlErrorCategory.XsltType : XmlErrorCategory.XsltStatic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = range;
        diagnostic.IncludeStack = ActiveLocations.ToArray();
        SourceLocation? primary = range == null ? null : range.Start;
        return new XsltStaticException(message, diagnostic, primary);
    }
}
