using asn1;

namespace asn1.Tests;

public class EcnPredeclaredEncodingCatalogTests
{
    [Test]
    public void CatalogProvidesStableStandardSetsAndObjectIdentifiers()
    {
        EcnPredeclaredEncodingObjectSet[] sets =
            EcnEncodingObjectSetCatalog.GetSets();
        EcnPredeclaredEncodingObjectSet? firstDer =
            EcnEncodingObjectSetCatalog.FindSet("DER");
        EcnPredeclaredEncodingObjectSet? secondDer =
            EcnEncodingObjectSetCatalog.FindSet("DER");

        Assert.Multiple(() =>
        {
            Assert.That(sets, Has.Length.EqualTo(7));
            Assert.That(sets.Select(set => set.Name), Is.EqualTo(new[]
            {
                "PER-BASIC-ALIGNED",
                "PER-BASIC-UNALIGNED",
                "PER-CANONICAL-ALIGNED",
                "PER-CANONICAL-UNALIGNED",
                "BER",
                "CER",
                "DER"
            }));
            Assert.That(sets.Select(set => set.ObjectIdentifier),
                Is.EqualTo(new[]
                {
                    "2.1.3.0.0",
                    "2.1.3.0.1",
                    "2.1.3.1.0",
                    "2.1.3.1.1",
                    "2.1.1",
                    "2.1.2.0",
                    "2.1.2.1"
                }));
            Assert.That(sets.Select(set => set.StableId).Distinct().ToArray(),
                Has.Length.EqualTo(7));
            Assert.That(firstDer, Is.SameAs(secondDer));
            Assert.That(EcnEncodingObjectSetCatalog.FindSet("missing"),
                Is.Null);
            Assert.That(sets.Single(set => set.Name == "PER-BASIC-ALIGNED")
                .Aligned, Is.True);
            Assert.That(sets.Single(set => set.Name == "PER-CANONICAL-UNALIGNED")
                .Canonical, Is.True);
            Assert.That(sets.Single(set => set.Name == "BER").Canonical,
                Is.False);
        });
    }

    [Test]
    public void CatalogSharesBasicObjectsAndSeparatesRuleSpecificObjects()
    {
        EcnPredeclaredEncodingObject? berInteger =
            EcnEncodingObjectSetCatalog.FindObject("BER", "#INT");
        EcnPredeclaredEncodingObject? perInteger =
            EcnEncodingObjectSetCatalog.FindObject(
                "PER-BASIC-ALIGNED",
                "#INT");
        EcnPredeclaredEncodingObject? berBoolean =
            EcnEncodingObjectSetCatalog.FindObject("BER", "#BOOLEAN");
        EcnPredeclaredEncodingObject? perBoolean =
            EcnEncodingObjectSetCatalog.FindObject(
                "PER-BASIC-ALIGNED",
                "#BOOLEAN");
        EcnPredeclaredEncodingObject? berOpen =
            EcnEncodingObjectSetCatalog.FindObject("BER", "#OPEN-TYPE");

        Assert.Multiple(() =>
        {
            Assert.That(berInteger, Is.Not.Null);
            Assert.That(berInteger, Is.SameAs(perInteger));
            Assert.That(berInteger?.Shared, Is.True);
            Assert.That(berInteger?.RequiresBounds, Is.True);
            Assert.That(berBoolean, Is.Not.Null);
            Assert.That(berBoolean, Is.Not.SameAs(perBoolean));
            Assert.That(berBoolean?.Rule,
                Is.EqualTo(EcnPredeclaredEncodingRule.Ber));
            Assert.That(perBoolean?.Rule,
                Is.EqualTo(EcnPredeclaredEncodingRule.PerBasicAligned));
            Assert.That(berOpen?.NoAdditionalEncoding, Is.True);
            Assert.That(EcnEncodingObjectSetCatalog.FindObject(
                "BER", "#ALTERNATIVES"), Is.Null);
            Assert.That(EcnEncodingObjectSetCatalog.FindObject(
                "BER", "#REPETITION"), Is.Null);
            Assert.That(EcnEncodingObjectSetCatalog.FindObject(
                "BER", "#PAD"), Is.Null);
            Assert.That(EcnEncodingObjectSetCatalog.GetSets()
                .All(set => set.Objects.Length == 46), Is.True);
        });
    }

    [Test]
    public void SemanticReferencesRetainPredeclaredSetIdentities()
    {
        EcnFixture fixture = Compile(
            "StandardReferences ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#MyInteger ::= #INTEGER\n"
                + "#MyBoolean ::= #BOOLEAN\n"
                + "custom #MyInteger ::= { }\n"
                + "Primary #ENCODINGS ::= { custom } COMPLETED BY DER\n"
                + "Standards #ENCODINGS ::= { PER-BASIC-ALIGNED }\n"
                + "Common #ENCODINGS ::= { BER INTERSECTION DER }\n"
                + "BerSpecific #ENCODINGS ::= { BER EXCEPT DER }\n"
                + "linked #INT ::= { USE-SET BER }\n"
                + "template {< #ENCODINGS : Rules >} #INT ::= { USE-SET Rules }\n"
                + "instantiated #INT ::= { USE template {< BER >} }\n"
                + "open #OPEN-TYPE ::= { ENCODED WITH BER "
                + "UNKNOWN IS #OCTETS ENCODED WITH DER }\n"
                + "END");

        EcnEncodingObjectSetDefinition primary = SetDefinition(
            fixture,
            "Primary");
        EcnEncodingObjectSetDefinition standards = SetDefinition(
            fixture,
            "Standards");
        EcnEncodingObjectSetDefinition common = SetDefinition(
            fixture,
            "Common");
        EcnEncodingObjectSetDefinition berSpecific = SetDefinition(
            fixture,
            "BerSpecific");
        EcnSymbol integerClass = ClassSymbol(fixture, "#MyInteger");
        EcnSymbol booleanClass = ClassSymbol(fixture, "#MyBoolean");
        EcnEncodingSelection integerSelection =
            EcnEncodingObjectSelector.SelectEncoding(primary, integerClass);
        EcnEncodingSelection booleanSelection =
            EcnEncodingObjectSelector.SelectEncoding(primary, booleanClass);
        EcnEncodingLink link = ObjectDefinition(fixture, "linked")
            .Links.Single();
        EcnActualParameter actual = ObjectDefinition(fixture, "instantiated")
            .Links.Single().ActualParameters.Single();
        EcnOpenTypeMapping open = ObjectDefinition(fixture, "open")
                .Conversion?.OpenTypeMapping
            ?? throw new AssertionException("The open mapping is missing.");

        Assert.Multiple(() =>
        {
            Assert.That(primary.PredeclaredCompletion?.Name,
                Is.EqualTo("DER"));
            Assert.That(standards.PredeclaredSets.Single().Name,
                Is.EqualTo("PER-BASIC-ALIGNED"));
            Assert.That(standards.PredeclaredObjects, Has.Length.EqualTo(46));
            Assert.That(common.PredeclaredObjects, Has.Length.EqualTo(7));
            Assert.That(common.PredeclaredObjects.All(item => item.Shared),
                Is.True);
            Assert.That(berSpecific.PredeclaredObjects,
                Has.Length.EqualTo(39));
            Assert.That(berSpecific.PredeclaredObjects.Any(item => item.Shared),
                Is.False);
            Assert.That(integerSelection.UserObject?.Name,
                Is.EqualTo("custom"));
            Assert.That(integerSelection.PredeclaredObject, Is.Null);
            Assert.That(booleanSelection.PredeclaredObject?.ClassName,
                Is.EqualTo("#BOOLEAN"));
            Assert.That(booleanSelection.StableId,
                Does.StartWith("x692:object:DER:"));
            Assert.That(link.PredeclaredObjectSet?.Name, Is.EqualTo("BER"));
            Assert.That(actual.PredeclaredSet,
                Is.SameAs(EcnEncodingObjectSetCatalog.FindSet("BER")));
            Assert.That(open.KnownPredeclaredEncodings,
                Is.SameAs(EcnEncodingObjectSetCatalog.FindSet("BER")));
            Assert.That(open.UnknownPredeclaredEncodings,
                Is.SameAs(EcnEncodingObjectSetCatalog.FindSet("DER")));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void BuiltInClassesExposeStableCatalogIdentities()
    {
        EcnBuiltInClassDefinition first = EcnBuiltInClassCatalog.FindClass(
            "#INTEGER")
            ?? throw new AssertionException("The class is missing.");
        EcnBuiltInClassDefinition second = EcnBuiltInClassCatalog.FindClass(
            "#INTEGER")
            ?? throw new AssertionException("The class is missing.");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.SameAs(second));
            Assert.That(first.StableId, Is.EqualTo("x692:class:#INTEGER"));
            Assert.That(EcnBuiltInClassCatalog.GetClasses()
                .Select(item => item.StableId).Distinct().ToArray(),
                Has.Length.EqualTo(53));
        });
    }

    private static EcnSymbol ClassSymbol(EcnFixture fixture, string name)
    {
        return fixture.Table.FindClassSymbol(name)
            ?? throw new AssertionException(
                "The encoding class '" + name + "' is missing.");
    }

    private static EcnEncodingObjectDefinition ObjectDefinition(
        EcnFixture fixture,
        string name)
    {
        return fixture.Table.FindSymbol(name, EcnSymbolKind.EncodingObject)
                ?.ObjectDefinition
            ?? throw new AssertionException(
                "The encoding object '" + name + "' is missing.");
    }

    private static EcnEncodingObjectSetDefinition SetDefinition(
        EcnFixture fixture,
        string name)
    {
        return fixture.Table.FindSymbol(name, EcnSymbolKind.EncodingObjectSet)
                ?.ObjectSetDefinition
            ?? throw new AssertionException(
                "The encoding object set '" + name + "' is missing.");
    }

    private static EcnFixture Compile(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "predeclared.edm",
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
