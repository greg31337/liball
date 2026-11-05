using asn1;

namespace asn1.Tests;

public class EcnSymbolTableTests
{
    [Test]
    public void BuilderIndexesAssignmentsAndScopedParametersWithGovernors()
    {
        EcnParseFixture fixture = ParseEcn(
            "Symbols ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Small {< INTEGER : limit, REFERENCE : path, #ENCODINGS : sets >} ::= #INT\n"
                + "valueEncoding {< #Small.&value : chosen, #ClassParameter >} #ClassParameter ::= { SIZE 8 }\n"
                + "Encodings #ENCODINGS ::= { valueEncoding {< 1, #Small >} }\n"
                + "END");
        EcnSymbolTable table = BuildSingle(fixture.Module, [], fixture.Diagnostics);
        EcnSymbol encodingClass = table.EncodingClasses.Single();
        EcnSymbol encodingObject = table.EncodingObjects.Single();

        Assert.Multiple(() =>
        {
            Assert.That(table.LocalSymbols.Select(item => item.Name),
                Is.EqualTo(new[] { "#Small", "valueEncoding", "Encodings" }));
            Assert.That(table.EncodingObjectSets.Single().GovernorKind,
                Is.EqualTo(EcnGovernorKind.EncodingObjectSet));
            Assert.That(table.Parameters, Has.Length.EqualTo(5));
            Assert.That(encodingClass.Scope?.Symbols.Select(item => item.ParameterKind),
                Is.EqualTo(new[]
                {
                    EcnParameterKind.AsnValue,
                    EcnParameterKind.Reference,
                    EcnParameterKind.EncodingObjectSet
                }));
            Assert.That(encodingClass.Scope?.Symbols[0].GovernorKind,
                Is.EqualTo(EcnGovernorKind.AsnType));
            Assert.That(encodingClass.Scope?.Symbols[0].GovernorBuiltIn, Is.True);
            Assert.That(encodingObject.GovernorSymbol,
                Is.SameAs(encodingObject.Scope?.Symbols[1]));
            Assert.That(encodingObject.GovernorSymbol?.ParameterKind,
                Is.EqualTo(EcnParameterKind.EncodingClass));
            Assert.That(encodingObject.Scope?.Symbols[0].GovernorKind,
                Is.EqualTo(EcnGovernorKind.EncodingClassField));
            Assert.That(encodingObject.Scope?.Symbols[0].GovernorSymbol,
                Is.SameAs(encodingClass));
            Assert.That(fixture.Module.SymbolTable, Is.SameAs(table));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void InlineConstructorCreatesAndGovernsAnEncodingStructure()
    {
        EcnParseFixture fixture = ParseEcn(
            "Structures ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Base ::= #INT\n"
                + "#Packet ::= #CONCATENATION { value #Base }\n"
                + "packet #Packet ::= { SIZE 8 }\n"
                + "END");
        EcnSymbolTable table = BuildSingle(fixture.Module, [], fixture.Diagnostics);
        EcnSymbol baseClass = table.EncodingClasses.Single();
        EcnSymbol structure = table.EncodingStructures.Single();
        EcnSymbol encodingObject = table.EncodingObjects.Single();

        Assert.Multiple(() =>
        {
            Assert.That(baseClass.Name, Is.EqualTo("#Base"));
            Assert.That(baseClass.GovernorBuiltIn, Is.True);
            Assert.That(baseClass.GovernorName, Is.EqualTo("#INT"));
            Assert.That(structure.Name, Is.EqualTo("#Packet"));
            Assert.That(structure.GovernorBuiltIn, Is.True);
            Assert.That(structure.GovernorName, Is.EqualTo("#CONCATENATION"));
            Assert.That(encodingObject.GovernorSymbol, Is.SameAs(structure));
            Assert.That(table.FindClassSymbol("#Packet"), Is.SameAs(structure));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void ImportedAsnTypeProvidesAnImplicitEncodingStructureGovernor()
    {
        AsnAssignment packetType = new AsnAssignment
        {
            Name = "Packet",
            Kind = AsnAssignmentKind.Type,
            Type = new AsnType
            {
                Name = "Packet",
                Kind = AsnTypeKind.Sequence
            }
        };
        AsnModule asnModule = new AsnModule
        {
            Name = "Abstract",
            Assignments = [packetType]
        };
        EcnParseFixture fixture = ParseEcn(
            "Bindings ENCODING-DEFINITIONS ::= BEGIN\n"
                + "IMPORTS #Packet FROM Abstract;\n"
                + "packetEncoding #Packet ::= { SIZE 8 }\n"
                + "END");

        EcnSymbolTable table = BuildSingle(
            fixture.Module,
            [asnModule],
            fixture.Diagnostics);
        EcnSymbol imported = table.ImportedSymbols.Single();

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Module.ImportedModules.Single().ModuleName,
                Is.EqualTo("Abstract"));
            Assert.That(fixture.Module.ImportedModules.Single().Symbols,
                Is.EqualTo(new[] { "#Packet" }));
            Assert.That(imported.Kind, Is.EqualTo(EcnSymbolKind.EncodingStructure));
            Assert.That(imported.ImportedAsnType, Is.True);
            Assert.That(imported.AsnTypeAssignment, Is.SameAs(packetType));
            Assert.That(imported.QualifiedName, Is.EqualTo("Abstract.#Packet"));
            Assert.That(imported.ClassCategory,
                Is.EqualTo(EcnClassCategory.EncodingStructure));
            Assert.That(imported.ClassCategoryGroups,
                Is.EqualTo(new[] { EcnClassCategoryGroup.BitField }));
            Assert.That(table.EncodingObjects.Single().GovernorSymbol,
                Is.SameAs(imported));
            Assert.That(table.EncodingObjects.Single().ClassCategory,
                Is.EqualTo(EcnClassCategory.EncodingStructure));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void EcnImportsRespectExportsAndQualifiedGovernors()
    {
        EcnParseFixture provider = ParseEcn(
            "Provider ENCODING-DEFINITIONS ::= BEGIN\n"
                + "EXPORTS #Base, baseEncoding;\n"
                + "#Base ::= #INT\n"
                + "baseEncoding #Base ::= { SIZE 8 }\n"
                + "#Hidden ::= #BOOL\n"
                + "END");
        EcnParseFixture consumer = ParseEcn(
            "Consumer ENCODING-DEFINITIONS ::= BEGIN\n"
                + "IMPORTS #Base, baseEncoding FROM Provider;\n"
                + "copy Provider.#Base ::= { SIZE 8 }\n"
                + "END");
        DiagnosticBag diagnostics = new DiagnosticBag(
            false,
            new AsnResourceLimits());

        EcnSymbolTable[] tables = new EcnSymbolTableBuilder(diagnostics)
            .BuildTables([provider.Module, consumer.Module], []);
        EcnSymbolTable consumerTable = tables[1];

        Assert.Multiple(() =>
        {
            Assert.That(tables[0].LocalSymbols.Select(item => item.Exported),
                Is.EqualTo(new[] { true, true, false }));
            Assert.That(consumerTable.ImportedSymbols.Select(item => item.Name),
                Is.EqualTo(new[] { "#Base", "baseEncoding" }));
            Assert.That(consumerTable.FindSymbol("copy", EcnSymbolKind.EncodingObject)
                    ?.GovernorSymbol,
                Is.SameAs(consumerTable.ImportedSymbols[0]));
            Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void BuilderDiagnosesDuplicatesAndInvalidGovernors()
    {
        EcnParseFixture fixture = ParseEcn(
            "Invalid ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Base ::= #INT\n"
                + "#Base ::= #BOOL\n"
                + "#Parameterized {< REFERENCE : name, REFERENCE : name >} ::= #INT\n"
                + "Encodings #ENCODINGS ::= { }\n"
                + "wrong Encodings ::= { SIZE 8 }\n"
                + "missing #Missing ::= { SIZE 8 }\n"
                + "END");

        EcnSymbolTable table = BuildSingle(fixture.Module, [], fixture.Diagnostics);
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.Multiple(() =>
        {
            Assert.That(table.LocalSymbols, Has.Length.EqualTo(6));
            Assert.That(diagnostics.Select(item => item.Code),
                Does.Contain("ASNECN0001"));
            Assert.That(diagnostics.Select(item => item.Code),
                Does.Contain("ASNECN0002"));
            Assert.That(diagnostics.Select(item => item.Code),
                Does.Contain("ASNECN0005"));
            Assert.That(diagnostics.Select(item => item.Code),
                Does.Contain("ASNECN0007"));
            Assert.That(diagnostics.Single(item => item.Code == "ASNECN0001")
                .RelatedLocations, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void CategoryResolverPropagatesAliasesStructuresAndObjectGovernors()
    {
        EcnParseFixture fixture = ParseEcn(
            "Categories ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#MyInteger ::= #INTEGER\n"
                + "#SecondInteger ::= #MyInteger\n"
                + "#Record ::= #CONCATENATION { first #SecondInteger, second #BOOLEAN }\n"
                + "integerEncoding #SecondInteger ::= { SIZE 8 }\n"
                + "recordEncoding #Record ::= { SIZE 16 }\n"
                + "END");

        EcnSymbolTable table = BuildSingle(fixture.Module, [], fixture.Diagnostics);
        EcnSymbol first = table.FindClassSymbol("#MyInteger")
            ?? throw new AssertionException("The first alias is missing.");
        EcnSymbol second = table.FindClassSymbol("#SecondInteger")
            ?? throw new AssertionException("The second alias is missing.");
        EcnSymbol structure = table.FindClassSymbol("#Record")
            ?? throw new AssertionException("The structure is missing.");

        Assert.Multiple(() =>
        {
            Assert.That(first.ClassCategory, Is.EqualTo(EcnClassCategory.Integer));
            Assert.That(first.PrimitiveClassName, Is.EqualTo("#INT"));
            Assert.That(second.ClassCategory, Is.EqualTo(EcnClassCategory.Integer));
            Assert.That(second.PrimitiveClassName, Is.EqualTo("#INT"));
            Assert.That(second.ClassCategoryGroups,
                Is.EqualTo(new[] { EcnClassCategoryGroup.BitField }));
            Assert.That(structure.Kind,
                Is.EqualTo(EcnSymbolKind.EncodingStructure));
            Assert.That(structure.ClassCategory,
                Is.EqualTo(EcnClassCategory.EncodingStructure));
            Assert.That(table.FindSymbol("integerEncoding", EcnSymbolKind.EncodingObject)
                    ?.ClassCategory,
                Is.EqualTo(EcnClassCategory.Integer));
            Assert.That(table.FindSymbol("recordEncoding", EcnSymbolKind.EncodingObject)
                    ?.ClassCategory,
                Is.EqualTo(EcnClassCategory.EncodingStructure));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void CategoryResolverDiagnosesAliasCycles()
    {
        EcnParseFixture fixture = ParseEcn(
            "Cycles ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#First ::= #Second\n"
                + "#Second ::= #First\n"
                + "END");

        EcnSymbolTable table = BuildSingle(fixture.Module, [], fixture.Diagnostics);
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.Multiple(() =>
        {
            Assert.That(table.EncodingClasses, Has.Length.EqualTo(2));
            Assert.That(table.EncodingClasses.All(item => item.ClassCategory
                == EcnClassCategory.Unknown), Is.True);
            Assert.That(diagnostics.Select(item => item.Code),
                Is.EqualTo(new[] { "ASNECN0009" }));
        });
    }

    private static EcnSymbolTable BuildSingle(
        EcnModuleSyntax module,
        AsnModule[] asnModules,
        DiagnosticBag diagnostics)
    {
        return new EcnSymbolTableBuilder(diagnostics).BuildTable(
            module,
            asnModules);
    }

    private static EcnParseFixture ParseEcn(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "symbols.edm",
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
                throw new AssertionException("Final ECN input requested more data.");
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
        return new EcnParseFixture
        {
            Diagnostics = session.Diagnostics,
            Module = parsed.Module ?? throw new AssertionException(
                "No ECN module was parsed.")
        };
    }

    private class EcnParseFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal EcnModuleSyntax Module = null!;
    }
}
