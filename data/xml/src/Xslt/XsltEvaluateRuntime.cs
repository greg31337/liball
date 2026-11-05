using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace xml;

internal class XsltEvaluateOptions
{
    internal bool Cache;
    internal bool AllowExternalResources;
    internal long MaximumOperations;
    internal int MaximumExpressionLength;

    internal XsltEvaluateOptions()
    {
        Cache = true;
        AllowExternalResources = true;
        MaximumOperations = 0;
        MaximumExpressionLength = 0;
    }
}

internal class XsltEvaluateCacheEntry
{
    internal XmlNode Instruction;
    internal string Source;
    internal string BaseUri;
    internal string NamespaceKey;
    internal string VariableKey;
    internal bool SchemaAware;
    internal XPathPlan Plan;

    internal XsltEvaluateCacheEntry(XmlNode instruction, string source, string baseUri, string namespaceKey, string variableKey, bool schemaAware, XPathPlan plan)
    {
        Instruction = instruction;
        Source = source;
        BaseUri = baseUri;
        NamespaceKey = namespaceKey;
        VariableKey = variableKey;
        SchemaAware = schemaAware;
        Plan = plan;
    }
}

internal class XsltEvaluateRuntime
{
    internal XsltTransformationState State;
    internal XsltTemplateRuntime Templates;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmMapOperations Maps;
    internal List<XsltEvaluateCacheEntry> Cache;

    internal XsltEvaluateRuntime(XsltTransformationState state)
    {
        State = state;
        Templates = state.TemplateRuntime == null ? throw new InvalidOperationException("The template runtime must be initialized before the dynamic XPath runtime.") : state.TemplateRuntime;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Maps = new XdmMapOperations();
        Cache = new List<XsltEvaluateCacheEntry>();
    }

    internal XdmSequence EvaluateXsltDynamicExpression(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionPlan instruction = item.Instruction == null ? throw new InvalidOperationException("An xsl:evaluate instruction has no plan.") : item.Instruction;
        XsltVariableFrame frame = invocation.Frame == null ? throw new InvalidOperationException("An xsl:evaluate instruction has no variable frame.") : invocation.Frame;
        XsltInstructionAttributePlan xpath = FindXsltEvaluateAttribute(instruction, "xpath") ?? throw new InvalidOperationException("An xsl:evaluate instruction has no xpath attribute.");
        XdmSequence xpathValue = EvaluateXsltEvaluateAttribute(xpath, frame);
        string source = ReadXsltEvaluateString(xpathValue, instruction);
        XsltEvaluateOptions options = ReadXsltEvaluateOptions(instruction, frame);
        EnforceXsltEvaluateSourceLimit(source, options, instruction);
        XPathVariableBinding[] variables = CreateXsltEvaluateVariables(item, frame, instruction);
        XmlNamespaceBinding[] namespaces = CreateXsltEvaluateNamespaces(instruction, frame);
        string defaultNamespace = ReadXsltEvaluateDefaultNamespace(namespaces, instruction);
        string baseUri = ReadXsltEvaluateBaseUri(instruction, invocation);
        bool schemaAware = ReadXsltEvaluateSchemaAwareness(instruction, invocation);
        XPathStaticContext staticContext = CreateXsltEvaluateStaticContext(xpath, variables, namespaces, defaultNamespace, baseUri, schemaAware);
        XPathPlan plan = FindOrCompileXsltEvaluatePlan(instruction, source, staticContext, variables, namespaces, options);
        XdmSequence contextItem = CreateXsltEvaluateContextItem(instruction, frame);
        XPathDynamicContext dynamicContext = CreateXsltEvaluateDynamicContext(frame, contextItem, variables, options);
        XPathRuntime runtime = new XPathRuntime(plan, dynamicContext);
        if (options.AllowExternalResources)
        {
            runtime.Resources = State.DocumentPool;
        }

        XdmSequence result = runtime.EvaluateXPathPlan();
        XsltInstructionAttributePlan? required = FindXsltEvaluateAttribute(instruction, "as");
        return required == null || required.SequenceType == null ? result : new XPathFunctionTypeConverter().ConvertXPathFunctionTypedValue(result, required.SequenceType, staticContext);
    }

    internal XdmSequence EvaluateXsltEvaluateAttribute(XsltInstructionAttributePlan attribute, XsltVariableFrame frame)
    {
        XPathPlan plan = attribute.Expression == null ? throw new InvalidOperationException("An xsl:evaluate expression attribute has no compiled plan.") : attribute.Expression;
        return Templates.EvaluateXsltInstructionExpression(plan, frame);
    }

    internal string ReadXsltEvaluateString(XdmSequence supplied, XsltInstructionPlan instruction)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count != 1)
        {
            throw CreateXsltEvaluateTypeError("The xpath attribute of xsl:evaluate must return one string.", XPathErrorCodes.XPTY0004, instruction.Instruction);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(atomized, 0);
        XdmAtomicValue? value = item.AtomicValue;
        if (value == null)
        {
            throw CreateXsltEvaluateTypeError("The xpath attribute of xsl:evaluate cannot be converted to a string.", XPathErrorCodes.XPTY0004, instruction.Instruction);
        }

        XdmSequenceType required = new XdmSequenceType();
        required.ItemKind = XdmItemKind.Atomic;
        required.AtomicType = XdmAtomicType.String;
        required.Occurrence = XdmOccurrence.ExactlyOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(atomized, required, CreateXsltEvaluateConversionContext(instruction));
        XdmSequenceItem convertedItem = Sequences.ReadXdmSequenceItem(converted, 0);
        return convertedItem.AtomicValue == null ? throw CreateXsltEvaluateTypeError("The xpath attribute of xsl:evaluate cannot be converted to a string.", XPathErrorCodes.XPTY0004, instruction.Instruction) : Converter.ReadXdmAtomicStringValue(convertedItem.AtomicValue);
    }

    internal XsltEvaluateOptions ReadXsltEvaluateOptions(XsltInstructionPlan instruction, XsltVariableFrame frame)
    {
        XsltEvaluateOptions result = new XsltEvaluateOptions();
        XsltInstructionAttributePlan? attribute = FindXsltEvaluateAttribute(instruction, "options");
        if (attribute == null)
        {
            return result;
        }

        XdmSequence supplied = EvaluateXsltEvaluateAttribute(attribute, frame);
        if (supplied.Count != 1)
        {
            throw CreateXsltEvaluateTypeError("The options attribute of xsl:evaluate must return one map.", XPathErrorCodes.XPTY0004, instruction.Instruction);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Kind != XdmItemKind.Map || item.Map == null)
        {
            throw CreateXsltEvaluateTypeError("The options attribute of xsl:evaluate must return a map.", XPathErrorCodes.XPTY0004, instruction.Instruction);
        }

        Maps.ValidateXdmMapStorage(item.Map);
        XdmMapEntry[] entries = item.Map.Entries == null ? [] : item.Map.Entries;
        int index = 0;
        while (index < entries.Length)
        {
            ApplyXsltEvaluateOption(result, entries[index], instruction);
            index = index + 1;
        }

        return result;
    }

    internal void ApplyXsltEvaluateOption(XsltEvaluateOptions options, XdmMapEntry entry, XsltInstructionPlan instruction)
    {
        XdmAtomicValue key = entry.Key == null ? throw CreateXsltEvaluateTypeError("An xsl:evaluate option has no key.", XPathErrorCodes.XPTY0004, instruction.Instruction) : entry.Key;
        string name;
        if (key.Type == XdmAtomicType.QName && key.NameValue != null)
        {
            name = key.NameValue.LocalName == null ? "" : key.NameValue.LocalName;
        }
        else if (Maps.IsXdmStringMapKey(key.Type))
        {
            name = Converter.ReadXdmAtomicStringValue(key);
        }
        else
        {
            throw CreateXsltEvaluateTypeError("An xsl:evaluate option key must be a string or QName.", XPathErrorCodes.XPTY0004, instruction.Instruction);
        }

        XdmSequence value = entry.Value == null ? throw CreateXsltEvaluateTypeError("An xsl:evaluate option has no value.", XPathErrorCodes.XPTY0004, instruction.Instruction) : entry.Value;
        if (name == "cache")
        {
            options.Cache = ReadXsltEvaluateBooleanOption(value, name, instruction);
        }
        else if (name == "allow-external-resources")
        {
            options.AllowExternalResources = ReadXsltEvaluateBooleanOption(value, name, instruction);
        }
        else if (name == "maximum-operations")
        {
            options.MaximumOperations = ReadXsltEvaluateLongOption(value, name, instruction);
        }
        else if (name == "maximum-expression-length")
        {
            long length = ReadXsltEvaluateLongOption(value, name, instruction);
            if (length > int.MaxValue)
            {
                throw CreateXsltEvaluateTypeError("The maximum-expression-length option is too large.", XPathErrorCodes.XPTY0004, instruction.Instruction);
            }

            options.MaximumExpressionLength = (int)length;
        }
        else
        {
            throw CreateXsltEvaluateTypeError("The xsl:evaluate option is not recognized.", XPathErrorCodes.XPTY0004, instruction.Instruction);
        }
    }

    internal bool ReadXsltEvaluateBooleanOption(XdmSequence supplied, string name, XsltInstructionPlan instruction)
    {
        if (supplied.Count != 1)
        {
            throw CreateXsltEvaluateTypeError("The " + name + " option must contain one boolean.", XPathErrorCodes.XPTY0004, instruction.Instruction);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.AtomicValue == null || item.AtomicValue.Type != XdmAtomicType.Boolean)
        {
            throw CreateXsltEvaluateTypeError("The " + name + " option must contain one boolean.", XPathErrorCodes.XPTY0004, instruction.Instruction);
        }

        return item.AtomicValue.BooleanValue;
    }

    internal long ReadXsltEvaluateLongOption(XdmSequence supplied, string name, XsltInstructionPlan instruction)
    {
        if (supplied.Count != 1)
        {
            throw CreateXsltEvaluateTypeError("The " + name + " option must contain one positive integer.", XPathErrorCodes.XPTY0004, instruction.Instruction);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        XdmAtomicValue? value = item.AtomicValue;
        XdmAtomicValue requiredValue = value == null ? throw CreateXsltEvaluateTypeError("The " + name + " option must contain one positive integer.", XPathErrorCodes.XPTY0004, instruction.Instruction) : value;
        if (!new XdmTypeSystem().IsXdmAtomicSubtypeOf(requiredValue.Type, XdmAtomicType.Integer))
        {
            throw CreateXsltEvaluateTypeError("The " + name + " option must contain one positive integer.", XPathErrorCodes.XPTY0004, instruction.Instruction);
        }

        BigInteger integer = new XdmNumericDatatype().ReadXdmIntegerValue(requiredValue);
        if (integer <= BigInteger.Zero || integer > new BigInteger(long.MaxValue))
        {
            throw CreateXsltEvaluateTypeError("The " + name + " option must contain one positive integer.", XPathErrorCodes.XPTY0004, instruction.Instruction);
        }

        return (long)integer;
    }

    internal void EnforceXsltEvaluateSourceLimit(string source, XsltEvaluateOptions options, XsltInstructionPlan instruction)
    {
        XmlSecurityLimits? limits = State.DynamicContext.SecurityLimits;
        int configured = limits == null ? 0 : limits.MaximumTokenLength;
        int maximum = configured > 0 ? configured : int.MaxValue;
        if (options.MaximumExpressionLength > 0 && options.MaximumExpressionLength < maximum)
        {
            maximum = options.MaximumExpressionLength;
        }

        if (source.Length > maximum)
        {
            throw CreateXsltEvaluateDynamicError("The dynamically supplied XPath expression exceeds the configured length limit.", XmlErrorCodes.LimitExceeded, instruction.Instruction);
        }
    }

    internal XPathVariableBinding[] CreateXsltEvaluateVariables(XsltSequenceItemPlan item, XsltVariableFrame frame, XsltInstructionPlan instruction)
    {
        List<XPathVariableBinding> result = new List<XPathVariableBinding>();
        XsltParameter[] parameters = Templates.EvaluateXsltWithParameters(item, frame);
        int index = 0;
        while (index < parameters.Length)
        {
            XsltParameter parameter = parameters[index];
            XPathVariableBinding binding = CreateXsltEvaluateVariable(parameter.Name, parameter.Value, FindXsltEvaluateParameterType(item, parameter.Name));
            SetXsltEvaluateVariable(result, binding);
            index = index + 1;
        }

        XsltInstructionAttributePlan? attribute = FindXsltEvaluateAttribute(instruction, "with-params");
        if (attribute != null)
        {
            AddXsltEvaluateMapVariables(result, EvaluateXsltEvaluateAttribute(attribute, frame), instruction);
        }

        return result.ToArray();
    }

    internal XPathTypeSyntax? FindXsltEvaluateParameterType(XsltSequenceItemPlan owner, XmlName? name)
    {
        XsltSequenceItemPlan[] items = owner.Content == null ? [] : owner.Content.Items;
        int index = 0;
        while (index < items.Length)
        {
            XsltInstructionPlan? instruction = items[index].Instruction;
            if (instruction != null && instruction.Kind == XsltInstructionKind.WithParam)
            {
                XsltInstructionAttributePlan? declaredName = FindXsltEvaluateAttribute(instruction, "name");
                if (declaredName != null && SameXsltEvaluateName(declaredName.ResolvedName, name))
                {
                    XsltInstructionAttributePlan? type = FindXsltEvaluateAttribute(instruction, "as");
                    return type == null ? null : type.SequenceType;
                }
            }

            index = index + 1;
        }

        return null;
    }

    internal void AddXsltEvaluateMapVariables(List<XPathVariableBinding> variables, XdmSequence supplied, XsltInstructionPlan instruction)
    {
        if (supplied.Count != 1)
        {
            throw CreateXsltEvaluateTypeError("The with-params attribute of xsl:evaluate must return one map.", XsltErrorCodes.XTTE3165, instruction.Instruction);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Kind != XdmItemKind.Map || item.Map == null)
        {
            throw CreateXsltEvaluateTypeError("The with-params attribute of xsl:evaluate must return a map.", XsltErrorCodes.XTTE3165, instruction.Instruction);
        }

        Maps.ValidateXdmMapStorage(item.Map);
        XdmMapEntry[] entries = item.Map.Entries == null ? [] : item.Map.Entries;
        int index = 0;
        while (index < entries.Length)
        {
            XdmAtomicValue? key = entries[index].Key;
            if (key == null || key.Type != XdmAtomicType.QName || key.NameValue == null || key.NameValue.LocalName == null || key.NameValue.LocalName.Length == 0)
            {
                throw CreateXsltEvaluateTypeError("Every with-params map key must be an xs:QName.", XsltErrorCodes.XTTE3165, instruction.Instruction);
            }

            XdmSequence? optionalValue = entries[index].Value;
            XdmSequence value = optionalValue == null ? Sequences.CreateEmptyXdmSequence() : optionalValue;
            SetXsltEvaluateVariable(variables, CreateXsltEvaluateVariable(key.NameValue, value, null));
            index = index + 1;
        }
    }

    internal XPathVariableBinding CreateXsltEvaluateVariable(XmlName? name, XdmSequence? value, XPathTypeSyntax? type)
    {
        XPathVariableBinding result = new XPathVariableBinding();
        result.Name = CopyXsltEvaluateName(name == null ? throw new InvalidOperationException("An xsl:evaluate parameter has no name.") : name);
        result.Value = value == null ? Sequences.CreateEmptyXdmSequence() : value;
        result.DeclaredType = XdmAtomicType.AnyAtomicType;
        result.DeclaredOccurrence = XdmOccurrence.ZeroOrMore;
        result.IsExternal = true;
        result.IsRequired = true;
        result.StaticTypeSyntax = type == null ? CreateXsltEvaluateItemType() : type;
        return result;
    }

    internal void SetXsltEvaluateVariable(List<XPathVariableBinding> variables, XPathVariableBinding binding)
    {
        int index = 0;
        while (index < variables.Count)
        {
            if (SameXsltEvaluateName(variables[index].Name, binding.Name))
            {
                variables[index] = binding;
                return;
            }

            index = index + 1;
        }

        variables.Add(binding);
    }

    internal XPathTypeSyntax CreateXsltEvaluateItemType()
    {
        return new XPathTypeSyntax("item", null, null, null, "*", false, new SourceRange());
    }

    internal XmlNamespaceBinding[] CreateXsltEvaluateNamespaces(XsltInstructionPlan instruction, XsltVariableFrame frame)
    {
        XsltInstructionAttributePlan? attribute = FindXsltEvaluateAttribute(instruction, "namespace-context");
        if (attribute == null)
        {
            return RemoveXsltEvaluateDefaultNamespace(instruction.StaticContext.Namespaces);
        }

        XdmSequence supplied = EvaluateXsltEvaluateAttribute(attribute, frame);
        if (supplied.Count != 1)
        {
            throw CreateXsltEvaluateTypeError("The namespace-context attribute of xsl:evaluate must return one node.", XsltErrorCodes.XTTE3170, instruction.Instruction);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Kind != XdmItemKind.Node || item.Node == null)
        {
            throw CreateXsltEvaluateTypeError("The namespace-context attribute of xsl:evaluate must return one node.", XsltErrorCodes.XTTE3170, instruction.Instruction);
        }

        return new TreeNavigation().SelectXmlNamespacesOperation(item.Node);
    }

    internal XmlNamespaceBinding[] RemoveXsltEvaluateDefaultNamespace(XmlNamespaceBinding[] source)
    {
        List<XmlNamespaceBinding> result = new List<XmlNamespaceBinding>();
        int index = 0;
        while (index < source.Length)
        {
            XmlNamespaceBinding binding = source[index];
            string prefix = binding.Prefix == null ? "" : binding.Prefix;
            if (prefix.Length != 0)
            {
                result.Add(binding);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal string ReadXsltEvaluateDefaultNamespace(XmlNamespaceBinding[] namespaces, XsltInstructionPlan instruction)
    {
        XsltInstructionAttributePlan? attribute = FindXsltEvaluateAttribute(instruction, "namespace-context");
        if (attribute == null)
        {
            return instruction.StaticContext.XPathDefaultNamespace;
        }

        int index = 0;
        while (index < namespaces.Length)
        {
            XmlNamespaceBinding binding = namespaces[index];
            if ((binding.Prefix == null || binding.Prefix.Length == 0) && !binding.IsUndeclaration)
            {
                return binding.NamespaceUri == null ? "" : binding.NamespaceUri;
            }

            index = index + 1;
        }

        return "";
    }

    internal string ReadXsltEvaluateBaseUri(XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        XsltInstructionAttributePlan? attribute = FindXsltEvaluateAttribute(instruction, "base-uri");
        string source = attribute == null ? instruction.StaticContext.BaseUri == null ? "" : instruction.StaticContext.BaseUri : Templates.SequenceRuntime.EvaluateXsltAttributeTemplate(attribute, invocation, "").Trim();
        string? basis = instruction.StaticContext.BaseUri;
        Task<string> pending = Task.Run(() => ResolveXsltEvaluateBaseUri(source, basis));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status == TaskStatus.RanToCompletion)
        {
            return pending.Result;
        }

        throw CreateXsltEvaluateDynamicError("The base-uri attribute of xsl:evaluate is not a valid URI.", XsltErrorCodes.XTDE3160, instruction.Instruction);
    }

    internal string ResolveXsltEvaluateBaseUri(string source, string? basis)
    {
        if (source.Length == 0)
        {
            return "";
        }

        Uri uri = UriTools.CreateReference(source);
        if (!uri.IsAbsoluteUri)
        {
            if (basis == null || basis.Length == 0)
            {
                throw new UriFormatException("A relative dynamic XPath base URI has no base URI.");
            }

            uri = UriTools.ResolveReference(UriTools.CreateAbsolute(basis), source);
        }

        return UriTools.CanonicalAbsoluteUri(uri);
    }

    internal bool ReadXsltEvaluateSchemaAwareness(XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        XsltInstructionAttributePlan? attribute = FindXsltEvaluateAttribute(instruction, "schema-aware");
        if (attribute == null)
        {
            return false;
        }

        string value = Templates.SequenceRuntime.EvaluateXsltAttributeTemplate(attribute, invocation, "no").Trim();
        if (value == "yes" || value == "true" || value == "1")
        {
            return State.Executable.IsSchemaAware;
        }

        if (value == "no" || value == "false" || value == "0")
        {
            return false;
        }

        throw CreateXsltEvaluateDynamicError("The schema-aware attribute of xsl:evaluate is not a valid boolean.", XsltErrorCodes.XTDE0030, instruction.Instruction);
    }

    internal XPathStaticContext CreateXsltEvaluateStaticContext(XsltInstructionAttributePlan xpath, XPathVariableBinding[] variables, XmlNamespaceBinding[] namespaces, string defaultNamespace, string baseUri, bool schemaAware)
    {
        XPathPlan plan = xpath.Expression == null ? throw new InvalidOperationException("An xsl:evaluate xpath attribute has no plan.") : xpath.Expression;
        XPathStaticContext result = new XPathFunctionContextFactory().CreateXPathFunctionStaticContext(plan.Environment);
        result.LanguageVersion = XPathLanguageVersion.XPath31;
        result.CompatibilityMode = XPathCompatibilityMode.Strict;
        result.Namespaces = namespaces;
        result.DefaultElementNamespace = defaultNamespace;
        result.DefaultFunctionNamespace = XPathNamespaces.Functions;
        result.BaseUri = baseUri.Length == 0 ? null : baseUri;
        result.CollationBindings = FilterXsltEvaluateCollations(plan.Environment.CollationBindings);
        result.Variables = variables;
        result.Functions = FilterXsltEvaluateFunctions(plan.Environment.Functions);
        result.SchemaAware = schemaAware;
        result.SchemaSet = schemaAware ? State.Stylesheet.SchemaSet : null;
        result.ResolverPolicy = State.DynamicContext.ResolverPolicy;
        result.AllowHigherOrderFunctions = true;
        return result;
    }

    internal XPathCollationBinding[] FilterXsltEvaluateCollations(XPathCollationBinding[] source)
    {
        List<XPathCollationBinding> result = new List<XPathCollationBinding>();
        int index = 0;
        while (index < source.Length)
        {
            string? uri = source[index].Uri;
            if (uri != XPathNamespaces.CodepointCollation && uri != XPathAsciiCaseInsensitiveCollation.Uri)
            {
                result.Add(source[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XPathFunctionBinding[] FilterXsltEvaluateFunctions(XPathFunctionBinding[] source)
    {
        List<XPathFunctionBinding> result = new List<XPathFunctionBinding>();
        int index = 0;
        while (index < source.Length)
        {
            XPathFunctionBinding binding = source[index];
            XmlName? name = binding.Name;
            string namespaceUri = name == null || name.NamespaceUri == null ? "" : name.NamespaceUri;
            if (namespaceUri != XPathNamespaces.Functions && namespaceUri != XsltNamespaces.Xslt && IsXsltEvaluateFunctionVisible(binding))
            {
                result.Add(binding);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal bool IsXsltEvaluateFunctionVisible(XPathFunctionBinding binding)
    {
        int index = 0;
        while (index < State.Stylesheet.Functions.Length)
        {
            XsltFunctionPlan function = State.Stylesheet.Functions[index];
            if (SameXsltEvaluateName(function.Binding.Name, binding.Name) && function.Binding.Arity == binding.Arity)
            {
                return function.Visibility != XsltComponentVisibility.Hidden && function.Visibility != XsltComponentVisibility.Private && function.Visibility != XsltComponentVisibility.Abstract;
            }

            index = index + 1;
        }

        return true;
    }

    internal XPathPlan FindOrCompileXsltEvaluatePlan(XsltInstructionPlan instruction, string source, XPathStaticContext context, XPathVariableBinding[] variables, XmlNamespaceBinding[] namespaces, XsltEvaluateOptions options)
    {
        string baseUri = context.BaseUri == null ? "" : context.BaseUri;
        string namespaceKey = CreateXsltEvaluateNamespaceKey(namespaces, context.DefaultElementNamespace);
        string variableKey = CreateXsltEvaluateVariableKey(variables);
        if (options.Cache)
        {
            int index = 0;
            while (index < Cache.Count)
            {
                XsltEvaluateCacheEntry entry = Cache[index];
                if (object.ReferenceEquals(entry.Instruction, instruction.Instruction) && entry.Source == source && entry.BaseUri == baseUri && entry.NamespaceKey == namespaceKey && entry.VariableKey == variableKey && entry.SchemaAware == context.SchemaAware)
                {
                    return entry.Plan;
                }

                index = index + 1;
            }
        }

        Task<XPathPlan> pending = Task.Run(() => new XPathPlanCompiler(context).CompileXPathSource(source, baseUri.Length == 0 ? instruction.Module.BaseUri : baseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            string detail = pending.Exception == null ? "" : " " + pending.Exception.Flatten().InnerExceptions[0].Message;
            throw CreateXsltEvaluateDynamicError("The target expression of xsl:evaluate could not be compiled." + detail, XsltErrorCodes.XTDE3160, instruction.Instruction);
        }

        XPathPlan plan = pending.Result;
        int maximumEntries = State.DynamicContext.SecurityLimits == null || State.DynamicContext.SecurityLimits.MaximumExternalResources <= 0 ? 64 : State.DynamicContext.SecurityLimits.MaximumExternalResources;
        if (options.Cache && Cache.Count < maximumEntries)
        {
            Cache.Add(new XsltEvaluateCacheEntry(instruction.Instruction, source, baseUri, namespaceKey, variableKey, context.SchemaAware, plan));
        }

        return plan;
    }

    internal string CreateXsltEvaluateNamespaceKey(XmlNamespaceBinding[] namespaces, string? defaultNamespace)
    {
        List<string> values = new List<string>();
        int index = 0;
        while (index < namespaces.Length)
        {
            XmlNamespaceBinding binding = namespaces[index];
            values.Add((binding.Prefix == null ? "" : binding.Prefix) + "=" + (binding.NamespaceUri == null ? "" : binding.NamespaceUri));
            index = index + 1;
        }

        values.Sort(StringComparer.Ordinal);
        StringBuilder result = new StringBuilder(defaultNamespace == null ? "" : defaultNamespace);
        index = 0;
        while (index < values.Count)
        {
            result.Append('|');
            result.Append(values[index]);
            index = index + 1;
        }

        return result.ToString();
    }

    internal string CreateXsltEvaluateVariableKey(XPathVariableBinding[] variables)
    {
        List<string> values = new List<string>();
        int index = 0;
        while (index < variables.Length)
        {
            XPathVariableBinding binding = variables[index];
            XmlName? name = binding.Name;
            XPathTypeSyntax? type = binding.StaticTypeSyntax;
            values.Add("Q{" + (name == null || name.NamespaceUri == null ? "" : name.NamespaceUri) + "}" + (name == null || name.LocalName == null ? "" : name.LocalName) + ":" + (type == null ? "" : type.Name + (type.Occurrence == null ? "" : type.Occurrence)));
            index = index + 1;
        }

        values.Sort(StringComparer.Ordinal);
        return string.Join("|", values);
    }

    internal XdmSequence CreateXsltEvaluateContextItem(XsltInstructionPlan instruction, XsltVariableFrame frame)
    {
        XsltInstructionAttributePlan? attribute = FindXsltEvaluateAttribute(instruction, "context-item");
        if (attribute == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XdmSequence result = EvaluateXsltEvaluateAttribute(attribute, frame);
        if (result.Count > 1)
        {
            throw CreateXsltEvaluateTypeError("The context-item attribute of xsl:evaluate must return at most one item.", XsltErrorCodes.XTTE3210, instruction.Instruction);
        }

        return result;
    }

    internal XPathDynamicContext CreateXsltEvaluateDynamicContext(XsltVariableFrame frame, XdmSequence contextItem, XPathVariableBinding[] variables, XsltEvaluateOptions options)
    {
        XPathDynamicContext basis = Templates.Variables.CreateXsltVariableDynamicContext(frame);
        XPathDynamicContext result = new XPathFunctionContextFactory().CopyXPathFunctionDynamicComponents(basis);
        result.ContextItem = contextItem;
        result.Position = contextItem.Count == 0 ? 0 : 1;
        result.Size = contextItem.Count;
        result.Variables = variables;
        result.VariableResolver = null;
        result.XsltCurrentGroupAvailable = false;
        result.XsltCurrentGroup = null;
        result.XsltCurrentGroupingKeyAvailable = false;
        result.XsltCurrentGroupingKey = null;
        result.XsltCurrentMergeGroupAvailable = false;
        result.XsltCurrentMergeGroup = null;
        result.XsltCurrentMergeSourceNames = null;
        result.XsltCurrentMergeSourceGroups = null;
        result.XsltCurrentMergeKeyAvailable = false;
        result.XsltCurrentMergeKey = null;
        result.XsltKeys = null;
        result.XsltAccumulators = null;
        result.XsltRegexGroups = null;
        result.XsltCurrentComponent = null;
        result.MaximumOperations = SelectXsltEvaluateOperationLimit(options);
        if (!options.AllowExternalResources)
        {
            XmlResolverPolicy policy = new XPathRuntimeContext(new XPathDynamicContext { ResolverPolicy = result.ResolverPolicy }).ResourcePolicy;
            policy.Access = XmlResourceAccess.Denied;
            policy.AllowedSchemes = [];
            policy.AllowedRoots = [];
            policy.CatalogEntries = [];
            result.ResolverPolicy = policy;
            result.CollectionResolver = null;
        }

        return result;
    }

    internal long SelectXsltEvaluateOperationLimit(XsltEvaluateOptions options)
    {
        long inherited = State.DynamicContext.MaximumOperations;
        XmlSecurityLimits? limits = State.DynamicContext.SecurityLimits;
        if (inherited <= 0 && limits != null && limits.MaximumNodeCount > 0)
        {
            inherited = limits.MaximumNodeCount;
        }

        if (inherited <= 0)
        {
            inherited = 1000000;
        }

        return options.MaximumOperations > 0 && options.MaximumOperations < inherited ? options.MaximumOperations : inherited;
    }

    internal XPathStaticContext CreateXsltEvaluateConversionContext(XsltInstructionPlan instruction)
    {
        XPathStaticContext result = new XPathStaticContext();
        result.LanguageVersion = XPathLanguageVersion.XPath31;
        result.CompatibilityMode = XPathCompatibilityMode.Strict;
        result.Namespaces = instruction.StaticContext.Namespaces;
        result.DefaultElementNamespace = instruction.StaticContext.XPathDefaultNamespace;
        result.DefaultFunctionNamespace = XPathNamespaces.Functions;
        result.BaseUri = instruction.StaticContext.BaseUri;
        result.SchemaSet = State.Stylesheet.SchemaSet;
        result.SchemaAware = State.Executable.IsSchemaAware;
        result.AllowHigherOrderFunctions = true;
        return result;
    }

    internal XsltInstructionAttributePlan? FindXsltEvaluateAttribute(XsltInstructionPlan instruction, string name)
    {
        int index = 0;
        while (index < instruction.Attributes.Length)
        {
            if (instruction.Attributes[index].Name == name)
            {
                return instruction.Attributes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool SameXsltEvaluateName(XmlName? left, XmlName? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return left.LocalName == right.LocalName && (left.NamespaceUri == null ? "" : left.NamespaceUri) == (right.NamespaceUri == null ? "" : right.NamespaceUri);
    }

    internal XmlName CopyXsltEvaluateName(XmlName source)
    {
        XmlName result = new XmlName();
        result.Prefix = source.Prefix;
        result.LocalName = source.LocalName;
        result.NamespaceUri = source.NamespaceUri;
        result.QualifiedName = source.QualifiedName;
        return result;
    }

    internal XsltDynamicException CreateXsltEvaluateDynamicError(string message, string code, XmlNode node)
    {
        return Templates.CreateXsltTemplateDynamicError(message, code, node);
    }

    internal XsltTypeException CreateXsltEvaluateTypeError(string message, string code, XmlNode node)
    {
        return Templates.CreateXsltTemplateTypeError(message, code, node);
    }
}
