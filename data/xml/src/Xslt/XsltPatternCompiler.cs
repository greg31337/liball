using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XsltPatternCompiler
{
    internal XsltStylesheetLoader Loader;

    internal XsltPatternCompiler(XsltStylesheetLoader loader)
    {
        Loader = loader;
    }

    internal void CompileXsltPatterns(XsltStylesheetPlan stylesheet)
    {
        int index = 0;
        while (index < stylesheet.Templates.Length)
        {
            XsltTemplatePlan template = stylesheet.Templates[index];
            XsltMatchPatternPlan? pattern = template.MatchPlan;
            if (pattern != null)
            {
                XPathStaticContext context = new XsltVariableCompiler(Loader).CreateXsltVariableXPathContext(template.StaticContext, []);
                AddXsltPatternKeyBindings(context);
                Task<bool> pending = Task.Run(() => CompileXsltPattern(pattern, context, template.Module.BaseUri));
                ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
                if (pending.Status != TaskStatus.RanToCompletion)
                {
                    throw CreateXsltPatternTaskError("The template match pattern could not be compiled.", template.Declaration, template.Module, pending.Exception);
                }
            }

            index = index + 1;
        }
    }

    internal bool CompileXsltPattern(XsltMatchPatternPlan pattern, XPathStaticContext context, string? sourceUri)
    {
        XPathParser parser = new XPathParser(pattern.Source, sourceUri);
        parser.ErrorCode = XsltErrorCodes.XTSE0340;
        XPathAstNode expression = parser.ParseXPathExpression();
        pattern.IsPredicatePattern = IsXsltPredicatePattern(expression);
        if (!pattern.IsPredicatePattern)
        {
            ValidateXsltSelectionPattern(expression);
        }

        XPathStaticAnalyzer analyzer = new XPathStaticAnalyzer(context);
        analyzer.AnalyzeXPathExpression(expression);
        ValidateXsltPatternOuterFunctions(expression);
        pattern.Expression = expression;
        pattern.HasRootedPath = ContainsXsltRootedPattern(expression);
        pattern.HasKeyCall = ContainsXsltKeyCall(expression);
        pattern.IsMotionless = !pattern.HasRootedPath && IsXsltMotionlessPattern(expression, pattern.IsPredicatePattern);
        List<XsltPatternPredicatePlan> predicates = new List<XsltPatternPredicatePlan>();
        CollectXsltPatternPredicatePlans(expression, pattern.Source, context, sourceUri, predicates);
        pattern.Predicates = predicates.ToArray();
        string equivalent = pattern.HasKeyCall ? pattern.Source : pattern.IsPredicatePattern ? pattern.Source : "root(.)//(" + pattern.Source + ")";
        pattern.EquivalentExpression = new XPathPlanCompiler(context).CompileXPathSource(equivalent, sourceUri);

        pattern.IsDeferred = false;
        return true;
    }

    internal void CollectXsltPatternPredicatePlans(XPathAstNode expression, string source, XPathStaticContext context, string? sourceUri, List<XsltPatternPredicatePlan> predicates)
    {
        if (expression.Kind == XPathAstKind.Predicate && expression.Right != null)
        {
            SourceLocation? startLocation = expression.Right.Range.Start;
            SourceLocation? endLocation = expression.Right.Range.End;
            long startOffset = startLocation == null ? 0 : startLocation.CharacterOffset;
            long endOffset = endLocation == null ? source.Length : endLocation.CharacterOffset;
            if (startOffset < 0 || endOffset < startOffset || endOffset > source.Length)
            {
                throw CreateXsltPatternSyntaxError("A pattern predicate has an invalid source range.", expression.Right.Range);
            }

            int start = (int)startOffset;
            int end = (int)endOffset;
            string predicateSource = source.Substring(start, end - start);
            XPathPlan plan = new XPathPlanCompiler(context).CompileXPathSource(predicateSource, sourceUri);
            predicates.Add(new XsltPatternPredicatePlan(expression, plan));
        }

        if (expression.Left != null)
        {
            CollectXsltPatternPredicatePlans(expression.Left, source, context, sourceUri, predicates);
        }

        if (expression.Right != null)
        {
            CollectXsltPatternPredicatePlans(expression.Right, source, context, sourceUri, predicates);
        }

        if (expression.Operand != null)
        {
            CollectXsltPatternPredicatePlans(expression.Operand, source, context, sourceUri, predicates);
        }

        int index = 0;
        while (index < expression.Children.Length)
        {
            CollectXsltPatternPredicatePlans(expression.Children[index], source, context, sourceUri, predicates);
            index = index + 1;
        }
    }

    internal void AddXsltPatternKeyBindings(XPathStaticContext context)
    {
        XPathFunctionBinding[] existing = context.Functions == null ? [] : context.Functions;
        bool hasTwo = HasXsltPatternKeyBinding(existing, 2);
        bool hasThree = HasXsltPatternKeyBinding(existing, 3);
        int additional = (hasTwo ? 0 : 1) + (hasThree ? 0 : 1);
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

        if (!hasTwo)
        {
            result[index] = CreateXsltPatternKeyBinding(2);
            index = index + 1;
        }

        if (!hasThree)
        {
            result[index] = CreateXsltPatternKeyBinding(3);
        }

        context.Functions = result;
    }

    internal bool HasXsltPatternKeyBinding(XPathFunctionBinding[] bindings, int arity)
    {
        int index = 0;
        while (index < bindings.Length)
        {
            XmlName? name = bindings[index].Name;
            if (name != null && name.NamespaceUri == XPathNamespaces.Functions && name.LocalName == "key" && bindings[index].Arity == arity)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XPathFunctionBinding CreateXsltPatternKeyBinding(int arity)
    {
        XPathFunctionBinding result = new XPathFunctionBinding();
        result.Name = new XmlName { LocalName = "key", QualifiedName = "key", NamespaceUri = XPathNamespaces.Functions };
        result.Arity = arity;
        result.ParameterTypes = null;
        result.ParameterOccurrences = null;
        result.ResultType = XdmAtomicType.AnyAtomicType;
        result.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        result.BindingKey = "xslt:key#" + arity;
        result.IsDeterministic = true;
        SourceRange range = new SourceRange();
        XPathTypeSyntax type = new XPathTypeSyntax("node", null, null, null, "*", false, range);
        type.NodeTest = new XPathNodeTest(XPathNodeTestKind.AnyKind, range);
        result.ResultTypeSyntax = type;
        XPathTypeSyntax[] parameterTypes = new XPathTypeSyntax[arity];
        int index = 0;
        while (index < parameterTypes.Length)
        {
            parameterTypes[index] = new XPathTypeSyntax("item", null, null, null, "*", false, range);
            index = index + 1;
        }

        result.ParameterTypeSyntax = parameterTypes;
        return result;
    }

    internal bool IsXsltPredicatePattern(XPathAstNode expression)
    {
        XPathAstNode current = expression;
        while (current.Kind == XPathAstKind.Predicate && current.Left != null)
        {
            current = current.Left;
        }

        return current.Kind == XPathAstKind.ContextItem;
    }

    internal void ValidateXsltSelectionPattern(XPathAstNode expression)
    {
        if (expression.Kind == XPathAstKind.Union || expression.Kind == XPathAstKind.Intersect || expression.Kind == XPathAstKind.Except)
        {
            if (expression.Left == null || expression.Right == null)
            {
                throw CreateXsltPatternSyntaxError("A pattern set operator requires two operands.", expression.Range);
            }

            ValidateXsltSelectionPattern(expression.Left);
            ValidateXsltSelectionPattern(expression.Right);
            return;
        }

        if (expression.Kind == XPathAstKind.Parenthesized)
        {
            if (expression.Operand == null)
            {
                throw CreateXsltPatternSyntaxError("An empty parenthesized expression is not a pattern.", expression.Range);
            }

            ValidateXsltSelectionPattern(expression.Operand);
            return;
        }

        if (expression.Kind == XPathAstKind.Path)
        {
            ValidateXsltPatternPath(expression);
            return;
        }

        ValidateXsltPatternFirstStep(expression);
    }

    internal void ValidateXsltPatternPath(XPathAstNode path)
    {
        if (path.Children.Length == 0)
        {
            if (!path.Absolute || path.LeadingDescendant)
            {
                throw CreateXsltPatternSyntaxError("A pattern path has no steps.", path.Range);
            }

            return;
        }

        int index = 0;
        while (index < path.Children.Length)
        {
            if (index == 0)
            {
                ValidateXsltPatternFirstStep(path.Children[index]);
            }
            else
            {
                ValidateXsltPatternRelativeStep(path.Children[index]);
            }

            index = index + 1;
        }
    }

    internal void ValidateXsltPatternFirstStep(XPathAstNode step)
    {
        XPathAstNode baseStep = UnwrapXsltPatternPredicates(step);
        if (baseStep.Kind == XPathAstKind.Variable)
        {
            return;
        }

        if (baseStep.Kind == XPathAstKind.StaticFunctionCall)
        {
            ValidateXsltPatternOuterFunction(baseStep);
            return;
        }

        ValidateXsltPatternRelativeStep(step);
    }

    internal void ValidateXsltPatternRelativeStep(XPathAstNode step)
    {
        ValidateXsltPatternPredicates(step);
        XPathAstNode baseStep = UnwrapXsltPatternPredicates(step);
        if (baseStep.Kind == XPathAstKind.Name || baseStep.Kind == XPathAstKind.AttributeStep || baseStep.Kind == XPathAstKind.AxisStep)
        {
            if (baseStep.Axis != XPathAxis.Child && baseStep.Axis != XPathAxis.Descendant && baseStep.Axis != XPathAxis.Attribute && baseStep.Axis != XPathAxis.Self && baseStep.Axis != XPathAxis.DescendantOrSelf && baseStep.Axis != XPathAxis.Namespace)
            {
                throw CreateXsltPatternSyntaxError("A pattern uses an axis that is not permitted in a forward step.", baseStep.Range);
            }

            return;
        }

        if (baseStep.Kind == XPathAstKind.Parenthesized && baseStep.Operand != null)
        {
            ValidateXsltSelectionPattern(baseStep.Operand);
            return;
        }

        throw CreateXsltPatternSyntaxError("A pattern path contains a step that is not permitted.", baseStep.Range);
    }

    internal void ValidateXsltPatternPredicates(XPathAstNode step)
    {
        XPathAstNode current = step;
        while (current.Kind == XPathAstKind.Predicate)
        {
            if (current.Left == null || current.Right == null)
            {
                throw CreateXsltPatternSyntaxError("A pattern predicate is incomplete.", current.Range);
            }

            current = current.Left;
        }
    }

    internal XPathAstNode UnwrapXsltPatternPredicates(XPathAstNode step)
    {
        XPathAstNode current = step;
        while (current.Kind == XPathAstKind.Predicate && current.Left != null)
        {
            current = current.Left;
        }

        return current;
    }

    internal void ValidateXsltPatternOuterFunction(XPathAstNode call)
    {
        string local = call.LocalName == null ? "" : call.LocalName;
        bool allowed = local == "doc" || local == "id" || local == "element-with-id" || local == "key" || local == "root";
        if (!allowed)
        {
            throw CreateXsltPatternSyntaxError("A rooted pattern uses a function that is not permitted.", call.Range);
        }

        if (local == "root" && call.Children.Length != 0 || local == "doc" && call.Children.Length != 1 ||
            (local == "id" || local == "element-with-id") && call.Children.Length != 1 && call.Children.Length != 2 ||
            local == "key" && call.Children.Length != 2 && call.Children.Length != 3)
        {
            throw CreateXsltPatternSyntaxError("A rooted pattern function has an invalid arity.", call.Range);
        }

        int index = 0;
        while (index < call.Children.Length)
        {
            XPathAstKind kind = call.Children[index].Kind;
            if (kind != XPathAstKind.Variable && kind != XPathAstKind.StringLiteral && kind != XPathAstKind.IntegerLiteral && kind != XPathAstKind.DecimalLiteral && kind != XPathAstKind.DoubleLiteral)
            {
                throw CreateXsltPatternSyntaxError("A rooted pattern function argument must be a variable reference or literal.", call.Children[index].Range);
            }

            index = index + 1;
        }
    }

    internal void ValidateXsltPatternOuterFunctions(XPathAstNode expression)
    {
        if (expression.Kind == XPathAstKind.StaticFunctionCall)
        {
            string local = expression.LocalName == null ? "" : expression.LocalName;
            if (local == "doc" || local == "id" || local == "element-with-id" || local == "key" || local == "root")
            {
                XmlName? name = expression.ResolvedName;
                if (name == null || name.NamespaceUri != XPathNamespaces.Functions)
                {
                    throw CreateXsltPatternSyntaxError("An outer pattern function must use the standard function namespace.", expression.Range);
                }
            }
        }

        VisitXsltPatternChildren(expression);
    }

    internal void VisitXsltPatternChildren(XPathAstNode expression)
    {
        if (expression.Left != null)
        {
            ValidateXsltPatternOuterFunctions(expression.Left);
        }

        if (expression.Right != null)
        {
            ValidateXsltPatternOuterFunctions(expression.Right);
        }

        if (expression.Operand != null)
        {
            ValidateXsltPatternOuterFunctions(expression.Operand);
        }

        int index = 0;
        while (index < expression.Children.Length)
        {
            ValidateXsltPatternOuterFunctions(expression.Children[index]);
            index = index + 1;
        }
    }

    internal bool ContainsXsltRootedPattern(XPathAstNode expression)
    {
        if (expression.Kind == XPathAstKind.Union || expression.Kind == XPathAstKind.Intersect || expression.Kind == XPathAstKind.Except)
        {
            return expression.Left != null && ContainsXsltRootedPattern(expression.Left) || expression.Right != null && ContainsXsltRootedPattern(expression.Right);
        }

        if (expression.Kind == XPathAstKind.Parenthesized)
        {
            return expression.Operand != null && ContainsXsltRootedPattern(expression.Operand);
        }

        XPathAstNode first = expression;
        if (expression.Kind == XPathAstKind.Path && expression.Children.Length != 0)
        {
            first = expression.Children[0];
        }

        first = UnwrapXsltPatternPredicates(first);
        if (first.Kind == XPathAstKind.Parenthesized && first.Operand != null)
        {
            return ContainsXsltRootedPattern(first.Operand);
        }

        return first.Kind == XPathAstKind.Variable || first.Kind == XPathAstKind.StaticFunctionCall && IsXsltOuterPatternFunction(first);
    }

    internal bool ContainsXsltKeyCall(XPathAstNode expression)
    {
        if (expression.Kind == XPathAstKind.StaticFunctionCall && expression.LocalName == "key" && expression.ResolvedName != null && expression.ResolvedName.NamespaceUri == XPathNamespaces.Functions)
        {
            return true;
        }

        return AnyXsltPatternDescendant(expression, "key");
    }

    internal bool AnyXsltPatternDescendant(XPathAstNode expression, string search)
    {
        if (expression.Left != null && (search == "rooted" ? ContainsXsltRootedPattern(expression.Left) : ContainsXsltKeyCall(expression.Left)))
        {
            return true;
        }

        if (expression.Right != null && (search == "rooted" ? ContainsXsltRootedPattern(expression.Right) : ContainsXsltKeyCall(expression.Right)))
        {
            return true;
        }

        if (expression.Operand != null && (search == "rooted" ? ContainsXsltRootedPattern(expression.Operand) : ContainsXsltKeyCall(expression.Operand)))
        {
            return true;
        }

        int index = 0;
        while (index < expression.Children.Length)
        {
            if (search == "rooted" ? ContainsXsltRootedPattern(expression.Children[index]) : ContainsXsltKeyCall(expression.Children[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsXsltOuterPatternFunction(XPathAstNode expression)
    {
        string local = expression.LocalName == null ? "" : expression.LocalName;
        return local == "doc" || local == "id" || local == "element-with-id" || local == "key" || local == "root";
    }

    internal bool IsXsltMotionlessPattern(XPathAstNode expression, bool predicatePattern)
    {
        if (predicatePattern)
        {
            return IsXsltMotionlessPredicateChain(expression);
        }

        if (expression.Kind == XPathAstKind.Union || expression.Kind == XPathAstKind.Intersect || expression.Kind == XPathAstKind.Except)
        {
            return expression.Left != null && expression.Right != null && IsXsltMotionlessPattern(expression.Left, false) && IsXsltMotionlessPattern(expression.Right, false);
        }

        if (expression.Kind == XPathAstKind.Path)
        {
            int index = 0;
            while (index < expression.Children.Length)
            {
                if (!IsXsltMotionlessPatternStep(expression.Children[index]))
                {
                    return false;
                }

                index = index + 1;
            }

            return true;
        }

        return IsXsltMotionlessPatternStep(expression);
    }

    internal bool IsXsltMotionlessPatternStep(XPathAstNode step)
    {
        XPathAstNode current = step;
        while (current.Kind == XPathAstKind.Predicate)
        {
            if (current.Right == null || !IsXsltMotionlessPredicateExpression(current.Right) || !IsXsltNonPositionalPredicate(current.Right))
            {
                return false;
            }

            if (current.Left == null)
            {
                return false;
            }

            current = current.Left;
        }

        if (current.Kind == XPathAstKind.Parenthesized && current.Operand != null)
        {
            return IsXsltMotionlessPattern(current.Operand, false);
        }

        return true;
    }

    internal bool IsXsltMotionlessPredicateChain(XPathAstNode expression)
    {
        XPathAstNode current = expression;
        while (current.Kind == XPathAstKind.Predicate)
        {
            if (current.Right == null || !IsXsltMotionlessPredicateExpression(current.Right) || !IsXsltNonPositionalPredicate(current.Right))
            {
                return false;
            }

            if (current.Left == null)
            {
                return false;
            }

            current = current.Left;
        }

        return current.Kind == XPathAstKind.ContextItem;
    }

    internal bool IsXsltMotionlessPredicateExpression(XPathAstNode expression)
    {
        if (expression.Kind == XPathAstKind.Name || expression.Kind == XPathAstKind.AxisStep || expression.Kind == XPathAstKind.AttributeStep || expression.Kind == XPathAstKind.ParentStep)
        {
            return expression.Axis == XPathAxis.Attribute || expression.Axis == XPathAxis.Namespace || expression.Axis == XPathAxis.Self || expression.Axis == XPathAxis.Parent || expression.Axis == XPathAxis.Ancestor || expression.Axis == XPathAxis.AncestorOrSelf;
        }

        if ((expression.Kind == XPathAstKind.GeneralComparison || expression.Kind == XPathAstKind.ValueComparison) && ContainsXsltDirectContextItem(expression))
        {
            return false;
        }

        if (expression.Kind == XPathAstKind.StaticFunctionCall && ContainsXsltDirectContextItemInArguments(expression) && IsXsltConsumingFocusFunction(expression))
        {
            return false;
        }

        if (expression.Kind == XPathAstKind.Path)
        {
            int pathIndex = 0;
            while (pathIndex < expression.Children.Length)
            {
                if (!IsXsltMotionlessPredicateExpression(expression.Children[pathIndex]))
                {
                    return false;
                }

                pathIndex = pathIndex + 1;
            }
        }

        if (expression.Left != null && !IsXsltMotionlessPredicateExpression(expression.Left) || expression.Right != null && !IsXsltMotionlessPredicateExpression(expression.Right) || expression.Operand != null && !IsXsltMotionlessPredicateExpression(expression.Operand))
        {
            return false;
        }

        int index = 0;
        while (index < expression.Children.Length)
        {
            if (!IsXsltMotionlessPredicateExpression(expression.Children[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsXsltNonPositionalPredicate(XPathAstNode expression)
    {
        if (ContainsXsltPositionalFunction(expression, true))
        {
            return false;
        }

        XPathStaticType? type = expression.StaticType;
        if (type == null || type.ItemKind == XPathStaticItemKind.Unknown || type.ItemKind == XPathStaticItemKind.AnyItem)
        {
            return false;
        }

        if (type.ItemKind != XPathStaticItemKind.Atomic)
        {
            return true;
        }

        XdmTypeSystem types = new XdmTypeSystem();
        return !types.IsXdmAtomicSubtypeOf(type.AtomicType, XdmAtomicType.Decimal) && !types.IsXdmAtomicSubtypeOf(type.AtomicType, XdmAtomicType.Double) && !types.IsXdmAtomicSubtypeOf(type.AtomicType, XdmAtomicType.Float);
    }

    internal bool ContainsXsltPositionalFunction(XPathAstNode expression, bool topLevel)
    {
        if (expression.Kind == XPathAstKind.Predicate && !topLevel)
        {
            return false;
        }

        if ((expression.Kind == XPathAstKind.StaticFunctionCall || expression.Kind == XPathAstKind.NamedFunctionReference) &&
            (expression.LocalName == "position" || expression.LocalName == "last" || expression.LocalName == "function-lookup"))
        {
            return true;
        }

        if (expression.Left != null && ContainsXsltPositionalFunction(expression.Left, false) || expression.Right != null && ContainsXsltPositionalFunction(expression.Right, false) || expression.Operand != null && ContainsXsltPositionalFunction(expression.Operand, false))
        {
            return true;
        }

        int index = 0;
        while (index < expression.Children.Length)
        {
            if (ContainsXsltPositionalFunction(expression.Children[index], false))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsXsltDirectContextItem(XPathAstNode expression)
    {
        return expression.Left != null && expression.Left.Kind == XPathAstKind.ContextItem || expression.Right != null && expression.Right.Kind == XPathAstKind.ContextItem;
    }

    internal bool ContainsXsltDirectContextItemInArguments(XPathAstNode expression)
    {
        int index = 0;
        while (index < expression.Children.Length)
        {
            if (expression.Children[index].Kind == XPathAstKind.ContextItem)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsXsltConsumingFocusFunction(XPathAstNode expression)
    {
        string local = expression.LocalName == null ? "" : expression.LocalName;
        return local == "data" || local == "string" || local == "number" || local == "string-length" || local == "normalize-space" || local == "starts-with" || local == "ends-with" || local == "contains" || local == "substring" || local == "substring-before" || local == "substring-after" || local == "matches" || local == "tokenize";
    }

    internal XPathStaticException CreateXsltPatternSyntaxError(string message, SourceRange range)
    {
        SourceLocation? location = range.Start;
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XsltErrorCodes.XTSE0340;
        diagnostic.Category = XmlErrorCategory.XsltStatic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = range;
        return new XPathStaticException(message, diagnostic, location);
    }

    internal XsltStaticException CreateXsltPatternTaskError(string message, XmlNode node, XsltStylesheetModule module, AggregateException? failure)
    {
        return new XsltStylesheetAssembler(Loader).CreateXsltAssemblyError(message, Loader.StaticProcessor.ReadXsltTaskErrorCode(failure), node, module);
    }
}
