using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal enum XPathNameUse
{
    Variable = 0,
    Function = 1,
    Element = 2,
    Attribute = 3,
    Type = 4
}

internal class XPathStaticEnvironment
{
    internal XPathLanguageVersion LanguageVersion;
    internal XPathCompatibilityMode CompatibilityMode;
    internal XmlNamespaceBinding[] Namespaces;
    internal string DefaultElementNamespace;
    internal string DefaultFunctionNamespace;
    internal string? BaseUri;
    internal string DefaultCollation;
    internal string[] Collations;
    internal XPathCollationBinding[] CollationBindings;
    internal XPathVariableBinding[] Variables;
    internal XPathFunctionBinding[] Functions;
    internal IXPathFunction?[] FunctionImplementations;
    internal IXPathFunctionResolver? FunctionResolver;
    internal XmlSchemaSet? SchemaSet;
    internal XmlName[] DecimalFormats;
    internal XPathDecimalFormat[] DecimalFormatDeclarations;
    internal string[] EnabledFeatures;
    internal XmlResolverPolicy? ResolverPolicy;
    internal bool SchemaAware;
    internal bool AllowHigherOrderFunctions;
    internal bool FunctionsArePrevalidated;
    internal XmlExtensionPolicy ExtensionPolicy;
    internal XPathSchemaNodeTest? SchemaTests;
    internal XdmTypeSystem Types;

    internal XPathStaticEnvironment(XPathStaticContext context)
    {
        Guard.NotNull(context, "context");
        LanguageVersion = context.LanguageVersion;
        CompatibilityMode = context.CompatibilityMode;
        DefaultElementNamespace = context.DefaultElementNamespace == null ? "" : context.DefaultElementNamespace;
        DefaultFunctionNamespace = context.DefaultFunctionNamespace == null ? XPathNamespaces.Functions : context.DefaultFunctionNamespace;
        BaseUri = ValidateXPathBaseUri(context.BaseUri);
        DefaultCollation = context.DefaultCollation == null ? XPathNamespaces.CodepointCollation : context.DefaultCollation;
        ResolverPolicy = CopyXPathStaticResolverPolicy(context.ResolverPolicy);
        SchemaAware = context.SchemaAware;
        AllowHigherOrderFunctions = context.AllowHigherOrderFunctions;
        FunctionsArePrevalidated = context.FunctionsArePrevalidated;
        ExtensionPolicy = new XmlExtensionPolicyEvaluator().CopyXmlExtensionPolicy(context.ExtensionPolicy);
        Types = new XdmTypeSystem();
        Namespaces = BuildXPathNamespaces(context.Namespaces);
        CollationBindings = BuildXPathCollationBindings(context.CollationBindings);
        Collations = BuildXPathCollations(context.Collations);
        Variables = BuildXPathVariables(context.Variables);
        Functions = BuildXPathFunctions(context.Functions);
        FunctionResolver = context.FunctionResolver;
        FunctionImplementations = BuildXPathFunctionImplementations(Functions, FunctionResolver, context);
        DecimalFormats = BuildXPathDecimalFormats(context.DecimalFormats);
        DecimalFormatDeclarations = CopyXPathDecimalFormatDeclarations(context.DecimalFormatDeclarations);
        EnabledFeatures = BuildXPathFeatures(context.EnabledFeatures);
        SchemaSet = context.SchemaSet;
        SchemaTests = null;
        if (SchemaAware && SchemaSet != null)
        {
            SchemaTests = new XPathSchemaNodeTest(SchemaSet);
        }
    }

    internal XmlResolverPolicy? CopyXPathStaticResolverPolicy(XmlResolverPolicy? source)
    {
        if (source == null)
        {
            return null;
        }

        return new XPathRuntimeContext(new XPathDynamicContext()).CopyXPathResolverPolicy(source);
    }

    internal XmlNamespaceBinding[] BuildXPathNamespaces(XmlNamespaceBinding[]? supplied)
    {
        List<XmlNamespaceBinding> result = new List<XmlNamespaceBinding>();
        SetXPathNamespace(result, "xml", XmlNamespaces.Xml, false);
        SetXPathNamespace(result, "xs", XPathNamespaces.XmlSchema, false);
        SetXPathNamespace(result, "fn", XPathNamespaces.Functions, false);
        SetXPathNamespace(result, "math", XPathNamespaces.Math, false);
        SetXPathNamespace(result, "map", XPathNamespaces.Map, false);
        SetXPathNamespace(result, "array", XPathNamespaces.Array, false);
        SetXPathNamespace(result, "err", XPathNamespaces.Errors, false);
        XmlNamespaceBinding[] source = supplied == null ? [] : supplied;
        int index = 0;
        while (index < source.Length)
        {
            XmlNamespaceBinding? binding = source[index];
            if (binding == null || binding.Prefix == null || binding.NamespaceUri == null)
            {
                throw new ArgumentException("Static namespace bindings require non-null prefixes and namespace URIs.", "context");
            }

            string prefix = binding.Prefix;
            string namespaceUri = binding.NamespaceUri;
            bool undeclaration = binding.IsUndeclaration || namespaceUri.Length == 0;
            if (prefix == "xml" && (undeclaration || namespaceUri != XmlNamespaces.Xml))
            {
                throw new ArgumentException("The xml prefix must remain bound to the XML namespace.", "context");
            }

            if (prefix != "xml" && namespaceUri == XmlNamespaces.Xml)
            {
                throw new ArgumentException("Only the xml prefix may be bound to the XML namespace.", "context");
            }

            SetXPathNamespace(result, prefix, namespaceUri, undeclaration);
            index = index + 1;
        }

        return result.ToArray();
    }

    internal void SetXPathNamespace(List<XmlNamespaceBinding> bindings, string prefix, string namespaceUri, bool undeclaration)
    {
        int found = -1;
        int index = 0;
        while (index < bindings.Count)
        {
            if (bindings[index].Prefix == prefix)
            {
                found = index;
                break;
            }

            index = index + 1;
        }

        if (undeclaration)
        {
            if (found >= 0)
            {
                bindings.RemoveAt(found);
            }

            return;
        }

        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = prefix;
        binding.NamespaceUri = namespaceUri;
        binding.IsUndeclaration = false;
        binding.Range = null;
        if (found >= 0)
        {
            bindings[found] = binding;
        }
        else
        {
            bindings.Add(binding);
        }
    }

    internal string[] BuildXPathCollations(string[]? supplied)
    {
        List<string> result = new List<string>();
        result.Add(XPathNamespaces.CodepointCollation);
        result.Add(XPathAsciiCaseInsensitiveCollation.Uri);
        string[] source = supplied == null ? [] : supplied;
        int index = 0;
        while (index < source.Length)
        {
            string? collation = source[index];
            if (collation == null || !IsXPathAbsoluteUri(collation, false))
            {
                throw new ArgumentException("Static collation names must be absolute URI values.", "context");
            }

            if (!ContainsXPathString(result, collation))
            {
                result.Add(collation);
            }

            index = index + 1;
        }

        index = 0;
        while (index < CollationBindings.Length)
        {
            string? uri = CollationBindings[index].Uri;
            if (uri != null && !ContainsXPathString(result, uri))
            {
                result.Add(uri);
            }

            index = index + 1;
        }

        if (!IsXPathAbsoluteUri(DefaultCollation, false) || !ContainsXPathString(result, DefaultCollation))
        {
            throw new ArgumentException("The default collation must identify an available absolute collation URI.", "context");
        }

        return result.ToArray();
    }

    internal XPathCollationBinding[] BuildXPathCollationBindings(XPathCollationBinding[]? supplied)
    {
        XPathCollationBinding builtIn = new XPathCollationBinding();
        builtIn.Uri = XPathNamespaces.CodepointCollation;
        builtIn.Implementation = new XPathCodepointCollation();
        XPathCollationBinding html = new XPathCollationBinding();
        html.Uri = XPathAsciiCaseInsensitiveCollation.Uri;
        html.Implementation = new XPathAsciiCaseInsensitiveCollation();
        List<XPathCollationBinding> result = new List<XPathCollationBinding>();
        result.Add(builtIn);
        result.Add(html);
        XPathCollationBinding[] source = supplied == null ? [] : supplied;
        int index = 0;
        while (index < source.Length)
        {
            XPathCollationBinding? candidate = source[index];
            if (candidate == null || candidate.Uri == null || candidate.Implementation == null || !IsXPathAbsoluteUri(candidate.Uri, false))
            {
                throw new ArgumentException("XPath collation bindings require an absolute URI and an implementation.", "context");
            }

            int earlier = 0;
            while (earlier < result.Count)
            {
                if (result[earlier].Uri == candidate.Uri)
                {
                    throw new ArgumentException("XPath collation binding URIs must be unique.", "context");
                }

                earlier = earlier + 1;
            }

            XPathCollationBinding copy = new XPathCollationBinding();
            copy.Uri = candidate.Uri;
            if (!new XmlExtensionPolicyEvaluator().IsXmlExtensionAllowed(ExtensionPolicy, candidate.Uri))
            {
                throw new ArgumentException("The XPath collation URI is denied by the extension trust policy.", "context");
            }

            copy.Implementation = candidate.Implementation.GetType().FullName == "xml.XPathExtensionCollation" ? candidate.Implementation : new XPathExtensionCollation(candidate.Implementation);
            result.Add(copy);
            index = index + 1;
        }

        return result.ToArray();
    }

    internal XPathVariableBinding[] BuildXPathVariables(XPathVariableBinding[]? supplied)
    {
        XPathVariableBinding[] source = supplied == null ? [] : supplied;
        XPathVariableBinding[] result = new XPathVariableBinding[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XPathVariableBinding? binding = source[index];
            if (binding == null || binding.Name == null)
            {
                throw new ArgumentException("Static variable bindings require names.", "context");
            }

            ValidateXPathName(binding.Name, "Static variable bindings require complete expanded names.");
            if (FindXPathVariableIn(result, index, binding.Name) != null)
            {
                throw new ArgumentException("Static variable names must be unique.", "context");
            }

            XPathVariableBinding copy = new XPathVariableBinding();
            copy.Name = CopyXPathName(binding.Name);
            copy.Value = binding.Value == null ? null : new XdmSequenceOperations().FreezeXdmSequenceStorage(binding.Value);
            copy.DeclaredType = binding.DeclaredType;
            copy.DeclaredOccurrence = binding.DeclaredOccurrence;
            copy.IsExternal = binding.IsExternal;
            copy.IsRequired = binding.IsRequired;
            copy.StaticTypeSyntax = binding.StaticTypeSyntax;
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal XPathFunctionBinding[] BuildXPathFunctions(XPathFunctionBinding[]? supplied)
    {
        XPathFunctionBinding[] source = supplied == null ? [] : supplied;
        XPathFunctionBinding[] result = new XPathFunctionBinding[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XPathFunctionBinding? binding = source[index];
            if (binding == null || binding.Name == null || binding.Arity < 0)
            {
                throw new ArgumentException("Static function bindings require names and nonnegative arities.", "context");
            }

            ValidateXPathName(binding.Name, "Static function bindings require complete expanded names.");
            new XdmFunctionOperations().ValidateXPathFunctionBinding(binding);
            if (FindXPathFunctionIn(result, index, binding.Name, binding.Arity) != null)
            {
                throw new ArgumentException("Static function name and arity pairs must be unique.", "context");
            }

            if (binding.ParameterTypes != null && binding.ParameterTypes.Length != binding.Arity || binding.ParameterOccurrences != null && binding.ParameterOccurrences.Length != binding.Arity)
            {
                throw new ArgumentException("Static function parameter metadata must match the function arity.", "context");
            }

            XPathFunctionBinding copy = new XPathFunctionBinding();
            copy.Name = CopyXPathName(binding.Name);
            copy.Arity = binding.Arity;
            copy.ParameterTypes = binding.ParameterTypes == null ? null : CopyXPathAtomicTypes(binding.ParameterTypes);
            copy.ParameterOccurrences = binding.ParameterOccurrences == null ? null : CopyXPathOccurrences(binding.ParameterOccurrences);
            copy.ResultType = binding.ResultType;
            copy.ResultOccurrence = binding.ResultOccurrence;
            copy.BindingKey = binding.BindingKey;
            copy.Implementation = binding.Implementation;
            copy.IsExtensionBinding = binding.IsExtensionBinding || !FunctionsArePrevalidated;
            copy.ExtensionPolicy = binding.ExtensionPolicy == null ? ExtensionPolicy : new XmlExtensionPolicyEvaluator().CopyXmlExtensionPolicy(binding.ExtensionPolicy);
            if (copy.IsExtensionBinding)
            {
                new XmlExtensionPolicyEvaluator().RequireXmlExtensionName(copy.ExtensionPolicy, binding.Name, "context");
                if (copy.BindingKey != null && copy.BindingKey.Length == 0)
                {
                    throw new ArgumentException("An extension function binding key cannot be empty.", "context");
                }
            }
            copy.IsDeterministic = binding.IsDeterministic;
            copy.ParameterTypeSyntax = binding.ParameterTypeSyntax;
            copy.ResultTypeSyntax = binding.ResultTypeSyntax;
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal IXPathFunction?[] BuildXPathFunctionImplementations(XPathFunctionBinding[] bindings, IXPathFunctionResolver? resolver, XPathStaticContext context)
    {
        IXPathFunction?[] result = new IXPathFunction?[bindings.Length];
        int index = 0;
        while (index < bindings.Length)
        {
            XmlName? name = bindings[index].Name;
            if (name == null)
            {
                throw new InvalidOperationException("A validated XPath function binding has no name.");
            }

            IXPathFunction? implementation = bindings[index].Implementation;
            if (implementation == null && resolver != null)
            {
                Task<XPathExtensionFunctionResolution> pending = Task.Run(() => ResolveXPathFunctionImplementation(resolver, name, bindings[index].Arity, context));
                ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
                if (pending.Status != TaskStatus.RanToCompletion)
                {
                    string detail = new XmlExtensionPolicyEvaluator().ReadXmlExtensionFailureMessage(pending.Exception);
                    throw new ArgumentException("The XPath function resolver failed: " + detail, "context");
                }

                implementation = pending.Result.Implementation;
            }

            if (implementation != null && bindings[index].IsExtensionBinding)
            {
                XmlExtensionPolicy? optionalPolicy = bindings[index].ExtensionPolicy;
                XmlExtensionPolicy policy = optionalPolicy == null ? ExtensionPolicy : optionalPolicy;
                implementation = new XPathExtensionFunctionImplementation(implementation, policy);
            }

            result[index] = implementation;
            index = index + 1;
        }

        return result;
    }

    internal XPathExtensionFunctionResolution ResolveXPathFunctionImplementation(IXPathFunctionResolver resolver, XmlName name, int arity, XPathStaticContext context)
    {
        return new XPathExtensionFunctionResolution(resolver.ResolveXPathFunction(CopyXPathName(name), arity, context));
    }

    internal XmlName[] BuildXPathDecimalFormats(XmlName[]? supplied)
    {
        XmlName[] source = supplied == null ? [] : supplied;
        XmlName[] result = new XmlName[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XmlName? name = source[index];
            if (name == null)
            {
                throw new ArgumentException("Named decimal formats require expanded names.", "context");
            }

            ValidateXPathName(name, "Named decimal formats require complete expanded names.");
            if (FindXPathNameIn(result, index, name) != null)
            {
                throw new ArgumentException("Named decimal format names must be unique.", "context");
            }

            result[index] = CopyXPathName(name);
            index = index + 1;
        }

        return result;
    }

    internal XPathDecimalFormat[] CopyXPathDecimalFormatDeclarations(XPathDecimalFormat[]? supplied)
    {
        XPathDecimalFormat[] source = supplied == null ? [] : supplied;
        XPathDecimalFormat[] result = new XPathDecimalFormat[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XPathDecimalFormat declaration = source[index];
            XPathDecimalFormat copy = new XPathDecimalFormat();
            copy.Name = declaration.Name == null ? null : CopyXPathName(declaration.Name);
            copy.DecimalSeparator = declaration.DecimalSeparator;
            copy.GroupingSeparator = declaration.GroupingSeparator;
            copy.Infinity = declaration.Infinity;
            copy.NaN = declaration.NaN;
            copy.MinusSign = declaration.MinusSign;
            copy.Percent = declaration.Percent;
            copy.PerMille = declaration.PerMille;
            copy.ZeroDigit = declaration.ZeroDigit;
            copy.Digit = declaration.Digit;
            copy.PatternSeparator = declaration.PatternSeparator;
            copy.ExponentSeparator = declaration.ExponentSeparator;
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal string[] BuildXPathFeatures(string[]? supplied)
    {
        List<string> result = new List<string>();
        string[] source = supplied == null ? [] : supplied;
        int index = 0;
        while (index < source.Length)
        {
            string? feature = source[index];
            if (feature == null || feature.Length == 0)
            {
                throw new ArgumentException("Enabled feature names cannot be null or empty.", "context");
            }

            if (!ContainsXPathString(result, feature))
            {
                result.Add(feature);
            }

            index = index + 1;
        }

        if (SchemaAware && !ContainsXPathString(result, "schema-aware"))
        {
            result.Add("schema-aware");
        }

        if (AllowHigherOrderFunctions && !ContainsXPathString(result, "higher-order-functions"))
        {
            result.Add("higher-order-functions");
        }

        return result.ToArray();
    }

    internal XmlName ResolveXPathName(string? prefix, string? localName, string? namespaceUri, XPathNameUse use, SourceRange range)
    {
        if (localName == null || localName.Length == 0)
        {
            throw new ArgumentException("An XPath name requires a local name.", "localName");
        }

        string? resolvedNamespace = namespaceUri;
        if (resolvedNamespace == null && prefix != null)
        {
            resolvedNamespace = FindXPathNamespace(prefix);
            if (resolvedNamespace == null)
            {
                throw CreateXPathStaticEnvironmentError("The XPath prefix '" + prefix + "' is not present in the statically known namespaces.", XPathErrorCodes.XPST0081, range);
            }
        }
        else if (resolvedNamespace == null && use == XPathNameUse.Function)
        {
            resolvedNamespace = NormalizeXPathNamespace(DefaultFunctionNamespace);
        }
        else if (resolvedNamespace == null && (use == XPathNameUse.Element || use == XPathNameUse.Type))
        {
            resolvedNamespace = NormalizeXPathNamespace(DefaultElementNamespace);
        }

        if (resolvedNamespace != null && new XdmStringDatatype().CollapseXdmStringWhitespace(resolvedNamespace) == XmlNamespaces.Xmlns)
        {
            throw CreateXPathStaticEnvironmentError("The namespace declaration namespace cannot be used in an XPath expanded name.", XPathErrorCodes.XQST0070, range);
        }

        XmlName result = new XmlName();
        result.Prefix = prefix;
        result.LocalName = localName;
        result.NamespaceUri = NormalizeXPathNamespace(resolvedNamespace);
        result.QualifiedName = prefix == null ? localName : prefix + ":" + localName;
        return result;
    }

    internal void ResolveXPathType(XPathTypeSyntax type)
    {
        if (type.InnerType != null)
        {
            ResolveXPathType(type.InnerType);
            type.ResolvedName = type.InnerType.ResolvedName;
            type.AtomicType = type.InnerType.AtomicType;
            type.SchemaType = type.InnerType.SchemaType;
            type.ParenthesizedTest = type.InnerType.ParenthesizedTest;
            type.WildcardTest = type.InnerType.WildcardTest;
            type.NodeTest = type.InnerType.NodeTest;
            type.ParameterTypes = type.InnerType.ParameterTypes;
            type.ResultType = type.InnerType.ResultType;
            type.KeyType = type.InnerType.KeyType;
            type.ValueType = type.InnerType.ValueType;
            type.MemberType = type.InnerType.MemberType;
            return;
        }

        if (type.NodeTest != null)
        {
            ResolveXPathNodeTest(type.NodeTest, XPathAxis.Child);
        }

        ResolveXPathTypes(type.ParameterTypes);
        if (type.ResultType != null)
        {
            ResolveXPathType(type.ResultType);
        }

        if (type.KeyType != null)
        {
            ResolveXPathType(type.KeyType);
        }

        if (type.ValueType != null)
        {
            ResolveXPathType(type.ValueType);
        }

        if (type.MemberType != null)
        {
            ResolveXPathType(type.MemberType);
        }

        if (IsXPathStructuralType(type))
        {
            return;
        }

        XmlName name = ResolveXPathName(type.Prefix, type.LocalName, type.NamespaceUri, XPathNameUse.Type, type.Range);
        type.ResolvedName = name;
        type.AtomicType = Types.FindXdmAtomicTypeDefinition(name);
        type.SchemaType = false;
        if (type.AtomicType == null && SchemaAware && SchemaTests != null && SchemaTests.HasXPathSchemaTypeDefinition(name))
        {
            type.SchemaType = true;
        }
    }

    internal void ResolveXPathTypes(XPathTypeSyntax[] types)
    {
        int index = 0;
        while (index < types.Length)
        {
            ResolveXPathType(types[index]);
            index = index + 1;
        }
    }

    internal void ResolveXPathNodeTest(XPathNodeTest test, XPathAxis axis)
    {
        if (test.Kind == XPathNodeTestKind.Name)
        {
            XPathNameUse use = axis == XPathAxis.Attribute || axis == XPathAxis.Namespace ? XPathNameUse.Attribute : XPathNameUse.Element;
            test.ResolvedName = ResolveXPathName(test.Prefix, test.LocalName, test.NamespaceUri, use, test.Range);
            test.NamespaceUri = test.ResolvedName.NamespaceUri;
        }
        else if (test.Kind == XPathNodeTestKind.NamespaceWildcard)
        {
            XmlName wildcard = ResolveXPathName(test.Prefix, "wildcard", null, XPathNameUse.Attribute, test.Range);
            test.NamespaceUri = wildcard.NamespaceUri;
        }
        else if (test.Kind == XPathNodeTestKind.Element || test.Kind == XPathNodeTestKind.SchemaElement)
        {
            if (test.LocalName != null && test.LocalName != "*")
            {
                test.ResolvedName = ResolveXPathName(test.Prefix, test.LocalName, test.NamespaceUri, XPathNameUse.Element, test.Range);
                test.NamespaceUri = test.ResolvedName.NamespaceUri;
            }
        }
        else if (test.Kind == XPathNodeTestKind.Attribute || test.Kind == XPathNodeTestKind.SchemaAttribute)
        {
            if (test.LocalName != null && test.LocalName != "*")
            {
                test.ResolvedName = ResolveXPathName(test.Prefix, test.LocalName, test.NamespaceUri, XPathNameUse.Attribute, test.Range);
                test.NamespaceUri = test.ResolvedName.NamespaceUri;
            }
        }

        if (test.Type != null)
        {
            ResolveXPathType(test.Type);
            test.ResolvedTypeName = test.Type.ResolvedName;
        }

        if (test.InnerTest != null)
        {
            ResolveXPathNodeTest(test.InnerTest, XPathAxis.Child);
        }
    }

    internal XPathVariableBinding? FindXPathVariable(XmlName name)
    {
        return FindXPathVariableIn(Variables, Variables.Length, name);
    }

    internal XPathFunctionBinding? FindXPathFunction(XmlName name, int arity)
    {
        return FindXPathFunctionIn(Functions, Functions.Length, name, arity);
    }

    internal IXPathFunction? FindXPathFunctionImplementation(XmlName name, int arity)
    {
        int index = 0;
        while (index < Functions.Length)
        {
            XPathFunctionBinding binding = Functions[index];
            if (binding.Name != null && binding.Arity == arity && SameXPathName(binding.Name, name))
            {
                return FunctionImplementations[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal IXPathCollation? FindXPathCollationImplementation(string uri)
    {
        int index = 0;
        while (index < CollationBindings.Length)
        {
            if (CollationBindings[index].Uri == uri)
            {
                return CollationBindings[index].Implementation;
            }

            index = index + 1;
        }

        return null;
    }

    internal XmlName? FindXPathDecimalFormat(XmlName name)
    {
        return FindXPathNameIn(DecimalFormats, DecimalFormats.Length, name);
    }

    internal bool HasXPathCollation(string uri)
    {
        int index = 0;
        while (index < Collations.Length)
        {
            if (Collations[index] == uri)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasXPathFeature(string feature)
    {
        int index = 0;
        while (index < EnabledFeatures.Length)
        {
            if (EnabledFeatures[index] == feature)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string? FindXPathNamespace(string prefix)
    {
        int index = Namespaces.Length - 1;
        while (index >= 0)
        {
            XmlNamespaceBinding binding = Namespaces[index];
            if (binding.Prefix == prefix)
            {
                return binding.NamespaceUri;
            }

            index = index - 1;
        }

        return null;
    }

    internal XPathVariableBinding? FindXPathVariableIn(XPathVariableBinding[] bindings, int count, XmlName name)
    {
        int index = 0;
        while (index < count)
        {
            XPathVariableBinding? candidate = bindings[index];
            if (candidate != null && candidate.Name != null && SameXPathName(candidate.Name, name))
            {
                return candidate;
            }

            index = index + 1;
        }

        return null;
    }

    internal XPathFunctionBinding? FindXPathFunctionIn(XPathFunctionBinding[] bindings, int count, XmlName name, int arity)
    {
        int index = 0;
        while (index < count)
        {
            XPathFunctionBinding? candidate = bindings[index];
            if (candidate != null && candidate.Name != null && candidate.Arity == arity && SameXPathName(candidate.Name, name))
            {
                return candidate;
            }

            index = index + 1;
        }

        return null;
    }

    internal XmlName? FindXPathNameIn(XmlName[] names, int count, XmlName name)
    {
        int index = 0;
        while (index < count)
        {
            XmlName? candidate = names[index];
            if (candidate != null && SameXPathName(candidate, name))
            {
                return candidate;
            }

            index = index + 1;
        }

        return null;
    }

    internal bool SameXPathName(XmlName left, XmlName right)
    {
        return left.LocalName == right.LocalName && NormalizeXPathNamespace(left.NamespaceUri) == NormalizeXPathNamespace(right.NamespaceUri);
    }

    internal bool IsXPathStructuralType(XPathTypeSyntax type)
    {
        if (type.Prefix != null || type.NamespaceUri != null)
        {
            return false;
        }

        string name = type.Name;
        return type.EmptySequence || type.ParenthesizedItemType || type.ParenthesizedTest && (name == "item" || name == "node" || name == "text" || name == "comment" || name == "namespace-node" || name == "processing-instruction" || name == "document-node" || name == "element" || name == "attribute" || name == "schema-element" || name == "schema-attribute" || name == "function" || name == "map" || name == "array");
    }

    internal string? ValidateXPathBaseUri(string? value)
    {
        if (value == null)
        {
            return null;
        }

        if (!IsXPathAbsoluteUri(value, true))
        {
            throw new ArgumentException("The XPath static base URI must be absolute and must not contain a fragment.", "context");
        }

        return new Uri(value, UriKind.Absolute).AbsoluteUri;
    }

    internal bool IsXPathAbsoluteUri(string value, bool prohibitFragment)
    {
        if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
        {
            return false;
        }

        Uri uri = new Uri(value, UriKind.Absolute);
        return !prohibitFragment || uri.Fragment.Length == 0;
    }

    internal bool ContainsXPathString(List<string> values, string value)
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

    internal void ValidateXPathName(XmlName name, string message)
    {
        if (name.LocalName == null || name.LocalName.Length == 0)
        {
            throw new ArgumentException(message, "context");
        }
    }

    internal XmlName CopyXPathName(XmlName source)
    {
        XmlName result = new XmlName();
        result.Prefix = source.Prefix;
        result.LocalName = source.LocalName;
        result.NamespaceUri = NormalizeXPathNamespace(source.NamespaceUri);
        result.QualifiedName = source.QualifiedName;
        return result;
    }

    internal XdmAtomicType[] CopyXPathAtomicTypes(XdmAtomicType[] source)
    {
        XdmAtomicType[] result = new XdmAtomicType[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    internal XdmOccurrence[] CopyXPathOccurrences(XdmOccurrence[] source)
    {
        XdmOccurrence[] result = new XdmOccurrence[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    internal string? NormalizeXPathNamespace(string? namespaceUri)
    {
        return namespaceUri == null || namespaceUri.Length == 0 ? null : namespaceUri;
    }

    internal XPathStaticException CreateXPathStaticEnvironmentError(string message, string code, SourceRange range)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathStatic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = range;
        return new XPathStaticException(message, diagnostic, range.Start);
    }
}
