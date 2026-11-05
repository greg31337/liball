using System.Collections.Generic;

namespace xml;

internal class XsltExecutableBuilder
{
    internal XsltStylesheetLoader Loader;

    internal XsltExecutableBuilder(XsltStylesheetLoader loader)
    {
        Loader = loader;
    }

    internal XsltExecutable BuildXsltExecutable(XsltStylesheetPlan stylesheet, XsltPackagePlan package)
    {
        LinkXsltExecutableComponents(stylesheet, package);
        stylesheet.Components = (XsltComponent[])package.Components.Clone();
        stylesheet.Globals = CollectXsltExecutableGlobals(stylesheet);
        stylesheet.PatternIndexes = (XsltTemplateRuleIndex[])stylesheet.TemplateRuleIndexes.Clone();
        stylesheet.Outputs = (XsltOutputFormatPlan[])stylesheet.OutputFormats.Clone();
        stylesheet.Packages = CollectXsltExecutablePackages(package);
        stylesheet.XPathPlans = CollectXsltExecutableXPathPlans(stylesheet);
        stylesheet.Conformance = CreateXsltExecutableConformance(stylesheet);
        stylesheet.SourceMap = CreateXsltExecutableSourceMap(stylesheet);
        XsltStreamabilityAnalysis? streamability = null;
        if (Loader.Options.AnalyzeStreamability || stylesheet.Conformance.Streaming)
        {
            streamability = new XsltStreamabilityAnalyzer().AnalyzeXsltStreamability(stylesheet);
            stylesheet.StreamabilityAnalysis = streamability;
        }

        if (stylesheet.Conformance.Streaming && streamability != null && !streamability.Package.Guaranteed)
        {
            string reason = streamability.Reasons.Length == 0 ? "The stylesheet is not guaranteed streamable." : streamability.Reasons[0];
            throw Loader.CreateXsltStaticError("Guaranteed-streaming conformance requires a streamable stylesheet: " + reason, XsltErrorCodes.XTSE3430, stylesheet.PrincipalModule.Root, stylesheet.PrincipalModule.BaseUri);
        }

        SnapshotXsltExecutableArrays(stylesheet);
        stylesheet.IsImmutable = true;

        XsltExecutable result = new XsltExecutable();
        result.Plan = stylesheet;
        result.CompiledPlan = stylesheet;
        result.PackageName = Loader.CreateXsltPackagePublicName(package.Name);
        result.PackageVersion = package.Version;
        XsltCompileOptions compiledOptions = Loader.CopyXsltCompileOptions(Loader.Options);
        XsltStreamabilityReport? compiledStreamability = CreateXsltExecutableStreamabilityReport(streamability, stylesheet.Conformance.SchemaAware);
        result.CompileOptions = Loader.CopyXsltCompileOptions(compiledOptions);
        result.CompiledOptions = compiledOptions;
        result.StreamabilityReport = compiledStreamability;
        result.Diagnostics = [];
        result.IsImmutable = true;
        result.IsSchemaAware = stylesheet.Conformance.SchemaAware;
        result.IsStreaming = stylesheet.Conformance.Streaming;
        result.IsThreadSafe = true;
        return result;
    }

    internal XsltStreamabilityReport? CreateXsltExecutableStreamabilityReport(XsltStreamabilityAnalysis? analysis, bool schemaAware)
    {
        if (analysis == null)
        {
            return null;
        }

        return new XsltStreamabilityReportFactory().CreateXsltStreamabilityReport(analysis, Loader.Options.ConformanceLevel, schemaAware);
    }

    internal void LinkXsltExecutableComponents(XsltStylesheetPlan stylesheet, XsltPackagePlan package)
    {
        int index = 0;
        while (index < package.Components.Length)
        {
            XsltComponent component = package.Components[index];
            if (component.Kind == XsltComponentKind.Template)
            {
                XsltTemplatePlan? template = FindXsltExecutableTemplate(stylesheet, component.Declaration);
                if (template != null)
                {
                    component.Template = template;
                    template.Component = component;
                }
            }
            else if (component.Kind == XsltComponentKind.Variable)
            {
                XsltVariablePlan? variable = FindXsltExecutableVariable(stylesheet, component.Declaration);
                if (variable != null)
                {
                    component.Variable = variable;
                    variable.Component = component;
                }
            }

            index = index + 1;
        }
    }

    internal XsltTemplatePlan? FindXsltExecutableTemplate(XsltStylesheetPlan stylesheet, XmlNode declaration)
    {
        int index = 0;
        while (index < stylesheet.Templates.Length)
        {
            if (object.ReferenceEquals(stylesheet.Templates[index].Declaration, declaration))
            {
                return stylesheet.Templates[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltVariablePlan? FindXsltExecutableVariable(XsltStylesheetPlan stylesheet, XmlNode declaration)
    {
        int index = 0;
        while (index < stylesheet.Variables.Length)
        {
            if (object.ReferenceEquals(stylesheet.Variables[index].Declaration, declaration))
            {
                return stylesheet.Variables[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltVariablePlan[] CollectXsltExecutableGlobals(XsltStylesheetPlan stylesheet)
    {
        List<XsltVariablePlan> result = new List<XsltVariablePlan>();
        int index = 0;
        while (index < stylesheet.Variables.Length)
        {
            XsltVariablePlan variable = stylesheet.Variables[index];
            if (variable.IsGlobal && variable.IsEffective)
            {
                result.Add(variable);
            }

            index = index + 1;
        }

        XsltPackagePlan? package = stylesheet.Package;
        XsltComponent[] components = package == null ? [] : package.Components;
        index = 0;
        while (index < components.Length)
        {
            XsltVariablePlan? variable = components[index].Variable;
            if (variable != null && variable.IsGlobal && !ContainsXsltExecutableGlobal(result, variable))
            {
                result.Add(variable);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal bool ContainsXsltExecutableGlobal(List<XsltVariablePlan> variables, XsltVariablePlan candidate)
    {
        int index = 0;
        while (index < variables.Count)
        {
            if (object.ReferenceEquals(variables[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XsltPackagePlan[] CollectXsltExecutablePackages(XsltPackagePlan package)
    {
        List<XsltPackagePlan> result = new List<XsltPackagePlan>();
        AddXsltExecutablePackage(result, package);
        return result.ToArray();
    }

    internal void AddXsltExecutablePackage(List<XsltPackagePlan> result, XsltPackagePlan package)
    {
        int index = 0;
        while (index < result.Count)
        {
            if (object.ReferenceEquals(result[index], package))
            {
                return;
            }

            index = index + 1;
        }

        result.Add(package);
        index = 0;
        while (index < package.Uses.Length)
        {
            AddXsltExecutablePackage(result, package.Uses[index].Target);
            index = index + 1;
        }
    }

    internal XPathPlan[] CollectXsltExecutableXPathPlans(XsltStylesheetPlan stylesheet)
    {
        List<XPathPlan> result = new List<XPathPlan>();
        CollectXsltVariableXPathPlans(result, stylesheet.Variables);
        CollectXsltValueTemplateXPathPlans(result, stylesheet.ValueTemplates);
        CollectXsltInstructionXPathPlans(result, stylesheet.Instructions);
        CollectXsltTemplateXPathPlans(result, stylesheet.Templates);
        CollectXsltFunctionXPathPlans(result, stylesheet.Functions);
        CollectXsltKeyXPathPlans(result, stylesheet.Keys);
        CollectXsltAccumulatorXPathPlans(result, stylesheet.Accumulators);
        return result.ToArray();
    }

    internal void CollectXsltVariableXPathPlans(List<XPathPlan> result, XsltVariablePlan[] variables)
    {
        int index = 0;
        while (index < variables.Length)
        {
            AddXsltExecutableXPathPlan(result, variables[index].SelectPlan);
            index = index + 1;
        }
    }

    internal void CollectXsltValueTemplateXPathPlans(List<XPathPlan> result, XsltValueTemplate[] templates)
    {
        int templateIndex = 0;
        while (templateIndex < templates.Length)
        {
            XsltValueTemplatePart[] parts = templates[templateIndex].Parts;
            int partIndex = 0;
            while (partIndex < parts.Length)
            {
                AddXsltExecutableXPathPlan(result, parts[partIndex].ExpressionPlan);
                partIndex = partIndex + 1;
            }

            templateIndex = templateIndex + 1;
        }
    }

    internal void CollectXsltInstructionXPathPlans(List<XPathPlan> result, XsltInstructionPlan[] instructions)
    {
        int instructionIndex = 0;
        while (instructionIndex < instructions.Length)
        {
            XsltInstructionAttributePlan[] attributes = instructions[instructionIndex].Attributes;
            int attributeIndex = 0;
            while (attributeIndex < attributes.Length)
            {
                AddXsltExecutableXPathPlan(result, attributes[attributeIndex].Expression);
                AddXsltExecutablePatternXPathPlans(result, attributes[attributeIndex].Pattern);
                attributeIndex = attributeIndex + 1;
            }

            instructionIndex = instructionIndex + 1;
        }
    }

    internal void CollectXsltTemplateXPathPlans(List<XPathPlan> result, XsltTemplatePlan[] templates)
    {
        int index = 0;
        while (index < templates.Length)
        {
            AddXsltExecutablePatternXPathPlans(result, templates[index].MatchPlan);
            index = index + 1;
        }
    }

    internal void CollectXsltFunctionXPathPlans(List<XPathPlan> result, XsltFunctionPlan[] functions)
    {
        int functionIndex = 0;
        while (functionIndex < functions.Length)
        {
            XsltFunctionExpressionPlan[] expressions = functions[functionIndex].Expressions;
            int expressionIndex = 0;
            while (expressionIndex < expressions.Length)
            {
                AddXsltExecutableXPathPlan(result, expressions[expressionIndex].Plan);
                expressionIndex = expressionIndex + 1;
            }

            functionIndex = functionIndex + 1;
        }
    }

    internal void CollectXsltKeyXPathPlans(List<XPathPlan> result, XsltKeyPlan[] keys)
    {
        int keyIndex = 0;
        while (keyIndex < keys.Length)
        {
            XsltKeyDeclarationPlan[] declarations = keys[keyIndex].Declarations;
            int declarationIndex = 0;
            while (declarationIndex < declarations.Length)
            {
                AddXsltExecutableXPathPlan(result, declarations[declarationIndex].Use);
                AddXsltExecutablePatternXPathPlans(result, declarations[declarationIndex].Match);
                declarationIndex = declarationIndex + 1;
            }

            keyIndex = keyIndex + 1;
        }
    }

    internal void CollectXsltAccumulatorXPathPlans(List<XPathPlan> result, XsltAccumulatorPlan[] accumulators)
    {
        int accumulatorIndex = 0;
        while (accumulatorIndex < accumulators.Length)
        {
            XsltAccumulatorDeclarationPlan[] declarations = accumulators[accumulatorIndex].Declarations;
            int declarationIndex = 0;
            while (declarationIndex < declarations.Length)
            {
                XsltAccumulatorDeclarationPlan declaration = declarations[declarationIndex];
                AddXsltExecutableXPathPlan(result, declaration.InitialValue);
                int ruleIndex = 0;
                while (ruleIndex < declaration.Rules.Length)
                {
                    AddXsltExecutableXPathPlan(result, declaration.Rules[ruleIndex].Select);
                    AddXsltExecutablePatternXPathPlans(result, declaration.Rules[ruleIndex].Match);
                    ruleIndex = ruleIndex + 1;
                }

                declarationIndex = declarationIndex + 1;
            }

            accumulatorIndex = accumulatorIndex + 1;
        }
    }

    internal void AddXsltExecutablePatternXPathPlans(List<XPathPlan> result, XsltMatchPatternPlan? pattern)
    {
        if (pattern == null)
        {
            return;
        }

        AddXsltExecutableXPathPlan(result, pattern.EquivalentExpression);
        int index = 0;
        while (index < pattern.Predicates.Length)
        {
            AddXsltExecutableXPathPlan(result, pattern.Predicates[index].Expression);
            index = index + 1;
        }
    }

    internal void AddXsltExecutableXPathPlan(List<XPathPlan> result, XPathPlan? plan)
    {
        if (plan == null)
        {
            return;
        }

        int index = 0;
        while (index < result.Count)
        {
            if (object.ReferenceEquals(result[index], plan))
            {
                return;
            }

            index = index + 1;
        }

        result.Add(plan);
    }

    internal XsltExecutableConformance CreateXsltExecutableConformance(XsltStylesheetPlan stylesheet)
    {
        XsltExecutableConformance result = new XsltExecutableConformance();
        result.Level = Loader.Options.ConformanceLevel;
        result.SchemaAware = result.Level == XsltConformanceLevel.SchemaAware;
        result.Streaming = result.Level == XsltConformanceLevel.Streaming;
        result.UsesPackages = stylesheet.Package != null && stylesheet.Package.Uses.Length != 0;
        result.UsesSchemaImports = stylesheet.SchemaImports.Length != 0;
        result.DeclaredModes = stylesheet.DeclaredModes;
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            XsltCompatibilityBehavior behavior = stylesheet.Declarations[index].CompatibilityBehavior;
            if (behavior == XsltCompatibilityBehavior.Backwards)
            {
                result.HasBackwardsCompatibleCode = true;
            }
            else if (behavior == XsltCompatibilityBehavior.Forwards)
            {
                result.HasForwardsCompatibleCode = true;
            }

            index = index + 1;
        }

        return result;
    }

    internal XsltSourceMapEntry[] CreateXsltExecutableSourceMap(XsltStylesheetPlan stylesheet)
    {
        List<XsltSourceMapEntry> result = new List<XsltSourceMapEntry>();
        AddXsltSourceMapEntry(result, stylesheet, "stylesheet", stylesheet.PrincipalModule.Root);
        AddXsltPackageSourceMapEntries(result, stylesheet);
        AddXsltVariableSourceMapEntries(result, stylesheet.Variables);
        AddXsltTemplateSourceMapEntries(result, stylesheet.Templates);
        AddXsltPatternIndexSourceMapEntries(result, stylesheet.TemplateRuleIndexes);
        AddXsltFunctionSourceMapEntries(result, stylesheet.Functions);
        AddXsltModeSourceMapEntries(result, stylesheet.Modes);
        AddXsltAttributeSetSourceMapEntries(result, stylesheet.AttributeSets);
        AddXsltInstructionSourceMapEntries(result, stylesheet.Instructions);
        AddXsltKeySourceMapEntries(result, stylesheet.Keys);
        AddXsltAccumulatorSourceMapEntries(result, stylesheet.Accumulators);
        AddXsltOutputSourceMapEntries(result, stylesheet.OutputFormats, stylesheet.PrincipalModule.Root);
        AddXsltXPathSourceMapEntries(result, stylesheet);
        return result.ToArray();
    }

    internal void AddXsltPackageSourceMapEntries(List<XsltSourceMapEntry> result, XsltStylesheetPlan stylesheet)
    {
        int index = 0;
        while (index < stylesheet.Packages.Length)
        {
            XsltPackagePlan package = stylesheet.Packages[index];
            XmlNode node = index == 0 ? stylesheet.PrincipalModule.Root : FindXsltPackageSourceNode(package);
            AddXsltSourceMapEntry(result, package, "package", node);
            int componentIndex = 0;
            while (componentIndex < package.Components.Length)
            {
                AddXsltSourceMapEntry(result, package.Components[componentIndex], "component", package.Components[componentIndex].Declaration);
                componentIndex = componentIndex + 1;
            }

            index = index + 1;
        }
    }

    internal XmlNode FindXsltPackageSourceNode(XsltPackagePlan package)
    {
        if (package.Components.Length != 0)
        {
            return package.Components[0].Declaration;
        }

        return Loader.Modules[0].Root;
    }

    internal void AddXsltVariableSourceMapEntries(List<XsltSourceMapEntry> result, XsltVariablePlan[] variables)
    {
        int index = 0;
        while (index < variables.Length)
        {
            AddXsltSourceMapEntry(result, variables[index], "variable", variables[index].Declaration);
            index = index + 1;
        }
    }

    internal void AddXsltTemplateSourceMapEntries(List<XsltSourceMapEntry> result, XsltTemplatePlan[] templates)
    {
        int index = 0;
        while (index < templates.Length)
        {
            AddXsltSourceMapEntry(result, templates[index], "template", templates[index].Declaration);
            AddXsltPatternSourceMapEntry(result, templates[index].MatchPlan, templates[index].Declaration);
            index = index + 1;
        }
    }

    internal void AddXsltPatternIndexSourceMapEntries(List<XsltSourceMapEntry> result, XsltTemplateRuleIndex[] indexes)
    {
        int index = 0;
        while (index < indexes.Length)
        {
            XsltTemplateRuleIndex patternIndex = indexes[index];
            AddXsltSourceMapEntry(result, patternIndex, "pattern-index", patternIndex.ModePlan.Origin);
            int ruleIndex = 0;
            while (ruleIndex < patternIndex.Rules.Length)
            {
                XsltTemplateRulePlan rule = patternIndex.Rules[ruleIndex];
                AddXsltSourceMapEntry(result, rule, "template-rule", rule.Template.Declaration);
                ruleIndex = ruleIndex + 1;
            }

            index = index + 1;
        }
    }

    internal void AddXsltFunctionSourceMapEntries(List<XsltSourceMapEntry> result, XsltFunctionPlan[] functions)
    {
        int index = 0;
        while (index < functions.Length)
        {
            AddXsltSourceMapEntry(result, functions[index], "function", functions[index].Declaration);
            index = index + 1;
        }
    }

    internal void AddXsltModeSourceMapEntries(List<XsltSourceMapEntry> result, XsltModePlan[] modes)
    {
        int index = 0;
        while (index < modes.Length)
        {
            AddXsltSourceMapEntry(result, modes[index], "mode", modes[index].Origin);
            index = index + 1;
        }
    }

    internal void AddXsltAttributeSetSourceMapEntries(List<XsltSourceMapEntry> result, XsltAttributeSetPlan[] sets)
    {
        int index = 0;
        while (index < sets.Length)
        {
            AddXsltSourceMapEntry(result, sets[index], "attribute-set", sets[index].Origin);
            index = index + 1;
        }
    }

    internal void AddXsltInstructionSourceMapEntries(List<XsltSourceMapEntry> result, XsltInstructionPlan[] instructions)
    {
        int index = 0;
        while (index < instructions.Length)
        {
            XsltInstructionPlan instruction = instructions[index];
            AddXsltSourceMapEntry(result, instruction, "instruction", instruction.Instruction);
            int attributeIndex = 0;
            while (attributeIndex < instruction.Attributes.Length)
            {
                XsltInstructionAttributePlan attribute = instruction.Attributes[attributeIndex];
                AddXsltPatternSourceMapEntry(result, attribute.Pattern, attribute.Attribute);
                attributeIndex = attributeIndex + 1;
            }

            index = index + 1;
        }
    }

    internal void AddXsltKeySourceMapEntries(List<XsltSourceMapEntry> result, XsltKeyPlan[] keys)
    {
        int keyIndex = 0;
        while (keyIndex < keys.Length)
        {
            XsltKeyPlan key = keys[keyIndex];
            XmlNode node = key.Declarations[0].Declaration;
            AddXsltSourceMapEntry(result, key, "key", node);
            int declarationIndex = 0;
            while (declarationIndex < key.Declarations.Length)
            {
                XsltKeyDeclarationPlan declaration = key.Declarations[declarationIndex];
                AddXsltSourceMapEntry(result, declaration, "key-declaration", declaration.Declaration);
                AddXsltPatternSourceMapEntry(result, declaration.Match, declaration.Declaration);
                declarationIndex = declarationIndex + 1;
            }

            keyIndex = keyIndex + 1;
        }
    }

    internal void AddXsltAccumulatorSourceMapEntries(List<XsltSourceMapEntry> result, XsltAccumulatorPlan[] accumulators)
    {
        int accumulatorIndex = 0;
        while (accumulatorIndex < accumulators.Length)
        {
            XsltAccumulatorPlan accumulator = accumulators[accumulatorIndex];
            AddXsltSourceMapEntry(result, accumulator, "accumulator", accumulator.EffectiveDeclaration.Declaration);
            int declarationIndex = 0;
            while (declarationIndex < accumulator.Declarations.Length)
            {
                XsltAccumulatorDeclarationPlan declaration = accumulator.Declarations[declarationIndex];
                AddXsltSourceMapEntry(result, declaration, "accumulator-declaration", declaration.Declaration);
                int ruleIndex = 0;
                while (ruleIndex < declaration.Rules.Length)
                {
                    XsltAccumulatorRulePlan rule = declaration.Rules[ruleIndex];
                    AddXsltSourceMapEntry(result, rule, "accumulator-rule", rule.Declaration);
                    AddXsltPatternSourceMapEntry(result, rule.Match, rule.Declaration);
                    ruleIndex = ruleIndex + 1;
                }

                declarationIndex = declarationIndex + 1;
            }

            accumulatorIndex = accumulatorIndex + 1;
        }
    }

    internal void AddXsltOutputSourceMapEntries(List<XsltSourceMapEntry> result, XsltOutputFormatPlan[] outputs, XmlNode root)
    {
        int outputIndex = 0;
        while (outputIndex < outputs.Length)
        {
            XsltOutputFormatPlan output = outputs[outputIndex];
            XmlNode node = output.Declarations.Length == 0 ? root : output.Declarations[0].Declaration;
            AddXsltSourceMapEntry(result, output, "output", node);

            outputIndex = outputIndex + 1;
        }
    }

    internal void AddXsltXPathSourceMapEntries(List<XsltSourceMapEntry> result, XsltStylesheetPlan stylesheet)
    {
        int index = 0;
        while (index < stylesheet.XPathPlans.Length)
        {
            XPathPlan plan = stylesheet.XPathPlans[index];
            XmlNode node = FindXsltXPathSourceNode(stylesheet, plan);
            AddXsltRangedSourceMapEntry(result, plan, "xpath", node, plan.Range);
            index = index + 1;
        }
    }

    internal XmlNode FindXsltXPathSourceNode(XsltStylesheetPlan stylesheet, XPathPlan plan)
    {
        int index = 0;
        while (index < stylesheet.Variables.Length)
        {
            if (object.ReferenceEquals(stylesheet.Variables[index].SelectPlan, plan))
            {
                return stylesheet.Variables[index].Declaration;
            }

            index = index + 1;
        }

        index = 0;
        while (index < stylesheet.ValueTemplates.Length)
        {
            XsltValueTemplate template = stylesheet.ValueTemplates[index];
            int partIndex = 0;
            while (partIndex < template.Parts.Length)
            {
                if (object.ReferenceEquals(template.Parts[partIndex].ExpressionPlan, plan))
                {
                    return template.Node;
                }

                partIndex = partIndex + 1;
            }

            index = index + 1;
        }

        index = 0;
        while (index < stylesheet.Instructions.Length)
        {
            XsltInstructionAttributePlan[] attributes = stylesheet.Instructions[index].Attributes;
            int attributeIndex = 0;
            while (attributeIndex < attributes.Length)
            {
                if (object.ReferenceEquals(attributes[attributeIndex].Expression, plan))
                {
                    return attributes[attributeIndex].Attribute;
                }

                attributeIndex = attributeIndex + 1;
            }

            index = index + 1;
        }


        index = 0;
        while (index < stylesheet.Functions.Length)
        {
            XsltFunctionExpressionPlan[] expressions = stylesheet.Functions[index].Expressions;
            int expressionIndex = 0;
            while (expressionIndex < expressions.Length)
            {
                if (object.ReferenceEquals(expressions[expressionIndex].Plan, plan))
                {
                    return expressions[expressionIndex].Node;
                }

                expressionIndex = expressionIndex + 1;
            }

            index = index + 1;
        }

        index = 0;
        while (index < stylesheet.Keys.Length)
        {
            XsltKeyDeclarationPlan[] declarations = stylesheet.Keys[index].Declarations;
            int declarationIndex = 0;
            while (declarationIndex < declarations.Length)
            {
                if (object.ReferenceEquals(declarations[declarationIndex].Use, plan))
                {
                    return declarations[declarationIndex].Declaration;
                }

                declarationIndex = declarationIndex + 1;
            }

            index = index + 1;
        }

        index = 0;
        while (index < stylesheet.Accumulators.Length)
        {
            XsltAccumulatorDeclarationPlan[] declarations = stylesheet.Accumulators[index].Declarations;
            int declarationIndex = 0;
            while (declarationIndex < declarations.Length)
            {
                XsltAccumulatorDeclarationPlan declaration = declarations[declarationIndex];
                if (object.ReferenceEquals(declaration.InitialValue, plan))
                {
                    return declaration.Declaration;
                }

                int ruleIndex = 0;
                while (ruleIndex < declaration.Rules.Length)
                {
                    if (object.ReferenceEquals(declaration.Rules[ruleIndex].Select, plan))
                    {
                        return declaration.Rules[ruleIndex].Declaration;
                    }

                    ruleIndex = ruleIndex + 1;
                }

                declarationIndex = declarationIndex + 1;
            }

            index = index + 1;
        }

        return stylesheet.PrincipalModule.Root;
    }

    internal void AddXsltPatternSourceMapEntry(List<XsltSourceMapEntry> result, XsltMatchPatternPlan? pattern, XmlNode node)
    {
        if (pattern != null)
        {
            AddXsltRangedSourceMapEntry(result, pattern, "pattern", node, pattern.Range);
        }
    }

    internal void AddXsltRangedSourceMapEntry(List<XsltSourceMapEntry> result, object item, string kind, XmlNode node, SourceRange? sourceRange)
    {
        int index = 0;
        while (index < result.Count)
        {
            if (object.ReferenceEquals(result[index].Item, item))
            {
                return;
            }

            index = index + 1;
        }

        SourceRange? range = CopyXsltExecutableRange(sourceRange == null ? node.Range : sourceRange);
        string? sourceUri = range == null || range.Start == null ? null : range.Start.SourceUri;
        XmlDocument? document = node.OwnerDocument;
        if (sourceUri == null && document != null)
        {
            sourceUri = document.BaseUri;
        }

        result.Add(new XsltSourceMapEntry(item, kind, sourceUri, range));
    }

    internal void AddXsltSourceMapEntry(List<XsltSourceMapEntry> result, object item, string kind, XmlNode node)
    {
        AddXsltRangedSourceMapEntry(result, item, kind, node, node.Range);
    }

    internal SourceRange? CopyXsltExecutableRange(SourceRange? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceRange result = new SourceRange();
        result.Start = CopyXsltExecutableLocation(source.Start);
        result.End = CopyXsltExecutableLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXsltExecutableLocation(SourceLocation? source)
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

    internal void SnapshotXsltExecutableArrays(XsltStylesheetPlan stylesheet)
    {
        stylesheet.Variables = (XsltVariablePlan[])stylesheet.Variables.Clone();
        stylesheet.ValueTemplates = (XsltValueTemplate[])stylesheet.ValueTemplates.Clone();
        stylesheet.Templates = (XsltTemplatePlan[])stylesheet.Templates.Clone();
        stylesheet.TemplateRuleIndexes = (XsltTemplateRuleIndex[])stylesheet.TemplateRuleIndexes.Clone();
        stylesheet.Functions = (XsltFunctionPlan[])stylesheet.Functions.Clone();
        stylesheet.Modes = (XsltModePlan[])stylesheet.Modes.Clone();
        stylesheet.AttributeSets = (XsltAttributeSetPlan[])stylesheet.AttributeSets.Clone();
        stylesheet.AttributeSetUses = (XsltAttributeSetUsePlan[])stylesheet.AttributeSetUses.Clone();
        stylesheet.Keys = (XsltKeyPlan[])stylesheet.Keys.Clone();
        stylesheet.Accumulators = (XsltAccumulatorPlan[])stylesheet.Accumulators.Clone();
        stylesheet.DecimalFormats = (XsltDecimalFormatPlan[])stylesheet.DecimalFormats.Clone();
        stylesheet.CharacterMaps = (XsltCharacterMapPlan[])stylesheet.CharacterMaps.Clone();
        stylesheet.OutputFormats = (XsltOutputFormatPlan[])stylesheet.OutputFormats.Clone();
        stylesheet.WhitespaceRules = (XsltWhitespaceRulePlan[])stylesheet.WhitespaceRules.Clone();
        stylesheet.Instructions = (XsltInstructionPlan[])stylesheet.Instructions.Clone();
    }
}
