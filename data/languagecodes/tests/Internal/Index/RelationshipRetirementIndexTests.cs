using languagecodes;

namespace languagecodes.Tests;

public class RelationshipRetirementIndexTests
{
    [Test]
    public void MacrolanguageIndexesSupportForwardReverseAndStatusFiltering()
    {
        RelationshipIndexEntry[] activeMembers =
            RelationshipIndex.FindMacrolanguageMembers("ara", false);
        RelationshipIndexEntry[] historicalMembers =
            RelationshipIndex.FindMacrolanguageMembers("ara", true);
        RelationshipIndexEntry[] parents =
            RelationshipIndex.FindMacrolanguagesForMember("aao", false);

        Assert.Multiple(() =>
        {
            Assert.That(
                activeMembers.Any(entry => entry.ChildCode == "aao"),
                Is.True);
            Assert.That(
                activeMembers.All(entry => entry.Status == ReconciledRelationshipStatus.Active),
                Is.True);
            Assert.That(
                activeMembers.Any(entry => entry.ChildCode == "ajp"),
                Is.False);
            Assert.That(
                historicalMembers.Any(
                    entry => entry.ChildCode == "ajp" &&
                        entry.Status == ReconciledRelationshipStatus.Retired),
                Is.True);
            Assert.That(parents.Select(entry => entry.ParentCode), Does.Contain("ara"));
            Assert.That(
                parents.All(entry => entry.Kind == LanguageRelationshipKind.MacrolanguageMember),
                Is.True);
        });
    }

    [Test]
    public void Set5IndexesSupportStableForwardAndReverseLookup()
    {
        RelationshipIndexEntry[] children = RelationshipIndex.FindGroupChildren("afa");
        RelationshipIndexEntry[] parents = RelationshipIndex.FindGroupParents("ber");

        Assert.Multiple(() =>
        {
            Assert.That(children.Select(entry => entry.ChildCode), Does.Contain("ber"));
            Assert.That(
                children.Select(entry => entry.ChildCode),
                Is.EqualTo(children.Select(entry => entry.ChildCode).OrderBy(code => code)));
            Assert.That(parents.Select(entry => entry.ParentCode), Does.Contain("afa"));
            Assert.That(
                children.All(entry => entry.Kind == LanguageRelationshipKind.Set5ParentChild),
                Is.True);
            Assert.That(
                RelationshipIndex.Data.GroupChildrenByParent.Comparer,
                Is.SameAs(StringComparer.Ordinal));
            Assert.That(
                RelationshipIndex.Data.GroupParentsByChild.Comparer,
                Is.SameAs(StringComparer.Ordinal));
        });
    }

    [Test]
    public void RelationshipIndexDoesNotRetainInputRowsOrReturnStoredEntries()
    {
        RelationshipRow source = new RelationshipRow(
            "aaa",
            "bbb",
            LanguageRelationshipKind.MacrolanguageMember,
            ReconciledRelationshipStatus.Active,
            ReconciledSource.Iso639Set3);
        RelationshipIndexData index = RelationshipIndexBuilder.BuildIndex(new[] { source });

        source.ChildCode = "changed";
        RelationshipIndexEntry[] first = index.MacrolanguageMembersByParent["aaa"];
        RelationshipIndexEntry[] copy =
            RelationshipIndexBuilder.CopyRelationshipEntries(first);
        copy[0].ChildCode = "also-changed";

        Assert.Multiple(() =>
        {
            Assert.That(first[0].ChildCode, Is.EqualTo("bbb"));
            Assert.That(copy[0], Is.Not.SameAs(first[0]));
            Assert.That(first[0].ChildCode, Is.EqualTo("bbb"));
        });
    }

    [Test]
    public void Set5HierarchyCyclesFailIndexConstruction()
    {
        RelationshipRow[] cycle =
        {
            CreateSet5Relationship("aaa", "bbb"),
            CreateSet5Relationship("bbb", "ccc"),
            CreateSet5Relationship("ccc", "aaa")
        };

        Assert.Throws<InvalidOperationException>(
            () => RelationshipIndexBuilder.BuildIndex(cycle));
    }

    [Test]
    public void GeneratedRetirementChainsPreserveEveryTerminalOutcome()
    {
        RetirementChainRow? singular = RetirementIndex.FindReplacementChain("fri");
        RetirementChainRow? split = RetirementIndex.FindReplacementChain("agp");
        RetirementChainRow? deleted = RetirementIndex.FindReplacementChain("aay");

        Assert.Multiple(() =>
        {
            Assert.That(singular?.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(singular?.Codes, Is.EqualTo(new[] { "fri", "fry" }));
            Assert.That(singular?.FinalCode, Is.EqualTo("fry"));
            Assert.That(split?.Status, Is.EqualTo(LanguageNormalizationStatus.Split));
            Assert.That(split?.Codes, Is.EqualTo(new[] { "agp" }));
            Assert.That(split?.ReplacementCandidates, Is.EqualTo(new[] { "apf", "prf" }));
            Assert.That(split?.FinalCode, Is.Null);
            Assert.That(deleted?.Status, Is.EqualTo(LanguageNormalizationStatus.NoReplacement));
            Assert.That(deleted?.Codes, Is.EqualTo(new[] { "aay" }));
            Assert.That(deleted?.FinalCode, Is.Null);
        });
    }

    [Test]
    public void RetirementIndexPrecomputesMultiHopReplacementChains()
    {
        RetirementIndexData index = RetirementIndexBuilder.BuildIndex(
            new[]
            {
                CreateRetirement("aaa", new[] { "bbb" }),
                CreateRetirement("bbb", new[] { "ccc" })
            });
        RetirementChainRow chain = index.ChainsByCode["aaa"];

        Assert.Multiple(() =>
        {
            Assert.That(chain.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(chain.Codes, Is.EqualTo(new[] { "aaa", "bbb", "ccc" }));
            Assert.That(chain.FinalCode, Is.EqualTo("ccc"));
            Assert.That(chain.ReplacementCandidates, Is.Empty);
        });
    }

    [Test]
    public void RetirementReplacementCyclesProduceFiniteDiagnosticChains()
    {
        RetirementIndexData index = RetirementIndexBuilder.BuildIndex(
            new[]
            {
                CreateRetirement("aaa", new[] { "bbb" }),
                CreateRetirement("bbb", new[] { "aaa" })
            });
        RetirementChainRow chain = index.ChainsByCode["aaa"];

        Assert.Multiple(() =>
        {
            Assert.That(chain.Status, Is.EqualTo(LanguageNormalizationStatus.ReplacementCycle));
            Assert.That(chain.Codes, Is.EqualTo(new[] { "aaa", "bbb" }));
            Assert.That(chain.FinalCode, Is.Null);
            Assert.That(chain.ReplacementCandidates, Is.Empty);
        });
    }

    [Test]
    public void RetirementLookupsReturnDefensiveNestedSnapshots()
    {
        RetirementRow? firstRecord = RetirementIndex.FindRetirement("agp");
        RetirementRow? secondRecord = RetirementIndex.FindRetirement("agp");
        RetirementChainRow? firstChain = RetirementIndex.FindReplacementChain("agp");
        RetirementChainRow? secondChain = RetirementIndex.FindReplacementChain("agp");

        firstRecord!.ReplacementCodes[0] = "changed";
        firstChain!.ReplacementCandidates[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(firstRecord, Is.Not.SameAs(secondRecord));
            Assert.That(secondRecord?.ReplacementCodes[0], Is.EqualTo("apf"));
            Assert.That(firstChain, Is.Not.SameAs(secondChain));
            Assert.That(secondChain?.ReplacementCandidates[0], Is.EqualTo("apf"));
            Assert.That(RetirementIndex.FindRetirement("agp")?.ReplacementCodes[0], Is.EqualTo("apf"));
        });
    }

    [Test]
    public void DuplicateRetirementCodesFailIndexConstruction()
    {
        RetirementRow[] duplicates =
        {
            CreateRetirement("aaa", new[] { "bbb" }),
            CreateRetirement("aaa", new[] { "ccc" })
        };

        Assert.Throws<InvalidOperationException>(
            () => RetirementIndexBuilder.BuildIndex(duplicates));
    }

    private static RelationshipRow CreateSet5Relationship(string parentCode, string childCode)
    {
        return new RelationshipRow(
            parentCode,
            childCode,
            LanguageRelationshipKind.Set5ParentChild,
            ReconciledRelationshipStatus.Active,
            ReconciledSource.Iso639Set5);
    }

    private static RetirementRow CreateRetirement(string code, string[] replacementCodes)
    {
        return new RetirementRow(
            code,
            code,
            LanguageRetirementReason.Change,
            replacementCodes,
            null,
            "2000-01-01");
    }
}
