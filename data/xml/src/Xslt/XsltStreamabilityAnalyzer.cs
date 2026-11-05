using System;
using System.Collections.Generic;

namespace xml;

internal class XsltStreamabilityAnalyzer
{
    internal List<XsltStreamabilityEntry> Entries;
    internal Dictionary<XPathPlanInstruction, XsltStreamabilityClassification> Expressions;
    internal Dictionary<XsltMatchPatternPlan, XsltStreamabilityClassification> Patterns;
    internal Dictionary<XsltSequenceConstructorPlan, XsltStreamabilityClassification> Constructors;
    internal Dictionary<XsltInstructionPlan, XsltStreamabilityClassification> Instructions;
    internal Dictionary<XsltTemplatePlan, XsltStreamabilityClassification> Templates;
    internal Dictionary<XsltFunctionPlan, XsltStreamabilityClassification> Functions;
    internal Dictionary<XsltAccumulatorDeclarationPlan, XsltStreamabilityClassification> Accumulators;
    internal Dictionary<XsltAttributeSetPlan, XsltStreamabilityClassification> AttributeSets;
    internal Dictionary<XsltModePlan, XsltStreamabilityClassification> Modes;
    internal XsltStylesheetPlan? Stylesheet;

    internal XsltStreamabilityAnalyzer()
    {
        Entries = new List<XsltStreamabilityEntry>();
        Expressions = new Dictionary<XPathPlanInstruction, XsltStreamabilityClassification>();
        Patterns = new Dictionary<XsltMatchPatternPlan, XsltStreamabilityClassification>();
        Constructors = new Dictionary<XsltSequenceConstructorPlan, XsltStreamabilityClassification>();
        Instructions = new Dictionary<XsltInstructionPlan, XsltStreamabilityClassification>();
        Templates = new Dictionary<XsltTemplatePlan, XsltStreamabilityClassification>();
        Functions = new Dictionary<XsltFunctionPlan, XsltStreamabilityClassification>();
        Accumulators = new Dictionary<XsltAccumulatorDeclarationPlan, XsltStreamabilityClassification>();
        AttributeSets = new Dictionary<XsltAttributeSetPlan, XsltStreamabilityClassification>();
        Modes = new Dictionary<XsltModePlan, XsltStreamabilityClassification>();
        Stylesheet = null;
    }

    internal XsltStreamabilityAnalysis AnalyzeXsltStreamability(XsltStylesheetPlan stylesheet)
    {
        Guard.NotNull(stylesheet, "stylesheet");
        Stylesheet = stylesheet;
        int index = 0;
        while (index < stylesheet.XPathPlans.Length)
        {
            AnalyzeXsltStreamabilityExpression(stylesheet.XPathPlans[index].Root);
            index = index + 1;
        }

        index = 0;
        while (index < stylesheet.Templates.Length)
        {
            AnalyzeXsltStreamabilityTemplate(stylesheet.Templates[index], stylesheet.Modes);
            index = index + 1;
        }

        index = 0;
        while (index < stylesheet.Functions.Length)
        {
            AnalyzeXsltStreamabilityFunction(stylesheet.Functions[index]);
            index = index + 1;
        }

        index = 0;
        while (index < stylesheet.Accumulators.Length)
        {
            AnalyzeXsltStreamabilityAccumulator(stylesheet.Accumulators[index].EffectiveDeclaration);
            index = index + 1;
        }

        index = 0;
        while (index < stylesheet.AttributeSets.Length)
        {
            AnalyzeXsltStreamabilityAttributeSet(stylesheet.AttributeSets[index]);
            index = index + 1;
        }

        AnalyzeXsltStreamabilityPackageComponents(stylesheet);

        List<XmlName> modes = new List<XmlName>();
        index = 0;
        while (index < stylesheet.Modes.Length)
        {
            XsltModePlan mode = stylesheet.Modes[index];
            AnalyzeXsltStreamabilityMode(mode, stylesheet.Templates);
            if (mode.Streamable)
            {
                modes.Add(CreateXsltStreamabilityModeName(mode.Name));
            }

            index = index + 1;
        }

        XsltStreamabilityClassification package = CreateXsltStreamabilityClassification(XsltPosture.Grounded, XsltSweep.Motionless, "");
        List<string> reasons = new List<string>();
        List<SourceRange> locations = new List<SourceRange>();
        index = 0;
        while (index < Entries.Count)
        {
            XsltStreamabilityEntry entry = Entries[index];
            bool packageRequirement = entry.Required && (entry.Kind == "mode" || entry.Kind == "function" || entry.Kind == "accumulator" || entry.Kind == "attribute-set");
            if (packageRequirement)
            {
                SelectWorseXsltStreamability(package, entry.Classification);
                if (!entry.Classification.Guaranteed)
                {
                    reasons.Add(entry.Kind + " " + entry.Name + ": " + entry.Classification.Reason);
                    locations.Add(entry.Range == null ? new SourceRange() : entry.Range);
                }
            }

            index = index + 1;
        }

        if (reasons.Count != 0)
        {
            FailXsltStreamability(package, reasons[0]);
        }

        string packageName = stylesheet.Package == null || stylesheet.Package.Name == null ? "#stylesheet" : stylesheet.Package.Name;
        Entries.Add(new XsltStreamabilityEntry("package", packageName, stylesheet.Package == null ? stylesheet : stylesheet.Package, stylesheet.PrincipalModule.Root.Range, package, true));
        XsltStreamabilityAnalysis result = new XsltStreamabilityAnalysis(Entries.ToArray(), package, reasons.ToArray(), locations.ToArray(), modes.ToArray());
        return result;
    }

    internal XsltStreamabilityClassification AnalyzeXsltStreamabilityExpression(XPathPlanInstruction instruction)
    {
        if (Expressions.ContainsKey(instruction))
        {
            return Expressions[instruction];
        }

        XsltStreamabilityClassification result = ClassifyXsltStreamabilityOpcode(instruction);
        bool path = instruction.Opcode == XPathPlanOpcode.Path;
        MergeOptionalXsltStreamabilityExpression(result, instruction.Left, path);
        MergeOptionalXsltStreamabilityExpression(result, instruction.Right, path);
        MergeOptionalXsltStreamabilityExpression(result, instruction.Operand, path);
        MergeOptionalXsltStreamabilityExpression(result, instruction.Condition, false);
        MergeOptionalXsltStreamabilityExpression(result, instruction.ThenInstruction, true);
        MergeOptionalXsltStreamabilityExpression(result, instruction.ElseInstruction, true);
        MergeOptionalXsltStreamabilityExpression(result, instruction.Body, false);
        int index = 0;
        while (index < instruction.Children.Length)
        {
            MergeXsltStreamability(result, AnalyzeXsltStreamabilityExpression(instruction.Children[index]), path);
            index = index + 1;
        }

        index = 0;
        while (index < instruction.Bindings.Length)
        {
            MergeXsltStreamability(result, AnalyzeXsltStreamabilityExpression(instruction.Bindings[index].Expression), false);
            index = index + 1;
        }

        index = 0;
        while (index < instruction.MapEntries.Length)
        {
            MergeXsltStreamability(result, AnalyzeXsltStreamabilityExpression(instruction.MapEntries[index].Key), false);
            MergeXsltStreamability(result, AnalyzeXsltStreamabilityExpression(instruction.MapEntries[index].Value), false);
            index = index + 1;
        }

        ApplyXsltStreamabilityExpressionRules(result, instruction);
        Expressions.Add(instruction, result);
        Entries.Add(new XsltStreamabilityEntry("expression", instruction.Opcode.ToString(), instruction, instruction.Range, result, false));
        return result;
    }

    internal XsltStreamabilityClassification ClassifyXsltStreamabilityOpcode(XPathPlanInstruction instruction)
    {
        if (instruction.Opcode == XPathPlanOpcode.ContextItem)
        {
            return CreateXsltStreamabilityClassification(XsltPosture.Striding, XsltSweep.Motionless, "");
        }

        if (instruction.Opcode == XPathPlanOpcode.AttributeStep || instruction.Opcode == XPathPlanOpcode.ParentStep)
        {
            return CreateXsltStreamabilityClassification(XsltPosture.Striding, XsltSweep.Motionless, "");
        }

        if (instruction.Opcode == XPathPlanOpcode.AxisStep)
        {
            if (instruction.Axis == XPathAxis.Child)
            {
                return CreateXsltStreamabilityClassification(XsltPosture.Striding, XsltSweep.Consuming, "");
            }

            if (instruction.Axis == XPathAxis.Descendant || instruction.Axis == XPathAxis.DescendantOrSelf)
            {
                return CreateXsltStreamabilityClassification(XsltPosture.Crawling, XsltSweep.Consuming, "");
            }

            if (instruction.Axis == XPathAxis.Following || instruction.Axis == XPathAxis.FollowingSibling || instruction.Axis == XPathAxis.Preceding || instruction.Axis == XPathAxis.PrecedingSibling)
            {
                return CreateXsltStreamabilityClassification(XsltPosture.Roaming, XsltSweep.FreeRanging, "A lateral axis is free-ranging.");
            }

            return CreateXsltStreamabilityClassification(XsltPosture.Striding, XsltSweep.Motionless, "");
        }

        if (instruction.Opcode == XPathPlanOpcode.DynamicFunctionCall || instruction.Opcode == XPathPlanOpcode.InlineFunction)
        {
            return CreateXsltStreamabilityClassification(XsltPosture.Roaming, XsltSweep.FreeRanging, "A dynamic function call cannot be guaranteed streamable.");
        }

        return CreateXsltStreamabilityClassification(XsltPosture.Grounded, XsltSweep.Motionless, "");
    }

    internal void ApplyXsltStreamabilityExpressionRules(XsltStreamabilityClassification result, XPathPlanInstruction instruction)
    {
        if (instruction.Opcode == XPathPlanOpcode.Path && (instruction.Absolute || instruction.LeadingDescendant))
        {
            FailXsltStreamability(result, "A rooted path is roaming.");
        }

        if (instruction.Opcode == XPathPlanOpcode.StaticFunctionCall)
        {
            XsltFunctionPlan? stylesheetFunction = FindXsltStreamabilityFunction(instruction);
            if (stylesheetFunction != null)
            {
                MergeXsltStreamability(result, ClassifyXsltStreamabilityFunctionCall(stylesheetFunction), false);
            }

            string localName = instruction.LocalName == null ? "" : instruction.LocalName;
            if (IsXsltFreeRangingFunction(localName))
            {
                FailXsltStreamability(result, "The " + localName + " function is free-ranging.");
            }
            else if (IsXsltConsumingFocusFunction(localName) && instruction.Children.Length == 0)
            {
                result.Sweep = XsltSweep.Consuming;
                result.ConsumingOperandCount = 1;
            }
        }

        if (instruction.Opcode == XPathPlanOpcode.MapConstructor || instruction.Opcode == XPathPlanOpcode.SquareArrayConstructor || instruction.Opcode == XPathPlanOpcode.CurlyArrayConstructor)
        {
            result.Posture = XsltPosture.Grounded;
        }

        if (IsXsltGroundedStaticType(instruction.StaticType))
        {
            result.Posture = XsltPosture.Grounded;
        }

        result.Guaranteed = result.Posture != XsltPosture.Roaming && result.Sweep != XsltSweep.FreeRanging;
        if (!result.Guaranteed && result.Reason.Length == 0)
        {
            result.Reason = "The expression is roaming or free-ranging.";
        }
    }

    internal bool IsXsltFreeRangingFunction(string name)
    {
        return name == "collection" || name == "current-group" || name == "current-merge-group" || name == "doc" || name == "document" || name == "element-with-id" || name == "evaluate" || name == "function-lookup" || name == "id" || name == "key" || name == "root" || name == "transform" || name == "unparsed-text";
    }

    internal bool IsXsltConsumingFocusFunction(string name)
    {
        return name == "data" || name == "normalize-space" || name == "number" || name == "string" || name == "string-length";
    }

    internal bool IsXsltGroundedStaticType(XPathStaticType type)
    {
        return type.Cardinality == XPathStaticCardinality.Empty || type.ItemKind == XPathStaticItemKind.Atomic || type.ItemKind == XPathStaticItemKind.Function || type.ItemKind == XPathStaticItemKind.Map || type.ItemKind == XPathStaticItemKind.Array;
    }

    internal XsltStreamabilityClassification AnalyzeXsltStreamabilityPattern(XsltMatchPatternPlan pattern)
    {
        if (Patterns.ContainsKey(pattern))
        {
            return Patterns[pattern];
        }

        XsltStreamabilityClassification result = pattern.IsMotionless ? CreateXsltStreamabilityClassification(XsltPosture.Grounded, XsltSweep.Motionless, "") : CreateXsltStreamabilityClassification(XsltPosture.Roaming, XsltSweep.FreeRanging, "The match pattern is not motionless.");
        if (pattern.EquivalentExpression != null)
        {
            AnalyzeXsltStreamabilityExpression(pattern.EquivalentExpression.Root);
        }

        int index = 0;
        while (index < pattern.Predicates.Length)
        {
            AnalyzeXsltStreamabilityExpression(pattern.Predicates[index].Expression.Root);
            index = index + 1;
        }

        Patterns.Add(pattern, result);
        Entries.Add(new XsltStreamabilityEntry("pattern", pattern.Source, pattern, pattern.Range, result, false));
        return result;
    }

    internal XsltStreamabilityClassification AnalyzeXsltStreamabilityConstructor(XsltSequenceConstructorPlan constructor, bool allowMultipleConsumption)
    {
        if (Constructors.ContainsKey(constructor))
        {
            return Constructors[constructor];
        }

        XsltStreamabilityClassification result = CreateXsltStreamabilityClassification(XsltPosture.Grounded, XsltSweep.Motionless, "");
        int index = 0;
        while (index < constructor.Items.Length)
        {
            XsltStreamabilityClassification item = AnalyzeXsltStreamabilitySequenceItem(constructor.Items[index]);
            MergeXsltStreamability(result, item, allowMultipleConsumption);
            index = index + 1;
        }

        Constructors.Add(constructor, result);
        Entries.Add(new XsltStreamabilityEntry("sequence-constructor", "#sequence", constructor, constructor.Range, result, false));
        return result;
    }

    internal XsltStreamabilityClassification AnalyzeXsltStreamabilitySequenceItem(XsltSequenceItemPlan item)
    {
        if (item.Kind == XsltSequenceItemKind.Instruction && item.Instruction != null)
        {
            return AnalyzeXsltStreamabilityInstruction(item.Instruction, item.Content);
        }

        XsltStreamabilityClassification result = CreateXsltStreamabilityClassification(XsltPosture.Grounded, XsltSweep.Motionless, "");
        int index = 0;
        while (index < item.AttributeTemplates.Length)
        {
            AnalyzeXsltStreamabilityValueTemplate(result, item.AttributeTemplates[index]);
            index = index + 1;
        }

        if (item.TextTemplate != null)
        {
            AnalyzeXsltStreamabilityValueTemplate(result, item.TextTemplate);
        }

        if (item.Variable != null && item.Variable.SelectPlan != null)
        {
            XsltStreamabilityClassification selected = AnalyzeXsltStreamabilityExpression(item.Variable.SelectPlan.Root);
            MergeXsltStreamability(result, selected, false);
            if (selected.Posture != XsltPosture.Grounded)
            {
                FailXsltStreamability(result, "A variable in a streamed constructor must bind a grounded value.");
            }
        }

        if (item.Content != null)
        {
            MergeXsltStreamability(result, AnalyzeXsltStreamabilityConstructor(item.Content, false), false);
        }

        if (item.Kind == XsltSequenceItemKind.Text || item.Kind == XsltSequenceItemKind.LiteralResultElement)
        {
            result.Posture = XsltPosture.Grounded;
            result.Guaranteed = result.Sweep != XsltSweep.FreeRanging;
        }

        return result;
    }

    internal void AnalyzeXsltStreamabilityValueTemplate(XsltStreamabilityClassification target, XsltValueTemplate template)
    {
        int index = 0;
        while (index < template.Parts.Length)
        {
            XPathPlan? expression = template.Parts[index].ExpressionPlan;
            if (expression != null)
            {
                MergeXsltStreamability(target, AnalyzeXsltStreamabilityExpression(expression.Root), false);
            }

            index = index + 1;
        }
    }

    internal XsltStreamabilityClassification AnalyzeXsltStreamabilityInstruction(XsltInstructionPlan instruction, XsltSequenceConstructorPlan? content)
    {
        if (Instructions.ContainsKey(instruction))
        {
            return Instructions[instruction];
        }

        XsltStreamabilityClassification result = CreateXsltStreamabilityClassification(XsltPosture.Grounded, XsltSweep.Motionless, "");
        int index = 0;
        while (index < instruction.Attributes.Length)
        {
            XsltInstructionAttributePlan attribute = instruction.Attributes[index];
            if (attribute.Expression != null)
            {
                MergeXsltStreamability(result, AnalyzeXsltStreamabilityExpression(attribute.Expression.Root), false);
            }

            if (attribute.Pattern != null)
            {
                XsltStreamabilityClassification pattern = AnalyzeXsltStreamabilityPattern(attribute.Pattern);
                if (!pattern.Guaranteed)
                {
                    FailXsltStreamability(result, pattern.Reason);
                }
            }

            index = index + 1;
        }

        if (instruction.Kind == XsltInstructionKind.ApplyTemplates && FindXsltStreamabilityAttribute(instruction, "select") == null)
        {
            MergeXsltStreamability(result, CreateXsltStreamabilityClassification(XsltPosture.Striding, XsltSweep.Consuming, ""), false);
        }

        if (content != null)
        {
            MergeXsltStreamability(result, AnalyzeXsltStreamabilityConstructor(content, instruction.Kind == XsltInstructionKind.Fork), instruction.Kind == XsltInstructionKind.Fork);
        }

        ApplyXsltStreamabilityInstructionRules(result, instruction);
        Instructions.Add(instruction, result);
        Entries.Add(new XsltStreamabilityEntry("instruction", instruction.LocalName, instruction, instruction.Instruction.Range, result, false));
        return result;
    }

    internal void ApplyXsltStreamabilityInstructionRules(XsltStreamabilityClassification result, XsltInstructionPlan instruction)
    {
        XsltInstructionKind kind = instruction.Kind;
        if (kind == XsltInstructionKind.Evaluate || kind == XsltInstructionKind.PerformSort || kind == XsltInstructionKind.Sort || kind == XsltInstructionKind.Unknown)
        {
            FailXsltStreamability(result, "xsl:" + instruction.LocalName + " is not guaranteed streamable.");
        }
        else if (kind == XsltInstructionKind.ForEachGroup && FindXsltStreamabilityAttribute(instruction, "group-adjacent") == null)
        {
            FailXsltStreamability(result, "Only adjacent grouping is guaranteed streamable.");
        }
        else if (kind == XsltInstructionKind.Merge && !HasXsltStreamableMergeSources(instruction))
        {
            FailXsltStreamability(result, "Every merge source must be declared streamable and already sorted.");
        }
        else if (kind == XsltInstructionKind.Number)
        {
            XsltInstructionAttributePlan? level = FindXsltStreamabilityAttribute(instruction, "level");
            if (level != null && level.Source.Trim() == "any")
            {
                FailXsltStreamability(result, "xsl:number level='any' is free-ranging.");
            }
        }
        else if (kind == XsltInstructionKind.SourceDocument)
        {
            XsltInstructionAttributePlan? streamable = FindXsltStreamabilityAttribute(instruction, "streamable");
            if (streamable != null && IsXsltStreamabilityYes(streamable.Source))
            {
                result.Sweep = XsltSweep.Consuming;
                if (result.ConsumingOperandCount == 0)
                {
                    result.ConsumingOperandCount = 1;
                }
            }
        }
        else if (kind == XsltInstructionKind.CallTemplate)
        {
            XsltTemplatePlan? target = FindXsltStreamabilityTemplate(instruction);
            if (target != null && Stylesheet != null)
            {
                MergeXsltStreamability(result, AnalyzeXsltStreamabilityTemplate(target, Stylesheet.Modes), false);
            }
        }

        if (kind == XsltInstructionKind.Attribute || kind == XsltInstructionKind.Comment || kind == XsltInstructionKind.Copy || kind == XsltInstructionKind.CopyOf || kind == XsltInstructionKind.Document || kind == XsltInstructionKind.Element || kind == XsltInstructionKind.Namespace || kind == XsltInstructionKind.ProcessingInstruction || kind == XsltInstructionKind.ResultDocument || kind == XsltInstructionKind.Text || kind == XsltInstructionKind.ValueOf)
        {
            result.Posture = XsltPosture.Grounded;
        }

        result.Guaranteed = result.Posture != XsltPosture.Roaming && result.Sweep != XsltSweep.FreeRanging;
    }

    internal XsltInstructionAttributePlan? FindXsltStreamabilityAttribute(XsltInstructionPlan instruction, string name)
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

    internal bool HasXsltStreamableMergeSources(XsltInstructionPlan instruction)
    {
        XmlNode[] children = instruction.ContentNodes;
        bool found = false;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            XmlName? name = child.Name;
            if (name != null && name.NamespaceUri == XsltNamespaces.Xslt && name.LocalName == "merge-source")
            {
                found = true;
                string? streamable = FindXsltNodeAttributeValue(child, "streamable");
                string? sorted = FindXsltNodeAttributeValue(child, "sort-before-merge");
                if (streamable == null || !IsXsltStreamabilityYes(streamable) || sorted != null && IsXsltStreamabilityYes(sorted))
                {
                    return false;
                }
            }

            index = index + 1;
        }

        return found;
    }

    internal string? FindXsltNodeAttributeValue(XmlNode node, string localName)
    {
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlName? name = attributes[index].Name;
            if (name != null && (name.NamespaceUri == null || name.NamespaceUri.Length == 0) && name.LocalName == localName)
            {
                return attributes[index].Value;
            }

            index = index + 1;
        }

        return null;
    }

    internal bool IsXsltStreamabilityYes(string value)
    {
        string normalized = value.Trim();
        return normalized == "yes" || normalized == "true" || normalized == "1";
    }

    internal XsltStreamabilityClassification AnalyzeXsltStreamabilityTemplate(XsltTemplatePlan template, XsltModePlan[] modes)
    {
        if (Templates.ContainsKey(template))
        {
            return Templates[template];
        }

        XsltStreamabilityClassification result = CreateXsltStreamabilityClassification(XsltPosture.Grounded, XsltSweep.Motionless, "");
        Templates.Add(template, result);
        CopyXsltStreamabilityValues(result, AnalyzeXsltStreamabilityConstructor(template.Body, false));
        if (template.MatchPlan != null)
        {
            XsltStreamabilityClassification pattern = AnalyzeXsltStreamabilityPattern(template.MatchPlan);
            if (!pattern.Guaranteed)
            {
                FailXsltStreamability(result, pattern.Reason);
            }
        }

        bool required = IsXsltTemplateInStreamableMode(template, modes);
        Entries.Add(new XsltStreamabilityEntry("template", template.Name == null ? template.Match == null ? "#template" : template.Match : template.Name, template, template.Declaration.Range, result, required));
        return result;
    }

    internal bool IsXsltTemplateInStreamableMode(XsltTemplatePlan template, XsltModePlan[] modes)
    {
        if (!template.IsTemplateRule)
        {
            return false;
        }

        int modeIndex = 0;
        while (modeIndex < modes.Length)
        {
            if (modes[modeIndex].Streamable && DoesXsltTemplateUseMode(template, modes[modeIndex].Name))
            {
                return true;
            }

            modeIndex = modeIndex + 1;
        }

        return false;
    }

    internal bool DoesXsltTemplateUseMode(XsltTemplatePlan template, string name)
    {
        int index = 0;
        while (index < template.Modes.Length)
        {
            if (template.Modes[index].IsAll || template.Modes[index].Name == name)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XsltStreamabilityClassification AnalyzeXsltStreamabilityFunction(XsltFunctionPlan function)
    {
        if (Functions.ContainsKey(function))
        {
            return Functions[function];
        }

        XsltStreamabilityClassification result = CreateXsltStreamabilityClassification(XsltPosture.Grounded, XsltSweep.Motionless, "");
        Functions.Add(function, result);
        CopyXsltStreamabilityValues(result, AnalyzeXsltStreamabilityConstructor(function.Body, false));
        string category = function.Streamability;
        bool required = category != "unclassified";
        if (required && !MatchesXsltFunctionStreamabilityCategory(result, category))
        {
            FailXsltStreamability(result, "The function body does not satisfy its " + category + " streamability category.");
        }

        Entries.Add(new XsltStreamabilityEntry("function", function.ExpandedName + "#" + function.Arity, function, function.Declaration.Range, result, required));
        return result;
    }

    internal bool MatchesXsltFunctionStreamabilityCategory(XsltStreamabilityClassification value, string category)
    {
        if (!value.Guaranteed)
        {
            return false;
        }

        if (category == "absorbing") return value.Posture == XsltPosture.Grounded;
        if (category == "inspection") return value.Posture == XsltPosture.Grounded && value.Sweep == XsltSweep.Motionless;
        if (category == "filter") return (value.Posture == XsltPosture.Grounded || value.Posture == XsltPosture.Striding) && value.Sweep == XsltSweep.Motionless;
        if (category == "shallow-descent") return value.Posture == XsltPosture.Striding && value.Sweep == XsltSweep.Consuming;
        if (category == "deep-descent") return (value.Posture == XsltPosture.Striding || value.Posture == XsltPosture.Crawling) && value.Sweep == XsltSweep.Consuming;
        if (category == "ascent") return value.Posture == XsltPosture.Striding && value.Sweep == XsltSweep.Motionless;
        return false;
    }

    internal XsltStreamabilityClassification AnalyzeXsltStreamabilityAccumulator(XsltAccumulatorDeclarationPlan accumulator)
    {
        if (Accumulators.ContainsKey(accumulator))
        {
            return Accumulators[accumulator];
        }

        XsltStreamabilityClassification result = CopyXsltStreamability(AnalyzeXsltStreamabilityExpression(accumulator.InitialValue.Root));
        int index = 0;
        while (index < accumulator.Rules.Length)
        {
            XsltAccumulatorRulePlan rule = accumulator.Rules[index];
            AnalyzeXsltStreamabilityPattern(rule.Match);
            XsltStreamabilityClassification value = rule.Select == null ? rule.Content == null ? CreateXsltStreamabilityClassification(XsltPosture.Grounded, XsltSweep.Motionless, "") : AnalyzeXsltStreamabilityConstructor(rule.Content, false) : AnalyzeXsltStreamabilityExpression(rule.Select.Root);
            SelectWorseXsltStreamability(result, value);
            index = index + 1;
        }

        if (accumulator.Streamable && !accumulator.IsGuaranteedStreamableCandidate)
        {
            FailXsltStreamability(result, "The accumulator initial value, match patterns, and rules must be grounded and motionless.");
        }

        Accumulators.Add(accumulator, result);
        Entries.Add(new XsltStreamabilityEntry("accumulator", accumulator.Name, accumulator, accumulator.Declaration.Range, result, accumulator.Streamable));
        return result;
    }

    internal XsltStreamabilityClassification AnalyzeXsltStreamabilityAttributeSet(XsltAttributeSetPlan set)
    {
        if (AttributeSets.ContainsKey(set))
        {
            return AttributeSets[set];
        }

        XsltStreamabilityClassification result = CreateXsltStreamabilityClassification(XsltPosture.Grounded, XsltSweep.Motionless, "");
        AttributeSets.Add(set, result);
        int index = 0;
        while (index < set.ExpandedAttributes.Length)
        {
            MergeXsltStreamability(result, AnalyzeXsltStreamabilitySequenceItem(set.ExpandedAttributes[index].Item), false);
            index = index + 1;
        }

        if (set.Streamable && result.Sweep != XsltSweep.Motionless)
        {
            FailXsltStreamability(result, "A streamable attribute set must be motionless.");
        }

        Entries.Add(new XsltStreamabilityEntry("attribute-set", set.Name, set, set.Origin.Range, result, set.Streamable));
        return result;
    }

    internal XsltStreamabilityClassification AnalyzeXsltStreamabilityMode(XsltModePlan mode, XsltTemplatePlan[] templates)
    {
        if (Modes.ContainsKey(mode))
        {
            return Modes[mode];
        }

        XsltStreamabilityClassification result = CreateXsltStreamabilityClassification(XsltPosture.Grounded, XsltSweep.Motionless, "");
        Modes.Add(mode, result);
        int index = 0;
        while (index < templates.Length)
        {
            XsltTemplatePlan template = templates[index];
            if (template.IsTemplateRule && DoesXsltTemplateUseMode(template, mode.Name))
            {
                SelectWorseXsltStreamability(result, AnalyzeXsltStreamabilityTemplate(template, [mode]));
            }

            index = index + 1;
        }

        index = 0;
        while (index < mode.Accumulators.Length)
        {
            SelectWorseXsltStreamability(result, AnalyzeXsltStreamabilityAccumulator(mode.Accumulators[index].EffectiveDeclaration));
            index = index + 1;
        }

        if (mode.Streamable && !result.Guaranteed && result.Reason.Length == 0)
        {
            result.Reason = "A template or accumulator used by the mode is not guaranteed streamable.";
        }

        Entries.Add(new XsltStreamabilityEntry("mode", mode.Name, mode, mode.Origin.Range, result, mode.Streamable));
        return result;
    }

    internal void AnalyzeXsltStreamabilityPackageComponents(XsltStylesheetPlan stylesheet)
    {
        XsltPackagePlan? package = stylesheet.Package;
        if (package == null)
        {
            return;
        }

        XsltTemplatePlan[] templates = CollectXsltStreamabilityPackageTemplates(package, stylesheet.Templates);
        int index = 0;
        while (index < package.Components.Length)
        {
            XsltComponent component = package.Components[index];
            if (component.Template != null)
            {
                AnalyzeXsltStreamabilityTemplate(component.Template, stylesheet.Modes);
            }
            else if (component.Function != null)
            {
                AnalyzeXsltStreamabilityFunction(component.Function);
            }
            else if (component.AttributeSet != null)
            {
                AnalyzeXsltStreamabilityAttributeSet(component.AttributeSet);
            }
            else if (component.Mode != null)
            {
                AnalyzeXsltStreamabilityMode(component.Mode, templates);
            }

            index = index + 1;
        }
    }

    internal XsltTemplatePlan[] CollectXsltStreamabilityPackageTemplates(XsltPackagePlan package, XsltTemplatePlan[] localTemplates)
    {
        List<XsltTemplatePlan> result = new List<XsltTemplatePlan>();
        int index = 0;
        while (index < localTemplates.Length)
        {
            result.Add(localTemplates[index]);
            index = index + 1;
        }

        index = 0;
        while (index < package.Components.Length)
        {
            XsltTemplatePlan? template = package.Components[index].Template;
            if (template != null && !result.Contains(template))
            {
                result.Add(template);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XsltTemplatePlan? FindXsltStreamabilityTemplate(XsltInstructionPlan instruction)
    {
        XsltPackagePlan? package = Stylesheet == null ? null : Stylesheet.Package;
        if (package == null)
        {
            return null;
        }

        int componentIndex = 0;
        while (componentIndex < package.Components.Length)
        {
            XsltSymbolicReference[] references = package.Components[componentIndex].References;
            int referenceIndex = 0;
            while (referenceIndex < references.Length)
            {
                XsltSymbolicReference reference = references[referenceIndex];
                if (reference.Kind == XsltComponentKind.Template && object.ReferenceEquals(reference.Node, instruction.Instruction) && reference.Target != null && reference.Target.Template != null)
                {
                    return reference.Target.Template;
                }

                referenceIndex = referenceIndex + 1;
            }

            componentIndex = componentIndex + 1;
        }

        XsltInstructionAttributePlan? name = FindXsltStreamabilityAttribute(instruction, "name");
        if (name == null || name.ResolvedName == null)
        {
            return null;
        }

        string expandedName = "Q{" + (name.ResolvedName.NamespaceUri == null ? "" : name.ResolvedName.NamespaceUri) + "}" + (name.ResolvedName.LocalName == null ? "" : name.ResolvedName.LocalName);
        componentIndex = package.Components.Length - 1;
        while (componentIndex >= 0)
        {
            XsltComponent component = package.Components[componentIndex];
            if (component.Kind == XsltComponentKind.Template && component.Name == expandedName && component.Visibility != XsltComponentVisibility.Hidden && component.Template != null)
            {
                return component.Template;
            }

            componentIndex = componentIndex - 1;
        }

        return null;
    }

    internal XsltFunctionPlan? FindXsltStreamabilityFunction(XPathPlanInstruction instruction)
    {
        XPathFunctionBinding? binding = instruction.FunctionBinding;
        string? key = binding == null ? null : binding.BindingKey;
        if (key == null || !key.StartsWith("xslt-function:", StringComparison.Ordinal))
        {
            return null;
        }

        XsltPackagePlan? package = Stylesheet == null ? null : Stylesheet.Package;
        XsltComponent[] components = package == null ? [] : package.Components;
        int index = components.Length - 1;
        while (index >= 0)
        {
            XsltFunctionPlan? function = components[index].Function;
            if (function != null && function.Binding.BindingKey == key)
            {
                return function;
            }

            index = index - 1;
        }

        XsltFunctionPlan[] functions = Stylesheet == null ? [] : Stylesheet.Functions;
        index = functions.Length - 1;
        while (index >= 0)
        {
            if (functions[index].Binding.BindingKey == key)
            {
                return functions[index];
            }

            index = index - 1;
        }

        return null;
    }

    internal XsltStreamabilityClassification ClassifyXsltStreamabilityFunctionCall(XsltFunctionPlan function)
    {
        string category = function.Streamability;
        if (category == "absorbing") return CreateXsltStreamabilityClassification(XsltPosture.Grounded, XsltSweep.Consuming, "");
        if (category == "inspection") return CreateXsltStreamabilityClassification(XsltPosture.Grounded, XsltSweep.Motionless, "");
        if (category == "filter") return CreateXsltStreamabilityClassification(XsltPosture.Striding, XsltSweep.Motionless, "");
        if (category == "shallow-descent") return CreateXsltStreamabilityClassification(XsltPosture.Striding, XsltSweep.Consuming, "");
        if (category == "deep-descent") return CreateXsltStreamabilityClassification(XsltPosture.Crawling, XsltSweep.Consuming, "");
        if (category == "ascent") return CreateXsltStreamabilityClassification(XsltPosture.Striding, XsltSweep.Motionless, "");
        return CreateXsltStreamabilityClassification(XsltPosture.Roaming, XsltSweep.FreeRanging, "A call to an unclassified stylesheet function is not guaranteed streamable.");
    }

    internal XmlName CreateXsltStreamabilityModeName(string expandedName)
    {
        XmlName result = new XmlName();
        if (expandedName.StartsWith("Q{", StringComparison.Ordinal))
        {
            int close = expandedName.IndexOf('}');
            if (close >= 2)
            {
                result.NamespaceUri = expandedName.Substring(2, close - 2);
                result.LocalName = expandedName.Substring(close + 1);
                result.QualifiedName = result.LocalName;
                return result;
            }
        }

        result.LocalName = expandedName;
        result.QualifiedName = expandedName;
        return result;
    }

    internal void MergeOptionalXsltStreamabilityExpression(XsltStreamabilityClassification target, XPathPlanInstruction? optional, bool allowMultipleConsumption)
    {
        if (optional != null)
        {
            MergeXsltStreamability(target, AnalyzeXsltStreamabilityExpression(optional), allowMultipleConsumption);
        }
    }

    internal void MergeXsltStreamability(XsltStreamabilityClassification target, XsltStreamabilityClassification value, bool allowMultipleConsumption)
    {
        if (value.Posture > target.Posture) target.Posture = value.Posture;
        if (value.Sweep > target.Sweep) target.Sweep = value.Sweep;
        target.ConsumingOperandCount = target.ConsumingOperandCount + value.ConsumingOperandCount;
        if (!value.Guaranteed && target.Reason.Length == 0) target.Reason = value.Reason;
        if (!allowMultipleConsumption && target.ConsumingOperandCount > 1)
        {
            target.Posture = XsltPosture.Roaming;
            target.Sweep = XsltSweep.FreeRanging;
            target.Reason = "The construct has more than one consuming operand.";
        }

        target.Guaranteed = target.Posture != XsltPosture.Roaming && target.Sweep != XsltSweep.FreeRanging;
    }

    internal void SelectWorseXsltStreamability(XsltStreamabilityClassification target, XsltStreamabilityClassification value)
    {
        if (value.Posture > target.Posture) target.Posture = value.Posture;
        if (value.Sweep > target.Sweep) target.Sweep = value.Sweep;
        if (value.ConsumingOperandCount > target.ConsumingOperandCount) target.ConsumingOperandCount = value.ConsumingOperandCount;
        if (!value.Guaranteed && target.Reason.Length == 0) target.Reason = value.Reason;
        target.Guaranteed = target.Posture != XsltPosture.Roaming && target.Sweep != XsltSweep.FreeRanging;
    }

    internal XsltStreamabilityClassification CreateXsltStreamabilityClassification(XsltPosture posture, XsltSweep sweep, string reason)
    {
        int consuming = sweep == XsltSweep.Consuming ? 1 : 0;
        bool guaranteed = posture != XsltPosture.Roaming && sweep != XsltSweep.FreeRanging;
        return new XsltStreamabilityClassification(posture, sweep, consuming, guaranteed, reason);
    }

    internal XsltStreamabilityClassification CopyXsltStreamability(XsltStreamabilityClassification source)
    {
        return new XsltStreamabilityClassification(source.Posture, source.Sweep, source.ConsumingOperandCount, source.Guaranteed, source.Reason);
    }

    internal void CopyXsltStreamabilityValues(XsltStreamabilityClassification target, XsltStreamabilityClassification source)
    {
        target.Posture = source.Posture;
        target.Sweep = source.Sweep;
        target.ConsumingOperandCount = source.ConsumingOperandCount;
        target.Guaranteed = source.Guaranteed;
        target.Reason = source.Reason;
    }

    internal void FailXsltStreamability(XsltStreamabilityClassification target, string reason)
    {
        target.Posture = XsltPosture.Roaming;
        target.Sweep = XsltSweep.FreeRanging;
        target.Guaranteed = false;
        if (target.Reason.Length == 0)
        {
            target.Reason = reason;
        }
    }
}
