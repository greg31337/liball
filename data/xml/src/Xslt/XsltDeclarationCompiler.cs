using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace xml;

internal class XsltDeclarationCompiler
{
    internal XsltStylesheetLoader Loader;

    internal XsltDeclarationCompiler(XsltStylesheetLoader loader)
    {
        Loader = loader;
    }

    internal void CompileXsltDeclarations(XsltStylesheetPlan stylesheet)
    {
        stylesheet.Metadata = CompileXsltPackageMetadata(stylesheet);
        stylesheet.SchemaImports = CompileXsltSchemaImports(stylesheet);
        stylesheet.ImportedSchemaNamespaces = SelectXsltImportedSchemaNamespaces(stylesheet.SchemaImports);
        stylesheet.SchemaSet = Loader.Options.SchemaSet;
        AttachXsltImportedSchemaNamespaces(stylesheet);
        stylesheet.GlobalContextItem = CompileXsltGlobalContextItem(stylesheet);
    }

    internal XsltPackageMetadataPlan CompileXsltPackageMetadata(XsltStylesheetPlan stylesheet)
    {
        XsltStylesheetModule principal = stylesheet.PrincipalModule;
        XmlNode root = principal.Root;
        XsltPackageMetadataPlan result = new XsltPackageMetadataPlan(root, principal);
        result.IsExplicit = principal.Kind == XsltStylesheetModuleKind.Package;
        result.Id = NormalizeXsltOptionalMetadata(Loader.FindXsltAttribute(root, "id", ""));
        result.Name = NormalizeXsltOptionalMetadata(Loader.FindXsltAttribute(root, "name", ""));
        string? version = Loader.FindXsltAttribute(root, "package-version", "");
        result.PackageVersion = version == null ? "1" : version.Trim();
        result.DeclaredModes = ReadXsltMetadataBoolean(root, "declared-modes", result.IsExplicit, principal);
        XsltStaticContext? context = FindXsltDeclarationStaticContext(stylesheet, root);
        if (context != null)
        {
            result.DefaultMode = context.DefaultMode;
            result.DefaultValidation = context.Validation;
            result.DefaultCollation = context.DefaultCollation;
        }

        ReadXsltInputTypeAnnotations(stylesheet, result);
        return result;
    }

    internal string? NormalizeXsltOptionalMetadata(string? value)
    {
        if (value == null)
        {
            return null;
        }

        string normalized = value.Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    internal bool ReadXsltMetadataBoolean(XmlNode node, string name, bool defaultValue, XsltStylesheetModule module)
    {
        string? value = Loader.FindXsltAttribute(node, name, "");
        if (value == null)
        {
            return defaultValue;
        }

        string normalized = value.Trim();
        if (normalized == "yes" || normalized == "true" || normalized == "1")
        {
            return true;
        }

        if (normalized == "no" || normalized == "false" || normalized == "0")
        {
            return false;
        }

        throw CreateXsltDeclarationError("The " + name + " attribute must be a valid boolean value.", XsltErrorCodes.XTSE0020, node, module);
    }

    internal void ReadXsltInputTypeAnnotations(XsltStylesheetPlan stylesheet, XsltPackageMetadataPlan metadata)
    {
        bool preserve = false;
        bool strip = false;
        XsltInputTypeAnnotations principalValue = XsltInputTypeAnnotations.Unspecified;
        int index = 0;
        while (index < stylesheet.Modules.Length)
        {
            XsltStylesheetModule module = stylesheet.Modules[index];
            XsltInputTypeAnnotations value = ParseXsltInputTypeAnnotations(module.Root, module);
            if (object.ReferenceEquals(module, stylesheet.PrincipalModule))
            {
                principalValue = value;
            }

            if (value == XsltInputTypeAnnotations.Preserve)
            {
                preserve = true;
            }
            else if (value == XsltInputTypeAnnotations.Strip)
            {
                strip = true;
            }

            index = index + 1;
        }

        if (preserve && strip || metadata.IsExplicit && principalValue == XsltInputTypeAnnotations.Unspecified && (preserve || strip))
        {
            throw CreateXsltDeclarationError("The package contains inconsistent input-type-annotations declarations.", XsltErrorCodes.XTSE0265, metadata.Root, metadata.Module);
        }

        metadata.InputTypeAnnotations = principalValue;
        XsltInputTypeAnnotations requested = preserve ? XsltInputTypeAnnotations.Preserve : strip ? XsltInputTypeAnnotations.Strip : XsltInputTypeAnnotations.Unspecified;
        metadata.EffectiveInputTypeAnnotations = requested == XsltInputTypeAnnotations.Preserve && Loader.Options.ConformanceLevel != XsltConformanceLevel.SchemaAware ? XsltInputTypeAnnotations.Strip : requested;
    }

    internal XsltInputTypeAnnotations ParseXsltInputTypeAnnotations(XmlNode root, XsltStylesheetModule module)
    {
        string? source = Loader.FindXsltAttribute(root, "input-type-annotations", "");
        if (source == null)
        {
            return XsltInputTypeAnnotations.Unspecified;
        }

        string value = source.Trim();
        if (value == "preserve")
        {
            return XsltInputTypeAnnotations.Preserve;
        }

        if (value == "strip")
        {
            return XsltInputTypeAnnotations.Strip;
        }

        if (value == "unspecified")
        {
            return XsltInputTypeAnnotations.Unspecified;
        }

        throw CreateXsltDeclarationError("The input-type-annotations attribute must be preserve, strip, or unspecified.", XsltErrorCodes.XTSE0020, root, module);
    }

    internal XsltSchemaImportPlan[] CompileXsltSchemaImports(XsltStylesheetPlan stylesheet)
    {
        List<XsltSchemaImportPlan> imports = new List<XsltSchemaImportPlan>();
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            XsltStylesheetDeclaration declaration = stylesheet.Declarations[index];
            if (declaration.LocalName == "import-schema")
            {
                if (Loader.Options.ConformanceLevel != XsltConformanceLevel.SchemaAware)
                {
                    throw CreateXsltDeclarationError("xsl:import-schema requires a schema-aware XSLT processor.", XsltErrorCodes.XTSE1650, declaration.Node, declaration.Module);
                }

                imports.Add(CreateXsltSchemaImport(declaration));
            }

            index = index + 1;
        }

        SelectEffectiveXsltSchemaImports(imports);
        CompileEffectiveXsltSchemas(imports);
        return imports.ToArray();
    }

    internal XsltSchemaImportPlan CreateXsltSchemaImport(XsltStylesheetDeclaration declaration)
    {
        XmlNode node = declaration.Node;
        XsltSchemaImportPlan result = new XsltSchemaImportPlan(node, declaration.Module);
        result.ImportPrecedence = declaration.ImportPrecedence;
        result.DeclarationOrder = declaration.DeclarationOrder;
        string? namespaceValue = Loader.FindXsltAttribute(node, "namespace", "");
        if (namespaceValue != null)
        {
            result.HasNamespace = true;
            result.NamespaceUri = namespaceValue.Trim();
            if (result.NamespaceUri.Length == 0)
            {
                throw CreateXsltDeclarationError("An xsl:import-schema namespace cannot be an empty URI.", XsltErrorCodes.XTSE0215, node, declaration.Module);
            }
        }

        string? location = Loader.FindXsltAttribute(node, "schema-location", "");
        result.SchemaLocation = location == null ? null : location.Trim();
        if (result.SchemaLocation != null && result.SchemaLocation.Length == 0)
        {
            throw CreateXsltDeclarationError("The schema-location attribute cannot be empty.", XsltErrorCodes.XTSE0215, node, declaration.Module);
        }

        result.InlineSchema = FindXsltInlineSchema(node, declaration.Module);
        if (result.InlineSchema != null && result.SchemaLocation != null)
        {
            throw CreateXsltDeclarationError("An xsl:import-schema cannot have both an inline schema and schema-location.", XsltErrorCodes.XTSE0215, node, declaration.Module);
        }

        if (result.InlineSchema != null)
        {
            string? target = Loader.FindXsltAttribute(result.InlineSchema, "targetNamespace", "");
            string inlineNamespace = target == null ? "" : target.Trim();
            if (result.HasNamespace && result.NamespaceUri != inlineNamespace)
            {
                throw CreateXsltDeclarationError("The inline schema target namespace does not match the imported namespace.", XsltErrorCodes.XTSE0215, node, declaration.Module);
            }

            if (!result.HasNamespace)
            {
                result.NamespaceUri = inlineNamespace;
            }
        }

        return result;
    }

    internal XmlNode? FindXsltInlineSchema(XmlNode importNode, XsltStylesheetModule module)
    {
        XmlNode? inline = null;
        XmlNode[] children = importNode.Children == null ? [] : importNode.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            bool whitespace = (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value);
            if (child.Kind == XmlNodeKind.Comment || child.Kind == XmlNodeKind.ProcessingInstruction || whitespace)
            {
                index = index + 1;
                continue;
            }

            if (child.Kind != XmlNodeKind.Element || Loader.XsltNodeNamespace(child) != XPathNamespaces.XmlSchema || Loader.XsltNodeLocalName(child) != "schema" || inline != null)
            {
                throw CreateXsltDeclarationError("An xsl:import-schema may contain at most one xs:schema element.", XsltErrorCodes.XTSE0010, child, module);
            }

            inline = child;
            index = index + 1;
        }

        return inline;
    }

    internal void SelectEffectiveXsltSchemaImports(List<XsltSchemaImportPlan> imports)
    {
        int index = 0;
        while (index < imports.Count)
        {
            XsltSchemaImportPlan candidate = imports[index];
            int highest = candidate.ImportPrecedence;
            int comparison = 0;
            while (comparison < imports.Count)
            {
                XsltSchemaImportPlan other = imports[comparison];
                if (other.NamespaceUri == candidate.NamespaceUri && other.ImportPrecedence > highest)
                {
                    highest = other.ImportPrecedence;
                }

                comparison = comparison + 1;
            }

            candidate.IsEffective = candidate.ImportPrecedence == highest;
            index = index + 1;
        }
    }

    internal void CompileEffectiveXsltSchemas(List<XsltSchemaImportPlan> imports)
    {
        List<XmlInput> inputs = new List<XmlInput>();
        List<XsltSchemaImportPlan> compiledImports = new List<XsltSchemaImportPlan>();
        int index = 0;
        while (index < imports.Count)
        {
            XsltSchemaImportPlan import = imports[index];
            if (import.IsEffective && import.InlineSchema != null)
            {
                inputs.Add(CreateXsltInlineSchemaInput(import));
                compiledImports.Add(import);
            }
            else if (import.IsEffective && import.SchemaLocation != null)
            {
                inputs.Add(ResolveXsltSchemaInput(import));
                compiledImports.Add(import);
            }

            index = index + 1;
        }

        if (inputs.Count == 0)
        {
            return;
        }

        AddXsltSuppliedSchemaInputs(inputs);

        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions();
        options.ResolverPolicy = Loader.Policy;
        options.SecurityLimits = Loader.Options.SecurityLimits;
        Task<XmlSchemaSet> pending = Task.Run(() => new XmlSchemaCompiler().CompileXmlSchemas(inputs.ToArray(), options, Loader.Resolver));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion || pending.Result == null)
        {
            XsltSchemaImportPlan origin = compiledImports[0];
            throw CreateXsltDeclarationError("The imported schema components could not be constructed consistently.", XsltErrorCodes.XTSE0220, origin.Declaration, origin.Module);
        }

        XmlSchemaSet schemaSet = pending.Result;
        Loader.Options.SchemaSet = schemaSet;
        index = 0;
        while (index < compiledImports.Count)
        {
            XsltSchemaImportPlan import = compiledImports[index];
            if (!ContainsXsltSchemaNamespace(schemaSet.TargetNamespaces, import.NamespaceUri))
            {
                throw CreateXsltDeclarationError("The imported schema does not have the requested target namespace.", XsltErrorCodes.XTSE0220, import.Declaration, import.Module);
            }

            import.CompiledSchema = schemaSet;
            index = index + 1;
        }
    }

    internal void AddXsltSuppliedSchemaInputs(List<XmlInput> inputs)
    {
        XmlSchemaSet? supplied = Loader.Options.SchemaSet;
        XmlDocument[] documents = supplied == null || supplied.SchemaDocuments == null ? [] : supplied.SchemaDocuments;
        int index = documents.Length - 1;
        while (index >= 0)
        {
            XmlNode? root = documents[index].DocumentElement;
            if (root != null)
            {
                inputs.Insert(0, CreateXsltSchemaNodeInput(root, documents[index].BaseUri));
            }

            index = index - 1;
        }
    }

    internal XmlInput CreateXsltInlineSchemaInput(XsltSchemaImportPlan import)
    {
        return CreateXsltSchemaNodeInput(import.InlineSchema == null ? import.Declaration : import.InlineSchema, Loader.ReadXsltNodeBaseUri(import.Declaration, import.Module.BaseUri));
    }

    internal XmlInput CreateXsltSchemaNodeInput(XmlNode schema, string? baseUri)
    {
        StringWriter writer = new StringWriter();
        XmlOutput output = new XmlOutput();
        output.Kind = XmlOutputKind.TextWriter;
        output.TextWriter = writer;
        XmlSerializationOptions options = new XmlSerializationOptions();
        options.Method = XmlSerializationMethod.Xml;
        options.OmitXmlDeclaration = true;
        new XmlSerializer().SerializeXmlNode(schema, output, options);
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = writer.ToString();
        input.BaseUri = baseUri;
        return input;
    }

    internal XmlInput ResolveXsltSchemaInput(XsltSchemaImportPlan import)
    {
        string location = import.SchemaLocation == null ? "" : import.SchemaLocation;
        string? baseUri = Loader.ReadXsltNodeBaseUri(import.Declaration, import.Module.BaseUri);
        Loader.EnterXsltExternalResource(import.Declaration);
        Task<XmlResolvedResource> pending = Task.Run(() => ResolveXsltSchemaInputCore(location, baseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion || pending.Result == null || pending.Result.Input == null)
        {
            throw CreateXsltDeclarationError("The schema-location resource could not be retrieved.", XsltErrorCodes.XTSE0220, import.Declaration, import.Module);
        }

        XmlResolvedResource resource = pending.Result;
        string? absolute = resource.AbsoluteUri == null ? baseUri : resource.AbsoluteUri;
        XmlInput result = Loader.CopyXsltInput(resource.Input, absolute);
        if (resource.OwnsResource)
        {
            result.OwnsResource = true;
        }

        return result;
    }

    internal XmlResolvedResource ResolveXsltSchemaInputCore(string location, string? baseUri)
    {
        using (ResolverGuardScope scope = Loader.ResourceGuard.BeginXmlResource(location, baseUri))
        {
            XmlResolvedResource resource = Loader.Resolver.ResolveXmlResource(XmlResourceKind.Schema, null, location, baseUri, Loader.Policy);
            Loader.ResourceGuard.ValidateXmlResolvedResource(scope, XmlResourceKind.Schema, resource);
            return resource;
        }
    }

    internal bool ContainsXsltSchemaNamespace(string[]? namespaces, string value)
    {
        if (namespaces == null)
        {
            return false;
        }

        int index = 0;
        while (index < namespaces.Length)
        {
            if (namespaces[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string[] SelectXsltImportedSchemaNamespaces(XsltSchemaImportPlan[] imports)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < imports.Length)
        {
            string value = imports[index].NamespaceUri;
            if (imports[index].IsEffective && !result.Contains(value))
            {
                result.Add(value);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal void AttachXsltImportedSchemaNamespaces(XsltStylesheetPlan stylesheet)
    {
        int index = 0;
        while (index < stylesheet.StaticContexts.Length)
        {
            stylesheet.StaticContexts[index].ImportedSchemaNamespaces = (string[])stylesheet.ImportedSchemaNamespaces.Clone();
            index = index + 1;
        }
    }

    internal XsltGlobalContextItemPlan CompileXsltGlobalContextItem(XsltStylesheetPlan stylesheet)
    {
        List<XsltGlobalContextItemDeclarationPlan> declarations = new List<XsltGlobalContextItemDeclarationPlan>();
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            XsltStylesheetDeclaration declaration = stylesheet.Declarations[index];
            if (declaration.LocalName == "global-context-item")
            {
                EnsureSingleXsltGlobalContextItem(stylesheet.Declarations, declaration);
                declarations.Add(CreateXsltGlobalContextItemDeclaration(stylesheet, declaration));
            }

            index = index + 1;
        }

        XsltGlobalContextItemPlan result = new XsltGlobalContextItemPlan();
        result.Declarations = declarations.ToArray();
        if (declarations.Count == 0)
        {
            return result;
        }

        XsltGlobalContextItemDeclarationPlan selected = declarations[0];
        index = 1;
        while (index < declarations.Count)
        {
            XsltGlobalContextItemDeclarationPlan candidate = declarations[index];
            if (candidate.Use != selected.Use || candidate.TypeIdentity != selected.TypeIdentity)
            {
                throw CreateXsltDeclarationError("Global context item declarations in a package must be consistent.", XsltErrorCodes.XTSE3087, candidate.Declaration, candidate.Module);
            }

            index = index + 1;
        }

        if (Loader.IsLibraryPackage && selected.Use == XsltContextItemUse.Required)
        {
            throw CreateXsltDeclarationError("A library package cannot require a global context item.", XsltErrorCodes.XTTE0590, selected.Declaration, selected.Module);
        }

        if (Loader.IsLibraryPackage)
        {
            return result;
        }

        result.IsDeclared = true;
        result.Use = selected.Use;
        result.TypeSource = selected.TypeSource;
        result.TypeIdentity = selected.TypeIdentity;
        result.Type = selected.Type;
        return result;
    }

    internal void EnsureSingleXsltGlobalContextItem(XsltStylesheetDeclaration[] declarations, XsltStylesheetDeclaration current)
    {
        int count = 0;
        int index = 0;
        while (index < declarations.Length)
        {
            if (declarations[index].LocalName == "global-context-item" && object.ReferenceEquals(declarations[index].Module, current.Module))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        if (count > 1)
        {
            throw CreateXsltDeclarationError("A stylesheet module must not contain more than one xsl:global-context-item declaration.", XsltErrorCodes.XTSE3087, current.Node, current.Module);
        }
    }

    internal XsltGlobalContextItemDeclarationPlan CreateXsltGlobalContextItemDeclaration(XsltStylesheetPlan stylesheet, XsltStylesheetDeclaration declaration)
    {
        XmlNode node = declaration.Node;
        if (new XsltTemplateCompiler(Loader).HasXsltTemplateSignificantChildren(node))
        {
            throw CreateXsltDeclarationError("An xsl:global-context-item declaration must be empty.", XsltErrorCodes.XTSE0010, node, declaration.Module);
        }

        string? useSource = Loader.FindXsltAttribute(node, "use", "");
        string use = useSource == null ? "optional" : useSource.Trim();
        XsltContextItemUse parsedUse;
        if (use == "required")
        {
            parsedUse = XsltContextItemUse.Required;
        }
        else if (use == "optional")
        {
            parsedUse = XsltContextItemUse.Optional;
        }
        else if (use == "absent")
        {
            parsedUse = XsltContextItemUse.Absent;
        }
        else
        {
            throw CreateXsltDeclarationError("The use attribute of xsl:global-context-item must be required, optional, or absent.", XsltErrorCodes.XTSE0020, node, declaration.Module);
        }

        string? typeSource = Loader.FindXsltAttribute(node, "as", "");
        if (parsedUse == XsltContextItemUse.Absent && typeSource != null)
        {
            throw CreateXsltDeclarationError("A global context item declared absent must not specify a type.", XsltErrorCodes.XTSE3089, node, declaration.Module);
        }

        string source = typeSource == null ? "item()" : typeSource;
        XsltStaticContext? context = FindXsltDeclarationStaticContext(stylesheet, node);
        if (context == null)
        {
            throw CreateXsltDeclarationError("A global context item declaration has no static context.", XsltErrorCodes.XTSE0010, node, declaration.Module);
        }

        XPathStaticContext xpathContext = new XsltVariableCompiler(Loader).CreateXsltVariableXPathContext(context, []);
        if (Loader.Options.ConformanceLevel != XsltConformanceLevel.SchemaAware && IsXsltSchemaAwareTypeSource(source, xpathContext))
        {
            throw CreateXsltDeclarationError("The global context item type requires a schema-aware XSLT processor.", XsltErrorCodes.XTSE1660, node, declaration.Module);
        }

        Task<XPathTypeSyntax> pending = Task.Run(() => new XsltTemplateCompiler(Loader).ParseXsltContextItemType(source, xpathContext, declaration.Module.BaseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltDeclarationError("A global context item declaration has an invalid item type.", Loader.StaticProcessor.ReadXsltTaskErrorCode(pending.Exception), node, declaration.Module);
        }

        XPathTypeSyntax type = pending.Result;
        if (Loader.Options.ConformanceLevel != XsltConformanceLevel.SchemaAware && RequiresXsltSchemaAwareType(type))
        {
            throw CreateXsltDeclarationError("The global context item type requires a schema-aware XSLT processor.", XsltErrorCodes.XTSE1660, node, declaration.Module);
        }

        XsltGlobalContextItemDeclarationPlan result = new XsltGlobalContextItemDeclarationPlan(node, declaration.Module, type);
        result.Use = parsedUse;
        result.TypeSource = source;
        result.TypeIdentity = CreateXsltTypeIdentity(type);
        result.ImportPrecedence = declaration.ImportPrecedence;
        result.DeclarationOrder = declaration.DeclarationOrder;
        return result;
    }

    internal bool IsXsltSchemaAwareTypeSource(string source, XPathStaticContext context)
    {
        Task<XPathTypeSyntax> pending = Task.Run(() => new XPathParser(source, context.BaseUri).ParseXPathItemType());
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        return pending.Status == TaskStatus.RanToCompletion && IsXsltSchemaAwareRawType(pending.Result, context);
    }

    internal bool IsXsltSchemaAwareRawType(XPathTypeSyntax type, XPathStaticContext context)
    {
        XPathNodeTest? test = type.NodeTest;
        if (test != null && (test.Kind == XPathNodeTestKind.SchemaElement || test.Kind == XPathNodeTestKind.SchemaAttribute))
        {
            return true;
        }

        if (test != null && test.Type != null && IsXsltSchemaAwareRawType(test.Type, context))
        {
            return true;
        }

        if (type.NodeTest == null && type.InnerType == null && type.ParameterTypes.Length == 0 && type.ResultType == null && type.KeyType == null && type.ValueType == null && type.MemberType == null && !type.EmptySequence && type.Name != "item" && type.Name != "function" && type.Name != "map" && type.Name != "array")
        {
            string namespaceUri = ResolveXsltRawTypeNamespace(type, context);
            if (namespaceUri != XPathNamespaces.XmlSchema)
            {
                return true;
            }
        }

        int index = 0;
        while (index < type.ParameterTypes.Length)
        {
            if (IsXsltSchemaAwareRawType(type.ParameterTypes[index], context))
            {
                return true;
            }

            index = index + 1;
        }

        return type.ResultType != null && IsXsltSchemaAwareRawType(type.ResultType, context) || type.KeyType != null && IsXsltSchemaAwareRawType(type.KeyType, context) || type.ValueType != null && IsXsltSchemaAwareRawType(type.ValueType, context) || type.MemberType != null && IsXsltSchemaAwareRawType(type.MemberType, context) || type.InnerType != null && IsXsltSchemaAwareRawType(type.InnerType, context);
    }

    internal string ResolveXsltRawTypeNamespace(XPathTypeSyntax type, XPathStaticContext context)
    {
        if (type.NamespaceUri != null)
        {
            return type.NamespaceUri;
        }

        string prefix = type.Prefix == null ? "" : type.Prefix;
        if (prefix.Length == 0)
        {
            return context.DefaultElementNamespace == null ? "" : context.DefaultElementNamespace;
        }

        XmlNamespaceBinding[] namespaces = context.Namespaces == null ? [] : context.Namespaces;
        int index = 0;
        while (index < namespaces.Length)
        {
            XmlNamespaceBinding binding = namespaces[index];
            if ((binding.Prefix == null ? "" : binding.Prefix) == prefix)
            {
                return binding.NamespaceUri == null ? "" : binding.NamespaceUri;
            }

            index = index + 1;
        }

        return "";
    }

    internal bool RequiresXsltSchemaAwareType(XPathTypeSyntax type)
    {
        if (type.SchemaType)
        {
            return true;
        }

        XPathNodeTest? test = type.NodeTest;
        if (test != null && (test.Kind == XPathNodeTestKind.SchemaElement || test.Kind == XPathNodeTestKind.SchemaAttribute || test.ResolvedTypeName != null))
        {
            return true;
        }

        int index = 0;
        while (index < type.ParameterTypes.Length)
        {
            if (RequiresXsltSchemaAwareType(type.ParameterTypes[index]))
            {
                return true;
            }

            index = index + 1;
        }

        if (type.ResultType != null && RequiresXsltSchemaAwareType(type.ResultType) || type.KeyType != null && RequiresXsltSchemaAwareType(type.KeyType) || type.ValueType != null && RequiresXsltSchemaAwareType(type.ValueType) || type.MemberType != null && RequiresXsltSchemaAwareType(type.MemberType) || type.InnerType != null && RequiresXsltSchemaAwareType(type.InnerType))
        {
            return true;
        }

        return test != null && test.Type != null && RequiresXsltSchemaAwareType(test.Type);
    }

    internal string CreateXsltTypeIdentity(XPathTypeSyntax type)
    {
        StringBuilder result = new StringBuilder();
        AppendXsltTypeIdentity(result, type);
        return result.ToString();
    }

    internal void AppendXsltTypeIdentity(StringBuilder result, XPathTypeSyntax type)
    {
        result.Append(type.EmptySequence ? "empty" : type.ResolvedName == null ? type.Name : "name");
        AppendXsltResolvedNameIdentity(result, type.ResolvedName);
        result.Append('|');
        result.Append(type.Occurrence == null ? "" : type.Occurrence);
        if (type.NodeTest != null)
        {
            AppendXsltNodeTestIdentity(result, type.NodeTest);
        }

        int index = 0;
        while (index < type.ParameterTypes.Length)
        {
            AppendXsltTypeIdentity(result, type.ParameterTypes[index]);
            index = index + 1;
        }

        if (type.ResultType != null)
        {
            AppendXsltTypeIdentity(result, type.ResultType);
        }

        if (type.KeyType != null)
        {
            AppendXsltTypeIdentity(result, type.KeyType);
        }

        if (type.ValueType != null)
        {
            AppendXsltTypeIdentity(result, type.ValueType);
        }

        if (type.MemberType != null)
        {
            AppendXsltTypeIdentity(result, type.MemberType);
        }

        if (type.InnerType != null)
        {
            AppendXsltTypeIdentity(result, type.InnerType);
        }
    }

    internal void AppendXsltNodeTestIdentity(StringBuilder result, XPathNodeTest test)
    {
        result.Append('#');
        result.Append((int)test.Kind);
        AppendXsltResolvedNameIdentity(result, test.ResolvedName);
        AppendXsltResolvedNameIdentity(result, test.ResolvedTypeName);
        result.Append(test.Nillable ? "+" : "-");
        if (test.Type != null)
        {
            AppendXsltTypeIdentity(result, test.Type);
        }

        if (test.InnerTest != null)
        {
            AppendXsltNodeTestIdentity(result, test.InnerTest);
        }
    }

    internal void AppendXsltResolvedNameIdentity(StringBuilder result, XmlName? name)
    {
        if (name == null)
        {
            return;
        }

        result.Append("Q{");
        result.Append(name.NamespaceUri == null ? "" : name.NamespaceUri);
        result.Append('}');
        result.Append(name.LocalName == null ? "" : name.LocalName);
    }

    internal XsltStaticContext? FindXsltDeclarationStaticContext(XsltStylesheetPlan stylesheet, XmlNode node)
    {
        int index = 0;
        while (index < stylesheet.StaticContexts.Length)
        {
            if (object.ReferenceEquals(stylesheet.StaticContexts[index].Node, node))
            {
                return stylesheet.StaticContexts[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltStaticException CreateXsltDeclarationError(string message, string code, XmlNode node, XsltStylesheetModule module)
    {
        return new XsltStylesheetAssembler(Loader).CreateXsltAssemblyError(message, code, node, module);
    }
}
