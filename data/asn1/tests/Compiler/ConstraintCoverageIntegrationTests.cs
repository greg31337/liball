using asn1;

namespace asn1.Tests;

public class ConstraintCoverageIntegrationTests
{
    [Test]
    public void SemanticPipelineCoversEveryConstraintAndSetOperatorKind()
    {
        ResolutionFixture fixture = Resolve(
            "Coverage DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "Items ITEM ::= { { &id 1, &Type INTEGER } }; "
                + "Base ::= INTEGER (1..10); "
                + "Single ::= INTEGER (1); "
                + "Subtype ::= INTEGER (INCLUDES Base); "
                + "Sized ::= UTF8String (SIZE (1..4)); "
                + "Alphabet ::= UTF8String (FROM (\"A\"..\"Z\")); "
                + "Patterned ::= UTF8String (PATTERN \"^[A-Z]+$\"); "
                + "Clock ::= TIME (SETTINGS \"Basic=Time Time=HMS\"); "
                + "OpenPayload ::= ANY (Base); "
                + "Numbers ::= SEQUENCE (WITH COMPONENT (1..3)) OF INTEGER; "
                + "Record ::= SEQUENCE { value INTEGER OPTIONAL } "
                + "(WITH COMPONENTS { ..., value PRESENT }); "
                + "Opaque ::= INTEGER (CONSTRAINED BY {}); "
                + "Simple ::= ITEM.&Type ({Items}); "
                + "Envelope ::= SEQUENCE { id ITEM.&id ({Items}), "
                + "payload ITEM.&Type ({Items}{@id}) }; "
                + "Encoded ::= OCTET STRING (CONTAINING Base); "
                + "Operators ::= INTEGER "
                + "(((((1 | 2 | 3) ^ (2 | 3 | 4)) EXCEPT 4) "
                + "| (ALL EXCEPT 100)), ..., 9 ! -2) END");
        System.Collections.Generic.HashSet<AsnConstraintKind> observed =
            new System.Collections.Generic.HashSet<AsnConstraintKind>();
        System.Collections.Generic.List<ResolvedTypeSyntax> visited =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();

        foreach (SymbolTableEntry symbol in fixture.Tables[0].TypeSymbols)
        {
            if (symbol.ResolvedType != null)
            {
                CollectTypeKinds(symbol.ResolvedType, observed, visited);
            }
        }

        AsnConstraintKind[] expected = System.Enum.GetValues<AsnConstraintKind>()
            .Where(item => item != AsnConstraintKind.Unknown).ToArray();
        Assert.That(observed.OrderBy(item => item), Is.EqualTo(expected));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            Format(fixture.Diagnostics));
    }

    [Test]
    public void RuntimeEvaluatorHonorsRangeSizeAlphabetAndExtensionBoundaries()
    {
        ResolutionFixture fixture = Resolve(
            "Boundaries DEFINITIONS ::= BEGIN "
                + "Closed ::= INTEGER (0..10); "
                + "LowerOpen ::= INTEGER (0<..10); "
                + "UpperOpen ::= INTEGER (0..<10); "
                + "KnownExtension ::= INTEGER (1..3, ..., 10..12); "
                + "OpenExtension ::= INTEGER (1..3, ...); "
                + "Sized ::= UTF8String (SIZE (2..4)); "
                + "Alphabet ::= UTF8String (FROM (\"A\"..\"C\")) END");

        AssertIntegerValidity(fixture, "Closed", 0, true);
        AssertIntegerValidity(fixture, "Closed", 10, true);
        AssertIntegerValidity(fixture, "Closed", -1, false);
        AssertIntegerValidity(fixture, "Closed", 11, false);
        AssertIntegerValidity(fixture, "LowerOpen", 0, false);
        AssertIntegerValidity(fixture, "LowerOpen", 1, true);
        AssertIntegerValidity(fixture, "UpperOpen", 9, true);
        AssertIntegerValidity(fixture, "UpperOpen", 10, false);
        AssertIntegerValidity(fixture, "KnownExtension", 3, true);
        AssertIntegerValidity(fixture, "KnownExtension", 10, true);
        AssertIntegerValidity(fixture, "KnownExtension", 12, true);
        AssertIntegerValidity(fixture, "KnownExtension", 9, true);
        AssertIntegerValidity(fixture, "KnownExtension", 13, true);
        AssertIntegerValidity(fixture, "OpenExtension", 100, true);

        AssertTextValidity(fixture, "Sized", "A", false);
        AssertTextValidity(fixture, "Sized", "A😀", true);
        AssertTextValidity(fixture, "Sized", "ABCD", true);
        AssertTextValidity(fixture, "Sized", "ABCDE", false);
        AssertTextValidity(fixture, "Alphabet", "A", true);
        AssertTextValidity(fixture, "Alphabet", "C", true);
        AssertTextValidity(fixture, "Alphabet", "@", false);
        AssertTextValidity(fixture, "Alphabet", "D", false);
    }

    [Test]
    public void RuntimeEvaluatorHonorsPatternSettingsAndInnerPresenceFailures()
    {
        ResolutionFixture fixture = Resolve(
            "RuntimeForms DEFINITIONS ::= BEGIN "
                + "Code ::= UTF8String (PATTERN \"^[A-Z]{2}[0-9]$\"); "
                + "Clock ::= TIME "
                + "(SETTINGS \"Basic=Time Time=HMS Local-or-UTC=Z\"); "
                + "Record ::= SEQUENCE { value INTEGER OPTIONAL } "
                + "(WITH COMPONENTS { ..., value (1..3) PRESENT }); "
                + "Numbers ::= SEQUENCE (WITH COMPONENT (1..3)) OF INTEGER END");

        AssertTextValidity(fixture, "Code", "AB7", true);
        AssertTextValidity(fixture, "Code", "Ab7", false);

        AsnRuntimeValue clock = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Time,
            TextValue = "12:34:56Z",
            TimeValue = new AsnTimeValue
            {
                Kind = AsnTimeKind.TimeOfDay,
                Text = "12:34:56Z"
            }
        };
        Assert.That(Validate(fixture, "Clock", clock), Is.Empty);
        clock.TextValue = "12:34:56";
        clock.TimeValue.Text = clock.TextValue;
        AssertFailure(fixture, "Clock", clock, AsnConstraintKind.PropertySettings, "$");

        AsnRuntimeValue missing = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence
        };
        AssertFailure(fixture, "Record", missing,
            AsnConstraintKind.WithComponents, "$.value");
        AsnRuntimeValue invalidField = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                new AsnRuntimeFieldValue
                {
                    Name = "value",
                    Present = true,
                    Value = IntegerValue(4)
                }
            ]
        };
        AssertFailure(fixture, "Record", invalidField,
            AsnConstraintKind.ValueRange, "$.value");

        AsnRuntimeValue invalidItem = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SequenceOf,
            Items = [IntegerValue(1), IntegerValue(4)]
        };
        AssertFailure(fixture, "Numbers", invalidItem,
            AsnConstraintKind.ValueRange, "$[1]");
    }

    [Test]
    public void CompilerDiagnosesContradictionsAndRetainsExceptionSpecifications()
    {
        ResolutionFixture range = ResolveAllowingFailure(
            "RangeContradiction DEFINITIONS ::= BEGIN "
                + "Value ::= INTEGER (1..3) (4..6) END");
        ResolutionFixture size = ResolveAllowingFailure(
            "SizeContradiction DEFINITIONS ::= BEGIN "
                + "Value ::= UTF8String (SIZE (1..2)) (SIZE (3..4)) END");
        ResolutionFixture alphabet = ResolveAllowingFailure(
            "AlphabetContradiction DEFINITIONS ::= BEGIN "
                + "Value ::= UTF8String (SIZE (1..MAX)) "
                + "(FROM (\"A\")) (FROM (\"B\")) END");

        Assert.That(range.Values.Success, Is.False);
        Assert.That(range.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain("ASNC0027"));
        Assert.That(size.Values.Success, Is.False);
        Assert.That(size.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain("ASNC0002"));
        Assert.That(alphabet.Values.Success, Is.False);
        Assert.That(alphabet.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain("ASNC0027"));

        ResolutionFixture valid = Resolve(
            "Exceptions DEFINITIONS ::= BEGIN "
                + "problemCode INTEGER ::= -17; "
                + "Reason ::= UTF8String; "
                + "Number ::= INTEGER (1..10 ! problemCode); "
                + "Text ::= UTF8String (SIZE (1..4) ! Reason : \"bad\") END");
        ResolvedTypeSyntax number = RequireType(valid, "Number");
        ResolvedTypeSyntax text = RequireType(valid, "Text");

        Assert.That(number.ConstraintExceptions.Single().IdentifierText,
            Is.EqualTo("problemCode"));
        Assert.That(number.ConstraintExceptions.Single().ExceptionValue?
            .EffectiveValue?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(-17)));
        Assert.That(text.ConstraintExceptions.Single().ExceptionType?.EffectiveKind,
            Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(text.ConstraintExceptions.Single().ExceptionValue?
            .EffectiveValue?.TextValue, Is.EqualTo("bad"));
    }

    private static void CollectTypeKinds(
        ResolvedTypeSyntax type,
        System.Collections.Generic.HashSet<AsnConstraintKind> observed,
        System.Collections.Generic.List<ResolvedTypeSyntax> visited)
    {
        if (visited.Contains(type))
        {
            return;
        }

        visited.Add(type);
        if (type.EffectiveConstraint != null)
        {
            CollectGraphKinds(type.EffectiveConstraint, observed);
        }

        if (type.InnerType != null)
        {
            CollectTypeKinds(type.InnerType, observed, visited);
        }

        foreach (ResolvedComponentSyntax component in type.Components)
        {
            if (component.TypeResolution != null)
            {
                CollectTypeKinds(component.TypeResolution, observed, visited);
            }
        }

        foreach (ResolvedTypeSyntax constraintType in type.ConstraintTypes)
        {
            CollectTypeKinds(constraintType, observed, visited);
        }
    }

    private static void CollectGraphKinds(
        NormalizedConstraintGraph graph,
        System.Collections.Generic.HashSet<AsnConstraintKind> observed)
    {
        if (graph.RootPartition?.Expression != null)
        {
            CollectNodeKinds(graph.RootPartition.Expression, observed);
        }

        if (graph.ExtensionPartition != null)
        {
            observed.Add(AsnConstraintKind.Extension);
            if (graph.ExtensionPartition.Expression != null)
            {
                CollectNodeKinds(graph.ExtensionPartition.Expression, observed);
            }
        }

        if (graph.Exception != null)
        {
            CollectNodeKinds(graph.Exception, observed);
        }

        foreach (NormalizedConstraintGraph application in graph.Applications)
        {
            CollectGraphKinds(application, observed);
        }
    }

    private static void CollectNodeKinds(
        NormalizedConstraintNode node,
        System.Collections.Generic.HashSet<AsnConstraintKind> observed)
    {
        observed.Add(node.Kind);
        foreach (NormalizedConstraintNode child in node.Children)
        {
            CollectNodeKinds(child, observed);
        }

        foreach (NormalizedConstraintGraph nested in node.NestedConstraints)
        {
            CollectGraphKinds(nested, observed);
        }
    }

    private static void AssertIntegerValidity(
        ResolutionFixture fixture,
        string name,
        int value,
        bool expectedValid)
    {
        AsnDiagnostic[] diagnostics = Validate(fixture, name, IntegerValue(value));
        Assert.That(diagnostics.Length == 0, Is.EqualTo(expectedValid),
            name + ":" + value.ToString() + ":" + Format(diagnostics));
    }

    private static void AssertTextValidity(
        ResolutionFixture fixture,
        string name,
        string value,
        bool expectedValid)
    {
        AsnRuntimeValue runtime = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
        };
        AsnDiagnostic[] diagnostics = Validate(fixture, name, runtime);
        Assert.That(diagnostics.Length == 0, Is.EqualTo(expectedValid),
            name + ":" + value + ":" + Format(diagnostics));
    }

    private static void AssertFailure(
        ResolutionFixture fixture,
        string name,
        AsnRuntimeValue value,
        AsnConstraintKind expectedKind,
        string expectedPath)
    {
        AsnDiagnostic diagnostic = Validate(fixture, name, value).Single();
        Assert.That(diagnostic.ConstraintKind, Is.EqualTo(expectedKind));
        Assert.That(diagnostic.ValuePath, Is.EqualTo(expectedPath));
    }

    private static AsnRuntimeValue IntegerValue(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = new System.Numerics.BigInteger(value)
        };
    }

    private static AsnDiagnostic[] Validate(
        ResolutionFixture fixture,
        string name,
        AsnRuntimeValue value)
    {
        ResolvedTypeSyntax type = RequireType(fixture, name);
        IAsnValueValidator validator = new AsnRuntimeConstraintEvaluator();
        return validator.ValidateRuntimeValue(value, new AsnTypeDescriptor
        {
            Name = name,
            Kind = type.EffectiveKind,
            Resolution = type
        });
    }

    private static ResolvedTypeSyntax RequireType(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();
    }

    private static ResolutionFixture Resolve(string source)
    {
        ResolutionFixture fixture = ResolveAllowingFailure(source);
        Assert.That(fixture.Values.Success, Is.True, Format(fixture.Diagnostics));
        return fixture;
    }

    private static ResolutionFixture ResolveAllowingFailure(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        diagnostics.ApplyCompilerOptions(new AsnCompilerOptions { EnableLegacyAny = true });
        IncrementalLexSession session = new IncrementalLexSession(
            "constraint-coverage.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult parsed = new ModuleParser(new TokenCursor(session, 4))
            .ParseModule();
        Assert.That(parsed.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(
            [parsed.Module ?? throw new InvalidOperationException()]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics)
            .BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(
            diagnostics).ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True, Format(diagnostics));
        Assert.That(types.Success, Is.True, Format(diagnostics));
        ValueResolutionResult values = new ValueResolver(diagnostics).ResolveValues(tables);
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Tables = tables,
            Values = values
        };
    }

    private static string Format(DiagnosticBag diagnostics)
    {
        return Format(diagnostics.GetDiagnostics());
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(item => item.Code + ":"
            + item.Message));
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
    }
}
