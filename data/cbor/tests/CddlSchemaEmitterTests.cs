using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlSchemaEmitterTests
{
    [Test]
    public void EmitsLifecycleRulesAndNodesInStableSourceOrder()
    {
        CddlSchema schema = Compile(
            "root = [child, 3]\n"
            + "child = 1 / 2\n"
            + "unused = #6.42(\"x\")",
            false
        );
        RecordingSink sink = new RecordingSink();
        sink.MutateBeginOptions = true;
        CddlEmissionOptions options = new CddlEmissionOptions();
        options.NamePrefix = "out_";

        new CddlSchemaEmitter().EmitSchema(schema, sink, options);

        Assert.Multiple(() =>
        {
            Assert.That(sink.Events[0], Is.EqualTo("begin"));
            Assert.That(sink.Events[^1], Is.EqualTo("end"));
            Assert.That(sink.RuleContexts.Select(item => item.Rule?.Name),
                Is.EqualTo(new[] { "root", "child", "unused" }));
            Assert.That(sink.RuleContexts.Select(item => item.RuleIndex),
                Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(sink.RuleContexts.Select(item => item.OutputName),
                Is.EqualTo(new[] {
                    "out_root", "out_child", "out_unused"
                }));
            Assert.That(sink.NodeContexts.Select(item => item.NodeIndex),
                Is.EqualTo(Enumerable.Range(
                    0,
                    schema.NormalizedNodes.Length
                )));
            Assert.That(sink.NodeContexts.Select(item => item.Node),
                Is.EqualTo(schema.NormalizedNodes));
            Assert.That(sink.NodeContexts,
                Has.Count.EqualTo(schema.NormalizedNodes.Length));
            Assert.That(sink.NodeContexts.All(item =>
                item.Span.SourceName == "schema-emitter.cddl"), Is.True);
            Assert.That(sink.NodeContexts.Any(item => item.Depth > 0),
                Is.True);
            Assert.That(sink.NodeContexts.Single(item =>
                item.Node?.Kind == CddlNodeKind.RuleReference).OutputName,
                Is.EqualTo("out_child"));
            Assert.That(sink.NodeContexts[0].Options.NamePrefix,
                Is.EqualTo("out_"));
            Assert.That(sink.Contexts.Distinct().Count,
                Is.EqualTo(sink.Contexts.Count));
        });
    }

    [Test]
    public void FiltersToReachableRulesAndCanSuppressNodesAndSourceContext()
    {
        CddlSchema schema = Compile(
            "unused = 0\n"
            + "child = 1\n"
            + "root = [child, uint]",
            true
        );
        RecordingSink sink = new RecordingSink();
        CddlEmissionOptions options = new CddlEmissionOptions();
        options.RootRuleName = "root";
        options.IncludeNormalizedNodes = false;
        options.IncludeSourceContext = false;
        options.NamePrefix = "api_";

        new CddlSchemaEmitter().EmitSchema(schema, sink, options);

        Assert.Multiple(() =>
        {
            Assert.That(sink.RuleContexts.Select(item => item.Rule?.Name),
                Is.EqualTo(new[] { "child", "root" }));
            Assert.That(sink.RuleContexts.All(item =>
                item.Rule?.IsPrelude == false), Is.True);
            Assert.That(sink.RuleContexts.Select(item => item.OutputName),
                Is.EqualTo(new[] { "api_child", "api_root" }));
            Assert.That(sink.RuleContexts.All(item =>
                item.Span.SourceName == null && item.Span.Length == 0),
                Is.True);
            Assert.That(sink.NodeContexts, Is.Empty);
            Assert.That(sink.Events,
                Is.EqualTo(new[] {
                    "begin", "rule:child", "rule:root", "end"
                }));
        });
    }

    [Test]
    public void RejectsUnknownRootsAndProtectsAgainstReentrantTraversal()
    {
        CddlSchema schema = Compile("root = 1", false);
        CddlSchemaEmitter emitter = new CddlSchemaEmitter();
        CddlEmissionOptions missing = new CddlEmissionOptions();
        missing.RootRuleName = "missing";
        Assert.Throws<ArgumentException>(() => emitter.EmitSchema(
            schema,
            new RecordingSink(),
            missing
        ));

        ReenteringSink sink = new ReenteringSink();
        sink.Emitter = emitter;
        sink.Schema = schema;
        sink.Options = new CddlEmissionOptions();
        emitter.EmitSchema(schema, sink, sink.Options);

        Assert.Multiple(() =>
        {
            Assert.That(sink.ReentrantException,
                Is.TypeOf<InvalidOperationException>());
            Assert.That(sink.EndCalls, Is.EqualTo(1));
            Assert.DoesNotThrow(() => emitter.EmitSchema(
                schema,
                new RecordingSink(),
                new CddlEmissionOptions()
            ));
        });
    }

    [Test]
    public void PreservesExactSourceMappingAndRepeatedStableOrdering()
    {
        CddlSchema schema = Compile(
            "root = [\n"
            + "  child,\n"
            + "  #6.42(\"value\")\n"
            + "]\n"
            + "child = 1 / 2",
            false
        );
        RecordingSink first = new RecordingSink();
        RecordingSink repeated = new RecordingSink();
        CddlSchemaEmitter emitter = new CddlSchemaEmitter();

        emitter.EmitSchema(schema, first, new CddlEmissionOptions());
        emitter.EmitSchema(schema, repeated, new CddlEmissionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Events, Is.EqualTo(first.Events));
            Assert.That(repeated.RuleContexts.Select(item => item.RuleIndex),
                Is.EqualTo(first.RuleContexts.Select(item => item.RuleIndex)));
            Assert.That(repeated.NodeContexts.Select(item => item.NodeIndex),
                Is.EqualTo(first.NodeContexts.Select(item => item.NodeIndex)));
            Assert.That(first.NodeContexts.All(item =>
                SpansEqual(item.Span, schema.SourceMap[item.NodeIndex])),
                Is.True);
            Assert.That(first.NodeContexts.All(item =>
                !ReferenceEquals(
                    item.Span,
                    schema.SourceMap[item.NodeIndex]
                )), Is.True);
            Assert.That(first.RuleContexts.All(item =>
                SpansEqual(item.Span, item.Rule!.Span)), Is.True);
            AssertLifecycleContext(first.Contexts[0], schema);
            AssertLifecycleContext(first.Contexts[^1], schema);
        });
    }

    [Test]
    public void IncludesPreludeOnRequestAndFiltersRecursiveReachability()
    {
        CddlSchema schema = Compile(
            "unused = 0\n"
            + "root = child\n"
            + "child = root / uint",
            true
        );
        RecordingSink prelude = new RecordingSink();
        CddlEmissionOptions preludeOptions = new CddlEmissionOptions();
        preludeOptions.IncludePreludeRules = true;
        preludeOptions.IncludeNormalizedNodes = false;
        new CddlSchemaEmitter().EmitSchema(
            schema,
            prelude,
            preludeOptions
        );
        RecordingSink filtered = new RecordingSink();
        CddlEmissionOptions filteredOptions = new CddlEmissionOptions();
        filteredOptions.RootRuleName = "root";
        new CddlSchemaEmitter().EmitSchema(
            schema,
            filtered,
            filteredOptions
        );

        Assert.Multiple(() =>
        {
            Assert.That(prelude.RuleContexts.Any(item =>
                item.Rule?.IsPrelude == true), Is.True);
            Assert.That(prelude.RuleContexts.Where(item =>
                item.Rule?.IsPrelude == true).All(item =>
                item.Span.SourceName == "<standard-prelude>"), Is.True);
            Assert.That(filtered.RuleContexts.Select(item => item.Rule?.Name),
                Is.EqualTo(new[] { "root", "child" }));
            Assert.That(filtered.RuleContexts.All(item =>
                item.Rule?.IsPrelude == false), Is.True);
            Assert.That(filtered.NodeContexts.Select(item => item.NodeIndex)
                .Distinct().Count(), Is.EqualTo(filtered.NodeContexts.Count));
            Assert.That(filtered.Events[0], Is.EqualTo("begin"));
            Assert.That(filtered.Events[^1], Is.EqualTo("end"));
        });
    }

    [TestCase("begin")]
    [TestCase("rule")]
    [TestCase("node")]
    [TestCase("end")]
    public void WrapsSinkFailuresWithTraversalContextAndRemainsReusable(
        string phase)
    {
        CddlSchema schema = Compile("root = [1]", false);
        CddlSchemaEmitter emitter = new CddlSchemaEmitter();
        ThrowingSink sink = new ThrowingSink();
        sink.Phase = phase;

        CddlEmissionException? exception = Assert.Throws<
            CddlEmissionException>(() => emitter.EmitSchema(
                schema,
                sink,
                new CddlEmissionOptions()
            ));
        RecordingSink retry = new RecordingSink();
        emitter.EmitSchema(schema, retry, new CddlEmissionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(exception?.ErrorCode,
                Is.EqualTo("CDDL_EMISSION_SINK_EXCEPTION"));
            Assert.That(exception?.Message, Does.Contain(phase));
            Assert.That(exception?.RuleName,
                phase == "rule" || phase == "node"
                    ? Is.EqualTo("root")
                    : Is.Null);
            Assert.That(exception?.RuleIndex,
                phase == "rule" || phase == "node"
                    ? Is.EqualTo(0)
                    : Is.EqualTo(-1));
            Assert.That(exception?.NodeIndex,
                phase == "node" ? Is.EqualTo(0) : Is.EqualTo(-1));
            Assert.That(exception?.Span?.SourceName,
                phase == "rule" || phase == "node"
                    ? Is.EqualTo("schema-emitter.cddl")
                    : Is.Null);
            Assert.That(sink.EndCalls,
                phase == "end" ? Is.EqualTo(1) : Is.EqualTo(0));
            Assert.That(retry.Events[^1], Is.EqualTo("end"));
        });
    }

    [Test]
    public void ValidatesInputsBeforeStartingLifecycleAndEmitsEmptySchemas()
    {
        CddlSchemaEmitter emitter = new CddlSchemaEmitter();
        CddlSchema schema = Compile("root = 1", false);
        RecordingSink sink = new RecordingSink();
        Assert.Throws<ArgumentNullException>(() => emitter.EmitSchema(
            null!,
            sink,
            new CddlEmissionOptions()
        ));
        Assert.Throws<ArgumentNullException>(() => emitter.EmitSchema(
            schema,
            null!,
            new CddlEmissionOptions()
        ));
        Assert.Throws<ArgumentNullException>(() => emitter.EmitSchema(
            schema,
            sink,
            null!
        ));
        CddlEmissionOptions missing = new CddlEmissionOptions();
        missing.RootRuleName = "missing";
        Assert.Throws<ArgumentException>(() => emitter.EmitSchema(
            schema,
            sink,
            missing
        ));
        Assert.That(sink.Events, Is.Empty);

        RecordingSink empty = new RecordingSink();
        emitter.EmitSchema(
            new CddlSchema(),
            empty,
            new CddlEmissionOptions()
        );

        Assert.That(empty.Events, Is.EqualTo(new[] { "begin", "end" }));
    }

    private static bool SpansEqual(
        CddlSourceSpan left,
        CddlSourceSpan right)
    {
        return left.SourceName == right.SourceName
            && left.Start == right.Start
            && left.Length == right.Length
            && left.StartLine == right.StartLine
            && left.StartColumn == right.StartColumn
            && left.EndLine == right.EndLine
            && left.EndColumn == right.EndColumn;
    }

    private static void AssertLifecycleContext(
        CddlEmissionContext context,
        CddlSchema schema)
    {
        Assert.Multiple(() =>
        {
            Assert.That(context.Schema, Is.SameAs(schema));
            Assert.That(context.Rule, Is.Null);
            Assert.That(context.Node, Is.Null);
            Assert.That(context.RuleIndex, Is.EqualTo(-1));
            Assert.That(context.NodeIndex, Is.EqualTo(-1));
            Assert.That(context.Depth, Is.EqualTo(0));
            Assert.That(context.OutputName, Is.Null);
        });
    }

    private static CddlSchema Compile(string source, bool includePrelude)
    {
        CddlDocument document = new CddlParser().ParseDocument(
            source,
            new CddlParseOptions
            {
                SourceName = "schema-emitter.cddl"
            }
        );
        return new CddlCompiler().CompileSchema(
            document,
            new CddlCompileOptions
            {
                IncludeStandardPrelude = includePrelude
            }
        );
    }

    private class RecordingSink : ICddlSchemaSink
    {
        public bool MutateBeginOptions;
        public List<string> Events = new List<string>();
        public List<CddlEmissionContext> Contexts =
            new List<CddlEmissionContext>();
        public List<CddlEmissionContext> RuleContexts =
            new List<CddlEmissionContext>();
        public List<CddlEmissionContext> NodeContexts =
            new List<CddlEmissionContext>();

        public void BeginSchema(CddlEmissionContext context)
        {
            Events.Add("begin");
            Contexts.Add(context);
            if (MutateBeginOptions)
            {
                context.Options.IncludeNormalizedNodes = false;
                context.Options.NamePrefix = "mutated_";
            }
        }

        public void AcceptRule(CddlEmissionContext context)
        {
            Events.Add("rule:" + context.Rule?.Name);
            Contexts.Add(context);
            RuleContexts.Add(context);
        }

        public void AcceptNode(CddlEmissionContext context)
        {
            Events.Add("node:" + context.NodeIndex);
            Contexts.Add(context);
            NodeContexts.Add(context);
        }

        public void EndSchema(CddlEmissionContext context)
        {
            Events.Add("end");
            Contexts.Add(context);
        }
    }

    private class ReenteringSink : ICddlSchemaSink
    {
        public CddlSchemaEmitter? Emitter;
        public CddlSchema? Schema;
        public CddlEmissionOptions Options = new CddlEmissionOptions();
        public Exception? ReentrantException;
        public int EndCalls;

        public void BeginSchema(CddlEmissionContext context)
        {
            ReentrantException = Assert.Throws<InvalidOperationException>(
                () => Emitter!.EmitSchema(Schema!, this, Options)
            );
        }

        public void AcceptRule(CddlEmissionContext context)
        {
        }

        public void AcceptNode(CddlEmissionContext context)
        {
        }

        public void EndSchema(CddlEmissionContext context)
        {
            EndCalls = EndCalls + 1;
        }
    }

    private class ThrowingSink : ICddlSchemaSink
    {
        public string Phase = "";
        public int EndCalls;

        public void BeginSchema(CddlEmissionContext context)
        {
            if (Phase == "begin")
            {
                throw new InvalidOperationException("begin failed");
            }
        }

        public void AcceptRule(CddlEmissionContext context)
        {
            if (Phase == "rule")
            {
                throw new InvalidOperationException("rule failed");
            }
        }

        public void AcceptNode(CddlEmissionContext context)
        {
            if (Phase == "node")
            {
                throw new InvalidOperationException("node failed");
            }
        }

        public void EndSchema(CddlEmissionContext context)
        {
            EndCalls = EndCalls + 1;
            if (Phase == "end")
            {
                throw new InvalidOperationException("end failed");
            }
        }
    }
}
