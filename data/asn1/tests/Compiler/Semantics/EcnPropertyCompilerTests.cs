using asn1;

namespace asn1.Tests;

public class EcnPropertyCompilerTests
{
    [Test]
    public void ParsesPrimitivePropertiesAndTypedValues()
    {
        EcnFixture fixture = Compile(
            "Properties ENCODING-DEFINITIONS ::= BEGIN\n"
                + "integerEncoding #INT ::= {\n"
                + "  REPRESENTATION twos-complement,\n"
                + "  RANGE -128..127,\n"
                + "  SIGNEDNESS signed,\n"
                + "  ENCODING-SPACE,\n"
                + "  SIZE 16,\n"
                + "  MULTIPLE OF octet,\n"
                + "  BYTE-ORDER big-endian,\n"
                + "  BIT-ORDER most-significant-first,\n"
                + "  ALIGNED TO NEXT octet,\n"
                + "  PADDING zero,\n"
                + "  BIT-REVERSAL reverse-bits-in-units\n"
                + "}\n"
                + "textEncoding #CHARS ::= {\n"
                + "  ALPHABET \"ABC\", TERMINATION null-terminated\n"
                + "}\n"
                + "END");

        EcnEncodingObjectDefinition integer = ObjectDefinition(
            fixture,
            "integerEncoding");
        EcnEncodingObjectDefinition text = ObjectDefinition(
            fixture,
            "textEncoding");
        EcnEncodingProperty range = Property(
            integer,
            EcnEncodingPropertyKind.Range);
        EcnEncodingProperty size = Property(
            integer,
            EcnEncodingPropertyKind.Size);
        EcnEncodingProperty multiple = Property(
            integer,
            EcnEncodingPropertyKind.Multiple);

        Assert.Multiple(() =>
        {
            Assert.That(integer.Properties.Select(property => property.Kind),
                Is.EqualTo(new[]
                {
                    EcnEncodingPropertyKind.Representation,
                    EcnEncodingPropertyKind.Range,
                    EcnEncodingPropertyKind.Signedness,
                    EcnEncodingPropertyKind.EncodingSpace,
                    EcnEncodingPropertyKind.Size,
                    EcnEncodingPropertyKind.Multiple,
                    EcnEncodingPropertyKind.ByteOrder,
                    EcnEncodingPropertyKind.BitOrder,
                    EcnEncodingPropertyKind.Alignment,
                    EcnEncodingPropertyKind.Padding,
                    EcnEncodingPropertyKind.BitReversal
                }));
            Assert.That(range.ValueKind,
                Is.EqualTo(EcnEncodingPropertyValueKind.Range));
            Assert.That(range.LowerBound, Is.EqualTo("-128"));
            Assert.That(range.UpperBound, Is.EqualTo("127"));
            Assert.That(size.Scope,
                Is.EqualTo(EcnEncodingPropertyScope.EncodingSpace));
            Assert.That(size.HasIntegerValue, Is.True);
            Assert.That(size.IntegerValue, Is.EqualTo(16));
            Assert.That(multiple.Unit, Is.EqualTo("octet"));
            Assert.That(integer.Properties.All(property => property.Valid),
                Is.True);
            Assert.That(Property(text, EcnEncodingPropertyKind.Alphabet)
                .ValueKind, Is.EqualTo(EcnEncodingPropertyValueKind.String));
            Assert.That(Property(text, EcnEncodingPropertyKind.Termination)
                .Value, Is.EqualTo("null-terminated"));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void ResolvesPropertyGroupScopesIndependently()
    {
        EcnFixture fixture = Compile(
            "Scopes ENCODING-DEFINITIONS ::= BEGIN\n"
                + "bits #BITS ::= {\n"
                + "  ENCODING-SPACE, SIZE 8,\n"
                + "  REPETITION-SPACE, SIZE self-delimiting-values,\n"
                + "  MULTIPLE OF repetitions, PATTERN '0'B,\n"
                + "  VALUE-PADDING, JUSTIFIED left, UNUSED BITS 3\n"
                + "}\n"
                + "END");

        EcnEncodingObjectDefinition bits = ObjectDefinition(fixture, "bits");
        EcnEncodingProperty[] sizes = bits.Properties
            .Where(property => property.Kind == EcnEncodingPropertyKind.Size)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(sizes, Has.Length.EqualTo(2));
            Assert.That(sizes[0].Scope,
                Is.EqualTo(EcnEncodingPropertyScope.EncodingSpace));
            Assert.That(sizes[1].Scope,
                Is.EqualTo(EcnEncodingPropertyScope.RepetitionSpace));
            Assert.That(Property(bits, EcnEncodingPropertyKind.Multiple).Unit,
                Is.EqualTo("repetitions"));
            Assert.That(Property(bits, EcnEncodingPropertyKind.Pattern)
                .ValueKind, Is.EqualTo(EcnEncodingPropertyValueKind.BitString));
            Assert.That(Property(bits, EcnEncodingPropertyKind.UnusedBits)
                .Scope, Is.EqualTo(EcnEncodingPropertyScope.ValuePadding));
            Assert.That(Property(bits, EcnEncodingPropertyKind.UnusedBits)
                .IntegerValue, Is.EqualTo(3));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void InheritsResolvedPropertiesFromReferencedObjects()
    {
        EcnFixture fixture = Compile(
            "Inheritance ENCODING-DEFINITIONS ::= BEGIN\n"
                + "baseEncoding #INT ::= { SIZE 8, SIGNEDNESS unsigned }\n"
                + "derivedEncoding #INT ::= baseEncoding\n"
                + "END");

        EcnEncodingObjectDefinition inherited = ObjectDefinition(
            fixture,
            "derivedEncoding");

        Assert.Multiple(() =>
        {
            Assert.That(inherited.Properties.Select(property => property.Name),
                Is.EqualTo(new[] { "SIZE", "SIGNEDNESS" }));
            Assert.That(Property(inherited, EcnEncodingPropertyKind.Size)
                .IntegerValue, Is.EqualTo(8));
            Assert.That(Property(inherited, EcnEncodingPropertyKind.Signedness)
                .Value, Is.EqualTo("unsigned"));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void DiagnosesInvalidDuplicateAndIncompatibleProperties()
    {
        EcnFixture fixture = Compile(
            "Invalid ENCODING-DEFINITIONS ::= BEGIN\n"
                + "invalidRange #INT ::= { RANGE 10..1 }\n"
                + "invalidPadding #INT ::= { PADDING unknown }\n"
                + "invalidSize #INT ::= { SIZE -9 }\n"
                + "duplicateSize #INT ::= { SIZE 8, SIZE 16 }\n"
                + "wrongAlphabet #INT ::= { ALPHABET \"ABC\" }\n"
                + "wrongSignedness #BOOL ::= { SIGNEDNESS signed }\n"
                + "END");

        string[] codes = fixture.Diagnostics.GetDiagnostics()
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(codes, Does.Contain("ASNECN0025"));
            Assert.That(codes, Does.Contain("ASNECN0026"));
            Assert.That(codes, Does.Contain("ASNECN0027"));
            Assert.That(ObjectDefinition(fixture, "invalidRange").Valid,
                Is.False);
            Assert.That(ObjectDefinition(fixture, "duplicateSize").Valid,
                Is.False);
            Assert.That(ObjectDefinition(fixture, "wrongAlphabet").Valid,
                Is.False);
        });
    }

    [Test]
    public void DiagnosesCyclicPropertyReferences()
    {
        EcnFixture fixture = Compile(
            "Cycle ENCODING-DEFINITIONS ::= BEGIN\n"
                + "first #INT ::= second\n"
                + "second #INT ::= first\n"
                + "END");

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Diagnostics.GetDiagnostics()
                .Select(diagnostic => diagnostic.Code),
                Does.Contain("ASNECN0028"));
            Assert.That(ObjectDefinition(fixture, "first").Valid, Is.False);
            Assert.That(ObjectDefinition(fixture, "second").Valid, Is.False);
        });
    }

    [Test]
    public void RecognizesEveryPropertyAndPropertyGroupProduction()
    {
        EcnPropertyCompiler compiler = new EcnPropertyCompiler(
            new DiagnosticBag(false, new AsnResourceLimits()));
        string[] names =
        [
            "REPRESENTATION",
            "SIZE",
            "RANGE",
            "ALPHABET",
            "ALIGNMENT",
            "PADDING",
            "BYTE-ORDER",
            "BIT-ORDER",
            "SIGNEDNESS",
            "TERMINATION",
            "ENCODING-SPACE",
            "REPETITION-SPACE",
            "REPETITION-ENCODING",
            "OPTIONAL-ENCODING",
            "ALTERNATIVE",
            "PRESENCE",
            "MULTIPLE",
            "PATTERN",
            "JUSTIFIED",
            "BIT-REVERSAL",
            "VALUE-PADDING",
            "VALUE-REVERSAL",
            "TRUE-PATTERN",
            "FALSE-PATTERN",
            "UNUSED-BITS",
            "WORD-ORDER"
        ];
        EcnEncodingPropertyKind[] expected =
        [
            EcnEncodingPropertyKind.Representation,
            EcnEncodingPropertyKind.Size,
            EcnEncodingPropertyKind.Range,
            EcnEncodingPropertyKind.Alphabet,
            EcnEncodingPropertyKind.Alignment,
            EcnEncodingPropertyKind.Padding,
            EcnEncodingPropertyKind.ByteOrder,
            EcnEncodingPropertyKind.BitOrder,
            EcnEncodingPropertyKind.Signedness,
            EcnEncodingPropertyKind.Termination,
            EcnEncodingPropertyKind.EncodingSpace,
            EcnEncodingPropertyKind.RepetitionSpace,
            EcnEncodingPropertyKind.RepetitionEncoding,
            EcnEncodingPropertyKind.OptionalEncoding,
            EcnEncodingPropertyKind.Alternative,
            EcnEncodingPropertyKind.Presence,
            EcnEncodingPropertyKind.Multiple,
            EcnEncodingPropertyKind.Pattern,
            EcnEncodingPropertyKind.Justification,
            EcnEncodingPropertyKind.BitReversal,
            EcnEncodingPropertyKind.ValuePadding,
            EcnEncodingPropertyKind.ValueReversal,
            EcnEncodingPropertyKind.TruePattern,
            EcnEncodingPropertyKind.FalsePattern,
            EcnEncodingPropertyKind.UnusedBits,
            EcnEncodingPropertyKind.WordOrder
        ];
        EcnEncodingPropertyKind[] actual = names.Select(
            compiler.GetPropertyKind).ToArray();
        EcnEncodingPropertyKind[] defined = System.Enum.GetValues<
                EcnEncodingPropertyKind>()
            .Where(item => item != EcnEncodingPropertyKind.Unknown)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Distinct(), Is.EqualTo(defined));
            Assert.That(compiler.GetPropertyKind("ALIGNED"),
                Is.EqualTo(EcnEncodingPropertyKind.Alignment));
            Assert.That(compiler.GetPropertyKind("PRE-PADDING"),
                Is.EqualTo(EcnEncodingPropertyKind.Padding));
            Assert.That(compiler.GetPropertyKind("POST-PADDING"),
                Is.EqualTo(EcnEncodingPropertyKind.Padding));
            Assert.That(compiler.GetPropertyKind("PAD-PATTERN"),
                Is.EqualTo(EcnEncodingPropertyKind.Pattern));
            Assert.That(compiler.GetPropertyKind("UNUSED"),
                Is.EqualTo(EcnEncodingPropertyKind.UnusedBits));
            Assert.That(compiler.GetPropertyKind("not-a-property"),
                Is.EqualTo(EcnEncodingPropertyKind.Unknown));
            Assert.That(expected.All(PropertyHasApplicableCategory), Is.True);
        });
    }

    [Test]
    public void EveryPropertyGroupAdvancesToItsPrescribedScope()
    {
        EcnPropertyCompiler compiler = new EcnPropertyCompiler(
            new DiagnosticBag(false, new AsnResourceLimits()));
        EcnEncodingPropertyKind[] groups =
        [
            EcnEncodingPropertyKind.EncodingSpace,
            EcnEncodingPropertyKind.RepetitionSpace,
            EcnEncodingPropertyKind.RepetitionEncoding,
            EcnEncodingPropertyKind.OptionalEncoding,
            EcnEncodingPropertyKind.Alternative,
            EcnEncodingPropertyKind.Presence,
            EcnEncodingPropertyKind.ValuePadding
        ];
        EcnEncodingPropertyScope[] scopes =
        [
            EcnEncodingPropertyScope.EncodingSpace,
            EcnEncodingPropertyScope.RepetitionSpace,
            EcnEncodingPropertyScope.RepetitionEncoding,
            EcnEncodingPropertyScope.OptionalEncoding,
            EcnEncodingPropertyScope.Alternative,
            EcnEncodingPropertyScope.Presence,
            EcnEncodingPropertyScope.ValuePadding
        ];
        int index = 0;
        while (index < groups.Length)
        {
            Assert.Multiple(() =>
            {
                Assert.That(compiler.IsGroupProperty(groups[index]),
                    Is.True,
                    groups[index].ToString());
                Assert.That(compiler.GetPropertyScope(
                        groups[index],
                        EcnEncodingPropertyScope.General),
                    Is.EqualTo(EcnEncodingPropertyScope.General),
                    groups[index].ToString());
                Assert.That(compiler.AdvanceScope(
                        groups[index],
                        EcnEncodingPropertyScope.General),
                    Is.EqualTo(scopes[index]),
                    groups[index].ToString());
            });
            index = index + 1;
        }
    }

    private static bool PropertyHasApplicableCategory(
        EcnEncodingPropertyKind kind)
    {
        EcnClassCategory[] categories =
            System.Enum.GetValues<EcnClassCategory>();
        int index = 0;
        while (index < categories.Length)
        {
            if (categories[index] != EcnClassCategory.Unknown
                && EcnPropertyApplicability.IsAllowed(
                    kind,
                    categories[index]))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    private static EcnEncodingProperty Property(
        EcnEncodingObjectDefinition definition,
        EcnEncodingPropertyKind kind)
    {
        return definition.Properties.Single(property => property.Kind == kind);
    }

    private static EcnEncodingObjectDefinition ObjectDefinition(
        EcnFixture fixture,
        string name)
    {
        return fixture.Table.FindSymbol(name, EcnSymbolKind.EncodingObject)
                ?.ObjectDefinition
            ?? throw new AssertionException(
                "The encoding object definition '" + name + "' is missing.");
    }

    private static EcnFixture Compile(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "properties.edm",
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
