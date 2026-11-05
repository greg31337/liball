using System;

namespace xml;

internal class XPathFlowEvaluator
{
    internal XPathRuntime Runtime;
    internal XdmSequenceOperations Sequences;

    internal XPathFlowEvaluator(XPathRuntime runtime)
    {
        Runtime = runtime;
        Sequences = new XdmSequenceOperations();
    }

    internal XdmSequence EvaluateXPathFlow(XPathPlanInstruction instruction)
    {
        if (instruction.Opcode == XPathPlanOpcode.For)
        {
            return EvaluateXPathForExpression(instruction);
        }

        if (instruction.Opcode == XPathPlanOpcode.Let)
        {
            return EvaluateXPathLetExpression(instruction);
        }

        if (instruction.Opcode == XPathPlanOpcode.Quantified)
        {
            return EvaluateXPathQuantifiedExpression(instruction);
        }

        if (instruction.Opcode == XPathPlanOpcode.If)
        {
            return EvaluateXPathIfExpression(instruction);
        }

        if (instruction.Opcode == XPathPlanOpcode.SimpleMap)
        {
            return EvaluateXPathSimpleMapExpression(instruction);
        }

        return EvaluateXPathArrowExpression(instruction);
    }

    internal XdmSequence EvaluateXPathForExpression(XPathPlanInstruction instruction)
    {
        if (instruction.Body == null)
        {
            throw new InvalidOperationException("An XPath for expression requires a return expression.");
        }

        XdmSequenceBuilder result = new XdmSequenceBuilder();
        EvaluateXPathForBinding(instruction, 0, result);
        return result.BuildImmutableXdmSequence();
    }

    internal void EvaluateXPathForBinding(XPathPlanInstruction instruction, int bindingIndex, XdmSequenceBuilder result)
    {
        if (bindingIndex == instruction.Bindings.Length)
        {
            XPathPlanInstruction? body = instruction.Body;
            if (body == null)
            {
                throw new InvalidOperationException("An XPath for expression requires a return expression.");
            }

            result.AppendXdmSequence(Runtime.EvaluateXPathInstruction(body));
            return;
        }

        XPathPlanBinding binding = instruction.Bindings[bindingIndex];
        XdmSequence values = Runtime.EvaluateXPathInstruction(binding.Expression);
        Sequences.ValidateXdmSequenceStorage(values);
        int position = 0;
        while (position < values.Count)
        {
            Runtime.Limiter.EnterXPathInstruction(binding.Range);
            Runtime.Variables.PushXPathVariableFrame();
            Runtime.Variables.SetXPathVariable(CreateXPathFlowVariable(binding, CreateXPathFlowSingleton(values, position)));
            EvaluateXPathForBinding(instruction, bindingIndex + 1, result);
            Runtime.Variables.PopXPathVariableFrame();
            Runtime.Limiter.ExitXPathInstruction();
            position = position + 1;
        }
    }

    internal XdmSequence EvaluateXPathLetExpression(XPathPlanInstruction instruction)
    {
        if (instruction.Body == null)
        {
            throw new InvalidOperationException("An XPath let expression requires a return expression.");
        }

        return EvaluateXPathLetBinding(instruction, 0);
    }

    internal XdmSequence EvaluateXPathLetBinding(XPathPlanInstruction instruction, int bindingIndex)
    {
        if (bindingIndex == instruction.Bindings.Length)
        {
            XPathPlanInstruction? body = instruction.Body;
            if (body == null)
            {
                throw new InvalidOperationException("An XPath let expression requires a return expression.");
            }

            return Runtime.EvaluateXPathInstruction(body);
        }

        XPathPlanBinding binding = instruction.Bindings[bindingIndex];
        XdmSequence value = Runtime.EvaluateXPathInstruction(binding.Expression);
        Runtime.Limiter.EnterXPathInstruction(binding.Range);
        Runtime.Variables.PushXPathVariableFrame();
        Runtime.Variables.SetXPathVariable(CreateXPathFlowVariable(binding, value));
        XdmSequence result = EvaluateXPathLetBinding(instruction, bindingIndex + 1);
        Runtime.Variables.PopXPathVariableFrame();
        Runtime.Limiter.ExitXPathInstruction();
        return result;
    }

    internal XdmSequence EvaluateXPathQuantifiedExpression(XPathPlanInstruction instruction)
    {
        if (instruction.Body == null)
        {
            throw new InvalidOperationException("An XPath quantified expression requires a satisfies expression.");
        }

        bool some = instruction.Operator == "some";
        bool value = EvaluateXPathQuantifiedBinding(instruction, 0, some);
        return Runtime.Operators.CreateXPathOperatorBoolean(value);
    }

    internal bool EvaluateXPathQuantifiedBinding(XPathPlanInstruction instruction, int bindingIndex, bool some)
    {
        if (bindingIndex == instruction.Bindings.Length)
        {
            XPathPlanInstruction? body = instruction.Body;
            if (body == null)
            {
                throw new InvalidOperationException("An XPath quantified expression requires a satisfies expression.");
            }

            XdmSequence value = Runtime.EvaluateXPathInstruction(body);
            return Runtime.Operators.EvaluateXPathEffectiveBooleanValue(value, body.Range);
        }

        XPathPlanBinding binding = instruction.Bindings[bindingIndex];
        XdmSequence values = Runtime.EvaluateXPathInstruction(binding.Expression);
        Sequences.ValidateXdmSequenceStorage(values);
        int position = 0;
        while (position < values.Count)
        {
            Runtime.Limiter.EnterXPathInstruction(binding.Range);
            Runtime.Variables.PushXPathVariableFrame();
            Runtime.Variables.SetXPathVariable(CreateXPathFlowVariable(binding, CreateXPathFlowSingleton(values, position)));
            bool value = EvaluateXPathQuantifiedBinding(instruction, bindingIndex + 1, some);
            Runtime.Variables.PopXPathVariableFrame();
            Runtime.Limiter.ExitXPathInstruction();
            if (some && value)
            {
                return true;
            }

            if (!some && !value)
            {
                return false;
            }

            position = position + 1;
        }

        return !some;
    }

    internal XdmSequence EvaluateXPathIfExpression(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? condition = instruction.Condition;
        XPathPlanInstruction? thenInstruction = instruction.ThenInstruction;
        XPathPlanInstruction? elseInstruction = instruction.ElseInstruction;
        if (condition == null || thenInstruction == null || elseInstruction == null)
        {
            throw new InvalidOperationException("An XPath if expression requires a condition and two branches.");
        }

        XdmSequence conditionValue = Runtime.EvaluateXPathInstruction(condition);
        bool selected = Runtime.Operators.EvaluateXPathEffectiveBooleanValue(conditionValue, condition.Range);
        return Runtime.EvaluateXPathInstruction(selected ? thenInstruction : elseInstruction);
    }

    internal XdmSequence EvaluateXPathSimpleMapExpression(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? leftInstruction = instruction.Left;
        XPathPlanInstruction? rightInstruction = instruction.Right;
        if (leftInstruction == null || rightInstruction == null)
        {
            throw new InvalidOperationException("An XPath simple map expression requires two operands.");
        }

        XdmSequence input = Runtime.EvaluateXPathInstruction(leftInstruction);
        Sequences.ValidateXdmSequenceStorage(input);
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int position = 0;
        while (position < input.Count)
        {
            Runtime.Limiter.EnterXPathInstruction(instruction.Range);
            XdmSequence item = CreateXPathFlowSingleton(input, position);
            Runtime.Focus.PushXPathFocus(item, position + 1, input.Count);
            result.AppendXdmSequence(Runtime.EvaluateXPathInstruction(rightInstruction));
            Runtime.Focus.PopXPathFocus();
            Runtime.Limiter.ExitXPathInstruction();
            position = position + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXPathArrowExpression(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? function = instruction.Right;
        XPathPlanInstruction? input = instruction.Left;
        if (function == null || input == null)
        {
            throw new InvalidOperationException("An XPath arrow expression requires an input and function specifier.");
        }

        if (function.Opcode == XPathPlanOpcode.Name)
        {
            return Runtime.EvaluateXPathInstruction(CreateXPathNamedArrowCall(instruction, function, input));
        }

        return Runtime.EvaluateXPathInstruction(CreateXPathDynamicArrowCall(instruction, function, input));
    }

    internal XPathPlanInstruction CreateXPathNamedArrowCall(XPathPlanInstruction arrow, XPathPlanInstruction function, XPathPlanInstruction input)
    {
        XPathPlanInstruction call = new XPathPlanInstruction(XPathPlanOpcode.StaticFunctionCall, arrow.Range, arrow.StaticType);
        call.Operator = function.Operator;
        call.Value = function.Value;
        call.Prefix = function.Prefix;
        call.LocalName = function.LocalName;
        call.NamespaceUri = function.NamespaceUri;
        call.ResolvedName = function.ResolvedName;
        call.FunctionBinding = function.FunctionBinding;
        call.TailCall = arrow.TailCall;
        call.Children = new XPathPlanInstruction[arrow.Children.Length + 1];
        call.Children[0] = input;
        int index = 0;
        while (index < arrow.Children.Length)
        {
            call.Children[index + 1] = arrow.Children[index];
            index = index + 1;
        }

        return call;
    }

    internal XPathPlanInstruction CreateXPathDynamicArrowCall(XPathPlanInstruction arrow, XPathPlanInstruction function, XPathPlanInstruction input)
    {
        XPathPlanInstruction call = new XPathPlanInstruction(XPathPlanOpcode.DynamicFunctionCall, arrow.Range, arrow.StaticType);
        call.Operand = function;
        call.TailCall = arrow.TailCall;
        call.Children = new XPathPlanInstruction[arrow.Children.Length + 1];
        call.Children[0] = input;
        int index = 0;
        while (index < arrow.Children.Length)
        {
            call.Children[index + 1] = arrow.Children[index];
            index = index + 1;
        }

        return call;
    }

    internal XdmSequence CreateXPathFlowSingleton(XdmSequence sequence, int position)
    {
        XdmSequenceItem item = Sequences.ReadValidatedXdmSequenceItem(sequence, position);
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        if (item.Kind == XdmItemKind.Node && item.Node != null)
        {
            builder.AddXdmNode(item.Node);
        }
        else if (item.Kind == XdmItemKind.Atomic && item.AtomicValue != null)
        {
            builder.AddXdmAtomicValue(item.AtomicValue);
        }
        else if (item.Kind == XdmItemKind.Function && item.Function != null)
        {
            builder.AddXdmFunction(item.Function);
        }
        else if (item.Kind == XdmItemKind.Map && item.Map != null)
        {
            builder.AddXdmMap(item.Map);
        }
        else if (item.Kind == XdmItemKind.Array && item.Array != null)
        {
            builder.AddXdmArray(item.Array);
        }
        else
        {
            throw new InvalidOperationException("An XPath sequence item has incomplete storage.");
        }

        return builder.BuildImmutableXdmSequence();
    }

    internal XPathVariableBinding CreateXPathFlowVariable(XPathPlanBinding binding, XdmSequence value)
    {
        XmlName name = new XmlName();
        name.LocalName = binding.LocalName;
        name.NamespaceUri = binding.NamespaceUri;
        name.QualifiedName = binding.Name;
        XPathVariableBinding result = new XPathVariableBinding();
        result.Name = name;
        result.Value = value;
        result.DeclaredType = XdmAtomicType.AnyAtomicType;
        result.DeclaredOccurrence = XdmOccurrence.ZeroOrMore;
        return result;
    }

}
