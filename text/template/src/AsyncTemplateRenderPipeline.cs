using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace template;

internal class AsyncTemplateRenderPipeline
{
    internal TemplateRenderer Renderer;
    internal TemplateRenderContext Context;
    internal AsyncTemplateExpressionEvaluator Evaluator;
    internal CancellationToken CancellationToken;

    internal AsyncTemplateRenderPipeline(TemplateRenderer renderer, TemplateRenderContext context, CancellationToken cancellationToken)
    {
        Renderer = renderer;
        Context = context;
        Evaluator = new AsyncTemplateExpressionEvaluator(context.Evaluator, cancellationToken);
        CancellationToken = cancellationToken;
    }

    internal Task<string> RenderRootToStringAsync(TemplateDocumentSyntax document)
    {
        if (CancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<string>(CancellationToken);
        }

        Context.BeginRender(document.Source);
        Renderer.RegisterTopLevelMacros(document, Context);
        return ContinueStringTask(RenderDocumentAsync(document), (string output) =>
        {
            string outputBeforeNormalization = output;
            string normalized = Renderer.NormalizeRenderedWhitespace(output, Context.Options.WhitespaceMode);
            Context.SourceMapState.TransformOutput(outputBeforeNormalization, normalized, document.Span);
            if (Context.Options.TrimFinalNewline)
            {
                int previousLength = normalized.Length;
                normalized = Renderer.TrimFinalNewline(normalized);
                Context.SourceMapState.TrimEnd(previousLength - normalized.Length);
            }

            Context.FlowControlSignal = "";
            Renderer.CompleteRender(Context);
            return Task.FromResult(normalized);
        });
    }

    internal Task RenderRootToWriterAsync(TemplateDocumentSyntax document)
    {
        Task<string> renderTask = RenderRootToStringAsync(document);
        return renderTask.ContinueWith((Task<string> completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                return Task.FromException(GetTaskException(completed.Exception));
            }

            CancellationToken.ThrowIfCancellationRequested();
            return ContinueWriteWithFlushAsync(Context.Writer.WriteAsync(completed.Result, CancellationToken));
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task ContinueWriteWithFlushAsync(Task writeTask)
    {
        return writeTask.ContinueWith((Task completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                return Task.FromException(GetTaskException(completed.Exception));
            }

            CancellationToken.ThrowIfCancellationRequested();
            return Context.Writer.FlushAsync(CancellationToken);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<string> RenderDocumentAsync(TemplateDocumentSyntax document)
    {
        TemplateStatementSyntax? extendsStatement = Renderer.FindExtendsStatement(document);
        if (extendsStatement != null)
        {
            return RenderExtendsAsync(document, extendsStatement);
        }

        return RenderNodesAsync(document.Children, 0, document.Children.Count, "", 0);
    }

    internal Task<string> RenderExtendsAsync(TemplateDocumentSyntax document, TemplateStatementSyntax statement)
    {
        if (statement.Arguments.Count == 0)
        {
            return Task.FromResult("");
        }

        return ContinueValueToString(Evaluator.EvaluateAsync(statement.Arguments[0]), (TemplateValue nameValue) =>
        {
            string name = TemplateOutputEncoder.ConvertValueToText(nameValue);
            return ContinueSourceToString(Context.Registry.LoadTemplateWithCancellationAsync(name, CancellationToken), (TemplateSource? source) =>
            {
                if (source == null)
                {
                    Context.ReportDiagnostic(new TemplateDiagnostic("COMPOSE001", TemplateDiagnosticSeverity.Warning, "Template '" + name + "' was not found.", statement.Span, "Register the dependency or correct its name."));
                    if (Context.Options.StrictMissingValues)
                    {
                        throw new InvalidOperationException("Template '" + name + "' was not found.");
                    }

                    return Task.FromResult("");
                }

                Context.RecordDependency(TemplateDependencyKind.Template, source);
                TemplateCompiledTemplate compiled = Renderer.CompileSourceForContext(source, Context.Options, Context);
                Dictionary<string, TemplateBlockOverride> previousOverrides = Context.BlockOverrides;
                return ContinueStringTask(RenderInheritancePreambleAsync(document, 0), (string ignored) =>
                {
                    Context.BlockOverrides = Renderer.ComposeBlockOverrides(document, previousOverrides, Context);
                    Renderer.EnterIncludeScope("template", source.Name, Context);
                    TemplateScopeFrame frame = Context.Scope.PushIncludeScope(source.Name, Context.Scope.GetCurrentValue());
                    Task<string> task = RenderDocumentAsync(compiled.Document);
                    return CompleteCompositionAsync(task, previousOverrides, true);
                });
            });
        });
    }

    internal Task<string> RenderInheritancePreambleAsync(TemplateDocumentSyntax document, int index)
    {
        if (index >= document.Children.Count)
        {
            return Task.FromResult("");
        }

        TemplateSyntaxNode node = document.Children[index];
        Task<string> task = Task.FromResult("");
        if (node.Kind == TemplateSyntaxKind.Statement && node.Statement != null)
        {
            string name = node.Statement.Name;
            if (name == "import" || name == "set" || name == "assign")
            {
                task = RenderStatementAsync(node.Statement);
            }
        }
        else if (node.Kind == TemplateSyntaxKind.Block && node.Block != null && node.Block.Name == "capture")
        {
            task = RenderCaptureAsync(node.Block);
        }

        return ContinueStringTask(task, (string ignored) => RenderInheritancePreambleAsync(document, index + 1));
    }

    internal Task<string> RenderNodesAsync(List<TemplateSyntaxNode> nodes, int index, int endIndex, string output, int pendingTrim)
    {
        if (CancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<string>(CancellationToken);
        }

        Context.CheckRenderTime();
        if (index >= endIndex || Context.FlowControlSignal != "")
        {
            return Task.FromResult(output);
        }

        TemplateSyntaxNode node = nodes[index];
        if (node.Kind == TemplateSyntaxKind.Text && node.Text != null)
        {
            string text = Renderer.ApplyLeadingTrim(node.Text.Text, pendingTrim);
            Context.SourceMapState.Record(text, node.Text.Span);
            return RenderNodesAsync(nodes, index + 1, endIndex, Renderer.AppendStringOutput(Context, output, text), 0);
        }

        string adjusted = output;
        TemplateTrimKind leftTrimKind = Renderer.GetOpeningLeftTrimKind(node);
        if (leftTrimKind == TemplateTrimKind.Greedy)
        {
            int previousLength = adjusted.Length;
            adjusted = Renderer.TrimTrailingWhitespace(adjusted);
            Context.SourceMapState.TrimEnd(previousLength - adjusted.Length);
        }
        else if (leftTrimKind == TemplateTrimKind.NonGreedy || Renderer.IsOpeningStandalone(node, Context))
        {
            int previousLength = adjusted.Length;
            adjusted = Renderer.TrimStandaloneLeft(adjusted);
            Context.SourceMapState.TrimEnd(previousLength - adjusted.Length);
        }

        string outputBeforeNode = adjusted;
        int outputStart = Context.SourceMapState.OutputLength;
        return ContinueStringTask(RenderNodeAsync(node, outputBeforeNode), (string rendered) =>
        {
            Context.SourceMapState.FinishNode(outputStart, rendered, Renderer.GetNodeSourceSpan(node));
            string nextOutput = Renderer.AppendStringOutput(Context, outputBeforeNode, rendered);
            int nextTrim = 0;
            TemplateTrimKind rightTrimKind = Renderer.GetClosingRightTrimKind(node);
            if (rightTrimKind == TemplateTrimKind.Greedy)
            {
                nextTrim = 1;
            }
            else if (rightTrimKind == TemplateTrimKind.NonGreedy || Renderer.IsClosingStandalone(node, Context))
            {
                nextTrim = 2;
            }

            return RenderNodesAsync(nodes, index + 1, endIndex, nextOutput, nextTrim);
        });
    }

    internal Task<string> RenderNodeAsync(TemplateSyntaxNode node, string outputSoFar)
    {
        if (node.Kind == TemplateSyntaxKind.Output && node.Output != null)
        {
            return RenderOutputAsync(node.Output, outputSoFar);
        }

        if (node.Kind == TemplateSyntaxKind.Raw && node.Raw != null)
        {
            return Task.FromResult(Renderer.RenderRawToString(node.Raw, Context));
        }

        if (node.Kind == TemplateSyntaxKind.Statement && node.Statement != null)
        {
            return RenderStatementAsync(node.Statement);
        }

        if (node.Kind == TemplateSyntaxKind.Block && node.Block != null)
        {
            return RenderBlockAsync(node.Block);
        }

        return Task.FromResult("");
    }

    internal Task<string> RenderOutputAsync(TemplateOutputSyntax output, string outputSoFar)
    {
        if (Context.UseHandlebarsCompatibility
            && output.Expression.ExpressionKind == TemplateExpressionSyntaxKind.Variable
            && Context.Registry.Helpers.ContainsKey(output.Expression.Text))
        {
            TemplateValue helperValue = Context.Registry.Helpers[output.Expression.Text](new List<TemplateValue>());
            return Task.FromResult(FormatOutputValue(output, helperValue, outputSoFar));
        }

        if (Context.UseHandlebarsCompatibility
            && output.Expression.ExpressionKind == TemplateExpressionSyntaxKind.Variable
            && Context.Registry.AsyncHelpers.ContainsKey(output.Expression.Text))
        {
            Task<TemplateValue> helperTask = Context.Registry.AsyncHelpers[output.Expression.Text](new List<TemplateValue>(), CancellationToken);
            if (helperTask == null)
            {
                return Task.FromException<string>(new InvalidOperationException("Asynchronous template helper returned null instead of a task."));
            }

            return ContinueValueToString(helperTask, (TemplateValue helperValue) => Task.FromResult(FormatOutputValue(output, helperValue, outputSoFar)));
        }

        return ContinueValueToString(Evaluator.EvaluateAsync(output.Expression), (TemplateValue value) =>
        {
            if (Context.UseHandlebarsCompatibility && value.Kind == TemplateValueKind.Function)
            {
                value = value.Invoke(new List<TemplateValue>());
            }

            return Task.FromResult(FormatOutputValue(output, value, outputSoFar));
        });
    }

    internal string FormatOutputValue(TemplateOutputSyntax output, TemplateValue value, string outputSoFar)
    {
        bool raw = output.Raw || !output.Escape;
        string text = Renderer.EncodeOutputValue(value, Context, raw);
        if (Context.Options.AutoIndentMultilineValues)
        {
            text = Renderer.ApplyAutoIndent(text, outputSoFar);
        }

        return text;
    }

    internal Task<string> RenderStatementAsync(TemplateStatementSyntax statement)
    {
        if (statement.Name == "liquidCycle" || statement.Name == "liquidCycleNamed" || statement.Name == "liquidComment" || statement.Name == "liquidRenderFor" || statement.Name == "liquidIncludeFor")
        {
            return Task.FromResult(Renderer.RenderStatementToString(statement, Context));
        }

        if (statement.Name == "break" || statement.Name == "continue")
        {
            Context.FlowControlSignal = statement.Name;
            return Task.FromResult("");
        }

        if (statement.Name == "set" || statement.Name == "assign")
        {
            return RenderAssignmentAsync(statement);
        }

        if (statement.Name == "return")
        {
            return RenderReturnAsync(statement);
        }

        if (statement.Name == "include" || statement.Name == "render" || statement.Name == "partial")
        {
            return RenderCompositionAsync(statement);
        }

        if (statement.Name == "import")
        {
            return RenderImportAsync(statement);
        }

        if (statement.Arguments.Count == 0)
        {
            return Task.FromResult(Renderer.RenderStatementToString(statement, Context));
        }

        return ContinueValueListToString(Evaluator.EvaluateExpressionListAsync(statement.Arguments, false), (List<TemplateValue> ignored) =>
        {
            return Task.FromResult("");
        });
    }

    internal Task<string> RenderAssignmentAsync(TemplateStatementSyntax statement)
    {
        string name = Renderer.GetAssignmentVariableName(statement);
        int index = Renderer.GetAssignmentValueIndex(statement);
        if (name == "" || index < 0)
        {
            return Task.FromResult("");
        }

        return ContinueValueToString(Evaluator.EvaluateAsync(statement.Arguments[index]), (TemplateValue value) =>
        {
            if (Context.UseLiquidCompatibility)
            {
                Context.Scope.SetLiquidLocal(name, value);
            }
            else
            {
                Context.Scope.SetLocal(name, value);
            }
            return Task.FromResult("");
        });
    }

    internal Task<string> RenderReturnAsync(TemplateStatementSyntax statement)
    {
        if (statement.Arguments.Count == 0)
        {
            Context.ReturnValue = TemplateValue.Null();
            Context.FlowControlSignal = "return";
            return Task.FromResult("");
        }

        return ContinueValueToString(Evaluator.EvaluateAsync(statement.Arguments[0]), (TemplateValue value) =>
        {
            Context.ReturnValue = value;
            Context.FlowControlSignal = "return";
            return Task.FromResult("");
        });
    }

    internal Task<string> RenderImportAsync(TemplateStatementSyntax statement)
    {
        if (statement.Arguments.Count == 0)
        {
            return Task.FromResult("");
        }

        return ContinueValueToString(Evaluator.EvaluateAsync(statement.Arguments[0]), (TemplateValue value) =>
        {
            string name = TemplateOutputEncoder.ConvertValueToText(value);
            return ContinueSourceToString(Context.Registry.LoadTemplateWithCancellationAsync(name, CancellationToken), (TemplateSource? source) =>
            {
                if (source == null)
                {
                    Context.ReportDiagnostic(new TemplateDiagnostic("COMPOSE001", TemplateDiagnosticSeverity.Warning, "Template '" + name + "' was not found.", statement.Span, "Register the dependency or correct its name."));
                    if (Context.Options.StrictMissingValues)
                    {
                        throw new InvalidOperationException("Template '" + name + "' was not found.");
                    }

                    return Task.FromResult("");
                }

                Context.RecordDependency(TemplateDependencyKind.Template, source);
                TemplateCompiledTemplate compiled = Renderer.CompileSourceForContext(source, Context.Options, Context);
                Renderer.RegisterTopLevelMacros(compiled.Document, Context);
                return Task.FromResult("");
            });
        });
    }

    internal Task<string> RenderCompositionAsync(TemplateStatementSyntax statement)
    {
        if (statement.Arguments.Count == 0)
        {
            return Task.FromResult("");
        }

        return ContinueValueToString(Evaluator.EvaluateAsync(statement.Arguments[0]), (TemplateValue value) =>
        {
            string name = TemplateOutputEncoder.ConvertValueToText(value);
            Task<TemplateSource?> loadTask;
            string kind;
            if (statement.Name == "partial")
            {
                if (Context.UseHandlebarsCompatibility && Context.HasVisibleHandlebarsInlinePartial(name))
                {
                    loadTask = Task.FromResult<TemplateSource?>(Context.HandlebarsInlinePartials[name]);
                }
                else
                {
                    loadTask = Context.Registry.LoadPartialWithCancellationAsync(name, CancellationToken);
                }

                kind = "partial";
            }
            else
            {
                loadTask = Context.Registry.LoadTemplateWithCancellationAsync(name, CancellationToken);
                kind = "template";
            }

            return ContinueSourceToString(loadTask, (TemplateSource? source) =>
            {
                if (source == null)
                {
                    string displayKind = kind;
                    if (kind == "partial")
                    {
                        displayKind = "Partial";
                    }
                    else
                    {
                        displayKind = "Template";
                    }

                    Context.ReportDiagnostic(new TemplateDiagnostic("COMPOSE001", TemplateDiagnosticSeverity.Warning, displayKind + " '" + name + "' was not found.", statement.Span, "Register the dependency or correct its name."));
                    if (Context.Options.StrictMissingValues)
                    {
                        throw new InvalidOperationException(kind + " '" + name + "' was not found.");
                    }

                    return Task.FromResult("");
                }

                return RenderLoadedCompositionAsync(statement, source, kind);
            });
        });
    }

    internal Task<string> RenderLoadedCompositionAsync(TemplateStatementSyntax statement, TemplateSource source, string kind)
    {
        if (kind == "partial" && Context.UseHandlebarsCompatibility)
        {
            string indentation = Renderer.GetHandlebarsPartialIndentation(statement);
            if (indentation != "")
            {
                string indentedText = Renderer.IndentHandlebarsPartialSource(source.Text, indentation);
                source = new TemplateSource(source.Name, source.Path, source.Version, TemplateSource.ComputeContentHash(indentedText), indentedText);
            }
        }

        Context.RecordDependency(kind == "partial" ? TemplateDependencyKind.Partial : TemplateDependencyKind.Template, source);
        TemplateCompiledTemplate compiled = Renderer.CompileSourceForContext(source, Context.Options, Context);
        Task<TemplateValue> modelTask;
        if (statement.Arguments.Count > 1)
        {
            modelTask = Evaluator.EvaluateAsync(statement.Arguments[1]);
        }
        else if (statement.Name == "render")
        {
            modelTask = Task.FromResult(Context.Scope.RootValue);
        }
        else
        {
            modelTask = Task.FromResult(Context.Scope.GetCurrentValue());
        }

        return ContinueValueToString(modelTask, (TemplateValue model) =>
        {
            return ContinueCompositionToString(EvaluateCompositionArgumentsAsync(statement, model), (TemplateCompositionArguments arguments) =>
            {
                if (Context.UseLiquidCompatibility && statement.Arguments.Count > 1 && arguments.ContextAliases.Count == 0)
                {
                    arguments.ContextAliases.Add(Renderer.GetLiquidTemplateAlias(source.Name));
                }

                if (statement.Name == "render")
                {
                    return RenderIsolatedCompositionAsync(compiled.Document, source, kind, arguments);
                }

                Renderer.EnterIncludeScope(kind, source.Name, Context);
                TemplateScopeFrame frame = Context.Scope.PushIncludeScope(source.Name, model);
                Renderer.BindCompositionArguments(frame, arguments);
                Dictionary<string, TemplateBlockOverride> previousOverrides = Context.BlockOverrides;
                Context.BlockOverrides = new Dictionary<string, TemplateBlockOverride>();
                Task<string> task = RenderDocumentAsync(compiled.Document);
                return CompleteCompositionAsync(task, previousOverrides, true);
            });
        });
    }

    internal Task<string> RenderIsolatedCompositionAsync(TemplateDocumentSyntax document, TemplateSource source, string kind, TemplateCompositionArguments arguments)
    {
        StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
        TemplateRenderContext child = new TemplateRenderContext(arguments.Model, writer, Context.Options, Context.Registry, Context.MemberResolver);
        child.DiagnosticSink = Context.DiagnosticSink;
        child.UseSourceMapStateFrom(Context);
        child.EscapingMode = Context.EscapingMode;
        child.AutoEscape = Context.AutoEscape;
        if (!Context.UseLiquidCompatibility)
        {
            child.Counters = Context.Counters;
            child.LiquidForOffsets = Context.LiquidForOffsets;
        }
        child.IncludeStack = Context.IncludeStack;
        child.Dependencies = Context.Dependencies;
        child.HandlebarsInlinePartials = Context.HandlebarsInlinePartials;
        child.HandlebarsInlinePartialScopeIds = Context.HandlebarsInlinePartialScopeIds;
        child.HandlebarsInlinePartialScopes = Context.HandlebarsInlinePartialScopes;
        child.HandlebarsInlinePartialScopeCounter = Context.HandlebarsInlinePartialScopeCounter;
        child.HandlebarsInlinePartialSavedSources = Context.HandlebarsInlinePartialSavedSources;
        child.HandlebarsInlinePartialSavedScopeIds = Context.HandlebarsInlinePartialSavedScopeIds;
        child.HandlebarsPartialBlockContexts = Context.HandlebarsPartialBlockContexts;
        child.UseRuntimeQuotasFrom(Context);
        child.UseMustacheInheritance = Context.UseMustacheInheritance;
        child.UseHandlebarsCompatibility = Context.UseHandlebarsCompatibility;
        child.UseLiquidCompatibility = Context.UseLiquidCompatibility;
        child.UseJinjaCompatibility = Context.UseJinjaCompatibility;
        Renderer.EnterIncludeScope(kind, source.Name, child);
        TemplateScopeFrame frame = child.Scope.PushIncludeScope(source.Name, arguments.Model);
        Renderer.BindCompositionArguments(frame, arguments);
        AsyncTemplateRenderPipeline pipeline = new AsyncTemplateRenderPipeline(Renderer, child, CancellationToken);
        Task<string> task = pipeline.RenderDocumentAsync(document);
        return task.ContinueWith((Task<string> completed) =>
        {
            child.Scope.PopFrame();
            child.PopActiveInclude();
            if (!Context.UseLiquidCompatibility)
            {
                Context.FlowControlSignal = child.FlowControlSignal;
                Context.ReturnValue = child.ReturnValue;
            }
            return CopyStringTask(completed);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<TemplateCompositionArguments> EvaluateCompositionArgumentsAsync(TemplateStatementSyntax statement, TemplateValue model)
    {
        TemplateCompositionArguments arguments = new TemplateCompositionArguments(model);
        if (Context.UseLiquidCompatibility && statement.Name == "include")
        {
            TemplateScopeFrame frame = Context.Scope.PushFrame(TemplateScopeFrameKind.Local, "", Context.Scope.GetCurrentValue());
            return EvaluateLiquidIncludeArgumentsAsync(statement, arguments, frame, 0);
        }

        List<TemplateExpressionSyntax> expressions = new List<TemplateExpressionSyntax>();
        int index = 0;
        while (index < statement.NamedArguments.Count)
        {
            expressions.Add(statement.NamedArguments[index].Value);
            index = index + 1;
        }

        index = 0;
        while (index < statement.HashArguments.Count)
        {
            expressions.Add(statement.HashArguments[index].Value);
            index = index + 1;
        }

        return ContinueValueListToComposition(Evaluator.EvaluateExpressionListAsync(expressions, false), (List<TemplateValue> values) =>
        {
            int valueIndex = 0;
            int namedIndex = 0;
            while (namedIndex < statement.NamedArguments.Count)
            {
                arguments.Parameters[statement.NamedArguments[namedIndex].Name] = values[valueIndex];
                namedIndex = namedIndex + 1;
                valueIndex = valueIndex + 1;
            }

            int hashIndex = 0;
            while (hashIndex < statement.HashArguments.Count)
            {
                arguments.Parameters[statement.HashArguments[hashIndex].Name] = values[valueIndex];
                hashIndex = hashIndex + 1;
                valueIndex = valueIndex + 1;
            }

            int aliasIndex = 0;
            while (aliasIndex < statement.BlockParameters.Count)
            {
                arguments.ContextAliases.Add(statement.BlockParameters[aliasIndex].Name);
                aliasIndex = aliasIndex + 1;
            }

            return Task.FromResult(arguments);
        });
    }

    internal Task<TemplateCompositionArguments> EvaluateLiquidIncludeArgumentsAsync(TemplateStatementSyntax statement, TemplateCompositionArguments arguments, TemplateScopeFrame frame, int index)
    {
        if (index >= statement.NamedArguments.Count)
        {
            Context.Scope.PopFrame();
            int aliasIndex = 0;
            while (aliasIndex < statement.BlockParameters.Count)
            {
                arguments.ContextAliases.Add(statement.BlockParameters[aliasIndex].Name);
                aliasIndex = aliasIndex + 1;
            }

            return Task.FromResult(arguments);
        }

        TemplateNamedArgumentSyntax parameter = statement.NamedArguments[index];
        return ContinueValueToComposition(Evaluator.EvaluateAsync(parameter.Value), (TemplateValue value) =>
        {
            arguments.Parameters[parameter.Name] = value;
            frame.SetLocal(parameter.Name, value);
            return EvaluateLiquidIncludeArgumentsAsync(statement, arguments, frame, index + 1);
        });
    }

    internal Task<string> CompleteCompositionAsync(Task<string> task, Dictionary<string, TemplateBlockOverride> previousOverrides, bool popScope)
    {
        return task.ContinueWith((Task<string> completed) =>
        {
            Context.BlockOverrides = previousOverrides;
            if (popScope)
            {
                Context.Scope.PopFrame();
            }

            Context.PopActiveInclude();
            return CopyStringTask(completed);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<string> RenderBlockAsync(TemplateBlockSyntax block)
    {
        bool handlebarsScope = Context.UseHandlebarsCompatibility && block.Name != "handlebarsInlinePartial";
        if (handlebarsScope)
        {
            Context.PushHandlebarsInlinePartialScope();
        }

        Task<string> task = RenderBlockCoreAsync(block);
        if (!handlebarsScope)
        {
            return task;
        }

        return ContinueStringTask(task, (string output) =>
        {
            Context.PopHandlebarsInlinePartialScope();
            return Task.FromResult(output);
        });
    }

    internal Task<string> RenderBlockCoreAsync(TemplateBlockSyntax block)
    {
        if (block.Name == "if" || block.Name == "unless")
        {
            return RenderIfAsync(block, block.Name == "unless");
        }

        if (block.Name == "for")
        {
            return RenderForAsync(block);
        }

        if (block.Name == "liquidTableRow")
        {
            return Task.FromResult(Renderer.RenderLiquidTableRowToString(block, Context));
        }

        if (block.Name == "case")
        {
            return RenderCaseAsync(block);
        }

        if (block.Name == "while")
        {
            return RenderWhileAsync(block, "");
        }

        if (block.Name == "with")
        {
            return RenderWithAsync(block);
        }

        if (block.Name == "capture")
        {
            return RenderCaptureAsync(block);
        }

        return Task.FromResult(Renderer.RenderBlockToString(block, Context));
    }

    internal Task<string> RenderCaseAsync(TemplateBlockSyntax block)
    {
        Task<TemplateValue> caseTask = Task.FromResult(TemplateValue.Missing());
        if (block.StartStatement.Arguments.Count > 0)
        {
            caseTask = Evaluator.EvaluateAsync(block.StartStatement.Arguments[0]);
        }

        return ContinueValueToString(caseTask, (TemplateValue caseValue) =>
        {
            return RenderCaseBranchAsync(block, caseValue, 0, -1, -1);
        });
    }

    internal Task<string> RenderCaseBranchAsync(TemplateBlockSyntax block, TemplateValue caseValue, int index, int elseStart, int elseEnd)
    {
        if (index >= block.ElseChildren.Count)
        {
            if (elseStart >= 0)
            {
                    return RenderBlockRangeAsync(block, elseStart, elseEnd);
            }

            return Task.FromResult("");
        }

        TemplateSyntaxNode node = block.ElseChildren[index];
        if (!Renderer.IsCaseBranchNode(node))
        {
            return RenderCaseBranchAsync(block, caseValue, index + 1, elseStart, elseEnd);
        }

        int next = Renderer.FindNextCaseBranch(block.ElseChildren, index + 1);
        if (node.Statement != null && node.Statement.Name == "else")
        {
            if (elseStart < 0)
            {
                elseStart = index + 1;
                elseEnd = next;
            }

            return RenderCaseBranchAsync(block, caseValue, next, elseStart, elseEnd);
        }

        if (node.Statement == null)
        {
            return RenderCaseBranchAsync(block, caseValue, next, elseStart, elseEnd);
        }

        int capturedElseStart = elseStart;
        int capturedElseEnd = elseEnd;
        return ContinueValueListToString(Evaluator.EvaluateExpressionListAsync(node.Statement.Arguments, false), (List<TemplateValue> values) =>
        {
            int valueIndex = 0;
            while (valueIndex < values.Count)
            {
                if (Renderer.CaseValueMatches(caseValue, values[valueIndex]))
                {
                    return RenderBlockRangeAsync(block, index + 1, next);
                }

                valueIndex = valueIndex + 1;
            }

            return RenderCaseBranchAsync(block, caseValue, next, capturedElseStart, capturedElseEnd);
        });
    }

    internal Task<string> RenderIfAsync(TemplateBlockSyntax block, bool invert)
    {
        if (block.StartStatement.Arguments.Count == 0)
        {
            return RenderConditionalBranchesAsync(block, 0);
        }

        return ContinueValueToString(Evaluator.EvaluateAsync(block.StartStatement.Arguments[0]), (TemplateValue value) =>
        {
            bool accepted = TemplateTruthiness.IsTruthy(value, Context.Options);
            if (invert)
            {
                accepted = !accepted;
            }

            if (accepted)
            {
                return RenderBlockChildrenAsync(block);
            }

            return RenderConditionalBranchesAsync(block, 0);
        });
    }

    internal Task<string> RenderConditionalBranchesAsync(TemplateBlockSyntax block, int index)
    {
        if (index >= block.ElseChildren.Count)
        {
            return Task.FromResult("");
        }

        TemplateSyntaxNode node = block.ElseChildren[index];
        if (!Renderer.IsConditionalBranchNode(node))
        {
            return RenderConditionalBranchesAsync(block, index + 1);
        }

        int next = Renderer.FindNextConditionalBranch(block.ElseChildren, index + 1);
        if (node.Statement != null && node.Statement.Name == "else")
        {
            return RenderBlockRangeAsync(block, index + 1, next);
        }

        if (node.Statement == null || node.Statement.Arguments.Count == 0)
        {
            return RenderConditionalBranchesAsync(block, next);
        }

        return ContinueValueToString(Evaluator.EvaluateAsync(node.Statement.Arguments[0]), (TemplateValue value) =>
        {
            if (TemplateTruthiness.IsTruthy(value, Context.Options))
            {
                return RenderBlockRangeAsync(block, index + 1, next);
            }

            return RenderConditionalBranchesAsync(block, next);
        });
    }

    internal Task<string> RenderForAsync(TemplateBlockSyntax block)
    {
        TemplateForLoopSpec spec = Renderer.ParseForLoopSpec(block.StartStatement);
        if (!spec.IsValid())
        {
            return Task.FromResult("");
        }

        return ContinueValueToString(Evaluator.EvaluateAsync(block.StartStatement.Arguments[spec.IterableArgumentIndex]), (TemplateValue iterable) =>
        {
            List<TemplateLoopIteration> iterations = Renderer.CreateLoopIterations(iterable);
            TemplateForLoopOptions options = new TemplateForLoopOptions();
            options.Reversed = spec.Reversed;
            return ContinueOptionsToString(EvaluateLoopOptionsAsync(block.StartStatement, spec, options), (TemplateForLoopOptions completedOptions) =>
            {
                string liquidOffsetKey = Renderer.GetLiquidOffsetKey(block.StartStatement, spec);
                if (Context.UseLiquidCompatibility && completedOptions.ContinueOffset && Context.LiquidForOffsets.ContainsKey(liquidOffsetKey))
                {
                    completedOptions.Offset = Context.LiquidForOffsets[liquidOffsetKey];
                }

                List<TemplateLoopIteration> selected = Renderer.ApplyForLoopOptions(iterations, completedOptions);
                if (Context.UseLiquidCompatibility)
                {
                    Context.LiquidForOffsets[liquidOffsetKey] = completedOptions.Offset + selected.Count;
                }

                if (selected.Count == 0)
                {
                    return RenderForEmptyAsync(block);
                }

                return RenderLoopIterationAsync(block, spec, selected, 0, "");
            });
        });
    }

    internal Task<TemplateForLoopOptions> EvaluateLoopOptionsAsync(TemplateStatementSyntax statement, TemplateForLoopSpec spec, TemplateForLoopOptions options)
    {
        if (spec.LimitArgumentIndex >= 0)
        {
            return ContinueValueToOptions(Evaluator.EvaluateAsync(statement.Arguments[spec.LimitArgumentIndex]), (TemplateValue value) =>
            {
                options.Limit = Renderer.ToLoopOptionCount(value);
                options.HasLimit = true;
                return EvaluateLoopOffsetAsync(statement, spec, options);
            });
        }

        return EvaluateLoopOffsetAsync(statement, spec, options);
    }

    internal Task<TemplateForLoopOptions> EvaluateLoopOffsetAsync(TemplateStatementSyntax statement, TemplateForLoopSpec spec, TemplateForLoopOptions options)
    {
        if (spec.OffsetArgumentIndex < 0)
        {
            return Task.FromResult(options);
        }

        return ContinueValueToOptions(Evaluator.EvaluateAsync(statement.Arguments[spec.OffsetArgumentIndex]), (TemplateValue value) =>
        {
            if (Context.UseLiquidCompatibility && value.IsStringLike() && value.StringValue == "__liquid_continue__")
            {
                options.ContinueOffset = true;
            }
            else
            {
                options.Offset = Renderer.ToLoopOptionCount(value);
            }

            return Task.FromResult(options);
        });
    }

    internal Task<string> RenderLoopIterationAsync(TemplateBlockSyntax block, TemplateForLoopSpec spec, List<TemplateLoopIteration> iterations, int index, string output)
    {
        if (CancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<string>(CancellationToken);
        }

        if (index >= iterations.Count)
        {
            return Task.FromResult(output);
        }

        Context.CountLoopIteration();
        TemplateLoopIteration iteration = iterations[index];
        TemplateValue parentLoop = Context.Scope.GetLocal("loop");
        TemplateScopeFrame frame = Context.Scope.PushLoopScope(iteration.CurrentValue);
        Renderer.BindLoopVariables(frame, spec, iteration, index, iterations.Count, parentLoop);
        Task<string> bodyTask = RenderBlockChildrenAsync(block);
        return bodyTask.ContinueWith((Task<string> completed) =>
        {
            Context.Scope.PopFrame();
            if (completed.IsCanceled || completed.IsFaulted)
            {
                return CopyStringTask(completed);
            }

            string nextOutput = Renderer.AppendStringOutput(Context, output, completed.Result);
            string signal = Context.FlowControlSignal;
            if (signal == "break")
            {
                Context.FlowControlSignal = "";
                return Task.FromResult(nextOutput);
            }

            if (signal == "continue")
            {
                Context.FlowControlSignal = "";
            }
            else if (signal != "")
            {
                return Task.FromResult(nextOutput);
            }

            return RenderLoopIterationAsync(block, spec, iterations, index + 1, nextOutput);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<string> RenderForEmptyAsync(TemplateBlockSyntax block)
    {
        int index = 0;
        while (index < block.ElseChildren.Count)
        {
            TemplateSyntaxNode node = block.ElseChildren[index];
            if (Renderer.IsForEmptyBranchNode(node))
            {
                int next = Renderer.FindNextForEmptyBranch(block.ElseChildren, index + 1);
                return RenderBlockRangeAsync(block, index + 1, next);
            }

            index = index + 1;
        }

        return Task.FromResult("");
    }

    internal Task<string> RenderWhileAsync(TemplateBlockSyntax block, string output)
    {
        if (CancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<string>(CancellationToken);
        }

        if (block.StartStatement.Arguments.Count == 0)
        {
            return Task.FromResult(output);
        }

        return ContinueValueToString(Evaluator.EvaluateAsync(block.StartStatement.Arguments[0]), (TemplateValue value) =>
        {
            if (!TemplateTruthiness.IsTruthy(value, Context.Options))
            {
                return Task.FromResult(output);
            }

            Context.CountLoopIteration();
            return ContinueStringTask(RenderBlockChildrenAsync(block), (string body) =>
            {
                string nextOutput = Renderer.AppendStringOutput(Context, output, body);
                if (Context.FlowControlSignal == "break")
                {
                    Context.FlowControlSignal = "";
                    return Task.FromResult(nextOutput);
                }

                if (Context.FlowControlSignal == "continue")
                {
                    Context.FlowControlSignal = "";
                }
                else if (Context.FlowControlSignal != "")
                {
                    return Task.FromResult(nextOutput);
                }

                return RenderWhileAsync(block, nextOutput);
            });
        });
    }

    internal Task<string> RenderWithAsync(TemplateBlockSyntax block)
    {
        Task<TemplateValue> task = Task.FromResult(TemplateValue.Missing());
        if (block.StartStatement.Arguments.Count > 0)
        {
            task = Evaluator.EvaluateAsync(block.StartStatement.Arguments[0]);
        }

        return ContinueValueToString(task, (TemplateValue value) =>
        {
            if (!TemplateTruthiness.IsTruthy(value, Context.Options))
            {
                return RenderWithElseAsync(block);
            }

            TemplateScopeFrame frame = Context.Scope.PushFrame(TemplateScopeFrameKind.Local, "", value);
            if (Context.UseHandlebarsCompatibility && block.StartStatement.BlockParameters.Count > 0)
            {
                frame.SetLocal(block.StartStatement.BlockParameters[0].Name, value);
            }

            Task<string> bodyTask = RenderBlockChildrenAsync(block);
            return bodyTask.ContinueWith((Task<string> completed) =>
            {
                Context.Scope.PopFrame();
                return CopyStringTask(completed);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
        });
    }

    internal Task<string> RenderWithElseAsync(TemplateBlockSyntax block)
    {
        int index = 0;
        while (index < block.ElseChildren.Count)
        {
            TemplateSyntaxNode node = block.ElseChildren[index];
            if (node.Statement != null && node.Statement.Name == "else")
            {
                return RenderBlockRangeAsync(block, index + 1, block.ElseChildren.Count);
            }

            index = index + 1;
        }

        return Task.FromResult("");
    }

    internal Task<string> RenderCaptureAsync(TemplateBlockSyntax block)
    {
        string name = Renderer.GetAssignmentVariableName(block.StartStatement);
        if (name == "")
        {
            return Task.FromResult("");
        }

        return ContinueStringTask(RenderBlockChildrenAsync(block), (string output) =>
        {
            if (Context.UseLiquidCompatibility)
            {
                Context.Scope.SetLiquidLocal(name, TemplateValue.FromString(output));
            }
            else
            {
                Context.Scope.SetLocal(name, TemplateValue.FromString(output));
            }
            return Task.FromResult("");
        });
    }

    internal Task<string> RenderBlockChildrenAsync(TemplateBlockSyntax block)
    {
        return ContinueStringTask(RenderNodesAsync(block.Children, 0, block.Children.Count, "", 0), (string output) =>
        {
            TemplateStatementSyntax? closingStatement = block.EndStatement;
            int index = 0;
            while (index < block.ElseChildren.Count)
            {
                TemplateSyntaxNode node = block.ElseChildren[index];
                if (node.Kind == TemplateSyntaxKind.Statement && node.Statement != null)
                {
                    closingStatement = node.Statement;
                    break;
                }

                index = index + 1;
            }

            return Task.FromResult(Renderer.ApplyInnerWhitespace(output, block.StartStatement, closingStatement, Context));
        });
    }

    internal Task<string> RenderBlockRangeAsync(TemplateBlockSyntax block, int startIndex, int endIndex)
    {
        return ContinueStringTask(RenderNodesAsync(block.ElseChildren, startIndex, endIndex, "", 0), (string output) =>
        {
            TemplateStatementSyntax openingStatement = block.StartStatement;
            if (startIndex > 0)
            {
                TemplateSyntaxNode openingNode = block.ElseChildren[startIndex - 1];
                if (openingNode.Statement != null)
                {
                    openingStatement = openingNode.Statement;
                }
            }

            TemplateStatementSyntax? closingStatement = block.EndStatement;
            if (endIndex < block.ElseChildren.Count)
            {
                TemplateSyntaxNode closingNode = block.ElseChildren[endIndex];
                if (closingNode.Statement != null)
                {
                    closingStatement = closingNode.Statement;
                }
            }

            return Task.FromResult(Renderer.ApplyInnerWhitespace(output, openingStatement, closingStatement, Context));
        });
    }

    internal Task<string> ContinueStringTask(Task<string> task, Func<string, Task<string>> continuation)
    {
        return task.ContinueWith((Task<string> completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled<string>(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                return Task.FromException<string>(GetTaskException(completed.Exception));
            }

            CancellationToken.ThrowIfCancellationRequested();
            return continuation(completed.Result);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<string> ContinueValueToString(Task<TemplateValue> task, Func<TemplateValue, Task<string>> continuation)
    {
        return task.ContinueWith((Task<TemplateValue> completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled<string>(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                return Task.FromException<string>(GetTaskException(completed.Exception));
            }

            CancellationToken.ThrowIfCancellationRequested();
            return continuation(completed.Result);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<string> ContinueSourceToString(Task<TemplateSource?> task, Func<TemplateSource?, Task<string>> continuation)
    {
        return task.ContinueWith((Task<TemplateSource?> completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled<string>(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                return Task.FromException<string>(GetTaskException(completed.Exception));
            }

            CancellationToken.ThrowIfCancellationRequested();
            return continuation(completed.Result);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<string> ContinueValueListToString(Task<List<TemplateValue>> task, Func<List<TemplateValue>, Task<string>> continuation)
    {
        return task.ContinueWith((Task<List<TemplateValue>> completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled<string>(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                return Task.FromException<string>(GetTaskException(completed.Exception));
            }

            CancellationToken.ThrowIfCancellationRequested();
            return continuation(completed.Result);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<string> ContinueOptionsToString(Task<TemplateForLoopOptions> task, Func<TemplateForLoopOptions, Task<string>> continuation)
    {
        return task.ContinueWith((Task<TemplateForLoopOptions> completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled<string>(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                return Task.FromException<string>(GetTaskException(completed.Exception));
            }

            CancellationToken.ThrowIfCancellationRequested();
            return continuation(completed.Result);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<string> ContinueCompositionToString(Task<TemplateCompositionArguments> task, Func<TemplateCompositionArguments, Task<string>> continuation)
    {
        return task.ContinueWith((Task<TemplateCompositionArguments> completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled<string>(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                return Task.FromException<string>(GetTaskException(completed.Exception));
            }

            CancellationToken.ThrowIfCancellationRequested();
            return continuation(completed.Result);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<TemplateCompositionArguments> ContinueValueListToComposition(Task<List<TemplateValue>> task, Func<List<TemplateValue>, Task<TemplateCompositionArguments>> continuation)
    {
        return task.ContinueWith((Task<List<TemplateValue>> completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled<TemplateCompositionArguments>(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                return Task.FromException<TemplateCompositionArguments>(GetTaskException(completed.Exception));
            }

            CancellationToken.ThrowIfCancellationRequested();
            return continuation(completed.Result);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<TemplateCompositionArguments> ContinueValueToComposition(Task<TemplateValue> task, Func<TemplateValue, Task<TemplateCompositionArguments>> continuation)
    {
        return task.ContinueWith((Task<TemplateValue> completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled<TemplateCompositionArguments>(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                return Task.FromException<TemplateCompositionArguments>(GetTaskException(completed.Exception));
            }

            CancellationToken.ThrowIfCancellationRequested();
            return continuation(completed.Result);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<TemplateForLoopOptions> ContinueValueToOptions(Task<TemplateValue> task, Func<TemplateValue, Task<TemplateForLoopOptions>> continuation)
    {
        return task.ContinueWith((Task<TemplateValue> completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled<TemplateForLoopOptions>(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                return Task.FromException<TemplateForLoopOptions>(GetTaskException(completed.Exception));
            }

            CancellationToken.ThrowIfCancellationRequested();
            return continuation(completed.Result);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<string> CopyStringTask(Task<string> completed)
    {
        if (completed.IsCanceled)
        {
            return Task.FromCanceled<string>(new CancellationToken(true));
        }

        if (completed.IsFaulted)
        {
            return Task.FromException<string>(GetTaskException(completed.Exception));
        }

        return Task.FromResult(completed.Result);
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
