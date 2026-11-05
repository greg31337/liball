using languagecodes;

namespace languagecodes.Tests;

public sealed class LanguageRelationshipCoverageTests
{
    [Test]
    public void ActiveMacrolanguageMappingSupportsDirectAndReverseQueries()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageCodeEntry? parent = service.GetMacrolanguageForMember("aao", false);
        LanguageCodeEntry[] members = service.GetMacrolanguageMembers("ara", false);
        LanguageCodeEntry member = members.Single(entry => entry.Set3Code == "aao");
        LanguageRelationship? parentRelationship = parent?.Relationships.Single(
            relationship => relationship.Kind == LanguageRelationshipKind.MacrolanguageMember &&
                relationship.ParentCode == "ara" && relationship.ChildCode == "aao");
        LanguageRelationship memberRelationship = member.Relationships.Single(
            relationship => relationship.Kind == LanguageRelationshipKind.MacrolanguageMember &&
                relationship.ParentCode == "ara" && relationship.ChildCode == "aao");

        Assert.Multiple(() =>
        {
            Assert.That(parent?.Set3Code, Is.EqualTo("ara"));
            Assert.That(parent?.Scope, Is.EqualTo(LanguageScope.Macrolanguage));
            Assert.That(parentRelationship?.IsRetired, Is.False);
            Assert.That(parentRelationship?.SourceSet, Is.EqualTo(Iso639Set.Set3));
            Assert.That(member.Scope, Is.EqualTo(LanguageScope.Individual));
            Assert.That(memberRelationship.IsRetired, Is.False);
            Assert.That(memberRelationship.SourceSet, Is.EqualTo(Iso639Set.Set3));
            Assert.That(
                members.Select(StableCode),
                Is.EqualTo(members.Select(StableCode).OrderBy(code => code, StringComparer.Ordinal)));
        });
    }

    [Test]
    public void RetiredMacrolanguageMappingRequiresExplicitInclusionInBothDirections()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageCodeEntry? excludedParent = service.GetMacrolanguageForMember("ajp", false);
        LanguageCodeEntry? includedParent = service.GetMacrolanguageForMember("ajp", true);
        LanguageCodeEntry[] activeMembers = service.GetMacrolanguageMembers("ara", false);
        LanguageCodeEntry[] historicalMembers = service.GetMacrolanguageMembers("ara", true);
        LanguageCodeEntry historicalMember = historicalMembers.Single(entry => entry.Set3Code == "ajp");
        LanguageRelationship historicalRelationship = historicalMember.Relationships.Single(
            relationship => relationship.ParentCode == "ara" && relationship.ChildCode == "ajp");

        Assert.Multiple(() =>
        {
            Assert.That(excludedParent, Is.Null);
            Assert.That(includedParent?.Set3Code, Is.EqualTo("ara"));
            Assert.That(activeMembers.Any(entry => entry.Set3Code == "ajp"), Is.False);
            Assert.That(historicalMembers.Any(entry => entry.Set3Code == "ajp"), Is.True);
            Assert.That(historicalMembers.Length, Is.GreaterThan(activeMembers.Length));
            Assert.That(historicalMember.Status, Is.EqualTo(LanguageCodeStatus.Retired));
            Assert.That(historicalMember.Retirement, Is.Not.Null);
            Assert.That(historicalRelationship.IsRetired, Is.True);
            Assert.That(historicalRelationship.SourceSet, Is.EqualTo(Iso639Set.Set3));
        });
    }

    [Test]
    public void LanguagesWithoutMacrolanguageRelationshipsReturnEmptyOutcomes()
    {
        LanguageCodeService service = new LanguageCodeService();

        Assert.Multiple(() =>
        {
            Assert.That(service.GetMacrolanguageForMember("eng", false), Is.Null);
            Assert.That(service.GetMacrolanguageForMember("ara", true), Is.Null);
            Assert.That(service.GetMacrolanguageForMember("unknown", true), Is.Null);
            Assert.That(service.GetMacrolanguageForMember(null, true), Is.Null);
            Assert.That(service.GetMacrolanguageMembers("eng", false), Is.Empty);
            Assert.That(service.GetMacrolanguageMembers("aao", true), Is.Empty);
            Assert.That(service.GetMacrolanguageMembers("unknown", true), Is.Empty);
            Assert.That(service.GetMacrolanguageMembers(null, true), Is.Empty);
        });
    }

    [Test]
    public void Set5RootAndLeafHaveEmptyOppositeSides()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageCodeEntry[] rootParents = service.GetGroupParents("ine", true);
        LanguageCodeEntry[] rootChildren = service.GetGroupChildren("ine", false);
        LanguageCodeEntry[] leafParents = service.GetGroupParents("gmw", false);
        LanguageCodeEntry[] leafChildren = service.GetGroupChildren("gmw", true);

        Assert.Multiple(() =>
        {
            Assert.That(rootParents, Is.Empty);
            Assert.That(rootChildren, Is.Not.Empty);
            Assert.That(rootChildren.Any(entry => entry.Set5Code == "gem"), Is.True);
            Assert.That(leafParents.Select(entry => entry.Set5Code), Is.EqualTo(new[] { "gem" }));
            Assert.That(leafChildren, Is.Empty);
        });
    }

    [Test]
    public void Set5TraversalDistinguishesDirectEdgesFromMultipleLevels()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageCodeEntry[] directChildren = service.GetGroupChildren("ine", false);
        LanguageCodeEntry[] allChildren = service.GetGroupChildren("ine", true);
        LanguageCodeEntry[] directParents = service.GetGroupParents("gmw", false);
        LanguageCodeEntry[] allParents = service.GetGroupParents("gmw", true);
        LanguageCodeEntry germanic = allChildren.Single(entry => entry.Set5Code == "gem");
        LanguageRelationship edge = germanic.Relationships.Single(
            relationship => relationship.Kind == LanguageRelationshipKind.Set5ParentChild &&
                relationship.ParentCode == "ine" && relationship.ChildCode == "gem");

        Assert.Multiple(() =>
        {
            Assert.That(directChildren.Any(entry => entry.Set5Code == "gem"), Is.True);
            Assert.That(directChildren.Any(entry => entry.Set5Code == "gmw"), Is.False);
            Assert.That(allChildren.Any(entry => entry.Set5Code == "gem"), Is.True);
            Assert.That(allChildren.Any(entry => entry.Set5Code == "gmw"), Is.True);
            Assert.That(allChildren.Length, Is.GreaterThan(directChildren.Length));
            Assert.That(directParents.Select(entry => entry.Set5Code), Is.EqualTo(new[] { "gem" }));
            Assert.That(allParents.Select(entry => entry.Set5Code), Is.EqualTo(new[] { "gem", "ine" }));
            Assert.That(edge.SourceSet, Is.EqualTo(Iso639Set.Set5));
            Assert.That(edge.IsRetired, Is.False);
            AssertStableUniqueOrder(allChildren);
            AssertStableUniqueOrder(allParents);
        });
    }

    [Test]
    public void UnknownAndWrongKindGroupCodesReturnEmptyResults()
    {
        LanguageCodeService service = new LanguageCodeService();

        Assert.Multiple(() =>
        {
            Assert.That(service.GetGroupParents("eng", true), Is.Empty);
            Assert.That(service.GetGroupChildren("eng", true), Is.Empty);
            Assert.That(service.GetGroupParents("unknown", true), Is.Empty);
            Assert.That(service.GetGroupChildren("unknown", true), Is.Empty);
            Assert.That(service.GetGroupParents(null, true), Is.Empty);
            Assert.That(service.GetGroupChildren(string.Empty, true), Is.Empty);
        });
    }

    private static void AssertStableUniqueOrder(LanguageCodeEntry[] entries)
    {
        string[] codes = entries.Select(StableCode).ToArray();
        Assert.That(codes, Is.EqualTo(codes.OrderBy(code => code, StringComparer.Ordinal)));
        Assert.That(codes.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(codes.Length));
    }

    private static string StableCode(LanguageCodeEntry entry)
    {
        return entry.Set3Code ?? entry.Set5Code ?? entry.Set2TerminologyCode ??
            entry.Set2BibliographicCode ?? entry.Alpha2Code ?? string.Empty;
    }
}
