using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace xml;

internal class XsltStaticPreprocessor
{
    internal XsltStylesheetLoader Loader;
    internal List<XsltStaticVariable> StaticVariables;
    internal List<XPathVariableBinding> ActiveVariables;
    internal List<XmlNode> ShadowedAttributes;
    internal TreeMutation Mutation;
    internal TreeContentMutation ContentMutation;
    internal DateTimeOffset CurrentDateTime;

    internal XsltStaticPreprocessor(XsltStylesheetLoader loader)
    {
        Loader = loader;
        StaticVariables = new List<XsltStaticVariable>();
        ActiveVariables = new List<XPathVariableBinding>();
        ShadowedAttributes = new List<XmlNode>();
        Mutation = new TreeMutation();
        ContentMutation = new TreeContentMutation(Mutation);
        CurrentDateTime = DateTimeOffset.UtcNow;
    }

    internal void ProcessXsltRootShadowAttributes(XmlNode root, string? location)
    {
        if (Loader.XsltNodeNamespace(root) == XsltNamespaces.Xslt)
        {
            EvaluateXsltShadowAttributes(root, null, location);
        }
    }

    internal bool ProcessXsltModuleRoot(XmlNode root, XsltStylesheetModule module)
    {
        string? useWhen = ReadXsltUseWhen(root);
        if (useWhen == null)
        {
            return true;
        }

        return EvaluateXsltStaticBoolean(useWhen, root, module, module.BaseUri);
    }

    internal bool ProcessXsltElementTree(XmlNode node, XsltStylesheetModule module)
    {
        if (Loader.XsltNodeNamespace(node) == XsltNamespaces.Xslt)
        {
            EvaluateXsltShadowAttributes(node, module, module.BaseUri);
        }

        string? useWhen = ReadXsltUseWhen(node);
        if (useWhen != null && !EvaluateXsltStaticBoolean(useWhen, node, module, module.BaseUri))
        {
            return false;
        }

        bool variable = Loader.XsltNodeNamespace(node) == XsltNamespaces.Xslt && (Loader.XsltNodeLocalName(node) == "variable" || Loader.XsltNodeLocalName(node) == "param");
        string? staticValue = variable ? Loader.FindXsltAttribute(node, "static", "") : null;
        bool isStatic = staticValue != null && ParseXsltStaticBoolean(staticValue, "static", node, module);
        if (isStatic)
        {
            if (!IsXsltTopLevelVariable(node))
            {
                throw CreateXsltPreprocessingError("The value static='yes' is permitted only on a top-level variable or parameter.", XsltErrorCodes.XTSE0020, node, module, module.BaseUri);
            }

            EvaluateXsltStaticVariable(node, module);
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element && !ProcessXsltElementTree(child, module))
            {
                Mutation.RemoveXmlTreeNodeOperation(node, child);
            }

            index = index + 1;
        }

        return true;
    }

    internal void EvaluateXsltShadowAttributes(XmlNode node, XsltStylesheetModule? module, string? location)
    {
        EvaluateXsltPriorityShadowAttribute(node, "_xpath-default-namespace", module, location);
        EvaluateXsltPriorityShadowAttribute(node, "_default-collation", module, location);
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode shadow = attributes[index];
            string localName = Loader.XsltNodeLocalName(shadow);
            if (Loader.XsltNodeNamespace(shadow).Length == 0 && localName.Length > 1 && localName[0] == '_' && localName[1] != '_' &&
                localName != "_xpath-default-namespace" && localName != "_default-collation")
            {
                string targetName = localName.Substring(1);
                string template = shadow.Value == null ? "" : shadow.Value;
                string value = EvaluateXsltStaticValueTemplate(template, node, module, location);
                ReplaceXsltShadowAttribute(node, shadow, targetName, value);
            }

            index = index + 1;
        }
    }

    internal void EvaluateXsltPriorityShadowAttribute(XmlNode node, string shadowName, XsltStylesheetModule? module, string? location)
    {
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode shadow = attributes[index];
            if (Loader.XsltNodeNamespace(shadow).Length == 0 && Loader.XsltNodeLocalName(shadow) == shadowName)
            {
                string template = shadow.Value == null ? "" : shadow.Value;
                string value = EvaluateXsltStaticValueTemplate(template, node, module, location);
                ReplaceXsltShadowAttribute(node, shadow, shadowName.Substring(1), value);
                return;
            }

            index = index + 1;
        }
    }

    internal void ReplaceXsltShadowAttribute(XmlNode node, XmlNode shadow, string targetName, string value)
    {
        XmlDocument? optionalDocument = node.OwnerDocument;
        if (optionalDocument == null)
        {
            return;
        }

        XmlName name = new XmlName();
        name.LocalName = targetName;
        name.NamespaceUri = "";
        name.QualifiedName = targetName;
        XmlNode target = Mutation.CreateXmlTreeNodeOperation(optionalDocument, XmlNodeKind.Attribute, name, value);
        target.Range = shadow.Range;
        ContentMutation.SetXmlTreeAttributeOperation(node, target);
        ShadowedAttributes.Add(target);
        XmlName? shadowName = shadow.Name;
        if (shadowName != null)
        {
            ContentMutation.RemoveXmlTreeAttributeOperation(node, shadowName);
        }
    }

    internal string EvaluateXsltStaticValueTemplate(string value, XmlNode node, XsltStylesheetModule? module, string? location)
    {
        XsltValueTemplate template = new XsltValueTemplateParser(Loader).ParseXsltValueTemplate(value, node, XsltValueTemplateKind.Shadow, module, location);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < template.Parts.Length)
        {
            XsltValueTemplatePart part = template.Parts[index];
            if (part.Kind == XsltValueTemplatePartKind.Fixed)
            {
                result.Append(part.Value);
            }
            else if (part.Value.Length != 0)
            {
                XdmSequence evaluated = EvaluateXsltStaticExpression(part.Value, node, module, location);
                result.Append(ConvertXsltStaticSequenceToString(evaluated, node, module, location));
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal bool IsXsltShadowedAttribute(XmlNode node)
    {
        int index = 0;
        while (index < ShadowedAttributes.Count)
        {
            if (object.ReferenceEquals(ShadowedAttributes[index], node))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void EvaluateXsltStaticVariable(XmlNode node, XsltStylesheetModule module)
    {
        string? lexicalName = Loader.FindXsltAttribute(node, "name", "");
        if (lexicalName == null || lexicalName.Trim().Length == 0)
        {
            throw CreateXsltPreprocessingError("A static variable or parameter requires a name.", XsltErrorCodes.XTSE0010, node, module, module.BaseUri);
        }

        XmlName name = ResolveXsltStaticVariableName(lexicalName, node, module);
        string expandedName = "Q{" + (name.NamespaceUri == null ? "" : name.NamespaceUri) + "}" + (name.LocalName == null ? "" : name.LocalName);
        string localName = Loader.XsltNodeLocalName(node);
        bool parameter = localName == "param";
        string? requiredValue = Loader.FindXsltAttribute(node, "required", "");
        bool required = requiredValue != null && ParseXsltStaticBoolean(requiredValue, "required", node, module);
        string? select = Loader.FindXsltAttribute(node, "select", "");
        ValidateXsltStaticVariableContent(node, parameter, required, select, module);
        XsltParameter? supplied = parameter ? FindXsltSuppliedStaticParameter(name) : null;
        XdmSequence value;
        if (supplied != null && supplied.Value != null)
        {
            value = new XdmSequenceOperations().FreezeXdmSequenceStorage(supplied.Value);
        }
        else if (required)
        {
            throw CreateXsltPreprocessingError("A required static parameter value was not supplied.", XsltErrorCodes.XTDE0050, node, module, module.BaseUri);
        }
        else if (select != null)
        {
            if (ReferencesXsltOwnStaticVariable(select, lexicalName.Trim()))
            {
                throw CreateXsltPreprocessingError("A static variable has a circular dependency on itself.", XsltErrorCodes.XTDE0640, node, module, module.BaseUri);
            }

            value = EvaluateXsltStaticExpression(select, node, module, module.BaseUri);
        }
        else
        {
            value = new XdmSequenceOperations().CreateEmptyXdmSequence();
        }

        XsltStaticVariable variable = new XsltStaticVariable(node, module, name, expandedName, value);
        variable.Select = select;
        variable.IsParameter = parameter;
        variable.IsRequired = required;
        variable.WasSupplied = supplied != null && supplied.Value != null;
        variable.DeclarationOrder = StaticVariables.Count;
        StaticVariables.Add(variable);
        SetXsltActiveStaticVariable(variable);
    }

    internal void ValidateXsltStaticVariableContent(XmlNode node, bool parameter, bool required, string? select, XsltStylesheetModule module)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            bool whitespace = (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value);
            if (!whitespace)
            {
                throw CreateXsltPreprocessingError("A static variable or parameter must have empty content.", XsltErrorCodes.XTSE0010, node, module, module.BaseUri);
            }

            index = index + 1;
        }

        if (!parameter && select == null)
        {
            throw CreateXsltPreprocessingError("A static variable requires a select attribute.", XsltErrorCodes.XTSE0010, node, module, module.BaseUri);
        }

        if (parameter && required && select != null)
        {
            throw CreateXsltPreprocessingError("A required static parameter must not specify a default value.", XsltErrorCodes.XTSE0010, node, module, module.BaseUri);
        }
    }

    internal bool ReferencesXsltOwnStaticVariable(string expression, string lexicalName)
    {
        string compact = expression.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");
        return compact.IndexOf("$" + lexicalName, StringComparison.Ordinal) >= 0;
    }

    internal bool IsXsltTopLevelVariable(XmlNode node)
    {
        XmlNode? parent = node.Parent;
        if (parent == null || Loader.XsltNodeNamespace(parent) != XsltNamespaces.Xslt)
        {
            return false;
        }

        string localName = Loader.XsltNodeLocalName(parent);
        return localName == "stylesheet" || localName == "transform" || localName == "package" || localName == "override";
    }

    internal string? ReadXsltUseWhen(XmlNode node)
    {
        string namespaceUri = Loader.XsltNodeNamespace(node) == XsltNamespaces.Xslt ? "" : XsltNamespaces.Xslt;
        return Loader.FindXsltAttribute(node, "use-when", namespaceUri);
    }

    internal bool EvaluateXsltStaticBoolean(string expression, XmlNode node, XsltStylesheetModule? module, string? location)
    {
        XdmSequence value = EvaluateXsltStaticExpression(expression, node, module, location);
        Task<bool> pending = Task.Run(() => new XdmTypeConverter().EffectiveBooleanValueXdmSequence(value));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltPreprocessingError("A use-when expression has no effective boolean value.", ReadXsltTaskErrorCode(pending.Exception), node, module, location);
        }

        return pending.Result;
    }

    internal XdmSequence EvaluateXsltStaticExpression(string expression, XmlNode node, XsltStylesheetModule? module, string? location)
    {
        XPathStaticContext staticContext = CreateXsltXPathStaticContext(node);
        XPathDynamicContext dynamicContext = CreateXsltXPathDynamicContext();
        Task<XdmSequence> pending = Task.Run(() => EvaluateXsltStaticExpressionCore(expression, staticContext, dynamicContext));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltPreprocessingError("A static XPath expression could not be evaluated.", ReadXsltTaskErrorCode(pending.Exception), node, module, location);
        }

        return pending.Result;
    }

    internal XdmSequence EvaluateXsltStaticExpressionCore(string expression, XPathStaticContext staticContext, XPathDynamicContext dynamicContext)
    {
        XPathCompiledExpression compiled = new XPathCompiler().CompileXPath(expression, staticContext);
        XPathEvaluationResult evaluated = new XPathEvaluator().EvaluateXPath(compiled, dynamicContext);
        return evaluated.Value == null ? new XdmSequenceOperations().CreateEmptyXdmSequence() : evaluated.Value;
    }

    internal XPathStaticContext CreateXsltXPathStaticContext(XmlNode node)
    {
        NamespaceContext namespaces = BuildXsltStaticNamespaceContext(node);
        XPathStaticContext result = new XPathStaticContext();
        result.LanguageVersion = XPathLanguageVersion.XPath31;
        result.CompatibilityMode = XPathCompatibilityMode.Strict;
        result.Namespaces = namespaces.EnumerateInScopeXmlNamespaces();
        result.DefaultElementNamespace = ReadXsltStaticXPathDefaultNamespace(node);
        result.DefaultFunctionNamespace = XPathNamespaces.Functions;
        result.BaseUri = Loader.ReadXsltNodeBaseUri(node, Loader.Options.BaseUri);
        result.DefaultCollation = ReadXsltStaticDefaultCollation(node, result.BaseUri);
        result.Collations = [XPathNamespaces.CodepointCollation, XPathAsciiCaseInsensitiveCollation.Uri];
        result.Variables = ActiveVariables.ToArray();
        XsltStaticFunction functions = new XsltStaticFunction(Loader, result.Namespaces, result.BaseUri);
        result.Functions = CreateXsltStaticFunctionBindings();
        result.FunctionResolver = functions;
        result.FunctionsArePrevalidated = true;
        result.SchemaSet = Loader.Options.SchemaSet;
        result.ResolverPolicy = Loader.Policy;
        result.SchemaAware = Loader.Options.ConformanceLevel == XsltConformanceLevel.SchemaAware;
        result.AllowHigherOrderFunctions = true;
        return result;
    }

    internal string ReadXsltStaticDefaultCollation(XmlNode node, string? baseUri)
    {
        XmlNode? current = node;
        while (current != null && current.Kind != XmlNodeKind.Document)
        {
            string attributeNamespace = Loader.XsltNodeNamespace(current) == XsltNamespaces.Xslt ? "" : XsltNamespaces.Xslt;
            string? value = Loader.FindXsltAttribute(current, "default-collation", attributeNamespace);
            if (value != null)
            {
                string[] candidates = value.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                int index = 0;
                while (index < candidates.Length)
                {
                    string? resolved = new XsltStaticContextCompiler(Loader).ResolveXsltCollationUri(candidates[index], baseUri);
                    if (resolved == XPathNamespaces.CodepointCollation || resolved == XPathAsciiCaseInsensitiveCollation.Uri)
                    {
                        return resolved;
                    }

                    index = index + 1;
                }

                return XPathNamespaces.CodepointCollation;
            }

            current = current.Parent;
        }

        return XPathNamespaces.CodepointCollation;
    }

    internal XPathFunctionBinding[] CreateXsltStaticFunctionBindings()
    {
        return
        [
            CreateXsltSequenceFunctionBinding("available-system-properties", 0, XdmAtomicType.QName, XdmOccurrence.ZeroOrMore),
            CreateXsltSequenceFunctionBinding("copy-of", 0, XdmAtomicType.AnyAtomicType, XdmOccurrence.ExactlyOne),
            CreateXsltSequenceFunctionBinding("copy-of", 1, XdmAtomicType.AnyAtomicType, XdmOccurrence.ZeroOrMore),
            CreateXsltContextFunctionBinding("current"),
            CreateXsltSequenceFunctionBinding("current-output-uri", 0, XdmAtomicType.AnyUri, XdmOccurrence.ZeroOrOne),
            CreateXsltSequenceFunctionBinding("document", 1, XdmAtomicType.AnyAtomicType, XdmOccurrence.ZeroOrMore),
            CreateXsltSequenceFunctionBinding("document", 2, XdmAtomicType.AnyAtomicType, XdmOccurrence.ZeroOrMore),
            CreateXsltStaticFunctionBinding("system-property", 1, XdmAtomicType.String),
            CreateXsltStaticFunctionBinding("element-available", 1, XdmAtomicType.Boolean),
            CreateXsltStaticFunctionBinding("function-available", 1, XdmAtomicType.Boolean),
            CreateXsltStaticFunctionBinding("function-available", 2, XdmAtomicType.Boolean),
            CreateXsltStaticFunctionBinding("type-available", 1, XdmAtomicType.Boolean),
            CreateXsltSequenceFunctionBinding("snapshot", 0, XdmAtomicType.AnyAtomicType, XdmOccurrence.ExactlyOne),
            CreateXsltSequenceFunctionBinding("snapshot", 1, XdmAtomicType.AnyAtomicType, XdmOccurrence.ZeroOrMore),
            CreateXsltStaticFunctionBinding("stream-available", 1, XdmAtomicType.Boolean),
            CreateXsltSequenceFunctionBinding("unparsed-entity-public-id", 1, XdmAtomicType.String, XdmOccurrence.ExactlyOne),
            CreateXsltSequenceFunctionBinding("unparsed-entity-public-id", 2, XdmAtomicType.String, XdmOccurrence.ExactlyOne),
            CreateXsltSequenceFunctionBinding("unparsed-entity-uri", 1, XdmAtomicType.AnyUri, XdmOccurrence.ExactlyOne),
            CreateXsltSequenceFunctionBinding("unparsed-entity-uri", 2, XdmAtomicType.AnyUri, XdmOccurrence.ExactlyOne),
            CreateXsltContextFunctionBinding("current-group"),
            CreateXsltContextFunctionBinding("current-grouping-key"),
            CreateXsltRegexGroupFunctionBinding()
        ];
    }

    internal XPathFunctionBinding CreateXsltRegexGroupFunctionBinding()
    {
        XPathFunctionBinding result = CreateXsltStaticFunctionBinding("regex-group", 1, XdmAtomicType.String);
        result.ParameterTypes = [XdmAtomicType.Integer];
        return result;
    }

    internal XPathFunctionBinding CreateXsltContextFunctionBinding(string localName)
    {
        XPathFunctionBinding result = CreateXsltStaticFunctionBinding(localName, 0, XdmAtomicType.AnyAtomicType);
        result.ParameterTypes = null;
        result.ParameterOccurrences = null;
        result.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        SourceRange range = new SourceRange();
        result.ResultTypeSyntax = new XPathTypeSyntax("item", null, null, null, "*", false, range);
        return result;
    }

    internal XPathFunctionBinding CreateXsltSequenceFunctionBinding(string localName, int arity, XdmAtomicType resultType, XdmOccurrence resultOccurrence)
    {
        XPathFunctionBinding result = CreateXsltStaticFunctionBinding(localName, arity, resultType);
        result.ParameterTypes = null;
        result.ParameterOccurrences = null;
        result.ResultOccurrence = resultOccurrence;
        SourceRange range = new SourceRange();
        result.ResultTypeSyntax = new XPathTypeSyntax(resultType == XdmAtomicType.QName ? "QName" : resultType == XdmAtomicType.AnyUri ? "anyURI" : "item", resultType == XdmAtomicType.QName || resultType == XdmAtomicType.AnyUri ? "xs" : null, resultType == XdmAtomicType.QName || resultType == XdmAtomicType.AnyUri ? XPathNamespaces.XmlSchema : null, null, resultOccurrence == XdmOccurrence.ZeroOrMore ? "*" : resultOccurrence == XdmOccurrence.ZeroOrOne ? "?" : "", false, range);
        return result;
    }

    internal XPathFunctionBinding CreateXsltStaticFunctionBinding(string localName, int arity, XdmAtomicType resultType)
    {
        XPathFunctionBinding result = new XPathFunctionBinding();
        XmlName name = new XmlName();
        name.LocalName = localName;
        name.NamespaceUri = XPathNamespaces.Functions;
        name.QualifiedName = "fn:" + localName;
        result.Name = name;
        result.Arity = arity;
        result.ParameterTypes = new XdmAtomicType[arity];
        result.ParameterOccurrences = new XdmOccurrence[arity];
        int index = 0;
        while (index < arity)
        {
            result.ParameterTypes[index] = index == 1 && localName == "function-available" ? XdmAtomicType.Integer : XdmAtomicType.String;
            result.ParameterOccurrences[index] = XdmOccurrence.ExactlyOne;
            index = index + 1;
        }

        result.ResultType = resultType;
        result.ResultOccurrence = XdmOccurrence.ExactlyOne;
        result.BindingKey = "xslt-static:" + localName + ":" + arity;
        result.IsDeterministic = true;
        return result;
    }

    internal XPathDynamicContext CreateXsltXPathDynamicContext()
    {
        XPathDynamicContext result = new XPathDynamicContext();
        result.ContextItem = null;
        result.Position = 0;
        result.Size = 0;
        result.CurrentDateTime = CurrentDateTime;
        result.ImplicitTimezoneMinutes = 0;
        result.EnvironmentNames = [];
        result.EnvironmentValues = [];
        result.ResolverPolicy = Loader.Policy;
        result.SecurityLimits = Loader.Options.SecurityLimits;
        return result;
    }

    internal NamespaceContext BuildXsltStaticNamespaceContext(XmlNode node)
    {
        List<XmlNode> ancestors = new List<XmlNode>();
        XmlNode? current = node;
        while (current != null && current.Kind != XmlNodeKind.Document)
        {
            ancestors.Add(current);
            current = current.Parent;
        }

        int index = ancestors.Count - 1;
        XmlNamespaceBinding[]? optionalRootBindings = index < 0 ? null : ancestors[index].NamespaceBindings;
        XmlNamespaceBinding[] rootBindings = optionalRootBindings == null ? [] : optionalRootBindings;
        NamespaceContext result = new NamespaceContext(null, rootBindings);
        index = index - 1;
        while (index >= 0)
        {
            XmlNamespaceBinding[]? optionalBindings = ancestors[index].NamespaceBindings;
            XmlNamespaceBinding[] bindings = optionalBindings == null ? [] : optionalBindings;
            result = result.PushXmlNamespaceFrame(bindings);
            index = index - 1;
        }

        return result;
    }

    internal string ReadXsltStaticXPathDefaultNamespace(XmlNode node)
    {
        XmlNode? current = node;
        while (current != null && current.Kind != XmlNodeKind.Document)
        {
            string attributeNamespace = Loader.XsltNodeNamespace(current) == XsltNamespaces.Xslt ? "" : XsltNamespaces.Xslt;
            string? value = Loader.FindXsltAttribute(current, "xpath-default-namespace", attributeNamespace);
            if (value != null)
            {
                return value.Trim();
            }

            current = current.Parent;
        }

        return "";
    }

    internal XmlName ResolveXsltStaticVariableName(string lexicalName, XmlNode node, XsltStylesheetModule module)
    {
        string name = lexicalName.Trim();
        NamespaceContext namespaces = BuildXsltStaticNamespaceContext(node);
        XmlName result = new XmlName();
        if (name.StartsWith("Q{", StringComparison.Ordinal))
        {
            int close = name.IndexOf('}');
            string localName = close < 0 ? "" : name.Substring(close + 1);
            if (close < 2 || !IsValidXsltStaticNcName(localName))
            {
                throw CreateXsltPreprocessingError("A static variable name must be an EQName.", XsltErrorCodes.XTSE0020, node, module, module.BaseUri);
            }

            result.NamespaceUri = name.Substring(2, close - 2);
            result.LocalName = localName;
            result.QualifiedName = name;
            return result;
        }

        int colon = name.IndexOf(':');
        if (colon < 0)
        {
            if (!IsValidXsltStaticNcName(name))
            {
                throw CreateXsltPreprocessingError("A static variable name must be an EQName.", XsltErrorCodes.XTSE0020, node, module, module.BaseUri);
            }

            result.NamespaceUri = "";
            result.LocalName = name;
            result.QualifiedName = name;
            return result;
        }

        string prefix = name.Substring(0, colon);
        string local = name.Substring(colon + 1);
        string? namespaceUri = colon == name.LastIndexOf(':') && IsValidXsltStaticNcName(prefix) && IsValidXsltStaticNcName(local) ? namespaces.LookupXmlNamespaceUri(prefix) : null;
        if (namespaceUri == null)
        {
            throw CreateXsltPreprocessingError("A static variable name uses an invalid or unbound prefix.", XsltErrorCodes.XTSE0280, node, module, module.BaseUri);
        }

        result.Prefix = prefix;
        result.LocalName = local;
        result.NamespaceUri = namespaceUri;
        result.QualifiedName = name;
        return result;
    }

    internal bool IsValidXsltStaticNcName(string value)
    {
        return new XdmStringDatatype().IsValidXdmStringLexical(value, XdmAtomicType.NcName);
    }

    internal XsltParameter? FindXsltSuppliedStaticParameter(XmlName name)
    {
        XsltParameter[] parameters = Loader.Options.StaticParameters == null ? [] : Loader.Options.StaticParameters;
        int index = 0;
        while (index < parameters.Length)
        {
            XsltParameter? parameter = parameters[index];
            if (parameter != null && parameter.Name != null && SameXsltStaticName(parameter.Name, name))
            {
                return parameter;
            }

            index = index + 1;
        }

        return null;
    }

    internal bool SameXsltStaticName(XmlName left, XmlName right)
    {
        string leftNamespace = left.NamespaceUri == null ? "" : left.NamespaceUri;
        string rightNamespace = right.NamespaceUri == null ? "" : right.NamespaceUri;
        return leftNamespace == rightNamespace && left.LocalName == right.LocalName;
    }

    internal void SetXsltActiveStaticVariable(XsltStaticVariable variable)
    {
        XPathVariableBinding binding = new XPathVariableBinding();
        binding.Name = variable.Name;
        binding.Value = variable.Value;
        binding.DeclaredType = XdmAtomicType.AnyAtomicType;
        binding.DeclaredOccurrence = XdmOccurrence.ZeroOrMore;
        int index = 0;
        while (index < ActiveVariables.Count)
        {
            XmlName? existingName = ActiveVariables[index].Name;
            if (existingName != null && SameXsltStaticName(existingName, variable.Name))
            {
                ActiveVariables[index] = binding;
                return;
            }

            index = index + 1;
        }

        ActiveVariables.Add(binding);
    }

    internal string ConvertXsltStaticSequenceToString(XdmSequence sequence, XmlNode node, XsltStylesheetModule? module, string? location)
    {
        Task<XdmSequence> pending = Task.Run(() => new XdmTypeConverter().AtomizeXdmSequence(sequence));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltPreprocessingError("A shadow attribute expression cannot be converted to a string.", ReadXsltTaskErrorCode(pending.Exception), node, module, location);
        }

        XdmSequence atomized = pending.Result;
        XdmSequenceOperations sequences = new XdmSequenceOperations();
        XdmTypeConverter converter = new XdmTypeConverter();
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < atomized.Count)
        {
            XdmSequenceItem item = sequences.ReadXdmSequenceItem(atomized, index);
            if (item.AtomicValue != null)
            {
                if (result.Length > 0)
                {
                    result.Append(' ');
                }

                result.Append(converter.ReadXdmAtomicStringValue(item.AtomicValue));
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal string ReadXsltTaskErrorCode(AggregateException? aggregate)
    {
        if (aggregate == null || aggregate.InnerException == null)
        {
            return XsltErrorCodes.XTSE0020;
        }

        Exception failure = aggregate.InnerException;
        XPathStaticException? staticFailure = failure as XPathStaticException;
        if (staticFailure != null && staticFailure.Diagnostic != null && staticFailure.Diagnostic.Code != null)
        {
            return staticFailure.Diagnostic.Code;
        }

        XPathDynamicException? dynamicFailure = failure as XPathDynamicException;
        if (dynamicFailure != null && dynamicFailure.Diagnostic != null && dynamicFailure.Diagnostic.Code != null)
        {
            return dynamicFailure.Diagnostic.Code;
        }

        XPathTypeException? typeFailure = failure as XPathTypeException;
        if (typeFailure != null && typeFailure.Diagnostic != null && typeFailure.Diagnostic.Code != null)
        {
            return typeFailure.Diagnostic.Code;
        }

        return XsltErrorCodes.XTSE0020;
    }

    internal bool ParseXsltStaticBoolean(string value, string attributeName, XmlNode node, XsltStylesheetModule module)
    {
        string normalized = value.Trim();
        if (normalized == "yes" || normalized == "true" || normalized == "1")
        {
            return true;
        }

        if (normalized == "no" || normalized == "false" || normalized == "0")
        {
            return false;
        }

        throw CreateXsltPreprocessingError("The " + attributeName + " attribute must be an XML boolean.", XsltErrorCodes.XTSE0020, node, module, module.BaseUri);
    }

    internal void CompleteXsltStaticVariables(XsltStylesheetPlan plan)
    {
        int index = 0;
        while (index < StaticVariables.Count)
        {
            XsltStaticVariable variable = StaticVariables[index];
            XsltStylesheetDeclaration? declaration = FindXsltStaticDeclaration(plan, variable.Declaration);
            if (declaration != null)
            {
                variable.ImportPrecedence = declaration.ImportPrecedence;
                variable.DeclarationOrder = declaration.DeclarationOrder;
            }

            index = index + 1;
        }

        ValidateXsltStaticVariableDuplicates();
        plan.StaticVariables = StaticVariables.ToArray();
    }

    internal XsltStylesheetDeclaration? FindXsltStaticDeclaration(XsltStylesheetPlan plan, XmlNode node)
    {
        int index = 0;
        while (index < plan.Declarations.Length)
        {
            if (object.ReferenceEquals(plan.Declarations[index].Node, node))
            {
                return plan.Declarations[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void ValidateXsltStaticVariableDuplicates()
    {
        int index = 0;
        while (index < StaticVariables.Count)
        {
            XsltStaticVariable current = StaticVariables[index];
            int earlier = 0;
            while (earlier < index)
            {
                XsltStaticVariable previous = StaticVariables[earlier];
                if (previous.ExpandedName == current.ExpandedName)
                {
                    if (previous.ImportPrecedence == current.ImportPrecedence && !HasXsltHigherStaticPrecedence(current.ExpandedName, current.ImportPrecedence))
                    {
                        throw CreateXsltPreprocessingError("Static variables with the same name and import precedence are ambiguous.", XsltErrorCodes.XTSE0630, current.Declaration, current.Module, current.Module.BaseUri);
                    }

                    if (previous.ImportPrecedence < current.ImportPrecedence && !AreXsltStaticVariablesConsistent(previous, current))
                    {
                        throw CreateXsltPreprocessingError("A later static variable at higher import precedence is inconsistent with the earlier declaration.", XsltErrorCodes.XTSE3450, current.Declaration, current.Module, current.Module.BaseUri);
                    }
                }

                earlier = earlier + 1;
            }

            index = index + 1;
        }
    }

    internal bool HasXsltHigherStaticPrecedence(string expandedName, int precedence)
    {
        int index = 0;
        while (index < StaticVariables.Count)
        {
            if (StaticVariables[index].ExpandedName == expandedName && StaticVariables[index].ImportPrecedence > precedence)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool AreXsltStaticVariablesConsistent(XsltStaticVariable left, XsltStaticVariable right)
    {
        if (left.IsParameter != right.IsParameter || ContainsXsltStaticFunctionItem(left.Value) || ContainsXsltStaticFunctionItem(right.Value))
        {
            return false;
        }

        if (left.Value.Count != right.Value.Count)
        {
            return false;
        }

        XdmSequenceOperations sequences = new XdmSequenceOperations();
        XdmTypeConverter converter = new XdmTypeConverter();
        int index = 0;
        while (index < left.Value.Count)
        {
            XdmSequenceItem leftItem = sequences.ReadXdmSequenceItem(left.Value, index);
            XdmSequenceItem rightItem = sequences.ReadXdmSequenceItem(right.Value, index);
            if (leftItem.Kind != XdmItemKind.Atomic || rightItem.Kind != XdmItemKind.Atomic || leftItem.AtomicValue == null || rightItem.AtomicValue == null ||
                leftItem.AtomicValue.Type != rightItem.AtomicValue.Type || converter.ReadXdmAtomicStringValue(leftItem.AtomicValue) != converter.ReadXdmAtomicStringValue(rightItem.AtomicValue))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool ContainsXsltStaticFunctionItem(XdmSequence value)
    {
        XdmSequenceOperations sequences = new XdmSequenceOperations();
        int index = 0;
        while (index < value.Count)
        {
            if (sequences.ReadXdmSequenceItem(value, index).Kind == XdmItemKind.Function)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XsltStaticException CreateXsltPreprocessingError(string message, string code, XmlNode node, XsltStylesheetModule? module, string? location)
    {
        if (module != null)
        {
            return new XsltStylesheetAssembler(Loader).CreateXsltAssemblyError(message, code, node, module);
        }

        return Loader.CreateXsltStaticError(message, code, node, location);
    }
}
