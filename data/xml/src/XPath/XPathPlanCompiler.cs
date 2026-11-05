using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace xml;

internal class XPathPlanCompiler
{
    internal XPathStaticAnalyzer Analyzer;

    internal XPathPlanCompiler(XPathStaticContext context)
    {
        Analyzer = new XPathStaticAnalyzer(context);
    }

    internal XPathPlan CompileXPathSource(string source, string? sourceUri)
    {
        Guard.NotNull(source, "source");
        XPathAstNode expression = Analyzer.AnalyzeXPathSource(source, sourceUri);
        XPathPlanInstruction root = CompileXPathNode(expression);
        return new XPathPlan(source, CopyXPathRange(expression.Range), root, Analyzer.Environment);
    }

    internal XPathPlan CompileXPathExpression(XPathAstNode expression)
    {
        Guard.NotNull(expression, "expression");
        Analyzer.AnalyzeXPathExpression(expression);
        XPathPlanInstruction root = CompileXPathNode(expression);
        return new XPathPlan(null, CopyXPathRange(expression.Range), root, Analyzer.Environment);
    }

    internal XPathPlanInstruction CompileXPathNode(XPathAstNode node)
    {
        if (node.Kind == XPathAstKind.Parenthesized)
        {
            if (node.Operand == null)
            {
                return CreateXPathConstantInstruction(node, []);
            }

            XPathPlanInstruction operand = CompileXPathNode(node.Operand);
            if (!IsXPathAxisPredicateInstruction(operand))
            {
                return operand;
            }

            XPathPlanInstruction parenthesized = CreateXPathInstructionShell(node);
            parenthesized.Operand = operand;
            return parenthesized;
        }

        if (node.Kind == XPathAstKind.Sequence)
        {
            return CompileXPathSequence(node);
        }

        if (node.Kind == XPathAstKind.If)
        {
            return CompileXPathIf(node);
        }

        XPathPlanInstruction result = CreateXPathInstructionShell(node);
        result.Left = CompileXPathOptionalNode(node.Left);
        result.Right = CompileXPathOptionalNode(node.Right);
        result.Operand = CompileXPathOptionalNode(node.Operand);
        result.Condition = CompileXPathOptionalNode(node.Condition);
        result.ThenInstruction = CompileXPathOptionalNode(node.ThenExpression);
        result.ElseInstruction = CompileXPathOptionalNode(node.ElseExpression);
        result.Body = CompileXPathOptionalNode(node.Body);
        result.Children = CompileXPathChildren(node.Children);
        result.Bindings = CompileXPathBindings(node.Bindings);
        result.Parameters = CopyXPathParameters(node.Parameters);
        result.MapEntries = CompileXPathMapEntries(node.MapEntries);
        if (result.Opcode == XPathPlanOpcode.InlineFunction && result.Body != null)
        {
            MarkXPathTailCallPosition(result.Body);
        }

        return FoldXPathInstruction(result);
    }

    internal bool IsXPathAxisPredicateInstruction(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction current = instruction;
        while (current.Opcode == XPathPlanOpcode.Predicate && current.Left != null)
        {
            current = current.Left;
        }

        return current.Opcode == XPathPlanOpcode.Name || current.Opcode == XPathPlanOpcode.ParentStep || current.Opcode == XPathPlanOpcode.AttributeStep || current.Opcode == XPathPlanOpcode.AxisStep;
    }

    internal XPathPlanInstruction CompileXPathSequence(XPathAstNode node)
    {
        List<XPathPlanInstruction> children = new List<XPathPlanInstruction>();
        int index = 0;
        while (index < node.Children.Length)
        {
            XPathPlanInstruction child = CompileXPathNode(node.Children[index]);
            AppendXPathSequenceInstruction(children, child);
            index = index + 1;
        }

        if (children.Count == 0)
        {
            return CreateXPathConstantInstruction(node, []);
        }

        if (children.Count == 1)
        {
            return children[0];
        }

        bool allConstant = true;
        index = 0;
        while (index < children.Count)
        {
            if (children[index].Opcode != XPathPlanOpcode.Constant)
            {
                allConstant = false;
                break;
            }

            index = index + 1;
        }

        if (allConstant)
        {
            List<XPathPlanLiteral> constants = new List<XPathPlanLiteral>();
            index = 0;
            while (index < children.Count)
            {
                AppendXPathConstants(constants, children[index].Constants);
                index = index + 1;
            }

            return CreateXPathConstantInstruction(node, constants.ToArray());
        }

        XPathPlanInstruction result = CreateXPathInstructionShell(node);
        result.Children = children.ToArray();
        return result;
    }

    internal void AppendXPathSequenceInstruction(List<XPathPlanInstruction> result, XPathPlanInstruction instruction)
    {
        if (instruction.Opcode == XPathPlanOpcode.Constant && instruction.Constants.Length == 0)
        {
            return;
        }

        if (instruction.Opcode == XPathPlanOpcode.Sequence)
        {
            int index = 0;
            while (index < instruction.Children.Length)
            {
                AppendXPathSequenceInstruction(result, instruction.Children[index]);
                index = index + 1;
            }

            return;
        }

        result.Add(instruction);
    }

    internal XPathPlanInstruction CompileXPathIf(XPathAstNode node)
    {
        XPathPlanInstruction? condition = CompileXPathOptionalNode(node.Condition);
        if (condition == null)
        {
            throw new InvalidOperationException("An analyzed if expression must have a condition.");
        }

        int effectiveBoolean = ReadXPathConstantEffectiveBoolean(condition);
        if (effectiveBoolean == 1)
        {
            if (node.ThenExpression == null)
            {
                throw new InvalidOperationException("An analyzed if expression must have a then branch.");
            }

            return CompileXPathNode(node.ThenExpression);
        }

        if (effectiveBoolean == 0)
        {
            if (node.ElseExpression == null)
            {
                throw new InvalidOperationException("An analyzed if expression must have an else branch.");
            }

            return CompileXPathNode(node.ElseExpression);
        }

        XPathPlanInstruction result = CreateXPathInstructionShell(node);
        result.Condition = condition;
        result.ThenInstruction = CompileXPathOptionalNode(node.ThenExpression);
        result.ElseInstruction = CompileXPathOptionalNode(node.ElseExpression);
        return result;
    }

    internal XPathPlanInstruction CreateXPathInstructionShell(XPathAstNode node)
    {
        XPathPlanOpcode opcode = (XPathPlanOpcode)(int)node.Kind;
        XPathPlanInstruction result = new XPathPlanInstruction(opcode, CopyXPathRange(node.Range), CopyXPathStaticType(node.StaticType));
        result.Operator = node.Operator;
        result.Value = node.Value;
        result.Prefix = node.Prefix;
        result.LocalName = node.LocalName;
        result.NamespaceUri = node.NamespaceUri;
        result.Type = CopyXPathType(node.Type);
        result.PathOperators = CopyXPathStrings(node.PathOperators);
        result.Absolute = node.Absolute;
        result.LeadingDescendant = node.LeadingDescendant;
        result.Arity = node.Arity;
        result.Axis = node.Axis;
        result.NodeTest = CopyXPathNodeTest(node.NodeTest);
        result.ReverseAxis = node.ReverseAxis;
        result.ResolvedName = CopyXPathName(node.ResolvedName);
        result.VariableBinding = CopyXPathVariableBinding(node.VariableBinding);
        result.FunctionBinding = CopyXPathFunctionBinding(node.FunctionBinding);
        return result;
    }

    internal XPathPlanInstruction? CompileXPathOptionalNode(XPathAstNode? node)
    {
        return node == null ? null : CompileXPathNode(node);
    }

    internal XPathPlanInstruction[] CompileXPathChildren(XPathAstNode[] nodes)
    {
        XPathPlanInstruction[] result = new XPathPlanInstruction[nodes.Length];
        int index = 0;
        while (index < nodes.Length)
        {
            result[index] = CompileXPathNode(nodes[index]);
            index = index + 1;
        }

        return result;
    }

    internal XPathPlanBinding[] CompileXPathBindings(XPathBinding[] bindings)
    {
        XPathPlanBinding[] result = new XPathPlanBinding[bindings.Length];
        int index = 0;
        while (index < bindings.Length)
        {
            XPathBinding binding = bindings[index];
            string localName = binding.LocalName == null ? binding.Name : binding.LocalName;
            result[index] = new XPathPlanBinding(binding.Name, localName, binding.NamespaceUri, CompileXPathNode(binding.Expression), CopyXPathRange(binding.Range));
            index = index + 1;
        }

        return result;
    }

    internal XPathPlanParameter[] CopyXPathParameters(XPathParameter[] parameters)
    {
        XPathPlanParameter[] result = new XPathPlanParameter[parameters.Length];
        int index = 0;
        while (index < parameters.Length)
        {
            XPathParameter parameter = parameters[index];
            string localName = parameter.LocalName == null ? parameter.Name : parameter.LocalName;
            result[index] = new XPathPlanParameter(parameter.Name, localName, parameter.NamespaceUri, CopyXPathType(parameter.Type), CopyXPathRange(parameter.Range));
            index = index + 1;
        }

        return result;
    }

    internal void MarkXPathTailCallPosition(XPathPlanInstruction instruction)
    {
        if (instruction.Opcode == XPathPlanOpcode.StaticFunctionCall || instruction.Opcode == XPathPlanOpcode.DynamicFunctionCall || instruction.Opcode == XPathPlanOpcode.Arrow)
        {
            instruction.TailCall = true;
            return;
        }

        if (instruction.Opcode == XPathPlanOpcode.If)
        {
            if (instruction.ThenInstruction != null)
            {
                MarkXPathTailCallPosition(instruction.ThenInstruction);
            }

            if (instruction.ElseInstruction != null)
            {
                MarkXPathTailCallPosition(instruction.ElseInstruction);
            }

            return;
        }

        if (instruction.Opcode == XPathPlanOpcode.Let && instruction.Body != null)
        {
            MarkXPathTailCallPosition(instruction.Body);
            return;
        }

        if (instruction.Opcode == XPathPlanOpcode.Parenthesized && instruction.Operand != null)
        {
            MarkXPathTailCallPosition(instruction.Operand);
        }
    }

    internal XPathPlanMapEntry[] CompileXPathMapEntries(XPathMapEntry[] entries)
    {
        XPathPlanMapEntry[] result = new XPathPlanMapEntry[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            XPathMapEntry entry = entries[index];
            result[index] = new XPathPlanMapEntry(CompileXPathNode(entry.Key), CompileXPathNode(entry.Value), CopyXPathRange(entry.Range));
            index = index + 1;
        }

        return result;
    }

    internal XPathPlanInstruction FoldXPathInstruction(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? literal = FoldXPathLiteralInstruction(instruction);
        if (literal != null)
        {
            return literal;
        }

        XPathPlanInstruction? unary = FoldXPathUnaryInstruction(instruction);
        if (unary != null)
        {
            return unary;
        }

        XPathPlanInstruction? binary = FoldXPathBinaryInstruction(instruction);
        return binary == null ? instruction : binary;
    }

    internal XPathPlanInstruction? FoldXPathLiteralInstruction(XPathPlanInstruction instruction)
    {
        XPathPlanLiteralKind kind = XPathPlanLiteralKind.String;
        bool literal = true;
        if (instruction.Opcode == XPathPlanOpcode.IntegerLiteral)
        {
            kind = XPathPlanLiteralKind.Integer;
        }
        else if (instruction.Opcode == XPathPlanOpcode.DecimalLiteral)
        {
            kind = XPathPlanLiteralKind.Decimal;
        }
        else if (instruction.Opcode == XPathPlanOpcode.DoubleLiteral)
        {
            kind = XPathPlanLiteralKind.Double;
        }
        else if (instruction.Opcode != XPathPlanOpcode.StringLiteral)
        {
            literal = false;
        }

        if (literal)
        {
            string value = instruction.Value == null ? "" : instruction.Value;
            return CreateXPathConstantFromInstruction(instruction, [new XPathPlanLiteral(kind, value)]);
        }

        if (instruction.Opcode == XPathPlanOpcode.StaticFunctionCall && instruction.Children.Length == 0 && IsXPathStandardFunction(instruction, "true"))
        {
            return CreateXPathBooleanConstant(instruction, true);
        }

        if (instruction.Opcode == XPathPlanOpcode.StaticFunctionCall && instruction.Children.Length == 0 && IsXPathStandardFunction(instruction, "false"))
        {
            return CreateXPathBooleanConstant(instruction, false);
        }

        return null;
    }

    internal XPathPlanInstruction? FoldXPathUnaryInstruction(XPathPlanInstruction instruction)
    {
        if (instruction.Opcode != XPathPlanOpcode.UnaryPlus && instruction.Opcode != XPathPlanOpcode.UnaryMinus)
        {
            return null;
        }

        XPathPlanInstruction? operand = instruction.Operand;
        if (operand == null || operand.Opcode != XPathPlanOpcode.Constant || operand.Constants.Length != 1)
        {
            return null;
        }

        XPathPlanLiteral value = operand.Constants[0];
        if (instruction.Opcode == XPathPlanOpcode.UnaryPlus)
        {
            if (value.Kind == XPathPlanLiteralKind.Integer || value.Kind == XPathPlanLiteralKind.Decimal || value.Kind == XPathPlanLiteralKind.Double)
            {
                return CreateXPathConstantFromInstruction(instruction, [CopyXPathLiteral(value)]);
            }

            return null;
        }

        if (value.Kind != XPathPlanLiteralKind.Integer)
        {
            return null;
        }

        BigInteger integer = BigInteger.Parse(value.Value, CultureInfo.InvariantCulture);
        string result = (-integer).ToString(CultureInfo.InvariantCulture);
        return CreateXPathConstantFromInstruction(instruction, [new XPathPlanLiteral(XPathPlanLiteralKind.Integer, result)]);
    }

    internal XPathPlanInstruction? FoldXPathBinaryInstruction(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? left = instruction.Left;
        XPathPlanInstruction? right = instruction.Right;
        if (left == null || right == null || left.Opcode != XPathPlanOpcode.Constant || right.Opcode != XPathPlanOpcode.Constant)
        {
            return null;
        }

        if (instruction.Opcode == XPathPlanOpcode.Addition || instruction.Opcode == XPathPlanOpcode.Subtraction || instruction.Opcode == XPathPlanOpcode.Multiplication)
        {
            return FoldXPathIntegerArithmetic(instruction, left, right);
        }

        if (instruction.Opcode == XPathPlanOpcode.StringConcatenation)
        {
            return FoldXPathStringConcatenation(instruction, left, right);
        }

        if (instruction.Opcode == XPathPlanOpcode.Or || instruction.Opcode == XPathPlanOpcode.And)
        {
            return FoldXPathBooleanOperation(instruction, left, right);
        }

        if (instruction.Opcode == XPathPlanOpcode.GeneralComparison || instruction.Opcode == XPathPlanOpcode.ValueComparison)
        {
            return FoldXPathComparison(instruction, left, right);
        }

        return null;
    }

    internal XPathPlanInstruction? FoldXPathIntegerArithmetic(XPathPlanInstruction instruction, XPathPlanInstruction left, XPathPlanInstruction right)
    {
        if (left.Constants.Length != 1 || right.Constants.Length != 1 || left.Constants[0].Kind != XPathPlanLiteralKind.Integer || right.Constants[0].Kind != XPathPlanLiteralKind.Integer)
        {
            return null;
        }

        BigInteger first = BigInteger.Parse(left.Constants[0].Value, CultureInfo.InvariantCulture);
        BigInteger second = BigInteger.Parse(right.Constants[0].Value, CultureInfo.InvariantCulture);
        BigInteger value = first + second;
        if (instruction.Opcode == XPathPlanOpcode.Subtraction)
        {
            value = first - second;
        }
        else if (instruction.Opcode == XPathPlanOpcode.Multiplication)
        {
            value = first * second;
        }

        return CreateXPathConstantFromInstruction(instruction, [new XPathPlanLiteral(XPathPlanLiteralKind.Integer, value.ToString(CultureInfo.InvariantCulture))]);
    }

    internal XPathPlanInstruction? FoldXPathStringConcatenation(XPathPlanInstruction instruction, XPathPlanInstruction left, XPathPlanInstruction right)
    {
        if (left.Constants.Length > 1 || right.Constants.Length > 1)
        {
            return null;
        }

        if (left.Constants.Length == 1 && left.Constants[0].Kind != XPathPlanLiteralKind.String || right.Constants.Length == 1 && right.Constants[0].Kind != XPathPlanLiteralKind.String)
        {
            return null;
        }

        string first = left.Constants.Length == 0 ? "" : left.Constants[0].Value;
        string second = right.Constants.Length == 0 ? "" : right.Constants[0].Value;
        return CreateXPathConstantFromInstruction(instruction, [new XPathPlanLiteral(XPathPlanLiteralKind.String, first + second)]);
    }

    internal XPathPlanInstruction? FoldXPathBooleanOperation(XPathPlanInstruction instruction, XPathPlanInstruction left, XPathPlanInstruction right)
    {
        if (!IsXPathSingleBooleanConstant(left) || !IsXPathSingleBooleanConstant(right))
        {
            return null;
        }

        bool first = left.Constants[0].Value == "true";
        bool second = right.Constants[0].Value == "true";
        bool value = instruction.Opcode == XPathPlanOpcode.Or ? first || second : first && second;
        return CreateXPathBooleanConstant(instruction, value);
    }

    internal XPathPlanInstruction? FoldXPathComparison(XPathPlanInstruction instruction, XPathPlanInstruction left, XPathPlanInstruction right)
    {
        if (left.Constants.Length != 1 || right.Constants.Length != 1)
        {
            return null;
        }

        XPathPlanLiteral first = left.Constants[0];
        XPathPlanLiteral second = right.Constants[0];
        if (first.Kind != second.Kind)
        {
            return null;
        }

        int comparison = CompareXPathPlanLiterals(first, second);
        if (comparison == 2)
        {
            return null;
        }

        string operation = instruction.Operator == null ? "" : instruction.Operator;
        bool value = EvaluateXPathComparisonResult(operation, comparison);
        return CreateXPathBooleanConstant(instruction, value);
    }

    internal int CompareXPathPlanLiterals(XPathPlanLiteral first, XPathPlanLiteral second)
    {
        if (first.Kind == XPathPlanLiteralKind.Integer)
        {
            BigInteger left = BigInteger.Parse(first.Value, CultureInfo.InvariantCulture);
            BigInteger right = BigInteger.Parse(second.Value, CultureInfo.InvariantCulture);
            return left.CompareTo(right);
        }

        if (first.Kind == XPathPlanLiteralKind.String && Analyzer.Environment.DefaultCollation == XPathNamespaces.CodepointCollation)
        {
            return new XPathCodepointCollation().CompareXPathCollation(first.Value, second.Value);
        }

        if (first.Kind == XPathPlanLiteralKind.Boolean)
        {
            bool left = first.Value == "true";
            bool right = second.Value == "true";
            if (left == right)
            {
                return 0;
            }

            return left ? 1 : -1;
        }

        return 2;
    }

    internal bool EvaluateXPathComparisonResult(string operation, int comparison)
    {
        if (operation == "=" || operation == "eq")
        {
            return comparison == 0;
        }

        if (operation == "!=" || operation == "ne")
        {
            return comparison != 0;
        }

        if (operation == "<" || operation == "lt")
        {
            return comparison < 0;
        }

        if (operation == "<=" || operation == "le")
        {
            return comparison <= 0;
        }

        if (operation == ">" || operation == "gt")
        {
            return comparison > 0;
        }

        if (operation == ">=" || operation == "ge")
        {
            return comparison >= 0;
        }

        return false;
    }

    internal int ReadXPathConstantEffectiveBoolean(XPathPlanInstruction instruction)
    {
        if (instruction.Opcode != XPathPlanOpcode.Constant)
        {
            return -1;
        }

        if (instruction.Constants.Length == 0)
        {
            return 0;
        }

        if (instruction.Constants.Length != 1)
        {
            return -1;
        }

        XPathPlanLiteral value = instruction.Constants[0];
        if (value.Kind == XPathPlanLiteralKind.Boolean)
        {
            return value.Value == "true" ? 1 : 0;
        }

        if (value.Kind == XPathPlanLiteralKind.String)
        {
            return value.Value.Length == 0 ? 0 : 1;
        }

        if (value.Kind == XPathPlanLiteralKind.Integer)
        {
            BigInteger integer = BigInteger.Parse(value.Value, CultureInfo.InvariantCulture);
            return integer.IsZero ? 0 : 1;
        }

        return -1;
    }

    internal bool IsXPathStandardFunction(XPathPlanInstruction instruction, string localName)
    {
        XmlName? name = instruction.ResolvedName;
        return instruction.FunctionBinding == null && name != null && name.NamespaceUri == XPathNamespaces.Functions && name.LocalName == localName;
    }

    internal bool IsXPathSingleBooleanConstant(XPathPlanInstruction instruction)
    {
        return instruction.Opcode == XPathPlanOpcode.Constant && instruction.Constants.Length == 1 && instruction.Constants[0].Kind == XPathPlanLiteralKind.Boolean;
    }

    internal XPathPlanInstruction CreateXPathBooleanConstant(XPathPlanInstruction source, bool value)
    {
        string text = value ? "true" : "false";
        return CreateXPathConstantFromInstruction(source, [new XPathPlanLiteral(XPathPlanLiteralKind.Boolean, text)]);
    }

    internal XPathPlanInstruction CreateXPathConstantInstruction(XPathAstNode source, XPathPlanLiteral[] values)
    {
        XPathPlanInstruction result = new XPathPlanInstruction(XPathPlanOpcode.Constant, CopyXPathRange(source.Range), CopyXPathStaticType(source.StaticType));
        result.Constants = CopyXPathLiterals(values);
        return result;
    }

    internal XPathPlanInstruction CreateXPathConstantFromInstruction(XPathPlanInstruction source, XPathPlanLiteral[] values)
    {
        XPathPlanInstruction result = new XPathPlanInstruction(XPathPlanOpcode.Constant, CopyXPathRange(source.Range), CopyXPathStaticType(source.StaticType));
        result.Constants = CopyXPathLiterals(values);
        return result;
    }

    internal void AppendXPathConstants(List<XPathPlanLiteral> result, XPathPlanLiteral[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            result.Add(CopyXPathLiteral(values[index]));
            index = index + 1;
        }
    }

    internal XPathPlanLiteral[] CopyXPathLiterals(XPathPlanLiteral[] values)
    {
        XPathPlanLiteral[] result = new XPathPlanLiteral[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = CopyXPathLiteral(values[index]);
            index = index + 1;
        }

        return result;
    }

    internal XPathPlanLiteral CopyXPathLiteral(XPathPlanLiteral value)
    {
        return new XPathPlanLiteral(value.Kind, value.Value);
    }

    internal XPathStaticType CopyXPathStaticType(XPathStaticType? type)
    {
        if (type == null)
        {
            return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrMore);
        }

        XPathStaticType result = new XPathStaticType(type.ItemKind, type.Cardinality);
        result.AtomicType = type.AtomicType;
        result.NodeKind = type.NodeKind;
        result.AnyNode = type.AnyNode;
        return result;
    }

    internal XPathTypeSyntax? CopyXPathType(XPathTypeSyntax? type)
    {
        if (type == null)
        {
            return null;
        }

        XPathTypeSyntax result = new XPathTypeSyntax(type.Name, type.Prefix, type.LocalName, type.NamespaceUri, type.Occurrence, type.EmptySequence, CopyXPathRange(type.Range));
        result.ResolvedName = CopyXPathName(type.ResolvedName);
        result.AtomicType = type.AtomicType;
        result.SchemaType = type.SchemaType;
        result.ParenthesizedTest = type.ParenthesizedTest;
        result.WildcardTest = type.WildcardTest;
        result.ParenthesizedItemType = type.ParenthesizedItemType;
        result.NodeTest = CopyXPathNodeTest(type.NodeTest);
        result.ParameterTypes = CopyXPathTypes(type.ParameterTypes);
        result.ResultType = CopyXPathType(type.ResultType);
        result.KeyType = CopyXPathType(type.KeyType);
        result.ValueType = CopyXPathType(type.ValueType);
        result.MemberType = CopyXPathType(type.MemberType);
        result.InnerType = CopyXPathType(type.InnerType);
        return result;
    }

    internal XPathTypeSyntax[] CopyXPathTypes(XPathTypeSyntax[] types)
    {
        XPathTypeSyntax[] result = new XPathTypeSyntax[types.Length];
        int index = 0;
        while (index < types.Length)
        {
            XPathTypeSyntax? copy = CopyXPathType(types[index]);
            if (copy == null)
            {
                throw new InvalidOperationException("A required XPath type could not be copied.");
            }

            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal XPathNodeTest? CopyXPathNodeTest(XPathNodeTest? test)
    {
        if (test == null)
        {
            return null;
        }

        XPathNodeTest result = new XPathNodeTest(test.Kind, CopyXPathRange(test.Range));
        result.Token = null;
        result.Value = test.Value;
        result.Prefix = test.Prefix;
        result.LocalName = test.LocalName;
        result.NamespaceUri = test.NamespaceUri;
        result.Type = CopyXPathType(test.Type);
        result.InnerTest = CopyXPathNodeTest(test.InnerTest);
        result.Nillable = test.Nillable;
        result.ResolvedName = CopyXPathName(test.ResolvedName);
        result.ResolvedTypeName = CopyXPathName(test.ResolvedTypeName);
        return result;
    }

    internal XPathVariableBinding? CopyXPathVariableBinding(XPathVariableBinding? binding)
    {
        if (binding == null)
        {
            return null;
        }

        return new XdmFunctionOperations().FreezeXPathVariableBinding(binding);
    }

    internal XPathFunctionBinding? CopyXPathFunctionBinding(XPathFunctionBinding? binding)
    {
        if (binding == null)
        {
            return null;
        }

        XPathFunctionBinding result = new XPathFunctionBinding();
        result.Name = CopyXPathName(binding.Name);
        result.Arity = binding.Arity;
        result.ParameterTypes = CopyXPathAtomicTypes(binding.ParameterTypes);
        result.ParameterOccurrences = CopyXPathOccurrences(binding.ParameterOccurrences);
        result.ResultType = binding.ResultType;
        result.ResultOccurrence = binding.ResultOccurrence;
        result.BindingKey = binding.BindingKey;
        result.IsDeterministic = binding.IsDeterministic;
        result.ParameterTypeSyntax = binding.ParameterTypeSyntax;
        result.ResultTypeSyntax = binding.ResultTypeSyntax;
        return result;
    }

    internal XdmAtomicType[]? CopyXPathAtomicTypes(XdmAtomicType[]? values)
    {
        if (values == null)
        {
            return null;
        }

        XdmAtomicType[] result = new XdmAtomicType[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }

    internal XdmOccurrence[]? CopyXPathOccurrences(XdmOccurrence[]? values)
    {
        if (values == null)
        {
            return null;
        }

        XdmOccurrence[] result = new XdmOccurrence[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }

    internal string[] CopyXPathStrings(string[] values)
    {
        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }

    internal XmlName? CopyXPathName(XmlName? name)
    {
        if (name == null)
        {
            return null;
        }

        XmlName result = new XmlName();
        result.Prefix = name.Prefix;
        result.LocalName = name.LocalName;
        result.NamespaceUri = name.NamespaceUri;
        result.QualifiedName = name.QualifiedName;
        return result;
    }

    internal SourceRange CopyXPathRange(SourceRange range)
    {
        SourceRange result = new SourceRange();
        result.Start = CopyXPathLocation(range.Start);
        result.End = CopyXPathLocation(range.End);
        return result;
    }

    internal SourceLocation? CopyXPathLocation(SourceLocation? location)
    {
        if (location == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = location.SourceUri;
        result.CharacterOffset = location.CharacterOffset;
        result.ByteOffset = location.ByteOffset;
        result.Line = location.Line;
        result.Column = location.Column;
        return result;
    }
}
