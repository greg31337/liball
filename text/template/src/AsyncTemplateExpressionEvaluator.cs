using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace template;

internal class AsyncTemplateExpressionEvaluator
{
    internal TemplateExpressionEvaluator Evaluator;
    internal CancellationToken CancellationToken;

    internal AsyncTemplateExpressionEvaluator(TemplateExpressionEvaluator evaluator, CancellationToken cancellationToken)
    {
        Evaluator = evaluator;
        CancellationToken = cancellationToken;
    }

    internal Task<TemplateValue> EvaluateAsync(TemplateExpressionSyntax expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        if (CancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<TemplateValue>(CancellationToken);
        }

        Evaluator.EnsureExpressionDepthAllowed(expression);
        Evaluator.CheckRenderTime();
        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Literal)
        {
            return EvaluateLiteralAsync(expression);
        }

        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Variable || expression.ExpressionKind == TemplateExpressionSyntaxKind.Path)
        {
            return EvaluatePathAsync(expression);
        }

        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Operator)
        {
            return EvaluateOperatorAsync(expression);
        }

        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Group)
        {
            if (expression.Children.Count == 0)
            {
                return Task.FromResult(TemplateValue.Missing());
            }

            return EvaluateAsync(expression.Children[0]);
        }

        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Call)
        {
            return EvaluateCallAsync(expression);
        }

        return Task.FromResult(TemplateValue.Missing());
    }

    internal Task<TemplateValue> EvaluateLiteralAsync(TemplateExpressionSyntax expression)
    {
        if (expression.LiteralKind == TemplateLiteralSyntaxKind.Array)
        {
            return ContinueValuesToValue(EvaluateExpressionListAsync(expression.Children, false), (List<TemplateValue> values) =>
            {
                return TemplateValue.FromList(values);
            });
        }

        if (expression.LiteralKind == TemplateLiteralSyntaxKind.Object)
        {
            return EvaluateObjectAsync(expression, 0, new Dictionary<string, TemplateValue>());
        }

        if (expression.LiteralKind == TemplateLiteralSyntaxKind.Range)
        {
            return EvaluateRangeAsync(expression);
        }

        return Task.FromResult(Evaluator.EvaluateLiteral(expression));
    }

    internal Task<TemplateValue> EvaluateObjectAsync(TemplateExpressionSyntax expression, int index, Dictionary<string, TemplateValue> values)
    {
        if (index + 1 >= expression.Children.Count)
        {
            return Task.FromResult(TemplateValue.FromMap(values));
        }

        TemplateExpressionSyntax keyExpression = expression.Children[index];
        Task<TemplateValue> keyTask;
        if (keyExpression.ExpressionKind == TemplateExpressionSyntaxKind.Variable)
        {
            keyTask = Task.FromResult(TemplateValue.FromString(keyExpression.Text));
        }
        else
        {
            keyTask = EvaluateAsync(keyExpression);
        }

        return ContinueValueTask(keyTask, (TemplateValue key) =>
        {
            return ContinueValueTask(EvaluateAsync(expression.Children[index + 1]), (TemplateValue value) =>
            {
                values[Evaluator.ConvertKey(key)] = value;
                return EvaluateObjectAsync(expression, index + 2, values);
            });
        });
    }

    internal Task<TemplateValue> EvaluateRangeAsync(TemplateExpressionSyntax expression)
    {
        if (expression.Children.Count < 2)
        {
            return Task.FromResult(TemplateValue.Missing());
        }

        return ContinueValueTask(EvaluateAsync(expression.Children[0]), (TemplateValue start) =>
        {
            return ContinueValueTask(EvaluateAsync(expression.Children[1]), (TemplateValue end) =>
            {
                return Task.FromResult(CreateRangeValue(start, end, expression.Span));
            });
        });
    }

    internal TemplateValue CreateRangeValue(TemplateValue start, TemplateValue end, TemplateSourceSpan span)
    {
        if (start.Kind != TemplateValueKind.Number || end.Kind != TemplateValueKind.Number)
        {
            return Evaluator.InvalidOperationValue("Range endpoints must be numbers.", span);
        }

        if (!Evaluator.IsInt32(start.NumberValue) || !Evaluator.IsInt32(end.NumberValue))
        {
            return Evaluator.InvalidOperationValue("Range endpoints must be 32-bit integers.", span);
        }

        decimal itemCount = end.NumberValue - start.NumberValue;
        if (itemCount < 0m)
        {
            itemCount = 0m - itemCount;
        }

        itemCount = itemCount + 1m;
        if (itemCount > Evaluator.Options.MaximumLoopIterations)
        {
            throw new InvalidOperationException("Range contains more than the configured maximum values.");
        }

        List<TemplateValue> values = new List<TemplateValue>();
        int current = (int)start.NumberValue;
        int final = (int)end.NumberValue;
        while (true)
        {
            CancellationToken.ThrowIfCancellationRequested();
            values.Add(TemplateValue.FromNumber(current));
            if (current == final)
            {
                break;
            }

            if (current < final)
            {
                current = current + 1;
            }
            else
            {
                current = current - 1;
            }
        }

        return TemplateValue.FromList(values);
    }

    internal Task<TemplateValue> EvaluatePathAsync(TemplateExpressionSyntax expression)
    {
        if (expression.PathSegments.Count == 0)
        {
            return Task.FromResult(Evaluator.ResolvePathRoot(expression.PathKind));
        }

        int index = 0;
        TemplateValue current = Evaluator.ResolvePathRoot(expression.PathKind);
        if (expression.PathKind == TemplatePathSyntaxKind.Variable || expression.PathKind == TemplatePathSyntaxKind.Local)
        {
            TemplatePathSegmentSyntax first = expression.PathSegments[0];
            if (first.SegmentKind != TemplatePathSegmentSyntaxKind.Name)
            {
                return Task.FromResult(TemplateValue.Missing());
            }

            if (expression.PathKind == TemplatePathSyntaxKind.Variable)
            {
                current = Evaluator.ResolveVariable(first.Text, expression.Span);
            }
            else
            {
                current = Evaluator.ResolveLocalVariable(first.Text);
            }

            index = 1;
        }

        return EvaluatePathSegmentAsync(expression, index, current);
    }

    internal Task<TemplateValue> EvaluatePathSegmentAsync(TemplateExpressionSyntax expression, int index, TemplateValue current)
    {
        if (index >= expression.PathSegments.Count)
        {
            if (current.IsMissing())
            {
                return Task.FromResult(Evaluator.MissingPathValue(expression.Text, expression.Span));
            }

            return Task.FromResult(current);
        }

        TemplatePathSegmentSyntax segment = expression.PathSegments[index];
        if (current.IsMissing() || current.IsNull())
        {
            if (segment.NullSafe)
            {
                return Task.FromResult(TemplateValue.Null());
            }

            return Task.FromResult(Evaluator.MissingPathValue(segment.Text, expression.Span));
        }

        if (segment.SegmentKind == TemplatePathSegmentSyntaxKind.Name)
        {
            Evaluator.EnsureMemberAccessAllowed(segment.Text, expression.Span);
            TemplateValue next = Evaluator.MemberResolver.ResolveMember(current, segment.Text);
            if (next.IsMissing() && segment.NullSafe)
            {
                return Task.FromResult(TemplateValue.Null());
            }

            return EvaluatePathSegmentAsync(expression, index + 1, next);
        }

        if (segment.Expression == null)
        {
            return Task.FromResult(TemplateValue.Missing());
        }

        return ContinueValueTask(EvaluateAsync(segment.Expression), (TemplateValue indexValue) =>
        {
            TemplateValue next = Evaluator.MemberResolver.ResolveIndex(current, indexValue);
            if (next.IsMissing() && segment.NullSafe)
            {
                return Task.FromResult(TemplateValue.Null());
            }

            return EvaluatePathSegmentAsync(expression, index + 1, next);
        });
    }

    internal Task<TemplateValue> EvaluateOperatorAsync(TemplateExpressionSyntax expression)
    {
        if (expression.Children.Count == 0)
        {
            return Task.FromResult(TemplateValue.Missing());
        }

        return ContinueValueTask(EvaluateAsync(expression.Children[0]), (TemplateValue left) =>
        {
            if (expression.OperatorKind == TemplateExpressionOperatorKind.UnaryNot)
            {
                return Task.FromResult(TemplateValue.FromBoolean(!TemplateTruthiness.IsTruthy(left, Evaluator.Options)));
            }

            if (expression.OperatorKind == TemplateExpressionOperatorKind.UnaryNegate)
            {
                return Task.FromResult(TemplateValue.FromNumber(0m - Evaluator.ToNumber(left)));
            }

            if (expression.OperatorKind == TemplateExpressionOperatorKind.UnaryPlus)
            {
                return Task.FromResult(TemplateValue.FromNumber(Evaluator.ToNumber(left)));
            }

            if (expression.OperatorKind == TemplateExpressionOperatorKind.And && !TemplateTruthiness.IsTruthy(left, Evaluator.Options))
            {
                return Task.FromResult(TemplateValue.FromBoolean(false));
            }

            if (expression.OperatorKind == TemplateExpressionOperatorKind.Or && TemplateTruthiness.IsTruthy(left, Evaluator.Options))
            {
                return Task.FromResult(TemplateValue.FromBoolean(true));
            }

            if (expression.OperatorKind == TemplateExpressionOperatorKind.NullCoalescing && !left.IsMissing() && !left.IsNull())
            {
                return Task.FromResult(left);
            }

            if (expression.OperatorKind == TemplateExpressionOperatorKind.Conditional)
            {
                int branch = 2;
                if (TemplateTruthiness.IsTruthy(left, Evaluator.Options))
                {
                    branch = 1;
                }

                if (branch >= expression.Children.Count)
                {
                    return Task.FromResult(TemplateValue.Missing());
                }

                return EvaluateAsync(expression.Children[branch]);
            }

            if (expression.Children.Count < 2)
            {
                return Task.FromResult(TemplateValue.Missing());
            }

            return ContinueValueTask(EvaluateAsync(expression.Children[1]), (TemplateValue right) =>
            {
                return Task.FromResult(EvaluateBinaryValue(expression.OperatorKind, left, right));
            });
        });
    }

    internal TemplateValue EvaluateBinaryValue(TemplateExpressionOperatorKind kind, TemplateValue left, TemplateValue right)
    {
        if (kind == TemplateExpressionOperatorKind.And || kind == TemplateExpressionOperatorKind.Or)
        {
            return TemplateValue.FromBoolean(TemplateTruthiness.IsTruthy(right, Evaluator.Options));
        }

        if (kind == TemplateExpressionOperatorKind.NullCoalescing)
        {
            return right;
        }

        if (kind == TemplateExpressionOperatorKind.Add)
        {
            if (left.IsStringLike() || right.IsStringLike())
            {
                return TemplateValue.FromString(Evaluator.ToText(left) + Evaluator.ToText(right));
            }

            return TemplateValue.FromNumber(Evaluator.ToNumber(left) + Evaluator.ToNumber(right));
        }

        if (kind == TemplateExpressionOperatorKind.Subtract)
        {
            return TemplateValue.FromNumber(Evaluator.ToNumber(left) - Evaluator.ToNumber(right));
        }

        if (kind == TemplateExpressionOperatorKind.Multiply)
        {
            return TemplateValue.FromNumber(Evaluator.ToNumber(left) * Evaluator.ToNumber(right));
        }

        if (kind == TemplateExpressionOperatorKind.Divide)
        {
            if (Evaluator.ToNumber(right) == 0m)
            {
                throw new InvalidOperationException("Cannot divide by zero.");
            }

            return TemplateValue.FromNumber(Evaluator.ToNumber(left) / Evaluator.ToNumber(right));
        }

        if (kind == TemplateExpressionOperatorKind.Modulo)
        {
            if (Evaluator.ToNumber(right) == 0m)
            {
                throw new InvalidOperationException("Cannot calculate a remainder with a zero divisor.");
            }

            return TemplateValue.FromNumber(Evaluator.ToNumber(left) % Evaluator.ToNumber(right));
        }

        if (kind == TemplateExpressionOperatorKind.Equal)
        {
            return TemplateValue.FromBoolean(Evaluator.ValuesEqual(left, right));
        }

        if (kind == TemplateExpressionOperatorKind.NotEqual)
        {
            return TemplateValue.FromBoolean(!Evaluator.ValuesEqual(left, right));
        }

        if (kind == TemplateExpressionOperatorKind.LessThan)
        {
            return TemplateValue.FromBoolean(Evaluator.CompareValues(left, right) < 0);
        }

        if (kind == TemplateExpressionOperatorKind.LessThanOrEqual)
        {
            return TemplateValue.FromBoolean(Evaluator.CompareValues(left, right) <= 0);
        }

        if (kind == TemplateExpressionOperatorKind.GreaterThan)
        {
            return TemplateValue.FromBoolean(Evaluator.CompareValues(left, right) > 0);
        }

        if (kind == TemplateExpressionOperatorKind.GreaterThanOrEqual)
        {
            return TemplateValue.FromBoolean(Evaluator.CompareValues(left, right) >= 0);
        }

        if (kind == TemplateExpressionOperatorKind.Contains)
        {
            return TemplateValue.FromBoolean(Evaluator.ValueContains(left, right));
        }

        if (kind == TemplateExpressionOperatorKind.StartsWith)
        {
            return TemplateValue.FromBoolean(Evaluator.ValueStartsWith(left, right));
        }

        if (kind == TemplateExpressionOperatorKind.EndsWith)
        {
            return TemplateValue.FromBoolean(Evaluator.ValueEndsWith(left, right));
        }

        return TemplateValue.Missing();
    }

    internal Task<TemplateValue> EvaluateCallAsync(TemplateExpressionSyntax expression)
    {
        bool allowMissing = expression.CallKind == TemplateExpressionCallKind.Test || (expression.CallKind == TemplateExpressionCallKind.Filter && Evaluator.FilterAllowsMissing(expression.Name));
        return ContinueValueTaskList(EvaluateExpressionListAsync(expression.Arguments, allowMissing), (List<TemplateValue> arguments) =>
        {
            return ContinueValueTask(AddArgumentMapsAsync(expression, arguments, 0, true), (TemplateValue ignored) =>
            {
                return InvokeCallAsync(expression, arguments);
            });
        });
    }

    internal Task<TemplateValue> AddArgumentMapsAsync(TemplateExpressionSyntax expression, List<TemplateValue> arguments, int index, bool named)
    {
        List<TemplateNamedArgumentSyntax> items = expression.NamedArguments;
        if (!named)
        {
            items = expression.HashArguments;
        }

        if (index >= items.Count)
        {
            if (named)
            {
                return AddArgumentMapsAsync(expression, arguments, 0, false);
            }

            return Task.FromResult(TemplateValue.Missing());
        }

        Dictionary<string, TemplateValue> map;
        if (arguments.Count > expression.Arguments.Count && arguments[arguments.Count - 1].Kind == TemplateValueKind.Map)
        {
            map = arguments[arguments.Count - 1].MapValue;
        }
        else
        {
            map = new Dictionary<string, TemplateValue>();
            arguments.Add(TemplateValue.FromMap(map));
        }

        TemplateNamedArgumentSyntax item = items[index];
        return ContinueValueTask(EvaluateAsync(item.Value), (TemplateValue value) =>
        {
            map[item.Name] = value;
            return AddArgumentMapsAsync(expression, arguments, index + 1, named);
        });
    }

    internal Task<TemplateValue> InvokeCallAsync(TemplateExpressionSyntax expression, List<TemplateValue> arguments)
    {
        Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>> asynchronous = Evaluator.AsyncFunctions;
        Dictionary<string, Func<List<TemplateValue>, TemplateValue>> synchronous = Evaluator.Functions;
        if (expression.CallKind == TemplateExpressionCallKind.Filter)
        {
            asynchronous = Evaluator.AsyncFilters;
            synchronous = Evaluator.Filters;
        }
        else if (expression.CallKind == TemplateExpressionCallKind.Test)
        {
            asynchronous = Evaluator.AsyncTests;
            synchronous = Evaluator.Tests;
        }

        string name = expression.Name;
        bool invert = false;
        if (!asynchronous.ContainsKey(name) && !synchronous.ContainsKey(name) && name.Length > 4 && name.Substring(0, 4) == "not ")
        {
            name = name.Substring(4);
            invert = true;
        }

        if (asynchronous.ContainsKey(name))
        {
            Task<TemplateValue> task = asynchronous[name](arguments, CancellationToken);
            if (task == null)
            {
                throw new InvalidOperationException("Asynchronous template function returned null.");
            }

            return ContinueValueTask(task, (TemplateValue value) =>
            {
                return Task.FromResult(ValidateCallableResult(value, invert));
            });
        }

        if (synchronous.ContainsKey(name))
        {
            TemplateValue value = Evaluator.InvokeFunction(synchronous[name], arguments);
            return Task.FromResult(ValidateCallableResult(value, invert));
        }

        TemplateValue scoped = Evaluator.ResolveVariable(name, expression.Span);
        if (scoped.Kind == TemplateValueKind.Function)
        {
            return Task.FromResult(ValidateCallableResult(scoped.Invoke(arguments), invert));
        }

        if (Evaluator.Options.StrictUnknownFunctions)
        {
            Evaluator.ReportDiagnostic("EVAL001", TemplateDiagnosticSeverity.Error, "Unknown template function '" + expression.Name + "'.", expression.Span, "Register the callable or correct its name.");
            throw new InvalidOperationException("Unknown template function '" + expression.Name + "'.");
        }

        Evaluator.ReportDiagnostic("EVAL001", TemplateDiagnosticSeverity.Warning, "Unknown template function '" + expression.Name + "'.", expression.Span, "Register the callable or correct its name.");
        return Task.FromResult(TemplateValue.Missing());
    }

    internal TemplateValue ValidateCallableResult(TemplateValue value, bool invert)
    {
        if (value == null)
        {
            throw new InvalidOperationException("Template function returned null.");
        }

        Evaluator.CheckRenderTime();
        CancellationToken.ThrowIfCancellationRequested();
        if (invert)
        {
            return TemplateValue.FromBoolean(!TemplateTruthiness.IsTruthy(value, Evaluator.Options));
        }

        return value;
    }

    internal Task<List<TemplateValue>> EvaluateExpressionListAsync(List<TemplateExpressionSyntax> expressions, bool allowMissing)
    {
        return EvaluateExpressionListItemAsync(expressions, 0, new List<TemplateValue>(), allowMissing);
    }

    internal Task<List<TemplateValue>> EvaluateExpressionListItemAsync(List<TemplateExpressionSyntax> expressions, int index, List<TemplateValue> values, bool allowMissing)
    {
        if (index >= expressions.Count)
        {
            return Task.FromResult(values);
        }

        if (allowMissing)
        {
            Evaluator.MissingToleranceDepth = Evaluator.MissingToleranceDepth + 1;
        }

        Task<TemplateValue> task = EvaluateAsync(expressions[index]);
        if (allowMissing)
        {
            return task.ContinueWith((Task<TemplateValue> completed) =>
            {
                Evaluator.MissingToleranceDepth = Evaluator.MissingToleranceDepth - 1;
                if (completed.IsCanceled)
                {
                    return Task.FromCanceled<List<TemplateValue>>(new CancellationToken(true));
                }

                if (completed.IsFaulted)
                {
                    return Task.FromException<List<TemplateValue>>(GetTaskException(completed.Exception));
                }

                CancellationToken.ThrowIfCancellationRequested();
                values.Add(completed.Result);
                return EvaluateExpressionListItemAsync(expressions, index + 1, values, true);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
        }

        return ContinueValueToValues(task, (TemplateValue value) =>
        {
            values.Add(value);
            return EvaluateExpressionListItemAsync(expressions, index + 1, values, allowMissing);
        });
    }

    internal Task<TemplateValue> ContinueValueTask(Task<TemplateValue> task, Func<TemplateValue, Task<TemplateValue>> continuation)
    {
        return task.ContinueWith((Task<TemplateValue> completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled<TemplateValue>(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                return Task.FromException<TemplateValue>(GetTaskException(completed.Exception));
            }

            CancellationToken.ThrowIfCancellationRequested();
            return continuation(completed.Result);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<TemplateValue> ContinueValueTaskList(Task<List<TemplateValue>> task, Func<List<TemplateValue>, Task<TemplateValue>> continuation)
    {
        return task.ContinueWith((Task<List<TemplateValue>> completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled<TemplateValue>(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                return Task.FromException<TemplateValue>(GetTaskException(completed.Exception));
            }

            CancellationToken.ThrowIfCancellationRequested();
            return continuation(completed.Result);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<List<TemplateValue>> ContinueValueToValues(Task<TemplateValue> task, Func<TemplateValue, Task<List<TemplateValue>>> continuation)
    {
        return task.ContinueWith((Task<TemplateValue> completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled<List<TemplateValue>>(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                return Task.FromException<List<TemplateValue>>(GetTaskException(completed.Exception));
            }

            CancellationToken.ThrowIfCancellationRequested();
            return continuation(completed.Result);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<TemplateValue> ContinueValuesToValue(Task<List<TemplateValue>> task, Func<List<TemplateValue>, TemplateValue> continuation)
    {
        return task.ContinueWith((Task<List<TemplateValue>> completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled<TemplateValue>(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                return Task.FromException<TemplateValue>(GetTaskException(completed.Exception));
            }

            CancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(continuation(completed.Result));
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Exception GetTaskException(AggregateException exception)
    {
        if (exception.InnerExceptions.Count == 1)
        {
            return exception.InnerExceptions[0];
        }

        return exception;
    }
}
