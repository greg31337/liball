using asn1;

namespace asn1.Tests;

public class RuntimeConstraintEvaluatorTests
{
    [Test]
    public void ValidationBoundsDiagnosticFloodsAndValueRecursion()
    {
        AsnConstraintDescriptor onlyZero = new AsnConstraintDescriptor
        {
            Kind = AsnConstraintKind.ValueRange,
            RootIntervals =
            [
                new AsnIntervalDescriptor
                {
                    HasLowerBound = true,
                    LowerBound = System.Numerics.BigInteger.Zero,
                    HasUpperBound = true,
                    UpperBound = System.Numerics.BigInteger.Zero
                }
            ]
        };
        AsnTypeDescriptor integer = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Integer,
            Constraints = [onlyZero]
        };
        AsnTypeDescriptor collection = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.SequenceOf,
            ElementType = integer
        };
        AsnRuntimeValue values = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SequenceOf,
            Items =
            [
                IntegerValue(1),
                IntegerValue(2),
                IntegerValue(3),
                IntegerValue(4)
            ]
        };
        AsnDiagnostic[] diagnostics = new RuntimeConstraintEvaluator()
            .ValidateRuntimeValue(
                values,
                collection,
                new AsnResourceLimits { MaximumDiagnostics = 2 });

        Assert.That(diagnostics.Length, Is.EqualTo(2));
        Assert.That(diagnostics.Select(item => item.ValuePath),
            Is.EqualTo(new[] { "$[0]", "$[1]" }));

        AsnTypeDescriptor recursive = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.SequenceOf
        };
        recursive.ElementType = recursive;
        AsnRuntimeValue nested = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SequenceOf,
            Items =
            [
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.SequenceOf,
                    Items =
                    [
                        new AsnRuntimeValue
                        {
                            Kind = AsnRuntimeValueKind.SequenceOf
                        }
                    ]
                }
            ]
        };
        AsnDiagnostic depth = new RuntimeConstraintEvaluator()
            .ValidateRuntimeValue(
                nested,
                recursive,
                new AsnResourceLimits { MaximumNestingDepth = 2 })
            .Single();

        Assert.That(depth.Category, Is.EqualTo(AsnDiagnosticCategory.Resource));
        Assert.That(depth.Code, Is.EqualTo("ASNR0002"));
        Assert.That(depth.ValuePath, Is.EqualTo("$[0][0]"));
    }

    [Test]
    public void EvaluatorExecutesNormalizedSetOperationsAndExtensionPartitions()
    {
        ResolutionFixture fixture = Resolve(
            "Runtime DEFINITIONS ::= BEGIN "
                + "Base ::= INTEGER (1 | 2 | 3); "
                + "Combined ::= Base ((2 | 3) EXCEPT 3) (ALL EXCEPT 0); "
                + "Extended ::= INTEGER (1..3, ..., 10) END");

        Assert.That(ValidateInteger(fixture, "Combined", 2), Is.Empty);
        AsnDiagnostic excluded = ValidateInteger(fixture, "Combined", 3).Single();
        Assert.That(excluded.Code, Is.EqualTo("ASNC0031"));
        Assert.That(excluded.ConstraintKind, Is.EqualTo(AsnConstraintKind.Except));
        Assert.That(excluded.ConstraintId, Does.Contain("Combined:constraint@"));
        Assert.That(excluded.ValuePath, Is.EqualTo("$"));
        Assert.That(excluded.Span, Is.Not.Null);
        Assert.That(ValidateInteger(fixture, "Extended", 10), Is.Empty);
        Assert.That(ValidateInteger(fixture, "Extended", 50), Is.Empty);
    }

    [Test]
    public void EvaluatorChecksSizeAlphabetPatternAndUnicodeScalarLocations()
    {
        ResolutionFixture fixture = Resolve(
            "Runtime DEFINITIONS ::= BEGIN "
                + "Upper ::= UTF8String (SIZE (2..4)) (FROM (\"A\"..\"Z\")); "
                + "Code ::= UTF8String (PATTERN \"^[A-Z]+$\") END");

        Assert.That(ValidateText(fixture, "Upper", "AZ"), Is.Empty);
        AsnDiagnostic size = ValidateText(fixture, "Upper", "A").Single();
        Assert.That(size.ConstraintKind, Is.EqualTo(AsnConstraintKind.Size));
        Assert.That(size.ValuePath, Is.EqualTo("$"));
        AsnDiagnostic alphabet = ValidateText(fixture, "Upper", "A😀").Single();
        Assert.That(alphabet.ConstraintKind,
            Is.EqualTo(AsnConstraintKind.PermittedAlphabet));
        Assert.That(alphabet.ValuePath, Is.EqualTo("$[1]"));
        AsnDiagnostic pattern = ValidateText(fixture, "Code", "AB1").Single();
        Assert.That(pattern.ConstraintKind, Is.EqualTo(AsnConstraintKind.Pattern));
    }

    [Test]
    public void EvaluatorReportsNestedComponentAndCollectionElementPaths()
    {
        ResolutionFixture fixture = Resolve(
            "Runtime DEFINITIONS ::= BEGIN "
                + "Record ::= SEQUENCE { label UTF8String OPTIONAL, count INTEGER OPTIONAL } "
                + "(WITH COMPONENTS { ..., label (FROM (\"A\"..\"Z\")) PRESENT, "
                + "count ABSENT }); "
                + "Numbers ::= SEQUENCE (WITH COMPONENT (1..3)) OF INTEGER END");
        AsnRuntimeValue record = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                new AsnRuntimeFieldValue
                {
                    Name = "label",
                    Present = true,
                    Value = TextValue("Az")
                }
            ]
        };
        AsnDiagnostic component = Validate(fixture, "Record", record).First();
        Assert.That(component.ValuePath, Is.EqualTo("$.label[1]"));

        record.Fields = [];
        AsnDiagnostic presence = Validate(fixture, "Record", record).Single();
        Assert.That(presence.ValuePath, Is.EqualTo("$.label"));

        AsnRuntimeValue collection = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SequenceOf,
            Items = [IntegerValue(1), IntegerValue(5)]
        };
        AsnDiagnostic element = Validate(fixture, "Numbers", collection).First();
        Assert.That(element.ValuePath, Is.EqualTo("$[1]"));
    }

    [Test]
    public void EvaluatorChecksContainedOpenAndEncodedContentsValues()
    {
        ResolutionFixture fixture = Resolve(
            "Runtime DEFINITIONS ::= BEGIN "
                + "Payload ::= UTF8String (SIZE (2..4)); "
                + "OpenPayload ::= ANY (Payload); "
                + "EncodedPayload ::= OCTET STRING (CONTAINING Payload) END");
        AsnRuntimeValue decoded = TextValue("ABC");
        AsnOpenTypeValue open = new AsnOpenTypeValue
        {
            Resolved = true,
            ResolvedTypeId = "Runtime.Payload",
            TypeName = "Wrong.Payload",
            Value = decoded
        };

        Assert.That(Validate(fixture, "OpenPayload", new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = open
        }), Is.Empty);
        Assert.That(Validate(fixture, "EncodedPayload", new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            OpenTypeValue = open
        }), Is.Empty);

        open.ResolvedTypeId = "type:opaque-payload";
        open.TypeName = "Runtime.Payload";
        Assert.That(Validate(fixture, "OpenPayload", new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = open
        }), Is.Empty);

        open.Unresolved = true;
        Assert.That(Validate(fixture, "OpenPayload", new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = open
        }), Is.Not.Empty);
        open.Unresolved = false;

        decoded.TextValue = "X";
        AsnDiagnostic invalid = Validate(fixture, "OpenPayload", new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = open
        }).Single();
        Assert.That(invalid.ConstraintKind, Is.EqualTo(AsnConstraintKind.Size));
        Assert.That(invalid.ValuePath, Is.EqualTo("$.value"));
    }

    [Test]
    public void EvaluatorChecksTimeSettingsAndReportsOpaqueRequirements()
    {
        ResolutionFixture fixture = Resolve(
            "Runtime DEFINITIONS ::= BEGIN "
                + "Clock ::= TIME (SETTINGS \"Basic=Time Time=HMS Local-or-UTC=Z\"); "
                + "Opaque ::= INTEGER (CONSTRAINED BY {}) END");
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
        clock.TimeValue.Text = "12:34:56";
        clock.TextValue = clock.TimeValue.Text;
        AsnDiagnostic settings = Validate(fixture, "Clock", clock).Single();
        Assert.That(settings.ConstraintKind,
            Is.EqualTo(AsnConstraintKind.PropertySettings));
        AsnDiagnostic opaque = ValidateInteger(fixture, "Opaque", 7).Single();
        Assert.That(opaque.Code, Is.EqualTo("ASNC0032"));
        Assert.That(opaque.ConstraintKind, Is.EqualTo(AsnConstraintKind.UserDefined));
    }

    [Test]
    public void EvaluatorChecksSingleValuesRealRangesAndTableDispatch()
    {
        ResolutionFixture fixture = Resolve(
            "Runtime DEFINITIONS ::= BEGIN "
                + "Flag ::= BOOLEAN (TRUE); "
                + "Ratio ::= REAL (0.5..1.5) END");
        AsnRuntimeValue flag = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Boolean,
            HasBooleanValue = true,
            BooleanValue = true
        };
        Assert.That(Validate(fixture, "Flag", flag), Is.Empty);
        flag.BooleanValue = false;
        Assert.That(Validate(fixture, "Flag", flag).Single().ConstraintKind,
            Is.EqualTo(AsnConstraintKind.SingleValue));

        AsnRuntimeValue ratio = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Real,
            RealValue = DecimalReal(10, -1)
        };
        AsnDiagnostic[] ratioDiagnostics = Validate(fixture, "Ratio", ratio);
        Assert.That(ratioDiagnostics, Is.Empty, string.Join(" | ", ratioDiagnostics.Select(
            item => item.ConstraintKind + ":" + item.Message)));
        ratio.RealValue = DecimalReal(20, -1);
        Assert.That(Validate(fixture, "Ratio", ratio).Single().ConstraintKind,
            Is.EqualTo(AsnConstraintKind.ValueRange));

        NormalizedConstraintNode tableNode = new NormalizedConstraintNode
        {
            Kind = AsnConstraintKind.SimpleTable,
            CompiledTableConstraint = new TableConstraintRelationship
            {
                StableId = "Runtime.Table.key",
                DispatchEntries =
                [
                    new AsnOpenTypeDispatchEntry
                    {
                        Key = new AsnValue
                        {
                            Kind = AsnValueKind.Integer,
                            HasIntegerValue = true,
                            IntegerValue = new System.Numerics.BigInteger(7)
                        },
                        Type = new AsnType { Name = "Payload" }
                    }
                ]
            }
        };
        AsnRuntimeValue tableValue = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = new AsnOpenTypeValue
            {
                Resolved = true,
                ResolvedTypeId = "Runtime.Payload",
                DispatchKey = IntegerValue(7)
            }
        };
        RuntimeConstraintEvaluationResult tableResult = new RuntimeConstraintEvaluator()
            .EvaluateNode(tableNode, tableValue, RequireType(fixture, "Flag"), "$");
        Assert.That(tableResult.Success, Is.True);
        tableValue.OpenTypeValue.DispatchKey = IntegerValue(8);
        Assert.That(new RuntimeConstraintEvaluator().EvaluateNode(
            tableNode,
            tableValue,
            RequireType(fixture, "Flag"),
            "$").Success, Is.False);
    }

    [Test]
    public void EvaluatorChecksRealAndDurationPseudoComponents()
    {
        ResolutionFixture fixture = Resolve(
            "PseudoComponents DEFINITIONS ::= BEGIN "
                + "Decimal ::= REAL (0 | WITH COMPONENTS {..., base (10)}); "
                + "Binary ::= REAL (WITH COMPONENTS {mantissa (1..10), "
                + "base (2), exponent (-4..4)}); "
                + "WholeDuration ::= DURATION (WITH COMPONENTS {..., "
                + "seconds ABSENT, fractional-part ABSENT}); "
                + "SecondsDuration ::= DURATION (WITH COMPONENTS {..., "
                + "seconds PRESENT}) END");
        AsnRuntimeValue decimalValue = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Real,
            RealValue = DecimalReal(12, -1)
        };
        AsnRuntimeValue binaryValue = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Real,
            RealValue = new AsnRealValue
            {
                Normalized = true,
                Kind = AsnRealKind.FiniteBinary,
                HasBinaryValue = true,
                Mantissa = new System.Numerics.BigInteger(5),
                Radix = 2,
                Exponent = new System.Numerics.BigInteger(-2)
            }
        };

        Assert.That(Validate(fixture, "Decimal", decimalValue), Is.Empty);
        Assert.That(Validate(fixture, "Binary", binaryValue), Is.Empty);
        binaryValue.RealValue.Radix = 8;
        AsnDiagnostic radix = Validate(fixture, "Binary", binaryValue).Single();
        Assert.That(radix.ValuePath, Is.EqualTo("$.base"));

        AsnRuntimeValue duration = DurationValue("P1D");
        Assert.That(Validate(fixture, "WholeDuration", duration), Is.Empty);
        duration = DurationValue("PT1.5S");
        Assert.That(Validate(fixture, "WholeDuration", duration)
            .Select(value => value.ValuePath),
            Does.Contain("$.seconds"));
        Assert.That(Validate(
            fixture,
            "SecondsDuration",
            DurationValue("PT0S")), Is.Empty);
        Assert.That(Validate(
            fixture,
            "SecondsDuration",
            DurationValue("P1D")).Single().ValuePath,
            Is.EqualTo("$.seconds"));
    }

    private static AsnRealValue DecimalReal(int coefficient, int exponent)
    {
        return new AsnRealValue
        {
            Normalized = true,
            Kind = AsnRealKind.FiniteDecimal,
            HasDecimalValue = true,
            DecimalText = coefficient.ToString() + "e" + exponent.ToString(),
            DecimalCoefficient = new System.Numerics.BigInteger(coefficient),
            DecimalExponent = new System.Numerics.BigInteger(exponent)
        };
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

    private static AsnRuntimeValue DurationValue(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Time,
            TextValue = value,
            TimeValue = new AsnTimeValue
            {
                Kind = AsnTimeKind.Duration,
                Text = value
            }
        };
    }

    private static AsnRuntimeValue TextValue(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
        };
    }

    private static AsnDiagnostic[] ValidateInteger(
        ResolutionFixture fixture,
        string name,
        int value)
    {
        return Validate(fixture, name, IntegerValue(value));
    }

    private static AsnDiagnostic[] ValidateText(
        ResolutionFixture fixture,
        string name,
        string value)
    {
        return Validate(fixture, name, TextValue(value));
    }

    private static AsnDiagnostic[] Validate(
        ResolutionFixture fixture,
        string name,
        AsnRuntimeValue value)
    {
        ResolvedTypeSyntax resolution = RequireType(fixture, name);
        return new AsnRuntimeConstraintEvaluator().ValidateRuntimeValue(
            value,
            new AsnTypeDescriptor
            {
                Name = name,
                Kind = resolution.EffectiveKind,
                Resolution = resolution
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
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        diagnostics.ApplyCompilerOptions(new AsnCompilerOptions { EnableLegacyAny = true });
        IncrementalLexSession session = new IncrementalLexSession(
            "runtime-constraints.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult parsed = new ModuleParser(new TokenCursor(session, 4)).ParseModule();
        Assert.That(parsed.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(
            [parsed.Module ?? throw new InvalidOperationException()]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True);
        Assert.That(types.Success, Is.True);
        ValueResolutionResult values = new ValueResolver(diagnostics).ResolveValues(tables);
        Assert.That(values.Success, Is.True, string.Join(" | ", diagnostics.GetDiagnostics()
            .Select(item => item.Code + ":" + item.Message)));
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Tables = tables
        };
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
    }
}
