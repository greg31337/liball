using languagecodes;

namespace languagecodes.Tests;

public class LanguageRelationshipEngineTests
{
    [Test]
    public void ActiveMemberResolvesItsDirectMacrolanguage()
    {
        LanguageCodeEntry? parent =
            LanguageRelationshipEngine.GetMacrolanguageForMember("aao", false);

        Assert.Multiple(() =>
        {
            Assert.That(parent, Is.Not.Null);
            Assert.That(parent?.Set3Code, Is.EqualTo("ara"));
            Assert.That(parent?.Scope, Is.EqualTo(LanguageScope.Macrolanguage));
            Assert.That(parent?.Status, Is.EqualTo(LanguageCodeStatus.Active));
            Assert.That(
                parent?.Relationships.Any(
                    relationship => relationship.Kind ==
                            LanguageRelationshipKind.MacrolanguageMember &&
                        relationship.ParentCode == "ara" &&
                        relationship.ChildCode == "aao"),
                Is.True);
        });
    }

    [Test]
    public void RetiredRelationshipsRequireExplicitInclusion()
    {
        LanguageCodeEntry? excluded =
            LanguageRelationshipEngine.GetMacrolanguageForMember("ajp", false);
        LanguageCodeEntry? included =
            LanguageRelationshipEngine.GetMacrolanguageForMember("ajp", true);
        LanguageCodeEntry[] activeMembers =
            LanguageRelationshipEngine.GetMacrolanguageMembers("ara", false);
        LanguageCodeEntry[] historicalMembers =
            LanguageRelationshipEngine.GetMacrolanguageMembers("ara", true);
        LanguageCodeEntry historicalAjp =
            historicalMembers.Single(entry => entry.Set3Code == "ajp");

        Assert.Multiple(() =>
        {
            Assert.That(excluded, Is.Null);
            Assert.That(included?.Set3Code, Is.EqualTo("ara"));
            Assert.That(activeMembers.Any(entry => entry.Set3Code == "ajp"), Is.False);
            Assert.That(historicalMembers.Any(entry => entry.Set3Code == "ajp"), Is.True);
            Assert.That(historicalMembers.Length, Is.GreaterThan(activeMembers.Length));
            Assert.That(historicalAjp.Status, Is.EqualTo(LanguageCodeStatus.Retired));
            Assert.That(historicalAjp.Retirement, Is.Not.Null);
            Assert.That(historicalAjp.Set3Code, Is.Not.EqualTo("apc"));
        });
    }

    [Test]
    public void MemberEnumerationIsUniqueAndUsesStableCodeOrder()
    {
        LanguageCodeEntry[] members =
            LanguageRelationshipEngine.GetMacrolanguageMembers("ara", true);
        string[] codes = members.Select(StableCode).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(members, Is.Not.Empty);
            Assert.That(codes, Is.EqualTo(codes.OrderBy(code => code, StringComparer.Ordinal)));
            Assert.That(codes.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(codes.Length));
            Assert.That(codes, Does.Contain("aao"));
            Assert.That(codes, Does.Contain("ajp"));
        });
    }

    [Test]
    public void QueriesDoNotSubstituteMembersMacrosOrUnknownCodes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                LanguageRelationshipEngine.GetMacrolanguageForMember("ara", true),
                Is.Null);
            Assert.That(
                LanguageRelationshipEngine.GetMacrolanguageForMember("eng", true),
                Is.Null);
            Assert.That(
                LanguageRelationshipEngine.GetMacrolanguageForMember(null, true),
                Is.Null);
            Assert.That(
                LanguageRelationshipEngine.GetMacrolanguageMembers("aao", true),
                Is.Empty);
            Assert.That(
                LanguageRelationshipEngine.GetMacrolanguageMembers("unknown", true),
                Is.Empty);
            Assert.That(
                LanguageRelationshipEngine.GetMacrolanguageMembers(null, true),
                Is.Empty);
        });
    }

    [Test]
    public void FixtureQueriesPreferActiveParentsAndRemainDirectOnly()
    {
        LanguageRow[] languages =
        {
            CreateRow("set3:aaa", "aaa", LanguageScope.Macrolanguage),
            CreateRow("set3:bbb", "bbb", LanguageScope.Macrolanguage),
            CreateRow("set3:ccc", "ccc", LanguageScope.Individual),
            CreateRow("set3:ddd", "ddd", LanguageScope.Individual)
        };
        RelationshipRow[] rows =
        {
            CreateRelationship("aaa", "bbb", ReconciledRelationshipStatus.Active),
            CreateRelationship("aaa", "bbb", ReconciledRelationshipStatus.Active),
            CreateRelationship("bbb", "ccc", ReconciledRelationshipStatus.Active),
            CreateRelationship("aaa", "ddd", ReconciledRelationshipStatus.Retired),
            CreateRelationship("bbb", "ddd", ReconciledRelationshipStatus.Active)
        };
        RelationshipIndexData relationships = RelationshipIndexBuilder.BuildIndex(rows);
        LanguageCatalogData catalog = LanguageCatalogBuilder.BuildCatalog(
            languages,
            Array.Empty<NameRow>(),
            Array.Empty<AliasRow>(),
            rows,
            Array.Empty<RetirementRow>());

        LanguageCodeEntry? directParent =
            LanguageRelationshipEngine.GetMacrolanguageForMemberEntries(
                "ccc",
                true,
                relationships,
                catalog);
        LanguageCodeEntry? activeParent =
            LanguageRelationshipEngine.GetMacrolanguageForMemberEntries(
                "ddd",
                true,
                relationships,
                catalog);
        LanguageCodeEntry[] directMembers =
            LanguageRelationshipEngine.GetMacrolanguageMemberEntries(
                "aaa",
                false,
                relationships,
                catalog);

        Assert.Multiple(() =>
        {
            Assert.That(directParent?.Set3Code, Is.EqualTo("bbb"));
            Assert.That(activeParent?.Set3Code, Is.EqualTo("bbb"));
            Assert.That(
                directMembers.Select(entry => entry.Set3Code),
                Is.EqualTo(new[] { "bbb" }));
            Assert.That(directMembers.Any(entry => entry.Set3Code == "ccc"), Is.False);
            Assert.That(directMembers.Any(entry => entry.Set3Code == "ddd"), Is.False);
        });
    }

    [Test]
    public void RelationshipResultsAreRecursiveDefensiveSnapshots()
    {
        LanguageCodeEntry? firstParent =
            LanguageRelationshipEngine.GetMacrolanguageForMember("aao", false);
        LanguageCodeEntry? secondParent =
            LanguageRelationshipEngine.GetMacrolanguageForMember("aao", false);
        LanguageCodeEntry[] firstMembers =
            LanguageRelationshipEngine.GetMacrolanguageMembers("ara", false);
        LanguageCodeEntry[] secondMembers =
            LanguageRelationshipEngine.GetMacrolanguageMembers("ara", false);
        LanguageCodeEntry firstAao = firstMembers.Single(entry => entry.Set3Code == "aao");
        LanguageCodeEntry secondAao = secondMembers.Single(entry => entry.Set3Code == "aao");
        string originalName = secondAao.Names[0].Text!;

        firstParent!.Set3Code = "changed";
        firstParent.Relationships[0].ParentCode = "changed";
        firstAao.Set3Code = "changed";
        firstAao.Names[0].Text = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(firstParent, Is.Not.SameAs(secondParent));
            Assert.That(secondParent?.Set3Code, Is.EqualTo("ara"));
            Assert.That(secondParent?.Relationships[0].ParentCode, Is.Not.EqualTo("changed"));
            Assert.That(firstMembers, Is.Not.SameAs(secondMembers));
            Assert.That(firstAao, Is.Not.SameAs(secondAao));
            Assert.That(secondAao.Set3Code, Is.EqualTo("aao"));
            Assert.That(secondAao.Names[0].Text, Is.EqualTo(originalName));
            Assert.That(
                LanguageRelationshipEngine.GetMacrolanguageForMember("aao", false)?.Set3Code,
                Is.EqualTo("ara"));
        });
    }

    private static string StableCode(LanguageCodeEntry entry)
    {
        return LanguageCatalogBuilder.FindStablePublicCode(entry);
    }

    private static LanguageRow CreateRow(
        string key,
        string code,
        LanguageScope scope)
    {
        return new LanguageRow(
            key,
            null,
            null,
            null,
            code,
            null,
            scope,
            LanguageType.Living,
            LanguageCodeStatus.Active,
            ReconciledGroupKind.None,
            null);
    }

    private static RelationshipRow CreateRelationship(
        string parentCode,
        string childCode,
        ReconciledRelationshipStatus status)
    {
        return new RelationshipRow(
            parentCode,
            childCode,
            LanguageRelationshipKind.MacrolanguageMember,
            status,
            ReconciledSource.Iso639Set3);
    }
}
