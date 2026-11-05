using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XsltAccumulatorCompiler
{
    internal XsltStylesheetLoader Loader;

    internal XsltAccumulatorCompiler(XsltStylesheetLoader loader)
    {
        Loader = loader;
    }

    internal void CompileXsltAccumulators(XsltStylesheetPlan stylesheet)
    {
        List<XsltAccumulatorDeclarationPlan> declarations = new List<XsltAccumulatorDeclarationPlan>();
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            XsltStylesheetDeclaration declaration = stylesheet.Declarations[index];
            if (declaration.LocalName == "accumulator")
            {
                declarations.Add(CompileXsltAccumulatorDeclaration(declaration, stylesheet));
            }

            index = index + 1;
        }

        stylesheet.Accumulators = MergeXsltAccumulatorDeclarations(declarations).ToArray();
        LinkXsltAccumulatorDependencies(stylesheet);
        ValidateXsltAccumulatorCycles(stylesheet);
        BindXsltModeAccumulators(stylesheet);
    }

    internal XsltAccumulatorDeclarationPlan CompileXsltAccumulatorDeclaration(XsltStylesheetDeclaration declaration, XsltStylesheetPlan stylesheet)
    {
        XmlNode node = declaration.Node;
        XsltStaticContext? staticContext = declaration.StaticContext;
        if (staticContext == null)
        {
            staticContext = new XsltStaticContextCompiler(Loader).FindXsltStaticContext(stylesheet, node);
        }

        if (staticContext == null)
        {
            throw CreateXsltAccumulatorError("An xsl:accumulator declaration has no static context.", XsltErrorCodes.XTSE0010, node, declaration.Module);
        }

        string? lexicalName = Loader.FindXsltAttribute(node, "name", "");
        string? initialSource = Loader.FindXsltAttribute(node, "initial-value", "");
        if (lexicalName == null || lexicalName.Trim().Length == 0 || initialSource == null)
        {
            throw CreateXsltAccumulatorError("An xsl:accumulator declaration requires name and initial-value attributes.", XsltErrorCodes.XTSE0010, node, declaration.Module);
        }

        XmlName name = Loader.StaticProcessor.ResolveXsltStaticVariableName(lexicalName, node, declaration.Module);
        string accumulatorNamespace = name.NamespaceUri == null ? "" : name.NamespaceUri;
        if (new XsltFunctionCompiler(Loader).IsXsltFunctionReservedNamespace(accumulatorNamespace))
        {
            throw CreateXsltAccumulatorError("An accumulator name must not use a reserved namespace.", XsltErrorCodes.XTSE0080, node, declaration.Module);
        }

        string expandedName = CreateXsltAccumulatorExpandedName(name);
        XPathTypeSyntax sequenceType = ReadXsltAccumulatorSequenceType(node, staticContext, declaration.Module);
        XPathPlan initialValue = CompileXsltAccumulatorExpression(initialSource, staticContext, node, declaration.Module, stylesheet, null);
        XsltAccumulatorDeclarationPlan result = new XsltAccumulatorDeclarationPlan(node, declaration.Module, staticContext, name, expandedName, initialSource, initialValue, sequenceType);
        result.SequenceTypeSource = Loader.FindXsltAttribute(node, "as", "");
        result.ImportPrecedence = declaration.ImportPrecedence;
        result.DeclarationOrder = declaration.DeclarationOrder;
        string? streamable = Loader.FindXsltAttribute(node, "streamable", "");
        result.Streamable = streamable != null && Loader.StaticProcessor.ParseXsltStaticBoolean(streamable, "streamable", node, declaration.Module);
        result.InitialValueIsGrounded = IsXsltAccumulatorGrounded(initialValue.Root);
        result.InitialValueIsMotionless = IsXsltAccumulatorMotionless(initialValue.Root);
        List<XsltVariablePlan> visible = new XsltVariableCompiler(Loader).CreateXsltVisibleVariablePlans(stylesheet, node);
        result.Dependencies = new XsltVariableCompiler(Loader).CollectXsltVariableDependencies(initialValue.Root, visible);
        result.AccumulatorDependencies = CollectXsltAccumulatorReferences(initialValue.Root, node, declaration.Module);
        result.Rules = CompileXsltAccumulatorRules(result, stylesheet);
        CompleteXsltAccumulatorStreamability(result);
        return result;
    }

    internal string CreateXsltAccumulatorExpandedName(XmlName name)
    {
        string namespaceUri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        string localName = name.LocalName == null ? "" : name.LocalName;
        return "Q{" + namespaceUri + "}" + localName;
    }

    internal XPathTypeSyntax ReadXsltAccumulatorSequenceType(XmlNode node, XsltStaticContext staticContext, XsltStylesheetModule module)
    {
        string? source = Loader.FindXsltAttribute(node, "as", "");
        if (source == null)
        {
            SourceRange range = node.Range == null ? new SourceRange() : node.Range;
            return new XPathTypeSyntax("item", null, null, null, "*", false, range);
        }

        XPathStaticContext context = new XsltVariableCompiler(Loader).CreateXsltVariableXPathContext(staticContext, []);
        Task<XPathTypeSyntax> pending = Task.Run(() => new XsltVariableCompiler(Loader).ParseXsltVariableSequenceType(source, context, module.BaseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltAccumulatorTaskError("An xsl:accumulator declaration has an invalid sequence type.", node, module, pending.Exception);
        }

        return pending.Result;
    }

    internal XsltAccumulatorRulePlan[] CompileXsltAccumulatorRules(XsltAccumulatorDeclarationPlan accumulator, XsltStylesheetPlan stylesheet)
    {
        List<XsltAccumulatorRulePlan> result = new List<XsltAccumulatorRulePlan>();
        XmlNode[] children = accumulator.Declaration.Children == null ? [] : accumulator.Declaration.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            bool whitespace = (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value);
            if (child.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(child) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(child) == "accumulator-rule")
            {
                XsltAccumulatorRulePlan rule = CompileXsltAccumulatorRule(child, accumulator, stylesheet);
                rule.RuleOrder = result.Count;
                result.Add(rule);
            }
            else if (!whitespace && child.Kind != XmlNodeKind.Comment && child.Kind != XmlNodeKind.ProcessingInstruction)
            {
                throw CreateXsltAccumulatorError("An xsl:accumulator may contain only xsl:accumulator-rule elements.", XsltErrorCodes.XTSE0010, child, accumulator.Module);
            }

            index = index + 1;
        }

        if (result.Count == 0)
        {
            throw CreateXsltAccumulatorError("An xsl:accumulator requires at least one xsl:accumulator-rule.", XsltErrorCodes.XTSE0010, accumulator.Declaration, accumulator.Module);
        }

        return result.ToArray();
    }

    internal XsltAccumulatorRulePlan CompileXsltAccumulatorRule(XmlNode node, XsltAccumulatorDeclarationPlan accumulator, XsltStylesheetPlan stylesheet)
    {
        XsltStaticContext? staticContext = new XsltStaticContextCompiler(Loader).FindXsltStaticContext(stylesheet, node);
        if (staticContext == null)
        {
            throw CreateXsltAccumulatorError("An xsl:accumulator-rule has no static context.", XsltErrorCodes.XTSE0010, node, accumulator.Module);
        }

        string? match = Loader.FindXsltAttribute(node, "match", "");
        if (match == null || match.Trim().Length == 0)
        {
            throw CreateXsltAccumulatorError("An xsl:accumulator-rule requires a non-empty match pattern.", XsltErrorCodes.XTSE0010, node, accumulator.Module);
        }

        XsltAccumulatorRulePlan result = new XsltAccumulatorRulePlan(node, accumulator.Module, staticContext, match);
        string? phase = Loader.FindXsltAttribute(node, "phase", "");
        if (phase != null)
        {
            string normalized = phase.Trim();
            if (normalized == "end")
            {
                result.Phase = XsltAccumulatorPhase.End;
            }
            else if (normalized != "start")
            {
                throw CreateXsltAccumulatorError("The phase of an xsl:accumulator-rule must be start or end.", XsltErrorCodes.XTSE0020, node, accumulator.Module);
            }
        }

        CompileXsltAccumulatorPattern(result, stylesheet);
        CompileXsltAccumulatorRuleValue(result, accumulator, stylesheet);
        return result;
    }

    internal void CompileXsltAccumulatorPattern(XsltAccumulatorRulePlan rule, XsltStylesheetPlan stylesheet)
    {
        XPathStaticContext context = new XsltValueTemplateCompiler(Loader).CreateXsltValueTemplateXPathContext(rule.StaticContext, stylesheet, rule.Declaration);
        AddXsltAccumulatorFunctionBindings(context);
        XsltPatternCompiler compiler = new XsltPatternCompiler(Loader);
        compiler.AddXsltPatternKeyBindings(context);
        Task<bool> pending = Task.Run(() => compiler.CompileXsltPattern(rule.Match, context, rule.Module.BaseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltAccumulatorTaskError("An xsl:accumulator-rule match pattern could not be compiled.", rule.Declaration, rule.Module, pending.Exception);
        }

        List<XsltVariablePlan> visible = new XsltVariableCompiler(Loader).CreateXsltVisibleVariablePlans(stylesheet, rule.Declaration);
        XPathPlan? equivalent = rule.Match.EquivalentExpression;
        if (equivalent != null)
        {
            XsltVariablePlan[] variables = new XsltVariableCompiler(Loader).CollectXsltVariableDependencies(equivalent.Root, visible);
            rule.Dependencies = MergeXsltAccumulatorVariableDependencies(rule.Dependencies, variables);
            XsltAccumulatorReferencePlan[] references = CollectXsltAccumulatorReferences(equivalent.Root, rule.Declaration, rule.Module);
            rule.AccumulatorDependencies = MergeXsltAccumulatorReferenceDependencies(rule.AccumulatorDependencies, references);
        }

        int index = 0;
        while (index < rule.Match.Predicates.Length)
        {
            XPathPlan predicate = rule.Match.Predicates[index].Expression;
            XsltVariablePlan[] variables = new XsltVariableCompiler(Loader).CollectXsltVariableDependencies(predicate.Root, visible);
            rule.Dependencies = MergeXsltAccumulatorVariableDependencies(rule.Dependencies, variables);
            XsltAccumulatorReferencePlan[] references = CollectXsltAccumulatorReferences(predicate.Root, rule.Declaration, rule.Module);
            rule.AccumulatorDependencies = MergeXsltAccumulatorReferenceDependencies(rule.AccumulatorDependencies, references);
            index = index + 1;
        }
    }

    internal void CompileXsltAccumulatorRuleValue(XsltAccumulatorRulePlan rule, XsltAccumulatorDeclarationPlan accumulator, XsltStylesheetPlan stylesheet)
    {
        string? select = Loader.FindXsltAttribute(rule.Declaration, "select", "");
        List<XmlNode> content = ReadXsltAccumulatorRuleContent(rule.Declaration);
        if (select != null && content.Count != 0)
        {
            throw CreateXsltAccumulatorError("The select attribute and sequence constructor of xsl:accumulator-rule are mutually exclusive.", XsltErrorCodes.XTSE0010, rule.Declaration, rule.Module);
        }

        if (select == null)
        {
            rule.ValueKind = XsltAccumulatorValueKind.Content;
            rule.Content = new XsltTemplateCompiler(Loader).CompileXsltSequenceConstructor(rule.Declaration, content.ToArray(), stylesheet, false);
            rule.ValueIsGrounded = content.Count == 0;
            rule.ValueIsMotionless = content.Count == 0;
            return;
        }

        rule.ValueKind = XsltAccumulatorValueKind.Expression;
        rule.SelectSource = select;
        XPathVariableBinding value = CreateXsltAccumulatorValueBinding(accumulator.SequenceType);
        rule.Select = CompileXsltAccumulatorExpression(select, rule.StaticContext, rule.Declaration, rule.Module, stylesheet, value);
        rule.SelectType = rule.Select.Root.StaticType;
        rule.ValueIsGrounded = IsXsltAccumulatorGrounded(rule.Select.Root);
        rule.ValueIsMotionless = IsXsltAccumulatorMotionless(rule.Select.Root);
        List<XsltVariablePlan> visible = CreateXsltAccumulatorRuleVisibleVariables(stylesheet, rule.Declaration);
        XsltVariablePlan[] variables = new XsltVariableCompiler(Loader).CollectXsltVariableDependencies(rule.Select.Root, visible);
        rule.Dependencies = MergeXsltAccumulatorVariableDependencies(rule.Dependencies, variables);
        XsltAccumulatorReferencePlan[] references = CollectXsltAccumulatorReferences(rule.Select.Root, rule.Declaration, rule.Module);
        rule.AccumulatorDependencies = MergeXsltAccumulatorReferenceDependencies(rule.AccumulatorDependencies, references);
    }

    internal List<XmlNode> ReadXsltAccumulatorRuleContent(XmlNode node)
    {
        List<XmlNode> result = new List<XmlNode>();
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            bool whitespace = (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value);
            if (!whitespace && child.Kind != XmlNodeKind.Comment && child.Kind != XmlNodeKind.ProcessingInstruction)
            {
                result.Add(child);
            }

            index = index + 1;
        }

        return result;
    }

    internal XPathVariableBinding CreateXsltAccumulatorValueBinding(XPathTypeSyntax type)
    {
        XPathVariableBinding result = new XPathVariableBinding();
        result.Name = new XmlName { LocalName = "value", QualifiedName = "value", NamespaceUri = "" };
        result.DeclaredType = type.AtomicType == null ? XdmAtomicType.AnyAtomicType : type.AtomicType.Type;
        result.DeclaredOccurrence = new XsltVariableCompiler(Loader).ReadXsltVariableOccurrence(type);
        result.StaticTypeSyntax = type;
        return result;
    }

    internal List<XsltVariablePlan> CreateXsltAccumulatorRuleVisibleVariables(XsltStylesheetPlan stylesheet, XmlNode node)
    {
        List<XsltVariablePlan> source = new XsltVariableCompiler(Loader).CreateXsltVisibleVariablePlans(stylesheet, node);
        List<XsltVariablePlan> result = new List<XsltVariablePlan>();
        int index = 0;
        while (index < source.Count)
        {
            XmlName name = source[index].Name;
            string namespaceUri = name.NamespaceUri == null ? "" : name.NamespaceUri;
            if (name.LocalName != "value" || namespaceUri.Length != 0)
            {
                result.Add(source[index]);
            }

            index = index + 1;
        }

        return result;
    }

    internal XPathPlan CompileXsltAccumulatorExpression(string source, XsltStaticContext staticContext, XmlNode node, XsltStylesheetModule module, XsltStylesheetPlan stylesheet, XPathVariableBinding? extraVariable)
    {
        XPathStaticContext context = new XsltValueTemplateCompiler(Loader).CreateXsltValueTemplateXPathContext(staticContext, stylesheet, node);
        if (extraVariable != null)
        {
            XPathVariableBinding[] existing = context.Variables == null ? [] : context.Variables;
            int retained = 0;
            int index = 0;
            while (index < existing.Length)
            {
                XmlName? name = existing[index].Name;
                string namespaceUri = name == null || name.NamespaceUri == null ? "" : name.NamespaceUri;
                if (name == null || name.LocalName != "value" || namespaceUri.Length != 0)
                {
                    retained = retained + 1;
                }

                index = index + 1;
            }

            XPathVariableBinding[] combined = new XPathVariableBinding[retained + 1];
            index = 0;
            int targetIndex = 0;
            while (index < existing.Length)
            {
                XmlName? name = existing[index].Name;
                string namespaceUri = name == null || name.NamespaceUri == null ? "" : name.NamespaceUri;
                if (name == null || name.LocalName != "value" || namespaceUri.Length != 0)
                {
                    combined[targetIndex] = existing[index];
                    targetIndex = targetIndex + 1;
                }

                index = index + 1;
            }

            combined[retained] = extraVariable;
            context.Variables = combined;
        }

        AddXsltAccumulatorFunctionBindings(context);
        new XsltPatternCompiler(Loader).AddXsltPatternKeyBindings(context);
        Task<XPathPlan> pending = Task.Run(() => new XPathPlanCompiler(context).CompileXPathSource(source, module.BaseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltAccumulatorTaskError("An accumulator expression could not be compiled.", node, module, pending.Exception);
        }

        return pending.Result;
    }

    internal void AddXsltAccumulatorFunctionBindings(XPathStaticContext context)
    {
        XPathFunctionBinding[] existing = context.Functions == null ? [] : context.Functions;
        bool hasBefore = HasXsltAccumulatorFunctionBinding(existing, "accumulator-before");
        bool hasAfter = HasXsltAccumulatorFunctionBinding(existing, "accumulator-after");
        int additional = (hasBefore ? 0 : 1) + (hasAfter ? 0 : 1);
        if (additional == 0)
        {
            return;
        }

        XPathFunctionBinding[] result = new XPathFunctionBinding[existing.Length + additional];
        int index = 0;
        while (index < existing.Length)
        {
            result[index] = existing[index];
            index = index + 1;
        }

        if (!hasBefore)
        {
            result[index] = CreateXsltAccumulatorFunctionBinding("accumulator-before");
            index = index + 1;
        }

        if (!hasAfter)
        {
            result[index] = CreateXsltAccumulatorFunctionBinding("accumulator-after");
        }

        context.Functions = result;
    }

    internal bool HasXsltAccumulatorFunctionBinding(XPathFunctionBinding[] bindings, string localName)
    {
        int index = 0;
        while (index < bindings.Length)
        {
            XmlName? name = bindings[index].Name;
            if (name != null && name.NamespaceUri == XPathNamespaces.Functions && name.LocalName == localName && bindings[index].Arity == 1)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XPathFunctionBinding CreateXsltAccumulatorFunctionBinding(string localName)
    {
        XPathFunctionBinding result = new XPathFunctionBinding();
        result.Name = new XmlName { LocalName = localName, QualifiedName = localName, NamespaceUri = XPathNamespaces.Functions };
        result.Arity = 1;
        result.ParameterTypes = null;
        result.ParameterOccurrences = null;
        result.ResultType = XdmAtomicType.AnyAtomicType;
        result.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        result.BindingKey = "xslt:" + localName + "#1";
        result.IsDeterministic = true;
        SourceRange range = new SourceRange();
        result.ResultTypeSyntax = new XPathTypeSyntax("item", null, null, null, "*", false, range);
        return result;
    }

    internal XsltAccumulatorReferencePlan[] CollectXsltAccumulatorReferences(XPathPlanInstruction instruction, XmlNode node, XsltStylesheetModule module)
    {
        List<XsltAccumulatorReferencePlan> result = new List<XsltAccumulatorReferencePlan>();
        CollectXsltAccumulatorReferencesFromInstruction(instruction, node, module, result);
        return result.ToArray();
    }

    internal void CollectXsltAccumulatorReferencesFromInstruction(XPathPlanInstruction instruction, XmlNode node, XsltStylesheetModule module, List<XsltAccumulatorReferencePlan> references)
    {
        XPathFunctionBinding? binding = instruction.FunctionBinding;
        string bindingKey = binding == null || binding.BindingKey == null ? "" : binding.BindingKey;
        if ((bindingKey == "xslt:accumulator-before#1" || bindingKey == "xslt:accumulator-after#1") && instruction.Children.Length == 1)
        {
            string? lexicalName = ReadXsltAccumulatorLiteralName(instruction.Children[0]);
            if (lexicalName != null)
            {
                XmlName name = Loader.StaticProcessor.ResolveXsltStaticVariableName(lexicalName, node, module);
                string expandedName = CreateXsltAccumulatorExpandedName(name);
                XsltAccumulatorPhase phase = bindingKey == "xslt:accumulator-before#1" ? XsltAccumulatorPhase.Start : XsltAccumulatorPhase.End;
                if (!ContainsXsltAccumulatorReference(references, expandedName, phase))
                {
                    references.Add(new XsltAccumulatorReferencePlan(expandedName, phase));
                }
            }
        }

        CollectXsltAccumulatorOptionalReference(instruction.Left, node, module, references);
        CollectXsltAccumulatorOptionalReference(instruction.Right, node, module, references);
        CollectXsltAccumulatorOptionalReference(instruction.Operand, node, module, references);
        CollectXsltAccumulatorOptionalReference(instruction.Condition, node, module, references);
        CollectXsltAccumulatorOptionalReference(instruction.ThenInstruction, node, module, references);
        CollectXsltAccumulatorOptionalReference(instruction.ElseInstruction, node, module, references);
        CollectXsltAccumulatorOptionalReference(instruction.Body, node, module, references);
        int index = 0;
        while (index < instruction.Children.Length)
        {
            CollectXsltAccumulatorReferencesFromInstruction(instruction.Children[index], node, module, references);
            index = index + 1;
        }

        index = 0;
        while (index < instruction.Bindings.Length)
        {
            CollectXsltAccumulatorReferencesFromInstruction(instruction.Bindings[index].Expression, node, module, references);
            index = index + 1;
        }

        index = 0;
        while (index < instruction.MapEntries.Length)
        {
            CollectXsltAccumulatorReferencesFromInstruction(instruction.MapEntries[index].Key, node, module, references);
            CollectXsltAccumulatorReferencesFromInstruction(instruction.MapEntries[index].Value, node, module, references);
            index = index + 1;
        }
    }

    internal void CollectXsltAccumulatorOptionalReference(XPathPlanInstruction? instruction, XmlNode node, XsltStylesheetModule module, List<XsltAccumulatorReferencePlan> references)
    {
        if (instruction != null)
        {
            CollectXsltAccumulatorReferencesFromInstruction(instruction, node, module, references);
        }
    }

    internal string? ReadXsltAccumulatorLiteralName(XPathPlanInstruction instruction)
    {
        if (instruction.Constants.Length == 1 && instruction.Constants[0].Kind == XPathPlanLiteralKind.String)
        {
            return instruction.Constants[0].Value;
        }

        return null;
    }

    internal bool ContainsXsltAccumulatorReference(List<XsltAccumulatorReferencePlan> references, string name, XsltAccumulatorPhase phase)
    {
        int index = 0;
        while (index < references.Count)
        {
            if (references[index].Name == name && references[index].ValuePhase == phase)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XsltAccumulatorReferencePlan[] MergeXsltAccumulatorReferenceDependencies(XsltAccumulatorReferencePlan[] first, XsltAccumulatorReferencePlan[] second)
    {
        List<XsltAccumulatorReferencePlan> result = new List<XsltAccumulatorReferencePlan>();
        int index = 0;
        while (index < first.Length)
        {
            result.Add(first[index]);
            index = index + 1;
        }

        index = 0;
        while (index < second.Length)
        {
            XsltAccumulatorReferencePlan reference = second[index];
            if (!ContainsXsltAccumulatorReference(result, reference.Name, reference.ValuePhase))
            {
                result.Add(reference);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XsltVariablePlan[] MergeXsltAccumulatorVariableDependencies(XsltVariablePlan[] first, XsltVariablePlan[] second)
    {
        List<XsltVariablePlan> result = new List<XsltVariablePlan>();
        int index = 0;
        while (index < first.Length)
        {
            result.Add(first[index]);
            index = index + 1;
        }

        index = 0;
        while (index < second.Length)
        {
            if (!result.Contains(second[index]))
            {
                result.Add(second[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal bool IsXsltAccumulatorGrounded(XPathPlanInstruction instruction)
    {
        return instruction.StaticType.Cardinality == XPathStaticCardinality.Empty || instruction.StaticType.ItemKind == XPathStaticItemKind.Atomic ||
            instruction.StaticType.ItemKind == XPathStaticItemKind.Function || instruction.StaticType.ItemKind == XPathStaticItemKind.Map || instruction.StaticType.ItemKind == XPathStaticItemKind.Array;
    }

    internal bool IsXsltAccumulatorMotionless(XPathPlanInstruction instruction)
    {
        if (instruction.Opcode == XPathPlanOpcode.Path && (instruction.Absolute || instruction.LeadingDescendant))
        {
            return false;
        }

        if (instruction.Opcode == XPathPlanOpcode.AxisStep && instruction.Axis != XPathAxis.Attribute && instruction.Axis != XPathAxis.Self && instruction.Axis != XPathAxis.Parent && instruction.Axis != XPathAxis.Ancestor && instruction.Axis != XPathAxis.AncestorOrSelf)
        {
            return false;
        }

        if (!IsXsltAccumulatorOptionalMotionless(instruction.Left) || !IsXsltAccumulatorOptionalMotionless(instruction.Right) || !IsXsltAccumulatorOptionalMotionless(instruction.Operand) ||
            !IsXsltAccumulatorOptionalMotionless(instruction.Condition) || !IsXsltAccumulatorOptionalMotionless(instruction.ThenInstruction) || !IsXsltAccumulatorOptionalMotionless(instruction.ElseInstruction) || !IsXsltAccumulatorOptionalMotionless(instruction.Body))
        {
            return false;
        }

        int index = 0;
        while (index < instruction.Children.Length)
        {
            if (!IsXsltAccumulatorMotionless(instruction.Children[index]))
            {
                return false;
            }

            index = index + 1;
        }

        index = 0;
        while (index < instruction.Bindings.Length)
        {
            if (!IsXsltAccumulatorMotionless(instruction.Bindings[index].Expression))
            {
                return false;
            }

            index = index + 1;
        }

        index = 0;
        while (index < instruction.MapEntries.Length)
        {
            if (!IsXsltAccumulatorMotionless(instruction.MapEntries[index].Key) || !IsXsltAccumulatorMotionless(instruction.MapEntries[index].Value))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsXsltAccumulatorOptionalMotionless(XPathPlanInstruction? instruction)
    {
        return instruction == null || IsXsltAccumulatorMotionless(instruction);
    }

    internal void CompleteXsltAccumulatorStreamability(XsltAccumulatorDeclarationPlan accumulator)
    {
        bool candidate = accumulator.Streamable && accumulator.InitialValueIsGrounded && accumulator.InitialValueIsMotionless;
        int index = 0;
        while (index < accumulator.Rules.Length)
        {
            XsltAccumulatorRulePlan rule = accumulator.Rules[index];
            if (!rule.Match.IsMotionless || !rule.ValueIsGrounded || !rule.ValueIsMotionless)
            {
                candidate = false;
            }

            int dependencyIndex = 0;
            while (dependencyIndex < rule.AccumulatorDependencies.Length)
            {
                if (rule.Phase == XsltAccumulatorPhase.Start && rule.AccumulatorDependencies[dependencyIndex].ValuePhase == XsltAccumulatorPhase.End)
                {
                    candidate = false;
                }

                dependencyIndex = dependencyIndex + 1;
            }

            index = index + 1;
        }

        accumulator.IsGuaranteedStreamableCandidate = candidate;
    }

    internal List<XsltAccumulatorPlan> MergeXsltAccumulatorDeclarations(List<XsltAccumulatorDeclarationPlan> declarations)
    {
        List<XsltAccumulatorPlan> result = new List<XsltAccumulatorPlan>();
        int index = 0;
        while (index < declarations.Count)
        {
            XsltAccumulatorDeclarationPlan declaration = declarations[index];
            XsltAccumulatorPlan? accumulator = FindXsltAccumulator(result, declaration.Name);
            if (accumulator == null)
            {
                accumulator = new XsltAccumulatorPlan(declaration.ResolvedName, declaration.Name, declaration);
                result.Add(accumulator);
            }

            index = index + 1;
        }

        index = 0;
        while (index < result.Count)
        {
            CompleteXsltMergedAccumulator(result[index], declarations);
            index = index + 1;
        }

        return result;
    }

    internal void CompleteXsltMergedAccumulator(XsltAccumulatorPlan accumulator, List<XsltAccumulatorDeclarationPlan> declarations)
    {
        List<XsltAccumulatorDeclarationPlan> matching = new List<XsltAccumulatorDeclarationPlan>();
        int index = 0;
        while (index < declarations.Count)
        {
            if (declarations[index].Name == accumulator.Name)
            {
                InsertXsltAccumulatorDeclaration(matching, declarations[index]);
            }

            index = index + 1;
        }

        accumulator.Declarations = matching.ToArray();
        int highest = matching[matching.Count - 1].ImportPrecedence;
        int highestCount = 0;
        index = 0;
        while (index < matching.Count)
        {
            XsltAccumulatorDeclarationPlan declaration = matching[index];
            declaration.IsEffective = declaration.ImportPrecedence == highest;
            if (declaration.IsEffective)
            {
                accumulator.EffectiveDeclaration = declaration;
                highestCount = highestCount + 1;
            }

            index = index + 1;
        }

        if (highestCount > 1)
        {
            XsltAccumulatorDeclarationPlan declaration = accumulator.EffectiveDeclaration;
            throw CreateXsltAccumulatorError("Two effective accumulator declarations have the same name and import precedence.", XsltErrorCodes.XTSE3350, declaration.Declaration, declaration.Module);
        }
    }

    internal void InsertXsltAccumulatorDeclaration(List<XsltAccumulatorDeclarationPlan> declarations, XsltAccumulatorDeclarationPlan declaration)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            XsltAccumulatorDeclarationPlan current = declarations[index];
            if (declaration.ImportPrecedence < current.ImportPrecedence || declaration.ImportPrecedence == current.ImportPrecedence && declaration.DeclarationOrder < current.DeclarationOrder)
            {
                declarations.Insert(index, declaration);
                return;
            }

            index = index + 1;
        }

        declarations.Add(declaration);
    }

    internal XsltAccumulatorPlan? FindXsltAccumulator(List<XsltAccumulatorPlan> accumulators, string name)
    {
        int index = 0;
        while (index < accumulators.Count)
        {
            if (accumulators[index].Name == name)
            {
                return accumulators[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltAccumulatorPlan? FindXsltAccumulatorInArray(XsltAccumulatorPlan[] accumulators, string name)
    {
        int index = 0;
        while (index < accumulators.Length)
        {
            if (accumulators[index].Name == name)
            {
                return accumulators[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void LinkXsltAccumulatorDependencies(XsltStylesheetPlan stylesheet)
    {
        int index = 0;
        while (index < stylesheet.Accumulators.Length)
        {
            XsltAccumulatorPlan accumulator = stylesheet.Accumulators[index];
            XsltAccumulatorDeclarationPlan declaration = accumulator.EffectiveDeclaration;
            List<XsltAccumulatorPlan> dependencies = new List<XsltAccumulatorPlan>();
            LinkXsltAccumulatorReferences(declaration.AccumulatorDependencies, stylesheet.Accumulators, dependencies);
            int ruleIndex = 0;
            while (ruleIndex < declaration.Rules.Length)
            {
                LinkXsltAccumulatorReferences(declaration.Rules[ruleIndex].AccumulatorDependencies, stylesheet.Accumulators, dependencies);
                ruleIndex = ruleIndex + 1;
            }

            accumulator.Dependencies = dependencies.ToArray();
            index = index + 1;
        }
    }

    internal void LinkXsltAccumulatorReferences(XsltAccumulatorReferencePlan[] references, XsltAccumulatorPlan[] accumulators, List<XsltAccumulatorPlan> dependencies)
    {
        int index = 0;
        while (index < references.Length)
        {
            XsltAccumulatorPlan? target = FindXsltAccumulatorInArray(accumulators, references[index].Name);
            references[index].Target = target;
            if (target != null && !dependencies.Contains(target))
            {
                dependencies.Add(target);
            }

            index = index + 1;
        }
    }

    internal void ValidateXsltAccumulatorCycles(XsltStylesheetPlan stylesheet)
    {
        int[] states = new int[stylesheet.Accumulators.Length];
        int index = 0;
        while (index < stylesheet.Accumulators.Length)
        {
            VisitXsltAccumulatorDependency(stylesheet.Accumulators[index], stylesheet, states);
            index = index + 1;
        }
    }

    internal void VisitXsltAccumulatorDependency(XsltAccumulatorPlan accumulator, XsltStylesheetPlan stylesheet, int[] states)
    {
        int index = FindXsltAccumulatorIndex(stylesheet.Accumulators, accumulator);
        if (index < 0 || states[index] == 2)
        {
            return;
        }

        if (states[index] == 1)
        {
            XsltAccumulatorDeclarationPlan declaration = accumulator.EffectiveDeclaration;
            throw CreateXsltAccumulatorError("Accumulator dependencies contain a circularity.", XsltErrorCodes.XTDE3400, declaration.Declaration, declaration.Module);
        }

        states[index] = 1;
        int dependencyIndex = 0;
        while (dependencyIndex < accumulator.Dependencies.Length)
        {
            VisitXsltAccumulatorDependency(accumulator.Dependencies[dependencyIndex], stylesheet, states);
            dependencyIndex = dependencyIndex + 1;
        }

        states[index] = 2;
    }

    internal int FindXsltAccumulatorIndex(XsltAccumulatorPlan[] accumulators, XsltAccumulatorPlan accumulator)
    {
        int index = 0;
        while (index < accumulators.Length)
        {
            if (object.ReferenceEquals(accumulators[index], accumulator))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void BindXsltModeAccumulators(XsltStylesheetPlan stylesheet)
    {
        int index = 0;
        while (index < stylesheet.Modes.Length)
        {
            XsltModePlan mode = stylesheet.Modes[index];
            List<XsltAccumulatorPlan> bindings = new List<XsltAccumulatorPlan>();
            if (mode.UseAllAccumulators)
            {
                int accumulatorIndex = 0;
                while (accumulatorIndex < stylesheet.Accumulators.Length)
                {
                    bindings.Add(stylesheet.Accumulators[accumulatorIndex]);
                    accumulatorIndex = accumulatorIndex + 1;
                }
            }
            else
            {
                int nameIndex = 0;
                while (nameIndex < mode.AccumulatorNames.Length)
                {
                    XsltAccumulatorPlan? accumulator = FindXsltAccumulatorInArray(stylesheet.Accumulators, mode.AccumulatorNames[nameIndex]);
                    if (accumulator == null || !accumulator.EffectiveDeclaration.Streamable)
                    {
                        throw CreateXsltAccumulatorError("A mode use-accumulators name must identify a declared-streamable accumulator.", XsltErrorCodes.XTSE3300, mode.Origin, mode.Module);
                    }

                    bindings.Add(accumulator);
                    nameIndex = nameIndex + 1;
                }
            }

            mode.Accumulators = bindings.ToArray();
            index = index + 1;
        }
    }

    internal XsltStaticException CreateXsltAccumulatorTaskError(string message, XmlNode node, XsltStylesheetModule module, AggregateException? failure)
    {
        return CreateXsltAccumulatorError(message, Loader.StaticProcessor.ReadXsltTaskErrorCode(failure), node, module);
    }

    internal XsltStaticException CreateXsltAccumulatorError(string message, string code, XmlNode node, XsltStylesheetModule module)
    {
        return new XsltStylesheetAssembler(Loader).CreateXsltAssemblyError(message, code, node, module);
    }
}
