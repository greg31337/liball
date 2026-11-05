using asn1;

namespace asn1.Tests;

public class EcnEncodingObjectCompilerTests
{
    [Test]
    public void CompilesObjectSettingsDefaultsReferencesAndOrderedLists()
    {
        EcnFixture fixture = Compile(
            "Objects ENCODING-DEFINITIONS ::= BEGIN\n"
                + "first #TRANSFORM ::= { }\n"
                + "second #TRANSFORM ::= { }\n"
                + "main #INT ::= { SIZE 8, mode DEFAULT compact, "
                + "TRANSFORMS { first, second } }\n"
                + "copy #INT ::= main\n"
                + "END");

        EcnEncodingObjectDefinition main = ObjectDefinition(fixture, "main");
        EcnEncodingObjectDefinition copy = ObjectDefinition(fixture, "copy");
        EcnEncodingObjectSetting defaulted = main.Settings.Single(
            setting => setting.Name == "mode");
        EcnOrderedEncodingObjectList ordered = main.OrderedLists.Single();

        Assert.Multiple(() =>
        {
            Assert.That(main.GovernorName, Is.EqualTo("#INT"));
            Assert.That(main.GovernorBuiltIn, Is.True);
            Assert.That(main.Settings.Select(setting => setting.Name),
                Is.EqualTo(new[] { "SIZE", "mode", "TRANSFORMS" }));
            Assert.That(defaulted.Defaulted, Is.True);
            Assert.That(defaulted.DefaultTokens.Select(token => token.Text),
                Is.EqualTo(new[] { "compact" }));
            Assert.That(ordered.Name, Is.EqualTo("TRANSFORMS"));
            Assert.That(ordered.Items.Select(item => item.Order),
                Is.EqualTo(new[] { 0, 1 }));
            Assert.That(ordered.Items.Select(item => item.ReferenceName),
                Is.EqualTo(new[] { "first", "second" }));
            Assert.That(ordered.Items.All(item => item.Valid), Is.True);
            Assert.That(copy.ReferenceDefinition, Is.True);
            Assert.That(copy.ReferencedObjectName, Is.EqualTo("main"));
            Assert.That(copy.ReferencedObjectSymbol?.Name, Is.EqualTo("main"));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void EvaluatesObjectSetAlgebraInDeclarationOrder()
    {
        EcnFixture fixture = Compile(
            "Sets ENCODING-DEFINITIONS ::= BEGIN\n"
                + "one #INT ::= { SIZE 1 }\n"
                + "two #INT ::= { SIZE 2 }\n"
                + "three #INT ::= { SIZE 3 }\n"
                + "First #ENCODINGS ::= { one | two }\n"
                + "Second #ENCODINGS ::= { two UNION three }\n"
                + "Combined #ENCODINGS ::= { First UNION three }\n"
                + "Common #ENCODINGS ::= { First INTERSECTION Second }\n"
                + "Remaining #ENCODINGS ::= { First EXCEPT Second }\n"
                + "END");

        Assert.Multiple(() =>
        {
            Assert.That(SetDefinition(fixture, "Combined").Objects
                    .Select(symbol => symbol.Name),
                Is.EqualTo(new[] { "one", "two", "three" }));
            Assert.That(SetDefinition(fixture, "Common").Objects
                    .Select(symbol => symbol.Name),
                Is.EqualTo(new[] { "two" }));
            Assert.That(SetDefinition(fixture, "Remaining").Objects
                    .Select(symbol => symbol.Name),
                Is.EqualTo(new[] { "one" }));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void SelectsCompatibleObjectsAndUsesCompletionSetAsDefault()
    {
        EcnFixture fixture = Compile(
            "Selection ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#MyInt ::= #INT\n"
                + "#MyBool ::= #BOOL\n"
                + "integerEncoding #INT ::= { SIZE 8 }\n"
                + "specialEncoding #MyInt ::= { SIZE 7 }\n"
                + "booleanEncoding #BOOL ::= { SIZE 1 }\n"
                + "Fallback #ENCODINGS ::= { booleanEncoding }\n"
                + "Primary #ENCODINGS ::= { integerEncoding | specialEncoding } "
                + "COMPLETED BY Fallback\n"
                + "END");

        EcnEncodingObjectSetDefinition primary = SetDefinition(
            fixture,
            "Primary");
        EcnSymbol intClass = ClassSymbol(fixture, "#MyInt");
        EcnSymbol boolClass = ClassSymbol(fixture, "#MyBool");

        Assert.Multiple(() =>
        {
            Assert.That(primary.CompletionName, Is.EqualTo("Fallback"));
            Assert.That(primary.CompletionSymbol?.Name, Is.EqualTo("Fallback"));
            Assert.That(EcnEncodingObjectSelector.SelectObjects(
                    primary,
                    intClass).Select(symbol => symbol.Name),
                Is.EqualTo(new[] { "specialEncoding" }));
            Assert.That(EcnEncodingObjectSelector.SelectObjects(
                    primary,
                    boolClass).Select(symbol => symbol.Name),
                Is.EqualTo(new[] { "booleanEncoding" }));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void BindsParameterizedClassesObjectsAndObjectSets()
    {
        EcnFixture fixture = Compile(
            "Parameters ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Box {< #Element >} ::= #CONCATENATION { value #Element }\n"
                + "#IntBox ::= #Box {< #INT >}\n"
                + "identity {< #Element >} #Element ::= { SIZE 8 }\n"
                + "intIdentity #INT ::= identity {< #INT >}\n"
                + "boxed {< #Element >} #Box {< #Element >} ::= { }\n"
                + "intBoxed #IntBox ::= boxed {< #INT >}\n"
                + "Base #ENCODINGS ::= { intIdentity }\n"
                + "Template {< #ENCODINGS : Members >} #ENCODINGS ::= Members\n"
                + "Instantiated #ENCODINGS ::= Template {< Base >}\n"
                + "END");

        EcnEncodingClassDefinition intBox = ClassSymbol(fixture, "#IntBox")
            .ClassDefinition
            ?? throw new AssertionException("The class definition is missing.");
        EcnEncodingObjectDefinition intIdentity = ObjectDefinition(
            fixture,
            "intIdentity");
        EcnEncodingObjectDefinition boxed = ObjectDefinition(fixture, "boxed");
        EcnEncodingObjectSetDefinition instantiated = SetDefinition(
            fixture,
            "Instantiated");

        Assert.Multiple(() =>
        {
            Assert.That(intBox.AliasActualParameters, Has.Length.EqualTo(1));
            Assert.That(intBox.AliasActualParameters[0].ExpectedKind,
                Is.EqualTo(EcnParameterKind.EncodingClass));
            Assert.That(intBox.AliasActualParameters[0].ReferenceName,
                Is.EqualTo("#INT"));
            Assert.That(intBox.AliasActualParameters[0].Valid, Is.True);
            Assert.That(intIdentity.ReferenceActualParameters,
                Has.Length.EqualTo(1));
            Assert.That(intIdentity.ReferenceActualParameters[0].BuiltIn,
                Is.True);
            Assert.That(boxed.GovernorActualParameters,
                Has.Length.EqualTo(1));
            Assert.That(boxed.GovernorActualParameters[0].Symbol?.Name,
                Is.EqualTo("#Element"));
            Assert.That(boxed.GovernorActualParameters[0].Valid, Is.True);
            Assert.That(instantiated.Terms.Single().ActualParameters,
                Has.Length.EqualTo(1));
            Assert.That(instantiated.Objects.Select(symbol => symbol.Name),
                Is.EqualTo(new[] { "intIdentity" }));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void DiagnosesInvalidReferencesParametersClassesAndSetCycles()
    {
        EcnFixture fixture = Compile(
            "Invalid ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Box {< #Element >} ::= #CONCATENATION { value #Element }\n"
                + "#BadBox ::= #Box {< >}\n"
                + "#MissingBox ::= #Box\n"
                + "integerEncoding #INT ::= { SIZE 8 }\n"
                + "badReference #BOOL ::= integerEncoding\n"
                + "missingReference #INT ::= unknownEncoding\n"
                + "Template {< #ENCODINGS : Members >} #ENCODINGS ::= Members\n"
                + "BadActual #ENCODINGS ::= Template {< integerEncoding >}\n"
                + "MissingActual #ENCODINGS ::= Template\n"
                + "Unknown #ENCODINGS ::= { missingEncoding }\n"
                + "CycleA #ENCODINGS ::= CycleB\n"
                + "CycleB #ENCODINGS ::= CycleA\n"
                + "END");

        string[] codes = fixture.Diagnostics.GetDiagnostics()
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(codes, Does.Contain("ASNECN0018"));
            Assert.That(codes, Does.Contain("ASNECN0020"));
            Assert.That(codes, Does.Contain("ASNECN0021"));
            Assert.That(codes, Does.Contain("ASNECN0022"));
            Assert.That(codes, Does.Contain("ASNECN0024"));
            Assert.That(ObjectDefinition(fixture, "badReference").Valid,
                Is.False);
            Assert.That(SetDefinition(fixture, "CycleA").Valid, Is.False);
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

    private static EcnEncodingObjectSetDefinition SetDefinition(
        EcnFixture fixture,
        string name)
    {
        return fixture.Table.FindSymbol(name, EcnSymbolKind.EncodingObjectSet)
                ?.ObjectSetDefinition
            ?? throw new AssertionException(
                "The encoding object set definition '" + name + "' is missing.");
    }

    private static EcnSymbol ClassSymbol(EcnFixture fixture, string name)
    {
        return fixture.Table.FindClassSymbol(name)
            ?? throw new AssertionException(
                "The encoding class '" + name + "' is missing.");
    }

    private static EcnFixture Compile(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "encoding-objects.edm",
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
