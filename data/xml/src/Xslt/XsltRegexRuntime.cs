using System;
using System.Collections.Generic;

namespace xml;

internal class XsltRegexSubstring
{
    internal string Value;
    internal bool IsMatch;
    internal string?[] Groups;

    internal XsltRegexSubstring(string value, bool isMatch, string?[] groups)
    {
        Value = value;
        IsMatch = isMatch;
        Groups = groups;
    }
}

internal class XsltRegexRuntime
{
    internal XsltSequenceConstructorRuntime SequencesRuntime;
    internal XsltTemplateRuntime Templates;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmTypeSystem Types;
    internal XdmFactory Factory;

    internal XsltRegexRuntime(XsltSequenceConstructorRuntime sequencesRuntime)
    {
        SequencesRuntime = sequencesRuntime;
        Templates = sequencesRuntime.Templates;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Types = new XdmTypeSystem();
        Factory = new XdmFactory();
    }

    internal XdmSequence EvaluateXsltAnalyzeString(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionPlan instruction = item.Instruction == null ? throw new InvalidOperationException("An analyze-string instruction has no plan.") : item.Instruction;
        if (invocation.Frame == null)
        {
            throw new InvalidOperationException("An analyze-string instruction has no variable frame.");
        }

        XsltInstructionAttributePlan? select = SequencesRuntime.FindXsltAttribute(instruction, "select");
        if (select == null || select.Expression == null)
        {
            throw new InvalidOperationException("An analyze-string instruction has no select expression.");
        }

        XdmSequence selected = Templates.EvaluateXsltInstructionExpression(select.Expression, invocation.Frame);
        string input = ReadXsltAnalyzeStringInput(selected, instruction);
        string pattern = SequencesRuntime.EvaluateXsltAttributeTemplate(SequencesRuntime.FindXsltAttribute(instruction, "regex"), invocation, "");
        string flags = SequencesRuntime.EvaluateXsltAttributeTemplate(SequencesRuntime.FindXsltAttribute(instruction, "flags"), invocation, "");
        XmlSchemaRegexProgram program = CompileXsltAnalyzeStringRegex(pattern, flags, instruction);
        XsltRegexSubstring[] substrings = PartitionXsltAnalyzeString(input, program);
        XsltSequenceItemPlan? matching = FindXsltRegexHelper(item, XsltInstructionKind.MatchingSubstring);
        XsltSequenceItemPlan? nonMatching = FindXsltRegexHelper(item, XsltInstructionKind.NonMatchingSubstring);
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < substrings.Length)
        {
            XsltRegexSubstring substring = substrings[index];
            XsltSequenceItemPlan? helper = substring.IsMatch ? matching : nonMatching;
            if (helper != null && helper.Content != null)
            {
                XsltTemplateInvocation focused = CreateXsltRegexInvocation(invocation, substring, index + 1, substrings.Length);
                result.AppendXdmSequence(SequencesRuntime.EvaluateXsltSequenceConstructor(helper.Content, focused));
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal string ReadXsltAnalyzeStringInput(XdmSequence supplied, XsltInstructionPlan instruction)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count != 1)
        {
            throw CreateXsltRegexTypeError("The select expression of xsl:analyze-string must return one string.", XPathErrorCodes.XPTY0004, instruction.Instruction);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(atomized, 0);
        XdmAtomicValue? value = item.AtomicValue;
        if (value == null || !Types.IsXdmStringDerivedAtomicType(value.Type) && value.Type != XdmAtomicType.UntypedAtomic && value.Type != XdmAtomicType.AnyUri)
        {
            throw CreateXsltRegexTypeError("The select expression of xsl:analyze-string cannot be converted to a string.", XPathErrorCodes.XPTY0004, instruction.Instruction);
        }

        return Converter.ReadXdmAtomicStringValue(value);
    }

    internal XmlSchemaRegexProgram CompileXsltAnalyzeStringRegex(string pattern, string flags, XsltInstructionPlan instruction)
    {
        ValidateXsltAnalyzeStringFlags(flags, instruction);
        XmlSchemaRegexCompilationResult compilation = new XmlSchemaRegexCompiler().CompileXmlSchemaRegex(pattern, flags, true);
        if (!compilation.Success || compilation.Program == null)
        {
            string message = compilation.ErrorMessage == null ? "The regular expression is invalid." : compilation.ErrorMessage;
            throw CreateXsltRegexDynamicError(message, XsltErrorCodes.XTDE1140, instruction.Instruction);
        }

        if (new XmlSchemaRegexMatcher().IsXmlSchemaRegexMatch(compilation.Program, ""))
        {
            throw CreateXsltRegexDynamicError("The regular expression must not match a zero-length string.", XsltErrorCodes.XTDE1150, instruction.Instruction);
        }

        return compilation.Program;
    }

    internal void ValidateXsltAnalyzeStringFlags(string flags, XsltInstructionPlan instruction)
    {
        int index = 0;
        while (index < flags.Length)
        {
            char flag = flags[index];
            if (flag != 's' && flag != 'm' && flag != 'i' && flag != 'x' && flag != 'q')
            {
                throw CreateXsltRegexDynamicError("The regular expression flags contain an invalid flag.", XsltErrorCodes.XTDE1145, instruction.Instruction);
            }

            index = index + 1;
        }
    }

    internal XsltRegexSubstring[] PartitionXsltAnalyzeString(string input, XmlSchemaRegexProgram program)
    {
        List<XsltRegexSubstring> result = new List<XsltRegexSubstring>();
        XmlSchemaRegexMatcher matcher = new XmlSchemaRegexMatcher();
        int offset = 0;
        while (offset < input.Length)
        {
            XmlSchemaRegexMatch match = matcher.FindXmlSchemaRegexMatch(program, input, offset);
            if (!match.Success)
            {
                result.Add(new XsltRegexSubstring(input.Substring(offset), false, []));
                offset = input.Length;
                continue;
            }

            if (match.Index > offset)
            {
                result.Add(new XsltRegexSubstring(input.Substring(offset, match.Index - offset), false, []));
            }

            result.Add(new XsltRegexSubstring(input.Substring(match.Index, match.Length), true, CopyXsltRegexGroups(match.Captures)));
            offset = match.Index + match.Length;
        }

        return result.ToArray();
    }

    internal string?[] CopyXsltRegexGroups(string?[] source)
    {
        string?[] result = new string?[source.Length];
        global::System.Array.Copy(source, result, source.Length);
        return result;
    }

    internal XsltSequenceItemPlan? FindXsltRegexHelper(XsltSequenceItemPlan item, XsltInstructionKind kind)
    {
        XsltSequenceItemPlan[] children = item.Content == null ? [] : item.Content.Items;
        int index = 0;
        while (index < children.Length)
        {
            XsltInstructionPlan? instruction = children[index].Instruction;
            if (instruction != null && instruction.Kind == kind)
            {
                return children[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltTemplateInvocation CreateXsltRegexInvocation(XsltTemplateInvocation invocation, XsltRegexSubstring substring, int position, int size)
    {
        XdmAtomicValue value = Factory.CreateXdmStringAtomicValue(substring.Value, XdmAtomicType.String);
        XdmSequence candidate = Sequences.CreateXdmAtomicSequence(value);
        XPathDynamicContext outer = invocation.Frame == null ? Templates.State.DynamicContext : invocation.Frame.Context;
        XPathDynamicContext context = Templates.CreateXsltTemplateFocusContext(outer, candidate, position, size);
        context.XsltRegexGroups = substring.IsMatch ? CopyXsltRegexGroups(substring.Groups) : null;
        XsltVariableFrame frame = new XsltVariableFrame(invocation.Frame, invocation.Frame == null ? [] : invocation.Frame.TunnelParameters, context);
        return SequencesRuntime.CreateXsltFocusedInvocation(invocation, candidate, frame, position, size);
    }

    internal XsltDynamicException CreateXsltRegexDynamicError(string message, string code, XmlNode node)
    {
        return Templates.CreateXsltTemplateDynamicError(message, code, node);
    }

    internal XsltTypeException CreateXsltRegexTypeError(string message, string code, XmlNode node)
    {
        return Templates.CreateXsltTemplateTypeError(message, code, node);
    }
}
