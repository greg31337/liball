using languagecodes;

namespace languagecodes.Tests;

public class LanguageGroupHierarchyEngineTests
{
    [Test]
    public void ChildQueriesDistinguishDirectAndTransitiveResults()
    {
        LanguageCodeEntry[] direct =
            LanguageRelationshipEngine.GetGroupChildren("ine", false);
        LanguageCodeEntry[] transitive =
            LanguageRelationshipEngine.GetGroupChildren("ine", true);

        Assert.Multiple(() =>
        {
            Assert.That(direct.Any(entry => entry.Set5Code == "gem"), Is.True);
            Assert.That(direct.Any(entry => entry.Set5Code == "gmw"), Is.False);
            Assert.That(transitive.Any(entry => entry.Set5Code == "gem"), Is.True);
            Assert.That(transitive.Any(entry => entry.Set5Code == "gmw"), Is.True);
            Assert.That(transitive.Length, Is.GreaterThan(direct.Length));
            Assert.That(
                transitive.Select(StableCode),
                Is.EqualTo(
                    transitive.Select(StableCode).OrderBy(
                        code => code,
                        StringComparer.Ordinal)));
            Assert.That(
                transitive.Select(StableCode).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(transitive.Length));
        });
    }

    [Test]
    public void ParentQueriesDistinguishDirectAndTransitiveResults()
    {
        LanguageCodeEntry[] direct =
            LanguageRelationshipEngine.GetGroupParents("gmw", false);
        LanguageCodeEntry[] transitive =
            LanguageRelationshipEngine.GetGroupParents("gmw", true);

        Assert.Multiple(() =>
        {
            Assert.That(direct.Select(entry => entry.Set5Code), Is.EqualTo(new[] { "gem" }));
            Assert.That(transitive.Any(entry => entry.Set5Code == "gem"), Is.True);
            Assert.That(transitive.Any(entry => entry.Set5Code == "ine"), Is.True);
            Assert.That(
                transitive.Select(StableCode),
                Is.EqualTo(
                    transitive.Select(StableCode).OrderBy(
                        code => code,
                        StringComparer.Ordinal)));
        });
    }

    [Test]
    public void RootsLeavesAndUnknownCodesReturnEmptySides()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LanguageRelationshipEngine.GetGroupParents("ine", true), Is.Empty);
            Assert.That(LanguageRelationshipEngine.GetGroupChildren("gmw", true), Is.Empty);
            Assert.That(LanguageRelationshipEngine.GetGroupParents("unknown", true), Is.Empty);
            Assert.That(LanguageRelationshipEngine.GetGroupChildren("unknown", true), Is.Empty);
            Assert.That(LanguageRelationshipEngine.GetGroupParents(null, true), Is.Empty);
            Assert.That(LanguageRelationshipEngine.GetGroupChildren(string.Empty, true), Is.Empty);
        });
    }

    [Test]
    public void InjectedCyclesAndDiamondsTerminateWithoutDuplicates()
    {
        RelationshipIndexEntry[] edges =
        {
            CreateEdge("aaa", "bbb"),
            CreateEdge("aaa", "bbb"),
            CreateEdge("aaa", "ccc"),
            CreateEdge("bbb", "ddd"),
            CreateEdge("ccc", "ddd"),
            CreateEdge("ddd", "aaa")
        };
        RelationshipIndexData relationships = BuildUncheckedIndex(edges);
        LanguageCatalogData catalog = BuildGroupCatalog(
            new[] { "ddd", "bbb", "aaa", "ccc" });

        LanguageCodeEntry[] directChildren =
            LanguageRelationshipEngine.GetGroupChildEntries(
                "aaa",
                false,
                relationships,
                catalog);
        LanguageCodeEntry[] allChildren =
            LanguageRelationshipEngine.GetGroupChildEntries(
                "aaa",
                true,
                relationships,
                catalog);
        LanguageCodeEntry[] allParents =
            LanguageRelationshipEngine.GetGroupParentEntries(
                "ddd",
                true,
                relationships,
                catalog);

        Assert.Multiple(() =>
        {
            Assert.That(
                directChildren.Select(entry => entry.Set5Code),
                Is.EqualTo(new[] { "bbb", "ccc" }));
            Assert.That(
                allChildren.Select(entry => entry.Set5Code),
                Is.EqualTo(new[] { "bbb", "ccc", "ddd" }));
            Assert.That(
                allParents.Select(entry => entry.Set5Code),
                Is.EqualTo(new[] { "aaa", "bbb", "ccc" }));
            Assert.That(allChildren.Any(entry => entry.Set5Code == "aaa"), Is.False);
            Assert.That(allParents.Any(entry => entry.Set5Code == "ddd"), Is.False);
        });
    }

    [Test]
    public void TraversalContinuesThroughAnUnmaterializedIntermediateCode()
    {
        RelationshipIndexEntry[] edges =
        {
            CreateEdge("aaa", "missing"),
            CreateEdge("missing", "bbb")
        };
        RelationshipIndexData relationships = BuildUncheckedIndex(edges);
        LanguageCatalogData catalog = BuildGroupCatalog(new[] { "aaa", "bbb" });

        LanguageCodeEntry[] direct = LanguageRelationshipEngine.GetGroupChildEntries(
            "aaa",
            false,
            relationships,
            catalog);
        LanguageCodeEntry[] transitive = LanguageRelationshipEngine.GetGroupChildEntries(
            "aaa",
            true,
            relationships,
            catalog);

        Assert.Multiple(() =>
        {
            Assert.That(direct, Is.Empty);
            Assert.That(transitive.Select(entry => entry.Set5Code), Is.EqualTo(new[] { "bbb" }));
        });
    }

    [Test]
    public void HierarchyResultsAreRecursiveDefensiveSnapshots()
    {
        LanguageCodeEntry[] first =
            LanguageRelationshipEngine.GetGroupChildren("ine", true);
        LanguageCodeEntry[] second =
            LanguageRelationshipEngine.GetGroupChildren("ine", true);
        LanguageCodeEntry firstGmw = first.Single(entry => entry.Set5Code == "gmw");
        LanguageCodeEntry secondGmw = second.Single(entry => entry.Set5Code == "gmw");
        string originalName = secondGmw.Names[0].Text!;

        firstGmw.Set5Code = "changed";
        firstGmw.Names[0].Text = "changed";
        firstGmw.Relationships[0].ParentCode = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(firstGmw, Is.Not.SameAs(secondGmw));
            Assert.That(secondGmw.Set5Code, Is.EqualTo("gmw"));
            Assert.That(secondGmw.Names[0].Text, Is.EqualTo(originalName));
            Assert.That(secondGmw.Relationships[0].ParentCode, Is.Not.EqualTo("changed"));
            Assert.That(
                LanguageRelationshipEngine.GetGroupChildren("ine", true)
                    .Any(entry => entry.Set5Code == "gmw"),
                Is.True);
        });
    }

    private static RelationshipIndexData BuildUncheckedIndex(RelationshipIndexEntry[] edges)
    {
        Dictionary<string, List<RelationshipIndexEntry>> children =
            RelationshipIndexBuilder.CreateRelationshipLists();
        Dictionary<string, List<RelationshipIndexEntry>> parents =
            RelationshipIndexBuilder.CreateRelationshipLists();
        foreach (RelationshipIndexEntry edge in edges)
        {
            RelationshipIndexBuilder.AddRelationshipEntry(children, edge.ParentCode, edge);
            RelationshipIndexBuilder.AddRelationshipEntry(parents, edge.ChildCode, edge);
        }

        Dictionary<string, RelationshipIndexEntry[]> empty =
            new Dictionary<string, RelationshipIndexEntry[]>(StringComparer.Ordinal);
        return new RelationshipIndexData(
            empty,
            new Dictionary<string, RelationshipIndexEntry[]>(StringComparer.Ordinal),
            RelationshipIndexBuilder.FreezeRelationshipEntries(children),
            RelationshipIndexBuilder.FreezeRelationshipEntries(parents));
    }

    private static LanguageCatalogData BuildGroupCatalog(string[] codes)
    {
        LanguageRow[] rows = codes.Select(CreateGroupRow).ToArray();
        return LanguageCatalogBuilder.BuildCatalog(
            rows,
            Array.Empty<NameRow>(),
            Array.Empty<AliasRow>(),
            Array.Empty<RelationshipRow>(),
            Array.Empty<RetirementRow>());
    }

    private static RelationshipIndexEntry CreateEdge(string parentCode, string childCode)
    {
        return new RelationshipIndexEntry(
            parentCode,
            childCode,
            LanguageRelationshipKind.Set5ParentChild,
            ReconciledRelationshipStatus.Active,
            ReconciledSource.Iso639Set5);
    }

    private static LanguageRow CreateGroupRow(string code)
    {
        return new LanguageRow(
            "set5:" + code,
            null,
            null,
            null,
            null,
            code,
            LanguageScope.Collection,
            LanguageType.Unknown,
            LanguageCodeStatus.Active,
            ReconciledGroupKind.Family,
            null);
    }

    private static string StableCode(LanguageCodeEntry entry)
    {
        return LanguageCatalogBuilder.FindStablePublicCode(entry);
    }
}
