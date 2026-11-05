namespace barcode.Tests;

public class Gs1AiTableTests
{
    [Test]
    public void GeneratedTableHasPinnedCountsAndAValidTrie()
    {
        bool[] referencedDefinitions =
            new bool[Gs1AiTable.Definitions.Length];
        int terminalCount = 0;
        int nodeIndex = 0;
        while (nodeIndex < Gs1AiTable.TrieNodeCount)
        {
            int definitionIndex =
                Gs1AiTable.TrieDefinitionIndices[nodeIndex];
            if (definitionIndex >= 0)
            {
                Assert.That(definitionIndex,
                    Is.LessThan(Gs1AiTable.Definitions.Length));
                referencedDefinitions[definitionIndex] = true;
                terminalCount++;
            }

            int digit = 0;
            while (digit < Gs1AiTable.TrieRadix)
            {
                int child = Gs1AiTable.TrieChildIndices[
                    (nodeIndex * Gs1AiTable.TrieRadix) + digit];
                Assert.That(child,
                    Is.EqualTo(-1).Or.InRange(
                        nodeIndex + 1,
                        Gs1AiTable.TrieNodeCount - 1));
                digit++;
            }

            nodeIndex++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(Gs1AiTable.SnapshotDate,
                Is.EqualTo("2026-08-26"));
            Assert.That(Gs1AiTable.SnapshotSha256,
                Is.EqualTo(
                    "1ea02f4e3b81d3092e095987600e01c7fd55310f4da5181f5a070262ff6ffc94"));
            Assert.That(Gs1AiTable.SourceEntryCount, Is.EqualTo(224));
            Assert.That(Gs1AiTable.ExpandedEntryCount, Is.EqualTo(541));
            Assert.That(Gs1AiTable.TrieNodeCount, Is.EqualTo(648));
            Assert.That(Gs1AiTable.Definitions,
                Has.Length.EqualTo(Gs1AiTable.SourceEntryCount));
            Assert.That(Gs1AiTable.TrieDefinitionIndices,
                Has.Length.EqualTo(Gs1AiTable.TrieNodeCount));
            Assert.That(Gs1AiTable.TrieChildIndices,
                Has.Length.EqualTo(
                    Gs1AiTable.TrieNodeCount * Gs1AiTable.TrieRadix));
            Assert.That(terminalCount,
                Is.EqualTo(Gs1AiTable.ExpandedEntryCount));
            Assert.That(referencedDefinitions, Has.All.True);
        });
    }

    [Test]
    public void EveryDeclaredRangeResolvesToItsOwningDefinition()
    {
        int resolvedCount = 0;
        int definitionIndex = 0;
        while (definitionIndex < Gs1AiTable.Definitions.Length)
        {
            Gs1AiDefinition definition =
                Gs1AiTable.Definitions[definitionIndex];
            int start = int.Parse(definition.AiStart);
            int end = int.Parse(definition.AiEnd);
            int width = definition.AiStart.Length;
            int value = start;
            while (value <= end)
            {
                string ai = value.ToString(new string('0', width));
                Assert.That(ResolveDefinitionIndex(ai),
                    Is.EqualTo(definitionIndex),
                    $"AI {ai} did not resolve to its generated definition");
                resolvedCount++;
                value++;
            }

            definitionIndex++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(resolvedCount,
                Is.EqualTo(Gs1AiTable.ExpandedEntryCount));
            Assert.That(ResolveDefinitionIndex(""), Is.EqualTo(-1));
            Assert.That(ResolveDefinitionIndex("0"), Is.EqualTo(-1));
            Assert.That(ResolveDefinitionIndex("04"), Is.EqualTo(-1));
            Assert.That(ResolveDefinitionIndex("9999"), Is.EqualTo(-1));
            Assert.That(ResolveDefinitionIndex("AB"), Is.EqualTo(-1));
        });
    }

    [Test]
    public void GtinDefinitionPreservesFlagsLintersAndAssociations()
    {
        Gs1AiDefinition definition = ResolveDefinition("01");

        Assert.Multiple(() =>
        {
            Assert.That(definition.AiStart, Is.EqualTo("01"));
            Assert.That(definition.AiEnd, Is.EqualTo("01"));
            Assert.That(definition.WildcardPattern, Is.EqualTo("01"));
            Assert.That(definition.PredefinedLength, Is.True);
            Assert.That(definition.DigitalLinkDataAttribute, Is.True);
            Assert.That(definition.Title, Is.EqualTo("GTIN"));
            Assert.That(definition.Components, Has.Length.EqualTo(1));
            Assert.That(definition.Components[0].CharacterSet,
                Is.EqualTo("N"));
            Assert.That(definition.Components[0].MinimumLength,
                Is.EqualTo(14));
            Assert.That(definition.Components[0].MaximumLength,
                Is.EqualTo(14));
            Assert.That(definition.Components[0].Linters,
                Is.EqualTo(new string[] { "csum", "gcppos2" }));
            Assert.That(definition.Exclusions,
                Is.EqualTo(new string[] { "255", "37" }));
            Assert.That(definition.DigitalLinkPrimaryKey, Is.True);
            Assert.That(definition.DigitalLinkQualifierSequences,
                Is.EqualTo(new string[][]
                {
                    new string[] { "22", "10", "21" },
                    new string[] { "235" }
                }));
        });
    }

    [Test]
    public void RangesOptionalComponentsAndSpecialCharacterSetsArePreserved()
    {
        Gs1AiDefinition measured = ResolveDefinition("3102");
        Gs1AiDefinition countries = ResolveDefinition("423");
        Gs1AiDefinition signature = ResolveDefinition("8030");
        Gs1AiDefinition coupon = ResolveDefinition("8110");
        Gs1AiDefinition internalUse = ResolveDefinition("97");

        Assert.Multiple(() =>
        {
            Assert.That(measured.AiStart, Is.EqualTo("3100"));
            Assert.That(measured.AiEnd, Is.EqualTo("3105"));
            Assert.That(measured.WildcardPattern, Is.EqualTo("310n"));
            Assert.That(measured.PredefinedLength, Is.True);
            Assert.That(measured.RequiredAssociations,
                Is.EqualTo(new string[][]
                {
                    new string[] { "01" },
                    new string[] { "02" }
                }));
            Assert.That(measured.Exclusions,
                Is.EqualTo(new string[] { "310n" }));
            Assert.That(countries.Components, Has.Length.EqualTo(5));
            Assert.That(countries.Components[0].Optional, Is.False);
            Assert.That(countries.Components[1].Optional, Is.True);
            Assert.That(countries.Components[4].Optional, Is.True);
            Assert.That(countries.Components[4].Linters,
                Is.EqualTo(new string[] { "iso3166" }));
            Assert.That(signature.Components[0].CharacterSet,
                Is.EqualTo("Z"));
            Assert.That(signature.Components[0].MinimumLength,
                Is.EqualTo(1));
            Assert.That(signature.Components[0].MaximumLength,
                Is.EqualTo(90));
            Assert.That(coupon.Title, Is.Empty);
            Assert.That(coupon.Components[0].Linters,
                Is.EqualTo(new string[] { "couponcode" }));
            Assert.That(internalUse.AiStart, Is.EqualTo("91"));
            Assert.That(internalUse.AiEnd, Is.EqualTo("99"));
            Assert.That(internalUse.WildcardPattern, Is.EqualTo("9n"));
            Assert.That(ResolveDefinition("91"),
                Is.SameAs(ResolveDefinition("99")));
        });
    }

    [Test]
    public void EveryDefinitionHasConsistentComponentAndAssociationData()
    {
        int definitionIndex = 0;
        while (definitionIndex < Gs1AiTable.Definitions.Length)
        {
            Gs1AiDefinition definition =
                Gs1AiTable.Definitions[definitionIndex];
            Assert.Multiple(() =>
            {
                Assert.That(definition.AiStart, Is.Not.Empty);
                Assert.That(definition.AiEnd,
                    Has.Length.EqualTo(definition.AiStart.Length));
                Assert.That(definition.WildcardPattern,
                    Has.Length.EqualTo(definition.AiStart.Length));
                Assert.That(definition.Components, Is.Not.Empty);
                Assert.That(definition.DigitalLinkQualifierSequences.Length == 0
                    || definition.DigitalLinkPrimaryKey,
                    Is.True);
            });

            bool optionalSeen = false;
            int componentIndex = 0;
            while (componentIndex < definition.Components.Length)
            {
                Gs1AiComponentDefinition component =
                    definition.Components[componentIndex];
                Assert.Multiple(() =>
                {
                    Assert.That(component.CharacterSet,
                        Is.EqualTo("N").Or.EqualTo("X")
                            .Or.EqualTo("Y").Or.EqualTo("Z"));
                    Assert.That(component.MinimumLength,
                        Is.GreaterThan(0));
                    Assert.That(component.MaximumLength,
                        Is.GreaterThanOrEqualTo(component.MinimumLength));
                    Assert.That(!optionalSeen || component.Optional, Is.True);
                    Assert.That(
                        component.MinimumLength == component.MaximumLength
                            || componentIndex
                                == definition.Components.Length - 1,
                        Is.True);
                    Assert.That(component.Linters,
                        Has.All.Not.Empty);
                });
                optionalSeen = optionalSeen || component.Optional;
                componentIndex++;
            }

            AssertAssociationGroups(definition.RequiredAssociations);
            Assert.That(definition.Exclusions, Has.All.Not.Empty);
            AssertAssociationGroups(
                definition.DigitalLinkQualifierSequences);
            definitionIndex++;
        }
    }

    static int ResolveDefinitionIndex(string ai)
    {
        int nodeIndex = 0;
        int characterIndex = 0;
        while (characterIndex < ai.Length)
        {
            char character = ai[characterIndex];
            if (character < '0' || character > '9')
            {
                return -1;
            }

            int digit = character - '0';
            nodeIndex = Gs1AiTable.TrieChildIndices[
                (nodeIndex * Gs1AiTable.TrieRadix) + digit];
            if (nodeIndex < 0)
            {
                return -1;
            }

            characterIndex++;
        }

        return Gs1AiTable.TrieDefinitionIndices[nodeIndex];
    }

    static Gs1AiDefinition ResolveDefinition(string ai)
    {
        int definitionIndex = ResolveDefinitionIndex(ai);
        Assert.That(definitionIndex, Is.GreaterThanOrEqualTo(0));
        return Gs1AiTable.Definitions[definitionIndex];
    }

    static void AssertAssociationGroups(string[][] groups)
    {
        int groupIndex = 0;
        while (groupIndex < groups.Length)
        {
            Assert.That(groups[groupIndex], Is.Not.Empty);
            Assert.That(groups[groupIndex], Has.All.Not.Empty);
            groupIndex++;
        }
    }
}
