using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XsltFunctionCompiler
{
    internal XsltStylesheetLoader Loader;

    internal XsltFunctionCompiler(XsltStylesheetLoader loader)
    {
        Loader = loader;
    }

    internal void DeclareXsltFunctions(XsltStylesheetPlan stylesheet)
    {
        List<XsltFunctionPlan> functions = new List<XsltFunctionPlan>();
        int moduleIndex = 0;
        while (moduleIndex < stylesheet.Modules.Length)
        {
            XsltStylesheetModule module = stylesheet.Modules[moduleIndex];
            if (module.Kind != XsltStylesheetModuleKind.Simplified)
            {
                CollectXsltFunctionDeclarations(module.Root, module, stylesheet, functions);
            }

            moduleIndex = moduleIndex + 1;
        }

        stylesheet.Functions = functions.ToArray();
        ValidateXsltFunctionDuplicates(stylesheet);
    }

    internal void CollectXsltFunctionDeclarations(XmlNode node, XsltStylesheetModule module, XsltStylesheetPlan stylesheet, List<XsltFunctionPlan> functions)
    {
        if (node.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(node) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(node) == "function")
        {
            if (!IsXsltFunctionDeclarationPosition(node, module))
            {
                throw CreateXsltFunctionError("An xsl:function element must be a top-level or overriding declaration.", XsltErrorCodes.XTSE0010, node, module);
            }

            functions.Add(CreateXsltFunctionDeclaration(node, module, stylesheet));
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.Element)
            {
                CollectXsltFunctionDeclarations(children[index], module, stylesheet, functions);
            }

            index = index + 1;
        }
    }

    internal bool IsXsltFunctionDeclarationPosition(XmlNode node, XsltStylesheetModule module)
    {
        if (object.ReferenceEquals(node.Parent, module.Root))
        {
            return true;
        }

        XmlNode? parent = node.Parent;
        if (parent == null || Loader.XsltNodeNamespace(parent) != XsltNamespaces.Xslt || Loader.XsltNodeLocalName(parent) != "override")
        {
            return false;
        }

        XmlNode? usePackage = parent.Parent;
        return usePackage != null && Loader.XsltNodeNamespace(usePackage) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(usePackage) == "use-package" &&
            object.ReferenceEquals(usePackage.Parent, module.Root);
    }

    internal XsltFunctionPlan CreateXsltFunctionDeclaration(XmlNode node, XsltStylesheetModule module, XsltStylesheetPlan stylesheet)
    {
        XsltStaticContext? context = FindXsltFunctionStaticContext(stylesheet, node);
        if (context == null)
        {
            throw CreateXsltFunctionError("A stylesheet function has no static context.", XsltErrorCodes.XTSE0010, node, module);
        }

        string? lexicalName = Loader.FindXsltAttribute(node, "name", "");
        if (lexicalName == null || lexicalName.Trim().Length == 0)
        {
            throw CreateXsltFunctionError("An xsl:function declaration requires a name.", XsltErrorCodes.XTSE0010, node, module);
        }

        XmlName name = Loader.StaticProcessor.ResolveXsltStaticVariableName(lexicalName, node, module);
        string namespaceUri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        if (namespaceUri.Length == 0)
        {
            throw CreateXsltFunctionError("A stylesheet function name must be in a namespace.", XsltErrorCodes.XTSE0740, node, module);
        }

        if (IsXsltFunctionReservedNamespace(namespaceUri))
        {
            throw CreateXsltFunctionError("A stylesheet function name must not use a reserved namespace.", XsltErrorCodes.XTSE0080, node, module);
        }

        string expandedName = CreateXsltFunctionExpandedName(name);
        XsltFunctionPlan result = new XsltFunctionPlan(node, module, context, name, expandedName);
        result.ImportPrecedence = new XsltVariableCompiler(Loader).ReadXsltVariableImportPrecedence(stylesheet, node);
        result.DeclarationOrder = new XsltVariableCompiler(Loader).ReadXsltVariableDeclarationOrder(stylesheet, node);
        result.IsOverride = IsXsltFunctionInsideOverride(node);
        result.ParameterTypes = ReadXsltFunctionParameterTypes(result);
        result.Arity = result.ParameterTypes.Length;
        ReadXsltFunctionReturnType(result);
        ReadXsltFunctionFlags(result);
        result.IsOverriddenByExtension = !result.OverrideExtensionFunction && FindXsltFunctionExtension(result) != null;
        CreateXsltFunctionBinding(result);
        return result;
    }

    internal bool IsXsltFunctionReservedNamespace(string namespaceUri)
    {
        return new XsltStaticContextCompiler(Loader).IsXsltReservedNamespace(namespaceUri) || namespaceUri == XPathNamespaces.Functions ||
            namespaceUri == XPathNamespaces.Math || namespaceUri == XPathNamespaces.Map || namespaceUri == XPathNamespaces.Array || namespaceUri == XPathNamespaces.Errors;
    }

    internal XsltExtensionBinding? FindXsltFunctionExtension(XsltFunctionPlan function)
    {
        XsltExtensionBinding[] extensions = Loader.Options.Extensions == null ? [] : Loader.Options.Extensions;
        int index = 0;
        while (index < extensions.Length)
        {
            XsltExtensionBinding? extension = extensions[index];
            if (extension != null && extension.IsFunction && SameXsltFunctionName(extension.Name, function.Name))
            {
                return extension;
            }

            index = index + 1;
        }

        return null;
    }

    internal string CreateXsltFunctionExpandedName(XmlName name)
    {
        return "Q{" + (name.NamespaceUri == null ? "" : name.NamespaceUri) + "}" + (name.LocalName == null ? "" : name.LocalName);
    }

    internal XPathTypeSyntax?[] ReadXsltFunctionParameterTypes(XsltFunctionPlan function)
    {
        List<XPathTypeSyntax?> result = new List<XPathTypeSyntax?>();
        XmlNode[] children = function.Declaration.Children == null ? [] : function.Declaration.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(child) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(child) == "param")
            {
                string? source = Loader.FindXsltAttribute(child, "as", "");
                result.Add(source == null ? null : ParseXsltFunctionSequenceType(source, function.StaticContext, child, function.Module));
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal void ReadXsltFunctionReturnType(XsltFunctionPlan function)
    {
        string? source = Loader.FindXsltAttribute(function.Declaration, "as", "");
        if (source != null)
        {
            function.ReturnTypeSource = source;
            function.ReturnType = ParseXsltFunctionSequenceType(source, function.StaticContext, function.Declaration, function.Module);
        }
    }

    internal XPathTypeSyntax ParseXsltFunctionSequenceType(string source, XsltStaticContext staticContext, XmlNode node, XsltStylesheetModule module)
    {
        XPathStaticContext context = new XsltVariableCompiler(Loader).CreateXsltVariableXPathContext(staticContext, []);
        Task<XPathTypeSyntax> pending = Task.Run(() => new XsltVariableCompiler(Loader).ParseXsltVariableSequenceType(source, context, module.BaseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltFunctionTaskError("A stylesheet function has an invalid sequence type.", node, module, pending.Exception);
        }

        return pending.Result;
    }

    internal void ReadXsltFunctionFlags(XsltFunctionPlan function)
    {
        string? cache = Loader.FindXsltAttribute(function.Declaration, "cache", "");
        if (cache != null)
        {
            function.Cache = Loader.StaticProcessor.ParseXsltStaticBoolean(cache, "cache", function.Declaration, function.Module);
        }

        string? newEachTime = Loader.FindXsltAttribute(function.Declaration, "new-each-time", "");
        if (newEachTime != null)
        {
            string normalized = newEachTime.Trim();
            if (normalized == "yes" || normalized == "true" || normalized == "1")
            {
                function.Determinism = XsltFunctionDeterminism.Proactive;
            }
            else if (normalized == "no" || normalized == "false" || normalized == "0")
            {
                function.Determinism = XsltFunctionDeterminism.Deterministic;
            }
            else if (normalized == "maybe")
            {
                function.Determinism = XsltFunctionDeterminism.Elidable;
            }
            else
            {
                throw CreateXsltFunctionError("The new-each-time attribute has an invalid value.", XsltErrorCodes.XTSE0020, function.Declaration, function.Module);
            }
        }

        ReadXsltFunctionOverrideFlags(function);
        ReadXsltFunctionStreamability(function);
    }

    internal void ReadXsltFunctionOverrideFlags(XsltFunctionPlan function)
    {
        string? current = Loader.FindXsltAttribute(function.Declaration, "override-extension-function", "");
        string? deprecated = Loader.FindXsltAttribute(function.Declaration, "override", "");
        bool currentValue = current == null || Loader.StaticProcessor.ParseXsltStaticBoolean(current, "override-extension-function", function.Declaration, function.Module);
        bool deprecatedValue = deprecated == null || Loader.StaticProcessor.ParseXsltStaticBoolean(deprecated, "override", function.Declaration, function.Module);
        if (current != null && deprecated != null && currentValue != deprecatedValue)
        {
            throw CreateXsltFunctionError("The override and override-extension-function attributes must agree.", XsltErrorCodes.XTSE0020, function.Declaration, function.Module);
        }

        function.HasOverrideExtensionFunction = current != null || deprecated != null;
        function.OverrideExtensionFunction = current == null ? deprecatedValue : currentValue;
    }

    internal void ReadXsltFunctionStreamability(XsltFunctionPlan function)
    {
        string? source = Loader.FindXsltAttribute(function.Declaration, "streamability", "");
        if (source == null)
        {
            return;
        }

        string normalized = source.Trim();
        bool standard = normalized == "unclassified" || normalized == "absorbing" || normalized == "inspection" || normalized == "filter" ||
            normalized == "shallow-descent" || normalized == "deep-descent" || normalized == "ascent";
        if (standard)
        {
            function.Streamability = normalized;
        }
        else
        {
            function.Streamability = CreateXsltFunctionExpandedName(Loader.StaticProcessor.ResolveXsltStaticVariableName(normalized, function.Declaration, function.Module));
        }

        if (function.Arity == 0 && function.Streamability != "unclassified")
        {
            throw CreateXsltFunctionError("A zero-argument stylesheet function must have unclassified streamability.", XsltErrorCodes.XTSE3155, function.Declaration, function.Module);
        }
    }

    internal void CreateXsltFunctionBinding(XsltFunctionPlan function)
    {
        XPathFunctionBinding binding = function.Binding;
        binding.Name = function.Name;
        binding.Arity = function.Arity;
        binding.ParameterTypes = new XdmAtomicType[function.Arity];
        binding.ParameterOccurrences = new XdmOccurrence[function.Arity];
        binding.ParameterTypeSyntax = function.ParameterTypes;
        int index = 0;
        while (index < function.Arity)
        {
            XPathTypeSyntax? type = function.ParameterTypes[index];
            binding.ParameterTypes[index] = type == null || type.AtomicType == null ? XdmAtomicType.AnyAtomicType : type.AtomicType.Type;
            binding.ParameterOccurrences[index] = new XsltVariableCompiler(Loader).ReadXsltVariableOccurrence(type);
            index = index + 1;
        }

        binding.ResultType = function.ReturnType == null || function.ReturnType.AtomicType == null ? XdmAtomicType.AnyAtomicType : function.ReturnType.AtomicType.Type;
        binding.ResultOccurrence = new XsltVariableCompiler(Loader).ReadXsltVariableOccurrence(function.ReturnType);
        binding.ResultTypeSyntax = function.ReturnType;
        binding.BindingKey = "xslt-function:" + function.ExpandedName + "#" + function.Arity;
        binding.IsDeterministic = function.Determinism == XsltFunctionDeterminism.Deterministic;
    }

    internal void ValidateXsltFunctionDuplicates(XsltStylesheetPlan stylesheet)
    {
        int index = 0;
        while (index < stylesheet.Functions.Length)
        {
            XsltFunctionPlan function = stylesheet.Functions[index];
            int maximum = function.ImportPrecedence;
            int candidateIndex = 0;
            while (candidateIndex < stylesheet.Functions.Length)
            {
                XsltFunctionPlan candidate = stylesheet.Functions[candidateIndex];
                if (candidate.ExpandedName == function.ExpandedName && candidate.Arity == function.Arity && candidate.ImportPrecedence > maximum)
                {
                    maximum = candidate.ImportPrecedence;
                }

                candidateIndex = candidateIndex + 1;
            }

            int count = 0;
            candidateIndex = 0;
            while (candidateIndex < stylesheet.Functions.Length)
            {
                XsltFunctionPlan candidate = stylesheet.Functions[candidateIndex];
                if (candidate.ExpandedName == function.ExpandedName && candidate.Arity == function.Arity)
                {
                    candidate.IsEffective = candidate.ImportPrecedence == maximum;
                    if (candidate.ImportPrecedence == maximum)
                    {
                        count = count + 1;
                    }
                }

                candidateIndex = candidateIndex + 1;
            }

            if (count > 1)
            {
                throw CreateXsltFunctionError("A package contains duplicate stylesheet functions at the same import precedence.", XsltErrorCodes.XTSE0770, function.Declaration, function.Module);
            }

            index = index + 1;
        }
    }

    internal void BindXsltFunctions(XsltStylesheetPlan stylesheet)
    {
        XsltPackagePlan? package = stylesheet.Package;
        if (package == null)
        {
            Loader.StylesheetFunctions = [];
            return;
        }

        int index = 0;
        while (index < stylesheet.Functions.Length)
        {
            XsltFunctionPlan function = stylesheet.Functions[index];
            XsltComponent? component = FindXsltFunctionComponent(package, function.Declaration);
            function.Component = component;
            if (component != null)
            {
                function.Visibility = component.Visibility;
                function.DeclaredVisibility = component.DeclaredVisibility;
                function.HasDeclaredVisibility = component.HasDeclaredVisibility;
                function.IsAbstract = component.DeclaredVisibility == XsltComponentVisibility.Abstract;
                function.IsOverride = component.IsOverride;
                component.Function = function;
            }
            else
            {
                function.IsEffective = false;
            }

            index = index + 1;
        }

        Loader.StylesheetFunctions = CreateXsltVisibleFunctionBindings(package, stylesheet);
    }

    internal XsltComponent? FindXsltFunctionComponent(XsltPackagePlan package, XmlNode declaration)
    {
        int index = 0;
        while (index < package.Components.Length)
        {
            XsltComponent component = package.Components[index];
            if (component.Kind == XsltComponentKind.Function && object.ReferenceEquals(component.Declaration, declaration) && component.Visibility != XsltComponentVisibility.Hidden)
            {
                return component;
            }

            index = index + 1;
        }

        return null;
    }

    internal XPathFunctionBinding[] CreateXsltVisibleFunctionBindings(XsltPackagePlan package, XsltStylesheetPlan stylesheet)
    {
        List<XPathFunctionBinding> result = new List<XPathFunctionBinding>();
        int index = 0;
        while (index < package.Components.Length)
        {
            XsltComponent component = package.Components[index];
            XsltFunctionPlan? function = component.Function;
            if (component.Kind == XsltComponentKind.Function && component.Visibility != XsltComponentVisibility.Hidden && component.Visibility != XsltComponentVisibility.Abstract && function != null)
            {
                AddXsltFunctionBinding(result, function.Binding);
                if (component.IsOverride)
                {
                    AddXsltFunctionBinding(result, CreateXsltOriginalFunctionBinding(function.Arity));
                }
            }

            index = index + 1;
        }

        index = 0;
        while (index < stylesheet.Functions.Length)
        {
            XsltFunctionPlan function = stylesheet.Functions[index];
            if (function.IsOverriddenByExtension)
            {
                AddXsltFunctionBinding(result, CreateXsltExtensionFunctionBinding(function));
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XPathFunctionBinding CreateXsltExtensionFunctionBinding(XsltFunctionPlan function)
    {
        XPathFunctionBinding result = new XPathFunctionBinding();
        XsltExtensionBinding? extension = FindXsltFunctionExtension(function);
        result.Name = function.Name;
        result.Arity = function.Arity;
        result.ParameterTypes = function.Binding.ParameterTypes;
        result.ParameterOccurrences = function.Binding.ParameterOccurrences;
        result.ParameterTypeSyntax = function.Binding.ParameterTypeSyntax;
        result.ResultType = function.Binding.ResultType;
        result.ResultOccurrence = function.Binding.ResultOccurrence;
        result.ResultTypeSyntax = function.Binding.ResultTypeSyntax;
        result.BindingKey = extension == null ? null : extension.BindingKey;
        result.Implementation = extension == null ? null : extension.FunctionImplementation;
        result.IsExtensionBinding = true;
        result.ExtensionPolicy = Loader.Options.ExtensionPolicy;
        result.IsDeterministic = false;
        return result;
    }

    internal void AddXsltFunctionBinding(List<XPathFunctionBinding> bindings, XPathFunctionBinding binding)
    {
        int index = 0;
        while (index < bindings.Count)
        {
            XPathFunctionBinding candidate = bindings[index];
            if (SameXsltFunctionName(candidate.Name, binding.Name) && candidate.Arity == binding.Arity)
            {
                return;
            }

            index = index + 1;
        }

        bindings.Add(binding);
    }

    internal XPathFunctionBinding CreateXsltOriginalFunctionBinding(int arity)
    {
        XPathFunctionBinding result = new XPathFunctionBinding();
        XmlName name = new XmlName();
        name.Prefix = "xsl";
        name.LocalName = "original";
        name.NamespaceUri = XsltNamespaces.Xslt;
        name.QualifiedName = "xsl:original";
        result.Name = name;
        result.Arity = arity;
        result.ParameterTypes = new XdmAtomicType[arity];
        result.ParameterOccurrences = new XdmOccurrence[arity];
        int index = 0;
        while (index < arity)
        {
            result.ParameterTypes[index] = XdmAtomicType.AnyAtomicType;
            result.ParameterOccurrences[index] = XdmOccurrence.ZeroOrMore;
            index = index + 1;
        }

        result.ResultType = XdmAtomicType.AnyAtomicType;
        result.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        result.BindingKey = "xslt-original-function:" + arity;
        return result;
    }

    internal bool SameXsltFunctionName(XmlName? left, XmlName? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return left.LocalName == right.LocalName && (left.NamespaceUri == null ? "" : left.NamespaceUri) == (right.NamespaceUri == null ? "" : right.NamespaceUri);
    }

    internal void CompileXsltFunctionBodies(XsltStylesheetPlan stylesheet)
    {
        int index = 0;
        while (index < stylesheet.Functions.Length)
        {
            CompileXsltFunctionBody(stylesheet.Functions[index], stylesheet);
            index = index + 1;
        }

        CompleteXsltFunctionReferences(stylesheet);
    }

    internal void CompileXsltFunctionBody(XsltFunctionPlan function, XsltStylesheetPlan stylesheet)
    {
        List<XsltVariablePlan> parameters = new List<XsltVariablePlan>();
        List<XmlNode> body = new List<XmlNode>();
        XmlNode[] children = function.Declaration.Children == null ? [] : function.Declaration.Children;
        bool sawBody = false;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            bool whitespace = (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value);
            XsltVariablePlan? parameter = new XsltVariableCompiler(Loader).FindXsltVariableByNode(stylesheet, child);
            if (parameter != null && parameter.Scope == XsltVariableScope.FunctionParameter)
            {
                if (sawBody)
                {
                    throw CreateXsltFunctionError("Function parameters must precede the sequence constructor.", XsltErrorCodes.XTSE0010, child, function.Module);
                }

                parameters.Add(parameter);
            }
            else if (!whitespace && child.Kind != XmlNodeKind.Comment && child.Kind != XmlNodeKind.ProcessingInstruction)
            {
                sawBody = true;
                body.Add(child);
            }

            index = index + 1;
        }

        function.Parameters = parameters.ToArray();
        function.Body = new XsltTemplateCompiler(Loader).CompileXsltSequenceConstructor(function.Declaration, body.ToArray(), stylesheet, false);
        if (function.IsAbstract && function.Body.Items.Length != 0)
        {
            throw CreateXsltFunctionError("An abstract stylesheet function must have an empty body.", XsltErrorCodes.XTSE0020, function.Declaration, function.Module);
        }

        List<XsltFunctionExpressionPlan> expressions = new List<XsltFunctionExpressionPlan>();
        index = 0;
        while (index < body.Count)
        {
            CollectXsltFunctionExpressions(body[index], function, stylesheet, expressions);
            index = index + 1;
        }

        CollectXsltFunctionValueTemplateExpressions(function, stylesheet, expressions);
        function.Expressions = expressions.ToArray();
        function.References = CollectXsltFunctionReferences(function, stylesheet);
    }

    internal void CollectXsltFunctionExpressions(XmlNode node, XsltFunctionPlan function, XsltStylesheetPlan stylesheet, List<XsltFunctionExpressionPlan> expressions)
    {
        if (node.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(node) == XsltNamespaces.Xslt)
        {
            XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
            int attributeIndex = 0;
            while (attributeIndex < attributes.Length)
            {
                XmlNode attribute = attributes[attributeIndex];
                string localName = Loader.XsltNodeLocalName(attribute);
                if (IsXsltFunctionExpressionAttribute(localName, Loader.XsltNodeNamespace(attribute)))
                {
                    string source = attribute.Value == null ? "" : attribute.Value;
                    XPathPlan? plan = FindXsltFunctionExistingExpressionPlan(node, localName, stylesheet);
                    if (plan == null)
                    {
                        plan = CompileXsltFunctionExpression(source, node, function, stylesheet);
                    }

                    expressions.Add(new XsltFunctionExpressionPlan(attribute, localName, source, plan));
                }

                attributeIndex = attributeIndex + 1;
            }
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            CollectXsltFunctionExpressions(children[index], function, stylesheet, expressions);
            index = index + 1;
        }
    }

    internal bool IsXsltFunctionExpressionAttribute(string localName, string namespaceUri)
    {
        if (namespaceUri.Length != 0)
        {
            return false;
        }

        return localName == "select" || localName == "test" || localName == "use" || localName == "group-by" || localName == "group-adjacent" ||
            localName == "initial-value" || localName == "xpath" || localName == "context-item" || localName == "namespace-context" ||
            localName == "with-params" || localName == "dynamic-params";
    }

    internal XPathPlan? FindXsltFunctionExistingExpressionPlan(XmlNode node, string attributeName, XsltStylesheetPlan stylesheet)
    {
        if (attributeName != "select")
        {
            return null;
        }

        XsltVariablePlan? variable = new XsltVariableCompiler(Loader).FindXsltVariableByNode(stylesheet, node);
        return variable == null ? null : variable.SelectPlan;
    }

    internal XPathPlan CompileXsltFunctionExpression(string source, XmlNode node, XsltFunctionPlan function, XsltStylesheetPlan stylesheet)
    {
        XsltStaticContext? staticContext = FindXsltFunctionStaticContext(stylesheet, node);
        if (staticContext == null)
        {
            throw CreateXsltFunctionError("A stylesheet function expression has no static context.", XsltErrorCodes.XTSE0010, node, function.Module);
        }

        XPathStaticContext context = new XsltValueTemplateCompiler(Loader).CreateXsltValueTemplateXPathContext(staticContext, stylesheet, node);
        Task<XPathPlan> pending = Task.Run(() => new XPathPlanCompiler(context).CompileXPathSource(source, function.Module.BaseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltFunctionTaskError("A stylesheet function XPath expression could not be compiled.", node, function.Module, pending.Exception);
        }

        return pending.Result;
    }

    internal void CollectXsltFunctionValueTemplateExpressions(XsltFunctionPlan function, XsltStylesheetPlan stylesheet, List<XsltFunctionExpressionPlan> expressions)
    {
        int templateIndex = 0;
        while (templateIndex < stylesheet.ValueTemplates.Length)
        {
            XsltValueTemplate template = stylesheet.ValueTemplates[templateIndex];
            if (new XsltVariableCompiler(Loader).IsXsltDescendantNode(template.Node, function.Declaration))
            {
                int partIndex = 0;
                while (partIndex < template.Parts.Length)
                {
                    XsltValueTemplatePart part = template.Parts[partIndex];
                    if (part.ExpressionPlan != null)
                    {
                        expressions.Add(new XsltFunctionExpressionPlan(template.Node, "value-template", part.Value, part.ExpressionPlan));
                    }

                    partIndex = partIndex + 1;
                }
            }

            templateIndex = templateIndex + 1;
        }
    }

    internal XsltFunctionReferencePlan[] CollectXsltFunctionReferences(XsltFunctionPlan function, XsltStylesheetPlan stylesheet)
    {
        List<XsltFunctionReferencePlan> result = new List<XsltFunctionReferencePlan>();
        int index = 0;
        while (index < function.Expressions.Length)
        {
            CollectXsltFunctionInstructionReferences(function.Expressions[index].Plan.Root, function.Expressions[index].Node, function, stylesheet, result);
            index = index + 1;
        }

        return result.ToArray();
    }

    internal void CollectXsltFunctionInstructionReferences(XPathPlanInstruction instruction, XmlNode node, XsltFunctionPlan function, XsltStylesheetPlan stylesheet, List<XsltFunctionReferencePlan> references)
    {
        if ((instruction.Opcode == XPathPlanOpcode.StaticFunctionCall || instruction.Opcode == XPathPlanOpcode.NamedFunctionReference || instruction.Opcode == XPathPlanOpcode.Name) && instruction.FunctionBinding != null && instruction.ResolvedName != null &&
            IsXsltStylesheetFunctionBinding(instruction.FunctionBinding))
        {
            AddXsltFunctionReference(instruction, node, function, stylesheet, references);
        }

        CollectXsltOptionalFunctionInstruction(instruction.Left, node, function, stylesheet, references);
        CollectXsltOptionalFunctionInstruction(instruction.Right, node, function, stylesheet, references);
        CollectXsltOptionalFunctionInstruction(instruction.Operand, node, function, stylesheet, references);
        CollectXsltOptionalFunctionInstruction(instruction.Condition, node, function, stylesheet, references);
        CollectXsltOptionalFunctionInstruction(instruction.ThenInstruction, node, function, stylesheet, references);
        CollectXsltOptionalFunctionInstruction(instruction.ElseInstruction, node, function, stylesheet, references);
        CollectXsltOptionalFunctionInstruction(instruction.Body, node, function, stylesheet, references);
        int index = 0;
        while (index < instruction.Children.Length)
        {
            CollectXsltFunctionInstructionReferences(instruction.Children[index], node, function, stylesheet, references);
            index = index + 1;
        }

        index = 0;
        while (index < instruction.Bindings.Length)
        {
            CollectXsltFunctionInstructionReferences(instruction.Bindings[index].Expression, node, function, stylesheet, references);
            index = index + 1;
        }

        index = 0;
        while (index < instruction.MapEntries.Length)
        {
            CollectXsltFunctionInstructionReferences(instruction.MapEntries[index].Key, node, function, stylesheet, references);
            CollectXsltFunctionInstructionReferences(instruction.MapEntries[index].Value, node, function, stylesheet, references);
            index = index + 1;
        }
    }

    internal bool IsXsltStylesheetFunctionBinding(XPathFunctionBinding binding)
    {
        string? key = binding.BindingKey;
        return key != null && (key.StartsWith("xslt-function:", StringComparison.Ordinal) || key.StartsWith("xslt-original-function:", StringComparison.Ordinal));
    }

    internal void CollectXsltOptionalFunctionInstruction(XPathPlanInstruction? instruction, XmlNode node, XsltFunctionPlan function, XsltStylesheetPlan stylesheet, List<XsltFunctionReferencePlan> references)
    {
        if (instruction != null)
        {
            CollectXsltFunctionInstructionReferences(instruction, node, function, stylesheet, references);
        }
    }

    internal void AddXsltFunctionReference(XPathPlanInstruction instruction, XmlNode node, XsltFunctionPlan function, XsltStylesheetPlan stylesheet, List<XsltFunctionReferencePlan> references)
    {
        XmlName? optionalName = instruction.ResolvedName;
        XPathFunctionBinding? optionalBinding = instruction.FunctionBinding;
        if (optionalName == null || optionalBinding == null)
        {
            return;
        }

        string expandedName = CreateXsltFunctionExpandedName(optionalName);
        int arity = ReadXsltFunctionInstructionArity(instruction);
        XsltFunctionReferencePlan reference = new XsltFunctionReferencePlan(optionalName, expandedName, arity, node, instruction.Range, optionalBinding);
        reference.IsOriginal = optionalName.NamespaceUri == XsltNamespaces.Xslt && optionalName.LocalName == "original";
        reference.TargetComponent = reference.IsOriginal && function.Component != null ? function.Component.OriginalComponent : FindXsltReferencedFunctionComponent(stylesheet.Package, expandedName, arity);
        reference.TargetFunction = reference.TargetComponent == null ? null : reference.TargetComponent.Function;
        reference.IsRecursive = object.ReferenceEquals(reference.TargetFunction, function);
        references.Add(reference);
        AddXsltFunctionComponentReference(function.Component, reference);
    }

    internal int ReadXsltFunctionInstructionArity(XPathPlanInstruction instruction)
    {
        if (instruction.Opcode == XPathPlanOpcode.StaticFunctionCall)
        {
            return instruction.Children.Length;
        }

        return instruction.Opcode == XPathPlanOpcode.Name && instruction.FunctionBinding != null ? instruction.FunctionBinding.Arity : instruction.Arity;
    }

    internal XsltComponent? FindXsltReferencedFunctionComponent(XsltPackagePlan? package, string name, int arity)
    {
        if (package == null)
        {
            return null;
        }

        int index = package.Components.Length - 1;
        while (index >= 0)
        {
            XsltComponent component = package.Components[index];
            if (component.Kind == XsltComponentKind.Function && component.Visibility != XsltComponentVisibility.Hidden && component.Name == name && component.Arity == arity)
            {
                return component;
            }

            index = index - 1;
        }

        return null;
    }

    internal void AddXsltFunctionComponentReference(XsltComponent? owner, XsltFunctionReferencePlan reference)
    {
        if (owner == null)
        {
            return;
        }

        int index = 0;
        while (index < owner.References.Length)
        {
            XsltSymbolicReference existing = owner.References[index];
            if (existing.Kind == XsltComponentKind.Function && existing.Name == reference.ExpandedName && existing.Arity == reference.Arity)
            {
                return;
            }

            index = index + 1;
        }

        XsltSymbolicReference[] result = new XsltSymbolicReference[owner.References.Length + 1];
        index = 0;
        while (index < owner.References.Length)
        {
            result[index] = owner.References[index];
            index = index + 1;
        }

        XsltSymbolicReference symbolic = new XsltSymbolicReference(XsltComponentKind.Function, reference.ExpandedName, reference.Arity, reference.Node);
        symbolic.Target = reference.TargetComponent;
        result[owner.References.Length] = symbolic;
        owner.References = result;
    }

    internal void CompleteXsltFunctionReferences(XsltStylesheetPlan stylesheet)
    {
        int variableIndex = 0;
        while (variableIndex < stylesheet.Variables.Length)
        {
            XsltVariablePlan variable = stylesheet.Variables[variableIndex];
            if (variable.SelectPlan != null)
            {
                AddXsltPlanFunctionComponentReferences(variable.SelectPlan, variable.Declaration, stylesheet);
            }

            variableIndex = variableIndex + 1;
        }

        int templateIndex = 0;
        while (templateIndex < stylesheet.ValueTemplates.Length)
        {
            XsltValueTemplate template = stylesheet.ValueTemplates[templateIndex];
            int partIndex = 0;
            while (partIndex < template.Parts.Length)
            {
                XPathPlan? plan = template.Parts[partIndex].ExpressionPlan;
                if (plan != null)
                {
                    AddXsltPlanFunctionComponentReferences(plan, template.Node, stylesheet);
                }

                partIndex = partIndex + 1;
            }

            templateIndex = templateIndex + 1;
        }
    }

    internal void AddXsltPlanFunctionComponentReferences(XPathPlan plan, XmlNode node, XsltStylesheetPlan stylesheet)
    {
        XsltComponent? owner = FindXsltOwningComponent(stylesheet.Package, node);
        AddXsltInstructionComponentReferences(plan.Root, node, owner, stylesheet.Package);
    }

    internal XsltComponent? FindXsltOwningComponent(XsltPackagePlan? package, XmlNode node)
    {
        if (package == null)
        {
            return null;
        }

        XmlNode? current = node.Kind == XmlNodeKind.Attribute ? node.Parent : node;
        while (current != null)
        {
            int index = 0;
            while (index < package.Components.Length)
            {
                XsltComponent component = package.Components[index];
                if (component.Visibility != XsltComponentVisibility.Hidden && object.ReferenceEquals(component.Declaration, current))
                {
                    return component;
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        return null;
    }

    internal void AddXsltInstructionComponentReferences(XPathPlanInstruction instruction, XmlNode node, XsltComponent? owner, XsltPackagePlan? package)
    {
        if ((instruction.Opcode == XPathPlanOpcode.StaticFunctionCall || instruction.Opcode == XPathPlanOpcode.NamedFunctionReference || instruction.Opcode == XPathPlanOpcode.Name) && instruction.FunctionBinding != null && instruction.ResolvedName != null &&
            IsXsltStylesheetFunctionBinding(instruction.FunctionBinding))
        {
            string name = CreateXsltFunctionExpandedName(instruction.ResolvedName);
            int arity = ReadXsltFunctionInstructionArity(instruction);
            XsltFunctionReferencePlan reference = new XsltFunctionReferencePlan(instruction.ResolvedName, name, arity, node, instruction.Range, instruction.FunctionBinding);
            reference.TargetComponent = name == "Q{" + XsltNamespaces.Xslt + "}original" && owner != null ? owner.OriginalComponent : FindXsltReferencedFunctionComponent(package, name, arity);
            AddXsltFunctionComponentReference(owner, reference);
        }

        AddXsltOptionalInstructionComponentReferences(instruction.Left, node, owner, package);
        AddXsltOptionalInstructionComponentReferences(instruction.Right, node, owner, package);
        AddXsltOptionalInstructionComponentReferences(instruction.Operand, node, owner, package);
        AddXsltOptionalInstructionComponentReferences(instruction.Condition, node, owner, package);
        AddXsltOptionalInstructionComponentReferences(instruction.ThenInstruction, node, owner, package);
        AddXsltOptionalInstructionComponentReferences(instruction.ElseInstruction, node, owner, package);
        AddXsltOptionalInstructionComponentReferences(instruction.Body, node, owner, package);
        int index = 0;
        while (index < instruction.Children.Length)
        {
            AddXsltInstructionComponentReferences(instruction.Children[index], node, owner, package);
            index = index + 1;
        }

        index = 0;
        while (index < instruction.Bindings.Length)
        {
            AddXsltInstructionComponentReferences(instruction.Bindings[index].Expression, node, owner, package);
            index = index + 1;
        }

        index = 0;
        while (index < instruction.MapEntries.Length)
        {
            AddXsltInstructionComponentReferences(instruction.MapEntries[index].Key, node, owner, package);
            AddXsltInstructionComponentReferences(instruction.MapEntries[index].Value, node, owner, package);
            index = index + 1;
        }
    }

    internal void AddXsltOptionalInstructionComponentReferences(XPathPlanInstruction? instruction, XmlNode node, XsltComponent? owner, XsltPackagePlan? package)
    {
        if (instruction != null)
        {
            AddXsltInstructionComponentReferences(instruction, node, owner, package);
        }
    }

    internal bool IsXsltFunctionInsideOverride(XmlNode node)
    {
        XmlNode? parent = node.Parent;
        return parent != null && Loader.XsltNodeNamespace(parent) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(parent) == "override";
    }

    internal XsltStaticContext? FindXsltFunctionStaticContext(XsltStylesheetPlan stylesheet, XmlNode node)
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

    internal XsltStaticException CreateXsltFunctionTaskError(string message, XmlNode node, XsltStylesheetModule module, AggregateException? failure)
    {
        return CreateXsltFunctionError(message, Loader.StaticProcessor.ReadXsltTaskErrorCode(failure), node, module);
    }

    internal XsltStaticException CreateXsltFunctionError(string message, string code, XmlNode node, XsltStylesheetModule module)
    {
        return new XsltStylesheetAssembler(Loader).CreateXsltAssemblyError(message, code, node, module);
    }
}
