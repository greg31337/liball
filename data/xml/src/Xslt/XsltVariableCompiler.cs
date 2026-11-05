using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XsltVariableCompiler
{
    internal XsltStylesheetLoader Loader;

    internal XsltVariableCompiler(XsltStylesheetLoader loader)
    {
        Loader = loader;
    }

    internal void CompileXsltVariables(XsltStylesheetPlan stylesheet)
    {
        List<XsltVariablePlan> variables = new List<XsltVariablePlan>();
        int moduleIndex = 0;
        while (moduleIndex < stylesheet.Modules.Length)
        {
            XsltStylesheetModule module = stylesheet.Modules[moduleIndex];
            CollectXsltVariables(module.Root, module, stylesheet, variables);
            moduleIndex = moduleIndex + 1;
        }

        stylesheet.Variables = variables.ToArray();
        ValidateXsltGlobalVariables(stylesheet);
        ValidateXsltLocalVariableNames(stylesheet);
        List<XsltVariablePlan> globals = CreateXsltEffectiveGlobalVariables(stylesheet);
        int index = 0;
        while (index < stylesheet.Variables.Length)
        {
            XsltVariablePlan variable = stylesheet.Variables[index];
            if (variable.IsGlobal)
            {
                CompileXsltVariableInitializer(variable, globals);
                CompileXsltLocalVariableTree(variable.Declaration, stylesheet, CopyXsltVariablePlans(globals));
            }

            index = index + 1;
        }

        moduleIndex = 0;
        while (moduleIndex < stylesheet.Modules.Length)
        {
            XsltStylesheetModule module = stylesheet.Modules[moduleIndex];
            XmlNode[] children = module.Root.Children == null ? [] : module.Root.Children;
            index = 0;
            while (index < children.Length)
            {
                XmlNode child = children[index];
                XsltVariablePlan? variable = FindXsltVariableByNode(stylesheet, child);
                if (child.Kind == XmlNodeKind.Element && (variable == null || !variable.IsGlobal))
                {
                    CompileXsltLocalVariableTree(child, stylesheet, CopyXsltVariablePlans(globals));
                }

                index = index + 1;
            }

            moduleIndex = moduleIndex + 1;
        }

        ValidateXsltVariableCycles(stylesheet);
    }

    internal void CollectXsltVariables(XmlNode node, XsltStylesheetModule module, XsltStylesheetPlan stylesheet, List<XsltVariablePlan> variables)
    {
        if (node.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(node) == XsltNamespaces.Xslt)
        {
            string localName = Loader.XsltNodeLocalName(node);
            if (localName == "variable" || localName == "param")
            {
                variables.Add(CreateXsltVariable(node, module, stylesheet, localName == "param"));
            }
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.Element)
            {
                CollectXsltVariables(children[index], module, stylesheet, variables);
            }

            index = index + 1;
        }
    }

    internal XsltVariablePlan CreateXsltVariable(XmlNode node, XsltStylesheetModule module, XsltStylesheetPlan stylesheet, bool parameter)
    {
        XsltStaticContext? context = FindXsltVariableStaticContext(stylesheet, node);
        if (context == null)
        {
            throw CreateXsltVariableError("A variable declaration has no static context.", XsltErrorCodes.XTSE0010, node, module);
        }

        string? lexicalName = Loader.FindXsltAttribute(node, "name", "");
        if (lexicalName == null || lexicalName.Trim().Length == 0)
        {
            throw CreateXsltVariableError("An xsl:variable or xsl:param declaration requires a name.", XsltErrorCodes.XTSE0010, node, module);
        }

        XmlName name = Loader.StaticProcessor.ResolveXsltStaticVariableName(lexicalName, node, module);
        string expandedName = "Q{" + (name.NamespaceUri == null ? "" : name.NamespaceUri) + "}" + (name.LocalName == null ? "" : name.LocalName);
        XsltVariablePlan result = new XsltVariablePlan(node, module, context, name, expandedName);
        result.IsParameter = parameter;
        result.IsGlobal = IsXsltGlobalVariable(node, module);
        result.Scope = ClassifyXsltVariableScope(node, module, parameter, result.IsGlobal);
        result.ImportPrecedence = ReadXsltVariableImportPrecedence(stylesheet, node);
        result.DeclarationOrder = ReadXsltVariableDeclarationOrder(stylesheet, node);
        ReadXsltVariableFlags(result);
        ReadXsltVariableSequenceType(result);
        ReadXsltVariableValue(result);
        ApplyXsltImplicitRequiredParameter(result);
        CreateXsltVariableBinding(result);
        return result;
    }

    internal XsltVariableScope ClassifyXsltVariableScope(XmlNode node, XsltStylesheetModule module, bool parameter, bool global)
    {
        if (global)
        {
            return XsltVariableScope.Global;
        }

        if (!parameter)
        {
            return XsltVariableScope.Local;
        }

        XmlNode? parent = node.Parent;
        string parentNamespace = parent == null ? "" : Loader.XsltNodeNamespace(parent);
        string parentName = parent == null ? "" : Loader.XsltNodeLocalName(parent);
        if (parentNamespace == XsltNamespaces.Xslt && parentName == "template")
        {
            return XsltVariableScope.TemplateParameter;
        }

        if (parentNamespace == XsltNamespaces.Xslt && parentName == "function")
        {
            return XsltVariableScope.FunctionParameter;
        }

        if (parentNamespace == XsltNamespaces.Xslt && parentName == "iterate")
        {
            return XsltVariableScope.IterationParameter;
        }

        throw CreateXsltVariableError("An xsl:param declaration is not permitted in this position.", XsltErrorCodes.XTSE0010, node, module);
    }

    internal void ReadXsltVariableFlags(XsltVariablePlan variable)
    {
        string? required = Loader.FindXsltAttribute(variable.Declaration, "required", "");
        string? tunnel = Loader.FindXsltAttribute(variable.Declaration, "tunnel", "");
        string? staticValue = Loader.FindXsltAttribute(variable.Declaration, "static", "");
        if (!variable.IsParameter && (required != null || tunnel != null))
        {
            throw CreateXsltVariableError("The required and tunnel attributes are permitted only on xsl:param.", XsltErrorCodes.XTSE0010, variable.Declaration, variable.Module);
        }

        variable.IsExplicitlyRequired = required != null && Loader.StaticProcessor.ParseXsltStaticBoolean(required, "required", variable.Declaration, variable.Module);
        variable.IsRequired = variable.IsExplicitlyRequired;
        variable.IsTunnel = tunnel != null && Loader.StaticProcessor.ParseXsltStaticBoolean(tunnel, "tunnel", variable.Declaration, variable.Module);
        variable.IsStatic = staticValue != null && Loader.StaticProcessor.ParseXsltStaticBoolean(staticValue, "static", variable.Declaration, variable.Module);
        if (variable.IsTunnel && variable.Scope != XsltVariableScope.TemplateParameter)
        {
            throw CreateXsltVariableError("Only a template parameter may be a tunnel parameter.", XsltErrorCodes.XTSE0020, variable.Declaration, variable.Module);
        }

        if (variable.Scope == XsltVariableScope.FunctionParameter)
        {
            variable.IsRequired = true;
        }

        if (variable.Scope == XsltVariableScope.IterationParameter && variable.IsRequired)
        {
            throw CreateXsltVariableError("An iteration parameter must have an initial value.", XsltErrorCodes.XTSE0020, variable.Declaration, variable.Module);
        }
    }

    internal void ReadXsltVariableValue(XsltVariablePlan variable)
    {
        string? select = Loader.FindXsltAttribute(variable.Declaration, "select", "");
        bool content = HasXsltVariableContent(variable.Declaration);
        if (select != null && content)
        {
            throw CreateXsltVariableError("A variable or parameter with a select attribute must have an empty sequence constructor.", XsltErrorCodes.XTSE0620, variable.Declaration, variable.Module);
        }

        if (variable.Scope == XsltVariableScope.FunctionParameter && (select != null || content))
        {
            throw CreateXsltVariableError("A stylesheet function parameter must not specify a default value.", XsltErrorCodes.XTSE0760, variable.Declaration, variable.Module);
        }

        if (variable.IsRequired && (select != null || content))
        {
            throw CreateXsltVariableError("A required parameter must not specify a default value.", XsltErrorCodes.XTSE0010, variable.Declaration, variable.Module);
        }

        variable.Select = select;
        variable.Content = variable.Declaration.Children == null ? [] : variable.Declaration.Children;
        if (variable.IsStatic)
        {
            variable.ValueKind = XsltVariableValueKind.StaticValue;
        }
        else if (select != null)
        {
            variable.ValueKind = XsltVariableValueKind.Select;
        }
        else if (content)
        {
            variable.ValueKind = XsltVariableValueKind.Content;
        }
        else if (variable.IsRequired || variable.Scope == XsltVariableScope.FunctionParameter)
        {
            variable.ValueKind = XsltVariableValueKind.Required;
        }
        else if (Loader.FindXsltAttribute(variable.Declaration, "as", "") != null)
        {
            variable.ValueKind = XsltVariableValueKind.EmptySequence;
        }
        else
        {
            variable.ValueKind = XsltVariableValueKind.EmptyString;
        }
    }

    internal bool HasXsltVariableContent(XmlNode node)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element || (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && !new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ReadXsltVariableSequenceType(XsltVariablePlan variable)
    {
        string? source = Loader.FindXsltAttribute(variable.Declaration, "as", "");
        if (source == null)
        {
            return;
        }

        variable.SequenceTypeSource = source;
        XPathStaticContext context = CreateXsltVariableXPathContext(variable.StaticContext, []);
        Task<XPathTypeSyntax> pending = Task.Run(() => ParseXsltVariableSequenceType(source, context, variable.Module.BaseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltVariableTaskError("A variable or parameter has an invalid sequence type.", variable.Declaration, variable.Module, pending.Exception);
        }

        variable.SequenceType = pending.Result;
    }

    internal void ApplyXsltImplicitRequiredParameter(XsltVariablePlan variable)
    {
        if (!variable.IsParameter || variable.IsRequired || variable.Scope == XsltVariableScope.IterationParameter || variable.Select != null || HasXsltVariableContent(variable.Declaration))
        {
            return;
        }

        XPathTypeSyntax? type = variable.SequenceType;
        if (type != null && !type.EmptySequence && type.Occurrence != "?" && type.Occurrence != "*")
        {
            variable.IsRequired = true;
            variable.ValueKind = XsltVariableValueKind.Required;
        }
    }

    internal XPathTypeSyntax ParseXsltVariableSequenceType(string source, XPathStaticContext context, string? sourceUri)
    {
        XPathParser parser = new XPathParser(source, sourceUri);
        XPathTypeSyntax result = parser.ParseXPathSequenceType();
        XPathToken remaining = parser.CurrentXPathToken();
        if (remaining.Kind != XPathTokenKind.EndOfInput)
        {
            throw parser.CreateXPathParserError("The sequence type contains an unexpected terminal.", remaining);
        }

        XPathStaticAnalyzer analyzer = new XPathStaticAnalyzer(context);
        analyzer.Environment.ResolveXPathType(result);
        analyzer.ValidateXPathTypeSyntax(result, XPathErrorCodes.XPST0051, result.Range);
        return result;
    }

    internal void CreateXsltVariableBinding(XsltVariablePlan variable)
    {
        XPathVariableBinding binding = variable.Binding;
        binding.Name = variable.Name;
        binding.Value = ReadXsltStaticVariableValue(variable);
        binding.DeclaredType = variable.SequenceType == null || variable.SequenceType.AtomicType == null ? XdmAtomicType.AnyAtomicType : variable.SequenceType.AtomicType.Type;
        binding.DeclaredOccurrence = ReadXsltVariableOccurrence(variable.SequenceType);
        binding.IsExternal = variable.IsGlobal && variable.IsParameter && !variable.IsStatic;
        binding.IsRequired = variable.IsRequired;
        binding.StaticTypeSyntax = variable.SequenceType;
    }

    internal XdmSequence? ReadXsltStaticVariableValue(XsltVariablePlan variable)
    {
        int index = 0;
        while (index < Loader.StaticProcessor.StaticVariables.Count)
        {
            XsltStaticVariable candidate = Loader.StaticProcessor.StaticVariables[index];
            if (object.ReferenceEquals(candidate.Declaration, variable.Declaration))
            {
                return candidate.Value;
            }

            index = index + 1;
        }

        return null;
    }

    internal XdmOccurrence ReadXsltVariableOccurrence(XPathTypeSyntax? type)
    {
        if (type == null || type.EmptySequence || type.Occurrence == "*")
        {
            return XdmOccurrence.ZeroOrMore;
        }

        if (type.Occurrence == "?")
        {
            return XdmOccurrence.ZeroOrOne;
        }

        if (type.Occurrence == "+")
        {
            return XdmOccurrence.OneOrMore;
        }

        return XdmOccurrence.ExactlyOne;
    }

    internal void ValidateXsltGlobalVariables(XsltStylesheetPlan stylesheet)
    {
        int index = 0;
        while (index < stylesheet.Variables.Length)
        {
            XsltVariablePlan current = stylesheet.Variables[index];
            if (current.IsGlobal)
            {
                int highest = current.ImportPrecedence;
                int otherIndex = 0;
                while (otherIndex < stylesheet.Variables.Length)
                {
                    XsltVariablePlan other = stylesheet.Variables[otherIndex];
                    if (other.IsGlobal && other.ExpandedName == current.ExpandedName && other.ImportPrecedence > highest)
                    {
                        highest = other.ImportPrecedence;
                    }

                    otherIndex = otherIndex + 1;
                }

                current.IsEffective = current.ImportPrecedence == highest;
                if (current.IsEffective)
                {
                    otherIndex = 0;
                    while (otherIndex < index)
                    {
                        XsltVariablePlan previous = stylesheet.Variables[otherIndex];
                        if (previous.IsGlobal && previous.IsEffective && previous.ExpandedName == current.ExpandedName && previous.ImportPrecedence == current.ImportPrecedence)
                        {
                            throw CreateXsltVariableError("Global variables or parameters with the same name and import precedence are ambiguous.", XsltErrorCodes.XTSE0630, current.Declaration, current.Module);
                        }

                        otherIndex = otherIndex + 1;
                    }
                }
            }

            index = index + 1;
        }
    }

    internal void ValidateXsltLocalVariableNames(XsltStylesheetPlan stylesheet)
    {
        int moduleIndex = 0;
        while (moduleIndex < stylesheet.Modules.Length)
        {
            ValidateXsltLocalVariableNamesInTree(stylesheet.Modules[moduleIndex].Root, stylesheet);
            moduleIndex = moduleIndex + 1;
        }
    }

    internal void ValidateXsltLocalVariableNamesInTree(XmlNode node, XsltStylesheetPlan stylesheet)
    {
        List<XsltVariablePlan> siblings = new List<XsltVariablePlan>();
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            XsltVariablePlan? variable = FindXsltVariableByNode(stylesheet, child);
            if (variable != null && !variable.IsGlobal)
            {
                int siblingIndex = 0;
                while (siblingIndex < siblings.Count)
                {
                    if (siblings[siblingIndex].ExpandedName == variable.ExpandedName)
                    {
                        throw CreateXsltVariableError("Sibling local variables and parameters must have distinct names.", XsltErrorCodes.XTSE0580, variable.Declaration, variable.Module);
                    }

                    siblingIndex = siblingIndex + 1;
                }

                siblings.Add(variable);
            }

            if (child.Kind == XmlNodeKind.Element)
            {
                ValidateXsltLocalVariableNamesInTree(child, stylesheet);
            }

            index = index + 1;
        }
    }

    internal List<XsltVariablePlan> CreateXsltEffectiveGlobalVariables(XsltStylesheetPlan stylesheet)
    {
        List<XsltVariablePlan> result = new List<XsltVariablePlan>();
        int index = 0;
        while (index < stylesheet.Variables.Length)
        {
            XsltVariablePlan variable = stylesheet.Variables[index];
            if (variable.IsGlobal && variable.IsEffective)
            {
                SetXsltVisibleVariable(result, variable);
            }

            index = index + 1;
        }

        return result;
    }

    internal void CompileXsltLocalVariableTree(XmlNode node, XsltStylesheetPlan stylesheet, List<XsltVariablePlan> inherited)
    {
        List<XsltVariablePlan> visible = CopyXsltVariablePlans(inherited);
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element)
            {
                XsltVariablePlan? variable = FindXsltVariableByNode(stylesheet, child);
                if (variable != null && !variable.IsGlobal)
                {
                    CompileXsltVariableInitializer(variable, visible);
                    CompileXsltLocalVariableTree(child, stylesheet, CopyXsltVariablePlans(visible));
                    SetXsltVisibleVariable(visible, variable);
                }
                else
                {
                    CompileXsltLocalVariableTree(child, stylesheet, CopyXsltVariablePlans(visible));
                }
            }

            index = index + 1;
        }
    }

    internal void CompileXsltVariableInitializer(XsltVariablePlan variable, List<XsltVariablePlan> visible)
    {
        if (variable.Select == null)
        {
            return;
        }

        XPathStaticContext context = CreateXsltVariableXPathContext(variable.StaticContext, CreateXsltVariableBindingsAtNode(visible, variable.Declaration));
        Task<XPathPlan> pending = Task.Run(() => new XPathPlanCompiler(context).CompileXPathSource(variable.Select, variable.Module.BaseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltVariableTaskError("A variable or parameter select expression could not be compiled.", variable.Declaration, variable.Module, pending.Exception);
        }

        variable.SelectPlan = pending.Result;
        variable.Dependencies = CollectXsltVariableDependencies(variable.SelectPlan.Root, visible);
    }

    internal XsltVariablePlan[] CollectXsltVariableDependencies(XPathPlanInstruction root, List<XsltVariablePlan> visible)
    {
        List<XsltVariablePlan> result = new List<XsltVariablePlan>();
        CollectXsltVariableInstructionDependencies(root, visible, result);
        return result.ToArray();
    }

    internal void CollectXsltVariableInstructionDependencies(XPathPlanInstruction instruction, List<XsltVariablePlan> visible, List<XsltVariablePlan> result)
    {
        if (instruction.Opcode == XPathPlanOpcode.Variable && instruction.ResolvedName != null)
        {
            XsltVariablePlan? dependency = FindXsltVisibleVariable(visible, instruction.ResolvedName);
            if (dependency != null && !ContainsXsltVariablePlan(result, dependency))
            {
                result.Add(dependency);
            }
        }

        CollectXsltOptionalVariableInstruction(instruction.Left, visible, result);
        CollectXsltOptionalVariableInstruction(instruction.Right, visible, result);
        CollectXsltOptionalVariableInstruction(instruction.Operand, visible, result);
        CollectXsltOptionalVariableInstruction(instruction.Condition, visible, result);
        CollectXsltOptionalVariableInstruction(instruction.ThenInstruction, visible, result);
        CollectXsltOptionalVariableInstruction(instruction.ElseInstruction, visible, result);
        CollectXsltOptionalVariableInstruction(instruction.Body, visible, result);
        int index = 0;
        while (index < instruction.Children.Length)
        {
            CollectXsltVariableInstructionDependencies(instruction.Children[index], visible, result);
            index = index + 1;
        }

        index = 0;
        while (index < instruction.Bindings.Length)
        {
            CollectXsltVariableInstructionDependencies(instruction.Bindings[index].Expression, visible, result);
            index = index + 1;
        }

        index = 0;
        while (index < instruction.MapEntries.Length)
        {
            CollectXsltVariableInstructionDependencies(instruction.MapEntries[index].Key, visible, result);
            CollectXsltVariableInstructionDependencies(instruction.MapEntries[index].Value, visible, result);
            index = index + 1;
        }
    }

    internal void CollectXsltOptionalVariableInstruction(XPathPlanInstruction? instruction, List<XsltVariablePlan> visible, List<XsltVariablePlan> result)
    {
        if (instruction != null)
        {
            CollectXsltVariableInstructionDependencies(instruction, visible, result);
        }
    }

    internal void ValidateXsltVariableCycles(XsltStylesheetPlan stylesheet)
    {
        int[] states = new int[stylesheet.Variables.Length];
        int index = 0;
        while (index < stylesheet.Variables.Length)
        {
            XsltVariablePlan variable = stylesheet.Variables[index];
            if (variable.IsGlobal && variable.IsEffective)
            {
                VisitXsltVariableDependency(variable, stylesheet, states);
            }

            index = index + 1;
        }
    }

    internal void CompleteXsltVariableDependencies(XsltStylesheetPlan stylesheet)
    {
        int index = 0;
        while (index < stylesheet.Variables.Length)
        {
            XsltVariablePlan variable = stylesheet.Variables[index];
            if (variable.ValueKind == XsltVariableValueKind.Content)
            {
                XmlNode[] children = variable.Declaration.Children == null ? [] : variable.Declaration.Children;
                int childIndex = 0;
                while (childIndex < children.Length)
                {
                    CollectXsltVariableContentDependencies(children[childIndex], variable, stylesheet);
                    childIndex = childIndex + 1;
                }

                CollectXsltVariableTemplateDependencies(variable, stylesheet);
            }

            index = index + 1;
        }

        ValidateXsltVariableCycles(stylesheet);
    }

    internal void CollectXsltVariableContentDependencies(XmlNode node, XsltVariablePlan owner, XsltStylesheetPlan stylesheet)
    {
        if (node.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(node) == XsltNamespaces.Xslt)
        {
            XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
            int attributeIndex = 0;
            while (attributeIndex < attributes.Length)
            {
                XmlNode attribute = attributes[attributeIndex];
                if (IsXsltVariableDependencyExpressionAttribute(Loader.XsltNodeLocalName(attribute), Loader.XsltNodeNamespace(attribute)))
                {
                    string source = attribute.Value == null ? "" : attribute.Value;
                    CollectXsltLexicalVariableDependencies(source, node, owner, stylesheet);
                }

                attributeIndex = attributeIndex + 1;
            }
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            CollectXsltVariableContentDependencies(children[index], owner, stylesheet);
            index = index + 1;
        }
    }

    internal bool IsXsltVariableDependencyExpressionAttribute(string localName, string namespaceUri)
    {
        if (namespaceUri.Length != 0)
        {
            return false;
        }

        return localName == "select" || localName == "test" || localName == "use" || localName == "group-by" || localName == "group-adjacent" ||
            localName == "group-starting-with" || localName == "group-ending-with" || localName == "initial-value" || localName == "xpath" ||
            localName == "context-item" || localName == "namespace-context" || localName == "with-params" || localName == "dynamic-params";
    }

    internal void CollectXsltLexicalVariableDependencies(string source, XmlNode node, XsltVariablePlan owner, XsltStylesheetPlan stylesheet)
    {
        Task<XPathToken[]> pending = Task.Run(() => new XPathLexer(source, owner.Module.BaseUri).TokenizeXPath());
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            return;
        }

        List<XsltVariablePlan> visible = CreateXsltVisibleVariablePlans(stylesheet, node);
        XPathToken[] tokens = pending.Result;
        int index = 0;
        while (index + 1 < tokens.Length)
        {
            XPathToken token = tokens[index + 1];
            if (tokens[index].Kind == XPathTokenKind.Dollar && token.LocalName != null)
            {
                XmlName name = ResolveXsltDependencyTokenName(token, node);
                XsltVariablePlan? dependency = FindXsltVisibleVariable(visible, name);
                if (dependency != null)
                {
                    AddXsltVariableDependency(owner, dependency);
                }
            }

            index = index + 1;
        }
    }

    internal XmlName ResolveXsltDependencyTokenName(XPathToken token, XmlNode node)
    {
        XmlName result = new XmlName();
        result.Prefix = token.Prefix;
        result.LocalName = token.LocalName;
        result.QualifiedName = token.Lexeme;
        if (token.NamespaceUri != null)
        {
            result.NamespaceUri = token.NamespaceUri;
            return result;
        }

        if (token.Prefix == null)
        {
            result.NamespaceUri = "";
            return result;
        }

        XmlNamespaceBinding[] namespaces = Loader.StaticProcessor.BuildXsltStaticNamespaceContext(node).EnumerateInScopeXmlNamespaces();
        int index = 0;
        while (index < namespaces.Length)
        {
            if (namespaces[index].Prefix == token.Prefix)
            {
                result.NamespaceUri = namespaces[index].NamespaceUri;
                return result;
            }

            index = index + 1;
        }

        result.NamespaceUri = "";
        return result;
    }

    internal void CollectXsltVariableTemplateDependencies(XsltVariablePlan owner, XsltStylesheetPlan stylesheet)
    {
        int templateIndex = 0;
        while (templateIndex < stylesheet.ValueTemplates.Length)
        {
            XsltValueTemplate template = stylesheet.ValueTemplates[templateIndex];
            if (IsXsltDescendantNode(template.Node, owner.Declaration))
            {
                List<XsltVariablePlan> visible = CreateXsltVisibleVariablePlans(stylesheet, template.Node);
                int partIndex = 0;
                while (partIndex < template.Parts.Length)
                {
                    XPathPlan? plan = template.Parts[partIndex].ExpressionPlan;
                    if (plan != null)
                    {
                        XsltVariablePlan[] dependencies = CollectXsltVariableDependencies(plan.Root, visible);
                        int dependencyIndex = 0;
                        while (dependencyIndex < dependencies.Length)
                        {
                            AddXsltVariableDependency(owner, dependencies[dependencyIndex]);
                            dependencyIndex = dependencyIndex + 1;
                        }
                    }

                    partIndex = partIndex + 1;
                }
            }

            templateIndex = templateIndex + 1;
        }
    }

    internal bool IsXsltDescendantNode(XmlNode node, XmlNode ancestor)
    {
        XmlNode? current = node.Parent;
        while (current != null)
        {
            if (object.ReferenceEquals(current, ancestor))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    internal void AddXsltVariableDependency(XsltVariablePlan owner, XsltVariablePlan dependency)
    {
        List<XsltVariablePlan> result = new List<XsltVariablePlan>();
        int index = 0;
        while (index < owner.Dependencies.Length)
        {
            result.Add(owner.Dependencies[index]);
            index = index + 1;
        }

        if (!ContainsXsltVariablePlan(result, dependency))
        {
            result.Add(dependency);
        }

        owner.Dependencies = result.ToArray();
    }

    internal void VisitXsltVariableDependency(XsltVariablePlan variable, XsltStylesheetPlan stylesheet, int[] states)
    {
        int index = FindXsltVariableIndex(stylesheet, variable);
        if (index < 0 || states[index] == 2)
        {
            return;
        }

        if (states[index] == 1)
        {
            throw CreateXsltVariableError("A global variable or parameter has a circular dependency.", XsltErrorCodes.XTDE0640, variable.Declaration, variable.Module);
        }

        states[index] = 1;
        int dependencyIndex = 0;
        while (dependencyIndex < variable.Dependencies.Length)
        {
            XsltVariablePlan dependency = variable.Dependencies[dependencyIndex];
            VisitXsltVariableDependency(dependency, stylesheet, states);

            dependencyIndex = dependencyIndex + 1;
        }

        states[index] = 2;
    }

    internal XPathStaticContext CreateXsltVariableXPathContext(XsltStaticContext context, XPathVariableBinding[] variables)
    {
        XPathStaticContext result = new XPathStaticContext();
        result.LanguageVersion = XPathLanguageVersion.XPath31;
        result.CompatibilityMode = context.XPathCompatibilityMode;
        result.Namespaces = context.Namespaces;
        result.DefaultElementNamespace = context.XPathDefaultNamespace;
        result.DefaultFunctionNamespace = XPathNamespaces.Functions;
        result.BaseUri = context.BaseUri;
        result.DefaultCollation = context.DefaultCollation;
        result.Collations = [XPathNamespaces.CodepointCollation, XPathAsciiCaseInsensitiveCollation.Uri];
        result.CollationBindings = context.CollationBindings;
        result.Variables = variables;
        result.Functions = CreateXsltXPathFunctionBindings();
        result.FunctionResolver = new XsltStaticFunction(Loader, context.Namespaces, context.BaseUri);
        result.ExtensionPolicy = context.ExtensionPolicy;
        result.FunctionsArePrevalidated = true;
        result.SchemaSet = Loader.Options.SchemaSet;
        result.DecimalFormats = context.DecimalFormats;
        result.ResolverPolicy = Loader.Policy;
        result.SchemaAware = Loader.Options.ConformanceLevel == XsltConformanceLevel.SchemaAware;
        result.AllowHigherOrderFunctions = true;
        return result;
    }

    internal XPathFunctionBinding[] CreateXsltXPathFunctionBindings()
    {
        XPathFunctionBinding[] standard = Loader.StaticProcessor.CreateXsltStaticFunctionBindings();
        XPathFunctionBinding[] extensions = Loader.Options.Functions == null ? [] : Loader.Options.Functions;
        XPathFunctionBinding[] result = new XPathFunctionBinding[standard.Length + Loader.StylesheetFunctions.Length + extensions.Length + 4];
        int index = 0;
        while (index < standard.Length)
        {
            result[index] = standard[index];
            index = index + 1;
        }

        int functionIndex = 0;
        while (functionIndex < Loader.StylesheetFunctions.Length)
        {
            result[index] = Loader.StylesheetFunctions[functionIndex];
            index = index + 1;
            functionIndex = functionIndex + 1;
        }

        functionIndex = 0;
        while (functionIndex < extensions.Length)
        {
            result[index] = extensions[functionIndex];
            index = index + 1;
            functionIndex = functionIndex + 1;
        }

        XsltPatternCompiler patterns = new XsltPatternCompiler(Loader);
        result[index] = patterns.CreateXsltPatternKeyBinding(2);
        result[index + 1] = patterns.CreateXsltPatternKeyBinding(3);
        XsltAccumulatorCompiler accumulators = new XsltAccumulatorCompiler(Loader);
        result[index + 2] = accumulators.CreateXsltAccumulatorFunctionBinding("accumulator-before");
        result[index + 3] = accumulators.CreateXsltAccumulatorFunctionBinding("accumulator-after");

        return result;
    }

    internal List<XsltVariablePlan> CreateXsltVisibleVariablePlans(XsltStylesheetPlan stylesheet, XmlNode node)
    {
        List<XsltVariablePlan> result = CreateXsltEffectiveGlobalVariables(stylesheet);
        XmlNode? target = node.Kind == XmlNodeKind.Attribute || node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData ? node.Parent : node;
        if (target == null)
        {
            return result;
        }

        List<XmlNode> path = new List<XmlNode>();
        XmlNode? current = target;
        while (current != null && current.Kind != XmlNodeKind.Document)
        {
            path.Add(current);
            current = current.Parent;
        }

        int pathIndex = path.Count - 1;
        while (pathIndex > 0)
        {
            XmlNode parent = path[pathIndex];
            XmlNode childOnPath = path[pathIndex - 1];
            XmlNode[] children = parent.Children == null ? [] : parent.Children;
            int childIndex = 0;
            while (childIndex < children.Length && !object.ReferenceEquals(children[childIndex], childOnPath))
            {
                XsltVariablePlan? variable = FindXsltVariableByNode(stylesheet, children[childIndex]);
                if (variable != null && !variable.IsGlobal)
                {
                    SetXsltVisibleVariable(result, variable);
                }

                childIndex = childIndex + 1;
            }

            pathIndex = pathIndex - 1;
        }

        return result;
    }

    internal XPathVariableBinding[] CreateXsltVariableBindings(List<XsltVariablePlan> variables)
    {
        XPathVariableBinding[] result = new XPathVariableBinding[variables.Count];
        int index = 0;
        while (index < variables.Count)
        {
            result[index] = variables[index].Binding;
            index = index + 1;
        }

        return result;
    }

    internal XPathVariableBinding[] CreateXsltVariableBindingsAtNode(List<XsltVariablePlan> variables, XmlNode node)
    {
        XPathVariableBinding[] ordinary = CreateXsltVariableBindings(variables);
        if (!IsXsltInsideOverride(node))
        {
            return ordinary;
        }

        XPathVariableBinding[] result = new XPathVariableBinding[ordinary.Length + 1];
        int index = 0;
        while (index < ordinary.Length)
        {
            result[index] = ordinary[index];
            index = index + 1;
        }

        XPathVariableBinding original = new XPathVariableBinding();
        XmlName name = new XmlName();
        name.Prefix = "xsl";
        name.LocalName = "original";
        name.NamespaceUri = XsltNamespaces.Xslt;
        name.QualifiedName = "xsl:original";
        original.Name = name;
        original.DeclaredType = XdmAtomicType.AnyAtomicType;
        original.DeclaredOccurrence = XdmOccurrence.ZeroOrMore;
        result[ordinary.Length] = original;
        return result;
    }

    internal bool IsXsltInsideOverride(XmlNode node)
    {
        XmlNode? current = node.Parent;
        while (current != null)
        {
            if (Loader.XsltNodeNamespace(current) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(current) == "override")
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    internal bool IsXsltGlobalVariable(XmlNode node, XsltStylesheetModule module)
    {
        if (object.ReferenceEquals(node.Parent, module.Root))
        {
            return true;
        }

        XmlNode? parent = node.Parent;
        return parent != null && Loader.XsltNodeNamespace(parent) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(parent) == "override";
    }

    internal void SetXsltVisibleVariable(List<XsltVariablePlan> variables, XsltVariablePlan variable)
    {
        int index = 0;
        while (index < variables.Count)
        {
            if (variables[index].ExpandedName == variable.ExpandedName)
            {
                variables[index] = variable;
                return;
            }

            index = index + 1;
        }

        variables.Add(variable);
    }

    internal XsltVariablePlan? FindXsltVisibleVariable(List<XsltVariablePlan> variables, XmlName name)
    {
        int index = variables.Count - 1;
        while (index >= 0)
        {
            XmlName candidate = variables[index].Name;
            if (candidate.LocalName == name.LocalName && (candidate.NamespaceUri == null ? "" : candidate.NamespaceUri) == (name.NamespaceUri == null ? "" : name.NamespaceUri))
            {
                return variables[index];
            }

            index = index - 1;
        }

        return null;
    }

    internal XsltVariablePlan? FindXsltVariableByNode(XsltStylesheetPlan stylesheet, XmlNode node)
    {
        int index = 0;
        while (index < stylesheet.Variables.Length)
        {
            if (object.ReferenceEquals(stylesheet.Variables[index].Declaration, node))
            {
                return stylesheet.Variables[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltStaticContext? FindXsltVariableStaticContext(XsltStylesheetPlan stylesheet, XmlNode node)
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

    internal int ReadXsltVariableImportPrecedence(XsltStylesheetPlan stylesheet, XmlNode node)
    {
        XmlNode? current = node;
        while (current != null)
        {
            int index = 0;
            while (index < stylesheet.Declarations.Length)
            {
                if (object.ReferenceEquals(stylesheet.Declarations[index].Node, current))
                {
                    return stylesheet.Declarations[index].ImportPrecedence;
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        return -1;
    }

    internal int ReadXsltVariableDeclarationOrder(XsltStylesheetPlan stylesheet, XmlNode node)
    {
        XmlNode? current = node;
        while (current != null)
        {
            int index = 0;
            while (index < stylesheet.Declarations.Length)
            {
                if (object.ReferenceEquals(stylesheet.Declarations[index].Node, current))
                {
                    return stylesheet.Declarations[index].DeclarationOrder;
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        return -1;
    }

    internal int FindXsltVariableIndex(XsltStylesheetPlan stylesheet, XsltVariablePlan variable)
    {
        int index = 0;
        while (index < stylesheet.Variables.Length)
        {
            if (object.ReferenceEquals(stylesheet.Variables[index], variable))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool ContainsXsltVariablePlan(List<XsltVariablePlan> variables, XsltVariablePlan variable)
    {
        int index = 0;
        while (index < variables.Count)
        {
            if (object.ReferenceEquals(variables[index], variable))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal List<XsltVariablePlan> CopyXsltVariablePlans(List<XsltVariablePlan> source)
    {
        List<XsltVariablePlan> result = new List<XsltVariablePlan>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(source[index]);
            index = index + 1;
        }

        return result;
    }

    internal XsltStaticException CreateXsltVariableTaskError(string message, XmlNode node, XsltStylesheetModule module, AggregateException? failure)
    {
        return CreateXsltVariableError(message, Loader.StaticProcessor.ReadXsltTaskErrorCode(failure), node, module);
    }

    internal XsltStaticException CreateXsltVariableError(string message, string code, XmlNode node, XsltStylesheetModule module)
    {
        return new XsltStylesheetAssembler(Loader).CreateXsltAssemblyError(message, code, node, module);
    }
}
