using asn1;

namespace asn1.Tests;

public class EcnConversionCompilerTests
{
    [Test]
    public void ResolvesReplacementAndContainedValueMapping()
    {
        EcnFixture fixture = Compile(
            "Contained ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Wrapped {< #Element >} ::= #CONCATENATION { length #INT, value #Element }\n"
                + "wrappedEncoding {< #Element >} #Wrapped {< #Element >} ::= { }\n"
                + "octets #OCTETS ::= { REPETITION-ENCODING {\n"
                + "  REPLACE STRUCTURE WITH #Wrapped ENCODED BY wrappedEncoding } }\n"
                + "copy #OCTETS ::= octets\n"
                + "END");

        EcnEncodingObjectDefinition octets = ObjectDefinition(fixture, "octets");
        EcnEncodingObjectDefinition copy = ObjectDefinition(fixture, "copy");
        EcnConversionDefinition conversion = octets.Conversion
            ?? throw new AssertionException("The conversion is missing.");
        EcnReplacementDefinition replacement = conversion.Replacements.Single();
        EcnContainedValueMapping mapping = conversion.ContainedValueMapping
            ?? throw new AssertionException("The contained mapping is missing.");

        Assert.Multiple(() =>
        {
            Assert.That(replacement.Scope,
                Is.EqualTo(EcnReplacementScope.Structure));
            Assert.That(replacement.ReplacementClassSymbol?.Name,
                Is.EqualTo("#Wrapped"));
            Assert.That(replacement.EncodingObjectSymbol?.Name,
                Is.EqualTo("wrappedEncoding"));
            Assert.That(replacement.Valid, Is.True);
            Assert.That(mapping.ContainerCategory,
                Is.EqualTo(EcnClassCategory.OctetString));
            Assert.That(mapping.ElementParameter?.Name, Is.EqualTo("#Element"));
            Assert.That(mapping.Reversible, Is.True);
            Assert.That(conversion.Reversible, Is.True);
            Assert.That(copy.Conversion, Is.Not.SameAs(conversion));
            Assert.That(copy.Conversion?.Replacements,
                Is.Not.SameAs(conversion.Replacements));
            Assert.That(
                fixture.Diagnostics.GetDiagnostics(),
                Is.Empty,
                string.Join(" | ", fixture.Diagnostics.GetDiagnostics()
                    .Select(diagnostic => diagnostic.Code + ": "
                        + diagnostic.Message)));
        });
    }

    [Test]
    public void ResolvesKnownAndUnknownOpenTypeMappings()
    {
        EcnFixture fixture = Compile(
            "OpenMappings ENCODING-DEFINITIONS ::= BEGIN\n"
                + "integer #INT ::= { }\n"
                + "Known #ENCODINGS ::= { integer }\n"
                + "unknown #OCTETS ::= { }\n"
                + "Unknown #ENCODINGS ::= { unknown }\n"
                + "#Unknown ::= #OCTETS\n"
                + "open #OPEN-TYPE ::= { ENCODED WITH Known "
                + "UNKNOWN IS #Unknown ENCODED WITH Unknown }\n"
                + "END");

        EcnOpenTypeMapping mapping = ObjectDefinition(fixture, "open")
                .Conversion?.OpenTypeMapping
            ?? throw new AssertionException("The open type mapping is missing.");

        Assert.Multiple(() =>
        {
            Assert.That(mapping.KnownEncodingsSymbol?.Name,
                Is.EqualTo("Known"));
            Assert.That(mapping.UnknownStructureSymbol?.Name,
                Is.EqualTo("#Unknown"));
            Assert.That(mapping.UnknownEncodingsSymbol?.Name,
                Is.EqualTo("Unknown"));
            Assert.That(mapping.Valid, Is.True);
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void BuildsOrderedReversibleAndDirectionalTransformPipelines()
    {
        EcnFixture fixture = Compile(
            "Transforms ENCODING-DEFINITIONS ::= BEGIN\n"
                + "mapped #INT ::= { USE #INT MAPPING TRANSFORMS "
                + "{{ INT-TO-INT add:1 }, { INT-TO-INT multiply:2 }} }\n"
                + "direct #INT ::= { TRANSFORMS {{ INT-TO-INT decrement:1 }} }\n"
                + "decoded {< REFERENCE : d >} #OPTIONAL ::= {\n"
                + "  PRESENCE DETERMINED BY field-to-be-used USING d\n"
                + "  DECODER-TRANSFORMS {{ INT-TO-BOOL TRUE-IS {0} }} }\n"
                + "encoded {< REFERENCE : d >} #OPTIONAL ::= {\n"
                + "  PRESENCE DETERMINED BY field-to-be-set USING d\n"
                + "  ENCODER-TRANSFORMS {{ BOOL-TO-INT standard }} }\n"
                + "END");

        EcnTransformPipeline mapping = ObjectDefinition(fixture, "mapped")
            .Conversion?.TransformPipelines.Single()
            ?? throw new AssertionException("The mapping pipeline is missing.");
        EcnTransformPipeline direct = ObjectDefinition(fixture, "direct")
            .Conversion?.TransformPipelines.Single()
            ?? throw new AssertionException("The value pipeline is missing.");
        EcnTransformPipeline decoder = ObjectDefinition(fixture, "decoded")
            .Conversion?.TransformPipelines.Single()
            ?? throw new AssertionException("The decoder pipeline is missing.");
        EcnTransformPipeline encoder = ObjectDefinition(fixture, "encoded")
            .Conversion?.TransformPipelines.Single()
            ?? throw new AssertionException("The encoder pipeline is missing.");

        Assert.Multiple(() =>
        {
            Assert.That(mapping.Direction,
                Is.EqualTo(EcnTransformDirection.Mapping));
            Assert.That(mapping.Steps.Select(step => step.Order),
                Is.EqualTo(new[] { 0, 1 }));
            Assert.That(mapping.Steps.Select(step => step.Operation),
                Is.EqualTo(new[] { "add", "multiply" }));
            Assert.That(mapping.Reversible, Is.True);
            Assert.That(mapping.Valid, Is.True);
            Assert.That(direct.Direction,
                Is.EqualTo(EcnTransformDirection.Value));
            Assert.That(direct.SourceKind,
                Is.EqualTo(EcnTransformValueKind.Integer));
            Assert.That(direct.ResultKind,
                Is.EqualTo(EcnTransformValueKind.Integer));
            Assert.That(direct.Valid, Is.True);
            Assert.That(decoder.Direction,
                Is.EqualTo(EcnTransformDirection.Decoder));
            Assert.That(decoder.Steps.Single().Reversible, Is.False);
            Assert.That(decoder.Valid, Is.True);
            Assert.That(encoder.Direction,
                Is.EqualTo(EcnTransformDirection.Encoder));
            Assert.That(encoder.SourceKind,
                Is.EqualTo(EcnTransformValueKind.Boolean));
            Assert.That(encoder.ResultKind,
                Is.EqualTo(EcnTransformValueKind.Integer));
            Assert.That(encoder.Valid, Is.True);
            Assert.That(
                fixture.Diagnostics.GetDiagnostics(),
                Is.Empty,
                string.Join(" | ", fixture.Diagnostics.GetDiagnostics()
                    .Select(diagnostic => diagnostic.Code + ": "
                        + diagnostic.Message)));
        });
    }

    [Test]
    public void ClassifiesEveryReplacementScopeAndTransformValueCategory()
    {
        EcnConversionCompiler compiler = new EcnConversionCompiler(
            new DiagnosticBag(false, new AsnResourceLimits()));
        AsnToken[][] replacementTokens =
        [
            Tokens("REPLACE", "STRUCTURE", "WITH", "#INT"),
            Tokens("REPLACE", "COMPONENT", "WITH", "#INT"),
            Tokens("REPLACE", "ALL", "COMPONENTS", "WITH", "#INT"),
            Tokens("REPLACE", "OPTIONALS", "WITH", "#INT"),
            Tokens("REPLACE", "NON-OPTIONALS", "WITH", "#INT")
        ];
        EcnReplacementScope[] replacementScopes =
        [
            EcnReplacementScope.Structure,
            EcnReplacementScope.Component,
            EcnReplacementScope.AllComponents,
            EcnReplacementScope.OptionalComponents,
            EcnReplacementScope.NonOptionalComponents
        ];
        string[] transformNames =
        [
            "BOOL",
            "INT",
            "REAL",
            "BITS",
            "OCTETS",
            "CHARS",
            "OID",
            "STRUCTURE",
            "OPEN-TYPE"
        ];
        EcnTransformValueKind[] transformKinds =
        [
            EcnTransformValueKind.Boolean,
            EcnTransformValueKind.Integer,
            EcnTransformValueKind.Real,
            EcnTransformValueKind.BitString,
            EcnTransformValueKind.OctetString,
            EcnTransformValueKind.CharacterString,
            EcnTransformValueKind.ObjectIdentifier,
            EcnTransformValueKind.Structure,
            EcnTransformValueKind.OpenType
        ];
        EcnReplacementScope[] actualScopes = replacementTokens.Select(
            item => compiler.ReadReplacementScope(item, 0, item.Length))
            .ToArray();
        EcnTransformValueKind[] actualKinds = transformNames.Select(
            compiler.ParseTransformKind).ToArray();
        EcnTransformValueKind[] definedKinds = System.Enum.GetValues<
                EcnTransformValueKind>()
            .Where(item => item != EcnTransformValueKind.Unknown)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(actualScopes, Is.EqualTo(replacementScopes));
            Assert.That(actualScopes.Distinct(), Is.EqualTo(
                System.Enum.GetValues<EcnReplacementScope>()));
            Assert.That(actualKinds, Is.EqualTo(transformKinds));
            Assert.That(actualKinds.Distinct(), Is.EqualTo(definedKinds));
            Assert.That(compiler.ParseTransformKind("BOOLEAN"),
                Is.EqualTo(EcnTransformValueKind.Boolean));
            Assert.That(compiler.ParseTransformKind("INTEGER"),
                Is.EqualTo(EcnTransformValueKind.Integer));
            Assert.That(compiler.ParseTransformKind("BIT-STRING"),
                Is.EqualTo(EcnTransformValueKind.BitString));
            Assert.That(compiler.ParseTransformKind("OCTET-STRING"),
                Is.EqualTo(EcnTransformValueKind.OctetString));
            Assert.That(compiler.ParseTransformKind("CHARACTER-STRING"),
                Is.EqualTo(EcnTransformValueKind.CharacterString));
            Assert.That(compiler.ParseTransformKind("OBJECT-IDENTIFIER"),
                Is.EqualTo(EcnTransformValueKind.ObjectIdentifier));
            Assert.That(compiler.ParseTransformKind("unknown"),
                Is.EqualTo(EcnTransformValueKind.Unknown));
        });
    }

    [Test]
    public void ReplacementScopeCompatibilityMatchesEveryClassCategory()
    {
        EcnConversionCompiler compiler = new EcnConversionCompiler(
            new DiagnosticBag(false, new AsnResourceLimits()));
        EcnClassCategory[] categories =
            System.Enum.GetValues<EcnClassCategory>();
        EcnReplacementScope[] scopes =
            System.Enum.GetValues<EcnReplacementScope>();
        int categoryIndex = 0;
        while (categoryIndex < categories.Length)
        {
            int scopeIndex = 0;
            while (scopeIndex < scopes.Length)
            {
                EcnClassCategory category = categories[categoryIndex];
                EcnReplacementScope scope = scopes[scopeIndex];
                bool expected = category == EcnClassCategory.Concatenation
                    || (category == EcnClassCategory.Optionality
                            || category == EcnClassCategory.EncodingProcedure)
                        && (scope == EcnReplacementScope.Structure
                            || scope == EcnReplacementScope.Component
                            || scope == EcnReplacementScope.AllComponents)
                    || category != EcnClassCategory.Concatenation
                        && category != EcnClassCategory.Optionality
                        && category != EcnClassCategory.EncodingProcedure
                        && scope == EcnReplacementScope.Structure;

                Assert.That(compiler.ReplacementScopeIsAllowed(
                        category,
                        scope),
                    Is.EqualTo(expected),
                    category + " + " + scope);
                scopeIndex = scopeIndex + 1;
            }
            categoryIndex = categoryIndex + 1;
        }
    }

    [Test]
    public void RejectsInvalidReplacementOpenTypeAndTransformConversions()
    {
        EcnFixture fixture = Compile(
            "InvalidConversions ENCODING-DEFINITIONS ::= BEGIN\n"
                + "badReplacement #OCTETS ::= { REPETITION-ENCODING { "
                + "REPLACE STRUCTURE WITH #INT } }\n"
                + "badOpen #OPEN-TYPE ::= { UNKNOWN IS #OCTETS }\n"
                + "wrongDirection {< REFERENCE : d >} #OPTIONAL ::= {\n"
                + "  PRESENCE DETERMINED BY field-to-be-used USING d\n"
                + "  ENCODER-TRANSFORMS {{ BOOL-TO-INT standard }} }\n"
                + "nonReversible #INT ::= { USE #INT MAPPING TRANSFORMS "
                + "{{ INT-TO-BOOL TRUE-IS {0} }} }\n"
                + "badChain #INT ::= { USE #INT MAPPING TRANSFORMS "
                + "{{ INT-TO-BOOL standard }, { INT-TO-INT add:1 }} }\n"
                + "END");

        string[] codes = fixture.Diagnostics.GetDiagnostics()
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(codes, Does.Contain("ASNECN0045"));
            Assert.That(codes, Does.Contain("ASNECN0046"));
            Assert.That(codes, Does.Contain("ASNECN0048"));
            Assert.That(codes, Does.Contain("ASNECN0049"));
            Assert.That(codes, Does.Contain("ASNECN0050"));
            Assert.That(ObjectDefinition(fixture, "badReplacement").Valid,
                Is.False);
            Assert.That(ObjectDefinition(fixture, "badOpen").Valid, Is.False);
            Assert.That(ObjectDefinition(fixture, "wrongDirection").Valid,
                Is.False);
            Assert.That(ObjectDefinition(fixture, "nonReversible").Valid,
                Is.False);
            Assert.That(ObjectDefinition(fixture, "badChain").Valid, Is.False);
        });
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

    private static AsnToken[] Tokens(params string[] values)
    {
        AsnToken[] result = new AsnToken[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = new AsnToken { Text = values[index] };
            index = index + 1;
        }
        return result;
    }

    private static EcnFixture Compile(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "conversions.edm",
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
