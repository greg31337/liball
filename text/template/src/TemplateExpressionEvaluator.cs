using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace template;

internal class TemplateExpressionEvaluator
{
    internal static readonly object LiquidEmptyMarker = new object();
    internal static readonly object LiquidBlankMarker = new object();
    internal TemplateScopeStack Scope;
    internal TemplateOptions Options;
    internal ITemplateMemberResolver MemberResolver;
    internal Dictionary<string, Func<List<TemplateValue>, TemplateValue>> Functions;
    internal Dictionary<string, Func<List<TemplateValue>, TemplateValue>> Filters;
    internal Dictionary<string, Func<List<TemplateValue>, TemplateValue>> Tests;
    internal Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>> AsyncFunctions;
    internal Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>> AsyncFilters;
    internal Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>> AsyncTests;
    internal TemplateRenderContext? RenderContext;
    internal int MissingToleranceDepth;

    internal static TemplateExpressionEvaluator Create(TemplateScopeStack scope)
    {
        return new TemplateExpressionEvaluator(scope, new TemplateOptions(), new TemplateMemberResolver(), new TemplateRegistry());
    }

    internal static TemplateExpressionEvaluator Create(TemplateScopeStack scope, TemplateOptions options, ITemplateMemberResolver memberResolver)
    {
        return new TemplateExpressionEvaluator(scope, options, memberResolver, new TemplateRegistry());
    }

    internal TemplateExpressionEvaluator(TemplateScopeStack scope, TemplateOptions options, ITemplateMemberResolver memberResolver, TemplateRegistry registry)
    {
        if (scope == null)
        {
            throw new ArgumentNullException("scope");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (memberResolver == null)
        {
            throw new ArgumentNullException("memberResolver");
        }

        if (registry == null)
        {
            throw new ArgumentNullException("registry");
        }

        Scope = scope;
        Options = options;
        MemberResolver = memberResolver;
        Functions = new Dictionary<string, Func<List<TemplateValue>, TemplateValue>>();
        Filters = new Dictionary<string, Func<List<TemplateValue>, TemplateValue>>();
        Tests = new Dictionary<string, Func<List<TemplateValue>, TemplateValue>>();
        AsyncFunctions = new Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>>();
        AsyncFilters = new Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>>();
        AsyncTests = new Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>>();
        RenderContext = null;
        MissingToleranceDepth = 0;
        AddBuiltInFilters();
        AddBuiltInTests();
        registry.ApplyTo(this);
    }

    internal void RegisterFunction(string name, Func<List<TemplateValue>, TemplateValue> function)
    {
        RegisterCallable(Functions, name, function);
    }

    internal void RegisterFilter(string name, Func<List<TemplateValue>, TemplateValue> function)
    {
        RegisterCallable(Filters, name, function);
    }

    internal void RegisterTest(string name, Func<List<TemplateValue>, TemplateValue> function)
    {
        RegisterCallable(Tests, name, function);
    }

    internal void RegisterAsyncFunction(string name, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>> function)
    {
        AsyncFunctions[name] = function;
    }

    internal void RegisterAsyncFilter(string name, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>> function)
    {
        AsyncFilters[name] = function;
    }

    internal void RegisterAsyncTest(string name, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>> function)
    {
        AsyncTests[name] = function;
    }

    internal TemplateValue Evaluate(TemplateExpressionSyntax expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        EnsureExpressionDepthAllowed(expression);
        CheckRenderTime();
        TemplateValue result = TemplateValue.Missing();
        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Literal)
        {
            result = EvaluateLiteral(expression);
        }
        else if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Variable || expression.ExpressionKind == TemplateExpressionSyntaxKind.Path)
        {
            result = EvaluatePath(expression);
        }
        else if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Operator)
        {
            result = EvaluateOperator(expression);
        }
        else if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Group)
        {
            if (expression.Children.Count == 0)
            {
                result = TemplateValue.Missing();
            }
            else
            {
                result = Evaluate(expression.Children[0]);
            }
        }
        else if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Call)
        {
            result = EvaluateCall(expression);
        }

        CheckRenderTime();
        return result;
    }

    internal void EnsureExpressionDepthAllowed(TemplateExpressionSyntax expression)
    {
        int depth = GetExpressionDepth(expression);
        if (depth > Options.MaximumExpressionDepth)
        {
            throw new InvalidOperationException("Maximum expression depth of " + Options.MaximumExpressionDepth.ToString() + " was exceeded.");
        }
    }

    internal int GetExpressionDepth(TemplateExpressionSyntax expression)
    {
        int maximumChildDepth = 0;
        int index = 0;
        while (index < expression.Children.Count)
        {
            int childDepth = GetExpressionDepth(expression.Children[index]);
            if (childDepth > maximumChildDepth)
            {
                maximumChildDepth = childDepth;
            }

            index = index + 1;
        }

        index = 0;
        while (index < expression.Arguments.Count)
        {
            int argumentDepth = GetExpressionDepth(expression.Arguments[index]);
            if (argumentDepth > maximumChildDepth)
            {
                maximumChildDepth = argumentDepth;
            }

            index = index + 1;
        }

        index = 0;
        while (index < expression.NamedArguments.Count)
        {
            int namedDepth = GetExpressionDepth(expression.NamedArguments[index].Value);
            if (namedDepth > maximumChildDepth)
            {
                maximumChildDepth = namedDepth;
            }

            index = index + 1;
        }

        index = 0;
        while (index < expression.HashArguments.Count)
        {
            int hashDepth = GetExpressionDepth(expression.HashArguments[index].Value);
            if (hashDepth > maximumChildDepth)
            {
                maximumChildDepth = hashDepth;
            }

            index = index + 1;
        }

        index = 0;
        while (index < expression.PathSegments.Count)
        {
            TemplatePathSegmentSyntax segment = expression.PathSegments[index];
            if (segment.Expression != null)
            {
                int segmentDepth = GetExpressionDepth(segment.Expression);
                if (segmentDepth > maximumChildDepth)
                {
                    maximumChildDepth = segmentDepth;
                }
            }

            index = index + 1;
        }

        return maximumChildDepth + 1;
    }

    internal void CheckRenderTime()
    {
        if (RenderContext != null)
        {
            RenderContext.CheckRenderTime();
        }
    }

    internal TemplateValue EvaluateLiteral(TemplateExpressionSyntax expression)
    {
        if (expression.LiteralKind == TemplateLiteralSyntaxKind.String)
        {
            return TemplateValue.FromString(UnquoteString(expression.Text));
        }

        if (expression.LiteralKind == TemplateLiteralSyntaxKind.Number)
        {
            decimal number = 0m;
            if (decimal.TryParse(expression.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out number))
            {
                return TemplateValue.FromNumber(number);
            }

            return TemplateValue.Missing();
        }

        if (expression.LiteralKind == TemplateLiteralSyntaxKind.Boolean)
        {
            return TemplateValue.FromBoolean(expression.Text == "true");
        }

        if (expression.LiteralKind == TemplateLiteralSyntaxKind.Null)
        {
            return TemplateValue.Null();
        }

        if (expression.LiteralKind == TemplateLiteralSyntaxKind.Empty)
        {
            if (Options.SyntaxProfile == "liquid")
            {
                return TemplateValue.FromObject(LiquidEmptyMarker);
            }

            return TemplateValue.FromList(new List<TemplateValue>());
        }

        if (expression.LiteralKind == TemplateLiteralSyntaxKind.Array)
        {
            return EvaluateArrayLiteral(expression);
        }

        if (expression.LiteralKind == TemplateLiteralSyntaxKind.Object)
        {
            return EvaluateObjectLiteral(expression);
        }

        if (expression.LiteralKind == TemplateLiteralSyntaxKind.Range)
        {
            return EvaluateRangeLiteral(expression);
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue EvaluateArrayLiteral(TemplateExpressionSyntax expression)
    {
        List<TemplateValue> values = new List<TemplateValue>();
        int index = 0;
        while (index < expression.Children.Count)
        {
            values.Add(Evaluate(expression.Children[index]));
            index = index + 1;
        }

        return TemplateValue.FromList(values);
    }

    internal TemplateValue EvaluateObjectLiteral(TemplateExpressionSyntax expression)
    {
        Dictionary<string, TemplateValue> values = new Dictionary<string, TemplateValue>();
        int index = 0;
        while (index + 1 < expression.Children.Count)
        {
            TemplateValue key = EvaluateObjectKey(expression.Children[index]);
            TemplateValue value = Evaluate(expression.Children[index + 1]);
            values[ConvertKey(key)] = value;
            index = index + 2;
        }

        return TemplateValue.FromMap(values);
    }

    internal TemplateValue EvaluateObjectKey(TemplateExpressionSyntax expression)
    {
        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Variable)
        {
            return TemplateValue.FromString(expression.Text);
        }

        return Evaluate(expression);
    }

    internal TemplateValue EvaluateRangeLiteral(TemplateExpressionSyntax expression)
    {
        if (expression.Children.Count < 2)
        {
            return TemplateValue.Missing();
        }

        TemplateValue start = Evaluate(expression.Children[0]);
        TemplateValue end = Evaluate(expression.Children[1]);
        if (start.Kind != TemplateValueKind.Number || end.Kind != TemplateValueKind.Number)
        {
            return InvalidOperationValue("Range endpoints must be numbers.", expression.Span);
        }

        if (!IsInt32(start.NumberValue) || !IsInt32(end.NumberValue))
        {
            return InvalidOperationValue("Range endpoints must be 32-bit integers.", expression.Span);
        }

        int startIndex = (int)start.NumberValue;
        int endIndex = (int)end.NumberValue;
        decimal itemCount = end.NumberValue - start.NumberValue;
        if (itemCount < 0m)
        {
            itemCount = 0m - itemCount;
        }

        itemCount = itemCount + 1m;
        if (itemCount > Options.MaximumLoopIterations)
        {
            throw new InvalidOperationException("Range contains more than the configured maximum of " + Options.MaximumLoopIterations.ToString(CultureInfo.InvariantCulture) + " values.");
        }

        List<TemplateValue> values = new List<TemplateValue>();
        int current = startIndex;
        if (startIndex <= endIndex)
        {
            while (true)
            {
                values.Add(TemplateValue.FromNumber(current));
                if (current == endIndex)
                {
                    break;
                }

                current = current + 1;
            }
        }
        else
        {
            while (true)
            {
                values.Add(TemplateValue.FromNumber(current));
                if (current == endIndex)
                {
                    break;
                }

                current = current - 1;
            }
        }

        return TemplateValue.FromList(values);
    }

    internal TemplateValue EvaluatePath(TemplateExpressionSyntax expression)
    {
        if (expression.PathSegments.Count == 0)
        {
            return ResolvePathRoot(expression.PathKind);
        }

        int index = 0;
        TemplateValue current = ResolvePathRoot(expression.PathKind);
        if (expression.PathKind == TemplatePathSyntaxKind.Variable)
        {
            TemplatePathSegmentSyntax first = expression.PathSegments[0];
            if (first.SegmentKind != TemplatePathSegmentSyntaxKind.Name)
            {
                return TemplateValue.Missing();
            }

            current = ResolveVariable(first.Text, expression.Span);
            index = 1;
        }
        else if (expression.PathKind == TemplatePathSyntaxKind.Local)
        {
            TemplatePathSegmentSyntax first = expression.PathSegments[0];
            if (first.SegmentKind != TemplatePathSegmentSyntaxKind.Name)
            {
                return TemplateValue.Missing();
            }

            current = ResolveLocalVariable(first.Text);
            index = 1;
        }

        while (index < expression.PathSegments.Count)
        {
            if (current.IsMissing() || current.IsNull())
            {
                if (expression.PathSegments[index].NullSafe)
                {
                    return TemplateValue.Null();
                }

                return MissingPathValue(expression.PathSegments[index].Text, expression.Span);
            }

            TemplatePathSegmentSyntax segment = expression.PathSegments[index];
            if (segment.SegmentKind == TemplatePathSegmentSyntaxKind.Name)
            {
                EnsureMemberAccessAllowed(segment.Text, expression.Span);
                current = MemberResolver.ResolveMember(current, segment.Text);
            }
            else
            {
                TemplateExpressionSyntax? indexExpression = segment.Expression;
                if (indexExpression == null)
                {
                    return TemplateValue.Missing();
                }

                current = MemberResolver.ResolveIndex(current, Evaluate(indexExpression));
            }

            if (current.IsMissing() && segment.NullSafe)
            {
                return TemplateValue.Null();
            }

            index = index + 1;
        }

        if (current.IsMissing())
        {
            return MissingPathValue(expression.Text, expression.Span);
        }

        return current;
    }

    internal TemplateValue ResolveVariable(string name, TemplateSourceSpan? span)
    {
        if (name == "this")
        {
            return Scope.GetCurrentValue();
        }

        if (Options.SyntaxProfile == "liquid" && name == "blank")
        {
            return TemplateValue.FromObject(LiquidBlankMarker);
        }

        EnsureMemberAccessAllowed(name, span);
        if (Options.SyntaxProfile == "handlebars")
        {
            TemplateValue handlebarsLocal = ResolveLocalVariable(name);
            if (!handlebarsLocal.IsMissing())
            {
                return handlebarsLocal;
            }

            TemplateValue handlebarsCurrent = MemberResolver.ResolveMember(Scope.GetCurrentValue(), name);
            if (!handlebarsCurrent.IsMissing())
            {
                return handlebarsCurrent;
            }

            if (RenderContext != null && RenderContext.HandlebarsPartialBlockContexts.Count > 0)
            {
                TemplateValue partialBlockContext = RenderContext.HandlebarsPartialBlockContexts[RenderContext.HandlebarsPartialBlockContexts.Count - 1];
                return MemberResolver.ResolveMember(partialBlockContext, name);
            }

            return TemplateValue.Missing();
        }

        if (Options.NameLookupOrder == TemplateNameLookupOrder.CurrentLocalRoot)
        {
            TemplateValue currentFirst = MemberResolver.ResolveMember(Scope.GetCurrentValue(), name);
            if (!currentFirst.IsMissing())
            {
                return currentFirst;
            }

            TemplateValue localSecond = ResolveLocalVariable(name);
            if (!localSecond.IsMissing())
            {
                return localSecond;
            }

            return MemberResolver.ResolveMember(Scope.RootValue, name);
        }

        if (Options.NameLookupOrder == TemplateNameLookupOrder.RootLocalCurrent)
        {
            TemplateValue rootFirst = MemberResolver.ResolveMember(Scope.RootValue, name);
            if (!rootFirst.IsMissing())
            {
                return rootFirst;
            }

            TemplateValue localSecond = ResolveLocalVariable(name);
            if (!localSecond.IsMissing())
            {
                return localSecond;
            }

            return MemberResolver.ResolveMember(Scope.GetCurrentValue(), name);
        }

        TemplateValue localFirst = ResolveLocalVariable(name);
        if (!localFirst.IsMissing())
        {
            return localFirst;
        }

        TemplateValue currentSecond = MemberResolver.ResolveMember(Scope.GetCurrentValue(), name);
        if (!currentSecond.IsMissing())
        {
            return currentSecond;
        }

        return MemberResolver.ResolveMember(Scope.RootValue, name);
    }

    internal TemplateValue ResolveLocalVariable(string name)
    {
        bool caseSensitive = true;
        TemplateMemberResolver? resolver = MemberResolver as TemplateMemberResolver;
        if (resolver != null)
        {
            caseSensitive = resolver.Options.CaseSensitive;
        }

        return Scope.GetLocalMatching(name, caseSensitive);
    }

    internal void EnsureMemberAccessAllowed(string memberName, TemplateSourceSpan? span)
    {
        TemplateMemberResolver? resolver = MemberResolver as TemplateMemberResolver;
        if (resolver == null || resolver.Options.SecurityPolicy == null)
        {
            return;
        }

        if (!resolver.Options.SecurityPolicy.CanAccessMember(memberName))
        {
            ReportDiagnostic("SECURITY011", TemplateDiagnosticSeverity.Warning, "Access to template member '" + memberName + "' is denied by the security policy.", span, "Remove the member access or explicitly allow only a safe model member.");
            if (Options.StrictUnsafeMembers)
            {
                throw new InvalidOperationException("Access to template member '" + memberName + "' is denied by the security policy.");
            }
        }
    }

    internal TemplateValue ResolvePathRoot(TemplatePathSyntaxKind pathKind)
    {
        if (pathKind == TemplatePathSyntaxKind.Root)
        {
            return Scope.RootValue;
        }

        if (pathKind == TemplatePathSyntaxKind.Current)
        {
            return Scope.GetCurrentValue();
        }

        if (pathKind == TemplatePathSyntaxKind.Parent)
        {
            return Scope.GetParentCurrentValue();
        }

        if (pathKind == TemplatePathSyntaxKind.Local)
        {
            return Scope.GetCurrentFrame().CurrentValue;
        }

        return Scope.GetCurrentValue();
    }

    internal TemplateValue MissingPathValue(string path, TemplateSourceSpan? span)
    {
        ReportDiagnostic("EVAL002", TemplateDiagnosticSeverity.Warning, "Template path '" + path + "' resolved to a missing value.", span, "Provide the value or guard the expression with a defined or missing test.");
        if (Options.StrictMissingValues && MissingToleranceDepth == 0)
        {
            throw new InvalidOperationException("Template path '" + path + "' resolved to a missing value.");
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue EvaluateOperator(TemplateExpressionSyntax expression)
    {
        if (expression.OperatorKind == TemplateExpressionOperatorKind.UnaryNot)
        {
            return TemplateValue.FromBoolean(!TemplateTruthiness.IsTruthy(EvaluateFirstChild(expression), Options));
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.UnaryNegate)
        {
            return TemplateValue.FromNumber(0m - ToNumber(EvaluateFirstChild(expression)));
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.UnaryPlus)
        {
            return TemplateValue.FromNumber(ToNumber(EvaluateFirstChild(expression)));
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.And)
        {
            TemplateValue leftAnd = EvaluateChild(expression, 0);
            if (!TemplateTruthiness.IsTruthy(leftAnd, Options))
            {
                return TemplateValue.FromBoolean(false);
            }

            return TemplateValue.FromBoolean(TemplateTruthiness.IsTruthy(EvaluateChild(expression, 1), Options));
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.Or)
        {
            TemplateValue leftOr = EvaluateChild(expression, 0);
            if (TemplateTruthiness.IsTruthy(leftOr, Options))
            {
                return TemplateValue.FromBoolean(true);
            }

            return TemplateValue.FromBoolean(TemplateTruthiness.IsTruthy(EvaluateChild(expression, 1), Options));
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.NullCoalescing)
        {
            TemplateValue leftCoalesce = EvaluateChildAllowMissing(expression, 0);
            if (leftCoalesce.IsMissing() || leftCoalesce.IsNull())
            {
                return EvaluateChild(expression, 1);
            }

            return leftCoalesce;
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.Conditional)
        {
            TemplateValue condition = EvaluateChild(expression, 0);
            if (TemplateTruthiness.IsTruthy(condition, Options))
            {
                return EvaluateChild(expression, 1);
            }

            return EvaluateChild(expression, 2);
        }

        return EvaluateBinaryOperator(expression);
    }

    internal TemplateValue EvaluateBinaryOperator(TemplateExpressionSyntax expression)
    {
        TemplateValue left = EvaluateChild(expression, 0);
        TemplateValue right = EvaluateChild(expression, 1);

        if (expression.OperatorKind == TemplateExpressionOperatorKind.Add)
        {
            if (left.IsStringLike() || right.IsStringLike())
            {
                return TemplateValue.FromString(ToText(left) + ToText(right));
            }

            return TemplateValue.FromNumber(ToNumber(left) + ToNumber(right));
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.Subtract)
        {
            return TemplateValue.FromNumber(ToNumber(left) - ToNumber(right));
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.Multiply)
        {
            return TemplateValue.FromNumber(ToNumber(left) * ToNumber(right));
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.Divide)
        {
            if (ToNumber(right) == 0m)
            {
                throw new InvalidOperationException("Cannot divide by zero.");
            }

            return TemplateValue.FromNumber(ToNumber(left) / ToNumber(right));
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.Modulo)
        {
            if (ToNumber(right) == 0m)
            {
                throw new InvalidOperationException("Cannot calculate a remainder with a zero divisor.");
            }

            return TemplateValue.FromNumber(ToNumber(left) % ToNumber(right));
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.Equal)
        {
            return TemplateValue.FromBoolean(ValuesEqual(left, right));
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.NotEqual)
        {
            return TemplateValue.FromBoolean(!ValuesEqual(left, right));
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.LessThan)
        {
            return TemplateValue.FromBoolean(CompareValues(left, right) < 0);
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.LessThanOrEqual)
        {
            return TemplateValue.FromBoolean(CompareValues(left, right) <= 0);
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.GreaterThan)
        {
            return TemplateValue.FromBoolean(CompareValues(left, right) > 0);
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.GreaterThanOrEqual)
        {
            return TemplateValue.FromBoolean(CompareValues(left, right) >= 0);
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.Contains)
        {
            return TemplateValue.FromBoolean(ValueContains(left, right));
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.StartsWith)
        {
            return TemplateValue.FromBoolean(ValueStartsWith(left, right));
        }

        if (expression.OperatorKind == TemplateExpressionOperatorKind.EndsWith)
        {
            return TemplateValue.FromBoolean(ValueEndsWith(left, right));
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue EvaluateCall(TemplateExpressionSyntax expression)
    {
        List<TemplateValue> arguments;
        if (expression.CallKind == TemplateExpressionCallKind.Test || (expression.CallKind == TemplateExpressionCallKind.Filter && FilterAllowsMissing(expression.Name)))
        {
            arguments = EvaluateArgumentsAllowMissing(expression);
        }
        else
        {
            arguments = EvaluateArguments(expression);
        }

        if (expression.CallKind == TemplateExpressionCallKind.Function || expression.CallKind == TemplateExpressionCallKind.Subexpression)
        {
            return InvokeCallable(Functions, expression.Name, arguments, expression.Span);
        }

        if (expression.CallKind == TemplateExpressionCallKind.Filter)
        {
            return InvokeCallable(Filters, expression.Name, arguments, expression.Span);
        }

        if (expression.CallKind == TemplateExpressionCallKind.Test)
        {
            return InvokeCallable(Tests, expression.Name, arguments, expression.Span);
        }

        return TemplateValue.Missing();
    }

    internal bool FilterAllowsMissing(string name)
    {
        return name == "default" || name == "d" || name == "default_if_none";
    }

    internal List<TemplateValue> EvaluateArguments(TemplateExpressionSyntax expression)
    {
        List<TemplateValue> arguments = new List<TemplateValue>();
        int index = 0;
        while (index < expression.Arguments.Count)
        {
            arguments.Add(Evaluate(expression.Arguments[index]));
            index = index + 1;
        }

        AddCallArgumentMap(arguments, expression.NamedArguments, expression.HashArguments);

        return arguments;
    }

    internal List<TemplateValue> EvaluateArgumentsAllowMissing(TemplateExpressionSyntax expression)
    {
        MissingToleranceDepth = MissingToleranceDepth + 1;
        List<TemplateValue> arguments = EvaluateArguments(expression);
        MissingToleranceDepth = MissingToleranceDepth - 1;
        return arguments;
    }

    internal void AddCallArgumentMap(List<TemplateValue> arguments, List<TemplateNamedArgumentSyntax> namedArguments, List<TemplateNamedArgumentSyntax> hashArguments)
    {
        if (namedArguments.Count == 0 && hashArguments.Count == 0)
        {
            return;
        }

        Dictionary<string, TemplateValue> values = new Dictionary<string, TemplateValue>();
        int index = 0;
        while (index < namedArguments.Count)
        {
            values[namedArguments[index].Name] = Evaluate(namedArguments[index].Value);
            index = index + 1;
        }

        index = 0;
        while (index < hashArguments.Count)
        {
            values[hashArguments[index].Name] = Evaluate(hashArguments[index].Value);
            index = index + 1;
        }

        arguments.Add(TemplateValue.FromMap(values));
    }

    internal TemplateValue InvokeCallable(Dictionary<string, Func<List<TemplateValue>, TemplateValue>> callables, string name, List<TemplateValue> arguments, TemplateSourceSpan? span)
    {
        if (callables.ContainsKey(name))
        {
            return InvokeFunction(callables[name], arguments);
        }

        if (name.Length > 4 && name.Substring(0, 4) == "not ")
        {
            string positiveName = name.Substring(4);
            if (callables.ContainsKey(positiveName))
            {
                TemplateValue positiveResult = InvokeFunction(callables[positiveName], arguments);
                return TemplateValue.FromBoolean(!TemplateTruthiness.IsTruthy(positiveResult, Options));
            }
        }

        TemplateValue scoped = ResolveVariable(name, span);
        if (scoped.Kind == TemplateValueKind.Function)
        {
            return scoped.Invoke(arguments);
        }

        if (Options.StrictUnknownFunctions)
        {
            ReportDiagnostic("EVAL001", TemplateDiagnosticSeverity.Error, "Unknown template function '" + name + "'.", span, "Register the callable or correct its name.");
            throw new InvalidOperationException("Unknown template function '" + name + "'.");
        }

        ReportDiagnostic("EVAL001", TemplateDiagnosticSeverity.Warning, "Unknown template function '" + name + "'.", span, "Register the callable or correct its name.");
        return TemplateValue.Missing();
    }

    internal TemplateValue InvokeFunction(Func<List<TemplateValue>, TemplateValue> function, List<TemplateValue> arguments)
    {
        TemplateValue result = function(arguments);
        if (result == null)
        {
            throw new InvalidOperationException("Template function returned null.");
        }

        return result;
    }

    internal TemplateValue EvaluateFirstChild(TemplateExpressionSyntax expression)
    {
        return EvaluateChild(expression, 0);
    }

    internal TemplateValue EvaluateChild(TemplateExpressionSyntax expression, int index)
    {
        if (index < 0 || index >= expression.Children.Count)
        {
            return TemplateValue.Missing();
        }

        return Evaluate(expression.Children[index]);
    }

    internal TemplateValue EvaluateChildAllowMissing(TemplateExpressionSyntax expression, int index)
    {
        MissingToleranceDepth = MissingToleranceDepth + 1;
        TemplateValue value = EvaluateChild(expression, index);
        MissingToleranceDepth = MissingToleranceDepth - 1;
        return value;
    }

    internal void RegisterCallable(Dictionary<string, Func<List<TemplateValue>, TemplateValue>> callables, string name, Func<List<TemplateValue>, TemplateValue> function)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (function == null)
        {
            throw new ArgumentNullException("function");
        }

        callables[name] = function;
    }

    internal void AddBuiltInFilters()
    {
        RegisterFilter("upper", (List<TemplateValue> arguments) =>
        {
            return CreateTextFilterResult(arguments, GetFilterText(arguments).ToUpper(Options.Culture));
        });
        RegisterFilter("upcase", (List<TemplateValue> arguments) =>
        {
            return CreateTextFilterResult(arguments, GetFilterText(arguments).ToUpper(Options.Culture));
        });
        RegisterFilter("lower", (List<TemplateValue> arguments) =>
        {
            return CreateTextFilterResult(arguments, GetFilterText(arguments).ToLower(Options.Culture));
        });
        RegisterFilter("downcase", (List<TemplateValue> arguments) =>
        {
            return CreateTextFilterResult(arguments, GetFilterText(arguments).ToLower(Options.Culture));
        });
        RegisterFilter("capitalize", (List<TemplateValue> arguments) =>
        {
            return CreateTextFilterResult(arguments, CapitalizeText(GetFilterText(arguments)));
        });
        RegisterFilter("title", (List<TemplateValue> arguments) =>
        {
            string text = GetFilterText(arguments).ToLower(Options.Culture);
            return CreateTextFilterResult(arguments, Options.Culture.TextInfo.ToTitleCase(text));
        });
        RegisterFilter("trim", (List<TemplateValue> arguments) =>
        {
            return CreateTextFilterResult(arguments, GetFilterText(arguments).Trim());
        });
        RegisterFilter("strip", (List<TemplateValue> arguments) =>
        {
            return CreateTextFilterResult(arguments, GetFilterText(arguments).Trim());
        });
        RegisterFilter("ltrim", (List<TemplateValue> arguments) =>
        {
            return CreateTextFilterResult(arguments, GetFilterText(arguments).TrimStart());
        });
        RegisterFilter("lstrip", (List<TemplateValue> arguments) =>
        {
            return CreateTextFilterResult(arguments, GetFilterText(arguments).TrimStart());
        });
        RegisterFilter("rtrim", (List<TemplateValue> arguments) =>
        {
            return CreateTextFilterResult(arguments, GetFilterText(arguments).TrimEnd());
        });
        RegisterFilter("rstrip", (List<TemplateValue> arguments) =>
        {
            return CreateTextFilterResult(arguments, GetFilterText(arguments).TrimEnd());
        });
        RegisterFilter("replace", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(GetFilterText(arguments).Replace(GetFilterTextAtIndex(arguments, 1), GetFilterTextAtIndex(arguments, 2)));
        });
        RegisterFilter("replace_first", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(ReplaceFirst(GetFilterText(arguments), GetFilterTextAtIndex(arguments, 1), GetFilterTextAtIndex(arguments, 2)));
        });
        RegisterFilter("remove", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(GetFilterText(arguments).Replace(GetFilterTextAtIndex(arguments, 1), ""));
        });
        RegisterFilter("remove_first", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(ReplaceFirst(GetFilterText(arguments), GetFilterTextAtIndex(arguments, 1), ""));
        });
        RegisterFilter("strip_newlines", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(GetFilterText(arguments).Replace("\r", "").Replace("\n", ""));
        });
        RegisterFilter("append", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(GetFilterText(arguments) + GetFilterTextAtIndex(arguments, 1));
        });
        RegisterFilter("prepend", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(GetFilterTextAtIndex(arguments, 1) + GetFilterText(arguments));
        });
        RegisterFilter("newline_to_br", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(NewlineToBr(GetFilterText(arguments)));
        });
        RegisterFilter("newlineToBr", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(NewlineToBr(GetFilterText(arguments)));
        });
        RegisterFilter("indent", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(IndentFilter(arguments));
        });
        RegisterFilter("split", (List<TemplateValue> arguments) =>
        {
            return SplitFilter(arguments);
        });
        RegisterFilter("join", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(JoinFilter(arguments));
        });
        RegisterFilter("slice", (List<TemplateValue> arguments) =>
        {
            return SliceValueFilter(arguments);
        });
        RegisterFilter("truncate", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(TruncateFilter(arguments));
        });
        RegisterFilter("truncate_words", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(TruncateWordsFilter(arguments));
        });
        RegisterFilter("truncateWords", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(TruncateWordsFilter(arguments));
        });
        RegisterFilter("truncatewords", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(TruncateWordsFilter(arguments));
        });
        RegisterFilter("html", (List<TemplateValue> arguments) =>
        {
            return EncodeFilter(arguments, TemplateEscapingMode.Html);
        });
        RegisterFilter("html_escape", (List<TemplateValue> arguments) =>
        {
            return EncodeFilter(arguments, TemplateEscapingMode.Html);
        });
        RegisterFilter("escape", (List<TemplateValue> arguments) =>
        {
            return EncodeFilter(arguments, TemplateEscapingMode.Html);
        });
        RegisterFilter("e", (List<TemplateValue> arguments) =>
        {
            return EncodeFilter(arguments, TemplateEscapingMode.Html);
        });
        RegisterFilter("force_escape", (List<TemplateValue> arguments) =>
        {
            return EncodeFilter(arguments, TemplateEscapingMode.Html);
        });
        RegisterFilter("forceescape", (List<TemplateValue> arguments) =>
        {
            return EncodeFilter(arguments, TemplateEscapingMode.Html);
        });
        RegisterFilter("escape_once", (List<TemplateValue> arguments) =>
        {
            return EscapeHtmlOnceFilter(arguments);
        });
        RegisterFilter("strip_html", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(StripHtmlTags(GetFilterText(arguments)));
        });
        RegisterFilter("striptags", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(StripHtmlTags(GetFilterText(arguments)));
        });
        RegisterFilter("json", (List<TemplateValue> arguments) =>
        {
            return SerializeJsonFilter(arguments);
        });
        RegisterFilter("to_json", (List<TemplateValue> arguments) =>
        {
            return SerializeJsonFilter(arguments);
        });
        RegisterFilter("tojson", (List<TemplateValue> arguments) =>
        {
            return SerializeJsonFilter(arguments);
        });
        RegisterFilter("json_escape", (List<TemplateValue> arguments) =>
        {
            return EncodeFilter(arguments, TemplateEscapingMode.JsonString);
        });
        RegisterFilter("url", (List<TemplateValue> arguments) =>
        {
            return UrlEncodeFilter(arguments);
        });
        RegisterFilter("url_encode", (List<TemplateValue> arguments) =>
        {
            return UrlEncodeFilter(arguments);
        });
        RegisterFilter("urlencode", (List<TemplateValue> arguments) =>
        {
            return UrlEncodeFilter(arguments);
        });
        RegisterFilter("url_decode", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(WebUtility.UrlDecode(GetFilterText(arguments)));
        });
        RegisterFilter("urldecode", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(WebUtility.UrlDecode(GetFilterText(arguments)));
        });
        RegisterFilter("csv", (List<TemplateValue> arguments) =>
        {
            return EncodeFilter(arguments, TemplateEscapingMode.CsvField);
        });
        RegisterFilter("csv_escape", (List<TemplateValue> arguments) =>
        {
            return EncodeFilter(arguments, TemplateEscapingMode.CsvField);
        });
        RegisterFilter("safe", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromSafeString(GetFilterText(arguments));
        });
        RegisterFilter("raw", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromSafeString(GetFilterText(arguments));
        });
        RegisterFilter("abs", (List<TemplateValue> arguments) =>
        {
            decimal number = GetFilterNumber(arguments, 0, 0m);
            if (number < 0m)
            {
                number = 0m - number;
            }

            return TemplateValue.FromNumber(number);
        });
        RegisterFilter("at_least", (List<TemplateValue> arguments) =>
        {
            decimal value = GetFilterNumber(arguments, 0, 0m);
            decimal minimum = GetFilterNumber(arguments, 1, 0m);
            return TemplateValue.FromNumber(value < minimum ? minimum : value);
        });
        RegisterFilter("at_most", (List<TemplateValue> arguments) =>
        {
            decimal value = GetFilterNumber(arguments, 0, 0m);
            decimal maximum = GetFilterNumber(arguments, 1, 0m);
            return TemplateValue.FromNumber(value > maximum ? maximum : value);
        });
        RegisterFilter("ceil", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromNumber(decimal.Ceiling(GetFilterNumber(arguments, 0, 0m)));
        });
        RegisterFilter("ceiling", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromNumber(decimal.Ceiling(GetFilterNumber(arguments, 0, 0m)));
        });
        RegisterFilter("floor", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromNumber(decimal.Floor(GetFilterNumber(arguments, 0, 0m)));
        });
        RegisterFilter("round", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromNumber(RoundFilter(arguments));
        });
        RegisterFilter("plus", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromNumber(GetFilterNumber(arguments, 0, 0m) + GetFilterNumber(arguments, 1, 0m));
        });
        RegisterFilter("minus", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromNumber(GetFilterNumber(arguments, 0, 0m) - GetFilterNumber(arguments, 1, 0m));
        });
        RegisterFilter("times", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromNumber(GetFilterNumber(arguments, 0, 0m) * GetFilterNumber(arguments, 1, 1m));
        });
        RegisterFilter("multiply", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromNumber(GetFilterNumber(arguments, 0, 0m) * GetFilterNumber(arguments, 1, 1m));
        });
        RegisterFilter("divide", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromNumber(DivideFilter(arguments));
        });
        RegisterFilter("divided_by", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromNumber(DivideFilter(arguments));
        });
        RegisterFilter("modulo", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromNumber(ModuloFilter(arguments));
        });
        RegisterFilter("mod", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromNumber(ModuloFilter(arguments));
        });
        RegisterFilter("format", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(FormatValueFilter(arguments));
        });
        RegisterFilter("number_format", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(FormatNumberFilter(arguments));
        });
        RegisterFilter("date", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(FormatTemporalFilter(arguments, "d"));
        });
        RegisterFilter("date_format", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(FormatTemporalFilter(arguments, "d"));
        });
        RegisterFilter("format_date", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(FormatTemporalFilter(arguments, "d"));
        });
        RegisterFilter("time", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(FormatTemporalFilter(arguments, "c"));
        });
        RegisterFilter("time_format", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(FormatTemporalFilter(arguments, "c"));
        });
        RegisterFilter("format_time", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(FormatTemporalFilter(arguments, "c"));
        });
        RegisterFilter("datetime", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(FormatTemporalFilter(arguments, "G"));
        });
        RegisterFilter("date_time", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(FormatTemporalFilter(arguments, "G"));
        });
        RegisterFilter("datetime_format", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(FormatTemporalFilter(arguments, "G"));
        });
        RegisterFilter("format_datetime", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromString(FormatTemporalFilter(arguments, "G"));
        });
        RegisterFilter("first", (List<TemplateValue> arguments) =>
        {
            return FirstFilter(arguments);
        });
        RegisterFilter("last", (List<TemplateValue> arguments) =>
        {
            return LastFilter(arguments);
        });
        RegisterFilter("size", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromNumber(SizeFilter(arguments));
        });
        RegisterFilter("length", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromNumber(SizeFilter(arguments));
        });
        RegisterFilter("reverse", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromList(ReverseFilter(arguments));
        });
        RegisterFilter("sort", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromList(SortFilter(arguments, false));
        });
        RegisterFilter("sort_natural", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromList(SortFilter(arguments, true));
        });
        RegisterFilter("sortNatural", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromList(SortFilter(arguments, true));
        });
        RegisterFilter("uniq", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromList(UniqFilter(arguments));
        });
        RegisterFilter("unique", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromList(UniqFilter(arguments));
        });
        RegisterFilter("compact", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromList(CompactFilter(arguments));
        });
        RegisterFilter("concat", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromList(ConcatFilter(arguments));
        });
        RegisterFilter("flatten", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromList(FlattenFilter(arguments));
        });
        RegisterFilter("map", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromList(MapFilter(arguments));
        });
        RegisterFilter("where", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromList(WhereFilter(arguments));
        });
        RegisterFilter("sum", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromNumber(SumFilter(arguments));
        });
        RegisterFilter("keys", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromList(KeysFilter(arguments));
        });
        RegisterFilter("values", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromList(ValuesFilter(arguments));
        });
        RegisterFilter("has_key", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(HasKeyFilter(arguments));
        });
        RegisterFilter("hasKey", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(HasKeyFilter(arguments));
        });
        RegisterFilter("haskey", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(HasKeyFilter(arguments));
        });
        RegisterFilter("default", (List<TemplateValue> arguments) =>
        {
            return DefaultFilter(arguments);
        });
        RegisterFilter("d", (List<TemplateValue> arguments) =>
        {
            return DefaultFilter(arguments);
        });
        RegisterFilter("default_if_none", (List<TemplateValue> arguments) =>
        {
            return DefaultIfNoneFilter(arguments);
        });
    }

    internal void AddBuiltInTests()
    {
        RegisterTest("defined", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && !arguments[0].IsMissing());
        });
        RegisterTest("missing", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count == 0 || arguments[0].IsMissing());
        });
        RegisterTest("undefined", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count == 0 || arguments[0].IsMissing());
        });
        RegisterTest("null", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && arguments[0].IsNull());
        });
        RegisterTest("none", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && arguments[0].IsNull());
        });
        RegisterTest("empty", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && IsEmptyValue(arguments[0]));
        });
        RegisterTest("truthy", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && TemplateTruthiness.IsTruthy(arguments[0], Options));
        });
        RegisterTest("falsy", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count == 0 || !TemplateTruthiness.IsTruthy(arguments[0], Options));
        });
        RegisterTest("true", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && arguments[0].Kind == TemplateValueKind.Boolean && arguments[0].BooleanValue);
        });
        RegisterTest("false", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && arguments[0].Kind == TemplateValueKind.Boolean && !arguments[0].BooleanValue);
        });
        RegisterTest("not defined", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count == 0 || arguments[0].IsMissing());
        });
        RegisterTest("not null", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count == 0 || !arguments[0].IsNull());
        });
        RegisterTest("not empty", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count == 0 || !IsEmptyValue(arguments[0]));
        });
        RegisterTest("number", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && arguments[0].Kind == TemplateValueKind.Number);
        });
        RegisterTest("string", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && arguments[0].Kind == TemplateValueKind.String);
        });
        RegisterTest("safe", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && arguments[0].IsSafeString());
        });
        RegisterTest("boolean", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && arguments[0].Kind == TemplateValueKind.Boolean);
        });
        RegisterTest("bool", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && arguments[0].Kind == TemplateValueKind.Boolean);
        });
        RegisterTest("list", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && arguments[0].Kind == TemplateValueKind.List);
        });
        RegisterTest("array", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && arguments[0].Kind == TemplateValueKind.List);
        });
        RegisterTest("map", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && arguments[0].Kind == TemplateValueKind.Map);
        });
        RegisterTest("mapping", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && arguments[0].Kind == TemplateValueKind.Map);
        });
        RegisterTest("sequence", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && IsSequenceValue(arguments[0]));
        });
        RegisterTest("iterable", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && IsIterableValue(arguments[0]));
        });
        RegisterTest("object", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(arguments.Count > 0 && arguments[0].Kind == TemplateValueKind.Object);
        });
        RegisterTest("odd", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(IsOddTest(arguments));
        });
        RegisterTest("even", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(IsEvenTest(arguments));
        });
        RegisterTest("divisible_by", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(IsDivisibleByTest(arguments));
        });
        RegisterTest("divisibleby", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(IsDivisibleByTest(arguments));
        });
        RegisterTest("divisible by", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(IsDivisibleByTest(arguments));
        });
        RegisterTest("equal_to", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(IsEqualToTest(arguments));
        });
        RegisterTest("equalto", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(IsEqualToTest(arguments));
        });
        RegisterTest("equal to", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(IsEqualToTest(arguments));
        });
        RegisterTest("eq", (List<TemplateValue> arguments) =>
        {
            return TemplateValue.FromBoolean(IsEqualToTest(arguments));
        });
    }

    internal string GetFilterText(List<TemplateValue> arguments)
    {
        return GetFilterTextAtIndex(arguments, 0);
    }

    internal TemplateValue CreateTextFilterResult(List<TemplateValue> arguments, string text)
    {
        if ((Options.SyntaxProfile == "jinja" || Options.SyntaxProfile == "django" || Options.SyntaxProfile == "nunjucks") && arguments.Count > 0 && arguments[0].IsSafeString())
        {
            return TemplateValue.FromSafeString(text);
        }

        return TemplateValue.FromString(text);
    }

    internal bool IsOddTest(List<TemplateValue> arguments)
    {
        if (arguments.Count == 0)
        {
            return false;
        }

        decimal number = ToNumber(arguments[0]);
        if (!IsIntegerDecimal(number))
        {
            return false;
        }

        decimal remainder = number % 2m;
        if (remainder < 0m)
        {
            remainder = 0m - remainder;
        }

        return remainder == 1m;
    }

    internal bool IsEvenTest(List<TemplateValue> arguments)
    {
        if (arguments.Count == 0)
        {
            return false;
        }

        decimal number = ToNumber(arguments[0]);
        return IsIntegerDecimal(number) && number % 2m == 0m;
    }

    internal bool IsDivisibleByTest(List<TemplateValue> arguments)
    {
        if (arguments.Count < 2)
        {
            return false;
        }

        decimal divisor = ToNumber(arguments[1]);
        if (divisor == 0m)
        {
            return false;
        }

        return ToNumber(arguments[0]) % divisor == 0m;
    }

    internal bool IsEqualToTest(List<TemplateValue> arguments)
    {
        if (arguments.Count < 2)
        {
            return false;
        }

        return ValuesEqual(arguments[0], arguments[1]);
    }

    internal bool IsIntegerDecimal(decimal number)
    {
        return decimal.Truncate(number) == number;
    }

    internal bool IsSequenceValue(TemplateValue value)
    {
        TemplateValue normalized = NormalizeFilterValue(value);
        return normalized.IsStringLike() || normalized.Kind == TemplateValueKind.List || normalized.Kind == TemplateValueKind.Fragment;
    }

    internal bool IsIterableValue(TemplateValue value)
    {
        TemplateValue normalized = NormalizeFilterValue(value);
        return IsSequenceValue(normalized) || normalized.Kind == TemplateValueKind.Map;
    }

    internal string GetFilterTextAtIndex(List<TemplateValue> arguments, int index)
    {
        if (index < 0 || index >= arguments.Count)
        {
            return "";
        }

        return ToText(arguments[index]);
    }

    internal bool GetFilterBoolean(List<TemplateValue> arguments, int index, bool fallback)
    {
        if (index < 0 || index >= arguments.Count)
        {
            return fallback;
        }

        TemplateValue value = arguments[index];
        if (value.Kind == TemplateValueKind.Boolean)
        {
            return value.BooleanValue;
        }

        return TemplateTruthiness.IsTruthy(value, Options);
    }

    internal string GetIndentText(List<TemplateValue> arguments)
    {
        if (arguments.Count <= 1)
        {
            return "    ";
        }

        TemplateValue width = arguments[1];
        if (width.IsStringLike())
        {
            return width.StringValue;
        }

        int count = GetFilterInteger(arguments, 1, 4);
        if (count <= 0)
        {
            return "";
        }

        return new string(' ', count);
    }

    internal string IndentFilter(List<TemplateValue> arguments)
    {
        string text = GetFilterText(arguments);
        string indentation = GetIndentText(arguments);
        bool indentFirst = GetFilterBoolean(arguments, 2, false);
        bool indentBlank = GetFilterBoolean(arguments, 3, false);
        StringBuilder builder = new StringBuilder();
        int lineNumber = 0;
        int index = 0;
        while (index < text.Length)
        {
            int lineStart = index;
            while (index < text.Length && text[index] != '\r' && text[index] != '\n')
            {
                index = index + 1;
            }

            int lineLength = index - lineStart;
            if ((lineNumber > 0 || indentFirst) && (lineLength > 0 || indentBlank))
            {
                builder.Append(indentation);
            }

            builder.Append(text.Substring(lineStart, lineLength));
            if (index < text.Length)
            {
                if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                {
                    builder.Append("\r\n");
                    index = index + 2;
                }
                else
                {
                    builder.Append(text[index]);
                    index = index + 1;
                }
            }

            lineNumber = lineNumber + 1;
        }

        return builder.ToString();
    }

    internal int GetFilterInteger(List<TemplateValue> arguments, int index, int fallback)
    {
        if (index < 0 || index >= arguments.Count)
        {
            return fallback;
        }

        decimal number = ToNumber(arguments[index]);
        if (number < -2147483648m)
        {
            return -2147483648;
        }

        if (number > 2147483647m)
        {
            return 2147483647;
        }

        return (int)number;
    }

    internal decimal GetFilterNumber(List<TemplateValue> arguments, int index, decimal fallback)
    {
        if (index < 0 || index >= arguments.Count)
        {
            return fallback;
        }

        return ToNumber(arguments[index]);
    }

    internal TemplateValue EncodeFilter(List<TemplateValue> arguments, TemplateEscapingMode mode)
    {
        return TemplateValue.FromSafeString(TemplateOutputEncoder.Encode(GetFilterText(arguments), mode));
    }

    internal TemplateValue UrlEncodeFilter(List<TemplateValue> arguments)
    {
        string text = GetFilterText(arguments);
        if (Options.SyntaxProfile == "liquid")
        {
            string? encoded = WebUtility.UrlEncode(text);
            if (encoded == null)
            {
                return TemplateValue.FromSafeString("");
            }

            return TemplateValue.FromSafeString(encoded);
        }

        return TemplateValue.FromSafeString(TemplateOutputEncoder.EncodeUrl(text));
    }

    internal TemplateValue SerializeJsonFilter(List<TemplateValue> arguments)
    {
        return TemplateValue.FromSafeString(TemplateJsonSerializer.Serialize(GetFilterValue(arguments, 0)));
    }

    internal TemplateValue EscapeHtmlOnceFilter(List<TemplateValue> arguments)
    {
        return TemplateValue.FromSafeString(EscapeHtmlOnce(GetFilterText(arguments)));
    }

    internal string EscapeHtmlOnce(string text)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            int entityLength = GetHtmlEntityLength(text, index);
            if (entityLength > 0)
            {
                builder.Append(text.Substring(index, entityLength));
                index = index + entityLength;
            }
            else
            {
                builder.Append(TemplateOutputEncoder.EncodeHtml(text[index].ToString()));
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    internal int GetHtmlEntityLength(string text, int index)
    {
        if (index < 0 || index >= text.Length || text[index] != '&')
        {
            return 0;
        }

        int semicolonIndex = text.IndexOf(';', index + 1);
        if (semicolonIndex < 0 || semicolonIndex - index > 32)
        {
            return 0;
        }

        string candidate = text.Substring(index, semicolonIndex - index + 1);
        string? decoded = WebUtility.HtmlDecode(candidate);
        if (decoded == null || decoded == candidate)
        {
            return 0;
        }

        return candidate.Length;
    }

    internal string StripHtmlTags(string text)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            if (index + 3 < text.Length && text.Substring(index, 4) == "<!--")
            {
                int commentEnd = text.IndexOf("-->", index + 4, StringComparison.Ordinal);
                if (commentEnd < 0)
                {
                    break;
                }

                index = commentEnd + 3;
            }
            else if (text[index] == '<')
            {
                int tagEnd = FindHtmlTagEnd(text, index + 1);
                if (tagEnd < 0)
                {
                    builder.Append(text[index]);
                    index = index + 1;
                }
                else
                {
                    index = tagEnd + 1;
                }
            }
            else
            {
                builder.Append(text[index]);
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    internal int FindHtmlTagEnd(string text, int startIndex)
    {
        char quote = '\0';
        int index = startIndex;
        while (index < text.Length)
        {
            char current = text[index];
            if (quote == '\0' && (current == '\'' || current == '"'))
            {
                quote = current;
            }
            else if (quote != '\0' && current == quote)
            {
                quote = '\0';
            }
            else if (quote == '\0' && current == '>')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal decimal RoundFilter(List<TemplateValue> arguments)
    {
        decimal number = GetFilterNumber(arguments, 0, 0m);
        int digits = GetFilterInteger(arguments, 1, 0);
        if (digits < 0)
        {
            digits = 0;
        }

        if (digits > 28)
        {
            digits = 28;
        }

        return Math.Round(number, digits, MidpointRounding.AwayFromZero);
    }

    internal decimal DivideFilter(List<TemplateValue> arguments)
    {
        decimal dividend = GetFilterNumber(arguments, 0, 0m);
        decimal divisor = GetFilterNumber(arguments, 1, 1m);
        decimal result = dividend / divisor;
        if (Options.SyntaxProfile == "liquid" && IsWholeNumberRepresentation(divisor))
        {
            return decimal.Floor(result);
        }

        return result;
    }

    internal decimal ModuloFilter(List<TemplateValue> arguments)
    {
        decimal dividend = GetFilterNumber(arguments, 0, 0m);
        decimal divisor = GetFilterNumber(arguments, 1, 1m);
        decimal result = dividend % divisor;
        if (Options.SyntaxProfile == "liquid" && result != 0m && ((result < 0m && divisor > 0m) || (result > 0m && divisor < 0m)))
        {
            result = result + divisor;
        }

        return result;
    }

    internal bool IsWholeNumberRepresentation(decimal number)
    {
        int[] bits = decimal.GetBits(number);
        int scale = (bits[3] >> 16) & 255;
        return scale == 0;
    }

    internal string FormatValueFilter(List<TemplateValue> arguments)
    {
        TemplateValue value = GetFilterValue(arguments, 0);
        if (value.ObjectValue != null)
        {
            IFormattable? formattable = value.ObjectValue as IFormattable;
            if (formattable != null)
            {
                return FormatFormattable(formattable, GetFilterTextAtIndex(arguments, 1));
            }
        }

        return FormatNumberFilter(arguments);
    }

    internal string FormatNumberFilter(List<TemplateValue> arguments)
    {
        decimal number = GetFilterNumber(arguments, 0, 0m);
        string format = GetFilterTextAtIndex(arguments, 1);
        if (format == "")
        {
            return number.ToString(Options.Culture);
        }

        return number.ToString(format, Options.Culture);
    }

    internal string FormatTemporalFilter(List<TemplateValue> arguments, string defaultFormat)
    {
        TemplateValue value = GetFilterValue(arguments, 0);
        string format = GetFilterTextAtIndex(arguments, 1);
        if (format == "")
        {
            format = defaultFormat;
        }

        format = NormalizeTemporalFormat(format);

        if (value.ObjectValue != null)
        {
            IFormattable? formattable = value.ObjectValue as IFormattable;
            if (formattable != null)
            {
                return FormatFormattable(formattable, format);
            }
        }

        if (value.Kind == TemplateValueKind.Number)
        {
            if (value.NumberValue < -62135596800m || value.NumberValue > 253402300799m)
            {
                if (Options.StrictTypeConversions)
                {
                    throw new InvalidOperationException("Value cannot be converted to a date or time.");
                }

                return "";
            }

            DateTimeOffset instant = DateTimeOffset.FromUnixTimeSeconds((long)value.NumberValue);
            return instant.ToString(format, Options.Culture);
        }

        string text = ToText(value);
        TimeSpan timeSpan = new TimeSpan();
        if (TimeSpan.TryParse(text, Options.Culture, out timeSpan))
        {
            return timeSpan.ToString(format, Options.Culture);
        }

        DateTimeOffset dateTimeOffset = new DateTimeOffset();
        if (DateTimeOffset.TryParse(text, Options.Culture, DateTimeStyles.None, out dateTimeOffset))
        {
            return dateTimeOffset.ToString(format, Options.Culture);
        }

        if (Options.StrictTypeConversions)
        {
            throw new InvalidOperationException("Value cannot be converted to a date or time.");
        }

        return "";
    }

    internal string NormalizeTemporalFormat(string format)
    {
        if (Options.SyntaxProfile != "liquid")
        {
            return format;
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < format.Length)
        {
            if (format[index] != '%' || index + 1 >= format.Length)
            {
                builder.Append(format[index]);
                index = index + 1;
            }
            else
            {
                char token = format[index + 1];
                string replacement = GetTemporalFormatReplacement(token);
                if (replacement == "")
                {
                    builder.Append("'%");
                    builder.Append(token);
                    builder.Append("'");
                }
                else
                {
                    builder.Append(replacement);
                }

                index = index + 2;
            }
        }

        return builder.ToString();
    }

    internal string GetTemporalFormatReplacement(char token)
    {
        if (token == '%')
        {
            return "'%'";
        }

        if (token == 'Y')
        {
            return "yyyy";
        }

        if (token == 'y')
        {
            return "yy";
        }

        if (token == 'm')
        {
            return "MM";
        }

        if (token == 'd')
        {
            return "dd";
        }

        if (token == 'H')
        {
            return "HH";
        }

        if (token == 'I')
        {
            return "hh";
        }

        if (token == 'M')
        {
            return "mm";
        }

        if (token == 'S')
        {
            return "ss";
        }

        if (token == 'p')
        {
            return "tt";
        }

        if (token == 'a')
        {
            return "ddd";
        }

        if (token == 'A')
        {
            return "dddd";
        }

        if (token == 'b')
        {
            return "MMM";
        }

        if (token == 'B')
        {
            return "MMMM";
        }

        if (token == 'z')
        {
            return "zzz";
        }

        return "";
    }

    internal TemplateValue GetFilterValue(List<TemplateValue> arguments, int index)
    {
        if (index < 0 || index >= arguments.Count)
        {
            return TemplateValue.Missing();
        }

        return arguments[index];
    }

    internal string FormatFormattable(IFormattable formattable, string format)
    {
        if (format == "")
        {
            return formattable.ToString(null, Options.Culture);
        }

        return formattable.ToString(format, Options.Culture);
    }

    internal string CapitalizeText(string text)
    {
        if (text.Length == 0)
        {
            return text;
        }

        string first = text.Substring(0, 1).ToUpper(Options.Culture);
        if (text.Length == 1)
        {
            return first;
        }

        return first + text.Substring(1).ToLower(Options.Culture);
    }

    internal string ReplaceFirst(string text, string oldValue, string newValue)
    {
        if (oldValue == "")
        {
            return text;
        }

        int index = text.IndexOf(oldValue, StringComparison.Ordinal);
        if (index < 0)
        {
            return text;
        }

        return text.Substring(0, index) + newValue + text.Substring(index + oldValue.Length);
    }

    internal string NewlineToBr(string text)
    {
        string result = text.Replace("\r\n", "\n");
        result = result.Replace("\r", "\n");
        return result.Replace("\n", "<br>");
    }

    internal TemplateValue SplitFilter(List<TemplateValue> arguments)
    {
        string text = GetFilterText(arguments);
        string separator = GetFilterTextAtIndex(arguments, 1);
        List<TemplateValue> values = new List<TemplateValue>();
        if (separator == "")
        {
            int charIndex = 0;
            while (charIndex < text.Length)
            {
                values.Add(TemplateValue.FromString(text[charIndex].ToString()));
                charIndex = charIndex + 1;
            }

            return TemplateValue.FromList(values);
        }

        string[] parts = text.Split(new string[] { separator }, StringSplitOptions.None);
        int index = 0;
        while (index < parts.Length)
        {
            values.Add(TemplateValue.FromString(parts[index]));
            index = index + 1;
        }

        return TemplateValue.FromList(values);
    }

    internal string JoinFilter(List<TemplateValue> arguments)
    {
        if (arguments.Count == 0)
        {
            return "";
        }

        string separator = GetFilterTextAtIndex(arguments, 1);
        TemplateValue value = arguments[0];
        if (value.Kind != TemplateValueKind.List)
        {
            return ToText(value);
        }

        string result = "";
        int index = 0;
        while (index < value.ListValue.Count)
        {
            if (index > 0)
            {
                result = result + separator;
            }

            result = result + ToText(value.ListValue[index]);
            index = index + 1;
        }

        return result;
    }

    internal TemplateValue FirstFilter(List<TemplateValue> arguments)
    {
        List<TemplateValue> items = GetFilterItems(GetFilterValue(arguments, 0));
        if (items.Count == 0)
        {
            return TemplateValue.Missing();
        }

        return items[0];
    }

    internal TemplateValue LastFilter(List<TemplateValue> arguments)
    {
        List<TemplateValue> items = GetFilterItems(GetFilterValue(arguments, 0));
        if (items.Count == 0)
        {
            return TemplateValue.Missing();
        }

        return items[items.Count - 1];
    }

    internal decimal SizeFilter(List<TemplateValue> arguments)
    {
        TemplateValue value = NormalizeFilterValue(GetFilterValue(arguments, 0));
        if (value.IsStringLike())
        {
            return value.StringValue.Length;
        }

        if (value.Kind == TemplateValueKind.List)
        {
            return value.ListValue.Count;
        }

        if (value.Kind == TemplateValueKind.Map)
        {
            return value.MapValue.Count;
        }

        if (value.Kind == TemplateValueKind.Fragment)
        {
            return value.FragmentValue.Count;
        }

        TemplateValue size = MemberResolver.ResolveMember(value, "size");
        if (size.Kind == TemplateValueKind.Number)
        {
            return size.NumberValue;
        }

        return 0m;
    }

    internal List<TemplateValue> ReverseFilter(List<TemplateValue> arguments)
    {
        List<TemplateValue> items = GetFilterItems(GetFilterValue(arguments, 0));
        List<TemplateValue> reversed = new List<TemplateValue>();
        int index = items.Count - 1;
        while (index >= 0)
        {
            reversed.Add(items[index]);
            index = index - 1;
        }

        return reversed;
    }

    internal List<TemplateValue> SortFilter(List<TemplateValue> arguments, bool natural)
    {
        List<TemplateValue> items = GetFilterItems(GetFilterValue(arguments, 0));
        List<TemplateValue> sorted = new List<TemplateValue>();
        int copyIndex = 0;
        while (copyIndex < items.Count)
        {
            sorted.Add(items[copyIndex]);
            copyIndex = copyIndex + 1;
        }

        string memberName = GetFilterTextAtIndex(arguments, 1);
        int index = 1;
        while (index < sorted.Count)
        {
            TemplateValue current = sorted[index];
            int insertIndex = index - 1;
            while (insertIndex >= 0 && CompareSortValues(sorted[insertIndex], current, memberName, natural) > 0)
            {
                sorted[insertIndex + 1] = sorted[insertIndex];
                insertIndex = insertIndex - 1;
            }

            sorted[insertIndex + 1] = current;
            index = index + 1;
        }

        return sorted;
    }

    internal List<TemplateValue> UniqFilter(List<TemplateValue> arguments)
    {
        List<TemplateValue> items = GetFilterItems(GetFilterValue(arguments, 0));
        List<TemplateValue> unique = new List<TemplateValue>();
        string memberName = GetFilterTextAtIndex(arguments, 1);
        int index = 0;
        while (index < items.Count)
        {
            bool exists = false;
            int uniqueIndex = 0;
            while (uniqueIndex < unique.Count)
            {
                if (ValuesEqual(SelectFilterMember(items[index], memberName), SelectFilterMember(unique[uniqueIndex], memberName)))
                {
                    exists = true;
                }

                uniqueIndex = uniqueIndex + 1;
            }

            if (!exists)
            {
                unique.Add(items[index]);
            }

            index = index + 1;
        }

        return unique;
    }

    internal List<TemplateValue> CompactFilter(List<TemplateValue> arguments)
    {
        List<TemplateValue> items = GetFilterItems(GetFilterValue(arguments, 0));
        List<TemplateValue> compacted = new List<TemplateValue>();
        string memberName = GetFilterTextAtIndex(arguments, 1);
        int index = 0;
        while (index < items.Count)
        {
            TemplateValue selected = SelectFilterMember(items[index], memberName);
            if (!IsNullishValue(selected))
            {
                compacted.Add(items[index]);
            }

            index = index + 1;
        }

        return compacted;
    }

    internal List<TemplateValue> MapFilter(List<TemplateValue> arguments)
    {
        List<TemplateValue> items = GetFilterItems(GetFilterValue(arguments, 0));
        List<TemplateValue> mapped = new List<TemplateValue>();
        string memberName = GetFilterTextAtIndex(arguments, 1);
        int index = 0;
        while (index < items.Count)
        {
            mapped.Add(SelectFilterMember(items[index], memberName));
            index = index + 1;
        }

        return mapped;
    }

    internal List<TemplateValue> WhereFilter(List<TemplateValue> arguments)
    {
        List<TemplateValue> items = GetFilterItems(GetFilterValue(arguments, 0));
        List<TemplateValue> matches = new List<TemplateValue>();
        string memberName = GetFilterTextAtIndex(arguments, 1);
        bool hasExpectedValue = arguments.Count > 2;
        TemplateValue expectedValue = GetFilterValue(arguments, 2);
        int index = 0;
        while (index < items.Count)
        {
            TemplateValue selected = SelectFilterMember(items[index], memberName);
            bool include = false;
            if (hasExpectedValue)
            {
                include = ValuesEqual(selected, expectedValue);
            }
            else
            {
                include = TemplateTruthiness.IsTruthy(selected, Options);
            }

            if (include)
            {
                matches.Add(items[index]);
            }

            index = index + 1;
        }

        return matches;
    }

    internal decimal SumFilter(List<TemplateValue> arguments)
    {
        List<TemplateValue> items = GetFilterItems(GetFilterValue(arguments, 0));
        string memberName = GetFilterTextAtIndex(arguments, 1);
        decimal total = 0m;
        int index = 0;
        while (index < items.Count)
        {
            total = total + ToNumber(SelectFilterMember(items[index], memberName));
            index = index + 1;
        }

        return total;
    }

    internal List<TemplateValue> KeysFilter(List<TemplateValue> arguments)
    {
        TemplateValue value = NormalizeFilterValue(GetFilterValue(arguments, 0));
        List<TemplateValue> keys = new List<TemplateValue>();
        if (value.Kind == TemplateValueKind.Map)
        {
            foreach (KeyValuePair<string, TemplateValue> pair in value.MapValue)
            {
                keys.Add(TemplateValue.FromString(pair.Key));
            }
        }

        return keys;
    }

    internal List<TemplateValue> ValuesFilter(List<TemplateValue> arguments)
    {
        TemplateValue value = NormalizeFilterValue(GetFilterValue(arguments, 0));
        if (value.Kind == TemplateValueKind.Map)
        {
            List<TemplateValue> values = new List<TemplateValue>();
            foreach (KeyValuePair<string, TemplateValue> pair in value.MapValue)
            {
                values.Add(pair.Value);
            }

            return values;
        }

        return GetFilterItems(value);
    }

    internal bool HasKeyFilter(List<TemplateValue> arguments)
    {
        TemplateValue value = NormalizeFilterValue(GetFilterValue(arguments, 0));
        string key = GetFilterTextAtIndex(arguments, 1);
        if (value.Kind != TemplateValueKind.Map)
        {
            return false;
        }

        return value.MapValue.ContainsKey(key);
    }

    internal TemplateValue DefaultFilter(List<TemplateValue> arguments)
    {
        TemplateValue value = GetFilterValue(arguments, 0);
        bool useDefault = value.IsMissing() || value.IsNull() || IsEmptyValue(value);
        if (Options.SyntaxProfile == "liquid")
        {
            if (value.Kind == TemplateValueKind.Boolean && !value.BooleanValue && !GetNamedBooleanArgument(arguments, "allow_false"))
            {
                useDefault = true;
            }
        }
        else if (Options.SyntaxProfile == "jinja" || Options.SyntaxProfile == "nunjucks")
        {
            useDefault = value.IsMissing();
            if (arguments.Count > 2 && TemplateTruthiness.IsTruthy(arguments[2], Options))
            {
                useDefault = !TemplateTruthiness.IsTruthy(value, Options);
            }
        }
        else if (Options.SyntaxProfile == "django")
        {
            useDefault = !TemplateTruthiness.IsTruthy(value, Options);
        }

        if (useDefault)
        {
            if (arguments.Count > 1)
            {
                return arguments[1];
            }

            return TemplateValue.FromString("");
        }

        return value;
    }

    internal TemplateValue DefaultIfNoneFilter(List<TemplateValue> arguments)
    {
        TemplateValue value = GetFilterValue(arguments, 0);
        if (value.IsNull())
        {
            return GetFilterValue(arguments, 1);
        }

        return value;
    }

    internal bool GetNamedBooleanArgument(List<TemplateValue> arguments, string name)
    {
        if (arguments.Count == 0)
        {
            return false;
        }

        TemplateValue candidate = arguments[arguments.Count - 1];
        if (candidate.Kind != TemplateValueKind.Map || !candidate.MapValue.ContainsKey(name))
        {
            return false;
        }

        return TemplateTruthiness.IsTruthy(candidate.MapValue[name], Options);
    }

    internal List<TemplateValue> GetFilterItems(TemplateValue value)
    {
        TemplateValue normalized = NormalizeFilterValue(value);
        List<TemplateValue> items = new List<TemplateValue>();
        if (normalized.Kind == TemplateValueKind.List)
        {
            int index = 0;
            while (index < normalized.ListValue.Count)
            {
                items.Add(normalized.ListValue[index]);
                index = index + 1;
            }

            return items;
        }

        if (normalized.Kind == TemplateValueKind.Map)
        {
            foreach (KeyValuePair<string, TemplateValue> pair in normalized.MapValue)
            {
                items.Add(pair.Value);
            }

            return items;
        }

        if (normalized.IsStringLike())
        {
            int charIndex = 0;
            while (charIndex < normalized.StringValue.Length)
            {
                items.Add(TemplateValue.FromString(normalized.StringValue[charIndex].ToString()));
                charIndex = charIndex + 1;
            }
        }

        return items;
    }

    internal List<TemplateValue> ConcatFilter(List<TemplateValue> arguments)
    {
        List<TemplateValue> values = GetFilterItems(GetFilterValue(arguments, 0));
        List<TemplateValue> additional = GetFilterItems(GetFilterValue(arguments, 1));
        int index = 0;
        while (index < additional.Count)
        {
            values.Add(additional[index]);
            index = index + 1;
        }

        return values;
    }

    internal List<TemplateValue> FlattenFilter(List<TemplateValue> arguments)
    {
        List<TemplateValue> flattened = new List<TemplateValue>();
        AddFlattenedValues(flattened, GetFilterValue(arguments, 0));
        return flattened;
    }

    internal void AddFlattenedValues(List<TemplateValue> flattened, TemplateValue value)
    {
        TemplateValue normalized = NormalizeFilterValue(value);
        if (normalized.Kind != TemplateValueKind.List)
        {
            flattened.Add(normalized);
            return;
        }

        int index = 0;
        while (index < normalized.ListValue.Count)
        {
            AddFlattenedValues(flattened, normalized.ListValue[index]);
            index = index + 1;
        }
    }

    internal TemplateValue NormalizeFilterValue(TemplateValue value)
    {
        if (value.Kind == TemplateValueKind.Object && value.ObjectValue != null)
        {
            TemplateValue converted = TemplateValueConverter.Convert(value.ObjectValue);
            if (converted.Kind != TemplateValueKind.Object)
            {
                return converted;
            }
        }

        return value;
    }

    internal TemplateValue SelectFilterMember(TemplateValue value, string memberName)
    {
        if (memberName == "")
        {
            return value;
        }

        return MemberResolver.ResolveMember(value, memberName);
    }

    internal int CompareSortValues(TemplateValue left, TemplateValue right, string memberName, bool natural)
    {
        TemplateValue selectedLeft = SelectFilterMember(left, memberName);
        TemplateValue selectedRight = SelectFilterMember(right, memberName);
        if (selectedLeft.Kind == TemplateValueKind.Number && selectedRight.Kind == TemplateValueKind.Number)
        {
            if (selectedLeft.NumberValue < selectedRight.NumberValue)
            {
                return -1;
            }

            if (selectedLeft.NumberValue > selectedRight.NumberValue)
            {
                return 1;
            }

            return 0;
        }

        if (natural)
        {
            return string.Compare(ToText(selectedLeft), ToText(selectedRight), true, Options.Culture);
        }

        return string.Compare(ToText(selectedLeft), ToText(selectedRight), StringComparison.Ordinal);
    }

    internal bool IsNullishValue(TemplateValue value)
    {
        return value.IsMissing() || value.IsNull();
    }

    internal TemplateValue SliceValueFilter(List<TemplateValue> arguments)
    {
        TemplateValue value = NormalizeFilterValue(GetFilterValue(arguments, 0));
        if (value.Kind == TemplateValueKind.List)
        {
            int listStart = GetFilterInteger(arguments, 1, 0);
            int listLength = GetFilterInteger(arguments, 2, value.ListValue.Count);
            if (listStart < 0)
            {
                listStart = value.ListValue.Count + listStart;
            }

            if (listStart < 0)
            {
                listStart = 0;
            }

            List<TemplateValue> sliced = new List<TemplateValue>();
            if (listStart >= value.ListValue.Count || listLength <= 0)
            {
                return TemplateValue.FromList(sliced);
            }

            int listIndex = listStart;
            int listEnd = value.ListValue.Count;
            if (listLength < value.ListValue.Count - listStart)
            {
                listEnd = listStart + listLength;
            }

            while (listIndex < listEnd)
            {
                sliced.Add(value.ListValue[listIndex]);
                listIndex = listIndex + 1;
            }

            return TemplateValue.FromList(sliced);
        }

        string text = ToText(value);
        int start = GetFilterInteger(arguments, 1, 0);
        int length = GetFilterInteger(arguments, 2, text.Length);
        if (start < 0)
        {
            start = text.Length + start;
        }

        if (start < 0)
        {
            start = 0;
        }

        if (start >= text.Length || length <= 0)
        {
            return TemplateValue.FromString("");
        }

        if (length > text.Length - start)
        {
            length = text.Length - start;
        }

        return TemplateValue.FromString(text.Substring(start, length));
    }

    internal string TruncateFilter(List<TemplateValue> arguments)
    {
        string text = GetFilterText(arguments);
        int length = GetFilterInteger(arguments, 1, 50);
        string suffix = GetFilterTextAtIndex(arguments, 2);
        if (arguments.Count < 3)
        {
            suffix = "...";
        }

        if (length < 0)
        {
            length = 0;
        }

        if (text.Length <= length)
        {
            return text;
        }

        if (length <= suffix.Length)
        {
            return text.Substring(0, length);
        }

        return text.Substring(0, length - suffix.Length) + suffix;
    }

    internal string TruncateWordsFilter(List<TemplateValue> arguments)
    {
        string text = GetFilterText(arguments);
        int length = GetFilterInteger(arguments, 1, 15);
        string suffix = GetFilterTextAtIndex(arguments, 2);
        if (arguments.Count < 3)
        {
            suffix = "...";
        }

        List<string> words = SplitWords(text);
        if (length < 0)
        {
            length = 0;
        }

        if (words.Count <= length)
        {
            return text;
        }

        string result = "";
        int index = 0;
        while (index < length)
        {
            if (index > 0)
            {
                result = result + " ";
            }

            result = result + words[index];
            index = index + 1;
        }

        return result + suffix;
    }

    internal List<string> SplitWords(string text)
    {
        List<string> words = new List<string>();
        int index = 0;
        while (index < text.Length)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index = index + 1;
            }

            int start = index;
            while (index < text.Length && !char.IsWhiteSpace(text[index]))
            {
                index = index + 1;
            }

            if (start < index)
            {
                words.Add(text.Substring(start, index - start));
            }
        }

        return words;
    }

    internal bool IsEmptyValue(TemplateValue value)
    {
        if (value.IsMissing() || value.IsNull())
        {
            return true;
        }

        if (value.IsStringLike())
        {
            return value.StringValue.Length == 0;
        }

        if (value.Kind == TemplateValueKind.List)
        {
            return value.ListValue.Count == 0;
        }

        if (value.Kind == TemplateValueKind.Map)
        {
            return value.MapValue.Count == 0;
        }

        if (value.Kind == TemplateValueKind.Fragment)
        {
            return value.FragmentValue.Count == 0;
        }

        return false;
    }

    internal bool ValuesEqual(TemplateValue left, TemplateValue right)
    {
        if (IsLiquidMarker(left, LiquidEmptyMarker) && IsLiquidMarker(right, LiquidEmptyMarker))
        {
            return true;
        }

        if (IsLiquidMarker(left, LiquidBlankMarker) && IsLiquidMarker(right, LiquidBlankMarker))
        {
            return true;
        }

        if (IsLiquidMarker(left, LiquidEmptyMarker))
        {
            return IsLiquidEmptyValue(right);
        }

        if (IsLiquidMarker(right, LiquidEmptyMarker))
        {
            return IsLiquidEmptyValue(left);
        }

        if (IsLiquidMarker(left, LiquidBlankMarker))
        {
            return IsLiquidBlankValue(right);
        }

        if (IsLiquidMarker(right, LiquidBlankMarker))
        {
            return IsLiquidBlankValue(left);
        }

        if ((left.IsMissing() || left.IsNull()) && (right.IsMissing() || right.IsNull()))
        {
            return true;
        }

        if (left.Kind == TemplateValueKind.Number && right.Kind == TemplateValueKind.Number)
        {
            return left.NumberValue == right.NumberValue;
        }

        if (left.Kind == TemplateValueKind.Boolean && right.Kind == TemplateValueKind.Boolean)
        {
            return left.BooleanValue == right.BooleanValue;
        }

        if (left.IsStringLike() || right.IsStringLike())
        {
            return ToText(left) == ToText(right);
        }

        return left.Kind == right.Kind && left.ObjectValue == right.ObjectValue;
    }

    internal bool IsLiquidMarker(TemplateValue value, object marker)
    {
        return value.Kind == TemplateValueKind.Object && value.ObjectValue == marker;
    }

    internal bool IsLiquidEmptyValue(TemplateValue value)
    {
        if (value.IsStringLike())
        {
            return value.StringValue.Length == 0;
        }

        if (value.Kind == TemplateValueKind.List)
        {
            return value.ListValue.Count == 0;
        }

        if (value.Kind == TemplateValueKind.Map)
        {
            return value.MapValue.Count == 0;
        }

        return value.Kind == TemplateValueKind.Fragment && value.FragmentValue.Count == 0;
    }

    internal bool IsLiquidBlankValue(TemplateValue value)
    {
        if (value.IsMissing() || value.IsNull())
        {
            return true;
        }

        if (value.Kind == TemplateValueKind.Boolean)
        {
            return !value.BooleanValue;
        }

        if (value.IsStringLike())
        {
            return value.StringValue.Trim().Length == 0;
        }

        return IsLiquidEmptyValue(value);
    }

    internal bool ValueContains(TemplateValue left, TemplateValue right)
    {
        TemplateValue normalized = NormalizeFilterValue(left);
        if (normalized.IsStringLike())
        {
            return normalized.StringValue.IndexOf(ToText(right), StringComparison.Ordinal) >= 0;
        }

        if (normalized.Kind == TemplateValueKind.List)
        {
            int index = 0;
            while (index < normalized.ListValue.Count)
            {
                if (ValuesEqual(normalized.ListValue[index], right))
                {
                    return true;
                }

                index = index + 1;
            }

            return false;
        }

        if (normalized.Kind == TemplateValueKind.Map)
        {
            return normalized.MapValue.ContainsKey(ToText(right));
        }

        return InvalidOperationBoolean("The contains operator requires a string, list, or map on the left side.");
    }

    internal bool ValueStartsWith(TemplateValue left, TemplateValue right)
    {
        if (!left.IsStringLike())
        {
            return InvalidOperationBoolean("The starts-with operator requires a string on the left side.");
        }

        return left.StringValue.StartsWith(ToText(right), StringComparison.Ordinal);
    }

    internal bool ValueEndsWith(TemplateValue left, TemplateValue right)
    {
        if (!left.IsStringLike())
        {
            return InvalidOperationBoolean("The ends-with operator requires a string on the left side.");
        }

        return left.StringValue.EndsWith(ToText(right), StringComparison.Ordinal);
    }

    internal bool InvalidOperationBoolean(string message)
    {
        if (Options.StrictTypeConversions)
        {
            throw new InvalidOperationException(message);
        }

        return false;
    }

    internal TemplateValue InvalidOperationValue(string message, TemplateSourceSpan? span)
    {
        ReportDiagnostic("EVAL003", TemplateDiagnosticSeverity.Warning, message, span, "Use compatible value types or enable strict conversions to fail immediately.");
        if (Options.StrictTypeConversions)
        {
            throw new InvalidOperationException(message);
        }

        return TemplateValue.Missing();
    }

    internal void ReportDiagnostic(string code, TemplateDiagnosticSeverity severity, string message, TemplateSourceSpan? span, string hint)
    {
        if (RenderContext != null)
        {
            RenderContext.ReportDiagnostic(new TemplateDiagnostic(code, severity, message, span, hint));
        }
    }

    internal int CompareValues(TemplateValue left, TemplateValue right)
    {
        if (left.Kind == TemplateValueKind.Number && right.Kind == TemplateValueKind.Number)
        {
            if (left.NumberValue < right.NumberValue)
            {
                return -1;
            }

            if (left.NumberValue > right.NumberValue)
            {
                return 1;
            }

            return 0;
        }

        return string.Compare(ToText(left), ToText(right), StringComparison.Ordinal);
    }

    internal decimal ToNumber(TemplateValue value)
    {
        if (value.Kind == TemplateValueKind.Number)
        {
            return value.NumberValue;
        }

        if (value.Kind == TemplateValueKind.Boolean)
        {
            if (value.BooleanValue)
            {
                return 1m;
            }

            return 0m;
        }

        if (value.IsStringLike())
        {
            decimal parsed = 0m;
            if (decimal.TryParse(value.StringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }
        }

        if (Options.StrictTypeConversions)
        {
            throw new InvalidOperationException("Value cannot be converted to a number.");
        }

        return 0m;
    }

    internal string ToText(TemplateValue value)
    {
        if (IsLiquidMarker(value, LiquidEmptyMarker) || IsLiquidMarker(value, LiquidBlankMarker))
        {
            return "";
        }

        if (value.IsStringLike())
        {
            return value.StringValue;
        }

        if (value.Kind == TemplateValueKind.Number)
        {
            return value.NumberValue.ToString(CultureInfo.InvariantCulture);
        }

        if (value.Kind == TemplateValueKind.Boolean)
        {
            if (value.BooleanValue)
            {
                return "true";
            }

            return "false";
        }

        if (value.IsMissing() || value.IsNull())
        {
            return "";
        }

        if (value.ObjectValue != null)
        {
            string? text = value.ObjectValue.ToString();
            if (text != null)
            {
                return text;
            }
        }

        return "";
    }

    internal string ConvertKey(TemplateValue key)
    {
        if (key.IsStringLike())
        {
            return key.StringValue;
        }

        if (key.Kind == TemplateValueKind.Number)
        {
            return key.NumberValue.ToString(CultureInfo.InvariantCulture);
        }

        if (key.Kind == TemplateValueKind.Boolean)
        {
            if (key.BooleanValue)
            {
                return "true";
            }

            return "false";
        }

        return "";
    }

    internal bool IsInt32(decimal value)
    {
        if (value < -2147483648m || value > 2147483647m)
        {
            return false;
        }

        return decimal.Truncate(value) == value;
    }

    internal string UnquoteString(string text)
    {
        if (text.Length < 2)
        {
            return text;
        }

        char quote = text[0];
        int end = text.Length - 1;
        if (text[end] != quote)
        {
            return text;
        }

        string body = text.Substring(1, text.Length - 2);
        return UnescapeString(body);
    }

    internal string UnescapeString(string text)
    {
        string result = "";
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '\\' && index + 1 < text.Length)
            {
                char escaped = text[index + 1];
                if (escaped == 'n')
                {
                    result = result + "\n";
                }
                else if (escaped == 'r')
                {
                    result = result + "\r";
                }
                else if (escaped == 't')
                {
                    result = result + "\t";
                }
                else
                {
                    result = result + escaped.ToString();
                }

                index = index + 2;
            }
            else
            {
                result = result + text[index].ToString();
                index = index + 1;
            }
        }

        return result;
    }
}
