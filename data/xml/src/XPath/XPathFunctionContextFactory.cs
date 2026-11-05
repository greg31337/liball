namespace xml;

internal class XPathFunctionContextFactory
{
    internal XPathStaticContext CreateXPathFunctionStaticContext(XPathStaticEnvironment source)
    {
        XPathStaticContext result = new XPathStaticContext();
        result.LanguageVersion = source.LanguageVersion;
        result.CompatibilityMode = source.CompatibilityMode;
        result.Namespaces = source.BuildXPathNamespaces(source.Namespaces);
        result.DefaultElementNamespace = source.DefaultElementNamespace;
        result.DefaultFunctionNamespace = source.DefaultFunctionNamespace;
        result.BaseUri = source.BaseUri;
        result.DefaultCollation = source.DefaultCollation;
        result.Collations = source.BuildXPathCollations(source.Collations);
        result.CollationBindings = CopyXPathFunctionCollations(source.CollationBindings);
        result.Variables = source.BuildXPathVariables(source.Variables);
        result.Functions = source.BuildXPathFunctions(source.Functions);
        result.FunctionResolver = source.FunctionResolver;
        result.ExtensionPolicy = new XmlExtensionPolicyEvaluator().CopyXmlExtensionPolicy(source.ExtensionPolicy);
        result.FunctionsArePrevalidated = true;
        result.SchemaSet = source.SchemaSet;
        result.DecimalFormats = source.BuildXPathDecimalFormats(source.DecimalFormats);
        result.DecimalFormatDeclarations = source.CopyXPathDecimalFormatDeclarations(source.DecimalFormatDeclarations);
        result.EnabledFeatures = source.BuildXPathFeatures(source.EnabledFeatures);
        result.ResolverPolicy = source.CopyXPathStaticResolverPolicy(source.ResolverPolicy);
        result.SchemaAware = source.SchemaAware;
        result.AllowHigherOrderFunctions = source.AllowHigherOrderFunctions;
        return result;
    }

    internal XPathCollationBinding[] CopyXPathFunctionCollations(XPathCollationBinding[] source)
    {
        XPathCollationBinding[] result = new XPathCollationBinding[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XPathCollationBinding copy = new XPathCollationBinding();
            copy.Uri = source[index].Uri;
            copy.Implementation = source[index].Implementation;
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal XPathDynamicContext CreateXPathFunctionDynamicContext(XPathRuntime runtime)
    {
        XPathDynamicContext source = runtime.Context.Snapshot;
        XPathFocusFrame focus = runtime.Focus.ReadXPathFocus();
        XPathDynamicContext result = CopyXPathFunctionDynamicComponents(source);
        result.ContextItem = focus.ContextItem;
        result.Position = focus.Position;
        result.Size = focus.Size;
        result.Variables = runtime.Variables.SnapshotXPathVariables();
        return result;
    }

    internal XPathDynamicContext CreateXPathInlineDynamicContext(XPathDynamicContext source, XPathPlanParameter[] parameters, XdmSequence[] arguments)
    {
        XPathDynamicContext result = CopyXPathFunctionDynamicComponents(source);
        result.ContextItem = null;
        result.Position = 0;
        result.Size = 0;
        XPathVariableBinding[] existing = source.Variables == null ? [] : source.Variables;
        XPathVariableBinding[] combined = new XPathVariableBinding[existing.Length + parameters.Length];
        int count = 0;
        int index = 0;
        while (index < existing.Length)
        {
            if (!ContainsXPathParameterName(parameters, existing[index].Name))
            {
                combined[count] = existing[index];
                count = count + 1;
            }

            index = index + 1;
        }

        index = 0;
        while (index < parameters.Length)
        {
            combined[count] = CreateXPathParameterVariable(parameters[index], arguments[index]);
            count = count + 1;
            index = index + 1;
        }

        XPathVariableBinding[] variables = new XPathVariableBinding[count];
        global::System.Array.Copy(combined, variables, count);
        result.Variables = variables;
        return result;
    }

    internal bool ContainsXPathParameterName(XPathPlanParameter[] parameters, XmlName? name)
    {
        if (name == null)
        {
            return false;
        }

        int index = 0;
        while (index < parameters.Length)
        {
            string parameterNamespace = parameters[index].NamespaceUri ?? string.Empty;
            string variableNamespace = name.NamespaceUri ?? string.Empty;
            if (parameters[index].LocalName == name.LocalName && parameterNamespace == variableNamespace)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XPathDynamicContext CopyXPathFunctionDynamicComponents(XPathDynamicContext source)
    {
        XPathDynamicContext result = new XPathDynamicContext();
        result.ContextItem = source.ContextItem;
        result.Position = source.Position;
        result.Size = source.Size;
        result.Variables = source.Variables;
        result.VariableResolver = source.VariableResolver;
        result.BaseUri = source.BaseUri;
        result.DefaultCollectionUri = source.DefaultCollectionUri;
        result.ImplicitTimezoneMinutes = source.ImplicitTimezoneMinutes;
        result.DefaultLanguage = source.DefaultLanguage;
        result.CurrentDateTime = source.CurrentDateTime;
        result.EnvironmentNames = source.EnvironmentNames;
        result.EnvironmentValues = source.EnvironmentValues;
        result.ResolverPolicy = source.ResolverPolicy;
        result.CollectionResolver = source.CollectionResolver;
        result.ResourceResolver = source.ResourceResolver;
        result.TraceReceiver = source.TraceReceiver;
        result.Cancellation = source.Cancellation;
        result.SecurityLimits = source.SecurityLimits;
        result.MaximumOperations = source.MaximumOperations;
        result.XsltCurrentGroupAvailable = source.XsltCurrentGroupAvailable;
        result.XsltCurrentGroup = source.XsltCurrentGroup;
        result.XsltCurrentGroupingKeyAvailable = source.XsltCurrentGroupingKeyAvailable;
        result.XsltCurrentGroupingKey = source.XsltCurrentGroupingKey;
        result.XsltCurrentMergeGroupAvailable = source.XsltCurrentMergeGroupAvailable;
        result.XsltCurrentMergeGroup = source.XsltCurrentMergeGroup;
        result.XsltCurrentMergeSourceNames = source.XsltCurrentMergeSourceNames == null ? null : (string[])source.XsltCurrentMergeSourceNames.Clone();
        result.XsltCurrentMergeSourceGroups = source.XsltCurrentMergeSourceGroups == null ? null : (XdmSequence[])source.XsltCurrentMergeSourceGroups.Clone();
        result.XsltCurrentMergeKeyAvailable = source.XsltCurrentMergeKeyAvailable;
        result.XsltCurrentMergeKey = source.XsltCurrentMergeKey;
        result.XsltKeys = source.XsltKeys;
        result.XsltAccumulators = source.XsltAccumulators;
        result.XsltRegexGroups = source.XsltRegexGroups == null ? null : (string?[])source.XsltRegexGroups.Clone();
        result.XsltFunctions = source.XsltFunctions;
        result.XsltCurrentComponent = source.XsltCurrentComponent;
        result.XsltCurrentItem = source.XsltCurrentItem;
        result.XsltCurrentOutputUri = source.XsltCurrentOutputUri;
        result.XsltDocuments = source.XsltDocuments;
        result.XsltStreamedNodes = source.XsltStreamedNodes;
        result.ExtensionState = source.ExtensionState;
        return result;
    }

    internal XPathVariableBinding CreateXPathParameterVariable(XPathPlanParameter parameter, XdmSequence value)
    {
        XmlName name = new XmlName();
        name.LocalName = parameter.LocalName;
        name.NamespaceUri = parameter.NamespaceUri;
        name.QualifiedName = parameter.Name;
        XPathVariableBinding result = new XPathVariableBinding();
        result.Name = name;
        result.Value = value;
        result.DeclaredType = XdmAtomicType.AnyAtomicType;
        result.DeclaredOccurrence = XdmOccurrence.ZeroOrMore;
        return result;
    }
}
