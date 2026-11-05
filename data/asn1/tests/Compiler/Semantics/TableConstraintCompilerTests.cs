using asn1;

namespace asn1.Tests;

public class TableConstraintCompilerTests
{
    [Test]
    public void CompilerBuildsSimpleTableDispatchRelationships()
    {
        ResolutionFixture fixture = Resolve(
            "Tables DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "first ITEM ::= { &id 1, &Type INTEGER }; "
                + "second ITEM ::= { &id 2, &Type UTF8String }; "
                + "Items ITEM ::= { first | second }; "
                + "Payload ::= ITEM.&Type ({Items}) END");
        ResolvedTypeSyntax payload = RequireType(fixture, "Payload");
        TableConstraintRelationship relationship = payload.TableConstraints.Single();

        Assert.That(relationship.Kind, Is.EqualTo(AsnConstraintKind.SimpleTable));
        Assert.That(relationship.ConstrainedField?.Field?.Name, Is.EqualTo("&Type"));
        Assert.That(relationship.ObjectSetSymbol?.Name, Is.EqualTo("Items"));
        Assert.That(relationship.ObjectSet?.ObjectClass?.Name, Is.EqualTo("ITEM"));
        Assert.That(relationship.DispatchEntries.Select(item => item.Key?.IntegerValue),
            Is.EqualTo(new System.Numerics.BigInteger[] { 1, 2 }));
        Assert.That(relationship.DispatchEntries.Select(item => item.Type?.Kind),
            Is.EqualTo(new[] { AsnTypeKind.Integer, AsnTypeKind.Utf8String }));
        Assert.That(relationship.ObjectSet?.DispatchEntries,
            Is.EqualTo(relationship.DispatchEntries));
        Assert.That(relationship.DispatchMap?.Buckets, Has.Length.EqualTo(2));
        OpenTypeDispatchLookupResult lookup = relationship.DispatchMap?.ResolveKey(
            new AsnValue
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = new System.Numerics.BigInteger(2),
                TextValue = "a different spelling"
            }) ?? throw new InvalidOperationException();
        Assert.That(lookup.Success, Is.True);
        Assert.That(lookup.Entry?.Type?.Kind, Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(lookup.Diagnostics, Is.Empty);
        Assert.That(relationship.StableId, Does.Contain("Tables.Items"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            string.Join(" | ", fixture.Diagnostics.GetDiagnostics().Select(item => item.Code
                + ":" + item.Message)));
    }

    [Test]
    public void CompilerResolvesOutermostAndRelativeAtNotationPaths()
    {
        ResolutionFixture fixture = Resolve(
            "Tables DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "Items ITEM ::= { { &id 1, &Type INTEGER } }; "
                + "Envelope ::= SEQUENCE { "
                + "id ITEM.&id ({Items}), "
                + "nested SEQUENCE { "
                + "localId ITEM.&id ({Items}), "
                + "payload ITEM.&Type ({Items}{@id, @.localId}) } } END");
        ResolvedTypeSyntax envelope = RequireType(fixture, "Envelope");
        ResolvedTypeSyntax nested = envelope.Components[1].TypeResolution
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax payload = nested.Components[1].TypeResolution
            ?? throw new InvalidOperationException();
        TableConstraintRelationship relationship = payload.TableConstraints.Single();

        Assert.That(relationship.Kind,
            Is.EqualTo(AsnConstraintKind.ComponentRelation));
        Assert.That(relationship.ReferencingComponent?.Component?.Name,
            Is.EqualTo("payload"));
        Assert.That(relationship.ConstrainedType, Is.SameAs(envelope));
        Assert.That(relationship.AtNotations.Select(item => item.Components.Single()
                .Component?.Name),
            Is.EqualTo(new[] { "id", "localId" }));
        Assert.That(relationship.AtNotations[0].StartType, Is.SameAs(envelope));
        Assert.That(relationship.AtNotations[1].StartType, Is.SameAs(nested));
        Assert.That(relationship.AtNotations.All(item => item.ReferencedField?.Field?.Name
            == "&id"), Is.True);
        Assert.That(relationship.AtNotations.All(item => item.ReferencedField?.Field?.Unique
            == true), Is.True);
        Assert.That(relationship.AtNotations[0].LookupComponents.Select(item => item.Component?.Name),
            Is.EqualTo(new[] { "id" }));
        Assert.That(relationship.AtNotations[1].LookupComponents.Select(item => item.Component?.Name),
            Is.EqualTo(new[] { "nested", "localId" }));
        OpenTypeDispatchMap map = relationship.DispatchMap
            ?? throw new InvalidOperationException();
        OpenTypeDispatchLookupResult matching = map.ResolveStructuredValue(
            CreateEnvelopeValue(1, 1, true));
        OpenTypeDispatchLookupResult conflicting = map.ResolveStructuredValue(
            CreateEnvelopeValue(1, 2, true));
        OpenTypeDispatchLookupResult missing = map.ResolveStructuredValue(
            CreateEnvelopeValue(1, 1, false));
        Assert.That(matching.Success, Is.True);
        Assert.That(matching.Entry?.Type?.Kind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(conflicting.Success, Is.False);
        Assert.That(conflicting.Diagnostics.Single().Code, Is.EqualTo("ASNC0035"));
        Assert.That(conflicting.Diagnostics.Single().ValuePath,
            Is.EqualTo("nested.localId"));
        Assert.That(missing.Success, Is.False);
        Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo("ASNC0033"));
        Assert.That(missing.Diagnostics.Single().ValuePath,
            Is.EqualTo("nested.localId"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            string.Join(" | ", fixture.Diagnostics.GetDiagnostics().Select(item => item.Code
                + ":" + item.Message)));
    }

    [Test]
    public void CompilerSupportsSimpleTablesOnInstanceOfTypes()
    {
        ResolutionFixture fixture = Resolve(
            "Tables DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "Items ITEM ::= { { &id 7, &Type BOOLEAN } }; "
                + "ItemInstance ::= INSTANCE OF ITEM ({Items}) END");
        TableConstraintRelationship relationship = RequireType(fixture, "ItemInstance")
            .TableConstraints.Single();

        Assert.That(relationship.ConstrainedField?.Field?.Name, Is.EqualTo("&Type"));
        Assert.That(relationship.DispatchEntries.Single().Key?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(7)));
        Assert.That(relationship.DispatchEntries.Single().Type?.Kind,
            Is.EqualTo(AsnTypeKind.Boolean));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompilerResolvesImportedGoverningClassesAndObjectSets()
    {
        ResolutionFixture fixture = ResolveSources([
            "Provider DEFINITIONS ::= BEGIN "
                + "EXPORTS ITEM, Items; "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "Items ITEM ::= { { &id 9, &Type OCTET STRING } } END",
            "Consumer DEFINITIONS ::= BEGIN "
                + "IMPORTS ITEM, Items FROM Provider; "
                + "Payload ::= ITEM.&Type ({Items}) END"
        ]);
        ResolvedTypeSyntax payload = fixture.Tables[1]
            .FindSymbol("Payload", AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();
        TableConstraintRelationship relationship = payload.TableConstraints.Single();

        Assert.That(relationship.ConstrainedField?.ObjectClassSymbol?.QualifiedName,
            Is.EqualTo("Provider.ITEM"));
        Assert.That(relationship.ObjectSetSymbol?.QualifiedName,
            Is.EqualTo("Provider.Items"));
        Assert.That(relationship.DispatchEntries.Single().Type?.Kind,
            Is.EqualTo(AsnTypeKind.OctetString));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompilerRejectsInvalidTableTypesAndObjectSetGovernors()
    {
        ResolutionFixture fixture = Resolve(
            "Tables DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "OTHER ::= CLASS { &key INTEGER UNIQUE, &OtherType }; "
                + "Items ITEM ::= { { &id 1, &Type INTEGER } }; "
                + "OtherItems OTHER ::= { { &key 1, &OtherType BOOLEAN } }; "
                + "BadType ::= INTEGER ({Items}{@missing}); "
                + "BadSet ::= ITEM.&Type ({OtherItems}) END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Count(item => item.Code == "ASNC0017"), Is.EqualTo(1));
        Assert.That(diagnostics.Count(item => item.Code == "ASNC0021"), Is.EqualTo(1));
        Assert.That(diagnostics.Where(item => item.Code == "ASNC0017"
                || item.Code == "ASNC0021")
            .All(item => item.Span != null), Is.True);
    }

    [Test]
    public void CompilerRejectsInvalidOrDifferentlyConstrainedAtNotationComponents()
    {
        ResolutionFixture fixture = Resolve(
            "Tables DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "Items ITEM ::= { { &id 1, &Type INTEGER } }; "
                + "OtherItems ITEM ::= { { &id 2, &Type BOOLEAN } }; "
                + "Message ::= SEQUENCE { "
                + "id ITEM.&id ({OtherItems}), "
                + "payload ITEM.&Type ({Items}{@id}), "
                + "missingPayload ITEM.&Type ({Items}{@missing}) } END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Count(item => item.Code == "ASNC0026"), Is.EqualTo(1));
        Assert.That(diagnostics.Count(item => item.Code == "ASNC0023"), Is.EqualTo(1));
        Assert.That(diagnostics.Where(item => item.Code == "ASNC0026"
                || item.Code == "ASNC0023")
            .All(item => item.Span != null), Is.True);
    }

    [Test]
    public void CompilerDiagnosesNormalizedDispatchKeyAmbiguity()
    {
        ResolutionFixture fixture = Resolve(
            "Tables DEFINITIONS ::= BEGIN "
                + "Code ::= INTEGER { named(1) }; "
                + "ITEM ::= CLASS { &id Code UNIQUE, &Type }; "
                + "namedItem ITEM ::= { &id named, &Type INTEGER }; "
                + "numericItem ITEM ::= { &id 1, &Type BOOLEAN }; "
                + "Items ITEM ::= { namedItem | numericItem }; "
                + "Payload ::= ITEM.&Type ({Items}) END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();
        OpenTypeDispatchMap map = RequireType(fixture, "Payload")
            .TableConstraints.Single().DispatchMap
            ?? throw new InvalidOperationException();
        OpenTypeDispatchLookupResult lookup = map.ResolveKey(new AsnValue
        {
            Kind = AsnValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = System.Numerics.BigInteger.One
        });

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Count(item => item.Code == "ASNC0035"), Is.EqualTo(1));
        Assert.That(diagnostics.Single(item => item.Code == "ASNC0035").Span,
            Is.Not.Null);
        Assert.That(map.Buckets.Single().Entries, Has.Length.EqualTo(2));
        Assert.That(lookup.Success, Is.False);
        Assert.That(lookup.Diagnostics.Single().Code, Is.EqualTo("ASNC0035"));
    }

    [Test]
    public void CompilerDiagnosesMissingDispatchKeysAndTypes()
    {
        ResolutionFixture fixture = Resolve(
            "Tables DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "missingKey ITEM ::= { &Type INTEGER }; "
                + "missingType ITEM ::= { &id 2 }; "
                + "Items ITEM ::= { missingKey | missingType }; "
                + "Payload ::= ITEM.&Type ({Items}) END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Count(item => item.Code == "ASNC0033"), Is.EqualTo(1));
        Assert.That(diagnostics.Count(item => item.Code == "ASNC0034"), Is.EqualTo(1));
        Assert.That(diagnostics.Where(item => item.Code == "ASNC0033"
                || item.Code == "ASNC0034").All(item => item.Span != null), Is.True);
    }

    private static AsnValue CreateEnvelopeValue(
        int outerIdentifier,
        int localIdentifier,
        bool includeLocalIdentifier)
    {
        AsnNamedValue[] nestedValues = includeLocalIdentifier
            ? [new AsnNamedValue
            {
                Name = "localId",
                Value = CreateIntegerValue(localIdentifier)
            }]
            : [];
        return new AsnValue
        {
            Kind = AsnValueKind.Sequence,
            NamedValues = [
                new AsnNamedValue
                {
                    Name = "id",
                    Value = CreateIntegerValue(outerIdentifier)
                },
                new AsnNamedValue
                {
                    Name = "nested",
                    Value = new AsnValue
                    {
                        Kind = AsnValueKind.Sequence,
                        NamedValues = nestedValues
                    }
                }
            ]
        };
    }

    private static AsnValue CreateIntegerValue(int value)
    {
        return new AsnValue
        {
            Kind = AsnValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = new System.Numerics.BigInteger(value)
        };
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
        return ResolveSources([source]);
    }

    private static ResolutionFixture ResolveSources(string[] sources)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        ModuleSyntax[] modules = new ModuleSyntax[sources.Length];
        int sourceIndex = 0;
        while (sourceIndex < sources.Length)
        {
            IncrementalLexSession session = new IncrementalLexSession(
                "table-constraints-" + sourceIndex.ToString() + ".asn",
                false,
                new AsnResourceLimits());
            session.SupplyTextChunk(sources[sourceIndex], true);
            ModuleParseResult parsed = new ModuleParser(new TokenCursor(session, 4))
                .ParseModule();
            Assert.That(parsed.Status, Is.EqualTo(ModuleParseStatus.Parsed));
            Assert.That(session.GetDiagnostics(), Is.Empty);
            modules[sourceIndex] = parsed.Module ?? throw new InvalidOperationException();
            sourceIndex = sourceIndex + 1;
        }

        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(modules);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True,
            string.Join(" | ", diagnostics.GetDiagnostics().Select(item => item.Code
                + ":" + item.Message)));
        Assert.That(types.Success, Is.True,
            string.Join(" | ", diagnostics.GetDiagnostics().Select(item => item.Code
                + ":" + item.Message)));
        ValueResolutionResult values = new ValueResolver(diagnostics).ResolveValues(tables);
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Tables = tables,
            Values = values
        };
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
    }
}
