using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlValidationStateTests
{
    [Test]
    public void SelectsExplicitRootAndSnapshotsOptionsAndFeatures()
    {
        CddlSchema schema = CompileSchema(
            "root = 1\nalternate = 2");
        CddlValidationOptions options = new CddlValidationOptions
        {
            RootRuleName = "alternate",
            EnabledFeatures = new string[] { "alpha", "Alpha", "alpha" },
            MaxSteps = 7
        };
        CddlValidationState state = new CddlValidationState(
            schema,
            new CborValue(),
            options);

        options.RootRuleName = "missing";
        options.EnabledFeatures[0] = "changed";
        options.MaxSteps = 1;

        Assert.Multiple(() =>
        {
            Assert.That(state.InitializeValidation(), Is.True);
            Assert.That(state.SelectedRule!.Name, Is.EqualTo("alternate"));
            Assert.That(state.Result.RuleName, Is.EqualTo("alternate"));
            Assert.That(state.Options, Is.Not.SameAs(options));
            Assert.That(state.Options.RootRuleName, Is.EqualTo("alternate"));
            Assert.That(state.Options.MaxSteps, Is.EqualTo(7));
            Assert.That(state.Features.CreateFeatureArray(),
                Is.EqualTo(new string[] { "alpha", "Alpha" }));
            Assert.That(state.Features.ContainsFeature("alpha"), Is.True);
            Assert.That(state.Features.ContainsFeature("Alpha"), Is.True);
            Assert.That(state.Features.ContainsFeature("ALPHA"), Is.False);
        });
    }

    [Test]
    public void TracksReversibleRuleInstanceAndSchemaPaths()
    {
        CddlSchema schema = CompileSchema("root = [1]");
        CddlValidationState state = new CddlValidationState(
            schema,
            new CborValue(),
            new CddlValidationOptions());
        Assert.That(state.InitializeValidation(), Is.True);
        CddlRule root = state.SelectedRule!;
        Assert.That(state.EnterRule(root), Is.True);
        state.InstancePath.PushArrayIndex(2);
        state.InstancePath.PushMapTextKey("key\"");
        state.SchemaPath.PushNodeIndex(0);
        state.SchemaPath.PushChoiceIndex(1);
        CddlValidationIssue issue = state.CreateIssue(
            CddlValidationCode.LiteralMismatch,
            "original",
            root.Value,
            "1",
            "2");
        state.AddIssue(issue);
        int expectedStart = issue.Span.Start;

        issue.Message = "changed";
        issue.Span.Start = 999;
        state.InstancePath.PopSegment();
        state.InstancePath.PopSegment();
        state.SchemaPath.PopSegment();
        state.SchemaPath.PopSegment();
        state.LeaveRule();
        CddlValidationResult result = state.CompleteValidation(false);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Message, Is.EqualTo("original"));
            Assert.That(result.Issues[0].InstancePath,
                Is.EqualTo("$[2][\"key\\\"\"]"));
            Assert.That(result.Issues[0].SchemaPath,
                Is.EqualTo("/rule(root)/node[0]/choice[1]"));
            Assert.That(result.Issues[0].RuleName, Is.EqualTo("root"));
            Assert.That(result.Issues[0].Span.Start,
                Is.EqualTo(expectedStart));
            Assert.That(state.RuleStack.Rules, Is.Empty);
            Assert.That(state.Budget.NestingDepth, Is.Zero);
            Assert.That(state.InstancePath.CreatePath(), Is.EqualTo("$"));
            Assert.That(state.SchemaPath.CreatePath(), Is.Empty);
        });
    }

    [Test]
    public void ProvidesPathsForMapEntriesTagsAndControls()
    {
        CddlInstancePath instance = new CddlInstancePath();
        CddlSchemaPath schema = new CddlSchemaPath();
        instance.PushMapEntryIndex(3);
        instance.PushTaggedValue();
        schema.PushRuleName("root");
        schema.PushControlName(".size");

        Assert.Multiple(() =>
        {
            Assert.That(instance.CreatePath(),
                Is.EqualTo("${entry:3}.value"));
            Assert.That(schema.CreatePath(),
                Is.EqualTo("/rule(root)/control(.size)"));
        });

        instance.PopSegment();
        instance.PopSegment();
        schema.PopSegment();
        schema.PopSegment();

        Assert.Multiple(() =>
        {
            Assert.That(instance.CreatePath(), Is.EqualTo("$"));
            Assert.That(schema.CreatePath(), Is.Empty);
        });
    }

    [Test]
    public void EnforcesEveryBudgetAndReportsEachLimitOnlyOnce()
    {
        CddlSchema schema = CompileSchema("root = 1");
        CddlValidationOptions options = new CddlValidationOptions
        {
            MaxIssues = 20,
            MaxNestingDepth = 1,
            MaxSteps = 1,
            MaxAlternatives = 1,
            MaxMemoEntries = 1,
            MaxEmbeddedBytes = 3,
            MaxPatternStates = 2
        };
        CborValue first = new CborValue();
        CborValue second = new CborValue();
        CddlValidationState state = new CddlValidationState(
            schema,
            first,
            options);
        Assert.That(state.InitializeValidation(), Is.True);
        CddlRule root = state.SelectedRule!;
        CddlNode node = root.Value!;

        Assert.Multiple(() =>
        {
            Assert.That(state.TakeStep(node), Is.True);
            Assert.That(state.TakeStep(node), Is.False);
            Assert.That(state.TakeStep(node), Is.False);
            Assert.That(state.TakeAlternative(node), Is.True);
            Assert.That(state.TakeAlternative(node), Is.False);
            Assert.That(state.EnterRule(root), Is.True);
            Assert.That(state.EnterDataNesting(node), Is.False);
        });
        state.LeaveRule();
        CddlValidationMemoEntry? memo = state.BeginMemo(
            root,
            first,
            0,
            0,
            node);
        CddlValidationMemoEntry? repeated = state.BeginMemo(
            root,
            first,
            0,
            0,
            node);
        CddlValidationMemoEntry? overMemo = state.BeginMemo(
            root,
            second,
            0,
            0,
            node);
        state.BeginMemo(root, second, 0, 0, node);

        Assert.Multiple(() =>
        {
            Assert.That(memo, Is.Not.Null);
            Assert.That(repeated, Is.SameAs(memo));
            Assert.That(overMemo, Is.Null);
            Assert.That(state.TakeEmbeddedBytes(2, node), Is.True);
            Assert.That(state.TakeEmbeddedBytes(2, node), Is.False);
            Assert.That(state.TakePatternStates(2, node), Is.True);
            Assert.That(state.TakePatternStates(1, node), Is.False);
        });
        CddlValidationResult result = state.CompleteValidation(false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues, Has.Length.EqualTo(6));
            Assert.That(result.Issues.Select(issue => issue.Code),
                Is.All.EqualTo(CddlValidationCode.ResourceLimit));
            Assert.That(state.ReportedLimits,
                Is.EqualTo(new string[]
                {
                    "validation steps", "alternatives", "nesting depth",
                    "memo entries", "embedded bytes", "pattern states"
                }));
            Assert.That(state.Budget.Steps, Is.EqualTo(1));
            Assert.That(state.Budget.Alternatives, Is.EqualTo(1));
            Assert.That(state.Budget.MemoEntries, Is.EqualTo(1));
            Assert.That(state.Budget.EmbeddedBytes, Is.EqualTo(2));
            Assert.That(state.Budget.PatternStates, Is.EqualTo(2));
        });
    }

    [Test]
    public void MemoizesByRuleValuePositionAndContextIdentity()
    {
        CddlSchema schema = CompileSchema("root = 1");
        CddlValidationOptions options = new CddlValidationOptions
        {
            MaxMemoEntries = 4
        };
        CborValue first = new CborValue { Kind = CborValueKind.UnsignedInteger };
        CborValue equalButDistinct = new CborValue
        {
            Kind = CborValueKind.UnsignedInteger
        };
        CddlValidationState state = new CddlValidationState(
            schema,
            first,
            options);
        Assert.That(state.InitializeValidation(), Is.True);
        CddlRule root = state.SelectedRule!;
        CddlValidationMemoEntry firstEntry = state.BeginMemo(
            root,
            first,
            2,
            3,
            root.Value)!;
        CddlValidationMemoEntry sameEntry = state.BeginMemo(
            root,
            first,
            2,
            3,
            root.Value)!;
        CddlValidationMemoEntry otherContext = state.BeginMemo(
            root,
            first,
            2,
            4,
            root.Value)!;
        CddlValidationMemoEntry otherValue = state.BeginMemo(
            root,
            equalButDistinct,
            2,
            3,
            root.Value)!;
        CddlValidationIssue rejection = state.CreateIssue(
            CddlValidationCode.TypeMismatch,
            "rejected",
            root.Value,
            "integer",
            "text");

        firstEntry.CompleteRejection(new CddlValidationIssue[] { rejection });
        otherContext.CompleteMatch();

        Assert.Multiple(() =>
        {
            Assert.That(firstEntry, Is.SameAs(sameEntry));
            Assert.That(otherContext, Is.Not.SameAs(firstEntry));
            Assert.That(otherValue, Is.Not.SameAs(firstEntry));
            Assert.That(firstEntry.Status,
                Is.EqualTo(CddlValidationMemoEntry.Rejected));
            Assert.That(firstEntry.Issues, Has.Length.EqualTo(1));
            Assert.That(otherContext.Status,
                Is.EqualTo(CddlValidationMemoEntry.Matched));
            Assert.That(otherValue.Status,
                Is.EqualTo(CddlValidationMemoEntry.Pending));
            Assert.That(state.Memo.Entries, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void BoundsTheEntireDiagnosticTreeAndClonesStoredIssues()
    {
        CddlValidationIssue grandchild = Issue(
            CddlValidationCode.LiteralMismatch,
            "grandchild");
        CddlValidationIssue first = Issue(
            CddlValidationCode.TypeMismatch,
            "first");
        first.Details = new CddlValidationIssue[] { grandchild };
        CddlValidationIssue second = Issue(
            CddlValidationCode.RangeMismatch,
            "second");
        CddlValidationIssue root = Issue(
            CddlValidationCode.ChoiceMismatch,
            "root");
        root.Details = new CddlValidationIssue[] { first, second };
        CddlValidationDiagnostics diagnostics =
            new CddlValidationDiagnostics(3, true, 10);
        diagnostics.AddIssue(root);
        CddlValidationIssue[] stored = diagnostics.CreateIssueArray();

        root.Message = "changed";
        first.Message = "changed";
        grandchild.Span.Start = 999;

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.HasIssues, Is.True);
            Assert.That(diagnostics.HasErrors, Is.True);
            Assert.That(diagnostics.StoredCount, Is.EqualTo(3));
            Assert.That(stored, Has.Length.EqualTo(1));
            Assert.That(stored[0].Message, Is.EqualTo("root"));
            Assert.That(stored[0].Details, Has.Length.EqualTo(1));
            Assert.That(stored[0].Details[0].Message, Is.EqualTo("first"));
            Assert.That(stored[0].Details[0].Details,
                Has.Length.EqualTo(1));
            Assert.That(stored[0].Details[0].Details[0].Message,
                Is.EqualTo("grandchild"));
            Assert.That(stored[0].Details[0].Details[0].Span.Start,
                Is.Not.EqualTo(999));
        });
    }

    [Test]
    public void HonorsSingleIssueZeroIssueAndDiagnosticDepthPolicies()
    {
        CddlValidationIssue detail = Issue(
            CddlValidationCode.TypeMismatch,
            "detail");
        CddlValidationIssue root = Issue(
            CddlValidationCode.ChoiceMismatch,
            "root");
        root.Details = new CddlValidationIssue[] { detail };
        CddlValidationDiagnostics single =
            new CddlValidationDiagnostics(20, false, 20);
        CddlValidationDiagnostics zero =
            new CddlValidationDiagnostics(0, true, 20);
        CddlValidationDiagnostics shallow =
            new CddlValidationDiagnostics(20, true, 0);

        single.AddIssue(root);
        single.AddIssue(detail);
        zero.AddIssue(root);
        shallow.AddIssue(root);

        Assert.Multiple(() =>
        {
            Assert.That(single.CreateIssueArray(), Has.Length.EqualTo(1));
            Assert.That(single.CreateIssueArray()[0].Details, Is.Empty);
            Assert.That(zero.HasIssues, Is.True);
            Assert.That(zero.HasErrors, Is.True);
            Assert.That(zero.CreateIssueArray(), Is.Empty);
            Assert.That(shallow.CreateIssueArray(), Has.Length.EqualTo(1));
            Assert.That(shallow.CreateIssueArray()[0].Details, Is.Empty);
        });
    }

    [Test]
    public void ReportsMissingAndNonTypeSelectedRulesAsInvalidSchemas()
    {
        CddlSchema compiled = CompileSchema("root = 1");
        CddlValidationOptions missingOptions = new CddlValidationOptions
        {
            RootRuleName = "missing"
        };
        CddlValidationState missing = new CddlValidationState(
            compiled,
            new CborValue(),
            missingOptions);
        CddlRule group = new CddlRule
        {
            Name = "group",
            Kind = CddlRuleKind.Group,
            Value = new CddlNode { Kind = CddlNodeKind.Group }
        };
        CddlSchema invalidSchema = new CddlSchema
        {
            Rules = new CddlRule[] { group },
            RootRule = group
        };
        CddlValidationState invalid = new CddlValidationState(
            invalidSchema,
            new CborValue(),
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(missing.InitializeValidation(), Is.False);
            Assert.That(missing.CompleteValidation(false).Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidSchema));
            Assert.That(missing.Result.RuleName, Is.Null);
            Assert.That(invalid.InitializeValidation(), Is.False);
            Assert.That(invalid.Result.RuleName, Is.EqualTo("group"));
            Assert.That(invalid.CompleteValidation(false).Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidSchema));
        });
    }

    private static CddlValidationIssue Issue(
        CddlValidationCode code,
        string message)
    {
        return new CddlValidationIssue
        {
            Code = code,
            Message = message,
            Span = new CddlSourceSpan
            {
                SourceName = "schema.cddl",
                Start = message.Length,
                Length = 1
            }
        };
    }

    private static CddlSchema CompileSchema(string source)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "state.cddl" });
        Assert.That(parse.IsSuccess, Is.True);
        Assert.That(parse.Document, Is.Not.Null);
        CddlCompileOptions options = new CddlCompileOptions
        {
            IncludeStandardPrelude = false
        };
        CddlCompileResult compile = new CddlCompiler().TryCompileSchema(
            parse.Document!,
            options);
        Assert.That(compile.IsSuccess, Is.True);
        Assert.That(compile.Schema, Is.Not.Null);
        return compile.Schema!;
    }
}
