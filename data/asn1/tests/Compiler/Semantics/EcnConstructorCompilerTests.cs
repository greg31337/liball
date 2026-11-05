using asn1;

namespace asn1.Tests;

public class EcnConstructorCompilerTests
{
    [Test]
    public void CompilesEveryConstructorAndConditionalForm()
    {
        EcnFixture fixture = Compile(
            "Constructors ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Packet ::= #CONCATENATION { length #INT, payload #OCTETS OPTIONAL-ENCODING #OPTIONAL }\n"
                + "#Choice ::= #ALTERNATIVES { small #INT, large #OCTETS }\n"
                + "#Items ::= #REPETITION { item #INT }\n"
                + "#Tagged ::= [ #TAG (3) ] #INT\n"
                + "packet #Packet ::= { }\n"
                + "choice #Choice ::= { }\n"
                + "items #Items ::= { }\n"
                + "tagged #Tagged ::= { }\n"
                + "alternative {< REFERENCE : selector >} #ALTERNATIVES ::= {\n"
                + "  ALTERNATIVE DETERMINED BY field-to-be-used USING selector }\n"
                + "optional {< REFERENCE : present >} #OPTIONAL ::= {\n"
                + "  PRESENCE DETERMINED BY field-to-be-set USING present }\n"
                + "repetition {< REFERENCE : count >} #REPETITION ::= {\n"
                + "  REPETITION-SPACE DETERMINED BY flag-to-be-used USING count }\n"
                + "space {< REFERENCE : length >} #INT ::= {\n"
                + "  ENCODING-SPACE DETERMINED BY field-to-be-used USING length }\n"
                + "tag #TAG ::= { SIZE 8 }\n"
                + "conditionalInt {< REFERENCE : enabled >} #CONDITIONAL-INT ::= { IF enabled }\n"
                + "conditionalItems {< REFERENCE : enabled >} #CONDITIONAL-REPETITION ::= { IF-ALL { enabled } }\n"
                + "END");

        EcnConstructorDefinition packet = Constructor(fixture, "packet");
        EcnConstructorDefinition alternative = Constructor(
            fixture,
            "alternative");
        EcnConstructorDefinition optional = Constructor(fixture, "optional");
        EcnConstructorDefinition repetition = Constructor(
            fixture,
            "repetition");

        Assert.Multiple(() =>
        {
            Assert.That(packet.Kind,
                Is.EqualTo(EcnConstructorKind.Concatenation));
            Assert.That(packet.Fields.Select(field => field.Name),
                Is.EqualTo(new[] { "length", "payload" }));
            Assert.That(Constructor(fixture, "choice").Kind,
                Is.EqualTo(EcnConstructorKind.Alternatives));
            Assert.That(Constructor(fixture, "items").Kind,
                Is.EqualTo(EcnConstructorKind.Repetition));
            Assert.That(Constructor(fixture, "tagged").Kind,
                Is.EqualTo(EcnConstructorKind.Tag));
            Assert.That(Constructor(fixture, "tag").Kind,
                Is.EqualTo(EcnConstructorKind.Tag));
            Assert.That(alternative.ControlDependencies.Single().Mode,
                Is.EqualTo(EcnDeterminationMode.FieldToBeUsed));
            Assert.That(alternative.ControlDependencies.Single()
                .RequiresPriorValue, Is.True);
            Assert.That(optional.Kind,
                Is.EqualTo(EcnConstructorKind.Optionality));
            Assert.That(optional.ControlDependencies.Single().EncoderWrites,
                Is.True);
            Assert.That(repetition.ControlDependencies.Single().Mode,
                Is.EqualTo(EcnDeterminationMode.FlagToBeUsed));
            Assert.That(Constructor(fixture, "space").ControlDependencies
                .Single().Kind,
                Is.EqualTo(EcnControlDependencyKind.EncodingSpace));
            Assert.That(Constructor(fixture, "conditionalInt").Kind,
                Is.EqualTo(EcnConstructorKind.ConditionalInteger));
            Assert.That(Constructor(fixture, "conditionalItems").Kind,
                Is.EqualTo(EcnConstructorKind.ConditionalRepetition));
            Assert.That(Constructor(fixture, "conditionalItems").Conditions
                .Single().All, Is.True);
            Assert.That(Constructor(fixture, "conditionalItems").Conditions
                .Single().Reference, Is.EqualTo("enabled"));
            Assert.That(Constructor(fixture, "conditionalInt").Conditions
                .Single().Reference, Is.EqualTo("enabled"));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void RetainsDeferredPathsAndInheritsConstructorSemantics()
    {
        EcnFixture fixture = Compile(
            "Inheritance ENCODING-DEFINITIONS ::= BEGIN\n"
                + "base #OPTIONAL ::= {\n"
                + "  PRESENCE DETERMINED BY field-to-be-used USING Root.enabled }\n"
                + "derived #OPTIONAL ::= base\n"
                + "handled #OPTIONAL ::= {\n"
                + "  PRESENCE DETERMINED BY handle HANDLE decision }\n"
                + "pointed #OPTIONAL ::= {\n"
                + "  PRESENCE DETERMINED BY pointer START-POINTER origin }\n"
                + "END");

        EcnConstructorDefinition baseConstructor = Constructor(
            fixture,
            "base");
        EcnConstructorDefinition derived = Constructor(fixture, "derived");

        Assert.Multiple(() =>
        {
            Assert.That(baseConstructor.ControlDependencies.Single()
                .Reference, Is.EqualTo("Root.enabled"));
            Assert.That(derived.Kind,
                Is.EqualTo(EcnConstructorKind.Optionality));
            Assert.That(derived.ControlDependencies.Single().Reference,
                Is.EqualTo("Root.enabled"));
            Assert.That(derived.ControlDependencies,
                Is.Not.SameAs(baseConstructor.ControlDependencies));
            Assert.That(Constructor(fixture, "handled").ControlDependencies
                .Single().AuxiliaryReference, Is.EqualTo("decision"));
            Assert.That(Constructor(fixture, "pointed").ControlDependencies
                .Single().AuxiliaryReference, Is.EqualTo("origin"));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void DiagnosesMalformedDuplicateAndCyclicControls()
    {
        EcnFixture fixture = Compile(
            "Invalid ENCODING-DEFINITIONS ::= BEGIN\n"
                + "missing #OPTIONAL ::= {\n"
                + "  PRESENCE DETERMINED BY field-to-be-set }\n"
                + "forbidden #OPTIONAL ::= {\n"
                + "  PRESENCE DETERMINED BY container USING Root.value }\n"
                + "wrongMode #ALTERNATIVES ::= {\n"
                + "  ALTERNATIVE DETERMINED BY container }\n"
                + "duplicate #OPTIONAL ::= {\n"
                + "  PRESENCE DETERMINED BY field-to-be-used USING Root.one,\n"
                + "  PRESENCE DETERMINED BY field-to-be-used USING Root.two }\n"
                + "cycle {< REFERENCE : selector >} #ALTERNATIVES ::= {\n"
                + "  ALTERNATIVE DETERMINED BY field-to-be-set USING selector IF selector }\n"
                + "missingCondition #CONDITIONAL-INT ::= { }\n"
                + "brokenCondition #CONDITIONAL-REPETITION ::= { IF }\n"
                + "END");

        string[] codes = fixture.Diagnostics.GetDiagnostics()
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(codes, Does.Contain("ASNECN0029"));
            Assert.That(codes, Does.Contain("ASNECN0030"));
            Assert.That(codes, Does.Contain("ASNECN0031"));
            Assert.That(codes, Does.Contain("ASNECN0033"));
            Assert.That(Constructor(fixture, "missing").Valid, Is.False);
            Assert.That(Constructor(fixture, "forbidden").Valid, Is.False);
            Assert.That(Constructor(fixture, "wrongMode").Valid, Is.False);
            Assert.That(Constructor(fixture, "duplicate").Valid, Is.False);
            Assert.That(Constructor(fixture, "cycle").Valid, Is.False);
            Assert.That(Constructor(fixture, "missingCondition").Valid,
                Is.False);
            Assert.That(Constructor(fixture, "brokenCondition").Valid,
                Is.False);
        });
    }

    [Test]
    public void RejectsConflictingForwardAndConditionDependencies()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(
            false,
            new AsnResourceLimits());
        EcnConstructorCompiler compiler = new EcnConstructorCompiler(
            diagnostics);
        EcnControlDependency reader = new EcnControlDependency
        {
            Kind = EcnControlDependencyKind.Presence,
            Mode = EcnDeterminationMode.FieldToBeUsed,
            Reference = "length",
            RequiresPriorValue = true,
            Valid = true
        };
        EcnControlDependency writer = new EcnControlDependency
        {
            Kind = EcnControlDependencyKind.EncodingSpace,
            Mode = EcnDeterminationMode.FieldToBeSet,
            Reference = "length",
            EncoderWrites = true,
            Valid = true
        };
        EcnControlDependency secondWriter = new EcnControlDependency
        {
            Kind = EcnControlDependencyKind.Alternative,
            Mode = EcnDeterminationMode.FlagToBeSet,
            Reference = "length",
            EncoderWrites = true,
            Valid = true
        };
        EcnConditionDefinition condition = new EcnConditionDefinition
        {
            Name = "IF",
            Expression = "length",
            Reference = "length",
            Valid = true
        };

        bool valid = compiler.ValidateControlSet(
            [reader, writer, secondWriter],
            [condition]);
        string[] codes = diagnostics.GetDiagnostics()
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(valid, Is.False);
            Assert.That(codes, Does.Contain("ASNECN0030"));
            Assert.That(codes, Does.Contain("ASNECN0032"));
            Assert.That(codes, Does.Contain("ASNECN0033"));
            Assert.That(writer.Valid, Is.False);
            Assert.That(secondWriter.Valid, Is.False);
            Assert.That(condition.Valid, Is.False);
        });
    }

    [Test]
    public void ClassifiesImportedAsnStructuresAndRetainsComponentOrder()
    {
        AsnType importedType = new AsnType
        {
            Kind = AsnTypeKind.Sequence,
            Components =
            [
                new AsnComponent { Name = "header" },
                new AsnComponent { Name = "payload", Optional = true }
            ]
        };
        EcnSymbol governor = new EcnSymbol
        {
            ImportedAsnType = true,
            AsnTypeAssignment = new AsnAssignment
            {
                Kind = AsnAssignmentKind.Type,
                Type = importedType
            }
        };
        EcnSymbol owner = new EcnSymbol
        {
            GovernorSymbol = governor
        };
        EcnConstructorCompiler compiler = new EcnConstructorCompiler(
            new DiagnosticBag(false, new AsnResourceLimits()));

        EcnConstructorDefinition sequence = compiler.CreateConstructor(
            new EcnEncodingObjectDefinition
            {
                Symbol = owner,
                Valid = true
            });
        importedType.Kind = AsnTypeKind.Choice;
        EcnConstructorKind choice = compiler.ResolveKind(owner, null);
        importedType.Kind = AsnTypeKind.SequenceOf;
        EcnConstructorKind repetition = compiler.ResolveKind(owner, null);
        importedType.Kind = AsnTypeKind.Tagged;
        EcnConstructorKind tag = compiler.ResolveKind(owner, null);

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Kind,
                Is.EqualTo(EcnConstructorKind.Concatenation));
            Assert.That(sequence.Fields.Select(field => field.Name),
                Is.EqualTo(new[] { "header", "payload" }));
            Assert.That(sequence.Fields[1].Optional, Is.True);
            Assert.That(choice, Is.EqualTo(EcnConstructorKind.Alternatives));
            Assert.That(repetition, Is.EqualTo(EcnConstructorKind.Repetition));
            Assert.That(tag, Is.EqualTo(EcnConstructorKind.Tag));
        });
    }

    private static EcnConstructorDefinition Constructor(
        EcnFixture fixture,
        string name)
    {
        return fixture.Table.FindSymbol(name, EcnSymbolKind.EncodingObject)
                ?.ObjectDefinition?.Constructor
            ?? throw new AssertionException(
                "The constructor definition for '" + name + "' is missing.");
    }

    private static EcnFixture Compile(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "constructors.edm",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        System.Collections.Generic.List<AsnToken> tokens =
            new System.Collections.Generic.List<AsnToken>();
        while (true)
        {
            IncrementalLexResult result = session.ScanNext();
            if (result.Status == IncrementalLexStatus.EndOfInput)
            {
                break;
            }

            if (result.Status == IncrementalLexStatus.NeedMoreData)
            {
                throw new AssertionException(
                    "Final ECN input requested more data.");
            }

            if (result.Token != null)
            {
                tokens.Add(result.Token);
            }
        }

        EcnParseResult parsed = new EcnSyntaxParser(tokens.ToArray())
            .ParseModuleSyntax();
        Assert.That(parsed.Success, Is.True, parsed.ErrorMessage);
        Assert.That(session.GetDiagnostics(), Is.Empty);
        EcnModuleSyntax module = parsed.Module
            ?? throw new AssertionException("No ECN module was parsed.");
        EcnSymbolTable table = new EcnSymbolTableBuilder(session.Diagnostics)
            .BuildTable(module, []);
        return new EcnFixture
        {
            Diagnostics = session.Diagnostics,
            Table = table
        };
    }

    private class EcnFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal EcnSymbolTable Table = null!;
    }
}
