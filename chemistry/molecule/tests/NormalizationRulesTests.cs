using System.Collections.Generic;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class NormalizationRulesTests
{
    [Test]
    public void Constructor_LoadsBuiltInChargeNormalizationRules()
    {
        NormalizationRules rules = new NormalizationRules();

        Assert.That(rules.ChargeRules.Count, Is.GreaterThanOrEqualTo(8));
        Assert.That(rules.ContainsChargeRule("nitro"), Is.True);
        Assert.That(rules.ContainsChargeRule("n-oxide"), Is.True);
        Assert.That(rules.ContainsChargeRule("sulfoxide"), Is.True);
        Assert.That(rules.ContainsChargeRule("phosphate"), Is.True);
        Assert.That(rules.ContainsChargeRule("carboxylate"), Is.True);
        Assert.That(rules.ContainsChargeRule("azide"), Is.True);
        Assert.That(rules.ContainsChargeRule("diazo"), Is.True);
        Assert.That(rules.ContainsChargeRule("amino-acid-zwitterion"), Is.True);
    }

    [Test]
    public void Constructor_LoadsBuiltInAcidBasePriorityRules()
    {
        NormalizationRules rules = new NormalizationRules();

        Assert.That(rules.AcidBaseRules.Count, Is.GreaterThanOrEqualTo(7));
        Assert.That(rules.ContainsAcidBaseRule("sulfonic-acid"), Is.True);
        Assert.That(rules.ContainsAcidBaseRule("phosphoric-acid"), Is.True);
        Assert.That(rules.ContainsAcidBaseRule("carboxylic-acid"), Is.True);
        Assert.That(rules.ContainsAcidBaseRule("phenol"), Is.True);
        Assert.That(rules.ContainsAcidBaseRule("thiol"), Is.True);
        Assert.That(rules.ContainsAcidBaseRule("alcohol"), Is.True);
        Assert.That(rules.ContainsAcidBaseRule("ammonium"), Is.True);
    }

    [Test]
    public void FindChargeRule_ReturnsPatternsAndAtomRoles()
    {
        NormalizationRules rules = new NormalizationRules();

        ChargeNormalizationRule nitro = rules.FindChargeRule(" Nitro ");
        ChargeNormalizationRule zwitterion = rules.FindChargeRule("AMINO-ACID-ZWITTERION");

        Assert.That(nitro.Name, Is.EqualTo("nitro"));
        Assert.That(nitro.Category, Is.EqualTo(NormalizationRules.ChargeCategory));
        Assert.That(nitro.Priority, Is.EqualTo(10));
        Assert.That(nitro.ReactantPattern, Is.EqualTo("N(=O)=O"));
        Assert.That(nitro.ProductPattern, Is.EqualTo("[N+](=O)[O-]"));
        Assert.That(nitro.PositiveAtomicNumber, Is.EqualTo(7));
        Assert.That(nitro.NegativeAtomicNumber, Is.EqualTo(8));
        Assert.That(zwitterion.ProductPattern, Is.EqualTo("[NH3+]CC(=O)[O-]"));
    }

    [Test]
    public void FindChargeRule_ReturnsEmptyRecordForMissingRule()
    {
        NormalizationRules rules = new NormalizationRules();

        ChargeNormalizationRule missing = rules.FindChargeRule("missing");

        Assert.That(missing.Name, Is.EqualTo(""));
        Assert.That(missing.ReactantPattern, Is.EqualTo(""));
        Assert.That(rules.ContainsChargeRule("missing"), Is.False);
    }

    [Test]
    public void GetRulesByCategory_ReturnsChargeRulesOnlyForChargeCategory()
    {
        NormalizationRules rules = new NormalizationRules();

        List<ChargeNormalizationRule> chargeRules = rules.GetRulesByCategory(" CHARGE ");
        List<ChargeNormalizationRule> unknownRules = rules.GetRulesByCategory("unknown");

        Assert.That(NormalizationRules.ChargeCategory, Is.EqualTo("charge"));
        Assert.That(chargeRules.Count, Is.EqualTo(rules.ChargeRules.Count));
        Assert.That(unknownRules.Count, Is.EqualTo(0));
        Assert.That(AllChargeRules(chargeRules), Is.True);
    }

    [Test]
    public void FindAcidBaseRule_ReturnsPriorityAndTopologyFields()
    {
        NormalizationRules rules = new NormalizationRules();

        AcidBaseRule carboxylicAcid = rules.FindAcidBaseRule(" Carboxylic-Acid ");
        AcidBaseRule alcohol = rules.FindAcidBaseRule("ALCOHOL");

        Assert.That(carboxylicAcid.Name, Is.EqualTo("carboxylic-acid"));
        Assert.That(carboxylicAcid.Category, Is.EqualTo(NormalizationRules.AcidBaseCategory));
        Assert.That(carboxylicAcid.Priority, Is.EqualTo(30));
        Assert.That(carboxylicAcid.AcidAtomicNumber, Is.EqualTo(8));
        Assert.That(carboxylicAcid.NeighborAtomicNumber, Is.EqualTo(6));
        Assert.That(carboxylicAcid.RequiresDoubleOxygenNeighbor, Is.True);
        Assert.That(alcohol.Priority, Is.EqualTo(80));
        Assert.That(alcohol.RequiresDoubleOxygenNeighbor, Is.False);
        Assert.That(rules.GetAcidBaseRulesByCategory(" acid-base ").Count, Is.EqualTo(rules.AcidBaseRules.Count));
    }

    [Test]
    public void GetOrderedChargeRules_SortsByPriorityThenName()
    {
        NormalizationRules rules = new NormalizationRules();
        rules.AddChargeRule("early-b", 5, "B", "B", "test", 0, 0);
        rules.AddChargeRule("early-a", 5, "A", "A", "test", 0, 0);

        List<ChargeNormalizationRule> ordered = rules.GetOrderedChargeRules();

        Assert.That(ordered[0].Name, Is.EqualTo("early-a"));
        Assert.That(ordered[1].Name, Is.EqualTo("early-b"));
        Assert.That(ordered[2].Name, Is.EqualTo("nitro"));
        Assert.That(IsSortedByPriorityThenName(ordered), Is.True);
        Assert.That(rules.ChargeRules[0].Name, Is.EqualTo("nitro"));
    }

    [Test]
    public void GetChargeRules_ReturnsCopyOfRuleList()
    {
        NormalizationRules rules = new NormalizationRules();

        List<ChargeNormalizationRule> copy = rules.GetChargeRules();
        copy.Clear();

        Assert.That(copy.Count, Is.EqualTo(0));
        Assert.That(rules.ChargeRules.Count, Is.GreaterThan(0));
    }

    static bool AllChargeRules(List<ChargeNormalizationRule> rules)
    {
        int index = 0;
        while (index < rules.Count)
        {
            if (rules[index].Category != NormalizationRules.ChargeCategory)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsSortedByPriorityThenName(List<ChargeNormalizationRule> rules)
    {
        int index = 1;
        while (index < rules.Count)
        {
            if (rules[index - 1].Priority > rules[index].Priority)
            {
                return false;
            }

            if (rules[index - 1].Priority == rules[index].Priority && string.CompareOrdinal(rules[index - 1].Name, rules[index].Name) > 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
