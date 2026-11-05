namespace cbor;

internal class CddlCompilationPipeline
{
    internal CddlDocument Document;
    internal CddlCompileOptions Options;

    internal CddlCompilationPipeline(
        CddlDocument document,
        CddlCompileOptions options)
    {
        Document = document;
        Options = options;
    }

    internal CddlCompileResult CompileDocument()
    {
        CddlCompileResult result = new CddlCompileResult();
        result.Options = Options;
        CddlDiagnosticCollector diagnostics = new CddlDiagnosticCollector(
            Document.SourceName,
            Options.MaxDiagnostics
        );
        diagnostics.AddDiagnostics(Document.Diagnostics);
        if (HasDocumentErrors())
        {
            result.Diagnostics = diagnostics.CreateOrderedArray();
            return result;
        }

        CddlRule[] loaded = new CddlPreludeLoader().LoadRules(
            Document,
            Options
        );
        CddlRuleTable table = new CddlRuleTable(loaded, Options);
        diagnostics.AddDiagnostics(table.Diagnostics);
        if (table.HasErrors)
        {
            diagnostics.MarkError();
        }
        CddlResolutionResult resolution = new CddlReferenceResolver(
            table,
            Document,
            Options
        ).ResolveDocument();
        diagnostics.AddDiagnostics(resolution.Diagnostics);
        if (resolution.HasErrors)
        {
            diagnostics.MarkError();
        }
        if (resolution.RootRule == null
            && !table.HasErrors
            && !resolution.HasErrors)
        {
            diagnostics.AddDiagnostic(CreateMissingRootDiagnostic());
        }

        CddlGenericExpansionResult expansion = new CddlGenericExpander(
            table,
            resolution,
            Options
        ).ExpandDocument();
        diagnostics.AddDiagnostics(expansion.Diagnostics);
        if (expansion.HasErrors)
        {
            diagnostics.MarkError();
        }
        CddlNormalizationResult normalization = new CddlSchemaNormalizer(
            table,
            resolution,
            expansion,
            Options
        ).NormalizeDocument();
        diagnostics.AddDiagnostics(normalization.Diagnostics);
        if (normalization.HasErrors)
        {
            diagnostics.MarkError();
        }

        if (!normalization.HasErrors)
        {
            CddlReferenceGraphResult graph = new CddlReferenceGraphBuilder(
                normalization,
                Options
            ).BuildGraph();
            diagnostics.AddDiagnostics(graph.Diagnostics);
            if (graph.HasErrors)
            {
                diagnostics.MarkError();
            }
            CddlSemanticResult semantics = new CddlSemanticChecker(
                normalization,
                Options
            ).CheckDocument();
            diagnostics.AddDiagnostics(semantics.Diagnostics);
            if (semantics.HasErrors)
            {
                diagnostics.MarkError();
            }
        }

        result.Diagnostics = diagnostics.CreateOrderedArray();
        if (diagnostics.HasErrors)
        {
            return result;
        }
        result.Schema = new CddlSchemaMaterializer(
            Document,
            normalization,
            Options
        ).MaterializeSchema();
        result.IsSuccess = true;
        return result;
    }

    internal bool HasDocumentErrors()
    {
        int index = 0;
        while (index < Document.Diagnostics.Count)
        {
            if (Document.Diagnostics[index].Severity
                == CddlDiagnosticSeverity.Error)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal CddlDiagnostic CreateMissingRootDiagnostic()
    {
        CddlDiagnostic diagnostic = new CddlDiagnostic();
        diagnostic.Severity = CddlDiagnosticSeverity.Error;
        diagnostic.Code = "CDDL_COMPILE_ROOT_MISSING";
        diagnostic.Message = "The CDDL document has no selectable root rule.";
        diagnostic.Span.SourceName = Document.SourceName;
        diagnostic.Span.StartLine = 1;
        diagnostic.Span.StartColumn = 1;
        diagnostic.Span.EndLine = 1;
        diagnostic.Span.EndColumn = 1;
        return diagnostic;
    }
}
