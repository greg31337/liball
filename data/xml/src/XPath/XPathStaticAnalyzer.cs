namespace xml;

internal class XPathStaticAnalyzer
{
    internal XPathStaticBinder Binder;
    internal XPathStaticEnvironment Environment;

    internal XPathStaticAnalyzer(XPathStaticContext context)
    {
        Binder = new XPathStaticBinder(context);
        Environment = Binder.Environment;
    }

    internal XPathAstNode AnalyzeXPathSource(string source, string? sourceUri)
    {
        XPathAstNode expression = new XPathParser(source, sourceUri).ParseXPathExpression();
        return AnalyzeXPathExpression(expression);
    }

    internal XPathAstNode AnalyzeXPathExpression(XPathAstNode expression)
    {
        Guard.NotNull(expression, "expression");
        Binder.BindXPathExpression(expression);
        AnalyzeXPathNode(expression);
        return expression;
    }

    internal XPathStaticType AnalyzeXPathNode(XPathAstNode node)
    {
        XPathStaticType result = AnalyzeXPathNodeCore(node);
        node.StaticType = result;
        return result;
    }

    internal XPathStaticType AnalyzeXPathNodeCore(XPathAstNode node)
    {
        switch (node.Kind)
        {
            case XPathAstKind.IntegerLiteral:
                return CreateXPathAtomicType(XdmAtomicType.Integer, XPathStaticCardinality.ExactlyOne);
            case XPathAstKind.DecimalLiteral:
                return CreateXPathAtomicType(XdmAtomicType.Decimal, XPathStaticCardinality.ExactlyOne);
            case XPathAstKind.DoubleLiteral:
                return CreateXPathAtomicType(XdmAtomicType.Double, XPathStaticCardinality.ExactlyOne);
            case XPathAstKind.StringLiteral:
                return CreateXPathAtomicType(XdmAtomicType.String, XPathStaticCardinality.ExactlyOne);
            case XPathAstKind.Variable:
                return AnalyzeXPathVariable(node);
            case XPathAstKind.ContextItem:
                return new XPathStaticType(XPathStaticItemKind.AnyItem, XPathStaticCardinality.ExactlyOne);
            case XPathAstKind.Name:
            case XPathAstKind.ParentStep:
            case XPathAstKind.AttributeStep:
            case XPathAstKind.AxisStep:
                return AnalyzeXPathAxisStep(node);
            case XPathAstKind.Parenthesized:
                return node.Operand == null ? new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.Empty) : AnalyzeXPathNode(node.Operand);
            case XPathAstKind.Sequence:
                return AnalyzeXPathSequence(node);
            case XPathAstKind.For:
            case XPathAstKind.Let:
                return AnalyzeXPathBindingExpression(node);
            case XPathAstKind.Quantified:
                AnalyzeXPathBindingsAndBody(node);
                return CreateXPathAtomicType(XdmAtomicType.Boolean, XPathStaticCardinality.ExactlyOne);
            case XPathAstKind.If:
                return AnalyzeXPathIf(node);
            case XPathAstKind.Or:
            case XPathAstKind.And:
                AnalyzeXPathEffectiveBooleanBinary(node);
                return CreateXPathAtomicType(XdmAtomicType.Boolean, XPathStaticCardinality.ExactlyOne);
            case XPathAstKind.GeneralComparison:
                return AnalyzeXPathComparison(node, false, false);
            case XPathAstKind.ValueComparison:
                return AnalyzeXPathComparison(node, false, true);
            case XPathAstKind.NodeComparison:
                return AnalyzeXPathComparison(node, true, true);
            case XPathAstKind.StringConcatenation:
                AnalyzeXPathStringConcatenation(node);
                return CreateXPathAtomicType(XdmAtomicType.String, XPathStaticCardinality.ExactlyOne);
            case XPathAstKind.Range:
                AnalyzeXPathRange(node);
                return CreateXPathAtomicType(XdmAtomicType.Integer, XPathStaticCardinality.ZeroOrMore);
            case XPathAstKind.Addition:
            case XPathAstKind.Subtraction:
            case XPathAstKind.Multiplication:
            case XPathAstKind.Division:
            case XPathAstKind.IntegerDivision:
            case XPathAstKind.Modulo:
                return AnalyzeXPathArithmetic(node);
            case XPathAstKind.Union:
            case XPathAstKind.Intersect:
            case XPathAstKind.Except:
                return AnalyzeXPathNodeSetOperation(node);
            case XPathAstKind.InstanceOf:
                AnalyzeXPathOptionalOperandAndType(node);
                return CreateXPathAtomicType(XdmAtomicType.Boolean, XPathStaticCardinality.ExactlyOne);
            case XPathAstKind.TreatAs:
                AnalyzeXPathOptionalOperandAndType(node);
                return CreateXPathTypeFromSyntax(node.Type);
            case XPathAstKind.CastableAs:
                AnalyzeXPathCast(node);
                return CreateXPathAtomicType(XdmAtomicType.Boolean, XPathStaticCardinality.ExactlyOne);
            case XPathAstKind.CastAs:
                AnalyzeXPathCast(node);
                return CreateXPathTypeFromSyntax(node.Type);
            case XPathAstKind.Arrow:
                return AnalyzeXPathArrow(node);
            case XPathAstKind.UnaryPlus:
            case XPathAstKind.UnaryMinus:
                return AnalyzeXPathUnaryArithmetic(node);
            case XPathAstKind.SimpleMap:
                return AnalyzeXPathSimpleMap(node);
            case XPathAstKind.Path:
                return AnalyzeXPathPath(node);
            case XPathAstKind.StaticFunctionCall:
                return AnalyzeXPathStaticFunction(node, node.Children, 0, null);
            case XPathAstKind.NamedFunctionReference:
                RequireXPathHigherOrderFunctions(node.Range);
                ValidateXPathFunctionExists(node, node.Arity);
                return new XPathStaticType(XPathStaticItemKind.Function, XPathStaticCardinality.ExactlyOne);
            case XPathAstKind.InlineFunction:
                return AnalyzeXPathInlineFunction(node);
            case XPathAstKind.DynamicFunctionCall:
                return AnalyzeXPathDynamicFunctionCall(node);
            case XPathAstKind.Predicate:
                return AnalyzeXPathPredicate(node);
            case XPathAstKind.Lookup:
                return AnalyzeXPathLookup(node, false);
            case XPathAstKind.MapConstructor:
                return AnalyzeXPathMapConstructor(node);
            case XPathAstKind.SquareArrayConstructor:
                RequireXPathHigherOrderFunctions(node.Range);
                AnalyzeXPathChildren(node.Children);
                return new XPathStaticType(XPathStaticItemKind.Array, XPathStaticCardinality.ExactlyOne);
            case XPathAstKind.CurlyArrayConstructor:
                RequireXPathHigherOrderFunctions(node.Range);
                AnalyzeXPathOptionalNode(node.Body);
                return new XPathStaticType(XPathStaticItemKind.Array, XPathStaticCardinality.ExactlyOne);
            case XPathAstKind.ArgumentPlaceholder:
                return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ExactlyOne);
            case XPathAstKind.UnaryLookup:
                return AnalyzeXPathLookup(node, true);
            case XPathAstKind.Wildcard:
                return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ExactlyOne);
            default:
                throw CreateXPathStaticAnalysisError("The XPath AST contains an unsupported expression kind.", XPathErrorCodes.XPST0003, node.Range);
        }
    }

    internal XPathStaticType AnalyzeXPathVariable(XPathAstNode node)
    {
        if (node.LocalBinding != null)
        {
            XPathStaticType? type = node.LocalBinding.Expression.StaticType;
            XPathStaticType result = type == null ? new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrMore) : CopyXPathStaticType(type);
            if (node.LocalBinding.SingletonValue)
            {
                result.Cardinality = XPathStaticCardinality.ExactlyOne;
            }

            return result;
        }

        if (node.ParameterBinding != null)
        {
            return CreateXPathTypeFromSyntax(node.ParameterBinding.Type);
        }

        XPathVariableBinding? binding = node.VariableBinding;
        if (binding == null)
        {
            throw CreateXPathStaticAnalysisError("The variable is not declared in the static context.", XPathErrorCodes.XPST0008, node.Range);
        }

        if (binding.StaticTypeSyntax != null)
        {
            return CreateXPathTypeFromSyntax(binding.StaticTypeSyntax);
        }

        return CreateXPathAtomicType(binding.DeclaredType, ConvertXPathOccurrence(binding.DeclaredOccurrence));
    }

    internal XPathStaticType AnalyzeXPathAxisStep(XPathAstNode node)
    {
        if (node.NodeTest == null)
        {
            return new XPathStaticType(XPathStaticItemKind.Node, XPathStaticCardinality.ZeroOrMore);
        }

        ValidateXPathNodeTest(node.NodeTest);
        XPathStaticCardinality cardinality = XPathStaticCardinality.ZeroOrMore;
        if (node.Axis == XPathAxis.Self || node.Axis == XPathAxis.Parent)
        {
            cardinality = XPathStaticCardinality.ZeroOrOne;
        }

        XPathStaticType result = new XPathStaticType(XPathStaticItemKind.Node, cardinality);
        result.AnyNode = node.NodeTest.Kind == XPathNodeTestKind.AnyKind || node.NodeTest.Kind == XPathNodeTestKind.Name || node.NodeTest.Kind == XPathNodeTestKind.Wildcard || node.NodeTest.Kind == XPathNodeTestKind.NamespaceWildcard || node.NodeTest.Kind == XPathNodeTestKind.LocalNameWildcard || node.NodeTest.Kind == XPathNodeTestKind.UriWildcard;
        result.NodeKind = SelectXPathNodeKind(node.NodeTest, node.Axis);
        return result;
    }

    internal XPathStaticType AnalyzeXPathSequence(XPathAstNode node)
    {
        XPathStaticType result = new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.Empty);
        int index = 0;
        while (index < node.Children.Length)
        {
            result = ConcatenateXPathTypes(result, AnalyzeXPathNode(node.Children[index]));
            index = index + 1;
        }

        return result;
    }

    internal XPathStaticType AnalyzeXPathBindingExpression(XPathAstNode node)
    {
        AnalyzeXPathBindingsAndBody(node);
        XPathStaticType body = node.Body == null || node.Body.StaticType == null ? new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrMore) : CopyXPathStaticType(node.Body.StaticType);
        if (node.Kind == XPathAstKind.For)
        {
            int index = 0;
            while (index < node.Bindings.Length)
            {
                XPathStaticType? bindingType = node.Bindings[index].Expression.StaticType;
                if (bindingType != null && MayXPathTypeBeEmpty(bindingType))
                {
                    body.Cardinality = XPathStaticCardinality.ZeroOrMore;
                }
                else if (body.Cardinality == XPathStaticCardinality.ExactlyOne)
                {
                    body.Cardinality = XPathStaticCardinality.OneOrMore;
                }

                index = index + 1;
            }
        }

        return body;
    }

    internal void AnalyzeXPathBindingsAndBody(XPathAstNode node)
    {
        int index = 0;
        while (index < node.Bindings.Length)
        {
            AnalyzeXPathNode(node.Bindings[index].Expression);
            index = index + 1;
        }

        AnalyzeXPathOptionalNode(node.Body);
    }

    internal XPathStaticType AnalyzeXPathIf(XPathAstNode node)
    {
        XPathStaticType condition = AnalyzeXPathRequiredNode(node.Condition, node.Range);
        ValidateXPathEffectiveBooleanValue(condition, node.Condition == null ? node.Range : node.Condition.Range);
        XPathStaticType thenType = AnalyzeXPathRequiredNode(node.ThenExpression, node.Range);
        XPathStaticType elseType = AnalyzeXPathRequiredNode(node.ElseExpression, node.Range);
        return JoinXPathTypes(thenType, elseType);
    }

    internal void AnalyzeXPathEffectiveBooleanBinary(XPathAstNode node)
    {
        XPathStaticType left = AnalyzeXPathRequiredNode(node.Left, node.Range);
        XPathStaticType right = AnalyzeXPathRequiredNode(node.Right, node.Range);
        ValidateXPathEffectiveBooleanValue(left, node.Left == null ? node.Range : node.Left.Range);
        ValidateXPathEffectiveBooleanValue(right, node.Right == null ? node.Range : node.Right.Range);
    }

    internal XPathStaticType AnalyzeXPathComparison(XPathAstNode node, bool nodesOnly, bool emptyResult)
    {
        XPathStaticType left = AnalyzeXPathRequiredNode(node.Left, node.Range);
        XPathStaticType right = AnalyzeXPathRequiredNode(node.Right, node.Range);
        if (nodesOnly)
        {
            RequireXPathNodeCompatible(left, node.Left == null ? node.Range : node.Left.Range, XPathErrorCodes.XPTY0004);
            RequireXPathNodeCompatible(right, node.Right == null ? node.Range : node.Right.Range, XPathErrorCodes.XPTY0004);
        }
        else
        {
            RequireXPathAtomizable(left, node.Left == null ? node.Range : node.Left.Range);
            RequireXPathAtomizable(right, node.Right == null ? node.Range : node.Right.Range);
        }

        XPathStaticCardinality cardinality = emptyResult && (MayXPathTypeBeEmpty(left) || MayXPathTypeBeEmpty(right)) ? XPathStaticCardinality.ZeroOrOne : XPathStaticCardinality.ExactlyOne;
        return CreateXPathAtomicType(XdmAtomicType.Boolean, cardinality);
    }

    internal void AnalyzeXPathStringConcatenation(XPathAstNode node)
    {
        XPathStaticType left = AnalyzeXPathRequiredNode(node.Left, node.Range);
        XPathStaticType right = AnalyzeXPathRequiredNode(node.Right, node.Range);
        RequireXPathAtomizable(left, node.Left == null ? node.Range : node.Left.Range);
        RequireXPathAtomizable(right, node.Right == null ? node.Range : node.Right.Range);
    }

    internal void AnalyzeXPathRange(XPathAstNode node)
    {
        XPathStaticType left = AnalyzeXPathRequiredNode(node.Left, node.Range);
        XPathStaticType right = AnalyzeXPathRequiredNode(node.Right, node.Range);
        RequireXPathIntegerCompatible(left, node.Left == null ? node.Range : node.Left.Range);
        RequireXPathIntegerCompatible(right, node.Right == null ? node.Range : node.Right.Range);
    }

    internal XPathStaticType AnalyzeXPathArithmetic(XPathAstNode node)
    {
        XPathStaticType left = AnalyzeXPathRequiredNode(node.Left, node.Range);
        XPathStaticType right = AnalyzeXPathRequiredNode(node.Right, node.Range);
        if (Environment.CompatibilityMode == XPathCompatibilityMode.BackwardsCompatible)
        {
            RequireXPathNumericCompatible(left, node.Left == null ? node.Range : node.Left.Range);
            RequireXPathNumericCompatible(right, node.Right == null ? node.Range : node.Right.Range);
            XdmAtomicType compatibilityType = node.Kind == XPathAstKind.IntegerDivision ? XdmAtomicType.Integer : XdmAtomicType.Double;
            return CreateXPathAtomicType(compatibilityType, XPathStaticCardinality.ExactlyOne);
        }

        bool numeric = IsXPathStaticNumericCompatible(left) && IsXPathStaticNumericCompatible(right);
        if (!numeric)
        {
            XPathStaticType? temporal = InferXPathTemporalArithmeticType(node.Kind, left, right);
            if (temporal != null)
            {
                temporal.Cardinality = MayXPathTypeBeEmpty(left) || MayXPathTypeBeEmpty(right) ? XPathStaticCardinality.ZeroOrOne : XPathStaticCardinality.ExactlyOne;
                return temporal;
            }

            if (IsXPathStaticUnknownAtomic(left) || IsXPathStaticUnknownAtomic(right))
            {
                RequireXPathAtomizable(left, node.Left == null ? node.Range : node.Left.Range);
                RequireXPathAtomizable(right, node.Right == null ? node.Range : node.Right.Range);
                return new XPathStaticType(XPathStaticItemKind.Atomic, MayXPathTypeBeEmpty(left) || MayXPathTypeBeEmpty(right) ? XPathStaticCardinality.ZeroOrOne : XPathStaticCardinality.ExactlyOne);
            }

            throw CreateXPathStaticAnalysisError("The arithmetic operands have an unsupported type combination.", XPathErrorCodes.XPTY0004, node.Range);
        }

        RequireXPathNumericCompatible(left, node.Left == null ? node.Range : node.Left.Range);
        RequireXPathNumericCompatible(right, node.Right == null ? node.Range : node.Right.Range);
        XdmAtomicType type = node.Kind == XPathAstKind.IntegerDivision ? XdmAtomicType.Integer : PromoteXPathNumericType(left, right);
        if (node.Kind == XPathAstKind.Division && type == XdmAtomicType.Integer)
        {
            type = XdmAtomicType.Decimal;
        }
        XPathStaticCardinality cardinality = MayXPathTypeBeEmpty(left) || MayXPathTypeBeEmpty(right) ? XPathStaticCardinality.ZeroOrOne : XPathStaticCardinality.ExactlyOne;
        return CreateXPathAtomicType(type, cardinality);
    }

    internal bool IsXPathStaticNumericCompatible(XPathStaticType type)
    {
        return type.ItemKind != XPathStaticItemKind.Atomic || type.AtomicType == XdmAtomicType.AnyAtomicType || type.AtomicType == XdmAtomicType.UntypedAtomic || Environment.Types.IsXdmNumericAtomicType(type.AtomicType);
    }

    internal bool IsXPathStaticUnknownAtomic(XPathStaticType type)
    {
        return type.ItemKind == XPathStaticItemKind.Unknown || type.ItemKind == XPathStaticItemKind.AnyItem || type.ItemKind == XPathStaticItemKind.Atomic && type.AtomicType == XdmAtomicType.AnyAtomicType;
    }

    internal XPathStaticType? InferXPathTemporalArithmeticType(XPathAstKind operation, XPathStaticType left, XPathStaticType right)
    {
        if (left.ItemKind != XPathStaticItemKind.Atomic || right.ItemKind != XPathStaticItemKind.Atomic)
        {
            return null;
        }

        XdmAtomicType leftType = left.AtomicType;
        XdmAtomicType rightType = right.AtomicType;
        bool leftDuration = leftType == XdmAtomicType.YearMonthDuration || leftType == XdmAtomicType.DayTimeDuration;
        bool rightDuration = rightType == XdmAtomicType.YearMonthDuration || rightType == XdmAtomicType.DayTimeDuration;
        bool leftTemporal = IsXPathStaticTemporalType(leftType);
        bool rightTemporal = IsXPathStaticTemporalType(rightType);
        bool leftNumeric = leftType == XdmAtomicType.UntypedAtomic || Environment.Types.IsXdmNumericAtomicType(leftType);
        bool rightNumeric = rightType == XdmAtomicType.UntypedAtomic || Environment.Types.IsXdmNumericAtomicType(rightType);
        if (operation == XPathAstKind.Addition)
        {
            if (leftDuration && rightDuration && leftType == rightType)
            {
                return CreateXPathAtomicType(leftType, XPathStaticCardinality.ExactlyOne);
            }

            if (leftTemporal && rightDuration && IsXPathStaticTemporalDurationPair(leftType, rightType))
            {
                return CreateXPathAtomicType(leftType, XPathStaticCardinality.ExactlyOne);
            }

            if (leftDuration && rightTemporal && IsXPathStaticTemporalDurationPair(rightType, leftType))
            {
                return CreateXPathAtomicType(rightType, XPathStaticCardinality.ExactlyOne);
            }
        }
        else if (operation == XPathAstKind.Subtraction)
        {
            if (leftDuration && rightDuration && leftType == rightType)
            {
                return CreateXPathAtomicType(leftType, XPathStaticCardinality.ExactlyOne);
            }

            if (leftTemporal && rightTemporal && IsXPathStaticComparableTemporalPair(leftType, rightType))
            {
                return CreateXPathAtomicType(XdmAtomicType.DayTimeDuration, XPathStaticCardinality.ExactlyOne);
            }

            if (leftTemporal && rightDuration && IsXPathStaticTemporalDurationPair(leftType, rightType))
            {
                return CreateXPathAtomicType(leftType, XPathStaticCardinality.ExactlyOne);
            }
        }
        else if (operation == XPathAstKind.Multiplication && (leftDuration && rightNumeric || leftNumeric && rightDuration))
        {
            return CreateXPathAtomicType(leftDuration ? leftType : rightType, XPathStaticCardinality.ExactlyOne);
        }
        else if (operation == XPathAstKind.Division)
        {
            if (leftDuration && rightNumeric)
            {
                return CreateXPathAtomicType(leftType, XPathStaticCardinality.ExactlyOne);
            }

            if (leftDuration && rightDuration && leftType == rightType)
            {
                return CreateXPathAtomicType(XdmAtomicType.Decimal, XPathStaticCardinality.ExactlyOne);
            }
        }

        return null;
    }

    internal bool IsXPathStaticTemporalType(XdmAtomicType type)
    {
        return type == XdmAtomicType.DateTime || type == XdmAtomicType.DateTimeStamp || type == XdmAtomicType.Date || type == XdmAtomicType.Time;
    }

    internal bool IsXPathStaticTemporalDurationPair(XdmAtomicType temporal, XdmAtomicType duration)
    {
        if (duration == XdmAtomicType.DayTimeDuration)
        {
            return IsXPathStaticTemporalType(temporal);
        }

        return duration == XdmAtomicType.YearMonthDuration && temporal != XdmAtomicType.Time && IsXPathStaticTemporalType(temporal);
    }

    internal bool IsXPathStaticComparableTemporalPair(XdmAtomicType left, XdmAtomicType right)
    {
        bool dateTimes = (left == XdmAtomicType.DateTime || left == XdmAtomicType.DateTimeStamp) && (right == XdmAtomicType.DateTime || right == XdmAtomicType.DateTimeStamp);
        return dateTimes || left == right && (left == XdmAtomicType.Date || left == XdmAtomicType.Time);
    }

    internal XPathStaticType AnalyzeXPathUnaryArithmetic(XPathAstNode node)
    {
        XPathStaticType operand = AnalyzeXPathRequiredNode(node.Operand, node.Range);
        RequireXPathNumericCompatible(operand, node.Operand == null ? node.Range : node.Operand.Range);
        if (Environment.CompatibilityMode == XPathCompatibilityMode.BackwardsCompatible)
        {
            return CreateXPathAtomicType(XdmAtomicType.Double, XPathStaticCardinality.ExactlyOne);
        }

        return CopyXPathStaticType(operand);
    }

    internal XPathStaticType AnalyzeXPathNodeSetOperation(XPathAstNode node)
    {
        XPathStaticType left = AnalyzeXPathRequiredNode(node.Left, node.Range);
        XPathStaticType right = AnalyzeXPathRequiredNode(node.Right, node.Range);
        RequireXPathNodeCompatible(left, node.Left == null ? node.Range : node.Left.Range, XPathErrorCodes.XPTY0004);
        RequireXPathNodeCompatible(right, node.Right == null ? node.Range : node.Right.Range, XPathErrorCodes.XPTY0004);
        XPathStaticType result = new XPathStaticType(XPathStaticItemKind.Node, XPathStaticCardinality.ZeroOrMore);
        result.AnyNode = true;
        return result;
    }

    internal void AnalyzeXPathOptionalOperandAndType(XPathAstNode node)
    {
        AnalyzeXPathOptionalNode(node.Operand);
        ValidateXPathTypeSyntax(node.Type, XPathErrorCodes.XPST0051, node.Range);
    }

    internal void AnalyzeXPathCast(XPathAstNode node)
    {
        XPathStaticType operand = AnalyzeXPathRequiredNode(node.Operand, node.Range);
        RequireXPathAtomizable(operand, node.Operand == null ? node.Range : node.Operand.Range);
        ValidateXPathTypeSyntax(node.Type, XPathErrorCodes.XPST0051, node.Range);
        if (node.Type != null && node.Type.AtomicType != null && (node.Type.AtomicType.Type == XdmAtomicType.Notation || node.Type.AtomicType.Type == XdmAtomicType.AnyAtomicType))
        {
            throw CreateXPathStaticAnalysisError("The target type is not permitted for cast or castable.", XPathErrorCodes.XPST0080, node.Type.Range);
        }

        if (node.Type != null && node.Type.SchemaType && (node.Type.ResolvedName == null || Environment.SchemaTests == null || !Environment.SchemaTests.IsXPathSchemaCastType(node.Type.ResolvedName)))
        {
            throw CreateXPathStaticAnalysisError("The target type is not an atomic schema type permitted for cast or castable.", XPathErrorCodes.XPST0080, node.Type.Range);
        }
    }

    internal XPathStaticType AnalyzeXPathArrow(XPathAstNode node)
    {
        XPathStaticType input = AnalyzeXPathRequiredNode(node.Left, node.Range);
        AnalyzeXPathChildren(node.Children);
        XPathAstNode? function = node.Right;
        if (function == null)
        {
            throw CreateXPathStaticAnalysisError("An arrow expression has no function specifier.", XPathErrorCodes.XPST0017, node.Range);
        }

        if (function.Kind == XPathAstKind.Name)
        {
            ValidateXPathFunctionExists(function, node.Children.Length + 1);
            XPathFunctionBinding? binding = function.FunctionBinding;
            if (binding != null)
            {
                ValidateXPathFunctionArgument(input, binding, 0, node.Left == null ? node.Range : node.Left.Range);
                int index = 0;
                while (index < node.Children.Length)
                {
                    ValidateXPathFunctionArgument(node.Children[index].StaticType, binding, index + 1, node.Children[index].Range);
                    index = index + 1;
                }

                if (HasXPathPlaceholder(node.Children))
                {
                    RequireXPathHigherOrderFunctions(node.Range);
                    return new XPathStaticType(XPathStaticItemKind.Function, XPathStaticCardinality.ExactlyOne);
                }

                return CreateXPathFunctionResultType(binding);
            }

            return InferXPathBuiltinFunctionResult(function, node.Children.Length + 1, HasXPathPlaceholder(node.Children));
        }

        RequireXPathHigherOrderFunctions(function.Range);
        AnalyzeXPathNode(function);
        if (function.StaticType != null && !IsXPathFunctionLike(function.StaticType))
        {
            throw CreateXPathStaticAnalysisError("The arrow function specifier is not a function item.", XPathErrorCodes.XPTY0004, function.Range);
        }

        return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrMore);
    }

    internal XPathStaticType AnalyzeXPathSimpleMap(XPathAstNode node)
    {
        XPathStaticType left = AnalyzeXPathRequiredNode(node.Left, node.Range);
        XPathStaticType right = AnalyzeXPathRequiredNode(node.Right, node.Range);
        XPathStaticType result = CopyXPathStaticType(right);
        if (MayXPathTypeHaveMany(left) || MayXPathTypeBeEmpty(left))
        {
            result.Cardinality = XPathStaticCardinality.ZeroOrMore;
        }

        return result;
    }

    internal XPathStaticType AnalyzeXPathPath(XPathAstNode node)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            AnalyzeXPathNode(node.Children[index]);
            index = index + 1;
        }

        index = 0;
        while (index < node.Children.Length)
        {
            XPathStaticType? analyzed = node.Children[index].StaticType;
            XPathStaticType step = analyzed == null ? new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrMore) : analyzed;
            if (index + 1 < node.Children.Length && !IsXPathNodeCompatible(step))
            {
                throw CreateXPathStaticAnalysisError("The left operand of a path operator is not a sequence of nodes.", XPathErrorCodes.XPTY0019, node.Children[index].Range);
            }

            index = index + 1;
        }

        XPathStaticType result = new XPathStaticType(XPathStaticItemKind.Node, XPathStaticCardinality.ZeroOrMore);
        result.AnyNode = true;
        if (node.Children.Length > 0)
        {
            XPathStaticType? finalType = node.Children[node.Children.Length - 1].StaticType;
            if (finalType != null && !IsXPathNodeCompatible(finalType))
            {
                result = CopyXPathStaticType(finalType);
                result.Cardinality = XPathStaticCardinality.ZeroOrMore;
            }
        }

        return result;
    }

    internal XPathStaticType AnalyzeXPathStaticFunction(XPathAstNode node, XPathAstNode[] arguments, int argumentOffset, XPathStaticType? leadingArgument)
    {
        AnalyzeXPathChildren(arguments);
        int arity = arguments.Length + argumentOffset;
        ValidateXPathFunctionExists(node, arity);
        XPathFunctionBinding? binding = node.FunctionBinding;
        bool placeholder = HasXPathPlaceholder(arguments);
        if (binding != null)
        {
            if (leadingArgument != null)
            {
                ValidateXPathFunctionArgument(leadingArgument, binding, 0, node.Range);
            }

            int index = 0;
            while (index < arguments.Length)
            {
                ValidateXPathFunctionArgument(arguments[index].StaticType, binding, index + argumentOffset, arguments[index].Range);
                index = index + 1;
            }

            if (placeholder)
            {
                RequireXPathHigherOrderFunctions(node.Range);
                return new XPathStaticType(XPathStaticItemKind.Function, XPathStaticCardinality.ExactlyOne);
            }

            return CreateXPathFunctionResultType(binding);
        }

        return InferXPathBuiltinFunctionResult(node, arity, placeholder);
    }

    internal XPathStaticType AnalyzeXPathInlineFunction(XPathAstNode node)
    {
        RequireXPathHigherOrderFunctions(node.Range);
        int left = 0;
        while (left < node.Parameters.Length)
        {
            ValidateXPathTypeSyntax(node.Parameters[left].Type, XPathErrorCodes.XPST0051, node.Parameters[left].Range);
            int right = left + 1;
            while (right < node.Parameters.Length)
            {
                if (node.Parameters[left].LocalName == node.Parameters[right].LocalName && NormalizeXPathNamespace(node.Parameters[left].NamespaceUri) == NormalizeXPathNamespace(node.Parameters[right].NamespaceUri))
                {
                    throw CreateXPathStaticAnalysisError("An inline function cannot declare the same parameter name more than once.", XPathErrorCodes.XQST0039, node.Parameters[right].Range);
                }

                right = right + 1;
            }

            left = left + 1;
        }

        ValidateXPathTypeSyntax(node.Type, XPathErrorCodes.XPST0051, node.Range);
        XPathStaticType body = node.Body == null ? new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.Empty) : AnalyzeXPathNode(node.Body);
        if (node.Type != null)
        {
            XPathStaticType expected = CreateXPathTypeFromSyntax(node.Type);
            ValidateXPathCoercion(body, expected, node.Body == null ? node.Range : node.Body.Range);
        }

        return new XPathStaticType(XPathStaticItemKind.Function, XPathStaticCardinality.ExactlyOne);
    }

    internal XPathStaticType AnalyzeXPathDynamicFunctionCall(XPathAstNode node)
    {
        RequireXPathHigherOrderFunctions(node.Range);
        XPathStaticType function = AnalyzeXPathRequiredNode(node.Operand, node.Range);
        AnalyzeXPathChildren(node.Children);
        if (!IsXPathFunctionLike(function) && function.ItemKind != XPathStaticItemKind.Unknown && function.ItemKind != XPathStaticItemKind.AnyItem)
        {
            throw CreateXPathStaticAnalysisError("The target of a dynamic call is not a function item.", XPathErrorCodes.XPTY0004, node.Operand == null ? node.Range : node.Operand.Range);
        }

        return HasXPathPlaceholder(node.Children) ? new XPathStaticType(XPathStaticItemKind.Function, XPathStaticCardinality.ExactlyOne) : new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrMore);
    }

    internal XPathStaticType AnalyzeXPathPredicate(XPathAstNode node)
    {
        XPathStaticType operand = AnalyzeXPathRequiredNode(node.Left, node.Range);
        AnalyzeXPathRequiredNode(node.Right, node.Range);
        XPathStaticType result = CopyXPathStaticType(operand);
        if (result.Cardinality == XPathStaticCardinality.ExactlyOne || result.Cardinality == XPathStaticCardinality.OneOrMore)
        {
            result.Cardinality = XPathStaticCardinality.ZeroOrMore;
        }

        return result;
    }

    internal XPathStaticType AnalyzeXPathLookup(XPathAstNode node, bool unary)
    {
        RequireXPathHigherOrderFunctions(node.Range);
        if (!unary)
        {
            XPathStaticType target = AnalyzeXPathRequiredNode(node.Left, node.Range);
            bool definitelyNonempty = target.Cardinality == XPathStaticCardinality.ExactlyOne || target.Cardinality == XPathStaticCardinality.OneOrMore;
            if (definitelyNonempty && target.ItemKind != XPathStaticItemKind.Map && target.ItemKind != XPathStaticItemKind.Array && target.ItemKind != XPathStaticItemKind.Unknown && target.ItemKind != XPathStaticItemKind.AnyItem)
            {
                throw CreateXPathStaticAnalysisError("A lookup target must be a map or array.", XPathErrorCodes.XPTY0004, node.Left == null ? node.Range : node.Left.Range);
            }
        }

        AnalyzeXPathOptionalNode(node.Right);
        return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrMore);
    }

    internal XPathStaticType AnalyzeXPathMapConstructor(XPathAstNode node)
    {
        RequireXPathHigherOrderFunctions(node.Range);
        int index = 0;
        while (index < node.MapEntries.Length)
        {
            XPathStaticType key = AnalyzeXPathNode(node.MapEntries[index].Key);
            RequireXPathAtomizable(key, node.MapEntries[index].Key.Range);
            bool uncertainCardinality = (key.ItemKind == XPathStaticItemKind.Unknown || key.ItemKind == XPathStaticItemKind.AnyItem) && key.Cardinality == XPathStaticCardinality.ZeroOrMore;
            if (key.Cardinality != XPathStaticCardinality.ExactlyOne && key.Cardinality != XPathStaticCardinality.ZeroOrOne && !uncertainCardinality)
            {
                throw CreateXPathStaticAnalysisError("A map constructor key must atomize to exactly one value.", XPathErrorCodes.XPTY0004, node.MapEntries[index].Key.Range);
            }

            AnalyzeXPathNode(node.MapEntries[index].Value);
            index = index + 1;
        }

        return new XPathStaticType(XPathStaticItemKind.Map, XPathStaticCardinality.ExactlyOne);
    }

    internal void ValidateXPathNodeTest(XPathNodeTest test)
    {
        if (test.Type != null && !IsXPathBuiltInNodeAnnotationType(test.Type))
        {
            ValidateXPathTypeSyntax(test.Type, XPathErrorCodes.XPST0008, test.Type.Range);
        }

        if (test.Kind == XPathNodeTestKind.SchemaElement || test.Kind == XPathNodeTestKind.SchemaAttribute)
        {
            bool found = false;
            if (Environment.SchemaAware && Environment.SchemaTests != null && test.ResolvedName != null)
            {
                found = test.Kind == XPathNodeTestKind.SchemaElement ? Environment.SchemaTests.HasXPathSchemaElementDeclaration(test.ResolvedName) : Environment.SchemaTests.HasXPathSchemaAttributeDeclaration(test.ResolvedName);
            }

            if (!found)
            {
                throw CreateXPathStaticAnalysisError("The schema node test does not identify an in-scope declaration.", XPathErrorCodes.XPST0008, test.Range);
            }
        }

        if (test.InnerTest != null)
        {
            ValidateXPathNodeTest(test.InnerTest);
        }
    }

    internal bool IsXPathBuiltInNodeAnnotationType(XPathTypeSyntax type)
    {
        XmlName? name = type.ResolvedName;
        return name != null && name.NamespaceUri == XPathNamespaces.XmlSchema && (name.LocalName == "untyped" || name.LocalName == "anyType" || name.LocalName == "anySimpleType");
    }

    internal void ValidateXPathTypeSyntax(XPathTypeSyntax? type, string missingCode, SourceRange fallback)
    {
        if (type == null)
        {
            return;
        }

        if (type.InnerType != null)
        {
            ValidateXPathTypeSyntax(type.InnerType, missingCode, fallback);
            return;
        }

        int index = 0;
        while (index < type.ParameterTypes.Length)
        {
            ValidateXPathTypeSyntax(type.ParameterTypes[index], missingCode, fallback);
            index = index + 1;
        }

        ValidateXPathTypeSyntax(type.ResultType, missingCode, fallback);
        ValidateXPathTypeSyntax(type.KeyType, missingCode, fallback);
        ValidateXPathTypeSyntax(type.ValueType, missingCode, fallback);
        ValidateXPathTypeSyntax(type.MemberType, missingCode, fallback);
        if (type.NodeTest != null)
        {
            ValidateXPathNodeTest(type.NodeTest);
        }

        if (Environment.IsXPathStructuralType(type))
        {
            return;
        }

        if (type.AtomicType == null && !type.SchemaType && !new XPathFunctionTypeConverter().IsXPathNumericTypeSyntax(type))
        {
            throw CreateXPathStaticAnalysisError("The sequence type name is not present in the in-scope schema types.", missingCode, type.Range);
        }

        if (type.SchemaType && (type.ResolvedName == null || Environment.SchemaTests == null || !Environment.SchemaTests.IsXPathSchemaSequenceType(type.ResolvedName)))
        {
            throw CreateXPathStaticAnalysisError("The sequence type name does not identify an atomic or union schema type.", missingCode, type.Range);
        }
    }

    internal void ValidateXPathFunctionExists(XPathAstNode node, int arity)
    {
        XmlName? name = node.ResolvedName;
        if (name != null && (name.NamespaceUri == XPathNamespaces.Map || name.NamespaceUri == XPathNamespaces.Array))
        {
            RequireXPathHigherOrderFunctions(node.Range);
        }

        if (name != null && NormalizeXPathNamespace(name.NamespaceUri) == XPathNamespaces.Functions && IsXPathHigherOrderSequenceFunction(name.LocalName, arity))
        {
            RequireXPathHigherOrderFunctions(node.Range);
        }

        if (node.FunctionBinding != null || IsKnownXPathBuiltinFunction(node.ResolvedName, arity) || IsXPathConstructorFunction(node.ResolvedName, arity))
        {
            return;
        }

        throw CreateXPathStaticAnalysisError("No function with the requested expanded name and arity is present in the static context.", XPathErrorCodes.XPST0017, node.Range);
    }

    internal bool IsKnownXPathBuiltinFunction(XmlName? name, int arity)
    {
        if (name == null || name.LocalName == null)
        {
            return false;
        }

        string? namespaceUri = NormalizeXPathNamespace(name.NamespaceUri);
        string local = name.LocalName;
        if (namespaceUri == XPathNamespaces.Map)
        {
            return IsKnownXPathMapFunction(local, arity);
        }

        if (namespaceUri == XPathNamespaces.Array)
        {
            return IsKnownXPathArrayFunction(local, arity);
        }

        if (namespaceUri == XPathNamespaces.Math)
        {
            if (local == "pi")
            {
                return arity == 0;
            }

            if (local == "pow" || local == "atan2")
            {
                return arity == 2;
            }

            return arity == 1 && (local == "exp" || local == "exp10" || local == "log" || local == "log10" || local == "sqrt" || local == "sin" || local == "cos" || local == "tan" || local == "asin" || local == "acos" || local == "atan");
        }

        if (namespaceUri != XPathNamespaces.Functions)
        {
            return false;
        }

        if (local == "concat")
        {
            return arity >= 2;
        }

        if (local == "string" || local == "number" || local == "name" || local == "local-name" || local == "namespace-uri" || local == "node-name" || local == "base-uri" || local == "document-uri" || local == "root" || local == "path" || local == "has-children" || local == "generate-id" || local == "nilled" || local == "data")
        {
            return arity == 0 || arity == 1;
        }

        if (local == "lang" || local == "id" || local == "element-with-id" || local == "idref")
        {
            return arity == 1 || arity == 2;
        }

        if (local == "innermost" || local == "outermost")
        {
            return arity == 1;
        }

        if (local == "string-length" || local == "normalize-space")
        {
            return arity == 0 || arity == 1;
        }

        if (local == "string-join" || local == "normalize-unicode" || local == "collation-key")
        {
            return arity == 1 || arity == 2;
        }

        if (local == "substring" || local == "compare" || local == "contains" || local == "starts-with" || local == "ends-with" || local == "substring-before" || local == "substring-after" || local == "matches" || local == "tokenize")
        {
            return arity == 2 || arity == 3 || (local == "tokenize" && arity == 1);
        }

        if (local == "resolve-uri")
        {
            return arity == 1 || arity == 2;
        }

        if (local == "sum" || local == "min" || local == "max" || local == "round" || local == "round-half-to-even")
        {
            return arity == 1 || arity == 2;
        }

        if (local == "replace")
        {
            return arity == 3 || arity == 4;
        }

        if (local == "analyze-string")
        {
            return arity == 2 || arity == 3;
        }

        if (local == "codepoint-equal")
        {
            return arity == 2;
        }

        if (local == "contains-token")
        {
            return arity == 2 || arity == 3;
        }

        if (local == "translate")
        {
            return arity == 3;
        }

        if (local == "trace")
        {
            return arity == 1 || arity == 2;
        }

        if (local == "error")
        {
            return arity >= 0 && arity <= 3;
        }

        if (local == "parse-xml" || local == "parse-xml-fragment" || local == "parse-ietf-date" || local == "transform")
        {
            return arity == 1;
        }

        if (local == "serialize" || local == "load-xquery-module")
        {
            return arity == 1 || arity == 2;
        }

        if (local == "random-number-generator")
        {
            return arity == 0 || arity == 1;
        }

        if (local == "collection" || local == "uri-collection")
        {
            return arity == 0 || arity == 1;
        }

        if (local == "unparsed-text" || local == "unparsed-text-lines" || local == "unparsed-text-available")
        {
            return arity == 1 || arity == 2;
        }

        if (local == "parse-json" || local == "json-doc" || local == "json-to-xml" || local == "xml-to-json")
        {
            return arity == 1 || arity == 2;
        }

        if (local == "QName" || local == "resolve-QName" || local == "namespace-uri-for-prefix")
        {
            return arity == 2;
        }

        if (local == "index-of" || local == "subsequence" || local == "deep-equal")
        {
            return arity == 2 || arity == 3;
        }

        if (local == "insert-before")
        {
            return arity == 3;
        }

        if (local == "remove")
        {
            return arity == 2;
        }

        if (local == "sort")
        {
            return arity == 1 || arity == 2 || arity == 3;
        }

        if (local == "for-each" || local == "filter")
        {
            return arity == 2;
        }

        if (local == "function-lookup" || local == "apply")
        {
            return arity == 2;
        }

        if (local == "fold-left" || local == "fold-right" || local == "for-each-pair")
        {
            return arity == 3;
        }

        if (local == "distinct-values")
        {
            return arity == 1 || arity == 2;
        }

        if (IsKnownXPathDateTimeFunction(local, arity))
        {
            return true;
        }

        if ((local == "format-integer" || local == "format-number") && (arity == 2 || arity == 3))
        {
            return true;
        }

        if (local == "position" || local == "last" || local == "true" || local == "false" || local == "current-dateTime" || local == "current-date" || local == "current-time" || local == "implicit-timezone" || local == "default-collation" || local == "default-language" || local == "static-base-uri" || local == "available-environment-variables")
        {
            return arity == 0;
        }

        return arity == 1 && IsKnownXPathUnaryFunction(local);
    }

    internal bool IsKnownXPathDateTimeFunction(string local, int arity)
    {
        if (local == "parse-ietf-date")
        {
            return arity == 1;
        }

        if (local == "years-from-duration" || local == "months-from-duration" || local == "days-from-duration" || local == "hours-from-duration" || local == "minutes-from-duration" || local == "seconds-from-duration" || local == "year-from-dateTime" || local == "month-from-dateTime" || local == "day-from-dateTime" || local == "hours-from-dateTime" || local == "minutes-from-dateTime" || local == "seconds-from-dateTime" || local == "timezone-from-dateTime" || local == "year-from-date" || local == "month-from-date" || local == "day-from-date" || local == "timezone-from-date" || local == "hours-from-time" || local == "minutes-from-time" || local == "seconds-from-time" || local == "timezone-from-time")
        {
            return arity == 1;
        }

        if (local == "dateTime")
        {
            return arity == 2;
        }

        if (local == "adjust-dateTime-to-timezone" || local == "adjust-date-to-timezone" || local == "adjust-time-to-timezone")
        {
            return arity == 1 || arity == 2;
        }

        return (local == "format-dateTime" || local == "format-date" || local == "format-time") && (arity == 2 || arity == 5);
    }

    internal bool IsXPathHigherOrderSequenceFunction(string? local, int arity)
    {
        if (local == "for-each" || local == "filter")
        {
            return arity == 2;
        }

        if (local == "fold-left" || local == "fold-right" || local == "for-each-pair")
        {
            return arity == 3;
        }

        return local == "sort" && arity == 3 || (local == "function-name" || local == "function-arity") && arity == 1;
    }

    internal bool IsKnownXPathUnaryFunction(string local)
    {
        return local == "boolean" || local == "not" || local == "count" || local == "empty" || local == "exists" || local == "head" || local == "tail" || local == "exactly-one" || local == "zero-or-one" || local == "one-or-more" || local == "string-length" || local == "normalize-space" || local == "upper-case" || local == "lower-case" || local == "string-to-codepoints" || local == "codepoints-to-string" || local == "encode-for-uri" || local == "iri-to-uri" || local == "escape-html-uri" || local == "prefix-from-QName" || local == "local-name-from-QName" || local == "namespace-uri-from-QName" || local == "in-scope-prefixes" || local == "data" || local == "document-uri" || local == "nilled" || local == "has-children" || local == "reverse" || local == "distinct-values" || local == "unordered" || local == "abs" || local == "ceiling" || local == "floor" || local == "round" || local == "round-half-to-even" || local == "avg" || local == "doc" || local == "doc-available" || local == "collection" || local == "uri-collection" || local == "parse-xml" || local == "parse-xml-fragment" || local == "parse-ietf-date" || local == "parse-json" || local == "json-doc" || local == "json-to-xml" || local == "xml-to-json" || local == "serialize" || local == "transform" || local == "load-xquery-module" || local == "random-number-generator" || local == "function-name" || local == "function-arity" || local == "environment-variable";
    }

    internal bool IsKnownXPathMapFunction(string local, int arity)
    {
        if (local == "size" || local == "keys")
        {
            return arity == 1;
        }

        if (local == "contains" || local == "get" || local == "entry" || local == "remove" || local == "for-each" || local == "find")
        {
            return arity == 2;
        }

        return (local == "put" && arity == 3) || (local == "merge" && (arity == 1 || arity == 2));
    }

    internal bool IsKnownXPathArrayFunction(string local, int arity)
    {
        if (local == "size" || local == "head" || local == "tail" || local == "reverse" || local == "join" || local == "flatten")
        {
            return arity == 1;
        }

        if (local == "get" || local == "append" || local == "remove" || local == "for-each" || local == "filter")
        {
            return arity == 2;
        }

        if (local == "subarray")
        {
            return arity == 2 || arity == 3;
        }

        if (local == "put" || local == "insert-before" || local == "fold-left" || local == "fold-right" || local == "for-each-pair")
        {
            return arity == 3;
        }

        return local == "sort" && (arity == 1 || arity == 2 || arity == 3);
    }

    internal bool IsXPathConstructorFunction(XmlName? name, int arity)
    {
        if (arity != 1 || name == null)
        {
            return false;
        }

        if (name.NamespaceUri == XPathNamespaces.XmlSchema && name.LocalName == "numeric")
        {
            return true;
        }

        if (IsXPathListConstructorFunction(name))
        {
            return true;
        }

        XdmAtomicTypeDefinition? builtIn = Environment.Types.FindXdmAtomicTypeDefinition(name);
        if (builtIn != null)
        {
            return !builtIn.IsAbstract && builtIn.Type != XdmAtomicType.Notation;
        }

        return Environment.SchemaAware && Environment.SchemaTests != null && Environment.SchemaTests.IsXPathSchemaAtomicType(name);
    }

    internal bool IsXPathListConstructorFunction(XmlName name)
    {
        return name.NamespaceUri == XPathNamespaces.XmlSchema && (name.LocalName == "IDREFS" || name.LocalName == "NMTOKENS" || name.LocalName == "ENTITIES");
    }

    internal XPathStaticType InferXPathBuiltinFunctionResult(XPathAstNode node, int arity, bool placeholder)
    {
        if (placeholder)
        {
            RequireXPathHigherOrderFunctions(node.Range);
            return new XPathStaticType(XPathStaticItemKind.Function, XPathStaticCardinality.ExactlyOne);
        }

        XmlName? name = node.ResolvedName;
        if (name != null && NormalizeXPathNamespace(name.NamespaceUri) == XPathNamespaces.Array)
        {
            string localName = name.LocalName == null ? "" : name.LocalName;
            if (localName == "size")
            {
                return CreateXPathAtomicType(XdmAtomicType.Integer, XPathStaticCardinality.ExactlyOne);
            }

            if (localName == "get" || localName == "head" || localName == "fold-left" || localName == "fold-right" || localName == "flatten")
            {
                return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrMore);
            }

            return new XPathStaticType(XPathStaticItemKind.Array, XPathStaticCardinality.ExactlyOne);
        }

        if (name != null && NormalizeXPathNamespace(name.NamespaceUri) == XPathNamespaces.Map)
        {
            string localName = name.LocalName == null ? "" : name.LocalName;
            if (localName == "size")
            {
                return CreateXPathAtomicType(XdmAtomicType.Integer, XPathStaticCardinality.ExactlyOne);
            }

            if (localName == "contains")
            {
                return CreateXPathAtomicType(XdmAtomicType.Boolean, XPathStaticCardinality.ExactlyOne);
            }

            if (localName == "keys")
            {
                return new XPathStaticType(XPathStaticItemKind.Atomic, XPathStaticCardinality.ZeroOrMore);
            }

            if (localName == "find")
            {
                return new XPathStaticType(XPathStaticItemKind.Array, XPathStaticCardinality.ExactlyOne);
            }

            if (localName == "merge" || localName == "put" || localName == "entry" || localName == "remove")
            {
                return new XPathStaticType(XPathStaticItemKind.Map, XPathStaticCardinality.ExactlyOne);
            }

            return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrMore);
        }

        if (name != null && NormalizeXPathNamespace(name.NamespaceUri) == XPathNamespaces.Math)
        {
            XPathStaticCardinality occurrence = name.LocalName == "pi" || name.LocalName == "atan2" ? XPathStaticCardinality.ExactlyOne : XPathStaticCardinality.ZeroOrOne;
            return CreateXPathAtomicType(XdmAtomicType.Double, occurrence);
        }

        if (IsXPathConstructorFunction(name, arity) && name != null)
        {
            if (name.NamespaceUri == XPathNamespaces.XmlSchema && name.LocalName == "numeric")
            {
                return CreateXPathAtomicType(XdmAtomicType.Double, XPathStaticCardinality.ZeroOrOne);
            }

            if (IsXPathListConstructorFunction(name))
            {
                XdmAtomicType itemType = name.LocalName == "IDREFS" ? XdmAtomicType.Idref : name.LocalName == "NMTOKENS" ? XdmAtomicType.Nmtoken : XdmAtomicType.Entity;
                return CreateXPathAtomicType(itemType, XPathStaticCardinality.ZeroOrMore);
            }

            XdmAtomicTypeDefinition? definition = Environment.Types.FindXdmAtomicTypeDefinition(name);
            return definition == null ? new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrOne) : CreateXPathAtomicType(definition.Type, XPathStaticCardinality.ZeroOrOne);
        }

        string local = name == null || name.LocalName == null ? "" : name.LocalName;
        if (local == "function-lookup")
        {
            return new XPathStaticType(XPathStaticItemKind.Function, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "doc")
        {
            XPathStaticType document = new XPathStaticType(XPathStaticItemKind.Node, XPathStaticCardinality.ZeroOrOne);
            document.NodeKind = XdmNodeKind.Document;
            return document;
        }

        if (local == "collection")
        {
            return new XPathStaticType(XPathStaticItemKind.AnyItem, XPathStaticCardinality.ZeroOrMore);
        }

        if (local == "uri-collection")
        {
            return CreateXPathAtomicType(XdmAtomicType.AnyUri, XPathStaticCardinality.ZeroOrMore);
        }

        if (local == "unparsed-text")
        {
            return CreateXPathAtomicType(XdmAtomicType.String, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "unparsed-text-lines")
        {
            return CreateXPathAtomicType(XdmAtomicType.String, XPathStaticCardinality.ZeroOrMore);
        }

        if (local == "environment-variable")
        {
            return CreateXPathAtomicType(XdmAtomicType.String, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "parse-json" || local == "json-doc")
        {
            return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "json-to-xml")
        {
            XPathStaticType document = new XPathStaticType(XPathStaticItemKind.Node, XPathStaticCardinality.ZeroOrOne);
            document.NodeKind = XdmNodeKind.Document;
            return document;
        }

        if (local == "xml-to-json")
        {
            return CreateXPathAtomicType(XdmAtomicType.String, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "parse-xml" || local == "parse-xml-fragment")
        {
            XPathStaticType document = new XPathStaticType(XPathStaticItemKind.Node, XPathStaticCardinality.ZeroOrOne);
            document.NodeKind = XdmNodeKind.Document;
            return document;
        }

        if (local == "parse-ietf-date")
        {
            return CreateXPathAtomicType(XdmAtomicType.DateTime, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "random-number-generator" || local == "transform" || local == "load-xquery-module")
        {
            return new XPathStaticType(XPathStaticItemKind.Map, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "error")
        {
            return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrMore);
        }

        if (local == "available-environment-variables")
        {
            return CreateXPathAtomicType(XdmAtomicType.String, XPathStaticCardinality.ZeroOrMore);
        }

        if (local == "default-language")
        {
            return CreateXPathAtomicType(XdmAtomicType.Language, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "function-name")
        {
            return CreateXPathAtomicType(XdmAtomicType.QName, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "apply")
        {
            return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrMore);
        }

        if (local == "true" || local == "false" || local == "boolean" || local == "not" || local == "empty" || local == "exists" || local == "contains" || local == "starts-with" || local == "ends-with" || local == "contains-token" || local == "matches" || local == "doc-available" || local == "has-children" || local == "lang" || local == "deep-equal")
        {
            return CreateXPathAtomicType(XdmAtomicType.Boolean, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "position" || local == "last" || local == "count" || local == "string-length" || local == "function-arity" || local == "size")
        {
            return CreateXPathAtomicType(XdmAtomicType.Integer, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "index-of")
        {
            return CreateXPathAtomicType(XdmAtomicType.Integer, XPathStaticCardinality.ZeroOrMore);
        }

        if (local == "current-dateTime")
        {
            return CreateXPathAtomicType(XdmAtomicType.DateTimeStamp, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "current-date")
        {
            return CreateXPathAtomicType(XdmAtomicType.Date, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "current-time")
        {
            return CreateXPathAtomicType(XdmAtomicType.Time, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "implicit-timezone")
        {
            return CreateXPathAtomicType(XdmAtomicType.DayTimeDuration, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "years-from-duration" || local == "months-from-duration" || local == "days-from-duration" || local == "hours-from-duration" || local == "minutes-from-duration" || local == "year-from-dateTime" || local == "month-from-dateTime" || local == "day-from-dateTime" || local == "hours-from-dateTime" || local == "minutes-from-dateTime" || local == "year-from-date" || local == "month-from-date" || local == "day-from-date" || local == "hours-from-time" || local == "minutes-from-time")
        {
            return CreateXPathAtomicType(XdmAtomicType.Integer, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "seconds-from-duration" || local == "seconds-from-dateTime" || local == "seconds-from-time")
        {
            return CreateXPathAtomicType(XdmAtomicType.Decimal, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "timezone-from-dateTime" || local == "timezone-from-date" || local == "timezone-from-time")
        {
            return CreateXPathAtomicType(XdmAtomicType.DayTimeDuration, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "dateTime" || local == "adjust-dateTime-to-timezone")
        {
            return CreateXPathAtomicType(XdmAtomicType.DateTime, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "adjust-date-to-timezone")
        {
            return CreateXPathAtomicType(XdmAtomicType.Date, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "adjust-time-to-timezone")
        {
            return CreateXPathAtomicType(XdmAtomicType.Time, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "format-dateTime" || local == "format-date" || local == "format-time")
        {
            return CreateXPathAtomicType(XdmAtomicType.String, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "format-integer" || local == "format-number")
        {
            return CreateXPathAtomicType(XdmAtomicType.String, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "default-collation")
        {
            return CreateXPathAtomicType(XdmAtomicType.String, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "static-base-uri")
        {
            return CreateXPathAtomicType(XdmAtomicType.AnyUri, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "resolve-uri" || local == "namespace-uri-from-QName" || local == "namespace-uri-for-prefix" || local == "base-uri" || local == "document-uri")
        {
            return CreateXPathAtomicType(XdmAtomicType.AnyUri, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "QName")
        {
            return CreateXPathAtomicType(XdmAtomicType.QName, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "node-name")
        {
            return CreateXPathAtomicType(XdmAtomicType.QName, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "namespace-uri")
        {
            return CreateXPathAtomicType(XdmAtomicType.AnyUri, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "nilled")
        {
            return CreateXPathAtomicType(XdmAtomicType.Boolean, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "resolve-QName")
        {
            return CreateXPathAtomicType(XdmAtomicType.QName, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "prefix-from-QName" || local == "local-name-from-QName")
        {
            return CreateXPathAtomicType(XdmAtomicType.NcName, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "in-scope-prefixes")
        {
            return CreateXPathAtomicType(XdmAtomicType.String, XPathStaticCardinality.ZeroOrMore);
        }

        if (local == "compare" || local == "codepoint-equal")
        {
            return CreateXPathAtomicType(local == "compare" ? XdmAtomicType.Integer : XdmAtomicType.Boolean, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "collation-key")
        {
            return CreateXPathAtomicType(XdmAtomicType.Base64Binary, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "string-to-codepoints")
        {
            return CreateXPathAtomicType(XdmAtomicType.Integer, XPathStaticCardinality.ZeroOrMore);
        }

        if (local == "tokenize")
        {
            return CreateXPathAtomicType(XdmAtomicType.String, XPathStaticCardinality.ZeroOrMore);
        }

        if (local == "analyze-string")
        {
            XPathStaticType result = new XPathStaticType(XPathStaticItemKind.Node, XPathStaticCardinality.ExactlyOne);
            result.NodeKind = XdmNodeKind.Element;
            return result;
        }

        if (local == "root" || local == "innermost" || local == "outermost" || local == "idref" || local == "id" || local == "element-with-id")
        {
            XPathStaticCardinality cardinality = local == "root" ? arity == 0 ? XPathStaticCardinality.ExactlyOne : XPathStaticCardinality.ZeroOrOne : XPathStaticCardinality.ZeroOrMore;
            XPathStaticType result = new XPathStaticType(XPathStaticItemKind.Node, cardinality);
            if (local == "id" || local == "element-with-id")
            {
                result.NodeKind = XdmNodeKind.Element;
            }
            else
            {
                result.AnyNode = true;
            }

            return result;
        }

        if (local == "data")
        {
            return new XPathStaticType(XPathStaticItemKind.Atomic, XPathStaticCardinality.ZeroOrMore);
        }

        if (local == "distinct-values")
        {
            return new XPathStaticType(XPathStaticItemKind.Atomic, XPathStaticCardinality.ZeroOrMore);
        }

        if (local == "insert-before" || local == "remove" || local == "reverse" || local == "subsequence" || local == "unordered" || local == "sort" || local == "for-each" || local == "filter" || local == "fold-left" || local == "fold-right" || local == "for-each-pair")
        {
            return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrMore);
        }

        if (local == "head" || local == "zero-or-one")
        {
            return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "one-or-more")
        {
            return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.OneOrMore);
        }

        if (local == "exactly-one")
        {
            return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "number")
        {
            return CreateXPathAtomicType(XdmAtomicType.Double, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "abs" || local == "ceiling" || local == "floor" || local == "round" || local == "round-half-to-even" || local == "avg" || local == "min" || local == "max")
        {
            return new XPathStaticType(XPathStaticItemKind.Atomic, XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "sum")
        {
            return new XPathStaticType(XPathStaticItemKind.Atomic, arity == 1 ? XPathStaticCardinality.ExactlyOne : XPathStaticCardinality.ZeroOrOne);
        }

        if (local == "string" || local == "name" || local == "local-name" || local == "generate-id" || local == "codepoints-to-string" || local == "concat" || local == "string-join" || local == "substring" || local == "normalize-space" || local == "normalize-unicode" || local == "upper-case" || local == "lower-case" || local == "translate" || local == "substring-before" || local == "substring-after" || local == "replace" || local == "encode-for-uri" || local == "iri-to-uri" || local == "escape-html-uri" || local == "serialize")
        {
            return CreateXPathAtomicType(XdmAtomicType.String, XPathStaticCardinality.ExactlyOne);
        }

        if (local == "path")
        {
            return CreateXPathAtomicType(XdmAtomicType.String, XPathStaticCardinality.ZeroOrOne);
        }

        return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrMore);
    }

    internal void ValidateXPathFunctionArgument(XPathStaticType? actual, XPathFunctionBinding binding, int index, SourceRange range)
    {
        if (actual != null && binding.ParameterTypeSyntax != null && index >= 0 && index < binding.ParameterTypeSyntax.Length)
        {
            ValidateXPathCoercion(actual, CreateXPathTypeFromSyntax(binding.ParameterTypeSyntax[index]), range);
            return;
        }

        if (actual == null || binding.ParameterTypes == null || index < 0 || index >= binding.ParameterTypes.Length)
        {
            return;
        }

        XPathStaticCardinality expectedCardinality = XPathStaticCardinality.ExactlyOne;
        if (binding.ParameterOccurrences != null && index < binding.ParameterOccurrences.Length)
        {
            expectedCardinality = ConvertXPathOccurrence(binding.ParameterOccurrences[index]);
        }

        XPathStaticType expected = CreateXPathAtomicType(binding.ParameterTypes[index], expectedCardinality);
        ValidateXPathCoercion(actual, expected, range);
    }

    internal void ValidateXPathCoercion(XPathStaticType actual, XPathStaticType expected, SourceRange range)
    {
        if (actual.Cardinality == XPathStaticCardinality.Empty && (expected.Cardinality == XPathStaticCardinality.ExactlyOne || expected.Cardinality == XPathStaticCardinality.OneOrMore))
        {
            throw CreateXPathStaticAnalysisError("The supplied expression cannot satisfy the required function cardinality.", XPathErrorCodes.XPTY0004, range);
        }

        if (actual.ItemKind == XPathStaticItemKind.Unknown || actual.ItemKind == XPathStaticItemKind.AnyItem || expected.ItemKind == XPathStaticItemKind.AnyItem)
        {
            return;
        }

        if (actual.ItemKind != expected.ItemKind)
        {
            throw CreateXPathStaticAnalysisError("The supplied expression cannot be converted to the required function item type.", XPathErrorCodes.XPTY0004, range);
        }

        if (actual.ItemKind == XPathStaticItemKind.Atomic && expected.AtomicType != XdmAtomicType.AnyAtomicType && actual.AtomicType != XdmAtomicType.AnyAtomicType && actual.AtomicType != XdmAtomicType.UntypedAtomic && !CanPromoteXPathAtomicType(actual.AtomicType, expected.AtomicType))
        {
            throw CreateXPathStaticAnalysisError("The supplied atomic type cannot be promoted to the required function parameter type.", XPathErrorCodes.XPTY0004, range);
        }

        if (actual.ItemKind == XPathStaticItemKind.Atomic && actual.AtomicType == XdmAtomicType.UntypedAtomic && (expected.AtomicType == XdmAtomicType.QName || expected.AtomicType == XdmAtomicType.Notation))
        {
            throw CreateXPathStaticAnalysisError("Untyped atomic values cannot be converted to namespace-sensitive function parameter types.", XPathErrorCodes.XPTY0117, range);
        }
    }

    internal bool CanPromoteXPathAtomicType(XdmAtomicType actual, XdmAtomicType expected)
    {
        if (actual == expected || Environment.Types.IsXdmAtomicSubtypeOf(actual, expected))
        {
            return true;
        }

        if (expected == XdmAtomicType.Double)
        {
            return Environment.Types.IsXdmNumericAtomicType(actual);
        }

        return expected == XdmAtomicType.Float && Environment.Types.IsXdmAtomicSubtypeOf(actual, XdmAtomicType.Decimal);
    }

    internal XPathStaticType CreateXPathFunctionResultType(XPathFunctionBinding binding)
    {
        if (binding.ResultTypeSyntax != null)
        {
            return CreateXPathTypeFromSyntax(binding.ResultTypeSyntax);
        }

        if (binding.BindingKey != null && binding.BindingKey.StartsWith("xslt-function:", global::System.StringComparison.Ordinal))
        {
            return CreateXPathTypeFromSyntax(null);
        }

        return CreateXPathAtomicType(binding.ResultType, ConvertXPathOccurrence(binding.ResultOccurrence));
    }

    internal XPathStaticType CreateXPathTypeFromSyntax(XPathTypeSyntax? syntax)
    {
        if (syntax == null)
        {
            return new XPathStaticType(XPathStaticItemKind.AnyItem, XPathStaticCardinality.ZeroOrMore);
        }

        XPathStaticCardinality cardinality = ConvertXPathOccurrenceText(syntax.Occurrence);
        if (syntax.EmptySequence)
        {
            return new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.Empty);
        }

        if (syntax.AtomicType != null)
        {
            return CreateXPathAtomicType(syntax.AtomicType.Type, cardinality);
        }

        if (syntax.Name == "node" || syntax.Name == "document-node" || syntax.Name == "element" || syntax.Name == "attribute" || syntax.Name == "text" || syntax.Name == "comment" || syntax.Name == "processing-instruction" || syntax.Name == "namespace-node" || syntax.Name == "schema-element" || syntax.Name == "schema-attribute")
        {
            XPathStaticType result = new XPathStaticType(XPathStaticItemKind.Node, cardinality);
            result.AnyNode = syntax.Name == "node";
            return result;
        }

        if (syntax.Name == "function")
        {
            return new XPathStaticType(XPathStaticItemKind.Function, cardinality);
        }

        if (syntax.Name == "map")
        {
            return new XPathStaticType(XPathStaticItemKind.Map, cardinality);
        }

        if (syntax.Name == "array")
        {
            return new XPathStaticType(XPathStaticItemKind.Array, cardinality);
        }

        return new XPathStaticType(XPathStaticItemKind.AnyItem, cardinality);
    }

    internal XPathStaticType CreateXPathAtomicType(XdmAtomicType type, XPathStaticCardinality cardinality)
    {
        XPathStaticType result = new XPathStaticType(XPathStaticItemKind.Atomic, cardinality);
        result.AtomicType = type;
        return result;
    }

    internal XPathStaticCardinality ConvertXPathOccurrence(XdmOccurrence occurrence)
    {
        if (occurrence == XdmOccurrence.ZeroOrOne)
        {
            return XPathStaticCardinality.ZeroOrOne;
        }

        if (occurrence == XdmOccurrence.ZeroOrMore)
        {
            return XPathStaticCardinality.ZeroOrMore;
        }

        return occurrence == XdmOccurrence.OneOrMore ? XPathStaticCardinality.OneOrMore : XPathStaticCardinality.ExactlyOne;
    }

    internal XPathStaticCardinality ConvertXPathOccurrenceText(string? occurrence)
    {
        if (occurrence == "?")
        {
            return XPathStaticCardinality.ZeroOrOne;
        }

        if (occurrence == "*")
        {
            return XPathStaticCardinality.ZeroOrMore;
        }

        return occurrence == "+" ? XPathStaticCardinality.OneOrMore : XPathStaticCardinality.ExactlyOne;
    }

    internal XPathStaticType ConcatenateXPathTypes(XPathStaticType left, XPathStaticType right)
    {
        if (left.Cardinality == XPathStaticCardinality.Empty)
        {
            return CopyXPathStaticType(right);
        }

        if (right.Cardinality == XPathStaticCardinality.Empty)
        {
            return CopyXPathStaticType(left);
        }

        XPathStaticType result = JoinXPathItemTypes(left, right);
        result.Cardinality = GuaranteesXPathItem(left) && GuaranteesXPathItem(right) ? XPathStaticCardinality.OneOrMore : XPathStaticCardinality.ZeroOrMore;
        return result;
    }

    internal XPathStaticType JoinXPathTypes(XPathStaticType left, XPathStaticType right)
    {
        if (left.Cardinality == XPathStaticCardinality.Empty)
        {
            XPathStaticType optionalRight = CopyXPathStaticType(right);
            optionalRight.Cardinality = MakeXPathOptional(optionalRight.Cardinality);
            return optionalRight;
        }

        if (right.Cardinality == XPathStaticCardinality.Empty)
        {
            XPathStaticType optionalLeft = CopyXPathStaticType(left);
            optionalLeft.Cardinality = MakeXPathOptional(optionalLeft.Cardinality);
            return optionalLeft;
        }

        XPathStaticType result = JoinXPathItemTypes(left, right);
        if (left.Cardinality == right.Cardinality)
        {
            result.Cardinality = left.Cardinality;
        }
        else if (!MayXPathTypeHaveMany(left) && !MayXPathTypeHaveMany(right))
        {
            result.Cardinality = XPathStaticCardinality.ZeroOrOne;
        }
        else
        {
            result.Cardinality = XPathStaticCardinality.ZeroOrMore;
        }

        return result;
    }

    internal XPathStaticType JoinXPathItemTypes(XPathStaticType left, XPathStaticType right)
    {
        if (left.ItemKind != right.ItemKind)
        {
            return new XPathStaticType(XPathStaticItemKind.AnyItem, XPathStaticCardinality.ZeroOrMore);
        }

        XPathStaticType result = CopyXPathStaticType(left);
        if (left.ItemKind == XPathStaticItemKind.Atomic && left.AtomicType != right.AtomicType)
        {
            result.AtomicType = XdmAtomicType.AnyAtomicType;
        }

        if (left.ItemKind == XPathStaticItemKind.Node && (left.AnyNode || right.AnyNode || left.NodeKind != right.NodeKind))
        {
            result.AnyNode = true;
        }

        return result;
    }

    internal XPathStaticType CopyXPathStaticType(XPathStaticType source)
    {
        XPathStaticType result = new XPathStaticType(source.ItemKind, source.Cardinality);
        result.AtomicType = source.AtomicType;
        result.NodeKind = source.NodeKind;
        result.AnyNode = source.AnyNode;
        return result;
    }

    internal XPathStaticCardinality MakeXPathOptional(XPathStaticCardinality cardinality)
    {
        if (cardinality == XPathStaticCardinality.ExactlyOne || cardinality == XPathStaticCardinality.Empty)
        {
            return XPathStaticCardinality.ZeroOrOne;
        }

        return cardinality == XPathStaticCardinality.OneOrMore ? XPathStaticCardinality.ZeroOrMore : cardinality;
    }

    internal XPathStaticType AnalyzeXPathRequiredNode(XPathAstNode? node, SourceRange fallback)
    {
        if (node == null)
        {
            throw CreateXPathStaticAnalysisError("The XPath AST is missing a required operand.", XPathErrorCodes.XPST0003, fallback);
        }

        return AnalyzeXPathNode(node);
    }

    internal void AnalyzeXPathOptionalNode(XPathAstNode? node)
    {
        if (node != null)
        {
            AnalyzeXPathNode(node);
        }
    }

    internal void AnalyzeXPathChildren(XPathAstNode[] children)
    {
        int index = 0;
        while (index < children.Length)
        {
            AnalyzeXPathNode(children[index]);
            index = index + 1;
        }
    }

    internal bool HasXPathPlaceholder(XPathAstNode[] arguments)
    {
        int index = 0;
        while (index < arguments.Length)
        {
            if (arguments[index].Kind == XPathAstKind.ArgumentPlaceholder)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void RequireXPathHigherOrderFunctions(SourceRange range)
    {
        if (!Environment.AllowHigherOrderFunctions && !Environment.HasXPathFeature("higher-order-functions"))
        {
            throw CreateXPathStaticAnalysisError("Higher-order function syntax is disabled in this static context.", XPathErrorCodes.XPST0017, range);
        }
    }

    internal void ValidateXPathEffectiveBooleanValue(XPathStaticType type, SourceRange range)
    {
        if (type.ItemKind == XPathStaticItemKind.Function || type.ItemKind == XPathStaticItemKind.Map || type.ItemKind == XPathStaticItemKind.Array)
        {
            throw CreateXPathStaticAnalysisError("A function item, map, or array has no effective boolean value.", XPathErrorCodes.FORG0006, range);
        }
    }

    internal void RequireXPathAtomizable(XPathStaticType type, SourceRange range)
    {
        if (type.ItemKind == XPathStaticItemKind.Function || type.ItemKind == XPathStaticItemKind.Map)
        {
            throw CreateXPathStaticAnalysisError("The expression is not atomizable.", XPathErrorCodes.FOTY0013, range);
        }
    }

    internal void RequireXPathNodeCompatible(XPathStaticType type, SourceRange range, string code)
    {
        if (!IsXPathNodeCompatible(type))
        {
            throw CreateXPathStaticAnalysisError("The expression is statically known not to contain nodes.", code, range);
        }
    }

    internal bool IsXPathNodeCompatible(XPathStaticType type)
    {
        return type.ItemKind == XPathStaticItemKind.Node || type.ItemKind == XPathStaticItemKind.Unknown || type.ItemKind == XPathStaticItemKind.AnyItem || type.Cardinality == XPathStaticCardinality.Empty;
    }

    internal bool IsXPathFunctionLike(XPathStaticType type)
    {
        return type.ItemKind == XPathStaticItemKind.Function || type.ItemKind == XPathStaticItemKind.Map || type.ItemKind == XPathStaticItemKind.Array || type.ItemKind == XPathStaticItemKind.Unknown || type.ItemKind == XPathStaticItemKind.AnyItem;
    }

    internal void RequireXPathNumericCompatible(XPathStaticType type, SourceRange range)
    {
        bool backwardsConvertible = Environment.CompatibilityMode == XPathCompatibilityMode.BackwardsCompatible && (type.AtomicType == XdmAtomicType.Boolean || Environment.Types.IsXdmStringDerivedAtomicType(type.AtomicType));
        if (type.ItemKind == XPathStaticItemKind.Atomic && type.AtomicType != XdmAtomicType.AnyAtomicType && type.AtomicType != XdmAtomicType.UntypedAtomic && !Environment.Types.IsXdmNumericAtomicType(type.AtomicType) && !backwardsConvertible)
        {
            throw CreateXPathStaticAnalysisError("The arithmetic operand is statically known not to be numeric.", XPathErrorCodes.XPTY0004, range);
        }

        RequireXPathAtomizable(type, range);
    }

    internal void RequireXPathIntegerCompatible(XPathStaticType type, SourceRange range)
    {
        if (type.ItemKind == XPathStaticItemKind.Atomic && type.AtomicType != XdmAtomicType.AnyAtomicType && type.AtomicType != XdmAtomicType.UntypedAtomic && !Environment.Types.IsXdmAtomicSubtypeOf(type.AtomicType, XdmAtomicType.Integer))
        {
            throw CreateXPathStaticAnalysisError("A range operand is statically known not to be an integer.", XPathErrorCodes.XPTY0004, range);
        }

        RequireXPathAtomizable(type, range);
    }

    internal XdmAtomicType PromoteXPathNumericType(XPathStaticType left, XPathStaticType right)
    {
        if (left.ItemKind != XPathStaticItemKind.Atomic || right.ItemKind != XPathStaticItemKind.Atomic)
        {
            return XdmAtomicType.AnyAtomicType;
        }

        if (left.AtomicType == XdmAtomicType.Double || right.AtomicType == XdmAtomicType.Double)
        {
            return XdmAtomicType.Double;
        }

        if (left.AtomicType == XdmAtomicType.Float || right.AtomicType == XdmAtomicType.Float)
        {
            return XdmAtomicType.Float;
        }

        if (left.AtomicType == XdmAtomicType.Decimal || right.AtomicType == XdmAtomicType.Decimal || HasXPathUnknownNumericType(left, right))
        {
            return XdmAtomicType.Decimal;
        }

        return XdmAtomicType.Integer;
    }

    internal bool HasXPathUnknownNumericType(XPathStaticType left, XPathStaticType right)
    {
        return left.AtomicType == XdmAtomicType.AnyAtomicType || right.AtomicType == XdmAtomicType.AnyAtomicType;
    }

    internal bool GuaranteesXPathItem(XPathStaticType type)
    {
        return type.Cardinality == XPathStaticCardinality.ExactlyOne || type.Cardinality == XPathStaticCardinality.OneOrMore;
    }

    internal bool MayXPathTypeBeEmpty(XPathStaticType type)
    {
        return type.Cardinality == XPathStaticCardinality.Empty || type.Cardinality == XPathStaticCardinality.ZeroOrOne || type.Cardinality == XPathStaticCardinality.ZeroOrMore;
    }

    internal bool MayXPathTypeHaveMany(XPathStaticType type)
    {
        return type.Cardinality == XPathStaticCardinality.OneOrMore || type.Cardinality == XPathStaticCardinality.ZeroOrMore;
    }

    internal XdmNodeKind SelectXPathNodeKind(XPathNodeTest test, XPathAxis axis)
    {
        if (test.Kind == XPathNodeTestKind.Document)
        {
            return XdmNodeKind.Document;
        }

        if (test.Kind == XPathNodeTestKind.Attribute || test.Kind == XPathNodeTestKind.SchemaAttribute || axis == XPathAxis.Attribute)
        {
            return XdmNodeKind.Attribute;
        }

        if (test.Kind == XPathNodeTestKind.Text)
        {
            return XdmNodeKind.Text;
        }

        if (test.Kind == XPathNodeTestKind.Comment)
        {
            return XdmNodeKind.Comment;
        }

        if (test.Kind == XPathNodeTestKind.NamespaceNode || axis == XPathAxis.Namespace)
        {
            return XdmNodeKind.Namespace;
        }

        if (test.Kind == XPathNodeTestKind.ProcessingInstruction)
        {
            return XdmNodeKind.ProcessingInstruction;
        }

        return XdmNodeKind.Element;
    }

    internal string? NormalizeXPathNamespace(string? namespaceUri)
    {
        return namespaceUri == null || namespaceUri.Length == 0 ? null : namespaceUri;
    }

    internal System.Exception CreateXPathStaticAnalysisError(string message, string code, SourceRange range)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = code.StartsWith("XPTY", System.StringComparison.Ordinal) || code.StartsWith("FOTY", System.StringComparison.Ordinal) ? XmlErrorCategory.XPathType : XmlErrorCategory.XPathStatic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = range;
        if (diagnostic.Category == XmlErrorCategory.XPathType)
        {
            return new XPathTypeException(message, diagnostic, range.Start);
        }

        return new XPathStaticException(message, diagnostic, range.Start);
    }
}
